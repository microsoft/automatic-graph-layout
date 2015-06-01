using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.LargeGraphLayout.NodeRailLevelCalculator;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Layout.OverlapRemovalFixedSegments;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Miscellaneous.ConstrainedSkeleton;
using Microsoft.Msagl.Miscellaneous.RegularGrid;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Visibility;
using SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    /// <summary>
    ///     enables to interactively explore a large graph
    /// </summary>
    public class LgInteractor {
        const bool ShrinkEdgeLengths = true;
        readonly bool _initFromPrecomputedLgData;
        readonly LgData _lgData;
        readonly LgLayoutSettings _lgLayoutSettings;
        readonly CancelToken _cancelToken;
        readonly GeometryGraph _mainGeometryGraph;

        /// <summary>
        /// </summary>
        bool _runInParallel = true;

        RailGraph _railGraph;
        Rectangle _visibleRectangle;

        /// <summary>
        ///     constructor
        /// </summary>
        /// <param name="geometryGraph"></param>
        /// <param name="lgLayoutSettings"></param>
        /// <param name="cancelToken"></param>
        public LgInteractor(GeometryGraph geometryGraph, LgLayoutSettings lgLayoutSettings, CancelToken cancelToken) {
            _mainGeometryGraph = geometryGraph;
            _lgLayoutSettings = lgLayoutSettings;
            _cancelToken = cancelToken;
            if (geometryGraph.LgData == null) {
                _lgData = new LgData(geometryGraph)
                {
                    GeometryNodesToLgNodeInfos = lgLayoutSettings.GeometryNodesToLgNodeInfos
                };

                geometryGraph.LgData = _lgData;
            }
            else {
                _initFromPrecomputedLgData = true;
                _lgData = geometryGraph.LgData;
                lgLayoutSettings.GeometryNodesToLgNodeInfos = _lgData.GeometryNodesToLgNodeInfos;
            }
            // add skeleton levels
            AddSkeletonLevels();
        }

        double CurrentZoomLevel { get; set; }

        /// <summary>
        ///     this graph is currently visible set of nodes and pieces of edges
        /// </summary>
        public RailGraph RailGraph {
            get { return _railGraph; }
        }

        /// <summary>
        /// </summary>
        public IDictionary<Edge, LgEdgeInfo> GeometryEdgesToLgEdgeInfos {
            get { return _lgData.GeometryEdgesToLgEdgeInfos; }
        }

        public Set<LgNodeInfo> SelectedNodeInfos {
            get { return _lgData.SelectedNodeInfos; }
        }


        void AddSkeletonLevels() {
            for (int i = _lgData.SkeletonLevels.Count; i < _lgData.Levels.Count(); i++) {
                _lgData.SkeletonLevels.Add(new LgSkeletonLevel {ZoomLevel = _lgData.Levels[i].ZoomLevel});
            }
        }

        /// <summary>
        ///     does the initialization
        /// </summary>
        public void Run() {
            if (_initFromPrecomputedLgData) {
                _lgData.CreateLevelNodeTrees(NodeDotWidth(1));
                _railGraph = new RailGraph();
                return;
            }

#if DEBUG && TEST_MSAGL
            _mainGeometryGraph.SetDebugIds();
#endif

            CreateConnectedGraphs();
            FillGeometryNodeToLgInfosTables();
            LevelCalculator.RankGraph(_lgData, _mainGeometryGraph);
            LayoutTheWholeGraph();
#if !SILVERLIGHT && !SHARPKIT
            var timer = new Timer();
            timer.Start();
#endif

            LevelCalculator.SetNodeZoomLevelsAndRouteEdgesOnLevels(_lgData, _mainGeometryGraph, _lgLayoutSettings);
            Debug.Assert(ClusterRanksAreNotLessThanChildrens());
            _railGraph = new RailGraph();
            LayoutAndRouteByLayers(_lgLayoutSettings.MaxNumberOfNodesPerTile, _lgLayoutSettings.MaxNumberOfRailsPerTile,
                _lgLayoutSettings.IncreaseNodeQuota);
#if !SILVERLIGHT && !SHARPKIT
            timer.Stop();
            Console.WriteLine("levels calculated for {0}", timer.Duration);
            if (_lgLayoutSettings.ExitAfterInit)
                Environment.Exit(0);
#endif
        }


        bool ClusterRanksAreNotLessThanChildrens() {
            return
                _mainGeometryGraph.RootCluster.AllClustersDepthFirstExcludingSelf().
                    All(cluster => cluster.Clusters.Concat(cluster.Nodes).Any(n =>
                        _lgData.GeometryNodesToLgNodeInfos[cluster].Rank >= _lgData.GeometryNodesToLgNodeInfos[n].Rank));
        }


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
                Node node in
                    _mainGeometryGraph.Nodes.Concat(_mainGeometryGraph.RootCluster.AllClustersWideFirstExcludingSelf()))
                _lgData.GeometryNodesToLgNodeInfos[node] = new LgNodeInfo(node);
            foreach (Edge edge in _mainGeometryGraph.Edges) {
                _lgData.GeometryEdgesToLgEdgeInfos[edge] = new LgEdgeInfo(edge);
            }
        }

        void LayoutTheWholeGraph() {
            if (_lgLayoutSettings.NeedToLayout) {
                if (_runInParallel)
                    Parallel.ForEach(_lgData.ConnectedGeometryGraphs, new ParallelOptions(), LayoutAndPadOneComponent);
                else
                    foreach (GeometryGraph connectedGraph in _lgData.ConnectedGeometryGraphs)
                        LayoutAndPadOneComponent(connectedGraph);

                Rectangle rect = MdsGraphLayout.PackGraphs(_lgData.ConnectedGeometryGraphs, _lgLayoutSettings);
                _mainGeometryGraph.BoundingBox = rect;
            }
            else
                foreach (GeometryGraph graph in _lgData.ConnectedGeometryGraphs)
                    graph.UpdateBoundingBox();
        }

        void LayoutAndPadOneComponent(GeometryGraph connectedGraph) {
            LayoutOneComponent(connectedGraph);
            connectedGraph.BoundingBox.Pad(_lgLayoutSettings.NodeSeparation/2);
        }

        void CreateConnectedGraphs() {
            Dictionary<Node, int> nodeToIndex;
            List<Node> listOfNodes = CreateNodeListForBasicGraph(out nodeToIndex);
            var basicGraph = new BasicGraph<SimpleIntEdge>(GetSimpleIntEdges(nodeToIndex), listOfNodes.Count);
            IEnumerable<IEnumerable<int>> comps = ConnectedComponentCalculator<SimpleIntEdge>.GetComponents(basicGraph);
            foreach (var comp in comps)
                _lgData.AddConnectedGeomGraph(GetConnectedSubgraph(comp, listOfNodes));
        }

        GeometryGraph GetConnectedSubgraph(IEnumerable<int> comp, List<Node> nodeList) {
            var edges = new List<Edge>();
            var nodes = new List<Node>();
            var geomGraph = new GeometryGraph();
            foreach (int i in comp) {
                Node node = nodeList[i];
                var cluster = node as Cluster;
                if (cluster != null) {
                    if (cluster.ClusterParents.First() == _mainGeometryGraph.RootCluster) {
                        //MainGeometryGraph.RootCluster.RemoveCluster(cluster);
                        geomGraph.RootCluster.AddCluster(cluster);
                    }
                }
                else {
                    nodes.Add(node);
                }

                foreach (Edge edge in node.OutEdges.Concat(node.SelfEdges)) {
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

            foreach (Node node in _mainGeometryGraph.Nodes) {
                nodeToIndex[node] = list.Count;
                list.Add(node);
            }
            foreach (Cluster node in _mainGeometryGraph.RootCluster.AllClustersDepthFirstExcludingSelf()) {
                nodeToIndex[node] = list.Count;
                list.Add(node);
            }
            return list;
        }

        List<SimpleIntEdge> GetSimpleIntEdges(Dictionary<Node, int> nodeToIndex) {
            var list =
                _mainGeometryGraph.Edges.Select(
                    edge => new SimpleIntEdge {Source = nodeToIndex[edge.Source], Target = nodeToIndex[edge.Target]})
                    .ToList();

            foreach (Cluster cluster in _mainGeometryGraph.RootCluster.AllClustersDepthFirstExcludingSelf()) {
                list.AddRange(
                    cluster.Clusters.Select(
                        child => new SimpleIntEdge {Source = nodeToIndex[cluster], Target = nodeToIndex[child]}));
                list.AddRange(
                    cluster.Nodes.Select(
                        child => new SimpleIntEdge {Source = nodeToIndex[cluster], Target = nodeToIndex[child]}));
            }
            return list;
        }

        void LayoutOneComponent(GeometryGraph component) {
            PrepareGraphForLayout(component);
            if (component.RootCluster.Clusters.Any()) {
                // todo: do we really use layered layout here?
                var layoutSettings = new SugiyamaLayoutSettings
                {
                    FallbackLayoutSettings =
                        new FastIncrementalLayoutSettings
                        {
                            AvoidOverlaps = true
                        },
                    NodeSeparation = _lgLayoutSettings.NodeSeparation,
                    LayerSeparation = _lgLayoutSettings.NodeSeparation,
                    EdgeRoutingSettings = _lgLayoutSettings.EdgeRoutingSettings,
                    LayeringOnly = true
                };
                var initialBc = new InitialLayoutByCluster(component, a => layoutSettings);
                initialBc.Run();
            }
            else {
                // todo: currently implemented as if there is only one component
                LayoutHelpers.CalculateLayout(component, GetMdsLayoutSettings(), _cancelToken);
                RemoveOverlapsForLgLayout(component);

            }
            Rectangle box = component.BoundingBox;
            box.Pad(_lgLayoutSettings.NodeSeparation/2);
            component.BoundingBox = box;
        }

        void RemoveOverlapsForLgLayout(GeometryGraph component) {
            Node[] componentNodes = component.Nodes.ToArray();
            Debug.Assert(RankingIsDefined(componentNodes));
            SortComponentNodesByRanking(componentNodes);
            List<int> approxLayerCounts = GetApproximateLayerCounts(componentNodes);
            Size[] sizes = GetApproxSizesForOverlapRemoval(approxLayerCounts, _lgLayoutSettings.NodeSeparation*3,
                componentNodes);
            OverlapRemoval.RemoveOverlapsForLayers(componentNodes, sizes);
        }

        Size[] GetApproxSizesForOverlapRemoval(List<int> approxLayerCounts, double w, Node[] componentNodes) {
            var ret = new Size[componentNodes.Length];
            int layer = 0;

            for (int i = 0; i < componentNodes.Length; i++) {
                ret[i] = new Size(w, w);
                if (i == approxLayerCounts[layer]) {
                    w /= 2;
                    layer++;
                }
            }
            return ret;
        }

        /// <summary>
        ///     trying to approximately evaluate what the number of nodes will be in each layer
        /// </summary>
        /// <param name="componentNodes"></param>
        /// <returns></returns>
        List<int> GetApproximateLayerCounts(Node[] componentNodes) {
            int total = componentNodes.Length;
            int nodesPerTile = _lgLayoutSettings.MaxNumberOfNodesPerTile;
            int numberOfTiles = 1;
            var ret = new List<int>();
            while (true) {
                int nodesOnLayer = Math.Min(nodesPerTile*numberOfTiles, total);
                ret.Add(nodesOnLayer);
                if (nodesOnLayer == total)
                    break;
                numberOfTiles *= 3; // thinking about those empty tiles!
            }
            return ret;
        }

        void SortComponentNodesByRanking(Node[] componentNodes) {
            var comparer = new RankComparer(_lgData.GeometryNodesToLgNodeInfos);
            Array.Sort(componentNodes, comparer);
        }

        bool RankingIsDefined(Node[] componentNodes) {
            if (componentNodes.Length == 0) return true;
            return _lgData.GeometryNodesToLgNodeInfos[componentNodes[0]].Rank > 0;
        }

        static void PrepareGraphForLayout(GeometryGraph connectedGraph) {
            foreach (Cluster cluster in connectedGraph.RootCluster.AllClustersDepthFirst()) {
                if (cluster.RectangularBoundary == null)
                    cluster.RectangularBoundary = new RectangularClusterBoundary();
            }

            foreach (Edge edge in connectedGraph.Edges) {
                if (edge.SourcePort == null) {
                    Edge e = edge;
                    edge.SourcePort = new RelativeFloatingPort(() => e.Source.BoundaryCurve,
                        () => e.Source.Center);
                }
                if (edge.TargetPort == null) {
                    Edge e = edge;
                    edge.TargetPort = new RelativeFloatingPort(() => e.Target.BoundaryCurve,
                        () => e.Target.Center);
                }
            }
        }


        MdsLayoutSettings GetMdsLayoutSettings() {
            var settings = new MdsLayoutSettings
            {
                EdgeRoutingSettings = {KeepOriginalSpline = true, EdgeRoutingMode = EdgeRoutingMode.None},
                RemoveOverlaps = false
            };
            return settings;
        }

        bool IsEdgeVisibleAtCurrentLevel(Edge e) {
            LgEdgeInfo ei;
            if (!GeometryEdgesToLgEdgeInfos.TryGetValue(e, out ei)) return false;

            double curLevel = _lgData.Levels[_lgData.GetLevelIndexByScale(CurrentZoomLevel)].ZoomLevel;
            return ei.ZoomLevel <= curLevel;
        }

        void AddHighlightedEdgesAndNodesToRailGraph() {
            _railGraph.Edges.InsertRange(_lgData.SelectedEdges);
            _railGraph.Nodes.InsertRange(_lgData.SelectedNodeInfos.Select(n => n.GeometryNode));
        }

        void AddVisibleRailsAndNodes() {
            _railGraph.Rails.Clear();
            var level = _lgData.GetCurrentLevelByScale(CurrentZoomLevel);
            _railGraph.Rails.InsertRange(level.GetRailsIntersectingRect(_visibleRectangle));
            _railGraph.Nodes.InsertRange(level.GetNodesIntersectingRect(_visibleRectangle));
            _railGraph.Edges.Clear();
            _railGraph.Edges.InsertRange(
                _railGraph.Rails.Select(r => r.TopRankedEdgeInfoOfTheRail.Edge).Where(IsEdgeVisibleAtCurrentLevel));
            AddHighlightedEdgesAndNodesToRailGraph();
        }

        internal static GeometryGraph CreateClusteredSubgraphFromFlatGraph(GeometryGraph subgraph,
            GeometryGraph mainGeometryGraph) {
            if (mainGeometryGraph.RootCluster.Clusters.Any() == false) return subgraph;
            var ret = new GeometryGraph();
            Dictionary<Node, Node> originalNodesToNewNodes = MapSubgraphNodesToNewNodesForRouting(subgraph);
            ReplicateClusterStructure(subgraph, originalNodesToNewNodes);
            AddNewNodeAndClustersToTheNewGraph(originalNodesToNewNodes, ret);


            foreach (Edge edge in subgraph.Edges) {
                Node ns = originalNodesToNewNodes[edge.Source];
                Node nt = originalNodesToNewNodes[edge.Target];
                ret.Edges.Add(new Edge(ns, nt)
                {
                    EdgeGeometry = edge.EdgeGeometry,
                    SourcePort = null,
                    TargetPort = null
                });
            }

            foreach (var kv in originalNodesToNewNodes) {
                Node newNode = kv.Value;
                var cluster = newNode as Cluster;
                if (cluster != null) {
                    Node oldNode = kv.Key;
                    if (oldNode.BoundaryCurve != newNode.BoundaryCurve) {
                        oldNode.BoundaryCurve = newNode.BoundaryCurve;
                        oldNode.RaiseLayoutChangeEvent(null);
                    }
                }
            }
            return ret;
            //LayoutAlgorithmSettings.ShowGraph(ret);
        }


        static void AddNewNodeAndClustersToTheNewGraph(Dictionary<Node, Node> onodesToNewNodes, GeometryGraph ret) {
            foreach (Node newNode in onodesToNewNodes.Values) {
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
            foreach (Node onode in geometryGraph.Nodes)
                foreach (Cluster oclparent in onode.ClusterParents) {
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
            foreach (Node oNode in geometryGraph.Nodes) {
                var cluster = oNode as Cluster;

                onodesToNewNodes[oNode] = cluster != null
                    ? new Cluster
                    {
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
        /// </summary>
        public void RunOnViewChange() {
            _visibleRectangle = _lgLayoutSettings.ClientViewportMappedToGraph();
            //Rectangle.Intersect(lgLayoutSettings.ClientViewportMappedToGraph, mainGeometryGraph.BoundingBox);

            //            if (MainGeometryGraph.Edges.Count == 33) {
            //                LayoutAlgorithmSettings.ShowDebugCurves(
            //                    new DebugCurve("red", MainGeometryGraph.BoundingBox.Perimeter()),
            //                    new DebugCurve("blue", visibleRectangle.Perimeter()));
            //                LayoutAlgorithmSettings.ShowGraph(clusterOGraph);
            //            }
            if (_visibleRectangle.IsEmpty) return; //probably we should avoid this situation

            CurrentZoomLevel = GetZoomFactorToTheGraph();
            FillRailGraph();
            _lgLayoutSettings.OnViewerChangeTransformAndInvalidateGraph();
        }

        public double GetZoomFactorToTheGraph() {
            return _lgLayoutSettings.TransformFromGraphToScreen()[0, 0]/FitFactor();
        }

        double FitFactor() {
            Rectangle vp = _lgLayoutSettings.ClientViewportFunc();
            return Math.Min(vp.Width/_mainGeometryGraph.Width, vp.Height/_mainGeometryGraph.Height);
        }


        void FillRailGraph() {
            _railGraph.Nodes.Clear();
            AddVisibleRailsAndNodes();
            RegisterEdgeSourceTargets();
        }



        void RegisterEdgeSourceTargets() {
            foreach (Edge e in _railGraph.Edges)
                AddEdgeSourceAndTarget(e);
        }

        void AddEdgeSourceAndTarget(Edge edge) {
            _railGraph.Nodes.Insert(edge.Source); //
            _railGraph.Nodes.Insert(edge.Target);
        }


        /// <summary>
        /// </summary>
        /// <returns></returns>
        public double GetMaximalZoomLevel() {
            if (_lgData == null)
                return 1;
            return _lgData.GetMaximalZoomLevel();
        }

        /// <summary>
        ///     gets all edges passing through rail from the rail's level.
        /// </summary>
        /// <param name="rail"></param>
        /// <returns></returns>
        public List<Edge> GetEdgesPassingThroughRail(Rail rail) {
            int i = (int) Math.Log(rail.ZoomLevel, 2);
            LgLevel railLevel = _lgData.Levels[i];
            List<Edge> passingEdges = railLevel.GetEdgesPassingThroughRail(rail);
            return passingEdges;
        }

        int GetIndexByZoomLevel(double z) {
            return (int) Math.Log(z, 2);
        }

//
//        void GetCellInfosOfLevel(int iLevel, List<LgCellInfo> cellInfos) {
//            Rectangle biggestTile = _lgData.BiggestTile.IsEmpty ? _mainGeometryGraph.BoundingBox : _lgData.BiggestTile;
//
//            var levelCellInfos = new List<LgCellInfo>();
//            Rectangle rect1 = _visibleRectangle.Intersection(biggestTile);
//            Rectangle rect2 = biggestTile;
//            var d = (int) Math.Pow(2, iLevel);
//            double cW = rect2.Width/d;
//            double cH = rect2.Height/d;
//            var iL = (int) ((rect1.Left - rect2.Left)/rect2.Width*d);
//            double iR = Math.Ceiling((rect1.Right - rect2.Left)/rect2.Width*d);
//            var iB = (int) ((rect1.Bottom - rect2.Bottom)/rect2.Height*d);
//            double iT = Math.Ceiling((rect1.Top - rect2.Bottom)/rect2.Height*d);
//            iL = Math.Min(d, Math.Max(iL, 0));
//            iR = Math.Min(d, Math.Max(iR, 0));
//            iT = Math.Min(d, Math.Max(iT, 0));
//            iB = Math.Min(d, Math.Max(iB, 0));
//
//            for (int ix = iL; ix < iR; ix++) {
//                for (int iy = iB; iy < iT; iy++) {
//                    Point pBottomLeft = biggestTile.LeftBottom + new Point(ix*cW, iy*cH);
//                    var r = new Rectangle(pBottomLeft, pBottomLeft + new Point(cW, cH));
//                    var ci = new LgCellInfo
//                    {
//                        CellRectangle = r,
//                        ZoomLevel = d,
//                        MaxNumberNodesPerTile = _lgLayoutSettings.MaxNumberOfNodesPerTile
//                    };
//                    levelCellInfos.Add(ci);
//                }
//            }
//
//            CountContainingNodesOfLevelLeq(d, levelCellInfos);
//            cellInfos.AddRange(levelCellInfos);
//        }

//        void CountContainingNodesOfLevelLeq(double zoomLevel, IEnumerable<LgCellInfo> cellInfos) {
//            throw new NotImplementedException();
////            var ciRtree = new RTree<LgCellInfo>();
////            var visCellsRect = new Rectangle();
////            visCellsRect.SetToEmpty();
////            foreach (LgCellInfo ci in cellInfos) {
////                ciRtree.Add(ci.CellRectangle, ci);
////                visCellsRect.Add(ci.CellRectangle.LeftBottom);
////                visCellsRect.Add(ci.CellRectangle.RightTop);
////            }
////            IEnumerable<LgNodeInfo> nodeInfosInCells = _lgNodeHierarchy.GetNodeItemsIntersectingRectangle(visCellsRect);
////            foreach (LgNodeInfo nodeInfo in nodeInfosInCells) {
////                if (nodeInfo.ZoomLevel <= zoomLevel) {
////                    LgCellInfo[] toCount = ciRtree.GetAllIntersecting(nodeInfo.GeometryNode.BoundingBox);
////                    foreach (LgCellInfo ci in toCount) {
////                        if (ci.ZoomLevel <= zoomLevel)
////                            ci.NumberNodesLeqItsLevelInside++;
////                    }
////                }
////            }
//        }



        public List<LgNodeInfo> GetAllNodesOnVisibleLayers() {
            var nodesToSelect = new List<LgNodeInfo>();
            for (int i = 0; i < _lgData.Levels.Count; i++) {
                var nodes = GetNodeInfosOnLevel(i);
                nodesToSelect.AddRange(nodes);
            }
            return nodesToSelect;
        }

        IEnumerable<LgNodeInfo> GetNodeInfosOnLevel(int i) {
            int iStart = (i == 0 ? 0 : _lgData.LevelNodeCounts[i - 1]);
            int iEnd = _lgData.LevelNodeCounts[i];
            for (int j = iStart; j < iEnd; j++)
                yield return _lgData.SortedLgNodeInfos[j];
        }

        IEnumerable<LgNodeInfo> GetNodeInfosOnLevelLeq(int i) {
            int iEnd = _lgData.LevelNodeCounts[i];
            for (int j = 0; j < iEnd; j++)
                yield return _lgData.SortedLgNodeInfos[j];
        }

        public void InitEdgesOfLevels() {
            foreach (Edge edge in GeometryEdgesToLgEdgeInfos.Keys) {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                int iLevel = GetIndexByZoomLevel(Math.Max(s.ZoomLevel, t.ZoomLevel));
                for (int j = iLevel; j < _lgData.Levels.Count; j++) {
                    _lgData.Levels[j]._railsOfEdges[edge] = new Set<Rail>();
                }
            }
        }

        List<LgNodeInfo> GetNeighborsOnLevel(LgNodeInfo ni, int i) {
            int zoomLevel = _lgData.Levels[i].ZoomLevel;
            Debug.Assert(ni.ZoomLevel <= zoomLevel);
            List<LgNodeInfo> neighb = (from e in ni.GeometryNode.OutEdges
                let nit = _lgData.GeometryNodesToLgNodeInfos[e.Target]
                where nit.ZoomLevel <= zoomLevel
                select nit).ToList();
            neighb.AddRange(from e in ni.GeometryNode.InEdges
                let nis = _lgData.GeometryNodesToLgNodeInfos[e.Source]
                where nis.ZoomLevel <= zoomLevel
                select nis);
            return neighb;
        }


        public void RouteEdgesOnZeroLayer() {
            var skeletonLevel = _lgData.SkeletonLevels[0];
            skeletonLevel.ClearSavedTrajectoriesAndUsedEdges();
            Console.Write("\nRouting edges");
            int numRouted = 0;

            foreach (LgNodeInfo ni in GetNodeInfosOnLevelLeq(0))
                foreach (LgNodeInfo t in GetNeighborsOnLevel(ni, 0)) {
                    var path = _lgData.SkeletonLevels[0].HasSavedTrajectory(ni, t)
                        ? skeletonLevel.GetTrajectory(ni, t)
                        : skeletonLevel.PathRouter.GetPath(ni, t, ShrinkEdgeLengths);
                    skeletonLevel.SetTrajectoryAndAddEdgesToUsed(ni, t, path);

                    if (numRouted++%100 == 0)
                        Console.Write(".");
                }
        }

        bool EdgeIsNew(LgNodeInfo s, LgNodeInfo t, int i) {
            return Math.Max(s.ZoomLevel, t.ZoomLevel) >= _lgData.Levels[i].ZoomLevel;
        }

        public void RouteEdges(int i) {
            AddAllPortEdges(i);
            if (i == 0)
                RouteEdgesOnZeroLayer();
            else
                RouteOnHigherLevel(i);
        }

        void RouteOnHigherLevel(int iLevel) {
            IEnumerable<LgNodeInfo> nodes;
            var skeletonLevel = DealWithPathsFromPreviousLevels(iLevel, out nodes);
            ComputeNewTrajectories(iLevel, nodes, skeletonLevel);
        }

        LgSkeletonLevel DealWithPathsFromPreviousLevels(int i, out IEnumerable<LgNodeInfo> nodes) {
            var skeletonLevel = _lgData.SkeletonLevels[i];
            var prevSkeletonLevel = _lgData.SkeletonLevels[i - 1];
            nodes = GetNodeInfosOnLevelLeq(i);

            skeletonLevel.ClearSavedTrajectoriesAndUsedEdges();
            skeletonLevel.PathRouter.ResetAllEdgeLengthMultipliers();

            Console.Write("\nRouting edges");
            Console.Write("\nUpdating old trajectories");
            UpdatePrevLayerTrajectories(i, nodes, prevSkeletonLevel, skeletonLevel);
            DecreaseWeightsAlongOldTrajectories(i, skeletonLevel);

            return skeletonLevel;
        }

//        bool susp(Point s, Point t) {
//            Point ss = new Point(73.79755, 210.1525);
//            Point tt = new Point(-119.5393, 50.605);
//            return (s - ss).Length < 2 && (t - tt).Length < 2;
//        }

        /// <summary>
        /// find a better name
        /// </summary>
        /// <param name="i"></param>
        /// <param name="nodes"></param>
        /// <param name="prevSkeletonLevel"></param>
        /// <param name="skeletonLevel"></param>
        void UpdatePrevLayerTrajectories(int i, IEnumerable<LgNodeInfo> nodes,
            LgSkeletonLevel prevSkeletonLevel, LgSkeletonLevel skeletonLevel
            ) {
            int numRouted = 0;
            foreach (LgNodeInfo s in nodes) {
                var neighb = GetNeighborsOnLevel(s, i).OrderBy(n => n.ZoomLevel);
                foreach (LgNodeInfo t in neighb) {
                    var oldTrajectory = prevSkeletonLevel.GetTrajectory(s, t);
                    if (oldTrajectory == null) continue;
                    var trajectory = skeletonLevel.PathRouter.GetPathOnSavedTrajectory(s, t,
                        oldTrajectory, ShrinkEdgeLengths);
                    skeletonLevel.SetTrajectoryAndAddEdgesToUsed(s, t, trajectory);

                    skeletonLevel.MarkEdgesAlongPathAsEdgesOnOldTrajectories(trajectory);
                }
                if (++numRouted%100 == 1) Console.Write(".");
            }
        }

        void DecreaseWeightsAlongOldTrajectories(int i, LgSkeletonLevel skeletonLevel) {
            foreach (var tuple in skeletonLevel.EdgeTrajectories) {
                // debug
                if (!EdgeIsNew(tuple.Key.A, tuple.Key.B, i)) {
                    List<Point> oldPath = tuple.Value;
                    double iEdgeLevel = Math.Log(Math.Max(tuple.Key.A.ZoomLevel, tuple.Key.B.ZoomLevel), 2);
                    double newWeight = //(iEdgeLevel < 1 ? 0.3 : (iEdgeLevel < 2 ? 0.6 : 1));                    
                        0.4 + iEdgeLevel/(Math.Max(i - 1.0, 1.0))*0.2;
                    newWeight = Math.Max(0.4, newWeight);
                    newWeight = Math.Min(0.6, newWeight);
                    skeletonLevel.PathRouter.DecreaseWeightOfEdgesAlongPath(oldPath, newWeight);
                }
            }
        }

        void ComputeNewTrajectories(int i, IEnumerable<LgNodeInfo> nodes, LgSkeletonLevel skeletonLevel) {
            int numRouted = 0;
            Console.Write("\nComputing new trajectories");
            foreach (LgNodeInfo s in nodes) {
                IOrderedEnumerable<LgNodeInfo> neighb = GetNeighborsOnLevel(s, i).OrderBy(n => n.ZoomLevel);

                foreach (LgNodeInfo t in neighb) {
                    List<Point> path = skeletonLevel.GetTrajectory(s, t);
                    if (path == null) {
                        path = skeletonLevel.PathRouter.GetPath(s, t, ShrinkEdgeLengths);
                        skeletonLevel.SetTrajectoryAndAddEdgesToUsed(s, t, path);
                    }
                    numRouted++;
                    if (numRouted%100 == 1)
                        Console.Write(".");
                }
            }
        }

        public bool CheckSanityAllRoutes(int i) {
            var nodes = GetNodeInfosOnLevelLeq(i);
            bool sanity = true;
            foreach (LgNodeInfo s in nodes) {
                sanity &= CheckSanityRoutes(s, i);
                if (!sanity) break;
            }
            return sanity;
        }

        public bool CheckSanityRoutes(LgNodeInfo s, int i) {
            List<LgNodeInfo> neighb = GetNeighborsOnLevel(s, i);
            Point rootPoint = _lgData.SkeletonLevels[i].PathRouter.AddVisGraphVertex(s.Center);

            var graph = new LgPathRouter();
            foreach (LgNodeInfo t in neighb) {
                List<Point> path = _lgData.SkeletonLevels[i].GetTrajectory(s, t);
                for (int j = 0; j < path.Count - 1; j++) {
                    graph.AddVisGraphEdge(path[j], path[j + 1]);
                }
            }
            bool hasCycles = graph.HasCycles(rootPoint);
            if (hasCycles) Console.WriteLine("BAD SP Tree at: " + s);

            //foreach (var t in neighb)
            //{
            //    List<Point> path, treePath;
            //    if (!lgData.SkeletonLevels[i].HasSavedTrajectory(s, t))
            //        continue;

            //    path = lgData.SkeletonLevels[i].GetTrajectory(s, t);
            //    tree.AddPathToTree(path);
            //    var tp = lgData.SkeletonLevels[i].VisGraph.AddVisGraphVertex(t.Center);
            //    treePath = tree.GetPathFromRoot(tp);
            //    if (!path.First().Equals(rootPoint))
            //    {
            //        path.Reverse();
            //    }
            //    bool pathsEqual = true;
            //    for (int j = 0; j < path.Count; j++)
            //    {
            //        pathsEqual &= path[j].Equals(treePath[j]);
            //        if (!pathsEqual)
            //            break;
            //    }
            //    if (!pathsEqual)
            //    {
            //        return false;
            //    }
            //}
            return !hasCycles;
        }

        void CopyGraphToNextLevel(int i) {
            foreach (var st in _lgData.SkeletonLevels[i].PathRouter.GetAllEdgesVisibilityEdges()) {
                _lgData.SkeletonLevels[i + 1].PathRouter.AddVisGraphEdge(st.SourcePoint, st.TargetPoint);
            }
        }


        public void ClearSkeleton(int iLevel) {
            _lgData.SkeletonLevels[iLevel].Clear();
        }

        public void RunOverlapRemovalBitmap(int iLevel) {
            //if (iLevel == 0) return;
            // should run even for level 0!

            var fixedNodes = iLevel > 0
                ? GetNodeInfosOnLevelLeq(iLevel - 1)
                : new List<LgNodeInfo>();
            var moveableNodes = GetNodeInfosOnLevel(iLevel);

            Rectangle[] fixedBoxes = (from node in fixedNodes select node.BoundaryOnLayer.BoundingBox).ToArray();
            Rectangle[] moveableBoxes = (from node in moveableNodes select node.BoundaryOnLayer.BoundingBox).ToArray();

//            string[] fixedBoxesLabels = (from node in fixedNodes select node.ToString()).ToArray();
//            string[] moveableBoxesLabels = (from node in moveableNodes select node.ToString()).ToArray();

            var fixedSegments = iLevel > 0
                ? _lgData.Levels[iLevel - 1].GetAllRailsEndpoints().ToArray()
                : new SymmetricSegment[0];

#if DEBUG
            // ShowNodesAndSegmentsForOverlapRemoval(fixedNodes, moveableNodes, fixedSegments);
#endif
            //Test
            //Test1.RunTestOrb1(moveableBoxes, fixedBoxes, fixedSegments);
            //return;
            //Test end

            var orb = new OverlapRemovalFixedSegmentsBitmap(moveableBoxes, fixedBoxes, fixedSegments)
            {
                NumPixelsPadding = 10
            };


            var fixedRectTree = new RTree<Rectangle>();
            var fixedSegTree = new RTree<SymmetricSegment>();
            foreach (Rectangle rect in fixedBoxes)
                fixedRectTree.Add(rect, rect);
            foreach (var seg in fixedSegments)
                fixedSegTree.Add(new Rectangle(seg.A, seg.B), seg);

            int numPlaced = 0;
            while (true) {
                numPlaced = orb.PositionAllMoveableRectsSameSize(numPlaced, fixedRectTree, fixedSegTree);
                if (numPlaced >= moveableBoxes.Count()) break;
                orb.ScaleBbox(1.25);
            }

            Point[] translations = orb.GetTranslations();
            int i = 0;
            foreach (var mv in moveableNodes) {
                mv.Translate(translations[i++]);
            }

#if DEBUG
            // ShowNodesAndSegmentsForOverlapRemoval(fixedNodes, moveableNodes, fixedSegments);
#endif
        }

        static void ShowNodesAndSegmentsForOverlapRemoval(IEnumerable<LgNodeInfo> fixedNodes,
            IEnumerable<LgNodeInfo> moveableNodes, SymmetricSegment[] fixedSegments) {
#if DEBUG && !SILVERLIGHT && !SHARPKIT
            var l = new List<DebugCurve>();
            if (fixedNodes != null && fixedNodes.Any()) {
                foreach (var node in fixedNodes) {
                    l.Add(new DebugCurve(200, 0.1, "red", node.BoundaryCurve));
                }
            }
            if (fixedSegments != null && fixedSegments.Any()) {
                foreach (var seg in fixedSegments) {
                    var ls = new LineSegment(seg.A, seg.B);
                    l.Add(new DebugCurve(100, 0.1, "magenta", ls));
                }
            }
            foreach (var node in moveableNodes) {
                l.Add(new DebugCurve(200, 0.1, "green", node.BoundaryCurve));
            }
            LayoutAlgorithmSettings.ShowDebugCurves(l.ToArray());
#endif
        }

        void ClearLevels(int num) {
            _lgData.Levels.Clear();
            _lgData.SkeletonLevels.Clear();
            int zoomLevel = 1;
            for (int i = 0; i < num; i++) {
                var level = new LgLevel(zoomLevel, _mainGeometryGraph);
                level.ClearRailTree();

                _lgData.Levels.Add(level);
                _lgData.SkeletonLevels.Add(new LgSkeletonLevel {ZoomLevel = zoomLevel});

                zoomLevel *= 2;
            }
        }

        void UpdateNodeInfoZoomLayers() {
            int level = 1;
            int j = 0;
            for (int i = 0; i < _lgData.SortedLgNodeInfos.Count; i++) {
                while (j < _lgData.LevelNodeCounts.Count && i == _lgData.LevelNodeCounts[j]) {
                    j++;
                    level *= 2;
                }
                _lgData.SortedLgNodeInfos[i].ZoomLevel = level;
            }
        }

        void InitRailsOfEdgesEmpty() {
            foreach (Edge edge in _mainGeometryGraph.Edges) {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                double zoomLevel = Math.Max(s.ZoomLevel, t.ZoomLevel);

                GeometryEdgesToLgEdgeInfos[edge].ZoomLevel = zoomLevel;
                int iLevel = GetIndexByZoomLevel(zoomLevel); //(int) Math.Log(zoomLevel, 2);
                for (int j = iLevel; j < _lgData.Levels.Count; j++)
                    _lgData.Levels[j]._railsOfEdges[edge] = new Set<Rail>();
            }
        }

        void InitRailsOfEdgesEmpty(int iMinLevel) {
            foreach (Edge edge in _mainGeometryGraph.Edges) {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                double zoomLevel = Math.Max(s.ZoomLevel, t.ZoomLevel);
                GeometryEdgesToLgEdgeInfos[edge].ZoomLevel = zoomLevel;

                int iLevel = Math.Max(GetIndexByZoomLevel(zoomLevel), iMinLevel);
                for (int j = iLevel; j < _lgData.Levels.Count; j++)
                    _lgData.Levels[j]._railsOfEdges[edge] = new Set<Rail>();
            }
        }

        public void AddAllPortEdges(int iLevel) {
            var nodes = GetNodeInfosOnLevelLeq(iLevel);
            var skeletonLevel = _lgData.SkeletonLevels[iLevel];
            skeletonLevel.AddGraphEdgesFromCentersToPointsOnBorders(nodes);
            //SplineRouter.ShowVisGraph(skeletonLevel.PathRouter.VisGraph, null,null,null);
        }

        void CreateGraphFromSteinerCdt(int iLevel) {
            var nodes = GetNodeInfosOnLevelLeq(iLevel);
            var cdt = new SteinerCdt(_lgData.SkeletonLevels[iLevel].PathRouter.VisGraph, nodes);
            cdt.ReadTriangleOutputAndPopulateTheLevelVisibilityGraphFromTriangulation();
        }

        public bool SimplifyRoutes(int iLevel) {
            Console.WriteLine("\nsimplifying level {0}", iLevel);
            var nodes = GetNodeInfosOnLevelLeq(iLevel);
            var skeletonLevel = _lgData.SkeletonLevels[iLevel];
            var fixedVertices = new Set<Point>(nodes.Select(n => n.Center));
            if (iLevel > 0)
                fixedVertices.InsertRange(_lgData.SkeletonLevels[iLevel - 1].GetPointsOnSavedTrajectories());
#if DEBUG
//            SplineRouter.ShowVisGraph(skeletonLevel.PathRouter.VisGraph, nodes.Select(n=>n.BoundaryOnLayer), null, null);
#endif

            var routeSimplifier = new RouteSimplifier(skeletonLevel.PathRouter, nodes, fixedVertices);
            return routeSimplifier.Run();
        }


#if DEBUG && !SILVERLIGHT && !SHARPKIT
        static void ShowOldNewIntersected(Set<Rail> oldIntersected, Set<Rail> newIntersected, Point a, Point shortcutted,
            Point b, LgSkeletonLevel skeletonLevel) {
            var ll = new List<DebugCurve>();
            ll.Add(new DebugCurve(new LineSegment(a, shortcutted)));
            ll.Add(new DebugCurve(new LineSegment(shortcutted, b)));
            ll.AddRange(oldIntersected.Select(r => new DebugCurve(100, 0.1, "blue", LineFromRail(r))));
            ll.AddRange(newIntersected.Select(r => new DebugCurve(100, 0.1, "red", LineFromRail(r))));
            ll.Add(new DebugCurve(CurveFactory.CreateCircle(3, shortcutted)));
            ll.Add(new DebugCurve("red", CurveFactory.CreateCircle(3, a)));
            ll.Add(new DebugCurve("green", CurveFactory.CreateCircle(3, b)));

            foreach (var rail in newIntersected) {
                var e = FindRailInVisGraph(rail, skeletonLevel);
                if (e == null) {
                    Console.WriteLine("rail {0} is not found in the vis graph", rail);
                }
            }


            LayoutAlgorithmSettings.ShowDebugCurves(ll.ToArray());
        }
#endif

#if DEBUG && !SILVERLIGHT && !SHARPKIT
        private static void ShowOldTrajectories(LgSkeletonLevel skeletonLevel)
        {
            if (skeletonLevel.ZoomLevel <= 1.0) return;
            var segs = skeletonLevel.PathRouter.EdgesOnOldTrajectories();
            var ll = new List<DebugCurve>();
            foreach (var seg in segs)
            {
                ll.Add(new DebugCurve(new LineSegment(seg.A, seg.B)));
            }
            LayoutAlgorithmSettings.ShowDebugCurves(ll.ToArray());
        }
#endif

        static VisibilityEdge FindRailInVisGraph(Rail rail, LgSkeletonLevel skeletonLevel) {
            Point s, t;
            rail.GetStartEnd(out s, out t);
            return skeletonLevel.PathRouter.FindEdge(s, t);
        }

        static ICurve LineFromRail(Rail rail) {
            Point s, t;
            rail.GetStartEnd(out s, out t);
            return new LineSegment(s, t);
        }



        /*
        var rect = new Rectangle(a, b);
            foreach (var railNode in skeletonLevel._railTree.GetAllLeavesIntersectingRectangle(rect)) {
                var rail = railNode.UserData;
                if (NontrivialIntersection(a, b, rail)) {
                    return false;
                }
            }
            return true;
        }
        */

        public void FillLevelsWithNodesOnly(int maxNodesPerTile)
        {
            var calc = new GreedyNodeRailLevelCalculator(_lgData.SortedLgNodeInfos) {
                GeometryNodesToLgNodeInfos = _lgData.GeometryNodesToLgNodeInfos
            };
            calc.MaxAmountNodesPerTile = maxNodesPerTile;

            var bbox = GetLargestTile();

            calc.PlaceNodesOnly(bbox);
            _lgData.LevelNodeCounts = calc.GetLevelNodeCounts();

            ClearLevels(_lgData.LevelNodeCounts.Count);
        }

        public void FillLevelWithNodesRoutesTryRerouting(int iLevel, int maxNodesPerTile, int maxSegmentsPerTile,
            double increaseNodeQuota) {
            var calc = new GreedyNodeRailLevelCalculator(_lgData.SortedLgNodeInfos) {
                GeometryNodesToLgNodeInfos = _lgData.GeometryNodesToLgNodeInfos
            };
            calc.InitBoundingBox();

            calc.MaxAmountNodesPerTile = maxNodesPerTile;
            calc.MaxAmountRailsPerTile = maxSegmentsPerTile;
            calc.IncreaseNodeQuota = increaseNodeQuota;

            var trajectories =
                _lgData.SkeletonLevels[iLevel].EdgeTrajectories;
            int numToInsert = _lgData.LevelNodeCounts[iLevel];

            int zoomLevel = _lgData.Levels[iLevel].ZoomLevel;
            int nodesOnPrevLevel = iLevel > 0 ? _lgData.LevelNodeCounts[iLevel - 1] : 0;

            var bbox = GetLargestTile();

            var oldSegs = GetSegmentsOnOldTrajectories(iLevel);

            //int numInserted = calc.TryInsertingNodesAndRoutesTryRerouting(numToInsert, trajectories, zoomLevel,
            //    nodesOnPrevLevel, new GridTraversal(bbox, iLevel), _lgData.SkeletonLevels[iLevel].PathRouter);

            int numInserted = calc.TryInsertingNodesAndRoutes(numToInsert, trajectories, oldSegs, zoomLevel,
    nodesOnPrevLevel, new GridTraversal(bbox, iLevel), _lgData.SkeletonLevels[iLevel].PathRouter);

            if (iLevel > 0 && numInserted < _lgData.LevelNodeCounts[iLevel - 1])
                // couldn't even get to current level
                numInserted = _lgData.LevelNodeCounts[iLevel - 1];

            if (numInserted < numToInsert)
                PushNodesToNextLevel(iLevel, numInserted);

            // update level node counts
            _lgData.LevelNodeCounts[iLevel] = numInserted;
            }

        public List<SymmetricSegment> GetSegmentsOnOldTrajectories(int iLevel)
        {
            var segs = new List<SymmetricSegment>();
            for (int i = 0; i < iLevel; i++)
            {
                segs.AddRange(_lgData.SkeletonLevels[i].PathRouter.SegmentsNotOnOldTrajectories());
            }
            return segs;
        }

        void AssignRailsByTrajectories(int iLevel) {
            var edges = new List<Edge>(_lgData.Levels[iLevel]._railsOfEdges.Keys);
            _lgData.Levels[iLevel]._railTree = new RTree<Rail>();
            foreach (Edge edge in edges) {
                LgEdgeInfo ei = _lgData.GeometryEdgesToLgEdgeInfos[edge];
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                if (_lgData.SkeletonLevels[iLevel].HasSavedTrajectory(s, t)) {
                    List<Point> trajectory = _lgData.SkeletonLevels[iLevel].GetTrajectory(s, t);
                    List<Rail> rails = _lgData.Levels[iLevel].FetchOrCreateRailSequence(trajectory);
                    _lgData.Levels[iLevel]._orderedRailsOfEdges[edge] = rails;

                    _lgData.AssembleEdgeAtLevel(ei, iLevel, new Set<Rail>(rails));
                }
            }
        }

        void PushNodesToNextLevel(int iLevel, int numInserted) {
            if (iLevel == _lgData.Levels.Count - 1) {
                var level = new LgLevel(_lgData.Levels[iLevel].ZoomLevel*2, _mainGeometryGraph);
                level.CreateEmptyRailTree();
                _lgData.Levels.Add(level);
                _lgData.SkeletonLevels.Add(new LgSkeletonLevel {ZoomLevel = level.ZoomLevel});
                _lgData.LevelNodeCounts.Add(_lgData.LevelNodeCounts.Last());
            }

            int numToInsert = _lgData.LevelNodeCounts[iLevel];
            if (numToInsert <= numInserted) return;

            int newZoomLevel = _lgData.Levels[iLevel + 1].ZoomLevel;
            for (int i = numInserted; i < numToInsert; i++) {
                _lgData.SortedLgNodeInfos[i].ZoomLevel = newZoomLevel;
            }

            _lgData.LevelNodeCounts[iLevel] = numInserted;

            // need to update assigned edge routes
            _lgData.Levels[iLevel]._railsOfEdges.Clear();
            _lgData.Levels[iLevel + 1]._railsOfEdges.Clear();
            InitRailsOfEdgesEmpty(iLevel);
        }


        /// <summary>
        /// calculates all info for LG Browsing
        /// </summary>
        /// <param name="maxNodesPerTile"></param>
        /// <param name="maxSegmentsPerTile"></param>
        /// <param name="increaseNodeQuota"></param>
        public void LayoutAndRouteByLayers(int maxNodesPerTile, int maxSegmentsPerTile, double increaseNodeQuota) {
            FillLevelsWithNodesOnly(maxNodesPerTile);
            
            InitRailsOfEdgesEmpty();
            //InitNodeLabelWidthToHeightRatios();
            AddSkeletonLevels();
            for (int i = 0; i < _lgData.Levels.Count; i++)
                RemoveOverlapsAndRouteForLayer(maxNodesPerTile, maxSegmentsPerTile, increaseNodeQuota, i);


            _lgData.CreateLevelNodeTrees(NodeDotWidth(1));
            //LabelingOfOneRun();
#if DEBUG
            TestAllEdgesConsistency();
#endif
        }

        public Rectangle GetLargestTile()
        {
            var bbox = _mainGeometryGraph.boundingBox.Clone();
            var p = NodeDotWidth(1)*0.5;
            bbox.Pad(p, p, p, p);
            return bbox;
        }

        public void LabelingOfOneRun() {
            foreach (LgNodeInfo node in _lgData.SortedLgNodeInfos) {
                //if (node.ZoomLevel > 1.0)
                //    node.LabelVisibleFromScale = node.ZoomLevel;
                //else
                node.LabelVisibleFromScale = 0.0;
            }

            double scale = 1.0/16.0;

            double delta = Math.Pow(2, 1.0/8.0);

            int numberOfLabeledNodes = 0;
            double hugeScale = Math.Pow(2, _lgData.Levels.Last().ZoomLevel + 1);
            while (numberOfLabeledNodes < _lgData.GeometryNodesToLgNodeInfos.Count && scale <= hugeScale) {
                numberOfLabeledNodes = InsertLabelsGreedily(scale);
                scale *= delta;
            }
            if (numberOfLabeledNodes < _lgData.SortedLgNodeInfos.Count)
                Console.WriteLine("Failed to label {0} nodes", _lgData.SortedLgNodeInfos.Count - numberOfLabeledNodes);

            CleanUpRails();

            // make labels appear slightly earlier
            foreach (LgNodeInfo node in _lgData.SortedLgNodeInfos) {
                node.LabelVisibleFromScale -= 0.0001;
            }
        }

/*
        private void TestRouteNodeOverlaps(int i)
        {
            var skeletonLevel = _lgData.SkeletonLevels[i];
            var nodes = GetNodesOnLevelLeq(i);
            var rtree = new RTree<LgNodeInfo>();
            foreach (var node in nodes)
            {
                var bbox = node.BoundingBox.Clone();
                bbox.ScaleAroundCenter(0.5);
                rtree.Add(bbox, node);
            }

            foreach (var tuple in skeletonLevel.EdgeTrajectories)
            {
                var s = tuple.Key.Item1;
                var t = tuple.Key.Item2;
                var path = tuple.Value;
                var ndsInt = new Set<LgNodeInfo>();
                for (int j = 0; j < path.Count - 1; j++)
                {
                    var p0 = path[j];
                    var p1 = path[j + 1];
                    var nds = rtree.GetAllIntersecting(new Rectangle(p0, p1)).ToList();
                    ndsInt.InsertRange( nds.Where(ni => RectSegIntersection.Intersect(ni.BoundingBox, p0, p1)) );
                }

                if (ndsInt.Count > 2)
                {
                    Console.WriteLine("Path {0} -> {1} intersects nodes {2}", s.GeometryNode.ToString(), t.GeometryNode, ndsInt);
                }
            }
        }
*/

        void RemoveOverlapsAndRouteForLayer(int maxNodesPerTile, int maxSegmentsPerTile, double increaseNodeQuota,
            int iLayer) {
            PrepareNodeBoundariesAndSkeletonOnLayer(iLayer);
            RunOverlapRemovalBitmap(iLayer);

            var skeletonLevel = _lgData.SkeletonLevels[iLayer];

            CreateGraphFromSteinerCdt(iLayer);            

            var bbox = GetLargestTile();

            var gt = new GridTraversal(bbox, iLayer);

#if DEBUG
//            var rects = gt.GetTileRectangles();
//            var rCurves = new List<ICurve>();
//            foreach (var r in rects)
//            {
//                rCurves.Add(CurveFactory.CreateRectangle(r));
//            }
//            SplineRouter.ShowVisGraph(skeletonLevel.PathRouter.VisGraph, null, rCurves, null);
#endif

            RouteEdges(iLayer);
            RemoveUnusedVisibilityGraphEdgesAndNodes(iLayer);
            var layer = _lgData.SkeletonLevels[iLayer];
            Debug.Assert(layer.RoutesAreConsistent());

            SimplifyRoutesOnLevelUntilDone(iLayer);
            Debug.Assert(layer.RoutesAreConsistent());
            FillLevelWithNodesRoutesTryRerouting(iLayer, maxNodesPerTile, maxSegmentsPerTile, increaseNodeQuota);
            RemoveTrajectoriesForEdgesWithHighZoom(iLayer);
            Debug.Assert(layer.RoutesAreConsistent());
            Debug.Assert(HigherTrajectoriesPreserved(iLayer));
            SimplifyRoutesOnLevelUntilDone(iLayer);
            Debug.Assert(layer.RoutesAreConsistent());
            AssignRailsByTrajectories(iLayer);
            Debug.Assert(layer.RoutesAreConsistent());
            if (iLayer < _lgData.Levels.Count - 1)
                CopyGraphToNextLevel(iLayer);
        }

        bool HigherTrajectoriesPreserved(int iLayer) {
            for (int j = 0; j <= iLayer; j++)
                if (!TestTrajectoriesPreserved(iLayer)) return false;
            return true;
        }

        void PrepareNodeBoundariesAndSkeletonOnLayer(int iLayer) {
            var scale = Math.Pow(2, iLayer);
            var rad = NodeDotWidth(scale); // make the BoundaryOnLayer twice larger than the node
            for (int i = 0; i < _lgData.LevelNodeCounts[iLayer]; i++) {
                LgNodeInfo ni = _lgData.SortedLgNodeInfos[i];
                ni.BoundaryOnLayer = CurveFactory.CreateRegularPolygon(_lgLayoutSettings.NumberOfNodeShapeSegs,
                    ni.Center, rad);
            }
            if (iLayer > 0) {
                ModifySkeletonWithNewBoundariesOnLayer(iLayer);
            }
        }

        void ModifySkeletonWithNewBoundariesOnLayer(int iLayer) {
            for (int i = 0; i < _lgData.LevelNodeCounts[iLayer]; i++)
                _lgData.SkeletonLevels[iLayer].PathRouter.ModifySkeletonWithNewBoundaryOnLayer(
                    _lgData.SortedLgNodeInfos[i]);
        }


        void RemoveUnusedVisibilityGraphEdgesAndNodes(int i) {
            _lgData.SkeletonLevels[i].RemoveUnusedGraphEdgesAndNodes();
        }

        void SimplifyRoutesOnLevelUntilDone(int i) {
#if DEBUG
//            ShowOldTrajectories(_lgData.SkeletonLevels[i]);
#endif

            do {
                if (!SimplifyRoutes(i))
                    break;
                UpdateRoutesAfterSimplification(i);
            } while (true);

#if DEBUG
//            ShowOldTrajectories(_lgData.SkeletonLevels[i]);
#endif
        }

        public void InitNodeLabelWidthToHeightRatios(List<double> noldeLabelRatios) {
            for(int i = 0; i< _mainGeometryGraph.Nodes.Count; i++)
            {
                var n = _mainGeometryGraph.Nodes[i];
                var ni = _lgData.GeometryNodesToLgNodeInfos[n];
                if (ni != null)
                {
                    ni.LabelWidthToHeightRatio = noldeLabelRatios[i];
                }
            }
        }

        void RemoveTrajectoriesForEdgesWithHighZoom(int iLevel) {
            // some edges are no longer used since their adjacent nodes moved to the next level.
            // need to delete them from the level EdgeTrajectories
            var edgeTrajectories = _lgData.SkeletonLevels[iLevel].EdgeTrajectories;
            int zoomLevel = _lgData.Levels[iLevel].ZoomLevel;
            var removeList =
                new List<SymmetricTuple<LgNodeInfo>>(
                    edgeTrajectories.Keys.Where(p => p.A.ZoomLevel > zoomLevel || p.B.ZoomLevel > zoomLevel));
            if (removeList.Count > 0) {
                _lgData.SkeletonLevels[iLevel].RemoveSomeEdgeTrajectories(removeList);
                _lgData.Levels[iLevel].RemoveFromRailEdges(removeList);
            }
        }

        void TestRailsForTrajectories(int iLevel) {
            foreach (var path in _lgData.SkeletonLevels[iLevel].EdgeTrajectories.Values) {
                for (int i = 0; i < path.Count - 1; i++) {
                    Rail rail = _lgData.Levels[iLevel].FindRail(path[i], path[i + 1]);
                    //lgData.Levels[iLevel].AddRailToDictionary(rail);
                    //lgData.Levels[iLevel].AddRailToRtree(rail);
                    if (rail == null) {
                        Console.WriteLine("Rail not found for trajectory!");
                    }
                }
            }
        }

        void UpdateRoutesAfterSimplification(int i) {
            var skeletonLevel = _lgData.SkeletonLevels[i];
            foreach (Edge edge in _lgData.Levels[i]._railsOfEdges.Keys) {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                List<Point> path = skeletonLevel.GetTrajectory(s, t);
                if (path == null) continue;

                int startedToSkip = 0;
                for (int j = 1; j < path.Count - 1; j++) {
                    if (skeletonLevel.PathRouter.ContainsVertex(path[j])) {
                        if (startedToSkip > 0) {
                            skeletonLevel.PathRouter.MarkEdgeUsed(path[startedToSkip - 1], path[j]);
                            startedToSkip = 0;
                        }
                        continue;
                    }
                    path.RemoveAt(j);
                    if (startedToSkip == 0) startedToSkip = j;
                    j--;
                }
                if (startedToSkip > 0)
                    skeletonLevel.PathRouter.MarkEdgeUsed(path[startedToSkip - 1], path.Last());

                if (!EdgeIsNew(s, t, i)) {
                    skeletonLevel.MarkEdgesAlongPathAsEdgesOnOldTrajectories(path);
                }
            }
        }

        bool TestTrajectoriesPreserved(int iLevel) {
            if (iLevel == 0) return true;
            foreach (var st in _lgData.SkeletonLevels[iLevel - 1].EdgeTrajectories) {
                LgNodeInfo s = st.Key.A;
                LgNodeInfo t = st.Key.B;
                List<Point> oldPath = st.Value;
                Point ps = oldPath.First();
                List<Point> newPath = _lgData.SkeletonLevels[iLevel].GetTrajectory(s, t);
                Debug.Assert(newPath.Count > 1);
                if (!newPath.First().Equals(ps))
                    newPath.Reverse();

                if (!newPath.First().Equals(ps)) {
                    Console.WriteLine("Endpoint of old path not found on new path!");
                    return false;
                }

                int i;
                int j;
                for (i = 1, j = 1; i < oldPath.Count; i++, j++) {
                    var subdivEdge = new List<Point> {oldPath[i - 1]};
                    while (j < newPath.Count && !oldPath[i].Equals(newPath[j])) {
                        subdivEdge.Add(newPath[j]);
                        j++;
                    }
                    if (j == newPath.Count || !oldPath[i].Equals(newPath[j])) {
                        Console.WriteLine("Point of old path not found on new path!");
                        return false;
                    }
                    subdivEdge.Add(newPath[j]);

                    bool subdivEdgeIsLine = RectSegIntersection.ArePointsOnLine(subdivEdge);
                    if (subdivEdgeIsLine) continue;
                    Console.WriteLine("New path segment is not a line!");
                    return false;
                }
            }
            return true;
        }

/*
        void TestTopRankedEdgeInfosOfTheRail(int iLevel) {
            // sanity check
            foreach (Edge edge in _lgData.Levels[iLevel]._railsOfEdges.Keys) {
                Set<Rail> rails = _lgData.Levels[iLevel]._railsOfEdges[edge];
                if (!rails.Any()) {
                    Console.WriteLine("Corrupt edge rail set");
                }
                foreach (Rail rail in rails) {
                    LgEdgeInfo ei = rail.TopRankedEdgeInfoOfTheRail;
                    if (ei == null) {
                        Console.WriteLine("Corrupt Rail");
                    }

                    if (!_lgData.Levels[iLevel].RailDictionary.ContainsValue(rail)) {
                        Console.WriteLine("used rail not in dictionary!");
                    }
                }
            }
        }
*/


        void CleanUpRails() {
            foreach (LgLevel level in _lgData.Levels) {
                var usedRails = new Set<Rail>();
                foreach (var rails in level._railsOfEdges.Values) {
                    usedRails.InsertRange(rails);
                }

                var unusedRails = new Set<Rail>(level._railDictionary.Values.Where(r => !usedRails.Contains(r)));
                if (!unusedRails.Any()) continue;

                foreach (Rail rail in unusedRails) {
                    level.RemoveRailFromDictionary(rail);
                    level.RemoveRailFromRtree(rail);
                }
            }
        }

        void TestAllEdgesConsistency() {
            foreach (Edge edge in _mainGeometryGraph.Edges) {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                double zoomLevel = Math.Max(s.ZoomLevel, t.ZoomLevel);
                int iLevel = GetIndexByZoomLevel(zoomLevel);
                for (int i = iLevel; i < _lgData.Levels.Count; i++) {
                    Set<Rail> rails = _lgData.Levels[i]._railsOfEdges[edge];
                    foreach (Rail rail in rails) {
                        var rc = rail.Geometry as ICurve;
                        if (rc != null) {
                            var ss = new SymmetricSegment(rc.Start, rc.End);
                            if (!_lgData.Levels[i]._railDictionary.ContainsKey(ss))
                                Debug.Assert(false, string.Format("rail {0} is not in the dictionary", rail));
                        }
                    }
                }
            }
        }

/*
        bool TestEdgeConsistency(Edge edge) {
            LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
            LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
            LgEdgeInfo ei = GeometryEdgesToLgEdgeInfos[edge];
            double zoomLevel = Math.Max(s.ZoomLevel, t.ZoomLevel);
            if (ei.ZoomLevel != zoomLevel) {
                Console.WriteLine("ZoomLevel not set!");
            }
            int iLevel = ViewModel.GetIndexByZoomLevel(zoomLevel);
            for (int i = iLevel; i < _lgData.Levels.Count; i++) {
                Set<Rail> rails = _lgData.Levels[i]._railsOfEdges[edge];
                foreach (Rail rail in rails) {
                    if (!_lgData.SkeletonLevels[i].RailDictionary.ContainsValue(rail)) {
                        Console.WriteLine("used rail not in dictionary!");
                    }
                }
            }
            return true;
        }
*/


        public void SelectAllEdgesIncidentTo(LgNodeInfo nodeInfo) {
            List<Edge> edges = nodeInfo.GeometryNode.Edges.ToList();
            if (!nodeInfo.Selected)
                _lgData.SelectEdges(edges);
            else
                _lgData.UnselectEdges(edges);
        }

        public void SelectEdge(LgEdgeInfo ei) {
            var edges = new List<Edge> {ei.Edge};
            _lgData.SelectEdges(edges);
        }

        public void DeselectAllEdges() {
            _lgData.PutOffAllEdges();
        }


        public void RunMds() {
            //DistributeUniformlyRandom();
            //return;

            var multipliers = new Dictionary<Tuple<Node, Node>, double>();

            //foreach (var s in lgData.SortedLgNodeInfos)
            //{
            //    foreach (var t in lgData.SortedLgNodeInfos)
            //    {
            //        if (s == t) continue;
            //        if (s.ZoomLevel < 2 && t.ZoomLevel < 2)
            //            multipliers.Add(new Tuple<Node, Node>(s.GeometryNode, t.GeometryNode), 10);
            //    }
            //}

            foreach (Edge e in GeometryEdgesToLgEdgeInfos.Keys) {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[e.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[e.Target];
                double zoomLevel = Math.Max(s.ZoomLevel, t.ZoomLevel);

                if (zoomLevel < 5) {
                    double mult = 10; // = Math.Pow(10, lgData.Levels.Last().ZoomLevel - zoomLevel);
                    multipliers.Add(new Tuple<Node, Node>(s.GeometryNode, t.GeometryNode), mult);

                    e.Length = 3;
                    //Math.Pow(1.5, lgData.Levels.Last().ZoomLevel - zoomLevel); //lgData.Levels.Last().ZoomLevel - zoomLevel + 1;
                    //Math.Pow(2, lgData.Levels.Last().ZoomLevel - zoomLevel);
                }
                //else if(Math.Min(s.ZoomLevel, t.ZoomLevel) < 2)
                //{
                //    e.Length = 0.1;
                //}
            }


            var settings = new MdsLayoutSettings {ScaleX = 600, ScaleY = 600, RunInParallel = false, PivotNumber = 100};

            //var mds = new MDSGraphLayoutWeighted(settings, mainGeometryGraph, multipliers);
            var mds = new MdsGraphLayout(settings, _mainGeometryGraph);
            mds.Run();
        }

        public void SelectTopEdgePassingThroughRailWithEndpoint(Rail rail, Set<LgNodeInfo> selectedVnodes) {
            List<Edge> edges =
                GetEdgesPassingThroughRail(rail)
                    .Where(e => selectedVnodes.Contains(_lgData.GeometryNodesToLgNodeInfos[e.Source])
                                || selectedVnodes.Contains(_lgData.GeometryNodesToLgNodeInfos[e.Target])).ToList();
            if (!edges.Any()) return;
            DeselectAllEdges();
            Edge edge = edges.OrderByDescending(e => _lgData.GeometryEdgesToLgEdgeInfos[e].Rank).First();
            SelectEdge(_lgData.GeometryEdgesToLgEdgeInfos[edge]);
        }

        Rectangle GetLabelRectForScale(LgNodeInfo nodeInfo, double scale) {
            double labelHeight = _lgLayoutSettings.NodeLabelHeightInInches*_lgLayoutSettings.DpiX/scale/
                                 FitFactor();
            double labelWidth = labelHeight*nodeInfo.LabelWidthToHeightRatio;

            double nodeDotWidth = GetNodeDotRect(nodeInfo, scale).Width;
            //_lgLayoutSettings.NodeDotWidthInInches * _lgLayoutSettings.DpiX / currentScale;

            Point offset = Point.Scale(labelWidth + nodeDotWidth*1.01, labelHeight + nodeDotWidth*1.01,
                nodeInfo.LabelOffset);

            var d = new Point(0.5*labelWidth, 0.5*labelHeight);

            return new Rectangle(nodeInfo.Center + offset - d, nodeInfo.Center + offset + d);
        }

        Rectangle GetNodeDotRect(LgNodeInfo node, double scale) {
            var rect = new Rectangle(node.Center);
            rect.Pad(NodeDotWidth(scale)/2);
            return rect;
        }

        double NodeDotWidth(double scale) {
            return _lgLayoutSettings.NodeDotWidthInInches*_lgLayoutSettings.DpiX/scale/FitFactor();
        }

        int InsertLabelsGreedily(double scale) {
            int labeledNodes = 0;

            int iLevel = 0;
            while (iLevel < _lgData.Levels.Count - 1 && scale >= _lgData.Levels[iLevel].ZoomLevel) {
                iLevel++;
            }

            var nodes = GetNodeInfosOnLevelLeq(iLevel);

            var labelRTree = new RTree<LgNodeInfo>();


            // insert all nodes inserted before
            foreach (LgNodeInfo node in nodes) {
                // add all node dots
                labelRTree.Add(GetNodeDotRect(node, scale), node);

                if (node.LabelVisibleFromScale > 0 && node.LabelVisibleFromScale < scale) {
                    Rectangle labelRect = GetLabelRectForScale(node, scale);
                    labelRTree.Add(labelRect, node);
                    labeledNodes++;
                }
            }

            foreach (LgNodeInfo node in nodes) {
                if (node.LabelVisibleFromScale > 0 && node.LabelVisibleFromScale < scale) {
                    // already inserted before
                    continue;
                }

                LgNodeInfo.LabelPlacement[] positions =
                {
                    LgNodeInfo.LabelPlacement.Bottom,
                    LgNodeInfo.LabelPlacement.Right,
                    LgNodeInfo.LabelPlacement.Left,
                    LgNodeInfo.LabelPlacement.Top
                };

                bool couldPlace = false;
                var labelRect = new Rectangle();
                foreach (LgNodeInfo.LabelPlacement placement in positions) {
                    node.LabelPosition = placement;
                    labelRect = GetLabelRectForScale(node, scale);
                    if (!labelRTree.IsIntersecting(labelRect)) {
                        couldPlace = true;
                        break;
                    }
                }


                if (!couldPlace) {
                    node.LabelVisibleFromScale = 0;
                }
                else {
                    if (node.LabelVisibleFromScale <= 0)
                        node.LabelVisibleFromScale = scale;
                    labelRTree.Add(labelRect, node);
                    labeledNodes++;
                }
            }

            return labeledNodes;
        }

        class RankComparer : IComparer<Node> {
            readonly Dictionary<Node, LgNodeInfo> _table;

            public RankComparer(Dictionary<Node, LgNodeInfo> geometryNodesToLgNodeInfos) {
                _table = geometryNodesToLgNodeInfos;
            }

            public int Compare(Node a, Node b) {
                return _table[b].Rank.CompareTo(_table[a].Rank);
            }
        }

        public bool RectIsEmptyStartingFromLevel(Rectangle tileBox, int iLevel) {
            for (int i = iLevel; i < _lgData.Levels.Count; i++) {
                if (!_lgData.Levels[i].RectIsEmptyOnLevel(tileBox))
                    return false;
            }
            return true;
        }

        public int GetNumberOfLevels() {
            return _lgData.Levels.Count;
        }

        LgNodeInfo FindClosestNodeInfoForMouseClickOnLevel(Point mouseDownPositionInGraph, int iLevel) {
            var level = _lgData.Levels[iLevel];
            var hitRectWidth = NodeDotWidth(CurrentZoomLevel)/2;
            var rect = new Rectangle(new Size(hitRectWidth, hitRectWidth), mouseDownPositionInGraph);
            var intersected = level.NodeInfoTree.GetAllIntersecting(rect);
            if (!intersected.Any()) return null;
            double dist = double.PositiveInfinity;
            LgNodeInfo closest = null;
            foreach (var ni in intersected) {
                var t = (ni.Center - mouseDownPositionInGraph).LengthSquared;
                if (t < dist) {
                    dist = t;
                    closest = ni;
                }
            }
            return closest;
        }

        LgNodeInfo FindClosestNodeInfoForMouseClickBelowCurrentLevel(Point mouseDownPositionInGraph) {
            var iLevel = _lgData.GetLevelIndexByScale(CurrentZoomLevel);
            for (int i = iLevel + 1; i < _lgData.LevelNodeCounts.Count; i++) {
                LgNodeInfo closest = FindClosestNodeInfoForMouseClickOnLevel(mouseDownPositionInGraph, i);
                if (closest != null) return closest;
            }
            return null;
        }

        public void AnalyzeClick(Point mouseDownPositionInGraph, int downCount) {
            var closest = FindClosestNodeInfoForMouseClickBelowCurrentLevel(mouseDownPositionInGraph);
            if (closest == null) return;
            if (downCount >= 2) {
                SelectAllEdgesIncidentTo(closest);
                var edges = closest.GeometryNode.Edges.ToList();
                _railGraph.Edges.InsertRange(edges);

            }
            closest.Selected = true;
            _lgData.SelectedNodeInfos.Insert(closest);
            _railGraph.Nodes.Insert(closest.GeometryNode);
            RunOnViewChange();            
        }

        
        
        public bool NumberOfNodesOfLastLayerIntersectedRectIsLessThanBound(int iLevel, Rectangle rect, int bound) {
            int lastLevel = GetNumberOfLevels() - 1;
            var level = _lgData.Levels[lastLevel];
            var zoom = Math.Pow(2, iLevel);
            return level.NodeInfoTree.NumberOfIntersectedIsLessThanBound(rect, bound, node=>node.ZoomLevel>zoom);
        }

        public bool TileIsEmpty(Rectangle rectangle) {
            int lastLevel = GetNumberOfLevels() - 1;
            var level = _lgData.Levels[lastLevel];
            LgNodeInfo t;
            return !level.NodeInfoTree.OneIntersecting(rectangle, out t);
        }

        public IEnumerable<Node> GetTileNodes(Tuple<int, int, int> tile) {
            var grid = new GridTraversal(this._mainGeometryGraph.BoundingBox, tile.Item1);
            var rect = grid.GetTileRect(tile.Item2, tile.Item3);
            return
                _lgData.Levels[GetNumberOfLevels() - 1].NodeInfoTree.GetAllIntersecting(rect)
                    .Select(nodeInfo =>  nodeInfo.GeometryNode);
        }
    }
}
