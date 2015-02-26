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
using Microsoft.Msagl.Routing.Visibility;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// Stores intersections between edges, hubs, and obstacles to speed up simulated annealing
    /// </summary>
    internal class IntersectionCache {
        readonly MetroGraphData metroGraphData;
        readonly BundlingSettings bundlingSettings;
        readonly CostCalculator costCalculator;
        readonly Cdt cdt;

        public IntersectionCache(MetroGraphData metroGraphData, BundlingSettings bundlingSettings, CostCalculator costCalculator, Cdt cdt) {
            Debug.Assert(cdt!=null);
            this.metroGraphData = metroGraphData;
            this.bundlingSettings = bundlingSettings;
            this.costCalculator = costCalculator;
            this.cdt = cdt;
        }

        internal void InitializeCostCache() {
            foreach (var v in metroGraphData.VirtualNodes()) {
                v.cachedIdealRadius = HubRadiiCalculator.CalculateIdealHubRadiusWithNeighbors(metroGraphData, bundlingSettings, v);
                v.cachedRadiusCost = costCalculator.RadiusCost(v, v.Position);
                v.cachedBundleCost = 0;
            }

            foreach (var edge in metroGraphData.VirtualEdges()) {
                var v = edge.Item1;
                var u = edge.Item2;
                StationEdgeInfo edgeInfo = metroGraphData.GetIjInfo(v, u);
                edgeInfo.cachedBundleCost = costCalculator.BundleCost(v, u, v.Position);
                v.cachedBundleCost += edgeInfo.cachedBundleCost;
                u.cachedBundleCost += edgeInfo.cachedBundleCost;
            }
        }

        internal void UpdateCostCache(Station node) {
            RectangleNode<CdtTriangle> cdtTree = cdt.GetCdtTree();
            node.CdtTriangle = cdtTree.FirstHitNode(node.Position, Test).UserData;

            node.cachedIdealRadius = HubRadiiCalculator.CalculateIdealHubRadiusWithNeighbors(metroGraphData, bundlingSettings, node);
            node.cachedRadiusCost = costCalculator.RadiusCost(node, node.Position);
            node.cachedBundleCost = 0;

            foreach (var adj in node.Neighbors) {
                if (!adj.IsRealNode) {
                    adj.cachedIdealRadius = HubRadiiCalculator.CalculateIdealHubRadiusWithNeighbors(metroGraphData, bundlingSettings, adj);
                    adj.cachedRadiusCost = costCalculator.RadiusCost(adj, adj.Position);
                }

                StationEdgeInfo edgeInfo = metroGraphData.GetIjInfo(node, adj);
                adj.cachedBundleCost -= edgeInfo.cachedBundleCost;

                edgeInfo.cachedBundleCost = costCalculator.BundleCost(node, adj, node.Position);
                node.cachedBundleCost += edgeInfo.cachedBundleCost;
                adj.cachedBundleCost += edgeInfo.cachedBundleCost;
            }
        }

        static internal HitTestBehavior Test(Point pnt, CdtTriangle t) {
            return Cdt.PointIsInsideOfTriangle(pnt, t) ? HitTestBehavior.Stop : HitTestBehavior.Continue;
        }

    }
}