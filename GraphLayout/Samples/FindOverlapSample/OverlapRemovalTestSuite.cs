
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.StressEnergy;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Node = Microsoft.Msagl.Core.Layout.Node;
using Timer = Microsoft.Msagl.DebugHelpers.Timer;

namespace FindOverlapSample {
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

        /// <summary>
        /// </summary>
        /// <param name="filename"></param>
        public OverlapRemovalTestSuite(String filename) {
            ResultLogfile = filename;
            resultWriter=new StreamWriter(filename,true);
        }


        private void TestOverlapRemovalOnGraph(string graphName, Graph parentGraph,
            HashSet<Tuple<int, int>> proximityEdges, HashSet<Tuple<int, int, int>> proximityTriangles, Tuple<String, Action<GeometryGraph>> layoutMethod, int layoutPos, Tuple<string, OverlapRemovalSettings> overlapMethod, int overlapMethodPos) {
//            Graph parentGraph = Helper.CopyGraph(parentGraphOriginal);
            var geomGraphOld = Helper.CopyGraph(parentGraph.GeometryGraph);
            var geomGraph = parentGraph.GeometryGraph;
//            GeometryGraph graph = Helper.CopyGraph(geomGraph);
            List<Tuple<String, double>> statistics = new List<Tuple<string, double>>();

            String layoutMethodName = layoutMethod.Item1;
            String overlapMethodName = overlapMethod.Item1;
            var overlapSettings = overlapMethod.Item2;
            
            IOverlapRemoval overlapRemover = GetOverlapRemover(overlapSettings, geomGraph);
            
            
            overlapRemover.RemoveOverlaps();
            
            RefreshAndCleanGraph(parentGraph);
         

            var statIterations = Tuple.Create("Iterations", (double)overlapRemover.GetLastRunIterations());

            var statEdgeLength = Statistics.Statistics.EdgeLengthDeviation(geomGraphOld, geomGraph, proximityEdges);
            var statProcrustes =
                Statistics.Statistics.ProcrustesStatistics(geomGraphOld.Nodes.Select(v => v.Center).ToList(),
                                                                  geomGraph.Nodes.Select(v => v.Center).ToList());
            var statTriangleOrient = Statistics.Statistics.TriangleOrientation(geomGraphOld, geomGraph, proximityTriangles);
            var statArea = Statistics.Statistics.Area(geomGraph);

//            statistics.Add(Tuple.Create());
            statistics.Add(statIterations);
            statistics.Add(statEdgeLength);
            statistics.Add(statProcrustes);
            statistics.Add(statArea);
            statistics.Add(statTriangleOrient);

            String nameAddon = "-" + layoutPos.ToString() + "_" + overlapMethodPos + "-" + layoutMethodName + "_" +
                               overlapMethodName;
          
//            RefreshAndCleanGraph(parentGraph);
//           Parallel.Invoke(
//            ()=>parentGraph.Write(graphName + nameAddon+".msagl"),
//            () => 
            SvgGraphWriter.Write(parentGraph, graphName + nameAddon + ".svg");
//            
//            );

            WriteHeader(statistics);
            String line = graphName + "," + geomGraph.Nodes.Count + "," + geomGraph.Edges.Count +","+layoutMethodName+","+overlapMethodName;
            for (int i = 0; i < statistics.Count; i++) {
                Tuple<string, double> stat = statistics[i];
                line += "," + stat.Item2;
            }
            WriteLine(line);
        }

