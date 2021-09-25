//
// SparseVisibilityGraphGenerator.cs
// MSAGL base class to create the visibility graph consisting of nlogn ScanSegment intersections for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;

namespace Microsoft.Msagl.Routing.Rectilinear {
    // Scan direction is parallel to the sweepline which moves in the perpendicular direction;
    // i.e. scan direction is "sideways" along the sweepline.  We do several passes, following Clarkson et al.,
    // "Rectilinear shortest paths through polygonal obstacles in O(n (log n)2) time" (checked into the enlistment).
    //   1.  Enumerate all obstacles and load their extreme vertex coordinate projections to the perpendicular axis.
    //   2.  Run a scanline (in each direction) that:
    //      a.  Accumulates the vertices and generates obstacle-related Steiner points.
    //      b.  Generates the ScanSegments.
    //   3.  Iterate in parallel along the ScanSegments and *VertexPoints to determine the sparse intersections
    //       by binary division, as in the paper.
    //   4.  Finally we create the VisibilityVertices and VisibilityEdges along each ScanSegment from its
    //       list of intersections.
    // Differences from the paper largely are due to the paper's creation of non-orthogonal edges along
    // obstacle sides; instead, we create orthogonal edges to the lateral sides of the obstacle's bounding
    // box. Also, we support overlapped obstacles (interior edges are weighted, as in the non-sparse
    // implementation) and groups.
    internal partial class SparseVisibilityGraphGenerator : VisibilityGraphGenerator {
        /// <summary>
        /// The points of obstacle vertices encountered on horizontal scan.
        /// </summary>
        private readonly Set<Point> horizontalVertexPoints = new Set<Point>();

        /// <summary>
        /// The points of obstacle vertices encountered on vertical scan.
        /// </summary>
        private readonly Set<Point> verticalVertexPoints = new Set<Point>();

        /// <summary>
        /// The Steiner points generated at the bounding box of obstacles.
        /// These help ensure that we can "go around" the obstacle, as with the non-orthogonal edges in the paper.
        /// </summary>
        private readonly Set<Point> boundingBoxSteinerPoints = new Set<Point>();

        /// <summary>
        /// Accumulates distinct vertex projections to the X axis during sweep.
        /// </summary>
        private readonly Set<double> xCoordAccumulator = new Set<double>();

        /// <summary>
        /// Accumulates distinct vertex projections to the Y axis during sweep.
        /// </summary>
        private readonly Set<double> yCoordAccumulator = new Set<double>();

        /// <summary>
        /// ScanSegment vector locations on the Y axis; final array after sweep.
        /// </summary>
        private ScanSegmentVector horizontalScanSegmentVector;

        /// <summary>
        /// ScanSegment vector locations on the X axis; final array after sweep.
        /// </summary>
        private ScanSegmentVector verticalScanSegmentVector;

        /// <summary>
        /// The index from a coordinate to a horizontal vector slot.
        /// </summary>
        private readonly Dictionary<double, int> horizontalCoordMap = new Dictionary<double, int>();

        /// <summary>
        /// The index from a point to a vertical vector slot.
        /// </summary>
        private readonly Dictionary<double, int> verticalCoordMap = new Dictionary<double, int>();

        /// <summary>
        /// The index from a coordinate to a vector slot on the axis we are intersecting to.
        /// </summary>
        private Dictionary<double, int> perpendicularCoordMap;

        /// <summary>
        /// The segment vector we are intersecting along.
        /// </summary>
        private ScanSegmentVector parallelSegmentVector;

        /// <summary>
        /// The segment vector we are intersecting to.
        /// </summary>
        private ScanSegmentVector perpendicularSegmentVector;

        /// <summary>
        /// The comparer for points along the horizontal or vertical axis.
        /// </summary>
        private IComparer<Point> currentAxisPointComparer;

        internal SparseVisibilityGraphGenerator()
                : base(wantReflections: false) {
        }

        internal override void Clear() {
            base.Clear();
            this.Cleanup();
        }

        private void Cleanup() {
            this.horizontalVertexPoints.Clear();
            this.verticalVertexPoints.Clear();
            this.boundingBoxSteinerPoints.Clear();
            this.xCoordAccumulator.Clear();
            this.yCoordAccumulator.Clear();
            this.horizontalCoordMap.Clear();
            this.verticalCoordMap.Clear();
        }

