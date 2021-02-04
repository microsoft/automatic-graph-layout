using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// Defines the anchors for a node; anchors can be not symmetrical in general
    /// 
    ///          |TopAnchor
    ///Left anchor|
    /// ======Origin==================RightAnchor
    ///          |
    ///          |
    ///          |BottomAnchor
    /// </summary>
#if TEST_MSAGL
    public
#else
    internal
#endif
        class Anchor {
        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return "la:ra " +
              la.ToString("#.##", CultureInfo.InvariantCulture) + " " + ra.ToString("#.##", CultureInfo.InvariantCulture) + " ta:ba " + ta.ToString("#.##", CultureInfo.InvariantCulture) + " " + ba.ToString("#.##", CultureInfo.InvariantCulture) + " x:y " + x.ToString("#.##", CultureInfo.InvariantCulture) + " " + y.ToString("#.##", CultureInfo.InvariantCulture);
        }

        double la;
        double ra;
        double ta;
        double ba;

        double labelCornersPreserveCoefficient;

        /// <summary>
        /// distance for the center of the node to its left boundary
        /// </summary>
        public double LeftAnchor {
            get {
                return la;
            }
            set {
                //the absence of this check allows a situation when an edge crosses its label or 
                // a label which does not belong to the edge
                //       if(value<-Curve.DistEps)
                //       throw new Exception("assigning negative value to a anchor");
                la = Math.Max(value, 0); ;
            }
        }

        /// <summary>
        /// distance from the center of the node to its right boundary
        /// </summary>
        public double RightAnchor {
            get {
                return ra;
            }
            set {
                //   if(value<-Curve.DistEps)
                //   throw new Exception("assigning negative value to a anchor: "+value );
                ra = Math.Max(value, 0);
            }
        }

        /// <summary>
        /// distance from the center of the node to its top boundary
        /// </summary>
        public double TopAnchor {
            get {
                return ta;
            }
            set {
                //if(value<-Curve.DistEps)
                //throw new Exception("assigning negative value to a anchor");
                ta = Math.Max(value, 0);
            }
        }

        /// <summary>
        /// distance from the center of the node to it bottom boundary
        /// </summary>
        public double BottomAnchor {
            get {
                return ba;
            }
            set {

                //if(value<-Curve.DistEps)
                //throw new InvalidOperationException();//"assigning negative value to a anchor");
                ba = Math.Max(value, 0);
            }
        }


        /// <summary>
        /// Left boundary of the node
        /// </summary>
        public double Left {
            get { return x - la; }
        }

        /// <summary>
        /// right boundary of the node
        /// </summary>
        public double Right {
            get { return x + ra; }
        }

        /// <summary>
        /// top boundary of the node
        /// </summary>
        public double Top {
            get { return y + ta; }
            set { 
                y += value - Top;
            }
        }

        /// <summary>
        /// bottom of the node
        /// </summary>
        public double Bottom {
            get { return y - ba; }
            set { y += value - Bottom; }
        }

        /// <summary>
        /// Left top corner
        /// </summary>
        public Point LeftTop {
            get { return new Point(Left, Top); }
        }
        /// <summary>
        /// Left bottom of the node
        /// </summary>
        public Point LeftBottom {
            get { return new Point(Left, Bottom); }
        }
        /// <summary>
        /// Right bottom of the node
        /// </summary>
        public Point RightBottom {
            get { return new Point(Right, Bottom); }
        }

        Node node;

        internal Node Node {
            get { return node; }
            set { 
                node = value;
                this.polygonalBoundary = null;
            }
        }

        /// <summary>
        /// Right top of the node
        /// </summary>
        public Point RightTop {
            get { return new Point(Right, Top); }
        }

        /// <summary>
        /// an empty constructor
        /// </summary>
        public Anchor(double labelCornersPreserveCoefficient) {
            this.labelCornersPreserveCoefficient = labelCornersPreserveCoefficient;
        }
        /// <summary>
        /// constructor
        /// </summary>
        public Anchor(double leftAnchor, double rightAnchor,
            double topAnchor, double bottomAnchor, Node node, double labelCornersPreserveCoefficient) {
            la = leftAnchor;
            ra = rightAnchor;
            ta = topAnchor;
            ba = bottomAnchor;
            Node = node;
            this.labelCornersPreserveCoefficient = labelCornersPreserveCoefficient;
        }

        double x;
        /// <summary>
        /// the x position
        /// </summary>
        internal double X {
            get {
                return x;
            }

            set {
                polygonalBoundary = null;
                x = value;
            }
        }

        double y;
        /// <summary>
        /// the y position
        /// </summary>
        internal double Y {
            get {
                return y;
            }

            set {
                polygonalBoundary = null;
                y = value;
            }
        }

        /// <summary>
        /// Center of the node
        /// </summary>
        public Point Origin {
            get {
                return new Point(x, y);
            }
        }

        bool alreadySitsOnASpline;
        /// <summary>
        /// signals if the spline has been routed already through the node
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811")]
        public bool AlreadySitsOnASpline {
            get { return alreadySitsOnASpline; }
            set { alreadySitsOnASpline = value; }
        }

        /// <summary>
        /// node widths
        /// </summary>
        public double Width {
            get { return this.la + this.ra; }
        }

        /// <summary>
        /// node height
        /// </summary>
        public double Height {
            get { return this.ta + this.ba; }
        }


        /// <summary>
        /// set to true if the anchor has been introduced for a label
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811")]
        public bool RepresentsLabel {
            get { return LabelToTheRightOfAnchorCenter || LabelToTheLeftOfAnchorCenter; }
        }

        bool labelIsToTheLeftOfTheSpline;

        /// <summary>
        /// An anchor for an edge label with the label to the right of the spline has its height equal to the one of the label
        /// Its leftAnchor is a reserved space for the spline and the rightAnchor is equal to the label width.
        /// </summary>
        internal bool LabelToTheLeftOfAnchorCenter {
            get { return labelIsToTheLeftOfTheSpline; }
            set { labelIsToTheLeftOfTheSpline = value; }
        }

        bool labelIsToTheRightOfTheSpline;

        /// <summary>
        /// An anchor for an edge label with the label to the left of the spline has its height equal to the one of the label
        /// Its rightAnchor is a reserved space for the spline and the leftAnchor is equal to the label width.
        /// </summary>
        internal bool LabelToTheRightOfAnchorCenter {
            get { return labelIsToTheRightOfTheSpline; }
            set { labelIsToTheRightOfTheSpline = value; }
        }

        internal bool HasLabel {
            get { return LabelToTheRightOfAnchorCenter || LabelToTheLeftOfAnchorCenter; }
        }

        internal double LabelWidth {
            get {
                if (LabelToTheLeftOfAnchorCenter)
                    return LeftAnchor;
                if (LabelToTheRightOfAnchorCenter)
                    return RightAnchor;

                throw new InvalidOperationException();
            }
        }

        Polyline polygonalBoundary;
        /// <summary>
        /// the polygon representing the boundary of a node
        /// </summary>
