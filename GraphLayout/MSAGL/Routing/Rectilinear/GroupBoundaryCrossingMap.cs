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

        internal GroupBoundaryCrossing AddIntersection(Point intersection, Obstacle group, Direction dirToInside) {
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
