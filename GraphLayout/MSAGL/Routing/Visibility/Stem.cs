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

namespace Microsoft.Msagl.Routing.Visibility {
    /// <summary>
    /// represents a chunk of a hole boundary
    /// </summary>
    internal class Stem {
        PolylinePoint start;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal PolylinePoint Start {
            get { return start; }
            set { start = value; }
        }
        PolylinePoint end;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal PolylinePoint End {
            get { return end; }
            set { end = value; }
        }
     
        
        internal Stem(PolylinePoint start, PolylinePoint end) {
            System.Diagnostics.Debug.Assert(start.Polyline==end.Polyline);
            this.start = start;
            this.end = end;
        }

        internal IEnumerable<PolylinePoint> Sides {
            get {
                PolylinePoint v=start;
                
                while(v!= end ) {
                    PolylinePoint side = v;
                    yield return side;
                    v = side.NextOnPolyline;
                }
            }
        }

        internal bool MoveStartClockwise() {
            if (Start != End) {
                Start = Start.NextOnPolyline;
                return true;
            }
            return false;
        }

        public override string ToString() {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "Stem({0},{1})", Start, End);
        }

    }
}
