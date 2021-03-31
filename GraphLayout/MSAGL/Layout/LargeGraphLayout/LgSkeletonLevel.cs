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
        //internal RTree<Rail,Point> RailTree = new RTree<Rail,Point>();

        //internal Dictionary<SymmetricSegment, Rail> RailDictionary =
        //    new Dictionary<SymmetricSegment, Rail>();

        readonly RTree<Point,Point> _visGraphVertices = new RTree<Point,Point>();

        internal int ZoomLevel;

        //VisibilityGraph VisGraph = new VisibilityGraph();
        internal LgPathRouter PathRouter = new LgPathRouter();

        readonly Dictionary<SymmetricTuple<LgNodeInfo>, List<Point>> _edgeTrajectories =
            new Dictionary<SymmetricTuple<LgNodeInfo>, List<Point>>();

        internal Dictionary<SymmetricTuple<LgNodeInfo>, List<Point>> EdgeTrajectories {
            get { return _edgeTrajectories; }
        }


        internal void AddGraphEdgesFromCentersToPointsOnBorders(IEnumerable<LgNodeInfo> nodeInfos) {
            foreach (var nodeInfo in nodeInfos)
                PathRouter.AddVisGraphEdgesFromNodeCenterToNodeBorder(nodeInfo);
        }


        internal void Clear() {
            _visGraphVertices.Clear();
            PathRouter = new LgPathRouter();
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
#if TEST_MSAGL && !SHARPKIT
            var routesOutOfVisGraph = routesDump - visEdgeDump;
            if (routesOutOfVisGraph.Count > 0) {
                SplineRouter.ShowVisGraph(PathRouter.VisGraph, null,null, Ttt(routesOutOfVisGraph));
            }

#endif
            return routesDump == visEdgeDump && usedEdges==routesDump;
        }

#if TEST_MSAGL
        IEnumerable<ICurve> Ttt(Set<SymmetricSegment> routesOutOfVisGraph) {
            foreach (var symmetricTuple in routesOutOfVisGraph) {
                yield return new LineSegment(symmetricTuple.A,symmetricTuple.B);
            }
        }
#endif

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
