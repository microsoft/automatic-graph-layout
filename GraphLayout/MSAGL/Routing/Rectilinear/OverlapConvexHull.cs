//
// OverlapConvexHull.cs
// MSAGL class for Convex Hulls around overlapping obstacles for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System.Collections.Generic;
using System.Linq;

using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// This stores the location and type of a Port.
    /// </summary>
    internal class OverlapConvexHull {
        internal Polyline Polyline { get; private set; }

        /// <summary>
        /// This is some arbitrary obstacle inside the convex hull so we qualify Select().Where() so we
        /// don't get the CH duplicated in the scanline etc. enumerations.
        /// </summary>
        internal Obstacle PrimaryObstacle { get; private set; }

        internal List<Obstacle> Obstacles { get; private set; }

        internal OverlapConvexHull(Polyline polyline, IEnumerable<Obstacle> obstacles) {
            this.Polyline = polyline;
            this.Obstacles = obstacles.ToList();
            this.PrimaryObstacle = this.Obstacles[0];

            Obstacle.RoundVerticesAndSimplify(this.Polyline);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return this.Polyline.ToString();
        }
    }
}
