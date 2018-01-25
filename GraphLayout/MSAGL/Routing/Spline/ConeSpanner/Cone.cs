using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner {
    internal class Cone {
        bool removed;

        internal bool Removed {
            get { return removed; }
            set { removed = value; }
        }
        Point apex;
        readonly IConeSweeper coneSweeper;

        internal Cone(Point apex, IConeSweeper coneSweeper) {
            this.apex = apex;
            this.coneSweeper = coneSweeper;
        }

        internal Point Apex {
            get { return apex; }
            set { apex = value; }
        }

        internal Point RightSideDirection {
            get { return coneSweeper.ConeRightSideDirection; }
        }

        internal Point LeftSideDirection {
            get { return coneSweeper.ConeLeftSideDirection; }
        }



        private ConeSide rightSide;

        internal ConeSide RightSide {
            get { return rightSide; }
            set { rightSide = value;
            rightSide.Cone = this;
            }
        }
        private ConeSide leftSide;

        internal ConeSide LeftSide {
            get { return leftSide; }
            set { 
                leftSide = value;
                leftSide.Cone = this;
            }
        }
    }
}
