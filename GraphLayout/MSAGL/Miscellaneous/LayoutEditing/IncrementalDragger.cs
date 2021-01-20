using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Miscellaneous.LayoutEditing;
using Microsoft.Msagl.Routing;
using GeomNode = Microsoft.Msagl.Core.Layout.Node;

namespace Microsoft.Msagl.Miscellaneous.LayoutEditing
{
    /// <summary>
    /// 
    /// </summary>
    public class IncrementalDragger {
        GeometryGraph graph { get; set; }
        readonly double nodeSeparation;
        readonly LayoutAlgorithmSettings layoutSettings;
        List<BumperPusher> listOfPushers = new List<BumperPusher>();
        readonly GeomNode[] pushingNodesArray;
        /// <summary>
        /// it is smaller graph that needs to be refreshed by the viewer
        /// </summary>
        public GeometryGraph ChangedGraph;
        Dictionary<EdgeGeometry,LabelFixture> labelFixtures=new Dictionary<EdgeGeometry, LabelFixture>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pushingNodes">the nodes are already at the correct positions</param>
        /// <param name="graph"></param>
        /// <param name="layoutSettings"></param>
        public IncrementalDragger(IEnumerable<GeomNode> pushingNodes, GeometryGraph graph, LayoutAlgorithmSettings layoutSettings) {
            this.graph = graph;
            this.nodeSeparation = layoutSettings.NodeSeparation;
            this.layoutSettings = layoutSettings;
            pushingNodesArray = pushingNodes as GeomNode[] ?? pushingNodes.ToArray();
            Debug.Assert(pushingNodesArray.All(n => DefaultClusterParent(n) == null) ||
                          (new Set<GeomNode>(pushingNodesArray.Select(n => n.ClusterParent))).Count == 1,
                                    "dragged nodes have to belong to the same cluster");
            InitBumperPushers();
        }

        void InitBumperPushers() {
            if (pushingNodesArray.Length == 0) return;
            var cluster = DefaultClusterParent(pushingNodesArray[0]);
            if (cluster == null)
                listOfPushers.Add(new BumperPusher(graph.Nodes, nodeSeparation, pushingNodesArray));
            else {
                listOfPushers.Add( new BumperPusher(cluster.Nodes.Concat(cluster.Clusters), nodeSeparation,
                                                             pushingNodesArray));
                do {
                    var pushingCluster = cluster;
                    cluster = DefaultClusterParent(cluster);
                    if (cluster == null) break;
                    listOfPushers.Add(new BumperPusher(cluster.Nodes.Concat(cluster.Clusters), nodeSeparation,
                                                       new[] {pushingCluster}));

                } while (true);
            }
        }


        static Cluster DefaultClusterParent(GeomNode n) {
            return n.ClusterParent;
        }

