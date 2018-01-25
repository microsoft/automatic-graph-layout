#region Using directives

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Diagnostics;
using Microsoft.Msagl.Layout.LargeGraphLayout;

#endregion

namespace Microsoft.Msagl.Core.Geometry {
  /// <summary>
  /// Represents a node containing a box and some user data.
  /// Is used in curve intersections routines.
  /// </summary>
  public class IntervalNode<TData> {
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
    IntervalNode<TData> left;

    IntervalNode<TData> right;

    /// <summary>
    /// creates an empty node
    /// </summary>
    public IntervalNode() {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="rect"></param>
    public IntervalNode(TData data, Interval rect) {
      UserData = data;
      Interval = rect;
      Count = 1;
    }

    IntervalNode(int count) {
      Count = count;
    }

    /// <summary>
    /// This field provides direct internal access to the value type Interval, which RTree and other callers
    /// modify directly with .Add(); the auto-property returns a temporary value-by-copy that is immediately discarded.
    /// </summary>
    // ReSharper disable InconsistentNaming
    internal Interval interval;
    // ReSharper restore InconsistentNaming

    /// <summary>
    /// gets or sets the interval of the node
    /// </summary>
    public Interval Interval {
      get { return interval; }
      set { interval = value; }
    }

    /// <summary>
    /// false if it is an internal node and true if it is a leaf
    /// </summary>
    internal bool IsLeaf {
      get { return left == null /*&& right==null*/; } //if left is a null then right is also a null
    }
    /// <summary>
    /// 
    /// </summary>
    public IntervalNode<TData> Left {
      get { return left; }
      internal set {
        if (left != null && left.Parent == this)
          left.Parent = null;
        left = value;
        if (left != null)
          left.Parent = this;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public IntervalNode<TData> Right {
      get { return right; }
      internal set {
        if (right != null && right.Parent == this)
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
    public IntervalNode<TData> Parent { get; private set; }

    internal bool IsLeftChild {
      get {
        Debug.Assert(Parent != null);
        return Equals(Parent.Left);
      }
    }

    /// <summary>
    /// brings the first leaf which interval was hit and the delegate is happy with the object
    /// </summary>
    /// <param name="point"></param>
    /// <param name="hitTestFordoubleDelegate"></param>
    /// <returns></returns>
    public IntervalNode<TData> FirstHitNode(double point, Func<double, TData, HitTestBehavior> hitTestFordoubleDelegate) {
      if (interval.Contains(point)) {
        if (IsLeaf) {
          if (hitTestFordoubleDelegate != null) {
            return hitTestFordoubleDelegate(point, UserData) == HitTestBehavior.Stop ? this : null;
          }
          return this;
        }
        return Left.FirstHitNode(point, hitTestFordoubleDelegate) ??
                Right.FirstHitNode(point, hitTestFordoubleDelegate);
      }
      return null;
    }


    /// <summary>
    /// brings the first leaf which interval was intersected
    /// </summary>
    /// <returns></returns>
    public IntervalNode<TData> FirstIntersectedNode(Interval r) {
      if (r.Intersects(interval)) {
        if (IsLeaf)
          return this;
        return Left.FirstIntersectedNode(r) ?? Right.FirstIntersectedNode(r);
      }
      return null;
    }



    /// <summary>
    /// brings the first leaf which interval was hit and the delegate is happy with the object
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public IntervalNode<TData> FirstHitNode(double point) {
      if (interval.Contains(point)) {
        if (IsLeaf)
          return this;
        return Left.FirstHitNode(point) ?? Right.FirstHitNode(point);
      }
      return null;
    }


    /// <summary>
    /// returns all leaf nodes for which the interval was hit and the delegate is happy with the object
    /// </summary>
    /// <param name="intervalPar"></param>
    /// <param name="hitTestAccept"></param>
    /// <returns></returns>
    public IEnumerable<TData> AllHitItems(Interval intervalPar, Func<TData, bool> hitTestAccept) {
      var stack = new Stack<IntervalNode<TData>>();
      stack.Push(this);
      while (stack.Count > 0) {
        IntervalNode<TData> node = stack.Pop();
        if (node.Interval.Intersects(intervalPar)) {
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
    /// returns all items for which the interval contains the point
    /// </summary>
    /// <returns></returns>
    public IEnumerable<TData> AllHitItems(double point) {
      var stack = new Stack<IntervalNode<TData>>();
      stack.Push(this);
      while (stack.Count > 0) {
        var node = stack.Pop();
        if (node.Interval.Contains(point)) {
          if (node.IsLeaf)
            yield return node.UserData;
          else {
            stack.Push(node.left);
            stack.Push(node.right);
          }
        }
      }
    }

    static HitTestBehavior VisitTreeStatic(IntervalNode<TData> intervalNode, Func<TData, HitTestBehavior> hitTest, Interval hitInterval) {
      if (intervalNode.Interval.Intersects(hitInterval)) {
        if (hitTest(intervalNode.UserData) == HitTestBehavior.Continue) {
          if (intervalNode.Left != null) {
            // If intervalNode.Left is not null, intervalNode.Right won't be either.
            if (VisitTreeStatic(intervalNode.Left, hitTest, hitInterval) == HitTestBehavior.Continue &&
                VisitTreeStatic(intervalNode.Right, hitTest, hitInterval) == HitTestBehavior.Continue) {
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
    public IntervalNode<TData> Clone() {
      var ret = new IntervalNode<TData>(Count) { UserData = UserData, Interval = Interval };
      if (Left != null)
        ret.Left = Left.Clone();
      if (Right != null)
        ret.Right = Right.Clone();
      return ret;
    }

    /// <summary>
    /// yields all leaves which intervals intersect the given one. We suppose that leaves are all nodes having UserData not a null.
    /// </summary>
    /// <param name="intervalPar"></param>
    /// <returns></returns>
    public IEnumerable<TData> GetNodeItemsIntersectingInterval(Interval intervalPar) {
      return GetLeafIntervalNodesIntersectingInterval(intervalPar).Select(node => node.UserData);
    }

    /// <summary>
    /// yields all leaves whose intervals intersect the given one. We suppose that leaves are all nodes having UserData not a null.
    /// </summary>
    /// <param name="intervalPar"></param>
    /// <returns></returns>
    [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
    public IEnumerable<IntervalNode<TData>> GetLeafIntervalNodesIntersectingInterval(Interval intervalPar) {
      var stack = new Stack<IntervalNode<TData>>();
      stack.Push(this);
      while (stack.Count > 0) {
        IntervalNode<TData> node = stack.Pop();
        if (node.Interval.Intersects(intervalPar)) {
          if (node.IsLeaf) {
            yield return node;
          }
          else {
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

    internal IEnumerable<IntervalNode<TData>> GetAllLeafNodes() {
      return EnumIntervalNodes(true /*leafOnly*/);
    }

    IEnumerable<IntervalNode<TData>> EnumIntervalNodes(bool leafOnly) {
      var stack = new Stack<IntervalNode<TData>>();
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
    public static IntervalNode<TData> CreateIntervalNodeOnEnumeration(IEnumerable<IntervalNode<TData>> nodes) {
      if (nodes == null)
        return null;
      var nodeList = new List<IntervalNode<TData>>(nodes);
      return CreateIntervalNodeOnListOfNodes(nodeList);
    }

    ///<summary>
    ///calculates a tree based on the given nodes
    ///</summary>
    ///<param name="dataEnumeration"></param>
    ///<param name="intervalDelegate"></param>
    ///<returns></returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
    public static IntervalNode<TData> CreateIntervalNodeOnData(IEnumerable<TData> dataEnumeration, Func<TData, Interval> intervalDelegate) {
      if (dataEnumeration == null || intervalDelegate == null)
        return null;
      var nodeList = new List<IntervalNode<TData>>(dataEnumeration.Select(d => new IntervalNode<TData>(d, intervalDelegate(d))));
      return CreateIntervalNodeOnListOfNodes(nodeList);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="nodes"></param>
    /// <returns></returns>
    [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes"), SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
    static public IntervalNode<TData> CreateIntervalNodeOnListOfNodes(IList<IntervalNode<TData>> nodes) {
      ValidateArg.IsNotNull(nodes, "nodes");
      if (nodes.Count == 0) return null;

      if (nodes.Count == 1) return nodes[0];

      //Finding the seeds
      var b0 = nodes[0].Interval;

      //the first seed
      int seed0 = 1;

      int seed1 = ChooseSeeds(nodes, ref b0, ref seed0);

      //We have two seeds at hand. Build two groups.
      var gr0 = new List<IntervalNode<TData>>();
      var gr1 = new List<IntervalNode<TData>>();

      gr0.Add(nodes[seed0]);
      gr1.Add(nodes[seed1]);

      var box0 = nodes[seed0].Interval;
      var box1 = nodes[seed1].Interval;
      //divide nodes on two groups
      DivideNodes(nodes, seed0, seed1, gr0, gr1, ref box0, ref box1, GroupSplitThreshold);

      var ret = new IntervalNode<TData>(nodes.Count) {
        Interval = new Interval(box0, box1),
        Left = CreateIntervalNodeOnListOfNodes(gr0),
        Right = CreateIntervalNodeOnListOfNodes(gr1)
      };

      return ret;

    }

    static int ChooseSeeds(IList<IntervalNode<TData>> nodes, ref Interval b0, ref int seed0) {
      double area = new Interval(b0, nodes[seed0].Interval).Length;
      for (int i = 2; i < nodes.Count; i++) {
        double area0 = new Interval(b0, nodes[i].Interval).Length;
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

      area = new Interval(nodes[seed0].Interval, nodes[seed1].Interval).Length;
      //Now try to improve the second seed

      for (int i = 0; i < nodes.Count; i++) {
        if (i == seed0)
          continue;
        double area1 = new Interval(nodes[seed0].Interval, nodes[i].Interval).Length;
        if (area1 > area) {
          seed1 = i;
          area = area1;
        }
      }
      return seed1;
    }

    static void DivideNodes(IList<IntervalNode<TData>> nodes, int seed0, int seed1, List<IntervalNode<TData>> gr0, List<IntervalNode<TData>> gr1,
        ref Interval box0, ref Interval box1, int groupSplitThreshold) {
      for (int i = 0; i < nodes.Count; i++) {

        if (i == seed0 || i == seed1)
          continue;

        // ReSharper disable InconsistentNaming
        var box0_ = new Interval(box0, nodes[i].Interval);
        double delta0 = box0_.Length - box0.Length;

        var box1_ = new Interval(box1, nodes[i].Interval);
        double delta1 = box1_.Length - box1.Length;
        // ReSharper restore InconsistentNaming

        //keep the tree roughly balanced

        if (gr0.Count * groupSplitThreshold < gr1.Count) {
          gr0.Add(nodes[i]);
          box0 = box0_;
        }
        else if (gr1.Count * groupSplitThreshold < gr0.Count) {
          gr1.Add(nodes[i]);
          box1 = box1_;
        }
        else if (delta0 < delta1) {
          gr0.Add(nodes[i]);
          box0 = box0_;
        }
        else if (delta1 < delta0) {
          gr1.Add(nodes[i]);
          box1 = box1_;
        }
        else if (box0.Length < box1.Length) {
          gr0.Add(nodes[i]);
          box0 = box0_;
        }
        else {
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
    static public void TraverseHierarchy(IntervalNode<TData> node, Action<IntervalNode<TData>> visitor) {
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
