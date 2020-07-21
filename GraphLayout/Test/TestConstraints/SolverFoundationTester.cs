//
// Uncomment this to remove debug traces.
//
//#define VERBOSE

// Comment this out to use the full Services object model.
//#define SIMPLESOLVER

using System;
using System.Collections.Generic;
using System.Diagnostics;
    // using Microsoft.SolverFoundation.Services; // conflicts with ProjectionSolver.Constraint
using SfServ = Microsoft.SolverFoundation.Services;

namespace TestConstraints
{
    using Microsoft.Msagl.UnitTests.Constraints;

    internal class SfVariable : ITestVariable
    {
        // Solver identifier.
#if SIMPLESOLVER
        internal int Id { get; set; }
        internal SFVariable(int idFromSolver) {
            this.Id = idFromSolver;
        }
        public override string ToString() {
            return this.Id.ToString();
        }
#else  // SIMPLESOLVER
        internal SfServ.Decision Decision { get; set; }
        internal SfVariable(SfServ.Decision d)
        {
            this.Decision = d;
        }
        public override string ToString()
        {
            return this.Decision.ToString();
        }
        internal void GetResolvedPosition()
        {
            this.ActualPos = this.Decision.GetDouble();
        }
#endif // SIMPLESOLVER

        // ITestVariable implementation.
        public double ActualPos { get; set; }
    } // end SFVariable

    internal class SolverFoundationTester : ResultVerifierBase, ITestConstraints
    {
        // No file-creation globals for SolverFoundation as it is just for testing already-created files.
        const double SizeNotUsed = 0.0;     // For SolverFoundation we use 0 as the solver doesn't use size; OverlapRemoval does.

#if SIMPLESOLVER
        // SolverFoundation solver variables.
        static InteriorPointSolver solver;
        static int goal;
        static int numOfRows=-1;
#else  // SIMPLESOLVER
        static readonly SfServ.SolverContext Context = SfServ.SolverContext.GetContext();
        static readonly List<SfServ.Constraint> Constraints = new List<SfServ.Constraint>();
        static SfServ.Model model;
        static SfServ.Term goalTerm;
        static SfServ.Solution solution;
        static SfServ.Goal goal;
#endif // SIMPLESOLVER

        //
        // These overloads work with the local Test*() routines.
        //
        bool CheckResult(VariableDef[] variableDefs, ConstraintDef[] constraintDefs
                            , NeighborDef[] rgNeighborDefs, double[] expectedPositionsX, bool fCheckResults)
        {
            for (uint id = 0; id < variableDefs.Length; ++id)
            {
                variableDefs[id].SetExpected(id, expectedPositionsX[id]);
            }

            return CheckResult(variableDefs, constraintDefs, rgNeighborDefs, fCheckResults);
        }

