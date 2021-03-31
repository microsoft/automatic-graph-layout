using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using Color=Microsoft.Msagl.Drawing.Color;
using Edge=Microsoft.Msagl.Drawing.Edge;
using Node=Microsoft.Msagl.Drawing.Node;
using Point=Microsoft.Msagl.Core.Geometry.Point;
using Rectangle=Microsoft.Msagl.Core.Geometry.Rectangle;
using GeomEdge=Microsoft.Msagl.Core.Layout.Edge;
namespace FindEmptySpotSample {
    public partial class Form1 : Form {
        readonly ToolTip toolTip1 = new ToolTip();
        public Form1() {
            this.Load += new EventHandler(Form1_Load);
            InitializeComponent();

            this.toolTip1.Active = true;
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 1000;
            toolTip1.ReshowDelay = 500;
        }

        void Form1_Load(object sender, EventArgs e) {
            gViewer.ObjectUnderMouseCursorChanged += new EventHandler<ObjectUnderMouseCursorChangedEventArgs>(gViewer_ObjectUnderMouseCursorChanged);

#if TEST_MSAGL
           Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif

           CreateGraph();
        }

        object selectedObjectAttr;
        object selectedObject;

        void gViewer_ObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e) {
            selectedObject = e.OldObject != null ? e.OldObject.DrawingObject : null;

            if (selectedObject != null) {
                if (selectedObject is Edge)
                    (selectedObject as Edge).Attr = selectedObjectAttr as EdgeAttr;
                else if (selectedObject is Node)
                    (selectedObject as Node).Attr = selectedObjectAttr as NodeAttr;

                selectedObject = null;
            }

            if (gViewer.SelectedObject == null) {
                label1.Text = "No object under the mouse";
                this.gViewer.SetToolTip(toolTip1, "");

            } else {
                selectedObject = gViewer.SelectedObject;
                Edge edge = selectedObject as Edge;
                if (edge != null) {
                    selectedObjectAttr = edge.Attr.Clone();
                    edge.Attr.Color = Microsoft.Msagl.Drawing.Color.Magenta;

                    //here we can use e.Attr.Id or e.UserData to get back to the user data
                    this.gViewer.SetToolTip(this.toolTip1, String.Format("edge from {0} {1}", edge.Source, edge.Target));

                } else if (selectedObject is Node) {

                    selectedObjectAttr = (gViewer.SelectedObject as Node).Attr.Clone();
                    (selectedObject as Node).Attr.Color = Microsoft.Msagl.Drawing.Color.Magenta;
                    //here you can use e.Attr.Id to get back to your data
                    this.gViewer.SetToolTip(toolTip1, String.Format("node {0}", (selectedObject as Node).Attr.Id));
                }
                label1.Text = selectedObject.ToString();
            }

            label1.Refresh();
            gViewer.Invalidate();
        }




        private void button1_Click(object sender, EventArgs e) {//this is abstract.dot of GraphViz
            var tree = RectangleNode<object,Point>.CreateRectangleNodeOnEnumeration(GetRectangleNodesFromGraph());

            var numberOfTries=10000;
            Random random=new Random(1);
            double rectWidth=50;
            double rectHeight=200;
            var delta=new Point(rectWidth/2, rectHeight/2);
            Rectangle bestRectangle = Rectangle.CreateAnEmptyBox();

            Point hint = (gViewer.Graph.BoundingBox.LeftBottom + gViewer.Graph.BoundingBox.RightTop) / 2;
            double minDistance=double.PositiveInfinity;
            for(int i=0;i<numberOfTries;i++){
                Point randomCenter=GetRandomCenter(rectHeight,rectWidth,random);
                Rectangle r=new Rectangle(randomCenter);
                r.Add(randomCenter+delta);
                r.Add(randomCenter-delta);
                if(tree.GetNodeItemsIntersectingRectangle(r).Any()){}
                else {
                    var len=(randomCenter-hint).LengthSquared;
                    if(len<minDistance){
                        minDistance = len;
                        bestRectangle=r;
                    }
                }
            }
            if (bestRectangle.IsEmpty == false)
                InsertNodeIntoGraph(bestRectangle);
            else 
                MessageBox.Show("cannot insert");
      
        }

