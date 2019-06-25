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
using System.Drawing.Drawing2D;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Drawing;
using Color = System.Drawing.Color;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;

namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// it is a class holding Microsoft.Msagl.Drawing.Edge and Microsoft.Msagl.Edge
    /// </summary>
    public sealed class DEdge : DObject, IViewerEdge, IHavingDLabel, IEditableObject {
        float dashSize;
        Edge drawingEdge;

        internal DEdge(DNode source, DNode target, DrawingEdge drawingEdgeParam, ConnectionToGraph connection,
                       GViewer gviewer) : base(gviewer) {
            DrawingEdge = drawingEdgeParam;
            Source = source;
            Target = target;

            if (connection == ConnectionToGraph.Connected) {
                if (source == target)
                    source.AddSelfEdge(this);
                else {
                    source.AddOutEdge(this);
                    target.AddInEdge(this);
                }
            }

            if (drawingEdgeParam.Label != null)
                Label = new DLabel(this, drawingEdge.Label, gviewer);
        }

        /// <summary>
        /// the corresponding drawing edge
        /// </summary>
        public Edge DrawingEdge {
            get { return drawingEdge; }
            set { drawingEdge = value; }
        }

        internal DNode Source { get; set; }

        internal DNode Target { get; set; }

        /// <summary>
        /// Can be set to GraphicsPath of GDI (
        /// </summary>
        internal GraphicsPath GraphicsPath { get; set; }

        /// <summary>
        /// Color of the edge
        /// </summary>
        public Color Color {
            get { return Draw.MsaglColorToDrawingColor(DrawingEdge.Attr.Color); }
        }

        #region IEditableObject Members

        /// <summary>
        /// is set to true then the edge should set up for editing
        /// </summary>
        public bool SelectedForEditing { get; set; }

        #endregion

        #region IHavingDLabel Members

        /// <summary>
        /// keeps the pointer to the corresponding label
        /// </summary>
        public DLabel Label { get; set; }

        #endregion

        #region IViewerEdge Members

        /// <summary>
        /// underlying Drawing edge
        /// </summary>
        public Edge Edge {
            get { return DrawingEdge; }
        }

        IViewerNode IViewerEdge.Source {
            get { return Source; }
        }

        IViewerNode IViewerEdge.Target {
            get { return Target; }
        }

        /// <summary>
        /// The underlying DrawingEdge
        /// </summary>
        public override DrawingObject DrawingObject {
            get { return DrawingEdge; }
        }

        /// <summary>
        ///the radius of circles drawin around polyline corners 
        /// </summary>
        public double RadiusOfPolylineCorner { get; set; }

        #endregion

        internal override float DashSize() {
            if (dashSize > 0)
                return dashSize;
            var w = (float)DrawingEdge.Attr.LineWidth;
            var dashSizeInPoints = (float)(Draw.dashSize * GViewer.Dpi);
            return dashSize = dashSizeInPoints / w;
        }
        /// <summary>
        /// 
        /// </summary>
        protected internal override void Invalidate() {
            GraphicsPath = null;
        }

        /// <summary>
        /// calculates the rendered rectangle and RenderedBox to it
        /// </summary>
        public override void UpdateRenderedBox() {
            Rectangle box = Edge.GeometryEdge.BoundingBox;
            AddLabelBox(ref box);
            AddArrows(ref box);
            box.Pad(DrawingEdge.Attr.LineWidth);
            if (SelectedForEditing)
                box.Pad(GViewer.UnderlyingPolylineRadiusWithNoScale);
            RenderedBox = box;
        }

        void AddArrows(ref Rectangle box) {
            AddArrowAtSource(ref box);
            AddArrowAtTarget(ref box);

        }
        void AddArrowAtTarget(ref Rectangle box) {
            if (DrawingEdge.EdgeCurve != null && DrawingEdge.Attr != null && DrawingEdge.Attr.ArrowAtTarget)
                AddArrowToBox(DrawingEdge.EdgeCurve.End, DrawingEdge.ArrowAtTargetPosition, DrawingEdge.Attr.LineWidth, ref box);
        }

        void AddArrowAtSource(ref Rectangle box) {
            if (DrawingEdge.EdgeCurve != null && DrawingEdge.Attr != null && DrawingEdge.Attr.ArrowAtSource)
                AddArrowToBox(DrawingEdge.EdgeCurve.End, DrawingEdge.ArrowAtSourcePosition, DrawingEdge.Attr.LineWidth, ref box);
        }

        void AddArrowToBox(Point start, Point end, double width, ref Rectangle box) {
            //it does not hurt to add a larger piece 
            Point dir = (end - start).Rotate(Math.PI / 2);

            box.Add(end + dir);
            box.Add(end - dir);
            box.Add(start + dir);
            box.Add(start - dir);

            box.Left -= width;
            box.Top += width;
            box.Right += width;
            box.Bottom -= width;
        }

        void AddLabelBox(ref Rectangle box) {
            if (Label != null && DGraph.DLabelIsValid(Label))
                box.Add(Label.DrawingLabel.BoundingBox);
        }
    }
}