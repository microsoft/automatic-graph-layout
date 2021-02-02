using System.Collections.Generic;

namespace Microsoft.Msagl.Core.GraphAlgorithms {

   
    static internal class ConnectedComponentCalculator<TEdge> where TEdge:IEdge {
        static internal IEnumerable<IEnumerable<int>> GetComponents(BasicGraphOnEdges<TEdge> graph) {
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

        static IEnumerable<int> Neighbors(BasicGraphOnEdges<TEdge> graph, int s) {
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
