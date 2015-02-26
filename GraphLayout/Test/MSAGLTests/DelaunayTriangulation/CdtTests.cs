/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Msagl.Routing.Spline.Bundling;

namespace Microsoft.Msagl.UnitTests.DelaunayTriangulation {
    [TestClass]
    public class CdtTests {
        [TestMethod]
        public void InCircleTest() {
            var a = new CdtSite(new Point());
            var b = new CdtSite(new Point(2, 0));
            var c = new CdtSite(new Point(1, 2));
            var s = new CdtSite(new Point(1, 1));
            Assert.IsTrue(CdtSweeper.InCircle(s, a, b, c));
            MoveSites(a, b, c, s);
            Assert.IsTrue(CdtSweeper.InCircle(s, a, b, c));
            RotateSites(a, b, c, s);
            Assert.IsTrue(CdtSweeper.InCircle(s, a, b, c));
            a = new CdtSite(new Point());
            b = new CdtSite(new Point(2, 0));
            c = new CdtSite(new Point(1, 2));
            s = new CdtSite(new Point(1, -1));
            Assert.IsTrue(!CdtSweeper.InCircle(s, a, b, c));
            MoveSites(a, b, c, s);
            Assert.IsTrue(!CdtSweeper.InCircle(s, a, b, c));
            RotateSites(a, b, c, s);
            Assert.IsTrue(!CdtSweeper.InCircle(s, a, b, c));
            a = new CdtSite(new Point());
            b = new CdtSite(new Point(1, 0));
            c = new CdtSite(new Point(5, 5));
            s = new CdtSite(new Point(3, 1));
            Assert.IsTrue(CdtSweeper.InCircle(s, a, b, c));
            MoveSites(a, b, c, s);
            Assert.IsTrue(CdtSweeper.InCircle(s, a, b, c));
            RotateSites(a, b, c, s);
            Assert.IsTrue(CdtSweeper.InCircle(s, a, b, c));
            Assert.IsTrue(CdtSweeper.InCircle(s, c, a, b));
            a = new CdtSite(new Point());
            b = new CdtSite(new Point(1, 0));
            c = new CdtSite(new Point(5, 5));
            s = new CdtSite(new Point(4, 1));
            Assert.IsTrue(!CdtSweeper.InCircle(s, a, b, c));
            MoveSites(a, b, c, s);
            Assert.IsTrue(!CdtSweeper.InCircle(s, a, b, c));
            RotateSites(a, b, c, s);
            Assert.IsTrue(!CdtSweeper.InCircle(s, a, b, c));
            a = new CdtSite(new Point());
            b = new CdtSite(new Point(1, 0));
            c = new CdtSite(new Point(5, 5));
            s = new CdtSite(new Point(3, 0.5));
            Assert.IsTrue(!CdtSweeper.InCircle(s, a, b, c));
            MoveSites(a, b, c, s);
            Assert.IsTrue(!CdtSweeper.InCircle(s, a, b, c));
            RotateSites(a, b, c, s);
            Assert.IsTrue(!CdtSweeper.InCircle(s, a, b, c));
            Assert.IsTrue(!CdtSweeper.InCircle(s, c, a, b)); 
        }
        
        static void RotateSites(CdtSite a, CdtSite b, CdtSite c, CdtSite s) {
            const double angle = Math.PI / 3;
            a.Point = a.Point.Rotate(angle);
            b.Point = b.Point.Rotate(angle);
            c.Point = c.Point.Rotate(angle);
            s.Point = s.Point.Rotate(angle);
        }
        
        static void MoveSites(CdtSite a, CdtSite b, CdtSite c, CdtSite s) {
            var del=new Point(20,-30);
            a.Point = a.Point + del;
            b.Point = b.Point + del;
            c.Point = c.Point + del;
            s.Point = s.Point + del;
        }

