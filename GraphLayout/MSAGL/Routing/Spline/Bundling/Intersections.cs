using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Routing.Visibility;
using Microsoft.Msagl.DebugHelpers;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// Check intersections between hubs and obstacles with kd-tree
    /// </summary>
    internal class Intersections {
        readonly MetroGraphData metroGraphData;
        readonly BundlingSettings bundlingSettings;
        Func<Station, Set<Polyline>> obstaclesToIgnore;
        /// <summary>
        /// represents loose or tight hierarchy
        /// </summary>
        internal RectangleNode<Polyline, Point> obstacleTree { get; set; }

        public Intersections(MetroGraphData metroGraphData, BundlingSettings bundlingSettings,
            RectangleNode<Polyline, Point> obstacleTree, Func<Station, Set<Polyline>> obstaclesToIgnore) {
            this.metroGraphData = metroGraphData;
            this.obstaclesToIgnore = obstaclesToIgnore;
            this.bundlingSettings = bundlingSettings;
            this.obstacleTree = obstacleTree;
        }

        internal Set<Polyline> ObstaclesToIgnoreForBundle(Station node, Station adj) {
            if (node != null && adj != null)
                return obstaclesToIgnore(node) + obstaclesToIgnore(adj);

            if (node == null && adj == null)
                return new Set<Polyline>();

            if (node != null) return obstaclesToIgnore(node);
            else return obstaclesToIgnore(adj);
        }

        #region Intersections with hub

        internal bool HubAvoidsObstacles(Station node, Point center, double upperBound,
            out List<Tuple<Polyline, Point>> touchedObstacles) {
            touchedObstacles = new List<Tuple<Polyline, Point>>();
            double minimalDistance = upperBound;
            return IntersectCircleWithTree(obstacleTree, center, upperBound, obstaclesToIgnore(node), touchedObstacles, ref minimalDistance);
        }

        internal bool HubAvoidsObstacles(Point center, double upperBound, Set<Polyline> obstaclesToIgnore) {
            List<Tuple<Polyline, Point>> touchedObstacles;
            double minimalDistance;
            return HubAvoidsObstacles(center, upperBound, obstaclesToIgnore, out touchedObstacles, out minimalDistance);
        }

        internal double GetMinimalDistanceToObstacles(Station node, Point nodePosition, double upperBound) {
            List<Tuple<Polyline, Point>> touchedObstacles = new List<Tuple<Polyline, Point>>();
            double minimalDistance = upperBound;
            if (!IntersectCircleWithTree(obstacleTree, nodePosition, upperBound, obstaclesToIgnore(node), touchedObstacles, ref minimalDistance))
                return 0;

            return minimalDistance;
        }

        bool HubAvoidsObstacles(Point center, double upperBound, Set<Polyline> obstaclesToIgnore,
            out List<Tuple<Polyline, Point>> touchedObstacles, out double minimalDistance) {
            touchedObstacles = new List<Tuple<Polyline, Point>>();
            minimalDistance = upperBound;
            return IntersectCircleWithTree(obstacleTree, center, upperBound, obstaclesToIgnore, touchedObstacles, ref minimalDistance);
        }

        /// <summary>
        /// Computes the intersection between the hub and obstacles
        /// </summary>
        /// <param name="node"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="obstaclesToIgnore">these are the obstacles the are ignored by the method</param>
        /// <param name="touchedObstacles">list of pairs (obstacle, closest point on the obstacle)</param>
        /// <param name="minimalDistance">min distance from the center to an obstacle</param>
        /// <returns>false iff center is inside of an obstacle</returns>
        static bool IntersectCircleWithTree(RectangleNode<Polyline, Point> node, Point center, double radius, Set<Polyline> obstaclesToIgnore,
            List<Tuple<Polyline, Point>> touchedObstacles, ref double minimalDistance) {
            if (!node.Rectangle.Contains(center, radius))
                return true;

            if (node.UserData == null) {
                bool res = IntersectCircleWithTree(node.Left, center, radius, obstaclesToIgnore, touchedObstacles, ref minimalDistance);
                if (!res) return false;
                res = IntersectCircleWithTree(node.Right, center, radius, obstaclesToIgnore, touchedObstacles, ref minimalDistance);
                if (!res) return false;
            }
            else {
                Polyline obstacle = node.UserData;
                if (obstaclesToIgnore.Contains(obstacle)) return true;

                PointLocation pl = Curve.PointRelativeToCurveLocation(center, obstacle);
                if (pl != PointLocation.Outside) return false;

                Point touchPoint = obstacle[obstacle.ClosestParameter(center)];
                double dist = (touchPoint - center).Length;
                if (dist <= radius)
                    touchedObstacles.Add(new Tuple<Polyline, Point>(obstacle, touchPoint));
                minimalDistance = Math.Min(dist, minimalDistance);
            }
            return true;
        }

        #endregion

        /// <summary>
        /// faster way to check segment-polyline intersection
        /// NOTE: polyline points should be oriented clockwise
        /// </summary>
        internal static bool LineSegmentIntersectPolyline(Point start, Point end, Polyline poly) {
            Point segDirection = end - start;   // the segment direction vector
            Debug.Assert(segDirection.Length > ApproximateComparer.DistanceEpsilon);

            double tStart = 0;                  // the maximum entering segment parameter
            double tEnd = 1;                    // the minimum leaving segment parameter

            foreach (var p in poly.PolylinePoints) {
                //process one edge
                var prev = p.PrevOnPolyline;
                Point e = prev.Point - p.Point;
                double num = Point.CrossProduct(e, start - p.Point);
                double den = -Point.CrossProduct(e, segDirection);
                if (Math.Abs(den) < ApproximateComparer.Tolerance) { // segment is nearly parallel to this edge
                    if (num < 0) return false;
                    continue;
                }

                //intersection parameter
                double t = num / den;
                if (den < 0) {
                    // segment is entering across this edge
                    tStart = Math.Max(tStart, t);
                    // segment enters after leaving polygon
                    if (tStart > tEnd) return false;
                }
                else {
                    // segment is leaving across this edge
                    tEnd = Math.Min(tEnd, t);
                    // segment leaves before entering polygon
                    if (tStart > tEnd) return false;
                }
            }

            return true;
        }


        static internal Polyline Create4gon(Point apex, Point baseCenter, double width1, double width2) {
            var norm = (baseCenter - apex).Normalize();
            norm = new Point(norm.Y, -norm.X);
            return new Polyline(apex + norm * width1 / 2, apex - norm * width1 / 2, baseCenter - norm * width2 / 2, baseCenter + norm * width2 / 2) { Closed = true };
        }


       
#if TEST_MSAGL
        /// <summary>
        /// check the validness of the drawing:
        /// 1. hubs are not inside loose obstacles
        /// 2. bundles do not cross loose obstacles
        /// </summary>
        internal bool HubPositionsAreOK() {
            //check polylines
            foreach (var line in metroGraphData.Metrolines) {
                var poly = line.Polyline;
                foreach (var p in poly.PolylinePoints)
                    Debug.Assert(metroGraphData.PointToStations.ContainsKey(p.Point));
            }

            foreach (var station in metroGraphData.Stations) {
                
                if (!station.IsRealNode && !HubAvoidsObstacles(station.Position, 0, obstaclesToIgnore(station))) {
                    if (LayoutAlgorithmSettings.ShowDebugCurvesEnumeration != null) {
                        HubDebugger.ShowHubs(metroGraphData, bundlingSettings, station);
                        ShowStationWithObstaclesToIgnore(station,  obstacleTree.AllHitItems(station.Position));
                    }
                    return false;
                }
                //bundles
                foreach (var adj in station.Neighbors) {
                    if (ApproximateComparer.Close(adj.Position, station.Position))
                        return false;

                    if (!EdgeIsLegal(station, adj, station.Position, adj.Position)) {
                        if (LayoutAlgorithmSettings.ShowDebugCurvesEnumeration != null) {
                            //debug visualization 
                            var l = new List<DebugCurve>();
                            //foreach (var st in metroGraphData.Stations) {
                            //    l.Add(new DebugCurve(100, 0.5, "grey", st.BoundaryCurve));
                            //}
                            foreach (var poly in obstaclesToIgnore(station)) {
                                l.Add(new DebugCurve(100, 5, "green", poly));
                            }

                            foreach (var obstacle in obstacleTree.GetAllLeaves()) {
                                l.Add(new DebugCurve(100, 1, "red", obstacle));
                            }

                            l.Add(new DebugCurve(1, "blue", station.BoundaryCurve));
                            l.Add(new DebugCurve(1, "blue", adj.BoundaryCurve));

                            l.Add(new DebugCurve(1, "blue", new LineSegment(adj.Position, adj.Neighbors.First().Position)));
                            l.Add(new DebugCurve(1, "blue", new LineSegment(station.Position, adj.Position)));

                            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
                            //end debug visualization
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        void ShowStationWithObstaclesToIgnore(Station station, IEnumerable<Polyline> allHitItems) {
            var l = new List<DebugCurve>();
            foreach (var poly in allHitItems) {
                l.Add(new DebugCurve(100, 0.5, "brown", poly));
            }
            if (obstaclesToIgnore(station) != null)
                foreach (var poly in obstaclesToIgnore(station))
                    l.Add(new DebugCurve(100, 1, "red", poly));


            foreach (var obstacle in obstacleTree.GetAllLeaves())
                l.Add(new DebugCurve(50, 0.1, "green", obstacle));

            l.Add(new DebugCurve(0.1, "blue", new Ellipse(1, 1, station.Position)));

            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        }

        /// <summary>
        /// edge doesn't cross obstacles
        /// NOTE: use method in CdtIntersection insetad!
        /// </summary>
        bool EdgeIsLegal(Station stationA, Station stationB, Point a, Point b) {
            var crossings = InteractiveEdgeRouter.IntersectionsOfLineAndRectangleNodeOverPolyline(new LineSegment(a, b), obstacleTree);
            Set<Polyline> obstaclesToIgnoreForBundle = ObstaclesToIgnoreForBundle(stationA, stationB);
            if (crossings.Count < 0) {
                var l = new List<DebugCurve>();
                var crossingSet = new Set<ICurve>(crossings.Select(ii => ii.Segment1));
                l.AddRange(crossingSet.Select(p=>new DebugCurve(100,1,"red",p)));
                l.AddRange(obstaclesToIgnoreForBundle.Select(p=>new DebugCurve(100,0.5,"green",p)));
                LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
            }
            return crossings.All(intersectionInfo => obstaclesToIgnoreForBundle.Contains((Polyline)intersectionInfo.Segment1));
        }
#endif
    }
}
