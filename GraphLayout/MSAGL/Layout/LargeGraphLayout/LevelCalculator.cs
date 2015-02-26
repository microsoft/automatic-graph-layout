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
using Microsoft.Msagl.Prototype.Ranking;
using Microsoft.Msagl.Routing;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    internal class LevelCalculator { 

        static void RouteEdgesOnLevels(LgData lgData, IZoomLevelCalculator nodeZoomLevelCalculator, LgLayoutSettings lgLayoutSettings, GeometryGraph mainGeomGraph) {
            AssignEdgesToLevels(lgData, nodeZoomLevelCalculator);

            var edgeInfos = SortEdgeInfosByLevel(lgData);

            foreach (int nodeCountOnLevel in nodeZoomLevelCalculator.LevelNodeCounts) {
                bool finished=RouteEdgesOnLevel(lgData, nodeZoomLevelCalculator, lgLayoutSettings, nodeCountOnLevel, edgeInfos, 
                    mainGeomGraph);
                if (finished)
                    break;
            }
            Console.WriteLine("routing is done");
        }

        static double GetTileSize(GeometryGraph graph, int zoomLevel)
        {
            return Math.Max(graph.Width, graph.Height)/zoomLevel;
        }

        static bool RouteEdgesOnLevel(LgData lgData, IZoomLevelCalculator nodeZoomLevelCalculator,
            LgLayoutSettings lgLayoutSettings, int nodeCountOnLevel, LgEdgeInfo[] edgeInfos, GeometryGraph mainGeomGraph) {
            var zoomLevel = (int) nodeZoomLevelCalculator.SortedLgNodeInfos[nodeCountOnLevel - 1].ZoomLevel;
            var edgeInfosOfLevel = edgeInfos.TakeWhile(ei => ei.ZoomLevel <= zoomLevel).ToList();

            var tmpGraphForRouting = CreateTmpGraphForRouting(lgData, edgeInfosOfLevel, zoomLevel);

            //LayoutAlgorithmSettings.ShowGraph(tmpGraphForRouting);
            
            var level = lgData.AddLevel(zoomLevel);
          
            RouteEdges(lgLayoutSettings, tmpGraphForRouting, lgData, level);
            //lgData.ExtractRailsFromRouting(tmpGraphForRouting.Edges, zoomLevel);
            return edgeInfosOfLevel.Count() == lgData.GeometryEdgesToLgEdgeInfos.Count;
        }

        static void RouteEdges(LgLayoutSettings lgLayoutSettings, 
            GeometryGraph tmpGraphForRouting, 
            LgData lgData,
            LgLevel level) {
            int routedEdges=0;
        Console.WriteLine("\nrouting for level {0}", level.ZoomLevel);
            var router = new SplineRouter(tmpGraphForRouting, lgLayoutSettings.EdgeRoutingSettings.Padding,
                lgLayoutSettings.EdgeRoutingSettings.PolylinePadding, Math.PI/6,
                null) {
                    RouteMultiEdgesAsBundles = false,
                    UseEdgeLengthMultiplier = true,
                    UsePolylineEndShortcutting = false,
                    UseInnerPolylingShortcutting = false,
                    // LineSweeperPorts = GetLineSweeperPorts(tmpGraphForRouting, tileSize),
                    AllowedShootingStraightLines = false,
                    ContinueOnOverlaps = true,
                    CacheCornersForSmoothing = true,
                    ReplaceEdgeByRails =
                        e => {
                            routedEdges++;
                            if (routedEdges % 1000 == 0)
                                Console.Write(".");
                            level.RegisterRailsOfEdge(lgData.GeometryEdgesToLgEdgeInfos[e]);
                        },
                        Bidirectional = true
                };//new BundlingSettings {KeepOverlaps = true, EdgeSeparation = 0, StopAfterShortestPaths = true});
            router.Run();            
            level.CreateRailTree();
            foreach (var edge in tmpGraphForRouting.Edges) {
                level.FillRailDictionaryForEdge(edge);
                edge.Curve = null;
            }
            // level.RunLevelStatistics(lgData.GeometryNodesToLgNodeInfos.Where(e=>e.Value.ZoomLevel<=level.ZoomLevel).Select(p=>p.Key));
        }

        static Point[] GetLineSweeperPorts(GeometryGraph tmpGraphForRouting, double tileSize)
        {
            var rtree =
                RectangleNode<Node>.CreateRectangleNodeOnEnumeration(
                    tmpGraphForRouting.Nodes.Select(n => new RectangleNode<Node>(n, n.BoundingBox)));

            List<Point> list = new List<Point>();
            for (double left = tmpGraphForRouting.Left; left < tmpGraphForRouting.Right; left += tileSize)
                for (double bottom = tmpGraphForRouting.Bottom; bottom < tmpGraphForRouting.Top; bottom+=tileSize)
                {
                    AddTilePoints(rtree, list, left, bottom, tileSize, tmpGraphForRouting.BoundingBox);
                }
            return list.ToArray();
        }

        static void AddTilePoints(RectangleNode<Node> rtree, List<Point> list, double left, double bottom,
            double tileSize, Rectangle boundingBox) {
            const int n = 2; //adding n*n points
            double del = tileSize/(n + 1);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++) {
                    var p = new Point(i*del + left, j*del + bottom);
                    if (!boundingBox.Contains(p)) continue;
                    if (rtree.FirstHitNode(p) != null)
                        continue;
                    list.Add(p);
                }
        }

        static GeometryGraph CreateTmpGraphForRouting(LgData lgData, IEnumerable<LgEdgeInfo> edgeInfosOfLevel, int levelZoom) {
            var tmpGraphForRouting = new GeometryGraph
                {
                    Nodes = FindNodesOfLevelGraph(edgeInfosOfLevel, levelZoom, lgData),
                    Edges = new SimpleEdgeCollection(edgeInfosOfLevel.Select(ei => ei.Edge))
                };
            tmpGraphForRouting.UpdateBoundingBox();
            return tmpGraphForRouting;
        }

        static LgEdgeInfo[] SortEdgeInfosByLevel(LgData lgData) {
            var edgeInfos = lgData.GeometryEdgesToLgEdgeInfos.Values.ToArray();
            Array.Sort(edgeInfos, (a, b) => a.ZoomLevel.CompareTo(b.ZoomLevel));
            return edgeInfos;
        }

        static void AssignEdgesToLevels(LgData lgData, IZoomLevelCalculator nodeZoomLevelCalculator) {
            foreach (int nodeCountOnLevel in nodeZoomLevelCalculator.LevelNodeCounts)
                EdgePicker.SetEdgeInfosZoomLevelsAndIcreaseRanks(lgData, nodeZoomLevelCalculator, nodeCountOnLevel);
        }

        static SimpleNodeCollection FindNodesOfLevelGraph(IEnumerable<LgEdgeInfo> edgeInfos, double levelZoom, LgData lgData) {
             var nodes = new Set<Node>();
            //add all nodes adjacent to the edges
            foreach (var edgeInfo in edgeInfos) {
                var edge = edgeInfo.Edge;
                MaybeScaleDownTheNodeBoundaryAndAddToNodes(levelZoom, lgData, edge.Source, nodes);
                MaybeScaleDownTheNodeBoundaryAndAddToNodes(levelZoom, lgData, edge.Target, nodes);
            }
            return new SimpleNodeCollection(nodes);

        }

        static void MaybeScaleDownTheNodeBoundaryAndAddToNodes(double levelZoom, LgData lgData, Node node, Set<Node> nodes) {
            
//            var nodeInfo = lgData.GeometryNodesToLgNodeInfos[node];
//            if (nodeInfo.ZoomLevel > levelZoom)
//                if (nodeInfo.OriginalCurveOfGeomNode != null)
//                    node.BoundaryCurve =
//                        nodeInfo.OriginalCurveOfGeomNode.Transform(
//                            PlaneTransformation.ScaleAroundCenterTransformation(LgLayoutSettings.PathNodesScale,
//                                                                                node.Center));
            nodes.Insert(node);
        }


        internal static void SetNodeZoomLevelsAndRouteEdgesOnLevels(
            LgData lgData,
            GeometryGraph mainGeometryGraph,
            LgLayoutSettings lgLayoutSettings) {


            //fromDrawingToEdgeInfo = new Dictionary<ICurve, LgEdgeInfo>();
            foreach (var connectedGraph in lgData.ConnectedGeometryGraphs)
                RankTheGraph(lgData, mainGeometryGraph, connectedGraph);

            UpdateRankClusters(lgData);

            var nodeZoomLevelCalculator =
                new DeviceIndependendZoomCalculatorForNodes(node => lgData.GeometryNodesToLgNodeInfos[node],
                                                            mainGeometryGraph, lgLayoutSettings, lgLayoutSettings.MaxNumberNodesPerTile);
            nodeZoomLevelCalculator.Run();
            lgData.SortedLgNodeInfos = nodeZoomLevelCalculator.SortedLgNodeInfos;
            lgData.LevelNodeCounts = nodeZoomLevelCalculator.LevelNodeCounts;
            RouteEdgesOnLevels(lgData, nodeZoomLevelCalculator, lgLayoutSettings, mainGeometryGraph);
        }

        static void UpdateRankClusters(LgData lgData) {
            foreach (var lgInfo in lgData.GeometryNodesToLgNodeInfos.Values) {
                foreach (var cluster in lgInfo.GeometryNode.ClusterParents) {
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