        /// <summary>
        /// Generate the visibility graph along which edges will be routed.
        /// </summary>
        /// <returns></returns>
        internal override void GenerateVisibilityGraph() {
            this.AccumulateVertexCoords();
            this.CreateSegmentVectorsAndPopulateCoordinateMaps();
            this.RunScanLineToCreateSegmentsAndBoundingBoxSteinerPoints();
            this.GenerateSparseIntersectionsFromVertexPoints();
            this.CreateScanSegmentTrees();

            HorizontalScanSegments.DevTraceVerifyVisibility();
            VerticalScanSegments.DevTraceVerifyVisibility();
            Debug_AssertGraphIsRectilinear(VisibilityGraph, ObsTree);

            this.Cleanup();
        }

        void AccumulateVertexCoords() {
            // Unlike the paper we only generate lines for extreme vertices (i.e. on the horizontal pass we
            // don't generate a horizontal vertex projection to the Y axis for a vertex that is not on the top
            // or bottom of the obstacle).  So we can just use the bounding box.
            foreach (var obstacle in this.ObsTree.GetAllObstacles()) {
                xCoordAccumulator.Insert(obstacle.VisibilityBoundingBox.Left);
                xCoordAccumulator.Insert(obstacle.VisibilityBoundingBox.Right);
                yCoordAccumulator.Insert(obstacle.VisibilityBoundingBox.Top);
                yCoordAccumulator.Insert(obstacle.VisibilityBoundingBox.Bottom);
            }
        }

        private void CreateSegmentVectorsAndPopulateCoordinateMaps() {
            this.horizontalScanSegmentVector = new ScanSegmentVector(yCoordAccumulator, true);
            this.verticalScanSegmentVector = new ScanSegmentVector(xCoordAccumulator, false);

            for (int slot = 0; slot < this.horizontalScanSegmentVector.Length; ++slot) {
                this.horizontalCoordMap[this.horizontalScanSegmentVector[slot].Coord] = slot;
            }
            for (int slot = 0; slot < this.verticalScanSegmentVector.Length; ++slot) {
                this.verticalCoordMap[this.verticalScanSegmentVector[slot].Coord] = slot;
            }
        }

        private void RunScanLineToCreateSegmentsAndBoundingBoxSteinerPoints() {
            // Do a scanline pass to create scan segments that span the entire height/width of the graph
            // (mixing overlapped with free segments as needed) and generate the type-2 Steiner points.
            base.GenerateVisibilityGraph();
            this.horizontalScanSegmentVector.ScanSegmentsComplete();
            this.verticalScanSegmentVector.ScanSegmentsComplete();
            this.xCoordAccumulator.Clear();
            this.yCoordAccumulator.Clear();
        }

        internal override void InitializeEventQueue(ScanDirection scanDir)
        {
            base.InitializeEventQueue(scanDir);
            this.SetVectorsAndCoordMaps(scanDir);
            this.AddAxisCoordinateEvents(scanDir);
        }

        private void AddAxisCoordinateEvents(ScanDirection scanDir) {
            // Normal event ordering will apply - and will thus order the ScanSegments created in the vectors.
            if (scanDir.IsHorizontal) {
                foreach (var coord in yCoordAccumulator) {
                    base.eventQueue.Enqueue(new AxisCoordinateEvent(new Point(ObsTree.GraphBox.Left - SentinelOffset, coord)));
                }
                return;
            }
            foreach (var coord in xCoordAccumulator) {
                base.eventQueue.Enqueue(new AxisCoordinateEvent(new Point(coord, ObsTree.GraphBox.Bottom - SentinelOffset)));
            }
        }

        protected override void ProcessCustomEvent(SweepEvent evt) {
            if (!ProcessAxisCoordinate(evt)) {
                base.ProcessCustomEvent(evt);
            }
        }

        private bool ProcessAxisCoordinate(SweepEvent evt) {
            var axisEvent = evt as AxisCoordinateEvent;
            if (null != axisEvent) {
                this.CreateScanSegmentsOnAxisCoordinate(axisEvent.Site);
                return true;
            }
            return false;
        }

