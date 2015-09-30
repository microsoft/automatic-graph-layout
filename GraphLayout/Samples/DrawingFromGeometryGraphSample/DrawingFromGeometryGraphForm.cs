using System;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CommonDrawingUtilsForSamples;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Edge = Microsoft.Msagl.Core.Layout.Edge;

namespace DrawingFromGeometryGraphSample
{
    public partial class DrawingFromGeometryGraphForm : Form {
        GeometryGraph _geometryGraph;
        public DrawingFromGeometryGraphForm() {
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
            if (_geometryGraph == null) {
                _geometryGraph = CreateAndLayoutGraph();
            }
            DrawingUtilsForSamples.DrawFromGraph(ClientRectangle, _geometryGraph, e.Graphics);
        }


        static internal GeometryGraph CreateAndLayoutGraph() {
            GeometryGraph graph = new GeometryGraph();

            double width = 40;
            double height = 10;

            foreach (string id in "0 1 2 3 4 5 6 A B C D E F G a b c d e".Split(' '))
            {
                DrawingUtilsForSamples.AddNode(id, graph, width, height);
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

            var settings = new SugiyamaLayoutSettings {
                Transformation = PlaneTransformation.Rotation(Math.PI/2),
                EdgeRoutingSettings = {EdgeRoutingMode = EdgeRoutingMode.Spline}
            };
            var layout = new LayeredLayout(graph, settings);
            layout.Run();
            return graph;
        }
    }
}
