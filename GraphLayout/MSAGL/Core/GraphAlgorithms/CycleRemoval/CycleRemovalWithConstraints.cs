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
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Core.GraphAlgorithms {
    internal class CycleRemovalWithConstraints<TEdge> where TEdge : IEdge {
        BasicGraph<TEdge> graph;
        BasicGraph<IntPair> graphOfConstraints;
        IEnumerable<IntPair> constrainedEdges;
        internal CycleRemovalWithConstraints(BasicGraph<TEdge> graph, Set<IntPair> constraints) {
            this.graph = graph;
            constrainedEdges = constraints;
            graphOfConstraints=new BasicGraph<IntPair>(constrainedEdges, graph.NodeCount);
        }

        internal IEnumerable<IEdge> GetFeedbackSet() {
            foreach (GraphForCycleRemoval graphForCycleRemoval in CreateGraphsForCycleRemoval()) {
                foreach (IEdge edge in GetFeedbackEdgeSet(graphForCycleRemoval))
                    yield return edge;
            }
        }
        /// <summary>
        /// following H.A.D Nascimento and P. Eades "User Hints for Directed Graph Drawing"
        /// </summary>
        /// <param name="graphForCycleRemoval">graphForCycleRemoval is connected</param>
        /// <returns></returns>
        private IEnumerable<IEdge> GetFeedbackEdgeSet(GraphForCycleRemoval graphForCycleRemoval) {
            graphForCycleRemoval.Initialize();
            //empty at the end of the method
            List<int> sl = new List<int>(); //sl - the sequence left part
            List<int> sr = new List<int>(); //sr - the sequence right part. In our case it is a reversed right part
            while (!graphForCycleRemoval.IsEmpty()) {
                int u;
                while (graphForCycleRemoval.TryGetSink(out u)) {
                    graphForCycleRemoval.RemoveNode(u);
                    sr.Add(u);
                }
                while (graphForCycleRemoval.TryGetSource(out u)) {
                    graphForCycleRemoval.RemoveNode(u);
                    sl.Add(u);
                }
                if (graphForCycleRemoval.TryFindVertexWithNoIncomingConstrainedEdgeAndMaximumOutDegreeMinusInDedree(out u)) {
                    graphForCycleRemoval.RemoveNode(u);
                    sl.Add(u);
                }
            }

            Dictionary<int, int> S = new Dictionary<int, int>(sl.Count + sr.Count);
            int j=0;
            foreach (int u in sl)
                S[u] = j++;
            for (int i = sr.Count - 1; i >= 0; i--)
                S[sr[i]] = j++;


            foreach (IntPair pair in graphForCycleRemoval.GetOriginalIntPairs())
                if (S[pair.First] > S[pair.Second])
                    yield return pair;

        }

        private IEnumerable<GraphForCycleRemoval> CreateGraphsForCycleRemoval() {
            foreach (IEnumerable<int> componentNodes in ConnectedComponentCalculator<IntPair>.GetComponents(GetCommonGraph()))
                yield return CreateGraphForCycleRemoval(componentNodes);
        }

        private BasicGraph<IntPair> GetCommonGraph() {
            return new BasicGraph<IntPair>((from edge in graph.Edges select new IntPair(edge.Source, edge.Target)).Concat(constrainedEdges), graph.NodeCount);
        }

        private GraphForCycleRemoval CreateGraphForCycleRemoval(IEnumerable<int> componentNodes) {
            GraphForCycleRemoval graphForCycleRemoval = new GraphForCycleRemoval();
            foreach (int i in componentNodes) {
                foreach (TEdge edge in this.graph.OutEdges(i))
                    graphForCycleRemoval.AddEdge(new IntPair(edge.Source,edge.Target));
                foreach (IntPair intPair in this.graphOfConstraints.OutEdges(i))
                    graphForCycleRemoval.AddConstraintEdge(intPair);
            }
            return graphForCycleRemoval;
        }
    }
}
