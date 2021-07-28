using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// static class for GeometryGraph extension method
    /// </summary>
    public static class GraphConnectedComponents {
        

#if UNTESTED_CONNECTED_COMPONENT_CODE
         class Biconnected
        {
            List<IEnumerable<Node>> components;
            public IEnumerable<IEnumerable<Node>> Components
            {
                get
                {
                    return components;
                }
            }

            HashSet<Node> visited;
            Dictionary<Node, Node> parent;
            Dictionary<Node, int> depth;
            Dictionary<Node, int> low;
            Stack<Edge> stack;

            public Biconnected(GeometryGraph graph)
            {
                components = new List<IEnumerable<Node>>();
                visited = new HashSet<Node>();
                parent = new Dictionary<Node, Node>();
                depth = new Dictionary<Node, int>();
                low = new Dictionary<Node, int>();

                foreach (var v in graph.Nodes)
                {
                    if (!visited.Contains(v))
                    {
                        Visit(v);
                    }
                }
            }

             void Visit(Node u)
            {
                visited.Add(u);
                low[u] = depth[u] = visited.Count;
                foreach (var e in u.OutEdges.Concat(u.InEdges))
                {
                    Visit(u, e);
                }
            }

             void Visit(Node u, Edge e)
            {
                Node v = e.Source == u ? e.Target : e.Source;
                if (!visited.Contains(v))
                {
                    stack.Push(e);
                    parent[v] = u;
                    Visit(v);
                    if (low[u] >= depth[u])
                    {
                        CreateComponent(e);
                    }
                    low[u] = Math.Min(low[u], low[v]);
                }
                else if (parent[u] != v && depth[v] < depth[u])
                {
                    stack.Push(e);
                    low[u] = Math.Min(low[u], depth[v]);
                }
            }

             void CreateComponent(Edge e)
            {
                HashSet<Node> component = new HashSet<Node>();
                Edge f;
                do
                {
                    f = stack.Pop();
                    component.Add(f.Source);
                    component.Add(f.Target);
                } while (f != e);
                components.Add(component);
            }
        }
#endif

        /// <summary>
        /// For a set of nodes and edges that have not already been added to a graph will return an enumerable of new
        /// graphs each of which contains a connected component.
        /// </summary>
        /// <remarks>
        /// Debug.Asserts that Parent of nodes and edges has not yet been assigned to ensure that this is not being
        /// applied to nodes and edges that have already been added to a graph.  Applying this to such edges would
        /// result in the Node InEdges and OutEdges lists containing duplicates.
        /// </remarks>
        /// <returns></returns>
        public static IEnumerable<GeometryGraph> CreateComponents(IList<Node> nodes, IEnumerable<Edge> edges, double nodeSeparation) {
            ValidateArg.IsNotNull(nodes, "nodes");
            ValidateArg.IsNotNull(edges, "edges");
            var nodeIndex = new Dictionary<Node, int>();
            int nodeCount = 0;
            foreach (var v in nodes) {
                // Debug.Assert(v.Parent == null, "Node is already in a graph");
                nodeIndex[v] = nodeCount++;
            }
            var intEdges = new List<SimpleIntEdge>();
            foreach (var e in edges) {
                // Debug.Assert(e.Parent == null, "Edge is already in a graph");
                intEdges.Add(new SimpleIntEdge {Source = nodeIndex[e.Source], Target = nodeIndex[e.Target]});
            }
            var components =
                ConnectedComponentCalculator<SimpleIntEdge>.GetComponents(new BasicGraphOnEdges<SimpleIntEdge>(intEdges,
                                                                                                        nodeCount));
            var nodeToGraph = new Dictionary<Node, GeometryGraph>();
            var graphs = new List<GeometryGraph>();
            foreach (var c in components) {
                var g = new GeometryGraph() { Margins = nodeSeparation/2 };

                foreach (var i in c) {
                    var v = nodes[i];
                    g.Nodes.Add(v);
                    nodeToGraph[v] = g;
                }
                graphs.Add(g);
            }
            foreach (var e in edges) {
                var g = nodeToGraph[e.Source];
                Debug.Assert(nodeToGraph[e.Target] == g, "source and target of edge are not in the same graph");
                g.Edges.Add(e);
            }
            return graphs;
        }

        /// <summary>
        /// Extension method to break a GeometryGraph into connected components taking into consideration clusters.
        /// Leaves the original graph intact, the resultant components contain copies of the original elements, with
        /// the original elements referenced in their UserData properties.
        /// </summary>
        /// <returns>
        /// the set of components, each as its own GeometryGraph.
        /// </returns>
        public static IEnumerable<GeometryGraph> GetClusteredConnectedComponents(this GeometryGraph graph) {
            var flatGraph = FlatGraph(graph);
            var basicFlatGraph = new BasicGraphOnEdges<AlgorithmDataEdgeWrap>(
                from e in flatGraph.Edges
                select (AlgorithmDataEdgeWrap) e.AlgorithmData,
                flatGraph.Nodes.Count);
            var nodes = flatGraph.Nodes.ToList();
            var graphComponents = new List<GeometryGraph>();
            foreach (
                var componentNodes in ConnectedComponentCalculator<AlgorithmDataEdgeWrap>.GetComponents(basicFlatGraph)) {
                var g = new GeometryGraph();
                var topClusters = new List<Cluster>();
                var topNodes = new List<Node>();
                foreach (int i in componentNodes) {
                    var v = nodes[i];
                    var original = (Node) v.UserData;
                    bool topLevel = ((AlgorithmDataNodeWrap) original.AlgorithmData).TopLevel;
                    if (v.UserData is Cluster) {
                        if (topLevel) {
                            topClusters.Add((Cluster) original);
                        }
                    } else {
                        // clear edges, we fix them up below
                        v.ClearEdges();

                        g.Nodes.Add(v);
                        if (topLevel) {
                            topNodes.Add(v);
                        }
                    }
                }

                // copy the cluster hierarchies from the original graph
                int index = g.Nodes.Count;
                if (topClusters.Count != 0) {
                    var root = new Cluster(topNodes);
                    foreach (var top in topClusters) {
                        root.AddChild(CopyCluster(top, ref index));
                    }
                    g.RootCluster = root;
                }

                // add the real edges from the original graph to the component graph
                foreach (var v in g.GetFlattenedNodesAndClusters()) {
                    var original = v.UserData as Node;
                    Debug.Assert(original != null);
                    foreach (var e in original.InEdges) {
                        var source = GetCopy(e.Source);
                        var target = GetCopy(e.Target);
                        var copy = new Edge(source, target) {
                                                                Length = e.Length,
                                                                UserData = e,
                                                                EdgeGeometry = e.EdgeGeometry
                                                            };
                        e.AlgorithmData = copy;
                        g.Edges.Add(copy);
                    }
                }

                graphComponents.Add(g);
            }
            return graphComponents;
        }

        static Node GetCopy(Node node) {
            return ((AlgorithmDataNodeWrap) node.AlgorithmData).node;
        }

        /// <summary>
        /// Create deep copy of Cluster hierarchy, where the nodes are already assumed to have been copied and loaded into the original nodes' AlgorithmData
        /// </summary>
        /// <param name="top">the source whose copy will become the new top of the cluster hierarchy</param>
        /// <param name="index">node counter index to use and increment as we add new Cluster nodes</param>
        /// <returns>Deep copy of cluster hierarchy</returns>
        static Cluster CopyCluster(Cluster top, ref int index) {
            var copy = new Cluster(from v in top.Nodes select GetCopy(v)) {
                                                                              UserData = top,
                                                                              RectangularBoundary =
                                                                                  top.RectangularBoundary,
                                                                              BoundaryCurve = top.BoundaryCurve.Clone(),
																			  CollapsedBoundary=top.CollapsedBoundary==null?null:top.CollapsedBoundary.Clone()
                                                                          };
            top.AlgorithmData = new AlgorithmDataNodeWrap(index++, copy);
            foreach (var c in top.Clusters)
                copy.AddChild(CopyCluster(c, ref index));
            return copy;
        }

        internal class AlgorithmDataNodeWrap {
            internal readonly int index;
            internal readonly Node node;
            internal bool TopLevel;

            internal AlgorithmDataNodeWrap(int index, Node node) {
                this.index = index;
                this.node = node;
            }
        }

        class AlgorithmDataEdgeWrap : IEdge {

            public int Source { get; set; }

            public int Target { get; set; }

            // make a dummy edge with no mapping back to a real edge
            internal static Edge MakeDummyEdgeFromNodeToItsCluster(AlgorithmDataNodeWrap u, AlgorithmDataNodeWrap v, double length) {
                var e = new Edge(u.node, v.node) {
                                                     Length = length,
                                                     AlgorithmData =
                                                         new AlgorithmDataEdgeWrap {Source = u.index, Target = v.index}
                                                 };
                return e;
            }

            // make an edge that corresponds to a real edge
            internal static Edge MakeEdge(Edge original) {
                var u = original.Source.AlgorithmData as AlgorithmDataNodeWrap;
                var v = original.Target.AlgorithmData as AlgorithmDataNodeWrap;
                var e = new Edge(u.node, v.node) {
                                                     Length = original.Length,
                                                     UserData = original,
                                                     AlgorithmData = new AlgorithmDataEdgeWrap {
                                                                                                   Source = u.index,
                                                                                                   Target = v.index
                                                                                               }
                                                 };
                return e;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")
        ]
        internal static GeometryGraph FlatGraph(GeometryGraph graph) {
            // it is a new graph with clusters replaced by nodes and edges connecting nodes to their parent cluster node
            var flatGraph = new GeometryGraph();

            // from the original graph, foreach node v create a new node u with u.UserData pointing back to v
            // and set v.AlgorithmData to a wrapper of u
            foreach (var v in graph.Nodes) {
                Debug.Assert(!(v is Cluster));
                var u = new Node(v.BoundaryCurve.Clone()) {
                                                              UserData = v
                                                          };
                v.AlgorithmData = new AlgorithmDataNodeWrap(flatGraph.Nodes.Count, u);
                flatGraph.Nodes.Add(u);
            }
            double avgLength = 0;
            foreach (var e in graph.Edges) {
                avgLength += e.Length;
                if (e.Source is Cluster || e.Target is Cluster) continue;
                flatGraph.Edges.Add(AlgorithmDataEdgeWrap.MakeEdge(e));
            }
            if (graph.Edges.Count != 0)
                avgLength /= graph.Edges.Count;
            else
                avgLength = 100;

            Cluster rootCluster = graph.RootCluster;
            // create edges from the children of each parent cluster to the parent cluster node
            foreach (var c in rootCluster.AllClustersDepthFirst()) {
                if (c == rootCluster) continue;
                if (c.BoundaryCurve == null)
                    c.BoundaryCurve = CurveFactory.CreateRectangleWithRoundedCorners(10, 10, 1, 1, new Point());
                var uOfCluster = new Node(c.BoundaryCurve.Clone()) {UserData = c};
                var uuOfCluster = new AlgorithmDataNodeWrap(flatGraph.Nodes.Count, uOfCluster);
                c.AlgorithmData = uuOfCluster;
                flatGraph.Nodes.Add(uOfCluster);

                foreach (var v in c.Nodes.Concat(from cc in c.Clusters select (Node) cc))
                    flatGraph.Edges.Add(
                        AlgorithmDataEdgeWrap.MakeDummyEdgeFromNodeToItsCluster(
                            v.AlgorithmData as AlgorithmDataNodeWrap, uuOfCluster,
                            avgLength));
            }

            // mark top-level nodes and clusters
            foreach (var v in rootCluster.Nodes.Concat(from cc in rootCluster.Clusters select (Node) cc))
                ((AlgorithmDataNodeWrap) v.AlgorithmData).TopLevel = true;

            // create edges between clusters
            foreach (var e in graph.Edges)
                if (e.Source is Cluster || e.Target is Cluster)
                    flatGraph.Edges.Add(AlgorithmDataEdgeWrap.MakeEdge(e));

            return flatGraph;
        }
    }
}
