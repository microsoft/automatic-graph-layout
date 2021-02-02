using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Msagl.Core.GraphAlgorithms{
    /// <summary>
    /// Implements the topological sort
    /// </summary>
    public static class TopologicalSort{
        /// <summary>
        /// Do a topological sort of a list of int edge tuples
        /// </summary>
        /// <param name="numberOfVertices">number of vertices</param>
        /// <param name="edges">edge pairs</param>
        /// <returns>ordered node indexes</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static int[] GetOrder(int numberOfVertices, IEnumerable<System.Tuple<int,int>> edges)
        {
            var dag = new BasicGraphOnEdges<IntPair>(from e in edges select new IntPair(e.Item1, e.Item2), numberOfVertices);
            return GetOrder(dag);
        }

        /// <summary>
        /// The function returns an array arr such that
        /// arr is a permutation of the graph vertices,
        /// and for any edge e in graph if e.Source=arr[i]
        /// e.Target=arr[j], then i is less than j
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        internal static int[] GetOrder<TEdge>(BasicGraphOnEdges<TEdge> graph) where TEdge : IEdge{
            var visited = new bool[graph.NodeCount];

            //no recursion! So we have to organize a stack
            var sv = new Stack<int>();
            var se = new Stack<IEnumerator<int>>();

            var order = new List<int>();

            IEnumerator<int> en;
            for (int u = 0; u < graph.NodeCount; u++){
                if (visited[u])
                    continue;

                int cu = u;
                visited[cu] = true;
                en = graph.OutEdges(u).Select(e => e.Target).GetEnumerator();

                do{
                    while (en.MoveNext()){
                        int v = en.Current;
                        if (!visited[v]){
                            visited[v] = true;
                            sv.Push(cu);
                            se.Push(en);
                            cu = v;
                            en = graph.OutEdges(cu).Select(e => e.Target).GetEnumerator();
                        }
                    }
                    order.Add(cu);


                    if (sv.Count > 0){
                        en = se.Pop();
                        cu = sv.Pop();
                    }
                    else
                        break;
                } while (true);
            }
            order.Reverse();
            return order.ToArray();
        }

        /// <summary>
        /// The function returns an array arr such that
        /// arr is a permutation of edge  vertices,
        /// and for any edge e from the list, if e.Source=arr[i]
        /// e.Target=arr[j], then i is less than j
        /// </summary>
        /// <returns></returns>
        internal static int[] GetOrderOnEdges<TEdge>(IEnumerable<TEdge> edges) where TEdge : IEdge {
            var visited = new Dictionary<int,bool>();
            var graph = new Dictionary<int, List<int>>();
            foreach (var e in edges) {
                visited[e.Source]=visited[e.Target]=false;
                var x=e.Source;
                List<int> list;
                if(! graph.TryGetValue(x, out list)) {
                   graph[x]=list=new List<int>();
                }
                list.Add(e.Target);
            }
            //organize a couple of stacks to avoid the recursion
            var sv = new Stack<int>();
            var se = new Stack<IEnumerator<int>>();

            var order = new List<int>();

            IEnumerator<int> en;
            foreach (var  u in visited.Keys.ToArray()) {
                if (visited[u])
                    continue;

                int cu = u;

                visited[cu] = true;
                List<int> glist;
                if (!graph.TryGetValue(u, out glist)) continue;
                if (glist == null) continue;
                en = glist.GetEnumerator();
                do {
                    while (en.MoveNext()) {
                        int v = en.Current;
                        if (!visited[v]) {
                            visited[v] = true;
                            List<int> list;
                            if (!graph.TryGetValue(v, out list)) {
                                order.Add(v);//it is a leaf
                                continue; 
                            }
                            sv.Push(cu);
                            se.Push(en);
                            cu = v;
                            en = list.GetEnumerator();
                        }
                    }
                    order.Add(cu);

                    if (sv.Count > 0) {
                        en = se.Pop();
                        cu = sv.Pop();
                    } else
                        break;
                } while (true);
            }
            order.Reverse();
            return order.ToArray();
        }
    }
}