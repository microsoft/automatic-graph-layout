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
using System.Diagnostics;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Visibility {
    [DebuggerDisplay("({Point.X} {Point.Y})")]
    internal class VisibilityVertex : IComparer<VisibilityEdge> {

        // This member is accessed a lot.  Using a field instead of a property for performance.
        readonly internal Point Point;

        readonly List<VisibilityEdge> inEdges=new List<VisibilityEdge>();

        internal List<VisibilityEdge> InEdges {
            get { return inEdges; }
        }

        readonly RbTree<VisibilityEdge> outEdges;
       /* VisibilityEdge prev; */

        /// <summary>
        /// this collection is sorted by the target point, in the lexicographical order
        /// </summary>
        internal RbTree<VisibilityEdge> OutEdges {
            get { return outEdges; }
        }

        internal int Degree {
            get { return InEdges.Count+OutEdges.Count; }            
        }
        /// <summary>
        /// needed for shortest path calculations
        /// </summary>
        internal double Distance { get;set;}

/*
        /// <summary>
        /// needed for shortest path calculations
        /// </summary>        
        internal VisibilityVertex Prev {
            get {
                if (prev == null) return null;
                if(prev.Source==this)
                    return prev.Target;
                return prev.Source;
            }
        }

        internal void SetPreviousEdge(VisibilityEdge e) {
            prev = e;
        }
        */
        internal VisibilityVertex(Point point) {
            outEdges = new RbTree<VisibilityEdge>(this);
            Point = point;
        }

        /// <summary>
        /// Rounded representation; DebuggerDisplay shows the unrounded form.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return Point.ToString();
        }

        /// <summary>
        /// These iterate from the end of the list because List.Remove is linear in
        /// the number of items, so callers have been optimized where possible to
        /// remove only the last or next-to-last edges (but in some cases such as
        /// rectilinear, this optimization isn't always possible).
        /// </summary>
        /// <param name="edge"></param>
        internal void RemoveOutEdge(VisibilityEdge edge) {
            OutEdges.Remove(edge);          
        }

        internal void RemoveInEdge(VisibilityEdge edge) {
            for (int ii = InEdges.Count - 1; ii >= 0; --ii) {
                if (InEdges[ii] == edge) {
                    InEdges.RemoveAt(ii);
                    break;
                }
            }
        }
        /// <summary>
        /// avoiding using delegates in calling RBTree.FindFirst because of the memory allocations
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="targetPoint"></param>
        /// <returns></returns>
        static RBNode<VisibilityEdge> FindFirst(RbTree<VisibilityEdge> tree, Point targetPoint) {
            return FindFirst(tree.Root, tree, targetPoint);
        }

        static RBNode<VisibilityEdge> FindFirst(RBNode<VisibilityEdge> n, RbTree<VisibilityEdge> tree, Point targetPoint)
        {
            if ( n ==  tree.Nil)
                return null;
            RBNode<VisibilityEdge> good = null;
            while (n != tree.Nil)
                n = n.Item.TargetPoint >= targetPoint ? (good = n).left : n.right;

            return good;
        }

        internal bool TryGetEdge(VisibilityVertex target, out VisibilityEdge visEdge) {
            var node = FindFirst(OutEdges, target.Point);// OutEdges.FindFirst(e => e.TargetPoint >= target.Point); 
            if (node != null) {
                if (node.Item.Target == target) {
                    visEdge = node.Item;
                    return true;
                }
            }
            node = FindFirst(target.OutEdges, Point);// target.OutEdges.FindFirst(e => e.TargetPoint >= Point);
            if (node != null) {
                if (node.Item.Target == this) {
                    visEdge= node.Item;
                    return true;
                }
            }
            visEdge = null;
            return false;
        }

        #region IComparer<VisibilityEdge>
        public int Compare(VisibilityEdge a, VisibilityEdge b) {
            ValidateArg.IsNotNull(a, "a");
            ValidateArg.IsNotNull(b, "b");
            return a.TargetPoint.CompareTo(b.TargetPoint);
        }
        #endregion // IComparer<VisibilityEdge>

    }
}
