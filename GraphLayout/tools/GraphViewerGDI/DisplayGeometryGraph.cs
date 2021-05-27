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
using Microsoft.Msagl.Core.DataStructures;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using System;
#if TEST_MSAGL
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Size=System.Drawing.Size;
using GeomNode = Microsoft.Msagl.Core.Layout.Node;

namespace Microsoft.Msagl.GraphViewerGdi{
    /// <summary>
    /// draws curved for debugging purpoposes
    /// </summary>
    public class DisplayGeometryGraph{
        
        ///<summary>
        ///</summary>
        ///<param name="geomGraph"></param>
        static public void ShowGraph(GeometryGraph geomGraph) {
            var graph = new Graph();
            geomGraph.UpdateBoundingBox();
            var bb = geomGraph.BoundingBox;
            bb.Pad(geomGraph.Margins);
            geomGraph.BoundingBox = bb;
            BindGeomGraphToDrawingGraph(graph, geomGraph);
            DisplayGraph(graph, new Form());
        }

      

        ///<summary>
        ///</summary>
        ///<param name="graph"></param>
        ///<param name="geomGraph"></param>
        static public void BindGeomGraphToDrawingGraph(Graph graph, GeometryGraph geomGraph) {
            graph.GeometryGraph = geomGraph;
            var nodeIds = new Dictionary<GeomNode, string>();
            BindNodes(graph, geomGraph, nodeIds);

            BindClusters(graph, geomGraph, nodeIds);

            BindEdges(graph, geomGraph, nodeIds);
        }

        static void BindEdges(Graph graph, GeometryGraph geomGraph, Dictionary<GeomNode, string> nodeIds) {
            foreach (var edge in geomGraph.Edges) {
                if (IsUnderCollapsedCluster(edge))
                    continue;

                var e = graph.AddEdge(nodeIds[edge.Source], nodeIds[edge.Target]);
                if (edge.EdgeGeometry != null && edge.EdgeGeometry.SourceArrowhead != null)
                    e.Attr.ArrowheadAtSource = ArrowStyle.Normal;

                e.Attr.ArrowheadAtTarget = edge.EdgeGeometry != null && edge.EdgeGeometry.TargetArrowhead != null
                    ? ArrowStyle.Normal
                    : ArrowStyle.None;

                e.GeometryEdge = edge;

                e.Attr.LineWidth = edge.LineWidth;

                if (edge.Label != null && edge.Label.Width != 0) {
                    e.LabelText = "label";
                    e.Label.GeometryLabel = edge.Label;
                    e.Label.Owner = e;
                }
            }
        }

        static bool IsUnderCollapsedCluster(Edge edge) {
            return IsUnderCollapsedCluster(edge.Source) || IsUnderCollapsedCluster(edge.Target);

        }

        static void BindClusters(Graph graph, GeometryGraph geomGraph, Dictionary<GeomNode, string> nodeIds) {
            foreach (var cluster in geomGraph.RootCluster.AllClustersWidthFirstExcludingSelfAvoidingChildrenOfCollapsed()) {
                string id = nodeIds.Count.ToString();
                var n = graph.AddNode(id);
                n.GeometryNode = cluster;
                n.Attr.Color = Color.RosyBrown;

                if (cluster.BoundaryCurve != null) {
                    n.Label.Width = cluster.Width/2;
                    n.Label.Owner = n;
                    n.Label.Height = cluster.Height/2;

                    n.Label.Owner = n;

                    n.LabelText = cluster.DebugId != null
                        ? cluster.DebugId.ToString().Substring(0, Math.Min(5, cluster.DebugId.ToString().Length))
                        : cluster.UserData == null
                            ? ""
                            : cluster.UserData.ToString().Substring(0, Math.Min(5, cluster.UserData.ToString().Length));
                }
                nodeIds[cluster] = id;
            }
        }

        static void BindNodes(Graph graph, GeometryGraph geomGraph, Dictionary<GeomNode, string> nodeIds) {
            foreach (var node in geomGraph.Nodes) {
                if (IsUnderCollapsedCluster(node)) continue;
                string id = nodeIds.Count.ToString();
                var n = graph.AddNode(id);
                n.GeometryNode = node;
                n.Attr.Color = new Color(50, 100, 100, 0);
                n.Attr.Shape = Shape.DrawFromGeometry;

                n.Label.Width = node.Width;
                n.Label.Owner = n;
                n.Label.Height = node.Height;

                n.LabelText = node.DebugId != null
                    ? node.DebugId.ToString().Substring(0, Math.Min(5, node.DebugId.ToString().Length))
                    : node.UserData == null
                        ? id
                        : node.UserData.ToString().Substring(0, Math.Min(5, node.UserData.ToString().Length));
                nodeIds[node] = id;
            }
        }

