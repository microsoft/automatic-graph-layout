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
