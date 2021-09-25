//
// FullVisibilityGraphGenerator.cs
// MSAGL base class to create the visibility graph consisting of all ScanSegment intersections for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;

namespace Microsoft.Msagl.Routing.Rectilinear {
    // Scan direction is parallel to the sweepline which moves in the perpendicular direction;
    // i.e. scan direction is "sideways" along the sweepline.  We also have lookahead scans
    // that enqueue events along the scan-primary coordinate (in the direction of the scan, i.e.
    // X for Hscan, Y for Vscan) to handle reflections from non-orthogonal obstacle sides, 
    // and lookback scans that have not had their reflections calculated because they reflect
    // backward from the scanline and thus must be picked up on a subsequent perpendicular sweep.
    internal class FullVisibilityGraphGenerator : VisibilityGraphGenerator {
        internal FullVisibilityGraphGenerator() : base(wantReflections:true) {
        }

        // This tracks the last (usually) added ScanSegment for subsumption as an optimization to
        // searching the tree for fully-internal subsumptions.
        ScanSegment hintScanSegment;

        /// <summary>
        /// Generate the visibility graph along which edges will be routed.
        /// </summary>
        /// <returns></returns>
        internal override void GenerateVisibilityGraph() {
            if (null == ObsTree.Root) {
                return;
            }

            // This will initialize events and call ProcessEvents which will call our specific functionality
            // to create scan segments.
            base.GenerateVisibilityGraph();

            // Merge any ScanSegments that share intervals.
            HorizontalScanSegments.MergeSegments();
            VerticalScanSegments.MergeSegments();

            // Done now with the ScanSegment generation; intersect them to create the VisibilityGraph.
            IntersectScanSegments();
            Debug_AssertGraphIsRectilinear(VisibilityGraph, ObsTree);
        }

        private void IntersectScanSegments()
        {
            var si = new SegmentIntersector();
            this.VisibilityGraph = si.Generate(HorizontalScanSegments.Segments, VerticalScanSegments.Segments);
            si.RemoveSegmentsWithNoVisibility(HorizontalScanSegments, VerticalScanSegments);
            HorizontalScanSegments.DevTraceVerifyVisibility();
            VerticalScanSegments.DevTraceVerifyVisibility();
        }

        internal override void InitializeEventQueue(ScanDirection scanDir) {
            base.InitializeEventQueue(scanDir);
            hintScanSegment = null;
        }

        internal override void Clear() {
            base.Clear();
            hintScanSegment = null;
        }

        protected override bool InsertPerpendicularReflectionSegment(Point start, Point end) {
            // If we're on the Vertical pass, the Horizontal pass may have loaded a Lookahead perpendicular
            // segment between two non-overlapped obstacles at a non-extremevertex point with exactly the
            // same span we have here, -or- we may be adding the non-overlapped extension of an overlapped
            // segment at exactly the same point that the horizontal pass added a reflection.
            // See TestRectilinear.Reflection_Staircase_Stops_At_BoundingBox_Side*.
            if (null != PerpendicularScanSegments.Find(start, end)) {
                return false;
            }
            PerpendicularScanSegments.InsertUnique(new ScanSegment(start, end, ScanSegment.ReflectionWeight, gbcList: null));
            return true;
        }

        protected override bool InsertParallelReflectionSegment(Point start, Point end, Obstacle eventObstacle,
                BasicObstacleSide lowNborSide, BasicObstacleSide highNborSide, BasicReflectionEvent action) {
            // See notes in InsertPerpendicularReflectionSegment for comments about an existing segment.
            // Here, we call AddSegment which adds the segment and continues the reflection staircase.
            if (null != ParallelScanSegments.Find(start, end)) {
                return false;
            }
            return AddSegment(start, end, eventObstacle, lowNborSide, highNborSide, action, ScanSegment.ReflectionWeight);
        }

