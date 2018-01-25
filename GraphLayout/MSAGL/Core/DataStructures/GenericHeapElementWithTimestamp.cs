using System;

namespace Microsoft.Msagl.Core.DataStructures {
    /// <summary>
    /// A priority queue element that is compared by priority and timestamp
    /// </summary>
    internal class GenericHeapElementWithTimestamp<T> : GenericHeapElement<T> {
        // Timestamp operates as a tiebreaker when priorities are equal.
        internal UInt64 Timestamp { get; set; }

        internal GenericHeapElementWithTimestamp(int index, double priority, T v, UInt64 timestamp)
                : base(index, priority, v) {
            this.Timestamp = timestamp;
        }

        internal int ComparePriority(GenericHeapElementWithTimestamp<T> other) {
            var cmp = base.priority.CompareTo(other.priority);

            // Consider a more recent sequence to have a lower priority value (== higher priority)
            // so reverse the direction of comparison.  Note: Timestamp only applies in the case
            // of tied priorities, so if it wraps, we'll have suboptimal processing and likely a different
            // path in the event of ties, but will not fail.
            return (cmp != 0) ? cmp : other.Timestamp.CompareTo(this.Timestamp);
        }
    }
}
