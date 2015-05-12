using System;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;

namespace SettingGraphBoundsSample {
    internal delegate void MD();

    public partial class Form1 : Form {
        Graph graph;
        StatusStrip statusStrip = new StatusStrip();
        ToolTip tt = new ToolTip();

        public Form1() {
            InitializeComponent();
            CreateWideGraph();
            tt.SetToolTip(aspectRatioUpDown, "Aspect ratio of the layout");
            SuspendLayout();
            ToolStripItem toolStripLabel = new ToolStripStatusLabel();
            statusStrip.Items.Add(toolStripLabel);
            Controls.Add(statusStrip);
            ResumeLayout();
            viewer.MouseMove += GViewerMouseMove;            
        }

        void GViewerMouseMove(object sender, MouseEventArgs e) {
            float viewerX;
            float viewerY;

            viewer.ScreenToSource(e.Location.X, e.Location.Y, out viewerX, out viewerY);
            string str = String.Format(String.Format("{0},{1}", viewerX, viewerY));
            foreach (var item in statusStrip.Items) {
                var label = item as ToolStripStatusLabel;
                if (label != null) {
                    label.Text = str;
                    return;
                }
            }
        }

        void Relayout() {
            SetGraphParams();
            viewer.Graph = graph;
        }

       
        void CreateWideGraph() {
            graph = new Graph();
            for (int i = 0; i < 100; i++)
                graph.AddEdge("A", i.ToString());
        }

        void gViewer1_Load(object sender, EventArgs e) {
            SetGraphParams();
            viewer.Graph = graph;
        }

        void SetGraphParams() {
            graph.Attr.AspectRatio = (double) aspectRatioUpDown.Value;
            graph.Attr.SimpleStretch = simpleStretchCheckBox.Checked;
            graph.Attr.MinimalWidth = (double)MinWidth.Value;
            graph.Attr.MinimalHeight = (double)MinHeight.Value;
        }

        private void button1_Click(object sender, EventArgs e) {
            Relayout();
        }
    }
}