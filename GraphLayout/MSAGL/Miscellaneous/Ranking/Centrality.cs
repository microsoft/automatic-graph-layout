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
