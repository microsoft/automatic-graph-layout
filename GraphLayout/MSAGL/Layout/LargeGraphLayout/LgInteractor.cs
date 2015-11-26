using System;
using System.CodeDom;
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
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.GraphmapsWithMesh;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.LargeGraphLayout.NodeRailLevelCalculator;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Layout.OverlapRemovalFixedSegments;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Miscellaneous.ConstrainedSkeleton;
using Microsoft.Msagl.Miscellaneous.RegularGrid;
using Microsoft.Msagl.Miscellaneous.Routing;
using Microsoft.Msagl.Routing.Visibility;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;

namespace Microsoft.Msagl.Layout.LargeGraphLayout
{
    /// <summary>
    ///     enables to interactively explore a large graph
    /// </summary>
    public class LgInteractor
    {
        const bool ShrinkEdgeLengths = true;
        readonly bool _initFromPrecomputedLgData;
        readonly LgData _lgData;
        readonly LgLayoutSettings _lgLayoutSettings;
        readonly CancelToken _cancelToken;
        readonly GeometryGraph _mainGeometryGraph;

        public Tiling g;

        Dictionary<Point, int> PointToId = new Dictionary<Point, int>();
        public Dictionary<Point, int> locationtoNode = new Dictionary<Point, int>();
        /// <summary>
        /// </summary>
        bool _runInParallel = true;

        RailGraph _railGraph;
        Rectangle _visibleRectangle;

        public Dictionary<LgNodeInfo, LgNodeInfo.LabelPlacement> SelectedNodeLabels = new Dictionary<LgNodeInfo, LgNodeInfo.LabelPlacement>();

        /// <summary>
        ///     constructor
        /// </summary>
        /// <param name="geometryGraph"></param>
        /// <param name="lgLayoutSettings"></param>
        /// <param name="cancelToken"></param>
        public LgInteractor(GeometryGraph geometryGraph, LgLayoutSettings lgLayoutSettings, CancelToken cancelToken)
        {
            _mainGeometryGraph = geometryGraph;
            _lgLayoutSettings = lgLayoutSettings;
            _cancelToken = cancelToken;
            if (geometryGraph.LgData == null)
            {
                _lgData = new LgData(geometryGraph)
                {
                    GeometryNodesToLgNodeInfos = lgLayoutSettings.GeometryNodesToLgNodeInfos
                };

                geometryGraph.LgData = _lgData;
            }
            else
            {
                _initFromPrecomputedLgData = true;
                _lgData = geometryGraph.LgData;
                _lgLayoutSettings.GeometryNodesToLgNodeInfos = _lgData.GeometryNodesToLgNodeInfos;
            }
            // add skeleton levels
            AddSkeletonLevels();
        }

        double CurrentZoomLevel { get; set; }

        /// <summary>
        ///     this graph is currently visible set of nodes and pieces of edges
        /// </summary>
        public RailGraph RailGraph
        {
            get { return _railGraph; }
        }

        /// <summary>
        /// </summary>
        public IDictionary<Edge, LgEdgeInfo> GeometryEdgesToLgEdgeInfos
        {
            get { return _lgData.GeometryEdgesToLgEdgeInfos; }
        }

        public Set<LgNodeInfo> SelectedNodeInfos
        {
            get { return _lgData.SelectedNodeInfos; }
        }


        void AddSkeletonLevels()
        {
            for (int i = _lgData.SkeletonLevels.Count; i < _lgData.Levels.Count(); i++)
            {
                _lgData.SkeletonLevels.Add(new LgSkeletonLevel { ZoomLevel = _lgData.Levels[i].ZoomLevel });
            }
        }

        public Tiling[] calculateGraphsForEachZoomLevel(Tiling g, Dictionary<Node, int> nodeToId)
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            Console.WriteLine("Calculating Graphs For Each ZoomLevel");
            List<Tiling> graphs = new List<Tiling>();
            Tiling OldGraphHolder = null;
            DijkstraAlgo dijkstra = new DijkstraAlgo();
            for (int i = 1; i <= g.maxTheoreticalZoomLevel; i *= 2)
            {
                Tiling graph = new Tiling(g.NumOfnodes, true);
                graph.N = g.N;
                for (int j = 0; j < g.NumOfnodes; j++)
                {
                    graph.VList[j] = new Vertex(g.VList[j].XLoc, g.VList[j].YLoc) { Id = g.VList[j].Id, ZoomLevel = g.VList[j].ZoomLevel };
                    graph.VList[j].TargetX = graph.VList[j].PreciseX = graph.VList[j].XLoc;
                    graph.VList[j].TargetY = graph.VList[j].PreciseY = graph.VList[j].YLoc;
                }

                if (OldGraphHolder != null)
                {
                    for (int j = 0; j < OldGraphHolder.NumOfnodes; j++)
                    {
                        for (int k = 0; k < OldGraphHolder.DegList[j]; k++)
                        {
                            graph.AddEdge(j, OldGraphHolder.EList[j, k].NodeId, OldGraphHolder.EList[j, k].Selected, OldGraphHolder.EList[j, k].Used);
                        }
                    }
                }

                foreach (Edge edge in _mainGeometryGraph.Edges)
                {
                    int sourceZoomLevel = g.VList[nodeToId[edge.Source]].ZoomLevel;
                    int targetZoomLevel = g.VList[nodeToId[edge.Target]].ZoomLevel;
                    if (sourceZoomLevel <= i && targetZoomLevel <= i)
                    {

                        if (sourceZoomLevel < i && targetZoomLevel < i)
                        {
                            //dijkstra.MSAGLAstarShortestPath(g.VList, g.EList, g.DegList, nodeToId[edge.Source], nodeToId[edge.Target], g.NumOfnodes);
                            //foreach (VertexNeighbor vn in dijkstra.Edgelist)
                            //graph.AddEdge(vn.A, g.EList[vn.A, vn.Neighbor].NodeId, g.EList[vn.A, vn.Neighbor].Selected, g.EList[vn.A, vn.Neighbor].Used);
                            int[] p = OldGraphHolder.pathList[edge].ToArray();
                            for (int pindex = 0; pindex < p.Length - 1; pindex++)
                            {
                                int dindex = 0;
                                for (dindex = 0; dindex < g.DegList[p[pindex]]; dindex++)
                                    if (g.EList[p[pindex], dindex].NodeId == p[pindex + 1]) break;
                                graph.AddEdge(p[pindex], p[pindex + 1], g.EList[p[pindex], dindex].Selected, g.EList[p[pindex], dindex].Used);
                            }
                            graph.pathList[edge] = OldGraphHolder.pathList[edge];
                            continue;
                        }
                        else graph.pathList[edge] = new List<int>();
                        List<int> pathvertices = dijkstra.MSAGLAstarShortestPath(g.VList, g.EList, g.DegList, nodeToId[edge.Source], nodeToId[edge.Target], g.NumOfnodes);
                        //List<int> pathvertices = dijkstra.MSAGLAstarShortestPath(g.VList, g.EList, g.DegList, nodeToId[edge.Source], nodeToId[edge.Target], g.NumOfnodes);

                        foreach (int vertexId in pathvertices)
                        {
                            graph.pathList[edge].Add(vertexId);
                            //if (!graph.JunctionToEdgeList.ContainsKey(vertexId))
                            //graph.JunctionToEdgeList[vertexId] = new List<Microsoft.Msagl.Core.Layout.Edge>();
                            //graph.JunctionToEdgeList[vertexId].Add(edge);
                        }
                        foreach (VertexNeighbor vn in dijkstra.Edgelist)
                            graph.AddEdge(vn.A, g.EList[vn.A, vn.Neighbor].NodeId, g.EList[vn.A, vn.Neighbor].Selected, g.EList[vn.A, vn.Neighbor].Used);
                    }
                }
                OldGraphHolder = graph;
                graphs.Add(graph);
            }
            //return graphs.ToArray();
            stopwatch.Stop();
            Console.WriteLine("Graphs for all levels calculated = " + stopwatch.ElapsedMilliseconds);
            stopwatch.Start();

            foreach (Tiling graph in graphs)
            {
                //find all degree two vertices.
                List<int> Deg2Vertices = new List<int>();
                for (int j = graph.N; j < graph.NumOfnodes; j++)
                {
                    if (graph.DegList[j] == 2) Deg2Vertices.Add(j);
                    graph.VList[j].Visited = false;
                }
                foreach (int w in Deg2Vertices)
                {
                    if (graph.VList[w].Visited) continue;
                    //find the one end of the path
                    int currentVertexId = w;
                    int oldVertexId = -1;
                    while (graph.DegList[currentVertexId] == 2 && currentVertexId >= g.N)
                    {
                        int neighbor1 = graph.EList[currentVertexId, 0].NodeId; ;
                        int neighbor2 = graph.EList[currentVertexId, 1].NodeId; ;
                        if (oldVertexId != neighbor1) { oldVertexId = currentVertexId; currentVertexId = neighbor1; }
                        else if (oldVertexId != neighbor2) { oldVertexId = currentVertexId; currentVertexId = neighbor2; }
                    }

                    //find the path from this end 
                    var tempId = oldVertexId;
                    oldVertexId = currentVertexId;
                    currentVertexId = tempId;

                    List<int> path = new List<int>();
                    path.Add(oldVertexId);
                    path.Add(currentVertexId);
                    graph.VList[oldVertexId].Visited = true;
                    graph.VList[currentVertexId].Visited = true;

                    //find the path and add it into a list
                    while (graph.DegList[currentVertexId] == 2 && currentVertexId >= g.N)
                    {
                        int neighbor1 = graph.EList[currentVertexId, 0].NodeId; ;
                        int neighbor2 = graph.EList[currentVertexId, 1].NodeId; ;
                        if (oldVertexId != neighbor1) { oldVertexId = currentVertexId; currentVertexId = neighbor1; }
                        else if (oldVertexId != neighbor2) { oldVertexId = currentVertexId; currentVertexId = neighbor2; }
                        path.Add(currentVertexId);
                        graph.VList[currentVertexId].Visited = true;
                    }

                    int[] pathVertices = path.ToArray();
                    Point[] PointList = new Point[path.Count];
                    int q = 0;
                    foreach (int vertex in path)
                    {
                        PointList[q++] = new Point(graph.VList[vertex].XLoc, graph.VList[vertex].YLoc);
                        if (MsaglUtilities.EucledianDistance(99, 60, graph.VList[vertex].XLoc, graph.VList[vertex].YLoc) < 2)
                            Console.WriteLine();
                    }
                    LocalModifications.PolygonalChainSimplification(PointList, 0, PointList.Length - 1, 100);

                    //Modify graph according to the simplified path
                    for (int currentPoint = 0; currentPoint < PointList.Length - 1; )
                    {
                        int nextPoint = currentPoint + 1;
                        for (; nextPoint < PointList.Length; nextPoint++)
                        {
                            if (PointList[nextPoint].X == -1)
                            {
                                //////graph.RemoveEdge(pathVertices[nextPoint], pathVertices[nextPoint-1]);
                                continue;
                            }
                            else
                            {
                                //////graph.RemoveEdge(pathVertices[nextPoint], pathVertices[nextPoint - 1]);
                                break;
                            }
                        }
                        if (nextPoint < PointList.Length)
                        {
                            //////graph.AddEdge(pathVertices[nextPoint], pathVertices[currentPoint]);
                            int left = currentPoint;
                            int right = nextPoint;
                            while (graph.noCrossings(pathVertices, pathVertices[left], pathVertices[right]) == false && left < right)
                            {
                                left++; right--;
                            }
                            if (left < right)
                            {
                                //connect each path vertrex in between to this path
                                for (int index = left + 1; index < right; index++)
                                {
                                    Vertex intermediateVertex = graph.VList[pathVertices[index]];
                                    Vertex leftVertex = graph.VList[pathVertices[left]];
                                    Vertex rightVertex = graph.VList[pathVertices[right]];
                                    Microsoft.Msagl.Core.Geometry.Point p = PointToSegmentDistance.getClosestPoint
                                                                            (leftVertex, rightVertex, intermediateVertex);
                                    intermediateVertex.PreciseX = p.X;
                                    intermediateVertex.PreciseY = p.Y;

                                    intermediateVertex.LeftX = leftVertex.XLoc;
                                    intermediateVertex.LeftY = leftVertex.YLoc;
                                    intermediateVertex.RightX = rightVertex.XLoc;
                                    intermediateVertex.RightY = rightVertex.YLoc;

                                    //foreach (Edge edge in graph.JunctionToEdgeList[intermediateVertex.Id])
                                    //{
                                    //graph.pathList[edge].Remove(intermediateVertex.Id);
                                    //}
                                }
                            }
                            currentPoint = nextPoint;
                        }
                    }
                }
            }

            Tiling[] allGraphs = graphs.ToArray();


            /*
            int gIndex = 0;
            for (int i = 1; i <= g.maxTheoreticalZoomLevel; i *= 2)
            {
                Tiling graph = allGraphs[gIndex];

                foreach (Edge edge in _mainGeometryGraph.Edges)
                {
                    int sourceZoomLevel = g.VList[nodeToId[edge.Source]].ZoomLevel;
                    int targetZoomLevel = g.VList[nodeToId[edge.Target]].ZoomLevel;
                    if (sourceZoomLevel <= i && targetZoomLevel <= i)
                    {

                        //graph.pathList[edge].Clear();
                        if (sourceZoomLevel < i && targetZoomLevel < i)
                        {
                            graph.pathList[edge] = allGraphs[gIndex - 1].pathList[edge];
                            continue;
                        }
                        else graph.pathList[edge].Clear();
 
                        List<int> pathvertices = dijkstra.MSAGLAstarShortestPath(g.VList, g.EList, g.DegList, nodeToId[edge.Source], nodeToId[edge.Target], g.NumOfnodes);

                        foreach (int vertexId in pathvertices)                        
                            graph.pathList[edge].Add(vertexId); 
                        
                        //foreach (VertexNeighbor vn in dijkstra.Edgelist)
                            //graph.AddEdge(vn.A, g.EList[vn.A, vn.Neighbor].NodeId, g.EList[vn.A, vn.Neighbor].Selected, g.EList[vn.A, vn.Neighbor].Used);
                    }
                }
                gIndex++;
            }
             * */

