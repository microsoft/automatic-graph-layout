

using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;

namespace LoadingDgmlGraph {
    class Program {
        static void Main(string[] args) {
#if TEST_MSAGL
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
﻿#if GraphModel

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
#endif
				}
    }
}
