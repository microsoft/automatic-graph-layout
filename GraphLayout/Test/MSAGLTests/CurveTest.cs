//-----------------------------------------------------------------------
// <copyright file="CurveTest.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
#if TEST_MSAGL
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.GraphViewerGdi;
#endif
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Msagl.Routing.Visibility;
using System.Diagnostics;

namespace Microsoft.Msagl.UnitTests {
    [TestClass]
    public class CurveTest {
        /*public void ClosestPolyTest() {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            Polyline pl1;
            Polyline pl2;
            Polyline pl3;
            Polyline pl0 = GetPolylines(out pl1, out pl2, out pl3);
            var point = new Point(373, 274);
            var tree = RectangleNode<Polyline, Point>.CreateRectangleNodeOnData(new[] { pl0, pl1, pl2, pl3 }, p => p.BoundingBox);
            Polyline closestPoly;
            Point closestPoint;
            Routing.Spline.Bundling.SteinerDijkstraOnVisibilityGraph.GetClosestObstacle(
                tree, point, tree.Rectangle.Diagonal, out closestPoly, out closestPoint);
            //LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(100, 1, "black", pl0),
            //                                        new DebugCurve(100, 1, "black", pl1),
            //                                        new DebugCurve(100, 1, "black", pl2),
            //                                        new DebugCurve(100, 1, "black", pl3),
            //                                        new DebugCurve(100, 1, "green", closestPoly),
            //                                        new DebugCurve(new LineSegment(point, closestPoint)));
        }
        [TestMethod]
        public void ClosestPolyToLineSegTest() {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            Polyline pl1;
            Polyline pl2;
            Polyline pl3;
            Polyline pl0 = GetPolylines(out pl1, out pl2, out pl3);
            var point = new Point(392, -187);
            var ls = new LineSegment(point, new Point(314, -303));
            var tree = RectangleNode<Polyline, Point>.CreateRectangleNodeOnData(new[] { pl0, pl1, pl2, pl3 }, p => p.BoundingBox);
            Polyline closestPoly;
            Point closestPoint;
            Point closestOnLineSeg;
            Routing.Spline.Bundling.SteinerDijkstraOnVisibilityGraph.GetClosestObstacleToLineSegment(
                tree, ls, tree.Rectangle.Diagonal, out closestPoly, out closestOnLineSeg, out closestPoint);
            //LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(100, 1, "black", pl0),
            //                                        new DebugCurve(100, 1, "black", pl1),
            //                                        new DebugCurve(100, 1, "black", pl2),
            //                                        new DebugCurve(100, 1, "black", pl3),
            //                                        new DebugCurve(100, 1, "green", closestPoly),
            //                                        new DebugCurve(new LineSegment(closestOnLineSeg, closestPoint)),
            //                                        new DebugCurve("red", ls));
        } */
        [TestMethod]
        public void CircleLineCross() {
            //  DisplayGeometryGraph.SetShowFunctions();

            const double chordLen = 1.5;
            const int cy = 3;
            const double cx = chordLen / 2;
            var r = Math.Sqrt(cy * cy + cx * cx);
            var center = new Point(cx, cy);
            var circle = new Ellipse(r, r, center);
            var a = new Point(-10, 0);
            var b = new Point(10, 0);
            var ca = a - center;
            var cb = b - center;
            const double angle = Math.PI / 6;
            ca = ca.Rotate(angle);
            cb = cb.Rotate(angle);
            a = center + ca;
            b = center + cb;
            var lineSeg = new LineSegment(a, b);
            var ii = Curve.GetAllIntersections(lineSeg, circle, true);
            //            DisplayGeometryGraph.ShowDebugCurves(new DebugCurve(100,0.01,"black", lineSeg), new DebugCurve(100,0.01,"blue", circle));
            Assert.IsTrue(ii.Count == 2);

            var roundedRect = new RoundedRect(new Rectangle(0, 100, 200, 0), 10, 10);
            lineSeg = new LineSegment(0, 0, 200, 100);
            //   DisplayGeometryGraph.ShowDebugCurves(new DebugCurve(100, 0.01, "black", lineSeg), new DebugCurve(100, 0.01, "blue", roundedRect));
            ii = Curve.GetAllIntersections(lineSeg, roundedRect, true);
            Assert.IsTrue(ii.Count == 2);

            lineSeg = new LineSegment(0, 100, 200, 0);
            // DisplayGeometryGraph.ShowDebugCurves(new DebugCurve(100, 0.01, "black", lineSeg), new DebugCurve(100, 0.01, "blue", roundedRect));            
            ii = Curve.GetAllIntersections(lineSeg, roundedRect, true);
            Assert.IsTrue(ii.Count == 2);

            lineSeg = new LineSegment(-2.05, 92.9, 8.27, 103.0);
            // DisplayGeometryGraph.ShowDebugCurves(new DebugCurve(100, 0.01, "black", lineSeg), new DebugCurve(100, 0.01, "blue", roundedRect));
            ii = Curve.GetAllIntersections(lineSeg, roundedRect, true);
            Assert.IsTrue(ii.Count == 0);

            var d = 10 * (Math.Sqrt(2) - 1) * Math.Sqrt(2);

            lineSeg = new LineSegment(d, 0, 0, d);
            var seg =
                roundedRect.Curve.Segments.Where(s => s is Ellipse).Select(s => (Ellipse)s).Where(
                    e => e.Center.X < 11 && e.Center.Y < 11).First();
            // DisplayGeometryGraph.ShowDebugCurves(new DebugCurve(100, 0.01, "black", lineSeg), new DebugCurve(100, 0.01, "blue", seg));
            ii = Curve.GetAllIntersections(lineSeg, seg, true);

            Assert.IsTrue(ii.Count == 1);


        }

