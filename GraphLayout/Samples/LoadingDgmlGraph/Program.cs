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
﻿

using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;

namespace LoadingDgmlGraph {
    class Program {
        static void Main(string[] args) {
#if DEBUG
            Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            //create a form
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            //create a viewer object
            Microsoft.Msagl.GraphViewerGdi.GViewer viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();

            
            //associate the viewer with the form
            form.SuspendLayout();
            viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            form.Controls.Add(viewer);
            form.ResumeLayout();
            //create a graph object
            Graph graph = DgmlParser.DgmlParser.Parse("fullstring.dgml");

            SugiyamaLayoutSettings ss = graph.LayoutAlgorithmSettings as SugiyamaLayoutSettings;

            // uncomment this line to see the wide graph
            // ss.MaxAspectRatioEccentricity = 100; 

            // uncommment this line to us Mds
            // ss.FallbackLayoutSettings = new MdsLayoutSettings {AdjustScale = true}; 
            
            
            // or uncomment the following line to use the default layering layout with vertical layer
            // graph.Attr.LayerDirection = LayerDirection.LR;
            
            viewer.Graph = graph;
            form.ShowDialog();
        }
    }
}
