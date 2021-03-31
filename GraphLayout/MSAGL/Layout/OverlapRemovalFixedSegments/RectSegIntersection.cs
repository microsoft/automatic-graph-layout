using Microsoft.Msagl.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry.Curves;
using SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;
namespace Microsoft.Msagl.Layout.OverlapRemovalFixedSegments
{
    public class RectSegIntersection
    {
        const int INSIDE = 0; // 0000
        const int LEFT = 1;   // 0001
        const int RIGHT = 2;  // 0010
        const int BOTTOM = 4; // 0100
        const int TOP = 8;    // 1000

        public static double acc = 10e-16; 
        public static bool GetInt(double x, double x1, double x2, out double alpha) {
            double dx = x2 - x1;
            double d = x - x1;

            if (Math.Abs(dx / d) > acc) {
                alpha = d / dx;
                return true;
            }

            alpha = 0;
            return false;
        }

        public static int GetOutCode(Rectangle rect, Point p) {
            int code = INSIDE;

            if (p.X < rect.Left)
                code |= LEFT;
            else if (p.X > rect.Right)
                code |= RIGHT;

            if (p.Y < rect.Bottom)
                code |= BOTTOM;
            else if (p.Y > rect.Top)
                code |= TOP;

            return code;
        }

        public static bool GetIntX(double x, Point p1, Point p2, out double alpha)
        {
            return GetInt(x, p1.X, p2.X, out alpha);
        }

        public static bool GetIntY(double y, Point p1, Point p2, out double alpha)
        {
            return GetInt(y, p1.Y, p2.Y, out alpha);
        }

        public static bool GetIntersectionPointX(double x, Point p1, Point p2, out Point p) { 
            double alpha;
            if (!GetIntX(x, p1, p2, out alpha)) {
                p = new Point();
                return false;
            }
            if (alpha < 0 || alpha > 1) {
                p = new Point();
                return false;
            }
            p = p1 + alpha * (p2 - p1);
            return true;
        }

        public static bool GetIntersectionPointY(double y, Point p1, Point p2, out Point p)
        {
            double alpha;
            if (!GetIntY(y, p1, p2, out alpha))
            {
                p = new Point();
                return false;
            }
            if (alpha < 0 || alpha > 1)
            {
                p = new Point();
                return false;
            }
            p = p1 + alpha * (p2 - p1);
            return true;
        }

        public static bool GetIntersectionLeft(Rectangle rect, Point p1, Point p2, out Point p)
        {
            return GetIntersectionPointX(rect.Left, p1, p2, out p);
        }

        public static bool GetIntersectionRight(Rectangle rect, Point p1, Point p2, out Point p)
        {
            return GetIntersectionPointX(rect.Right, p1, p2, out p);
        }

        public static bool GetIntersectionBottom(Rectangle rect, Point p1, Point p2, out Point p)
        {
            return GetIntersectionPointY(rect.Bottom, p1, p2, out p);
        }

        public static bool GetIntersectionTop(Rectangle rect, Point p1, Point p2, out Point p)
        {
            return GetIntersectionPointY(rect.Top, p1, p2, out p);
        }

        public static double GetOverlapAmountBothOnBorder(Rectangle rect, Point p1, Point p2)
        {
            Point ipLeft, ipRight, ipTop, ipBottom;
            bool iLeft = GetIntersectionLeft(rect, p1, p2, out ipLeft);
            bool iRight = GetIntersectionRight(rect, p1, p2, out ipRight);
            bool iBottom = GetIntersectionBottom(rect, p1, p2, out ipBottom);
            bool iTop = GetIntersectionTop(rect, p1, p2, out ipTop);

            double a = rect.Area;
            double a1 = 0;

            if (iTop && iBottom)
                a1 = GetHorizontalTrapezoidArea(rect.LeftBottom, ipBottom, ipTop, rect.LeftTop);
            else if (iTop && iLeft)
                a1 = GetTriangleArea(ipLeft, ipTop, rect.LeftTop);
            else if (iTop && iRight)
                a1 = GetTriangleArea(ipRight, rect.RightTop, ipTop);
            else if (iLeft && iRight)
                a1 = GetVerticalTrapezoidArea(rect.LeftBottom, rect.RightBottom, ipRight, ipLeft);
            else if (iLeft && iBottom)
                a1 = GetTriangleArea(rect.LeftBottom, ipBottom, ipLeft);
            else if (iRight && iBottom)
                a1 = GetTriangleArea(ipBottom, rect.RightBottom, ipRight);

            return Math.Min(a1, a - a1);
        }

