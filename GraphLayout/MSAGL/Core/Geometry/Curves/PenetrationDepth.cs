using System;
using System.Collections.Generic;

namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// Works for convex curves by calculating a value which might be slightly greater than the exact penetration depth
    /// It is presize on convex polygons.
    /// </summary>
    static public class PenetrationDepth {
      
        /// <summary>
        /// Calculates a vector of minimal length by which to move the first curve to avoid intersections of interiors.
        /// It is presize on polylines and will be slightly longer on curves.
        /// </summary>
        /// <param name="curve0"></param>
        /// <param name="curve1"></param>
        /// <returns>the shortest vector d such that d+curve0 does not intersect the interiou curve1</returns>
        static public Point PenetrationVector(ICurve curve0, ICurve curve1) {
            return PenetrationDepthForPolylines(PolylineAroundClosedCurve(curve0), PolylineAroundClosedCurve(curve1));
        }
        /// <summary>
        /// returns a polyline around the curve
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public static Polyline PolylineAroundClosedCurve(ICurve curve) {
            Polyline poly = new Polyline();
            foreach (Point point in PointsOnAroundPolyline(curve))
                poly.AddPoint(point);
            if (Point.GetTriangleOrientation(poly.StartPoint.Point, poly.StartPoint.Next.Point, poly.StartPoint.Next.Next.Point) == TriangleOrientation.Counterclockwise)
                poly = (Polyline)poly.Reverse();
            poly.Closed = true;
            return poly;
        }

      
         static IEnumerable<Point> PointsOnAroundPolyline(ICurve curve) {
            bool firstSide = true;
            CurveTangent prevTangent = null;
            CurveTangent firstCurveTangent = null;
            foreach (CurveTangent curveTangent in TangentsAroundCurve(curve)) {
                if (firstSide) {
                    firstSide = false;
                    firstCurveTangent = prevTangent = curveTangent;
                } else {
                    if (!TangentsAreParallel(prevTangent, curveTangent))
                        yield return TangentIntersection(prevTangent, curveTangent);
                    prevTangent = curveTangent;
                }
            }
            yield return TangentIntersection(firstCurveTangent, prevTangent);

        }

      
        
         static IEnumerable<CurveTangent> TangentsAroundCurve(ICurve iCurve) {
            Curve c = iCurve as Curve;
            if (c != null) {
                foreach (ICurve seg in c.Segments)
                    foreach (CurveTangent ct in TangentsAroundCurve(seg))
                        yield return ct;
            } else {
                LineSegment ls = iCurve as LineSegment;
                if (ls != null)
                    yield return new CurveTangent(ls.Start, ls.Derivative(0));
                else {
                    Ellipse ellipse = iCurve as Ellipse;
                    if (ellipse != null) {
                        foreach (CurveTangent ct in TangentsOfEllipse(ellipse))
                            yield return ct;
                    } else {
                        CubicBezierSegment bez = iCurve as CubicBezierSegment;
                        if (bez != null)
                            foreach (CurveTangent ct in TangentsOfBezier(bez))
                                yield return ct;
                        }
                }
                
            }
        }

         static IEnumerable<CurveTangent> TangentsOfBezier(CubicBezierSegment bez) {
            const int numOfTangents = 8;
            double span = (bez.ParEnd - bez.ParStart) / numOfTangents;
            for (int i = 0; i < numOfTangents; i++)
                yield return TangentOnICurve(span / 2 + bez.ParStart + span * i, bez);
        }

         static CurveTangent TangentOnICurve(double p, ICurve iCurve) {
            return new CurveTangent(iCurve[p], iCurve.Derivative(p));
        }

         static IEnumerable<CurveTangent> TangentsOfEllipse(Ellipse ellipse) {
            const double angle=Math.PI/3;
#if SHARPKIT //https://github.com/SharpKit/SharpKit/issues/4 integer rounding issue
            int numOfTangents =((int) Math.Ceiling((ellipse.ParEnd - ellipse.ParStart) / angle)) + 1;
#else
            int numOfTangents =(int) Math.Ceiling((ellipse.ParEnd - ellipse.ParStart) / angle) + 1;
#endif
            double span = (ellipse.ParEnd - ellipse.ParStart) / numOfTangents;
            for (int i = 0; i < numOfTangents; i++)
                yield return TangentOnICurve(span / 2 + ellipse.ParStart + span * i, ellipse);
        }

         static Point TangentIntersection(CurveTangent tangentA, CurveTangent tangentB) {
            Point x;
            Point.LineLineIntersection(tangentA.touchPoint, tangentA.touchPoint+tangentA.direction, tangentB.touchPoint, tangentB.touchPoint+tangentB.direction, out x);
            return x;
        }

         static bool TangentsAreParallel(CurveTangent a, CurveTangent b) {
            return Math.Abs(a.direction.X * b.direction.Y - a.direction.Y * b.direction.X) < ApproximateComparer.DistanceEpsilon;
        }

        /// <summary>
        /// Calculates a vector of minimal length by which to move the first polyline to avoid intersection of interiors
        /// </summary>
        /// <param name="poly0"></param>
        /// <param name="poly1"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polylines")]
        static public Point PenetrationDepthForPolylines(Polyline poly0, Polyline poly1) {
            ValidateArg.IsNotNull(poly0, "poly0");
            ValidateArg.IsNotNull(poly1, "poly1");
            System.Diagnostics.Debug.Assert(
                Point.GetTriangleOrientation(poly0[0], poly0[1], poly0[2]) == TriangleOrientation.Clockwise
                &&
                Point.GetTriangleOrientation(poly1[0], poly1[1], poly1[2]) == TriangleOrientation.Clockwise);

            poly0 = FlipPolyline(poly0);
            PolylinePoint p0 = GetMostLeftLow(poly0);
            PolylinePoint p1 = GetMostLeftLow(poly1);
            Polyline minkovski = Merge(p0, p1);
            Point origin = new Point();
            PolylinePoint pp = minkovski.StartPoint;
            double d = double.PositiveInfinity;
            Point ret = new Point();
            do {
                PolylinePoint pn = pp.Polyline.Next(pp);
                if (Point.GetTriangleOrientation(origin, pp.Point, pn.Point) != TriangleOrientation.Clockwise) {
                    return new Point();
                }
                double t;
                double dist = Point.DistToLineSegment(origin, pp.Point, pn.Point, out t);
                if (dist < d) {
                    d = dist;
                    ret = (1 - t) * pp.Point + t * pn.Point;
                }
                pp = pn;
            } while (pp != minkovski.StartPoint);
            return ret;
        }

         static Polyline Merge(PolylinePoint p0, PolylinePoint p1) {
            PolylinePoint s0 = p0;
            PolylinePoint s1 = p1;
            
            Polyline ret = new Polyline();
            while (true) {
                ret.AddPoint(p0.Point + p1.Point);
                PickNextVertex(ref p0, ref p1);
                if (p0 == s0 && p1 == s1)
                    break;
              
            } 
            ret.Closed = true;
            return ret;
        }

         static void PickNextVertex(ref PolylinePoint p0, ref PolylinePoint p1) {
            Point d = p1.Point - p0.Point;
            TriangleOrientation orient = Point.GetTriangleOrientation(p1.Point, p1.Polyline.Next(p1).Point, p0.Polyline.Next(p0).Point + d);
            if (orient == TriangleOrientation.Counterclockwise)
                p0 = p0.Polyline.Next(p0);
            else
                p1 = p1.Polyline.Next(p1);
        }

         static Polyline FlipPolyline(Polyline poly) {
            Polyline ret = new Polyline();
            for (PolylinePoint pp = poly.StartPoint; pp != null; pp = pp.Next)
                ret.AddPoint( -pp.Point);
            ret.Closed = true;
            return ret;
        }

         static PolylinePoint GetMostLeftLow(Polyline poly) {
            PolylinePoint ret = poly.StartPoint;
            for (PolylinePoint p = ret.Next; p != null; p = p.Next)
                if (p.Point.X < ret.Point.X)
                    ret = p;
                else if (p.Point.X == ret.Point.X)
                    if (p.Point.Y < ret.Point.Y)
                        ret = p;

            return ret;
        }

    }
}
