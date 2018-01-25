//
// SegmentIntersector.cs
// MSAGL class for intersecting Rectilinear Edge Routing ScanLine segments.
//
// Copyright Microsoft Corporation.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
    internal class SegmentIntersector : IComparer<SegmentIntersector.SegEvent>, IComparer<ScanSegment> {
        // The event types.  We sweep vertically, with a horizontal scanline, so the vertical
        // segments that are active and have X coords within the current vertical segment's
        // span all create intersections with it.  All events are ordered on Y coordinate then X.

        internal SegmentIntersector() {
            verticalSegmentsScanLine = new RbTree<ScanSegment>(this);
            findFirstPred = new Func<ScanSegment, bool>(IsVSegInHSegRange);
        }

        bool IsVSegInHSegRange(ScanSegment v) {
            return PointComparer.Compare(v.Start.X, findFirstHSeg.Start.X) >= 0;
        }

        // This creates the VisibilityVertex objects along the segments.
        internal VisibilityGraph Generate(IEnumerable<ScanSegment> hSegments, IEnumerable<ScanSegment> vSegments) {
            foreach (ScanSegment seg in vSegments) {
                eventList.Add(new SegEvent(SegEventType.VOpen, seg));
                eventList.Add(new SegEvent(SegEventType.VClose, seg));
            }
            foreach (ScanSegment seg in hSegments) {
                eventList.Add(new SegEvent(SegEventType.HOpen, seg));
            }
            if (0 == eventList.Count) {
                return null; // empty
            }
            eventList.Sort(this);

            // Note: We don't need any sentinels in the scanline here, because the lowest VOpen
            // events are loaded before the first HOpen is.

            // Process all events.
            visGraph = VisibilityGraphGenerator.NewVisibilityGraph();
            foreach (SegEvent evt in eventList) {
                switch (evt.EventType) {
                    case SegEventType.VOpen:
                        OnSegmentOpen(evt.Segment);
                        ScanInsert(evt.Segment);
                        break;
                    case SegEventType.VClose:
                        OnSegmentClose(evt.Segment);
                        ScanRemove(evt.Segment);
                        break;
                    case SegEventType.HOpen:
                        OnSegmentOpen(evt.Segment);
                        ScanIntersect(evt.Segment);
                        break;
                    default:
                        Debug.Assert(false, "Unknown SegEventType");
// ReSharper disable HeuristicUnreachableCode
                        break;
// ReSharper restore HeuristicUnreachableCode
                }
            } // endforeach
            return visGraph;
        }

        void OnSegmentOpen(ScanSegment seg) {
            seg.OnSegmentIntersectorBegin(visGraph);
        }

        void OnSegmentClose(ScanSegment seg) {
            seg.OnSegmentIntersectorEnd(visGraph);
            if (null == seg.LowestVisibilityVertex) {
                segmentsWithoutVisibility.Add(seg);
            }
        }

        // Scan segments with no visibility will usually be internal to an overlap clump, 
        // but may be in an external "corner" of intersecting sides for a small enough span
        // that no other segment crosses them.  In that case we don't need them and they 
        // would require extra handling later.
        internal void RemoveSegmentsWithNoVisibility(ScanSegmentTree horizontalScanSegments,
                                                     ScanSegmentTree verticalScanSegments) {
            foreach (ScanSegment seg in segmentsWithoutVisibility) {
                (seg.IsVertical ? verticalScanSegments : horizontalScanSegments).Remove(seg);
            }
        }

        #region Scanline utilities

        void ScanInsert(ScanSegment seg) {
            Debug.Assert(null == this.verticalSegmentsScanLine.Find(seg), "seg already exists in the rbtree");

            // RBTree's internal operations on insert/remove etc. mean the node can't cache the
            // RBNode returned by insert(); instead we must do find() on each call.  But we can
            // use the returned node to get predecessor/successor.
            verticalSegmentsScanLine.Insert(seg);
        }

        void ScanRemove(ScanSegment seg) {
            verticalSegmentsScanLine.Remove(seg);
        }

        void ScanIntersect(ScanSegment hSeg) {
            // Find the VSeg in the scanline with the lowest X-intersection with HSeg, then iterate
            // all VSegs in the scan line after that until we leave the HSeg range.
            // We only use FindFirstHSeg in this routine, to find the first satisfying node,
            // so we don't care that we leave leftovers in it.
            findFirstHSeg = hSeg;
            RBNode<ScanSegment> segNode = verticalSegmentsScanLine.FindFirst(findFirstPred);

            for (; null != segNode; segNode = verticalSegmentsScanLine.Next(segNode)) {
                ScanSegment vSeg = segNode.Item;
                if (1 == PointComparer.Compare(vSeg.Start.X, hSeg.End.X)) {
                    break; // Out of HSeg range
                }
                VisibilityVertex newVertex = visGraph.AddVertex(new Point(vSeg.Start.X, hSeg.Start.Y));

                // HSeg has just opened so if we are overlapped and newVertex already existed,
                // it was because we just closed a previous HSeg or VSeg and are now opening one
                // whose Start is the same as previous.  So we may be appending a vertex that
                // is already the *Seg.HighestVisibilityVertex, which will be a no-op.  Otherwise
                // this will add a (possibly Overlapped)VisibilityEdge in the *Seg direction.
                hSeg.AppendVisibilityVertex(visGraph, newVertex);
                vSeg.AppendVisibilityVertex(visGraph, newVertex);
            } // endforeach scanline VSeg in range

            OnSegmentClose(hSeg);
        }

        // end ScanIntersect()

        #endregion // Scanline utilities

        #region IComparer<SegEvent>

        /// <summary>
        /// For ordering events first by Y, then X, then by whether it's an H or V seg.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public int Compare(SegEvent first, SegEvent second) {
            if (first == second) {
                return 0;
            }
            if (first == null) {
                return -1;
            }
            if (second == null) {
                return 1;
            }

            // Unlike the ScanSegment-generating scanline in VisibilityGraphGenerator, this scanline has no slope
            // calculations so no additional rounding error is introduced.
            int cmp = PointComparer.Compare(first.Site.Y, second.Site.Y);
            if (0 != cmp) {
                return cmp;
            }

            // Both are at same Y so we must ensure that for equivalent Y, VClose comes after 
            // HOpen which comes after VOpen, thus make sure VOpen comes before VClose.
            if (first.IsVertical && second.IsVertical) {
                // Separate segments may join at Start and End due to overlap.
                Debug.Assert(!StaticGraphUtility.IntervalsOverlap(first.Segment, second.Segment)
                             || (0 == PointComparer.Compare(first.Segment.Start, second.Segment.End))
                             || (0 == PointComparer.Compare(first.Segment.End, second.Segment.Start))
                             , "V subsumption failure detected in SegEvent comparison");
                if (0 == cmp) {
                    // false is < true.
                    cmp = (SegEventType.VClose == first.EventType).CompareTo(SegEventType.VClose == second.EventType);
                }
                return cmp;
            }

            // If both are H segs, then sub-order by X.
            if (!first.IsVertical && !second.IsVertical) {
                // Separate segments may join at Start and End due to overlap, so compare by Start.X;
                // the ending segment (lowest Start.X) comes before the Open (higher Start.X).
                Debug.Assert(!StaticGraphUtility.IntervalsOverlap(first.Segment, second.Segment)
                             || (0 == PointComparer.Compare(first.Segment.Start, second.Segment.End))
                             || (0 == PointComparer.Compare(first.Segment.End, second.Segment.Start))
                             , "H subsumption failure detected in SegEvent comparison");
                cmp = PointComparer.Compare(first.Site.X, second.Site.X);
                return cmp;
            }

            // One is Vertical and one is Horizontal; we are only interested in the vertical at this point.
            SegEvent vEvent = first.IsVertical ? first : second;

            // Make sure that we have opened all V segs before and closed them after opening
            // an H seg at the same Y coord. Otherwise we'll miss "T" or "corner" intersections.
            // (RectilinearTests.Connected_Vertical_Segments_Are_Intersected tests that we get the expected count here.)
            // Start assuming Vevent is 'first' and it's VOpen, which should come before HOpen.
            cmp = -1; // Start with first == VOpen
            if (SegEventType.VClose == vEvent.EventType) {
                cmp = 1; // change to first == VClose
            }
            if (vEvent != first) {
                cmp *= -1; // undo the swap.
            }

            return cmp;
        }

        #endregion // IComparer<SegEvent>

        #region IComparer<ScanSegment>

        /// <summary>
        /// For ordering V segments in the scanline by X.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public int Compare(ScanSegment first, ScanSegment second) {
            if (first == second) {
                return 0;
            }
            if (first == null) {
                return -1;
            }
            if (second == null) {
                return 1;
            }

            // Note: Unlike the ScanSegment-generating scanline, this scanline has no slope
            // calculations so no additional rounding error is introduced.
            int cmp = PointComparer.Compare(first.Start.X, second.Start.X);

            // Separate segments may join at Start and End due to overlap, so compare the Y positions;
            // the Close (lowest Y) comes before the Open.
            if (0 == cmp) {
                cmp = PointComparer.Compare(first.Start.Y, second.Start.Y);
            }
            return cmp;
        }

        #endregion // IComparer<ScanSegment>

        #region Nested type: SegEvent

        internal class SegEvent {
            internal SegEvent(SegEventType eventType, ScanSegment seg) {
                EventType = eventType;
                Segment = seg;
            }

            internal SegEventType EventType { get; private set; }
            internal ScanSegment Segment { get; private set; }

            internal bool IsVertical {
                get { return (SegEventType.HOpen != EventType); }
            }

            internal Point Site {
                get { return (SegEventType.VClose == EventType) ? Segment.End : Segment.Start; }
            }


            /// <summary>
            /// </summary>
            /// <returns></returns>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object[])")]
            public override string ToString() {
                return string.Format("{0} {1} {2} {3}", EventType, IsVertical, Site, Segment);
            }
        }

        #endregion

        #region Internal data members

        // To be returned to caller; created in Generate() and used in ScanGenerate().

        // Accumulates the set of events and then sorts them by Y coord.
        readonly List<SegEvent> eventList = new List<SegEvent>();

        // Tracks the currently open V segments.
        readonly Func<ScanSegment, bool> findFirstPred;

        readonly List<ScanSegment> segmentsWithoutVisibility = new List<ScanSegment>();
        readonly RbTree<ScanSegment> verticalSegmentsScanLine;

        // For searching the tree to find the first VSeg for an HSeg.
        ScanSegment findFirstHSeg;
        VisibilityGraph visGraph;

        #endregion // Internal data members

        #region Nested type: SegEventType

        internal enum SegEventType {
            VOpen // Vertical segment start (bottom)
            ,
            VClose // Vertical segment close (top)
            ,
            HOpen // Horizontal segment open (flat, uses left point but could be either)
        }

        #endregion
    }
}