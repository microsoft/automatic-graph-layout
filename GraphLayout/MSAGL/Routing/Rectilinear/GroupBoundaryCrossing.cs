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
// GroupBoundaryCrossing.cs
// MSAGL Obstacle class for a Group boundary crossing for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear {
    // A Group is a Shape that has children.
    // This class defines a single crossing of a group boundary, from a point on the group boundary.
    // It is intended as the Value of a GroupBoundaryCrossingMap entry, or as an element in a VisiblityEdge.GroupCrossings
    // array, so the actual crossing coordinates are not included.
    internal class GroupBoundaryCrossing {
        // The group to which this applies.
        internal Obstacle Group { get; set; }

        // The direction from the vertex on the group boundary toward the inside of the group.
        internal Directions DirectionToInside { get; private set; }

        internal GroupBoundaryCrossing(Obstacle group, Directions dirToInside) {
            Debug.Assert(CompassVector.IsPureDirection(dirToInside), "Impure direction");
            this.Group = group;
            this.DirectionToInside = dirToInside;
        }

        static internal double BoundaryWidth { get { return ApproximateComparer.DistanceEpsilon; } }

        internal Point GetInteriorVertexPoint(Point outerVertex) {
            return ApproximateComparer.Round(outerVertex + (CompassVector.ToPoint(DirectionToInside) * BoundaryWidth));
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1}", DirectionToInside, Group);
        }
    }
}
