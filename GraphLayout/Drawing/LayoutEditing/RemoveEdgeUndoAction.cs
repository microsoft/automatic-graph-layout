using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    internal class RemoveEdgeUndoAction:UndoRedoAction {
        IViewer viewer;
        IViewerEdge removedEdge;
        internal RemoveEdgeUndoAction(Graph graph, IViewer viewer, IViewerEdge edge) :base(graph.GeometryGraph){
            this.viewer = viewer;
            this.removedEdge = edge;
            this.GraphBoundingBoxAfter = graph.BoundingBox; //do not change the bounding box
        }

        public override void Undo() {
            base.Undo(); 
            this.viewer.AddEdge(removedEdge, false);
        }

        public override void Redo() {
            base.Redo();
            this.viewer.RemoveEdge(removedEdge, false);
        }
    }
}