        void InsertNodeIntoGraph(Rectangle rectangle) {
            Node node = new Node("testNode");
            node.Attr.FillColor=Color.Red;
            node.Attr.Shape = Shape.DrawFromGeometry;
             node.Label=null;
            var geomNode =
                node.GeometryNode = GeometryGraphCreator.CreateGeometryNode(gViewer.Graph, gViewer.Graph.GeometryGraph, node, ConnectionToGraph.Disconnected);
            var center = (rectangle.LeftBottom + rectangle.RightTop) / 2;
            geomNode.BoundaryCurve = CurveFactory.CreateRectangle(rectangle.Width, rectangle.Height, center);
            node.GeometryNode=geomNode;
            var dNode = gViewer.CreateIViewerNode(node);
            gViewer.AddNode(dNode, true);
        }

        Point GetRandomCenter(double nodeHeight, double nodWidth, Random random) {
            double x = random.NextDouble();
            double y = random.NextDouble();
            x=gViewer.Graph.Left+nodWidth/2+(gViewer.Graph.Width-nodWidth)*x;
            y = gViewer.Graph.Bottom + nodeHeight / 2 + (gViewer.Graph.Height - nodeHeight) * y;
            return new Point(x, y);
        }

        IEnumerable<RectangleNode<object,Point>> GetRectangleNodesFromGraph() {
            var graph = gViewer.Graph.GeometryGraph;
            foreach (var node in graph.Nodes) 
                yield return new RectangleNode<object,Point>(node, node.BoundingBox);
            foreach (var edge in graph.Edges) {
                foreach (var edgeRectNode in EdgeRectNodes(edge))
                    yield return edgeRectNode;

            }
        }

        IEnumerable<RectangleNode<object, Point >> EdgeRectNodes(GeomEdge edge) {
            const int parts = 64; //divide each edge into 64 segments
            var curve=edge.Curve;
            double delta=(curve.ParEnd-curve.ParStart)/parts;
             Point p0=curve.Start;
             for (int i = 1; i <= parts; i++) 
                 yield return new RectangleNode<object,Point>(edge, new Rectangle(p0, p0=curve[curve.ParStart+i*delta]));

             if (edge.ArrowheadAtSource)
                 yield return new RectangleNode<object,Point>(edge, new Rectangle(edge.EdgeGeometry.SourceArrowhead.TipPosition, curve.Start));

             if (edge.ArrowheadAtTarget)
                 yield return new RectangleNode<object,Point>(edge, new Rectangle(edge.EdgeGeometry.TargetArrowhead.TipPosition, curve.End));

        }