        static bool IsUnderCollapsedCluster(GeomNode node) {
            if (node.AllClusterAncestors.Any(cl => cl.IsCollapsed))
                return true;
            return false;
        }

        /// <summary>
        /// displays an array of curves
        /// </summary>
        /// <param name="curves"></param>
        public static void ShowCurves(params ICurve[] curves){
            var g = new Graph("");
            ShowCurvesWithColorsSet(curves, g, new Form());
        }

        private static void ShowCurvesWithColorsSet(IEnumerable<ICurve> curves,
                                                    Graph g, Form f){
            AllocateDebugCurves(g);
            //   g.ShowControlPoints = true;

            var graphBox = new Rectangle();

            AddCurvesToGraph(curves, g);
            bool firstTime = true;
            foreach (ICurve c0 in curves){
                if (c0 != null){
                    Parallelogram b = c0.ParallelogramNodeOverICurve.Parallelogram;

                    for (int i = 0; i < 4; i++){
                        if (firstTime){
                            firstTime = false;
                            graphBox = new Rectangle(b.Vertex((VertexId) i));
                        }
                        graphBox.Add(b.Vertex((VertexId) i));
                    }
                }
            }

            Point del = (graphBox.LeftBottom - graphBox.RightTop)/10;
            if (del.X == 0)
                del.X = 1;
            if (del.Y == 0)
                del.Y = 1;
            graphBox.Add(graphBox.LeftBottom + del);
            graphBox.Add(graphBox.RightTop - del);
            var gg = new GeometryGraph{BoundingBox = graphBox};
            g.GeometryGraph = gg;
            try{
                DisplayGraph(g, f);
            }
            catch (Exception e){
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// display colorored curves
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="colors"></param>
        public static void ShowCurvesWithColors(IEnumerable<ICurve> curves, IEnumerable<string> colors){
            var g = new Graph("");
            AddColorsToGraph(colors, g);
            
            ShowCurvesWithColorsSet(curves, g, new Form());
        }

        private static void AddColorsToGraph(IEnumerable<string> colors, Graph g){
            g.DebugColors = new List<Color>();
            foreach (string s in colors){
                if (s.ToLower() == "green")
                    g.DebugColors.Add(Color.Green);
                else if (s.ToLower() == "black")
                    g.DebugColors.Add(Color.Black);
                else if (s.ToLower() == "red")
                    g.DebugColors.Add(Color.Red);
                else if (s.ToLower() == "blue"){
                    g.DebugColors.Add(Color.Blue);
                }
            }
        }

        private static void AllocateDebugCurves(Graph g){
            g.DebugICurves = new List<ICurve>();
        }

        private static void AddCurvesToGraph(IEnumerable<ICurve> curves, Graph g){
            g.DebugICurves.AddRange(curves);
        }


        /// <summary>
        /// displays the database
        /// </summary>
        /// <param name="db"></param>
        /// <param name="curves"></param>
        public static void ShowDataBase(Database db, params ICurve[] curves){
            var g = new Graph("");
            AllocateDebugCurves(g);

            var graphBox = new Rectangle(db.Anchors[0].LeftTop);

            var cl = new List<ICurve>(curves);

            foreach (Anchor a in db.Anchors){
                graphBox.Add(a.LeftTop);
                graphBox.Add(a.RightBottom);
                cl.Add(a.PolygonalBoundary);
            }

            AddCurvesToGraph(cl, g);

            Point del = (graphBox.LeftBottom - graphBox.RightTop)/10;
            graphBox.Add(graphBox.LeftBottom + del);
            graphBox.Add(graphBox.RightTop - del);
            var gg = new GeometryGraph{BoundingBox = graphBox};
            g.DataBase = db;
            g.GeometryGraph = gg;
            
            DisplayGraph(g, new Form());
            db.nodesToShow = null;
        }


        private static void DisplayGraph(Graph g, Form form){
            var gviewer = new GViewer{BuildHitTree = false};
            form.SuspendLayout();
            form.Controls.Add(gviewer);
            gviewer.Dock = DockStyle.Fill;
            
            
            var b = new Button {Text = "Save DebugCurves"};
            b.Click += BClick;
            b.Left = Screen.PrimaryScreen.WorkingArea.Size.Width*3/8;
            b.AutoSize = true;
            b.ForeColor = System.Drawing.Color.Blue;
            form.Controls.Add(b);

            var l = new System.Windows.Forms.Label() {Text = "no object"};
            l.Name = "label";
            l.Dock = DockStyle.Right;
            l.AutoSize = true;
            form.Controls.Add(l);
            gviewer.SendToBack();


          
            var statusStrip = new StatusStrip();
            var toolStribLbl = new ToolStripStatusLabel("test");
            statusStrip.Items.Add(toolStribLbl);
            form.Controls.Add(statusStrip);
            form.ResumeLayout();
            gviewer.ObjectUnderMouseCursorChanged += DisplayGeometryGraph_ObjectUnderMouseCursorChanged;
            gviewer.NeedToCalculateLayout = false;
            gviewer.MouseClick += GviewerMouseClick;
           
            form.Size = new Size(Screen.PrimaryScreen.WorkingArea.Size.Width*3/4,
                              Screen.PrimaryScreen.WorkingArea.Size.Height*3/4);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.TopLevel = true;            
            gviewer.Graph = g;

            form.Text = Process.GetCurrentProcess().MainModule.FileName;

            form.ShowDialog();
        }

        static void DisplayGeometryGraph_ObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e) {
            var gv = (GViewer)sender;
            Form form = (Form) gv.Parent;
            System.Windows.Forms.Label label = null;
            foreach (var l in form.Controls) {
                label = l as System.Windows.Forms.Label;
                if (label != null)
                    break;
            }

            var selObj = gv.SelectedObject;
            if (selObj == null)
                label.Text = "no object";
            else
                label.Text = selObj.ToString();
            
        }

        static void BClick(object sender, EventArgs e){
            var button = sender as Button;
            GViewer gViewer = null;
            foreach (var f in (button.Parent as Form).Controls){
                gViewer = f as GViewer;
                if(gViewer!=null)
                    break;
            }
            if(gViewer==null){
                MessageBox.Show("need to fix the debugging tool, sorry");
                return;
            }
                
            if(gViewer.Graph == null){
                MessageBox.Show("graph is not set");
                return;
            }
            if(gViewer.Graph.DebugCurves == null){
                MessageBox.Show("DebugCurves are not set");
                return;
            }

            var fileDialog = new SaveFileDialog();
            if(fileDialog.ShowDialog() == DialogResult.OK)
                DebugCurveCollection.WriteToFile(gViewer.Graph.DebugCurves, fileDialog.FileName);

        }



        private static void GviewerMouseClick(object sender, MouseEventArgs e){
            var gviewer = sender as GViewer;
            if (gviewer != null){
                float viewerX;
                float viewerY;
                gviewer.ScreenToSource(e.Location.X, e.Location.Y, out viewerX, out viewerY);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void SetShowFunctions(){
            LayoutAlgorithmSettings.Show = new Show(ShowCurves);
            LayoutAlgorithmSettings.ShowDatabase = new ShowDatabase(ShowDataBase);
            LayoutAlgorithmSettings.ShowDebugCurves=new ShowDebugCurves(ShowDebugCurves);
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration = new ShowDebugCurvesEnumeration(ShowDebugCurvesEnumeration);
            LayoutAlgorithmSettings.ShowGraph = ShowGraph;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="debugCurves"></param>
        static public void ShowDebugCurves(params DebugCurve[] debugCurves){
            var g = new Graph("");
            ShowShapesOnGraph(debugCurves, g);
        }

        static  void ShowDebugCurvesOnForm(DebugCurve[] debugCurves, Form f) {
            var g = new Graph("");
            ShowShapesOnGraphWithForm(debugCurves, g, f);
        }

        static void ShowDebugCurvesEnumeration(IEnumerable<DebugCurve> debugCurves) {
            ShowDebugCurves(new List<DebugCurve>(debugCurves).ToArray());
        }

        ///<summary>
        ///</summary>
        ///<param name="debugCurves"></param>
        ///<param name="f"></param>
        static public void ShowDebugCurvesEnumerationOnForm(IEnumerable<DebugCurve> debugCurves, Form f) {
            ShowDebugCurvesOnForm(debugCurves.ToArray(), f);
        }
        static void ShowShapesOnGraph(DebugCurve[] debugCurves, Graph graph) {
            FillGraph(graph, debugCurves);
            try {
                DisplayGraph(graph, new Form());
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }
        
        static void ShowShapesOnGraphWithForm(DebugCurve[] debugCurves, Graph graph, Form f) {
            FillGraph(graph, debugCurves);
            try {
                DisplayGraph(graph, f);
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        static void FillGraph(Graph graph, DebugCurve[] debugCurves) {
            var gg = new GeometryGraph { DebugCurves = debugCurves};
            gg.Margins = 5;
            graph.GeometryGraph = gg;
            
            gg.UpdateBoundingBox();

           
          
        }
    }
}
#endif