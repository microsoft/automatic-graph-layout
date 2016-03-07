using System;
using System.Collections.Generic;
using System.Linq;
using ArgsParser;
using OverlapGraphExperiments.Statistics;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Node = Microsoft.Msagl.Core.Layout.Node;
using System.Windows.Forms;

namespace OverlapGraphExperiments {
    /// <summary>
    /// This class is used to run some tests with different node overlap removal methods.
    /// </summary>
    internal class Program {

        private static void Main(string[] args) {
#if DEBUG && !SILVERLIGHT
            DisplayGeometryGraph.SetShowFunctions();
            // ProximityOverlapRemoval.DebugMode = true;
#endif
            var argsParser = new ArgsParser.ArgsParser(args);
            argsParser.AddOptionWithAfterStringWithHelp("-graphs", "number of graphs with circles to generate");
            argsParser.AddOptionWithAfterStringWithHelp("-circles", "number of circles per graph to  generate");
            argsParser.AddOptionWithAfterStringWithHelp("-nodes_per_circle", "number of nodes per circle");
            argsParser.AddOptionWithAfterStringWithHelp("-box_width", "the initial width of the rectangle to layout the nodes");
            argsParser.AddOptionWithAfterStringWithHelp("-test_dir", "the directory of test files");

            if (!argsParser.Parse())
            {
                Console.WriteLine(argsParser.ErrorMessage);
                Console.WriteLine(argsParser.UsageString());
                return;
            }
            string test_dir = argsParser.GetValueOfOptionWithAfterString("-test_dir");

            if (test_dir == null)
            {
                Console.WriteLine("-test_dir is not given, exiting");
                return;
            }
            bool circlesAreCreated = CreateCircleGraphsIfRequired(argsParser);
            OverlapRemovalTestSuite.ComparisonSuite(
                test_dir,
                "ResultsPrism-original-datasetTestSuite1.csv", false, !circlesAreCreated );
        }

        static void CleanDirectory(string dir)
        {
            System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(dir);
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories())
                subDirectory.Delete(true);
        }

        private static bool CreateCircleGraphsIfRequired(ArgsParser.ArgsParser argsParser)
        {
            int nOfGraphs = GetNumberOfGraphsToGenerate(argsParser);
            if (nOfGraphs == 0)
                return false;
            int nOfCircles = GetNumberOfCircles(argsParser);
            int nOfNodesPerCircle = GetNumberOfNodesPerCircle(argsParser);
            string dir = argsParser.GetValueOfOptionWithAfterString("-test_dir");
            if (System.IO.Directory.Exists(dir))
            {
                CleanDirectory(dir);
            } else
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            var random=new Random(1);

            for (int i = 0; i < nOfGraphs; i++)
                GenerateGraph(dir, i, random, argsParser);

            return true;
        }

        static void GenerateGraph(string dir, int i, Random random, ArgsParser.ArgsParser argsParser)
        {
            Graph graph = new Graph();
            string graphName = "circle_graph" + i;
            int nOfCrc=FillGraph(graph, random, argsParser, graphName);
            string fileName = System.IO.Path.Combine(dir, graphName + "_ncrc"+ nOfCrc+".dot");
            System.IO.File.WriteAllText(fileName, graph.ToString());
        }

        internal static void ShowGraph(Graph graph)
        {
            Form f = new Form();
            GViewer v = new GViewer();
            v.Dock = DockStyle.Fill;
            f.SuspendLayout();
            f.Controls.Add(v);
            f.ResumeLayout();
            v.NeedToCalculateLayout = false;
            v.Graph = graph;
            f.ShowDialog();
        }

        static double GetWidthOfGraph(ArgsParser.ArgsParser argsParser) {
            string s = argsParser.GetValueOfOptionWithAfterString("-box_width");
            if (s == null)
                return 1000;
            double ret;
            if (!double.TryParse(s, out ret))
            {
                Console.WriteLine("Cannot parse string '{0}' following option '-box width'; Returning 10 by default", s);
                return 1000;
            }
            return ret;
        }

