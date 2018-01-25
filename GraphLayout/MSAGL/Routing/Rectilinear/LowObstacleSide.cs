//
// LowObstacleSide.cs
// MSAGL class for ObstacleSides with the lower scanline-parallel coordinates for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// LowObstacleSide is a side of the obstacle that is between LowestVertex and HighestVertex
    /// to the lower scanline-parallel coordinate-value side; i.e. to the left (lower X-coordinate)
    /// for horizontal scan (vertical sweep), and to the right (due to Cartesian coordinates) for Y.
    /// This is different from LeftObstacleSide, which refers to the traversal direction being along
    /// the clockwise polyline direction; LowObstacleSide uses the ctor arg traverseClockwise to manage that.
    /// </summary>
    internal class LowObstacleSide : BasicObstacleSide {
        internal LowObstacleSide(Obstacle obstacle, PolylinePoint startVertex, ScanDirection scanDir)
            : base(obstacle, startVertex, scanDir, scanDir.IsHorizontal /*traverseClockwise*/)
        {
        }
    }
}
