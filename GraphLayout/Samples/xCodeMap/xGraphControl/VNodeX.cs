using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Ellipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using Node = Microsoft.Msagl.Drawing.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Shape = Microsoft.Msagl.Drawing.Shape;
using Size = System.Windows.Size;

namespace xCodeMap.xGraphControl
{
    
    internal class XNode : IViewerNodeX, IInvalidatable {
        internal Path BoundaryPath;        
        internal List<XEdge> inEdges = new List<XEdge>();
        internal List<XEdge> outEdges = new List<XEdge>();
        internal List<XEdge> selfEdges = new List<XEdge>();

        public LgNode LgNode { get; set; }

        private FrameworkElement _visualObject;
        public FrameworkElement VisualObject
        {
            get { return _visualObject; }
        }
        
        public Node Node { get; private set; }
        private string _category;
        
        public XNode(Node node, string category = null)
        {
            Node = node;
            _category = category;

            Border b = new Border();
            double size = node.Label.Text.Length * 9;
            b.Width = size + 12;
            b.Height = size * 2 / 3 + 4;
            _visualObject = b;

            Brush strokeBrush = CommonX.BrushFromMsaglColor(Node.Attr.Color);
            if (category != null)
            {                
                Brush brush = Categories.GetBrush(_category);
                if (brush != null) strokeBrush = brush;
            }
            
            BoundaryPath = new Path {
                //Data = CreatePathFromNodeBoundary(),
                Stroke = strokeBrush, 
                Fill = CommonX.BrushFromMsaglColor(Node.Attr.FillColor),
                StrokeThickness = Node.Attr.LineWidth
            };

            Node.Attr.LineWidthHasChanged += AttrLineWidthHasChanged;
            //Node.Attr.GeometryNode.LayoutChangeEvent += GeometryNodeBeforeLayoutChangeEvent;
            
        }

	    double Scale() {
            return LgNode == null ? 1 : LgNode.Scale;
        }

        public void GeometryNodeBeforeLayoutChangeEvent(object sender, LayoutChangeEventArgs e) {
            var newBoundaryCurve = e.DataAfterChange as ICurve;
            if (newBoundaryCurve != null) {
                //just compare the bounding boxes for the time being
                var nb = newBoundaryCurve.BoundingBox;
                var box = Node.BoundingBox;
                if (Math.Abs(nb.Width - box.Width) > 0.00001 || Math.Abs(nb.Height - box.Height) > 0.00001)
                    BoundaryCurveIsDirty = true;
            } else
                BoundaryCurveIsDirty = true;

        }

        void PositionPath(Rectangle box) {
            Canvas.SetLeft(BoundaryPath, box.Left + box.Width/2);
            Canvas.SetTop(BoundaryPath, box.Bottom + box.Height/2);
        }


        void AttrLineWidthHasChanged(object sender, EventArgs e) {
            BoundaryPath.StrokeThickness = Node.Attr.LineWidth;
        }


        Geometry DoubleCircle() {
            double w = Node.BoundingBox.Width;
            double h = Node.BoundingBox.Height;
            var pathGeometry = new PathGeometry();
            var r = new Rect(-w/2, -h/2, w, h);
            pathGeometry.AddGeometry(new EllipseGeometry(r));
            r.Inflate(-5, -5);
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
                    geometry = CreateGeometryFromMsaglCurve(Node.Attr.GeometryNode.BoundaryCurve);
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
            var pathFigure = new PathFigure {IsClosed = true, IsFilled = true};

            Point c = Node.Attr.GeometryNode.Center;
            //we need to move the center to the origin, because the node position is later shifted to the center

            pathFigure.StartPoint = CommonX.WpfPoint(iCurve.Start - c);
            var curve = iCurve as Curve;
            if (curve != null) {
                AddCurve(pathFigure, c, curve);
            }
            else {
                var rect = iCurve as RoundedRect;
                if (rect != null)
                    AddCurve(pathFigure, c, rect.Curve);
                else
                    throw new Exception();
            }

            pathGeometry.Figures.Add(pathFigure);

            return pathGeometry;
        }


