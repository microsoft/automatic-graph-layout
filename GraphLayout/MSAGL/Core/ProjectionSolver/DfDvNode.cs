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
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DfDvNode.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for Constraint-tree traversal iteration nodes for Projection Solver.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    using System.Globalization;

    /// <summary>
    /// variableDoneEval is NULL if we are starting an evaluation; if recursive, it's the variable
    /// on that side from the parent call, which was already processed.
    /// </summary>
    class DfDvNode
    {
        internal DfDvNode Parent { get; private set; }
        internal Constraint ConstraintToEval { get; private set; }
        internal Variable VariableToEval { get; private set; }
        internal Variable VariableDoneEval { get; private set; }

        // For Solution.MaxConstraintTreeDepth
        internal int Depth { get; set; }

        internal bool ChildrenHaveBeenPushed { get; set; }

        internal DfDvNode(DfDvNode parent, Constraint constraintToEval, Variable variableToEval, Variable variableDoneEval)
        {
            Set(parent, constraintToEval, variableToEval, variableDoneEval);
        }

        // For DummyParentNode only.
        internal DfDvNode(Constraint dummyConstraint)
        {
            this.ConstraintToEval = dummyConstraint;
            this.Depth = -1;        // The first real node adds 1, so it starts at 0.
        }

        internal DfDvNode Set(DfDvNode parent, Constraint constraintToEval, Variable variableToEval, Variable variableDoneEval)
        {
            this.Parent = parent;
            this.ConstraintToEval = constraintToEval;
            this.VariableToEval = variableToEval;
            this.VariableDoneEval = variableDoneEval;
            this.Depth = 0;
            this.ChildrenHaveBeenPushed = false;

            constraintToEval.Lagrangian = 0.0;
            return this;
        }

        internal bool IsLeftToRight { get { return this.VariableToEval == this.ConstraintToEval.Right; } }

        /// <summary>
        /// </summary>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}{2} - {3}{4} ({5})",
#if VERIFY || VERBOSE
                            this.ConstraintToEval.Id,
#else
                            "",
#endif
                            IsLeftToRight ? "" : "*", this.ConstraintToEval.Left.Name,
                            IsLeftToRight ? "*" : "", this.ConstraintToEval.Right.Name, Depth);
        }
    }
}