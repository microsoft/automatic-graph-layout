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