            for (int index = 0; index < allGraphs.Length; index++)
            {
                for (int nodeindex = allGraphs[index].N; nodeindex < allGraphs[index].NumOfnodes; nodeindex++)
                {
                    if (index < allGraphs.Length - 1)
                    {
                        allGraphs[index].VList[nodeindex].TargetX = allGraphs[index + 1].VList[nodeindex].PreciseX;
                        allGraphs[index].VList[nodeindex].TargetY = allGraphs[index + 1].VList[nodeindex].PreciseY;
                    }
                    else
                    {
                        allGraphs[index].VList[nodeindex].TargetX = allGraphs[index].VList[nodeindex].PreciseX;
                        allGraphs[index].VList[nodeindex].TargetY = allGraphs[index].VList[nodeindex].PreciseY;
                    }
                }
            }
            stopwatch.Stop();
            Console.WriteLine("Done Simplification = " + stopwatch.ElapsedMilliseconds);

            return allGraphs;
        }
        public void RunForMsaglFiles()
        {
            Dictionary<Node, int> nodeToId;

            var g = TryCompetitionMeshApproach(out nodeToId);

            var stopwatch = new Stopwatch();

            Tiling[] graphs = calculateGraphsForEachZoomLevel(g, nodeToId);

            stopwatch.Start();
            RenderGraphForEachZoomLevel(graphs, nodeToId);
            stopwatch.Stop();
            Console.WriteLine("Time for RenderGraphForEachZoomLevel = " + stopwatch.ElapsedMilliseconds);

            //g = graphs[0];
            //RenderGraph( g,   nodeToId);

        }


        public void RenderGraphForEachZoomLevel(Tiling[] g, Dictionary<Node, int> nodeToId)
        {

            //until all edges are added create a layer and add vertices one after another
            int layer = 0;
            int plottedNodeCount = 0;
            List<Node> nodes = new List<Node>();




            foreach (var node in _mainGeometryGraph.Nodes)
                _lgData.GeometryNodesToLgNodeInfos[node].ZoomLevel = 100;

            for (int i = 0; i < g.Length; i++)
            {

                layer++;
                var level = CreateLevel(layer);

                //set the zoomlevel of the nodes
                for (int j = 0; j < g[i].N; j++)
                {
                    if (g[i].DegList[j] > 0)
                    {
                        Node nd = idToNode[g[i].VList[j].Id];
                        if (_lgData.GeometryNodesToLgNodeInfos[nd].ZoomLevel == 100)
                        {                            
                            _lgData.GeometryNodesToLgNodeInfos[nd].ZoomLevel = layer;
                            _lgLayoutSettings.GeometryNodesToLgNodeInfos[nd].ZoomLevel = layer;
                        }
                    }
                }
                
                //add the edges to the current level for this graph
                Dictionary<Node, int> nodeId = new Dictionary<Node, int>();//-remove later - not needed

                Set<Rail> railsOfEdge = new Set<Rail>();
                foreach (Edge edge in _mainGeometryGraph.Edges)
                {
                    //if (!level._railsOfEdges.ContainsKey(edge))
                    {
                        railsOfEdge = MsaglAddRailsOfEdge(level, g[i], edge, nodeId);
                        if (railsOfEdge.Count > 0)
                        {
                            level._railsOfEdges[edge] = railsOfEdge;
                        }
                    }
                }

            }
            /*//why so many nodes are in the top level?
            for (int j = 0; j < _lgData.SortedLgNodeInfos.Count; j++)
            {
                Node nd = _lgData.SortedLgNodeInfos[j].GeometryNode;
                _lgData.SortedLgNodeInfos[j].ZoomLevel = _lgData.GeometryNodesToLgNodeInfos[nd].ZoomLevel;
            }
            */
            /*
            foreach (LgNodeInfo node in _lgData.SortedLgNodeInfos)
            {
                _lgData.SortedLgNodeInfos[plottedNodeCount].ZoomLevel = layer;
                _lgData.GeometryNodesToLgNodeInfos[node.GeometryNode].ZoomLevel = layer;

                nodes.Add(node.GeometryNode);


                int graphLayer = layer;
                if (layer >= g.Length) graphLayer = g.Length - 1;
                double nodeOnlyZoomLevel = Math.Log(g[graphLayer].VList[nodeToId[node.GeometryNode]].ZoomLevel, 2);

                //Console.WriteLine("vertex level " + node.GeometryNode + " : " + layer + " " + nodeOnlyZoomLevel+" " +g[graphLayer].VList[nodeToId[node.GeometryNode]].ZoomLevel);


                while (nodeOnlyZoomLevel > layer || MsaglNodeSuccessfullyPlotted(g[graphLayer], level, graphLayer, plottedNodeCount, nodes, nodeToId) == false)
                {

                    layer++;
                    level = CreateLevel(layer);
                    _lgData.SortedLgNodeInfos[plottedNodeCount].ZoomLevel = layer;
                    _lgData.GeometryNodesToLgNodeInfos[node.GeometryNode].ZoomLevel = layer;
                    _lgLayoutSettings.GeometryNodesToLgNodeInfos[node.GeometryNode].ZoomLevel = layer;
                    node.ZoomLevel = layer;

                    if (layer >= g.Length) graphLayer = g.Length - 1;
                    else graphLayer = layer;

                }


                plottedNodeCount++;
            }


            //adjust levels
            for (int i = 0; i < layer; i++)
            {
                //check if some layer i is empty
                if (_lgData.Levels[i]._railDictionary.Count == 0)
                {
                    foreach (Edge e in _lgData.Levels[i - 1]._railsOfEdges.Keys)
                    {
                        _lgData.Levels[i]._railsOfEdges[e] = _lgData.Levels[i - 1]._railsOfEdges[e];
                    }
                    foreach (SymmetricSegment s in _lgData.Levels[i - 1]._railDictionary.Keys)
                    {
                        _lgData.Levels[i]._railDictionary[s] = _lgData.Levels[i - 1]._railDictionary[s];
                    }
                    _lgData.Levels[i]._railTree = _lgData.Levels[i - 1]._railTree;
                }

            }
            _lgLayoutSettings.maximumNumOfLayers = g.Length - 1;
            */
            Console.WriteLine("MAX Num of Level " + layer);
            /*
            level.RunLevelStatistics(_mainGeometryGraph.Nodes);

            double ink = 0;
            foreach (SymmetricSegment s in level.RailDictionary.Keys)
            {
                ink += Math.Sqrt((s.A.X - s.B.X) * (s.A.X - s.B.X) + (s.A.Y - s.B.Y) * (s.A.Y - s.B.Y));
            }
            Console.WriteLine("Total Rails " + level.RailDictionary.Keys.Count);
            Console.WriteLine("Total Ink " + ink);
            */
        }

        /*
        public void RenderGraphForEachZoomLevel(Tiling[] g, Dictionary<Node, int> nodeToId)
        {
       
            //until all edges are added create a layer and add vertices one after another
            int layer = 0;
            int plottedNodeCount = 0;
            List<Node> nodes = new List<Node>();
            var level = CreateLevel(layer);

            foreach (var node in _mainGeometryGraph.Nodes)
                _lgData.GeometryNodesToLgNodeInfos[node].ZoomLevel = 100;

            

            foreach (LgNodeInfo node in _lgData.SortedLgNodeInfos)
            {
                _lgData.SortedLgNodeInfos[plottedNodeCount].ZoomLevel = layer;
                _lgData.GeometryNodesToLgNodeInfos[node.GeometryNode].ZoomLevel = layer;
                
                nodes.Add(node.GeometryNode);
                

                int graphLayer = layer;
                if (layer >= g.Length)  graphLayer = g.Length - 1;
                double nodeOnlyZoomLevel = Math.Log(g[graphLayer].VList[nodeToId[node.GeometryNode]].ZoomLevel,2);
                
                //Console.WriteLine("vertex level " + node.GeometryNode + " : " + layer + " " + nodeOnlyZoomLevel+" " +g[graphLayer].VList[nodeToId[node.GeometryNode]].ZoomLevel);


                while (nodeOnlyZoomLevel > layer || MsaglNodeSuccessfullyPlotted(g[graphLayer], level, graphLayer, plottedNodeCount, nodes, nodeToId) == false)
                {
                    
                    layer++;
                    level = CreateLevel(layer);
                    _lgData.SortedLgNodeInfos[plottedNodeCount].ZoomLevel = layer;
                    _lgData.GeometryNodesToLgNodeInfos[node.GeometryNode].ZoomLevel = layer;
                    _lgLayoutSettings.GeometryNodesToLgNodeInfos[node.GeometryNode].ZoomLevel = layer;
                    node.ZoomLevel = layer;
                    
                    if (layer >= g.Length) graphLayer = g.Length - 1;
                    else graphLayer = layer;
                    
                }
                plottedNodeCount++;
            }

            _lgLayoutSettings.maximumNumOfLayers = g.Length-1;
            
            Console.WriteLine("MAX Num of Level " + layer);
             
            level.RunLevelStatistics(_mainGeometryGraph.Nodes);

            double ink = 0;
            foreach (SymmetricSegment s in level.RailDictionary.Keys)
            {
                ink += Math.Sqrt((s.A.X - s.B.X) * (s.A.X - s.B.X) + (s.A.Y - s.B.Y) * (s.A.Y - s.B.Y));
            }
            Console.WriteLine("Total Rails " + level.RailDictionary.Keys.Count);
            Console.WriteLine("Total Ink " + ink);
             
        }
*/
        public Dictionary<int, Node> idToNode;

        private Tiling TryCompetitionMeshApproach(out Dictionary<Node, int> nodeToId)
        {
            Console.WriteLine("Nodes = " + _mainGeometryGraph.Nodes.Count + "Edges = " + _mainGeometryGraph.Edges.Count);

            //create a set of nodes and empty edges 
            g = new Tiling(_mainGeometryGraph.Nodes.Count, true);

            idToNode = new Dictionary<int, Node>();
            nodeToId = new Dictionary<Node, int>();

            //this._lgLayoutSettings.MaxNumberOfNodesPerTile = 10;
            //this._lgLayoutSettings.MaxNumberOfRailsPerTile = 40;
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            CreateConnectedGraphs();
            FillGeometryNodeToLgInfosTables();
            LevelCalculator.RankGraph(_lgData, _mainGeometryGraph);
            LayoutTheWholeGraph();
            int maxY;
            var maxX = CreateNodePositions(g, nodeToId, idToNode, out maxY);
            stopwatch.Stop();
            Console.WriteLine("Conected Graph and MDS Time = " + stopwatch.ElapsedMilliseconds);

            //create competitionMesh of G //less than a minute for 1500 vertices and 5000 edges
            /*
            * CCM:CCMWLP
            * 
            seed   Spanning R      Avg Spanning R
            1     1.67:1.45 		1.238:1.225
            2     1.61:1.41 		1.232:1.212
            3     1.80:1.71		1.240:1.228
            4     1.78:1.49		1.241:1.225
            */

            stopwatch.Start();
            //MeshCreator.CreateMeshByLayers(g, idToNode, maxX, maxY);
            //MeshCreator.CreateCompetitionMesh(g, idToNode, maxX, maxY);
            MeshCreator.FastCompetitionMesh(g, idToNode, maxX, maxY, locationtoNode);
            //MeshCreator.CreateCompetitionMeshWithLeftPriority(g, idToNode, maxX, maxY);
            stopwatch.Stop();
            Console.WriteLine("MeshCreation Time = " + stopwatch.ElapsedMilliseconds);


            //Compute Spanning ratio
            //ComputeSpanningRatio(g);

            stopwatch.Start();
            //Create Detour //less than a minute for 1500 vertices and 5000 edges
            Console.WriteLine("Computing Detour Around Vertex");
            g.MsaglDetour(idToNode);
            g.CreateNodeTreeEdgeTree();
            stopwatch.Stop();
            Console.WriteLine("Detour Creation Time = " + stopwatch.ElapsedMilliseconds);

            //Compute the EdgeList for each vertex
            //var AdjacencyList = BuildAdjacencyListFromEdgeList();
            Dictionary<Node, List<Edge>> AdjacencyList = null; // BuildAdjacencyListFromEdgeList();

            for (int iteration = 1; iteration < 2; iteration++)
            {



                stopwatch.Start();
                //RouteByLayers();
                stopwatch.Stop();
                Console.WriteLine("Lev's = " + stopwatch.ElapsedMilliseconds);

                /*
                //UPDATE ACCORDING TO DIJKSTRA            
                Console.WriteLine("Computing Edge Routes");
                stopwatch.Start();
                var g1 = ComputeEdgeRoutes(g, nodeToId, AdjacencyList);
                stopwatch.Stop();
                Console.WriteLine("Edge Routing Time = " + stopwatch.ElapsedMilliseconds);
                
 
                g = g1;
                */

                Console.WriteLine("Removing Deg 2 junctions");
                //Remove Deg 2 Nodes when possible //less than a minute for 1500 vertices and 5000 edges
                stopwatch.Start();
                g.MsaglRemoveDeg2(idToNode);
                stopwatch.Stop();
                Console.WriteLine("Deg 2 removal Time = " + stopwatch.ElapsedMilliseconds);

                stopwatch.Start();
                //PlanarGraphUtilities.TransformToGeometricPlanarGraph(g);
                stopwatch.Stop();
                Console.WriteLine("Planar Graph Building Time = " + stopwatch.ElapsedMilliseconds);

                stopwatch.Start();
                //PlanarGraphUtilities.RemoveLongEdgesFromThinFaces(g);
                stopwatch.Stop();
                Console.WriteLine("Thin Face Removal Time = " + stopwatch.ElapsedMilliseconds);


                //Move the points towards median
                //Console.WriteLine("Moving junctions to minimize ink");
                stopwatch.Start();
                //LocalModifications.MsaglStretchAccordingToZoomLevel(g, idToNode);
                LocalModifications.MsaglMoveToMedian(g, idToNode);
                stopwatch.Stop();
                Console.WriteLine("Ink Minimization Time = " + stopwatch.ElapsedMilliseconds);


            }

            return g;
        }

