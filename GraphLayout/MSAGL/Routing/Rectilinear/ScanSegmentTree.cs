//
// ScanSegmentTree.cs
// MSAGL class for visibility scan segment tree for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear {
    using DebugHelpers;

    internal class ScanSegmentTree : IComparer<ScanSegment> {
        internal ScanDirection ScanDirection { get; private set; }
        private readonly RbTree<ScanSegment> segmentTree;

        // Temporary variables for lookup.
        private readonly ScanSegment lookupSegment = new ScanSegment(new Point(0, 0), new Point(0, 1));  // dummy values avoid AF; will be overwritten
        readonly Func<ScanSegment, bool> findIntersectorPred;
        readonly Func<ScanSegment, bool> findPointPred;

        internal ScanSegmentTree(ScanDirection scanDir) {
            ScanDirection = scanDir;
            this.segmentTree = new RbTree<ScanSegment>(this);
            this.findIntersectorPred = new Func<ScanSegment, bool>(this.CompareIntersector);
            this.findPointPred = new Func<ScanSegment, bool>(this.CompareToPoint);
        }

        internal IEnumerable<ScanSegment> Segments { get { return this.segmentTree; } }

        // If the seg is already in the tree it returns that instance, else it inserts the new
        // seg and returns that.
        internal RBNode<ScanSegment> InsertUnique(ScanSegment seg) {
            // RBTree's internal operations on insert/remove etc. mean the node can't cache the
            // RBNode returned by insert(); instead we must do find() on each call.  But we can
            // use the returned node to get predecessor/successor.
            AssertValidSegmentForInsertion(seg);
            var node = this.segmentTree.Find(seg);
            if (null != node) {
                Debug.Assert(seg.IsOverlapped == node.Item.IsOverlapped, "Existing node found with different isOverlapped");
                return node;
            }
            return this.segmentTree.Insert(seg);
        }

        [Conditional("TEST_MSAGL")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        void AssertValidSegmentForInsertion(ScanSegment seg) {
            Debug.Assert((seg.End.X >= seg.Start.X) && (seg.End.Y >= seg.Start.Y), "Reversed direction in ScanSegment");
            Debug.Assert(ScanDirection.IsFlat(seg.Start, seg.End), "non-flat segment cannot be inserted");
        }

        internal void Remove(ScanSegment seg) {
            Debug.Assert(seg.IsVertical == ScanDirection.IsVertical, "seg.IsVertical != ScanDirection.IsVertical");
            this.segmentTree.Remove(seg);
        }

        internal ScanSegment Find(Point start, Point end) {
            Debug.Assert(PointComparer.Equal(start, end) || !ScanDirection.IsPerpendicular(start, end)
                        , "perpendicular segment passed");
            this.lookupSegment.Update(start, end);
            RBNode<ScanSegment> node = this.segmentTree.Find(this.lookupSegment);
            if ((null != node) && PointComparer.Equal(node.Item.End, end)) {
                return node.Item;
            }
            return null;
        }

        // Find the lowest perpendicular scanseg that intersects the segment endpoints.
        internal ScanSegment FindLowestIntersector(Point start, Point end) {
            var node = FindLowestIntersectorNode(start, end);
            return (null != node) ? node.Item : null;
        }

        internal RBNode<ScanSegment> FindLowestIntersectorNode(Point start, Point end) {
            Debug.Assert(ScanDirection.IsPerpendicular(start, end), "non-perpendicular segment passed");

            // Find the last segment that starts at or before 'start'.
            this.lookupSegment.Update(start, start);
            RBNode<ScanSegment> node = this.segmentTree.FindLast(this.findIntersectorPred);

            // We have a segment that intersects start/end, or one that ends before 'start' and thus we
            // must iterate to find the lowest bisector.  TODOperf: see how much that iteration costs us
            // (here and Highest); consider a BSP tree or interval tree (maybe 2-d RBTree for updatability).
            if (PointComparer.Equal(start, end)) {
                if ((null != node) && (ScanDirection.Compare(node.Item.End, start) < 0)) {
                    node = null;
                }
            }
            else {
                this.lookupSegment.Update(start, end);
                while ((null != node) && !node.Item.IntersectsSegment(this.lookupSegment)) {
                    // If the node segment starts after 'end', no intersection was found.
                    if (ScanDirection.Compare(node.Item.Start, end) > 0) {
                        return null;
                    }
                    node = this.segmentTree.Next(node);
                }
            }
            return node;
        }

        // Find the highest perpendicular scanseg that intersects the segment endpoints.
        internal ScanSegment FindHighestIntersector(Point start, Point end) {
            Debug.Assert(ScanDirection.IsPerpendicular(start, end), "non-perpendicular segment passed");

            // Find the last segment that starts at or before 'end'.
            this.lookupSegment.Update(end, end);
            RBNode<ScanSegment> node = this.segmentTree.FindLast(this.findIntersectorPred);

            // Now we either have a segment that intersects start/end, or one that ends before 
            // 'end' and need to iterate to find the highest bisector.
            if (PointComparer.Equal(start, end)) {
                if ((null != node) && (ScanDirection.Compare(node.Item.End, start) < 0)) {
                    node = null;
                }
            }
            else {
                this.lookupSegment.Update(start, end);
                while ((null != node) && !node.Item.IntersectsSegment(this.lookupSegment)) {
                    // If the node segment ends before 'start', no intersection was found.
                    if (ScanDirection.Compare(node.Item.End, start) < 0) {
                        return null;
                    }
                    node = this.segmentTree.Previous(node);
                }
            }
            return (null != node) ? node.Item : null;
        }

        bool CompareIntersector(ScanSegment seg) {
            // We're looking for the last segment that starts before LookupSegment.Start.
            return (ScanDirection.Compare(seg.Start, this.lookupSegment.Start) <= 0);
        }

        internal ScanSegment FindSegmentContainingPoint(Point location, bool allowUnfound) {
            return FindSegmentOverlappingPoints(location, location, allowUnfound);
        }
        internal ScanSegment FindSegmentOverlappingPoints(Point start, Point end, bool allowUnfound) {
            this.lookupSegment.Update(start, end);
            RBNode<ScanSegment> node = this.segmentTree.FindFirst(this.findPointPred);

            // If we had any segments in the tree that end after 'start', node has the first one.
            // Now we need to that it starts before 'end'.  ScanSegment.CompareToPointPositionFullLength
            // asserts the point is on the segment which we don't want to require here, so
            // compare the endpoints directly.
            if (null != node) {
                ScanSegment seg = node.Item;
                if (ScanDirection.Compare(seg.Start, end) <= 0) {
                    return seg;
                }
            }

            // Not found.
            if (!allowUnfound) {
                Debug.Assert(false, "Could not find expected segment");
            }
            return null;
        }

        bool CompareToPoint(ScanSegment treeSeg) {
            // Test if treeSeg overlaps the LookupSegment.Start point.  We're using FindFirst,
            // so we'll just return false for everything that ends before the point and true for anything
            // that ends at or after it, then the caller will verify overlap.
            return (ScanDirection.Compare(treeSeg.End, this.lookupSegment.Start) >= 0);
        }

        RBNode<ScanSegment> MergeAndRemoveNextNode(ScanSegment currentSegment, RBNode<ScanSegment> nextSegNode) {
            // Merge at the ends only - if we're here, start will be the same or greater.
            if (-1 == ScanDirection.Compare(currentSegment.End, nextSegNode.Item.End)) {
                currentSegment.Update(currentSegment.Start, nextSegNode.Item.End);
            }

            // Removing the node can revise the tree's RBNodes internally so re-get the current segment.
            currentSegment.MergeGroupBoundaryCrossingList(nextSegNode.Item.GroupBoundaryPointAndCrossingsList);
            this.segmentTree.DeleteNodeInternal(nextSegNode);
            return this.segmentTree.Find(currentSegment);
        }

        internal void MergeSegments() {
            // As described in the doc, hintScanSegment handles all the non-overlapped non-reflection cases.
            DevTraceInfo(1, "{0} ScanSegmentTree MergeSegments, count = {1}"
                            , ScanDirection.IsHorizontal ? "Horizontal" : "Vertical", this.segmentTree.Count);
            if (this.segmentTree.Count < 2) {
                return;
            }
            RBNode<ScanSegment> currentSegNode = this.segmentTree.TreeMinimum();
            RBNode<ScanSegment> nextSegNode = this.segmentTree.Next(currentSegNode);
            for ( ; null != nextSegNode; nextSegNode = this.segmentTree.Next(currentSegNode)) {
                DevTraceInfo(2, "Current {0}  Next {1}", currentSegNode.Item, nextSegNode.Item);
                int cmp = ScanDirection.Compare(nextSegNode.Item.Start, currentSegNode.Item.End);
                switch (cmp) {
                    case 1:
                        // Next segment starts after the current one.
                        currentSegNode = nextSegNode;
                        break;
                    case 0:
                        if (nextSegNode.Item.IsOverlapped == currentSegNode.Item.IsOverlapped) {
                            // Overlapping is the same, so merge.  Because the ordering in the tree is that
                            // same-Start nodes are ordered by longest-End first, this will retain the tree ordering.
                            DevTraceInfo(2, "  (merged; start-at-end with same overlap)");
                            currentSegNode = MergeAndRemoveNextNode(currentSegNode.Item, nextSegNode);
                        }
                        else {
                            // Touching start/end with differing IsOverlapped so they need a connecting vertex.
                            DevTraceInfo(2, "  (marked with NeedFinalOverlapVertex)");
                            currentSegNode.Item.NeedEndOverlapVertex = true;
                            nextSegNode.Item.NeedStartOverlapVertex = true;
                            currentSegNode = nextSegNode;
                        }
                        break;
                    default:    // -1 == cmp
                        // nextSegNode.Item.Start is before currentSegNode.Item.End.
                        Debug.Assert((nextSegNode.Item.Start != currentSegNode.Item.Start)
                                     || (nextSegNode.Item.End < currentSegNode.Item.End)
                                     , "Identical segments are not allowed, and longer ones must come first");

                        // Because longer segments are ordered before shorter ones at the same start position,
                        // nextSegNode.Item must be a duplicate segment or is partially or totally overlapped.
                        // In the case of reflection lookahead segments, the side-intersection calculated from
                        // horizontal vs. vertical directions may be slightly different along the parallel
                        // coordinate from an overlapped segment, so let non-overlapped win that disagreement.
                        if (currentSegNode.Item.IsOverlapped != nextSegNode.Item.IsOverlapped) {
                            Debug.Assert(ApproximateComparer.CloseIntersections(currentSegNode.Item.End, nextSegNode.Item.Start)
                                        , "Segments share a span with different IsOverlapped");
                            if (currentSegNode.Item.IsOverlapped) {
                                // If the Starts are different, then currentSegNode is the only item at its
                                // start, so we don't need to re-insert.  Otherwise, we need to remove it and
                                // re-find nextSegNode's side.
                                if (currentSegNode.Item.Start == nextSegNode.Item.Start) {
                                    // currentSegNode is a tiny overlapped segment between two non-overlapped segments (so
                                    // we'll have another merge later, when we hit the other non-overlapped segment).
                                    // Notice reversed params.  TestNote: No longer have repro with the change to convex hulls;
                                    // this may no longer happen since overlapped edges will now always be inside rectangular
                                    // obstacles so there are no angled-side calculations.
                                    currentSegNode = MergeAndRemoveNextNode(nextSegNode.Item, currentSegNode);
                                    DevTraceInfo(2, "  (identical starts so discarding isOverlapped currentSegNode)");
                                }
                                else {
                                    currentSegNode.Item.Update(currentSegNode.Item.Start, nextSegNode.Item.Start);
                                    DevTraceInfo(2, "  (trimming isOverlapped currentSegNode to {0})", currentSegNode.Item);
                                    currentSegNode = nextSegNode;
                                }
                            }
                            else {
                                if (currentSegNode.Item.End == nextSegNode.Item.End) {
                                    // nextSegNode is a tiny non-overlapped segment between two overlapped segments (so
                                    // we'll have another merge later, when we hit the other non-overlapped segment).
                                    // TestNote: No longer have repro with the change to convex hulls;
                                    // this may no longer happen since overlapped edges will now always be inside rectangular
                                    // obstacles so there are no angled-side calculations.
                                    currentSegNode = MergeAndRemoveNextNode(currentSegNode.Item, nextSegNode);
                                    DevTraceInfo(2, "  (identical ends so discarding isOverlapped currentSegNode)");
                                }
                                else {
                                    // Remove nextSegNode, increment its start to be after currentSegment, re-insert nextSegNode, and
                                    // re-find currentSegNode (there may be more segments between nextSegment.Start and currentSegment.End).
                                    ScanSegment nextSegment = nextSegNode.Item;
                                    ScanSegment currentSegment = currentSegNode.Item;
                                    this.segmentTree.DeleteNodeInternal(nextSegNode);
                                    nextSegment.Update(currentSegment.End, nextSegment.End);
                                    this.segmentTree.Insert(nextSegment);
                                    nextSegment.TrimGroupBoundaryCrossingList();
                                    DevTraceInfo(2, "  (trimming isOverlapped nextSegNode to {0})", nextSegment);
                                    currentSegNode = this.segmentTree.Find(currentSegment);
                                }
                            }
                            break;
                        }

                        // Overlaps match so do a normal merge operation.
                        DevTraceInfo(2, "  (merged; start-before-end)");
                        currentSegNode = MergeAndRemoveNextNode(currentSegNode.Item, nextSegNode);
                        break;
                } // endswitch
            } // endfor

#if DEVTRACE
            DevTraceInfo(1, "{0} ScanSegmentTree after MergeSegments, count = {1}"
                            , ScanDirection.IsHorizontal ? "Horizontal" : "Vertical", this.segmentTree.Count);
            if (this.scanSegmentVerify.IsLevel(1)) {
                DevTraceInfo(1, "{0} ScanSegmentTree Consistency Check"
                            , ScanDirection.IsHorizontal ? "Horizontal" : "Vertical");
                bool retval = true;
                RBNode<ScanSegment> prevSegNode = this.segmentTree.TreeMinimum();
                currentSegNode = this.segmentTree.Next(prevSegNode);
                while (null != currentSegNode) {
                    DevTraceInfo(4, currentSegNode.Item.ToString());
                    // We should only have end-to-end touching, and that only if differing IsOverlapped.
                    if (   (-1 != Compare(prevSegNode.Item, currentSegNode.Item))
                        || (1 != ScanDirection.Compare(currentSegNode.Item.Start, prevSegNode.Item.Start)
                        || ((0 == ScanDirection.Compare(currentSegNode.Item.Start, prevSegNode.Item.End))
                            && (currentSegNode.Item.IsOverlapped == prevSegNode.Item.IsOverlapped)))) {
                        this.scanSegmentTrace.WriteError(0, "Segments are not strictly increasing:");
                        this.scanSegmentTrace.WriteFollowup(0, prevSegNode.Item.ToString());
                        this.scanSegmentTrace.WriteFollowup(0, currentSegNode.Item.ToString());
                        retval = false;
                    }
                    prevSegNode = currentSegNode;
                    currentSegNode = this.segmentTree.Next(currentSegNode);
                }
                Debug.Assert(retval, "ScanSegments are not strictly increasing");
            }
#endif // DEVTRACE
        }

        #region IComparer<ScanSegment>
        /// <summary>
        /// For ordering the line segments inserted by the ScanLine. Assuming vertical sweep (sweeping up from
        /// bottom, scanning horizontally) then order ScanSegments first by lowest Y coord, then by lowest X coord.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public int Compare(ScanSegment first, ScanSegment second) {
            if (first == second)
                return 0;
            if (first == null)
                return -1;
            if (second == null)
                return 1;

            // This orders on both axes.
            int cmp = ScanDirection.Compare(first.Start, second.Start);
            if (0 == cmp) {
                // Longer segments come first, to make overlap removal easier.
                cmp = -ScanDirection.Compare(first.End, second.End);
            }
            return cmp;
        }
        #endregion // IComparer<ScanSegment>

        #region DevTrace
#if DEVTRACE
        readonly DevTrace scanSegmentTrace = new DevTrace("Rectilinear_ScanSegmentTrace", "ScanSegments");
        readonly DevTrace scanSegmentVerify = new DevTrace("Rectilinear_ScanSegmentVerify");
#endif // DEVTRACE

        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        void DevTraceInfo(int verboseLevel, string format, params object[] args) {
#if DEVTRACE
            this.scanSegmentTrace.WriteLineIf(DevTrace.Level.Info, verboseLevel, format, args);
#endif // DEVTRACE
        }

        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        internal void DevTraceVerifyVisibility() {
#if DEVTRACE
            if (this.scanSegmentVerify.IsLevel(1)) {
                DevTraceInfo(1, "Beginning {0} ScanSegmentTree VisibilityVertex Check..."
                            , ScanDirection.IsHorizontal ? "Horizontal" : "Vertical");
                bool retval = true;
                for (RBNode<ScanSegment> segNode = this.segmentTree.TreeMinimum(); null != segNode; segNode = this.segmentTree.Next(segNode)) {
                    if (null == segNode.Item.LowestVisibilityVertex) {
                        this.scanSegmentTrace.WriteError(0, "Segment has no VisibilityVertex: {0}", segNode.Item.ToString());
                        retval = false;
                    }
                }
                DevTraceInfo(1, "{0} ScanSegmentTree VisibilityVertex Check complete: status {1}"
                            , ScanDirection.IsHorizontal ? "Horizontal" : "Vertical", retval ? "pass" : "fail");
                Debug.Assert(retval, "One or more ScanSegments do not have a VisibilityVertex");
            }
#endif // DEVTRACE
        }
        #endregion // DevTrace
    }
}
