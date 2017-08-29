using FluentVisualizer.Core;
using FluentVisualizer.Models;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SameLayerSample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            GViewer gViewer = new GViewer() { Dock = DockStyle.Fill };
            SuspendLayout();
            Controls.Add(gViewer);
            ResumeLayout();
            var graph = new Graph();
            var sugiyamaSettings = (SugiyamaLayoutSettings)graph.LayoutAlgorithmSettings;

            var json = System.IO.File.ReadAllText("fluentGraph.json");
            var typeName = "com.microsoft.azure.management.redis.RedisCache";

            var nodes = JsonConvert.DeserializeObject<List<FluentNode>>(json); 
            foreach (var node in nodes)
            {
                if (node is FluentInterface ||
                    node is FluentMethod)
                {
                    graph.AddNode(node.Id);
                }
                else
                {
                    throw new NotImplementedException($"'{node.GetType()}' is not supported");
                }
            }

            var path = ShortestPathByDijkstra.GetPath(nodes, $"{typeName}.Definition.Blank", typeName);
            // now add edges
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                foreach (var childId in node.ChildrenIds)
                {
                    graph.AddEdge(node.Id, childId);
                }
            }

            graph.CreateGeometryGraph();

            // This one throws index out of bounds exception
            //sugiyamaSettings.AddUpDownVerticalConstraints(path.Select(nodeId => graph.FindNode(nodeId).GeometryNode).ToArray());
            
            for (int i = 0; i < path.Count - 1; i++)
            {
                sugiyamaSettings.AddUpDownConstraint(graph.FindNode(path[i]).GeometryNode, graph.FindNode(path[i + 1]).GeometryNode);
            }

            gViewer.Graph = graph;

        }
    }
}
