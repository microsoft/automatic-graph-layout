using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// represents a phylogenetic tree: a tree with edges of specific length
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Phylogenetic")]
    public class PhyloTree:Graph {
        /// <summary>
        /// creates the geometry graph corresponding to the tree
        /// </summary>
        public override Core.Layout.GeometryGraph CreateGeometryGraph() {
            return this.GeometryGraph = GeometryGraphCreator.CreatePhyloTree(this);
        }

        /// <summary>
        /// adds an edge to the tree
        /// </summary>
        /// <param name="source"></param>
        /// <param name="edgeLabel"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        override public Edge AddEdge(string source, string edgeLabel, string target) {

            string l = edgeLabel;
            if (l == null)
                l = "";


            PhyloEdge edge = new PhyloEdge(source, target, 1) {
                                                                  SourceNode = AddNode(source),
                                                                  TargetNode = AddNode(target)
                                                              };

            if (source != target) {
                edge.SourceNode.AddOutEdge(edge);
                edge.TargetNode.AddInEdge(edge);
            } else
                edge.SourceNode.AddSelfEdge(edge);



            return edge;
        }

        /// <summary>
        /// adds an edge to the tree
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public override Edge AddEdge(string source, string target) {

            return AddEdge(source, null, target);
        }
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Node> Leaves {
            get {
                foreach (Node node in this.Nodes)
                    if (IsLeaf(node))
                        yield return node;
            }
        }

        /// <summary>
        /// true if the node is a leaf
        /// </summary>
        static public bool IsLeaf(Node node) {
            return !node.OutEdges.GetEnumerator().MoveNext();
        }
    }
}
