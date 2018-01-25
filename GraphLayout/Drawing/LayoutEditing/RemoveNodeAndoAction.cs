using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    internal class RemoveNodeUndoAction:UndoRedoAction {
        IViewerNode removedNode;
        IViewer viewer;
        internal RemoveNodeUndoAction(IViewer viewer, IViewerNode node) :base(viewer.ViewerGraph.DrawingGraph.GeometryGraph){
            this.viewer = viewer;
            this.removedNode = node;
            this.GraphBoundingBoxAfter = viewer.ViewerGraph.DrawingGraph.BoundingBox; //do not change the bounding box
        }

        public override void Undo() {
            base.Undo();
            this.viewer.AddNode(removedNode, false);
        }

        public override void Redo() {
            base.Redo();
            this.viewer.RemoveNode(removedNode, false);
        }
    }
}