        private Dictionary<Node, List<Edge>> BuildAdjacencyListFromEdgeList()
        {
            Dictionary<Node, List<Edge>> AdjacencyList = new Dictionary<Node, List<Edge>>();
            foreach (Node node in _mainGeometryGraph.Nodes)
            {
                List<Edge> EdgeList = new List<Edge>();
                foreach (Edge edge in _mainGeometryGraph.Edges)
                {
                    if (edge.Source.ToString().Equals(node.ToString()) || edge.Target.ToString().Equals(node.ToString()))
                    {
                        if (_lgData.GeometryNodesToLgNodeInfos[edge.Source].ZoomLevel <=
                            _lgData.GeometryNodesToLgNodeInfos[node].ZoomLevel &&
                            _lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel <=
                            _lgData.GeometryNodesToLgNodeInfos[node].ZoomLevel)
                            EdgeList.Add(edge);
                    }
                }
                AdjacencyList.Add(node, EdgeList);
            }
            return AdjacencyList;
        }

        public void RouteByLayers()//int maxNodesPerTile, int maxSegmentsPerTile, double increaseNodeQuota)
        {
            FillLevelsWithNodesOnly(int.MaxValue);

            InitRailsOfEdgesEmpty();
            //InitNodeLabelWidthToHeightRatios();
            AddSkeletonLevels();
            for (int i = 0; i < _lgData.Levels.Count; i++)
                RemoveOverlapsAndRouteForLayer(int.MaxValue, int.MaxValue, int.MaxValue, i);


            _lgData.CreateLevelNodeTrees(NodeDotWidth(1));
            //LabelingOfOneRun();
#if DEBUG
            TestAllEdgesConsistency();
#endif
        }

        private int CreateNodePositions(Tiling g, Dictionary<Node, int> nodeToId, Dictionary<int, Node> idToNode, out int maxY)
        {
            //PointSet ps = new PointSet(_mainGeometryGraph.Nodes.Count);
            //find maxX and maxY
            int maxX = 0;
            maxY = 0;

            /*
            int nodeIndex = 0;
            foreach (Node node in _mainGeometryGraph.Nodes)
            {
                node.Center = new Point((int) ps.Pt[nodeIndex].X, (int) ps.Pt[nodeIndex].Y);                
                g.VList[nodeIndex] = new Vertex((int) ps.Pt[nodeIndex].X, (int) ps.Pt[nodeIndex].Y) {Id = nodeIndex};
                nodeToId.Add(node, nodeIndex);
                idToNode.Add(nodeIndex, node);
                if (node.Center.X > maxX) maxX = ps.Pt[nodeIndex].X + 5; //(int)node.Center.X+10;
                if (node.Center.Y > maxY) maxY = ps.Pt[nodeIndex].Y + 5; //(int)node.Center.Y+10;
                nodeIndex++;
            }
            */

            //check if negative coordinate
            double minX = 0;
            double minY = 0;
            foreach (Node node in _mainGeometryGraph.Nodes)
            {
                if (node.Center.X < minX) minX = node.Center.X;
                if (node.Center.Y < minY) minY = node.Center.Y;
            }

            //shift to positive coordinate
            int nodeIndex = 0;
            foreach (Node node in _mainGeometryGraph.Nodes)
            {
                nodeToId.Add(node, nodeIndex);
                idToNode.Add(nodeIndex, node);
                node.Center = new Point((int)(node.Center.X - minX + 5), (int)(node.Center.Y - minY + 5));
                if (node.Center.X > maxX) maxX = (int)node.Center.X;
                if (node.Center.Y > maxY) maxY = (int)node.Center.Y;
                nodeIndex++;
            }

            //bound the graph inside a 1000 by 1000 box
            foreach (Node node in _mainGeometryGraph.Nodes)
            {
                var x =  (node.Center.X / maxX) * 1000;
                var y = (node.Center.Y / maxY) * 1000;
                Point p = new Point((int)x, (int)y);


                x = (int)x;
                y = (int)y;
                while (locationtoNode.ContainsKey(p))
                {
                    x++;
                    y++;
                    p = new Point((int)x, (int)y);
                }
                node.Center = p;

                if (!locationtoNode.ContainsKey(p)) locationtoNode.Add(p, nodeToId[node]);
                g.VList[nodeToId[node]] = new Vertex((int)p.X, (int)p.Y) { Id = nodeToId[node] };
            }
            /*
            //make sure that no two nodes are on the same position
            for (int i = 0; i < g.NumOfnodes;i++ )
            {
                while (g.GetNodeOtherthanThis(i, g.VList[i].XLoc, g.VList[i].YLoc) >= 0)
                {
                    g.VList[i].XLoc++;
                    g.VList[i].YLoc++;
                }
            }
             */
            maxX = 0;
            maxY = 0;
            //scale the node positions to create intermediate gaps
            for (int i = 0; i < g.NumOfnodes; i++)
            {

                g.VList[i].XLoc *= 3;
                g.VList[i].YLoc *= 3;
                idToNode[i].Center = new Point(g.VList[i].XLoc, g.VList[i].YLoc);
                if (g.VList[i].XLoc > maxX) maxX = g.VList[i].XLoc;
                if (g.VList[i].YLoc > maxY) maxY = g.VList[i].YLoc;
            }
            locationtoNode = new Dictionary<Point, int>();
            for (int i = 0; i < g.NumOfnodes; i++)
            {
                Point p = new Point(g.VList[i].XLoc, g.VList[i].YLoc);
                locationtoNode.Add(p, i);
            }
            int index = 0;
            foreach (Node node in _mainGeometryGraph.Nodes)
            {
                Point p = new Point(g.VList[index].XLoc, g.VList[index].YLoc);
                g.nodeToLoc.Add(node, p);
                index++;
            }

            _lgData.SortedLgNodeInfos = new List<LgNodeInfo>();

            //FillGeometryNodeToLgInfosTables();

            //CreateConnectedGraphs();
            //LevelCalculator.RankGraph(_lgData, _mainGeometryGraph);
            LevelCalculator.SetNodeZoomLevelsAndRouteEdgesOnLevels(_lgData, _mainGeometryGraph, _lgLayoutSettings);

            foreach (var node in _lgData.SortedLgNodeInfos)
            {
                int l = (int)node.ZoomLevel;
                g.VList[nodeToId[node.GeometryNode]].ZoomLevel = l;
                if (g.maxTheoreticalZoomLevel < l) g.maxTheoreticalZoomLevel = l;
            }
            _lgData.Levels.Clear();


            _mainGeometryGraph.UpdateBoundingBox();
            return maxX;
        }

        private static void ComputeSpanningRatio(Tiling g)
        {
            double spanningratio = 0;
            double averageratio = 0;
            for (int i = 0; i < g.N; i++)
            {
                for (int j = i + 1; j < g.N; j++)
                {
                    DijkstraAlgo dijkstra = new DijkstraAlgo();
                    dijkstra.MSAGLSelectShortestPath(g.VList, g.EList, g.DegList, i, j, g.NumOfnodes);
                    double baseDistance = g.GetEucledianDist(i, j);
                    if (dijkstra.Distance / baseDistance > spanningratio) spanningratio = dijkstra.Distance / baseDistance;
                    averageratio += dijkstra.Distance / baseDistance;
                }
            }
            Console.WriteLine("Spanning Ratio = " + spanningratio);
            Console.WriteLine("Average Spanning Ratio = " + (averageratio / (g.N * (g.N + 1) / 2)));
        }

        private Tiling ComputeEdgeRoutes(Tiling g, Dictionary<Node, int> nodeId, Dictionary<Node, List<Edge>> AdjacencyList)
        {
            Tiling g1 = new Tiling(g.NumOfnodes, true);
            g1.N = g.N;
            g1.isPlanar = g.isPlanar;
            g1.NumOfnodesBeforeDetour = g.NumOfnodesBeforeDetour;
            g1.nodeTree = g.nodeTree;
            g1.maxTheoreticalZoomLevel = g.maxTheoreticalZoomLevel;
            for (int i = 0; i < g.NumOfnodes; i++)
                g1.VList[i] = new Vertex(g.VList[i].XLoc, g.VList[i].YLoc) { Id = g.VList[i].Id, ZoomLevel = g.VList[i].ZoomLevel };


            /*
            Parallel.ForEach(_mainGeometryGraph.Edges, (edge) =>
            {

                //List<Edge> processedEdges = new List<Edge>();
                DijkstraAlgo dijkstra = new DijkstraAlgo();

                dijkstra.MSAGLGreedy(g.VList, g.EList, g.DegList, nodeId[edge.Source],
                    nodeId[edge.Target],
                    g.NumOfnodes);
                // dijkstra.MSAGLAstarShortestPath(g.VList, g.EList, g.DegList, nodeId[edge.Source],nodeId[edge.Target],g.NumOfnodes);
                foreach (VertexNeighbor vn in dijkstra.Edgelist)
                    g1.AddEdge(vn.A, g.EList[vn.A, vn.Neighbor].NodeId, g.EList[vn.A, vn.Neighbor].Selected,
                        g.EList[vn.A, vn.Neighbor].Used);

            });*/

            //List<Edge> processedEdges = new List<Edge>();
            DijkstraAlgo dijkstra = new DijkstraAlgo();
            //Dictionary<Edge, bool> processedEdges = new Dictionary<Edge, bool>();
            int counter = 0;
            foreach (var edge in _mainGeometryGraph.Edges)
            {
                counter++;
                //if (processedEdges.ContainsKey(edge)) continue;
                //else processedEdges.Add(edge, true);
                if (counter % 10000 == 0) Console.WriteLine(".");

                dijkstra.MSAGLGreedy(g.VList, g.EList, g.DegList, nodeId[edge.Source],
                   nodeId[edge.Target],
                   g.NumOfnodes);
                // dijkstra.MSAGLAstarShortestPath(g.VList, g.EList, g.DegList, nodeId[edge.Source],nodeId[edge.Target],g.NumOfnodes);
                foreach (VertexNeighbor vn in dijkstra.Edgelist)
                    g1.AddEdge(vn.A, g.EList[vn.A, vn.Neighbor].NodeId, g.EList[vn.A, vn.Neighbor].Selected, g.EList[vn.A, vn.Neighbor].Used);

            }

            /*
               Dictionary<Edge, bool> processedEdges = new Dictionary<Edge, bool>();
               foreach (var node in _mainGeometryGraph.Nodes)
               {
 
                   dijkstra.MSAGLAstarSSSP(g.VList, g.EList, g.DegList, nodeId[node], nodeId,
                       _mainGeometryGraph, g.NumOfnodesBeforeDetour,
                       g.NumOfnodes, g, g1);

               
               }
           */
            for (int i = 0; i < g1.NumOfnodes; i++)
                if (g1.DegList[i] == 0) g1.VList[i].Invalid = true;


            return g1;
        }




        public Dictionary<SymmetricSegment, Rail> Segs = new Dictionary<SymmetricSegment, Rail>();

        GreedyNodeRailLevelCalculator calc;

        public int[] graphLayer = new int[100];

        public bool MsaglNodeSuccessfullyPlotted(Tiling g, LgLevel level, int currentGraphLayer, int nodeToBePlotted, IEnumerable<Node> nodes, Dictionary<Node, int> nodeId)
        {

            graphLayer[level.ZoomLevel] = currentGraphLayer;
            Set<Rail> railsOfEdge = new Set<Rail>();

            foreach (Edge edge in _mainGeometryGraph.Edges)
            {
                if (!level._railsOfEdges.ContainsKey(edge))
                {
                    railsOfEdge = MsaglAddRailsOfEdge(level, g, edge, nodeId);
                    if (railsOfEdge.Count > 0)
                    {
                        level._railsOfEdges[edge] = railsOfEdge;
                    }
                }
            }


            var bbox = GetLargestTile();
            GridTraversal grid = new GridTraversal(bbox, level.ZoomLevel);
            GreedyNodeRailLevelCalculator calc = new GreedyNodeRailLevelCalculator(_lgData.SortedLgNodeInfos);


            calc.MaxAmountRailsPerTile = _lgLayoutSettings.MaxNumberOfRailsPerTile;


            SymmetricSegment s;
            List<SymmetricSegment> newSegments = new List<SymmetricSegment>();
            foreach (var rail in level.RailDictionary.Values)
            {
                s = new SymmetricSegment(rail.Left, rail.Right);

                if (!Segs.ContainsKey(s))
                {
                    Segs.Add(s, rail);
                    newSegments.Add(s);
                }
            }
            //Console.WriteLine("Total Rails = " + level.RailDictionary.Values.Count);

            Boolean allInserted = true;
            foreach (var seg in newSegments)
                if (calc.IfCanInsertLooseSegmentUpdateTiles(seg, grid) == false)
                {
                    allInserted = false;
                    //break;
                }

            if (!allInserted && level.ZoomLevel - 1 >= 0)
            {

                foreach (Edge edge in level._railsOfEdges.Keys)
                    level._railsOfEdges[edge].Clear();
                foreach (SymmetricSegment sg in newSegments)
                    Segs.Remove(sg);
                foreach (Edge edge in _lgData.Levels[level.ZoomLevel - 1]._railsOfEdges.Keys)
                {
                    railsOfEdge = _lgData.Levels[level.ZoomLevel - 1]._railsOfEdges[edge];
                    level._railsOfEdges[edge] = railsOfEdge;
                }

                return false;
            }

            return true;//level.QuotaSatisfied(nodes, this._lgLayoutSettings.MaxNumberOfNodesPerTile, this._lgLayoutSettings.MaxNumberOfRailsPerTile);
        }
        public Dictionary<Rail, List<Edge>> RailToEdges = new Dictionary<Rail, List<Edge>>();

