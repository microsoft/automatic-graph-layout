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
    /// Calculates node radii with 'water algorithm'
    /// </summary>
    public class HubRadiiCalculator {
        /// <summary>
        /// bundle data
        /// </summary>
        readonly MetroGraphData metroGraphData;
        /// <summary>
        /// Algorithm settings
        /// </summary>
        readonly BundlingSettings bundlingSettings;

        internal HubRadiiCalculator(MetroGraphData metroGraphData, BundlingSettings bundlingSettings) {
            this.metroGraphData = metroGraphData;
            this.bundlingSettings = bundlingSettings;
        }

        /// <summary>
        /// calculate node radii with fixed hubs
        /// </summary>
        internal void CreateNodeRadii() {
            //set radii to zero
            foreach (var v in metroGraphData.VirtualNodes()) {
                v.Radius = 0;
                v.cachedIdealRadius = CalculateIdealHubRadiusWithNeighbors(metroGraphData, bundlingSettings, v);
                ;
            }

            //TimeMeasurer.DebugOutput("Initial cost of radii: " + Cost());

            GrowHubs(false);
            //maximally use free space
            GrowHubs(true);

            //TimeMeasurer.DebugOutput("Optimized cost of radii: " + Cost());

            //ensure radii are not zero
            foreach (var v in metroGraphData.VirtualNodes()) {
                v.Radius = Math.Max(v.Radius, bundlingSettings.MinHubRadius);
            }
        }

        /// <summary>
        /// Grow hubs
        /// </summary>
        bool GrowHubs(bool useHalfEdgesAsIdealR) {
            var queue = new GenericBinaryHeapPriorityQueue<Station>();
            foreach (var v in metroGraphData.VirtualNodes()) {
                queue.Enqueue(v, -CalculatePotential(v, useHalfEdgesAsIdealR));
            }

            bool progress = false;
            //choose a hub with the greatest potential
            while (!queue.IsEmpty()) {
                double hu;
                Station v = queue.Dequeue(out hu);
                if (hu >= 0)
                    break;

                //grow the hub
                if (TryGrowHub(v, useHalfEdgesAsIdealR)) {
                    queue.Enqueue(v, -CalculatePotential(v, useHalfEdgesAsIdealR));
                    progress = true;
                }
            }
            return progress;
        }

        bool TryGrowHub(Station v, bool useHalfEdgesAsIdealR) {
            double oldR = v.Radius;
            double allowedRadius = CalculateAllowedHubRadius(v);
            Debug.Assert(allowedRadius > 0);
            if (v.Radius >= allowedRadius)
                return false;
            double idealR = useHalfEdgesAsIdealR ?
                                  CalculateIdealHubRadiusWithAdjacentEdges(bundlingSettings, v) :
                                  v.cachedIdealRadius;

            Debug.Assert(idealR > 0);
            if (v.Radius >= idealR)
                return false;
            double step = 0.05;
            double delta = step * (idealR - v.Radius);
            if (delta < 1.0)
                delta = 1.0;

            double newR = Math.Min(v.Radius + delta, allowedRadius);
            if (newR <= v.Radius)
                return false;

            v.Radius = newR;
            return true;
        }

        double CalculatePotential(Station v, bool useHalfEdgesAsIdealR) {
            double idealR = useHalfEdgesAsIdealR ?
                            CalculateIdealHubRadiusWithAdjacentEdges(bundlingSettings, v) :
                            v.cachedIdealRadius;

            if (idealR <= v.Radius)
                return 0.0;
            return (idealR - v.Radius) / idealR;
        }

        #region allowed and desired radii

        ///<summary>
        /// Returns the maximal possible radius of the node
        /// </summary>
        double CalculateAllowedHubRadius(Station node) {
            double r = bundlingSettings.MaxHubRadius;

            //adjacent nodes
            foreach (Station adj in node.Neighbors) {
                double dist = (adj.Position - node.Position).Length;
                Debug.Assert(dist - 0.05 * (node.Radius + adj.Radius) + 1 >= node.Radius + adj.Radius);
                r = Math.Min(r, dist / 1.05 - adj.Radius);
            }
            //TODO: still we can have two intersecting hubs for not adjacent nodes

            //obstacles
            double minimalDistance = metroGraphData.tightIntersections.GetMinimalDistanceToObstacles(node, node.Position, r);
            if (minimalDistance < r)
                r = minimalDistance - 0.001;

            return Math.Max(r, 0.1);
        }

        /// <summary>
        /// Returns the ideal radius of the hub
        /// </summary>
        static double CalculateIdealHubRadius(MetroGraphData metroGraphData, BundlingSettings bundlingSettings, Station node) {
            double r = 1.0;
            foreach (Station adj in node.Neighbors) {
                double width = metroGraphData.GetWidth(adj, node, bundlingSettings.EdgeSeparation);
                double nr = width / 2.0 + bundlingSettings.EdgeSeparation;
                r = Math.Max(r, nr);
            }

            r = Math.Min(r, 2 * bundlingSettings.MaxHubRadius);
            return r;
        }

        /// <summary>
        /// Returns the ideal radius of the hub
        /// </summary>
        internal static double CalculateIdealHubRadiusWithNeighbors(MetroGraphData metroGraphData, BundlingSettings bundlingSettings, Station node) {
            return CalculateIdealHubRadiusWithNeighbors(metroGraphData, bundlingSettings, node, node.Position);
        }

        /// <summary>
        /// Returns the ideal radius of the hub
        /// </summary>
        internal static double CalculateIdealHubRadiusWithNeighbors(MetroGraphData metroGraphData, BundlingSettings bundlingSettings, Station node, Point newPosition) {
            double r = CalculateIdealHubRadius(metroGraphData, bundlingSettings, node);

            if (node.Neighbors.Count() > 1) {
                Station[] adjNodes = node.Neighbors;
                //there must be enough space between neighbor bundles
                for (int i = 0; i < adjNodes.Length; i++) {
                    Station adj = adjNodes[i];
                    Station nextAdj = adjNodes[(i + 1) % adjNodes.Length];
                    r = Math.Max(r, GetMinRadiusForTwoAdjacentBundles(r, node, newPosition, adj, nextAdj, metroGraphData, bundlingSettings));
                }
            }
            r = Math.Min(r, 2 * bundlingSettings.MaxHubRadius);
            return r;
        }

        /// <summary>
        /// Returns the ideal radius of the hub
        /// </summary>
        static double CalculateIdealHubRadiusWithAdjacentEdges(BundlingSettings bundlingSettings, Station node) {
            double r = bundlingSettings.MaxHubRadius;
            foreach (var adj in node.Neighbors) {
                r = Math.Min(r, (node.Position - adj.Position).Length / 2);
            }

            return r;
        }

        internal static double GetMinRadiusForTwoAdjacentBundles(double r, Station node, Point nodePosition, Station adj0, Station adj1,
            MetroGraphData metroGraphData, BundlingSettings bundlingSettings) {
            double w0 = metroGraphData.GetWidth(node, adj0, bundlingSettings.EdgeSeparation);
            double w1 = metroGraphData.GetWidth(node, adj1, bundlingSettings.EdgeSeparation);

            return GetMinRadiusForTwoAdjacentBundles(r, nodePosition, adj0.Position, adj1.Position, w0, w1, metroGraphData, bundlingSettings);
        }

        /// <summary>
        /// Radius we need to draw to separate adjacent bundles ab and ac
        /// </summary>
        internal static double GetMinRadiusForTwoAdjacentBundles(double r, Point a, Point b, Point c, double widthAB, double widthAC,
            MetroGraphData metroGraphData, BundlingSettings bundlingSettings) {
            if (widthAB < ApproximateComparer.DistanceEpsilon || widthAC < ApproximateComparer.DistanceEpsilon)
                return r;

            double angle = Point.Angle(b, a, c);
            angle = Math.Min(angle, Math.PI * 2 - angle);
            if (angle < ApproximateComparer.DistanceEpsilon)
                return 2 * bundlingSettings.MaxHubRadius;

            if (angle >= Math.PI / 2)
                return r * 1.05;

            //find the intersection point of two bundles
            double sina = Math.Sin(angle);
            double cosa = Math.Cos(angle);
            double aa = widthAB / (4 * sina);
            double bb = widthAC / (4 * sina);
            double d = 2 * Math.Sqrt(aa * aa + bb * bb + 2 * aa * bb * cosa);
            d = Math.Min(d, 2 * bundlingSettings.MaxHubRadius);
            d = Math.Max(d, r);
            return d;
        }

        #endregion
    }
}
