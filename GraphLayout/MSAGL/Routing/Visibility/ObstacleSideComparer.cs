using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core;

namespace Microsoft.Msagl.Routing.Visibility {
    internal class ObstacleSideComparer : IComparer<SegmentBase> {

        readonly LineSweeperBase lineSweeper;


        internal ObstacleSideComparer(LineSweeperBase lineSweeper) {
            this.lineSweeper = lineSweeper;
        }

        /// <summary>
        /// the intersection of the sweepline and the active segment
        /// </summary>
        Point x;


        public int Compare(SegmentBase a, SegmentBase b) {
            ValidateArg.IsNotNull(b, "b");
            var orient = Point.GetTriangleOrientation(b.Start, b.End, x);
            switch (orient) {
                case TriangleOrientation.Collinear:
                    return 0;
                case TriangleOrientation.Clockwise:
                    return 1;
                default:
                    return -1;
            }
        }


        internal void SetOperand(SegmentBase side) {
            x = IntersectionOfSideAndSweepLine(side);
        }

        internal Point IntersectionOfSideAndSweepLine(SegmentBase obstacleSide) {
            var den = obstacleSide.Direction * lineSweeper.SweepDirection;
            Debug.Assert(Math.Abs(den) > ApproximateComparer.DistanceEpsilon);
            var t = (lineSweeper.Z - obstacleSide.Start * lineSweeper.SweepDirection) / den;
            return obstacleSide.Start + t * obstacleSide.Direction;
        }

    }
}