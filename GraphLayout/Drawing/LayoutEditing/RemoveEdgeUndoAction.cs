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
﻿using System;
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
