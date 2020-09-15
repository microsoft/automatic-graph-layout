// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Constraint.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL Constraint class for Projection solutions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

// Remove this from project build and uncomment here to selectively enable per-class.
//#define VERBOSE

using System;
using System.Diagnostics;

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    /// <summary>
    /// A Constraint defines the required minimal separation between two Variables
    /// (thus is essentially a wrapper around the require minimal separation between
    /// two nodes).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes")]
    public class Constraint : IComparable<Constraint>
    {
        /// <summary>
        /// The Left (if horizontal; Top, if vertical) variable of the constraint.
        /// </summary>
        public Variable Left { get; private set; }

        /// <summary>
        /// The Right (if horizontal; Bottom, if vertical) variable of the constraint.
        /// </summary>
        public Variable Right { get; private set; }

        /// <summary>
        /// The required separation of the points of the two Variables along the current axis.
        /// </summary>
        public double Gap { get; private set; }

        /// <summary>
        /// Indicates if the distance between the two variables must be equal to the gap
        /// (rather than greater or equal to).
        /// </summary>
        public bool IsEquality { get; private set; }

        internal double Lagrangian { get; set; }
        internal bool IsActive { get; private set; }
#if TEST_MSAGL
        internal int IdDfDv { get; set; }
#endif // TEST_MSAGL
        internal bool IsUnsatisfiable { get; set; }

        // Index in Solver.AllConstraints, to segregate active from inactive constraints.
        internal int VectorIndex { get; private set; }
        internal void SetActiveState(bool activeState, int newVectorIndex)
        {
            // Note: newVectorIndex may be the same as the old one if we are changing the state
            // of the last inactive or first active constraint.
            Debug.Assert(IsActive != activeState, "Constraint is already set to activationState");
            IsActive = activeState;
            VectorIndex = newVectorIndex;
            if (IsActive)
            {
                ++Left.ActiveConstraintCount;
                ++Right.ActiveConstraintCount;
            }
            else
            {
                --Left.ActiveConstraintCount;
                --Right.ActiveConstraintCount;
            }
        }
        internal void SetVectorIndex(int vectorIndex)
        {
            // This is separate from set_VectorIndex because we can't restrict the caller to a specific
            // class and we only want ConstraintVector to be able to call this.
            this.VectorIndex = vectorIndex;
        }

        internal void Reinitialize()
        {
            // Called by Qpsc or equivalence-constraint-regapping initial block restructuring.
            // All variables have been moved to their own blocks again, so reset solution states.
            IsActive = false;
            IsUnsatisfiable = false;
            this.ClearDfDv();
        }

        // This is an internal function, not a propset, because we only want it called by the Solver.
        internal void UpdateGap(double newGap) { this.Gap = newGap; }

#if VERIFY || VERBOSE
        internal uint Id { get; private set; }
#endif // VERIFY || VERBOSE

        // The Constraint constructor takes the two variables and their required distance.
        // The constraints will be generated either manually or by ConstraintGenerator,
        // both of which know about the sizes when the constraints are generated (as
        // well as any necessary padding), so the sizes are accounted for at that time
        // and ProjectionSolver classes are not aware of Variable sizes.
        internal Constraint(Variable left, Variable right, double gap, bool isEquality
#if VERIFY || VERBOSE
                            , uint constraintId
#endif // VERIFY || VERBOSE
)
        {
            this.Left = left;
            this.Right = right;
            this.Gap = gap;
            this.IsEquality = isEquality;
#if VERIFY || VERBOSE
            Id = constraintId;
#endif // VERIFY || VERBOSE

            this.Lagrangian = 0.0;
            this.IsActive = false;
        }

        // For Solver.ComputeDfDv's DummyParentNode's constraint only.
        internal Constraint(Variable variable)
        {
            this.Left = this.Right = variable;
        }

        /// <summary>
        /// Generates a string representation of the Constraint.
        /// </summary>
        /// <returns>A string representation of the Constraint.</returns>
        public override string ToString()
        {
#if VERIFY || VERBOSE
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "  Cst ({8}/{9}-{10}): [{0}] [{1}] {2} {3:F5} vio {4:F5} Lm {5:F5}/{6:F5} {7}actv",
                                this.Left, this.Right, this.IsEquality ? "==" : ">=", this.Gap,
                                this.Violation, this.Lagrangian, this.Lagrangian * 2,
                                this.IsActive ? "+" : (this.IsUnsatisfiable ? "!" : "-"),
                                this.Id, this.Left.Block.Id, this.Right.Block.Id);
#else  // VERIFY || VERBOSE
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "  Cst: [{0}] [{1}] {2} {3:F5} vio {4:F5} Lm {5:F5}/{6:F5} {7}actv",
                                this.Left, this.Right, this.IsEquality ? "==" : ">=", this.Gap,
                                this.Violation, this.Lagrangian, this.Lagrangian * 2,
                                this.IsActive ? "+" : (this.IsUnsatisfiable ? "!" : "-"));
