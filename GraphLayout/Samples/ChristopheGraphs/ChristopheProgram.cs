using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Miscellaneous;

namespace ChristopheGraphs {
    class Program {
        static void Main(string[] args) {
            var graphList = GetGraphList(@"z:\");
            List<Node> nodes = PositionNodes(graphList);
            WriteGraphs(@"c:\\tmp\out_graph.txt", graphList, nodes);
        }

        private static void WriteGraphs(string fileName, string[] graphList, List<Node> nodes) {
            using (System.IO.StreamWriter file =new System.IO.StreamWriter(fileName, true)) {
                for (int i = 1; i < graphList.Length; i++) {
                    WriteGraph(file, graphList[i], i, nodes);
                }
            }
        }

        private static void WriteGraph(StreamWriter file, string graphToParse, int z, List<Node> nodes) {
            var edges = ParseGraph(graphToParse);
            int edgeNumber = 0;
            foreach(var e in edges) {
                Node s = nodes[e.Item1];
                Node t = nodes[e.Item2];
                file.WriteLine("{0},{1},{2},{3},{4}", ++edgeNumber, s.Center.X, s.Center.Y, z, edgeNumber) ;
                file.WriteLine("{0},{1},{2},{3},{4}", edgeNumber, t.Center.X, t.Center.Y, z, edgeNumber);                
            }
        }

        private static List<Node> PositionNodes(string[] graphList) {
            //int i = graphList.Length - 1; // take the last graph
            int i = 1; 
            var edges = ParseGraph(graphList[i]);
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

        private static List<Tuple<int, int>> ParseGraph(string fileName) {
            string edgeLine = GetEdgeLine(fileName);
            return ParseEdgeLine(edgeLine);
        }


        private static string GetEdgeLine(string fileName) {
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            file.ReadLine(); file.ReadLine(); file.ReadLine();
            string line = file.ReadLine();
            return line.Substring(line.IndexOf('['));
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



        private static void WriteGraphToFile(GeometryGraph geometryGraph, string fileName, bool round) {
            using (System.IO.StreamWriter file =
           new System.IO.StreamWriter(fileName, true)) {
                int edgeNumber = 0;
                foreach (var e in geometryGraph.Edges) {
                    edgeNumber++;
                    if (round) {
                        file.WriteLine("{0},{1},{2},0,{3}", edgeNumber, (int)(e.Source.Center.X + 0.5), (int)(e.Source.Center.Y + 0.5), edgeNumber);
                        file.WriteLine("{0},{1},{2},0,{3}", edgeNumber, (int)(e.Target.Center.X + 0.5), (int)(e.Target.Center.Y + 0.5), edgeNumber);
                    }
                    else {
                        file.WriteLine("{0},{1},{2},0,{3}", edgeNumber, e.Source.Center.X, e.Source.Center.Y, edgeNumber);
                        file.WriteLine("{0},{1},{2},0,{3}", edgeNumber, e.Target.Center.X, e.Target.Center.Y, edgeNumber);
                    }
                }
            }
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

        
        private static void ReserveEnoughNodes(List<Node> nodes, int i) {
            if (i + 1 > nodes.Count) {
                nodes.AddRange(Enumerable.Repeat<Node>(null, 2 * (i + 1)));
            }
        }
    }
}
