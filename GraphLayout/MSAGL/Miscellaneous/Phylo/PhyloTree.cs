using System.Collections.Generic;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Prototype.Phylo {
    /// <summary>
    /// Implements a phylogenetic tree
    /// </summary>
    public class PhyloTree : GeometryGraph {
        Node root;
        //used in the other solution
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Node Root {
            get { return root != null ? root : root = GetRoot(); }
        }

        private Node GetRoot() {
            Node ret = null;
            foreach (Node n in this.Nodes) {
                ret = n; break;
            }

            Node oldVal;
            do {
                oldVal = ret;
                foreach (Edge e in ret.InEdges)
                    ret = e.Source;
            } while (oldVal != ret);
     
            return ret;
        }
        /// <summary>
        /// the leaves of the tree
        /// </summary>
        public IEnumerable<Node> Leaves {
            get {
                foreach (Node node in this.Nodes)
                    if (NodeIsALeaf(node))
                        yield return node;
            }
        }

        static private bool NodeIsALeaf(Node node) {
            return !node.OutEdges.GetEnumerator().MoveNext();
        }
    }
}