        private IOverlapRemoval GetOverlapRemover(OverlapRemovalSettings settings, GeometryGraph geomGraph) {
            if (settings.Method == OverlapRemovalMethod.MinimalSpanningTree) return new OverlapRemoval(settings, geomGraph.Nodes.ToArray());
            else if (settings.Method==OverlapRemovalMethod.Prism) return new ProximityOverlapRemoval(settings);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphFilename"></param>
        public void RunOverlapRemoval(String graphFilename) {
            String graphName = Path.GetFileNameWithoutExtension(graphFilename);
            Graph graph=DotLoader.LoadGraphFile(graphFilename);
            Point[] initPositions = graph.GeometryGraph.Nodes.Select(v => v.Center).ToArray();
            if (graph == null || graph.GeometryGraph == null) {
                Console.WriteLine("Failed to load graph: {0}", graphName);
                return;
            }

            for (int i = 0; i < layoutMethods.Count(); i++) {
                var layoutMethod = layoutMethods[i];
                layoutMethod.Item2.Invoke(graph.GeometryGraph); //do initial layout
                //randomize cooincident points
                Point[] nodePositions = graph.GeometryGraph.Nodes.Select(v => v.Center).ToArray();

//                LayoutAlgorithmSettings.ShowDebugCurves(
//                    nodePositions.Select(p => new DebugCurve(220, 0.01, "green", CurveFactory.CreateOctagon(2, 2, p)))
//                                 .ToArray());

                ProximityOverlapRemoval.RandomizePoints(nodePositions, new Random(100), 0.01, true);
                for (int k = 0; k < nodePositions.Length; k++) {
                    graph.GeometryGraph.Nodes[k].Center = nodePositions[k];
                }

                DoInitialScaling(graph.GeometryGraph,InitialScaling.Inch72Pixel);
                RefreshAndCleanGraph(graph);
                SvgGraphWriter.Write(graph, graphName + "-" + i.ToString() + "-" + layoutMethod.Item1 + ".svg");

//                HashSet<Point> pointSet=new HashSet<Point>();
//                foreach (Point p in nodePositions) {
//                    Console.WriteLine(p);
//                    if (pointSet.Contains(p)) {
//                        Console.WriteLine("Coincident points.");
//                    }
//                    else pointSet.Add(p);
//                }

                HashSet<Tuple<int, int, int>> proximityTriangles;
                HashSet<Tuple<int, int>> proximityEdges;
                GetProximityRelations(graph.GeometryGraph, out proximityEdges, out proximityTriangles);

                for (int j = 0; j < overlapMethods.Count; j++) {
                    var overlapMethod = overlapMethods[j];
                    TestOverlapRemovalOnGraph(graphName, graph,proximityEdges,proximityTriangles, layoutMethod, i, overlapMethod, j);

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

        static void DoInitialScaling(GeometryGraph Graph,InitialScaling initScaling) {
            if (Graph.Edges.Count == 0) return;
            var nodePositions = Graph.Nodes.Select(v => v.Center).ToArray();
            var nodeBoxes = Graph.Nodes.Select(v => v.BoundingBox).ToArray();

            var avgEdgeLength = AvgEdgeLength(Graph);

            double goalLength;
            if (initScaling== InitialScaling.Inch72Pixel)
                goalLength = 72;
            else if (initScaling == InitialScaling.AvgNodeSize)
                goalLength = nodeBoxes.Average(box => (box.Width + box.Height) / 2);
            else return;

            double scaling = goalLength / avgEdgeLength;


            for (int j = 0; j < nodePositions.Length; j++) {
                nodePositions[j] *= scaling;
                Rectangle rect = nodeBoxes[j];
                rect.Center = nodePositions[j];
                nodeBoxes[j] = rect;
                Graph.Nodes[j].Center = nodePositions[j];
            }

        

        }

        private static double AvgEdgeLength(GeometryGraph Graph) {
            int i = 0;
            double avgEdgeLength = 0;
            foreach (Edge edge in Graph.Edges) {
                Point sPoint = edge.Source.Center;
                Point tPoint = edge.Target.Center;
                double euclid = (sPoint - tPoint).Length;
                avgEdgeLength += euclid;
                i++;
            }
            avgEdgeLength /= i;
            return avgEdgeLength;
        }


        private static void SetOldPositions(Point[] initPositions, Graph parentGraph) {
            for (int k = 0; k < initPositions.Length; k++) {
                parentGraph.GeometryGraph.Nodes[k].Center = initPositions[k];
            }
        }


        private void RefreshAndCleanGraph(Graph graph) {
//            List<Microsoft.Msagl.Drawing.Edge> selfEdges=new List<Microsoft.Msagl.Drawing.Edge>();
//            foreach (Microsoft.Msagl.Drawing.Node node in graph.Nodes) {
//                selfEdges.AddRange(node.SelfEdges);
//            }
//
//            selfEdges.ForEach((edge) => graph.RemoveEdge(edge));

//            if (graph.Edges.Count() > 3000) {
//            //delete all edges
//                graph.GeometryGraph.Edges.Clear();
////            var edgeList=graph.GeometryGraph.Edges.Reverse().ToList();
////                edgeList.ForEach((edge)=>graph.GeometryGraph.Edges.Remove(edge));
//            }
//            else {

          


//            }

            foreach (Microsoft.Msagl.Drawing.Node node in graph.Nodes) {
                var c = node.Attr.Color;
                node.Attr.FillColor = new Color(170,c.R,c.G,c.B);
            }

            foreach (Microsoft.Msagl.Drawing.Edge edge in graph.Edges) {
                edge.Attr.Color=new Color(220,115,115,115);
            }

            foreach (Edge edge in graph.GeometryGraph.Edges) {
                StraightLineEdges.RouteEdge(edge, 0);
            }
            
        }

        private ProximityOverlapRemoval RunOverlapRemoval(GeometryGraph graphCopy, GeometryGraph graphOriginal, HashSet<Tuple<int, int>> proximityEdges,
                                       HashSet<Tuple<int, int, int>> proximityTriangles, List<Tuple<string, double>> statistics, OverlapRemovalSettings settings) {
            ProximityOverlapRemoval prism = new ProximityOverlapRemoval(graphCopy);
            prism.Settings = settings;
            Timer timer = new Timer();
            timer.Start();
            prism.RemoveOverlaps();
            timer.Stop();
            var cpuTimeSpan = TimeSpan.FromSeconds(timer.Duration);
            var statCpuTime = Tuple.Create("CPUTime", cpuTimeSpan.TotalSeconds);
            var statIterations = Tuple.Create("Iterations", (double)prism.LastRunIterations);

            var statEdgeLength = Statistics.Statistics.EdgeLengthDeviation(graphOriginal, graphCopy, proximityEdges);
            var statProcrustes =
                Statistics.Statistics.ProcrustesStatistics(graphOriginal.Nodes.Select(v => v.Center).ToList(),
                                                                  graphCopy.Nodes.Select(v => v.Center).ToList());
            var statTriangleOrient = Statistics.Statistics.TriangleOrientation(graphOriginal, graphCopy, proximityTriangles);
            var statArea = Statistics.Statistics.Area(graphCopy);


            statistics.Add(statCpuTime);
            statistics.Add(statIterations);
            statistics.Add(statEdgeLength);
            statistics.Add(statProcrustes);
            statistics.Add(statArea);
            statistics.Add(statTriangleOrient);
            return prism;
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
        public static void ComparisonSuite(String graphsFolder, String resultLog, bool parallelTest ) {

            string dataFolder = Path.GetFileName(Path.GetDirectoryName(graphsFolder));
            string dateTime = DateTime.Now.ToString("-yyyy.MM.dd-HH_mm");
            
            //Set the current directory.
            subFolderName = "TestSuite2-";
            Directory.CreateDirectory(subFolderName + dataFolder+dateTime);
            Directory.SetCurrentDirectory(subFolderName+dataFolder+dateTime);
            int numberCores = 1;
            if (parallelTest) numberCores = Environment.ProcessorCount;

            var testSuite = new OverlapRemovalTestSuite(resultLog);

            testSuite.overlapMethods = CollectionOverlapRemovalMethods();
            testSuite.layoutMethods = CollectionInitialLayout();


          string[] filePaths = Directory.GetFiles(graphsFolder, "*.dot");
            
            Parallel.ForEach(
                filePaths,
                new ParallelOptions { MaxDegreeOfParallelism = numberCores },
                testSuite.RunOverlapRemoval
                );

            testSuite.Finished();
        }


        private static MdsLayoutSettings GetMdsSettings() {
            MdsLayoutSettings mSettings=new MdsLayoutSettings();
            mSettings.IterationsWithMajorization = 0;
            mSettings.RemoveOverlaps = false;
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
        public static Tuple<String,Action<GeometryGraph>>[] CollectionInitialLayout() {
            return new Tuple<String, Action<GeometryGraph>>[] {
//                new Tuple<String, Action<GeometryGraph>>("PivotMDS",PivotMDS),
                new Tuple<String, Action<GeometryGraph>>("PivotMDS+Stress",PivotMdsFullStress),
                new Tuple<String, Action<GeometryGraph>>("SFDP",SFDP)
            };
        }

        private String ActionName<T> (Expression<Action<T>> method) {
            
                var info = (MethodCallExpression)method.Body;
                string name = info.Method.Name;
            
            return name;
            
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
            settings.InitialScaling=InitialScaling.Inch72Pixel;
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

            settings = settings.Clone();
            settings.StressSettings.MaxStressIterations = 15;
            settings.StressSettings.SolvingMethod = SolvingMethod.Localized;
            testList.Add(Tuple.Create("PRISM-LM",settings));

            settings = settings.Clone();
            settings.Method=OverlapRemovalMethod.MinimalSpanningTree;
            testList.Add(Tuple.Create("MST", settings));

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
}