        static Polyline GetPolylines(out Polyline pl1, out Polyline pl2, out Polyline pl3) {
            var p0 = new[] { 223, 255, 172, 272, 129, 195, 174, 120, 217, 135, 282, 205 };
            var p1 = new[] { 381, 194, 334, 196, 311, 181, 316, 128, 390, 156 };
            var p2 = new[] { 559, 323, 491, 338, 428, 303, 451, 167, 560, 187 };
            var p3 = new[] { 384, 453, 332, 401, 364, 365, 403, 400 };
            var pl0 = new Polyline(PointsFromData(p0)) { Closed = true };
            pl1 = new Polyline(PointsFromData(p1)) { Closed = true };
            pl2 = new Polyline(PointsFromData(p2)) { Closed = true };
            pl3 = new Polyline(PointsFromData(p3)) { Closed = true };
            return pl0;
        }


        static Point[] PointsFromData(int[] data) {
            var ps = new Point[data.Length / 2];
            for (int i = 0; i < data.Length; i += 2) {
                ps[i / 2] = new Point(data[i], -data[i + 1]); // to orient the polyline
            }
            return ps;
        }
        [TestMethod]
        public void TwoTriangles() {
            var points = new List<Point> {
              new Point(0, 0),
              new Point(1, 1),
              new Point(2, 0),
              new Point(3, 0),
              new Point(3, 3),
              new Point(4, 1)

            };
            var p = new Polyline();
  p.AddPoint(points[0]);
            p.AddPoint(points[1]);
            p.AddPoint(points[2]);
            p.Closed = true;
            var q = new Polyline();
            q.AddPoint(points[0 + 3]);
            q.AddPoint(points[1 + 3]);
            q.AddPoint(points[2 + 3]);
            q.Closed = true;
            var P = new Polygon(p);
            var Q = new Polygon(q);
            var di = Polygon.Distance(P, Q);
            Assert.IsTrue(di > 0);
        }

