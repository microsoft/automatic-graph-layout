using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Routing;
using P = Microsoft.Msagl.Core.Geometry.Point;

namespace UsingMdsLayoutSample {
    public partial class Form1 : Form {
        GeometryGraph gleeGraph;
        public Form1() {
            InitializeComponent();
            this.SizeChanged += new EventHandler(Form1_SizeChanged);
        }

        void Form1_SizeChanged(object sender, EventArgs e) {
            this.Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {

            base.OnPaint(e);
            if (gleeGraph == null)
                gleeGraph = CreateAndLayoutGraph();



            DrawFromGraph(e.Graphics);
        }

        private void DrawFromGraph(Graphics graphics) {
            SetGraphicsTransform(graphics);
            Pen pen = new Pen(Brushes.Black);
            DrawNodes(pen,graphics);
            DrawEdges(pen,graphics);
        }

        private void SetGraphicsTransform(Graphics graphics) {
            RectangleF r = this.ClientRectangle;
            var gr = this.gleeGraph.BoundingBox;
            if (r.Height > 1 && r.Width > 1) {
                float scale = Math.Min(r.Width / (float)gr.Width, r.Height / (float)gr.Height);
                float g0 = (float)(gr.Left + gr.Right) / 2;
                float g1 = (float)(gr.Top + gr.Bottom) / 2;

                float c0 = (r.Left + r.Right) / 2;
                float c1 = (r.Top + r.Bottom) / 2;
                float dx = c0 - scale * g0;
                float dy = c1 + scale * g1;
                graphics.Transform = new System.Drawing.Drawing2D.Matrix(scale, 0, 0, -scale, dx, dy);
            }
        }

        private void DrawEdges( Pen pen, Graphics graphics) {
            foreach (Edge e in gleeGraph.Edges)
                DrawEdge(e, pen, graphics);
        }

        private void DrawEdge(Edge e, Pen pen, Graphics graphics) {
            ICurve curve = e.Curve;
            Curve c = curve as Curve;
            if (c != null) {
                foreach (ICurve s in c.Segments) {
                    LineSegment l = s as LineSegment;
                    if (l != null)
                        graphics.DrawLine(pen, MsaglPointToDrawingPoint(l.Start), MsaglPointToDrawingPoint(l.End));
                    CubicBezierSegment cs = s as CubicBezierSegment;
                    if (cs != null)
                        graphics.DrawBezier(pen, MsaglPointToDrawingPoint(cs.B(0)), MsaglPointToDrawingPoint(cs.B(1)), MsaglPointToDrawingPoint(cs.B(2)), MsaglPointToDrawingPoint(cs.B(3)));

                }
                if (e.ArrowheadAtSource)
                    DrawArrow(e, pen, graphics, e.Curve.Start, e.EdgeGeometry.SourceArrowhead.TipPosition);
                if (e.ArrowheadAtTarget)
                    DrawArrow(e, pen, graphics, e.Curve.End, e.EdgeGeometry.TargetArrowhead.TipPosition);
            } else {
                var l=curve as LineSegment;
                if (l != null)
                    graphics.DrawLine(pen, MsaglPointToDrawingPoint(l.Start), MsaglPointToDrawingPoint(l.End));
            }
        }

        private void DrawArrow(Edge e, Pen pen, Graphics graphics, P start, P end) {
            PointF[] points;
            float arrowAngle = 30;

            P dir = end - start;
            P h = dir;
            dir /= dir.Length;

            P s = new P(-dir.Y, dir.X);

            s *= h.Length * ((float)Math.Tan(arrowAngle * 0.5f * (Math.PI / 180.0)));

            points = new PointF[] { MsaglPointToDrawingPoint(start + s), MsaglPointToDrawingPoint(end), MsaglPointToDrawingPoint(start - s) };

            graphics.FillPolygon(pen.Brush, points);
        }

       
        private void DrawNodes(Pen pen, Graphics graphics) {
            foreach (Node n in gleeGraph.Nodes)
                DrawNode(n, pen, graphics);
        }

        private void DrawNode(Node n, Pen pen, Graphics graphics) {
            ICurve curve = n.BoundaryCurve;
            Ellipse el = curve as Ellipse;
            if (el != null) {
                graphics.DrawEllipse(pen, new RectangleF((float)el.BoundingBox.Left, (float)el.BoundingBox.Bottom,
                    (float)el.BoundingBox.Width, (float)el.BoundingBox.Height));
            } else {
                Curve c = curve as Curve;
                foreach (ICurve seg in c.Segments) {
                    LineSegment l=seg as LineSegment;
                    if(l!=null)
                        graphics.DrawLine(pen, MsaglPointToDrawingPoint(l.Start),MsaglPointToDrawingPoint(l.End));
                }
            }
        }

        private System.Drawing.Point MsaglPointToDrawingPoint(P point) {
            return new System.Drawing.Point((int)point.X, (int)point.Y);
        }

        static internal GeometryGraph CreateAndLayoutGraph() {
            double w = 30;
            double h = 20;
            GeometryGraph graph = new GeometryGraph();
            Node a = new Node( new Ellipse(w, h, new P()),"a");
            Node b = new Node( CurveFactory.CreateRectangle(w, h, new P()),"b");
            Node c = new Node( CurveFactory.CreateRectangle(w, h, new P()),"c");
            Node d = new Node(CurveFactory.CreateRectangle(w, h, new P()), "d");

            graph.Nodes.Add(a);
            graph.Nodes.Add(b);
            graph.Nodes.Add(c);
            graph.Nodes.Add(d);
            Edge e = new Edge(a, b) { Length = 10 };
            graph.Edges.Add(e);
            graph.Edges.Add(new Edge(b, c) { Length = 3 });
            graph.Edges.Add(new Edge(b, d) { Length = 4 });
          
            //graph.Save("c:\\tmp\\saved.msagl");
            var settings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();
            LayoutHelpers.CalculateLayout(graph, settings, null);

            return graph;
        }
    }
}