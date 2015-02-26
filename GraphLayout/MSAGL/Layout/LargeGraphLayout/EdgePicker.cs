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
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    internal class EdgePicker {
        readonly LgData lgData;
        readonly IZoomLevelCalculator nodeZoomLevelCalculator;
        readonly int _nodeCountOnLevel;
        internal bool PickShortestPaths;
        internal static void SetEdgeInfosZoomLevelsAndIcreaseRanks(LgData lgData,
                                                    IZoomLevelCalculator nodeZoomLevelCalculator,
                                                    int nodeCountOnLevel) {
            var edgePicker = new EdgePicker(lgData, nodeZoomLevelCalculator, nodeCountOnLevel);
            edgePicker.Run();
        }

        void Run() {
            if (_nodeCountOnLevel == lgData.GeometryNodesToLgNodeInfos.Count) {
                foreach (var e in lgData.GeometryEdgesToLgEdgeInfos.Keys)
                    UpdateEdgeInfoZoomLevel(e);
                return;
            }

            FillShortRoutes();
            if (PickShortestPaths)
                FillShortestPathsFromSomeSelectedNodes();
            //ShowStuff();
        }

        void FillShortestPathsFromSomeSelectedNodes() {
            //those pairs of nodes that are not connected by the short routes
            // will be connected through selected nodes

            int bound = Math.Min(50, _nodeCountOnLevel);
            for (int i = 0; i < bound; i++)
                IncludeEdgesOfShortestPathsFromNodeToOtherNodesOnLevel(nodeZoomLevelCalculator.SortedLgNodeInfos[i]);
        }

        void IncludeEdgesOfShortestPathsFromNodeToOtherNodesOnLevel(LgNodeInfo lgNodeInfo) {
            RunShortestPathsToEveryOtherNode(lgNodeInfo);
            IncludeEdgeFromShortestPathTree(lgNodeInfo);
        }

        void RunShortestPathsToEveryOtherNode(LgNodeInfo lgNodeInfo) {
            var shortestPathToAllOthers = new ShortestPathToAllOthers(lgNodeInfo,
                                                                      lgData.GeometryNodesToLgNodeInfos);
            shortestPathToAllOthers.Run();
        }


        void IncludeEdgeFromShortestPathTree(LgNodeInfo lgNodeInfo) {
            for (int i = 0; i < _nodeCountOnLevel; i++) {
                var otherNodeInfo = nodeZoomLevelCalculator.SortedLgNodeInfos[i];
                if (lgNodeInfo != otherNodeInfo) {
                    var edgeRank = lgNodeInfo.Rank + otherNodeInfo.Rank;
                    foreach (var e in EdgesOfPath(lgNodeInfo, otherNodeInfo))
                        UpdateEdgeInfoZoomLevel(e, edgeRank);
                }
            }
        }


        IEnumerable<Edge> EdgesOfPath(LgNodeInfo a, LgNodeInfo b) {
            if (b.Prev == null) yield break;
            do {
                var e = b.Prev;
                yield return e;
                b = e.Source == b.GeometryNode
                        ? lgData.GeometryNodesToLgNodeInfos[e.Target]
                        : lgData.GeometryNodesToLgNodeInfos[e.Source];
            } while (b != a);
        }

//        void ShowStuff() {
//            var l = new List<DebugCurve>();
//            for (int i = 0; i <= levelLastNodeIndex; i++) {
//                var node = nodeZoomLevelCalculator.SortedLgNodeInfos[i].GeometryNode;
//                l.Add(new DebugCurve(200, 1, "green", node.BoundaryCurve));
//            }
//
//            foreach (
//                var e in lgData.GeometryEdgesToLgEdgeInfos.Where(e => e.Value.ZoomLevel <= zoomLevel).Select(g => g.Key)
//                )
//                l.Add(new DebugCurve(100, 1, "brown", new LineSegment(e.Source.Center, e.Target.Center)));
//
//            foreach (
//                var e in lgData.GeometryEdgesToLgEdgeInfos.Where(e => e.Value.ZoomLevel > zoomLevel).Select(g => g.Key))
//                l.Add(new DebugCurve(20, 0.5, "green", new LineSegment(e.Source.Center, e.Target.Center)));
//
//            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
//
//        }


        readonly int zoomLevel;
     
        EdgePicker(LgData lgData, IZoomLevelCalculator nodeZoomLevelCalculator,
                   int nodeCountOnLevel) {
            this.lgData = lgData;
            this.nodeZoomLevelCalculator = nodeZoomLevelCalculator;
            this._nodeCountOnLevel = nodeCountOnLevel;
            zoomLevel = (int) nodeZoomLevelCalculator.SortedLgNodeInfos[nodeCountOnLevel - 1].ZoomLevel;
        }

        void FillShortRoutes() {
            //creating one edge long routes between the nodes of the level
             for (int i = 0; i < _nodeCountOnLevel; i++)
                FillShortRoutesOfNode(nodeZoomLevelCalculator.SortedLgNodeInfos[i].GeometryNode);

        }

        void FillShortRoutesOfNode(Node node) {
            foreach (var e in node.OutEdges)
                TryPickingEdge(e);
        }

        void TryPickingEdge(Edge edge) {
            if (lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel <= zoomLevel)
                //connected to a node from the same level
                UpdateEdgeInfoZoomLevel(edge);
        }

        void UpdateEdgeInfoZoomLevel(Edge edge) {
            var sourceNodeInfo = lgData.GeometryNodesToLgNodeInfos[edge.Source];
            var targetNodeInfo = lgData.GeometryNodesToLgNodeInfos[edge.Target];
            UpdateEdgeInfoZoomLevel(edge, sourceNodeInfo.Rank + targetNodeInfo.Rank);
        }

        void UpdateEdgeInfoZoomLevel(Edge edge, double edgeRank) {
            var edgeInfo = lgData.GeometryEdgesToLgEdgeInfos[edge];
            TryToDecreaseZoomLevel(edgeInfo);
            TryToIncreaseRank(edgeInfo, edgeRank);
        }

        static void TryToIncreaseRank(LgEdgeInfo edgeInfo, double edgeRank) {
            if (edgeInfo.Rank < edgeRank)
                edgeInfo.Rank = edgeRank;
        }

        void TryToDecreaseZoomLevel(LgEdgeInfo edgeInfo) {
            if (edgeInfo.ZoomLevel > zoomLevel)
                edgeInfo.ZoomLevel = zoomLevel;
        }
    }
}