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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Drawing;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Ellipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using Node = Microsoft.Msagl.Drawing.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Polyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
using Shape = Microsoft.Msagl.Drawing.Shape;
using Size = System.Windows.Size;

namespace Microsoft.Msagl.WpfGraphControl {
    public class VNode : IViewerNode, IInvalidatable {
        internal Path BoundaryPath;
        internal FrameworkElement FrameworkElementOfNodeForLabel;
        readonly Func<Edge, VEdge> funcFromDrawingEdgeToVEdge;
        internal LgNodeInfo LgNodeInfo;
        Subgraph subgraph;
        Node node;
        Border collapseButtonBorder;
        Rectangle topMarginRect;
        Path collapseSymbolPath;
        Brush collapseSymbolPathInactive = Brushes.Silver;
        bool _lowTransparency = false;
        byte _lowTransparencyVal = 80;

        internal int ZIndex {
            get {
                var geomNode = Node.GeometryNode;
                if (geomNode == null)
                    return 0;
                int ret = 0;
                do {
                    if (geomNode.ClusterParents == null)
                        return ret;
                    geomNode = geomNode.ClusterParents.FirstOrDefault();
                    if (geomNode != null)
                        ret++;
                    else
                        return ret;
                } while (true);
            }
        }

        public Node Node {
            get { return node; }
            private set {
                node = value;
                subgraph = node as Subgraph;               
            }
        }


        internal VNode(Node node, LgNodeInfo lgNodeInfo, FrameworkElement frameworkElementOfNodeForLabelOfLabel,
            Func<Edge, VEdge> funcFromDrawingEdgeToVEdge, Func<double> pathStrokeThicknessFunc)
        {
            PathStrokeThicknessFunc = pathStrokeThicknessFunc;
            LgNodeInfo = lgNodeInfo;
            Node = node;
            FrameworkElementOfNodeForLabel = frameworkElementOfNodeForLabelOfLabel;

            this.funcFromDrawingEdgeToVEdge = funcFromDrawingEdgeToVEdge;

            CreateNodeBoundaryPath();
            if (FrameworkElementOfNodeForLabel != null) {
                FrameworkElementOfNodeForLabel.Tag = this; //get a backpointer to the VNode 
                Common.PositionFrameworkElement(FrameworkElementOfNodeForLabel, node.GeometryNode.Center, 1);
                Panel.SetZIndex(FrameworkElementOfNodeForLabel, Panel.GetZIndex(BoundaryPath) + 1);
            }
            SetupSubgraphDrawing();
            Node.GeometryNode.BeforeLayoutChangeEvent += GeometryNodeBeforeLayoutChangeEvent;
            Node.Attr.VisualsChanged += (a, b) => Invalidate();         
           
        }

        internal IEnumerable<FrameworkElement> FrameworkElements {
            get {
                if (FrameworkElementOfNodeForLabel != null) yield return FrameworkElementOfNodeForLabel;
                if (BoundaryPath != null) yield return BoundaryPath;
                if (collapseButtonBorder != null) {
                    yield return collapseButtonBorder;
                    yield return topMarginRect;
                    yield return collapseSymbolPath;
                }
            }
        }

        void SetupSubgraphDrawing() {
            if (subgraph == null) return;

            SetupTopMarginBorder();
            SetupCollapseSymbol();
        }

        void SetupTopMarginBorder() {
            var cluster = (Cluster) subgraph.GeometryObject;
            topMarginRect = new Rectangle {
                Fill = Brushes.Transparent,
                Width = Node.Width,
                Height = cluster.RectangularBoundary.TopMargin
            };
            PositionTopMarginBorder(cluster);
            SetZIndexAndMouseInteractionsForTopMarginRect();
        }

        void PositionTopMarginBorder(Cluster cluster) {
            var box = cluster.BoundaryCurve.BoundingBox;

            Common.PositionFrameworkElement(topMarginRect,
                box.LeftTop + new Point(topMarginRect.Width/2, -topMarginRect.Height/2), 1);

            
        }

