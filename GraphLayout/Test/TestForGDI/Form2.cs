using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Dot2Graph;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using Microsoft.Msagl.Routing.Spline.Bundling;
using AglNode = Microsoft.Msagl.Core.Layout.Node;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Label = Microsoft.Msagl.Drawing.Label;
using Node = Microsoft.Msagl.Drawing.Node;
using Path = System.IO.Path;

namespace TestForGdi {
    
    internal partial class Form2 : Form {
        public bool VerbosePar { get; set; }
        readonly StatusStrip statusStrip;
        readonly ToolTip toolTip = new ToolTip();
        readonly bool verbose;
        double cornerFitRadius = 3;
        Graph generatedGraph;
        string lastFileName;
        int offsetInDemo;
        IEnumerator<object> searchEnumerator;

        CancelToken currentCancelToken;

        // GViewer gViewer = new GViewer();

        readonly ToolStripStatusLabel toolStripLbl = new ToolStripStatusLabel("test");
            
        public Form2(bool verbosePar) {
            VerbosePar = verbosePar;
            InitializeComponent();
            verbose = verbosePar;

            
//            rectilinearToolStripMenuItem.Checked = edgeRoutingMode == EdgeRoutingMode.Rectilinear;
//            rectilinearToCenterToolStripMenuItem.Checked=edgeRoutingMode == EdgeRoutingMode.RectilinearToCenter;
            aspectRatio.Maximum = 1000000;
            AcceptButton = searchButton;
            searchTextBox.TextChanged += SearchTextBoxTextChanged;
            gViewer.ObjectUnderMouseCursorChanged +=
                GViewerObjectUnderMouseCursorChanged;
            SuspendLayout();
            gViewer.Dock = DockStyle.Fill;
            statusStrip = new StatusStrip();
            statusStrip.Items.Add(toolStripLbl);
       
                

            Controls.Add(statusStrip);
            ResumeLayout();
            Invalidate();
            ContextMenu = new ContextMenu();
            demoPauseValueNumericValue.Value = 2;
            //  this.ContextMenu.Popup+=new EventHandler(ContextMenu_Popup);
            toolTip.SetToolTip(nodeSeparMult, "Multiplier for node separation");
            toolTip.SetToolTip(layerSeparationMult, "Multiplier for layer separation");
            toolTip.SetToolTip(aspectRatio, "Aspect Ratio value");

            minimumRoutingOffset.Minimum = 0.1m;
            minimumRoutingOffset.Maximum = 0.4m;
            minimumRoutingOffset.Value = (decimal) gViewer.TightOffsetForRouting;
            minimumRoutingOffset.Increment = 0.1m;
            minimumRoutingOffset.ValueChanged += MinimumRoutingOffsetValueChanged;
            minimumRoutingOffset.DecimalPlaces = 3;

            looseObstaclesOffset.Minimum = 0.1m;
            looseObstaclesOffset.Maximum = 0.6m;
            looseObstaclesOffset.Value = (decimal) gViewer.LooseOffsetForRouting;
            looseObstaclesOffset.Increment = 0.1m;
            looseObstaclesOffset.ValueChanged += LooseObstaclesOffsetValueChanged;
            looseObstaclesOffset.DecimalPlaces = 3;

            routingRelaxOffset.Minimum = 0.1m;
            routingRelaxOffset.Maximum = 1.6m;
            routingRelaxOffset.Value = (decimal) gViewer.OffsetForRelaxingInRouting;
            routingRelaxOffset.Increment = 0.1m;
            routingRelaxOffset.ValueChanged += RoutingRelaxOffsetValueChanged;
            routingRelaxOffset.DecimalPlaces = 3;

            graphBorderSize.Value = 0;


            toolTip.SetToolTip(demoPauseValueNumericValue, "Demo pause value");

            layerSeparationMult.Minimum = 0.1m;
            nodeSeparMult.Minimum = 0.1m;
            nodeSeparMult.Increment = 0.5m;
            layerSeparationMult.Value = 1;


            layerSeparationMult.Increment = 0.5M;

            gViewer.KeyDown += GViewerKeyDown;

            gViewer.AsyncLayout = true;

            gViewer.MouseMove += GViewerMouseMove;
            
        }

