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
using System.Linq;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;

namespace TestWpfViewer {
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