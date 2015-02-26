/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;

namespace TestWpfViewer {
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