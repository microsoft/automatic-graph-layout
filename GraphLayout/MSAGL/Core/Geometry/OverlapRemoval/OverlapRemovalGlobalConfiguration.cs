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
