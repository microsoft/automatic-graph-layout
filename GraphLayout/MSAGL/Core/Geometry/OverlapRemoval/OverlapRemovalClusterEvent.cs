// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlapRemovalClusterEvent.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL Event class for Overlap removal constraint generation for Projection solutions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Msagl.Core.Geometry
{
    public partial class OverlapRemovalCluster
    {
        // An event signifies that the scan line has encountered the opening or closing border
        // of a node.  Thus, the ConstraintGenerator has a list that contains two events for
        // each node: the position at which the scan encounters the opening border, and the
        // position at which it encounters the closing border.
        // @@PERF: two of these are created per node/cluster during Generate(); consider making 
        //           them a struct instead to reduce heap churn - they are in lists that are sorted
        //           so consider the CompareTo overhead of passing the struct instead of classref.
         class Event 
        {
            internal bool IsForOpen { get; private set; }
            double Position { get; set; }
            internal OverlapRemovalNode Node { get; private set; }

            internal Event(bool isForOpen, OverlapRemovalNode node, double position)
            {
                this.IsForOpen = isForOpen;
                this.Node = node;
                this.Position = position;
            }

        } // end class Event
    }
}