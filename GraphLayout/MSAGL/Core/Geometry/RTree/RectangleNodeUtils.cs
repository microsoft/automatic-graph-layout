using System;

namespace Microsoft.Msagl.Core.Geometry {
    internal sealed class RectangleNodeUtils {
        internal static void CrossRectangleNodes<TA, TB>(RectangleNode<TA> a, RectangleNode<TB> b, Action<TA, TB> action) {
            if (!a.rectangle.Intersects(b.rectangle))
                return;
            if (a.Left == null) { //a is a leat
                if (b.Left == null) //b is a leaf                    
                    action(a.UserData, b.UserData);
                else {
                    CrossRectangleNodes(a, b.Left, action);
                    CrossRectangleNodes(a, b.Right, action);
                }
            } else { //a is not a leaf            
                if (b.Left != null) {
                    CrossRectangleNodes(a.Left, b.Left, action);
                    CrossRectangleNodes(a.Left, b.Right, action);
                    CrossRectangleNodes(a.Right, b.Left, action);
                    CrossRectangleNodes(a.Right, b.Right, action);
                } else { // b is a leaf
                    CrossRectangleNodes(a.Left, b, action);
                    CrossRectangleNodes(a.Right, b, action);
                }
            }
        }

        internal static void CrossRectangleNodes<TA>(RectangleNode<TA> a, RectangleNode<TA> b, Action<TA, TA> action) {
            if (!a.rectangle.Intersects(b.rectangle))
                return;
            if (Equals(a, b))
                HandleEquality(a, action);
            else if (a.Left == null) {
                if (b.Left == null) {
                    action(a.UserData, b.UserData);
                } else {
                    CrossRectangleNodes<TA>(a, b.Left, action);
                    CrossRectangleNodes<TA>(a, b.Right, action);
                }
            } else {
                if (b.Left != null) {
                    CrossRectangleNodes<TA>(a.Left, b.Left, action);
                    CrossRectangleNodes<TA>(a.Left, b.Right, action);
                    CrossRectangleNodes<TA>(a.Right, b.Left, action);
                    CrossRectangleNodes<TA>(a.Right, b.Right, action);
                } else {
                    CrossRectangleNodes<TA>(a.Left, b, action);
                    CrossRectangleNodes<TA>(a.Right, b, action);
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
        internal static bool FindIntersectionWithProperty<TA>(RectangleNode<TA> a, RectangleNode<TA> b, Func<TA, TA, bool> property) {
            if (!a.rectangle.Intersects(b.rectangle))
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
            } else {
                if (b.Left != null) {
                    if (FindIntersectionWithProperty(a.Left, b.Left, property))
                        return true;
                    if (FindIntersectionWithProperty(a.Left, b.Right, property))
                        return true;
                    if (FindIntersectionWithProperty(a.Right, b.Left, property))
                        return true;
                    if (FindIntersectionWithProperty(a.Right, b.Right, property))
                        return true;
                } else {

                    if (FindIntersectionWithProperty(a.Left, b, property))
                        return true;
                    if (FindIntersectionWithProperty(a.Right, b, property))
                        return true;
                }
            }
            return false;
        }

        static bool HandleEqualityCheck<TA>(RectangleNode<TA> a, Func<TA, TA, bool> func) {
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
        static void HandleEquality<TA>(RectangleNode<TA> a, Action<TA, TA> action) {
            if (a.Left == null) return; //we don't do anything for two equal leafs
            CrossRectangleNodes<TA>(a.Left, a.Left, action);
            CrossRectangleNodes<TA>(a.Left, a.Right, action);
            CrossRectangleNodes<TA>(a.Right, a.Right, action);
        }
    }
}