        [TestMethod]
        public void CdtTriangleCreationTest() {
            var a = new CdtSite(new Point());
            var b = new CdtSite(new Point(2, 0));
            var c = new CdtSite(new Point(1, 2));
            var tri = new CdtTriangle(a, b, c, Cdt.GetOrCreateEdge);
            var e = tri.Edges[0];
            Assert.IsTrue(e.upperSite == a);
            Assert.IsTrue(e.lowerSite == b);
            Assert.IsTrue(e.CcwTriangle == tri && e.CwTriangle == null);

            e = tri.Edges[1];
            Assert.IsTrue(e.upperSite == c);
            Assert.IsTrue(e.lowerSite == b);
            Assert.IsTrue(e.CwTriangle == tri && e.CcwTriangle == null);
            
            e = tri.Edges[2];
            Assert.IsTrue(e.upperSite == c);
            Assert.IsTrue(e.lowerSite == a);
            Assert.IsTrue(e.CcwTriangle == tri && e.CwTriangle == null);

            var tri0=new CdtTriangle(new CdtSite(new Point(2,2)), tri.Edges[1], Cdt.GetOrCreateEdge );
            Assert.IsTrue(tri0.Edges[0]==tri.Edges[1]);
            Assert.IsTrue(tri.Edges[1].CcwTriangle != null && tri.Edges[1].CwTriangle != null );
        }

        [TestMethod]
        public void SmallTriangulation() {
#if DEBUG&& TEST_MSAGL
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            var cdt = new Cdt(Points(), null, new []{new Tuple<Point, Point>(new Point(109,202),new Point(506,135) ),
            new Tuple<Point, Point>(new Point(139,96),new Point(452,96) )});
            cdt.Run();
        }

        static IEnumerable<Point> Points() {
            foreach (var segment in Segments()) {
                yield return segment.Item1;
                yield return segment.Item2;
            }
            yield return new Point(157,198);
        }

        static IEnumerable<Tuple<Point, Point>> Segments() {
            yield return new Tuple<Point, Point>(new Point(181, 186), new Point(242, 73));
            yield return new Tuple<Point, Point>(new Point(236, 122), new Point(268, 202));
            yield return new Tuple<Point, Point>(new Point(274, 167), new Point(343, 76));
            yield return new Tuple<Point, Point>(new Point(352, 131), new Point(361, 201));
            yield return new Tuple<Point, Point>(new Point(200, 209), new Point(323, 237));
            yield return new Tuple<Point, Point>(new Point(372, 253), new Point(451, 185));
            yield return new Tuple<Point, Point>(new Point(448, 133), new Point(517, 272));
            yield return new Tuple<Point, Point>(new Point(339, 327), new Point(327, 145));
            yield return new Tuple<Point, Point>(new Point(185, 220), new Point(207, 172));
            yield return new Tuple<Point, Point>(new Point(61, 226), new Point(257, 253));
            yield return new Tuple<Point, Point>(new Point(515, 228), new Point(666, 258));
        }
        [DeploymentItem(@"Resources\triangles")]
        [TestMethod]
        public void ThreadOverVertex() {
#if DEBUG&&TEST_MSAGL
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            var stream = File.Open("triangles", FileMode.Open);
            var bformatter = new BinaryFormatter();

            var trs = (CdtTriangle[])bformatter.Deserialize(stream);
            var start = (Point)bformatter.Deserialize(stream);
            var end = (Point)bformatter.Deserialize(stream);
            stream.Close();
            int count = 0;
            foreach (var t in FindStartTriangle(trs, start)) {
                count++;
                if (count == 1) continue;
                ThreadOnTriangle(start, end, t);
            }


        }

        private static void ThreadOnTriangle(Point start, Point end, CdtTriangle t) {
            var threader = new CdtThreader(t, start, end);
            while(threader.MoveNext()){}

        }

