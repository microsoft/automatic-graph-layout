using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Core.GraphAlgorithms {
    /// <summary>
    /// Calculates a set of edges to reverse, so called "feedback set", for obtaining a DAG
    /// </summary>
    static internal class CycleRemoval<TEdge> where TEdge : IEdge {
        
        /// <summary>
        /// Returning a list of edges reversing which makes the graph into a DAG
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="constraints"></param>
        /// <returns></returns>
        static internal IEnumerable<IEdge> GetFeedbackSetWithConstraints(BasicGraphOnEdges<TEdge> graph, Set<IntPair> constraints) {
            if (constraints == null || constraints.Count == 0) {
                return GetFeedbackSet(graph);
            } else
                return GetFeedbackSetWithConstraintsLocal(graph, constraints);
        }

        static IEnumerable<IEdge> GetFeedbackSetWithConstraintsLocal(BasicGraphOnEdges<TEdge> graph, Set<IntPair> constraints) {
            var v = new CycleRemovalWithConstraints<TEdge>(graph, constraints);
            return v.GetFeedbackSet();
        }


        static void Push(Stack<IEnumerator<TEdge>> enumStack, Stack<int> vertexStack, VertStatus[] status, int vertex, IEnumerator<TEdge> outEnum) {
            status[vertex] = VertStatus.InStack;
            enumStack.Push(outEnum);
            vertexStack.Push(vertex);
        }

        static void Pop(Stack<IEnumerator<TEdge>> enumStack, Stack<int> vertexStack, VertStatus[] status, out int vertex, out IEnumerator<TEdge> outEnum) {
            outEnum = enumStack.Pop();
            vertex = vertexStack.Pop();
            status[vertex] = VertStatus.Visited;
        }

        enum VertStatus {
            NotVisited,
            InStack,
            Visited,
        }

        /// <summary>
        /// We build a spanning tree by following the DFS, the tree induces an order on vertices 
        /// measured by the distance from the tree root. The feedback set will consist of edges 
        /// directed against this order.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        static internal IEnumerable<IEdge> GetFeedbackSet(BasicGraphOnEdges<TEdge> graph) {
            if (graph != null && graph.NodeCount > 0 && graph.Edges.Count > 0) {
                Set<IEdge> feedbackSet = new Set<IEdge>();
                VertStatus[] status = new VertStatus[graph.NodeCount]; //will be Unvisited at the beginning
#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=367 arrays are not default initialised
                for (int i = 0; i < status.Length; i++)
                    status[i] = VertStatus.NotVisited;
#endif
                for (int vertex = 0; vertex < graph.NodeCount; vertex++) {
                    if (status[vertex] == VertStatus.Visited)
                        continue;

                    System.Diagnostics.Debug.Assert(status[vertex] != VertStatus.InStack);

                    Stack<IEnumerator<TEdge>> enumStack = new Stack<IEnumerator<TEdge>>(); //avoiding the recursion
                    Stack<int> vertexStack = new Stack<int>(); //avoiding the recursion
                    IEnumerator<TEdge> outEnum = graph.OutEdges(vertex).GetEnumerator();
                    Push(enumStack, vertexStack, status, vertex, outEnum);
                    while (enumStack.Count > 0) {
                        Pop(enumStack, vertexStack, status, out vertex, out outEnum);

                        while (outEnum.MoveNext()) {
                            TEdge e = outEnum.Current;

                            if (e.Source == e.Target)
                                continue;

                            VertStatus targetStatus = status[e.Target];
                            if (targetStatus == VertStatus.InStack) {
                                feedbackSet.Insert(e);
                            } else if (targetStatus == VertStatus.NotVisited) {				//have to go deeper
                                Push(enumStack, vertexStack, status, vertex, outEnum);
                                vertex = e.Target;
                                status[e.Target] = VertStatus.Visited;
                                outEnum = graph.OutEdges(vertex).GetEnumerator();
                            }
                        }
                    }
                }
                return feedbackSet as IEnumerable<IEdge>;
            } else
                return new Set<IEdge>();
        }
    }
}
