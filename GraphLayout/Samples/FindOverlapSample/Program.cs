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
        double _nodeWidth;
        double _circleRadius;
        ArgsParser.ArgsParser _argsParser;
        int _nofCircles;
        int _nofTriangles;
        double _graphWidth;
        string _testDir;
        int _nodesPerCircle;
        Random _random;
        
        public Program(ArgsParser.ArgsParser argsParser)
        {
            _argsParser = argsParser;
            _random = new Random(1);
        }

        private static void Main(string[] args) {
#if DEBUG && !SILVERLIGHT
            DisplayGeometryGraph.SetShowFunctions();
            // ProximityOverlapRemoval.DebugMode = true;
#endif
            var argsParser = new ArgsParser.ArgsParser(args);
            argsParser.AddOptionWithAfterStringWithHelp("-graphs", "number of graphs with circles to generate");
            argsParser.AddOptionWithAfterStringWithHelp("-circles", "number of circles per graph to  generate");
            argsParser.AddOptionWithAfterStringWithHelp("-fcrc", "number of circles per graph to  generate");
            argsParser.AddOptionWithAfterStringWithHelp("-triangles", "number of triangles per graph to  generate");
            argsParser.AddOptionWithAfterStringWithHelp("-nodes_per_circle", "number of nodes per circle");
            argsParser.AddOptionWithAfterStringWithHelp("-box_width", "the initial width of the rectangle to layout the nodes");
            argsParser.AddOptionWithAfterStringWithHelp("-test_dir", "the directory of test files");

            if (!argsParser.Parse())
            {
                Console.WriteLine(argsParser.ErrorMessage);
                Console.WriteLine(argsParser.UsageString());
                return;
            }
            var program = new Program(argsParser);
            program.Run();
        }

        private void Run()
        {
            _testDir = _argsParser.GetValueOfOptionWithAfterString("-test_dir");

            if (_testDir == null)
            {
                Console.WriteLine("-test_dir is not given, exiting");
                return;
            }
            _graphWidth = GetWidthOfGraph();
            _circleRadius = _graphWidth / 10;
            _nofCircles = GetNumberOfCircles();
            _nodesPerCircle = GetNumberOfNodesPerCircle();
            _nodeWidth = 2 * _circleRadius / 3;

            bool artificialGraphsAreCreated = CreateArtificialGraphsIfRequired();
            OverlapRemovalTestSuite.ComparisonSuite(
                _testDir,
                "ResultsPrism-original-datasetTestSuite1.csv", false, !artificialGraphsAreCreated);
        }

        static void CleanDirectory(string dir)
        {
            System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(dir);
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories())
                subDirectory.Delete(true);
        }

        private bool CreateArtificialGraphsIfRequired()
        {
            int nOfGraphs = GetNumberOfGraphsToGenerate(_argsParser);
            if (nOfGraphs == 0)
                return false;
            
            if (System.IO.Directory.Exists(_testDir))
            {
                CleanDirectory(_testDir);
            } else
            {
                System.IO.Directory.CreateDirectory(_testDir);
            }

            for (int i = 0; i < nOfGraphs; i++)
                GenerateGraph(i);

            return true;
        }

        void GenerateGraph(int i)
        {
            Graph graph = new Graph();
            string graphName = "graph" + i;
            string signature = FillGraph(graph, graphName);
            string fileName = System.IO.Path.Combine(_testDir, graphName + signature+".dot");
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

        double GetWidthOfGraph() {
            string s = _argsParser.GetValueOfOptionWithAfterString("-box_width");
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

        private string FillGraph(Graph graph, string graphName)
        {
            graph.Label.Text = graphName;
            graph.CreateGeometryGraph();
            graph.GeometryGraph.Margins = _graphWidth / 30;
            int circles = AddCircles(graph);
            int triangles = AddTriangles(graph);
            graph.GeometryGraph.UpdateBoundingBox();
            return string.Format("crc{0}tri{1}", circles, triangles);
        }

        private int AddCircles(Graph graph)
        {
            if (_nofCircles > 0)
            {
                int h = _nofCircles / 2;
                int circles = h + _random.Next() % _nofCircles;
                for (int i = 0; i < circles; i++)
                    AddCircleToGraph(graph);
                return circles;
            }
            return 0;
        }

        private int AddTriangles(Graph graph)
        {
            _argsParser.GetIntOptionValue("-triangles", out _nofTriangles);
            if (_nofTriangles == 0) return 0;
            int h = _nofTriangles / 2;
            int triangles = Math.Max(h + _random.Next() % _nofTriangles,1);
            Console.WriteLine("generating {0} triangles", triangles);
            for (int i = 0; i < triangles; i++)
            {
                AddTriangle(graph);
            }
            return triangles;
        }

        void AddTriangle(Graph graph)
        {
            Point center = RandomCenter();
            double angle = _random.NextDouble() * Math.PI;
            foreach (Point nodeCenter in GetNodeCentersForTriangle(center, angle))
                CreateNodeOnCenter(graph, nodeCenter);

        }

        private IEnumerable<Point> GetNodeCentersForTriangle(Point center, double angle)
        {
            Point []verts = GetTriangleVertices(center, angle);
            for (int i=0;i<3;i++)
            {
                yield return verts[i];
                foreach (Point p in GetTriangleSide(verts[i], verts[i + 1]))
                    yield return p;

            }


        }

        private IEnumerable<Point> GetTriangleSide(Point a, Point b)
        {
            Point ab = b - a;
            int n = (int)((b- a).Length / _nodeWidth)+1; // we already have two nodes at the ends, so there will be overlaps
            for (int i = 1; i < n; i++)
                yield return a + i*(b-a)/n;

        }

        private Point[] GetTriangleVertices(Point center, double angle)
        {

            Point[] ret = new Point[4];
            for(int i=0; i < 3; i++)
            {
                ret[i] = center + 2*_circleRadius * new Point(Math.Cos(angle + i * 2*Math.PI / 3), Math.Sin(angle + i * 2* Math.PI / 3));
            }
            ret[3] = ret[0];
            return ret;
        }

        private void AddCircleToGraph(Graph graph)
        {
            Point center = RandomCenter();
            double angle = 2 * Math.PI / _nodesPerCircle;
            List<Microsoft.Msagl.Drawing.Node> circleNodes = new List<Microsoft.Msagl.Drawing.Node>();
            for (int i = 0; i < _nodesPerCircle; i++)
                circleNodes.Add(AddNodeToCircle(center, graph, i * angle));
            for (int i = 0; i < circleNodes.Count - 1; i++)
                AddEdge(circleNodes[i], circleNodes[i + 1], graph);
            AddEdge(circleNodes[circleNodes.Count - 1], circleNodes[0], graph);
        }

        private Point RandomCenter()
        {
            return _graphWidth * (new Point(_random.NextDouble(), _random.NextDouble()));
        }

        private static void AddEdge(Microsoft.Msagl.Drawing.Node a, Microsoft.Msagl.Drawing.Node b, Graph graph)
        {
            var de = new Microsoft.Msagl.Drawing.Edge(a, b, ConnectionToGraph.Connected);
            de.Attr.LineWidth = Math.Max(1,(int)(a.Width/10));
            de.Attr.Color = new Microsoft.Msagl.Drawing.Color(200, 100, 100, 100);
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

        private Microsoft.Msagl.Drawing.Node AddNodeToCircle(Point center, Graph graph, double angle)
        {
            var nodeCenter = center + _circleRadius * (new Point(Math.Cos(angle), Math.Sin(angle)));
            Microsoft.Msagl.Drawing.Node node = CreateNodeOnCenter(graph, nodeCenter);
            return node;
        }

        private Microsoft.Msagl.Drawing.Node CreateNodeOnCenter(Graph graph, Point nodeCenter)
        {
            var node = new Microsoft.Msagl.Drawing.Node(graph.NodeCount.ToString());
            node.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Circle;
            node.Attr.Color = new Color(0, 100, 100, 100);
            node.Attr.FillColor= new Color(100, 100, 100, 100);
            var geomNode = new Node(CurveFactory.CreateCircle(_nodeWidth / 2, nodeCenter));
            node.GeometryNode = geomNode;
            geomNode.UserData = node;
            graph.AddNode(node);
            graph.GeometryGraph.Nodes.Add(geomNode);
            return node;
        }

        private static int GetNumberOfGraphsToGenerate(ArgsParser.ArgsParser _argsParser)
        {
            int ret;
            if (!_argsParser.GetIntOptionValue("-graphs", out ret))
            {
                ret = 10;
            }
            return ret;
        }

        private int GetNumberOfNodesPerCircle()
        {
            int ret;
            if (!_argsParser.GetIntOptionValue("-nodes_per_circle", out ret))
            {
                ret = 10;
            }
            return ret;
        }

        private int GetNumberOfCircles()
        {
            int ret;
            if (!_argsParser.GetIntOptionValue("-circles", out ret))
            {
                ret = 10;
            }
            return ret;
        }
    }
}
