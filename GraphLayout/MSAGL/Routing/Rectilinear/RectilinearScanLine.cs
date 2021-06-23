//
// RectilinearScanLine.cs
// MSAGL ScanLine class for Rectilinear Edge Routing line generation.
//
// Copyright Microsoft Corporation.
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Core;

namespace Microsoft.Msagl.Routing.Rectilinear {
    internal class RectilinearScanLine : IComparer<BasicObstacleSide> {
        readonly ScanDirection scanDirection;

        // This is the data structure that allows fast insert/remove of obstacle edges as well as
        // scanning for next/prev edges along the direction of the scan line.
        RbTree<BasicObstacleSide> SideTree { get; set; }

        // Because sides may overlap and thus their relative positions change, retain the current
        // position, which is set on insertions by parameter, and by Overlap events via SetLinePosition.
        private Point linePositionAtLastInsertOrRemove;

        internal RectilinearScanLine(ScanDirection scanDir, Point start) {
            scanDirection = scanDir;
            SideTree = new RbTree<BasicObstacleSide>(this);
            this.linePositionAtLastInsertOrRemove = start;
        }

        internal RBNode<BasicObstacleSide> Insert(BasicObstacleSide side, Point scanPos) {
            DevTraceInfo(1, "prev LinePos = {0}, new LinePos = {1}, inserting side = {2}", this.linePositionAtLastInsertOrRemove, scanPos, side.ToString());
            Assert(!scanDirection.IsFlat(side), "Flat sides are not allowed in the scanline");
            Assert(null == Find(side), "side already exists in the ScanLine");
            this.linePositionAtLastInsertOrRemove = scanPos;

            // RBTree's internal operations on insert/remove etc. mean the node can't cache the
            // RBNode returned by insert(); instead we must do find() on each call.  But we can
            // use the returned node to get predecessor/successor.
            var node = SideTree.Insert(side);
            DevTraceDump(2);
            return node;
        }

        internal int Count { get { return SideTree.Count; } }

        internal void Remove(BasicObstacleSide side, Point scanPos) {
            DevTraceInfo(1, "current linePos = {0}, removing side = {1}", this.linePositionAtLastInsertOrRemove, side.ToString());
            Assert(null != Find(side), "side does not exist in the ScanLine");
            this.linePositionAtLastInsertOrRemove = scanPos;
            SideTree.Remove(side);
            DevTraceDump(2);
        }

        internal RBNode<BasicObstacleSide> Find(BasicObstacleSide side) {
            // Sides that start after the current position cannot be in the scanline.
            if (-1 == scanDirection.ComparePerpCoord(this.linePositionAtLastInsertOrRemove, side.Start)) {
                return null;
            }
            return SideTree.Find(side);
        }

        internal RBNode<BasicObstacleSide> NextLow(BasicObstacleSide side) {
            return NextLow(Find(side));
        }

        internal RBNode<BasicObstacleSide> NextLow(RBNode<BasicObstacleSide> sideNode) {
            var pred = SideTree.Previous(sideNode);
            return pred;
        }

        internal RBNode<BasicObstacleSide> NextHigh(BasicObstacleSide side) {
            return NextHigh(Find(side));
        }

        internal RBNode<BasicObstacleSide> NextHigh(RBNode<BasicObstacleSide> sideNode) {
            var succ = SideTree.Next(sideNode);
            return succ;
        }

        internal RBNode<BasicObstacleSide> Next(Direction dir, RBNode<BasicObstacleSide> sideNode) {
            var succ = (StaticGraphUtility.IsAscending(dir)) ? SideTree.Next(sideNode) : SideTree.Previous(sideNode);
            return succ;
        }

        internal RBNode<BasicObstacleSide> Lowest()
        {
            return SideTree.TreeMinimum();
        }

#if DEVTRACE
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
// ReSharper disable InconsistentNaming
        internal void DevTrace_VerifyConsistency(string descFormat, params object[] descArgs) {
// ReSharper restore InconsistentNaming
            bool retval = true;
            if (scanLineVerify.IsLevel(1)) {
                DevTraceInfo(2, "ScanLineConsistencyCheck LinePos = {0}", this.linePositionAtLastInsertOrRemove);
                BasicObstacleSide prevSide = null;
                DevTraceInfo(3, "ScanLine dump {0}, count = {1}:", string.Format(descFormat, descArgs), SideTree.Count);
                foreach (var currentSide in SideTree) {
                    DevTraceInfo(3, currentSide.ToString());
                    if ((null != prevSide) && (-1 != Compare(prevSide, currentSide))) {
                        scanLineTrace.WriteError(0, "Sides are not strictly increasing:");
                        scanLineTrace.WriteFollowup(0, prevSide.ToString());
                        scanLineTrace.WriteFollowup(0, currentSide.ToString());
                        retval = false;
                    }
                    prevSide = currentSide;
                }
            }
            Assert(retval, "Sides are not strictly increasing");
        }
#endif // DEVTRACE
        #region IComparer<BasicObstacleSide>
        /// <summary>
        /// For ordering lines along the scanline at segment starts/ends.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        public int Compare(BasicObstacleSide first, BasicObstacleSide second) {
            ValidateArg.IsNotNull(first, "first");
            ValidateArg.IsNotNull(second, "second");
            
            // If these are two sides of the same obstacle then the ordering is obvious.
            if (first.Obstacle == second.Obstacle) {
                if (first == second) {
                    return 0;
                }
                return (first is LowObstacleSide) ? -1 : 1;
            }

            Debug_VerifySidesDoNotIntersect(first, second);

