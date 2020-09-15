// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Test_ProjectionSolver.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using Microsoft.Msagl.UnitTests.Constraints;

namespace TestConstraints
{
    internal class ProjectionSolverTester : ProjectionSolverTests, ITestConstraints
    {
        #region CreateFile variables

        internal static int NumberOfNeighboursPerVar;       // For neighbour-pair test generation
        internal static bool WantEqualityConstraints;       // For Equality constraint generation.
        internal static bool WantStartAtZero;               // For forcing all variables in random generation to start at 0.
        internal static int MaxGapToGenerate = 3;           // Set in TestConstraints cmdline arg list for larger gap range.
        internal static double MaxScaleToGenerate;          // Set >0 in arg list to generate random scales in that range

        #endregion // CreateFile variables

        #region ITestConstraints implementation

        public void Reset()
        {
            Initialize();
        }

        public void ProcessFile(string strFullName)
        {
            var testFileReader = LoadFile(strFullName);
            if (0.0 != TestConstraints.ProcessFilesFakeScale)
            {
                testFileReader.VariableDefs.ForEach(varDef => varDef.ScaleX = TestConstraints.ProcessFilesFakeScale);
            }

            int cViolations;
            if (!CheckResult(testFileReader.VariableDefs,
                            testFileReader.ConstraintDefsX,
                            testFileReader.NeighborDefs,
                            true /* fCheckResults */,
                            out cViolations))
            {
                TestConstraints.ListOfFailedTestAndFileNames.Add(strFullName);
            }
        }

        public bool CreateFile(uint cVars, uint cConstraintsPerVar, string strOutFile)
        {
            Debug.Assert(cVars > 0, "Test file creation requires cVars > 0");
            Debug.Assert(0 != cConstraintsPerVar, "Test file creation requires cConstraintsPerVar > 0");

            // Generate a new set of Variable and Constraint definitions.
            var lstVarDefs = new List<VariableDef>();
            var rng = TestConstraints.NewRng();

            // Print this so that in case of errors we can re-run with this seed.
            System.Diagnostics.Debug.WriteLine("Creating test file with seed 0x{0}", TestConstraints.RandomSeed.ToString("X"));

            //
            // This code is adapted and extended from satisfy_inc.
            //
            for (int idxVar = 0; idxVar < cVars; ++idxVar)
            {
                // Assign initial variable positions in the range [0..TestConstraints.MaxNodePosition].
                double dblPos = WantStartAtZero ? 0.0 : TestConstraints.RoundRand(rng, TestConstraints.MaxNodePosition);
                double dblWeight = 1.0;
                if (TestConstraints.MaxWeightToGenerate > 0.0)
                {
                    // Ensure nonzero weight.
                    dblWeight = TestConstraints.RoundRand(rng, TestConstraints.MaxWeightToGenerate);
                    if (dblWeight < 0.01)
                    {
                        dblWeight = 0.01;
                    }
                    Debug.Assert(dblWeight >= 0.01, "Random variable weight assignment is less than expected");
                }
                lstVarDefs.Add(new VariableDef((uint)idxVar, dblPos, ValueNotUsed, dblWeight));
            } // endfor idxVar

            var lstCstDefs = new List<ConstraintDef>();

            // Create constraints outgoing from every variable except the final one.
            for (int idxLhs = 0; idxLhs < (cVars - 1); ++idxLhs)
            {
                // rng.Next returns a value in [0..arg-1], so add 1.
                int cCurCst = rng.Next((int)cConstraintsPerVar) + 1;

                // Randomly make an equality constraint if the lhs variable is not currently in such
                // a constraint (avoid transitivity, which may lead to impossible-to-satisfy conditions;
                // checking lhs is sufficient as we'll only create equality to the lhs+1 variable).
                int idxCst = 0;         // Current index from 0 .. cCurCst-1
                int iRhsBump = 1;       // Will be used to skip the immediate-next if we've created an == cst to it
                if (WantEqualityConstraints && !lstVarDefs[idxLhs].IsInEqualityConstraint)
                {
                    // Test one (mid-digit) bit for about a 50% likelihood, less the likelihood of lhs being 
                    // the rhs of an equality constraint from lhs-1; we want flexibility between equality
                    // constraints so we don't have unsatisfiable cases like
                    //      a + 3 == b; b + 3 == c; a + 9 <= c
                    const int Mask = 0x10;
                    if ((rng.Next() & Mask) == Mask)
                    {
                        double dblGap = TestConstraints.RoundRand(rng, MaxGapToGenerate);
                        lstCstDefs.Add(new ConstraintDef(lstVarDefs[idxLhs], lstVarDefs[idxLhs + 1], dblGap, true /*fEquality*/));
                        if ((cVars - 2) == idxLhs)
                        {
                            // This was the next-to-last variable and we've made an equality constraint
                            // to the next one, so don't make any more constraints for this var.
                            continue;
                        }
                    }
                    ++idxCst;
                    ++iRhsBump;
                }

                for (; idxCst < cCurCst; ++idxCst)
                {
                    // Create a constraint from the current idxLhs (lhs) to a randomly-determined
                    // rhs variable that has an ordinal higher than lhs.  This ensures no constraint
                    // cycles, with the number and length of chains depending upon cConstraintsPerVar.
                    int idxRhs = rng.Next(idxLhs, (int)cVars - iRhsBump) + iRhsBump;
                    idxRhs = (int)Math.Min(idxRhs, cVars - 1);

                    double dblGap = TestConstraints.RoundRand(rng, MaxGapToGenerate);
                    lstCstDefs.Add(new ConstraintDef(lstVarDefs[idxLhs], lstVarDefs[idxRhs], dblGap, false /*fEquality*/));
                }
            } // endfor create non-cyclic constraints

            // Create cyclic constraints if requested by creating constraints from higher indexes to lower.
            // Do this randomly so that chains of varying length are created; some reverse constraints may
            // be harmless but most should create cycles.
            for (int idxCst = 0; idxCst < NumberOfCyclesToCreate; ++idxCst)
            {
                int idxLhs = rng.Next((int)cVars - 1);
                int idxRhs = rng.Next(idxLhs, (int)cVars - 1) + 1;
                double dblGap = TestConstraints.RoundRand(rng, MaxGapToGenerate);

                // Create in reverse direction.
                lstCstDefs.Add(new ConstraintDef(lstVarDefs[idxRhs], lstVarDefs[idxLhs], dblGap, false /*fEquality*/));
            }

            var lstNbourDefs = new List<NeighborDef>();

            if (NumberOfNeighboursPerVar > 0)
            {
                // Create neighbours outgoing from every variable except the final one.
                for (int idxLhs = 0; idxLhs < (cVars - 1); ++idxLhs)
                {
                    // rng.Next returns a value in [0..arg-1], so add 1.
                    int cCurNbour = rng.Next(NumberOfNeighboursPerVar) + 1;
                    for (uint idxNbour = 0; idxNbour < cCurNbour; ++idxNbour)
                    {
                        // Create a neighbour from the current idxLhs (lhs) to a randomly-determined
                        // rhs variable that has an ordinal higher than lhs.  This ensures no neighbour
                        // cycles, with the number and length of chains depending upon cNeighboursPerVar.
                        int idxRhs = rng.Next(idxLhs, (int)cVars - 1) + 1;

                        double dblWeight = 1.0;
                        if (TestConstraints.MaxWeightToGenerate > 0.0)
                        {
                            // Create the neighbour relationship with random non-zero weight.
                            dblWeight = TestConstraints.RoundRand(rng, TestConstraints.MaxWeightToGenerate);
                            if (dblWeight < 0.01)
                            {
                                dblWeight = 0.01;
                            }
                            Debug.Assert(dblWeight >= 0.01, "Random neighbour weight assignment is less than expected");
                        }
                        lstNbourDefs.Add(new NeighborDef(lstVarDefs[idxLhs], lstVarDefs[idxRhs], dblWeight));
                    }
                } // endfor create neighbours
            } // endif create neighbours

            // Do variable scale last; scaling was added after files were generated so we don't want to
            // affect the rng sequence before this.  These can end up very small in qpsc so we'll want
            // to support creating them much smaller than weight.
            if (0.0 != MaxScaleToGenerate)
            {
                foreach (var varDef in lstVarDefs)
                {
                    // Ensure nonzero scale.
                    varDef.ScaleX = Math.Max(TestConstraints.RoundRand(rng, MaxScaleToGenerate), 1e-6);
                }
            }

            return WriteTestFile(lstVarDefs, lstCstDefs, lstNbourDefs, strOutFile);
        } // end CreateFile()

