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
// GroupBoundaryIntersections.cs
// MSAGL Obstacle class for tracking Group boundary crossings for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear {
    // A Group is a Shape that has children.
    // This class maps between intersection points on Group boundaries and the groups and crossing
    // directions at those intersection points.
    internal class GroupBoundaryCrossingMap {
        // Note:  Like VisibilityGraph, this does not use PointComparer but assumes already-rounded key values.
        readonly Dictionary<Point, List<GroupBoundaryCrossing>> pointCrossingMap = new Dictionary<Point, List<GroupBoundaryCrossing>>();

        internal int Count { get { return pointCrossingMap.Count; } }

        internal GroupBoundaryCrossing AddIntersection(Point intersection, Obstacle group, Directions dirToInside) {
            List<GroupBoundaryCrossing> crossings;
            if (!pointCrossingMap.TryGetValue(intersection, out crossings)) {
                crossings = new List<GroupBoundaryCrossing>();
                pointCrossingMap[intersection] = crossings;
            }

            // We may hit the same point on neighbor traversal in multiple directions.  We will have more than one item
            // in this list only if there are multiple group boundaries at this point, which should be unusual.
            var crossingsCount = crossings.Count;       // cache for perf
            for (int ii = 0; ii < crossingsCount; ++ii) {
                var crossing = crossings[ii];
                if (crossing.Group == group) {
                    // At a given location for a given group, there is only one valid dirToInside.
                    Debug.Assert(dirToInside == crossing.DirectionToInside, "Mismatched dirToInside");
                    return crossing;
                }
            }
            var newCrossing = new GroupBoundaryCrossing(group, dirToInside);
            crossings.Add(newCrossing);
            return newCrossing;
        }

        internal List<GroupBoundaryCrossing> GetCrossings(Point intersection) {
            List<GroupBoundaryCrossing> crossings;
            pointCrossingMap.TryGetValue(intersection, out crossings);
            return crossings;
        }

        internal void Clear() {
            pointCrossingMap.Clear();
        }

        readonly List<Point> pointList = new List<Point>();

        internal PointAndCrossingsList GetOrderedListBetween(Point start, Point end) {
            if (0 == pointCrossingMap.Count) {
                return null;
            }

            if (PointComparer.Compare(start, end) > 0) {
                Point temp = start;
                start = end;
                end = temp;
            }

            // Start and end are inclusive.
            pointList.Clear();
            foreach (var intersection in pointCrossingMap.Keys) {
                if ((PointComparer.Compare(intersection, start) >= 0) && (PointComparer.Compare(intersection, end) <= 0)) {
                    pointList.Add(intersection);
                }
            }

            pointList.Sort();
            var pointAndCrossingList = new PointAndCrossingsList();
            var numCrossings = pointList.Count;
            for (int ii = 0; ii < numCrossings; ++ii) {
                Point intersect = pointList[ii];
                pointAndCrossingList.Add(intersect, pointCrossingMap[intersect]);
            }
            return pointAndCrossingList;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", pointCrossingMap.Count);
        }
    }
}
