//-----------------------------------------------------------------------
// <copyright file="RTreeTest.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// This is a test class for RTreeTest and is intended
    /// to contain all RTreeTest Unit Tests
    /// </summary>
    [TestClass()]
    public class RTreeTest : MsaglTestBase {
        [TestMethod]
        public void RTreeQuery_RandomPoints() {
            var bspQueryTime = new Stopwatch();
            var checkQueryTime = new Stopwatch();
            const int Seeds = 5;
            const int Repeats = 5;
            for (int seed = 0; seed < Seeds; ++seed) {
                int n = 100000;
                var points = new Point[n];
                var rand = new Random(seed);
                double scale = 1000;
                for (int i = 0; i < n; ++i) {
                    points[i] = new Point(rand.NextDouble() * scale, rand.NextDouble() * scale);
                }
                var queryTree = new RTree<Point, Point>(
                    from p in points
                    select new KeyValuePair<IRectangle<Point>, Point>(new Rectangle(p), p));
                Assert.AreEqual(queryTree.GetAllLeaves().Count(), n);
                Assert.AreEqual(queryTree.GetAllIntersecting(new Rectangle(-2, -2, -1, -1)).Count(), 0);
                Assert.AreEqual(queryTree.GetAllIntersecting(new Rectangle(0, 0, scale, scale)).Count(), n);
                for (int i = 0; i < Repeats; ++i) {
                    double s = scale / 100;
                    var query = new Rectangle(rand.NextDouble() * s, rand.NextDouble() * s, rand.NextDouble() * s, rand.NextDouble() * s);
                    bspQueryTime.Start();
                    var result = queryTree.GetAllIntersecting(query).ToList();
                    bspQueryTime.Stop();
                    checkQueryTime.Start();
                    var checkList = (from p in points
                                     where query.Contains(p)
                                     select p).ToList();
                    checkQueryTime.Stop();
                    var checkSet = new HashSet<Point>(checkList);
                    Assert.AreEqual(result.Count, checkList.Count);
                    foreach (var r in result) {
                        Assert.IsTrue(query.Contains(r));
                        Assert.IsTrue(checkSet.Contains(r));
                    }
                }
                Assert.IsTrue(bspQueryTime.ElapsedMilliseconds < checkQueryTime.ElapsedMilliseconds);
            }
        }

        [TestMethod]
        public void RTreeQuery_PointIntersectionTest() {
            for (int seed = 0; seed < 1; ++seed) {
                int n = 100000;
                var points = new Point[n];
                var rand = new Random(seed);
                double scale = 1000;
                for (int i = 0; i < n; ++i) {
                    points[i] = new Point(rand.NextDouble() * scale, rand.NextDouble() * scale);
                }
                var bsptree = new RTree<Point, Point>(
                    from p in points
                    select new KeyValuePair<IRectangle<Point>, Point>(new Rectangle(p), p));
                Assert.AreEqual(bsptree.GetAllLeaves().Count(), n);
                Assert.AreEqual(bsptree.GetAllIntersecting(new Rectangle(-2, -2, -1, -1)).Count(), 0);
                Assert.AreEqual(bsptree.GetAllIntersecting(new Rectangle(0, 0, scale, scale)).Count(), n);
                int intersecting = 0;
                for (int i = 0; i < 10000; ++i) {
                    double s = scale / 100;
                    var query = new Rectangle(rand.NextDouble() * s, rand.NextDouble() * s, rand.NextDouble() * s, rand.NextDouble() * s);
                    if (bsptree.IsIntersecting(query)) {
                        ++intersecting;
                    }
                }
                System.Diagnostics.Debug.WriteLine(intersecting);
            }
        }

        [TestMethod]
        public void RTreeRemove() {
            for (int size = 2; size < 20; size++) {
                var rects = new List<Tuple<Rectangle, char>>(); ;
                var rand = new Random(0);
                for (int i = 0; i < size; ++i) {
                    int RegionSize = 10;
                    rects.Add(new Tuple<Rectangle,char>(new Rectangle(rand.Next(RegionSize), rand.Next(RegionSize), new Point(2, 2)), (char)('a'+i)));
                }

                var tree = new RTree<char, Point>(Enumerable.Range(0, size).Select(i => new KeyValuePair<IRectangle<Point>, char>(rects[i].Item1, rects[i].Item2)));

                while (rects.Count > 0) {
                    var i = rand.Next(rects.Count);
                    tree.Remove(rects[i].Item1, rects[i].Item2);
                    rects.RemoveAt(i);
                }
                
            }

        }

        private RTree<double, Point> CreateOnRecs(List<Rectangle> rects) {
            var r = new RTree<double, Point>();
            foreach (var p in rects) {
                r.Add(p, p.Area);
            }
            return r;
        }

        [TestMethod]
        public void RTreeQuery_Rectangles() {
            const int Seeds = 100;
            const int RectCount = 100;
            const int RegionSize = 1000;
            const int RectSize = 10;
            for (int seed = 0; seed < Seeds; ++seed) {
                var rects = new Rectangle[RectCount];
                var rand = new Random(seed);
                for (int i = 0; i < RectCount; ++i) {
                    rects[i] = new Rectangle(rand.Next(RegionSize), rand.Next(RegionSize), new Point(RectSize, RectSize));
                }
                var bsptree = new RTree<Rectangle, Point>(
                    from r in rects
                    select new KeyValuePair<IRectangle<Point>, Rectangle>(r, r));
                Assert.AreEqual(bsptree.GetAllLeaves().Count(), RectCount);
                Assert.AreEqual(bsptree.GetAllIntersecting(new Rectangle(0, 0, RegionSize + RectSize, RegionSize + RectSize)).Count(), RectCount);
                Assert.AreEqual(bsptree.GetAllIntersecting(new Rectangle(-2, -2, -1, -1)).Count(), 0);
                var rect = GetRandRect(RegionSize, rand);
                var checkList = (from r in rects
                                 where rect.Intersects(r)
                                 select r).ToList();
                var checkSet = new HashSet<string>(checkList.Select(r => r.ToString()));
                var result = bsptree.GetAllIntersecting(rect).ToList();
                Assert.AreEqual(result.Count, checkList.Count, "result and check are different sizes: seed={0}", seed);
                foreach (var r in result) {
                    Assert.IsTrue(rect.Intersects(r), "rect doesn't intersect query: seed={0}, rect={1}, query={2}", seed, r, rect);

                    Assert.IsTrue(checkSet.Contains(r.ToString()), "check set does not contain rect: seed={0}", seed);
                }
            }
        }

        private static Rectangle GetRandRect(int RegionSize, Random rand) {
            return new Rectangle(rand.Next(RegionSize), rand.Next(RegionSize), rand.Next(RegionSize), rand.Next(RegionSize));
        }

        [TestMethod]
        public void RTreeQuery_IncrementalRectangles() {
            const int RectsCount = 1000;
            const int RegionSize = 1000;
            const int RectSize = 10;
            var rects = new Rectangle[RectsCount];
            var rand = new Random(0);
            for (int i = 0; i < RectsCount; ++i) {
                rects[i] = new Rectangle(new Point(rand.Next(RegionSize), rand.Next(RegionSize)));
            }

            // create rTree with just the first rectangle
            var rtree = new RTree<Rectangle, Point>();
            rtree.Add(rects[0], rects[0]);

            // add remaining rectangles 10 at a time
            for (int a = 1, b = 10; b < RectsCount; a = b, b += 10) {
                for (int i = a; i < b; ++i) {
                    rtree.Add(rects[i], rects[i]);
                }
                var allLeaves = rtree.GetAllLeaves().ToArray();
                Assert.AreEqual(allLeaves.Length, b, "did we lose leaves?");
                Assert.AreEqual(rtree.GetAllIntersecting(
                    new Rectangle(0, 0, RegionSize + RectSize, RegionSize + RectSize)).Count(), b,
                    "are all leaves inside the max range?");
                Assert.AreEqual(rtree.GetAllIntersecting(new Rectangle(-2, -2, -1, -1)).Count(), 0,
                    "should be no leaves inside this rectangle!");
                var rect = new Rectangle(rand.Next(RegionSize), rand.Next(RegionSize), rand.Next(RegionSize), rand.Next(RegionSize));
                var checkList = (from r in rects.Take(b)
                                 where rect.Intersects(r)
                                 select r).ToList();
                var checkSet = new HashSet<string>(checkList.Select(r => r.ToString()));
                var result = rtree.GetAllIntersecting(rect).ToList();
                Assert.AreEqual(result.Count, checkList.Count, "result and check are different sizes");
                foreach (var r in result) {
                    Assert.IsTrue(rect.Intersects(r), "rect doesn't intersect query: rect={1}, query={2}", r, rect);
                    Assert.IsTrue(checkSet.Contains(r.ToString()), "check set does not contain rect");
                }
            }
        }
    }
}
