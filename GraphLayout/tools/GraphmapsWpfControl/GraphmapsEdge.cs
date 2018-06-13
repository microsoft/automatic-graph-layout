using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Routing;
using Color = Microsoft.Msagl.Drawing.Color;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Ellipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Polyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Size = System.Windows.Size;
using WpfColor = System.Windows.Media.Color;

namespace Microsoft.Msagl.GraphmapsWpfControl {
    internal class GraphmapsEdge : IViewerEdge, IInvalidatable {
        readonly LgLayoutSettings lgSettings;

        internal FrameworkElement LabelFrameworkElement;

        public GraphmapsEdge(Edge edge, FrameworkElement labelFrameworkElement) {
            Edge = edge;
            CurvePath = new Path {
                Data = GetICurveWpfGeometry(edge.GeometryEdge.Curve),
                Tag = this
            };

            EdgeAttrClone = edge.Attr.Clone();

            if (edge.Attr.ArrowAtSource)
                SourceArrowHeadPath = new Path {
                    Data = DefiningSourceArrowHead(),
                    Tag = this
                };
            if (edge.Attr.ArrowAtTarget)
                TargetArrowHeadPath = new Path {
                    Data = DefiningTargetArrowHead(Edge.GeometryEdge.EdgeGeometry, PathStrokeThickness),
                    Tag = this
                };

            SetPathStroke();

            if (labelFrameworkElement != null) {
                LabelFrameworkElement = labelFrameworkElement;
                Common.PositionFrameworkElement(LabelFrameworkElement, edge.Label.Center, 1);
            }
            edge.Attr.VisualsChanged += (a, b) => Invalidate();
            
        }

        internal IEnumerable<FrameworkElement> FrameworkElements {
            get {
                if (lgSettings == null) {
                    if (SourceArrowHeadPath != null)
                        yield return this.SourceArrowHeadPath;
                    if (TargetArrowHeadPath != null)
                        yield return TargetArrowHeadPath;

                    if (CurvePath != null)
                        yield return CurvePath;

                    if (
                        LabelFrameworkElement != null)
                        yield return
                            LabelFrameworkElement;
                } else {
                    
                }
            }
        }

        internal EdgeAttr EdgeAttrClone { get; set; }
            
        internal static Geometry DefiningTargetArrowHead(EdgeGeometry edgeGeometry, double thickness) {
            if (edgeGeometry.TargetArrowhead == null || edgeGeometry.Curve==null)
                return null;
            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open()) {
                AddArrow(context, edgeGeometry.Curve.End,
                         edgeGeometry.TargetArrowhead.TipPosition, thickness);
                return streamGeometry;
            }
        }

