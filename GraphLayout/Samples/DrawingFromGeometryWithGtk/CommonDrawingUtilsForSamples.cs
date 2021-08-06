using System;
using System.Drawing;
using System.Linq;

using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

using Point = Microsoft.Msagl.Core.Geometry.Point;

namespace CommonDrawingUtilsForSamples
{
    public class DrawingUtilsForSamples {
        public static void DrawFromGraph(Cairo.Rectangle clientRectangle, GeometryGraph geometryGraph, Cairo.Context context) {
            SetGraphTransform(geometryGraph, clientRectangle, context);
            var pen = new Cairo.Color(0, 0, 0);
            context.SetSourceColor(pen);
            DrawNodes(geometryGraph, pen, context);
            DrawEdges(geometryGraph, pen, context);
        }

        public static void SetGraphTransform(GeometryGraph geometryGraph, Cairo.Rectangle clientRectangle, Cairo.Context context) {
            var gr = geometryGraph.BoundingBox;
            var matrix = CalculateTransformMatrix(gr, clientRectangle);
            if (matrix is not null)
            {
                context.Transform(matrix);
            }
        }

        public static Cairo.Matrix CalculateTransformMatrix(Microsoft.Msagl.Core.Geometry.Rectangle geometryRectangle, Cairo.Rectangle clientRectangle)
        {
            if (clientRectangle.Height > 1 && clientRectangle.Width > 1) {
                var scale = Math.Min(clientRectangle.Width * 0.9 / geometryRectangle.Width, clientRectangle.Height * 0.9 / geometryRectangle.Height);
                var g0 = (geometryRectangle.Left + geometryRectangle.Right) / 2;
                var g1 = (geometryRectangle.Top + geometryRectangle.Bottom) / 2;

                var c0 = (clientRectangle.X + (clientRectangle.X + clientRectangle.Width)) / 2;
                var c1 = (clientRectangle.Y + (clientRectangle.Y + clientRectangle.Height)) / 2;
                var dx = c0 - scale * g0;
                var dy = c1 - scale * g1;
                /*
                //instead of setting transormation for graphics it is possible to transform the geometry graph, just to test that GeometryGraph.Transform() works
            
                var planeTransformation=new PlaneTransformation(scale,0,dx, 0, scale, dy); 
                geometryGraph.Transform(planeTransformation);
                //*/
                return new Cairo.Matrix((float)scale, 0, 0, (float)scale, (float)dx, (float)dy);
            }
            return new Cairo.Matrix(1,0,0,1,0,0);
        }

        static void DrawEdges(GeometryGraph geometryGraph, Cairo.Color pen, Cairo.Context graphics) {
            foreach (Edge e in geometryGraph.Edges)
                DrawEdge(e, pen, graphics);
        }

        static void DrawEdge(Edge e, Cairo.Color pen, Cairo.Context graphics) {
            //graphics.DrawPath(pen, CreateGraphicsPath(e.Curve));
            if (e.Curve == null)
            {
                return;
            }

            graphics.SetSourceColor(pen);
            DrawGraphicsPath(graphics, e.Curve);
            graphics.Stroke();

            if (e.EdgeGeometry != null && e.EdgeGeometry.SourceArrowhead != null )
                DrawArrow(pen, graphics, e.Curve.Start, e.EdgeGeometry.SourceArrowhead.TipPosition);
            if (e.EdgeGeometry != null && e.EdgeGeometry.TargetArrowhead != null)
                DrawArrow(pen, graphics, e.Curve.End, e.EdgeGeometry.TargetArrowhead.TipPosition);
        }

        internal static void DrawBezier(Cairo.Context context, CubicBezierSegment bezierSegment)
        {
            context.MoveTo(bezierSegment.B(0).X, bezierSegment.B(0).Y);
            context.CurveTo(bezierSegment.B(1).X, bezierSegment.B(1).Y, bezierSegment.B(2).X, bezierSegment.B(2).Y, bezierSegment.B(3).X, bezierSegment.B(3).Y);
            context.Stroke();
        }

