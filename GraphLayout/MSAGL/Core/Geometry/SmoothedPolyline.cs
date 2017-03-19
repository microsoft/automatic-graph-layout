using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Core.Geometry {
    /// <summary>
    /// represents the polyline of an edge
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"),
     System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly",
         MessageId = "Polyline")
#if TEST_MSAGL
    ,Serializable
#endif
    ]
    public class SmoothedPolyline: IEnumerable<Point> {


        /// <summary>
        /// creates the polyline from corner points
        /// </summary>
        ///<param name="points">points of the polyline</param>
        public static SmoothedPolyline FromPoints(IEnumerable<Point> points){
            ValidateArg.IsNotNull(points, "points");
            SmoothedPolyline ret=null;
            Site site=null;
            foreach(Point p in points){
                if(site==null){
                    site=new Site(p);
                    ret=new SmoothedPolyline(site);
                } else {
                    Site s=new Site(p);
                    s.Previous=site;
                    site.Next=s;
                    site=s;
                }
            }
            return ret;
        }


        readonly Site headSite;
        /// <summary>
        /// the first site of the polyline
        /// </summary>
        public Site HeadSite {
            get { return headSite; }
        }
/// <summary>
/// clones the polyline
/// </summary>
/// <returns></returns>
        public SmoothedPolyline Clone() {
            Site h; //the site of teh clone
            Site s = headSite; //the old site
            Site prev = null;
            Site headOfTheClone=null;
            while (s != null) {
                h=s.Clone();
                h.Previous=prev;
                if (prev != null)
                    prev.Next = h;
                else
                    headOfTheClone = h;
                s = s.Next;
                prev = h;
            }
            return new SmoothedPolyline(headOfTheClone);
        }
/// <summary>
/// a constructor
/// </summary>
/// <param name="head"></param>
        public SmoothedPolyline(Site head) {
            this.headSite = head;
        }

        /// <summary>
        /// the last site of the polyline
        /// </summary>
        public Site LastSite {
            get {
                Site ret = headSite;
                while (ret.Next != null)
                    ret = ret.Next;
                return ret;
            }
        }

#if TEST_MSAGL
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerable<LineSegment> GetSegments() {
            Site s0 = headSite;
            Site s1 = headSite.Next;
            while (s1 != null) {
               yield return new LineSegment(s0.Point,s1.Point);
                s0=s1;
                s1=s1.Next;
            }
        }
#endif

        /// <summary>
        /// Creates a curve by using the underlying polyline
        /// </summary>
        /// <returns></returns>
        public Curve CreateCurve() {
            Curve curve = new Curve();
            Site a = HeadSite;//the corner start
            Site b; //the corner origin
            Site c;//the corner other end

            while (Curve.FindCorner(a, out b, out c)) {
                CubicBezierSegment bezierSeg = CreateBezierSegOnSite(b);
                if (curve.Segments.Count == 0) {
                    if (!ApproximateComparer.Close(a.Point, bezierSeg.Start))
                        Curve.AddLineSegment(curve, a.Point, bezierSeg.Start);
                } else if (!ApproximateComparer.Close(curve.End, bezierSeg.Start))
                    Curve.ContinueWithLineSegment(curve, bezierSeg.Start);
                curve.AddSegment(bezierSeg);
                a = b;
            }

            System.Diagnostics.Debug.Assert(a.Next.Next == null);

            if (curve.Segments.Count == 0) {
                if (!ApproximateComparer.Close(a.Point, a.Next.Point)) {
                    Curve.AddLineSegment(curve, a.Point, a.Next.Point);
                } else {
                    double w = 5;
                    curve.Segments.Add(new CubicBezierSegment(a.Point, a.Point + new Point(w, w), a.Point + new Point(-w, w), b.Point));
                }
            } else if (!ApproximateComparer.Close(curve.End, a.Next.Point))
                Curve.ContinueWithLineSegment(curve, a.Next.Point);
            return curve;
        }

        static internal CubicBezierSegment CreateBezierSegOnSite(Site b) {
            var kPrev = b.PreviousBezierSegmentFitCoefficient;
            var kNext = b.NextBezierSegmentFitCoefficient;
            var a = b.Previous;
            var c = b.Next;
            var s = kPrev*a.Point + (1 - kPrev)*b.Point;
            var e = kNext*c.Point + (1 - kNext)*b.Point;
            var u = s*b.PreviousTangentCoefficient + (1 - b.PreviousTangentCoefficient)*b.Point;
            var v = e*b.NextTangentCoefficient + (1 - b.NextTangentCoefficient)*b.Point;
            return new CubicBezierSegment(s, u, v, e);
        }

        #region IEnumerable<Point> Members
/// <summary>
/// the enumerator of the polyline corners
/// </summary>
/// <returns></returns>
        public IEnumerator<Point> GetEnumerator() {
            return new PointNodesList(this.headSite);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return new PointNodesList(this.headSite);
        }

        #endregion
    }
}
