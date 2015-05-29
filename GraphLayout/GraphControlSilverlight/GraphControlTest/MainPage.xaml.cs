using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.GraphControlSilverlight;
using MsaglColor = Microsoft.Msagl.Drawing.Color;
using MsaglShape = Microsoft.Msagl.Drawing.Shape;

namespace GraphControlTest
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();

            SetupGraphControl();
            
            GraphControlSilverlight.Graph.Name = "GraphControl Test";
            CreateInitialGraph(GraphControlSilverlight.Graph);
            GraphControlSilverlight.BeginLayoutWithConstraints();
            
            DGraph.Name = "DGraph Test";
            CreateInitialGraph(DGraph);
            DGraph.BeginLayout();
            
            GraphControlForClusters.Graph.Name = "Clusters";
            CreateClusteredGraph();

            GraphControlForClusters_Complex.Name = "Complex Clusters";
            CreateComplexClusteredGraph();
            
            GraphControlForNesting.Graph.Name = "Nesting";
            CreateNestedGraph();
            
            GraphControlForNesting_Complex.Name = "Complex Nesting";
            CreateComplexNestedGraph();
            //*/
            //CreateGraphFromGeometry();
        }

        #region Normal Graphs

        private void CreateInitialGraph(DGraph dgraph)
        {
            var nodeA0 = dgraph.AddNode("A0");
            nodeA0.Node.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Box;
            nodeA0.Node.Attr.XRadius = 5.0;
            nodeA0.Node.Attr.YRadius = 5.0;
            nodeA0.Node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Green;
            nodeA0.Label = new DTextLabel(nodeA0, new Microsoft.Msagl.Drawing.Label()) { Text = "Node A0", Margin = new Thickness(5.0, 2.0, 5.0, 2.0) };
            dgraph.AddNode("A1");
            dgraph.AddNode("A2").Node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Blue;
            var nodeA3 = dgraph.AddNode("A3");
            var edgeA0A1 = dgraph.AddEdgeBetweenNodes(dgraph.NodeMap["A0"], dgraph.NodeMap["A1"]);
            dgraph.AddEdgeBetweenNodes(dgraph.NodeMap["A0"], dgraph.NodeMap["A2"]);
            dgraph.AddEdgeBetweenNodes(dgraph.NodeMap["A2"], dgraph.NodeMap["A1"]);
            dgraph.AddEdgeBetweenNodes(dgraph.NodeMap["A0"], dgraph.NodeMap["A3"]);
            nodeA3.Label = new DTextLabel(nodeA3, new Microsoft.Msagl.Drawing.Label()) { Text = "Node A3" };
            edgeA0A1.Label = new DTextLabel(edgeA0A1, new Microsoft.Msagl.Drawing.Label()) { Text = "Edge A0->A1" };
            dgraph.Graph.Attr.LayerDirection = Microsoft.Msagl.Drawing.LayerDirection.RL;
        }

        private void SetupGraphControl()
        {
            GraphControlSilverlight.AllowGraphEditing = true;
            GraphControlSilverlight.AllowLabelEditing = true;
            GraphControlSilverlight.ShowExperimentalControls = true;

            GraphControlSilverlight.AddNodeType(new NodeTypeEntry() { Name = "Rounded Box", Shape = Microsoft.Msagl.Drawing.Shape.Box, XRadius = 5.0, YRadius = 5.0 });
            GraphControlSilverlight.AddNodeType(new NodeTypeEntry() { Name = "Ellipse", Shape = Microsoft.Msagl.Drawing.Shape.Ellipse });
            GraphControlSilverlight.AddNodeType(new NodeTypeEntry() { Name = "Box", Shape = Microsoft.Msagl.Drawing.Shape.Box });
            GraphControlSilverlight.AddNodeType(new NodeTypeEntry() { Name = "Inv. House", Shape = Microsoft.Msagl.Drawing.Shape.InvHouse });
            GraphControlSilverlight.AddNodeType(new NodeTypeEntry() { Name = "House", Shape = Microsoft.Msagl.Drawing.Shape.House });
            GraphControlSilverlight.AddNodeType(new NodeTypeEntry() { Name = "Octagon", Shape = Microsoft.Msagl.Drawing.Shape.Octagon });
            GraphControlSilverlight.AddNodeType(new NodeTypeEntry() { Name = "Diamond", Shape = Microsoft.Msagl.Drawing.Shape.Diamond });
        }

        #endregion

        #region Clustering

        private void Clustering_ApplyNodeAttributes(DNode n)
        {
            n.DrawingNode.Attr.Color = MsaglColor.Green;
            n.DrawingNode.Attr.LineWidth = 2.0;
            n.DrawingNode.Attr.Shape = MsaglShape.Box;
            n.DrawingNode.Attr.XRadius = 5.0;
            n.DrawingNode.Attr.YRadius = 5.0;
        }

        private DNode Clustering_Cell_AddNode(DGraph graph, DCluster cluster, string id, string label)
        {
            var ret = graph.AddNode(id);
            if (label != null)
                ret.Label = new DTextLabel(ret, label);
            graph.AddNodeToCluster(cluster, ret);
            Clustering_ApplyNodeAttributes(ret);
            return ret;
        }

        private DEdge Clustering_Cell_AddEdge(DGraph graph, DNode source, DNode target, string label)
        {
            DEdge ret = graph.AddEdgeBetweenNodes(source, target);
            if (label != null)
                ret.Label = new DTextLabel(ret, label);
            ret.DrawingEdge.Attr.Color = MsaglColor.Red;
            return ret;
        }

        private void CreateMitosis(DGraph graph, DCluster mitosis)
        {
            var inNode = graph.AddNode("Mitosis_In");
            graph.AddNodeToCluster(mitosis, inNode);
            inNode.DrawingNode.Attr.Shape = MsaglShape.Circle;
            inNode.DrawingNode.Attr.FillColor = MsaglColor.Red;

            var g0 = Clustering_Cell_AddNode(graph, mitosis, "G0", "G0");

            var mitosis_cycle = graph.AddCluster(mitosis, "Mitosis_Cycle");
            Clustering_ApplyNodeAttributes(mitosis_cycle);
            var g1 = Clustering_Cell_AddNode(graph, mitosis_cycle, "G1", "G1");
            var s = Clustering_Cell_AddNode(graph, mitosis_cycle, "S", "S");
            var g2 = Clustering_Cell_AddNode(graph, mitosis_cycle, "G2", "G2");
            var m = Clustering_Cell_AddNode(graph, mitosis_cycle, "M", "M");

            Clustering_Cell_AddEdge(graph, inNode, g0, null);
            Clustering_Cell_AddEdge(graph, g0, g1, null);
            Clustering_Cell_AddEdge(graph, g1, s, null);
            Clustering_Cell_AddEdge(graph, s, g2, null);
            Clustering_Cell_AddEdge(graph, g2, m, null);
            Clustering_Cell_AddEdge(graph, mitosis_cycle, g0, "exit cell\ncycle");//*/
        }

        private void CreateMeiosis(DGraph graph, DCluster meiosis)
        {
            var inter = Clustering_Cell_AddNode(graph, meiosis, "Inter", "Inter");
            var rest = Clustering_Cell_AddNode(graph, meiosis, "Rest", "Rest");

            var meiosis_cycle = graph.AddCluster(meiosis, "Meiosis_Cycle");
            Clustering_ApplyNodeAttributes(meiosis_cycle);
            var pro = Clustering_Cell_AddNode(graph, meiosis_cycle, "Pro", "Pro");
            var meta = Clustering_Cell_AddNode(graph, meiosis_cycle, "Meta", "Meta");
            var ana = Clustering_Cell_AddNode(graph, meiosis_cycle, "Ana", "Ana");
            var telo = Clustering_Cell_AddNode(graph, meiosis_cycle, "Telo", "Telo");

            Clustering_Cell_AddEdge(graph, inter, pro, null);
            Clustering_Cell_AddEdge(graph, pro, meta, null);
            Clustering_Cell_AddEdge(graph, meta, ana, null);
            Clustering_Cell_AddEdge(graph, ana, telo, null);
            Clustering_Cell_AddEdge(graph, meiosis_cycle, inter, "exit cell\ncycle");
            Clustering_Cell_AddEdge(graph, meiosis_cycle, rest, null);//*/
        }

        private void CreateProliferation(DGraph graph, DCluster proliferation)
        {
            var inNode = graph.AddNode("Proliferation_in");
            inNode.DrawingNode.Attr.Shape = MsaglShape.Circle;
            inNode.DrawingNode.Attr.FillColor = MsaglColor.Red;
            graph.AddNodeToCluster(proliferation, inNode);

            var splitter = graph.AddNode("Proliferation_Splitter");
            splitter.DrawingNode.Attr.Shape = MsaglShape.Circle;
            splitter.DrawingNode.Attr.FillColor = MsaglColor.Blue;
            graph.AddNodeToCluster(proliferation, splitter);

            var mitosis = graph.AddCluster(proliferation, "Mitosis");
            Clustering_ApplyNodeAttributes(mitosis);
            CreateMitosis(graph, mitosis);

            var meiosis = graph.AddCluster(proliferation, "Meiosis");
            Clustering_ApplyNodeAttributes(meiosis);
            CreateMeiosis(graph, meiosis);

            Clustering_Cell_AddEdge(graph, inNode, mitosis, null);
            Clustering_Cell_AddEdge(graph, mitosis, splitter, "Early Meiosis");
            Clustering_Cell_AddEdge(graph, splitter, graph.NodeMap["G0"], null);
            Clustering_Cell_AddEdge(graph, splitter, graph.NodeMap["Inter"], null);//*/
        }

        private void CreateGamete(DGraph graph, DCluster gamete)
        {
            var inNode = graph.AddNode("Gamete_in");
            inNode.DrawingNode.Attr.Shape = MsaglShape.Circle;
            inNode.DrawingNode.Attr.FillColor = MsaglColor.Red;
            graph.AddNodeToCluster(gamete, inNode);

            var splitter = graph.AddNode("Gamete_Splitter");
            splitter.DrawingNode.Attr.Shape = MsaglShape.Circle;
            splitter.DrawingNode.Attr.FillColor = MsaglColor.Blue;
            graph.AddNodeToCluster(gamete, splitter);

            var sperm = Clustering_Cell_AddNode(graph, gamete, "Sperm", "Sperm");
            var oocyte = Clustering_Cell_AddNode(graph, gamete, "Oocyte", "Oocyte");
            var matureOocyte = Clustering_Cell_AddNode(graph, gamete, "MatureOocyte", "Mature\nOocyte");
            var zygote = Clustering_Cell_AddNode(graph, gamete, "Zygote", "Zygote");

            Clustering_Cell_AddEdge(graph, inNode, splitter, null);
            Clustering_Cell_AddEdge(graph, splitter, sperm, "Sperm_Effector_Act");
            Clustering_Cell_AddEdge(graph, splitter, oocyte, "Oocyte_Effector_Act");
            Clustering_Cell_AddEdge(graph, oocyte, matureOocyte, "Maturation");
            Clustering_Cell_AddEdge(graph, matureOocyte, zygote, "Fertilization");//*/
        }

        private void CreateDifferentiation(DGraph graph, DCluster differentiation)
        {
            var inNode = graph.AddNode("Differentiation_in");
            inNode.DrawingNode.Attr.Shape = MsaglShape.Circle;
            inNode.DrawingNode.Attr.FillColor = MsaglColor.Red;
            graph.AddNodeToCluster(differentiation, inNode);

            var precursor = Clustering_Cell_AddNode(graph, differentiation, "Precursor", "Precursor");
            var earlyMeiosis = Clustering_Cell_AddNode(graph, differentiation, "EarlyMeiosis", "Early Meiosis");
            var meiosis = Clustering_Cell_AddNode(graph, differentiation, "Diff_Meiosis", "Meiosis");

            var gamete = graph.AddCluster(differentiation, "Gamete");
            Clustering_ApplyNodeAttributes(gamete);
            CreateGamete(graph, gamete);

            Clustering_Cell_AddEdge(graph, inNode, precursor, null);
            Clustering_Cell_AddEdge(graph, precursor, earlyMeiosis, "GLD-1_Act\nOR\nGLD-2_Act");
            Clustering_Cell_AddEdge(graph, earlyMeiosis, meiosis, "Pachytene");
            Clustering_Cell_AddEdge(graph, meiosis, gamete, "MEK-2_Act\nAND\nMPK-1_Act");//*/
        }

        private void CreateComplexClusteredGraph()
        {
            var graph = GraphControlForClusters_Complex.Graph;

            var root = graph.AddRootCluster();

            var cell = graph.AddCluster(root, "Cell");
            Clustering_ApplyNodeAttributes(cell);

            var proliferation = graph.AddCluster(cell, "Proliferation");
            //proliferation.Label = new DLabel(proliferation, new TextBlock() { Text = "Proliferation", FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Top });
            CreateProliferation(graph, proliferation);

            var differentiation = graph.AddCluster(cell, "Differentiation");
            CreateDifferentiation(graph, differentiation);//*/

            GraphControlForClusters_Complex.LayeredLayout = false;
            GraphControlForClusters_Complex.Graph.GraphLayoutStarting += Graph_Complex_GraphLayoutStarting;
            GraphControlForClusters_Complex.Graph.GraphLayoutDone += Graph_Complex_GraphLayoutDone;
            GraphControlForClusters_Complex.Graph.BeginLayout();
        }

        private List<DEdge> m_SpecialEdges_Complex = new List<DEdge>();

        void Graph_Complex_GraphLayoutStarting(object sender, EventArgs e)
        {
            var g = (sender as DGraph);
            m_SpecialEdges_Complex.Clear();
            foreach (DEdge edge in g.Edges)
            {
                if (edge.Source.ParentObject == edge.Target.ParentObject) // Siblings: OK
                    continue;
                if (edge.Target.IsAncestorOf(edge.Source)) // Object->Ancestor: OK
                    continue;
                m_SpecialEdges_Complex.Add(edge);
            }
            foreach (DEdge edge in m_SpecialEdges_Complex)
                g.RemoveEdge(edge, false);
        }

        void Graph_Complex_GraphLayoutDone(object sender, EventArgs e)
        {
            var g = (sender as DGraph);
            List<DEdge> newEdges = new List<DEdge>();
            foreach (DEdge de in m_SpecialEdges_Complex)
            {
                newEdges.Add(g.AddEdgeBetweenNodes(de.Source, de.Target));
            }
            g.RouteEdgeSetSpline(newEdges);
            g.PopulateChildren();
        }

        private void CreateClusteredGraph()
        {
            var g = GraphControlForClusters.Graph;

            // Add non-clustered elements.
            var node11 = g.AddNode("ID1.1");
            var node12 = g.AddNode("ID1.2");
            var node2 = g.AddNode("ID2");
            var node31 = g.AddNode("ID3.1");
            var node32 = g.AddNode("ID3.2");
            var edge11_12 = g.AddEdgeBetweenNodes(node11, node12);
            var edge31_32 = g.AddEdgeBetweenNodes(node31, node32);

            // Add clusters.
            var root = g.AddRootCluster();
            g.AddNodeToCluster(root, node2);
            var cluster1 = g.AddCluster(root, "ID1");
            g.AddNodeToCluster(cluster1, node11);
            g.AddNodeToCluster(cluster1, node12);
            var cluster3 = g.AddCluster(root, "ID3");
            g.AddNodeToCluster(cluster3, node31);
            g.AddNodeToCluster(cluster3, node32);
            g.AddEdgeBetweenNodes(cluster1, node2);
            g.AddEdgeBetweenNodes(node2, cluster3);

            // Add cross-edges.
            var edge11_3 = g.AddEdgeBetweenNodes(node11, cluster3);
            var edge12_31 = g.AddEdgeBetweenNodes(node12, node31);

            // Set some labels.
            node11.Label = new DTextLabel(node11) { Text = "Node 1.1" };
            node12.Label = new DTextLabel(node12) { Text = "Node 1.2" };
            node31.Label = new DTextLabel(node31) { Text = "Node 3.1" };
            node32.Label = new DTextLabel(node32) { Text = "Node 3.2" };
            node2.Label = new DTextLabel(node2) { Text = "Node 2" };

            GraphControlForClusters.LayeredLayout = false;
            GraphControlForClusters.Graph.GraphLayoutStarting += Graph_GraphLayoutStarting;
            GraphControlForClusters.Graph.GraphLayoutDone += Graph_GraphLayoutDone;
            GraphControlForClusters.Graph.BeginLayout();
        }

        private List<DEdge> m_SpecialEdges = new List<DEdge>();

        void Graph_GraphLayoutStarting(object sender, EventArgs e)
        {
            var g = (sender as DGraph);
            m_SpecialEdges.Clear();
            foreach (DEdge edge in g.Edges)
            {
                if (edge.Source.ParentObject == edge.Target.ParentObject) // Siblings: OK
                    continue;
                if (edge.Target.IsAncestorOf(edge.Source)) // Object->Ancestor: OK
                    continue;
                m_SpecialEdges.Add(edge);
            }
            foreach (DEdge edge in m_SpecialEdges)
                g.RemoveEdge(edge, false);
        }

        void Graph_GraphLayoutDone(object sender, EventArgs e)
        {
            var g = (sender as DGraph);
            List<DEdge> newEdges = new List<DEdge>();
            foreach (DEdge de in m_SpecialEdges)
            {
                newEdges.Add(g.AddEdgeBetweenNodes(de.Source, de.Target));
            }
            g.RouteEdgeSetSpline(newEdges);
            g.PopulateChildren();
        }

        #endregion

        #region Nesting

        private DNode Nesting_Cell_AddNode(DGraph graph, string id, string label)
        {
            DNode ret = graph.AddNode(id);
            if (label != null)
                ret.Label = new DTextLabel(ret, label);
            ret.DrawingNode.Attr.Color = MsaglColor.Green;
            ret.DrawingNode.Attr.LineWidth = 2.0;
            ret.DrawingNode.Attr.Shape = MsaglShape.Box;
            ret.DrawingNode.Attr.XRadius = 5.0;
            ret.DrawingNode.Attr.YRadius = 5.0;
            return ret;
        }

        private DEdge Nesting_Cell_AddEdge(DGraph graph, DNode source, DNode target, string label)
        {
            DEdge ret = graph.AddEdgeBetweenNodes(source, target);
            if (label != null)
                ret.Label = new DTextLabel(ret, label);
            ret.DrawingEdge.Attr.Color = MsaglColor.Red;
            return ret;
        }

        private void Nesting_CreateMitosisGraph(DGraph graph)
        {
            DNode inNode = graph.AddNode("Mi_in");
            inNode.DrawingNode.Attr.Shape = MsaglShape.Circle;
            inNode.DrawingNode.Attr.FillColor = MsaglColor.Red;
            DNode g0Node = Nesting_Cell_AddNode(graph, "Mi_g0", "G0");

            DNode cycleNode = Nesting_Cell_AddNode(graph, "Mi_cycle", null);
            DGraph cycleGraph = new DGraph() { Name = "Mitosis Cycle" };
            cycleGraph.ConfigureIncrementalLayout();
            DNode c_G1_Node = Nesting_Cell_AddNode(cycleGraph, "Mi_cycle_G1", "G1");
            DNode c_S_Node = Nesting_Cell_AddNode(cycleGraph, "Mi_cycle_S", "S");
            DNode c_G2_Node = Nesting_Cell_AddNode(cycleGraph, "Mi_cycle_G2", "G2");
            DNode c_M_Node = Nesting_Cell_AddNode(cycleGraph, "Mi_cycle_M", "M");
            Nesting_Cell_AddEdge(cycleGraph, c_G1_Node, c_S_Node, null);
            Nesting_Cell_AddEdge(cycleGraph, c_S_Node, c_G2_Node, null);
            Nesting_Cell_AddEdge(cycleGraph, c_G2_Node, c_M_Node, null);
            cycleNode.Label = new DNestedGraphLabel(cycleNode, cycleGraph);

            Nesting_Cell_AddEdge(graph, cycleNode, g0Node, "exit cell\ncycle");
            Nesting_Cell_AddEdge(graph, inNode, g0Node, null);

            DEdge g0_g1_edge = graph.AddEdgeBetweenNodes(g0Node, c_G1_Node);
            g0_g1_edge.DrawingEdge.Attr.Color = MsaglColor.Red;//*/
        }

        private void Nesting_CreateMeiosisGraph(DGraph graph)
        {
            DNode interNode = Nesting_Cell_AddNode(graph, "Me_inter", "Inter");
            DNode restNode = Nesting_Cell_AddNode(graph, "Me_rest", "Rest");

            DNode cycleNode = Nesting_Cell_AddNode(graph, "Me_cycle", null);
            DGraph cycleGraph = new DGraph() { Name = "Meiosis Cycle" };
            cycleGraph.ConfigureSugiyamaLayout(Math.PI / 2.0);
            DNode c_Pro_Node = Nesting_Cell_AddNode(cycleGraph, "Me_cycle_Pro", "Pro");
            DNode c_Meta_Node = Nesting_Cell_AddNode(cycleGraph, "Me_cycle_Meta", "Meta");
            DNode c_Ana_Node = Nesting_Cell_AddNode(cycleGraph, "Me_cycle_Ana", "Ana");
            DNode c_Telo_Node = Nesting_Cell_AddNode(cycleGraph, "Me_cycle_Telo", "Telo");
            Nesting_Cell_AddEdge(cycleGraph, c_Pro_Node, c_Meta_Node, null);
            Nesting_Cell_AddEdge(cycleGraph, c_Meta_Node, c_Ana_Node, null);
            Nesting_Cell_AddEdge(cycleGraph, c_Ana_Node, c_Telo_Node, null);
            cycleNode.Label = new DNestedGraphLabel(cycleNode, cycleGraph);

            Nesting_Cell_AddEdge(graph, cycleNode, interNode, "exit cell\ncycle");
            Nesting_Cell_AddEdge(graph, cycleNode, restNode, null);

            DEdge inter_pro_edge = graph.AddEdgeBetweenNodes(interNode, c_Pro_Node);
            inter_pro_edge.DrawingEdge.Attr.Color = MsaglColor.Red;//*/
        }

        private void Nesting_CreateProliferationGraph(DGraph graph)
        {
            DNode inNode = graph.AddNode("Pr_in");
            inNode.DrawingNode.Attr.Shape = MsaglShape.Circle;
            inNode.DrawingNode.Attr.FillColor = MsaglColor.Red;

            DNode mitosisNode = Nesting_Cell_AddNode(graph, "Pr_mitosis", null);
            Grid mitosisContainer = new Grid();
            mitosisContainer.RowDefinitions.Add(new RowDefinition());
            mitosisContainer.RowDefinitions.Add(new RowDefinition());
            mitosisContainer.Children.Add(new TextBlock() { Text = "Mitosis", TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold });
            mitosisNode.Label = new DNestedGraphLabel(mitosisNode, mitosisContainer);
            DGraph mitosisGraph = new DGraph(mitosisNode.Label as DNestedGraphLabel) { Name = "Mitosis" };
            Nesting_CreateMitosisGraph(mitosisGraph);
            Grid.SetRow(mitosisGraph, 1);
            mitosisContainer.Children.Add(mitosisGraph);
            (mitosisNode.Label as DNestedGraphLabel).Graphs.Add(mitosisGraph);

            DNode splitterNode = graph.AddNode("Pr_sp");
            splitterNode.DrawingNode.Attr.Shape = MsaglShape.Circle;
            splitterNode.DrawingNode.Attr.FillColor = MsaglColor.Blue;

            DNode meiosisNode = Nesting_Cell_AddNode(graph, "Pr_meiosis", null);
            Grid meiosisContainer = new Grid();
            meiosisContainer.RowDefinitions.Add(new RowDefinition());
            meiosisContainer.RowDefinitions.Add(new RowDefinition());
            meiosisContainer.Children.Add(new TextBlock() { Text = "Meiosis", TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold });
            meiosisNode.Label = new DNestedGraphLabel(meiosisNode, meiosisContainer);
            DGraph meiosisGraph = new DGraph(meiosisNode.Label as DNestedGraphLabel) { Name = "Meiosis" };
            Nesting_CreateMeiosisGraph(meiosisGraph);
            Grid.SetRow(meiosisGraph, 1);
            meiosisContainer.Children.Add(meiosisGraph);
            (meiosisNode.Label as DNestedGraphLabel).Graphs.Add(meiosisGraph);

            Nesting_Cell_AddEdge(graph, inNode, mitosisNode, null);
            Nesting_Cell_AddEdge(graph, mitosisNode, splitterNode, "Early Meiosis");

            DEdge splitter_g0_edge = graph.AddEdgeBetweenNodes(splitterNode, mitosisGraph.NodeMap["Mi_g0"]);
            splitter_g0_edge.DrawingEdge.Attr.Color = MsaglColor.Red;
            DEdge splitter_inter_edge = graph.AddEdgeBetweenNodes(splitterNode, meiosisGraph.NodeMap["Me_inter"]);
            splitter_inter_edge.DrawingEdge.Attr.Color = MsaglColor.Red;//*/
        }

        private void Nesting_CreateGameteGraph(DGraph graph)
        {
            DNode inNode = graph.AddNode("Ga_in");
            inNode.DrawingNode.Attr.Shape = MsaglShape.Circle;
            inNode.DrawingNode.Attr.FillColor = MsaglColor.Red;
            DNode splitterNode = graph.AddNode("Ga_sp");
            splitterNode.DrawingNode.Attr.Shape = MsaglShape.Circle;
            splitterNode.DrawingNode.Attr.FillColor = MsaglColor.Blue;

            DNode spermNode = Nesting_Cell_AddNode(graph, "Ga_sperm", "Sperm");
            DNode oocyteNode = Nesting_Cell_AddNode(graph, "Ga_oocyte", "Oocyte");
            DNode matureOocyteNode = Nesting_Cell_AddNode(graph, "Ga_matureOocyte", "Mature\nOocyte");
            DNode zygoteNode = Nesting_Cell_AddNode(graph, "Ga_zygote", "Zygote");

            Nesting_Cell_AddEdge(graph, inNode, splitterNode, null);
            Nesting_Cell_AddEdge(graph, splitterNode, spermNode, "Sperm_Effector_Act");
            Nesting_Cell_AddEdge(graph, splitterNode, oocyteNode, "Oocyte_Effector_Act");
            Nesting_Cell_AddEdge(graph, oocyteNode, matureOocyteNode, "Maturation");
            Nesting_Cell_AddEdge(graph, matureOocyteNode, zygoteNode, "Fertilization");
        }

        private void CreateDifferentiationGraph(DGraph graph)
        {
            DNode inNode = graph.AddNode("Di_in");
            inNode.DrawingNode.Attr.Shape = MsaglShape.Circle;
            inNode.DrawingNode.Attr.FillColor = MsaglColor.Red;
            DNode precursorNode = Nesting_Cell_AddNode(graph, "Di_precursor", "Precursor");
            DNode earlyMeiosisNode = Nesting_Cell_AddNode(graph, "Di_early_meiosis", "Early Meiosis");
            DNode meiosisNode = Nesting_Cell_AddNode(graph, "Di_meiosis", "Meiosis");

            DNode gameteNode = Nesting_Cell_AddNode(graph, "Di_gamete", null);
            Grid gameteContainer = new Grid();
            gameteContainer.RowDefinitions.Add(new RowDefinition());
            gameteContainer.RowDefinitions.Add(new RowDefinition());
            gameteContainer.Children.Add(new TextBlock() { Text = "Gamete", TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold });
            gameteNode.Label = new DNestedGraphLabel(gameteNode, gameteContainer);
            DGraph gameteGraph = new DGraph(gameteNode.Label as DNestedGraphLabel) { Name = "Gamete" };
            Nesting_CreateGameteGraph(gameteGraph);
            Grid.SetRow(gameteGraph, 1);
            gameteContainer.Children.Add(gameteGraph);
            (gameteNode.Label as DNestedGraphLabel).Graphs.Add(gameteGraph);

            Nesting_Cell_AddEdge(graph, inNode, precursorNode, null);
            Nesting_Cell_AddEdge(graph, precursorNode, earlyMeiosisNode, "GLD-1_Act\nOR\nGLD-2_Act");
            Nesting_Cell_AddEdge(graph, earlyMeiosisNode, meiosisNode, "Pachytene");
            Nesting_Cell_AddEdge(graph, meiosisNode, gameteNode, "MEK-2_Act\nAND\nMPK-1_Act");
        }

        private void CreateComplexNestedGraph()
        {
            DGraph outerMost = GraphControlForNesting_Complex.Graph;
            outerMost.Name = "Outermost";
            DNode cellNode = Nesting_Cell_AddNode(outerMost, "Cell", "Cell");
            Grid cell_container = new Grid();
            cell_container.RowDefinitions.Add(new RowDefinition());
            cell_container.RowDefinitions.Add(new RowDefinition());
            cell_container.RowDefinitions.Add(new RowDefinition());
            cell_container.ColumnDefinitions.Add(new ColumnDefinition());
            cell_container.ColumnDefinitions.Add(new ColumnDefinition());
            cellNode.Label = new DNestedGraphLabel(cellNode, cell_container);
            TextBlock cell_header = new TextBlock() { Text = "Cell", TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold };
            Grid.SetColumnSpan(cell_header, 2);
            cell_container.Children.Add(cell_header);
            TextBlock proliferation_header = new TextBlock() { Text = "Proliferation", TextAlignment = TextAlignment.Center };
            Grid.SetRow(proliferation_header, 1);
            cell_container.Children.Add(proliferation_header);
            TextBlock differentiation_header = new TextBlock() { Text = "Differentiation", TextAlignment = TextAlignment.Center };
            Grid.SetRow(differentiation_header, 1);
            Grid.SetColumn(differentiation_header, 1);
            cell_container.Children.Add(differentiation_header);

            DGraph proliferation = new DGraph(cellNode.Label as DNestedGraphLabel) { Name = "Proliferation", VerticalAlignment = VerticalAlignment.Top };
            Nesting_CreateProliferationGraph(proliferation);
            Grid.SetRow(proliferation, 2);
            cell_container.Children.Add(proliferation);
            (cellNode.Label as DNestedGraphLabel).Graphs.Add(proliferation);

            DGraph differentiation = new DGraph(cellNode.Label as DNestedGraphLabel) { Name = "Differentiation", VerticalAlignment = VerticalAlignment.Top };
            CreateDifferentiationGraph(differentiation);
            Grid.SetRow(differentiation, 2);
            Grid.SetColumn(differentiation, 1);
            cell_container.Children.Add(differentiation);
            (cellNode.Label as DNestedGraphLabel).Graphs.Add(differentiation);

            Dispatcher.BeginInvoke(() => outerMost.BeginLayout());
        }

        private void CreateNestedGraph()
        {
            var g = GraphControlForNesting.Graph;

            // Create first nested graph.
            var inner1 = new DGraph() { Name = "Inner 1" };
            var node11 = inner1.AddNode("ID1.1");
            var node12 = inner1.AddNode("ID1.2");
            var edge11_12 = inner1.AddEdgeBetweenNodes(node11, node12);

            // Create second nested graph.
            var inner3 = new DGraph() { Name = "Inner 2" };
            var node31 = inner3.AddNode("ID3.1");
            var node32 = inner3.AddNode("ID3.2");
            var edge31_32 = inner3.AddEdgeBetweenNodes(node31, node32);

            // Create outer graph.
            var node1 = g.AddNode("ID1");
            node1.Label = new DNestedGraphLabel(node1, inner1);
            node1.DrawingNode.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Box;
            var node2 = g.AddNode("ID2");
            var node3 = g.AddNode("ID3");
            node3.Label = new DNestedGraphLabel(node3, inner3);
            node3.DrawingNode.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Box;
            var edge1_2 = g.AddEdgeBetweenNodes(node1, node2);
            var edge2_3 = g.AddEdgeBetweenNodes(node2, node3);

            // Set some labels.
            node11.Label = new DTextLabel(node11) { Text = "Node 1.1" };
            node12.Label = new DTextLabel(node12) { Text = "Node 1.2" };
            node31.Label = new DTextLabel(node31) { Text = "Node 3.1" };
            node32.Label = new DTextLabel(node32) { Text = "Node 3.2" };
            node2.Label = new DTextLabel(node2) { Text = "Node 2" };

            DEdge crossEdge1 = g.AddEdgeBetweenNodes(node11, node3);
            crossEdge1.Label = new DTextLabel(crossEdge1, "cross edge");
            DEdge crossEdge2 = g.AddEdgeBetweenNodes(node12, node31);

            g.BeginLayout();
        }

        #endregion

        #region From Geometry

        private void CreateGraphFromGeometry()
        {
            var gg = GeometryTest.Create();
            SetGeometryGraph(gg);
        }

        public void SetGeometryGraph(Microsoft.Msagl.Core.Layout.GeometryGraph gg)
        {
            GeometryTest.Layout(gg);
            var dg = FromGeometry.CreateDrawingGraph(gg);
            var dgraph = DGraph.FromDrawingGraph(dg);
            dgraph.Name = "From Geometry";
            FromGeometryContainer.Children.Insert(0, dgraph);
        }

        private void LayoutGeometry_Click(object sender, RoutedEventArgs e)
        {
            var dgraph = FromGeometryContainer.Children[0] as DGraph;
            var dg = dgraph.Graph;
            GeometryTest.Layout(dg.GeometryGraph);
            dgraph.Invalidate();
        }

        #endregion

        private void GeometryTest_Click(object sender, RoutedEventArgs e)
        {
            GeometryTest.Go();
        }
    }
}
