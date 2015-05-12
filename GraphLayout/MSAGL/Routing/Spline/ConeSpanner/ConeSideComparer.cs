using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner {
    internal class ConeSideComparer : IComparer<ConeSide> {
        Point x;
        internal void SetOperand(ConeSide activeElement) {
            x = IntersectionOfSegmentAndSweepLine((activeElement));
        }
        readonly IConeSweeper coneSweeper;

        internal ConeSideComparer(IConeSweeper coneSweeper) {
            this.coneSweeper = coneSweeper;
        }

        public int Compare(ConeSide a, ConeSide b) {
            var aObst = a as BrokenConeSide;
            var bObst = b as BrokenConeSide;
            if (aObst != null) {
                return bObst != null ? CompareBrokenSides(aObst, bObst) : CompareObstacleSideAndConeSide(b);
            } else {
                //a is ConeSide
                return bObst != null ? CompareConeSideAndObstacleSide(a, bObst) : CompareNotIntersectingSegs(a, b);
            }
        }

        static int CompareNotIntersectingSegs(ConeSide a, ConeSide b) {
            var signedArea = Point.GetTriangleOrientation(a.Start, b.Start, b.Start + b.Direction);

            switch (signedArea) {
                case TriangleOrientation.Counterclockwise:
                    return -1;
                case TriangleOrientation.Clockwise:
                    return 1;
                default:
                    return 0;
            }
        }

        int CompareObstacleSideAndConeSide(ConeSide coneSide) {
            var orientation = Point.GetTriangleOrientation(x, coneSide.Start,
                                                                           coneSide.Start + coneSide.Direction);
            if (orientation == TriangleOrientation.Counterclockwise)
                return -1;
            if (orientation == TriangleOrientation.Clockwise)
                return 1;

            //we have the case where x belongs to the cone side

            return coneSide is ConeLeftSide ? -1 : 1;
        }

        int CompareConeSideAndObstacleSide(ConeSide coneSide, BrokenConeSide brokenConeSide) {
            var orientation = Point.GetTriangleOrientation(x, brokenConeSide.Start, brokenConeSide.End);
            if (orientation == TriangleOrientation.Counterclockwise)
                return -1;
            if (orientation == TriangleOrientation.Clockwise)
                return 1;

            //we have the case where x belongs to the cone side

      //      lineSweeper.Show(CurveFactory.CreateDiamond(5,5, brokenConeSide.EndVertex.Point));

            return coneSide is ConeLeftSide ? 1 : -1;
        }

        internal Point IntersectionOfSegmentAndSweepLine(ConeSide obstacleSide) {
            var den = obstacleSide.Direction*coneSweeper.SweepDirection;
            Debug.Assert(Math.Abs(den) > 0);
            var t = (coneSweeper.Z - obstacleSide.Start*coneSweeper.SweepDirection)/den;
            return obstacleSide.Start + t*obstacleSide.Direction;
        }


        int CompareBrokenSides(BrokenConeSide aObst, BrokenConeSide bObst) {
            if (aObst.EndVertex == bObst.EndVertex)
                return CompareNotIntersectingSegs(aObst.ConeSide, bObst.ConeSide);

            if (Point.GetTriangleOrientation(x, bObst.Start, bObst.EndVertex.Point) ==
                TriangleOrientation.Counterclockwise)
                return -1;
            return 1;
        }
    }
}