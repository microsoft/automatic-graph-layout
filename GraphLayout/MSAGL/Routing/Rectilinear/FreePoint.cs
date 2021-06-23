//
// FreePoint.cs
// MSAGL class for non-obstacle path control points for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System;
using System.Diagnostics;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear{
    using SegmentAndCrossings = Tuple<LineSegment, PointAndCrossingsList>;

    /// <summary>
    /// This is a point on a path that is not associated with an obstacle, such as
    /// a port for the end of a dragged path, or a waypoint.
    /// </summary>
    internal class FreePoint {

        // The VisibilityVertex for this path point; created if it does not already exist.
        internal VisibilityVertex Vertex { get; private set; }
        internal Point Point { get { return Vertex.Point; } }
        internal bool IsOverlapped { get; set; }
        internal double InitialWeight { get { return this.IsOverlapped ? ScanSegment.OverlappedWeight : ScanSegment.NormalWeight; } }
        internal Direction OutOfBoundsDirectionFromGraph { get; set; }
        internal bool IsOutOfBounds { get { return Direction. None != OutOfBoundsDirectionFromGraph; }}

        private readonly SegmentAndCrossings[] maxVisibilitySegmentsAndCrossings = new SegmentAndCrossings[4];

        // Called if we must create the vertex.
        internal FreePoint(TransientGraphUtility transUtil, Point point) {
            OutOfBoundsDirectionFromGraph = Direction. None;
            this.GetVertex(transUtil, point);
        }

        internal void GetVertex(TransientGraphUtility transUtil, Point point) {
            this.Vertex = transUtil.FindOrAddVertex(point);
        }

        // Adds an edge from this.Vertex to a (possibly new) vertex at an intersection with an
        // existing Edge that adjoins the point.  We take 'dir' as an input parameter for edge
        // extension because we may be on the edge so can't calculate the direction.
        internal VisibilityVertex AddEdgeToAdjacentEdge(TransientGraphUtility transUtil
                    , VisibilityEdge targetEdge, Direction dirToExtend, Rectangle limitRect) {
            Point targetIntersect = StaticGraphUtility.SegmentIntersection(targetEdge, this.Point);
            VisibilityVertex targetVertex = transUtil.VisGraph.FindVertex(targetIntersect);
            if (null != targetVertex) {
                AddToAdjacentVertex(transUtil, targetVertex, dirToExtend, limitRect);
            }
            else {
                targetVertex = transUtil.AddEdgeToTargetEdge(this.Vertex, targetEdge, targetIntersect);
            }
            ExtendEdgeChain(transUtil, targetVertex, dirToExtend, limitRect);
            return targetVertex;
        }

        internal void AddToAdjacentVertex(TransientGraphUtility transUtil
                    , VisibilityVertex targetVertex, Direction dirToExtend, Rectangle limitRect) {
            if (!PointComparer.Equal(this.Point, targetVertex.Point)) {
                transUtil.FindOrAddEdge(this.Vertex, targetVertex, InitialWeight);
            }
            ExtendEdgeChain(transUtil, targetVertex, dirToExtend, limitRect);
        }

        internal void ExtendEdgeChain(TransientGraphUtility transUtil, VisibilityVertex targetVertex, Direction dirToExtend, Rectangle limitRect) {
            // Extend the edge chain to the opposite side of the limit rectangle.
            StaticGraphUtility.Assert(PointComparer.Equal(this.Point, targetVertex.Point)
                        || (PointComparer.GetPureDirection(this.Point, targetVertex.Point) == dirToExtend)
                        , "input dir does not match with to-targetVertex direction", transUtil.ObstacleTree, transUtil.VisGraph);

            var extendOverlapped = IsOverlapped;
            if (extendOverlapped)
            {
                // The initial vertex we connected to may be on the border of the enclosing obstacle,
                // or of another also-overlapped obstacle.  If the former, we turn off overlap now.
                extendOverlapped = transUtil.ObstacleTree.PointIsInsideAnObstacle(targetVertex.Point, dirToExtend);
            }

            // If we're inside an obstacle's boundaries we'll never extend past the end of the obstacle
            // due to encountering the boundary from the inside.  So start the extension at targetVertex.
            SegmentAndCrossings segmentAndCrossings = GetSegmentAndCrossings(this.IsOverlapped ? targetVertex : this.Vertex, dirToExtend, transUtil);
            transUtil.ExtendEdgeChain(targetVertex, limitRect, segmentAndCrossings.Item1, segmentAndCrossings.Item2, extendOverlapped);
        }

        private SegmentAndCrossings GetSegmentAndCrossings(VisibilityVertex startVertex, Direction dirToExtend, TransientGraphUtility transUtil) {
            var dirIndex = CompassVector.ToIndex(dirToExtend);
            var segmentAndCrossings = this.maxVisibilitySegmentsAndCrossings[dirIndex];
            if (null == segmentAndCrossings) {
                PointAndCrossingsList pacList;
                var maxVisibilitySegment = transUtil.ObstacleTree.CreateMaxVisibilitySegment(startVertex.Point, dirToExtend, out pacList);
                segmentAndCrossings = new SegmentAndCrossings(maxVisibilitySegment, pacList);
                this.maxVisibilitySegmentsAndCrossings[dirIndex] = segmentAndCrossings;
            } else {
                // For a waypoint this will be a target and then a source, so there may be a different lateral edge to
                // connect to. In that case make sure we are consistent in directions - back up the start point if needed.
                if (PointComparer.GetDirections(startVertex.Point, segmentAndCrossings.Item1.Start) == dirToExtend) {
                    segmentAndCrossings.Item1.Start = startVertex.Point;
                }
            }
            return segmentAndCrossings;
        }

        internal Point MaxVisibilityInDirectionForNonOverlappedFreePoint(Direction dirToExtend, TransientGraphUtility transUtil) {
            Debug.Assert(!this.IsOverlapped, "Do not precalculate overlapped obstacle visibility as we should extend from the outer target vertex instead");
            SegmentAndCrossings segmentAndCrossings = GetSegmentAndCrossings(this.Vertex, dirToExtend, transUtil);
            return segmentAndCrossings.Item1.End;
        }

        internal void AddOobEdgesFromGraphCorner(TransientGraphUtility transUtil, Point cornerPoint) {
            Direction dirs = PointComparer.GetDirections(cornerPoint, Vertex.Point);
            VisibilityVertex cornerVertex = transUtil.VisGraph.FindVertex(cornerPoint);

            // For waypoints we want to be able to enter in both directions. 
            transUtil.ConnectVertexToTargetVertex(cornerVertex, this.Vertex, dirs & (Direction.North | Direction.South), ScanSegment.NormalWeight);
            transUtil.ConnectVertexToTargetVertex(cornerVertex, this.Vertex, dirs & (Direction.East | Direction.West), ScanSegment.NormalWeight);
        }

        internal void RemoveFromGraph() {
            // Currently all transient removals and edge restorations are done by TransientGraphUtility itself.
            Vertex = null;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return Vertex.ToString();
        }
    }
}