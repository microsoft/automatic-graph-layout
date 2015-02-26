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
using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core;

namespace Microsoft.Msagl.Routing.Visibility {
    internal class ObstacleSideComparer : IComparer<SegmentBase> {

        readonly LineSweeperBase lineSweeper;


        internal ObstacleSideComparer(LineSweeperBase lineSweeper) {
            this.lineSweeper = lineSweeper;
        }

        /// <summary>
        /// the intersection of the sweepline and the active segment
        /// </summary>
        Point x;


        public int Compare(SegmentBase a, SegmentBase b) {
            ValidateArg.IsNotNull(b, "b");
            var orient = Point.GetTriangleOrientation(b.Start, b.End, x);
            switch (orient) {
                case TriangleOrientation.Collinear:
                    return 0;
                case TriangleOrientation.Clockwise:
                    return 1;
                default:
                    return -1;
            }
        }


        internal void SetOperand(SegmentBase side) {
            x = IntersectionOfSideAndSweepLine(side);
        }

        internal Point IntersectionOfSideAndSweepLine(SegmentBase obstacleSide) {
            var den = obstacleSide.Direction * lineSweeper.SweepDirection;
            Debug.Assert(Math.Abs(den) > ApproximateComparer.DistanceEpsilon);
            var t = (lineSweeper.Z - obstacleSide.Start * lineSweeper.SweepDirection) / den;
            return obstacleSide.Start + t * obstacleSide.Direction;
        }

    }
}