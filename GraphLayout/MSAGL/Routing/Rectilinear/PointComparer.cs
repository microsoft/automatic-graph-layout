using System;
using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
    internal class PointComparer {
        // Due to the vagaries of rounding, we may encounter a result that is not quite 0
        // when subtracting two numbers that are close.
// ReSharper disable InconsistentNaming
        private static readonly double differenceEpsilon = ApproximateComparer.DistanceEpsilon / 2;
// ReSharper restore InconsistentNaming

        static internal double DifferenceEpsilon {
            get { return differenceEpsilon; }
        }

        /// <summary>
        /// Determines whether the specified Points, which are assumed to have been Round()ed,
        /// are close enough to be considered equal.
        /// </summary>
        /// <param name="a">The first object of type Point to compare.</param>
        /// <param name="b">The second object of type Point to compare.</param>
        /// <returns>True if the inputs are close enough to be considered equal, else false</returns>
        public static bool Equal(Point a, Point b) {
            return Equal(a.X, b.X) && Equal(a.Y, b.Y);
        }

        /// <summary>
        /// Determines whether the specified double values, which are assumed to have been Round()ed,
        /// are close enough to be considered equal.
        /// </summary>
        /// <param name="x">The first double value to compare.</param>
        /// <param name="y">The second double value to compare.</param>
        /// <returns>True if the inputs are close enough to be considered equal, else false</returns>
        public static bool Equal(double x, double y) {
            return (0 == Compare(x, y));
        }

        /// <summary>
        /// The usual Compare operation, with inputs that are assumed to have been Round()ed.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns>0 if the inputs are close enough to be considered equal, else -1 if lhs is 
        /// less than rhs, else 1.</returns>
        public static int Compare(double lhs, double rhs) {
            // If the inputs are not rounded, then two numbers that are close together at the 
            // middle of the rounding range may Compare as 0 but Round to different values
            // (e.g., with rounding to 6 digits, xxx.yyyyyy49 and xxx.yyyyyy51 will exhibit this).
            Assert_Rounded(lhs);
            Assert_Rounded(rhs);

            int cmp = 0;
            if (lhs + DifferenceEpsilon < rhs) {
                cmp = -1;
            } else if (rhs + DifferenceEpsilon < lhs) {
                cmp = 1;
            }

            // Just to be sure we're in sync with CompassVector
            Debug.Assert((cmp < 0) == (Direction.East == CompassVector.VectorDirection(new Point(lhs, 0), new Point(rhs, 0))));
            Debug.Assert((0 == cmp) == (Direction. None == CompassVector.VectorDirection(new Point(lhs, 0), new Point(rhs, 0))));
            return cmp;
        }

        /// <summary>
        /// The usual Compare operation, with inputs that are assumed to have been Round()ed.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns>0 if the inputs are close enough to be considered equal, else -1 if lhs is 
        /// less than rhs, else 1.</returns>
        public static int Compare(Point lhs, Point rhs) {
            int cmp = Compare(lhs.X, rhs.X);
            if (0 == cmp) {
                cmp = Compare(lhs.Y, rhs.Y);
            }
            return cmp;
        }

        /// <summary>
        /// return true if less or equal holds for two values that are assumed to have been Round()ed
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool LessOrEqual(double a, double b){
            int comp = Compare(a, b);
            return comp < 0 || comp == 0;
            
        }

        /// <summary>
        /// return true if less holds for two values that are assumed to have been Round()ed
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Less(double a, double b) {
            return Compare(a, b) < 0;
        }

        [Conditional("TEST_MSAGL")]
// ReSharper disable InconsistentNaming
        static void Assert_Rounded(double d) {
            // Be sure there is enough precision to round that far; anything larger than this is
            // unlikely to be a graph coordinate (it's probably a line intersection way out of range).
            if (Math.Log10(Math.Abs(d)) < (14 - ApproximateComparer.DistanceEpsilonPrecision)) {
                Debug.Assert(Math.Abs(ApproximateComparer.Round(d) - d) < DifferenceEpsilon, "unRounded value passed");
            }
        }

        [Conditional("TEST_MSAGL")]
        static void Assert_Rounded(Point p) {
            Assert_Rounded(p.X);
            Assert_Rounded(p.Y);
        }
// ReSharper restore InconsistentNaming

        #region Direction_Utilities

        // These call through to CompassVector methods, assuming operands that use PointComparer rounding.
        internal static Direction GetDirections(Point a, Point b) {
            Assert_Rounded(a);
            Assert_Rounded(b);
            return CompassVector.DirectionsFromPointToPoint(a, b);
        }

        internal static Direction GetPureDirection(Point a, Point b) {
            Assert_Rounded(a);
            Assert_Rounded(b);
            Direction dir = GetDirections(a, b);
            Debug.Assert(CompassVector.IsPureDirection(dir), "Impure direction found");
            return dir;
        }

        internal static bool IsPureDirection(Point a, Point b) {
            Assert_Rounded(a);
            Assert_Rounded(b);
            return CompassVector.IsPureDirection(GetDirections(a, b));
        }
        internal static bool IsPureDirection(Direction dir) {
            return CompassVector.IsPureDirection(dir);
        }

        internal static bool IsPureLower(Point a, Point b) {
            Assert_Rounded(a);
            Assert_Rounded(b);

            // Is a lower than b along the orthogonal line segment?  That means moving
            // from a to b is in the increasing direction.
            Direction dir = GetDirections(a, b);
            return (Direction.East == dir) || (Direction.North == dir);
        }

        static internal Direction GetPureDirection(VisibilityVertex first, VisibilityVertex second) {
            Assert_Rounded(first.Point);
            Assert_Rounded(second.Point);
            return GetPureDirection(first.Point, second.Point);
        }
 
        #endregion // Direction_Utilities
    }
}