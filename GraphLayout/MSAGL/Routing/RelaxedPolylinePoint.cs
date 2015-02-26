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
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing {
    internal class RelaxedPolylinePoint  {
        private PolylinePoint polylinePoint;

        internal PolylinePoint PolylinePoint {
            get { return polylinePoint; }
            set { polylinePoint = value; }
        }
        private Point originalPosition;

        internal Point OriginalPosition {
            get { return originalPosition; }
            set { originalPosition = value; }
        }
        
        internal RelaxedPolylinePoint(PolylinePoint polylinePoint, Point originalPosition) {
            this.PolylinePoint = polylinePoint;
            this.OriginalPosition = originalPosition;
        }

        RelaxedPolylinePoint next;

        internal RelaxedPolylinePoint Next {
            get { return next; }
            set { next = value; }
        }
        RelaxedPolylinePoint prev;

        internal RelaxedPolylinePoint Prev {
            get { return prev; }
            set { prev = value; }
        }
    }
}
