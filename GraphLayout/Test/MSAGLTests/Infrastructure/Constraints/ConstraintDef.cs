// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConstraintDef.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

using Microsoft.Msagl.Core.ProjectionSolver;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    // Class for testing constraint definitions.  We can have two dimensions, X and Y, for
    // OverlapRemoval, but only one for ProjectionSolver, so we use X for that.
    internal class ConstraintDef
    {
        public VariableDef LeftVariableDef { get; set; }
        public VariableDef RightVariableDef { get; set; }
        public double Gap { get; set; }
        public double ReGap { get; set; }
        public bool IsEquality { get; set; }
        public Constraint Constraint { get; set; }

        public ConstraintDef(VariableDef leftVariableDef, VariableDef rightVariableDef, double dblGap)
            : this(leftVariableDef, rightVariableDef, dblGap, /*isEquality:*/ false)
        {
        }

        public ConstraintDef(VariableDef leftVariableDef, VariableDef rightVariableDef, double dblGap, bool isEquality)
        {
            this.LeftVariableDef = leftVariableDef;
            this.RightVariableDef = rightVariableDef;
            this.Gap = dblGap;
            this.IsEquality = isEquality;

            if (isEquality)
            {
                // For autogeneration, to avoid infeasible transitions such as a+3=b, b+3=c, a+9=b.
                this.LeftVariableDef.IsInEqualityConstraint = true;
                this.RightVariableDef.IsInEqualityConstraint = true;
            }
            leftVariableDef.LeftConstraints.Add(this);
        }

        internal bool VerifyGap(double tolerance)
        {
            return VerifyGap(this.Gap, tolerance);
        }

        internal bool VerifyGap(double gap, double tolerance)
        {
            if (this.IsEquality)
            {
                if (Math.Abs(this.LeftVariableDef.ActualPosX + gap - this.RightVariableDef.ActualPosX) > tolerance)
                {
                    return false;
                }
                if (TestGlobals.IsTwoDimensional)
                {
                    if (Math.Abs(this.LeftVariableDef.ActualPosY + gap - this.RightVariableDef.ActualPosY) > tolerance)
                    {
                        return false;
                    }
                }
            }
            else
            {
                if ((this.LeftVariableDef.ActualPosX + gap - this.RightVariableDef.ActualPosX) > tolerance)
                {
                    return false;
                }
                if (TestGlobals.IsTwoDimensional)
                {
                    if ((this.LeftVariableDef.ActualPosY + gap - this.RightVariableDef.ActualPosY) > tolerance)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
