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
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core;

namespace Microsoft.Msagl.Routing.Visibility {
    internal class ActiveEdgeComparerWithRay : IComparer<PolylinePoint> {
        Point pivot;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Point Pivot {
            get { return pivot; }
            set { pivot = value; }
        }
        Point pointOnTheRay;

        internal Point IntersectionOfTheRayAndInsertedEdge {
            get { return pointOnTheRay; }
            set { pointOnTheRay = value; }
        }

        int IComparer<PolylinePoint>.Compare(PolylinePoint x, PolylinePoint y) {
            ValidateArg.IsNotNull(x, "x");
            ValidateArg.IsNotNull(y, "y");
            System.Diagnostics.Debug.Assert(IntersectionPointBelongsToTheInsertedEdge(x));
           
            switch (Point.GetTriangleOrientation( IntersectionOfTheRayAndInsertedEdge, y.Point, y.NextOnPolyline.Point)) {
                case TriangleOrientation.Counterclockwise:
                    return -1;
                default:
                    return 1;
            }
        }



        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private bool IntersectionPointBelongsToTheInsertedEdge(PolylinePoint x) {
            Point a = x.Point - IntersectionOfTheRayAndInsertedEdge;
            Point b = x.NextOnPolyline.Point - IntersectionOfTheRayAndInsertedEdge;
            return Math.Abs(a.X * b.Y - b.X * a.Y) < ApproximateComparer.DistanceEpsilon;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        Point IntersectEdgeWithRay(Point source, Point target, Point ray) {
            //let x(t-s)+s is on the ray, then for some y we x(t-s)+s=y*ray+pivot, or x(t-s)-y*ray=pivot-s
            double x, y;
            bool result = LinearSystem2.Solve(target.X-source.X, -ray.X, Pivot.X-source.X, target.Y-source.Y, -ray.Y, Pivot.Y-source.Y, out x, out y);
            if (!(-ApproximateComparer.Tolerance <= x && x <= 1 + ApproximateComparer.Tolerance))
                throw new Exception();
            if (!result)
                throw new InvalidOperationException();

            return Pivot + y * ray;
        }

        internal Point IntersectEdgeWithRay(PolylinePoint side, Point ray){
            return IntersectEdgeWithRay(side.Point, side.NextOnPolyline.Point, ray);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ActiveEdgeComparerWithRay(Point pivot, Point pointOnTheRay) {
            this.pivot = pivot;
            this.pointOnTheRay = pointOnTheRay;
        }

        internal ActiveEdgeComparerWithRay() {}
    }
}
