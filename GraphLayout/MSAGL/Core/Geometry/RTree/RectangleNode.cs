#region Using directives

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Diagnostics;

#endregion

namespace Microsoft.Msagl.Core.Geometry {
    /// <summary>
    /// Represents a node containing a box and some user data.
    /// Is used in curve intersections routines.
    /// </summary>
    public class RectangleNode<TData> {
#if TEST_MSAGL
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        public override string ToString() {
            return IsLeaf ? (Count + " " + UserData) : Count.ToString();
        }
#endif
        /// <summary>
        /// 
        /// </summary>
        public int Count { get; set; }
        RectangleNode<TData> left;

        RectangleNode<TData> right;

        /// <summary>
        /// creates an empty node
        /// </summary>
        public RectangleNode() {            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="rect"></param>
        public RectangleNode(TData data, Rectangle rect) {
            UserData = data;
            Rectangle = rect;
            Count = 1;
        }

        RectangleNode(int count) {
            Count=count;
        }

        /// <summary>
        /// This field provides direct internal access to the value type Rectangle, which RTree and other callers
        /// modify directly with .Add(); the auto-property returns a temporary value-by-copy that is immediately discarded.
        /// </summary>
// ReSharper disable InconsistentNaming
        internal Rectangle rectangle;
// ReSharper restore InconsistentNaming

        /// <summary>
        /// gets or sets the rectangle of the node
        /// </summary>
        public Rectangle Rectangle {
            get { return rectangle; }
            set { rectangle = value; }
        }

        /// <summary>
        /// false if it is an internal node and true if it is a leaf
        /// </summary>
        internal bool IsLeaf
        {
            get { return left==null /*&& right==null*/; } //if left is a null then right is also a null
        }
        /// <summary>
        /// 
        /// </summary>
        public RectangleNode<TData> Left {
            get { return left; }
            internal set {
                if (left != null && left.Parent==this)
                    left.Parent = null;
                left = value;
                if (left != null)
                    left.Parent = this;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public RectangleNode<TData> Right {
            get { return right; }
            internal set {
                if (right != null&& right.Parent==this)
                    right.Parent = null;
                right = value;
                if (right != null)
                    right.Parent = this;
            }
        }

        /// <summary>
        /// The actual data if a leaf node, else null or a value-type default.
        /// </summary>
        public TData UserData { get; set; }

        /// <summary>
        /// Parent of this node.
        /// </summary>
        public RectangleNode<TData> Parent { get; private set; }

        internal bool IsLeftChild {
            get {
                Debug.Assert(Parent!=null);
                return Equals(Parent.Left);
            }
        }

        /// <summary>
        /// brings the first leaf which rectangle was hit and the delegate is happy with the object
        /// </summary>
        /// <param name="point"></param>
        /// <param name="hitTestForPointDelegate"></param>
        /// <returns></returns>
        public RectangleNode<TData> FirstHitNode(Point point, Func<Point, TData, HitTestBehavior> hitTestForPointDelegate) {
            if (rectangle.Contains(point)) {
                if (IsLeaf) {
                    if (hitTestForPointDelegate != null) {
                        return hitTestForPointDelegate(point, UserData) == HitTestBehavior.Stop ? this : null;
                    }
                    return this;
                }
                return Left.FirstHitNode(point, hitTestForPointDelegate) ??
                        Right.FirstHitNode(point, hitTestForPointDelegate);
            }
            return null;
        }


        /// <summary>
        /// brings the first leaf which rectangle was intersected
        /// </summary>
        /// <returns></returns>
        public RectangleNode<TData> FirstIntersectedNode(Rectangle r) {
            if (r.Intersects(rectangle)) {
                if (IsLeaf)
                    return this;
                return Left.FirstIntersectedNode(r) ?? Right.FirstIntersectedNode(r);
            }
            return null;
        }



        /// <summary>
        /// brings the first leaf which rectangle was hit and the delegate is happy with the object
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public RectangleNode<TData> FirstHitNode(Point point) {
            if (rectangle.Contains(point)) {
                if (IsLeaf)                    
                    return this;                
                return Left.FirstHitNode(point) ?? Right.FirstHitNode(point);
            }
            return null;
        }


        /// <summary>
        /// returns all leaf nodes for which the rectangle was hit and the delegate is happy with the object
        /// </summary>
        /// <param name="rectanglePar"></param>
        /// <param name="hitTestAccept"></param>
        /// <returns></returns>
        public IEnumerable<TData> AllHitItems(Rectangle rectanglePar, Func<TData, bool> hitTestAccept) {
            var stack = new Stack<RectangleNode<TData>>();
            stack.Push(this);
            while (stack.Count > 0) {
                RectangleNode<TData> node = stack.Pop();
                if (node.Rectangle.Intersects(rectanglePar)) {
                    if (node.IsLeaf) {
                        if ((null == hitTestAccept) || hitTestAccept(node.UserData)) {
                            yield return node.UserData;
                        }
                    }
                    else {
                        stack.Push(node.left);
                        stack.Push(node.right);
                    }
                }
            }
        }

        /// <summary>
        /// returns all items for which the rectangle contains the point
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TData> AllHitItems(Point point) {
            var stack = new Stack<RectangleNode<TData>>();
            stack.Push(this);
            while (stack.Count > 0) {
                var node = stack.Pop();
                if (node.Rectangle.Contains(point)) {
                    if (node.IsLeaf)
                        yield return node.UserData;
                    else {
                        stack.Push(node.left);
                        stack.Push(node.right);
                    }
                }
            }
        }


        /// <summary>
        /// Returns all leaves whose rectangles intersect hitRectangle (or all leaves before hitTest returns false).
        /// </summary>
        /// <param name="hitTest"></param>
        /// <param name="hitRectangle"></param>
        /// <returns></returns>
        internal void VisitTree(Func<TData, HitTestBehavior> hitTest, Rectangle hitRectangle) {
            VisitTreeStatic(this, hitTest, hitRectangle);
        }

        static HitTestBehavior VisitTreeStatic(RectangleNode<TData> rectangleNode, Func<TData, HitTestBehavior> hitTest, Rectangle hitRectangle) {
            if (rectangleNode.Rectangle.Intersects(hitRectangle)) {
                if (hitTest(rectangleNode.UserData) == HitTestBehavior.Continue) {
                    if (rectangleNode.Left != null) {
                        // If rectangleNode.Left is not null, rectangleNode.Right won't be either.
                        if (VisitTreeStatic(rectangleNode.Left, hitTest, hitRectangle) == HitTestBehavior.Continue &&
                            VisitTreeStatic(rectangleNode.Right, hitTest, hitRectangle) == HitTestBehavior.Continue) {
                            return HitTestBehavior.Continue;
                        }
                        return HitTestBehavior.Stop;
                    }
                    return HitTestBehavior.Continue;
                }
                return HitTestBehavior.Stop;
            }
            return HitTestBehavior.Continue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public RectangleNode<TData> Clone() {
            var ret = new RectangleNode<TData>(Count) {UserData = UserData, Rectangle = Rectangle};
            if (Left != null)
                ret.Left = Left.Clone();
            if (Right != null)
                ret.Right = Right.Clone();
            return ret;
        }

        /// <summary>
        /// yields all leaves which rectangles intersect the given one. We suppose that leaves are all nodes having UserData not a null.
        /// </summary>
        /// <param name="rectanglePar"></param>
        /// <returns></returns>
        public IEnumerable<TData> GetNodeItemsIntersectingRectangle(Rectangle rectanglePar) {
            return GetLeafRectangleNodesIntersectingRectangle(rectanglePar).Select(node => node.UserData);
        }

        /// <summary>
        /// yields all leaves whose rectangles intersect the given one. We suppose that leaves are all nodes having UserData not a null.
        /// </summary>
        /// <param name="rectanglePar"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public IEnumerable<RectangleNode<TData>> GetLeafRectangleNodesIntersectingRectangle(Rectangle rectanglePar) {
            var stack = new Stack<RectangleNode<TData>>();
            stack.Push(this);
            while (stack.Count > 0) {
                RectangleNode<TData> node = stack.Pop();
                if (node.Rectangle.Intersects(rectanglePar)) {
                    if (node.IsLeaf) {
                        yield return node;
                    } else {
                        stack.Push(node.left);
                        stack.Push(node.right);
                    }
                }
            }
        }

        /// <summary>
        /// Walk the tree and return the data from all leaves
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerable<TData> GetAllLeaves() {
            return GetAllLeafNodes().Select(n => n.UserData);
        }

        internal IEnumerable<RectangleNode<TData>> GetAllLeafNodes() {
            return EnumRectangleNodes(true /*leafOnly*/);
        }

        internal IEnumerable<RectangleNode<TData>> GetAllNodes() {
            return EnumRectangleNodes(false /*leafOnly*/);
        }

        IEnumerable<RectangleNode<TData>> EnumRectangleNodes(bool leafOnly) {
            var stack = new Stack<RectangleNode<TData>>();
            stack.Push(this);
            while (stack.Count > 0) {
                var node = stack.Pop();
                if (node.IsLeaf || !leafOnly) {
                    yield return node;
                }
                if (!node.IsLeaf) {
                    stack.Push(node.left);
                    stack.Push(node.right);
                }
            }
        }

        const int GroupSplitThreshold = 2;

        
        /// <summary>
        /// calculates a tree based on the given nodes
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes"), SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static RectangleNode<TData> CreateRectangleNodeOnEnumeration(IEnumerable<RectangleNode<TData>> nodes) {
            if(nodes==null)
                return null;
            var nodeList = new List<RectangleNode<TData>>(nodes);
            return CreateRectangleNodeOnListOfNodes(nodeList);
        }

        ///<summary>
        ///calculates a tree based on the given nodes
        ///</summary>
        ///<param name="dataEnumeration"></param>
        ///<param name="rectangleDelegate"></param>
        ///<returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static RectangleNode<TData> CreateRectangleNodeOnData(IEnumerable<TData>  dataEnumeration, Func<TData, Rectangle> rectangleDelegate) {
            if (dataEnumeration == null || rectangleDelegate == null)
                return null;
            var nodeList = new List<RectangleNode<TData>>(dataEnumeration.Select(d=>new RectangleNode<TData>(d, rectangleDelegate(d))));
            return CreateRectangleNodeOnListOfNodes(nodeList);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes"), SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        static public RectangleNode<TData> CreateRectangleNodeOnListOfNodes(IList<RectangleNode<TData>> nodes) {
            ValidateArg.IsNotNull(nodes, "nodes");
            if (nodes.Count == 0) return null;

            if (nodes.Count == 1)return nodes[0];

            //Finding the seeds
            var b0 = nodes[0].Rectangle;

            //the first seed
            int seed0 = 1;

            int seed1 = ChooseSeeds(nodes, ref b0, ref seed0);

            //We have two seeds at hand. Build two groups.
            var gr0 = new List<RectangleNode<TData>>();
            var gr1 = new List<RectangleNode<TData>>();

            gr0.Add(nodes[seed0]);
            gr1.Add(nodes[seed1]);

            var box0 = nodes[seed0].Rectangle;
            var box1 = nodes[seed1].Rectangle;
            //divide nodes on two groups
            DivideNodes(nodes, seed0, seed1, gr0, gr1, ref box0, ref box1, GroupSplitThreshold);

            var ret = new RectangleNode<TData>(nodes.Count) {
                    Rectangle = new Rectangle(box0, box1),
                    Left = CreateRectangleNodeOnListOfNodes(gr0),
                    Right = CreateRectangleNodeOnListOfNodes(gr1)
            };

            return ret;

        }

        static int ChooseSeeds(IList<RectangleNode<TData>> nodes, ref Rectangle b0, ref int seed0) {
            double area = new Rectangle(b0, nodes[seed0].Rectangle).Area;
            for (int i = 2; i < nodes.Count; i++) {
                double area0 = new Rectangle(b0, nodes[i].Rectangle).Area;
                if (area0 > area) {
                    seed0 = i;
                    area = area0;
                }
            }

            //Got the first seed seed0
            //Now looking for a seed for the second group
            int seed1 = 0; //the compiler forces me to init it

            //init seed1
            for (int i = 0; i < nodes.Count; i++) {
                if (i != seed0) {
                    seed1 = i;
                    break;
                }
            }

            area = new Rectangle(nodes[seed0].Rectangle, nodes[seed1].Rectangle).Area;
            //Now try to improve the second seed

            for (int i = 0; i < nodes.Count; i++) {
                if (i == seed0)
                    continue;
                double area1 = new Rectangle(nodes[seed0].Rectangle, nodes[i].Rectangle).Area;
                if (area1 > area) {
                    seed1 = i;
                    area = area1;
                }
            }
            return seed1;
        }

        static void DivideNodes(IList<RectangleNode<TData>> nodes, int seed0, int seed1, List<RectangleNode<TData>> gr0, List<RectangleNode<TData>> gr1, 
            ref Rectangle box0, ref Rectangle box1, int groupSplitThreshold) {
            for (int i = 0; i < nodes.Count; i++) {

                if (i == seed0 || i == seed1)
                    continue;

// ReSharper disable InconsistentNaming
                var box0_ = new Rectangle(box0, nodes[i].Rectangle);
                double delta0 = box0_.Area - box0.Area;

                var box1_ = new Rectangle(box1, nodes[i].Rectangle);
                double delta1 = box1_.Area - box1.Area;
// ReSharper restore InconsistentNaming

                //keep the tree roughly balanced

                if (gr0.Count * groupSplitThreshold < gr1.Count) {
                    gr0.Add(nodes[i]);
                    box0 = box0_;
                } else if (gr1.Count * groupSplitThreshold < gr0.Count) {
                    gr1.Add(nodes[i]);
                    box1 = box1_;
                } else if (delta0 < delta1) {
                    gr0.Add(nodes[i]);
                    box0 = box0_;
                } else if (delta1 < delta0) {
                    gr1.Add(nodes[i]);
                    box1 = box1_;
                } else if (box0.Area < box1.Area) {
                    gr0.Add(nodes[i]);
                    box0 = box0_;
                } else {
                    gr1.Add(nodes[i]);
                    box1 = box1_;
                }
            }
        }
       
        

        /// <summary>
        /// Walk the tree from node down and apply visitor to all nodes
        /// </summary>
        /// <param name="node"></param>
        /// <param name="visitor"></param>
        [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes"), SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        static public void TraverseHierarchy(RectangleNode<TData> node, Action<RectangleNode<TData>> visitor) {
            ValidateArg.IsNotNull(node, "node");
            ValidateArg.IsNotNull(visitor, "visitor");
            visitor(node);
            if (node.Left != null)
                TraverseHierarchy(node.Left, visitor);
            if (node.Right != null)
                TraverseHierarchy(node.Right, visitor);
        }
    }
}