        protected override bool InsertPerpendicularReflectionSegment(Point start, Point end) {
            Debug.Assert(false, "base.wantReflections is false in Sparse mode so this should never be called");
// ReSharper disable HeuristicUnreachableCode
            return false;
// ReSharper restore HeuristicUnreachableCode
        }

        protected override bool InsertParallelReflectionSegment(Point start, Point end, Obstacle eventObstacle,
                BasicObstacleSide lowNborSide, BasicObstacleSide highNborSide, BasicReflectionEvent action) {
            Debug.Assert(false, "base.wantReflections is false in Sparse mode so this should never be called");
// ReSharper disable HeuristicUnreachableCode
            return false;
// ReSharper restore HeuristicUnreachableCode
        }

        protected override void ProcessVertexEvent(RBNode<BasicObstacleSide> lowSideNode,
                                                   RBNode<BasicObstacleSide> highSideNode, BasicVertexEvent vertexEvent) {
            var vertexPoints = (base.ScanDirection.IsHorizontal) ? this.horizontalVertexPoints : this.verticalVertexPoints;
            vertexPoints.Insert(vertexEvent.Site);

            // For easier reading...
            var lowNborSide = LowNeighborSides.LowNeighbor.Item;
            var highNborSide = HighNeighborSides.HighNeighbor.Item;
            var highDir = base.ScanDirection.Direction;
            var lowDir = base.ScanDirection.OppositeDirection;

            // Generate the neighbor side intersections, regardless of overlaps; these are the type-2 Steiner points.
            var lowSteiner = ScanLineIntersectSide(vertexEvent.Site, lowNborSide);
            var highSteiner = ScanLineIntersectSide(vertexEvent.Site, highNborSide);

            // Add the intersections at the neighbor bounding boxes if the intersection is not at a sentinel.  
            // Go in the opposite direction from the neighbor intersection to find the border between the Steiner 
            // point and vertexEvent.Site (unless vertexEvent.Site is inside the bounding box).
            if (ObsTree.GraphBox.Contains(lowSteiner)) {
                var bboxIntersectBeforeLowSteiner = StaticGraphUtility.RectangleBorderIntersect(lowNborSide.Obstacle.VisibilityBoundingBox, lowSteiner, highDir);
                if (PointComparer.IsPureLower(bboxIntersectBeforeLowSteiner, vertexEvent.Site)) {
                    this.boundingBoxSteinerPoints.Insert(bboxIntersectBeforeLowSteiner);
                }
            }
            if (ObsTree.GraphBox.Contains(highSteiner)) {
                var bboxIntersectBeforeHighSteiner = StaticGraphUtility.RectangleBorderIntersect(highNborSide.Obstacle.VisibilityBoundingBox, highSteiner, lowDir);
                if (PointComparer.IsPureLower(vertexEvent.Site, bboxIntersectBeforeHighSteiner)) {
                    this.boundingBoxSteinerPoints.Insert(bboxIntersectBeforeHighSteiner);
                }
            }

            // Add the corners of the bounding box of the vertex obstacle, if they are visible to the event site.
            // This ensures that we "go around" the obstacle, as with the non-orthogonal edges in the paper.
            Point lowCorner, highCorner;
            GetBoundingCorners(lowSideNode.Item.Obstacle.VisibilityBoundingBox, vertexEvent is OpenVertexEvent, this.ScanDirection.IsHorizontal,
                        out lowCorner, out highCorner);
            if (PointComparer.IsPureLower(lowSteiner, lowCorner) || lowNborSide.Obstacle.IsInSameClump(vertexEvent.Obstacle)) {
                vertexPoints.Insert(lowCorner);
            }
            if (PointComparer.IsPureLower(highCorner, highSteiner) || highNborSide.Obstacle.IsInSameClump(vertexEvent.Obstacle)) {
                vertexPoints.Insert(highCorner);
            }
        }

        private static void GetBoundingCorners(Rectangle boundingBox, bool isLowSide, bool isHorizontal, out Point lowCorner, out Point highCorner) {
            if (isLowSide) {
                lowCorner = boundingBox.LeftBottom;
                highCorner = isHorizontal ? boundingBox.RightBottom : boundingBox.LeftTop;
                return;
            }
            lowCorner = isHorizontal ? boundingBox.LeftTop : boundingBox.RightBottom;
            highCorner = boundingBox.RightTop;
        }

