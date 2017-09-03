using System;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CommonDrawingUtilsForSamples;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;

namespace LayerConstraintsFromGeometrySample
{
    public partial class LayerConstraintsFromGeometrySampleForm : Form
    {
        GeometryGraph _geometryGraph;
        static readonly Random RandomGenerator = new Random(1);
        public LayerConstraintsFromGeometrySampleForm()
        {
            //   Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
            InitializeComponent();
            SizeChanged += Form1_SizeChanged;
            // the magic calls for invoking doublebuffering
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        void Form1_SizeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            base.OnPaint(e);
            if (_geometryGraph == null)
            {
                _geometryGraph = CreateAndLayoutGraph();
            }
            DrawingUtilsForSamples.DrawFromGraph(ClientRectangle, _geometryGraph, e.Graphics);
        }


        internal static GeometryGraph CreateAndLayoutGraph()
        {
            double w = 40;
            double h = 10;
            GeometryGraph graph = new GeometryGraph();
            // columns
                        var col0 = new[] { "a", "b", "c" };
            var col1 = new[] { "d", "e", "f", "g" };
            var col2 = new[] { "k", "l", "m", "n" };
            var col3 = new[] { "w", "y", "z" };

            var settings = new SugiyamaLayoutSettings();

            foreach (var id in col0)
                DrawingUtilsForSamples.AddNode(id, graph, w, h);
            foreach (var id in col1)
                DrawingUtilsForSamples.AddNode(id, graph, w, h);
            foreach (var id in col2)
                DrawingUtilsForSamples.AddNode(id, graph, w, h);
            foreach (var id in col3)
                DrawingUtilsForSamples.AddNode(id, graph, w, h);

            //pinning columns
            settings.PinNodesToSameLayer(col0.Select(s => graph.FindNodeByUserData(s)).ToArray());
            settings.PinNodesToSameLayer(col1.Select(s => graph.FindNodeByUserData(s)).ToArray());
            settings.PinNodesToSameLayer(col2.Select(s => graph.FindNodeByUserData(s)).ToArray());
            settings.PinNodesToSameLayer(col3.Select(s => graph.FindNodeByUserData(s)).ToArray());

            AddEdgesBetweenColumns(col0, col1, graph);
            AddEdgesBetweenColumns(col1, col2, graph);
            AddEdgesBetweenColumns(col2, col3, graph);
            // rotate layer to columns
            settings.Transformation = PlaneTransformation.Rotation(Math.PI/2);  
            settings.NodeSeparation = 5;
            settings.LayerSeparation = 100;
            var ll = new LayeredLayout(graph, settings);
            ll.Run();
            return graph;

        }
        static void AddEdgesBetweenColumns(string[] col0, string[] col1, GeometryGraph graph)
        {
            foreach (var id in col0)
            {
                Edge edge = new Edge(graph.FindNodeByUserData(id),
                    graph.FindNodeByUserData(col1[RandomGenerator.Next(col1.Length)])) {
                        EdgeGeometry = {TargetArrowhead = new Arrowhead()}
                    };
                graph.Edges.Add(edge);
                edge = new Edge(graph.FindNodeByUserData(id), graph.FindNodeByUserData(col1[RandomGenerator.Next(col1.Length)]));
                graph.Edges.Add(edge);

            }
        }
    }
}
