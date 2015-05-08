using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Animation;
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
using Microsoft.VisualStudio.GraphModel;
using System.Globalization;

namespace xCodeMap.xGraphControl
{
    
    internal class XNode : IViewerNodeX, IInvalidatable {
        internal Path BoundaryPath;
        internal List<XEdge> inEdges = new List<XEdge>();
        internal List<XEdge> outEdges = new List<XEdge>();
        internal List<XEdge> selfEdges = new List<XEdge>();

        public LgNodeInfo LgNodeInfo { get; set; }

        private LevelOfDetailsContainer _visualObject;
        public FrameworkElement VisualObject
        {
            get { return _visualObject; }
        }
        
        public Node Node { get; private set; }
        private GraphNode _vsGraphNodeInfo;
        private string _category;
        private Brush _fill;
        
        public XNode(Node node, GraphNode gnode = null)
        {
            Node = node;
            _vsGraphNodeInfo = gnode;

            _visualObject = new LevelOfDetailsContainer();

            Brush strokeBrush = CommonX.BrushFromMsaglColor(Node.Attr.Color);
            _fill = CommonX.BrushFromMsaglColor(Node.Attr.FillColor);
            if (gnode != null)
            {                
                if (gnode.Categories.Count() > 0)
                {
                    _category = gnode.Categories.ElementAt(0).ToString().Replace("CodeSchema_", "");

                    _fill = NodeCategories.GetFill(_category);
                    Brush brush = NodeCategories.GetStroke(_category);
                    if (brush != null) strokeBrush = brush;
                }
            }
            
            BoundaryPath = new Path {
                //Data = CreatePathFromNodeBoundary(),
                Stroke = strokeBrush,
                Fill = _fill,
                StrokeThickness = Node.Attr.LineWidth,
                Tag = this
            };
            BoundaryCurveIsDirty = true;

            Node.Attr.VisualsChanged += AttrLineWidthHasChanged;
            //Node.Attr.GeometryNode.LayoutChangeEvent += GeometryNodeBeforeLayoutChangeEvent;
            
        }


	    double Scale() {
            return LgNodeInfo == null ? 1 : LgNodeInfo.Scale;
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


        void AttrLineWidthHasChanged(object sender, EventArgs e) {
            BoundaryPath.StrokeThickness = Node.Attr.LineWidth;
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
                                                StartPoint = CommonX.WpfPoint(iCurve.Start)
                                            };

            var curve = iCurve as Curve;
            if (curve != null) {
                AddCurve(pathFigure, curve);
            }
            else {
                var rect = iCurve as RoundedRect;
                if (rect != null)
                    AddCurve(pathFigure, rect.Curve);
                else
                    throw new Exception();
            }

            pathGeometry.Figures.Add(pathFigure);

            return pathGeometry;
        }


