using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;

namespace TestGraphmaps {
    /// <summary>
    /// parsers
    /// </summary>
    internal class NetParser {
        public static Graph Parse(string fileName, out int line, out int column, out string msg) {
            var g = new Graph();
            line = 0;
            column = 0;
            msg = "";
            try {
                using (var f = new StreamReader(fileName)) {
                    do {
                        var str = f.ReadLine();
                        if (str == null)
                            return g;
                        ProcessLine(g, str);
                    } while (true);
                }
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
                msg = e.Message;
            }
            return null;
        }

        static void ProcessLine(Graph graph, string str) {
            if (String.IsNullOrEmpty(str))
                return;
            if (str[0] == '*') return;

            var arrayStr = str.Split(new []{' '},StringSplitOptions.RemoveEmptyEntries).ToArray();
            if (arrayStr.Length == 0)
                return;
            var source = graph.AddNode(arrayStr[0]);
            for (int i = 1; i < arrayStr.Length; i++) {
                var e = new Edge(source, graph.AddNode(arrayStr[i]), ConnectionToGraph.Connected);
                graph.AddPrecalculatedEdge(e);
            }
        }
    }
    }