// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestGlobals.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Msagl.Core.ProjectionSolver;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    internal struct TestGlobals
    {
        internal const double IgnorePosition = double.MaxValue;

        // Set by TestConstraints.exe.
        internal static int VerboseLevel;
        internal static bool IsTwoDimensional;                  // Olap vs. Proj
        internal static uint TestReps = 1;

        // Also set by TestConstraints.exe, and used by ResultVerifierBase and derived
        // classes to restore after running tests.
        internal static readonly Parameters InitialSolverParameters = new Parameters();
    }
}
