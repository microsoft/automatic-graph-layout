using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner {
    internal class ConeRightSide:ConeSide {
        internal ConeRightSide(Cone cone) {
            this.Cone = cone; 
        }

        internal override Point Start {
            get { return Cone.Apex; }
        }

        internal override Point Direction {
            get { return Cone.RightSideDirection; }
        }

        public override string ToString() {
            return "ConeRightSide " + Start + " " + Direction;
        }
    }
}