        //
        // ... and this overload works with DataFiles as well as performing the actual work.
        //
        bool CheckResult(IEnumerable<VariableDef> iterVariableDefs, IEnumerable<ConstraintDef> iterConstraintDefs
                            , IEnumerable<NeighborDef> iterNeighborDefs, bool fCheckResults)
        {
            CreateSolver();
            var sw = new Stopwatch();

            bool fRet = true;
            for (uint cRep = 0; cRep < TestGlobals.TestReps; ++cRep)
            {
                sw.Start();

                // Load the solver and solve it.
                foreach (VariableDef varDef in iterVariableDefs)
                {
                    AddNodeWithIdealPosition(varDef);
                }
                foreach (ConstraintDef cstDef in iterConstraintDefs)
                {
                    AddLeftRightSeparationConstraint(cstDef.LeftVariableDef, cstDef.RightVariableDef, cstDef.Gap, cstDef.IsEquality);
                }
                if (null != iterNeighborDefs)
                {
                    foreach (var nbourDef in iterNeighborDefs)
                    {
                        AddGoalTwoNodesAreClose(nbourDef.LeftVariableDef, nbourDef.RightVariableDef, nbourDef.Weight);
                    }
                }

                if (!Solve())
                {
                    System.Diagnostics.Debug.WriteLine("Error: SolverFoundation.Solve returned false");
                    fRet = false;
                }

                // Transfer positions into variables
                foreach (VariableDef varDef in iterVariableDefs)
                {
                    GetNodeResolvedPosition(varDef);
                }

                sw.Stop();

                bool fViolationSeen = false;
                foreach (ConstraintDef cstDef in iterConstraintDefs)
                {
                    // Make this slightly more permissive than the default ProjectionSolver.Parameters.GapTolerance.
                    if (!cstDef.VerifyGap(1e-3))
                    {
                        if (!fViolationSeen)
                        {
                            System.Diagnostics.Debug.WriteLine("Error: H Constraint Violation(s):");
                            fViolationSeen = true;
                        }
                        if (TestGlobals.VerboseLevel >= 1)
                        {
                            System.Diagnostics.Debug.WriteLine("    {0} <--> {1} {2} {3:F5}"
                                            , cstDef.LeftVariableDef.Ordinal, cstDef.RightVariableDef.Ordinal
                                            , cstDef.IsEquality ? "==" : ">=", cstDef.Gap);
                        }
                        fRet = false;
                    }
                }
            } // endfor each test rep

            // Verify the Variable positions are as expected.
            if (!PostCheckResults(iterVariableDefs, goal.ToDouble(), double.NaN, sw, fCheckResults))
            {
                fRet = false;
            }
            if (TestGlobals.VerboseLevel >= 1)
            {
                System.Diagnostics.Debug.WriteLine("  Completion status: {0}", solution.Quality);
                System.Diagnostics.Debug.WriteLine("  Goal Function value: {0}", goal.ToDouble());
            }

            return fRet;
        } // end CheckResult()

        //
        // Test() functions are taken from Test_ProjectionSolver.
        //
        public bool Test1()
        {
            var expectedPositionsX = new [] { 1.4, 4.4, 7.4, 7.4, 10.4 };
            var variableDefs = new [] {
                  new VariableDef(2, SizeNotUsed)
                , new VariableDef(9, SizeNotUsed)
                , new VariableDef(9, SizeNotUsed)

                , new VariableDef(9, SizeNotUsed)
                , new VariableDef(2, SizeNotUsed)
            };
            var constraintDefs = new [] {
                  new ConstraintDef(variableDefs[0], variableDefs[4], 3)
                , new ConstraintDef(variableDefs[0], variableDefs[1], 3)
                , new ConstraintDef(variableDefs[1], variableDefs[2], 3)

                , new ConstraintDef(variableDefs[2], variableDefs[4], 3)
                , new ConstraintDef(variableDefs[3], variableDefs[4], 3)
            };
            return CheckResult(variableDefs, constraintDefs, null /*nbours*/, expectedPositionsX, true /* fCheckResults */);
        }

        public bool Test2()
        {
            var expectedPositionsX = new [] { 0.5, 6, 3.5, 6.5, 9.5 };
            var variableDefs = new [] {
                  new VariableDef(4, SizeNotUsed)
                , new VariableDef(6, SizeNotUsed)
                , new VariableDef(9, SizeNotUsed)

                , new VariableDef(2, SizeNotUsed)
                , new VariableDef(5, SizeNotUsed)
            };
            var constraintDefs = new [] {
                  new ConstraintDef(variableDefs[0], variableDefs[2], 3)
                , new ConstraintDef(variableDefs[0], variableDefs[3], 3)
                , new ConstraintDef(variableDefs[1], variableDefs[4], 3)

                , new ConstraintDef(variableDefs[2], variableDefs[4], 3)
                , new ConstraintDef(variableDefs[2], variableDefs[3], 3)
                , new ConstraintDef(variableDefs[3], variableDefs[4], 3)
            };
            return CheckResult(variableDefs, constraintDefs, null /*nbours*/, expectedPositionsX, true /* fCheckResults */);
        }

        // ReSharper disable InconsistentNaming

        public bool Test2_Nbours()
        {
            //Non-nbours Test2 :                  { 0.50000, 6.00000, 3.50000, 6.50000, 9.50000 };
            var expectedPositionsX = new [] { 0.40385, 6.38462, 3.40385, 6.40385, 9.40385 };
            var variableDefs = new []
                {
                    new VariableDef(4, SizeNotUsed), new VariableDef(6, SizeNotUsed), new VariableDef(9, SizeNotUsed),
                    new VariableDef(2, SizeNotUsed), new VariableDef(5, SizeNotUsed)
                };
            var constraintDefs = new []
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            var neighborDefs = new []
                {
                    new NeighborDef(variableDefs[0], variableDefs[4], 10.0),
                    new NeighborDef(variableDefs[1], variableDefs[3], 20.0)
                };

            // See comments in Test_ProjectionSolver.Test2_Nbours
            return CheckResult(variableDefs, constraintDefs, neighborDefs, expectedPositionsX, true /* fCheckResults */);
        }

