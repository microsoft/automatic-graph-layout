using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Ellipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using Node = Microsoft.Msagl.Drawing.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Polyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
using Shape = Microsoft.Msagl.Drawing.Shape;
using FrameworkElement = Windows.UI.Xaml.FrameworkElement;
using Border = Windows.UI.Xaml.Controls.Border;
using Panel = Windows.UI.Xaml.Controls.Canvas;
using TextBlock = Windows.UI.Xaml.Controls.TextBlock;
using ToolTipService = Windows.UI.Xaml.Controls.ToolTipService;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;
using Path = Windows.UI.Xaml.Shapes.Path;
using Brush = Windows.UI.Xaml.Media.Brush;
using Geometry = Windows.UI.Xaml.Media.Geometry;
using EllipseGeometry = Windows.UI.Xaml.Media.EllipseGeometry;
using PathGeometry = Windows.UI.Xaml.Media.PathGeometry;
using PathFigure = Windows.UI.Xaml.Media.PathFigure;
using RotateTransform = Windows.UI.Xaml.Media.RotateTransform;
using SweepDirection = Windows.UI.Xaml.Media.SweepDirection;
using ArcSegment = Windows.UI.Xaml.Media.ArcSegment;
using CornerRadius = Windows.UI.Xaml.CornerRadius;
using SolidColorBrush = Windows.UI.Xaml.Media.SolidColorBrush;
using Colors = Windows.UI.Colors;
using Size = Windows.Foundation.Size;
using WPoint = Windows.Foundation.Point;
using Visibility = Windows.UI.Xaml.Visibility;
using PointerRoutedEventArgs = Windows.UI.Xaml.Input.PointerRoutedEventArgs;
using Canvas = Windows.UI.Xaml.Controls.Canvas;
using System;

namespace Microsoft.Msagl.Viewers.Uwp {
    public class VNode : IViewerNode {
        internal Path BoundaryPath;
        internal FrameworkElement FrameworkElementOfNodeForLabel;
        readonly Func<Edge, VEdge> _funcFromDrawingEdgeToVEdge;
        Subgraph _subgraph;
        Node _node;
        Border _collapseButtonBorder;
        Rectangle _topMarginRect;
        Path _collapseSymbolPath;
        readonly Brush _collapseSymbolPathInactive = new SolidColorBrush(Colors.Silver);

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
            get { return _node; }
            private set {
                _node = value;
                _subgraph = _node as Subgraph;
            }
        }


        internal VNode(Node node, FrameworkElement frameworkElementOfNodeForLabelOfLabel,
            Func<Edge, VEdge> funcFromDrawingEdgeToVEdge, Func<double> pathStrokeThicknessFunc) {
            PathStrokeThicknessFunc = pathStrokeThicknessFunc;
            Node = node;
            FrameworkElementOfNodeForLabel = frameworkElementOfNodeForLabelOfLabel;

            _funcFromDrawingEdgeToVEdge = funcFromDrawingEdgeToVEdge;

            CreateNodeBoundaryPath();
            if (FrameworkElementOfNodeForLabel != null) {
                FrameworkElementOfNodeForLabel.Tag = this; //get a backpointer to the VNode
                Common.PositionFrameworkElement(FrameworkElementOfNodeForLabel, node.GeometryNode.Center, 1);
                Panel.SetZIndex(FrameworkElementOfNodeForLabel, Panel.GetZIndex(BoundaryPath) + 1);
            }
            SetupSubgraphDrawing();
            Node.Attr.VisualsChanged += (a, b) => Invalidate();
            Node.IsVisibleChanged += obj => {
                foreach (var frameworkElement in FrameworkElements)
                    frameworkElement.Opacity = Node.IsVisible ? 1 : 0;
            };
        }

        internal IEnumerable<FrameworkElement> FrameworkElements {
            get {
                if (FrameworkElementOfNodeForLabel != null) yield return FrameworkElementOfNodeForLabel;
                if (BoundaryPath != null) yield return BoundaryPath;
                if (_collapseButtonBorder != null) {
                    yield return _collapseButtonBorder;
                    yield return _topMarginRect;
                    yield return _collapseSymbolPath;
                }
            }
        }

        void SetupSubgraphDrawing() {
            if (_subgraph == null) return;

            SetupTopMarginBorder();
            SetupCollapseSymbol();
        }

