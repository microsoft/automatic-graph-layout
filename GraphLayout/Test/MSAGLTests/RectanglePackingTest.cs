//-----------------------------------------------------------------------
// <copyright file="RectanglePackingTest.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// This is a test class for RectanglePackingRectanglePackingTest and is intended
    /// to contain all RectanglePackingRectanglePackingTest Unit Tests
    /// </summary>
    [TestClass]
    public class RectanglePackingTest
    {
        private TestContext testContextInstance;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        [Description("Packing of 2 unit squares with aspect ratio phi should give two rectangles side-by-side")]
        public void RectanglePackingTwoSquares()
        {
            List<RectangleToPack<int>> rectangles = new List<RectangleToPack<int>>();
            for (int i = 0; i < 2; ++i)
            {
                rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(0, 0), new Point(1, 1)), i));
            }
            var rectanglePacking = new OptimalRectanglePacking<int>(rectangles, PackingConstants.GoldenRatio);
            rectanglePacking.Run();
            Assert.AreEqual(2, rectanglePacking.PackedWidth, "packing is wrong width");
            Assert.AreEqual(1, rectanglePacking.PackedHeight, "packing is wrong height");
            Assert.IsFalse(IsOverlapping(rectangles), "There are overlaps between the packed rectangles");
        }

        [TestMethod]
        [Description("Packing of 9 unit squares with an aspect ratio of 1.0 should return a 3X3 grid")]
        public void RectanglePackingNineSquares()
        {
            List<RectangleToPack<int>> rectangles = new List<RectangleToPack<int>>();
            for (int i = 0; i < 9; ++i)
            {
                rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(0, 0), new Point(1, 1)), i));
            }
            RectanglePacking<int> rectanglePacking = new RectanglePacking<int>(rectangles, 3.0);
            rectanglePacking.Run();
            Assert.AreEqual(3, rectanglePacking.PackedWidth, "packing is wrong width");
            Assert.AreEqual(3, rectanglePacking.PackedHeight, "packing is wrong height");
            Assert.IsFalse(IsOverlapping(rectangles), "There are overlaps between the packed rectangles");
        }

        [TestMethod]
        [Description("Rect: 1x2 + two unit squares, should pack to 2x2")]
        public void RectanglePackingTallRectAndTwoSquares()
        {
            List<RectangleToPack<int>> rectangles = new List<RectangleToPack<int>>();
            rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(0, 0), new Point(1, 2)), 0));
            rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(0, 0), new Point(1, 1)), 1));
            rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(0, 0), new Point(1, 1)), 2));
            RectanglePacking<int> rectanglePacking = new RectanglePacking<int>(rectangles, 2.0);
            rectanglePacking.Run();
            Assert.AreEqual(2, rectanglePacking.PackedWidth, "packing is wrong width");
            Assert.AreEqual(2, rectanglePacking.PackedHeight, "packing is wrong height");
            Assert.IsFalse(IsOverlapping(rectangles), "There are overlaps between the packed rectangles");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "rectangles")]
        private static void ShowDebugView(List<RectangleToPack<int>> rectangles)
        {
#if TEST_MSAGL
            if (!MsaglTestBase.EnableDebugViewer())
            {
                return;
            }
            var shapes = from r in rectangles select new DebugCurve(CurveFactory.CreateRectangle(r.Rectangle));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(shapes);
#endif
        }

        [TestMethod]
        [Description("Five rectangles of different heights that should fit into 3x3 bounding box")]
        public void SimpleRectanglesDifferentHeights()
        {
            List<RectangleToPack<int>> rectangles = new List<RectangleToPack<int>>();
            rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(), new Point(1, 3)), 0));
            rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(), new Point(1, 2)), 1));
            rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(), new Point(1, 2)), 2));
            rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(), new Point(1, 1)), 3));
            rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(), new Point(1, 1)), 4));
            const double Scale = 100;
            foreach (var r in rectangles)
            {
                r.Rectangle = new Rectangle(new Point(), new Point(r.Rectangle.Width * Scale, r.Rectangle.Height * Scale));
            }

            // shuffle
            rectangles = rectangles.OrderBy(x => Guid.NewGuid()).ToList();

            RectanglePacking<int> rectanglePacking = new RectanglePacking<int>(rectangles, 3 * Scale);
            rectanglePacking.Run();
            Assert.AreEqual(3 * Scale, rectanglePacking.PackedWidth, "packing is wrong width");
            Assert.AreEqual(3 * Scale, rectanglePacking.PackedHeight, "packing is wrong height");
            Assert.IsFalse(IsOverlapping(rectangles), "There are overlaps between the packed rectangles");
            ShowDebugView(rectangles);
        }

        [TestMethod]
        [Description("pack random rectangles")]
        public void RandomRectangles()
        {
            const int N = 100;
            Random rand = new Random(0);
            double desiredAspectRatio = 1;
            List<RectangleToPack<int>> rectangles = new List<RectangleToPack<int>>();
            double area = 0;
            const double Scale = 100;
            for (int i = 0; i < N; ++i)
            {
                double width = Scale * rand.NextDouble();
                double height = Scale * rand.NextDouble();
                area += width * height;
                rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(0, 0), new Point(width, height)), i));
            }

            double maxWidth = Math.Sqrt(area);
            RectanglePacking<int> rectanglePacking = new RectanglePacking<int>(rectangles, maxWidth);
            rectanglePacking.Run();
            double appoxAspectRatio = rectanglePacking.PackedWidth / rectanglePacking.PackedHeight;
            Assert.IsTrue(rectanglePacking.PackedWidth < maxWidth, "Packing is wider than the max width we specified");
            Assert.IsFalse(IsOverlapping(rectangles), "There are overlaps between the packed rectangles");

            OptimalRectanglePacking<int> optimalRectanglePacking = new OptimalRectanglePacking<int>(rectangles, desiredAspectRatio);
            optimalRectanglePacking.Run();
            double optimalAspectRatio = optimalRectanglePacking.PackedWidth / optimalRectanglePacking.PackedHeight;

            Assert.IsTrue(Math.Abs(appoxAspectRatio - desiredAspectRatio) > Math.Abs(optimalAspectRatio - desiredAspectRatio), "aspect ratio calculated by OptimalRectanglePacking was not better than regular RectanglePacking");

            Assert.IsFalse(IsOverlapping(rectangles), "There are overlaps between the packed rectangles");
            ShowDebugView(rectangles);
        }


        [TestMethod]
        [Description("pack random rectangles")]
        public void PowerLawRandomRectangles()
        {
            const int N = 100;
            Random rand = new Random(0);
            double desiredAspectRatio = 1;
            List<RectangleToPack<int>> rectangles = new List<RectangleToPack<int>>();
            double area = 0;
            const double Scale = 100;
            for (int i = 0; i < N; ++i)
            {
                double s = Scale * Math.Pow(2, i / 10.0);
                double width = s * rand.NextDouble();
                double height = s * rand.NextDouble();
                area += width * height;
                rectangles.Add(new RectangleToPack<int>(new Rectangle(new Point(0, 0), new Point(width, height)), i));
            }

            double maxWidth = Math.Sqrt(area);
            RectanglePacking<int> rectanglePacking = new RectanglePacking<int>(rectangles, maxWidth);
            rectanglePacking.Run();
            ShowDebugView(rectangles);
            double appoxAspectRatio = rectanglePacking.PackedWidth / rectanglePacking.PackedHeight;
            Assert.IsTrue(rectanglePacking.PackedWidth < maxWidth, "Packing is wider than the max width we specified");
            Assert.IsFalse(IsOverlapping(rectangles), "There are overlaps between the packed rectangles");

            OptimalRectanglePacking<int> optimalRectanglePacking = new OptimalRectanglePacking<int>(rectangles, desiredAspectRatio);
            optimalRectanglePacking.Run();
            double optimalAspectRatio = optimalRectanglePacking.PackedWidth / optimalRectanglePacking.PackedHeight;
            ShowDebugView(rectangles);

            Assert.IsTrue(Math.Abs(appoxAspectRatio - desiredAspectRatio) > Math.Abs(optimalAspectRatio - desiredAspectRatio), "aspect ratio calculated by OptimalRectanglePacking was not better than regular RectanglePacking");

            Assert.IsFalse(IsOverlapping(rectangles), "There are overlaps between the packed rectangles");
        }

        [TestMethod]
        [Description("test golden section algorithm with f(x) = x^2")]
        public void GoldenSectionTest()
        {
            const double Precision = 1e-2;
            double xopt = OptimalRectanglePacking<int>.GoldenSectionSearch(x => x * x, -1, -0.2, 1, Precision);
            Assert.AreEqual(0, xopt, Precision, "GoldenSectionSearch didn't find correct root for f(x)=x^2");
        }

        /// <summary>
        /// fool-proof overlap test
        /// </summary>
        /// <param name="rectangles">rectangles to check</param>
        /// <returns>true if overlap exists</returns>
        private static bool IsOverlapping(IEnumerable<Rectangle> rectangles)
        {
            var rs = rectangles.ToArray();
            for (int i = 0; i < rs.Length; ++i)
            {
                rs[i].Pad(-0.01);
            }
            for (int i = 0; i < rs.Length - 1; ++i)
            {
                for (int j = i + 1; j < rs.Length; ++j)
                {
                    if (rs[i].Intersects(rs[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// convenience overload for PackingRectangle for
        /// </summary>
        /// <param name="rectangles">rectangles to check</param>
        /// <returns>true if overlap exists</returns>
        private static bool IsOverlapping(IEnumerable<RectangleToPack<int>> rectangles)
        {
            return IsOverlapping(from r in rectangles select r.Rectangle);
        }
    }
}