        private void CreateScanSegmentsOnAxisCoordinate(Point site) {
            base.CurrentGroupBoundaryCrossingMap.Clear();

            // Iterate the ScanLine and create ScanSegments.  There will always be at least the two sentinel sides.
            var sideNode = base.scanLine.Lowest();
            var nextNode = base.scanLine.NextHigh(sideNode);
            var overlapDepth = 0;
            var start = site;
            bool isInsideOverlappedObstacle = false;
            for (; null != nextNode; nextNode = base.scanLine.NextHigh(nextNode)) {
                if (SkipSide(start, nextNode.Item)) {
                    continue;
                }

                if (nextNode.Item.Obstacle.IsGroup) {
                    // Do not create internal group crossings in non-overlapped obstacles.
                    if ((overlapDepth == 0) || isInsideOverlappedObstacle) {
                        HandleGroupCrossing(site, nextNode.Item);
                    }
                    continue;
                }

                var isLowSide = nextNode.Item is LowObstacleSide;
                if (isLowSide) {
                    if (overlapDepth > 0) {
                        ++overlapDepth;
                        continue;
                    }

                    // We are not overlapped, so create a ScanSegment from the previous side intersection to the
                    // intersection with the side in nextNode.Item.
                    start = CreateScanSegment(start, nextNode.Item, ScanSegment.NormalWeight);
                    base.CurrentGroupBoundaryCrossingMap.Clear();
                    overlapDepth = 1;
                    isInsideOverlappedObstacle = nextNode.Item.Obstacle.IsOverlapped;
                    continue;
                }

                // This is a HighObstacleSide.  If we've got overlap nesting, decrement the depth.
                Debug.Assert(overlapDepth > 0, "Overlap depth must be positive");
                --overlapDepth;
                if (overlapDepth > 0) {
                    continue;
                }

                // If we are not within an overlapped obstacle, don't bother creating the overlapped ScanSegment
                // as there will never be visibility connecting to it.
                start = (nextNode.Item.Obstacle.IsOverlapped || nextNode.Item.Obstacle.OverlapsGroupCorner)
                        ? this.CreateScanSegment(start, nextNode.Item, ScanSegment.OverlappedWeight)
                        : this.ScanLineIntersectSide(start, nextNode.Item);
                base.CurrentGroupBoundaryCrossingMap.Clear();
                isInsideOverlappedObstacle = false;
            }

            // The final piece.
            var end = base.ScanDirection.IsHorizontal
                    ? new Point(ObsTree.GraphBox.Right + SentinelOffset, start.Y)
                    : new Point(start.X, ObsTree.GraphBox.Top + SentinelOffset);
            this.parallelSegmentVector.CreateScanSegment(start, end, ScanSegment.NormalWeight,
                    base.CurrentGroupBoundaryCrossingMap.GetOrderedListBetween(start, end));
            this.parallelSegmentVector.ScanSegmentsCompleteForCurrentSlot();
        }

        private void HandleGroupCrossing(Point site, BasicObstacleSide groupSide) {
            if (!base.ScanLineCrossesObstacle(site, groupSide.Obstacle)) {
                return;
            }
            // Here we are always going left-to-right.  As in base.SkipToNeighbor, we don't stop traversal for groups,
            // neither do we create overlapped edges (unless we're inside a non-group obstacle).  Instead we turn
            // the boundary crossing on or off based on group membership at ShortestPath-time.  Even though this is
            // the sparse VG, we always create these edges at group boundaries so we don't skip over them.
            Direction dirToInsideOfGroup = (groupSide is LowObstacleSide) ? base.ScanDirection.Direction : base.ScanDirection.OppositeDirection;
            var intersect = this.ScanLineIntersectSide(site, groupSide);
            var crossing = base.CurrentGroupBoundaryCrossingMap.AddIntersection(intersect, groupSide.Obstacle, dirToInsideOfGroup);

            // The vertex crossing the edge is perpendicular to the group boundary.  A rectilinear group will also have
            // an edge parallel to that group boundary that includes the point of that crossing vertex; therefore we must
            // split that non-crossing edge at that vertex.
            AddPerpendicularCoordForGroupCrossing(intersect);

            // Similarly, the crossing edge's opposite vertex may be on a perpendicular segment.
            var interiorPoint = crossing.GetInteriorVertexPoint(intersect);
            AddPerpendicularCoordForGroupCrossing(interiorPoint);
        }

