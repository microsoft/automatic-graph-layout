// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlapRemovalNode.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL Node class for Overlap removal constraint generation for Projection solutions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Msagl.Core.Geometry
{
    /// <summary>
    /// A node essentially wraps the coordinates of a Variable for the Open and Close Events for
    /// that Variable.  It contains the list of left and right nodes which are immediate neighbours,
    /// where immediate is defined as overlapping or some subset of the closest non-overlapping
    /// Variables (currently this subset is the first one encountered on any event, since it is
    /// transitive; if there is a second non-overlapping node, then the first non-overlapping
    /// node will have a constraint on it).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes")]
    public class OverlapRemovalNode : IComparable<OverlapRemovalNode>, IComparable
    {
        /// <summary>
        /// Passed through as a convenience to the caller; it is not used by OverlapRemoval directly
        /// (except in VERIFY/VERBOSE where it uses ToString()).  When Solve() is complete, the caller
        /// should copy the Node.Position property into whatever property the class specialization for this has.
        /// </summary>
        public Object UserData { get; set; }

        /// <summary>
        /// The string representing the user data object, or a null indicator string.
        /// </summary>
        protected string UserDataString
        {
            get
            {
                return (null == this.UserData) ? "-0-" : this.UserData.ToString();
            }
        }

        /// <summary>
        /// The Variable representing this Node (or Cluster border) in the ProjectionSolver passed to
        /// Generate().  Once Solve() is called, this is cleared out.
        /// </summary>
        public ProjectionSolver.Variable Variable { get; set; }

        // Set and retrieved during Cluster.GenerateFromEvents.
        internal List<OverlapRemovalNode> LeftNeighbors { get; set; }
        internal List<OverlapRemovalNode> RightNeighbors { get; set; }

        // If these are set, it means that during the horizontal pass we deferred a node's constraint
        // generation to the vertical pass, so we can't jump out of neighbour evaluation on that node.
        internal bool DeferredLeftNeighborToV { get; set; }
        internal bool DeferredRightNeighborToV { get; set; }

        // These track the (P)erpendicular coordinates to the Variable's coordinates.
        // This is to order the Events, and for horizontal constraint generation, is
        // also used to decide which direction resolves the overlap with minimal movement.

        /// <summary>
        /// The coordinate of the Node along the primary axis.  Updated by ConstraintGenerator.Solve().
        /// </summary>
        public double Position { get; internal set; }

        /// <summary>
        /// The coordinate of the Node along the secondary (Perpendicular) axis.
        /// </summary>
        public double PositionP { get; internal set; }  // Updated only for Clusters

        /// <summary>
        /// The size of the Node along the primary axis.
        /// </summary>
        public double Size { get; internal set; }       // Updated only for Clusters

        /// <summary>
        /// The size of the Node along the secondary (Perpendicular) axis.
        /// </summary>
        public double SizeP { get; internal set; }      // Updated only for Clusters

        /// <summary>
        /// The opening border of the Node along the primary axis; Left if horizontal,
        /// Top if Vertical.
        /// </summary>
        public double Open
        {
            get { return Position - (Size / 2); }
        }

        /// <summary>
        /// The closing border of the Node along the primary axis; Right if horizontal,
        /// Bottom if Vertical.
        /// </summary>
        public double Close
        {
            get { return Position + (Size / 2); }
        }

        /// <summary>
        /// The opening border of the Node along the secondary (Perpendicular) axis; Top if horizontal,
        /// Bottom if Vertical.
        /// </summary>
        public double OpenP
        {
            get { return PositionP - (SizeP / 2); }
        }

        /// <summary>
        /// The closing border of the Node along the secondary (Perpendicular) axis; Bottom if horizontal,
        /// Right if Vertical.
        /// </summary>
        public double CloseP
        {
            get { return PositionP + (SizeP / 2); }
        }

        /// <summary>
        /// The weight of the node along the primary axis.
        /// </summary>
        public double Weight { get; internal set; }

        // This identifies the node for consistent-sorting purposes in the Event list.
        internal uint Id { get; private set; }

        // This is the normal node ctor, from ConstraintGenerator.
        internal OverlapRemovalNode(uint id, object userData, double position, double positionP,
                    double size, double sizeP, double weight)
        {
            ValidateArg.IsPositive(size, "size");
            ValidateArg.IsPositive(size, "sizeP");
            ValidateArg.IsPositive(weight, "weight");
            if (weight <= 0)
            {
                throw new ArgumentOutOfRangeException("weight"
#if TEST_MSAGL
                        , "Invalid node properties"
#endif // TEST_MSAGL
                        );
            }
            double dblCheck = (Math.Abs(position) + size) * weight;
            if (double.IsInfinity(dblCheck) || double.IsNaN(dblCheck))
            {
                throw new ArgumentOutOfRangeException("position"
#if TEST_MSAGL
                        , "Invalid node properties"
#endif // TEST_MSAGL
                        );
            }
            dblCheck = (Math.Abs(positionP) + sizeP) * weight;
            if (double.IsInfinity(dblCheck) || double.IsNaN(dblCheck))
            {
                throw new ArgumentOutOfRangeException("positionP"
#if TEST_MSAGL
                        , "Invalid node properties"
#endif // TEST_MSAGL
                        );
            }
            this.Id = id;
            this.UserData = userData;
            this.Position = position;
            this.PositionP = positionP;
            this.Size = size;
            this.SizeP = sizeP;
            this.Weight = weight;
        }

        // This is the constructor for the "fake nodes" of a Cluster and its Borders.
        // We default to free border weight so the cluster borders can move freely during Solve().
        // The weight is overridden for the Cluster border nodes during Cluster.Generate.
        internal OverlapRemovalNode(uint id, object userData)
            : this(id, userData, 0.0, 0.0, 0.0, 0.0, BorderInfo.DefaultFreeWeight)
        {
        }

        internal static double Overlap(OverlapRemovalNode n1, OverlapRemovalNode n2, double padding)
        {
            // Returns > 0 if the nodes overlap (combined sizes/2 plus required padding between
            // nodes is greater than the distance between the nodes).
            return ((n1.Size + n2.Size) / 2) + padding - Math.Abs(n1.Position - n2.Position);
        }

        internal static double OverlapP(OverlapRemovalNode n1, OverlapRemovalNode n2, double paddingP)
        {
            // Returns > 0 if the nodes overlap (combined sizes/2 plus required padding between
            // nodes is greater than the distance between the nodes).
            return ((n1.SizeP + n2.SizeP) / 2) + paddingP - Math.Abs(n1.PositionP - n2.PositionP);
        }

        /// <summary>
        /// Create the backing Variable for this Node in the solver.
        /// </summary>
        /// <param name="solver"></param>
        public void CreateVariable(ProjectionSolver.Solver solver)
        {
            ValidateArg.IsNotNull(solver, "solver");
            // Due to multiple hierarchies, we must check to see if the variable has been created yet;
            // we share one Node (and its single Variable) across all clusters it's a member of.
            if (null == this.Variable)
            {
                this.Variable = solver.AddVariable(this /* userData */, this.Position, this.Weight);
            }
            else
            {
                // Make sure the position is updated as the caller may have called this before and then we recalculated
                // the position at some point (e.g. for Cluster boundary nodes).
                UpdateDesiredPosition(this.Position);
            }
        }

        // Overridden by Cluster.  Called after Solve(); sets the Node position to that of its Variable.
        internal virtual void UpdateFromVariable()
        {
            // If the Variable is null then we were already updated from an earlier cluster in
            // another hierarchy.
            if (null != this.Variable)
            {
                this.Position = this.Variable.ActualPos;

                // Currently we don't use this anymore.
                this.Variable = null;
            }
        }

        // Currently called only from clusters when repositioning "fake border" nodes from the
        // constraint-generation position (outer edge) to the pre-Solve position (central edge).
        internal void UpdateDesiredPosition(double newPosition)
        {
            this.Position = newPosition;
            this.Variable.DesiredPos = newPosition;
        }

        /// <summary>
        /// Generate a string representation of the Node.
        /// </summary>
        /// <returns>A string representation of the Node.</returns>
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "Nod '{0}': id {1} p {2:F5} s {3:F5} pP {4:F5} sP {5:F5}",
                                this.UserDataString, this.Id, this.Position, this.Size, this.PositionP, this.SizeP);
        }

        #region IComparable<Node> Members
        /// <summary>
        /// Compare the Nodes by ActualPos in ascending left-to-right order (this == lhs, other == rhs).
        /// </summary>
        /// <param name="other">The object being compared to.</param>
        /// <returns>-1 if 'this' is "less"; +1 if 'this' is "greater"; 0 if 'this' and rhs are equal.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.CompareTo(System.String)")]
        public int CompareTo(OverlapRemovalNode other)
        {
            ValidateArg.IsNotNull(other, "other");
            int cmp = this.Position.CompareTo(other.Position);
            if (0 == cmp)
            {
                cmp = this.Id.CompareTo(other.Id);
            }
            return cmp;
        }
        #endregion // IComparable<Node> Members

        #region IComparable Members
        /// <summary>
        /// Compare the Nodes by ActualPos in ascending left-to-right order (this == lhs, other == rhs).
        /// </summary>
        /// <param name="obj">The object being compared to.</param>
        /// <returns>-1 if 'this' is "less"; +1 if 'this' is "greater"; 0 if 'this' and rhs are equal.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.CompareTo(System.String)")]
        public int CompareTo(object obj)
        {
            var rhs = obj as OverlapRemovalNode;
            if (null == rhs)
            {
                throw new InvalidOperationException(
#if TEST_MSAGL
                        "Argument 'obj' must be a Node"
#endif // TEST_MSAGL
);
            }
            return this.CompareTo(rhs);
        }
        #endregion // IComparable<Node> Members

