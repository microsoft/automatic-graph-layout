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

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner {
    internal class Cone {
        bool removed;

        internal bool Removed {
            get { return removed; }
            set { removed = value; }
        }
        Point apex;
        readonly IConeSweeper coneSweeper;

        internal Cone(Point apex, IConeSweeper coneSweeper) {
            this.apex = apex;
            this.coneSweeper = coneSweeper;
        }

        internal Point Apex {
            get { return apex; }
            set { apex = value; }
        }

        internal Point RightSideDirection {
            get { return coneSweeper.ConeRightSideDirection; }
        }

        internal Point LeftSideDirection {
            get { return coneSweeper.ConeLeftSideDirection; }
        }



        private ConeSide rightSide;

        internal ConeSide RightSide {
            get { return rightSide; }
            set { rightSide = value;
            rightSide.Cone = this;
            }
        }
        private ConeSide leftSide;

        internal ConeSide LeftSide {
            get { return leftSide; }
            set { 
                leftSide = value;
                leftSide.Cone = this;
            }
        }

        internal bool PointIsInside(Point p)
        {
            return Point.PointToTheRightOfLineOrOnLine(p, Apex, Apex + LeftSideDirection) &&
            Point.PointToTheLeftOfLineOrOnLine(p, Apex, Apex + RightSideDirection);
        }
    }
}
