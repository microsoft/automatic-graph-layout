using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.OverlapRemovalFixedSegments;
using Microsoft.Msagl.Miscellaneous.RegularGrid;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing.Visibility;
using SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;

namespace Microsoft.Msagl.Layout.LargeGraphLayout.NodeRailLevelCalculator {
    public class GreedyNodeRailLevelCalculator {

        public List<LgNodeInfo> SortedLgNodeInfos { get; set; }

        public int ReroutingAttempts = 0;

        public Rectangle BoundingBox;
        public double MaxGridSize;

        public int NumCones = 8;

        public int MaxLevel = 1024*16;

        public int MaxAmountNodesPerTile = 15; //20; debug

        public int MaxAmountRailsPerTile = 20*8*2/4; //debug

        readonly List<LgNodeInfo> _insertedNodes = new List<LgNodeInfo>();
        
        readonly RTree<LgNodeInfo, Point> _insertedNodesTree = new RTree<LgNodeInfo, Point>();
        readonly RTree<Rectangle, Point> _insertedNodeRectsTree = new RTree<Rectangle, Point>();

        readonly RTree<SymmetricSegment, Point> _insertedSegmentsTree = new RTree<SymmetricSegment, Point>();

        Dictionary<Tuple<int, int>, int> _nodeTileTable = new Dictionary<Tuple<int, int>, int>();
        Dictionary<Tuple<int, int>, int> _segmentTileTable = new Dictionary<Tuple<int, int>, int>();

        readonly Dictionary<int, List<LgNodeInfo>> _nodeLevels = new Dictionary<int, List<LgNodeInfo>>();
        
        public double ScaleBbox = 1.10;

        public bool UpdateBitmapForEveryInsertion = true;

        readonly LgPathRouter _pathRouter = new LgPathRouter();

        public GreedyNodeRailLevelCalculator(List<LgNodeInfo> sortedLgNodeInfos) {
            SortedLgNodeInfos = sortedLgNodeInfos;

            foreach (var node in SortedLgNodeInfos) {
                node.Processed = false;
            }

            InitBoundingBox();
        }

        public void InitBoundingBox() {
            BoundingBox = new Rectangle(SortedLgNodeInfos.Select(ni => ni.Center));
            //Point center = BoundingBox.Center;
            //MaxGridSize = Math.Max(BoundingBox.Width, BoundingBox.Height)*ScaleBbox;
            //BoundingBox = new Rectangle(new Size(MaxGridSize, MaxGridSize), center);
        }


        public void PlaceNodesOnly(Rectangle bbox)
        {
            BoundingBox = bbox;
            int numInserted = 0;
            int level = 1;
            int iLevel = 0;

            while (numInserted < SortedLgNodeInfos.Count && level <= MaxLevel) {
                numInserted = DrawNodesOnlyOnLevel(level, numInserted);
                AddAllToNodeLevel(iLevel);
                level *= 2;
                iLevel++;
            }
        }

        void MarkAllNodesNotProcessed() {
            foreach (var node in SortedLgNodeInfos) {
                node.Processed = false;
            }
        }

        internal int TryInsertingNodesAndRoutes(int numNodesToInsert,
            Dictionary<SymmetricTuple<LgNodeInfo>, List<Point>> trajectories,
            List<SymmetricSegment> oldSegments,  
            int zoomLevel, int numNodesOnPreviousLevel,
            GridTraversal grid, LgPathRouter pathRouter)
        {
            MarkAllNodesNotProcessed();
            _segmentTileTable = new Dictionary<Tuple<int, int>, int>();
            _nodeTileTable = new Dictionary<Tuple<int, int>, int>();

            var canAddOldSegments = TryAddingOldSegments(oldSegments, grid);
            if (!canAddOldSegments)
            {
                return 0;
            }

            AddOldNodes(numNodesOnPreviousLevel, grid);

            int i;
            for (i = numNodesOnPreviousLevel; i < numNodesToInsert; i++)
            {
                var ni = SortedLgNodeInfos[i];
                var nodeTile = grid.PointToTuple(ni.Center);
                if (!_nodeTileTable.ContainsKey(nodeTile))
                    _nodeTileTable[nodeTile] = 0;

                if (_nodeTileTable[nodeTile] >= MaxNodesPerTile(zoomLevel)) //test MaxAmountNodesPerTile
                {
                    ShowDebugInsertedSegments(grid, zoomLevel, ni, null, null);

                    break;
                }

                Set<VisibilityEdge> edges = GetSegmentsOnPathsToInsertedNeighborsNotOnOldTrajectories(ni, trajectories,
                    pathRouter);

                Set<SymmetricSegment> segments = new Set<SymmetricSegment>(
                    edges.Select(e => new SymmetricSegment(e.SourcePoint, e.TargetPoint)));

                var newToAdd = segments.Where(seg => !IsSegmentAlreadyAdded(seg)).ToList();

                Set<SymmetricSegment> insertedSegments;
                bool canInsertPaths = TryAddingSegmentsUpdateTiles(newToAdd, grid, out insertedSegments);

                if (canInsertPaths) {

                    AddSegmentsToRtree(newToAdd);
                    ni.Processed = true;
                    _nodeTileTable[nodeTile]++;
                    _insertedNodes.Add(ni);
                    continue;
                }
                //debug output
                //AddSegmentsToRtree(newToAdd);   //remove
            //    ShowDebugInsertedSegments(grid, zoomLevel, ni, newToAdd, segments);
                break;
            }

            var nextNode = numNodesToInsert < SortedLgNodeInfos.Count ? SortedLgNodeInfos[numNodesToInsert] :
            null;
           // ShowDebugInsertedSegments(grid, zoomLevel, nextNode, null, null);

            return i;
        }

