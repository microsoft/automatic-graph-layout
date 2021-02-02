using System.Collections;
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Core.GraphAlgorithms {

    /// <summary>
    /// The base class for graphs: layering and ordering work on an instance of this class.
    /// </summary>
    internal class BasicGraphOnEdges<TEdge> where TEdge : IEdge {
        protected List<TEdge> edges;
        TEdge[][] inEdges;
        int numberOfVertices;
        TEdge[][] outEdges;
        TEdge[][] selfEdges;

        /// <summary>
        /// a default constructor
        /// </summary>
        internal BasicGraphOnEdges() {
            SetEdges(new List<TEdge>(), 0);
        }

        internal BasicGraphOnEdges(IEnumerable<TEdge> edges) : this(edges, VertexCount(edges)) {}

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="edges"></param>
        /// <param name="numberOfVerts"></param>
        internal BasicGraphOnEdges(IEnumerable<TEdge> edges, int numberOfVerts) {
            SetEdges(edges, numberOfVerts);
        }


        /// <summary>
        /// returning number of vertices of the graph
        /// </summary>
        internal int NodeCount {
            get { return numberOfVertices; }
        }

        /// <summary>
        /// returning all edges of the graph
        /// </summary>
        public ICollection<TEdge> Edges {
            get { return edges; }
        }

        /// <summary>
        /// the method is not efficient, takes linear time
        /// </summary>
        /// <param name="edge"></param>
        internal void RemoveEdge(TEdge edge) {
            edges.Remove(edge);
            if (edge.Source != edge.Target) {
                TEdge[] edgesToChange = outEdges[edge.Source];
                var newEdges = new TEdge[edgesToChange.Length - 1];
                FillArraySkippingTheEdge(edge, edgesToChange, newEdges);
                outEdges[edge.Source] = newEdges;
                edgesToChange = inEdges[edge.Target];
                newEdges = new TEdge[edgesToChange.Length - 1];
                FillArraySkippingTheEdge(edge, edgesToChange, newEdges);
                inEdges[edge.Target] = newEdges;
            }
        }

        static void FillArraySkippingTheEdge(TEdge edge, TEdge[] edgesToChange, TEdge[] newEdges) {
            for (int i = 0, j = 0; i < edgesToChange.Length; i++)
                if ((object) edgesToChange[i] != (object) edge)
                    newEdges[i - j] = edgesToChange[i];
                else
                    j = 1;
        }

        /// <summary>
        /// actually finds maximum of sources and targets+1
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        internal static int VertexCount(IEnumerable edges) {
            int nov = 0;
            foreach (TEdge ie in edges) {
                if (ie.Source >= nov)
                    nov = ie.Source + 1;
                if (ie.Target >= nov)
                    nov = ie.Target + 1;
            }
            return nov;
        }

        /// <summary>
        /// Edges exiting a vertex
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public IList<TEdge> OutEdges(int vertex) {
            return outEdges[vertex];
        }

        internal IList<TEdge> SelfEdges(int vertex) {
            return selfEdges[vertex];
        }

        /// <summary>
        /// Edges entering a vertex
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public IList<TEdge> InEdges(int vertex) {
            return inEdges[vertex];
        }

        /// <summary>
        /// sets edges of the graph
        /// </summary>
        /// <param name="valEdges"></param>
        /// <param name="nov">number of vertices</param>
        internal void SetEdges(IEnumerable<TEdge> valEdges, int nov) {
            edges = valEdges as List<TEdge> ?? new List<TEdge>(valEdges);

            numberOfVertices = nov;
            var outEdgesCounts = new int[numberOfVertices];
            var inEdgesCounts = new int[numberOfVertices];
            var selfEdgesCounts = new int[numberOfVertices];

            outEdges = new TEdge[numberOfVertices][];
            inEdges = new TEdge[numberOfVertices][];
            selfEdges = new TEdge[numberOfVertices][];

            foreach (TEdge e in edges) {
                if (e.Source != e.Target) {
                    outEdgesCounts[e.Source]++;
                    inEdgesCounts[e.Target]++;
                } else {
                    selfEdgesCounts[e.Source]++;
                }
            }

            //allocate now
            for (int i = 0; i < numberOfVertices; i++) {
                outEdges[i] = new TEdge[outEdgesCounts[i]];
                outEdgesCounts[i] = 0; //used later for edge insertion
                inEdges[i] = new TEdge[inEdgesCounts[i]];
                inEdgesCounts[i] = 0; //used later for edge insertion

                selfEdges[i] = new TEdge[selfEdgesCounts[i]];
                selfEdgesCounts[i] = 0; //used later for edge insertion
            }

            //set up backward and forward edges now
            foreach (TEdge e in edges) {
                int u = e.Source;
                int v = e.Target;
                if (u != v) {
                    outEdges[u][outEdgesCounts[u]++] = e;
                    inEdges[v][inEdgesCounts[v]++] = e;
                } else
                    selfEdges[u][selfEdgesCounts[u]++] = e;
            }
        }

        public int InEdgesCount(int node) {
            return InEdges(node).Count;
        }

        public int OutEdgesCount(int node) {
            return OutEdges(node).Count;
        }

        /// <summary>
        /// this function is extremely non-efficient and is called only when adding virtual edges
        /// </summary>
        /// <param name="e"></param>
        internal void AddEdge(TEdge e) {
            Edges.Add(e);
            AddEdgeToInEdges(e, e.Target);
            AddEdgeToOutEdges(e, e.Source);
        }

        /// <summary>
        /// this function is extremely non-efficient and is called only when adding virtual edges
        /// </summary>
        /// <param name="e"></param>
        /// <param name="source"></param>
        void AddEdgeToOutEdges(TEdge e, int source) {
            TEdge[] ies = outEdges[source];
            var nies = new TEdge[ies.Length + 1];
            ies.CopyTo(nies, 1);
            nies[0] = e;
            outEdges[source] = nies;
        }

        /// <summary>
        /// this function is extremely non-efficient and is called only when adding virtual edges
        /// </summary>
        /// <param name="e"></param>
        /// <param name="target"></param>
        void AddEdgeToInEdges(TEdge e, int target) {
            TEdge[] ies = inEdges[target];
            var nies = new TEdge[ies.Length + 1];
            ies.CopyTo(nies, 1);
            nies[0] = e;
            inEdges[target] = nies;
        }

        /// <summary>
        /// We assume that the graph is connected here
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<int> NodesOfConnectedGraph() {
            if (edges.Count > 0) {
                var enqueed = new Set<int>();
                var q = new Queue<int>();
                int i = edges[0].Source;
                Enqueue(enqueed, q, i);
                yield return i;
                while (q.Count > 0) {
                    i = q.Dequeue();
                    foreach (TEdge e in outEdges[i]) {
                        int s = e.Target;
                        if (!enqueed.Contains(s)) {
                            Enqueue(enqueed, q, s);
                            yield return s;
                        }
                    }
                    foreach (TEdge e in inEdges[i]) {
                        int s = e.Source;
                        if (!enqueed.Contains(s)) {
                            Enqueue(enqueed, q, s);
                            yield return s;
                        }
                    }
                }
            }
        }

        static void Enqueue(Set<int> enqueed, Queue<int> q, int i) {
            q.Enqueue(i);
            enqueed.Insert(i);
        }
    }
}
