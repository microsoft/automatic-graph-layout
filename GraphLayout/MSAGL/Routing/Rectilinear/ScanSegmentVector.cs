using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
    internal partial class SparseVisibilityGraphGenerator : VisibilityGraphGenerator {
        /// <summary>
        /// This forms the vector of ScanSegments for the sparse VisibilityGraph.
        /// </summary>
        internal class ScanSegmentVector {
            private readonly ScanSegmentVectorItem[] vector;

            internal ScanSegmentVector(Set<double> coordMap, bool isHorizontal) {
                this.vector = new ScanSegmentVectorItem[coordMap.Count];
                this.IsHorizontal = isHorizontal;
                var coords = coordMap.OrderBy(coord => coord).GetEnumerator();
                coords.MoveNext();
                for (int ii = 0; ii < this.vector.Length; ++ii) {
                    this.vector[ii] = new ScanSegmentVectorItem(coords.Current);
                    coords.MoveNext();
                }
            }

            /// <summary>
            /// The index of the scan segment vector we're appending to on the ScanSegment-generation sweep.
            /// </summary>
            internal int CurrentSlotIndex { get; set; }

            /// <summary>
            /// The number of slots.
            /// </summary>
            internal int Length {
                get { return this.vector.Length; }
            }
            
            /// <summary>
            /// The item at the index of the scan segment vector we're appending to on the ScanSegment-generation sweep.
            /// </summary>
            internal ScanSegmentVectorItem CurrentSlot { get { return this.vector[this.CurrentSlotIndex]; } }

            /// <summary>
            /// The indexed item in the vector.
            /// </summary>
            internal ScanSegmentVectorItem this[int slot] {
                get { return this.vector[slot]; }
            }

            /// <summary>
            /// Appends a ScanSegment to the linked list in the "Current" slot.
            /// </summary>
            internal void CreateScanSegment(Point start, Point end, double weight, PointAndCrossingsList gbcList) {
                this.CurrentSlot.AppendScanSegment(new ScanSegment(start, end, weight, gbcList));
            }

            internal void ScanSegmentsCompleteForCurrentSlot() {
                ++this.CurrentSlotIndex;
            }

            internal void ScanSegmentsComplete() {
                foreach (var item in this.vector) {
                    item.AddPendingPerpendicularCoordsToScanSegments();
                }
            }

            /// <summary>
            /// Returns an enumeration of the vector of ScanSegmentVectorItems.
            /// </summary>
            internal IEnumerable<ScanSegmentVectorItem> Items {
                get { return this.vector; }
            }

            /// <summary>
            /// Reset vector state between passes.
            /// </summary>
            internal void ResetForIntersections() {
                foreach (ScanSegmentVectorItem t in this.vector) {
                    t.ResetForIntersections();
                }
            }

            /// <summary>
            /// Indicates if this contains horizontal or vertical ScanSegments.
            /// </summary>
            private bool IsHorizontal { get; set;  }

            /// <summary>
            /// Search the vector for the nearest slot in the specified direction.
            /// </summary>
            internal int FindNearest(double coord, int directionIfMiss) {
                // Array.BinarySearch doesn't allow mapping from ScanSegmentVectorItem to its Coord.
                int low = 0;
                int high = this.vector.Length - 1;

                if (coord <= this.vector[low].Coord) {
                    return low;
                }
                if (coord >= this.vector[high].Coord) {
                    return high;
                }

                while ((high - low) > 2) {
                    var mid = low + ((high - low)/2);
                    var item = this.vector[mid];
                    if (coord < item.Coord) {
                        high = mid;
                        continue;
                    }
                    if (coord > item.Coord) {
                        low = mid;
                        continue;
                    }

                    // TODOsparse - profile - see if I really need the perpCoordMap
                    Debug.Assert(false, "Should not be here if coord is in the vector");
                    return mid;
                }

                // We know the value is between low and high, non-inclusive.
                for (++low; low <= high; ++low) {
                    var item = this.vector[low];
                    if (coord < item.Coord) {
                        return (directionIfMiss > 0) ? low : low - 1;
                    }
                    if (coord == item.Coord) {
                        break;
                    }
                }

                // TODOsparse - profile - see if I really need the perpCoordMap
                Debug.Assert(false, "Should not be here if coord is in the vector");
                return low;
            }

            internal void CreateSparseVerticesAndEdges(VisibilityGraph vg) {
                foreach (var item in this.vector) {
                    item.ResetForIntersections();
                    for (var segment = item.FirstSegment; segment != null; segment = segment.NextSegment) {
                        segment.CreateSparseVerticesAndEdges(vg);
                    }
                }
            }

            // Get the coordinate that remains constant along a segment in this vector.
            internal double GetParallelCoord(Point site) {
                return this.IsHorizontal ? site.Y : site.X;
            }

            // Get the coordinate that changes along a segment in this vector (and is thus the parallel
            // coord of an intersecting segment).
            internal double GetPerpendicularCoord(Point site) {
                return this.IsHorizontal ? site.X : site.Y;
            }

            internal void ConnectAdjoiningSegmentEndpoints() {
                // Make sure that any series of segments (of different overlappedness) that have points in the
                // graph are connected at adjoining starts/ends and ends/starts (these adjoining points may not be
                // Steiner points in the graph if they are on indirect segments.
                foreach (var item in this.vector) {
                    item.ResetForIntersections();
                    var prevSegment = item.FirstSegment;
                    for (var segment = prevSegment.NextSegment; segment != null; segment = segment.NextSegment) {
                        if (segment.HasSparsePerpendicularCoords && prevSegment.HasSparsePerpendicularCoords) {
                            if (segment.Start == prevSegment.End) {
                                double perpCoord = this.GetPerpendicularCoord(segment.Start);
                                prevSegment.AddSparseEndpoint(perpCoord);
                                segment.AddSparseEndpoint(perpCoord);
                            }
                        }
                        prevSegment = segment;
                    }
                }
           }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            public override string ToString() {
                return (this.IsHorizontal ? "(H) count == " : "(V) count == ") + this.vector.Count();
            }
        }
    }
}