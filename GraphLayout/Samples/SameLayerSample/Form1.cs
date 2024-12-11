using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace SameLayerSample
{
    [SupportedOSPlatform("windows")]
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            GViewer gViewer = new GViewer() { Dock = DockStyle.Fill };
            SuspendLayout();
            Controls.Add(gViewer);
            ResumeLayout();
            Graph graph = new Graph();
            var sugiyamaSettings = (SugiyamaLayoutSettings)graph.LayoutAlgorithmSettings;
            sugiyamaSettings.NodeSeparation *= 2;
            graph.AddEdge("A", "B");
            graph.AddEdge("A", "C");
            graph.AddEdge("A", "D");
            //graph.LayerConstraints.PinNodesToSameLayer(new[] { graph.FindNode("A"), graph.FindNode("B"), graph.FindNode("C") });
            graph.LayerConstraints.AddSameLayerNeighbors(graph.FindNode("A"), graph.FindNode("B"), graph.FindNode("C"));
            gViewer.Graph = graph;

        }
    }
}
