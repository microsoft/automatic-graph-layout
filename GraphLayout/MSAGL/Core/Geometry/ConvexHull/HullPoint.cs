
namespace Microsoft.Msagl.Core.Geometry {
    internal class HullPoint {
        Point point;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Point Point {
            get { return point; }
            set { point = value; }
        }
        bool deleted;

        internal bool Deleted {
            get { return deleted; }
            set { deleted = value; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal HullPoint(Point point) {
            this.Point = point;
        }

        public override string ToString() {
            return point + (Deleted ? "X" : "");
        }
    }
}
