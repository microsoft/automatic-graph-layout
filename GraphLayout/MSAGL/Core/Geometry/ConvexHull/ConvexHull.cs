using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing;

namespace Microsoft.Msagl.Core.Geometry {
    /// <summary>
    /// Creates the convex hull of a set of points following "Computational Geometry, second edition" of O'Rourke
    /// </summary>
    public sealed class ConvexHull {
        HullPoint[] hullPoints;
        Point pivot;
        HullStack stack;
        HullPointComparer comparer;

        ConvexHull(IEnumerable<Point> bodyPoints) {
            SetPivotAndAllocateHullPointsArray(bodyPoints);
        }

        void SetPivotAndAllocateHullPointsArray(IEnumerable<Point> bodyPoints) {
            pivot = new Point(0, Double.MaxValue); //set Y to a very big value
            int pivotIndex = -1;
            int n = 0;
            foreach (Point point in bodyPoints) {
                if (point.Y < pivot.Y) {
                    pivot = point;
                    pivotIndex = n;
                }
                else if (point.Y == pivot.Y)
                    if (point.X > pivot.X) {
                        pivot = point;
                        pivotIndex = n;
                    }
                n++;
            }
            if (n >= 1) {
                hullPoints = new HullPoint[n - 1]; //we will not copy the pivot into the hull points
                n = 0;
                foreach (Point point in bodyPoints)
                    if (n != pivotIndex)
                        hullPoints[n++] = new HullPoint(point);
                    else
                        pivotIndex = -1; //forget where the pivot was
            }
        }

        Point StackTopPoint {
            get { return stack.Point; }
        }

        Point StackSecondPoint {
            get { return stack.Next.Point; }
        }

        /// <summary>
        /// calculates the convex hull of the given set of points
        /// </summary>
        /// <param name="pointsOfTheBody">Point of the convex hull.</param>
        /// <returns>The list of extreme points of the hull boundaries in the clockwise order</returns>
        public static IEnumerable<Point> CalculateConvexHull(IEnumerable<Point> pointsOfTheBody) {
            var convexHull = new ConvexHull(pointsOfTheBody);
            return convexHull.Calculate();
        }

        IEnumerable<Point> Calculate() {
            if (pivot.Y == Double.MaxValue)
                return new Point[0];
            if (hullPoints.Length == 0)
                return new[] {pivot};
            SortAllPointsWithoutPivot();
            Scan();
            return EnumerateStack();
        }

        IEnumerable<Point> EnumerateStack() {
            HullStack stackCell = stack;
            while (stackCell != null) {
                yield return stackCell.Point;
                stackCell = stackCell.Next;
            }
        }

        void Scan() {
            int i = 0;
            while (hullPoints[i].Deleted)
                i++;
           
            stack = new HullStack(pivot);
            Push(i++);
            if (i < hullPoints.Length) {
                if (!hullPoints[i].Deleted)
                    Push(i++);
                else
                    i++;
            }

            while (i < hullPoints.Length) {
                if (!hullPoints[i].Deleted) {
                    if (LeftTurn(i))
                        Push(i++);
                    else
                        Pop();
                }
                else
                    i++;
            }

            //cleanup the end
            while (StackHasMoreThanTwoPoints() && !LeftTurnToPivot())
                Pop();
        }

        bool LeftTurnToPivot() {
            return Point.GetTriangleOrientation(StackSecondPoint, StackTopPoint, pivot) ==
                   TriangleOrientation.Counterclockwise;
        }

        bool StackHasMoreThanTwoPoints() {
            return stack.Next != null && stack.Next.Next != null;
        }

        void Pop() {
            stack = stack.Next;
        }

       
        bool LeftTurn(int i) {
            if (stack.Next == null)
                return true; //there is only one point in the stack
            var orientation = Point.GetTriangleOrientationWithIntersectionEpsilon(StackSecondPoint, StackTopPoint, hullPoints[i].Point);
            if (orientation == TriangleOrientation.Counterclockwise)
                return true;
            if (orientation == TriangleOrientation.Clockwise)
                return false;
            return BackSwitchOverPivot(hullPoints[i].Point);
        }
        bool BackSwitchOverPivot(Point point) {
            //we know here that there at least two points in the stack but it has to be exaclty two 
            if (stack.Next.Next != null)
                return false;
            Debug.Assert(StackSecondPoint == pivot);
            return StackTopPoint.X > pivot.X + ApproximateComparer.DistanceEpsilon &&
                   point.X < pivot.X - ApproximateComparer.DistanceEpsilon;
        }

        void Push(int p) {
            var t = new HullStack(hullPoints[p].Point) {Next = stack};
            stack = t;
        }

        void SortAllPointsWithoutPivot() {
            comparer = new HullPointComparer(pivot);
            Array.Sort(hullPoints, comparer);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        internal static Polyline CreateConvexHullAsClosedPolyline(IEnumerable<Point> points) {
            var convexHull = new Polyline(CalculateConvexHull(points)) { Closed = true };
#if TEST_MSAGL
            foreach (var point in points) {
                if (Curve.PointRelativeToCurveLocation(point, convexHull) == PointLocation.Outside) {
                    var hullPoint = convexHull[convexHull.ClosestParameter(point)];

                    // This can be too restrictive if very close points are put into the hull.  It is probably 
                    // better to clean up in the caller before doing this, but this assert can also be relaxed.
                  Debug.Assert(ApproximateComparer.Close(point, hullPoint, ApproximateComparer.IntersectionEpsilon * 20), String.Format("not CloseIntersections: initial point {0}, hull point {1}", point, hullPoint));
                    
                }
            }
#endif // TEST_MSAGL
            return convexHull;
        }
    }
}
