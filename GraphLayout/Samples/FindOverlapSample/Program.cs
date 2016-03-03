using System;
using System.Collections.Generic;
using System.Linq;
using ArgsParser;
using FindOverlapSample.Statistics;
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

namespace FindOverlapSample {
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

            bool circlesAreCreated = CreateDirectoryWithCirclesIfNeeded(argsParser);
            

            //          OverlapRemovalTestSuite.ComparisonSuite(@"C:\dev\GraphLayout\graphs\overlapSamples\debugOnly\", "DebugOnlyTestSuite1.csv", false);
            //          OverlapRemovalTestSuite.ComparisonSuite(@"C:\dev\GraphLayout\graphs\overlapSamples\", "ResultsOverlapRemovalTestSuite1.csv", false);
            //            OverlapRemovalTestSuite.ComparisonSuite(@"C:\dev\GraphLayout\graphs\overlapSamples\net50comp1\", "ResultsNet50comp1TestSuite1.csv", false);
            //          OverlapRemovalTestSuite.ComparisonSuite(@"C:\dev\GraphLayout\graphs\overlapSamples\large\", "ResultsLargeGraphsTestSuite1.csv", false);
            OverlapRemovalTestSuite.ComparisonSuite(
                test_dir,
                "ResultsPrism-original-datasetTestSuite1.csv", false, !circlesAreCreated );


            //            Console.ReadLine();
            //            var rootGraph = DotLoader.LoadFile(@"C:\dev\GraphLayout\graphs\overlapSamples\root.dot");

            ////          var rootGraph = DotLoader.LoadFile(@"C:\dev\GraphLayout\graphs\overlapSamples\net50comp1\net50comp_1.gv.dot");
            ////          var rootGraph = DotLoader.LoadFile(@"C:\dev\GraphLayout\graphs\overlapSamples\badvoro.gv.dot");
            ////          var rootGraph = DotLoader.LoadFile(@"C:\dev\GraphLayout\graphs\large\twittercrawl-sfdp.dot");
            ////          var oldPositions = rootGraph.Nodes.Select(v => v.Center).ToList();
            ////          LayoutAlgorithmSettings.ShowGraph(rootGraph);
            //            ProximityOverlapRemoval prism=new ProximityOverlapRemoval(rootGraph);
            //            prism.Settings.WorkInInches = true;
            //            prism.Settings.StressSettings.ResidualTolerance = 0.06;
            //#if DEBUG
            //            ProximityOverlapRemoval.DebugMode = false;
            //#endif 
            //            prism.RemoveOverlap();
            ////            var newPositions = rootGraph.Nodes.Select(v => v.Center).ToList();
            ////            var procrustes = Statistics.Statistics.ProcrustesStatistics(oldPositions, newPositions);
            ////            Console.WriteLine("ProcrustesStatistics: {0}",procrustes);
            //            
            //#if DEBUG
            //
            //            LayoutAlgorithmSettings.ShowGraph(rootGraph);
            //#endif
            //            Console.ReadLine();
        }

        static void CleanDirectory(string dir)
        {
            System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(dir);
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories())
                subDirectory.Delete(true);
        }

        private static bool CreateDirectoryWithCirclesIfNeeded(ArgsParser.ArgsParser argsParser)
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
            string fileName = System.IO.Path.Combine(dir, graphName + ".dot");
            FillGraph(graph, random, argsParser, graphName);
            //LayoutAlgorithmSettings.ShowGraph(graph.GeometryGraph);
            System.IO.File.WriteAllText(fileName, graph.ToString());
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

        private static void FillGraph(Graph graph, Random random, ArgsParser.ArgsParser argsParser, string graphName)
        {
            graph.Label.Text = graphName;
            double width = GetWidthOfGraph(argsParser);
            double rad = width / 10; // the node radius
            int circles = GetNumberOfCircles(argsParser);
            graph.CreateGeometryGraph();
            while (circles-- > 0)
                AddCircleToGraph(graph, random, argsParser, width, rad);

        }

        private static void AddCircleToGraph(Graph graph, Random random, ArgsParser.ArgsParser argsParser, double w, double rad)
        {
            Point center = w * (new Point(random.NextDouble(), random.NextDouble()));
            int nodesPerCircle = GetNumberOfNodesPerCircle(argsParser);
            double angle = 2*Math.PI / nodesPerCircle;
            while (nodesPerCircle-- > 0)
                AddCircleToGraphOnCenter(center, graph, rad, nodesPerCircle*angle);
        }

        private static void AddCircleToGraphOnCenter(Point center, Graph graph, double rad, double angle)
        {
            var node = new Microsoft.Msagl.Drawing.Node(graph.NodeCount.ToString());
            node.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Circle;
            var nodeCenter = center + rad * (new Point(Math.Cos(angle), Math.Sin(angle)));
            var geomNode = new Node(CurveFactory.CreateCircle(rad / 100, nodeCenter));
            node.GeometryNode = geomNode;
            geomNode.UserData = node;
            graph.AddNode(node);
            graph.GeometryGraph.Nodes.Add(geomNode);
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