        public bool Test_Scale5()
        {
            var expectedPositionsX = new [] {
                5.512113055, 
                1.402422611,
                3.506056528,
                8.512113055,
                10.01211306
            };
            var variableDefs = new [] {
                  new VariableDef(8, SizeNotUsed, 10) { ScaleX = 2}
                , new VariableDef(9, SizeNotUsed, 1)  { ScaleX = 10}
                , new VariableDef(4, SizeNotUsed, 9)  { ScaleX = 4}

                , new VariableDef(7, SizeNotUsed, 7)  { ScaleX = 2}
                , new VariableDef(4, SizeNotUsed, 3)  { ScaleX = 2}
            };
            var constraintDefs = new [] {
                  new ConstraintDef(variableDefs[0], variableDefs[2], 3)
                , new ConstraintDef(variableDefs[2], variableDefs[3], 3)
                , new ConstraintDef(variableDefs[1], variableDefs[3], 3)
                , new ConstraintDef(variableDefs[3], variableDefs[4], 3)
            };

            // See comments in Test_ProjectionSolver.Test2_Nbours
            return CheckResult(variableDefs, constraintDefs, null /*nbours*/, expectedPositionsX, true /* fCheckResults */);
        }

        public bool Test_QPSC_no_constraints_weight_difference_1e14()
        {
            var expectedPositionsX = new [] { 
                500.00000           // was 499.97502
                , 500.00000         // was 499.99999
            };

            var variableDefs = new [] {
                  new VariableDef(10.0, SizeNotUsed, 0.000001)
                , new VariableDef(500, SizeNotUsed, 1e8)
            };
            var neighborDefs = new [] {
                  new NeighborDef(variableDefs[0], variableDefs[1], 100000.0)
            };

            // Note:  With scaling and such a wide weight variation we don't need to reset parameters
            // (see Test_QPSC_no_constraints_weight_difference_1e4 for a case where we do).

            GoalFunctionValueX = -24999999999999.8;
            var result = CheckResult(variableDefs, new ConstraintDef[0], neighborDefs, expectedPositionsX, true /* fCheckResults */);

            // This allows inspecting the actual values to examine at greater precision if desired.
            //System.Diagnostics.Debug.WriteLine("Full Pos: var0 {0}, var1 {1}", rgVariableDefs[0].ActualPosX, rgVariableDefs[1].ActualPosX);
            return result;
        }

        public bool Test_QPSC_no_constraints_weight_difference_1e4()
        {
            var expectedPositionsX = new [] { 499.95096, 499.95100 };

            var variableDefs = new [] {
                  new VariableDef(10.0, SizeNotUsed, 0.01)
                , new VariableDef(500, SizeNotUsed, 1e2)
            };
            var neighborDefs = new [] {
                  new NeighborDef(variableDefs[0], variableDefs[1], 100000.0)
            };

            // Note:  SF does not have parameters for this test as there are in Test_ProjectionSolver.

            GoalFunctionValueX = -24997600.2403185;
            return CheckResult(variableDefs, new ConstraintDef[0], neighborDefs, expectedPositionsX, true /* fCheckResults */);
        }

        public bool Test_QPSC_no_constraints_weight_difference_1e4_no_neighbor_weight()
        {
            var expectedPositionsX = new [] { 495.10048, 499.95149 };

            var variableDefs = new [] {
                  new VariableDef(10.0, SizeNotUsed, 0.01)
                , new VariableDef(500, SizeNotUsed, 1e2)
            };
            var neighborDefs = new [] {
                  new NeighborDef(variableDefs[0], variableDefs[1], 1.0)
            };

            GoalFunctionValueX = -24997624.007623;
            return CheckResult(variableDefs, new ConstraintDef[0], neighborDefs, expectedPositionsX, true /* fCheckResults */);
        }

