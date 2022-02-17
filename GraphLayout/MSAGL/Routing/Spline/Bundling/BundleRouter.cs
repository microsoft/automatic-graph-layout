using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// The class is responsible for general edge bundling with ordered bundles.
    /// Currently the router will fail if there are node overlaps.
    /// </summary>
    public class BundleRouter : AlgorithmBase {
        readonly BundlingSettings bundlingSettings;
        readonly GeometryGraph geometryGraph;
        readonly Edge[] regularEdges;

        double LoosePadding { get; set; }
        //for the shortest path calculation we will use not loosePadding, but loosePadding*SuperLoosePaddingCoefficient
        internal const double SuperLoosePaddingCoefficient = 1.1;

        readonly SdShortestPath shortestPathRouter;
        RectangleNode<Polyline, Point> TightHierarchy { get; set; }
        RectangleNode<Polyline, Point> LooseHierarchy { get; set; }

        ///<summary>
        /// reports the status of the bundling
        ///</summary>
        public BundlingStatus Status { get; set; }

        internal VisibilityGraph VisibilityGraph { get; set; }

        Func<Port, Polyline> loosePolylineOfPort;
    
#if TEST_MSAGL
        void CheckGraph() {
            foreach (var e in geometryGraph.Edges) {
                if (e.Source == e.Target)
                    continue;
                CheckPortOfNode(e.Source, e.SourcePort);
                CheckPortOfNode(e.Target, e.TargetPort);
            }
        }

        static void CheckPortOfNode(Node node, Port nodePort) {
            if (node is Cluster)
                Debug.Assert(nodePort is ClusterBoundaryPort || nodePort is HookUpAnywhereFromInsidePort || nodePort is CurvePort);
        }
#endif

        internal BundleRouter(GeometryGraph geometryGraph, SdShortestPath shortestPathRouter,
                              VisibilityGraph visibilityGraph, BundlingSettings bundlingSettings, double loosePadding, RectangleNode<Polyline, Point> tightHierarchy,
                              RectangleNode<Polyline, Point> looseHierarchy,
                              Dictionary<EdgeGeometry, Set<Polyline>> edgeLooseEnterable, Dictionary<EdgeGeometry, Set<Polyline>> edgeTightEnterable, Func<Port, Polyline> loosePolylineOfPort) {
            ValidateArg.IsNotNull(geometryGraph, "geometryGraph");
            ValidateArg.IsNotNull(bundlingSettings, "bundlingSettings");

            this.geometryGraph = geometryGraph;
            this.bundlingSettings = bundlingSettings;
            regularEdges = geometryGraph.Edges.Where(e => e.Source != e.Target).ToArray();
            VisibilityGraph = visibilityGraph;
            this.shortestPathRouter = shortestPathRouter;
            LoosePadding = loosePadding;
            LooseHierarchy = looseHierarchy;
            TightHierarchy = tightHierarchy;
            EdgeLooseEnterable = edgeLooseEnterable;
            EdgeTightEnterable = edgeTightEnterable;
            this.loosePolylineOfPort = loosePolylineOfPort;
        }

        bool ThereAreOverlaps(RectangleNode<Polyline, Point> hierarchy) {
            return RectangleNodeUtils.FindIntersectionWithProperty(hierarchy, hierarchy, Curve.CurvesIntersect);
        }

        /// <summary>
        /// edge routing with Ordered Bundles:
        /// 1. route edges with bundling
        /// 2. nudge bundles and hubs
        /// 3. order paths
        /// </summary>
        protected override void RunInternal() {
            //TimeMeasurer.DebugOutput("edge bundling started");
            if (ThereAreOverlaps(TightHierarchy)) {
                /*
                LayoutAlgorithmSettings.ShowDebugCurves(
                    TightHierarchy.GetAllLeaves().Select(p => new DebugCurve(100, 1, "black", p)).ToArray());*/
                Status = BundlingStatus.Overlaps;
                TimeMeasurer.DebugOutput("overlaps in edge bundling");
                return;
            }

            FixLocationsForHookAnywherePorts(geometryGraph.Edges);
            if (!RoutePathsWithSteinerDijkstra()) {
                Status = BundlingStatus.EdgeSeparationIsTooLarge;
                return;
            }
            FixChildParentEdges();
            if (!bundlingSettings.StopAfterShortestPaths) {

                var metroGraphData = new MetroGraphData(regularEdges.Select(e => e.EdgeGeometry).ToArray(),
                                                        LooseHierarchy,
                                                        TightHierarchy,
                                                        bundlingSettings,
                                                        shortestPathRouter.CdtProperty,
                                                        EdgeLooseEnterable,
                                                        EdgeTightEnterable,
                                                        loosePolylineOfPort);
                NodePositionsAdjuster.FixRouting(metroGraphData, bundlingSettings);
                new EdgeNudger(metroGraphData, bundlingSettings).Run();
                //TimeMeasurer.DebugOutput("edge bundling ended");
            }
            RouteSelfEdges();
            FixArrowheads();
        }

        /// <summary>
        /// set endpoint of the edge from child to parent (cluster) to the boundary of the parent
        /// TODO: is there a better solution?
        /// </summary>
        void FixChildParentEdges() {
            foreach (var edge in regularEdges) {
                var sPort = edge.SourcePort;
                var ePort = edge.TargetPort;
                if (sPort.Curve.BoundingBox.Contains(ePort.Curve.BoundingBox)) {
                    IntersectionInfo ii = Curve.CurveCurveIntersectionOne(sPort.Curve, new LineSegment(edge.Curve.Start, edge.Curve.End), true);
                    ((Polyline)edge.Curve).StartPoint.Point = ii.IntersectionPoint;
                }
                if (ePort.Curve.BoundingBox.Contains(sPort.Curve.BoundingBox)) {
                    IntersectionInfo ii = Curve.CurveCurveIntersectionOne(ePort.Curve, new LineSegment(edge.Curve.Start, edge.Curve.End), true);
                    ((Polyline)edge.Curve).EndPoint.Point = ii.IntersectionPoint;
                }
            }
        }

        static internal Cdt CreateConstrainedDelaunayTriangulation(RectangleNode<Polyline, Point> looseHierarchy) {
            IEnumerable<Polyline> obstacles = looseHierarchy.GetAllLeaves();

            Rectangle rectangle = (Rectangle)looseHierarchy.Rectangle;
            rectangle.Pad(rectangle.Diagonal / 4);

            var additionalObstacles = new[] {
                rectangle.Perimeter() };

            return GetConstrainedDelaunayTriangulation(obstacles.Concat(additionalObstacles));
        }

        static Cdt GetConstrainedDelaunayTriangulation(IEnumerable<Polyline> obstacles) {
            var constrainedDelaunayTriangulation = new Cdt(null, obstacles, null);
            constrainedDelaunayTriangulation.Run();
            return constrainedDelaunayTriangulation;
        }
#if TEST_MSAGL
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        // ReSharper disable UnusedMember.Local
        void ShowGraphLocal() {
            // ReSharper restore UnusedMember.Local
            var l = new List<ICurve>();
            l.Clear();
            foreach (var e in geometryGraph.Edges) {
                {
                    l.Add(new Ellipse(2, 2, e.Curve.Start));
                    l.Add(CurveFactory.CreateDiamond(5, 5, e.Curve.End));
                    l.Add(e.Curve);
                }
            }
            SplineRouter.ShowVisGraph(VisibilityGraph, LooseHierarchy.GetAllLeaves(), null, l);
        }
#endif

        void FixLocationsForHookAnywherePorts(IEnumerable<Edge> edges) {
            foreach (var edge in edges) {
                var hookPort = edge.SourcePort as HookUpAnywhereFromInsidePort;
                if (hookPort != null)
                    hookPort.SetLocation(FigureOutHookLocation(hookPort.LoosePolyline, edge.TargetPort, edge.EdgeGeometry));
                else {
                    hookPort = edge.TargetPort as HookUpAnywhereFromInsidePort;
                    if (hookPort != null)
                        hookPort.SetLocation(FigureOutHookLocation(hookPort.LoosePolyline, edge.SourcePort, edge.EdgeGeometry));
                }
            }
        }

        Point FigureOutHookLocation(Polyline poly, Port otherEdgeEndPort, EdgeGeometry edgeGeom) {
            var clusterPort = otherEdgeEndPort as ClusterBoundaryPort;
            if (clusterPort == null) {
                return FigureOutHookLocationForSimpleOtherPort(poly, otherEdgeEndPort, edgeGeom);
            }
            return FigureOutHookLocationForClusterOtherPort(poly, clusterPort, edgeGeom);
        }

        Point FigureOutHookLocationForClusterOtherPort(Polyline poly, ClusterBoundaryPort otherEdgeEndPort, EdgeGeometry edgeGeom) {
            var shapes = shortestPathRouter.MakeTransparentShapesOfEdgeGeometry(edgeGeom);
            //SplineRouter.ShowVisGraph(this.VisibilityGraph, this.LooseHierarchy.GetAllLeaves(),
            //    shapes.Select(sh => sh.BoundaryCurve), new[] { new LineSegment(edgeGeom.SourcePort.Location, edgeGeom.TargetPort.Location) });
            var s = new MultipleSourceMultipleTargetsShortestPathOnVisibilityGraph(otherEdgeEndPort.LoosePolyline.Select(p => VisibilityGraph.FindVertex(p)),             
                poly.Select(p => VisibilityGraph.FindVertex(p)), VisibilityGraph);
            var path = s.GetPath();
            foreach (var sh in shapes)
                sh.IsTransparent = false;
            return path.Last().Point;
        }

        private Point FigureOutHookLocationForSimpleOtherPort(Polyline poly, Port otherEdgeEndPort, EdgeGeometry edgeGeom) {
            Point otherEdgeEnd = otherEdgeEndPort.Location;
            var shapes = shortestPathRouter.MakeTransparentShapesOfEdgeGeometry(edgeGeom);
            //SplineRouter.ShowVisGraph(this.VisibilityGraph, this.LooseHierarchy.GetAllLeaves(),
            //    shapes.Select(sh => sh.BoundaryCurve), new[] { new LineSegment(edgeGeom.SourcePort.Location, edgeGeom.TargetPort.Location) });
            var s = new SingleSourceMultipleTargetsShortestPathOnVisibilityGraph(
                VisibilityGraph.FindVertex(otherEdgeEnd),
                poly.PolylinePoints.Select(p => VisibilityGraph.FindVertex(p.Point)), VisibilityGraph);
            var path = s.GetPath();
            foreach (var sh in shapes)
                sh.IsTransparent = false;
            return path.Last().Point;
        }

        Dictionary<EdgeGeometry, Set<Polyline>> EdgeLooseEnterable { get; set; }
        Dictionary<EdgeGeometry, Set<Polyline>> EdgeTightEnterable { get; set; }

        bool RoutePathsWithSteinerDijkstra() {
            shortestPathRouter.VisibilityGraph = VisibilityGraph;
            shortestPathRouter.BundlingSettings = bundlingSettings;
            shortestPathRouter.EdgeGeometries = regularEdges.Select(e => e.EdgeGeometry).ToArray();
            shortestPathRouter.ObstacleHierarchy = LooseHierarchy;
            shortestPathRouter.RouteEdges();

            //find appropriate edge separation
            if (shortestPathRouter.CdtProperty != null)
                if (!AnalyzeEdgeSeparation())
                    return false;
            return true;
        }

        /// <summary>
        /// calculates maximum possible edge separation for the computed routing
        ///   if it is greater than bundlingSettings.EdgeSeparation, then proceed 
        ///   if it is smaller, then either
        ///     stop edge bundling, or
        ///     reduce edge separation, or
        ///     move obstacles to get more free space
        /// </summary>
        bool AnalyzeEdgeSeparation() {
            Dictionary<EdgeGeometry, Set<CdtEdge>> crossedCdtEdges = new Dictionary<EdgeGeometry, Set<CdtEdge>>();
            shortestPathRouter.FillCrossedCdtEdges(crossedCdtEdges);
            Dictionary<CdtEdge, Set<EdgeGeometry>> pathsOnCdtEdge = GetPathsOnCdtEdge(crossedCdtEdges);
            double es = CalculateMaxAllowedEdgeSeparation(pathsOnCdtEdge);
           // TimeMeasurer.DebugOutput("opt es: " + es);

            if (es >= bundlingSettings.EdgeSeparation)
                return true; //we can even enlarge it here

            if (es <= 0.02) {
                TimeMeasurer.DebugOutput("edge bundling can't be executed: not enough free space around obstacles");
                foreach (var e in regularEdges)
                    e.Curve = null;

                return false;
            }
            // reducing edge separation
           // TimeMeasurer.DebugOutput("reducing edge separation to " + es);
            bundlingSettings.EdgeSeparation = es;
            shortestPathRouter.RouteEdges();
            return true;
        }

        Dictionary<CdtEdge, Set<EdgeGeometry>> GetPathsOnCdtEdge(Dictionary<EdgeGeometry, Set<CdtEdge>> crossedEdges) {
            Dictionary<CdtEdge, Set<EdgeGeometry>> res = new Dictionary<CdtEdge, Set<EdgeGeometry>>();
            foreach (var edge in crossedEdges.Keys) {
                foreach (var cdtEdge in crossedEdges[edge])
                    CollectionUtilities.AddToMap(res, cdtEdge, edge);
            }

            return res;
        }

        double CalculateMaxAllowedEdgeSeparation(Dictionary<CdtEdge, Set<EdgeGeometry>> pathsOnCdtEdge) {
            double l = 0.01;
            double r = 10;// ?TODO: change to bundlingSettings.EdgeSeparation;
            if (EdgeSeparationIsOk(pathsOnCdtEdge, r))
                return r;
            while (Math.Abs(r - l) > 0.01) {
                double cen = (l + r) / 2;
                if (EdgeSeparationIsOk(pathsOnCdtEdge, cen))
                    l = cen;
                else
                    r = cen;
            }
            return l;
        }

        bool EdgeSeparationIsOk(Dictionary<CdtEdge, Set<EdgeGeometry>> pathsOnCdtEdge, double separation) {
            //total number of cdt edges
            double total = pathsOnCdtEdge.Count;
            if (total == 0)
                return true;

            //number of edges with requiredWidth <= availableWidth
            double ok = 0;
            foreach (var edge in pathsOnCdtEdge.Keys)
                if (EdgeSeparationIsOk(edge, pathsOnCdtEdge[edge], separation))
                    ok++;

            //at least 95% of edges should be okay
            return (ok / total > bundlingSettings.MinimalRatioOfGoodCdtEdges);
        }

        bool EdgeSeparationIsOk(CdtEdge edge, Set<EdgeGeometry> paths, double separation) {
            double requiredWidth = paths.Select(v => v.LineWidth).Sum() + (paths.Count - 1) * separation;
            double availableWidth = edge.Capacity;

            return (requiredWidth <= availableWidth);
        }

        void RouteSelfEdges() {
            foreach (var edge in geometryGraph.Edges.Where(e => e.Source == e.Target)) {
                SmoothedPolyline sp;
                edge.Curve = Edge.RouteSelfEdge(edge.Source.BoundaryCurve, LoosePadding * 2, out sp);
            }
        }

        void FixArrowheads() {
            foreach (var edge in geometryGraph.Edges)
                Arrowheads.TrimSplineAndCalculateArrowheads(edge.EdgeGeometry,
                                                                 edge.Source.BoundaryCurve,
                                                                 edge.Target.BoundaryCurve,
                                                                 edge.Curve, false);
        }
    }
}