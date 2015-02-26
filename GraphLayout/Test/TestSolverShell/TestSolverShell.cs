/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using Microsoft.Msagl.Core.ProjectionSolver;
using ProjSolv = Microsoft.Msagl.Core.ProjectionSolver;

//
// A few simple tests of SolverShell to verify basic functionality.
//

namespace TestSolverShell {
    class TestSolverShell {
        static int s_cExecutionLimitExceeded;
        static int s_cExecutionFailed;

        static int Main(string[] args) {
            // Simple little cmdline parsing... allow all tests, or one test name, for now.
            bool fFoundTestName = (0 == args.Length);       // false if there is a name supplied
            if ((0 == args.Length) || (0 == string.Compare("zero_g", args[0], true /*ignorecase*/))) {
                Test_zero_g();
                fFoundTestName = true;
            }
            if ((0 == args.Length) || (0 == string.Compare("random", args[0], true /*ignorecase*/))) {
                Test_random();
                fFoundTestName = true;
            }
            if ((0 == args.Length) || (0 == string.Compare("dummy_ideal_position", args[0], true /*ignorecase*/))) {
                Test_dummy_ideal_position();
                fFoundTestName = true;
            }

            int iRet = 0;
            if (!fFoundTestName) {
                System.Console.WriteLine("  Error - Unknown test name '{0}", args[0]);
                iRet = 1;
            }

            if (0 != s_cExecutionFailed) {
                System.Console.WriteLine("  Error - {0} tests failed", s_cExecutionFailed);
                iRet = 1;
            }
            else {
                System.Console.WriteLine("  All tests completed");

            }
            if (0 != s_cExecutionLimitExceeded) {
                System.Console.WriteLine("  Warning - {0} test(s) exceeded one or more execution limits", s_cExecutionLimitExceeded);
            }
            return iRet;
        }

        static bool Solve(ISolverShell solver) {
            return Solve(solver, null);
        }

        static bool Solve(ISolverShell solver, ProjSolv.Parameters parameters) {
            bool executionLimitExceeded = false, retval = true;
            if (!solver.Solve(parameters, out executionLimitExceeded)) {
                System.Console.WriteLine("  Error - Solve failed");
                ++s_cExecutionFailed;
                retval = false;
            }
            if (executionLimitExceeded) {
                System.Console.WriteLine("  Warning - one or more execution limits were exceeded");
                ++s_cExecutionLimitExceeded;
            }
            return retval;
        }

        public static void Test_random() {
            Random random = new Random(123);

            // Notes:
            //  Iteration 0 has a high negative alpha.
            //  Iteration 52 terminates due to QpscConvergenceQuotient, not QpscConvergenceEpsilon,
            //    with the default Parameters.
            for (int ntest = 0; ntest < 100; ntest++) {
                System.Console.WriteLine("Executing test " + ntest + "...");

                ISolverShell solver = new SolverShell();

                solver.AddVariableWithIdealPosition(1, GetRandomDouble(random), 2.0);
                solver.AddVariableWithIdealPosition(0, GetRandomDouble(random), 2.0);

                solver.AddGoalTwoVariablesAreClose(0, 1, 1.0);

                double lS = GetRandomDouble(random);
                double rS = lS + GetRandomDouble(random);

                double lT = GetRandomDouble(random);
                double rT = lT + GetRandomDouble(random);

                solver.AddFixedVariable(2, lS);
                solver.AddFixedVariable(3, rS);
                solver.AddFixedVariable(4, lT);
                solver.AddFixedVariable(5, rT);

                solver.AddLeftRightSeparationConstraint(2, 0, 0.01);
                solver.AddLeftRightSeparationConstraint(0, 3, 0.01);
                solver.AddLeftRightSeparationConstraint(4, 1, 0.01);
                solver.AddLeftRightSeparationConstraint(1, 5, 0.01);

                Solve(solver);
                System.Console.WriteLine(solver.GetVariableResolvedPosition(0));
                System.Console.WriteLine(solver.GetVariableResolvedPosition(1));
            }
        }

        private static double GetRandomDouble(Random random) {
            double res = (double)random.Next(10000);
            return res + random.NextDouble();
        }

        private static void Test_zero_g() {
            // Tests the 'g' vector becoming zero'd.
            ISolverShell solver = new SolverShell();

            solver.AddVariableWithIdealPosition(0, 236.5, 2.0);
            solver.AddVariableWithIdealPosition(1, 255.58133348304591, 2.0);
            solver.AddFixedVariable(2, 102.68749237060547);

            solver.AddGoalTwoVariablesAreClose(0, 1, 1.0);

            solver.AddLeftRightSeparationConstraint(2, 0, 0);

            Solve(solver);

            System.Console.WriteLine(solver.GetVariableResolvedPosition(0));
            System.Console.WriteLine(solver.GetVariableResolvedPosition(1));
        }

        private static void Test_dummy_ideal_position() {
            //just test for equation (x-500)^2 -> min
            ISolverShell solver = new SolverShell();

            solver.AddVariableWithIdealPosition(0, 10, 0.000001);
            solver.AddFixedVariable(1, 500);

            solver.AddGoalTwoVariablesAreClose(0, 1, 100000.0);

            // The default parameters have too large a QpscConvergenceQuotient and too
            // small an OuterProjectIterationsLimit (we don't add constraints here so the
            // inner iterations do nothing and all movement is done by the QPSC step adjustments
            // in the outer iterations).
            ProjSolv.Parameters parameters = new ProjSolv.Parameters();
            parameters.QpscConvergenceQuotient = 1e-14;
            parameters.OuterProjectIterationsLimit = 0; // no limit
            
            Solve(solver, parameters);

            System.Console.WriteLine(solver.GetVariableResolvedPosition(0));
            System.Console.WriteLine(solver.GetVariableResolvedPosition(1));
        }

    } // end class TestSolverShell
} // end namespace TestSolverShell
