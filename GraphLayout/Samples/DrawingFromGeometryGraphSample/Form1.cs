using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using P = Microsoft.Msagl.Core.Geometry.Point;
using System.Drawing.Drawing2D;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Node = Microsoft.Msagl.Core.Layout.Node;

namespace DrawingFromMsaglGraph {
    public partial class Form1 : Form {
        GeometryGraph geometryGraph;
        static Random r = new Random(1);
        public Form1() {
            //   Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
            InitializeComponent();
            SizeChanged += Form1_SizeChanged;
            // the magic calls for invoking doublebuffering
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        void Form1_SizeChanged(object sender, EventArgs e) {
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            base.OnPaint(e);
            if (geometryGraph == null) {
                geometryGraph = CreateAndLayoutGraph();
            }
            DrawFromGraph(e.Graphics);
        }

        private void DrawFromGraph(Graphics graphics) {
            SetGraphTransform();
            var pen = new Pen(Brushes.Black);
            DrawNodes(pen, graphics);
            DrawEdges(pen, graphics);
        }

        private void SetGraphTransform() {
            //instead of setting transormation for graphics we are going to transform the geometry graph, just to test that GeometryGraph.Transform() works
            RectangleF clientRectangle = ClientRectangle;
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

        private void DrawEdges(Pen pen, Graphics graphics) {
            foreach (Edge e in geometryGraph.Edges)
                DrawEdge(e, pen, graphics);
        }

        private void DrawEdge(Edge e, Pen pen, Graphics graphics) {
            graphics.DrawPath(pen, CreateGraphicsPath(e.Curve));

            if (e.EdgeGeometry != null && e.EdgeGeometry.SourceArrowhead != null)
                DrawArrow(e, pen, graphics, e.Curve.Start, e.EdgeGeometry.SourceArrowhead.TipPosition);
            if (e.EdgeGeometry != null && e.EdgeGeometry.TargetArrowhead != null)
                DrawArrow(e, pen, graphics, e.Curve.End, e.EdgeGeometry.TargetArrowhead.TipPosition);
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

        private static System.Drawing.PointF PointF(Microsoft.Msagl.Core.Geometry.Point point) {
            return new System.Drawing.PointF((float)point.X, (float)point.Y);
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
            foreach (Node n in geometryGraph.Nodes)
                DrawNode(n, pen, graphics);
        }
        private static float EllipseSweepAngle(Ellipse el) {
            return (float)((el.ParEnd - el.ParStart) / Math.PI * 180);
        }

        private static float EllipseStartAngle(Ellipse el) {
            return (float)(el.ParStart / Math.PI * 180);
        }

        private void DrawNode(Node n, Pen pen, Graphics graphics) {
            ICurve curve = n.BoundaryCurve;
            Ellipse el = curve as Ellipse;
            if (el != null) {
                graphics.DrawEllipse(pen, new RectangleF((float)el.BoundingBox.Left, (float)el.BoundingBox.Bottom,
                    (float)el.BoundingBox.Width, (float)el.BoundingBox.Height));
            } else
                graphics.DrawPath(pen, CreateGraphicsPath(curve));
        }

        private System.Drawing.Point MsaglPointToDrawingPoint(P point) {
            return new System.Drawing.Point((int)point.X, (int)point.Y);
        }

        static internal GeometryGraph CreateAndLayoutGraph() {
            GeometryGraph graph = new GeometryGraph();

            double width = 40;
            double height = 10;

            foreach (string id in "0 1 2 3 4 5 6 A B C D E F G a b c d e".Split(' '))
            {
                AddNode(id, graph, width, height);
            }

            graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("B")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("C")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("D")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("E")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("B"), graph.FindNodeByUserData("E")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("0"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("1"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("2"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("3"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("4"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("5"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("6"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("a"), graph.FindNodeByUserData("b")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("b"), graph.FindNodeByUserData("c")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("c"), graph.FindNodeByUserData("d")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("d"), graph.FindNodeByUserData("e")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("B"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("C"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("E"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("F"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("G"), graph.FindNodeByUserData("a")));

            var settings = new SugiyamaLayoutSettings();
            settings.Transformation = PlaneTransformation.Rotation(Math.PI/2);
            settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Spline;
            var layout = new LayeredLayout(graph, settings);
            layout.Run();
            return graph;
//            double w = 40;
//            double h = 10;
//            GeometryGraph graph = new GeometryGraph();
            //columns
//            var col0 = new[] { "a", "b", "c" };
//            var col1 = new[] { "d", "e", "f", "g" };
//            var col2 = new[] { "k", "l", "m", "n" };
//            var col3 = new[] { "w", "y", "z" };
//            
//            var settings = new SugiyamaLayoutSettings();
//            
//            foreach (var id in col0)
//                AddNode(id, graph, w, h);
//            foreach (var id in col1)
//                AddNode(id, graph, w, h);
//            foreach (var id in col2)
//                AddNode(id, graph, w, h);
//            foreach (var id in col3)
//                AddNode(id, graph, w, h);
//
            //pinning columns
//            settings.PinNodesToSameLayer(col0.Select(s=>graph.FindNodeByUserData(s)).ToArray());
//            settings.PinNodesToSameLayer(col1.Select(s => graph.FindNodeByUserData(s)).ToArray());
//            settings.PinNodesToSameLayer(col2.Select(s => graph.FindNodeByUserData(s)).ToArray());
//            settings.PinNodesToSameLayer(col3.Select(s => graph.FindNodeByUserData(s)).ToArray());
//
//            AddEdgesBetweenColumns(col0, col1, graph);
//            AddEdgesBetweenColumns(col1, col2, graph);
//            AddEdgesBetweenColumns(col2, col3, graph);
            //rotate layer to columns
           // graph.Transformation = PlaneTransformation.Rotation(Math.PI / 2);
//            settings.NodeSeparation = 5;
//            settings.LayerSeparation = 100;
//            var ll = new LayeredLayout(graph, settings);
//            ll.Run();
//            return graph;
        }

        private static void AddNode(string id, GeometryGraph graph, double w, double h) {
            graph.Nodes.Add(new Node(CreateCurve(w, h), id));
        }

        private static ICurve CreateCurve(double w, double h) {
            return CurveFactory.CreateRectangle(w, h, new Microsoft.Msagl.Core.Geometry.Point()) ;
        }


        static void AddEdgesBetweenColumns(string[] col0, string[] col1, GeometryGraph graph) {
            foreach (var id in col0) {
                Edge edge = new Edge(graph.FindNodeByUserData(id), graph.FindNodeByUserData(col1[r.Next(col1.Length)]));
                edge.EdgeGeometry.TargetArrowhead = null;
                graph.Edges.Add(edge);
                edge = new Edge(graph.FindNodeByUserData(id), graph.FindNodeByUserData(col1[r.Next(col1.Length)]));
                graph.Edges.Add(edge);

            }
        }
    }
}