        private void AddPerpendicularCoordForGroupCrossing(Point intersect) {
            var nonCrossingPerpSlot = this.FindPerpendicularSlot(intersect, 0);
            if (-1 != nonCrossingPerpSlot) {
                this.perpendicularSegmentVector[nonCrossingPerpSlot].AddPendingPerpendicularCoord(this.parallelSegmentVector.CurrentSlot.Coord);
            }
        }

        private bool SkipSide(Point start, BasicObstacleSide side) {
            if (side.Obstacle.IsSentinel) {
                return true;
            }

            // Skip sides of obstacles that we do not actually pass through.
            var bbox = side.Obstacle.VisibilityBoundingBox;
            if (base.ScanDirection.IsHorizontal) {
                return ((start.Y == bbox.Bottom) || (start.Y == bbox.Top));
            }
            return ((start.X == bbox.Left) || (start.X == bbox.Right));
        }

        private Point CreateScanSegment(Point start, BasicObstacleSide side, double weight) {
            var end = ScanLineIntersectSide(start, side);
            if (start != end) {
                this.parallelSegmentVector.CreateScanSegment(start, end, weight, CurrentGroupBoundaryCrossingMap.GetOrderedListBetween(start, end));
            }
            return end;
        }

        private void GenerateSparseIntersectionsFromVertexPoints() {
            this.VisibilityGraph = NewVisibilityGraph();

            // Generate the sparse intersections between ScanSegments based upon the ordered vertexPoints.
            GenerateSparseIntersectionsAlongHorizontalAxis();
            GenerateSparseIntersectionsAlongVerticalAxis();

            this.ConnectAdjoiningScanSegments();

            // Now each segment has the coordinates all of its intersections, so create the visibility graph.
            this.horizontalScanSegmentVector.CreateSparseVerticesAndEdges(this.VisibilityGraph);
            this.verticalScanSegmentVector.CreateSparseVerticesAndEdges(this.VisibilityGraph);
        }

        private void GenerateSparseIntersectionsAlongHorizontalAxis() {
            this.currentAxisPointComparer = new HorizontalPointComparer();
            var vertexPoints = this.horizontalVertexPoints.OrderBy(point => point, this.currentAxisPointComparer).ToArray();
            var bboxSteinerPoints = this.boundingBoxSteinerPoints.OrderBy(point => point, this.currentAxisPointComparer).ToList();
            base.ScanDirection = ScanDirection.HorizontalInstance;
            SetVectorsAndCoordMaps(base.ScanDirection);
            this.GenerateSparseIntersections(vertexPoints, bboxSteinerPoints);
        }

        private void GenerateSparseIntersectionsAlongVerticalAxis() {
            this.currentAxisPointComparer = new VerticalPointComparer();
            var vertexPoints = this.verticalVertexPoints.OrderBy(point => point, this.currentAxisPointComparer).ToArray();
            var bboxSteinerPoints = this.boundingBoxSteinerPoints.OrderBy(point => point, this.currentAxisPointComparer).ToList();
            base.ScanDirection = ScanDirection.VerticalInstance;
            SetVectorsAndCoordMaps(base.ScanDirection);
            this.GenerateSparseIntersections(vertexPoints, bboxSteinerPoints);
        }

        private void SetVectorsAndCoordMaps(ScanDirection scanDir) {
            if (scanDir.IsHorizontal) {
                this.parallelSegmentVector = this.horizontalScanSegmentVector;
                this.perpendicularSegmentVector = this.verticalScanSegmentVector;
                this.perpendicularCoordMap = this.verticalCoordMap;
            } else {
                this.parallelSegmentVector = this.verticalScanSegmentVector;
                this.perpendicularSegmentVector = this.horizontalScanSegmentVector;
                this.perpendicularCoordMap = this.horizontalCoordMap;
            }
        }

        private void ConnectAdjoiningScanSegments() {
            // Ensure there is a vertex at the end/start point of two ScanSegments; these will always differ in overlappedness.
            this.horizontalScanSegmentVector.ConnectAdjoiningSegmentEndpoints();
            this.verticalScanSegmentVector.ConnectAdjoiningSegmentEndpoints();
        }

