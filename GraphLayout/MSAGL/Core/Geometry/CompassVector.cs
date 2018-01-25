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

    internal CompassVector(Directions direction)
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


    internal Directions Dir { get; set; }
    internal CompassVector Right {
      get { return new CompassVector(RotateRight(Dir)); }
    }

    internal static Directions RotateRight(Directions direction) {
      switch (direction) {
        case Directions.North:
          return Directions.East;
        case Directions.East:
          return Directions.South;
        case Directions.South:
          return Directions.West;
        case Directions.West:
          return Directions.North;
        default:
          throw new InvalidOperationException();
      }
    }

    internal static Directions RotateLeft(Directions direction) {
      switch (direction) {
        case Directions.North:
          return Directions.West;
        case Directions.West:
          return Directions.South;
        case Directions.South:
          return Directions.East;
        case Directions.East:
          return Directions.North;
        default:
          throw new InvalidOperationException();
      }
    }

    internal static int ToIndex(Directions direction) {
      switch (direction) {
        case Directions.North:
          return 0;
        case Directions.East:
          return 1;
        case Directions.South:
          return 2;
        case Directions.West:
          return 3;
        default:
          throw new InvalidOperationException();
      }
    }

    internal static Directions VectorDirection(Point d) {
      Directions r = Directions.None;
      if (d.X > ApproximateComparer.DistanceEpsilon)
        r = Directions.East;
      else if (d.X < -ApproximateComparer.DistanceEpsilon)
        r = Directions.West;
      if (d.Y > ApproximateComparer.DistanceEpsilon)
        r |= Directions.North;
      else if (d.Y < -ApproximateComparer.DistanceEpsilon)
        r |= Directions.South;
      return r;
    }

    internal static Directions VectorDirection(Point a, Point b) {
      Directions r = Directions.None;

      // This method is called a lot as part of rectilinear layout.
      // Try to keep it quick.
      double horizontalDiff = b.X - a.X;
      double verticalDiff = b.Y - a.Y;
      double halfEpsilon = ApproximateComparer.DistanceEpsilon / 2;

      if (horizontalDiff > halfEpsilon)
        r = Directions.East;
      else if (-horizontalDiff > halfEpsilon)
        r = Directions.West;
      if (verticalDiff > halfEpsilon)
        r |= Directions.North;
      else if (-verticalDiff > halfEpsilon)
        r |= Directions.South;
      return r;
    }

    internal static Directions DirectionsFromPointToPoint(Point a, Point b) {
      return VectorDirection(a, b);
    }

    internal static Directions PureDirectionFromPointToPoint(Point a, Point b) {
      Directions dir = VectorDirection(a, b);
      Debug.Assert(IsPureDirection(dir), "Impure direction found");
      return dir;
    }

    internal static Directions OppositeDir(Directions direction) {
      switch (direction) {
        case Directions.North:
          return Directions.South;
        case Directions.West:
          return Directions.East;
        case Directions.South:
          return Directions.North;
        case Directions.East:
          return Directions.West;
        default:
          return Directions.None;
      }
    }

    internal static bool IsPureDirection(Directions direction) {
      switch (direction) {
        case Directions.North:
          return true;
        case Directions.East:
          return true;
        case Directions.South:
          return true;
        case Directions.West:
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


    internal static bool DirectionsAreParallel(Directions a, Directions b) {
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
      if ((Dir & Directions.East) == Directions.East)
        p.X += 1;
      if ((Dir & Directions.North) == Directions.North)
        p.Y += 1;
      if ((Dir & Directions.West) == Directions.West)
        p.X -= 1;
      if ((Dir & Directions.South) == Directions.South)
        p.Y -= 1;
      return p;
    }

    /// <summary>
    /// Translates a direction into a Point.
    /// </summary>
    /// <returns></returns>
    public static Point ToPoint(Directions dir) {
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
