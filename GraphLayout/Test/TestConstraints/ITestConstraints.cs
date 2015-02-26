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