        private Set<VisibilityEdge> GetSegmentsOnPathsToInsertedNeighborsNotOnOldTrajectories(LgNodeInfo ni, Dictionary<SymmetricTuple<LgNodeInfo>, List<Point>> trajectories, LgPathRouter pathRouter)
        {
            var edges = new Set<VisibilityEdge>();
            var neighbors = GetAdjacentProcessed(ni);
            foreach (var neighb in neighbors) {
                var t1 = new SymmetricTuple<LgNodeInfo>(ni, neighb);
                List<Point> trajectory;
                if (trajectories.ContainsKey(t1)) trajectory = trajectories[t1];
                else continue;
                for (int i = 0; i < trajectory.Count - 1; i++)
                {
                    var p0 = trajectory[i];
                    var p1 = trajectory[i + 1];

                    var e = pathRouter.FindEdge(p0, p1);

                    Debug.Assert(e!=null, "VisibilityEdge on trajectory not found!");

                    if (!pathRouter.IsOnOldTrajectory(e))
                    {
                        edges.Insert(e);
                    }
                }
            }
            return edges;
        }

        private bool TryAddingOldSegments(List<SymmetricSegment> oldSegments, GridTraversal grid) {
            Set<SymmetricSegment> insertedSegments;
            bool canInsertOldPaths = TryAddingSegmentsUpdateTiles(oldSegments, grid, out insertedSegments);

            if (canInsertOldPaths) {
                AddSegmentsToRtree(oldSegments);
                return true;
            }

            // if couldn't even insert previous level, terminate
            return false;            
        }

        private void AddOldNodes(int numNodesOnPreviousLevel, GridTraversal grid) {
            for (int i = 0; i < numNodesOnPreviousLevel; i++) {
                var ni = SortedLgNodeInfos[i];
                var nodeTile = grid.PointToTuple(ni.Center);
                if (!_nodeTileTable.ContainsKey(nodeTile))
                    _nodeTileTable[nodeTile] = 0;

                ni.Processed = true;
                _nodeTileTable[nodeTile]++;
                _insertedNodes.Add(ni);
            }
        }

        
        private void ShowDebugInsertedSegments(GridTraversal grid, int zoomLevel, LgNodeInfo nodeToAdd, IEnumerable<SymmetricSegment> newToAdd, IEnumerable<SymmetricSegment> allOnNewEdges)
        {
#if TEST_MSAGL && !SHARPKIT && PREPARE_DEMO

            var edges = _pathRouter.GetAllEdgesVisibilityEdges();
            var ll = new List<DebugCurve>();

            foreach (var ni in _insertedNodes) {
                ll.Add(new DebugCurve(5, "green", ni.BoundaryCurve));
            }

            if (nodeToAdd != null) {
                var curve = _insertedNodes.Last().BoundaryCurve.Clone();
                curve.Translate(nodeToAdd.Center - _insertedNodes.Last().Center);
                ll.Add(new DebugCurve(5, "red", curve));
            }
            
            foreach (var e in edges)
            {
                ll.Add(new DebugCurve(new LineSegment(e.SourcePoint, e.TargetPoint)));
            }

            int n = zoomLevel;
            int maxNodes = MaxNodesPerTile(zoomLevel);

            for (int ix = 0; ix < n; ix++)
            {
                for (int iy = 0; iy < n; iy++)
                {
                    var tile = new Tuple<int, int>(ix, iy);
                    var r = grid.GetTileRect(ix, iy);
                    
                    if (_nodeTileTable.ContainsKey(tile)
                        && _nodeTileTable[tile] >= maxNodes)
                    {
                        ll.Add(new DebugCurve(5, "yellow", CurveFactory.CreateRectangle(r)));
                    }
                    else if (_segmentTileTable.ContainsKey(tile)
                             && _segmentTileTable[tile] >= MaxAmountRailsPerTile)
                    {
                        ll.Add(new DebugCurve(5, "orange", CurveFactory.CreateRectangle(r)));                    
                    }
                    else
                    {
                        ll.Add(new DebugCurve(5, "blue", CurveFactory.CreateRectangle(r)));
                    }
                }
            }

            if (allOnNewEdges != null) {
                foreach (var seg in allOnNewEdges) {
                    ll.Add(new DebugCurve(5, "yellow", new LineSegment(seg.A, seg.B)));
                }
            }

            if (newToAdd != null)
            {
                foreach (var seg in newToAdd)
                {
                    ll.Add(new DebugCurve(5, "red", new LineSegment(seg.A, seg.B)));
                }
            }

            LayoutAlgorithmSettings.ShowDebugCurves(ll.ToArray());

            PrintInsertedNodesLabels();
#endif
        }