        public GViewer GViewer {
            get { return gViewer; }

            set { gViewer = value; }
        }

        public string DemoFileName { get; set; }

        public Thread GraphLayoutCalculationThread { get; set; }

        public Graph GeneratedGraph {
            get { return generatedGraph; }
            set { generatedGraph = value; }
        }

        public double CornerFitRadius {
            get { return cornerFitRadius; }
            set { cornerFitRadius = value; }
        }

        void GViewerMouseMove(object sender, MouseEventArgs e) {
            float viewerX;
            float viewerY;
            gViewer.ScreenToSource(e.Location.X, e.Location.Y, out viewerX, out viewerY);
            var str = String.Format(String.Format("{0},{1}", viewerX, viewerY));
            foreach (var item in statusStrip.Items) {
                var label = item as ToolStripStatusLabel;
                if (label == null) continue;
                label.Text =str;
                return;
            }
        }


        void RoutingRelaxOffsetValueChanged(object sender, EventArgs e) {
            gViewer.OffsetForRelaxingInRouting = (double) routingRelaxOffset.Value;
        }

        void LooseObstaclesOffsetValueChanged(object sender, EventArgs e) {
            gViewer.LooseOffsetForRouting = (double) looseObstaclesOffset.Value;
        }

        void MinimumRoutingOffsetValueChanged(object sender, EventArgs e) {
            gViewer.TightOffsetForRouting = (double) minimumRoutingOffset.Value;
        }

        void GViewerObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e) {
            IViewerObject underCursor = gViewer.ObjectUnderMouseCursor;
            object sel = underCursor == null ? null : underCursor.DrawingObject;
            if (sel is Node) {
                var n = sel as Node;
                selection.Text = n.Id;
            }
            else if (sel is Edge) {
                var edge = sel as Edge;
                selection.Text = edge.Source + "->" + edge.Target;
            }
            else
                selection.Text = "";
        }

        void GViewerKeyDown(object sender, KeyEventArgs e) {
            
            if (e.KeyData == Keys.Space) {
                var node = gViewer.ObjectUnderMouseCursor as IViewerNode;
                if (node != null)
                    EnlargeNode(node);
            }
            foreach (IViewerObject o in (gViewer).Entities)
                gViewer.Invalidate(o);

            (gViewer as IViewer).Invalidate();
            gViewer.ClearBoundingBoxHierarchy();
        }

        void EnlargeNode(IViewerNode node) {
            var geomNode = node.DrawingObject.GeometryObject as AglNode;
            geomNode.BoundaryCurve = geomNode.BoundaryCurve.OffsetCurve(geomNode.BoundingBox.Width/2,
                                                                         new Point(1, 0));
            LayoutHelpers.IncrementalLayout(gViewer.Graph.GeometryGraph, geomNode, gViewer.Graph.LayoutAlgorithmSettings as SugiyamaLayoutSettings);
        }


        void SearchTextBoxTextChanged(object sender, EventArgs e) {
            searchButton.Text = "Search";
            if (searchEnumerator != null)
                searchEnumerator = GraphObjects().GetEnumerator();
        }


        /*
        Microsoft.Msagl.Drawing CreateGraphFromDotGraph(string dotFileName)
        {
      
      
          StreamReader sr=new StreamReader(dotFileName);
      
          string dotString= sr.ReadToEnd();
      
          sr.Close();

          DotParser.Microsoft.Msagl.Drawing g = DotParser.Microsoft.Msagl.Drawing.GraphFromDotString(dotString);

          return CreateFromDotGraph(g);



        }*/