#if NOTNEEDED_FXCOP // This entails a perf hit due to ==/!= becoming a non-inlined function call in some cases.
                    // We only create one Node object per variable so do not need anything but reference
                    // ==/!=, so we suppress:1036 above.  Add UnitTests for these if they're enabled.
        #region RequiredOverridesForIComparable

        // Omitting getHashCode violates rule: OverrideGetHashCodeOnOverridingEquals.
        public override int GetHashCode() {
            return this.Position.GetHashCode() ^ this.Id.GetHashCode();
        }
        // Omitting any of the following violates rule: OverrideMethodsOnComparableTypes.
        public override bool Equals(Object obj) {
            if (!(obj is Node))
                return false;
            return (this.CompareTo(obj) == 0);
        }
        public static bool operator == (Node lhs, Node rhs) {
            if (null == (object)lhs) {          // Cast to object to avoid recursive op==
                return (null == (object)rhs);
            }
            return lhs.Equals(rhs);
        }
        public static bool operator != (Node lhs, Node rhs) {
            return !(lhs == rhs);
        }
        public static bool operator < (Node lhs, Node rhs) {
            return (lhs.CompareTo(rhs) < 0);
        }
        public static bool operator > (Node lhs, Node rhs) {
            return (lhs.CompareTo(rhs) > 0);
        }
        #endregion // RequiredOverridesForIComparable
#endif // NOTNEEDED_FXCOP
    } // end class Node

    // NodeComparer is used by the RBTree.
    internal class NodeComparer : IComparer<OverlapRemovalNode>
    {
        #region IComparer<Node> Members
        /// <summary>
        /// Compare the points by point ActualPos in ascending left-to-right order.
        /// </summary>
        /// <param name="lhs">Left-hand side of the comparison.</param>
        /// <param name="rhs">Right-hand side of the comparison.</param>
        /// <returns>-1 if lhs is less than rhs; +1 if lhs is greater than rhs; else 0.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.CompareTo(System.String)")]
        public int Compare(OverlapRemovalNode lhs, OverlapRemovalNode rhs)
        {
            ValidateArg.IsNotNull(lhs, "lhs");
            ValidateArg.IsNotNull(rhs, "rhs");

            return lhs.CompareTo(rhs);
        }
        #endregion // IComparer<Node> Members
    }
}