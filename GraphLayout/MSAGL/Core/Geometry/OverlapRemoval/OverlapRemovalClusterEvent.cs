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
         class Event : IComparable<Event>
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

            /// <summary>
            /// Generate a string representation of the Event.
            /// </summary>
            /// <returns>A string representation of the Event.</returns>
            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                    "Event: pos {0:F5} {1} {2}",
                                    this.Position, this.IsForOpen ? "open" : "close", this.Node);
            }

            #region IComparable Members
            /// <summary>
            /// Compare the current event's position to that of rhs in ascending left-to-right order,
            /// with Close events for the same position coming before Open events (so we don't generate
            /// unnecessary constraints for adjacent nodes).
            /// </summary>
            /// <param name="other">The right-hand side of the comparison</param>
            /// <returns></returns>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.CompareTo(System.String)")]
            public int CompareTo(Event other)
            {
                ValidateArg.IsNotNull(other, "other");
                
                int cmp = 0;
                // Use a range so that rounding inaccuracy will give consistent results.
                if (Math.Abs(this.Position - other.Position) > OverlapRemovalGlobalConfiguration.EventComparisonEpsilon)
                {
                    cmp = this.Position.CompareTo(other.Position);
                }
                if (0 == cmp)
                {
                    // Sub-order by IsRendered (false precedes true, which is what we want).
                    cmp = this.IsForOpen.CompareTo(other.IsForOpen);

                    if (0 == cmp)
                    {
                        // Sub-order by node id
                        cmp = this.Node.Id.CompareTo(other.Node.Id);
                    }
                }
                return cmp;
            }
            #endregion

        } // end class Event
    }
}