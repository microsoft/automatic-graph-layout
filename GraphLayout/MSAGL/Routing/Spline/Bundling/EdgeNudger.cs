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

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// this class nudges the edges
    /// </summary>
    internal class EdgeNudger : AlgorithmBase {
        readonly BundlingSettings bundlingSettings;
        readonly MetroGraphData metroGraphData;
        IMetroMapOrderingAlgorithm metroOrdering;

        /// <summary>
        /// Constructor
        /// </summary>
        internal EdgeNudger(MetroGraphData metroGraphData, BundlingSettings bundlingSettings) {
            this.metroGraphData = metroGraphData;
            this.bundlingSettings = bundlingSettings;
        }


      

        protected override void RunInternal() {
            CreateMetroOrdering();
            InitRadii();
            FinalizePaths();
        }

        void InitRadii() {
            new HubRadiiCalculator(metroGraphData, bundlingSettings).CreateNodeRadii();
        }

        /// <summary>
        /// bundle-map ordering
        /// </summary>
        void CreateMetroOrdering() {
            if (bundlingSettings.UseGreedyMetrolineOrdering)
                metroOrdering = new GeneralMetroMapOrdering(metroGraphData.Metrolines);
            else
                metroOrdering = new LinearMetroMapOrdering(metroGraphData.Metrolines, metroGraphData.PointToStations);
        }

        void FinalizePaths() {
            CreateBundleBases();
            CreateSegmentsInsideHubs();

#if TEST_MSAGL
            //ShowHubs(metroGraphData, metroOrdering, null);
#endif
            CreateCurves();
        }

        void CreateBundleBases() {
            var bbCalc = new BundleBasesCalculator(metroOrdering, metroGraphData, bundlingSettings);
            bbCalc.Run();
        }

        void CreateCurves() {
            Debug.Assert(metroGraphData.Metrolines.Count == metroGraphData.Edges.Length);
            for (int i = 0; i < metroGraphData.Metrolines.Count; i++)
                CreateCurveLine(metroGraphData.Metrolines[i], metroGraphData.Edges[i]);
        }

        void CreateCurveLine(Metroline line, EdgeGeometry edge) {
            var c = new Curve();
            Point start = FindCurveStart(metroGraphData, metroOrdering, line);
            Point currentEnd = start;
            var hubSegsOfLine = HubSegsOfLine(metroGraphData, metroOrdering, line);
            foreach (var seg in hubSegsOfLine) {
                if (seg == null) continue;
                c.AddSegment(new LineSegment(currentEnd, seg.Start));
                c.AddSegment(seg);
                currentEnd = seg.End;
            }
            c.AddSegment(new LineSegment(currentEnd, FindCurveEnd(metroGraphData, metroOrdering, line)));
            edge.Curve = c;
            
        }

        static Point FindCurveStart(MetroGraphData metroGraphData, IMetroMapOrderingAlgorithm metroOrdering, Metroline metroline) {
            Station u = metroGraphData.PointToStations[metroline.Polyline.StartPoint.Point];
            Station v = metroGraphData.PointToStations[metroline.Polyline.StartPoint.Next.Point];

            BundleBase bb = u.BundleBases[v];
            int index = (!bb.IsParent ? metroOrdering.GetLineIndexInOrder(v, u, metroline) : metroOrdering.GetLineIndexInOrder(u, v, metroline));
            return bb.Points[index];
        }

        static Point FindCurveEnd(MetroGraphData metroGraphData, IMetroMapOrderingAlgorithm metroOrdering, Metroline metroline) {
            Station u = metroGraphData.PointToStations[metroline.Polyline.EndPoint.Prev.Point];
            Station v = metroGraphData.PointToStations[metroline.Polyline.EndPoint.Point];

            BundleBase bb = v.BundleBases[u];
            int index = (!bb.IsParent ? metroOrdering.GetLineIndexInOrder(u, v, metroline) : metroOrdering.GetLineIndexInOrder(v, u, metroline));
            return bb.Points[index];
        }

        
        static IEnumerable<ICurve> HubSegsOfLine(MetroGraphData metroGraphData, IMetroMapOrderingAlgorithm metroOrdering, Metroline line) {
            for (PolylinePoint i = line.Polyline.StartPoint.Next; i.Next != null; i = i.Next)
                yield return SegOnLineVertex(metroGraphData, metroOrdering, line, i);
        }

        static ICurve SegOnLineVertex(MetroGraphData metroGraphData, IMetroMapOrderingAlgorithm metroOrdering, Metroline line, PolylinePoint i) {
            Station u = metroGraphData.PointToStations[i.Prev.Point];
            Station v = metroGraphData.PointToStations[i.Point];
            BundleBase h0 = v.BundleBases[u];
            int j0 = metroOrdering.GetLineIndexInOrder(u, v, line);
            if (h0.OrientedHubSegments[j0] == null || h0.OrientedHubSegments[j0].Segment == null) {
                var w = metroGraphData.PointToStations[i.Next.Point];
                var otherBase = v.BundleBases[w];
                var j1 = metroOrdering.GetLineIndexInOrder(w, v, line);
                return new LineSegment(h0.Points[j0], otherBase.Points[j1]);
            }

            return h0.OrientedHubSegments[j0].Segment;
        }

        void CreateSegmentsInsideHubs() {
            foreach (var metroline in metroGraphData.Metrolines)
                CreateOrientedSegsOnLine(metroline);

            if (bundlingSettings.UseCubicBezierSegmentsInsideOfHubs)
                FanBezierSegs();
        }

        void CreateOrientedSegsOnLine(Metroline line) {
            for (PolylinePoint polyPoint = line.Polyline.StartPoint.Next; polyPoint.Next != null; polyPoint = polyPoint.Next)
                CreateOrientedSegsOnLineVertex(line, polyPoint);
        }

        void CreateOrientedSegsOnLineVertex(Metroline line, PolylinePoint polyPoint) {
            Station u = metroGraphData.PointToStations[polyPoint.Prev.Point];
            Station v = metroGraphData.PointToStations[polyPoint.Point];
            Station w = metroGraphData.PointToStations[polyPoint.Next.Point];
            BundleBase h0 = v.BundleBases[u];
            BundleBase h1 = v.BundleBases[w];
            int j0 = metroOrdering.GetLineIndexInOrder(u, v, line);
            int j1 = metroOrdering.GetLineIndexInOrder(w, v, line);
            var seg = bundlingSettings.UseCubicBezierSegmentsInsideOfHubs ?
                StandardBezier(h0.Points[j0], h0.Tangents[j0], h1.Points[j1], h1.Tangents[j1]) :
                BiArc(h0.Points[j0], h0.Tangents[j0], h1.Points[j1], h1.Tangents[j1]);

            h0.OrientedHubSegments[j0].Segment = seg;
            h1.OrientedHubSegments[j1].Segment = seg;
        }

        #region Debug show

#if TEST_MSAGL
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static internal void ShowHubs(MetroGraphData metroGraphData, IMetroMapOrderingAlgorithm metroMapOrdering, Station station) {
            var ttt = GetAllDebugCurves(metroMapOrdering, metroGraphData);
            if (station != null)
                ttt = ttt.Concat(new[] { new DebugCurve(255, 3, "pink", CurveFactory.CreateDiamond(20, 20, station.Position)) });
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(ttt);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static internal IEnumerable<DebugCurve> GetAllDebugCurves(IMetroMapOrderingAlgorithm metroMapOrdering, MetroGraphData metroGraphData) {
            return GraphNodes(metroGraphData).Concat(VertexDebugCurves(metroMapOrdering, metroGraphData)).Concat(DebugEdges(metroGraphData));
        }

        static IEnumerable<DebugCurve> DebugEdges(MetroGraphData metroGraphData1) {
            return metroGraphData1.Edges.Select(e => new DebugCurve(40, 0.1, "gray", e.Curve));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static IEnumerable<DebugCurve> VertexDebugCurves(IMetroMapOrderingAlgorithm metroMapOrdering, MetroGraphData metroGraphData) {
            return DebugCircles(metroGraphData).Concat(DebugHubBases(metroGraphData)).
                Concat(DebugSegs(metroGraphData, metroMapOrdering)).
                Concat(BetweenHubs(metroMapOrdering, metroGraphData));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static IEnumerable<DebugCurve> BetweenHubs(IMetroMapOrderingAlgorithm metroMapOrdering, MetroGraphData metroGraphData) {
            foreach (Metroline ml in metroGraphData.Metrolines) {
                List<Tuple<Point, Point>> segs = GetInterestingSegs(metroGraphData, metroMapOrdering, ml);
                string color = GetMonotoneColor(ml.Polyline.Start, ml.Polyline.End, segs);
                foreach (var seg in segs)
                    yield return new DebugCurve(100, ml.Width, color, new LineSegment(seg.Item1, seg.Item2));
            }
        }

        static List<Tuple<Point, Point>> GetInterestingSegs(MetroGraphData metroGraphData, IMetroMapOrderingAlgorithm metroMapOrdering, Metroline line) {
            var ret = new List<Tuple<Point, Point>>();
            Point start = FindCurveStart(metroGraphData, metroMapOrdering, line);
            var cubicSegs = HubSegsOfLine(metroGraphData, metroMapOrdering, line);
            foreach (var seg in cubicSegs) {
                if (seg == null) continue;
                ret.Add(new Tuple<Point, Point>(start, seg.Start));
                start = seg.End;
            }
            ret.Add(new Tuple<Point, Point>(start, FindCurveEnd(metroGraphData, metroMapOrdering, line)));

            return ret;
        }

        static string GetMonotoneColor(Point start, Point end, List<Tuple<Point, Point>> segs) {
            return "green";
            //            Point dir = end - start;
            //            bool monotone = segs.All(seg => (seg.Second - seg.First)*dir >= 0);
            //            return monotone ? "green" : "magenta";
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static IEnumerable<DebugCurve> DebugHubBases(MetroGraphData metroGraphData) {
            List<DebugCurve> dc = new List<DebugCurve>();
            foreach (var s in metroGraphData.Stations)
                foreach (var h in s.BundleBases.Values) {
                    dc.Add(new DebugCurve(100, 1, "red", new LineSegment(h.LeftPoint, h.RightPoint)));
                }

            return dc;
            //return
            //    metroGraphData.Stations.SelectMany(s => s.BundleBases.Values).Select(
            //        h => new DebugCurve(100, 0.01, "red", new LineSegment(h.Points[0], h.Points.Last())));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static IEnumerable<DebugCurve> DebugCircles(MetroGraphData metroGraphData) {
            return
                metroGraphData.Stations.Select(
                    station => new DebugCurve(100, 0.1, "blue", CurveFactory.CreateCircle(station.Radius, station.Position)));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static IEnumerable<DebugCurve> DebugSegs(MetroGraphData metroGraphData, IMetroMapOrderingAlgorithm metroMapOrdering) {

            var ls = new List<ICurve>();
            foreach (var s in metroGraphData.VirtualNodes()) {
                foreach (var b in s.BundleBases.Values) {
                    foreach (var h in b.OrientedHubSegments) {
                        if (h == null) continue;
                        if (h.Segment == null) {
                            var uBase = h.Other.BundleBase;
                            var i = h.Index;
                            var j = h.Other.Index;
                            ls.Add(new LineSegment(b.Points[i], uBase.Points[j]));
                        }
                        else {
                            ls.Add(h.Segment);
                        }
                    }
                }
            }

            return ls.Select(s => new DebugCurve(100, 0.01, "green", s));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static IEnumerable<DebugCurve> GraphNodes(MetroGraphData metroGraphData) {
            var nodes =
                new Set<ICurve>(
                    metroGraphData.Edges.Select(e => e.SourcePort.Curve).Concat(
                        metroGraphData.Edges.Select(e => e.TargetPort.Curve)));
            return nodes.Select(n => new DebugCurve(40, 1, "black", n));
        }
#endif

        #endregion

        #region BiArcs
        /// <summary>
        /// Following "Biarc approximation of NURBS curves", Les A. Piegl, and Wayne Tiller. The paper has a bug in V, where they write that v=p0+p4, it is p0-p4.
        /// Also I treat special cases differently.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="ts"></param>
        /// <param name="p4"></param>
        /// <param name="te"></param>
        /// <returns></returns>
        internal static ICurve BiArc(Point p0, Point ts, Point p4, Point te) {
            Debug.Assert(ApproximateComparer.Close(ts.LengthSquared, 1));
            Debug.Assert(ApproximateComparer.Close(te.LengthSquared, 1));
            var v = p0 - p4;
            if (v.Length < ApproximateComparer.DistanceEpsilon)
                return null;

            var vtse = v * (ts - te);
            var tste = -ts * te;

            //solving a quadratic equation
            var a = 2 * (tste - 1);
            var b = 2 * vtse;
            var c = v * v;
            double al;
            if (Math.Abs(a) < ApproximateComparer.DistanceEpsilon) { //we have b*al+c=0
                if (Math.Abs(b) > ApproximateComparer.DistanceEpsilon) {
                    al = -c / b;
                }
                else {
                    return null;
                }
            }
            else {
                var d = b * b - 4 * a * c;
                Debug.Assert(d >= -ApproximateComparer.Tolerance);
                if (d < 0)
                    d = 0;
                d = Math.Sqrt(d);
                al = (-b + d) / (2 * a);
                if (al < 0)
                    al = (-b - d) / (2 * a);
            }

            var p1 = p0 + al * ts;
            var p3 = p4 + al * te;
            var p2 = 0.5 * (p1 + p3);
            var curve = new Curve();
            curve.AddSegs(ArcOn(p0, p1, p2), ArcOn(p2, p3, p4));

            //bad input for BiArc. we shouldn't allow such cases during bundle bases construction
            if (ts * (p4 - p0) <= 0 && ts * te <= 0) {
                //switch to Bezier
                var curve2 = StandardBezier(p0, ts, p4, te);
#if TEST_MSAGL
                /*List<DebugCurve> dc = new List<DebugCurve>();
                dc.Add(new DebugCurve(curve));
                dc.Add(new DebugCurve(0.3, "black", curve2));
                dc.Add(new DebugCurve(0.1, "red", new LineSegment(p0, p0 + 3 * ts)));
                dc.Add(new DebugCurve(0.1, "blue", new LineSegment(p4, p4 + 3 * te)));
                LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dc);*/
#endif
                return curve2;
            }

            return curve;
        }

        /// <summary>
        /// returns the arc that a,b,c touches
        /// </summary>
        /// <param name="a">belongs to the arc, the tangent point of ba</param>
        /// <param name="b">ba, and bc are tangents to the arc</param>
        /// <param name="c">belongs to the arc, the tangent point of bc</param>
        /// <returns></returns>
        internal static ICurve ArcOn(Point a, Point b, Point c) {
            Point center;
            if (Math.Abs(Point.SignedDoubledTriangleArea(a, b, c)) < 0.0001 || !FindArcCenter(a, b, c, out center))
                return new LineSegment(a, c);

            var radius = (a - center).Length;
            var chordLength = (a - b).Length;
            if (chordLength / radius < 0.0001)//to avoid a floating point error
                return new LineSegment(a, c);
            var cenA = a - center;
            var aAngle = Math.Atan2(cenA.Y, cenA.X);
            var cenC = c - center;
            var cAngle = Math.Atan2(cenC.Y, cenC.X);
            var delac = cAngle - aAngle;
            if (delac < 0) {
                delac += 2 * Math.PI;
                cAngle += 2 * Math.PI;
            }
            if (delac <= Math.PI) {
                //going ccw
                var el = new Ellipse(aAngle, cAngle, new Point(radius, 0), new Point(0, radius), center);
                return el;
            }

            //going clockwise
            if (cAngle > 2 * Math.PI)
                cAngle -= 2 * Math.PI;

            aAngle = Math.PI - aAngle;
            cAngle = Math.PI - cAngle;
            if (aAngle < 0)
                aAngle += 2 * Math.PI;
            while (cAngle < aAngle)
                cAngle += 2 * Math.PI;

            delac = cAngle - aAngle;

            Debug.Assert(delac <= Math.PI);

            return new Ellipse(aAngle, cAngle, new Point(-radius, 0), new Point(0, radius), center);
        }

        static bool FindArcCenter(Point a, Point b, Point c, out Point center) {
            var perp0 = (b - a).Rotate90Cw();
            var perp1 = (b - c).Rotate90Cw();
            return Point.LineLineIntersection(a, a + perp0, c, c + perp1, out center);
        }

        #endregion

        #region Bezier segments

        internal static CubicBezierSegment StandardBezier(Point segStart, Point tangentAtStart, Point segEnd, Point tangentAtEnd) {
            double len = (segStart - segEnd).Length / 4.0;
            return new CubicBezierSegment(segStart, segStart + tangentAtStart * len, segEnd + tangentAtEnd * len, segEnd);
        }

        void FanBezierSegs() {
            bool progress = true;
            const int maxSteps = 5;
            int steps = 0;
            while (progress && steps++ < maxSteps) {
                progress = false;
                foreach (Station s in metroGraphData.Stations)
                    foreach (var segmentHub in s.BundleBases.Values)
                        progress |= FanEdgesOfHubSegment(segmentHub);
            }
        }

        bool FanEdgesOfHubSegment(BundleBase bundleHub) {
            bool ret = false;
            for (int i = 0; i < bundleHub.Count - 1; i++)
                ret |= FanCouple(bundleHub, i, bundleHub.CurveCenter, bundleHub.Curve.BoundingBox.Diagonal / 2);
            return ret;
        }

        /// <summary>
        /// fans the couple i,i+1
        /// </summary>
        bool FanCouple(BundleBase bundleHub, int i, Point center, double radius) {
            OrientedHubSegment lSeg = bundleHub.OrientedHubSegments[i];
            OrientedHubSegment rSeg = bundleHub.OrientedHubSegments[i + 1];
            if (lSeg == null) return false;
            Point x;
            if (LineSegment.Intersect(lSeg.Segment.Start, lSeg.Segment.End, rSeg.Segment.Start, rSeg.Segment.End, out x))
                return false; //it doesn not make sense to push these segs apart
            if (Point.GetTriangleOrientation(lSeg[0], lSeg[0.5], lSeg[1]) !=
                Point.GetTriangleOrientation(rSeg[0], rSeg[0.5], rSeg[1]))
                return false;
            double ll = BaseLength(lSeg);
            double rl = BaseLength(rSeg);
            if (Math.Abs(ll - rl) < ApproximateComparer.IntersectionEpsilon)
                return false;
            if (ll > rl)
                return AdjustLongerSeg(lSeg, rSeg, center, radius);
            return AdjustLongerSeg(rSeg, lSeg, center, radius);
            /*
            var del0 = lSeg.Start - rSeg.Start;
            var del1 = lSeg.End - rSeg.End;
           var desiredDelta = Math.Min(del0, del1);
			var leftMiddle = lSeg[0.5];
			var rightMiddle = rSeg[0.5];
			if ((leftMiddle - rightMiddle).Length >= desiredDelta - ApproximateComparer.DistanceEpsilon)
				return false;
			var leftMiddleToCenter = (leftMiddle - BundleHub.Vertex).Length;
			var rightMiddleToCenter = (rightMiddle - BundleHub.Vertex).Length;
			if (leftMiddleToCenter > rightMiddleToCenter) {
				if (MoveSegToDesiredDistance(rightMiddle, lSeg, desiredDelta))
					return true;
			} else if (MoveSegToDesiredDistance(leftMiddle, rSeg, desiredDelta))
				return true;

			return false;*/
        }

        bool AdjustLongerSeg(OrientedHubSegment longerSeg, OrientedHubSegment shorterSeg, Point center,
                                    double radius) {
            Point del0 = longerSeg[0] - shorterSeg[0];
            Point del1 = longerSeg[1] - shorterSeg[1];
            double minDelLength = Math.Min(del0.Length, del1.Length);
            Point midPointOfShorter = shorterSeg[0.5];
            double maxDelLen = Math.Max(del0.Length, del1.Length);
            if (NicelyAligned((CubicBezierSegment)longerSeg.Segment, del0, del1, midPointOfShorter, minDelLength, maxDelLen) == 0)
                return false;
            return FitLonger(longerSeg, del0, del1, midPointOfShorter, minDelLength, maxDelLen, center, radius);
        }

        const double SqueezeBound = 0.2;

        bool FitLonger(OrientedHubSegment longerOrientedSeg, Point del0, Point del1, Point midPointOfShorter,
                              double minDelLength, double maxDel, Point center, double radius) {
            CubicBezierSegment seg = (CubicBezierSegment)longerOrientedSeg.Segment;
            Point start = seg.Start;
            Point end = seg.End;
            // LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve("green", shorterDebugOnly), new DebugCurve("red", seg));

            int steps = 0;
            const int maxSteps = 10;
            Point lowP1 = (1 - SqueezeBound) * seg.Start + SqueezeBound * seg.B(1);
            Point lowP2 = (1 - SqueezeBound) * seg.End + SqueezeBound * seg.B(2);
            Point highP1 = 2 * seg.B(1) - seg.Start;
            //originally the tangents were 0.25 of the length of seg[1]-seg[0] - so were are safe to lengthen two times
            Point highP2 = 2 * seg.B(2) - seg.End;
            PullControlPointToTheCircle(seg.Start, ref highP1, center, radius);
            int r = NicelyAligned(seg, del0, del1, midPointOfShorter, minDelLength, maxDel);
            do {
                if (r == -1) {
                    //pull the control points lower
                    Point p1 = (seg.B(1) + lowP1) / 2;
                    Point p2 = (seg.B(2) + lowP2) / 2;
                    highP1 = seg.B(1);
                    highP2 = seg.B(2);
                    seg = new CubicBezierSegment(start, p1, p2, end);
                }
                else {
                    Debug.Assert(r == 1);
                    //pull the control points higher
                    Point p1 = (seg.B(1) + highP1) / 2;
                    Point p2 = (seg.B(2) + highP2) / 2;
                    lowP1 = seg.B(1);
                    lowP2 = seg.B(2);
                    seg = new CubicBezierSegment(start, p1, p2, end);
                }


                if ((r = NicelyAligned(seg, del0, del1, midPointOfShorter, minDelLength, maxDel)) == 0) {
                    longerOrientedSeg.Other.Segment = longerOrientedSeg.Segment = seg;
                    return true;
                }
                if (steps++ > maxSteps) return false; //cannot fix it
            } while (true);
        }

        void PullControlPointToTheCircle(Point start, ref Point highP, Point center, double radius) {
            Point closestPointOnLine = Point.ProjectionToLine(start, highP, center);
            //the max offset from closestPointOnLine
            double maxOffset = Math.Sqrt(radius * radius - (closestPointOnLine - center).LengthSquared);
            Point offsetNow = highP - closestPointOnLine;
            double offsetLen = offsetNow.Length;
            if (offsetLen > maxOffset)
                highP = closestPointOnLine + maxOffset * offsetNow / offsetLen;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="longerSeg"></param>
        /// <param name="del0"></param>
        /// <param name="del1"></param>
        /// <param name="midPointOfShorter"></param>
        /// <param name="minDelLength"></param>
        /// <param name="maxDelLen"></param>
        /// <returns> 1 - need to stretch, -1 - need to squeze, 0 - OK </returns>
        int NicelyAligned(CubicBezierSegment longerSeg, Point del0, Point del1, Point midPointOfShorter,
                                 double minDelLength, double maxDelLen) {
            const double eps = 0.001;
            Point midDel = longerSeg[0.5] - midPointOfShorter;
            double midDelLen = midDel.Length;
            if (del0 * midDel < 0 || del1 * midDel < 0)
                return 1;
            if (midDelLen < minDelLength - eps)
                return 1;
            if (midDelLen > maxDelLen + eps)
                return -1;
            return 0;
        }

        double BaseLength(OrientedHubSegment seg) {
            return (seg[0] - seg[1]).LengthSquared;
        }


        #endregion
    }
}
