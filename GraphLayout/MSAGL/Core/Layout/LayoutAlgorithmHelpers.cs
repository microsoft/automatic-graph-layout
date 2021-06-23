using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Core.Layout
{
    /// <summary>
    /// static helper methods for layout algorithms
    /// </summary>
    public static class LayoutAlgorithmHelpers
    {
        /// <summary>
        /// Linearly interpolates a result between the minResult and the maxResult based on the location of the value between the lowerThreshold and the upperThreshold.
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <param name="lowerThreshold">If the input value is lower than the lowerThreshold, minResult is returned.</param>
        /// <param name="upperThreshold">If the input value is higher than the upperThreshold, maxResult is returned.</param>
        /// <param name="minResult">The minimum result.</param>
        /// <param name="maxResult">The maximum result.</param>
        /// <returns>The linearly interpolated result.  Between minResult and maxResult, inclusive.</returns>
        internal static int LinearInterpolation(int value, int lowerThreshold, int upperThreshold, int minResult, int maxResult)
        {
            if (value < lowerThreshold) return minResult;
            if (value > upperThreshold) return maxResult;
            double fraction = (value - lowerThreshold) / (double)(upperThreshold - lowerThreshold);
            return minResult + (int)(fraction * (maxResult - minResult));
        }

        /// <summary>
        /// Negatively linearly interpolates a result between the minResult and the maxResult based on the location of the value between the lowerThreshold and the upperThreshold.
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <param name="lowerThreshold">If the input value is lower than the lowerThreshold, maxResult is returned.</param>
        /// <param name="upperThreshold">If the input value is higher than the upperThreshold, minResult is returned.</param>
        /// <param name="minResult">The minimum result.</param>
        /// <param name="maxResult">The maximum result.</param>
        /// <returns>The linearly interpolated result.  Between minResult and maxResult, inclusive.</returns>
        internal static int NegativeLinearInterpolation(int value, int lowerThreshold, int upperThreshold, int minResult, int maxResult)
        {
            if (value < lowerThreshold) return maxResult;
            if (value > upperThreshold) return minResult;
            double fraction = (value - lowerThreshold) / (double)(upperThreshold - lowerThreshold);
            return minResult + (int)((1 - fraction) * (maxResult - minResult));
        }

        /// <summary>
        /// Compute ideal edge lengths for the given graph component based on the given settings
        /// </summary>
        /// <param name="settings">settings for calculating ideal edge length</param>
        /// <param name="component">a graph component</param>
        public static void ComputeDesiredEdgeLengths(EdgeConstraints settings, GeometryGraph component)
        {
            if (component == null)
            {
                return;
            }
            foreach (var e in component.Edges)
            {
                e.SourcePort = null;
                e.TargetPort = null;

                // use larger of DefaultLength and
                // minimum of the diagonal of a square of area equal to the bounding box of the source and that of the target
                e.Length = 
                    Math.Sqrt(2d * Math.Min(e.Source.BoundingBox.Width * e.Source.BoundingBox.Height, e.Target.BoundingBox.Width * e.Target.BoundingBox.Height));
            }
            
        }

        ///<summary>
        ///Set ideal edge lengths to be proportional to the symmetric difference between neighbour
        ///sets of the pair of nodes associated with the edge.
        ///</summary>
        ///<param name="graph"></param>
        ///<param name="lengthAdjustment">The fraction of the edge length to add for each symmetric difference unit. (Example: 0.05)</param>
        ///<param name="lengthInitialOffset">The initial fraction of the edge length to use before adding the symmetric difference adjustment. (Example: 0.8)</param>
        internal static void SetEdgeLengthsProportionalToSymmetricDifference(GeometryGraph graph, double lengthInitialOffset, double lengthAdjustment)
        {
            ValidateArg.IsNotNull(graph, "graph");
            var neighbors = new Dictionary<Node, Set<Node>>();
            foreach (var u in graph.Nodes)
            {
                var ns = neighbors[u] = new Set<Node>();
                foreach (var e in u.OutEdges)
                {
                    if (!(e.Target is Cluster))
                    {
                        ns.Insert(e.Target);
                    }
                }
                foreach (var e in u.InEdges)
                {
                    if (!(e.Source is Cluster))
                    {
                        ns.Insert(e.Source);
                    }
                }
            }
            foreach (var e in graph.Edges)
            {
                if (!(e.Source is Cluster || e.Target is Cluster))
                {
                    var u = neighbors[e.Source];
                    var v = neighbors[e.Target];
                    double l = u.Union(v).Count() - u.Intersect(v).Count();

                    // cap it at 30
                    l = Math.Min(30, l);

                    e.Length *= lengthInitialOffset + l * lengthAdjustment;
                }
            }
        }
    }
}
