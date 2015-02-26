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
