using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Dot2Graph;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Microsoft.Msagl.Routing.Spline.Bundling;
using Microsoft.Msagl.Routing.Visibility;
using Microsoft.Msagl.UnitTests;
using TestFormForGViewer;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Node = Microsoft.Msagl.Core.Layout.Node;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test01 {
    internal class Program {
        static bool bundling;
        const string SvgFileNameOption = "-svgout";

        const string QuietOption = "-quiet";
        const string FileOption = "-file";
        const string BundlingOption = "-bundling";
        const string ListOfFilesOption = "-listoffiles";
        const string TestCdtOption = "-tcdt";
        const string TestCdtOption2 = "-tcdt2";
        const string TestCdtOption0 = "-tcdt0";
        const string TestCdtOption1 = "-tcdt1";
        const string ReverseXOption = "-rx";
        const string MdsOption = "-mds";
        const string RectRoutingOption = "-rect";
        const string SplineRoutingOption = "-spline";

        const string FdOption = "-fd";
        const string EdgeSeparationOption = "-es";
        const string RecoverSugiyamaTestOption = "-rst";
        const string InkImportanceOption = "-ink";
        const string ConstraintsTestOption = "-tcnstr";
        const string TightPaddingOption = "-tpad";
        const string LoosePaddingOption = "-lpad";
        const string CapacityCoeffOption = "-cc";
        const string PolygonDistanceTestOption = "-pd";
        const string PolygonDistanceTestOption3 = "-pd3";
        const string RandomBundlingTest = "-rbt";
        const string TestCdtThreaderOption = "-tth";
        const string AsyncLayoutOption = "-async";


        [STAThread]
        static void Main(string[] args) {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            ArgsParser.ArgsParser argsParser = SetArgsParser(args);
            if (argsParser.OptionIsUsed("-help")) {
                Console.WriteLine(argsParser.UsageString());
                Environment.Exit(0);
            }
            if (argsParser.OptionIsUsed(PolygonDistanceTestOption))
                TestPolygonDistance();
            else if (argsParser.OptionIsUsed(TestCdtThreaderOption))
                TestCdtThreader();
            else if (argsParser.OptionIsUsed(RandomBundlingTest))
                RandomBundlingTests.RsmContent();


            bundling = argsParser.OptionIsUsed(BundlingOption);

            var gviewer = new GViewer();
            gviewer.MouseMove += Draw.GviewerMouseMove;
            if (argsParser.OptionIsUsed(FdOption)) {
                TestFD();
                gviewer.CurrentLayoutMethod = LayoutMethod.IcrementalLayout;
            }
            Form form = CreateForm(null, gviewer);
            if (argsParser.OptionIsUsed(AsyncLayoutOption))
                gviewer.AsyncLayout = true;

            string listOfFilesFile = argsParser.GetStringOptionValue(ListOfFilesOption);
            if (listOfFilesFile != null) {
                ProcessListOfFiles(listOfFilesFile, argsParser);
                return;
            }
            string fileName = argsParser.GetStringOptionValue(FileOption);
            string ext = Path.GetExtension(fileName);
            if (ext != null) {
                ext = ext.ToLower();
                if (ext == ".dot") {
                    ProcessDotFile(gviewer, argsParser, fileName);
                }
                else {
                    if (ext == ".geom") {
                        GeometryGraph geometryGraph = GeometryGraphReader.CreateFromFile(fileName);
                        geometryGraph.Margins = 10;

                        FixHookPorts(geometryGraph);
                        // if (argsParser.OptionIsUsed(BundlingOption)) {
                        for (int i = 0; i < 1; i++) {
#if TEST_MSAGL
                            /*DisplayGeometryGraph.ShowGraph(geometryGraph);
                                var l = new List<DebugCurve>(); l.AddRange(geometryGraph.Nodes.Select(n=>new DebugCurve(100,1,"black",n.BoundaryCurve)));
                                l.AddRange(geometryGraph.Edges.Select(e=>new DebugCurve(100,1,"black", new LineSegment(e.Source.Center,e.Target.Center))));
                                foreach (var cl in geometryGraph.RootCluster.AllClustersDepthFirst()) {
                                    l.Add(new DebugCurve(100,2,"blue",cl.BoundaryCurve));
                                    foreach (var node in cl.Nodes)
                                        l.Add(new DebugCurve(100, 2, "brown", node.BoundaryCurve));

                                    foreach (var e in cl.Edges)
                                        l.Add(new DebugCurve(100, 2, "pink", new LineSegment(e.Source.Center, e.Target.Center)));

                                }

                                DisplayGeometryGraph.ShowDebugCurves(l.ToArray());*/

#endif
                            BundlingSettings bs = GetBundlingSettings(argsParser);

                            double loosePadding;
                            double tightPadding = GetPaddings(argsParser, out loosePadding);
                            if (argsParser.OptionIsUsed(MdsOption)) {
                                var mdsLayoutSettings = new MdsLayoutSettings { RemoveOverlaps = true, NodeSeparation = loosePadding * 3 };
                                var mdsLayout = new MdsGraphLayout(mdsLayoutSettings, geometryGraph);
                                mdsLayout.Run();
                            }
                            else {
                                if (argsParser.OptionIsUsed(FdOption)) {
                                    var settings = new FastIncrementalLayoutSettings { AvoidOverlaps = true };
                                    (new InitialLayout(geometryGraph, settings)).Run();
                                }
                            }
                            var splineRouter = new SplineRouter(geometryGraph, geometryGraph.Edges, tightPadding,
                                                                loosePadding,
                                                                Math.PI / 6, bs);
                            splineRouter.Run();
                        }
#if TEST_MSAGL
                        DisplayGeometryGraph.ShowGraph(geometryGraph);
#endif
                        return;
                    }
                    else {
                        if (ext == ".msagl") {
                            Graph graph = Graph.Read(fileName);
                            //           DisplayGeometryGraph.ShowGraph(graph.GeometryGraph);
                            if (graph != null) {
                                if (argsParser.OptionIsUsed(BundlingOption)) {
                                    BundlingSettings bs = GetBundlingSettings(argsParser);

                                    double loosePadding;
                                    double tightPadding = GetPaddings(argsParser, out loosePadding);
                                    var br = new SplineRouter(graph.GeometryGraph, tightPadding, loosePadding, Math.PI / 6,
                                                              bs);
                                    br.Run();
                                    //                 DisplayGeometryGraph.ShowGraph(graph.GeometryGraph);
                                }
                            }
                            gviewer.NeedToCalculateLayout = false;
                            gviewer.Graph = graph;
                            gviewer.NeedToCalculateLayout = true;
                        }
                    }
                }
            }
            else if (argsParser.OptionIsUsed(TestCdtOption)) {
                Triangulation(argsParser.OptionIsUsed(ReverseXOption));
                Environment.Exit(0);
            }
            else if (argsParser.OptionIsUsed(TestCdtOption0)) {
                TestTriangulationOnSmallGraph(argsParser);
                Environment.Exit(0);
            }
            else if (argsParser.OptionIsUsed(TestCdtOption2)) {
                TestTriangulationOnPolys();
                Environment.Exit(0);
            }
            else if (argsParser.OptionIsUsed(TestCdtOption1)) {
                ThreadThroughCdt();
                Environment.Exit(0);
            }
            else if (argsParser.OptionIsUsed(ConstraintsTestOption))
                TestGraphWithConstraints();
            if (!argsParser.OptionIsUsed(QuietOption))
                Application.Run(form);

        }

        static void TestFD() {
            GeometryGraph graph = CreateGeometryGraphForFD();
            //LayoutAlgorithmSettings.ShowGraph(graph);
            var settings = new FastIncrementalLayoutSettings {
                AvoidOverlaps = true,
                ApplyForces = false,
                RungeKuttaIntegration = true
            };

            var ir = new InitialLayout(graph, settings);
            ir.Run();
            RouteEdges(graph, settings);
            //LayoutAlgorithmSettings.ShowGraph(graph);
            //  AddNodeFd(graph);

            var n = new Node(CurveFactory.CreateDiamond(200, 200, new Point(350, 230)));
            var e = new Edge(n, graph.Nodes[42]);
            graph.Edges.Add(e);
            e = new Edge(n, graph.Nodes[6]);
            graph.Edges.Add(e);
            e = new Edge(n, graph.Nodes[12]);
            graph.Edges.Add(e);

            graph.Nodes.Add(n);
            graph.RootCluster.AddChild(n);
            settings.algorithm = new FastIncrementalLayout(graph, settings, settings.MaxConstraintLevel, f => settings);
            settings.Unconverge();
            settings.CreateLock(n, new Rectangle(200, 400, 500, 100));
            do {
                settings.IncrementalRun(graph);
            } while (!settings.Converged);

            RouteEdges(graph, settings);
#if TEST_MSAGL
            LayoutAlgorithmSettings.ShowGraph(graph);
#endif
            Environment.Exit(0);
        }

        static void RouteEdges(GeometryGraph graph, FastIncrementalLayoutSettings settings) {
            if (graph.Edges.Count < 1000) {
                var router = new SplineRouter(graph, settings.EdgeRoutingSettings.Padding,
                                              settings.EdgeRoutingSettings.PolylinePadding,
                                              settings.EdgeRoutingSettings.ConeAngle,
                                              settings.EdgeRoutingSettings.BundlingSettings);

                router.Run();
            }
            else {
                var sr = new StraightLineEdges(graph.Edges, 1);
                sr.Run();
            }

        }

        static GeometryGraph CreateGeometryGraphForFD() {
            var g = new GeometryGraph();

            for (int i = 0; i < 50; i++) {
                var a = new Node(CreateCurveAt(0, 0, 50));
                g.Nodes.Add(a);
                g.RootCluster.AddChild(a);
            }
            for (int i = 0; i < g.Nodes.Count; i++)
                for (int j = i + g.Nodes.Count / 2; j < g.Nodes.Count; j++)
                    g.Edges.Add(NewEdge(g, i, j));


            return g;
        }

        static Edge NewEdge(GeometryGraph g, int i, int j) {
            var e = new Edge(g.Nodes[i], g.Nodes[j]) { LineWidth = 0.01 };
            return e;
        }

        static ICurve CreateCurveAt(double x, double y, double size) {

            return CurveFactory.CreateRectangleWithRoundedCorners(size, size, size / 10, size / 10, new Point(x, y));
        }


        static void TestTriangulationOnPolys() {
            FileStream stream = File.Open("polys", FileMode.Open);
            var bformatter = new BinaryFormatter();

            var polys = (Polyline[])bformatter.Deserialize(stream);
            stream.Close();
            var cdt = new Cdt(null, polys, null);
            cdt.Run();
        }

        static void TestCdtThreader() {
            //            var rnd = new Random(1);
            //            double boxSize = 100;
            //            var obstacleSize = 10.0;
            //            var separation = 2.0;
            //            int numberOfObstacles = 5;
            //            int numberOfTestRepetions = 1000;
            //            for(int i=0;i<numberOfTestRepetions;i++) {
            //                Polyline[] obstacles = GenerateObstacles(rnd);
            //                Point a, b;
            //                GetStartEndEnd(out a, out b, rnd);
            //                Set<Polyline> obst0 = GetObstaclesByThreading();
            //                Set<Polyline> obst1 = GetObstaclesByCrossing();
            //                Debug.Assert(obst0==obst1);
            //            }
        }


        private static void ThreadThroughCdt() {
            FileStream stream = File.Open("triangles2", FileMode.Open);
            var bformatter = new BinaryFormatter();

            var trs = (CdtTriangle[])bformatter.Deserialize(stream);
            var start = (Point)bformatter.Deserialize(stream);
            var end = (Point)bformatter.Deserialize(stream);
            stream.Close();
#if TEST_MSAGL
            foreach (var t in FindStartTriangle(trs, start)) {

                var ll = ThreadOnTriangle(start, end, t);
                foreach (var cdtTriangle in trs) {
                    AddTriangleToListOfDebugCurves(ll, cdtTriangle, 50, 1, "blue");
                }
                DisplayGeometryGraph.ShowDebugCurves(ll.ToArray());
            }
#endif
        }

#if TEST_MSAGL
        static List<DebugCurve> ThreadOnTriangle(Point start, Point end, CdtTriangle t) {
            var l = new List<DebugCurve> { new DebugCurve(10, "red", new LineSegment(start, end)) };
            AddTriangleToListOfDebugCurves(l, t, 100, 3, "brown");
            var threader = new CdtThreader(t, start, end);
            foreach (var triangle in threader.Triangles()) {
                AddTriangleToListOfDebugCurves(l, triangle, 100, 3, "black");
                //                CdtSweeper.ShowFront(trs, null, new ICurve[] { new LineSegment(start, end), new Polyline(triangle.Sites.Select(s => s.Point)) { Closed = true } }, new []{new LineSegment(threader.CurrentPiercedEdge.lowerSite.Point,threader.CurrentPiercedEdge.upperSite.Point) });
            }
            return l;
        }

        static void AddTriangleToListOfDebugCurves(List<DebugCurve> debugCurves, CdtTriangle triangle, byte transparency,
                                                   double width, string color) {
            foreach (CdtEdge cdtEdge in triangle.Edges) {
                debugCurves.Add(new DebugCurve(transparency, width, color,
                                               new LineSegment(cdtEdge.upperSite.Point, cdtEdge.lowerSite.Point)));
            }
        }

        static IEnumerable<CdtTriangle> FindStartTriangle(CdtTriangle[] trs, Point p) {
            foreach (CdtTriangle t in trs) {
                PointLocation loc = CdtIntersections.PointLocationInsideTriangle(p, t);
                if (loc != PointLocation.Outside)
                    yield return t;
            }
        }
#endif

        static void TestPolygonDistance() {
            IFormatter formatter = new BinaryFormatter();
            var stream = new FileStream(@"data\polygons", FileMode.Open, FileAccess.Read, FileShare.None);
            var a = (Polygon)formatter.Deserialize(stream);
            var b = (Polygon)formatter.Deserialize(stream);
            Polygon.Distance(a, b);
        }

        static void FixHookPorts(GeometryGraph geometryGraph) {
            foreach (Edge edge in geometryGraph.Edges) {
                Node s = edge.Source;
                Node t = edge.Target;
                var sc = s as Cluster;
                if (sc != null && Ancestor(sc, t)) {
                    edge.SourcePort = new HookUpAnywhereFromInsidePort(() => s.BoundaryCurve);
                }
                else {
                    var tc = t as Cluster;
                    if (tc != null && Ancestor(tc, s)) {
                        edge.TargetPort = new HookUpAnywhereFromInsidePort(() => t.BoundaryCurve);
                    }
                }
            }
        }


        static bool Ancestor(Cluster root, Node node) {
            if (node.ClusterParent == root)
                return true;
            return node.AllClusterAncestors.Any(p => p.ClusterParent == root);
        }

        static BundlingSettings GetBundlingSettings(ArgsParser.ArgsParser argsParser) {
            if (!argsParser.OptionIsUsed(BundlingOption))
                return null;
            var bs = new BundlingSettings();
            string ink = argsParser.GetStringOptionValue(InkImportanceOption);
            double inkCoeff;
            if (ink != null && double.TryParse(ink, out inkCoeff)) {
                bs.InkImportance = inkCoeff;
                BundlingSettings.DefaultInkImportance = inkCoeff;
            }

            string esString = argsParser.GetStringOptionValue(EdgeSeparationOption);
            if (esString != null) {
                double es;
                if (double.TryParse(esString, out es)) {
                    BundlingSettings.DefaultEdgeSeparation = es;
                    bs.EdgeSeparation = es;
                }
                else {
                    System.Diagnostics.Debug.WriteLine("cannot parse {0}", esString);
                    Environment.Exit(1);
                }
            }

            string capacityCoeffString = argsParser.GetStringOptionValue(CapacityCoeffOption);
            if (capacityCoeffString != null) {
                double capacityCoeff;
                if (double.TryParse(capacityCoeffString, out capacityCoeff)) {
                    bs.CapacityOverflowCoefficient = capacityCoeff;
                }
                else {
                    System.Diagnostics.Debug.WriteLine("cannot parse {0}", capacityCoeffString);
                    Environment.Exit(1);
                }
            }


            return bs;
        }

        static void ProcessDotFile(GViewer gviewer, ArgsParser.ArgsParser argsParser, string dotFileName) {
            int line;
            int col;
            string msg;
            Graph graph = Parser.Parse(dotFileName, out line, out col, out msg);
            if (graph == null) {
                System.Diagnostics.Debug.WriteLine("{0}({1},{2}): error: {3}", dotFileName, line, col, msg);
                Environment.Exit(1);
            }
            if (argsParser.OptionIsUsed(RecoverSugiyamaTestOption)) {
                gviewer.CalculateLayout(graph);
                graph.GeometryGraph.AlgorithmData = null;
                LayeredLayout.RecoverAlgorithmData(graph.GeometryGraph);

                Node node = graph.GeometryGraph.Nodes[1];
                node.BoundaryCurve = node.BoundaryCurve.Transform(new PlaneTransformation(3, 0, 0, 0, 3, 0));

                LayeredLayout.IncrementalLayout(graph.GeometryGraph, node);
                gviewer.NeedToCalculateLayout = false;
                gviewer.Graph = graph;
                gviewer.NeedToCalculateLayout = true;
               
                return;
            }

            if (argsParser.OptionIsUsed(MdsOption))
                graph.LayoutAlgorithmSettings = gviewer.mdsLayoutSettings;
            else if (argsParser.OptionIsUsed(FdOption))
                graph.LayoutAlgorithmSettings = new FastIncrementalLayoutSettings();

            if (argsParser.OptionIsUsed(BundlingOption)) {
                graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SplineBundling;
                BundlingSettings bs = GetBundlingSettings(argsParser);
                graph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings = bs;
                string ink = argsParser.GetStringOptionValue(InkImportanceOption);
                if (ink != null) {
                    double inkCoeff;
                    if (double.TryParse(ink, out inkCoeff)) {
                        bs.InkImportance = inkCoeff;
                        BundlingSettings.DefaultInkImportance = inkCoeff;
                    }
                    else {
                        System.Diagnostics.Debug.WriteLine("cannot parse {0}", ink);
                        Environment.Exit(1);
                    }
                }

                string esString = argsParser.GetStringOptionValue(EdgeSeparationOption);
                if (esString != null) {
                    double es;
                    if (double.TryParse(esString, out es)) {
                        BundlingSettings.DefaultEdgeSeparation = es;
                        bs.EdgeSeparation = es;
                    }
                    else {
                        System.Diagnostics.Debug.WriteLine("cannot parse {0}", esString);
                        Environment.Exit(1);
                    }
                }
            }
            if (argsParser.OptionIsUsed(RectRoutingOption)) {
                graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Rectilinear;                
            }
            if (argsParser.OptionIsUsed(SplineRoutingOption)) {
                graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Spline;
            }

            gviewer.Graph = graph;
            string svgout = argsParser.GetStringOptionValue(SvgFileNameOption);
            try {
                if (svgout != null) {
                    SvgGraphWriter.Write(gviewer.Graph, svgout, null, null, 4);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        static void TestTriangulationOnSmallGraph(ArgsParser.ArgsParser parser) {
            var polyline = new Polyline(
                new Point(20.8211097717285, 40.9088821411133),
                new Point(21.4894065856934, 46.6845321655273),
                new Point(22.9755554199219, 41.3355484008789),
                new Point(20.8211097717285, 40.9088821411133));
            var polylines = new List<Polyline>();
            polylines.Add(polyline);
            var points = new List<Point>();
            var centroid = new Point(21.7620239257813, 42.9763209025065);
            points.Add(centroid);
            var testCdt = new Cdt(points, polylines, null);
            testCdt.Run();
        }

        static void ProcessListOfFiles(string listOfFilesFile, ArgsParser.ArgsParser argsParser) {
            StreamReader sr;
            try {
                sr = new StreamReader(listOfFilesFile);
            }
            catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return;
            }
            string fileName;
            string dir = Path.GetDirectoryName(listOfFilesFile);
            var gviewer = new GViewer();
            Form form = FormStuff.CreateOrAttachForm(gviewer, null);
            int nOfBugs = 0;
            while ((fileName = sr.ReadLine()) != null) {
                if (String.IsNullOrEmpty(fileName)) continue;
                fileName = Path.Combine(dir, fileName.ToLower());
                ProcessFile(fileName, argsParser, gviewer, ref nOfBugs);
                
                if (argsParser.OptionIsUsed(QuietOption) == false)
                    form.ShowDialog();
                
            }
        }

        static void ProcessFile(string fileName, ArgsParser.ArgsParser argsParser, GViewer gViewer, ref int nOfBugs) {
            System.Diagnostics.Debug.WriteLine("processing " + fileName);
            try {
                string extension = Path.GetExtension(fileName);
                if (extension == ".msagl")
                    ProcessMsaglFile(fileName, argsParser);
                else if (extension == ".dot") {
                    ProcessDotFile(gViewer, argsParser, fileName);
                }
                else if (extension == ".geom") {
                    ProcessMsaglGeomFile(fileName, argsParser);
                }
            }
            catch (Exception e) {
                nOfBugs++;
                System.Diagnostics.Debug.WriteLine("bug " + nOfBugs);
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }           
        }

        static void ProcessMsaglGeomFile(string fileName, ArgsParser.ArgsParser argsParser) {
        }


        static void ProcessMsaglFile(string fileName, ArgsParser.ArgsParser argsParser) {
            Graph graph = Graph.Read(fileName);
            if (graph == null) {
                System.Diagnostics.Debug.WriteLine("cannot read " + fileName);
                return;
            }

            if (graph.GeometryGraph != null && graph.BoundingBox.Width > 0) {
                //graph does not need a layout
                if (argsParser.OptionIsUsed(BundlingOption)) {
                    RouteBundledEdges(graph.GeometryGraph, argsParser);
                    if (!argsParser.OptionIsUsed(QuietOption)) {
                        var gviewer = new GViewer();
                        gviewer.MouseMove += Draw.GviewerMouseMove;
                        Form form = CreateForm(graph, gviewer);
                        form.ShowDialog(); // to block the thread
                    }
                }
            }
        }

        static Form CreateForm(Graph graph, GViewer gviewer) {
            Form form = FormStuff.CreateOrAttachForm(gviewer, null);
            form.SuspendLayout();
            SetEdgeSeparationBar(form);

            gviewer.GraphChanged += GviewerGraphChanged;

            if (graph != null)
                gviewer.Graph = graph;
            return form;
        }

        static void GviewerGraphChanged(object sender, EventArgs e) {
            var gviewer = (GViewer)sender;
            Graph drawingGraph = gviewer.Graph;
            if (drawingGraph != null) {
                var form = (Form)gviewer.Parent;
                CheckBox checkBox = null;
                foreach (object c in form.Controls) {
                    checkBox = c as CheckBox;
                    if (checkBox != null)
                        break;
                }
                if (bundling) {
                    drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode =
                        EdgeRoutingMode.SplineBundling;
                    SetTransparency(drawingGraph);

                    if (drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings == null)
                        drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings =
                            new BundlingSettings();
                }
            }
        }

        static TrackBar edgeSeparationTrackBar;

        static void SetEdgeSeparationBar(Form form) {
            edgeSeparationTrackBar = new TrackBar();
            form.Controls.Add(edgeSeparationTrackBar);
            edgeSeparationTrackBar.Location = new System.Drawing.Point(form.MainMenuStrip.Location.X + 400,
                                                                       form.MainMenuStrip.Location.Y);
            edgeSeparationTrackBar.Maximum = 20;
            edgeSeparationTrackBar.Value = (int)(0.5 * (edgeSeparationTrackBar.Minimum + edgeSeparationTrackBar.Maximum));
            edgeSeparationTrackBar.ValueChanged += EdgeSeparationTrackBarValueChanged;


            edgeSeparationTrackBar.BringToFront();
            form.ResumeLayout();
        }

        static void EdgeSeparationTrackBarValueChanged(object sender, EventArgs e) {
            var edgeSeparationTruckBar = (TrackBar)sender;
            GViewer gviewer = GetGviewer(edgeSeparationTruckBar);

            Graph drawingGraph = gviewer.Graph;
            if (drawingGraph == null)
                return;


            EdgeRoutingSettings edgeRoutingSettings = drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings;
            edgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SplineBundling;
            if (edgeRoutingSettings.BundlingSettings == null)
                edgeRoutingSettings.BundlingSettings = new BundlingSettings();
            edgeRoutingSettings.BundlingSettings.EdgeSeparation = GetEdgeSeparation(edgeSeparationTruckBar);
            var br = new SplineRouter(drawingGraph.GeometryGraph, 1, 1, Math.PI / 6, edgeRoutingSettings.BundlingSettings);
            br.Run();

            IViewer iv = gviewer;
            foreach (IViewerObject edge in iv.Entities) {
                if (edge is IViewerEdge)
                    iv.Invalidate(edge);
            }
        }

        static void SetTransparency(Graph drawingGraph) {
            foreach (Microsoft.Msagl.Drawing.Edge edge in drawingGraph.Edges) {
                Color color = edge.Attr.Color;
                edge.Attr.Color = new Color(100, color.R, color.G, color.B);
            }
        }

        static double GetEdgeSeparation(TrackBar edgeSeparationTruckBar) {
            double max = edgeSeparationTruckBar.Maximum;
            double min = edgeSeparationTruckBar.Minimum;
            double val = edgeSeparationTruckBar.Value;
            double alpha = (val - min) / (max - min);
            const double sepMaxMult = 2;
            const double sepMinMult = 0.1;
            const double span = sepMaxMult - sepMinMult;
            return (alpha - 0.5) * span + 0.5; //0.5 is the default edge separation
        }

        static GViewer GetGviewer(Control edgeSeparationTruckBar) {
            Control form = edgeSeparationTruckBar.Parent;
            return GetGViewerFromForm(form);
        }

        static GViewer GetGViewerFromForm(Control form) {
            GViewer gv = null;
            foreach (object g in form.Controls) {
                gv = g as GViewer;
                if (gv != null)
                    break;
            }
            return gv;
        }

        static void RouteBundledEdges(GeometryGraph geometryGraph, ArgsParser.ArgsParser argsParser) {
            double loosePadding;
            double tightPadding = GetPaddings(argsParser, out loosePadding);

            var br = new SplineRouter(geometryGraph, tightPadding, loosePadding, Math.PI / 6, new BundlingSettings());
            br.Run();
        }

        static double GetPaddings(ArgsParser.ArgsParser argsParser, out double loosePadding) {
            double tightPadding = 0.5;
            if (argsParser.OptionIsUsed(TightPaddingOption)) {
                string tightPaddingString = argsParser.GetStringOptionValue(TightPaddingOption);
                if (!double.TryParse(tightPaddingString, out tightPadding)) {
                    System.Diagnostics.Debug.WriteLine("cannot parse {0} {1}", TightPaddingOption, tightPaddingString);
                    Environment.Exit(1);
                }
            }
            loosePadding = 2.25;
            if (argsParser.OptionIsUsed(LoosePaddingOption)) {
                string loosePaddingString = argsParser.GetStringOptionValue(LoosePaddingOption);
                if (!double.TryParse(loosePaddingString, out loosePadding)) {
                    System.Diagnostics.Debug.WriteLine("cannot parse {0} {1}", LoosePaddingOption, loosePaddingString);
                    Environment.Exit(1);
                }
            }
            return tightPadding;
        }

        static ArgsParser.ArgsParser SetArgsParser(string[] args) {
            var argsParser = new ArgsParser.ArgsParser(args);
            argsParser.AddAllowedOptionWithHelpString("-help", "print the usage method");
            argsParser.AddAllowedOption(RecoverSugiyamaTestOption);
            argsParser.AddAllowedOption(QuietOption);
            argsParser.AddAllowedOption(BundlingOption);
            argsParser.AddOptionWithAfterStringWithHelp(FileOption, "the name of the input file");
            argsParser.AddOptionWithAfterStringWithHelp(SvgFileNameOption, "the name of the svg output file");
            argsParser.AddOptionWithAfterStringWithHelp(ListOfFilesOption,
                                                  "the name of the file containing a list of files");
            argsParser.AddAllowedOptionWithHelpString(TestCdtOption, "testing Constrained Delaunay Triangulation");
            argsParser.AddAllowedOptionWithHelpString(TestCdtOption0,
                                                      "testing Constrained Delaunay Triangulation on a small graph");
            argsParser.AddAllowedOptionWithHelpString(TestCdtOption1, "testing threading through a CDT");
            argsParser.AddAllowedOptionWithHelpString(TestCdtOption2,
                                                      "testing Constrained Delaunay Triangulation on file \'polys\'");
            argsParser.AddAllowedOptionWithHelpString(ReverseXOption, "reversing X coordinate");
            argsParser.AddOptionWithAfterStringWithHelp(EdgeSeparationOption, "use specified edge separation");
            argsParser.AddAllowedOptionWithHelpString(MdsOption, "use mds layout");
            argsParser.AddAllowedOptionWithHelpString(RectRoutingOption, "use rect layout");
            argsParser.AddAllowedOptionWithHelpString(SplineRoutingOption, "use spline layout");
            argsParser.AddAllowedOptionWithHelpString(FdOption, "use force directed layout");
            argsParser.AddAllowedOptionWithHelpString(ConstraintsTestOption, "test constraints");
            argsParser.AddOptionWithAfterStringWithHelp(InkImportanceOption, "ink importance coefficient");
            argsParser.AddOptionWithAfterStringWithHelp(TightPaddingOption, "tight padding coefficient");
            argsParser.AddOptionWithAfterStringWithHelp(LoosePaddingOption, "loose padding coefficient");
            argsParser.AddOptionWithAfterStringWithHelp(CapacityCoeffOption, "capacity coeffiecient");
            argsParser.AddAllowedOptionWithHelpString(PolygonDistanceTestOption, "test Polygon.Distance");
            argsParser.AddAllowedOptionWithHelpString(PolygonDistanceTestOption3, "test PolygonDistance3");
            argsParser.AddAllowedOptionWithHelpString(RandomBundlingTest, "random bundling test");
            argsParser.AddAllowedOptionWithHelpString(TestCdtThreaderOption, "test CdtThreader");
            argsParser.AddAllowedOptionWithHelpString(AsyncLayoutOption, "test viewer in the async mode");

            if (!argsParser.Parse()) {
Console.WriteLine(argsParser.ErrorMessage);
                System.Diagnostics.Debug.WriteLine(argsParser.UsageString());
                Environment.Exit(1);
            }
            return argsParser;
        }

        static void Triangulation(bool reverseX) {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            int r = reverseX ? -1 : 1;
            IEnumerable<Point> points = Points().Select(p => new Point(r * p.X, p.Y));

            var poly = (Polyline)RussiaPolyline.GetTestPolyline().ScaleFromOrigin(1, 1);
            var cdt = new Cdt(null, new[] { poly }, null);
            cdt.Run();
#if TEST_MSAGL
            CdtSweeper.ShowFront(cdt.GetTriangles(), null, null, null);
#endif
        }

        static IEnumerable<Point> Points() {
            foreach (var segment in Segments()) {
                yield return segment.Item1;
                yield return segment.Item2;
            }
            yield return new Point(157, 198);
        }

        static IEnumerable<Tuple<Point, Point>> Segments() {
            yield return new Tuple<Point, Point>(new Point(181, 186), new Point(242, 73));
            yield return new Tuple<Point, Point>(new Point(236, 122), new Point(268, 202));
            yield return new Tuple<Point, Point>(new Point(274, 167), new Point(343, 76));
            yield return new Tuple<Point, Point>(new Point(352, 131), new Point(361, 201));
            yield return new Tuple<Point, Point>(new Point(200, 209), new Point(323, 237));
            yield return new Tuple<Point, Point>(new Point(372, 253), new Point(451, 185));
            yield return new Tuple<Point, Point>(new Point(448, 133), new Point(517, 272));
            yield return new Tuple<Point, Point>(new Point(339, 327), new Point(327, 145));
            yield return new Tuple<Point, Point>(new Point(185, 220), new Point(207, 172));
            yield return new Tuple<Point, Point>(new Point(61, 226), new Point(257, 253));
            yield return new Tuple<Point, Point>(new Point(515, 228), new Point(666, 258));
        }

        static ICurve CreateEllipse() {
            return CurveFactory.CreateEllipse(20, 10, new Point());
        }

        static void TestGraphWithConstraints() {
            var graph = new GeometryGraph();

            var closed = new Node(CreateEllipse(), "closed");
            var line = new Node(CreateEllipse(), "line");
            var bezier = new Node(CreateEllipse(), "bezier");
            var arc = new Node(CreateEllipse(), "arc");
            var rectangle = new Node(CreateEllipse(), "rectangle");
            var ellipse = new Node(CreateEllipse(), "ellipse");
            var polygon = new Node(CreateEllipse(), "polygon");
            var shapes = new Node(CreateEllipse(), "shapes");
            var open = new Node(CreateEllipse(), "open");
            graph.Nodes.Add(closed);
            graph.Nodes.Add(line);
            graph.Nodes.Add(bezier);
            graph.Nodes.Add(arc);
            graph.Nodes.Add(rectangle);
            graph.Nodes.Add(ellipse);
            graph.Nodes.Add(polygon);
            graph.Nodes.Add(shapes);
            graph.Nodes.Add(open);

            var so = new Edge(shapes, open);
            var sc = new Edge(shapes, closed);
            var ol = new Edge(open, line);
            var ob = new Edge(open, bezier);
            var oa = new Edge(open, arc);
            var cr = new Edge(closed, rectangle);
            var ce = new Edge(closed, ellipse);
            var cp = new Edge(closed, polygon);
            graph.Edges.Add(so);
            graph.Edges.Add(sc);
            graph.Edges.Add(ol);
            graph.Edges.Add(ob);
            graph.Edges.Add(oa);
            graph.Edges.Add(cr);
            graph.Edges.Add(ce);
            graph.Edges.Add(cp);

            var settings = new SugiyamaLayoutSettings();
            settings.AddUpDownVerticalConstraint(closed, ellipse);
            settings.AddUpDownVerticalConstraint(open, bezier);
            settings.AddUpDownConstraint(closed, open);
            settings.AddSameLayerNeighbors(polygon, open);
            settings.AddLeftRightConstraint(closed, open);
            settings.AddLeftRightConstraint(open, closed);

            ////To verify 444585, just turn on this following commented line
            settings.AddLeftRightConstraint(ellipse, rectangle);
            settings.AddLeftRightConstraint(ellipse, bezier);

            var layeredLayout = new LayeredLayout(graph, settings);
            layeredLayout.Run();
#if TEST_MSAGL
            DisplayGeometryGraph.ShowGraph(graph);
#endif
        }
    }
}
