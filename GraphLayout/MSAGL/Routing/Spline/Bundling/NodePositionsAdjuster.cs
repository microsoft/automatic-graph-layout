using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Microsoft.Msagl.DebugHelpers;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// Adjust current bundle-routing with a number of heuristic
    /// </summary>
    public class NodePositionsAdjuster {
        /// <summary>
        /// Algorithm settings
        /// </summary>
        readonly BundlingSettings bundlingSettings;

        /// <summary>
        /// bundle data
        /// </summary>
        readonly MetroGraphData metroGraphData;

        NodePositionsAdjuster(MetroGraphData metroGraphData, BundlingSettings bundlingSettings) {
            this.metroGraphData = metroGraphData;
            this.bundlingSettings = bundlingSettings;
        }


        /// <summary>
        /// apply a number of heuristics to improve current routing
        /// </summary>
        internal static void FixRouting(MetroGraphData metroGraphData, BundlingSettings bundlingSettings) {
#if TEST_MSAGL
            Debug.Assert(metroGraphData.looseIntersections.HubPositionsAreOK());
#endif
            //TimeMeasurer.DebugOutput("Initial cost = " + CostCalculator.Cost(metroGraphData, bundlingSettings));
            //TimeMeasurer.DebugOutput("Initial cost of forces: " + CostCalculator.CostOfForces(metroGraphData, bundlingSettings));

            var adjuster = new NodePositionsAdjuster(metroGraphData, bundlingSettings);
            adjuster.GlueConflictingNodes();
            adjuster.UnglueEdgesFromBundleToSaveInk(true);

            var step = 0;
            int MaxSteps = 10;
            while (++step < MaxSteps) {
/*#if TEST_MSAGL
                Debug.Assert(metroGraphData.looseIntersections.HubPositionsAreOK());
#endif*/
                //heuristics to improve routing
                
                bool progress = adjuster.GlueConflictingNodes();

                progress |= adjuster.RelaxConstrainedEdges();

                progress |= (step <= 3 && adjuster.UnglueEdgesFromBundleToSaveInk(false));


                progress |= adjuster.GlueCollinearNeighbors(step);

                progress |= (step == 3 && adjuster.RemoveDoublePathCrossings());

                if (!progress) break;
            }

            //one SA has to be executed with bundle forces
            metroGraphData.cdtIntersections.ComputeForcesForBundles = true;
            adjuster.RemoveDoublePathCrossings();
            adjuster.UnglueEdgesFromBundleToSaveInk(true);
            while (adjuster.GlueConflictingNodes()) { }
            metroGraphData.Initialize(true); //this time initialize the tight enterables also

//            HubDebugger.ShowHubs(metroGraphData, bundlingSettings);
            //TimeMeasurer.DebugOutput("NodePositionsAdjuster stopped after " + step + " steps");
            //HubDebugger.ShowHubs(metroGraphData, bundlingSettings, true);

            //TimeMeasurer.DebugOutput("Final cost: " + CostCalculator.Cost(metroGraphData, bundlingSettings));
            //TimeMeasurer.DebugOutput("Final cost of forces: " + CostCalculator.CostOfForces(metroGraphData, bundlingSettings));
        }

        #region Gluing coinciding nodes

        /// <summary>
        /// unite the nodes that are close to each other
        /// </summary>
        bool GlueConflictingNodes() {

            var circlesHierarchy = GetCirclesHierarchy();
            if (circlesHierarchy == null) return false;
            var gluingMap = new Dictionary<Station, Station>();
            var gluedDomain = new Set<Station>();
            RectangleNodeUtils.CrossRectangleNodes<Station, Point>(circlesHierarchy, circlesHierarchy,
                                                            (i, j) => TryToGlueNodes(i, j, gluingMap, gluedDomain));
            if (gluingMap.Count == 0)
                return false;

            for (int i = 0; i < metroGraphData.Edges.Length; i++)
                RegenerateEdge(gluingMap, i);

            //can it be more efficient?
            HashSet<Point> affectedPoints = new HashSet<Point>();
            foreach (var s in gluedDomain) {
                affectedPoints.Add(s.Position);
                foreach (var neig in s.Neighbors)
                    if (!neig.IsRealNode)
                        affectedPoints.Add(neig.Position);
            }

            //TimeMeasurer.DebugOutput("gluing nodes");
            metroGraphData.Initialize(false);

            SimulatedAnnealing.FixRouting(metroGraphData, bundlingSettings, affectedPoints);
            return true;
        }

        RectangleNode<Station, Point> GetCirclesHierarchy() {
            foreach (var v in metroGraphData.VirtualNodes()) {
                v.Radius = GetCurrentHubRadius(v);
            }

            return RectangleNode<Station, Point>.CreateRectangleNodeOnEnumeration(from i in metroGraphData.VirtualNodes()
                                                                           let p = i.Position
                                                                           let r = Math.Max(i.Radius, 5)
                                                                           let del = new Point(r, r)
                                                                           let b = new Rectangle(p + del, p - del)
                                                                           select new RectangleNode<Station, Point>(i, b));
        }

        double GetCurrentHubRadius(Station node) {
            if (node.IsRealNode) {
                return node.BoundaryCurve.BoundingBox.Diagonal / 2;
            } else {
                double idealR = node.cachedIdealRadius = HubRadiiCalculator.CalculateIdealHubRadiusWithNeighbors(this.metroGraphData, this.bundlingSettings, node);
                double r = metroGraphData.looseIntersections.GetMinimalDistanceToObstacles(node, node.Position, idealR);
                Debug.Assert(r <= idealR);
                foreach (var adj in node.Neighbors)
                    r = Math.Min(r, (node.Position - adj.Position).Length);
                return r;
            }
        }

        void TryToGlueNodes(Station i, Station j, Dictionary<Station, Station> gluingMap, Set<Station> gluedDomain) {
            Debug.Assert(i != j);
            double d = (i.Position - j.Position).Length;
            double r1 = Math.Max(i.Radius, 5);
            double r2 = Math.Max(j.Radius, 5);
            if (d >= r1 + r2)
                return;
            //we are greedily trying to glue i to j
            if (!TryGlueOrdered(i, j, gluedDomain, gluingMap))
                TryGlueOrdered(j, i, gluedDomain, gluingMap);
        }

        bool TryGlueOrdered(Station i, Station j, Set<Station> gluedDomain, Dictionary<Station, Station> gluingMap) {
            if (!gluingMap.ContainsKey(i) && !gluedDomain.Contains(i) && NodeGluingIsAllowed(i, j, gluingMap)) {
                Map(i, j, gluedDomain, gluingMap);
                //TimeMeasurer.DebugOutput("gluing nodes " + i.serialNumber + " and " + j.serialNumber);
                return true;
            }
            return false;
        }

        void Map(Station i, Station j, Set<Station> gluedDomain, Dictionary<Station, Station> gluingMap) {
            gluingMap[i] = j;
            gluedDomain.Insert(j);
        }

        /// <summary>
        /// trying to glue i to j
        /// </summary>
        bool NodeGluingIsAllowed(Station i, Station j, Dictionary<Station, Station> gluingMap) {
            foreach (var adj in i.Neighbors) {
                var k = Glued(adj, gluingMap);
                //1. check that we can merge these stations (== no intersections)
                Set<Polyline> obstaclesToIgnore = metroGraphData.looseIntersections.ObstaclesToIgnoreForBundle(k, i);
                if (!metroGraphData.cdtIntersections.EdgeIsLegal(k, j, k.Position, j.Position, obstaclesToIgnore))
                    return false;
            }

            //2. check that cost of the routing is reduced
            double delta = ComputeCostDeltaAfterNodeGluing(i, j, gluingMap);
            if (delta < 0) return false;

            return true;
        }

        double ComputeCostDeltaAfterNodeGluing(Station i, Station j, Dictionary<Station, Station> gluingMap) {
            double d = (i.Position - j.Position).Length;
            if (i.Radius >= d || j.Radius >= d) return 1.0;

            double gain = 0;

            //ink
            double oldInk = metroGraphData.Ink;
            double newInk = metroGraphData.Ink - (j.Position - i.Position).Length;
            foreach (var adj in i.Neighbors) {
                var k = Glued(adj, gluingMap);
                newInk -= (k.Position - i.Position).Length;
                newInk += (metroGraphData.RealEdgeCount(k, j) == 0 ? (k.Position - j.Position).Length : 0);
            }

            gain += CostCalculator.InkError(oldInk, newInk, bundlingSettings);

            //path lengths
            foreach (var metroInfo in metroGraphData.MetroNodeInfosOfNode(i)) {
                double oldLength = metroInfo.Metroline.Length;
                double newLength = metroInfo.Metroline.Length;

                PolylinePoint pi = metroInfo.PolyPoint;
                PolylinePoint pa = pi.Prev;
                PolylinePoint pb = pi.Next;

                newLength -= (pa.Point - i.Position).Length + (pb.Point - i.Position).Length;
                newLength += (pa.Point - j.Position).Length + (pb.Point - j.Position).Length;

                gain += CostCalculator.PathLengthsError(oldLength, newLength, metroInfo.Metroline.IdealLength,
                                                        bundlingSettings);
            }

            return gain;
        }

        void RegenerateEdge(Dictionary<Station, Station> gluingMap, int edgeIndex) {
            var poly = metroGraphData.Metrolines[edgeIndex].Polyline;
            if (!poly.Any(p => gluingMap.ContainsKey(metroGraphData.PointToStations[p])))
                return;

            metroGraphData.Edges[edgeIndex].Curve =
                new Polyline(GluedPolyline(poly.Select(p => metroGraphData.PointToStations[p]).ToArray(), gluingMap));
        }

        static IEnumerable<Point> GluedPolyline(Station[] metroline, Dictionary<Station, Station> gluedMap) {
            int i;
            var ret = new Stack<Station>();
            ret.Push(metroline[0]);
            var seenStations = new Set<Station>();
            for (i = 1; i < metroline.Length - 1; i++) {
                var station = Glued(metroline[i], gluedMap);
                if (seenStations.Contains(station)) {
                    //we made a cycle - need to cut it out
                    while (ret.Peek() != station)
                        seenStations.Delete(ret.Pop());
                    continue;
                }
                if (ApproximateComparer.Close(station.Position, ret.Peek().Position))
                    continue;
                seenStations.Insert(station);
                ret.Push(station);
            }
            ret.Push(metroline[i]);
            return ret.Reverse().Select(n => n.Position);
        }

        static Station Glued(Station i, Dictionary<Station, Station> gluedMap) {
            Station j;
            return gluedMap.TryGetValue(i, out j) ? j : i;
        }

        #endregion

        #region Shortcut single polylines

        double ink;
        Dictionary<Metroline, double> polylineLength;

        /// <summary>
        /// Unbundle unnecessary edges:
        ///  instead of one bundle (a->bcd) we get two bundles (a->b,a->cd) with smaller ink
        /// </summary>
        bool UnglueEdgesFromBundleToSaveInk(bool alwaysExecuteSA) {
            var segsToPolylines = new Dictionary<PointPair, Set<Metroline>>();
            ink = metroGraphData.Ink;
            polylineLength = new Dictionary<Metroline, double>();
            //create polylines
            foreach (var metroline in metroGraphData.Metrolines) {
                polylineLength[metroline] = metroline.Length;
                for (var pp = metroline.Polyline.StartPoint; pp.Next != null; pp = pp.Next) {
                    var segment = new PointPair(pp.Point, pp.Next.Point);
                    CollectionUtilities.AddToMap(segsToPolylines, segment, metroline);
                }
            }
            var affectedPoints = new HashSet<Point>();
            var progress = false;
            foreach (var metroline in metroGraphData.Metrolines) {
                var obstaclesAllowedToIntersect =
                    metroGraphData.PointToStations[metroline.Polyline.Start].EnterableLoosePolylines*
                    metroGraphData.PointToStations[metroline.Polyline.End].EnterableLoosePolylines;
                if (TrySeparateOnPolyline(metroline, segsToPolylines, affectedPoints, obstaclesAllowedToIntersect))
                    progress = true;
            }

            if (progress) //TimeMeasurer.DebugOutput("unbundling");
                metroGraphData.Initialize(false);

            if (alwaysExecuteSA || progress)
                SimulatedAnnealing.FixRouting(metroGraphData, bundlingSettings, alwaysExecuteSA ? null : affectedPoints);

            return progress;
        }

        bool TrySeparateOnPolyline(Metroline metroline, Dictionary<PointPair, Set<Metroline>> segsToPolylines,
                                   HashSet<Point> affectedPoints, Set<Polyline> obstaclesAllowedToIntersect) {
            bool progress = false;
            var relaxing = true;
            while (relaxing) {
                relaxing = false;
                for (var p = metroline.Polyline.StartPoint; p.Next != null && p.Next.Next != null; p = p.Next)
                    if (TryShortcutPolypoint(p, segsToPolylines, affectedPoints, obstaclesAllowedToIntersect))
                        relaxing = true;
                if (relaxing) progress = true;
            }
            return progress;
        }
        
        bool TryShortcutPolypoint(PolylinePoint pp, Dictionary<PointPair, Set<Metroline>> segsToPolylines, HashSet<Point> affectedPoints, Set<Polyline> obstaclesAllowedToIntersect) {            
            if (SeparationShortcutAllowed(pp, segsToPolylines, obstaclesAllowedToIntersect)) {
                affectedPoints.Add(pp.Point);
                affectedPoints.Add(pp.Next.Point);
                affectedPoints.Add(pp.Next.Next.Point);
                RemoveShortcuttedPolypoint(pp, segsToPolylines);
                
                return true;
            }
            return false;
        }

        
        /// <summary>
        /// allowed iff line (a,c) is legal and inkgain > 0
        /// </summary>
        bool SeparationShortcutAllowed(PolylinePoint pp, Dictionary<PointPair, Set<Metroline>> segsToPolylines, Set<Polyline> obstaclesAllowedToIntersect) {
            var a = pp.Point;
            var b = pp.Next.Point;
            var c = pp.Next.Next.Point;
            var aStation = metroGraphData.PointToStations[a];
            var bStation = metroGraphData.PointToStations[b];
            var cStation = metroGraphData.PointToStations[c];
            //1. intersections
            if (!metroGraphData.cdtIntersections.EdgeIsLegal(aStation, cStation,
                                                             a, c,
                                                             obstaclesAllowedToIntersect*
                                                             bStation.EnterableLoosePolylines*
                                                             (aStation.EnterableLoosePolylines +
                                                              cStation.EnterableLoosePolylines)))
                return false;

            //2. cost gain
            var inkgain = GetInkgain(pp, segsToPolylines, a, b, c);
            if (inkgain < 0) return false;

            return true;
        }

        double GetInkgain(PolylinePoint pp, Dictionary<PointPair, Set<Metroline>> segsToPolylines, Point a, Point b, Point c) {
            Set<Metroline> abPolylines, bcPolylines, abcPolylines;
            FindPolylines(pp, segsToPolylines, out abPolylines, out bcPolylines, out abcPolylines);
            double gain = 0;
            //ink
            double oldInk = ink;
            double newInk = ink;
            double ab = (a - b).Length;
            double bc = (b - c).Length;
            double ac = (a - c).Length;
            if (abPolylines.Count == abcPolylines.Count) newInk -= ab;
            if (bcPolylines.Count == abcPolylines.Count) newInk -= bc;
            if (!segsToPolylines.ContainsKey(new PointPair(a, c)) || segsToPolylines[new PointPair(a, c)].Count == 0)
                newInk += ac;
            gain += CostCalculator.InkError(oldInk, newInk, bundlingSettings);

            //path lengths
            foreach (var metroline in abcPolylines) {
                double oldLength = polylineLength[metroline];
                double newLength = polylineLength[metroline];
                newLength -= ab + bc - ac;

                gain += CostCalculator.PathLengthsError(oldLength, newLength, metroline.IdealLength, bundlingSettings);
            }

            //radii
            double nowR = GetCurrentHubRadius(metroGraphData.PointToStations[a]);
            double widthABC = metroGraphData.GetWidth(abcPolylines, bundlingSettings.EdgeSeparation);
            double widthABD = metroGraphData.GetWidth(abPolylines - abcPolylines, bundlingSettings.EdgeSeparation);
            double idealR = HubRadiiCalculator.GetMinRadiusForTwoAdjacentBundles(nowR, a, c, b, widthABC, widthABD, metroGraphData, bundlingSettings);
            if (idealR > nowR) {
                gain -= CostCalculator.RError(idealR, nowR, bundlingSettings);
            }

            //check opposite side
            nowR = GetCurrentHubRadius(metroGraphData.PointToStations[c]);
            double widthCBD = metroGraphData.GetWidth(bcPolylines - abcPolylines, bundlingSettings.EdgeSeparation);
            idealR = HubRadiiCalculator.GetMinRadiusForTwoAdjacentBundles(nowR, c, b, a, widthCBD, widthABC, metroGraphData, bundlingSettings);
            if (idealR > nowR) {
                gain -= CostCalculator.RError(idealR, nowR, bundlingSettings);
            }

            return gain;
        }

        void RemoveShortcuttedPolypoint(PolylinePoint pp, Dictionary<PointPair, Set<Metroline>> segsToPolylines) {
            var a = pp.Point;
            var b = pp.Next.Point;
            var c = pp.Next.Next.Point;

            Set<Metroline> abPolylines, bcPolylines, abcPolylines;
            FindPolylines(pp, segsToPolylines, out abPolylines, out bcPolylines, out abcPolylines);

            double ab = (a - b).Length;
            double bc = (b - c).Length;
            double ac = (a - c).Length;

            //fixing ink
            if (abPolylines.Count == abcPolylines.Count) ink -= ab;
            if (bcPolylines.Count == abcPolylines.Count) ink -= bc;
            if (!segsToPolylines.ContainsKey(new PointPair(a, c)) || segsToPolylines[new PointPair(a, c)].Count == 0)
                ink += ac;

            //fixing edge lengths
            foreach (var metroline in abcPolylines)
                polylineLength[metroline] -= ab + bc - ac;

            //fixing polylines
            foreach (var metroline in abcPolylines) {
                RemovePolypoint(metroline.Polyline.PolylinePoints.First(p => p.Point == b));
                CollectionUtilities.RemoveFromMap(segsToPolylines, new PointPair(a, b), metroline);
                CollectionUtilities.RemoveFromMap(segsToPolylines, new PointPair(b, c), metroline);
                CollectionUtilities.AddToMap(segsToPolylines, new PointPair(a, c), metroline);
            }
        }

        void FindPolylines(PolylinePoint pp, Dictionary<PointPair, Set<Metroline>> segsToPolylines,
                out Set<Metroline> abPolylines, out Set<Metroline> bcPolylines, out Set<Metroline> abcPolylines) {
            Point a = pp.Point;
            Point b = pp.Next.Point;
            Point c = pp.Next.Next.Point;
            abPolylines = segsToPolylines[new PointPair(a, b)];
            bcPolylines = segsToPolylines[new PointPair(b, c)];
            abcPolylines = abPolylines * bcPolylines;
        }

        void RemovePolypoint(PolylinePoint p) {
            PolylinePoint prev = p.Prev;
            PolylinePoint next = p.Next;
            prev.Next = next;
            next.Prev = prev;
        }

        #endregion

        #region Fix collinear neighbors

        /// <summary>
        /// Fix the situation where a station has two neighbors that are almost in the same directions
        /// </summary>
        bool GlueCollinearNeighbors(int step) {
            HashSet<Point> affectedPoints = new HashSet<Point>();
            bool progress = false;
            foreach (var node in metroGraphData.Stations)
                if (GlueCollinearNeighbors(node, affectedPoints, step)) progress = true;

            if (progress) {
                //TimeMeasurer.DebugOutput("gluing edges");
                metroGraphData.Initialize(false);
                SimulatedAnnealing.FixRouting(metroGraphData, bundlingSettings, affectedPoints);
            }

            return progress;
        }

        bool GlueCollinearNeighbors(Station node, HashSet<Point> affectedPoints, int step) {
            if (node.Neighbors.Length <= 1) return false;

            //node,adj => new via point
            Dictionary<Tuple<Station, Station>, Point> gluedEdges = new Dictionary<Tuple<Station, Station>, Point>();
            var neighbors = node.Neighbors;
            for (int i = 0; i < neighbors.Length; i++)
                TryToGlueEdges(node, neighbors[i], neighbors[(i + 1) % neighbors.Length], gluedEdges, step);

            if (gluedEdges.Count == 0)
                return false;

            foreach (var keyValuePair in gluedEdges) {
                GlueEdge(keyValuePair);
                affectedPoints.Add(keyValuePair.Key.Item1.Position);
                affectedPoints.Add(keyValuePair.Key.Item2.Position);
                affectedPoints.Add(keyValuePair.Value);
            }

            return true;
        }

        void TryToGlueEdges(Station node, Station a, Station b, Dictionary<Tuple<Station, Station>, Point> gluedEdges, int step) {
            Debug.Assert(a != b);
            var angle = Point.Angle(a.Position, node.Position, b.Position);
            if (angle < bundlingSettings.AngleThreshold) {
                var la = (a.Position - node.Position).Length;
                var lb = (b.Position - node.Position).Length;
                double ratio = Math.Min(la, lb) / Math.Max(la, lb);
                if (ratio < 0.05) return;

                if (la < lb) {
                    if (EdgeGluingIsAllowed(node, a, b)) {
                        AddEdgeToGlue(node, b, a, a.Position, gluedEdges);
                        return;
                    }
                }
                else {
                    if (EdgeGluingIsAllowed(node, b, a)) {
                        AddEdgeToGlue(node, a, b, b.Position, gluedEdges);
                        return;
                    }
                }

                //TODO: need this???
                if (step < 5 && ratio > 0.5) {
                    Point newPosition = ConstructGluingPoint(node, a, b);
                    if (EdgeGluingIsAllowed(node, a, b, newPosition)) {
                        AddEdgeToGlue(node, b, a, newPosition, gluedEdges);
                    }
                }
            }
        }

        Point ConstructGluingPoint(Station node, Station a, Station b) {
            //temp
            double len = Math.Min((a.Position - node.Position).Length, (b.Position - node.Position).Length) / 2;
            Point dir = (a.Position - node.Position).Normalize() + (b.Position - node.Position).Normalize();
            return node.Position + dir * len / 2;
        }


        bool EdgeGluingIsAllowed(Station node, Station a, Station b) {
            //0. can't pass through real nodes
            if (a.IsRealNode || b.IsRealNode) return false;

            //1. check intersections)  Here we are bending the edge (node->b) to pass through a.Position.
            //We need to be sure that segments (node,a) and (a,b) intersect only obstacles enterable for the bundle (node, b)
            if (!metroGraphData.cdtIntersections.EdgeIsLegal(a, b, a.Position, b.Position)) return false;

            var enterableForEdgeNodeB = metroGraphData.looseIntersections.ObstaclesToIgnoreForBundle(node, b);

            var crossingsOfEdgeNodeA = InteractiveEdgeRouter.IntersectionsOfLineAndRectangleNodeOverPolyline(new LineSegment(node.Position, a.Position), metroGraphData.LooseTree);
            if (crossingsOfEdgeNodeA.Exists(ii => !enterableForEdgeNodeB.Contains(ii.Segment1)))
                return false;

            var crossingsOfEdgeab = InteractiveEdgeRouter.IntersectionsOfLineAndRectangleNodeOverPolyline(new LineSegment(a.Position, b.Position), metroGraphData.LooseTree);
            if (crossingsOfEdgeab.Exists(ii => !enterableForEdgeNodeB.Contains(ii.Segment1)))
                return false;

            //2. check cost
            double delta = ComputeCostDeltaAfterEdgeGluing(node, a, b, a.Position);
            if (delta < 0) return false;

            return true;
        }

        bool EdgeGluingIsAllowed(Station node, Station a, Station b, Point gluingPoint) {
            //0. can't pass through real nodes
            if (!metroGraphData.looseIntersections.HubAvoidsObstacles(gluingPoint, 0, a.EnterableLoosePolylines * b.EnterableLoosePolylines)) return false;

            //1. check intersections
            if (!metroGraphData.cdtIntersections.EdgeIsLegal(node, null, node.Position, gluingPoint)) return false;
            if (!metroGraphData.cdtIntersections.EdgeIsLegal(a, null, a.Position, gluingPoint)) return false;
            if (!metroGraphData.cdtIntersections.EdgeIsLegal(b, null, b.Position, gluingPoint)) return false;

            //2. check cost
            double delta = ComputeCostDeltaAfterEdgeGluing(node, a, b, gluingPoint);
            if (delta < 0) return false;

            return true;
        }

        double ComputeCostDeltaAfterEdgeGluing(Station node, Station a, Station b, Point newp) {
            double gain = 0;

            //ink
            double oldInk = metroGraphData.Ink;
            double newInk = metroGraphData.Ink - (node.Position - b.Position).Length - (node.Position - a.Position).Length +
                (node.Position - newp).Length + (newp - a.Position).Length + (newp - b.Position).Length;
            gain += CostCalculator.InkError(oldInk, newInk, bundlingSettings);

            //path lengths
            foreach (var metroline in metroGraphData.GetIjInfo(node, b).Metrolines) {
                double oldLength = metroline.Length;
                double newLength = metroline.Length - (node.Position - b.Position).Length +
                                   (node.Position - newp).Length + (newp - b.Position).Length;
                gain += CostCalculator.PathLengthsError(oldLength, newLength, metroline.IdealLength, bundlingSettings);
            }
            foreach (var metroline in metroGraphData.GetIjInfo(node, a).Metrolines) {
                double oldLength = metroline.Length;
                double newLength = metroline.Length - (node.Position - a.Position).Length +
                                   (node.Position - newp).Length + (newp - a.Position).Length;
                gain += CostCalculator.PathLengthsError(oldLength, newLength, metroline.IdealLength, bundlingSettings);
            }

            //also compute radii gain
            //double nowR = Math.Min(GetCurrentHubRadius(node), (node.Position - newp).Length);
            //double id2 = HubRadiiCalculator.CalculateIdealHubRadiusWithNeighbors(metroGraphData, bundlingSettings, node);
            double id2 = node.cachedIdealRadius;
            double nowR = GetCurrentHubRadius(node);
            double idealR = HubRadiiCalculator.GetMinRadiusForTwoAdjacentBundles(nowR, node, node.Position, a, b, metroGraphData, bundlingSettings);

            if (idealR > nowR) {
                gain += CostCalculator.RError(idealR, nowR, bundlingSettings);
            }

            if (id2 > (node.Position - newp).Length && !node.IsRealNode) {
                gain -= CostCalculator.RError(id2, (node.Position - newp).Length, bundlingSettings);
            }

            return gain;
        }

        void AddEdgeToGlue(Station node, Station b, Station a, Point newp, Dictionary<Tuple<Station, Station>, Point> gluedEdges) {
            //same edge in the reverse direction      
            if (gluedEdges.ContainsKey(new Tuple<Station, Station>(a, node))) return;
            if (gluedEdges.ContainsKey(new Tuple<Station, Station>(b, node))) return;
            if (gluedEdges.ContainsKey(new Tuple<Station, Station>(node, a))) return;
            if (gluedEdges.ContainsKey(new Tuple<Station, Station>(node, b))) return;

            gluedEdges[new Tuple<Station, Station>(node, a)] = newp;
            gluedEdges[new Tuple<Station, Station>(node, b)] = newp;           
        }

        void GlueEdge(KeyValuePair<Tuple<Station, Station>, Point> keyValuePair) {
            var node = keyValuePair.Key.Item1;
            var a = keyValuePair.Key.Item2;
            var newp = keyValuePair.Value;

            foreach (var polylinePoint in node.MetroNodeInfos.Select(i => i.PolyPoint)) {
                if (polylinePoint.Next != null && polylinePoint.Next.Point == a.Position)
                    SplitPolylinePoint(polylinePoint, newp);
                else if (polylinePoint.Prev != null && polylinePoint.Prev.Point == a.Position)
                    SplitPolylinePoint(polylinePoint.Prev, newp);
            }
        }

        void SplitPolylinePoint(PolylinePoint node, Point pointToInsert) {
            if (node.Point == pointToInsert || node.Next.Point == pointToInsert) return;

            var p = new PolylinePoint(pointToInsert) { Polyline = node.Polyline, Next = node.Next, Prev = node };
            p.Next.Prev = p;
            p.Prev.Next = p;
        }

        #endregion

        #region Split edges that are constrained by the obstacles

        /// <summary>
        /// split each edge that is too much constrained by the obstacles
        /// </summary>
        bool RelaxConstrainedEdges() {
            HashSet<Point> affectedPoints = new HashSet<Point>();
            bool progress = false;
            foreach (var edge in metroGraphData.VirtualEdges())
                if (RelaxConstrainedEdge(edge.Item1, edge.Item2, affectedPoints)) progress = true;

            if (progress) {
                //TimeMeasurer.DebugOutput("relaxing constrained edges");
                metroGraphData.Initialize(false);

                SimulatedAnnealing.FixRouting(metroGraphData, bundlingSettings, affectedPoints);
            }

            return progress;
        }

        bool RelaxConstrainedEdge(Station a, Station b, HashSet<Point> affectedPoints) {
            //find conflicting obstacles
            double idealWidth = metroGraphData.GetWidth(a, b, bundlingSettings.EdgeSeparation);
            List<Tuple<Point, Point>> closestPoints;
            /*bool res =*/ metroGraphData.cdtIntersections.BundleAvoidsObstacles(a, b, a.Position, b.Position, 0.99 * idealWidth / 2.0, out closestPoints); 
           // Debug.Assert(res); //todo still unsolved

            if (closestPoints.Count > 0) {
                //find closest obstacle
                double bestDist = -1;
                Point bestPoint = new Point();
                foreach (var d in closestPoints) {
                    //should not be too close
                    double distToSegmentEnd = Math.Min((a.Position - d.Item2).Length, (b.Position - d.Item2).Length);
                    double distAB = (a.Position - b.Position).Length;
                    double ratio = distToSegmentEnd / distAB;
                    if (ratio < 0.1) continue;

                    //choose the closest
                    double dist = (d.Item1 - d.Item2).Length;
                    if (bestDist == -1 || dist < bestDist) {
                        bestDist = dist;
                        bestPoint = d.Item2;
                    }
                }
                if (bestDist == -1) return false;

                if (!metroGraphData.looseIntersections.HubAvoidsObstacles(bestPoint, 0, a.EnterableLoosePolylines * b.EnterableLoosePolylines))
                    return false;

                affectedPoints.Add(bestPoint);
                affectedPoints.Add(a.Position);
                affectedPoints.Add(b.Position);

                foreach (var metroline in metroGraphData.GetIjInfo(a, b).Metrolines) {
                    PolylinePoint pp = null;
                    //TODO: replace the cycle!
                    foreach (var ppp in metroline.Polyline.PolylinePoints)
                        if (ppp.Point == a.Position) {
                            pp = ppp;
                            break;
                        }

                    Debug.Assert(pp != null);
                    if (pp.Next != null && pp.Next.Point == b.Position)
                        SplitPolylinePoint(pp, bestPoint);
                    else
                        SplitPolylinePoint(pp.Prev, bestPoint);
                }

                return true;
            }

            return false;
        }

        #endregion

        #region Switch flips to reduce path crossings

        /// <summary>
        /// switch flips
        /// </summary>
        bool RemoveDoublePathCrossings() {
            bool progress = new PathFixer(metroGraphData, metroGraphData.PointIsAcceptableForEdge).Run();

            if (progress) {
                metroGraphData.Initialize(false);
                SimulatedAnnealing.FixRouting(metroGraphData, bundlingSettings);
            }

            return progress;
        }

        #endregion

    }
}
