// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Solver.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL main class Projection Solver.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

// Remove this from project build and uncomment here to selectively enable per-class.
//#define VERBOSE

// Other #define values used; these can be set in project settings.
//  CACHE_STATS - outputs some summary Block and Cache information.
//  VERIFY_MIN_CONSTRAINT - VERIFY || VERBOSE enables this for verification.

//#define Inline_Violation

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    using System.Linq;

    /// <summary>
    /// A Solver is the driving class that collects Variables and Constraints and then generates a
    /// solution that minimally satisfies the constraints.
    /// </summary>
    public class Solver
    {
        #region Internal members

        // Notes about hierarchy:
        //  1.  Each Variable is initially assigned to its own block, and subsequently MergeBlocks()
        //      and SplitBlocks() may change its block membership, but the variable is always in one
        //      and only one block, so we enumerate variables by enumerating blocks and variables.
        //  2.  The list of (active and inactive) constraints is within each block's variable list;
        //      we simply enumerate each block's LeftConstraints.
        private readonly BlockVector allBlocks = new BlockVector();

        // To speed up SearchAllConstraints, have a single Array in addition to the per-block
        // variable Lists (Array indexing is faster than List).
        private readonly ConstraintVector allConstraints = new ConstraintVector();
        private int numberOfConstraints;                // Updated on AddConstraint; used to create AllConstraints
        private int numberOfVariables;

        // Also for speed, a separate list of Equality constraints (which we expect to be fairly rare).
        private readonly List<Constraint> equalityConstraints = new List<Constraint>();

        private struct ConstraintListForVariable
        {
            // All constraints.
            internal readonly List<Constraint> Constraints;

            // The number of Constraints that are LeftConstraints for the variable keying this object.
            internal readonly int NumberOfLeftConstraints;

            internal ConstraintListForVariable(List<Constraint> constraints, int numberOfLeftConstraints)
            {
                this.Constraints = constraints;
                this.NumberOfLeftConstraints = numberOfLeftConstraints;
            }
        }

        // Also for speed, store variables -> constraint list while we load, then convert this into
        // arrays when we call Solve().  The members are List of constraints, and number of Left constraints.
        private Dictionary<Variable, ConstraintListForVariable> loadedVariablesAndConstraintLists
                            = new Dictionary<Variable, ConstraintListForVariable>();

        // We bundle up the constraints first, so we can use Array rather than List iteration for speed. 
        // To make the code cleaner (not having to check for NULL all over the place) use an empty List/Array
        // for Variables' constraint Lists/Arrays, and to help memory efficiency, use a single object.
        private readonly Constraint[] emptyConstraintList = new Constraint[0];         // For long-lived Variable objects

        // For UpdateConstraint(), we want to buffer up the changes so variable values are not changed
        // by doing an immediate Block.Split which updates the Block's ReferencePos.
        private readonly List<KeyValuePair<Constraint, double>> updatedConstraints = new List<KeyValuePair<Constraint, double>>();

        // For caching violations to improve GetMaxViolatedConstraint performance.
        private ViolationCache violationCache = new ViolationCache();
        private Block lastModifiedBlock;
        private int violationCacheMinBlockCutoff;
        private bool hasNeighbourPairs;
#if CACHE_STATS
        private ViolationCache.CacheStats cacheStats = new ViolationCache.CacheStats();
#endif // CACHE_STATS

        private uint nextVariableOrdinal;
#if VERIFY || VERBOSE
        private uint nextNewBlockOrdinal;
        private uint nextNewConstraintOrdinal;
#endif // VERIFY || VERBOSE

        // May be overridden by the caller's Parameters object passed to Solve.
        private Parameters solverParams = new Parameters();

        // Solution results - will be cloned to return to caller.
        private Solution solverSolution = new Solution();

        private bool IsQpsc { get { return this.hasNeighbourPairs || this.solverParams.Advanced.ForceQpsc; } }

        // For execution-time limit.
        private Stopwatch timeoutStopwatch;

        #endregion // Internal members

        /// <summary>
        /// Add a Variable (for example, wrapping a node on one axis of the graph) to the Solver.
        /// </summary>
        /// <param name="userData">a tag or other user data - can be null</param>
        /// <param name="desiredPos">The position of the variable, such as the coordinate of a node along one axis.</param>
        /// <returns>The created variable</returns>
        public Variable AddVariable(Object userData, double desiredPos)
        {
            return AddVariable(userData, desiredPos, /*weight:*/ 1.0, /*scale:*/ 1.0);
        }
        /// <summary>
        /// Add a Variable (for example, wrapping a node on one axis of the graph) to the Solver.
        /// </summary>
        /// <param name="userData">a tag or other user data - can be null</param>
        /// <param name="desiredPos">The position of the variable, such as the coordinate of a node along one axis.</param>
        /// <param name="weight">The weight of the variable (makes it less likely to move if the weight is high).</param>
        /// <returns></returns>
        public Variable AddVariable(Object userData, double desiredPos, double weight)
        {
            return AddVariable(userData, desiredPos, weight, /*scale:*/ 1.0);
        }

        /// <summary>
        /// Add a Variable (for example, wrapping a node on one axis of the graph) to the Solver.
        /// </summary>
        /// <param name="userData">a tag or other user data - can be null</param>
        /// <param name="desiredPos">The position of the variable, such as the coordinate of a node along one axis.</param>
        /// <param name="weight">The weight of the variable (makes it less likely to move if the weight is high).</param>
        /// <param name="scale">The scale of the variable, for improving convergence.</param>
        /// <returns>The created variable</returns>
        public Variable AddVariable(Object userData, double desiredPos, double weight, double scale)
        {
            // @@DCR "Incremental Solving": For now we disallow this; if we support it, we'll need to
            // retain loadedVariablesAndConstraintLists, store up the added Variables (TryGetValue and if that fails add 
            // the existing variable, then iterate through variables with new Constraints and replace the arrays.
            // Also remember to check for emptyConstraintList - don't add to it.
            if (!this.allConstraints.IsEmpty)
            {
                throw new InvalidOperationException(
#if TEST_MSAGL
                        "Cannot add Variables or Constraints once Solve() has been called"
#endif // TEST_MSAGL
                    );
            }

            var varNew = new Variable(this.nextVariableOrdinal++, userData, desiredPos, weight, scale);
            var block = new Block(varNew, this.allConstraints
#if VERIFY || VERBOSE
                                , ref this.nextNewBlockOrdinal
#endif // VERIFY || VERBOSE
                );
            varNew.Block = block;
            this.allBlocks.Add(block);
            ++this.numberOfVariables;

            // Initialize the variable in the dictionary with a null list and zero left constraints.
            this.loadedVariablesAndConstraintLists[varNew] = new ConstraintListForVariable(new List<Constraint>(), 0);
            return varNew;
        } // end AddVariable()

        /// <summary>
        /// Must be called before Solve() if the caller has updated variable Initial positions; this
        /// reconciles internals such as Block.ReferencePos.
        /// </summary>
        public void UpdateVariables()
        {
            // Although the name is "UpdateVariables", that's just for the caller to not need to know
            // about the internals; this really is updating the blocks after the variables have already
            // been updated one at a time. (This doesn't need to be called if constraints are re-gapped
            // while variable positions are unchanged; Solve() checks for that).
            foreach (Block block in this.allBlocks.Vector)
            {
                block.UpdateReferencePos();
            }
        } // end UpdateVariables()

        /// <summary>
        /// This enumerates all Variables created by AddVariable.
        /// </summary>
        public IEnumerable<Variable> Variables
        {
            get
            {
                return this.allBlocks.Vector.SelectMany(block => block.Variables);
            }
        }

        /// <summary>
        /// The number of variables added to the Solver.
        /// </summary>
        public int VariableCount
        {
            get { return this.numberOfVariables; }
        }

        /// <summary>
        /// This enumerates all Constraints created by AddConstraint (which in turn may have
        /// been called from OverlapRemoval.ConstraintGenerator.Generate()).
        /// </summary>
        public IEnumerable<Constraint> Constraints
        {
            get
            {
                if (!this.allConstraints.IsEmpty)
                {
                    // Solve() has been called.
                    foreach (Constraint constraint in this.allConstraints.Vector)
                    {
                        yield return constraint;
                    }
                }
                else
                {
                    // Solve() has not yet been called.
                    foreach (var variable in this.loadedVariablesAndConstraintLists.Keys)
                    {
                        ConstraintListForVariable constraintsForVar = this.loadedVariablesAndConstraintLists[variable];
                        if (null != constraintsForVar.Constraints)
                        {
                            // Return all variables in the LeftConstraints list for each variable.
                            int numConstraints = constraintsForVar.Constraints.Count;     // Cache for perf
                            for (int ii = 0; ii < numConstraints; ++ii)
                            {
                                Constraint constraint = constraintsForVar.Constraints[ii];
                                if (variable == constraint.Left)
                                {
                                    yield return constraint;
                                }
                            }
                        }
                    }
                } // endifelse (!AllConstraints.Empty)
            }
        } // end Constraints property

        /// <summary>
        /// The number of constraints added to the Solver.
        /// </summary>
        public int ConstraintCount
        {
            get { return this.numberOfConstraints; }
        }

        /// <summary>
        /// Add a constraint 'left + gap' is equal to right
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="gap"></param>
        /// <returns></returns>
        public Constraint AddEqualityConstraint(Variable left, Variable right, double gap)
        {
            return AddConstraint(left, right, gap, true);
        }

        /// <summary>
        /// Add a constraint 'left + gap' is less than or equal to 'right'
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="gap">The gap required between the variables.</param>
        /// <param name="isEquality"></param>
        /// <returns>The new constraint.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public Constraint AddConstraint(Variable left, Variable right, double gap, bool isEquality)
        {
            // @@DCR "Incremental Solving": See notes in AddVariable; for now, this is disallowed.
            if (!this.allConstraints.IsEmpty)
            {
                throw new InvalidOperationException(
#if TEST_MSAGL
                        "Cannot add Variables or Constraints once Solve() has been called"
#endif // TEST_MSAGL
                        );
            }

            if (left == right)
            {
                throw new ArgumentException(
#if TEST_MSAGL
                        "Cannot add a constraint between a variable and itself"
#endif // TEST_MSAGL
                        );
            }

            // Get the dictionary entries so we can store these until Solve() is called.  kvp.Key == lstConstraints,
            // kvp.Value == number of constraints in lstConstraints that are LeftConstraints for the variable.
            // kvpConstraintsForVar(Left|Right) are bidirectional for that variable, but we're operating only on
            // varLeft's LeftConstraints and varRight's RightConstraints; this is slightly more complicated logic
            // than just having two Lists, but for large numbers of variables, having all constraints in a single
            // list is more memory-efficient.
            ConstraintListForVariable constraintsForLeftVar = this.loadedVariablesAndConstraintLists[left];
            ConstraintListForVariable constraintsForRightVar = this.loadedVariablesAndConstraintLists[right];

#if NO_DUP_CONSTRAINTS
            // Ignore duplicates.  This is only done on load, and it's not likely we'll have a huge
            // number of constraints per variable, so the enumeration approach is fine perf-wise.
            // Warning: needs testing if used.
            int leftVarConstraintCount = constraintsForLeftVar.Constraints.Count;           // cache for perf
            for (int constraintIndex = 0; constraintIndex < leftVarConstraintCount; ++constraintIndex) {
                Constraint existingConstraint = constraintsForLeftVar.Constraints[constraintIndex];
                if ((existingConstraint.Left == left) && (existingConstraint.Right == right)) {
                    if (existingConstraint.Gap < gap)
                    {
                        existingConstraint.UpdateGap(gap);
                    }
                    return existingConstraint;
                }
            }
#endif

            // Now create the new constraint and update the structures.  For varLeft, we must also update the
            // left-variable count and that requires another lookup to update the structure in the Dictionary
            // since it's a value type so a copy was returned by-value from Dictionary lookup.
            var constraint = new Constraint(left, right, gap, isEquality
#if VERIFY || VERBOSE
                                    , this.nextNewConstraintOrdinal++
#endif // VERIFY || VERBOSE
                );

            // Structure update requires replacing the full structure.
            this.loadedVariablesAndConstraintLists[left] = new ConstraintListForVariable(
                            constraintsForLeftVar.Constraints, constraintsForLeftVar.NumberOfLeftConstraints + 1);
            constraintsForLeftVar.Constraints.Add(constraint);
            constraintsForRightVar.Constraints.Add(constraint);
            ++this.numberOfConstraints;
            if (isEquality)
            {
                this.equalityConstraints.Add(constraint);
            }
            return constraint;
        }

        /// <summary>
        /// Add a constraint 'left + gap' is less than or equal to 'right'
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="gap">The gap required between the variables.</param>
        /// <returns>The new constraint.</returns>
        public Constraint AddConstraint(Variable left, Variable right, double gap)
        {
            return AddConstraint(left, right, gap, false /*isEquality*/);
        }

        /// <summary>
        /// Register an update to a constraint's gap; this defers the actual update until Solve() is called.
        /// </summary>
        /// <param name="constraint">The constraint to update</param>
        /// <param name="gap">The new gap</param>
        public void SetConstraintUpdate(Constraint constraint, double gap)
        {
            ValidateArg.IsNotNull(constraint, "constraint");

            // Defer this to the Solve() call, so the variables' positions are not altered by doing a
            // Block.Split here (which updates Block.ReferencePos, upon which Variable.(Scaled)ActualPos relies).
            if (gap != constraint.Gap)
            {
                this.updatedConstraints.Add(new KeyValuePair<Constraint, double>(constraint, gap));
            }
        }

        /// <summary>
        /// Add a pair of connected variables for goal functions of the form (x1-x2)^2.  These are
        /// minimally satisfied, along with the default (x-i)^2 goal function, while also satisfying
        /// all constraints.
        /// </summary>
        /// <param name="variable1">The first variable</param>
        /// <param name="variable2">The second variable</param>
        /// <param name="relationshipWeight">The weight of the relationship</param>
        public void AddNeighborPair(Variable variable1, Variable variable2, double relationshipWeight)
        {
            ValidateArg.IsNotNull(variable1, "variable1");
            ValidateArg.IsNotNull(variable2, "variable2");
            if ((relationshipWeight <= 0) || double.IsNaN(relationshipWeight) || double.IsInfinity(relationshipWeight))
            {
                throw new ArgumentOutOfRangeException("relationshipWeight"
#if TEST_MSAGL
                        , "Invalid Neighbor Weight"
#endif // TEST_MSAGL
                    );
            }
            if (variable1 == variable2)
            {
                throw new InvalidOperationException(
#if TEST_MSAGL
                        "Cannot make a Variable a neighbor of itself"
#endif // TEST_MSAGL
                    );
            }
            variable1.AddNeighbor(variable2, relationshipWeight);
            variable2.AddNeighbor(variable1, relationshipWeight);
            this.hasNeighbourPairs = true;
        } // end AddNeighborPair()

        /// <summary>
        /// Sets Variable.ActualPos to the positions of the Variables that minimally satisfy the constraints
        /// along this axis.  This overload uses default solution parameter values.
        /// </summary>
        /// <returns>A Solution object.</returns>
        public Solution Solve()
        {
            return Solve(null);
        }

        /// <summary>
        /// Sets Variable.ActualPos to the positions of the Variables that minimally satisfy the constraints
        /// along this axis.  This overload takes a parameter specification.
        /// </summary>
        /// <param name="solverParameters">Solution-generation options.</param>
        /// <returns>The only failure condition is if there are one or more unsatisfiable constraints, such as cycles
        ///         or mutually exclusive equality constraints; if these are encountered, a list of lists of these 
        ///         constraints is returned, where each list contains a single cycle, which may be of length one for
        ///         unsatisfiable equality constraints.  Otherwise, the return value is null.</returns>
        public Solution Solve(Parameters solverParameters)
        {
            if (null != solverParameters)
            {
                this.solverParams = (Parameters)solverParameters.Clone();
            }

            // Reset some parameter defaults to per-solver-instance values.
            if (this.solverParams.OuterProjectIterationsLimit < 0)
            {
                // If this came in 0, it stays that way, and there is no limit.  Otherwise, set it to a value
                // reflecting the expectation of convergence roughly log-linearly in the number of variables.
#if SHARPKIT //https://github.com/SharpKit/SharpKit/issues/4 integer rounding issue
                this.solverParams.OuterProjectIterationsLimit = 100 * (((int)Math.Log(this.numberOfVariables, 2.0)) + 1);
#else
                this.solverParams.OuterProjectIterationsLimit = 100 * ((int)Math.Log(this.numberOfVariables, 2.0) + 1);
#endif
            }
            if (this.solverParams.InnerProjectIterationsLimit < 0)
            {
                // If this came in 0, it stays that way, and there is no limit.  Otherwise, assume that for
                // any pass, each constraint may be violated (most likely this happens only on the first pass),
                // and add some extra based upon constraint count.  Now that we split and retry on unsatisfied
                // constraints, assume that any constraint may be seen twice on a pass.
#if SHARPKIT //https://github.com/SharpKit/SharpKit/issues/4 integer rounding issue
                this.solverParams.InnerProjectIterationsLimit = (this.numberOfConstraints * 2) + (100 * (((int)Math.Log(this.numberOfConstraints, 2.0)) + 1));
#else
                this.solverParams.InnerProjectIterationsLimit = (this.numberOfConstraints * 2) + (100 * ((int)Math.Log(this.numberOfConstraints, 2.0) + 1));
#endif
            }

            // ReSolving can be done for updated constraints.
            bool isReSolve = !this.allConstraints.IsEmpty;
            CheckForUpdatedConstraints();

            this.solverSolution = new Solution { MinInnerProjectIterations = int.MaxValue };
            this.allConstraints.MaxConstraintTreeDepth = 0;
            this.allConstraints.SolverParameters = this.solverParams;

            //
            // First set up all the internal stuff we'll use for solutions.
            //
#if CACHE_STATS
            cacheStats.Clear();
#endif // CACHE_STATS

            // If no constraints have been loaded, there's nothing to do.  Two distinct variables
            // are required to create a constraint, so this also ensures a minimum number of variables.
            if (0 == this.numberOfConstraints)
            {
                // For Qpsc, we may have neighbours but no constraints.
                if (!this.IsQpsc)
                {
                    return (Solution)this.solverSolution.Clone();
                }
            }
            else if (!isReSolve)
            {
                SetupConstraints();
            }

            // This is the number of unsatisfiable constraints encountered.
            this.allConstraints.NumberOfUnsatisfiableConstraints = 0;

            // Merge Equality constraints first.  These do not do any constraint-splitting, and thus
            // remain in the same blocks, always satisfied, regardless of whether we're solving the full
            // Qpsc or the simpler loop.
            MergeEqualityConstraints();

            // Prepare for timeout checking.
            if (this.solverParams.TimeLimit > 0)
            {
                this.timeoutStopwatch = new Stopwatch();
                this.timeoutStopwatch.Start();
            }

            //
            // Done with initial setup.  Now if we have neighbour pairs, we do the full SolveQpsc logic
            // complete with Gradient projection.  Otherwise, we have a much simpler Project/Split loop.
            //
            if (this.IsQpsc)
            {
                this.SolveQpsc();
            }
            else
            {
                this.SolveByStandaloneProject();
                this.CalculateStandaloneProjectGoalFunctionValue();
            }

            // We initialized this to int.MaxValue so make sure it's sane if we didn't complete a Project iteration.
            if (this.solverSolution.MinInnerProjectIterations > this.solverSolution.MaxInnerProjectIterations)
            {
                // Probably this is 0.
                this.solverSolution.MinInnerProjectIterations = this.solverSolution.MaxInnerProjectIterations;
            }

#if CACHE_STATS
            cacheStats.Print();
            System.Diagnostics.Debug.WriteLine("  NumFinalBlocks = {0}, MinCacheBlocks = {1}, MaxCacheSize = {2}",
                                allBlocks.Count, violationCacheMinBlockCutoff, ViolationCache.MaxConstraints);
#endif // CACHE_STATS

            // Done.  Caller will copy each var.ActualPos back to the Nodes.  If we had any unsatisfiable
            // constraints, copy them back out to the caller.
            this.solverSolution.NumberOfUnsatisfiableConstraints = this.allConstraints.NumberOfUnsatisfiableConstraints;
#if BLOCK_STATS
            int minBlockVars = this.numberOfVariables;
            int maxBlockVars = 0;
            foreach (Block block in allBlocks.Vector) {
                if (minBlockVars > block.Variables.Count) {
                    minBlockVars = block.Variables.Count;
                }
                if (maxBlockVars < block.Variables.Count) {
                    maxBlockVars = block.Variables.Count;
                }
            } // endforeach block

            System.Diagnostics.Debug.WriteLine("Num final Blocks: {0}, Min Block Vars: {1}, Max Block Vars: {2}",
                    allBlocks.Count, minBlockVars, maxBlockVars);
#endif // BLOCK_STATS
            this.solverSolution.MaxConstraintTreeDepth = this.allConstraints.MaxConstraintTreeDepth;
            return (Solution)this.solverSolution.Clone();
        } // end Solve()

        private void CheckForUpdatedConstraints()
        {
            if (0 == this.updatedConstraints.Count)
            {
                return;
            }
            Debug.Assert(!this.allConstraints.IsEmpty, "Cannot have updated constraints if AllConstraints is empty.");

            // For Qpsc, all Block.ReferencePos values are based upon Variable.DesiredPos values, and the latter
            // have been restored from what they were on the last Qpsc iteration to their initial values).
            bool mustReinitializeBlocks = this.IsQpsc;
            foreach (KeyValuePair<Constraint, double> kvpUpdate in this.updatedConstraints)
            {
                // Update the constraint, then split its block if it's active, so the next call to Solve()
                // will start the merge/split cycle again.
                Constraint constraint = kvpUpdate.Key;
                constraint.UpdateGap(kvpUpdate.Value);
                if (!mustReinitializeBlocks && !constraint.IsEquality)
                {
                    SplitOnConstraintIfActive(constraint);
                    continue;
                }

                // Equality constraints must always be evaluated first and never split.
                // If we have updated one we must reinitialize the block structure.
                mustReinitializeBlocks = true;
            }
            this.updatedConstraints.Clear();

            if (mustReinitializeBlocks)
            {
                this.ReinitializeBlocks();
            }
        }

        private void SplitOnConstraintIfActive(Constraint constraint)
        {
            if (constraint.IsActive)
            {
                // Similar handling as in SplitBlocks, except that we know which constraint we're splitting on.
                Block newSplitBlock = constraint.Left.Block.SplitOnConstraint(constraint
#if VERIFY || VERBOSE
                                                                            , ref this.nextNewBlockOrdinal
#endif // VERIFY || VERBOSE
                    );
                if (null != newSplitBlock)
                {
                    this.allBlocks.Add(newSplitBlock);
                }
            } // endif constraint.IsActive
        }

        private void SetupConstraints()
        {
            // Optimize the lookup in SearchAllConstraints; create an array (which has faster
            // iteration than List).
            this.allConstraints.Create(this.numberOfConstraints);

            foreach (var variable in this.loadedVariablesAndConstraintLists.Keys)
            {
                ConstraintListForVariable constraintsForVar = this.loadedVariablesAndConstraintLists[variable];
                List<Constraint> constraints = constraintsForVar.Constraints;
                int numAllConstraints = 0;
                int numLeftConstraints = 0;
                int numRightConstraints = 0;
                if (null != constraints)
                {
                    numAllConstraints = constraints.Count;
                    numLeftConstraints = constraintsForVar.NumberOfLeftConstraints;
                    numRightConstraints = numAllConstraints - numLeftConstraints;
                }

                // Create the Variable's Constraint arrays, using the single emptyConstraintList for efficiency.
                Constraint[] leftConstraints = this.emptyConstraintList;
                if (0 != numLeftConstraints)
                {
                    leftConstraints = new Constraint[numLeftConstraints];
                }
                Constraint[] rightConstraints = this.emptyConstraintList;
                if (0 != numRightConstraints)
                {
                    rightConstraints = new Constraint[numRightConstraints];
                }
                variable.SetConstraints(leftConstraints, rightConstraints);

                // Now load the Variables' Arrays.  We're done with the loadedVariablesAndConstraintLists lists after this.
                int leftConstraintIndex = 0;
                int rightConstraintIndex = 0;
                for (int loadedConstraintIndex = 0; loadedConstraintIndex < numAllConstraints; ++loadedConstraintIndex)
                {
                    // numAllConstraints is 0 if null == constraints.
// ReSharper disable PossibleNullReferenceException
                    Constraint loadedConstraint = constraints[loadedConstraintIndex];
// ReSharper restore PossibleNullReferenceException
                    if (variable == loadedConstraint.Left)
                    {
                        leftConstraints[leftConstraintIndex++] = loadedConstraint;
                    }
                    else
                    {
                        rightConstraints[rightConstraintIndex++] = loadedConstraint;
                    }
                }
                Debug.Assert(leftConstraintIndex == numLeftConstraints, "leftConstraintIndex must == numLeftConstraints");
                Debug.Assert(rightConstraintIndex == numRightConstraints, "rightConstraintIndex must == numRightConstraints");

                // Done with per-variable constraint loading.  Now load the big list of all constraints.
                // All constraints are stored in a LeftConstraints array (and duplicated in a RightConstraints
                // array), so just load the LeftConstraints into AllConstraints. Array.Foreach is optimized.
                foreach (var constraint in variable.LeftConstraints)
                {
                    this.allConstraints.Add(constraint);
                }
            }
            this.allConstraints.Debug_AssertIsFull();

            // Done with the dictionary now.
            this.loadedVariablesAndConstraintLists.Clear();
            this.loadedVariablesAndConstraintLists = null;

            // If we don't have many blocks then the caching optimization's overhead may outweigh
            // its benefit. Similarly, after blocks have merged past a certain point it's faster to
            // just enumerate them all.  Initialize this to off.
            this.violationCacheMinBlockCutoff = int.MaxValue;
            if (this.solverParams.Advanced.UseViolationCache && (this.solverParams.Advanced.ViolationCacheMinBlocksDivisor > 0))
            {
                this.violationCacheMinBlockCutoff = Math.Min(this.allBlocks.Count / this.solverParams.Advanced.ViolationCacheMinBlocksDivisor,
                                                this.solverParams.Advanced.ViolationCacheMinBlocksCount);
            }
#if VERBOSE
            DumpState("Initial");
#endif // VERBOSE
        }

        private void SolveByStandaloneProject()
        {
            // Loop until we have no constraints with violations and no blocks are split.
            // Note:  this functions differently from the loop-termination test in SolveQpsc, which tests the
            // total movement resulting from Project() against some epsilon.  We do this differently here because
            // we're not doing the Gradient portion of SolveQpsc, so we'll just keep going as long as we have any
            // violations greater than the minimum violation we look for in GetMaxViolatedConstraint (and as long
            // as we don't split any blocks whether or not we find such a violation).
            for (; ; )
            {
                // Don't check the return of Project; defer the termination check to SplitBlocks.
                // This also examines limits post-Project; because it happens pre-SplitBlocks it ensures
                // a feasible stopping state.
                bool violationsFound;
                if (!this.RunProject(out violationsFound))
                {
                    return;
                }

                // If SplitBlocks doesn't find anything to split then Project would do nothing.
                if (!SplitBlocks())
                {
                    break;
                }
            }
        }

        private bool RunProject(out bool violationsFound)
        {
            ++this.solverSolution.OuterProjectIterations;
            violationsFound = Project();

            // Examine limits post-Project but pre-SplitBlocks to ensure a feasible stopping state.
            return !CheckForLimitsExceeded();
        }

        private bool CheckForLimitsExceeded()
        {
            if (null != this.timeoutStopwatch)
            {
                if (this.timeoutStopwatch.ElapsedMilliseconds >= this.solverParams.TimeLimit)
                {
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine("Solve() aborting due to time limit: max {0}, elapsed {1}",
                                    solverParams.TimeLimit, timeoutStopwatch.ElapsedMilliseconds);
#endif // VERBOSE
                    this.solverSolution.TimeLimitExceeded = true;
                    return true;
                }
            }
            if (this.solverParams.OuterProjectIterationsLimit > 0)
            {
                if (this.solverSolution.OuterProjectIterations >= this.solverParams.OuterProjectIterationsLimit)
                {
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine("Solve() aborting due to max outer iterations: max {0}",
                                    solverParams.OuterProjectIterationsLimit);
#endif // VERBOSE
                    this.solverSolution.OuterProjectIterationsLimitExceeded = true;
                    return true;
                }
            }
            if (this.solverSolution.InnerProjectIterationsLimitExceeded)
            {
                return true;
            }
            return false;
        }

        private void CalculateStandaloneProjectGoalFunctionValue()
        {
            // Fill in the non-Qpsc Goal function value.  See Qpsc.HasConverged for details; this is a
            // streamlined form of (x'Ax)/2 + bx here, where A has only the diagonals (as there are no
            // neighbours) with 2*wi and b is a vector of -2*wi*di, and x is current position.
            this.solverSolution.GoalFunctionValue = 0.0;
            int numBlocks = this.allBlocks.Count;                // cache for perf
            for (int ii = 0; ii < numBlocks; ++ii)
            {
                Block block = this.allBlocks[ii];
                int numVars = block.Variables.Count;
                for (int jj = 0; jj < numVars; ++jj)
                {
                    var variable = block.Variables[jj];

                    // (x'Ax)/2
                    this.solverSolution.GoalFunctionValue += variable.Weight * variable.ActualPos * variable.ActualPos;
                    // +bx
                    this.solverSolution.GoalFunctionValue -= 2 * variable.Weight * variable.DesiredPos * variable.ActualPos;
                }
            }
        }

        // Implements the full solve_QPSC from the Ipsep_Cola and Scaling papers.
        private void SolveQpsc()
        {
            this.solverSolution.AlgorithmUsed = this.solverParams.Advanced.ScaleInQpsc ? SolverAlgorithm.QpscWithScaling : SolverAlgorithm.QpscWithoutScaling;
            if (!QpscMakeFeasible())
            {
                return;
            }

            // Initialize the Qpsc state, which also sets the scale for all variables (if we are scaling).
            var qpsc = new Qpsc(this.solverParams, this.numberOfVariables);
            foreach (var block in this.allBlocks.Vector)
            {
                foreach (var variable in block.Variables)
                {
                    qpsc.AddVariable(variable);
                }
            }
            qpsc.VariablesComplete();
            this.ReinitializeBlocks();
            this.MergeEqualityConstraints();
            VerifyConstraintsAreFeasible();

            // Iterations
            bool foundSplit = false;
            bool foundViolation = false;
            for (; ; )
            {
                //
                // Calculate initial step movement.  We assume there will be some movement needed
                // even on the first pass in the vast majority of cases.  This also tests convergence
                // of the goal-function value; if it is sufficiently close to the previous iteration's
                // result and the previous iteration did not split or encounter a violation, we're done.
                //
                if (!qpsc.PreProject() && !foundSplit && !foundViolation)
                {
                    break;
                }

                //
                // Split the blocks (if this the first time through the loop then all variables are in their
                // own block except for any equality constraints, which we don't split; but we still need to
                // have UpdateReferencePos called).
                //
                foundSplit = SplitBlocks();

                // Examine limits post-Project to ensure a feasible stopping state.  We don't test for 
                // termination due to "no violations found" here, deferring that to the next iteration's PreProject().
                if (!this.RunProject(out foundViolation))
                {
                    break;
                }

                //
                // Calculate the new adjustment to the current positions based upon the amount of movement
                // done by split/project.  If this returns false then it means that movement was zero and
                // we're done if there was no split or constraint violation.
                //
                if (!qpsc.PostProject() && !foundSplit && !foundViolation)
                {
                    break;
                }
            } // end forever

            this.solverSolution.GoalFunctionValue = qpsc.QpscComplete();
        }

        private bool QpscMakeFeasible()
        {
            // Start off with one Project pass so the initial Qpsc state is feasible (not in violation
            // of constraints).  If this takes more than the max allowable time, we're done.
            bool foundViolation;
            return this.RunProject(out foundViolation);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), Conditional("TEST_MSAGL")]
        private void VerifyConstraintsAreFeasible()
        {
#if EX_VERIFY
            if (null != allConstraints.Vector) {
                int numUnsatConstraints = 0;
                Constraint firstUnsatConstraint = null;
                foreach (var constraint in allConstraints.Vector) {
                    if (!constraint.IsUnsatisfiable && (constraint.Violation > this.solverParams.GapTolerance)) {
                        ++numUnsatConstraints;
                        firstUnsatConstraint = firstUnsatConstraint ?? constraint;
                    }
                }
                Debug.Assert(0 == numUnsatConstraints, string.Format("{0} unsatisfied constraints exist", numUnsatConstraints));
            }
#endif // EX_VERIFY
        }

        private void ReinitializeBlocks()
        {
            // For Qpsc we want to discard the previous block structure, because it did not consider
            // neighbors, and the gradient may want to pull things in an entirely different way.
            // We must also do this for a re-Solve that updated the gap of an equality constraint.
            var oldBlocks = this.allBlocks.Vector.ToArray();
            this.allBlocks.Vector.Clear();
#if VERIFY || VERBOSE
            this.nextNewBlockOrdinal = 0;
#endif // VERIFY || VERBOSE

            foreach (Block oldBlock in oldBlocks)
            {
                foreach (Variable variable in oldBlock.Variables)
                {
                    variable.Reinitialize();
                    var newBlock = new Block(variable, this.allConstraints
#if VERIFY || VERBOSE
                                            , ref this.nextNewBlockOrdinal
#endif // VERIFY || VERBOSE
                        );
                    this.allBlocks.Add(newBlock);
                }
            }

            this.allConstraints.Reinitialize();
            this.violationCache.Clear();
        }

        private void MergeEqualityConstraints()
        {
            // PerfNote: We only call this routine once so don't worry about List-Enumerator overhead.
            foreach (var constraint in this.equalityConstraints)
            {
                if (constraint.Left.Block == constraint.Right.Block)
                {
                    // They are already in the same block and we are here on the first pass that merges blocks 
                    // containing only equality constraints.  Thus we know that there is already a chain of equality
                    // constraints joining constraint.Left and constraint.Right, and that chain will always be
                    // moved as a unit because we never split or expand equality constraints, so this constraint
                    // will remain retain its current satisfied state and does not need to be activated (which
                    // would potentially lead to cycles; this is consistent with the non-equality constraint
                    // approach of not activating constraints that are not violated).
                    if (Math.Abs(constraint.Violation) > this.solverParams.GapTolerance)
                    {
                        // This is an equivalence conflict, such as a + 3 == b; b + 3 == c; a + 9 == c.
                        constraint.IsUnsatisfiable = true;
                        ++this.allConstraints.NumberOfUnsatisfiableConstraints;
                    }
                    continue;
                }
                MergeBlocks(constraint);
            }
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("  -- End ProcessEqualityConstraints -- ");
#endif // VERBOSE
        }

        private bool Project()
        {
            if (0 == this.numberOfConstraints)
            {
                // We are here for the neighbours-only case.
                return false;
            }

            // Get the maximum violation (the Constraint with the biggest difference between the
            // required gap between its two variables vs. their actual relative positions).
            // If there is no violation, we're done (although SplitBlocks may change things so
            // we have to go again).
            this.violationCache.Clear();
            this.lastModifiedBlock = null;
            bool useViolationCache = this.allBlocks.Count > this.violationCacheMinBlockCutoff;

            // The first iteration gets the first violated constraint.
            int cIterations = 1;

            double maxViolation;
            Constraint maxViolatedConstraint = GetMaxViolatedConstraint(out maxViolation, useViolationCache);
            if (null == maxViolatedConstraint)
            {
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("Project() found no violations");
#endif // VERBOSE
                return false;
            }

            // We have at least one violation, so process them until there are no more.
            while (null != maxViolatedConstraint)
            {
                Debug.Assert(!maxViolatedConstraint.IsUnsatisfiable, "maxViolatedConstraint should not be unsatisfiable");
                Debug.Assert(!maxViolatedConstraint.IsEquality, "maxViolatedConstraint should not be equality");
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("MaxVio: {0}", maxViolatedConstraint);

                // Write a line in satisfy_inc format to compare:
                // most violated is: (45=9.73427)+3<=(51=0.456531)(-12.2777) [chop off lm as it's uninitialized in satisfy_inc output]
                System.Diagnostics.Debug.WriteLine("  most violated is: ({0}={1:F6})+{2:F0}<=({3}={4:F6})({5:F6})",
                                maxViolatedConstraint.Left.Name, maxViolatedConstraint.Left.ActualPos, maxViolatedConstraint.Gap,
                                maxViolatedConstraint.Right.Name, maxViolatedConstraint.Right.ActualPos, maxViolatedConstraint.Violation);
#endif // VERBOSE

                // Perf note: Variables (and Blocks) use the default Object.Equals implementation, which is
                // simply ReferenceEquals for reference types.
                if (maxViolatedConstraint.Left.Block == maxViolatedConstraint.Right.Block)
                {
                    maxViolatedConstraint.Left.Block.Expand(maxViolatedConstraint);
                    if (maxViolatedConstraint.IsUnsatisfiable)
                    {
                        this.violationCache.Clear();   // We're confusing the lineage of lastModifiedBlock
                    }
                    this.lastModifiedBlock = maxViolatedConstraint.Left.Block;
                }
                else
                {
                    // The variables are in different blocks so merge the blocks.
                    this.lastModifiedBlock = MergeBlocks(maxViolatedConstraint);
                }
#if VERBOSE
                DumpCost();
#endif // VERBOSE

                // Note that aborting here does not guarantee a feasible state.
                if (this.solverParams.InnerProjectIterationsLimit > 0)
                {
                    if (cIterations >= this.solverParams.InnerProjectIterationsLimit)
                    {
#if VERBOSE
                        System.Diagnostics.Debug.WriteLine("PostProject aborting due to max inner iterations: max {0}",
                                        solverParams.InnerProjectIterationsLimit);
#endif // VERBOSE
                        this.solverSolution.InnerProjectIterationsLimitExceeded = true;
                        break;
                    }
                }

                // Now we've potentially changed one or many variables' positions so recalculate the max violation.
                useViolationCache = this.allBlocks.Count > this.violationCacheMinBlockCutoff;
                if (!useViolationCache)
                {
                    this.violationCache.Clear();
                }
                ++cIterations;
                maxViolatedConstraint = GetMaxViolatedConstraint(out maxViolation, useViolationCache);
            } // endwhile violations exist

            this.solverSolution.InnerProjectIterationsTotal += cIterations;
            if (this.solverSolution.MaxInnerProjectIterations < cIterations)
            {
                this.solverSolution.MaxInnerProjectIterations = cIterations;
            }
            if (this.solverSolution.MinInnerProjectIterations > cIterations)
            {
                this.solverSolution.MinInnerProjectIterations = cIterations;
            }

            // If we got here, we had at least one violation.
            this.allConstraints.Debug_AssertConsistency();
            return true;
        } // end Project()

