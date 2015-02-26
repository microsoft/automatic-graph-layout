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