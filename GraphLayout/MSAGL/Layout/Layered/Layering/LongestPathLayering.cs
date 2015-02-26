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
#region Using directives



#endregion

using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// Layering the DAG by longest path
    /// </summary>
    internal class LongestPathLayering : LayerCalculator {

        BasicGraph<IntEdge> graph;

        public int[] GetLayers() {
            //sort the vertices in topological order
            int[] topoOrder = IntEdge.GetOrder(graph);
            int[] layering = new int[graph.NodeCount];

            //going backward from leaves
            int k = graph.NodeCount;
            while (k-- > 0) {
                int v = topoOrder[k];
                foreach (IntEdge e in graph.InEdges(v)) {
                    int u = e.Source;
                    int l = layering[v] + e.Separation;
                    if (layering[u] < l)
                        layering[u] = l;
                }
            }
            return layering;

        }

        internal LongestPathLayering(BasicGraph<IntEdge> graph){
            this.graph=graph;
        }
        
    }
}
