using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Prototype.LayoutEditing;
using GeomEdge = Microsoft.Msagl.Core.Layout.Edge;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// undoes/redoes edge editing when dragging the smoothed polyline corner
    /// </summary>
    public class EdgeDragUndoRedoAction: UndoRedoAction {
        GeomEdge editedEdge;
      
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="editedEdgePar"></param>
        public EdgeDragUndoRedoAction(GeomEdge editedEdgePar):base((GeometryGraph)editedEdgePar.GeometryParent) {
            editedEdge = editedEdgePar;         
        }
        /// <summary>
        /// undoes the editing
        /// </summary>
        public override void Undo() {
            ClearAffectedObjects();
            Restore();
        }

       
        /// <summary>
        /// redoes the editing
        /// </summary>
        public override void Redo() {
            ClearAffectedObjects();
            Restore();
        }

        void Restore() {
            var erd = (EdgeRestoreData) GetRestoreData(editedEdge);
            editedEdge.Curve = erd.Curve;
            editedEdge.UnderlyingPolyline = erd.UnderlyingPolyline;
            if (editedEdge.EdgeGeometry.SourceArrowhead != null)
                editedEdge.EdgeGeometry.SourceArrowhead.TipPosition = erd.ArrowheadAtSourcePosition;
            if (editedEdge.EdgeGeometry.TargetArrowhead != null)
                editedEdge.EdgeGeometry.TargetArrowhead.TipPosition = erd.ArrowheadAtTargetPosition;
        }
    }
}