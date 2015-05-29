using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging {
    internal class AxisEdgeLowPointEvent : SweepEvent {
        Point site;
        
        internal AxisEdge AxisEdge { get; set; }

        public AxisEdgeLowPointEvent(AxisEdge  edge, Point point) {
            site = point;
            AxisEdge = edge;
        }

        internal override Point Site {
            get { return site; }
        }

       
    }
}