        void SetupTopMarginBorder() {
            var cluster = (Cluster)_subgraph.GeometryObject;
            _topMarginRect = new Rectangle {
                Fill = new SolidColorBrush(Colors.Transparent),
                Width = Node.Width,
                Height = cluster.RectangularBoundary.TopMargin
            };
            PositionTopMarginBorder(cluster);
            SetZIndexAndMouseInteractionsForTopMarginRect();
        }

        void PositionTopMarginBorder(Cluster cluster) {
            var box = cluster.BoundaryCurve.BoundingBox;

            Common.PositionFrameworkElement(_topMarginRect,
                box.LeftTop + new Point(_topMarginRect.Width / 2, -_topMarginRect.Height / 2), 1);
        }

        void SetZIndexAndMouseInteractionsForTopMarginRect() {
            _topMarginRect.PointerEntered +=
                    (a, b) => {
                        _collapseButtonBorder.Background =
                            Common.BrushFromMsaglColor(_subgraph.CollapseButtonColorActive);
                        _collapseSymbolPath.Stroke = new SolidColorBrush(Colors.Black);
                    };

            _topMarginRect.PointerReleased +=
                (a, b) => {
                    _collapseButtonBorder.Background = Common.BrushFromMsaglColor(_subgraph.CollapseButtonColorInactive);
                    _collapseSymbolPath.Stroke = new SolidColorBrush(Colors.Silver);
                };
            Panel.SetZIndex(_topMarginRect, int.MaxValue);
        }

        void SetupCollapseSymbol() {
            var collapseBorderSize = GetCollapseBorderSymbolSize();
            Debug.Assert(collapseBorderSize > 0);
            _collapseButtonBorder = new Border {
                Background = Common.BrushFromMsaglColor(_subgraph.CollapseButtonColorInactive),
                Width = collapseBorderSize,
                Height = collapseBorderSize,
                CornerRadius = new CornerRadius(collapseBorderSize / 2)
            };

            Panel.SetZIndex(_collapseButtonBorder, Panel.GetZIndex(BoundaryPath) + 1);


            var collapseButtonCenter = GetCollapseButtonCenter(collapseBorderSize);
            Common.PositionFrameworkElement(_collapseButtonBorder, collapseButtonCenter, 1);

            double w = collapseBorderSize * 0.4;
            _collapseSymbolPath = new Path {
                Data = CreateCollapseSymbolPath(collapseButtonCenter + new Point(0, -w / 2), w),
                Stroke = _collapseSymbolPathInactive,
                StrokeThickness = 1
            };

            Panel.SetZIndex(_collapseSymbolPath, Panel.GetZIndex(_collapseButtonBorder) + 1);
            _topMarginRect.PointerPressed += (s, e) =>
                     TopMarginRectMouseLeftButtonDown(e);

        }


        /// <summary>
        /// </summary>
        public event Action<IViewerNode> IsCollapsedChanged;

        void InvokeIsCollapsedChanged() {
            if (IsCollapsedChanged != null)
                IsCollapsedChanged(this);
        }

        void TopMarginRectMouseLeftButtonDown(PointerRoutedEventArgs e) {
            var point = e.GetCurrentPoint(_collapseButtonBorder);
            if (point.Properties.IsLeftButtonPressed) return;
            if (point.Position.X <= _collapseButtonBorder.Width && point.Position.Y <= _collapseButtonBorder.Height && point.Position.X >= 0 &&
                point.Position.Y >= 0) {
                e.Handled = true;
                var cluster = (Cluster)_subgraph.GeometryNode;
                cluster.IsCollapsed = !cluster.IsCollapsed;
                InvokeIsCollapsedChanged();
            }
        }

        double GetCollapseBorderSymbolSize() {
            return ((Cluster)_subgraph.GeometryNode).RectangularBoundary.TopMargin -
                   PathStrokeThickness / 2 - 0.5;
        }

        Point GetCollapseButtonCenter(double collapseBorderSize) {
            var box = _subgraph.GeometryNode.BoundaryCurve.BoundingBox;
            //cannot trust subgraph.GeometryNode.BoundingBox for a cluster
            double offsetFromBoundaryPath = PathStrokeThickness / 2 + 0.5;
            var collapseButtonCenter = box.LeftTop + new Point(collapseBorderSize / 2 + offsetFromBoundaryPath,
                -collapseBorderSize / 2 - offsetFromBoundaryPath);
            return collapseButtonCenter;
        }



