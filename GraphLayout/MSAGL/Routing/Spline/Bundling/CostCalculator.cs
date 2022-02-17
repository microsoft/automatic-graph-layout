using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Routing.Visibility;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using System;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// Calculates the cost of the routing
    /// </summary>
    internal class CostCalculator {
        internal const double Inf = 1000000000.0;

        readonly MetroGraphData metroGraphData;
        readonly BundlingSettings bundlingSettings;

        internal CostCalculator(MetroGraphData metroGraphData, BundlingSettings bundlingSettings) {
            this.metroGraphData = metroGraphData;
            this.bundlingSettings = bundlingSettings;
        }

        /// <summary>
        /// Error of ink
        /// </summary>
        static internal double InkError(double oldInk, double newInk, BundlingSettings bundlingSettings) {
            return (oldInk - newInk) * bundlingSettings.InkImportance;
        }

        /// <summary>
        /// Error of path lengths
        /// </summary>
        static internal double PathLengthsError(double oldLength, double newLength, double idealLength, BundlingSettings bundlingSettings) {
            return (oldLength - newLength) * (bundlingSettings.PathLengthImportance / idealLength);
        }

        /// <summary>
        /// Error of hubs
        /// </summary>
        static internal double RError(double idealR, double nowR, BundlingSettings bundlingSettings) {
            if (idealR <= nowR) return 0;

            double res = bundlingSettings.HubRepulsionImportance * (1.0 - nowR / idealR) * (idealR - nowR);
            return res;
        }

        /// <summary>
        /// Error of bundles
        /// </summary>
        static internal double BundleError(double idealWidth, double nowWidth, BundlingSettings bundlingSettings) {
            if (idealWidth <= nowWidth) return 0;

            double res = bundlingSettings.BundleRepulsionImportance * (1.0 - nowWidth / idealWidth) * (idealWidth - nowWidth);
            return res;
        }

        /// <summary>
        /// Cost of the whole graph
        /// </summary>
        static internal double Cost(MetroGraphData metroGraphData, BundlingSettings bundlingSettings) {
            double cost = 0;

            //ink
            cost += bundlingSettings.InkImportance * metroGraphData.Ink;

            //path lengths
            foreach (var metroline in metroGraphData.Metrolines) {
                cost += bundlingSettings.PathLengthImportance * metroline.Length / metroline.IdealLength;
            }

            cost += CostOfForces(metroGraphData);

            return cost;
        }

        /// <summary>
        /// Cost of the whole graph (hubs and bundles)
        /// </summary>
        static internal double CostOfForces(MetroGraphData metroGraphData) {
            double cost = 0;

            //hubs
            foreach (var v in metroGraphData.VirtualNodes()) {
                cost += v.cachedRadiusCost;
            }

            //bundles
            foreach (var edge in metroGraphData.VirtualEdges()) {
                var v = edge.Item1;
                var u = edge.Item2;
                cost += metroGraphData.GetIjInfo(v, u).cachedBundleCost;
            }

            return cost;
        }

        /// <summary>
        /// Gain of ink
        /// </summary>
        internal double InkGain(Station node, Point newPosition) {
            //ink
            double oldInk = metroGraphData.Ink;
            double newInk = metroGraphData.Ink;
            foreach (var adj in node.Neighbors) {
                Point adjPosition = adj.Position;
                newInk -= (adjPosition - node.Position).Length;
                newInk += (adjPosition - newPosition).Length;
            }
            return InkError(oldInk, newInk, bundlingSettings);
        }

        /// <summary>
        /// Gain of path lengths
        /// </summary>
        internal double PathLengthsGain(Station node, Point newPosition) {
            double gain = 0;
            //edge lengths
            foreach (var e in metroGraphData.MetroNodeInfosOfNode(node)) {
                var oldLength = e.Metroline.Length;
         
                var prev = e.PolyPoint.Prev.Point;
                var next = e.PolyPoint.Next.Point;

                var newLength = e.Metroline.Length + (next - newPosition).Length + (prev - newPosition).Length - (next - node.Position).Length - (prev - node.Position).Length;

                gain += PathLengthsError(oldLength, newLength, e.Metroline.IdealLength, bundlingSettings);
            }

            return gain;
        }

        /// <summary>
        /// Gain of radii
        /// </summary>
        internal double RadiusGain(Station node, Point newPosition) {
            double gain = 0;

            gain += node.cachedRadiusCost;
            gain -= RadiusCost(node, newPosition);

            return gain;
        }

        internal double RadiusCost(Station node, Point newPosition) {
            double idealR;
            if (ApproximateComparer.Close(node.Position, newPosition))
                idealR = node.cachedIdealRadius;
            else
                idealR = HubRadiiCalculator.CalculateIdealHubRadiusWithNeighbors(metroGraphData, bundlingSettings, node, newPosition);


            List<Tuple<Polyline, Point>> touchedObstacles;
            if (!metroGraphData.looseIntersections.HubAvoidsObstacles(node, newPosition, idealR, out touchedObstacles)) {
                return Inf;
            }

            double cost = 0;
            foreach (var d in touchedObstacles) {
                double dist = (d.Item2 - newPosition).Length;
                cost += RError(idealR, dist, bundlingSettings);
            }

            return cost;
        }

        /// <summary>
        /// Gain of bundles
        /// if a newPosition is not valid (e.g. intersect obstacles) the result is -inf
        /// </summary>
        internal double BundleGain(Station node, Point newPosition) {
            double gain = node.cachedBundleCost;
            foreach (var adj in node.Neighbors) {
                double lgain = BundleCost(node, adj, newPosition);
                if (ApproximateComparer.GreaterOrEqual(lgain, Inf)) return -Inf;
                gain -= lgain;
            }

            return gain;
        }

        internal double BundleCost(Station node, Station adj, Point newPosition) {
            double idealWidth = metroGraphData.GetWidth(node, adj, bundlingSettings.EdgeSeparation);
            List<Tuple<Point, Point>> closestDist;

            double cost = 0;
            //find conflicting obstacles
            if (!metroGraphData.cdtIntersections.BundleAvoidsObstacles(node, adj, newPosition, adj.Position, idealWidth, out closestDist)) {
                return Inf;
            }

            foreach (var pair in closestDist) {
                double dist = (pair.Item1 - pair.Item2).Length;
                cost += BundleError(idealWidth / 2, dist, bundlingSettings);
            }

            return cost;
        }

    }
}