        [TestMethod]
        public void PolygonPolygonDistanceTest0() {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            Polyline pl1;
            Polyline pl2;
            Polyline pl3;
            Polyline pl0 = GetPolylines(out pl1, out pl2, out pl3);
            var point = new Point(373, 274);
            var ls = new LineSegment(point, new Point(314, 303));
            var pl5 = new Polyline(ls.Start, ls.End);
            Point p, q;
            //LayoutAlgorithmSettings.Show(pl0);
            var dist = Polygon.Distance(new Polygon(pl5), new Polygon(pl0), out p, out q);
            dist = Polygon.Distance(new Polygon(pl5), new Polygon(pl1), out p, out q);
            dist = Polygon.Distance(new Polygon(pl5), new Polygon(pl2), out p, out q);
            dist = Polygon.Distance(new Polygon(pl5), new Polygon(pl3), out p, out q);
        }
        [TestMethod]
        public void TestLineSegmentLineSegmentMinDist() {
            var count = 1000;
            for(int i=1;i<count;i++)
                TestLineSegmentLineSegmentMinDistForI(i);
        }

        void TestLineSegmentLineSegmentMinDistForI(int i) {
            var random = new Random(i);
            Point a = GeneratePointOnRandom(random);
            Point b = GeneratePointOnRandom(random);
            Point c = GeneratePointOnRandom(random);
            Point d = GeneratePointOnRandom(random);
            double parab;
            double parcd;
            var dist=LineSegment.MinDistBetweenLineSegments(a, b, c, d, out parab, out parcd);
            Assert.IsTrue(ApproximateComparer.Close(dist, (a+(b-a)*parab-(c+(d-c)*parcd)).Length));
            const int steps= 64;
            for(int j=0;j<=steps;j++) {
                var abPoint = a + ((double) j/steps)*(b - a);
                for (int k = 0; k <= steps; k++) {
                    var cdPoint = c + ((double) k/steps)*(d - c);
                    Assert.IsTrue(dist - ApproximateComparer.Tolerance <= (cdPoint - abPoint).Length);
                }
            }
        }

        Point GeneratePointOnRandom(Random random) {
            return new Point(random.NextDouble(), random.NextDouble());
        }


        [TestMethod]
        public void LineSegmentCurveIntersectionTest() {
            //DisplayGeometryGraph.SetShowFunctions();

            LineSegment ls = new LineSegment(new Point(4099.7139171825129, 3574.8416020767831), new Point(3799.4470573258791, 3413.5396137128196));
            Rectangle rect = new Rectangle(new Point(3896.7207486979, 3441.1203417969), new Point(3956.1474153646, 3467.0803417969));
            RoundedRect rr = new RoundedRect(rect, 3, 3);
            var inters = Curve.GetAllIntersections(ls, rr, true);

            //List<DebugCurve> dc = new List<DebugCurve>();
            //dc.Add(new DebugCurve(0.01, rr));
           // dc.Add(new DebugCurve(0.01, ls));
           // LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dc);

            Assert.IsTrue(inters.Count == 2);
        }

        [TestMethod]
        public void PolygonPolygonDistanceTest() {
#if TEST_MSAGL
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            var a = new Polygon(new Polyline(new[] { new Point(0, 0), new Point(0, 100), new Point(42, 109), new Point(100, 100), new Point(100, 0) }));
            var b = new Polygon(new Polyline(new[] { new Point(-2, 105), new Point(50, 130) }));
            Point p0, p1;
            var dist = Polygon.Distance(a, b, out p0, out p1);
            TestDist(a, b, dist);

            // LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(new LineSegment(p0,p1)), new DebugCurve("blue", poly0.Polyline), new DebugCurve("red",poly1.Polyline));
            b = new Polygon(new Polyline(new[] { new Point(159, 60), new Point(91, 118) }));
            dist = Polygon.Distance(b, a, out p0, out p1);
            TestDist(a, b, dist);

            b = new Polygon(new Polyline(new[] { new Point(159, 60), new Point(140, 50), new Point(91, 118) }));
            dist = Polygon.Distance(b, a, out p0, out p1);
            //  LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(new LineSegment(p0, p1)),
            //    new DebugCurve("blue", a.Polyline), new DebugCurve("red", b.Polyline));

            TestDist(a, b, dist);

        }

