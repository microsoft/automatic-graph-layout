// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlapRemovalTester.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Msagl.UnitTests;
using Microsoft.Msagl.UnitTests.Constraints;

using OlapBorderInfo = Microsoft.Msagl.Core.Geometry.BorderInfo;

namespace TestConstraints
{
    internal class OverlapRemovalTester : OverlapRemovalTests, ITestConstraints
    {
        #region CreateFile variables

        internal static double MinClusterSizeX;
        internal static double MinClusterSizeY;

        internal static int MaxMargin;
        internal static int MaxSize = 10;               // Set in arg list for larger variable sizes
        internal static int MaxClusters;                // Set >0 in arg list to generate N clusters with random contents
        internal static bool WantRandomClusters;        // Set true in arg list to generate 1..N random clusters
        internal static bool WantSingleClusterRoot;     // If MaxClusters > 0, create only a single root hierarchy
        internal static bool WantFixedLeftBorder;
        internal static bool WantFixedRightBorder;
        internal static bool WantFixedTopBorder;
        internal static bool WantFixedBottomBorder;

        internal static int IndexOfOneFixedCluster = -1;

        #endregion // CreateFile variables

        #region ITestConstraints implementation

        public void Reset()
        {
            Initialize();
        }

        public void ProcessFile(string strFullName)
        {
            var testFileReader = LoadFile(strFullName);
            if (!CheckResult(testFileReader.VariableDefs, testFileReader.ClusterDefs
                           , testFileReader.ConstraintDefsX, testFileReader.ConstraintsDefY
                           , true /* fCheckResults */))
            {
                TestConstraints.ListOfFailedTestAndFileNames.Add(strFullName);
            }
        }

        private static OlapBorderInfo GetBorderInfo(int idxNewCluster, double dblMargin, bool fIsFixed)
        {
            // Return a BorderInfo indicating whether it's fixed or not - don't actually calculate
            // the fixed position yet; that's done in CheckResult.  If we are only to do one fixed
            // cluster, see if this is it.
            if (!fIsFixed || ((-1 != IndexOfOneFixedCluster) && (idxNewCluster != IndexOfOneFixedCluster)))
            {
                return new OlapBorderInfo(dblMargin);
            }
            return new OlapBorderInfo(dblMargin, 0.0 /* will be filled in during CheckResult */, OlapBorderInfo.DefaultFixedWeight);
        }

