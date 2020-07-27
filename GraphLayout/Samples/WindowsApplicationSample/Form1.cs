using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Color = Microsoft.Msagl.Drawing.Color;
using Label = Microsoft.Msagl.Drawing.Label;
using MouseButtons = System.Windows.Forms.MouseButtons;

namespace WindowsApplicationSample {
    public partial class Form1 : Form {
        readonly ToolTip toolTip1 = new ToolTip();
        object selectedObject;
        AttributeBase selectedObjectAttr;

        public Form1() {
            Load += Form1Load;
            InitializeComponent();
            gViewer.MouseWheel += GViewerMouseWheel;
            gViewer.AsyncLayout = true;
            toolTip1.Active = true;
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 1000;
            toolTip1.ReshowDelay = 500;
            gViewer.LayoutEditor.DecorateObjectForDragging = SetDragDecorator;
            gViewer.LayoutEditor.RemoveObjDraggingDecorations = RemoveDragDecorator;
            gViewer.MouseDown += WaMouseDown;
            gViewer.MouseUp += WaMouseUp;
            gViewer.MouseMove += GViewerOnMouseMove;
        }

        void GViewerOnMouseMove(object sender, MouseEventArgs mouseEventArgs) {
            if (labelToChange == null) return;
            labelToChange.Text = MousePosition.ToString();
            if (viewerEntityCorrespondingToLabelToChange == null) {
                foreach (var e in gViewer.Entities) {
                    if (e.DrawingObject == labelToChange) {
                        viewerEntityCorrespondingToLabelToChange = e;
                        break;
                    }
                }
            }
            if (viewerEntityCorrespondingToLabelToChange == null) return;
            var rect = labelToChange.BoundingBox;
            var font = new Font(labelToChange.FontName, (int)labelToChange.FontSize);
            double width;
            double height;
            StringMeasure.MeasureWithFont(labelToChange.Text, font, out width, out height);

            if (width <= 0)
                //this is a temporary fix for win7 where Measure fonts return negative lenght for the string " "
                StringMeasure.MeasureWithFont("a", font, out width, out height);

            labelToChange.Width = width;
            labelToChange.Height = height;
            rect.Add(labelToChange.BoundingBox);
            gViewer.Invalidate(gViewer.MapSourceRectangleToScreenRectangle(rect));
        }

