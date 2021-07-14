// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectionSolverTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;

using Microsoft.Msagl.Core.ProjectionSolver;

using ProjSolv = Microsoft.Msagl.Core.ProjectionSolver;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///  File-independent tests for ProjectionSolver.
    /// </summary>
    [TestClass]
    [Ignore]
    public class ProjectionSolverTests : ProjectionSolverVerifier
    {
        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test")]
        public void Test1()
        {
            var expectedPositionsX = new[] { 1.4, 4.4, 7.4, 7.4, 10.4 };
            var variableDefs = new[]
                {
                    new VariableDef(2, ValueNotUsed),
                    new VariableDef(9, ValueNotUsed),
                    new VariableDef(9, ValueNotUsed),
                    new VariableDef(9, ValueNotUsed),
                    new VariableDef(2, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        // ReSharper disable InconsistentNaming

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test")]
        public void Test2()
        {
            var expectedPositionsX = new[] { 0.5, 6, 3.5, 6.5, 9.5 };
            var variableDefs = new[]
                {
                    new VariableDef(4, ValueNotUsed),
                    new VariableDef(6, ValueNotUsed),
                    new VariableDef(9, ValueNotUsed),
                    new VariableDef(2, ValueNotUsed),
                    new VariableDef(5, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test")]
        public void Test3()
        {
            var expectedPositionsX = new[] { 5, 0.5, 3.5, 6.5, 9.5 };
            var variableDefs = new[]
                {
                    new VariableDef(5, ValueNotUsed),
                    new VariableDef(6, ValueNotUsed),
                    new VariableDef(7, ValueNotUsed),
                    new VariableDef(4, ValueNotUsed),
                    new VariableDef(3, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test")]
        public void Test4()
        {
            var expectedPositionsX = new[] { 0.8, 3.8, 0.8, 3.8, 6.8 };
            var variableDefs = new[]
                {
                    new VariableDef(7, ValueNotUsed),
                    new VariableDef(1, ValueNotUsed),
                    new VariableDef(6, ValueNotUsed),
                    new VariableDef(0, ValueNotUsed),
                    new VariableDef(2, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test with a few more variables")]
        public void Test5()
        {
            var expectedPositionsX = new[] { -3.71429, 4, 1, -0.714286, 2.28571, 2.28571, 7, 5.28571, 8.28571, 11.2857 };
            var variableDefs = new[]
                {
                    new VariableDef(0, ValueNotUsed),
                    new VariableDef(9, ValueNotUsed),
                    new VariableDef(1, ValueNotUsed),
                    new VariableDef(9, ValueNotUsed),
                    new VariableDef(5, ValueNotUsed),
                    new VariableDef(1, ValueNotUsed),
                    new VariableDef(2, ValueNotUsed),
                    new VariableDef(1, ValueNotUsed),
                    new VariableDef(6, ValueNotUsed),
                    new VariableDef(3, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[8], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[6], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[6], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[5], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[6], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[7], 3),
                    new ConstraintDef(variableDefs[4], variableDefs[8], 3),
                    new ConstraintDef(variableDefs[4], variableDefs[7], 3),
                    new ConstraintDef(variableDefs[5], variableDefs[8], 3),
                    new ConstraintDef(variableDefs[5], variableDefs[7], 3),
                    new ConstraintDef(variableDefs[5], variableDefs[8], 3),
                    new ConstraintDef(variableDefs[6], variableDefs[9], 3),
                    new ConstraintDef(variableDefs[7], variableDefs[8], 3),
                    new ConstraintDef(variableDefs[7], variableDefs[9], 3),
                    new ConstraintDef(variableDefs[8], variableDefs[9], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test")]
        public void Test6()
        {
            var expectedPositionsX = new[] { -0.75, 0, 2.25, 5.25, 8.25 };
            var variableDefs = new[]
                {
                    new VariableDef(7, ValueNotUsed),
                    new VariableDef(0, ValueNotUsed),
                    new VariableDef(3, ValueNotUsed),
                    new VariableDef(1, ValueNotUsed),
                    new VariableDef(4, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test")]
        public void Test7()
        {
            var expectedPositionsX = new[] { -0.5, 2, 2.5, 5.5, 8.5 };
            var variableDefs = new[]
                {
                    new VariableDef(4, ValueNotUsed),
                    new VariableDef(2, ValueNotUsed),
                    new VariableDef(3, ValueNotUsed),
                    new VariableDef(1, ValueNotUsed),
                    new VariableDef(8, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test")]
        public void Test8()
        {
            var expectedPositionsX = new[] { -2.4, 0.6, 3.6, 6.6, 9.6 };
            var variableDefs = new[]
                {
                    new VariableDef(3, ValueNotUsed),
                    new VariableDef(4, ValueNotUsed),
                    new VariableDef(0, ValueNotUsed),
                    new VariableDef(5, ValueNotUsed),
                    new VariableDef(6, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test")]
        public void Test9()
        {
            var expectedPositionsX = new[] { 3.6, 0.6, 3.6, 6.6, 9.6 };
            var variableDefs = new[]
                {
                    new VariableDef(8, ValueNotUsed),
                    new VariableDef(2, ValueNotUsed),
                    new VariableDef(6, ValueNotUsed),
                    new VariableDef(5, ValueNotUsed),
                    new VariableDef(3, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test with more constraints than variables")]
        public void Test13()
        {
            // Note:  These results taken from output of satisfy_inc.
            var expectedPositionsX = new[] { 0.0035988, 0.0035988, 3.0036, 6.0036, 9.0036 };
            var variableDefs = new[]
                {
                    new VariableDef(0.485024, ValueNotUsed),
                    new VariableDef(3.52714, ValueNotUsed),
                    new VariableDef(4.01263, ValueNotUsed),
                    new VariableDef(4.58524, ValueNotUsed),
                    new VariableDef(5.40796, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Small solver test")]
        public void Test_3_Vars()
        {
            var expectedPositionsX = new[] { -2.00000, 1.00000, 4.00000 };
            var variableDefs = new[]
                {
                    new VariableDef(1.0, ValueNotUsed),
                    new VariableDef(1.0, ValueNotUsed),
                    new VariableDef(1.0, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test with weights")]
        public void Test_3_Vars_Weight()
        {
            var expectedPositionsX = new[] { -4.67568, -1.67568, 1.32432 };
            var variableDefs = new[]
                {
                    new VariableDef(1.0, ValueNotUsed, 1.0),
                    new VariableDef(1.0, ValueNotUsed, 10.0),
                    new VariableDef(1.0, ValueNotUsed, 100.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Small solver test")]
        public void Test_4_Vars()
        {
            var expectedPositionsX = new[] { -3.50000, -0.50000, 2.50000, 5.50000 };
            var variableDefs = new[]
                {
                    new VariableDef(1.0, ValueNotUsed),
                    new VariableDef(1.0, ValueNotUsed),
                    new VariableDef(1.0, ValueNotUsed),
                    new VariableDef(1.0, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Small solver test with weights")]
        public void Test_4_Vars_Weight()
        {
            var expectedPositionsX = new[] { -7.66787, -4.66787, -1.66787, 1.33213 };
            var variableDefs = new[]
                {
                    new VariableDef(1.0, ValueNotUsed, 1.0),
                    new VariableDef(1.0, ValueNotUsed, 10.0),
                    new VariableDef(1.0, ValueNotUsed, 100.0),
                    new VariableDef(1.0, ValueNotUsed, 1000.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test comparing reversed positions with Test2")]
        public void Test2_Reverse()
        {
            //var expectedPositionsX = new[] { 0.5, 6.0, 3.5, 6.5, 9.5 };
            var expectedPositionsX = new[] { 9.5, 6.0, 6.5, 3.5, 0.5 };

            var variableDefs = new[]
                {
                    new VariableDef(4, ValueNotUsed),
                    new VariableDef(6, ValueNotUsed),
                    new VariableDef(9, ValueNotUsed),
                    new VariableDef(2, ValueNotUsed),
                    new VariableDef(5, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[2], variableDefs[0], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[0], 3),
                    new ConstraintDef(variableDefs[4], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[4], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[4], variableDefs[3], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test with negative gap")]
        public void Test2_NegativeGap()
        {
            // For negative gap, the interpretation is "a can be up to <+gap> greater than b".
            //var expectedPositionsX = new[] { 0.5, 6.0, 3.5, 6.5, 9.5 };
            var expectedPositionsX = new[] { 4.0, 6.0, 7.0, 4.0, 5.0 };

            var variableDefs = new[]
                {
                    new VariableDef(4, ValueNotUsed),
                    new VariableDef(6, ValueNotUsed),
                    new VariableDef(9, ValueNotUsed),
                    new VariableDef(2, ValueNotUsed),
                    new VariableDef(5, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], -3),
                    new ConstraintDef(variableDefs[0], variableDefs[3], -3),
                    new ConstraintDef(variableDefs[1], variableDefs[4], -3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], -3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], -3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], -3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test with equal weights")]
        public void Test1_Weight4()
        {
            // Results should be the same as Test1 because the weights are all equal.
            var expectedPositionsX = new[] { 1.4, 4.4, 7.4, 7.4, 10.4 };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1, unlike above tests.
                    new VariableDef(0, 2.0, ValueNotUsed, 4.0),
                    new VariableDef(1, 9.0, ValueNotUsed, 4.0),
                    new VariableDef(2, 9.0, ValueNotUsed, 4.0),
                    new VariableDef(3, 9.0, ValueNotUsed, 4.0),
                    new VariableDef(4, 2.0, ValueNotUsed, 4.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test with varying small weights")]
        public void Test1_Weight4_2()
        {
            var expectedPositionsX = new[] { 2.0, 5.33333, 8.33333, 8.33333, 11.33333 };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1, unlike above tests.
                    new VariableDef(0, 2.0, ValueNotUsed, 4.0),
                    new VariableDef(1, 9.0, ValueNotUsed, 4.0),
                    new VariableDef(2, 9.0, ValueNotUsed, 4.0),
                    new VariableDef(3, 9.0, ValueNotUsed, 2.0),
                    new VariableDef(4, 2.0, ValueNotUsed, 2.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test with equality constraints that push apart variables")]
        public void Test1_Equality_Push()
        {
            var expectedPositionsX = new[] { 0.40, 3.40, 6.40, 16.40, 19.40 };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1.  Set initial positions close to "push"
                    // the Equality variables apart to the same result as _Pull.
                    new VariableDef(0, 2.0, ValueNotUsed, 1.0),
                    new VariableDef(1, 9.0, ValueNotUsed, 1.0),
                    new VariableDef(2, 16.50, ValueNotUsed, 1.0),
                    new VariableDef(3, 16.50, ValueNotUsed, 1.0),
                    new VariableDef(4, 2.0, ValueNotUsed, 1.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 10, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test with equality constraints that pull together variables")]
        public void Test1_Equality_Pull()
        {
            var expectedPositionsX = new[] { 0.40, 3.40, 6.40, 16.40, 19.40 };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1.  Set initial positions close to "pull" 
                    // the Equality variables together to the same result as _Push.
                    new VariableDef(0, 2.0, ValueNotUsed, 1.0),
                    new VariableDef(1, 9.0, ValueNotUsed, 1.0),
                    new VariableDef(2, 9.0, ValueNotUsed, 1.0),
                    new VariableDef(3, 24.0, ValueNotUsed, 1.0),
                    new VariableDef(4, 2.0, ValueNotUsed, 1.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 10, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of unsatisfiable inequality constraints")]
        public void Test_Unsatisfiable_Direct_Inequality()
        {
            //      var0 + 2 <= var1
            //      var1 + 3 <= var0
            // Contradictory.

            // Expected positions reflect the failure due to unsatisfiable constraints.
            // Note that it is sensitive to evaluation order as to which constraints are satisfied
            // and which are marked IsUnsatisfiable.
            var expectedPositionsX = new[] { 3.0000, 0.0000 };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1.
                    new VariableDef(0, 1.0, ValueNotUsed, 1.0),
                    new VariableDef(1, 2.0, ValueNotUsed, 1.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 2.0, false /* fIsEquality */),
                    new ConstraintDef(variableDefs[1], variableDefs[0], 3.0, false /* fIsEquality */)
                };
            ExpectedUnsatisfiedConstraintCount = 1;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of unsatisfiable equality and inequality constraints")]
        public void Test_Equality_Unsatisfiable_Inequality()
        {
            //      var0 + 0 == var1
            //      var0 + 4 <= var1
            // Contradictory.

            // Expected positions reflect the failure due to unsatisfiable constraints.
            var expectedPositionsX = new[] { 1.50000, 1.50000 };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1.
                    new VariableDef(0, 1.0, ValueNotUsed, 1.0),
                    new VariableDef(1, 2.0, ValueNotUsed, 1.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 0, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[0], variableDefs[1], 4, false /* fIsEquality */)
                };
            ExpectedUnsatisfiedConstraintCount = 1;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of unsatisfiable direct cycle with equality constraints")]
        public void Test_Equality_Unsatisfiable_Cycle_Direct()
        {
            //      var0 + 4 == var1
            //      var1 + 4 == var2
            //      var0 + 10 == var2
            // The first and second combine to conflict with the third.

            // Expected positions reflect the failure due to unsatisfiable constraints.
            var expectedPositionsX = new[] { -2.00000, 2.00000, 6.00000 };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1.
                    new VariableDef(0, 1.0, ValueNotUsed, 1.0),
                    new VariableDef(1, 2.0, ValueNotUsed, 1.0),
                    new VariableDef(2, 3.0, ValueNotUsed, 1.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[0], variableDefs[2], 10, true /* fIsEquality */)
                };
            ExpectedUnsatisfiedConstraintCount = 1;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of unsatisfiable indirect cycle with equality constraints")]
        public void Test_Equality_Unsatisfiable_Cycle_Indirect1()
        {
            //      0 == 3
            //      1 == 5
            //      1 <= 6
            //      2 <= 3 <= 4 <= 5
            //      2 == 4
            // This is a short-circuit by 2 == 4 of the 2 <= .. <= 5 path.

            // Expected positions reflect the failure due to unsatisfiable constraints.
            var expectedPositionsX = new[] 
            {
                -0.94059,
                5.05941,
                0.05941,
                3.05941,
                6.05941,
                9.05941,
                4.05941
            };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1.
                    new VariableDef(0, 4.32277, ValueNotUsed, 1.0),
                    new VariableDef(1, 0.35485, ValueNotUsed, 1.0),
                    new VariableDef(2, 5.92211, ValueNotUsed, 1.0),
                    new VariableDef(3, 4.64681, ValueNotUsed, 1.0),
                    new VariableDef(4, 1.74243, ValueNotUsed, 1.0),
                    new VariableDef(5, 7.65862, ValueNotUsed, 1.0),
                    new VariableDef(6, 1.76830, ValueNotUsed, 1.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[3], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[1], variableDefs[5], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[1], variableDefs[6], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[6], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[4], variableDefs[5], 3)
                };
            ExpectedUnsatisfiedConstraintCount = 1;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of unsatisfiable indirect cycle with equality constraints")]
        public void Test_Equality_Unsatisfiable_Cycle_Indirect2()
        {
            //      0 <= 2 <= 3 <= 5
            //      0 == 4
            //      1 <= 4
            //      1 == [..] 3
            // The constraints on var1 cause var3 to move left, causing var0 to move left,
            // causing var 4 to move left, causing var1 to move left, causing var3 to move left...

            // Expected positions reflect the failure due to unsatisfiable constraints.
            var expectedPositionsX = new[] 
            { 
                -1.09371,
                0.90629,
                1.90629,
                4.90629,
                2.90629,
                7.90629
            };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1.
                    new VariableDef(0, 6.96836, ValueNotUsed, 1.0),
                    new VariableDef(1, 0.40162, ValueNotUsed, 1.0),
                    new VariableDef(2, 1.57679, ValueNotUsed, 1.0),
                    new VariableDef(3, 0.65356, ValueNotUsed, 1.0),
                    new VariableDef(4, 1.49899, ValueNotUsed, 1.0),
                    new VariableDef(5, 6.33842, ValueNotUsed, 1.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[4], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[1], variableDefs[3], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[5], 3)
                };
            ExpectedUnsatisfiedConstraintCount = 1;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of unsatisfiable indirect cycle with equality constraints")]
        public void Test_Equality_Unsatisfiable_Cycle_Indirect3()
        {
            //      0 == 2
            //      0 <= 3 <= 4
            //      1 == 4
            //      1 <= 2
            // The constraints on var1 cause var4 to move left which causes var0 to move left
            // which causes var2 to move left which causes var1 to move left... This adds the
            // tricky aspect of having the direct equality constraints *before* the transitionals.

            // Expected positions reflect the failure due to unsatisfiable constraints.
            var expectedPositionsX = new[]
            {
                -0.71841,
                0.28159,
                3.28159,
                2.28159,
                4.28159 
            };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1.
                    new VariableDef(0, 0.40150, ValueNotUsed, 1.0),
                    new VariableDef(1, 4.98420, ValueNotUsed, 1.0),
                    new VariableDef(2, 2.17846, ValueNotUsed, 1.0),
                    new VariableDef(3, 0.66815, ValueNotUsed, 1.0),
                    new VariableDef(4, 1.17563, ValueNotUsed, 1.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            ExpectedUnsatisfiedConstraintCount = 1;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of multiple unsatisfiable indirect cycle with equality constraints")]
        public void Test_Equality_Unsatisfiable_Cycle_Indirect_Multiple()
        {
            // This combines _Direct and _Indirect3 above to test handling of multiple cycles.

            // Expected positions reflect the failure due to unsatisfiable constraints.
            var expectedPositionsX = new[]
            {
                1.48523,
                4.48523,
                7.48523,
                5.48523,
                -0.71841,
                0.28159,
                3.28159,
                2.28159,
                4.28159
            };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1.
                    new VariableDef(0, 7.76463, ValueNotUsed, 1.0),
                    new VariableDef(1, 0.12174, ValueNotUsed, 1.0),
                    new VariableDef(2, 3.08792, ValueNotUsed, 1.0),
                    new VariableDef(3, 7.96664, ValueNotUsed, 1.0),
                    new VariableDef(4, 0.40150, ValueNotUsed, 1.0),
                    new VariableDef(5, 4.98420, ValueNotUsed, 1.0),
                    new VariableDef(6, 2.17846, ValueNotUsed, 1.0),
                    new VariableDef(7, 0.66815, ValueNotUsed, 1.0),
                    new VariableDef(8, 1.17563, ValueNotUsed, 1.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[4], variableDefs[6], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[4], variableDefs[7], 3),
                    new ConstraintDef(variableDefs[5], variableDefs[8], 4, true /* fIsEquality */),
                    new ConstraintDef(variableDefs[5], variableDefs[6], 3),
                    new ConstraintDef(variableDefs[7], variableDefs[8], 3)
                };
            ExpectedUnsatisfiedConstraintCount = 2;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of unsatisfiable indirect cycle with a leaf variable")]
        public void Test_Unsatisfiable_Cycle_Indirect_With_SingleConstraint_Var()
        {
            //      var0 + 4 <= var1
            //      var1 + 4 <= var2
            //      var2 + 4 <= var1
            // The second and third conflict, but the cycle was not detected due to the perf optimization
            // to skip stack-push for single-constraint vars in ComputeDfDv and RecurseGetConnectedVariables.

            // Expected positions reflect the failure due to unsatisfiable constraints.
            var expectedPositionsX = new[]
            {
                0.66667,
                4.66667,
                0.66667 
            };
            var variableDefs = new[]
                {
                    // Note: ordinals here in column 1.
                    new VariableDef(0, 1.0, ValueNotUsed, 1.0),
                    new VariableDef(1, 2.0, ValueNotUsed, 1.0),
                    new VariableDef(2, 3.0, ValueNotUsed, 1.0)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[1], 4, false /* fIsEquality */),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 4, false /* fIsEquality */),
                    new ConstraintDef(variableDefs[2], variableDefs[1], 4, false /* fIsEquality */)
                };
            ExpectedUnsatisfiedConstraintCount = 1;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Re"), TestMethod]
        [Timeout(2000)]
        [Description("Test regapping of constraints with equality constraints in a non-overlapping 3-segment line")]
        public void Test_Equality_3Segments_ReGap()
        {
            //  a + 4 == b  (first segment)
            //  c + 4 == d  (second segment)
            //  b + 2 == c  (third segment joins the first two)
            var expectedPositionsX = new[]
            { 
                -2.0,
                2.0,
                4.0,
                8.0
            };
            var variableDefs = new[]
            {
                // Note: ordinals here in column 1.
                new VariableDef(0, 3, ValueNotUsed, 1.0),
                new VariableDef(1, 3, ValueNotUsed, 1.0),
                new VariableDef(2, 3, ValueNotUsed, 1.0),
                new VariableDef(4, 3, ValueNotUsed, 1.0)
            };
            var constraintDefs = new[]
            {
                new ConstraintDef(variableDefs[0], variableDefs[1], 4, true /* fIsEquality */),
                new ConstraintDef(variableDefs[2], variableDefs[3], 4, true /* fIsEquality */),
                new ConstraintDef(variableDefs[1], variableDefs[2], 2, true /* fIsEquality */)
            };
            GoalFunctionValueX = 16;
            ExpectedUnsatisfiedConstraintCount = 0;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);

            // Re-gap; because there is no overlap we only have the success mode here.
            expectedPositionsX = new[]
            { 
                -7.0,
                1.0,
                5.0,
                13.0
            };
            GoalFunctionValueX = 172;
            ExpectedUnsatisfiedConstraintCount = 0;
            this.SolverX.SetConstraintUpdate(constraintDefs[0].Constraint, 8.0);
            this.SolverX.SetConstraintUpdate(constraintDefs[1].Constraint, 8.0);
            this.SolverX.SetConstraintUpdate(constraintDefs[2].Constraint, 4.0);
            Validate.IsTrue(
                    SolveRegap("expected all satisfied", variableDefs, constraintDefs, expectedPositionsX),
                    ReGapFailureString);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Re"), TestMethod]
        [Timeout(2000)]
        [Description("Test regapping of constraints with equality constraints where the third segment overlaps the first two")]
        public void Test_Equality_3Segments_Overlap_ReGap()
        {
            //  a + 4 == b  (first segment)
            //  b + 4 == c  (second segment)
            //  a + 8 == c  (both segments)
            var expectedPositionsX = new[]
            {
                -1.0,
                3.0,
                7.0
            };
            var variableDefs = new[]
            {
                // Note: ordinals here in column 1.
                new VariableDef(0, 3, ValueNotUsed, 1.0),
                new VariableDef(1, 3, ValueNotUsed, 1.0),
                new VariableDef(2, 3, ValueNotUsed, 1.0)
            };
            var constraintDefs = new[]
            {
                new ConstraintDef(variableDefs[0], variableDefs[1], 4, true /* fIsEquality */),
                new ConstraintDef(variableDefs[1], variableDefs[2], 4, true /* fIsEquality */),
                new ConstraintDef(variableDefs[0], variableDefs[2], 8, true /* fIsEquality */)
            };
            GoalFunctionValueX = 5;
            ExpectedUnsatisfiedConstraintCount = 0;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);

            // Re-gap such that the overlap is satisfied.
            expectedPositionsX = new[]
            { 
                -5.0,
                3.0,
                11.0
            };
            GoalFunctionValueX = 101;
            ExpectedUnsatisfiedConstraintCount = 0;
            this.SolverX.SetConstraintUpdate(constraintDefs[0].Constraint, 8.0);
            this.SolverX.SetConstraintUpdate(constraintDefs[1].Constraint, 8.0);
            this.SolverX.SetConstraintUpdate(constraintDefs[2].Constraint, 16.0);
            Validate.IsTrue(
                    SolveRegap("expected all satisfied", variableDefs, constraintDefs, expectedPositionsX),
                    ReGapFailureString);

            // Re-gap such that the overlap cannot be satisfied.
            expectedPositionsX = new[]
            { 
                -5.0,
                3.0,
                11.0
            };
            GoalFunctionValueX = 101;
            ExpectedUnsatisfiedConstraintCount = 1;
            this.SolverX.SetConstraintUpdate(constraintDefs[0].Constraint, 8.0);
            this.SolverX.SetConstraintUpdate(constraintDefs[1].Constraint, 8.0);
            this.SolverX.SetConstraintUpdate(constraintDefs[2].Constraint, 12.0);
            Validate.IsTrue(
                    SolveRegap("expected overlapping constraint unsatisfied", variableDefs, constraintDefs, expectedPositionsX),
                    ReGapFailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple solver test with neighbors")]
        public void Test2_Neighbors()
        {
            //Non-neighbors Test2 :         { 0.50000, 6.00000, 3.50000, 6.50000, 9.50000 };
            var expectedPositionsX = new[] { 0.40385, 6.38462, 3.40385, 6.40385, 9.40385 };
            var variableDefs = new[]
                {
                    new VariableDef(4, ValueNotUsed),
                    new VariableDef(6, ValueNotUsed),
                    new VariableDef(9, ValueNotUsed),
                    new VariableDef(2, ValueNotUsed),
                    new VariableDef(5, ValueNotUsed)
                };
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[0], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3)
                };
            var neighborDefs = new[]
                {
                    new NeighborDef(variableDefs[0], variableDefs[4], 10.0),
                    new NeighborDef(variableDefs[1], variableDefs[3], 20.0)
                };

            // TODOqpsc TODOdoc: update this, perhaps with ref to the Mathematica workbook.
            // Expected A matrix with foregoing neighbors (digits are var ordinals):
            //          0       1       2       3       4
            //          ----    ----    ----    ----    ----
            //  0       w0+w04                          -w04
            //  1               w1+w13          -w13
            //  2                       w2
            //  3               -w13            w3+w13
            //  4       -w04                            w4+w04
            //
            // Translating to numbers based upon the above:
            //          0       1       2       3       4
            //          ----    ----    ----    ----    ----
            //  0       11                              -10
            //  1               21              -20
            //  2                       1
            //  3               -20             21
            //  4       -10                             11
            //
            // And vecWiDi (row-wise to save space):
            //          0       1       2       3       4
            //          ----    ----    ----    ----    ----
            //          4       6       9       2       5
            //
            // So the first gradient vector g=Ax+b (actually minus b):
            //          0           1           2           3           4
            //          ----        ----        ----        ----        ----
            //          44-50       126-40      9           -120+42     -40+55
            //  Ax =    -6          86          9           -78         15
            //  g=Ax-b= -10         80          0           -80         10
            //  g'g =   100         6400        0           6400        100
            //    (sum) = 13000
            //  So the first Ag is:
            //          -110-100    1806+1600   0           -1600-1680  100+110
            //  =       -210        3280        0           -3280       210
            //  g'Ag =  2100        262400      0           262400      2100
            //    (sum) = 529000
            //  s = 13000/529000 = 0.025

            GoalFunctionValueX = 731.192307692308;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, neighborDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test proper handling of a zero gradient vector")]
        public void Test_Qpsc_Zero_Gradient()
        {
            // This tests proper handling of a g'g/g'Ag when g is all 0.
            var expectedPositionsX = new[] { 241.27033, 250.81100, 102.68749 };
            var variableDefs = new[]
                {
                    new VariableDef(236.5, ValueNotUsed, 2.0),
                    new VariableDef(255.58133348304591, ValueNotUsed, 2.0),
                    new VariableDef(102.68749237060547, ValueNotUsed, 1e8)
                };
            var constraintDefs = new[] { new ConstraintDef(variableDefs[2], variableDefs[0], 1.0) };
            var neighborDefs = new[] { new NeighborDef(variableDefs[0], variableDefs[1], 1.0) };

            GoalFunctionValueX = -1054472351262.4;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, neighborDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Minimal test of proper handling of zero-width constraint")]
        public void Test_Qpsc_Zero_Gap()
        {
            // This tests proper handling of zero gap.
            var expectedPositionsX = new[] { 4.00000, 4.00000 };
            var variableDefs = new[]
            {
                new VariableDef(3, ValueNotUsed),
                new VariableDef(5, ValueNotUsed) 
            };
            
            // Rhs starts out two to the left of Lhs so they split the difference.
            var constraintDefs = new[] { new ConstraintDef(variableDefs[1], variableDefs[0], 0.0) };

            GoalFunctionValueX = -32;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, null /*rgNeighborDefs */, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Ignore] // TODOunit: scaling apparently required; the high weight of var 2 causes it to swamp the stepsize
        [Timeout(2000)]
        [Description("Test of zero-width constraint with a heavy neighbor")]
        public void Test_Qpsc_Zero_Gap_Heavy_Neighbor()
        {
            // This tests two nodes with constraints satisfying both those constraints and a
            // neighbor-pair minimization that influences the direction of constraint-satisfying movement:
            //    [0] <gap==0> [1] <==pair==> [2]
            // should result in moving var 0 to the right to satisfy the required var1+0<=var3 gap:
            //                 [01]           [2]
            // because of the high weight of var2, which should make it resistant to movement
            // and thus cause the movement of vars 0 and 1 to be toward 1 to satisfy minimal
            // distance between vars 1 and 2, and because of the high weight of var2, both vars 0 and 1
            // actually move past var1's initial "desired pos" due to the rubber-band effect.
            // calculations and thus fails to pass; requires scaling.
            // TODOunit:  Create more heavyweight tests derived from Rectilinear nudging
            var expectedPositionsX = new[] { 5.33333, 5.33333, 8.00000 };
            var variableDefs = new[]
                {
                    new VariableDef(3, ValueNotUsed),
                    new VariableDef(5, ValueNotUsed),
                    new VariableDef(8, ValueNotUsed, 1e8)
                };
            
            // Rhs starts out two to the left of Lhs but the minimization of distance with the
            // fixed neighbor causes the lhs var to move all the way instead of splitting the difference.
            var constraintDefs = new[] { new ConstraintDef(variableDefs[1], variableDefs[0], 0.0) };
            var neighborDefs = new[] { new NeighborDef(variableDefs[2], variableDefs[1], 1.0) };

            GoalFunctionValueX = 42;    // placeholder value; fill in when activated via -verbose 1
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, neighborDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Ignore] // TODOunit: scaling apparently required, for the same reason as Test_QPSC_Zero_Gap_Heavy_Neighbor
        [Timeout(2000)]
        [Description("Test of nonzero-width constraint with a heavy neighbor")]
        public void Test_Qpsc_Four_Gap_Heavy_Neighbor()
        {
            // A variation of Test_QPSC_Zero_Gap_Heavy_Neighbor, where the movement of the constrained pair
            // is influenced by a minimal-distance goal between the left-hand side and a heavyweight
            // node to the right:
            //    [0] <gap==4> [1] <==pair==> [2]
            // should result in moving var 1 to the right to satisfy the required var0+4<=var1 gap:
            //    [0]                         [12]
            // Because of the high weight of var2, vars 0 and 1 move past var1's initial ideal position.
            var expectedPositionsX = new[] { 4.00000, 8.00000, 8.00000 };
            var variableDefs = new[]
                {
                    new VariableDef(3, ValueNotUsed),
                    new VariableDef(5, ValueNotUsed),
                    new VariableDef(8, ValueNotUsed, 1e8)
                };

            // Rhs starts out two to the left of Lhs but the minimization of distance with the
            // fixed neighbor causes the lhs var to move all the way instead of splitting the difference.
            var constraintDefs = new[] { new ConstraintDef(variableDefs[0], variableDefs[1], 4.0) };
            var neighborDefs = new[] { new NeighborDef(variableDefs[2], variableDefs[0], 1.0) };

            GoalFunctionValueX = 42;    // placeholder value; fill in when activated via -verbose 1
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, neighborDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of neighbors with no constraints")]
        public void Test_Scale5()
        {
            var expectedPositionsX = new[]
            {
                5.512113055, 
                1.402422611,
                3.506056528,
                8.512113055,
                10.01211306
            };
            var variableDefs = new[]
            {
                new VariableDef(8, ValueNotUsed, 10) { ScaleX = 2 },
                new VariableDef(9, ValueNotUsed, 1) { ScaleX = 10 },
                new VariableDef(4, ValueNotUsed, 9) { ScaleX = 4 },

                new VariableDef(7, ValueNotUsed, 7) { ScaleX = 2 },
                new VariableDef(4, ValueNotUsed, 3) { ScaleX = 2 }
            };
            var constraintDefs = new[]
            {
                new ConstraintDef(variableDefs[0], variableDefs[2], 3),
                new ConstraintDef(variableDefs[2], variableDefs[3], 3),
                new ConstraintDef(variableDefs[1], variableDefs[3], 3),
                new ConstraintDef(variableDefs[3], variableDefs[4], 3)
            };

            // See comments in Test_ProjectionSolver.Test2_Nbours
            Validate.IsTrue(
                CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of neighbors with weight 100k, with no constraints and variable weights differing by 1e14")]
        public void Test_Qpsc_No_Constraints_Weight_Difference_1E14()
        {
            // Test that Qpsc with only neighbour relationships, no constraints, correctly repositions
            // the nodes.  This is different from the Fail_QPSC tests above, because those initialize
            // Qpsc with the solved positions as desired positions.  Note:  This is in the MSAGL enlistment
            // TestSolverShell as Test_dummy_ideal_position.
            // TODOtest:  Get expected goal function values for all tests.

            // Note: Scaling has much greater accuracy here so the expected positions reflect that
            // there is no (perceptible in our range) movement.
            var expectedPositionsX = new[]
            { 
                500.00000,          // was 499.97502
                500.00000           // was 499.99999
            };

            var variableDefs = new[]
            {
                new VariableDef(10.0, ValueNotUsed, 0.000001),
                new VariableDef(500, ValueNotUsed, 1e8)
            };
            var neighborDefs = new[]
            {
                new NeighborDef(variableDefs[0], variableDefs[1], 100000.0)
            };

            // Note:  With scaling and such a wide weight variation we don't need to reset parameters
            // (see Test_QPSC_no_constraints_weight_difference_1e4 for a case where we do).

            GoalFunctionValueX = -24999999999999.8;
            Validate.IsTrue(
                    CheckResult(variableDefs, new ConstraintDef[0], neighborDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);

            // This allows inspecting the actual values to examine at greater precision if desired.
            //System.Diagnostics.Debug.WriteLine("Full Pos: var0 {0}, var1 {1}", rgVariableDefs[0].ActualPosX, rgVariableDefs[1].ActualPosX);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of neighbors with weight 100k, with no constraints and variable weights differing by 1e4")]
        public void Test_Qpsc_No_Constraints_Weight_Difference_1E4()
        {
            // Note: This test was added for visibility of changes with scaling, which has greater accuracy,
            // so the range of weights has been reduced and the expected positions modified for this version.
            // Expected values are from SolverFoundation.
            var expectedPositionsX = new[] { 499.95096, 499.95100 };
            var variableDefs = new[]
            {
                new VariableDef(10.0, ValueNotUsed, 0.01),
                new VariableDef(500, ValueNotUsed, 1e2)
            };
            var neighborDefs = new[] 
            {
                new NeighborDef(variableDefs[0], variableDefs[1], 100000.0)
            };

            // Set up parameters.  Without these, the movement both variables stops around 142.8.
            // Dropping the QpscConvergence values further has no effect.
            TestGlobals.InitialSolverParameters.OuterProjectIterationsLimit = 0;
            TestGlobals.InitialSolverParameters.QpscConvergenceQuotient = 1e-16;
            TestGlobals.InitialSolverParameters.QpscConvergenceEpsilon = 1e-16;

            GoalFunctionValueX = -24997600.2403185;
            Validate.IsTrue(
                CheckResult(variableDefs, new ConstraintDef[0], neighborDefs, expectedPositionsX, true /* fCheckResults */),
                FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of neighbors with weight 1.0, with no constraints and variable weights differing by 1e14")]
        public void Test_Qpsc_No_Constraints_Weight_Difference_1E4_No_Neighbor_Weight()
        {
            // Note: This test was added for visibility of changes with scaling, which has greater accuracy,
            // so the range of weights has been reduced and the expected positions modified for this version.
            var expectedPositionsX = new[] { 495.10048, 499.95149 };
            var variableDefs = new[]
            {
                new VariableDef(10.0, ValueNotUsed, 0.01),
                new VariableDef(500, ValueNotUsed, 1e2)
            };
            var neighborDefs = new[]
            {
                new NeighborDef(variableDefs[0], variableDefs[1], 1.0)
            };

            GoalFunctionValueX = -24997624.007623;
            Validate.IsTrue(
                CheckResult(variableDefs, new ConstraintDef[0], neighborDefs, expectedPositionsX, true /* fCheckResults */),
                FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Move light and heavy variables to segment ends by pushing them out from the middle via constraints")]
        public void Test_MoveMidVarsToSegmentEnds_PushOut()
        {
            var solver = new Solver();
            const double FixedVarWeight = 1000000;
            var heavy0 = solver.AddVariable(null, 0, FixedVarWeight);
            var heavy1 = solver.AddVariable(null, 2, FixedVarWeight);
            var light0 = solver.AddVariable(null, 1);
            var light1 = solver.AddVariable(null, 1);
            solver.AddConstraint(heavy0, light0, 0);
            solver.AddConstraint(heavy0, light1, 0);
            solver.AddConstraint(light0, heavy1, 0);
            solver.AddConstraint(light1, heavy1, 0);
            solver.AddConstraint(light0, light1, 100);
            solver.Solve();

            // expected values
            const double Heavy0Expected = -49.0;
            const double Light0Expected = -49.0;
            const double Light1Expected = 51.0;
            const double Heavy1Expected = 51.0;
            if (!ApproxEquals(heavy0.ActualPos, Heavy0Expected) || !ApproxEquals(light0.ActualPos, Light0Expected)
                || !ApproxEquals(light1.ActualPos, Light1Expected) || !ApproxEquals(heavy1.ActualPos, Heavy1Expected))
            {
                if (TestGlobals.VerboseLevel > 0)
                {
                    WriteLine("Failed - actual/expected: h0={0}/{1} l0={2}/{3} l1={4}/{5} h1={6}/{7}",
                            heavy0.ActualPos,
                            Heavy0Expected,
                            light0.ActualPos,
                            Light0Expected,
                            light1.ActualPos,
                            Light1Expected,
                            heavy1.ActualPos,
                            Heavy1Expected);
                }
                Validate.Fail("Results were not as expected");
            }
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Move light and heavy variables to segment ends by pushing them inward via constraints and a neighbor minimal-distance specification")]
        public void Test_MoveMidVarsToSegmentEnds_PushIn()
        {
            var solver = new Solver();
            const double FixedVarWeight = 1e8;
            var heavy0 = solver.AddVariable(null, 0, FixedVarWeight);
            var heavy1 = solver.AddVariable(null, 2, FixedVarWeight);
            var light0 = solver.AddVariable(null, 1);
            var light1 = solver.AddVariable(null, 1);
            solver.AddConstraint(light0, heavy0, 0);
            solver.AddConstraint(heavy1, light1, 0);
            solver.AddNeighborPair(light0, light1, 1 / FixedVarWeight);
            solver.Solve();

            // expected values
            const double Heavy0Expected = 1.00000001E-08;
            const double Light0Expected = 1.00000001E-08;
            const double Light1Expected = 1.99999999;
            const double Heavy1Expected = 1.99999999;
            if (!ApproxEquals(heavy0.ActualPos, Heavy0Expected) || !ApproxEquals(light0.ActualPos, Light0Expected)
                || !ApproxEquals(light1.ActualPos, Light1Expected) || !ApproxEquals(heavy1.ActualPos, Heavy1Expected))
            {
                if (TestGlobals.VerboseLevel > 0)
                {
                    WriteLine("Failed - actual/expected: h0={0}/{1} l0={2}/{3} l1={4}/{5} h1={6}/{7}",
                            heavy0.ActualPos,
                            Heavy0Expected,
                            light0.ActualPos,
                            Light0Expected,
                            light1.ActualPos,
                            Light1Expected,
                            heavy1.ActualPos,
                            Heavy1Expected);
                }
                Validate.Fail("Results were not as expected");
            }
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Start all variables at zero, then move them apart via constraints in a single chain.")]
        public void Test_StartAtZero100()
        {
            StartAtZeroWorker(100, 1);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Start all variables at zero, then move them apart via constraints in 100 chains.")]
        public void Test_StartAtZero100X100()
        {
            StartAtZeroWorker(100, 100);
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Start all variables at zero, then move them apart via constraints in 1000 chains.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void StartAtZero1000X100()
        {
            StartAtZeroWorker(1000, 100);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Start all variables at zero, then move them apart via constraints in 1000 short chains.")]
        public void Test_StartAtZero1000X10()
        {
            StartAtZeroWorker(1000, 10);
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Start all variables at zero, then move them apart via constraints in a single very long chain.")]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        public void StartAtZero10000()
        {
            StartAtZeroWorker(10000, 1);
        }

        internal void StartAtZeroWorker(int numVarsPerBlock, int numBlocks)
        {
            var expectedPositionsXNotUsed = new double[numVarsPerBlock * numBlocks];
            var variableDefs = new VariableDef[numVarsPerBlock * numBlocks];
            var constraintDefs = new ConstraintDef[(numVarsPerBlock - 1) * numBlocks];

            for (int idxBlock = 0; idxBlock < numBlocks; ++idxBlock)
            {
                int varOffset = numVarsPerBlock * idxBlock;
                int cstOffset = (numVarsPerBlock - 1) * idxBlock;
                for (int idxVar = 0; idxVar < numVarsPerBlock; ++idxVar)
                {
                    int varIndex = varOffset + idxVar;
                    variableDefs[varIndex] = new VariableDef(0.0, ValueNotUsed);
                    if (idxVar > 0)
                    {
                        constraintDefs[cstOffset + idxVar - 1] = new ConstraintDef(
                            variableDefs[varIndex - 1], variableDefs[varIndex], 1.0);
                    }
                }
            }
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsXNotUsed, false /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test a specific constraint tree formation.")]
        public void Test_Tree()
        {
            var expectedPositionsX = new[] { -3.50000, -3.50000, -0.50000, -0.50000, 2.50000, 5.50000 };
            var variableDefs = new[]
                {
                    new VariableDef(0.0, ValueNotUsed),
                    new VariableDef(0.0, ValueNotUsed),
                    new VariableDef(0.0, ValueNotUsed),
                    new VariableDef(0.0, ValueNotUsed),
                    new VariableDef(0.0, ValueNotUsed),
                    new VariableDef(0.0, ValueNotUsed)
                };

            // 0 --> 2 -->  4 --> 5
            // 1 ----^      ^
            //       3 -----|
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[5], 3),
                    new ConstraintDef(variableDefs[4], variableDefs[5], 3)
                };
            GoalFunctionValueX = 61.5;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test a specific constraint tree formation.")]
        public void Test_Tree2()
        {
            var expectedPositionsX = new[] { -2.50000, -2.50000, 0.50000, -5.50000, 3.50000, 6.50000 };
            var variableDefs = new[]
                {
                    new VariableDef(0.0, ValueNotUsed),
                    new VariableDef(0.0, ValueNotUsed),
                    new VariableDef(0.0, ValueNotUsed),
                    new VariableDef(0.0, ValueNotUsed),
                    new VariableDef(0.0, ValueNotUsed),
                    new VariableDef(0.0, ValueNotUsed)
                };

            // 3 -- > 0 --> 2 -->  4 --> 5
            // ^ -- > 1 ----^
            var constraintDefs = new[]
                {
                    new ConstraintDef(variableDefs[0], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[1], variableDefs[2], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[4], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[0], 3),
                    new ConstraintDef(variableDefs[3], variableDefs[1], 3),
                    new ConstraintDef(variableDefs[2], variableDefs[5], 3),
                    new ConstraintDef(variableDefs[4], variableDefs[5], 3)
                };
            GoalFunctionValueX = 97.5;
            Validate.IsTrue(
                    CheckResult(variableDefs, constraintDefs, expectedPositionsX, true /* fCheckResults */),
                    FailureString);
        }
    }
}
