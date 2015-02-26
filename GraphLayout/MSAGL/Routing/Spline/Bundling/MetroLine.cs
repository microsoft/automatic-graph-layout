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
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// holds the data of a path
    /// </summary>
#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=203
    //SharpKit/Colin - Interface implementations
    // FP: this needs to be public because it is referenced by interfaces elsewhere. It's not public in the .NET version because that version can use explicitly-defined interfaces.
    public class Metroline {
#else
    internal class Metroline {
#endif
        internal double Width;
        internal int Count { get { return Polyline.Count; } }
        internal double Length { get; set; }

        internal double IdealLength { get; set; }

        internal Polyline Polyline { get; set; }
        public int Index { get; set; }

        internal Metroline(Polyline polyline, double width, Func<Tuple<Polyline, Polyline>> sourceAndTargetLoosePolys, int index) {
            Width = width;
            Polyline = polyline;
            this.sourceAndTargetLoosePolylines = sourceAndTargetLoosePolys;
        }

        internal void UpdateLengths() {
            var l = 0.0;
            for (var p = Polyline.StartPoint; p.Next != null; p = p.Next) {
                l += (p.Next.Point - p.Point).Length;
            }
            Length = l;
            IdealLength = (Polyline.End - Polyline.Start).Length;
        }

        internal Func<Tuple<Polyline, Polyline>> sourceAndTargetLoosePolylines;
    }
}