        void ReadGraphFromFile(string fileName) {
            int eLine, eColumn;
            bool msaglFile;
            Graph graph = CreateDrawingGraphFromFile(fileName, out eLine, out eColumn , out msaglFile);
            lastFileName = fileName;
            if (graph == null)
                MessageBox.Show(String.Format("{0}({1},{2}): cannot process the file", fileName, eLine, eColumn));

            else
            {
                graph.Attr.AspectRatio = (double)aspectRatio.Value;
                graph.Attr.NodeSeparation *= (double)nodeSeparMult.Value;
                graph.Attr.LayerSeparation *= (double)layerSeparationMult.Value;
                graph.Attr.Border = (int)graphBorderSize.Value;
                var settings = graph.LayoutAlgorithmSettings as SugiyamaLayoutSettings;
                if (EdgeRoutingSettings != null && OverrideRoutingSettings)
                    graph.LayoutAlgorithmSettings.EdgeRoutingSettings = EdgeRoutingSettings;
                if (settings != null)
                {
                    //                    if(this.)
                    //                    if (leftRigthToolStripMenuItem.Checked)
                    //                        graph.Attr.LayerDirection = LayerDirection.LR;
                    //                    if (rightLeftToolStripMenuItem.Checked)
                    //                        graph.Attr.LayerDirection = LayerDirection.RL;
                    //                    if (topBottomToolStripMenuItem.Checked)
                    //                        graph.Attr.LayerDirection = LayerDirection.TB;
                    //                    if (bottomTopToolStripMenuItem.Checked)
                    //                        graph.Attr.LayerDirection = LayerDirection.RL;
                }

#if TEST_MSAGL
                graph.LayoutAlgorithmSettings.Reporting = verbose;
#endif
                gViewer.FileName = fileName;
                Stopwatch sw = null;
                if (verbose){
                    gViewer.AsyncLayout = false;
                    sw = new Stopwatch();
                    sw.Start();
				}
                gViewer.Graph = graph;
                if (sw != null)
                {
                    sw.Stop();
                    System.Diagnostics.Debug.WriteLine("layout done for {0} ms", (double)sw.ElapsedMilliseconds / 1000);
                }
            }
        }

        protected bool OverrideRoutingSettings {
            get {
                return overrideGraphRoutingSettingsToolStripMenuItem.Checked;
            }
            set {
                overrideGraphRoutingSettingsToolStripMenuItem.Checked = value;
            }
        }

        EdgeRoutingSettings EdgeRoutingSettings { get; set; }


        internal static Graph CreateDrawingGraphFromFile(string fileName, out int line, out int column, out bool msaglFile) {
            string msg;
            var graph= Parser.Parse(fileName, out line, out column, out msg);
            if (graph != null) {
                msaglFile = false;
                return graph;
            }

            try
            {
                graph = Graph.Read(fileName);
                msaglFile = true;
                return graph;
            }
            catch (Exception) {
                System.Diagnostics.Debug.WriteLine("cannot read " + fileName);
            }
            msaglFile = false;
            return null;
        }


        void DemoButtonClick(object sender, EventArgs e) {
            if (demoButton.Text == "Demo") {
                demoButton.Text = "Stop Demo";
                if (DemoFileName == null) {
                    var of = new OpenFileDialog {RestoreDirectory = true};
                    if (of.ShowDialog() == DialogResult.OK)
                        DemoFileName = of.FileName;
                }

                if (DemoFileName != null) {
                    GraphLayoutCalculationThread = new Thread(DemonstrateFile);
                    GraphLayoutCalculationThread.Start();
                }
            }
            else {
                if (GraphLayoutCalculationThread != null) {
                    GraphLayoutCalculationThread.Abort();
                    GraphLayoutCalculationThread = null;
                }
                demoButton.Text = "Demo";
            }
        }

        void UpdateViewer(object s) {
            BringToFront();
            GViewer.SetCalculatedLayout(s);
        }