        static void AddCurve(PathFigure pathFigure, Curve curve) {
            foreach (ICurve seg in curve.Segments) {
                var ls = seg as LineSegment;
                if (ls != null)
                    pathFigure.Segments.Add(new System.Windows.Media.LineSegment(CommonX.WpfPoint(ls.End), true));
                else {
                    var ellipse = seg as Ellipse;
                    pathFigure.Segments.Add(new ArcSegment(CommonX.WpfPoint(ellipse.End),
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
            return new EllipseGeometry( CommonX.WpfPoint(Node.BoundingBox.Center), Node.BoundingBox.Width/2,
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
        
        public void Invalidate(double scale = 1)
        {
            if (BoundaryCurveIsDirty) {
                BoundaryPath.Data = CreatePathFromNodeBoundary();
                BoundaryCurveIsDirty = false;
            }

           
            double node_scale = Scale();
            Rectangle bounds = LgNodeInfo.OriginalCurveOfGeomNode.BoundingBox;
            Size real_size = new Size(bounds.Width * node_scale * scale,
                                    bounds.Height * node_scale * scale);
            
            if (node_scale < 0.5 && (real_size.Width<_fontSize || real_size.Height<_fontSize))
            {
                _visualObject.LevelOfDetail = 0;
            }
            else
            {
                if (_visualObject.MaxLevelOfDetail == 0)    
                {
                    // First time becoming visible: generate visuals
                    InitiateContainer();
                    Visual visual;
                    Rectangle rect;

                    rect = CreateIcon(new Point(), out visual);
                    _visualObject.AddDetail(visual, rect);

                    rect = CreateTitle(rect.RightBottom, out visual);
                    _visualObject.AddDetail(visual, rect);

                    rect = CreateDescription(new Point(rect.Center.X, rect.Top), out visual);
                    _visualObject.AddDetail(visual, rect);
                }
                _visualObject.LevelOfDetail = _visualObject.MeasureLevelOfDetail(real_size);

                CommonX.PositionElement(_visualObject, _visualObject.BoundingBox, Node.BoundingBox, 1 / scale);
            }
        }

        private double _fontSize = 12;

        private void InitiateContainer()
        {
            _visualObject.ToolTip = _category + " " + Node.LabelText;

            _visualObject.MouseEnter += (o, e) => { BoundaryPath.Fill = Brushes.Gold; };
            _visualObject.MouseLeave += (o, e) => { BoundaryPath.Fill = _fill; };

            if (LgNodeInfo.GeometryNode is Cluster)
            {
                _fontSize *= 1.25;
                _visualObject.VerticalAlignment = VerticalAlignment.Top;
                _visualObject.Margin = new Thickness(0, -_fontSize / 2, 0, -_fontSize / 2);
            }
        }

        private TextBlock _textMeasurer = new TextBlock { FontFamily = new FontFamily("Calibri") };

        private Rectangle CreateIcon(Point origin, out Visual visual)
        {
            if (_category != null)
            {
                ImageSource src = NodeCategories.GetIcon(_category);
                if (src != null)
                {
                    DrawingVisual icon = new DrawingVisual();
                    icon.CacheMode = new BitmapCache(1);
                    DrawingContext context = icon.RenderOpen();
                    context.DrawImage(src, new Rect(origin.X, origin.Y, _fontSize, _fontSize));
                    context.Close();

                    visual = icon;
                    return new Rectangle(origin.X, origin.Y, _fontSize, _fontSize);
                }
            }
            visual = null;
            return new Rectangle();
        }

        private Rectangle CreateTitle(Point origin, out Visual visual)
        {            
            DrawingVisual title = new DrawingVisual();
            DrawingContext context = title.RenderOpen();
            FormattedText fText = new FormattedText(Node.LabelText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Calibri"), _fontSize, Brushes.Black);
            context.DrawText(fText, CommonX.WpfPoint(origin));
            context.Close();
            visual = title;

            _textMeasurer.FontSize = _fontSize;
            _textMeasurer.Text = Node.LabelText;

            Size size = CommonX.Measure(_textMeasurer);
            return new Rectangle(origin.X, origin.Y, origin.X + size.Width, origin.Y + size.Height);
        }

        private Rectangle CreateDescription(Point origin, out Visual visual)
        {
            if (_vsGraphNodeInfo != null)
            {
                DrawingVisual desc = new DrawingVisual();
                DrawingContext context = desc.RenderOpen();

                string properties = "";
                foreach (KeyValuePair<GraphProperty, object> kvp in _vsGraphNodeInfo.Properties)
                {
                    string name = kvp.Key.ToString();
                    bool value = (kvp.Value is bool && ((bool)kvp.Value) == true);
                    if (name.StartsWith("CodeSchemaProperty_Is") && value)
                    {
                        properties += (properties.Length > 0 ? " : " : "") + name.Replace("CodeSchemaProperty_Is", "");
                    }
                }

                FormattedText fText = new FormattedText(properties, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Calibri"), _fontSize * 0.8, Brushes.Black);
                fText.SetFontStyle(FontStyles.Italic);
                fText.TextAlignment = TextAlignment.Center;

                context.DrawText(fText, CommonX.WpfPoint(origin));
                context.Close();

                _textMeasurer.FontSize = _fontSize * 0.8;
                _textMeasurer.Text = properties;
                Size size = CommonX.Measure(_textMeasurer);

                visual = desc;
                return new Rectangle(origin.X-size.Width/2, origin.Y, origin.X+size.Width/2, origin.Y+size.Height);
            }
            visual = null;
            return new Rectangle();
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


    internal static class NodeCategories
    {
        static VisualBrush _hatchBrush;

        internal static Brush GetStroke(string category)
        {
            switch (category)
            {
                case "Class": return Brushes.DarkRed;
                case "Method": return Brushes.MediumVioletRed;
                case "Property": return Brushes.DimGray;
                case "Field": return Brushes.MidnightBlue;
                case "Namespace": return Brushes.Transparent;
                default: return null;
            }
        }

        internal static Brush GetFill(string category)
        {
            switch (category)
            {
                case "Namespace":
                    if (_hatchBrush == null)
                    {
                        DrawingVisual visual = new DrawingVisual();
                        DrawingContext context = visual.RenderOpen();
                        context.DrawLine(new Pen(Brushes.LightGray, 0.5), new System.Windows.Point(-3, -1), new System.Windows.Point(3, 5));
                        context.DrawLine(new Pen(Brushes.LightGray, 0.5), new System.Windows.Point(1, -1), new System.Windows.Point(7, 5));
                        context.Close();
                        
                        _hatchBrush = new VisualBrush(visual);
                        _hatchBrush.TileMode = TileMode.Tile;
                        _hatchBrush.Viewbox = new Rect(0, 0, 4, 4);
                        _hatchBrush.ViewboxUnits = BrushMappingMode.Absolute;
                        _hatchBrush.Viewport = new Rect(0, 0, 4, 4);
                        _hatchBrush.ViewportUnits = BrushMappingMode.Absolute;
                    }
                    return _hatchBrush;
                default: return Brushes.White;
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