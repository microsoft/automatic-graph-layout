using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing {
    internal class RelaxedPolylinePoint  {
        private PolylinePoint polylinePoint;

        internal PolylinePoint PolylinePoint {
            get { return polylinePoint; }
            set { polylinePoint = value; }
        }
        private Point originalPosition;

        internal Point OriginalPosition {
            get { return originalPosition; }
            set { originalPosition = value; }
        }
        
        internal RelaxedPolylinePoint(PolylinePoint polylinePoint, Point originalPosition) {
            this.PolylinePoint = polylinePoint;
            this.OriginalPosition = originalPosition;
        }

        RelaxedPolylinePoint next;

        internal RelaxedPolylinePoint Next {
            get { return next; }
            set { next = value; }
        }
        RelaxedPolylinePoint prev;

        internal RelaxedPolylinePoint Prev {
            get { return prev; }
            set { prev = value; }
        }
    }
}
