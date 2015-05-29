// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlapRemovalFileTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using OlapCluster = Microsoft.Msagl.Core.Geometry.OverlapRemovalCluster;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///  File-dependent tests for OverlapRemoval.
    /// </summary>
    [TestClass]
    [DeploymentItem(@"Resources\Constraints\OverlapRemoval\Data", @"Constraints\OverlapRemoval\Data")]
    public class OverlapRemovalFileTests : OverlapRemovalVerifier
    {
        [ClassInitialize]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "testContext")]
        public static void ClassInitialize(TestContext testContext)
        {
            OverlapRemovalTests.ClassInitialize(testContext);
        }

        [ClassCleanup]
        public static void ClassCleanup() { RestoreDefaultTraceListener(); }

        private void RunTestDataFile(string fileName)
        {
            var pathAndFileSpec = Path.Combine(TestContext.DeploymentDirectory, @"Constraints\OverlapRemoval\Data", fileName);
            var testFileReader = this.LoadTestDataFile(pathAndFileSpec);
            Validate.IsTrue(CheckResult(testFileReader.VariableDefs,
                            testFileReader.ClusterDefs,
                            testFileReader.ConstraintDefsX,
                            testFileReader.ConstraintsDefY,
                            true /* fCheckResults */), FailureString);
        }

        // ReSharper disable InconsistentNaming

        [TestMethod]
        [Timeout(10 * 2000)]
        [WorkItem(568064)]
        [Description("Test of clusters.")]
        public void Clusters_Vars1000_ConstraintsMax10_ClustersDefault_MinSize100X120Y()
        {
            RunTestDataFile("Clusters_Vars1000_ConstraintsMax10_ClustersDefault_MinSize100X120Y.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [WorkItem(568064)]
        [Description("Test of clusters.")]
        public void Clusters_Vars100_ConstraintsMax10_ClustersDefault_MinSize100X120Y()
        {
            RunTestDataFile("Clusters_Vars100_ConstraintsMax10_ClustersDefault_MinSize100X120Y.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Clusters_Vars100_ConstraintsMax10_WeightMax100_ClustersRandom50_Margins30()
        {
            RunTestDataFile("Clusters_Vars100_ConstraintsMax10_WeightMax100_ClustersRandom50_Margins30.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [WorkItem(568064)]
        [Description("Test of clusters.")]
        public void Clusters_Vars100_ConstraintsMax3_ClustersDefault_MinSize100X120Y()
        {
            RunTestDataFile("Clusters_Vars100_ConstraintsMax3_ClustersDefault_MinSize100X120Y.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Clusters_Vars100_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_Clusters20()
        {
            RunTestDataFile("Clusters_Vars100_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_Clusters20.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Clusters_Vars100_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_ClustersDefault_FixedBordersRandom()
        {
            RunTestDataFile("Clusters_Vars100_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_ClustersDefault_FixedBordersRandom.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Clusters_Vars200_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_Clusters40()
        {
            RunTestDataFile("Clusters_Vars200_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_Clusters40.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Clusters_Vars200_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_ClustersDefault_FixedBordersRandom()
        {
            RunTestDataFile("Clusters_Vars200_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_ClustersDefault_FixedBordersRandom.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [WorkItem(568064)]
        [Description("Test of clusters.")]
        public void Clusters_Vars2500_ConstraintsMax10_ClustersDefault_MinSize100X120Y()
        {
            RunTestDataFile("Clusters_Vars2500_ConstraintsMax10_ClustersDefault_MinSize100X120Y.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Clusters_Vars300_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_Clusters60()
        {
            RunTestDataFile("Clusters_Vars300_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_Clusters60.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Clusters_Vars300_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_ClustersDefault_FixedBordersRandom()
        {
            RunTestDataFile("Clusters_Vars300_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_ClustersDefault_FixedBordersRandom.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Clusters_Vars400_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_Clusters80()
        {
            RunTestDataFile("Clusters_Vars400_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_Clusters80.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Clusters_Vars400_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_ClustersDefault_FixedBordersRandom()
        {
            RunTestDataFile("Clusters_Vars400_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_ClustersDefault_FixedBordersRandom.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [WorkItem(568064)]
        [Description("Test of clusters.")]
        public void Clusters_Vars500_ConstraintsMax10_ClustersDefault_MinSize100X120Y()
        {
            RunTestDataFile("Clusters_Vars500_ConstraintsMax10_ClustersDefault_MinSize100X120Y.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [WorkItem(568064)]
        [Description("Test of clusters.")]
        public void Clusters_Vars500_ConstraintsMax3_ClustersDefault_MinSize100X120Y()
        {
            RunTestDataFile("Clusters_Vars500_ConstraintsMax3_ClustersDefault_MinSize100X120Y.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Clusters_Vars500_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_Clusters100()
        {
            RunTestDataFile("Clusters_Vars500_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_Clusters100.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Clusters_Vars500_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_ClustersDefault_FixedBordersRandom()
        {
            RunTestDataFile("Clusters_Vars500_ConstraintsMax3_PosMax1M_WeightMax10K_Margins100_ClustersDefault_FixedBordersRandom.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [WorkItem(568064)]
        [Description("Test of clusters with a single root.")]
        public void Clusters_Vars50_ConstraintsMax10_ClustersDefaultSingleRoot_MinSize100X120Y()
        {
            RunTestDataFile("Clusters_Vars50_ConstraintsMax10_ClustersDefaultSingleRoot_MinSize100X120Y.txt");
        }

        [TestMethod]
        [Timeout(2000)]
        [WorkItem(568064)]
        [Description("Test of clusters with a single root.")]
        public void Clusters_Vars100_ConstraintsMax10_ClustersDefaultSingleRoot_MinSize100X120Y()
        {
            RunTestDataFile("Clusters_Vars100_ConstraintsMax10_ClustersDefaultSingleRoot_MinSize100X120Y.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap0_Vars100_ConstraintsMax10()
        {
            RunTestDataFile("Overlap0_Vars100_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap0_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Overlap0_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars100_ConstraintsMax10()
        {
            RunTestDataFile("Overlap1_Vars100_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Overlap1_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars200_ConstraintsMax10()
        {
            RunTestDataFile("Overlap1_Vars200_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars200_ConstraintsMax3()
        {
            RunTestDataFile("Overlap1_Vars200_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars300_ConstraintsMax10()
        {
            RunTestDataFile("Overlap1_Vars300_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars300_ConstraintsMax3()
        {
            RunTestDataFile("Overlap1_Vars300_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars400_ConstraintsMax10()
        {
            RunTestDataFile("Overlap1_Vars400_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars400_ConstraintsMax3()
        {
            RunTestDataFile("Overlap1_Vars400_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars500_ConstraintsMax10()
        {
            RunTestDataFile("Overlap1_Vars500_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars500_ConstraintsMax3()
        {
            RunTestDataFile("Overlap1_Vars500_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars600_ConstraintsMax10()
        {
            RunTestDataFile("Overlap1_Vars600_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars600_ConstraintsMax3()
        {
            RunTestDataFile("Overlap1_Vars600_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars700_ConstraintsMax10()
        {
            RunTestDataFile("Overlap1_Vars700_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars700_ConstraintsMax3()
        {
            RunTestDataFile("Overlap1_Vars700_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars800_ConstraintsMax10()
        {
            RunTestDataFile("Overlap1_Vars800_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars800_ConstraintsMax3()
        {
            RunTestDataFile("Overlap1_Vars800_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars900_ConstraintsMax10()
        {
            RunTestDataFile("Overlap1_Vars900_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars900_ConstraintsMax3()
        {
            RunTestDataFile("Overlap1_Vars900_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap2_Vars100_ConstraintsMax10()
        {
            RunTestDataFile("Overlap2_Vars100_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap2_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Overlap2_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap3_Vars100_ConstraintsMax10()
        {
            RunTestDataFile("Overlap3_Vars100_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap3_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Overlap3_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap4_Vars100_ConstraintsMax10()
        {
            RunTestDataFile("Overlap4_Vars100_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap4_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Overlap4_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap5_Vars100_ConstraintsMax10()
        {
            RunTestDataFile("Overlap5_Vars100_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap5_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Overlap5_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap6_Vars100_ConstraintsMax10()
        {
            RunTestDataFile("Overlap6_Vars100_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap6_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Overlap6_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap7_Vars100_ConstraintsMax10()
        {
            RunTestDataFile("Overlap7_Vars100_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap7_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Overlap7_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap8_Vars100_ConstraintsMax10()
        {
            RunTestDataFile("Overlap8_Vars100_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap8_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Overlap8_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap9_Vars100_ConstraintsMax10()
        {
            RunTestDataFile("Overlap9_Vars100_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap9_Vars100_ConstraintsMax3()
        {
            RunTestDataFile("Overlap9_Vars100_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap_Vars10()
        {
            RunTestDataFile("Overlap_Vars10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap_Vars1000_ConstraintsMax10()
        {
            RunTestDataFile("Overlap_Vars1000_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap_Vars1000_ConstraintsMax3()
        {
            RunTestDataFile("Overlap_Vars1000_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap_Vars2500_ConstraintsMax10()
        {
            RunTestDataFile("Overlap_Vars2500_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap_Vars2500_ConstraintsMax3()
        {
            RunTestDataFile("Overlap_Vars2500_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap_Vars5000_ConstraintsMax10()
        {
            RunTestDataFile("Overlap_Vars5000_ConstraintsMax10.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap_Vars5000_ConstraintsMax3()
        {
            RunTestDataFile("Overlap_Vars5000_ConstraintsMax3.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars1000_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars1000_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars100_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars100_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars100_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars100_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars200_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars200_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars200_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars200_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars300_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars300_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars300_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars300_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars400_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars400_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars400_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars400_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars500_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars500_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars500_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars500_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars600_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars600_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars600_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars600_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars700_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars700_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars700_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars700_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars800_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars800_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars800_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars800_ConstraintsMax3_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars900_ConstraintsMax10_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars900_ConstraintsMax10_WeightMax1K.txt");
        }

        [TestMethod]
        [Timeout(10 * 2000)]
        [Description("Test of clusters.")]
        public void Overlap1_Vars900_ConstraintsMax3_WeightMax1K()
        {
            RunTestDataFile("Overlap1_Vars900_ConstraintsMax3_WeightMax1K.txt");
        }
    }
}
