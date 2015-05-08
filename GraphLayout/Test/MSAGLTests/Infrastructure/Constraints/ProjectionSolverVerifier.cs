// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectionSolverVerifier.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.Msagl.Core.ProjectionSolver;

using ProjSolv = Microsoft.Msagl.Core.ProjectionSolver;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    using System.Diagnostics;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Basic verification logic, as well as the single-inheritance-only propagation of MsaglTestBase.
    /// </summary>
    [TestClass]
    public class ProjectionSolverVerifier : ResultVerifierBase
    {
        // The ProjectionSolver created in CheckResult is used by CreateTestFile.  We have only the single dimension in the 
        // ProjectionSolver tests.
        protected Solver SolverX { get; set; }
        protected Solution SolutionX { get; set; }

        // We can have two dimensions, X and Y, for OverlapRemoval, but only one for ProjectionSolver,
        // so use a known "ignore" value for the Y direction for ProjectionSolver. Similarly,
        // for ProjectionSolver we use 0 as the solver doesn't use size; OverlapRemoval does.
        protected const double ValueNotUsed = 0.0;

        // For handling expected failures due to cyclic or contradictory constraints.
        internal int ExpectedUnsatisfiedConstraintCount { get; set; }

        // Cycles are allowed only when testing our error handling for them; this is set only by TestConstraints.
        internal static int NumberOfCyclesToCreate { get; set; }

        // For re-gapping constraints.
        internal static int ReGapInterval { get; set; }
        internal static bool RestoreGapsAndReSolve { get; set; }

        internal ProjectionSolverVerifier()
        {
            this.InitializeMembers();
        }

        // Override of [TestInitialize] method.
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
            this.InitializeMembers();
        }

        private void InitializeMembers()
        {
            SolverX = null;
            SolutionX = null;
            ExpectedUnsatisfiedConstraintCount = 0;
            ReGapInterval = 0;
            RestoreGapsAndReSolve = false;
        }

        ////
        //// These overloads work with the local Test*() routines.
        ////
        internal bool CheckResult(VariableDef[] variableDefs, ConstraintDef[] constraintDefs,
                            double[] expectedPositions, bool checkResults)
        {
            return CheckResult(variableDefs, constraintDefs, null, expectedPositions, checkResults);
        }
        internal bool CheckResult(VariableDef[] variableDefs, ConstraintDef[] constraintDefs,
                            NeighborDef[] neighborDefs, double[] expectedPositions, bool checkResults)
        {
            SetVariableExpectedPositions(variableDefs, expectedPositions);

            int violationCount;
            return CheckResult(variableDefs, constraintDefs, neighborDefs, checkResults, out violationCount);
        }

        private static void SetVariableExpectedPositions(VariableDef[] variableDefs, double[] expectedPositions)
        {
            for (uint id = 0; id < variableDefs.Length; ++id)
            {
                variableDefs[id].SetExpected(id, expectedPositions[id]);
            }
        }

        ////
        //// ... and this overload works with DataFiles as well as performing the actual work.
        ////
        internal bool CheckResult(IEnumerable<VariableDef> iterVariableDefs, IEnumerable<ConstraintDef> iterConstraintDefs,
                            IEnumerable<NeighborDef> iterNeighborDefs, bool checkResults, out int violationsCount)
        {
            var sw = new Stopwatch();
            violationsCount = 0;

            bool succeeded = true;
            for (uint rep = 0; rep < TestGlobals.TestReps; ++rep)
            {
                sw.Start();
                succeeded = CreateSolverAndGetSolution(iterVariableDefs, iterConstraintDefs, iterNeighborDefs, succeeded);
                sw.Stop();
                succeeded = this.VerifyConstraints(rep, iterConstraintDefs, succeeded, ref violationsCount, checkResults);
            }

            // Verify the Variable positions are as expected.
            if (!PostCheckResults(iterVariableDefs, this.SolutionX.GoalFunctionValue, double.NaN, sw, checkResults))
            {
                succeeded = false;
            }
            DisplayVerboseResults();
            return succeeded;
        }

        private void DisplayVerboseResults()
        {
            if (TestGlobals.VerboseLevel >= 1)
            {
                var strIterationsX = GetIterationsString(this.SolutionX);
                this.WriteLine("  Project iterations: {0}", strIterationsX);
                this.WriteLine("  Goal Function value: {0}", this.SolutionX.GoalFunctionValue);
                this.WriteLine("Max Constraint tree depth: {0}", this.SolutionX.MaxConstraintTreeDepth);

                int countOfVarsWithActiveCsts = 0;
                int activeCstCount = 0;
                foreach (var var in this.SolverX.Variables)
                {
                    bool hasInactive = false;
                    if (null != var.LeftConstraints)
                    {
                        foreach (Constraint cst in var.LeftConstraints)
                        {
                            if (cst.IsActive)
                            {
                                ++activeCstCount;
                            }
                            else if (!hasInactive)
                            {
                                ++countOfVarsWithActiveCsts;
                                hasInactive = true;
                            }
                        } // endfor each constraint in var
                    } // endif var has constraints
                } // endforeach var

                this.WriteLine("Vars with inactive csts: {0} of {1}; Active Csts {2}/{3} ({4:F2}%)",
                    countOfVarsWithActiveCsts, this.SolverX.VariableCount, activeCstCount, this.SolverX.ConstraintCount,
                    ((double)activeCstCount / this.SolverX.ConstraintCount) * 100);
            }
        }

        private bool CreateSolverAndGetSolution(IEnumerable<VariableDef> iterVariableDefs, IEnumerable<ConstraintDef> iterConstraintDefs, IEnumerable<NeighborDef> iterNeighborDefs, bool succeeded)
        {
            this.SolverX = new Solver();
            foreach (VariableDef varDef in iterVariableDefs)
            {
                varDef.VariableX = new ProjTestVariable(
                    this.SolverX.AddVariable(varDef.IdString /* userData */, varDef.DesiredPosX, varDef.WeightX, varDef.ScaleX));
            }

            int nextReGap = 0;
            int sign = 1;
            foreach (ConstraintDef cstDef in iterConstraintDefs)
            {
                double gap = cstDef.Gap;
                ++nextReGap;
                if ((0 != ReGapInterval) && (0 == (nextReGap % ReGapInterval)))
                {
                    // Just something semi-random to mix it up. The idea is that we do a first pass with 
                    // a munged gap, verify constraint consistency, then restore the original gap and resolve,
                    // and verify that second solution against the expected results.
                    gap += (gap / 2) * sign;
                    sign *= -1;
                }
                cstDef.ReGap = gap;
                cstDef.Constraint = this.SolverX.AddConstraint(
                    ((ProjTestVariable)cstDef.LeftVariableDef.VariableX).Variable,
                    ((ProjTestVariable)cstDef.RightVariableDef.VariableX).Variable,
                    gap,
                    cstDef.IsEquality);
            }
            if (null != iterNeighborDefs)
            {
                foreach (NeighborDef nbourDef in iterNeighborDefs)
                {
                    this.SolverX.AddNeighborPair(
                        ((ProjTestVariable)nbourDef.LeftVariableDef.VariableX).Variable,
                        ((ProjTestVariable)nbourDef.RightVariableDef.VariableX).Variable,
                        nbourDef.Weight);
                }
            }

            this.SolutionX = this.SolverX.Solve(this.SolverParameters);
            if (!this.VerifySolutionMembers(this.SolutionX, iterNeighborDefs))
            {
                succeeded = false;
            }

            if (RestoreGapsAndReSolve)
            {
                if (0 != this.SolutionX.NumberOfUnsatisfiableConstraints)
                {
                    this.WriteLine("ReSolve: unsatisfiable constraints exist after first Solve()");
                }
                int oldViolationCount = 0;
                int activeReGapCount = 0;
                int totalReGapCount = 0;

                foreach (ConstraintDef cstDef in iterConstraintDefs)
                {
                    if (cstDef.Constraint.Violation > this.SolverParameters.GapTolerance)
                    {
                        // This should not happen unless we have unsatisfiable constraints.  In that case,
                        // we have not achieved a feasible state, so we may still see diffs in the output
                        // after the second Solve() (one infeasible state leading to another may not reach
                        // the same unfeasible state as when the test is run without re-Solve().
                        ++oldViolationCount;
                    }
                    if (cstDef.Gap != cstDef.ReGap)
                    {
                        ++totalReGapCount;
                        if (cstDef.Constraint.IsActive)
                        {
                            ++activeReGapCount;
                        }

                        // This defers the update until Solve().
                        this.SolverX.SetConstraintUpdate(cstDef.Constraint, cstDef.Gap);
                    }
                }
                this.SolutionX = this.SolverX.Solve(this.SolverParameters);
                int newViolationCount = iterConstraintDefs.Count(cstDef => !cstDef.VerifyGap(this.SolverParameters.GapTolerance));
                this.WriteLine("ReSolve: {0} of {1} regaps were active with {2} overall violations ({3} overall violations with restored gaps)",
                        activeReGapCount, totalReGapCount, oldViolationCount, newViolationCount);
                if (0 != this.SolutionX.NumberOfUnsatisfiableConstraints)
                {
                    this.WriteLine("ReGap: unsatisfiable constraints exist after second solve");
                }

            }
            return succeeded;
        }

        private bool VerifyConstraints(uint rep, IEnumerable<ConstraintDef> iterConstraintDefs, bool succeeded, ref int violationsCount, bool checkResults)
        {
            if (0 != this.ExpectedUnsatisfiedConstraintCount)
            {
                if (TestGlobals.VerboseLevel >= 2)
                {
                    this.WriteLine("Constraints that were created to force cycles:");
                    foreach (ConstraintDef cstDef in iterConstraintDefs)
                    {
                        // We create constraints from low to high variable numbers, except for those we create
                        // to force cycles, which are reversed.
                        if (int.Parse(cstDef.LeftVariableDef.IdString) > int.Parse(cstDef.RightVariableDef.IdString))
                        {
                            this.WriteLine("       *{0}", cstDef.Constraint);
                        }
                    }
                }
            }

            if (0 != this.SolutionX.NumberOfUnsatisfiableConstraints)
            {
                if (!this.VerifyUnsatisfiableConstraints(rep, ref violationsCount, checkResults))
                {
                    succeeded = false;
                }
            }
            else
            {
                // There are no unsatisfiable constraints.
                if ((0 != NumberOfCyclesToCreate) || (0 != this.ExpectedUnsatisfiedConstraintCount))
                {
                    succeeded = false;
                    this.WriteLine(" *** Error: {0}{1} unsatisfiable constraint lists expected, but none were found ***",
                        (0 != this.ExpectedUnsatisfiedConstraintCount) ? string.Empty : "Creation of ",
                        (0 != this.ExpectedUnsatisfiedConstraintCount) ? this.ExpectedUnsatisfiedConstraintCount : NumberOfCyclesToCreate);
                }
            }

            // Since we mark each constraint with .IsUnsatisfiable as appropriate, we can verify all constraints here.
            bool violationSeen = false;
            foreach (Constraint cst in this.SolverX.Constraints)
            {
                if (!this.VerifyConstraint(this.SolverParameters, cst, true /*fIsHorizontal*/, ref violationSeen))
                {
                    succeeded = false;
                }
            }

            // If there are a lot of variable position diffs, the output of this can scroll out of view, so repeat it.
            if (SolutionX.ExecutionLimitExceeded)
            {
                this.WriteLine(GetCutoffString(SolutionX));
            }
            return succeeded;
        }

        private bool VerifyUnsatisfiableConstraints(uint rep, ref int violationsCount, bool checkResults)
        {
            bool succeeded = true;
            if ((0 == NumberOfCyclesToCreate) &&
                    (this.SolutionX.NumberOfUnsatisfiableConstraints != this.ExpectedUnsatisfiedConstraintCount))
            {
                // From a canned test or file with a known expected number of unsat constraints/lists.
                succeeded = false;
                if (0 == this.ExpectedUnsatisfiedConstraintCount)
                {
                    this.WriteLine(" *** Error: {0} unsatisfiable constraint lists found ***",
                        this.SolutionX.NumberOfUnsatisfiableConstraints);
                }
                else
                {
                    this.WriteLine(" *** Error: {0} unsatisfiable constraint lists found; expected {1} ***",
                        this.SolutionX.NumberOfUnsatisfiableConstraints, this.ExpectedUnsatisfiedConstraintCount);
                }
            }
            else
            {
                // Informative only, to show how many unsat constraints/lists were created.
                if (0 != NumberOfCyclesToCreate)
                {
                    this.WriteLine("  Created file with {0} unsatisfiable constraint lists",
                        this.SolutionX.NumberOfUnsatisfiableConstraints);
                    this.ExpectedUnsatisfiedConstraintCount = this.SolutionX.NumberOfUnsatisfiableConstraints;
                }
                else if ((TestGlobals.VerboseLevel >= 1) && (0 == rep))
                {
                    this.WriteLine("  Expected number of unsatisfiable constraint lists ({0}) detected",
                        this.SolutionX.NumberOfUnsatisfiableConstraints);
                }
            }

            // There were unsatisfiable constraints so verify they were as expected.
            if (0 == rep)
            {
                if (TestGlobals.VerboseLevel >= 2)
                {
                    this.WriteLine("  Unsatisfiable constraint lists:");
                    foreach (Constraint cst in this.SolverX.Constraints.Where(cst => cst.IsUnsatisfiable))
                    {
                        // We create constraints from low to high variable numbers, except for those we create
                        // to force cycles, which are reversed.
                        this.WriteLine("       {0}{1}", (int.Parse(cst.Left.Name) > int.Parse(cst.Right.Name)) ? "*" : " ", cst);
                    }
                } // endif VerboseLevel

                int constraintCount = 0;
                foreach (Constraint cst in this.SolverX.Constraints)
                {
                    ++constraintCount;
                    bool hasViolation = cst.Violation > this.SolverParameters.GapTolerance;
                    if (cst.IsEquality)
                    {
                        hasViolation = Math.Abs(cst.Violation) > this.SolverParameters.GapTolerance;
                    }
                    if (hasViolation)
                    {
                        ++violationsCount;
                        if (checkResults && (TestGlobals.VerboseLevel >= 2))
                        {
                            if (1 == violationsCount)
                            {
                                this.WriteLine("     Violated constraints:");
                            }
                            this.WriteLine("     {0}{1}", cst.IsUnsatisfiable ? " ok" : "bad", cst);
                        } // endif VerboseLevel
                    } // endif fViolation
                }
                this.WriteLine("  ({0} of {1} constraints are in violation)", violationsCount, constraintCount);
                if (violationsCount != this.ExpectedUnsatisfiedConstraintCount)
                {
                    this.WriteLine(" *** Error: {0} unsatisfiable constraints found; expected {1} ***", violationsCount, this.ExpectedUnsatisfiedConstraintCount);
                    succeeded = false;
                }
            }
            return succeeded;
        }

        internal bool SolveRegap(string tag, VariableDef[] variableDefs, ConstraintDef[] constraintDefs, double[] expectedPositionsX)
        {
            if (TestGlobals.VerboseLevel >= 1)
            {
                this.WriteLine("... ReGapping ({0})...", tag);
            }
            SetVariableExpectedPositions(variableDefs, expectedPositionsX);
            int numViolations = 0;
            var sw = new Stopwatch();
            sw.Start();
            this.SolutionX = this.SolverX.Solve(this.SolverParameters);
            sw.Stop();
            bool succeeded = true;
            if (!VerifySolutionMembers(this.SolutionX, /*iterNeighbourDefs:*/ null))
            {
                succeeded = false;
            }
            if (!VerifyConstraints(/*cRep:*/ 0, constraintDefs, /*succeeded:*/ true, ref numViolations, /*checkResults:*/ true))
            {
                succeeded = false;
            }
            if (!PostCheckResults(variableDefs, this.SolutionX.GoalFunctionValue, double.NaN, sw, /*checkResults:*/ true))
            {
                succeeded = false;
            }
            DisplayVerboseResults();
            return succeeded;
        }

        // Worker to ensure we set values from loaded testfiles.
        internal TestFileReader LoadTestDataFile(string strFileName)
        {
            var tdf = new TestFileReader(/*isTwoDimensional:*/ false);
            tdf.Load(strFileName);
            GoalFunctionValueX = tdf.GoalX;
            GoalFunctionValueY = tdf.GoalY;
            ExpectedUnsatisfiedConstraintCount = tdf.UnsatisfiableConstraintCount;
            return tdf;
        }
    }
}
