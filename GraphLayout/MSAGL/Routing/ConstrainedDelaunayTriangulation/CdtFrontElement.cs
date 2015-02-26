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
﻿using System.Diagnostics;

namespace Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation {
    internal class CdtFrontElement {
        //The LeftSite should coincide with the leftmost end of the Edge, and the edge should not be vertical

        internal CdtSite LeftSite;
        internal CdtEdge Edge;
        internal CdtSite RightSite;

        internal double X {
            get { return LeftSite.Point.X; }
        }

        internal CdtFrontElement(CdtSite leftSite, CdtEdge edge) {
            Debug.Assert(edge.upperSite.Point.X != edge.lowerSite.Point.X &&
                         edge.upperSite.Point.X < edge.lowerSite.Point.X && leftSite == edge.upperSite ||
                         edge.upperSite.Point.X > edge.lowerSite.Point.X && leftSite == edge.lowerSite);
            RightSite = edge.upperSite == leftSite ? edge.lowerSite : edge.upperSite;
            LeftSite = leftSite;
            Edge = edge;
        }
    }
}