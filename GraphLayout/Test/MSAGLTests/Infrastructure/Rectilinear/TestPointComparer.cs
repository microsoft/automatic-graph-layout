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
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestPointComparer.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.UnitTests.Rectilinear 
{
    /// <summary>
    /// A simple PointComparer that is more permissive than the internal version since
    /// it is comparing across runs rather than consistent evaluation within a single run.
    /// Also its methods are not static, so we can have multiple instances with different
    /// tolerances.  And it has both Rounding and direct-comparison (IsClose()) features.
    /// </summary>
    internal class TestPointComparer : IEqualityComparer<Point>
    {
        /// <summary>
        /// Determines whether the specified Points are equal after rounding.
        /// </summary>
        /// <param name="x">The first object of type Point to compare.</param>
        /// <param name="y">The second object of type Point to compare.</param>
        /// <returns>
        /// True if the specified points are equal after rounding; otherwise, false.
        /// </returns>
        public bool Equals(Point x, Point y)
        {
            return IsEqual(x, y);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <returns>
        /// A hash code for the specified object.
        /// </returns>
        /// <param name="point">The point for which a hash code is to be returned.</param>
        public int GetHashCode(Point point)
        {
            return Round(point).GetHashCode();
        }

        private readonly int numberOfDigitsToRound;

        internal double CloseEpsilon { get { return Math.Pow(10, -this.numberOfDigitsToRound); } }

        internal double RoundEpsilon { get { return Math.Pow(10, -this.numberOfDigitsToRound) / 2; } }

        // More permissive than PointComparer
        internal static int DefaultNumberOfDigitsToRound { get { return 4; } }

        internal TestPointComparer()
        {
            this.numberOfDigitsToRound = DefaultNumberOfDigitsToRound;
        }

        internal TestPointComparer(int digitsToRound)
        {
            this.numberOfDigitsToRound = digitsToRound;
        }

        internal Point Round(Point point)
        {
            return new Point(Round(point.X), Round(point.Y));
        }

        internal double Round(double value)
        {
            return Math.Round(value, this.numberOfDigitsToRound);
        }

        internal bool IsEqual(Point a, Point b) 
        {
            return IsEqual(a.X, b.X) && IsEqual(a.Y, b.Y);
        }

        internal bool IsEqual(double x, double y) 
        {
            return 0 == Compare(x, y);
        }

        internal bool IsEqual(LineSegment a, LineSegment b) 
        {
            return IsEqual(a.Start, b.Start) && IsEqual(a.End, b.End);
        }

        internal int Compare(double lhs, double rhs) 
        {
            double c = Round(lhs) - Round(rhs);
            if (c < -RoundEpsilon)
            {
                return -1;
            }
            return (c > RoundEpsilon) ? 1 : 0;
        }

        internal bool IsClose(Point a, Point b) 
        {
            return IsClose(a.X, b.X) && IsClose(a.Y, b.Y);
        }

        internal bool IsClose(double a, double b) 
        {
            return Math.Abs(a - b) <= CloseEpsilon;
        }

        internal bool IsClose(LineSegment a, LineSegment b) 
        {
            return IsClose(a.Start, b.Start) && IsClose(a.End, b.End);
        }

        internal int CompareClose(Point a, Point b) 
        {
            if (IsClose(a, b))
            {
                return 0;
            }
            int cmp = CompareClose(a.X, b.X);
            if (0 == cmp) 
            {
                cmp = CompareClose(a.Y, b.Y);
            }
            return cmp;
        }

        internal int CompareClose(double a, double b) 
        {
            return IsClose(a, b) ? 0 : a.CompareTo(b);
        }

        internal int CompareClose(LineSegment a, LineSegment b) 
        {
            var cmp = CompareClose(a.Start, b.Start);
            if (0 == cmp) 
            {
                cmp = CompareClose(a.End, b.End);
            }
            return cmp;
        }
    }
} // end namespace TestRectilinear
