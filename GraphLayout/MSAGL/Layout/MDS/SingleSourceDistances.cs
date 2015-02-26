/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.MDS {
    /// <summary>
    /// Provides functionality for computing distances in a graph.
    /// </summary>
    public class SingleSourceDistances : AlgorithmBase {
        private GeometryGraph graph;

        private Node source;

        private bool directed;

        /// <summary>
        /// Dijkstra algorithm. Computes graph-theoretic distances from a node to
        /// all other nodes in a graph with nonnegative edge lengths.
        /// The distance between a node and itself is 0; the distance between a pair of
        /// nodes for which no connecting path exists is Double.PositiveInfinity.
        /// </summary>
        /// <param name="graph">A graph.</param>
        /// <param name="source">The source node.</param>
        /// <param name="directed">Whether the graph is directed.</param>
        public SingleSourceDistances(GeometryGraph graph, Node source, bool directed)
        {
            this.graph = graph;
            this.source = source;
            this.directed = directed;
        }

        /// <summary>
        /// An array of distances from the source node to all nodes.
        /// Nodes are indexed in their natural order when iterating over them.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "This is performance critical.  Copying the array would be slow.")]
        public double[] Result { get; private set; }

        /// <summary>
        /// Executes the algorithm.
        /// </summary>
        protected override void RunInternal() {
            this.StartListenToLocalProgress(graph.Nodes.Count);

            Result = new double[graph.Nodes.Count];

            var q = new Microsoft.Msagl.Core.DataStructures.GenericBinaryHeapPriorityQueue<Node>();
            Dictionary<Node, double> d = new Dictionary<Node, double>();
            foreach (Node node in graph.Nodes) {
                q.Enqueue(node, Double.PositiveInfinity);
                d[node] = Double.PositiveInfinity;
            }
            q.DecreasePriority(source, 0);

            while (q.Count>0) {
                
                ProgressStep();

                double prio;
                Node u = q.Dequeue(out prio);
                d[u] = prio;
                IEnumerator<Edge> enumerator;
                if (directed)
                    enumerator = u.OutEdges.GetEnumerator();
                else
                    enumerator = u.Edges.GetEnumerator();
                while (enumerator.MoveNext()) {
                    Edge uv = enumerator.Current;
                    Node v = uv.Target;
                    if (u == v)
                        v = uv.Source;
                    // relaxation step
                    if (d[v] > d[u] + uv.Length) {
                        d[v] = d[u] + uv.Length;
                        q.DecreasePriority(v, d[v]);
                    }
                }
            }
            int i = 0;
            foreach (Node v in graph.Nodes) {
#if SHARPKIT //https://github.com/SharpKit/SharpKit/issues/7 out keyword not working with arrays
                double dummy;
                if (!d.TryGetValue(v, out dummy))
                    dummy = Double.PositiveInfinity;
                Result[i] = dummy;
#else
                if (!d.TryGetValue(v, out Result[i]))
                    Result[i] = Double.PositiveInfinity;
#endif
                i++;
            }
        }


        ///// <summary>
        ///// Computes distances between a selected set of nodes and all nodes.
        ///// Pivot nodes are selected with maxmin strategy (first at random, later
        ///// ones to maximize distances to all previously selected ones).
        ///// </summary>
        ///// <param name="graph">A graph.</param>
        ///// <param name="directed">Whether shortest paths are directed.</param>
        ///// <param name="numberOfPivots">Number of pivots.</param>
        ///// <returns>A square matrix with shortest path distances.</returns>
        //public static double[][] PivotUniformDistances(GeometryGraph graph, bool directed, int numberOfPivots) {
        //    double[][] d = new double[numberOfPivots][];

        //    Node[] nodes = new Node[graph.Nodes.Count];
        //    graph.Nodes.CopyTo(nodes, 0);
        //    double[] min = new double[graph.Nodes.Count];
        //    for (int i = 0; i < min.Length; i++) {
        //        min[i] = Double.PositiveInfinity;
        //    }
        //    System.Console.Write("pivoting ");
        //    Node pivot = nodes[0];
        //    for (int i = 0; i < numberOfPivots; i++) {
        //        System.Console.Write(".");
        //        d[i] = SingleSourceUniformDistances(graph, pivot, directed);
        //        int argmax = 0;
        //        for (int j = 0; j < d[i].Length; j++) {
        //            min[j] = Math.Min(min[j], d[i][j]);
        //            if (min[j] > min[argmax])
        //                argmax = j;
        //        }
        //        pivot = nodes[argmax];
        //    }
        //    System.Console.WriteLine();
        //    return d;
        //}




        ///// <summary>
        ///// Determines whether the graph is (weakly) connected, that is,
        ///// if there is a path connecting every two nodes.
        ///// </summary>
        ///// <param name="graph">A graph.</param>
        ///// <returns>true iff the graph is connected.</returns>
        //public static bool IsConnected(GeometryGraph graph) {
        //    IEnumerator<Node> enumerator = graph.Nodes.GetEnumerator();
        //    enumerator.MoveNext();
        //    Node node=enumerator.Current;
        //    double[] distances=SingleSourceUniformDistances(graph, node, false);
        //    for (int i = 0; i < distances.Length; i++) {
        //        if (distances[i] == Double.PositiveInfinity) return false;
        //    }
        //    return true;
        //}

        /// <summary>
        /// Gives graphs representing the connected components of the graph
        /// </summary>
        /// <param name="graph">A graph.</param>
        /// <param name="nodeToNodeIndex">the dictionary: node -> node index in the NodeMap</param>
        /// <returns>An array of connected components.</returns>
        
        internal static GeometryGraph[] ComponentGraphs(GeometryGraph graph, Dictionary<Node, int> nodeToNodeIndex) {
            Node[] nodes = new Node[graph.Nodes.Count];
            graph.Nodes.CopyTo(nodes, 0);

            BasicGraph<IntPair> basicGraph = new BasicGraph<IntPair>(
                from edge in graph.Edges 
                where ! (edge.Source is Cluster || edge.Target is Cluster)
                select new IntPair(nodeToNodeIndex[edge.Source], nodeToNodeIndex[edge.Target]), graph.Nodes.Count);
            List<IEnumerable<int>> comps = new List<IEnumerable<int>>( ConnectedComponentCalculator<IntPair>.GetComponents(basicGraph));
            if (comps.Count == 1)
                return new GeometryGraph[] { graph };
            GeometryGraph[] ret = new GeometryGraph[comps.Count];
            int i = 0;
            foreach (var comp in comps)
                ret[i++] = GeomGraphFromBasicGraph(comp, nodes);

            return ret;
        }

        private static GeometryGraph GeomGraphFromBasicGraph(IEnumerable<int> componentNodeIndices, Node[] nodes) {
            GeometryGraph geometryGraph = new GeometryGraph();

            Dictionary<Node, Node> nodeMap = new Dictionary<Node, Node>();
            foreach (int v in componentNodeIndices) {
                Node originalNode = nodes[v];
                Node newNode = new Node(originalNode.BoundaryCurve.Clone());
                newNode.UserData = originalNode;
                newNode.Center = originalNode.Center;
                geometryGraph.Nodes.Add(newNode);
                nodeMap[originalNode] = newNode;
            }

            foreach (int v in componentNodeIndices){
                Node node = nodes[v]; 
                //avoiding adding InEdges, otherwise each edge will appear twice
                foreach (Edge e in node.OutEdges) {
                    Edge ed = new Edge(nodeMap[e.Source], nodeMap[e.Target]);
                    ed.Length = e.Length;
                    geometryGraph.Edges.Add(ed);
                }
                //self edges do not play any role here
            }
            Debug.Assert(IsConnected(geometryGraph));
            return geometryGraph;
        }

        static bool IsConnected(GeometryGraph geometryGraph) {
            return GraphConnectedComponents.CreateComponents(geometryGraph.Nodes, geometryGraph.Edges).Count() == 1;            
        }
    }
}
