using System;

namespace Microsoft.Msagl.Core.Routing {
    /// <summary>
    /// defines the way edges are routed
    /// </summary>
    public enum EdgeRoutingMode {
        /// <summary>
        /// routing splines over tangent visibility graph edge as a sequence of Bezier segments 
        /// </summary>
        Spline,
        /// <summary>
        /// drawing ordered bundles
        /// </summary>
        SplineBundling,
        /// <summary>
        /// draw edges as straight lines 
        /// </summary>
        StraightLine,
        /// <summary>
        /// inside of Sugiyama algorithm use the standard spline routing
        /// </summary>
        SugiyamaSplines,
        /// <summary>
        /// rectilinear edge routing
        /// </summary>
        Rectilinear,
        /// <summary>
        /// rectilinear but not checking for the optimal port and routing just to the node centers
        /// </summary>
        RectilinearToCenter,
        /// <summary>
        /// means no routing should be done
        /// </summary>
        None
    }
}