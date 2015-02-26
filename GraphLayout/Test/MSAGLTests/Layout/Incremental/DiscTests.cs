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
﻿//-----------------------------------------------------------------------
// <copyright file="DiscTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Additional tests for Circle at Microsoft.Msagl.Layout.Incremental
    /// </summary>
    [TestClass]
    public class DiscTests : MsaglTestBase
    {
        private const double Tolerance = 0.0001;
        private const int MaxValue = 500;
        private Random random = new Random(999);

        #region Test
        [TestMethod]
        [WorkItem(443732)]
        [Description("Collinear three points case for disc")]
        public void CollinearThreePointsDiscTests()
        {
            WriteLine("Try collinear case first");
            Point p1 = new Point(random.Next(MaxValue), random.Next(MaxValue));
            Point p2 = new Point(p1.X - 100, p1.Y);
            Point p3 = new Point(p1.X + 100, p1.Y);
            Disc disc = new Disc(p1, p2, p3);
            WriteLine(string.Format("p1 {0}, p2 {1} and p3 {2}", p1, p2, p3));
            Assert.AreEqual(disc.Radius, (disc.Center - p2).Length, Tolerance, string.Format("point p2 {0} not on the disc centered {1} with radius {2}", p2, disc.Center, disc.Radius));
            Assert.AreEqual(disc.Radius, (disc.Center - p3).Length, Tolerance, string.Format("point p3 {0} not on the disc centered {1} with radius {2}", p3, disc.Center, disc.Radius));

            WriteLine("Try non-collinear case next");
            p2 = new Point(random.Next(MaxValue), random.Next(MaxValue));
            p3 = new Point(random.Next(MaxValue), random.Next(MaxValue));
            MakePointsNotSame(ref p1, ref p2);
            MakePointsNotSame(ref p1, ref p3);
            MakePointsNotSame(ref p2, ref p3);
            disc = new Disc(p1, p2, p3);
            WriteLine(string.Format("p1 {0}, p2 {1} and p3 {2}", p1, p2, p3));
            Assert.AreEqual(disc.Radius, (disc.Center - p1).Length, Tolerance, string.Format("point p1 {0} not on the disc centered {1} with radius {2}", p1, disc.Center, disc.Radius));
            Assert.AreEqual(disc.Radius, (disc.Center - p2).Length, Tolerance, string.Format("point p2 {0} not on the disc centered {1} with radius {2}", p2, disc.Center, disc.Radius));
            Assert.AreEqual(disc.Radius, (disc.Center - p3).Length, Tolerance, string.Format("point p3 {0} not on the disc centered {1} with radius {2}", p3, disc.Center, disc.Radius));
        }

        [TestMethod]
        [Description("Non Collinear three points case for disc")]
        public void NonCollinearThreePointsDiscTests()
        {
            WriteLine("Try non-colliear case next");
            Point p1 = new Point(random.Next(MaxValue), random.Next(MaxValue));
            Point p2 = new Point(random.Next(MaxValue), random.Next(MaxValue));
            Point p3 = new Point(random.Next(MaxValue), random.Next(MaxValue));
            MakePointsNotSame(ref p1, ref p2);
            MakePointsNotSame(ref p1, ref p3);
            MakePointsNotSame(ref p2, ref p3);
            Disc disc = new Disc(p1, p2, p3);
            WriteLine(string.Format("p1 {0}, p2 {1} and p3 {2}", p1, p2, p3));
            Assert.AreEqual(disc.Radius, (disc.Center - p1).Length, Tolerance, string.Format("point p1 {0} not on the disc centered {1} with radius {2}", p1, disc.Center, disc.Radius));
            Assert.AreEqual(disc.Radius, (disc.Center - p2).Length, Tolerance, string.Format("point p2 {0} not on the disc centered {1} with radius {2}", p2, disc.Center, disc.Radius));
            Assert.AreEqual(disc.Radius, (disc.Center - p3).Length, Tolerance, string.Format("point p3 {0} not on the disc centered {1} with radius {2}", p3, disc.Center, disc.Radius));
        }
       
        [TestMethod]
        [Description("Testing bool Contains(points, except)")]
        public void ContainingTests()
        {
            Disc disc = new Disc(new Point(10, 0), new Point(-10, 0));
            Point[] points = new Point[] { new Point(0, 10), new Point(0, 12), new Point(0, -10), new Point(20, 0) };
            WriteLine("Trying correct except");
            int[] except = new int[] { 1, 3 };
            Assert.IsTrue(disc.Contains(points, except), "ContainingTest not working");
            WriteLine("Trying incomplete except");
            except = new int[] { 1 };
            Assert.IsFalse(disc.Contains(points, except), "ContainingTest not working");
            WriteLine("Trying empty and incorrect except");
            except = new int[] { };
            Assert.IsFalse(disc.Contains(points, except), "ContainingTest not working");
        }

        #endregion

        #region Helpers
        private static void MakePointsNotSame(ref Point p1, ref Point p2)
        {
            if (Math.Abs(p2.X - p1.X) <= Tolerance)
            {
                p2.X += 100;
            }
            if (Math.Abs(p2.Y - p1.Y) <= Tolerance)
            {
                p2.Y += 100;
            }
        }
        #endregion
    }
}
