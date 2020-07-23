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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using BBox = Microsoft.Msagl.Core.Geometry.Rectangle;
using Color = System.Drawing.Color;
using MouseButtons = System.Windows.Forms.MouseButtons;
using P2 = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// this class serves as a drawing panel for GViewer
    /// </summary>
    internal class DrawingPanel : Control {
        readonly Color rubberRectColor = Color.Black;
        MouseButtons currentPressedButton;
        GViewer gViewer;

        System.Drawing.Point mouseDownPoint;

        System.Drawing.Point mouseUpPoint;
        P2 rubberLineEnd;
        P2 rubberLineStart;
        bool zoomWindow;
        PlaneTransformation mouseDownTransform;

        internal GViewer GViewer {
            private get { return gViewer; }
            set { gViewer = value; }
        }

    
        DraggingMode MouseDraggingMode {
            get {
                if (gViewer.panButton.Checked)
                    return DraggingMode.Pan;
                if (gViewer.windowZoomButton.Checked)
                    return DraggingMode.WindowZoom;
                return DraggingMode.Default;
            }
        }

        bool DrawingRubberEdge { get; set; }

        P2 RubberLineEnd {
            get { return rubberLineEnd; }
            set {
                if (DrawingRubberEdge)
                    Invalidate(CreateRectForRubberEdge());
                rubberLineEnd = value;
            }
        }

        EdgeGeometry CurrentRubberEdge { get; set; }

        internal void SetDoubleBuffering() {
            // the magic calls for invoking doublebuffering
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }


        protected override void OnPaint(PaintEventArgs e) {

            if (gViewer != null && gViewer.Graph != null && gViewer.Graph.GeometryGraph != null) {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                gViewer.ProcessOnPaint(e.Graphics, null);
            }
            if (CurrentRubberEdge != null)
                using (GraphicsPath gp = Draw.CreateGraphicsPath(CurrentRubberEdge.Curve))
                using (var pen = new Pen(Brushes.Black, (float) GViewer.LineThicknessForEditing))
                    e.Graphics.DrawPath(pen, gp);

            if (DrawingRubberEdge)
                e.Graphics.DrawLine(new Pen(Brushes.Black, (float) GViewer.LineThicknessForEditing),
                                    (float) rubberLineStart.X, (float) rubberLineStart.Y, (float) RubberLineEnd.X,
                                    (float) RubberLineEnd.Y);
            base.OnPaint(e); // Filippo Polo 13/11/07; if I don't do this, onpaint events won't be invoked
            gViewer.RaisePaintEvent(e);
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            MsaglMouseEventArgs iArgs = CreateMouseEventArgs(e);
            gViewer.RaiseMouseDownEvent(iArgs);
            if (!iArgs.Handled) {
                currentPressedButton = e.Button;
                if (currentPressedButton == MouseButtons.Left)
                    if (ClientRectangle.Contains(PointToClient(MousePosition))) {
                        mouseDownPoint = new Point(e.X, e.Y);
                        if (MouseDraggingMode != DraggingMode.Pan)
                            zoomWindow = true;
                        else {
                            mouseDownTransform = gViewer.Transform.Clone();
                        }
                    }
            }
        }


        
        protected override void OnMouseUp(MouseEventArgs args) {
            base.OnMouseUp(args);
            MsaglMouseEventArgs iArgs = CreateMouseEventArgs(args);
            gViewer.RaiseMouseUpEvent(iArgs);
            
            if (!iArgs.Handled) {
                if (gViewer.OriginalGraph != null && MouseDraggingMode == DraggingMode.WindowZoom) {
                    var p = mouseDownPoint;
                    double f = Math.Max(Math.Abs(p.X - args.X), Math.Abs(p.Y - args.Y))/GViewer.Dpi;
                    if (f > gViewer.ZoomWindowThreshold && zoomWindow) {
                        mouseUpPoint = new Point(args.X, args.Y);
                        if (ClientRectangle.Contains(mouseUpPoint)) {
                            //var r = GViewer.RectFromPoints(mouseDownPoint, mouseUpPoint);
                            //r.Intersect(gViewer.DestRect);
                            if (GViewer.ModifierKeyWasPressed() == false) {
                              
                                P2 p1 = gViewer.ScreenToSource(mouseDownPoint);
                                P2 p2 = gViewer.ScreenToSource(mouseUpPoint);
                                double sc = Math.Min(Width / Math.Abs(p1.X - p2.X),
                                    Height / Math.Abs(p1.Y - p2.Y));
                                P2 center = 0.5f*(p1 + p2);
                                gViewer.SetTransformOnScaleAndCenter(sc, center);
                                Invalidate();
                            }
                        }
                    }
                }
            }
            zoomWindow = false;
        }

        /// <summary>
        /// Set context menu strip
        /// </summary>
        /// <param name="contexMenuStrip"></param>
        public void SetCms(ContextMenuStrip contexMenuStrip) {
            MouseClick +=
                delegate(object sender, MouseEventArgs e) {
                    if (e.Button == MouseButtons.Right) {
                        var newE = new MouseEventArgs(
                            MouseButtons.None,
                            e.Clicks,
                            e.X,
                            e.Y,
                            e.Delta);

                        OnMouseMove(newE);

                        contexMenuStrip.Show(this, e.X, e.Y);
                    }
                };
        }

        protected override void OnMouseMove(MouseEventArgs args) {
            MsaglMouseEventArgs iArgs = CreateMouseEventArgs(args);
            gViewer.RaiseMouseMoveEvent(iArgs);
            gViewer.RaiseRegularMouseMove(args);
            if (!iArgs.Handled) {
                if (gViewer.Graph != null) {
                    SetCursor(args);
                    if (MouseDraggingMode == DraggingMode.Pan)
                        ProcessPan(args);
                    else if (zoomWindow)
                    {
                        //the user is holding the left button, do nothing
                    }
                    else
                        HitIfBbNodeIsNotNull(args);
                }
            }
        }


        void HitIfBbNodeIsNotNull(MouseEventArgs args) {
            if (gViewer.DGraph != null && gViewer.BbNode != null)
                gViewer.Hit(args);
        }


        static MsaglMouseEventArgs CreateMouseEventArgs(MouseEventArgs args) {
            return new ViewerMouseEventArgs(args);
        }


        void SetCursor(MouseEventArgs args) {
            Cursor cur;
            if (MouseDraggingMode == DraggingMode.Pan) {
                cur = args.Button == MouseButtons.Left
                          ? gViewer.panGrabCursor
                          : gViewer.panOpenCursor;
            } else
                cur = gViewer.originalCursor;

            if (cur != null)
                Cursor = cur;
        }

        
        void ProcessPan(MouseEventArgs args) {
            if (ClientRectangle.Contains(args.X, args.Y)) {
                if (args.Button == MouseButtons.Left) {
                    if (mouseDownTransform != null) {
                        gViewer.Transform[0, 2] = mouseDownTransform[0, 2] + args.X - mouseDownPoint.X;
                        gViewer.Transform[1, 2] = mouseDownTransform[1, 2] + args.Y - mouseDownPoint.Y;
                    }
                    gViewer.Invalidate();
                } else
                    GViewer.Hit(args);
            }
        }

        protected override void OnKeyUp(KeyEventArgs e) {
            gViewer.OnKey(e);
            base.OnKeyUp(e);
        }


        internal void DrawRubberLine(MsaglMouseEventArgs args) {
            RubberLineEnd = gViewer.ScreenToSource(new Point(args.X, args.Y));
            DrawRubberLineWithKnownEnd();
        }

        internal void DrawRubberLine(P2 point) {
            RubberLineEnd = point;
            DrawRubberLineWithKnownEnd();
        }

        void DrawRubberLineWithKnownEnd() {
            DrawingRubberEdge = true;
            Invalidate(CreateRectForRubberEdge());
        }

        Rectangle CreateRectForRubberEdge() {
            var rect = new BBox(rubberLineStart, RubberLineEnd);
            double w = gViewer.LineThicknessForEditing;
            var del = new P2(-w, w);
            rect.Add(rect.LeftTop + del);
            rect.Add(rect.RightBottom - del);
            return GViewer.CreateScreenRectFromTwoCornersInTheSource(rect.LeftTop, rect.RightBottom);
        }

        internal void StopDrawRubberLine() {
            DrawingRubberEdge = false;
            Invalidate(CreateRectForRubberEdge());
        }

        internal void MarkTheStartOfRubberLine(P2 point) {
            rubberLineStart = point;
        }

        internal void DrawRubberEdge(EdgeGeometry edgeGeometry) {
            BBox rectToInvalidate = edgeGeometry.BoundingBox;
            if (CurrentRubberEdge != null) {
                BBox b = CurrentRubberEdge.BoundingBox;
                rectToInvalidate.Add(b);
            }
            CurrentRubberEdge = edgeGeometry;
            GViewer.Invalidate(GViewer.CreateScreenRectFromTwoCornersInTheSource(rectToInvalidate.LeftTop,
                                                                                 rectToInvalidate.RightBottom));
        }

        internal void StopDrawingRubberEdge() {
            if (CurrentRubberEdge != null)
                GViewer.Invalidate(
                    GViewer.CreateScreenRectFromTwoCornersInTheSource(
                        CurrentRubberEdge.BoundingBox.LeftTop,
                        CurrentRubberEdge.BoundingBox.RightBottom));

            CurrentRubberEdge = null;
        }
    }
}