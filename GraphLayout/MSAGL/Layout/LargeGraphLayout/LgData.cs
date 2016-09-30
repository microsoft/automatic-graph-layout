using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Windows.Media;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Prototype.Ranking;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    /// <summary>
    /// data for large graph browsing
    /// </summary>
    public class LgData {
        readonly List<GeometryGraph> _connectedComponents = new List<GeometryGraph>();
        internal Dictionary<Node, LgNodeInfo> GeometryNodesToLgNodeInfos;
        internal readonly Dictionary<Edge, LgEdgeInfo> GeometryEdgesToLgEdgeInfos = new Dictionary<Edge, LgEdgeInfo>();

        readonly Set<Edge> _selectedEdges=new Set<Edge>();
        Set<LgNodeInfo> _selectedNodeInfos = new Set<LgNodeInfo>();
        readonly List<LgLevel> _levels = new List<LgLevel>();

        internal readonly List<LgSkeletonLevel> SkeletonLevels = new List<LgSkeletonLevel>();

        /// <summary>
        /// the list of levels
        /// </summary>
        public IList<LgLevel> Levels { get { return _levels; } }

        internal LgData(GeometryGraph mainGeomGraph) {
            this.mainGeomGraph = mainGeomGraph;
        }

       

        #region Level class

        #endregion  end of Level class

        
        internal void AddConnectedGeomGraph(GeometryGraph geomGraph) {
            _connectedComponents.Add(geomGraph);
        }

        internal IEnumerable<GeometryGraph> ConnectedGeometryGraphs {
            get { return _connectedComponents; }
        }

        /// <summary>
        /// LgNodeInfos sorted by their importance
        /// </summary>
        public List<LgNodeInfo> SortedLgNodeInfos { get; set; }
        /// <summary>
        /// the last indices of nodes on levels, the indices point to SortedLgNodeInfos
        /// </summary>
        public List<int> LevelNodeCounts { get; set; }


        /*
                static void showdeletelater(ICurve curve, LinkedList<Tuple<double, Point>> samples, Point middleValue) {
                    var l = new List<DebugCurve>();
                    l.Add(new DebugCurve(100, 1, "black", curve));
                    foreach (var sample in samples)
                        l.Add(new DebugCurve( 0.1, "red", new Ellipse(1, 1, sample.Item2)));
                    l.Add(new DebugCurve(1, "blue", new Ellipse(3, 3, middleValue)));

                    LayoutAlgorithmSettings.ShowDebugCurves(l.ToArray());
                }
        */

        internal LgLevel GetCurrentLevelByScale(double zoomLevel) {
            return this._levels[GetLevelIndexByScale(zoomLevel)];
        }
        
        internal LgLevel AddLevel() {
            int zoomLevel = (int) Math.Pow(2, _levels.Count());
            var level = new LgLevel(zoomLevel, mainGeomGraph);
            _levels.Add(level);
            return level;
        }

        GeometryGraph mainGeomGraph; //needed for statistice only
        
//        public void ExtractRailsFromRouting(EdgeCollection edges, int levelZoom) {
//            var level = AddLevel(levelZoom);
//            level.CreateRailTree(edges);
//
//
//
//            Console.WriteLine("segs={0} rail count = {1}",
//                              edges.Sum(
//                                  e =>
//                                  ((e.Curve is Curve) ? ((Curve) e.Curve).Segments.Count : 1) +
//                                  (e.EdgeGeometry.SourceArrowhead != null ? 1 : 0) +
//                                  (e.EdgeGeometry.TargetArrowhead != null ? 1 : 0)), level.Rails.Count());
//        }

        //        void UpdateEdgeGeometriesByLevel(int levelIndex, Rail rail) {
        //            var edgeInfo = rail.EdgeInfo;
        //            var edgeGeomsByLevels = edgeInfo.EdgeGeometriesByLevels;
        //            if (edgeGeomsByLevels == null)
        //                edgeInfo.EdgeGeometriesByLevels = edgeGeomsByLevels = new List<EdgePartialGeometryOnLevel>();
        //
        //            EdgePartialGeometryOnLevel edgePartialGeom;
        //            if (edgeInfo.EdgeGeometriesByLevels.Count < levelIndex + 1)
        //                edgeGeomsByLevels.Add(edgePartialGeom = new EdgePartialGeometryOnLevel());
        //            else
        //                edgePartialGeom = edgeGeomsByLevels[levelIndex];
        //
        //            var geom = rail.Geometry;
        //            var edgeGeometry = edgeInfo.Edge.EdgeGeometry;
        //            var arrowhead = geom as Arrowhead;
        //            if (arrowhead != null) {
        //                if (arrowhead == edgeGeometry.SourceArrowhead) {
        //                    edgePartialGeom.SourceArrowhead = arrowhead;
        //                } else {
        //                    Debug.Assert(arrowhead == edgeGeometry.TargetArrowhead);
        //                    edgePartialGeom.TargetArrowhead = arrowhead;
        //                }
        //            } else {
        //                if (edgePartialGeom.ConnectedPiecesOfCurve == null)
        //                    edgePartialGeom.ConnectedPiecesOfCurve = new List<ICurve>();
        //                edgePartialGeom.ConnectedPiecesOfCurve.Add((ICurve) geom);
        //            }
        //        }


        internal int GetLevelIndexByScale(double scale) {
            if (scale <= 1) return 0;
            if (scale >= _levels.Last().ZoomLevel) return _levels.Count - 1;
            var z = Math.Log(scale, 2);
            //if (z >= _levels.Last().ZoomLevel) return _levels.Count - 1;
            int ret = (int)Math.Ceiling(z);
            Debug.Assert(0 <= ret && ret < _levels.Count);
            return ret;
        }



        /*
                void RemoveRailsOfPassingEdgesFromLowerLevelsAndReduceHighlights(List<Edge> passingEdges) {
                    var dimmedRails = new Set<Rail>();
                    foreach (var edge in passingEdges) {
                        for (int i = levels.Count - 1; i >= 0; i--) {
                            var level = levels[i];                    
                            DiminishHighlightAndCollectDimmedRailOnLevel(edge, level, dimmedRails);
                        }
                    }
      
                    RemoveDimmedRailsFromLowerLevels(dimmedRails);
                    foreach (var edge in passingEdges) {
                        var ei = GeometryEdgesToLgEdgeInfos[edge];
                        for (int i = 0; i<levels.Count; i++) {
                            var level = levels[i];
                            if (ei.ZoomLevel > level.ZoomLevel) {
                                Set<Rail> railsOfEdgeOnLevel;
                                if (level._railsOfEdges.TryGetValue(edge, out railsOfEdgeOnLevel))
                                    if (railsOfEdgeOnLevel.Any(dimmedRails.Contains))
                                        level._railsOfEdges.Remove(edge);
                            }
                            else break;
                        }
                    }
                }
        */
#if DEBUG && !SILVERLIGHT && !SHARPKIT
        static void ShowDimmedRails(Set<Rail> dimmedRails) {
            var l = new List<DebugHelpers.DebugCurve>();
            foreach (var r in dimmedRails) {
                var s = r.ToString();
                string color =
                    s.StartsWith("15") ? "green" : (s.StartsWith("1384") ? "red" : "blue");
                 l.Add(new DebugHelpers.DebugCurve(100, 1, color, r.Geometry as ICurve));
                
            }
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        }
#endif
        //        void RemoveDimmedRailsFromLowerLevels(Set<Rail> dimmedRails) {
//            foreach(var rail in dimmedRails)
//                RemoveDimmedRailFromLowerLevels(rail);
//        }
//
//        void RemoveDimmedRailFromLowerLevels(Rail rail) {
//            Debug.Assert(rail.IsHighlighted==0);
//            for (int i = 0; i < levels.Count; i++) {
//                var level = levels[i];
//                if (level.ZoomLevel < rail.ZoomLevel) {
//                    //the rail does not belong to this level
//                    var railTuple = rail.PointTuple();
//                    var rect = new Rectangle(railTuple.Item1, railTuple.Item2);
//                    level._railTree.Remove(rect, rail);                    
//                }
//                else break;
//            }
//
//        }

//        static void DiminishHighlightAndCollectDimmedRailOnLevel(Edge edge, Level level, Set<Rail> dimmedRails) {
//            Set<Rail> railsOfEdge;
//            if (!level._railsOfEdges.TryGetValue(edge, out railsOfEdge)) return;
//            foreach (var rail in railsOfEdge) {
//                if (rail.IsHighlighted == 0) continue;
//                rail.IsHighlighted--;
//                if (rail.IsHighlighted == 0)
//                    dimmedRails.Insert(rail);
//            }
//        }



        internal void SelectEdges(List<Edge> passingEdges) {
            SelectedEdges.InsertRange(passingEdges);
            for (int i = _levels.Count - 1; i >= 0; i--)
                SelectEdgesOnLevel(i, passingEdges);
             
        }
        internal void SelectEdges(List<Edge> passingEdges, int currentLayer)
        {
            SelectedEdges.InsertRange(passingEdges);
 
            foreach (Edge edge in passingEdges)
            {
                for (int i = 0; i <= currentLayer; i++)
                {
                    Set<Rail> railsOfEdge;
                    if (_levels[i]._railsOfEdges.TryGetValue(edge, out railsOfEdge))
                    {
                        var passingEdge = new List<Edge>();
                        passingEdge.Add(edge);
                        SelectEdgesOnLevel(i, passingEdge);
                    }
                }
            }
        }
        public Set<Edge> SelectedEdges {
            get { return _selectedEdges; } }

        internal Set<LgNodeInfo> SelectedNodeInfos {
            get { return _selectedNodeInfos; }
            private set { _selectedNodeInfos = value; }
        }

        void SelectEdgesOnLevel(int i, List<Edge> edges) {
            var level = _levels[i];
            var railsToHighlight = new Set<Rail>();
            foreach (var edge in edges) {
                Set<Rail> railsOfEdge;                
                //Added the following two lines and commented out the third line
                level._railsOfEdges.TryGetValue(edge, out railsOfEdge);

                // if (railsOfEdge== null ||railsOfEdge.Count ==0)                
                {
                    var edgeInfo = GeometryEdgesToLgEdgeInfos[edge];
                    int edgeLevelIndex = (int) Math.Min(Math.Log(edgeInfo.ZoomLevel, 2), _levels.Count - 1);
                    edgeLevelIndex =  _levels.Count - 1 ; 
                    

                    //if (railsOfEdge == null)
                    {
                        if (_levels[edgeLevelIndex]._railsOfEdges.ContainsKey(edge) == false) continue; //jyoti is it a bug?
                        railsOfEdge = _levels[edgeLevelIndex]._railsOfEdges[edge];
                        
                        TransferHighlightedRails(level, edge, railsOfEdge);
                        railsToHighlight.InsertRange(railsOfEdge);
                    }
                     
                }
                //else                
                    // railsToHighlight.InsertRange(railsOfEdge);

                foreach (Rail r in railsOfEdge)
                {
                    if(r.Color==null) r.Color = new List<SolidColorBrush>();
                    if (r.Color.Contains(edge.Color) == false && edge.Color!=null ) r.Color.Add(edge.Color);
                    r.IsHighlighted = true;
                }
            }

            //need to analyze and highlight the new highlighted rails
            foreach (var rail in railsToHighlight) {
                if (!level.HighlightedRails.Contains(rail)) {
                    rail.IsHighlighted = true;
                    level.HighlightedRails.Insert(rail);
                }
            }
            
            //DEBUG comment out by jyoti
            //var l = level.HighlightedRails.Select(r => new DebugCurve((ICurve) r.Geometry));
            //LayoutAlgorithmSettings.ShowDebugCurves(l.ToArray());

        }

        /// <summary>
        /// gets all rails corresponding to edges on given level.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="edges"></param>
        /// <returns></returns>
        public Set<Rail> GetRailsOfEdgesOnLevel(int i, List<Edge> edges)
        {
            var level = _levels[i];
            var railsToHighlight = new Set<Rail>();
            foreach (var edge in edges)
            {
                Set<Rail> railsOfEdge;
                if (!level._railsOfEdges.TryGetValue(edge, out railsOfEdge))
                {
                    var edgeInfo = GeometryEdgesToLgEdgeInfos[edge];
                    int edgeLevelIndex = (int)Math.Min(Math.Log(edgeInfo.ZoomLevel, 2), _levels.Count - 1);
                    railsOfEdge = _levels[edgeLevelIndex]._railsOfEdges[edge];
                    TransferHighlightedRails(level, edge, railsOfEdge); // roman: todo: necessary?
                }
                else
                    railsToHighlight.InsertRange(railsOfEdge);
            }
            return railsToHighlight;
        }

        public Set<Rail> GetRailsOfEdgeOnLevel(int i, Edge edge)
        {
            var level = _levels[i];
            var railsToHighlight = new Set<Rail>();

            Set<Rail> railsOfEdge;
            if (level._railsOfEdges.TryGetValue(edge, out railsOfEdge))
            {
                railsToHighlight.InsertRange(railsOfEdge);
            }
            //else
            //{
            //    var edgeInfo = GeometryEdgesToLgEdgeInfos[edge];
            //    int edgeLevelIndex = (int)Math.Min(Math.Log(edgeInfo.ZoomLevel, 2), levels.Count - 1);
            //    railsOfEdge = levels[edgeLevelIndex]._railsOfEdges[edge];
            //    TransferHighlightedRails(level, edge, railsOfEdge); // roman: todo: necessary?
            //}

            return railsToHighlight;
        }

        static void TransferHighlightedRails(LgLevel level, Edge edge, Set<Rail> railsOfEdge) {
            //need to remove those rails later, when putting them off
            foreach (var rail in railsOfEdge)
            {
                /*
                if (rail.A.X <= 4154 && rail.A.X >= 4152)
                {
                    Console.WriteLine("NOW I SEE YOU");
                }*/
                AddRailToRailTreeOfLowerLevel(rail, level);
            }
            level._railsOfEdges[edge] = railsOfEdge;
            level.HighlightedRails.InsertRange(railsOfEdge);
        }

/*
        void HiglightEdge(Edge edge) {
            Set<Rail> railsOfEdge = null;
            //adding highlight for edge rails in each level
            for (int i = levels.Count - 1; i >= 0; i--) {                
                var level = levels[i];

                Set<Rail> railsOfEdgeOfLevelWithHigherZoom;
                if (level._railsOfEdges.TryGetValue(edge, out railsOfEdgeOfLevelWithHigherZoom)) {
                    railsOfEdge = railsOfEdgeOfLevelWithHigherZoom;
                }
                else {
                    //the edge is not represented at the level, so we are reusing the rails from the level above
                    level._railsOfEdges[edge] = railsOfEdge;
                    foreach (var rail1 in railsOfEdge)
                        AddRailToRailTreeOfLowerLevel(rail1, level);
                }
                foreach (var rail1 in railsOfEdge) //highlight every rail: does not matter new or old
                    rail1.HiglightCount ++;
            }
        }
*/

        static void AddRailToRailTreeOfLowerLevel(Rail rail, LgLevel lowerLevel) {
            var pt = rail.PointTuple();
            var box = new Rectangle(pt.Item1, pt.Item2);
            if (!lowerLevel._railTree.Contains(box, rail))
                lowerLevel._railTree.Add(box, rail);
        }


        internal double GetMaximalZoomLevel() {
            if (_levels == null || _levels.Count == 0) return 1;
            return _levels.Last().ZoomLevel;
        }

        internal void PutOffEdgesPassingThroughTheRail(Rail rail) {
            var railLevel = _levels[(int)Math.Log(rail.ZoomLevel, 2)];
            var passingEdges = railLevel.GetEdgesPassingThroughRail(rail);
            UnselectEdges(passingEdges);
        }


        public void UnselectColoredEdges(List<Edge> edgesToPutOff, SolidColorBrush c)
        {
            
            //do not put off all the edges, otherwise it will cause disconnected components in
            //multiple selection
            Set<Rail> railsOfEdge = new Set<Rail>();
            Dictionary<Edge, Boolean> removeEdge = new Dictionary<Edge, Boolean>();
            foreach (Edge e in edgesToPutOff)
            {
                for (int i = _levels.Count - 1; i >= 0; i--)//int i = Levels.Count - 1;
                {
                    if (_levels[i]._railsOfEdges.ContainsKey(e) == false) continue; //is it a bug?
                    railsOfEdge = _levels[i]._railsOfEdges[e];
                    foreach (var r in railsOfEdge)
                    {
                        if (r.Color == null) continue;

                        if (r.Color.Count >= 1)
                            if (!removeEdge.ContainsKey(e)) removeEdge[e] = true;
                        r.Color.Remove(c);
                    }
                }
                if (!removeEdge.ContainsKey(e)) e.Color = null;
            }
            
            //foreach (Edge e in removeEdge.Keys)
                //edgesToPutOff.Remove(e);
           

            var edgesToPutoffSet = new Set<Edge>(edgesToPutOff);
            for (int i = _levels.Count - 1; i >= 0; i--)
                PutOffEdgesOnLevel(i, edgesToPutoffSet);
            
            foreach (var e in edgesToPutOff)
                SelectedEdges.Remove(e);
        }

        public void UnselectEdges(List<Edge> edgesToPutOff )
        {
            Set<Rail> railsOfEdge = new Set<Rail>();
            Dictionary<Edge,Boolean> removeEdge = new Dictionary<Edge,Boolean>();
            foreach (Edge e in edgesToPutOff)
            {
                //for (int i = _levels.Count - 1; i >= 0; i--)
                int i = Levels.Count-1;
                {
                    //if (_levels[i]._railsOfEdges.ContainsKey(e) == false) continue;
                    railsOfEdge = _levels[i]._railsOfEdges[e];
                    foreach (var r in railsOfEdge)
                    {
                        if (r.Color == null || r.Color.Count==0)continue;
                         

                        if(r.Color.Count>1) 
                            if(!removeEdge.ContainsKey(e)) removeEdge[e] = true;
                        r.Color.Remove(e.Color);
                    }
                }
                if (!removeEdge.ContainsKey(e)) e.Color = null;
            }
            foreach (Edge e in removeEdge.Keys)
                edgesToPutOff.Remove(e);

            var edgesToPutoffSet = new Set<Edge>(edgesToPutOff);
            for (int i = _levels.Count - 1; i >= 0; i--)
                PutOffEdgesOnLevel(i, edgesToPutoffSet);
            /*
            foreach (Edge edge in edgesToPutOff)
            {
                for (int i = 0; i <= _levels.Count - 1; i++){
                    Set<Rail> railsOfEdge;
                    if (_levels[i]._railsOfEdges.TryGetValue(edge, out railsOfEdge))
                    {
                        var PutoffSet = new List<Edge>();
                        PutoffSet.Add(edge);
                        PutOffEdgesOnLevel(i, new Set<Edge>(PutoffSet));
                        break;
                    }
                }
            }
             */
            foreach (var e in edgesToPutOff)
                SelectedEdges.Remove(e);
        }

        void PutOffEdgesOnLevel(int i, Set<Edge> edgesToPutOff) {
            var level = _levels[i];
            var railsThatShouldBeHiglighted = GetRailsThatShouldBeHighlighted(level, edgesToPutOff);

            foreach (var rail in level.HighlightedRails) {
                if (!railsThatShouldBeHiglighted.Contains(rail)) {
                    if (RailBelongsToLevel(level, rail))
                        if (rail.Color != null && rail.Color.Count > 0) { }
                        else rail.IsHighlighted = false;
                    else
                    {
                        
                        var railTuple = rail.PointTuple();  
                        var rect = new Rectangle(railTuple.Item1, railTuple.Item2);
                        /*
                        if (rail.A.X <= 4154 && rail.A.X >= 4152)
                        {
                            Console.WriteLine("NOW I SEE YOU = " + level._railTree.Contains(rect, rail));
                        }*/
                        if (level._railTree.Contains(rect, rail) == false)
                        {                            
                            rect = new Rectangle(rail.A, rail.B);
                            level._railTree.Remove(rect, rail);
                        }
                        else level._railTree.Remove(rect, rail);
                    }
                        
                }
            }
            foreach (var edge in edgesToPutOff) {
                var lgEdgeInfo = GeometryEdgesToLgEdgeInfos[edge];
                if (lgEdgeInfo.ZoomLevel > level.ZoomLevel)
                    level._railsOfEdges.Remove(edge);
            }

            level.HighlightedRails = railsThatShouldBeHiglighted;
        }

        static bool RailBelongsToLevel(LgLevel level, Rail rail) {
            return level.ZoomLevel >= rail.ZoomLevel;
        }

        Set<Rail> GetRailsThatShouldBeHighlighted(LgLevel level, Set<Edge> edgesToPutOff) {
            var ret = new Set<Rail>();
            foreach (var edge in SelectedEdges) {
                if (edgesToPutOff.Contains(edge)) continue;
                Set<Rail> railsOfEdge;
                if (level._railsOfEdges.TryGetValue(edge, out railsOfEdge)) {
                    ret.InsertRange(railsOfEdge);
                }
            }
            return ret;
        }


        public void AssembleEdgeAtLevel(LgEdgeInfo lgEi, int iLevel, Set<Rail> rails) {
            var edge = lgEi.Edge;

            var level = Levels[iLevel];

            level._railsOfEdges[edge] = rails;
            foreach (Rail rail in rails) {
                level.AddRail(rail);
                rail.UpdateTopEdgeInfo(lgEi);
            }
        }

        public void PutOffAllEdges()
        {
            UnselectEdges(SelectedEdges.ToList());
        }

        public void CreateLevelNodeTrees(double nodeDotWidth) {
            for (int i = 0; i < _levels.Count; i++) {
                _levels[i].CreateNodeTree(SortedLgNodeInfos.Take(this.LevelNodeCounts[i]), nodeDotWidth);
                nodeDotWidth /= 2;
            }
        }


        public bool NodeTreeIsCorrectOnLevel(int iLevel) {
            return _levels[iLevel].NodeInfoTree.Count == LevelNodeCounts[iLevel];
        }
    }
}