        [TestMethod]
        public void PolygonPolygonDistanceTest2() {
#if TEST_MSAGL
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            var a = new Polyline(new[] {   new Point(-3397.10020369428, 993.94470736826),
                                            new Point(-3426.74057842555, 1014.3329144183),
                                            new Point(-3426.74057842555, 1045.96907990543),
                                            new Point(-3397.10020369428, 1066.35728695547),
                                            new Point(-3357.98527032, 1066.35728695547),
                                            new Point(-3328.34489558873, 1045.96907990543),
                                            new Point(-3328.34489558873, 1014.3329144183),
                                            new Point(-3357.98527032, 993.94470736826)}) { Closed = true };

            var b = new Polyline(new[] {   new Point(-2588.08967113495, 1130.55203056335),
                                            new Point(-3327.46492624868, 1013.85788393446)}) { Closed = true };

            //DisplayGeometryGraph.ShowDebugCurves(new DebugCurve(100,1,"red",a),new DebugCurve(100,1,"blue",b));
            var pa = new Polygon(a);
            var pb = new Polygon(b);
            Point p0, p1;
            var dist0 = Polygon.Distance(pb, pa, out p1, out p0);
            TestDist(pb, pa, dist0);
            var dist = Polygon.Distance(pa, pb, out p0, out p1);
            TestDist(pa, pb, dist);
        }

        static void TestDist(Polygon a, Polygon b, double dist) {
            double u, v;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++) {
                    var d = LineSegment.MinDistBetweenLineSegments(a.Pnt(i), a.Pnt(i + 1), b.Pnt(j),
                                                                                b.Pnt(j + 1), out u, out v);
                    Assert.IsTrue(d >= dist - 0.0000001);
                }
        }
        [Timeout(1000)]
        [TestMethod]
        [Description("Testing Curve.CurveIsInsideOther ")]
        public void CurveIsInsideOfAnother() {
#if TEST_MSAGL
            if (!MsaglTestBase.DontShowTheDebugViewer()) {
                DisplayGeometryGraph.SetShowFunctions();
            }
#endif
            var smallEllipse = CurveFactory.CreateEllipse(10, 10, new Point());
            var ellipse = CurveFactory.CreateEllipse(50, 40, new Point());
            var rect = CurveFactory.CreateRectangle(100, 80, new Point());
            Assert.IsTrue(Curve.CurveIsInsideOther(smallEllipse, ellipse));
            Assert.IsTrue(Curve.CurveIsInsideOther(ellipse, rect));
            var biggerEllipse = CurveFactory.CreateEllipse(51, 40, new Point());
            Assert.IsTrue(Curve.CurveIsInsideOther(ellipse, rect));
            Assert.IsFalse(Curve.CurveIsInsideOther(biggerEllipse, rect));
            var shiftedRect = CurveFactory.CreateRectangle(50, 40, new Point(1, 0));
            Assert.IsFalse(Curve.CurveIsInsideOther(rect, shiftedRect));
            var roundedCornerRect = CurveFactory.CreateRectangleWithRoundedCorners(100, 80, 3, 3, new Point());
            Assert.IsTrue(Curve.CurveIsInsideOther(roundedCornerRect, rect));
            var smallerRect = CurveFactory.CreateRectangle(99, 79, new Point());
            Assert.IsFalse(Curve.CurveIsInsideOther(smallerRect, roundedCornerRect));
        }
        [Timeout(2000)]
        [TestMethod]
        [Description("Testing Curve.CurveInteriorsOverlap ")]
        public void CurveInteriorsOverlap() {
            var smallEllipse = CurveFactory.CreateEllipse(10, 10, new Point());
            var ellipse = CurveFactory.CreateEllipse(50, 40, new Point());
            var rect = CurveFactory.CreateRectangle(100, 80, new Point());
            Assert.IsTrue(Curve.ClosedCurveInteriorsIntersect(smallEllipse, ellipse));
            Assert.IsTrue(Curve.CurveIsInsideOther(ellipse, rect));
            var biggerEllipse = CurveFactory.CreateEllipse(51, 40, new Point());
            Assert.IsTrue(Curve.ClosedCurveInteriorsIntersect(ellipse, rect));
            Assert.IsTrue(Curve.ClosedCurveInteriorsIntersect(biggerEllipse, rect));
            var shiftedRect = CurveFactory.CreateRectangle(50, 40, new Point(1, 0));
            Assert.IsTrue(Curve.ClosedCurveInteriorsIntersect(rect, shiftedRect));
            var roundedCornerRect = CurveFactory.CreateRectangleWithRoundedCorners(100, 80, 3, 3, new Point());
            Assert.IsTrue(Curve.ClosedCurveInteriorsIntersect(roundedCornerRect, rect));
            var smallerRect = CurveFactory.CreateRectangle(99, 79, new Point());
            Assert.IsTrue(Curve.ClosedCurveInteriorsIntersect(smallerRect, roundedCornerRect));
            var shiftedSmallEllipse = CurveFactory.CreateEllipse(10, 10, new Point(20, 0));
            Assert.IsFalse(Curve.ClosedCurveInteriorsIntersect(smallEllipse, shiftedSmallEllipse));
            var shiftedFurterSmallEllipse = CurveFactory.CreateEllipse(10, 10, new Point(20, 1));
            Assert.IsFalse(Curve.ClosedCurveInteriorsIntersect(smallEllipse, shiftedFurterSmallEllipse));

        }

