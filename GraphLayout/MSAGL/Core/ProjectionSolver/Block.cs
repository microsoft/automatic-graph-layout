// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Block.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL Block class for Projection solutions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

// Remove this from project build and uncomment here to selectively enable per-class.
//#define VERBOSE

#if DEVTRACE
//#define COMPARE_RECURSIVE_DFDV
#endif // DEVTRACE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    // A Block is essentially a collection of Variables, which in turn contain
    // a collection of Constraints.
    internal class Block
    {
        // The list of variables also contains the list of active Constraints in the block; all Active
        // constraints will have both their Left- and Right-hand Variables in the block's Variables List.
        // Additionally, inactive constraints are only enumerated on Project(); for those, block membership
        // isn't set, so we just enumerate all Blocks' Variables' LeftConstraints.
        // Perf note: Updates to the constraints/variables along active constraints are sufficiently common
        // that maintaining a priority queue of all constraints would be more expense than gain; also, we would
        // want this prioritization along the path between each variable pair we'd encounter.  Thus it doesn't
        // seem possible to improve upon the iteration approach.
        // Perf note: We use List instead of Set because the only benefit to Set is faster .Remove,
        // but it requires using enumerators which are slower than direct indexing; we only .Remove in Block.Split
        // which isn't frequent enough to offset the slower enumerators.
        internal List<Variable> Variables { get; private set; }

        // Block reference position for use in Variable.(Scaled)ActualPos.
        internal double ReferencePos { get; set; }

        // The scale of the block - same as that of the first of its variables.
        internal double Scale { get; set; }

        // AD from the paper; modified for weights to be sum a[i] * d[i] * w[i]
        double sumAd;

        // AB from the paper; modified for weights to be sum a[i] * b[i] * w[i]
        double sumAb;

        // A2 from the paper; modified for weights to be sum a[i] * a[i] * w[i]
        double sumA2;

        // Index into Solver.BlockVector for faster removal.
        internal int VectorIndex { get; set; }

        // For Path traversal in Expand.
        struct ConstraintDirectionPair
        {
            internal readonly Constraint Constraint;
            internal readonly bool IsForward;
            internal ConstraintDirectionPair(Constraint constraint, bool isLeftToRight)
            {
                this.Constraint = constraint;
                this.IsForward = isLeftToRight;
            }
        }
        List<ConstraintDirectionPair> constraintPath;
        Variable pathTargetVariable;

#if TEST_MSAGL
        // For detecting and reporting cycles in ComputeDfDv in case we have some unexpected
        // case that gets past the null-minLagrangian check in Block.Expand.
        int idDfDv;
#endif // TEST_MSAGL

        // The global list of all constraints, used in the "recursive iteration" functions
        // and for active/inactive constraint partitioning.
        readonly ConstraintVector allConstraints;

#if VERIFY || VERBOSE
        internal uint Id { get; private set; }
#endif // VERIFY || VERBOSE

        internal Block(Variable initialVariable, ConstraintVector allConstraints
#if VERIFY || VERBOSE
                        , ref uint blockId
#endif // VERIFY || VERBOSE
)
        {
            this.Variables = new List<Variable>();

            // On initialization, each variable is put into its own block.  If this was called from Block.Split
            // initialVariable will be null.
            if (null != initialVariable)
            {
                AddVariable(initialVariable);
            }
            this.allConstraints = allConstraints;
#if VERIFY || VERBOSE
            // Increment the ID so the caller doesn't need a second #if VERIFY... block to do so.
            Id = blockId++;
#endif // VERIFY || VERBOSE
        }

        /// <summary>
        /// Generate a string representation of the Block.
        /// </summary>
        /// <returns>A string representation of the Block.</returns>
        public override string ToString()
        {
#if VERIFY || VERBOSE
            return string.Format(CultureInfo.InvariantCulture,
                                "[Block ({0}): nvars = {1} refpos = {2:F5} scale = {3:F5}]",
                                Id, this.Variables.Count, ReferencePos, Scale);
#else  // VERIFY || VERBOSE
            return string.Format(CultureInfo.InvariantCulture,
                                "[Block: nvars = {0} refpos = {1:F5} scale = {2:F5}]",
                                this.Variables.Count, ReferencePos, Scale);
#endif // VERIFY || VERBOSE
        }

#if COMPARE_RECURSIVE_DFDV
        double Recursive_DfDv(Variable varToEval, Constraint currentConstraint, int level) {
            var dfdv = varToEval.DfDv;
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("Recursive_DfDv level {0} initial dfdv = {1}", level, dfdv);
            System.Diagnostics.Debug.WriteLine("    varToEval = " + varToEval);
            if (0 != level) {
                System.Diagnostics.Debug.WriteLine("    currentConstraint  = " + currentConstraint);
                System.Diagnostics.Debug.WriteLine("    " + ((varToEval == currentConstraint.Right) ? "(LToR)" : "RToL"));
            }
#endif // VERBOSE

            foreach (var constraint in varToEval.LeftConstraints) {
                if (constraint.IsActive && (constraint != currentConstraint)) {
                    constraint.Lagrangian = Recursive_DfDv(constraint.Right, constraint, level + 1);
                    dfdv += constraint.Lagrangian;
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine("Recursive_DfDv level {0} LToR cst {1} Lm = {2}, .Left scale = {3}, dfdv = {4}",
                            level, constraint.Id, constraint.Lagrangian, constraint.Left.Scale, dfdv);
#endif // VERBOSE
                }
            }

            foreach (var constraint in varToEval.RightConstraints) {
                if (constraint.IsActive && (constraint != currentConstraint)) {
                    constraint.Lagrangian = -Recursive_DfDv(constraint.Left, constraint, level + 1);
                    dfdv -= constraint.Lagrangian;
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine("Recursive_DfDv level {0} RToL cst {1} Lm = {2}, .Right scale = {3}, dfdv = {4}",
                            level, constraint.Id, constraint.Lagrangian, constraint.Right.Scale, dfdv);
#endif // VERBOSE
                }
            }

            if (0 == level) {
                DebugVerifyFinalDfDvValue(dfdv, String.Format(CultureInfo.InvariantCulture, "nonzero final Recursive_DfDv value ({0})", dfdv));
                Debug_ClearDfDv(false /* forceFull */);
            }
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("Recursive_DfDv level {0} final dfdv = {1}", level, dfdv);
#endif // VERBOSE
            return dfdv;
        }
#endif // COMPARE_RECURSIVE_DFDV

#if TEST_MSAGL
        void DebugVerifyFinalDfDvValue(double dfdv, string message)
        {
            // Account for rounding.
            double divisor = Math.Max(this.sumAd, Math.Max(this.sumAb, this.sumA2));
            Debug.Assert((Math.Abs(dfdv) / divisor) < 0.001, message);
        }
#endif

        // The dummy parent node that saves us from having to do null testing.
        DfDvNode dfDvDummyParentNode;

        internal void ComputeDfDv(Variable initialVarToEval)
        {
#if COMPARE_RECURSIVE_DFDV
            var recursiveDfDv = Recursive_DfDv(initialVarToEval, null, 0);
#endif // COMPARE_RECURSIVE_DFDV
#if TEST_MSAGL
            Debug.Assert(0 != this.idDfDv, "idDfDv should not be 0");
#endif // TEST_MSAGL
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("ComputeDfDv initialVarToEval: [{0}]", initialVarToEval);
#endif // VERBOSE

            // Compute the derivative of the spanning tree (comprised of our active constraints) at the
            // point of variableToEval (with "change" being the difference between "Desired" position and the calculated
            // position for the current pass), for all paths that do not include the edge variableToEval->variableDoneEval.

            // Recursiteratively process all outgoing paths from variableToEval to its right (i.e. where it is constraint.Left),
            // but don't include variableDoneEval because it's already been evaluated.
            // At each variable on the rightward traversal, we'll also process leftward paths (incoming to) that
            // variable (via the following constraint loop) before returning here.
            // variableToEval and variableDoneEval (if not null) are guaranteed to be in this Block, since they're co-located
            // in an active Constraint in this Block.
            //
            // For Expand, we want to find the constraint path from violatedConstraint.Left to violatedConstraint.Right;
            // the latter is in pathTargetVariable.  This is ComputePath from the doc.  The logic there is:
            //    Do the iterations of ComputeDvDv
            //    If we find the target, then traverse the parent chain to populate the list bottom-up

            Debug.Assert(0 == this.allConstraints.DfDvStack.Count, "Leftovers in ComputeDfDvStack");
            this.allConstraints.DfDvStack.Clear();

            // Variables for initializing the first node.
            var dummyConstraint = new Constraint(initialVarToEval);
            dfDvDummyParentNode = new DfDvNode(dummyConstraint);
            var firstNode = GetDfDvNode(dfDvDummyParentNode, dummyConstraint, initialVarToEval, null /*no "done" var yet*/);
            this.allConstraints.DfDvStack.Push(firstNode);

            // Iteratively recurse, processing all children of a constraint before the constraint itself.
            // Loop termination is by testing for completion based on node==firstNode which is faster than 
            // (non-inlined) Stack.Count.
            for (; ; )
            {
                // Leave the node on the stack until we've processed all of its children.
                var node = this.allConstraints.DfDvStack.Peek();
                int prevStackCount = this.allConstraints.DfDvStack.Count;

#if VERBOSE
                System.Diagnostics.Debug.WriteLine("ComputeDfDv peeking at node {0}: varDoneEval {1}, {2}Constraint: {3}",
                                    node, node.VariableDoneEval, node.IsLeftToRight ? "Left" : "Right", node.ConstraintToEval);
#endif // VERBOSE

                if (!node.ChildrenHaveBeenPushed)
                {
                    node.ChildrenHaveBeenPushed = true;
                    foreach (var constraint in node.VariableToEval.LeftConstraints)
                    {
                        // Direct violations (a -> b -> a) are not caught by the constraint-based cycle detection
                        // because VariableDoneEval prevents them from being entered (b -> a is not entered because a is
                        // VariableDoneEval).  These cycles should be caught by the null-minLagrangian IsUnsatisfiable
                        // setting in Block.Expand (but assert with IsActive not IsUnsatisfiable, as the constraint
                        // may not have been encountered yet).  Test_Unsatisfiable_Cycle_InDirect_With_SingleConstraint_Var.
                        Debug.Assert(!constraint.IsActive || !(node.IsLeftToRight && (constraint.Right == node.VariableDoneEval)),
                                "this cycle should not happen");
                        if (constraint.IsActive && (constraint.Right != node.VariableDoneEval))
                        {
                            // variableToEval is now considered "done"
                            var childNode = GetDfDvNode(node, constraint, constraint.Right, node.VariableToEval);

                            // If the node has no constraints other than the one we're now processing, it's a leaf
                            // and we don't need the overhead of pushing to and popping from the stack.
                            if (1 == constraint.Right.ActiveConstraintCount)
                            {
                                ProcessDfDvLeafNodeDirectly(childNode);
                            }
                            else
                            {
                                PushDfDvNode(childNode);
                            }
                        }
                    }

                    foreach (var constraint in node.VariableToEval.RightConstraints)
                    {
                        // See comments in .LeftConstraints.
                        Debug.Assert(!constraint.IsActive || !(!node.IsLeftToRight && (constraint.Left == node.VariableDoneEval)),
                                "this cycle should not happen");
                        if (constraint.IsActive && (constraint.Left != node.VariableDoneEval))
                        {
                            var childNode = GetDfDvNode(node, constraint, constraint.Left, node.VariableToEval);
                            if (1 == constraint.Left.ActiveConstraintCount)
                            {
                                ProcessDfDvLeafNodeDirectly(childNode);
                            }
                            else
                            {
                                PushDfDvNode(childNode);
                            }
                        }
                    }

                    // If we just pushed one or more nodes, loop back up and "recurse" into them.
                    if (this.allConstraints.DfDvStack.Count > prevStackCount)
                    {
                        continue;
                    }
                } // endif !node.ChildrenHaveBeenPushed

                // We are at a non-leaf node and have "recursed" through all its descendents; therefore pop it off
                // the stack and process it.  If it's the initial node, we've already updated DummyConstraint.Lagrangian
                // from all child nodes, and it's in the DummyParentNode as well so this will add the final dfdv.
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("ComputeDfDv: Node has no children or all have been processed");
#endif // VERBOSE
                Debug.Assert(this.allConstraints.DfDvStack.Peek() == node, "DfDvStack.Peek() should be 'node'");
                this.allConstraints.DfDvStack.Pop();
                ProcessDfDvLeafNode(node);
                if (node == firstNode)
                {
                    Debug.Assert(0 == this.allConstraints.DfDvStack.Count, "Leftovers in DfDvStack on completion of loop");
                    break;
                }
            } // endwhile stack is not empty

#if VERBOSE
            System.Diagnostics.Debug.WriteLine("ComputeDfDv result: {0:F5}", dummyConstraint.Lagrangian);
#endif // VERBOSE

#if TEST_MSAGL
            // From the definition of the optimal position of all variables that satisfies the constraints, the
            // final value of this should be zero.  Think of the constraints as rigid rods and the variables as
            // the attachment points of the rods.  Also think of those attachment points as having springs connecting
            // them to their ideal positions.  In order for the whole system to be at rest (i.e. at the optimal
            // position) the net force on the right-hand side of each rod must be equal and opposite to the net
            // force on the left-hand side.  Thus, the final return value of compute_dfdv is the sum of the entire
            // left-hand side and right-hand side - which should cancel (within rounding error).
            ////
            // The dummyConstraint "rolls up" to its parent which is itself, thus it will be twice the leftover.
            DebugVerifyFinalDfDvValue(dummyConstraint.Lagrangian / 2.0,
                    String.Format(CultureInfo.InvariantCulture, "nonzero final ComputeDfDv value ({0})", dummyConstraint.Lagrangian));
#if COMPARE_RECURSIVE_DFDV
            DebugVerifyFinalDfDvValue((dummyConstraint.Lagrangian / 2.0) - recursiveDfDv,
                    String.Format(CultureInfo.InvariantCulture, "Unequal DfDv values; Recursive = {0}, iterative = {1}", recursiveDfDv, dummyConstraint.Lagrangian));
#endif // COMPARE_RECURSIVE_DFDV
#endif // TEST_MSAGL
        } // end ComputeDfDv()

        void ProcessDfDvLeafNode(DfDvNode node)
        {
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("  ComputeDfDv depth {0} evaluating {1}Constraint: {2}", 
                                node.Depth, node.IsLeftToRight ? "Left" : "Right", node.ConstraintToEval);

            // If pathTargetVariable is non-null, we haven't found it yet, so this constraint is a dead end.
            if (null != this.pathTargetVariable) {
                System.Diagnostics.Debug.WriteLine("    {0} dead end: {1}", node.IsLeftToRight ? "LtoR" : "RtoL", node.ConstraintToEval);
            }
#endif // VERBOSE

            double dfdv = node.VariableToEval.DfDv;

            // Add dfdv to constraint.Lagrangian if we are going left-to-right, else subtract it ("negative slope");
            // similarly, add it to or subtract it from the parent's Lagrangian.
            if (node.IsLeftToRight)
            {
                node.ConstraintToEval.Lagrangian += dfdv;
                node.Parent.ConstraintToEval.Lagrangian += node.ConstraintToEval.Lagrangian;
            }
            else
            {
                // Any child constraints have already put their values into the current constraint 
                // according to whether they were left-to-right or right-to-left.  This is the equivalent
                // to the sum of return values in the recursive approach in the paper.  However, the paper
                // negates this return value when setting it into a right-to-left parent's Lagrangian;
                // we're that right-to-left parent now so do that first (negate the sum of children).
                node.ConstraintToEval.Lagrangian = -(node.ConstraintToEval.Lagrangian + dfdv);
                node.Parent.ConstraintToEval.Lagrangian -= node.ConstraintToEval.Lagrangian;
            }
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("  ComputeDfDv: incremental {0} dfdv = {1:F5}", node.IsLeftToRight ? "LToR" : "RToL", dfdv);
            System.Diagnostics.Debug.WriteLine("    Constraint {0} Lagrangian: {1:F5} (scale when {2} parent = {3:F5})",
                                node.ConstraintToEval.Id, node.ConstraintToEval.Lagrangian,
                                node.IsLeftToRight ? "added to" : "subtracted from",
                                node.IsLeftToRight ? node.ConstraintToEval.Left.Scale : node.ConstraintToEval.Right.Scale);
            System.Diagnostics.Debug.WriteLine("        Parent {0} Lagrangian: {1:F5}",
                                node.Parent.ConstraintToEval.Id, node.Parent.ConstraintToEval.Lagrangian);
#endif // VERBOSE

            // See if this node found the target variable.
            CheckForConstraintPathTarget(node);

            // If this active constraint is violated, record it.
            Debug_CheckForViolatedActiveConstraint(node.ConstraintToEval);

            // We're done with this node.
            this.allConstraints.RecycleDfDvNode(node);
        }

        [Conditional("TEST_MSAGL")]
        void Debug_CheckForViolatedActiveConstraint(Constraint constraint)
        {
            // Test is: Test_Unsatisfiable_Direct_Inequality(); it should not encounter this.
            if (constraint.Violation > this.allConstraints.SolverParameters.GapTolerance)
            {
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("Violated active constraint encountered: {0}", constraint);
#endif // VERBOSE
                Debug.Assert(false, "Violated active constraint should never be encountered");
            }
        }

        // Directly evaluate a leaf node rather than defer it to stack push/pop.
        void ProcessDfDvLeafNodeDirectly(DfDvNode node)
        {
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("ComputeDfDv directly processing the following leaf constraint:");
#endif // VERBOSE
            Debug_MarkForCycleCheck(node.ConstraintToEval);
            ProcessDfDvLeafNode(node);
        }

        DfDvNode GetDfDvNode(DfDvNode parent, Constraint constraintToEval, Variable variableToEval, Variable variableDoneEval)
        {
            DfDvNode node = (this.allConstraints.DfDvRecycleStack.Count > 0)
                            ? this.allConstraints.DfDvRecycleStack.Pop().Set(parent, constraintToEval, variableToEval, variableDoneEval)
                            : new DfDvNode(parent, constraintToEval, variableToEval, variableDoneEval);
            node.Depth = node.Parent.Depth + 1;
            if (this.allConstraints.MaxConstraintTreeDepth < node.Depth)
            {
                this.allConstraints.MaxConstraintTreeDepth = node.Depth;
            }
            return node;
        }

        // Called by ComputeDfDv.
        void PushDfDvNode(DfDvNode node)
        {
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("ComputeDfDv depth {0} pushing non-leaf {1}Constraint: {2}",
                    node.Depth, node.IsLeftToRight ? "Left" : "Right", node.ConstraintToEval);
#endif // VERBOSE
            Debug_CycleCheck(node.ConstraintToEval);
            PushOnDfDvStack(node);
        }

        [Conditional("TEST_MSAGL")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private void Debug_CycleCheck(Constraint constraint)
        {
#if TEST_MSAGL
            Debug.Assert(this.idDfDv != constraint.IdDfDv, "Cycle detected someplace other than null minLagrangian");
#endif // TEST_MSAGL
        }

        [Conditional("TEST_MSAGL")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private void Debug_MarkForCycleCheck(Constraint constraint)
        {
#if TEST_MSAGL
            constraint.IdDfDv = this.idDfDv;
#endif // TEST_MSAGL
        }

        // Called by RecurseGetConnectedVariables.
        void AddVariableAndPushDfDvNode(List<Variable> lstVars, DfDvNode node)
        {
            Debug_CycleCheck(node.ConstraintToEval);
            lstVars.Add(node.VariableToEval);
            PushOnDfDvStack(node);
        }

        void PushOnDfDvStack(DfDvNode node)
        {
            Debug_MarkForCycleCheck(node.ConstraintToEval);
            this.allConstraints.DfDvStack.Push(node);
        }

        void CheckForConstraintPathTarget(DfDvNode node)
        {
            if (this.pathTargetVariable == node.VariableToEval)
            {
                // Add every variable from pathTargetVariable up the callchain up to but not including initialVarToEval.
                while (node.Parent != this.dfDvDummyParentNode)
                {
                    this.constraintPath.Add(new ConstraintDirectionPair(node.ConstraintToEval, node.IsLeftToRight));
                    node = node.Parent;
                }
                this.pathTargetVariable = null;         // Path is complete
            }
        }

        [Conditional("TEST_MSAGL")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        void Debug_ClearDfDv(bool forceFull)
        {
#if TEST_MSAGL
            // This is now TEST_MSAGL-only, in case we encounter some strange case that gets past the check
            // for null minLagrangian in Block.Expand.

            // Clear the Lagrangian multiplier of all active constraints in this block; i.e. for all
            // Left-starting active constraints, and since inactive constraints don't care about the
            // Lagrangian, save the test and just clear all of them.
            if (forceFull || (int.MaxValue == this.idDfDv))
            {
                // To reduce the times we go through the variable list, use an ID that grows and only
                // pass through the list when it hits the maximum value and wraps.  0 is not a valid
                // value, so Constraints in their initial state don't trigger false positives.
                this.idDfDv = 1;
                int numVariables = this.Variables.Count;          // cache for perf
                for (int ii = 0; ii < numVariables; ++ii)
                {
                    var variable = this.Variables[ii];
                    foreach (var constraint in variable.LeftConstraints)
                    {
                        constraint.ClearDfDv();
                    }
                }
            }
            else
            {
                ++this.idDfDv;
#if VERIFY || VERBOSE
                // We removed FULL_CLEAR_DFDV for VERIFY mode because we now have cycle detection,
                // but in VERIFY we'll zero out the Lagrangian values to cause VERIFY output to differ from RELEASE
                // if is any reference to the stale Lagrangians (and to remove stale values from the VERBOSE output).
                foreach (var variable in this.Variables)
                {
                    foreach (var constraint in variable.LeftConstraints)
                    {
                        constraint.Lagrangian = 0.0;
                    }
                }
#endif // VERIFY || VERBOSE
            }
#endif // TEST_MSAGL
        }

#if VERBOSE
        internal void DumpState(string strPrefix)
        {
            if (null != (object)strPrefix)
            {
                System.Diagnostics.Debug.WriteLine("*** {0} ***", strPrefix);
            }

            foreach (var variable in this.Variables)
            {
                foreach (var constraint in variable.LeftConstraints)
                {
                    System.Diagnostics.Debug.WriteLine(constraint);
                }
            }

            // Display any variables that don't have constraints.
            foreach (var variable in this.Variables)
            {
                if (0 == variable.LeftConstraints.Length)
                {
                    // 2 spaces lead pad because Var.ToString is called for this or from Cst.ToString.
                    // "-0-: " is a placeholder for the missing "Cst: ".
                    System.Diagnostics.Debug.WriteLine("  -0-: [{0}]", variable);
                }
            }
        }
        void DumpPath(string strPrefix, Constraint minLagrangianConstraint)
        {
            if (null != (object)strPrefix)
            {
                System.Diagnostics.Debug.WriteLine("*** {0} ***", strPrefix);
            }
            foreach (ConstraintDirectionPair pathItem in constraintPath)
            {
                System.Diagnostics.Debug.WriteLine("{0} Dir={1}{2}", pathItem.Constraint, pathItem.IsForward ? "fwd" : "bwd",
                                                   (pathItem.Constraint == minLagrangianConstraint) ? " (min Lm)" : "");
            }
        }

        static void DumpSplitConstraint(string strPrefix, Constraint minLagrangianConstraint)
        {
            if (null != (object)strPrefix)
            {
                System.Diagnostics.Debug.WriteLine("*** {0} ***", strPrefix);
            }
            System.Diagnostics.Debug.WriteLine(minLagrangianConstraint);
        }
#endif // VERBOSE

        internal void Expand(Constraint violatedConstraint)
        {
            Debug_ClearDfDv(false /* forceFull */);

            // Calculate the derivative at the point of each constraint.
            // violatedConstraint's edge may be the minimum so pass null for variableDoneEval.
            // 
            // We also want to find the path along the active constraint tree from violatedConstraint.Left
            // to violatedConstraint.Right, and find the constraint on that path with the lowest Langragian
            // multiplier. The ActiveConstraints form a spanning tree so there will be no more than
            // one path. violatedConstraint is not yet active so it will not appear in this list.
            if (null == this.constraintPath)
            {
                this.constraintPath = new List<ConstraintDirectionPair>();
            }
            this.constraintPath.Clear();
            this.pathTargetVariable = violatedConstraint.Right;

#if VERBOSE
            System.Diagnostics.Debug.WriteLine("Before Block.Expand ComputeDfDv: {0}", this);
#endif // VERBOSE

            ComputeDfDv(violatedConstraint.Left);

#if VERBOSE
            System.Diagnostics.Debug.WriteLine("After Block.Expand ComputeDfDv: {0}", this);
            DumpState(null /* no prefix */);
#endif // VERBOSE

            // Now find the forward non-equality constraint on the path that has the minimal Lagrangina.
            // Both variables of the constraint are in the same block so a path should always be found.
            Constraint minLagrangianConstraint = null;
            if (this.constraintPath.Count > 0)
            {
                // We found an existing path so must remove an edge from our active list so that all 
                // connected variables from its varRight onward can move to the right; this will
                // make the "active" status false for that edge.  The active non-Equality constraint
                // with the minimal Lagrangian *that points rightward* is our split point (do *not*
                // split Equality constraints).
                foreach (ConstraintDirectionPair pathItem in this.constraintPath)
                {
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine("ConstraintPath: {0} ({1})", pathItem.Constraint, pathItem.IsForward ? "forward" : "backward");
#endif // VERBOSE
                    if (pathItem.IsForward 
                            && ((null == minLagrangianConstraint) || (pathItem.Constraint.Lagrangian < minLagrangianConstraint.Lagrangian)))
                    {
                        if (!pathItem.Constraint.IsEquality)
                        {
                            minLagrangianConstraint = pathItem.Constraint;
                        }
                    }
                }
#if VERBOSE
                DumpPath("Expand path", minLagrangianConstraint);
#endif // VERBOSE
                if (null != minLagrangianConstraint)
                {
                    // Deactivate this constraint as we are splitting on it.
                    this.allConstraints.DeactivateConstraint(minLagrangianConstraint);
                }
            }

            this.constraintPath.Clear();
            this.pathTargetVariable = null;

            if (null == minLagrangianConstraint)
            {
                // If no forward non-equality edge was found, violatedConstraint would have created a cycle.
                Debug.Assert(!violatedConstraint.IsUnsatisfiable, "An already-unsatisfiable constraint should not have been attempted");
                violatedConstraint.IsUnsatisfiable = true;
                ++this.allConstraints.NumberOfUnsatisfiableConstraints;
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("  -- Expand: No forward non-equality edge (minLagrangianConstraint) found, therefore the constraint is unsatisfiable -- ");
                System.Diagnostics.Debug.WriteLine("     Unsatisfiable constraint: {0}", violatedConstraint);
#endif // VERBOSE
                return;
            }

            // Note: for perf, expand in-place (as in Ipsep) rather than Split/Merge (as in the Scaling paper).

            // Adjust the offset of each variable at and past the right-hand side of violatedConstraint in the
            // active spanning tree.  Because we've removed minLagrangianConstraint, this will widen the
            // gap between minLagrangianConstraint.Left and .Right.  Note that this must include not only
            // violatedConstraint.Right and those to its right, but also those to its left that are connected
            // to it by active constraints - because the definition of an active constraint is that the
            // gap matches exactly with the actual position, so all will move as a unit.
            var lstConnectedVars = new List<Variable>();

            // We consider .Left "already evaluated" because we don't want the path evaluation to back
            // up to it (because we're splitting .Right off from it by deactivating the constraint).
            GetConnectedVariables(lstConnectedVars, violatedConstraint.Right, violatedConstraint.Left);
            double violation = violatedConstraint.Violation;
            int cConnectedVars = lstConnectedVars.Count;
            for (int ii = 0; ii < cConnectedVars; ++ii)
            {
                lstConnectedVars[ii].OffsetInBlock += violation;
            }

            // Now make the (no-longer-) violated constraint active.
            this.allConstraints.ActivateConstraint(violatedConstraint);

            // Clear the DfDv values.  For TEST_MSAGL, the new constraint came in from outside this block 
            // so this will make sure it doesn't have a stale cycle-detection flag.
            violatedConstraint.ClearDfDv();

            // Update this block's reference position.
            this.UpdateReferencePos();

#if VERBOSE
            System.Diagnostics.Debug.WriteLine("Block.Expand result: {0}", this);
            DumpState(null /* no prefix */);
#endif // VERBOSE
        } // end Expand()

        internal Block Split(bool isQpsc
#if VERIFY || VERBOSE
                            , ref uint blockId
#endif // VERIFY || VERBOSE
)
        {

            if (isQpsc)
            {
                // In the Qpsc case, we've modified current positions in PreProject() so need to update them here.
                this.UpdateReferencePos();
            }
#if EX_VERIFY
            else
            {
                // In the non-Qpsc case we call Project() before SplitBlocks, so the block's position should be
                // current and we can skip the UpdateReferencePos line in the paper.
                DebugVerifyReferencePos();
            }
#endif // EX_VERIFY

            // If there is only one variable there's nothing to split.
            if (this.Variables.Count < 2)
            {
                return null;
            }

            Constraint minLagrangianConstraint = null;
            Debug_ClearDfDv(false /* forceFull */);

            // Pick a variable from the active constraint list - it doesn't matter which; any variable in
            // the block is active (except for the initial one-var-per-block case), so ComputeDfDv will evaluate
            // it along the active path.  Eventually all variables needing to be repositioned will be part of
            // active constraints; even if SplitBlocks eventually happens, if the variable must be repositioned
            // again (via the global-constraint-maxviolation check) its constraint will be reactivated.
            // By the same token, ExpandBlock and SplitBlocks implicitly address/optimize all situations
            // (or close enough) where an Active (i.e. == Gap) constraint would be better made inactive 
            // and the gap grown.
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("Before Block.Split ComputeDfDv: {0}", this);
#endif // VERBOSE

            ComputeDfDv(this.Variables[0]);

#if VERBOSE
            System.Diagnostics.Debug.WriteLine("After Block.Split ComputeDfDv: {0}", this);
            DumpState(null /* no prefix */);
#endif // VERBOSE

            // We only split the block if it has a non-equality constraint with a Lagrangian that is more than a
            // rounding error below 0.0.
            double minLagrangian = this.allConstraints.SolverParameters.Advanced.MinSplitLagrangianThreshold;
            var numVars = this.Variables.Count;             // cache for perf
            for (var ii = 0; ii < numVars; ++ii)
            {
                foreach (var constraint in this.Variables[ii].LeftConstraints)      // Array.Foreach is optimized.
                {
                    if (constraint.IsActive && !constraint.IsEquality && (constraint.Lagrangian < minLagrangian))
                    {
#if TEST_MSAGL
                        Debug.Assert(constraint.IdDfDv == this.idDfDv, "stale constraint.Lagrangian");
#endif // TEST_MSAGL
                        minLagrangianConstraint = constraint;
                        minLagrangian = constraint.Lagrangian;
                    }
                }
            }

            // If we have no satisfying constraint, we're done.
            if (null == minLagrangianConstraint)
            {
                return null;
            }

#if VERBOSE
            DumpSplitConstraint("Splitting block at Constraint", minLagrangianConstraint);
#endif // VERBOSE

            return this.SplitOnConstraint(minLagrangianConstraint
#if VERIFY || VERBOSE
                                        , ref blockId
#endif // VERIFY || VERBOSE
                );
        }

        internal Block SplitOnConstraint(Constraint constraintToSplit
#if VERIFY || VERBOSE
                                        , ref uint blockId
#endif // VERIFY || VERBOSE
            )
        {
            // We have a split point.  Remove that constraint from our active list and transfer it and all
            // variables to its right to a new block.  As mentioned above, all variables and associated
            // constraints in the block are active, and the block split and recalc of reference positions
            // doesn't change the actual positions of any variables.
            this.allConstraints.DeactivateConstraint(constraintToSplit);
            var newSplitBlock = new Block(null, this.allConstraints
#if VERIFY || VERBOSE
                                        , ref blockId
#endif // VERIFY || VERBOSE
                );

            // Transfer the connected variables.  This has the side-effect of moving the associated active
            // constraints as well (because they are carried in the variables' LeftConstraints).
            // This must include not only minLagrangianConstraint.Right and those to its right, but also
            // those to its left that are connected to it by active constraints - because connected variables
            // must be within a single a block.  Since we are splitting the constraint, there will be at least
            // one variable (minLagrangianConstraint.Left) in the current block when we're done.  Because the active
            // constraints form a tree, we won't have a situation where minLagrangianConstraint.Left is
            // also the .Right of a constraint of a variable to the left of varRight.
            // minLagrangianConstraint.Left is "already evaluated" because we don't want the path evaluation to
            // back up to it (because we're splitting minLagrangianConstraint by deactivating it).
            this.DebugVerifyBlockConnectivity();
            this.TransferConnectedVariables(newSplitBlock, constraintToSplit.Right, constraintToSplit.Left);
            if (newSplitBlock.Variables.Count > 0)
            {
                // We may have removed the first variable so fully recalculate the reference position.
                this.UpdateReferencePos();

                // The new block's sums were not updated as its variables were added directly to its
                // variables list, so fully recalculate.
                newSplitBlock.UpdateReferencePos();

                this.DebugVerifyBlockConnectivity();
                newSplitBlock.DebugVerifyBlockConnectivity();

#if VERBOSE
                System.Diagnostics.Debug.WriteLine("Block.Split Result: {0} {1}", this, newSplitBlock);
                System.Diagnostics.Debug.WriteLine("Old block: {0}", this);
                this.DumpState(null /* no prefix */);
                System.Diagnostics.Debug.WriteLine("New block: {0}", newSplitBlock);
                newSplitBlock.DumpState(null /* no prefix */);
#endif // VERBOSE
            }
            else
            {
                // If there were unsatisfiable constraints, we may have tried to transfer all variables;
                // in that case we simply ignored the transfer operation and left all variables in 'this' block.
                // Return NULL so Solver.SplitBlocks knows we didn't split.
                newSplitBlock = null;
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("Block.Split Result: all variables would be moved, so skipping");
                this.DumpState(null /* no prefix */);
#endif // VERBOSE
            }

            return newSplitBlock;
        } // end Split()

        [Conditional("TEST_MSAGL")]
        private void DebugVerifyBlockConnectivity()
        {
            // This ensures that splitting a block does not split the variables of a constraint across
            // blocks, which was occurring in the cyclic case.
            foreach (var v in Variables)
            {
                foreach (var c in v.LeftConstraints)
                {
                    Debug.Assert(!c.IsActive || (c.Left.Block == c.Right.Block), "LeftConstraint outside of Block");
                }
                foreach (var c in v.RightConstraints)
                {
                    Debug.Assert(!c.IsActive || (c.Left.Block == c.Right.Block), "RightConstraint outside of Block");
                }
            }
        }

        [Conditional("TEST_MSAGL")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        internal void DebugVerifyReferencePos()
        {
#if TEST_MSAGL && EX_VERIFY
            // Due to rounding differences in MergeBlocks calculation of new refpos vs. UpdateReferencePos()
            // these may be slightly different, so have a tolerance range.  Restore it when done so there are no
            // VERIFY vs. RELEASE differences.
            var tempRefPos = this.ReferencePos;
            var tempSumAd = this.sumAd;
            var tempSumAb = this.sumAb;
            var tempSumA2 = this.sumA2;
            this.UpdateReferencePos();

            // Don't divide by 0.
            if (this.ReferencePos != tempRefPos) {
                double divisor = (0.0 != tempRefPos) ? tempRefPos : this.ReferencePos;
                Debug.Assert(Math.Abs((this.ReferencePos - tempRefPos) / divisor) < GlobalConfiguration.BlockReferencePositionEpsilon,
                        "Unexpected difference in Block.UpdateReferencePos from previously calculated value");
            }
            this.ReferencePos = tempRefPos;
            this.sumAd = tempSumAd;
            this.sumAb = tempSumAb;
            this.sumA2 = tempSumA2;
#endif // TEST_MSAGL && EX_VERIFY
        }

        internal void AddVariable(Variable variable)
        {
            // Don't recalculate position yet; that will be done after all Block.AddVariable calls and then
            // block-merge processing are done.
            this.Variables.Add(variable);
            variable.Block = this;

            if (1 == this.Variables.Count)
            {
                // The block's information is set to that of the initial variable's "actual" state; we won't
                // call UpdateReferencePosFromSums.
                this.Scale = variable.Scale;
                this.ReferencePos = variable.ActualPos;
                this.sumAd = variable.ActualPos * variable.Weight;
                this.sumAb = 0;
                this.sumA2 = variable.Weight;
                variable.OffsetInBlock = 0.0;
            }
            else
            {
                // Don't update ReferencePos yet because this is called from MergeBlocks or SplitBlock
                // for a number of variables and we'll call UpdateReferencePosFromSums when they're all added.
                AddVariableToBlockSums(variable);
            }
        }

        internal void UpdateReferencePos()
        {
            // Make sure we're using the first variable's scale, in case the previous first-variable
            // has been removed.
            this.Scale = this.Variables[0].Scale;

            // Note:  This does not keep the variables at their current positions; rather, it pulls them
            // closer to their desired positions (this is easily seen by running through the math for a
            // single variable).  However the relative positions are preserved.  This helps the solution
            // remain minimal.
            this.sumAd = 0.0;
            this.sumAb = 0.0;
            this.sumA2 = 0.0;
            var numVars = this.Variables.Count;           // cache for perf
            for (var ii = 0; ii < numVars; ++ii)
            {
                AddVariableToBlockSums(this.Variables[ii]);
            }
            UpdateReferencePosFromSums();
        }

        void AddVariableToBlockSums(Variable variable)
        {
            // a and b are from the scaling paper - with calculations modified for weights.
            var a = this.Scale / variable.Scale;
            var b = variable.OffsetInBlock / variable.Scale;
            var aw = a * variable.Weight;
            this.sumAd += aw * variable.DesiredPos;
            this.sumAb += aw * b;
            this.sumA2 += aw * a;
        }

        internal void UpdateReferencePosFromSums()
        {
            // This is called from Solver.MergeBlocks as well as internally.
            if (double.IsInfinity(this.sumAd) || double.IsInfinity(this.sumAb) || double.IsInfinity(this.sumA2))
            {
                throw new OverflowException(
#if TEST_MSAGL
                        "Block Reference Position component is infinite"
#endif // TEST_MSAGL
                );
            }
            this.ReferencePos = (sumAd - sumAb) / sumA2;
            UpdateVariablePositions();
        }

        void UpdateVariablePositions()
        {
            double scaledReferencePos = this.Scale * this.ReferencePos;
            int numVars = this.Variables.Count;         // iteration is faster than foreach for List
            for (int ii = 0; ii < numVars; ++ii)
            {
                var v = this.Variables[ii];

                // The derivation on this is from the paper:  a_i * YB + b_i
                //      a_i == this.Scale / v.Scale
                //      YB  == this.ReferencePos
                //      b_i == v.OffsetInBlock / v.Scale
                // Thus
                //      ((this.Scale / v.Scale) * this.ReferencePos) + (v.OffsetInBlock / v.Scale)
                // reorganizes to...
                //      ((this.Scale * this.ReferencePos) / v.Scale) + (v.OffsetInBlock / v.Scale)
                // which simplifies to...
                v.ActualPos = (scaledReferencePos + v.OffsetInBlock) / v.Scale;
            }
        }

        internal void GetConnectedVariables(List<Variable> lstVars, Variable varToEval, Variable varDoneEval)
        {
            // First set up cycle-detection in TEST_MSAGL mode.
            Debug_ClearDfDv(false /* forceFull */);
            RecurseGetConnectedVariables(lstVars, varToEval, varDoneEval);

#if VERBOSE
            System.Diagnostics.Debug.WriteLine("GetConnectedVariables result:");
            foreach (var variable in lstVars) {
                System.Diagnostics.Debug.WriteLine("  {0}", variable);
            }
#endif // VERBOSE
        }

        internal void RecurseGetConnectedVariables(List<Variable> lstVars, Variable initialVarToEval, Variable initialVarDoneEval)
        {
            // Get all the vars at and to the right of 'var', including backtracking to get all
            // variables that are connected from the left.  This is just like ComputeDfDv except
            // that in this case we start with the variableDoneEval being the Left variable.
            Debug.Assert(0 == this.allConstraints.DfDvStack.Count, "Leftovers in ComputeDfDvStack");
            this.allConstraints.DfDvStack.Clear();
            Debug.Assert(0 == lstVars.Count, "Leftovers in lstVars");

            // Variables for initializing the first node.
            var dummyConstraint = new Constraint(initialVarToEval);
            dfDvDummyParentNode = new DfDvNode(dummyConstraint);
            this.allConstraints.DfDvStack.Push(GetDfDvNode(dfDvDummyParentNode, dummyConstraint, initialVarToEval, initialVarDoneEval));
            lstVars.Add(initialVarToEval);

            // Do a pre-order tree traversal (process the constraint before its children), for consistency
            // with prior behaviour.
            while (this.allConstraints.DfDvStack.Count > 0)
            {
                // Leave the node on the stack until we've processed all of its children.
                var node = this.allConstraints.DfDvStack.Peek();
                int prevStackCount = this.allConstraints.DfDvStack.Count;

                if (!node.ChildrenHaveBeenPushed)
                {
                    node.ChildrenHaveBeenPushed = true;
                    foreach (var constraint in node.VariableToEval.LeftConstraints)
                    {
                        if (constraint.IsActive && (constraint.Right != node.VariableDoneEval))
                        {
                            // If the node has no constraints other than the one we're now processing, it's a leaf
                            // and we don't need the overhead of pushing to and popping from the stack.
                            if (1 == constraint.Right.ActiveConstraintCount)
                            {
                                Debug_CycleCheck(constraint);
                                Debug_MarkForCycleCheck(constraint);
                                lstVars.Add(constraint.Right);
                            }
                            else
                            {
                                // variableToEval is now considered "done"
                                AddVariableAndPushDfDvNode(lstVars, GetDfDvNode(node, constraint, constraint.Right, node.VariableToEval));
                            }
                        }
                    }

                    foreach (var constraint in node.VariableToEval.RightConstraints)
                    {
                        if (constraint.IsActive && (constraint.Left != node.VariableDoneEval))
                        {
                            // See comments in .LeftConstraints
                            if (1 == constraint.Left.ActiveConstraintCount)
                            {
                                Debug_CycleCheck(constraint);
                                Debug_MarkForCycleCheck(constraint);
                                lstVars.Add(constraint.Left);
                            }
                            else
                            {
                                AddVariableAndPushDfDvNode(lstVars, GetDfDvNode(node, constraint, constraint.Left, node.VariableToEval));
                            }
                        }
                    }
                } // endif !node.ChildrenHaveBeenPushed

                // If we just pushed one or more nodes, loop back up and "recurse" into them.
                if (this.allConstraints.DfDvStack.Count > prevStackCount)
                {
                    continue;
                }

                // We are at a non-leaf node and have "recursed" through all its descendents, so we're done with it.
                Debug.Assert(this.allConstraints.DfDvStack.Peek() == node, "DfDvStack.Peek() should be 'node'");
                this.allConstraints.RecycleDfDvNode(this.allConstraints.DfDvStack.Pop());
            } // endwhile stack is not empty
        }

        void TransferConnectedVariables(Block newSplitBlock, Variable varToEval, Variable varDoneEval)
        {
            GetConnectedVariables(newSplitBlock.Variables, varToEval, varDoneEval);
            int numVarsToMove = newSplitBlock.Variables.Count;                       // cache for perf

            // The constraints transferred to the new block need to have any stale cycle-detection values cleared out.
            newSplitBlock.Debug_ClearDfDv(true /* forceFull */);

            // Avoid the creation of an inner loop on List<T>.Remove (which does linear scan and shift
            // to preserve the order of members).  We don't care about variable ordering within the block
            // so we can just repeatedly swap in the end one over whichever we're removing.
            for (int moveIndex = 0; moveIndex < numVarsToMove; ++moveIndex)
            {
                newSplitBlock.Variables[moveIndex].Block = newSplitBlock;
            }

            // Now iterate from the end and swap in the last one we'll keep over the ones we'll remove.
            int lastKeepIndex = this.Variables.Count - 1;
            for (int currentIndex = this.Variables.Count - 1; currentIndex >= 0; --currentIndex)
            {
                Variable currentVariable = this.Variables[currentIndex];
                if (currentVariable.Block == newSplitBlock)
                {
                    if (currentIndex < lastKeepIndex)
                    {
                        // Swap in the one from the end.
                        this.Variables[currentIndex] = this.Variables[lastKeepIndex];
                    }
                    --lastKeepIndex;
                }
            } // end for each var to keep

            // Now remove the end slots we're not keeping.  lastKeepIndex is -1 if we are removing all variables.
            Debug.Assert(numVarsToMove == this.Variables.Count - lastKeepIndex - 1, "variable should not be found twice (probable cycle-detection problem");
            this.Variables.RemoveRange(lastKeepIndex + 1, this.Variables.Count - lastKeepIndex - 1);

            if (0 == this.Variables.Count)
            {
                // This is probably due to unsatisfiable constraints; we've transferred all the variables,
                // so just don't split at all; move the variables back into the current block rather than
                // leaving an empty block in the list.  Caller will detect the empty newSplitBlock and ignore it.
                for (int moveIndex = 0; moveIndex < numVarsToMove; ++moveIndex)
                {
                    var variableToMove = newSplitBlock.Variables[moveIndex];
                    this.Variables.Add(variableToMove);
                    variableToMove.Block = this;
                }
                newSplitBlock.Variables.Clear();
            }
        } // end TransferConnectedVariables()

        [Conditional("TEST_MSAGL")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        internal void Debug_PostMerge(Block blockFrom)
        {
#if TEST_MSAGL
            // If blockFrom's DfDv-cycle detection value was higher than ours, we need to set ours to
            // that value, to avoid running into stale values.
            if (blockFrom.idDfDv > this.idDfDv)
            {
                this.idDfDv = blockFrom.idDfDv;
            }
#endif // TEST_MSAGL
        }
    }
}