        Geometry DefiningSourceArrowHead() {
            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open()) {
                AddArrow(context, Edge.GeometryEdge.Curve.Start, Edge.GeometryEdge.EdgeGeometry.SourceArrowhead.TipPosition, PathStrokeThickness);
                return streamGeometry;
            }
        }


        public double PathStrokeThickness { get {
            return PathStrokeThicknessFunc != null ? PathStrokeThicknessFunc() : this.Edge.Attr.LineWidth;
        } }

        internal Path CurvePath { get; set; }
        internal Path SourceArrowHeadPath { get; set; }
        internal Path TargetArrowHeadPath { get; set; }

        static internal Geometry GetICurveWpfGeometry(ICurve curve) {
            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open()) {
                FillStreamGeometryContext(context, curve);

                //test freeze for performace
                streamGeometry.Freeze();
                return streamGeometry;               
            }
        }

        static void FillStreamGeometryContext(StreamGeometryContext context, ICurve curve) {
            if (curve == null)
                return;
            FillContextForICurve(context, curve);
        }

        static internal void FillContextForICurve(StreamGeometryContext context,ICurve iCurve) {
            
            context.BeginFigure(Common.WpfPoint(iCurve.Start),false,false);

            var c = iCurve as Curve;
            if(c != null)
                FillContexForCurve(context,c);
            else {
                var cubicBezierSeg = iCurve as CubicBezierSegment;
                if(cubicBezierSeg != null)
                    context.BezierTo(Common.WpfPoint(cubicBezierSeg.B(1)),Common.WpfPoint(cubicBezierSeg.B(2)),
                                     Common.WpfPoint(cubicBezierSeg.B(3)),true,false);
                else {
                    var ls = iCurve as LineSegment;
                    if(ls != null)
                        context.LineTo(Common.WpfPoint(ls.End),true,false);
                    else {
                        var rr = iCurve as RoundedRect;
                        if(rr != null)
                            FillContexForCurve(context,rr.Curve);
                        else {
                            var poly = iCurve as Polyline;
                            if (poly != null)
                                FillContexForPolyline(context, poly);
                            else
                            {
                                var ellipse = iCurve as Ellipse;
                                if (ellipse != null) {
                                    //       context.LineTo(Common.WpfPoint(ellipse.End),true,false);
                                    double sweepAngle = EllipseSweepAngle(ellipse);
                                    bool largeArc = Math.Abs(sweepAngle) >= Math.PI;
                                    Rectangle box = ellipse.FullBox();
                                    context.ArcTo(Common.WpfPoint(ellipse.End),
                                                  new Size(box.Width/2, box.Height/2),
                                                  sweepAngle,
                                                  largeArc,
                                                  sweepAngle < 0
                                                      ? SweepDirection.Counterclockwise
                                                      : SweepDirection.Clockwise,
                                                  true, true);
                                } else {
                                    throw new NotImplementedException();
                                }
                            }
                        }
                    }
                }
            }
        }

        static void FillContexForPolyline(StreamGeometryContext context,Polyline poly) {
            for(PolylinePoint pp = poly.StartPoint.Next;pp != null;pp = pp.Next)
                context.LineTo(Common.WpfPoint(pp.Point),true,false);
        }

        static void FillContexForCurve(StreamGeometryContext context,Curve c) {
            foreach(ICurve seg in c.Segments) {
                var bezSeg = seg as CubicBezierSegment;
                if(bezSeg != null) {
                    context.BezierTo(Common.WpfPoint(bezSeg.B(1)),
                                     Common.WpfPoint(bezSeg.B(2)),Common.WpfPoint(bezSeg.B(3)),true,false);
                } else {
                    var ls = seg as LineSegment;
                    if(ls != null)
                        context.LineTo(Common.WpfPoint(ls.End),true,false);
                    else {
                        var ellipse = seg as Ellipse;
                        if(ellipse != null) {
                            //       context.LineTo(Common.WpfPoint(ellipse.End),true,false);
                            double sweepAngle = EllipseSweepAngle(ellipse);
                            bool largeArc = Math.Abs(sweepAngle) >= Math.PI;
                            Rectangle box = ellipse.FullBox();
                            context.ArcTo(Common.WpfPoint(ellipse.End),
                                          new Size(box.Width / 2,box.Height / 2),
                                          sweepAngle,
                                          largeArc,
                                          sweepAngle < 0
                                              ? SweepDirection.Counterclockwise
                                              : SweepDirection.Clockwise,
                                          true,true);
                        } else
                            throw new NotImplementedException();
                    }
                }
            }
        }

        public static double EllipseSweepAngle(Ellipse ellipse) {
            double sweepAngle = ellipse.ParEnd - ellipse.ParStart;
            return ellipse.OrientedCounterclockwise() ? sweepAngle : -sweepAngle;
        }


        public static void AddArrow(StreamGeometryContext context,Point start,Point end, double thickness) {
            
            if(thickness > 1) {
                Point dir = end - start;
                Point h = dir;
                double dl = dir.Length;
                if(dl < 0.001)
                    return;
                dir /= dl;

                var s = new Point(-dir.Y,dir.X);
                double w = 0.5 * thickness;
                Point s0 = w * s;

                s *= h.Length * HalfArrowAngleTan;
                s += s0;

                double rad = w / HalfArrowAngleCos;

                context.BeginFigure(Common.WpfPoint(start + s),true,true);
                context.LineTo(Common.WpfPoint(start - s),true,false);
                context.LineTo(Common.WpfPoint(end - s0),true,false);
                context.ArcTo(Common.WpfPoint(end + s0),new Size(rad,rad),
                              Math.PI - ArrowAngle,false,SweepDirection.Clockwise,true,false);
            } else {
                Point dir = end - start;
                double dl = dir.Length;
                //take into account the widths
                double delta = Math.Min(dl / 2, thickness + thickness / 2);
                dir *= (dl - delta) / dl;
                end = start + dir;
                dir = dir.Rotate(Math.PI / 2);
                Point s = dir * HalfArrowAngleTan;

                context.BeginFigure(Common.WpfPoint(start + s),true,true);
                context.LineTo(Common.WpfPoint(end),true,true);
                context.LineTo(Common.WpfPoint(start - s),true,true);
            }
        }

        static readonly double HalfArrowAngleTan = Math.Tan(ArrowAngle * 0.5 * Math.PI / 180.0);
        static readonly double HalfArrowAngleCos = Math.Cos(ArrowAngle * 0.5 * Math.PI / 180.0);
        const double ArrowAngle = 30.0; //degrees

        #region Implementation of IViewerObject

        public DrawingObject DrawingObject {
            get { return Edge; }
        }

        public bool MarkedForDragging { get; set; }

