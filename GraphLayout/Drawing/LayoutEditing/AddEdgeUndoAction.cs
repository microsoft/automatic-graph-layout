using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Drawing {

    internal class AddEdgeUndoAction : UndoRedoAction {
        IViewerEdge addedEdge;
        IViewer viewer;
        internal AddEdgeUndoAction(IViewer viewer, IViewerEdge edge) :base(viewer.ViewerGraph.DrawingGraph.GeometryGraph){
            this.viewer = viewer;
            this.addedEdge = edge;
        }

        public override void Undo() {
            base.Undo();
            this.viewer.RemoveEdge(addedEdge, false);
        }

        public override void Redo() {
            base.Redo();
            this.viewer.AddEdge(addedEdge, false);
        }
    }
}
