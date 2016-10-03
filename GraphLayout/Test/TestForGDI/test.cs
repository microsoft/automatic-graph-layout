using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using DebugCurveViewer;
using Dot2Graph;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.ProjectionSolver;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Prototype.LayoutEditing;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using Microsoft.Msagl.Routing.Spline.Bundling;
using TestFormForGViewer;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Label = Microsoft.Msagl.Core.Layout.Label;
using Node = Microsoft.Msagl.Core.Layout.Node;
using Path = System.IO.Path;
using Shape = Microsoft.Msagl.Routing.Shape;
using Size = System.Drawing.Size;
#if REPORTING
using Timer = Microsoft.Msagl.DebugHelpers.Timer;
#endif
//using Timer = Microsoft.Msagl.Timer;

namespace TestForGdi {
    internal static class Test {
        static bool mds;
        internal static GViewer gViewer;
        internal static bool Verbose;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            var listFileNames = new List<string>();
            var geomFileNames = new List<string>();
            var dotFileSpecs = new List<string>();
            bool show = true;
            EdgeRoutingMode edgeRoutingMode = EdgeRoutingMode.SugiyamaSplines;
            bool useSparseVisibilityGraph = false;
            bool useObstacleRectangles = false;
#if DEBUG
            DisplayGeometryGraph.SetShowFunctions();
#endif
            const string badEdgeOption = "-edge";
            const string mdsOption = "-mds";
            const string initialLayoutOption = "-initl";

            const string phyloOption = "-phylo";
            bool phylo = false;

            const string multiedges = "-multi";
            bool multiedgesTest = false;

            const string testSaveOption = "-save";
            bool testSave = false;


            const string testConvexHullString = "-convexHull";
            bool testConvexHull = false;
            bool bundling = false;

            double bendPenalty = SsstRectilinearPath.DefaultBendPenaltyAsAPercentageOfDistance;