#if VERBOSE
        private void DumpState(string strPrefix)
        {
            if (null != (object)strPrefix)
            {
                System.Diagnostics.Debug.WriteLine("*** {0} ***", strPrefix);
            }
            for (int ii = 0; ii < allBlocks.Count; ++ii)
            {
                Block block = allBlocks[ii];
                System.Diagnostics.Debug.WriteLine(block);
                block.DumpState(null);
            }
        }
        private void DumpCost()
        {
            double cost = 0.0;
            for (int ii = 0; ii < allBlocks.Count; ++ii) {
                Block block = allBlocks[ii];
                for (int jj = 0; jj < block.Variables.Count; ++jj) {
                    var variable = block.Variables[jj];
                    cost += Math.Pow((variable.ActualPos * variable.Scale) - variable.DesiredPos, 2);
                }
            }
            System.Diagnostics.Debug.WriteLine("NumBlocks = {0}, Cost = {1:F5}", allBlocks.Count, cost);
        }
#endif // VERBOSE

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ReferencePos")]
        private Block MergeBlocks(Constraint violatedConstraint)
        {
            // Start off evaluating left-to-right.
            var blockTo = violatedConstraint.Left.Block;
            var blockFrom = violatedConstraint.Right.Block;
            Debug.Assert(blockTo != blockFrom, "Merging of constraints in the same block is not allowed");

            // The violation amount is the needed distance to move to tightly satisfy the constraint.
            // Calculate this based on offsets even though the vars are in different blocks; we'll normalize
            // that when we recalculate the block reference position and the offsets in the Right block.
            double distance = violatedConstraint.Left.OffsetInBlock + violatedConstraint.Gap - violatedConstraint.Right.OffsetInBlock;
            if (blockFrom.Variables.Count > blockTo.Variables.Count)
            {
                // Reverse this so we minimize variable movement by moving stuff from the block with the least
                // number of vars into the block with the greater number.
                blockTo = violatedConstraint.Right.Block;
                blockFrom = violatedConstraint.Left.Block;
                distance = -distance;
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("MergeBlocks merging left ({0}) into right ({1}), distance = {2:F5}",
                                blockFrom, blockTo, distance);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MergeBlocks merging right ({0}) into left ({1}), distance = {2:F5}",
                                blockFrom, blockTo, distance);
#endif // VERBOSE
            }
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("blockFrom: {0}", blockFrom);
            blockFrom.DumpState(null /* no prefix */);
            System.Diagnostics.Debug.WriteLine("blockTo: {0}", blockTo);
            blockTo.DumpState(null /* no prefix */);