        // Return value is whether or not we added a new segment.
        bool AddSegment(Point start, Point end, Obstacle eventObstacle
                                , BasicObstacleSide lowNborSide, BasicObstacleSide highNborSide
                                , SweepEvent action, double weight) {
            DevTraceInfoVgGen(1, "Adding Segment [{0} -> {1} {2}] weight {3}", start, end, weight);
            DevTraceInfoVgGen(2, "     side {0}", lowNborSide);
            DevTraceInfoVgGen(2, "  -> side {0}", highNborSide);
            if (PointComparer.Equal(start, end)) {
                return false;
            }

            // See if the new segment subsumes or can be subsumed by the last one.  gbcList may be null.
            PointAndCrossingsList gbcList = CurrentGroupBoundaryCrossingMap.GetOrderedListBetween(start, end);
            bool extendStart, extendEnd;
            bool wasSubsumed = ScanSegment.Subsume(ref hintScanSegment, start, end, weight, gbcList, ScanDirection
                                        , ParallelScanSegments, out extendStart, out extendEnd);
            if (!wasSubsumed) {
                Debug.Assert((weight != ScanSegment.ReflectionWeight) || (ParallelScanSegments.Find(start, end) == null),
                            "Reflection segments already in the ScanSegmentTree should should have been detected before calling AddSegment");
                hintScanSegment = ParallelScanSegments.InsertUnique(new ScanSegment(start, end, weight, gbcList)).Item;
            } else if (weight == ScanSegment.ReflectionWeight) {
                // Do not continue this; it is probably a situation where a side is at a tiny angle from the axis,
                // resulting in an initial reflection segment that is parallel and very close to the extreme-vertex-derived
                // segment, so as the staircase progresses they eventually converge due to floating-point rounding.
                // See RectilinearFilesTest.ReflectionStaircasesConverge.
                return false;
            }

            // Do reflections only if the new segment is not overlapped.
            if (ScanSegment.OverlappedWeight != weight) {
                // If these fire, it's probably an indication that isOverlapped is not correctly set
                // and one of the neighbors is an OverlapSide from CreateScanSegments.
                Debug.Assert(lowNborSide is HighObstacleSide, "lowNbor is not HighObstacleSide");
                Debug.Assert(highNborSide is LowObstacleSide, "highNbor is not LowObstacleSide");

                // If we are closing the obstacle then the initial Obstacles of the reflections (the ones it
                // will bounce between) are the opposite neighbors.  Otherwise, the OpenVertexEvent obstacle
                // is the ReflectionEvent initial obstacle.
                if (action is CloseVertexEvent) {
                    // If both neighbor sides reflect upward, they can't intersect, so we don't need
                    // to store a lookahead site (if neither reflect upward, StoreLookaheadSite no-ops).
                    if (!SideReflectsUpward(lowNborSide) || !SideReflectsUpward(highNborSide)) {
                        // Try to store both; only one will "take" (for the upward-reflecting side).
                        // The initial Obstacle is the opposite neighbor.
                        if (extendStart) {
                            this.StoreLookaheadSite(highNborSide.Obstacle, lowNborSide, start, wantExtreme:false);
                        }
                        if (extendEnd) {
                            this.StoreLookaheadSite(lowNborSide.Obstacle, highNborSide, end, wantExtreme: false);
                        }
                    }
                }
                else {
                    if (extendStart) {
                        StoreLookaheadSite(eventObstacle, LowNeighborSides.GroupSideInterveningBeforeLowNeighbor, lowNborSide, start);
                    }
                    if (extendEnd) {
                        StoreLookaheadSite(eventObstacle, HighNeighborSides.GroupSideInterveningBeforeHighNeighbor, highNborSide, end);
                    }
                }
            }

            DevTraceInfoVgGen(2, "HintScanSegment {0}{1}", hintScanSegment, wasSubsumed ? " (subsumed)" : "");
            DevTrace_DumpScanSegmentsDuringAdd(3);
            return true;
        }

        private void StoreLookaheadSite(Obstacle eventObstacle, BasicObstacleSide interveningGroupSide, BasicObstacleSide neighborSide, Point siteOnSide) {
            // For reflections, NeighborSides won't be set, so there won't be an intervening group.  Otherwise,
            // this is on an OpenVertexEvent, so we'll either reflect of the intervening group if any, or neighborSide.
            if (null == interveningGroupSide) {
                this.StoreLookaheadSite(eventObstacle, neighborSide, siteOnSide, wantExtreme: false);
            } else {
                var siteOnGroup = ScanLineIntersectSide(siteOnSide, interveningGroupSide, this.ScanDirection);
                this.StoreLookaheadSite(eventObstacle, interveningGroupSide, siteOnGroup, wantExtreme: false);
            }
        }

        private bool IntersectionAtSideIsInsideAnotherObstacle(BasicObstacleSide side, BasicVertexEvent vertexEvent) {
            Point intersect = ScanLineIntersectSide(vertexEvent.Site, side);
            return IntersectionAtSideIsInsideAnotherObstacle(side, vertexEvent.Obstacle, intersect);
        }

