// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConstraintVector.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for Constraint vector management for Projection Solver.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Collections.Generic;

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    /// <summary>
    /// </summary>
    class ConstraintVector
    {
        internal Constraint[] Vector { get; private set; }
        internal bool IsEmpty { get { return null == Vector; } }

        internal void Create(int numConstraints)
        {
            Vector = new Constraint[numConstraints];

            // Initialize this to out of range.
            firstActiveConstraintIndex = numConstraints;
        }

        private int nextConstraintIndex;
        internal void Add(Constraint constraint)
        {
            Debug.Assert(!constraint.IsActive, "Constraint should not be active");
            constraint.SetVectorIndex(nextConstraintIndex);
            Vector[nextConstraintIndex++] = constraint;
        }

        private int firstActiveConstraintIndex;
        internal void ActivateConstraint(Constraint constraint)
        {
            Debug.Assert(!constraint.IsActive, "Constraint is already active");

            // Swap it from the inactive region to the start of the active region of the Vector.
            Debug.Assert(firstActiveConstraintIndex > 0, "All constraints are already active");
            --firstActiveConstraintIndex;
            Debug.Assert(!Vector[firstActiveConstraintIndex].IsActive, "Constraint in inactive region is active");

            SwapConstraint(constraint);

            //Debug_AssertConsistency();
        }

        internal void DeactivateConstraint(Constraint constraint)
        {
            Debug.Assert(constraint.IsActive, "Constraint is not active");

            // Swap it from the active region to the end of the inactive region of the Vector.
            Debug.Assert(firstActiveConstraintIndex < Vector.Length, "All constraints are already inactive");
            Debug.Assert(Vector[firstActiveConstraintIndex].IsActive, "Constraint in active region is not active");

            SwapConstraint(constraint);
            ++firstActiveConstraintIndex;

            //Debug_AssertConsistency();
        }

        private void SwapConstraint(Constraint constraint)
        {
            // Swap out the constraint at the current active/inactive border index (which has been updated
            // according to the direction we're moving it).
            Constraint swapConstraint = Vector[firstActiveConstraintIndex];
            swapConstraint.SetVectorIndex(constraint.VectorIndex);
            Vector[constraint.VectorIndex] = swapConstraint;

            // Toggle the state of the constraint being updated.
            Vector[firstActiveConstraintIndex] = constraint;
            constraint.SetActiveState(!constraint.IsActive, firstActiveConstraintIndex);
        }

        internal void Reinitialize()
        {
            // Qpsc requires reinitializing the block structure
            if (null == Vector)
            {
                return;
            }
            foreach (var constraint in Vector)
            {
                constraint.Reinitialize();
            }
            firstActiveConstraintIndex = Vector.Length;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), Conditional("TEST_MSAGL")]
        internal void Debug_AssertIsFull()
        {
            Debug.Assert(Vector.Length == nextConstraintIndex, "AllConstraints.Vector is not full");
            Debug_AssertConsistency();
        }

        [Conditional("TEST_MSAGL")]
        internal void Debug_AssertConsistency()
        {
            for (int ii = 0; ii < Vector.Length; ++ii)
            {
                var constraint = Vector[ii];
                Debug.Assert(constraint.VectorIndex == ii, "Inconsistent constraint.VectorIndex");
                if (constraint.IsActive)
                {
                    Debug.Assert(constraint.VectorIndex >= firstActiveConstraintIndex, "Active constraint is in Inactive region");
                }
                else
                {
                    Debug.Assert(constraint.VectorIndex < firstActiveConstraintIndex, "Inactive constraint is in Active region");
                }
            }
        }

        #region BlockConstraintGlobals
        // Some convenient constraint-related "globals" for communication between the Solver and Blocks.
        internal Parameters SolverParameters;

        // The node stack for "recursive iteration" of constraint trees, and the recycled node stack
        // to reduce inner-loop alloc/GC overhead.
        internal Stack<DfDvNode> DfDvStack = new Stack<DfDvNode>();
        internal Stack<DfDvNode> DfDvRecycleStack = new Stack<DfDvNode>();

        internal void RecycleDfDvNode(DfDvNode node) {
            // In the case of long constraint chains make sure this does not end up as big as the number of constraints in the block.
            if (this.DfDvRecycleStack.Count < 1024) {
                DfDvRecycleStack.Push(node);
            }
        }

        // Initialized in Solve() and computed during Block.ComputeDfDv.
        internal int MaxConstraintTreeDepth;

        // This is the list of lists of unsatisfiable constraints accumulated during all Block.Expand calls.
        // As in the doc, this can only happen during Block.Expand. The only way to get a cycle is to add a constraint
        // where both variables are already connected by an active tree, so therefore they must already be in the
        // same block; therefore the cycle can't be created by MergeBlocks.  If there is a forward non-equality
        // constraint in the path, then that constraint will be deactivated and its variables moved, so there is
        // no cycle.  So the only condition for a cycle is that Expand finds no forward non-equality constraint.
        ////
        // Equality constraints (forward or backward) returned in the path between the .left and .right variables
        // of the constraint passed to Expand() do not change this; if you have an unsatisfied inequality constraint
        // between the two variables of an equality constraint, then the inequality is unsatisfiable; and by extension
        // then if it is between two variables between which there exists a path consisting solely of equality
        // constraints and backward-inequality constraints, it is unsatisfiable.
        ////
        // Negative gaps mean "left can be up to <+gap> greater than right", so again this does not affect it.
        ////
        // Therefore the only reason multi-constraint cycles would exist is if a block was expanded to accommodate
        // the constraint (incrementing the offsets to the right) despite not having found a forward minLagrangian.
        // This also means that ComputeDfDv should never encounter cycles.
        internal int NumberOfUnsatisfiableConstraints;

        #endregion // BlockConstraintGlobals

        /// <summary>
        /// </summary>
        public override string ToString()
        {
            return Vector.ToString();
        }
    }
}