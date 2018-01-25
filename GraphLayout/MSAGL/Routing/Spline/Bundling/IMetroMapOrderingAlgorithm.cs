using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// 
    /// </summary>
    interface IMetroMapOrderingAlgorithm {
        IEnumerable<Metroline> GetOrder(Station u, Station v);

        /// <summary>
        /// Get the index of line on the edge (u->v) and node u
        /// </summary>
        int GetLineIndexInOrder(Station u, Station v, Metroline metroLine);
    }



}
