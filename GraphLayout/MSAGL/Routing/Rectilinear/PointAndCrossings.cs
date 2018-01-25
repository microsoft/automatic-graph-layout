//
// PointAndCrossings.cs
// MSAGL class for a Point and any Group boundary crossings at that Point, for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.
using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear {
    internal struct PointAndCrossings {
        internal Point Location { get; private set; }
        internal List<GroupBoundaryCrossing> Crossings { get; private set; }

        internal PointAndCrossings(Point loc, List<GroupBoundaryCrossing> crossings) : this() {
            Location = loc;
            Crossings = crossings;
        }
    }
}