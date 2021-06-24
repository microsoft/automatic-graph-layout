using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using Microsoft.Msagl.Prototype.Ranking;
using Microsoft.Msagl.Routing.Spline.Bundling;

namespace Microsoft.Msagl.Miscellaneous {
    /// <summary>
    /// A set of helper methods for executing a layout.
    /// These exist for compatibility with previous consumers of MSAGL,
    /// but the new APIs should be used for new code.
    /// </summary>
    public static class LayoutHelpers {
        /// <summary>
        /// Calculates the graph layout
        /// </summary>
        /// <exception cref="System.OperationCanceledException">Thrown when the layout is canceled.</exception>
        public static void CalculateLayout(GeometryGraph geometryGraph, LayoutAlgorithmSettings settings, CancelToken cancelToken, string tileDirectory = null) {
            if (settings is RankingLayoutSettings) {
                var rankingLayoutSettings = settings as RankingLayoutSettings;
                var rankingLayout = new RankingLayout(rankingLayoutSettings, geometryGraph);
                rankingLayout.Run(cancelToken);
                RouteAndLabelEdges(geometryGraph, settings, geometryGraph.Edges, 0, cancelToken);
            }
            else if (settings is MdsLayoutSettings) {
                var mdsLayoutSettings = settings as MdsLayoutSettings;
                var mdsLayout = new MdsGraphLayout(mdsLayoutSettings, geometryGraph);
                mdsLayout.Run(cancelToken);
                if (settings.EdgeRoutingSettings.EdgeRoutingMode != EdgeRoutingMode.None)
                    RouteAndLabelEdges(geometryGraph, settings, geometryGraph.Edges, 0, cancelToken);
            }
            else if (settings is FastIncrementalLayoutSettings) {
                var incrementalSettings = settings as FastIncrementalLayoutSettings;
                incrementalSettings.AvoidOverlaps = true;
                var initialLayout = new InitialLayout(geometryGraph, incrementalSettings);
                initialLayout.Run(cancelToken);
                if (settings.EdgeRoutingSettings.EdgeRoutingMode != EdgeRoutingMode.None)
                    RouteAndLabelEdges(geometryGraph, settings, geometryGraph.Edges, 0, cancelToken);
                //incrementalSettings.IncrementalRun(geometryGraph);
            }
            else {
                var sugiyamaLayoutSettings = settings as SugiyamaLayoutSettings;
                if (sugiyamaLayoutSettings != null)
                    ProcessSugiamaLayout(geometryGraph, sugiyamaLayoutSettings, cancelToken);
                else {
                    Debug.Assert(settings is LgLayoutSettings);
                    LayoutLargeGraphWithLayers(geometryGraph, settings, cancelToken, tileDirectory);
                }
            }
        }

        /// <summary>
        /// calculates all data necessery for large graph browsing
        /// </summary>
        /// <param name="geometryGraph"></param>
        /// <param name="settings"></param>
        /// <param name="cancelToken"></param>
        static public void LayoutLargeGraphWithLayers(GeometryGraph geometryGraph, LayoutAlgorithmSettings settings, CancelToken cancelToken, string tileDirectory) {
            var largeGraphLayoutSettings = (LgLayoutSettings)settings;
            var largeGraphLayout = new LgInteractor(geometryGraph, largeGraphLayoutSettings, cancelToken);
            largeGraphLayoutSettings.Interactor = largeGraphLayout;
            largeGraphLayout.Run(tileDirectory);
        }

        static public void ComputeNodeLabelsOfLargeGraphWithLayers(GeometryGraph geometryGraph, LayoutAlgorithmSettings settings, List<double> noldeLabelRatios, CancelToken cancelToken) {
            var largeGraphLayoutSettings = (LgLayoutSettings)settings;
            var largeGraphLayout = largeGraphLayoutSettings.Interactor;
            largeGraphLayout.InitNodeLabelWidthToHeightRatios(noldeLabelRatios);
            largeGraphLayout.LabelingOfOneRun();
        }

        static void ProcessSugiamaLayout(GeometryGraph geometryGraph, SugiyamaLayoutSettings sugiyamaLayoutSettings, CancelToken cancelToken) {
            PlaneTransformation originalTransform;
            var transformIsNotIdentity = HandleTransformIsNotIdentity(geometryGraph, sugiyamaLayoutSettings, out originalTransform);

            if (geometryGraph.RootCluster.Clusters.Any()) {
                PrepareGraphForInitialLayoutByCluster(geometryGraph, sugiyamaLayoutSettings);
                var initialBc = new InitialLayoutByCluster(geometryGraph, a => sugiyamaLayoutSettings);
                initialBc.Run(cancelToken);
                //route the rest of the edges, those between the clusters
                var edgesToRoute = sugiyamaLayoutSettings.EdgeRoutingSettings.EdgeRoutingMode == EdgeRoutingMode.SplineBundling ? geometryGraph.Edges.ToArray() : geometryGraph.Edges.Where(e => e.Curve == null).ToArray();
                RouteAndLabelEdges(geometryGraph, sugiyamaLayoutSettings, edgesToRoute, 0, cancelToken);
            }
            else
                geometryGraph.AlgorithmData = SugiyamaLayoutSettings.CalculateLayout(geometryGraph,
                    sugiyamaLayoutSettings, cancelToken);

            if (transformIsNotIdentity)
                sugiyamaLayoutSettings.Transformation = originalTransform;

            PostRunTransform(geometryGraph, sugiyamaLayoutSettings);
        }

