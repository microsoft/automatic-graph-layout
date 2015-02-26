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
// HighObstacleSide.cs
// MSAGL class for ObstacleSides with the higher scanline-parallel coordinates for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.
using Microsoft.Msagl;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// HighObstacleSide is a side of the obstacle that is between LowestVertex and HighestVertex
    /// to the higher scanline-parallel coordinate-value side; i.e. to the right (higher X-coordinate)
    /// for horizontal scan (vertical sweep), and to the left (due to Cartesian coordinates) for Y.
    /// This is different from RightObstacleSide, which refers to the traversal direction being opposite
    /// the clockwise polyline direction; HighObstacleSide uses the ctor arg traverseClockwise to manage that.
    /// </summary>
    internal class HighObstacleSide : BasicObstacleSide {
        internal HighObstacleSide(Obstacle obstacle, PolylinePoint startVertex, ScanDirection scanDir)
            : base(obstacle, startVertex, scanDir, scanDir.IsVertical /*traverseClockwise*/)
        {
        }
    }
}