        [TestMethod]
        [Description("Testing ICurve.Length for Cubuc Bezier")]
        public void LengthTestingForCubucBezier() {
            var bs = new CubicBezierSegment(new Point(), new Point(0, 1), new Point(1, 1), new Point(1, 0));
            var len = bs.Length;
            Assert.IsTrue(Math.Abs(len - 2) < 0.001);
            bs = new CubicBezierSegment(new Point(), new Point(0, 1), new Point(1, 2), new Point(1, 0));

            var par = bs.GetParameterAtLength(2);
            var trimmed = bs.Trim(0, par);
            var trimmedLen =trimmed.Length;

            Assert.IsTrue(Math.Abs(trimmedLen - 2) < 0.000001);
        }
        [TestMethod]
        [Description("Testing ICurve.Length for Curve")]
        public void LengthTestingForCurve() {
#if TEST_MSAGL
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            var curve = new Curve(Getsegs());
            var lengths = new List<double>();
            foreach (var seg in curve.Segments) {
                lengths.Add(seg.Length);
            }
            var curveLen = curve.Length;
            double eps = Curve.LineSegmentThreshold*10;
            
            Assert.IsTrue(ApproximateComparer.Close(lengths.Sum(), curveLen));
            var del = curveLen/1000;
            //special case 
            for (int i = 745; i < 746; i++) {
                var li = i*del;
                var pi = curve.GetParameterAtLength(li);
                Assert.IsTrue(Math.Abs(curve.Trim(0, pi).Length - li) < eps);
                for (int j = 724; j < 725; j++)
                    TestLengths(del, j, curve, eps, pi, li);
            }


            for (int i = 0; i < 1000; i++) {
                var li = i*del;
                var pi = curve.GetParameterAtLength(li);
                Assert.IsTrue(Math.Abs(curve.Trim(0, pi).Length - li) < eps);
                for (int j = 0; j < i; j++)
                    TestLengths(del, j, curve, eps, pi, li);
            }

        }

