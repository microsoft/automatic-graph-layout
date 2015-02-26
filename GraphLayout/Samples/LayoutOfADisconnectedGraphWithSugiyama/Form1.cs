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
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.Layered;
using Label = Microsoft.Msagl.Core.Layout.Label;
using Node = Microsoft.Msagl.Drawing.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;

namespace LayoutOfADisconnectedGraphWithSugiyama {
    public partial class Form1 : Form {
        public Form1() {
#if DEBUG
            DisplayGeometryGraph.SetShowFunctions();
#endif

            InitializeComponent();
            SetGraph();
        }

        private void SetGraph() {
            var graph = new Graph();
            graph.AddEdge("a", "b");
            graph.AddEdge("e", "b");
            graph.AddEdge("d", "b");
            graph.AddEdge("b", "c");

            graph.AddEdge("a22", "b22");
            graph.AddEdge("e22", "b22");
            graph.AddEdge("d22", "b22");
            graph.AddEdge("b22", "c22");

            graph.AddEdge("a0", "b0");
            graph.AddEdge("b0", "c0");

            graph.AddEdge("a33", "b33");
            graph.AddEdge("e33", "b33");
            graph.AddEdge("d33", "b33");
            graph.AddEdge("b33", "c33");

            graph.AddEdge("a11", "b11");
            graph.AddEdge("b11", "c11").LabelText = "Test labels!";

            graph.CreateGeometryGraph();
            foreach (Node node in graph.Nodes)
                node.GeometryNode.BoundaryCurve = CreateLabelAndBoundary(node);

            foreach (var edge in graph.Edges) {
                if (edge.Label != null) {
                    var geomEdge = edge.GeometryEdge;
                    double width;
                    double height;
                    StringMeasure.MeasureWithFont(edge.LabelText,
                                                  new Font(edge.Label.FontName, (float)edge.Label.FontSize), out width, out height);
                    edge.Label.GeometryLabel=geomEdge.Label = new Label(width, height, geomEdge);                    
                }

            }

            var geomGraph=graph.GeometryGraph;

            var geomGraphComponents = GraphConnectedComponents.CreateComponents(geomGraph.Nodes, geomGraph.Edges);
            var settings = new SugiyamaLayoutSettings();
            foreach (var subgraph in geomGraphComponents) {
               
                var layout=new LayeredLayout(subgraph, settings);
                subgraph.Margins = settings.NodeSeparation/2;
                layout.Run();

            }

           Microsoft.Msagl.Layout.MDS.MdsGraphLayout.PackGraphs(geomGraphComponents, settings);

            geomGraph.UpdateBoundingBox();


            gViewer1.NeedToCalculateLayout = false;
            gViewer1.Graph = graph;

        }

        static ICurve CreateLabelAndBoundary(Node node) {
            node.Attr.Shape = Shape.DrawFromGeometry;
            node.Attr.LabelMargin *= 2;
            double width;
            double height;
            StringMeasure.MeasureWithFont(node.Label.Text,
                                          new Font(node.Label.FontName, (float)node.Label.FontSize), out width, out height);
            node.Label.Width = width;
            node.Label.Height = height;
            int r = node.Attr.LabelMargin;
            return CurveFactory.CreateRectangleWithRoundedCorners(width + r * 2, height + r * 2, r, r, new Point());
        }
    }
}
