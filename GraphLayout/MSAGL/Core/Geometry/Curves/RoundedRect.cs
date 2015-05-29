using System;
using System.Diagnostics;

namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// A rectanglular curve with rounded corners
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    public class RoundedRect : ICurve {
        ///<summary>
        ///</summary>
         Curve curve;
        ///<summary>
        /// underlying curve
        ///</summary>
        public Curve Curve
        {
            get { return curve; }
        }

        /// <summary>
        /// The horizontal radius of the corner ellipse segments
        /// </summary>
        public double RadiusX { get;  set; }

        /// <summary>
        /// The vertical radius of the corner ellipse segments
        /// </summary>
        public double RadiusY { get;  set; }

         RoundedRect()
        {
        }

        /// <summary>
        /// Create a rounded rectangle geometry
        /// </summary>
        /// <param name="bounds">rounded rectangle will fit these bounds</param>
        /// <param name="radiusX">horizontal radius of the corner ellipse segments</param>
        /// <param name="radiusY">vertical radius of the corner ellipse segments</param>
        public RoundedRect(Rectangle bounds, double radiusX, double radiusY) {
            RadiusX = radiusX;
            RadiusY = radiusY;
            curve = new Curve(8);
            CurveFactory.CreateRectangleWithRoundedCorners(curve,
                bounds.Width, bounds.Height, radiusX, radiusY, bounds.Center);
        }

        /// <summary>
        /// Create a new RoundedRect with the same CornerRadius inside the target bounds
        /// </summary>
        /// <param name="target">target bounds</param>
        /// <returns>new RoundedRect with the same CornerRadius inside the target bounds</returns>
        public ICurve FitTo(Rectangle target) {
            if (ApproximateComparer.Close(target, this.BoundingBox, ApproximateComparer.UserDefinedTolerance))
            {
                return this.Clone();
            }

            return new RoundedRect(target, RadiusX, RadiusY);
        }

        /// <summary>
        /// Returns the point on the curve corresponding to parameter t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point this[double t] {
            get { return curve[t]; }
        }

        /// <summary>
        /// first derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point Derivative(double t) {
            return curve.Derivative(t);
        }

        /// <summary>
        /// second derivative
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point SecondDerivative(double t) {
            return curve.SecondDerivative(t);
        }

        /// <summary>
        /// third derivative
        /// </summary>
        /// <param name="t">the parameter of the derivative</param>
        /// <returns></returns>
        public Point ThirdDerivative(double t) {
            return curve.ThirdDerivative(t);
        }

        /// <summary>
        /// A tree of ParallelogramNodes covering the curve. 
        /// This tree is used in curve intersections routines.
        /// </summary>
        /// <value></value>
        public ParallelogramNodeOverICurve ParallelogramNodeOverICurve {
            get { return curve.ParallelogramNodeOverICurve; }
        }

        bool dirtyBounds = true;
        Rectangle cachedBounds;

        /// <summary>
        /// XY bounding box of the curve
        /// </summary>
        public Rectangle BoundingBox {
            get 
            {
                if (dirtyBounds)
                {
                    cachedBounds = curve.BoundingBox;
                    dirtyBounds = false;
                }
                return cachedBounds;
            }
        }

        /// <summary>
        /// the start of the parameter domain
        /// </summary>
        public double ParStart {
            get { return curve.ParStart; }
        }

        /// <summary>
        /// the end of the parameter domain
        /// </summary>
        public double ParEnd {
            get { return curve.ParEnd; }
        }

        /// <summary>
        /// returns the trim curve
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public ICurve Trim(double start, double end) {
            return curve.Trim(start, end);
        }

        /// <summary>
        /// Returns the trimmed curve, wrapping around the end if start is greater than end.
        /// </summary>
        /// <param name="start">The starting parameter</param>
        /// <param name="end">The ending parameter</param>
        /// <returns>The trimmed curve</returns>
        public ICurve TrimWithWrap(double start, double end) {
            return curve.TrimWithWrap(start, end);
        }

        /// <summary>
        /// Returns the curved moved by delta
        /// </summary>
        public void Translate(Point delta) {
            this.curve.Translate(delta);
            dirtyBounds = true;
        }

        /// <summary>
        /// Returns the curved with all points scaled from the original by x and y
        /// </summary>
        /// <returns></returns>
        public ICurve ScaleFromOrigin(double xScale, double yScale) {
            var bounds = curve.BoundingBox;
            var lt = Point.Scale(xScale, yScale, bounds.LeftTop);
            var rb = Point.Scale(xScale, yScale, bounds.RightBottom);
            return new RoundedRect(new Rectangle(lt, rb), RadiusX, RadiusY);
        }

        /// <summary>
        /// this[ParStart]
        /// </summary>
        public Point Start {
            get { return curve.Start; }
        }

        /// <summary>
        /// this[ParEnd]
        /// </summary>
        public Point End {
            get { return curve.End; }
        }

        /// <summary>
        /// this[Reverse[t]]=this[ParEnd+ParStart-t] - not implemented
        /// </summary>
        /// <returns></returns>
        public ICurve Reverse() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Offsets the curve in the direction of dir
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public ICurve OffsetCurve(double offset, Point dir) {
            return curve.OffsetCurve(offset, dir);
        }

        /// <summary>
        /// return length of the curve segment [start,end] 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public double LengthPartial(double start, double end) {
            return curve.LengthPartial(start, end);
        }

        /// <summary>
        /// Get the length of the curve
        /// </summary>
        public double Length {
            get { return curve.Length; }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public double GetParameterAtLength(double length) {
            return curve.GetParameterAtLength(length);
        }

        /// <summary>
        /// Return the transformed curve
        /// </summary>
        /// <param name="transformation"></param>
        /// <returns>the transformed curve</returns>
        public ICurve Transform(PlaneTransformation transformation) {
            var bounds = curve.BoundingBox;
            var lt = transformation * bounds.LeftTop;
            var rb = transformation * bounds.RightBottom;
            var transBounds = new Rectangle(lt, rb);
            return new RoundedRect(transBounds, RadiusX, RadiusY);
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
            return this.Curve.ClosestParameterWithinBounds( targetPoint, low, high);
        }

        /// <summary>
        /// returns a parameter t such that the distance between curve[t] and a is minimal
        /// </summary>
        /// <param name="targetPoint"></param>
        /// <returns></returns>
        public double ClosestParameter(Point targetPoint) {
            return curve.ClosestParameter(targetPoint);
        }

        /// <summary>
        /// clones the curve. 
        /// </summary>
        /// <returns>the cloned curve</returns>
        public ICurve Clone() {
            RoundedRect newRect = new RoundedRect();
            newRect.RadiusX = this.RadiusX;
            newRect.RadiusY = this.RadiusY;
            newRect.curve = (Curve)this.curve.Clone();
            return newRect;
        }

        /// <summary>
        /// The left derivative at t. 
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point LeftDerivative(double t) {
            return curve.LeftDerivative(t);
        }

        /// <summary>
        /// the right derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        public Point RightDerivative(double t) {
            return curve.RightDerivative(t);
        }

        /// <summary>
        /// the signed curvature of the segment at t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double Curvature(double t) {
            return curve.Curvature(t);
        }

        /// <summary>
        /// the derivative of the curvature at t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double CurvatureDerivative(double t) {
            return curve.CurvatureDerivative(t);
        }

        /// <summary>
        /// the derivative of CurvatureDerivative
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double CurvatureSecondDerivative(double t) {
            return curve.CurvatureSecondDerivative(t);
        }
    }
}