        static void TestLengths(double del, int j, Curve curve, double eps, double pi, double li) {
            var lj = del*j;
            var pj = curve.GetParameterAtLength(lj);
            Assert.IsTrue(Math.Abs(curve.Trim(0, pj).Length - lj) < eps);
            var trimJi = curve.Trim(pj, pi);
            var lenJi = trimJi.Length;
            Assert.IsTrue(Math.Abs(lenJi - (li - lj)) < eps);
        }

        List<ICurve> Getsegs() {
            var l = new List<ICurve>();
            var bs0 = new CubicBezierSegment(new Point(), new Point(0, 1), new Point(1, 2), new Point(1, 0));

            l.Add(bs0);
            var bs1 = new CubicBezierSegment(new Point(2,2), new Point(3, 1), new Point(4, 2), new Point(1, -5));
            bs1.Translate(new Point(10,10));
            l.Add(new LineSegment(bs0.End, bs1.Start));
            l.Add(bs1);
            var xAxis = new Point(2, 0);
            l.Add(new Ellipse(0, Math.PI/2, xAxis, new Point(0, 1), bs1.End - xAxis));

            return l;
        }

        [Timeout(2000)]
        [TestMethod]
        [Description("Testing ICurve.ClosestParameterWithinBounds ")]
        public void ClosestParameterWithinBounds() {
#if TEST_MSAGL
            if (!MsaglTestBase.DontShowTheDebugViewer()) {
                DisplayGeometryGraph.SetShowFunctions();
            }
#endif
            var ellipse = CurveFactory.CreateEllipse(8, 10, new Point());
            var point = new Point(20, 1);
            var t = ellipse.ClosestParameter(point);
            var low = t - 1;
            var high = t + 1;
            var t1 = ellipse.ClosestParameterWithinBounds(point, low, high);
#if TEST_MSAGL
            if (!MsaglTestBase.DontShowTheDebugViewer()) {
                LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(100, 0.1, "black", ellipse),
                        new DebugCurve(100, 0.01, "brown", new LineSegment(ellipse[t], point)),
                        new DebugCurve(100, 0.01, "green", new LineSegment(ellipse[t1], point)));
            }
#endif
            var dist = point - ellipse[t];
            var dist1 = point - ellipse[t1];
            Assert.IsTrue(ApproximateComparer.Close(dist.Length, dist1.Length) && ApproximateComparer.Close(t, t1));
#if TEST_MSAGL
            if (!MsaglTestBase.DontShowTheDebugViewer()) {
                LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(ellipse), new DebugCurve("red", new LineSegment(point, ellipse[t])));
            }
#endif

            var curve = new Curve();
            curve.AddSegment(new LineSegment(new Point(-10, -10), new Point(10, -10)));
            curve.AddSegment(new Ellipse(0, Math.PI / 2, new Point(0, -10), new Point(10, 0), new Point(10, 0)));
            Curve.ContinueWithLineSegment(curve, new Point(20, 10));
            curve.AddSegment(new CubicBezierSegment(curve.End, new Point(20, 20), new Point(15, 25), new Point(10, 25)));
            Curve.ContinueWithLineSegment(curve, new Point(-10, 25));
            Point p = new Point(11, 0);
            t = curve.ClosestParameter(p);

#if TEST_MSAGL
            if (!MsaglTestBase.DontShowTheDebugViewer()) {
                LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(curve), new DebugCurve("red", new LineSegment(p, curve[t])));
            }
#endif
            t1 = curve.ClosestParameterWithinBounds(p, 1 + Math.PI / 4, 2);
#if TEST_MSAGL
            if (!MsaglTestBase.DontShowTheDebugViewer()) {
                LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(curve), new DebugCurve("red", new LineSegment(p, curve[t1])));
            }
