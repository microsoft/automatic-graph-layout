// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentOrderer.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests.Rectilinear 
{
    internal class SegmentOrderer : IComparer<LineSegment> 
    {
        private readonly TestPointComparer comparer;

        internal SegmentOrderer(TestPointComparer comp)
        {
            this.comparer = comp;
        }

        /// <summary>
        /// The requisite comparison method
        /// </summary>
        /// <param name="lhs">The left-hand side.</param>
        /// <param name="rhs">The right-hand side.</param>
        /// <returns>-1 if lhs is less than rhs, 1 if lhs is greater than rhs, else 0</returns>
        public int Compare(LineSegment lhs, LineSegment rhs)
        {
            Validate.IsNotNull(lhs, "Lhs must not be null");
            Validate.IsNotNull(rhs, "Rhs must not be null");
            var cmp = comparer.Compare(lhs.Start.X, rhs.Start.X);
            if (0 == cmp)
            {
                cmp = comparer.Compare(lhs.Start.Y, rhs.Start.Y);
            }

            // At same vertex so get the horizontal first - that means the greater X first.
            if (0 == cmp)
            {
                cmp = comparer.Compare(rhs.End.X, lhs.End.X);
            }
            if (0 == cmp)
            {
                cmp = comparer.Compare(rhs.End.Y, lhs.End.Y);
            }
            return cmp;
        }
    }
} // end namespace TestRectilinear
