using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// the helper class to create curves
    /// </summary>
    public sealed class CurveFactory {
        CurveFactory() { }
        /// <summary>
        /// Creates an ellipse by the length of axes and the center
        /// </summary>
        /// <param name="radiusInXDirection"></param>
        /// <param name="radiusInYDirection"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        static public ICurve CreateEllipse(double radiusInXDirection, double radiusInYDirection, Point center) {
            return new Ellipse(radiusInXDirection, radiusInYDirection, center);
        }

        /// <summary>
        /// Creates an ellipse by the length of axes and the center
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        static public ICurve CreateCircle(double radius, Point center) {
            return new Ellipse(radius, radius, center);
        }

        /// <summary>
        /// Create a rectangle with smoothed corners
        /// </summary>
        /// <param name="width">the rectangle width</param>
        /// <param name="height">the rectangle height</param>
        /// <param name="radiusInXDirection">the length of the x axis of the corner smoothing ellipse</param>
        /// <param name="radiusInYDirection">the length of the y axis of the corner smoothing ellipse</param>
        /// <param name="center">the rectangle center</param>
        /// <returns></returns>
        static public ICurve CreateRectangleWithRoundedCorners(double width, double height, double radiusInXDirection, double radiusInYDirection, Point center) {
            var box = new Rectangle(center.X - width / 2, center.Y - height / 2, center.X + width / 2, center.Y + height / 2);
            return new RoundedRect(box, radiusInXDirection, radiusInYDirection);
        }

        /// <summary>
        /// Create in the specified curve, a rectangle with smoothed corners
        /// </summary>
        /// <param name="c"></param>
        /// <param name="width">the rectangle width</param>
        /// <param name="height">the rectangle height</param>
        /// <param name="radiusInXDirection">the length of the x axis of the corner smoothing ellipse</param>
        /// <param name="radiusInYDirection">the length of the y axis of the corner smoothing ellipse</param>
        /// <param name="center">the rectangle center</param>
        /// <returns></returns>
        static internal void CreateRectangleWithRoundedCorners(Curve c, double width, double height, double radiusInXDirection, double radiusInYDirection, Point center) {
            if (radiusInXDirection == 0 || radiusInYDirection == 0) {
                CreateRectangle(c, width, height, center);
                return;
            }
            double w = width / 2;
            if (radiusInXDirection > w / 2)
                radiusInXDirection = w / 2;
            double h = height / 2;
            if (radiusInYDirection > h / 2)
                radiusInYDirection = h / 2;
            double x = center.X;
            double y = center.Y;
            double ox = w - radiusInXDirection;
            double oy = h - radiusInYDirection;
            double top = y + h;
            double bottom = y - h;
            double left = x - w;
            double right = x + w;
            //ellipse's axises
            Point a = new Point(radiusInXDirection, 0);
            Point b = new Point(0, radiusInYDirection);

            c.IncreaseSegmentCapacity(8);

            if (ox > 0)
                c.AddSegment(new LineSegment(new Point(x - ox, bottom), new Point(x + ox, bottom)));
            c.AddSegment(new Ellipse(1.5 * Math.PI, 2 * Math.PI, a, b, x + ox, y - oy));
            if (oy > 0)
                c.AddSegment(new LineSegment(new Point(right, y - oy), new Point(right, y + oy)));
            c.AddSegment(new Ellipse(0, 0.5 * Math.PI, a, b, x + ox, y + oy));
            if (ox > 0)
                c.AddSegment(new LineSegment(new Point(x + ox, top), new Point(x - ox, top)));
            c.AddSegment(new Ellipse(0.5 * Math.PI, Math.PI, a, b, x - ox, y + oy));
            if (oy > 0)
                c.AddSegment(new LineSegment(new Point(left, y + oy), new Point(left, y - oy)));
            c.AddSegment(new Ellipse(Math.PI, 1.5 * Math.PI, a, b, x - ox, y - oy));
        }

        /// <summary>
        /// create a box of the given width and height at center.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        static public ICurve CreateRectangle(double width, double height, Point center) {
            double w = width / 2;
            double h = height / 2;
            double x = center.X;
            double y = center.Y;
            Curve c = new Curve();
            Point[] p = new Point[] { new Point(x - w, y - h), new Point(x + w, y - h), new Point(x + w, y + h), new Point(x - w, y + h) };
            c.AddSegs(new LineSegment(p[0], p[1]), new LineSegment(p[1], p[2]), new LineSegment(p[2], p[3]), new LineSegment(p[3], p[0]));
            return c;
        }

        /// <summary>
        /// create a box of the given width and height at center.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        static internal void CreateRectangle(Curve c, double width, double height, Point center) {
            double w = width / 2;
            double h = height / 2;
            double x = center.X;
            double y = center.Y;
            Point[] p = new Point[] { new Point(x - w, y - h), new Point(x + w, y - h), new Point(x + w, y + h), new Point(x - w, y + h) };
            c.AddSegs(new LineSegment(p[0], p[1]), new LineSegment(p[1], p[2]), new LineSegment(p[2], p[3]), new LineSegment(p[3], p[0]));
        }

        /// <summary>
        /// Create a polyline curve for the given rectangle
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        static public ICurve CreateRectangle(Rectangle rectangle) {
            return CreateRectangle(rectangle.Width, rectangle.Height, rectangle.Center);
        }

        /// <summary>
        /// Creates a curve resembling a house large enough to inscribe within it a rectangle of the 
        /// given width and height at center.
        /// </summary>
        /// <param name="width">the house width - the rectangle width</param>
        /// <param name="height">the height of the inscribed rectangle; the house will be half-again higher</param>
        /// <param name="center">the center of the inscribed rectangle</param>
        /// <returns></returns>
        static public ICurve CreateHouse(double width, double height, Point center) {
            double w = width / 2;
            double h = height / 2;
            double x = center.X;
            double y = center.Y;
            Curve c = new Curve(4);
            Curve.AddLineSegment(c, x - w, y - h, x + w, y - h);
            Curve.ContinueWithLineSegment(c, x + w, y + h);
            Curve.ContinueWithLineSegment(c, x, y + 2 * h);
            Curve.ContinueWithLineSegment(c, x - w, y + h);
            Curve.CloseCurve(c);
            return c;
        }

        /// <summary>
        /// Creates curve resembling a house within the rectangle formed by width and height at center
        /// (if the rectangle is a square, the house has the shape of home plate in baseball).
        /// </summary>
        /// <param name="width">the bounding rectangle width</param>
        /// <param name="height">the bounding rectangle height</param>
        /// <param name="center">the bounding rectangle center</param>
        /// <returns></returns>
        static public ICurve CreateInteriorHouse(double width, double height, Point center) {
            double w = width / 2;
            double h = height / 2;
            double x = center.X;
            double y = center.Y;
            Curve c = new Curve(4);
            Curve.AddLineSegment(c, x - w, y - h, x + w, y - h);
            Curve.ContinueWithLineSegment(c, x + w, y);
            Curve.ContinueWithLineSegment(c, x, y + h);
            Curve.ContinueWithLineSegment(c, x - w, y);
            Curve.CloseCurve(c);
            return c;
        }

        /// <summary>
        /// Creates a curve resembling an inverted house
        /// </summary>
        /// <param name="width">the house width</param>
        /// <param name="height">the house heigth</param>
        /// <param name="center">the house center</param>
        /// <returns></returns>
        static public ICurve CreateInvertedHouse(double width, double height, Point center) {
            ICurve shape = CreateHouse(width, height, center);
            return RotateCurveAroundCenterByDegree(shape, center, 180.0);
        }

        /// <summary>
        /// Creates a curve resembling a diamond large enough to inscribe within it a rectangle of the 
        /// given width and height at center.
        /// </summary>
        /// <param name="width">the width of the inscribed rectangle</param>
        /// <param name="height">the height of the inscribed rectangle</param>
        /// <param name="center">the diamond center</param>
        /// <returns></returns>
        static public ICurve CreateDiamond(double width, double height, Point center) {
            double w = width;
            double h = height;
            double x = center.X;
            double y = center.Y;
            Curve c = new Curve();
            Point[] p = new Point[] { new Point(x, y - h), new Point(x + w, y), new Point(x, y + h), new Point(x - w, y) };
            c.AddSegs(new LineSegment(p[0], p[1]), new LineSegment(p[1], p[2]), new LineSegment(p[2], p[3]), new LineSegment(p[3], p[0]));
            return c;
        }

        /// <summary>
        /// Creates a curve resembling a diamond within the rectangle formed by width and height at center.
        /// </summary>
        /// <param name="width">the bounding rectangle width</param>
        /// <param name="height">the bounding rectangle height</param>
        /// <param name="center">the bounding rectangle center</param>
        /// <returns></returns>
        static public ICurve CreateInteriorDiamond(double width, double height, Point center) {
            return CreateDiamond(width / 2, height / 2, center);
        }

        // This adds the padding to the edges around the inscribed rectangle of an octagon.
        static double octagonPad = 1.0 / 4;

        /// <summary>
        /// Creates a curve of the form of an octagon large enough to inscribe a rectangle
        /// of width and height around center.
        /// </summary>
        /// <param name="width">the inscribed rectangle width</param>
        /// <param name="height">the inscribed rectangle height</param>
        /// <param name="center">the inscribed rectangle (and octagon) center</param>
        /// <returns></returns>
        public static Polyline CreateOctagon(double width, double height, Point center) {
            double w = width / 2;
            double h = height / 2;
            Point[] ps = new Point[8];

            // Pad out horizontally
            ps[0] = new Point(w + (octagonPad * w), h - (h * octagonPad));
            ps[3] = new Point(-ps[0].X, ps[0].Y);
            ps[4] = new Point(ps[3].X, -ps[3].Y);
            ps[7] = new Point(ps[0].X, -ps[0].Y);

            // Pad out vertically
            ps[1] = new Point(w - (w * octagonPad), h + (h * octagonPad));
            ps[2] = new Point(-ps[1].X, ps[1].Y);
            ps[6] = new Point(ps[1].X, -ps[1].Y);
            ps[5] = new Point(ps[2].X, -ps[2].Y);

            for (int i = 0; i < 8; i++) {
                ps[i] += center;
            }

            Polyline polyline = new Polyline(ps) { Closed = true };
            return polyline;
        }

        /// <summary>
        /// Creates a curve of the form of an hexagon large enough to inscribe a rectangle
        /// of width and height around center.
        /// </summary>
        /// <param name="width">the inscribed rectangle width</param>
        /// <param name="height">the inscribed rectangle height</param>
        /// <param name="center">the inscribed rectangle (and hexagon) center</param>
        /// <returns></returns>
        public static ICurve CreateHexagon(double width, double height, Point center) {
            var h = height / 2;
            var w = width / 2;
            var x = center.X;
            var y = center.Y;
            return new Polyline(new[]{
                new Point(-w     + x, - h  + y),
                new Point( w     + x, - h  + y),
                new Point( w + h + x,  0  + y),
                new Point( w     + x, h + y),
                new Point(-w     + x, h + y),
                new Point(-w - h + x, 0  + y),
            }) { Closed = true };
        }

        /// <summary>
        /// Creates a curve in the form of an octagon within the rectangle formed by width and height at center.
        /// </summary>
        /// <param name="width">the bounding rectangle width</param>
        /// <param name="height">the bounding rectangle height</param>
        /// <param name="center">the bounding rectangle center</param>
        /// <returns></returns>
        public static ICurve CreateInteriorOctagon(double width, double height, Point center) {
            return CreateOctagon(width / (1.0 + octagonPad), height / (1.0 + octagonPad), center);
        }

        /// <summary>
        /// testing, don't use
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static ICurve CreateTestShape(double width, double height) {
            int mult = 1;
            double w = width * 3;
            double h = height * 3;
            Curve curve = new Curve(9);
            Curve.AddLineSegment(curve, -w, -h, 0, -h / 2);
            Curve.ContinueWithLineSegment(curve, w / 2, -0.75 * h);
            Curve.ContinueWithLineSegment(curve, w, -h);
            Curve.ContinueWithLineSegment(curve, 0.75 * w, -h / 2);
            Curve.ContinueWithLineSegment(curve, w / 2, 0);
            Curve.ContinueWithLineSegment(curve, w, h);

            Curve.ContinueWithLineSegment(curve, 0, h / 2);
            Curve.ContinueWithLineSegment(curve, -w, mult * h);
            Curve.ContinueWithLineSegment(curve, -w / 3, 0);
            Curve.CloseCurve(curve);
            return curve;
        }

        /// <summary>
        /// create a triangle inside the box formed by width and height.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        static public ICurve CreateInteriorTriangle(double width, double height, Point center) {
            double w = width / 2;
            double h = height / 2;
            double x = center.X;
            double y = center.Y;
            Curve c = new Curve(3);
            Point[] p = new Point[] { new Point(x - w, y - h), new Point(x + w, y - h), new Point(x, y + h) };
            c.AddSegment(new LineSegment(p[0], p[1]));
            c.AddSegment(new LineSegment(p[1], p[2]));
            c.AddSegment(new LineSegment(p[2], p[0]));
            return c;
        }

        /// <summary>
        /// Rotate a curve around a given point using radians
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="center"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static ICurve RotateCurveAroundCenterByRadian(ICurve curve, Point center, double angle) {
            ValidateArg.IsNotNull(curve, "curve");
            var c = Math.Cos(angle);
            var s = Math.Sin(angle);
            var transform = new PlaneTransformation(1, 0, center.X, 0, 1, center.Y) * new PlaneTransformation(c, -s, 0, s, c, 0) * new PlaneTransformation(1, 0, -center.X, 0, 1, -center.Y);
            return curve.Transform(transform);
        }

        /// <summary>
        /// Rotate a curve around a given point using degrees
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="center"></param>
        /// <param name="degree"></param>
        /// <returns></returns>
        public static ICurve RotateCurveAroundCenterByDegree(ICurve curve, Point center, double degree) {
            return RotateCurveAroundCenterByRadian(curve, center, degree * (Math.PI / 180.0));
        }

        ///<summary>
        ///</summary>
        ///<param name="width"></param>
        ///<param name="center"></param>
        ///<returns></returns>
        public static ICurve CreateStar(double width, Point center) {
            const double a = Math.PI * 2 / 5;
            var r2 = width / (2 * Math.Sin(a));
            var r = r2 / 2;
            return new Polyline(StarPoints(r, r2, center, a)) { Closed = true };

        }

        static IEnumerable<Point> StarPoints(double r, double r2, Point center, double a) {
            var ang = Math.PI / 2;
            var anghalf = a / 2;
            for (int i = 0; i < 5; i++) {
                yield return center + r2 * new Point(Math.Cos(ang), Math.Sin(ang));
                yield return center + r * new Point(Math.Cos(ang + anghalf), Math.Sin(ang + anghalf));
                ang += a;
            }
        }

        internal static Polyline CreateRegularPolygon(int n, Point center, double rad) {
            var pt = new Point[n];
            double a = 2 * Math.PI / n;
            for (int i = 0; i < n; i++)
                pt[i] = rad * (new Point(Math.Cos(i * a), Math.Sin(i * a))) + center;
            return new Polyline(pt) { Closed = true };
        }
    }
}
