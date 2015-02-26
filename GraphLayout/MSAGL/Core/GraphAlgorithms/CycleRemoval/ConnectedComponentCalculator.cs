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
using System.Collections.Generic;

namespace Microsoft.Msagl.Core.GraphAlgorithms {

   
    static internal class ConnectedComponentCalculator<TEdge> where TEdge:IEdge {
        static internal IEnumerable<IEnumerable<int>> GetComponents(BasicGraph<TEdge> graph) {
            bool[] enqueueed = new bool[graph.NodeCount];
#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=367 bool arrays are not default initialised
            for (var i = 0; i < graph.NodeCount; i++)
                enqueueed[i] = false;
#endif
            Queue<int> queue = new Queue<int>();
            for (int i = 0; i < graph.NodeCount; i++) {
                if (!enqueueed[i]) {
                    List<int> nodes = new List<int>();

                    Enqueue(i, queue, enqueueed);

                    while (queue.Count > 0) {
                        int s = queue.Dequeue();
                        nodes.Add(s);
                        foreach (int neighbor in Neighbors(graph, s))
                            Enqueue(neighbor, queue, enqueueed);
                    }

                    yield return nodes;
                }
            }
        }

        static IEnumerable<int> Neighbors(BasicGraph<TEdge> graph, int s) {
            foreach (TEdge e in graph.OutEdges(s))
                yield return e.Target;
            foreach (TEdge e in graph.InEdges(s))
                yield return e.Source;
        }
    
        static void Enqueue(int i, Queue<int> q, bool[] enqueueed) {
            if (enqueueed[i] == false) {
                q.Enqueue(i);
                enqueueed[i] = true;
            }
        }
        
    }
}
