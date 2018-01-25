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