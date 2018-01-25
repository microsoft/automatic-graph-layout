// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalConfiguration.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for Projection Solver global configuration constants.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    /// <summary>
    /// Global configuration constants for the ProjectionSolver namespace.
    /// </summary>
    public struct GlobalConfiguration
    {
        #region InternalConstants

#if EX_VERIFY
        /// <summary>
        /// For comparing block recalculation positions in EX_VERIFY mode; the rounding epsilon.
        /// </summary>
        internal const double BlockReferencePositionEpsilon = 1e-6;
#endif // EX_VERIFY

        #endregion // InternalConstants

    }
}