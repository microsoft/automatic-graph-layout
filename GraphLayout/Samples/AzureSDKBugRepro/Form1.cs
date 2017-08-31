using FluentVisualizer.Core;
using FluentVisualizer.Models;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
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
                    AddNode(graph, node);
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

            foreach (var n in graph.Nodes)
            {
                CreateBoundaryCurve(n);
            }

            // This one throws index out of bounds exception
            sugiyamaSettings.AddUpDownVerticalConstraints(path.Select(nodeId => graph.FindNode(nodeId).GeometryNode).ToArray());

            for (int i = 0; i < path.Count - 1; i++)
            {
                var u = graph.FindNode(path[i]);
                var v = graph.FindNode(path[i + 1]);
                foreach (var e in u.OutEdges)
                {
                    if (e.TargetNode == v)
                        e.Attr.Color = Microsoft.Msagl.Drawing.Color.Red;
                }
            }
            LayoutHelpers.CalculateLayout(graph.GeometryGraph, sugiyamaSettings, new Microsoft.Msagl.Core.CancelToken());
            gViewer.NeedToCalculateLayout = false;
            gViewer.Graph = graph;

        }

        private void CreateBoundaryCurve(Microsoft.Msagl.Drawing.Node node)
        {
            double w, h;
            var label = node.Label;
            var font = new Font(label.FontName, (float)label.FontSize);
            StringMeasure.MeasureWithFont(label.Text, font, out w, out h);
            node.Label.Width = w;
            node.Label.Height = h;
            node.Attr.Shape = Shape.DrawFromGeometry;
            node.GeometryNode.BoundaryCurve = CurveFactory.CreateRectangleWithRoundedCorners(1.2 * w, 1.2 * h, 3, 3, new Microsoft.Msagl.Core.Geometry.Point());
        }

        private static void AddNode(Graph graph, FluentNode node)
        {
            var n = graph.AddNode(node.Id);
            var labelString = (((FluentUINode)node).Type == "method") ? ((FluentUINode)node).Name : node.Id.Split('.').Last();
            n.LabelText = $"{labelString} {graph.NodeCount}";
        }
    }
}
