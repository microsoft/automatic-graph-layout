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
using System;
using System.Collections.Generic;

namespace Microsoft.Msagl.Core.Geometry {
    internal class HullPointComparer:IComparer<HullPoint> {
        Point pivot;
      
        /// <summary>
        /// note that this function can change "deleted" member for collinear points
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=203
        //SharpKit/Colin - https://code.google.com/p/sharpkit/issues/detail?id=332
        public int Compare(HullPoint i, HullPoint j) {
#else
        int IComparer<HullPoint>.Compare(HullPoint i, HullPoint j) {
#endif
            if (i == j)
                return 0;
            if (i == null)
                return -1;
            if (j == null)
                return 1;

            switch (Point.GetTriangleOrientationWithIntersectionEpsilon(pivot, i.Point, j.Point)) {
                case TriangleOrientation.Counterclockwise:
                    return -1;
                case TriangleOrientation.Clockwise:
                    return 1;
                case TriangleOrientation.Collinear: {
                    //because of the double point error pi and pj can be on different sizes of the pivot on the horizontal line passing through the pivot, or rather just above it
                    var piDelX = i.Point.X - pivot.X;
                    var pjDelX = j.Point.X - pivot.X;
                    if (piDelX > ApproximateComparer.DistanceEpsilon && pjDelX < -ApproximateComparer.DistanceEpsilon) {
                        return -1;
                    }
                    if (piDelX < -ApproximateComparer.DistanceEpsilon && pjDelX > ApproximateComparer.DistanceEpsilon) {
                        return 1;
                    }

                    //here i and j cannot be on the different sides of the pivot because of the choice of the pivot
                    //delete the one that is closer to the pivot.
                    var pi = i.Point - pivot;
                    var pj = j.Point - pivot;
                    var iMinJ = pi.L1 - pj.L1;
                    if (iMinJ < 0) {
                        i.Deleted = true;
                        return -1;
                    }
                    if (iMinJ > 0) {
                        j.Deleted = true;
                        return 1;
                    }

                    //points are the same, leave the one with the greatest hash code
                    if (i.GetHashCode() < j.GetHashCode())
                        i.Deleted = true;
                    else
                        j.Deleted = true;

                    return 0;
                }

            }
            throw new InvalidOperationException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal HullPointComparer(Point pivotPoint) {
            this.pivot = pivotPoint;
        }
        
    }
}
