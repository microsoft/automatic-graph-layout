using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// Line segment
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    [DebuggerDisplay("[({a.X} {a.Y}),({b.X} {b.Y})]")]
    public class LineSegment : ICurve {

        Point a;//the line goes from a to side1

        /// <summary>
        /// Offsets the curve in the direction of dir
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public ICurve OffsetCurve(double offset, Point dir) { return null; }
        /// <summary>
        /// the line start point
        /// </summary>
        public Point Start {
            get { return a; }
            set { a = value; }
        }
        Point b;
        /// <summary>
        /// the line end point
        /// </summary>
        public Point End {
            get { return b; }
            set { b = value; }
        }


        /// <summary>
        /// the start parameter
        /// </summary>
        public double ParStart {
            get { return 0; }
        }

        /// <summary>
        /// the end parameter
        /// </summary>
        public double ParEnd {
            get { return 1; }
        }

        /// <summary>
        /// Returns the trim curve
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public ICurve Trim(double start, double end) {

            if (start > end)
                throw new InvalidOperationException();//"wrong params in trimming");

            Point p1 = start <= ParStart ? Start : (1 - start) * a + start * b;
            Point p2 = end >= ParEnd ? End : (1 - end) * a + end * b;
            if (ApproximateComparer.Close(start, end)) {
                return null;
            }
            return new LineSegment(p1, p2);
        }

        /// <summary>
        /// Not Implemented: Returns the trimmed curve, wrapping around the end if start is greater than end.
        /// </summary>
        /// <param name="start">The starting parameter</param>
        /// <param name="end">The ending parameter</param>
        /// <returns>The trimmed curve</returns>
        public ICurve TrimWithWrap(double start, double end) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A tree of ParallelogramNodes covering the curve. 
        /// This tree is used in curve intersections routines.
        /// </summary>
        /// <value></value>
        public ParallelogramNodeOverICurve ParallelogramNodeOverICurve {
            get {
                Point side = 0.5 * (b - a);

                return new ParallelogramLeaf(0, 1, new Parallelogram(a, side, side), this, 0);
            }
        }

        internal Point Normal {
            get {
                Point t = a - b;
                t /= t.Length;
                return new Point(-t.Y, t.X);
            }
        }
        /// <summary>
        /// construct a line segment
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public LineSegment(Point start, Point end) {
#if SHARPKIT
            a = start.Clone();
            b = end.Clone();
#else
            a = start;
            b = end;
#endif
        }

        /// <summary>
        /// constructs a line segment
        /// </summary>
        /// <param name="a">the first point</param>
        /// <param name="x">x-coordinate of the second point</param>
        /// <param name="y">y-coordinate of the second point</param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a"), SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y")]
        public LineSegment(Point a, double x, double y) : this(a, new Point(x, y)) { }

        /// <summary>
        /// constructs a line segment
        /// </summary>
        /// <param name="x0">x-coordinate of the first point</param>
        /// <param name="y0">y-coordinate of the first point</param>
        /// <param name="x1">x-coordinate of the second point</param>
        /// <param name="y1">y-coordinate of the second point</param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y"), SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
        public LineSegment(double x0, double y0, double x1, double y1) : this(new Point(x0, y0), new Point(x1, y1)) { }

        /// <summary>
        /// Returns the point on the curve corresponding to parameter t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point this[double t] {
            get { return a + (b - a) * t; }
        }

        /// <summary>
        /// first derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point Derivative(double t) {
            return b - a;
        }

        /// <summary>
        /// second derivative
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point SecondDerivative(double t) {
            return new Point(0, 0);
        }

        /// <summary>
        /// third derivative
        /// </summary>
        /// <param name="t">the parameter of the derivative</param>
        /// <returns></returns>
        public Point ThirdDerivative(double t) {
            return new Point(0, 0);
        }

        /// <summary>
        /// This is the rounded form; DebuggerDisplay shows the unrounded form.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return "{" + a + "," + b + "}";
        }

        /// <summary>
        /// this[Reverse[t]]=this[ParEnd+ParStart-t]
        /// </summary>
        /// <returns></returns>

        public ICurve Reverse() {
            return new LineSegment(b, a);
        }

        /*      
          static internal IntersectionInfo Cross(LineSeg coeff, LineSeg side1){
            IntersectionInfo xx=CrossTwoLines(coeff.Start, coeff.End-coeff.Start,side1.Start, side1.End-side1.Start);
            if (xx == null)
            {
              //parallel segs
              Point adir=coeff.d1(0);
              Point bdir=side1.d1(0);

              if (adir.Length > bdir.Length)
              {
                if (adir.Length > Curve.DistEps)
                {
                  adir = adir.Normalize();
                  if(Math.Abs((coeff-side1)*adir<Curve.DistEps)){

                  }
                }
              }
              return null;
            }

            if(xx.Par0>1){
              if (ApproximateComparer.Close(coeff.End, xx.X))
              {
                xx.X = coeff.End;
                xx.Par0 = 1;
              }
              else
                return null;
            }
            else if(xx.Par0<0){
              if(ApproximateComparer.Close(coeff.Start,xx.X)){
                xx.X=coeff.Start; 
                xx.Par0=1;
              }
              else
                return null;
            }

            if (xx.Par1 > 1)
            {
              if (ApproximateComparer.Close(side1.End, xx.X))
              {
                xx.X = coeff.End;
                xx.Par1 = 1;
              }
              else
                return null;
            }
            else if (xx.Par1 < 0)
            {
              if (ApproximateComparer.Close(side1.Start, xx.X))
              {
                xx.X = coeff.Start;
                xx.Par1 = 1;
              }
              else
                return null;
            }

            return xx;
          }
         * */

        /// <summary>
        /// Returns the curved moved by delta
        /// </summary>
        public void Translate(Point delta)
        {
            a += delta;
            b += delta;
        }

        /// <summary>
        /// Scale (multiply) from zero by x and y
        /// </summary>
        /// <param name="xScale"></param>
        /// <param name="yScale"></param>
        /// <returns>scaled copy</returns>
        public ICurve ScaleFromOrigin(double xScale, double yScale)
        {
            return new LineSegment(Point.Scale(xScale, yScale, a), Point.Scale(xScale, yScale, b));
        }

        /// <summary>
        /// gets the parameter at a specific length from the start along the curve
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public double GetParameterAtLength(double length) {
            var len = (b - a).Length;
            if (len < ApproximateComparer.Tolerance)
                return 0;
            var t = length/len;
            return t > 1 ? 1 : (t < 0 ? 0 : t);
        }

        /// <summary>
        /// Return the transformed curve
        /// </summary>
        /// <param name="transformation"></param>
        /// <returns>the transformed curve</returns>
        public ICurve Transform(PlaneTransformation transformation) {
            return new LineSegment(transformation * a, transformation * b);
        }

        /// <summary>
        /// returns a parameter t such that the distance between curve[t] and targetPoint is minimal 
        /// and t belongs to the closed segment [low,high]
        /// </summary>
        /// <param name="targetPoint">the point to find the closest point</param>
        /// <param name="high">the upper bound of the parameter</param>
        /// <param name="low">the low bound of the parameter</param>
        /// <returns></returns>
        public double ClosestParameterWithinBounds(Point targetPoint, double low, double high) {
            var t = ClosestParameter(targetPoint);
            if (t < low)
                t = low;
            if(t>high)
                t=high;
            return t;
        }

        /// <summary>
        /// return length of the curve segment [start,end] 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public double LengthPartial(double start, double end) {
            return (this[end] - this[start]).Length;
        }

        /// <summary>
        /// Get the length of the curve
        /// </summary>
        public double Length {
            get {
                return (a - b).Length;
            }
        }
        /// <summary>
        /// The bounding box of the line
        /// </summary>
        public Rectangle BoundingBox {
            get {
                return new Rectangle(Start, End);
            }
        }



        /// <summary>
        /// clones the curve. 
        /// </summary>
        /// <returns>the cloned curve</returns>

        public ICurve Clone() {
            return new LineSegment(this.a, this.b);
        }

        /// <summary>
        /// returns a parameter t such that the distance between curve[t] and a is minimal
        /// </summary>
        /// <param name="targetPoint"></param>
        /// <returns></returns>
        public double ClosestParameter(Point targetPoint) {
            return Point.ClosestParameterOnLineSegment(targetPoint, Start, End);
        }
        /// <summary>
        /// left derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point LeftDerivative(double t) {
            return Derivative(t);
        }

        /// <summary>
        /// right derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point RightDerivative(double t) {
            return Derivative(t);
        }
        /// <summary>
        /// returns true if segments are not parallel and are intesecting
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "d"), SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "c"), SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b"), SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a"), SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "4#")]
        public static bool Intersect(Point a, Point b, Point c, Point d, out Point x) {
            if (!Point.LineLineIntersection(a, b, c, d, out x))
                return false;
            return XIsBetweenPoints(ref a, ref b, ref x) && XIsBetweenPoints(ref c, ref d, ref x);
        }

        static bool XIsBetweenPoints(ref Point a, ref Point b, ref Point x) {
            return (a - x) * (b - x) <= ApproximateComparer.DistanceEpsilon;
        }

