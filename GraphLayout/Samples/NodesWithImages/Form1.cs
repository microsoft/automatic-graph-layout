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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Color = System.Drawing.Color;
using P2 = Microsoft.Msagl.Core.Geometry.Point;
using GeomNode = Microsoft.Msagl.Core.Layout.Node;
using GeomEdge = Microsoft.Msagl.Core.Layout.Edge;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;

namespace NodesWithImages {
    public partial class Form1 : Form {
        Image creek;
        Image leaves;
        Image tree;
        Image waterfall;
        GViewer viewer = new GViewer();

        public Form1() {
#if DEBUG
            DisplayGeometryGraph.SetShowFunctions();
#endif
            InitializeComponent();
            creek = Image.FromFile("Creek.jpg");
            leaves = Image.FromFile("Autumn Leaves.jpg");
            tree = Image.FromFile("tree.jpg");
            waterfall = Image.FromFile("waterfall.jpg");
            SuspendLayout();
            this.Controls.Add(viewer);
            viewer.Dock = DockStyle.Fill;
            ResumeLayout();
            viewer.LayoutAlgorithmSettingsButtonVisible = false;
            InitGraph();

        }

        ICurve GetNodeBoundary(Microsoft.Msagl.Drawing.Node node) {
            Image image = ImageOfNode(node);
            double width = image.Width;
            double height = image.Height;

            return CurveFactory.CreateRectangleWithRoundedCorners(width, height, width * radiusRatio, height * radiusRatio, new P2());
        }

        bool DrawNode(DrawingNode node, object graphics) {
            Graphics g = (Graphics)graphics;
            Image image = ImageOfNode(node);

            //flip the image around its center
            using (System.Drawing.Drawing2D.Matrix m = g.Transform)
            {
                using (System.Drawing.Drawing2D.Matrix saveM = m.Clone())
                {

                    g.SetClip(FillTheGraphicsPath(node.GeometryNode.BoundaryCurve));
                    using (var m2 = new System.Drawing.Drawing2D.Matrix(1, 0, 0, -1, 0, 2 * (float)node.GeometryNode.Center.Y))
                        m.Multiply(m2);

                    g.Transform = m;
                    g.DrawImage(image, new PointF((float)(node.GeometryNode.Center.X - node.GeometryNode.Width / 2),
                        (float)(node.GeometryNode.Center.Y - node.GeometryNode.Height / 2)));
                    g.Transform = saveM;

                }
            }

            return true;//returning false would enable the default rendering
        }

        private Image ImageOfNode(DrawingNode node) {
            Image image;
            if (node.Id == leavesId)
                image = leaves;
            else if (node.Id == creekId)
                image = creek;
            else if (node.Id == wId)
                image = waterfall;
            else
                image = tree;
            return image;
        }


        static System.Drawing.Drawing2D.GraphicsPath FillTheGraphicsPath( ICurve iCurve) {
            var curve = ((RoundedRect)iCurve).Curve;
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            foreach (ICurve seg in curve.Segments)
                AddSegmentToPath(seg, ref path);
            return path;
        }

        private static void AddSegmentToPath(ICurve seg, ref System.Drawing.Drawing2D.GraphicsPath p) {
            const float radiansToDegrees = (float)(180.0 / Math.PI);                    
            LineSegment line = seg as LineSegment;
            if (line != null)
                p.AddLine(PointF(line.Start), PointF(line.End));
            else {
                CubicBezierSegment cb = seg as CubicBezierSegment;
                if (cb != null)
                    p.AddBezier(PointF(cb.B(0)), PointF(cb.B(1)), PointF(cb.B(2)), PointF(cb.B(3)));
                else {
                    Ellipse ellipse = seg as Ellipse;
                    if (ellipse != null)
                        p.AddArc((float)(ellipse.Center.X - ellipse.AxisA.Length), (float)(ellipse.Center.Y - ellipse.AxisB.Length),
                            (float)(2 * ellipse.AxisA.Length), (float)(2 * ellipse.AxisB.Length), (float)(ellipse.ParStart * radiansToDegrees),
                            (float)((ellipse.ParEnd - ellipse.ParStart) * radiansToDegrees));

                }
            }
        }

        static internal PointF PointF(P2 p) { return new PointF((float)p.X, (float)p.Y); }

        float radiusRatio = 0.3f;
        

        string leavesId = "leaves";
        string creekId = "creek";
        string treeId = "tree";
        string wId = "waterfall";

        private void InitGraph() {
            Graph drawingGraph = new Graph();
           
            drawingGraph.AddEdge(leavesId, creekId);
            drawingGraph.AddEdge(leavesId, treeId);
            drawingGraph.AddEdge(leavesId, wId);

            foreach (DrawingNode node in drawingGraph.Nodes) {
                node.Attr.Shape = Shape.DrawFromGeometry;
                node.DrawNodeDelegate = new DelegateToOverrideNodeRendering(DrawNode);
                node.NodeBoundaryDelegate = new DelegateToSetNodeBoundary(GetNodeBoundary);
            }

            double width = leaves.Width;
            double height = leaves.Height;

            drawingGraph.Attr.LayerSeparation = height / 2;
            drawingGraph.Attr.NodeSeparation = width / 2;
            double arrowHeadLenght = width / 10;
            foreach (Microsoft.Msagl.Drawing.Edge e in drawingGraph.Edges)
                e.Attr.ArrowheadLength = (float)arrowHeadLenght;
            drawingGraph.LayoutAlgorithmSettings = new SugiyamaLayoutSettings();
            viewer.Graph = drawingGraph;
        }
    }
}