#endif // VERIFY || VERBOSE
        }

        internal double Violation
        {
            // If the difference in position is negative or zero, it means the startpos of the Rhs
            // node is greater than or equal to the gap and thus there's no violation.
            // This uses an absolute (unscaled) positional comparison (multiplying by scale
            // "undoes" the division-by-scale in Block.UpdateVariablePositions).
            // Note: this is too big for the CLR to inline so it is "manually inlined" in 
            // high-call-volume places; these are marked with Inline_Violation.
            get { return (this.Left.ActualPos * this.Left.Scale) + this.Gap - (this.Right.ActualPos * this.Right.Scale); }
        }

        internal void ClearDfDv()
        {
#if TEST_MSAGL
            this.IdDfDv = 0;
#endif // TEST_MSAGL
            this.Lagrangian = 0.0;
        }

        #region IComparable<Constraint> Members
        /// <summary>
        /// Compare this Constraint to rhs by their Variables in ascending order (this == lhs, other == rhs).
        /// </summary>
        /// <param name="other">The object being compared to.</param>
        /// <returns>-1 if this.Left/Right are "less"; +1 if this.Left/Right are "greater"; 0 if this.Left/Right
        ///         and rhs.Left/Right are equal.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.CompareTo(System.String)")]
        public int CompareTo(Constraint other)
        {
            ValidateArg.IsNotNull(other, "other");
            int cmp = this.Left.CompareTo(other.Left);
            if (0 == cmp)
            {
                cmp = this.Right.CompareTo(other.Right);
            }
            if (0 == cmp)
            {
                cmp = this.Gap.CompareTo(other.Gap);
            }
            return cmp;
        }
        #endregion // IComparable<Constraint> Members

#if NOTNEEDED_FXCOP // This entails a perf hit due to ==/!= becoming a non-inlined function call in some cases.
                    // We only create one Constraint object per constraint so do not need anything but reference
                    // ==/!=, so we suppress:1036 above.  Add UnitTests for these if they're enabled.
        #region RequiredOverridesForIComparable

        // Omitting getHashCode violates rule: OverrideGetHashCodeOnOverridingEquals.
        public override int GetHashCode() {
            return this.Left.GetHashCode() ^ this.Right.GetHashCode();
        }
        // Omitting any of the following violates rule: OverrideMethodsOnComparableTypes.
        public override bool Equals(Object obj) {
            if (!(obj is Constraint))
                return false;
            return (this.CompareTo((Constraint)obj) == 0);
        }
        public static bool operator ==(Constraint lhs, Constraint rhs) {
            if (null == (object)lhs) {          // Cast to object to avoid recursive op==
                return (null == (object)rhs);
            }
            return lhs.Equals(rhs);
        }
        public static bool operator !=(Constraint lhs, Constraint rhs) {
            return !(lhs == rhs);
        }
        public static bool operator <(Constraint lhs, Constraint rhs) {
            return (lhs.CompareTo(rhs) < 0);
        }
        public static bool operator >(Constraint lhs, Constraint rhs) {
            return (lhs.CompareTo(rhs) > 0);
        }
        #endregion // RequiredOverridesForIComparable
#endif // NOTNEEDED_FXCOP

    }
}