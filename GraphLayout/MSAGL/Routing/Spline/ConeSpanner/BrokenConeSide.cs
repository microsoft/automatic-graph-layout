using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner{
    /// <summary>
    /// represents a cone side that is broken by the obstacle 
    /// </summary>
    internal class BrokenConeSide:ConeSide {
        /// <summary>
        /// point where it starts
        /// </summary>
        internal Point start;

        internal override Point Start {
            get { return start; }
        }

        /// <summary>
        /// it is the side of the cone that intersects the obstacle side
        /// </summary>
        internal ConeSide ConeSide { get; set; }

        internal PolylinePoint EndVertex { get; set; }

        internal Point End {
            get { return EndVertex.Point; }
        }
      

        internal BrokenConeSide(Point start, PolylinePoint end, ConeSide coneSide) {
            this.start = start;
            EndVertex = end;
            ConeSide = coneSide;
        }


        internal override Point Direction {
            get { return End-Start; }
        }

        public override string ToString() {
            return "BrokenConeSide: " + Start + "," + End;
        }
    }
}