        private static int FillGraph(Graph graph, Random random, ArgsParser.ArgsParser argsParser, string graphName)
        {
            graph.Label.Text = graphName;
            double width = GetWidthOfGraph(argsParser);
            double rad = width / 10; // the node radius
            int circles = GetNumberOfCircles(argsParser);
            int h = circles / 2;
            circles = h + random.Next() % circles;
            graph.CreateGeometryGraph();
            graph.GeometryGraph.Margins = width / 100;
            for(int i=0;i<circles;i++)
                AddCircleToGraph(graph, random, argsParser, width, rad);
            graph.GeometryGraph.UpdateBoundingBox();
            return circles;
        }

        private static void AddCircleToGraph(Graph graph, Random random, ArgsParser.ArgsParser argsParser, double w, double rad)
        {
            Point center = w * (new Point(random.NextDouble(), random.NextDouble()));
            int nodesPerCircle = GetNumberOfNodesPerCircle(argsParser);
            double angle = 2*Math.PI / nodesPerCircle;
            List<Microsoft.Msagl.Drawing.Node> circleNodes = new List<Microsoft.Msagl.Drawing.Node>();
            while (nodesPerCircle-- > 0)
                circleNodes.Add(AddCircleToGraphOnCenter(center, graph, rad, nodesPerCircle*angle));
            for (int i = 0; i < circleNodes.Count - 1; i++)
                AddEdge(circleNodes[i], circleNodes[i + 1], graph);
            AddEdge(circleNodes[circleNodes.Count - 1], circleNodes[0], graph);
        }

        private static void AddEdge(Microsoft.Msagl.Drawing.Node a, Microsoft.Msagl.Drawing.Node b, Graph graph)
        {
            var de = new Microsoft.Msagl.Drawing.Edge(a, b, ConnectionToGraph.Connected);
            de.Attr.LineWidth = Math.Max(1,(int)(a.Width/10));
            de.Attr.Color = new Microsoft.Msagl.Drawing.Color(100, 100, 100, 100);
            var ge = new Edge(a.GeometryNode, b.GeometryNode);
            Point start = a.GeometryNode.Center;
            Point end = b.GeometryNode.Center;
            Point d = (end - start) / 4;
            Point b1 = start + d;
            Point b2 = b1 + d;
            Point b3 = b2 + d;
            ge.EdgeGeometry.Curve = new CubicBezierSegment(start, b1, b2, b3);
            ge.EdgeGeometry.TargetArrowhead = new Arrowhead() { TipPosition = end};
            de.GeometryEdge = ge;
            ge.UserData = de;
        }

        private static Microsoft.Msagl.Drawing.Node AddCircleToGraphOnCenter(Point center, Graph graph, double rad, double angle)
        {
            var node = new Microsoft.Msagl.Drawing.Node(graph.NodeCount.ToString());
            node.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Circle;
            node.Attr.FillColor = new Microsoft.Msagl.Drawing.Color(100, 100, 100, 100);
            node.Attr.Color = node.Attr.FillColor;
            var nodeCenter = center + rad * (new Point(Math.Cos(angle), Math.Sin(angle)));
            var geomNode = new Node(CurveFactory.CreateCircle(rad / 3, nodeCenter));
            node.GeometryNode = geomNode;
            geomNode.UserData = node;
            graph.AddNode(node);
            graph.GeometryGraph.Nodes.Add(geomNode);
            return node;
        }

        private static int GetNumberOfGraphsToGenerate(ArgsParser.ArgsParser argsParser)
        {
            int ret;
            if (!argsParser.GetIntOptionValue("-graphs", out ret))
            {
                ret = 10;
            }
            return ret;
        }

        private static int GetNumberOfNodesPerCircle(ArgsParser.ArgsParser argsParser)
        {
            int ret;
            if (!argsParser.GetIntOptionValue("-nodes_per_circle", out ret))
            {
                ret = 10;
            }
            return ret;
        }

        private static int GetNumberOfCircles(ArgsParser.ArgsParser argsParser)
        {
            int ret;
            if (!argsParser.GetIntOptionValue("-circles", out ret))
            {
                ret = 10;
            }
            return ret;
        }
    }
}