#if TEST_MSAGL
        public
#else
        internal
#endif
 Polyline PolygonalBoundary {
            get {
                if (polygonalBoundary != null)
                    return polygonalBoundary;
                return polygonalBoundary = Pad(CreatPolygonalBoundaryWithoutPadding(),Padding);
                }
        }

        static Polyline Pad(Polyline curve, double padding) {
            if (padding == 0)
                return curve;

            if (CurveIsConvex(curve)) {
                return PadConvexCurve(curve, padding);
            } else
                return PadConvexCurve(Curve.StandardRectBoundary(curve), padding);
        }

        static void PadCorner(Polyline poly, PolylinePoint p0, PolylinePoint p1, PolylinePoint p2, double padding) {
            Point a, b;
            int numberOfPoints=GetPaddedCorner(p0, p1, p2, out a, out b, padding);
            poly.AddPoint(a);
            if (numberOfPoints==2)
                poly.AddPoint(b);
        }


        static  Polyline PadConvexCurve(Polyline poly, double padding) { 
            Polyline ret = new Polyline();

            PadCorner(ret, poly.EndPoint.Prev, poly.EndPoint, poly.StartPoint, padding);
            PadCorner(ret, poly.EndPoint, poly.StartPoint, poly.StartPoint.Next, padding);

            for (PolylinePoint pp = poly.StartPoint; pp.Next.Next != null; pp = pp.Next)
                PadCorner(ret, pp, pp.Next, pp.Next.Next, padding);

            ret.Closed = true;
            return ret;

        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="third"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="padding"></param>
        /// <returns>number of new points</returns>
        static int GetPaddedCorner(PolylinePoint first, PolylinePoint second, PolylinePoint third, out Point a, out Point b,
            double padding) {
            Point u = first.Point;
            Point v = second.Point;
            Point w = third.Point;
            bool ccw = Point.GetTriangleOrientation(u, v, w) == TriangleOrientation.Counterclockwise;

            //uvPerp has to look outside of the curve
            var uvPerp = (v - u).Rotate((ccw? - Math.PI:Math.PI) / 2).Normalize();



            //l is bisector of the corner (u,v,w) pointing out of the corner - outside of the polyline
            Point l = (v - u).Normalize() + (v - w).Normalize();
            Debug.Assert(l * uvPerp >= 0);
            if (l.Length < ApproximateComparer.IntersectionEpsilon) {
                a = b = v + padding * uvPerp;
                return 1;
            }
// flip uvPerp if it points inside of the polyline
            Point d = l.Normalize() * padding;
            Point dp = d.Rotate(Math.PI / 2);

            //look for a in the form d+x*dp
            //we have:  Padding=(d+x*dp)*uvPerp
            double xp = (padding - d * uvPerp) / (dp * uvPerp);
            a = d + xp * dp + v;
            b = d - xp * dp + v;
            return 2; //number of points to add 
        }

        static IEnumerable<TriangleOrientation> Orientations(Polyline poly) {
            yield return Point.GetTriangleOrientation(poly.EndPoint.Point, poly.StartPoint.Point, poly.StartPoint.Next.Point);
            yield return Point.GetTriangleOrientation(poly.EndPoint.Prev.Point, poly.EndPoint.Point, poly.StartPoint.Point);
              
            var pp = poly.StartPoint;
            while (pp.Next.Next != null ) {
                yield return Point.GetTriangleOrientation(pp.Point, pp.Next.Point, pp.Next.Next.Point);
                pp = pp.Next;
            }
        }
        static bool CurveIsConvex(Polyline poly) {
            var orientation = TriangleOrientation.Collinear;
            foreach (var or in Orientations(poly)) {
                if (or == TriangleOrientation.Collinear)
                    continue;
                if (orientation == TriangleOrientation.Collinear)
                    orientation = or;
                else if (or != orientation)
                    return false;
            }
            return true;
        }


        //private static double TurnAfterSeg(Curve curve, int i) {
        //    return Point.SignedDoubledTriangleArea(curve.Segments[i].Start, curve.Segments[i].End, curve.Segments[(i + 1) / curve.Segments.Count].End);
        //}

        private Polyline CreatPolygonalBoundaryWithoutPadding() {
            Polyline ret;
            if (this.HasLabel)
                ret = LabelToTheLeftOfAnchorCenter ? PolygonOnLeftLabel() : PolygonOnRightLabel();
            else if (this.NodeBoundary == null)
                ret = StandardRectBoundary();
            else 
                ret = Curve.PolylineAroundClosedCurve(this.NodeBoundary);
            return ret;
        }

        private Polyline StandardRectBoundary() {
            Polyline poly = new Polyline();
            poly.AddPoint(LeftTop);
            poly.AddPoint(RightTop);
            poly.AddPoint(RightBottom);
            poly.AddPoint(LeftBottom);
            poly.Closed = true;
            return poly;
        }

        private Polyline PolygonOnLeftLabel() {
            Polyline poly = new Polyline();
            double t = Left + (1 - this.labelCornersPreserveCoefficient) * LabelWidth;
            poly.AddPoint(new Point(t, Top));
            poly.AddPoint(RightTop);
            poly.AddPoint(RightBottom);
            poly.AddPoint(new Point(t, Bottom));
            poly.AddPoint(new Point(Left, Y));
            poly.Closed = true;
            return poly;
        }

        private Polyline PolygonOnRightLabel() {
            Polyline poly = new Polyline();
            double t = Right - (1 - this.labelCornersPreserveCoefficient) * LabelWidth;
            poly.AddPoint(t, Top);
            poly.AddPoint(Right, Y);
            poly.AddPoint(t, Bottom);
            poly.AddPoint(Left, Bottom);
            poly.AddPoint(Left, Top);
            poly.Closed = true;
            return poly;

        }

      
        internal ICurve NodeBoundary {
            get { return Node!=null?Node.BoundaryCurve:null; }
        }


        double padding;
        /// <summary>
        /// node padding
        /// </summary>
        public double Padding {
            get { return padding; }
            set { padding = value; }
        }

        internal void Move(Point p){
            this.X += p.X;
            this.Y += p.Y;
        }

#if TEST_MSAGL

        /// <summary>
        /// UserData
        /// </summary>
        public object UserData { get; set; }

#endif

    }
}
