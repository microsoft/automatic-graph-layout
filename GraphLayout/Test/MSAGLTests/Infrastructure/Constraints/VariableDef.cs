// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VariableDef.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    // Class for testing variable definitions.  We can have two dimensions, X and Y, for
    // OverlapRemoval, but only one for ProjectionSolver, so we use X for that.
    internal class VariableDef : IPositionInfo
    {
        internal uint Ordinal { get; private set; }
        internal string IdString
        {
            get { return this.Ordinal.ToString(); }
        }

        private readonly double desiredPosX, desiredPosY;

        private readonly double sizeX, sizeY; // Only used for ProjectionSolver
        internal double WeightX { get; private set; }
        internal double WeightY { get; private set; }

        internal double ExpectedResultPosX { get; private set; }
        internal double ExpectedResultPosY { get; private set; }

        internal ITestVariable VariableX { get; set; }
        internal ITestVariable VariableY { get; set; }
        internal bool IsInEqualityConstraint { get; set; }

        // Add Scaling for ProjectionSolver only (so no ScaleY).
        internal double ScaleX { get; set; }

        internal double ActualPosX { get { return VariableX.ActualPos * ScaleX; } }
        internal double ActualPosY { get { return VariableY.ActualPos; } }

        // Nodes have potentially multiple ParentClusters (one per hierarchy).
        internal List<ClusterDef> ParentClusters { get; set; }

        // List of left constraints (to clean up cycles in Equality constraints, mostly).
        internal List<ConstraintDef> LeftConstraints { get; set; }

        ////
        //// These work with the local Test*() routines...
        ////
        public VariableDef(double desiredPosX, double sizeX)
            : this(desiredPosX, TestGlobals.IgnorePosition,     // initial Desired pos
                    sizeX, TestGlobals.IgnorePosition,          // Size
                    1.0, TestGlobals.IgnorePosition)            // Weight
            { }
        public VariableDef(double desiredPosX, double sizeX, double weightX)
            : this(desiredPosX, TestGlobals.IgnorePosition,     // initial Desired pos
                    sizeX, TestGlobals.IgnorePosition,          // Size
                    weightX, TestGlobals.IgnorePosition)        // Weight
            { }
        public VariableDef(double desiredPosX, double desiredPosY, double sizeX, double sizeY)
            : this(desiredPosX, desiredPosY,                    // initial Desired pos
                    sizeX, sizeY,                               // Size
                    1.0, 1.0)                                   // Weight
            { }

        public VariableDef(double desiredPosX, double desiredPosY,
                double sizeX, double sizeY,
                double weightX, double weightY)
        {
            this.desiredPosX = desiredPosX;
            this.desiredPosY = desiredPosY;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.WeightX = weightX;
            this.WeightY = weightY;
            this.ScaleX = 1.0;
            ParentClusters = new List<ClusterDef>();
            LeftConstraints = new List<ConstraintDef>();
        }
        public void SetExpected(uint id, double expectedX)
        {
            SetExpected(id, expectedX, TestGlobals.IgnorePosition);
        }
        public void SetExpected(uint id, double expectedX, double expectedY)
        {
            this.Ordinal = id;
            this.ExpectedResultPosX = expectedX;
            this.ExpectedResultPosY = expectedY;
        }

        ////
        //// ... and these work with the DataFile format.
        ////
        public VariableDef(uint ordinal,
                        double desiredPosX,
                        double sizeX,
                        double weightX)
            : this(ordinal,
                    desiredPosX, TestGlobals.IgnorePosition,
                    sizeX, TestGlobals.IgnorePosition,
                    weightX, TestGlobals.IgnorePosition) { }
        public VariableDef(uint ordinal,
                        double desiredPosX, double desiredPosY,
                        double sizeX, double sizeY,
                        double weightX, double weightY)
        {
            this.Ordinal = ordinal;
            this.desiredPosX = desiredPosX;
            this.desiredPosY = desiredPosY;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.WeightX = weightX;
            this.WeightY = weightY;
            this.ScaleX = 1.0;
            ParentClusters = new List<ClusterDef>();
            LeftConstraints = new List<ConstraintDef>();
        }
        public void SetExpected(double dblExpectedX)
        {
            SetExpected(dblExpectedX, TestGlobals.IgnorePosition);
        }
        public void SetExpected(double dblExpectedX, double expectedY)
        {
            this.ExpectedResultPosX = dblExpectedX;
            this.ExpectedResultPosY = expectedY;
        }

        private static double MaxAllowedDiff { get { return ResultVerifierBase.DefaultPositionTolerance; } }
        public bool Verify()
        {
            if (!VerifyX())
            {
                return false;
            }
            if (TestGlobals.IgnorePosition != this.ExpectedResultPosY)
            {
                if (!VerifyY())
                {
                    return false;
                }
            }
            return true;
        }
        public bool VerifyX()
        {
            return MaxAllowedDiff >= Math.Abs(this.ExpectedResultPosX - this.VariableX.ActualPos);
        }
        public bool VerifyY()
        {
            return MaxAllowedDiff >= Math.Abs(this.ExpectedResultPosY - this.VariableY.ActualPos);
        }

        public override string ToString()
        {
            return this.IdString;
        }

        #region IPositionInfo

        // Available only after Solve()
        public double PositionX { get { return this.VariableX.ActualPos; } }
        public double PositionY { get { return this.VariableY.ActualPos; } }
        public double Left
        {
            get { return PositionX - (this.sizeX / 2); }
        }
        public double Right
        {
            get { return PositionX + (this.sizeX / 2); }
        }
        public double Top
        {
            get { return PositionY - (this.sizeY / 2); }
        }
        public double Bottom
        {
            get { return PositionY + (this.sizeY / 2); }
        }

        // Available after initialization.
        public double DesiredPosX
        {
            get { return this.desiredPosX; }
        }
        public double DesiredPosY
        {
            get { return this.desiredPosY; }
        }
        public double SizeX
        {
            get { return this.sizeX; }
        }
        public double SizeY
        {
            get { return this.sizeY; }
        }
        public double InitialLeft
        {
            get { return this.desiredPosX - (this.sizeX / 2); }
        }
        public double InitialRight
        {
            get { return this.desiredPosX + (this.sizeX / 2); }
        }
        public double InitialTop
        {
            get { return this.desiredPosY - (this.sizeY / 2); }
        }
        public double InitialBottom
        {
            get { return this.desiredPosY + (this.sizeY / 2); }
        }

        public string ClassName { get { return "Var"; } }
        public string InstanceId { get { return this.IdString; } }
        #endregion // IPositionInfo

    }
}