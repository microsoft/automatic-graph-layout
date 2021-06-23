//
// NeighborSides.cs
// MSAGL class for neighboring sides for scanline operations in Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear {
    internal partial class VisibilityGraphGenerator
    {
        /// <summary>
        /// From an OpenVertexEvent or CloseVertexEvent, we search in the high and low direction for neighbors.
        /// </summary>
        protected class NeighborSides
        {
            /// <summary>
            /// The HighObstacleSide of the low neighbor.
            /// </summary>
            internal RBNode<BasicObstacleSide> LowNeighbor { get; private set; }

            /// <summary>
            /// Dereferences the node if non-null to return the side Item.
            /// </summary>
            internal BasicObstacleSide LowNeighborSide { get { return (null == this.LowNeighbor) ? null : this.LowNeighbor.Item; } }

            /// <summary>
            /// A LowObstacleSide that we pass through in the low direction into open space.
            /// </summary>
            internal RBNode<BasicObstacleSide> LowOverlapEnd { get; private set; }

            /// <summary>
            /// A group that we pass through toward the low neighbor.  Avoids reflections going through group boundaries.
            /// </summary>
            internal BasicObstacleSide GroupSideInterveningBeforeLowNeighbor { get; private set; }

            /// <summary>
            /// The LowObstacleSide of the high neighbor.
            /// </summary>
            internal RBNode<BasicObstacleSide> HighNeighbor { get; private set; }

            /// <summary>
            /// Dereferences the node if non-null to return the side Item.
            /// </summary>
            internal BasicObstacleSide HighNeighborSide { get { return (null == this.HighNeighbor) ? null : this.HighNeighbor.Item; } }

            /// <summary>
            /// A HighObstacleSide that we pass through in the high direction into open space.
            /// </summary>
            internal RBNode<BasicObstacleSide> HighOverlapEnd { get; private set; }

            /// <summary>
            /// A group that we pass through toward the high neighbor.  Avoids reflections going through group boundaries.
            /// </summary>
            internal BasicObstacleSide GroupSideInterveningBeforeHighNeighbor { get; private set; }

            internal void Clear() {
                this.LowNeighbor = null;
                this.LowOverlapEnd = null;
                this.GroupSideInterveningBeforeLowNeighbor = null;
                this.HighNeighbor = null;
                this.HighOverlapEnd = null;
                this.GroupSideInterveningBeforeHighNeighbor = null;
            }

            internal void SetSides(Direction dir, RBNode<BasicObstacleSide> neighborNode, RBNode<BasicObstacleSide> overlapEndNode,
                                    BasicObstacleSide interveningGroupSide) {
                if (StaticGraphUtility.IsAscending(dir)) {
                    HighNeighbor = neighborNode;
                    HighOverlapEnd = overlapEndNode;
                    this.GroupSideInterveningBeforeHighNeighbor = interveningGroupSide;
                    return;
                }
                LowNeighbor = neighborNode;
                LowOverlapEnd = overlapEndNode;
                this.GroupSideInterveningBeforeLowNeighbor = interveningGroupSide;
            }
        }
    }
}