#endif
            Assert.IsTrue(t1 < t);
            p = new Point(30, 30);
            t = curve.ClosestParameter(p);
            t1 = curve.ClosestParameterWithinBounds(p, t - 0.5, t + 0.5);
            Assert.IsTrue(t > 1 + Math.PI / 2 + 1 && t < 1 + Math.PI / 2 + 2);
            Assert.IsTrue(ApproximateComparer.Close(t, t1));

            var poly = new Polyline(new Point(0, 0), new Point(10, 0), new Point(20, 10));
            p = new Point(9, 9);
            const double l = 0.7;
            const double h = 1.3;
            t = poly.ClosestParameterWithinBounds(p, l, h);
#if TEST_MSAGL
            if (!MsaglTestBase.DontShowTheDebugViewer()) {
                LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(poly), new DebugCurve("red", new LineSegment(p, poly[t])));
            }
#endif
            var d = (p - poly[t]).Length;

            Assert.IsTrue(d <= (p - poly[l]).Length + ApproximateComparer.Tolerance && d < (p - poly[h]).Length + ApproximateComparer.Tolerance && d < (p - poly[(l + h) / 2]).Length + ApproximateComparer.Tolerance);
        }
#if !TEST_MSAGL
        [Timeout(1000)]
#endif
        [TestMethod]
        [Description("Testing Ellipse.BoundingBox ")]
        public void EllipseBoundingBox() {
#if TEST_MSAGL
            if (!MsaglTestBase.DontShowTheDebugViewer()) {
                DisplayGeometryGraph.SetShowFunctions();
            }
#endif
            Ellipse ell = new Ellipse(Math.PI / 6, Math.PI / 4, new Point(10, 0), new Point(0, 5), new Point(1, 1));
            TestEllipseBoxOnEllipse(ell);
            ell = new Ellipse(Math.PI / 6, 2 * Math.PI / 4, new Point(10, 0), new Point(0, 5), new Point(1, 1));
            TestEllipseBoxOnEllipse(ell);
            ell = new Ellipse(Math.PI / 6, 3 * Math.PI / 4, new Point(10, 0), new Point(0, 5), new Point(1, 1));
            TestEllipseBoxOnEllipse(ell);
            ell = new Ellipse(-Math.PI / 6, 3 * Math.PI / 4, new Point(10, 0), new Point(0, 5), new Point(1, 1));
            TestEllipseBoxOnEllipse(ell);
            ell = new Ellipse(0, 3 * Math.PI / 4, new Point(10, 0), new Point(0, 5), new Point(1, 1));
            TestEllipseBoxOnEllipse(ell);
        }
        static void TestEllipseBoxOnEllipse(Ellipse ell) {
            var b = ell.BoundingBox;
            var smallerBox = new Rectangle(ell.Start);
            const int steps = 1000;
            var del = (ell.ParEnd - ell.ParStart) / steps;
            for (int i = 1; i <= steps; i++)
                smallerBox.Add(ell[ell.ParStart + i * del]);
#if TEST_MSAGL
            if (!MsaglTestBase.DontShowTheDebugViewer()) {
                LayoutAlgorithmSettings.ShowDebugCurves(
                    new DebugCurve(100, 0.1, "purple", ell),
                    new DebugCurve(100, 0.1, "red", smallerBox.Perimeter()),
                    new DebugCurve(100, 0.05, "green", b.Perimeter()),
                    new DebugCurve(100, 0.1, "brown", new LineSegment(ell.Start, ell.End)),
                    new DebugCurve(100, 0.1, "black", new LineSegment(ell.Center, ell.Start)),
                    new DebugCurve(100, 0.1, "black", new LineSegment(ell.Center, ell.End)));
            }
#endif
            Assert.IsTrue(ApproximateComparer.CloseIntersections(b.LeftTop, smallerBox.LeftTop) && ApproximateComparer.CloseIntersections(b.RightBottom, smallerBox.RightBottom));
        }

    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               