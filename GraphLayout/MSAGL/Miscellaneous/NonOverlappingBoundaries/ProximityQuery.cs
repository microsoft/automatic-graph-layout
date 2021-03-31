using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Prototype.NonOverlappingBoundaries {
    /// <summary>
    /// An IHull is used for proximity queries and should implement the Project method which (similar to IConstraint)
    /// should remove overlap between two hulls by moving them as little as possible
    /// </summary>
    public interface IHull {
        /// <summary>
        /// Actual or weighted center of the hull
        /// </summary>
        Point Center { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="delta"></param>
        void MoveCenter(Point delta);
        /// <summary>
        /// Bounding box
        /// </summary>
        RectangleNode<IHull,Point> RectangleNode { get; }
        /// <summary>
        /// Remove overlap between this and another IHull by moving them both as little as possible
        /// </summary>
        /// <param name="other"></param>
        /// <returns>amount of displacement</returns>
        double Project(IHull other);
    }
    /// <summary>
    /// Uses Lev's HierarchyCalculatorWithRectangularNodes to do overlap queries
    /// </summary>
    internal class ProximityQuery {
        internal ProximityQuery(List<IHull> nodeHulls) {
            hierarchy = RectangleNode<IHull,Point>.CreateRectangleNodeOnEnumeration(GetNodeRects(nodeHulls));
        }

        /// <summary>
        /// Find all overlapping pairs.
        /// </summary>
        /// <returns>List of overlapping pairs</returns>
        internal List<Tuple<IHull, IHull>> GetAllIntersections() {
            List<Tuple<IHull, IHull>> closePairs = new List<Tuple<IHull, IHull>>();
            GetClosePairs(hierarchy, hierarchy, closePairs);
            return closePairs;
        }
        private RectangleNode<IHull,Point> hierarchy;
        /// <summary>
        /// Search in the hierarchy for rectangles that intersect with leafNode.
        /// </summary>
        /// <param name="leafNode"></param>
        /// <param name="intersecting"></param>
        /// <param name="internalNode"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void GetIntersecting(RectangleNode<IHull,Point> leafNode, RectangleNode<IHull,Point> internalNode, List<IHull> intersecting) {
            Debug.Assert(leafNode.UserData != null);
            if (!leafNode.Rectangle.Intersects(internalNode.Rectangle))
                return;
            if (internalNode.UserData != null) {
                if (leafNode.UserData != internalNode.UserData) {
                    intersecting.Add(internalNode.UserData);
                }
            } else {
                GetIntersecting(leafNode, internalNode.Left, intersecting);
                GetIntersecting(leafNode, internalNode.Right, intersecting);
            }
        }
        private void GetClosePairs(RectangleNode<IHull,Point> a, RectangleNode<IHull,Point> b, List<Tuple<IHull, IHull>> closePairs) {
            if (!a.Rectangle.Intersects(b.Rectangle))
                return;
            if (a.UserData != null && b.UserData != null) {
                if (a.UserData != b.UserData) {
                    Point ap = a.UserData.Center;
                    Point bp = b.UserData.Center;
                    if (ap.X <= bp.X || (ap.X == bp.X && ap.Y <= bp.Y)) {
                        closePairs.Add(new Tuple<IHull, IHull>(a.UserData, b.UserData));
                    }
                }
            } else if (a.UserData == null && b.UserData == null) {
                GetClosePairs(a.Left, b.Left, closePairs);
                GetClosePairs(a.Left, b.Right, closePairs);
                GetClosePairs(a.Right, b.Left, closePairs);
                GetClosePairs(a.Right, b.Right, closePairs);
            } else if (a.UserData == null) {
                GetClosePairs(a.Left, b, closePairs);
                GetClosePairs(a.Right, b, closePairs);
            } else {
                GetClosePairs(a, b.Left, closePairs);
                GetClosePairs(a, b.Right, closePairs);
            }
        }

        private IEnumerable<RectangleNode<IHull,Point>> GetNodeRects(List<IHull> nodes) {
            foreach (var v in nodes) {
                yield return v.RectangleNode;
            }
        }
    }
}