        static void AddCurve(PathFigure pathFigure, Point c, Curve curve) {
            foreach (ICurve seg in curve.Segments) {
                var ls = seg as LineSegment;
                if (ls != null)
                    pathFigure.Segments.Add(new System.Windows.Media.LineSegment(CommonX.WpfPoint(ls.End - c), true));
                else {
                    var ellipse = seg as Ellipse;
                    pathFigure.Segments.Add(new ArcSegment(CommonX.WpfPoint(ellipse.End - c),
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
            return new EllipseGeometry(new System.Windows.Point(0, 0), Node.BoundingBox.Width/2,
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
            get { return inEdges; }
        }

        public IEnumerable<IViewerEdge> OutEdges {
            get { return outEdges; }
        }

        public IEnumerable<IViewerEdge> SelfEdges {
            get { return selfEdges; }
        }
        public void SetStrokeFill() {
            throw new NotImplementedException();
        }

        public void AddPort(Port port) {
            
        }

        public void RemovePort(Port port) {
          
        }

        private TextBlock _vTitle;
        private Image _vIcon;

        public void Invalidate(double scale = 1)
        {
            if (BoundaryCurveIsDirty) {
                BoundaryPath.Data = CreatePathFromNodeBoundary();
                BoundaryCurveIsDirty = false;
            }

            PositionPath(Node.BoundingBox);

            var node_scale = Scale();
            if (_visualObject != null) {
                if (node_scale < 0.5)
                {
                    if (_visualObject.Visibility != Visibility.Hidden)
                    {
                        _visualObject.Visibility = Visibility.Hidden;
                        BoundaryPath.Fill = BoundaryPath.Stroke;
                    }
                }
                else
                {
                    if (_visualObject.Visibility != Visibility.Visible)
                    {
                        _visualObject.Visibility = Visibility.Visible;
                        BoundaryPath.Fill = CommonX.BrushFromMsaglColor(Node.Attr.FillColor);
                    }

                    if (_vTitle == null)
                    {
                        Grid g = new Grid();
                        ((Border)_visualObject).Child = g;

                        g.Margin = new Thickness(4);
                        g.VerticalAlignment = VerticalAlignment.Center;
                        g.HorizontalAlignment = HorizontalAlignment.Center;
                        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        _vTitle = new TextBlock { Text = Node.Label.Text, Name = "Title" };
                        _vTitle.VerticalAlignment = VerticalAlignment.Center;
                        g.Children.Add(_vTitle);
                        Grid.SetColumn(_vTitle, 1);

                        if (_category != null)
                        {
                            ImageSource src = Categories.GetIcon(_category);
                            if (src != null) {
                                _vIcon = new Image { Source = src };
                                _vIcon.VerticalAlignment = VerticalAlignment.Center;
                                RenderOptions.SetBitmapScalingMode(_vIcon, BitmapScalingMode.HighQuality);
                                g.Children.Add(_vIcon);
                            }
                        }
                    }

                    double fontSize = Math.Min(12 / (node_scale * scale), 12);
                    _vTitle.FontSize = fontSize;
                    if (_vIcon != null) _vIcon.Width = fontSize * 2;

                    CommonX.PositionElement(_visualObject, Node.BoundingBox.Center, node_scale);
                }
            }
        }

        public override string ToString() {
            return Node.Id;
        }
        protected bool BoundaryCurveIsDirty { get; set; }

        internal double BorderPathThickness 
        {
            set
            {
                BoundaryPath.StrokeThickness = value;
            }
        }
    }


    internal static class Categories
    {

        internal static Brush GetBrush(string category)
        {
            switch (category)
            {
                case "Class": return Brushes.DarkRed;
                case "Method": return Brushes.MediumVioletRed;
                case "Property": return Brushes.DimGray;
                case "Field": return Brushes.MidnightBlue;
                default: return null;
            }
        }

        internal static ImageSource GetIcon(string category)
        {
            switch (category)
            {
                case "Class":
                case "Method":
                case "Property":
                case "Field":
                case "Interface":
                case "Namespace":
                case "Delegate":
                case "Event":
                case "Solution":
                    return new BitmapImage(new Uri("Images/Icon_" + category + ".png", UriKind.RelativeOrAbsolute));
                default: return null;
            }
        }
    }
}