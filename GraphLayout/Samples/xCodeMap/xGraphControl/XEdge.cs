using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Routing;
using Ellipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Polyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Size = System.Windows.Size;

namespace xCodeMap.xGraphControl
{
    internal class XEdge : IViewerEdgeX, IInvalidatable
    {

        #region Implementation of IViewerEdge

        private FrameworkElement _visualObject;
        public FrameworkElement VisualObject
        {
            get { return _visualObject; }
        }
                
        public Edge Edge { get; private set; }

        public IViewerNode Source { get; private set; }
        public IViewerNode Target { get; private set; }
        public double RadiusOfPolylineCorner { get; set; }

        public XLabel XLabel { get; set; }
        private string _category;

        #endregion
        
        public XEdge(Edge edge, string category=null)
        {
            Edge = edge;
            _category = category;
            _strokePathThickness = edge.Attr.LineWidth;

            if (edge.Label != null)
            {
                XLabel label = new XLabel(edge);
                this.XLabel = label;
                _visualObject = label.VisualObject;
            }
            
            Path = new Path
            {
                Stroke = CommonX.BrushFromMsaglColor(edge.Attr.Color),
                StrokeThickness = _strokePathThickness,
                StrokeDashArray = EdgeCategories.GetDashArray(category),
                Tag = this
            };

        }

        public Path Path { get; set; }

        public Geometry DefiningGeometry(Edge drawingEdge, Microsoft.Msagl.Core.Layout.Edge geometryEdge)
        {
            if (geometryEdge.Curve == null)
            {
                Path.Visibility = Visibility.Hidden;
            }
            else
            {
                Path.Visibility = Visibility.Visible;

                if (Path.Triggers.Count == 0)
                {
                    AddPathAnimation();
                }
            }


            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open())
            {
                FillStreamGeometryContext(context, drawingEdge, geometryEdge);
                return streamGeometry;
            }
        }

        private void AddPathAnimation()
        {
            EventTrigger et = new EventTrigger(Path.MouseEnterEvent);
            BeginStoryboard bs = new BeginStoryboard();
            ColorAnimation ca = new ColorAnimation { To = Colors.LightBlue, Duration = TimeSpan.FromSeconds(0), AutoReverse = false };
            Storyboard.SetTarget(ca, Path);
            Storyboard.SetTargetProperty(ca, new PropertyPath("Stroke.Color"));
            bs.Storyboard = new Storyboard();
            bs.Storyboard.Children.Add(ca);
            bs.Storyboard.Completed += (o, e) => { Path.StrokeThickness = _strokePathThickness * 2; };
            et.Actions.Add(bs);
            Path.Triggers.Add(et);

            et = new EventTrigger(Path.MouseLeaveEvent);
            bs = new BeginStoryboard();
            ca = new ColorAnimation { To = ((SolidColorBrush)Path.Stroke).Color, Duration = TimeSpan.FromSeconds(0), AutoReverse = false };
            Storyboard.SetTarget(ca, Path);
            Storyboard.SetTargetProperty(ca, new PropertyPath("Stroke.Color"));
            bs.Storyboard = new Storyboard();
            bs.Storyboard.Children.Add(ca);
            bs.Storyboard.Completed += (o, e) => { Path.StrokeThickness = _strokePathThickness; };
            et.Actions.Add(bs);
            Path.Triggers.Add(et);

            Path.ToolTip = Edge.SourceNode.LabelText + " " + _category + " " + Edge.TargetNode.LabelText;
        }

        void FillStreamGeometryContext(StreamGeometryContext context, Edge drawingEdge,
                                       Microsoft.Msagl.Core.Layout.Edge geometryEdge)
        {
            if (geometryEdge.Curve == null)
                return;
            FillContextForICurve(context, geometryEdge.Curve);

            if (geometryEdge.EdgeGeometry != null && geometryEdge.EdgeGeometry.SourceArrowhead != null)
                AddArrow(drawingEdge, context, geometryEdge.Curve.Start,
                         geometryEdge.EdgeGeometry.SourceArrowhead.TipPosition, drawingEdge.SourceNode.Attr.LineWidth);
            if (geometryEdge.EdgeGeometry != null && geometryEdge.EdgeGeometry.TargetArrowhead != null)
                AddArrow(drawingEdge, context, geometryEdge.Curve.End,
                         geometryEdge.EdgeGeometry.TargetArrowhead.TipPosition, drawingEdge.TargetNode.Attr.LineWidth);
        }

        internal void FillContextForICurve(StreamGeometryContext context, ICurve iCurve)
        {
            if (iCurve == null) return;
            context.BeginFigure(CommonX.WpfPoint(iCurve.Start), false, false);

            var c = iCurve as Curve;
            if (c != null)
                FillContexForCurve(context, c);
            else
            {
                var cubicBezierSeg = iCurve as CubicBezierSegment;
                if (cubicBezierSeg != null)
                    context.BezierTo(CommonX.WpfPoint(cubicBezierSeg.B(1)), CommonX.WpfPoint(cubicBezierSeg.B(2)),
                                     CommonX.WpfPoint(cubicBezierSeg.B(3)), true, false);
                else
                {
                    var ls = iCurve as LineSegment;
                    if (ls != null)
                        context.LineTo(CommonX.WpfPoint(ls.End), true, false);
                    else
                    {
                        var rr = iCurve as RoundedRect;
                        if (rr != null)
                            FillContexForCurve(context, rr.Curve);
                        else
                        {
                            var poly = iCurve as Polyline;
                            FillContexForPolyline(context, poly);
                        }
                    }
                }
            }
        }

