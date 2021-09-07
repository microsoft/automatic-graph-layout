// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectionSolverFileTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Msagl.UnitTests.Constraints
{
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///  File-dependent tests for ProjectionSolver.
    /// </summary>
    [TestClass]
   // [Ignore]
    [DeploymentItem(@"Resources\Constraints\ProjectionSolver\Data", @"Constraints\ProjectionSolver\Data")]
    public class ProjectionSolverFileTests : ProjectionSolverVerifier
    {
        private void RunTestDataFile(string fileName)
        {
            var pathAndFileSpec = Path.Combine(TestContext.DeploymentDirectory, @"Constraints\ProjectionSolver\Data", fileName);
            var testFileReader = this.LoadTestDataFile(pathAndFileSpec);
            int violationCount;
            Validate.IsTrue(CheckResult(testFileReader.VariableDefs,
                            testFileReader.ConstraintDefsX,
                            testFileReader.NeighborDefs,
                            true /* fCheckResults */,
                            out violationCount), FailureString);
        }

        // ReSharper disable InconsistentNaming

        [TestMethod]
        [Timeout(2000)]
        [Description("Smaller test with expected cycles, equality constraints, and wide ranges of positions, gaps, and weights.")]
        public void Cycles_Vars100_ConstraintsMax10_EqualityConstraints_PosMax1M_GapMax100K_WeightMax10K_Cycles10()
        {
            RunTestDataFile("Cycles_Vars100_ConstraintsMax10_EqualityConstraints_PosMax1M_GapMax100K_WeightMax10K_Cycles10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Smaller test with expected cycles, no equality constraints, and wide ranges of positions, gaps, and weights.")]
        public void Cycles_Vars100_ConstraintsMax10_PosMax1M_GapMax100K_WeightMax10K_Cycles10()
        {
            RunTestDataFile("Cycles_Vars100_ConstraintsMax10_PosMax1M_GapMax100K_WeightMax10K_Cycles10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Larger test with expected cycles, equality constraints, and wide ranges of positions, gaps, and weights.")]
        public void Cycles_Vars500_ConstraintsMax10_EqualityConstraints_PosMax1M_GapMax100K_WeightMax10K_Cycles10()
        {
            this.RunTestDataFile("Cycles_Vars500_ConstraintsMax10_EqualityConstraints_PosMax1M_GapMax100k_WeightMax10k_Cycles10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Larger test with expected cycles, no equality constraints, and wide ranges of positions, gaps, and weights.")]
        public void Cycles_Vars500_ConstraintsMax10_PosMax1M_GapMax100K_WeightMax10K_Cycles10()
        {
            RunTestDataFile("Cycles_Vars500_ConstraintsMax10_PosMax1M_GapMax100K_WeightMax10K_Cycles10.txt");
        }

        [TestMethod]
        [Timeout(6000)]
        [Description("Test with Neighbors with weights between 1 and 100, and variable weights at 90% 1.0, 10% 1e6.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Neighbors_Vars1000_ConstraintsMax10_NeighborsMax10_NeighborWeightMax100_VarWeights_1_To_1E6_At_10_Percent()
        {
            RunTestDataFile("Neighbors_Vars1000_ConstraintsMax10_NeighborsMax10_NeighborWeightMax100_VarWeights_1_To_1E6_At_10_Percent.txt");
        }

        [TestMethod]
        [Timeout(7000)]
        [Description("Test with Neighbors and weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Neighbors_Vars1000_ConstraintsMax10_NeighborsMax10_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars1000_ConstraintsMax10_NeighborsMax10_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(5000)]
        [Description("Test with Neighbors and weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Neighbors_Vars1000_ConstraintsMax3_NeighborsMax3_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars1000_ConstraintsMax3_NeighborsMax3_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars100_ConstraintsMax10_NeighborsMax10_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars100_ConstraintsMax10_NeighborsMax10_WeightMax100.txt");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Re"), TestMethod]
        [Timeout(2000)]
        [Description("Test re-gapping with Neighbors and weights.")]
        public void Neighbors_Vars100_ConstraintsMax10_NeighborsMax10_WeightMax100__ReGap()
        {
            ReGapInterval = 3;
            RestoreGapsAndReSolve = true;
            RunTestDataFile("Neighbors_Vars100_ConstraintsMax10_NeighborsMax10_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars100_ConstraintsMax3_NeighborsMax3_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars100_ConstraintsMax3_NeighborsMax3_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars10_ConstraintsMax10_NeighborsMax10_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars10_ConstraintsMax10_NeighborsMax10_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars10_ConstraintsMax3_NeighborsMax3_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars10_ConstraintsMax3_NeighborsMax3_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars200_ConstraintsMax10_NeighborsMax10_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars200_ConstraintsMax10_NeighborsMax10_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars200_ConstraintsMax3_NeighborsMax3_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars200_ConstraintsMax3_NeighborsMax3_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(40 * 1000)]
        [Description("Test with Neighbors and weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Neighbors_Vars2500_ConstraintsMax10_NeighborsMax10_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars2500_ConstraintsMax10_NeighborsMax10_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars300_ConstraintsMax10_NeighborsMax10_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars300_ConstraintsMax10_NeighborsMax10_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars300_ConstraintsMax3_NeighborsMax3_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars300_ConstraintsMax3_NeighborsMax3_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars400_ConstraintsMax10_NeighborsMax10_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars400_ConstraintsMax10_NeighborsMax10_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars400_ConstraintsMax3_NeighborsMax3_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars400_ConstraintsMax3_NeighborsMax3_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars500_ConstraintsMax10_NeighborsMax10_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars500_ConstraintsMax10_NeighborsMax10_WeightMax100.txt");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Re"), TestMethod]
        [Timeout(4000)]
        [Description("Test regapping with Neighbors and weights.")]
        public void Neighbors_Vars500_ConstraintsMax10_NeighborsMax10_WeightMax100__ReGap()
        {
            ReGapInterval = 3;
            RestoreGapsAndReSolve = true;
            RunTestDataFile("Neighbors_Vars500_ConstraintsMax10_NeighborsMax10_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with Neighbors and weights.")]
        public void Neighbors_Vars500_ConstraintsMax3_NeighborsMax3_WeightMax100()
        {
            RunTestDataFile("Neighbors_Vars500_ConstraintsMax3_NeighborsMax3_WeightMax100.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver1_Vars100_ConstraintsMax10()
        {
            RunTestDataFile("Solver1_Vars100_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver1_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Solver1_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(5000)]
        [Description("Test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver1_Vars1000_ConstraintsMax10()
        {
            RunTestDataFile("Solver1_Vars1000_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver1_Vars200_ConstraintsMax10()
        {
            RunTestDataFile("Solver1_Vars200_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver1_Vars200_ConstraintsMax3()
        {
            RunTestDataFile("Solver1_Vars200_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver1_Vars300_ConstraintsMax10()
        {
            RunTestDataFile("Solver1_Vars300_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver1_Vars300_ConstraintsMax3()
        {
            RunTestDataFile("Solver1_Vars300_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver1_Vars400_ConstraintsMax10()
        {
            RunTestDataFile("Solver1_Vars400_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver1_Vars400_ConstraintsMax3()
        {
            RunTestDataFile("Solver1_Vars400_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver1_Vars500_ConstraintsMax10()
        {
            RunTestDataFile("Solver1_Vars500_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver1_Vars500_ConstraintsMax3()
        {
            RunTestDataFile("Solver1_Vars500_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver1_Vars600_ConstraintsMax10()
        {
            RunTestDataFile("Solver1_Vars600_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver1_Vars600_ConstraintsMax3()
        {
            RunTestDataFile("Solver1_Vars600_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver1_Vars700_ConstraintsMax10()
        {
            RunTestDataFile("Solver1_Vars700_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver1_Vars700_ConstraintsMax3()
        {
            RunTestDataFile("Solver1_Vars700_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver1_Vars800_ConstraintsMax10()
        {
            RunTestDataFile("Solver1_Vars800_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver1_Vars800_ConstraintsMax3()
        {
            RunTestDataFile("Solver1_Vars800_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2500)]
        [Description("Test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver1_Vars900_ConstraintsMax10()
        {
            RunTestDataFile("Solver1_Vars900_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2500)]
        [Description("Test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver1_Vars900_ConstraintsMax3()
        {
            RunTestDataFile("Solver1_Vars900_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver2_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Solver2_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver3_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Solver3_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver4_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Solver4_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver5_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Solver5_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver6_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Solver6_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver7_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Solver7_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver8_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Solver8_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver9_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Solver9_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(90 * 1000)]
        [Description("Large test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars10000_ConstraintsMax3()
        {
            RunTestDataFile("Solver_Vars10000_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(80 * 1000)]
        [Description("Large test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars10000_ConstraintsMax3_StartAtZero()
        {
            RunTestDataFile("Solver_Vars10000_ConstraintsMax3_StartAtZero.txt");
        }

        [TestMethod]
        [Timeout(5000)]
        [Description("Test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars1000_ConstraintsMax10()
        {
            RunTestDataFile("Solver_Vars1000_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(5000)]
        [Description("Test with random constraints, some equality constraints, and large range of positions, gaps, and weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars1000_ConstraintsMax10_EqualityConstraints_PosMax1M_GapMax100K_WeightMax10K()
        {
            RunTestDataFile("Solver_Vars1000_ConstraintsMax10_EqualityConstraints_PosMax1M_GapMax100K_WeightMax10K.txt");
        }

        [TestMethod]
        [Timeout(5000)]
        [Description("Test with random constraints and neighbors, some equality constraints, and large range of positions, gaps, and weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars1000_ConstraintsMax10_NeighborsMax3_EqualityConstraints_PosMax1M_GapMax100K_WeightMax10K()
        {
            RunTestDataFile("Solver_Vars1000_ConstraintsMax10_NeighborsMax3_EqualityConstraints_PosMax1M_GapMax100K_WeightMax10K.txt");
        }

        [TestMethod]
        [Timeout(10000)]
        [Description("Test with random constraints and neighbors, and large range of positions, gaps, and weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars1000_ConstraintsMax10_NeighborsMax3_PosMax1M_GapMax100K_WeightMax10K()
        {
            RunTestDataFile("Solver_Vars1000_ConstraintsMax10_NeighborsMax3_PosMax1M_GapMax100K_WeightMax10K.txt");
        }

        [TestMethod]
        [Timeout(5000)]
        [Description("Test with random constraints and large range of positions, gaps, and weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars1000_ConstraintsMax10_PosMax1M_GapMax100K_WeightMax10K()
        {
            RunTestDataFile("Solver_Vars1000_ConstraintsMax10_PosMax1M_GapMax100K_WeightMax10K.txt");
        }

        [TestMethod]
        [Timeout(5000)]
        [Description("Test with random constraints and all variable positions starting at zero.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars1000_ConstraintsMax10_StartAtZero()
        {
            RunTestDataFile("Solver_Vars1000_ConstraintsMax10_StartAtZero.txt");
        }

        [TestMethod]
        [Timeout(20 * 1000)]
        [Description("Test with a large number of random constraints per variable.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars1000_ConstraintsMax50()
        {
            RunTestDataFile("Solver_Vars1000_ConstraintsMax50.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and all variable positions starting at zero.")]
        public void Solver_Vars100_ConstraintsMax10_StartAtZero()
        {
            RunTestDataFile("Solver_Vars100_ConstraintsMax10_StartAtZero.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver_Vars20_ConstraintsMax7()
        {
            RunTestDataFile("Solver_Vars20_ConstraintsMax7.txt");
        }

        [TestMethod]
        [Timeout(20 * 1000)]
        [Description("Large test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars2500_ConstraintsMax10()
        {
            RunTestDataFile("Solver_Vars2500_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints.")]
        public void Solver_Vars40_ConstraintsMax7()
        {
            RunTestDataFile("Solver_Vars40_ConstraintsMax7.txt");
        }

        [TestMethod]
        [Timeout(80 * 1000)]
        [Description("Large test with random constraints.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars5000_ConstraintsMax10()
        {
            RunTestDataFile("Solver_Vars5000_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and all variable positions starting at zero.")]
        public void Solver_Vars500_ConstraintsMax10_StartAtZero()
        {
            RunTestDataFile("Solver_Vars500_ConstraintsMax10_StartAtZero.txt");
        }

        [TestMethod]
        [Timeout(3000)]
        [Description("Test with random constraints and moderate weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars1000_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars1000_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        public void Solver_Vars100_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars100_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        public void Solver_Vars100_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars100_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        public void Solver_Vars200_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars200_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        public void Solver_Vars200_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars200_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        public void Solver_Vars300_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars300_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        public void Solver_Vars300_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars300_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        public void Solver_Vars400_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars400_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        public void Solver_Vars400_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars400_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        public void Solver_Vars500_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars500_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        public void Solver_Vars500_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars500_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars600_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars600_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars600_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars600_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars700_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars700_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars700_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars700_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars800_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars800_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars800_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars800_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(3000)]
        [Description("Test with random constraints and moderate weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars900_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars900_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and moderate weights.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars900_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Solver_Vars900_ConstraintsMax3_WeightMax1K.txt");
        }
        
        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and neighbours, and wide range of positions, gaps, and variable weights.")]
        public void Solver_Vars100_ConstraintsMax10_NeighborsMax3_PosMax1M_GapMax100K_WeightMax10K()
        {
            RunTestDataFile("Solver_Vars100_ConstraintsMax10_NeighborsMax3_PosMax1M_GapMax100K_WeightMax10K.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and wide range of positions, gaps, and variable weights, with variable scale maxed at 0.01.")]
        public void Solver_Vars100_ConstraintsMax10_PosMax1M_GapMax100K_WeightMax10K_Scale01()
        {
            RunTestDataFile("Solver_Vars100_ConstraintsMax10_PosMax1M_GapMax100K_WeightMax10K_Scale01.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with random constraints and wide range of positions, gaps, and variable weights, with variable scale maxed at 1 million.")]
        public void Solver_Vars100_ConstraintsMax10_PosMax1M_GapMax100K_WeightMax10K_Scale1M()
        {
            RunTestDataFile("Solver_Vars100_ConstraintsMax10_PosMax1M_GapMax100K_WeightMax10K_Scale1m.txt");
        }

        [TestMethod]
        [Timeout(5000)]
        [Description("Test with random constraints and variable weights 90% 1.0, 10% 1e6.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars1000_ConstraintsMax10_VarWeights_1_To_1E6_At_10_Percent()
        {
            RunTestDataFile("Solver_Vars1000_ConstraintsMax10_VarWeights_1_To_1E6_At_10_Percent.txt");
        }

        [TestMethod]
        [Timeout(5000)]
        [Description("Test with random constraints and variable weights 75% 1.0, 25% 1e6.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void Solver_Vars1000_ConstraintsMax10_VarWeights_1_To_1E6_At_25_Percent()
        {
            RunTestDataFile("Solver_Vars1000_ConstraintsMax10_VarWeights_1_To_1E6_At_25_Percent.txt");
        }
    }
}