        public static double GetOverlapAmount(Rectangle rect, Point p1, Point p2)
        {
            int out1 = GetOutCode(rect, p1);
            int out2 = GetOutCode(rect, p2);
            if ((out1 | out2) == 0) {
                Point d = (p2 - p1).Normalize();
                return GetOverlapAmount(rect, p1 - d * rect.Diagonal, p2 + d * rect.Diagonal);
            }
            if (out1 == INSIDE)
            {
                return GetOverlapAmountInsideOutside(rect, p1, p2);
            }
            if (out2 == INSIDE)
            {
                return GetOverlapAmountInsideOutside(rect, p2, p1);
            }

            Point pc1, pc2;
            ClipOnRect(rect, p1, p2, out pc1, out pc2);
            return GetOverlapAmountBothOnBorder(rect, pc1, pc2);
        }

        public static bool Intersect(Rectangle rect, Point p1, Point p2) {
            Point p;
            return ClipOnRect(rect, p1, p2, out p);
        }

        public static bool ClipOnRect(Rectangle rect, Point pointToClip, Point otherEnd, out Point pClipped) {
            pClipped = new Point(pointToClip.X, pointToClip.Y);
            int outCode = GetOutCode(rect, pointToClip);
            int outCodeOther = GetOutCode(rect, otherEnd);
            if (outCode == INSIDE) {
                pClipped = pointToClip;
                return true;
            }

            while (outCode != INSIDE){
                
                if ((outCode & outCodeOther) != 0)
                { // trivial reject
                    pClipped = new Point();
                    return false;
                }

                Point p = new Point(pClipped.X, pClipped.Y);
                if ( (outCode & TOP) != 0)
                {
                    GetIntersectionPointY(rect.Top, p, otherEnd, out pClipped);
                    pClipped.Y = rect.Top;
                }
                else if ( (outCode & BOTTOM) != 0)
                {
                    GetIntersectionPointY(rect.Bottom, p, otherEnd, out pClipped);
                    pClipped.Y = rect.Bottom;
                }
                else if ( (outCode & LEFT) != 0)
                {
                    GetIntersectionPointX(rect.Left, p, otherEnd, out pClipped);
                    pClipped.X = rect.Left;
                }
                else if ( (outCode & RIGHT) != 0)
                {
                    GetIntersectionPointX(rect.Right, p, otherEnd, out pClipped);
                    pClipped.X = rect.Right;
                }
                outCode = GetOutCode(rect, pClipped);
            }
            return true;
        }

        public static bool ClipOnRect(Rectangle rect, Point pointToClip1, Point pointToClip2, out Point pClipped1, out Point pClipped2)
        {
            Point p1, p2;
            bool intersect = ClipOnRect(rect, pointToClip1, pointToClip2, out p1);
            if (!intersect) {
                pClipped1 = new Point();
                pClipped2 = new Point();
                return false;
            }
            ClipOnRect(rect, pointToClip2, pointToClip1, out p2);

            pClipped1 = p1;
            pClipped2 = p2;
            return true;
        }

         static double GetOverlapAmountInsideOutside(Rectangle rect, Point pInside, Point pOutside)
        {
            // todo: maybe reduce penalty
            int outCode = GetOutCode(rect, pOutside);
            Point d = (pInside - pOutside).Normalize();

            return GetOverlapAmount(rect, pOutside, pInside + d * rect.Diagonal);

            //return rect.Area;
        }

        public static double GetHorizontalTrapezoidArea(Point pBottomLeft, Point pBottomRight, Point pTopRight, Point pTopLeft) {
            double h = pTopLeft.Y - pBottomLeft.Y;
            double w1 = pBottomRight.X - pBottomLeft.X;
            double w2 = pTopRight.X - pTopLeft.X;
            return h * (w1 + w2) * 0.5;
        }

        public static double GetVerticalTrapezoidArea(Point pBottomLeft, Point pBottomRight, Point pTopRight, Point pTopLeft)
        {
            double w = pTopRight.X - pTopLeft.X;
            double h1 = pTopLeft.Y - pBottomLeft.Y;
            double h2 = pTopRight.Y - pBottomRight.Y;
            return w * (h1 + h2) * 0.5;
        }

        public static double GetTriangleArea(Point p0, Point p1, Point p2)
        {
            Point v1 = p1 - p0;
            Point v2 = p2 - p0;
            double det = v1.X * v2.Y - v1.Y * v2.X;
            return 0.5 * Math.Abs(det);
        }

        public static bool SegmentsIntersect(Point p1, Point p2, Point q1, Point q2)
        {
            var seg1 = new LineSegment(p1, p2);
            var seg2 = new LineSegment(q1, q2);
            var allInt = Curve.GetAllIntersections(seg1, seg2, false);
            return allInt.Any();
        }
        /// <summary>
        /// closest point on the segment
        /// </summary>
        /// <param name="p1">first endpoint</param>
        /// <param name="p2">second endpoint</param>
        /// <param name="x">reference point</param>
        /// <returns></returns>
        public static Point ClosestPointOnSegment(Point p1, Point p2, Point x)
        {
            Point l = (p2 - p1);
            Point v = l.Normalize();
            double dot = Point.Dot(v, x - p1) / l.Length;
            Point p = p1 + l * dot;
            return dot <= 0 ? p1 : (dot >= 1 ? p2 : p1 + l * dot);
        }

