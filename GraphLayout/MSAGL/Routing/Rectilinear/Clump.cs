//
// Clump.cs
// MSAGL class for accreting clumps of overlapped obstacles for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System.Collections.Generic;

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// This is the list of obstacles in the clump.
    /// </summary>
    internal class Clump : List<Obstacle> {
        internal Clump(IEnumerable<Obstacle> obstacles) {
            this.AddRange(obstacles);
        }
#if TEST_MSAGL
        /// <summary>
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public override string ToString() {
            return string.Format("({0}", this.Count);
        }
#endif
    }
}
