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
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MST
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
        /// <param name="sizeId"></param>
        /// <returns></returns>
        static internal List<Tuple<int, int, double, double, double>> GetMstOnTuple(List<Tuple<int,int,double,double,double>> proximityEdges, int sizeId) {
            if (proximityEdges.Count == 0)
            {
                return null;
            }
            var intPairs = proximityEdges.Select(t => new IntPair(t.Item1, t.Item2)).ToArray();
            var weighting = new Dictionary<IntPair, Tuple<int, int, double, double, double>>(intPairs.Count());
            for (int i = 0; i < proximityEdges.Count; i++) {
                weighting[intPairs[i]] = proximityEdges[i];
            }
            var graph = new BasicGraph<IEdge>(intPairs, sizeId);

            var mstOnBasicGraph = new MinimumSpanningTreeByPrim(graph, intPair => weighting[(IntPair)intPair].Item5, intPairs[0].First);

            List<Tuple<int, int, double, double, double>> treeEdges = mstOnBasicGraph.GetTreeEdges().Select(e => weighting[(IntPair) e]).ToList();
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

            var graph = new BasicGraph<IEdge>( intPairsToCdtEdges.Keys, siteArray.Length);

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
#if DEBUG && !SILVERLIGHT && !SHARPKIT
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
