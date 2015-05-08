using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner{
    internal class LowestVertexEvent:VertexEvent {
        internal 
            LowestVertexEvent(PolylinePoint p) : base(p) { }
    }
}
