namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// The interface for curves: instances of ICurve inside of GLEE
    /// are BSpline,Curve,LineSeg, Ellipse,CubicBezierSeg and ArrowTipCurve.
    /// </summary>
    public interface ICurve {
        /// <summary>
        /// Returns the point on the curve corresponding to parameter t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "t")]
        Point this[double t] { get; }
        /// <summary>
        /// first derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "t")]
        Point Derivative(double t);
        /// <summary>
        /// second derivative
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "t")]
        Point SecondDerivative(double t);
        /// <summary>
        /// third derivative
        /// </summary>
        /// <param name="t">the parameter of the derivative</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "t")]
        Point ThirdDerivative(double t);

        /// <summary>
        /// A tree of ParallelogramNodes covering the curve. 
        /// This tree is used in curve intersections routines.
        /// </summary>
        /// <value></value>
        ParallelogramNodeOverICurve ParallelogramNodeOverICurve { get; }

        /// <summary>
        /// XY bounding box of the curve
        /// </summary>
        Rectangle BoundingBox { get;}

        /// <summary>
        /// the start of the parameter domain
        /// </summary>
        double ParStart { get;}

        /// <summary>
        /// the end of the parameter domain
        /// </summary>
        double ParEnd { get;}

        /// <summary>
        /// Returns the trim curve between start and end, without wrap
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "End")]
        ICurve Trim(double start, double end);

        /// <summary>
        /// Returns the trim curve between start and end, with wrap, if supported by the implementing class.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "End")]
        ICurve TrimWithWrap(double start, double end);

        /// <summary>
        /// Moves the curve by the delta.
        /// </summary>
        void Translate(Point delta);

        /// <summary>
        /// Returns the curved with all points scaled from the original by x and y
        /// </summary>
        /// <returns></returns>
        ICurve ScaleFromOrigin(double xScale, double yScale);

        /// <summary>
        /// this[ParStart]
        /// </summary>
        Point Start { get;}

        /// <summary>
        /// this[ParEnd]
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "End")]
        Point End { get; }

        /// <summary>
        /// this[Reverse[t]]=this[ParEnd+ParStart-t]
        /// </summary>
        /// <returns></returns>
        ICurve Reverse();



      /// <summary>
        /// Offsets the curve in the direction of dir
      /// </summary>
      /// <param name="offset"></param>
      /// <param name="dir"></param>
      /// <returns></returns>
        
        
        
        ICurve OffsetCurve(double offset, Point dir);

        /// <summary>
        /// return length of the curve segment [start,end] 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "End")]
        double LengthPartial(double start, double end);

        /// <summary>
        /// Get the length of the curve
        /// </summary>
        double Length { get;}


        /// <summary>
        /// gets the parameter at a specific length from the start along the curve
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "t")]
        double GetParameterAtLength(double length);

        /// <summary>
        /// Return the transformed curve
        /// </summary>
        /// <param name="transformation"></param>
        /// <returns>the transformed curve</returns>
        ICurve Transform(PlaneTransformation transformation);

        
        /// <summary>
        /// returns a parameter t such that the distance between curve[t] and targetPoint is minimal 
        /// and t belongs to the closed segment [low,high]
        /// </summary>
        /// <param name="targetPoint">the point to find the closest point</param>
        /// <param name="high">the upper bound of the parameter</param>
        /// <param name="low">the low bound of the parameter</param>
        /// <returns></returns>
        double ClosestParameterWithinBounds(Point targetPoint, double low, double high);
        
        /// <summary>
        /// returns a parameter t such that the distance between curve[t] and a is minimal
        /// </summary>
        /// <param name="targetPoint"></param>
        /// <returns></returns>
        double ClosestParameter(Point targetPoint);
/// <summary>
/// clones the curve. 
/// </summary>
/// <returns>the cloned curve</returns>
        ICurve Clone();


        /// <summary>
        /// The left derivative at t. 
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "t")]
        Point LeftDerivative(double t);


        /// <summary>
        /// the right derivative at t
        /// </summary>
        /// <param name="t">the parameter where the derivative is calculated</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "t")]
        Point RightDerivative(double t);

        
        /// <summary>
        /// the signed curvature of the segment at t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "t")]
        double Curvature(double t);
        /// <summary>
        /// the derivative of the curvature at t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "t")]
        double CurvatureDerivative(double t);


        /// <summary>
        /// the derivative of CurvatureDerivative
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "t")]
        double CurvatureSecondDerivative(double t);


    }
}
