using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using ArgsParser;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Miscellaneous;

namespace Z3Graphs {

    class Program {
        
        static void Main(string[] args) {
            int numberGrapsToWrite, diff;
            bool onlyDiffs;
            string inputDir;
            string outputDir;
            SetupAndParseArgs(args, out numberGrapsToWrite, out diff, out onlyDiffs, out inputDir, out outputDir);

            string[] graphList = GetAllGraphsNamesSorted(inputDir);
            List<Node> nodes = PositionNodes(graphList);
            graphList = GetListOfGraphsToWrite(graphList, numberGrapsToWrite, diff);

            string fileName = @"out";
            fileName += "_" + numberGrapsToWrite.ToString() + "_" + diff.ToString() + "_" + onlyDiffs.ToString() + ".csv";
            fileName = Path.Combine(outputDir, fileName);
            WriteGraphs(fileName, graphList, nodes, onlyDiffs);

        }

        private static void SetupAndParseArgs(string[] args, out int numberGrapsToWrite, out int diff, out bool onlyDiffs,
            out string inputDir, out string outputDir) {
            ArgsParser.ArgsParser ap = new ArgsParser.ArgsParser(args);
            ap.AddOptionWithAfterStringWithHelp("ng", "number of graphs to output");
            ap.AddOptionWithAfterStringWithHelp("diff", "the minimal differences between two consequtive graphs in the output");
            ap.AddOptionWithAfterStringWithHelp("inputDir", "the input directory");
            ap.AddOptionWithAfterStringWithHelp("outputDir", "the output directory");
            ap.AddAllowedOptionWithHelpString("onlydiff", "output differences only");
            ap.AddAllowedOptionWithHelpString("/h", "prints the usage string and exits");
            ParseCommandLine(ap, out numberGrapsToWrite, out diff, out onlyDiffs, out inputDir, out outputDir);
        }

        static string[] GetListOfGraphsToWrite(string[] graphList, int numberGrapsToWrite, int diff) {
            var ret = new List<string>();
            var edges = ParseGraphEdgesOnly(graphList[0]);
            ret.Add(graphList[0]);
            for (int next = 1; next < graphList.Length; next++) {
                var es = ParseGraphEdgesOnly(graphList[next]);
                if (Diff(es, edges) >= ((diff/ 100.0) * (edges.Count + es.Count))) { // diff percentage of the half sum of the edges in both graphs
                    ret.Add(graphList[next]);
                    if (ret.Count >= numberGrapsToWrite)
                        break;
                    edges = es;
                }
            }
            return ret.ToArray();
        }

        private static int Diff(HashSet<Tuple<ushort, ushort>> edges, HashSet<Tuple<ushort, ushort>> es) {
            int r = 0;
            foreach (var e in edges) {
                if (!es.Contains(e))
                    r++;
            }
            foreach (var e in es) {
                if (!edges.Contains(e))
                    r++;
            }
            return r;
        }

        private static HashSet<Tuple<ushort, ushort>> ParseGraphEdgesOnly(string fileName) {
            string edgeLine = GetEdgeLine(fileName);
            var edges = ParseEdgeLine(edgeLine);
            var s = new HashSet<Tuple<ushort, ushort>>();
            foreach (var e in edges) { s.Add(e); }
            return s;
            
        }

        static void ParseCommandLine(ArgsParser.ArgsParser ap, out int ng, out int diff, out bool onlyDiffs, 
            out string inputDir, out string outputDir ) {
            ng = 10;
            diff = 5; // percentage of edges
            onlyDiffs = false;
            inputDir = "Z:\\";
            outputDir = ".";
            if (ap.Parse() == false) {
                Console.WriteLine("{0}", ap.ErrorMessage);
                Environment.Exit(1);
            }
            if (ap.OptionIsUsed("/h")) {
                Console.WriteLine(ap.UsageString());
                Environment.Exit(0);
            }
            if (ap.OptionIsUsed("onlydiff")) {
                onlyDiffs = true;
            }
            if (ap.OptionIsUsed("inputDir")) {
                inputDir = ap.GetStringOptionValue("inputDir");
            }
            if (ap.OptionIsUsed("outputDir")) {
                outputDir = ap.GetStringOptionValue("outputDir");
            }
            if (ap.OptionIsUsed("ng")) {
                ap.GetIntOptionValue("ng", out ng);
            }
            if (ap.OptionIsUsed("diff")) {
                ap.GetIntOptionValue("diff", out diff);
            }
        }

