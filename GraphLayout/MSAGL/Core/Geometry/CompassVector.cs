using System;
using System.Diagnostics;

namespace Microsoft.Msagl.Core.Geometry {
    internal struct CompassVector {

        /// <summary>
        /// Override op==
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(CompassVector a, CompassVector b) {
            return a.Dir == b.Dir;
        }

        /// <summary>
        /// Return the hash code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            return (int)Dir;
        }

        /// <summary>
        /// Override op!=
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(CompassVector a, CompassVector b) {
            return a.Dir != b.Dir;
        }

        internal CompassVector(Direction direction)
            : this() {
            Dir = direction;
        }

        // Warning: do not use for VisibilityGraph generation.  See VectorDirection(a) comments.
        internal CompassVector(Point a)
            : this() {
            Dir = VectorDirection(a);
        }

        internal CompassVector(Point a, Point b)
            : this() {
            Dir = VectorDirection(a, b);
        }


        internal Direction Dir { get; set; }
        internal CompassVector Right {
            get { return new CompassVector(RotateRight(Dir)); }
        }

        internal static Direction RotateRight(Direction direction) {
            switch (direction) {
                case Direction.North:
                    return Direction.East;
                case Direction.East:
                    return Direction.South;
                case Direction.South:
                    return Direction.West;
                case Direction.West:
                    return Direction.North;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal static Direction RotateLeft(Direction direction) {
            switch (direction) {
                case Direction.North:
                    return Direction.West;
                case Direction.West:
                    return Direction.South;
                case Direction.South:
                    return Direction.East;
                case Direction.East:
                    return Direction.North;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal static int ToIndex(Direction direction) {
            switch (direction) {
                case Direction.North:
                    return 0;
                case Direction.East:
                    return 1;
                case Direction.South:
                    return 2;
                case Direction.West:
                    return 3;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal static Direction VectorDirection(Point d){
            Direction r = Direction.None;
            if(d.X > ApproximateComparer.DistanceEpsilon)
                r = Direction.East;
            else if(d.X < -ApproximateComparer.DistanceEpsilon)
                r = Direction.West;
            if(d.Y > ApproximateComparer.DistanceEpsilon)
                r |= Direction.North;
            else if(d.Y < -ApproximateComparer.DistanceEpsilon)
                r |= Direction.South;
            return r;
        }

        internal static Direction VectorDirection(Point a, Point b) {
            Direction r = Direction.None;

            // This method is called a lot as part of rectilinear layout.
            // Try to keep it quick.
            double horizontalDiff = b.X - a.X;
            double verticalDiff = b.Y - a.Y;
            double halfEpsilon = ApproximateComparer.DistanceEpsilon / 2;

            if (horizontalDiff > halfEpsilon)
                r = Direction.East;
            else if (-horizontalDiff > halfEpsilon)
                r = Direction.West;
            if (verticalDiff > halfEpsilon)
                r |= Direction.North;
            else if (-verticalDiff > halfEpsilon)
                r |= Direction.South;
            return r;
        }

        internal static Direction DirectionsFromPointToPoint(Point a, Point b) {
            return VectorDirection(a, b);
        }

        internal static Direction PureDirectionFromPointToPoint(Point a, Point b) {
            Direction dir = VectorDirection(a, b);
            Debug.Assert(IsPureDirection(dir), "Impure direction found");
            return dir;
        }

        internal static Direction OppositeDir(Direction direction) {
            switch (direction) {
                case Direction.North:
                    return Direction.South;
                case Direction.West:
                    return Direction.East;
                case Direction.South:
                    return Direction.North;
                case Direction.East:
                    return Direction.West;
                default:
                    return Direction.None;
            }
        }

        internal static bool IsPureDirection(Direction direction) {
            switch (direction) {
                case Direction.North:
                    return true;
                case Direction.East:
                    return true;
                case Direction.South:
                    return true;
                case Direction.West:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsPureDirection(Point a, Point b) {
            return IsPureDirection(DirectionsFromPointToPoint(a, b));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(CompassVector other) {
            return Equals(other.Dir, Dir);
        }


        internal static bool DirectionsAreParallel(Direction a, Direction b){
            return a == b || a == OppositeDir(b);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof(CompassVector)) return false;
            return Equals((CompassVector)obj);
        }

        /// <summary>
        /// Returns a string representing the direction.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return Dir.ToString();
        }

        /// <summary>
        /// Translates the CompassVector's direction into a new Point.
        /// </summary>
        /// <returns></returns>
        public Point ToPoint() {
            var p = new Point();
            if ((Dir & Direction.East) == Direction.East)
                p.X += 1;
            if ((Dir & Direction.North) == Direction.North)
                p.Y += 1;
            if ((Dir & Direction.West) == Direction.West)
                p.X -= 1;
            if ((Dir & Direction.South) == Direction.South)
                p.Y -= 1;
            return p;
        }

        /// <summary>
        /// Translates a direction into a Point.
        /// </summary>
        /// <returns></returns>
        public static Point ToPoint(Direction dir) {
            return (new CompassVector(dir)).ToPoint();
        }

        /// <summary>
        ///  the negation operator
        /// </summary>
        /// <param name="directionVector"></param>
        /// <returns></returns>
        public static CompassVector operator -(CompassVector directionVector) {
            return new CompassVector(OppositeDir(directionVector.Dir));
        }
        /// <summary>
        /// the negation operator
        /// </summary>
        /// <returns></returns>
        public CompassVector Negate() {
            return -this;
        }


    }
}
