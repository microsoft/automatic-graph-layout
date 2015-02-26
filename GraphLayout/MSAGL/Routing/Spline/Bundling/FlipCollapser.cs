/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
#if DEBUG && TEST_MSAGL
using Microsoft.Msagl.DebugHelpers;
#endif
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    internal class FlipCollapser {
        readonly MetroGraphData metroGraphData;
        readonly BundlingSettings bundlingSettings;
        readonly Cdt cdt;

        //these weights are used to break the ties in the cycles, (Alexander Holroyd's idea)
        readonly Dictionary<PointPair, double> randomWeights = new Dictionary<PointPair, double>();
        readonly Dictionary<Polyline, EdgeGeometry> polylineToEdgeGeom = new Dictionary<Polyline, EdgeGeometry>();
        readonly Dictionary<Point, Set<PolylinePoint>> pathsThroughPoints = new Dictionary<Point, Set<PolylinePoint>>();
        readonly Random random = new Random(1);
        readonly Set<Point> interestingPoints = new Set<Point>();

        IEnumerable<Polyline> Polylines {
            get { return polylineToEdgeGeom.Keys; }
        }

        RectangleNode<CdtSite> siteHierarchy;
        RectangleNode<CdtTriangle> triangleHierarchy;

        internal FlipCollapser(MetroGraphData metroGraphData, BundlingSettings bundlingSettings, Cdt cdt) {
            this.metroGraphData = metroGraphData;
            this.bundlingSettings = bundlingSettings;
            this.cdt = cdt;
        }

        internal void Run() {
            Init();
            RemoveFlips();
        }

        void Init() {
            foreach (EdgeGeometry e in metroGraphData.Edges) {
                polylineToEdgeGeom[(Polyline)e.Curve] = e;
            }

            var del = new Point(ApproximateComparer.DistanceEpsilon, ApproximateComparer.DistanceEpsilon);

            if (cdt != null) {
                siteHierarchy = RectangleNode<CdtSite>.CreateRectangleNodeOnEnumeration(
                        cdt.PointsToSites.Values.Select(site => new RectangleNode<CdtSite>(site, new Rectangle(site.Point + del, site.Point - del))));

                triangleHierarchy = RectangleNode<CdtTriangle>.CreateRectangleNodeOnEnumeration(
                        cdt.GetTriangles().Select(tr => new RectangleNode<CdtTriangle>(tr, TriangleRectangle(tr.Sites))));
            }

            CreatePathsThroughPoints();
        }

        void CreatePathsThroughPoints() {
            foreach (Polyline poly in Polylines)
                CreatePathsThroughPointsForPolyline(poly);
        }

        void CreatePathsThroughPointsForPolyline(Polyline poly) {
            foreach (PolylinePoint pp in poly.PolylinePoints) {
                pp.Polyline = poly; //make sure that the polypoints point to their Polylines
                RegisterPolylinePointInPathsThrough(pp);
            }
        }

        void RegisterPolylinePointInPathsThrough(PolylinePoint pp) {
            Set<PolylinePoint> set = GetOrCreatePassingPathsSet(pp);
            set.Insert(pp);
        }

        //TODO
        Set<PolylinePoint> GetOrCreatePassingPathsSet(PolylinePoint pp) {
            Set<PolylinePoint> set;
            if (pathsThroughPoints.TryGetValue(pp.Point, out set))
                return set;

            pathsThroughPoints[pp.Point] = set = new Set<PolylinePoint>();
            return set;
        }

        void RemoveFlips() {
            var queued = new Set<Polyline>(Polylines);
            var queue = new Queue<Polyline>();
            foreach (Polyline e in Polylines)
                queue.Enqueue(e);
            while (queue.Count > 0) {
                Polyline initialPolyline = queue.Dequeue();
                queued.Remove(initialPolyline);
                Polyline changedPolyline = ProcessPolyline(initialPolyline);
                if (changedPolyline != null)
                    if (!queued.Contains(changedPolyline)) {
                        queued.Insert(changedPolyline);
                        queue.Enqueue(changedPolyline);
                    }
            }
        }

        Polyline ProcessPolyline(Polyline polyline) {
            Polyline flipSide = FindFlip(polyline);
            if (flipSide != null) {
                return CollapseFlip(flipSide, polyline);
            }
            return null;
        }

        Polyline FindFlip(Polyline poly) {
            var departed = new Dictionary<Polyline, PolylinePoint>();
            for (PolylinePoint pp = poly.StartPoint.Next; pp != null; pp = pp.Next) {
                FillDepartedOnPrev(departed, pp);

                //look for a returning path
                foreach (PolylinePoint polyPoint in pathsThroughPoints[pp.Point]) {
                    PolylinePoint pointOfDeparture;
                    if (departed.TryGetValue(polyPoint.Polyline, out pointOfDeparture))
                        return pointOfDeparture.Polyline;
                }
            }

            return null;
        }

        void FillDepartedOnPrev(Dictionary<Polyline, PolylinePoint> departed, PolylinePoint pp) {
            Point prevPoint = pp.Prev.Point;
            foreach (PolylinePoint polyPoint in pathsThroughPoints[prevPoint]) {
                if (!IsNeighbor(polyPoint, pp)) {
                    Debug.Assert(!departed.ContainsKey(polyPoint.Polyline));
                    departed[polyPoint.Polyline] = polyPoint;
                }
            }
        }

        bool IsNeighbor(PolylinePoint a, PolylinePoint b) {
            return a.Prev != null && a.Prev.Point == b.Point || a.Next != null && a.Next.Point == b.Point;
        }

        /// <summary>
        /// try to collapse the polylines reducing routing cost
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>
        /// returns the changed polyline
        /// </returns>
        Polyline CollapseFlip(Polyline a, Polyline b) {
            Point aFirst; //the first point of a on b
            Point bFirst; //the first point of b on a
            Point aLast; //the last point of a on b
            Point bLast; //the last point of b on a
            Dictionary<Point, PolylinePoint> aPointMap = GetPointMap(a);
            Dictionary<Point, PolylinePoint> bPointMap = GetPointMap(b);
            FindFirstAndLast(a, out aFirst, out aLast, bPointMap);
            FindFirstAndLast(b, out bFirst, out bLast, aPointMap);

            /*List<DebugCurve> dc = new List<DebugCurve>();
            dc.Add(new DebugCurve("red", a));
            dc.Add(new DebugCurve("blue", b));
            dc.Add(new DebugCurve("red", CurveFactory.CreateCircle(5, aFirst)));
            dc.Add(new DebugCurve("red", CurveFactory.CreateCircle(10, aLast)));
            dc.Add(new DebugCurve("blue", CurveFactory.CreateCircle(5, bFirst)));
            dc.Add(new DebugCurve("blue", CurveFactory.CreateCircle(10, bLast)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dc);*/

            PolylinePoint baFirstP = bPointMap[aFirst];
            PolylinePoint baLastP = bPointMap[aLast];
            bool aFirstBeforeALastOnB = PolylinePointsAreInCorrectOrder(baFirstP, baLastP);
            PolylinePoint abFirstP = aPointMap[bFirst];
            PolylinePoint abLastP = aPointMap[bLast];
            bool bFirstBeforeBLastOnA = PolylinePointsAreInCorrectOrder(abFirstP, abLastP);
            Set<CdtEdge> aChannel = GetCdtEdgesCrossedByPath(a);
            Set<CdtEdge> bChannel = GetCdtEdgesCrossedByPath(b);
            Set<CdtEdge> aCollapsedEdges = GetChannelEdgeOfCollapsedA(a, aPointMap[aFirst], baFirstP, aPointMap[aLast], baLastP, aFirstBeforeALastOnB);
            Set<CdtEdge> bCollapsedEdges = GetChannelEdgeOfCollapsedA(b, bPointMap[bFirst], abFirstP, bPointMap[bLast], abLastP, bFirstBeforeBLastOnA);
            Set<CdtEdge> edgesAbondonedByA = cdt == null ? null : aChannel - aCollapsedEdges;
            Set<CdtEdge> edgesEnteredByA = cdt == null ? null : aCollapsedEdges - aChannel;
            Set<CdtEdge> edgesAbondonedByB = cdt == null ? null : bChannel - bCollapsedEdges;
            Set<CdtEdge> edgesEnteredByB = cdt == null ? null : bCollapsedEdges - bChannel;

            EdgeGeometry aEdgeGeom = polylineToEdgeGeom[a];
            EdgeGeometry bEdgeGeom = polylineToEdgeGeom[b];

            double deltaCostForCollapsingA = CostGrowth(aFirst, aLast, aPointMap, bPointMap, aFirstBeforeALastOnB, aEdgeGeom, edgesAbondonedByA, edgesEnteredByA);
            double deltaCostForCollapsingB = CostGrowth(bFirst, bLast, bPointMap, aPointMap, bFirstBeforeBLastOnA, bEdgeGeom, edgesAbondonedByB, edgesEnteredByB);

            if (deltaCostForCollapsingA >= 0 && deltaCostForCollapsingB >= 0) {
                return null;
                //try to swap them to reduce the number of path crossings
                //return SwapFlips(a, b);
            }

            //end debug
            if (deltaCostForCollapsingA < deltaCostForCollapsingB) {
                Collapse(aFirst, aLast, aPointMap, bPointMap, aFirstBeforeALastOnB, edgesAbondonedByA, edgesEnteredByA);
                return a;
            }
            if (deltaCostForCollapsingB < deltaCostForCollapsingA) {
                Collapse(bFirst, bLast, bPointMap, aPointMap, bFirstBeforeBLastOnA, edgesEnteredByB, edgesEnteredByB);
                return b;
            }

            deltaCostForCollapsingA = DeltaRandomCost(aFirst, aLast, aPointMap, bPointMap, aFirstBeforeALastOnB);
            deltaCostForCollapsingB = DeltaRandomCost(bFirst, bLast, bPointMap, aPointMap, bFirstBeforeBLastOnA);
            if (deltaCostForCollapsingA < deltaCostForCollapsingB) {
                Collapse(aFirst, aLast, aPointMap, bPointMap, aFirstBeforeALastOnB, edgesEnteredByA, edgesEnteredByB);
                return a;
            }
            else {
                Collapse(bFirst, bLast, bPointMap, aPointMap, bFirstBeforeBLastOnA, edgesAbondonedByB, edgesEnteredByB);
                return b;
            }
        }

        Set<CdtEdge> GetCdtEdgesCrossedByPath(Polyline polyline) {
            if (cdt == null) return null;
            var ret = new CdtGeneralPolylineTracer(polyline.PolylinePoints, siteHierarchy, triangleHierarchy).GetCrossedEdges();
            return ret;
        }

        //        void ShowFlip(Polyline a, Polyline b) {
        //            var l = new List<DebugCurve>();
        //
        //            l.AddRange(
        //                Cdt.GetTriangles().Select(
        //                    t => new DebugCurve(100, 1, "green", new Polyline(t.Sites.Select(v => v.Point)) {Closed = true})));
        //            l.AddRange(new[] {
        //                                 new DebugCurve(120, 1, "red", a),
        //                                 new DebugCurve(120, 1, "blue", b)
        //                             });
        //            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        //        }

        IEnumerable<PolylinePoint> CollapsedPolylinePolylinePoints(Polyline a, PolylinePoint aFirstP, PolylinePoint abFirstP,
                                                     PolylinePoint aLastP, PolylinePoint abLastP,
                                                     bool aFirstBeforeALastOnB) {
            Debug.Assert(abLastP.Polyline != a);
            Debug.Assert(abFirstP.Polyline != a);
            var ret = new List<PolylinePoint>();
            for (PolylinePoint p = a.StartPoint; p != aFirstP; p = p.Next)
                ret.Add(p);

            if (aFirstBeforeALastOnB)
                for (PolylinePoint p = abFirstP; p != abLastP; p = p.Next)
                    ret.Add(p);
            else
                for (PolylinePoint p = abFirstP; p != abLastP; p = p.Prev)
                    ret.Add(p);

            for (PolylinePoint p = aLastP; p != null; p = p.Next)
                ret.Add(p);

            return ret;
        }

        Set<CdtEdge> GetChannelEdgeOfCollapsedA(Polyline a, PolylinePoint aFirstP, PolylinePoint abFirstP,
                                                PolylinePoint aLastP, PolylinePoint abLastP, bool aFirstBeforeALastOnB) {
            if (cdt == null) return null;
            var tracer = new CdtGeneralPolylineTracer(CollapsedPolylinePolylinePoints(a, aFirstP, abFirstP, aLastP,
                                                                                      abLastP,
                                                                                      aFirstBeforeALastOnB),
                                                      siteHierarchy, triangleHierarchy);
            return tracer.GetCrossedEdges();
        }

        /*
                IEnumerable<CdtEdge> GetCdtEdgesCrossedBySegmentStartingFromSiteAtStart(CdtSiteFeature f, PolylinePoint pp, out CdtFeature nextFeature) {
                    Debug.Assert(f.Prev == null);
                    var ret = new List<CdtEdge>();
                    var site = f.Site;
                    foreach (var t in site.Triangles) {
                        var si = t.Sites.Index(site);
                        if (Point.PointIsInsideCone(pp.Point, site.Point, t.Sites[si + 1].Point, t.Sites[si + 2].Point)) {
                            if (Point.GetTriangleOrientation(pp.Point, t.Sites[si + 1].Point, t.Sites[si + 2].Point) ==
                                TriangleOrientation.Collinear) {//pp belongs to the edge [si+1]
                                ret.Add(t.Edges[si + 1]);
                                if (Point.GetTriangleOrientation(site.Point, t.Sites[si + 1].Point, pp.Point) ==
                                    TriangleOrientation.Collinear)
                                    nextFeature = new CdtSiteFeature(t.Sites[si + 1], f) {Prev = f};
                                else if (Point.GetTriangleOrientation(site.Point, t.Sites[si + 2].Point, pp.Point) ==
                                         TriangleOrientation.Collinear)
                                    nextFeature = new CdtSiteFeature(t.Sites[si + 2], f) {Prev = f};
                                else
                                    nextFeature = new CdtEdgeFeature(t.Edges[si + 1], pp.Point, f) {Prev = f};
                            }
                        }
                    }
                }
        */


        /*
                Set<CdtEdge> GetCdtEdgesCrossedByPath0(List<PolylinePoint> polyPoints) {
                    PolylinePoint prevPolyPoint = null, prevPrevPolyPoint=null;
                    var ret = new Set<CdtEdge>();
                    foreach (var polylinePoint in polyPoints) {
                        if (prevPolyPoint!=null)
                            ret.InsertRange(GetCdtEdgesCrossedBySegment(prevPrevPolyPoint, prevPolyPoint, polylinePoint));

                        prevPrevPolyPoint = prevPolyPoint;
                        prevPolyPoint = polylinePoint;
                    }
        //            var l = new List<DebugCurve>();
        //            
        //            l.AddRange(
        //                Cdt.GetTriangles().Select(
        //                    t => new DebugCurve(100, 1, "green", new Polyline(t.Sites.Select(v => v.Point)) {Closed = true})));
        //            l.Add(new DebugCurve(150,2,"blue",new Polyline(polyPoints)));
        //          l.AddRange(ret.Select(e=>new DebugCurve(200,2,"brown", new LineSegment(e.upperSite.Point,e.lowerSite.Point))));
        //            LayoutAlgorithmSettings.ShowDebugCurves(l.ToArray());
        //           
                    return ret;
                }
        */

        /*
                IEnumerable<CdtEdge> GetCdtEdgesCrossedBySegment(PolylinePoint prevA, PolylinePoint a, PolylinePoint b) {
                    var pp = new PointPair(a.Point, b.Point);
                    IEnumerable<CdtEdge> intersections;
                    if (segsToCdtEdges.TryGetValue(pp, out intersections))
                        return intersections;
                    return segsToCdtEdges[pp] = Cdt.GetCdtEdgesCrossedBySegment(prevA, a, b);
                }
        */

        Dictionary<Point, PolylinePoint> GetPointMap(Polyline a) {
            var aPointMap = new Dictionary<Point, PolylinePoint>();
            foreach (PolylinePoint p in a.PolylinePoints)
                aPointMap[p.Point] = p;
            return aPointMap;
        }

        bool PolylinePointsAreInCorrectOrder(PolylinePoint u, PolylinePoint v) {
            Debug.Assert(u.Polyline == v.Polyline);
            for (PolylinePoint p = u; p != null; p = p.Next)
                if (p == v)
                    return true;

            return false;
        }

        void Collapse(Point aFirst, Point aLast, Dictionary<Point, PolylinePoint> aPointMap,
                      Dictionary<Point, PolylinePoint> bPointMap, bool aFirstBeforeALastOnB, IEnumerable<CdtEdge> abondonedEdges, IEnumerable<CdtEdge> enteredEdges) {

            PolylinePoint aFirstP = aPointMap[aFirst];
            PolylinePoint aLastP = aPointMap[aLast];

            UnregisterSegment(aFirstP, aLastP);
            PolylinePoint afb = bPointMap[aFirst];
            PolylinePoint alb = bPointMap[aLast];


            Func<PolylinePoint, PolylinePoint> nxt = aFirstBeforeALastOnB
                                                         ? (Func<PolylinePoint, PolylinePoint>)(p => p.Next)
                                                         : (p => p.Prev);

            for (afb = nxt(afb); afb != alb; afb = nxt(afb)) {
                var pp = new PolylinePoint(afb.Point) { Prev = aFirstP, Polyline = aFirstP.Polyline };
                aFirstP.Next = pp;
                aFirstP = pp;
                RegisterPolylinePointInPathsThrough(pp);
            }

            aFirstP.Next = aLastP;
            aLastP.Prev = aFirstP;
            if (cdt != null)
                UpdateResidualCapacities(aFirstP, abondonedEdges, enteredEdges);
        }

        void UpdateResidualCapacities(PolylinePoint aFirstP, IEnumerable<CdtEdge> abondonedEdges, IEnumerable<CdtEdge> enteredEdges) {
            double width = polylineToEdgeGeom[aFirstP.Polyline].LineWidth;
            double edgeSeparation = bundlingSettings.EdgeSeparation;
            foreach (var edge in abondonedEdges) {
                edge.ResidualCapacity += (edgeSeparation + width);
                if (edge.ResidualCapacity > edge.Capacity)
                    edge.ResidualCapacity = edge.Capacity;
            }

            foreach (var edge in enteredEdges) {
                if (edge.ResidualCapacity == edge.Capacity)
                    edge.ResidualCapacity -= width;
                else
                    edge.ResidualCapacity -= width + edgeSeparation;
            }
        }

        double CostGrowth(Point aFirst, Point aLast,
                         Dictionary<Point, PolylinePoint> aPointMap,
                         Dictionary<Point, PolylinePoint> bPointMap,
                         bool aFirstBeforeALastOnB, EdgeGeometry edgeGeometry,
                         IEnumerable<CdtEdge> edgesAbondonedByA, IEnumerable<CdtEdge> edgesEnteredByA) {
            double savedCost = SavedCostBetween(aPointMap[aFirst], aPointMap[aLast], edgeGeometry, edgesAbondonedByA);
            double addedCost = AddedCostBetweenAfterCollapse(bPointMap[aFirst], bPointMap[aLast], aFirstBeforeALastOnB, edgeGeometry, edgesEnteredByA);
            return addedCost - savedCost;
        }

        double SavedCostBetween(PolylinePoint aFirst, PolylinePoint aLast, EdgeGeometry edgeGeometry, IEnumerable<CdtEdge> edgesAbondonedByA) {
            double savedCost = 0;

            //path length
            savedCost += LengthBetween(aFirst, aLast);
            //capacity
            savedCost += SavedCapacityPenalty(edgeGeometry, edgesAbondonedByA);

            return savedCost;
        }

        double SavedCapacityPenalty(EdgeGeometry edgeGeometry, IEnumerable<CdtEdge> abondonedEdges) {
            if (abondonedEdges == null) return 0;
            return abondonedEdges.Sum(cdtEdge => SavedCapacityPenaltyOnCdtEdge(cdtEdge, edgeGeometry));
        }

        double SavedCapacityPenaltyOnCdtEdge(CdtEdge cdtEdge, EdgeGeometry edgeGeometry) {
            if (cdtEdge.ResidualCapacity > 0) return 0;

            double savedDelta;
            double width = edgeGeometry.LineWidth;
            if (cdtEdge.ResidualCapacity == cdtEdge.Capacity - width)
                savedDelta = width;
            else
                savedDelta = width + bundlingSettings.EdgeSeparation;
            if (savedDelta > -cdtEdge.ResidualCapacity)
                savedDelta = -cdtEdge.ResidualCapacity;
            return savedDelta * SdShortestPath.CapacityOverflowPenaltyMultiplier(bundlingSettings);
        }

        double AddedCostBetweenAfterCollapse(PolylinePoint aFirst, PolylinePoint aLast, bool aFirstBeforeALastOnB, EdgeGeometry edgeGeometry, IEnumerable<CdtEdge> edgesEnteredByA) {
            double addedCost = 0;

            //path length
            addedCost += aFirstBeforeALastOnB ? LengthBetween(aFirst, aLast) : LengthBetween(aLast, aFirst);
            //capacity
            addedCost += AddedCapacityPenalty(edgeGeometry, edgesEnteredByA);

            return addedCost;
        }

        double AddedCapacityPenalty(EdgeGeometry edgeGeometry, IEnumerable<CdtEdge> enteredCdtEdges) {
            if (cdt == null) return 0;
            return enteredCdtEdges.Sum(cdtEdge => AddedCapacityPenaltyForCdtEdge(cdtEdge, edgeGeometry));
        }

        double AddedCapacityPenaltyForCdtEdge(CdtEdge cdtEdge, EdgeGeometry edgeGeometry) {
            double capacityOverflowMultiplier = SdShortestPath.CapacityOverflowPenaltyMultiplier(bundlingSettings);
            return SdShortestPath.CostOfCrossingCdtEdge(capacityOverflowMultiplier,   bundlingSettings, edgeGeometry, cdtEdge);
        }

        double DeltaRandomCost(Point aFirst, Point aLast, Dictionary<Point, PolylinePoint> aPointMap, Dictionary<Point, PolylinePoint> bPointMap, bool aFirstBeforeALastOnB) {
            PolylinePoint aFirstPp = aPointMap[aFirst];
            PolylinePoint aLastPp = aPointMap[aLast];
            double oldCost = RandomCost(aFirstPp, aLastPp);
            double newCost = RandomCostBetweenAfterCollapse(bPointMap[aFirst], bPointMap[aLast], aFirstBeforeALastOnB);
            return newCost - oldCost;
        }

        double RandomCostBetweenAfterCollapse(PolylinePoint u, PolylinePoint v, bool aFirstBeforeALastOnB) {
            return aFirstBeforeALastOnB ? RandomCost(u, v) : RandomCost(v, u);
        }

        double LengthBetween(PolylinePoint aFirst, PolylinePoint aLast) {
            double r = 0;
            for (PolylinePoint p = aFirst; p != aLast; p = p.Next)
                r += (p.Point - p.Next.Point).Length;
            return r;
        }

        void FindFirstAndLast(Polyline a, out Point aFirst, out Point aLast, Dictionary<Point, PolylinePoint> bPoints) {
            aFirst = aLast = new Point();
            for (PolylinePoint p = a.StartPoint; p != null; p = p.Next) {
                if (bPoints.ContainsKey(p.Point)) {
                    aFirst = p.Point;
                    break;
                }
            }

            for (PolylinePoint p = a.EndPoint; p != null; p = p.Prev) {
                if (bPoints.ContainsKey(p.Point)) {
                    aLast = p.Point;
                    return;
                }
            }

            throw new InvalidOperationException();
        }


        //TODO
        void UnregisterSegment(PolylinePoint b, PolylinePoint bm) {
            for (b = b.Next; b != bm; b = b.Next)
                UnregisterPolypointFromPathsThrough(b);
        }

        void UnregisterPolypointFromPathsThrough(PolylinePoint b) {
            Set<PolylinePoint> paths = pathsThroughPoints[b.Point];
            Debug.Assert(paths.Contains(b));
            paths.Remove(b);
        }


        /// <summary>
        /// this function should be called very rarely
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <returns></returns>
        double RandomCost(PolylinePoint p0, PolylinePoint p1) {
            double w = 0;
            for (; p0 != p1; p0 = p0.Next)
                w += GetRandomWeightOfPolylinePoint(p0);
            return w;
        }

        Rectangle TriangleRectangle(ThreeArray<CdtSite> sites) {
            var rect = new Rectangle(sites[0].Point);
            rect.Add(sites[1].Point);
            rect.Add(sites[2].Point);
            return rect;
        }

        double GetRandomWeightOfPolylinePoint(PolylinePoint p) {
            PointPair pointPair = OrderedPair(p);
            double weight;
            if (randomWeights.TryGetValue(pointPair, out weight))
                return weight;
            randomWeights[pointPair] = weight = random.NextDouble();
            return weight;
        }

        internal static PointPair OrderedPair(PolylinePoint pp) {
            return OrderedPair(pp, pp.Next);
        }

        static PointPair OrderedPair(PolylinePoint p0, PolylinePoint p1) {
            return new PointPair(p0.Point, p1.Point);
        }

        internal Set<Point> GetChangedCrossing() {
            return interestingPoints;
        }
    }
}