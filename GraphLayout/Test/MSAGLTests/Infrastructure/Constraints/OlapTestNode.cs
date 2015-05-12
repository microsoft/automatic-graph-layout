// --------------------------------------------------------------------------------------------------------------------
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
