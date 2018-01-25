// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SolverAlgorithm.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for algorithm enumeration for Projection Solver.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    /// <summary>
    /// The algorithm used by the ProjectionSolver.
    /// </summary>
    public enum SolverAlgorithm
    {
        /// <summary>
        /// Iterative Project/Split only.
        /// </summary>
        ProjectOnly,

        /// <summary>
        /// Diagonally-scaled gradient projection/Qpsc (Quadratic Programming for Separation Constraints).
        /// </summary>
        QpscWithScaling,

        /// <summary>
        /// Gradient projection/Qpsc (Quadratic Programming for Separation Constraints) without diagonal scaling.
        /// </summary>
        QpscWithoutScaling
    }
}
