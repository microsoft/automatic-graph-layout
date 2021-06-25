using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.MDS {
    /// <summary>
    /// Provides functionality for computing distances in a graph.
    /// </summary>
    public class SingleSourceDistances : AlgorithmBase {
        private GeometryGraph graph;

        private Node source;

        
        /// <summary>
        /// Dijkstra algorithm. Computes graph-theoretic distances from a node to
        /// all other nodes in a graph with nonnegative edge lengths.
        /// The distance between a node and itself is 0; the distance between a pair of
        /// nodes for which no connecting path exists is Double.PositiveInfinity.
        /// </summary>
        /// <param name="graph">A graph.</param>
        /// <param name="source">The source node.</param>
        /// <param name="directed">Whether the graph is directed.</param>
        public SingleSourceDistances(GeometryGraph graph, Node source)
        {
            this.graph = graph;
            this.source = source;
        }

        /// <summary>
        /// An array of distances from the source node to all nodes.
        /// Nodes are indexed in their natural order when iterating over them.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "This is performance critical.  Copying the array would be slow.")]
        public double[] Result { get; private set; }

        /// <summary>
        /// Executes the algorithm.
        /// </summary>
        protected override void RunInternal() {
            this.StartListenToLocalProgress(graph.Nodes.Count);

            Result = new double[graph.Nodes.Count];

            var q = new Microsoft.Msagl.Core.DataStructures.GenericBinaryHeapPriorityQueue<Node>();
            Dictionary<Node, double> d = new Dictionary<Node, double>();
            foreach (Node node in graph.Nodes) {
                q.Enqueue(node, Double.PositiveInfinity);
                d[node] = Double.PositiveInfinity;
            }
            q.DecreasePriority(source, 0);

            while (q.Count>0) {
                
                ProgressStep();

                double prio;
                Node u = q.Dequeue(out prio);
                d[u] = prio;
                IEnumerator<Edge> enumerator;
                
                    enumerator = u.Edges.GetEnumerator();
                while (enumerator.MoveNext()) {
                    Edge uv = enumerator.Current;
                    Node v = uv.Target;
                    if (u == v)
                        v = uv.Source;
                    // relaxation step
                    if (d[v] > d[u] + uv.Length) {
                        d[v] = d[u] + uv.Length;
                        q.DecreasePriority(v, d[v]);
                    }
                }
            }
            int i = 0;
            foreach (Node v in graph.Nodes) {
#if SHARPKIT //https://github.com/SharpKit/SharpKit/issues/7 out keyword not working with arrays
                double dummy;
                if (!d.TryGetValue(v, out dummy))
                    dummy = Double.PositiveInfinity;
                Result[i] = dummy;
#else
                if (!d.TryGetValue(v, out Result[i]))
                    Result[i] = Double.PositiveInfinity;
#endif
                i++;
            }
        }
    }
}