        void GraphLayoutCalculation() {
            try {
                var gv = new GViewer();
                gv.CurrentLayoutMethod=this.GViewer.CurrentLayoutMethod;
                object res = gv.CalculateLayout(generatedGraph);
                double ar = generatedGraph.GeometryGraph.BoundingBox.Width/
                            generatedGraph.GeometryGraph.BoundingBox.Height;
                if (ar > 5)
                    System.Diagnostics.Debug.WriteLine("ar={0}", ar);

                //update the dotviewer
                if (InvokeRequired)
                    Invoke(new VoidFunctionWithOneParameterDelegate(UpdateViewer), new[] {res});
                else
                    UpdateViewer(res);
            }
            catch (Exception e) {
                //if it is thread abort exception then just ignore it
                if (!(e is ThreadAbortException)) {}
            }
        }

        void DemonstrateFile() {
            do {
                string fn = DemoFileName;
                var sr = new StreamReader(fn);
                string l;
                int i = 0;
                while ((l = sr.ReadLine()) != null && ContinueDemo()) {
                    if (i < offsetInDemo) {
                        i++;
                        continue;
                    }
                    i++;
                    offsetInDemo++;


                    l = l.ToLower();
                    if (l.EndsWith(".dot")) {
                        l = Path.Combine(Path.GetDirectoryName(fn), l);

                        System.Diagnostics.Debug.WriteLine("processing {0} ...", l);
                        int line;
                        int column;
                        bool msaglFile;
                        GeneratedGraph = CreateDrawingGraphFromFile(l, out line, out column,out msaglFile);
                        if (generatedGraph != null)
                        {
                            //                            if (l.Contains("102"))
                            //                                generatedGraph.Attr.LayerSeparation *= 10;
                            //                            else if (l.Contains("103"))
                            //                                generatedGraph.Attr.LayerSeparation *= 10;
                            //                            else if (l.Contains("69"))
                            //                                generatedGraph.Attr.LayerSeparation *= 5;
                            //                            else if (l.Contains("rowe"))
                            //                                generatedGraph.Attr.NodeSeparation *= 8;
                            //                            else if (l.ToLower().Contains("nan"))
                            //                                generatedGraph.Attr.LayerSeparation *= 4;
                            //

                            GraphLayoutCalculation();
                        }
                        else
                            System.Diagnostics.Debug.WriteLine(String.Format("{0}({1},{2}): cannot process the file", fn, line, column));
                    }

                    Thread.Sleep(1000*(int) demoPauseValueNumericValue.Value);
                }
                offsetInDemo = 0;
                sr.Close();
            } while (ContinueDemo());
        }

        bool ContinueDemo() {
            return demoButton.Text != "Demo";
        }

        void SearchButtonClick(object sender, EventArgs e) {
            if (GViewer.Graph != null) {
                string s = searchTextBox.Text.ToLower();
                if (searchEnumerator == null)
                    searchEnumerator = GraphObjects().GetEnumerator();

                bool textFound = false;
                while (searchEnumerator.MoveNext()) {
                    object t = searchEnumerator.Current;
                    var n = t as Node;
                    Label label = n != null ? n.Label : ((Edge) t).Label;

                    if (label != null && label.Text.ToLower().Contains(s)) {
                        GViewer.CenterToGroup(t);
                        searchButton.Text = "Next";
                        textFound = true;
                        break;
                    }
                }

                if (searchButton.Text == "Search")
                    MessageBox.Show(String.Format("'{0}' not found", s));
                else {
                    if (textFound == false) {
                        searchEnumerator = GraphObjects().GetEnumerator(); //resetting
                        searchButton.Text = "Wrap around and Search";
                    }
                }
            }
        }

        IEnumerable<object> GraphObjects() {
            foreach (Node n in GViewer.Graph.Nodes) {
                yield return n;
            }
            foreach (Edge e in GViewer.Graph.Edges)
                yield return e;
        }


        void ReloadGraph() {
            if (lastFileName != null)
                ReadGraphFromFile(lastFileName);
        }


