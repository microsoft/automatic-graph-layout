using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.OverlapRemovalFixedSegments;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Visibility;
using SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;
namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    internal class LgSkeletonLevel {
        //internal RTree<Rail> RailTree = new RTree<Rail>();

        //internal Dictionary<SymmetricSegment, Rail> RailDictionary =
        //    new Dictionary<SymmetricSegment, Rail>();

        internal readonly Dictionary<Point, List<Point>> NodePorts = new Dictionary<Point, List<Point>>();

        internal Dictionary<LgNodeInfo, List<SymmetricSegment>> NodePortEdges =
            new Dictionary<LgNodeInfo, List<SymmetricSegment>>();

        internal readonly Dictionary<LgNodeInfo, Point> NodeCenters = new Dictionary<LgNodeInfo, Point>();

        readonly RTree<Point> _visGraphVertices = new RTree<Point>();

        internal int ZoomLevel;

        //VisibilityGraph VisGraph = new VisibilityGraph();
        internal LgPathRouter PathRouter = new LgPathRouter();

        readonly Dictionary<SymmetricTuple<LgNodeInfo>, List<Point>> _edgeTrajectories =
            new Dictionary<SymmetricTuple<LgNodeInfo>, List<Point>>();

        internal Dictionary<SymmetricTuple<LgNodeInfo>, List<Point>> EdgeTrajectories {
            get { return _edgeTrajectories; }
        }


        internal void SetNodePortEdges(Dictionary<LgNodeInfo, List<SymmetricSegment>> nodePortEdges) {
            NodePortEdges = nodePortEdges;
        }

        internal void InitNodeCenters(IEnumerable<LgNodeInfo> nodes) {
            foreach (var node in nodes) {
                NodeCenters[node] = node.BoundingBox.Center;
            }
        }

        internal List<SymmetricSegment> GetNodePortSegments() {
            var segs = new List<SymmetricSegment>();
            foreach (var node in NodeCenters.Keys) {
                var pts = PathRouter.GetPortVertices(node);
                var center = node.BoundingBox.Center;
                segs.AddRange(pts.Select(pt => new SymmetricSegment(center, pt)));
            }
            return segs;
        }

        internal List<VisibilityEdge> GetAllGraphEdgesWithEndpointInInteriorOf(IEnumerable<Rectangle> rects,
            double slack = 0.01) {
            var rtree = new RTree<Rectangle>();
            foreach (var rect in rects) {
                var shrinkedRect = rect.Clone();
                shrinkedRect.ScaleAroundCenter(1 - slack);
                rtree.Add(shrinkedRect, shrinkedRect);
            }

            var edges = (from edge in PathRouter.GetAllEdgesVisibilityEdges()
                let qrect1 = new Rectangle(edge.SourcePoint, edge.SourcePoint)
                let qrect2 = new Rectangle(edge.TargetPoint, edge.TargetPoint)
                where rtree.GetAllIntersecting(qrect1).Any() || rtree.GetAllIntersecting(qrect2).Any()
                select edge).ToList();

            return edges;
        }

        internal List<SymmetricSegment> GetAllGraphEdgeSegments() {
            var segs =
                PathRouter.GetAllEdgesVisibilityEdges()
                    .Select(edge => new SymmetricSegment(edge.SourcePoint, edge.TargetPoint))
                    .ToList();
            return segs;
        }



        internal void AddGraphEdgesFromCentersToPointsOnBorders(IEnumerable<LgNodeInfo> nodeInfos) {
            foreach (var nodeInfo in nodeInfos)
                PathRouter.AddVisGraphEdgesFromNodeCenterToNodeBorder(nodeInfo);
        }


        internal void InitNodePortEdges(IEnumerable<LgNodeInfo> nodes, IEnumerable<SymmetricSegment> segments) {
            NodePortEdges.Clear();
            NodeCenters.Clear();

            RTree<SymmetricSegment> rtree = new RTree<SymmetricSegment>();
            RTree<Point> pointRtree = new RTree<Point>();

            foreach (var seg in segments) {
                rtree.Add(new Rectangle(seg.A, seg.B), seg);

                pointRtree.Add(new Rectangle(seg.A), seg.A);
                pointRtree.Add(new Rectangle(seg.B), seg.B);
            }

            foreach (var node in nodes) {
                var bbox = node.BoundingBox.Clone();
                bbox.ScaleAroundCenter(0.9);
                NodePortEdges[node] = new List<SymmetricSegment>();
                var segInt = rtree.GetAllIntersecting(bbox).ToList();
                foreach (var seg in segInt) {
                    if (RectSegIntersection.Intersect(bbox, seg.A, seg.B)) {
                        NodePortEdges[node].Add(seg);
                    }
                    if (!(node.BoundingBox.Contains(seg.A) && node.BoundingBox.Contains(seg.B)))
                        Debug.Assert(false, "found long edge");
                }

                bbox = node.BoundingBox.Clone();
                bbox.ScaleAroundCenter(0.01);

                Point x;
                if (pointRtree.OneIntersecting(bbox, out x))
                    NodeCenters[node] = x;
            }
        }

        internal void Clear() {
            _visGraphVertices.Clear();
            PathRouter = new LgPathRouter();
        }

        internal static List<Point> GetSubdividedBox(Rectangle rect, int nx, int ny) {
            var pts = new List<Point>();
            var dx = (rect.RightBottom - rect.LeftBottom)*1.0/nx;
            var dy = (rect.RightTop - rect.RightBottom)*1.0/ny;
            Point p = rect.LeftBottom;
            //pts.Add(p);
            for (int ix = 0; ix < nx; ix++) {
                pts.Add(p);
                p = p + dx;
            }
            for (int iy = 0; iy < ny; iy++) {
                pts.Add(p);
                p = p + dy;
            }
            for (int ix = 0; ix < nx; ix++) {
                pts.Add(p);
                p = p - dx;
            }
            for (int iy = 0; iy < ny; iy++) {
                pts.Add(p);
                p = p - dy;
            }
            return pts;
        }


        internal void SetTrajectoryAndAddEdgesToUsed(LgNodeInfo s, LgNodeInfo t, List<Point> path) {
            var t1 = new SymmetricTuple<LgNodeInfo>(s, t);
            if (_edgeTrajectories.ContainsKey(t1)) return;
            _edgeTrajectories[t1] = path;
            PathRouter.MarkEdgesUsedAlongPath(path);
        }


        internal bool HasSavedTrajectory(LgNodeInfo s, LgNodeInfo t) {
            var t1 = new SymmetricTuple<LgNodeInfo>(s, t);
            return EdgeTrajectories.ContainsKey(t1);
        }

        internal List<Point> GetTrajectory(LgNodeInfo s, LgNodeInfo t) {
            List<Point> path;
            var tuple = new SymmetricTuple<LgNodeInfo>(s, t);
            EdgeTrajectories.TryGetValue(tuple, out path);
            return path;
        }


        internal void ClearSavedTrajectoriesAndUsedEdges() {
            _edgeTrajectories.Clear();
            PathRouter.ClearUsedEdges();
        }

        internal Set<Point> GetPointsOnSavedTrajectories() {
            var points = new Set<Point>();
            foreach (var edgeTrajectory in _edgeTrajectories.Values) {
                points.InsertRange(edgeTrajectory);
            }
            return points;
        }

        internal void AddGraphEdges(List<SymmetricSegment> segments) {
            PathRouter.AddEdges(segments);
        }

        internal void RemoveUnusedGraphEdgesAndNodes() {
            List<VisibilityEdge> unusedEdges = GetUnusedGraphEdges();
            PathRouter.RemoveVisibilityEdges(unusedEdges);
        }

        internal List<VisibilityEdge> GetUnusedGraphEdges() {
            return PathRouter.GetAllEdgesVisibilityEdges().Where(e => !PathRouter.IsEdgeUsed(e)).ToList();
        }

        IEnumerable<SymmetricSegment> SymSegsOfPointList(List<Point> ps) {
            for (int i = 0; i < ps.Count - 1; i++)
                yield return new SymmetricSegment(ps[i], ps[i + 1]);
        }

        internal bool RoutesAreConsistent() {
            var usedEdges=new Set<SymmetricSegment>(PathRouter.UsedEdges());
            var routesDump =
                new Set<SymmetricSegment>(_edgeTrajectories.Select(p => p.Value).SelectMany(SymSegsOfPointList));
            var visEdgeDump =
                new Set<SymmetricSegment>(
                    PathRouter.VisGraph.Edges.Select(e => new SymmetricSegment(e.SourcePoint, e.TargetPoint)));
#if DEBUG
            foreach (var s in routesDump - visEdgeDump)
                Console.WriteLine("{0} is in routes but no in vis graph", s);
            foreach (var s in visEdgeDump - routesDump)
                Console.WriteLine("{0} is in visgraph but no in routes", s);
            var routesOutOfVisGraph = routesDump - visEdgeDump;
            if (routesOutOfVisGraph.Count > 0) {
                SplineRouter.ShowVisGraph(PathRouter.VisGraph, null,null, Ttt(routesOutOfVisGraph));
            }
            foreach (var s in usedEdges - routesDump)
                Console.WriteLine("{0} is in usedEdges but not in routes", s);

            foreach (var s in routesDump - usedEdges)
                Console.WriteLine("{0} is in routes but not in usedEdges", s);

#endif
            return routesDump == visEdgeDump && usedEdges==routesDump;
        }

        IEnumerable<ICurve> Ttt(Set<SymmetricSegment> routesOutOfVisGraph) {
            foreach (var symmetricTuple in routesOutOfVisGraph) {
                yield return new LineSegment(symmetricTuple.A,symmetricTuple.B);
            }
        }

        internal void RemoveSomeEdgeTrajectories(List<SymmetricTuple<LgNodeInfo>> removeList) {
            foreach (var symmetricTuple in removeList)
                RemoveEdgeTrajectory(symmetricTuple);
            RemoveUnusedGraphEdgesAndNodes();
        }

        void RemoveEdgeTrajectory(SymmetricTuple<LgNodeInfo> symmetricTuple) {
            List<Point> trajectory;
            if (_edgeTrajectories.TryGetValue(symmetricTuple, out trajectory)) {
                for (int i = 0; i < trajectory.Count - 1; i++) {
                    DiminishUsed(trajectory[i], trajectory[i + 1]);
                }
                _edgeTrajectories.Remove(symmetricTuple);
            }
        }

        void DiminishUsed(Point a, Point b) {
            PathRouter.DiminishUsed(a, b);
        }

        public void MarkEdgesAlongPathAsEdgesOnOldTrajectories(List<Point> trajectory)
        {
            PathRouter.MarkEdgesAlongPathAsEdgesOnOldTrajectories(trajectory);
        }
    }
}
