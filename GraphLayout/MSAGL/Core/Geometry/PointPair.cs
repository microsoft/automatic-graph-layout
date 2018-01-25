using System;

namespace Microsoft.Msagl.Core.Geometry {
    /// <summary>
    /// A segment line wrapper used for creation Dictionary of pairs of points
    /// </summary>
    internal class PointPair : IComparable<PointPair> {
        Point first;
        Point second;

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289
        //SharpKit/Colin - hashing
        SharpKit.JavaScript.JsString _hashKey;
#endif

        public PointPair(Point first, Point second)
        {
            if (IsLess(first, second)) {
                this.first = first;
                this.second = second;
            } else {
                this.first = second;
                this.second = first;
            }
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289
            //SharpKit/Colin - hashing
            _hashKey = this.first.X + "," + this.first.Y + ";" + this.second.X + "," + this.second.Y;
#endif
        }

        public Point First {
            get { return first; }
        }

        public Point Second {
            get { return second; }
        }

        public double Length {
            get { return (first - second).Length; }
        }

        #region IComparable<PointPair> Members

        public int CompareTo(PointPair other) {
            ValidateArg.IsNotNull(other, "other");
            int cr = first.CompareTo(other.first);
            if (cr != 0) return cr;
            return second.CompareTo(other.second);
        }

        #endregion

        static bool IsLess(Point p1, Point p2) {
            if (p1.Y < p2.Y)
                return true;
            if (p1.Y > p2.Y)
                return false;
            return p1.X < p2.X;
        }

        public override int GetHashCode() {
            var hc = (uint) first.GetHashCode();
            return (int) ((hc << 5 | hc >> 27) + (uint) second.GetHashCode());
        }

        public override bool Equals(object obj) {
            var otherPair = obj as PointPair;
            if (ReferenceEquals(otherPair, null))
                return false;
            return otherPair == this;
        }

        public static bool operator ==(PointPair pair0, PointPair pair1) {
            return (pair0.first == pair1.first && pair0.second == pair1.second);
        }

        public static bool operator !=(PointPair pair0, PointPair pair1) {
            return !(pair0 == pair1);
        }

        public override string ToString() {
            return first + " " + second;
        }
    }
}