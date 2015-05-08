using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing.Visibility;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    internal class BundleBasesCalculator {
        readonly IMetroMapOrderingAlgorithm metroOrdering;
        readonly MetroGraphData metroGraphData;
        readonly BundlingSettings bundlingSettings;

        List<BundleInfo> Bundles;
        //boundary curve with bases going outside the hub
        Dictionary<ICurve, List<BundleBase>> externalBases;
        //boundary curve with bases going inside the cluster
        Dictionary<ICurve, List<BundleBase>> internalBases;

        internal BundleBasesCalculator(IMetroMapOrderingAlgorithm metroOrdering, MetroGraphData metroGraphData, BundlingSettings bundlingSettings) {
            this.metroOrdering = metroOrdering;
            this.metroGraphData = metroGraphData;
            this.bundlingSettings = bundlingSettings;
        }

        internal void Run() {
            //HubDebugger.ShowHubs(metroGraphData, bundlingSettings, true);
            //HubDebugger.ShowHubs(metroGraphData, bundlingSettings);

            AllocateBundleBases();
            SetBasesRightLeftParamsToTheMiddles();
            if (bundlingSettings.KeepOverlaps) {
                UpdateSourceAndTargetBases();
                CreateOrientedSegs();
            }
            else {
                SetRightLeftParamsFeasiblySymmetrically();
                //EdgeNudger.ShowHubs(metroGraphData, metroOrdering, null);
                //these bases can be too wide and overlap each other, so we need to adjust them
                AdjustStartEndParamsToAvoidBaseOverlaps();
                UpdateSourceAndTargetBases();
                //EdgeNudger.ShowHubs(metroGraphData, metroOrdering, null);

                CreateOrientedSegs();
                //EdgeNudger.ShowHubs(metroGraphData, metroOrdering, null);

                //optimization: moving bases to reduce cost
                //TimeMeasurer.DebugOutput("Initial cost of bundle bases: " + Cost());
                Optimize();
                //EdgeNudger.ShowHubs(metroGraphData, metroOrdering, null);
                AdjustStartEndParamsToAvoidBaseOverlaps();
                UpdateSourceAndTargetBases();
            }

            //TimeMeasurer.DebugOutput("Optimized cost of bundle bases: " + Cost());
//            EdgeNudger.ShowHubs(metroGraphData, metroOrdering, null);
        }

        #region Initialization

        void AllocateBundleBases() {
            externalBases = new Dictionary<ICurve, List<BundleBase>>();
            internalBases = new Dictionary<ICurve, List<BundleBase>>();
            Bundles = new List<BundleInfo>();

            foreach (var station in metroGraphData.Stations) {
                if (station.BoundaryCurve == null)
                    station.BoundaryCurve = new Ellipse(station.Radius, station.Radius, station.Position);
            }

            foreach (var station in metroGraphData.Stations) {
                foreach (Station neighbor in station.Neighbors) {
                    if (station < neighbor) {
                        var bb = new BundleBase(metroGraphData.RealEdgeCount(station, neighbor), station.BoundaryCurve, station.Position, station.IsRealNode, neighbor.SerialNumber);
                        station.BundleBases[neighbor] = bb;

                        var bb2 = new BundleBase(metroGraphData.RealEdgeCount(station, neighbor), neighbor.BoundaryCurve, neighbor.Position, neighbor.IsRealNode, station.SerialNumber);
                        neighbor.BundleBases[station] = bb2;

                        if (Curve.PointRelativeToCurveLocation(neighbor.Position, station.BoundaryCurve) != PointLocation.Outside) {
                            bb.IsParent = true;
                            CollectionUtilities.AddToMap(internalBases, station.BoundaryCurve, bb);
                            CollectionUtilities.AddToMap(externalBases, neighbor.BoundaryCurve, bb2);
                        }
                        else if (Curve.PointRelativeToCurveLocation(station.Position, neighbor.BoundaryCurve) != PointLocation.Outside) {
                            bb2.IsParent = true;
                            CollectionUtilities.AddToMap(externalBases, station.BoundaryCurve, bb);
                            CollectionUtilities.AddToMap(internalBases, neighbor.BoundaryCurve, bb2);
                        }
                        else {
                            CollectionUtilities.AddToMap(externalBases, station.BoundaryCurve, bb);
                            CollectionUtilities.AddToMap(externalBases, neighbor.BoundaryCurve, bb2);
                        }

                        Set<Polyline> obstaclesToIgnore = metroGraphData.tightIntersections.ObstaclesToIgnoreForBundle(station, neighbor);
                        var bundle = new BundleInfo(bb, bb2, obstaclesToIgnore, bundlingSettings.EdgeSeparation, metroOrdering.GetOrder(station, neighbor).Select(l => l.Width / 2).ToArray());
                        bb.OutgoingBundleInfo = bb2.IncomingBundleInfo = bundle;
                        Bundles.Add(bundle);
                    }

                }
            }

            //neighbors
            SetBundleBaseNeighbors();
        }

        void SetBundleBaseNeighbors() {
            foreach (var c in externalBases.Keys) {
                List<BundleBase> list = externalBases[c];
                SortBundlesCounterClockwise(list);

                //set left
                SetLeftRightBases(list);
            }

            foreach (var c in internalBases.Keys) {
                List<BundleBase> list = internalBases[c];
                SortBundlesCounterClockwise(list);

                //set left
                SetLeftRightBases(list);
            }
        }

        void SortBundlesCounterClockwise(List<BundleBase> list) {
            if (list.Count > 2) {
                Point pivot = list[0].OppositeBase.Position;
                Point center = list[0].CurveCenter;
                list.Sort(delegate(BundleBase u, BundleBase v) {
                    return Point.GetOrientationOf3Vectors(pivot - center, u.OppositeBase.Position - center, v.OppositeBase.Position - center);
                });
            }
        }

        void SetLeftRightBases(List<BundleBase> bases) {
            int count = bases.Count;
            if (count <= 1)
                return;
            for (int i = 0; i < count; i++) {
                bases[i].Prev = bases[(i - 1 + count) % count];
                bases[i].Next = bases[(i + 1) % count];
            }
        }

        void CreateOrientedSegs() {
            foreach (var metroline in metroGraphData.Metrolines) {
                CreateOrientedSegsOnLine(metroline);
            }
        }

        void CreateOrientedSegsOnLine(Metroline line) {
            for (PolylinePoint polyPoint = line.Polyline.StartPoint.Next; polyPoint.Next != null; polyPoint = polyPoint.Next)
                CreateOrientedSegsOnLineVertex(line, polyPoint);
        }

        void CreateOrientedSegsOnLineVertex(Metroline line, PolylinePoint polyPoint) {
            Station u = metroGraphData.PointToStations[polyPoint.Prev.Point];
            Station v = metroGraphData.PointToStations[polyPoint.Point];
            Station w = metroGraphData.PointToStations[polyPoint.Next.Point];
            BundleBase h0 = v.BundleBases[u];
            BundleBase h1 = v.BundleBases[w];
            int j0 = metroOrdering.GetLineIndexInOrder(u, v, line);
            int j1 = metroOrdering.GetLineIndexInOrder(w, v, line);
            OrientedHubSegment or0 = h0.OrientedHubSegments[j0] = new OrientedHubSegment(null, false, j0, h0);
            OrientedHubSegment or1 = h1.OrientedHubSegments[j1] = new OrientedHubSegment(null, true, j1, h1);
            or1.Other = or0;
            or0.Other = or1;
        }

        void UpdateSourceAndTargetBases() {
            foreach (var bundleInfo in Bundles)
                bundleInfo.UpdateSourceAndTargetBases(true, true);
        }

        void SetBasesRightLeftParamsToTheMiddles() {
            foreach (var bundle in Bundles) {
                var sbase = bundle.SourceBase;
                var tbase = bundle.TargetBase;
                sbase.ParRight = sbase.ParLeft = GetBaseMiddleParamInDirection(sbase, sbase.Position, tbase.Position);
                tbase.ParRight = tbase.ParLeft = GetBaseMiddleParamInDirection(tbase, tbase.Position, sbase.Position);
            }
        }

        double GetBaseMiddleParamInDirection(BundleBase targetBase, Point sPos, Point neighbPos) {
            var curve = targetBase.Curve;
            var circle = curve as Ellipse;
            if (circle != null && circle.IsArc())
                return Point.Angle(circle.AxisA, neighbPos - sPos);

            var intersections = Curve.GetAllIntersections(curve, new LineSegment(sPos, neighbPos), true);
            foreach (var intersectionInfo in intersections) {
                var xP = intersectionInfo.IntersectionPoint;
                if ((xP - sPos) * (xP - neighbPos) <= 0) {
                    return intersectionInfo.Par0;
                }
            }

            throw new InvalidOperationException();
        }

        void SetRightLeftParamsFeasiblySymmetrically() {
            foreach (var bundle in Bundles) {
                bundle.SetParamsFeasiblySymmetrically(metroGraphData.TightTree);
            }
        }

        void AdjustStartEndParamsToAvoidBaseOverlaps() {
            foreach (var c in externalBases.Keys) {
                AdjustCurrentBundleWidthsOnCurve(externalBases[c]);
            }

            foreach (var c in internalBases.Keys) {
                AdjustCurrentBundleWidthsOnCurve(internalBases[c]);
            }
        }

        void AdjustCurrentBundleWidthsOnCurve(List<BundleBase> bases) {
            int count = bases.Count;
            if (count <= 1)
                return;

            for (int i = 0; i < count; i++) {
                BundleBase rBase = bases[i];
                BundleBase lBase = rBase.Next;

                ShrinkBasesToMakeTwoConsecutiveNeighborsHappy(rBase, lBase);
                Debug.Assert(!rBase.Intersect(lBase));
            }
        }

        void ShrinkBasesToMakeTwoConsecutiveNeighborsHappy(BundleBase rBase, BundleBase lBase) {
            if (!rBase.Intersect(lBase))
                return;

            //segments are now [l1..r1] and [l2..r2]
            double l1 = rBase.ParRight;
            double r1 = rBase.ParLeft;
            double l2 = lBase.ParRight;
            double r2 = lBase.ParLeft;

            double span = lBase.ParameterSpan;

            //make them regular
            if (l1 > r1)
                l1 -= span;
            if (l2 > r2)
                l2 -= span;

            //make them intersecting
            if (l2 > r1) {
                l2 -= span;
                r2 -= span;
            }

            if (l1 > r2) {
                l1 -= span;
                r1 -= span;
            }

            //they do intersect!
            Debug.Assert(!(l2 >= r1) && !(l1 >= r2));

            double t = RegularCut(l1, r1, l2, r2, rBase.Span, lBase.Span);
            TriangleOrientation to = Point.GetTriangleOrientation(lBase.CurveCenter, lBase.OppositeBase.InitialMidPoint, rBase.OppositeBase.InitialMidPoint);

            if (to == TriangleOrientation.Clockwise) {
                r1 = t;
                l2 = t;
            }
            else if (to == TriangleOrientation.Counterclockwise) {
                r2 = t;
                l1 = t;
            }
            else {
                if (r2 - l1 >= r1 - l2) {
                    r1 = t;
                    l2 = t;
                }
                else {
                    r2 = t;
                    l1 = t;
                }
            }

            Debug.Assert(!rBase.Intersect(l1, r1, l2, r2));

            lBase.ParRight = lBase.AdjustParam(l2);
            lBase.ParLeft = lBase.AdjustParam(r2);
            rBase.ParRight = rBase.AdjustParam(l1);
            rBase.ParLeft = rBase.AdjustParam(r1);
        }

        /// <summary>
        /// find a cut point for 2 segments
        /// </summary>
        /// <returns>true if the segment interiors intersect</returns>
        double RegularCut(double l1, double r1, double l2, double r2, double span1, double span2) {
            double cutParam = (span1 * r2 + span2 * l1) / (span1 + span2);
            double mn = Math.Min(r1, r2);
            double mx = Math.Max(l1, l2);
            Debug.Assert(ApproximateComparer.LessOrEqual(mx, cutParam) && ApproximateComparer.LessOrEqual(cutParam, mn));
            if (cutParam < mx)
                cutParam = mx;
            if (cutParam > mn)
                cutParam = mn;

            return cutParam;
        }

        #endregion

        #region Optimization

        static readonly int[][] Deltas = new[] {
              new [] {1,1},//rotating both point ccw
              new [] {0,1},//rotating both point ccw
              new [] {-1,1},//rotating both point ccw
              new [] {1,0},//rotating both point ccw
              new [] {-1,0},//rotating both point ccw
              new [] {1,-1},//rotating both point ccw
              new [] {0,-1},//rotating both point ccw
              new [] {-1,-1},//rotating both point ccw
              new [] {0,0},//rotating both point ccw
            };

        const double SeparationCoeff = 1;
        const double SqueezeCoeff = 1;
        const double CenterCoeff = 10;
        const double AssymetryCoeff = 1;

        const int MaxIterations = 200;
        const double MaxParameterChange = 8 / 360.0;//it would be one degree for a circle
        const double MinParameterChange = 0.1 / 360.0;

        const double CostThreshold = 0.00001;
        const double CostDeltaThreshold = 0.01;

        HashSet<BundleInfo> fixedBundles = new HashSet<BundleInfo>();

        void Optimize() {
            double parameterChange = MaxParameterChange;
            double cost = Cost();

            int iteration = 0;
            //HubDebugger.ShowHubs(metroGraphData, bundlingSettings, true);

            while (iteration++ < MaxIterations) {

                double oldCost = cost;
                OptimizeBundles(parameterChange, ref cost);

                parameterChange = UpdateParameterChange(parameterChange, oldCost, cost);
                if (parameterChange < MinParameterChange)
                    break;

                if (iteration % 10 == 0) {
                    //TimeMeasurer.DebugOutput("bundle bases cost is " + cost + " (" + iteration + " iterations) " + parameterChange * 360);
                    //EdgeNudger.ShowHubs(metroGraphData, metroOrdering, null);
                }
            }

            //TimeMeasurer.DebugOutput("bases optimization completed after " + iteration + " iterations (cost=" + cost + ")");
        }


        //the cooling scheme follows Yifan Hu, Efficient and high quality force-directed graph drawing
        int stepsWithProgress = 0;
        double UpdateParameterChange(double step, double oldEnergy, double newEnergy) {
            //cooling factor
            double T = 0.8;
            if (newEnergy + 1.0 < oldEnergy) {
                stepsWithProgress++;
                if (stepsWithProgress >= 5) {
                    stepsWithProgress = 0;
                    //step = Math.Min(MaxParameterChange, step / T);
                    fixedBundles.Clear();
                }
            }
            else {
                stepsWithProgress = 0;
                step *= T;
                fixedBundles.Clear();
            }

            return step;
        }

        bool OptimizeBundles(double parameterChange, ref double cost) {
            var progress = false;
            foreach (var bundleInfo in Bundles) {
                if (fixedBundles.Contains(bundleInfo))
                    continue;

                if (OptimizeBundle(bundleInfo, parameterChange, ref cost)) {
                    progress = true;
                    /*bool isClusterS = bundleInfo.SourceBase.CurveCenter != bundleInfo.SourceBase.Position;
                    bool isClusterT = bundleInfo.TargetBase.CurveCenter != bundleInfo.TargetBase.Position;
                    while ((isClusterS || isClusterT) && OptimizeBundle(bundleInfo, parameterChange, ref cost)) { }*/
                }
                else
                    fixedBundles.Add(bundleInfo);
            }
            return progress;
        }

        bool OptimizeBundle(BundleInfo bundleInfo, double parameterChange, ref double cost) {
            double bundleCost = Cost(bundleInfo);
            if (bundleCost < CostThreshold)
                return false;

            //choose the best step
            double bestDelta = 0;
            int bestI = -1, bestJ = -1;

            for (int i = 0; i < Deltas.Length - 1; i++) {
                double delta = DeltaWithChangedAngles(Deltas[i][0], Deltas[i][1], 0, 0, bundleInfo, bundleCost, parameterChange);
                if (delta > CostDeltaThreshold && delta > bestDelta) {
                    bestI = i;
                    bestJ = Deltas.Length - 1;
                    bestDelta = delta;
                }

                delta = DeltaWithChangedAngles(0, 0, Deltas[i][0], Deltas[i][1], bundleInfo, bundleCost, parameterChange);
                if (delta > CostDeltaThreshold && delta > bestDelta) {
                    bestI = Deltas.Length - 1;
                    bestJ = i;
                    bestDelta = delta;
                }
            }

            if (bestDelta < CostDeltaThreshold)
                return false;
            //do the change
            cost -= bestDelta;

            bundleInfo.RotateBy(Deltas[bestI][0], Deltas[bestI][1], Deltas[bestJ][0], Deltas[bestJ][1], parameterChange);

            return true;
        }

        double DeltaWithChangedAngles(int rotationOfSourceRigthPoint, int rotationOfSourceLeftPoint, int rotationOfTargetRigthPoint, int rotationOfTargetLeftPoint,
            BundleInfo bundleInfo, double bundleCost, double parameterChange) {
            if (!bundleInfo.RotationIsLegal(rotationOfSourceRigthPoint, rotationOfSourceLeftPoint, rotationOfTargetRigthPoint, rotationOfTargetLeftPoint, parameterChange))
                return 0;

            bundleInfo.RotateBy(rotationOfSourceRigthPoint, rotationOfSourceLeftPoint, rotationOfTargetRigthPoint, rotationOfTargetLeftPoint, parameterChange);
            var newCost = Cost(bundleInfo, bundleCost);

            //restoring
            bundleInfo.RotateBy(-rotationOfSourceRigthPoint, -rotationOfSourceLeftPoint, -rotationOfTargetRigthPoint, -rotationOfTargetLeftPoint, parameterChange);

            return bundleCost - newCost;
        }

        double Cost(BundleInfo bundleInfo) {
            return
                SeparationCoeff * SeparationCost(bundleInfo) +
                SqueezeCoeff * SqueezeCost(bundleInfo) +
                AssymetryCoeff * AssymetryCost(bundleInfo) +
                CenterCoeff * CenterCost(bundleInfo);
        }

        //this is an accelerated version of the above function (calculate cost partly)
        double Cost(BundleInfo bundleInfo, double limit) {
            double cost = 0;
            cost += CenterCoeff * CenterCost(bundleInfo);
            if (cost > limit)
                return cost;
            cost += SeparationCoeff * SeparationCost(bundleInfo);
            if (cost > limit)
                return cost;
            cost += SqueezeCoeff * SqueezeCost(bundleInfo);
            if (cost > limit)
                return cost;
            cost += AssymetryCoeff * AssymetryCost(bundleInfo);
            return cost;
        }

        double SqueezeCost(BundleInfo bundleInfo) {
            var middleLineDir = (bundleInfo.TargetBase.MidPoint - bundleInfo.SourceBase.MidPoint).Normalize();
            var perp = middleLineDir.Rotate90Ccw();
            var projecton0 = Math.Abs((bundleInfo.SourceBase.RightPoint - bundleInfo.SourceBase.LeftPoint) * perp);
            var projecton1 = Math.Abs((bundleInfo.TargetBase.RightPoint - bundleInfo.TargetBase.LeftPoint) * perp);

            double del0 = Math.Abs(bundleInfo.TotalRequiredWidth - projecton0) / bundleInfo.TotalRequiredWidth;
            double del1 = Math.Abs(bundleInfo.TotalRequiredWidth - projecton1) / bundleInfo.TotalRequiredWidth;
            double del = Math.Abs(projecton0 - projecton1) / bundleInfo.TotalRequiredWidth;

            double cost = Math.Exp(del0 * 10) - 1 + Math.Exp(del1 * 10) - 1;

            cost += del;
            return cost;
        }

        double CenterCost(BundleInfo bundleInfo) {
            if (!bundleInfo.SourceBase.BelongsToRealNode && !bundleInfo.TargetBase.BelongsToRealNode)
                return 0;

            return CenterCost(bundleInfo.SourceBase) + CenterCost(bundleInfo.TargetBase);
        }

        double CenterCost(BundleBase bundleBase) {
            if (!bundleBase.BelongsToRealNode)
                return 0;

            double currentMid = bundleBase.ParMid;
            double mn = Math.Min(bundleBase.InitialMidParameter, currentMid);
            double mx = Math.Max(bundleBase.InitialMidParameter, currentMid);
            double dist = Math.Min(mx - mn, mn + bundleBase.ParameterSpan - mx);
            if (bundleBase.CurveCenter == bundleBase.Position || bundleBase.IsParent)
                return 25 * dist * dist;
            else
                return 500 * dist * dist;
        }

        double AssymetryCost(BundleInfo bundleInfo) {
            return GetAssymetryCostForBase(bundleInfo.SourceBase) + GetAssymetryCostForBase(bundleInfo.TargetBase);
        }

        double GetAssymetryCostForBase(BundleBase bundleBase) {
            if (bundleBase.BelongsToRealNode)
                return 0;

            double assymetryWeight = bundleBase.OppositeBase.BelongsToRealNode ? 200 : 500;
            double cost = 0;
            foreach (var o in bundleBase.OrientedHubSegments) {
                int i0 = o.Index;
                int i1 = o.Other.Index;

                var a = bundleBase.Points[i0];
                var ta = bundleBase.Tangents[i0];

                var oppositeBase = o.Other.BundleBase;
                var b = oppositeBase.Points[i1];
                var tb = oppositeBase.Tangents[i1];

                double s = bundleBase.Count + oppositeBase.Count;
                cost += GetAssymetryCostOnData(a, ta, b, tb, assymetryWeight) / s;
            }

            return cost;
        }

        double GetAssymetryCostOnData(Point a, Point tangentA, Point b, Point tangentB, double assymetryWeight) {
            var xAxis = (a - b);
            var len = xAxis.Length;
            if (len < ApproximateComparer.DistanceEpsilon)
                return 0;
            xAxis /= len;
            //Tangents both have length 1. If they compensate each other on x-asis,
            //then their projections on y-axis are the same.
            var delx = (tangentA + tangentB) * xAxis;
            //var yAxis = xAxis.Rotate90Ccw();
            //var ay = tangentA * yAxis;
            //var by = tangentB * yAxis;
            var ay = Point.CrossProduct(xAxis, tangentA);
            var by = Point.CrossProduct(xAxis, tangentB);

            var dely = ay - by;

            //double ac = Math.Sqrt(delx * delx + dely * dely);
            //double bc = Math.Sqrt(ay * ay + by * by);
            double ac = delx * delx + dely * dely;
            double bc = ay * ay + by * by;
            return 10 * ac + assymetryWeight * bc;
        }

        double SeparationCost(BundleInfo bundleInfo) {
            return SeparationCostForBundleBase(bundleInfo.SourceBase) + SeparationCostForBundleBase(bundleInfo.TargetBase);
        }

        double SeparationCostForBundleBase(BundleBase bBase) {
            if (bBase.Prev == null)
                return 0;

            return SeparationCostForAdjacentBundleBases(bBase, bBase.Prev) + SeparationCostForAdjacentBundleBases(bBase, bBase.Next);
        }

        double SeparationCostForAdjacentBundleBases(BundleBase base0, BundleBase base1) {
            Debug.Assert(base0.Curve == base1.Curve);

            ICurve boundaryCurve = base0.Curve;
            double len = IntervalsOverlapLength(base0.ParRight, base0.ParLeft, base1.ParRight, base1.ParLeft, boundaryCurve);
            double mn = Math.Min(base0.Span, base1.Span);
            Debug.Assert(ApproximateComparer.LessOrEqual(len, mn));
            Debug.Assert(mn > 0);
            return Math.Exp(len / mn * 10) - 1;
        }

        /// <summary>
        /// returns the length of the overlapped interval in parameter space
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="curve"></param>
        double IntervalsOverlapLength(double a, double b, double c, double d, ICurve curve) {
            var s = curve.ParStart;
            var e = curve.ParEnd;
            if (a < b) {
                if (c < d)
                    return IntersectRegularIntervals(a, b, c, d);
                return IntersectRegularIntervals(a, b, c, e) + IntersectRegularIntervals(a, b, s, d);
            }
            if (c < d)
                return IntersectRegularIntervals(a, e, c, d) + IntersectRegularIntervals(s, b, c, d);

            return IntersectRegularIntervals(a, e, c, e) + IntersectRegularIntervals(s, b, s, d);
        }

        double IntersectRegularIntervals(double a, double b, double c, double d) {
            var low = Math.Max(a, c);
            var up = Math.Min(b, d);
            if (low < up) {
                return up - low;
            }
            return 0;
        }

        double Cost() {
            double cost = 0;
            foreach (var bundleInfo in Bundles) {
                double c1 = SeparationCoeff * SeparationCost(bundleInfo);
                double c2 = AssymetryCoeff * AssymetryCost(bundleInfo);
                double c3 = SqueezeCoeff * SqueezeCost(bundleInfo);
                double c4 = CenterCoeff * CenterCost(bundleInfo);

                cost += c1 / 2.0 + c2 / 2.0 + c3 + c4;
                Debug.Assert(cost < double.PositiveInfinity);
            }
            return cost;
        }

        #endregion
    }
}