        Geometry CreateCollapseSymbolPath(Point center, double width) {
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = Common.WpfPoint(center + new Point(-width, width)) };

            pathFigure.Segments.Add(new Windows.UI.Xaml.Media.LineSegment { Point = Common.WpfPoint(center) });
            pathFigure.Segments.Add(
                new Windows.UI.Xaml.Media.LineSegment { Point = Common.WpfPoint(center + new Point(width, width)) });

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        internal void CreateNodeBoundaryPath() {
            if (FrameworkElementOfNodeForLabel != null) {
                var center = Node.GeometryNode.Center;
                var margin = 2 * Node.Attr.LabelMargin;
                var bc = NodeBoundaryCurves.GetNodeBoundaryCurve(Node,
                    FrameworkElementOfNodeForLabel
                        .Width + margin,
                    FrameworkElementOfNodeForLabel
                        .Height + margin);
                bc.Translate(center);
            }
            BoundaryPath = new Path { Data = CreatePathFromNodeBoundary(), Tag = this };
            Panel.SetZIndex(BoundaryPath, ZIndex);
            SetFillAndStroke();
            if (Node.Label != null) {
                ToolTipService.SetToolTip(BoundaryPath, Node.LabelText);
                if (FrameworkElementOfNodeForLabel != null)
                    ToolTipService.SetToolTip(FrameworkElementOfNodeForLabel, Node.LabelText);
            }
        }

        internal Func<double> PathStrokeThicknessFunc;

        double PathStrokeThickness {
            get { return PathStrokeThicknessFunc != null ? PathStrokeThicknessFunc() : Node.Attr.LineWidth; }
        }

        byte GetTransparency(byte t) {
            return t;
        }

        void SetFillAndStroke() {
            byte trasparency = GetTransparency(Node.Attr.Color.A);
            BoundaryPath.Stroke =
                Common.BrushFromMsaglColor(new Drawing.Color(trasparency, Node.Attr.Color.R, Node.Attr.Color.G,
                    Node.Attr.Color.B));
            SetBoundaryFill();
            BoundaryPath.StrokeThickness = PathStrokeThickness;

            var textBlock = FrameworkElementOfNodeForLabel as TextBlock;
            if (textBlock != null) {
                var col = Node.Label.FontColor;
                textBlock.Foreground =
                    Common.BrushFromMsaglColor(new Drawing.Color(GetTransparency(col.A), col.R, col.G, col.B));
            }
        }

        void SetBoundaryFill() {
            BoundaryPath.Fill = Common.BrushFromMsaglColor(Node.Attr.FillColor);
        }

        Geometry DoubleCircle() {
            var box = Node.BoundingBox;
            double w = box.Width;
            double h = box.Height;
            var t = box.Top;
            var l = box.Left;
            var i = Math.Min(5.0, Math.Min(w / 3, h / 3));
            var w2 = w / 2;
            var lw2 = l + w2;
            var h2 = h / 2;
            return new PathGeometry {
                    Figures = {
                        new PathFigure
                        {
                            StartPoint = new WPoint(lw2,t),
                            IsClosed = true,
                            Segments = {
                                new ArcSegment
                                {
                                    IsLargeArc = false,
                                    Size = new Size(w2, h2),
                                    Point = new WPoint(lw2, t+h)
                                },
                                new ArcSegment
                                {
                                    IsLargeArc = false,
                                    Size = new Size(w2 , h2),
                                    Point = new WPoint(lw2, t),
                                }
                            }
                        },
                        new PathFigure
                        {
                            StartPoint = new WPoint(lw2,t-i),
                            IsClosed = true,
                            Segments = {
                                new ArcSegment
                                {
                                    IsLargeArc = false,
                                    Size = new Size(w2+i, h2+i),
                                    Point = new WPoint(lw2, t+h+i)
                                },
                                new ArcSegment
                                {
                                    IsLargeArc = false,
                                    Size = new Size(w2+i , h2+i),
                                    Point = new WPoint(lw2, t-i),
                                }
                            }
                        }
                    }              
            };
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
                        return new EllipseGeometry {
                            Center = Common.WpfPoint(ellipse.Center),
                            RadiusX = ellipse.AxisA.Length,
                            RadiusY = ellipse.AxisB.Length
                        };
                    }
                    var poly = iCurve as Polyline;
                    if (poly != null) {
                        var p = poly.StartPoint.Next;
                        do {
                            pathFigure.Segments.Add(new Windows.UI.Xaml.Media.LineSegment { Point = Common.WpfPoint(p.Point) });
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
                    pathFigure.Segments.Add(new Windows.UI.Xaml.Media.LineSegment { Point = Common.WpfPoint(ls.End) });
                else {
                    var ellipse = seg as Ellipse;
                    if (ellipse != null)
                        pathFigure.Segments.Add(new ArcSegment {
                            Point = Common.WpfPoint(ellipse.End),
                            Size = new Size(ellipse.AxisA.Length, ellipse.AxisB.Length),
                            RotationAngle = Point.Angle(new Point(1, 0), ellipse.AxisA),
                            IsLargeArc = ellipse.ParEnd - ellipse.ParEnd >= Math.PI,
                            SweepDirection = !ellipse.OrientedCounterclockwise()
                                ? SweepDirection.Counterclockwise
                                : SweepDirection.Clockwise
                        });
                }
            }
        }

