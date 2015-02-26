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
using System.Threading.Tasks;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Prototype.Ranking;
using Microsoft.Msagl.Routing;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    /// <summary>
    /// enables to interactively explore a large graph
    /// </summary>
    public class LgInteractor {
        readonly GeometryGraph mainGeometryGraph;
        readonly LgLayoutSettings lgLayoutSettings;
        RailGraph railGraph;
        Rectangle visibleRectangle;
        readonly Set<LgNodeInfo> visibleNodeSet = new Set<LgNodeInfo>();
        RectangleNode<LgNodeInfo> lgNodeHierarchy;
        double CurrentZoomLevel { get; set; }
        readonly LgData lgData;
        readonly CancelToken cancelToken;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="geometryGraph"></param>
        /// <param name="lgLayoutSettings"></param>
        /// <param name="cancelToken"></param>
        public LgInteractor(GeometryGraph geometryGraph, LgLayoutSettings lgLayoutSettings, CancelToken cancelToken) {
            mainGeometryGraph = geometryGraph;
            this.lgLayoutSettings = lgLayoutSettings;
            this.cancelToken = cancelToken;
            if (geometryGraph.LgData == null) {
                lgData = new LgData(geometryGraph) {
                    GeometryNodesToLgNodeInfos = lgLayoutSettings.GeometryNodesToLgNodeInfos
                };

                geometryGraph.LgData = lgData;
            }
            else {
                _initFromPrecomputedLgData = true;
                lgData = geometryGraph.LgData;
                lgLayoutSettings.GeometryNodesToLgNodeInfos = lgData.GeometryNodesToLgNodeInfos;
            }
        }

        /// <summary>
        /// does the initialization
        /// </summary>
        public void Initialize() {
            if (_initFromPrecomputedLgData) {
                InitOnPrecomputedLgData();
                return;
            }

#if DEBUG && TEST_MSAGL
            mainGeometryGraph.SetDebugIds();
#endif

            CreateConnectedComponentsAndLayoutTheWholeGraph();
#if (!SILVERLIGHT && !SHARPKIT)
            Timer timer=new Timer();
            timer.Start();
#endif
            LevelCalculator.SetNodeZoomLevelsAndRouteEdgesOnLevels(lgData, mainGeometryGraph, lgLayoutSettings);
            TestZoomLevels();
            //    CalculateEdgeZoomLevels();
            //    Console.WriteLine("nodes {0} edges {1}", MainGeometryGraph.Nodes.Count(), MainGeometryGraph.Edges.Count());
            lgNodeHierarchy =
                RectangleNode<LgNodeInfo>.CreateRectangleNodeOnEnumeration(
                    lgData.GeometryNodesToLgNodeInfos.Values.Where(n => !(n.GeometryNode is Cluster)).Select(
                        lginfo => new RectangleNode<LgNodeInfo>(lginfo, lginfo.BoundingBox)));
            railGraph = new RailGraph();
#if (!SILVERLIGHT && !SHARPKIT)
            timer.Stop();
            Console.WriteLine("levels calculated for {0}", timer.Duration);

            if(lgLayoutSettings.ExitAfterInit)
                Environment.Exit(0);
#endif
        }

        void InitOnPrecomputedLgData() {
            lgNodeHierarchy =
            RectangleNode<LgNodeInfo>.CreateRectangleNodeOnEnumeration(
                lgData.GeometryNodesToLgNodeInfos.Values.Where(n => !(n.GeometryNode is Cluster)).Select(
                    lginfo => new RectangleNode<LgNodeInfo>(lginfo, lginfo.BoundingBox)));
            railGraph = new RailGraph();
        }

        void TestZoomLevels() {
            foreach (var cluster in mainGeometryGraph.RootCluster.AllClustersDepthFirstExcludingSelf()) {
                foreach (var n in cluster.Clusters.Concat(cluster.Nodes)) {
                    Debug.Assert(lgData.GeometryNodesToLgNodeInfos[cluster].Rank >=
                                 lgData.GeometryNodesToLgNodeInfos[n].Rank);
                }

            }
        }





        internal Interval MaximalEdgeZoomLevelInterval { get; set; }








        //        void TestVisibleTogether() {
        //            for (int i = 0; i < geometryGraph.Nodes.Count - 1; i++) {
        //                var a = GeometryNodesToLgNodes[geometryGraph.Nodes[i]];
        //                for (int j = i + 1; j < geometryGraph.Nodes.Count; j++) {
        //                    var b = GeometryNodesToLgNodes[geometryGraph.Nodes[j]];
        //                    string color = VisibleTogether(a, b) ? "green" : "red";
        //                    var l = new List<DebugCurve>();
        //                    foreach (var n in geometryGraph.Nodes) {
        //                        if(n!=a.GeometryNode && n!=b.GeometryNode)
        //                        l.Add(new DebugCurve(100, 1, "black", n.BoundaryCurve));
        //                        else
        //                            l.Add(new DebugCurve(3, color, n.BoundaryCurve));
        //
        //                    }
        //
        //                    l.Add(new DebugCurve(5,color, a.DominatedRect.Perimeter()));
        //                    l.Add(new DebugCurve(5, color, b.DominatedRect.Perimeter()));
        //                    LayoutAlgorithmSettings.ShowDebugCurves(l.ToArray());
        //                }
        //            }
        //        }


        void FillGeometryNodeToLgInfosTables() {
            foreach (
                var node in
                    mainGeometryGraph.Nodes.Concat(mainGeometryGraph.RootCluster.AllClustersWideFirstExcludingSelf()))
                lgData.GeometryNodesToLgNodeInfos[node] = new LgNodeInfo(node);
            foreach (var edge in mainGeometryGraph.Edges) {
                lgData.GeometryEdgesToLgEdgeInfos[edge] = new LgEdgeInfo(edge);
            }
        }

        void CreateConnectedComponentsAndLayoutTheWholeGraph() {
            CreateConnectedGraphs();
            if (lgLayoutSettings.NeedToLayout) {
                if (RunInParallel)
                    Parallel.ForEach(lgData.ConnectedGeometryGraphs, new ParallelOptions(), LayoutAndPadOneComponent);
                else
                    foreach (var connectedGraph in lgData.ConnectedGeometryGraphs)
                        LayoutAndPadOneComponent(connectedGraph);

                var rect = MdsGraphLayout.PackGraphs(lgData.ConnectedGeometryGraphs, lgLayoutSettings);
                mainGeometryGraph.BoundingBox = rect;

            }
            else
                foreach (var graph in lgData.ConnectedGeometryGraphs)
                    graph.UpdateBoundingBox();

            //after this moment  MainGeometryGraph still contains correct pointers to the nodes
            FillGeometryNodeToLgInfosTables();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RunInParallel = true;

        bool _initFromPrecomputedLgData;

        void LayoutAndPadOneComponent(GeometryGraph connectedGraph)
        {
            LayoutOneComponent(connectedGraph);
            var box = connectedGraph.BoundingBox;
            box.Pad(lgLayoutSettings.NodeSeparation/2);
        }

        void CreateConnectedGraphs() {
            Dictionary<Node, int> nodeToIndex;
            var listOfNodes = CreateNodeListForBasicGraph(out nodeToIndex);
            var basicGraph = new BasicGraph<SimpleIntEdge>(GetSimpleIntEdges(nodeToIndex), listOfNodes.Count);
            var comps = ConnectedComponentCalculator<SimpleIntEdge>.GetComponents(basicGraph);
            foreach (var comp in comps)
                lgData.AddConnectedGeomGraph(GetConnectedSubgraph(comp, listOfNodes));

        }

        GeometryGraph GetConnectedSubgraph(IEnumerable<int> comp, List<Node> nodeList) {
            var edges = new List<Edge>();
            var nodes = new List<Node>();
            var geomGraph = new GeometryGraph();
            foreach (var i in comp) {
                var node = nodeList[i];
                var cluster = node as Cluster;
                if (cluster != null) {
                    if (cluster.ClusterParents.First() == mainGeometryGraph.RootCluster) {
                        //MainGeometryGraph.RootCluster.RemoveCluster(cluster);
                        geomGraph.RootCluster.AddCluster(cluster);
                    }
                }
                else {
                    nodes.Add(node);
                }

                foreach (var edge in node.OutEdges.Concat(node.SelfEdges)) {
                    Debug.Assert(!edges.Contains(edge));
                    edges.Add(edge);
                }
            }
            geomGraph.Edges = new SimpleEdgeCollection(edges);
            geomGraph.Nodes = new SimpleNodeCollection(nodes);
            return geomGraph;
        }

        List<Node> CreateNodeListForBasicGraph(out Dictionary<Node, int> nodeToIndex) {
            var list = new List<Node>();
            nodeToIndex = new Dictionary<Node, int>();

            foreach (var node in mainGeometryGraph.Nodes) {
                nodeToIndex[node] = list.Count;
                list.Add(node);
            }
            foreach (var node in mainGeometryGraph.RootCluster.AllClustersDepthFirstExcludingSelf()) {
                nodeToIndex[node] = list.Count;
                list.Add(node);
            }
            return list;
        }

        List<SimpleIntEdge> GetSimpleIntEdges(Dictionary<Node, int> nodeToIndex) {
            var list = new List<SimpleIntEdge>();
            foreach (var edge in mainGeometryGraph.Edges)
                list.Add(new SimpleIntEdge {Source = nodeToIndex[edge.Source], Target = nodeToIndex[edge.Target]});

            foreach (var cluster in mainGeometryGraph.RootCluster.AllClustersDepthFirstExcludingSelf()) {
                foreach (var child in cluster.Clusters)
                    list.Add(new SimpleIntEdge {Source = nodeToIndex[cluster], Target = nodeToIndex[child]});

                foreach (var child in cluster.Nodes)
                    list.Add(new SimpleIntEdge {Source = nodeToIndex[cluster], Target = nodeToIndex[child]});
            }
            return list;
        }

        void LayoutOneComponent(GeometryGraph component) {
            PrepareGraphForLayout(component);
            if (component.RootCluster.Clusters.Any()) {
                var layoutSettings = new SugiyamaLayoutSettings {
                    FallbackLayoutSettings =
                        new FastIncrementalLayoutSettings {
                            AvoidOverlaps = true
                        },
                    NodeSeparation = lgLayoutSettings.NodeSeparation,
                    LayerSeparation = lgLayoutSettings.NodeSeparation,
                    EdgeRoutingSettings = lgLayoutSettings.EdgeRoutingSettings,
                    LayeringOnly = true
                };
                var initialBc = new InitialLayoutByCluster(component, a => layoutSettings);
                initialBc.Run();
            }
            else
                LayoutHelpers.CalculateLayout(component, GetMdsLayoutSettings(), cancelToken);

            var box = component.BoundingBox;
            box.Pad(lgLayoutSettings.NodeSeparation/2);
            component.BoundingBox = box;
        }


        static void PrepareGraphForLayout(GeometryGraph connectedGraph) {
            foreach (var cluster in connectedGraph.RootCluster.AllClustersDepthFirst()) {
                if (cluster.RectangularBoundary == null)
                    cluster.RectangularBoundary = new RectangularClusterBoundary();
            }

            foreach (var edge in connectedGraph.Edges) {
                if (edge.SourcePort == null) {
                    var e = edge;
#if SHARPKIT // Lambdas bind differently in JS
                    edge.SourcePort = ((Func<Edge,RelativeFloatingPort>)(ed => new RelativeFloatingPort(() => ed.Source.BoundaryCurve,
                        () => ed.Source.Center)))(e);
#else
                    edge.SourcePort = new RelativeFloatingPort(() => e.Source.BoundaryCurve,
                                                               () => e.Source.Center);
#endif
                }
                if (edge.TargetPort == null) {
                    var e = edge;
#if SHARPKIT // Lambdas bind differently in JS
                    edge.TargetPort = ((Func<Edge, RelativeFloatingPort>)(ed => new RelativeFloatingPort(() => ed.Target.BoundaryCurve,
                        () => ed.Target.Center)))(e);
#else
                    edge.TargetPort = new RelativeFloatingPort(() => e.Target.BoundaryCurve,
                                                               () => e.Target.Center);
#endif
                }
            }
        }


        MdsLayoutSettings GetMdsLayoutSettings() {
            var settings = new MdsLayoutSettings {
                EdgeRoutingSettings = {KeepOriginalSpline = true, EdgeRoutingMode = EdgeRoutingMode.None}
            };
            settings.ScaleX *= 3;
            settings.ScaleY *= 3;
            return settings;
        }


        void FindVisibleRails() {
            railGraph.Rails.Clear();
            railGraph.Rails.InsertRange(lgData.GetSetOfVisibleRails(visibleRectangle, CurrentZoomLevel));
            railGraph.VisibleEdges.Clear();
            railGraph.VisibleEdges.InsertRange(railGraph.Rails.Select(r => r.TopRankedEdgeInfoOfTheRail.Edge));
        }


        internal enum ZoomRequest {
            NoChange,
            ZoomIn,
            ZoomOut
        };







        //        void FixArrowHeadLengths() {
        //            const double arrowheadRatioToBoxDiagonal=0.3; 
        //            var maximalArrowheadLength = lgLayoutSettings.MaximalArrowheadLength();
        //            
        //            foreach (Edge edge in OGraph.Edges) {
        //                var edgeInfo = this.lgData.GeometryEdgesToLgEdgeInfos[edge];
        //
        //                if (edge.EdgeGeometry.SourceArrowhead != null)
        //                    edge.EdgeGeometry.SourceArrowhead.Length =
        //                        Math.Min(Math.Min(edgeInfo.OriginalSourceArrowheadLength, maximalArrowheadLength),
        //                                 edge.Source.BoundingBox.Diagonal*arrowheadRatioToBoxDiagonal);
        //                if (edge.EdgeGeometry.TargetArrowhead != null)
        //                    edge.EdgeGeometry.TargetArrowhead.Length =
        //                        Math.Min(Math.Min(edgeInfo.OriginalTargetArrowheadLength, maximalArrowheadLength),
        //                                 edge.Target.BoundingBox.Diagonal*arrowheadRatioToBoxDiagonal);
        //            }
        //        }

        internal static GeometryGraph CreateClusteredSubgraphFromFlatGraph(GeometryGraph subgraph,
                                                                           GeometryGraph mainGeometryGraph) {
            if (mainGeometryGraph.RootCluster.Clusters.Any() == false) return subgraph;
            var ret = new GeometryGraph();
            var originalNodesToNewNodes = MapSubgraphNodesToNewNodesForRouting(subgraph);
            ReplicateClusterStructure(subgraph, originalNodesToNewNodes);
            AddNewNodeAndClustersToTheNewGraph(originalNodesToNewNodes, ret);


            foreach (var edge in subgraph.Edges) {
                var ns = originalNodesToNewNodes[edge.Source];
                var nt = originalNodesToNewNodes[edge.Target];
                ret.Edges.Add(new Edge(ns, nt) {
                    EdgeGeometry = edge.EdgeGeometry,
                    SourcePort = null,
                    TargetPort = null
                });
            }

            foreach (var kv in originalNodesToNewNodes) {
                var newNode = kv.Value;
                var cluster = newNode as Cluster;
                if (cluster != null) {
                    var oldNode = kv.Key;
                    if (oldNode.BoundaryCurve != newNode.BoundaryCurve) {
                        oldNode.BoundaryCurve = newNode.BoundaryCurve;
                        oldNode.RaiseLayoutChangeEvent(null);
                    }
                }
            }
            return ret;
            //LayoutAlgorithmSettings.ShowGraph(ret);
        }

        /*
                void SetClusterBoundary(Cluster cluster) {
                    double radX, radY;
                    GetRadXRadYFromClusterBoundary(cluster, out radX, out radY);
                    if (!cluster.Clusters.Any() && !cluster.Nodes.Any()) {
                        var box = new Rectangle(cluster.Center);
                        box.Pad(5);//todo: 5???
                        cluster.BoundaryCurve = new RoundedRect(box, radX, radY);
                    } else {
                        var box = Rectangle.CreateAnEmptyBox();
                        foreach (var cl in cluster.Clusters) {
                            SetClusterBoundary(cl);
                            box.Add(cl.BoundaryCurve.BoundingBox);
                        }
                        foreach (var node in cluster.Nodes) {
                            box.Add(node.BoundaryCurve.BoundingBox);
                        }
                        //cannot use cluster.BoundingBox here, since it is recursive
                        box.Pad(lgLayoutSettings.ClusterPadding);
                        cluster.BoundaryCurve = new RoundedRect(box, radX, radY);
                    }
                }
        */

        /*
                void GetRadXRadYFromClusterBoundary(Cluster cluster, out double radX, out double radY) {
                    var roundedRect = cluster.BoundaryCurve as RoundedRect;
                    if (roundedRect != null) {
                        radX = roundedRect.RadiusX;
                        radY = roundedRect.RadiusY;
                    } else
                        radX = radY = 5;
                }
        */

        static void AddNewNodeAndClustersToTheNewGraph(Dictionary<Node, Node> onodesToNewNodes, GeometryGraph ret) {
            foreach (var newNode in onodesToNewNodes.Values) {
                var cl = newNode as Cluster;
                if (cl == null)
                    ret.Nodes.Add(newNode);
                else {
                    if (!cl.ClusterParents.Any())
                        ret.RootCluster.AddCluster(cl);
                }
            }
        }

        static void ReplicateClusterStructure(GeometryGraph geometryGraph, Dictionary<Node, Node> onodesToNewNodes) {
            foreach (var onode in geometryGraph.Nodes)
                foreach (var oclparent in onode.ClusterParents) {
                    Node newParent;
                    if (onodesToNewNodes.TryGetValue(oclparent, out newParent))
                        ((Cluster) newParent).AddNode(onodesToNewNodes[onode]);
                }
        }

        /*
                bool IsRootCluster(Cluster oclparent) {
                    return !oclparent.ClusterParents.Any();
                }
        */

        static Dictionary<Node, Node> MapSubgraphNodesToNewNodesForRouting(GeometryGraph geometryGraph) {
            var onodesToNewNodes = new Dictionary<Node, Node>();
            foreach (var oNode in geometryGraph.Nodes) {
                var cluster = oNode as Cluster;

                onodesToNewNodes[oNode] = cluster != null
                                              ? new Cluster() {
                                                  CollapsedBoundary = cluster.CollapsedBoundary,
                                                  BoundaryCurve = oNode.BoundaryCurve,
#if DEBUG && TEST_MSAGL
                                                                DebugId = oNode.DebugId
#endif
                                              }
                                              : new Node(oNode.BoundaryCurve);
            }
            return onodesToNewNodes;
        }


        /// <summary>
        /// this graph is currently visible set of nodes and pieces of edges 
        /// </summary>
        public RailGraph RailGraph {
            get { return railGraph; }
        }

        /// <summary>
        /// 
        /// </summary>
        public IDictionary<Edge, LgEdgeInfo> GeometryEdgesToLgEdgeInfos {
            get { return lgData.GeometryEdgesToLgEdgeInfos; }

        }

        void AddFullyVisibleNodeToRailGraph(LgNodeInfo nodeInfo) {
            //      Debug.Assert(nodeInfo.IsOpen == false);
            nodeInfo.Kind = LgNodeInfoKind.FullyVisible;
            visibleNodeSet.Insert(nodeInfo);
            nodeInfo.Scale = 1;
            AddAllParentsToOpenNodes(nodeInfo.GeometryNode);
        }

        double FigureOutSatelliteScale(LgNodeInfo satellite) {
            var lower = satellite.ZoomLevel/lgLayoutSettings.SattelliteZoomFactor;
            var upper = satellite.ZoomLevel;
            return Math.Sqrt(0.8*(CurrentZoomLevel - lower)/(upper - lower) + 0.2);
            //            Debug.Assert(0 <= a && a <= 1);          
            //            var ret = a + (1.0 - a)*0.1;
            //            return ret;
        }


        /*
        void AdjustNodeBoundariesAccordingToRanks(Node[] nodeArray, double[] ranks) {
            Array.Sort(ranks, nodeArray);
            Array.Reverse(nodeArray);
            Array.Reverse(ranks);
            double sum=0.0;
            double middle = 0.5;
            int i = 0;
            for (; i < ranks.Length; i++)
                sum += ranks[i];

            var halfSum = 0.5*sum;
            sum = 0;
            for(i=0;i<ranks.Length;i++){
               sum += ranks[i];
                if(sum>=halfSum){
                    middle = ranks[i];
                    break;
                }
            }
            
            
            //linear mapping from ranks; all before index i are enlarlged
            //all after i are shrinked
//enlarging from 1 to 2
            //middle->1
            //ranks[0]->2

            double k = 1/(ranks[0] - middle);
            double b = 1 - k*middle;
            int j;
            for (j = 0; j <= i; j++) {
                var scale = k*ranks[j] + b;
                var node = nodeArray[j];
                node.BoundaryCurve =
                    node.BoundaryCurve.Transform(PlaneTransformation.ScaleAroundCenterTransformation(scale,
                                                                                                     node.Center));                
            }

            //ranks[j+1]->1
            //ranks[last]->0.3
            const double lowScale = 0.3;
            k = (1-lowScale) / (ranks[j] - ranks[ranks.Length-1]);
            b = 1 - k * ranks[j];
            for (; j < ranks.Length; j++)
            {
                var scale = k * ranks[j] + b;
                var node = nodeArray[j];
                node.BoundaryCurve =
                    node.BoundaryCurve.Transform(PlaneTransformation.ScaleAroundCenterTransformation(scale,
                                                                                                     node.Center));                
            }

        }
*/


        /// <summary>
        /// 
        /// </summary>
        public void RunOnViewChange() {
            visibleRectangle = Rectangle.Intersect(lgLayoutSettings.ClientViewportMappedToGraph,
                                                   mainGeometryGraph.BoundingBox);

            //            if (MainGeometryGraph.Edges.Count == 33) {
            //                LayoutAlgorithmSettings.ShowDebugCurves(
            //                    new DebugCurve("red", MainGeometryGraph.BoundingBox.Perimeter()),
            //                    new DebugCurve("blue", visibleRectangle.Perimeter()));
            //                LayoutAlgorithmSettings.ShowGraph(clusterOGraph);
            //            }
            if (visibleRectangle.IsEmpty) return; //probably we should avoid this situation

            CurrentZoomLevel = GetZoomFactorToTheGraph();
            FillRailGraph();

            //            if (MainGeometryGraph.Edges.Count == 33) 
            //                LayoutAlgorithmSettings.ShowGraph(clusterOGraph);

            lgLayoutSettings.OnViewerChangeTransformAndInvalidateGraph(null);
        }

        internal double GetZoomFactorToTheGraph() {
            return lgLayoutSettings.TransformFromGraphToScreen()[0, 0]/FitFactor();
        }

        double FitFactor() {
            var vp = lgLayoutSettings.ClientViewportFunc();
            return Math.Min(vp.Width/mainGeometryGraph.Width, vp.Height/mainGeometryGraph.Height);
        }


        void FillRailGraph() {
            ProcessOpenNodesAndSatelliteNodes();
            FindVisibleRails();
            RegisterPathNodes();

            railGraph.Nodes.Clear();
            foreach (var lgInfo in visibleNodeSet)
                railGraph.Nodes.Insert(lgInfo.GeometryNode);

        }


        void ProcessOpenNodesAndSatelliteNodes() {
            ClearOpenAndSatelliteSets();
            FillOpenNodeAdnSatelliteSets();
        }



        void ClearOpenAndSatelliteSets() {
            foreach (var lgNodeInfo in visibleNodeSet)
                lgNodeInfo.Kind = LgNodeInfoKind.OutOfView;
            visibleNodeSet.Clear();
        }

        void FillOpenNodeAdnSatelliteSets() {
            lgLayoutSettings.BackgroundImageIsHidden = true;
            foreach (var nodeInfo in lgNodeHierarchy.GetNodeItemsIntersectingRectangle(visibleRectangle))
                AddVisibleNode(nodeInfo);
        }

        void AddVisibleNode(LgNodeInfo nodeInfo) {
            if (nodeInfo.ZoomLevel <= CurrentZoomLevel)
                AddFullyVisibleNodeToRailGraph(nodeInfo);
            else if (nodeInfo.ZoomLevel <= CurrentZoomLevel*lgLayoutSettings.SattelliteZoomFactor) {
                visibleNodeSet.Insert(nodeInfo);
                nodeInfo.Kind = LgNodeInfoKind.Satellite;                
                nodeInfo.Scale = FigureOutSatelliteScale(nodeInfo);
                AddAllParentsToOpenNodes(nodeInfo.GeometryNode);
                //                foreach (var cluster in nodeInfo.GeometryNode.AllClusterAncestors) {
                //                    if (cluster == geometryGraph.RootCluster) continue;
                //                    var clusterInfo = lgData.GeometryNodesToLgNodeInfos[cluster];
                //                    if (clusterInfo.ZoomLevel <= CurrentZoomLevel) {
                //                        OpenNodeInfosSet.Insert(clusterInfo);
                //                        clusterInfo.IsOpen = true;
                //                    } else {
                //                        SatelliteSet.Insert(clusterInfo);
                //                    }
                //                }
            }
            else { lgLayoutSettings.BackgroundImageIsHidden = false; }
            
        }

        void RegisterPathNodes() {
            foreach (var e in railGraph.VisibleEdges)
                RegisterSourceAndTargetOfPathEdgeAsPathNodes(e);
        }

        void RegisterSourceAndTargetOfPathEdgeAsPathNodes(Edge edge) {
            RegisterPathEdgeNode(edge.Source);
            RegisterPathEdgeNode(edge.Target);
        }

        void RegisterPathEdgeNode(Node node) {
            LgNodeInfo nodeInfo = lgData.GeometryNodesToLgNodeInfos[node];
            if (nodeInfo.Kind == LgNodeInfoKind.FullyVisible)
                return;
            if (node is Cluster) {
                AddFullyVisibleNodeToRailGraph(lgData.GeometryNodesToLgNodeInfos[node]);
                return;
            }
           
            nodeInfo.Kind = LgNodeInfoKind.PathNode;
            visibleNodeSet.Insert(nodeInfo);
            AddAllParentsToOpenNodes(node);

        }

        void AddAllParentsToOpenNodes(Node node) {
            foreach (var cluster in node.AllClusterAncestors) {
                if (cluster == mainGeometryGraph.RootCluster) continue;
                var clusterLgInfo = lgData.GeometryNodesToLgNodeInfos[cluster];
                clusterLgInfo.Kind = LgNodeInfoKind.FullyVisible;
                visibleNodeSet.Insert(clusterLgInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rail"></param>
        internal void HighlightEdgesPassingThroughTheRail(Rail rail) {
            lgData.HighlightEdgesPassingThroughRail(rail);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="edges"></param>
        internal void HighlightEdges(IEnumerable<Edge> edges)
        {
            lgData.HighlightEdges(edges.ToList());
        }



        internal bool TheRailLevelIsTooHigh(Rail rail) {           
            return lgData.GetRelevantEdgeLevel(rail.ZoomLevel) > lgData.GetRelevantEdgeLevel(GetZoomFactorToTheGraph());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double GetMaximalZoomLevel() {
            if (lgData == null)
                return 1;
            return lgData.GetMaximalZoomLevel();
        }

        internal void PutOffEdgesPassingThroughTheRail(Rail rail) {
            lgData.PutOffEdgesPassingThroughTheRail(rail);
        }

        internal void PutOffEdges(List<Edge> edges) {
            lgData.PutOffEdges(edges);
        }
    }
}
