using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Core.Geometry {
    /// <summary>
    /// Two dimensional point
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    [DebuggerDisplay("({X},{Y})")]
    public struct Point : IComparable<Point> {
        /// <summary>
        /// overrides the equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if (!(obj is Point))
                return false;
            return (Point)obj == this;
        }

        static internal Point P(double xCoordinate, double yCoordinate) {
            return new Point(xCoordinate, yCoordinate);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            uint hc = (uint)X.GetHashCode();
            return (int)((hc << 5 | hc >> 27) + (uint)Y.GetHashCode());
        }
        /// <summary>
        /// the point norm
        /// </summary>
        public double Length { get { return (double)Math.Sqrt(X * X + Y * Y); } }
        /// <summary>
        /// point norm squared (faster to compute than Length)
        /// </summary>
        public double LengthSquared { get { return X * X + Y * Y; } }
        /// <summary>
        /// overrides the equality
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static bool operator ==(Point point0, Point point1) {
            return point0.X == point1.X && point0.Y == point1.Y;
        }
        /// <summary>
        /// overrides the less operator
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static bool operator <(Point point0, Point point1) {
            return point0.CompareTo(point1) < 0;
        }
        /// <summary>
        /// overrides the less or equal operator
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static bool operator <=(Point point0, Point point1) {
            return point0.CompareTo(point1) <= 0;
        }

        /// <summary>
        /// overrides the greater or equal operator
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static bool operator >=(Point point0, Point point1) {
            return point0.CompareTo(point1) >= 0;
        }
        /// <summary>
        /// overrides the greater operator
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static bool operator >(Point point0, Point point1) {
            return point0.CompareTo(point1) > 0;
        }

        /// <summary>
        /// the inequality operator
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static bool operator !=(Point point0, Point point1) {
            return !(point0 == point1);
        }
        /// <summary>
        ///  the negation operator
        /// </summary>
        /// <param name="point0"></param>
        /// <returns></returns>
        public static Point operator -(Point point0) {
            return new Point(-point0.X, -point0.Y);
        }
        /// <summary>
        /// the negation operator
        /// </summary>
        /// <returns></returns>
        public Point Negate() {
            return -this;
        }

        /// <summary>
        ///  the addition
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static Point operator +(Point point0, Point point1) {
            return new Point(point0.X + point1.X, point0.Y + point1.Y);
        }
        /// <summary>
        ///  the addition
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static Point Add(Point point0, Point point1) {
            return point0 + point1;
        }
        /// <summary>
        /// overrides the substraction
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static Point operator -(Point point0, Point point1) {
            return new Point(point0.X - point1.X, point0.Y - point1.Y);
        }


        /// <summary>
        /// overrides the substraction
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static Point Subtract(Point point0, Point point1) {
            return point0 - point1;
        }

        /// <summary>
        /// othe internal product
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static double operator *(Point point0, Point point1) {
            return point0.X * point1.X + point0.Y * point1.Y;
        }


        /// <summary>
        /// cross product
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <returns></returns>
        public static double CrossProduct(Point point0, Point point1) {
            return point0.X * point1.Y - point0.Y * point1.X;
        }
        /// <summary>
        /// the multipliction by scalar in x and y
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y")]
        public static Point Scale(double xScale, double yScale, Point point) {
            return new Point(xScale * point.X, yScale * point.Y);
        }
        /// <summary>
        /// the multipliction by scalar
        /// </summary>
        /// <param name="coefficient"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Point operator *(double coefficient, Point point) {
            return new Point(coefficient * point.X, coefficient * point.Y);
        }
        /// <summary>
        /// multiplication on coefficient scalar
        /// </summary>
        /// <param name="point"></param>
        /// <param name="coefficient"></param>
        /// <returns></returns>
        public static Point operator *(Point point, double coefficient) {
            return new Point(coefficient * point.X, coefficient * point.Y);
        }
        /// <summary>
        ///  multiplication on coefficient scalar
        /// </summary>
        /// <param name="coefficient"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Point Multiply(double coefficient, Point point) {
            return new Point(coefficient * point.X, coefficient * point.Y);
        }

        /// <summary>
        ///  multiplication on coefficient scalar
        /// </summary>
        /// <param name="point"></param>
        /// <param name="coefficient"></param>
        /// <returns></returns>
        public static Point Multiply(Point point, double coefficient) {
            return new Point(coefficient * point.X, coefficient * point.Y);
        }
        /// <summary>
        ///  division on coefficient scalar
        /// </summary>
        /// <param name="point"></param>
        /// <param name="coefficient"></param>
        /// <returns></returns>
        public static Point operator /(Point point, double coefficient) {
            return new Point(point.X / coefficient, point.Y / coefficient);
        }

        /// <summary>
        /// division on coefficient scalar
        /// </summary>
        /// <param name="coefficient"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Point operator /(double coefficient, Point point) {
            return new Point(point.X / coefficient, point.Y / coefficient);
        }
        /// <summary>
        /// division on coefficient scalar
        /// </summary>
        /// <param name="point"></param>
        /// <param name="coefficient"></param>
        /// <returns></returns>
        public static Point Divide(Point point, double coefficient) {
            return new Point(point.X / coefficient, point.Y / coefficient);
        }
        /// <summary>
        /// division on coefficient scalar
        /// </summary>
        /// <param name="coefficient"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Point Divide(double coefficient, Point point) {
            return new Point(point.X / coefficient, point.Y / coefficient);
        }
        static internal string DoubleToString(double d) {
            return (Math.Abs(d) < 1e-11) ? "0" : d.ToString("#.##########", CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Rounded representation of the points.  DebuggerDisplay shows the unrounded form.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return "(" + DoubleToString(X) + "," + DoubleToString(Y) + ")";
        }

        /// <summary>
        /// the x coordinate
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "X")]
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=369
        // Filippo: because structs become by-ref in SharpKit, assigning to the coordinates directly can be really dangerous. If you assign Point P1 to Point P2, and then
        // modify P2, in .NET you're modifying a copy, but in JS you're modifying the content of a reference. For this reason, I need to take a look every instance of
        // assignment to a Point coordinate and make sure it's not accidentally using a reference in the JS version (in this case, the assignment where the Point was
        // created must be converted to a call to Clone()).
        private double m_X;
        public double X
        {
            get { return m_X; }
            set
            {
                m_X = value;
                UpdateHashKey();
            }
        }
