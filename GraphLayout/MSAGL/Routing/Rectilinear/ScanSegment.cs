//
// ScanSegment.cs
// MSAGL class for visibility scan segments for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
    internal class ScanSegment : SegmentBase {
        // This is a single segment added by the ScanLine.
        internal PointAndCrossingsList GroupBoundaryPointAndCrossingsList;
        private Point endPoint;
        private Point startPoint;

        internal const double NormalWeight = VisibilityEdge.DefaultWeight;
        internal const double ReflectionWeight = 5;
        internal const double OverlappedWeight = 500;
        internal double Weight { get; private set; }

        // For sparse visibility graph.
        internal ScanSegment NextSegment { get; set; }

        internal ScanSegment(Point start, Point end) : this(start, end, NormalWeight, gbcList:null) {
        }

        internal ScanSegment(Point start, Point end, double weight, PointAndCrossingsList gbcList) {
            Update(start, end);
            Weight = weight;
            GroupBoundaryPointAndCrossingsList = gbcList;
        }

        internal override Point Start {
            get { return startPoint; }
        }

        internal override Point End {
            get { return endPoint; }
        }

        internal bool IsVertical {
            get { return IsVerticalSegment(Start, End); }
        }

        internal ScanDirection ScanDirection {
            get { return IsVertical ? ScanDirection.VerticalInstance : ScanDirection.HorizontalInstance; }
        }

        // For fast intersection calculation and ScanSegment splicing.
        internal VisibilityVertex LowestVisibilityVertex { get; private set; }
        internal VisibilityVertex HighestVisibilityVertex { get; private set; }

        // For overlaps, we will need to create a VisibilityVertex at the junction of overlapped/nonoverlapped
        // segments, but we don't want to create this for non-overlapped situations.
        internal bool IsOverlapped {
            get { return OverlappedWeight == this.Weight; }
        }
        internal bool IsReflection {
            get { return ReflectionWeight == this.Weight; }
        }

        internal bool NeedStartOverlapVertex { get; set; }
        internal bool NeedEndOverlapVertex { get; set; }

        internal static bool IsVerticalSegment(Point start, Point end) {
            return start.X == end.X;
        }

        // For group boundary crossings.

        internal void MergeGroupBoundaryCrossingList(PointAndCrossingsList other) {
            if (null != other) {
                if (null == GroupBoundaryPointAndCrossingsList) {
                    GroupBoundaryPointAndCrossingsList = new PointAndCrossingsList();
                }
                GroupBoundaryPointAndCrossingsList.MergeFrom(other);
            }
        }

        internal void TrimGroupBoundaryCrossingList() {
            if (null != GroupBoundaryPointAndCrossingsList) {
                GroupBoundaryPointAndCrossingsList.Trim(Start, End);
            }
        }

        // ctor

        internal void Update(Point start, Point end) {
            Debug.Assert(PointComparer.Equal(start, end)
                    || StaticGraphUtility.IsAscending(PointComparer.GetPureDirection(start, end))
                    , "non-ascending segment");
            startPoint = start;
            endPoint = end;
        }

        private void SetInitialVisibilityVertex(VisibilityVertex newVertex) {
            LowestVisibilityVertex = newVertex;
            HighestVisibilityVertex = newVertex;
        }

        // Append a vertex before LowestVisibilityVertex or after HighestVisibilityVertex.
        internal void AppendVisibilityVertex(VisibilityGraph vg, VisibilityVertex newVertex) {
            Debug.Assert(null != newVertex, "newVertex must not be null");
            Debug.Assert((null == LowestVisibilityVertex) == (null == HighestVisibilityVertex), "Mismatched null Lowest/HighestVisibilityVertex");
            Debug.Assert(StaticGraphUtility.PointIsOnSegment(this, newVertex.Point), "newVertex is out of segment range");
            if (null == HighestVisibilityVertex) {
                if (!AddGroupCrossingsBeforeHighestVisibilityVertex(vg, newVertex)) {
                    SetInitialVisibilityVertex(newVertex);
                }
            } else {
                // In the event of overlaps where ScanSegments share a Start/End at a border, SegmentIntersector
                // may be appending the same Vertex twice.  If that point is on the border of a group,
                // then we may have just added the border-crossing edge as well.
                if (PointComparer.IsPureLower(newVertex.Point, HighestVisibilityVertex.Point)) {
                    Debug.Assert(null != vg.FindEdge(newVertex.Point, HighestVisibilityVertex.Point)
                            , "unexpected low/middle insertion to ScanSegment");
                    return;
                }

                // Add the new edge.  This will always be in the ascending direction.
                if (!AddGroupCrossingsBeforeHighestVisibilityVertex(vg, newVertex)) {
                    AppendHighestVisibilityVertex(newVertex);
                }
            }
        }

        private VisibilityEdge AddVisibilityEdge( VisibilityVertex source, VisibilityVertex target) {
            Debug.Assert(source.Point != target.Point, "Self-edges are not allowed");
            Debug.Assert(PointComparer.IsPureLower(source.Point, target.Point), "Impure or reversed direction encountered");

            // Make sure we aren't adding two edges in the same direction to the same vertex.
            Debug.Assert(null == StaticGraphUtility.FindAdjacentVertex(source, StaticGraphUtility.EdgeDirection(source, target))
                    , "Duplicate outEdge from Source vertex");
            Debug.Assert(null == StaticGraphUtility.FindAdjacentVertex(target, StaticGraphUtility.EdgeDirection(target, source))
                    , "Duplicate inEdge to Target vertex");
            var edge = new VisibilityEdge(source, target, this.Weight);
            VisibilityGraph.AddEdge(edge);
            return edge;
        }

        private void AppendHighestVisibilityVertex( VisibilityVertex newVertex) {
            if (!PointComparer.Equal(HighestVisibilityVertex.Point, newVertex.Point)) {
                AddVisibilityEdge( HighestVisibilityVertex, newVertex);
                HighestVisibilityVertex = newVertex;
            }
        }

        private void LoadStartOverlapVertexIfNeeded(VisibilityGraph vg) {
            // For adjacent segments with different IsOverlapped, we need a vertex that
            // joins the two so a path may be run.  This is paired with the other segment's
            // LoadEndOverlapVertexIfNeeded.
            if (NeedStartOverlapVertex) {
                VisibilityVertex vertex = vg.FindVertex(Start);
                AppendVisibilityVertex(vg, vertex ?? vg.AddVertex(Start));
            }
        }

        private void LoadEndOverlapVertexIfNeeded(VisibilityGraph vg) {
            // See comments in LoadStartOverlapVertexIfNeeded.
            if (NeedEndOverlapVertex) {
                VisibilityVertex vertex = vg.FindVertex(End);
                AppendVisibilityVertex(vg, vertex ?? vg.AddVertex(End));
            }
        }

        internal void OnSegmentIntersectorBegin(VisibilityGraph vg) {
            // If we process any group crossings, they'll have created the first point.
            if (!AppendGroupCrossingsThroughPoint(vg, Start)) {
                LoadStartOverlapVertexIfNeeded(vg);
            }
        }

        internal void OnSegmentIntersectorEnd(VisibilityGraph vg) {
            AppendGroupCrossingsThroughPoint(vg, End);
            GroupBoundaryPointAndCrossingsList = null;
            if ((null == HighestVisibilityVertex) || (PointComparer.IsPureLower(HighestVisibilityVertex.Point, End))) {
                LoadEndOverlapVertexIfNeeded(vg);
            }
        }

        // If we have collinear segments, then we may be able to just update the previous one
        // instead of growing the ScanSegmentTree.
        //  - For multiple collinear OpenVertexEvents, neighbors to the high side have not yet
        //    been seen, so a segment is created that spans the lowest and highest neighbors.
        //    A subsequent collinear OpenVertexEvent will be to the high side and will add a
        //    subsegment of that segment, so we subsume it into LastAddedSegment.
        //  - For multiple collinear CloseVertexEvents, closing neighbors to the high side are
        //    still open, so a segment is created from the lowest neighbor to the next-highest
        //    collinear obstacle to be closed.  When that next-highest CloseVertexEvent is
        //    encountered, it will extend LastAddedSegment.
        //  - For multiple collinear mixed Open and Close events, we'll do all Opens first,
        //    followed by all closes (per EventQueue opening), so we may add multiple discrete
        //    segments, which ScanSegmentTree will merge.
        internal static bool Subsume(ref ScanSegment seg, Point newStart, Point newEnd,
                double weight, PointAndCrossingsList gbcList,
                ScanDirection scanDir, ScanSegmentTree tree, out bool extendStart,
                out bool extendEnd) {
            // Initialize these to the non-subsumed state; the endpoints were extended (or on a
            // different line).
            extendStart = true;
            extendEnd = true;
            if (null == seg) {
                return false;
            }

            // If they don't overlap (including touching at an endpoint), we don't subsume.
            if (!StaticGraphUtility.IntervalsOverlap(seg.Start, seg.End, newStart, newEnd)) {
                return false;
            }

            // If the overlapped-ness isn't the same, we don't subsume.  ScanSegmentTree::MergeSegments
            // will mark that the low-to-high direction needs a VisibilityVertex to link the two segments.
            // These may differ by more than Curve.DistanceEpsilon in the case of reflection lookahead
            // segments collinear with vertex-derived segments, so have a looser tolerance here and we'll
            // adjust the segments in ScanSegmentTree.MergeSegments.
            if (seg.Weight != weight) {
                if ((seg.Start == newStart) && (seg.End == newEnd)) {
                    // This is probably because of a rounding difference by one DistanceEpsilon reporting being
                    // inside an obstacle vs. the scanline intersection calculation side-ordering.
                    // Test is RectilinearFileTests.Overlap_Rounding_Vertex_Intersects_Side.
                    seg.Weight = Math.Min(seg.Weight, weight);
                    return true;
                }
                
                // In the case of groups, we go through the group boundary; this may coincide with a
                // reflection segment. RectilinearFileTests.ReflectionSubsumedBySegmentExitingGroup.
                Debug.Assert((seg.Weight == OverlappedWeight) == (weight == OverlappedWeight) ||
                                ApproximateComparer.CloseIntersections(seg.End, newStart) ||
                                ApproximateComparer.CloseIntersections(seg.Start, newEnd)
                        , "non-equal overlap-mismatched ScanSegments overlap by more than just Start/End");
                return false;
            }

            // Subsume the input segment.  Return whether the start/end points were extended (newStart
            // is before this.Start, or newEnd is after this.End), so the caller can generate reflections
            // and so we can merge group border crossings.
            extendStart = (-1 == scanDir.CompareScanCoord(newStart, seg.Start));
            extendEnd = (1 == scanDir.CompareScanCoord(newEnd, seg.End));
            if (extendStart || extendEnd) {
                // We order by start and end so need to replace this in the tree regardless of which end changes.
                tree.Remove(seg);
                seg.startPoint = scanDir.Min(seg.Start, newStart);
                seg.endPoint = scanDir.Max(seg.End, newEnd);
                seg = tree.InsertUnique(seg).Item;
                seg.MergeGroupBoundaryCrossingList(gbcList);
            }
            return true;
        }

        internal bool IntersectsSegment(ScanSegment seg) {
            return StaticGraphUtility.SegmentsIntersect(this, seg);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return "[" + Start + " -> " + End + (IsOverlapped ? " olap" : " free") + "]";
        }

        #region SparseVisibilityGraphGenerator utilities
        
        internal bool ContainsPoint(Point test) {
            // This may be off the line so do not use GetPureDirections. 
            return PointComparer.Equal(this.Start, test)
                    || PointComparer.Equal(this.End, test)
                    || (PointComparer.GetDirections(this.Start, test) == PointComparer.GetDirections(test, this.End));
        }

        private Set<double> sparsePerpendicularCoords;

        internal bool HasSparsePerpendicularCoords {
            get {
                return (this.sparsePerpendicularCoords == null) ? false : (this.sparsePerpendicularCoords.Count > 0);
            }
        }

        private Point CreatePointFromPerpCoord(double perpCoord) {
            return this.IsVertical ? new Point(this.Start.X, perpCoord) : new Point(perpCoord, this.Start.Y);
        }

        internal void AddSparseVertexCoord(double perpCoord) {
            Debug.Assert(this.ContainsPoint(this.CreatePointFromPerpCoord(perpCoord)), "vertexLocation is not on Segment");
            if (this.sparsePerpendicularCoords == null) {
                this.sparsePerpendicularCoords = new Set<double>();
            }
            this.sparsePerpendicularCoords.Insert(perpCoord);
        }

        internal bool AddSparseEndpoint(double coord) {
            // This is called after AddSparseVertexCoord so this.sparsePerpendicularCoords is already instantiated.
            if (!this.sparsePerpendicularCoords.Contains(coord)) {
                this.sparsePerpendicularCoords.Insert(coord);
                return true;
            }
            return false;
        }

        internal void CreateSparseVerticesAndEdges(VisibilityGraph vg) {
            if (this.sparsePerpendicularCoords == null) {
                return;
            }

            AppendGroupCrossingsThroughPoint(vg, Start);
            foreach (var perpCoord in this.sparsePerpendicularCoords.OrderBy(d => d)) {
                var vertexLocation = this.CreatePointFromPerpCoord(perpCoord);
                Debug.Assert(this.ContainsPoint(vertexLocation), "vertexLocation is not on Segment");
                this.AppendVisibilityVertex(vg, vg.FindVertex(vertexLocation) ?? vg.AddVertex(vertexLocation));
            }
            AppendGroupCrossingsThroughPoint(vg, End);
            GroupBoundaryPointAndCrossingsList = null;

            this.sparsePerpendicularCoords.Clear();
            this.sparsePerpendicularCoords = null;
        }

        internal bool HasVisibility() {
            // Skip this only if it has no visibility vertex.
            return (null != this.LowestVisibilityVertex);
        }

        #endregion // SparseVisibilityGraphGenerator utilities

        #region Group utilities

        private bool AddGroupCrossingsBeforeHighestVisibilityVertex(VisibilityGraph vg, VisibilityVertex newVertex) {
            if (AppendGroupCrossingsThroughPoint(vg, newVertex.Point)) {
                // We may have added an interior vertex that is just higher than newVertex.
                if (PointComparer.IsPureLower(HighestVisibilityVertex.Point, newVertex.Point)) {
                    AddVisibilityEdge(HighestVisibilityVertex, newVertex);
                    HighestVisibilityVertex = newVertex;
                }
                return true;
            }
            return false;
        }

        private bool AppendGroupCrossingsThroughPoint(VisibilityGraph vg, Point lastPoint) {
            if (null == GroupBoundaryPointAndCrossingsList) {
                return false;
            }

            bool found = false;
            while (GroupBoundaryPointAndCrossingsList.CurrentIsBeforeOrAt(lastPoint)) {
                // We will only create crossing Edges that the segment actually crosses, not those it ends before crossing.
                // For those terminal crossings, the adjacent segment creates the interior vertex and crossing edge.
                PointAndCrossings pac = GroupBoundaryPointAndCrossingsList.Pop();
                GroupBoundaryCrossing[] lowDirCrossings = null;
                GroupBoundaryCrossing[] highDirCrossings = null;
                if (PointComparer.Compare(pac.Location, Start) > 0) {
                    lowDirCrossings = PointAndCrossingsList.ToCrossingArray(pac.Crossings,
                            ScanDirection.OppositeDirection);
                }
                if (PointComparer.Compare(pac.Location, End) < 0) {
                    highDirCrossings = PointAndCrossingsList.ToCrossingArray(pac.Crossings, ScanDirection.Direction);
                }

                found = true;
                VisibilityVertex crossingVertex = vg.FindVertex(pac.Location) ?? vg.AddVertex(pac.Location);

                if ((null != lowDirCrossings) || (null != highDirCrossings)) {
                    AddLowCrossings(vg, crossingVertex, lowDirCrossings);
                    AddHighCrossings(vg, crossingVertex, highDirCrossings);
                } else {
                    // This is at this.Start with only lower-direction toward group interior(s), or at this.End with only 
                    // higher-direction toward group interior(s).  Therefore an adjacent ScanSegment will create the crossing
                    // edge, so create the crossing vertex here and we'll link to it.
                    if (null == LowestVisibilityVertex) {
                        SetInitialVisibilityVertex(crossingVertex);
                    } else {
                        Debug.Assert(PointComparer.Equal(End, crossingVertex.Point), "Expected this.End crossingVertex");
                        AppendHighestVisibilityVertex(crossingVertex);
                    }
                }
            }
            return found;
        }

        private static VisibilityVertex GetCrossingInteriorVertex(VisibilityGraph vg, VisibilityVertex crossingVertex,
                GroupBoundaryCrossing crossing) {
            Point interiorPoint = crossing.GetInteriorVertexPoint(crossingVertex.Point);
            return vg.FindVertex(interiorPoint) ?? vg.AddVertex(interiorPoint);
        }

        private void AddCrossingEdge(VisibilityGraph vg, VisibilityVertex lowVertex, VisibilityVertex highVertex, GroupBoundaryCrossing[] crossings) {
            VisibilityEdge edge = null;
            if (null != HighestVisibilityVertex) {
                // We may have a case where point xx.xxxxx8 has added an ascending-direction crossing, and now we're on
                // xx.xxxxx9 adding a descending-direction crossing.  In that case there should already be a VisibilityEdge 
                // in the direction we want.
                if (PointComparer.Equal(this.HighestVisibilityVertex.Point, highVertex.Point)) {
                    edge = vg.FindEdge(lowVertex.Point, highVertex.Point);
                    Debug.Assert(edge != null, "Inconsistent forward-backward sequencing in HighVisibilityVertex");
                } else {
                    AppendHighestVisibilityVertex(lowVertex);
                }
            }
            if (edge == null) {
                edge = AddVisibilityEdge(lowVertex, highVertex);
            }

            var crossingsArray = crossings.Select(c => c.Group.InputShape).ToArray();
            var prevIsPassable = edge.IsPassable;
            if (prevIsPassable == null) {
                edge.IsPassable = delegate { return crossingsArray.Any(s => s.IsTransparent); };
            } else {
                // Because we don't have access to the previous delegate's internals, we have to chain.  Fortunately this
                // will never be more than two deep.  File Test: Groups_Forward_Backward_Between_Same_Vertices.
                edge.IsPassable = delegate { return crossingsArray.Any(s => s.IsTransparent) || prevIsPassable(); };
            }
            if (null == LowestVisibilityVertex) {
                SetInitialVisibilityVertex(lowVertex);
            }
            HighestVisibilityVertex = highVertex;
        }

        private void AddLowCrossings(VisibilityGraph vg, VisibilityVertex crossingVertex, GroupBoundaryCrossing[] crossings) {
            if (null != crossings) {
                VisibilityVertex interiorVertex = GetCrossingInteriorVertex(vg, crossingVertex, crossings[0]);
                this.AddCrossingEdge(vg, interiorVertex, crossingVertex, crossings); // low-to-high
            }
        }

        private void AddHighCrossings(VisibilityGraph vg, VisibilityVertex crossingVertex, GroupBoundaryCrossing[] crossings) {
            if (null != crossings) {
                VisibilityVertex interiorVertex = GetCrossingInteriorVertex(vg, crossingVertex, crossings[0]);
                this.AddCrossingEdge(vg, crossingVertex, interiorVertex, crossings); // low-to-high
            }
        }
        #endregion // Group utilities
    }
}