        static bool HandleTransformIsNotIdentity(GeometryGraph geometryGraph, SugiyamaLayoutSettings sugiyamaLayoutSettings,
            out PlaneTransformation originalTransform) {
            var transformIsNotIdentity = !sugiyamaLayoutSettings.Transformation.IsIdentity;
            originalTransform = sugiyamaLayoutSettings.Transformation;
            if (transformIsNotIdentity) {
                var m = sugiyamaLayoutSettings.Transformation.Inverse;
                foreach (Node n in geometryGraph.Nodes) n.Transform(m);
                //calculate new label widths and heights
                foreach (Edge e in geometryGraph.Edges) {
                    if (e.Label != null) {
                        e.OriginalLabelWidth = e.Label.Width;
                        e.OriginalLabelHeight = e.Label.Height;
                        var r = new Rectangle(m * new Point(0, 0), m * new Point(e.Label.Width, e.Label.Height));
                        e.Label.Width = r.Width;
                        e.Label.Height = r.Height;
                    }
                }
            }
            sugiyamaLayoutSettings.Transformation = PlaneTransformation.UnitTransformation; //restore it later
            return transformIsNotIdentity;
        }

        static void PrepareGraphForInitialLayoutByCluster(GeometryGraph geometryGraph,
            SugiyamaLayoutSettings sugiyamaLayoutSettings) {
            foreach (var cluster in geometryGraph.RootCluster.AllClustersDepthFirst()) {
                if (cluster.RectangularBoundary == null)
                    cluster.RectangularBoundary = new RectangularClusterBoundary { TopMargin = 10 };

                if (cluster.BoundaryCurve == null)
                    cluster.BoundaryCurve = new RoundedRect(new Rectangle(0, 0, 10, 10), 3, 3);
            }

            foreach (var edge in geometryGraph.Edges) {
                edge.Curve = null;
                if (edge.SourcePort == null) {
                    var e = edge;
#if SHARPKIT // Lambdas bind differently in JS
                    edge.SourcePort = ((Func<Edge,RelativeFloatingPort>)(ed => new RelativeFloatingPort(() => ed.Source.BoundaryCurve,
                        () => ed.Source.Center)))(e);
#else
                    edge.SourcePort = new RelativeFloatingPort(() => e.Source.BoundaryCurve,
                        () => e.Source.Center);
#endif
                }
                if (edge.TargetPort == null) {
                    var e = edge;
#if SHARPKIT // Lambdas bind differently in JS
                    edge.TargetPort = ((Func<Edge, RelativeFloatingPort>)(ed => new RelativeFloatingPort(() => ed.Target.BoundaryCurve,
                        () => ed.Target.Center)))(e);
#else
                    edge.TargetPort = new RelativeFloatingPort(() => e.Target.BoundaryCurve,
                        () => e.Target.Center);
#endif
                }
            }
            if (sugiyamaLayoutSettings.FallbackLayoutSettings == null)
                sugiyamaLayoutSettings.FallbackLayoutSettings = new FastIncrementalLayoutSettings() {
                    AvoidOverlaps = true
                };
            AddOrphanNodesToRootCluster(geometryGraph);
        }

