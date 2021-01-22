using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Msagl.Core.Geometry.Curves{
    /// <summary>
    /// A class representing an ellipse.
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    public class Ellipse : ICurve{
#if TEST_MSAGL
        /// <summary>
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object[])")]
        public override string ToString() {
            return String.Format("{0} {1} from {2} to {3} a0={4} a1={5}", Start, End, ParStart, ParEnd, AxisA, AxisB);
        }
#endif
        Rectangle box;

        ParallelogramNodeOverICurve parallelogramNodeOverICurve;

        /// <summary>
        /// offsets the curve in the given direction
        /// </summary>
        /// <param name="offset">the width of the offset</param>
        /// <param name="dir">the direction of the offset</param>
        /// <returns></returns>
        public ICurve OffsetCurve(double offset, Point dir){
            //is dir inside or outside
            Point d = dir - center;
            double angle = Point.Angle(aAxis, d);
            Point s = aAxis*Math.Cos(angle) + bAxis*Math.Sin(angle);
            if(s.Length < d.Length){
                double al = aAxis.Length;
                double bl = bAxis.Length;
                return new Ellipse((al + offset)*aAxis.Normalize(), (bl + offset)*bAxis.Normalize(), center);
            }
            {
                double al = aAxis.Length;
                double bl = bAxis.Length;
#if DEBUGCURVES
        if (al < offset || bl < offset)
          throw new Exception("wrong parameter for ellipse offset");
#endif
                return new Ellipse((al - offset)*aAxis.Normalize(), (bl - offset)*bAxis.Normalize(), center);
            }
        }

        /// <summary>
        /// Reverse the ellipe: not implemented.
        /// </summary>
        /// <returns>returns the reversed curve</returns>
        public ICurve Reverse(){
            return null; // throw new Exception("not implemented");
        }

        /// <summary>
        /// Returns the start point of the curve
        /// </summary>
        public Point Start{
            get { return this[ParStart]; }
        }

        /// <summary>
        /// Returns the end point of the curve
        /// </summary>
        public Point End{
            get { return this[ParEnd]; }
        }

        /// <summary>
        /// Trims the curve
        /// </summary>
        /// <param name="start">the trim start parameter</param>
        /// <param name="end">the trim end parameter</param>
        /// <returns></returns>
        public ICurve Trim(double start, double end){
            Debug.Assert(start<=end);
            Debug.Assert(start>=ParStart-ApproximateComparer.Tolerance);
            Debug.Assert(end<=ParEnd+ApproximateComparer.Tolerance);
            
            return new Ellipse(Math.Max(start, ParStart), Math.Min(end,ParEnd), AxisA, AxisB, center);
        }

        /// <summary>
        /// Not Implemented: Returns the trimmed curve, wrapping around the end if start is greater than end.
        /// </summary>
        /// <param name="start">The starting parameter</param>
        /// <param name="end">The ending parameter</param>
        /// <returns>The trimmed curve</returns>
        public ICurve TrimWithWrap(double start, double end)
        {
            throw new NotImplementedException();
        }

        //half x axes
        Point aAxis;

        /// <summary>
        /// the X axis of the ellipse
        /// </summary>
        public Point AxisA{
            get { return aAxis; }
            set { aAxis = value; }
        }

        Point bAxis;

        /// <summary>
        /// the Y axis of the ellipse
        /// </summary>
        public Point AxisB{
            get { return bAxis; }
            set { bAxis = value; }
        }

        Point center;

        /// <summary>
        /// the center of the ellipse
        /// </summary>
        public Point Center{
            get { return center; }
            set { center = value; }
        }

        /// <summary>
        /// The bounding box of the ellipse
        /// </summary>
        public Rectangle BoundingBox{
            get { return box; }
        }

        /// <summary>
        /// Returns the point on the curve corresponding to parameter t
        /// </summary>
        /// <param name="t">the parameter of the derivative</param>
        /// <returns></returns>
        public Point this[double t]{
            get { return center + Math.Cos(t)*aAxis + Math.Sin(t)*bAxis; }
        }

        /// <summary>
        /// first derivative
        /// </summary>
        /// <param name="t">the p</param>
        /// <returns></returns>
        public Point Derivative(double t){
            return -Math.Sin(t)*aAxis + Math.Cos(t)*bAxis;
        }

        /// <summary>
        /// second derivative
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point SecondDerivative(double t){
            return -Math.Cos(t)*aAxis - Math.Sin(t)*bAxis;
        }

        /// <summary>
        /// third derivative
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point ThirdDerivative(double t){
            return Math.Sin(t)*aAxis - Math.Cos(t)*bAxis;
        }

        /// <summary>
        /// a tree of ParallelogramNodes covering the edge
        /// </summary>
        /// <value></value>
        public ParallelogramNodeOverICurve ParallelogramNodeOverICurve{
            get{
#if PPC
                lock(this){
#endif
                if(parallelogramNodeOverICurve != null)

                    return parallelogramNodeOverICurve;
                return parallelogramNodeOverICurve = CreateParallelogramNodeForCurveSeg(this);
#if PPC
                }
#endif
            }
        }


        static ParallelogramNodeOverICurve CreateNodeWithSegmentSplit(double start, double end, Ellipse seg, double eps){
            var pBNode = new ParallelogramInternalTreeNode(seg, eps);
            pBNode.AddChild(CreateParallelogramNodeForCurveSeg(start, 0.5*(start + end), seg, eps));
            pBNode.AddChild(CreateParallelogramNodeForCurveSeg(0.5*(start + end), end, seg, eps));
            var boxes = new List<Parallelogram>();
            boxes.Add(pBNode.Children[0].Parallelogram);
            boxes.Add(pBNode.Children[1].Parallelogram);
            pBNode.Parallelogram = Parallelogram.GetParallelogramOfAGroup(boxes);
            return pBNode;
        }

        internal static ParallelogramNodeOverICurve CreateParallelogramNodeForCurveSeg(double start, double end,
                                                                                       Ellipse seg, double eps){
            bool closedSeg = (start == seg.ParStart && end == seg.ParEnd && ApproximateComparer.Close(seg.Start, seg.End));
            if(closedSeg)
                return CreateNodeWithSegmentSplit(start, end, seg, eps);

            Point s = seg[start];
            Point e = seg[end];
            Point w = e - s;
            Point middle = seg[(start + end) / 2];
           
            if (ParallelogramNodeOverICurve.DistToSegm(middle, s, e) <= ApproximateComparer.IntersectionEpsilon &&
                w * w < Curve.LineSegmentThreshold * Curve.LineSegmentThreshold && end - start < Curve.LineSegmentThreshold) {
                var ls = new LineSegment(s, e);
                var leaf = ls.ParallelogramNodeOverICurve as ParallelogramLeaf;
                leaf.Low = start;
                leaf.High = end;
                leaf.Seg = seg;
                leaf.Chord = ls;
                return leaf;
            }

            bool we = WithinEpsilon(seg, start, end, eps);
            var box = new Parallelogram();

            if(we && CreateParallelogramOnSubSeg(start, end, seg, ref box)){
                return new ParallelogramLeaf(start, end, box, seg, eps);
            } else{
                return CreateNodeWithSegmentSplit(start, end, seg, eps);
            }
        }

        internal static bool CreateParallelogramOnSubSeg(double start, double end, Ellipse seg, ref Parallelogram box){
            Point tan1 = seg.Derivative(start);

            Point tan2 = seg.Derivative(end);
            Point tan2Perp = Point.P(-tan2.Y, tan2.X);

            Point corner = seg[start];

            Point e = seg[end];

            Point p = e - corner;

            double numerator = p*tan2Perp;
            double denumerator = (tan1*tan2Perp);
            double x; // = (p * tan2Perp) / (tan1 * tan2Perp);
            if(Math.Abs(numerator) < ApproximateComparer.DistanceEpsilon)
                x = 0;
            else if(Math.Abs(denumerator) < ApproximateComparer.DistanceEpsilon){
                //it is degenerated; adjacent sides are parallel, but 
                //since p * tan2Perp is big it does not contain e
                return false;
            } else x = numerator/denumerator;

            tan1 *= x;

            box = new Parallelogram(corner, tan1, e - corner - tan1);
#if DEBUGCURVES
      if (!box.Contains(seg[end]))
      {
      
        throw new InvalidOperationException();//"the box does not contain the end of the segment");
      }
#endif

            return true;
        }

        static bool WithinEpsilon(Ellipse seg, double start, double end, double eps){
            int n = 3; //hack !!!! but maybe can be proven for Bezier curves and other regular curves
            double d = (end - start)/n;
            Point s = seg[start];
            Point e = seg[end];

            double d0 = ParallelogramNodeOverICurve.DistToSegm(seg[start + d], s, e);
            if(d0 > eps)
                return false;

            double d1 = ParallelogramNodeOverICurve.DistToSegm(seg[start + d*(n - 1)], s, e);

            return d1 <= eps;
        }

        static ParallelogramNodeOverICurve CreateParallelogramNodeForCurveSeg(Ellipse seg) {
            return CreateParallelogramNodeForCurveSeg(seg.ParStart, seg.ParEnd, seg,
                                                      ParallelogramNodeOverICurve.DefaultLeafBoxesOffset);
        }

        double parStart;

        /// <summary>
        /// the start of the parameter domain
        /// </summary>   
        public double ParStart{
            get { return parStart; }
            set { parStart = value; }
        }

        double parEnd;

        /// <summary>
        /// the end of the parameter domain
        /// </summary>
        public double ParEnd{
            get { return parEnd; }
            set { parEnd = value; }
        }

        /// <summary>
        /// The point on the ellipse corresponding to the parameter t is calculated by 
        /// the formula center + cos(t)*axis0 + sin(t) * axis1.
        /// To get an ellipse rotating clockwise use, for example,
        /// axis0=(-1,0) and axis1=(0,1)
        /// <param name="parStart">start angle in radians</param>
        /// <param name="parEnd">end angle in radians</param>
        /// <param name="axis0">x radius</param>
        /// <param name="axis1">y radius</param>
        /// <param name="center">the ellipse center</param>
        /// </summary>
        public Ellipse(double parStart, double parEnd, Point axis0, Point axis1, Point center){
            Debug.Assert(parStart<=parEnd);
            ParStart = parStart;
            ParEnd = parEnd;
            AxisA = axis0;
            AxisB = axis1;
            this.center = center;
            SetBoundingBox();
        }

        void SetBoundingBox() {
            if (ApproximateComparer.Close(ParStart, 0) && ApproximateComparer.Close(ParEnd, Math.PI * 2))
                box = FullBox();
            else {
                //the idea is that the box of an arc staying in one quadrant is just the box of the start and the end point of the arc
                box = new Rectangle(Start, End);
                //now Start and End are in the box, we need just add all k*P/2 that are in between
                double t;
                for (int i = (int)Math.Ceiling(ParStart / (Math.PI / 2)); (t = i * Math.PI / 2) < ParEnd; i++)
                    if (t > parStart) 
                        box.Add(this[t]);

            }
        }

        
        /// <summary>
        /// The point on the ellipse corresponding to the parameter t is calculated by 
        /// the formula center + cos(t)*axis0 + sin(t) * axis1.
        /// To get an ellipse rotating clockwise use, for example,
        /// axis0=(-1,0) and axis1=(0,1)
        /// </summary>
        /// <param name="parStart">start angle in radians</param>
        /// <param name="parEnd">end angle in radians</param>
        /// <param name="axis0">the x axis</param>
        /// <param name="axis1">the y axis</param>
        /// <param name="centerX">x coordinate of the center</param>
        /// <param name="centerY">y coordinate of the center</param>
        public Ellipse(double parStart, double parEnd, Point axis0, Point axis1, double centerX, double centerY)
            : this(parStart, parEnd, axis0, axis1, new Point(centerX, centerY)){
        }

        /// <summary>
        /// Construct a full ellipse by two axes
        /// </summary>
        /// <param name="axis0">an axis</param>
        /// <param name="axis1">an axis</param>
        /// <param name="center"></param>
        public Ellipse(Point axis0, Point axis1, Point center)
            : this(0, Math.PI*2, axis0, axis1, center){
        }


        /// <summary>
        /// Constructs a full ellipse with axes aligned to X and Y directions
        /// </summary>
        /// <param name="axisA">the length of the X axis</param>
        /// <param name="axisB">the length of the Y axis</param>
        /// <param name="center"></param>
        public Ellipse(double axisA, double axisB, Point center)
            : this(0, Math.PI*2, new Point(axisA, 0), new Point(0, axisB), center){
        }

        /// <summary>
        /// Moves the ellipse to the delta vector
        /// </summary>
        public void Translate(Point delta)
        {
            this.center += delta;
            this.box.Center += delta;
            parallelogramNodeOverICurve = null;
        }

        /// <summary>
        /// Scales the ellipse by x and by y
        /// </summary>
        /// <param name="xScale"></param>
        /// <param name="yScale"></param>
        /// <returns>the moved ellipse</returns>
        public ICurve ScaleFromOrigin(double xScale, double yScale)
        {
            return new Ellipse(parStart, parEnd, aAxis * xScale, bAxis * yScale, Point.Scale(xScale, yScale, center));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public double GetParameterAtLength(double length) {
            //todo: slow version!
            const double eps = 0.001;

            var l = ParStart;
            var u = ParEnd;
            var lenplus = length + eps;
            var lenminsu = length - eps;
            while (u - l > ApproximateComparer.DistanceEpsilon) {
                var m = 0.5*(u + l);
                var len = LengthPartial(ParStart, m);
                if(len>lenplus)
                    u=m;
                else if (len < lenminsu)
                    l = m;
                else return m;
            }
            return (u + l)/2;
        }

        /// <summary>
        /// Transforms the ellipse
        /// </summary>
        /// <param name="transformation"></param>
        /// <returns>the transformed ellipse</returns>
        public ICurve Transform(PlaneTransformation transformation){
            if(transformation != null){
                Point ap = transformation*aAxis - transformation.Offset;
                Point bp = transformation*bAxis - transformation.Offset;
                return new Ellipse(parStart, parEnd, ap, bp, transformation*center);
            }
            return this;
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
            const int numberOfTestPoints = 8;
            double t = (high - low) / (numberOfTestPoints + 1);
            double closest = low;
            double minDist = Double.MaxValue;
            for (int i = 0; i <= numberOfTestPoints; i++) {
                double par = low + i * t;
                Point p = targetPoint - this[par];
                double d = p * p;
                if (d < minDist) {
                    minDist = d;
                    closest = par;
                }
            }
            if (closest == 0 && high == Math.PI*2)
                low = -Math.PI;
            double ret = ClosestPointOnCurve.ClosestPoint(this, targetPoint, closest, low, high);
            if (ret < 0)
                ret += 2 * Math.PI;
            return ret;
        }

        /// <summary>
        /// return length of the curve segment [start,end] : not implemented
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public double LengthPartial(double start, double end) {
            return Curve.LengthWithInterpolationAndThreshold(Trim(start, end), Curve.LineSegmentThreshold/100);
        }

        /// <summary>
        /// Return the length of the ellipse curve: not implemented
        /// </summary>
        public double Length{
            get {
                return Curve.LengthWithInterpolation(this);
            }
        }


        /// <summary>
        /// clones the curve. 
        /// </summary>
        /// <returns>the cloned curve</returns>
        public ICurve Clone(){
            return new Ellipse(parStart, parEnd, aAxis, bAxis, center);
        }

        #region ICurve Members

        /// <summary>
        /// returns a parameter t such that the distance between curve[t] and a is minimal
        /// </summary>
        /// <param name="targetPoint"></param>
        /// <returns>the parameter of the closest point</returns>     
        public double ClosestParameter(Point targetPoint){
            double savedParStart = 0;
            const int numberOfTestPoints = 8;
            double t = (ParEnd - ParStart)/(numberOfTestPoints + 1);
            double closest = ParStart;
            double minDist = Double.MaxValue;
            for (int i = 0; i <= numberOfTestPoints; i++){
                double par = ParStart + i*t;
                Point p = targetPoint - this[par];
                double d = p*p;
                if(d < minDist){
                    minDist = d;
                    closest = par;
                }
            }
            bool parStartWasChanged = false;
            if(closest == 0 && ParEnd == Math.PI*2){
                parStartWasChanged = true;
                savedParStart = ParStart;
                ParStart = -Math.PI;
            }
            double ret = ClosestPointOnCurve.ClosestPoint(this, targetPoint, closest, ParStart, ParEnd);
            if(ret<0)
                ret += 2*Math.PI;
            if (parStartWasChanged)
                ParStart = savedParStart;
            return ret;
        }

        /// <summary>
        /// left derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point LeftDerivative(double t){
            return Derivative(t);
        }

        /// <summary>
        /// right derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point RightDerivative(double t){
            return Derivative(t);
        }

        #endregion

        #region ICurve Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double Curvature(double t){
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double CurvatureDerivative(double t){
            throw new NotImplementedException();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double CurvatureSecondDerivative(double t){
            throw new NotImplementedException();
        }

        #endregion
        /// <summary>
        /// returns true if the ellipse goes counterclockwise
        /// </summary>
        /// <returns></returns>
        public bool OrientedCounterclockwise(){
            return AxisA.X*AxisB.Y - AxisB.X*AxisA.Y > 0;
        }

      
        ///<summary>
        ///returns the box of the ellipse that this ellipse is a part of
        ///</summary>
        ///<returns></returns>
        public Rectangle FullBox() {
            var del=AxisA + AxisB;
            return new Rectangle(center + del, center - del);
        }

        ///<summary>
        ///is it a proper circle?
        ///</summary>
        public bool IsArc() {
            return AxisA.X == AxisB.Y && AxisA.Y == -AxisB.X;
        }

    }
}