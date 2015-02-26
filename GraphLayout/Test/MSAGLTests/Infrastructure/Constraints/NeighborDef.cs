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