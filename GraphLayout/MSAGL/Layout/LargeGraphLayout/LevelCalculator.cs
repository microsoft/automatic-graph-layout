using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Prototype.Ranking;
using Microsoft.Msagl.Routing;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    internal class LevelCalculator { 

        static void AssignEdges(LgData lgData, IZoomLevelCalculator nodeZoomLevelCalculator) {
            AssignEdgesToLevels(lgData, nodeZoomLevelCalculator);
            var edgeInfos = SortEdgeInfosByZoomLevel(lgData);

            foreach (int nodeCountOnLevel in nodeZoomLevelCalculator.LevelNodeCounts) {
                bool finished = AddLevel(lgData, nodeZoomLevelCalculator, nodeCountOnLevel, edgeInfos);
                if (finished)
                    break;
            }
        }

        static bool AddLevel(LgData lgData, IZoomLevelCalculator nodeZoomLevelCalculator, int nodeCountOnLevel, LgEdgeInfo[] edgeInfos) {
            var zoomLevel = (int) nodeZoomLevelCalculator.SortedLgNodeInfos[nodeCountOnLevel - 1].ZoomLevel;
            var edgeInfosOfLevel = edgeInfos.TakeWhile(ei => ei.ZoomLevel <= zoomLevel).ToList();
            lgData.AddLevel();
            return edgeInfosOfLevel.Count() == lgData.GeometryEdgesToLgEdgeInfos.Count;
        }

        static LgEdgeInfo[] SortEdgeInfosByZoomLevel(LgData lgData) {
            var edgeInfos = lgData.GeometryEdgesToLgEdgeInfos.Values.ToArray();
            Array.Sort(edgeInfos, (a, b) => a.ZoomLevel.CompareTo(b.ZoomLevel));
            return edgeInfos;
        }

        static void AssignEdgesToLevels(LgData lgData, IZoomLevelCalculator nodeZoomLevelCalculator) {
            foreach (int nodeCountOnLevel in nodeZoomLevelCalculator.LevelNodeCounts)
                EdgePicker.SetEdgeInfosZoomLevelsAndIcreaseRanks(lgData, nodeZoomLevelCalculator, nodeCountOnLevel);
        }


        internal static void SetEdgesOnLevels(
            LgData lgData,
            GeometryGraph mainGeometryGraph,
            LgLayoutSettings lgLayoutSettings)
        {
            
            var nodeZoomLevelCalculator =
                new DeviceIndependendZoomCalculatorForNodes(node => lgData.GeometryNodesToLgNodeInfos[node],
                                                            mainGeometryGraph, lgLayoutSettings, lgLayoutSettings.MaxNumberOfNodesPerTile);

            nodeZoomLevelCalculator.RunAfterFlow(lgData);
            lgData.SortedLgNodeInfos = nodeZoomLevelCalculator.SortedLgNodeInfos;
            lgData.LevelNodeCounts = nodeZoomLevelCalculator.LevelNodeCounts;
        }

        internal static void SetNodeZoomLevelsAndRouteEdgesOnLevels(
            LgData lgData,
            GeometryGraph mainGeometryGraph,
            LgLayoutSettings lgLayoutSettings) {

            var nodeZoomLevelCalculator =
                new DeviceIndependendZoomCalculatorForNodes(node => lgData.GeometryNodesToLgNodeInfos[node],
                                                            mainGeometryGraph, lgLayoutSettings, lgLayoutSettings.MaxNumberOfNodesPerTile);
            //jyoti this is the place where you might want to bound the theoretical zoom level
            nodeZoomLevelCalculator.Run();
            lgData.SortedLgNodeInfos = nodeZoomLevelCalculator.SortedLgNodeInfos;
            lgData.LevelNodeCounts = nodeZoomLevelCalculator.LevelNodeCounts;
            AssignEdges(lgData, nodeZoomLevelCalculator);
        }

        internal static void RankGraph(LgData lgData, GeometryGraph mainGeometryGraph) {
//fromDrawingToEdgeInfo = new Dictionary<ICurve, LgEdgeInfo>();
            foreach (var connectedGraph in lgData.ConnectedGeometryGraphs)
                RankTheGraph(lgData, mainGeometryGraph, connectedGraph);

            UpdateRanksOfClusters(lgData);
        }

        static void UpdateRanksOfClusters(LgData lgData) {
            foreach (var lgInfo in lgData.GeometryNodesToLgNodeInfos.Values) {
                var cluster = lgInfo.GeometryNode.ClusterParent;
                if (cluster != null) {
                    LgNodeInfo clusterLgInfo;
                    if (lgData.GeometryNodesToLgNodeInfos.TryGetValue(cluster, out clusterLgInfo))
                        if (clusterLgInfo.Rank < lgInfo.Rank)
                            clusterLgInfo.Rank = lgInfo.Rank;
                }
            }
        }


        static void RankTheGraph(LgData lgData,
                                          GeometryGraph mainGeometryGraph, GeometryGraph geomGraph) {

            var nodeArray = geomGraph.Nodes.ToArray();
            var flatGraph = new GeometryGraph {
                Nodes = new SimpleNodeCollection(nodeArray),
                Edges =
                    new SimpleEdgeCollection(
                        geomGraph.Edges.Where(e => !(e.Source is Cluster) && !(e.Target is Cluster)))
            };

            var pageRank = Centrality.PageRank(flatGraph, 0.85, false);
            double normalizer = (double)geomGraph.Nodes.Count/mainGeometryGraph.Nodes.Count;
            for (int i = 0; i < nodeArray.Length; i++) {
                var node = nodeArray[i];
                Debug.Assert(node != mainGeometryGraph.RootCluster);
                lgData.GeometryNodesToLgNodeInfos[node].Rank = normalizer*pageRank[i];
            }
        }
    }
}