#pragma warning disable 0067
        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;
#pragma warning restore 0067

        #endregion

        #region Implementation of IViewerEdge

        public Edge Edge { get;  set; }
        public IViewerNode Source { get;  set; }
        public IViewerNode Target { get;  set; }
        public double RadiusOfPolylineCorner { get; set; }

        public GraphmapsLabel GraphmapsLabel { get; set; }

        #endregion

        internal void Invalidate(FrameworkElement fe, Rail rail) {
            var path = fe as Path;
            if (path != null)
                SetPathStrokeToRailPath(rail, path);
        }
        public void Invalidate()
        {
            var vis = Edge.IsVisible ? Visibility.Visible : Visibility.Hidden;
            foreach (var fe in FrameworkElements) fe.Visibility = vis;
            if (vis == Visibility.Hidden)
                return;
            if (lgSettings != null) {
                InvalidateForLgCase();
                return;
            }
            CurvePath.Data = GetICurveWpfGeometry(Edge.GeometryEdge.Curve);
            if (Edge.Attr.ArrowAtSource)
                SourceArrowHeadPath.Data = DefiningSourceArrowHead();
            if (Edge.Attr.ArrowAtTarget)
                TargetArrowHeadPath.Data = DefiningTargetArrowHead(Edge.GeometryEdge.EdgeGeometry, PathStrokeThickness);
            SetPathStroke();
            if (GraphmapsLabel != null)
                ((IInvalidatable) GraphmapsLabel).Invalidate();
        }

        void InvalidateForLgCase() {
            throw new NotImplementedException();
        }

        void SetPathStroke() {
            SetPathStrokeToPath(CurvePath);
            if (SourceArrowHeadPath != null) {
                SourceArrowHeadPath.Stroke = SourceArrowHeadPath.Fill = Common.BrushFromMsaglColor(Edge.Attr.Color);
                SourceArrowHeadPath.StrokeThickness = PathStrokeThickness;
            }
            if (TargetArrowHeadPath != null) {
                TargetArrowHeadPath.Stroke = TargetArrowHeadPath.Fill = Common.BrushFromMsaglColor(Edge.Attr.Color);
                TargetArrowHeadPath.StrokeThickness = PathStrokeThickness;
            }
        }

        public void SetPathStrokeToRailPath(Rail rail, Path path) {
            
            path.Stroke = SetStrokeColorForRail(rail);

            double thickness = 1.0;
            if (rail.TopRankedEdgeInfoOfTheRail != null)
            {
                thickness = Math.Max(5 - Math.Log(rail.MinPassingEdgeZoomLevel, 1.5), 1);
            }

            path.StrokeThickness = thickness * PathStrokeThickness / 2; // todo : figure out a way to do it nicer than dividing by 2

            //jyoti added this to make the selected edges prominent
            if (rail.IsHighlighted)
            {
                thickness = 2.5;
                path.StrokeThickness = thickness * PathStrokeThickness / 2;
            }
            /////////////////
            
            foreach (var style in Edge.Attr.Styles) {
                if (style == Drawing.Style.Dotted) {
                    path.StrokeDashArray = new DoubleCollection {1, 1};
                } else if (style == Drawing.Style.Dashed) {
                    var f = DashSize();
                    path.StrokeDashArray = new DoubleCollection {f, f};
                    //CurvePath.StrokeDashOffset = f;
                }
            }
        }

        Brush SetStrokeColorForRail(Rail rail)
        {
            // road colors: Brushes.PaleVioletRed; Brushes.PaleGoldenrod; Brushes.White;
            WpfColor brush;
            //brush = rail.IsHighlighted == false
            //           ? new SolidColorBrush(new System.Windows.Media.Color {
            //               A = 255, //transparency,
            //               R = Edge.Attr.Color.R,
            //               G = Edge.Attr.Color.G,
            //               B = Edge.Attr.Color.B
            //           })
            //           : Brushes.Red;

            brush = rail.IsHighlighted ? Brushes.Red.Color : Brushes.SlateGray.Color;
            if (rail.TopRankedEdgeInfoOfTheRail == null) return new SolidColorBrush(brush);

            if (lgSettings != null)
            {
                /*
                var col = lgSettings.GetColorForZoomLevel(rail.MinPassingEdgeZoomLevel);
                brush = ((SolidColorBrush)(new BrushConverter().ConvertFrom(col))).Color;
                 */
                //jyoti: changed rail colors
                if (rail.MinPassingEdgeZoomLevel <= 1) brush = Brushes.LightSkyBlue.Color; //Brushes.DimGray.Color;
                else if (rail.MinPassingEdgeZoomLevel <= 2)
                    brush = Brushes.LightSkyBlue.Color; //Brushes.SlateGray.Color;
                else if (rail.MinPassingEdgeZoomLevel <= 3)
                    brush = Brushes.LightGoldenrodYellow.Color; //Brushes.SlateGray.Color;
                else if (rail.MinPassingEdgeZoomLevel <= 4)
                    brush = Brushes.WhiteSmoke.Color; //Brushes.SlateGray.Color;
                else brush = Brushes.LightGray.Color; //Brushes.Gray.Color;
            }
            else
            {
                //jyoti: changed rail colors
                if (rail.MinPassingEdgeZoomLevel <= 1) brush = Brushes.LightSkyBlue.Color; //Brushes.DimGray.Color;
                else if (rail.MinPassingEdgeZoomLevel <= 2)
                    brush = Brushes.LightSteelBlue.Color; //Brushes.SlateGray.Color;
                else if (rail.MinPassingEdgeZoomLevel <= 3)
                    brush = Brushes.LightGoldenrodYellow.Color; //Brushes.SlateGray.Color;
                else if (rail.MinPassingEdgeZoomLevel <= 4)
                    brush = Brushes.WhiteSmoke.Color; //Brushes.SlateGray.Color;
                else brush = Brushes.LightGray.Color; //Brushes.Gray.Color;
            }

            brush.A = 100;
            if (rail.IsHighlighted)
            {
                //jyoti changed edge selection color 
               
                //this is a garbage rail
                if (rail.Color == null || rail.Color.Count == 0)
                {
                    rail.IsHighlighted = false;
                }
                else
                {
                    int Ax = 0, Rx = 0, Gx = 0, Bx = 0;
                    foreach (SolidColorBrush c in rail.Color)
                    {
                        Ax += c.Color.A;
                        Rx += c.Color.R;
                        Gx += c.Color.G;
                        Bx += c.Color.B;
                    }
                    byte Ay = 0, Ry = 0, Gy = 0, By = 0;
                    Ay = (Byte) ((int) (Ax/rail.Color.Count));
                    Ry = (Byte) ((int) (Rx/rail.Color.Count));
                    Gy = (Byte) ((int) (Gx/rail.Color.Count));
                    By = (Byte) ((int) (Bx/rail.Color.Count));

                    brush = new System.Windows.Media.Color
                    {
                        A = Ay,
                        R = Ry,
                        G = Gy,
                        B = By
                    };

                 
                }

                //jyoti changed edge selection color 



                //if (rail.MinPassingEdgeZoomLevel <= 1) brush = Brushes.Red.Color;
                //else if (rail.MinPassingEdgeZoomLevel <= 2) brush = new WpfColor{A = 255, R=235, G=48, B=68};
                //else brush = new WpfColor { A = 255, R = 229, G = 92, B = 127 };
            }

            if (rail.Weight == 0 && !rail.IsHighlighted )
            {
                brush = new System.Windows.Media.Color
                {
                    A = 0,
                    R = 0,
                    G = 0,
                    B = 255
                };
            }

            return new SolidColorBrush(brush);
        }

        void SetPathStrokeToPath(Path path) {
            path.Stroke = Common.BrushFromMsaglColor(Edge.Attr.Color);
            path.StrokeThickness = PathStrokeThickness;

            foreach (var style in Edge.Attr.Styles) {
                if (style == Drawing.Style.Dotted) {
                    path.StrokeDashArray = new DoubleCollection {1, 1};
                } else if (style == Drawing.Style.Dashed) {
                    var f = DashSize();
                    path.StrokeDashArray = new DoubleCollection {f, f};
                    //CurvePath.StrokeDashOffset = f;
                }
            }
        }

        public override string ToString() {
            return Edge.ToString();
        }

        internal static double dashSize = 0.05; //inches
        internal Func<double> PathStrokeThicknessFunc;

        public GraphmapsEdge(Edge edge, LgLayoutSettings lgSettings) {
            Edge = edge;
            EdgeAttrClone = edge.Attr.Clone();
            this.lgSettings = lgSettings;
        }

        internal double DashSize()
        {
            var w = PathStrokeThickness;
            var dashSizeInPoints = dashSize * GraphmapsViewer.DpiXStatic;
            return dashSize = dashSizeInPoints / w;
        }

        internal void RemoveItselfFromCanvas(Canvas graphCanvas) {
            if(CurvePath!=null)
                graphCanvas.Children.Remove(CurvePath);

            if (SourceArrowHeadPath != null)
                graphCanvas.Children.Remove(SourceArrowHeadPath);

            if (TargetArrowHeadPath != null)
                graphCanvas.Children.Remove(TargetArrowHeadPath);

            if(GraphmapsLabel!=null)
                graphCanvas.Children.Remove(GraphmapsLabel.FrameworkElement );

        }

        public FrameworkElement CreateFrameworkElementForRail(Rail rail) {
            var iCurve = rail.Geometry as ICurve;
            Path fe = null;
            if (iCurve != null) {
                fe = (Path)CreateFrameworkElementForRailCurve(rail, iCurve);
                // test: rounded ends
                fe.StrokeEndLineCap = PenLineCap.Round;
                fe.StrokeStartLineCap = PenLineCap.Round;
                fe.Tag = rail;
            }            
            return fe;
        }

        public FrameworkElement CreateFrameworkElementForRailArrowhead(Rail rail, Arrowhead arrowhead, Point curveAttachmentPoint, byte edgeTransparency) {
            var streamGeometry = new StreamGeometry();

            using (StreamGeometryContext context = streamGeometry.Open()) {
                AddArrow(context, curveAttachmentPoint, arrowhead.TipPosition,
                         PathStrokeThickness);
                //arrowhead.BasePoint = curveAttachmentPoint;

            }

            var path=new Path
            {
                Data = streamGeometry,
                Tag = this
            };

            SetPathStrokeToRailPath(rail, path);
            return path;
        }

        FrameworkElement CreateFrameworkElementForRailCurve(Rail rail, ICurve iCurve) {
            var path = new Path
            {
                Data = GetICurveWpfGeometry(iCurve),
            };
            SetPathStrokeToRailPath(rail, path);
            return path;
        }
    }
}