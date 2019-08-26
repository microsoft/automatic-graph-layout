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

namespace ChristopheGraphs {

    class Program {
        static void Main(string[] args) {
            ArgsParser.ArgsParser ap = new ArgsParser.ArgsParser(args);
            ap.AddOptionWithAfterStringWithHelp("ng", "number of graphs to process");
            ap.AddOptionWithAfterStringWithHelp("m", "the graph index multiplier");
            int numberGrapsToWrite; int increment;
            ParseCommandLine(ap, out numberGrapsToWrite, out increment);
                
            var graphList = GetGraphList(@"z:\");
            List<Node> nodes = PositionNodes(graphList);
            int k = 1;
            for (int i = 0; i < increment; i++) {
                string fileName = "z:\\out\\out_graph";
                k *= 2;
                fileName += numberGrapsToWrite.ToString() + "_" + k.ToString() + ".csv";
                WriteGraphs(fileName, graphList, nodes, numberGrapsToWrite, k);
            }
        }

        static void ParseCommandLine(ArgsParser.ArgsParser ap, out int ng, out int increment) {
            ng = 10;
            increment = 6;
            if (ap.Parse() == false) {
                Console.WriteLine("{0}", ap.ErrorMessage);
                return;
            }
            if (ap.OptionIsUsed("ng")) {
                ap.GetIntOptionValue("ng", out ng);
            }

            if (ap.OptionIsUsed("m")) {
                ap.GetIntOptionValue("m", out increment);
            }
        }

        private static void WriteGraphs(string fileName, string[] graphList,
            List<Node> nodes, int numberOfGraphToWrite, int increment) {
            using (System.IO.StreamWriter file =new System.IO.StreamWriter(fileName)) {
                int edgeNumber = 0;
                int k = 1;
                for (int i = 1; k < graphList.Length && i <= numberOfGraphToWrite; i++, k = 1 + (i - 1) * increment) {
                    WriteGraph(file, graphList[k], i, nodes, ref edgeNumber);
                }
            }
        }

        private static void WriteGraph(StreamWriter file, string graphToParse, int z, List<Node> nodes, ref int edgeNumber) {
            List<int> activities = new List<int>();
            var edges = ParseGraph(graphToParse, activities);
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
            int i = 1;
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

        private static List<Node> CreateNodes(List<Tuple<int, int>> edges) {
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

        private static List<Tuple<int, int>> ParseGraph(string fileName, List<int> activities) {
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

        private static string[] GetGraphList(string dir) {
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

        private static List<Tuple<int, int>> ParseEdgeLine(string line) {
            char[] delimiterChars = { '[', ']', '(', ')', ',', ' ', '}' };
            var list = new List<Tuple<int, int>>();
            string[] words = line.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length - 1; i += 2) {
                AddEdgeToList(words[i], words[i + 1], list);
            }
            return list;
        }

        private static void AddEdgeToList(string a, string b, List<Tuple<int, int>> list) {
            list.Add(new Tuple<int, int>(Int32.Parse(a), Int32.Parse(b)));
        }     
    }
}