        void CancelLayoutToolStripMenuItemClick(object sender, EventArgs e) {
            // This currently doesn't cancel all possible actions in TestForGdi.
            // All the algorithms need to be updated to populate currentCancelToken before they execute.
            if (this.currentCancelToken != null)
            {
                this.currentCancelToken.Canceled = true;
            }
        }

        void ReloadToolStripMenuItemClick(object sender, EventArgs e) {
            ReloadGraph();
        }

        void OpenDotFileToolStripMenuItemClick(object sender, EventArgs e) {
            var openFileDialog = new OpenFileDialog {
                                                        RestoreDirectory = true,
                                                        Filter = " dot files (*.dot)|*.dot|All files (*.*)|*.*"
                                                    };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
                ReadGraphFromFile(openFileDialog.FileName);
        }

        void RouteEdgesRegular() {
            if (GViewer.Graph != null) {
#if TEST_MSAGL
                gViewer.Graph.DebugICurves.Clear();
#endif
                var edgeMode = GViewer.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode;
                if (GViewer.Graph.LayoutAlgorithmSettings is SugiyamaLayoutSettings && (edgeMode == EdgeRoutingMode.SugiyamaSplines))
                    GViewer.Graph = GViewer.Graph;
                else
                    RouteGraphEdgesAsSplines();
            }
        }


        void RouteGraphEdgesAsSplines() {
            var geomGraph = gViewer.Graph.GeometryGraph;
            var layoutSettings = gViewer.Graph.LayoutAlgorithmSettings;
            EdgeRoutingSettings routingSettings = layoutSettings.EdgeRoutingSettings;
            if (routingSettings.EdgeRoutingMode == EdgeRoutingMode.StraightLine)
                RouteStraightLines();
            else
                if (routingSettings.EdgeRoutingMode == EdgeRoutingMode.Spline) {
                    var coneAngle = routingSettings.ConeAngle;
                    var padding = layoutSettings.NodeSeparation * gViewer.TightOffsetForRouting * 2;
                    this.currentCancelToken = new CancelToken();
                    var router = new SplineRouter(geomGraph, padding, 0.65 * padding, coneAngle, null);
                    router.Run(this.currentCancelToken);
                } else if (routingSettings.EdgeRoutingMode == EdgeRoutingMode.SplineBundling) {
                    var coneAngle = routingSettings.ConeAngle;
                    var padding = layoutSettings.NodeSeparation / 3;
                    var loosePadding = SplineRouter.ComputeLooseSplinePadding(layoutSettings.NodeSeparation, padding);
                    if(layoutSettings.EdgeRoutingSettings.BundlingSettings==null)
                        layoutSettings.EdgeRoutingSettings.BundlingSettings=new BundlingSettings();
                    var br = new SplineRouter(geomGraph, padding, loosePadding, coneAngle, layoutSettings.EdgeRoutingSettings.BundlingSettings);
                    br.Run();
                } else {
                    MessageBox.Show(String.Format("Mode {0} is not supported with this settings",
                                                  routingSettings.EdgeRoutingMode));
                    return;
                }

            new EdgeLabelPlacement(geomGraph).Run();
           
            InvalidateEdges();
        }

        void InvalidateEdges() {            
            foreach (var edge in gViewer.Entities.Where(e => e is IViewerEdge))
                gViewer.Invalidate(edge);
        }

        void RouteStraightLines() {
            var geomGraph = gViewer.Graph.GeometryGraph;
            foreach (var e in geomGraph.Edges)
                StraightLineEdges.CreateSimpleEdgeCurveWithUnderlyingPolyline(e);
        }

//        bool UseSparseVisibilityGraph() {
//            if (gViewer.Graph != null)
//                return gViewer.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.UseSparseVisibilityGraph;
//            return false;
//        }

