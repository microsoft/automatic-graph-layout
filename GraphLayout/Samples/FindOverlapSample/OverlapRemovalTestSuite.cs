
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.StressEnergy;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Timer = Microsoft.Msagl.DebugHelpers.Timer;
using Dot2Graph;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.GraphViewerGdi;
using Color = Microsoft.Msagl.Drawing.Color;
using Node = Microsoft.Msagl.Drawing.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;

namespace OverlapGraphExperiments
{
    internal class OverlapRemovalTestSuite {
        StreamWriter resultWriter;
     
        /// <summary>
        /// 
        /// </summary>
        public string ResultLogfile { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool HeaderWritten {
            get { return headerWritten; }
            set { headerWritten = value; }
        }

        bool headerWritten;
        private List<Tuple<string, OverlapRemovalSettings>> overlapMethods;
        private Tuple<String,Action<GeometryGraph>>[] layoutMethods;
        private static string subFolderName;
        public Dictionary<string, ErrorCouple> _errorDict = new Dictionary<string, ErrorCouple>();

        /// <summary>
        /// </summary>
        /// <param name="filename"></param>
        public OverlapRemovalTestSuite(String filename) {
            ResultLogfile = filename;
            resultWriter=new StreamWriter(filename,true);
        }


        private void TestOverlapRemovalOnGraph(string graphName, Graph parentGraph,
            HashSet<Tuple<int, int>> proximityEdges, HashSet<Tuple<int, int, int>> proximityTriangles,
            Tuple<String, Action<GeometryGraph>> layoutMethod, int layoutPos,
            Tuple<string, OverlapRemovalSettings> overlapMethod, int overlapMethodPos) {
//            Graph parentGraph = Helper.CopyGraph(parentGraphOriginal);
            var geomGraphOld = Helper.CopyGraph(parentGraph.GeometryGraph);
            var geomGraph = parentGraph.GeometryGraph;
            graphName = Path.GetFileNameWithoutExtension(graphName);
//            GeometryGraph graph = Helper.CopyGraph(geomGraph);
            List<Tuple<String, double>> statistics = new List<Tuple<string, double>>();

            String layoutMethodName = layoutMethod.Item1;
            String overlapMethodName = overlapMethod.Item1;
            var overlapSettings = overlapMethod.Item2;

            IOverlapRemoval overlapRemover = GetOverlapRemover(overlapSettings, geomGraph);

            overlapRemover.RemoveOverlaps();

            RouteGraphEdges(parentGraph);

            MakeEdgesTransparent(parentGraph);

            List<Tuple<string, double>> kClosestNeighborsError = new List<Tuple<string, double>>();
            var statIterations = RunStats(proximityEdges, proximityTriangles, overlapRemover, geomGraphOld, geomGraph,
                kClosestNeighborsError);
            foreach (var t in kClosestNeighborsError)
                statistics.Add(t);
            statistics.Add(statIterations);

            String nameAddon = "-" + layoutPos + "_" + overlapMethodPos + "-" + layoutMethodName + "_" +
                               overlapMethodName;

            parentGraph.GeometryGraph.UpdateBoundingBox();
            DumpProximityCdtToSvg("cdt_" + graphName + nameAddon + ".svg", parentGraph, proximityEdges);


            SvgGraphWriter.WriteAllExceptEdgesInBlack(parentGraph, graphName + nameAddon + ".svg");

            WriteHeader(statistics);
            String line = graphName + "," + geomGraph.Nodes.Count + "," + geomGraph.Edges.Count + "," + layoutMethodName +
                          "," + overlapMethodName;

            double closestNeighErr = 0;
            for (int i = 0; i < statistics.Count; i++) {
                Tuple<string, double> stat = statistics[i];
                line += "," + stat.Item2;
                if (stat.Item1.StartsWith("f"))
                    closestNeighErr += stat.Item2;
            }
            ErrorCouple ec;
            if (!_errorDict.TryGetValue(graphName, out ec)) {
                ec = new ErrorCouple();
                _errorDict[graphName] = ec;
            }
            if (overlapMethodName.Contains("PRISM"))
                ec.prismError = closestNeighErr;
            else
                ec.gtreeError = closestNeighErr;

            WriteLine(line);
        }

        private static Tuple<string, double> RunStats(HashSet<Tuple<int, int>> proximityEdges, HashSet<Tuple<int, int, int>> proximityTriangles, IOverlapRemoval overlapRemover,
            GeometryGraph geomGraphOld, GeometryGraph geomGraph, List<Tuple<string, double>> sharedFamily) {
            var statIterations = Tuple.Create("Iterations", (double) overlapRemover.GetLastRunIterations());
            /*
            rotationAngleMean = Statistics.Statistics.RotationAngleMean(geomGraphOld, geomGraph, proximityEdges);
            statProcrustes = Statistics.Statistics.ProcrustesStatistics(geomGraphOld.Nodes.Select(v => v.Center).ToList(),
                geomGraph.Nodes.Select(v => v.Center).ToList());
            statTriangleOrient = Statistics.Statistics.TriangleOrientation(geomGraphOld, geomGraph,
                proximityTriangles);
            statArea = Statistics.Statistics.Area(geomGraph);
            distortionOfTriangles = Statistics.Statistics.TrianglePropertyError(geomGraphOld, geomGraph, proximityTriangles);*/
            for (int k = 8; k <= 12; k++)
                sharedFamily.Add(Statistics.Statistics.SharedFamily(geomGraphOld, geomGraph, k));
            return statIterations;
        }

        private void DumpProximityCdtToSvg(string svgFileName, Graph graph, HashSet<Tuple<int, int>> proximityEdges) {
            SvgGraphWriter writer=new SvgGraphWriter(File.Create(svgFileName), graph);
            writer.TransformGraphByFlippingY();
            writer.WriteOpening();
            foreach (var t in proximityEdges) {
                writer.WriteLine(GetIthPoint(t.Item1, graph), GetIthPoint(t.Item2, graph));
            }
            writer.TransformGraphByFlippingY();
            writer.Close();
        }

        private Point GetIthPoint(int i, Graph graph) {
            return graph.GeometryGraph.Nodes[i].Center;
        }

        private void MakeEdgesTransparent(Graph parentGraph)
        {
            foreach (var e in parentGraph.Edges)
                e.Attr.Color = new Color(0, 0, 0, 0);
        }

        private IOverlapRemoval GetOverlapRemover(OverlapRemovalSettings settings, GeometryGraph geomGraph) {
            if (settings.Method == OverlapRemovalMethod.MinimalSpanningTree) return new GTreeOverlapRemoval(settings, geomGraph.Nodes.ToArray());
            else if (settings.Method==OverlapRemovalMethod.Prism) return new ProximityOverlapRemoval(settings, geomGraph);
            return null;
        }

        private static void GetProximityRelations(GeometryGraph graphOriginal, out HashSet<Tuple<int, int>> proximityEdges, out HashSet<Tuple<int, int, int>> proximityTriangles) {
            
// triangulation needed for statistics
            Cdt cdt = new Cdt(graphOriginal.Nodes.Select((v, index) => new Tuple<Point, object>(
                                                                           v.Center, (object) (index))));
            cdt.Run();
            proximityTriangles = GetProximityTriangles(cdt);
            proximityEdges = GetProximityEdges(cdt);
        }

        static Graph LoadGraphFile(String fileName, bool runLayout)
        {
            int line, column;
            string msg;
            Graph gwgraph = Parser.Parse(fileName, out line, out column, out msg);
            if (gwgraph == null) {
                MessageBox.Show(msg + String.Format(" line {0} column {1}", line, column));
                return null;
            }
            if (runLayout) {
                if (gwgraph.GeometryGraph == null) {
                    gwgraph.CreateGeometryGraph();
                    InitNodeBoundaries(gwgraph);
                }
                PivotMdsFullStress(gwgraph.GeometryGraph);
            }

            gwgraph.GeometryGraph.Margins = gwgraph.Width / 50;
            gwgraph.GeometryGraph.UpdateBoundingBox();
            return gwgraph;
        }

        private static void InitNodeBoundaries(Graph gwgraph) {
            foreach (var node in gwgraph.Nodes) {
                InitNodeBoundary(node);
            }
        }

        private static void InitNodeBoundary(Node node) {

            double width = 0;
            double height = 0;
            if (node.Label != null) {
                var font = new Font(node.Label.FontName, (float)node.Label.FontSize);
                StringMeasure.MeasureWithFont(node.LabelText, font, out width, out height);
              }
            if (width <= 0)
                StringMeasure.MeasureWithFont("a", new Font("New Courer", 10), out width, out height);

            node.GeometryNode.BoundaryCurve = CurveFactory.CreateEllipse(width/2, height/2, new Point());
        }

//        private static List<double> _sharedFamilyForPrism=new List<double>();
//        static List<double> _sharedFamilyForGtree=new List<double>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphFilename"></param>
        /// <param name="runLayout"></param>
        public void RunOverlapRemoval(string graphFilename, bool runLayout) {
            String graphName = Path.GetFileNameWithoutExtension(graphFilename);
            Graph graph = LoadGraphFile(graphFilename, runLayout);
            if (graph == null) {
                Console.WriteLine("Failed to load drawing graph: {0}", graphName);
                return;
            }
            if (graph.GeometryGraph == null)
            {
                Console.WriteLine("Failed to load geometry graph: {0}", graphName);
                return;
            }
            Point[] initPositions = graph.GeometryGraph.Nodes.Select(v => v.Center).ToArray();

            for (int i = 0; i < layoutMethods.Count(); i++)
            {
                var layoutMethod = layoutMethods[i];
              
                layoutMethod.Item2.Invoke(graph.GeometryGraph); //do initial layout
                //randomize cooincident points
                Point[] nodePositions = graph.GeometryGraph.Nodes.Select(v => v.Center).ToArray();

                //                LayoutAlgorithmSettings.ShowDebugCurves(
                //                    nodePositions.Select(p => new DebugCurve(220, 0.01, "green", CurveFactory.CreateOctagon(2, 2, p)))
                //                                 .ToArray());

                RandomizeNodes(graph, nodePositions);
                SvgGraphWriter.WriteAllExceptEdges(graph,
                    Path.GetFileNameWithoutExtension(graphName) + "-" + i.ToString() + "-" + layoutMethod.Item1 + ".svg");
                
                HashSet<Tuple<int, int, int>> proximityTriangles;
                HashSet<Tuple<int, int>> proximityEdges;
                GetProximityRelations(graph.GeometryGraph, out proximityEdges, out proximityTriangles);
                DumpProximityCdtToSvg("cdt_" + graphName + "-" + i.ToString() + "-" + layoutMethod.Item1 + ".svg",graph, proximityEdges);
                for (int j = 0; j < overlapMethods.Count; j++) {
                    if (graph.NodeCount == 0) continue;
                    var overlapMethod = overlapMethods[j];
                    TestOverlapRemovalOnGraph(graphName, graph, proximityEdges, proximityTriangles, layoutMethod, i,
                        overlapMethod, j);
                    SetOldPositions(nodePositions, graph);
                }
                SetOldPositions(initPositions, graph);
            }






            //#if DEBUG
            //            //write the number of crossings per iteration
            //            String convergenceFilename = graphName + "-crossPerIterat.csv";
            //            List<int> crossings1 = prism1.crossingsOverTime;
            //            List<int> crossings2 = prism2.crossingsOverTime;
            //
            //            int maxIter = Math.Max(crossings1.Count, crossings2.Count);
            //            List<String> lines=new List<string>();
            //            lines.Add("iteration,crossingsPRISM,crossingsGridBoost");
            //            for (int i = 0; i < maxIter; i++) {
            //                String l = i.ToString();
            //                if (i < crossings1.Count)
            //                    l += "," + crossings1[i];
            //                else l += ",0";
            //                if (i < crossings2.Count)
            //                    l += "," + crossings2[i];
            //                else l += ",0";
            //                lines.Add(l);
            //            }
            //            File.WriteAllLines(convergenceFilename,
            //                lines.ToArray(),Encoding.UTF8);
            //#endif
        }

        private static void RandomizeNodes(Graph graph, Point[] nodePositions)
        {
            ProximityOverlapRemoval.RandomizePoints(nodePositions, new Random(100), 0.01, true);
            for (int k = 0; k < nodePositions.Length; k++)
            {
                graph.GeometryGraph.Nodes[k].Center = nodePositions[k];
            }
        }
        
        private static void SetOldPositions(Point[] initPositions, Graph parentGraph) {
            for (int k = 0; k < initPositions.Length; k++) {
                parentGraph.GeometryGraph.Nodes[k].Center = initPositions[k];
            }
        }


        private void RouteGraphEdges(Graph graph) {
            foreach (Edge edge in graph.GeometryGraph.Edges) {
                StraightLineEdges.RouteEdge(edge, 0);
            }
        }

        private void WriteHeader(List<Tuple<string, double>> statistics) {
            lock (resultWriter) {
                if (!HeaderWritten) {
                    HeaderWritten = true;
                    String headerLine = "GraphName,|V|,|E|,InitialLayout,OverlapRemMethod";
                    var names = statistics.Select(s => "," + s.Item1);
                    foreach (string name in names) {
                        headerLine += name;
                    }
                    WriteLine(headerLine);
                }
            }
        }


        private static HashSet<Tuple<int, int,int>> GetProximityTriangles(Cdt cdt) {
            HashSet<Tuple<int, int,int>> proxTriangles = new HashSet<Tuple<int,int, int>>();

            foreach (var triangle in cdt.GetTriangles()) {
                int a = (int)triangle.Sites[0].Owner;
                int b = (int)triangle.Sites[1].Owner;
                int c = (int)triangle.Sites[2].Owner;

                proxTriangles.Add(Tuple.Create(a, b, c));
            }

            return proxTriangles;
        }

        private static HashSet<Tuple<int, int>>  GetProximityEdges(Cdt cdt) {
            HashSet<Tuple<int, int>> proxEdges = new HashSet<Tuple<int, int>>();

            foreach (var triangle in cdt.GetTriangles()) {
                foreach (var edge in triangle.Edges) {
                    int a = (int) edge.lowerSite.Owner;
                    int b = (int) edge.upperSite.Owner;
                    if (!proxEdges.Contains(Tuple.Create(a,b)) && !proxEdges.Contains(Tuple.Create(b,a)))
                    proxEdges.Add(Tuple.Create(a, b));
                }
            }

            return proxEdges;
        }

        /// <summary>
        ///     Used to close the file log and finalize the results.
        /// </summary>
        public void Finished() {
            lock (resultWriter) {
                resultWriter.Close();
            }
        }

        /// <summary>
        /// Runs the comparison for a given folder.
        /// </summary>
        public static void ComparisonSuite(String graphsFolder, String resultLog, bool parallelTest, bool runLayout) {

            string dataFolder = Path.GetFileName(Path.GetDirectoryName(graphsFolder));
            string dateTime = DateTime.Now.ToString("-yyyy.MM.dd-HH_mm");

            //Set the current directory.
            subFolderName = "TestSuite2-";
            Directory.CreateDirectory(subFolderName + dataFolder + dateTime);
            Directory.SetCurrentDirectory(subFolderName + dataFolder + dateTime);
            int numberCores = 1;
            if (parallelTest) numberCores = Environment.ProcessorCount;

            var testSuite = new OverlapRemovalTestSuite(resultLog);

            testSuite.overlapMethods = CollectionOverlapRemovalMethods();
            testSuite.layoutMethods = CollectionInitialLayout(runLayout);


            string[] filePaths = Directory.GetFiles(graphsFolder, "*.dot");
            
            Console.WriteLine(@"total graphs  = {0} ", filePaths.Count());
            Parallel.ForEach(
                filePaths,
                new ParallelOptions {MaxDegreeOfParallelism = numberCores},
                graphFilename => testSuite.RunOverlapRemoval(graphFilename, runLayout)
                );

            int prismWins = 0;
            int gtreeWins = 0;
            foreach (var t in testSuite._errorDict) {
                if (t.Value.prismError < t.Value.gtreeError)
                    prismWins++;
                else if (t.Value.prismError > t.Value.gtreeError)
                    gtreeWins++;
            }
            Console.WriteLine(@"total graphs {2} prism wins {0} gtree wins {1}", prismWins, gtreeWins, filePaths.Length);
            testSuite.Finished();
        }


        private static MdsLayoutSettings GetMdsSettings() {
            MdsLayoutSettings mSettings=new MdsLayoutSettings();
            mSettings.IterationsWithMajorization = 0;
            mSettings.RemoveOverlaps = false;
            mSettings.AdjustScale = false;
            mSettings.ScaleX = mSettings.ScaleY = 2;
            return mSettings;
        }

        public static void PivotMDS(GeometryGraph graph) {
            if (graph == null) return;
            var mdsLayoutSettings = GetMdsSettings();
            var mdsLayout = new MdsGraphLayout(mdsLayoutSettings, graph);
            mdsLayout.Run(new CancelToken());
        }

        public static void PivotMdsFullStress(GeometryGraph graph) {
            if (graph == null) return;

            var mdsLayoutSettings = GetMdsSettings();
            mdsLayoutSettings.IterationsWithMajorization = Math.Min((int)Math.Sqrt(graph.Nodes.Count),30);
            var mdsLayout = new MdsGraphLayout(mdsLayoutSettings, graph);
            mdsLayout.Run(new CancelToken());
        }

        public static void SFDP(GeometryGraph graph) {
            // input dot files should already have SFDP layout
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graph1"></param>
        /// <param name="graph2"></param>
        public void CompareGraphs(GeometryGraph graph1, GeometryGraph graph2) {
        }

        /// <summary>
        /// Set of Layout Collection
        /// </summary>
        /// <returns></returns>
        public static Tuple<String,Action<GeometryGraph>>[] CollectionInitialLayout(bool runLayout) {
//            if (runLayout)
//                return new Tuple<String, Action<GeometryGraph>>[] {
////                new Tuple<String, Action<GeometryGraph>>("PivotMDS",PivotMDS),
//                new Tuple<String, Action<GeometryGraph>>("PivotMDS_Stress",PivotMdsFullStress)
//   //             new Tuple<String, Action<GeometryGraph>>("SFDP",SFDP)
//            };
//            else
                return new Tuple<String, Action<GeometryGraph>>[] {
                new Tuple<String, Action<GeometryGraph>>("ident", geometryGraph => {geometryGraph.Edges.Clear(); })
            };
        }

        /// <summary>
        /// List of the settings for which the comparison should be made.
        /// </summary>
        /// <returns></returns>
        public static List<Tuple<String,OverlapRemovalSettings>> CollectionOverlapRemovalMethods() {
            List<Tuple<String,OverlapRemovalSettings>> testList=new List<Tuple<String,OverlapRemovalSettings>>();

            //set OverlapRemovalSettings so that we can be sure that the wanted parameters are used.
            OverlapRemovalSettings settings=new OverlapRemovalSettings();
            settings.Method=OverlapRemovalMethod.Prism;
            settings.Epsilon=1E-6;
            settings.IterationsMax = 1000;
            settings.StopOnMaxIterat = false;
            settings.NodeSeparation = 4;
            settings.RandomizationSeed = 21;
            settings.WorkInInches = false;
            settings.RandomizeAllPointsOnStart = false;
            settings.RandomizationSeed = 10;

            settings.StressSettings.MaxStressIterations = 27;
            settings.StressSettings.SolvingMethod = SolvingMethod.PrecondConjugateGradient;
            settings.StressSettings.UpdateMethod = UpdateMethod.Parallel;
            settings.StressSettings.StressChangeTolerance = 10E-4;
            settings.StressSettings.CancelOnStressConvergence = true;
            settings.StressSettings.CancelOnStressMaxIteration = true;
            settings.StressSettings.Parallelize = false;
            settings.StressSettings.SolverMaxIteratMethod=MaxIterationMethod.SqrtProblemSize;
            //relevant for conjugate gradient methods only
            settings.StressSettings.ResidualTolerance = 0.01;
            settings.StressSettings.CancelAfterFirstConjugate = true;

            testList.Add(Tuple.Create("PRISM-CG",settings)); //prism +precond. conjugate gradient

            settings = settings.Clone();
            settings.StressSettings.SolverMaxIteratMethod=MaxIterationMethod.LinearProblemSize;
            settings.StressSettings.ResidualTolerance = 0.01;
            settings.StressSettings.SolvingMethod = SolvingMethod.PrecondConjugateGradient;
//            testList.Add(Tuple.Create("PRISM-CG-2", settings));

            //settings = settings.Clone();
            //settings.StressSettings.MaxStressIterations = 15;
            //settings.StressSettings.SolvingMethod = SolvingMethod.Localized;
            //testList.Add(Tuple.Create("PRISM-LM",settings));

            settings = settings.Clone();
            settings.Method=OverlapRemovalMethod.MinimalSpanningTree;
            testList.Add(Tuple.Create("GTree", settings));

            return testList;
        }

        /// <summary>
        /// Thread safe wr
        /// </summary>
        /// <param name="line"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void WriteLine(String line) {
            lock (resultWriter) {
                resultWriter.WriteLine(line);
            }
        }
    }

    internal class ErrorCouple {
        public double prismError;
        public double gtreeError;
    }
}
