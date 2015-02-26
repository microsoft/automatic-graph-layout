/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
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