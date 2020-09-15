using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;
using Color = Microsoft.Msagl.Drawing.Color;
using Node = Microsoft.Msagl.Drawing.Node;
using Shape = Microsoft.Msagl.Drawing.Shape;
#if TEST_MSAGL
using Microsoft.Msagl.GraphViewerGdi;
#endif 
namespace Editing {
    public partial class Form1 : Form {
        bool helpIsShown;
       
        public Form1() {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            graphEditor = new GraphEditor();
            InitializeComponent();
            graphEditor.AddNodeType("Ellipse", Shape.Ellipse, Color.Transparent, Color.Black, 10, "user data",
                                    "New Node");
            graphEditor.AddNodeType("Square", Shape.Box, Color.Transparent, Color.Black, 6, "user data", "");
            graphEditor.AddNodeType("Double Circle", Shape.DoubleCircle, Color.Transparent, Color.Black, 6, "user data",
                                    "New Node");
            graphEditor.AddNodeType("Diamond", Shape.Diamond, Color.Transparent, Color.Black, 6, "user data", "New Node");
            graphEditor.Viewer.NeedToCalculateLayout = true;
            CreateGraph();
            graphEditor.Viewer.NeedToCalculateLayout = false;
            SuspendLayout();
            helpButton.BringToFront();
            graphEditor.Viewer.LayoutAlgorithmSettingsButtonVisible = false;
            ResumeLayout();

            helpButton.Click += helpButton_Click;
#if TEST_MSAGL
            //   Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
        }


       
        void helpButton_Click(object sender, EventArgs e) {
            if (!helpIsShown) {
                helpIsShown = true;
                var helpForm = new HelpForm();
                var readMe = new StreamReader("ReadMe.txt");

                helpForm.RichTectBox.Text = readMe.ReadToEnd();
                helpForm.Show();
                helpForm.FormClosed += delegate { helpIsShown = false; };
            }
        }