        public string PrintInsertedNodesLabels()
        {
            var bbox = BoundingBox;
            var str = "<group>\n<path stroke=\"black\">";
            str += bbox.LeftTop.X + " " + bbox.LeftTop.Y + " m\n";
            str += bbox.LeftBottom.X + " " + bbox.LeftBottom.Y + " l\n";
            str += bbox.RightBottom.X + " " + bbox.RightBottom.Y + " l\n";
            str += bbox.RightTop.X + " " + bbox.RightTop.Y + " l\n";
            str += "h\n</path>\n";

            int i = 0;
            foreach (var ni in _insertedNodes)
            {
                str += "<text transformations=\"translations\" pos=\"";
                str += ni.Center.X + " " + ni.Center.Y;
                str += "\" stroke=\"black\" type=\"label\" valign=\"baseline\">" +i +"</text>\n";
                i++;
            }
            str += "</group>";
            return str;
        }

        bool TryAddingSegmentsUpdateTiles(IEnumerable<SymmetricSegment> segments, GridTraversal grid,
            out Set<SymmetricSegment> insertedSegments) {
            if (!IfCanInsertLooseSegmentsUpdateTiles(segments.ToList(), out insertedSegments, grid)) {
                // quota broken when inserting node boundary segments
                RemoveLooseSegmentsDecrementTiles(insertedSegments, grid);
                return false;
            }
            return true;
        }


        void AddNodeToInserted(LgNodeInfo ni) {
            _insertedNodes.Add(ni);

            var rect = new Rectangle(ni.Center, ni.Center);

            _insertedNodesTree.Add(rect, ni);
            _insertedNodeRectsTree.Add(rect, rect);
        }

        void AddAllToNodeLevel(int iLevel) {
            if (!_nodeLevels.ContainsKey(iLevel))
                _nodeLevels[iLevel] = new List<LgNodeInfo>();

            foreach (var ni in _insertedNodes)
            {
                _nodeLevels[iLevel].Add(ni);
            }
        }

        public List<int> GetLevelNodeCounts()
        {
            var nl = new List<int>(_nodeLevels.Values.Select(l => l.Count));
            nl.Sort();
            return nl;
        }

        int DrawNodesOnlyOnLevel(int level, int startInd)
        {
            int iLevel = (int) Math.Log(level, 2);
            GridTraversal grid= new GridTraversal(BoundingBox, iLevel);
            UpdateTilesCountInsertedNodesOnly(level, grid);
            for (int i = startInd; i < SortedLgNodeInfos.Count; i++) {
                var ni = SortedLgNodeInfos[i];
                var tuple = grid.PointToTuple(ni.Center);
                if (!_nodeTileTable.ContainsKey(tuple))
                    _nodeTileTable[tuple] = 0;

                if (_nodeTileTable[tuple] >= MaxNodesPerTile(level)) {
                    return i;
                }

                PerformNodeInsertion(ni, tuple);
                ni.ZoomLevel = level;
            }

            return SortedLgNodeInfos.Count;
        }

