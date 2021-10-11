//
// ObstaclePortEntrance.cs
// MSAGL class for a single entrance point on an Obstacle for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// An ObstaclePortEntrance is a single edge entering or leaving an obstacle in one of the NSEW Compass directions.
    /// </summary>
    internal class ObstaclePortEntrance {
        ObstaclePort ObstaclePort { get; set; }
        Obstacle Obstacle { get { return ObstaclePort.Obstacle; } }

        // The intersection point on the obstacle border (e.g. intersection with a port point, or
        // midpoint of PortEntry) and the direction from that point to find the outer vertex.
        internal Point UnpaddedBorderIntersect { get; private set; }
        internal Direction OutwardDirection { get; private set; }
        internal Point VisibilityBorderIntersect { get; private set; }
        internal bool IsOverlapped { get; private set; }
        internal double InitialWeight { get { return this.IsOverlapped ? ScanSegment.OverlappedWeight : ScanSegment.NormalWeight; } }
        private double unpaddedToPaddedBorderWeight = ScanSegment.NormalWeight; 
        
        internal bool IsCollinearWithPort {
            get { return CompassVector.IsPureDirection(PointComparer.GetDirections(this.VisibilityBorderIntersect, this.ObstaclePort.Location)); }
        }

        // The line segment from VisibilityBorderIntersect to the first obstacle it hits.
        internal LineSegment MaxVisibilitySegment { get; private set; }
        private readonly PointAndCrossingsList pointAndCrossingsList;
        internal bool IsVertical { get { return StaticGraphUtility.IsVertical(this.MaxVisibilitySegment); } }

        // If the port has entrances that are collinear, don't do the optimization for non-collinear ones.
        internal bool WantVisibilityIntersection {
            get { return !this.IsOverlapped && this.CanExtend && (!this.ObstaclePort.HasCollinearEntrances || this.IsCollinearWithPort); }
        }
        internal bool CanExtend { get { return PointComparer.GetDirections(this.MaxVisibilitySegment.Start, this.MaxVisibilitySegment.End) != Direction. None; } }

        internal ObstaclePortEntrance(ObstaclePort oport, Point unpaddedBorderIntersect, Direction outDir, ObstacleTree obstacleTree) {
            ObstaclePort = oport;
            UnpaddedBorderIntersect = unpaddedBorderIntersect;
            OutwardDirection = outDir;

            // Get the padded intersection.
            var lineSeg = new LineSegment(UnpaddedBorderIntersect, StaticGraphUtility.RectangleBorderIntersect(
                                            oport.Obstacle.VisibilityBoundingBox, UnpaddedBorderIntersect, outDir));
            IList<IntersectionInfo> xxs = Curve.GetAllIntersections(lineSeg, oport.Obstacle.VisibilityPolyline, true /*liftIntersections*/);
            Debug.Assert(1 == xxs.Count, "Expected one intersection");
            this.VisibilityBorderIntersect = ApproximateComparer.Round(xxs[0].IntersectionPoint);

            this.MaxVisibilitySegment = obstacleTree.CreateMaxVisibilitySegment(this.VisibilityBorderIntersect,
                    this.OutwardDirection, out this.pointAndCrossingsList);

            // Groups are never in a clump (overlapped) but they may still have their port entrance overlapped.
            if (this.Obstacle.IsOverlapped || (this.Obstacle.IsGroup && !this.Obstacle.IsInConvexHull)) {
                this.IsOverlapped = obstacleTree.IntersectionIsInsideAnotherObstacle(/*sideObstacle:*/ null, this.Obstacle
                        , this.VisibilityBorderIntersect, ScanDirection.GetInstance(OutwardDirection));
                if (!this.Obstacle.IsGroup || this.IsOverlapped || this.InteriorEdgeCrossesObstacle(obstacleTree)) {
                    unpaddedToPaddedBorderWeight = ScanSegment.OverlappedWeight;
                }
            }
            if (this.Obstacle.IsInConvexHull && (unpaddedToPaddedBorderWeight == ScanSegment.NormalWeight)) {
                SetUnpaddedToPaddedBorderWeightFromHullSiblingOverlaps(obstacleTree);
            }
        }

        private void SetUnpaddedToPaddedBorderWeightFromHullSiblingOverlaps(ObstacleTree obstacleTree) {
            if (this.Obstacle.IsGroup ? this.InteriorEdgeCrossesObstacle(obstacleTree) : this.InteriorEdgeCrossesConvexHullSiblings()) {
                this.unpaddedToPaddedBorderWeight = ScanSegment.OverlappedWeight;
            }
        }

        private bool InteriorEdgeCrossesObstacle(ObstacleTree obstacleTree) {
            // File Test: Nudger_Overlap4
            // Use the VisibilityBoundingBox for groups because those are what the tree consists of.
            var rect = new Rectangle(this.UnpaddedBorderIntersect, this.VisibilityBorderIntersect);
            return InteriorEdgeCrossesObstacle(rect, obs => obs.VisibilityPolyline,
                    obstacleTree.Root.GetLeafRectangleNodesIntersectingRectangle(rect)
                        .Where(node => !node.UserData.IsGroup && (node.UserData != this.Obstacle)).Select(node => node.UserData));
        }

        private bool InteriorEdgeCrossesConvexHullSiblings() {
            // There is no RectangleNode tree that includes convex hull non-primary siblings, so we just iterate;
            // this will only be significant to perf in extremely overlapped cases that we are not optimizing for.
            var rect = new Rectangle(this.UnpaddedBorderIntersect, this.VisibilityBorderIntersect);
            return InteriorEdgeCrossesObstacle(rect, obs => obs.PaddedPolyline,
                    this.Obstacle.ConvexHull.Obstacles.Where(obs => obs != this.Obstacle));
        }

        private bool InteriorEdgeCrossesObstacle(Rectangle rect, Func<Obstacle, Polyline> whichPolylineToUse, IEnumerable<Obstacle> candidates) {
            LineSegment lineSeg = null;
            foreach (var blocker in candidates) {
                var blockerPolyline = whichPolylineToUse(blocker);
                if (!StaticGraphUtility.RectangleInteriorsIntersect(rect, blockerPolyline.BoundingBox)) {
                    continue;
                }
                lineSeg = lineSeg ?? new LineSegment(this.UnpaddedBorderIntersect, this.VisibilityBorderIntersect);
                var xx = Curve.CurveCurveIntersectionOne(lineSeg, blockerPolyline, liftIntersection: false);
                if (xx != null) {
                    return true;
                }
                if (PointLocation.Outside != Curve.PointRelativeToCurveLocation(this.UnpaddedBorderIntersect, blockerPolyline)) {
                    return true;
                }
            }
            return false;
        }

        internal bool HasGroupCrossings { get { return (this.pointAndCrossingsList != null) && (this.pointAndCrossingsList.Count > 0); } }

        internal bool HasGroupCrossingBeforePoint(Point point) {
            if (!this.HasGroupCrossings) {
                return false;
            }
            var pac = StaticGraphUtility.IsAscending(this.OutwardDirection) ? this.pointAndCrossingsList.First : this.pointAndCrossingsList.Last;
            return PointComparer.GetDirections(this.MaxVisibilitySegment.Start, pac.Location) == PointComparer.GetDirections(pac.Location, point);
        }

        internal void AddToAdjacentVertex(TransientGraphUtility transUtil, VisibilityVertex targetVertex
                            , Rectangle limitRect, bool routeToCenter) {
            VisibilityVertex borderVertex = transUtil.VisGraph.FindVertex(this.VisibilityBorderIntersect);
            if (borderVertex != null) {
                ExtendEdgeChain(transUtil, borderVertex, borderVertex, limitRect, routeToCenter);
                return;
            }

            // There is no vertex at VisibilityBorderIntersect, so create it and link it to targetVertex.
            // Note: VisibilityBorderIntersect may == targetIntersect if that is on our border, *and*
            // targetIntersect may be on the border of a touching obstacle, in which case this will splice
            // into or across the adjacent obstacle, which is consistent with "touching is overlapped".
            // So we don't use UnpaddedBorderIntersect as prevPoint when calling ExtendEdgeChain.

            // VisibilityBorderIntersect may be rounded just one Curve.DistanceEpsilon beyond the ScanSegment's
            // perpendicular coordinate; e.g. our X may be targetIntersect.X + Curve.DistanceEpsilon, thereby
            // causing the direction from VisibilityBorderIntersect to targetIntersect to be W instead of E.
            // So use the targetIntersect if they are close enough; they will be equal for flat borders, and
            // otherwise the exact value we use only needs be "close enough" to the border.  (We can't use
            // CenterVertex as the prevPoint because that could be an impure direction).
            // Update: With the change to carry MaxVisibilitySegment within the PortEntrance, PortManager finds
            // targetVertex between VisibilityBorderIntersect and MaxVisibilitySegment.End, so this should no longer
            // be able to happen.
            // See RectilinearTests.PaddedBorderIntersectMeetsIncomingScanSegment for an example of what happens
            // when VisibilityBorderIntersect is on the incoming ScanSegment (it jumps out above with borderVertex found).
            if (OutwardDirection == PointComparer.GetPureDirection(targetVertex.Point, this.VisibilityBorderIntersect)) {
                Debug.Assert(false, "Unexpected reversed direction between VisibilityBorderIntersect and targetVertex");
// ReSharper disable HeuristicUnreachableCode
                this.VisibilityBorderIntersect = targetVertex.Point;
                borderVertex = targetVertex;
// ReSharper restore HeuristicUnreachableCode
            }
            else {
                borderVertex = transUtil.FindOrAddVertex(this.VisibilityBorderIntersect);
                transUtil.FindOrAddEdge(borderVertex, targetVertex,  InitialWeight);
            }
            ExtendEdgeChain(transUtil, borderVertex, targetVertex, limitRect, routeToCenter);
        }
        
        internal void ExtendEdgeChain(TransientGraphUtility transUtil, VisibilityVertex paddedBorderVertex
                                , VisibilityVertex targetVertex, Rectangle limitRect, bool routeToCenter) {
            // Extend the edge chain to the opposite side of the limit rectangle.
            transUtil.ExtendEdgeChain(targetVertex, limitRect, this.MaxVisibilitySegment, this.pointAndCrossingsList, this.IsOverlapped);

            // In order for Nudger to be able to map from the (near-) endpoint vertex to a PortEntry, we must 
            // always connect a vertex at UnpaddedBorderIntersect to the paddedBorderVertex, even if routeToCenter.
            var unpaddedBorderVertex = transUtil.FindOrAddVertex(UnpaddedBorderIntersect);
            transUtil.FindOrAddEdge(unpaddedBorderVertex, paddedBorderVertex, this.unpaddedToPaddedBorderWeight);
            if (routeToCenter) {
                // Link the CenterVertex to the vertex at UnpaddedBorderIntersect.
                transUtil.ConnectVertexToTargetVertex(ObstaclePort.CenterVertex, unpaddedBorderVertex, OutwardDirection, InitialWeight);
            }
        }

        
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1}~{2} {3}",
                                ObstaclePort.Location, UnpaddedBorderIntersect, this.VisibilityBorderIntersect, OutwardDirection);
        }
    }
}