        void SetZIndexAndMouseInteractionsForTopMarginRect() {
            topMarginRect.MouseEnter +=
                (
                    (a, b) => {
                        collapseButtonBorder.Background = Common.BrushFromMsaglColor(subgraph.CollapseButtonColorActive);
                        collapseSymbolPath.Stroke = Brushes.Black;
                    }
                    );

            topMarginRect.MouseLeave +=
                (a, b) => {
                    collapseButtonBorder.Background = Common.BrushFromMsaglColor(subgraph.CollapseButtonColorInactive);
                    collapseSymbolPath.Stroke = Brushes.Silver;
                };
            Panel.SetZIndex(topMarginRect, int.MaxValue);
        }

        void SetupCollapseSymbol() {
            var collapseBorderSize = GetCollapseBorderSymbolSize();
            Debug.Assert(collapseBorderSize > 0);
            collapseButtonBorder = new Border {
                Background = Common.BrushFromMsaglColor(subgraph.CollapseButtonColorInactive),
                Width = collapseBorderSize,
                Height = collapseBorderSize,
                CornerRadius = new CornerRadius(collapseBorderSize/2)
            };

            Panel.SetZIndex(collapseButtonBorder, Panel.GetZIndex(BoundaryPath) + 1);


            var collapseButtonCenter = GetCollapseButtonCenter(collapseBorderSize);
            Common.PositionFrameworkElement(collapseButtonBorder, collapseButtonCenter, 1);

            double w = collapseBorderSize*0.4;
            collapseSymbolPath = new Path {
                Data = CreateCollapseSymbolPath(collapseButtonCenter + new Point(0, -w/2), w),
                Stroke = collapseSymbolPathInactive,
                StrokeThickness = 1
            };

            Panel.SetZIndex(collapseSymbolPath, Panel.GetZIndex(collapseButtonBorder) + 1);
            topMarginRect.MouseLeftButtonDown += TopMarginRectMouseLeftButtonDown;
        }


        /// <summary>
        /// </summary>
        public event Action<IViewerNode> IsCollapsedChanged;

        void InvokeIsCollapsedChanged()
        {
            if (IsCollapsedChanged != null)
                IsCollapsedChanged(this);
        }



        void TopMarginRectMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var pos = e.GetPosition(collapseButtonBorder);
            if (pos.X <= collapseButtonBorder.Width && pos.Y <= collapseButtonBorder.Height && pos.X >= 0 && pos.Y >= 0) {
                e.Handled = true;
                var cluster=(Cluster)subgraph.GeometryNode;
                cluster.IsCollapsed = !cluster.IsCollapsed;
                InvokeIsCollapsedChanged();
            }
        }

        double GetCollapseBorderSymbolSize() {
            return ((Cluster) subgraph.GeometryNode).RectangularBoundary.TopMargin -
                                        PathStrokeThickness/2 - 0.5;
        }

        Point GetCollapseButtonCenter(double collapseBorderSize) {
            var box = subgraph.GeometryNode.BoundaryCurve.BoundingBox;
            //cannot trust subgraph.GeometryNode.BoundingBox for a cluster
            double offsetFromBoundaryPath = PathStrokeThickness/2 + 0.5;
            var collapseButtonCenter = box.LeftTop + new Point(collapseBorderSize/2 + offsetFromBoundaryPath,
                -collapseBorderSize/2 - offsetFromBoundaryPath);
            return collapseButtonCenter;
        }

        
/*
        void FlipCollapsePath() {
            var size = GetCollapseBorderSymbolSize();
            var center = GetCollapseButtonCenter(size);

            if (collapsePathFlipped) {
                collapsePathFlipped = false;
                collapseSymbolPath.RenderTransform = null;
            }
            else {
                collapsePathFlipped = true;
                collapseSymbolPath.RenderTransform = new RotateTransform(180, center.X, center.Y);
            }
        }
*/

