/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Diagnostics;

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
            a = start;
            b = end;
        }

        /// <summary>
        /// constructs a line segment
        /// </summary>
        /// <param name="a">the first point</param>
        /// <param name="x">x-coordinate of the second point</param>
        /// <param name="y">y-coordinate of the second point</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y")]
        public LineSegment(Point a, double x, double y) : this(a, new Point(x, y)) { }

        /// <summary>
        /// constructs a line segment
        /// </summary>
        /// <param name="x0">x-coordinate of the first point</param>
        /// <param name="y0">y-coordinate of the first point</param>
        /// <param name="x1">x-coordinate of the second point</param>
        /// <param name="y1">y-coordinate of the second point</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "d"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "c"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "4#")]
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
    }
}

