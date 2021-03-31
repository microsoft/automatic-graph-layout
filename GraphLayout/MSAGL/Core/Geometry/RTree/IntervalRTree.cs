using System; 
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Layout.LargeGraphLayout;

namespace Microsoft.Msagl.Core.Geometry {
    /// <summary>
    /// A search tree for rapid lookup of TData objects keyed by rectangles inside a given rectangular region
    /// It is very similar to "R-TREES. A DYNAMIC INDEX STRUCTURE FOR SPATIAL SEARCHING" by Antonin Guttman
    /// </summary>
    public class IntervalRTree<TData> {
        /// <summary>
        /// 
        /// </summary>
        public IntervalNode<TData> RootNode
        {
            get { return rootNode; }
            set { rootNode=value; }
        }

        IntervalNode<TData> rootNode;
       

        /// <summary>
        /// Create the query tree for a given enumerable of TData keyed by Intervals
        /// </summary>
        /// <param name="rectsAndData"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public IntervalRTree(IEnumerable<KeyValuePair<Interval, TData>> rectsAndData) {
            rootNode = IntervalNode<TData>.CreateIntervalNodeOnEnumeration(GetNodeRects(rectsAndData));
        }

        /// <summary>
        /// Create a query tree for a given root node
        /// </summary>
        /// <param name="rootNode"></param>
        public IntervalRTree(IntervalNode<TData> rootNode) {
            this.rootNode = rootNode;
        }

        ///<summary>
        ///</summary>
        public IntervalRTree() {
            
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
        public void Add(Interval key, TData value) {
            Add(new IntervalNode<TData>(value, key));            
        }

        internal void Add(IntervalNode<TData> node) {

            if (rootNode == null)
                rootNode = node;
            else if (Count <= 2)
                rootNode = IntervalNode<TData>.CreateIntervalNodeOnEnumeration(rootNode.GetAllLeafNodes().Concat(new[] {node}));
            else
                AddNodeToTreeRecursive(node, rootNode);
        }
        /// <summary>
        /// rebuild the whole tree
        /// </summary>
        public void Rebuild() {
            rootNode = IntervalNode<TData>.CreateIntervalNodeOnEnumeration(rootNode.GetAllLeafNodes());
        }

        static IEnumerable<IntervalNode<TData>> GetNodeRects(IEnumerable<KeyValuePair<Interval, TData>> nodes) {
            return nodes.Select(v => new IntervalNode<TData>(v.Value, v.Key));
        }

        static void AddNodeToTreeRecursive(IntervalNode<TData> newNode, IntervalNode<TData> existingNode) {
            if (existingNode.IsLeaf) {
                existingNode.Left = new IntervalNode<TData>(existingNode.UserData, existingNode.Interval);
                existingNode.Right = newNode;
                existingNode.Count = 2;
                existingNode.UserData = default(TData);                
            } else {
                existingNode.Count++;
                Interval leftBox;
                Interval rightBox;
                if (2 * existingNode.Left.Count < existingNode.Right.Count) {
                    //keep the balance
                    AddNodeToTreeRecursive(newNode, existingNode.Left);
                    existingNode.Left.Interval = new Interval(existingNode.Left.Interval, newNode.Interval);
                } else if (2 * existingNode.Right.Count < existingNode.Left.Count) {
                    //keep the balance
                    AddNodeToTreeRecursive(newNode, existingNode.Right);
                    existingNode.Right.Interval = new Interval(existingNode.Right.Interval, newNode.Interval);
                } else { //decide basing on the boxes
                    leftBox = new Interval(existingNode.Left.Interval, newNode.Interval);
                    var delLeft = leftBox.Area - existingNode.Left.Interval.Area;
                    rightBox = new Interval(existingNode.Right.Interval, newNode.Interval);
                    var delRight = rightBox.Area - existingNode.Right.Interval.Area;
                    if (delLeft < delRight) {
                        AddNodeToTreeRecursive(newNode, existingNode.Left);
                        existingNode.Left.Interval = leftBox;
                    } else if(delLeft>delRight){
                        AddNodeToTreeRecursive(newNode, existingNode.Right);
                        existingNode.Right.Interval = rightBox;
                    } else { //the deltas are the same; add to the smallest
                        if(leftBox.Area<rightBox.Area) {
                            AddNodeToTreeRecursive(newNode, existingNode.Left);
                            existingNode.Left.Interval = leftBox;
                        }else {
                            AddNodeToTreeRecursive(newNode, existingNode.Right);
                            existingNode.Right.Interval = rightBox;
                        }
                    }
                }
            }
            existingNode.Interval = new Interval(existingNode.Left.Interval, existingNode.Right.Interval);
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
        public IEnumerable<TData> GetAllIntersecting(Interval queryRegion)
        {
            return rootNode == null || Count == 0 ? new TData[0] : rootNode.GetNodeItemsIntersectingInterval(queryRegion);
        }

        /// <summary>
        /// Does minimal work to determine if any objects in the tree intersect with the query region
        /// </summary>
        /// <param name="queryRegion"></param>
        /// <returns></returns>
        public bool IsIntersecting(Interval queryRegion) {
            return GetAllIntersecting(queryRegion).Any();
        }

        /// <summary>
        /// return true iff there is a node with the rectangle and UserData that equals to the parameter "userData"
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public bool Contains(Interval rectangle, TData userData) {
            if (rootNode == null) return false;
            return
                rootNode.GetLeafIntervalNodesIntersectingInterval(rectangle)
                        .Any(node => node.UserData.Equals(userData));
        }

        ///<summary>
        ///</summary>
        ///<param name="rectangle"></param>
        ///<param name="userData"></param>
        ///<returns></returns>
        public TData Remove(Interval rectangle, TData userData) {
            if (rootNode==null)
            {
                return default(TData);
            }
            var ret = rootNode.GetLeafIntervalNodesIntersectingInterval(rectangle).FirstOrDefault(node => node.UserData.Equals(userData));
            if (ret == null)
                return default(TData);
            if (RootNode.Count == 1)
                RootNode = null;
            else
                RemoveLeaf(ret);
            return ret.UserData;
        }

        void RemoveLeaf(IntervalNode<TData> leaf) {
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
                    rootNode = new IntervalNode<TData>();
                } else {
                    TransferFromSibling(parent, leaf.IsLeftChild ? parent.Right : parent.Left);
                    UpdateParent(parent);
                }
            }
           Debug.Assert(TreeIsCorrect(RootNode));
        }