        private static void WriteGraphs(string fileName, string[] graphList, List<Node> nodes, bool onlyDiffs) {
            if (onlyDiffs == false) {
                using (var file = new System.IO.StreamWriter(fileName)) {
                    int edgeNumber = 0;
                    int z = 0;
                    foreach (var graph in graphList) {
                        WriteGraph(file, graph, z++, nodes, ref edgeNumber);
                    }
                }
            }
            else {
                Console.WriteLine("writing diffs only");
                using (var file = new StreamWriter(fileName)) {
                    int edgeNumber = 0;
                    int z = 0;
                    HashSet<Tuple<ushort, ushort>> edges = null;
                    foreach (var graph in graphList) {
                        WriteGraphDiffs(file, graph, z++, nodes, ref edgeNumber, ref edges);
                    }
                }
            }
        }

        private static void WriteGraphDiffs(StreamWriter file, string graph, int z, List<Node> nodes, ref int edgeNumber, ref HashSet<Tuple<ushort, ushort>> prevEdges) {
            var activities = new List<int>();
            var edges = ParseGraph(graph, activities);
            var edgeSet = new HashSet<Tuple<ushort, ushort>>();

            foreach (var e in edges) {
                edgeSet.Add(e);
                if (prevEdges != null && prevEdges.Contains(e)) continue;
                int si = e.Item1;
                int ti = e.Item2;
                Node s = nodes[si];
                Node t = nodes[ti];
                file.WriteLine("{0},{1},{2},{3},{4},{5}", ++edgeNumber, s.Center.X, s.Center.Y, z, activities[si], "n");
                file.WriteLine("{0},{1},{2},{3},{4},{5}", edgeNumber, t.Center.X, t.Center.Y, z, activities[ti], "n");
            }
            if (prevEdges != null)
            foreach (var e in prevEdges) { // dump dead edges
                if (edges.Contains(e)) continue;
                int si = e.Item1;
                int ti = e.Item2;
                Node s = nodes[si];
                Node t = nodes[ti];
                file.WriteLine("{0},{1},{2},{3},{4},{5}", ++edgeNumber, s.Center.X, s.Center.Y, z, activities[si], "o");
                file.WriteLine("{0},{1},{2},{3},{4},{5}", edgeNumber, t.Center.X, t.Center.Y, z, activities[ti], "o");
            }
            prevEdges = edgeSet;
        }

        private static void WriteGraph(StreamWriter file, string fileName, int z, List<Node> nodes, ref int edgeNumber) {
            var activities = new List<int> ();
            var edges = ParseGraph(fileName, activities);
            foreach(var e in edges) {
                int si = e.Item1;
                int ti = e.Item2;
                Node s = nodes[si];
                Node t = nodes[ti];
                file.WriteLine("{0},{1},{2},{3},{4}", ++edgeNumber, s.Center.X, s.Center.Y, z, activities[si] ) ;
                file.WriteLine("{0},{1},{2},{3},{4}",   edgeNumber, t.Center.X, t.Center.Y, z, activities[ti]);                
            }
        }

        private static List<Node> PositionNodes(string[] graphList) {
            //int i = graphList.Length - 1; // take the last graph
            int i = 0;
            List<int> activities = new List<int>();
            var edges = ParseGraph(graphList[i], activities);
            GeometryGraph g = new GeometryGraph();
            List<Node> nodes = CreateNodes(edges);
            foreach (var n in nodes)
                if (n != null)
                    g.Nodes.Add(n);
            foreach (var t in edges) {
                g.Edges.Add(new Edge(nodes[t.Item1], nodes[t.Item2]));
            }
            LayoutGraph(g);
            return nodes;
        }

