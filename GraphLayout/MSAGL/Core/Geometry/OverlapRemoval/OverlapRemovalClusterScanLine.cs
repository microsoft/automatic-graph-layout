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
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlapRemovalClusterScanLine.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL ScanLine class for Overlap removal constraint generation for Projection solutions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Core.Geometry 
{
    public partial class OverlapRemovalCluster
    {
        private class ScanLine
        {

            // This is the data structure that allows fast insert/remove of nodes as well as
            // scanning next/prev in the perpendicular direction to the scan line movement 
            // (i.e. if this the scan line is moving vertically (top to bottom), then we are
            // on the horizontal separation constraint pass and this.nodeTree orders nodes in the
            // horizontal direction).
            // Note that this is ordered on the midpoint (aka Variable.DesiredPos).  Once
            // again, transitivity saves us; we don't have to worry about combinations of
            // midpoint and sizes that mean that a node with a further midpoint has a closer
            // border than a node with a nearer midpoint, because in that case, the node with
            // the nearer midpoint would have a constraint generated on the node with the
            // further midpoint (though in that case we probably generate a duplicative constraint
            // between the current node and the node with the further midpoint).
            private readonly RbTree<OverlapRemovalNode> nodeTree = new RbTree<OverlapRemovalNode>(new NodeComparer());

            internal void Insert(OverlapRemovalNode node)
            {
                Debug.Assert(null == this.nodeTree.Find(node), "node already exists in the rbtree");

                // RBTree's internal operations on insert/remove etc. mean the node can't cache the
                // RBNode returned by insert(); instead we must do find() on each call.
                this.nodeTree.Insert(node);
            }

            internal void Remove(OverlapRemovalNode node)
            {
                this.nodeTree.Remove(node);
            }

            internal OverlapRemovalNode NextLeft(OverlapRemovalNode node)
            {
                var pred = this.nodeTree.Previous(this.nodeTree.Find(node));
                return (null != pred) ? pred.Item : null;
            }

            internal OverlapRemovalNode NextRight(OverlapRemovalNode node)
            {
                var succ = this.nodeTree.Next(this.nodeTree.Find(node));
                return (null != succ) ? succ.Item : null;
            }
        } // end class ScanLine
    }
} 
