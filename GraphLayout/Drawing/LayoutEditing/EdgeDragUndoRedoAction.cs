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