// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResultVerifierBase.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.ProjectionSolver;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Layout.Layered;

using ProjSolv = Microsoft.Msagl.Core.ProjectionSolver;

// Suppress this - it only fires for SugiyamaLayoutSettings.
// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Microsoft.Msagl.UnitTests.Constraints
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Basic verification logic, as well as the single-inheritance-only propagation of MsaglTestBase.
    /// </summary>
    [TestClass]
    public class ResultVerifierBase : MsaglTestBase
    {
        // Used by ProjectionSolver and SolverFoundation.
        internal double GoalFunctionValueX { get; set; }
        internal double GoalFunctionValueY { get; set; }

        // SolverParameters and solution.  We hold onto a savedSolverParameters to restore
        // cmdline args because some tests may modify one or more parameters.
        internal Parameters SolverParameters { get; set; }
        internal static bool ForceQpsc { get; set; }

        // Dump rectangles if desired.
        internal static bool DumpRectCoordinates { get; set; }
        internal static bool ShowRects { get; set; }
        internal static bool ShowInitialRects { get; set; }

        internal static string FailureString { get { return "CheckResult failed; see detailed output"; } }
        internal static string ReGapFailureString { get { return "SolveReGap failed; see detailed output"; } }

        internal ResultVerifierBase()
        {
            this.InitializeMembers();
        }

        internal const double DefaultPositionTolerance = 0.01;

        // Override of [TestInitialize] method.  This is the base level at which we override this;
        // common variables between OverlapRemoval and ProjectionSolver (and in TestConstraints.exe,
        // SolverFoundation) go here.  This further overridden in the OverlapRemovalVerifier and ProjectionSolverVerifier
        // to reset the variables at that level; and for TestConstraints.exe, there is one more
        // level of subclass to do this for file regeneration.
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
            this.InitializeMembers();
        }

        private void InitializeMembers()
        {
            this.GoalFunctionValueX = double.NaN;
            this.GoalFunctionValueY = double.NaN;
            SolverParameters = TestGlobals.InitialSolverParameters;
        }

        protected static bool ApproxEquals(double first, double second)
        {
            return Math.Abs(first - second) < 0.01;
        }


        protected static string GetCutoffString(Solution solution)
        {
            Validate.IsNotNull(solution, "Solution must not be null");
            string strCutoff = string.Empty;
            if (solution.ExecutionLimitExceeded)
            {
                strCutoff = " [Cutoff:";
                if (solution.TimeLimitExceeded)
                {
                    strCutoff += " TimeLimit";
                }
                if (solution.OuterProjectIterationsLimitExceeded)
                {
                    strCutoff += " OuterIterLimit";
                }
                if (solution.InnerProjectIterationsLimitExceeded)
                {
                    strCutoff += " InnerIterLimit";
                }
                strCutoff += "]";
            }
            return strCutoff;
        }
        
        protected static string GetIterationsString(Solution solution)
        {
            Validate.IsNotNull(solution, "Solution must not be null");
            string strCutoff = GetCutoffString(solution);
            return string.Format(
                "outer {0}; inner min={1} max={2} total={3} average={4:F2}; algo = {5}{6}",
                solution.OuterProjectIterations,
                solution.MinInnerProjectIterations,
                solution.MaxInnerProjectIterations,
                solution.InnerProjectIterationsTotal,
                (0.0 == solution.OuterProjectIterations) ? 0.0 : ((double)solution.InnerProjectIterationsTotal / solution.OuterProjectIterations),
                solution.AlgorithmUsed,
                strCutoff);
        }

        internal bool VerifySolutionMembers(Solution solution, IEnumerable<NeighborDef> iterNeighbourDefs)
        {
            bool usedQpsc = (solution.AlgorithmUsed == SolverAlgorithm.QpscWithScaling)
                         || (solution.AlgorithmUsed == SolverAlgorithm.QpscWithoutScaling);
            if (usedQpsc != (ForceQpsc || ((null != iterNeighbourDefs) && iterNeighbourDefs.Any())))
            {
                WriteLine("UsedQPSC is not as expected");
                return false;
            }
            return true;
        }

        private void DumpRectangles(IEnumerable<VariableDef> iterVariableDefs)
        {
            if (DumpRectCoordinates)
            {
                this.WriteLine("// Node [left, low] [right, high] points:");
                foreach (VariableDef varDef in iterVariableDefs)
                {
                    this.WriteLine("  [{0:F5}, {1:F5}] [{2:F5}, {3:F5}]", varDef.Left, varDef.Top, varDef.Right, varDef.Bottom);
                }
                this.WriteLine();
            }
        }

        internal void DumpRectangles(IEnumerable<ClusterDef> iterClusterDefs) 
        {
            if (DumpRectCoordinates)
            {
                this.WriteLine("// Cluster [left, low] [right, high] points:");
                foreach (ClusterDef clusDef in iterClusterDefs)
                {
                    this.WriteLine("  [{0:F5}, {1:F5}] [{2:F5}, {3:F5}]", clusDef.Left, clusDef.Top, clusDef.Right, clusDef.Bottom);
                }
                this.WriteLine();
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Test code.")]
        internal bool PostCheckResults(IEnumerable<VariableDef> iterVariableDefs,
                            double goalX, double goalY, Stopwatch sw, bool checkResults)
        {
            DumpRectangles(iterVariableDefs);

            uint diffsX = 0, diffsY = 0;
            double minAbsErrX = double.MaxValue, minAbsErrY = double.MaxValue;
            double maxAbsErrX = double.MinValue, maxAbsErrY = double.MinValue;
            double minPctErrX = double.MaxValue, minPctErrY = double.MaxValue;
            double maxPctErrX = double.MinValue, maxPctErrY = double.MinValue;
            double totalAbsErrX = 0.0, totalAbsErrY = 0.0;
            double totalPctErrX = 0.0, totalPctErrY = 0.0;
            if (!checkResults)
            {
                if (TestGlobals.VerboseLevel >= 1)
                {
                    WriteLine("Skipping expected results check");
                }
            }
            else
            {
                if (TestGlobals.VerboseLevel >= 1)
                {
                    WriteLine("Dumping results");
                }
                foreach (var varDef in iterVariableDefs)
                {
                    // Verify the result.
                    bool hasErrX = !varDef.VerifyX();
                    bool hasErrY = TestGlobals.IsTwoDimensional && !varDef.VerifyY();
                    bool wantVerbose = (TestGlobals.VerboseLevel >= 2) || ((hasErrX || hasErrY) && (TestGlobals.VerboseLevel >= 1));
                    if (wantVerbose)
                    {
                        if (TestGlobals.IsTwoDimensional)
                        {
                            Console.Write(
                                "[{0}] actual/expected ({1:F5}/{2:F5}, {3:F5}/{4:F5})",
                                varDef.IdString,
                                varDef.VariableX.ActualPos,
                                varDef.ExpectedResultPosX,
                                varDef.VariableY.ActualPos,
                                varDef.ExpectedResultPosY);
                        }
                        else
                        {
                            Console.Write(
                                "[{0}] actual/expected ({1:F5}/{2:F5})",
                                varDef.IdString,
                                varDef.VariableX.ActualPos,
                                varDef.ExpectedResultPosX);
                        }
                    }

                    // Keep error statistics.
                    if (hasErrX || hasErrY)
                    {
                        // X...
                        double percentErrX = 0.0;
                        if (hasErrX)
                        {
                            double absoluteErrX = Math.Abs(varDef.ExpectedResultPosX - varDef.VariableX.ActualPos);
                            percentErrX = (absoluteErrX / Math.Abs(varDef.ExpectedResultPosX)) * 100.0;
                            minAbsErrX = Math.Min(minAbsErrX, absoluteErrX);
                            maxAbsErrX = Math.Max(maxAbsErrX, absoluteErrX);
                            minPctErrX = Math.Min(minPctErrX, percentErrX);
                            maxPctErrX = Math.Max(maxPctErrX, percentErrX);
                            totalAbsErrX += absoluteErrX;
                            totalPctErrX += percentErrX;
                            ++diffsX;
                        }

                        // Y...
                        double percentErrY = 0.0;
                        if (hasErrY)
                        {
                            double absoluteErrY = Math.Abs(varDef.ExpectedResultPosY - varDef.VariableY.ActualPos);
                            percentErrY = (absoluteErrY / Math.Abs(varDef.ExpectedResultPosY)) * 100.0;
                            minAbsErrY = Math.Min(minAbsErrY, absoluteErrY);
                            maxAbsErrY = Math.Max(maxAbsErrY, absoluteErrY);
                            minPctErrY = Math.Min(minPctErrY, percentErrY);
                            maxPctErrY = Math.Max(maxPctErrY, percentErrY);
                            totalAbsErrY += absoluteErrY;
                            totalPctErrY += percentErrY;
                            ++diffsY;
                        }

                        if (TestGlobals.VerboseLevel >= 1)
                        {
                            if (TestGlobals.IsTwoDimensional)
                            {
                                Console.Write(" <== Failed: diffs ({0:F5}, {1:F5}%; {2:F5}, {3:F5}%)",
                                        varDef.ExpectedResultPosX - varDef.VariableX.ActualPos, percentErrX,
                                        varDef.ExpectedResultPosY - varDef.VariableY.ActualPos, percentErrY);
                            }
                            else
                            {
                                Console.Write(" <== Failed: diffs ({0:F5}, {1:F5}%)",
                                        varDef.ExpectedResultPosX - varDef.VariableX.ActualPos, percentErrX);
                            }
                        }
                    }
                    if (wantVerbose)
                    {
                        WriteLine();
                    }
                } // endforeach varDef
            } // endifelse checkResults

            bool hasResultDiff = (0 != diffsX) || (0 != diffsY);
            bool hasGoal = checkResults && !double.IsNaN(this.GoalFunctionValueX);
            bool hasGoalDiff = false;
            bool succeeded = true;

            // Verify the goal function value(s) is/are as expected.  GoalX will always exist if GoalY does.
            // This will also tell us whether any per-variable expected result diffs are within acceptable range.
            if (hasGoal)
            {
                hasGoalDiff = GetHasGoalDiff(goalX, goalY);
                if (hasGoalDiff)
                {
                    succeeded = false;
                }
                else if (hasResultDiff)
                {
                    // Positions changed a bit but the goal function is still OK so it's just a warning.
                    // Print this here so that we don't have anything but failure notifications below the time line.
                    this.WriteLine("Warning:  Expected positions differed but goal function was satisfied");
                }
            }

            TimeSpan ts = sw.Elapsed;
            WriteLine("  Elapsed time: {0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            if (hasResultDiff)
            {
                WriteLine("  {0} X diff(s), {1} Y diff(s)", diffsX, diffsY);
                if (diffsX > 0)
                {
                    WriteLine("    X: MinAbs {0:F5}, MaxAbs {1:F5}, AvgAbs {2:F5};  Min% {3:F5}, Max% {4:F5} Avg% {5:F5}",
                            minAbsErrX,
                            maxAbsErrX,
                            totalAbsErrX / diffsX,
                            minPctErrX,
                            maxPctErrX,
                            totalPctErrX / diffsX);
                }
                if (diffsY > 0)
                {
                    WriteLine("    Y: MinAbs {0:F5}, MaxAbs {1:F5}, AvgAbs {2:F5};  Min% {3:F5}, Max% {4:F5} Avg% {5:F5}",
                            minAbsErrY,
                            maxAbsErrY,
                            totalAbsErrY / diffsY,
                            minPctErrY,
                            maxPctErrY,
                            totalPctErrY / diffsY);
                }
            }

            if (hasResultDiff && (!hasGoal || hasGoalDiff))
            {
                // No goal function to override, so any positional difference is an error.  This prints
                // even in non-verbose mode, indicating that the previously-printed TestXX name failed.
                WriteLine("Error:  Expected positions failed!");
                succeeded = false;
            }
            return succeeded;
        }

        private bool GetHasGoalDiff(double goalX, double goalY)
        {
            bool hasGoalDiff = false;
            double dblDiff = goalX - this.GoalFunctionValueX;
            double dblPctDiff = Math.Abs(dblDiff / this.GoalFunctionValueX);
            if (dblPctDiff > 0.001)
            {
                hasGoalDiff = true;
                this.WriteLine("Error:  GoalX Function value failed: actual = {0}, expected = {1}, diff = {2}, diff% = {3}",
                    goalX, this.GoalFunctionValueX, dblDiff, dblPctDiff * 100);
            }
            else if (TestGlobals.VerboseLevel >= 1)
            {
                this.WriteLine("GoalX Function value passed: actual = {0}, expected = {1}, diff = {2}, diff% = {3}",
                    goalX, this.GoalFunctionValueX, dblDiff, dblPctDiff * 100);
            }
            if (!double.IsNaN(this.GoalFunctionValueY))
            {
                dblDiff = goalY - this.GoalFunctionValueY;
                dblPctDiff = Math.Abs(dblDiff / this.GoalFunctionValueY);
                if (dblPctDiff > 0.001)
                {
                    hasGoalDiff = true;
                    this.WriteLine("Error:  GoalY Function value failed: actual = {0}, expected = {1}, diff = {2}, diff% = {3}",
                        goalY, this.GoalFunctionValueY, dblDiff, dblPctDiff * 100);
                }
                else if (TestGlobals.VerboseLevel >= 1)
                {
                    this.WriteLine("GoalY Function value passed: actual = {0}, expected = {1}, diff = {2}, diff% = {3}",
                        goalY, this.GoalFunctionValueY, dblDiff, dblPctDiff * 100);
                }
            }
            return hasGoalDiff;
        }

        internal bool VerifyConstraint(Parameters solverParameters, Constraint cst, bool isHorizontal, ref bool violationsSeen)
        {
            if (cst.IsUnsatisfiable)
            {
                return true;
            }
            bool hasViolation = cst.Violation > solverParameters.GapTolerance;
            if (cst.IsEquality)
            {
                hasViolation = Math.Abs(cst.Violation) > solverParameters.GapTolerance;
            }
            if (hasViolation)
            {
                if (!violationsSeen)
                {
                    WriteLine("  {0} Violation(s) of Constraint(s) that were not marked Unsatisfiable:", isHorizontal ? "X" : "Y");
                    violationsSeen = true;
                }
                if (TestGlobals.VerboseLevel >= 1)
                {
                    WriteLine("    {0}", cst);
                }
            }
            return !hasViolation;
        }

        internal static void ShowRectangles(IEnumerable<VariableDef> variableDefs, IEnumerable<ClusterDef> clusterDefs)
        {
            if (!ShowRects) 
            {
                return;
            }

#if TEST_MSAGL
            var variableDebugCurves = new List<DebugCurve>();
            var clusterDebugCurves = new List<DebugCurve>();

            if (variableDefs != null)
            {
                variableDebugCurves.AddRange(variableDefs.Select(v => new Rectangle(v.Left, v.Top, v.Right, v.Bottom)).Select(
                    rect => new DebugCurve(0.1, "black", CurveFactory.CreateRectangle(rect))));
            }
            if (clusterDefs != null)
            {
                clusterDebugCurves.AddRange(clusterDefs.Select(v => new Rectangle(v.Left, v.Top, v.Right, v.Bottom)).Select(
                        rect => new DebugCurve(0.1, "green", CurveFactory.CreateRectangle(rect))));
            }

            System.Diagnostics.Debug.WriteLine("ShowRectangles: there are {0} variables and {1} clusters",
                    variableDebugCurves.Count, clusterDebugCurves.Count);
            SugiyamaLayoutSettings.ShowDebugCurvesEnumeration(variableDebugCurves.Concat(clusterDebugCurves));
#else  // TEST_MSAGL
            System.Diagnostics.Debug.WriteLine("-show* options require TEST mode");
#endif // TEST_MSAGL
        }

        internal static void ShowInitialRectangles(IEnumerable<VariableDef> variableDefs)
        {
            if (!ShowInitialRects)
            {
                return;
            }

#if TEST_MSAGL
            var debugCurves = new List<DebugCurve>();
            debugCurves.AddRange(variableDefs.Select(
                v => new Rectangle(v.InitialLeft, v.InitialTop, v.InitialRight, v.InitialBottom)).Select(
                rect => new DebugCurve(0.1, "black", CurveFactory.CreateRectangle(rect))));
            SugiyamaLayoutSettings.ShowDebugCurvesEnumeration(debugCurves);
#else  // TEST_MSAGL
            System.Diagnostics.Debug.WriteLine("-show* options require TEST mode");
#endif // TEST_MSAGL
        }
    }
}
