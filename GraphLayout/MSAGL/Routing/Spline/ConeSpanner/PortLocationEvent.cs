using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner {
    internal class PortLocationEvent : SweepEvent {
        public PortLocationEvent(Point portLocation) {
            PortLocation = portLocation;
        }

        internal override Point Site {
            get {return PortLocation; }
        }

        protected Point PortLocation { get; set; }
    }
}