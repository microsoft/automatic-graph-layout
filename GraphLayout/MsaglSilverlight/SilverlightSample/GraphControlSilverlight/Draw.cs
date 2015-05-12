using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.DataStructures;
using MsaglRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Color = System.Windows.Media.Color;
using DrawingGraph = Microsoft.Msagl.Drawing.Graph;
using GeometryEdge = Microsoft.Msagl.Core.Layout.Edge;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using MsaglPoint = Microsoft.Msagl.Core.Geometry.Point;
using WinPoint = System.Windows.Point;
using MsaglStyle = Microsoft.Msagl.Drawing.Style;
using MsaglLineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using WinLineSegment = System.Windows.Media.LineSegment;
using WinSize = System.Windows.Size;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    /// <summary>
    /// exposes some drawing functionality
    /// </summary>
    public sealed class Draw
    {
        /// <summary>
        /// private constructor
        /// </summary>
        Draw()
        {
        }

        static double doubleCircleOffsetRatio = 0.9;

        internal static double DoubleCircleOffsetRatio
        {
            get { return doubleCircleOffsetRatio; }
        }


        internal static float dashSize = 0.05f; //inches

        /// <summary>
        /// A color converter
        /// </summary>
        /// <param name="gleeColor"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msagl")]
        public static Color MsaglColorToDrawingColor(Drawing.Color gleeColor)
        {
            return Color.FromArgb(gleeColor.A, gleeColor.R, gleeColor.G, gleeColor.B);
        }

        internal static void AddStyleForPen(DObject dObj, Brush myPen, MsaglStyle style)
        {
            if (style == MsaglStyle.Dashed)
            {
                throw new NotImplementedException();
                /*
                myPen.DashStyle = DashStyle.Dash;

                if (dObj.DashPatternArray == null)
                {
                    float f = dObj.DashSize();
                    dObj.DashPatternArray = new[] { f, f };
                }
                myPen.DashPattern = dObj.DashPatternArray;

                myPen.DashOffset = dObj.DashPatternArray[0];
                */
            }
            else if (style == MsaglStyle.Dotted)
            {
                throw new NotImplementedException();
                /*
                myPen.DashStyle = DashStyle.Dash;
                if (dObj.DashPatternArray == null)
                {
                    float f = dObj.DashSize();
                    dObj.DashPatternArray = new[] { 1, f };
                }
                myPen.DashPattern = dObj.DashPatternArray;
                */
            }
        }

        internal static void DrawUnderlyingPolyline(PathGeometry pg, DEdge edge)
        {
            IEnumerable<WinPoint> points = edge.GeometryEdge.UnderlyingPolyline.Select(p => WinPoint(p));
            PathFigure pf = new PathFigure() { IsFilled = false, IsClosed = false, StartPoint = points.First() };
            foreach (WinPoint p in points)
            {
                if (p != points.First())
                    pf.Segments.Add(new WinLineSegment() { Point = p });
                PathFigure circle = new PathFigure() { IsFilled = false, IsClosed = true, StartPoint = new WinPoint(p.X - edge.RadiusOfPolylineCorner, p.Y) };
                circle.Segments.Add(
                   new ArcSegment()
                   {
                       Size = new WinSize(edge.RadiusOfPolylineCorner, edge.RadiusOfPolylineCorner),
                       SweepDirection = SweepDirection.Clockwise,
                       Point = new WinPoint(p.X + edge.RadiusOfPolylineCorner, p.Y)
                   });
                circle.Segments.Add(
                   new ArcSegment()
                   {
                       Size = new WinSize(edge.RadiusOfPolylineCorner, edge.RadiusOfPolylineCorner),
                       SweepDirection = SweepDirection.Clockwise,
                       Point = new WinPoint(p.X - edge.RadiusOfPolylineCorner, p.Y)
                   });
                pg.Figures.Add(circle);
            }
            pg.Figures.Add(pf);
        }

        internal static void DrawEdgeArrows(PathGeometry pg, DrawingEdge edge, bool fillAtSource, bool fillAtTarget)
        {
            ArrowAtTheEnd(pg, edge, fillAtTarget);
            ArrowAtTheBeginning(pg, edge, fillAtSource);
        }

        private static void ArrowAtTheBeginning(PathGeometry pg, DrawingEdge edge, bool fill)
        {
            if (edge.GeometryEdge != null && edge.Attr.ArrowAtSource)
                DrawArrowAtTheBeginningWithControlPoints(pg, edge, fill);
        }

        private static void DrawArrowAtTheBeginningWithControlPoints(PathGeometry pg, DrawingEdge edge, bool fill)
        {
            if (edge.EdgeCurve != null)
                if (edge.Attr.ArrowheadAtSource == ArrowStyle.None)
                    DrawLine(pg, edge.EdgeCurve.Start, edge.ArrowAtSourcePosition);
                else
                    DrawArrow(pg, edge.EdgeCurve.Start, edge.ArrowAtSourcePosition, edge.Attr.LineWidth, edge.Attr.ArrowheadAtSource, fill);
        }

        private static void ArrowAtTheEnd(PathGeometry pg, DrawingEdge edge, bool fill)
        {
            if (edge.GeometryEdge != null && edge.Attr.ArrowAtTarget)
                DrawArrowAtTheEndWithControlPoints(pg, edge, fill);
        }

        const float toDegrees = 180 / (float)Math.PI;

        static void DrawArrowAtTheEndWithControlPoints(PathGeometry pg, DrawingEdge edge, bool fill)
        {
            if (edge.EdgeCurve != null)
                if (edge.Attr.ArrowheadAtTarget == ArrowStyle.None)
                    DrawLine(pg, edge.EdgeCurve.End,
                             edge.ArrowAtTargetPosition);
                else
                    DrawArrow(pg, edge.EdgeCurve.End,
                              edge.ArrowAtTargetPosition, edge.Attr.LineWidth, edge.Attr.ArrowheadAtTarget, fill);
        }


        internal static WinPoint WinPoint(MsaglPoint p)
        {
            return new WinPoint(p.X, p.Y);
        }

        internal static void CreateGraphicsPathFromCurve(PathFigure pathFigure, Curve curve)
        {
            foreach (var seg in curve.Segments)
            {
                var bezSeg = seg as CubicBezierSegment;
                if (bezSeg != null)
                    pathFigure.Segments.Add(new BezierSegment
                    {
                        Point1 = WinPoint(bezSeg.B(1)),
                        Point2 = WinPoint(bezSeg.B(2)),
                        Point3 = WinPoint(bezSeg.B(3))
                    });
                else
                {
                    var ls = seg as MsaglLineSegment;
                    if (ls != null)
                        pathFigure.Segments.Add(new WinLineSegment() { Point = WinPoint(ls.End) });
                    else
                    {
                        var ellipse = seg as Ellipse;
                        if (ellipse != null)
                            pathFigure.Segments.Add(
                                new ArcSegment()
                                {
                                    Size = new WinSize(ellipse.AxisA.Length, ellipse.AxisB.Length),
                                    SweepDirection = SweepDirection.Clockwise,
                                    Point = WinPoint(ellipse.End)
                                });
                        else
                            throw new InvalidOperationException();
                    }
                }
            }
        }

        internal static PathFigure CreateGraphicsPath(ICurve iCurve)
        {
            var pathFigure = new PathFigure { StartPoint = WinPoint(iCurve.Start), IsFilled = false, IsClosed = false };

            if (iCurve is Curve)
            {
                CreateGraphicsPathFromCurve(pathFigure, iCurve as Curve);
            }
            else if (iCurve is CubicBezierSegment)
            {
                var bezSeg = iCurve as CubicBezierSegment;
                pathFigure.Segments.Add(new BezierSegment
                {
                    Point1 = WinPoint(bezSeg.B(1)),
                    Point2 = WinPoint(bezSeg.B(2)),
                    Point3 = WinPoint(bezSeg.B(3))
                });
            }
            else if (iCurve is MsaglLineSegment)
            {
                var segment = iCurve as MsaglLineSegment;
                pathFigure.Segments.Add(
                    new WinLineSegment()
                    {
                        Point = WinPoint(segment.End)
                    });
            }
            else if (iCurve is Ellipse)
            {
                var ellipse = iCurve as Ellipse;
                pathFigure.Segments.Add(
                   new ArcSegment()
                   {
                       Size = new WinSize(ellipse.BoundingBox.Width / 2, ellipse.BoundingBox.Height / 2),
                       SweepDirection = SweepDirection.Clockwise,
                       Point = WinPoint(ellipse[Math.PI])
                   });
                pathFigure.Segments.Add(
                   new ArcSegment()
                   {
                       Size = new WinSize(ellipse.BoundingBox.Width / 2, ellipse.BoundingBox.Height / 2),
                       SweepDirection = SweepDirection.Clockwise,
                       Point = WinPoint(ellipse.Start)
                   });
            }
            else if (iCurve is RoundedRect)
            {
                CreateGraphicsPathFromCurve(pathFigure, (iCurve as RoundedRect).Curve);
            }
            return pathFigure;

            /*
            var graphicsPath = new GraphicsPath();
            if (iCurve == null)
                return null;
            var c = iCurve as Curve;

            if (c != null)
            {
                foreach (ICurve seg in c.Segments)
                {
                    var cubic = seg as CubicBezierSegment;
                    if (cubic != null)
                        graphicsPath.AddBezier(PointF(cubic.B(0)), PointF(cubic.B(1)), PointF(cubic.B(2)),
                                               PointF(cubic.B(3)));
                    else
                    {
                        var ls = seg as LineSegment;
                        if (ls != null)
                            graphicsPath.AddLine(PointF(ls.Start), PointF(ls.End));
                        else
                        {
                            var el = seg as Ellipse;
                            //                            double del = (el.ParEnd - el.ParStart)/11.0;
                            //                            graphicsPath.AddLines(Enumerable.Range(1, 10).Select(i => el[el.ParStart + del*i]).
                            //                                    Select(p => new PointF((float) p.X, (float) p.Y)).ToArray());

                            Rectangle box = el.BoundingBox;

                            float startAngle = EllipseStandardAngle(el, el.ParStart);

                            float sweepAngle = EllipseSweepAngle(el);

                            graphicsPath.AddArc((float)box.Left,
                                                (float)box.Bottom,
                                                (float)box.Width,
                                                (float)box.Height,
                                                startAngle,
                                                sweepAngle);
                        }
                    }
                }
            }
            else
            {
                var ls = iCurve as LineSegment;
                if (ls != null)
                    graphicsPath.AddLine(PointF(ls.Start), PointF(ls.End));
                else
                {
                    var seg = (CubicBezierSegment)iCurve;
                    graphicsPath.AddBezier(PointF(seg.B(0)), PointF(seg.B(1)), PointF(seg.B(2)), PointF(seg.B(3)));
                }
            }

            return graphicsPath; */
        }

        static float EllipseSweepAngle(Ellipse el)
        {
            float sweepAngle = (float)(el.ParEnd - el.ParStart) * toDegrees;

            if (!OrientedCounterClockwise(el))
                sweepAngle = -sweepAngle;
            return sweepAngle;
        }

        static bool OrientedCounterClockwise(Ellipse ellipse)
        {
            return ellipse.AxisA.X * ellipse.AxisB.Y - ellipse.AxisB.X * ellipse.AxisA.Y > 0;
        }

        static float EllipseStandardAngle(Ellipse ellipse, double angle)
        {
            MsaglPoint p = Math.Cos(angle) * ellipse.AxisA + Math.Sin(angle) * ellipse.AxisB;
            return (float)Math.Atan2(p.Y, p.X) * toDegrees;
        }

        static PathGeometry CreateControlPointPolygon(Tuple<double, double> t, CubicBezierSegment cubic)
        {
            throw new NotImplementedException();
            /*
            var gp = new GraphicsPath();
            gp.AddLines(new[] { PP(cubic.B(0)), PP(cubic.B(1)), PP(cubic.B(2)), PP(cubic.B(3)) });
            return gp;
            */
        }

        static WinPoint PP(Point point)
        {
            return new WinPoint(point.X, point.Y);
        }

        static PathGeometry CreatePathOnCurvaturePoint(Tuple<double, double> t, CubicBezierSegment cubic)
        {
            throw new NotImplementedException();
            /*
            var gp = new GraphicsPath();
            Point center = cubic[t.First];
            int radius = 10;
            gp.AddEllipse((float)(center.X - radius), (float)(center.Y - radius),
                          (2 * radius), (2 * radius));

            return gp; //*/
        }

        static bool NeedToFill(Color fillColor)
        {
            return fillColor.A != 0; //the color is not transparent
        }

        internal static void DrawDoubleCircle(Canvas g, Brush pen, DNode dNode)
        {
            throw new NotImplementedException();
            /*
            NodeAttr nodeAttr = dNode.DrawingNode.Attr;
            double x = nodeAttr.Pos.X - nodeAttr.Width / 2.0f;
            double y = nodeAttr.Pos.Y - nodeAttr.Height / 2.0f;
            if (NeedToFill(dNode.FillColor))
            {
                g.FillEllipse(new SolidBrush(dNode.FillColor), (float)x, (float)y, (float)nodeAttr.Width,
                              (float)nodeAttr.Height);
            }

            g.DrawEllipse(pen, (float)x, (float)y, (float)nodeAttr.Width, (float)nodeAttr.Height);
            var w = (float)nodeAttr.Width;
            var h = (float)nodeAttr.Height;
            float m = Math.Max(w, h);
            float coeff = (float)1.0 - (float)(DoubleCircleOffsetRatio);
            x += coeff * m / 2.0;
            y += coeff * m / 2.0;
            g.DrawEllipse(pen, (float)x, (float)y, w - coeff * m, h - coeff * m);
             * */
        }

        static Color FillColor(NodeAttr nodeAttr)
        {
            return MsaglColorToDrawingColor(nodeAttr.FillColor);
        }

        const double arrowAngle = 25.0; //degrees

        internal static void DrawArrow(PathGeometry pg, MsaglPoint start, MsaglPoint end, double thickness, ArrowStyle arrowStyle, bool fill)
        {
            switch (arrowStyle)
            {
                case ArrowStyle.NonSpecified:
                case ArrowStyle.Normal:
                    DrawNormalArrow(pg, start, end, thickness, fill);
                    break;
                case ArrowStyle.Tee:
                    DrawTeeArrow(pg, start, end, fill);
                    break;
                case ArrowStyle.Diamond:
                    DrawDiamondArrow(pg, start, end);
                    break;
                case ArrowStyle.ODiamond:
                    DrawODiamondArrow(pg, start, end);
                    break;
                case ArrowStyle.Generalization:
                    DrawGeneralizationArrow(pg, start, end);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal static void DrawNormalArrow(PathGeometry pg, MsaglPoint start, MsaglPoint end, double thickness, bool fill)
        {
            MsaglPoint dir = end - start;
            MsaglPoint h = dir;
            dir /= dir.Length;

            // compensate for line thickness
            end -= dir * thickness / ((double)Math.Tan(arrowAngle * (Math.PI / 180.0)));

            var s = new MsaglPoint(-dir.Y, dir.X);

            s *= h.Length * ((double)Math.Tan(arrowAngle * 0.5 * (Math.PI / 180.0)));

            PathFigure pf = new PathFigure() { IsFilled = fill, IsClosed=true };
            pf.StartPoint = WinPoint(start + s);
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(end) });
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(start - s) });
            pg.Figures.Add(pf);
            /*pf = new PathFigure();
            pf.StartPoint = WinPoint(start);
            pf.Segments.Add(new System.Windows.Media.LineSegment() { Point = WinPoint(end) });
            pg.Figures.Add(pf);*/

        }

        // For tee arrows, "fill" indicates whether the line should continue up to the node's boundary.
        static void DrawTeeArrow(PathGeometry pg, MsaglPoint start, MsaglPoint end, bool fill)
        {
            MsaglPoint dir = end - start;
            MsaglPoint h = dir;
            dir /= dir.Length;

            if (fill)
            {
                PathFigure pf = new PathFigure();
                pf.StartPoint = WinPoint(start);
                pf.Segments.Add(new WinLineSegment() { Point = WinPoint(end) });
                pg.Figures.Add(pf);
            }

            var s = new MsaglPoint(-dir.Y, dir.X);

            s *= 2 * h.Length * ((float)Math.Tan(arrowAngle * 0.5f * (Math.PI / 180.0)));
            s += s.Normalize();

            PathFigure pf2 = new PathFigure();
            pf2.StartPoint = WinPoint(start + s);
            pf2.Segments.Add(new WinLineSegment() { Point = WinPoint(start - s) });
            pg.Figures.Add(pf2);
        }

        internal static void DrawDiamondArrow(PathGeometry pg, MsaglPoint start, MsaglPoint end)
        {
            MsaglPoint dir = end - start;
            MsaglPoint h = dir;
            dir /= dir.Length;

            var s = new MsaglPoint(-dir.Y, dir.X);

            PathFigure pf = new PathFigure();
            pf.StartPoint = WinPoint(start - dir);
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(start + (h / 2) + s * (h.Length / 3)) });
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(end) });
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(start + (h / 2) - s * (h.Length / 3)) });
            pf.IsClosed = true;
            pg.Figures.Add(pf);
        }

        internal static void DrawODiamondArrow(PathGeometry pg, MsaglPoint start, MsaglPoint end)
        {
            throw new NotImplementedException();
            /*
            double lw = lineWidth == -1 ? 1 : lineWidth;
            using (var p = new Pen(brush, (float)lw))
            {
                Point dir = end - start;
                Point h = dir;
                dir /= dir.Length;

                var s = new Point(-dir.Y, dir.X);

                var points = new[]{
                                      PointF(start - dir), PointF(start + (h/2) + s*(h.Length/3)), PointF(end),
                                      PointF(start + (h/2) - s*(h.Length/3))
                                  };
                g.DrawPolygon(p, points);
            }*/
        }

        internal static void DrawGeneralizationArrow(PathGeometry pg, MsaglPoint start, MsaglPoint end)
        {
            throw new NotImplementedException();
            /*
            double lw = lineWidth == -1 ? 1 : lineWidth;
            using (var p = new Pen(brush, (float)lw))
            {
                Point dir = end - start;
                Point h = dir;
                dir /= dir.Length;

                var s = new Point(-dir.Y, dir.X);

                var points = new[]{
                                      PointF(start), PointF(start + s*(h.Length/2)), PointF(end), PointF(start - s*(h.Length/2))
                                  };

                // g.FillPolygon(p.Brush, points);
                g.DrawPolygon(p, points);
            }*/
        }

        internal static void DrawLine(PathGeometry pg, MsaglPoint start, MsaglPoint end)
        {
            PathFigure pf = new PathFigure() { StartPoint = WinPoint(start) };
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(end) });
            pg.Figures.Add(pf);
        }

        internal static void DrawBox(PathGeometry pg, DNode dNode)
        {
            throw new NotImplementedException();

            /*
            NodeAttr nodeAttr = dNode.DrawingNode.Attr;
            if (nodeAttr.XRadius == 0 || nodeAttr.YRadius == 0)
            {
                double x = nodeAttr.Pos.X - nodeAttr.Width / 2.0f;
                double y = nodeAttr.Pos.Y - nodeAttr.Height / 2.0f;

                if (NeedToFill(dNode.FillColor))
                {
                    Color fc = FillColor(nodeAttr);
                    g.FillRectangle(new SolidBrush(fc), (float)x, (float)y, (float)nodeAttr.Width,
                                    (float)nodeAttr.Height);
                }

                g.DrawRectangle(pen, (float)x, (float)y, (float)nodeAttr.Width, (float)nodeAttr.Height);
            }
            else
            {
                var width = (float)nodeAttr.Width;
                var height = (float)nodeAttr.Height;
                var xRadius = (float)nodeAttr.XRadius;
                var yRadius = (float)nodeAttr.YRadius;
                using (var path = new GraphicsPath())
                {
                    FillTheGraphicsPath(nodeAttr, width, height, ref xRadius, ref yRadius, path);

                    if (NeedToFill(dNode.FillColor))
                    {
                        g.FillPath(new SolidBrush(dNode.FillColor), path);
                    }


                    g.DrawPath(pen, path);
                }
            }
             * */
        }


        internal static void DrawDiamond(Canvas g, Brush pen, DNode dNode)
        {
            throw new NotImplementedException();
            /*
            NodeAttr nodeAttr = dNode.DrawingNode.Attr;
            double w2 = nodeAttr.Width / 2.0f;
            double h2 = nodeAttr.Height / 2.0f;
            double cx = nodeAttr.Pos.X;
            double cy = nodeAttr.Pos.Y;
            var ps = new[]{
                              new PointF((float) cx - (float) w2, (float) cy),
                              new PointF((float) cx, (float) cy + (float) h2),
                              new PointF((float) cx + (float) w2, (float) cy),
                              new PointF((float) cx, (float) cy - (float) h2)
                          };

            if (NeedToFill(dNode.FillColor))
            {
                Color fc = FillColor(nodeAttr);
                g.FillPolygon(new SolidBrush(fc), ps);
            }

            g.DrawPolygon(pen, ps); //*/
        }

        internal static void DrawEllipse(Canvas g, Brush pen, DNode dNode)
        {
            var node = dNode.DrawingNode;
            NodeAttr nodeAttr = node.Attr;
            var x = (float)(node.GeometryNode.Center.X - node.Width / 2.0);
            var y = (float)(node.GeometryNode.Center.Y - node.Height / 2.0);
            var width = (float)node.Width;
            var height = (float)node.Height;

            DrawEllipseOnPosition(dNode, nodeAttr, g, x, y, width, height, pen);
        }

        static void DrawEllipseOnPosition(DNode dNode, NodeAttr nodeAttr, Canvas g, float x, float y, float width,
                                          float height, Brush pen)
        {
            throw new NotImplementedException();
            /*
            if (NeedToFill(dNode.FillColor))
                g.FillEllipse(new SolidBrush(dNode.FillColor), x, y, width, height);
            if (nodeAttr.Shape == Shape.Point)
                g.FillEllipse(new SolidBrush(pen.Color), x, y, width, height);

            g.DrawEllipse(pen, x, y, width, height); //*/
        }


        //don't know what to do about the throw-catch block
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static void DrawLabel(Canvas g, DLabel label)
        {
            if (label == null)
                return;

            throw new NotImplementedException();
            /*
            try
            {
                DrawStringInRectCenter(g, new SolidBrush(MsaglColorToDrawingColor(label.DrawingLabel.FontColor)),
                                       label.Font, label.DrawingLabel.Text,
                                       new RectangleF((float)label.DrawingLabel.Left, (float)label.DrawingLabel.Bottom,
                                                      (float)label.DrawingLabel.Size.Width,
                                                      (float)label.DrawingLabel.Size.Height));
            }
            catch
            {
            }
            if (label.MarkedForDragging)
            {
                var pen = new Pen(MsaglColorToDrawingColor(label.DrawingLabel.FontColor));
                pen.DashStyle = DashStyle.Dot;
                DrawLine(g, pen, label.DrawingLabel.GeometryLabel.AttachmentSegmentStart,
                         label.DrawingLabel.GeometryLabel.AttachmentSegmentEnd);
            } //*/
        }

        static void DrawStringInRectCenter(Canvas g, Brush brush, /*Font f,*/ string s, Rect r
            /*, double rectLineWidth*/)
        {
            if (String.IsNullOrEmpty(s))
                return;

            throw new NotImplementedException();
            /*
            using (Matrix m = g.Transform)
            {
                using (Matrix saveM = m.Clone())
                {
                    //rotate the label around its center
                    float c = (r.Bottom + r.Top) / 2;

                    using (var m2 = new Matrix(1, 0, 0, -1, 0, 2 * c))
                    {
                        m.Multiply(m2);
                    }
                    g.Transform = m;
                    using (StringFormat stringFormat = StringFormat.GenericTypographic)
                    {
                        g.DrawString(s, f, brush, r.Left, r.Top, stringFormat);
                    }
                    g.Transform = saveM;
                }
            }
            */
        }

        internal static WinPoint PointF(MsaglPoint p)
        {
            return new WinPoint(p.X, p.Y);
        }


        internal static void DrawFromMsaglCurve(Canvas g, Brush pen, DNode dNode)
        {
            throw new NotImplementedException();
            /*
            NodeAttr attr = dNode.DrawingNode.Attr;
            var c = attr.GeometryNode.BoundaryCurve as Curve;
            if (c != null)
            {
                var path = new GraphicsPath();
                foreach (ICurve seg in c.Segments)
                    AddSegToPath(seg, ref path);

                if (NeedToFill(dNode.FillColor))
                {
                    g.FillPath(new SolidBrush(dNode.FillColor), path);
                }
                g.DrawPath(pen, path);
            }
            else
            {
                var ellipse = attr.GeometryNode.BoundaryCurve as Ellipse;
                if (ellipse != null)
                {
                    double w = ellipse.AxisA.X;
                    double h = ellipse.AxisB.Y;
                    DrawEllipseOnPosition(dNode, dNode.DrawingNode.Attr, g, (float)(ellipse.Center.X - w),
                                          (float)(ellipse.Center.Y - h),
                                          (float)w * 2, (float)h * 2, pen);
                }
            }*/
        }


        static void AddSegToPath(ICurve seg, ref PathGeometry path)
        {
            throw new NotImplementedException();
            /*
            var line = seg as LineSegment;
            if (line != null)
                path.AddLine(PointF(line.Start), PointF(line.End));
            else
            {
                var cb = seg as CubicBezierSegment;
                if (cb != null)
                    path.AddBezier(PointF(cb.B(0)), PointF(cb.B(1)), PointF(cb.B(2)), PointF(cb.B(3)));
                else
                {
                    var ellipse = seg as Ellipse;
                    if (ellipse != null)
                    {
                        //we assume that ellipes are going counterclockwise
                        double cx = ellipse.Center.X;
                        double cy = ellipse.Center.Y;
                        double w = ellipse.AxisA.X * 2;
                        double h = ellipse.AxisB.Y * 2;
                        double sweep = ellipse.ParEnd - ellipse.ParStart;

                        if (sweep < 0)
                            sweep += Math.PI * 2;
                        const double toDegree = 180 / Math.PI;
                        path.AddArc((float)(cx - w / 2), (float)(cy - h / 2), (float)w, (float)h,
                                    (float)(ellipse.ParStart * toDegree), (float)(sweep * toDegree));
                    }
                }
            }*/
        }
    }
}