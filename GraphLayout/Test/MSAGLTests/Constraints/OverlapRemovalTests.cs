// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlapRemovalTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.Msagl.Core.Geometry;
using OlapCluster = Microsoft.Msagl.Core.Geometry.OverlapRemovalCluster;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    using System.Diagnostics.CodeAnalysis;

    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OverlapRemovalTests : OverlapRemovalVerifier
    {
        [ClassInitialize]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "testContext")]
        public static void ClassInitialize(TestContext testContext)
        {
            ClusterDef.TestContext = testContext;
        }

        ////
        // Test_*() test small specific node layouts; larger-scale layouts are created
        // via CreateTestFile.  Note that the Constraints are sorted on left then right
        // variables, and are only used for comparisons in regression analysis, not for
        // populating the solvers.
        ////

        // ReSharper disable InconsistentNaming

        [TestMethod]
        [Timeout(2000)]
        [Description("All vertical movement with no padding")]
        public void Test_AllVertical_Pad0()
        {
            var expectedPositionsX = new[] { 2.0, 3.0, 4.0, 5.0, 6.0 };
            var expectedPositionsY = new[] { 0.0, 2.0, 4.0, 6.0, 8.0 };
            var variableDefs = new[]
                {
                    //                posXY     sizeXY
                    new VariableDef(2.0, 2.0, 3.0, 2.0),
                    new VariableDef(3.0, 3.0, 3.0, 2.0),
                    new VariableDef(4.0, 4.0, 3.0, 2.0),
                    new VariableDef(5.0, 5.0, 3.0, 2.0),
                    new VariableDef(6.0, 6.0, 3.0, 2.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[0], variableDefs[2], 2.0),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 2.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, null /* iterCstX */, constraintDefsY, expectedPositionsX, expectedPositionsY, 
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("All vertical movement with padding")]
        public void Test_AllVertical_Pad7()
        {
            var expectedPositionsX = new[] { 2.0, 3.0, 4.0, 5.0, 6.0 };
            var expectedPositionsY = new[] { -14.0, -5.0, 4.0, 13.0, 22.0 };
            var variableDefs = new[]
                {
                    //                posXY     sizeXY
                    new VariableDef(2.0, 2.0, 3.0, 2.0),
                    new VariableDef(3.0, 3.0, 3.0, 2.0),
                    new VariableDef(4.0, 4.0, 3.0, 2.0),
                    new VariableDef(5.0, 5.0, 3.0, 2.0),
                    new VariableDef(6.0, 6.0, 3.0, 2.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 9.0),
                    new ConstraintDef(variableDefs[0], variableDefs[2], 9.0),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 9.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 9.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 9.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 9.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 9.0)
                };
            MinPaddingX = 7.0;
            MinPaddingY = 7.0;
            Validate.IsTrue(
                    CheckResult(variableDefs, null /* iterCstX */, constraintDefsY, expectedPositionsX, expectedPositionsY, 
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("All horizontal movement with no padding")]
        public void Test_AllHorizontal_Pad0()
        {
            var expectedPositionsX = new[] { 0.0, 2.0, 4.0, 6.0, 8.0 };
            var expectedPositionsY = new[] { 2.0, 3.0, 4.0, 5.0, 6.0 };
            var variableDefs = new[]
                {
                    //                posXY     sizeXY
                    new VariableDef(2.0, 2.0, 2.0, 3.0),
                    new VariableDef(3.0, 3.0, 2.0, 3.0),
                    new VariableDef(4.0, 4.0, 2.0, 3.0),
                    new VariableDef(5.0, 5.0, 2.0, 3.0),
                    new VariableDef(6.0, 6.0, 2.0, 3.0)
                };

            // If we get any Y constraints, it would be due to adjacent nodes being
            // ordered by Open rather than Close (as currently) in the Event list.
            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[0], variableDefs[2], 2.0),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 2.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefsX, null /* iterCstY */, expectedPositionsX, expectedPositionsY, 
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("All horizontal movement with padding")]
        public void Test_AllHorizontal_Pad7()
        {
            var expectedPositionsX = new[] { -14.0, -5.0, 4.0, 13.0, 22.0 };
            var expectedPositionsY = new[] { 2.0, 3.0, 4.0, 5.0, 6.0 };
            var variableDefs = new[]
                {
                    //                posXY     sizeXY
                    new VariableDef(2.0, 2.0, 2.0, 3.0),
                    new VariableDef(3.0, 3.0, 2.0, 3.0),
                    new VariableDef(4.0, 4.0, 2.0, 3.0),
                    new VariableDef(5.0, 5.0, 2.0, 3.0),
                    new VariableDef(6.0, 6.0, 2.0, 3.0)
                };
            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 9.0),
                    new ConstraintDef(variableDefs[0], variableDefs[2], 9.0),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 9.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 9.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 9.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 9.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 9.0)
                };
            MinPaddingX = 7.0;
            MinPaddingY = 7.0;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefsX, null /* iterCstY */, expectedPositionsX, expectedPositionsY, 
                            /*checkResults:*/ true),
                    FailureString);
            }

        [TestMethod]
        [Timeout(2000)]
        [Description("Horizontal and vertical movement with no padding")]
        public void Test_Mixed_Pad0()
        {
            var expectedPositionsX = new[] { 1.5, 2.5, 4.5, 5.5, 6.0 };
            var expectedPositionsY = new[] { 4.5, 6.5, 3.0, 5.0, 7.0 };
            var variableDefs = new[]
                {
                    //                posXY     sizeXY
                    new VariableDef(2.0, 5.0, 3.0, 2.0),
                    new VariableDef(3.0, 6.0, 3.0, 2.0),
                    new VariableDef(4.0, 4.0, 3.0, 2.0), 
                    new VariableDef(5.0, 5.0, 3.0, 2.0),
                    new VariableDef(6.0, 6.0, 3.0, 2.0)
                };

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Horizontal and vertical movement with padding")]
        public void Test_Mixed_Pad7()
        {
            var expectedPositionsX = new[] { -2.0, -2.0, 8.0, 8.0, 8.0 };
            var expectedPositionsY = new[] { 1.0, 10.0, -4.0, 5.0, 14.0 };
            var variableDefs = new[]
                {
                    //                posXY     sizeXY
                    new VariableDef(2.0, 5.0, 3.0, 2.0),
                    new VariableDef(3.0, 6.0, 3.0, 2.0),
                    new VariableDef(4.0, 4.0, 3.0, 2.0),
                    new VariableDef(5.0, 5.0, 3.0, 2.0),
                    new VariableDef(6.0, 6.0, 3.0, 2.0)
                };

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 10.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 10.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 10.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 10.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 10.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 9.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 9.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 9.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 9.0)
                };
            MinPaddingX = 7.0;
            MinPaddingY = 7.0;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify that variables do not move if there are no overlaps; variables are already in the expected minimally-non-overlapping positions")]
        public void Test_No_Movement_If_No_Overlaps()
        {
            var expectedPositionsX = new[] { 2.0, 3.0, 4.0, 5.0, 6.0 };
            var expectedPositionsY = new[] { 0.0, 2.0, 4.0, 6.0, 8.0 };
            var variableDefs = new[]
                {
                    //              posXY     sizeXY
                    new VariableDef(2.0, 0.0, 3.0, 2.0),
                    new VariableDef(3.0, 2.0, 3.0, 2.0),
                    new VariableDef(4.0, 4.0, 3.0, 2.0),
                    new VariableDef(5.0, 6.0, 3.0, 2.0),
                    new VariableDef(6.0, 8.0, 3.0, 2.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[0], variableDefs[2], 2.0),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 2.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, null /* iterCstX */, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of random positions")]
        public void Test_Rand4()
        {
            // This tests comes from an early failure in CreateRandomTestFile output
            // (ProjSolver was using Size in MergeBlocks).
            var expectedPositionsX = new[] { 6.30718, 8.61872, 6.38248, 2.71563 };
            var expectedPositionsY = new[] { 1.55372, 6.00620, 4.28180, 4.86094 };
            var variableDefs = new[]
                {
                    //                posXY                 sizeXY
                    new VariableDef(6.30718, 2.47695, 3.96573, 1.17767),
                    new VariableDef(8.14909, 6.00620, 0.22357, 2.42140),
                    new VariableDef(6.57753, 3.35857, 4.24890, 4.27848),
                    new VariableDef(2.99020, 4.86094, 3.08481, 3.38951)
                };

            Validate.IsTrue(
                    CheckResult(variableDefs, null /* constraintDefsX */, null /* constraintDefsY */, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test that forms a comparison base for deferring constraints to the vertical pass; this test does not defer")]
        public void Test_DeferToVertical_WithoutDefer()
        {
            var expectedPositionsX = new[] { 2.5, 5.5 };
            var expectedPositionsY = new[] { 4.0, 5.0 };
            AllowDeferToVertical = false;
            DeferToVertical_Worker(expectedPositionsX, expectedPositionsY);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test with results that are compared with constraints not being deferred to the vertical pass; this test defers")]
        public void Test_DeferToVertical_WithDefer()
        {
            var expectedPositionsX = new[] { 4.0, 4.0 };
            var expectedPositionsY = new[] { 3.0, 6.0 };
            DeferToVertical_Worker(expectedPositionsX, expectedPositionsY);
        }

        private void DeferToVertical_Worker(double[] expectedPositionsX, double[] expectedPositionsY)
        {
            var variableDefs = new[]
                {
                    //              posXY     sizeXY
                    new VariableDef(4.0, 4.0, 3.0, 3.0),
                    new VariableDef(4.0, 5.0, 3.0, 3.0)
                };

            var constraintDefsY = new[] { new ConstraintDef(variableDefs[0], variableDefs[1], 2.0) };
            Validate.IsTrue(
                    CheckResult(variableDefs, null /* iterCstX */, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test a case where deferring constraints to the vertical pass revealed a missing horizontal constraint")]
        public void Test_DeferToVertical_Missing_Horizontal_Constraint()
        {
            // This is a problem with evaluating neighbours based upon midpoints when we have a
            // defer-to-vertical operation that breaks the transitional chain.  For example:
            //  .         +-----+
            //            |  B  |
            //       +----|-----|----------+
            //  +----|-+  |     |     C    |
            //  | A  | |  +-----+          |
            //  |    +-|-------------------+
            //  +------+
            // A detects a non-overlapping neighbour B, whose midpoint is before C.  Normally
            // we'd assume that B had a constraint on C so transitionally A would have a constraint
            // on C.  However, since the horizontal movement to resolve the B/C overlap is greater
            // than the vertical movement that can also resolve it, B defers the constraint.  Because
            // the neighbour search stops at the first non-overlapping neighbour, A stops at B and
            // thus there is no A->C constraint generated on the horizontal pass.  This overlap is
            // removed on the vertical pass but causes a larger-than-needed movement.
            // Failure positions:
            //var expectedPositionsX = new[] { 4.0,     13.0,     17.0 };
            //var expectedPositionsY = new[] { 7.66667, -0.83333, 3.66667 };

            // Success positions:
            var expectedPositionsX = new[] { 2.5, 13.0, 18.5 };
            var expectedPositionsY = new[] { 4.5, 0.75, 5.25 };

            var variableDefs = new[]
                {
                    //                posXY     sizeXY
                    new VariableDef(4.0, 4.5, 8.0, 4.0),    // L/R   0/8
                    new VariableDef(13.0, 2.0, 7.0, 5.0),   // L/R 9.5/16.5
                    new VariableDef(17.0, 4.0, 24.0, 4.0)   // L/R   5/29
                };

            var constraintDefsY = new[] { new ConstraintDef(variableDefs[0], variableDefs[1], 2.0) };
            Validate.IsTrue(
                    CheckResult(variableDefs, null /* iterCstX */, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("For comparisons, demonstrates a case where node A is pushed horizontally by node B, which is then pushed vertically by Node C so that the horizontal movement was unnecessary, thus appearing to leave unnecessary space")]
        public void Test_Extra_Space_Due_To_Two_Passes() 
        {
            var expectedPositionsX = new[] { 3.5, 15.5, 12.0 };
            var expectedPositionsY = new[] { 4.0, -1.0, 4.0 };

            var variableDefs = new[]
                {
                    //                posXY     sizeXY
                    new VariableDef(4.0, 4.0, 4.0, 4.0),
                    new VariableDef(15.0, 1.0, 20.0, 4.0),
                    new VariableDef(12.0, 2.0, 6.0, 6.0)
                };

            var constraintDefsY = new[] { new ConstraintDef(variableDefs[0], variableDefs[1], 2.0) };
            Validate.IsTrue(
                    CheckResult(variableDefs, null /* iterCstX */, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test a single cluster with no padding")]
        public void Test_Cluster_Single1_Pad0()
        {
            var expectedPositionsX = new[] { 3.00000, 1.50000, 3.00000, 4.50000, 1.50000, 3.00000, 4.50000, 3.00000 };
            var expectedPositionsY = new[] { -5.00100, 1.00000, -2.00100, 1.00000, 4.00000, 7.00100, 4.00000, 10.00100 };

            var variableDefs = new[]
                {
                    //ordinal                  posXY     sizeXY      There is only one cluster, with 'b' vars; 'a' is root
                    // Note: Arrows indicate which nodes move but not nessarily (in fact probably not) where they end up.
                    /* 0  */ new VariableDef(3.0, 1.0, 3.0, 3.0),   //..a..

                    // 123
                    /* 1  */ new VariableDef(2.0, 2.0, 3.0, 3.0),   //.bab.     (b=variableDefs[1],a=[2],b=[3])
                    /* 2  */ new VariableDef(3.0, 2.0, 3.0, 3.0),
                    //  |---^   (move this var out of the cluster)
                    /* 3  */ new VariableDef(4.0, 2.0, 3.0, 3.0),

                    // 456
                    /* 4  */ new VariableDef(2.0, 3.0, 3.0, 3.0),   //.bab.     (b=variableDefs[4],a=[5],b=[6])
                    /* 5  */ new VariableDef(3.0, 3.0, 3.0, 3.0),
                    //  |---^   (move this var out of the cluster)
                    /* 6  */ new VariableDef(4.0, 3.0, 3.0, 3.0),

                    /* 7  */ new VariableDef(3.0, 4.0, 3.0, 3.0),   //..a..
                };

            var clusterDefs = new[] { new ClusterDef() };

            // 'a' vars above are not added to a cluster - they are at the root.
            clusterDefs[0].AddVariableDef(variableDefs[1]); // exclude [2] from cluster
            clusterDefs[0].AddVariableDef(variableDefs[3]);
            clusterDefs[0].AddVariableDef(variableDefs[4]); // exclude [5] from cluster
            clusterDefs[0].AddVariableDef(variableDefs[6]);
            clusterDefs[0].SetResultPositions(-0.00100, 6.00100, -0.50100, 5.50100);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test a single cluster with no padding and specific minimal sizes")]
        public void Test_Cluster_Single1_Pad0_MinSize_15_10()
        {
            var expectedPositionsX = new[] { 3.00000, 1.50000, 3.00000, 4.50000, 1.50000, 3.00000, 4.50000, 3.00000, 8.00000, 12.00000 };
            var expectedPositionsY = new[] { -7.20060, 1.00000, -4.20060, 1.00000, 4.00000, 8.80040, 4.00000, 11.80040, 8.80040, 6.00000 };

            var variableDefs = new[]
                {
                    //ordinal                  posXY     sizeXY      There is only one cluster, with 'b' vars; 'a' is root
                    // Note: Arrows indicate which nodes move but not nessarily (in fact probably not) where they end up.
                    /* 0  */ new VariableDef(3.0, 1.0, 3.0, 3.0),   //..a..

                    // 123
                    /* 1  */ new VariableDef(2.0, 2.0, 3.0, 3.0),   //.bab.     (b=variableDefs[1],a=[2],b=[3])
                    /* 2  */ new VariableDef(3.0, 2.0, 3.0, 3.0),
                    //  |---^   (move this var out of the cluster)
                    /* 3  */ new VariableDef(4.0, 2.0, 3.0, 3.0),

                    // 456
                    /* 4  */ new VariableDef(2.0, 3.0, 3.0, 3.0),  //.bab.     (b=variableDefs[4],a=[5],b=[6])
                    /* 5  */ new VariableDef(3.0, 3.0, 3.0, 3.0),
                    //  |---^   (move this var out of the cluster)
                    /* 6  */ new VariableDef(4.0, 3.0, 3.0, 3.0),

                    /* 7  */ new VariableDef(3.0, 4.0, 3.0, 3.0),  //..a..

                    /* 8  */ new VariableDef(8.0, 8.0, 3.0, 3.0),  //..x.. moves down
                    /* 9  */ new VariableDef(12.0, 6.0, 3.0, 3.0)  //..y.. moves right
                };

            var clusterDefs = new[] { new ClusterDef(15, 10) };

            // 'a' vars above are not added to a cluster - they are at the root.
            clusterDefs[0].AddVariableDef(variableDefs[1]); // exclude [2] from cluster
            clusterDefs[0].AddVariableDef(variableDefs[3]);
            clusterDefs[0].AddVariableDef(variableDefs[4]); // exclude [5] from cluster
            clusterDefs[0].AddVariableDef(variableDefs[6]);
            // 8 and 9 are not in cluster
            clusterDefs[0].SetResultPositions(-4.50100, 10.50000, -2.70060, 7.30040);

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, null /*constraintDefsX*/, null /*constraintDefsY*/, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test a single cluster with no padding")]
        public void Test_Cluster_Single2_Pad0()
        {
            var expectedPositionsX = new[] { 0.66667, 3.66667, 1.50000, 3.66667, 4.50000, 1.50000, 2.33333, 4.50000, 2.33333, 5.33333 };
            var expectedPositionsY = new[] { -2.00100, -5.00100, 1.00000, -2.00100, 1.00000, 4.00000, 7.00100, 4.00000, 10.00100, 7.00100 };

            var variableDefs = new[]
                {
                    //ordinal                  posXY     sizeXY      There is only one cluster, with 'b' vars; 'a' is root
                    /* 0  */ new VariableDef(2.0, 1.0, 3.0, 3.0),   //.aa..
                    /* 1  */ new VariableDef(3.0, 1.0, 3.0, 3.0),   //.aa..

                    // 234
                    /* 2  */ new VariableDef(2.0, 2.0, 3.0, 3.0),   //.bab.     (b=variableDefs[1],a=[2],b=[3])
                    /* 3  */ new VariableDef(3.0, 2.0, 3.0, 3.0),
                    //  |---^   (move this var out of the cluster)
                    /* 4  */ new VariableDef(4.0, 2.0, 3.0, 3.0),

                    // 567
                    /* 5  */ new VariableDef(2.0, 3.0, 3.0, 3.0),   //.bab.     (b=variableDefs[4],a=[5],b=[6])
                    /* 6  */ new VariableDef(3.0, 3.0, 3.0, 3.0),
                    //  |---^   (move this var out of the cluster)
                    /* 7  */ new VariableDef(4.0, 3.0, 3.0, 3.0),

                    /* 8  */ new VariableDef(3.0, 4.0, 3.0, 3.0),   //..aa.
                    /* 9  */ new VariableDef(4.0, 4.0, 3.0, 3.0)    //..aa.
                };

            var clusterDefs = new[] { new ClusterDef() };

            // TODOclust:  Update the "diagrams" to show the right result shape

            // 'a' vars above are not added to a cluster - they are at the root.
            clusterDefs[0].AddVariableDef(variableDefs[2]); // exclude [3] from cluster
            clusterDefs[0].AddVariableDef(variableDefs[4]);
            clusterDefs[0].AddVariableDef(variableDefs[5]); // exclude [6] from cluster
            clusterDefs[0].AddVariableDef(variableDefs[7]);
            clusterDefs[0].SetResultPositions(-0.00100, 6.00100, -0.50100, 5.50100);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test a single cluster in the southeast quadrant")]
        public void Test_Cluster_Quadrant_Southeast()
        {
            // Test cluster boundaries for a cluster contained entirely in the southeast quadrant
            // (the one where both X and Y are positive).
            var expectedPositionsX = new[] { 9.66600, 12.66700, 12.66700 };
            var expectedPositionsY = new[] { 13.00000, 11.00000, 14.00000 };

            var variableDefs = new[]
                {
                    //ordinal                  posXY             sizeXY
                    /* 0  */ new VariableDef(11.0, 13.0, 3.0, 3.0),
                    /* 1  */ new VariableDef(12.0, 12.0, 3.0, 3.0),
                    /* 2  */ new VariableDef(12.0, 13.0, 3.0, 3.0),
                };

            var clusterDefs = new[] { new ClusterDef() };

            clusterDefs[0].AddVariableDef(variableDefs[1]); // exclude [0] from cluster
            clusterDefs[0].AddVariableDef(variableDefs[2]);
            clusterDefs[0].SetResultPositions(11.16600, 14.16800, 9.49900, 15.50100);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 2.0)
                };

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test a single cluster in the northwest quadrant")]
        public void Test_Cluster_Quadrant_Northwest()
        {
            // Test cluster boundaries for a cluster contained entirely in the northwest quadrant
            // (the one where both X and Y are negative).
            var expectedPositionsX = new[] { -13.00000, -11.00000, -14.00000 };
            var expectedPositionsY = new[] { -9.66600, -12.66700, -12.66700 };

            var variableDefs = new[]
                {
                    //ordinal                  posXY             sizeXY
                    /* 0  */ new VariableDef(-13.0, -11.0, 3.0, 3.0),
                    /* 1  */ new VariableDef(-12.0, -12.0, 3.0, 3.0),
                    /* 2  */ new VariableDef(-13.0, -12.0, 3.0, 3.0)
                };

            var clusterDefs = new[] { new ClusterDef() };

            clusterDefs[0].AddVariableDef(variableDefs[1]); // exclude [0] from cluster
            clusterDefs[0].AddVariableDef(variableDefs[2]);
            clusterDefs[0].SetResultPositions(-15.50100, -9.49900, -14.16800, -11.16600);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 2.0)
                };

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test nested clusters with no padding")]
        public void Test_Cluster_Nest_Pad0()
        {
            var expectedPositionsX = new[]
                {
                    6.50000, 2.00000, 0.50000, 3.50000, 5.00000, 6.50000, 9.50000, 8.00000, 2.00000, 0.50000, 3.50000,
                    5.00000, 6.50000, 9.50000, 8.00000, 3.50000
                };

            var expectedPositionsY = new[]
                {
                    -5.00200, 1.00000, -2.00100, -5.00200, 1.00000, -2.00100, -5.00200, 1.00000, 4.00000, 10.00200,
                    7.00100, 4.00000, 10.00200, 7.00100, 4.00000, 10.00200
                };

            // TODOunit: Removing the ".0" from position results in the overloaded
            // VariableDef ctor for (uint ordinal, ...) being called, thus sending
            // double.MaxValue parameter values to the generator.  Add a unit test
            // that does this and verifies the throw (same for ProjSolv.Variable).
            var variableDefs = new[]
                {
                    // ordinal                 posXY     sizeXY      There are two nested clusters; 'a' is root, 'b' is first level, 'c' is in 'b'
                    /* 0  */ new VariableDef(5.0, 1.0, 3.0, 3.0),   //   ....a....

                    //    1234567
                    /* 1  */ new VariableDef(2.0, 2.0, 3.0, 3.0),
                    //   .cbacbac.      (c=variableDefs[1],b=[2],a=[9], ...)
                    /* 2  */ new VariableDef(3.0, 2.0, 3.0, 3.0),
                    // ^----|--|        (move 'a' vars outside all clusters)
                    /* 3  */ new VariableDef(4.0, 2.0, 3.0, 3.0),
                    //     |--|---^     (move 'b' vars outside of all 'c' vars)
                    /* 4  */ new VariableDef(5.0, 2.0, 3.0, 3.0),
                    //    |->|<-|       (move 'c' vars together into cluster nested in 'b')
                    /* 5  */ new VariableDef(6.0, 2.0, 3.0, 3.0),
                    /* 6  */ new VariableDef(7.0, 2.0, 3.0, 3.0),
                    /* 7  */ new VariableDef(8.0, 2.0, 3.0, 3.0),

                    //    89ABCDE       (hex ordinals)
                    /* 8  */ new VariableDef(2.0, 3.0, 3.0, 3.0),
                    //   .cabcabc.      (c=variableDefs[7],b=[8],a=[9], ...)
                    /* 9  */ new VariableDef(3.0, 3.0, 3.0, 3.0),
                    // ^---|--|         (move 'a' vars outside all clusters)
                    /* 10 */ new VariableDef(4.0, 3.0, 3.0, 3.0),
                    //      |--|---^    (move 'b' vars outside of all 'c' vars)
                    /* 11 */ new VariableDef(5.0, 3.0, 3.0, 3.0),
                    //    |->|<-|       (move 'c' vars together into cluster nested in 'b')
                    /* 12 */ new VariableDef(6.0, 3.0, 3.0, 3.0),
                    /* 13 */ new VariableDef(7.0, 3.0, 3.0, 3.0),
                    /* 14 */ new VariableDef(8.0, 3.0, 3.0, 3.0),

                    /* 15 */ new VariableDef(5.0, 4.0, 3.0, 3.0)    //   ....a....
                };

            var clusterDefs = new[] { new ClusterDef(), new ClusterDef() };

            // 'a' vars above are not added to a cluster - they are at the root.
            clusterDefs[0].AddVariableDef(variableDefs[0x2]);
            clusterDefs[0].AddVariableDef(variableDefs[0x5]);
            clusterDefs[0].AddVariableDef(variableDefs[0xA]);
            clusterDefs[0].AddVariableDef(variableDefs[0xD]);
            clusterDefs[0].SetResultPositions(-1.00100, 11.00100, -3.50200, 8.50200);
            clusterDefs[0].AddClusterDef(clusterDefs[1]);

            clusterDefs[1].AddVariableDef(variableDefs[0x1]);
            clusterDefs[1].AddVariableDef(variableDefs[0x4]);
            clusterDefs[1].AddVariableDef(variableDefs[0x7]);
            clusterDefs[1].AddVariableDef(variableDefs[0x8]);
            clusterDefs[1].AddVariableDef(variableDefs[0xB]);
            clusterDefs[1].AddVariableDef(variableDefs[0xE]);
            clusterDefs[1].SetResultPositions(0.49904, 9.50096, -0.50100, 5.50100);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test nested clusters with no padding and with minsizes")]
        public void Test_Cluster_Nest_Pad0_MinSize_12_8()
        {
            var expectedPositionsX = new[]
                {
                    6.50000,
                    2.00000,
                    0.50000,
                    3.50000,
                    5.00000,
                    6.50000,
                    9.50000,
                    8.00000,
                    2.00000,
                    0.50000,
                    3.50000,
                    5.00000,
                    6.50000,
                    9.50000,
                    8.00000,
                    3.50000
                };

            var expectedPositionsY = new[]
                {
                    -6.00100,
                    1.00000,
                    -3.00000,
                    -6.00100,
                    1.00000,
                    -3.00000,
                    -6.00100,
                    1.00000,
                    4.00000,
                    11.00100,
                    8.00000,
                    4.00000,
                    11.00100,
                    8.00000,
                    4.00000,
                    11.00100
        };

            // TODOunit: Removing the ".0" from position results in the overloaded
            // VariableDef ctor for (uint ordinal, ...) being called, thus sending
            // double.MaxValue parameter values to the generator.  Add a unit test
            // that does this and verifies the throw (same for ProjSolv.Variable).
            var variableDefs = new[]
                {
                    // ordinal                 posXY     sizeXY      There are two nested clusters; 'a' is root, 'b' is first level, 'c' is in 'b'
                    /* 0  */ new VariableDef(5.0, 1.0, 3.0, 3.0),   //   ....a....

                    //    1234567
                    /* 1  */ new VariableDef(2.0, 2.0, 3.0, 3.0),
                    //   .cbacbac.      (c=variableDefs[1],b=[2],a=[9], ...)
                    /* 2  */ new VariableDef(3.0, 2.0, 3.0, 3.0),
                    // ^----|--|        (move 'a' vars outside all clusters)
                    /* 3  */ new VariableDef(4.0, 2.0, 3.0, 3.0),
                    //     |--|---^     (move 'b' vars outside of all 'c' vars)
                    /* 4  */ new VariableDef(5.0, 2.0, 3.0, 3.0),
                    //    |->|<-|       (move 'c' vars together into cluster nested in 'b')
                    /* 5  */ new VariableDef(6.0, 2.0, 3.0, 3.0),
                    /* 6  */ new VariableDef(7.0, 2.0, 3.0, 3.0),
                    /* 7  */ new VariableDef(8.0, 2.0, 3.0, 3.0),

                    //    89ABCDE       (hex ordinals)
                    /* 8  */ new VariableDef(2.0, 3.0, 3.0, 3.0),
                    //   .cabcabc.      (c=variableDefs[7],b=[8],a=[9], ...)
                    /* 9  */ new VariableDef(3.0, 3.0, 3.0, 3.0),
                    // ^---|--|         (move 'a' vars outside all clusters)
                    /* 10 */ new VariableDef(4.0, 3.0, 3.0, 3.0),
                    //      |--|---^    (move 'b' vars outside of all 'c' vars)
                    /* 11 */ new VariableDef(5.0, 3.0, 3.0, 3.0),
                    //    |->|<-|       (move 'c' vars together into cluster nested in 'b')
                    /* 12 */ new VariableDef(6.0, 3.0, 3.0, 3.0),
                    /* 13 */ new VariableDef(7.0, 3.0, 3.0, 3.0),
                    /* 14 */ new VariableDef(8.0, 3.0, 3.0, 3.0),

                    /* 15 */ new VariableDef(5.0, 4.0, 3.0, 3.0)    //   ....a....
                };

            var clusterDefs = new[] { new ClusterDef(12, 8), new ClusterDef(12, 8) };

            // 'a' vars above are not added to a cluster - they are at the root.
            clusterDefs[0].AddVariableDef(variableDefs[0x2]);
            clusterDefs[0].AddVariableDef(variableDefs[0x5]);
            clusterDefs[0].AddVariableDef(variableDefs[0xA]);
            clusterDefs[0].AddVariableDef(variableDefs[0xD]);
            clusterDefs[0].SetResultPositions(-1.00100, 11.00100, -4.50100, 9.50100);
            clusterDefs[0].AddClusterDef(clusterDefs[1]);

            clusterDefs[1].AddVariableDef(variableDefs[0x1]);
            clusterDefs[1].AddVariableDef(variableDefs[0x4]);
            clusterDefs[1].AddVariableDef(variableDefs[0x7]);
            clusterDefs[1].AddVariableDef(variableDefs[0x8]);
            clusterDefs[1].AddVariableDef(variableDefs[0xB]);
            clusterDefs[1].AddVariableDef(variableDefs[0xE]);
            clusterDefs[1].SetResultPositions(-1.00000, 11.00000, -1.50000, 6.50000);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [WorkItem(568064)]
        [Description("Test sibling clusters with no padding")]
        public void Test_Cluster_Sibling_Pad0()
        {
            var expectedPositionsX = new[]
                {
                    6.00000,
                    2.00001,
                    0.50000,
                    3.00000,
                    5.00000,
                    6.50000,
                    12.50100,
                    7.99999,
                    2.00000,
                    -2.50100,
                    3.50000,
                    5.00000,
                    7.00000,
                    9.50000,
                    8.00000,
                    4.00000                
                };
            var expectedPositionsY = new[]
                {
                    -3.28786,
                    2.71514,
                    -0.28686,
                    -3.28786,
                    2.71514,
                    -0.28686,
                    2.00000,
                    2.71514,
                    5.71514,
                    3.00000,
                    -0.28686,
                    5.71514,
                    8.71614,
                    -0.28686,
                    5.71514,
                    8.71614
                };
            var variableDefs = new[]
                {
                    // ordinal                 posXY     sizeXY      There are two sibling clusters, 'b' and 'c'; 'a' is root
                    /* 0  */ new VariableDef(5.0, 1.0, 3.0, 3.0),   //   ....a....

                    //    1234567
                    /* 1  */ new VariableDef(2.0, 2.0, 3.0, 3.0),
                    //   .cbacbac.     (c=variableDefs[1],b=[2],a=[9], ...)
                    /* 2  */ new VariableDef(3.0, 2.0, 3.0, 3.0),
                    // ^----|--|       (move 'a' vars outside all clusters)
                    /* 3  */ new VariableDef(4.0, 2.0, 3.0, 3.0),
                    //     |--|---^    (move 'b' vars outside of all 'c' vars)
                    /* 4  */ new VariableDef(5.0, 2.0, 3.0, 3.0),
                    //  ^-|--|--|      (move 'c' vars together into cluster nested in 'b')
                    /* 5  */ new VariableDef(6.0, 2.0, 3.0, 3.0),
                    /* 6  */ new VariableDef(7.0, 2.0, 3.0, 3.0),
                    /* 7  */ new VariableDef(8.0, 2.0, 3.0, 3.0),

                    //    89ABCDE      (hex ordinals)
                    /* 8  */ new VariableDef(2.0, 3.0, 3.0, 3.0),
                    //   .cabcabc.     (c=variableDefs[7],b=[8],a=[9], ...)
                    /* 9  */ new VariableDef(3.0, 3.0, 3.0, 3.0),
                    // ^---|--|        (move 'a' vars outside all clusters)
                    /* 10 */ new VariableDef(4.0, 3.0, 3.0, 3.0),
                    //      |--|---^   (move 'b' vars outside of all 'c' vars)
                    /* 11 */ new VariableDef(5.0, 3.0, 3.0, 3.0),
                    //  ^-|--|--|      (move 'c' vars together into cluster nested in 'b')
                    /* 12 */ new VariableDef(6.0, 3.0, 3.0, 3.0),
                    /* 13 */ new VariableDef(7.0, 3.0, 3.0, 3.0),
                    /* 14 */ new VariableDef(8.0, 3.0, 3.0, 3.0),

                    /* 15 */ new VariableDef(5.0, 4.0, 3.0, 3.0)    //   ....a....
                };

            var clusterDefs = new[] { new ClusterDef(), new ClusterDef() };

            // 'a' vars above are not added to a cluster - they are at the root.
            clusterDefs[0].AddVariableDef(variableDefs[0x2]);
            clusterDefs[0].AddVariableDef(variableDefs[0x5]);
            clusterDefs[0].AddVariableDef(variableDefs[0xA]);
            clusterDefs[0].AddVariableDef(variableDefs[0xD]);
            clusterDefs[0].SetResultPositions(-1.00100, 11.00100, -1.78786, 1.21414);
            // This is the difference from Test_Cluster_Nest; make them siblings here instead
            //clusterDefs[0].AddClusterDef(clusterDefs[1]);

            clusterDefs[1].AddVariableDef(variableDefs[0x1]);
            clusterDefs[1].AddVariableDef(variableDefs[0x4]);
            clusterDefs[1].AddVariableDef(variableDefs[0x7]);
            clusterDefs[1].AddVariableDef(variableDefs[0x8]);
            clusterDefs[1].AddVariableDef(variableDefs[0xB]);
            clusterDefs[1].AddVariableDef(variableDefs[0xE]);
            clusterDefs[1].SetResultPositions(0.49901, 9.50099, 1.21414, 7.21614);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };

            // Note: The results look like there's unexpected space around node 9 (to far left),
            // but that's only because of the fact that Cluster1 shoves it left, before Cluster2 shoves Cluster1
            // down - thus Node 9 is moved further left than it needs to be.  See Test_Extra_Space_Due_To_Two_Passes
            // for a test specific to this.
            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [WorkItem(568064)]
        [Description("Test sibling clusters with no padding and with minsizes")]
        public void Test_Cluster_Sibling_Pad0_MinSize_12_8()
        {
            var expectedPositionsX = new[]
                {
                    6.50000,
                    2.00000,
                    0.50000,
                    3.50000,
                    5.00000,
                    6.50000,
                    9.50000,
                    8.00000,
                    2.00000,
                    0.50000,
                    3.50000,
                    5.00000,
                    6.50000,
                    9.50000,
                    8.00000,
                    3.50000
                };
            var expectedPositionsY = new[]
                {
                    -7.75012,
                    3.25088,
                    0.24888,
                    -7.75012,
                    3.25088,
                    0.24888,
                    -7.75012,
                    3.25088,
                    6.25088,
                    11.24988,
                    0.24888,
                    6.25088,
                    11.24988,
                    0.24888,
                    6.25088,
                    11.24988
                };
            var variableDefs = new[]
                {
                    // ordinal                 posXY     sizeXY      There are two sibling clusters, 'b' and 'c'; 'a' is root
                    /* 0  */ new VariableDef(5.0, 1.0, 3.0, 3.0),   //   ....a....

                    //    1234567
                    /* 1  */ new VariableDef(2.0, 2.0, 3.0, 3.0),
                    //   .cbacbac.     (c=variableDefs[1],b=[2],a=[9], ...)
                    /* 2  */ new VariableDef(3.0, 2.0, 3.0, 3.0),
                    // ^----|--|       (move 'a' vars outside all clusters)
                    /* 3  */ new VariableDef(4.0, 2.0, 3.0, 3.0),
                    //     |--|---^    (move 'b' vars outside of all 'c' vars)
                    /* 4  */ new VariableDef(5.0, 2.0, 3.0, 3.0),
                    //  ^-|--|--|      (move 'c' vars together into cluster nested in 'b')
                    /* 5  */ new VariableDef(6.0, 2.0, 3.0, 3.0),
                    /* 6  */ new VariableDef(7.0, 2.0, 3.0, 3.0),
                    /* 7  */ new VariableDef(8.0, 2.0, 3.0, 3.0),

                    //    89ABCDE      (hex ordinals)
                    /* 8  */ new VariableDef(2.0, 3.0, 3.0, 3.0),
                    //   .cabcabc.     (c=variableDefs[7],b=[8],a=[9], ...)
                    /* 9  */ new VariableDef(3.0, 3.0, 3.0, 3.0),
                    // ^---|--|        (move 'a' vars outside all clusters)
                    /* 10 */ new VariableDef(4.0, 3.0, 3.0, 3.0),
                    //      |--|---^   (move 'b' vars outside of all 'c' vars)
                    /* 11 */ new VariableDef(5.0, 3.0, 3.0, 3.0),
                    //  ^-|--|--|      (move 'c' vars together into cluster nested in 'b')
                    /* 12 */ new VariableDef(6.0, 3.0, 3.0, 3.0),
                    /* 13 */ new VariableDef(7.0, 3.0, 3.0, 3.0),
                    /* 14 */ new VariableDef(8.0, 3.0, 3.0, 3.0),

                    /* 15 */ new VariableDef(5.0, 4.0, 3.0, 3.0)    //   ....a....
                };

            var clusterDefs = new[] { new ClusterDef(12, 8), new ClusterDef(12, 8) };

            // 'a' vars above are not added to a cluster - they are at the root.
            clusterDefs[0].AddVariableDef(variableDefs[0x2]);
            clusterDefs[0].AddVariableDef(variableDefs[0x5]);
            clusterDefs[0].AddVariableDef(variableDefs[0xA]);
            clusterDefs[0].AddVariableDef(variableDefs[0xD]);
            clusterDefs[0].SetResultPositions(-1.00100, 11.00100, -6.25012, 1.74988);
            // This is the difference from Test_Cluster_Nest; make them siblings here instead
            //clusterDefs[0].AddClusterDef(clusterDefs[1]);

            clusterDefs[1].AddVariableDef(variableDefs[0x1]);
            clusterDefs[1].AddVariableDef(variableDefs[0x4]);
            clusterDefs[1].AddVariableDef(variableDefs[0x7]);
            clusterDefs[1].AddVariableDef(variableDefs[0x8]);
            clusterDefs[1].AddVariableDef(variableDefs[0xB]);
            clusterDefs[1].AddVariableDef(variableDefs[0xE]);
            clusterDefs[1].SetResultPositions(-1.00000, 11.00000, 1.74988, 9.74988);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };


            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test nested clusters with margins")]
        public void Test_Cluster_Nest_Margins()
        {
            var expectedPositionsX = new[]
                {
                    23.93469, -15.06731, -24.06831, 19.93469, -7.06731, 9.93369, 31.93469, 0.93269, -11.06731, 3.00000,
                    -20.06831, -3.06731, 27.93469, 13.93369, 4.93269, 23.93469
                };

            var expectedPositionsY = new[]
                {
                    0.00000, 1.14258, 2.00000, 2.00000, 1.14258, 2.00000, 2.00000, 1.14258, 1.14258, 11.14458, 3.00000,
                    1.14258, 3.00000, 3.00000, 1.14258, 5.00000
                };

            var variableDefs = new[]
                {
                    // ordinal                 posXY     sizeXY      There are two nested clusters; 'a' is root, 'b' is first level, 'c' is in 'b'
                    /* 0  */ new VariableDef(5.0, 1.0, 3.0, 3.0),   //   ....a....

                    //    1234567
                    /* 1  */ new VariableDef(2.0, 2.0, 3.0, 3.0),
                    //   .cbacbac.      (c=variableDefs[1],b=[2],a=[9], ...)
                    /* 2  */ new VariableDef(3.0, 2.0, 3.0, 3.0),
                    // ^----|--|        (move 'a' vars outside all clusters)
                    /* 3  */ new VariableDef(4.0, 2.0, 3.0, 3.0),
                    //     |--|---^     (move 'b' vars outside of all 'c' vars)
                    /* 4  */ new VariableDef(5.0, 2.0, 3.0, 3.0),
                    //    |->|<-|       (move 'c' vars together into cluster nested in 'b')
                    /* 5  */ new VariableDef(6.0, 2.0, 3.0, 3.0),
                    /* 6  */ new VariableDef(7.0, 2.0, 3.0, 3.0),
                    /* 7  */ new VariableDef(8.0, 2.0, 3.0, 3.0),

                    //    89ABCDE       (hex ordinals)
                    /* 8  */ new VariableDef(2.0, 3.0, 3.0, 3.0),
                    //   .cabcabc.      (c=variableDefs[7],b=[8],a=[9], ...)
                    /* 9  */ new VariableDef(3.0, 3.0, 3.0, 3.0),
                    // ^---|--|         (move 'a' vars outside all clusters)
                    /* 10 */ new VariableDef(4.0, 3.0, 3.0, 3.0),
                    //      |--|---^    (move 'b' vars outside of all 'c' vars)
                    /* 11 */ new VariableDef(5.0, 3.0, 3.0, 3.0),
                    //    |->|<-|       (move 'c' vars together into cluster nested in 'b')
                    /* 12 */ new VariableDef(6.0, 3.0, 3.0, 3.0),
                    /* 13 */ new VariableDef(7.0, 3.0, 3.0, 3.0),
                    /* 14 */ new VariableDef(8.0, 3.0, 3.0, 3.0),

                    /* 15 */ new VariableDef(5.0, 4.0, 3.0, 3.0)    //   ....a....
                };

            var clusterDefs = new[]
                {
                    new ClusterDef(
                        new BorderInfo(4.0),
                        new BorderInfo(1.0),
                        new BorderInfo(0.0),
                        new BorderInfo(0.0)),
                    new ClusterDef(
                        new BorderInfo(0.0),
                        new BorderInfo(0.0),
                        new BorderInfo(5.0),
                        new BorderInfo(1.0))
                };

            // 'a' vars above are not added to a cluster - they are at the root.
            clusterDefs[0].AddVariableDef(variableDefs[0x2]);
            clusterDefs[0].AddVariableDef(variableDefs[0x5]);
            clusterDefs[0].AddVariableDef(variableDefs[0xA]);
            clusterDefs[0].AddVariableDef(variableDefs[0xD]);
            clusterDefs[0].SetResultPositions(-30.56931, 17.43469, -9.35942, 7.64458);
            clusterDefs[0].AddClusterDef(clusterDefs[1]);

            clusterDefs[1].AddVariableDef(variableDefs[0x1]);
            clusterDefs[1].AddVariableDef(variableDefs[0x4]);
            clusterDefs[1].AddVariableDef(variableDefs[0x7]);
            clusterDefs[1].AddVariableDef(variableDefs[0x8]);
            clusterDefs[1].AddVariableDef(variableDefs[0xB]);
            clusterDefs[1].AddVariableDef(variableDefs[0xE]);
            clusterDefs[1].SetResultPositions(-17.56831, 7.43369, -7.35842, 5.64358);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };
            MinPaddingX = 1.0;
            MinPaddingY = 2.0;
            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
            }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test sibling clusters with margins")]
        public void Test_Cluster_Sibling_Margins()
        {
            var expectedPositionsX = new[]
                {
                    7.00000, 1.42843, 0.50000, 4.00000, 4.42843, 6.50000, 10.42943, 7.42843, 1.42843, 1.00000, 3.50000,
                    4.42843, 7.00000, 9.50000, 7.42843, 4.00000
                };
            var expectedPositionsY = new[]
                {
                    2.73267, -4.26733, 5.73367, 2.73267, -4.26733, 5.73367, 2.00000, -4.26733, -1.26733, 8.73467, 5.73367,
                    -1.26733, 8.73467, 5.73367, -1.26733, 8.73467
                };
            var variableDefs = new[]
                {
                    // ordinal                 posXY     sizeXY      There are two sibling clusters, 'b' and 'c'; 'a' is root
                    /* 0  */ new VariableDef(5.0, 1.0, 3.0, 3.0),   //   ....a....

                    //    1234567
                    /* 1  */ new VariableDef(2.0, 2.0, 3.0, 3.0),
                    //   .cbacbac.     (c=variableDefs[1],b=[2],a=[9], ...)
                    /* 2  */ new VariableDef(3.0, 2.0, 3.0, 3.0),
                    // ^----|--|       (move 'a' vars outside all clusters)
                    /* 3  */ new VariableDef(4.0, 2.0, 3.0, 3.0),
                    //     |--|---^    (move 'b' vars outside of all 'c' vars)
                    /* 4  */ new VariableDef(5.0, 2.0, 3.0, 3.0),
                    //  ^-|--|--|      (move 'c' vars together into cluster nested in 'b')
                    /* 5  */ new VariableDef(6.0, 2.0, 3.0, 3.0),
                    /* 6  */ new VariableDef(7.0, 2.0, 3.0, 3.0),
                    /* 7  */ new VariableDef(8.0, 2.0, 3.0, 3.0),

                    //    89ABCDE      (hex ordinals)
                    /* 8  */ new VariableDef(2.0, 3.0, 3.0, 3.0),
                    //   .cabcabc.     (c=variableDefs[7],b=[8],a=[9], ...)
                    /* 9  */ new VariableDef(3.0, 3.0, 3.0, 3.0),
                    // ^---|--|        (move 'a' vars outside all clusters)
                    /* 10 */ new VariableDef(4.0, 3.0, 3.0, 3.0),
                    //      |--|---^   (move 'b' vars outside of all 'c' vars)
                    /* 11 */ new VariableDef(5.0, 3.0, 3.0, 3.0),
                    //  ^-|--|--|      (move 'c' vars together into cluster nested in 'b')
                    /* 12 */ new VariableDef(6.0, 3.0, 3.0, 3.0),
                    /* 13 */ new VariableDef(7.0, 3.0, 3.0, 3.0),
                    /* 14 */ new VariableDef(8.0, 3.0, 3.0, 3.0),

                    /* 15 */ new VariableDef(5.0, 4.0, 3.0, 3.0)    //   ....a....
                };

            var clusterDefs = new[]
                {
                    new ClusterDef(
                        new BorderInfo(4.0),
                        new BorderInfo(1.0),
                        new BorderInfo(0.0),
                        new BorderInfo(0.0)),
                    new ClusterDef(
                        new BorderInfo(0.0),
                        new BorderInfo(0.0),
                        new BorderInfo(5.0),
                        new BorderInfo(1.0))
                };

            // 'a' vars above are not added to a cluster - they are at the root.
            clusterDefs[0].AddVariableDef(variableDefs[0x2]);
            clusterDefs[0].AddVariableDef(variableDefs[0x5]);
            clusterDefs[0].AddVariableDef(variableDefs[0xA]);
            clusterDefs[0].AddVariableDef(variableDefs[0xD]);
            clusterDefs[0].SetResultPositions(-5.00000, 12.00000, 4.23267, 7.23467);
            // This is the difference from Test_Cluster_Nest; make them siblings here instead
            //clusterDefs[0].AddClusterDef(clusterDefs[1]);

            clusterDefs[1].AddVariableDef(variableDefs[0x1]);
            clusterDefs[1].AddVariableDef(variableDefs[0x4]);
            clusterDefs[1].AddVariableDef(variableDefs[0x7]);
            clusterDefs[1].AddVariableDef(variableDefs[0x8]);
            clusterDefs[1].AddVariableDef(variableDefs[0xB]);
            clusterDefs[1].AddVariableDef(variableDefs[0xE]);
            clusterDefs[1].SetResultPositions(-0.07257, 8.92943, -10.76733, 1.23267);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [WorkItem(568064)]
        [Description("Test sibling and nested clusters with no padding and minimum sizes taller than wide")]
        public void Test_Cluster_Pad0_Sibling9_Nest1_MinSize_5_10()
        {
            var expectedPositionsX = new[]
            {
                -17.00100,
                -2.00100,
                11.00100,
                -12.00100,
                2.00000,
                16.00100,
                -7.00100,
                6.00100,
                21.00100
            };
            var expectedPositionsY = new[]
            {
                1.00000,
                1.00000,
                1.00000,
                2.00000,
                2.00000,
                2.00000,
                3.00000,
                3.00000,
                3.00000
            };

            VariableDef[] variableDefs;
            ClusterDef[] clusterDefs;
            Setup__Test_Cluster_Pad0_Sibling9_Nest1_MinSize(5, 10, out variableDefs, out clusterDefs);

            // Note: The clusters are taller than wide, and with the change to precalculate cluster sizes according to
            // MinSize, DeferToVertical does not come into play and thus the spacing is spread out horizontally resulting in more total
            // movement of the nodes.
            clusterDefs[0].SetResultPositions(-20.50100, 24.50100, -3.00100, 7.00100);
            clusterDefs[1].SetResultPositions(-20.50000, -15.50000, -3.00000, 7.00000);
            clusterDefs[2].SetResultPositions(-5.50000, -0.50000, -3.00000, 7.00000);
            clusterDefs[3].SetResultPositions(9.50000, 14.50000, -3.00000, 7.00000);
            clusterDefs[4].SetResultPositions(-15.50000, -10.50000, -3.00000, 7.00000);
            clusterDefs[5].SetResultPositions(-0.50000, 4.50000, -3.00000, 7.00000);
            clusterDefs[6].SetResultPositions(14.50000, 19.50000, -3.00000, 7.00000);
            clusterDefs[7].SetResultPositions(-10.50000, -5.50000, -3.00000, 7.00000);
            clusterDefs[8].SetResultPositions(4.50000, 9.50000, -3.00000, 7.00000);
            clusterDefs[9].SetResultPositions(19.50000, 24.50000, -3.00000, 7.00000);

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, null /*constraintDefsX*/, null /*constraintDefsY*/, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [WorkItem(568064)]
        [Description("Test sibling and nested clusters with no padding and minimum sizes wider than tall")]
        public void Test_Cluster_Pad0_Sibling9_Nest1_MinSize_10_5()
        {
            var expectedPositionsX = new[]
            {
                1.00000,
                2.00000,
                3.00000,
                1.00000,
                2.00000,
                3.00000,
                1.00000,
                2.00000,
                3.00000           
            };
            var expectedPositionsY = new[]
            {
                -17.00100,
                -12.00100,
                -7.00100,
                -2.00100,
                2.00000,
                6.00100,
                11.00100,
                16.00100,
                21.00100
            };

            VariableDef[] variableDefs;
            ClusterDef[] clusterDefs;
            Setup__Test_Cluster_Pad0_Sibling9_Nest1_MinSize(10, 5, out variableDefs, out clusterDefs);

            clusterDefs[0].SetResultPositions(-3.00100, 7.00100, -20.50100, 24.50100);
            clusterDefs[1].SetResultPositions(-3.00000, 7.00000, -20.50000, -15.50000);
            clusterDefs[2].SetResultPositions(-3.00000, 7.00000, -15.50000, -10.50000);
            clusterDefs[3].SetResultPositions(-3.00000, 7.00000, -10.50000, -5.50000);
            clusterDefs[4].SetResultPositions(-3.00000, 7.00000, -5.50000, -0.50000);
            clusterDefs[5].SetResultPositions(-3.00000, 7.00000, -0.50000, 4.50000);
            clusterDefs[6].SetResultPositions(-3.00000, 7.00000, 4.50000, 9.50000);
            clusterDefs[7].SetResultPositions(-3.00000, 7.00000, 9.50000, 14.50000);
            clusterDefs[8].SetResultPositions(-3.00000, 7.00000, 14.50000, 19.50000);
            clusterDefs[9].SetResultPositions(-3.00000, 7.00000, 19.50000, 24.50000);

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, null /*constraintDefsX*/, null /*constraintDefsY*/, expectedPositionsX, expectedPositionsY,
                /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [WorkItem(568064)]
        [Description("Test sibling and nested clusters with no padding and minimum sizes equally wide and tall")]
        public void Test_Cluster_Pad0_Sibling9_Nest1_MinSize_5_5()
        {
            var expectedPositionsX = new[]
            {
                -2.00100,
                 2.00000,
                 6.00100,
                -2.00100,
                 2.00000,
                 6.00100,
                -2.00100,
                 2.00000,
                 6.00100
            };
            var expectedPositionsY = new[]
            {
                -2.00100,
                -2.00100,
                -2.00100,
                 2.00000,
                 2.00000,
                 2.00000,
                 6.00100,
                 6.00100,
                 6.00100
            };

            VariableDef[] variableDefs;
            ClusterDef[] clusterDefs;
            Setup__Test_Cluster_Pad0_Sibling9_Nest1_MinSize(5, 5, out variableDefs, out clusterDefs);

            // Note: The clusters are taller than wide, and with the change to precalculate cluster sizes according to
            // MinSize, DeferToVertical does not come into play and thus the spacing is spread out horizontally resulting in more total
            // movement of the nodes.
            clusterDefs[0].SetResultPositions(-5.50100, 9.50100, -5.50100, 9.50100);
            clusterDefs[1].SetResultPositions(-5.50000, -0.50000, -5.50000, -0.50000);
            clusterDefs[2].SetResultPositions(-0.50000, 4.50000, -5.50000, -0.50000);
            clusterDefs[3].SetResultPositions(4.50000, 9.50000, -5.50000, -0.50000);
            clusterDefs[4].SetResultPositions(-5.50000, -0.50000, -0.50000, 4.50000);
            clusterDefs[5].SetResultPositions(-0.50000, 4.50000, -0.50000, 4.50000);
            clusterDefs[6].SetResultPositions(4.50000, 9.50000, -0.50000, 4.50000);
            clusterDefs[7].SetResultPositions(-5.50000, -0.50000, 4.50000, 9.50000);
            clusterDefs[8].SetResultPositions(-0.50000, 4.50000, 4.50000, 9.50000);
            clusterDefs[9].SetResultPositions(4.50000, 9.50000, 4.50000, 9.50000);

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, null /*constraintDefsX*/, null /*constraintDefsY*/, expectedPositionsX, expectedPositionsY,
                /*checkResults:*/ true),
                    FailureString);
        }

        private static void Setup__Test_Cluster_Pad0_Sibling9_Nest1_MinSize(int width, int height, out VariableDef[] variableDefs, out ClusterDef[] clusterDefs)
        {
            variableDefs = new[]
                {
                    //ordinal                  posXY     sizeXY      There is only one cluster, with 'b' vars; 'a' is root
                    /* 0  */ new VariableDef(1.0, 1.0, 3.0, 3.0),
                    /* 1  */ new VariableDef(2.0, 1.0, 3.0, 3.0),
                    /* 2  */ new VariableDef(3.0, 1.0, 3.0, 3.0),

                    /* 3  */ new VariableDef(1.0, 2.0, 3.0, 3.0),
                    /* 4  */ new VariableDef(2.0, 2.0, 3.0, 3.0),
                    /* 5  */ new VariableDef(3.0, 2.0, 3.0, 3.0),

                    /* 6  */ new VariableDef(1.0, 3.0, 3.0, 3.0),
                    /* 7  */ new VariableDef(2.0, 3.0, 3.0, 3.0),
                    /* 8  */ new VariableDef(3.0, 3.0, 3.0, 3.0),
                };

            clusterDefs = new ClusterDef[10];
            clusterDefs[0] = new ClusterDef(); // Parent cluster

            for (int ii = 1; ii <= 9; ++ii)
            {
                // 1-based to skip the parent cluster.
                clusterDefs[ii] = new ClusterDef(width, height);
                clusterDefs[ii].AddVariableDef(variableDefs[ii - 1]); // 0-based
                clusterDefs[0].AddClusterDef(clusterDefs[ii]);
            }
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test one fixed border on opposite sides of two non-overlapping clusters in the horizontal axis")]
        public void Test_Fixed_Left1Right2()
        {
            // Success positions:
            var expectedPositionsX = new[] { 2.50100, 11.49900, 2.50100, 11.49900 };
            var expectedPositionsY = new[] { 4.5, 4.5, 14.5, 14.5 };

            // Test one fixed border on opposite sides of two non-overlapping clusters in the horizontal axis.
            // Note that we cannot really test this if we put the fixed border further away
            // in its direction than the outermost variables (e.g. Left border fixed and 
            // further to the left than it needs to be to satisfy the Left-border constraints
            // on its contained nodes), because we'll never move the variables toward it
            // (we don't try to shrink the distance unless there are equality constraints).
            // So start the variables outside where we want the borders to be.
            var variableDefs = new[]
                {
                    //                posXY     sizeXY
                    new VariableDef(-5.0, 4.5, 3.0, 3.0),
                    new VariableDef(27.0, 4.5, 3.0, 3.0),
                    new VariableDef(-5.0, 14.5, 3.0, 3.0),
                    new VariableDef(27.0, 14.5, 3.0, 3.0)
                };

            var clusterDefs = new[]
                {
                    new ClusterDef(
                        new BorderInfo(0.0, 1.0, BorderInfo.DefaultFixedWeight),
                        new BorderInfo(0.0),
                        new BorderInfo(0.0),
                        new BorderInfo(0.0)),
                    new ClusterDef(
                        new BorderInfo(0.0),
                        new BorderInfo(0.0, 13.0, BorderInfo.DefaultFixedWeight),
                        new BorderInfo(0.0),
                        new BorderInfo(0.0))
                };

            clusterDefs[0].AddVariableDef(variableDefs[0]);
            clusterDefs[0].AddVariableDef(variableDefs[2]);
            clusterDefs[0].SetResultPositions(1.00000, 4.00200, 2.99900, 16.00100);
            clusterDefs[0].RetainFixedBorders = true;

            clusterDefs[1].AddVariableDef(variableDefs[1]);
            clusterDefs[1].AddVariableDef(variableDefs[3]);
            clusterDefs[1].SetResultPositions(9.99800, 13.0000, 2.99900, 16.00100);
            clusterDefs[1].RetainFixedBorders = true;

            var constraintDefsX = new[] { new ConstraintDef(variableDefs[0], variableDefs[2], 3.0) };

            var constraintDefsY = new[] { new ConstraintDef(variableDefs[0], variableDefs[1], 2.0) };

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test one fixed border on opposite sides of two non-overlapping clusters in both horizontal and vertical axes")]
        public void Test_Fixed_LeftRightTopBottom()
        {
            // Success positions:
            var expectedPositionsX = new[] { 51.50100, 1.0, 54.5, 7.0 };
            var expectedPositionsY = new[] { 201.50100, 7.0, 201.50100, 7.0 };

            // Test one fixed border on opposite sides of two non-overlapping clusters in both horizontal and vertical axes.
            // Note that we cannot really test this if we put the fixed border further away
            // in its direction than the outermost variables (e.g. Left border fixed and 
            // further to the left than it needs to be to satisfy the Left-border constraints
            // on its contained nodes), because we'll never move the variables toward it
            // (we don't try to shrink the distance unless there are equality constraints).
            // So start the variables outside where we want the borders to be.
            var variableDefs = new[]
                {
                    //                posXY     sizeXY
                    new VariableDef(1.0, 1.0, 3.0, 3.0),
                    new VariableDef(1.0, 7.0, 3.0, 3.0),
                    new VariableDef(7.0, 1.0, 3.0, 3.0),
                    new VariableDef(7.0, 7.0, 3.0, 3.0)
                };

            var clusterDefs = new[]
                {
                    new ClusterDef(
                        new BorderInfo(0.0, 50.0, BorderInfo.DefaultFixedWeight),
                        new BorderInfo(0.0, 100.0, BorderInfo.DefaultFixedWeight),
                        new BorderInfo(0.0, 200.0, BorderInfo.DefaultFixedWeight),
                        new BorderInfo(0.0, 300.0, BorderInfo.DefaultFixedWeight))
                };

            clusterDefs[0].AddVariableDef(variableDefs[0]);
            clusterDefs[0].AddVariableDef(variableDefs[2]);
            clusterDefs[0].SetResultPositions(50.0, 100.0, 200.0, 300.0);
            clusterDefs[0].RetainFixedBorders = true;

            var constraintDefsX = new[] { new ConstraintDef(variableDefs[0], variableDefs[2], 3.0) };

            var constraintDefsY = new[] { new ConstraintDef(variableDefs[0], variableDefs[1], 2.0) };

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test clusters arranged in a layer cake")]
        public void Test_LayerCake()
        {
            var expectedPositionsX = new[]
                {
                    -0.00200, 3.00000, 6.00200, -0.00200, 3.00000, 6.00200, -0.00200, 3.00000, 6.00200, -0.00200, 3.00000,
                    6.00200, -3.00300, 9.00300, -3.00300, 9.00300
                };
            var expectedPositionsY = new[]
                {
                    -2.00100, -2.00100, -2.00100, 0.99900, 0.99900, 0.99900, 4.00100, 4.00100, 4.00100, 7.00100, 7.00100,
                    7.00100, 0.99900, 0.99900, 4.00100, 4.00100
                };

            // Result should be as follows, where # are horizontal cluster borders and =+| are vertical:
            //       +===+ +===+ +===+
            //       | 0 | | 1 | | 2 |
            //  #####|###|#|###|#|###|#####
            //  # 12 | 3 | | 4 | | 5 | 13 #
            //  #####|###|#|###|#|###|#####
            //       |   | |   | |   |
            //  #####|###|#|###|#|###|#####
            //  # 14 | 6 | | 7 | | 8 | 15 #
            //  #####|###|#|###|#|###|#####
            //       | 9 | | 10| | 11|
            //       +===+ +===+ +===+
            var variableDefs = new[]
                {
                    // ordinal                 posXY     sizeXY
                    /* 0  */ new VariableDef(2.0, 1.0, 3.0, 3.0),
                    /* 1  */ new VariableDef(3.0, 1.0, 3.0, 3.0),
                    /* 2  */ new VariableDef(4.0, 1.0, 3.0, 3.0),

                    /* 3  */ new VariableDef(2.0, 2.0, 3.0, 3.0),
                    /* 4  */ new VariableDef(3.0, 2.0, 3.0, 3.0),
                    /* 5  */ new VariableDef(4.0, 2.0, 3.0, 3.0),

                    /* 6  */ new VariableDef(2.0, 3.0, 3.0, 3.0),
                    /* 7  */ new VariableDef(3.0, 3.0, 3.0, 3.0),
                    /* 8  */ new VariableDef(4.0, 3.0, 3.0, 3.0),

                    /* 9  */ new VariableDef(2.0, 4.0, 3.0, 3.0),
                    /* 10 */ new VariableDef(3.0, 4.0, 3.0, 3.0),
                    /* 11 */ new VariableDef(4.0, 4.0, 3.0, 3.0),

                    // The above has vars in Vertical but not Horizontal clusters.
                    // Add vars in Horizontal but not Vertical clusters.
                    /* 12 */ new VariableDef(1.0, 2.0, 3.0, 3.0),
                    /* 13 */ new VariableDef(5.0, 2.0, 3.0, 3.0),
                    /* 14 */ new VariableDef(1.0, 3.0, 3.0, 3.0),
                    /* 15 */ new VariableDef(5.0, 3.0, 3.0, 3.0)
                };

            var clusterDefs = new[]
                {
                    new ClusterDef(), new ClusterDef(), new ClusterDef(), new ClusterDef(), new ClusterDef(),
                    new ClusterDef()
                };

            // Horizontal clusters are in the root hierarchy.
            int idxClus = 0;
            clusterDefs[idxClus].AddVariableDef(variableDefs[3]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[4]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[5]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[12]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[13]);
            clusterDefs[idxClus].SetResultPositions(-4.50300, 10.50300, -0.50200, 2.50000);

            ++idxClus;
            clusterDefs[idxClus].AddVariableDef(variableDefs[6]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[7]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[8]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[14]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[15]);
            clusterDefs[idxClus].SetResultPositions(-4.50300, 10.50300, 2.50000, 5.50200);

            // clusterDefs[idxNewHier] has no variables in this test, only the clusters of its hierarchy.
            // It is the root of a hierarchy so has no borders.
            ++idxClus;
            int idxNewHier = idxClus;
            clusterDefs[idxClus].IsNewHierarchy = true;

            ++idxClus;
            clusterDefs[idxClus].AddVariableDef(variableDefs[0]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[3]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[6]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[9]);
            clusterDefs[idxClus].SetResultPositions(-1.50300, 1.49900, -3.50200, 8.50200);
            clusterDefs[idxNewHier].AddClusterDef(clusterDefs[idxClus]);

            ++idxClus;
            clusterDefs[idxClus].AddVariableDef(variableDefs[1]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[4]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[7]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[10]);
            clusterDefs[idxClus].SetResultPositions(1.49900, 4.50100, -3.50200, 8.50200);
            clusterDefs[idxNewHier].AddClusterDef(clusterDefs[idxClus]);

            ++idxClus;
            clusterDefs[idxClus].AddVariableDef(variableDefs[2]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[5]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[8]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[11]);
            clusterDefs[idxClus].SetResultPositions(4.50100, 7.50300, -3.50200, 8.50200);
            clusterDefs[idxNewHier].AddClusterDef(clusterDefs[idxClus]);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test clusters arranged in a layer cake with fixed cluster borders")]
        public void Test_LayerCake_Fixed()
        {
            var expectedPositionsX = new[]
                {
                    -4.00200, 3.00000, 9.00200, -4.00200, 3.00000, 9.00200, -4.00200, 3.00000, 9.00200, -4.00200, 3.00000,
                    9.00200, 0.00000, 6.00000, 0.00000, 6.00000
                };
            var expectedPositionsY = new[]
                {
                    1.00000, 1.00000, 1.00000, -4.00100, -4.00100, -4.00100, 9.00100, 9.00100, 9.00100, 4.00000, 4.00000,
                    4.00000, -4.00100, -4.00100, 9.00100, 9.00100
                };

            // Result should be as follows, where # are horizontal cluster borders and =+| are vertical
            // (note the extra space between clusters relative to Test_LayerCake, due to the fixed border
            // positions here):
            //       +===+  +===+  +===+
            //       | 0 |  | 1 |  | 2 |
            //  #####|###|##|###|##|###|#####
            //  # 12 | 3 |  | 4 |  | 5 | 13 #
            //  #####|###|##|###|##|###|#####
            //       |   |  |   |  |   |
            //       |   |  |   |  |   |
            //       |   |  |   |  |   |
            //  #####|###|##|###|##|###|#####
            //  # 14 | 6 |  | 7 |  | 8 | 15 #
            //  #####|###|##|###|##|###|#####
            //       | 9 |  | 10|  | 11|
            //       +===+  +===+  +===+
            // As in Test_Fixed, we'll test one fixed border on opposite sides of the two non-overlapping
            // clusters on either end of each hierarchy (the middle vertical cluster is not fixed).
            // Also as in Test_Fixed, we cannot really test this if we put the fixed border further away
            // in its direction than the outermost variables (e.g. Left border fixed and 
            // further to the left than it needs to be to satisfy the Left-border constraints
            // on its contained nodes), because we'll never move the variables toward it
            // (we don't try to shrink the distance unless there are equality constraints).
            // So start the variables outside where we want the borders to be.  So anything less than 0
            // or greater than 20 is outside the expected fixed-border range.
            var variableDefs = new[]
                {
                    // ordinal                 posXY     sizeXY
                    /* 0  */ new VariableDef(-10.0, 1.0, 3.0, 3.0),
                    // Pushed right by Left border and up by Top border
                    /* 1  */ new VariableDef(3.0, 1.0, 3.0, 3.0),
                    // pushed up by Top border
                    /* 2  */ new VariableDef(24.0, 1.0, 3.0, 3.0),
                    // Pushed left by Right border and up by Top border

                    /* 3  */ new VariableDef(-10.0, -10.0, 3.0, 3.0),
                    // Move right and down
                    /* 4  */ new VariableDef(3.0, -10.0, 3.0, 3.0),     // Move down
                    /* 5  */ new VariableDef(24.0, -10.0, 3.0, 3.0),
                    // Move left and down

                    /* 6  */ new VariableDef(-10.0, 20.0, 3.0, 3.0),
                    // Move right and up
                    /* 7  */ new VariableDef(3.0, 20.0, 3.0, 3.0),      // Move up
                    /* 8  */ new VariableDef(20.0, 20.0, 3.0, 3.0),     // Move left and up

                    /* 9  */ new VariableDef(-10.0, 4.0, 3.0, 3.0),
                    // Pushed right by Left border and down by Bottom border
                    /* 10 */ new VariableDef(3.0, 4.0, 3.0, 3.0),
                    // Pushed down by Bottom border
                    /* 11 */ new VariableDef(20.0, 4.0, 3.0, 3.0),
                    // Pushed left by Right border and down by Bottom border

                    // The above has vars in Vertical but not Horizontal clusters.
                    // Add vars in Horizontal but not Vertical clusters.  Only force the
                    // Y coordinates to be initially out of range.
                    /* 12 */ new VariableDef(1.0, -12.0, 3.0, 3.0),
                    // Pushed right by Left border
                    /* 13 */ new VariableDef(5.0, -12.0, 3.0, 3.0),
                    /* 14 */ new VariableDef(1.0, 20.0, 3.0, 3.0),
                    // Pushed left by Right border
                    /* 15 */ new VariableDef(5.0, 20.0, 3.0, 3.0),
                };

            var clusterDefs = new[]
                {
                    new ClusterDef(), new ClusterDef(), new ClusterDef(), new ClusterDef(), new ClusterDef(),
                    new ClusterDef()
                };

            // Horizontal clusters are in the root hierarchy.
            int idxClus = 0;
            clusterDefs[idxClus].AddVariableDef(variableDefs[3]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[4]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[5]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[12]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[13]);
            // Non-fixed                             (-4.50300, 10.50300, -0.50200,  2.50000);
            clusterDefs[idxClus].SetResultPositions(-5.50300, 10.50300, -5.50200, -2.50000);
            clusterDefs[idxClus].TopBorderInfo = new BorderInfo(
                0.0 /* no margin */, clusterDefs[idxClus].TopResultPos, BorderInfo.DefaultFixedWeight);
            clusterDefs[idxClus].RetainFixedBorders = true;

            ++idxClus;
            clusterDefs[idxClus].AddVariableDef(variableDefs[6]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[7]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[8]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[14]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[15]);
            // Non-fixed                           (-4.50300, 10.50300, 2.50000, 5.50200);
            clusterDefs[idxClus].SetResultPositions(-5.50300, 10.50300, 7.50000, 10.50200);
            clusterDefs[idxClus].BottomBorderInfo = new BorderInfo(
                0.0 /* no margin */, clusterDefs[idxClus].BottomResultPos, BorderInfo.DefaultFixedWeight);
            clusterDefs[idxClus].RetainFixedBorders = true;

            // clusterDefs[idxNewHier] has no variables in this test, only the clusters of its hierarchy.
            // It is the root of a hierarchy so has no borders.
            ++idxClus;
            int idxNewHier = idxClus;
            clusterDefs[idxClus].IsNewHierarchy = true;

            ++idxClus;
            clusterDefs[idxClus].AddVariableDef(variableDefs[0]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[3]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[6]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[9]);
            // Non-fixed                           (-1.50300,  1.49900, -3.50200, 8.50200);
            clusterDefs[idxClus].SetResultPositions(-5.50300, -2.50100, -5.50200, 10.50200);
            clusterDefs[idxClus].LeftBorderInfo = new BorderInfo(
                0.0 /* no margin */, clusterDefs[idxClus].LeftResultPos, BorderInfo.DefaultFixedWeight);
            clusterDefs[idxClus].RetainFixedBorders = true;
            clusterDefs[idxNewHier].AddClusterDef(clusterDefs[idxClus]);

            ++idxClus;
            clusterDefs[idxClus].AddVariableDef(variableDefs[1]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[4]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[7]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[10]);
            // Non-fixed                           (1.49900, 4.50100, -3.50200, 8.50200);
            clusterDefs[idxClus].SetResultPositions(1.49900, 4.50100, -5.50200, 10.50200);
            clusterDefs[idxNewHier].AddClusterDef(clusterDefs[idxClus]);

            ++idxClus;
            clusterDefs[idxClus].AddVariableDef(variableDefs[2]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[5]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[8]);
            clusterDefs[idxClus].AddVariableDef(variableDefs[11]);
            // Non-fixed                            4.50100, 7.50300, -3.50200, 8.50200);
            clusterDefs[idxClus].SetResultPositions(7.50100, 10.50300, -5.50200, 10.50200);
            clusterDefs[idxClus].RightBorderInfo = new BorderInfo(
                0.0 /* no margin */, clusterDefs[idxClus].RightResultPos, BorderInfo.DefaultFixedWeight);
            clusterDefs[idxClus].RetainFixedBorders = true;
            clusterDefs[idxNewHier].AddClusterDef(clusterDefs[idxClus]);

            var constraintDefsX = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3.0),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3.0),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3.0)
                };

            var constraintDefsY = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 2.0),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 2.0),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 2.0)
                };

            Validate.IsTrue(
                    CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, expectedPositionsX, expectedPositionsY,
                            /*checkResults:*/ true),
                    FailureString);
        }
    }
}