        public bool ReCreateFile(string strInFile, string strOutFile)
        {
            // For investigating differences in results from the same set of variable definitions.
            // Load the file's variables into the vardefs then recreate the output from them and
            // rewrite the entire file.
            var testDataFile = LoadFile(strInFile);

            return WriteTestFile(testDataFile.VariableDefs, testDataFile.ConstraintDefsX
                                    , testDataFile.NeighborDefs, strOutFile);
        }

        private bool WriteTestFile(List<VariableDef> lstVarDefs, List<ConstraintDef> lstCstDefs
                                     , List<NeighborDef> lstNbourDefs, string strOutFile)
        {
            var swOutFile = new StreamWriter(Path.GetFullPath(strOutFile));

            // Get the results first so we can write the goal function value.
            // Handle the exception so we can at least write out the variables and constraints.
            bool fSuccess = false;
            int cViolations = 0;
            try
            {
                fSuccess = CheckResult(lstVarDefs, lstCstDefs, lstNbourDefs, false /* fCheckResults */, out cViolations);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                swOutFile.WriteLine();
                swOutFile.WriteLine("// --- Exception thrown when generating results --- ");
                swOutFile.WriteLine();
            }

            ProjFileWriter.WriteFile(TestConstraints.RandomSeed,
                        TestConstraints.MaxWeightToGenerate,
                        MaxScaleToGenerate,
                        this.SolutionX,
                        lstVarDefs,
                        lstCstDefs,
                        lstNbourDefs,
                        swOutFile,
                        cViolations);
            return fSuccess;
        }

        // Worker to ensure we set values from loaded testfiles.
        public TestFileReader LoadFile(string strFileName)
        {
            var tdf = LoadTestDataFile(strFileName);
            TestConstraints.RandomSeed = tdf.Seed;
            TestConstraints.MaxWeightToGenerate = tdf.Weight;
            MaxScaleToGenerate = tdf.Scale;
            return tdf;
        }

        #endregion // ITestConstraints implementation
    }
}
