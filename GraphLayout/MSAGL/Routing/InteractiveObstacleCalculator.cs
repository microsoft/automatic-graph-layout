using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing {
    /// <summary>
    /// calculations with obstacles
    /// </summary>
    public class InteractiveObstacleCalculator {
        Dictionary<Polyline,double> tightPolylinesToLooseDistances;
        double TightPadding { get; set; }
        internal double LoosePadding { get; set; }

        internal InteractiveObstacleCalculator(IEnumerable<ICurve> obstacles, double tightPadding, double loosePadding, bool ignoreTightPadding)
        {
            Obstacles = obstacles;
            TightPadding = tightPadding;
            LoosePadding = loosePadding;
            IgnoreTightPadding = ignoreTightPadding;
        }

        /// <summary>
        /// the obstacles for routing
        /// </summary>
        IEnumerable<ICurve> Obstacles { get; set; }
        

        /// <summary>
        /// Returns true if overlaps are detected in the initial set of TightObstacles.
        /// TightObstacles will then have been repaired by merging each overlapping group 
        /// of shapes into a single obstacle.
        /// </summary>
        public bool OverlapsDetected { get; private set; }

        Set<Polyline> tightObstacles=new Set<Polyline>();

        /// <summary>
        /// Before routing we pad shapes such that every point of the boundary
        /// of the padded shape is at least router.Padding outside the boundary of the original shape.
        /// We also add extra faces to round corners a little.  Edge paths are guaranteed not to come
        /// inside this padded TightObstacle boundary even after spline smoothing.
        /// </summary>
        internal Set<Polyline> TightObstacles {
            get { return tightObstacles; }
            private set { tightObstacles = value; }
        }

        /// <summary>
        /// We also pad TightObstacles by router.LoosePadding (where possible) to generate the shape
        /// overwhich the visibility graph is actually generated.
        /// </summary>
        internal List<Polyline> LooseObstacles { get; private set; }

        /// <summary>
        /// Root of binary space partition tree used for rapid region queries over TightObstacles.
        /// </summary>
        // TODO: replace these with the BinarySpacePartitionTree wrapper class
        internal RectangleNode<Polyline, Point> RootOfTightHierarchy { get; set; }

        /// <summary>
        /// Root of binary space partition tree used for rapid region queries over LooseObstacles.
        /// </summary>
        // TODO: replace these with the BinarySpacePartitionTree wrapper class
        internal RectangleNode<Polyline, Point> RootOfLooseHierarchy { get; set; }

        internal bool IgnoreTightPadding {
            get;
            set;
        }

        internal const double LooseDistCoefficient = 2.1;

        
        /// <summary>
        /// There are two sets of obstacles: tight and loose.
        /// We route the shortest path between LooseObstacles, and then smooth it with splines but without
        /// going inside TightObstacles.
        /// </summary>
        /// <returns></returns>
        internal void Calculate() {
            if (!IgnoreTightPadding)
                CreateTightObstacles();
            else
                CreateTightObstaclesIgnoringTightPadding();
            if (!IsEmpty())
                CreateLooseObstacles();
        }

        /// <summary>
        /// 
        /// </summary>
        internal void CreateLooseObstacles() {
            tightPolylinesToLooseDistances = new Dictionary<Polyline, double>();
            LooseObstacles = new List<Polyline>();
            foreach (var tightPolyline in TightObstacles) {
                var distance = FindMaxPaddingForTightPolyline(RootOfTightHierarchy, tightPolyline, LoosePadding);
                tightPolylinesToLooseDistances[tightPolyline] = distance;
                LooseObstacles.Add(LoosePolylineWithFewCorners(tightPolyline, distance));
            }
            RootOfLooseHierarchy = CalculateHierarchy(LooseObstacles);
            Debug.Assert(GetOverlappedPairSet(RootOfLooseHierarchy).Count == 0,"Overlaps are found in LooseObstacles");
        }


        //internal void ShowRectangleNodesHierarchy(RectangleNode<Polyline, Point> node) {
        //    List<ICurve> ls = new List<ICurve>();
        //    FillList(ls, node);
        //    SugiyamaLayoutSettings.Show(ls.ToArray());
        //}

        //internal void FillList(List<ICurve> ls, RectangleNode<Polyline, Point> node) {
        //    if (node == null)
        //        return;
        //    if (node.UserData != null)
        //        ls.Add(node.UserData);
        //    else {
        //        FillList(ls, node.Left);
        //        FillList(ls, node.Right);
        //    }
        //}

        internal static void UpdateRectsForParents(RectangleNode<Polyline, Point> node) {
            while (node.Parent != null) {
                node.Parent.rectangle.Add(node.Rectangle);
                node = node.Parent;
            }
        }

        /// <summary>
        /// Handling overlapping obstacles:
        ///  - create tightobstacles
        ///  - find overlapping tightobstacles
        ///    - replace with convexhull of overlapping
        ///  
        /// Not particularly optimal method O(m * n log n) - where m is number of overlaps, n is number of obstacles:
        ///
        /// overlapping = 0
        /// do
        ///   foreach o in TightObstacles:
        ///     I = set of all other obstacles which intersect o
        ///     if I != 0
        ///       overlapping = I + o
        ///       break  
        ///   if overlapping != 0
        ///     combinedObstacle = new obstacle from convex hull of overlapping
        ///     tightObstacles.delete(overlapping)
        ///     tightObstacles.add(combinedObstacle)
        /// while overlapping != 0
        /// </summary>
        void CreateTightObstacles() {
            RootOfTightHierarchy = CreateTightObstacles(Obstacles, TightPadding, TightObstacles);
            OverlapsDetected = TightObstacles.Count < Obstacles.Count();
        }

        static internal RectangleNode<Polyline, Point> CreateTightObstacles(IEnumerable<ICurve> obstacles, double tightPadding, Set<Polyline> tightObstacleSet) {
            Debug.Assert(tightObstacleSet!=null);
            if(obstacles.Count()==0)
                return null;
            foreach (ICurve curve in obstacles)
                CalculateTightPolyline(tightObstacleSet, tightPadding, curve);

            return RemovePossibleOverlapsInTightPolylinesAndCalculateHierarchy(tightObstacleSet);
        }

        static internal RectangleNode<Polyline, Point> RemovePossibleOverlapsInTightPolylinesAndCalculateHierarchy(Set<Polyline> tightObstacleSet) {
            var hierarchy = CalculateHierarchy(tightObstacleSet);
            Set<Tuple<Polyline, Polyline>> overlappingPairSet;
            while ((overlappingPairSet = GetOverlappedPairSet(hierarchy)).Count > 0)
                hierarchy = ReplaceTightObstaclesWithConvexHulls(tightObstacleSet, overlappingPairSet);
            return hierarchy;
        }

        void CreateTightObstaclesIgnoringTightPadding() {
            var polysWithoutPadding = Obstacles.Select(o => Curve.PolylineAroundClosedCurve(o)).ToArray();
            var polylineHierarchy = CalculateHierarchy(polysWithoutPadding);
            var overlappingPairSet = GetOverlappedPairSet(polylineHierarchy);
            TightObstacles = new Set<Polyline>();
            if (overlappingPairSet.Count == 0) {
                foreach (var polyline in polysWithoutPadding) {
                    var distance = FindMaxPaddingForTightPolyline(polylineHierarchy, polyline, TightPadding);
                    TightObstacles.Insert(LoosePolylineWithFewCorners(polyline, distance));
                }
                RootOfTightHierarchy = CalculateHierarchy(TightObstacles);
            } else {

                foreach (var poly in polysWithoutPadding)
                    TightObstacles.Insert(CreatePaddedPolyline(poly, TightPadding));


                if (!IsEmpty()) {
                    RootOfTightHierarchy = CalculateHierarchy(TightObstacles);
                    OverlapsDetected = false;
                    while ((overlappingPairSet = GetOverlappedPairSet(RootOfTightHierarchy)).Count > 0) {
                        RootOfTightHierarchy= ReplaceTightObstaclesWithConvexHulls(TightObstacles, overlappingPairSet);
                        OverlapsDetected = true;
                    }
                }
            }
        }


        static void CalculateTightPolyline(Set<Polyline> tightObstacles, double tightPadding, ICurve curve) {
            var tightPoly = PaddedPolylineBoundaryOfNode(curve, tightPadding);
//            if (AdditionalPolylinesContainedInTightPolyline != null) {
//                var polysToContain = AdditionalPolylinesContainedInTightPolyline(curve);
//                if (polysToContain.Any(poly => !Curve.CurveIsInsideOther(poly, tightPoly))) {
//                    var points = (IEnumerable<Point>)tightPoly;
//                    points = points.Concat(polysToContain.SelectMany(p => p));
//                    tightPoly = new Polyline(ConvexHull.CalculateConvexHull(points)) { Closed = true };
//                }
//            }
            tightObstacles.Insert(tightPoly);
        }


        internal static RectangleNode<Polyline, Point> ReplaceTightObstaclesWithConvexHulls(Set<Polyline> tightObsts, IEnumerable<Tuple<Polyline, Polyline>> overlappingPairSet) {
            var overlapping = new Set<Polyline>();
            foreach (var pair in overlappingPairSet) {
                overlapping.Insert(pair.Item1);
                overlapping.Insert(pair.Item2);
            }
            var intToPoly = overlapping.ToArray();
            var polyToInt = MapToInt(intToPoly);
            var graph = new BasicGraphOnEdges<IntPair>(
                    overlappingPairSet.
                    Select(pair => new IntPair(polyToInt[pair.Item1], polyToInt[pair.Item2])));
            var connectedComponents = ConnectedComponentCalculator<IntPair>.GetComponents(graph);
            foreach (var component in connectedComponents) {
                var polys = component.Select(i => intToPoly[i]);
                var points = polys.SelectMany(p => p);
                var convexHull = ConvexHull.CreateConvexHullAsClosedPolyline(points);
                foreach (var poly in polys)
                    tightObsts.Remove(poly);
                tightObsts.Insert(convexHull);
            }
            return CalculateHierarchy(tightObsts);
        }

        internal static Dictionary<T, int> MapToInt<T>(T[] objects) {
            var ret = new Dictionary<T, int>();
            for (int i = 0; i < objects.Length; i++)
                ret[objects[i]] = i;
            return ret;
        }

        internal bool IsEmpty()
        {
            return TightObstacles == null || TightObstacles.Count == 0;
        }

        internal static Set<Tuple<Polyline, Polyline>> GetOverlappedPairSet(RectangleNode<Polyline, Point> rootOfObstacleHierarchy)
        {
            var overlappingPairSet = new Set<Tuple<Polyline, Polyline>>();
            RectangleNodeUtils.CrossRectangleNodes<Polyline,Point>(rootOfObstacleHierarchy, rootOfObstacleHierarchy,
                                                   (a, b) =>
                                                       {
                                                           if (PolylinesIntersect(a, b))
                                                           {
                                                               overlappingPairSet.Insert(
                                                                   new Tuple<Polyline, Polyline>(a, b));
                                                           }
                                                       });
            return overlappingPairSet;
        }

        internal static bool PolylinesIntersect(Polyline a, Polyline b) {
            var ret= Curve.CurvesIntersect(a, b) ||
                   OneCurveLiesInsideOfOther(a, b);         
            return ret;
        }


        internal static RectangleNode<Polyline, Point> CalculateHierarchy(IEnumerable<Polyline> polylines) {
            var rectNodes = polylines.Select(polyline => CreateRectNodeOfPolyline(polyline)).ToList();
            return RectangleNode<Polyline, Point>.CreateRectangleNodeOnListOfNodes(rectNodes);
        }


        static RectangleNode<Polyline, Point> CreateRectNodeOfPolyline(Polyline polyline) {
            return new RectangleNode<Polyline, Point>(polyline, (polyline as ICurve).BoundingBox);
        }

        internal static bool OneCurveLiesInsideOfOther(ICurve polyA, ICurve polyB) {
            Debug.Assert(!Curve.CurvesIntersect(polyA, polyB), "The curves should not intersect");
            return (Curve.PointRelativeToCurveLocation(polyA.Start, polyB) != PointLocation.Outside ||
                    Curve.PointRelativeToCurveLocation(polyB.Start, polyA) != PointLocation.Outside);
        }

        static internal Polyline CreatePaddedPolyline(Polyline poly, double padding) {

            Debug.Assert(Point.GetTriangleOrientation(poly[0], poly[1], poly[2]) == TriangleOrientation.Clockwise
                         , "Unpadded polyline is not clockwise");

            var ret = new Polyline();
            if (!PadCorner(ret, poly.EndPoint.Prev, poly.EndPoint, poly.StartPoint, padding))
                return CreatePaddedPolyline(new Polyline(ConvexHull.CalculateConvexHull(poly)) {Closed = true}, padding);

            if (!PadCorner(ret, poly.EndPoint, poly.StartPoint, poly.StartPoint.Next, padding))
                return CreatePaddedPolyline(new Polyline(ConvexHull.CalculateConvexHull(poly)) {Closed = true}, padding);


            for (var pp = poly.StartPoint; pp.Next.Next != null; pp = pp.Next)
                if (!PadCorner(ret, pp, pp.Next, pp.Next.Next, padding))
                    return CreatePaddedPolyline(new Polyline(ConvexHull.CalculateConvexHull(poly)) {Closed = true},
                                                padding);

            Debug.Assert(Point.GetTriangleOrientation(ret[0], ret[1], ret[2]) != TriangleOrientation.Counterclockwise
                         , "Padded polyline is counterclockwise");

            ret.Closed = true;
            return ret;
        }

        /// <summary>
        /// return true if succeeds and false if it is a non convex case
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        static bool PadCorner(Polyline poly, PolylinePoint p0, PolylinePoint p1, PolylinePoint p2, double padding) {
            Point a, b;
            int numberOfPoints = GetPaddedCorner(p0, p1, p2, out a, out b, padding);
            if (numberOfPoints == -1)
                return false;
            poly.AddPoint(a);
            if (numberOfPoints == 2)
                poly.AddPoint(b);
            return true;
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
        static int GetPaddedCorner(PolylinePoint first, PolylinePoint second, PolylinePoint third, out Point a,
                                   out Point b,
                                   double padding) {
            Point u = first.Point;
            Point v = second.Point;
            Point w = third.Point;
            if (Point.GetTriangleOrientation(u, v, w) == TriangleOrientation.Counterclockwise) {
                a = new Point();
                b = new Point();
                return -1;
            }


            Point uvPerp = (v - u).Rotate(Math.PI/2).Normalize();

            if (CornerIsNotTooSharp(u, v, w)) {
                //the angle is not too sharp: just continue the offset lines of the sides and return their intersection
                uvPerp *= padding;
                Point vwPerp = ((w - v).Normalize()*padding).Rotate(Math.PI/2);

                bool result = Point.LineLineIntersection(u + uvPerp, v + uvPerp, v + vwPerp, w + vwPerp, out a);
                Debug.Assert(result);
                b = a;
                return 1;
            }

            Point l = (v - u).Normalize() + (v - w).Normalize();
            if (l.Length < ApproximateComparer.IntersectionEpsilon) {
                a = b = v + padding*uvPerp;
                return 1;
            }
            Point d = l.Normalize()*padding;
            Point dp = d.Rotate(Math.PI/2);

            //look for a in the form d+x*dp
            //we have:  Padding=(d+x*dp)*uvPerp
            double xp = (padding - d*uvPerp)/(dp*uvPerp);
            a = d + xp*dp + v;
            b = d - xp*dp + v;
            return 2; //number of points to add 
        }

        static bool CornerIsNotTooSharp(Point u, Point v, Point w) {
            Point a = (u - v).Rotate(Math.PI/4) + v;
            return Point.GetTriangleOrientation(v, a, w) == TriangleOrientation.Counterclockwise;

            //   return Point.Angle(u, v, w) > Math.PI / 4;
        }


        /// <summary>
        /// in general works for convex curves
        /// </summary>
        /// <param name="iCurve"></param>
        /// <param name="pointInside"></param>
        /// <returns></returns>
        internal static bool CurveIsClockwise(ICurve iCurve, Point pointInside) {
            return
                Point.GetTriangleOrientation(pointInside, iCurve.Start,
                                             iCurve.Start + iCurve.Derivative(iCurve.ParStart)) ==
                TriangleOrientation.Clockwise;
        }

        /// <summary>
        /// Creates a padded polyline boundary of the node. The polyline offsets at least as the padding from the node boundary.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public static Polyline PaddedPolylineBoundaryOfNode(ICurve curve, double padding) {
            return CreatePaddedPolyline(Curve.PolylineAroundClosedCurve(curve), padding);
        }

        internal static Polyline LoosePolylineWithFewCorners(Polyline tightPolyline, double p) {
            if (p < ApproximateComparer.DistanceEpsilon)
                return tightPolyline;
            Polyline loosePolyline = CreateLoosePolylineOnBisectors(tightPolyline, p);

            //LayoutAlgorithmSettings.Show(tightPolyline, loosePolyline);

            return loosePolyline;
        }

        static Polyline CreateLoosePolylineOnBisectors(Polyline tightPolyline, double offset) {
            return new Polyline(ConvexHull.CalculateConvexHull(BisectorPoints(tightPolyline, offset))) { Closed = true };
        }

        static IEnumerable<Point> BisectorPoints(Polyline tightPolyline, double offset){
            List<Point> ret=new List<Point>();
            for (PolylinePoint pp = tightPolyline.StartPoint; pp != null; pp = pp.Next) {
                bool skip;
                Point currentSticking = GetStickingVertexOnBisector(pp, offset, out skip);
                if (!skip)
                    ret.Add( currentSticking);
            }
            return ret;
        }

        static Point GetStickingVertexOnBisector(PolylinePoint pp, double p, out bool skip) {
            Point u = pp.Polyline.Prev(pp).Point;
            Point v = pp.Point;
            Point w = pp.Polyline.Next(pp).Point;
            var z = (v - u).Normalize() + (v - w).Normalize();
            var zLen = z.Length;
            if (zLen < ApproximateComparer.Tolerance)
                skip = true;
            else {
                skip = false;
                z /= zLen;
            }
            return p*z + v;
        }

        

        /// <summary>
        /// Find tight obstacles close to the specified polyline and find the max amount of padding (up to
        /// desiredPadding) which can be applied to the polyline such that it will not overlap any of the
        /// surrounding polylines when they are also padded.  That is, we find the minimum separation
        /// between these shapes and divide by 2 (and a bit) - and if this is less than desiredPadding
        /// we return this as the amount to padd the polyline to create the looseObstacle.
        /// </summary>
        /// <param name="hierarchy"></param>
        /// <param name="polyline">a polyline to pad (tightObstacle)</param>
        /// <param name="desiredPadding">desired amount to pad</param>
        /// <returns>maximum amount we can pad without creating overlaps</returns>
        internal static double FindMaxPaddingForTightPolyline(RectangleNode<Polyline, Point> hierarchy, Polyline polyline, double desiredPadding ) {
            var dist = desiredPadding;
            var polygon = new Polygon(polyline);
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=369 there are no structs in js
            var boundingBox = polyline.BoundingBox.Clone();
#else
            var boundingBox = polyline.BoundingBox;
#endif
            boundingBox.Pad(2.0 * desiredPadding);
            foreach (var poly in hierarchy.GetNodeItemsIntersectingRectangle(boundingBox).Where(p=>p!=polyline)) {
                var separation = Polygon.Distance(polygon, new Polygon(poly));
                dist = Math.Min(dist, separation/LooseDistCoefficient);
            }
            return dist;
        }


        internal bool ObstaclesIntersectLine(Point a, Point b) {
            return ObstaclesIntersectICurve(new LineSegment(a, b));
        }

        internal bool ObstaclesIntersectICurve(ICurve curve) {
            Rectangle rect = curve.BoundingBox;
            return CurveIntersectsRectangleNode(curve, ref rect, RootOfTightHierarchy);
        }

        static bool CurveIntersectsRectangleNode(ICurve curve, ref Rectangle curveBox, RectangleNode<Polyline, Point> rectNode) {
            if (!rectNode.Rectangle.Intersects(curveBox))
                return false;

            if (rectNode.UserData != null) {
                var curveUnderTest = rectNode.UserData;
                return Curve.CurveCurveIntersectionOne(curveUnderTest, curve, false) != null ||
                       Inside(curveUnderTest, curve);
            }
            Debug.Assert(rectNode.Left != null && rectNode.Right != null);

            return CurveIntersectsRectangleNode(curve, ref curveBox, rectNode.Left) ||
                   CurveIntersectsRectangleNode(curve, ref curveBox, rectNode.Right);
        }

        /// <summary>
        /// we know here that there are no intersections between "curveUnderTest" and "curve",
        /// We are testing that curve is inside of "curveUnderTest"
        /// </summary>
        /// <param name="curveUnderTest"></param>
        /// <param name="curve"></param>
        /// <returns></returns>
        static bool Inside(ICurve curveUnderTest, ICurve curve) {
            return Curve.PointRelativeToCurveLocation(curve.Start, curveUnderTest) == PointLocation.Inside;
        }
    }
}
