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
﻿// --------------------------------------------------------------------------------------------------------------------
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