#endif // VERBOSE

            // Move all vars from blockFrom to blockTo, and adjust their offsets by dist as
            // mentioned above.  This has the side-effect of moving the associated active constraints 
            // as well (because they are carried in the variables' LeftConstraints); violatedConstraint
            // is therefore also moved if it was in blockFrom.
            var numVars = blockFrom.Variables.Count;                              // iteration is faster than foreach for List<>s
            for (var ii = 0; ii < numVars; ++ii)
            {
                var variable = blockFrom.Variables[ii];
                variable.OffsetInBlock += distance;
                blockTo.AddVariable(variable);
            }
            blockTo.UpdateReferencePosFromSums();
            blockTo.DebugVerifyReferencePos();

            // Do any final bookkeeping necessary.
            blockTo.Debug_PostMerge(blockFrom);

#if VERBOSE
            System.Diagnostics.Debug.WriteLine("MergeBlocks result: {0}", blockTo);
            blockTo.DumpState(null /* no prefix */);
#endif // VERBOSE

            // Make the (no-longer-) violated constraint active.
            this.allConstraints.ActivateConstraint(violatedConstraint);

            // We have no further use for blockFrom as nobody references it.
            this.allBlocks.Remove(blockFrom);
            return blockTo;
        } // end MergeBlocks()

        private bool SplitBlocks()
        {
            // First enumerate all blocks and accumulate any new ones that we form by splitting off
            // from an existing block.  Then add those to our block list in a second pass (to avoid
            // a "collection modified during enumeration" exception).
            var newBlocks = new List<Block>();
            int numBlocks = this.allBlocks.Count;        // Cache for perf
            for (int ii = 0; ii < numBlocks; ++ii)
            {
                var block = this.allBlocks[ii];
                Debug.Assert(0 != block.Variables.Count, "block must have nonzero variable count");

                var newSplitBlock = block.Split(this.IsQpsc
#if VERIFY || VERBOSE
                                                , ref this.nextNewBlockOrdinal
#endif // VERIFY || VERBOSE
                    );
                if (null != newSplitBlock)
                {
                    newBlocks.Add(newSplitBlock);
                }
            }

            int numNewBlocks = newBlocks.Count;         // cache for perf
            for (int ii = 0; ii < numNewBlocks; ++ii)
            {
                Block block = newBlocks[ii];
                this.allBlocks.Add(block);
            }
#if VERBOSE
            if (0 != newBlocks.Count)
            {
                DumpCost();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("SplitBlocks found nothing to split");
            }
#endif // VERBOSE

            // The paper uses "did not split" for the return but "did split" seems more intuitive
            return (0 != newBlocks.Count);
        } // end SplitBlocks

        private Constraint GetMaxViolatedConstraint(out double maxViolation, bool useViolationCache)
        {
#if CACHE_STATS
            ++cacheStats.NumberOfCalls;
#endif // CACHE_STATS

            // Get the most-violated constraint in the Solver.  Active constraints are calculated
            // to keep their constraint minimally satisfied, so any nonzero active-constraint
            // violation is due to rounding error; therefore just look for inactive constraints.
            // Pass maxViolation to subroutines because it is initialized to a limiting value.
            maxViolation = this.solverParams.GapTolerance;
            Constraint maxViolatedConstraint = this.SearchViolationCache(maxViolation);
            if (null != maxViolatedConstraint)
            {
                return maxViolatedConstraint;
            }

            // Nothing in ViolationCache or we've got too many Constraints in the block, so search 
            // the list of all constraints.
            return SearchAllConstraints(maxViolation, useViolationCache);
        } // end GetMaxViolatedConstraint()

        private Constraint SearchViolationCache(double maxViolation)
        {
            // If we have any previously cached max violated constraints, then we'll first remove any
            // that are incoming to or outgoing from the lastModifiedBlock on the current Project()
            // iteration; these constraints are the only ones that may have changed violation values
            // (due to block expansion or merging).  If any of the cached maxvio constraints remain after
            // that, then we can use the largest of these if it's larger than any constraints in lastModifiedBlock.
            // Even if no cached violations remain after filtering, we still know that the largest violations were
            // most likely associated with lastModifiedBlock.  So we take a pass through lastModifiedBlock and put
            // its top constraints into the cache and then take the largest constraint from the violation cache, 
            // which may or may not be associated with lastModifiedBlock.  (This would happen after filling the
            // cache from multiple blocks in the first pass, or after Block.Split moved some variables (with
            // cached inactive constraints) to the new block).
            //
            // This iteration is slower (relative to the number of constraints in the block) than
            // SearchAllConstraints, due to two loops, so only do it if the block has a sufficiently small
            // number of constraints.  Use the Variables as a proxy for the constraint count of the block.
            // @@PERF: the block could keep a constraint count to make ViolationCache cutoff more accurate.
            Constraint maxViolatedConstraint = null;
            if ((null != this.lastModifiedBlock)
                    && (this.lastModifiedBlock.Variables.Count < (this.numberOfVariables >> 1))
                    && this.violationCache.FilterBlock(this.lastModifiedBlock)      // Also removes unsatisfiables
                )
            {
#if CACHE_STATS
                ++cacheStats.NumberOfHits;
#endif // CACHE_STATS

                // First evaluate all (inactive) outgoing constraints for all variables in the block; this gets
                // both all intra-block constraints and all inter-block constraints where the lastModifiedBlock
                // is the source.  Then evaluate incoming constraints where the source is outside the block.
                int numVarsInBlock = this.lastModifiedBlock.Variables.Count;             // cache for perf
                for (int variableIndex = 0; variableIndex < numVarsInBlock; ++variableIndex)
                {
                    var variable = this.lastModifiedBlock.Variables[variableIndex];
                    foreach (var constraint in variable.LeftConstraints)           // Array.Foreach is optimized.
                    {
                        if (!constraint.IsActive && !constraint.IsUnsatisfiable)
                        {
#if Inline_Violation
                            double violation = constraint.Violation;       // Inline_Violation
#else  // Inline_Violation
                            double violation = (constraint.Left.ActualPos * constraint.Left.Scale)
                                                + constraint.Gap
                                                - (constraint.Right.ActualPos * constraint.Right.Scale);
                            Debug.Assert(ApproximateComparer.Close(constraint.Violation, violation), "LeftConstraints: constraint.Violation must == violation");
#endif // Inline_Violation
                            if (ApproximateComparer.Greater(violation, maxViolation))
                            {
                                // Cache the previous high violation.  Pass the violation as a tiny perf optimization
                                // to save re-doing the double operations in this inner loop.
                                if ((null != maxViolatedConstraint) && (maxViolation > this.violationCache.LowViolation))
                                {
                                    this.violationCache.Insert(maxViolatedConstraint, maxViolation);
                                }
                                maxViolation = constraint.Violation;
                                maxViolatedConstraint = constraint;
                            }
                        }
                    } // endfor each LeftConstraint

                    foreach (var constraint in variable.RightConstraints)           // Array.Foreach is optimized.
                    {
                        if (!constraint.IsActive && !constraint.IsUnsatisfiable && (constraint.Left.Block != this.lastModifiedBlock))
                        {
#if Inline_Violation
                            double violation = constraint.Violation;      // Inline_Violation
#else  // Inline_Violation
                            double violation = (constraint.Left.ActualPos * constraint.Left.Scale)
                                                + constraint.Gap
                                                - (constraint.Right.ActualPos * constraint.Right.Scale);
                            //Debug.Assert(constraint.Violation == violation, "LeftConstraints: constraint.Violation must == violation");
                            Debug.Assert(ApproximateComparer.Close(constraint.Violation, violation), "LeftConstraints: constraint.Violation must == violation");

#endif // Inline_Violation
                            //if (violation > maxViolation)
                            if (ApproximateComparer.Greater(violation, maxViolation))
                            {
                                if ((null != maxViolatedConstraint) && (maxViolation > this.violationCache.LowViolation))
                                {
                                    this.violationCache.Insert(maxViolatedConstraint, maxViolation);
                                }
                                maxViolation = violation;
                                maxViolatedConstraint = constraint;
                            }
                        }
                    } // endfor each RightConstraint
                } // endfor each var in lastModifiedBlock.Variables

                // Now see if any of the cached maxvios are greater than we have now.  Don't remove
                // it here; we'll wait until Expand/Merge set lastModifiedBlock and then the removal
                // occurs above in ViolationCache.FilterBlock in this block when we come back in.
                Constraint cachedConstraint = this.violationCache.FindIfGreater(maxViolation);
                if (null != cachedConstraint)
                {
                    // The cache had something more violated than maxViolatedConstraint, but maxViolatedConstraint
                    // may be larger than at least one cache element.
                    if ((null != maxViolatedConstraint) && (maxViolation > this.violationCache.LowViolation))
                    {
                        this.violationCache.Insert(maxViolatedConstraint, maxViolation);
                    }
                    maxViolatedConstraint = cachedConstraint;
                }
            } // endif FilterBlock

            return maxViolatedConstraint;     // Remains null if we don't find one
        }

        private Constraint SearchAllConstraints(double maxViolation, bool useViolationCache)
        {
            // Iterate all constraints, finding the most-violated and populating the violation cache
            // with the next-highest violations.
            Constraint maxViolatedConstraint = null;
            this.violationCache.Clear();

            foreach (Constraint constraint in this.allConstraints.Vector)             // Array.Foreach is optimized.
            {
                // The constraint vector is now organized with all inactive constraints first.
                if (constraint.IsActive)
                {
                    break;
                }
                if (constraint.IsUnsatisfiable)
                {
                    continue;
                }

                // Note:  The docs have >= 0 for violation condition but it should be just > 0.
#if Inline_Violation
                double violation = constraint.Violation;      // Inline_Violation
#else  // Inline_Violation
                double violation = (constraint.Left.ActualPos * constraint.Left.Scale)
                                    + constraint.Gap
                                    - (constraint.Right.ActualPos * constraint.Right.Scale);
                Debug.Assert(ApproximateComparer.Close(constraint.Violation, violation), "constraint.Violation must == violation");
#endif // Inline_Violation
                Constraint cacheInsertConstraint = null;
                double cacheInsertViolation = 0.0;
                if (ApproximateComparer.Greater(violation, maxViolation))
                {
                    if (maxViolation > this.violationCache.LowViolation)
                    {
                        cacheInsertConstraint = maxViolatedConstraint;
                        cacheInsertViolation = maxViolation;
                    }
                    maxViolation = violation;
                    maxViolatedConstraint = constraint;
                }

                if (useViolationCache)
                {
                    // If constraint was a violation but not > maxViolation, then we'll look to insert it into the cache.
                    // (We already know that if the previous maxViolatedConstraint is to be inserted, then its violation is
                    // greater than any in the cache).  On the first iteration of "for each constraint", maxViolatedConstraint
                    // is null, hence the constraint != maxViolatedConstraint test.
                    if ((null == cacheInsertConstraint)
                            && (constraint != maxViolatedConstraint)
                            && (!this.violationCache.IsFull || (violation > this.violationCache.LowViolation)))
                    {
                        // Either the cache isn't full or the new constraint is more violated than the lowest cached constraint.
                        cacheInsertConstraint = constraint;
                        cacheInsertViolation = violation;
                    }

                    if ((null != cacheInsertConstraint) && (cacheInsertViolation > this.violationCache.LowViolation))
                    {
                        this.violationCache.Insert(cacheInsertConstraint, cacheInsertViolation);
                    }
                } // endif useViolationCache
            } // endfor each constraint

            return maxViolatedConstraint;     // Remains null if we don't find one
        }
    }
}