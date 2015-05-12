// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjTestVariable.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Msagl.UnitTests.Constraints
{
    using Microsoft.Msagl.Core.ProjectionSolver;

    /// <summary>
    /// Implementation of ITestVariable for the ProjectionSolver.
    /// </summary>
    internal class ProjTestVariable : ITestVariable
    {
        internal Variable Variable { get; private set; }

        internal ProjTestVariable(Variable var)
        {
            this.Variable = var;
        }

        public override string ToString()
        {
            return this.Variable.ToString();
        }

        // ITestVariable implementation.
        public double ActualPos
        {
            // If this.Node is null then we're calling this at the wrong time.
            get { return this.Variable.ActualPos; }
        }
    }
}
