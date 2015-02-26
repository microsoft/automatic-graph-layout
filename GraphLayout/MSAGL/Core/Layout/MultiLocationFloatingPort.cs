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

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// Same behavior as RelativeFloatingPort but layout engines can choose the best relative
    /// location for routing from the list of possible locations.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multi")]
    public class MultiLocationFloatingPort : RelativeFloatingPort {
        private List<Point> PossibleOffsets;

        /// <summary>
        /// Enumerate the offsets this was created with
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Point> LocationOffsets { get { return PossibleOffsets; } }

        /// <summary>
        /// LocationOffset is PossibleOffsets[ActiveOffsetIndex]
        /// </summary>
        public int ActiveOffsetIndex { get; private set; }
        
        /// <summary>
        /// LocationOffset is PossibleOffsets[ActiveOffsetIndex]
        /// </summary>
        public override Point LocationOffset {
            // The set() is private to RelativeFloatingPort; this class should set ActiveOffsetIndex
            // rather than setting LocationOffset directly.
            get {
                return PossibleOffsets[ActiveOffsetIndex];
            }
        }
        
        /// <summary>
        /// Same behavior as RelativeFloatingPort but layout engines can choose the best offset for routing
        /// from the list of possible offsets.
        /// </summary>
        /// <param name="curveDelegate">The curve the locations are relative to</param>
        /// <param name="centerDelegate">The center the locations are relative to</param>
        /// <param name="possibleOffsets">The offsets from the center that form the locations</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public MultiLocationFloatingPort(Func<ICurve> curveDelegate,
                Func<Point> centerDelegate, List<Point> possibleOffsets)
            : base(curveDelegate, centerDelegate) {
            PossibleOffsets = possibleOffsets;
        }
        
        /// <summary>
        /// Set LocationOffset to the PossibleOffset + CenterDelegate() that is closest to point.
        /// </summary>
        /// <param name="point"></param>
        internal void SetClosestLocation(Point point) {
            // Set the ActiveOffsetIndex to the first offset initially; this indirectly sets
            // Location via LocationOffset.
            ActiveOffsetIndex = 0;
            double mind = (point - Location).LengthSquared;

            // If there's another offset that's closer to the point, update ActiveOffsetIndex.
            int n = PossibleOffsets.Count;
            for (int i = 1; i < n; ++i) {
                var p = PossibleOffsets[i] + CenterDelegate();
                double d = (point - p).LengthSquared;
                if (d < mind) {
                    mind = d;
                    ActiveOffsetIndex = i;
                }
            }
        }


        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var builder = new System.Text.StringBuilder();
            builder.Append(CenterDelegate());
            builder.Append(" [");
            bool first = false;
            foreach (Point p in LocationOffsets) {
                if (first) {
                    builder.Append(", ");
                    first = false;
                }
                builder.Append(p.ToString());
            }
            builder.Append("]");
            return builder.ToString();
        }
    }
}
