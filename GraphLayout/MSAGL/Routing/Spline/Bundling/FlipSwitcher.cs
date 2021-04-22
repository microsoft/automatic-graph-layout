using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
#if TEST_MSAGL
using Microsoft.Msagl.DebugHelpers;
#endif

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    internal class FlipSwitcher {
        readonly MetroGraphData metroGraphData;

        Dictionary<Polyline, EdgeGeometry> polylineToEdgeGeom = new Dictionary<Polyline, EdgeGeometry>();
        Dictionary<Point, Set<PolylinePoint>> pathsThroughPoints = new Dictionary<Point, Set<PolylinePoint>>();
        Set<Point> interestingPoints = new Set<Point>();
        int numberOfReducedCrossings;

        IEnumerable<Polyline> Polylines {
            get { return polylineToEdgeGeom.Keys; }
        }

        internal FlipSwitcher(MetroGraphData metroGraphData) {
            this.metroGraphData = metroGraphData;
        }

        internal void Run() {
            //TimeMeasurer.DebugOutput("switching flips...");
            Init();
            SwitchFlips();
        }

        void Init() {
            foreach (EdgeGeometry e in metroGraphData.Edges)
                polylineToEdgeGeom[(Polyline)e.Curve] = e;

            foreach (Polyline poly in Polylines)
                RegisterPolylinePointInPathsThrough(poly.PolylinePoints);
        }

        void RegisterPolylinePointInPathsThrough(IEnumerable<PolylinePoint> points) {
            foreach (var pp in points)
                RegisterPolylinePointInPathsThrough(pp);
        }

        void RegisterPolylinePointInPathsThrough(PolylinePoint pp) {
            CollectionUtilities.AddToMap(pathsThroughPoints, pp.Point, pp);
        }

        void UnregisterPolylinePointInPathsThrough(IEnumerable<PolylinePoint> points) {
            foreach (var pp in points)
                UnregisterPolylinePointInPathsThrough(pp);
        }

        void UnregisterPolylinePointInPathsThrough(PolylinePoint pp) {
            CollectionUtilities.RemoveFromMap(pathsThroughPoints, pp.Point, pp);
        }

        void SwitchFlips() {
            var queued = new Set<Polyline>(Polylines);
            var queue = new Queue<Polyline>();
            foreach (Polyline e in Polylines)
                queue.Enqueue(e);
            while (queue.Count > 0) {
                Polyline initialPolyline = queue.Dequeue();
                queued.Remove(initialPolyline);
                Polyline changedPolyline = ProcessPolyline(initialPolyline);
                if (changedPolyline != null) {
                    //we changed both polylines
                    if (!queued.Contains(initialPolyline)) {
                        queued.Insert(initialPolyline);
                        queue.Enqueue(initialPolyline);
                    }
                    if (!queued.Contains(changedPolyline)) {
                        queued.Insert(changedPolyline);
                        queue.Enqueue(changedPolyline);
                    }
                }
            }
        }

        Polyline ProcessPolyline(Polyline polyline) {
            var departed = new Dictionary<Polyline, PolylinePoint>();
            for (PolylinePoint pp = polyline.StartPoint.Next; pp != null; pp = pp.Next) {
                FillDepartedPolylinePoints(pp, departed);

                //find returning
                foreach (PolylinePoint polyPoint in pathsThroughPoints[pp.Point]) {
                    if (departed.ContainsKey(polyPoint.Polyline)) {
                        if (ProcessFlip(polyline, polyPoint.Polyline, departed[polyPoint.Polyline].Point, pp.Point))
                            return polyPoint.Polyline;
                        departed.Remove(polyPoint.Polyline);
                    }
                }
            }

            return null;
        }

        void FillDepartedPolylinePoints(PolylinePoint pp, Dictionary<Polyline, PolylinePoint> departed) {
            Point prevPoint = pp.Prev.Point;
            foreach (PolylinePoint polyPoint in pathsThroughPoints[prevPoint]) {
                if (!IsNeighbor(polyPoint, pp)) {
                    Debug.Assert(!departed.ContainsKey(polyPoint.Polyline));
                    departed[polyPoint.Polyline] = polyPoint;
                }
            }
        }

        bool ProcessFlip(Polyline polylineA, Polyline polylineB, Point flipStart, Point flipEnd) {
            //temporary switching polylines of the same width only
            //need to check capacities here
            if (polylineToEdgeGeom[polylineA].LineWidth != polylineToEdgeGeom[polylineB].LineWidth) return false;
            PolylinePoint aFirst, aLast, bFirst, bLast;
            bool forwardOrderA, forwardOrderB;
            FindPointsOnPolyline(polylineA, flipStart, flipEnd, out aFirst, out aLast, out forwardOrderA);
            FindPointsOnPolyline(polylineB, flipStart, flipEnd, out bFirst, out bLast, out forwardOrderB);
            Debug.Assert(PolylinePointsAreInForwardOrder(aFirst, aLast) == forwardOrderA);
            Debug.Assert(PolylinePointsAreInForwardOrder(bFirst, bLast) == forwardOrderB);

            //0 - the end
            //1 - not intersect
            //2 - intersect
            int rel1 = FindRelationOnFirstPoint(aFirst, bFirst, forwardOrderA, forwardOrderB);
            int rel2 = FindRelationOnLastPoint(aLast, bLast, forwardOrderA, forwardOrderB);

            //no intersection on both sides
            if (rel1 != 2 && rel2 != 2) return false;
            //can't swap to reduce crossings
            if (rel1 == 1 || rel2 == 1) return false;

            //unregister
            UnregisterPolylinePointInPathsThrough(polylineA.PolylinePoints);
            UnregisterPolylinePointInPathsThrough(polylineB.PolylinePoints);

            //switching
            Swap(aFirst, bFirst, aLast, bLast, forwardOrderA, forwardOrderB);

            //register back
            RegisterPolylinePointInPathsThrough(polylineA.PolylinePoints);
            RegisterPolylinePointInPathsThrough(polylineB.PolylinePoints);

            RegisterInterestingPoint(aFirst.Point);
            RegisterInterestingPoint(aLast.Point);
            numberOfReducedCrossings++;

            /*dc = new List<DebugCurve>();
            Polyline pl = new Polyline(polylineA);
            pl.Shift(new Point(1, 0));
            dc.Add(new DebugCurve("blue", pl));
            dc.Add(new DebugCurve("red", polylineB));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dc);*/

            return true;
        }

        void FindPointsOnPolyline(Polyline polyline, Point first, Point last,
            out PolylinePoint ppFirst, out PolylinePoint ppLast, out bool forwardOrder) {
            ppFirst = ppLast = null;
            forwardOrder = false;
            for (PolylinePoint pp = polyline.StartPoint; pp != null; pp = pp.Next) {
                if (pp.Point == first) ppFirst = pp;
                if (pp.Point == last) ppLast = pp;
                if (ppFirst != null && ppLast == null) forwardOrder = true;
                if (ppFirst == null && ppLast != null) forwardOrder = false;
            }
            Debug.Assert(ppFirst != null && ppLast != null);
        }

        bool PolylinePointsAreInForwardOrder(PolylinePoint u, PolylinePoint v) {
            Debug.Assert(u.Polyline == v.Polyline);
            for (PolylinePoint p = u; p != null; p = p.Next)
                if (p == v) return true;
            return false;
        }

        PolylinePoint Next(PolylinePoint p, bool forwardOrder) {
            return forwardOrder ? p.Next : p.Prev;
        }

        PolylinePoint Prev(PolylinePoint p, bool forwardOrder) {
            return forwardOrder ? p.Prev : p.Next;
        }

        int FindRelationOnFirstPoint(PolylinePoint aFirst, PolylinePoint bFirst, bool forwardOrderA, bool forwardOrderB) {
            Debug.Assert(aFirst.Point == bFirst.Point);

            PolylinePoint a0 = aFirst;
            PolylinePoint b0 = bFirst;
            while (true) {
                PolylinePoint prevA = Prev(aFirst, forwardOrderA);
                PolylinePoint prevB = Prev(bFirst, forwardOrderB);

                if (prevA == null || prevB == null) {
                    Debug.Assert(prevA == null && prevB == null);
                    return 0;
                }

                if (prevA.Point != prevB.Point) break;

                aFirst = prevA;
                bFirst = prevB;
            }

            return PolylinesIntersect(a0, b0, aFirst, bFirst, forwardOrderA, forwardOrderB);
        }

        int FindRelationOnLastPoint(PolylinePoint aLast, PolylinePoint bLast, bool forwardOrderA, bool forwardOrderB) {
            Debug.Assert(aLast.Point == bLast.Point);

            PolylinePoint a0 = aLast;
            PolylinePoint b0 = bLast;
            while (true) {
                PolylinePoint nextA = Next(aLast, forwardOrderA);
                PolylinePoint nextB = Next(bLast, forwardOrderB);

                if (nextA == null || nextB == null) {
                    Debug.Assert(nextA == null && nextB == null);
                    return 0;
                }

                if (nextA.Point != nextB.Point) break;

                aLast = nextA;
                bLast = nextB;
            }

            while (Next(aLast, forwardOrderA).Point == Prev(bLast, forwardOrderB).Point) {
                aLast = Next(aLast, forwardOrderA);
                bLast = Prev(bLast, forwardOrderB);
            }

            return PolylinesIntersect(aLast, bLast, a0, b0, forwardOrderA, forwardOrderB);
        }

        int PolylinesIntersect(PolylinePoint a0, PolylinePoint b0, PolylinePoint a1, PolylinePoint b1, bool forwardOrderA, bool forwardOrderB) {
            PolylinePoint a0p = Prev(a0, forwardOrderA);
            PolylinePoint a0n = Next(a0, forwardOrderA);
            PolylinePoint a1n = Next(a1, forwardOrderA);
            PolylinePoint a1p = Prev(a1, forwardOrderA);
            PolylinePoint b0n = Next(b0, forwardOrderB);
            PolylinePoint b1p = Prev(b1, forwardOrderB);

            if (a0.Point == a1.Point) {
                Point bs = a0.Point;
                int left0 = Point.GetOrientationOf3Vectors(a1p.Point - bs, b1p.Point - bs, a0n.Point - bs);
                int left1 = Point.GetOrientationOf3Vectors(a1p.Point - bs, b0n.Point - bs, a0n.Point - bs);
                /*
                if (left0 == 0 || left1 ==0) {
                    List<DebugCurve> dc = new List<DebugCurve>();
                    Polyline pl = new Polyline(a0.Polyline);
                    Point sh = new Point(3, 0);
                    pl.Shift(sh);
                    dc.Add(new DebugCurve(100,1,"blue", a0.Polyline));
                    dc.Add(new DebugCurve(100,1,"black", b0.Polyline));
                    
                    dc.Add(new DebugCurve("blue", CurveFactory.CreateCircle(3, bs)));

                    dc.Add(new DebugCurve(100,0.5, "blue", new LineSegment(a0p.Point, bs)));
                    dc.Add(new DebugCurve("red", CurveFactory.CreateCircle(5, b0.Point)));
                    dc.Add(new DebugCurve("red", CurveFactory.CreateCircle(10, b1.Point)));
                    LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dc);
                } */
                Debug.Assert(left0 != 0 && left1 != 0);
                return left0 == left1 ? 1 : 2;
            }
            else {
                int left0 = Point.GetOrientationOf3Vectors(a0p.Point - a0.Point, a0n.Point - a0.Point, b0n.Point - a0.Point);
                int left1 = Point.GetOrientationOf3Vectors(a1n.Point - a1.Point, b1p.Point - a1.Point, a1p.Point - a1.Point);
                Debug.Assert(left0 != 0 && left1 != 0);
                return left0 == left1 ? 1 : 2;
            }
        }


        void Swap(PolylinePoint aFirst, PolylinePoint bFirst, PolylinePoint aLast, PolylinePoint bLast, bool forwardOrderA, bool forwardOrderB) {
            List<PolylinePoint> intermediateAPoints = GetRangeOnPolyline(Next(aFirst, forwardOrderA), aLast, forwardOrderA);
            List<PolylinePoint> intermediateBPoints = GetRangeOnPolyline(Next(bFirst, forwardOrderB), bLast, forwardOrderB);

            //changing a
            ChangePolylineSegment(aFirst, aLast, forwardOrderA, intermediateBPoints);

            //changing b
            ChangePolylineSegment(bFirst, bLast, forwardOrderB, intermediateAPoints);

            //resulting polylines might have cycles
            PathFixer.RemoveSelfCyclesFromPolyline(aFirst.Polyline);
            Debug.Assert(PolylineIsOK(aFirst.Polyline));

            PathFixer.RemoveSelfCyclesFromPolyline(bFirst.Polyline);
            Debug.Assert(PolylineIsOK(bFirst.Polyline));
        }

        void ChangePolylineSegment(PolylinePoint aFirst, PolylinePoint aLast, bool forwardOrderA, List<PolylinePoint> intermediateBPoints) {
            PolylinePoint curA = aFirst;
            foreach (PolylinePoint b in intermediateBPoints) {
                var newp = new PolylinePoint(b.Point) { Polyline = curA.Polyline };
                if (forwardOrderA) {
                    newp.Prev = curA;
                    curA.Next = newp;
                }
                else {
                    newp.Next = curA;
                    curA.Prev = newp;
                }
                curA = newp;
            }
            if (forwardOrderA) {
                curA.Next = aLast;
                aLast.Prev = curA;
            }
            else {
                curA.Prev = aLast;
                aLast.Next = curA;
            }
        }

        List<PolylinePoint> GetRangeOnPolyline(PolylinePoint start, PolylinePoint end, bool forwardOrder) {
            List<PolylinePoint> res = new List<PolylinePoint>();
            for (PolylinePoint pp = start; pp != end; pp = Next(pp, forwardOrder))
                res.Add(pp);

            return res;
        }

        bool IsNeighbor(PolylinePoint a, PolylinePoint b) {
            return a.Prev != null && a.Prev.Point == b.Point || a.Next != null && a.Next.Point == b.Point;
        }

        void RegisterInterestingPoint(Point p) {
            if (!interestingPoints.Contains(p))
                interestingPoints.Insert(p);
        }

        internal Set<Point> GetChangedHubs() {
            return interestingPoints;
        }

        internal int NumberOfReducedCrossings() {
            return numberOfReducedCrossings;
        }

        bool PolylineIsOK(Polyline poly) {
            HashSet<Point> pointsToPP = new HashSet<Point>();
            for (var pp = poly.StartPoint; pp != null; pp = pp.Next) {
                if (pp == poly.StartPoint) {
                    if (pp.Prev != null) return false;
                }
                else {
                    if (pp.Prev.Next != pp) return false;
                }
                if (pp == poly.EndPoint) {
                    if (pp.Next != null) return false;
                }
                else {
                    if (pp.Next.Prev != pp) return false;
                }

                if (pointsToPP.Contains(pp.Point)) return false;
                pointsToPP.Add(pp.Point);
            }

            if (poly.StartPoint.Prev != null) return false;
            if (poly.EndPoint.Next != null) return false;
            return true;
        }
    }
}