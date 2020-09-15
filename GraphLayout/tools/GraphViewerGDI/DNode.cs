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
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Color = System.Drawing.Color;
using GLEEEdge = Microsoft.Msagl.Core.Layout.Edge;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using GLEENode = Microsoft.Msagl.Core.Layout.Node;
using DrawingNode = Microsoft.Msagl.Drawing.Node;

namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// it is a class holding Microsoft.Msagl.Drawing.Node and Microsoft.Msagl.Node
    /// </summary>
    public sealed class DNode : DObject, IViewerNode, IHavingDLabel {
        readonly Set<Port> portsToDraw = new Set<Port>();
        float dashSize;
        internal List<DEdge> inEdges = new List<DEdge>();
        internal List<DEdge> outEdges = new List<DEdge>();
        internal List<DEdge> selfEdges = new List<DEdge>();

        internal DNode(DrawingNode drawingNodeParam, GViewer gviewer) : base(gviewer) {
            DrawingNode = drawingNodeParam;
        }

        /// <summary>
        /// the corresponding drawing node
        /// </summary>
        public DrawingNode DrawingNode { get; set; }


        /// <summary>
        /// return the color of a node
        /// </summary>
        public Color Color {
            get { return Draw.MsaglColorToDrawingColor(DrawingNode.Attr.Color); }
        }


        /// <summary>
        /// Fillling color of a node
        /// </summary>
        public Color FillColor {
            get { return Draw.MsaglColorToDrawingColor(DrawingNode.Attr.FillColor); }
        }

        
        #region IHavingDLabel Members

        /// <summary>
        /// gets / sets the rendered label of the object
        /// </summary>
        public DLabel Label { get; set; }

        #endregion

        #region IViewerNode Members

        /// <summary>
        /// 
        /// </summary>
        public void SetStrokeFill() {
        }

        /// <summary>
        /// returns the corresponding DrawingNode
        /// </summary>
        public DrawingNode Node {
            get { return DrawingNode; }
        }

        /// <summary>
        /// return incoming edges
        /// </summary>
        public IEnumerable<IViewerEdge> InEdges {
            get {
                foreach (DEdge e in inEdges)
                    yield return e;
            }
        }

        /// <summary>
        /// returns outgoing edges
        /// </summary>
        public IEnumerable<IViewerEdge> OutEdges {
            get {
                foreach (DEdge e in outEdges)
                    yield return e;
            }
        }

        /// <summary>
        /// returns self edges
        /// </summary>
        public IEnumerable<IViewerEdge> SelfEdges {
            get {
                foreach (DEdge e in selfEdges)
                    yield return e;
            }
        }

#pragma warning disable 67
        public event Action<IViewerNode> IsCollapsedChanged;
#pragma warning restore 67

        /// <summary>
        /// returns the corresponding drawing object
        /// </summary>
        public override DrawingObject DrawingObject {
            get { return DrawingNode; }
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddPort(Port port) {
            portsToDraw.Insert(port);
        }


        /// <summary>
        /// it is a class holding Microsoft.Msagl.Drawing.Node and Microsoft.Msagl.Node
        /// </summary>
        public void RemovePort(Port port) {
            portsToDraw.Remove(port);
        }

        #endregion

        internal void AddOutEdge(DEdge edge) {
            outEdges.Add(edge);
        }

        internal void AddInEdge(DEdge edge) {
            inEdges.Add(edge);
        }

        internal void AddSelfEdge(DEdge edge) {
            selfEdges.Add(edge);
        }

        internal override float DashSize() {
            if (dashSize > 0)
                return dashSize;
            var w = (float) DrawingNode.Attr.LineWidth;
            if (w < 0)
                w = 1;
            var dashSizeInPoints = (float) (Draw.dashSize*GViewer.Dpi);
            return dashSize = dashSizeInPoints/w;
        }

        /// <summary>
        /// 
        /// </summary>
        protected internal override void Invalidate() {
        }
        /// <summary>
        /// calculates the rendered rectangle and RenderedBox to it
        /// </summary>
        public override void UpdateRenderedBox() {
            DrawingNode node = DrawingNode;
            double del = node.Attr.LineWidth/2;
          
            Rectangle box = node.GeometryNode.BoundaryCurve.BoundingBox;
            box.Pad(del);
            RenderedBox = box;
        }


        internal void RemoveOutEdge(DEdge de) {
            outEdges.Remove(de);
        }

        internal void RemoveInEdge(DEdge de) {
            inEdges.Remove(de);
        }

        internal void RemoveSelfEdge(DEdge de) {
            selfEdges.Remove(de);
        }
    }
}