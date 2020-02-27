// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlapRemovalParameters.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for Overlap removal parameters.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.Msagl.Core.ProjectionSolver;

namespace Microsoft.Msagl.Core.Geometry
{
    /// <summary>
    /// Per-instance parameters for OverlapRemoval.ConstraintGenerator.Generate()/Solve().
    /// </summary>
    public class OverlapRemovalParameters
 : ICloneable
    {
        /// <summary>
        /// If true and the current instance's IsHorizontal property is true, then by default
        /// constraints will not be generated on the horizontal pass if a vertical constraint
        /// would result in less movement.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public bool AllowDeferToVertical { get; set; }

        /// <summary>
        /// The calculation to choose in deciding which way to resolve overlap (horizontally or vertically)
        /// between two nodes u and v.
        /// If this is false the calculation is simply HOverlap > VOverlap, otherwise we use:
        /// HOverlap / (u.Width + v.Width) > VOverlap / (u.Height + v.Height)
        /// </summary>
        public bool ConsiderProportionalOverlap { get; set; }
        
        /// <summary>
        /// Parameters to the Solver, used in Generate as well as passed through to the Solver.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public Parameters SolverParameters { get; set; }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public OverlapRemovalParameters()
            : this(new Parameters())
        {
        }

        /// <summary>
        /// Constructor taking solver parameters.
        /// </summary>
        /// <param name="solverParameters"></param>
        public OverlapRemovalParameters(Parameters solverParameters)
        {
            this.SolverParameters = solverParameters;
            AllowDeferToVertical = true;
        }

        /// <summary>
        /// Constructor taking OverlapRemoval parameter and solver parameters.
        /// </summary>
        /// <param name="allowDeferToVertical"></param>
        /// <param name="solverParameters"></param>
        public OverlapRemovalParameters(bool allowDeferToVertical, Parameters solverParameters)
        {
            this.AllowDeferToVertical = allowDeferToVertical;
            this.SolverParameters = solverParameters;
        }

        #region ICloneable members
        /// <summary>
        /// Deep-copy the SolverParameters.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            OverlapRemovalParameters newParams = (OverlapRemovalParameters)this.MemberwiseClone();
            newParams.SolverParameters = (Parameters)this.SolverParameters.Clone();
            return newParams;
        }
        #endregion // ICloneable members

    }
}