        static IEnumerable<CdtTriangle> FindStartTriangle(CdtTriangle[] trs, Point p) {

            foreach (var t in trs) {
                var loc = CdtIntersections.PointLocationInsideTriangle(p, t);
                if (loc != PointLocation.Outside)
                    yield return t;
            }            
        }
        [TestMethod]
        public void TriangulationWithSizes() {
#if DEBUG&&TEST_MSAGL
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            int[] dimensions = { 2, 10, 100, 10000, 100000 };
            const double size=10.0;
            foreach (var n in dimensions)
                Triangulate(n, size);
        }

        void Triangulate(int n, double size) {
            var random = new Random(n);
            var w = n * size;
            var cdt = new Cdt(PointsForCdt(random, n, w), null, SegmentsForCdt(w));
            cdt.Run();
#if DEBUG&&TEST_MSAGL
            CdtSweeper.ShowFront(cdt.GetTriangles(), null, null,null);
#endif
        }

        IEnumerable<Tuple<Point, Point>> SegmentsForCdt(double size) {
            var w = size / 2;
            var corners=new []{ new Point(0, 0), new Point(0, size), new Point(size,0), new Point(size,size)};
            var center=new Point(w,w);
            foreach (var corner in corners)
                yield return new Tuple<Point, Point>(center, corner);
        }

        static IEnumerable<Point> PointsForCdt(Random random, int n, double d) {
            for (int i = 0; i < n; i++)
                yield return CreateRandomPointInRange(random, d);
        }

        static Point CreateRandomPointInRange(Random random, double d) {
            return new Point(d * random.NextDouble(), d * random.NextDouble());
        }

#if DEBUG&&TEST_MSAGL
        [DeploymentItem(@"Resources\polys")]
        [TestMethod]
        public void TestRepeatedSite() {
            //in this method the triangulation is such that a repeated site appears in EdgeEvent
            var stream = File.Open("polys", FileMode.Open);
            var bformatter = new BinaryFormatter();

            var polys = (Polyline[])bformatter.Deserialize(stream);
            stream.Close();
            var cdt = new Cdt(null, polys, null);
            cdt.Run();
            TestTriangles(cdt.GetTriangles());
        }

        void TestTriangles(IEnumerable<CdtTriangle>  triangles) {
            var usedSites = new Set<CdtSite>();
            foreach (var t in triangles)
                usedSites.InsertRange(t.Sites);
            foreach (var triangle in triangles) {
                TestTriangle(triangle, usedSites);
            }
        }

        void TestTriangle(CdtTriangle triangle, Set<CdtSite> usedSites) {
            var tsites = triangle.Sites;
            foreach (var site in usedSites) {
                if (!tsites.Contains(site)) {
                    Assert.IsTrue(SeparatedByConstrainedEdge(triangle, site) || !CdtSweeper.InCircle(site, tsites[0], tsites[1], tsites[2])); 
//                    {
//                        List<ICurve> redCurves = new List<ICurve>();
//                        redCurves.Add(new Ellipse(2, 2, site.Point));
//                        List<ICurve> blueCurves = new List<ICurve>();
//                        blueCurves.Add(Circumcircle(tsites[0].Point, tsites[1].Point, tsites[2].Point));
//                        ShowFront(Triangles, front, redCurves, blueCurves);
//                    }
                }
            }
        }

        static bool SeparatedByConstrainedEdge(CdtTriangle triangle, CdtSite site) {
            for (int i = 0; i < 3; i++)
                if (SeparatedByEdge(triangle, i, site))
                    return true;
            return false;
        }

        static bool SeparatedByEdge(CdtTriangle triangle, int i, CdtSite site) {
            var e = triangle.Edges[i];
            var s = triangle.Sites[i + 2];
            var a0 = ApproximateComparer.Sign(Point.SignedDoubledTriangleArea(s.Point, e.upperSite.Point, e.lowerSite.Point));
            var a1 = ApproximateComparer.Sign(Point.SignedDoubledTriangleArea(site.Point, e.upperSite.Point, e.lowerSite.Point));
            return a0 * a1 <= 0;
        }
#endif
    }
}