#region ICurve Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double Curvature(double t) {
            return 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double CurvatureDerivative(double t) {
            return 0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double CurvatureSecondDerivative(double t) {
            throw new NotImplementedException();
        }

#endregion

        /// <summary>
        /// [a,b] and [c,d] are the segments. u and v are the corresponding closest point params
        /// see http://www.geometrictools.com/Documentation/DistanceLine3Line3.pdf
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="parab"></param>
        /// <param name="parcd"></param>
        internal static double MinDistBetweenLineSegments(Point a, Point b, Point c, Point d, out double parab, out double parcd) {
            Point u = b - a;
            Point v = d - c;
            Point w = a - c;

            double D = Point.CrossProduct(u, v);

            double uu = u * u;        // always >= 0
            double uv = u * v;
            double vv = v * v;        // always >= 0
            double uw = u * w;
            double vw = v * w;
            double sN, tN;
            double absD = Math.Abs(D);
            double sD = absD, tD = absD;

            // compute the line parameters of the two closest points
            if (absD < ApproximateComparer.Tolerance) { // the lines are almost parallel
                sN = 0.0;        // force using point a on segment [a..b]
                sD = 1.0;        // to prevent possible division by 0.0 later
                tN = vw;
                tD = vv;
            }
            else {                // get the closest points on the infinite lines
                sN = Point.CrossProduct(v, w);
                tN = Point.CrossProduct(u, w);
                if (D < 0) {
                    sN = -sN;
                    tN = -tN;                  
                }

                if (sN < 0.0) {       // parab < 0 => the s=0 edge is visible
                    sN = 0.0;
                    tN = vw;
                    tD = vv;
                }
                else if (sN > sD) {  // parab > 1 => the s=1 edge is visible
                    sN = sD = 1;
                    tN = vw + uv;
                    tD = vv;
                }
            }

            if (tN < 0.0) {           // tc < 0 => the t=0 edge is visible
                tN = 0.0;
                // recompute parab for this edge
                if (-uw < 0.0)
                    sN = 0.0;
                else if (-uw > uu)
                    sN = sD;
                else {
                    sN = -uw;
                    sD = uu;
                }
            }
            else if (tN > tD) {      // tc > 1 => the t=1 edge is visible
                tN = tD = 1;
                // recompute parab for this edge
                if ((-uw + uv) < 0.0)
                    sN = 0;
                else if ((-uw + uv) > uu)
                    sN = sD;
                else {
                    sN = (-uw + uv);
                    sD = uu;
                }
            }

            // finally do the division to get parameters
            parab = (Math.Abs(sN) < ApproximateComparer.Tolerance ? 0.0 : sN / sD);
            parcd = (Math.Abs(tN) < ApproximateComparer.Tolerance ? 0.0 : tN / tD);

            // get the difference of the two closest points
            Point dP = w + (parab * u) - (parcd * v);

            return dP.Length;   // return the closest distance
        }
    }
}