        #region SolverFoundation wrappers
#if SIMPLESOLVER
        static void CreateSolver() {
            StaticReset();
            solver = new InteriorPointSolver();
            solver.AddRow("goal", out goal);
            solver.AddGoal(goal, 0, true); //minimizing the goal
        }

        // trying to position a node at a certain place
        static void AddNodeWithIdealPosition(VariableDef node) {
            int x = GetOrCreateVar(node);
            //adding the term weight*(x-position)^2 
            SetGoalCoefficient(node.WeightX, x, x);     // Apparently multiplying by 2 is not necessary
            SetGoalCoefficient(x, -2 * node.WeightX * node.m_dblDesiredPosX);
        }

        // leftNode+gap leq RightNode 
        static void AddLeftRightSeparationConstraint(VariableDef leftNode, VariableDef rightNode, double gap, bool isEquality) {
            var left = GetOrCreateVar(leftNode);
            var right = GetOrCreateVar(rightNode);
            int rightMinusLeft;
            solver.AddRow( numOfRows--, out rightMinusLeft);    // Constraints have negative row numbers

            // TODO:  Equality constraints?
            solver.SetCoefficient(rightMinusLeft, right, 1); //right-left>=gap
            solver.SetCoefficient(rightMinusLeft, left, -1);
            solver.SetLowerBound(rightMinusLeft, gap);
        }

        // (i-j)*(i-j)*coefficient is added to the goal
        static void AddGoalTwoNodesAreClose(VariableDef i, VariableDef j, double weight) {
            int x = GetOrCreateVar(i);
            int y = GetOrCreateVar(j);

            // weight * (x * x - 2 * x * y + y * y)
            SetGoalCoefficient(weight, x, x);
            SetGoalCoefficient(-2 * weight, x, y);      // Apparently this handles y,x as well
            SetGoalCoefficient(weight, y, y);
        }

        static void SetGoalCoefficient(double coeff, int xId, int yId) {
            var existingCoef = solver.GetCoefficient(goal, xId, yId);
            solver.SetCoefficient(goal, existingCoef + coeff, xId, yId);
        }

        static void SetGoalCoefficient(int xId, double coeff) {
            var existingCoef = solver.GetCoefficient(goal, xId);
            solver.SetCoefficient(goal, xId, existingCoef + coeff);
        }

        static int GetOrCreateVar(VariableDef varDef) {
            if (null == (object)varDef.m_varX) {
                int d;
                if (!solver.AddVariable((int)varDef.Ordinal, out d)) {
                    throw new ApplicationException("Cannot Create SF Variable");
                }
                solver.SetBounds(d, -10000000.0, 10000000.0); // TODO a hack - to make the solver happy - should be fixed later
                varDef.m_varX = new SFVariable(d);
                System.Diagnostics.Debug.Assert(d >= 0, "expected: d >= 0");
                return d;
            }
            return ((SFVariable)varDef.m_varX).Id;
        }

        static bool Solve() {
            Microsoft.SolverFoundation.Services.ILinearSolution solution = solver.Solve(new InteriorPointSolverParams());
            return solution.LpResult == Microsoft.SolverFoundation.Services.LinearResult.Optimal;
        }

        static void GetNodeResolvedPosition(VariableDef varDef) {
            ((SFVariable)varDef.m_varX).ActualPos = (double)solver.GetValue(GetOrCreateVar(varDef));
        }

#else  // SIMPLESOLVER

        static void CreateSolver()
        {
            StaticReset();
        }

        // meaning that we would like position i at "position"
        static void AddNodeWithIdealPosition(VariableDef varDef)
        {
            double weight = varDef.WeightX;
            double position = varDef.DesiredPosX;
            SfServ.Decision x = GetOrCreateDecision(varDef);

            //adding the term (ix-pos)^2; workaround for bug processing x(x - 2*position)
            SfServ.Term term = weight * (x * x - 2 * position * x);
            //model.AddConstraint(varDef.Ordinal.ToString() + "ttt", x >= -100000.0);    // TODO: hack to make the solver happy - fix it later!!!!
            AddTermToGoalTerm(term);
            return;
        }

