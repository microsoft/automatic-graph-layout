using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Incremental {
    /// <summary>
    /// Wrapper for the MSAGL node to add force and velocity vectors
    /// </summary>
    internal class FiNode {
        internal Point desiredPosition;
        internal Point force;
        internal int index;
        internal Node mNode;
        internal OverlapRemovalNode mOlapNodeX, mOlapNodeY;
        internal Point previousCenter;
        private Point center;
        /// <summary>
        /// local cache of node center (which in the MSAGL node has to be computed from the bounding box)
        /// </summary>
        internal Point Center {
            get {
                return center;
            }
            set {
                center = mNode.Center = value;
            }
        }
        /// <summary>
        /// When mNode's bounds change we need to update our local
        /// previous and current center to MSAGL node center
        /// and update width and height
        /// </summary>
        internal void ResetBounds() {
            center = previousCenter = mNode.Center;
            Width = mNode.Width;
            Height = mNode.Height;
        }
        internal double stayWeight = 1;

        /// <summary>
        /// We also keep a local copy of Width and Height since it doesn't change and we don't want to keep going back to
        /// mNode.BoundingBox
        /// </summary>
        internal double Width;
        internal double Height;

        public FiNode(int index, Node mNode) {
            this.index = index;
            this.mNode = mNode;
            ResetBounds();
        }

        internal OverlapRemovalNode getOlapNode(bool horizontal) {
            return horizontal ? mOlapNodeX : mOlapNodeY;
        }

        internal void SetOlapNode(bool horizontal, OverlapRemovalNode olapNode) {
            if (horizontal)
                mOlapNodeX = olapNode;
            else
                mOlapNodeY = olapNode;
        }

        internal void SetVariableDesiredPos(bool horizontal) {
            if (horizontal)
                mOlapNodeX.Variable.DesiredPos = desiredPosition.X;
            else
                mOlapNodeY.Variable.DesiredPos = desiredPosition.Y;
        }
        /// <summary>
        /// Update the current X or Y coordinate of the node center from the result of a solve
        /// </summary>
        /// <param name="horizontal"></param>
        internal void UpdatePos(bool horizontal) {
            if (horizontal)
                // Y has not yet been solved so reuse the previous position.
                Center = new Point(getOlapNode(true).Position, previousCenter.Y);
            else
                // Assumes X has been solved and set on prior pass.
                Center = new Point(Center.X, getOlapNode(false).Position);
        }

        public override string ToString()
        {
            return "FINode(" + index + "):" + mNode;
        }
    }
}