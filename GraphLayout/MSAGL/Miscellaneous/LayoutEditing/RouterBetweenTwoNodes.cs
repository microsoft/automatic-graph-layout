using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Prototype.LayoutEditing {
    /// <summary>
    /// the router between nodes
    /// </summary>
    public class RouterBetweenTwoNodes {
        readonly Dictionary<Point, Polyline> pointsToObstacles = new Dictionary<Point, Polyline>();
        VisibilityGraph _visGraph;
        ObstacleCalculator obstacleCalculator;

        /// <summary>
        /// the port of the edge start
        /// </summary>
        public Port SourcePort { get; private set; }

        /// <summary>
        /// the port of the edge end
        /// </summary>
        public Port TargetPort { get; private set; }

        const double enteringAngleBound = 10;

        /// <summary>
        /// the minimum angle between a node boundary curve and and an edge 
        /// curve at the place where the edge curve intersects the node boundary
        /// </summary>
        public double EnteringAngleBound {
            get { return enteringAngleBound; }
        }


        double minimalPadding = 1;

        /// <summary>
        /// the curve should not come to the nodes closer than MinimalPaddin
        /// </summary>
        public double Padding {
            get { return minimalPadding; }
            private set { minimalPadding = value; }
        }


        /// <summary>
        /// we pad each node but not more than MaximalPadding
        /// </summary>
        public double LoosePadding { get; set; }


        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        readonly GeometryGraph graph;

        internal Node Target { get; private set; }

        VisibilityVertex sourceVisibilityVertex;

        VisibilityVertex targetVisibilityVertex;

        VisibilityVertex TargetVisibilityVertex {
            get { return targetVisibilityVertex; }
            //            set { targetVisibilityVertex = value; }
        }

        internal Node Source { get; private set; }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal GeometryGraph Graph {
            get { return graph; }
        }

        Polyline Polyline { get; set; }

        internal static double DistanceFromPointToPolyline(Point p, Polyline poly) {
            double d = double.PositiveInfinity;
            double u;
            for (PolylinePoint pp = poly.StartPoint; pp.Next != null; pp = pp.Next)
                d = Math.Min(d, Point.DistToLineSegment(p, pp.Point, pp.Next.Point, out u));
            return d;
        }

        /// <summary>
        /// 
        /// </summary>
        [SuppressMessage("Microsoft.Naming",
            "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        double OffsetForPolylineRelaxing { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="minimalPadding"></param>
        /// <param name="maximalPadding"></param>
        /// <param name="offsetForRelaxing"></param>
        public RouterBetweenTwoNodes(GeometryGraph graph, double minimalPadding, double maximalPadding,
                                     double offsetForRelaxing) {
            this.graph = graph;
            LoosePadding = maximalPadding;
            Padding = minimalPadding;
            OffsetForPolylineRelaxing = offsetForRelaxing;
        }


        /// <summary>
        /// Routes a spline between two graph nodes. sourcePort and targetPort define the start and end point of the resulting curve:
        /// startPoint=source.BoundaryCurve[sourcePort] and endPoint=target.BoundaryCurve[target].
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="takeYourTime">if set to true then the method will try to improve the spline</param>
        public void RouteEdge(Edge edge, bool takeYourTime) {
            Source = edge.Source;
            Target = edge.Target;
            SourcePort = edge.SourcePort;
            TargetPort = edge.TargetPort;
            CalculateObstacles();
            LineSegment lineSeg = TryRouteStraightLine();
            if (lineSeg != null) {
                Polyline = new Polyline(lineSeg.Start, lineSeg.End);
                edge.UnderlyingPolyline = SmoothedPolyline.FromPoints(Polyline);
            } else {
                CalculateTangentVisibilityGraph();
                Polyline = GetShortestPolyline();
                // ShowPolylineAndObstacles();
                RelaxPolyline();
                //ShowPolylineAndObstacles();
                //ReducePolyline();
                //ShowPolylineAndObstacles();
                edge.UnderlyingPolyline = SmoothedPolyline.FromPoints(Polyline);

                if (takeYourTime) {
                    TryToRemoveInflectionsAndCollinearSegs(edge.UnderlyingPolyline);
                    SmoothCorners(edge.UnderlyingPolyline);
                }
            }

            edge.Curve = edge.UnderlyingPolyline.CreateCurve();
        }

        //void ReducePolyline() {
        //    for (PolylinePoint pp = this.Polyline.StartPoint.Next; pp.Next != null && pp.Next.Next != null;pp=pp.Next )
        //        pp = TryToRemoveOrDiminishSegment(pp, pp.Next);
        //}

        //PolylinePoint TryToRemoveOrDiminishSegment(PolylinePoint pp, PolylinePoint polylinePoint) {
        //    TriangleOrientation orientation = Point.GetTriangleOrientation(pp.Prev.Point, pp.Point, pp.Next.Point);
        //    if (orientation == Point.GetTriangleOrientation(pp.Point, pp.Next.Point, pp.Next.Next.Point)) {
        //        Point x;
        //        if (Point.LineLineIntersection(pp.Prev.Point, pp.Point, pp.Next.Point, pp.Next.Next.Point, out x)) {
        //            if (orientation == Point.GetTriangleOrientation(pp.Point, x, pp.Next.Point)) {
        //                if (!LineIntersectsTightObstacles(pp.Prev.Point, x) && !LineIntersectsTightObstacles(x, pp.Next.Next.Point)) {
        //                    PolylinePoint px = new PolylinePoint(x);
        //                    //inserting px instead of pp and pp.Next
        //                    px.Prev = pp.Prev;
        //                    pp.Prev.Next = px;
        //                    px.Next = pp.Next.Next;
        //                    pp.Next.Next.Prev = px;
        //                    return px.Prev;
        //                } else {
        //                    for (double k = 0.5; k > 0.01; k /= 2) {
        //                        Point a = pp.Point * (1 - k) + x * k;
        //                        Point b = pp.Next.Point * (1 - k) + x * k;

        //                        if (!LineIntersectsTightObstacles(pp.Point, a) &&
        //                            !LineIntersectsTightObstacles(a, b) &&
        //                            !LineIntersectsTightObstacles(b, pp.Next.Point)) {
        //                            pp.Point = a;
        //                            pp.Next.Point = b;
        //                            break;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return pp;
        //}


        //bool LineIntersectsTightObstacles(Point point, Point x) {
        //    return LineIntersectsTightObstacles(new LineSegment(point, x));    
        //}
#if TEST_MSAGL
        //private void ShowPolylineAndObstaclesWithGraph() {
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
            RelaxedPolylinePoint relaxedPolylinePoint = CreateRelaxedPolylinePoints(Polyline);
            //ShowPolylineAndObstacles();
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
                var r = new RelaxedPolylinePoint(p, p.Point) { Prev = currentRelaxed };
                currentRelaxed.Next = r;
                currentRelaxed = r;
            }
            return ret;
        }


        void RelaxPolylinePoint(RelaxedPolylinePoint relaxedPoint) {
            for (double d = OffsetForPolylineRelaxing; !RelaxWithGivenOffset(d, relaxedPoint); d /= 2) {
            }
        }

        bool RelaxWithGivenOffset(double offset, RelaxedPolylinePoint relaxedPoint) {
            SetRelaxedPointLocation(offset, relaxedPoint);
#if TEST_MSAGL
            //ShowPolylineAndObstacles();
#endif
            if (StickingSegmentDoesNotIntersectTightObstacles(relaxedPoint))
                return true;
            PullCloserRelaxedPoint(relaxedPoint.Prev);
            return false;
        }

        static void PullCloserRelaxedPoint(RelaxedPolylinePoint relaxedPolylinePoint) {
            relaxedPolylinePoint.PolylinePoint.Point = 0.2 * relaxedPolylinePoint.OriginalPosition +
                                                       0.8 * relaxedPolylinePoint.PolylinePoint.Point;
        }

        bool StickingSegmentDoesNotIntersectTightObstacles(RelaxedPolylinePoint relaxedPoint) {
            return
                !LineIntersectsTightObstacles(new LineSegment(relaxedPoint.PolylinePoint.Point,
                                                              relaxedPoint.Prev.PolylinePoint.Point)) && (
                                                                                                             (relaxedPoint
                                                                                                                  .Next ==
                                                                                                              null ||
                                                                                                              !LineIntersectsTightObstacles
                                                                                                                   (new LineSegment
                                                                                                                        (relaxedPoint
                                                                                                                             .
                                                                                                                             PolylinePoint
                                                                                                                             .
                                                                                                                             Point,
                                                                                                                         relaxedPoint
                                                                                                                             .
                                                                                                                             Next
                                                                                                                             .
                                                                                                                             PolylinePoint
                                                                                                                             .
                                                                                                                             Point))));
        }

        bool LineIntersectsTightObstacles(LineSegment ls) {
            return LineIntersectsRectangleNode(ls, obstacleCalculator.RootOfTightHierararchy);
        }

        static bool LineIntersectsRectangleNode(LineSegment ls, RectangleNode<Polyline, Point> rectNode) {
            if (!ls.BoundingBox.Intersects((Rectangle)rectNode.Rectangle))
                return false;
            if (rectNode.UserData != null) {
                // SugiyamaLayoutSettings.Show(ls, rectNode.UserData);
                return Curve.GetAllIntersections(rectNode.UserData, ls, false).Count > 0;
            }


            return LineIntersectsRectangleNode(ls, rectNode.Left) || LineIntersectsRectangleNode(ls, rectNode.Right);
        }


        static void SetRelaxedPointLocation(double offset, RelaxedPolylinePoint relaxedPoint) {
            bool leftTurn = Point.GetTriangleOrientation(relaxedPoint.Next.OriginalPosition,
                                                         relaxedPoint.OriginalPosition,
                                                         relaxedPoint.Prev.OriginalPosition) ==
                            TriangleOrientation.Counterclockwise;
            Point v =
                ((relaxedPoint.Next.OriginalPosition - relaxedPoint.Prev.OriginalPosition).Normalize() * offset).Rotate(
                    Math.PI / 2);

            if (!leftTurn)
                v = -v;
            relaxedPoint.PolylinePoint.Point = relaxedPoint.OriginalPosition + v;
        }

#if TEST_MSAGL
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void ShowPolylineAndObstacles(){
            List<ICurve> ls = CreateListWithObstaclesAndPolyline();
            SugiyamaLayoutSettings.Show(ls.ToArray());
        }


        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        List<ICurve> CreateListWithObstaclesAndPolyline(){
            var ls = new List<ICurve>();
            foreach (Polyline poly in obstacleCalculator.TightObstacles)
                ls.Add(poly);
            foreach (Polyline poly in obstacleCalculator.LooseObstacles)
                ls.Add(poly);

            ls.Add(Polyline);
            return ls;
        }
#endif

        void SmoothCorners(SmoothedPolyline edgePolyline) {
            Site a = edgePolyline.HeadSite; //the corner start
            Site b; //the corner origin
            Site c; //the corner other end
            const double mult = 1.5;

            while (Curve.FindCorner(a, out b, out c)) {
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
                } else {
                    u = v = 1;
                }

                do {
                    seg = Curve.CreateBezierSeg(k * u, k * v, a, b, c);
                    b.PreviousBezierSegmentFitCoefficient = k * u;
                    b.NextBezierSegmentFitCoefficient = k * v;
                    k /= mult;
                } while (obstacleCalculator.ObstaclesIntersectICurve(seg));

                k *= mult; //that was the last k
                if (k < 0.5) {
                    //one time try a smoother seg
                    k = 0.5 * (k + k * mult);
                    seg = Curve.CreateBezierSeg(k * u, k * v, a, b, c);
                    if (!obstacleCalculator.ObstaclesIntersectICurve(seg)) {
                        b.PreviousBezierSegmentFitCoefficient = k * u;
                        b.NextBezierSegmentFitCoefficient = k * v;
                    }
                }
                a = b;
            }
        }


        void TryToRemoveInflectionsAndCollinearSegs(SmoothedPolyline underlyingPolyline) {
            bool progress = true;
            while (progress) {
                progress = false;
                for (Site s = underlyingPolyline.HeadSite; s != null && s.Next != null; s = s.Next) {
                    if (s.Turn * s.Next.Turn < 0)
                        progress = TryToRemoveInflectionEdge(ref s) || progress;
                }
            }
        }

        bool TryToRemoveInflectionEdge(ref Site s) {
            if (!obstacleCalculator.ObstaclesIntersectLine(s.Previous.Point, s.Next.Point)) {
                Site a = s.Previous; //forget s
                Site b = s.Next;
                a.Next = b;
                b.Previous = a;
                s = a;
                return true;
            }
            if (!obstacleCalculator.ObstaclesIntersectLine(s.Previous.Point, s.Next.Next.Point)) {
                //forget about s and s.Next
                Site a = s.Previous;
                Site b = s.Next.Next;
                a.Next = b;
                b.Previous = a;
                s = a;
                return true;
            }
            if (!obstacleCalculator.ObstaclesIntersectLine(s.Point, s.Next.Next.Point)) {
                //forget about s.Next
                Site b = s.Next.Next;
                s.Next = b;
                b.Previous = s;
                return true;
            }

            return false;
        }


        LineSegment TryRouteStraightLine() {
            var ls = new LineSegment(SourcePoint, TargetPoint);
            if (obstacleCalculator.ObstaclesIntersectICurve(ls))
                return null;
            return ls;
        }

        internal Point TargetPoint {
            get {
                var tp = TargetPort as CurvePort;
                if (tp != null)
                    return Target.BoundaryCurve[tp.Parameter];
                return TargetPort.Location;
            }
        }

        internal Point SourcePoint {
            get {
                var sp = SourcePort as CurvePort;
                if (sp != null)
                    return Source.BoundaryCurve[sp.Parameter];
                return SourcePort.Location;
            }
        }


        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        Polyline GetShortestPolyline() {
            var pathCalc = new SingleSourceSingleTargetShortestPathOnVisibilityGraph(_visGraph, this.sourceVisibilityVertex,
                                                                                     TargetVisibilityVertex);
            var path = pathCalc.GetPath(false);
            var ret = new Polyline();
            foreach (var v in path)
                ret.AddPoint(v.Point);
            return RemoveCollinearPoint(ret);
        }

        static Polyline RemoveCollinearPoint(Polyline ret) {
            for (PolylinePoint pp = ret.StartPoint.Next; pp.Next != null; pp = pp.Next)
                if (Point.GetTriangleOrientation(pp.Prev.Point, pp.Point, pp.Next.Point) == TriangleOrientation.Collinear) {
                    pp.Prev.Next = pp.Next;
                    pp.Next.Prev = pp.Prev;
                }

            return ret;
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        void CalculateTangentVisibilityGraph() {
            _visGraph = VisibilityGraph.GetVisibilityGraphForShortestPath(
                SourcePoint, TargetPoint, obstacleCalculator.LooseObstacles, out sourceVisibilityVertex,
                out targetVisibilityVertex);
        }


        void CalculateObstacles() {
            obstacleCalculator = new ObstacleCalculator(this);
            obstacleCalculator.Calculate();
            foreach (Polyline poly in obstacleCalculator.TightObstacles)
                foreach (Point p in poly)
                    pointsToObstacles[p] = poly;
        }
    }
}