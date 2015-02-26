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
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using BBox = Microsoft.Msagl.Core.Geometry.Rectangle;
using P2=Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// 
    /// </summary>
    internal class Line : Geometry {
        internal P2 start, end;
        double lineWidth;

        internal double LineWidth {
            get { return lineWidth; }
        }
        internal Line(DObject tag, P2 start, P2 end, double lw)
            : base(tag) {
            lineWidth = lw;
            P2 dir = end - start;
            if (lineWidth < 0)
                lineWidth = 1;

            double len = dir.Length;
            if (len > ApproximateComparer.IntersectionEpsilon) {
                dir /= (len / (lineWidth / 2));
                dir = dir.Rotate(Math.PI / 2);
            } else {
                dir.X = 0;
                dir.Y = 0;
            }

            this.bBox = new BBox(start + dir);
            this.bBox.Add(start - dir);
            this.bBox.Add(end + dir);
            this.bBox.Add(end - dir);
            this.start = start;
            this.end = end;

            if (this.bBox.LeftTop.X == this.bBox.RightBottom.X) {
                bBox.LeftTop = bBox.LeftTop + new P2(-0.05f, 0);
                bBox.RightBottom = bBox.RightBottom + new P2(0.05f, 0);
            }
            if (this.bBox.LeftTop.Y == this.bBox.RightBottom.Y) {
                bBox.LeftTop = bBox.LeftTop + new P2(0, -0.05f);
                bBox.RightBottom = bBox.RightBottom + new P2(0, 0.05f);
            }

        }
    }
}