        static bool TreeIsCorrect(IntervalNode<TData> node)
        {
            if (node == null)
                return true;
            bool ret= node.Left != null && node.Right != null  ||
                   node.Left == null && node.Right == null;
            if (!ret)
                return false;
            return TreeIsCorrect(node.Left) && TreeIsCorrect(node.Right);
        }

        static void UpdateParent(IntervalNode<TData> parent) {
            for(var node=parent.Parent; node!=null; node=node.Parent) {
                node.Count--;
                node.Interval=new Interval(node.Left.Interval, node.Right.Interval);
            }
        } 

        static void TransferFromSibling(IntervalNode<TData> parent, IntervalNode<TData> sibling) {
            parent.UserData=sibling.UserData;
            parent.Left = sibling.Left;
            parent.Right=sibling.Right;
            parent.Count--;
            parent.Interval = sibling.Interval;
        }

        static void RebuildUnderNodeWithoutLeaf(IntervalNode<TData> nodeForRebuild, IntervalNode<TData> leaf)
        {
            Debug.Assert(leaf.IsLeaf);
            Debug.Assert(!nodeForRebuild.IsLeaf);
            var newNode =
                IntervalNode<TData>.CreateIntervalNodeOnEnumeration(
                    nodeForRebuild.GetAllLeafNodes().Where(n => !(n.Equals(leaf))));
            nodeForRebuild.Count = newNode.Count;
            nodeForRebuild.Left = newNode.Left;
            nodeForRebuild.Right = newNode.Right;
            nodeForRebuild.Interval = new Interval(newNode.Left.interval, newNode.Right.interval);
        }

        static IntervalNode<TData> FindTopUnbalancedNode(IntervalNode<TData> node) {
            for (var parent = node.Parent; parent != null; parent = parent.Parent)
                if (! Balanced(parent))
                    return parent;
            return null;
        }

        static bool Balanced(IntervalNode<TData> rectangleNode) {
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