        void RunPushers() {
            for (int i = 0; i < listOfPushers.Count;i++ ) {
                var bumperPusher = listOfPushers[i];
                bumperPusher.PushNodes();
                var cluster = DefaultClusterParent(bumperPusher.FirstPushingNode());
                if (cluster == null || cluster==graph.RootCluster) break;
                var box = cluster.BoundaryCurve.BoundingBox;
                cluster.CalculateBoundsFromChildren(layoutSettings.ClusterMargin);
                Debug.Assert(cluster.Nodes.All(n => cluster.BoundingBox.Contains(n.BoundingBox)));
                var newBox = cluster.BoundaryCurve.BoundingBox;
                if (newBox == box) {
                    break;
                }
                listOfPushers[i + 1].UpdateRTreeByChangedNodeBox(cluster, box);
            } 
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="delta"></param>
        public void Drag(Point delta) {
            if(delta.Length>0)
                foreach (var n in pushingNodesArray) {
                    n.Center += delta;
                    var cl = n as Cluster;
                    if (cl != null)
                        cl.DeepContentsTranslation(delta, true);
                }

            RunPushers();
            RouteChangedEdges();
        }

        void RouteChangedEdges() {
            ChangedGraph = GetChangedFlatGraph();
            var changedClusteredGraph = LgInteractor.CreateClusteredSubgraphFromFlatGraph(ChangedGraph, graph);


            InitLabelFixtures(changedClusteredGraph);
            var router = new SplineRouter(changedClusteredGraph, layoutSettings.EdgeRoutingSettings.Padding,
                                          layoutSettings.EdgeRoutingSettings.PolylinePadding,
                                          layoutSettings.EdgeRoutingSettings.ConeAngle,
                                          layoutSettings.EdgeRoutingSettings.BundlingSettings) {
                                              ContinueOnOverlaps
                                                  = true
                                          };

            router.Run();
            PositionLabels(changedClusteredGraph);

        }

        void PositionLabels(GeometryGraph changedClusteredGraph) {
            foreach (var edge in changedClusteredGraph.Edges)
                PositionEdge(edge);
        }

        void PositionEdge(Edge edge) {
            LabelFixture lf;
            if (!labelFixtures.TryGetValue(edge.EdgeGeometry, out lf)) return;
            var curve = edge.Curve;
            var lenAtLabelAttachment = curve.Length*lf.RelativeLengthOnCurve;
            var par = curve.GetParameterAtLength(lenAtLabelAttachment);
            var tang = curve.Derivative(par);
            var norm = (lf.RightSide ? tang.Rotate90Cw() : tang.Rotate90Ccw()).Normalize()*lf.NormalLength;
            edge.Label.Center = curve[par] + norm;           
        }

        void InitLabelFixtures(GeometryGraph changedClusteredGraph) {
            foreach (var edge in changedClusteredGraph.Edges)
                InitLabelFixture(edge);
        }

        void InitLabelFixture(Edge edge) {
            if (edge.Label == null) return;
            if (labelFixtures.ContainsKey(edge.EdgeGeometry)) return;

            var attachmentPar = edge.Curve.ClosestParameter(edge.Label.Center);
            
            var curve = edge.Curve;
            var tang = curve.Derivative(attachmentPar);
            var normal = tang.Rotate90Cw();
            var fromCurveToLabel = edge.Label.Center - curve[attachmentPar];
            var fixture = new LabelFixture() {
                RelativeLengthOnCurve = curve.LengthPartial(0, attachmentPar)/curve.Length,
                NormalLength = fromCurveToLabel.Length,
                RightSide = fromCurveToLabel*normal>0
            };

            labelFixtures[edge.EdgeGeometry] = fixture;
        }


        GeometryGraph GetChangedFlatGraph() {
            var changedNodes = GetChangedNodes();
            var changedEdges = GetChangedEdges(changedNodes);
            foreach (var e in changedEdges) {
                changedNodes.Insert(e.Source);
                changedNodes.Insert(e.Target);
            }

            var changedGraph = new GeometryGraph {
                Nodes = new SimpleNodeCollection(changedNodes),
                Edges = new SimpleEdgeCollection(changedEdges)
            };
            return changedGraph;
        }

        List<Edge> GetChangedEdges(Set<Node> changedNodes) {
            var list = new List<Edge>();
            var box = Rectangle.CreateAnEmptyBox();
            foreach(var node in changedNodes)
                box.Add(node.BoundaryCurve.BoundingBox);


            var boxPoly = box.Perimeter();

            foreach (var e in graph.Edges)
                if (EdgeNeedsRouting(ref box, e, boxPoly, changedNodes))
                    list.Add(e);
            return list;
        }

        bool EdgeNeedsRouting(ref Rectangle box, Edge edge, Polyline boxPolyline, Set<Node> changedNodes) {
            if (edge.Curve == null)
                return true;
            if (changedNodes.Contains(edge.Source) || changedNodes.Contains(edge.Target))
                return true;
            if (edge.Source.BoundaryCurve.BoundingBox.Intersects(box) ||
                edge.Target.BoundaryCurve.BoundingBox.Intersects(box))
                return true;

            if (!edge.BoundingBox.Intersects(box))
                return false;

            return Curve.CurveCurveIntersectionOne(boxPolyline, edge.Curve, false) != null;
        }

        Set<Node> GetChangedNodes() {
            return new Set<Node>(this.listOfPushers.SelectMany(p => p.FixedNodes));
        }
    }
}