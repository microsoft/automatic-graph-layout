#region Using directives



#endregion

using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// Layering the DAG by longest path
    /// </summary>
    internal class LongestPathLayering : LayerCalculator {

        BasicGraphOnEdges<IntEdge> graph;

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

        internal LongestPathLayering(BasicGraphOnEdges<IntEdge> graph){
            this.graph=graph;
        }
        
    }
}
