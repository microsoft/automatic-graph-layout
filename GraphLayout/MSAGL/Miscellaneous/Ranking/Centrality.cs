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
using System.Linq;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Prototype.Ranking {
    /// <summary>
    /// Class for centrality computation.
    /// </summary>
    sealed internal class Centrality {
        private Centrality() { } //to avoid creating the public constructor
        /// <summary>
        /// Computes the PageRank in a directed graph.
        /// </summary>
        /// <param name="graph">A directed graph.</param>
        /// <param name="omega">Probability of a jump
        /// (with uniform probability) to some other node.</param>
        /// <param name="inverse">false=PageRank, true=TrustRank.</param>
        /// <returns>PageRank scores.</returns>
        public static double[] PageRank(GeometryGraph graph, double omega, bool inverse) {
            Dictionary<Node, double> p=new Dictionary<Node,double>();
            int n = graph.Nodes.Count;
            foreach(Node v in graph.Nodes) {
                p[v] = 1d / (graph.Nodes.Count);
            }
            for(int c=0; c<50; c++) {
                Dictionary<Node, double> q = new Dictionary<Node, double>();
                foreach (Node v in graph.Nodes) {
                    q[v] = (1 - omega) / (graph.Nodes.Count);
                }
                if(inverse) { // backward propagation
                    foreach(Node v in graph.Nodes) {
                        foreach(Edge edge in v.OutEdges) {
                            Node u=edge.Target;
                            q[v]+=omega*p[u]/u.InEdges.Count();
                        }
                    }
                } else { // forward propagation
                    foreach(Node v in graph.Nodes) {
                        foreach(Edge edge in v.InEdges) {
                            Node u=edge.Source;
                            q[v]+=omega*p[u]/u.OutEdges.Count();
                        }
                    }
                }
                p=q;
            }
            double[] result=new double[n];
            int counter=0;
            foreach(Node v in graph.Nodes) {
                result[counter]=p[v];
                counter++;
            }
            return result;
        }
    }
}
