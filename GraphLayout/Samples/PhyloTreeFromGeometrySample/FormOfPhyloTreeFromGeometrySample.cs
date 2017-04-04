using System;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CommonDrawingUtilsForSamples;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Prototype.Phylo;
using Microsoft.Msagl.Prototype.Ranking;
using Microsoft.Msagl.Routing;

namespace PhyloTreeFromGeometrySample
{
    public partial class FormOfPhyloTreeFromGeometrySample : Form
    {
        GeometryGraph _geometryGraph;
        public FormOfPhyloTreeFromGeometrySample()
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


        internal static GeometryGraph CreateAndLayoutGraph() {
            PhyloTree phyloTree = new PhyloTree();
            double width = 40;
            double height = 10;

            foreach (string id in "A B C D E F G".Split(' '))
                DrawingUtilsForSamples.AddNode(id, phyloTree, width, height);

            PhyloEdge e;
            double age_of_BC = 2;
            double age_of_D = 3.5;
            double age_of_F = 1.5;
            double age_of_G = 3.5;
            double age_of_E = 2;

            phyloTree.Edges.Add(e = new PhyloEdge(phyloTree.FindNodeByUserData("A"), phyloTree.FindNodeByUserData("B")));
            e.Length = age_of_BC;
            phyloTree.Edges.Add(e = new PhyloEdge(phyloTree.FindNodeByUserData("A"), phyloTree.FindNodeByUserData("C")));
            e.Length = age_of_BC;
            phyloTree.Edges.Add(e = new PhyloEdge(phyloTree.FindNodeByUserData("A"), phyloTree.FindNodeByUserData("D")));
            e.Length = age_of_D;
            phyloTree.Edges.Add(e = new PhyloEdge(phyloTree.FindNodeByUserData("C"), phyloTree.FindNodeByUserData("E")));
            e.Length = age_of_E;
            phyloTree.Edges.Add(e = new PhyloEdge(phyloTree.FindNodeByUserData("C"), phyloTree.FindNodeByUserData("F")));
            e.Length = age_of_F;
            phyloTree.Edges.Add(e = new PhyloEdge(phyloTree.FindNodeByUserData("C"), phyloTree.FindNodeByUserData("G")));
            e.Length = age_of_G;
            var sugiyamaLayoutSettings = new SugiyamaLayoutSettings();
            foreach (var edge in phyloTree.Edges) {
                edge.EdgeGeometry.TargetArrowhead = new Arrowhead();
            }
            Microsoft.Msagl.Miscellaneous.LayoutHelpers.CalculateLayout(phyloTree, new SugiyamaLayoutSettings(), null);

            // add a couple of  non-tree edges
            Edge e0 = new Edge(phyloTree.FindNodeByUserData("F"), phyloTree.FindNodeByUserData("D")) {
                EdgeGeometry = {SourceArrowhead = new Arrowhead()}
            };
            phyloTree.Edges.Add(e0);
            Edge e1 = new Edge(phyloTree.FindNodeByUserData("G"), phyloTree.FindNodeByUserData("D")) {
                EdgeGeometry = {SourceArrowhead = new Arrowhead()}
            };
            phyloTree.Edges.Add(e1);

            // route the non-tree edges, every other edge is routed already
            double loosePadding = sugiyamaLayoutSettings.NodeSeparation/10;
            double tightPadding = sugiyamaLayoutSettings.NodeSeparation/10;
            double coneAngle = Math.PI/6;
            var router = new SplineRouter(phyloTree, new[] {e0, e1}, tightPadding, loosePadding, coneAngle, null);
            router.Run();
            return phyloTree;
        }
    }
}