        Geometry GetEllipseGeometry() => new EllipseGeometry {
            Center = Common.WpfPoint(Node.BoundingBox.Center),
            RadiusX = Node.BoundingBox.Width / 2,
            RadiusY = Node.BoundingBox.Height / 2
        };


        #region Implementation of IViewerObject

        public DrawingObject DrawingObject {
            get { return Node; }
        }

        bool markedForDragging;

        /// <summary>
        /// Implements a property of an interface IEditViewer
        /// </summary>
        public bool MarkedForDragging {
            get {
                return markedForDragging;
            }
            set {
                markedForDragging = value;
                if (value) {
                    MarkedForDraggingEvent?.Invoke(this, null);
                }
                else {
                    UnmarkedForDraggingEvent?.Invoke(this, null);
                }
            }
        }

        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;

        #endregion

        public IEnumerable<IViewerEdge> InEdges => Node.InEdges.Select(e => _funcFromDrawingEdgeToVEdge(e));
        public IEnumerable<IViewerEdge> OutEdges => Node.OutEdges.Select(e => _funcFromDrawingEdgeToVEdge(e));
        public IEnumerable<IViewerEdge> SelfEdges => Node.SelfEdges.Select(e => _funcFromDrawingEdgeToVEdge(e));
        public void Invalidate() {
            if (!Node.IsVisible) {
                foreach (var fe in FrameworkElements)
                    fe.Opacity = 0;
                return;
            }

            BoundaryPath.Data = CreatePathFromNodeBoundary();

            Common.PositionFrameworkElement(FrameworkElementOfNodeForLabel, Node.BoundingBox.Center, 1);


            SetFillAndStroke();
            if (_subgraph == null) return;
            PositionTopMarginBorder((Cluster)_subgraph.GeometryNode);
            double collapseBorderSize = GetCollapseBorderSymbolSize();
            var collapseButtonCenter = GetCollapseButtonCenter(collapseBorderSize);
            Common.PositionFrameworkElement(_collapseButtonBorder, collapseButtonCenter, 1);
            double w = collapseBorderSize * 0.4;
            _collapseSymbolPath.Data = CreateCollapseSymbolPath(collapseButtonCenter + new Point(0, -w / 2), w);
            _collapseSymbolPath.RenderTransform = ((Cluster)_subgraph.GeometryNode).IsCollapsed
                ? new RotateTransform {
                    Angle = 180,
                    CenterX = collapseButtonCenter.X,
                    CenterY = collapseButtonCenter.Y
                }
                : null;

            _topMarginRect.Visibility =
                _collapseSymbolPath.Visibility =
                    _collapseButtonBorder.Visibility = Visibility.Visible;

        }

        public override string ToString() {
            return Node.Id;
        }

        internal void DetouchFromCanvas(Canvas graphCanvas) {
            if (BoundaryPath != null)
                graphCanvas.Children.Remove(BoundaryPath);
            if (FrameworkElementOfNodeForLabel != null)
                graphCanvas.Children.Remove(FrameworkElementOfNodeForLabel);
        }
    }
}