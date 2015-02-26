/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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

            internal void SetSides(Directions dir, RBNode<BasicObstacleSide> neighborNode, RBNode<BasicObstacleSide> overlapEndNode,
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