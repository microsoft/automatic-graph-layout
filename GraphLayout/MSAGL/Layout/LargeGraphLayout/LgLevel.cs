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
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    /// <summary>
    /// class keeping a level info
    /// </summary>
    public class LgLevel {
        internal readonly Dictionary<Tuple<Point, Point>, Rail> _railDictionary =
            new Dictionary<Tuple<Point, Point>, Rail>(new LgData.CurveRailComparer());

        internal readonly Dictionary<Edge, Set<Rail>> _railsOfEdges = new Dictionary<Edge, Set<Rail>>();
        internal Set<Rail> HighlightedRails=new Set<Rail>();
        internal readonly int ZoomLevel;
        readonly GeometryGraph _geomGraph;
            
        internal LgLevel(int zoomLevel,
            GeometryGraph geomGraph // needed for statistics only
            ) {
            _geomGraph = geomGraph;
            ZoomLevel = zoomLevel;
        }

        internal RTree<Rail> _railTree;

        internal void CreateRailTree() {
            _railTree =
                new RTree<Rail>(
                    _railDictionary.Values.Select(rail => new KeyValuePair<Rectangle, Rail>(rail.BoundingBox, rail)));

            Console.WriteLine("edges = {0}, rails = {1}, edge segments = {2}", _railsOfEdges.Count, _railDictionary.Count,
                _railsOfEdges.Values.Sum(s => s.Count));

        }
        /// <summary>
        /// records the fact that the edge passes through the rail
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="rail"></param>
        public void AssociateEdgeAndRail(Edge edge, Rail rail) {
            Set<Rail> railSet;
            if (!_railsOfEdges.TryGetValue(edge, out railSet)) {
                _railsOfEdges[edge] = railSet = new Set<Rail>();
            }
            railSet.Insert(rail);
        }
  

        internal void FillRailDictionaryForEdge(Edge edge) {
            var setOfRails = new Set<Rail>();
            _railsOfEdges[edge] = setOfRails;
            FillRailDictionaryForEdgeCurve(edge.Curve, setOfRails);
            FillRailDictionaryForArrowSource(edge.EdgeGeometry.SourceArrowhead, edge.Curve, setOfRails);
            FillRailDictionaryForArrowTarget(edge.EdgeGeometry.TargetArrowhead, edge.Curve, setOfRails);
        }

        void FillRailDictionaryForArrowSource(Arrowhead sourceArrowhead, ICurve curve, Set<Rail> railSet) {
            if (sourceArrowhead == null)
                return;
            railSet.Insert(_railDictionary[new Tuple<Point, Point>(curve.Start, sourceArrowhead.TipPosition)]);
        }

        void FillRailDictionaryForArrowTarget(Arrowhead targetArrowhead, ICurve curve, Set<Rail> railSet) {
            if (targetArrowhead == null)
                return;
            railSet.Insert(_railDictionary[new Tuple<Point, Point>(curve.End, targetArrowhead.TipPosition)]);
        }

        void FillRailDictionaryForEdgeCurve(ICurve curve, Set<Rail> railSet) {
            var cc = curve as Curve;
            if (cc != null)
                foreach (var seg in cc.Segments)
                    FillRailDictionaryForSeg(seg, railSet);
            else
                FillRailDictionaryForSeg(curve, railSet);
        }

        void FillRailDictionaryForSeg(ICurve seg, Set<Rail> railSet) {
            var tuple = new Tuple<Point, Point>(seg.Start, seg.End);
            railSet.Insert(_railDictionary[tuple]);
        }

        internal void RegisterRailsOfEdge(LgEdgeInfo edgeInfo) {
            var curve = edgeInfo.Edge.Curve as Curve;
            if (curve != null)
                foreach (var seg in curve.Segments)
                    RegisterElementaryRail(edgeInfo, seg);
            else
                RegisterElementaryRail(edgeInfo, edgeInfo.Edge.Curve);
            RegisterRailsForArrowheads(edgeInfo);
        }

        void RegisterRailsForArrowheads(LgEdgeInfo edgeInfo) {
            var edgeGeom = edgeInfo.Edge.EdgeGeometry;
            if (edgeGeom.SourceArrowhead != null) {
                var tuple = new Tuple<Point, Point>(edgeGeom.SourceArrowhead.TipPosition, edgeGeom.Curve.Start);
                Rail rail;
                if (_railDictionary.TryGetValue(tuple, out rail)) {
                    if (rail.TopRankedEdgeInfoOfTheRail.Rank < edgeInfo.Rank) //the newcoming edgeInfo is more important
                        rail.TopRankedEdgeInfoOfTheRail = edgeInfo;
                }
                else
                    _railDictionary[tuple] = new Rail(edgeGeom.SourceArrowhead, edgeGeom.Curve.Start, edgeInfo,
                        ZoomLevel);
            }

            if (edgeGeom.TargetArrowhead != null) {
                var tuple = new Tuple<Point, Point>(edgeGeom.TargetArrowhead.TipPosition, edgeGeom.Curve.End);
                Rail rail;
                if (_railDictionary.TryGetValue(tuple, out rail)) {
                    if (rail.TopRankedEdgeInfoOfTheRail.Rank < edgeInfo.Rank) //the newcoming edgeInfo is more important
                        _railDictionary[tuple] = new Rail(edgeGeom.TargetArrowhead, edgeGeom.Curve.End, edgeInfo,
                            ZoomLevel);
                }
                else
                    _railDictionary[tuple] = new Rail(edgeGeom.TargetArrowhead, edgeGeom.Curve.End, edgeInfo,
                        ZoomLevel);
            }

        }

        void RegisterElementaryRail(LgEdgeInfo edgeInfo, ICurve seg) {
            Rail rail;
            var tuple = new Tuple<Point, Point>(seg.Start, seg.End);
            //was the seg registered before?
            if (_railDictionary.TryGetValue(tuple, out rail)) {
                if (rail.TopRankedEdgeInfoOfTheRail.Rank < edgeInfo.Rank) //newcoming edgeInfo is more important
                    rail.TopRankedEdgeInfoOfTheRail = edgeInfo;
            }
            else
                _railDictionary[tuple] = new Rail(seg, edgeInfo, ZoomLevel);
        }

        internal IEnumerable<Rail> GetRailsIntersectionVisRect(Rectangle visibleRectange) {
            var ret = new Set<Rail>();
            foreach (var rail in _railTree.GetAllIntersecting(visibleRectange))
                ret.Insert(rail);
            return ret;
        }

        internal List<Edge> GetEdgesPassingThroughRail(Rail rail) {
            return (from kv in _railsOfEdges where kv.Value.Contains(rail) select kv.Key).ToList();
        }

        #region Statistics

        internal void RunLevelStatistics(IEnumerable<Node> nodes)
        {
            Console.WriteLine("running stats");

            foreach (var rail in _railDictionary.Values)
                CreateStatisticsForRail(rail);

            RunStatisticsForNodes(nodes);

            double numberOfTiles = (double)ZoomLevel * ZoomLevel;
            double averageRailsForTile = 0;
            double averageVerticesForTile = 0;

            int maxVerticesPerTile = 0;
            int maxRailsPerTile = 0;
            int maxTotalPerTile = 0;

            foreach (var tileStatistic in tileTableForStatistic.Values)
            {
                averageVerticesForTile += tileStatistic.vertices / numberOfTiles;
                averageRailsForTile += tileStatistic.rails / numberOfTiles;
                if (maxRailsPerTile < tileStatistic.rails)
                    maxRailsPerTile = tileStatistic.rails;
                if (maxVerticesPerTile < tileStatistic.vertices)
                    maxVerticesPerTile = tileStatistic.vertices;
                if (maxTotalPerTile < tileStatistic.vertices + tileStatistic.rails)
                    maxTotalPerTile = tileStatistic.vertices + tileStatistic.rails;
            }

            Console.WriteLine("level {0}: average rails per tile {1}\n" +
                              "average verts per tile {2}, total average per tile {1}.\n", ZoomLevel,
                averageRailsForTile, averageVerticesForTile);

            Console.WriteLine("max rails per tile {0}\n" +
                              "max verts per tile {1}, total max per tile {2}.\n", maxRailsPerTile,
                maxVerticesPerTile, maxTotalPerTile);

            Console.WriteLine("done with stats");
        }

        void RunStatisticsForNodes(IEnumerable<Node> nodes)
        {
            foreach (var node in nodes)
                CreateStatisticsForNode(node);
        }

        void CreateStatisticsForNode(Node node)
        {
            foreach (var tile in GetCurveTiles(node.BoundaryCurve))
                tile.vertices++;

        }

        void CreateStatisticsForRail(Rail rail)
        {
            var arrowhead = rail.Geometry as Arrowhead;
            if (arrowhead != null)
                CreateStatisticsForArrowhead(arrowhead);
            else
                foreach (var t in GetCurveTiles(rail.Geometry as ICurve))
                    t.rails++;
        }

        void CreateStatisticsForArrowhead(Arrowhead arrowhead)
        {
            TileStatistic tile = GetOrCreateTileStatistic(arrowhead.TipPosition);
            tile.rails++;
        }

        TileStatistic GetOrCreateTileStatistic(Point p)
        {
            Tuple<int, int> t = DeviceIndependendZoomCalculatorForNodes.PointToTuple(_geomGraph.LeftBottom, p,
                GetGridSize());
            TileStatistic ts;
            if (tileTableForStatistic.TryGetValue(t, out ts))
                return ts;

            tileTableForStatistic[t] = ts = new TileStatistic { rails = 0, vertices = 0 };
            return ts;
        }

        IEnumerable<TileStatistic> GetCurveTiles(ICurve curve)
        {
            var tiles = new Set<TileStatistic>();
            const int n = 64;
            var s = curve.ParStart;
            var e = curve.ParEnd;
            var d = (e - s) / (n - 1);
            for (int i = 0; i < 64; i++)
            {
                var t = s + i * d;
                var ts = GetOrCreateTileStatistic(curve[t]);
                tiles.Insert(ts);
            }
            return tiles;
        }

        class TileStatistic
        {
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

    }
}