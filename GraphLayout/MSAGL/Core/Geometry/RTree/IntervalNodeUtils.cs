using System;

namespace Microsoft.Msagl.Core.Geometry {
  internal sealed class IntervalNodeUtils {
    internal static void CrossIntervalNodes<TA, TB>(IntervalNode<TA> a, IntervalNode<TB> b, Action<TA, TB> action) {
      if (!a.interval.Intersects(b.interval))
        return;
      if (a.Left == null) { //a is a leat
        if (b.Left == null) //b is a leaf                    
          action(a.UserData, b.UserData);
        else {
          CrossIntervalNodes(a, b.Left, action);
          CrossIntervalNodes(a, b.Right, action);
        }
      }
      else { //a is not a leaf            
        if (b.Left != null) {
          CrossIntervalNodes(a.Left, b.Left, action);
          CrossIntervalNodes(a.Left, b.Right, action);
          CrossIntervalNodes(a.Right, b.Left, action);
          CrossIntervalNodes(a.Right, b.Right, action);
        }
        else { // b is a leaf
          CrossIntervalNodes(a.Left, b, action);
          CrossIntervalNodes(a.Right, b, action);
        }
      }
    }

    internal static void CrossIntervalNodes<TA>(IntervalNode<TA> a, IntervalNode<TA> b, Action<TA, TA> action) {
      if (!a.interval.Intersects(b.interval))
        return;
      if (Equals(a, b))
        HandleEquality(a, action);
      else if (a.Left == null) {
        if (b.Left == null) {
          action(a.UserData, b.UserData);
        }
        else {
          CrossIntervalNodes<TA>(a, b.Left, action);
          CrossIntervalNodes<TA>(a, b.Right, action);
        }
      }
      else {
        if (b.Left != null) {
          CrossIntervalNodes<TA>(a.Left, b.Left, action);
          CrossIntervalNodes<TA>(a.Left, b.Right, action);
          CrossIntervalNodes<TA>(a.Right, b.Left, action);
          CrossIntervalNodes<TA>(a.Right, b.Right, action);
        }
        else {
          CrossIntervalNodes<TA>(a.Left, b, action);
          CrossIntervalNodes<TA>(a.Right, b, action);
        }
      }
    }

    /// <summary>
    /// returns true if "property" holds for some pair
    /// </summary>
    /// <typeparam name="TA"></typeparam>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="property"></param>
    /// <returns></returns>
    internal static bool FindIntersectionWithProperty<TA>(IntervalNode<TA> a, IntervalNode<TA> b, Func<TA, TA, bool> property) {
      if (!a.interval.Intersects(b.interval))
        return false;
      if (Equals(a, b))
        return HandleEqualityCheck(a, property);

      if (a.Left == null) {
        if (b.Left == null)
          return property(a.UserData, b.UserData);

        if (FindIntersectionWithProperty(a, b.Left, property))
          return true;
        if (FindIntersectionWithProperty(a, b.Right, property))
          return true;
      }
      else {
        if (b.Left != null) {
          if (FindIntersectionWithProperty(a.Left, b.Left, property))
            return true;
          if (FindIntersectionWithProperty(a.Left, b.Right, property))
            return true;
          if (FindIntersectionWithProperty(a.Right, b.Left, property))
            return true;
          if (FindIntersectionWithProperty(a.Right, b.Right, property))
            return true;
        }
        else {

          if (FindIntersectionWithProperty(a.Left, b, property))
            return true;
          if (FindIntersectionWithProperty(a.Right, b, property))
            return true;
        }
      }
      return false;
    }

    static bool HandleEqualityCheck<TA>(IntervalNode<TA> a, Func<TA, TA, bool> func) {
      if (a.Left == null) return false; //we don't do anything for two equal leafs
      return FindIntersectionWithProperty(a.Left, a.Left, func) ||
             FindIntersectionWithProperty(a.Left, a.Right, func) || FindIntersectionWithProperty(a.Right, a.Right, func);
    }

    /// <summary>
    /// we need to avoid calling action twice for the same pair
    /// </summary>
    /// <typeparam name="TA"></typeparam>
    /// <param name="a"></param>
    /// <param name="action"></param>
    static void HandleEquality<TA>(IntervalNode<TA> a, Action<TA, TA> action) {
      if (a.Left == null) return; //we don't do anything for two equal leafs
      CrossIntervalNodes<TA>(a.Left, a.Left, action);
      CrossIntervalNodes<TA>(a.Left, a.Right, action);
      CrossIntervalNodes<TA>(a.Right, a.Right, action);
    }
  }
}