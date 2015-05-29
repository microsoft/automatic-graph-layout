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
