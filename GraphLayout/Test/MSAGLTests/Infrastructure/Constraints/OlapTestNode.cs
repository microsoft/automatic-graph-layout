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
﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OlapTestNode.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    /// <summary>
    /// Implementation of ITestVariable for OverlapRemoval.
    /// </summary>
    internal class OlapTestNode : ITestVariable
    {
        internal OverlapRemovalNode Node { get; private set; }

        internal OlapTestNode(OverlapRemovalNode node)
        {
            this.Node = node;
        }

        public override string ToString()
        {
            return this.Node.ToString();
        }

        // ITestVariable implementation.
        public double ActualPos
        {
            // We've updated the position by the time this is called.
            // If this.Node is null then we're calling this at the wrong time.
            get { return this.Node.Position; }
        }
    }
}
