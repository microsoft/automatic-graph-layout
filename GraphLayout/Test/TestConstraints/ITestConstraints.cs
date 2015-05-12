//-----------------------------------------------------------------------
// <copyright file="ITestConstraints.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace TestConstraints {
    using Microsoft.Msagl.UnitTests.Constraints;

    // This is the interface that the test specializations (e.g. ProjectionSolver and ConstraintGenerator)
    // created in s_ITester must implement.
    internal interface ITestConstraints {
        // Reset between test methods and each iteration of ToFailure.
        void Reset();

        // Execute a specific file.
        void ProcessFile(string strFullName);

        // Create a test file according to global parameters.
        bool CreateFile(uint cVars, uint cConstraintsPerVar, string strOutFile);

        // Load, execute, and re-write a test file.
        bool ReCreateFile(string strInFile, string strOutFile);

        // Load a test file.
        TestFileReader LoadFile(string fileName);
    }
}