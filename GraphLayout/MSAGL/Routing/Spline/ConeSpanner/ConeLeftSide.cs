using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner {
    internal class ConeLeftSide:ConeSide {
        internal ConeLeftSide(Cone cone) { Cone = cone; }

        internal override Point Start {
            get { return Cone.Apex; }
        }

        internal override Point Direction {
            get { return Cone.LeftSideDirection; }
        }
        public override string ToString() {
            return "ConeLeftSide " + Start + " " + Direction;
        }

    }
}
