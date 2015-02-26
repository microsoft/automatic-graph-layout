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
//
// EventQueue.cs
// MSAGL class for Rectilinear Edge Routing between two nodes.
//
// Copyright Microsoft Corporation.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;
using RectRout = Microsoft.Msagl.Routing.Rectilinear;

namespace Microsoft.Msagl.Routing.Rectilinear {
    using DebugHelpers;

    /// <summary>
    /// Wrap the tree of events.
    /// </summary>
    internal class EventQueue : IComparer<SweepEvent> {
        ScanDirection scanDirection;
        readonly BinaryHeapWithComparer<SweepEvent> eventTree;

        internal EventQueue() {
            eventTree = new BinaryHeapWithComparer<SweepEvent>(this);
        }
        
        internal void Reset(ScanDirection scanDir) {
            Debug.Assert(0 == eventTree.Count, "Stray events in EventQueue.Reset");
            scanDirection = scanDir;
        }

        internal void Enqueue(SweepEvent evt) {
            DevTraceInfo(1, "Enqueueing {0}", evt);
            eventTree.Enqueue(evt);
        }

        internal SweepEvent Dequeue() {
            SweepEvent evt = eventTree.Dequeue();
            DevTraceInfo(1, "Dequeueing {0}", evt);
            return evt;
        }

        internal int Count {
            get { return eventTree.Count; }
        }

        #region IComparer<SweepEvent>
        /// <summary>
        /// For ordering events in the event list.
        /// Assuming vertical sweep (sweeping up from bottom, scanning horizontally) then order events
        /// first by lowest Y coord, then by lowest X coord (thus assuming we use Cartesian coordinates
        /// (negative is downward == bottom).
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public int Compare(SweepEvent lhs, SweepEvent rhs) {
            if (lhs == rhs) {
                return 0;
            }
            if (lhs == null) {
                return -1;
            }
            if (rhs == null) {
                return 1;
            }

            // First see if it's at the same scanline level (perpendicular coordinate).
            int cmp = scanDirection.ComparePerpCoord(lhs.Site, rhs.Site);
            if (0 == cmp) {
                // Event sites are at the same scanline level. Make sure that any reflection events are lowest (come before
                // any side events, which could remove the side the reflection event was queued for).  We may have two
                // reflection events at same coordinate, because we enqueue in two situations: when a side is opened,
                // and when a side that is within that side's scanline-parallel span is closed.
                bool lhsIsNotReflection = !(lhs is BasicReflectionEvent);
                bool rhsIsNotReflection = !(rhs is BasicReflectionEvent);
                cmp = lhsIsNotReflection.CompareTo(rhsIsNotReflection);

                // If the scanline-parallel coordinate is the same these events are at the same point.
                if (0 == cmp) {
                    cmp = scanDirection.CompareScanCoord(lhs.Site, rhs.Site);
                }
            }   
            return cmp;
        }
        #endregion // IComparer<SweepEvent>

        #region DevTrace
#if DEVTRACE
        readonly DevTrace eventQueueTrace = new DevTrace("Rectilinear_EventQueueTrace", "EventQueue");
#endif // DEVTRACE

        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        void DevTraceInfo(int verboseLevel, string format, params object[] args) {
#if DEVTRACE
            eventQueueTrace.WriteLineIf(DevTrace.Level.Info, verboseLevel, format, args);
#endif // DEVTRACE
        }
        #endregion // DevTrace
    }
}