        // obstacleToIgnore is the event obstacle if we're looking at intersections along its boundary.
        private bool IntersectionAtSideIsInsideAnotherObstacle(BasicObstacleSide side, Obstacle eventObstacle, Point intersect) {
            // See if the intersection with an obstacle side is inside another obstacle (that encloses
            // at least the part of side.Obstacle containing the intersection).  This will only happen
            // if side.Obstacle is overlapped and in the same clump (if it's not the same clump, we must
            // be hitting it from the outside).
            if (!side.Obstacle.IsOverlapped) {
                return false;
            }
            if (!side.Obstacle.IsGroup && !eventObstacle.IsGroup && (side.Obstacle.Clump != eventObstacle.Clump)) {
                return false;
            }
            return ObsTree.IntersectionIsInsideAnotherObstacle(side.Obstacle, eventObstacle, intersect, ScanDirection);
        }

        // As described in the document, we currently don't create ScanSegments where a flat top/bottom boundary may have
        // intervals that are embedded within overlapped segments:
        //      obstacle1 |  |obstacle2  | obstacle3 |          | obstacle4 | obstacle2|  |
        //                |  +-----------|===========|??????????|===========|----------+  | obstacle5
        //   ...__________|              |___________|          |___________|             |__________...
        // Here, there will be no ScanSegment created at ??? along the border of obstacle2 between obstacle3
        // and obstacle4.  This is not a concern because that segment is useless anyway; a path from outside
        // obstacle2 will not be able to see it unless there is another obstacle in that gap, and then that
        // obstacle's side-derived ScanSegments will create non-overlapped edges; and there are already edges
        // from the upper/lower extreme vertices of obstacle 3 and obstacle4 to ensure a connected graph.
        // If there is a routing from an obstacle outside obstacle2 to one embedded within obstacle2, the
        // port visibility will create the necessary edges.
        //
        // We don't try to determine nesting depth and create different VisibilityEdge weights to prevent spurious
        // nested-obstacle crossings; we just care about overlapped vs. not-overlapped.
        // If this changes, we would have to: Find the overlap depth at the lowNborSide intersection,
        // then increment/decrement according to side type as we move from low to high, then create a different
        // ScanSegment at each obstacle-side crossing, making ScanSegment.IsOverlapped a depth instead of bool.
        // Then pass that depth through to VisibilityEdge as an increased weight.  (This would also automatically
        // handle the foregoing situation of non-overlapped intervals in the middle of a flat top/bottom border,
        // not that it would really gain anything).
        void CreateScanSegments(Obstacle obstacle, HighObstacleSide lowNborSide, BasicObstacleSide lowOverlapSide, 
                                BasicObstacleSide highOverlapSide, LowObstacleSide highNborSide, BasicVertexEvent vertexEvent) {
            // If we have either of the high/low OverlapSides, we'll need to see if they're inside
            // another obstacle.  If not, they end the overlap.
            if ((null == highOverlapSide) || IntersectionAtSideIsInsideAnotherObstacle(highOverlapSide, vertexEvent)) {
                highOverlapSide = highNborSide;
            }
            if ((null == lowOverlapSide) || IntersectionAtSideIsInsideAnotherObstacle(lowOverlapSide, vertexEvent)) {
                lowOverlapSide = lowNborSide;
            }

            // There may be up to 3 segments; for a simple diagram, |# means low-side border of
            // obstacle '#' and #| means high-side, with 'v' meaning our event vertex.  Here are
            // the two cases where we create a single non-overlapped ScanSegment (from the Low
            // side in the diagram, but the same logic works for the High side).
            //  - non-overlapped:   1|  v  |2
            //                 ...---+     +---...
            //  - non-overlapped to an "inner" highNbor on a flat border:   1|  vLow  |2   vHigh
            //                                                          ...---+  +-----+=========...
            // This may be the low side of a flat bottom or top border, so lowNbor or highNbor 
            // may be in the middle of the border.
            Point lowNborIntersect = ScanLineIntersectSide(vertexEvent.Site, lowNborSide);
            Point highNborIntersect = ScanLineIntersectSide(vertexEvent.Site, highNborSide);
            bool lowNborEndpointIsOverlapped = IntersectionAtSideIsInsideAnotherObstacle(lowNborSide,
                        vertexEvent.Obstacle /*obstacleToIgnore*/, lowNborIntersect);

            if (!lowNborEndpointIsOverlapped && (lowNborSide == lowOverlapSide)) {
                // Nothing is overlapped so create one segment.
                AddSegment(lowNborIntersect, highNborIntersect, obstacle, lowNborSide, highNborSide, vertexEvent,
                        ScanSegment.NormalWeight);
                return;
            }

            // Here are the different interval combinations for overlapped cases.
            //  - non-overlapped, overlapped:  1|  |2  v  |3
            //                            ...---+  +------+===...
            //  - non-overlapped, overlapped, non-overlapped:  1|  |2  v  2|  |3
            //                                                ==+  +-------+  +--...
            //  - overlapped:   |1  2|  v  |3  ...1|
            //            ...---+====+-----+===...-+
            //  - overlapped, non-overlapped:  |1  2|  v  1|  |3
            //                           ...---+====+------+  +---...
            // We will not start overlapped and then go to non-overlapped and back to overlapped,
            // because we would have found the overlap-ending/beginning sides as nearer neighbors.

            // Get the other intersections we'll want.
            Point highOverlapIntersect = (highOverlapSide == highNborSide) ? highNborIntersect
                                            : ScanLineIntersectSide(vertexEvent.Site, highOverlapSide);
            Point lowOverlapIntersect = (lowOverlapSide == lowNborSide) ? lowNborIntersect
                                            : ScanLineIntersectSide(vertexEvent.Site, lowOverlapSide);

            // Create the segments.
            if (!lowNborEndpointIsOverlapped) {
                // First interval is non-overlapped; there is a second overlapped interval, and may be a 
                // third non-overlapping interval if another obstacle surrounded this vertex.
                AddSegment(lowNborIntersect, lowOverlapIntersect, obstacle, lowNborSide, lowOverlapSide, vertexEvent,
                        ScanSegment.NormalWeight);
                AddSegment(lowOverlapIntersect, highOverlapIntersect, obstacle, lowOverlapSide, highOverlapSide, vertexEvent,
                        ScanSegment.OverlappedWeight);
                if (highOverlapSide != highNborSide) {
                    AddSegment(highOverlapIntersect, highNborIntersect, obstacle, highOverlapSide, highNborSide, vertexEvent,
                        ScanSegment.NormalWeight);
                }
            }
            else {
                // Starts off overlapped so ignore lowOverlapSide.
                AddSegment(lowNborIntersect, highOverlapIntersect, obstacle, lowNborSide, highOverlapSide, vertexEvent, 
                        ScanSegment.OverlappedWeight);
                if (highOverlapSide != highNborSide) {
                    AddSegment(highOverlapIntersect, highNborIntersect, obstacle, highOverlapSide, highNborSide, vertexEvent,
                        ScanSegment.NormalWeight);
                }
            }
        }

