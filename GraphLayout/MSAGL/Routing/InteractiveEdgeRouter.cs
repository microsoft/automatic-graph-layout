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
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;
using Microsoft.Msagl.Routing.Visibility;
#if TEST_MSAGL
using Microsoft.Msagl.DebugHelpers;
#endif

namespace Microsoft.Msagl.Routing {
    /// <summary>
    /// the router between nodes
    /// </summary>
    public class InteractiveEdgeRouter : AlgorithmBase {
       
        /// <summary>
        /// the obstacles for routing
        /// </summary>
        public IEnumerable<ICurve> Obstacles { get; private set; }

        /// <summary>
        /// the minimum angle between a node boundary curve and and an edge 
        /// curve at the place where the edge curve intersects the node boundary
        /// </summary>
        double EnteringAngleBound { get; set; }

        Polyline _sourceTightPolyline;

        Polyline SourceTightPolyline {
            get { return _sourceTightPolyline; }
            set { _sourceTightPolyline = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        Polyline SourceLoosePolyline { get; set; }

        Polyline targetTightPolyline;

        /// <summary>
        /// 
        /// </summary>
        Polyline TargetTightPolyline {
            get { return targetTightPolyline; }
            set { targetTightPolyline = value; }
        }

        Polyline targetLoosePolyline;

        Polyline TargetLoosePolyline {
            get { return targetLoosePolyline; }
            set { targetLoosePolyline = value; }
        }

        //RectangleNode<Polyline, Point> RootOfTightHierarchy {
        //    get { return this.obstacleCalculator.RootOfTightHierararchy; }
        //}

        Rectangle activeRectangle = Rectangle.CreateAnEmptyBox();

        VisibilityGraph visibilityGraph;

        internal VisibilityGraph VisibilityGraph {
            get { return visibilityGraph; }
            set { visibilityGraph = value; }
        }

        //List<Polyline> activeTightPolylines = new List<Polyline>();
        List<Polygon> activePolygons = new List<Polygon>();
        readonly Set<Polyline> alreadyAddedOrExcludedPolylines = new Set<Polyline>();

        //    Dictionary<Point, Polyline> pointsToObstacles = new Dicitonary<Point, Polyline>();

        Port sourcePort;

        /// <summary>
        /// the port of the edge start
        /// </summary>
        public Port SourcePort {
            get { return sourcePort; }
            private set {
                sourcePort = value;
                if (sourcePort != null) {
                    SourceTightPolyline = GetFirstHitPolyline(sourcePort.Location,
                                                              ObstacleCalculator.RootOfTightHierarchy);
                    if (sourcePort is FloatingPort) {
                        alreadyAddedOrExcludedPolylines.Insert(SourceLoosePolyline);
                        //we need to exclude the loose polyline around the source port from the tangent visibily graph
                        StartPointOfEdgeRouting = SourcePort.Location;
                    }
                    else {
                        var bp = (CurvePort) sourcePort;
                        StartPointOfEdgeRouting = TakeBoundaryPortOutsideOfItsLoosePolyline(bp.Curve, bp.Parameter,
                                                                                            SourceLoosePolyline);
                    }
                }
            }
        }

        Port targetPort;

        /// <summary>
        /// the port of the edge end
        /// </summary>
        Port TargetPort {
            get { return targetPort; }
            set { targetPort = value; }
        }


        /// <summary>
        /// the curve should not come closer than Padding to the nodes
        /// </summary>
        public double TightPadding { get; set; }

        double loosePadding;

        /// <summary>
        /// we further pad each node but not more than LoosePadding.
        /// </summary>
        public double LoosePadding {
            get {
                return loosePadding;
            }
            internal set {
                loosePadding = value;
                if(ObstacleCalculator!=null)
                    ObstacleCalculator.LoosePadding = value;
            }
        }


        VisibilityVertex _sourceVisibilityVertex;

        VisibilityVertex SourceVisibilityVertex {
            get { return _sourceVisibilityVertex; } //            set { sourceVisibilityVertex = value; }
        }

        VisibilityVertex targetVisibilityVertex;

        VisibilityVertex TargetVisibilityVertex {
            get { return targetVisibilityVertex; } //            set { targetVisibilityVertex = value; }
        }


        Polyline _polyline;


        /// <summary>
        /// 
        /// </summary>
        double OffsetForPolylineRelaxing { get; set; }

        /// <summary>
        /// Set up the router and calculate the set of obstacles over which to route.
        /// </summary>
        /// <param name="obstacles">the obstacles for routing</param>
        /// <param name="padding">obstacles are inflated by this much to find an inner boundary within which edges cannot enter</param>
        /// <param name="loosePadding">
        /// obstacles are inflated again by this much to find initial 
        /// routing but then spline smoothing is allowed to come inside this outer boundary.
        /// Loose padding of 0 will give sharp corners (no spline smoothing)</param>
        /// <param name="coneSpannerAngle">if this is greater than 0 then a "cone spanner" visibility graph with be
        /// generated using cones of the specified angle to search for visibility edges.  The cone spanner graph is
        /// a sparser graph than the complete visibility graph and is hence much faster to generate and route over
        /// but may not give strictly shortest path routes</param>
        public InteractiveEdgeRouter(IEnumerable<ICurve> obstacles, double padding, double loosePadding,
                                     double coneSpannerAngle):this(obstacles, padding, loosePadding, coneSpannerAngle, false) {}

        /// <summary>
        /// The expected number of progress steps this algorithm will take.
        /// </summary>
        public int ExpectedProgressSteps { get; private set; }

        bool targetIsInsideOfSourceTightPolyline;
        bool sourceIsInsideOfTargetTightPolyline;
        internal bool UseEdgeLengthMultiplier;
        /// <summary>
        /// if set to true the algorithm will try to shortcut a shortest polyline inner points
        /// </summary>
        internal bool UseInnerPolylingShortcutting=true;

        /// <summary>
        /// if set to true the algorithm will try to shortcut a shortest polyline start and end
        /// </summary>
        internal bool UsePolylineEndShortcutting=true;

        internal bool AllowedShootingStraightLines = true;
        Dictionary<Corner, Tuple<double,double>> cornerTable;
        bool cacheCorners;


        /// <summary>
        /// An empty constructor for calling it from inside of MSAGL
        /// </summary>        
        internal InteractiveEdgeRouter() {
            ObstacleCalculator = new InteractiveObstacleCalculator(Obstacles, TightPadding, LoosePadding, false);
        }

        Point StartPointOfEdgeRouting { get; set; }

        void ExtendVisibilityGraphToLocation(Point location) {
            if (VisibilityGraph == null)
                VisibilityGraph = new VisibilityGraph();
            List<Polygon> addedPolygons = null;
            if (!activeRectangle.Contains(location)) {
                if (activeRectangle.IsEmpty)
                    activeRectangle = new Rectangle(SourcePort.Location, location);
                else
                    activeRectangle.Add(location);
                addedPolygons = GetAddedPolygonesAndMaybeExtendActiveRectangle();
                foreach (Polygon polygon in addedPolygons)
                    VisibilityGraph.AddHole(polygon.Polyline);
            }
            if (addedPolygons == null || addedPolygons.Count == 0) {
                if (targetVisibilityVertex != null)
                    VisibilityGraph.RemoveVertex(targetVisibilityVertex);
                CalculateEdgeTargetVisibilityGraph(location);
            }
            else {
                RemovePointVisibilityGraphs();
                var visibilityGraphGenerator = new InteractiveTangentVisibilityGraphCalculator(addedPolygons,
                                                                                               activePolygons,
                                                                                               VisibilityGraph);
                visibilityGraphGenerator.Run();
                activePolygons.AddRange(addedPolygons);
                CalculateEdgeTargetVisibilityGraph(location);
                CalculateSourcePortVisibilityGraph();
            }
        }


        void RemovePointVisibilityGraphs() {
            if (targetVisibilityVertex != null)
                VisibilityGraph.RemoveVertex(targetVisibilityVertex);
            if (_sourceVisibilityVertex != null)
                VisibilityGraph.RemoveVertex(_sourceVisibilityVertex);
        }

        void CalculateEdgeTargetVisibilityGraph(Point location) {
            PointVisibilityCalculator.CalculatePointVisibilityGraph(GetActivePolylines(), VisibilityGraph, location,
                                                                    VisibilityKind.Tangent, out targetVisibilityVertex);
        }

        void CalculateSourcePortVisibilityGraph() {
            PointVisibilityCalculator.CalculatePointVisibilityGraph(GetActivePolylines(), VisibilityGraph,
                                                                    StartPointOfEdgeRouting, VisibilityKind.Tangent,
                                                                    out _sourceVisibilityVertex);
            Debug.Assert(_sourceVisibilityVertex != null);
        }


        Point TakeBoundaryPortOutsideOfItsLoosePolyline(ICurve nodeBoundary, double parameter, Polyline loosePolyline) {
            Point location = nodeBoundary[parameter];
            Point tangent =
                (nodeBoundary.LeftDerivative(parameter).Normalize() +
                 nodeBoundary.RightDerivative(parameter).Normalize()).Normalize();
            if (Point.GetTriangleOrientation(PointInsideOfConvexCurve(nodeBoundary), location, location + tangent) ==
                TriangleOrientation.Counterclockwise)
                tangent = -tangent;

            tangent = tangent.Rotate(Math.PI/2);

            double len = loosePolyline.BoundingBox.Diagonal;
            var ls = new LineSegment(location, location + len*tangent);
            Point p = Curve.GetAllIntersections(ls, loosePolyline, false)[0].IntersectionPoint;

            Point del = tangent*(p - location).Length*0.5;
            //Point del = tangent * this.OffsetForPolylineRelaxing * 2;


            while (true) {
                ls = new LineSegment(location, p + del);
                bool foundIntersectionsOutsideOfSource = false;
                foreach (IntersectionInfo ii in
                    IntersectionsOfLineAndRectangleNodeOverPolyline(ls, ObstacleCalculator.RootOfLooseHierarchy))
                    if (ii.Segment1 != loosePolyline) {
                        del /= 1.5;
                        foundIntersectionsOutsideOfSource = true;
                        break;
                    }
                if (!foundIntersectionsOutsideOfSource)
                    break;
            }

            return ls.End;
        }

        static Point PointInsideOfConvexCurve(ICurve nodeBoundary) {
            return (nodeBoundary[0] + nodeBoundary[1.5])/2; //a hack !!!!!!!!!!!!!!!!!!!!!!
        }

        //Point TakeSourcePortOutsideOfLoosePolyline() {
        //    CurvePort bp = SourcePort as CurvePort;
        //    ICurve nodeBoundary = bp.Node.BoundaryCurve;
        //    Point location = bp.Location;
        //    Point tangent = (nodeBoundary.LeftDerivative(bp.Parameter).Normalize() + nodeBoundary.RightDerivative(bp.Parameter).Normalize()).Normalize();
        //    if (Point.GetTriangleOrientation(bp.Node.Center, location, location + tangent) == TriangleOrientation.Counterclockwise)
        //        tangent = -tangent;

        //    tangent = tangent.Rotate(Math.PI / 2);

        //    double len = this.sourceLoosePolyline.BoundingBox.Diagonal;
        //    Point portLocation = bp.Location;
        //    LineSegment ls = new LineSegment(portLocation, portLocation + len * tangent);
        //    Point p = Curve.GetAllIntersections(ls, this.SourceLoosePolyline, false)[0].IntersectionPoint;
        //    Point del = tangent * this.OffsetForPolylineRelaxing * 2;

        //    while (true) {
        //        ls = new LineSegment(portLocation, p + del);
        //        bool foundIntersectionsOutsideOfSource = false;
        //        foreach (IntersectionInfo ii in IntersectionsOfLineAndRectangleNodeOverPolyline(ls, this.obstacleCalculator.RootOfLooseHierarchy))
        //            if (ii.Segment1 != this.SourceLoosePolyline) {
        //                del /= 1.5;
        //                foundIntersectionsOutsideOfSource = true;
        //                break;
        //            }
        //        if (!foundIntersectionsOutsideOfSource)
        //            break;
        //    }

        //    return ls.End;
        //}

        IEnumerable<Polyline> GetActivePolylines() {
            foreach (Polygon polygon in activePolygons)
                yield return polygon.Polyline;
        }

        List<Polygon> GetAddedPolygonesAndMaybeExtendActiveRectangle() {
            Rectangle rect = activeRectangle;
            var addedPolygones = new List<Polygon>();
            bool added;
            do {
                added = false;
                foreach (Polyline loosePoly in
                    ObstacleCalculator.RootOfLooseHierarchy.GetNodeItemsIntersectingRectangle(activeRectangle)) {
                    if (!alreadyAddedOrExcludedPolylines.Contains(loosePoly)) {
                        rect.Add(loosePoly.BoundingBox);
                        addedPolygones.Add(new Polygon(loosePoly));
                        alreadyAddedOrExcludedPolylines.Insert(loosePoly);
                        //we register the loose polyline in the set to not add it twice
                        added = true;
                    }
                }
                if (added)
                    activeRectangle = rect;
            } while (added);
            return addedPolygones;
        }

        #region commented out code

        // List<Polyline> GetActivePolylines(Rectangle rectangleOfVisibilityGraph) {
        //    return this.activeTightPolylines;
        //}


        //bool LineIntersectsTightObstacles(Point point, Point x) {
        //    return LineIntersectsTightObstacles(new LineSegment(point, x));    
        //}

        #endregion

#if TEST_MSAGL
        // void ShowPolylineAndObstaclesWithGraph() {
        //    List<ICurve> ls = new List<ICurve>();
        //    foreach (Polyline poly in this.obstacleCalculator.TightObstacles)
        //        ls.Add(poly);
        //    foreach (Polyline poly in this.obstacleCalculator.LooseObstacles)
        //        ls.Add(poly);
        //    AddVisibilityGraph(ls);
        //    ls.Add(Polyline);
        //    SugiyamaLayoutSettings.Show(ls.ToArray());
        //}
#endif
        //pull the polyline out from the corners
        void RelaxPolyline() {
            RelaxedPolylinePoint relaxedPolylinePoint = CreateRelaxedPolylinePoints(_polyline);
            for (relaxedPolylinePoint = relaxedPolylinePoint.Next;
                 relaxedPolylinePoint.Next != null;
                 relaxedPolylinePoint = relaxedPolylinePoint.Next)
                RelaxPolylinePoint(relaxedPolylinePoint);
        }

        static RelaxedPolylinePoint CreateRelaxedPolylinePoints(Polyline polyline) {
            PolylinePoint p = polyline.StartPoint;
            var ret = new RelaxedPolylinePoint(p, p.Point);
            RelaxedPolylinePoint currentRelaxed = ret;
            while (p.Next != null) {
                p = p.Next;
                var r = new RelaxedPolylinePoint(p, p.Point) {Prev = currentRelaxed};
                currentRelaxed.Next = r;
                currentRelaxed = r;
            }
            return ret;
        }


        void RelaxPolylinePoint(RelaxedPolylinePoint relaxedPoint) {
            if (relaxedPoint.PolylinePoint.Prev.Prev == null && SourcePort is CurvePort &&
                relaxedPoint.PolylinePoint.Polyline != SourceLoosePolyline)
                return;
            if (relaxedPoint.PolylinePoint.Next.Next == null && TargetPort is CurvePort &&
                relaxedPoint.PolylinePoint.Polyline != TargetLoosePolyline)
                return;
            for (double d = OffsetForPolylineRelaxing;
                 d > ApproximateComparer.DistanceEpsilon && !RelaxWithGivenOffset(d, relaxedPoint);
                 d /= 2) {
            }
        }

        bool RelaxWithGivenOffset(double offset, RelaxedPolylinePoint relaxedPoint) {
            Debug.Assert(offset > ApproximateComparer.DistanceEpsilon); //otherwise we are cycling infinitely here
            SetRelaxedPointLocation(offset, relaxedPoint);

            if (StickingSegmentDoesNotIntersectTightObstacles(relaxedPoint)) {
                return true;
            }
            PullCloserRelaxedPoint(relaxedPoint.Prev);
            return false;
        }

        static void PullCloserRelaxedPoint(RelaxedPolylinePoint relaxedPolylinePoint) {
            relaxedPolylinePoint.PolylinePoint.Point = 0.2*relaxedPolylinePoint.OriginalPosition +
                                                       0.8*relaxedPolylinePoint.PolylinePoint.Point;
        }

        bool StickingSegmentDoesNotIntersectTightObstacles(RelaxedPolylinePoint relaxedPoint) {
            return
                !PolylineSegmentIntersectsTightHierarchy(relaxedPoint.PolylinePoint.Point,
                                                         relaxedPoint.Prev.PolylinePoint.Point) &&
                (relaxedPoint.Next == null ||
                 !PolylineSegmentIntersectsTightHierarchy(relaxedPoint.PolylinePoint.Point,
                                                          relaxedPoint.Next.PolylinePoint.Point));
        }

        bool PolylineSegmentIntersectsTightHierarchy(Point a, Point b) {
            return PolylineIntersectsPolyRectangleNodeOfTightHierarchy(a, b, ObstacleCalculator.RootOfTightHierarchy);
        }

        bool PolylineIntersectsPolyRectangleNodeOfTightHierarchy(Point a, Point b, RectangleNode<Polyline, Point> rect) {
            return PolylineIntersectsPolyRectangleNodeOfTightHierarchy(new LineSegment(a, b), rect);
        }

        bool PolylineIntersectsPolyRectangleNodeOfTightHierarchy(LineSegment ls, RectangleNode<Polyline, Point> rect) {
            if (!ls.BoundingBox.Intersects((Rectangle)rect.Rectangle))
                return false;
            if (rect.UserData != null) {
                foreach (IntersectionInfo ii in Curve.GetAllIntersections(ls, rect.UserData, false)) {
                    if (ii.Segment1 != SourceTightPolyline && ii.Segment1 != TargetTightPolyline)
                        return true;
                    if (ii.Segment1 == SourceTightPolyline && SourcePort is CurvePort)
                        return true;
                    if (ii.Segment1 == TargetTightPolyline && TargetPort is CurvePort)
                        return true;
                }
                return false;
            }
            return PolylineIntersectsPolyRectangleNodeOfTightHierarchy(ls, rect.Left) ||
                   PolylineIntersectsPolyRectangleNodeOfTightHierarchy(ls, rect.Right);
        }

        internal static List<IntersectionInfo> IntersectionsOfLineAndRectangleNodeOverPolyline(LineSegment ls,
                                                                                               RectangleNode<Polyline, Point>
                                                                                                   rectNode) {
            var ret = new List<IntersectionInfo>();
            IntersectionsOfLineAndRectangleNodeOverPolyline(ls, rectNode, ret);
            return ret;
        }

        static void IntersectionsOfLineAndRectangleNodeOverPolyline(LineSegment ls, RectangleNode<Polyline, Point> rectNode,
                                                                    List<IntersectionInfo> listOfIntersections) {
            if (rectNode == null) {
                return;
            }

            if (!ls.BoundingBox.Intersects((Rectangle)rectNode.Rectangle)) {
                return;
            }

            if (rectNode.UserData != null) {
                listOfIntersections.AddRange(Curve.GetAllIntersections(ls, rectNode.UserData, true));
                return;
            }

            IntersectionsOfLineAndRectangleNodeOverPolyline(ls, rectNode.Left, listOfIntersections);
            IntersectionsOfLineAndRectangleNodeOverPolyline(ls, rectNode.Right, listOfIntersections);
        }

        bool LineCanBeAcceptedForRouting(LineSegment ls) {
            bool sourceIsFloating = SourcePort is FloatingPort;
            bool targetIsFloating = TargetPort is FloatingPort;

            if (!sourceIsFloating && !targetIsInsideOfSourceTightPolyline)
                if (!InsideOfTheAllowedConeOfBoundaryPort(ls.End, SourcePort as CurvePort))
                    return false;
            if (!targetIsFloating && TargetPort != null && !sourceIsInsideOfTargetTightPolyline)
                if (!InsideOfTheAllowedConeOfBoundaryPort(ls.Start, TargetPort as CurvePort))
                    return false;
            List<IntersectionInfo> xx = IntersectionsOfLineAndRectangleNodeOverPolyline(ls,
                                                                                        ObstacleCalculator.
                                                                                            RootOfTightHierarchy);
            foreach (IntersectionInfo ii in xx) {
                if (ii.Segment1 == SourceTightPolyline)
                    continue;
                if (ii.Segment1 == targetTightPolyline)
                    continue;

                return false;
            }
            return true;
        }


        bool InsideOfTheAllowedConeOfBoundaryPort(Point pointToTest, CurvePort port) {
            ICurve boundaryCurve = port.Curve;
            bool curveIsClockwise = InteractiveObstacleCalculator.CurveIsClockwise(boundaryCurve,
                                                                                   PointInsideOfConvexCurve(
                                                                                       boundaryCurve));
            Point portLocation = port.Location;
            Point pointOnTheRightConeSide = GetPointOnTheRightBoundaryPortConeSide(portLocation, boundaryCurve,
                                                                                   curveIsClockwise, port.Parameter);
            Point pointOnTheLeftConeSide = GetPointOnTheLeftBoundaryPortConeSide(portLocation, boundaryCurve,
                                                                                 curveIsClockwise, port.Parameter);
            return Point.GetTriangleOrientation(portLocation, pointOnTheRightConeSide, pointToTest) !=
                   TriangleOrientation.Clockwise &&
                   Point.GetTriangleOrientation(portLocation, pointToTest, pointOnTheLeftConeSide) !=
                   TriangleOrientation.Clockwise;
        }

        Point GetPointOnTheRightBoundaryPortConeSide(Point portLocation, ICurve boundaryCurve, bool curveIsClockwise,
                                                     double portParam) {
            Point tan = curveIsClockwise
                            ? boundaryCurve.RightDerivative(portParam)
                            : -boundaryCurve.LeftDerivative(portParam);

            return portLocation + tan.Rotate(EnteringAngleBound);
        }

        Point GetPointOnTheLeftBoundaryPortConeSide(Point portLocation, ICurve boundaryCurve, bool curveIsClockwise,
                                                    double portParam) {
            Point tan = curveIsClockwise
                            ? -boundaryCurve.LeftDerivative(portParam)
                            : boundaryCurve.RightDerivative(portParam);
            return portLocation + tan.Rotate(-EnteringAngleBound);
        }


        static void SetRelaxedPointLocation(double offset, RelaxedPolylinePoint relaxedPoint) {
            bool leftTurn =
                Point.GetTriangleOrientation(relaxedPoint.Next.OriginalPosition, relaxedPoint.OriginalPosition,
                                             relaxedPoint.Prev.OriginalPosition) == TriangleOrientation.Counterclockwise;
            Point v =
                ((relaxedPoint.Next.OriginalPosition - relaxedPoint.Prev.OriginalPosition).Normalize()*offset).Rotate(
                    Math.PI/2);

            if (!leftTurn)
                v = -v;
            relaxedPoint.PolylinePoint.Point = relaxedPoint.OriginalPosition + v;
        }

#if TEST_MSAGL
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
// ReSharper disable UnusedMember.Local
        internal void ShowPolylineAndObstacles(params ICurve[] curves)
        {
// ReSharper restore UnusedMember.Local
            IEnumerable<DebugCurve> ls = GetDebugCurves(curves);
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(ls);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        IEnumerable<DebugCurve> GetDebugCurves(params ICurve[] curves)
        {
            var ls = CreateListWithObstaclesAndPolyline(curves);
            //ls.AddRange(this.VisibilityGraph.Edges.Select(e => new DebugCurve(100,0.1, e is TollFreeVisibilityEdge?"red":"green", new LineSegment(e.SourcePoint, e.TargetPoint))));
            if (_sourceVisibilityVertex != null)
                ls.Add(new DebugCurve("red", CurveFactory.CreateDiamond(4, 4, _sourceVisibilityVertex.Point)));
            if (targetVisibilityVertex != null)
                ls.Add(new DebugCurve("purple", new Ellipse(4, 4, targetVisibilityVertex.Point)));
            var anywerePort=targetPort as HookUpAnywhereFromInsidePort;
            if (anywerePort != null)
                ls.Add(new DebugCurve("purple", anywerePort.LoosePolyline));
            return ls;
        }


        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        List<DebugCurve> CreateListWithObstaclesAndPolyline(params ICurve[] curves)
        {
            var ls = new List<DebugCurve>(ObstacleCalculator.RootOfLooseHierarchy.GetAllLeaves().Select(e => new DebugCurve(100,0.01, "green", e)));
            ls.AddRange(curves.Select(c=>new DebugCurve(100,0.01,"red", c)));
            ls.AddRange(ObstacleCalculator.RootOfTightHierarchy.GetAllLeaves().Select(e => new DebugCurve(100, 0.01, "blue", e)));

            // ls.AddRange(visibilityGraph.Edges.Select(e => (ICurve) new LineSegment(e.SourcePoint, e.TargetPoint)));
            if (_polyline != null)
                ls.Add(new DebugCurve(100, 0.03, "blue",_polyline));
            return ls;
        }
#endif

        /// <summary>
        /// smoothing the corners of the polyline
        /// </summary>
        /// <param name="edgePolyline"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public void SmoothCorners(SmoothedPolyline edgePolyline) {
            ValidateArg.IsNotNull(edgePolyline, "edgePolyline");

            Site a = edgePolyline.HeadSite; //the corner start
            Site b; //the corner origin
            Site c; //the corner other end
            while (Curve.FindCorner(a, out b, out c))
                a = SmoothOneCorner(a, c, b);
        }

        Site SmoothOneCorner(Site a, Site c, Site b) {
            if (CacheCorners) {
                double p, n;
                if (FindCachedCorner(a, b, c, out p, out n)) {
                    b.PreviousBezierSegmentFitCoefficient = p;
                    b.NextBezierSegmentFitCoefficient = n;
                    return b;
                }
            }
            const double mult = 1.5;
            const double kMin = 0.01;
            
            double k = 0.5;
            CubicBezierSegment seg;
            double u, v;
            if (a.Previous == null) {
                //this will allow to the segment to start from site "a"
                u = 2;
                v = 1;
            } else if (c.Next == null) {
                u = 1;
                v = 2; //this will allow to the segment to end at site "c"
            } else
                u = v = 1;

            do {
                seg = Curve.CreateBezierSeg(k*u, k*v, a, b, c);
                b.PreviousBezierSegmentFitCoefficient = k*u;
                b.NextBezierSegmentFitCoefficient = k*v;
                k /= mult;
            } while (ObstacleCalculator.ObstaclesIntersectICurve(seg) && k > kMin);

            k *= mult; //that was the last k
            if (k < 0.5 && k > kMin) {
                //one time try a smoother seg
                k = 0.5*(k + k*mult);
                seg = Curve.CreateBezierSeg(k*u, k*v, a, b, c);
                if (!ObstacleCalculator.ObstaclesIntersectICurve(seg)) {
                    b.PreviousBezierSegmentFitCoefficient = k*u;
                    b.NextBezierSegmentFitCoefficient = k*v;
                }
            }
            if (CacheCorners)
                CacheCorner(a, b, c);
            return b;
        }

        internal int foundCachedCorners;
        bool FindCachedCorner(Site a, Site b, Site c, out double prev, out double next) {
            Corner corner=new Corner(a.Point,b.Point,c.Point);
            Tuple<double, double> prevNext;
            if (cornerTable.TryGetValue(corner, out prevNext)) {
                if (a.Point == corner.a) {
                    prev = prevNext.Item1;
                    next = prevNext.Item2;
                }
                else {
                    prev = prevNext.Item2;
                    next = prevNext.Item1;
                }
                foundCachedCorners++;
                return true;
            }
            prev = next = 0;
            return false;
        }

        void CacheCorner(Site a, Site b, Site c) {
            cornerTable[new Corner(a.Point,b.Point,c.Point)]=new Tuple<double, double>(b.PreviousBezierSegmentFitCoefficient,b.NextBezierSegmentFitCoefficient);
        }

        /// <summary>
        /// is set to true will cache three points defining the corner 
        /// to avoid obstacle avoidance calculation
        /// </summary>
        public bool CacheCorners {
            get { return cacheCorners; }
            set {
                cacheCorners = value;
                if (cacheCorners)
                    cornerTable = new Dictionary<Corner, Tuple<double, double>>();
                else {
                    if (cornerTable != null)
                        cornerTable.Clear();
                } 

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="underlyingPolyline"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public void TryToRemoveInflectionsAndCollinearSegments(SmoothedPolyline underlyingPolyline) {
            ValidateArg.IsNotNull(underlyingPolyline, "underlyingPolyline");
            bool progress = true;
            while (progress) {
                progress = false;
                for (Site s = underlyingPolyline.HeadSite; s != null && s.Next != null; s = s.Next) {
                    if (s.Turn*s.Next.Turn < 0)
                        progress = TryToRemoveInflectionEdge(ref s) || progress;
                }
            }
        }

        bool TryToRemoveInflectionEdge(ref Site s) {
            if (!ObstacleCalculator.ObstaclesIntersectLine(s.Previous.Point, s.Next.Point)) {
                Site a = s.Previous; //forget s
                Site b = s.Next;
                a.Next = b;
                b.Previous = a;
                s = a;
                return true;
            }
            if (!ObstacleCalculator.ObstaclesIntersectLine(s.Previous.Point, s.Next.Next.Point)) {
                //forget about s and s.Next
                Site a = s.Previous;
                Site b = s.Next.Next;
                a.Next = b;
                b.Previous = a;
                s = a;
                return true;
            }
            if (!ObstacleCalculator.ObstaclesIntersectLine(s.Point, s.Next.Next.Point)) {
                //forget about s.Next
                Site b = s.Next.Next;
                s.Next = b;
                b.Previous = s;
                return true;
            }

            return false;
        }

        //internal Point TargetPoint {
        //    get {
        //        CurvePort tp = this.TargetPort as CurvePort;
        //        if (tp != null)
        //            return this.Target.BoundaryCurve[tp.Parameter];
        //        else
        //            return (this.TargetPort as FloatingPort).Location;
        //    }
        //}

        //internal Point SourcePoint {
        //    get {
        //        CurvePort sp = this.SourcePort as CurvePort;
        //        if (sp != null)
        //            return this.Source.BoundaryCurve[sp.Parameter];
        //        else
        //            return (this.SourcePort as FloatingPort).Location;
        //    }
        //}


        Polyline GetShortestPolyline(VisibilityVertex sourceVisVertex, VisibilityVertex _targetVisVertex) {
            CleanTheGraphForShortestPath();
            var pathCalc = new SingleSourceSingleTargetShortestPathOnVisibilityGraph(this.visibilityGraph, sourceVisVertex, _targetVisVertex);

            IEnumerable<VisibilityVertex> path = pathCalc.GetPath(UseEdgeLengthMultiplier);
            if (path == null) {
                //ShowIsPassable(_sourceVisibilityVertex, _targetVisVertex);
                return null;
            }


            Debug.Assert(path.First() == sourceVisVertex && path.Last() == _targetVisVertex);
            var ret = new Polyline();
            foreach (VisibilityVertex v in path)
                ret.AddPoint(v.Point);
            return RemoveCollinearVertices(ret);
        }

#if TEST_MSAGL
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void ShowIsPassable(VisibilityVertex sourceVisVertex, VisibilityVertex targetVisVertex)
        {
            var dd = new List<DebugCurve>(
                visibilityGraph.Edges.Select(
                    e =>
                    new DebugCurve(100, 0.5, e.IsPassable == null || e.IsPassable() ? "green" : "red",
                                   new LineSegment(e.SourcePoint, e.TargetPoint))));
            if(sourceVisVertex!=null)
                dd.Add(new DebugCurve(CurveFactory.CreateDiamond(3, 3, sourceVisVertex.Point)));
            if(targetVisVertex!=null)
                dd.Add(new DebugCurve(CurveFactory.CreateEllipse(3, 3, targetVisVertex.Point)));
                              
            if (Obstacles != null)
                dd.AddRange(Obstacles.Select(o => new DebugCurve(o)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dd);
        }
#endif

        void CleanTheGraphForShortestPath() {
            visibilityGraph.ClearPrevEdgesTable();            
        }

        internal static Polyline RemoveCollinearVertices(Polyline ret) {
            for (PolylinePoint pp = ret.StartPoint.Next; pp.Next != null; pp = pp.Next) {
                if (Point.GetTriangleOrientation(pp.Prev.Point, pp.Point, pp.Next.Point) ==
                    TriangleOrientation.Collinear) {
                    pp.Prev.Next = pp.Next;
                    pp.Next.Prev = pp.Prev;
                }
            }
            return ret;
        }


        /// <summary>
        /// returns true if the nodes overlap or just positioned too close
        /// </summary>
        public bool OverlapsDetected {
            get { return ObstacleCalculator.OverlapsDetected; }
        }

        ///<summary>
        ///</summary>
        public double ConeSpannerAngle { get; set; }

        internal RectangleNode<Polyline, Point> TightHierarchy {
            get { return ObstacleCalculator.RootOfTightHierarchy; }
            set { ObstacleCalculator.RootOfTightHierarchy = value; }
        }

        internal RectangleNode<Polyline, Point> LooseHierarchy {
            get { return ObstacleCalculator.RootOfLooseHierarchy; }
            set { ObstacleCalculator.RootOfLooseHierarchy = value; }
        }

        internal bool UseSpanner { get; set; }


        void CalculateObstacles() {
            ObstacleCalculator = new InteractiveObstacleCalculator(Obstacles, TightPadding, LoosePadding,
                                                                   IgnoreTightPadding);                                 
            ObstacleCalculator.Calculate();
        }


        //  int count;

        /// <summary>
        ///
        /// </summary>
        /// <param name="targetLocation"></param>
        /// <returns></returns>
        public EdgeGeometry RouteEdgeToLocation(Point targetLocation) {
            TargetPort = new FloatingPort((ICurve) null, targetLocation); //otherwise route edge to a port would be called
            TargetTightPolyline = null;
            TargetLoosePolyline = null;
            var edgeGeometry = new EdgeGeometry();

            var ls = new LineSegment(SourcePort.Location, targetLocation);

            if (LineCanBeAcceptedForRouting(ls)) {
                _polyline = new Polyline();
                _polyline.AddPoint(ls.Start);
                _polyline.AddPoint(ls.End);
                edgeGeometry.SmoothedPolyline = SmoothedPolyline.FromPoints(_polyline);
                edgeGeometry.Curve = edgeGeometry.SmoothedPolyline.CreateCurve();
                return edgeGeometry;
            }

            //can we do with just two line segments?
            if (SourcePort is CurvePort) {
                ls = new LineSegment(StartPointOfEdgeRouting, targetLocation);
                if (
                    IntersectionsOfLineAndRectangleNodeOverPolyline(ls, ObstacleCalculator.RootOfTightHierarchy).Count ==
                    0) {
                    _polyline = new Polyline();
                    _polyline.AddPoint(SourcePort.Location);
                    _polyline.AddPoint(ls.Start);
                    _polyline.AddPoint(ls.End);
                    //RelaxPolyline();
                    edgeGeometry.SmoothedPolyline = SmoothedPolyline.FromPoints(_polyline);
                    edgeGeometry.Curve = edgeGeometry.SmoothedPolyline.CreateCurve();
                    return edgeGeometry;
                }
            }

            ExtendVisibilityGraphToLocation(targetLocation);

            _polyline = GetShortestPolyline(SourceVisibilityVertex, TargetVisibilityVertex);

            RelaxPolyline();
            if (SourcePort is CurvePort)
                _polyline.PrependPoint(SourcePort.Location);

            edgeGeometry.SmoothedPolyline = SmoothedPolyline.FromPoints(_polyline);
            edgeGeometry.Curve = edgeGeometry.SmoothedPolyline.CreateCurve();
            return edgeGeometry;
        }

        /// <summary>
        /// routes the edge to the port
        /// </summary>
        /// <param name="edgeTargetPort"></param>
        /// <param name="portLoosePolyline"></param>
        /// <param name="smooth"> if true will smooth the edge avoiding the obstacles, will take more time</param>
        /// <param name="smoothedPolyline"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#"), SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public ICurve RouteEdgeToPort(Port edgeTargetPort, Polyline portLoosePolyline, bool smooth, out SmoothedPolyline smoothedPolyline) {
            ValidateArg.IsNotNull(edgeTargetPort, "edgeTargetToPort");
            if (!ObstacleCalculator.IsEmpty()) {
                TargetPort = edgeTargetPort;
                TargetTightPolyline = GetFirstHitPolyline(edgeTargetPort.Location,
                                                          ObstacleCalculator.RootOfTightHierarchy);
                Debug.Assert(targetTightPolyline != null);
                var bp = edgeTargetPort as CurvePort;
                if (bp != null)
                    return RouteEdgeToBoundaryPort(portLoosePolyline, smooth, out smoothedPolyline);
                return RouteEdgeToFloatingPortOfNode(portLoosePolyline, smooth, out smoothedPolyline);
            }
            if (sourcePort != null && targetPort != null) {
                smoothedPolyline = SmoothedPolylineFromTwoPoints(sourcePort.Location, targetPort.Location);
                return new LineSegment(sourcePort.Location, targetPort.Location);
            }
            smoothedPolyline = null;
            return null;
        }

        SmoothedPolyline SmoothedPolylineFromTwoPoints(Point s, Point e) {
            _polyline = new Polyline();
            _polyline.AddPoint(s);
            _polyline.AddPoint(e);
            return SmoothedPolyline.FromPoints(_polyline);
        }

        ICurve RouteEdgeToFloatingPortOfNode(Polyline portLoosePolyline, bool smooth, out SmoothedPolyline smoothedPolyline) {
            if (sourcePort is FloatingPort)
                return RouteFromFloatingPortToFloatingPort(portLoosePolyline, smooth, out smoothedPolyline);
            return RouteFromBoundaryPortToFloatingPort(portLoosePolyline, smooth, out smoothedPolyline);
        }

        ICurve RouteFromBoundaryPortToFloatingPort(Polyline targetPortLoosePolyline, bool smooth, out SmoothedPolyline polyline) {
            Point sourcePortLocation = SourcePort.Location;
            Point targetPortLocation = targetPort.Location;
            var ls = new LineSegment(sourcePortLocation, targetPortLocation);
            if (LineCanBeAcceptedForRouting(ls)) {
                polyline = SmoothedPolylineFromTwoPoints(ls.Start, ls.End);
                return ls;
            }
            if (!targetIsInsideOfSourceTightPolyline) {
                //try a variant with two segments
                Point takenOutPoint = TakeBoundaryPortOutsideOfItsLoosePolyline(SourcePort.Curve,
                                                                                ((CurvePort) SourcePort).Parameter,
                                                                                SourceLoosePolyline);
                ls = new LineSegment(takenOutPoint, targetPortLocation);
                if (LineAvoidsTightHierarchy(ls, targetPortLoosePolyline)) {
                    polyline = SmoothedPolylineFromTwoPoints(ls.Start, ls.End);
                    return ls;
                }
            }
            //we need to route throw the visibility graph
            ExtendVisibilityGraphToLocationOfTargetFloatingPort(targetPortLoosePolyline);
            _polyline = GetShortestPolyline(SourceVisibilityVertex, TargetVisibilityVertex);
            Polyline tmp = SourceTightPolyline;
            if (!targetIsInsideOfSourceTightPolyline)
                //this is done to avoid shorcutting through the source tight polyline             
                SourceTightPolyline = null;
            TryShortcutPolyline();
            SourceTightPolyline = tmp;
            RelaxPolyline();
            _polyline.PrependPoint(sourcePortLocation);
            return SmoothCornersAndReturnCurve(smooth, out polyline);
        }

        ICurve SmoothCornersAndReturnCurve(bool smooth, out SmoothedPolyline smoothedPolyline) {
            smoothedPolyline = SmoothedPolyline.FromPoints(_polyline);
            if (smooth)
                SmoothCorners(smoothedPolyline);
            return smoothedPolyline.CreateCurve();
        }


        ICurve RouteFromFloatingPortToFloatingPort(Polyline portLoosePolyline, bool smooth, out SmoothedPolyline smoothedPolyline) {
            Point targetPortLocation = TargetPort.Location;

            var ls = new LineSegment(StartPointOfEdgeRouting, targetPortLocation);
            if ( AllowedShootingStraightLines && LineAvoidsTightHierarchy(ls, SourceTightPolyline, targetTightPolyline) ) {
                smoothedPolyline = SmoothedPolylineFromTwoPoints(ls.Start, ls.End);
                return ls;
            }
            //we need to route through the visibility graph
            ExtendVisibilityGraphToLocationOfTargetFloatingPort(portLoosePolyline);
            _polyline = GetShortestPolyline(SourceVisibilityVertex, TargetVisibilityVertex);
            if (_polyline == null) {
                smoothedPolyline = null;
                return null;
            }
            if (UseSpanner)
                TryShortcutPolyline();
            RelaxPolyline();
            smoothedPolyline = SmoothedPolyline.FromPoints(_polyline);

            return SmoothCornersAndReturnCurve(smooth, out smoothedPolyline);

        }

        void TryShortcutPolyline() {
            if(UseInnerPolylingShortcutting) 
                while (ShortcutPolylineOneTime()) {}
            if (UsePolylineEndShortcutting)
                TryShortCutThePolylineEnds();
        }
        
        void TryShortCutThePolylineEnds() {
            TryShortcutPolylineStart();
            TryShortcutPolylineEnd();
        }

        void TryShortcutPolylineEnd() {
            PolylinePoint a = _polyline.EndPoint;
            PolylinePoint b = a.Prev;
            if (b == null) return;
            PolylinePoint c = b.Prev;
            if (c == null) return;
            Point m = 0.5*(b.Point + c.Point);
            if (LineAvoidsTightHierarchy(a.Point, m, _sourceTightPolyline, targetTightPolyline)) {
                var p = new PolylinePoint(m) {Next = a, Prev = c};
                a.Prev = p;
                c.Next = p;
            }
        }

        void TryShortcutPolylineStart() {
            PolylinePoint a = _polyline.StartPoint;
            PolylinePoint b = a.Next;
            if (b == null) return;
            PolylinePoint c = b.Next;
            if (c == null) return;
            Point m = 0.5*(b.Point + c.Point);
            if (LineAvoidsTightHierarchy(a.Point, m, _sourceTightPolyline, targetTightPolyline)) {
                var p = new PolylinePoint(m) {Prev = a, Next = c};
                a.Next = p;
                c.Prev = p;
            }
        }

        bool ShortcutPolylineOneTime() {
            bool ret = false;
            for (PolylinePoint pp = _polyline.StartPoint; pp.Next != null && pp.Next.Next != null; pp = pp.Next)
                ret |= TryShortcutPolyPoint(pp);
            return ret;
        }

        bool TryShortcutPolyPoint(PolylinePoint pp) {
            if (LineAvoidsTightHierarchy(new LineSegment(pp.Point, pp.Next.Next.Point), SourceTightPolyline,
                                         targetTightPolyline)) {
                //remove pp.Next
                pp.Next = pp.Next.Next;
                pp.Next.Prev = pp;
                return true;
            }
            return false;
        }

        void ExtendVisibilityGraphToLocationOfTargetFloatingPort(Polyline portLoosePolyline) {
            if (VisibilityGraph == null)
                VisibilityGraph = new VisibilityGraph();

            List<Polygon> addedPolygons = null;
            Point targetLocation = targetPort.Location;
            if (!activeRectangle.Contains(targetLocation)) {
                if (activeRectangle.IsEmpty)
                    activeRectangle = new Rectangle(SourcePort.Location, targetLocation);
                else
                    activeRectangle.Add(targetLocation);
                addedPolygons = GetAddedPolygonesAndMaybeExtendActiveRectangle();
                foreach (Polygon polygon in addedPolygons)
                    VisibilityGraph.AddHole(polygon.Polyline);
            }

            if (addedPolygons == null) {
                if (targetVisibilityVertex != null)
                    VisibilityGraph.RemoveVertex(targetVisibilityVertex);
                CalculateEdgeTargetVisibilityGraphForFloatingPort(targetLocation, portLoosePolyline);
                if (SourceVisibilityVertex == null)
                    CalculateSourcePortVisibilityGraph();
            }
            else {
                RemovePointVisibilityGraphs();
                var visibilityGraphGenerator = new InteractiveTangentVisibilityGraphCalculator(addedPolygons,
                                                                                               activePolygons,
                                                                                               VisibilityGraph);
                visibilityGraphGenerator.Run();
                activePolygons.AddRange(addedPolygons);
                CalculateEdgeTargetVisibilityGraphForFloatingPort(targetLocation, portLoosePolyline);
                CalculateSourcePortVisibilityGraph();
            }
        }

        void CalculateEdgeTargetVisibilityGraphForFloatingPort(Point targetLocation, Polyline targetLoosePoly) {
            if (UseSpanner)
                targetVisibilityVertex = AddTransientVisibilityEdgesForPort(targetLocation, targetLoosePoly);
            else
                PointVisibilityCalculator.CalculatePointVisibilityGraph(
                    GetActivePolylinesWithException(targetLoosePoly), VisibilityGraph, targetLocation,
                    VisibilityKind.Tangent, out targetVisibilityVertex);
        }

        VisibilityVertex AddTransientVisibilityEdgesForPort(Point point, IEnumerable<Point> loosePoly) {
            VisibilityVertex v = GetVertex(point);

            if (v != null)
                return v;

            v = visibilityGraph.AddVertex(point);
            if (loosePoly != null) //if the edges have not been calculated do it in a quick and dirty mode
                foreach (Point p in loosePoly)
                    visibilityGraph.AddEdge(point, p, ((a, b) => new TollFreeVisibilityEdge(a, b)));
            else {
                PointVisibilityCalculator.CalculatePointVisibilityGraph(GetActivePolylines(),
                                                                        VisibilityGraph, point,
                                                                        VisibilityKind.Tangent,
                                                                        out v);
                Debug.Assert(v != null);
            }
            return v;
        }

        VisibilityVertex GetVertex(Point point) {
            VisibilityVertex v = visibilityGraph.FindVertex(point);
            if (v == null && LookForRoundedVertices)
                v = visibilityGraph.FindVertex(ApproximateComparer.Round(point));
            return v;
        }

        internal bool LookForRoundedVertices { get; set; }

        internal InteractiveObstacleCalculator ObstacleCalculator { get; set; }

        ///<summary>
        ///</summary>
        ///<param name="obstacles"></param>
        ///<param name="padding"></param>
        ///<param name="loosePadding"></param>
        ///<param name="coneSpannerAngle"></param>
        ///<param name="ignoreTightPadding"></param>
        public InteractiveEdgeRouter(IEnumerable<ICurve> obstacles, double padding, double loosePadding, double coneSpannerAngle, bool ignoreTightPadding) {
            IgnoreTightPadding = ignoreTightPadding;
            EnteringAngleBound = 80 * Math.PI / 180;
            TightPadding = padding;
            LoosePadding = loosePadding;
            OffsetForPolylineRelaxing = 0.75 * padding;
            if (coneSpannerAngle > 0) {
                Debug.Assert(coneSpannerAngle > Math.PI / 180);
                Debug.Assert(coneSpannerAngle <= 90 * Math.PI / 180);
                UseSpanner = true;
                ExpectedProgressSteps = ConeSpanner.GetTotalSteps(coneSpannerAngle);
            } else {
                ExpectedProgressSteps = obstacles.Count();
            }
            ConeSpannerAngle = coneSpannerAngle;
            Obstacles = obstacles;
            CalculateObstacles();
        }

        internal bool IgnoreTightPadding {get;set;}
        

        IEnumerable<Polyline> GetActivePolylinesWithException(Polyline targetLoosePoly) {
            return from polygon in activePolygons where polygon.Polyline != targetLoosePoly select polygon.Polyline;
        }

        ICurve RouteEdgeToBoundaryPort(Polyline portLoosePolyline, bool smooth, out SmoothedPolyline smoothedPolyline) {
            TargetLoosePolyline = portLoosePolyline;
            if (sourcePort is FloatingPort)
                return RouteFromFloatingPortToBoundaryPort(smooth, out smoothedPolyline);
            return RouteFromBoundaryPortToBoundaryPort(smooth, out smoothedPolyline);
        }

        ICurve RouteFromBoundaryPortToBoundaryPort(bool smooth, out SmoothedPolyline smoothedPolyline) {
            Point sourcePortLocation = SourcePort.Location;
            ICurve curve;
            Point targetPortLocation = targetPort.Location;
            var ls = new LineSegment(sourcePortLocation, targetPortLocation);
            if (LineCanBeAcceptedForRouting(ls)) {
                _polyline = new Polyline();
                _polyline.AddPoint(ls.Start);
                _polyline.AddPoint(ls.End);
                smoothedPolyline = SmoothedPolylineFromTwoPoints(ls.Start,ls.End);
                curve = SmoothedPolyline.FromPoints(_polyline).CreateCurve();
            } else {
                //try three variants with two segments
                Point takenOutPoint = TakeBoundaryPortOutsideOfItsLoosePolyline(targetPort.Curve,
                                                                                ((CurvePort) targetPort).Parameter,
                                                                                TargetLoosePolyline);
                ls = new LineSegment(sourcePortLocation, takenOutPoint);
                if (InsideOfTheAllowedConeOfBoundaryPort(takenOutPoint, SourcePort as CurvePort) &&
                    LineAvoidsTightHierarchy(ls, _sourceTightPolyline)) {
                    _polyline = new Polyline();
                    _polyline.AddPoint(ls.Start);
                    _polyline.AddPoint(ls.End);
                    _polyline.AddPoint(targetPortLocation);
                    curve = SmoothCornersAndReturnCurve(smooth, out smoothedPolyline);
                } else {
                    ls = new LineSegment(StartPointOfEdgeRouting, targetPortLocation);
                    if (InsideOfTheAllowedConeOfBoundaryPort(StartPointOfEdgeRouting, TargetPort as CurvePort) &&
                        LineAvoidsTightHierarchy(ls)) {
                        _polyline = new Polyline();
                        _polyline.AddPoint(sourcePortLocation);
                        _polyline.AddPoint(ls.Start);
                        _polyline.AddPoint(ls.End);
                        curve = SmoothCornersAndReturnCurve(smooth, out smoothedPolyline);
                    } else {
                        // we still can make the polyline with two segs when the port sticking segs are intersecting
                        Point x;
                        if (LineSegment.Intersect(sourcePortLocation, StartPointOfEdgeRouting, targetPortLocation,
                                                  takenOutPoint, out x)) {
                            _polyline = new Polyline();
                            _polyline.AddPoint(sourcePortLocation);
                            _polyline.AddPoint(x);
                            _polyline.AddPoint(targetPortLocation);
                            curve = SmoothCornersAndReturnCurve(smooth, out smoothedPolyline);
                        } else if (ApproximateComparer.Close(StartPointOfEdgeRouting, takenOutPoint)) {
                            _polyline = new Polyline();
                            _polyline.AddPoint(sourcePortLocation);
                            _polyline.AddPoint(takenOutPoint);
                            _polyline.AddPoint(targetPortLocation);
                            curve = SmoothCornersAndReturnCurve(smooth, out smoothedPolyline);
                        } else if (LineAvoidsTightHierarchy(new LineSegment(StartPointOfEdgeRouting, takenOutPoint))) {
                            //can we do three segments?
                            _polyline = new Polyline();
                            _polyline.AddPoint(sourcePortLocation);
                            _polyline.AddPoint(StartPointOfEdgeRouting);
                            _polyline.AddPoint(takenOutPoint);
                            _polyline.AddPoint(targetPortLocation);
                            curve = SmoothCornersAndReturnCurve(smooth, out smoothedPolyline);
                        } else {
                            ExtendVisibilityGraphToTargetBoundaryPort(takenOutPoint);
                            _polyline = GetShortestPolyline(SourceVisibilityVertex, TargetVisibilityVertex);
                            
                            Polyline tmpTargetTight;
                            Polyline tmpSourceTight = HideSourceTargetTightsIfNeeded(out tmpTargetTight);
                            TryShortcutPolyline();
                            RecoverSourceTargetTights(tmpSourceTight, tmpTargetTight);

                            RelaxPolyline();

                            _polyline.PrependPoint(sourcePortLocation);
                            _polyline.AddPoint(targetPortLocation);
                            curve = SmoothCornersAndReturnCurve(smooth, out smoothedPolyline);
                        }
                    }
                }
            }
            return curve;
        }

        void RecoverSourceTargetTights(Polyline tmpSourceTight, Polyline tmpTargetTight) {
            SourceTightPolyline = tmpSourceTight;
            TargetTightPolyline = tmpTargetTight;
        }

        Polyline HideSourceTargetTightsIfNeeded(out Polyline tmpTargetTight) {
            Polyline tmpSourceTight = SourceTightPolyline;
            tmpTargetTight = TargetTightPolyline;
            SourceTightPolyline = TargetTightPolyline = null;
            return tmpSourceTight;
        }

        bool LineAvoidsTightHierarchy(LineSegment lineSegment) {
            return
                IntersectionsOfLineAndRectangleNodeOverPolyline(lineSegment, ObstacleCalculator.RootOfTightHierarchy).
                    Count == 0;
        }


        ICurve RouteFromFloatingPortToBoundaryPort(bool smooth, out SmoothedPolyline smoothedPolyline) {
            Point targetPortLocation = targetPort.Location;
            LineSegment ls;
            if (InsideOfTheAllowedConeOfBoundaryPort(sourcePort.Location, (CurvePort)targetPort)) {
                ls = new LineSegment(SourcePort.Location, targetPortLocation);
                if (LineCanBeAcceptedForRouting(ls)) {
                    smoothedPolyline = SmoothedPolylineFromTwoPoints(ls.Start, ls.End);
                    return ls;
                }
            }
            Point takenOutTargetPortLocation = TakeBoundaryPortOutsideOfItsLoosePolyline(TargetPort.Curve,
                                                                                         ((CurvePort) TargetPort).
                                                                                             Parameter,
                                                                                         TargetLoosePolyline);
            //can we do with just two line segments?
            ls = new LineSegment(SourcePort.Location, takenOutTargetPortLocation);
            if (LineAvoidsTightHierarchy(ls, _sourceTightPolyline)) {
                _polyline=new Polyline(ls.Start, ls.End, targetPortLocation);
                smoothedPolyline = SmoothedPolyline.FromPoints(_polyline);
                return smoothedPolyline.CreateCurve();
            }
            ExtendVisibilityGraphToTargetBoundaryPort(takenOutTargetPortLocation);

            _polyline = GetShortestPolyline(SourceVisibilityVertex, TargetVisibilityVertex);

            RelaxPolyline();
            _polyline.AddPoint(targetPortLocation);
            return SmoothCornersAndReturnCurve(smooth, out smoothedPolyline);
        }

        bool LineAvoidsTightHierarchy(LineSegment ls, Polyline polylineToExclude) {
            bool lineIsGood = true;
            foreach (IntersectionInfo ii in
                IntersectionsOfLineAndRectangleNodeOverPolyline(ls, ObstacleCalculator.RootOfTightHierarchy))
                if (ii.Segment1 != polylineToExclude) {
                    lineIsGood = false;
                    break;
                }
            return lineIsGood;
        }

        bool LineAvoidsTightHierarchy(LineSegment ls, Polyline polylineToExclude0, Polyline polylineToExclude1) {
            bool lineIsGood = true;
            foreach (IntersectionInfo ii in
                IntersectionsOfLineAndRectangleNodeOverPolyline(ls, ObstacleCalculator.RootOfTightHierarchy))
                if (!(ii.Segment1 == polylineToExclude0 || ii.Segment1 == polylineToExclude1)) {
                    lineIsGood = false;
                    break;
                }
            return lineIsGood;
        }

        bool LineAvoidsTightHierarchy(Point a, Point b, Polyline polylineToExclude0, Polyline polylineToExclude1) {
            return LineAvoidsTightHierarchy(new LineSegment(a, b), polylineToExclude0, polylineToExclude1);
        }

        void ExtendVisibilityGraphToTargetBoundaryPort(Point takenOutTargetPortLocation) {
            List<Polygon> addedPolygons = null;
            if (VisibilityGraph == null)
                VisibilityGraph = new VisibilityGraph();

            if (!activeRectangle.Contains(takenOutTargetPortLocation) ||
                !activeRectangle.Contains(TargetLoosePolyline.BoundingBox)) {
                if (activeRectangle.IsEmpty) {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=369 there are no structs in js
                    activeRectangle = TargetLoosePolyline.BoundingBox.Clone();
#else
                    activeRectangle = TargetLoosePolyline.BoundingBox;
#endif
                    activeRectangle.Add(SourcePort.Location);
                    activeRectangle.Add(StartPointOfEdgeRouting);
                    activeRectangle.Add(takenOutTargetPortLocation);
                } else {
                    activeRectangle.Add(takenOutTargetPortLocation);
                    activeRectangle.Add(TargetLoosePolyline.BoundingBox);
                }
                addedPolygons = GetAddedPolygonesAndMaybeExtendActiveRectangle();
                foreach (Polygon polygon in addedPolygons)
                    VisibilityGraph.AddHole(polygon.Polyline);
            }

            if (addedPolygons == null) {
                if (targetVisibilityVertex != null)
                    VisibilityGraph.RemoveVertex(targetVisibilityVertex);
                CalculateEdgeTargetVisibilityGraph(takenOutTargetPortLocation);
            } else {
                RemovePointVisibilityGraphs();
                var visibilityGraphGenerator = new InteractiveTangentVisibilityGraphCalculator(addedPolygons,
                                                                                               activePolygons,
                                                                                               VisibilityGraph);
                visibilityGraphGenerator.Run();
                activePolygons.AddRange(addedPolygons);
                CalculateEdgeTargetVisibilityGraph(takenOutTargetPortLocation);
                CalculateSourcePortVisibilityGraph();
            }
        }

        /// <summary>
        /// returns the hit object
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public Polyline GetHitLoosePolyline(Point point) {
            if (ObstacleCalculator.IsEmpty() || ObstacleCalculator.RootOfLooseHierarchy == null)
                return null;
            return GetFirstHitPolyline(point, ObstacleCalculator.RootOfLooseHierarchy);
        }

        internal static Polyline GetFirstHitPolyline(Point point, RectangleNode<Polyline, Point> rectangleNode) {
            RectangleNode<Polyline, Point> rectNode = GetFirstHitRectangleNode(point, rectangleNode);
            return rectNode != null ? rectNode.UserData : null;
        }

        static RectangleNode<Polyline, Point> GetFirstHitRectangleNode(Point point, RectangleNode<Polyline, Point> rectangleNode) {
            if (rectangleNode == null)
                return null;
            return rectangleNode.FirstHitNode(point,
                                              (pnt, polyline) =>
                                              Curve.PointRelativeToCurveLocation(pnt, polyline) != PointLocation.Outside
                                                  ? HitTestBehavior.Stop
                                                  : HitTestBehavior.Continue);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clean() {
            SourcePort = TargetPort = null;
            SourceLoosePolyline = SourceTightPolyline = null;
            targetTightPolyline = TargetLoosePolyline = null;

            VisibilityGraph = null;
            _sourceVisibilityVertex = targetVisibilityVertex = null;
            activePolygons.Clear();
            alreadyAddedOrExcludedPolylines.Clear();
            activeRectangle.SetToEmpty();
        }

        /// <summary>
        /// setting source port and the loose polyline of the port
        /// </summary>
        /// <param name="port"></param>
        /// <param name="sourceLoosePolylinePar"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "polyline"),
         SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public void SetSourcePortAndSourceLoosePolyline(Port port, Polyline sourceLoosePolylinePar) {
            SourceLoosePolyline = sourceLoosePolylinePar;
            sourcePort = port;
            if (sourcePort != null) {
                SourceTightPolyline = GetFirstHitPolyline(sourcePort.Location, ObstacleCalculator.RootOfTightHierarchy);
                if (sourcePort is FloatingPort) {
                    alreadyAddedOrExcludedPolylines.Insert(SourceLoosePolyline);
                    //we need to exclude the loose polyline around the source port from the tangent visibily graph
                    StartPointOfEdgeRouting = SourcePort.Location;
                }
                else
                    StartPointOfEdgeRouting = TakeBoundaryPortOutsideOfItsLoosePolyline(SourcePort.Curve,
                                                                                        ((CurvePort) sourcePort).
                                                                                            Parameter,
                                                                                        SourceLoosePolyline);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void RunInternal() {
            CalculateWholeTangentVisibilityGraph();
        }

        internal void CalculateWholeTangentVisibilityGraph() {
            VisibilityGraph = new VisibilityGraph();
            CalculateWholeVisibilityGraphOnExistingGraph();
        }

        internal void CalculateWholeVisibilityGraphOnExistingGraph() {
            activePolygons = new List<Polygon>(AllPolygons());
            foreach (Polyline polylineLocal in ObstacleCalculator.LooseObstacles)
                VisibilityGraph.AddHole(polylineLocal);

            AlgorithmBase visibilityGraphGenerator;
            if (UseSpanner) {
                visibilityGraphGenerator = new ConeSpanner(ObstacleCalculator.LooseObstacles, VisibilityGraph)
                {ConeAngle = ConeSpannerAngle};
            }
            else {
                visibilityGraphGenerator = new InteractiveTangentVisibilityGraphCalculator(new List<Polygon>(),
                                                                                           activePolygons,
                                                                                           visibilityGraph);
            }
            visibilityGraphGenerator.Run();
        }

        ///<summary>
        ///</summary>
        ///<param name="sourcePortLocal"></param>
        ///<param name="targetPortLocal"></param>
        ///<param name="smooth"></param>
        ///<param name="smoothedPolyline"></param>
        ///<returns></returns>
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        public ICurve RouteSplineFromPortToPortWhenTheWholeGraphIsReady(Port sourcePortLocal,
                                                                          Port targetPortLocal, bool smooth, out SmoothedPolyline smoothedPolyline) {
            bool reversed = (sourcePortLocal is FloatingPort && targetPortLocal is CurvePort) 
                || sourcePortLocal is HookUpAnywhereFromInsidePort;
            if (reversed) {
                Port tmp = sourcePortLocal;
                sourcePortLocal = targetPortLocal;
                targetPortLocal = tmp;
            }
            sourcePort = sourcePortLocal;
            targetPort = targetPortLocal;

            FigureOutSourceTargetPolylinesAndActiveRectangle();
            ICurve curve = GetEdgeGeomByRouting(smooth, out smoothedPolyline);
            if (curve == null) return null;

            _sourceVisibilityVertex = targetVisibilityVertex = null;
            if (reversed)
                curve = curve.Reverse();
            return curve;
        }

        ICurve GetEdgeGeomByRouting(bool smooth, out SmoothedPolyline smoothedPolyline) {
            targetIsInsideOfSourceTightPolyline = SourceTightPolyline == null ||
                                                  Curve.PointRelativeToCurveLocation(targetPort.Location,
                                                                                     SourceTightPolyline) ==
                                                  PointLocation.Inside;
            sourceIsInsideOfTargetTightPolyline = TargetTightPolyline == null ||
                                                  Curve.PointRelativeToCurveLocation(sourcePort.Location,
                                                                                     TargetTightPolyline) ==
                                                  PointLocation.Inside;
            var curvePort = sourcePort as CurvePort;
            ICurve curve;
            if (curvePort != null) {
                StartPointOfEdgeRouting = !targetIsInsideOfSourceTightPolyline
                                              ? TakeBoundaryPortOutsideOfItsLoosePolyline(curvePort.Curve,
                                                                                          curvePort.Parameter,
                                                                                          SourceLoosePolyline)
                                              : curvePort.Location;
                CalculateSourcePortVisibilityGraph();
                if (targetPort is CurvePort)
                    curve = RouteFromBoundaryPortToBoundaryPort(smooth, out smoothedPolyline);
                else
                    curve = RouteFromBoundaryPortToFloatingPort(targetLoosePolyline, smooth, out smoothedPolyline);
            }
            else {
                if (targetPort is FloatingPort) {
                    ExtendVisibilityGraphFromFloatingSourcePort();
                    Debug.Assert(_sourceVisibilityVertex != null);
                    //the edge has to be reversed to route from CurvePort to FloatingPort
                    curve = RouteFromFloatingPortToFloatingPort(targetLoosePolyline, smooth, out smoothedPolyline);
                } else
                    curve = RouteFromFloatingPortToAnywherePort(((HookUpAnywhereFromInsidePort) targetPort).LoosePolyline,
                                                                smooth, out smoothedPolyline,
                                                                (HookUpAnywhereFromInsidePort) targetPort);
            }
            return curve;
        }

        ICurve RouteFromFloatingPortToAnywherePort(Polyline targetLoosePoly, bool smooth, out SmoothedPolyline smoothedPolyline, HookUpAnywhereFromInsidePort port) {
            if (!port.Curve.BoundingBox.Contains(sourcePort.Location)) {
                smoothedPolyline = null;
                return null;
            }

            _sourceVisibilityVertex = GetVertex(sourcePort.Location);

            _polyline = GetShortestPolylineToMulitpleTargets(SourceVisibilityVertex, Targets(targetLoosePoly));
            if (_polyline == null) {
                smoothedPolyline = null;
                return null;
            }
            if (UseSpanner)
                TryShortcutPolyline();
            RelaxPolyline();
            FixLastPolylinePointForAnywherePort(port);
            if (port.HookSize > 0)
                BuildHook(port);

            return SmoothCornersAndReturnCurve(smooth, out smoothedPolyline);
        }

        void BuildHook(HookUpAnywhereFromInsidePort port) {
            var curve = port.Curve;
            //creating a hook
            var ellipse = new Ellipse(port.HookSize, port.HookSize, _polyline.End);
            var intersections = Curve.GetAllIntersections(curve, ellipse, true).ToArray();
            Debug.Assert(intersections.Length == 2);
            if (Point.GetTriangleOrientation(intersections[0].IntersectionPoint, _polyline.End, _polyline.EndPoint.Prev.Point) == TriangleOrientation.Counterclockwise)
                intersections.Reverse(); //so the [0] point is to the left of the Polyline

            var polylineTangent = (_polyline.End - _polyline.EndPoint.Prev.Point).Normalize();

            var tan0 = curve.Derivative(intersections[0].Par0).Normalize();
            var prj0 = tan0 * polylineTangent;
            if (Math.Abs(prj0) < 0.2)
                ExtendPolyline(tan0, intersections[0], polylineTangent, port);
            else {
                var tan1 = curve.Derivative(intersections[1].Par0).Normalize();
                var prj1 = tan1 * polylineTangent;
                if (prj1 < prj0)
                    ExtendPolyline(tan1, intersections[1], polylineTangent, port);
                else
                    ExtendPolyline(tan0, intersections[0], polylineTangent, port);

            }
        }

        void ExtendPolyline(Point tangentAtIntersection, IntersectionInfo x, Point polylineTangent, HookUpAnywhereFromInsidePort port) {

            var normal=tangentAtIntersection.Rotate(Math.PI/2);
            if(normal*polylineTangent<0)
                normal=-normal;

            var pointBeforeLast = x.IntersectionPoint + normal * port.HookSize;
            Point pointAfterX;
            if (!Point.LineLineIntersection(pointBeforeLast, pointBeforeLast+tangentAtIntersection, _polyline.End, _polyline.End+polylineTangent, out pointAfterX))
                return;

            _polyline.AddPoint(pointAfterX);
            _polyline.AddPoint(pointBeforeLast);
            _polyline.AddPoint(x.IntersectionPoint);
        }

        void FixLastPolylinePointForAnywherePort(HookUpAnywhereFromInsidePort port) {
            while (true) {
                PolylinePoint lastPointInside = GetLastPointInsideOfCurveOnPolyline(port.Curve);
                lastPointInside.Next.Next = null;
                _polyline.EndPoint=lastPointInside.Next;
                var dir = lastPointInside.Next.Point - lastPointInside.Point;
                dir = dir.Normalize()*port.Curve.BoundingBox.Diagonal; //make it a long vector
                var dir0 = dir.Rotate(-port.AdjustmentAngle);
                var dir1 = dir.Rotate(port.AdjustmentAngle);
                var rx=Curve.CurveCurveIntersectionOne(port.Curve, new LineSegment(lastPointInside.Point, lastPointInside.Point+dir0), true);
                var lx=Curve.CurveCurveIntersectionOne(port.Curve, new LineSegment(lastPointInside.Point, lastPointInside.Point+dir1), true);
                if (rx == null || lx == null) return;
                   //this.ShowPolylineAndObstacles(Polyline, new LineSegment(lastPointInside.Point, lastPointInside.Point+dir0), new LineSegment(lastPointInside.Point, rerPoint+dir1), port.Curve);

                var trimmedCurve = GetTrimmedCurveForHookingUpAnywhere(port.Curve, lastPointInside, rx, lx);
                var newLastPoint = trimmedCurve[trimmedCurve.ClosestParameter(lastPointInside.Point)];
                if (!LineAvoidsTightHierarchy(new LineSegment(lastPointInside.Point, newLastPoint), SourceTightPolyline, null)) {
                    var xx=Curve.CurveCurveIntersectionOne(port.Curve, new LineSegment(lastPointInside.Point, lastPointInside.Next.Point), false);
                    if (xx == null) return;
                        //this.ShowPolylineAndObstacles(Polyline, port.Curve);
                    _polyline.EndPoint.Point = xx.IntersectionPoint;
                    break;
                }

                _polyline.EndPoint.Point = newLastPoint;
                if (lastPointInside.Prev == null  || !TryShortcutPolyPoint(lastPointInside.Prev))
                    break;                
            }
        }

        static ICurve GetTrimmedCurveForHookingUpAnywhere(ICurve curve, PolylinePoint lastPointInside, IntersectionInfo x0, IntersectionInfo x1) {
            var clockwise =
                Point.GetTriangleOrientation(x1.IntersectionPoint, x0.IntersectionPoint, lastPointInside.Point) ==
                TriangleOrientation.Clockwise;

            double rightX = x0.Par0;
            double leftX = x1.Par0;
            ICurve tr0, tr1;
            Curve ret;
            if (clockwise) {
                if (rightX < leftX)
                    return curve.Trim(rightX, leftX);

                tr0 = curve.Trim(rightX, curve.ParEnd);
                tr1 = curve.Trim(curve.ParStart, leftX);
                ret = new Curve();
                return ret.AddSegs(tr0, tr1);
            }

            if (leftX < rightX)
                return curve.Trim(leftX, rightX);
            tr0 = curve.Trim(leftX, curve.ParEnd);
            tr1 = curve.Trim(curve.ParStart, rightX);
            ret = new Curve();
            return ret.AddSegs(tr0, tr1);
        }

        PolylinePoint GetLastPointInsideOfCurveOnPolyline(ICurve curve) {
            for (var p = _polyline.EndPoint.Prev; p != null; p = p.Prev) {
                if (p.Prev == null)
                    return p;
                if (Curve.PointRelativeToCurveLocation(p.Point, curve) == PointLocation.Inside)
                    return p;
            }

            throw new InvalidOperationException();

        }

        Polyline GetShortestPolylineToMulitpleTargets(VisibilityVertex sourceVisVertex, IEnumerable<VisibilityVertex> targets) {
            CleanTheGraphForShortestPath();
            //ShowPolylineAndObstacles(targets.Select(t=>new Ellipse(3,3,t.Point)).ToArray());
            var pathCalc = new SingleSourceMultipleTargetsShortestPathOnVisibilityGraph(sourceVisVertex, targets, VisibilityGraph);// { dd = ShowPolylineAndObstacles };
            IEnumerable<VisibilityVertex> path = pathCalc.GetPath();
            if (path == null)
                return null;


            Debug.Assert(path.First() == sourceVisVertex && targets.Contains(path.Last()));
            var ret = new Polyline();
            foreach (VisibilityVertex v in path)
                ret.AddPoint(v.Point);
            return RemoveCollinearVertices(ret);
            
        }

        IEnumerable<VisibilityVertex> Targets(Polyline targetLoosePoly) {
            return new List<VisibilityVertex> (targetLoosePoly.Select(p=>visibilityGraph.FindVertex(p))); 
        }

        void ExtendVisibilityGraphFromFloatingSourcePort() {
            var fp = sourcePort as FloatingPort;
            Debug.Assert(fp != null);
            StartPointOfEdgeRouting = fp.Location;
            if (UseSpanner)
                _sourceVisibilityVertex = AddTransientVisibilityEdgesForPort(sourcePort.Location, SourceLoosePolyline);
            else {
                PointVisibilityCalculator.CalculatePointVisibilityGraph(
                    from p in GetActivePolylines() where p != SourceLoosePolyline select p, VisibilityGraph,
                    StartPointOfEdgeRouting, VisibilityKind.Tangent, out _sourceVisibilityVertex);
            }
        }

        void FigureOutSourceTargetPolylinesAndActiveRectangle() {
            _sourceTightPolyline = GetFirstHitPolyline(sourcePort.Location, ObstacleCalculator.RootOfTightHierarchy);
            SourceLoosePolyline = GetFirstHitPolyline(sourcePort.Location, ObstacleCalculator.RootOfLooseHierarchy);
            targetTightPolyline = GetFirstHitPolyline(targetPort.Location, ObstacleCalculator.RootOfTightHierarchy);
            targetLoosePolyline = GetFirstHitPolyline(targetPort.Location, ObstacleCalculator.RootOfLooseHierarchy);
            activeRectangle = new Rectangle(new Point(double.NegativeInfinity, double.PositiveInfinity),
                                            new Point(double.PositiveInfinity, double.NegativeInfinity));
        }


        IEnumerable<Polygon> AllPolygons() {
            foreach (Polyline p in ObstacleCalculator.LooseObstacles)
                yield return new Polygon(p);
        }


        /// <summary>
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public VisibilityGraph GetVisibilityGraph() {
            return VisibilityGraph;
        }

        //        internal void CalculateVisibilityGraph(IEnumerable<EdgeGeometry> edgeGeometries, bool qualityAtPorts)
        //        {
        //            CalculateWholeTangentVisibilityGraph();
        //            if (ConeSpannerAngle > 0 && qualityAtPorts && edgeGeometries != null)
        //                CalculatePortVisibilityGraph(GetPortLocationsPointSet(edgeGeometries));
        //        }

#if DEBUG_MSAGL && !SILVERLIGHT
        internal void ShowObstaclesAndVisGraph()
        {
            var obs = this.obstacleCalculator.LooseObstacles.Select(o => new DebugCurve(100, 1, "blue", o));
            var edges =
                visibilityGraph.Edges.Select(
                    e =>
                    new DebugCurve(70, 1, (e is TransientVisibilityEdge ? "red" : "green"),
                                   new LineSegment(e.SourcePoint, e.TargetPoint)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(obs.Concat(edges));
        }
#endif

        internal void AddActivePolygons(IEnumerable<Polygon> polygons) {
            activePolygons.AddRange(polygons);
        }
        internal void ClearActivePolygons() {
            activePolygons.Clear();
        }
    }
}