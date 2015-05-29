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