        int MaxNodesPerTile(int zoomLevel) {
            //if (iLevel > 2) return MaxAmountNodesPerTile;
            //return 3*MaxAmountNodesPerTile/2;
            var iLevel = Math.Log(zoomLevel, 2);
            int maxNodes = MaxAmountNodesPerTile;
            //maxNodes += (int)(IncreaseNodeQuota * Math.Sqrt(iLevel) * MaxAmountNodesPerTile);
            maxNodes += (int) (IncreaseNodeQuota*Math.Pow(iLevel, 0.4)*MaxAmountNodesPerTile);
            return maxNodes;
        }

        
        Set<LgNodeInfo> GetAdjacentProcessed(LgNodeInfo ni) {
            var nodes = new Set<LgNodeInfo>(from edge in ni.GeometryNode.Edges
                let s = GeometryNodesToLgNodeInfos[edge.Source]
                let t = GeometryNodesToLgNodeInfos[edge.Target]
                select ni == s ? t : s
                into v
                where v.Processed
                select v);
            return nodes;
        }

        void PerformNodeInsertion(LgNodeInfo node, Tuple<int, int> tile) {
            _nodeTileTable[tile]++;
            AddNodeToInserted(node);
            //orb.DrawDilated(node.BoundingBox);
        }


        internal bool IfCanInsertLooseSegmentUpdateTiles(SymmetricSegment seg,  GridTraversal grid) {
            //test if already inserted
            if (IsSegmentAlreadyAdded(seg))
                return true;

            var intersectedTiles = GetIntersectedTiles(seg.A, seg.B, grid);

            int maxNumRailPerTile = 0;

            bool canInsertSegment = true;
            foreach (var tile in intersectedTiles) {
                if (!_segmentTileTable.ContainsKey(tile))
                    _segmentTileTable[tile] = 0;
                if(maxNumRailPerTile<_segmentTileTable[tile])maxNumRailPerTile = _segmentTileTable[tile];
                canInsertSegment &= _segmentTileTable[tile] < MaxAmountRailsPerTile;
            }

            if (!canInsertSegment)
            {
                return false;
            }

            foreach (var tile in intersectedTiles) {
                _segmentTileTable[tile]++;
            }
            return true;
        }

        bool IfCanInsertLooseSegmentsUpdateTiles(IEnumerable<SymmetricSegment> segs, out Set<SymmetricSegment> insertedSegs, GridTraversal grid) {
            insertedSegs = new Set<SymmetricSegment>();
            foreach (var seg in segs) {
                if (IfCanInsertLooseSegmentUpdateTiles(seg, grid))
                    insertedSegs.Insert(seg);
                else
                    return false;
            }
            return true;
        }

        void RemoveLooseSegmentsDecrementTiles(IEnumerable<SymmetricSegment> segs, GridTraversal grid) {
            foreach (var seg in segs) {
                RemoveLooseSegmentDecrementTiles(seg, grid);
            }
        }

        void RemoveLooseSegmentDecrementTiles(SymmetricSegment seg, GridTraversal grid) {
            var intersectedTiles = GetIntersectedTiles(seg.A, seg.B,  grid);

            foreach (var tile in intersectedTiles) {
                if (_segmentTileTable.ContainsKey(tile))
                    _segmentTileTable[tile]--;
            }
        }

        void AddSegmentToRtree(SymmetricSegment seg) {
            if (IsSegmentAlreadyAdded(seg))
                return;

            var rect = new Rectangle(seg.A, seg.B);
            //rect = new Rectangle(rect.LeftBottom - segRtreeBuffer*new Point(1, 1),
            //    rect.RightTop + segRtreeBuffer*new Point(1, 1));

            _insertedSegmentsTree.Add(rect, seg);

            // add to visgraph
            _pathRouter.AddVisGraphEdge(seg.A, seg.B);
        }


        bool IsSegmentAlreadyAdded(SymmetricSegment seg) {
            return _pathRouter.ExistsEdge(seg.A, seg.B);
        }

        void AddSegmentsToRtree(IEnumerable<SymmetricSegment> segs) {
            foreach (var seg in segs) {
                AddSegmentToRtree(seg);
            }
        }


        void UpdateTilesCountInsertedNodes(int level, GridTraversal grid) {
            foreach (var node in _insertedNodes) {
                var tuple = grid.PointToTuple(node.Center);
                if (!_nodeTileTable.ContainsKey(tuple))
                    _nodeTileTable[tuple] = 0;

                _nodeTileTable[tuple]++;
            }
        }

        void UpdateTilesCountInsertedNodesOnly(int level, GridTraversal grid) {
            _nodeTileTable = new Dictionary<Tuple<int, int>, int>();
            UpdateTilesCountInsertedNodes(level, grid);
        }

        List<Tuple<int, int>> GetIntersectedTiles(Point p1, Point p2,  GridTraversal grid) {
            return grid.GetTilesIntersectedByLineSeg(p1, p2);
        }

        public Dictionary<Node, LgNodeInfo> GeometryNodesToLgNodeInfos { get; set; }
        public double IncreaseNodeQuota { get; set; }
    }
}
