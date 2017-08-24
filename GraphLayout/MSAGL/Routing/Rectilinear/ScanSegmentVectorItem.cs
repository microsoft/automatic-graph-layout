//
// ScanSegmentVectorItem.cs
// MSAGL base class to create the visibility graph consisting of nlogn ScanSegment intersections for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear {
    internal partial class SparseVisibilityGraphGenerator {
        /// <summary>
        /// This forms one slot in the scan segment vector.
        /// </summary>
        internal class ScanSegmentVectorItem {
            /// <summary>
            /// The head of the linked list.
            /// </summary>
            internal ScanSegment FirstSegment;

            /// <summary>
            /// The current segment of the linked list, used when appending or intersecting.
            /// </summary>
            internal ScanSegment CurrentSegment;

            /// <summary>
            /// Perpendicular coordinates that are not in a ScanSegment, due to either not having the ScanSegments created
            /// yet or because it will be faster to do a single pass after accumulating them (e.g. for GroupBoundaryCrossings).
            /// </summary>
            private List<double> pendingPerpCoords;

            internal void AddPendingPerpendicularCoord(double coord) {
                if (pendingPerpCoords == null) {
                    pendingPerpCoords = new List<double>();
                }
                pendingPerpCoords.Add(coord);
            }

            /// <summary>
            /// Restores state between intersection passes.
            /// </summary>
            internal void ResetForIntersections() {
                Debug.Assert(null != this.FirstSegment, "Empty ScanSegmentVectorItem");
                this.CurrentSegment = this.FirstSegment;
            }

            /// <summary>
            /// Indicates whether ScanSegments in this item are horizontally or vertically oriented.
            /// </summary>
            internal bool IsHorizontal {
                get { return !this.FirstSegment.IsVertical; }
            }

            /// <summary>
            /// Returns the constant coordinate of the ScanSegments in this item, i.e. the coordinate
            /// that intersects the perpendicular axis.
            /// </summary>
            internal double Coord { get; private set; }

            /// <summary>
            /// Ctor, taking the parallel (constant) coordinate.
            /// </summary>
            /// <param name="coord">the parallel (constant) coordinate</param>
            internal ScanSegmentVectorItem(double coord) {
                this.Coord = coord;
            }

            /// <summary>
            /// Move along the linked list until we hit the ScanSegment that contains the point.
            /// </summary>
            internal bool TraverseToSegmentContainingPoint(Point point) {
                // This is not a simple Next() because scan segments are extended "through" obstacles
                // (intermixing overlapped and non-overlapped) and thus a ScanSegment's Start and End
                // may not be in the vertexPoints collection and the ScanSegment must be skipped.
                if (this.CurrentSegment.ContainsPoint(point)) {
                    return true;
                }

                var pointCoord = this.IsHorizontal ? point.Y : point.X;
                if (!PointComparer.Equal(this.Coord, pointCoord)) {
                    Debug.Assert(PointComparer.Compare(this.Coord, pointCoord) == -1, "point is before current Coord");
                    while (this.MoveNext()) {
                        // Skip to the end of the linked list if this point is not on the same coordinate.
                    }
                    return false;
                }

                for (;;) {
                    // In the event of mismatched rounding on horizontal versus vertical intersections
                    // with a sloped obstacle side, we may have a point that is just before or just
                    // after the current segment.  If the point is in some space that doesn't have a
                    // scansegment, and if we are "close enough" to one end or the other of a scansegment,
                    // then grow the scansegment enough to include the new point.
                    if ((null == this.CurrentSegment.NextSegment) ||
                            PointComparer.GetDirections(this.CurrentSegment.End, point)
                                == PointComparer.GetDirections(point, this.CurrentSegment.NextSegment.Start)) {
                        if (ApproximateComparer.CloseIntersections(this.CurrentSegment.End, point)) {
                            this.CurrentSegment.Update(this.CurrentSegment.Start, point);
                            return true;
                        }
                    }

                    if (!this.MoveNext()) {
                        return false;
                    }
                    if (this.CurrentSegment.ContainsPoint(point)) {
                        return true;
                    }

                    // This is likely the reverse of the above; the point rounding mismatched to just before
                    // rather than just after the current segment.
                    if (PointComparer.IsPureLower(point, this.CurrentSegment.Start)) {
                        Debug.Assert(ApproximateComparer.CloseIntersections(this.CurrentSegment.Start, point), "Skipped over the point in the ScanSegment linked list");
                        this.CurrentSegment.Update(point, this.CurrentSegment.End);
                        return true;
                    }
                }
            
            }

            internal bool MoveNext() {
                this.CurrentSegment = this.CurrentSegment.NextSegment;
                return this.HasCurrent;
            }

            internal bool HasCurrent {
                get { return (null != this.CurrentSegment); }
            }

            /// <summary>
            /// Returns true if the point is the end of the current segment and there is an adjoining NextSegment.
            /// </summary>
            internal bool PointIsCurrentEndAndNextStart(Point point) {
                return (point == this.CurrentSegment.End) && (null != this.CurrentSegment.NextSegment) && (point == this.CurrentSegment.NextSegment.Start);
            }

            /// <summary>
            /// Set Current to the ScanSegment containing the perpendicular coordinate, then add that coordinate to its
            /// sparse-vector coordinate list.
            /// </summary>
            /// <param name="perpCoord"></param>
            internal void AddPerpendicularCoord(double perpCoord) {
                var point = this.IsHorizontal ? new Point(perpCoord, this.Coord) : new Point(this.Coord, perpCoord);
                this.TraverseToSegmentContainingPoint(point);
                this.CurrentSegment.AddSparseVertexCoord(perpCoord);
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            public override string ToString() {
                if (null == this.FirstSegment) {
                    return "-0- " + this.Coord;
                }
                return (this.IsHorizontal ? "(H) Y == " : "(V) X == ") + this.Coord;
            }

            internal void AppendScanSegment(ScanSegment segment) {
                if (null == this.FirstSegment) {
                    this.FirstSegment = segment;
                } else {
                    // Note: segment.Start may != Current.End due to skipping internal ScanSegment creation for non-overlapped obstacles.
                    this.CurrentSegment.NextSegment = segment;
                }
                this.CurrentSegment = segment;
            }
            
            internal void AddPendingPerpendicularCoordsToScanSegments() {
                if (this.pendingPerpCoords != null) {
                    this.ResetForIntersections();
                    foreach (var point in this.pendingPerpCoords) {
                        this.AddPerpendicularCoord(point);
                    }
                }
            }
        }
    }
}