        internal static void DrawGraphicsPath(Cairo.Context context, ICurve iCurve)
        {
            if (iCurve == null)
                return;

            switch (iCurve)
            {
                case Curve curve:
                    foreach (ICurve seg in curve.Segments)
                        DrawGraphicsPath(context, seg);
                    break;
                case RoundedRect rr:
                    DrawGraphicsPath(context, rr.Curve);
                    break;
                case CubicBezierSegment cubic:
                    DrawBezier(context, cubic);
                    break;
                case LineSegment ls:
                    context.MoveTo(ls.Start.X, ls.Start.Y);
                    context.LineTo(ls.End.X, ls.End.Y);
                    context.Stroke();
                    break;
                case Ellipse el:
                    DrawEllipse(context, el, el.ParStart, el.ParEnd);
                    break;
                default:
                    Console.WriteLine($"Encountered: {iCurve.GetType()}");
                    throw new Exception($"Encountered: {iCurve.GetType()}");
            }

        }

        static void DrawArrow(Cairo.Color pen, Cairo.Context graphics, Point start, Point end) {
            float arrowAngle = 30;

            Point dir = end - start;
            Point h = dir;
            dir /= dir.Length;

            Point s = new(-dir.Y, dir.X);

            s *= h.Length * ((float)Math.Tan(arrowAngle * 0.5f * (Math.PI / 180.0)));

            var points = new Cairo.Point[] { MsaglPointToDrawingPoint(start + s), MsaglPointToDrawingPoint(end), MsaglPointToDrawingPoint(start - s) };

            //graphics.FillPolygon(pen.Brush, points);
            graphics.SetSourceColor(pen);
            var first = points.First();
            graphics.MoveTo(first.X, first.Y);
            foreach (var point in points.Skip(1))
            {
                graphics.LineTo(point.X, point.Y);
            }
            graphics.LineTo(first.X, first.Y);
            graphics.Fill();
        }

        public static void DrawNodes(GeometryGraph geometryGraph, Cairo.Color pen, Cairo.Context graphics) {
            foreach (Node n in geometryGraph.Nodes)
                DrawNode(n, pen, graphics);
        }

        public static void DrawNode(Node n, Cairo.Color pen, Cairo.Context context) {
            var color = new Cairo.Color(0,0,0);
            context.SetSourceColor(color);
            ICurve curve = n.BoundaryCurve;
            Ellipse el = curve as Ellipse;
            if (el != null) {
                DrawEllipse(context, el);
            } 
            else
            {
                // graphics.DrawPath(pen, CreateGraphicsPath(curve));
                DrawGraphicsPath(context, curve);
            }
            if (n.UserData != null)
            {
                var font = Pango.FontDescription.FromString("sans 8");
                var text = (string)n.UserData;
                var textSize = MeasureTextSize(font, text);
                context.Save();
                var layout = Pango.CairoHelper.CreateLayout(context);
                layout.FontDescription = font;
                layout.SetText(text);
                context.SetSourceColor(new Cairo.Color(0.5, 0.5, 0.5));
                context.MoveTo(n.Center.X - textSize.X/2, n.Center.Y - textSize.Y/2);
                Pango.CairoHelper.ShowLayout(context, layout);
                context.Restore();
            }
            context.Stroke();
        }

        internal static Cairo.Point MeasureTextSize(Pango.FontDescription font, string text)
        {
            var label = new Gtk.Label(text);
            var layout = label.CreatePangoLayout(text);
            layout.FontDescription = font;
            int width, height;
            layout.GetPixelSize(out width, out height);
            return new Cairo.Point(width, height);
        }

        internal static void DrawEllipse(Cairo.Context context, Ellipse el, double startAngle = 0, double endAngle = Math.PI * 2)
        {
            var radius = Math.Max(el.AxisA.Length, el.AxisB.Length);
            var scaleX = el.AxisA.Length / radius;
            var scaleY = el.AxisB.Length / radius;
            context.Save();
            context.Scale(scaleX, scaleY);
            context.Arc(el.Center.X, el.Center.Y, radius, startAngle, endAngle);
            context.Stroke();
            context.Restore();
        }

        public static Cairo.Point MsaglPointToDrawingPoint(Point point) {
            return new Cairo.Point((int)point.X, (int)point.Y);
        }

        public static void AddNode(string id, GeometryGraph graph, double w, double h) {
            graph.Nodes.Add(new Node(CreateCurve(w, h), id));
        }

        public static ICurve CreateCurve(double w, double h) {
            return CurveFactory.CreateRectangle(w, h, new Point()) ;
        }
    }
}
