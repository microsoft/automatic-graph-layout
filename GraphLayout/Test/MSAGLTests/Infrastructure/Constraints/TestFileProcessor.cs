// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestFileProcessor.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    internal class TestFileProcessor : BasicFileProcessor
    {
        private readonly Action<string> processingFunc;

        internal TestFileProcessor(Action<string> writeLineFunc,
                            Func<string, bool> errorFunc,
                            Action<string> processingFunc,
                            bool verbose,
                            bool quiet)
            : base(writeLineFunc, errorFunc, verbose, quiet)
        {
            this.processingFunc = processingFunc;
        }

        internal override void LoadAndProcessFile(string fileName)
        {
            // BasicFileProcessor provides exception management.
            processingFunc(fileName);
        }

    }
} // end namespace TestRectilinear