            int fileReps = 1;
            int randomShifts = 0;
            bool showForm = true;
            try {
                for (int iarg = 0; iarg < args.Length; ++iarg) {
                    string s = args[iarg].ToLower();

                    if ("converttogeom" == s) {
                        if (iarg > (args.Length - 3))
                            throw new ApplicationException(
                                "{0} option requires input .dot filename and output .geom filename");
                        ConvertDotToGeom(args[iarg + 1], args[iarg + 2]);
                        return;
                    }

                    if (s == "-devTrace") {
#if DEVTRACE
                        ++iarg;
                        if (iarg >= args.Length) {      // require one value
                            throw new ApplicationException("Missing filename for -" + s);
                        }

#if MSAGL_INTERNALS_NOT_VISIBLE
                        // Copied the one line from this method here because of access issues, signing, etc.
                        // with InternalsVisibleTo.
                        //DevTrace.AddListenerToFile(args[iarg]);
#else
                        // Use File.Create to overwrite any existing file.
                        Trace.Listeners.Add(new TextWriterTraceListener(File.Create(args[iarg])));
#endif

#else
                        Console.WriteLine("-devtrace requires the DEVTRACE build configuration");
                        return;
#endif
                    }

                    // Start with some specific tests that we'll just pass arguments to.
                    // These are not "-" prefixed.
                    if ("testbinaryqueue" == s) {
                        GenericBinaryHeapPriorityQueue<int>.Test();
                        showForm = false;
                    } else {
                        switch (s) {
                            case "deb11":
                                TestDeb11();
                                return;
                            case "treewithconstraints":
                                TreeWithConstraints();
                                return;
                            
                            case "testgrouprouting":
                                TestGroupRouting();
                                return;
                            case "-vdc":
                                if (iarg == args.Length - 1) {
                                    Console.WriteLine("argument is missing after -vdc");
                                    return;
                                }
#if DEBUG
                                ShowDebugCurves(args[iarg + 1]);
#endif
                                return;
                            case "-testportentry":
                                TestPortEntry();
                                return;
                            case "-geom": // must be before case "g":
                                geomFileNames.Add(args[iarg + 1]);
                                ++iarg;
                                showForm = false;
                                break;
                            case "-g": {
                                var edgeRoutingSettings = new EdgeRoutingSettings {
                                                                                      EdgeRoutingMode = edgeRoutingMode,
                                                                                      BundlingSettings =
                                                                                          bundling
                                                                                              ? new BundlingSettings()
                                                                                              : null,
                                                                                      UseObstacleRectangles =
                                                                                          useObstacleRectangles,
                                                                                      BendPenalty = bendPenalty
                                                                                  };

                                LoadGeomFiles(args.Skip(iarg + 1), edgeRoutingSettings, show);
                                return;
                            }
                            case "-drawinggraphs": {
                                var edgeRoutingSettings = new EdgeRoutingSettings {
                                                                                      EdgeRoutingMode = edgeRoutingMode,
                                                                                      BundlingSettings =
                                                                                          bundling
                                                                                              ? new BundlingSettings()
                                                                                              : null
                                                                                  };

                                LoadDrawingGraphs(args.Skip(iarg + 1), edgeRoutingSettings, bendPenalty);
                                return;
                            }
                            case "-gtest":
                                LoadGeomFilesTest(args.Skip(iarg + 1), edgeRoutingMode, useSparseVisibilityGraph,
                                                  useObstacleRectangles,
                                                  bendPenalty, 0.52359877559829882);
                                return;
                            case "-grouptest":
                                GroupRoutingTest();
                                return;
                            case "-groupbundling":
                                return;
                            case "-wbundling":
                                BundleWithWidths();
                                return;

                            case "-grouptestrect":
                                GroupRoutingTestRect();
                                return;
                            case "-grouptestspline":
                                GroupRoutingTestSpline();
                                return;
                            case "-geomtestcone":
                                LoadGeomFilesTest(args.Skip(iarg + 1), edgeRoutingMode, useSparseVisibilityGraph,
                                                  useObstacleRectangles,
                                                  bendPenalty, Math.PI/6);
                                return;
                            case "layerseparationwithtransform":
                                LayerSeparationWithTransform();
                                return;
                            case "testwaypoints":
                                TestWayPoints();
                                break;
                            case "-bundling":
                                bundling = true;
                                edgeRoutingMode = EdgeRoutingMode.SplineBundling;
                                break;
                            case "testsolver":
                                TestSolver();
                                showForm = false;
                                break;
                            case "testrouting":
                                TestRouting();
                                showForm = false;
                                break;
                            case "-rectsp":
                                edgeRoutingMode = EdgeRoutingMode.Rectilinear;
                                Console.WriteLine("setting rectsp");
                                break;
                            case "-sparsevg":
                                useSparseVisibilityGraph = true;
                                Console.WriteLine("setting sparseVg");
                                break;
                            case "-userect":
                                useObstacleRectangles = true;
                                Console.WriteLine("setting useRect");
                                break;
                            case "-bendpenalty":
                                bendPenalty = double.Parse(args[iarg + 1]);
                                Console.WriteLine("setting bendPenalty");
                                ++iarg;
                                break;
                            case "freespline":
                                edgeRoutingMode = EdgeRoutingMode.Spline;
                                Console.WriteLine("setting EdgeRoutingMode.Spline");
                                break;
                            case "-rectcenter":
                                edgeRoutingMode = EdgeRoutingMode.RectilinearToCenter;
                                Console.WriteLine("setting rectToCenter");
                                break;
                                //                            case "testspanner":
                                //#if TEST_MSAGL
                                //                                ConeSpannerTest.TestSpanner();
                                //#else
                                //                                Console.WriteLine("ConeSpannerTest is only available in TEST mode");
                                //#endif
                            default:
                                if (s.StartsWith(badEdgeOption)) {} else if (s.StartsWith(mdsOption)) {
                                    mds = true;
                                } else if (s.StartsWith(initialLayoutOption)) {
                                    FormStuff.initialLayout = true;
                                } else if (s.StartsWith("-bundling")) {
                                    bundling = true;
                                } else if (s.StartsWith(testConvexHullString)) {
                                    testConvexHull = true;
                                } else if (s == "-filereps")
                                    // This is probably used with -quiet.  Must come before StartsWith("-f") option.
                                    fileReps = Int32.Parse(args[++iarg]);
                                else if (s == "-randomshifts")
                                    randomShifts = Int32.Parse(args[++iarg]);
                                else if (s.StartsWith("-f")) {
                                    listFileNames.Add(GetFileSpec(args, ref iarg));
                                    showForm = false;
                                }  else if ((s == "-noshow") || (s == "-quiet")) {
                                    show = false;
                                } else if (s == "-verbose") {
                                    Verbose = true;
                                } else if (s.StartsWith("-log:")) {
                                    // Microsoft.Msagl.Layout.LogFileName = s.Substring(5);
                                    Verbose = true;
                                } else if (s == multiedges) {
                                    multiedgesTest = true;
                                } else if (s == phyloOption) {
                                    phylo = true;
                                } else if (s == testSaveOption)
                                    testSave = true;
                                else if (s.StartsWith("-p")) {
                                    dotFileSpecs.Add(GetFileSpec(args, ref iarg));
                                    showForm = false;
                                } else Console.WriteLine("unknown option " + s);
                                break;
                        }
                    }
                }

                var sw = new Stopwatch();
                sw.Start();

                if (testConvexHull)
                    TestConvexHull();

                if (testSave)
                    TestSave();


                if (phylo)
                    TestPhylo();

                // if (useBrandes)
                // Layout.BrandesThreshold = 0;

                // if (verbose)
                // Microsoft.Msagl.Layout.Reporting = true;

                if (multiedgesTest)
                    TestMultiedges();

                // If both were specified, do listfiles first, then filespecs.
                foreach (string listFileName in listFileNames) {
                    ProcessFileList(listFileName, fileReps, show, mds, edgeRoutingMode, bendPenalty, bundling,
                                    randomShifts,
                                    useSparseVisibilityGraph, useObstacleRectangles);
                }
                foreach (string fileSpec in dotFileSpecs) {
                    ProcessFileSpec(fileSpec, fileReps, show, mds, edgeRoutingMode, bendPenalty, bundling, randomShifts,
                                    useSparseVisibilityGraph, useObstacleRectangles);
                }
                foreach (string geomFileName in geomFileNames) {
                    var edgeRoutingSettings = new EdgeRoutingSettings {
                                                                          EdgeRoutingMode = edgeRoutingMode,
                                                                          BundlingSettings =
                                                                              bundling ? new BundlingSettings() : null,
                                                                          UseObstacleRectangles = useObstacleRectangles,
                                                                          BendPenalty = bendPenalty
                                                                      };
                    LoadGeomFile(geomFileName, edgeRoutingSettings, show);
                }

                sw.Stop();
                var ts = sw.Elapsed;
                Console.WriteLine("  Elapsed time: {0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds,
                                  ts.Milliseconds);

                if (showForm) {
                    gViewer = new GViewer();
                    gViewer.MouseMove += Draw.GviewerMouseMove;
                    var form = FormStuff.CreateForm(gViewer);
                    Application.Run(form);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            return;
        }


        static void FillPoints(List<Point> ps, Random r, int size) {
            for(int i=0;i<size;i++)
                ps.Add(new Point(r.NextDouble(), r.NextDouble()));
        }

        static void LoadDrawingGraphs(IEnumerable<string> fileNames, EdgeRoutingSettings edgeRoutingSettings, double bendPenalty) {
            foreach (var fileName in fileNames) {
                LoadDrawingGraph(fileName, edgeRoutingSettings, bendPenalty);
            }
        }

        static void LoadDrawingGraph(string fileName, EdgeRoutingSettings edgeRoutingSettings, double bendPenalty) {
            var graph = Graph.Read(fileName);
            if (edgeRoutingSettings.EdgeRoutingMode == EdgeRoutingMode.Rectilinear || edgeRoutingSettings.EdgeRoutingMode == EdgeRoutingMode.RectilinearToCenter) {
                var sugiyamaSettings=new SugiyamaLayoutSettings();
                RouteRectEdgesOfGeomGraph(edgeRoutingSettings.EdgeRoutingMode, true ,
                                        edgeRoutingSettings.UseObstacleRectangles, bendPenalty, graph.GeometryGraph, sugiyamaSettings);
            } else {
                const double angle = 30 * Math.PI / 180;
                var router = new SplineRouter(graph.GeometryGraph, edgeRoutingSettings.Padding, edgeRoutingSettings.PolylinePadding, angle, edgeRoutingSettings.BundlingSettings);
                router.Run();

                TestPadding(graph.GeometryGraph);
#if DEBUG
                var gv = new GViewer();
                var f = new Form {
                    StartPosition = FormStartPosition.CenterScreen,
                    //     f.layerSeparationMult.Value = (decimal)(gwgraph.GraphAttr.LayerSep / gwgraph.GraphAttr.MinLayerSep);

                    Size = new Size(Screen.PrimaryScreen.WorkingArea.Width,
                                      Screen.PrimaryScreen.WorkingArea.Height)
                };
                f.SuspendLayout();
                f.Controls.Add(gv);
                gv.Dock = DockStyle.Fill;
                gv.NeedToCalculateLayout = false;
                gv.Graph = graph;
                f.ResumeLayout();

                f.ShowDialog();
#endif
            }
        }

        static void TestDeb11() {
            GeometryGraph graph = GeometryGraphReader.CreateFromFile("c:\\tmp\\DebugGraph11.msagl.geom");

            var allEdges = new List<Edge>(graph.Edges);
            int start = 64;
            int end = 64;
            for (int i = 0; i < allEdges.Count; i++)
                if (i < start || i > end)
                    graph.Edges.Remove(allEdges[i]);

            var splineRouter = new SplineRouter(graph, 1, 20, Math.PI / 6, null);
            splineRouter.Run();
#if TEST_MSAGL
            LayoutAlgorithmSettings.ShowGraph(graph);
#endif
        }

        static void BundleWithWidths() {
            bool msaglFile;
            int line;
            int column;
            var graph = Form2.CreateDrawingGraphFromFile("c:\\dev\\graphlayout\\graphs\\tail.dot", out line, out column, out msaglFile);
            var graph1 = Form2.CreateDrawingGraphFromFile("c:\\dev\\graphlayout\\graphs\\tail.dot", out line, out column, out msaglFile);

            double w = 0.3;
            foreach (var edge in graph.Edges) {
                edge.Attr.LineWidth = w;
                if (w == 0.3)
                    w = 0.4;
                else if (w == 0.4)
                    w = .5;
                else w = 0.3;
            }

            var gv = new GViewer();
            gv.Graph = graph1;
            graph.CreateGeometryGraph();
            var gg = graph.GeometryGraph;
            var gg1 = graph1.GeometryGraph;

            for (int i = 0; i < gg.Nodes.Count; i++)
                gg.Nodes[i].BoundaryCurve = gg1.Nodes[i].BoundaryCurve;

            var ss = new SugiyamaLayoutSettings();
            var ll = new LayeredLayout(graph.GeometryGraph, ss);
            ll.Run();

            var bundler = new SplineRouter(graph.GeometryGraph, ss.NodeSeparation * 2, ss.NodeSeparation / 3, Math.PI / 6, ss.EdgeRoutingSettings.BundlingSettings);

            bundler.Run();
            
            var f = new Form();
            f.SuspendLayout();
            f.Controls.Add(gv);
            gv.Dock = DockStyle.Fill;
            gv.NeedToCalculateLayout = false;
            gv.Graph = graph;
            f.ResumeLayout();

            f.ShowDialog();


        }

        static void TestConstraints() {
            var gg = new GeometryGraph();

            var node1 = new Node {GeometryParent = gg};
            node1.BoundaryCurve =CurveFactory.CreateEllipse(20.0,10.0,new Point(0.0, 0.0));
            gg.Nodes.Add(node1);

            var node2 = new Node {GeometryParent = gg};
            node2.BoundaryCurve =
                CurveFactory.CreateEllipse(20.0,10.0,new Point(0.0, 0.0));
            gg.Nodes.Add(node2);

            var edge = new Edge(node1,node2) {GeometryParent = gg};
            gg.Edges.Add(edge);
            edge.Label = new Label {GeometryParent = edge,
                //need to set the label dimensions, otherwise the assertion is thrown
                Width = 10, Height = 10
            };
            //node1.AddOutEdge(edge); //the edges have been already added to the nodes in gg.Edges.Add(edge)
            //node2.AddInEdge(edge);

            var settings = new SugiyamaLayoutSettings();
            settings.AddSameLayerNeighbors(node1, node2);
            var layoutAlgorithm = new
                LayeredLayout(gg, settings);
            layoutAlgorithm.Run();
#if TEST_MSAGL
            LayoutAlgorithmSettings.ShowGraph(gg);
#endif
            Environment.Exit(0);
        }


        static void TestSpeedOfIterations() {
            int n = 1000;
            var g = new GeometryGraph();
            var nl = new Node[1000];
            for (int i = 0; i < n; i++) {
                Node node = nl[i] = new Node();
                g.Nodes.Add(node);
            }
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    g.Edges.Add(new Edge(nl[i], nl[j]));

            var watch = new Stopwatch();
            watch.Start();
            foreach (Edge e in g.Edges)
                foo(e);
            watch.Stop();
            long t0 = watch.ElapsedMilliseconds;
            Console.WriteLine("enumerating of the node list: {0}ms", t0);

            watch.Start();
            foreach (Edge e in EnumerateThroughNodes(g))
                foo(e);
            watch.Stop();
            long t1 = watch.ElapsedMilliseconds;
            Console.WriteLine("enumerating through the edges adjacent to nodes: {0} ms", t1);
            Console.WriteLine("ratio is {0}", ((double) t1)/t0);


            var set = new Set<Edge>(g.Edges);
            watch.Start();
            foreach (Edge e in set)
                foo(e);
            watch.Stop();
            long t2 = watch.ElapsedMilliseconds;
            Console.WriteLine("enumerating the Set: {0}ms", t2);
            Console.WriteLine("ratio is {0}", ((double) t2)/t0);


            Environment.Exit(0);
        }

        static void foo(Edge p0) {
            p0.Weight *= 2;
            p0.LineWidth *= 2;
            p0.Weight *= 2;
            p0.LineWidth *= 2;
            p0.Weight *= 2;
            p0.LineWidth *= 2;
            p0.Weight *= 2;
            if (p0.EdgeGeometry != null)
                p0.EdgeGeometry.LineWidth /= 2;
            p0.LineWidth *= 2;
        }

        static IEnumerable<Edge> EnumerateThroughNodes(GeometryGraph g) {
            foreach (Node node in g.Nodes) {
                foreach (Edge outEdge in node.OutEdges) {
                    yield return outEdge;
                }
                foreach (Edge selfEdge in node.SelfEdges) {
                    yield return selfEdge;
                }
            }
        }

        static void LayerSeparationWithTransform() {
            GeometryGraph graph = GeometryGraphReader.CreateFromFile("c:/tmp/wrongLayout.msagl.geom");
            var settings = new SugiyamaLayoutSettings();
            settings.Transformation = PlaneTransformation.Rotation(-Math.PI/2);
            var layeredLayout = new LayeredLayout(graph, settings);
            layeredLayout.Run();
            //   LayoutHelpers.CalculateLayout(graph, settings);
            GeometryGraphWriter.Write(graph, "c:\\tmp\\correctLayout");
#if DEBUG
            LayoutAlgorithmSettings.ShowGraph(graph);
#endif
        }

#if DEBUG

        static void ShowDebugCurves(string fileName) {
            DisplayGeometryGraph.ShowDebugCurvesEnumerationOnForm(GetDebugCurves(fileName), new Form1());
        }

        static IEnumerable<DebugCurve> GetDebugCurves(string fileName) {
            IFormatter formatter = new BinaryFormatter();
            Stream file = null;

            try {
                file = File.Open(fileName, FileMode.Open);
                var debugCurveCollection = formatter.Deserialize(file) as DebugCurveCollection;
                if (null == debugCurveCollection) {
                    Console.WriteLine("cannot read debugcurves from " + fileName);
                    return null;
                }
                return debugCurveCollection.DebugCurvesArray;
            }
            catch (FileNotFoundException ex) {
                Console.WriteLine();
                Console.WriteLine(ex.Message);
            }
            catch (Exception) {
                throw;
            }
            finally {
                if (null != file) {
                    file.Close();
                }
            }
            return null;
        }
#endif

        static void GroupRoutingTestSpline() {
            LayoutAlgorithmSettings settings;
            var graph = GetTestGraphWithClusters(out settings);
#if DEBUG
            //DisplayGeometryGraph.ShowGraph(graph);
#endif
            var router = new SplineRouter(graph, 2, 9, Math.PI / 6, new BundlingSettings());
            router.Run();
#if DEBUG
            DisplayGeometryGraph.ShowGraph(graph);
#endif
        }

//        static void FixClusterBoundaries(Cluster cluster, double i) {
//            foreach (Cluster cl in cluster.Clusters)
//                FixClusterBoundaries(cl, i);
//
//
//            cluster.BoundaryCurve = new Polyline(
//                ConvexHull.CalculateConvexHull(
//                    cluster.Clusters.SelectMany(c => ClusterPoints(c)).Concat(
//                        cluster.Nodes.SelectMany(n => NodePerimeterPoints(n))))) {Closed = true};
//            cluster.RectangularBoundary=new RectangularClusterBoundary();
//        }

        static void FixClusterBoundariesWithNoRectBoundaries(Cluster cluster, double padding)
        {
            foreach (Cluster cl in cluster.Clusters)
                FixClusterBoundariesWithNoRectBoundaries(cl, padding);

            var box = Rectangle.CreateAnEmptyBox();

            var clusterPoints =
                    cluster.Clusters.SelectMany(c => ClusterPoints(c)).Concat(
                        cluster.Nodes.SelectMany(n => NodePerimeterPoints(n)));
            foreach (var clusterPoint in clusterPoints)
                box.Add(clusterPoint);

            box.Pad(padding);
            cluster.BoundaryCurve=box.Perimeter();
            cluster.RectangularBoundary = new RectangularClusterBoundary();
        }

        static IEnumerable<Point> ClusterPoints(Cluster cluster) {
            return cluster.BoundaryCurve as Polyline;
        }

        static IEnumerable<Point> NodePerimeterPoints(Node node) {
            return new[] {
                node.BoundingBox.RightTop, node.BoundingBox.LeftBottom, node.BoundingBox.LeftTop,
                node.BoundingBox.RightBottom
            };
        }


        static void TestGroupRouting() {
            GeometryGraph graph = GeometryGraphReader.CreateFromFile("c:/tmp/bug.msagl.geom");
            var router = new SplineRouter(graph, 10, 5, Math.PI/6);
            router.Run();
#if DEBUG
            DisplayGeometryGraph.ShowGraph(graph);
#endif
        }


        
                public static void TreeWithConstraints() {
            var graph = new GeometryGraph();

            var closed = new Node(CreateEllipse(), "clos");
            var line = new Node(CreateEllipse(), "line");
            var bezier = new Node(CreateEllipse(), "bezi");
            var arc = new Node(CreateEllipse(), "arc");
            var rectangle = new Node(CreateEllipse(), "rect");
            var ellipse = new Node(CreateEllipse(), "elli");
            var polygon = new Node(CreateEllipse(), "poly");
            var shapes = new Node(CreateEllipse(), "shap");
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

            settings.AddUpDownConstraint(closed, open);
            //settings.AddSameLayerNeighbors(polygon, open);
            settings.AddLeftRightConstraint(closed, open);

            //To verify 444585, just turn on this following commented line
            settings.AddLeftRightConstraint(ellipse, rectangle);
            settings.AddLeftRightConstraint(ellipse, bezier);

            var layeredLayout = new LayeredLayout(graph, settings);
            layeredLayout.Run();
#if DEBUG
            LayoutAlgorithmSettings.ShowGraph(graph);
#endif

            Debug.Assert(Math.Abs(closed.Center.X - ellipse.Center.X) < 0.01);
            Debug.Assert(Math.Abs(open.Center.X - bezier.Center.X) < 0.01);

            foreach (Node n0 in graph.Nodes) {
                foreach (Node n1 in graph.Nodes) {
                    if (n0 == n1) {
                        continue;
                    }
                    Debug.Assert(!n0.BoundingBox.Intersects(n1.BoundingBox));
                }
            }

            ValidateUpDownVerticalConstraint(closed, ellipse);
            ValidateUpDownVerticalConstraint(open, bezier);
            ValidateUpDownConstraint(closed, open);
            ValidateNeighborConstraint(graph, polygon, open);
            ValidateLeftRightConstraint(closed, open);

            //To verify 444585, also turn on this following commented line
            ValidateLeftRightConstraint(ellipse, rectangle);
            ValidateLeftRightConstraint(ellipse, bezier);
        }

        static ICurve CreateEllipse() {
            return new Ellipse(10, 10, new Point());
        }

        static void ValidateLeftRightConstraint(Node left, Node open) {
            Debug.Assert(left.Center.X < open.Center.X);
        }

        static void ValidateNeighborConstraint(GeometryGraph graph, Node a, Node b) {
            ValidateLeftRightConstraint(a, b);
            IEnumerable<Node> sameLayerNodes =
                graph.Nodes.Where(n => n != a && n != b && ApproximateComparer.Close(n.Center.Y, a.Center.Y));
            Debug.Assert(sameLayerNodes.Any(n => n.Center.X < a.Center.X || n.Center.X > b.Center.X));
        }

        static void ValidateUpDownConstraint(Node a, Node b) {
            Debug.Assert(a.Center.Y > b.Center.Y);
        }

        static void ValidateUpDownVerticalConstraint(Node a, Node b) {
            Debug.Assert(ApproximateComparer.Close(a.Center.X, b.Center.X));
        }

        static void BundleRouterTest() {
            var graph = CreateGraphForGroupRouting();

            var router = new SplineRouter(graph, 1, 1, Math.PI / 180 * 30, new BundlingSettings());
            router.Run();
#if DEBUG

            //int j = 1;
            List<DebugCurve> edges =
                graph.Edges.Select(edgeGeometry => new DebugCurve(200, 0.5, "black" /*Nudger.Colors[j++]*/, edgeGeometry.Curve))
                    .ToList();
            var boundaries = graph.RootCluster.AllClustersDepthFirst().Select(
                    s => new DebugCurve(s.BoundaryCurve, s.UserData)).Concat(graph.RootCluster.AllClustersDepthFirst().SelectMany(c=>c.Nodes.Select(n=>new DebugCurve(n.BoundaryCurve))));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(
                boundaries.Concat(edges));
#endif

        }

        static void GroupRoutingTest() {
            const int count = 1;
            var sw = new Stopwatch();
            sw.Start();
            //RouteGroupGraph(count);
            RouteCustomEdges(count);
            sw.Stop();
            Console.WriteLine((double) sw.ElapsedMilliseconds/1000);
            return;
#if false // turns off "unreachable code" warning due to the foregoing "return;".

            RouteBus1138(count);

            GeometryGraph graph = GeometryGraphReader.CreateFromFile("c:\\tmp\\graph0.msagl.geom");
            for (int i = 0; i < count; i++)
            {
                var box = graph.BoundingBox;
                box.Pad(box.Diagonal/4);
                graph.BoundingBox = box;
                var router = new SplineRouter(graph, 10, 1, Math.PI/6);
                router.Run();
            }
#if DEBUG
            LayoutAlgorithmSettings.ShowGraph(graph);
#endif
            graph = GeometryGraphReader.CreateFromFile("c:\\tmp\\graph1.msagl.geom");
            var splineRouter = new SplineRouter(graph, 10, 1, Math.PI / 6);
            splineRouter.Run();
#if DEBUG
            LayoutAlgorithmSettings.ShowGraph(graph);
#endif
            //RoutingTest0();
            for (int i = 0; i < count; i++)
            {
                graph = GeometryGraphReader.CreateFromFile("c:\\tmp\\graph2.msagl.geom");
                var router = new SplineRouter(graph, 10, 1, Math.PI / 6);
                router.Run();
                var box = graph.BoundingBox;
                box.Pad(box.Diagonal/4);
                graph.BoundingBox = box;
            }
#if DEBUG
            LayoutAlgorithmSettings.ShowGraph(graph);
#endif
#endif
        }
        static void RouteCustomEdges(int count) {
            for (int i = 0; i < count; i++) {
                var graph = CreateGraphForGroupRouting();
#if DEBUG
                LayoutAlgorithmSettings.ShowGraph(graph);
#endif
                var router = new SplineRouter(graph, 3, 3,Math.PI/180*30);
                router.Run();
#if DEBUG

                int j = 0;
                List<DebugCurve> edges =
                    graph.Edges.Select(edge => new DebugCurve(200, 2, DebugCurve.Colors[j++], edge.Curve))
                        .ToList();
                LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(graph.RootCluster.AllClustersDepthFirst().Select(
                    s => new DebugCurve(s.BoundaryCurve, s.UserData)).
                                                                       Concat(edges));
#endif
            }
        }

        static Shape FindShape(IEnumerable<Shape> nodeShapes, string id) {
            return nodeShapes.First(sh => (string) sh.UserData == id);
        }

        static ICurve CurveOnNodes(GeometryGraph graph, double pad, params string[] nodeIds) {
            Rectangle rect = Rectangle.CreateAnEmptyBox();
            foreach (string nodeId in nodeIds) {
                rect.Add(graph.FindNodeByUserData(nodeId).BoundingBox);
            }
            rect.Pad(pad);
            return rect.Perimeter();
        }

        static void GroupRoutingTestRect() {
            LayoutAlgorithmSettings settings;
            GeometryGraph graph = GetTestGraphWithClusters(out settings);
            var sugiyamaSettings = (SugiyamaLayoutSettings) settings;
            var router = new RectilinearEdgeRouter(graph, sugiyamaSettings.NodeSeparation/6,
                                                   sugiyamaSettings.NodeSeparation/6,
                                                   true,
                                                   sugiyamaSettings.EdgeRoutingSettings.UseObstacleRectangles)
                                                   {
                                                       BendPenaltyAsAPercentageOfDistance = sugiyamaSettings.EdgeRoutingSettings.BendPenalty
                                                   };
            router.Run();
#if DEBUG
            DisplayGeometryGraph.ShowGraph(graph);
#endif
        }

        static GeometryGraph GetTestGraphWithClusters(out LayoutAlgorithmSettings settings) {            
            GeometryGraph graph =
                GeometryGraphReader.CreateFromFile(
                    "C:\\dev\\GraphLayout\\MSAGLTests\\Resources\\MSAGLGeometryGraphs\\abstract.msagl.geom",
                    //"E:\\dev\\MSAGL\\GraphLayout\\MSAGLTests\\Resources\\MSAGLGeometryGraphs\\abstract.msagl.geom",
                    out settings);
            foreach (var edge in graph.Edges) {
                edge.Curve = null;
                edge.EdgeGeometry.TargetArrowhead = null;
            }
            graph.UpdateBoundingBox();
            var root = graph.RootCluster;
            var a = new Cluster {UserData = "a"};
            foreach (string id in new[] {"17", "39", "13", "19", "28", "12"})
                a.AddChild(graph.FindNodeByUserData(id));

            var b = new Cluster {UserData = "b"};
            b.AddChild(a);
            b.AddChild(graph.FindNodeByUserData("18"));
            root.AddChild(b);

            var c = new Cluster {UserData = "c"};
            foreach (string id in new[] {"30", "5", "6", "7", "8"})
                c.AddChild(graph.FindNodeByUserData(id));
            root.AddChild(c);

            var clusterNodes = new Set<Node>(root.AllClustersDepthFirst().SelectMany(cl => cl.Nodes));
            foreach (var node in graph.Nodes.Where(n => clusterNodes.Contains(n) == false))
                root.AddChild(node);

            FixClusterBoundariesWithNoRectBoundaries(root, 5);
            var fastIncrementalLayoutSettings = new FastIncrementalLayoutSettings();


            var d=new Dictionary<Cluster, LayoutAlgorithmSettings>();
            d[root] = new FastIncrementalLayoutSettings { AvoidOverlaps = true };

            var initialLayout = new InitialLayoutByCluster(graph, fastIncrementalLayoutSettings);
            initialLayout.Run();
            graph.UpdateBoundingBox();
            //FixClusterBoundariesWithNoRectBoundaries(root, 5);
            return graph;
        }

        static IEnumerable<Edge> CreateEdgesForGroupRouting(IEnumerable<Cluster> clusters) {
            var c = GetClusterById(clusters, "c");
            var f = GetClusterById(clusters, "f");
            var a = c.Clusters.Single(s => s.UserData.Equals("a"));
            var b = c.Clusters.Single(s => s.UserData.Equals("b"));
            var d = f.Clusters.Single(s => s.UserData.Equals("d"));
            var e = f.Clusters.Single(s => s.UserData.Equals("e"));
            var h = GetClusterById(clusters, "h");
            var g = GetClusterById(clusters, "g");
            var l = GetClusterById(clusters, "l");
            var k = GetClusterById(clusters, "k");
            var ports = new Dictionary<Cluster, Set<Port>>();
            AddPortToShape(k, ports, new CurvePort(k.BoundaryCurve, 0.4*(k.BoundaryCurve.ParEnd + k.BoundaryCurve.ParStart)));
            AddPortToShape(l, ports);
            AddPortToShape(a, ports);
            AddPortToShape(b, ports);
            AddPortToShape(d, ports);
            AddPortToShape(e, ports);
            AddPortToShape(h, ports);
            AddPortToShape(g, ports);
            var hSecondPort = new CurvePort(h.BoundaryCurve, 0.1);

            ports[h].Insert(hSecondPort);
            var hFreePort = new FloatingPort(null, new Point(-129, -55));
            var kFreePort = new FloatingPort(null, new Point(137, -51));
            ports[h].Insert(hFreePort);
            ports[k].Insert(kFreePort);

            var fFreePort = new FloatingPort(null, new Point(3, 3));
            var kFreePort0 = new FloatingPort(null, new Point(106, 8));
            AddPortToShape(f,ports, fFreePort);
            AddPortToShape(k, ports, kFreePort0);

            AddPortToShape(c,ports,new CurvePort(c.BoundaryCurve, 0));
            AddPortToShape(f, ports,new FloatingPort(f.BoundaryCurve, f.BoundingBox.Center));
            var freePort0 = new FloatingPort(null, new Point(72, -1.3));

            var halfFreeEdge = new EdgeGeometry(freePort0, ports[a].First());
            var freePort1 = new FloatingPort(null, new Point(-122, 110));
            var freeEdge = new EdgeGeometry(freePort0, freePort1);
            var ab = new EdgeGeometry(ports[a].First(), ports[b].First());
            var abE = new Edge(a, b) {EdgeGeometry = ab};
            var ae = new EdgeGeometry(ports[a].First(), ports[e].First());
            var aeE = new Edge(a, e) {EdgeGeometry = ae};
            var de = new EdgeGeometry(ports[d].First(), ports[e].First());
            var deE = new Edge(d, e) {EdgeGeometry = de};
            var ad = new EdgeGeometry(ports[a].First(), ports[d].First());
            var adE = new Edge(a, d) {EdgeGeometry = ad};
            var he = new EdgeGeometry(ports[h].First(), ports[e].First());
            var heE = new Edge(h, e) {EdgeGeometry = he};
            var gd = new EdgeGeometry(ports[g].First(), ports[d].First());
            var gdE = new Edge(g, d) {EdgeGeometry = gd};
            var lg = new EdgeGeometry(ports[l].First(), ports[g].First());
            var lgE = new Edge(l, g) {EdgeGeometry = lg};
            var le = new EdgeGeometry(ports[l].First(), ports[e].First());
            var leE = new Edge(l, e) {EdgeGeometry = le};
            var ca = new EdgeGeometry(ports[c].First(), ports[a].First());
            var caE = new Edge(c, a) {EdgeGeometry = ca};
            var cd = new EdgeGeometry(ports[c].First(), ports[d].First());
            var cdE = new Edge(c, d) {EdgeGeometry = cd};
            var hl = new EdgeGeometry(hSecondPort, ports[l].First());
            var hlE = new Edge(h, l) {EdgeGeometry = hl};
            var lh = new EdgeGeometry(ports[l].First(), hSecondPort);
            var lhE = new Edge(l, h) {EdgeGeometry = lh};
            var ha = new EdgeGeometry(hSecondPort, ports[a].First());
            var haE = new Edge(h, a) {EdgeGeometry = ha};
            var hk = new EdgeGeometry(hSecondPort, ports[k].First());
            var hkE = new Edge(h, k) {EdgeGeometry = hk};

            var hk0 = new EdgeGeometry(hFreePort, kFreePort);
            var hk0E = new Edge(h, k) {EdgeGeometry = hk0};

            var fk = new EdgeGeometry(fFreePort, kFreePort0);
            var fkE = new Edge(f, k) {EdgeGeometry = fk};

            var hThirdPort = new CurvePort(h.BoundaryCurve, 0.5);
            var halfFreeInside = new EdgeGeometry(ports[b].First(), freePort1);
            //var halfFreeInside = new Edge(b, d) { EdgeGeometry = ad };

            ports[h].Insert(hThirdPort);
            var lSecondPort = new CurvePort(l.BoundaryCurve, 0);
            ports[l].Insert(lSecondPort);
            var hThirdLSecond = new EdgeGeometry(hThirdPort, lSecondPort);
            var hThirdLSecondE = new Edge(h, l) {EdgeGeometry = hThirdLSecond};
            //            return new[] {
            //                             /*halfFreeInside, freeEdge,*/ halfFreeEdge, lh
            //                         };
            return new[] {
                             lhE, abE, aeE, deE, adE, heE, gdE, lgE, leE, cdE, hlE,
                             hThirdLSecondE, caE, haE, hkE, hk0E, fkE
                         };
        }

        static void AddPortToShape(Cluster a, Dictionary<Cluster, Set<Port>> ports, Port port) {
            Set<Port> s;
            if (!ports.TryGetValue(a, out s))
                s = ports[a] = new Set<Port>();

            s.Insert(port);
        }

        static Cluster GetClusterById(IEnumerable<Cluster> shapes, object id) {
            return shapes.Single(s => id.Equals(s.UserData));
        }

        static void AddPortToShape(Cluster a, Dictionary<Cluster,Set<Port>> ports) {
            Set<Port> s;
            if(!ports.TryGetValue(a,out s))
                s=ports[a]=new Set<Port>();

            s.Insert(new FloatingPort(a.BoundaryCurve, a.BoundaryCurve.BoundingBox.Center));
        }


        static GeometryGraph CreateGraphForGroupRouting() {
            return GeometryGraphReader.CreateFromFile("c:\\tmp\\bug.msagl.geom");
#if UNREACHABLE_CODE
            var graph=new GeometryGraph();

            var h = GenerateClusterH();
            var f = GenerateShapeF();
            var k = GenerateShapeK();
            var root = graph.RootCluster;;
            root.AddChild(h);
            root.AddChild(f);
            root.AddChild(k);
            var edges =
                    CreateEdgesForGroupRouting(graph.RootCluster.AllClustersDepthFirst());
            foreach (var edge in edges) {
                graph.Edges.Add(edge);
            }
            return graph;
#endif // UNREACHABLE_CODE
        }

        static Cluster GenerateShapeK() {
            var k = MakeCluster("k", 137, 4, 95, 147);
            k.AddChild(MakeCluster("l", 137, 4, 40, 80));
            return k;
        }

        static Cluster GenerateClusterH() {
            var g = MakeCluster("g", -160, -50, 36, 72);
            var h = MakeCluster("h", -130, -40, 117, 173);
            h.AddChild(g);
            h.AddChild(GenerateShapeC());
            return h;
        }

        static Cluster GenerateShapeC() {
            var a = MakeCluster("a", -100, -22, 15, 30);
            var b = MakeCluster("b", -99, -62, 15, 30);
            var c = MakeCluster("c", -100, -42, 32, 93);
            c.AddChild(a);
            c.AddChild(b);
            return c;
        }

        static Cluster GenerateShapeF() {
            var d = MakeCluster("d", 0, 43, 60, 60);
            var e = MakeCluster("e", 2, -17, 60, 24);
            var f = MakeCluster("f", 0, 0, 92, 173);
            //LayoutAlgorithmSettings.Show(d.BoundaryCurve, e.BoundaryCurve, f.BoundaryCurve);
            f.AddChild(d);
            f.AddChild(e);
            return f;
        }

        static Cluster MakeCluster(IComparable id, int x, int y, int w, int h) {
            var ret = new Cluster {
                                      UserData = id
                                  };
            ret.BoundaryCurve = MakeShapeCurve(new Point(x, y), w, h);
            ret.RectangularBoundary = new RectangularClusterBoundary();
            return ret;
        }

        static ICurve MakeShapeCurve(Point center, int width, int height) {
            var r = new Rectangle(center) {Width = width, Height = height};
            var c = new Curve();
            Curve.AddLineSegment(c, r.LeftBottom, r.LeftTop);
            Curve.ContinueWithLineSegment(c, r.RightTop);
            Curve.ContinueWithLineSegment(c, r.RightBottom);
            Curve.CloseCurve(c);
            return c;
        }


        static void TestPortEntry() {
            {
                ICurve c = CurveFactory.CreateRectangleWithRoundedCorners(100, 50, 3, 2, new Point());
                const int x = 200;
                const int y = 24;
                var p0 = new Point(x, -y);
                var p1 = new Point(x, y);
                var p2 = new Point(-x, y/2.0);
                var p3 = new Point(-x, -y/2.0);

                double par0 = c.ClosestParameter(p0);
                double par1 = c.ClosestParameter(p1);
                double par2 = c.ClosestParameter(p2);
                double par3 = c.ClosestParameter(p3);
                double par4 = c.ClosestParameter(new Point(48, -27));
                double par5 = c.ClosestParameter(new Point(44, -26));

                double par6 = c.ClosestParameter(new Point(-52, -22));
                double par7 = c.ClosestParameter(new Point(-49, -26.6));

#if TEST_MSAGL

                var portEntry = new PortEntryOnCurve(c, new[] {
                                    new Tuple<double, double>(par5, par4),
                                    new Tuple<double, double>(par2, par3),
                                    new Tuple<double, double>(par0, par1),
                                    new Tuple<double, double>(par6, par7),
                                });

                IEnumerable<DebugCurve> dc =
                    portEntry.GetEntryPoints().Select(
                        p => new DebugCurve(100, 1, "blue", CurveFactory.CreateEllipse(3, 3, p))).Concat(new[] {
                                                                    new DebugCurve(100, 1, "brown", c)
                                                                });
                dc = dc.Concat(
                    portEntry.Spans.SelectMany(
                        span =>
                        new[] {
                                  new DebugCurve(100, 1, "green", CurveFactory.CreateDiamond(3, 3, c[span.Item1])),
                                  new DebugCurve(100, 1, "green", CurveFactory.CreateDiamond(3, 3, c[span.Item2]))
                              }));

                LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dc);

#endif
            }
            {
                ICurve c = CurveFactory.CreateEllipse(50, 25, new Point());
                double par0 = c.ClosestParameter(new Point(31, -21));
                double par1 = c.ClosestParameter(new Point(34, -19));
                double par2 = c.ClosestParameter(new Point(49, -3));
                double par3 = c.ClosestParameter(new Point(50, 3));
                double par4 = c.ClosestParameter(new Point(-22, 23));
                double par5 = c.ClosestParameter(new Point(-29, 20));


                var portEntry = new PortEntryOnCurve(c,
                                                     new[] {
                                                               new Tuple<double, double>(par0, par1),
                                                               //  new Tuple<double, double>(par0, par1)
                                                           });
#if TEST_MSAGL
                IEnumerable<DebugCurve> dc =
                    portEntry.GetEntryPoints().Select(
                        p => new DebugCurve(100, 1, "blue", CurveFactory.CreateEllipse(3, 3, p))).Concat(new[] {
                                                                                new DebugCurve(100, 1, "brown", c)
                                                                            });
                dc = dc.Concat(
                    portEntry.Spans.SelectMany(
                        span =>
                        new[] {
                                  new DebugCurve(100, 1, "green", CurveFactory.CreateDiamond(3, 3, c[span.Item1])),
                                  new DebugCurve(100, 1, "green", CurveFactory.CreateDiamond(3, 3, c[span.Item2]))
                              }));

                LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dc);

                portEntry = new PortEntryOnCurve(c,
                                                 new[] {
                                                           new Tuple<double, double>(par4, par5),
                                                           new Tuple<double, double>(par2, par3),
                                                           new Tuple<double, double>(par0, par1),
                                                       });

                dc =
                    portEntry.GetEntryPoints().Select(
                        p => new DebugCurve(100, 1, "blue", CurveFactory.CreateEllipse(3, 3, p))).Concat(new[] {
                                                                                new DebugCurve(100, 1, "brown", c)
                                                                            });

                dc = dc.Concat(
                    portEntry.Spans.SelectMany(
                        span =>
                        new[] {
                                  new DebugCurve(100, 1, "green", CurveFactory.CreateDiamond(3, 3, c[span.Item1])),
                                  new DebugCurve(100, 1, "green", CurveFactory.CreateDiamond(3, 3, c[span.Item2]))
                              }));

                LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dc);
#endif
            }
        }

        static void LoadGeomFiles(IEnumerable<string> fileList, EdgeRoutingSettings edgeRoutingSettings, bool show) {
            foreach (string s in fileList)
                LoadGeomFile(s, edgeRoutingSettings, show);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileList"></param>
        /// <param name="edgeRoutingMode"></param>
        /// <param name="useSparseVisibilityGraph"></param>
        /// <param name="useObstacleRectangles"></param>
        /// <param name="bendPenalty"></param>
        /// <param name="coneAngle"></param>
        static void LoadGeomFilesTest(IEnumerable<string> fileList, EdgeRoutingMode edgeRoutingMode, bool useSparseVisibilityGraph,
                                    bool useObstacleRectangles, double bendPenalty, double coneAngle) {
            foreach (string s in fileList)
                LoadGeomFileTest(s, edgeRoutingMode, useSparseVisibilityGraph, useObstacleRectangles, bendPenalty, coneAngle);
        }

        static void LoadGeomFile(string s, EdgeRoutingSettings edgeRoutingSettings, bool show) {
            LayoutAlgorithmSettings settings;
            GeometryGraph geomGraph = GeometryGraphReader.CreateFromFile(s, out settings);
            if (geomGraph == null) {
                //Opens file "data.xml" and deserializes the object from it.
                var stream = File.Open(s, FileMode.Open);
                var formatter = new BinaryFormatter();
                geomGraph = (GeometryGraph) formatter.Deserialize(stream);
                geomGraph.RootCluster = new Cluster();
                stream.Close();
            }
            geomGraph.UpdateBoundingBox();
            if (FormStuff.initialLayout) {
                var l = new List<Cluster>();
                l.Add(geomGraph.RootCluster);
                var il = new InitialLayoutByCluster(geomGraph, new FastIncrementalLayoutSettings());
                il.Run();
            }
            else if (mds) {
                var mdsSettings = new MdsLayoutSettings
                { IterationsWithMajorization = 21,ScaleX = 1, ScaleY=1, RemoveOverlaps = true
                };
                var mdslayout = new MdsGraphLayout(mdsSettings, geomGraph);
                mdslayout.Run();
                var router = new SplineRouter(geomGraph, 1, 20, Math.PI / 6, edgeRoutingSettings.BundlingSettings);
                router.Run();
            }
            else {
                var sugiyamaSettings = (SugiyamaLayoutSettings) settings;
                if (edgeRoutingSettings.EdgeRoutingMode == EdgeRoutingMode.Rectilinear ||
                    edgeRoutingSettings.EdgeRoutingMode == EdgeRoutingMode.RectilinearToCenter) {
                    RouteRectEdgesOfGeomGraph(edgeRoutingSettings.EdgeRoutingMode, true, 
                                            edgeRoutingSettings.UseObstacleRectangles, edgeRoutingSettings.BendPenalty, geomGraph, sugiyamaSettings);
                } else {
                    const double angle = 30*Math.PI/180;
                    var router = new SplineRouter(geomGraph, 1, 20, angle, edgeRoutingSettings.BundlingSettings);
                    router.Run();

                    TestPadding(geomGraph);
                }
            }

#if DEBUG
            if (show) {
                geomGraph.UpdateBoundingBox();
                var b = geomGraph.BoundingBox;
                b.Pad(40);
                geomGraph.BoundingBox = b;
                DisplayGeometryGraph.ShowGraph(geomGraph);
            }
#endif
        }

        static void TestPadding(GeometryGraph geomGraph) {
            foreach (var edge in geomGraph.Edges) {
                TestPaddingForEdge(geomGraph, edge);
            }
        }

        static void TestPaddingForEdge(GeometryGraph geomGraph, Edge edge) {
            const int steps = 100;
            var edgeCurve = edge.Curve;
            var step = (edgeCurve.ParEnd - edgeCurve.ParStart)/steps;
            for(var par=edgeCurve.ParStart; par<edgeCurve.ParEnd; par= par+step) {
                var curvePoint = edgeCurve[par];
                foreach (var node in geomGraph.Nodes.Where(node => node == edge.Source && node == edge.Target)) {
                    var nb = node.BoundaryCurve;

                    var p = nb.ClosestParameter(curvePoint);
                    var nodePoint = nb[p];
#if DEBUG
                    if ((nodePoint - curvePoint).Length < 0.99)
                        LayoutAlgorithmSettings.Show(new LineSegment(nodePoint, curvePoint), nb, edgeCurve);
#endif
                }
            }
        }

        static void RouteRectEdgesOfGeomGraph(EdgeRoutingMode edgeRoutingMode, bool useSparseVisibilityGraph, bool useObstacleRectangles,
                                            double bendPenalty, GeometryGraph geomGraph, SugiyamaLayoutSettings settings) {
            var nodeShapeMap = new Dictionary<Node, Shape>();
            foreach (Node node in geomGraph.Nodes) {
                Shape shape = RectilinearInteractiveEditor.CreateShapeWithRelativeNodeAtCenter(node);
                nodeShapeMap[node] = shape;
            }

            var padding = (settings == null) ? 3 : settings.NodeSeparation / 3;
            var router = new RectilinearEdgeRouter(nodeShapeMap.Values, padding, 3, useSparseVisibilityGraph, useObstacleRectangles)
            {
                RouteToCenterOfObstacles = edgeRoutingMode == EdgeRoutingMode.RectilinearToCenter,
                BendPenaltyAsAPercentageOfDistance = bendPenalty
            };

            foreach (Edge edge in geomGraph.Edges) {
                EdgeGeometry edgeGeom = edge.EdgeGeometry;
                edgeGeom.SourcePort = nodeShapeMap[edge.Source].Ports.First();
                edgeGeom.TargetPort = nodeShapeMap[edge.Target].Ports.First();

                // Remove any path results retrieved from the geom file.
                edgeGeom.Curve = null;
                if (edgeGeom.SourceArrowhead != null)
                {
                    edgeGeom.SourceArrowhead.TipPosition = new Point();
                }
                if (edgeGeom.TargetArrowhead != null)
                {
                    edgeGeom.TargetArrowhead.TipPosition = new Point();
                }

                router.AddEdgeGeometryToRoute(edgeGeom);
            }
            router.Run();
        }

        static void LoadGeomFileTest(string s, EdgeRoutingMode edgeRoutingMode, bool useSparseVisibilityGraph,
                                    bool useObstacleRectangles, double bendPenalty, double coneAngle) {
            var random = new Random(1);
            var delta = new Point(2, 2);
            GeometryGraph geomGraph = GeometryGraphReader.CreateFromFile(s);
            const int reps = 1000;

            if (edgeRoutingMode == EdgeRoutingMode.Rectilinear || edgeRoutingMode == EdgeRoutingMode.RectilinearToCenter)
                RectilinearTestOnGeomGraph(edgeRoutingMode, useSparseVisibilityGraph, useObstacleRectangles, bendPenalty, reps, random, geomGraph, delta);
            else {
                var router = new SplineRouter(geomGraph, 9, 0.95238095238095233, Math.PI/6, null);
                router.Run();
#if TEST_MSAGL
                LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(DebugCurvesFromGraph(geomGraph));
#endif
            }
        }

        static void RectilinearTestOnGeomGraph(EdgeRoutingMode edgeRoutingMode, bool useSparseVisibilityGraph, bool useObstacleRectangles,
                                               double bendPenalty, int reps, Random random, GeometryGraph geomGraph, Point delta) {
            Console.WriteLine("shifting nodes and calling RectilinearEdgeRouter {0} times", reps);
            for (int i = 0; i < reps; i++) {
                Console.WriteLine(i + 1);
                ShiftNodes(random, geomGraph, delta);
                //                    if(i<=567)
                //                        continue;
                if (i == -1) {
                    GeometryGraphWriter.Write(geomGraph, "c:/tmp/ch0");
#if TEST_MSAGL
                    LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(DebugCurvesFromGraph(geomGraph));
#endif
                }
                var nodeShapeMap = new Dictionary<Node, Shape>();
                foreach (Node node in geomGraph.Nodes) {
                    Shape shape = RectilinearInteractiveEditor.CreateShapeWithRelativeNodeAtCenter(node);
                    nodeShapeMap[node] = shape;
                }

                var router = new RectilinearEdgeRouter(nodeShapeMap.Values, RectilinearEdgeRouter.DefaultPadding,
                                                       RectilinearEdgeRouter.DefaultCornerFitRadius, useSparseVisibilityGraph, useObstacleRectangles) {
                    RouteToCenterOfObstacles = edgeRoutingMode == EdgeRoutingMode.RectilinearToCenter,
                    BendPenaltyAsAPercentageOfDistance =  bendPenalty
                };

                foreach (Edge edge in geomGraph.Edges) {
                    EdgeGeometry edgeGeom = edge.EdgeGeometry;
                    edgeGeom.SourcePort = nodeShapeMap[edge.Source].Ports.First();
                    edgeGeom.TargetPort = nodeShapeMap[edge.Target].Ports.First();
                    router.AddEdgeGeometryToRoute(edgeGeom);
                }

                router.UpdateObstacles(geomGraph.Nodes.Select(n => nodeShapeMap[n]));
                router.Run();
            }
        }

        static void ShiftNodes(Random random, GeometryGraph geomGraph, Point delta) {
            foreach (Node node in geomGraph.Nodes) {
                double s = random.NextDouble();
                s *= 2;
                s -= 1;
                node.Center += delta*s;
            }
        }

#if TEST_MSAGL
        static IEnumerable<DebugCurve> DebugCurvesFromGraph(GeometryGraph graph) {
            IEnumerable<DebugCurve> dd = graph.Nodes.Where(n => n.BoundaryCurve != null).
                Select(n => new DebugCurve(1, "black", n.BoundaryCurve));
            return
                dd.Concat(graph.Edges.Where(edge => edge.Curve != null).Select(e => new DebugCurve(1, "blue", e.Curve)));
        }
#endif

        static void ConvertDotToGeom(string dotFileName, string geomFileName) {
            int line, column;
            string msg;
            Graph graph = Parser.Parse(dotFileName, out line, out column, out msg);
            var gv = new GViewer();
            gv.CalculateLayout(graph);
            GeometryGraph geomGraph = graph.GeometryGraph;
            GeometryGraphWriter.Write(geomGraph, geomFileName);
        }

        static void TestWayPoints() {
//#if TEST_MSAGL
//            var graph = Parser.GraphFromFile("c:/dev/graphlayout/graphs/fsm.dot");
//            var gv = new GViewer();
//#if DEBUG
//            gv.MouseMove += DisplayGeometryGraph.GviewerMouseMove;
//#endif
//            gv.CalculateLayout(graph);
//            var geomGraph = graph.GeometryGraph;
//            var router =
//                new RectilinearEdgeRouter(
//                    geomGraph.Nodes.Select(
//                        n => new Shape(n.Id, n.BoundaryCurve)));
//
//
//            foreach (var shape in router.Obstacles) {
//                var n = geomGraph.FindNode(shape.Id);
//                shape.Ports.Insert(new FloatingPort(n.BoundaryCurve, n.Center));
//            }
//
//            foreach (var e in graph.Edges) {
//                e.Label = null;
//                var geomEdge = e.GeometryEdge;
//                if (geomEdge.SourcePort == null)
//                    geomEdge.SourcePort = router.FindObstacle(geomEdge.Source.Id).Ports.First();
//                if (geomEdge.TargetPort == null)
//                    geomEdge.TargetPort = router.FindObstacle(geomEdge.Target.Id).Ports.First();
//                if ((string)geomEdge.Source.Id == "LR_6" && (string)geomEdge.Target.Id == "LR_5")
//                    geomEdge.EdgeGeometry.Waypoints = new[] { new Point(640, -183), new Point(670, -183) };
//                router.AddEdgeGeometryToRoute(e.GeometryEdge.EdgeGeometry);
//            }
//            router.RouteToCenterOfObstacles = true;
//            router.RouteEdges();
//
//
//            var f = new Form();
//            f.SuspendLayout();
//            f.Controls.Add(gv);
//            gv.Dock = DockStyle.Fill;
//            gv.NeedToCalculateLayout = false;
//            var statusStrip = new StatusStrip();
//            var toolStribLbl = new ToolStripStatusLabel("test");
//            statusStrip.Items.Add(toolStribLbl);
//            f.Controls.Add(statusStrip);
//            f.ResumeLayout();
//
//            gv.Graph = graph;
//            f.ResumeLayout();
//
//            f.ShowDialog();
//
//            Environment.Exit(0);
//#else
//            Console.WriteLine("Test_WayPoints is only available in TEST mode");
//#endif
        }

        static string GetFileSpec(string[] args, ref int iarg) {
            string s = args[iarg];
            string fileName = s.Substring(2);
            if (String.IsNullOrEmpty(fileName)) {
                if (iarg < (args.Length - 1)) {
                    fileName = args[++iarg];
                }
            }
            if (String.IsNullOrEmpty(fileName)) {
                throw new ApplicationException(String.Format("'{0}' is not followed by a {1}", s,
                                                             ('f' == s[1]) ? "listfile" : "filespec"));
            }
            return fileName;
        }

        static void TestSolver() {
            var solver = new Solver();
            Variable a = solver.AddVariable("a", 0, 2);
            Variable b = solver.AddVariable("b", 1, 2);
            solver.AddConstraint(a, b, 2);
            solver.Solve();
        }

        static void TestRouting() {
            LayoutAlgorithmSettings settings;
            GeometryGraph graph = GeometryGraphReader.CreateFromFile("../../../graphs/ch0.msagl.geom", out settings);
            foreach (Edge e in graph.Edges) {
                if (e.SourcePort == null)
                    e.SourcePort = new FloatingPort(e.Source.BoundaryCurve, e.Source.Center);
                if (e.TargetPort == null)
                    e.TargetPort = new FloatingPort(e.Target.BoundaryCurve, e.Target.Center);
            }
            var t = new Microsoft.Msagl.DebugHelpers.Timer();
            t.Start();

            EdgeRoutingSettings routingSettings = settings.EdgeRoutingSettings;
            for (int i = 0; i < 20; i++)
                if (routingSettings.EdgeRoutingMode == EdgeRoutingMode.Spline) {
                    var router = new SplineRouter(graph, 2, 3, 0.349065850398866, null);
                    router.Run();
                }
            t.Stop();
            Console.WriteLine(t.Duration);
            //#if DEBUG

            //            LayoutAlgorithmSettings.Show(a, b, c, eg.Curve, eg0.Curve, eg1.Curve);
            //#endif
        }

        static void TestConvexHull() {
            //            Curve curve = (Curve)CurveFactory.CreateTestShape(10, 9);
            //            IEnumerable<Microsoft.Msagl.Point> hull = ConvexHull.CalculateConvexHull(CurveEnds(curve));
            //            IEnumerator<Microsoft.Msagl.Point> en = hull.GetEnumerator();
            //            en.MoveNext();
            //            Microsoft.Msagl.Point a = en.Current;
            //            en.MoveNext();
            //            Microsoft.Msagl.Point b = en.Current;
            //
            //            Curve hullCurve = new Curve();
            //            hullCurve.AddSegment(new LineSegment(a, b));
            //            while (en.MoveNext()) {
            //                hullCurve.AddSegment(new LineSegment(hullCurve.End, en.Current));
            //            }
            //
            //            hullCurve.AddSegment(new LineSegment(hullCurve.End, hullCurve.Start));
            //#if DEBUG
            //            LayoutAlgorithmSettings.Show(hullCurve);
            //#endif
        }

        static void TestSave() {
            var g = new Graph();
            g.AddEdge("a", "b");
            var gv = new GViewer();
            gv.CalculateLayout(g);
            const string fileName = "c:\\tmp\\saved.msagl";
            g.Write(fileName);

            g = Graph.Read(fileName);

            var f = new Form();
            f.SuspendLayout();
            f.Controls.Add(gv);
            gv.Dock = DockStyle.Fill;
            gv.NeedToCalculateLayout = false;
            gv.Graph = g;
            f.ResumeLayout();

            f.ShowDialog();

            Environment.Exit(0);
        }

        static void TestPhylo() {
            var f = new Form2(false);
            var tree = new PhyloTree();
            var edge = (PhyloEdge)tree.AddEdge("a", "b");
            edge.Length = 1.0;
            edge = (PhyloEdge)tree.AddEdge("a", "c");
            edge.Length = 1.0000001;
            edge = (PhyloEdge)tree.AddEdge("c", "d");
            edge.Length = 2;
            edge = (PhyloEdge)tree.AddEdge("c", "e");
            edge.Length = 3;
            edge = (PhyloEdge)tree.AddEdge("b", "f");
            edge.Length = 3.5;
            edge = (PhyloEdge)tree.AddEdge("f", "l");
            edge.Length = 3.5;
            
            edge = (PhyloEdge)tree.AddEdge("b", "g");
            edge.Length = 4;
            tree.FindNode("a").Label.Text = "";
            tree.FindNode("b").Label.Text = "";
            tree.FindNode("c").Label.Text = "";
            tree.FindNode("d").Label.Text = "";
            
            f.GViewer.Graph = tree;
            f.ShowDialog();
        }

        static void TestMultiedges() {
            var drGraph = new Graph("foo");
            const int comps = 30;
            const int edgesInComp = 30;
            for (int i = 0; i < comps; i++) {
                string source = i.ToString();
                string target = (i + 1).ToString();
                for (int j = 0; j < edgesInComp; j++)
                    drGraph.AddEdge(source, i + " " + j, target);
            }

            var f = new Form2(false);
            object ret = f.GViewer.CalculateLayout(drGraph);

            f.StartPosition = FormStartPosition.CenterScreen;
            //   f.layerSeparationMult.Value = (decimal)(drGraph.GraphAttr.LayerSep / drGraph.GraphAttr.MinLayerSep);
            f.GViewer.SetCalculatedLayout(ret);

            f.Size = new Size(Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height);
            f.ShowDialog();
        }

        static void ProcessFileList(string listFile, int fileReps, bool show, bool mds, EdgeRoutingMode edgeRoutingMode, double bendPenalty,
                                    bool bundling, int randomShifts, bool useSparseVisibilityGraph, bool useObstacleRectangles) {
            StreamReader sr;
            try {
                sr = new StreamReader(listFile);
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return;
            }
            string fileName;
            int nOfBugs = 0;
            while ((fileName = sr.ReadLine()) != null) {
                if (String.IsNullOrEmpty(fileName)) continue;
                fileName = Path.Combine(Path.GetDirectoryName(listFile), fileName.ToLower());
                ProcessFile(fileName, fileReps, show, mds, edgeRoutingMode, bendPenalty, ref nOfBugs, bundling, randomShifts,
                            useSparseVisibilityGraph, useObstacleRectangles);
            }
            sr.Close();
        }

        static void ProcessFileSpec(string fileSpec, int fileReps, bool show, bool mds, EdgeRoutingMode edgeRoutingMode, double bendPenalty,
                                    bool bundling, int randomShifts, bool useSparseVisibilityGraph, bool useObstacleRectangles) {
            // strPathFileSpec may be with or without directory or wildcards, e.g.:
            //   x.txt, Test\Data\x.txt, Test\Data\Rand*.txt
            string fileName = Path.GetFileName(fileSpec);
            string dirName = Path.GetDirectoryName(fileSpec);
            dirName = Path.GetFullPath((0 == dirName.Length) ? "." : dirName);

            int nOfBugs = 0;
            FileSystemInfo[] fileInfos = new DirectoryInfo(dirName).GetFileSystemInfos(fileName);
            if (0 == fileInfos.Length) {
                Console.WriteLine("No matching files found for '{0}'", fileSpec);
                return;
            }
            foreach (FileSystemInfo fileInfo in fileInfos)
                ProcessFile(fileInfo.FullName, fileReps, show, mds, edgeRoutingMode, bendPenalty, ref nOfBugs, bundling, randomShifts,
                        useSparseVisibilityGraph, useObstacleRectangles);
        }

        static void ProcessFile(string fileName, int fileReps, bool show, bool mds, EdgeRoutingMode edgeRoutingMode, double bendPenalty,
                                ref int nOfBugs, bool bundling, int randomShifts, bool useSparseVisibilityGraph, bool useObstacleRectangles) {
            Console.WriteLine(fileName);
            var random = new Random(1);
            for (int rep = 0; rep < fileReps; ++rep) {
                try {
                    int line, column;
                    bool msaglFile;
                    Graph gwgraph = Form2.CreateDrawingGraphFromFile(fileName, out line, out column, out msaglFile);
                    if (msaglFile) {
                        using (var f = new Form2(false) {
                                    EdgeRoutingMode = edgeRoutingMode,
                                    UseObstacleRectangles = useObstacleRectangles}) {
                            f.GViewer.NeedToCalculateLayout=false;
                            if (edgeRoutingMode == EdgeRoutingMode.SplineBundling) {
                                var nodeSeparation = gwgraph.LayoutAlgorithmSettings.NodeSeparation;
                                GeometryGraph originalGraph=gwgraph.GeometryGraph;
                                var coneAngle = gwgraph.LayoutAlgorithmSettings.EdgeRoutingSettings.ConeAngle;
                                var padding = nodeSeparation / 3;
                                var loosePadding = SplineRouter.ComputeLooseSplinePadding(nodeSeparation, padding) * 2;
                                var br = new SplineRouter(originalGraph, padding, loosePadding, coneAngle,
                                                                            gwgraph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings);
                                br.Run();
                            }

                            if (show)
                            {
                                f.StartPosition = FormStartPosition.CenterScreen;
                                //     f.layerSeparationMult.Value = (decimal)(gwgraph.GraphAttr.LayerSep / gwgraph.GraphAttr.MinLayerSep);
                               
                                f.Size = new Size(Screen.PrimaryScreen.WorkingArea.Width,
                                                  Screen.PrimaryScreen.WorkingArea.Height);
                                f.GViewer.Graph = gwgraph;
                                f.ShowDialog();
                            }
                            
                        }
                    }
                    else 
                    if (gwgraph != null) {
                        gwgraph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = edgeRoutingMode;
                        gwgraph.LayoutAlgorithmSettings.EdgeRoutingSettings.UseObstacleRectangles = useObstacleRectangles;
                        gwgraph.LayoutAlgorithmSettings.EdgeRoutingSettings.BendPenalty = bendPenalty;
                        using (var f = new Form2(false) {
                                    EdgeRoutingMode = edgeRoutingMode,
                                    UseObstacleRectangles = useObstacleRectangles,
                                    BendPenalty = bendPenalty
                                }) {
                            if (mds) {
                                foreach (Microsoft.Msagl.Drawing.Node n in gwgraph.Nodes)
                                    n.Attr.FillColor = Color.WhiteSmoke;

                                gwgraph.LayoutAlgorithmSettings = new MdsLayoutSettings {
                                    EdgeRoutingSettings = { 
                                        EdgeRoutingMode = edgeRoutingMode,
                                        BundlingSettings = bundling ? new BundlingSettings() : null
                                    } 
                                };
                            }

                            object ret = f.GViewer.CalculateLayout(gwgraph);

                            if (randomShifts > 0) {
                                var del = new Point(2, 2);
                                GeometryGraph geomGraph = f.GViewer.Graph.GeometryGraph;
                                for (int i = 0; i < randomShifts; i++) {
                                    ShiftNodes(random, geomGraph, del);
                                    if (edgeRoutingMode == EdgeRoutingMode.Rectilinear ||
                                        edgeRoutingMode == EdgeRoutingMode.RectilinearToCenter)
                                        RouteRectEdgesOfGeomGraph(edgeRoutingMode, useSparseVisibilityGraph, useObstacleRectangles, bendPenalty,
                                                                  geomGraph, (SugiyamaLayoutSettings)gwgraph.LayoutAlgorithmSettings);
                                }
                            }

                            if (show) {
                                f.Text = Process.GetCurrentProcess().MainModule.FileName;
                                f.StartPosition = FormStartPosition.CenterScreen;
                                //     f.layerSeparationMult.Value = (decimal)(gwgraph.GraphAttr.LayerSep / gwgraph.GraphAttr.MinLayerSep);
                                f.GViewer.SetCalculatedLayout(ret);

                                f.Size = new Size(Screen.PrimaryScreen.WorkingArea.Width,
                                                  Screen.PrimaryScreen.WorkingArea.Height);
                                f.ShowDialog();
                            }
                        }
                    } else
                        Console.WriteLine(" skipping - cannot parse");
                }
                catch (Exception e) {
                    nOfBugs++;
                    Console.WriteLine("bug " + nOfBugs);
                    if (fileReps > 1) {
                        Console.WriteLine("  (iteration: {0})", rep);
                    }
                    Console.WriteLine(e.ToString());
                    return;
                }
                if ((rep > 1) && (0 == (rep%100))) {
                    Console.WriteLine("  {0} reps", rep);
                }
            }
        }

        static GeometryGraph LoadGraph(string filePath) {
            int line, column;
            string msg;
            Graph drawingGraph = Parser.Parse(filePath, out line, out column, out msg);
            drawingGraph.CreateGeometryGraph();
            GeometryGraph graph = drawingGraph.GeometryGraph;
            SugiyamaLayoutSettings settings = drawingGraph.CreateLayoutSettings();
            SetRandomNodeShapes(graph, new Random(1));
            return graph;
        }

        static void SetRandomNodeShapes(GeometryGraph graph, Random random) {
            foreach (Node node in graph.Nodes) {
                node.BoundaryCurve = GetRandomShape(random);
            }
        }

        static ICurve GetRandomShape(Random random) {
            //we support rectangle, roundedRectangle, circle, ellipse, diamond, Octagon, triangle, star            
            int index = random.Next(8);
            switch (index) {
                case 0:
                    return CurveFactory.CreateRectangle(25, 15, new Point());
                case 1:
                    return CurveFactory.CreateRectangleWithRoundedCorners(35, 25, 3, 3,
                                                                          new Point());
                case 2:
                    return CurveFactory.CreateCircle(19, new Point());
                case 3:
                    return CurveFactory.CreateEllipse(26, 18, new Point());
                case 4:
                    return CurveFactory.CreateDiamond(25, 15, new Point());
                case 5:
                    return CurveFactory.CreateOctagon(25, 15, new Point());
                case 6:
                    return CurveFactory.CreateInteriorTriangle(30, 20, new Point());
                case 7:
                    return CurveFactory.CreateStar(33, new Point());
            }

            return null;
        }
    }
}