        void CreateScanSegments(Obstacle obstacle, NeighborSides neighborSides, BasicVertexEvent vertexEvent) {
            this.CreateScanSegments(obstacle, (HighObstacleSide)neighborSides.LowNeighbor.Item
                                    , (null == neighborSides.LowOverlapEnd) ? null : neighborSides.LowOverlapEnd.Item
                                    , (null == neighborSides.HighOverlapEnd) ? null : neighborSides.HighOverlapEnd.Item
                                    , (LowObstacleSide)neighborSides.HighNeighbor.Item, vertexEvent);
        }

        private void CreateScanSegmentFromLowSide(RBNode<BasicObstacleSide> lowSideNode, BasicVertexEvent vertexEvent) {
            // Create one or more segments from low to high using the neighbors of the LowObstacleSide.
            this.CreateScanSegments(lowSideNode.Item.Obstacle, this.LowNeighborSides, vertexEvent);
        }

        void CreateScanSegmentFromHighSide(RBNode<BasicObstacleSide> highSideNode, BasicVertexEvent vertexEvent) {
            // Create one or more segments from low to high using the neighbors of the HighObstacleSide.
            this.CreateScanSegments(highSideNode.Item.Obstacle, this.HighNeighborSides, vertexEvent);
        }

        protected override void ProcessVertexEvent(RBNode<BasicObstacleSide> lowSideNode,
                    RBNode<BasicObstacleSide> highSideNode, BasicVertexEvent vertexEvent) {
            // Create the scan segment from the low side.
            CreateScanSegmentFromLowSide(lowSideNode, vertexEvent);

            // If the low segment covered up to our high neighbor, we're done.  Otherwise, there were overlaps
            // inside a flat boundary and now we need to come in from the high side.  In this case there's a chance
            // that we're redoing a single subsegment in the event of two obstacles' outside edges crossing
            // the middle of a flat boundary of the event obstacle, but that should be sufficiently rare that
            // we don't need to optimize it away as the segments will be merged by ScanSegmentTree.MergeSegments.
            // TODOgroup TODOperf: currentGroupBoundaryCrossingMap still has the Low-side stuff in it but it shouldn't
            // matter much - profile to see how much time GetOrderedIndexBetween takes.
            if (LowNeighborSides.HighNeighbor.Item != HighNeighborSides.HighNeighbor.Item) {
                CreateScanSegmentFromHighSide(highSideNode, vertexEvent);
            }
        }
    }
}