        static void FillContexForPolyline(StreamGeometryContext context, Polyline poly)
        {
            for (PolylinePoint pp = poly.StartPoint.Next; pp != null; pp = pp.Next)
                context.LineTo(CommonX.WpfPoint(pp.Point), true, false);
        }

        static void FillContexForCurve(StreamGeometryContext context, Curve c)
        {
            foreach (ICurve seg in c.Segments)
            {
                var bezSeg = seg as CubicBezierSegment;
                if (bezSeg != null)
                {
                    context.BezierTo(CommonX.WpfPoint(bezSeg.B(1)),
                                     CommonX.WpfPoint(bezSeg.B(2)), CommonX.WpfPoint(bezSeg.B(3)), true, false);
                }
                else
                {
                    var ls = seg as LineSegment;
                    if (ls != null)
                        context.LineTo(CommonX.WpfPoint(ls.End), true, false);
                    else
                    {
                        var ellipse = seg as Ellipse;
                        if (ellipse != null)
                        {
                            //       context.LineTo(Common.WpfPoint(ellipse.End),true,false);
                            double sweepAngle = EllipseSweepAngle(ellipse);
                            bool largeArc = Math.Abs(sweepAngle) >= Math.PI;
                            Rectangle box = ellipse.FullBox();
                            context.ArcTo(CommonX.WpfPoint(ellipse.End),
                                          new Size(box.Width / 2, box.Height / 2),
                                          sweepAngle,
                                          largeArc,
                                          sweepAngle < 0
                                              ? SweepDirection.Counterclockwise
                                              : SweepDirection.Clockwise,
                                          true, true);
                        }
                        else
                            throw new NotImplementedException();
                    }
                }
            }
        }

        public static double EllipseSweepAngle(Ellipse ellipse)
        {
            double sweepAngle = ellipse.ParEnd - ellipse.ParStart;
            return ellipse.OrientedCounterclockwise() ? sweepAngle : -sweepAngle;
        }


        void AddArrow(Edge drawingEdge, StreamGeometryContext context, Point start, Point end, double lineWidthOfAttachedNode)
        {
            
            Point dir = end - start;
            double dl = dir.Length;

            double scaling = (dl<12? 1 : 12 / dl) / _scale;
            Point new_start = end - (end - start) * scaling;

            //take into account the widths
            double delta = Math.Min(dl / 2, drawingEdge.Attr.LineWidth + lineWidthOfAttachedNode / 2);
            //dir *= (dl - delta) / dl;
            end = start + dir;
            dir = dir.Rotate(Math.PI / 2);
            Point s = dir * HalfArrowAngleTan * scaling;

            context.BeginFigure(CommonX.WpfPoint(start), true, true);
            context.LineTo(CommonX.WpfPoint(new_start), true, true);

            if (_category == "References")
            {
                double r = dl * scaling / 2;
                context.ArcTo(CommonX.WpfPoint(end), new Size(r, r), 0, true, SweepDirection.Clockwise, true, true);
                context.ArcTo(CommonX.WpfPoint(new_start), new Size(r, r), 0, true, SweepDirection.Clockwise, true, true);
            }
            else
            {
                context.LineTo(CommonX.WpfPoint(new_start + s), true, true);
                context.LineTo(CommonX.WpfPoint(end), true, true);
                context.LineTo(CommonX.WpfPoint(new_start - s), true, true);
                context.LineTo(CommonX.WpfPoint(new_start), true, true);
            }
        }

        static readonly double HalfArrowAngleTan = Math.Tan(ArrowAngle * 0.5 * Math.PI / 180.0);
        static readonly double HalfArrowAngleCos = Math.Cos(ArrowAngle * 0.5 * Math.PI / 180.0);
        const double ArrowAngle = 30.0; //degrees

        #region Implementation of IViewerObject

        public DrawingObject DrawingObject
        {
            get { return Edge; }
        }

        public bool MarkedForDragging { get; set; }
        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;

        #endregion

        private double _scale = 1;
        
        public void Invalidate(double scale = 1)
        {
            _scale = scale;
            Path.Data = DefiningGeometry(Edge, Edge.GeometryEdge);

            if (XLabel != null)
                ((IInvalidatable)XLabel).Invalidate();
        }

        public override string ToString()
        {
            return Edge.ToString();
        }

        private double _strokePathThickness;
        internal double StrokePathThickness
        {
            set
            {
                _strokePathThickness = value;
                Path.StrokeThickness = value;
            }
        }
    }

    internal static class EdgeCategories
    {
        internal static DoubleCollection GetDashArray(string category)
        {
            switch (category)
            {
                case "References": return new DoubleCollection(new double[] { 6, 4 });
                default: return null;
            }
        }
    }
}