        private Set<Rail> MsaglAddRailsOfEdge(LgLevel level, Tiling g, Edge edge, Dictionary<Node, int> nodeId)
        {
            //if (_lgData.GeometryNodesToLgNodeInfos[edge.Source].ZoomLevel > level.ZoomLevel ||
            // _lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel > level.ZoomLevel)
            //return new Set<Rail>();

            if (!g.pathList.ContainsKey(edge)) return new Set<Rail>();

            _lgData.GeometryEdgesToLgEdgeInfos[edge] = new LgEdgeInfo(edge)
            {
                Rank = Math.Min(_lgData.GeometryNodesToLgNodeInfos[edge.Source].ZoomLevel,
                        _lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel),
                ZoomLevel =
                    Math.Max(_lgData.GeometryNodesToLgNodeInfos[edge.Source].ZoomLevel,
                        _lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel)
            };


            var railsOfEdge = new Set<Rail>();


            int[] pathVertices = g.pathList[edge].ToArray();
            for (int index = 0; index < pathVertices.Length - 1; index++)
            {
                var a = new Point(g.VList[pathVertices[index]].PreciseX, g.VList[pathVertices[index]].PreciseY);
                var b = new Point(g.VList[pathVertices[index + 1]].PreciseX, g.VList[pathVertices[index + 1]].PreciseY);
                var initial_a = new Point(g.VList[pathVertices[index]].XLoc, g.VList[pathVertices[index]].YLoc);
                var initial_b = new Point(g.VList[pathVertices[index + 1]].XLoc, g.VList[pathVertices[index + 1]].YLoc);
                var target_a = new Point(g.VList[pathVertices[index]].TargetX, g.VList[pathVertices[index]].TargetY);
                var target_b = new Point(g.VList[pathVertices[index + 1]].TargetX, g.VList[pathVertices[index + 1]].TargetY);

                var left = new Point(0, 0);
                var right = new Point(0, 0);

                if (g.VList[pathVertices[index]].LeftX != 0 && g.VList[pathVertices[index]].RightX != 0)
                {
                    left = new Point(g.VList[pathVertices[index]].LeftX, g.VList[pathVertices[index]].LeftY);
                    right = new Point(g.VList[pathVertices[index]].RightX, g.VList[pathVertices[index]].RightY);
                }
                else if (g.VList[pathVertices[index + 1]].LeftX != 0 && g.VList[pathVertices[index + 1]].RightX != 0)
                {
                    left = new Point(g.VList[pathVertices[index + 1]].LeftX, g.VList[pathVertices[index + 1]].LeftY);
                    right = new Point(g.VList[pathVertices[index + 1]].RightX, g.VList[pathVertices[index + 1]].RightY);
                }


                var tuple = new SymmetricSegment(a, b);
                Rail rail;
                if (!level._railDictionary.TryGetValue(tuple, out rail))
                {
                    var ls = new LineSegment(a, b);
                    //CubicBezierSegment cb = new CubicBezierSegment(a, a, a, a);
                    rail = new Rail(ls, _lgData.GeometryEdgesToLgEdgeInfos[edge],
                        (int)_lgData.GeometryEdgesToLgEdgeInfos[edge].ZoomLevel);

                    if (!RailToEdges.ContainsKey(rail)) RailToEdges[rail] = new List<Edge>();
                    if (!RailToEdges[rail].Contains(edge)) RailToEdges[rail].Add(edge);

                    rail.A = a;
                    rail.B = b;
                    rail.initialA = initial_a;
                    rail.initialB = initial_b;
                    rail.targetA = target_a;
                    rail.targetB = target_b;

                    if (left.X != 0 && left.Y != 0)
                    { rail.Left = left; rail.Right = right; }
                    else { rail.Left = a; rail.Right = b; }

                    level._railDictionary[tuple] = rail;
                    level._railTree.Add(ls.BoundingBox, rail);
                }
                else
                {
                    rail.ZoomLevel = Math.Max(rail.ZoomLevel, level.ZoomLevel);
                    if (!RailToEdges[rail].Contains(edge)) RailToEdges[rail].Add(edge);
                }
                railsOfEdge.Insert(rail);
            }

            return railsOfEdge;
        }

        private void FillGeometryNodesToLgNodeInfos()
        {
            int rankIndex = int.MaxValue;
            _lgData.SortedLgNodeInfos = new List<LgNodeInfo>();
            foreach (Node node in _mainGeometryGraph.Nodes)
            {
                var nodeInfo = new LgNodeInfo(node)
                {
                    Rank = rankIndex--,
                    ZoomLevel = 1000,
                    LabelVisibleFromScale = 1, //GetDoubleAttributeOrDefault(GeometryToken.LabelVisibleFromScale, 1.0),
                    LabelWidthToHeightRatio = 1, // = GetDoubleAttributeOrDefault(GeometryToken.LabelWidthToHeightRatio, 1.0),
                    LabelOffset = new Point(1, 1) // = TryGetPointAttribute(GeometryToken.LabelOffset)
                };
                _lgData.SortedLgNodeInfos.Add(nodeInfo);
                _lgData.GeometryNodesToLgNodeInfos[node] = nodeInfo;
                Console.WriteLine("" + node);
            }
        }

