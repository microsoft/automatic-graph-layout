using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree
{
    /// <summary>
    /// Computes the minimum spanning tree on a triangulation or on a set of edges given by a list of tuple.
    /// </summary>
    public class MstOnDelaunayTriangulation
    {


        /// <summary>
        /// Computes the minimum spanning tree on a set of edges
        /// </summary>
        /// <param name="proximityEdges">list of tuples, each representing an edge with: nodeId1, nodeId2, t(overlapFactor), ideal distance, edge weight.</param>
        /// <param name="sizeId"></par­am>
        /// <returns></returns>
        static internal List<OverlappedEdge> GetMstOnTuple(List<OverlappedEdge> proximityEdges, int sizeId) {
            if (proximityEdges.Count == 0)
            {
                return null;
            }
            var intPairs = proximityEdges.Select(t => new IntPair(t.source, t.target)).ToArray();
            var weighting = new Dictionary<IntPair, OverlappedEdge>(intPairs.Count());
            for (int i = 0; i < proximityEdges.Count; i++) {
                weighting[intPairs[i]] = proximityEdges[i];
            }
            var graph = new BasicGraphOnEdges<IEdge>(intPairs, sizeId);

            var mstOnBasicGraph = new MinimumSpanningTreeByPrim(graph, intPair => weighting[(IntPair)intPair].weight, intPairs[0].First);

            List<OverlappedEdge> treeEdges = mstOnBasicGraph.GetTreeEdges().Select(e => weighting[(IntPair) e]).ToList();
            return treeEdges;
        }

        /// <summary>
        /// Computes the minimum spanning tree on a DT with given weights.
        /// </summary>
        /// <param name="cdt"></param>
        /// <param name="weights"></param>
        /// <returns></returns>
        static internal List<CdtEdge> GetMstOnCdt(Cdt cdt, Func<CdtEdge, double> weights) {
            var siteArray = cdt.PointsToSites.Values.ToArray();
            var siteIndex = new Dictionary<CdtSite, int>();
            for (int i = 0; i < siteArray.Length; i++)
                siteIndex[siteArray[i]] = i;

            Dictionary<IntPair, CdtEdge> intPairsToCdtEdges = GetEdges(siteArray, siteIndex);

            var graph = new BasicGraphOnEdges<IEdge>( intPairsToCdtEdges.Keys, siteArray.Length);

            var mstOnBasicGraph = new MinimumSpanningTreeByPrim(graph, intPair => weights(intPairsToCdtEdges[(IntPair)intPair]), 0);

            return new List<CdtEdge>(mstOnBasicGraph.GetTreeEdges().Select(e=>intPairsToCdtEdges[(IntPair)e]));
        }

        static Dictionary<IntPair, CdtEdge> GetEdges(CdtSite[] siteArray, Dictionary<CdtSite, int> siteIndex) {
            var d = new Dictionary<IntPair, CdtEdge>();
            for (int i = 0; i < siteArray.Length; i++) {
                var site = siteArray[i];
                var sourceIndex = siteIndex[site];
                foreach (var e in site.Edges)
                    d[new IntPair(sourceIndex, siteIndex[e.lowerSite])] = e;
            }
            return d;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Test()
        {
#if TEST_MSAGL && !SHARPKIT
            int count = 100;
            var random = new Random(3);
            var points = new List<Point>();
            for (int i = 0; i < count; i++)
                points.Add(20 * new Point(random.NextDouble(), random.NextDouble()));

            var cdt = new Cdt(points, null, null);
            cdt.Run();
            var ret = GetMstOnCdt(cdt, e => (e.lowerSite.Point - e.upperSite.Point).Length);
            var l = new List<DebugCurve>();
            foreach(var s in cdt.PointsToSites.Values)
                foreach (var e in s.Edges)
                {
                    l.Add(new DebugCurve(100, 0.1, "black", new LineSegment(e.lowerSite.Point, e.upperSite.Point)));
                }
            foreach (var e in ret)
            {
                l.Add(new DebugCurve(100, 0.12, "red", new LineSegment(e.lowerSite.Point, e.upperSite.Point)));
            }
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
#endif
        }
    }
}
