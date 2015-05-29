namespace Microsoft.Msagl.Core.Geometry {
    internal class HullStack {
        Point hullPoint;

        internal Point Point {
            get { return hullPoint; }
            set { hullPoint = value; }
        }
        HullStack next;

        internal HullStack Next {
            get { return next; }
            set { next = value; }
        }

        internal HullStack(Point hullPoint) {
            this.Point = hullPoint;
        }

        public override string ToString() {
            return hullPoint.ToString();
        }

    }
}