        void WaMouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left)
                myMouseUpPoint = e.Location;
        }

        void WaMouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left)
                myMouseDownPoint = e.Location;
        }

        readonly Dictionary<object, Color> draggedObjectOriginalColors = new Dictionary<object, Color>();
        System.Drawing.Point myMouseDownPoint;
        System.Drawing.Point myMouseUpPoint;

        void SetDragDecorator(IViewerObject obj) {
            var dNode = obj as DNode;
            if (dNode != null) {
                draggedObjectOriginalColors[dNode] = dNode.DrawingNode.Attr.Color;
                dNode.DrawingNode.Attr.Color = Color.Magenta;
                gViewer.Invalidate(obj);
            }
        }

        void RemoveDragDecorator(IViewerObject obj) {
            var dNode = obj as DNode;
            if (dNode != null) {
                dNode.DrawingNode.Attr.Color = draggedObjectOriginalColors[dNode];
                draggedObjectOriginalColors.Remove(obj);
                gViewer.Invalidate(obj);
            }
        }

        void GViewerMouseWheel(object sender, MouseEventArgs e) {
            int delta = e.Delta;
            if (delta != 0)
                gViewer.ZoomF *= delta < 0 ? 0.9 : 1.1;
        }

        void Form1Load(object sender, EventArgs e) {
            gViewer.ObjectUnderMouseCursorChanged += GViewerObjectUnderMouseCursorChanged;

#if TEST_MSAGL
            //DisplayGeometryGraph.SetShowFunctions();
#endif

            /////  CreateGraph();
        }

        void GViewerObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e) {
            selectedObject = e.OldObject != null ? e.OldObject.DrawingObject : null;

            if (selectedObject != null) {
                RestoreSelectedObjAttr();
                gViewer.Invalidate(e.OldObject);
                selectedObject = null;
            }

            if (gViewer.ObjectUnderMouseCursor == null) {
                label1.Text = "No object under the mouse";
                gViewer.SetToolTip(toolTip1, "");
            }
            else {
                selectedObject = gViewer.ObjectUnderMouseCursor.DrawingObject;
                var edge = selectedObject as Edge;
                if (edge != null) {
                    selectedObjectAttr = edge.Attr.Clone();
                    edge.Attr.Color = Color.Blue;
                    gViewer.Invalidate(e.NewObject);

                    //         here we can use e.Attr.Id or e.UserData to get back to the user data
                    gViewer.SetToolTip(toolTip1, String.Format("edge from {0} to {1}", edge.Source, edge.Target));
                }
                else if (selectedObject is Microsoft.Msagl.Drawing.Node) {
                    selectedObjectAttr = (gViewer.SelectedObject as Microsoft.Msagl.Drawing.Node).Attr.Clone();
                    (selectedObject as Microsoft.Msagl.Drawing.Node).Attr.Color = Color.Green;
                    // //   here you can use e.Attr.Id to get back to your data
                    gViewer.SetToolTip(toolTip1,
                                       String.Format("node {0}",
                                                     (selectedObject as Microsoft.Msagl.Drawing.Node).Attr.Id));
                    gViewer.Invalidate(e.NewObject);
                }
                label1.Text = selectedObject.ToString();                
            }

            label1.Refresh();
        }

        void RestoreSelectedObjAttr() {
            var edge = selectedObject as Edge;
            if (edge != null) {
                edge.Attr = (EdgeAttr)selectedObjectAttr;
            }
            else {
                var node = selectedObject as Microsoft.Msagl.Drawing.Node;
                if (node != null)
                    node.Attr = (NodeAttr)selectedObjectAttr;

            }

        }


        void Button1Click(object sender, EventArgs e) {
            switch (comboBox1.SelectedIndex) {
                case 1:
                    CreateGraphClustersSmall();
                    break;
                case 2:
                    CreateGraphClustersBig(false);
                    break;
                case 3:
                    CreateGraphClustersBig(true);
                    break;
                default:
                    CreateGraph();
                    break;
            }
        }

        Label labelToChange;
        IViewerObject viewerEntityCorrespondingToLabelToChange;

        void CreateGraphClustersSmall() {
            Graph graph = new Graph("clusters");
            var receivers_growth = new Subgraph("Receivers_growth");
            var receiver0_growth = new Node("Receiver0_growth");
            receivers_growth.AddNode(receiver0_growth);
            var receivers_control = new Subgraph("Receivers_control");
            var receiver0_control = new Node("Receiver0_control");
            receivers_control.AddNode(receiver0_control);
            var receivers = new Subgraph("Receivers");
            var receiver0 = new Node("Receiver0");
            receivers.AddNode(receiver0);
            graph.RootSubgraph.AddSubgraph(receivers_growth);
            graph.AddNode(receiver0_growth);
            graph.RootSubgraph.AddSubgraph(receivers_control);
            graph.AddNode(receiver0_control);
            graph.RootSubgraph.AddSubgraph(receivers);
            graph.AddNode(receiver0);
            graph.AddEdge(receivers_growth.Id, receivers_control.Id);
            graph.AddEdge(receiver0_control.Id, receiver0.Id);
            graph.AddEdge(receiver0_growth.Id, receiver0.Id);
            gViewer.Graph = graph;
            this.propertyGrid1.SelectedObject = graph;
        }

        void CreateGraphClustersBig(bool horizontal) {
            Graph graph = new Graph("clusters big");

            void addCluster(string clusterId, params string[] nodeIds) {
                if (nodeIds.Length == 0)
                    nodeIds = new string[] { clusterId };
                var cluster = new Subgraph(clusterId);
                foreach (var nodeId in nodeIds) {
                    var node = new Node(clusterId + "." + nodeId);
                    node.LabelText = nodeId;
                    graph.AddNode(node);
                    cluster.AddNode(node);
                }
                if (horizontal)
                    cluster.Attr.ClusterLabelMargin = Microsoft.Msagl.Core.Layout.LgNodeInfo.LabelPlacement.Right;
                graph.RootSubgraph.AddSubgraph(cluster);
            }

            addCluster("Arabinose");
            addCluster("Arabinose_control");
            addCluster("Arabinose_growth");
            addCluster("Auto");
            addCluster("Auto_control");
            addCluster("Auto_growth");
            addCluster("Degrader");
            addCluster("Degrader_control");
            addCluster("Degrader_growth");
            addCluster("Receivers", "Receiver0", "Receiver1", "Receiver2", "Receiver3");
            addCluster("Receivers_control", "Receiver0_control", "Receiver1_control", "Receiver2_control", "Receiver3_control");
            addCluster("Receivers_growth", "Receiver0_growth", "Receiver1_growth", "Receiver2_growth", "Receiver3_growth");
            addCluster("Relays", "Relay1", "Relay2");
            addCluster("Relays_control", "Relay1_control", "Relay2_control");
            addCluster("Relays_growth", "Relay1_growth", "Relay2_growth");
            addCluster("Standard");
            addCluster("Standard_control");
            addCluster("Standard_growth");

            void addEdge(string sourceCluster, string sourceNode, string targetCluster, string targetNode) {
                graph.AddEdge(sourceNode == null ? sourceCluster : (sourceCluster + "." + sourceNode), targetNode == null ? targetCluster : (targetCluster + "." + targetNode));
            }

            addEdge("Arabinose", "Arabinose", "Degrader", "Degrader");
            addEdge("Arabinose_control", "Arabinose_control", "Arabinose", "Arabinose");
            addEdge("Arabinose_control", "Arabinose_control", "Degrader_control", "Degrader_control");
            addEdge("Arabinose_growth", "Arabinose_growth", "Arabinose_control", "Arabinose_control");
            addEdge("Auto", null, "Standard", null);
            addEdge("Auto_control", "Auto_control", "Auto", "Auto");
            addEdge("Auto_control", null, "Standard_control", null);
            addEdge("Auto_growth", "Auto_growth", "Auto_control", "Auto_control");
            addEdge("Degrader_control", "Degrader_control", "Degrader", "Degrader");
            addEdge("Degrader_growth", "Degrader_growth", "Degrader_control", "Degrader_control");
            addEdge("Receivers", null, "Relays", null);
            addEdge("Receivers_control", null, "Relays_control", null);
            addEdge("Receivers_control", "Receiver0_control", "Receivers", "Receiver0");
            addEdge("Receivers_control", "Receiver1_control", "Receivers", "Receiver1");
            addEdge("Receivers_control", "Receiver2_control", "Receivers", "Receiver2");
            addEdge("Receivers_control", "Receiver3_control", "Receivers", "Receiver3");
            addEdge("Receivers_growth", "Receiver0_growth", "Receivers_control", "Receiver0_control");
            addEdge("Receivers_growth", "Receiver1_growth", "Receivers_control", "Receiver1_control");
            addEdge("Receivers_growth", "Receiver2_growth", "Receivers_control", "Receiver2_control");
            addEdge("Receivers_growth", "Receiver3_growth", "Receivers_control", "Receiver3_control");
            addEdge("Relays", null, "Degrader", null);
            addEdge("Relays_control", "Relay1_control", "Relays", "Relay1");
            addEdge("Relays_control", "Relay2_control", "Relays", "Relay2");
            addEdge("Relays_growth", "Relay1_growth", "Relays_control", "Relay1_control");
            addEdge("Relays_growth", "Relay2_growth", "Relays_control", "Relay2_control");
            addEdge("Standard", null, "Receivers", null);
            addEdge("Standard_control", null, "Receivers_control", null);
            addEdge("Standard_control", "Standard_control", "Standard", "Standard");
            addEdge("Standard_growth", "Standard_growth", "Standard_control", "Standard_control");

            if (horizontal)
                (graph.LayoutAlgorithmSettings as SugiyamaLayoutSettings).Transformation = PlaneTransformation.Rotation(Math.PI / 2.0);
            gViewer.Graph = graph;
            this.propertyGrid1.SelectedObject = graph;
        }

        void CreateGraph() {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            //Graph graph = new Graph();
            //graph.AddEdge("47", "58");
            //graph.AddEdge("70", "71");



            //var subgraph = new Subgraph("subgraph1");
            //graph.RootSubgraph.AddSubgraph(subgraph);
            //subgraph.AddNode(graph.FindNode("47"));
            //subgraph.AddNode(graph.FindNode("58"));

            //var subgraph2 = new Subgraph("subgraph2");
            //subgraph2.Attr.Color = Color.Black;
            //subgraph2.Attr.FillColor = Color.Yellow;
            //subgraph2.AddNode(graph.FindNode("70"));
            //subgraph2.AddNode(graph.FindNode("71"));
            //subgraph.AddSubgraph(subgraph2);
            //graph.AddEdge("58", subgraph2.Id);
            //graph.Attr.LayerDirection = LayerDirection.LR;
            //gViewer.Graph = graph;
            Graph graph = new Graph("graph");
            //graph.LayoutAlgorithmSettings=new MdsLayoutSettings();
            gViewer.BackColor = System.Drawing.Color.FromArgb(10, System.Drawing.Color.Red);

            /*
              4->5
    5->7
    7->8
    8->22
    22->24
    */

            graph.AddEdge("1", "2");
            graph.AddEdge("1", "3");
            var e = graph.AddEdge("4", "5");
            e.Attr.Color = Color.Red;
            e.Attr.LineWidth *= 2;
            graph.LayerConstraints.AddUpDownVerticalConstraint(graph.FindNode("4"), graph.FindNode("5"));
            StraightenEdge(e, graph);
            e = graph.AddEdge("4", "6");
            e.LabelText = "Changing label";
            this.labelToChange = e.Label;
            e = graph.AddEdge("7", "8");
            e.Attr.LineWidth *= 2;
            e.Attr.Color = Color.Red;
            StraightenEdge(e, graph);
            graph.AddEdge("7", "9");
            e = graph.AddEdge("5", "7");
            e.Attr.Color = Color.Red;
            e.Attr.LineWidth *= 2;
            StraightenEdge(e, graph);

            graph.AddEdge("2", "7");
            graph.AddEdge("10", "11");
            graph.AddEdge("10", "12");
            graph.AddEdge("2", "10");
            graph.AddEdge("8", "10");
            graph.AddEdge("5", "10");
            graph.AddEdge("13", "14");
            graph.AddEdge("13", "15");
            graph.AddEdge("8", "13");
            graph.AddEdge("2", "13");
            graph.AddEdge("5", "13");
            graph.AddEdge("16", "17");
            graph.AddEdge("16", "18");
            graph.AddEdge("19", "20");
            graph.AddEdge("19", "21");
            graph.AddEdge("17", "19");
            graph.AddEdge("2", "19");
            graph.AddEdge("22", "23");

            e = graph.AddEdge("22", "24");
            e.Attr.Color = Color.Red;
            e.Attr.LineWidth *= 2;
            StraightenEdge(e, graph);

            e = graph.AddEdge("8", "22");
            e.Attr.Color = Color.Red;
            e.Attr.LineWidth *= 2;
            StraightenEdge(e, graph);
            graph.AddEdge("20", "22");
            graph.AddEdge("25", "26");
            graph.AddEdge("25", "27");
            graph.AddEdge("20", "25");
            graph.AddEdge("28", "29");
            graph.AddEdge("28", "30");
            graph.AddEdge("31", "32");
            graph.AddEdge("31", "33");
            graph.AddEdge("5", "31");
            graph.AddEdge("8", "31");
            graph.AddEdge("2", "31");
            graph.AddEdge("20", "31");
            graph.AddEdge("17", "31");
            graph.AddEdge("29", "31");
            graph.AddEdge("34", "35");
            graph.AddEdge("34", "36");
            graph.AddEdge("20", "34");
            graph.AddEdge("29", "34");
            graph.AddEdge("5", "34");
            graph.AddEdge("2", "34");
            graph.AddEdge("8", "34");
            graph.AddEdge("17", "34");
            graph.AddEdge("37", "38");
            graph.AddEdge("37", "39");
            graph.AddEdge("29", "37");
            graph.AddEdge("5", "37");
            graph.AddEdge("20", "37");
            graph.AddEdge("8", "37");
            graph.AddEdge("2", "37");
            graph.AddEdge("40", "41");
            graph.AddEdge("40", "42");
            graph.AddEdge("17", "40");
            graph.AddEdge("2", "40");
            graph.AddEdge("8", "40");
            graph.AddEdge("5", "40");
            graph.AddEdge("20", "40");
            graph.AddEdge("29", "40");
            graph.AddEdge("43", "44");
            graph.AddEdge("43", "45");
            graph.AddEdge("8", "43");
            graph.AddEdge("2", "43");
            graph.AddEdge("20", "43");
            graph.AddEdge("17", "43");
            graph.AddEdge("5", "43");
            graph.AddEdge("29", "43");
            graph.AddEdge("46", "47");
            graph.AddEdge("46", "48");
            graph.AddEdge("29", "46");
            graph.AddEdge("5", "46");
            graph.AddEdge("17", "46");
            graph.AddEdge("49", "50");
            graph.AddEdge("49", "51");
            graph.AddEdge("5", "49");
            graph.AddEdge("2", "49");
            graph.AddEdge("52", "53");
            graph.AddEdge("52", "54");
            graph.AddEdge("17", "52");
            graph.AddEdge("20", "52");
            graph.AddEdge("2", "52");
            graph.AddEdge("50", "52");
            graph.AddEdge("55", "56");
            graph.AddEdge("55", "57");
            graph.AddEdge("58", "59");
            graph.AddEdge("58", "60");
            graph.AddEdge("20", "58");
            graph.AddEdge("29", "58");
            graph.AddEdge("5", "58");
            graph.AddEdge("47", "58");

            //ChangeNodeSizes(graph);

            //var sls = graph.LayoutAlgorithmSettings as SugiyamaLayoutSettings;
            //if (sls != null)
            //{
            //    sls.GridSizeByX = 30;
            //    // sls.GridSizeByY = 0;
            //}
            //layout the graph and draw it
            gViewer.Graph = graph;
            this.propertyGrid1.SelectedObject = graph;
        }

        private void StraightenEdge(Edge e, Graph graph) {
            graph.LayerConstraints.AddUpDownVerticalConstraint(e.SourceNode, e.TargetNode);
        }

        void RecalculateLayoutButtonClick(object sender, EventArgs e) {
            gViewer.Graph = propertyGrid1.SelectedObject as Graph;
        }


        bool MouseDownPointAndMouseUpPointsAreFarEnough() {
            double dx = myMouseDownPoint.X - myMouseUpPoint.X;
            double dy = myMouseDownPoint.Y - myMouseUpPoint.Y;

            return dx * dx + dy * dy >= 25; //so 5X5 pixels already give something
        }

        void ShowObjectsInTheLastRectClick(object sender, EventArgs e) {
            string message;
            if (gViewer.Graph == null) {
                message = "there is no graph";
            }
            else {
                if (MouseDownPointAndMouseUpPointsAreFarEnough()) {
                    var p0 = gViewer.ScreenToSource(myMouseDownPoint);
                    var p1 = gViewer.ScreenToSource(myMouseUpPoint);
                    var rubberRect = new Microsoft.Msagl.Core.Geometry.Rectangle(p0, p1);
                    var stringB = new StringBuilder();
                    foreach (var node in gViewer.Graph.Nodes)
                        if (rubberRect.Contains(node.BoundingBox))
                            stringB.Append(node.LabelText + "\n");

                    foreach (var edge in gViewer.Graph.Edges)
                        if (rubberRect.Contains(edge.BoundingBox))
                            stringB.Append(String.Format("edge from {0} to {1}\n", edge.SourceNode.LabelText,
                                                         edge.TargetNode.LabelText));

                    message = stringB.ToString();
                }
                else
                    message = "the window is not defined";
            }

            MessageBox.Show(message);

        }
    }
}