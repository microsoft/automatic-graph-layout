using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// Wrapper for geometry graph with coinciding edges:
    ///  'real' nodes stand for edge ends (source,target)
    ///  'virtual' nodes stand for polyline control points
    ///  
    ///  'real' edges are original graph edges
    ///  'virtual' edges are polyline segments
    /// </summary>
    internal class MetroGraphData {
        internal Set<Station> Stations;

        /// info on the edges passing through a couple
        Dictionary<Tuple<Station, Station>, StationEdgeInfo> edgeInfoDictionary;

        /// current ink
        double ink;

        /// Edges
        List<Metroline> metrolines;

        ///  position -> (node)
        internal Dictionary<Point, Station> PointToStations;

        readonly EdgeGeometry[] regularEdges;

        ///  objects to check crossings and calculate distances
        internal Intersections looseIntersections;
        internal Intersections tightIntersections;

        ///  objects to check crossings and calculate distances
        internal CdtIntersections cdtIntersections;

        Dictionary<EdgeGeometry, Set<Polyline>> EdgeLooseEnterable { get; set; }
        Dictionary<EdgeGeometry, Set<Polyline>> EdgeTightEnterable { get; set; }

        internal Func<Port, Polyline> LoosePolylineOfPort;

        /// <summary>
        /// triangulation
        /// </summary>
        internal Cdt Cdt;

        internal MetroGraphData(EdgeGeometry[] regularEdges,
            RectangleNode<Polyline, Point> looseTree, RectangleNode<Polyline, Point> tightTree,
            BundlingSettings bundlingSettings, Cdt cdt,
            Dictionary<EdgeGeometry, Set<Polyline>> edgeLooseEnterable, Dictionary<EdgeGeometry, Set<Polyline>> edgeTightEnterable, Func<Port, Polyline> loosePolylineOfPort) {
            //Debug.Assert(cdt != null);
            this.regularEdges = regularEdges;
            if (cdt != null)
                Cdt = cdt;
            else
                Cdt = BundleRouter.CreateConstrainedDelaunayTriangulation(looseTree);

            EdgeLooseEnterable = edgeLooseEnterable;
            EdgeTightEnterable = edgeTightEnterable;
            LoosePolylineOfPort = loosePolylineOfPort;

            looseIntersections = new Intersections(this, bundlingSettings, looseTree, station => station.EnterableLoosePolylines);
            tightIntersections = new Intersections(this, bundlingSettings, tightTree, station => station.EnterableTightPolylines);
            cdtIntersections = new CdtIntersections(this, bundlingSettings);

            Initialize(false);
        }

        internal double Ink {
            get { return ink; }
        }

        internal EdgeGeometry[] Edges {
            get { return regularEdges; }
        }

        internal IEnumerable<Station> VirtualNodes() {
            return Stations.Where(s => !s.IsRealNode);
        }

        internal List<Metroline> Metrolines { get { return metrolines; } }

        internal RectangleNode<Polyline, Point> LooseTree { get { return looseIntersections.obstacleTree; } }

        internal RectangleNode<Polyline, Point> TightTree { get { return tightIntersections.obstacleTree; } }

        internal IEnumerable<Tuple<Station, Station>> VirtualEdges() {
            return edgeInfoDictionary.Keys;
        }

        /// <summary>
        /// number of real edges passing the edge uv
        /// </summary>
        internal int RealEdgeCount(Station u, Station v) {
            var couple = u < v ? new Tuple<Station, Station>(u, v) : new Tuple<Station, Station>(v, u);
            StationEdgeInfo cw;
            if (edgeInfoDictionary.TryGetValue(couple, out cw))
                return cw.Count;
            return 0;
        }

        /// <summary>
        /// real edges passing the node
        /// </summary>
        internal List<MetroNodeInfo> MetroNodeInfosOfNode(Station node) {
            return node.MetroNodeInfos;
        }

        /// <summary>
        /// real edges passing the edge uv
        /// </summary>
        internal StationEdgeInfo GetIjInfo(Station u, Station v) {
            var couple = u < v ? new Tuple<Station, Station>(u, v) : new Tuple<Station, Station>(v, u);
            return edgeInfoDictionary[couple];
        }

        /// <summary>
        /// Move node to the specified position
        /// </summary>
        internal void MoveNode(Station node, Point newPosition) {
            Point oldPosition = node.Position;
            PointToStations.Remove(oldPosition);
            PointToStations.Add(newPosition, node);
            node.Position = newPosition;

            //move curves
            foreach (MetroNodeInfo metroNodeInfo in MetroNodeInfosOfNode(node))
                metroNodeInfo.PolyPoint.Point = newPosition;

            //update lengths
            foreach (MetroNodeInfo e in MetroNodeInfosOfNode(node)) {
                var metroLine = e.Metroline;
                var prev = e.PolyPoint.Prev.Point;
                var succ = e.PolyPoint.Next.Point;
                metroLine.Length += (succ - newPosition).Length + (prev - newPosition).Length
                    - (succ - oldPosition).Length - (prev - oldPosition).Length;
            }

            //update ink
            foreach (var adj in node.Neighbors) {
                ink += (newPosition - adj.Position).Length - (oldPosition - adj.Position).Length;
            }

            //update neighbors order
            SortNeighbors(node);
            foreach (var adj in node.Neighbors)
                SortNeighbors(adj);            
        }

        internal double GetWidth(Station u, Station v, double edgeSeparation) {
            var couple = u < v ? new Tuple<Station, Station>(u, v) : new Tuple<Station, Station>(v, u);
            StationEdgeInfo cw;
            if (edgeInfoDictionary.TryGetValue(couple, out cw))
                return cw.Width + (cw.Count - 1) * edgeSeparation;
            return 0;
        }

        internal double GetWidth(IEnumerable<Metroline> metrolines, double edgeSeparation) {
            double width = 0;
            foreach (var metroline in metrolines) {
                width += metroline.Width;
            }
            int count = metrolines.Count();
            width += count > 0 ? (count - 1) * edgeSeparation : 0;
            Debug.Assert(ApproximateComparer.GreaterOrEqual(width, 0));
            return width;
        }

        /// <summary>
        /// Initialize data
        /// </summary>
        internal void Initialize(bool initTightTree) {
            //TimeMeasurer.DebugOutput("bundle graph data initializing...");

            SimplifyRegularEdges();

            InitializeNodeData();

            InitializeEdgeData();

            InitializeVirtualGraph();

            InitializeEdgeNodeInfo(initTightTree);

            InitializeCdtInfo();

//            Debug.Assert(looseIntersections.HubPositionsAreOK());
  //          Debug.Assert(tightIntersections.HubPositionsAreOK());
        
        }

        /// <summary>
        /// remove self-cycles
        /// </summary>
        void SimplifyRegularEdges() {
            foreach (var edge in regularEdges)
                SimplifyRegularEdge(edge);
        }

        /// <summary>
        /// change the polyline by removing cycles
        /// </summary>
        void SimplifyRegularEdge(EdgeGeometry edge) {
            Polyline polyline = (Polyline)edge.Curve;

            var stack = new Stack<Point>();
            var seen = new Set<Point>();
            for (var p = polyline.EndPoint; p != null; p = p.Prev) {
                var v = p.Point;
                if (seen.Contains(p.Point)) {
                    var pp = p.Next;
                    do {
                        var u = stack.Peek();
                        if (u != v) {
                            seen.Remove(u);
                            stack.Pop();
                            pp = pp.Next;
                        }
                        else
                            break;
                    } while (true);
                    pp.Prev = p.Prev;
                    pp.Prev.Next = pp;
                }
                else {
                    stack.Push(v);
                    seen.Insert(v);
                }
            }
        }

        void InitializeNodeData() {
            Stations = new Set<Station>();
            //create indexes
            PointToStations = new Dictionary<Point, Station>();
            int i = 0;
            foreach (var edge in regularEdges) {
                Polyline poly = (Polyline)edge.Curve;
                i = ProcessPolylinePoints(i, poly);
            }
        }

        int ProcessPolylinePoints(int i, Polyline poly) {
            var pp = poly.StartPoint;
            i = RegisterStation(i, pp, true);

            for (pp = pp.Next; pp != poly.EndPoint; pp = pp.Next)
                i = RegisterStation(i, pp, false);

            i = RegisterStation(i, pp, true);
            return i;
        }

        int RegisterStation(int i, PolylinePoint pp, bool isRealNode) {
            if (!PointToStations.ContainsKey(pp.Point)) {
                // Filippo Polo: assigning the return value of the assignment operator (i.e. a = b = c) does not work well in Sharpkit.
                Station station = new Station(i++, isRealNode, pp.Point);
                PointToStations[pp.Point] = station;
                Stations.Insert(station);
            }
            else {
#if TEST_MSAGL
                var s = PointToStations[pp.Point];
                Debug.Assert(s.IsRealNode == isRealNode);
#endif
            }
            return i;
        }

        void InitializeEdgeData() {
            metrolines = new List<Metroline>();
            for (int i = 0; i < regularEdges.Length; i++) {
                EdgeGeometry geomEdge=regularEdges[i];
                InitEdgeData(geomEdge, i);
            }
        }

        void InitEdgeData(EdgeGeometry geomEdge, int index) {
            var metroEdge = new Metroline((Polyline)geomEdge.Curve, geomEdge.LineWidth, EdgeSourceAndTargetFunc(geomEdge), index);
            metrolines.Add(metroEdge);
            PointToStations[metroEdge.Polyline.Start].BoundaryCurve = geomEdge.SourcePort.Curve;
            PointToStations[metroEdge.Polyline.End].BoundaryCurve = geomEdge.TargetPort.Curve;
        }

        internal Func<Tuple<Polyline, Polyline>> EdgeSourceAndTargetFunc(EdgeGeometry geomEdge) {
            return
                () =>
                new Tuple<Polyline, Polyline>(LoosePolylineOfPort(geomEdge.SourcePort),
                                               LoosePolylineOfPort(geomEdge.TargetPort));
        }

        /// <summary>
        /// Initialize graph comprised of stations and their neighbors
        /// </summary>
        void InitializeVirtualGraph() {
            Dictionary<Station, Set<Station>> neighbors = new Dictionary<Station, Set<Station>>();
            foreach (var metroline in metrolines) {
                Station u = PointToStations[metroline.Polyline.Start];
                Station v;
                for (var p = metroline.Polyline.StartPoint; p.Next != null; p = p.Next, u = v) {
                    v = PointToStations[p.Next.Point];
                    CollectionUtilities.AddToMap(neighbors, u, v);
                    CollectionUtilities.AddToMap(neighbors, v, u);
                }
            }

            foreach (var s in Stations) {
                s.Neighbors = neighbors[s].ToArray();
            }
        }

        StationEdgeInfo GetUnorderedIjInfo(Station i, Station j) {
            Debug.Assert(i != j);
            return (i < j ? GetOrderedIjInfo(i, j) : GetOrderedIjInfo(j, i));
        }

        StationEdgeInfo GetOrderedIjInfo(Station i, Station j) {
            Debug.Assert(i < j);
            var couple = new Tuple<Station, Station>(i, j);
            StationEdgeInfo cw;
            if (edgeInfoDictionary.TryGetValue(couple, out cw))
                return cw;
            edgeInfoDictionary[couple] = cw = new StationEdgeInfo(i.Position, j.Position);
            return cw;
        }

        void InitializeEdgeNodeInfo(bool initTightTree) {
            edgeInfoDictionary = new Dictionary<Tuple<Station, Station>, StationEdgeInfo>();

            InitMetroNodeInfos(initTightTree);
            SortNeighbors();
            InitEdgeIjInfos();
            ink = 0;
            foreach (var edge in VirtualEdges())
                ink += (edge.Item1.Position - edge.Item2.Position).Length;
        }

        void InitMetroNodeInfos(bool initTightTree) {
            for (int i = 0; i < metrolines.Count; i++) {
                var metroline = metrolines[i];
                InitMetroNodeInfos(metroline);
                InitNodeEnterableLoosePolylines(metroline, regularEdges[i]);
                if (initTightTree)
                    InitNodeEnterableTightPolylines(metroline, regularEdges[i]);
                metroline.UpdateLengths();
            }
        }

        void InitMetroNodeInfos(Metroline metroline) {
            for (var pp = metroline.Polyline.StartPoint; pp != null; pp = pp.Next) {
                Station station = PointToStations[pp.Point];
                station.MetroNodeInfos.Add(new MetroNodeInfo(metroline, station, pp));
            }
        }

        void InitNodeEnterableLoosePolylines(Metroline metroline, EdgeGeometry regularEdge) {
            //If we have groups, EdgeLooseEnterable are precomputed.
            var metrolineEnterable = EdgeLooseEnterable != null ? EdgeLooseEnterable[regularEdge] : new Set<Polyline>();

            for (var p = metroline.Polyline.StartPoint.Next; p!=null && p.Next != null; p = p.Next) {
                var v = PointToStations[p.Point];
                if (v.EnterableLoosePolylines != null)
                    v.EnterableLoosePolylines *= metrolineEnterable;
                else
                    v.EnterableLoosePolylines = new Set<Polyline>(metrolineEnterable);
            }

            AddLooseEnterableForMetrolineStartEndPoints(metroline);
        }

        void AddLooseEnterableForMetrolineStartEndPoints(Metroline metroline) {
            AddLooseEnterableForEnd(metroline.Polyline.Start);
            AddLooseEnterableForEnd(metroline.Polyline.End);
        }

        void AddTightEnterableForMetrolineStartEndPoints(Metroline metroline) {
            AddTightEnterableForEnd(metroline.Polyline.Start);
            AddTightEnterableForEnd(metroline.Polyline.End);
        }

        Dictionary<Point, Set<Polyline>> cachedEnterableLooseForEnd = new Dictionary<Point, Set<Polyline>>();

        void AddLooseEnterableForEnd(Point point) {
            Station station = PointToStations[point];
            if (!cachedEnterableLooseForEnd.ContainsKey(point)) {
                foreach (var poly in LooseTree.AllHitItems(point))
                    if (Curve.PointRelativeToCurveLocation(point, poly) == PointLocation.Inside) 
                        station.AddEnterableLoosePolyline(poly);
                    
                cachedEnterableLooseForEnd.Add(point, station.EnterableLoosePolylines);
            }
            else {
                station.EnterableLoosePolylines = cachedEnterableLooseForEnd[point];
            }

            //foreach (var poly in LooseTree.AllHitItems(point))
            //    if (Curve.PointRelativeToCurveLocation(point, poly) == PointLocation.Inside)
            //        station.EnterableLoosePolylines.Insert(poly);
        }

        void AddTightEnterableForEnd(Point point) {
            Station station = PointToStations[point];
            foreach (var poly in TightTree.AllHitItems(point))
                if (Curve.PointRelativeToCurveLocation(point, poly) == PointLocation.Inside)
                    station.AddEnterableTightPolyline(poly);
        }

        void InitNodeEnterableTightPolylines(Metroline metroline, EdgeGeometry regularEdge) {
            //If we have groups, EdgeTightEnterable are precomputed.
            var metrolineEnterable = EdgeTightEnterable != null ? EdgeTightEnterable[regularEdge] : new Set<Polyline>();

            for (var p = metroline.Polyline.StartPoint.Next; p!=null && p.Next != null; p = p.Next) {
                var v = PointToStations[p.Point];
                Set<Polyline> nodeEnterable = v.EnterableTightPolylines;
                if (nodeEnterable != null)
                    v.EnterableTightPolylines = nodeEnterable * metrolineEnterable;
                else
                    v.EnterableTightPolylines = new Set<Polyline>(metrolineEnterable);
            }

            AddTightEnterableForMetrolineStartEndPoints(metroline);
        }

        void SortNeighbors() {
            //counter-clockwise sorting
            foreach (var station in Stations)
                SortNeighbors(station);
        }

        void SortNeighbors(Station station) {
            //nothing to sort
            if (station.Neighbors.Length <= 2) return;

            Point pivot = station.Neighbors[0].Position;
            Point center = station.Position;
            Array.Sort(station.Neighbors, delegate(Station u, Station v) {
                return Point.GetOrientationOf3Vectors(pivot - center, u.Position - center, v.Position - center);
            });              
        }

        void InitEdgeIjInfos() {
            foreach (Metroline metroLine in metrolines) {
                var poly = metroLine.Polyline;
                var u = PointToStations[poly.Start];
                Station v;
                for (var p = metroLine.Polyline.StartPoint; p.Next != null; p = p.Next, u = v) {
                    v = PointToStations[p.Next.Point];
                    var info = GetUnorderedIjInfo(u, v);
                    info.Count++;
                    info.Width += metroLine.Width;
                    info.Metrolines.Add(metroLine);
                }
            }
       }

        void InitializeCdtInfo() {
            RectangleNode<CdtTriangle,Point> cdtTree = Cdt.GetCdtTree();
            foreach (var station in Stations) {
                station.CdtTriangle = cdtTree.FirstHitNode(station.Position, IntersectionCache.Test).UserData;
                Debug.Assert(station.CdtTriangle != null);
            }
        }

        internal bool PointIsAcceptableForEdge(Metroline metroline, Point point) {
            if (LoosePolylineOfPort == null)
                return true;
            var polys = metroline.sourceAndTargetLoosePolylines();
            return Curve.PointRelativeToCurveLocation(point, polys.Item1) == PointLocation.Outside &&
                   Curve.PointRelativeToCurveLocation(point, polys.Item2) == PointLocation.Outside;
        }
    }
}