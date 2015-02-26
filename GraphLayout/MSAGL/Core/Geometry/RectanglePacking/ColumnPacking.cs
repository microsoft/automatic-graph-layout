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
﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Geometry
{
    /// <summary>
    /// Flow fill of columns to some maximum height
    /// </summary>
    public class ColumnPacking<TData> : Packing
    {
        private IEnumerable<RectangleToPack<TData>> orderedRectangles;
        private double maxHeight;

        /// <summary>
        /// Constructor for packing, call Run to do the actual pack.
        /// Each RectangleToPack.Rectangle is updated in place.
        /// Pack rectangles tallest to shortest, left to right until wrapWidth is reached, 
        /// then wrap to right-most rectangle still with vertical space to fit the next rectangle
        /// </summary>
        /// <param name="rectangles"></param>
        /// <param name="maxHeight"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public ColumnPacking(IEnumerable<RectangleToPack<TData>> rectangles, double maxHeight)
        {
            this.orderedRectangles = rectangles;
            this.maxHeight = maxHeight;
        }

        /// <summary>
        /// Pack columns by iterating over rectangle enumerator until column height exceeds wrapHeight.
        /// When that happens, create a new column at position PackedWidth.
        /// </summary>
        protected override void RunInternal()
        {
            PackedHeight = PackedWidth = 0;
            double columnPosition = 0;
            double columnHeight = 0;
            foreach (var current in orderedRectangles)
            {
                Rectangle r = current.Rectangle;
                if (columnHeight + r.Height > maxHeight)
                {
                    columnPosition = PackedWidth;
                    columnHeight = 0;
                }
                r = current.Rectangle = new Rectangle(columnPosition, columnHeight,
                        new Point(r.Width, r.Height));
                PackedWidth = Math.Max(PackedWidth, columnPosition + r.Width);
                columnHeight += r.Height;
                PackedHeight = Math.Max(PackedHeight, columnHeight);
            }
        }
    }
}
