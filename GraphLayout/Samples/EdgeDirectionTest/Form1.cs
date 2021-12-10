using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing;

namespace EdgeDirectionTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        { // 
            var edgeAttr = gv.LayoutEditor.EdgeAttr;
            edgeAttr.ArrowheadAtSource = ArrowStyle.None;
            edgeAttr.ArrowheadAtTarget = ArrowStyle.None;
            ((IViewer)gv).MouseDown += new EventHandler<MsaglMouseEventArgs>(viewer_MouseDown);
            Graph gr = new Graph();
            gr.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = Microsoft.Msagl.Core.Routing.EdgeRoutingMode.StraightLine;
            Node n1 = new Node("1");
            Node n2 = new Node("2");
            Node n3 = new Node("3");
            Node n4 = new Node("4");
            gr.AddNode(n1);
            gr.AddNode(n2);
            gr.AddNode(n3);
            gr.AddNode(n4);
            Edge edge12 = gr.AddEdge("1", "2");
            edge12.LabelText = "33";
            edge12.Attr.ArrowheadAtSource = ArrowStyle.None;
            edge12.Attr.ArrowheadAtTarget = ArrowStyle.None;
            gv.Graph = gr;
        }

        private void viewer_MouseDown(object sender, MsaglMouseEventArgs e)
        {
            if(e.RightButtonIsPressed)
            {
                Microsoft.Msagl.Core.Geometry.Point p = gv.ScreenToSource(e.X, e.Y);
                Node n = new Node((gv.Graph.NodeCount+1).ToString());
                IViewerNode iwn = gv.CreateIViewerNode(n, p, true);
                gv.AddNode(iwn, true);
            }
        }

        private void gv_EdgeAdded(object sender, EventArgs e)
        {
            var ed = (Edge)sender;
            gv.SetEdgeLabel(ed, new Microsoft.Msagl.Drawing.Label("3p"));
            // change the default EdgeAttr of LayoutEditor
            gv.LayoutEditor.EdgeAttr.ArrowheadAtSource = ArrowStyle.Normal;
            gv.LayoutEditor.EdgeAttr.ArrowheadAtTarget = ArrowStyle.Normal;
            if (gv.LayoutEditor.EdgeAttr.ArrowheadLength > 5)
                gv.LayoutEditor.EdgeAttr.ArrowheadLength /= 2;

        }
    }
}
