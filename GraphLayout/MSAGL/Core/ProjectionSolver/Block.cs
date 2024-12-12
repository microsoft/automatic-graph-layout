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


        // The dummy parent node that saves us from having to do null testing.
        DfDvNode dfDvDummyParentNode;

        internal void ComputeDfDv(Variable initialVarToEval)
        {

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
                Debug.Assert(this.allConstraints.DfDvStack.Peek() == node, "DfDvStack.Peek() should be 'node'");
                this.allConstraints.DfDvStack.Pop();
                ProcessDfDvLeafNode(node);
                if (node == firstNode)
                {
                    Debug.Assert(0 == this.allConstraints.DfDvStack.Count, "Leftovers in DfDvStack on completion of loop");
                    break;
                }
            } // endwhile stack is not empty


            // From the definition of the optimal position of all variables that satisfies the constraints, the
            // final value of this should be zero.  Think of the constraints as rigid rods and the variables as
            // the attachment points of the rods.  Also think of those attachment points as having springs connecting
            // them to their ideal positions.  In order for the whole system to be at rest (i.e. at the optimal
            // position) the net force on the right-hand side of each rod must be equal and opposite to the net
            // force on the left-hand side.  Thus, the final return value of compute_dfdv is the sum of the entire
            // left-hand side and right-hand side - which should cancel (within rounding error).
            ////
            // The dummyConstraint "rolls up" to its parent which is itself, thus it will be twice the leftover.
        } // end ComputeDfDv()

        void ProcessDfDvLeafNode(DfDvNode node)
        {
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

            // See if this node found the target variable.
            CheckForConstraintPathTarget(node);

            // We're done with this node.
            this.allConstraints.RecycleDfDvNode(node);
        }

        
        // Directly evaluate a leaf node rather than defer it to stack push/pop.
        void ProcessDfDvLeafNodeDirectly(DfDvNode node)
        {
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
            PushOnDfDvStack(node);
        }


        // Called by RecurseGetConnectedVariables.
        void AddVariableAndPushDfDvNode(List<Variable> lstVars, DfDvNode node)
        {
            lstVars.Add(node.VariableToEval);
            PushOnDfDvStack(node);
        }

        void PushOnDfDvStack(DfDvNode node)
        {
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



        internal void Expand(Constraint violatedConstraint)
        {
            
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


            ComputeDfDv(violatedConstraint.Left);


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
                    if (pathItem.IsForward 
                            && ((null == minLagrangianConstraint) || (pathItem.Constraint.Lagrangian < minLagrangianConstraint.Lagrangian)))
                    {
                        if (!pathItem.Constraint.IsEquality)
                        {
                            minLagrangianConstraint = pathItem.Constraint;
                        }
                    }
                }
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

        } // end Expand()

        internal Block Split(bool isQpsc
)
        {

            if (isQpsc)
            {
                // In the Qpsc case, we've modified current positions in PreProject() so need to update them here.
                this.UpdateReferencePos();
            }

            // If there is only one variable there's nothing to split.
            if (this.Variables.Count < 2)
            {
                return null;
            }

            Constraint minLagrangianConstraint = null;

            // Pick a variable from the active constraint list - it doesn't matter which; any variable in
            // the block is active (except for the initial one-var-per-block case), so ComputeDfDv will evaluate
            // it along the active path.  Eventually all variables needing to be repositioned will be part of
            // active constraints; even if SplitBlocks eventually happens, if the variable must be repositioned
            // again (via the global-constraint-maxviolation check) its constraint will be reactivated.
            // By the same token, ExpandBlock and SplitBlocks implicitly address/optimize all situations
            // (or close enough) where an Active (i.e. == Gap) constraint would be better made inactive 
            // and the gap grown.

            ComputeDfDv(this.Variables[0]);


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


            return this.SplitOnConstraint(minLagrangianConstraint
                );
        }

        internal Block SplitOnConstraint(Constraint constraintToSplit
            )
        {
            // We have a split point.  Remove that constraint from our active list and transfer it and all
            // variables to its right to a new block.  As mentioned above, all variables and associated
            // constraints in the block are active, and the block split and recalc of reference positions
            // doesn't change the actual positions of any variables.
            this.allConstraints.DeactivateConstraint(constraintToSplit);
            var newSplitBlock = new Block(null, this.allConstraints
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
            this.TransferConnectedVariables(newSplitBlock, constraintToSplit.Right, constraintToSplit.Left);
            if (newSplitBlock.Variables.Count > 0)
            {
                // We may have removed the first variable so fully recalculate the reference position.
                this.UpdateReferencePos();

                // The new block's sums were not updated as its variables were added directly to its
                // variables list, so fully recalculate.
                newSplitBlock.UpdateReferencePos();
            }
            else
            {
                // If there were unsatisfiable constraints, we may have tried to transfer all variables;
                // in that case we simply ignored the transfer operation and left all variables in 'this' block.
                // Return NULL so Solver.SplitBlocks knows we didn't split.
                newSplitBlock = null;
            }

            return newSplitBlock;
        } // end Split()

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
                        "Block Reference Position component is infinite" // TEST_MSAGL
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
            RecurseGetConnectedVariables(lstVars, varToEval, varDoneEval);
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

    }
}