        // Note: cConstraintsPerVar is an artifact of ProjectionSolver testing and is ignored
        // in OverlapRemoval since we generate only the necessary constraints for removing overlaps (it 
        // could be extended to generate additional overlaps but that has not yet been done).
        public bool CreateFile(uint cVars, uint cConstraintsPerVar, string strOutFile)
        {
            Validate.IsTrue(cVars > 0, "Test file creation requires cVars > 0");
            Validate.AreNotEqual((uint)0, cConstraintsPerVar, "Test file creation requires cConstraintsPerVar > 0");

            // Generate a new set of Variable definitions.
            var lstVarDefs = new List<VariableDef>();
            Random rng = TestConstraints.NewRng();

            // Print this so that in case of errors we can re-run with this seed.
            System.Diagnostics.Debug.WriteLine("Creating test file with seed 0x{0}", TestConstraints.RandomSeed.ToString("X"));

            //
            // This code was adapted from satisfy_inc for ProjSolver, and then the second dimension
            // and sizes were added for OverlapRemoval, as well as other extensions.
            //

            for (int idxVar = 0; idxVar < cVars; ++idxVar)
            {
                double dblPosX = TestConstraints.RoundRand(rng, TestConstraints.MaxNodePosition);
                double dblPosY = TestConstraints.RoundRand(rng, TestConstraints.MaxNodePosition);

                // Ensure nonzero sizes.
                double dblSizeX = TestConstraints.RoundRand(rng, MaxSize) + 1.0;
                double dblSizeY = TestConstraints.RoundRand(rng, MaxSize) + 1.0;

                double dblWeightX = 1.0, dblWeightY = 1.0;
                if (TestConstraints.MaxWeightToGenerate > 0.0)
                {
                    // Ensure nonzero weights.
                    dblWeightX = TestConstraints.RoundRand(rng, TestConstraints.MaxWeightToGenerate) + 0.01;
                    dblWeightY = TestConstraints.RoundRand(rng, TestConstraints.MaxWeightToGenerate) + 0.01;
                }

                lstVarDefs.Add(new VariableDef((uint)idxVar
                                                , dblPosX, dblPosY
                                                , dblSizeX, dblSizeY
                                                , dblWeightX, dblWeightY));
            } // endfor idxVar

            List<ClusterDef> lstClusDefs = null;
            if (MaxClusters > 0)
            {
                // If we are generating a random number of clusters, get that number here.
                int cClusters = MaxClusters;
                if (WantRandomClusters)
                {
                    cClusters = rng.Next(cClusters);
                }

                // Add the first cluster, at the root level - hence no parent and no borders.
                // No BorderInfo needed for root clusters - it's ignored.  RoundRand returns
                // 0 if its arg is 0.
                lstClusDefs = new List<ClusterDef>(cClusters)
                    {
                        new ClusterDef(
                            TestConstraints.RoundRand(rng, MinClusterSizeX),
                            TestConstraints.RoundRand(rng, MinClusterSizeY))
                            {
                                IsNewHierarchy = true
                            }
                    };
                if (TestGlobals.VerboseLevel >= 3)
                {
                    System.Diagnostics.Debug.WriteLine("Level-1 cluster: {0}", lstClusDefs[0].ClusterId);
                }

                // If we are doing a single hierarchy only, restrict the range to the current set of parents,
                // otherwise ourselves about a 10% chance of being at the root level instead of being nested.
                int cRootExtra = WantSingleClusterRoot ? 0 : Math.Max(cClusters / 10, 1);

                // Create the clusters, randomly selecting a parent for each from the items previously
                // put in the list.
                for (int idxNewClus = 1; idxNewClus < cClusters; ++idxNewClus)
                {
                    int idxParentClus = rng.Next(lstClusDefs.Count + cRootExtra);   // Allow out-of-bounds index as "root level" flag

                    // Margin stuff stays 0 if MaxMargin is 0 (and border stuff is ignored if it's a root cluster).
                    var clusNew = new ClusterDef(TestConstraints.RoundRand(rng, MinClusterSizeX), TestConstraints.RoundRand(rng, MinClusterSizeY),
                                               GetBorderInfo(lstClusDefs.Count, TestConstraints.RoundRand(rng, MaxMargin), WantFixedLeftBorder),
                                               GetBorderInfo(lstClusDefs.Count, TestConstraints.RoundRand(rng, MaxMargin), WantFixedRightBorder),
                                               GetBorderInfo(lstClusDefs.Count, TestConstraints.RoundRand(rng, MaxMargin), WantFixedTopBorder),
                                               GetBorderInfo(lstClusDefs.Count, TestConstraints.RoundRand(rng, MaxMargin), WantFixedBottomBorder));
                    lstClusDefs.Add(clusNew);

                    // If we are doing a single hierarchy only, restrict the range to the current set of parents.
                    // Otherwise, if the parent index is >= the index we're adding now, that's our way of
                    // selecting the node to be at the root level.
                    if (WantSingleClusterRoot)
                    {
                        idxParentClus %= idxNewClus;
                    }
                    if (idxParentClus < idxNewClus)
                    {
                        ClusterDef clusParent = lstClusDefs[idxParentClus];
                        clusParent.AddClusterDef(clusNew);
                        if (TestGlobals.VerboseLevel >= 3)
                        {
                            System.Diagnostics.Debug.Write($"Nested cluster: {clusNew.ClusterId}");
                            for (; null != clusParent; clusParent = clusParent.ParentClusterDef)
                            {
                                System.Diagnostics.Debug.Write($" {clusParent.ClusterId}");
                            }
                            System.Diagnostics.Debug.WriteLine("");
                        }
                    }
                    else
                    {
                        // Create a simple cluster since root clusters don't honor borders.
                        clusNew.IsNewHierarchy = true;
                        if (TestGlobals.VerboseLevel >= 3)
                        {
                            System.Diagnostics.Debug.WriteLine("Level-1 cluster: {0}", clusNew.ClusterId);
                        }
                    }
                }

                // Now run through the nodes and randomly assign them into the clusters.
                foreach (VariableDef varDef in lstVarDefs)
                {
                    int idxParentClus = rng.Next(lstClusDefs.Count + cRootExtra);
                    if (idxParentClus < lstClusDefs.Count)
                    {                        // Don't write the ones at the root level.
                        lstClusDefs[idxParentClus].AddVariableDef(varDef);
                    }
                    else if (TestGlobals.VerboseLevel >= 3)
                    {
                        System.Diagnostics.Debug.WriteLine("Root var: {0}", varDef.IdString);
                    }
                }
            } // endif MaxClusters > 0

            return WriteTestFile(lstVarDefs, lstClusDefs, strOutFile);
        }

        public bool ReCreateFile(string strInFile, string strOutFile)
        {
            // For investigating differences in results from the same set of variable definitions.
            // Load the file's variables into the vardefs then recreate the output from them and
            // rewrite the entire file.
            var testFileReader = LoadFile(strInFile);
            return WriteTestFile(testFileReader.VariableDefs, testFileReader.ClusterDefs, strOutFile);
        }

        private bool WriteTestFile(List<VariableDef> lstVarDefs, List<ClusterDef> lstClusDefs, string strOutFile)
        {
            var swOutFile = new StreamWriter(Path.GetFullPath(strOutFile));

            // Get the results first so we can write the goal function value and constraint counts.
            // Handle the exception so we can at least write out the variables and clusters.
            bool retval = false;
            try
            {
                // Constraint defs are filled in by the solvers in CheckResult.
                retval = CheckResult(
                    lstVarDefs, lstClusDefs, null /* iterCstDefsX */, null /* iterCstDefsY */, false /* fCheckResults */);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                swOutFile.WriteLine();
                swOutFile.WriteLine("// --- Exception thrown when generating results --- ");
                swOutFile.WriteLine();
            }

            OlapFileWriter.WriteFile(TestConstraints.RandomSeed,
                        TestConstraints.MaxWeightToGenerate,
                        SolverX,
                        SolverY,
                        SolutionX,
                        SolutionY,
                        MinPaddingX,
                        MinPaddingY,
                        MinClusterSizeX,
                        MinClusterSizeY,
                        MaxMargin,
                        lstVarDefs,
                        lstClusDefs,
                        swOutFile);
            return retval;
        }

        // Worker to ensure we set values from loaded testfiles.
        public TestFileReader LoadFile(string strFileName)
        {
            var tdf = LoadTestDataFile(strFileName);
            TestConstraints.RandomSeed = tdf.Seed;
            TestConstraints.MaxWeightToGenerate = tdf.Weight;
            MinClusterSizeX = tdf.MinClusterSizeX;
            MinClusterSizeY = tdf.MinClusterSizeY;
            MinPaddingX = tdf.PaddingX;
            MinPaddingY = tdf.PaddingY;
            MaxMargin = tdf.Margin;
            return tdf;
        }

        #endregion // ITestConstraints implementation

    }
}