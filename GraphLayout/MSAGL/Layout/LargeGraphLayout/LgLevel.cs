using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;
namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    /// <summary>
    /// class keeping a level info
    /// </summary>
    public class LgLevel {
        internal readonly Dictionary<SymmetricSegment, Rail> _railDictionary =
            new Dictionary<SymmetricSegment, Rail>();

        internal Dictionary<SymmetricSegment, Rail> RailDictionary {
            get { return _railDictionary; }
        }

        internal readonly Dictionary<Edge, Set<Rail>> _railsOfEdges = new Dictionary<Edge, Set<Rail>>();
        internal Set<Rail> HighlightedRails = new Set<Rail>();
        internal readonly int ZoomLevel;
        readonly GeometryGraph _geomGraph;
        internal RTree<Rail,Point> _railTree = new RTree<Rail,Point>();
        internal RTree<Rail,Point> RailTree {
            get { return _railTree; }
        }

        RTree<LgNodeInfo, Point> _nodeInfoTree = new RTree<LgNodeInfo, Point>();

        internal readonly Dictionary<Edge, List<Rail>> _orderedRailsOfEdges = new Dictionary<Edge, List<Rail>>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="zoomLevel"></param>
        /// <param name="geomGraph">needed only for statistics</param>
        internal LgLevel(int zoomLevel, GeometryGraph geomGraph) {
            _geomGraph = geomGraph;
            ZoomLevel = zoomLevel;
        }

        internal RTree<LgNodeInfo, Point> NodeInfoTree {
            get { return _nodeInfoTree; }            
        }


        internal void CreateEmptyRailTree() {
            _railTree = new RTree<Rail,Point>();
        }

        //public void CreateRailTree() {
        //    RailTree = new RTree<Rail,Point>(
        //        RailDictionary.Values.Select(rail => new KeyValuePair<Rectangle, Rail>(rail.BoundingBox, rail)));
        //}

        /// <summary>
        /// get endpoint tuples of all rails
        /// </summary>
        /// <returns></returns>
        public List<SymmetricSegment> GetAllRailsEndpoints() {
            var endpts = new List<SymmetricSegment>();
            Point p0, p1;
            foreach (var rail in _railTree.GetAllLeaves()) {
                if (rail.GetStartEnd(out p0, out p1)) {
                    endpts.Add(new SymmetricSegment(p0, p1));
                }
            }
            return endpts;
        }

        public List<Rail> FetchOrCreateRailSequence(List<Point> path) {
            List<Rail> rails = new List<Rail>();
            for (int i = 0; i < path.Count - 1; i++) {
                var rail = FindOrCreateRail(path[i], path[i + 1]);
                if (rail == null)
                    continue;

                rails.Add(rail);
            }
            return rails;
        }

        public Rail FindOrCreateRail(Point s, Point t) {
            var p0 = s;
            var p1 = t;

            var t1 = new SymmetricSegment(p0, p1);
            Rail rail;
            if (RailDictionary.TryGetValue(t1, out rail)) return rail;
            var t2 = new SymmetricSegment(p1, p0);
            if (RailDictionary.TryGetValue(t2, out rail)) return rail;
            return CreateRail(p0, p1);
        }

        public Rail CreateRail(Point ep0, Point ep1) {
            var st = new SymmetricSegment(ep0, ep1);
            Rail rail;
            if (RailDictionary.TryGetValue(st, out rail)) {
                return rail;
            }
            var ls = new LineSegment(ep0, ep1);
            rail = new Rail(ls, ZoomLevel);
            RailTree.Add(rail.BoundingBox, rail);
            RailDictionary.Add(st, rail);
            return rail;
        }

        public Rail FindRail(Point s, Point t) {
            var p0 = s;
            var p1 = t;
            var ss = new SymmetricSegment(p1, p0);
            Rail rail;
            RailDictionary.TryGetValue(ss, out rail);
            return rail;
        }
        
        internal IEnumerable<Rail> GetRailsIntersectingRect(Rectangle visibleRectange) {
            var ret = new Set<Rail>();
            foreach (var rail in _railTree.GetAllIntersecting(visibleRectange))
                ret.Insert(rail);
            return ret;
        }

        internal List<Edge> GetEdgesPassingThroughRail(Rail rail) {
            return (from kv in _railsOfEdges where kv.Value.Contains(rail) select kv.Key).ToList();
        }

        public Rail RemoveRailFromRtree(Rail rail) {
            return _railTree.Remove(rail.BoundingBox, rail);
        }

        /// <summary>
        /// try adding single rail to dictionary
        /// </summary>
        /// <param name="rail"></param>
        /// <returns>true iff the rail does not belong to _railDictionary</returns>
        public bool AddRailToDictionary(Rail rail) {
            Point p0, p1;
            if (!rail.GetStartEnd(out p0, out p1)) return false;
            var st = new SymmetricSegment(p0, p1);
            if (!_railDictionary.ContainsKey(st)) {
                _railDictionary.Add(st, rail);
                return true;
            }
            return false;
        }

        public void AddRailToRtree(Rail rail) {
            Point p0, p1;
            if (!rail.GetStartEnd(out p0, out p1)) return;
            if (_railTree.Contains(new Rectangle(p0, p1), rail))
                return;
            _railTree.Add(new Rectangle(p0, p1), rail);
        }

        public void RemoveRailFromDictionary(Rail rail) {
            Point p0, p1;
            if (!rail.GetStartEnd(out p0, out p1)) return;
            _railDictionary.Remove(new SymmetricSegment(p0, p1));
        }

        internal bool PrintQuota(IEnumerable<Node> nodes, int NodeQuota, int RailQuota)
        {
            tileTableForStatistic.Clear();
            foreach (var rail in _railDictionary.Values)
                if (rail.ZoomLevel == ZoomLevel)
                    CreateStatisticsForRail(rail);

            RunStatisticsForNodes(nodes);


            int maxVerticesPerTile = 0;
            int maxRailsPerTile = 0;

            foreach (var tileStatistic in tileTableForStatistic.Values)
            {
                if (maxRailsPerTile < tileStatistic.rails)
                    maxRailsPerTile = tileStatistic.rails;
                if (maxVerticesPerTile < tileStatistic.vertices)
                    maxVerticesPerTile = tileStatistic.vertices;
            }

            System.Diagnostics.Debug.WriteLine("max rails per tile {0}\n" + "max verts per tile {1}.\n", maxRailsPerTile, maxVerticesPerTile);

            if (maxVerticesPerTile <= NodeQuota && maxRailsPerTile <= RailQuota) return true;
            //if ( maxRailsPerTile <= RailQuota) return true;
            return false;
        }

        internal bool QuotaSatisfied(IEnumerable<Node> nodes, int NodeQuota, int RailQuota)
        {
            tileTableForStatistic.Clear();
            foreach (var rail in _railDictionary.Values)
                if (rail.ZoomLevel == ZoomLevel)
                    CreateStatisticsForRail(rail);
 
            RunStatisticsForNodes(nodes);

 
            int maxVerticesPerTile = 0;
            int maxRailsPerTile = 0;
 
            foreach (var tileStatistic in tileTableForStatistic.Values)
            {
                 if (maxRailsPerTile < tileStatistic.rails)
                    maxRailsPerTile = tileStatistic.rails;
                if (maxVerticesPerTile < tileStatistic.vertices)
                    maxVerticesPerTile = tileStatistic.vertices;
             }
             
            if ( maxRailsPerTile <= RailQuota) 
                return true;
            
            return false;
        }

        #region Statistics

        internal void RunLevelStatistics(IEnumerable<Node> nodes) {
            System.Diagnostics.Debug.WriteLine("running stats");

            foreach (var rail in _railDictionary.Values)
                CreateStatisticsForRail(rail);

            RunStatisticsForNodes(nodes);

            double numberOfTiles = (double) ZoomLevel*ZoomLevel;
            double averageRailsForTile = 0;
            double averageVerticesForTile = 0;

            int maxVerticesPerTile = 0;
            int maxRailsPerTile = 0;
            int maxTotalPerTile = 0;

            foreach (var tileStatistic in tileTableForStatistic.Values) {
                averageVerticesForTile += tileStatistic.vertices/numberOfTiles;
                averageRailsForTile += tileStatistic.rails/numberOfTiles;
                if (maxRailsPerTile < tileStatistic.rails)
                    maxRailsPerTile = tileStatistic.rails;
                if (maxVerticesPerTile < tileStatistic.vertices)
                    maxVerticesPerTile = tileStatistic.vertices;
                if (maxTotalPerTile < tileStatistic.vertices + tileStatistic.rails)
                    maxTotalPerTile = tileStatistic.vertices + tileStatistic.rails;
            }

            System.Diagnostics.Debug.WriteLine("level {0}: average rails per tile {1}\n" +
                              "average verts per tile {2}, total average per tile {1}.\n", ZoomLevel,
                averageRailsForTile, averageVerticesForTile);

            System.Diagnostics.Debug.WriteLine("max rails per tile {0}\n" +
                              "max verts per tile {1}, total max per tile {2}.\n", maxRailsPerTile,
                maxVerticesPerTile, maxTotalPerTile);

            System.Diagnostics.Debug.WriteLine("done with stats");
        }
 

        void RunStatisticsForNodes(IEnumerable<Node> nodes) {
            foreach (var node in nodes)
            {
                CreateStatisticsForNode(node);
            }
        }

        void CreateStatisticsForNode(Node node) {
            foreach (var tile in GetCurveTiles(node.BoundaryCurve))
                tile.vertices++;

        }

        void CreateStatisticsForRail(Rail rail) {
            var arrowhead = rail.Geometry as Arrowhead;
            if (arrowhead != null)
                CreateStatisticsForArrowhead(arrowhead);
            else
                foreach (var t in GetCurveTiles(rail.Geometry as ICurve))
                    t.rails++;
        }

        void CreateStatisticsForArrowhead(Arrowhead arrowhead) {
            TileStatistic tile = GetOrCreateTileStatistic(arrowhead.TipPosition);
            tile.rails++;
        }

        TileStatistic GetOrCreateTileStatistic(Point p) {
            Tuple<int, int> t = DeviceIndependendZoomCalculatorForNodes.PointToTuple(_geomGraph.LeftBottom, p,
                GetGridSize());
            TileStatistic ts;
            if (tileTableForStatistic.TryGetValue(t, out ts))
                return ts;

            tileTableForStatistic[t] = ts = new TileStatistic {rails = 0, vertices = 0};
            return ts;
        }

        IEnumerable<TileStatistic> GetCurveTiles(ICurve curve) {
            var tiles = new Set<TileStatistic>();
            const int n = 64;
            var s = curve.ParStart;
            var e = curve.ParEnd;
            var d = (e - s)/(n - 1);
            for (int i = 0; i < 64; i++) {
                var t = s + i*d;
                var ts = GetOrCreateTileStatistic(curve[t]);
                tiles.Insert(ts);
            }
            return tiles;
        }

        class TileStatistic {
            public int vertices;
            public int rails;
        }
        readonly Dictionary<Tuple<int, int>, TileStatistic> tileTableForStatistic =
           new Dictionary<Tuple<int, int>, TileStatistic>();

        double GetGridSize()
        {
            return Math.Max(_geomGraph.Width, _geomGraph.Height) / ZoomLevel;
        }

        #endregion


        internal void AddRail(Rail rail) {
            if (AddRailToDictionary(rail))
                AddRailToRtree(rail);
        }

        public void ClearRailTree() {
            _railTree.Clear();
        }

        public void CreateNodeTree(IEnumerable<LgNodeInfo> nodeInfos, double nodeDotWidth) {
            foreach (var n in nodeInfos)
                NodeInfoTree.Add(new RectangleNode<LgNodeInfo,Point>(n,
                    new Rectangle(new Size(nodeDotWidth, nodeDotWidth),n.Center)));
        }

        public IEnumerable<Node> GetNodesIntersectingRect(Rectangle visibleRectangle) {
            return NodeInfoTree.GetAllIntersecting(visibleRectangle).Select(n => n.GeometryNode);
        }
        public IEnumerable<Node> GetNodesIntersectingRectLabelzero(Rectangle visibleRectangle, double l)
        {
            var x =  NodeInfoTree.GetAllIntersecting(visibleRectangle);//.Select(n => n.GeometryNode);
            List<Node> r = new List<Node>();
            foreach (var y in x)
            {
                if(y.ZoomLevel <= l) r.Add(y.GeometryNode);

            }
            return r;
        }
        public bool RectIsEmptyOnLevel(Rectangle tileBox) {
            LgNodeInfo lg;
            return !NodeInfoTree.OneIntersecting(tileBox, out lg);
        }

        internal void RemoveFromRailEdges(List<SymmetricTuple<LgNodeInfo>> removeList) {
            foreach (var symmetricTuple in removeList)
                RemoveTupleFromRailEdges(symmetricTuple);
        }

        void RemoveTupleFromRailEdges(SymmetricTuple<LgNodeInfo> tuple) {
            var a = tuple.A.GeometryNode;
            var b = tuple.B.GeometryNode;
            foreach (var edge in EdgesBetween(a, b))
                _railsOfEdges.Remove(edge);
        }

        IEnumerable<Edge> EdgesBetween(Node a, Node b) {
            foreach (var e in a.InEdges.Where(e => e.Source == b))
                yield return e;
            foreach (var e in a.OutEdges.Where(e => e.Target == b))
                yield return e;
        }
    }
}