//
// AxisCoordinateEvent.cs
// MSAGL class for axis-coordinate events for sparse VisibilityGraph generation for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;

namespace Microsoft.Msagl.Routing.Rectilinear {
    internal class AxisCoordinateEvent : SweepEvent {
        internal AxisCoordinateEvent(Point p) {
            this.site = p;
        }

        internal override Point Site {
            get { return site; }
        }

        private readonly Point site;
    }
}