        private void CreateGraph() {
            Graph graph = new Graph("graph");

            Edge edge = (Edge)graph.AddEdge("S24", "27");
            edge.LabelText = "Edge Label Test";

            graph.AddEdge("S24", "25");
            edge = graph.AddEdge("S1", "10") as Edge;

            edge.LabelText = "Init";
            edge.Attr.ArrowheadAtTarget = ArrowStyle.Tee;
            //  edge.Attr.Weight = 10;
            edge = graph.AddEdge("S1", "2") as Edge;
            // edge.Attr.Weight = 10;
            graph.AddEdge("S35", "36");
            graph.AddEdge("S35", "43");
            graph.AddEdge("S30", "31");
            graph.AddEdge("S30", "33");
            graph.AddEdge("9", "42");
            graph.AddEdge("9", "T1");
            graph.AddEdge("25", "T1");
            graph.AddEdge("25", "26");
            graph.AddEdge("27", "T24");
            graph.AddEdge("2", "3");
            graph.AddEdge("2", "16");
            graph.AddEdge("2", "17");
            graph.AddEdge("2", "T1");
            graph.AddEdge("2", "18");
            graph.AddEdge("10", "11");
            graph.AddEdge("10", "14");
            graph.AddEdge("10", "T1");
            graph.AddEdge("10", "13");
            graph.AddEdge("10", "12");
            graph.AddEdge("31", "T1");
            edge = (Edge)graph.AddEdge("31", "32");
            edge.Attr.ArrowheadAtTarget = ArrowStyle.Tee;
            edge.Attr.LineWidth = 10;
            edge.Attr.Weight = 10;
            edge.Attr.ArrowheadLength *= 2;
            edge = (Edge)graph.AddEdge("33", "T30");
            edge.Attr.LineWidth = 15;
            edge.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dashed);
            graph.AddEdge("33", "34");
            graph.AddEdge("42", "4");
            graph.AddEdge("26", "4");
            graph.AddEdge("3", "4");
            graph.AddEdge("16", "15");
            graph.AddEdge("17", "19");
            graph.AddEdge("18", "29");
            graph.AddEdge("11", "4");
            graph.AddEdge("14", "15");
            graph.AddEdge("37", "39");
            graph.AddEdge("37", "41");
            graph.AddEdge("37", "38");
            graph.AddEdge("37", "40");
            graph.AddEdge("13", "19");
            graph.AddEdge("12", "29");
            graph.AddEdge("43", "38");
            graph.AddEdge("43", "40");
            graph.AddEdge("36", "19");
            graph.AddEdge("32", "23");
            graph.AddEdge("34", "29");
            graph.AddEdge("39", "15");
            graph.AddEdge("41", "29");
            graph.AddEdge("38", "4");
            graph.AddEdge("40", "19");
            graph.AddEdge("4", "5");
            graph.AddEdge("19", "21");
            graph.AddEdge("19", "20");
            graph.AddEdge("19", "28");
            graph.AddEdge("5", "6");
            graph.AddEdge("5", "T35");
            graph.AddEdge("5", "23");
            edge = graph.AddEdge("21", "22");
            edge.Attr.ArrowheadLength *= 3;
            graph.AddEdge("20", "15");
            graph.AddEdge("28", "29");
            graph.AddEdge("6", "7");
            graph.AddEdge("15", "T1");
            graph.AddEdge("22", "23");
            graph.AddEdge("22", "T35");
            graph.AddEdge("29", "T30");
            graph.AddEdge("7", "T8");
            graph.AddEdge("23", "T24");
            graph.AddEdge("23", "T1");


            Node node = graph.FindNode("S1") as Node;
            node.LabelText = "Label Test";
            CreateSourceNode(graph.FindNode("S1") as Node);
            CreateSourceNode(graph.FindNode("S24") as Node);
            CreateSourceNode(graph.FindNode("S35") as Node);


            CreateTargetNode(graph.FindNode("T24") as Node);
            CreateTargetNode(graph.FindNode("T1") as Node);
            CreateTargetNode(graph.FindNode("T30") as Node);
            CreateTargetNode(graph.FindNode("T8") as Node);


            //layout the graph and draw it
            //graph.AddEdge("f", "a");
            //graph.AddEdge("a", "l").LabelText = "from node a\n to node l\n with sincerity";
            //graph.AddEdge("a", "b").LabelText="a=>b";
            //graph.AddEdge("b", "a"); //changing the order of a and b causes a crash
            //graph.AddEdge("a", "l");
            //graph.AddEdge("a", "b").LabelText = "a=>b label\n number 2";
            //graph.AddEdge("a", "c");
            //graph.AddEdge("b", "d");
            //graph.AddEdge("d", "c");
            //graph.AddEdge("c", "b");
            //graph.AddEdge("b", "c");
            //graph.AddEdge("a", "e");
            //graph.AddEdge("d", "k").LabelText="dk label";

            //Microsoft.Msagl.SugiyamaLayoutSettings settings = graph.LayoutAlgorithmSettings as Microsoft.Msagl.SugiyamaLayoutSettings;
            //settings.PinNodesToMaxLayer("2","S35");
            //settings.AddUpDownVerticalConstraint("39", "15");
            //settings.AddSameLayerNeighbors("S24", "2");
            //settings.AddUpDownVerticalConstraint("9", "3");
            //settings.AddUpDownVerticalConstraint("S1", "T8");



#if TEST_MSAGL
            Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif


            //settings.AddUpDownVerticalConstraints("3", "28");

            //settings.PinNodesToMinLayer("T1", "T8", "T24");   
            //settings.PinNodesToSameLayer("12", "S30", "19");
            //settings.PinNodesToSameLayer("29", "31", "4");
            //settings.AddUpDownConstraint("29", "12");