        private static List<Node> CreateNodes(List<Tuple<ushort, ushort>> edges) {
            int length = -1;

            foreach (var e in edges) {
                length = Math.Max(length, Math.Max(e.Item1, e.Item2));
            }
            length++;
            List<Node> l = new List<Node>();
            for (int i = 0; i < length; i++) {
                l.Add(null);
            }
            foreach (var e in edges) {
                AddNode(l, e.Item1);
                AddNode(l, e.Item2);
            }
            return l;
        }

        private static void AddNode(List<Node> l, int n) {
            if (l[n] == null)
                l[n] = new Node(CurveFactory.CreateCircle(5, new Microsoft.Msagl.Core.Geometry.Point()));
        }

        private static List<Tuple<ushort, ushort>> ParseGraph(string fileName, List<int> activities) {
            string activityLine;
            string edgeLine = GetEdgeLineAndActivityLine(fileName, out activityLine);
            var edges = ParseEdgeLine(edgeLine);
            FillActivities(activityLine, ref activities);
            return edges;
        }

        private static void FillActivities(string line, ref List<int> activities) {
            char[] delimiterChars = { '[', ']', '(', ')', ',', ' ', '}' };
            string[] words = line.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            try {
                foreach (var w in words) {
                    activities.Add(Int32.Parse(w));
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        private static string GetEdgeLineAndActivityLine(string fileName, out string activityLine) {
            using (System.IO.StreamReader file = new System.IO.StreamReader(fileName)) {
                file.ReadLine(); file.ReadLine(); file.ReadLine();
                string line = file.ReadLine();
                var ret = line.Substring(line.IndexOf('['));
                activityLine = file.ReadLine();
                activityLine = activityLine.Substring(activityLine.IndexOf('['));
                return ret;
            }
        }
        private static string GetEdgeLine(string fileName) {
            using (var file = new StreamReader(fileName)) {
                file.ReadLine(); file.ReadLine(); file.ReadLine();
                string line = file.ReadLine();
                var ret = line.Substring(line.IndexOf('['));                
                return ret;
            }
        }

        class GraphNameCompare : IComparer {

            public int Compare(object x, object y) {
                string a = x as string;
                string b = y as string;
                int l = a.Length - b.Length;
                if (l != 0)
                    return l;
                return String.Compare(a, b);
            }
        }

        private static string[] GetAllGraphsNamesSorted(string dir) {
            string[] files = Directory.GetFiles(dir, "*.json")
                                     .Select(Path.GetFileName)
                                     .ToArray();
            
            Array.Sort(files, new GraphNameCompare());
            for (int i = 0; i < files.Length; i++) {
                files[i] = Path.Combine(dir, files[i]);
            }
            return files;            
        }
        private static void LayoutGraph(GeometryGraph geometryGraph) {
            var settings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();
            settings.EdgeRoutingSettings.EdgeRoutingMode = Microsoft.Msagl.Core.Routing.EdgeRoutingMode.StraightLine;
            LayoutHelpers.CalculateLayout(geometryGraph, settings, null);
            Console.WriteLine("layout done");
        }

        private static List<Tuple<ushort, ushort>> ParseEdgeLine(string line) {
            char[] delimiterChars = { '[', ']', '(', ')', ',', ' ', '}' };
            var list = new List<Tuple<ushort, ushort>>();
            string[] words = line.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length - 1; i += 2) {
                AddEdgeToList(words[i], words[i + 1], list);
            }
            return list;
        }

        private static void AddEdgeToList(string a, string b, List<Tuple<ushort, ushort>> list) {
            list.Add(new Tuple<ushort, ushort>(ushort.Parse(a), ushort.Parse(b)));
        }     
    }
}
