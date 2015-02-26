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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Routing;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Shape = Microsoft.Msagl.Drawing.Shape;

namespace FastIncrementalLayoutWithGdi {
    public partial class Form1 : Form {
        readonly GViewer gViewer = new GViewer();
        public Form1() {
#if DEBUG
            DisplayGeometryGraph.SetShowFunctions();
#endif
            InitializeComponent();
            SuspendLayout();
            Controls.Add(gViewer);
            gViewer.Dock = DockStyle.Fill;
            ResumeLayout();
            CreatClusteredLayout();


        }

        void CreatClusteredLayout() {
            FastIncrementalLayoutSettings settings;
            Graph drawingGraph = CtreateDrawingGraph(out settings);
            var geometryGraph = drawingGraph.GeometryGraph;
            FillClustersAndSettings(settings, geometryGraph);
            (new InitialLayout(geometryGraph, settings)).Run();
            geometryGraph.UpdateBoundingBox();

           

#if DEBUG
            DisplayGeometryGraph.BindGeomGraphToDrawingGraph(drawingGraph, geometryGraph);
            drawingGraph.DebugCurves = GetClusterBounds(geometryGraph.RootCluster.Clusters).ToArray();
            
#endif

            //LayoutAlgorithmSettings.Show(geometryGraph.Nodes.Select(node => node.BoundaryCurve).ToArray());

            var splineRouter=new SplineRouter(geometryGraph, settings.NodeSeparation/6, settings.NodeSeparation/6, Math.PI/6  );
            splineRouter.Run();
           
            gViewer.NeedToCalculateLayout = false;
            gViewer.Graph = drawingGraph;
            gViewer.ZoomF = 1;
        }
#if DEBUG
        static List<DebugCurve> GetClusterBounds(IEnumerable<Cluster> listOfClusters){
            var ret = new List<DebugCurve>();
            foreach (var cluster in listOfClusters){
                FillClusterBounds(ret, cluster);
            }
            return ret;
        }

        static void FillClusterBounds(List<DebugCurve> curves, Cluster cluster){
            foreach(var cl in cluster.Clusters)
                FillBoundsUnderCluster(curves, cl);
        }

        static Microsoft.Msagl.Core.Geometry.Rectangle FillBoundsUnderCluster(List<DebugCurve> curves, Cluster cluster){
            var b = Microsoft.Msagl.Core.Geometry.Rectangle.CreateAnEmptyBox();
            foreach (var node in cluster.Nodes)
                if (b.IsEmpty)
                    b = node.BoundingBox;
                else
                    b.Add(node.BoundingBox);

            foreach (var cl in cluster.Clusters){
                var clb = FillBoundsUnderCluster(curves, cl);
                if (b.IsEmpty)
                    b = clb;
                else
                    b.Add(clb);
            }
            b.Pad(0.5);
            curves.Add(new DebugCurve(100,1,"blue",CurveFactory.CreateRectangle(b.Width,b.Height, b.Center)));
            return b;
        }
        
#endif

        static void FillClustersAndSettings(FastIncrementalLayoutSettings settings, GeometryGraph geometryGraph) {
            settings.AvoidOverlaps = true;
            // settings.RectangularClusters = true;
            var root = new Cluster();
            var cluster = new Cluster();
            root.AddChild(cluster);

            for (int i = 0; i < 4; i++) {
                var istring = i.ToString();
                cluster.AddChild(geometryGraph.Nodes.First(n => n.UserData.ToString() == istring));
            }
            cluster.BoundaryCurve = cluster.BoundingBox.Perimeter();
            cluster = new Cluster();
            root.AddChild(cluster);
            geometryGraph.RootCluster.AddChild(root);



            //make a subcluster 
            var parent = cluster;
            cluster = new Cluster();
            cluster.AddChild(geometryGraph.Nodes.First(n => n.UserData.ToString() == "4"));
            cluster.AddChild(geometryGraph.Nodes.First(n => n.UserData.ToString() == "5"));
            parent.AddChild(cluster);
            

            cluster = new Cluster();
            for (int i = 6; i < 9; i++) {
                var istring = i.ToString();
                cluster.AddChild(geometryGraph.Nodes.First(n => n.UserData.ToString() == istring));
            }
            parent.AddChild(cluster);
            foreach (var cl in geometryGraph.RootCluster.AllClustersDepthFirst()) {
                if(cl.BoundaryCurve==null)
                    cl.BoundaryCurve=cl.BoundingBox.Perimeter();
                
            }
        }

        static Graph CtreateDrawingGraph(out FastIncrementalLayoutSettings settings) {
            settings = new FastIncrementalLayoutSettings { RouteEdges = true, NodeSeparation = 30};
            var drawingGraph = new Graph();
            const string id0 = "0";
            const string id1 = "1";
            AddEdge(drawingGraph, id0, id1);
            AddEdge(drawingGraph, "0", "2");
            AddEdge(drawingGraph, "1", "3");
            AddEdge(drawingGraph, "2", "4");
            AddEdge(drawingGraph, "2", "5");
            AddEdge(drawingGraph, "2", "6");
            AddEdge(drawingGraph, "5", "7");
            AddEdge(drawingGraph, "5", "6");
            AddEdge(drawingGraph, "7", "8");
            AddEdge(drawingGraph, "8", "6");

       
            drawingGraph.CreateGeometryGraph();
            foreach (Microsoft.Msagl.Drawing.Node node in drawingGraph.Nodes) {
                double w, h;
                var label = node.Label;
                var font = new Font(label.FontName, (float)label.FontSize);
                StringMeasure.MeasureWithFont(label.Text, font, out w, out h);
                node.Label.Width = w;
                node.Label.Height = h;
                node.Attr.Shape = Shape.DrawFromGeometry;
                node.GeometryNode.BoundaryCurve=CurveFactory.CreateRectangleWithRoundedCorners(1.2*w,1.2*h,3,3,new Point());
            }

            return drawingGraph;
        }

        static void AddEdge(Graph drawingGraph, string id0, string id1) {
            var edge = drawingGraph.AddEdge(id0, id1);
    //        edge.Attr.ArrowheadAtTarget = ArrowStyle.None;
            edge.Attr.Length = 6;
        }
    }
}