            // Add vertical and horizontal constraints
            //settings.AddUpDownVerticalConstraint("37", "38");
            //settings.AddUpDownVerticalConstraint("38", "4");
            //settings.AddUpDownVerticalConstraint("4", "5");
            //settings.AddSameLayerNeighbors("2", "10");

            //(graph.LayoutAlgorithmSettings as Microsoft.Msagl.SugiyamaLayoutSettings).AddUpDownConstraint("19", "40");
            //(graph.LayoutAlgorithmSettings as Microsoft.Msagl.SugiyamaLayoutSettings).AddUpDownConstraint("17", "40");
            //(graph.LayoutAlgorithmSettings as Microsoft.Msagl.SugiyamaLayoutSettings).AddSameLayerNeighbors("43", "S24");

            //(graph.LayoutAlgorithmSettings as Microsoft.Msagl.SugiyamaLayoutSettings).AddUpDownConstraint("7", "T1");
            //(graph.LayoutAlgorithmSettings as Microsoft.Msagl.SugiyamaLayoutSettings).AddUpDownConstraint("7", "T245");
            //(graph.LayoutAlgorithmSettings as Microsoft.Msagl.SugiyamaLayoutSettings).AddUpDownConstraint("7", "T30");
            //(graph.LayoutAlgorithmSettings as Microsoft.Msagl.SugiyamaLayoutSettings).AddUpDownConstraint("5", "4");
            //(graph.LayoutAlgorithmSettings as Microsoft.Msagl.SugiyamaLayoutSettings).AddUpDownConstraint("4", "3");

            gViewer.Graph = graph;

            // Verify vertical and neighbour constraints
            var node2 = graph.FindNode("2");
            var node4 = graph.FindNode("4");
            var node5 = graph.FindNode("5");
            var node10 = graph.FindNode("10");
            var node37 = graph.FindNode("37");
            var node38 = graph.FindNode("38");
            bool fXalign_37_38 = (Math.Abs(node37.GeometryNode.Center.X - node38.GeometryNode.Center.X) < 0.0001)
                                && (node37.GeometryNode.Center.Y > node38.GeometryNode.Center.Y);
            bool fXalign_38_4 = (Math.Abs(node37.GeometryNode.Center.X - node38.GeometryNode.Center.X) < 0.0001)
                                && (node37.GeometryNode.Center.Y > node38.GeometryNode.Center.Y);
            bool fXalign_4_5 =  (Math.Abs(node37.GeometryNode.Center.X - node38.GeometryNode.Center.X) < 0.0001)
                                && (node37.GeometryNode.Center.Y > node38.GeometryNode.Center.Y);
            bool fXalign = true;
            if (!fXalign_37_38 || !fXalign_38_4 || !fXalign_4_5) {
                Console.WriteLine();
                Console.WriteLine("Xalign tests failed");
                Console.WriteLine();
                fXalign = false;
            }

            bool fYalign = (Math.Abs(node2.GeometryNode.Center.Y - node10.GeometryNode.Center.Y) < 0.0001)
                            && (node2.GeometryNode.Center.X < node10.GeometryNode.Center.Y);
            if (!fYalign) {
                Console.WriteLine();
                Console.WriteLine("Yalign tests failed");
                Console.WriteLine();
            }

            if (fXalign && fYalign) {
                Console.WriteLine();
                Console.WriteLine("Xalign and Yalign tests passed");
                Console.WriteLine();
            }

            this.propertyGrid1.SelectedObject = graph;
        }

        private static void CreateSourceNode(Node a) {
            a.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Box;
            a.Attr.XRadius = 3;
            a.Attr.YRadius = 3;
            a.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Green;
            a.Attr.LineWidth = 10;
        }

        private void CreateTargetNode(Node a) {
            a.Attr.Shape = Microsoft.Msagl.Drawing.Shape.DoubleCircle;
            a.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightGray;

            a.Attr.LabelMargin = -4;
            a.UserData = this;
        }


        private void recalculateLayoutButton_Click(object sender, EventArgs e) {
            this.gViewer.Graph = this.propertyGrid1.SelectedObject as Microsoft.Msagl.Drawing.Graph;

        }

    }
}
