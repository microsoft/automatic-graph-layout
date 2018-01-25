using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Prototype.LayoutEditing;
using Microsoft.Msagl.Routing;
using GeomEdge = Microsoft.Msagl.Core.Layout.Edge;

namespace Microsoft.Msagl.Drawing {
    internal class SiteRemoveUndoAction:UndoRedoAction {
        Site removedSite;
        internal Site RemovedSite {
            get { return removedSite; }
            set { 
                removedSite = value;
            }
        }

        GeomEdge editedEdge;

        /// <summary>
        /// Constructor. At the moment of the constructor call the site should not be inserted yet
        /// </summary>
        /// <param name="edgePar"></param>
        public SiteRemoveUndoAction(GeomEdge edgePar)
            : base((GeometryGraph)edgePar.GeometryParent) {
            this.editedEdge = edgePar;
            this.AddRestoreData(editedEdge, RestoreHelper.GetRestoreData(editedEdge));
        }
        /// <summary>
        /// undoes the editing
        /// </summary>
        public override void Undo() {
            Site prev = RemovedSite.Previous;
            Site next = RemovedSite.Next;
            prev.Next = RemovedSite;
            next.Previous = RemovedSite;
            GeometryGraphEditor.DragEdgeWithSite(new Point(0, 0), editedEdge, prev);
        }

        /// <summary>
        /// redoes the editing
        /// </summary>
        public override void Redo() {
            Site prev = RemovedSite.Previous;
            Site next = RemovedSite.Next;
            prev.Next = next;
            next.Previous = prev;
            GeometryGraphEditor.DragEdgeWithSite(new Point(0, 0), editedEdge, prev);
        }
    }
}
