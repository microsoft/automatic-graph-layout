//-----------------------------------------------------------------------
// <copyright file="DebugAssertRedirector.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Msagl.UnitTests
{
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    internal class DebugAssertRedirector : DefaultTraceListener 
    {
        public override void Fail(string message) 
        {
            Assert.Fail(message);
        }
    }
}