        internal class HorizontalPointComparer : IComparer<Point> {
            // Order by vertical first, then horizontal.
            public int Compare(Point lhs, Point rhs) {
                var cmp = lhs.Y.CompareTo(rhs.Y);
                return (0 != cmp) ? cmp : lhs.X.CompareTo(rhs.X);
            }
        }

        public class VerticalPointComparer : IComparer<Point> {
            // Order by horizontal first, then vertical.
            public int Compare(Point lhs, Point rhs) {
                var cmp = lhs.X.CompareTo(rhs.X);
                return (0 != cmp) ? cmp : lhs.Y.CompareTo(rhs.Y);
            }
        }

        private void GenerateSparseIntersections(Point [] vertexPoints, List<Point> bboxSteinerPoints) {
            this.perpendicularSegmentVector.ResetForIntersections();
            this.parallelSegmentVector.ResetForIntersections();

            // Position the enumerations to the first point.
            //vertexPoints.MoveNext();
            int j = 0;
            int i = 0;
            foreach (var item in parallelSegmentVector.Items) {
                for (; ; ) {
                    if (!item.CurrentSegment.ContainsPoint(vertexPoints[i])) {
                        // Done accumulating intersections for the current segment; move to the next segment.
                        if (!this.AddSteinerPointsToInterveningSegments(vertexPoints[i], bboxSteinerPoints, ref j, item)
                                || !item.TraverseToSegmentContainingPoint(vertexPoints[i])) {
                            // Done with this vectorItem, move to the next item.
                            break;
                        }
                    }

                    this.AddPointsToCurrentSegmentIntersections(bboxSteinerPoints, ref j, item);
                    this.GenerateIntersectionsFromVertexPointForCurrentSegment(vertexPoints[i], item);

                    if (item.PointIsCurrentEndAndNextStart(vertexPoints[i])) {
                        // MoveNext will always return true because the test to enter this block returned true.
                        item.MoveNext();
                        Debug.Assert(item.HasCurrent, "MoveNext ended before EndAndNextStart");
                        continue;
                    }

                    if (++i >= vertexPoints.Length) {
                        // No more vertexPoints; we're done.
                        return;
                    }
                }
            }

            // We should have exited in the "no more vertexPoints" case above.
            Debug.Assert(false, "Mismatch in points and segments");
        }

        private bool AddSteinerPointsToInterveningSegments(Point currentVertexPoint, List<Point> bboxSteinerPoints, ref int j, ScanSegmentVectorItem item) {
            // With overlaps, we may have bboxSteinerPoints on segments that do not contain vertices.
            while (j < boundingBoxSteinerPoints.Count && (this.currentAxisPointComparer.Compare(bboxSteinerPoints[j], currentVertexPoint) == -1)) {
                if (!item.TraverseToSegmentContainingPoint(bboxSteinerPoints[j])) {
                    // Done with this vectorItem, move to the next item.
                    return false;
                }
                this.AddPointsToCurrentSegmentIntersections(bboxSteinerPoints, ref j, item);
            }
            return true;
        }

        private void AddPointsToCurrentSegmentIntersections(List<Point> pointsToAdd, ref int j, ScanSegmentVectorItem parallelItem) {
            // The first Steiner point should be in the segment, unless we have a non-orthogonal or overlapped or both situation
            // that results in no Steiner points having been generated, or Steiner points being generated on a segment that has
            // the opposite overlap state from the segment containing the corresponding vertex.
            for (; j < pointsToAdd.Count && parallelItem.CurrentSegment.ContainsPoint(pointsToAdd[j]); j++) {
                int steinerSlot = this.FindPerpendicularSlot(pointsToAdd[j], 0);
                this.AddSlotToSegmentIntersections(parallelItem, steinerSlot);
            }
        }

