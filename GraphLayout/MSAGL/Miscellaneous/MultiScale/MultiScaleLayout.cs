using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Layout.Incremental;

namespace Microsoft.Msagl.Prototype.MultiScale
{
    class MultiScaleLayout
    {
        /// <summary>
        /// Fastest method we have for laying out large graphs.  Also does a pretty good job
        /// of unfolding graphs.
        /// The idea is that a stack of successively more and more simplified (abridged) graphs
        /// is constructed, then each graph on the stack is laid out, starting nodes at the positions
        /// of their ancestors in the more abridged graph.
        /// </summary>
        public void CalculateLayout(GeometryGraph graph, double edgeLengthOffset, double edgeLengthMultiplier)
        {
            if (graph.Nodes.Count <= 1)
            {
                return;
            }
            GeometryGraph G = graph;
            // build stack of successively more abridged graphs
            var GraphStack = new Stack<GeometryGraph>();
            while (G.Nodes.Count > 3)
            {
                GraphStack.Push(G);
                int n = G.Nodes.Count;
                G = CreateAbridgedGraph(G, edgeLengthOffset, edgeLengthMultiplier);
                if (G.Nodes.Count == n)
                {
                    // if the abridged graph is no smaller then pop back the previous one
                    // and break the loop.  If graph contains more than one component then
                    // Nodes.Count may not get below 3.
                    G = GraphStack.Pop();
                    break;
                }
            }
            // layout most abridged graph
            SimpleLayout(G, 1.0, edgeLengthMultiplier);
            // work back up the stack, expanding each successive graph such that nodes
            // with a single ancestor in the abridged graph are initially placed at the same position
            // as obtained with the previous layout, and nodes which were previously paired
            // are placed at the centroid of their neighbours.  Then apply layout again.
            double totalGraphCount = GraphStack.Count + 1;
            while (GraphStack.Count > 0)
            {
                var toCenter = new List<Node>();
                foreach (var u in G.Nodes)
                {
                    var v = u.UserData as Node;
                    if (v != null)
                    {
                        v.Center = u.Center;
                    }
                    else
                    {
                        var e = u.UserData as Edge;
                        e.Source.Center = e.Target.Center = u.Center;
                        toCenter.Add(e.Source);
                        toCenter.Add(e.Target);
                    }
                }
                toCenter.ForEach(CenterNode);
                G = GraphStack.Pop();
                SimpleLayout(G, GraphStack.Count / totalGraphCount, edgeLengthMultiplier);
            }
        }
        /// <summary>
        /// Place node at the centroid of its neighbours and its initial position
        /// </summary>
        /// <param name="u">node to center</param>
        private static void CenterNode(Node u)
        {
            var c = u.Center;
            int count = 1;
            foreach (var e in u.InEdges)
            {
                c += e.Source.Center;
                ++count;
            }
            foreach (var e in u.OutEdges)
            {
                c += e.Target.Center;
                ++count;
            }
            Random r = new Random(3);
            c += new Point(r.NextDouble(), r.NextDouble());
            u.Center = c / count;
        }
        /// <summary>
        /// Simple unconstrained layout of graph used by multiscale layout
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="edgeLengthMultiplier"></param>
        /// <param name="level">double in range [0,1] indicates how far down the multiscale stack we are
        /// 1 is at the top, 0 at the bottom, controls repulsive force strength and ideal edge length</param>
        private void SimpleLayout(GeometryGraph graph, double level, double edgeLengthMultiplier)
        {
            var settings = new FastIncrementalLayoutSettings
            {
                MaxIterations = 10,
                MinorIterations = 3,
                GravityConstant = 1.0 - level,
                RepulsiveForceConstant =
                    Math.Log(edgeLengthMultiplier * level * 500 + Math.E),
                InitialStepSize = 0.6
            };
            foreach (var e in graph.Edges)
                e.Length *= Math.Log(level * 500 + Math.E);
            settings.InitializeLayout(graph, settings.MinConstraintLevel);

            do
            {
#pragma warning disable 618
                LayoutHelpers.CalculateLayout(graph, settings, null);
#pragma warning restore 618
            } while (settings.Iterations < settings.MaxIterations);
        }
        /// <summary>
        /// Create an abstraction of the graph by pairing edges with the shortest
        /// symmetric difference of neighbour sets.  In a perfect world the result
        /// would have half as many nodes as the input graph.  In practice we only
        /// process the edge list once, and don't pair edges whose ends are already
        /// paired so the output may have more than n/2 nodes.
        /// </summary>
        /// <param name="graph">The input graph</param>
        /// <param name="edgeLengthOffset">Initial edge length adjustment percent before proportional adjustments.</param>
        /// <param name="edgeLengthMultiplier">The percent length adjustment unit.</param>
        /// <returns>an abridged graph</returns>
        private GeometryGraph CreateAbridgedGraph(GeometryGraph graph, double edgeLengthOffset, double edgeLengthMultiplier)
        {
            LayoutAlgorithmHelpers.SetEdgeLengthsProportionalToSymmetricDifference(graph, edgeLengthOffset, edgeLengthMultiplier);
            var g2 = new GeometryGraph();
            var nodes = new Set<Node>(graph.Nodes);
            var rand = new Random(1);
            // edges sorted by increasing weight, edges with the same weight shuffled
            var edges = (from e in graph.Edges
                         let r = rand.Next()
                         orderby e.Length, r
                         select e).ToList();
            // mapping from nodes in graph to nodes in g2
            // each node in g2 may represent either 1 or 2 nodes from graph
            var nodeMap = new Dictionary<Node, Node>();

            // populate g2 with nodes representing pairs of nodes, paired across the shortest edges first
            foreach (var e in edges)
            {
                if (nodes.Contains(e.Source) && nodes.Contains(e.Target))
                {
                    var pairNode = new Node
                    {
                        UserData = e
                    };
                    nodeMap[e.Source] = pairNode;
                    nodeMap[e.Target] = pairNode;
                    g2.Nodes.Add(pairNode);
                    nodes.Remove(e.Source);
                    nodes.Remove(e.Target);
                }
            }
            // populate g2 with remaining singleton nodes from graph
            foreach (var u in nodes)
            {
                var v = new Node
                {
                    UserData = u
                };
                g2.Nodes.Add(v);
                nodeMap[u] = v;
            }
            // populate g2 with edges - no duplicates, reverse duplicates or self edges allowed
            var neighbours = new Set<KeyValuePair<Node, Node>>();
            foreach (var e in edges)
            {
                Node u = nodeMap[e.Source], v = nodeMap[e.Target];
                var pair = new KeyValuePair<Node, Node>(u, v);
                var raip = new KeyValuePair<Node, Node>(v, u);
                if (pair.Key != pair.Value
                    && !neighbours.Contains(pair)
                    && !neighbours.Contains(raip))
                {
                    var e2 = new Edge(u, v) { Length = e.Length };
                    neighbours.Insert(pair);
                    g2.Edges.Add(e2);
                }
            }
            return g2;
        }
    }
}