        static void AddOrphanNodesToRootCluster(GeometryGraph geometryGraph) {
            var clusterNodeSet = new Set<Node>();
            foreach (var cl in geometryGraph.RootCluster.AllClustersDepthFirst())
                clusterNodeSet.InsertRange(cl.Nodes);
            foreach (var node in geometryGraph.Nodes) {
                if (clusterNodeSet.Contains(node)) continue;
                geometryGraph.RootCluster.AddNode(node);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="geometryGraph"></param>
        /// <param name="layoutSettings"></param>
        /// <param name="edgesToRoute"></param>
        public static void RouteAndLabelEdges(GeometryGraph geometryGraph, LayoutAlgorithmSettings layoutSettings, IEnumerable<Edge> edgesToRoute, int straighLineRoutingThreshold, CancelToken cancelToken) {
            //todo: what about parent edges!!!!
            var filteredEdgesToRoute =
                edgesToRoute.Where(e => !e.UnderCollapsedCluster()).ToArray();
            
            var ers = layoutSettings.EdgeRoutingSettings;
            var mode = (straighLineRoutingThreshold == 0 || geometryGraph.Nodes.Count < straighLineRoutingThreshold) ? ers.EdgeRoutingMode : EdgeRoutingMode.StraightLine; 
            if (mode == EdgeRoutingMode.Rectilinear ||
                mode == EdgeRoutingMode.RectilinearToCenter) {
                RectilinearInteractiveEditor.CreatePortsAndRouteEdges(
                    layoutSettings.NodeSeparation / 3,
                    layoutSettings.NodeSeparation / 3,
                    geometryGraph.Nodes,
                    edgesToRoute,
                    mode,
                    true,
                    ers.UseObstacleRectangles,
                    ers.BendPenalty, cancelToken);
            }
            else if (mode == EdgeRoutingMode.Spline || mode == EdgeRoutingMode.SugiyamaSplines) {
                new SplineRouter(geometryGraph, filteredEdgesToRoute, ers.Padding, ers.PolylinePadding, ers.ConeAngle, null) {
                    ContinueOnOverlaps = true,
                    KeepOriginalSpline = ers.KeepOriginalSpline
                }.Run(cancelToken);
            }
            else if (mode == EdgeRoutingMode.SplineBundling) {
                var edgeBundlingSettings = ers.BundlingSettings ?? new BundlingSettings();
                var bundleRouter = new SplineRouter(geometryGraph, filteredEdgesToRoute, ers.Padding, ers.PolylinePadding, ers.ConeAngle,
                                                    edgeBundlingSettings) {
                    KeepOriginalSpline = ers.KeepOriginalSpline
                };
                bundleRouter.Run(cancelToken);
                if (bundleRouter.OverlapsDetected) {
                    new SplineRouter(geometryGraph, filteredEdgesToRoute, ers.Padding, ers.PolylinePadding, ers.ConeAngle, null) {
                        ContinueOnOverlaps = true,
                        KeepOriginalSpline = ers.KeepOriginalSpline
                    }.Run(cancelToken);
                }
            }
            else if (mode == EdgeRoutingMode.StraightLine) {
                var router = new StraightLineEdges(filteredEdgesToRoute, ers.Padding);
                router.Run();
            }
            var elb = new EdgeLabelPlacement(geometryGraph.Nodes, filteredEdgesToRoute);
            elb.Run();
            geometryGraph.UpdateBoundingBox();
        }



        /// <summary>
        /// adaptes to the node boundary curve change
        /// </summary>
        public static void IncrementalLayout(GeometryGraph geometryGraph, Node node, SugiyamaLayoutSettings settings) {
            if (settings == null)
                return;
            var engine = geometryGraph.AlgorithmData as LayeredLayoutEngine;

            if (engine != null) {
                engine.IncrementalRun(node);
                PostRunTransform(geometryGraph, settings);
            }
        }

        static void PostRunTransform(GeometryGraph geometryGraph, SugiyamaLayoutSettings settings) {
            bool transform = !settings.Transformation.IsIdentity;
            if (transform) {
                foreach (Node n in geometryGraph.Nodes)
                    n.Transform(settings.Transformation);
                foreach (var n in geometryGraph.RootCluster.AllClustersDepthFirst()) {
                    n.Transform(settings.Transformation);
                    if (n.BoundaryCurve != null)
                        n.RectangularBoundary.Rect = n.BoundaryCurve.BoundingBox;
                }

                //restore labels widths and heights
                foreach (Edge e in geometryGraph.Edges) {
                    if (e.Label != null) {
                        e.Label.Width = e.OriginalLabelWidth;
                        e.Label.Height = e.OriginalLabelHeight;
                    }
                }

                TransformCurves(geometryGraph, settings);
            }
            geometryGraph.UpdateBoundingBox();
        }

        static void TransformCurves(GeometryGraph geometryGraph, SugiyamaLayoutSettings settings) {
            PlaneTransformation transformation = settings.Transformation;
            geometryGraph.BoundingBox = new Rectangle(transformation * geometryGraph.LeftBottom, transformation * geometryGraph.RightTop);
            foreach (Edge e in geometryGraph.Edges) {
                if (e.Label != null)
                    e.Label.Center = transformation * e.Label.Center;
                if (e.Curve != null) {
                    e.Curve = e.Curve.Transform(transformation);
                    var eg = e.EdgeGeometry;
                    if (eg.SourceArrowhead != null)
                        eg.SourceArrowhead.TipPosition = transformation * eg.SourceArrowhead.TipPosition;
                    if (eg.TargetArrowhead != null)
                        eg.TargetArrowhead.TipPosition = transformation * eg.TargetArrowhead.TipPosition;
                    TransformUnderlyingPolyline(e, settings);
                }
            }
        }

        static void TransformUnderlyingPolyline(Edge e, SugiyamaLayoutSettings settings) {
            if (e.UnderlyingPolyline != null) {
                for (Site s = e.UnderlyingPolyline.HeadSite; s != null; s = s.Next) {
                    s.Point = settings.Transformation * s.Point;
                }
            }
        }
    }
}
