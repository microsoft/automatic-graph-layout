using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;

#if TEST_MSAGL

#endif
namespace Microsoft.Msagl.Routing.Visibility {
    /// <summary>
    /// the polygon is going clockwise
    /// </summary>

#if TEST_MSAGL
    [Serializable]
#endif
    internal class Polygon {
        Polyline polyline;

        internal Polyline Polyline {
            get { return polyline; }
#if TEST_MSAGL
            set { polyline = value; }
#endif
        }

        readonly PolylinePoint[] points;

        internal Polygon(Polyline polyline) {
            this.polyline = polyline;
            points = new PolylinePoint[polyline.Count];
            int i = 0;
            PolylinePoint pp = polyline.StartPoint;
            for (; i < polyline.Count; i++, pp = pp.Next)
                points[i] = pp;

        }

        internal int Next(int i) {
            return Module(i + 1);
        }

        internal int Prev(int i) {
            return Module(i - 1);
        }

        internal int Count { get { return Polyline.Count; } }

        internal int Module(int i) {
            if (i < 0)
                return i + Count;
            if (i < Count)
                return i;
            return i - Count;
        }

        internal PolylinePoint this[int i] {
            get {
                return points[Module(i)];
            }
        }


        //private LineSegment ls(Point pivot, int p) {
        //    return new LineSegment(pivot, Pnt(p));
        //}

        internal Point Pnt(int i) {
            return this[i].Point;
        }


        public override string ToString() {
            return polyline.ToString();
        }

        /// <summary>
        /// the median of a chunk going clockwise from p1 to p2
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        internal int Median(int p1, int p2) {
            System.Diagnostics.Debug.Assert(p1 != p2);//otherwise we do not know what arc is mean: the whole one or just the point
            if (p2 > p1)
                return (p2 + p1) / 2;

            return Module((p2 + Count + p1) / 2);
        }

        /// <summary>
        /// p1 and p2 represent the closest feature. Two cases are possible p1=p2, or p1 and p2 share an edge going from p1 to p2
        /// Remind that the polygons are oriented clockwise
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="bisectorPivot"></param>
        /// <param name="bisectorRay"></param>
        /// <returns></returns>
        internal int FindTheFurthestVertexFromBisector(int p1, int p2, Point bisectorPivot, Point bisectorRay) {
            Point directionToTheHill = bisectorRay.Rotate(Math.PI / 2);
            if ((polyline.StartPoint.Point - bisectorPivot) * directionToTheHill < 0)
                directionToTheHill = -directionToTheHill;
            if (p1 == p2)
                p2 = Next(p1);
            //binary search
            do {
                int m = Median(p2, p1); //now the chunk goes clockwise from p2 to p1
                Point mp = Pnt(m);

                if ((Pnt(Next(m)) - mp) * directionToTheHill >= 0)
                    p2 = Next(m);
                else if ((Pnt(Prev(m)) - mp) * directionToTheHill >= 0)
                    p1 = Prev(m);
                else
                    p1 = p2 = m;
            }
            while (p1 != p2);

            return p1;
        }
#if TEST_MSAGL
        // ReSharper disable UnusedMember.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static double TestPolygonDist(Polygon a, Polygon b) {
            // ReSharper restore UnusedMember.Local
            double ret = double.PositiveInfinity, u, v;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    ret = Math.Min(ret, LineSegment.MinDistBetweenLineSegments(a.Pnt(i), a.Pnt(i + 1), b.Pnt(j),
                                                                                b.Pnt(j + 1), out u, out v));

            return ret;
        }
#endif

        /// <summary>
        /// Distance between two polygons
        /// p and q are the closest points
        /// The function doesn't work if the polygons intersect each other
        /// </summary>
        static public double Distance(Polygon a, Polygon b, out Point p, out Point q) {
            var tp = new TangentPair(a, b);
            tp.FindClosestPoints(out p, out q);
#if TEST_MSAGL
            if(!ApproximateComparer.Close((p - q).Length,TestPolygonDist(a,b))) {
                using(var stream = File.Open(@"c:\tmp\polygonBug",FileMode.Create)) {
                    var bf = new BinaryFormatter();
                    bf.Serialize(stream, a);
                    bf.Serialize(stream, b);                    
                }
                LayoutAlgorithmSettings.ShowDebugCurves?.Invoke(
                    new DebugCurve(100, 0.1, "red", a.Polyline),
                    new DebugCurve(100, 0.1, "blue", b.Polyline),
                    new DebugCurve(100, 0.1, "black", new LineSegment(p, q)));
                System.Diagnostics.Debug.Fail("wrong distance between two polygons");

            }
#endif
            return (p - q).Length;
        }

        /// <summary>
        /// Distance between two polygons
        /// </summary>
        static public double Distance(Polygon a, Polygon b) {
            System.Diagnostics.Debug.Assert(PolygonIsLegalDebug(a));
            System.Diagnostics.Debug.Assert(PolygonIsLegalDebug(b));

            Point p, q;
            return Distance(a, b, out p, out q);
        }

        private static bool PolygonIsLegalDebug(Polygon a)
        {
            var poly = a.Polyline;
            for (var p = poly.StartPoint; p.Next != null && p.Next.Next != null; p = p.Next)
                if (Point.GetTriangleOrientation(p.Point, p.Next.Point, p.Next.Next.Point) ==
                    TriangleOrientation.Collinear)
                    return false;
            return true;
        }

        /// <summary>
        /// Distance between polygon and point
        /// </summary>
        static public double Distance(Polygon poly, Point b)
        {
            double res = double.PositiveInfinity;
            for (int i = 0; i < poly.Count; i++) {
                double par;
                double dist = Point.DistToLineSegment(b, poly.points[i].Point, poly.points[(i + 1) % poly.Count].Point, out par);
                res = Math.Min(res, dist);
            }
            return res;
        }

        internal void GetTangentPoints(out int leftTangentPoint, out int rightTangentPoint, Point point) {
            var bimodalSequence = new BimodalSequence(GetSequenceDelegate(point), Count);
            leftTangentPoint = bimodalSequence.FindMaximum();
            rightTangentPoint = bimodalSequence.FindMinimum();
        }

        private Func<int, double> GetSequenceDelegate(Point point) {
            Point pointOfP = Pnt(0);
            return delegate(int i) {
                double d = Point.Angle(pointOfP, point, Pnt(i));
                return d < Math.PI ? d : d - 2 * Math.PI;
            };
        }
    }
}