        void RouteEdges() {
            if (gViewer.Graph == null) return;
            
            if(overrideGraphRoutingSettingsToolStripMenuItem.Checked ) {
                Debug.Assert(EdgeRoutingSettings!=null);
                gViewer.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings = EdgeRoutingSettings;
            }

            var edgeMode = gViewer.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode;
            if (gViewer.Graph != null)
                gViewer.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings = EdgeRoutingSettings;
            if (edgeMode == EdgeRoutingMode.Rectilinear ||
                edgeMode == EdgeRoutingMode.RectilinearToCenter)
                RouteEdgesRectilinearly();
            else
                RouteEdgesRegular();
        }

        void RouteEdgesRectilinearly() {
            if(gViewer.Graph==null || gViewer.Graph.GeometryGraph==null)
                return;

//            var geomGraph = gViewer.Graph.GeometryGraph;
//            var rie = new RectilinearInteractiveEditor();
//            rie.CreatePortsAndRouteEdges(geomGraph.NodeSeparation / 3, 1, geomGraph.Nodes, geomGraph.Edges, geomGraph.LayoutAlgorithmSettings.EdgeRoutingMode);
//            EdgeLabelPlacement.PlaceLabels(geomGraph);
//
//             foreach(var edge in gViewer.Entities.Where(edge => edge is IViewerEdge))
//                 gViewer.Invalidate(edge);
            LayoutEditor.RouteEdgesRectilinearly(gViewer);
        }


        EdgeRoutingMode edgeRoutingMode;
        internal EdgeRoutingMode EdgeRoutingMode {
            get { return edgeRoutingMode; }
            set { 
                edgeRoutingMode = value;
                OverrideRoutingSettings = true;
                if (EdgeRoutingSettings == null) {
                    EdgeRoutingSettings = new EdgeRoutingSettings { EdgeRoutingMode = edgeRoutingMode };
                } else {
                    EdgeRoutingSettings.EdgeRoutingMode = edgeRoutingMode;
                }
            }
        }

        
        internal bool UseObstacleRectangles {
            get { return null == EdgeRoutingSettings ? false : this.EdgeRoutingSettings.UseObstacleRectangles; }
            set {
                // Make sure the EdgeRoutingSettings are there, using the default routing mode.
                OverrideRoutingSettings = true;
                if (EdgeRoutingSettings == null) {
                    EdgeRoutingSettings = new EdgeRoutingSettings();
                }
                EdgeRoutingSettings.UseObstacleRectangles = value;
            }
        }

        internal double BendPenalty {
            get { return null == EdgeRoutingSettings ? SsstRectilinearPath.DefaultBendPenaltyAsAPercentageOfDistance : this.EdgeRoutingSettings.BendPenalty; }
            set {
                // Make sure the EdgeRoutingSettings are there, using the default routing mode.
                OverrideRoutingSettings = true;
                if (EdgeRoutingSettings == null) {
                    EdgeRoutingSettings = new EdgeRoutingSettings();
                }
                EdgeRoutingSettings.BendPenalty = value;
            }
        }

        #region Nested type: VoidFunctionWithOneParameterDelegate

        delegate void VoidFunctionWithOneParameterDelegate(object s);

        #endregion

        void RoutingSettingsToolStripMenuItemClick(object sender, EventArgs e) {
            if (EdgeRoutingSettings == null)
                EdgeRoutingSettings = new EdgeRoutingSettings();

            if(overrideGraphRoutingSettingsToolStripMenuItem.Checked)
                EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode;

            Form f = CreateFormForEdgeRoutingSettings();

            var dr = f.ShowDialog();
            if (dr == DialogResult.OK) {
                overrideGraphRoutingSettingsToolStripMenuItem.Checked = true;
                EdgeRoutingMode = EdgeRoutingSettings.EdgeRoutingMode;
                RouteEdges();
            }
        }