        private void GenerateIntersectionsFromVertexPointForCurrentSegment(Point site, ScanSegmentVectorItem parallelItem) {
            int perpStartSlot = this.FindPerpendicularSlot(parallelItem.CurrentSegment.Start, 1);
            int perpEndSlot = this.FindPerpendicularSlot(parallelItem.CurrentSegment.End, -1);
            int siteSlot = this.FindPerpendicularSlot(site, 0);

            // See comments in FindIntersectingSlot; we don't add non-extreme vertices in the perpendicular direction
            // so in some heavily-overlapped scenarios, we may not have any intersections within this scan segment.
            if (perpStartSlot >= perpEndSlot) {
                return;
            }

            this.AddSlotToSegmentIntersections(parallelItem, perpStartSlot);
            this.AddSlotToSegmentIntersections(parallelItem, perpEndSlot);
            if ((siteSlot > perpStartSlot) && (siteSlot < perpEndSlot)) {
                this.AddSlotToSegmentIntersections(parallelItem, siteSlot);
                this.AddBinaryDivisionSlotsToSegmentIntersections(parallelItem, perpStartSlot, siteSlot, perpEndSlot);
            }
        }
 
        // These are called when the site may not be in the vector.
        private int FindPerpendicularSlot(Point site, int directionIfMiss) {
            return FindIntersectingSlot(perpendicularSegmentVector, perpendicularCoordMap, site, directionIfMiss);
        }

        private static int FindIntersectingSlot(ScanSegmentVector segmentVector, Dictionary<double, int> coordMap, Point site, int directionIfMiss) {
            var coord = segmentVector.GetParallelCoord(site);
            int slot;
            if (coordMap.TryGetValue(coord, out slot)) {
                return slot;
            }

            // There are a few cases where the perpCoord is not in the map:
            // 1.  The first ScanSegment in a slot will have a Start at the sentinel, which is before the first 
            //     perpendicular segment; similarly, the last ScanSegment in a slot will have an out-of-range End.
            // 2.  Sequences of overlapped/nonoverlapped scan segments that pass through obstacles.  Their start
            //     and end points are not in vertexPoints because they were not vertex-derived, so we find the
            //     closest bracketing coordinates that are in the vectors.
            // 3.  Non-extreme vertices in the perpendicular direction (e.g. for a triangle, we add the X's of
            //     the left and right to the coords, but not of the top).
            // 4.  Non-rectilinear group side intersections.
            return (0 == directionIfMiss) ? -1 : segmentVector.FindNearest(coord, directionIfMiss);
        }

        private void AddSlotToSegmentIntersections(ScanSegmentVectorItem parallelItem, int perpSlot) {
            ScanSegmentVectorItem perpItem = this.perpendicularSegmentVector[perpSlot];
            parallelItem.CurrentSegment.AddSparseVertexCoord(perpItem.Coord);
            perpItem.AddPerpendicularCoord(parallelItem.Coord);
        }

        private void AddBinaryDivisionSlotsToSegmentIntersections(ScanSegmentVectorItem parallelItem, int startSlot, int siteSlot, int endSlot) {
            // The input parameters' slots have already been added to the segment's coords.  
            // If there was no object to the low or high side, then the start or end slot was already
            // the graphbox max (0 or perpSegmentVector.Length, respectively).  So start dividing.
            int low = 0;
            int high = this.perpendicularSegmentVector.Length - 1;

            // Terminate when we are one away because we don't have an edge from a point to itself.
            while ((high - low) > 1) {
                int mid = low + ((high - low) / 2);

                // We only use the half of the graph that the site is in, so arbitrarily decide that it is
                // in the lower half if it is at the midpoint.
                if (siteSlot <= mid) {
                    high = mid;
                    if ((siteSlot < high) && (high <= endSlot)) {
                        this.AddSlotToSegmentIntersections(parallelItem, high);
                    }
                    continue;
                }
                low = mid;
                if ((siteSlot > low) && (low >= startSlot)) {
                    this.AddSlotToSegmentIntersections(parallelItem, low);
                }
            }
        }

        // Create the ScanSegmentTrees that functions as indexes for port-visibility splicing.
        private void CreateScanSegmentTrees() {
            CreateScanSegmentTree(this.horizontalScanSegmentVector, this.HorizontalScanSegments);
            CreateScanSegmentTree(this.verticalScanSegmentVector, this.VerticalScanSegments);
        }

        private static void CreateScanSegmentTree(ScanSegmentVector segmentVector, ScanSegmentTree segmentTree) {
            foreach (var item in segmentVector.Items) {
                for (var segment = item.FirstSegment; segment != null; segment = segment.NextSegment) {
                    if (segment.HasVisibility()) {
                        segmentTree.InsertUnique(segment);
                    }
                }
            }
        }
    }
}