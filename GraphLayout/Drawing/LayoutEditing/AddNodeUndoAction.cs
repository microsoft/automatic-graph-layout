using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    internal class AddNodeUndoAction: UndoRedoAction {
        IViewerNode addedNode;
        IViewer viewer;
        internal AddNodeUndoAction(Graph graph, IViewer viewer, IViewerNode node) :base(graph.GeometryGraph){
            this.viewer = viewer;
            this.addedNode = node;
        }

        public override void Undo() {
            base.Undo();
            this.viewer.RemoveNode(addedNode, false);
        }

        public override void Redo() {
            base.Redo();
            this.viewer.AddNode(addedNode, false);
        }
    }
}