        Form CreateFormForEdgeRoutingSettings() {
            var f = new Form();
            var pg = new PropertyGrid {SelectedObject = EdgeRoutingSettings};
            f.SuspendLayout();
            f.Controls.Add(pg);
            pg.Dock = DockStyle.Fill;
            f.DialogResult = DialogResult.Cancel;
            var okButton = new Button {Text = "OK", Anchor = AnchorStyles.Right | AnchorStyles.Bottom};
            f.Controls.Add(okButton);
            var buttonsTop = f.ClientRectangle.Height - okButton.Height;
            
            okButton.BringToFront();
            f.AcceptButton = okButton;
            var cancelButton = new Button {Text = "Cancel", Anchor = AnchorStyles.Right | AnchorStyles.Bottom};
            f.Controls.Add(cancelButton);
            f.CancelButton = cancelButton;
            cancelButton.Location = new System.Drawing.Point(f.ClientRectangle.Width - cancelButton.Width, buttonsTop);
            cancelButton.BringToFront();
            f.ResumeLayout();
            f.StartPosition = FormStartPosition.Manual;
            f.Location = Cursor.Position;
            cancelButton.Click += (a,b)=>f.Close();
            okButton.Click += ((a, b) => {
                                   f.DialogResult = DialogResult.OK;
                                   f.Close();
                               });
            okButton.Location = new System.Drawing.Point(f.ClientSize.Width - cancelButton.Width-1-okButton.Width, buttonsTop);
            return f;
        }

        private void OverrideGaphRoutingSettingsToolStripMenuItemClick(object sender, EventArgs e) {
            if (overrideGraphRoutingSettingsToolStripMenuItem.Checked)
                if (EdgeRoutingSettings == null)
                    EdgeRoutingSettings = new EdgeRoutingSettings();
        }

