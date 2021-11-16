using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;
using Microsoft.Msagl.Routing.Visibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests.Routing {
    [TestClass]
    public class TestLineSweeper {
        [TestMethod]
        public void TwoPorts() {
            var obstacles = new List<Polyline>();

            var direction = new Point(0, 1);
            var vg = new VisibilityGraph();
            var ports = new Set<Point>();
            ports.Insert(new Point(0, 0));
            ports.Insert(new Point(0.1, 10));
            Polyline border = null;
            LineSweeper.Sweep(
              obstacles,
              direction,
              Math.PI / 6,
              vg,
              ports,
              border
            );

        }
        [TestMethod]
        public void TwoInACone() {
            var obstacles = new List<Polyline>();

            var direction = new Point(0, 1);
            var vg = new VisibilityGraph();
            var ports = new Set<Point>();
            ports.Insert(new Point(0, 0));
            ports.Insert(new Point(0.1, 10));
            ports.Insert(new Point(-0.1, 10));
            Polyline border = null;
            LineSweeper.Sweep(
              obstacles,
              direction,
              Math.PI / 6,
              vg,
              ports,
              border
            );
            Assert.AreEqual(vg.Edges.Count(), 1);
        }
        [TestMethod]
        public void TwoInAConeLargerOffset() {
            var obstacles = new List<Polyline>();

            var direction = new Point(0, 1);
            var vg = new VisibilityGraph();
            var points = new List<Point> {
                new Point(0, 0),
                new Point(0.1, 10),
                new Point(-0.1, 20)
            };
            var ports = new Set<Point>();
            ports.Insert(points[0]);
            ports.Insert(points[1]);
            ports.Insert(points[2]);
            Polyline border = null;
            LineSweeper.Sweep(
              obstacles,
              direction,
              Math.PI / 6,
              vg,
              ports,
              border
            );
            Assert.AreEqual(vg.Edges.Count(), 2);
            var orig = vg.FindVertex(points[0]);
            var outOrig = new List<VisibilityEdge>(orig.OutEdges);
            Assert.AreEqual(outOrig.Count, 1);
            var v10 = vg.FindVertex(points[1]);
            Assert.AreEqual(v10.OutEdges.Count(), 1);
            //Assert.IsTrue(vg.FindEdge(


        }



        [TestMethod]
        public void RandomPorts() {
            GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
            for (int i = 0; i < 10; i++) {
                RunOnRandom(i);
            }

        }

        private static void RunOnRandom(int i) {
            var ran = new Random(i);
            var ps = new List<Point>();
            for (int j = 0; j < 10; j++) {
                ps.Add(20 * (new Point(ran.NextDouble(), ran.NextDouble())));

            }
            var vg = new VisibilityGraph();
            var dir = new Point(0, 1);
            LineSweeper.Sweep(
              new List<Polyline>(),
              dir,
              Math.PI / 6,
              vg,
              new Set<Point>(ps),
              null
            );
            CheckVG(vg, ps, dir);
            //Msagl.Routing.SplineRouter.ShowVisGraph(vg, null, null, null);
        }

        private static void CheckVG(VisibilityGraph vg, List<Point> ps, Point dir) {
            foreach (var p in ps) {
                CheckVGOnPoint(p, vg, ps, dir);
            }
            
        }

        private static void CheckVGOnPoint(Point p, VisibilityGraph vg, List<Point> ps, Point dir) {
            var inCone = new List<Point>(ps.Where(q => p != q && InCone(p, q, dir, Math.PI / 6)));


            var v = vg.FindVertex(p);
            Assert.IsTrue(
                (inCone.Count == 0 
                && 
                (v==null || (v.OutEdges.Count == 0&& v.InEdges.Count()> 0) ))
                || vg.FindVertex(p).OutEdges.Count == 1);
        }

        private static bool InCone(Point apex, Point q, Point dir, double ang) {
            return InCone(q, apex + dir.Rotate(ang / 2), apex, apex + dir.Rotate(-ang / 2));
        }

        static bool InCone(Point pi, Point a, Point b, Point c ) {
            Debug.Assert(Point.GetTriangleOrientation(a, b, c) == TriangleOrientation.Counterclockwise);

            return Point.GetTriangleOrientation(a, pi, b) == TriangleOrientation.Clockwise &&
                Point.GetTriangleOrientation(b, pi, c) == TriangleOrientation.Clockwise;
        }

    }
}
