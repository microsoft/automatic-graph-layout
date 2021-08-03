using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Msagl.Core.Geometry.Curves {
#pragma warning disable 1587
    /// <summary>
    /// Curve: keeps a sequence of connected ICurves
    /// </summary>
#pragma warning restore 1587
#if TEST_MSAGL
    [Serializable]
#endif
#pragma warning disable 1591
    public partial class Curve : ICurve {
#pragma warning restore 1591
        ParallelogramInternalTreeNode pBNode;
        //the parameter domain is [0,n] where n is the number of segments
        readonly List<ICurve> segs;

        static bool IsCloseToLineSeg(double a, ref Point ap, double b, ref Point bp, ICurve s, double e) {
            const double x = 1.0/3;
            double p = a*x + b*(1 - x);

            Point delta = s[p] - (ap*x + bp*(1 - x));
            if (delta*delta > e)
                return false;

            p = a*(1 - x) + b*x;

            delta = s[p] - (ap*(1 - x) + bp*x);
            if (delta*delta > e)
                return false;

            p = a*0.5 + b*0.5;

            delta = s[p] - (ap*0.5 + bp*0.5);
            if (delta*delta > e)
                return false;

            return true;
        }

        /// <summary>
        /// interpolates the curve between parameters a and side1 as by a sequence of line segments
        /// </summary>
        /// <param name="startParameter">start parameter of the interpolation</param>
        /// <param name="start">start point of the interpolation</param>
        /// <param name="endParameter">end parameter of the interpolation</param>
        /// <param name="end">end point of the interpolation</param>
        /// <param name="curve">the interpolated curve</param>
        /// <param name="epsilon">the maximal allowed distance between the curve and its inerpolation</param>
        /// <returns></returns>
        public static IList<LineSegment> Interpolate(double startParameter, Point start, double endParameter, Point end,
            ICurve curve, double epsilon) {
            return Interpolate(startParameter, ref start, endParameter, ref end, curve, epsilon);
        }

        internal static List<LineSegment> Interpolate(double a, ref Point ap, double b, ref Point bp, ICurve s,
            double eps) {
            var r = new List<LineSegment>();
            if (IsCloseToLineSeg(a, ref ap, b, ref bp, s, eps))
                r.Add(new LineSegment(ap, bp));
            else {
                double m = 0.5*(a + b);
                Point mp = s[m];
                r.AddRange(Interpolate(a, ref ap, m, ref mp, s, eps));
                r.AddRange(Interpolate(m, ref mp, b, ref bp, s, eps));
            }

            return r;
        }

        /// <summary>
        /// this function always produces at least two segments
        /// </summary>
        /// <param name="eps"></param>
        /// <param name="a"></param>
        /// <param name="ap"></param>
        /// <param name="b"></param>
        /// <param name="bp"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static List<LineSegment> Interpolate(double eps, double a, ref Point ap, double b, ref Point bp,
            ICurve s) {
            double m = (a + b)/2;
            Point mp = s[m];
            List<LineSegment> ret = Interpolate(a, ref ap, m, ref mp, s, eps*eps);
            ret.AddRange(Interpolate(m, ref mp, b, ref bp, s, eps*eps));
            return ret;
        }

        internal static List<LineSegment> Interpolate(ICurve s, double eps) {
            Point start = s.Start;
            Point end = s.End;
            return Interpolate(eps, s.ParStart, ref start, s.ParEnd, ref end, s);
        }


        /// <summary>
        /// this[Reverse[t]]=this[ParEnd+ParStart-t]
        /// </summary>
        /// <returns></returns>
        public ICurve Reverse() {
            var ret = new Curve(segs.Count);
            for (int i = segs.Count - 1; i >= 0; i--)
                ret.AddSegment(segs[i].Reverse());
            return ret;
        }

        /// <summary>
        /// Constructs the curve
        /// </summary>
        public Curve() {
            segs = new List<ICurve>();
        }

        /// <summary>
        /// Constructs a curve with an initial segment capacity.
        /// </summary>
        public Curve(int segmentCapacity) {
            segs = new List<ICurve>(segmentCapacity);
        }

        internal Curve(List<ICurve> segs) {
            this.segs = segs;
            ParStart = 0;

            foreach (ICurve s in segs)
                ParEnd += s.ParEnd - s.ParStart;
        }

        /// <summary>
        /// this[ParStart]
        /// </summary>
        public Point Start {
            get { return segs[0].Start; }
        }

        /// <summary>
        /// this[ParEnd]
        /// </summary>
        public Point End {
            get { return segs[segs.Count - 1].End; }
        }

        /// <summary>
        /// Returns the trim curve
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public ICurve Trim(double start, double end) {
            AdjustStartEndEndParametersToDomain(ref start, ref end);

            int sseg;
            double spar;

            GetSegmentAndParameter(start, out spar, out sseg);

            int eseg;
            double epar;

            GetSegmentAndParameter(end, out epar, out eseg);

            if (sseg == eseg)
                return Segments[sseg].Trim(spar, epar);

            var c = new Curve(eseg-sseg+1);

            if (spar < Segments[sseg].ParEnd)
                c = c.AddSegment(Segments[sseg].Trim(spar, Segments[sseg].ParEnd));

            for (int i = sseg + 1; i < eseg; i++)
                c = c.AddSegment(Segments[i]);

            if (Segments[eseg].ParStart < epar)
                c = c.AddSegment(Segments[eseg].Trim(Segments[eseg].ParStart, epar));

            return c;
        }

       void AdjustStartEndEndParametersToDomain(ref double start, ref double end) {
            if (start > end) {
                double t = start;
                start = end;
                end = t;
            }

            if (start < ParStart)
                start = ParStart;

            if (end > ParEnd)
                end = ParEnd;
           
        }

        /// <summary>
        /// Returns the trimmed curve, wrapping around the end if start is greater than end.
        /// </summary>
        /// <param name="start">The starting parameter</param>
        /// <param name="end">The ending parameter</param>
        /// <returns>The trimmed curve</returns>
        public ICurve TrimWithWrap(double start, double end) {
            Debug.Assert(start >= ParStart && start <= ParEnd);
            Debug.Assert(end >= ParStart && end <= ParEnd);
            if (start < end)
                return Trim(start, end) as Curve;

            Debug.Assert(ApproximateComparer.Close(Start, End), "Curve must be closed to wrap");
            var c = new Curve();
            c.AddSegment(Trim(start, ParEnd) as Curve);
            c.AddSegment(Trim(ParStart, end) as Curve);
            return c;
        }

        /// <summary>
        /// Adds a segment to the curve
        /// </summary>
        /// <param name="curve">the curve that we add a segment to</param>
        /// <returns>the new curve extended with the segment</returns>
        public Curve AddSegment(ICurve curve) {
            if (curve == null)
                return this; //nothing happens
#if TEST_MSAGL
            if (segs.Count > 0 && !ApproximateComparer.Close(End, curve.Start, 0.001))
                throw new InvalidOperationException(); //discontinuous curve
#endif
            ParStart = 0;
            var c = curve as Curve;
            if (c == null) {
                segs.Add(curve);
                ParEnd += curve.ParEnd - curve.ParStart;
            }
            else {
                IncreaseSegmentCapacity(c.Segments.Count);
                foreach (ICurve cc in c.Segments) {
                    segs.Add(cc);
                    ParEnd += cc.ParEnd - cc.ParStart;
                }
            }
            return this;
        }

        /// <summary>
        /// Increases the capacity of the segment list.
        /// Call this before adding a bunch of segments to prevent the list from having to resize multiple times.
        /// </summary>
        public void IncreaseSegmentCapacity(int additionalAmount) {
            int capacityNeeded = segs.Count + additionalAmount;
            if (segs.Capacity < capacityNeeded) {
                int capacity = Math.Max(segs.Capacity, 1);
                while (capacity < capacityNeeded) capacity *= 2;
                segs.Capacity = capacity;
            }
        }

        internal Curve AddSegs(ICurve a, ICurve b) {
            Curve c = AddSegment(a);
            return c.AddSegment(b);
        }

        internal Curve AddSegs(ICurve a, ICurve b, ICurve c, ICurve d) {
            IncreaseSegmentCapacity(4);
            Curve r = AddSegment(a);
            r = r.AddSegment(b);
            r = r.AddSegment(c);
            return r.AddSegment(d);
        }
        public IList<ICurve> Segments {
            get { return segs; }
        }

        /// <summary>
        /// A tree of ParallelogramNodes covering the curve. 
        /// This tree is used in curve intersections routines.
        /// </summary>
        /// <value></value>
        public ParallelogramNodeOverICurve ParallelogramNodeOverICurve {
            get {
                lock (this) {
                    if (pBNode != null)
                        return pBNode;

                    pBNode = new ParallelogramInternalTreeNode(this, ParallelogramNodeOverICurve.DefaultLeafBoxesOffset);
                    var parallelograms = new List<Parallelogram>();
                    foreach (ICurve curveSeg in segs) {
                        ParallelogramNodeOverICurve pBoxNode = curveSeg.ParallelogramNodeOverICurve;

                        parallelograms.Add(pBoxNode.Parallelogram);

                        pBNode.AddChild(pBoxNode);
                    }
                    pBNode.Parallelogram = Parallelogram.GetParallelogramOfAGroup(parallelograms);

                    return pBNode;
                }
            }
        }

        /// <summary>
        /// finds an intersection between to curves, 
        /// </summary>
        /// <param name="curve0"></param>
        /// <param name="curve1"></param>
        /// <param name="liftIntersection">if set to true parameters of the intersection point will be given in the curve parametrization</param>
        /// <returns>InterseciontInfo or null if there are no intersections</returns>
        public static IntersectionInfo CurveCurveIntersectionOne(ICurve curve0, ICurve curve1, bool liftIntersection) {
            ValidateArg.IsNotNull(curve0, "curve0");
            ValidateArg.IsNotNull(curve1, "curve1");
            Debug.Assert(curve0 != curve1, "curve0 == curve1");
#if TEST_MSAGL
//            double c0S = curve0.ParStart, c1S = curve1.ParStart;
//            if (CurvesAreCloseAtParams(curve0, curve1, c0S, c1S)) {
//                double mc0 = 0.5 * (curve0.ParStart + curve0.ParEnd);
//                double mc1 = 0.5 * (curve1.ParStart + curve1.ParEnd);
//                double c0E = curve0.ParEnd;
//                if (CurvesAreCloseAtParams(curve0, curve1, mc0, mc1)) {
//                    double c1E = curve1.ParEnd;
//                    CurvesAreCloseAtParams(curve0, curve1, c0E, c1E);
//                    throw new InvalidOperationException();
//                }
//            }
#endif
            //recurse down to find all PBLeaf pairs which intesect and try to cross their segments

            IntersectionInfo ret = CurveCurveXWithParallelogramNodesOne(curve0.ParallelogramNodeOverICurve,
                                                                        curve1.ParallelogramNodeOverICurve);

            if (liftIntersection && ret != null)
                ret = LiftIntersectionToCurves(curve0, curve1, ret);


            return ret;
        }

        /// <summary>
        /// calculates all intersections between curve0 and curve1
        /// </summary>
        /// <param name="curve0"></param>
        /// <param name="curve1"></param>
        /// <param name="liftIntersections">if set to true parameters of the intersection point will be given in the curve parametrization</param>
        /// <returns></returns>
        public static IList<IntersectionInfo> GetAllIntersections(ICurve curve0, ICurve curve1, bool liftIntersections) {
            ValidateArg.IsNotNull(curve0, "curve0");
            ValidateArg.IsNotNull(curve1, "curve1");
            Debug.Assert(curve0 != curve1);
#if TEST_MSAGL
//            var c0S = curve0.ParStart;
//            var c1S = curve1.ParStart;
//            var c0E = curve0.ParEnd;
//            var c1E = curve1.ParEnd;
//            if (CurvesAreCloseAtParams(curve0, curve1, c0S, c1S)) {
//                if (CurvesAreCloseAtParams(curve0, curve1, c0E, c1E)) {
//                    var mc0 = 0.5*(curve0.ParStart + curve0.ParEnd);
//                    var mc1 = 0.5*(curve1.ParStart + curve1.ParEnd);
//                    if (CurvesAreCloseAtParams(curve0, curve1, mc0, mc1))
//                        throw new InvalidOperationException();
//                }
//            }
#endif

            var lineSeg = curve0 as LineSegment;
            if (lineSeg != null)
                return GetAllIntersectionsOfLineAndICurve(lineSeg, curve1, liftIntersections);

            return GetAllIntersectionsInternal(curve0, curve1, liftIntersections);
        }


        internal static IList<IntersectionInfo> GetAllIntersectionsInternal(ICurve curve0, ICurve curve1,
            bool liftIntersections) {
            //recurse down to find all PBLeaf pairs which intesect and try to cross their segments

            var intersections = new List<IntersectionInfo>();
            CurveCurveXWithParallelogramNodes(curve0.ParallelogramNodeOverICurve, curve1.ParallelogramNodeOverICurve,
                                              ref intersections);

            if (liftIntersections)
                for (int i = 0; i < intersections.Count; i++)
                    intersections[i] = LiftIntersectionToCurves(curve0, curve1, intersections[i]);


            //fix the parameters - adjust them to the curve
            return intersections;
        }

        static IList<IntersectionInfo> GetAllIntersectionsOfLineAndICurve(LineSegment lineSeg, ICurve iCurve,
            bool liftIntersections) {
            var poly = iCurve as Polyline;
            if (poly != null)
                return GetAllIntersectionsOfLineAndPolyline(lineSeg, poly);

            var curve = iCurve as Curve;
            if (curve != null)
                return GetAllIntersectionsOfLineAndCurve(lineSeg, curve, liftIntersections);

            var roundedRect = iCurve as RoundedRect;
            if (roundedRect != null)
                return GetAllIntersectionsOfLineAndRoundedRect(lineSeg, roundedRect, liftIntersections);

            var ellipse = iCurve as Ellipse;
            if (ellipse != null && ellipse.IsArc())
                return GetAllIntersectionsOfLineAndArc(lineSeg, ellipse);

            return GetAllIntersectionsInternal(lineSeg, iCurve, liftIntersections);
        }

        static IList<IntersectionInfo> GetAllIntersectionsOfLineAndRoundedRect(LineSegment lineSeg, RoundedRect roundedRect, bool liftIntersections) {
            var ret = GetAllIntersectionsOfLineAndCurve(lineSeg, roundedRect.Curve, liftIntersections);
            if(liftIntersections)
                foreach (var intersectionInfo in ret) {
                    intersectionInfo.Segment1 = roundedRect;
                }
            return ret;
        }

        static IList<IntersectionInfo> GetAllIntersectionsOfLineAndCurve(LineSegment lineSeg, Curve curve, bool liftIntersections) {
            var ret = new List<IntersectionInfo>();
            var lineParallelogram = lineSeg.ParallelogramNodeOverICurve;
            var curveParallelogramRoot = curve.ParallelogramNodeOverICurve;
            if (Parallelogram.Intersect(lineParallelogram.Parallelogram, curveParallelogramRoot.Parallelogram) == false)
                return ret;
            var parOffset = 0.0;
            foreach (var seg in curve.Segments) {
                var iiList = GetAllIntersections(lineSeg, seg, false);
                if(liftIntersections) {
                    foreach (var intersectionInfo in iiList) {
                        intersectionInfo.Par1 += parOffset-seg.ParStart;
                        intersectionInfo.Segment1 = curve;
                    }
                    parOffset += seg.ParEnd - seg.ParStart;
                }
                foreach (var intersectionInfo in iiList) {
                    if(! AlreadyInside(ret, intersectionInfo))
                        ret.Add(intersectionInfo);
                }
            }

            return ret;
        }

        static bool AlreadyInside(List<IntersectionInfo> ret, IntersectionInfo intersectionInfo) {
            for(int i=0;i<ret.Count;i++) {
                var ii = ret[i];
                if (ApproximateComparer.CloseIntersections(ii.IntersectionPoint, intersectionInfo.IntersectionPoint))
                    return true;
            }
            return false;
        }

        static IList<IntersectionInfo> GetAllIntersectionsOfLineAndArc(LineSegment lineSeg, Ellipse ellipse) {
            Point lineDir = lineSeg.End - lineSeg.Start;
            var ret = new List<IntersectionInfo>();
            double segLength = lineDir.Length;
            if (segLength < ApproximateComparer.DistanceEpsilon) {
                if (ApproximateComparer.Close((lineSeg.Start - ellipse.Center).Length, ellipse.AxisA.Length)) {
                    double angle = Point.Angle(ellipse.AxisA, lineSeg.Start - ellipse.Center);
                    if (ellipse.ParStart - ApproximateComparer.Tolerance <= angle) {
                        angle = Math.Max(angle, ellipse.ParStart);
                        if (angle <= ellipse.ParEnd + ApproximateComparer.Tolerance) {
                            angle = Math.Min(ellipse.ParEnd, angle);
                            ret.Add(new IntersectionInfo(0, angle, lineSeg.Start, lineSeg, ellipse));
                        }
                    }
                }
                return ret;
            }

            Point perp = lineDir.Rotate90Ccw()/segLength;
            double segProjection = (lineSeg.Start - ellipse.Center)*perp;
            Point closestPointOnLine = ellipse.Center + perp*segProjection;

            double rad = ellipse.AxisA.Length;
            double absSegProj = Math.Abs(segProjection);
            if (rad < absSegProj - ApproximateComparer.DistanceEpsilon)
                return ret; //we don't have an intersection
            lineDir = perp.Rotate90Cw();
            if (ApproximateComparer.Close(rad, absSegProj))
                TryToAddPointToLineCircleCrossing(lineSeg, ellipse, ret, closestPointOnLine, segLength, lineDir);
            else {
                Debug.Assert(rad > absSegProj);
                double otherLeg = Math.Sqrt(rad*rad - segProjection*segProjection);
                TryToAddPointToLineCircleCrossing(lineSeg, ellipse, ret, closestPointOnLine + otherLeg*lineDir,
                                                  segLength, lineDir);
                TryToAddPointToLineCircleCrossing(lineSeg, ellipse, ret, closestPointOnLine - otherLeg*lineDir,
                                                  segLength, lineDir);
            }

            return ret;
        }

        static void TryToAddPointToLineCircleCrossing(LineSegment lineSeg,
            Ellipse ellipse, List<IntersectionInfo> ret, Point point, double segLength, Point lineDir) {
            Point ds = point - lineSeg.Start;
            Point de = point - lineSeg.End;
            double t = ds*lineDir;
            if (t < -ApproximateComparer.DistanceEpsilon)
                return;
            t = Math.Max(t, 0);
            if (t > segLength + ApproximateComparer.DistanceEpsilon)
                return;
            t = Math.Min(t, segLength);
            t /= segLength;

            double angle = Point.Angle(ellipse.AxisA, point - ellipse.Center);
            if (ellipse.ParStart - ApproximateComparer.Tolerance <= angle) {
                angle = Math.Max(angle, ellipse.ParStart);
                if (angle <= ellipse.ParEnd + ApproximateComparer.Tolerance) {
                    angle = Math.Min(ellipse.ParEnd, angle);
                    ret.Add(new IntersectionInfo(t, angle, point, lineSeg, ellipse));
                }
            }
        }

        static IList<IntersectionInfo> GetAllIntersectionsOfLineAndPolyline(LineSegment lineSeg, Polyline poly) {
            var ret = new List<IntersectionInfo>();
            double offset = 0.0;
            double par0, par1;
            Point x;
            PolylinePoint polyPoint = poly.StartPoint;
            for (; polyPoint != null && polyPoint.Next != null; polyPoint = polyPoint.Next) {
                if (CrossTwoLineSegs(lineSeg.Start, lineSeg.End, polyPoint.Point, polyPoint.Next.Point, 0, 1, 0, 1,
                                     out par0, out par1, out x)) {
                    AdjustSolution(lineSeg.Start, lineSeg.End, polyPoint.Point, polyPoint.Next.Point, ref par0, ref par1,
                                   ref x);
                    if (!OldIntersection(ret, ref x))
                        ret.Add(new IntersectionInfo(par0, offset + par1, x, lineSeg, poly));
                }
                offset++;
            }
            if (poly.Closed)
                if (CrossTwoLineSegs(lineSeg.Start, lineSeg.End, polyPoint.Point, poly.Start, 0, 1, 0, 1, out par0,
                                     out par1, out x)) {
                    AdjustSolution(lineSeg.Start, lineSeg.End, polyPoint.Point, poly.Start, ref par0, ref par1, ref x);
                    if (!OldIntersection(ret, ref x))
                        ret.Add(new IntersectionInfo(par0, offset + par1, x, lineSeg, poly));
                }

            return ret;
        }

        static void AdjustSolution(Point aStart, Point aEnd, Point bStart, Point bEnd, ref double par0, ref double par1,
            ref Point x) {
            //adjust the intersection if it is close to the ends of the segs
            if (ApproximateComparer.CloseIntersections(x, aStart)) {
                x = aStart;
                par0 = 0;
            }
            else if (ApproximateComparer.CloseIntersections(x, aEnd)) {
                x = aEnd;
                par0 = 1;
            }

            if (ApproximateComparer.CloseIntersections(x, bStart)) {
                x = bStart;
                par1 = Math.Floor(par1);
            }
            else if (ApproximateComparer.CloseIntersections(x, bEnd)) {
                x = bEnd;
                par1 = Math.Ceiling(par1);
            }
        }

        static IntersectionInfo CurveCurveXWithParallelogramNodesOne(ParallelogramNodeOverICurve n0,
            ParallelogramNodeOverICurve n1) {
            if (!Parallelogram.Intersect(n0.Parallelogram, n1.Parallelogram))
                // Boxes n0.Box and n1.Box do not intersect
                return null;
            var n0Pb = n0 as ParallelogramInternalTreeNode;
            var n1Pb = n1 as ParallelogramInternalTreeNode;
            if (n0Pb != null && n1Pb != null)
                foreach (ParallelogramNodeOverICurve n00 in n0Pb.Children)
                    foreach (ParallelogramNodeOverICurve n11 in n1Pb.Children) {
                        IntersectionInfo x = CurveCurveXWithParallelogramNodesOne(n00, n11);
                        if (x != null) return x;
                    }
            else if (n1Pb != null)
                foreach (ParallelogramNodeOverICurve n in n1Pb.Children) {
                    IntersectionInfo x = CurveCurveXWithParallelogramNodesOne(n0, n);
                    if (x != null) return x;
                }
            else if (n0Pb != null)
                foreach (ParallelogramNodeOverICurve n in n0Pb.Children) {
                    IntersectionInfo x = CurveCurveXWithParallelogramNodesOne(n, n1);
                    if (x != null) return x;
                }
            else
                return CrossOverIntervalsOne(n0, n1);

            return null;
        }


        static void CurveCurveXWithParallelogramNodes(ParallelogramNodeOverICurve n0, ParallelogramNodeOverICurve n1,
            ref List<IntersectionInfo> intersections) {
            if (!Parallelogram.Intersect(n0.Parallelogram, n1.Parallelogram))
                // Boxes n0.Box and n1.Box do not intersect
                return;
            var n0Pb = n0 as ParallelogramInternalTreeNode;
            var n1Pb = n1 as ParallelogramInternalTreeNode;
            if (n0Pb != null && n1Pb != null)
                foreach (ParallelogramNodeOverICurve n00 in n0Pb.Children)
                    foreach (ParallelogramNodeOverICurve n11 in n1Pb.Children)
                        CurveCurveXWithParallelogramNodes(n00, n11, ref intersections);
            else if (n1Pb != null)
                foreach (ParallelogramNodeOverICurve n in n1Pb.Children)
                    CurveCurveXWithParallelogramNodes(n0, n, ref intersections);
            else if (n0Pb != null)
                foreach (ParallelogramNodeOverICurve n in n0Pb.Children)
                    CurveCurveXWithParallelogramNodes(n, n1, ref intersections);
            else intersections = CrossOverIntervals(n0, n1, intersections);
        }

        static IntersectionInfo CrossOverIntervalsOne(ParallelogramNodeOverICurve n0, ParallelogramNodeOverICurve n1) {
            //both are leafs 
            var l0 = n0 as ParallelogramLeaf;
            var l1 = n1 as ParallelogramLeaf;
            double d0 = (l0.High - l0.Low)/2;
            double d1 = (l1.High - l1.Low)/2;

            for (int i = 1; i < 2; i++) {
                double p0 = i*d0 + l0.Low;
                for (int j = 1; j < 2; j++) {
                    double p1 = j*d1 + l1.Low;
                    double aSol, bSol;
                    Point x;
                    bool r;
                    if (l0.Chord == null && l1.Chord == null)
                        r = CrossWithinIntervalsWithGuess(n0.Seg, n1.Seg, l0.Low, l0.High, l1.Low, l1.High,
                                                          p0, p1, out aSol, out bSol, out x);
                    else if (l0.Chord != null && l1.Chord == null) {
                        r = CrossWithinIntervalsWithGuess(l0.Chord, n1.Seg, 0, 1, l1.Low, l1.High,
                                                          0.5*i,
                                                          p1, out aSol, out bSol, out x);
                        if (r)
                            aSol = l0.Low + aSol*(l0.High - l0.Low);
                    }
                    else if (l0.Chord == null) {
                        r = CrossWithinIntervalsWithGuess(n0.Seg, l1.Chord,
                                                          l0.Low, l0.High, 0, 1, p0,
                                                          0.5*j, out aSol, out bSol, out x);
                        if (r)
                            bSol = l1.Low + bSol*(l1.High - l1.Low);
                    }
                    else //if (l0.Chord != null && l1.Chord != null)
                    {
                        r = CrossWithinIntervalsWithGuess(l0.Chord, l1.Chord,
                                                          0, 1, 0, 1, 0.5*i,
                                                          0.5*j, out aSol, out bSol, out x);
                        if (r) {
                            bSol = l1.Low + bSol*(l1.High - l1.Low);
                            aSol = l0.Low + aSol*(l0.High - l0.Low);
                        }
                    }

                    if (r)
                        return CreateIntersectionOne(l0, l1, aSol, bSol, x);
                }
            }

            return GoDeeperOne(l0, l1);
        }

        static List<IntersectionInfo> CrossOverIntervals(ParallelogramNodeOverICurve n0, ParallelogramNodeOverICurve n1,
            List<IntersectionInfo> intersections) {
            //both are leafs 
            var l0 = n0 as ParallelogramLeaf;
            var l1 = n1 as ParallelogramLeaf;
            double d0 = (l0.High - l0.Low)/2;
            double d1 = (l1.High - l1.Low)/2;
            bool found = false;

            for (int i = 1; i < 2; i++) {
                double p0 = i*d0 + l0.Low;
                for (int j = 1; j < 2; j++) {
                    double p1 = j*d1 + l1.Low;


                    double aSol, bSol;
                    Point x;


                    bool r;
                    if (l0.Chord == null && l1.Chord == null)
                        r = CrossWithinIntervalsWithGuess(n0.Seg, n1.Seg, l0.Low, l0.High, l1.Low, l1.High,
                                                          p0, p1, out aSol, out bSol, out x);
                    else if (l0.Chord != null && l1.Chord == null) {
                        r = CrossWithinIntervalsWithGuess(l0.Chord, n1.Seg, 0, 1, l1.Low, l1.High,
                                                          0.5*i,
                                                          p1, out aSol, out bSol, out x);
                        if (r)
                            aSol = l0.Low + aSol*(l0.High - l0.Low);
                    }
                    else if (l0.Chord == null) {
//&& l1.Chord != null) 
                        r = CrossWithinIntervalsWithGuess(n0.Seg, l1.Chord,
                                                          l0.Low, l0.High, 0, 1, p0,
                                                          0.5*j, out aSol, out bSol, out x);
                        if (r)
                            bSol = l1.Low + bSol*(l1.High - l1.Low);
                    }
                    else //if (l0.Chord != null && l1.Chord != null)
                    {
                        r = CrossWithinIntervalsWithGuess(l0.Chord, l1.Chord,
                                                          0, 1, 0, 1, 0.5*i,
                                                          0.5*j, out aSol, out bSol, out x);
                        if (r) {
                            bSol = l1.Low + bSol*(l1.High - l1.Low);
                            aSol = l0.Low + aSol*(l0.High - l0.Low);
                        }
                    }

                    if (r) {
                        AddIntersection(l0, l1, intersections, aSol, bSol, x);
                        found = true;
                    }
                }
            }

            if (!found)
                GoDeeper(ref intersections, l0, l1);
            return intersections;
        }

        static void AddIntersection(ParallelogramLeaf n0, ParallelogramLeaf n1, List<IntersectionInfo> intersections,
            double aSol, double bSol,
            Point x) {
            //adjust the intersection if it is close to the ends of the segs
            if (ApproximateComparer.CloseIntersections(x, n0.Seg[n0.Low])) {
                x = n0.Seg[n0.Low];
                aSol = n0.Low;
            }
            else if (ApproximateComparer.CloseIntersections(x, n0.Seg[n0.High])) {
                x = n0.Seg[n0.High];
                aSol = n0.High;
            }

            if (ApproximateComparer.CloseIntersections(x, n1.Seg[n1.Low])) {
                x = n1.Seg[n1.Low];
                bSol = n1.Low;
            }
            else if (ApproximateComparer.CloseIntersections(x, n1.Seg[n1.High])) {
                x = n1.Seg[n1.High];
                bSol = n1.High;
            }

            bool oldIntersection = OldIntersection(intersections, ref x);
            if (!oldIntersection) {
                var xx = new IntersectionInfo(aSol, bSol, x, n0.Seg, n1.Seg);
                intersections.Add(xx);
            }

            return;
        }

        /// <summary>
        /// returns true if the intersection exists already
        /// </summary>
        /// <param name="intersections"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        static bool OldIntersection(List<IntersectionInfo> intersections, ref Point x) {
            bool oldIntersection = false;
            //we don't expect many intersections so it's ok just go through all of them
            foreach (IntersectionInfo ii in intersections)
                if ((x - ii.IntersectionPoint).Length < ApproximateComparer.DistanceEpsilon*100)
                    //please no close intersections
                {
                    oldIntersection = true;
                    break;
                }
            return oldIntersection;
        }

        static IntersectionInfo CreateIntersectionOne(ParallelogramLeaf n0, ParallelogramLeaf n1,
            double aSol, double bSol, Point x) {
            //adjust the intersection if it is close to the ends of the segs
            if (ApproximateComparer.CloseIntersections(x, n0.Seg[n0.Low])) {
                x = n0.Seg[n0.Low];
                aSol = n0.Low;
            }
            else if (ApproximateComparer.CloseIntersections(x, n0.Seg[n0.High])) {
                x = n0.Seg[n0.High];
                aSol = n0.High;
            }

            if (ApproximateComparer.CloseIntersections(x, n1.Seg[n1.Low])) {
                x = n1.Seg[n1.Low];
                bSol = n1.Low;
            }
            else if (ApproximateComparer.CloseIntersections(x, n1.Seg[n1.High])) {
                x = n1.Seg[n1.High];
                bSol = n1.High;
            }

            return new IntersectionInfo(aSol, bSol, x, n0.Seg, n1.Seg);
        }

        static IntersectionInfo LiftIntersectionToCurves(
            ICurve c0, ICurve c1, double aSol,
            double bSol, Point x, ICurve seg0, ICurve seg1) {
            double a = LiftParameterToCurve(c0, aSol - seg0.ParStart, seg0);
            double b = LiftParameterToCurve(c1, bSol - seg1.ParStart, seg1);

            return new IntersectionInfo(a, b, x, c0, c1);
        }

        static IntersectionInfo DropIntersectionToSegs(IntersectionInfo xx) {
            ICurve seg0;
            double par0;

            if (xx.Segment0 is Curve)
                (xx.Segment0 as Curve).GetSegmentAndParameter(xx.Par0, out par0, out seg0);
            else {
                par0 = xx.Par0;
                seg0 = xx.Segment0;
            }

            ICurve seg1;
            double par1;

            if (xx.Segment1 is Curve)
                (xx.Segment1 as Curve).GetSegmentAndParameter(xx.Par1, out par1, out seg1);
            else {
                par1 = xx.Par1;
                seg1 = xx.Segment1;
            }

            return new IntersectionInfo(par0, par1, xx.IntersectionPoint, seg0, seg1);
        }

        internal static IntersectionInfo LiftIntersectionToCurves(ICurve c0, ICurve c1, IntersectionInfo xx) {
            return LiftIntersectionToCurves(c0, c1, xx.Par0, xx.Par1, xx.IntersectionPoint, xx.Segment0, xx.Segment1);
        }

        internal static double LiftParameterToCurve(ICurve curve, double par, ICurve seg) {
            if (curve == seg)
                return par;

            var c = curve as Curve;

            if (c != null) {
                double offset = 0;
                foreach (ICurve s in c.Segments) {
                    if (s == seg)
                        return par + offset;
                    offset += ParamSpan(s);
                }
            }
            var roundedRect = curve as RoundedRect;
            if (roundedRect != null)
                return LiftParameterToCurve(roundedRect.Curve, par, seg);

            throw new InvalidOperationException(); //"bug in LiftParameterToCurve");
        }

        static double ParamSpan(ICurve s) {
            return s.ParEnd - s.ParStart;
        }

        static IntersectionInfo GoDeeperOne(ParallelogramLeaf l0, ParallelogramLeaf l1) {
            double eps = ApproximateComparer.DistanceEpsilon;
            // did not find an intersection
            if (l0.LeafBoxesOffset > eps && l1.LeafBoxesOffset > eps) {
                // going deeper on both with offset l0.LeafBoxesOffset / 2, l1.LeafBoxesOffset / 2
                ParallelogramNodeOverICurve nn0 = ParallelogramNodeOverICurve.CreateParallelogramNodeForCurveSeg(
                    l0.Low, l0.High, l0.Seg, l0.LeafBoxesOffset/2);
                ParallelogramNodeOverICurve nn1 = ParallelogramNodeOverICurve.CreateParallelogramNodeForCurveSeg(
                    l1.Low, l1.High, l1.Seg, l1.LeafBoxesOffset/2);
                return CurveCurveXWithParallelogramNodesOne(nn0, nn1);
            }
            if (l0.LeafBoxesOffset > eps) {
                // go deeper on the left
                ParallelogramNodeOverICurve nn0 = ParallelogramNodeOverICurve.CreateParallelogramNodeForCurveSeg(
                    l0.Low, l0.High, l0.Seg, l0.LeafBoxesOffset/2);
                return CurveCurveXWithParallelogramNodesOne(nn0, l1);
            }
            if (l1.LeafBoxesOffset > eps) {
                // go deeper on the right
                ParallelogramNodeOverICurve nn1 = ParallelogramNodeOverICurve.CreateParallelogramNodeForCurveSeg(
                    l1.Low, l1.High, l1.Seg, l1.LeafBoxesOffset/2);
                return CurveCurveXWithParallelogramNodesOne(l0, nn1);
            }
            //just cross LineSegs and adjust the solutions if the segments are not straight lines
            Point l0Low = l0.Seg[l0.Low];
            Point l0High = l0.Seg[l0.High];
            if (!ApproximateComparer.Close(l0Low, l0High)) {
                Point l1Low = l1.Seg[l1.Low];
                Point l1High = l1.Seg[l1.High];
                if (!ApproximateComparer.Close(l1Low, l1High)) {
                    LineSegment ls0 = l0.Seg is LineSegment ? l0.Seg as LineSegment : new LineSegment(l0Low, l0High);
                    LineSegment ls1 = l1.Seg is LineSegment ? l1.Seg as LineSegment : new LineSegment(l1Low, l1High);

                    double asol, bsol;
                    Point x;
                    bool r = CrossWithinIntervalsWithGuess(ls0, ls1, 0, 1, 0, 1, 0.5, 0.5, out asol, out bsol, out x);
                    if (r) {
                        AdjustParameters(l0, ls0, l1, ls1, x, ref asol, ref bsol);
                        return CreateIntersectionOne(l0, l1, asol, bsol, x);
                    }
                }
            }
            return null;
        }

        static void GoDeeper(ref List<IntersectionInfo> intersections, ParallelogramLeaf l0, ParallelogramLeaf l1) {
            double eps = ApproximateComparer.DistanceEpsilon;
            // did not find an intersection
            if (l0.LeafBoxesOffset > eps && l1.LeafBoxesOffset > eps) {
                // going deeper on both with offset l0.LeafBoxesOffset / 2, l1.LeafBoxesOffset / 2
                ParallelogramNodeOverICurve nn0 = ParallelogramNodeOverICurve.CreateParallelogramNodeForCurveSeg(
                    l0.Low, l0.High, l0.Seg, l0.LeafBoxesOffset/2);
                ParallelogramNodeOverICurve nn1 = ParallelogramNodeOverICurve.CreateParallelogramNodeForCurveSeg(
                    l1.Low, l1.High, l1.Seg, l1.LeafBoxesOffset/2);
                CurveCurveXWithParallelogramNodes(nn0, nn1, ref intersections);
            }
            else if (l0.LeafBoxesOffset > eps) {
                // go deeper on the left
                ParallelogramNodeOverICurve nn0 = ParallelogramNodeOverICurve.CreateParallelogramNodeForCurveSeg(
                    l0.Low, l0.High, l0.Seg, l0.LeafBoxesOffset/2);
                CurveCurveXWithParallelogramNodes(nn0, l1, ref intersections);
            }
            else if (l1.LeafBoxesOffset > eps) {
                // go deeper on the right
                ParallelogramNodeOverICurve nn1 = ParallelogramNodeOverICurve.CreateParallelogramNodeForCurveSeg(
                    l1.Low, l1.High, l1.Seg, l1.LeafBoxesOffset/2);
                CurveCurveXWithParallelogramNodes(l0, nn1, ref intersections);
            }
            else {
                //just cross LineSegs since the polylogramms are so thin
                Point l0Low = l0.Seg[l0.Low];
                Point l0High = l0.Seg[l0.High];
                if (!ApproximateComparer.Close(l0Low, l0High)) {
                    Point l1Low = l1.Seg[l1.Low];
                    Point l1High = l1.Seg[l1.High];
                    if (!ApproximateComparer.Close(l1Low, l1High)) {
                        LineSegment ls0 = l0.Seg is LineSegment ? l0.Seg as LineSegment : new LineSegment(l0Low, l0High);
                        LineSegment ls1 = l1.Seg is LineSegment ? l1.Seg as LineSegment : new LineSegment(l1Low, l1High);

                        double asol, bsol;
                        Point x;
                        bool r = CrossWithinIntervalsWithGuess(ls0, ls1, 0, 1, 0, 1, 0.5, 0.5, out asol, out bsol, out x);
                        if (r) {
                            AdjustParameters(l0, ls0, l1, ls1, x, ref asol, ref bsol);
                            AddIntersection(l0, l1, intersections, asol, bsol, x);
                        }
                    }
                }
            }
        }


        static void AdjustParameters(ParallelogramLeaf l0, LineSegment ls0, ParallelogramLeaf l1, LineSegment ls1,
            Point x, ref double asol, ref double bsol) {
            if (ls0 != l0.Seg && l0.Seg is Polyline == false) //l0.Seg is not a LineSegment and not a polyline
                asol = l0.Seg.ClosestParameter(x); //we need to find the correct parameter
            else
                asol = l0.Low + asol*(l0.High - l0.Low);
            if (ls1 != l1.Seg && l1.Seg is Polyline == false) //l1.Seg is not a LineSegment and not a polyline
                bsol = l1.Seg.ClosestParameter(x); //we need to find the correct parameter
            else
                bsol = l1.Low + bsol*(l1.High - l1.Low);
        }

        static double lineSegThreshold = 0.05;

        /// <summary>
        /// The distance between the start and end point of a curve segment for which we consider the segment as a line segment
        /// </summary>
        public static double LineSegmentThreshold {
            get { return lineSegThreshold; }
            set { lineSegThreshold = value; }
        }

        /// <summary>
        /// returns the segment correspoinding to t and the segment parameter
        /// </summary>
        /// <param name="t"></param>
        /// <param name="par"></param>
        /// <param name="segment"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "t"),
         SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#"),
         SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public void GetSegmentAndParameter(double t, out double par, out ICurve segment) {
            double u = ParStart; //u is the sum of param domains
            foreach (ICurve sg in segs) {
                double domLen = sg.ParEnd - sg.ParStart;
                if (t >= u && t <= u + domLen) {
                    par = t - u + sg.ParStart;
                    segment = sg;
                    return;
                }
                u += domLen;
            }
            segment = segs.Last();
            par = segment.ParEnd;
        }

        internal void GetSegmentAndParameter(double t, out double par, out int segIndex) {
            double u = 0; //u is the sum of param domains
            segIndex = 0;
            par = -1;

            for (int i = 0; i < segs.Count; i++)
            {
                var sg = segs[i];
                double domLen = sg.ParEnd - sg.ParStart;
                if (t >= u && (t <= u + domLen || (i == segs.Count-1 && ApproximateComparer.Compare(t,u+domLen)<=0) ))
                {
                    par = t - u + sg.ParStart;
                    return;
                }
                segIndex++;
                u += domLen;
            }

            throw new InvalidOperationException(string.Format("Check, args t:{0}, par:{1}, segIndex:{2} and u:{3}", t, par, segIndex, u));
        }


        /// <summary>
        /// Returns the point on the curve corresponding to parameter t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point this[double t] {
            get {
                double par;
                ICurve seg;

                GetSegmentAndParameter(t, out par, out seg);

                return seg[par];
            }
        }

        /// <summary>
        /// first derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point Derivative(double t) {
            double par;
            ICurve seg;

            GetSegmentAndParameter(t, out par, out seg);
            return seg.Derivative(par);
        }

        /// <summary>
        /// second derivative
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point SecondDerivative(double t) {
            double par;
            ICurve seg;

            GetSegmentAndParameter(t, out par, out seg);
            return seg.SecondDerivative(par);
        }

        /// <summary>
        /// third derivative
        /// </summary>
        /// <param name="t">the parameter of the derivative</param>
        /// <returns></returns>
        public Point ThirdDerivative(double t) {
            double par;
            ICurve seg;

            GetSegmentAndParameter(t, out par, out seg);
            return seg.ThirdDerivative(par);
        }

        // For curves A(s) and B(t), if you have some evidence that 
        //  there is at most one intersection point, and you have some guess for the parameters (s0, t0)...
        // You are trying to bring to (0,0) the vector( F^2 s , F^2 t ).
        //F(s,t) = A(s) - B(t).  To minimize F^2,
        //You get the system of equations to solve for ds and dt: 
        //F*Fs + (F*Fss + Fs*Fs)ds + (F*Fst + Fs*Ft)dt = 0
        //F*Ft + (F*Fst + Fs*Ft)ds + (F*Ftt + Ft*Ft)dt = 0
        // 
        //Where F = F(si,ti), Fs and Ft are the first partials at si, ti, Fxx are the second partials, 
        //    and s(i+1) = si+ds, t(i+1) = ti+dt. 
        //Of course you have to make sure that ds and dt do not take you out of your domain.  This will converge if the curves have 2nd order continuity and your starting parameters are reasonable.  It is not a good method for situations that are not well behaved, but it is really simple.

        internal static bool CrossWithinIntervalsWithGuess(
            ICurve a, ICurve b,
            double amin, double amax,
            double bmin, double bmax,
            double aGuess,
            double bGuess,
            out double aSolution,
            out double bSolution, out Point x) {
            bool r;
            if (a is LineSegment && b is LineSegment)
                if (CrossTwoLineSegs(a.Start, a.End, b.Start, b.End, amin, amax, bmin, bmax, out aSolution,
                                     out bSolution, out x))
                    return true;
            //it also handles the case of almost parallel segments
            Point aPoint;
            Point bPoint;
            r = MinDistWithinIntervals(a,
                                       b,
                                       amin,
                                       amax, bmin,
                                       bmax,
                                       aGuess, bGuess,
                                       out aSolution,
                                       out bSolution, out aPoint, out bPoint);


            x = 0.5*(aPoint + bPoint);
            Point aMinusB = aPoint - bPoint;
            //if side1 is  false tnen the values a and side1 are meaningless
            bool ret = r && aMinusB*aMinusB < ApproximateComparer.DistanceEpsilon*ApproximateComparer.DistanceEpsilon;
            return ret;
        }

        static bool CrossTwoLineSegs(Point aStart, Point aEnd, Point bStart, Point bEnd, double amin, double amax,
            double bmin, double bmax, out double aSolution, out double bSolution, out Point x) {
            Point u = aEnd - aStart;
            Point v = bStart - bEnd;
            Point w = bStart - aStart;
            bool r = LinearSystem2.Solve(u.X, v.X, w.X, u.Y, v.Y, w.Y, out aSolution, out bSolution);
            x = aStart + aSolution*u;

            if (r) {
                if (aSolution < amin - ApproximateComparer.Tolerance)
                    return false;

                aSolution = Math.Max(aSolution, amin);

                if (aSolution > amax + ApproximateComparer.Tolerance)
                    return false;

                aSolution = Math.Min(aSolution, amax);

                if (bSolution < bmin - ApproximateComparer.Tolerance)
                    return false;

                bSolution = Math.Max(bSolution, bmin);

                if (bSolution > bmax + ApproximateComparer.Tolerance)
                    return false;

                bSolution = Math.Min(bSolution, bmax);

                //  if(!ApproximateComparer.Close(x,B[bSolution]))
                //  throw new InvalidOperationException();// ("segs");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Decides if the point lies inside, outside or on the curve
        /// </summary>
        /// <param name="point">the point under test</param>
        /// <param name="curve">a closed curve</param>
        /// <returns>the point position characteristic</returns>
        public static PointLocation PointRelativeToCurveLocation(Point point, ICurve curve) {
              System.Diagnostics.Debug.Assert(!Double.IsNaN(point.X) && !Double.IsNaN(point.Y));
            ValidateArg.IsNotNull(curve, "curve");
            if (!curve.BoundingBox.Contains(point))
                return PointLocation.Outside;

            double l = 2*curve.BoundingBox.Diagonal; //l should be big enough for the line to exit outside of the curve

            const double degree = Math.PI/180.0;
            int inside = 0;
            for (int i = 13; i < 360; i += 13) {
                var lineDir = new Point(Math.Cos(i*degree), Math.Sin(i*degree));
                var ls = new LineSegment(point, point + l*lineDir);

                IList<IntersectionInfo> intersections = GetAllIntersections(ls, curve, true);


                //SugiyamaLayoutSettings.Show(ls, curve);
                // CurveSerializer.Serialize("cornerC:\\tmp\\ls",ls);
                // CurveSerializer.Serialize("cornerC:\\tmp\\pol",curve);
                if (AllIntersectionsAreGood(intersections, curve)) {
                    foreach (IntersectionInfo xx in intersections)
                        if (ApproximateComparer.Close(xx.IntersectionPoint, point))
                            return PointLocation.Boundary;
                    bool insideThisTime = intersections.Count%2 == 1;
                    //to be on the safe side we need to get the same result at least twice
                    if (insideThisTime)
                        inside++;
                    else
                        inside--;

                    if (inside >= 2)
                        return PointLocation.Inside;
                    if (inside <= -2)
                        return PointLocation.Outside;
                }
            }
            //if all intersections are not good then we probably have the point on the boundaryCurve

            return PointLocation.Boundary;
        }


        static bool AllIntersectionsAreGood(IList<IntersectionInfo> intersections, ICurve polygon) {
            var curve = polygon as Curve;
            if (curve == null) {
                // If this isn't a Curve, try a Polyline.
                var polyLine = polygon as Polyline;
                if (polyLine != null)
                    curve = polyLine.ToCurve();
            }
            if (curve != null)
                foreach (IntersectionInfo xx in intersections)
                    if (!RealCut(DropIntersectionToSegs(xx), curve, false))
                        return false;
            return true;
        }


        /// <summary>
        /// Returns true if curves do not touch in the intersection point
        /// </summary>
        /// <param name="xx"></param>
        /// <param name="polygon"></param>
        /// <param name="onlyFromInsideCuts">if set to true and first curve is closed will return true 
        /// only when the second curve cuts the first one from the inside</param>
        /// <returns></returns>
        public static bool RealCutWithClosedCurve(IntersectionInfo xx, Curve polygon, bool onlyFromInsideCuts) {
            ValidateArg.IsNotNull(xx, "xx");
            ValidateArg.IsNotNull(polygon, "polygon");
            ICurve sseg = xx.Segment0;
            ICurve pseg = xx.Segment1;
            double spar = xx.Par0;
            double ppar = xx.Par1;
            Point x = xx.IntersectionPoint;

            //normalised tangent to spline
            Point ts = sseg.Derivative(spar).Normalize();
            Point pn = pseg.Derivative(ppar).Normalize().Rotate(Math.PI/2);

            if (ApproximateComparer.Close(x, pseg.End)) {
                //so pseg enters the spline 
                ICurve exitSeg = null;
                for (int i = 0; i < polygon.Segments.Count; i++)
                    if (polygon.Segments[i] == pseg) {
                        exitSeg = polygon.Segments[(i + 1)%polygon.Segments.Count];
                        break;
                    }

                if (exitSeg == null)
                    throw new InvalidOperationException(); //"exitSeg==null");

                Point tsn = ts.Rotate((Math.PI/2));

                bool touch = (tsn*pseg.Derivative(pseg.ParEnd))*(tsn*exitSeg.Derivative(exitSeg.ParStart)) <
                             ApproximateComparer.Tolerance;

                return !touch;
            }

            if (ApproximateComparer.Close(x, pseg.Start)) {
                //so pseg exits the spline 
                ICurve enterSeg = null;
                for (int i = 0; i < polygon.Segments.Count; i++)
                    if (polygon.Segments[i] == pseg) {
                        enterSeg = polygon.Segments[i > 0 ? (i - 1) : polygon.Segments.Count - 1];
                        break;
                    }

                Point tsn = ts.Rotate((Math.PI/2));
                bool touch = (tsn*pseg.Derivative(pseg.ParStart))*
                             (tsn*enterSeg.Derivative(enterSeg.ParEnd)) < ApproximateComparer.Tolerance;

                return !touch;
            }

            double d = ts*pn;
            if (onlyFromInsideCuts)
                return d > ApproximateComparer.DistanceEpsilon;
            return Math.Abs(d) > ApproximateComparer.DistanceEpsilon;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xx"></param>
        /// <param name="polyline"></param>
        /// <param name="onlyFromInsideCuts">consider a cut good only if the segment cuts the polygon from inside</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "polyline")]
        public static bool RealCut(IntersectionInfo xx, Curve polyline, bool onlyFromInsideCuts) {
            ValidateArg.IsNotNull(xx, "xx");
            ValidateArg.IsNotNull(polyline, "polyline");
            ICurve sseg = xx.Segment0;
            ICurve pseg = xx.Segment1;
            double spar = xx.Par0;
            double ppar = xx.Par1;
            Point x = xx.IntersectionPoint;


            //normalised tangent to spline
            Point ts = sseg.Derivative(spar).Normalize();
            Point pn = pseg.Derivative(ppar).Normalize().Rotate(Math.PI/2);

            if (ApproximateComparer.Close(x, pseg.End)) {
                //so pseg enters the spline 
                ICurve exitSeg = null;
                for (int i = 0; i < polyline.Segments.Count - 1; i++)
                    if (polyline.Segments[i] == pseg) {
                        exitSeg = polyline.Segments[i + 1];
                        break;
                    }

                if (exitSeg == null)
                    return false; //hit the end of the polyline

                Point tsn = ts.Rotate((Math.PI/2));

                bool touch = (tsn*pseg.Derivative(pseg.ParEnd))*(tsn*exitSeg.Derivative(exitSeg.ParStart)) <
                             ApproximateComparer.Tolerance;

                return !touch;
            }

            if (ApproximateComparer.Close(x, pseg.Start)) {
                //so pseg exits the spline 
                ICurve enterSeg = null;
                for (int i = polyline.segs.Count - 1; i > 0; i--)
                    if (polyline.Segments[i] == pseg) {
                        enterSeg = polyline.Segments[i - 1];
                        break;
                    }
                if (enterSeg == null)
                    return false;
                Point tsn = ts.Rotate((Math.PI/2));
                bool touch = (tsn*pseg.Derivative(pseg.ParStart))*
                             (tsn*enterSeg.Derivative(enterSeg.ParEnd)) < ApproximateComparer.Tolerance;

                return !touch;
            }

            double d = ts*pn;
            if (onlyFromInsideCuts)
                return d > ApproximateComparer.DistanceEpsilon;
            return Math.Abs(d) > ApproximateComparer.DistanceEpsilon;
        }

        internal static bool MinDistWithinIntervals(
            ICurve a, ICurve b, double aMin, double aMax, double bMin, double bMax,
            double aGuess, double bGuess, out double aSolution, out double bSolution, out Point aPoint, out Point bPoint) {
            var md = new MinDistCurveCurve(a, b, aMin, aMax, bMin, bMax, aGuess, bGuess);
            md.Solve();
            aSolution = md.ASolution;
            aPoint = md.APoint;
            bSolution = md.BSolution;
            bPoint = md.BPoint;

            return md.Status;
        }

#if DEBUGCURVES
    public override string ToString()
    {
      bool poly = true;
      foreach (ICurve s in segs)
        if (s is LineSeg == false)
        {
          poly = false;
          break;
        }

      string ret;
      if (!poly)
      {
         ret = "{";

        foreach (ICurve seg in Segs)
        {
          ret += seg + ",";
        }


        return ret + "}";
      }
      ret = "{";
      if (segs.Count > 0)
        ret += segs[0].Start.X.ToString() + "," + segs[0].Start.Y.ToString()+" ";
      foreach(LineSeg s in segs)
        ret += s.End.X.ToString() + "," + s.End.Y.ToString() + " ";
      return ret + "}";
    }
#endif

        /// <summary>
        /// Offsets the curve in the direction of dir
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public ICurve OffsetCurve(double offset, Point dir) {
            return null;
        }

        /// <summary>
        /// The bounding rectangle of the curve
        /// </summary>
        public Rectangle BoundingBox {
            get {
                if (Segments.Count == 0)
                    return new Rectangle(0, 0, -1, -1);
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=369 there are no structs in js
                Rectangle b = Segments[0].BoundingBox.Clone();
#else
                Rectangle b = Segments[0].BoundingBox;
#endif
                for (int i = 1; i < Segments.Count; i++)
                    b.Add(Segments[i].BoundingBox);

                return b;
            }
        }

        #region ICurve Members

        /// <summary>
        /// clones the curve. 
        /// </summary>
        /// <returns>the cloned curve</returns>
        public ICurve Clone() {
            var c = new Curve(Segments.Count);
            foreach (ICurve seg in Segments)
                c.AddSegment(seg.Clone());
            return c;
        }

        #endregion

        #region ICurve Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public double GetParameterAtLength(double length) {
            var parSpan = 0.0;
            foreach (var seg in Segments) {
                var segL = seg.Length;
                if (segL >= length)
                    return parSpan+seg.GetParameterAtLength(length);

                length -= segL;
                parSpan += seg.ParEnd - seg.ParStart;
            }
            return ParEnd;
        }

        /// <summary>
        /// returns a parameter t such that the distance between curve[t] and targetPoint is minimal 
        /// and t belongs to the closed segment [low,high]
        /// </summary>
        /// <param name="targetPoint">the point to find the closest point</param>
        /// <param name="high">the upper bound of the parameter</param>
        /// <param name="low">the low bound of the parameter</param>
        /// <returns></returns>
        public double ClosestParameterWithinBounds(Point targetPoint, double low, double high) {
            double par = 0;
            double dist = Double.MaxValue;
            double offset = 0;
            foreach (ICurve seg in Segments) {
                if (offset > high)
                    break; //we are out of the [low, high] segment
                double segParamSpan = ParamSpan(seg);
                double segEnd = offset + segParamSpan;
                if (segEnd >= low) {
                    //we are in business
                    double segLow = Math.Max(seg.ParStart, seg.ParStart + (low - offset));
                    double segHigh = Math.Min(seg.ParEnd, seg.ParStart + (high - offset));
                    Debug.Assert(segHigh >= segLow);
                    double t = seg.ClosestParameterWithinBounds(targetPoint, segLow, segHigh);
                    Point d = targetPoint - seg[t];
                    double dd = d*d;
                    if (dd < dist) {
                        par = offset + t - seg.ParStart;
                        dist = dd;
                    }
                }
                offset += segParamSpan;
            }
            return par;
        }

        /// <summary>
        /// returns a parameter t such that the distance between curve[t] and a is minimal
        /// </summary>
        /// <param name="targetPoint"></param>
        /// <returns></returns>
        public double ClosestParameter(Point targetPoint) {
            double par = 0;
            double dist = Double.MaxValue;
            double offset = 0;
            foreach (ICurve c in Segments) {
                double t = c.ClosestParameter(targetPoint);
                Point d = targetPoint - c[t];
                double dd = d*d;
                if (dd < dist) {
                    par = offset + t - c.ParStart;
                    dist = dd;
                }
                offset += ParamSpan(c);
            }
            return par;
        }

        #endregion

        #region Curve concatenations

        ///<summary>
        ///adds a line segment to the curve
        ///</summary>
        ///<param name="curve"></param>
        ///<param name="pointA"></param>
        ///<param name="pointB"></param>
        ///<returns></returns>
        public static Curve AddLineSegment(Curve curve, Point pointA, Point pointB) {
            ValidateArg.IsNotNull(curve, "curve");
            return curve.AddSegment(new LineSegment(pointA, pointB));
        }


        //static internal void AddLineSegment(Curve c, double x, double y, Point b) {
        //    AddLineSegment(c, new Point(x, y), b);
        //}

        /// <summary>
        /// adds a line segment to the curve
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"),
         SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y")]
        public static void AddLineSegment(Curve curve, double x0, double y0, double x1, double y1) {
            AddLineSegment(curve, new Point(x0, y0), new Point(x1, y1));
        }

        /// <summary>
        /// adds a line segment to the curve
        /// </summary>
        /// <param name="c"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y"),
         SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"),
         SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "c")]
        public static void ContinueWithLineSegment(Curve c, double x, double y) {
            ValidateArg.IsNotNull(c, "c");
            AddLineSegment(c, c.End, new Point(x, y));
        }

        /// <summary>
        /// adds a line segment to the curve
        /// </summary>
        /// <param name="c"></param>
        /// <param name="x"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"),
         SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "c"),
         SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Seg")]
        public static void ContinueWithLineSegment(Curve c, Point x) {
            ValidateArg.IsNotNull(c, "c");
            AddLineSegment(c, c.End, x);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="curve"></param>
        public static void CloseCurve(Curve curve) {
            ValidateArg.IsNotNull(curve, "curve");
            ContinueWithLineSegment(curve, curve.Start);
        }

        #endregion

        /// <summary>
        /// left derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point LeftDerivative(double t) {
            ICurve seg = TryToGetLeftSegment(t);
            if (seg != null)
                return seg.Derivative(seg.ParEnd);
            return Derivative(t);
        }

        /// <summary>
        /// right derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point RightDerivative(double t) {
            ICurve seg = TryToGetRightSegment(t);
            if (seg != null)
                return seg.Derivative(seg.ParStart);
            return Derivative(t);
        }


        ICurve TryToGetLeftSegment(double t) {
            if (Math.Abs(t - ParStart) < ApproximateComparer.Tolerance) {
                if (Start == End)
                    return Segments[Segments.Count - 1];
                return null;
            }
            foreach (ICurve seg in Segments) {
                t -= ParamSpan(seg);
                if (Math.Abs(t) < ApproximateComparer.Tolerance)
                    return seg;
            }
            return null;
        }

        ICurve TryToGetRightSegment(double t) {
            if (Math.Abs(t - ParEnd) < ApproximateComparer.Tolerance) {
                if (Start == End)
                    return Segments[0];
                return null;
            }

            foreach (ICurve seg in Segments) {
                if (Math.Abs(t) < ApproximateComparer.Tolerance)
                    return seg;

                t -= ParamSpan(seg);
            }
            return null;
        }

        /// <summary>
        /// gets the closest point together with its parameter
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="location"></param>
        /// <param name="pointOnCurve"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        public static double ClosestParameterWithPoint(ICurve curve, Point location, out Point pointOnCurve) {
            ValidateArg.IsNotNull(curve, "curve");
            double t = curve.ClosestParameter(location);
            pointOnCurve = curve[t];
            return t;
        }

        /// <summary>
        /// gets the point on the curve that is closest to the given point
        /// </summary>
        /// <param name="curve">the curve to examine</param>
        /// <param name="location">the target point</param>
        /// <returns>the closest point</returns>
        public static Point ClosestPoint(ICurve curve, Point location) {
            ValidateArg.IsNotNull(curve, "curve");
            return curve[curve.ClosestParameter(location)];
        }

        /// <summary>
        /// Tests whether the first curve is inside the second.
        /// We suppose that the curves are convex and they are 
        /// not degenerated into a point
        /// </summary>
        /// <param name="innerCurve"></param>
        /// <param name="outerCurve"></param>
        /// <returns></returns>
        public static bool CurveIsInsideOther(ICurve innerCurve, ICurve outerCurve) {
            ValidateArg.IsNotNull(innerCurve, "innerCurve");
            ValidateArg.IsNotNull(outerCurve, "outerCurve");
            if (!outerCurve.BoundingBox.Contains(innerCurve.BoundingBox)) return false;
            IList<IntersectionInfo> xx = GetAllIntersections(innerCurve, outerCurve, true);
            if (xx.Count == 0) return NonIntersectingCurveIsInsideOther(innerCurve, outerCurve);
            if (xx.Count == 1) //it has to be a touch
                return innerCurve.Start != xx[0].IntersectionPoint
                           ? PointRelativeToCurveLocation(innerCurve.Start, outerCurve) == PointLocation.Inside
                           : PointRelativeToCurveLocation(innerCurve[(innerCurve.ParStart + innerCurve.ParEnd)/2],
                                                          outerCurve) == PointLocation.Inside;
            return
                PointsBetweenIntersections(innerCurve, xx).All(
                    p => PointRelativeToCurveLocation(p, outerCurve) != PointLocation.Outside);
        }

        // Return points between but not including the intersections.
        internal static IEnumerable<Point> PointsBetweenIntersections(ICurve a, IList<IntersectionInfo> xx) {
            xx.OrderBy(x => x.Par0);
            for (int i = 0; i < xx.Count - 1; i++)
                yield return a[(xx[i].Par0 + xx[i + 1].Par0)/2];
            //take care of the last interval
            double start = xx[xx.Count - 1].Par0;
            double end = xx[0].Par0;
            double len = a.ParEnd - start + end - a.ParStart;
            double middle = start + len/2;
            if (middle > a.ParEnd)
                middle = a.ParStart + middle - a.ParEnd;
            yield return a[middle];
        }

        static bool NonIntersectingCurveIsInsideOther(ICurve a, ICurve b) {
            ValidateArg.IsNotNull(a, "a");
            ValidateArg.IsNotNull(b, "b");
            // Due to rounding, even curves with 0 intersections may return Boundary.
            for (double par = a.ParStart; par < a.ParEnd; par += 0.5) {
                // continue as long as we have boundary points.
                PointLocation parLoc = PointRelativeToCurveLocation(a[par], b);
                if (PointLocation.Boundary != parLoc) return PointLocation.Inside == parLoc;
            }

            // All points so far were on border so it is not considered inside; test the End.
            return PointLocation.Outside != PointRelativeToCurveLocation(a.End, b);
        }

        /// <summary>
        /// Tests whether the interiors of two closed convex curves intersect
        /// </summary>
        /// <param name="curve1">convex closed curve</param>
        /// <param name="curve2">convex closed curve</param>
        /// <returns></returns>
        public static bool ClosedCurveInteriorsIntersect(ICurve curve1, ICurve curve2) {
            ValidateArg.IsNotNull(curve1, "curve1");
            ValidateArg.IsNotNull(curve2, "curve2");
            if (!curve2.BoundingBox.Intersects(curve1.BoundingBox))
                return false;
            IList<IntersectionInfo> xx = GetAllIntersections(curve1, curve2, true);
            if (xx.Count == 0)
                return NonIntersectingCurveIsInsideOther(curve1, curve2) ||
                       NonIntersectingCurveIsInsideOther(curve2, curve1);
            if (xx.Count == 1) //it is a touch
                return curve1.Start != xx[0].IntersectionPoint
                           ? PointRelativeToCurveLocation(curve1.Start, curve2) == PointLocation.Inside
                           : PointRelativeToCurveLocation(curve1[(curve1.ParStart + curve1.ParEnd)/2], curve2) ==
                             PointLocation.Inside ||
                             curve2.Start != xx[0].IntersectionPoint
                                 ? PointRelativeToCurveLocation(curve2.Start, curve1) == PointLocation.Inside
                                 : PointRelativeToCurveLocation(curve2[(curve2.ParStart + curve2.ParEnd)/2], curve1) ==
                                   PointLocation.Inside;
            return
                PointsBetweenIntersections(curve1, xx).Any(
                    p => PointRelativeToCurveLocation(p, curve2) == PointLocation.Inside);
        }

        #region ICurve Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double Curvature(double t) {
            ICurve seg;
            double par;
            GetSegmentAndParameter(t, out par, out seg);
            return seg.Curvature(par);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double CurvatureDerivative(double t) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double CurvatureSecondDerivative(double t) {
            throw new NotImplementedException();
        }

        #endregion

        ///<summary>
        ///</summary>
        ///<returns>True if the curves intersect each other.</returns>
        public static bool CurvesIntersect(ICurve curve1, ICurve curve2) {
            return curve1 == curve2 || (CurveCurveIntersectionOne(curve1, curve2, false) != null);
        }

        internal static CubicBezierSegment CreateBezierSeg(double kPrev, double kNext, Site a, Site b, Site c) {
            Point s = kPrev*a.Point + (1 - kPrev)*b.Point;
            Point e = kNext*c.Point + (1 - kNext)*b.Point;
            Point t = (2.0/3.0)*b.Point;
            return new CubicBezierSegment(s, s/3.0 + t, t + e/3.0, e);
        }

        internal static CubicBezierSegment CreateBezierSeg(Point a, Point b, Point perp, int i) {
            Point d = perp*i;
            return new CubicBezierSegment(a, a + d, b + d, b);
        }

        internal static bool FindCorner(Site a, out Site b, out Site c) {
            c = null; // to silence the compiler
            b = a.Next;
            if (b.Next == null)
                return false; //no corner has been found
            c = b.Next;
            return c != null;
        }

        internal static ICurve TrimEdgeSplineWithNodeBoundaries(ICurve sourceBoundary,
            ICurve targetBoundary, ICurve spline,
            bool narrowestInterval) {
            
            var start = spline.ParStart;
            var end = spline.ParEnd;
            if (sourceBoundary != null)
                FindNewStart(spline, ref start, sourceBoundary, narrowestInterval);
            if (targetBoundary != null)
                FindNewEnd(spline, targetBoundary, narrowestInterval, ref end);

            double st = Math.Min(start, end);
            double en = Math.Max(start, end);
            return st < en ? spline.Trim(st, en) : spline;
        }

        static void FindNewEnd(ICurve spline, ICurve targetBoundary, bool narrowestInterval, ref double end) {
            //SugiyamaLayoutSettings.Show(c, spline);
            IList<IntersectionInfo> intersections = GetAllIntersections(spline, targetBoundary, true);
            if (intersections.Count == 0) {
                end = spline.ParEnd;
                return;
            }
            if (narrowestInterval) {
                end = spline.ParEnd;
                foreach (IntersectionInfo xx in intersections)
                    if (xx.Par0 < end)
                        end = xx.Par0;
            }
            else {
                //looking for the last intersection
                end = spline.ParStart;
                foreach (IntersectionInfo xx in intersections)
                    if (xx.Par0 > end)
                        end = xx.Par0;
            }
        }

        static void FindNewStart(ICurve spline, ref double start, ICurve sourceBoundary, bool narrowestInterval) {
            IList<IntersectionInfo> intersections = GetAllIntersections(spline, sourceBoundary, true);
            if (intersections.Count == 0) {
                start = spline.ParStart;
                return;
            }
            if (narrowestInterval) {
                start = spline.ParStart;
                foreach (IntersectionInfo xx in intersections)
                    if (xx.Par0 > start)
                        start = xx.Par0;
            }
            else {
                start = spline.ParEnd;
                foreach (IntersectionInfo xx in intersections)
                    if (xx.Par0 < start)
                        start = xx.Par0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static Polyline PolylineAroundClosedCurve(ICurve curve) {
            Polyline ret;
            var ellipse = curve as Ellipse;
            if (ellipse != null)
                ret = RefineEllipse(ellipse);
            else {
                var poly = curve as Polyline;
                if (poly != null)
                    return poly;
                var c = curve as Curve;
                if (c != null && AllSegsAreLines(c)) {
                    ret = new Polyline();
                    foreach (LineSegment ls in c.Segments)
                        ret.AddPoint(ls.Start);
                    ret.Closed = true;
                    if (!ret.IsClockwise())
                        ret = (Polyline) ret.Reverse();
                }
                else
                    ret = StandardRectBoundary(curve);
            }
            return ret;
        }

        static bool AllSegsAreLines(Curve c) {
            foreach (ICurve s in c.Segments)
                if (!(s is LineSegment))
                    return false;
            return true;
        }

        /// <summary>
        /// this code only works for the standard ellipse
        /// </summary>
        /// <param name="ellipse"></param>
        /// <returns></returns>
        static Polyline RefineEllipse(Ellipse ellipse) {
            Polyline rect = StandardRectBoundary(ellipse);
            var dict = new SortedDictionary<double, Point>();
            double a = Math.PI/4;
            double w = ellipse.BoundingBox.Width;
            double h = ellipse.BoundingBox.Height;
            double l = Math.Sqrt(w*w + h*h);
            for (int i = 0; i < 4; i++) {
                double t = a + i*Math.PI/2; // parameter
                Point p = ellipse[t]; //point on the ellipse
                Point tan = l*(ellipse.Derivative(t).Normalize()); //make it long enough

                var ls = new LineSegment(p - tan, p + tan);
                foreach (IntersectionInfo ix in GetAllIntersections(rect, ls, true))
                    dict[ix.Par0] = ix.IntersectionPoint;
            }

            Debug.Assert(dict.Count > 0);
            return new Polyline(dict.Values) {Closed = true};
        }

        internal static Polyline StandardRectBoundary(ICurve curve) {
            Rectangle bbox = curve.BoundingBox;
            return bbox.Perimeter();
        }

        /// <summary>
        /// Create a closed Polyline from a rectangle
        /// </summary>
        /// <returns></returns>
        public static Polyline PolyFromBox(Rectangle rectangle) {
            var p = new Polyline();
            p.AddPoint(rectangle.LeftTop);
            p.AddPoint(rectangle.RightTop);
            p.AddPoint(rectangle.RightBottom);
            p.AddPoint(rectangle.LeftBottom);
            p.Closed = true;
            return p;
        }
    }
}