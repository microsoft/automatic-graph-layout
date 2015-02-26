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
