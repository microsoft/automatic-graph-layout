using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// Check intersections between edges and obstacles using triangulation (faster than kd-tree)
    /// </summary>
    internal class CdtIntersections {
        readonly MetroGraphData metroGraphData;
        readonly BundlingSettings bundlingSettings;

        internal bool ComputeForcesForBundles = false;

        public CdtIntersections(MetroGraphData metroGraphData, BundlingSettings bundlingSettings) {
            this.metroGraphData = metroGraphData;
            this.bundlingSettings = bundlingSettings;
        }

        /// <summary>
        /// returns false iff the edge overlap an obstacle
        /// otherwise it calulates distances to the closest obstacles
        /// </summary>
        internal bool BundleAvoidsObstacles(Station v, Station u, Point vPosition, Point uPosition, double upperBound,
            out List<Tuple<Point, Point>> closestDist) {

            closestDist = new List<Tuple<Point, Point>>();
            //return true;

            Set<Polyline> obstaclesToIgnore = metroGraphData.looseIntersections.ObstaclesToIgnoreForBundle(v, u);
            Dictionary<Polyline, Tuple<Point, Point>> closeObstacles = FindCloseObstaclesForBundle(u.CdtTriangle, uPosition, vPosition, obstaclesToIgnore, upperBound);
            if (closeObstacles == null) return false;

            //Polyline bundle = Intersections.Create4gon(vPosition, uPosition, upperBound, upperBound);

            foreach (var item in closeObstacles) {
                Tuple<Point, Point> dist = item.Value;

                //TODO: get rif od this call!
                //if (!Intersections.ClosedPolylinesIntersect(bundle, obstacle)) continue;
                //if (!Curve.ClosedCurveInteriorsIntersect(bundle, obstacle)) continue;

                closestDist.Add(dist);
           } 

            return true;
        }

        internal bool HubAvoidsObstacles(Station v, Point vPosition, double upperBound,
            out List<double> closestDist) {

            closestDist = new List<double>();
            Set<Polyline> obstaclesToIgnore = metroGraphData.looseIntersections.ObstaclesToIgnoreForHub(v);
            Dictionary<Polyline, double> closeObstacles = FindCloseObstaclesForHub(v.CdtTriangle, v.Position, vPosition, obstaclesToIgnore, upperBound);
            if (closeObstacles == null) return false;

            foreach (var item in closeObstacles) {
                double dist = item.Value;

                closestDist.Add(dist);
            }

            return true;
        }

        /// <summary>
        /// returns null iff the edge overlap an obstacle
        /// </summary>
        Dictionary<Polyline, Tuple<Point, Point>> FindCloseObstaclesForBundle(CdtTriangle startTriangle, Point start,
                                                                              Point end, Set<Polyline> obstaclesToIgnore,
                                                                              double upperBound) {
            var obstacles = new Dictionary<Polyline, Tuple<Point, Point>>();
            List<CdtTriangle> list;
            if (!ThreadLineSegmentThroughTriangles(startTriangle, start, end, obstaclesToIgnore, out list))
                return null;

            if (!ComputeForcesForBundles && !bundlingSettings.HighestQuality)
                return obstacles;

            var checkedSites = new HashSet<CdtSite>();

            foreach (var t in list) {
                foreach (var s in t.Sites) {
                    if (!checkedSites.Add(s)) continue;

                    var poly = (Polyline) s.Owner;
                    if (obstaclesToIgnore.Contains(poly)) continue;

                    PolylinePoint pp = FindPolylinePoint(poly, s.Point);
                    double par11, par12, par21, par22;
                    double d12 = LineSegment.MinDistBetweenLineSegments(pp.Point, pp.NextOnPolyline.Point, start, end,
                                                                  out par11, out par12);
                    double d22 = LineSegment.MinDistBetweenLineSegments(pp.Point, pp.PrevOnPolyline.Point, start, end,
                                                                  out par21, out par22);
                    Point r1, r2;
                    double dist;
                    if (d12 < d22) {
                        dist = d12;
                        if (dist > upperBound) continue;
                        r1 = pp.Point + (pp.NextOnPolyline.Point - pp.Point)*par11;
                        r2 = start + (end - start)*par12;
                    }
                    else {
                        dist = d22;
                        if (dist > upperBound) continue;
                        r1 = pp.Point + (pp.PrevOnPolyline.Point - pp.Point)*par21;
                        r2 = start + (end - start)*par22;
                    }
                    //if (dist > upperBound) continue;

                    Tuple<Point, Point> currentValue;
                    if (!obstacles.TryGetValue(poly, out currentValue))
                        obstacles.Add(poly, new Tuple<Point, Point>(r1, r2));
                }
            }

            return obstacles;
        }

        Dictionary<Polyline, double> FindCloseObstaclesForHub(CdtTriangle startTriangle, Point start, Point end, Set<Polyline> obstaclesToIgnore, double upperBound) {
            var obstacles = new Dictionary<Polyline, double>();
            List<CdtTriangle> nearestTriangles;
            if (!ThreadLineSegmentThroughTriangles(startTriangle, start, end, obstaclesToIgnore, out nearestTriangles))
                return null;

            var checkedSites = new HashSet<CdtSite>();

            foreach (var t in nearestTriangles) {
                foreach (var s in t.Sites) {
                    CheckSite(end, obstaclesToIgnore, checkedSites, s, upperBound, obstacles);

                    var edge = t.OppositeEdge(s);
                    var ot = edge.GetOtherTriangle(t);
                    if (ot != null) {
                        CheckSite(end, obstaclesToIgnore, checkedSites, ot.OppositeSite(edge), upperBound, obstacles);
                    }
                }
            }

            return obstacles;
        }

        void CheckSite(Point end, Set<Polyline> obstaclesToIgnore, HashSet<CdtSite> checkedSites, CdtSite s, double upperBound, Dictionary<Polyline, double> obstacles) {
            if (!checkedSites.Add(s)) return;

            var poly = (Polyline)s.Owner;
            if (obstaclesToIgnore.Contains(poly)) return;

            //distance to the obstacle
            PolylinePoint pp = FindPolylinePoint(poly, s.Point);
            double par;
            double d12 = Point.DistToLineSegment(end, pp.Point, pp.NextOnPolyline.Point, out par);
            double d22 = Point.DistToLineSegment(end, pp.Point, pp.PrevOnPolyline.Point, out par);
            double dist = Math.Min(d12, d22);
            if (dist > upperBound) return;

            double currentValue;
            if (!obstacles.TryGetValue(poly, out currentValue)) {
                obstacles.Add(poly, dist);
            }
            else if (currentValue > dist) {
                obstacles[poly] = dist;
            }
        }
        
        /// <summary>
        /// returns false iff the edge overlap an obstacle
        /// </summary>
        bool ThreadLineSegmentThroughTriangles(CdtTriangle currentTriangle, Point start, Point end, Set<Polyline> obstaclesToIgnore,
            out List<CdtTriangle> triangles) {
            Debug.Assert(Cdt.PointIsInsideOfTriangle(start, currentTriangle));
            triangles = new List<CdtTriangle>();

            if (Cdt.PointIsInsideOfTriangle(end, currentTriangle)) {
                triangles.Add(currentTriangle);
                return true;
            }

            var threader = new CdtThreader(currentTriangle, start, end);
            triangles.Add(currentTriangle);

            while (threader.MoveNext()) {
                triangles.Add(threader.CurrentTriangle);
                var piercedEdge = threader.CurrentPiercedEdge;
                if (piercedEdge.Constrained) {
                    Debug.Assert(piercedEdge.lowerSite.Owner == piercedEdge.upperSite.Owner);
                    var poly = (Polyline) piercedEdge.lowerSite.Owner;
                    if (!obstaclesToIgnore.Contains(poly)) return false;
                }                
            }
            if (threader.CurrentTriangle != null)
                triangles.Add(threader.CurrentTriangle);
//
//            int positiveSign, negativeSign;
//            CdtEdge piercedEdge = FindFirstPiercedEdge(currentTriangle, start, end, out negativeSign, out positiveSign,  null);
//            
//            Debug.Assert(positiveSign > negativeSign);
//
//            Debug.Assert(piercedEdge != null);
//
//            do {
//                triangles.Add(currentTriangle);
//                if (piercedEdge.Constrained) {
//                    Debug.Assert(piercedEdge.lowerSite.Owner == piercedEdge.upperSite.Owner);
//                    Polyline poly = (Polyline)piercedEdge.lowerSite.Owner;
//                    if (!obstaclesToIgnore.Contains(poly)) return false;
//                }
//            }
//            while (FindNextPierced(start, end, ref currentTriangle, ref piercedEdge, ref negativeSign, ref positiveSign));
//            if (currentTriangle != null)
//                triangles.Add(currentTriangle);

            return true;
        }

        static internal int GetHyperplaneSign(Point start, Point end, CdtSite cdtSite) {
            var area = Point.SignedDoubledTriangleArea(start, cdtSite.Point, end);
            if (area > ApproximateComparer.DistanceEpsilon) return 1;
            if (area < -ApproximateComparer.DistanceEpsilon) return -1;
            return 0;
        }

        internal static PointLocation PointLocationInsideTriangle(Point p, CdtTriangle triangle) {
            bool seenBoundary=false;
            for (int i = 0; i < 3; i++) {
                var area = Point.SignedDoubledTriangleArea(p, triangle.Sites[i].Point, triangle.Sites[i + 1].Point);
                if (area < -ApproximateComparer.DistanceEpsilon)
                    return PointLocation.Outside;
                if (area < ApproximateComparer.DistanceEpsilon)
                    seenBoundary = true;
            }

            return seenBoundary ? PointLocation.Boundary : PointLocation.Inside;
        }

        
        static PolylinePoint FindPolylinePoint(Polyline poly, Point point) {
            foreach (var ppp in poly.PolylinePoints)
                if (ppp.Point == point)
                    return ppp;

            throw new NotSupportedException();
        }

        /// <summary>
        /// checks if an edge intersects obstacles
        /// otherwise it calulates distances to the closest obstacles
        /// </summary>
        internal bool EdgeIsLegal(Station v, Station u, Point vPosition, Point uPosition) {
            List<CdtTriangle> list;
            Set<Polyline> obstaclesToIgnore = metroGraphData.looseIntersections.ObstaclesToIgnoreForBundle(v, u);
            return ThreadLineSegmentThroughTriangles(v.CdtTriangle, vPosition, uPosition, obstaclesToIgnore, out list);
        }

        /// <summary>
        /// checks if an edge intersects obstacles
        /// otherwise it calulates distances to the closest obstacles
        /// </summary>
        internal bool EdgeIsLegal(Station v, Station u, Point vPosition, Point uPosition, Set<Polyline> obstaclesToIgnore) {
            var start = v.Position;

            CdtTriangle currentTriangle = v.CdtTriangle;
            Debug.Assert(Cdt.PointIsInsideOfTriangle(start, currentTriangle));

            Point end = u.Position;
            if (Cdt.PointIsInsideOfTriangle(end, currentTriangle)) {
                return true;
            }

            var threader = new CdtThreader(currentTriangle, start, end);
            
            while (threader.MoveNext()) {
                var piercedEdge = threader.CurrentPiercedEdge;
                if (piercedEdge.Constrained) {
                    Debug.Assert(piercedEdge.lowerSite.Owner == piercedEdge.upperSite.Owner);
                    var poly = (Polyline)piercedEdge.lowerSite.Owner;
                    if (!obstaclesToIgnore.Contains(poly)) return false;
                }
            }
            return true;
        }

    }
}
