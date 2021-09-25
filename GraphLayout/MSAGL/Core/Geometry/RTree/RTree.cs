using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Core.Geometry {
    /// <summary>
    /// A search tree for rapid lookup of TData objects keyed by rectangles inside a given rectangular region
    /// It is very similar to "R-TREES. A DYNAMIC INDEX STRUCTURE FOR SPATIAL SEARCHING" by Antonin Guttman
    /// </summary>
    public class RTree<T,P>  {
        /// <summary>
        /// 
        /// </summary>
        public RectangleNode<T, P> RootNode
        {
            get { return _rootNode; }
            set { _rootNode=value; }
        }

        RectangleNode<T,P> _rootNode;
       

        /// <summary>
        /// Create the query tree for a given enumerable of TData keyed by Rectangles
        /// </summary>
        /// <param name="rectsAndData"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public RTree(IEnumerable<KeyValuePair<IRectangle<P>, T>> rectsAndData) {
            _rootNode = RectangleNode<T, P>.CreateRectangleNodeOnEnumeration(GetNodeRects(rectsAndData));
        }

        /// <summary>
        /// Create a query tree for a given root node
        /// </summary>
        /// <param name="rootNode"></param>
        public RTree(RectangleNode<T, P> rootNode) {
            this._rootNode = rootNode;
        }

        ///<summary>
        ///</summary>
        public RTree() {
            
        }

        /// <summary>
        /// The number of data elements in the tree (number of leaf nodes)
        /// </summary>
        public int Count {
            get { return _rootNode == null ? 0 : _rootNode.Count; }
        }

     
        /// <summary>
        /// Add the given key, value pair
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(IRectangle<P> key, T value) {
            Add(new RectangleNode<T, P>(value, key));            
        }

        internal void Add(RectangleNode<T, P> node) {
            if (_rootNode == null)
                _rootNode = node;
            else if (Count <= 2)
                _rootNode = RectangleNode<T, P>.CreateRectangleNodeOnEnumeration(_rootNode.GetAllLeafNodes().Concat(new[] {node}));
            else
                AddNodeToTreeRecursive(node, _rootNode);
        }
        /// <summary>
        /// rebuild the whole tree
        /// </summary>
        public void Rebuild() {
            _rootNode = RectangleNode<T, P>.CreateRectangleNodeOnEnumeration(_rootNode.GetAllLeafNodes());
        }

        static IEnumerable<RectangleNode<T, P>> GetNodeRects(IEnumerable<KeyValuePair<IRectangle<P>, T>> nodes) {
            return nodes.Select(v => new RectangleNode<T, P>(v.Value, v.Key));
        }

        static void AddNodeToTreeRecursive(RectangleNode<T, P> newNode, RectangleNode<T, P> existingNode) {
            if (existingNode.IsLeaf) {
                existingNode.Left = new RectangleNode<T, P>(existingNode.UserData, existingNode.Rectangle);
                existingNode.Right = newNode;
                existingNode.Count = 2;
                existingNode.UserData = default(T);                
            } else {
                existingNode.Count++;
                IRectangle<P> leftBox;
                IRectangle<P> rightBox;
                if (2 * existingNode.Left.Count < existingNode.Right.Count) {
                    //keep the balance
                    AddNodeToTreeRecursive(newNode, existingNode.Left);
                    existingNode.Left.Rectangle = existingNode.Left.Rectangle.Unite( newNode.Rectangle);
                } else if (2 * existingNode.Right.Count < existingNode.Left.Count) {
                    //keep the balance
                    AddNodeToTreeRecursive(newNode, existingNode.Right);
                    existingNode.Right.Rectangle = existingNode.Right.Rectangle.Unite( newNode.Rectangle);
                } else { //decide basing on the boxes
                    leftBox = existingNode.Left.Rectangle.Unite( newNode.Rectangle);
                    var delLeft = leftBox.Area - existingNode.Left.Rectangle.Area;
                    rightBox = existingNode.Right.Rectangle.Unite(newNode.Rectangle);
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
            existingNode.Rectangle = existingNode.Left.Rectangle.Unite( existingNode.Right.Rectangle);
        }


        /// <summary>
        /// return all the data elements stored at the leaves of the BSPTree in an IEnumerable
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerable<T> GetAllLeaves() {
            return _rootNode!=null && Count>0 ? _rootNode.GetAllLeaves():new T[0];
        }

        /// <summary>
        /// Get all data items with rectangles intersecting the specified rectangular region
        /// </summary>
        /// <param name="queryRegion"></param>
        /// <returns></returns>
        public T[] GetAllIntersecting(IRectangle<P> queryRegion)
        {
            return _rootNode == null || Count == 0 ? new T[0] : _rootNode.GetNodeItemsIntersectingRectangle(queryRegion).ToArray();
        }

        public bool OneIntersecting(IRectangle<P> queryRegion, out T intersectedLeaf) {
            if (_rootNode == null || Count == 0) {
                intersectedLeaf = default(T);
                return false;
            }
            RectangleNode<T, P> ret = _rootNode.FirstIntersectedNode(queryRegion);
            if (ret == null) {
                intersectedLeaf = default(T);
                return false;
            }
            intersectedLeaf = ret.UserData;
            return true;
        }

        /// <summary>
        /// Get all leaf nodes with rectangles intersecting the specified rectangular region
        /// </summary>
        /// <param name="queryRegion"></param>
        /// <returns></returns>
        internal IEnumerable<RectangleNode<T, P>> GetAllLeavesIntersectingRectangle(IRectangle<P> queryRegion) {
            return _rootNode == null || Count == 0 ? new RectangleNode<T, P>[0] : _rootNode.GetLeafRectangleNodesIntersectingRectangle(queryRegion);
        }

        /// <summary>
        /// Does minimal work to determine if any objects in the tree intersect with the query region
        /// </summary>
        /// <param name="queryRegion"></param>
        /// <returns></returns>
        public bool IsIntersecting(IRectangle<P> queryRegion) {
            return GetAllIntersecting(queryRegion).Any();
        }

        /// <summary>
        /// return true iff there is a node with the rectangle and UserData that equals to the parameter "userData"
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public bool Contains(IRectangle<P> rectangle, T userData) {
            if (_rootNode == null) return false;
            return
                _rootNode.GetLeafRectangleNodesIntersectingRectangle(rectangle)
                        .Any(node => node.UserData.Equals(userData));
        }

        ///<summary>
        ///</summary>
        ///<param name="rectangle"></param>
        ///<param name="userData"></param>
        ///<returns></returns>
        public T Remove(IRectangle<P> rectangle, T userData) {
            if (_rootNode==null)
            {
                return default(T);
            }
            var ret = _rootNode.GetLeafRectangleNodesIntersectingRectangle(rectangle).FirstOrDefault(node => node.UserData.Equals(userData));
            if (ret == null)
                return default(T);
            if (RootNode.Count == 1)
                RootNode = null;
            else
                RemoveLeaf(ret);
            return ret.UserData;
        }

        void RemoveLeaf(RectangleNode<T, P> leaf) {
            Debug.Assert(leaf.IsLeaf);
            
            var unbalancedNode = FindTopUnbalancedNode(leaf);
            if (unbalancedNode != null) {
                RebuildUnderNodeWithoutLeaf(unbalancedNode, leaf);
                UpdateParent(unbalancedNode);
            } else {
                //replace the parent with the sibling and update bounding boxes and counts
                var parent = leaf.Parent;
                if (parent == null) {
                    Debug.Assert(_rootNode == leaf);
                    _rootNode = new RectangleNode<T, P>();
                } else {
                    TransferFromSibling(parent, leaf.IsLeftChild ? parent.Right : parent.Left);
                    UpdateParent(parent);
                }
            }
            Debug.Assert(TreeIsCorrect(RootNode));
        }

        static internal bool TreeIsCorrect(RectangleNode<T, P> node) {
            if (node == null)
                return true;
            bool ret = node.Left != null && node.Right != null ||
                   node.Left == null && node.Right == null;
            if (!ret)
                return false;
            if (node.Left != null && node.Left.Parent != node)
                return false;
            if (node.Right != null && node.Right.Parent != node)
                return false;

            return TreeIsCorrect(node.Left) && TreeIsCorrect(node.Right);
        }

        static void UpdateParent(RectangleNode<T, P> parent) {
            for(var node=parent.Parent; node!=null; node=node.Parent) {
                node.Count--;
                node.Rectangle=node.Left.Rectangle.Unite( node.Right.Rectangle);
            }
        } 

        static void TransferFromSibling(RectangleNode<T, P> parent, RectangleNode<T, P> sibling) {
            parent.UserData=sibling.UserData;
            parent.Left = sibling.Left;
            parent.Right=sibling.Right;
            parent.Count--;
            parent.Rectangle = sibling.Rectangle;
        }

        static void RebuildUnderNodeWithoutLeaf(RectangleNode<T, P> nodeForRebuild, RectangleNode<T, P> leaf)
        {
            Debug.Assert(leaf.IsLeaf);
            Debug.Assert(!nodeForRebuild.IsLeaf);
            var newNode =
                RectangleNode<T, P>.CreateRectangleNodeOnEnumeration(
                    nodeForRebuild.GetAllLeafNodes().Where(n => !(n.Equals(leaf))));
            nodeForRebuild.Count = newNode.Count;
            nodeForRebuild.Left = newNode.Left;
            nodeForRebuild.Right = newNode.Right;
            nodeForRebuild.Rectangle = newNode.Left.rectangle.Unite(newNode.Right.rectangle);
            Debug.Assert(TreeIsCorrect(nodeForRebuild));
        }

        static RectangleNode<T, P> FindTopUnbalancedNode(RectangleNode<T, P> node) {
            for (var parent = node.Parent; parent != null; parent = parent.Parent)
                if (! Balanced(parent))
                    return parent;
            return null;
        }

        static bool Balanced(RectangleNode<T, P> rectangleNode) {
            return 2*rectangleNode.Left.Count >= rectangleNode.Right.Count &&
                   2*rectangleNode.Right.Count >= rectangleNode.Left.Count;
        }

        /// <summary>
        /// Removes everything from the tree
        /// </summary>
        public void Clear() {
            RootNode = null;
        }

        public bool NumberOfIntersectedIsLessThanBound(IRectangle<P> rect, int bound, Func<T, bool> conditionFunc ) {
            return NumberOfIntersectedIsLessThanBoundOnNode(_rootNode, rect, ref bound, conditionFunc);
        }

        static bool NumberOfIntersectedIsLessThanBoundOnNode(RectangleNode<T, P> node, IRectangle<P> rect, ref int bound, Func<T, bool> conditionFunc) {
            Debug.Assert(bound > 0);
            if (!node.Rectangle.Intersects(rect)) return true;
            if (node.IsLeaf) {
                if (conditionFunc(node.UserData))
                    return (--bound) != 0;
                return true;
            }

            return NumberOfIntersectedIsLessThanBoundOnNode(node.Left, rect, ref bound, conditionFunc) &&
                   NumberOfIntersectedIsLessThanBoundOnNode(node.Right, rect, ref bound, conditionFunc);

        }
    }

}
