using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;

namespace CommonDrawingUtilsForSamples {
    public class DrawingUtilsForSamples {
        public static void DrawFromGraph(System.Drawing.Rectangle clientRectangle, GeometryGraph geometryGraph, Graphics graphics) {
            SetGraphTransform(geometryGraph, clientRectangle);
            var pen = new Pen(Brushes.Black);
            DrawNodes(geometryGraph, pen, graphics);
            DrawEdges(geometryGraph, pen, graphics);
        }

        public static void SetGraphTransform(GeometryGraph geometryGraph, System.Drawing.Rectangle rectangle) {
            //instead of setting transormation for graphics we are going to transform the geometry graph, just to test that GeometryGraph.Transform() works
            RectangleF clientRectangle = rectangle;
            var gr = geometryGraph.BoundingBox;
            if (clientRectangle.Height > 1 && clientRectangle.Width > 1) {
                var scale = Math.Min(clientRectangle.Width * 0.9 / gr.Width, clientRectangle.Height * 0.9 / gr.Height);
                var g0 = (gr.Left + gr.Right) / 2;
                var g1 = (gr.Top + gr.Bottom) / 2;

                var c0 = (clientRectangle.Left + clientRectangle.Right) / 2;
                var c1 = (clientRectangle.Top + clientRectangle.Bottom) / 2;
                var dx = c0 - scale * g0;
                var dy = c1 - scale * g1;
                var planeTransformation=new PlaneTransformation(scale,0,dx, 0, scale, dy); 
                geometryGraph.Transform(planeTransformation);
            }
        }

        static void DrawEdges(GeometryGraph geometryGraph, Pen pen, Graphics graphics) {
            foreach (Edge e in geometryGraph.Edges)
                DrawEdge(e, pen, graphics);
        }

        static void DrawEdge(Edge e, Pen pen, Graphics graphics) {
            graphics.DrawPath(pen, CreateGraphicsPath(e.Curve));

            if (e.EdgeGeometry?.SourceArrowhead != null)
                DrawArrow(pen, graphics, e.Curve.Start, e.EdgeGeometry.SourceArrowhead.TipPosition);
            if (e.EdgeGeometry?.TargetArrowhead != null)
                DrawArrow(pen, graphics, e.Curve.End, e.EdgeGeometry.TargetArrowhead.TipPosition);
        }

        internal static GraphicsPath CreateGraphicsPath(ICurve iCurve) {
            GraphicsPath graphicsPath = new GraphicsPath();
            if (iCurve == null)
                return null;
            Curve c = iCurve as Curve;
            if (c != null)
            {
                foreach (ICurve seg in c.Segments)
                {
                    CubicBezierSegment cubic = seg as CubicBezierSegment;
                    if (cubic != null)
                        graphicsPath.AddBezier(PointF(cubic.B(0)), PointF(cubic.B(1)), PointF(cubic.B(2)), PointF(cubic.B(3)));
                    else
                    {
                        LineSegment ls = seg as LineSegment;
                        if (ls != null)
                            graphicsPath.AddLine(PointF(ls.Start), PointF(ls.End));

                        else
                        {
                            Ellipse el = seg as Ellipse;
                            if (el != null)
                            {
                                graphicsPath.AddArc((float)(el.Center.X - el.AxisA.X), (float)(el.Center.Y - el.AxisB.Y), (float)(el.AxisA.X * 2), Math.Abs((float)el.AxisB.Y * 2), EllipseStartAngle(el), EllipseSweepAngle(el));

                            }
                        }
                    }
                }
            }
            else {
                var ls = iCurve as LineSegment;
                if (ls != null)
                    graphicsPath.AddLine(PointF(ls.Start), PointF(ls.End));
            }

            return graphicsPath;
        }

        static PointF PointF(Point point) {
            return new PointF((float)point.X, (float)point.Y);
        }

        static void DrawArrow(Pen pen, Graphics graphics, Point start, Point end) {
            float arrowAngle = 30;

            Point dir = end - start;
            Point h = dir;
            dir /= dir.Length;

            Point s = new Point(-dir.Y, dir.X);

            s *= h.Length * ((float)Math.Tan(arrowAngle * 0.5f * (Math.PI / 180.0)));

            var points = new PointF[] { MsaglPointToDrawingPoint(start + s), MsaglPointToDrawingPoint(end), MsaglPointToDrawingPoint(start - s) };

            graphics.FillPolygon(pen.Brush, points);
        }

        public static void DrawNodes(GeometryGraph geometryGraph, Pen pen, Graphics graphics) {
            foreach (Node n in geometryGraph.Nodes)
                DrawNode(n, pen, graphics);
        }

        public static float EllipseSweepAngle(Ellipse el) {
            return (float)((el.ParEnd - el.ParStart) / Math.PI * 180);
        }

        public static float EllipseStartAngle(Ellipse el) {
            return (float)(el.ParStart / Math.PI * 180);
        }

        public static void DrawNode(Node n, Pen pen, Graphics graphics) {
            ICurve curve = n.BoundaryCurve;
            Ellipse el = curve as Ellipse;
            if (el != null) {
                graphics.DrawEllipse(pen, new RectangleF((float)el.BoundingBox.Left, (float)el.BoundingBox.Bottom,
                    (float)el.BoundingBox.Width, (float)el.BoundingBox.Height));
            } else
                graphics.DrawPath(pen, CreateGraphicsPath(curve));
        }

        public static System.Drawing.Point MsaglPointToDrawingPoint(Point point) {
            return new System.Drawing.Point((int)point.X, (int)point.Y);
        }

        public static void AddNode(string id, GeometryGraph graph, double w, double h) {
            graph.Nodes.Add(new Node(CreateCurve(w, h), id));
        }

        public static ICurve CreateCurve(double w, double h) {
            return CurveFactory.CreateRectangle(w, h, new Point()) ;
        }
    }
}