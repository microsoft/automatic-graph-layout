using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Core.GraphAlgorithms {
    internal class CycleRemovalWithConstraints<TEdge> where TEdge : IEdge {
        BasicGraphOnEdges<TEdge> graph;
        BasicGraphOnEdges<IntPair> graphOfConstraints;
        IEnumerable<IntPair> constrainedEdges;
        internal CycleRemovalWithConstraints(BasicGraphOnEdges<TEdge> graph, Set<IntPair> constraints) {
            this.graph = graph;
            constrainedEdges = constraints;
            graphOfConstraints=new BasicGraphOnEdges<IntPair>(constrainedEdges, graph.NodeCount);
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
         IEnumerable<IEdge> GetFeedbackEdgeSet(GraphForCycleRemoval graphForCycleRemoval) {
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

         IEnumerable<GraphForCycleRemoval> CreateGraphsForCycleRemoval() {
            foreach (IEnumerable<int> componentNodes in ConnectedComponentCalculator<IntPair>.GetComponents(GetCommonGraph()))
                yield return CreateGraphForCycleRemoval(componentNodes);
        }

         BasicGraphOnEdges<IntPair> GetCommonGraph() {
            return new BasicGraphOnEdges<IntPair>((from edge in graph.Edges select new IntPair(edge.Source, edge.Target)).Concat(constrainedEdges), graph.NodeCount);
        }

         GraphForCycleRemoval CreateGraphForCycleRemoval(IEnumerable<int> componentNodes) {
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
