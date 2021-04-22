using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// class representing a polyline
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"),
     SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
#if TEST_MSAGL
    [Serializable]
#endif
    public class Polyline : ICurve, IEnumerable<Point> {
        bool needToInit = true;

        /// <summary>
        /// 
        /// </summary>
        internal void RequireInit() {
            needToInit = true;
        }

        bool NeedToInit {
            get {
                return needToInit;
            }
            set {
                needToInit = value;
            }
        }

		/// <summary>
		/// 
		/// </summary>
		public IEnumerable<PolylinePoint> PolylinePoints {
            get {
                PolylinePoint p = StartPoint;
                while (p != null) {
                    yield return p;
                    p = p.Next;
                }
            }
        }
#if TEST_MSAGL
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object,System.Object)")]
        public override string ToString() {
            return String.Format("{0},{1},count={2}", Start,End, Count);
        }
#endif 
        internal Curve ToCurve() {
            var c = new Curve();
            Curve.AddLineSegment(c, StartPoint.Point, StartPoint.Next.Point);
            PolylinePoint p = StartPoint.Next;
            while ((p = p.Next) != null)
                Curve.ContinueWithLineSegment(c, p.Point);
            if (Closed)
                Curve.ContinueWithLineSegment(c, StartPoint.Point);
            return c;
        }


        ParallelogramInternalTreeNode pBNode;

        PolylinePoint startPoint;

		/// <summary>
		/// 
		/// </summary>
		public PolylinePoint StartPoint {
            get { return startPoint; }
            set {
                RequireInit();
                startPoint = value;
            }
        }

        PolylinePoint endPoint;

		/// <summary>
		/// 
		/// </summary>
		public PolylinePoint EndPoint {
            get { return endPoint; }
            set {
                RequireInit();
                endPoint = value;
            }
        }

        int count;

        internal int Count {
            get {
                if (needToInit)
                    Init();
                return count;
            }            
        }

        bool closed;

        /// <summary>
        /// 
        /// </summary>
        public bool Closed {
            get { return closed; }
            set {
                if (closed != value) {
                    closed = value;
                    RequireInit();
                }
            }
        }

        #region ICurve Members

        /// <summary>
        /// the value of the curve at the parameter
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point this[double t] {
            get {
                Point a, b;
                if (NeedToInit)
                    Init();
                GetAdjustedParamAndStartEndPoints(ref t, out a, out b);
                return (1 - t)*a + t*b;
            }
        }

        void GetAdjustedParamAndStartEndPoints(ref double t, out Point a, out Point b) {
            Debug.Assert(t >= -ApproximateComparer.Tolerance);
            Debug.Assert(StartPoint != null);
            PolylinePoint s = StartPoint;

            while (s.Next != null) {
                if (t <= 1) {
                    a = s.Point;
                    b = s.Next.Point;
                    return;
                }
                s = s.Next;
                t -= 1;
            }

            if (Closed) {
                if (t <= 1) {
                    a = EndPoint.Point;
                    b = StartPoint.Point;
                    return;
                }
            }

            throw new InvalidOperationException(); //"out of the parameter domain");
        }


        /// <summary>
        /// first derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point Derivative(double t) {
            Point a, b;
            if (NeedToInit)
                Init();
            GetAdjustedParamAndStartEndPoints(ref t, out a, out b);
            return b - a;
        }


        /// <summary>
        /// left derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point LeftDerivative(double t) {
            if(NeedToInit)
                Init();
            PolylinePoint pp = TryToGetPolylinePointCorrespondingToT(t);
            if (pp == null)
                return Derivative(t);
            PolylinePoint prev = TryToGetPrevPointToPolylinePoint(pp);
            if (prev != null)
                return pp.Point - prev.Point;
            return pp.Next.Point - pp.Point;
        }

        /// <summary>
        /// right derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point RightDerivative(double t) {
            if(NeedToInit)
                Init();
            var pp = TryToGetPolylinePointCorrespondingToT(t);
            if (pp == null)
                return Derivative(t);
            PolylinePoint next = TryToGetNextPointToPolylinePoint(pp);
            if (next != null)
                return next.Point - pp.Point;
            return pp.Point - pp.Prev.Point;
        }


        PolylinePoint TryToGetPolylinePointCorrespondingToT(double t) {
            for (PolylinePoint p = StartPoint; p != null; p = p.Next, t--)
                if (Math.Abs(t) < ApproximateComparer.Tolerance)
                    return p;
            return null;
        }

        PolylinePoint TryToGetPrevPointToPolylinePoint(PolylinePoint p) {
            if (p != StartPoint)
                return p.Prev;

            if (!Closed)
                return null;

            return EndPoint;
        }


        PolylinePoint TryToGetNextPointToPolylinePoint(PolylinePoint p) {
            if (p != EndPoint)
                return p.Next;

            if (!Closed)
                return null;

            return StartPoint;
        }


        /// <summary>
        /// second derivative
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point SecondDerivative(double t) {
            return new Point();
        }

        /// <summary>
        /// third derivative
        /// </summary>
        /// <param name="t">the parameter of the derivative</param>
        /// <returns></returns>
        public Point ThirdDerivative(double t)
        {
            return new Point();
        }


        /// <summary>
        /// A tree of ParallelogramNodes covering the curve. 
        /// This tree is used in curve intersections routines.
        /// </summary>
        /// <value></value>
        public ParallelogramNodeOverICurve ParallelogramNodeOverICurve {
            get {
#if PPC
                lock(this){
#endif
                if(NeedToInit)
                    Init();
                return pBNode;

                
                
#if PPC
                }
#endif

            }
        }

        static Parallelogram ParallelogramOfLineSeg(Point a, Point b) {
            Point side = 0.5*(b - a);
            return new Parallelogram(a, side, side);
        }

        Rectangle boundingBox = Rectangle.CreateAnEmptyBox();

        /// <summary>
        /// bounding box of the polyline
        /// </summary>
        public Rectangle BoundingBox {
            get {
#if PPC
                lock(this){
#endif
                if (NeedToInit)
                    Init();
                
                return boundingBox;
#if PPC
                }
#endif
            }
        }

        void Init() {
            
            boundingBox = new Rectangle(StartPoint.Point);
            count = 1;
            foreach (Point p in this.Skip(1)) {
                boundingBox.Add(p);
                count++;
            }

            CalculatePbNode();

            NeedToInit = false;
        }

        void CalculatePbNode() {
            pBNode = new ParallelogramInternalTreeNode(this, ParallelogramNodeOverICurve.DefaultLeafBoxesOffset);
            var parallelograms = new List<Parallelogram>();
            PolylinePoint pp = StartPoint;
            int offset = 0;
            while (pp.Next != null) {
                Parallelogram parallelogram = ParallelogramOfLineSeg(pp.Point, pp.Next.Point);
                parallelograms.Add(parallelogram);
                pBNode.AddChild(new ParallelogramLeaf(offset, offset + 1, parallelogram, this, 0));
                pp = pp.Next;
                offset++;
            }

            if (Closed) {
                Parallelogram parallelogram = ParallelogramOfLineSeg(EndPoint.Point, StartPoint.Point);
                parallelograms.Add(parallelogram);
                pBNode.AddChild(new ParallelogramLeaf(offset, offset + 1, parallelogram, this, 0));
            }

            pBNode.Parallelogram = Parallelogram.GetParallelogramOfAGroup(parallelograms);
        }

        /// <summary>
        /// the start of the parameter domain
        /// </summary>
        public double ParStart {
            get { return 0; }
        }

        /// <summary>
        /// the end of the parameter domain
        /// </summary>
        public double ParEnd {
            get { return Closed ? Count : Count - 1; }
        }

        /// <summary>
        /// Returns the trimmed polyline. Does not change this polyline. Reversed start and end if start is less than end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public ICurve Trim(double start, double end) {
            //this is a very lazy version!
            Curve curve = ToCurve();
            curve = (Curve) curve.Trim(start, end);

            return PolylineFromCurve(curve);
        }

        /// <summary>
        /// Returns the trimmed polyline, wrapping around the end if start is greater than end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public ICurve TrimWithWrap(double start, double end) {
            Debug.Assert((start < end) || this.Closed, "Polyline must be closed to wrap");

            //this is a very lazy version!
            var curve = (Curve)this.ToCurve().TrimWithWrap(start, end);
            return PolylineFromCurve(curve);
        }

        internal static Polyline PolylineFromCurve(Curve curve) {
            var ret = new Polyline();
            ret.AddPoint(curve.Start);
            foreach (var ls in curve.Segments)
                ret.AddPoint(ls.End);
            ret.Closed = curve.Start == curve.End;
            return ret;
        }

        /// <summary>
        /// Returns the curved moved by delta
        /// </summary>
        public void Translate(Point delta)
        {
            PolylinePoint polyPoint = StartPoint;
            while (polyPoint != null)
            {
                polyPoint.Point += delta;
                polyPoint = polyPoint.Next;
            }

            RequireInit();
        }

        /// <summary>
        /// Returns the curved with all points scaled from the original by x and y
        /// </summary>
        /// <param name="xScale"></param>
        /// <param name="yScale"></param>
        /// <returns></returns>
        public ICurve ScaleFromOrigin(double xScale, double yScale)
        {
            var ret = new Polyline();
            PolylinePoint polyPoint = StartPoint;
            while (polyPoint != null)
            {
                ret.AddPoint(Point.Scale(xScale, yScale, polyPoint.Point));
                polyPoint = polyPoint.Next;
            }
            ret.Closed = Closed;
            return ret;
        }

        internal void AddPoint(double x, double y) {
            AddPoint(new Point(x, y));
        }

        internal void PrependPoint(Point p) {
            Debug.Assert(EndPoint == null || !ApproximateComparer.Close(p, EndPoint.Point));           
            var pp = new PolylinePoint(p) {Polyline = this};
            if (StartPoint != null) {
                if (!ApproximateComparer.Close(p, StartPoint.Point))
                {
                    StartPoint.Prev = pp;
                    pp.Next = StartPoint;
                    StartPoint = pp;
                }
            } else {
                StartPoint = EndPoint = pp;
            }
            RequireInit();
        }

        ///<summary>
        ///adds a point to the polyline
        ///</summary>
        ///<param name="point"></param>
        public void AddPoint(Point point) {
            var pp = new PolylinePoint(point) {Polyline = this};
            if (EndPoint != null) {
               // if (!ApproximateComparer.Close(point, EndPoint.Point)) {
                    EndPoint.Next = pp;
                    pp.Prev = EndPoint;
                    EndPoint = pp;
               // }
            } else {
                StartPoint = EndPoint = pp;
            }
            RequireInit();
        }

        /// <summary>
        /// this[ParStart]
        /// </summary>
        public Point Start
        {
            get { return StartPoint.Point; }
        }

        /// <summary>
        /// this[ParEnd]
        /// </summary>
        public Point End
        {
            get { return EndPoint.Point; }
        }

        /// <summary>
        /// this[Reverse[t]]=this[ParEnd+ParStart-t]
        /// </summary>
        /// <returns></returns>
        public ICurve Reverse()
        {
            return ReversePolyline();
        }

        /// <summary>
        /// Offsets the curve in the direction of dir
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public ICurve OffsetCurve(double offset, Point dir)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// return length of the curve segment [start,end] 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public double LengthPartial(double start, double end)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the length of the curve
        /// </summary>
        public double Length
        {
            get {
                double ret = 0;
                if (StartPoint != null && StartPoint.Next != null) {
                    PolylinePoint p = StartPoint.Next;
                    do {
                        ret += (p.Point - p.Prev.Point).Length;
                        p = p.Next;
                    } while (p != null);
                }
                return ret;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public double GetParameterAtLength(double length) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// returns the transformed polyline
        /// </summary>
        /// <param name="transformation"></param>
        /// <returns></returns>
      public ICurve Transform(PlaneTransformation transformation) {
            if (transformation == null)
                return this;
            var poly = new Polyline {Closed = Closed};
            foreach (var p in this)
            {
                poly.AddPoint(transformation*p);
            }
            return poly;
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
            double ret = 0;
            double dist = Double.MaxValue;
            int offset = 0;
            PolylinePoint pp = StartPoint;
            while (pp.Next != null) {
                if (offset <= high && offset + 1 >= low) {
                    var lowLocal = Math.Max(0, low - offset);
                    var highLocal = Math.Min(1, high - offset);
                    var ls = new LineSegment(pp.Point, pp.Next.Point);
                    double t = ls.ClosestParameterWithinBounds(targetPoint, lowLocal, highLocal);
                    Point delta = ls[t] - targetPoint;
                    double newDist = delta*delta;
                    if (newDist < dist) {
                        dist = newDist;
                        ret = t + offset;
                    }
                }
                pp = pp.Next;
                offset++;
            }

            if (Closed) {
                if (offset <= high && offset + 1 >= low) {
                    var lowLocal = Math.Max(0, low - offset);
                    var highLocal = Math.Min(1, high - offset);
                    var ls = new LineSegment(EndPoint.Point, StartPoint.Point);
                    double t = ls.ClosestParameterWithinBounds(targetPoint, lowLocal, highLocal);
                    Point delta = ls[t] - targetPoint;
                    double newDist = delta*delta;
                    if (newDist < dist)
                        ret = t + offset;
                }
            }
            return ret;
        }

        /// <summary>
        /// gets the parameter of the closest point
        /// </summary>
        /// <param name="targetPoint"></param>
        /// <returns></returns>
        public double ClosestParameter(Point targetPoint) {
            double ret = 0;
            double dist = Double.MaxValue;
            int offset = 0;
            PolylinePoint pp = StartPoint;
            while (pp.Next != null) {
                var ls = new LineSegment(pp.Point, pp.Next.Point);
                double t = ls.ClosestParameter(targetPoint);
                Point delta = ls[t] - targetPoint;
                double newDist = delta*delta;
                if (newDist < dist) {
                    dist = newDist;
                    ret = t + offset;
                }
                pp = pp.Next;
                offset++;
            }

            if (Closed) {
                var ls = new LineSegment(EndPoint.Point, StartPoint.Point);
                double t = ls.ClosestParameter(targetPoint);
                Point delta = ls[t] - targetPoint;
                double newDist = delta*delta;
                if (newDist < dist)
                    ret = t + offset;
            }
            return ret;
        }

        /// <summary>
        /// clones the curve. 
        /// </summary>
        /// <returns>the cloned curve</returns>
        ICurve ICurve.Clone() {
            var ret = new Polyline();
            foreach (Point p in this)
                ret.AddPoint(p);
            ret.Closed = Closed;
            return ret;
        }

        #endregion

        #region IEnumerable<Point> Members

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=332
        public IEnumerator<Point> GetEnumerator()
        {
#else
        IEnumerator<Point> IEnumerable<Point>.GetEnumerator() {
#endif
            return new PolylineIterator(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() {
            return new PolylineIterator(this);
        }

        #endregion

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Polyline ReversePolyline() {
            var ret = new Polyline();
            PolylinePoint pp = EndPoint;
            while (pp.Prev != null) {
                ret.AddPoint(pp.Point);
                pp = pp.Prev;
            }
            ret.AddPoint(StartPoint.Point);
            ret.Closed = Closed;
            return ret;
        }

        internal PolylinePoint Next(PolylinePoint a) {
            return a.Next ?? (Closed ? StartPoint : null);
        }

        internal PolylinePoint Prev(PolylinePoint a) {
            return a.Prev ?? (Closed ? EndPoint : null);
        }

        /// <summary>
        /// creates a polyline from a point enumeration
        /// </summary>
        /// <param name="points"></param>
        public Polyline(IEnumerable<Point> points) {
            ValidateArg.IsNotNull(points, "points");
            foreach (var p in points)
                AddPoint(p);
        }

        /// <summary>
        /// creating a polyline from two points
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b"),
         SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
        public Polyline(Point a, Point b) {
            AddPoint(a);
            AddPoint(b);
        }


        /// <summary>
        /// an empty constructor
        /// </summary>
        public Polyline() {}

        ///<summary>
        ///</summary>
        ///<param name="points"></param>
#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=339
        [SharpKit.JavaScript.JsMethod(NativeParams = false)]
#endif
        public Polyline(params Point[] points) : this((IEnumerable<Point>)points) { }

        /// <summary>
        /// true in general for convex polylines
        /// </summary>
        /// <returns></returns>
        internal bool IsClockwise() {
            return Point.GetTriangleOrientation(StartPoint.Point, StartPoint.Next.Point, StartPoint.Next.Next.Point) ==
                   TriangleOrientation.Clockwise;
        }

        internal void RemoveStartPoint() {
            PolylinePoint p = StartPoint.Next;
            p.Prev = null;
            StartPoint = p;
            RequireInit();
        }

        internal void RemoveEndPoint() {
            PolylinePoint p = EndPoint.Prev;
            p.Next = null;
            EndPoint = p;
            RequireInit();
        }

        /// <summary>
        /// Returns the point location value. The assumption is that the polyline goes clockwise and is closed and convex.
        /// </summary>
        /// <param name="point">Point to find.</param>
        /// <param name="witness">if the point belongs to the boundary then witness is
        ///         the first point of the boundary segment containing p </param>
        /// <returns></returns>
        internal PointLocation GetPointLocation(Point point, out PolylinePoint witness) {
            Debug.Assert(Closed && IsClockwise());
            witness = null;

            foreach (PolylinePoint polyPoint in PolylinePoints) {
                PolylinePoint secondPoint = Next(polyPoint);
                TriangleOrientation triangleOrientation = Point.GetTriangleOrientation(point, polyPoint.Point,
                                                                                       secondPoint.Point);
                if (triangleOrientation == TriangleOrientation.Counterclockwise)
                    return PointLocation.Outside;
                if (triangleOrientation == TriangleOrientation.Collinear)
                    if ((point - polyPoint.Point)*(secondPoint.Point - point) >= 0) {
                        witness = polyPoint;
                        return PointLocation.Boundary;
                    }
            }

            return PointLocation.Inside;
        }

        /// <summary>
        /// Returns the point location value and the edge containing it if it belongs to a boundary. 
        /// The assumption is that the polyline goes clockwise and is closed and convex.
        /// </summary>
        /// <param name="point">Point to find</param>
        /// <param name="edgeStart">The starting point of the boundary hit, if any</param>
        /// <param name="edgeEnd">The ending point of the boundary hit, if any</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public PointLocation GetPointLocation(Point point, out Point edgeStart, out Point edgeEnd) {
            PolylinePoint start;
            edgeStart = new Point();
            edgeEnd = new Point();
            PointLocation loc = GetPointLocation(point, out start);
            if (PointLocation.Boundary == loc) {
                edgeStart = start.Point;
                edgeEnd = start.NextOnPolyline.Point;
            }
            return loc;
        }

        /// <summary>
        /// shift the given polyline by delta
        /// </summary>
        /// <param name="delta"></param>
        public void Shift(Point delta) {
            for (PolylinePoint pp = StartPoint; pp != null; pp = pp.Next)
                pp.Point += delta;
        }

        #region ICurve Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double Curvature(double t) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double CurvatureDerivative(double t) {
            throw new NotImplementedException();
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

        internal void AddRangeOfPoints(IEnumerable<Point> points){
            foreach (var point in points)
                AddPoint(point);
        }
    }
}
