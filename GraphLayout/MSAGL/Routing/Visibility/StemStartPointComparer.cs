using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core;

namespace Microsoft.Msagl.Routing.Visibility {
    /// <summary>
    /// compares couples only by looking at the couple first point
    /// we need the couple to hold the stem
    /// </summary>
    internal class StemStartPointComparer : IComparer<Stem> {
        Point pivot;

        int IComparer<Stem>.Compare(Stem i, Stem j) {
            if (i == j)
                return 0;
            if (i == null)
                return -1;
            if (j == null)
                return 1;

            Point a = i.Start.Point - pivot;
            Point b = j.Start.Point - pivot;

            return CompareVectorsByAngleToXAxis(a, b);

        }

        internal static int CompareVectorsByAngleToXAxis(Point a, Point b) {
            if (a.Y >= 0) {
                if (b.Y < 0)
                    return -1;
                return CompareVectorsPointingToTheSameYHalfPlane(ref a, ref b);

            } else {
                //a.Y <0
                if (b.Y >= 0)
                    return 1;
                return CompareVectorsPointingToTheSameYHalfPlane(ref a, ref b);
            }

        }

        private static int CompareVectorsPointingToTheSameYHalfPlane(ref Point a, ref Point b) {
            //now we know that a and b do not point to different Y half planes
            double sign = a.X * b.Y - a.Y * b.X;
            if (sign > ApproximateComparer.Tolerance)
                return -1;
            if (sign < -ApproximateComparer.Tolerance)
                return 1;
            //are they on the opposite sides of the pivot by X?
            if (a.X >= 0) {
                if (b.X < 0)//yes
                    return -1;
            } else
                if (b.X >= 0)
                    return 1;

            double del = Math.Abs(a.X) - Math.Abs(b.X);
            if (del < 0)
                return -1;
            if (del > 0)
                return 1;


            del = Math.Abs(a.Y) - Math.Abs(b.Y);
            if (del < 0)
                return -1;
            if (del > 0)
                return 1;

            return 0; //points are equal
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal StemStartPointComparer(Point pivotPoint) {
            this.pivot = pivotPoint;
        }

    }
}