            // Other than intersecting sides at vertices of the same obstacle, there should be no interior intersections...
            Point firstIntersect = VisibilityGraphGenerator.ScanLineIntersectSide(this.linePositionAtLastInsertOrRemove, first, scanDirection);
            Point secondIntersect = VisibilityGraphGenerator.ScanLineIntersectSide(this.linePositionAtLastInsertOrRemove, second, scanDirection);
            var cmp = firstIntersect.CompareTo(secondIntersect);

            // ... but we may still have rectangular sides that coincide, or angled sides that are close enough here but
            // are not detected by the convex-hull overlap calculations.  In those cases, we refine the comparison by side
            // type, with High coming before Low, and then by obstacle ordinal if needed. Because there are no interior
            // intersections, this ordering will remain valid as long as the side(s) are in the scanline.
            if (0 == cmp) {
                bool firstIsLow = first is LowObstacleSide;
                bool secondIsLow = second is LowObstacleSide;
                cmp = firstIsLow.CompareTo(secondIsLow);
                if (0 == cmp) {
                    cmp = first.Obstacle.Ordinal.CompareTo(second.Obstacle.Ordinal);
                }
            }

            DevTraceInfo(4, "Compare {0} @ {1:F5} {2:F5} and {3:F5} {4:F5}: {5} {6}",
                            cmp, firstIntersect.X, firstIntersect.Y, secondIntersect.X, secondIntersect.Y, first, second);
            return cmp;
        }

        [Conditional("TEST_MSAGL")]
        internal static void Debug_VerifySidesDoNotIntersect(BasicObstacleSide side1, BasicObstacleSide side2) {
            Point intersect;
            if (!Point.LineLineIntersection(side1.Start, side1.End, side2.Start, side2.End, out intersect)) {
                return;
            }

            // The test for being within the interval is just multiplying to ensure that both subtractions 
            // return same-signed results (including endpoints).
            var isInterior = ((side1.Start - intersect) * (intersect - side1.End) >= -ApproximateComparer.DistanceEpsilon)
                        && ((side2.Start - intersect) * (intersect - side2.End) >= -ApproximateComparer.DistanceEpsilon);
            Debug.Assert(!isInterior, "Shouldn't have interior intersections except sides of the same obstacle");
        }

        #endregion // IComparer<BasicObstacleSide>

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return this.linePositionAtLastInsertOrRemove + " " + scanDirection;
        }

        [Conditional("TEST_MSAGL")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        void Assert(bool condition, string message) {
#if TEST_MSAGL
            if (!condition) {
                Test_DumpScanLine();
            }
#endif // TEST
            Debug.Assert(condition, message);
        }

        #region DevTrace
#if DEVTRACE
        readonly DevTrace scanLineTrace = new DevTrace("Rectilinear_ScanLineTrace", "RectScanLine");
        readonly DevTrace scanLineDump = new DevTrace("Rectilinear_ScanLineDump");
        readonly DevTrace scanLineVerify = new DevTrace("Rectilinear_ScanLineVerify");
#endif // DEVTRACE

        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        void DevTraceInfo(int verboseLevel, string format, params object[] args) {
#if DEVTRACE
            scanLineTrace.WriteLineIf(DevTrace.Level.Info, verboseLevel, format, args);
#endif // DEVTRACE
        }

        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        void DevTraceDump(int verboseLevel) {
#if DEVTRACE
            if (scanLineDump.IsLevel(verboseLevel)) {
                Test_DumpScanLine();
            }
#endif // DEVTRACE
        }
        #endregion // DevTrace

        #region DebugCurves

#if TEST_MSAGL
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        internal void Test_DumpScanLine() {
            DebugCurveCollection.WriteToFile(Test_GetScanLineDebugCurves(), StaticGraphUtility.GetDumpFileName("ScanLine"));
        }
#endif // TEST

#if TEST_MSAGL
        internal List<DebugCurve> Test_GetScanLineDebugCurves() {
// ReSharper restore InconsistentNaming
            var debugCurves = new List<DebugCurve>();

            // Alternate the colors between green and blue, so that any inconsistency will stand out.
            // Use red to highlight that.
            string[] colors = { "green", "blue" };
            int index = 0;
            var bbox = new Rectangle();
            BasicObstacleSide prevSide = null;
            foreach (var currentSide in SideTree) {
                string color = colors[index];
                index ^= 1;
                if (null == prevSide) {
                    // Create this the first time through; adding to an empty rectangle leaves 0,0.
                    bbox = new Rectangle(currentSide.Start, currentSide.End);
                }
                else {
                    if (-1 != Compare(prevSide, currentSide)) {
                        // Note: we toggled the index, so the red replaces the colour whose turn it is now
                        // and will leave the red line bracketed by two sides of the same colour.
                        color = "red";
                    }
                    bbox.Add(currentSide.Start);
                    bbox.Add(currentSide.End);
                }
                debugCurves.Add(new DebugCurve(0.1, color, new LineSegment(currentSide.Start, currentSide.End)));
                prevSide = currentSide;
            }

            // Add the sweep line.
            Point start = StaticGraphUtility.RectangleBorderIntersect(bbox, this.linePositionAtLastInsertOrRemove, scanDirection.OppositeDirection);
            Point end = StaticGraphUtility.RectangleBorderIntersect(bbox, this.linePositionAtLastInsertOrRemove, scanDirection.Direction);
            debugCurves.Add(new DebugCurve(0.025, "black", new LineSegment(start, end)));
            return debugCurves;
        }
#endif // TEST
        #endregion // DebugCurves
    }
}
