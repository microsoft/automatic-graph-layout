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
//
// BasicObstacleSide.cs
// MSAGL base class for ObstacleSides for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// BasicObstacleSide is base class for an obstacle side that is to the low or high end of the
    /// scanline-parallel coordinate, and knows which direction to traverse to find the endVertex.
    /// This is different from RightObstacleSide or LeftObstacleSide, where the class itself is the
    /// determinant of traversal direction being with or opposite to the clockwise polyline direction;
    /// BasicObstacleSide uses the ctor arg traverseClockwise to manage that.
    /// </summary>
    internal abstract class BasicObstacleSide : ObstacleSide {
        internal Obstacle Obstacle { get; private set; }
        readonly PolylinePoint endVertex;
        internal double Slope { get; private set; }
        internal double SlopeInverse { get; private set; }

        internal BasicObstacleSide(Obstacle obstacle, PolylinePoint startVertex, ScanDirection scanDir, bool traverseClockwise)
            : base(startVertex)
        {
            Obstacle = obstacle;
            endVertex = traverseClockwise ? startVertex.NextOnPolyline : startVertex.PrevOnPolyline;
            if (!scanDir.IsPerpendicular(startVertex.Point, endVertex.Point))
            {
                Slope = StaticGraphUtility.Slope(startVertex.Point, endVertex.Point, scanDir);
                SlopeInverse = 1.0 / Slope;
            }
        }
        
        internal override PolylinePoint EndVertex {
            get { return endVertex; }
        }
    }
}
