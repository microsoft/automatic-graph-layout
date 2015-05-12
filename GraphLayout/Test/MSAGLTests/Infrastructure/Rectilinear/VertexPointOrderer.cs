// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VertexPointOrderer.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.UnitTests.Rectilinear 
{
    internal class VertexPointOrderer : IComparer<Point> 
    {
        /// <summary>
        /// The requisite comparison method
        /// </summary>
        /// <param name="lhs">The left-hand side.</param>
        /// <param name="rhs">The right-hand side.</param>
        /// <returns>-1 if lhs is less than rhs, 1 if lhs is greater than rhs, else 0</returns>
        public int Compare(Point lhs, Point rhs)
        {
            var cmp = lhs.X.CompareTo(rhs.X);
            if (0 == cmp)
            {
                cmp = lhs.Y.CompareTo(rhs.Y);
            }
            return cmp;
        }
    }
} // end namespace TestRectilinear
