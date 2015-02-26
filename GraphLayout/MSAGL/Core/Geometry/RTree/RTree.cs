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
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Msagl.Core.Geometry {
    /// <summary>
    /// A search tree for rapid lookup of TData objects keyed by rectangles inside a given rectangular region
    /// It is very similar to "R-TREES. A DYNAMIC INDEX STRUCTURE FOR SPATIAL SEARCHING" by Antonin Guttman
    /// </summary>
    public class RTree<TData> {
        /// <summary>
        /// 
        /// </summary>
        public RectangleNode<TData> RootNode
        {
            get { return rootNode; }
            set { rootNode=value; }
        }

        RectangleNode<TData> rootNode;
       

        /// <summary>
        /// Create the query tree for a given enumerable of TData keyed by Rectangles
        /// </summary>
        /// <param name="rectsAndData"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public RTree(IEnumerable<KeyValuePair<Rectangle, TData>> rectsAndData) {
            rootNode = RectangleNode<TData>.CreateRectangleNodeOnEnumeration(GetNodeRects(rectsAndData));
        }

        /// <summary>
        /// Create a query tree for a given root node
        /// </summary>
        /// <param name="rootNode"></param>
        public RTree(RectangleNode<TData> rootNode) {
            this.rootNode = rootNode;
        }

        ///<summary>
        ///</summary>
        public RTree() {
            
        }

        /// <summary>
        /// The number of data elements in the tree (number of leaf nodes)
        /// </summary>
        public int Count {
            get { return rootNode == null ? 0 : rootNode.Count; }
        }

     
        /// <summary>
        /// Add the given key, value pair
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(Rectangle key, TData value) {
            Add(new RectangleNode<TData>(value, key));            
        }

        internal void Add(RectangleNode<TData> node) {

            if (rootNode == null)
                rootNode = node;
            else if (Count <= 2)
                rootNode = RectangleNode<TData>.CreateRectangleNodeOnEnumeration(rootNode.GetAllLeafNodes().Concat(new[] {node}));
            else
                AddNodeToTreeRecursive(node, rootNode);
        }
        /// <summary>
        /// rebuild the whole tree
        /// </summary>
        public void Rebuild() {
            rootNode = RectangleNode<TData>.CreateRectangleNodeOnEnumeration(rootNode.GetAllLeafNodes());
        }

        static IEnumerable<RectangleNode<TData>> GetNodeRects(IEnumerable<KeyValuePair<Rectangle, TData>> nodes) {
            return nodes.Select(v => new RectangleNode<TData>(v.Value, v.Key));
        }

        static void AddNodeToTreeRecursive(RectangleNode<TData> newNode, RectangleNode<TData> existingNode) {
            if (existingNode.IsLeaf) {
                existingNode.Left = new RectangleNode<TData>(existingNode.UserData, existingNode.Rectangle);
                existingNode.Right = newNode;
                existingNode.Count = 2;
                existingNode.UserData = default(TData);                
            } else {
                existingNode.Count++;
                Rectangle leftBox;
                Rectangle rightBox;
                if (2 * existingNode.Left.Count < existingNode.Right.Count) {
                    //keep the balance
                    AddNodeToTreeRecursive(newNode, existingNode.Left);
                    existingNode.Left.Rectangle = new Rectangle(existingNode.Left.Rectangle, newNode.Rectangle);
                } else if (2 * existingNode.Right.Count < existingNode.Left.Count) {
                    //keep the balance
                    AddNodeToTreeRecursive(newNode, existingNode.Right);
                    existingNode.Right.Rectangle = new Rectangle(existingNode.Right.Rectangle, newNode.Rectangle);
                } else { //decide basing on the boxes
                    leftBox = new Rectangle(existingNode.Left.Rectangle, newNode.Rectangle);
                    var delLeft = leftBox.Area - existingNode.Left.Rectangle.Area;
                    rightBox = new Rectangle(existingNode.Right.Rectangle, newNode.Rectangle);
                    var delRight = rightBox.Area - existingNode.Right.Rectangle.Area;
                    if (delLeft < delRight) {
                        AddNodeToTreeRecursive(newNode, existingNode.Left);
                        existingNode.Left.Rectangle = leftBox;
                    } else if(delLeft>delRight){
                        AddNodeToTreeRecursive(newNode, existingNode.Right);
                        existingNode.Right.Rectangle = rightBox;
                    } else { //the deltas are the same; add to the smallest
                        if(leftBox.Area<rightBox.Area) {
                            AddNodeToTreeRecursive(newNode, existingNode.Left);
                            existingNode.Left.Rectangle = leftBox;
                        }else {
                            AddNodeToTreeRecursive(newNode, existingNode.Right);
                            existingNode.Right.Rectangle = rightBox;
                        }
                    }
                }
            }
            existingNode.Rectangle = new Rectangle(existingNode.Left.Rectangle, existingNode.Right.Rectangle);
        }


        /// <summary>
        /// return all the data elements stored at the leaves of the BSPTree in an IEnumerable
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerable<TData> GetAllLeaves() {
            return rootNode!=null && Count>0 ? rootNode.GetAllLeaves():new TData[0];
        }

        /// <summary>
        /// Get all data items with rectangles intersecting the specified rectangular region
        /// </summary>
        /// <param name="queryRegion"></param>
        /// <returns></returns>
        public IEnumerable<TData> GetAllIntersecting(Rectangle queryRegion)
        {
            return rootNode == null || Count == 0 ? new TData[0] : rootNode.GetNodeItemsIntersectingRectangle(queryRegion);
        }


        /// <summary>
        /// Get all leaf nodes with rectangles intersecting the specified rectangular region
        /// </summary>
        /// <param name="queryRegion"></param>
        /// <returns></returns>
        internal IEnumerable<RectangleNode<TData>> GetAllLeavesIntersectingRectangle(Rectangle queryRegion) {
            return rootNode == null || Count == 0 ? new RectangleNode<TData>[0] : rootNode.GetLeafRectangleNodesIntersectingRectangle(queryRegion);
        }

        /// <summary>
        /// Does minimal work to determine if any objects in the tree intersect with the query region
        /// </summary>
        /// <param name="queryRegion"></param>
        /// <returns></returns>
        public bool IsIntersecting(Rectangle queryRegion) {
            return GetAllIntersecting(queryRegion).Any();
        }

        /// <summary>
        /// return true iff there is a node with the rectangle and UserData that equals to the parameter "userData"
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public bool Contains(Rectangle rectangle, TData userData) {
            if (rootNode == null) return false;
            return
                rootNode.GetLeafRectangleNodesIntersectingRectangle(rectangle)
                        .Any(node => node.UserData.Equals(userData));
        }

        ///<summary>
        ///</summary>
        ///<param name="rectangle"></param>
        ///<param name="userData"></param>
        ///<returns></returns>
        public TData Remove(Rectangle rectangle, TData userData) {
            if (rootNode==null)
            {
                return default(TData);
            }
            var ret = rootNode.GetLeafRectangleNodesIntersectingRectangle(rectangle).FirstOrDefault(node => node.UserData.Equals(userData));
            if (ret == null)
                return default(TData);
            if (RootNode.Count == 1)
                RootNode = null;
            else
                RemoveLeaf(ret);
            return ret.UserData;
        }

        void RemoveLeaf(RectangleNode<TData> leaf) {
            Debug.Assert(leaf.IsLeaf);
            
            var unbalancedNode = FindTopUnbalancedNode(leaf);
            if (unbalancedNode != null) {
                RebuildUnderNodeWithoutLeaf(unbalancedNode, leaf);
                UpdateParent(unbalancedNode);
            } else {
                //replace the parent with the sibling and update bounding boxes and counts
                var parent = leaf.Parent;
                if (parent == null) {
                    Debug.Assert(rootNode == leaf);
                    rootNode = new RectangleNode<TData>();
                } else {
                    TransferFromSibling(parent, leaf.IsLeftChild ? parent.Right : parent.Left);
                    UpdateParent(parent);
                }
            }
           Debug.Assert(TreeIsCorrect(RootNode));
        }

        static bool TreeIsCorrect(RectangleNode<TData> node)
        {
            if (node == null)
                return true;
            bool ret= node.Left != null && node.Right != null  ||
                   node.Left == null && node.Right == null;
            if (!ret)
                return false;
            return TreeIsCorrect(node.Left) && TreeIsCorrect(node.Right);
        }

        static void UpdateParent(RectangleNode<TData> parent) {
            for(var node=parent.Parent; node!=null; node=node.Parent) {
                node.Count--;
                node.Rectangle=new Rectangle(node.Left.Rectangle, node.Right.Rectangle);
            }
        } 

        static void TransferFromSibling(RectangleNode<TData> parent, RectangleNode<TData> sibling) {
            parent.UserData=sibling.UserData;
            parent.Left = sibling.Left;
            parent.Right=sibling.Right;
            parent.Count--;
            parent.Rectangle = sibling.Rectangle;
        }

        static void RebuildUnderNodeWithoutLeaf(RectangleNode<TData> nodeForRebuild, RectangleNode<TData> leaf)
        {
            Debug.Assert(leaf.IsLeaf);
            Debug.Assert(!nodeForRebuild.IsLeaf);
            var newNode =
                RectangleNode<TData>.CreateRectangleNodeOnEnumeration(
                    nodeForRebuild.GetAllLeafNodes().Where(n => !(n.Equals(leaf))));
            nodeForRebuild.Count = newNode.Count;
            nodeForRebuild.Left = newNode.Left;
            nodeForRebuild.Right = newNode.Right;
            nodeForRebuild.Rectangle = new Rectangle(newNode.Left.rectangle, newNode.Right.rectangle);
        }

        static RectangleNode<TData> FindTopUnbalancedNode(RectangleNode<TData> node) {
            for (var parent = node.Parent; parent != null; parent = parent.Parent)
                if (! Balanced(parent))
                    return parent;
            return null;
        }

        static bool Balanced(RectangleNode<TData> rectangleNode) {
            return 2*rectangleNode.Left.Count >= rectangleNode.Right.Count &&
                   2*rectangleNode.Right.Count >= rectangleNode.Left.Count;
        }
        /// <summary>
        /// Removes everything from the tree
        /// </summary>
        public void Clean()
        {
            RootNode = null;
        }
    }

}
