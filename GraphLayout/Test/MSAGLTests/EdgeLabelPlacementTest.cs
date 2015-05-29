//-----------------------------------------------------------------------
// <copyright file="EdgeLabelPlacementTest.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// This is a test class for EdgeLabelPlacementTest and is intended
    /// to contain all EdgeLabelPlacementTest Unit Tests
    /// </summary>
    [TestClass()]
    public class EdgeLabelPlacementTest : MsaglTestBase
    {
        #region ExpandingSearch

        /// <summary>
        /// Increasing only
        /// </summary>
        [TestMethod]
        public void ExpandingSearchTest_IncreasingOnly()
        {
            int[] expected = new int[] { 0, 1, 2, 3, 4 };
            var r = EdgeLabelPlacement.ExpandingSearch(0, 0, 5).ToList();
            CollectionAssert.AreEqual(expected, r);
        }

        /// <summary>
        /// Decreasing only
        /// </summary>
        [TestMethod]
        public void ExpandingSearchTest_DecreasingOnly()
        {
            int[] expected = new int[] { 4, 3, 2, 1, 0 };
            var r = EdgeLabelPlacement.ExpandingSearch(4, 0, 5).ToList();
            CollectionAssert.AreEqual(expected, r);
        }

        /// <summary>
        /// Expanding search from the middle of the array out.  Step forward, step backward, etc.
        /// 5, 6, 4, 7, 3, 8, 2, 9, 0
        /// </summary>
        [TestMethod]
        public void ExpandingSearchTest_MiddleOut()
        {
            int[] expected = new int[] { 5, 6, 4, 7, 3, 8, 2, 9, 1, 0 };
            var r = EdgeLabelPlacement.ExpandingSearch(5, 0, 10).ToList();
            CollectionAssert.AreEqual(expected, r);
        }

        /// <summary>
        /// Expanding search from the middle of the array out.  Step forward, step backward, etc.
        /// 3, 4, 2, 5, 1, 6, 0, 7, 8, 9
        /// </summary>
        [TestMethod]
        public void ExpandingSearchTest_LessThanMiddle()
        {
            var expected = new int[] { 3, 4, 2, 5, 1, 6, 0, 7, 8, 9 };
            var r = EdgeLabelPlacement.ExpandingSearch(3, 0, 10).ToList();
            CollectionAssert.AreEqual(expected, r);
        }

        /// <summary>
        /// Expanding search from the middle of the array out.  Step forward, step backward, etc.
        /// 7, 8, 6, 9, 5, 4, 3, 2, 1, 0
        /// </summary>
        [TestMethod]
        public void ExpandingSearchTest_BiggerThanMiddle()
        {
            var expected = new int[] { 7, 8, 6, 9, 5, 4, 3, 2, 1, 0 };
            var r = EdgeLabelPlacement.ExpandingSearch(7, 0, 10).ToList();
            CollectionAssert.AreEqual(expected, r);
        }

        #endregion ExpandingSearch

        #region GetPossibleSides

        [TestMethod]
        [Description("Verifies that GetPossibleSides results in the correct search direction for various lines.")]
        public void GetPossibleSides()
        {
            for (double angle = 0; angle < 2 * Math.PI; angle += Math.PI / 4)
            {
                double lineLength = 10;
                Point targetPoint = new Point(lineLength * Math.Sin(angle), lineLength * Math.Cos(angle));
                LineSegment line = new LineSegment(new Point(0, 0), targetPoint);

                Point derivative = line.Derivative(0.5);

                double[] topSides = GetPossibleSides(Label.PlacementSide.Top, derivative).ToArray();
                Assert.AreEqual(1, topSides.Length, "Top placement side should have 1 resulting possible side");
                Point searchDir = GetSearchDirection(derivative, topSides[0]);
                Assert.IsTrue(searchDir.Y <= 0, "Top placement side should result in a Y negative search");

                double[] bottomSides = GetPossibleSides(Label.PlacementSide.Bottom, derivative).ToArray();
                Assert.AreEqual(1, bottomSides.Length, "Bottom placement side should have 1 resulting possible side");
                searchDir = GetSearchDirection(derivative, bottomSides[0]);
                Assert.IsTrue(searchDir.Y >= 0, "Bottom placement side should result in a Y positive search");

                double[] leftSides = GetPossibleSides(Label.PlacementSide.Left, derivative).ToArray();
                Assert.AreEqual(1, leftSides.Length, "Left placement side should have 1 resulting possible side");
                searchDir = GetSearchDirection(derivative, leftSides[0]);
                Assert.IsTrue(searchDir.X <= 0, "Left placement side should result in a X negative search");
                
                double[] rightSides = GetPossibleSides(Label.PlacementSide.Right, derivative).ToArray();
                Assert.AreEqual(1, rightSides.Length, "Right placement side should have 1 resulting possible side");
                searchDir = GetSearchDirection(derivative, rightSides[0]);
                Assert.IsTrue(searchDir.X >= 0, "Right placement side should result in a X positive search");

                double[] portSide = GetPossibleSides(Label.PlacementSide.Port, derivative).ToArray();
                Assert.AreEqual(1, portSide.Length, "Port placement side should have 1 resulting possible side");
                Assert.AreEqual(-1, portSide[0], "Port placement side should be -1");

                double[] starboardSide = GetPossibleSides(Label.PlacementSide.Starboard, derivative).ToArray();
                Assert.AreEqual(1, starboardSide.Length, "Starboard placement side should have 1 resulting possible side");
                Assert.AreEqual(1, starboardSide[0], "Starboard placement side should be 1");

                double[] anySide = GetPossibleSides(Label.PlacementSide.Any, derivative).ToArray();
                Assert.AreEqual(2, anySide.Length, "Any placement side should have 2 resulting possible sides");
                Assert.AreEqual(-1, anySide[0], "Any placement side should start with -1");
                Assert.AreEqual(1, anySide[1], "Any placement side should end with 1");
            }
        }

        private static Point GetSearchDirection(Point derivative, double side)
        {
            Point direction = derivative.Rotate(Math.PI / 2).Normalize() * side;

            // Rotating can cause tiny drift in the X/Y values.  Round so that 0 actually equals 0
            direction = new Point(Math.Round(direction.X, 5), Math.Round(direction.Y, 5));

            return direction;
        }

        private static IEnumerable<double> GetPossibleSides(Label.PlacementSide side, Point derivative)
        {
            MethodInfo methodInfo = typeof(EdgeLabelPlacement).GetMethod("GetPossibleSides", BindingFlags.Static | BindingFlags.NonPublic);
            return (IEnumerable<double>)methodInfo.Invoke(null, new object[] { side, derivative });
        }

        #endregion
    }
}
