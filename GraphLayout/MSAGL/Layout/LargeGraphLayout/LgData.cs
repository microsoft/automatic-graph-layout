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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        RTree<GeometryGraph> rTreeOfConnectedComps = new RTree<GeometryGraph>();
        internal Dictionary<Node, LgNodeInfo> GeometryNodesToLgNodeInfos;
        internal Dictionary<Edge, LgEdgeInfo> GeometryEdgesToLgEdgeInfos = new Dictionary<Edge, LgEdgeInfo>();

        readonly Set<Edge> _higlightedEdges=new Set<Edge>(); 
        
        List<LgLevel> levels = new List<LgLevel>();
        /// <summary>
        /// the list of levels
        /// </summary>
        public IList<LgLevel> Levels { get { return levels; } }

        internal LgData(GeometryGraph mainGeomGraph)
        {
            this.mainGeomGraph = mainGeomGraph;
        }

        /// <summary>
        /// two curves are equal here if their end point sets are the same
        /// </summary>
        internal class CurveRailComparer : IEqualityComparer<Tuple<Point, Point>> {
            public bool Equals(Tuple<Point, Point> x, Tuple<Point, Point> y) {
                return ApproximateComparer.Close(x.Item1, y.Item1, 0.00001) &&
                       ApproximateComparer.Close(x.Item2, y.Item2, 0.00001) ||
                       ApproximateComparer.Close(x.Item1, y.Item2, 0.00001) &&
                       ApproximateComparer.Close(x.Item2, y.Item1, 0.00001);
            }

            public int GetHashCode(Tuple<Point, Point> tuple) {
                return ApproximateComparer.Round(tuple.Item1, 5).GetHashCode() |
                       ApproximateComparer.Round(tuple.Item2, 5).GetHashCode();
            }
        }

        internal class ArrowheadRailComparer : IEqualityComparer<Arrowhead> {
            public bool Equals(Arrowhead x, Arrowhead y) {
                return ApproximateComparer.Close(x.TipPosition, y.TipPosition);
            }

            public int GetHashCode(Arrowhead arrowhead) {
                return ApproximateComparer.Round(arrowhead.TipPosition, 5).GetHashCode();
            }
        }

        #region Level class

        #endregion  end of Level class

        
        internal void AddConnectedGeomGraph(GeometryGraph geomGraph) {
            rTreeOfConnectedComps.Add(geomGraph.BoundingBox, geomGraph);
        }

        internal IEnumerable<GeometryGraph> ConnectedGeometryGraphs {
            get { return rTreeOfConnectedComps.GetAllLeaves(); }
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


        internal Set<Rail> GetSetOfVisibleRails(Rectangle visibleRectangle,
                                                double zoomLevel) {
            var visibleLevelIndex = GetRelevantEdgeLevel(zoomLevel);
            return  new Set<Rail>(levels[visibleLevelIndex].GetRailsIntersectionVisRect(visibleRectangle));
        }


        internal LgLevel AddLevel(int levelZoom) {
            var level = new LgLevel(levelZoom, mainGeomGraph);
            levels.Add(level);
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


        internal int GetRelevantEdgeLevel(double zoomLevel) {
            var logOfZoomLevel = Math.Log(zoomLevel, 2);
            if (logOfZoomLevel >= levels.Count) {
                return levels.Count - 1;
            }
            var doubleIndexOfLevel = logOfZoomLevel - 1;
            var floor = (int) Math.Floor(doubleIndexOfLevel);
            var ceiling = (int) Math.Ceiling(doubleIndexOfLevel);
            Debug.Assert(floor <= ceiling && floor <= doubleIndexOfLevel && doubleIndexOfLevel <= ceiling);

            if (ceiling <= 0)
                return 0;
            if (floor == ceiling)
                return floor;

            if (doubleIndexOfLevel < floor + 0.9)
                return floor;

            return ceiling;
            //                        else {
            //                            //showing tow levels
            //                            ret.Add(floor);
            //                            ret.Add(ceiling);
            //                        }
            //                    }
            //                return ret;
        }

        internal void HighlightEdgesPassingThroughRail(Rail rail) {
            var railLevel = levels[(int)Math.Log( rail.ZoomLevel, 2)];
            var passingEdges = railLevel.GetEdgesPassingThroughRail(rail);
            HighlightEdges(passingEdges);
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



        internal void HighlightEdges(List<Edge> passingEdges) {
            _higlightedEdges.InsertRange(passingEdges);
            for (int i = levels.Count - 1; i >= 0; i--)
                HighlightEdgesOnLevel(i, passingEdges);
        }

        void HighlightEdgesOnLevel(int i, List<Edge> edges) {
            var level = levels[i];
            var railsToHighlight = new Set<Rail>();
            foreach (var edge in edges) {
                Set<Rail> railsOfEdge;
                if (!level._railsOfEdges.TryGetValue(edge, out railsOfEdge)) {
                    var edgeInfo = GeometryEdgesToLgEdgeInfos[edge];
                    int edgeLevelIndex = (int)Math.Min( Math.Log(edgeInfo.ZoomLevel, 2), levels.Count - 1);
                    railsOfEdge = levels[edgeLevelIndex]._railsOfEdges[edge];
                    TransferHighlightedRails(level, edge, railsOfEdge);
                }
                else
                    railsToHighlight.InsertRange(railsOfEdge);
            }
            //need to analyze and highlight the new highlighted rails
            foreach (var rail in railsToHighlight) {
                if (!level.HighlightedRails.Contains(rail)) {
                    rail.IsHighlighted = true;
                    level.HighlightedRails.Insert(rail);
                }
            }

        }

        static void TransferHighlightedRails(LgLevel level, Edge edge, Set<Rail> railsOfEdge) {
            //need to remove those rails later, when putting them off
            foreach (var rail in railsOfEdge)
                AddRailToRailTreeOfLowerLevel(rail, level);
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
            if (levels == null || levels.Count == 0) return 1;
            return levels.Last().ZoomLevel;
        }

        internal void PutOffEdgesPassingThroughTheRail(Rail rail) {
            var railLevel = levels[(int)Math.Log(rail.ZoomLevel, 2)];
            var passingEdges = railLevel.GetEdgesPassingThroughRail(rail);
            PutOffEdges(passingEdges);
        }

        public void PutOffEdges(List<Edge> edgesToPutOff) {
            var edgesToPutoffSet = new Set<Edge>(edgesToPutOff);
            for (int i = levels.Count - 1; i >= 0; i--)
                PutOffEdgesOnLevel(i, edgesToPutoffSet);
            foreach (var e in edgesToPutOff)
                _higlightedEdges.Remove(e);
        }

        void PutOffEdgesOnLevel(int i, Set<Edge> edgesToPutOff) {
            var level = levels[i];
            var railsThatShouldBeHiglighted = GetRailsThatShouldBeHighlighted(level, edgesToPutOff);

            foreach (var rail in level.HighlightedRails) {
                if (!railsThatShouldBeHiglighted.Contains(rail)) {
                    if (RailBelongsToLevel(level, rail))
                        rail.IsHighlighted = false;
                    else {
                        var railTuple = rail.PointTuple();  
                        var rect = new Rectangle(railTuple.Item1, railTuple.Item2);
                        level._railTree.Remove(rect, rail);
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
            foreach (var edge in _higlightedEdges) {
                if (edgesToPutOff.Contains(edge)) continue;
                Set<Rail> railsOfEdge;
                if (level._railsOfEdges.TryGetValue(edge, out railsOfEdge)) {
                    ret.InsertRange(railsOfEdge);
                }
            }
            return ret;
        }
    }
}