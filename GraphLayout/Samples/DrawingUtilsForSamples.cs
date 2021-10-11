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
            SetGraphTransform(geometryGraph, clientRectangle, graphics);
            var pen = new Pen(Brushes.Black);
            DrawNodes(geometryGraph, pen, graphics);
            DrawEdges(geometryGraph, pen, graphics);
        }

        public static void SetGraphTransform(GeometryGraph geometryGraph, System.Drawing.Rectangle rectangle, Graphics graphics) {
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
                /*
                //instead of setting transormation for graphics it is possible to transform the geometry graph, just to test that GeometryGraph.Transform() works
            
                var planeTransformation=new PlaneTransformation(scale,0,dx, 0, scale, dy); 
                geometryGraph.Transform(planeTransformation);
                */
                graphics.Transform = new Matrix((float)scale, 0, 0, (float)scale, (float)dx, (float)dy);
            }
        }

        static void DrawEdges(GeometryGraph geometryGraph, Pen pen, Graphics graphics) {
            foreach (Edge e in geometryGraph.Edges)
                DrawEdge(e, pen, graphics);
        }

        static void DrawEdge(Edge e, Pen pen, Graphics graphics) {
            graphics.DrawPath(pen, Microsoft.Msagl.GraphViewerGdi.Draw.CreateGraphicsPath(e.Curve));

            if (e.EdgeGeometry != null && e.EdgeGeometry.SourceArrowhead != null )
                DrawArrow(pen, graphics, e.Curve.Start, e.EdgeGeometry.SourceArrowhead.TipPosition);
            if (e.EdgeGeometry != null && e.EdgeGeometry.TargetArrowhead != null)
                DrawArrow(pen, graphics, e.Curve.End, e.EdgeGeometry.TargetArrowhead.TipPosition);
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
                graphics.DrawPath(pen, Microsoft.Msagl.GraphViewerGdi.Draw.CreateGraphicsPath(curve));
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