        Geometry CreateCollapseSymbolPath(Point center, double width) {
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure {StartPoint = Common.WpfPoint(center + new Point(-width, width))};

            pathFigure.Segments.Add(new System.Windows.Media.LineSegment(Common.WpfPoint(center), true));
            pathFigure.Segments.Add(
                new System.Windows.Media.LineSegment(Common.WpfPoint(center + new Point(width, width)), true));

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        double Scale() {
            return LgNodeInfo == null ? 1 : LgNodeInfo.Scale;
        }

        void GeometryNodeBeforeLayoutChangeEvent(object sender, LayoutChangeEventArgs e) {
            var newBoundaryCurve = e.DataAfterChange as ICurve;
            if (newBoundaryCurve != null) {
                //just compare the bounding boxes for the time being
                var nb = newBoundaryCurve.BoundingBox;
                var box = Node.BoundingBox;
                if (Math.Abs(nb.Width - box.Width) > 0.00001 || Math.Abs(nb.Height - box.Height) > 0.00001)
                    BoundaryCurveIsDirty = true;
            }
            else
                BoundaryCurveIsDirty = true;
        }

        internal void CreateNodeBoundaryPath() {
            if (FrameworkElementOfNodeForLabel != null) {
                // FrameworkElementOfNode.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var center = Node.GeometryNode.Center;
                var margin = 2*Node.Attr.LabelMargin;
                var bc = NodeBoundaryCurves.GetNodeBoundaryCurve(Node,
                                                                 FrameworkElementOfNodeForLabel
                                                                     .Width + margin,
                                                                 FrameworkElementOfNodeForLabel
                                                                     .Height + margin);
                bc.Translate(center);
                //                if (LgNodeInfo != null) {
                //                    //LgNodeInfo.OriginalCurveOfGeomNode = bc;
                //                    Node.GeometryNode.BoundaryCurve =
                //                        bc.Transform(PlaneTransformation.ScaleAroundCenterTransformation(LgNodeInfo.Scale,
                //                            Node.GeometryNode.Center))
                //                            .Clone();
                //                }
            }
            BoundaryPath = new Path {Data = CreatePathFromNodeBoundary(), Tag = this};
            Panel.SetZIndex(BoundaryPath, ZIndex);
            SetFillAndStroke();
            if (Node.Label != null) {
                BoundaryPath.ToolTip = this.Node.LabelText;
                if(FrameworkElementOfNodeForLabel!=null)
                    FrameworkElementOfNodeForLabel.ToolTip = this.Node.LabelText;
            }
        }

        internal Func<double> PathStrokeThicknessFunc;
        double PathStrokeThickness
        {
            get
            {
                return PathStrokeThicknessFunc != null ? PathStrokeThicknessFunc() : this.Node.Attr.LineWidth;
            }
        }

        byte GetTransparency(byte t)
        {
            if (LgNodeInfo == null)
            {
                return t;
            }
            if (LgNodeInfo.Kind == LgNodeInfoKind.FullyVisible)
            {
                return t;
            }
            
            return (byte)(t * LgNodeInfo.Scale);
        }

        void SetFillAndStroke() {
            byte trasparency = GetTransparency(Node.Attr.Color.A);
            BoundaryPath.Stroke = Common.BrushFromMsaglColor(new Drawing.Color(trasparency,Node.Attr.Color.R,Node.Attr.Color.G, Node.Attr.Color.B));
            SetBoundaryFill();
            BoundaryPath.StrokeThickness = PathStrokeThickness;

            var textBlock = FrameworkElementOfNodeForLabel as TextBlock;
            if (textBlock != null)
            {
                var col = Node.Label.FontColor;
                textBlock.Foreground = Common.BrushFromMsaglColor(new Drawing.Color(GetTransparency(col.A), col.R, col.G, col.B));
            }
           

        }

        
    void SetBoundaryFill() {
            BoundaryPath.Fill = LgNodeInfo != null && LgNodeInfo.SlidingZoomLevel == 0
                ? Brushes.Aqua
                : Common.BrushFromMsaglColor(Node.Attr.FillColor);
        }


        Geometry DoubleCircle() {
            var box = Node.BoundingBox;
            double w = box.Width;
            double h = box.Height;
            var pathGeometry = new PathGeometry();
            var r = new Rect(box.Left, box.Bottom, w, h);
            pathGeometry.AddGeometry(new EllipseGeometry(r));
            var inflation = Math.Min(5.0, Math.Min(w/3, h/3));
            r.Inflate(-inflation, -inflation);
            pathGeometry.AddGeometry(new EllipseGeometry(r));
            return pathGeometry;
        }

        Geometry CreatePathFromNodeBoundary() {
            Geometry geometry;
            switch (Node.Attr.Shape) {
                case Shape.Box:
                case Shape.House:
                case Shape.InvHouse:
                case Shape.Diamond:
                case Shape.Octagon:
                case Shape.Hexagon:

                    geometry = CreateGeometryFromMsaglCurve(Node.GeometryNode.BoundaryCurve);
                    break;

                case Shape.DoubleCircle:
                    geometry = DoubleCircle();
                    break;


                default:
                    geometry = GetEllipseGeometry();
                    break;
            }

            return geometry;
        }

        Geometry CreateGeometryFromMsaglCurve(ICurve iCurve) {
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure {
                IsClosed = true,
                IsFilled = true,
                StartPoint = Common.WpfPoint(iCurve.Start)
            };

            var curve = iCurve as Curve;
            if (curve != null) {
                AddCurve(pathFigure, curve);
            }
            else {
                var rect = iCurve as RoundedRect;
                if (rect != null)
                    AddCurve(pathFigure, rect.Curve);
                else {
                    var ellipse = iCurve as Ellipse;
                    if (ellipse != null) {
                        return new EllipseGeometry(Common.WpfPoint(ellipse.Center), ellipse.AxisA.Length,
                            ellipse.AxisB.Length);
                    }
                    var poly = iCurve as Polyline;
                    if (poly != null) {
                        var p = poly.StartPoint.Next;
                        do {
                            pathFigure.Segments.Add(new System.Windows.Media.LineSegment(Common.WpfPoint(p.Point),
                                true));

                            p = p.NextOnPolyline;
                        } while (p != poly.StartPoint);
                    }
                }
            }


            pathGeometry.Figures.Add(pathFigure);

            return pathGeometry;
        }


        static void AddCurve(PathFigure pathFigure, Curve curve) {
            foreach (ICurve seg in curve.Segments) {
                var ls = seg as LineSegment;
                if (ls != null)
                    pathFigure.Segments.Add(new System.Windows.Media.LineSegment(Common.WpfPoint(ls.End), true));
                else {
                    var ellipse = seg as Ellipse;
                    pathFigure.Segments.Add(new ArcSegment(Common.WpfPoint(ellipse.End),
                        new Size(ellipse.AxisA.Length, ellipse.AxisB.Length),
                        Point.Angle(new Point(1, 0), ellipse.AxisA),
                        ellipse.ParEnd - ellipse.ParEnd >= Math.PI,
                        !ellipse.OrientedCounterclockwise()
                            ? SweepDirection.Counterclockwise
                            : SweepDirection.Clockwise, true));
                }
            }
        }

        Geometry GetEllipseGeometry() {
            return new EllipseGeometry(Common.WpfPoint(Node.BoundingBox.Center), Node.BoundingBox.Width/2,
                Node.BoundingBox.Height/2);
        }

        #region Implementation of IViewerObject

        public DrawingObject DrawingObject {
            get { return Node; }
        }

        public bool MarkedForDragging { get; set; }
        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;

        #endregion

        public IEnumerable<IViewerEdge> InEdges {
            get { foreach (var e in Node.InEdges) yield return funcFromDrawingEdgeToVEdge(e); }
        }

        public IEnumerable<IViewerEdge> OutEdges {
            get { foreach (var e in Node.OutEdges) yield return funcFromDrawingEdgeToVEdge(e); }
        }

        public IEnumerable<IViewerEdge> SelfEdges {
            get { foreach (var e in Node.SelfEdges) yield return funcFromDrawingEdgeToVEdge(e); }
        }

        public void SetStrokeFill() {
            throw new NotImplementedException();
        }




        public void Invalidate() {
            if (!Node.IsVisible) {
                foreach (var fe in FrameworkElements)
                    fe.Visibility = Visibility.Hidden;
                return;
            }

            if (BoundaryCurveIsDirty) {
                BoundaryPath.Data = CreatePathFromNodeBoundary();
                BoundaryCurveIsDirty = false;
            }

            if (LgNodeInfo != null)
            {
                double scale = 1;// LgNodeInfo != null && LgNodeInfo.Kind == LgNodeInfoKind.Satellite
                                  // ? LgNodeInfo.Scale
                                  // : 1;
                var planeTransform = PlaneTransformation.ScaleAroundCenterTransformation(scale,
                                                                                         node.BoundingBox.Center);
                var transform = new MatrixTransform(planeTransform[0, 0], planeTransform[0, 1],
                                                    planeTransform[1, 0], planeTransform[1, 1],
                                                    planeTransform[0, 2], planeTransform[1, 2]);
                BoundaryPath.RenderTransform = transform;
                
                if (FrameworkElementOfNodeForLabel != null)
                    Common.PositionFrameworkElement(FrameworkElementOfNodeForLabel, node.GeometryNode.Center,
                                                    scale);

            }
            else
                Common.PositionFrameworkElement(FrameworkElementOfNodeForLabel, Node.BoundingBox.Center, 1);
            

            SetFillAndStroke();
            if (subgraph == null) return;
            PositionTopMarginBorder((Cluster) subgraph.GeometryNode);
            double collapseBorderSize = GetCollapseBorderSymbolSize();
            var collapseButtonCenter = GetCollapseButtonCenter(collapseBorderSize);
            Common.PositionFrameworkElement(collapseButtonBorder, collapseButtonCenter, 1);
            double w = collapseBorderSize*0.4;
            collapseSymbolPath.Data = CreateCollapseSymbolPath(collapseButtonCenter + new Point(0, -w/2), w);
            collapseSymbolPath.RenderTransform = ((Cluster) subgraph.GeometryNode).IsCollapsed
                                                     ? new RotateTransform(180, collapseButtonCenter.X,
                                                                           collapseButtonCenter.Y)
                                                     : null;

            topMarginRect.Visibility =
                collapseSymbolPath.Visibility =
                collapseButtonBorder.Visibility = Visibility.Visible;

        }

        public override string ToString() {
            return Node.Id;
        }

        protected bool BoundaryCurveIsDirty { get; set; }


        internal void DetouchFromCanvas(Canvas graphCanvas) {
            if (BoundaryPath != null)
                graphCanvas.Children.Remove(BoundaryPath);
            if (FrameworkElementOfNodeForLabel != null)
                graphCanvas.Children.Remove(FrameworkElementOfNodeForLabel);
        }


        byte Low(byte b)
        {
            return (byte)(b/3);
        }

        internal void SetLowTransparency()
        {
            _lowTransparency = true;
            if (BoundaryPath != null)
            {
                var col = Node.Attr.Color;
                BoundaryPath.Stroke = Common.BrushFromMsaglColor(new Drawing.Color(Low(col.A), Low(col.R), Low(col.G), Low(col.B)));
            }
            var textBlock = FrameworkElementOfNodeForLabel as TextBlock;
            if (textBlock != null)
            {
                var col = Node.Label.FontColor;
                textBlock.Foreground = Common.BrushFromMsaglColor(new Drawing.Color(Low(col.A), Low(col.R), Low(col.G), Low(col.B)));
            }
           
        }

    }
}