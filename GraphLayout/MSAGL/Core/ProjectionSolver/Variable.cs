// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Variable.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for Variables for Projection Solver.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

// Remove this from project build and uncomment here to selectively enable per-class.
//#define VERBOSE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    /// <summary>
    /// A Variable is essentially a wrapper around a node, containing the node's initial and 
    /// current (Actual) positions along the current axis and a collection of Constraints.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes")]
    public class Variable : IComparable<Variable>
    {
        /// <summary>
        /// Passed through as a convenience to the caller; it is not used by ProjectionSolver directly
        /// (except in VERIFY/VERBOSE where it uses ToString()), but if the variable list returned by
        /// Solver.Variables is sorted, then UserData must implement IComparable.  When Solve() is
        /// complete, the caller should copy the Variable's ActualPos property into whatever property
        /// the class specialization for this has.
        /// </summary>
        public Object UserData { get; set; }

        // These properties are initialized by caller before being passed to Solver.AddVariable.

        /// <summary>
        /// This holds the desired position of the node (the position we'd like it to have, initially 
        /// calculated before any constraint application).  This may change during the process of
        /// solution; currently that only happens if there are neighbors.  Each iteration of the
        /// solution keeps block reference-position calculation as close as possible to this position.
        /// </summary>
        public double DesiredPos { get; set; }

        // Variable has no Size member.  We use only the DesiredPos and (Scaled)ActualPos and
        // assume only a point (in a single dimension).  OverlapRemoval takes care of generating
        // constraints using size information.  It would not make sense to incorporate Size into
        // the violation calculation at the ProjectionSolver level for two reasons:
        //   - It would not take into account the potential deferral from horizontal to vertical
        //     when the vertical movement could be much less.
        //   - It would not automatically ensure that all overlap constraints were even calculated;
        //     it would only enforce constraints added by the caller, which would either use
        //     OverlapRemoval or have constraint-generation logic optimized for its own scenario.

        /// <summary>
        /// The weight of the node; a variable with a higher weight than others in its block will
        /// move less than it would if all weights were equal.
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// The scale of the variable.  May be set by the application.  For Qpsc this is computed 
        /// from the Hessian diagonal and replaces any application-set value during Solve().
        /// </summary>
        public double Scale { get; set; }

        /// <summary>
        /// The current position of the variable; s[i]y[i] in the scaling paper.  It is updated on each 
        /// iteration inside Solve(), then unscaled to contain the final position when Solve() completes.
        /// </summary>
        public double ActualPos { get; set; }

        /// <summary>
        /// The derivative value - essentially the weighted difference in position.
        /// </summary>
        internal double DfDv { get { return (2 * Weight * (ActualPos - DesiredPos)) / this.Scale; } }

        // Updated through Solve().
        internal double OffsetInBlock { get; set; }
        internal Block Block { get; set; }

        // For Qpsc
        internal uint Ordinal { get; private set; }

        // Use an array[] for Constraints for performance.  Their membership in the Variable doesn't change after
        // Solve() initializes, so we can use the fixed-size array and gain performance (at some up-front cost due
        // to buffering in AddVariable/AddConstraint, but the tradeoff is a great improvement).  This cannot be done
        // for Variables (whose membership in a Block changes) or Blocks (whose membership in the block list changes).

        // Constraints where 'this' is constraint.Left
        internal Constraint[] LeftConstraints { get; private set; }
        // Constraints where 'this' is constraint.Right
        internal Constraint[] RightConstraints { get; private set; }

        internal int ActiveConstraintCount
        {
            get { return activeConstraintCount; }
            set
            {
                Debug.Assert(value >= 0, "ActiveConstraintCount must be >= 0");
                activeConstraintCount = value;
            }
        }
        private int activeConstraintCount;

        internal struct NeighborAndWeight
        {
            internal Variable Neighbor { get; private set; }
            internal double Weight { get; private set; }

            internal NeighborAndWeight(Variable neighbor, double weight) : this()
            {
                this.Neighbor = neighbor;
                this.Weight = weight;
            }
        }
        
        // The (x1-x2)^2 neighbor relationships: Key == NeighborVar, Value == Weight of relationship
        internal List<NeighborAndWeight> Neighbors { get; private set; }

        internal Variable(uint ordinal, Object userData, double desiredPos, double weight, double scale)
        {
            if (weight <= 0)
            {
                throw new ArgumentOutOfRangeException("weight"
#if TEST_MSAGL
                        , "Variable Weight must be greater than zero"
#endif // TEST_MSAGL
                    );
            }
            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale"
#if TEST_MSAGL
                        , "Variable Scale must be greater than zero"
#endif // TEST_MSAGL
                    );
            }
            double check = desiredPos * weight;
            if (double.IsInfinity(check) || double.IsNaN(check))
            {
                throw new ArgumentOutOfRangeException("desiredPos"
#if TEST_MSAGL
                        , "Invalid Variable DesiredPosition * Weight"
#endif // TEST_MSAGL
                    );
            }
            check = desiredPos * scale;
            if (double.IsInfinity(check) || double.IsNaN(check))
            {
                throw new ArgumentOutOfRangeException("desiredPos"
#if TEST_MSAGL
                        , "Invalid Variable DesiredPosition * Scale"
#endif // TEST_MSAGL
                    );
            }
            this.Ordinal = ordinal;
            this.UserData = userData;
            this.DesiredPos = desiredPos;
            this.Weight = weight;
            this.Scale = scale;
            this.OffsetInBlock = 0.0;
            this.ActualPos = this.DesiredPos;
        }

        internal void Reinitialize()
        {
            // // Called by Qpsc or equivalence-constraint-regapping initial block restructuring.
            this.ActiveConstraintCount = 0;
            this.OffsetInBlock = 0.0;

            // If we are in Qpsc, this simply repeats (in the opposite direction) what
            // Qpsc.VariablesComplete did after (possibly) scaling.  If we're not in Qpsc,
            // then we've reset all the blocks because we could not incrementally re-Solve
            // due to changes to equality constraints, so this restores the initial state.
            this.ActualPos = this.DesiredPos;
        }

        internal void AddNeighbor(Variable neighbor, double weight)
        {
            if (null == this.Neighbors)
            {
                this.Neighbors = new List<NeighborAndWeight>();
            }
            this.Neighbors.Add(new NeighborAndWeight(neighbor, weight));
        }

        /// <summary>
        /// Gets a string representation of the Variable; calls UserData.ToString as part of this.
        /// </summary>
        /// <returns>A string representation of the variable.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
