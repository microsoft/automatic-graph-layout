using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Layout.OverlapRemovalFixedSegments;
using System;
using Microsoft.Msagl.Miscellaneous.RegularGrid;
using Microsoft.Msagl.GraphmapsWpfControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using  SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;

namespace TestGraphmaps
{
    [TestClass]
    public class Test1 {
        public static void AssemblyInitialize(TestContext testContext) {}

        public static void RunTest1() {
            Rectangle rect1 = new Rectangle(0, 0, 2, 2);
            Rectangle rect2 = new Rectangle(3, 3, 4, 4);
            Rectangle rect3 = new Rectangle(4, 1, 5, 2);

            Rectangle[] moveableRectangles = {rect1, rect2, rect3};
            Rectangle[] fixedRectangles = {};
            SymmetricSegment[] fixedSegments = {};

            OverlapRemovalFixedSegmentsMst orfs = new OverlapRemovalFixedSegmentsMst(moveableRectangles, fixedRectangles,
                fixedSegments);
            orfs.InitCdt();

            double dist = orfs.GetDistance(rect1, rect2);

            var mstEdges = orfs.GetMstFromCdt();
        }

        public static void RunTest2() {
            Point p1, p2;
            Rectangle rect1 = new Rectangle(1, 1, 4, 4);
            Point pc1, pc2;
            double a;


            p1 = new Point(-1, 0);
            p2 = new Point(2, 3);
            RectSegIntersection.ClipOnRect(rect1, p1, p2, out pc1, out pc2);

            a = RectSegIntersection.GetOverlapAmount(rect1, p1, p2);

            p1 = new Point(1, 0);
            p2 = new Point(5, 4);
            a = RectSegIntersection.GetOverlapAmount(rect1, p1, p2);

            p1 = new Point(0, 4);
            p2 = new Point(4, 0);
            a = RectSegIntersection.GetOverlapAmount(rect1, p1, p2);

            p1 = new Point(0, 4);
            p2 = new Point(4, 0);
            a = RectSegIntersection.GetOverlapAmount(rect1, p1, p2);

            p1 = new Point(1, 5);
            p2 = new Point(5, 1);
            a = RectSegIntersection.GetOverlapAmount(rect1, p1, p2);

            p1 = new Point(0, 1);
            p2 = new Point(5, 4);
            a = RectSegIntersection.GetOverlapAmount(rect1, p1, p2);

            p1 = new Point(-1, 3);
            p2 = new Point(2, 6);
            a = RectSegIntersection.GetOverlapAmount(rect1, p1, p2);

            bool intersect;

            p1 = new Point(-1, 3);
            p2 = new Point(2, 6);
            intersect = RectSegIntersection.Intersect(rect1, p1, p2);

            p1 = new Point(1, 2);
            p2 = new Point(1, 3);
            intersect = RectSegIntersection.Intersect(rect1, p1, p2);
            a = RectSegIntersection.GetOverlapAmount(rect1, p1, p2);
        }

        public static void RunTest3() {
            var moveableRects = new Rectangle[0];
            var fixedRects = new Rectangle[1];
            fixedRects[0] = new Rectangle(-10, -20, 50, 60);
            SymmetricSegment[] fixedSegments = new SymmetricSegment[0];
            var orb = new OverlapRemovalFixedSegmentsBitmap(moveableRects, fixedRects, fixedSegments);
            
        }

        public static void RunTest4() {
            Rectangle rect = new Rectangle(1, 1, 4, 4);
            Point moveDir = new Point(0, 1);
            Point p1 = new Point(1, 3);
            Point p2 = new Point(4, 3);

            var delta = RectSegIntersection.GetOrthShiftUntilNoLongerOverlapRectSeg(rect, p1, p2, moveDir);
        }



        [TestMethod]

        public void RunTest6() {
            Point bl = new Point(0, 0);
            Point p1 = new Point(0.5, 1.5);
            Point p2 = new Point(9.5, 10.1);

            GridTraversal grid = new GridTraversal(new Rectangle(bl, bl + new Point(20, 15)), 2);
            var tiles = grid.GetTilesIntersectedByLineSeg(p1, p2);

#if TEST_MSAGL
            Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions(); 
            ShowTiles(tiles, grid, p1, p2);
#endif
        }

#if TEST_MSAGL
        static void ShowTiles(List<Tuple<int, int>> tiles, GridTraversal grid, Point p1, Point p2) {
            var ll = tiles.Select(t => grid.GetTileRect(t.Item1, t.Item2)).Select(r => new DebugCurve("black", r.Perimeter())).ToList();
            ll.Add(new DebugCurve("red", new LineSegment(p1, p2)));
            LayoutAlgorithmSettings.ShowDebugCurves(ll.ToArray());
        }
#endif

        [TestMethod]
        public void RunTest7() {
            Point p1 = new Point(-497.12352212078628, 1689.84931190121);
            Point p2 = new Point(198.64235142705752, 2139.4677380013277);
            Point bl = new Point(-5191.0147700187063, -4395.7850131819132);
            double gridSize = 553.23948409846571;

            GridTraversal grid = new GridTraversal(new Rectangle(bl, bl + new Point(gridSize, gridSize)), 20);
            var tiles = grid.GetTilesIntersectedByLineSeg(p1, p2);
#if TEST_MSAGL
            Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
            ShowTiles(tiles, grid, p1, p2);
#endif
        }
    }
}