        static void AddTermToGoalTerm(SfServ.Term term)
        {
            if (null == (object)goalTerm)
            {
                goalTerm = term;
            }
            else
            {
                goalTerm += term;
            }
        }

        // leftNode+gap leq RightNode 
        static void AddLeftRightSeparationConstraint(VariableDef leftNode, VariableDef rightNode, double gap, bool isEquality)
        {
            SfServ.Decision leftDecision = GetOrCreateDecision(leftNode);
            SfServ.Decision rightDecision = GetOrCreateDecision(rightNode);

            SfServ.Term term;
            if (isEquality)
            {
                term = (leftDecision * leftNode.ScaleX) + gap == (rightDecision * rightNode.ScaleX);
            }
            else
            {
                term = (leftDecision * leftNode.ScaleX) + gap <= (rightDecision * rightNode.ScaleX);
            }
            Constraints.Add(model.AddConstraint(Constraints.Count.ToString(), term));
        }

        // (i-j)*(i-j)*coefficient is added to the goal
        static void AddGoalTwoNodesAreClose(VariableDef i, VariableDef j, double weight)
        {
            SfServ.Decision x = GetOrCreateDecision(i);
            SfServ.Decision y = GetOrCreateDecision(j);
            SfServ.Term term = weight * (x * x - 2 * x * y + y * y);
            AddTermToGoalTerm(term);
        }

        static SfServ.Decision GetOrCreateDecision(VariableDef varDef)
        {
            if (null == varDef.VariableX)
            {
                var d = new SfServ.Decision(SfServ.Domain.Real, varDef.Ordinal.ToString());
                varDef.VariableX = new SfVariable(d);
                model.AddDecision(d);
                return d;
            }
            return ((SfVariable)varDef.VariableX).Decision;
        }

        static bool Solve()
        {
            if (null != goal)
            {
                model.RemoveGoal(goal);
            }
            if (null == (object)goalTerm)
            {
                goalTerm = 0;
            }
            goal = model.AddGoal("goal", SfServ.GoalKind.Minimize, goalTerm);
            solution = Context.Solve();
            if (TestGlobals.VerboseLevel >= 2)
            {
                SfServ.Report report = solution.GetReport();
                System.Diagnostics.Debug.WriteLine(report.ToString());
            }
            return solution.Quality == SfServ.SolverQuality.Optimal;
        }

        static void GetNodeResolvedPosition(VariableDef varDef)
        {
            ((SfVariable)varDef.VariableX).GetResolvedPosition();
        }

#endif // SIMPLESOLVER

        #endregion // SolverFoundation wrappers

        #region ITestConstraints implementation

        static void StaticReset()
        {
#if SIMPLESOLVER
            solver = null;
            goal = 0;
            numOfRows = -1;
#else  // SIMPLESOLVER
            solution = null;
            Context.ClearModel();
            model = Context.CreateModel();
            Constraints.Clear();
            goalTerm = null;
            solution = null;
            goal = null;
#endif // SIMPLESOLVER
        }

        public void Reset()
        {
            StaticReset();
        }

        public void ProcessFile(string strFullName)
        {
            var testDataFile = LoadFile(strFullName);
            if (0.0 != TestConstraints.ProcessFilesFakeScale)
            {
                testDataFile.VariableDefs.ForEach(varDef => varDef.ScaleX = TestConstraints.ProcessFilesFakeScale);
            }


            if (!CheckResult(testDataFile.VariableDefs, testDataFile.ConstraintDefsX
                            , testDataFile.NeighborDefs, true /* fCheckResults */))
            {
                TestConstraints.ListOfFailedTestAndFileNames.Add(strFullName);
            }
        }

        public bool CreateFile(uint cVars, uint cConstraintsPerVar, string strOutFile)
        {
            throw new InvalidOperationException("CreateFile not supported for SolverFoundation");
        }

        public bool ReCreateFile(string strInFile, string strOutFile)
        {
            throw new InvalidOperationException("ReCreateFile not supported for SolverFoundation");
        }

        public TestFileReader LoadFile(string strFileName)
        {
            // The SolverFoundation stuff has no base class in UnitTests so just load the file directly here.
            var tdf = new TestFileReader(/*isTwoDimensional:*/ false);
            tdf.Load(strFileName);
            return tdf;
        }
        #endregion // ITestConstraints implementation

    }
}
