// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlapRemovalGlobalConfiguration.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for Overlap removal global configuration constants.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Msagl.Core.Geometry {
    /// <summary>
    /// Global configuration constants for the OverlapRemoval namespace.
    /// </summary>
    public static class OverlapRemovalGlobalConfiguration {
        /// <summary>
        /// Default weight for a freely movable cluster border; overridable per BorderInfo instance.
        /// Should be very small compared to default node weight (1) so that it has no visible effect on layout.
        /// Too large and it will cause clusters to be squashed by their bounding variables (since OverlapRemovalCluster
        /// swaps the positions of Left/Right, Top/Bottom nodes to ensure that cluster bounds tightly fit their contents after a solve).
        /// Too small and you will see cluster boundaries "sticking" to nodes outside the cluster (because such constraints will not be
        /// split when they can be because the lagrangian multipliers will be so small as to be ignored before solver termination).
        /// </summary>
        public const double ClusterDefaultFreeWeight = 1e-6;

        /// <summary>
        /// Default weight for an unfixed (freely movable) cluster border; overridable per BorderInfo instance.
        /// </summary>
        public const double ClusterDefaultFixedWeight = 1e8;

        /// <summary>
        /// Default width of cluster borders; overridable per BorderInfo instance via BorderInfo.InnerMargin.
        /// </summary>
        public const double ClusterDefaultBorderWidth = 1e-3;

        #region InternalConstants

        /// <summary>
        /// For comparing event positions, the rounding epsilon.
        /// </summary>
        internal const double EventComparisonEpsilon = 1e-6;

        #endregion // InternalConstants

    } // end struct GlobalConfiguration
} // end namespace ProjectionSolver
