using System;
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
using Microsoft.Msagl.GraphControlSilverlight;

namespace GraphControlTest
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
            
            SetupGraphControl();

            CreateInitialGraph(GraphControlSilverlight.Graph);
            CreateInitialGraph(DGraph);

            GraphControlSilverlight.BeginLayoutWithConstraints();
            DGraph.BeginLayout();
        }

        private void CreateInitialGraph(DGraph dgraph)
        {
            var nodeA0 = dgraph.AddNode("A0");
            dgraph.AddNode("A1");
            dgraph.AddNode("A2");
            var nodeA3 = dgraph.AddNode("A3");
            nodeA0.Node.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Box;
            nodeA0.Node.Attr.XRadius = 5.0;
            nodeA0.Node.Attr.YRadius = 5.0;
            var edgeA0A1 = dgraph.AddEdgeBetweenNodes(dgraph.NodeMap["A0"], dgraph.NodeMap["A1"]);
            dgraph.AddEdgeBetweenNodes(dgraph.NodeMap["A0"], dgraph.NodeMap["A2"]);
            dgraph.AddEdgeBetweenNodes(dgraph.NodeMap["A2"], dgraph.NodeMap["A1"]);
            dgraph.AddEdgeBetweenNodes(dgraph.NodeMap["A0"], dgraph.NodeMap["A3"]);
            nodeA0.Label = new DTextLabel(nodeA0, new Microsoft.Msagl.Drawing.Label()) { Text = "Node A0" };
            nodeA3.Label = new DTextLabel(nodeA3, new Microsoft.Msagl.Drawing.Label()) { Text = "Node A3" };
            edgeA0A1.Label = new DTextLabel(edgeA0A1, new Microsoft.Msagl.Drawing.Label()) { Text = "Edge A0->A1" };
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
    }
}