        /*
        void ContextMenu_Popup(object sender, EventArgs e)
        {

          this.ContextMenu.MenuItems.Clear();
          MenuItem mi = new MenuItem("graph attr");

          mi.Click+=new EventHandler(mi_Click);

          this.ContextMenu.MenuItems.Add(mi);
        }

        void mi_Click(object sender, EventArgs e)
        {
      
          if (gViewer.Microsoft.Msagl.Drawing != null)
          {
            Form f1 = new Form();


            PropertyGrid pg = new PropertyGrid();

            pg.SelectedObject = this.gViewer.Microsoft.Msagl.Drawing.GraphAttr;


            f1.SuspendLayout();
            f1.Controls.Add(pg);

            pg.Dock = DockStyle.Fill;

            f1.ResumeLayout();
            f1.ShowDialog();
            gViewer.Invalidate();
          }
        }
        */
        /*
        static P2 P2(DotParser.P2 p)
        {
          return new P2(p.x, p.y);
        }

        static P2 P2(double x, double y)
        {
          return new P2(x, y);
        }

        public static Microsoft.Msagl.Drawing CreateFromDotGraph(DotParser.Microsoft.Msagl.Drawing graph)
        {

          Microsoft.Msagl.Drawing ret = new Microsoft.Msagl.Drawing(graph.id);
          System.Drawing.SizeF size = graph.graphAttr.Size;

          ret.GraphAttr.BoundingBox = new Rectangle(P2(0, 0), P2(size.Width, size.Height));

          foreach (DotParser.Edge e in graph.edges)
          {
            Edge e1 = ret.AddEdge(e.head, e.id, e.tail);
            e1.Attr.Id = e.attr.id;
            e1.Attr.Label = e.id;
            DotParser.PosData pd = e.attr.posData;
            e1.Attr.PosData = new PosData();

            foreach (DotParser.P2 p in pd.controlPoints)
              e1.Attr.PosData.controlPoints.Add(P2(p));

            e1.Attr.PosData.ArrowAtSourcePosition = P2(pd.arrowAtTheBeginnigPosition);
            e1.Attr.PosData.ArrowAtSource = pd.ArrowAtSource;
            e1.Attr.PosData.ArrowAtTarget = pd.ArrowAtTarget;
            e1.Attr.PosData.ArrowAtTargetPosition = P2(pd.ArrowAtTargetPosition);
          }
          foreach (DotParser.Node n in graph.Nodes)
          {
            Node n0 = ret.AddNode(n.id);
            n0.Attr.LabelCenter = P2(n.attr.LabelCenter);
            n0.Attr.Width = n.attr.width;
            n0.Attr.Height = n.attr.height;
            SetNodeShape(n, n0);
          }
          foreach (DotParser.Microsoft.Msagl.Drawing sg in graph.Subgraphs)
            ret.AddSubgraph(CreateFromDotGraph(sg));

          return ret;
        }
        */
        /*
        private static void SetNodeShape(DotParser.Node n, Node n0)
        {
          switch (n.attr.Shape)
          {

            case DotParser.Shape.None: break;
            case DotParser.Shape.Diamond: n0.Attr.Shape = Shape.Diamond; break;

            case DotParser.Shape.Ellipse: n0.Attr.Shape = Shape.Ellipse; break;
            case DotParser.Shape.Box: n0.Attr.Shape = Shape.Box; break;
            case DotParser.Shape.Circle: n0.Attr.Shape = Shape.Circle; break;
            case DotParser.Shape.Record: n0.Attr.Shape = Shape.Record; break;
            case DotParser.Shape.Plaintext: n0.Attr.Shape = Shape.Plaintext; break;
            case DotParser.Shape.Point: n0.Attr.Shape = Shape.Point; break;
            case DotParser.Shape.Mdiamond: n0.Attr.Shape = Shape.Mdiamond; break;
            case DotParser.Shape.Msquare: n0.Attr.Shape = Shape.Msquare; break;
            case DotParser.Shape.polygon: n0.Attr.Shape = Shape.Polygon; break;
            case DotParser.Shape.DoubleCircle: n0.Attr.Shape = Shape.DoubleCircle; break;
            case DotParser.Shape.House: n0.Attr.Shape = Shape.House; break;
            case DotParser.Shape.InvHouse: n0.Attr.Shape = Shape.InvHouse; break;
            case DotParser.Shape.Parallelogram: n0.Attr.Shape = Shape.Parallelogram; break;
            case DotParser.Shape.Octagon: n0.Attr.Shape = Shape.Octagon; break;
            case DotParser.Shape.TripleOctagon: n0.Attr.Shape = Shape.TripleOctagon; break;
            case DotParser.Shape.Triangle: n0.Attr.Shape = Shape.Triangle; break;
            case DotParser.Shape.Trapezium: n0.Attr.Shape = Shape.Trapezium; break;


          }
        }


        public static Microsoft.Msagl.Drawing CreateTopologyFromDotGraph(DotParser.Microsoft.Msagl.Drawing graph)
        {

          Microsoft.Msagl.Drawing ret = new Microsoft.Msagl.Drawing(graph.id);


          foreach (DictionaryEntry de in graph.NodeMap)
          {
            Node n0 = ret.AddNode(de.Key as string);
            n0.Id = de.Key as String;
            DotParser.Node gvn = de.Value as DotParser.Node;
            if (gvn.attr.Label != null)
              n0.Attr.Label = gvn.attr.Label;
            n0.Attr.Font = new System.Drawing.Font(gvn.attr.FontName, gvn.attr.Fontsize * (float)LayoutAlgorithmSettings.PointSize);
            n0.Attr.Color = gvn.attr.Color;
            n0.Attr.Fontcolor = gvn.attr.Fontcolor;
            SetNodeShape(gvn, n0);
          }
          foreach (DotParser.Edge e in graph.edges)
          {
            Edge e1 = ret.AddEdge(e.head, e.id, e.tail);
            e1.Attr.Id = e.attr.id;
            e1.Attr.Label = e.id;
            e1.Attr.Font = new System.Drawing.Font(e.attr.FontName, e.attr.Fontsize * (float)(LayoutAlgorithmSettings.PointSize));
            e1.Attr.Color = e.attr.Color;
            e1.Attr.Fontcolor = e.attr.Fontcolor;

          }
          foreach (DotParser.Microsoft.Msagl.Drawing sg in graph.Subgraphs)
            ret.AddSubgraph(CreateTopologyFromDotGraph(sg));

          return ret;
        }
    */
    }
}