        void CreateGraph() {
            var g = new Graph();

            // First DNA sequence
            g.AddEdge("prom1A", "rbs1A");
            g.AddEdge("rbs1A", "pcr1A");
            g.AddEdge("pcr1A", "rbs2A");
            g.AddEdge("rbs2A", "pcr2A");
            g.AddEdge("pcr2A", "ter1A");
            g.AddEdge("ter1A", "prom2A");
            g.AddEdge("prom2A", "rbs3A");
            g.AddEdge("rbs3A", "pcr3A");
            g.AddEdge("pcr3A", "ter2A");
            g.AddEdge("ter2A", "prom3A");
            g.AddEdge("prom3A", "rbs4A");
            g.AddEdge("rbs4A", "pcr4A");
            g.AddEdge("pcr4A", "ter3A");

            // Second DNA sequence
            g.AddEdge("prom1B", "rbs1B");
            g.AddEdge("rbs1B", "pcr1B");
            g.AddEdge("pcr1B", "ter1B");
            g.AddEdge("ter1B", "prom2B");
            g.AddEdge("prom2B", "rbs2B");
            g.AddEdge("rbs2B", "pcr2B");
            g.AddEdge("pcr2B", "rbs3B");
            g.AddEdge("rbs3B", "pcr3B");
            g.AddEdge("pcr3B", "ter2B");

            // Protein coding
            g.AddEdge("pcr1A", "prot_Q2b");
            g.AddEdge("pcr2A", "prot_Q1a");
            g.AddEdge("pcr3A", "prot_A");
            g.AddEdge("pcr4A", "prot_ccdB");
            g.AddEdge("pcr1B", "prot_ccdB");
            g.AddEdge("pcr2B", "prot_Q1b");
            g.AddEdge("pcr3B", "prot_Q2a");

            // Regulation
            g.AddEdge("prot_Q2b-H2", "prom2A");
            g.AddEdge("prot_H1-Q1b", "prom1B");
            
            // Reactions
            g.AddEdge("prot_Q2b", "r1");
            g.AddEdge("prot_H2", "r1");
            g.AddEdge("r1", "prot_Q2b-H2");
            g.AddEdge("prot_Q2b-H2", "r2");
            g.AddEdge("r2", "prot_H2");
            g.AddEdge("r2", "prot_Q2b");
            g.AddEdge("prot_H1", "r3");
            g.AddEdge("prot_Q1a", "r4");
            g.AddEdge("r4", "prot_H1");
            g.AddEdge("prot_H1", "r5");
            g.AddEdge("prot_Q1a", "r6");
            g.AddEdge("prot_ccdB", "r6");
            g.AddEdge("prot_H2", "r7");
            g.AddEdge("prot_H2", "r8");
            g.AddEdge("prot_Q2a", "r9");
            g.AddEdge("r9", "prot_H2");
            g.AddEdge("r10", "prot_H1");
            g.AddEdge("r10", "prot_Q1b");
            g.AddEdge("prot_H1-Q1b", "r10");
            g.AddEdge("prot_A", "r11");
            g.AddEdge("prot_ccdB", "r11");
            g.AddEdge("prot_Q1b", "r12");
            g.AddEdge("prot_H1", "r12");
            g.AddEdge("r12", "prot_H1-Q1b");
            g.AddEdge("prot_Q2a", "r13");
            g.AddEdge("prot_ccdB", "r13");

          
            // Set DNA sequences to be one above the other
            g.LayerConstraints.AddUpDownConstraint(Dn(g,"prom1A"), Dn(g,"prom1B"));

            // Set DNA sequence 1 to be in a row
            g.LayerConstraints.AddSameLayerNeighbors(Dn(g, "prom1A"),Dn(g,"rbs1A"),Dn(g,"pcr1A" ),Dn(g,"rbs2A" ),Dn(g,"pcr2A" ),Dn(g,"ter1A" ),
                Dn(g,"prom2A" ),Dn(g,"rbs3A" ),Dn(g,"pcr3A" ),Dn(g,"ter2A" ),Dn(g,"prom3A" ),Dn(g,"rbs4A" ),Dn(g,"pcr4A" ),Dn(g,"ter3A" ));

            // Set DNA sequence 2 to be in a row
            g.LayerConstraints.AddSameLayerNeighbors(Dn(g, "prom1B"), Dn(g, "rbs1B"), Dn(g, "pcr1B"), Dn(g, "ter1B"), Dn(g, "prom2B"), Dn(g, "rbs2B"),
                Dn(g, "pcr2B"), Dn(g, "rbs3B"), Dn(g, "pcr3B"), Dn(g, "ter2B"));
            
            // Set proteins to be below their pcr
            g.LayerConstraints.AddSequenceOfUpDownVerticalConstraint(Dn(g, "pcr1A"), Dn(g, "prot_Q2b"), Dn(g, "prot_A"), Dn(g, "prot_ccdB"));
            g.LayerConstraints.AddUpDownVerticalConstraint(Dn(g,"pcr2A"), Dn(g,"prot_Q1a"));
            g.LayerConstraints.AddUpDownConstraint(Dn(g,"pcr3A"), Dn(g,"prot_A"));
            g.LayerConstraints.AddUpDownConstraint(Dn(g,"pcr4A"), Dn(g,"prot_ccdB"));
            g.LayerConstraints.AddUpDownConstraint(Dn(g,"pcr1B"), Dn(g,"prot_ccdB"));
            g.LayerConstraints.AddUpDownConstraint(Dn(g,"pcr2B"), Dn(g,"prot_Q1b"));
            g.LayerConstraints.AddUpDownConstraint(Dn(g,"pcr3B"), Dn(g,"prot_Q2a"));
            /////////////////
            

            
            graphEditor.Graph = g;

        }

        static Node Dn(Graph g, string s) {
            return g.FindNode(s);
        }
    }
}