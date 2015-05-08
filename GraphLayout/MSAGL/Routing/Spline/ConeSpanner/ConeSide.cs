using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner {
    /// <summary>
    /// represents a cone side
    /// </summary>
    internal abstract class ConeSide {
        internal abstract Point Start { get; }
        internal abstract Point Direction { get; }
        protected internal Cone Cone { get; set; }
        internal bool Removed { get; set; }
    }
}