        public static double Dot(Point p1, Point p2)
        {
            return p1.X*p2.X + p1.Y*p2.Y;
        }

        public static Point GetOrthShiftUntilNoLongerOverlapRectSeg(Rectangle rect, Point p1, Point p2, Point moveDir)
        {
            Point d = (p2 - p1).Rotate90Cw().Normalize();
            if (Dot(d, moveDir) < 0) d = -d;
            double minProj = 0;

            minProj = Math.Min(minProj, Dot(rect.LeftBottom - p1, d));
            minProj = Math.Min(minProj, Dot(rect.LeftTop - p1, d));
            minProj = Math.Min(minProj, Dot(rect.RightBottom - p1, d));
            minProj = Math.Min(minProj, Dot(rect.RightTop - p1, d));

            return -minProj*d;
        }

        public static List<SymmetricSegment> GetBoundarySegments(Rectangle bbox)
        {
            List<SymmetricSegment> segs = new List<SymmetricSegment>();
            segs.Add(new SymmetricSegment(bbox.LeftBottom, bbox.RightBottom));
            segs.Add(new SymmetricSegment(bbox.RightBottom, bbox.RightTop));
            segs.Add(new SymmetricSegment(bbox.RightTop, bbox.LeftTop));
            segs.Add(new SymmetricSegment(bbox.LeftTop, bbox.LeftBottom));
            return segs;
        }

        public static Point GetIntersectionWithRayFromRectOrigin(Rectangle rect, Point dir)
        {
            double t = Double.MaxValue;
            double alpha = 0;

            var p1 = rect.Center;
            var p2 = rect.Center + dir;

            GetIntX(rect.Left, p1, p2, out alpha);
            if (alpha > 0) t = Math.Min(t, alpha);

            GetIntX(rect.Right, p1, p2, out alpha);
            if (alpha > 0) t = Math.Min(t, alpha);

            GetIntY(rect.Bottom, p1, p2, out alpha);
            if (alpha > 0) t = Math.Min(t, alpha);

            GetIntY(rect.Top, p1, p2, out alpha);
            if (alpha > 0) t = Math.Min(t, alpha);

            return p1 + t*dir;
        }

        public static Rectangle GetScaled(Rectangle rect, double scale)
        {
            var scaled = rect.Clone();
            scaled.ScaleAroundCenter(scale);
            return scaled;
        }

        public static Rectangle GetScaled(Rectangle rect, double xScale, double yScale)
        {
            double dx = rect.Width/2;
            double dy = rect.Height/2;
            dx *= xScale;
            dy *= yScale;

            var scaled = new Rectangle(rect.Center - new Point(dx, dy), rect.Center + new Point(dx, dy));
            return scaled;
        }

        public static bool IsSegmentTouchingSomeRectangle(Point p1, Point p2, RTree<Rectangle, Point> rtree, double tolerance)
        {
            var searchRect = new Rectangle(p1, p2);
            searchRect = GetDilated(searchRect, tolerance);
            var intersected = rtree.GetAllIntersecting(searchRect);
            var touching =
                (from r in intersected
                 let boxBig = GetScaled(r, 1 + tolerance)
                 let boxSmall = GetScaled(r, 1 - tolerance)
                 where Intersect(boxBig, p1, p2) && !Intersect(boxSmall, p1, p2)
                 select r);
            return touching.Any();
        }

        public static bool IsSegmentInsideBoundaryOfSomeRectangle(Point p1, Point p2, RTree<Rectangle, Point> rtree,
            double tolerance)
        {
            var searchRect = new Rectangle(p1, p2);
            searchRect = GetDilated(searchRect, tolerance);
            var intersected = rtree.GetAllIntersecting(searchRect);

            var boxesContaining = new List<Rectangle>();
            foreach (var r in intersected)
            {
                var boxBig = GetDilated(r, tolerance);
                var boxSmall = GetDilated(r, -tolerance);
                var insideBoundary = true;
                insideBoundary &= boxBig.Contains(p1);
                insideBoundary &= boxBig.Contains(p2);
                insideBoundary &= !boxSmall.Contains(p1);
                insideBoundary &= !boxSmall.Contains(p2);

                if(insideBoundary)
                    boxesContaining.Add(r);
            }

            return boxesContaining.Any();
        }

        public static Rectangle GetDilated(Rectangle rect, double eps)
        {
            return new Rectangle(rect.LeftBottom-new Point(eps,eps), rect.RightTop+new Point(eps,eps));
        }

        public static bool ArePointsOnLine(List<Point> points)
        {
            for (int i = 1; i < points.Count-1; i++)
            {
                var p0 = points[i - 1];
                var p1 = points[i];
                var p2 = points[i + 1];

                var v0 = p1 - p0;
                var v1 = p2 - p1;

                if (!Point.ParallelWithinEpsilon(v0, v1, 1))
                    return false;
            }
            return true;
        }
    }
}
