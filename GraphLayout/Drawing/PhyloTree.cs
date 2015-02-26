/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
        public override void CreateGeometryGraph() {
            this.GeometryGraph = GeometryGraphCreator.CreatePhyloTree(this);
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
