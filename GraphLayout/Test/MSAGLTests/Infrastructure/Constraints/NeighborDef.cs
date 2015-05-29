// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NeighborDef.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Msagl.UnitTests.Constraints
{
    // Class for testing neighbour-pair definitions for minimizing (lhs-rhs)^2.
    // We can have two dimensions, X and Y, for OverlapRemoval, but only one for
    // ProjectionSolver, so we use X for that.

    // Because we have only the two variables and weight, we can't directly test
    // satisfaction of the minimization; instead we will just test the resultant
    // positions against a general-purpose solver's results.
    internal class NeighborDef
    {
        internal VariableDef LeftVariableDef { get; set; }
        internal VariableDef RightVariableDef { get; set; }
        internal double Weight { get; set; }

        public NeighborDef(VariableDef leftVariableDef, VariableDef rightVariableDef, double weight)
        {
            this.LeftVariableDef = leftVariableDef;
            this.RightVariableDef = rightVariableDef;
            this.Weight = weight;
        }
    }
}