#if VERBOSE
                                "Var: '{0}' a {1:F5} d {2:F5} o {3:F5} w {4:F5} s {5:F5}",
                                this.Name, ActualPos, DesiredPos, OffsetInBlock, Weight, Scale);
#else  // VERBOSE
                                "{0} {1:F5} ({2:F5}) {3:F5} {4:F5}",
                                this.Name, ActualPos, DesiredPos, Weight, Scale);
#endif // VERBOSE
        }

        /// <summary>
        /// Gets the string representation of UserData.
        /// </summary>
        /// <returns>A string representation of Node.Object.</returns>
        public string Name
        {
            get { return (null == this.UserData) ? "-0-" : this.UserData.ToString(); }
        }

        internal void SetConstraints(Constraint[] leftConstraints, Constraint[] rightConstraints)
        {
            this.LeftConstraints = leftConstraints;
            this.RightConstraints = rightConstraints;
        }

        #region IComparable<Variable> Members
        /// <summary>
        /// Compare the Variables by their ordinals, in ascending order (this == lhs, other == rhs).
        /// </summary>
        /// <param name="other">The object being compared to.</param>
        /// <returns>-1 if this.Ordinal is "less"; +1 if this.Ordinal is "greater"; 0 if this.Ordinal
        ///         and rhs are equal.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.CompareTo(System.String)")]
        public int CompareTo(Variable other)
        {
            ValidateArg.IsNotNull(other, "other");
            return this.Ordinal.CompareTo(other.Ordinal);
        }
        #endregion // IComparer<Node> Members

#if NOTNEEDED_FXCOP // This entails a perf hit due to ==/!= becoming a non-inlined function call in some cases.
                    // We only create one Variable object per variable so do not need anything but reference
                    // ==/!=, so we suppress:1036 above.  Add UnitTests for these if they're enabled.
        #region RequiredOverridesForIComparable

        // Omitting getHashCode violates rule: OverrideGetHashCodeOnOverridingEquals.
        public override int GetHashCode() {
            return Ordinal.GetHashCode();
        }

        // Omitting any of the following violates rule: OverrideMethodsOnComparableTypes.
        public override bool Equals(Object obj) {
            if (!(obj is Variable))
                return false;
            return (this.CompareTo((Variable)obj) == 0);
        }
        public static bool operator ==(Variable lhs, Variable rhs) {
            if (null == (object)lhs) {          // Cast to object to avoid recursive op==
                return (null == (object)rhs);
            }
            return lhs.Equals(rhs);
        }
        public static bool operator !=(Variable lhs, Variable rhs) {
            return !(lhs == rhs);
        }
        public static bool operator <(Variable lhs, Variable rhs) {
            return (lhs.CompareTo(rhs) < 0);
        }
        public static bool operator >(Variable lhs, Variable rhs) {
            return (lhs.CompareTo(rhs) > 0);
        }
        #endregion // RequiredOverridesForIcomparable
#endif // NOTNEEDED_FXCOP

    }
}