#else
        public double X; // This is member accessed a lot.  Using a field instead of a property for performance.
#endif

        /// <summary>
        /// the y coordinate
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Y")]
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=369
        private double m_Y;
        public double Y
        {
            get { return m_Y; }
            set
            {
                m_Y = value;
                UpdateHashKey();
            }
        }
#else
        public double Y; // This is member accessed a lot.  Using a field instead of a property for performance.
#endif

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289
        //SharpKit/Colin - hashing
        private SharpKit.JavaScript.JsString _hashKey;
        private void UpdateHashKey()
        {
            _hashKey = "(" + X + "," + Y + ")";
        }

        public Point Clone()
        {
            return new Point(m_X, m_Y);
        }
#endif

        /// <summary>
        /// construct the point from x and y coordinates
        /// </summary>
        /// <param name="xCoordinate"></param>
        /// <param name="yCoordinate"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y")]
        public Point(double xCoordinate, double yCoordinate)
        {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289
            m_X = xCoordinate;
            m_Y = yCoordinate;
            _hashKey = null;
            UpdateHashKey();
#else
            X = xCoordinate;
            Y = yCoordinate;
#endif
        }

        static internal bool ParallelWithinEpsilon(Point a, Point b, double eps) {
            double alength = a.Length;
            double blength = b.Length;
            if (alength < eps || blength < eps)
                return true;

            a /= alength;
            b /= blength;

            return Math.Abs(-a.X * b.Y + a.Y * b.X) < eps;
        }


        /// <summary>
        /// returns this rotated by the angle counterclockwise; does not change "this" value
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public Point Rotate(double angle) {
            double c = Math.Cos(angle);
            double s = Math.Sin(angle);
            return new Point(c * X - s * Y, s * X + c * Y);
        }

        /// <summary>
        /// creates coefficient new point with the norm 1, does not change "this" point
        /// </summary>
        /// <returns></returns>
        public Point Normalize() {
            var length = Length;
            if (length < ApproximateComparer.Tolerance)
                throw new InvalidOperationException(); //the vector is too short to be normalized
            return this / length;
        }

        /// <summary>
        /// The counter-clockwise angle when rotating point1 towards point3 around point2
        /// </summary>
        /// <returns></returns>
        static public double Angle(Point point1, Point center, Point point3) {
            return Angle(point1 - center, point3 - center);
        }

        public static double Dot(Point v1, Point v2)
        {
            return v1.X*v2.X + v1.Y*v2.Y;
        }

        /// <summary>
        /// The angle you need to turn "side0" counterclockwise to make it collinear with "side1"
        /// </summary>
        /// <param name="side0"></param>
        /// <param name="side1"></param>
        /// <returns></returns>
        static public double Angle(Point side0, Point side1) {
            var ax = side0.X;
            var ay = side0.Y;
            var bx = side1.X;
            var by = side1.Y;

            double cross = ax * by - ay * bx;
            double dot = ax * bx + ay * by;

            if (Math.Abs(dot) < ApproximateComparer.Tolerance) {
                if (Math.Abs(cross) < ApproximateComparer.Tolerance)
                    return 0;


                if (cross < -ApproximateComparer.Tolerance)
                    return 3 * Math.PI / 2;
                return Math.PI / 2;
            }

            if (Math.Abs(cross) < ApproximateComparer.Tolerance) {
                if (dot < -ApproximateComparer.Tolerance)
                    return Math.PI;
                return 0.0;
            }

            double atan2 = Math.Atan2(cross, dot);
            if (cross >= -ApproximateComparer.Tolerance)
                return atan2;
            return Math.PI * 2.0 + atan2;
        }

        /// <summary>
        /// computes orientation of three vectors with a common source
        /// (compare polar angles of v1 and v2 with respect to v0)
        /// </summary>
        /// <returns>
        ///  -1 if the orientation is v0 v1 v2
        ///   1 if the orientation is v0 v2 v1
        ///   0  if v1 and v2 are collinear and codirectinal
        /// </returns>
        static public int GetOrientationOf3Vectors(Point vector0, Point vector1, Point vector2) {
            const double multiplier = 1000; //TODO, need to fix it?

            vector0 *= multiplier;
            vector1 *= multiplier;
            vector2 *= multiplier;

            double xp2 = Point.CrossProduct(vector0, vector2);
            double dotp2 = vector0 * vector2;
            double xp1 = Point.CrossProduct(vector0, vector1);
            double dotp1 = vector0 * vector1;

            // v1 is collinear with v0
            if (ApproximateComparer.Close(xp1, 0.0) && ApproximateComparer.GreaterOrEqual(dotp1, 0.0)) {
                if (ApproximateComparer.Close(xp2, 0.0) && ApproximateComparer.GreaterOrEqual(dotp2, 0.0))
                    return 0;
                return 1;
            }

            // v2 is collinear with v0
            if (ApproximateComparer.Close(xp2, 0.0) && ApproximateComparer.GreaterOrEqual(dotp2, 0.0)) {
                return -1;
            }

            if (ApproximateComparer.Close(xp1, 0.0) || ApproximateComparer.Close(xp2, 0.0) || xp1 * xp2 > 0.0) {
                // both on same side of v0, compare to each other
                return ApproximateComparer.Compare(Point.CrossProduct(vector2, vector1), 0.0);
            }

            // vectors "less than" zero degrees are actually large, near 2 pi
            return -ApproximateComparer.Compare(Math.Sign(xp1), 0.0);
        }

        /// <summary>
        /// If the area is negative then C lies to the right of the line [cornerA, cornerB] or, in another words, the triangle (A , B, C) is oriented clockwize
        /// If it is positive then C lies ot he left of the line [A,B] another words, the triangle A,B,C is oriented counter-clockwize.
        /// Otherwise A ,B and C are collinear.
        /// </summary>
        /// <param name="cornerA"></param>
        /// <param name="cornerB"></param>
        /// <param name="cornerC"></param>
        /// <returns></returns>        
        static public double SignedDoubledTriangleArea(Point cornerA, Point cornerB, Point cornerC) {
            return (cornerB.X - cornerA.X) * (cornerC.Y - cornerA.Y) - (cornerC.X - cornerA.X) * (cornerB.Y - cornerA.Y);
        }

        /// <summary>
        /// figures out the triangle on the plane orientation: positive- counterclockwise, negative - clockwise
        /// </summary>
        /// <param name="cornerA"></param>
        /// <param name="cornerB"></param>
        /// <param name="cornerC"></param>
        /// <returns></returns>
        static public TriangleOrientation GetTriangleOrientation(Point cornerA, Point cornerB, Point cornerC) {
            double area = SignedDoubledTriangleArea(cornerA, cornerB, cornerC);
            if (area > ApproximateComparer.DistanceEpsilon)
                return TriangleOrientation.Counterclockwise;
            if (area < -ApproximateComparer.DistanceEpsilon)
                return TriangleOrientation.Clockwise;
            return TriangleOrientation.Collinear;
        }


        /// <summary>
        /// figures out the triangle on the plane orientation: positive- counterclockwise, negative - clockwise
        /// </summary>
        /// <param name="cornerA"></param>
        /// <param name="cornerB"></param>
        /// <param name="cornerC"></param>
        /// <returns></returns>
        static public TriangleOrientation GetTriangleOrientationWithIntersectionEpsilon(Point cornerA, Point cornerB, Point cornerC)
        {
            var area = Point.SignedDoubledTriangleArea(cornerA, cornerB, cornerC);
            if (area > ApproximateComparer.IntersectionEpsilon)
                return TriangleOrientation.Counterclockwise;
            if (area < -ApproximateComparer.IntersectionEpsilon)
                return TriangleOrientation.Clockwise;
            return TriangleOrientation.Collinear;
        }


      
        /// <summary>
        /// returns true if an orthogonal projection of point on [segmentStart,segmentEnd] exists
        /// </summary>
        /// <param name="point"></param>
        /// <param name="segmentStart"></param>
        /// <param name="segmentEnd"></param>
        /// <returns></returns>
        public static bool CanProject(Point point, Point segmentStart, Point segmentEnd) {
            Point bc = segmentEnd - segmentStart;

            Point ba = point - segmentStart;

            if (ba * bc < 0) // point belongs to the halfplane before the segment
                return false;

            Point ca = point - segmentEnd;
            if (ca * bc > 0) //point belongs to the halfplane after the segment
                return false;

            return true;
        }

        //returns true if there is an intersection lying in the inner part of the segment
        static internal bool IntervalIntersectsRay(Point segStart, Point segEnd, Point rayOrigin, Point rayDirection, out Point x) {
            if (!LineLineIntersection(segStart, segEnd, rayOrigin, rayOrigin + rayDirection, out x))
                return false;
            var ds = segStart - x;
            var de = x - segEnd;
            if (ds * de <= 0)
                return false;
            if ((x - rayOrigin) * rayDirection < 0)
                return false;
            return ds * ds > ApproximateComparer.SquareOfDistanceEpsilon && de * de >= ApproximateComparer.SquareOfDistanceEpsilon;
        }

        //returns true if there is an intersection lying in the inner part of rays
        static internal bool RayIntersectsRayInteriors(Point aOrig, Point aDirection, Point bOrig, Point bDirection, out Point x) {
            return Point.LineLineIntersection(aOrig, aOrig + aDirection,
                                              bOrig, bOrig + bDirection, out x) &&
                   (x - aOrig) * aDirection / aDirection.L1 > ApproximateComparer.DistanceEpsilon &&
                   (x - bOrig) * bDirection / bDirection.L1 > ApproximateComparer.DistanceEpsilon;
        }

        /// <summary>
        /// projects a point to an infinite line
        /// </summary>
        /// <param name="pointOnLine0"> a point on the line </param>
        /// <param name="pointOnLine1"> a point on the line </param>
        /// <param name="point"> the point to project </param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OnLine")]
        static public Point ProjectionToLine(Point pointOnLine0, Point pointOnLine1, Point point) {
            var d = pointOnLine1 - pointOnLine0;
            var dLen = d.Length;
            if (dLen < ApproximateComparer.DistanceEpsilon)
                return pointOnLine0;
            d /= dLen;
            var pr = (point - pointOnLine0) * d; //projection 
            var ret = pointOnLine0 + pr * d;
            Debug.Assert(Math.Abs((point - ret) * d) < ApproximateComparer.DistanceEpsilon);
            return ret;
        }

        /// <summary>
        /// The closest point on the segment [segmentStart,segmentEnd] to "point". 
        /// See the drawing DistToLineSegment.gif.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="parameter">the parameter of the closest point</param>
        /// <returns></returns>
        static internal double DistToLineSegment(Point a, Point b, Point c, out double parameter) {
            Point bc = c - b;
            Point ba = a - b;
            double c1, c2;
            if ((c1 = bc * ba) <= 0.0 + ApproximateComparer.Tolerance) {
                parameter = 0;
                return ba.Length;
            }
            if ((c2 = bc * bc) <= c1 + ApproximateComparer.Tolerance) {
                parameter = 1;
                return (a - c).Length;
            }
            parameter = c1 / c2;
            return (ba - parameter * bc).Length;
        }

        /// <summary>
        /// The closest point on the segment [segmentStart,segmentEnd] to "point". 
        /// See the drawing DistToLineSegment.gif.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="segmentStart"></param>
        /// <param name="segmentEnd"></param>
        /// <returns></returns>
        static public Point ClosestPointAtLineSegment(Point point, Point segmentStart, Point segmentEnd) {
            Point bc = segmentEnd - segmentStart;
            Point ba = point - segmentStart;
            double c1, c2;
            if ((c1 = bc * ba) <= 0.0 + ApproximateComparer.Tolerance)
                return segmentStart;

            if ((c2 = bc * bc) <= c1 + ApproximateComparer.Tolerance)
                return segmentEnd;

            double parameter = c1 / c2;
            return segmentStart + parameter * bc;
        }

        /// <summary>
        /// return parameter on the segment [segStart, segEnd] which is closest to the "point"
        /// see the drawing DistToLineSegment.gif
        /// </summary>
        /// <param name="point"></param>
        /// <param name="segmentStart"></param>
        /// <param name="segmentEnd"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "seg"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OnLine")]
        static public double ClosestParameterOnLineSegment(Point point, Point segmentStart, Point segmentEnd) {
            Point bc = segmentEnd - segmentStart;
            Point ba = point - segmentStart;
            double c1, c2;
            if ((c1 = bc * ba) <= 0.0 + ApproximateComparer.Tolerance)
                return 0;

            if ((c2 = bc * bc) <= c1 + ApproximateComparer.Tolerance)
                return 1;

            return c1 / c2;
        }


        /// <summary>
        /// get the intersection of two infinite lines
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "4#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "d"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "c"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
        static public bool LineLineIntersection(Point a, Point b, Point c, Point d, out Point x) {
            //look for the solution in the form a+u*(b-a)=c+v*(d-c)
            double u, v;
            Point ba = b - a;
            Point cd = c - d;
            Point ca = c - a;
            bool ret = LinearSystem2.Solve(ba.X, cd.X, ca.X, ba.Y, cd.Y, ca.Y, out u, out v);
            if (ret) {
                x = a + u * ba;
                return true;
            }
            else {
                x = new Point();
                return false;
            }
        }

        /// <summary>
        /// get the intersection of two line segments
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "4#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "d"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "c"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
        static public bool SegmentSegmentIntersection(Point a, Point b, Point c, Point d, out Point x) {
            //look for the solution in the form a+u*(b-a)=c+v*(d-c)
            double u, v;
            Point ba = b - a;
            Point cd = c - d;
            Point ca = c - a;
            bool ret = LinearSystem2.Solve(ba.X, cd.X, ca.X, ba.Y, cd.Y, ca.Y, out u, out v);
            double eps = ApproximateComparer.Tolerance;
            if (ret && u > -eps && u < 1.0 + eps && v > -eps && v < 1.0 + eps) {
                x = a + u * ba;
                return true;
            }
            else {
                x = new Point();
                return false;
            }
        }

        /// <summary>
        ///returns true if "point" lies to the left of or on the line linePoint0, linePoint1 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="linePoint0"></param>
        /// <param name="linePoint1"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702")]
        public static bool PointToTheLeftOfLineOrOnLine(Point point, Point linePoint0, Point linePoint1) {
            return SignedDoubledTriangleArea(point, linePoint0, linePoint1) >= 0;
        }

        ///<summary>
        /// returns true if "point" lies to the left of the line linePoint0, linePoint1 
        ///</summary>
        ///<returns></returns>
        public static bool PointToTheLeftOfLine(Point point, Point linePoint0, Point linePoint1) {
            return SignedDoubledTriangleArea(point, linePoint0, linePoint1) > 0;
        }
        /// <summary>
        ///returns true if "point" lies to the right of the line linePoint0, linePoint1 
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public static bool PointToTheRightOfLineOrOnLine(Point point, Point linePoint0, Point linePoint1) {
            return SignedDoubledTriangleArea(linePoint0, linePoint1, point) <= 0;
        }

        ///<summary>
        ///</summary>
        ///<returns></returns>
        public static bool PointToTheRightOfLine(Point point, Point linePoint0, Point linePoint1) {
            return SignedDoubledTriangleArea(linePoint0, linePoint1, point) < 0;
        }


        #region IComparable<Point> Members
        /// <summary>
        /// compares two points in the lexigraphical order
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Point other) {
            var r = X.CompareTo(other.X);
            return r != 0 ? r : Y.CompareTo(other.Y);
        }
        /// <summary>
        /// 
        /// </summary>
        internal Direction CompassDirection {
            get { return CompassVector.VectorDirection(this); }
        }
        /// <summary>
        /// the L1 norm
        /// </summary>
        public double L1 {
            get { return Math.Abs(X) + Math.Abs(Y); }
        }

        #endregion

        ///<summary>
        ///rotates the point 90 degrees counterclockwise
        ///</summary>
        public Point Rotate90Ccw() {
            return new Point(-Y, X);
        }

        ///<summary>
        ///rotates the point 90 degrees counterclockwise
        ///</summary>
        public Point Rotate90Cw() {
            return new Point(Y, -X);
        }

        internal static bool PointIsInsideCone(Point p, Point apex, Point leftSideConePoint, Point rightSideConePoint) {
            return PointToTheRightOfLineOrOnLine(p, apex, leftSideConePoint) &&
                   PointToTheLeftOfLineOrOnLine(p, apex, rightSideConePoint);
        }

        static Random rnd = new Random(1);
        ///<summary>
        ///creates random unit point
        ///</summary>
        internal static Point RandomPoint() {
            double x = -1 + 2 * rnd.NextDouble();
            double y = -1 + 2 * rnd.NextDouble();
            return new Point(x, y).Normalize();
        }
    }
}
