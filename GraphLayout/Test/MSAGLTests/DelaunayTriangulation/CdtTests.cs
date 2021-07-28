using System;
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
using Microsoft.Msagl.Routing.Spline.Bundling;
using Microsoft.Msagl.GraphViewerGdi;



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
            double angle = Math.PI / 3;
            a.Point = a.Point.Rotate(angle);
            b.Point = b.Point.Rotate(angle);
            c.Point = c.Point.Rotate(angle);
            s.Point = s.Point.Rotate(angle);
        }

        static void MoveSites(CdtSite a, CdtSite b, CdtSite c, CdtSite s) {
            var del = new Point(20, -30);
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

            var tri0 = new CdtTriangle(new CdtSite(new Point(2, 2)), tri.Edges[1], Cdt.GetOrCreateEdge);
            Assert.IsTrue(tri0.Edges[0] == tri.Edges[1]);
            Assert.IsTrue(tri.Edges[1].CcwTriangle != null && tri.Edges[1].CwTriangle != null);
        }
        [Ignore]
        [TestMethod]
        public void FlatLine() {
#if TEST_MSAGL
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            var points = new List<Point> { new Point(0, 0), new Point(100, 0), new Point(300, 0) };
            var cdt = new Cdt(points, null, null);
            cdt.Run();
#if TEST_MSAGL
CdtSweeper.ShowFront(cdt.GetTriangles(), null, null, null);
#endif
        }
        [TestMethod]
        public void SmallTriangulation() {
#if TEST_MSAGL
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            var cdt = new Cdt(Points(), null, new[]{new SymmetricTuple<Point>(new Point(109,202),new Point(506,135) ),
            new SymmetricTuple<Point>(new Point(139,96),new Point(452,96) )}.ToList());
            cdt.Run();
            //            CdtSweeper.ShowFront(cdt.GetTriangles(), null, new[]{new LineSegment(new Point(109,202),new Point(506,135) ),
            //          new LineSegment(new Point(139,96),new Point(452,96) )}.ToList(), null);
        }

        static IEnumerable<Point> Points() {
            foreach (var segment in Segments()) {
                yield return segment.A;
                yield return segment.B;
            }
            yield return new Point(157, 198);
        }

        static IEnumerable<SymmetricTuple<Point>> Segments() {
            yield return new SymmetricTuple<Point>(new Point(181, 186), new Point(242, 73));
            yield return new SymmetricTuple<Point>(new Point(236, 122), new Point(268, 202));
            yield return new SymmetricTuple<Point>(new Point(274, 167), new Point(343, 76));
            yield return new SymmetricTuple<Point>(new Point(352, 131), new Point(361, 201));
            yield return new SymmetricTuple<Point>(new Point(200, 209), new Point(323, 237));
            yield return new SymmetricTuple<Point>(new Point(372, 253), new Point(451, 185));
            yield return new SymmetricTuple<Point>(new Point(448, 133), new Point(517, 272));
            yield return new SymmetricTuple<Point>(new Point(339, 327), new Point(327, 145));
            yield return new SymmetricTuple<Point>(new Point(185, 220), new Point(207, 172));
            yield return new SymmetricTuple<Point>(new Point(61, 226), new Point(257, 253));
            yield return new SymmetricTuple<Point>(new Point(515, 228), new Point(666, 258));
        }

        private static void ThreadOnTriangle(Point start, Point end, CdtTriangle t) {
            var threader = new CdtThreader(t, start, end);
            while (threader.MoveNext()) { }

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
#if TEST_MSAGL
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            int[] dimensions = { 2, 10, 100, 10000, 100000 };
            double size = 10.0;
            foreach (var n in dimensions)
                Triangulate(n, size);
        }

        void Triangulate(int n, double size) {
            var random = new Random(n);
            var w = n * size;
            var cdt = new Cdt(PointsForCdt(random, n, w), null, SegmentsForCdt(w).ToList());
            cdt.Run();
#if TEST_MSAGL
            //CdtSweeper.ShowFront(cdt.GetTriangles(), null, null,null);
#endif
        }

        IEnumerable<SymmetricTuple<Point>> SegmentsForCdt(double size) {
            var w = size / 2;
            var corners = new[] { new Point(0, 0), new Point(0, size), new Point(size, 0), new Point(size, size) };
            var center = new Point(w, w);
            foreach (var corner in corners)
                yield return new SymmetricTuple<Point>(center, corner);
        }

        static IEnumerable<Point> PointsForCdt(Random random, int n, double d) {
            for (int i = 0; i < n; i++)
                yield return CreateRandomPointInRange(random, d);
        }

        static Point CreateRandomPointInRange(Random random, double d) {
            return new Point(d * random.NextDouble(), d * random.NextDouble());
        }


        void TestTriangles(IEnumerable<CdtTriangle> triangles) {
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

        [TestMethod]
        public void TwoHoles() {
#if TEST_MSAGL
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            var corners = new[]{
            new Point(0, 0),
            new Point(100, 0),
            new Point(100, 100),
            new Point(0, 100),
        };
            var triangle = new Polyline();
            triangle.AddPoint(35.0, 50);
            triangle.AddPoint(40, 31);
            triangle.AddPoint(30, 30);
            triangle.Closed = true;

            var holes = new Polyline[] {
    new Rectangle(new Point(10, 10), new Point(20, 20)).Perimeter(),
    triangle,
  };
            var cut = new SymmetricTuple<Point>[] { new SymmetricTuple<Point>(new Point(80, 80), new Point(90, 75)) };
            var cdt = new Cdt(corners, holes, cut.ToList());
            cdt.Run();
            //  CdtSweeper.ShowFront(cdt.GetTriangles(), null, holes, cut.Select((c) => new LineSegment(c.A, c.B)));
        }
        [TestMethod]
        public void Grid() {
            var corners = new List<Point>();
            for (var i = 0; i < 10; i++) {
                for (var j = 0; j < 10; j++) {
                    corners.Add(new Point(10 * i, 10 * j));
                }
            }
            var cdt = new Cdt(corners, null, null);
            cdt.Run();
        }
        [TestMethod]
        public void GridRotated() {
#if TEST_MSAGL
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            var corners = new List<Point>();
            var ang = Math.PI / 6;
            for (var i = 0; i < 10; i++) {
                for (var j = 0; j < 10; j++) {
                    corners.Add(new Point(10 * i, 10 * j).Rotate(ang));
                }
            }
            var cdt = new Cdt(corners, null, null);
            cdt.Run();
#if TEST_MSAGL
            //  CdtSweeper.ShowFront(cdt.GetTriangles(), null, null, null);
#endif
        }
        [TestMethod]
        public void AlongFrontTest() {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            List<Polyline> polys = CreatePolylines();
            var constrainedDelaunayTriangulation = new Cdt(null, polys, null);
            constrainedDelaunayTriangulation.Run();
        }

        private static List<Polyline> CreatePolylines() {
            List<List<Point>> obst = CreateObstacles();
            var polys = new List<Polyline>();
            foreach (var pts in obst) {
                polys.Add(new Polyline(pts) { Closed = true });
            }

            return polys;
        }

        private static List<List<Point>> CreateObstacles() {
            return new List<List<Point>>{
new List<Point>
{


new Point(214.7766718754,296.841985099),

new Point(214.7766718754,661.4271486588),

new Point(742.5484585818,661.4271486588),

new Point(742.5484585818,296.841985099),

},


new List<Point> {


new Point(416.4923232284,362.431685751),

new Point(398.6733849139,375.1116781352),

new Point(398.6733849139,394.1026705261),

new Point(416.4923232284,406.7826629103),

new Point(440.6400773036,406.7826629103),

new Point(458.459015618,394.1026705261),

new Point(458.459015618,375.1116781352),

new Point(440.6400773036,362.431685751),

},


new List<Point> {


new Point(229.7766718754,409.4491594297),

new Point(229.7766718754,590.4271486588),

new Point(492.2879507447,590.4271486588),

new Point(492.2879507447,409.4491594297),

},


new List<Point> {


new Point(357.156802678,492.8714858652),

new Point(357.156802678,526.1972850486),

new Point(395.4826018614,526.1972850486),

new Point(395.4826018614,492.8714858652),

},


new List<Point> {


new Point(305.8886606874,486.1319379791),

new Point(287.1853541085,499.5500032044),

new Point(287.1853541085,519.9379766123),

new Point(305.8886606874,533.3560418376),

new Point(330.9339119613,533.3560418376),

new Point(349.6372185402,519.9379766123),

new Point(349.6372185402,499.5500032044),

new Point(330.9339119613,486.1319379791),

},


new List<Point> {


new Point(462.2585913586,363.8664389571),

new Point(462.2585913586,384.9327718209),

new Point(497.3249242223,384.9327718209),

new Point(497.3249242223,363.8664389571),

},


new List<Point> {


new Point(406.366212695,505.1111188568),

new Point(406.366212695,529.4271486588),

new Point(435.682242497,529.4271486588),

new Point(435.682242497,505.1111188568),

},


new List<Point> {


new Point(608.3303205434,796.0936451782),

new Point(590.511382229,808.7736375623),

new Point(590.511382229,827.7646299533),

new Point(608.3303205434,840.4446223374),

new Point(632.4780746186,840.4446223374),

new Point(650.297012933,827.7646299533),

new Point(650.297012933,808.7736375623),

new Point(632.4780746186,796.0936451782),

},


new List<Point> {


new Point(489.9943132577,665.1111188568),

new Point(489.9943132577,793.4271486588),

new Point(690.6541396567,793.4271486588),

new Point(690.6541396567,665.1111188568),

},


new List<Point> {


new Point(395.0782382134,679.0936038572),

new Point(377.2592744612,691.7736174711),

new Point(377.2592744612,710.7646500445),

new Point(395.0782382134,723.4446636584),

new Point(419.2260181039,723.4446636584),

new Point(437.0449818561,710.7646500445),

new Point(437.0449818561,691.7736174711),

new Point(419.2260181039,679.0936038572),

},


new List<Point> {


new Point(454.994182647,685.6738544915),

new Point(454.994182647,703.9899455148),

new Point(486.3102736703,703.9899455148),

new Point(486.3102736703,685.6738544915),

},


new List<Point> {


new Point(386.818797647,532.0936451782),

new Point(368.9998593326,544.7736375623),

new Point(368.9998593326,563.7646299533),

new Point(386.818797647,576.4446223374),

new Point(410.9665517222,576.4446223374),

new Point(428.7854900367,563.7646299533),

new Point(428.7854900367,544.7736375623),

new Point(410.9665517222,532.0936451782),

},


new List<Point> {


new Point(341.993982647,699.4351154254),

new Point(341.993982647,723.7512064488),

new Point(374.3100736703,723.7512064488),

new Point(374.3100736703,699.4351154254),

},


new List<Point> {


new Point(298.7156035586,601.05005054),

new Point(298.7156035586,635.4882169756),

new Point(345.1537699942,635.4882169756),

new Point(345.1537699942,601.05005054),

},


new List<Point> {


new Point(341.3544945372,737.9218846469),

new Point(341.3544945372,767.2379144489),

new Point(385.6705243392,767.2379144489),

new Point(385.6705243392,737.9218846469),

},


new List<Point> {


new Point(298.4491785239,725.4856134548),

new Point(298.4491785239,757.8017044781),

new Point(335.7652695472,757.8017044781),

new Point(335.7652695472,725.4856134548),

},


new List<Point> {


new Point(250.103722341,915.6582660006),

new Point(250.103722341,941.5947277132),

new Point(287.0401840536,941.5947277132),

new Point(287.0401840536,915.6582660006),

},


new List<Point> {


new Point(277.6157743353,837.0554270956),

new Point(277.6157743353,863.0395855339),

new Point(312.5999327736,863.0395855339),

new Point(312.5999327736,837.0554270956),

},


new List<Point> {


new Point(454.994182647,817.9256086628),

new Point(454.994182647,834.2416996862),

new Point(488.3102736703,834.2416996862),

new Point(488.3102736703,817.9256086628),

},


new List<Point> {


new Point(314.8439626862,811.2394571918),

new Point(314.8439626862,832.3294807887),

new Point(349.933986283,832.3294807887),

new Point(349.933986283,811.2394571918),

},


new List<Point> {


new Point(349.7896151435,784.5348877495),

new Point(349.7896151435,808.6249113463),

new Point(381.8796387404,808.6249113463),

new Point(381.8796387404,784.5348877495),

},


new List<Point> {


new Point(42.6353542293,1001.3660055157),

new Point(22.2726421722,1016.1689713139),

new Point(22.2726421722,1039.1782033601),

new Point(42.6353542293,1053.9811691584),

new Point(69.3646457707,1053.9811691584),

new Point(89.7273578278,1039.1782033601),

new Point(89.7273578278,1016.1689713139),

new Point(69.3646457707,1001.3660055157),

},


new List<Point> {


new Point(43.0083102293,830.0127101371),

new Point(23.3805986845,844.202261888),

new Point(23.3805986845,866.0504597764),

new Point(43.0083102293,880.2400115274),

new Point(68.9916897707,880.2400115274),

new Point(88.6194013155,866.0504597764),

new Point(88.6194013155,844.202261888),

new Point(68.9916897707,830.0127101371),

},


new List<Point> {


new Point(191.5331469979,723.9044109683),

new Point(173.7142086835,736.5844033524),

new Point(173.7142086835,755.5753957434),

new Point(191.5331469979,768.2553881275),

new Point(215.6809010731,768.2553881275),

new Point(233.4998393876,755.5753957434),

new Point(233.4998393876,736.5844033524),

new Point(215.6809010731,723.9044109683),

},


new List<Point> {


new Point(112.118900295,837.5444942668),

new Point(92.4911887501,851.7340460178),

new Point(92.4911887501,873.5822439061),

new Point(112.118900295,887.7717956571),

new Point(138.1022798363,887.7717956571),

new Point(157.7299913812,873.5822439061),

new Point(157.7299913812,851.7340460178),

new Point(138.1022798363,837.5444942668),

},


new List<Point> {


new Point(185.7348287275,846.9515947378),

new Point(166.312545075,860.9697012274),

new Point(166.312545075,882.4933975732),

new Point(185.7348287275,896.5115040628),

new Point(211.5097306861,896.5115040628),

new Point(230.9320143385,882.4933975732),

new Point(230.9320143385,860.9697012274),

new Point(211.5097306861,846.9515947378),

},


new List<Point> {


new Point(253.5332340902,714.7986335325),

new Point(235.7142703381,727.4786471464),

new Point(235.7142703381,746.4696797198),

new Point(253.5332340902,759.1496933336),

new Point(277.6810139808,759.1496933336),

new Point(295.499977733,746.4696797198),

new Point(295.499977733,727.4786471464),

new Point(277.6810139808,714.7986335325),

},


new List<Point> {


new Point(237.4332191528,866.0554270956),

new Point(237.4332191528,896.0395855339),

new Point(279.4173775911,896.0395855339),

new Point(279.4173775911,866.0554270956),

},


new List<Point> {


new Point(265.3412673197,946.3795706007),

new Point(245.6025199455,960.6617902291),

new Point(245.6025199455,982.685384445),

new Point(265.3412673197,996.9676040734),

new Point(291.4373310784,996.9676040734),

new Point(311.1760784526,982.685384445),

new Point(311.1760784526,960.6617902291),

new Point(291.4373310784,946.3795706007),

},


new List<Point> {


new Point(280.8524994749,1082.5155724361),

new Point(280.8524994749,1239.4910205897),

new Point(659.968283585,1239.4910205897),

new Point(659.968283585,1082.5155724361),

},


new List<Point> {


new Point(460.6035617464,1091.6408273645),

new Point(439.7944199908,1106.8163728325),

new Point(439.7944199908,1130.5308018416),

new Point(460.6035617464,1145.7063473096),

new Point(487.7859104812,1145.7063473096),

new Point(508.5950522368,1130.5308018416),

new Point(508.5950522368,1106.8163728325),

new Point(487.7859104812,1091.6408273645),

},


new List<Point> {


new Point(396.9700235572,1171.2880405761),

new Point(396.9700235572,1203.7076722448),

new Point(439.389655226,1203.7076722448),

new Point(439.389655226,1171.2880405761),

},


new List<Point> {


new Point(312.9366244306,1181.1574757881),

new Point(295.1176606785,1193.837489402),

new Point(295.1176606785,1212.8285219754),

new Point(312.9366244306,1225.5085355892),

new Point(337.0844043212,1225.5085355892),

new Point(354.9033680734,1212.8285219754),

new Point(354.9033680734,1193.837489402),

new Point(337.0844043212,1181.1574757881),

},


new List<Point> {


new Point(357.8525688643,1188.1001487803),

new Point(357.8525688643,1205.4162398036),

new Point(393.1686598876,1205.4162398036),

new Point(393.1686598876,1188.1001487803),

},


new List<Point> {


new Point(-3.158014901,665.1111188568),

new Point(-3.158014901,1078.831602238),

new Point(451.3101430596,1078.831602238),

new Point(451.3101430596,665.1111188568),

},


new List<Point> {


new Point(11.841985099,770.9218846469),

new Point(11.841985099,1063.831602238),

new Point(395.4926418429,1063.831602238),

new Point(395.4926418429,770.9218846469),

},


new List<Point> {


new Point(95.7809167822,1003.4545041192),

new Point(95.7809167822,1029.8926705548),

new Point(134.2190832178,1029.8926705548),

new Point(134.2190832178,1003.4545041192),

},


new List<Point> {


new Point(245.5700410057,598.9615519365),

new Point(225.2073289486,613.7645177347),

new Point(225.2073289486,636.7737497809),

new Point(245.5700410057,651.5767155791),

new Point(272.2993325471,651.5767155791),

new Point(292.6620446043,636.7737497809),

new Point(292.6620446043,613.7645177347),

new Point(272.2993325471,598.9615519365),

},


new List<Point> {


new Point(636.2770415379,733.05005054),

new Point(636.2770415379,767.4882169756),

new Point(678.7152079735,767.4882169756),

new Point(678.7152079735,733.05005054),

},


new List<Point> {


new Point(141.7809167822,1003.4545041192),

new Point(141.7809167822,1028.8926705548),

new Point(181.2190832178,1028.8926705548),

new Point(181.2190832178,1003.4545041192),

},


new List<Point> {


new Point(457.9737284714,308.1801348026),

new Point(438.5268753417,322.2187463969),

new Point(438.5268753417,343.7812536031),

new Point(457.9737284714,357.8198651974),

new Point(483.7735646546,357.8198651974),

new Point(503.2204177843,343.7812536031),

new Point(503.2204177843,322.2187463969),

new Point(483.7735646546,308.1801348026),

},


new List<Point> {


new Point(456.7983150637,1154.9921656798),

new Point(456.7983150637,1189.8649886039),

new Point(495.6711379878,1189.8649886039),

new Point(495.6711379878,1154.9921656798),

},


new List<Point> {


new Point(448.3650423095,1243.1749907877),

new Point(448.3650423095,1263.4910205897),

new Point(483.6810721115,1263.4910205897),

new Point(483.6810721115,1243.1749907877),

},


new List<Point> {


new Point(611.8449476122,1180.6372255609),

new Point(611.8449476122,1208.5678677047),

new Point(646.7755897559,1208.5678677047),

new Point(646.7755897559,1180.6372255609),

},


new List<Point> {


new Point(564.5965427767,1173.8297523178),

new Point(564.5965427767,1206.4488680626),

new Point(605.2156585215,1206.4488680626),

new Point(605.2156585215,1173.8297523178),

},


new List<Point> {


new Point(516.952932442,1159.993217497),

new Point(498.9026442233,1172.8662890406),

new Point(498.9026442233,1192.2227303488),

new Point(516.952932442,1205.0958018924),

new Point(541.3354709384,1205.0958018924),

new Point(559.3857591571,1192.2227303488),

new Point(559.3857591571,1172.8662890406),

new Point(541.3354709384,1159.993217497),

},


new List<Point> {


new Point(592.787682388,674.9615519365),

new Point(572.4249703308,689.7645177347),

new Point(572.4249703308,712.7737497809),

new Point(592.787682388,727.5767155791),

new Point(619.5169739293,727.5767155791),

new Point(639.8796859865,712.7737497809),

new Point(639.8796859865,689.7645177347),

new Point(619.5169739293,674.9615519365),

},


new List<Point> {


new Point(520.787682388,674.9615519365),

new Point(500.4249703308,689.7645177347),

new Point(500.4249703308,712.7737497809),

new Point(520.787682388,727.5767155791),

new Point(547.5169739293,727.5767155791),

new Point(567.8796859865,712.7737497809),

new Point(567.8796859865,689.7645177347),

new Point(547.5169739293,674.9615519365),

},


new List<Point> {


new Point(522.0340749313,734.9515852187),

new Point(504.1276825412,747.7045647063),

new Point(504.1276825412,766.8337028093),

new Point(522.0340749313,779.5866822969),

new Point(546.270581386,779.5866822969),

new Point(564.1769737761,766.8337028093),

new Point(564.1769737761,747.7045647063),

new Point(546.270581386,734.9515852187),

},


new List<Point> {


new Point(584.3778715283,734.9515852187),

new Point(566.4714791382,747.7045647063),

new Point(566.4714791382,766.8337028093),

new Point(584.3778715283,779.5866822969),

new Point(608.6143779831,779.5866822969),

new Point(626.5207703732,766.8337028093),

new Point(626.5207703732,747.7045647063),

new Point(608.6143779831,734.9515852187),

},


new List<Point> {


new Point(243.7124610028,491.579038652),

new Point(243.7124610028,529.0234901993),

new Point(283.15691255,529.0234901993),

new Point(283.15691255,491.579038652),

},


new List<Point> {


new Point(397.156902678,454.8188347783),

new Point(397.156902678,492.1446339617),

new Point(437.4827018614,492.1446339617),

new Point(437.4827018614,454.8188347783),

},


new List<Point> {


new Point(441.7981808462,524.8964412845),

new Point(441.7981808462,543.5599512795),

new Point(478.4616908412,543.5599512795),

new Point(478.4616908412,524.8964412845),

},


new List<Point> {


new Point(776.0841729748,451.5287966958),

new Point(776.0841729748,476.6512277003),

new Point(810.2066039793,476.6512277003),

new Point(810.2066039793,451.5287966958),

},


new List<Point> {


new Point(923.1525243404,494.4145236184),

new Point(905.333586026,507.0945160026),

new Point(905.333586026,526.0855083936),

new Point(923.1525243404,538.7655007777),

new Point(947.3002784156,538.7655007777),

new Point(965.1192167301,526.0855083936),

new Point(965.1192167301,507.0945160026),

new Point(947.3002784156,494.4145236184),

},


new List<Point> {


new Point(747.4501729847,314.5666419616),

new Point(747.4501729847,609.7924361755),

new Point(1113.5110302124,609.7924361755),

new Point(1113.5110302124,314.5666419616),

},


new List<Point> {


new Point(578.8916667136,504.3946778071),

new Point(578.8916667136,537.668605354),

new Point(620.1655942605,537.668605354),

new Point(620.1655942605,504.3946778071),

},


new List<Point> {


new Point(839.9874429654,360.6325666532),

new Point(839.9874429654,379.9486576765),

new Point(867.3035339887,379.9486576765),

new Point(867.3035339887,360.6325666532),

},


new List<Point> {


new Point(495.9720209427,407.9087305464),

new Point(495.9720209427,550.6896564815),

new Point(727.5484585818,550.6896564815),

new Point(727.5484585818,407.9087305464),

},


new List<Point> {


new Point(510.971990332,449.3132699159),

new Point(510.971990332,480.6293609392),

new Point(554.2880813554,480.6293609392),

new Point(554.2880813554,449.3132699159),

},


new List<Point> {


new Point(414.9527954358,421.5795425576),

new Point(414.9527954358,445.6348061037),

new Point(455.0080589819,445.6348061037),

new Point(455.0080589819,421.5795425576),

},


new List<Point> {


new Point(651.171360463,475.8476622295),

new Point(651.171360463,512.2858286651),

new Point(699.6095268986,512.2858286651),

new Point(699.6095268986,475.8476622295),

},


new List<Point> {


new Point(893.4136577906,546.1479922761),

new Point(872.6559392218,561.2806211683),

new Point(872.6559392218,584.913820198),

new Point(893.4136577906,600.0464490903),

new Point(920.5438199329,600.0464490903),

new Point(941.3015385017,584.913820198),

new Point(941.3015385017,561.2806211683),

new Point(920.5438199329,546.1479922761),

},


new List<Point> {


new Point(861.1524243404,494.4145236184),

new Point(843.333486026,507.0945160026),

new Point(843.333486026,526.0855083936),

new Point(861.1524243404,538.7655007777),

new Point(885.3001784156,538.7655007777),

new Point(903.1191167301,526.0855083936),

new Point(903.1191167301,507.0945160026),

new Point(885.3001784156,494.4145236184),

},


new List<Point> {


new Point(816.3016121309,451.5287966958),

new Point(816.3016121309,476.6512277003),

new Point(848.4240431353,476.6512277003),

new Point(848.4240431353,451.5287966958),

},


new List<Point> {


new Point(1195.5965225395,-6.2192921806),

new Point(1195.5965225395,16.2192921806),

new Point(1229.0351069006,16.2192921806),

new Point(1229.0351069006,-6.2192921806),

},


new List<Point> {


new Point(1142.4510808355,-8.3078638972),

new Point(1122.0881951285,6.495246825),

new Point(1122.0881951285,29.504753175),

new Point(1142.4510808355,44.3078638972),

new Point(1169.1805486046,44.3078638972),

new Point(1189.5434343116,29.504753175),

new Point(1189.5434343116,6.495246825),

new Point(1169.1805486046,-8.3078638972),

},


new List<Point> {


new Point(408.3431126793,66.8244700994),

new Point(390.5241489272,79.5044837133),

new Point(390.5241489272,98.4955162867),

new Point(408.3431126793,111.1755299006),

new Point(432.4908925699,111.1755299006),

new Point(450.3098563221,98.4955162867),

new Point(450.3098563221,79.5044837133),

new Point(432.4908925699,66.8244700994),

},


new List<Point> {


new Point(457.6474930284,174.6924181787),

new Point(437.2847809712,189.4953839769),

new Point(437.2847809712,212.5046160231),

new Point(457.6474930284,227.3075818213),

new Point(484.3767845697,227.3075818213),

new Point(504.7394966269,212.5046160231),

new Point(504.7394966269,189.4953839769),

new Point(484.3767845697,174.6924181787),

},


new List<Point> {


new Point(285.6894447968,21.4953839769),

new Point(285.6894447968,44.5046160231),

new Point(306.0521568539,59.3075818213),

new Point(332.7814483953,59.3075818213),

new Point(353.1441604525,44.5046160231),

new Point(353.1441604525,21.4953839769),

new Point(332.7814483953,6.6924181787),

new Point(306.0521568539,6.6924181787),

},


new List<Point> {


new Point(275.2587877236,-3.158014901),

new Point(275.2587877236,161.158014901),

new Point(514.5750175256,161.158014901),

new Point(514.5750175256,-3.158014901),

},


new List<Point> {


new Point(287.1977194068,120.7809167822),

new Point(287.1977194068,149.2190832178),

new Point(320.6358858424,149.2190832178),

new Point(320.6358858424,120.7809167822),

},


new List<Point> {


new Point(307.3429126793,66.8244700994),

new Point(289.5239489272,79.5044837133),

new Point(289.5239489272,98.4955162867),

new Point(307.3429126793,111.1755299006),

new Point(331.4906925699,111.1755299006),

new Point(349.3096563221,98.4955162867),

new Point(349.3096563221,79.5044837133),

new Point(331.4906925699,66.8244700994),

},


new List<Point> {


new Point(460.1979194068,64.7809167822),

new Point(460.1979194068,103.2190832178),

new Point(502.6360858424,103.2190832178),

new Point(502.6360858424,64.7809167822),

},


new List<Point> {


new Point(352.258857113,73.2330762977),

new Point(352.258857113,97.549167321),

new Point(387.5749481363,97.549167321),

new Point(387.5749481363,73.2330762977),

},


new List<Point> {


new Point(354.8541238981,164.841985099),

new Point(354.8541238981,293.158014901),

new Point(587.1701537,293.158014901),

new Point(587.1701537,164.841985099),

},


new List<Point> {


new Point(529.6474930284,174.6924181787),

new Point(509.2847809712,189.4953839769),

new Point(509.2847809712,212.5046160231),

new Point(529.6474930284,227.3075818213),

new Point(556.3767845697,227.3075818213),

new Point(576.7394966269,212.5046160231),

new Point(576.7394966269,189.4953839769),

new Point(556.3767845697,174.6924181787),

},


new List<Point> {


new Point(526.4811169371,232.7809167822),

new Point(526.4811169371,257.2190832178),

new Point(565.9192833727,257.2190832178),

new Point(565.9192833727,232.7809167822),

},


new List<Point> {


new Point(385.6474930284,174.6924181787),

new Point(365.2847809712,189.4953839769),

new Point(365.2847809712,212.5046160231),

new Point(385.6474930284,227.3075818213),

new Point(412.3767845697,227.3075818213),

new Point(432.7394966269,212.5046160231),

new Point(432.7394966269,189.4953839769),

new Point(412.3767845697,174.6924181787),

},


new List<Point> {


new Point(386.9382488538,234.8244700994),

new Point(369.1192851016,247.5044837133),

new Point(369.1192851016,266.4955162867),

new Point(386.9382488538,279.1755299006),

new Point(411.0860287443,279.1755299006),

new Point(428.9049924965,266.4955162867),

new Point(428.9049924965,247.5044837133),

new Point(411.0860287443,234.8244700994),

},


new List<Point> {


new Point(431.8541932874,238.5722434841),

new Point(431.8541932874,273.8883345074),

new Point(472.1702843107,273.8883345074),

new Point(472.1702843107,238.5722434841),

},


new List<Point> {


new Point(477.7193819196,244.2706365943),

new Point(477.7193819196,274.2322730649),

new Point(516.6810183902,274.2322730649),

new Point(516.6810183902,244.2706365943),

},


new List<Point> {


new Point(575.0562458984,452.8912155468),

new Point(557.2372821463,465.5712291606),

new Point(557.2372821463,484.562261734),

new Point(575.0562458984,497.2422753479),

new Point(599.204025789,497.2422753479),

new Point(617.0229895412,484.562261734),

new Point(617.0229895412,465.5712291606),

new Point(599.204025789,452.8912155468),

},


new List<Point> {


new Point(670.0257979101,417.759163626),

new Point(649.6630858529,432.5621294242),

new Point(649.6630858529,455.5713614704),

new Point(670.0257979101,470.3743272687),

new Point(696.7550894515,470.3743272687),

new Point(717.1178015086,455.5713614704),

new Point(717.1178015086,432.5621294242),

new Point(696.7550894515,417.759163626),

},


new List<Point> {


new Point(596.2107313383,422.8870331048),

new Point(596.2107313383,450.2464577898),

new Point(640.5701560233,450.2464577898),

new Point(640.5701560233,422.8870331048),

},


new List<Point> {


new Point(855.9068376236,448.9165830325),

new Point(855.9068376236,479.2634413637),

new Point(887.2536959548,479.2634413637),

new Point(887.2536959548,448.9165830325),

},


new List<Point> {


new Point(871.2250352251,415.3709289803),

new Point(871.2250352251,440.8090954159),

new Point(903.6632016607,440.8090954159),

new Point(903.6632016607,415.3709289803),

},


new List<Point> {


new Point(521.1058122791,360.8912568677),

new Point(503.2868739647,373.5712492518),

new Point(503.2868739647,392.5622416428),

new Point(521.1058122791,405.242234027),

new Point(545.2535663543,405.242234027),

new Point(563.0725046687,392.5622416428),

new Point(563.0725046687,373.5712492518),

new Point(545.2535663543,360.8912568677),

},


new List<Point> {


new Point(566.021743805,371.5556362636),

new Point(566.021743805,388.8717272869),

new Point(594.3378348284,388.8717272869),

new Point(594.3378348284,371.5556362636),

},


new List<Point> {


new Point(795.0714985317,344.0863275534),

new Point(777.2525347796,356.7663411672),

new Point(777.2525347796,375.7573737406),

new Point(795.0714985317,388.4373873545),

new Point(819.2192784223,388.4373873545),

new Point(837.0382421745,375.7573737406),

new Point(837.0382421745,356.7663411672),

new Point(819.2192784223,344.0863275534),

},


new List<Point> {


new Point(762.987373576,330.103842553),

new Point(762.987373576,491.7480270991),

new Point(1040.973829621,491.7480270991),

new Point(1040.973829621,330.103842553),

},


new List<Point> {


new Point(984.7419247748,355.0443674194),

new Point(966.9229610226,367.7243810333),

new Point(966.9229610226,386.7154136067),

new Point(984.7419247748,399.3954272205),

new Point(1008.8897046653,399.3954272205),

new Point(1026.7086684175,386.7154136067),

new Point(1026.7086684175,367.7243810333),

new Point(1008.8897046653,355.0443674194),

},


new List<Point> {


new Point(1051.5967315022,327.0427742361),

new Point(1051.5967315022,361.4809406717),

new Point(1101.0348979379,361.4809406717),

new Point(1101.0348979379,327.0427742361),

},


new List<Point> {


new Point(872.7077201073,375.9822182705),

new Point(872.7077201073,404.0867297343),

new Point(912.8122315712,404.0867297343),

new Point(912.8122315712,375.9822182705),

},


new List<Point> {


new Point(924.6576692084,366.7039167404),

new Point(924.6576692084,398.0200077637),

new Point(963.9737602317,398.0200077637),

new Point(963.9737602317,366.7039167404),

},


new List<Point> {


new Point(-445.8608139093,-451.0106629056),

new Point(-445.8608139093,1706.193819598),

new Point(1671.737905909,1706.193819598),

new Point(1671.737905909,-451.0106629056),

}


};
        }
    }

}
