using System;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;

namespace PhyloTreeSampleOverGDI {
    public partial class Form1 : Form {
        GViewer viewer = new GViewer();

        public Form1() {
            InitializeComponent();
            SuspendLayout();
            Controls.Add(viewer);
            viewer.Dock = DockStyle.Fill;
            viewer.LayoutAlgorithmSettingsButtonVisible = false;
            ResumeLayout();
        }

        void button1_Click(object sender, EventArgs e) {
            var tree = new PhyloTree();
            var edge = (PhyloEdge) tree.AddEdge("a", "b");
            //edge.Length = 0.8;
            edge = (PhyloEdge) tree.AddEdge("a", "c");
            //edge.Length = 0.2;
            tree.AddEdge("c", "d");
            tree.AddEdge("c", "e");
            tree.AddEdge("c", "f");
            tree.AddEdge("e", "0");
            tree.AddEdge("e", "1");
            tree.AddEdge("e", "2");
   
            viewer.Graph = tree;
        }
    }
}