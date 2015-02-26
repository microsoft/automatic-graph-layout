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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dot2Graph;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval;
using Microsoft.Msagl.Drawing;

namespace FindOverlapSample {
    class DotLoader {

        public static GeometryGraph LoadFile(String fileName) {
            int line, column;
            string msg;
            Graph gwgraph = Parser.Parse(fileName, out line, out column, out msg);
//            TestGraph(gwgraph);
            if (gwgraph != null) {
                return gwgraph.GeometryGraph;
            } else
                MessageBox.Show(msg + String.Format(" line {0} column {1}", line, column));
            return null;
        }
        public static Graph LoadGraphFile(String fileName) {
            int line, column;
            string msg;
            Graph gwgraph = Parser.Parse(fileName, out line, out column, out msg);
            //            TestGraph(gwgraph);
            if (gwgraph != null) {
                return gwgraph;
            } else
                MessageBox.Show(msg + String.Format(" line {0} column {1}", line, column));
            return null;
        }
    }
}
