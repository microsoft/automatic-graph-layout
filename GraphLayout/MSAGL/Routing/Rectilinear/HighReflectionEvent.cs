//
// HighReflectionEvent.cs
// MSAGL Obstacle class for reflection events from a HighObstacleSide for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// This records an intersection of a lookahead ray with an obstacle edge.
    /// </summary>
    internal class HighReflectionEvent : BasicReflectionEvent {
        internal HighObstacleSide Side { get; private set; }

        internal HighReflectionEvent(BasicReflectionEvent previousSite, HighObstacleSide targetSide, Point site)
            : base (previousSite, targetSide.Obstacle, site) {
            this.Side = targetSide;
        }
    }
}
