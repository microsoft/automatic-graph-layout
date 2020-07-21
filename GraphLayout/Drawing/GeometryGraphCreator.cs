using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.Layout.Layered;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    ///     a helper class to create a geometry graph
    /// </summary>
    public sealed class GeometryGraphCreator {
        readonly Graph drawingGraph;
        readonly Dictionary<Node, Core.Layout.Node> nodeMapping = new Dictionary<Node, Core.Layout.Node>();

        internal GeometryGraphCreator(Graph drawingGraph) {
            this.drawingGraph = drawingGraph;
        }

        internal GeometryGraph Create() {
            var msaglGraph = new GeometryGraph();
            return FillGraph(msaglGraph);
        }

        GeometryGraph FillGraph(GeometryGraph geometryGraph) {
            ProcessNodes(geometryGraph);
            if (drawingGraph.RootSubgraph != null) {
                geometryGraph.RootCluster = ProcessSubGraphs(drawingGraph.RootSubgraph);
                geometryGraph.RootCluster.GeometryParent = geometryGraph;
            }
            ProcessEdges(geometryGraph);
            ProcessGraphAttrs(drawingGraph, geometryGraph, drawingGraph.LayoutAlgorithmSettings);
            return geometryGraph;
        }

        GeometryGraph FillPhyloTree(GeometryGraph msaglGraph) {
            ProcessNodes(msaglGraph);
            ProcessPhyloEdges(drawingGraph, msaglGraph);
            ProcessGraphAttrs(drawingGraph, msaglGraph, drawingGraph.LayoutAlgorithmSettings);
            return msaglGraph;
        }

        void ProcessEdges(GeometryGraph msaglGraph) {
            foreach (Edge drawingEdge in drawingGraph.Edges) {
                Core.Layout.Node sourceNode = nodeMapping[drawingEdge.SourceNode];
                Core.Layout.Node targetNode = nodeMapping[drawingEdge.TargetNode];

                if (sourceNode == null) {
                    sourceNode = CreateGeometryNode(drawingGraph, msaglGraph,
                                                    drawingGraph.FindNode(drawingEdge.Source),
                                                    ConnectionToGraph.Connected);
                    nodeMapping[drawingEdge.SourceNode] = sourceNode;
                }
                if (targetNode == null) {
                    targetNode = CreateGeometryNode(drawingGraph, msaglGraph,
                                                    drawingGraph.FindNode(drawingEdge.Target),
                                                    ConnectionToGraph.Connected);
                    nodeMapping[drawingEdge.TargetNode] = targetNode;
                }

                var msaglEdge = CreateGeometryEdgeAndAddItToGeometryGraph(drawingEdge, msaglGraph);
               
            }
        }

        /// <summary>
        /// create a geometry edge, the geometry source and target have to be set already
        /// </summary>
        /// <param name="drawingEdge"></param>
        /// <param name="msaglGraph"></param>
        /// <returns></returns>
        static Core.Layout.Edge CreateGeometryEdgeAndAddItToGeometryGraph(Edge drawingEdge, GeometryGraph msaglGraph) {
            var msaglEdge = CreateGeometryEdgeFromDrawingEdge(drawingEdge);

            msaglGraph.Edges.Add(msaglEdge);
            
            return msaglEdge;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="drawingEdge"></param>
        /// <returns></returns>
        public static Core.Layout.Edge CreateGeometryEdgeFromDrawingEdge(Edge drawingEdge) {
            var sourceNode = drawingEdge.SourceNode.GeometryNode;
            var targetNode = drawingEdge.TargetNode.GeometryNode;
            var msaglEdge = new Core.Layout.Edge(sourceNode, targetNode);
            drawingEdge.GeometryEdge = msaglEdge;
            msaglEdge.SourcePort = drawingEdge.SourcePort;
            msaglEdge.TargetPort = drawingEdge.TargetPort;

            if (drawingEdge.Label != null) {
                msaglEdge.Label = drawingEdge.Label.GeometryLabel;
                msaglEdge.Label.GeometryParent = msaglEdge;
            }
            msaglEdge.Weight = drawingEdge.Attr.Weight;
            msaglEdge.Length = drawingEdge.Attr.Length;
            msaglEdge.Separation = drawingEdge.Attr.Separation;
            if (drawingEdge.Attr.ArrowAtSource) {
                msaglEdge.EdgeGeometry.SourceArrowhead = new Arrowhead {Length = drawingEdge.Attr.ArrowheadLength};
            }
            if (drawingEdge.Attr.ArrowAtTarget) {
                msaglEdge.EdgeGeometry.TargetArrowhead = new Arrowhead {Length = drawingEdge.Attr.ArrowheadLength};
            }
            msaglEdge.UserData = drawingEdge;
            msaglEdge.LineWidth = drawingEdge.Attr.LineWidth;
            return msaglEdge;
        }

        void ProcessPhyloEdges(Graph graph, GeometryGraph msaglGraph) {
            foreach (Edge e in graph.Edges) {
                Core.Layout.Node sourceNode = nodeMapping[e.SourceNode];
                Core.Layout.Node targetNode = nodeMapping[e.TargetNode];

                if (sourceNode == null) {
                    sourceNode = CreateGeometryNode(graph, msaglGraph, graph.FindNode(e.Source),
                                                    ConnectionToGraph.Connected);
                    nodeMapping[e.SourceNode] = sourceNode;
                }
                if (targetNode == null) {
                    targetNode = CreateGeometryNode(graph, msaglGraph, graph.FindNode(e.Target),
                                                    ConnectionToGraph.Connected);
                    nodeMapping[e.TargetNode] = targetNode;
                }

                Core.Layout.Edge msaglEdge = new Prototype.Phylo.PhyloEdge(sourceNode, targetNode);
                msaglEdge.Weight = e.Attr.Weight;
                msaglEdge.Separation = e.Attr.Separation;
                if (e.Attr.ArrowAtSource) {
                    msaglEdge.EdgeGeometry.SourceArrowhead = new Arrowhead {Length = e.Attr.ArrowheadLength};
                }
                if (e.Attr.ArrowAtTarget) {
                    msaglEdge.EdgeGeometry.TargetArrowhead = new Arrowhead {Length = e.Attr.ArrowheadLength};
                }
                msaglGraph.Edges.Add(msaglEdge);
                msaglEdge.UserData = e;
                msaglEdge.LineWidth = e.Attr.LineWidth;
            }
        }


        void ProcessNodes(GeometryGraph msaglGraph) {
            foreach (Node n in drawingGraph.Nodes)
                nodeMapping[n] = CreateGeometryNode(drawingGraph, msaglGraph, n, ConnectionToGraph.Connected);
            foreach (Node n in SubgraphNodes())
                if (!nodeMapping.ContainsKey(n))
                    nodeMapping[n] = CreateGeometryNode(drawingGraph, msaglGraph, n, ConnectionToGraph.Disconnected);
        }

        IEnumerable<Node> SubgraphNodes() {
            if (drawingGraph.RootSubgraph == null) yield break;
            foreach (Subgraph sg in drawingGraph.RootSubgraph.Subgraphs)
                foreach (Node node in sg.Nodes)
                    yield return node;
        }

        Cluster ProcessSubGraphs(Subgraph subgraph) {
            var geomCluster = new Cluster(subgraph.Nodes.Select(n => nodeMapping[n]),
                                          subgraph.Subgraphs.Select(ProcessSubGraphs));
            foreach (Cluster sub in geomCluster.Clusters)
                sub.GeometryParent = geomCluster;
            subgraph.GeometryNode = geomCluster;
            geomCluster.UserData = subgraph;
            nodeMapping[subgraph] = geomCluster;
            return geomCluster;
        }

        static void ProcessGraphAttrs(Graph graph, GeometryGraph msaglGraph, LayoutAlgorithmSettings settings) {
            msaglGraph.Margins = graph.Attr.Margin;
            var ss = settings as SugiyamaLayoutSettings;
            if (ss != null) {
                switch (graph.Attr.LayerDirection) {
                    case LayerDirection.None:
                    case LayerDirection.TB:
                        break;
                    case LayerDirection.LR:
                        ss.Transformation = PlaneTransformation.Rotation(Math.PI/2);
                        break;
                    case LayerDirection.RL:
                        ss.Transformation = PlaneTransformation.Rotation(-Math.PI/2);
                        break;
                    case LayerDirection.BT:
                        ss.Transformation = PlaneTransformation.Rotation(Math.PI);
                        break;
                    default:
                        throw new InvalidOperationException(); //"unexpected layout direction");
                }

                TransferConstraints(ss, graph);
            }
        }

        static void TransferConstraints(SugiyamaLayoutSettings sugiyamaLayoutSettings, Graph graph) {
            TransferHorizontalConstraints(graph.LayerConstraints.HorizontalConstraints, sugiyamaLayoutSettings);
            TransferVerticalConstraints(graph.LayerConstraints.VerticalConstraints, sugiyamaLayoutSettings);
        }

        static void TransferVerticalConstraints(VerticalConstraintsForLayeredLayout verticalConstraints,
                                                SugiyamaLayoutSettings sugiyamaLayoutSettings) {
            foreach (Node node in verticalConstraints._minLayerOfDrawingGraph)
            {
                CheckGeomNode(node);
                sugiyamaLayoutSettings.PinNodesToMinLayer(node.GeometryNode);
            }
            foreach (Node node in verticalConstraints._maxLayerOfDrawingGraph)
            {
                CheckGeomNode(node);
                sugiyamaLayoutSettings.PinNodesToMaxLayer(node.GeometryNode);
            }
            foreach (var couple in verticalConstraints.SameLayerConstraints)
            {
                CheckGeomNode(couple.Item1); CheckGeomNode(couple.Item2);
                sugiyamaLayoutSettings.PinNodesToSameLayer(couple.Item1.GeometryNode,
                    couple.Item2.GeometryNode);
            }
            foreach (var couple in verticalConstraints.UpDownConstraints)
            {

                CheckGeomNode(couple.Item1); CheckGeomNode(couple.Item2);
                sugiyamaLayoutSettings.AddUpDownConstraint(couple.Item1.GeometryNode,
                    couple.Item2.GeometryNode);
            }
                                                }

        private static void CheckGeomNode(Node node)
        {
            if (node.GeometryNode == null)
                throw new InvalidDataException(
                    String.Format("node \"{0}\" probably does not belong to the drawing graph because its GeometryNode is null", node));
        }

        static void TransferHorizontalConstraints(HorizontalConstraintsForLayeredLayout horizontalConstraints,
                                                  SugiyamaLayoutSettings sugiyamaLayoutSettings) {
            foreach (var couple in horizontalConstraints.UpDownVerticalConstraints)
                sugiyamaLayoutSettings.AddUpDownVerticalConstraint(couple.Item1.GeometryNode,
                                                                   couple.Item2.GeometryNode);
            foreach (var couple in horizontalConstraints.LeftRightConstraints)
                sugiyamaLayoutSettings.AddLeftRightConstraint(couple.Item1.GeometryNode,
                                                              couple.Item2.GeometryNode);

            foreach (var couple in horizontalConstraints.LeftRightNeighbors)
                sugiyamaLayoutSettings.AddSameLayerNeighbors(couple.Item1.GeometryNode,
                                                             couple.Item2.GeometryNode);
        }

        /// <summary>
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static SugiyamaLayoutSettings CreateLayoutSettings(Graph graph) {
            var settings = (SugiyamaLayoutSettings) graph.LayoutAlgorithmSettings;
            if (settings != null) {
                settings.NodeSeparation = graph.Attr.NodeSeparation;
                settings.AspectRatio = graph.attr.AspectRatio;
                settings.MinimalWidth = graph.attr.MinimalWidth;
                settings.MinimalHeight = graph.attr.MinimalHeight;

                switch (graph.Attr.LayerDirection) {
                    case LayerDirection.LR:
                        if (settings.AspectRatio != 0)
                            settings.AspectRatio = 1/settings.AspectRatio;
                        settings.MinimalWidth = graph.attr.MinimalHeight;
                        settings.MinimalHeight = graph.attr.MinimalWidth;
                        break;
                    case LayerDirection.RL:
                        if (settings.AspectRatio != 0)
                            settings.AspectRatio = 1/settings.AspectRatio;
                        settings.MinimalWidth = graph.attr.MinimalHeight;
                        settings.MinimalHeight = graph.attr.MinimalWidth;
                        break;
                    case LayerDirection.BT:
                        break;
                    case LayerDirection.None:
                        break;
                    case LayerDirection.TB:
                        break;
                    default:
                        throw new InvalidOperationException(); //"unexpected layout direction");
                }
            }

            return settings;
        }

        /// <summary>
        ///     a helper function creating a geometry node
        /// </summary>
        /// <param name="drawingGraph"> </param>
        /// <param name="geometryGraph"></param>
        /// <param name="node"></param>
        /// <param name="connection">controls if the node is connected to the graph</param>
        /// <returns></returns>
        public static Core.Layout.Node CreateGeometryNode(Graph drawingGraph, GeometryGraph geometryGraph, Node node,
                                                          ConnectionToGraph connection) {
            var geomNode = new Core.Layout.Node();

            if (connection == ConnectionToGraph.Connected)
                geometryGraph.Nodes.Add(geomNode);

            node.GeometryNode = geomNode;

            geomNode.UserData = node;
            geomNode.Padding = node.Attr.Padding;
            return geomNode;
        }

        internal static GeometryGraph CreatePhyloTree(PhyloTree drawingTree) {
            var creator = new GeometryGraphCreator(drawingTree);
            var phyloTree = new Prototype.Phylo.PhyloTree();
            creator.FillPhyloTree(phyloTree);
            AssignLengthsToGeometryEdges(phyloTree);
            return phyloTree;
        }

        static void AssignLengthsToGeometryEdges(Prototype.Phylo.PhyloTree phyloGeometryTree) {
            foreach (Prototype.Phylo.PhyloEdge msaglEdge in phyloGeometryTree.Edges) {
                var drawingEdge = msaglEdge.UserData as PhyloEdge;
                msaglEdge.Length = drawingEdge.Length;
            }
        }
    }
}