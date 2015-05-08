using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;

namespace TestGraphmaps {
    internal class GmlParser {
        public static Graph Parse(string fileName) {
            var g = new Graph();
            try
            {
                using (var f = new StreamReader(fileName))
                {
                    do
                    {
                        var str = f.ReadLine();
                        if (str == null)
                            return g;
                        ProcessLine(g, str,f);
                    } while (true);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);            
            }
            return null;
        }

        static void ProcessLine(Graph graph, string str, StreamReader f) {
            if (str.Contains("node"))
                ReadNode(graph, f);
            else if(str.Contains("edge"))
                ReadEdge(graph, f);

        }

        static void ReadEdge(Graph graph, StreamReader f) {
            f.ReadLine();
            var s = f.ReadLine();
            s = s.Trim(' ', '\t', '"');
            var source = s.Split(' ')[1];
            s = f.ReadLine();

            s = s.Trim(' ', '\t', '"');
            var target = s.Split(' ')[1];
            graph.AddEdge(source, target);
            f.ReadLine();
        }

        static void ReadNode(Graph graph, StreamReader f) {
            f.ReadLine();
            var s=f.ReadLine();
            s = s.Trim(' ', '\t', '"');
            var id = s.Split(' ')[1];
            s = f.ReadLine();
            s = s.Trim(' ', '\t', '"');
            var split = s.Split(' ');
            var label = split[1].Trim('"');
            graph.AddNode(id).LabelText = label;
            f.ReadLine();            
        }
    }
}