        /// <summary>
        ///     does the initialization
        /// </summary>
        public void Run()
        {
            Dictionary<Node, int> nodeToIndex;


            Console.WriteLine("dot graph");
            RunForMsaglFiles();
            //RunForDotFiles();



            //ProcessEdges(points, nodeToIndex, level, _grid);

            _lgData.CreateLevelNodeTrees(NodeDotWidth(1));
            _railGraph = new RailGraph();
            return;


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

        private bool NodeSuccessfullyPlotted(PointSet points, Dictionary<Node, int> nodeToIndex, LgLevel level, Tiling grid, int nodeToBePlotted, IEnumerable<Node> nodes)
        {
            DijkstraAlgo dijkstra = new DijkstraAlgo();

            foreach (Edge edge in _mainGeometryGraph.Edges)
            {
                var railsOfEdge = AddRailsOfEdge(points, nodeToIndex, level, grid, edge, dijkstra);
                level._railsOfEdges[edge] = railsOfEdge;

            }

            return level.QuotaSatisfied(nodes, _lgLayoutSettings.MaxNumberOfNodesPerTile, _lgLayoutSettings.MaxNumberOfRailsPerTile);
        }

        private Set<Rail> AddRailsOfEdge(PointSet points, Dictionary<Node, int> nodeToIndex, LgLevel level, Tiling grid, Edge edge,
            DijkstraAlgo dijkstra)
        {
            if (_lgData.GeometryNodesToLgNodeInfos[edge.Source].ZoomLevel > level.ZoomLevel ||
                _lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel > level.ZoomLevel)
                return new Set<Rail>();



            _lgData.GeometryEdgesToLgEdgeInfos[edge] = new LgEdgeInfo(edge)
            {
                Rank = Math.Min(_lgData.GeometryNodesToLgNodeInfos[edge.Source].ZoomLevel,
                        _lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel),
                ZoomLevel =
                    Math.Max(_lgData.GeometryNodesToLgNodeInfos[edge.Source].ZoomLevel,
                        _lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel)
            };

            dijkstra.SelectShortestPath(grid.VList, grid.EList, grid.DegList, points.Pt[nodeToIndex[edge.Source]].GridPoint,
                points.Pt[nodeToIndex[edge.Target]].GridPoint, grid.NumOfnodes);

            var railsOfEdge = new Set<Rail>();
            foreach (VertexNeighbor e in dijkstra.Edgelist)
            {
                var a = new Point(grid.VList[e.A].XLoc, grid.VList[e.A].YLoc);
                var b = new Point(grid.VList[grid.EList[e.A, e.Neighbor].NodeId].XLoc,
                    grid.VList[grid.EList[e.A, e.Neighbor].NodeId].YLoc);
                //_grid.EList[e.A, e.Neighbor].Used++;
                var tuple = new SymmetricSegment(a, b);
                Rail rail;
                if (!level._railDictionary.TryGetValue(tuple, out rail))
                {
                    var ls = new LineSegment(a, b);
                    rail = new Rail(ls, _lgData.GeometryEdgesToLgEdgeInfos[edge],
                        (int)_lgData.GeometryEdgesToLgEdgeInfos[edge].ZoomLevel);
                    level._railDictionary[tuple] = rail;
                    level._railTree.Add(ls.BoundingBox, rail);
                }
                else rail.ZoomLevel = Math.Max(rail.ZoomLevel, level.ZoomLevel);
                railsOfEdge.Insert(rail);
            }
            return railsOfEdge;
        }

        private LgLevel CreateLevel(int layer)
        {
            //FillGeometryNodeToLgInfosTables();
            LgLevel level = new LgLevel(layer, _mainGeometryGraph);

            int levelNodeCount = _mainGeometryGraph.Nodes.Count;
            if (_lgData.LevelNodeCounts == null)
            {
                _lgData.LevelNodeCounts = new List<int>();
            }
            _lgData.LevelNodeCounts.Add(levelNodeCount);
            _lgData.Levels.Add(level);
            return level;
        }


        bool ClusterRanksAreNotLessThanChildrens()
        {
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


        void FillGeometryNodeToLgInfosTables()
        {
            foreach (
                Node node in
                    _mainGeometryGraph.Nodes.Concat(_mainGeometryGraph.RootCluster.AllClustersWideFirstExcludingSelf()))
            {

                _lgData.GeometryNodesToLgNodeInfos[node] = new LgNodeInfo(node);
            }
            foreach (Edge edge in _mainGeometryGraph.Edges)
            {
                _lgData.GeometryEdgesToLgEdgeInfos[edge] = new LgEdgeInfo(edge);
            }
        }

        void LayoutTheWholeGraph()
        {
            if (_lgLayoutSettings.NeedToLayout)
            {
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

        void LayoutAndPadOneComponent(GeometryGraph connectedGraph)
        {
            LayoutOneComponent(connectedGraph);
            connectedGraph.BoundingBox.Pad(_lgLayoutSettings.NodeSeparation / 2);
        }

        void CreateConnectedGraphs()
        {
            Dictionary<Node, int> nodeToIndex;
            List<Node> listOfNodes = CreateNodeListForBasicGraph(out nodeToIndex);
            var basicGraph = new BasicGraph<SimpleIntEdge>(GetSimpleIntEdges(nodeToIndex), listOfNodes.Count);
            IEnumerable<IEnumerable<int>> comps = ConnectedComponentCalculator<SimpleIntEdge>.GetComponents(basicGraph);
            foreach (var comp in comps)
                _lgData.AddConnectedGeomGraph(GetConnectedSubgraph(comp, listOfNodes));
        }

        GeometryGraph GetConnectedSubgraph(IEnumerable<int> comp, List<Node> nodeList)
        {
            var edges = new List<Edge>();
            var nodes = new List<Node>();
            var geomGraph = new GeometryGraph();
            foreach (int i in comp)
            {
                Node node = nodeList[i];
                var cluster = node as Cluster;
                if (cluster != null)
                {
                    if (cluster.ClusterParents.First() == _mainGeometryGraph.RootCluster)
                    {
                        //MainGeometryGraph.RootCluster.RemoveCluster(cluster);
                        geomGraph.RootCluster.AddCluster(cluster);
                    }
                }
                else
                {
                    nodes.Add(node);
                }

                foreach (Edge edge in node.OutEdges.Concat(node.SelfEdges))
                {
                    Debug.Assert(!edges.Contains(edge));
                    edges.Add(edge);
                }
            }
            geomGraph.Edges = new SimpleEdgeCollection(edges);
            geomGraph.Nodes = new SimpleNodeCollection(nodes);
            return geomGraph;
        }

        List<Node> CreateNodeListForBasicGraph(out Dictionary<Node, int> nodeToIndex)
        {
            var list = new List<Node>();
            nodeToIndex = new Dictionary<Node, int>();

            foreach (Node node in _mainGeometryGraph.Nodes)
            {
                nodeToIndex[node] = list.Count;
                list.Add(node);
            }
            foreach (Cluster node in _mainGeometryGraph.RootCluster.AllClustersDepthFirstExcludingSelf())
            {
                nodeToIndex[node] = list.Count;
                list.Add(node);
            }
            return list;
        }

        List<SimpleIntEdge> GetSimpleIntEdges(Dictionary<Node, int> nodeToIndex)
        {
            var list =
                _mainGeometryGraph.Edges.Select(
                    edge => new SimpleIntEdge { Source = nodeToIndex[edge.Source], Target = nodeToIndex[edge.Target] })
                    .ToList();

            foreach (Cluster cluster in _mainGeometryGraph.RootCluster.AllClustersDepthFirstExcludingSelf())
            {
                list.AddRange(
                    cluster.Clusters.Select(
                        child => new SimpleIntEdge { Source = nodeToIndex[cluster], Target = nodeToIndex[child] }));
                list.AddRange(
                    cluster.Nodes.Select(
                        child => new SimpleIntEdge { Source = nodeToIndex[cluster], Target = nodeToIndex[child] }));
            }
            return list;
        }

        void LayoutOneComponent(GeometryGraph component)
        {
            PrepareGraphForLayout(component);
            if (component.RootCluster.Clusters.Any())
            {
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
            else
            {
                // todo: currently implemented as if there is only one component
                LayoutHelpers.CalculateLayout(component, GetMdsLayoutSettings(), _cancelToken);
                RemoveOverlapsForLgLayout(component);

            }
            Rectangle box = component.BoundingBox;
            box.Pad(_lgLayoutSettings.NodeSeparation / 2);
            component.BoundingBox = box;
        }

        void RemoveOverlapsForLgLayout(GeometryGraph component)
        {
            Node[] componentNodes = component.Nodes.ToArray();
            Debug.Assert(RankingIsDefined(componentNodes));
            SortComponentNodesByRanking(componentNodes);
            List<int> approxLayerCounts = GetApproximateLayerCounts(componentNodes);
            Size[] sizes = GetApproxSizesForOverlapRemoval(approxLayerCounts, _lgLayoutSettings.NodeSeparation * 3,
                componentNodes);
            OverlapRemoval.RemoveOverlapsForLayers(componentNodes, sizes);
        }

        Size[] GetApproxSizesForOverlapRemoval(List<int> approxLayerCounts, double w, Node[] componentNodes)
        {
            var ret = new Size[componentNodes.Length];
            int layer = 0;

            for (int i = 0; i < componentNodes.Length; i++)
            {
                ret[i] = new Size(w, w);
                if (i == approxLayerCounts[layer])
                {
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
        List<int> GetApproximateLayerCounts(Node[] componentNodes)
        {
            int total = componentNodes.Length;
            int nodesPerTile = _lgLayoutSettings.MaxNumberOfNodesPerTile;
            int numberOfTiles = 1;
            var ret = new List<int>();
            while (true)
            {
                int nodesOnLayer = Math.Min(nodesPerTile * numberOfTiles, total);
                ret.Add(nodesOnLayer);
                if (nodesOnLayer == total)
                    break;
                numberOfTiles *= 3; // thinking about those empty tiles!
            }
            return ret;
        }

        void SortComponentNodesByRanking(Node[] componentNodes)
        {
            var comparer = new RankComparer(_lgData.GeometryNodesToLgNodeInfos);
            Array.Sort(componentNodes, comparer);
        }

        bool RankingIsDefined(Node[] componentNodes)
        {
            if (componentNodes.Length == 0) return true;
            return _lgData.GeometryNodesToLgNodeInfos[componentNodes[0]].Rank > 0;
        }

        static void PrepareGraphForLayout(GeometryGraph connectedGraph)
        {
            foreach (Cluster cluster in connectedGraph.RootCluster.AllClustersDepthFirst())
            {
                if (cluster.RectangularBoundary == null)
                    cluster.RectangularBoundary = new RectangularClusterBoundary();
            }

            foreach (Edge edge in connectedGraph.Edges)
            {
                if (edge.SourcePort == null)
                {
                    Edge e = edge;
                    edge.SourcePort = new RelativeFloatingPort(() => e.Source.BoundaryCurve,
                        () => e.Source.Center);
                }
                if (edge.TargetPort == null)
                {
                    Edge e = edge;
                    edge.TargetPort = new RelativeFloatingPort(() => e.Target.BoundaryCurve,
                        () => e.Target.Center);
                }
            }
        }


        MdsLayoutSettings GetMdsLayoutSettings()
        {
            var settings = new MdsLayoutSettings
            {
                EdgeRoutingSettings = { KeepOriginalSpline = true, EdgeRoutingMode = EdgeRoutingMode.None },
                RemoveOverlaps = false
            };
            return settings;
        }

        bool IsEdgeVisibleAtCurrentLevel(Edge e)
        {
            LgEdgeInfo ei;
            if (!GeometryEdgesToLgEdgeInfos.TryGetValue(e, out ei)) return false;

            double curLevel = _lgData.Levels[_lgData.GetLevelIndexByScale(CurrentZoomLevel)].ZoomLevel;
            return ei.ZoomLevel <= curLevel;
        }

        void AddHighlightedEdgesAndNodesToRailGraph()
        {
            _railGraph.Edges.InsertRange(_lgData.SelectedEdges);
            _railGraph.Nodes.InsertRange(_lgData.SelectedNodeInfos.Select(n => n.GeometryNode));
        }

        public void AddLabelsOfHighlightedNodes(double scale)
        {
            SelectedNodeLabels.Clear();

            var selNodesSet = new Set<LgNodeInfo>();
            foreach (var edge in _lgData.SelectedEdges)
            {
                var ni = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                if (_visibleRectangle.Intersects(GetNodeDotRect(ni, scale)))
                {
                    selNodesSet.Insert(ni);
                }
                ni = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                if (_visibleRectangle.Intersects(GetNodeDotRect(ni, scale)))
                {
                    selNodesSet.Insert(ni);
                }
            }
            selNodesSet.InsertRange(_lgData.SelectedNodeInfos.Where(ni => _visibleRectangle.Intersects(GetNodeDotRect(ni, scale))));

            if (!selNodesSet.Any())
            {
                return;
            }

            var selNodes = selNodesSet.ToList();
            selNodes = selNodes.OrderByDescending(ni => ni.Rank).ToList();

            InsertCandidateLabelsGreedily(selNodes, scale);
        }

        void AddVisibleRailsAndNodes()
        {
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
            GeometryGraph mainGeometryGraph)
        {
            if (mainGeometryGraph.RootCluster.Clusters.Any() == false) return subgraph;
            var ret = new GeometryGraph();
            Dictionary<Node, Node> originalNodesToNewNodes = MapSubgraphNodesToNewNodesForRouting(subgraph);
            ReplicateClusterStructure(subgraph, originalNodesToNewNodes);
            AddNewNodeAndClustersToTheNewGraph(originalNodesToNewNodes, ret);


            foreach (Edge edge in subgraph.Edges)
            {
                Node ns = originalNodesToNewNodes[edge.Source];
                Node nt = originalNodesToNewNodes[edge.Target];
                ret.Edges.Add(new Edge(ns, nt)
                {
                    EdgeGeometry = edge.EdgeGeometry,
                    SourcePort = null,
                    TargetPort = null
                });
            }

            foreach (var kv in originalNodesToNewNodes)
            {
                Node newNode = kv.Value;
                var cluster = newNode as Cluster;
                if (cluster != null)
                {
                    Node oldNode = kv.Key;
                    if (oldNode.BoundaryCurve != newNode.BoundaryCurve)
                    {
                        oldNode.BoundaryCurve = newNode.BoundaryCurve;
                        oldNode.RaiseLayoutChangeEvent(null);
                    }
                }
            }
            return ret;
            //LayoutAlgorithmSettings.ShowGraph(ret);
        }


        static void AddNewNodeAndClustersToTheNewGraph(Dictionary<Node, Node> onodesToNewNodes, GeometryGraph ret)
        {
            foreach (Node newNode in onodesToNewNodes.Values)
            {
                var cl = newNode as Cluster;
                if (cl == null)
                    ret.Nodes.Add(newNode);
                else
                {
                    if (!cl.ClusterParents.Any())
                        ret.RootCluster.AddCluster(cl);
                }
            }
        }

        static void ReplicateClusterStructure(GeometryGraph geometryGraph, Dictionary<Node, Node> onodesToNewNodes)
        {
            foreach (Node onode in geometryGraph.Nodes)
                foreach (Cluster oclparent in onode.ClusterParents)
                {
                    Node newParent;
                    if (onodesToNewNodes.TryGetValue(oclparent, out newParent))
                        ((Cluster)newParent).AddNode(onodesToNewNodes[onode]);
                }
        }

        /*
                bool IsRootCluster(Cluster oclparent) {
                    return !oclparent.ClusterParents.Any();
                }
        */

        static Dictionary<Node, Node> MapSubgraphNodesToNewNodesForRouting(GeometryGraph geometryGraph)
        {
            var onodesToNewNodes = new Dictionary<Node, Node>();
            foreach (Node oNode in geometryGraph.Nodes)
            {
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
        public void RunOnViewChange()
        {
            _visibleRectangle = _lgLayoutSettings.ClientViewportMappedToGraph();
            //Rectangle.Intersect(lgLayoutSettings.ClientViewportMappedToGraph, mainGeometryGraph.BoundingBox);

            //            if (MainGeometryGraph.Edges.Count == 33) {
            //                LayoutAlgorithmSettings.ShowDebugCurves(
            //                    new DebugCurve("red", MainGeometryGraph.BoundingBox.Perimeter()),
            //                    new DebugCurve("blue", visibleRectangle.Perimeter()));
            //                LayoutAlgorithmSettings.ShowGraph(clusterOGraph);
            //            }
            if (_visibleRectangle.IsEmpty) return; //probably we should avoid this situation

            CurrentZoomLevel = GetZoomFactorToTheGraph();//jyoti added -1
            //CurrentZoomLevel = Math.Floor(GetZoomFactorToTheGraph());
            FillRailGraph();
            _lgLayoutSettings.OnViewerChangeTransformAndInvalidateGraph();
        }

        public double GetZoomFactorToTheGraph()
        {
            return _lgLayoutSettings.TransformFromGraphToScreen()[0, 0] / FitFactor();
        }

        public double FitFactor()
        {
            Rectangle vp = _lgLayoutSettings.ClientViewportFunc();
            _lgLayoutSettings.mainGeometryGraphWidth = _mainGeometryGraph.Width;
            _lgLayoutSettings.mainGeometryGraphHeight = _mainGeometryGraph.Height;
            return Math.Min(vp.Width / _mainGeometryGraph.Width, vp.Height / _mainGeometryGraph.Height);
        }


        void FillRailGraph()
        {
            _railGraph.Nodes.Clear();
            AddVisibleRailsAndNodes();
            RegisterEdgeSourceTargets();
        }



        void RegisterEdgeSourceTargets()
        {
            foreach (Edge e in _railGraph.Edges)
                AddEdgeSourceAndTarget(e);
        }

        void AddEdgeSourceAndTarget(Edge edge)
        {
            _railGraph.Nodes.Insert(edge.Source); //
            _railGraph.Nodes.Insert(edge.Target);
        }


        /// <summary>
        /// </summary>
        /// <returns></returns>
        public double GetMaximalZoomLevel()
        {
            if (_lgData == null)
                return 1;
            return _lgData.GetMaximalZoomLevel();
        }

        /// <summary>
        ///     gets all edges passing through rail from the rail's level.
        /// </summary>
        /// <param name="rail"></param>
        /// <returns></returns>
        public List<Edge> GetEdgesPassingThroughRail(Rail rail)
        {
            int i = (int)Math.Log(rail.ZoomLevel, 2);
            LgLevel railLevel = _lgData.Levels[i];
            List<Edge> passingEdges = railLevel.GetEdgesPassingThroughRail(rail);
            return passingEdges;
        }

        int GetIndexByZoomLevel(double z)
        {
            return (int)Math.Log(z, 2);
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



        public List<LgNodeInfo> GetAllNodesOnVisibleLayers()
        {
            var nodesToSelect = new List<LgNodeInfo>();
            for (int i = 0; i < _lgData.Levels.Count; i++)
            {
                var nodes = GetNodeInfosOnLevel(i);
                nodesToSelect.AddRange(nodes);
            }
            return nodesToSelect;
        }

        IEnumerable<LgNodeInfo> GetNodeInfosOnLevel(int i)
        {
            int iStart = (i == 0 ? 0 : _lgData.LevelNodeCounts[i - 1]);
            int iEnd = _lgData.LevelNodeCounts[i];
            for (int j = iStart; j < iEnd; j++)
                yield return _lgData.SortedLgNodeInfos[j];
        }

        IEnumerable<LgNodeInfo> GetNodeInfosOnLevelLeq(int i)
        {
            int iEnd = _lgData.LevelNodeCounts[i];
            for (int j = 0; j < iEnd; j++)
                yield return _lgData.SortedLgNodeInfos[j];
        }

        public void InitEdgesOfLevels()
        {
            foreach (Edge edge in GeometryEdgesToLgEdgeInfos.Keys)
            {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                int iLevel = GetIndexByZoomLevel(Math.Max(s.ZoomLevel, t.ZoomLevel));
                for (int j = iLevel; j < _lgData.Levels.Count; j++)
                {
                    _lgData.Levels[j]._railsOfEdges[edge] = new Set<Rail>();
                }
            }
        }

        List<LgNodeInfo> GetNeighborsOnLevel(LgNodeInfo ni, int i)
        {
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



        //adapting lev's shortest path - jyoti
        public void RouteEdgesOnZeroLayer()
        {

            Tiling g1 = new Tiling(g.NumOfnodes, true);
            g1.N = g.N;
            g1.isPlanar = g.isPlanar;
            g1.NumOfnodesBeforeDetour = g.NumOfnodesBeforeDetour;
            g1.nodeTree = g.nodeTree;
            g1.maxTheoreticalZoomLevel = g.maxTheoreticalZoomLevel;
            g1.nodeToLoc = g.nodeToLoc;
            for (int i = 0; i < g.NumOfnodes; i++)
                g1.VList[i] = new Vertex(g.VList[i].XLoc, g.VList[i].YLoc) { Id = g.VList[i].Id, ZoomLevel = g.VList[i].ZoomLevel };







            var skeletonLevel = _lgData.SkeletonLevels[0];
            skeletonLevel.ClearSavedTrajectoriesAndUsedEdges();
            Console.Write("\nRouting edges");
            int numRouted = 0;

            foreach (LgNodeInfo ni in GetNodeInfosOnLevelLeq(0))
            {
                skeletonLevel.PathRouter.SetAllEdgeLengthMultipliersMin(0.8);

                DecreaseWeightsAlongOldTrajectoriesFromSource(0, skeletonLevel, ni);

                var neighb = GetNeighborsOnLevel(ni, 0);
                foreach (LgNodeInfo t in neighb)
                {
                    var path = _lgData.SkeletonLevels[0].HasSavedTrajectory(ni, t)
                        ? skeletonLevel.GetTrajectory(ni, t)
                        : skeletonLevel.PathRouter.GetPath(ni, t, ShrinkEdgeLengths, g);
                    skeletonLevel.SetTrajectoryAndAddEdgesToUsed(ni, t, path);

                    //collect the path

                    int node1Id = PointToId[path.First()];
                    int lastnode = PointToId[path.Last()];
                    var zoomlevel = Math.Max(g.VList[node1Id].ZoomLevel, g.VList[lastnode].ZoomLevel);

                    int node2Id = -1;

                    foreach (Point p in path)
                    {
                        node2Id = PointToId[p];
                        if (node1Id == node2Id) continue;

                        int j = -1;
                        //find the neighbor where node2 is located
                        for (j = 0; j < g.DegList[node1Id]; j++)
                            if (g.EList[node1Id, j].NodeId == node2Id) break;

                        //g1.AddEdge(node1Id, node2Id, g.EList[node1Id, j].Selected, g.EList[node1Id, j].Used);
                        g1.AddEdge(node1Id, node2Id, 1, zoomlevel);
                        node1Id = node2Id;
                    }




                    // decrease weights
                    skeletonLevel.PathRouter.DecreaseWeightOfEdgesAlongPath(path, 0.5);

                    //if (numRouted++%100 == 0)
                    //  Console.Write(".");
                }
            }

            //clear unused junctions
            for (int i = 0; i < g1.NumOfnodes; i++)
                if (g1.DegList[i] == 0) g1.VList[i].Invalid = true;

            g = g1;

        }

        bool EdgeIsNew(LgNodeInfo s, LgNodeInfo t, int i)
        {
            return Math.Max(s.ZoomLevel, t.ZoomLevel) >= _lgData.Levels[i].ZoomLevel;
        }

        public void RouteEdges(int i)
        {
            AddAllPortEdges(i);
            if (i == 0)
                RouteEdgesOnZeroLayer();
            else
                RouteOnHigherLevel(i);
        }

        void RouteOnHigherLevel(int iLevel)
        {
            IEnumerable<LgNodeInfo> nodes;
            var skeletonLevel = DealWithPathsFromPreviousLevels(iLevel, out nodes);
            ComputeNewTrajectories(iLevel, nodes, skeletonLevel);
        }

        LgSkeletonLevel DealWithPathsFromPreviousLevels(int i, out IEnumerable<LgNodeInfo> nodes)
        {
            var skeletonLevel = _lgData.SkeletonLevels[i];
            var prevSkeletonLevel = _lgData.SkeletonLevels[i - 1];
            nodes = GetNodeInfosOnLevelLeq(i);

            skeletonLevel.ClearSavedTrajectoriesAndUsedEdges();
            skeletonLevel.PathRouter.ResetAllEdgeLengthMultipliers();

            Console.Write("\nRouting edges");
            Console.Write("\nUpdating old trajectories");

            // reset multipliers
            skeletonLevel.PathRouter.ResetAllEdgeLengthMultipliers();

            UpdatePrevLayerTrajectories(i, nodes, prevSkeletonLevel, skeletonLevel);
            //DecreaseWeightsAlongOldTrajectories(i, skeletonLevel);

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
            )
        {
            int numRouted = 0;
            foreach (LgNodeInfo s in nodes)
            {
                var neighb = GetNeighborsOnLevel(s, i).OrderBy(n => n.ZoomLevel);
                foreach (LgNodeInfo t in neighb)
                {
                    var oldTrajectory = prevSkeletonLevel.GetTrajectory(s, t);
                    if (oldTrajectory == null) continue;
                    var trajectory = skeletonLevel.PathRouter.GetPathOnSavedTrajectory(s, t,
                        oldTrajectory, ShrinkEdgeLengths);
                    skeletonLevel.SetTrajectoryAndAddEdgesToUsed(s, t, trajectory);

                    skeletonLevel.MarkEdgesAlongPathAsEdgesOnOldTrajectories(trajectory);
                }
                if (++numRouted % 100 == 1) Console.Write(".");
            }
        }

        void DecreaseWeightsAlongOldTrajectories(int i, LgSkeletonLevel skeletonLevel)
        {
            foreach (var tuple in skeletonLevel.EdgeTrajectories)
            {
                // debug
                if (!EdgeIsNew(tuple.Key.A, tuple.Key.B, i))
                {
                    List<Point> oldPath = tuple.Value;
                    double iEdgeLevel = Math.Log(Math.Max(tuple.Key.A.ZoomLevel, tuple.Key.B.ZoomLevel), 2);
                    //double newWeight = //(iEdgeLevel < 1 ? 0.3 : (iEdgeLevel < 2 ? 0.6 : 1));                    
                    //    0.4 + iEdgeLevel/(Math.Max(i - 1.0, 1.0))*0.2;
                    //newWeight = Math.Max(0.4, newWeight);
                    //newWeight = Math.Min(0.6, newWeight);

                    double newWeight = (iEdgeLevel < 1 ? 0.1 : (iEdgeLevel < 2 ? 0.2 : 0.6));
                    newWeight = Math.Max(0.1, newWeight);
                    newWeight = Math.Min(0.6, newWeight);

                    skeletonLevel.PathRouter.DecreaseWeightOfEdgesAlongPath(oldPath, newWeight);
                }
            }
        }

        void DecreaseWeightsAlongOldTrajectoriesFromSource(int i, LgSkeletonLevel skeletonLevel, LgNodeInfo s)
        {

            IOrderedEnumerable<LgNodeInfo> neighb = GetNeighborsOnLevel(s, i).OrderBy(n => n.ZoomLevel);

            foreach (var t in neighb)
            {
                var tuple = new SymmetricTuple<LgNodeInfo>(s, t);
                List<Point> path;
                skeletonLevel.EdgeTrajectories.TryGetValue(tuple, out path);
                if (path != null)
                {
                    double iEdgeLevel = Math.Log(Math.Max(s.ZoomLevel, t.ZoomLevel), 2);

                    double newWeight = (iEdgeLevel < 1 ? 0.4 : (iEdgeLevel < 2 ? 0.5 : 0.6));
                    newWeight = Math.Max(0.4, newWeight);
                    newWeight = Math.Min(0.6, newWeight);

                    skeletonLevel.PathRouter.DecreaseWeightOfEdgesAlongPath(path, newWeight);
                    //#if DEBUG
                    //                    skeletonLevel.PathRouter.AssertEdgesPresentAndPassable(path);
                    //#endif

                }

            }
        }

        List<LineSegment> GetSegmentsOnOldTrajectoriesFromSource(int i, LgSkeletonLevel skeletonLevel, LgNodeInfo s)
        {
            var pc = new List<LineSegment>();

            IOrderedEnumerable<LgNodeInfo> neighb = GetNeighborsOnLevel(s, i).OrderBy(n => n.ZoomLevel);

            foreach (var t in neighb)
            {
                var tuple = new SymmetricTuple<LgNodeInfo>(s, t);
                List<Point> path;
                skeletonLevel.EdgeTrajectories.TryGetValue(tuple, out path);
                if (path != null)
                {
                    for (int j = 0; j < path.Count - 1; j++)
                    {
                        pc.Add(new LineSegment(path[j], path[j + 1]));
                    }
                }
            }
            return pc;
        }

        void SetWeightsAlongOldTrajectoriesFromSourceToMin(int i, LgSkeletonLevel skeletonLevel, LgNodeInfo s, double wmin)
        {

            IOrderedEnumerable<LgNodeInfo> neighb = GetNeighborsOnLevel(s, i).OrderBy(n => n.ZoomLevel);

            foreach (var t in neighb)
            {
                var tuple = new SymmetricTuple<LgNodeInfo>(s, t);
                List<Point> path;
                skeletonLevel.EdgeTrajectories.TryGetValue(tuple, out path);
                if (path != null)
                {
                    skeletonLevel.PathRouter.SetWeightOfEdgesAlongPathToMin(path, wmin);
                }

            }
        }

        void ComputeNewTrajectories(int i, IEnumerable<LgNodeInfo> nodes, LgSkeletonLevel skeletonLevel)
        {
            int numRouted = 0;
            Console.Write("\nComputing new trajectories");

            // reset multipliers
            skeletonLevel.PathRouter.SetAllEdgeLengthMultipliersMin(0.8);

            foreach (LgNodeInfo s in nodes)
            {

                // decrease weights along old trajectories
                DecreaseWeightsAlongOldTrajectoriesFromSource(i, skeletonLevel, s);

                IOrderedEnumerable<LgNodeInfo> neighb = GetNeighborsOnLevel(s, i).OrderBy(n => n.ZoomLevel);

                foreach (LgNodeInfo t in neighb)
                {

                    List<Point> path = skeletonLevel.GetTrajectory(s, t);
                    if (path == null)
                    {
                        path = skeletonLevel.PathRouter.GetPath(s, t, ShrinkEdgeLengths);
                        skeletonLevel.SetTrajectoryAndAddEdgesToUsed(s, t, path);
                    }
                    numRouted++;
                    if (numRouted % 100 == 1)
                        Console.Write(".");

                    //#if DEBUG
                    //                    List<LineSegment> pc = new List<LineSegment>();
                    //                    List<LineSegment> pcold = GetSegmentsOnOldTrajectoriesFromSource(i, skeletonLevel, s);

                    //                    for (int j = 0; j < path.Count - 1; j++)
                    //                    {
                    //                        pc.Add(new LineSegment(path[j],path[j+1]));
                    //                    }
                    //                    if ((int)s.GeometryNode.DebugId == 301 && (int)t.GeometryNode.DebugId == 81) {
                    //                        SplineRouter.ShowVisGraph(skeletonLevel.PathRouter.VisGraph,
                    //                            nodes.Select(n => n.BoundaryOnLayer), pcold, pc);
                    //                    }
                    //#endif
                }

                // reset multipliers
                SetWeightsAlongOldTrajectoriesFromSourceToMin(i, skeletonLevel, s, 0.8);
            }
        }

        public bool CheckSanityAllRoutes(int i)
        {
            var nodes = GetNodeInfosOnLevelLeq(i);
            bool sanity = true;
            foreach (LgNodeInfo s in nodes)
            {
                sanity &= CheckSanityRoutes(s, i);
                if (!sanity) break;
            }
            return sanity;
        }

        public bool CheckSanityRoutes(LgNodeInfo s, int i)
        {
            List<LgNodeInfo> neighb = GetNeighborsOnLevel(s, i);
            Point rootPoint = _lgData.SkeletonLevels[i].PathRouter.AddVisGraphVertex(s.Center);

            var graph = new LgPathRouter();
            foreach (LgNodeInfo t in neighb)
            {
                List<Point> path = _lgData.SkeletonLevels[i].GetTrajectory(s, t);
                for (int j = 0; j < path.Count - 1; j++)
                {
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

        void CopyGraphToNextLevel(int i)
        {
            foreach (var st in _lgData.SkeletonLevels[i].PathRouter.GetAllEdgesVisibilityEdges())
            {
                _lgData.SkeletonLevels[i + 1].PathRouter.AddVisGraphEdge(st.SourcePoint, st.TargetPoint);
            }
        }


        public void ClearSkeleton(int iLevel)
        {
            _lgData.SkeletonLevels[iLevel].Clear();
        }

        public void RunOverlapRemovalBitmap(int iLevel)
        {
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
            while (true)
            {
                numPlaced = orb.PositionAllMoveableRectsSameSize(numPlaced, fixedRectTree, fixedSegTree);
                if (numPlaced >= moveableBoxes.Count()) break;
                orb.ScaleBbox(1.25);
            }

            Point[] translations = orb.GetTranslations();
            int i = 0;
            foreach (var mv in moveableNodes)
            {
                mv.Translate(translations[i++]);
            }

#if DEBUG
            // ShowNodesAndSegmentsForOverlapRemoval(fixedNodes, moveableNodes, fixedSegments);
#endif
        }

        static void ShowNodesAndSegmentsForOverlapRemoval(IEnumerable<LgNodeInfo> fixedNodes,
            IEnumerable<LgNodeInfo> moveableNodes, SymmetricSegment[] fixedSegments)
        {
#if DEBUG && !SILVERLIGHT && !SHARPKIT
            var l = new List<DebugCurve>();
            if (fixedNodes != null && fixedNodes.Any())
            {
                foreach (var node in fixedNodes)
                {
                    l.Add(new DebugCurve(200, 0.1, "red", node.BoundaryCurve));
                }
            }
            if (fixedSegments != null && fixedSegments.Any())
            {
                foreach (var seg in fixedSegments)
                {
                    var ls = new LineSegment(seg.A, seg.B);
                    l.Add(new DebugCurve(100, 0.1, "magenta", ls));
                }
            }
            foreach (var node in moveableNodes)
            {
                l.Add(new DebugCurve(200, 0.1, "green", node.BoundaryCurve));
            }
            LayoutAlgorithmSettings.ShowDebugCurves(l.ToArray());
#endif
        }

        void ClearLevels(int num)
        {
            _lgData.Levels.Clear();
            _lgData.SkeletonLevels.Clear();
            int zoomLevel = 1;
            for (int i = 0; i < num; i++)
            {
                var level = new LgLevel(zoomLevel, _mainGeometryGraph);
                level.ClearRailTree();

                _lgData.Levels.Add(level);
                _lgData.SkeletonLevels.Add(new LgSkeletonLevel { ZoomLevel = zoomLevel });

                zoomLevel *= 2;
            }
        }

        void UpdateNodeInfoZoomLayers()
        {
            int level = 1;
            int j = 0;
            for (int i = 0; i < _lgData.SortedLgNodeInfos.Count; i++)
            {
                while (j < _lgData.LevelNodeCounts.Count && i == _lgData.LevelNodeCounts[j])
                {
                    j++;
                    level *= 2;
                }
                _lgData.SortedLgNodeInfos[i].ZoomLevel = level;
            }
        }

        void InitRailsOfEdgesEmpty()
        {
            foreach (Edge edge in _mainGeometryGraph.Edges)
            {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                double zoomLevel = Math.Max(s.ZoomLevel, t.ZoomLevel);

                GeometryEdgesToLgEdgeInfos[edge].ZoomLevel = zoomLevel;
                int iLevel = GetIndexByZoomLevel(zoomLevel); //(int) Math.Log(zoomLevel, 2);
                for (int j = iLevel; j < _lgData.Levels.Count; j++)
                    _lgData.Levels[j]._railsOfEdges[edge] = new Set<Rail>();
            }
        }

        void InitRailsOfEdgesEmpty(int iMinLevel)
        {
            foreach (Edge edge in _mainGeometryGraph.Edges)
            {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                double zoomLevel = Math.Max(s.ZoomLevel, t.ZoomLevel);
                GeometryEdgesToLgEdgeInfos[edge].ZoomLevel = zoomLevel;

                int iLevel = Math.Max(GetIndexByZoomLevel(zoomLevel), iMinLevel);
                for (int j = iLevel; j < _lgData.Levels.Count; j++)
                    _lgData.Levels[j]._railsOfEdges[edge] = new Set<Rail>();
            }
        }

        public void AddAllPortEdges(int iLevel)
        {
            var nodes = GetNodeInfosOnLevelLeq(iLevel);
            var skeletonLevel = _lgData.SkeletonLevels[iLevel];
            skeletonLevel.AddGraphEdgesFromCentersToPointsOnBorders(nodes);
            //SplineRouter.ShowVisGraph(skeletonLevel.PathRouter.VisGraph, null,null,null);
        }

        void CreateGraphFromSteinerCdt(int iLevel)
        {
            var nodes = GetNodeInfosOnLevelLeq(iLevel);
            var cdt = new SteinerCdt(_lgData.SkeletonLevels[iLevel].PathRouter.VisGraph, nodes);
            //cdt.ReadTriangleOutputAndPopulateTheLevelVisibilityGraphFromTriangulation();
            for (int index = 0; index < g.NumOfnodes; index++)
            {
                var p = new Point(g.VList[index].XLoc, g.VList[index].YLoc);
                var w = cdt._visGraph.AddVertex(p);
                if (index < g.NumOfnodesBeforeDetour) w.isReal = true;
                else w.isReal = false;
                cdt._outPoints[index] = w;
                cdt._visGraph.PointToVertexMap[p] = w;
                cdt._visGraph.visVertexToId[w] = index;
                PointToId[p] = index;
            }

            for (int index = 0; index < g.NumOfnodes; index++)
            {
                for (int neighbor = 0; neighbor < g.DegList[index]; neighbor++)
                {
                    var a = cdt._outPoints[index];
                    var b = cdt._outPoints[g.EList[index, neighbor].NodeId];
                    cdt.AddVisEdge(a, b);
                }
            }
        }
        /*
                void CreateGraphFromSteinerCdt(int iLevel)
                {
                    var nodes = GetNodeInfosOnLevelLeq(iLevel);
                    var cdt = new SteinerCdt(_lgData.SkeletonLevels[iLevel].PathRouter.VisGraph, nodes);
                    cdt.ReadTriangleOutputAndPopulateTheLevelVisibilityGraphFromTriangulation();
                }
                */
        public bool SimplifyRoutes(int iLevel)
        {
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
            Point b, LgSkeletonLevel skeletonLevel)
        {
            var ll = new List<DebugCurve>();
            ll.Add(new DebugCurve(new LineSegment(a, shortcutted)));
            ll.Add(new DebugCurve(new LineSegment(shortcutted, b)));
            ll.AddRange(oldIntersected.Select(r => new DebugCurve(100, 0.1, "blue", LineFromRail(r))));
            ll.AddRange(newIntersected.Select(r => new DebugCurve(100, 0.1, "red", LineFromRail(r))));
            ll.Add(new DebugCurve(CurveFactory.CreateCircle(3, shortcutted)));
            ll.Add(new DebugCurve("red", CurveFactory.CreateCircle(3, a)));
            ll.Add(new DebugCurve("green", CurveFactory.CreateCircle(3, b)));

            foreach (var rail in newIntersected)
            {
                var e = FindRailInVisGraph(rail, skeletonLevel);
                if (e == null)
                {
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

        static VisibilityEdge FindRailInVisGraph(Rail rail, LgSkeletonLevel skeletonLevel)
        {
            Point s, t;
            rail.GetStartEnd(out s, out t);
            return skeletonLevel.PathRouter.FindEdge(s, t);
        }

        static ICurve LineFromRail(Rail rail)
        {
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
            var calc = new GreedyNodeRailLevelCalculator(_lgData.SortedLgNodeInfos)
            {
                GeometryNodesToLgNodeInfos = _lgData.GeometryNodesToLgNodeInfos
            };
            calc.MaxAmountNodesPerTile = maxNodesPerTile;
            calc.IncreaseNodeQuota = _lgLayoutSettings.IncreaseNodeQuota;

            var bbox = GetLargestTile();

            calc.PlaceNodesOnly(bbox);
            _lgData.LevelNodeCounts = calc.GetLevelNodeCounts();

            ClearLevels(_lgData.LevelNodeCounts.Count);
        }

        public void FillLevelWithNodesRoutesTryRerouting(int iLevel, int maxNodesPerTile, int maxSegmentsPerTile,
            double increaseNodeQuota)
        {
            var calc = new GreedyNodeRailLevelCalculator(_lgData.SortedLgNodeInfos)
            {
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

        void AssignRailsByTrajectories(int iLevel)
        {
            var edges = new List<Edge>(_lgData.Levels[iLevel]._railsOfEdges.Keys);
            _lgData.Levels[iLevel]._railTree = new RTree<Rail>();
            foreach (Edge edge in edges)
            {
                LgEdgeInfo ei = _lgData.GeometryEdgesToLgEdgeInfos[edge];
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                if (_lgData.SkeletonLevels[iLevel].HasSavedTrajectory(s, t))
                {
                    List<Point> trajectory = _lgData.SkeletonLevels[iLevel].GetTrajectory(s, t);
                    List<Rail> rails = _lgData.Levels[iLevel].FetchOrCreateRailSequence(trajectory);
                    _lgData.Levels[iLevel]._orderedRailsOfEdges[edge] = rails;

                    _lgData.AssembleEdgeAtLevel(ei, iLevel, new Set<Rail>(rails));
                }
            }
        }

        void PushNodesToNextLevel(int iLevel, int numInserted)
        {
            if (iLevel == _lgData.Levels.Count - 1)
            {
                var level = new LgLevel(_lgData.Levels[iLevel].ZoomLevel * 2, _mainGeometryGraph);
                level.CreateEmptyRailTree();
                _lgData.Levels.Add(level);
                _lgData.SkeletonLevels.Add(new LgSkeletonLevel { ZoomLevel = level.ZoomLevel });
                _lgData.LevelNodeCounts.Add(_lgData.LevelNodeCounts.Last());
            }

            int numToInsert = _lgData.LevelNodeCounts[iLevel];
            if (numToInsert <= numInserted) return;

            int newZoomLevel = _lgData.Levels[iLevel + 1].ZoomLevel;
            for (int i = numInserted; i < numToInsert; i++)
            {
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
        public void LayoutAndRouteByLayers(int maxNodesPerTile, int maxSegmentsPerTile, double increaseNodeQuota)
        {
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
            var p = NodeDotWidth(1) * 0.5;
            bbox.Pad(p, p, p, p);
            return bbox;
        }

        public void LabelingOfOneRun()
        {
            foreach (LgNodeInfo node in _lgData.SortedLgNodeInfos)
            {
                //if (node.ZoomLevel > 1.0)
                //    node.LabelVisibleFromScale = node.ZoomLevel;
                //else
                node.LabelVisibleFromScale = 0.0;
            }

            double scale = 1.0 / 16.0;

            double delta = Math.Pow(2, 1.0 / 8.0);

            int numberOfLabeledNodes = 0;
            double hugeScale = Math.Pow(2, _lgData.Levels.Last().ZoomLevel + 1);
            while (numberOfLabeledNodes < _lgData.GeometryNodesToLgNodeInfos.Count && scale <= hugeScale)
            {
                numberOfLabeledNodes = InsertLabelsGreedily(scale);
                scale *= delta;
            }
            if (numberOfLabeledNodes < _lgData.SortedLgNodeInfos.Count)
                Console.WriteLine("Failed to label {0} nodes", _lgData.SortedLgNodeInfos.Count - numberOfLabeledNodes);

            CleanUpRails();

            // make labels appear slightly earlier
            foreach (LgNodeInfo node in _lgData.SortedLgNodeInfos)
            {
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
            int iLayer)
        {
            PrepareNodeBoundariesAndSkeletonOnLayer(iLayer);
            //commented by jyoti - takes too long
            //RunOverlapRemovalBitmap(iLayer);

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
            /*
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
             * */
        }

        bool HigherTrajectoriesPreserved(int iLayer)
        {
            for (int j = 0; j <= iLayer; j++)
                if (!TestTrajectoriesPreserved(iLayer)) return false;
            return true;
        }

        void PrepareNodeBoundariesAndSkeletonOnLayer(int iLayer)
        {
            var scale = Math.Pow(2, iLayer);
            var rad = NodeDotWidth(scale); // make the BoundaryOnLayer twice larger than the node
            for (int i = 0; i < _lgData.LevelNodeCounts[iLayer]; i++)
            {
                LgNodeInfo ni = _lgData.SortedLgNodeInfos[i];
                ni.BoundaryOnLayer = CurveFactory.CreateRegularPolygon(_lgLayoutSettings.NumberOfNodeShapeSegs,
                    ni.Center, rad);
            }
            if (iLayer > 0)
            {
                ModifySkeletonWithNewBoundariesOnLayer(iLayer);
            }
        }

        void ModifySkeletonWithNewBoundariesOnLayer(int iLayer)
        {
            for (int i = 0; i < _lgData.LevelNodeCounts[iLayer]; i++)
                _lgData.SkeletonLevels[iLayer].PathRouter.ModifySkeletonWithNewBoundaryOnLayer(
                    _lgData.SortedLgNodeInfos[i]);
        }


        void RemoveUnusedVisibilityGraphEdgesAndNodes(int i)
        {
            _lgData.SkeletonLevels[i].RemoveUnusedGraphEdgesAndNodes();
        }

        void SimplifyRoutesOnLevelUntilDone(int i)
        {
#if DEBUG
            //            ShowOldTrajectories(_lgData.SkeletonLevels[i]);
#endif

            do
            {
                if (!SimplifyRoutes(i))
                    break;
                UpdateRoutesAfterSimplification(i);
            } while (true);

#if DEBUG
            //            ShowOldTrajectories(_lgData.SkeletonLevels[i]);
#endif
        }

        public void InitNodeLabelWidthToHeightRatios(List<double> noldeLabelRatios)
        {
            for (int i = 0; i < _mainGeometryGraph.Nodes.Count; i++)
            {
                var n = _mainGeometryGraph.Nodes[i];
                var ni = _lgData.GeometryNodesToLgNodeInfos[n];
                if (ni != null)
                {
                    ni.LabelWidthToHeightRatio = noldeLabelRatios[i];
                }
            }
        }

        void RemoveTrajectoriesForEdgesWithHighZoom(int iLevel)
        {
            // some edges are no longer used since their adjacent nodes moved to the next level.
            // need to delete them from the level EdgeTrajectories
            var edgeTrajectories = _lgData.SkeletonLevels[iLevel].EdgeTrajectories;
            int zoomLevel = _lgData.Levels[iLevel].ZoomLevel;
            var removeList =
                new List<SymmetricTuple<LgNodeInfo>>(
                    edgeTrajectories.Keys.Where(p => p.A.ZoomLevel > zoomLevel || p.B.ZoomLevel > zoomLevel));
            if (removeList.Count > 0)
            {
                _lgData.SkeletonLevels[iLevel].RemoveSomeEdgeTrajectories(removeList);
                _lgData.Levels[iLevel].RemoveFromRailEdges(removeList);
            }
        }

        void TestRailsForTrajectories(int iLevel)
        {
            foreach (var path in _lgData.SkeletonLevels[iLevel].EdgeTrajectories.Values)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Rail rail = _lgData.Levels[iLevel].FindRail(path[i], path[i + 1]);
                    //lgData.Levels[iLevel].AddRailToDictionary(rail);
                    //lgData.Levels[iLevel].AddRailToRtree(rail);
                    if (rail == null)
                    {
                        Console.WriteLine("Rail not found for trajectory!");
                    }
                }
            }
        }

        void UpdateRoutesAfterSimplification(int i)
        {
            var skeletonLevel = _lgData.SkeletonLevels[i];
            foreach (Edge edge in _lgData.Levels[i]._railsOfEdges.Keys)
            {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                List<Point> path = skeletonLevel.GetTrajectory(s, t);
                if (path == null) continue;

                int startedToSkip = 0;
                for (int j = 1; j < path.Count - 1; j++)
                {
                    if (skeletonLevel.PathRouter.ContainsVertex(path[j]))
                    {
                        if (startedToSkip > 0)
                        {
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

                if (!EdgeIsNew(s, t, i))
                {
                    skeletonLevel.MarkEdgesAlongPathAsEdgesOnOldTrajectories(path);
                }
            }
        }

        bool TestTrajectoriesPreserved(int iLevel)
        {
            if (iLevel == 0) return true;
            foreach (var st in _lgData.SkeletonLevels[iLevel - 1].EdgeTrajectories)
            {
                LgNodeInfo s = st.Key.A;
                LgNodeInfo t = st.Key.B;
                List<Point> oldPath = st.Value;
                Point ps = oldPath.First();
                List<Point> newPath = _lgData.SkeletonLevels[iLevel].GetTrajectory(s, t);
                Debug.Assert(newPath.Count > 1);
                if (!newPath.First().Equals(ps))
                    newPath.Reverse();

                if (!newPath.First().Equals(ps))
                {
                    Console.WriteLine("Endpoint of old path not found on new path!");
                    return false;
                }

                int i;
                int j;
                for (i = 1, j = 1; i < oldPath.Count; i++, j++)
                {
                    var subdivEdge = new List<Point> { oldPath[i - 1] };
                    while (j < newPath.Count && !oldPath[i].Equals(newPath[j]))
                    {
                        subdivEdge.Add(newPath[j]);
                        j++;
                    }
                    if (j == newPath.Count || !oldPath[i].Equals(newPath[j]))
                    {
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


        void CleanUpRails()
        {
            foreach (LgLevel level in _lgData.Levels)
            {
                var usedRails = new Set<Rail>();
                foreach (var rails in level._railsOfEdges.Values)
                {
                    usedRails.InsertRange(rails);
                }

                var unusedRails = new Set<Rail>(level._railDictionary.Values.Where(r => !usedRails.Contains(r)));
                if (!unusedRails.Any()) continue;

                foreach (Rail rail in unusedRails)
                {
                    level.RemoveRailFromDictionary(rail);
                    level.RemoveRailFromRtree(rail);
                }
            }
        }

        void TestAllEdgesConsistency()
        {
            foreach (Edge edge in _mainGeometryGraph.Edges)
            {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[edge.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[edge.Target];
                double zoomLevel = Math.Max(s.ZoomLevel, t.ZoomLevel);
                int iLevel = GetIndexByZoomLevel(zoomLevel);
                for (int i = iLevel; i < _lgData.Levels.Count; i++)
                {
                    Set<Rail> rails = _lgData.Levels[i]._railsOfEdges[edge];
                    foreach (Rail rail in rails)
                    {
                        var rc = rail.Geometry as ICurve;
                        if (rc != null)
                        {
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


        public void SelectAllEdgesIncidentTo(LgNodeInfo nodeInfo)
        {
            List<Edge> edges = nodeInfo.GeometryNode.Edges.ToList();

            //START-jyoti to select only the neighbors within the current zoom level
            List<Edge> filteredEdges = new List<Edge>();
            foreach (Edge edge in edges)
            {
                //if (_lgData.GeometryNodesToLgNodeInfos[edge.Source].ZoomLevel > CurrentZoomLevel ||
                //    _lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel > CurrentZoomLevel) continue;
                filteredEdges.Add(edge);
            }
            edges = filteredEdges;
            //END-jyoti to select only the neighbors within the current zoom level


            if (!nodeInfo.Selected)
                _lgData.SelectEdges(edges);
            else
                _lgData.UnselectEdges(edges);
        }

        public void SelectVisibleEdgesIncidentTo(LgNodeInfo nodeInfo, int currentLayer)
        {

            List<Edge> edges = nodeInfo.GeometryNode.Edges.ToList();
            int currentZoomFromLayer = Math.Max(0, currentLayer - 1);

            //START-jyoti to select only the neighbors within the current zoom level
            List<Edge> filteredEdges = new List<Edge>();
            foreach (Edge edge in edges)
            {
                if (_lgData.GeometryNodesToLgNodeInfos[edge.Source].ZoomLevel <= currentZoomFromLayer &&
                    _lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel <= currentZoomFromLayer)
                    filteredEdges.Add(edge);
            }
            //END-jyoti to select only the neighbors within the current zoom level


            if (!nodeInfo.Selected)
                _lgData.SelectEdges(edges, Math.Min(currentLayer, _lgLayoutSettings.maximumNumOfLayers));
            else
                _lgData.UnselectEdges(edges);
        }

        public void UpdateVisibleEdgesIncidentTo(LgNodeInfo nodeInfo, int currentLayer)
        {
            List<Edge> edges = nodeInfo.GeometryNode.Edges.ToList();
            int currentZoomFromLayer = Math.Max(0, currentLayer - 1);

            //START-jyoti to select only the neighbors within the current zoom level
            List<Edge> filteredEdges = new List<Edge>();

            foreach (Edge edge in edges)
            {
                if (_lgData.GeometryNodesToLgNodeInfos[edge.Source].ZoomLevel <= currentZoomFromLayer &&
                    _lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel <= currentZoomFromLayer)
                    filteredEdges.Add(edge);
            }

            edges = filteredEdges;
            //END-jyoti to select only the neighbors within the current zoom level


            if (nodeInfo.Selected)
                _lgData.SelectEdges(edges, Math.Min(currentLayer, _lgLayoutSettings.maximumNumOfLayers));
        }

        public void SelectEdge(LgEdgeInfo ei)
        {
            var edges = new List<Edge> { ei.Edge };
            _lgData.SelectEdges(edges);
        }

        public void DeselectAllEdges()
        {
            _lgData.PutOffAllEdges();
        }


        public void RunMds()
        {
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

            foreach (Edge e in GeometryEdgesToLgEdgeInfos.Keys)
            {
                LgNodeInfo s = _lgData.GeometryNodesToLgNodeInfos[e.Source];
                LgNodeInfo t = _lgData.GeometryNodesToLgNodeInfos[e.Target];
                double zoomLevel = Math.Max(s.ZoomLevel, t.ZoomLevel);

                if (zoomLevel < 5)
                {
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


            var settings = new MdsLayoutSettings { ScaleX = 600, ScaleY = 600, RunInParallel = false, PivotNumber = 100 };

            //var mds = new MDSGraphLayoutWeighted(settings, mainGeometryGraph, multipliers);
            var mds = new MdsGraphLayout(settings, _mainGeometryGraph);
            mds.Run();
        }

        public void SelectTopEdgePassingThroughRailWithEndpoint(Rail rail, Set<LgNodeInfo> selectedVnodes)
        {
            List<Edge> edges =
                GetEdgesPassingThroughRail(rail)
                    .Where(e => selectedVnodes.Contains(_lgData.GeometryNodesToLgNodeInfos[e.Source])
                                || selectedVnodes.Contains(_lgData.GeometryNodesToLgNodeInfos[e.Target])).ToList();
            if (!edges.Any()) return;
            DeselectAllEdges();
            Edge edge = edges.OrderByDescending(e => _lgData.GeometryEdgesToLgEdgeInfos[e].Rank).First();
            SelectEdge(_lgData.GeometryEdgesToLgEdgeInfos[edge]);
        }

        Rectangle GetLabelRectForScale(LgNodeInfo nodeInfo, double scale)
        {
            double labelHeight = _lgLayoutSettings.NodeLabelHeightInInches * _lgLayoutSettings.DpiX / scale /
                                 FitFactor();
            double labelWidth = labelHeight * nodeInfo.LabelWidthToHeightRatio;

            double nodeDotWidth = GetNodeDotRect(nodeInfo, scale).Width;
            //_lgLayoutSettings.NodeDotWidthInInches * _lgLayoutSettings.DpiX / currentScale;

            Point offset = Point.Scale(labelWidth + nodeDotWidth * 1.01, labelHeight + nodeDotWidth * 1.01,
                nodeInfo.LabelOffset);

            var d = new Point(0.5 * labelWidth, 0.5 * labelHeight);

            return new Rectangle(nodeInfo.Center + offset - d, nodeInfo.Center + offset + d);
        }

        Rectangle GetLabelRectForScale(LgNodeInfo nodeInfo, LgNodeInfo.LabelPlacement placement, double scale)
        {
            double labelHeight = _lgLayoutSettings.NodeLabelHeightInInches * _lgLayoutSettings.DpiX / scale /
                                 FitFactor();
            double labelWidth = labelHeight * nodeInfo.LabelWidthToHeightRatio;

            double nodeDotWidth = GetNodeDotRect(nodeInfo, scale).Width;
            //_lgLayoutSettings.NodeDotWidthInInches * _lgLayoutSettings.DpiX / currentScale;            

            Point offset = Point.Scale(labelWidth + nodeDotWidth * 1.01, labelHeight + nodeDotWidth * 1.01,
                LgNodeInfo.GetLabelOffset(placement));

            var d = new Point(0.5 * labelWidth, 0.5 * labelHeight);

            return new Rectangle(nodeInfo.Center + offset - d, nodeInfo.Center + offset + d);
        }

        Rectangle GetNodeDotRect(LgNodeInfo node, double scale)
        {
            var rect = new Rectangle(node.Center);
            rect.Pad(NodeDotWidth(scale) / 2);
            return rect;
        }

        double NodeDotWidth(double scale)
        {
            return _lgLayoutSettings.NodeDotWidthInInches * _lgLayoutSettings.DpiX / scale / FitFactor();
        }

        private void InsertCandidateLabelsGreedily(List<LgNodeInfo> candidates, double _scale)
        {
            var scale = CurrentZoomLevel;

            var labelRTree = new RTree<LgNodeInfo>();

            // insert all nodes inserted before
            foreach (var node in _railGraph.Nodes)
            {

                var ni = _lgData.GeometryNodesToLgNodeInfos[node];

                // add all node dots
                labelRTree.Add(GetNodeDotRect(ni, scale), ni);

                if (ni.LabelVisibleFromScale <= scale)
                {
                    Rectangle labelRect = GetLabelRectForScale(ni, scale);
                    labelRTree.Add(labelRect, ni);
                }
            }

            foreach (LgNodeInfo node in candidates)
            {
                if (node.LabelVisibleFromScale <= scale)
                {
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

                LgNodeInfo.LabelPlacement pl = LgNodeInfo.LabelPlacement.Bottom;

                foreach (LgNodeInfo.LabelPlacement placement in positions)
                {
                    pl = placement;
                    labelRect = GetLabelRectForScale(node, pl, scale);
                    if (!labelRTree.IsIntersecting(labelRect))
                    {
                        couldPlace = true;
                        break;
                    }
                }


                if (couldPlace)
                {
                    labelRTree.Add(labelRect, node);
                    SelectedNodeLabels[node] = pl;
                }
            }

        }

        int InsertLabelsGreedily(double scale)
        {
            int labeledNodes = 0;

            int iLevel = 0;
            while (iLevel < _lgData.Levels.Count - 1 && scale >= _lgData.Levels[iLevel].ZoomLevel)
            {
                iLevel++;
            }

            var nodes = GetNodeInfosOnLevelLeq(iLevel);

            var labelRTree = new RTree<LgNodeInfo>();


            // insert all nodes inserted before
            foreach (LgNodeInfo node in nodes)
            {
                // add all node dots
                labelRTree.Add(GetNodeDotRect(node, scale), node);

                if (node.LabelVisibleFromScale > 0 && node.LabelVisibleFromScale < scale)
                {
                    Rectangle labelRect = GetLabelRectForScale(node, scale);
                    labelRTree.Add(labelRect, node);
                    labeledNodes++;
                }
            }

            foreach (LgNodeInfo node in nodes)
            {
                if (node.LabelVisibleFromScale > 0 && node.LabelVisibleFromScale < scale)
                {
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
                foreach (LgNodeInfo.LabelPlacement placement in positions)
                {
                    node.LabelPosition = placement;
                    labelRect = GetLabelRectForScale(node, scale);
                    if (!labelRTree.IsIntersecting(labelRect))
                    {
                        couldPlace = true;
                        break;
                    }
                }


                if (!couldPlace)
                {
                    node.LabelVisibleFromScale = 0;
                }
                else
                {
                    if (node.LabelVisibleFromScale <= 0)
                        node.LabelVisibleFromScale = scale;
                    labelRTree.Add(labelRect, node);
                    labeledNodes++;
                }
            }

            return labeledNodes;
        }

        class RankComparer : IComparer<Node>
        {
            readonly Dictionary<Node, LgNodeInfo> _table;

            public RankComparer(Dictionary<Node, LgNodeInfo> geometryNodesToLgNodeInfos)
            {
                _table = geometryNodesToLgNodeInfos;
            }

            public int Compare(Node a, Node b)
            {
                return _table[b].Rank.CompareTo(_table[a].Rank);
            }
        }

        public bool RectIsEmptyStartingFromLevel(Rectangle tileBox, int iLevel)
        {
            for (int i = iLevel; i < _lgData.Levels.Count; i++)
            {
                if (!_lgData.Levels[i].RectIsEmptyOnLevel(tileBox))
                    return false;
            }
            return true;
        }

        public int GetNumberOfLevels()
        {
            return _lgData.Levels.Count;
        }

        LgNodeInfo FindClosestNodeInfoForMouseClickOnLevel(Point mouseDownPositionInGraph, int iLevel)
        {
            if (iLevel >= _lgData.Levels.Count) return null;
            var level = _lgData.Levels[iLevel];
            var hitRectWidth = NodeDotWidth(CurrentZoomLevel) / 2;
            var rect = new Rectangle(new Size(hitRectWidth, hitRectWidth), mouseDownPositionInGraph);
            var intersected = level.NodeInfoTree.GetAllIntersecting(rect);
            if (!intersected.Any()) return null;
            double dist = double.PositiveInfinity;
            LgNodeInfo closest = null;
            foreach (var ni in intersected)
            {
                var t = (ni.Center - mouseDownPositionInGraph).LengthSquared;
                if (t < dist)
                {
                    dist = t;
                    closest = ni;
                }
            }
            return closest;
        }

        LgNodeInfo FindClosestNodeInfoForMouseClickBelowCurrentLevel(Point mouseDownPositionInGraph)
        {
            var iLevel = _lgData.GetLevelIndexByScale(CurrentZoomLevel);
            for (int i = iLevel + 1; i < _lgData.LevelNodeCounts.Count; i++)
            {
                LgNodeInfo closest = FindClosestNodeInfoForMouseClickOnLevel(mouseDownPositionInGraph, i);
                if (closest != null) return closest;
            }
            return null;
        }

        public void AnalyzeClick(Point mouseDownPositionInGraph, int downCount)
        {
            var closest = FindClosestNodeInfoForMouseClickBelowCurrentLevel(mouseDownPositionInGraph);
            if (closest == null) return;
            SelectAllEdgesIncidentTo(closest);
            var edges = closest.GeometryNode.Edges.ToList();
            _railGraph.Edges.InsertRange(edges);
            closest.Selected = true;
            _lgData.SelectedNodeInfos.Insert(closest);
            _railGraph.Nodes.Insert(closest.GeometryNode);
            RunOnViewChange();
        }



        public bool NumberOfNodesOfLastLayerIntersectedRectIsLessThanBound(int iLevel, Rectangle rect, int bound)
        {
            int lastLevel = GetNumberOfLevels() - 1;
            var level = _lgData.Levels[lastLevel];
            var zoom = Math.Pow(2, iLevel);
            return level.NodeInfoTree.NumberOfIntersectedIsLessThanBound(rect, bound, node => node.ZoomLevel > zoom);
        }

        public bool TileIsEmpty(Rectangle rectangle)
        {
            int lastLevel = GetNumberOfLevels() - 1;
            var level = _lgData.Levels[lastLevel];
            LgNodeInfo t;
            return !level.NodeInfoTree.OneIntersecting(rectangle, out t);
        }

        public IEnumerable<Node> GetTileNodes(Tuple<int, int, int> tile)
        {
            var grid = new GridTraversal(this._mainGeometryGraph.BoundingBox, tile.Item1);
            var rect = grid.GetTileRect(tile.Item2, tile.Item3);
            return
                _lgData.Levels[GetNumberOfLevels() - 1].NodeInfoTree.GetAllIntersecting(rect)
                    .Select(nodeInfo => nodeInfo.GeometryNode);
        }
    }

}
