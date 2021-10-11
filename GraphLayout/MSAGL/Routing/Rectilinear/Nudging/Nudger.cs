using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.ProjectionSolver;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging {
    /// <summary>
    /// following paper "Orthogonal Connector Routing" which is included in the project
    /// </summary>
#if TEST_MSAGL
    public
#else
        internal
#endif
    class Nudger {

        bool HasGroups {
            get { return (null != HierarchyOfGroups) && (HierarchyOfGroups.Count > 0); }
        }

        Dictionary<AxisEdge, Polyline> axisEdgesToObstaclesTheyOriginatedFrom;

        List<Path> Paths { get; set; }

        IEnumerable<Polyline> Obstacles { get; set; }
        internal VisibilityGraph PathVisibilityGraph { get; set; }

        /// <summary>
        ///  "nudge" paths to decrease the number of intersections and stores the results inside WidePaths of "paths"
        /// </summary>
        /// <param name="paths">paths through the graph</param>
        /// <param name="cornerFitRad">two parallel paths should be separated by this distance if it is feasible</param>
        /// <param name="obstacles">polygonal convex obstacles organized in a tree; the obstacles here are padded original obstacles</param>
        /// <param name="ancestorsSets"></param>
        /// <returns></returns>
        internal Nudger(IEnumerable<Path> paths, double cornerFitRad, IEnumerable<Polyline> obstacles, 
            Dictionary<Shape, Set<Shape>> ancestorsSets) {
            AncestorsSets = ancestorsSets;
            HierarchyOfGroups = RectangleNode<Shape, Point>.CreateRectangleNodeOnEnumeration(
                    ancestorsSets.Keys.Where(shape => shape.IsGroup).Select(group => new RectangleNode<Shape, Point>(group, group.BoundingBox)));
            Obstacles = obstacles;
            EdgeSeparation = 2 * cornerFitRad;
            Paths = new List<Path>(paths);
            HierarchyOfObstacles =
                RectangleNode<Polyline, Point>.CreateRectangleNodeOnEnumeration(
                    obstacles.Select(p => new RectangleNode<Polyline, Point>(p, p.BoundingBox)));
            MapPathsToTheirObstacles();
        }

        Dictionary<Shape, Set<Shape>> AncestorsSets { get; set; }

        void MapPathsToTheirObstacles() {
            PathToObstacles = new Dictionary<Path, Tuple<Polyline, Polyline>>();
            foreach (var path in Paths)
                MapPathToItsObstacles(path);
        }

        void MapPathToItsObstacles(Path path) {
            var startNode = HierarchyOfObstacles.FirstHitNode(path.PathPoints.First(),ObstacleTest);
            var endNode = HierarchyOfObstacles.FirstHitNode(path.PathPoints.Last(), ObstacleTest);
            if ((null != startNode) && (null != endNode)) {
                PathToObstacles[path] = new Tuple<Polyline, Polyline>(startNode.UserData, endNode.UserData);
            }
        }

        static HitTestBehavior ObstacleTest(Point pnt, Polyline polyline) {
            return (Curve.PointRelativeToCurveLocation(pnt, polyline) !=
                                                   PointLocation.Outside)
                                                      ? HitTestBehavior.Stop
                                                      : HitTestBehavior.Continue;
        }
        /// <summary>
        /// 
        /// </summary>
        protected RectangleNode<Polyline, Point> HierarchyOfObstacles { get; set; }
        /// <summary>
        /// 
        /// </summary>
        protected RectangleNode<Shape, Point> HierarchyOfGroups { get; set; }

        internal void Calculate(Direction direction, bool mergePaths) {
            NudgingDirection = direction;
            PathRefiner.RefinePaths(Paths, mergePaths);
            GetPathOrdersAndPathGraph();
            MapAxisEdgesToTheirObstacles();            
            DrawPaths();
            //ShowPathsDebug(Paths);
        }

        void MapAxisEdgesToTheirObstacles() {
            axisEdgesToObstaclesTheyOriginatedFrom = new Dictionary<AxisEdge, Polyline>();
            foreach (var path in Paths)
                MapPathEndAxisEdgesToTheirObstacles(path);

            //The assignment above was too greedy. An edge belonging to interiour edges of some path can be marked by mistake.
            foreach (var path in Paths)
                UmmapPathInteriourFromStrangerObstacles(path);
        }

        void UmmapPathInteriourFromStrangerObstacles(Path path) {
            var firstUnmappedEdge = FindFirstUnmappedEdge(path);
            if(firstUnmappedEdge==null) return;
            var lastUnmappedEdge = FindLastUnmappedEdge(path);
            for (var edge = firstUnmappedEdge; edge != null && edge != lastUnmappedEdge; edge = edge.Next)
                axisEdgesToObstaclesTheyOriginatedFrom.Remove(edge.AxisEdge);
        }

        PathEdge FindLastUnmappedEdge(Path path) {
            for (var edge = path.LastEdge; edge != null; edge = edge.Prev)
                if (edge.AxisEdge.Direction != NudgingDirection)
                    return edge;
            return null;
        }

        PathEdge FindFirstUnmappedEdge(Path path) {
            for (var edge = path.FirstEdge; edge != null; edge = edge.Next)
                if (edge.AxisEdge.Direction != NudgingDirection)
                    return edge;
            return null;
        }

        void MapPathEndAxisEdgesToTheirObstacles(Path path) {
            Tuple<Polyline, Polyline> coupleOfObstacles;
            if (PathToObstacles.TryGetValue(path, out coupleOfObstacles)) {
                ProcessThePathStartToMapAxisEdgesToTheirObstacles(path, coupleOfObstacles.Item1);
                ProcessThePathEndToMapAxisEdgesToTheirObstacles(path, coupleOfObstacles.Item2);
            }            
        }

        void ProcessThePathEndToMapAxisEdgesToTheirObstacles(Path path, Polyline endPolyline) {
            for (var edge = path.LastEdge;
                 edge != null && CompassVector.DirectionsAreParallel(edge.Direction, NudgingDirection);
                 edge = edge.Prev)
                axisEdgesToObstaclesTheyOriginatedFrom[edge.AxisEdge] = endPolyline;
        }

        void ProcessThePathStartToMapAxisEdgesToTheirObstacles(Path path, Polyline startPolyline) {
            for (var edge = path.FirstEdge;
                 edge != null && CompassVector.DirectionsAreParallel(edge.Direction, NudgingDirection);
                 edge = edge.Next)
                axisEdgesToObstaclesTheyOriginatedFrom[edge.AxisEdge] = startPolyline; 
            //possible bug here because an edge might ignore two obstacles if it connects them
        }



        void GetPathOrdersAndPathGraph() {
            var combinatorialNudger = new CombinatorialNudger(Paths);
            PathOrders = combinatorialNudger.GetOrder();
            PathVisibilityGraph = combinatorialNudger.PathVisibilityGraph;
        }

        Direction NudgingDirection { get; set; }

        #region debugging
#if TEST_MSAGL
        static internal ICurve[] GetCurvesForShow(IEnumerable<Path> paths, IEnumerable<Polyline> obstacles) {
            var ret = new List<ICurve>();
            foreach (var path in paths) {
                var poly = new Polyline();
                foreach (var point in path.PathPoints)
                    poly.AddPoint(point);
                ret.Add(poly);
            }
            ret.AddRange(obstacles.Cast<ICurve>());
            return ret.ToArray();
        }
#endif
        #endregion
        void DrawPaths() {
            SetWidthsOfArrowheads();
            CreateLongestNudgedSegments();
            FindFreeSpaceInDirection(PathVisibilityGraph.Edges.Cast<AxisEdge>());
            MoveLongestSegsIdealPositionsInsideFeasibleIntervals();
            PositionShiftedEdges();
        }


        
        
        void SetWidthsOfArrowheads() {
            foreach (Path edgePath in Paths)
                SetWidthsOfArrowheadsForEdge(edgePath);
        }

        static void SetWidthsOfArrowheadsForEdge(Path path) {
            var edgeGeom = path.EdgeGeometry;
            if (edgeGeom.TargetArrowhead != null) {
                PathEdge pathEdge = path.LastEdge;
                pathEdge.Width = Math.Max(edgeGeom.TargetArrowhead.Width, pathEdge.Width);
            }
            if (edgeGeom.SourceArrowhead != null) {
                PathEdge pathEdge = path.FirstEdge;
                pathEdge.Width = Math.Max(edgeGeom.SourceArrowhead.Width, pathEdge.Width);
            }
        }

        internal double EdgeSeparation { get; set; }

        void PositionShiftedEdges() {
            //we are using 2*cornerFitRadius for the minimal edge separation
            Solver = new UniformOneDimensionalSolver(EdgeSeparation);
            for (var i = 0; i < LongestNudgedSegs.Count; i++)
                CreateVariablesOfLongestSegment(LongestNudgedSegs[i]);
            CreateConstraintsOfTheOrder();
            CreateConstraintsBetweenLongestSegments();
            Solver.Solve();
            ShiftPathEdges();
        }

        void MoveLongestSegsIdealPositionsInsideFeasibleIntervals() {
            for (int i = 0; i < this.LongestNudgedSegs.Count; i++) {
                var seg = this.LongestNudgedSegs[i];
                MoveLongestSegIdealPositionsInsideFeasibleInterval(seg);
            }
        }

        static void MoveLongestSegIdealPositionsInsideFeasibleInterval(LongestNudgedSegment seg) {
            if (seg.IsFixed) return;
            var leftBound = seg.GetLeftBound();
            var rightBound = seg.GetRightBound();
            if (seg.IdealPosition < leftBound)
                seg.IdealPosition=leftBound;
            else if( seg.IdealPosition> rightBound)
                seg.IdealPosition=rightBound;
        }

        void ShiftPathEdges() {
            foreach (var path in Paths)
                path.PathPoints = GetShiftedPoints(path).ToArray();
        }

        IEnumerable<Point> GetShiftedPoints(Path path) {            
            return RemoveSwitchbacksAndMiddlePoints(GetShiftedPointsSimple(path));
        }
        /// <summary>
        /// sometimes we have very small mistakes in the positions that have to be fixed
        /// </summary>
        /// <returns></returns>
        static Point Rectilinearise(Point a, Point b) {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=369 there are no structs in js
            b = b.Clone();
#endif
            if (a.X != b.X && a.Y != b.Y) {
                var dx = Math.Abs(a.X - b.X);
                var dy = Math.Abs(a.Y - b.Y);
                if (dx < dy)
                    b.X = a.X;
                else
                    b.Y = a.Y;
            }
            return b;
        }

        IEnumerable<Point> GetShiftedPointsSimple(Path path) {
            var edge = path.FirstEdge;
            yield return ShiftedPoint(edge.Source, edge.LongestNudgedSegment);
            foreach (var e in path.PathEdges)
                yield return ShiftedEdgePositionOfTarget(e);
        }

        Point ShiftedEdgePositionOfTarget(PathEdge e) {
            return e.LongestNudgedSegment != null || e.Next == null
                       ? ShiftedPoint(e.Target, e.LongestNudgedSegment)
                       : ShiftedPoint(e.Next.Source, e.Next.LongestNudgedSegment);
        }

        Point ShiftedPoint(Point point, LongestNudgedSegment segment) {
            if (segment == null)
                return point;
            var t = Solver.GetVariablePosition(segment.Id);
            return NudgingDirection == Direction.North ? new Point(t, point.Y) : new Point(point.X, -t);
        }

        #region debug
#if TEST_MSAGL
        internal static void ShowPathsFromPoints(IEnumerable<Path> paths, IEnumerable<Polyline> enumerable) {
            var dd = new List<DebugCurve>();
            if (enumerable != null)
                dd.AddRange(GetObstacleBoundaries(enumerable, "grey"));
            int i = 0;
            foreach (var p in paths)
                dd.AddRange(PathDebugCurvesFromPoints(p, DebugCurve.Colors[Math.Min(DebugCurve.Colors.Length, i++)]));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dd);
        }

        static IEnumerable<DebugCurve> PathDebugCurvesFromPoints(Path path, string color) {
            const double startWidth = 0.01;
            const double endWidth = 3;

            var pts = path.PathPoints.ToArray();
            double delta = (endWidth - startWidth) / (pts.Length - 1);
            for (int i = 0; i < pts.Length - 1; i++)
                yield return new DebugCurve(startWidth + delta * i, color, new LineSegment(pts[i], pts[i + 1]));
        }

          internal static void ShowParamPaths( Point s, Point e, params Path[] paths ) {
              ShowOrderedPaths(null,paths,s,e);
          }

        //         ReSharper disable UnusedMember.Local
        internal static void ShowOrderedPaths(IEnumerable<Polyline> obstacles, IEnumerable<Path> paths, Point s, Point e) {
            //           ReSharper restore UnusedMember.Local
            string[] colors = { "red", "green", "blue", "violet", "rose", "black" };

            const double startWidth = 0.001;
            const double endWidth = 0.1;
            var dd = new List<DebugCurve>();
            if (obstacles != null)
                dd.AddRange(GetObstacleBoundaries(obstacles, "grey"));
            int i = 0;
            foreach (var path in paths)
                dd.AddRange(GetTestPathAsDebugCurve(startWidth, endWidth, colors[Math.Min(colors.Length - 1, i++)], path));

            var ell = new DebugCurve(1, "black", new Ellipse(0.01, 0.01, s));
            dd.Add(ell);
            dd.Add(new DebugCurve(1, "black", new Ellipse(0.02, 0.02, e)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dd.Concat(GetObstacleBoundaries(obstacles, "lightblue")));
        }

        static IEnumerable<DebugCurve> GetTestPathAsDebugCurve(double startWidth, double endWidth, string color, Path path) {
            if (path.PathEdges.Count() > 0) {
                int count = path.PathEdges.Count();

                double deltaW = count > 1 ? (endWidth - startWidth)/(count - 1) : 1;
                    //if count ==1 the value of deltaW does not matter
                int i = 0;
                foreach (var e in path.PathEdges)
                    yield return
                        new DebugCurve(150, startWidth + deltaW*(i++), color, new LineSegment(e.Source, e.Target));
            }else {
                int count = path.PathPoints.Count();
                var pts = path.PathPoints.ToArray();

                var deltaW = count > 1 ? (endWidth - startWidth) / (count - 1) : 1;
                //if count ==1 the value of deltaW does not matter
                for (int i = 0; i < count - 1;i++ )
                    yield return new DebugCurve(150, startWidth+deltaW*i, color, new LineSegment(pts[i], pts[i+1]));                    
            }
        }

        internal static IEnumerable<DebugCurve> GetTestEdgePathAsDebugCurves(double startWidth, double endWidth, string color, Path path) {
            int count = path.PathPoints.Count();

            double deltaW = count > 1 ? (endWidth - startWidth) / (count - 1) : 1; //if count ==1 the value of deltaW does not matter
            var points = path.PathPoints.ToArray();
            for (int i = 0; i < points.Length - 1; i++)
                yield return new DebugCurve(125, startWidth + deltaW * i, color, new LineSegment(points[i], points[i + 1]));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static IEnumerable<DebugCurve> GetEdgePathFromPathEdgesAsDebugCurves(double startWidth, double endWidth, string color, Path path) {
            var points = path.PathPoints.ToArray();

            int count = points.Length;

            double deltaW = count > 1 ? (endWidth - startWidth) / (count - 1) : 1; //if count ==1 the value of deltaW does not matter
            for (int i = 0; i < points.Length - 1; i++)
                yield return new DebugCurve(120, startWidth + deltaW * i, color, new LineSegment(points[i], points[i + 1]));
        }
         // ReSharper disable UnusedMember.Local
        
        static internal void ShowEdgePaths(IEnumerable<Polyline> obstacles, IEnumerable<Path> edgePaths) {
            // ReSharper restore UnusedMember.Local
            List<DebugCurve> debCurves = GetDebCurvesOfPaths(obstacles, edgePaths);

            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(debCurves);
        }

        internal static List<DebugCurve> GetDebCurvesOfPaths(IEnumerable<Polyline> enumerable, IEnumerable<Path> edgePaths) {
            var debCurves = GetObstacleBoundaries(enumerable, "black");
            int i = 0;

            foreach (var edgePath in edgePaths)
                debCurves.AddRange(GetTestEdgePathAsDebugCurves(0.2, 4, DebugCurve.Colors[(i++) % DebugCurve.Colors.Length], edgePath));
            return debCurves;
        }

        internal static void ShowPathsInLoop(IEnumerable<Polyline> enumerable, IEnumerable<Path> edgePaths, Point point) {
            foreach (var edgePath in edgePaths.Where(path=>(path.PathPoints.First()-point).Length<1 ||(path.PathPoints.Last()-point).Length<1 )) {
                var debCurves = GetObstacleBoundaries(enumerable, "black");
                debCurves.AddRange(GetTestEdgePathAsDebugCurves(0.1, 4, "red", edgePath));
                LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(debCurves);
            }
        }


        // ReSharper disable UnusedMember.Local
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void ShowLongSegsWithIdealPositions(Direction dir) {
            // ReSharper restore UnusedMember.Local
            var debCurves = GetObstacleBoundaries(Obstacles, "black");
            int i = 0;

            debCurves.AddRange(LongestNudgedSegs.Select(ls => DebugCurveOfLongSeg(ls, DebugCurve.Colors[i++ % DebugCurve.Colors.Length], dir)));

            DebugCurveCollection.WriteToFile(debCurves, "c:/tmp/longSegs");

            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(debCurves);
        }

        static DebugCurve DebugCurveOfLongSeg(LongestNudgedSegment ls, string s, Direction dir) {
            return new DebugCurve(1, s, LineSegOfLongestSeg(ls, dir));
        }

        static ICurve LineSegOfLongestSeg(LongestNudgedSegment ls, Direction dir) {
            var projectionToDir = dir == Direction.East ? (PointProjection)(p => p.X) : (p => p.Y);
            var min = Double.PositiveInfinity;
            var max = Double.NegativeInfinity;
            foreach (var edge in ls.Edges) {
                UpdateMinMaxWithPoint(ref min, ref max, projectionToDir, edge.Source);
                UpdateMinMaxWithPoint(ref min, ref max, projectionToDir, edge.Target);
            }
            return dir == Direction.East
                       ? new LineSegment(min, -ls.IdealPosition, max, -ls.IdealPosition)
                       : new LineSegment(ls.IdealPosition, min, ls.IdealPosition, max);
        }

        static void UpdateMinMaxWithPoint(ref double min, ref double max, PointProjection projectionToDir, Point point) {
            double p = projectionToDir(point);
            if (min > p)
                min = p;
            if (max < p)
                max = p;
        }


        // ReSharper disable UnusedMember.Local
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void ShowPathsDebug(IEnumerable<Path> edgePaths) {
            var debCurves = GetObstacleBoundaries(Obstacles, "black");
            int i = 0;

            foreach (var edgePath in edgePaths)
                debCurves.AddRange(GetEdgePathFromPathEdgesAsDebugCurves(0.01, 0.4, DebugCurve.Colors[(i++) % DebugCurve.Colors.Length], edgePath));

            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(debCurves);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static IEnumerable<DebugCurve> PathDebugCurves(Path path, string color) {
            var d = path.PathEdges.Select(e => new DebugCurve(70, 0.5, color, new LineSegment(e.Source, e.Target)));
            return d.Concat(MarkPathVerts(path));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static IEnumerable<DebugCurve> MarkPathVerts(Path path) {
            bool first = true;
            var p = new Point();
            foreach (var p0 in path.PathPoints) {
                if (first) {
                    yield return new DebugCurve(200, 1, "violet", CurveFactory.CreateDiamond(5, 5, p0));
                    first = false;
                } else
                    yield return new DebugCurve(100, 0.5, "brown", CurveFactory.CreateEllipse(1.5, 1.5, p0));
                p = p0;
            }
            yield return new DebugCurve(200, 1, "green", CurveFactory.CreateDiamond(3, 3, p));
        }

        static internal IEnumerable<DebugCurve> PathDebugCurvesFromPoint(Path path) {
            var l = new List<Point>(path.PathPoints);
            for (int i = 0; i < l.Count - 1; i++)
                yield return new DebugCurve(4, "red", new LineSegment(l[i], l[i + 1]));
        }
        //
        // ReSharper disable UnusedMember.Local
        //        void ShowEdgesOfEdgePath(Path path){
        // ReSharper restore UnusedMember.Local
        //            string[] colors = {"red", "brown", "purple"};
        //            const double w0 = 1;
        //            const double w1 = 3;
        //            double dw = (w1 - w0)/path.OrientedSubpaths.Count;
        //            int i = 0;
        //            var dc = new List<DebugCurve>();
        //            foreach (var s in path.OrientedSubpaths){
        //                dc.AddRange(SubpathDebugCurves(w0 + dw*i, colors[Math.Min(i++, colors.Length - 1)], s));
        //            }
        //            LayoutAlgorithmSettings.ShowDebugCurves(dc.ToArray());
        //        }
        //
        //        static IEnumerable<DebugCurve> SubpathDebugCurves(double w, string color, OrientedSubpath subpath){
        //            return subpath.LinkedPath.Select(e => new DebugCurve(w, color, new LineSegment(e.Source.Point, e.Target.Point)));
        //        }


        static internal List<DebugCurve> GetObstacleBoundaries(IEnumerable<Polyline> obstacles, string color) {
            var debugCurves = new List<DebugCurve>();
            if (obstacles != null)
                debugCurves.AddRange(obstacles.Select(poly => new DebugCurve(50, 0.3, color, poly)));
            return debugCurves;
        }
#endif
        #endregion

        void CreateConstraintsBetweenLongestSegments() {
            foreach (var segment in LongestNudgedSegs)
                CreateConstraintsBetweenLongestSegmentsForSegment(segment);
        }

        void CreateConstraintsBetweenLongestSegmentsForSegment(LongestNudgedSegment segment) {
            var rightNeighbors = new Set<LongestNudgedSegment>();
            foreach (var pathEdge in segment.Edges) {
                var axisEdge = pathEdge.AxisEdge;
                if (axisEdge != null)
                    foreach (var rightNeiAxisEdge in axisEdge.RightNeighbors)
                        foreach (var longSeg in rightNeiAxisEdge.LongestNudgedSegments)
                            rightNeighbors.Insert(longSeg);
            }

            foreach (var seg in rightNeighbors)
                ConstraintTwoLongestSegs(segment, seg);

        }

        void CreateConstraintsOfTheOrder() {
            foreach (var kv in PathOrders)
                if (ParallelToDirection(kv.Key, NudgingDirection))
                    CreateConstraintsOfThePathOrder(kv.Value);
        }

        static bool ParallelToDirection(VisibilityEdge edge, Direction direction) {
            switch (direction) {
                case Direction.North:
                case Direction.South:
                    return ApproximateComparer.Close(edge.SourcePoint.X, edge.TargetPoint.X);
                default:
                    return ApproximateComparer.Close(edge.SourcePoint.Y, edge.TargetPoint.Y);
            }
        }

        void CreateConstraintsOfThePathOrder(IEnumerable<PathEdge> pathOrder) {
            PathEdge prevEdge = null;

            foreach (var pathEdge in pathOrder.Where(p => p.LongestNudgedSegment != null)) {
                if (prevEdge != null)
                    ConstraintTwoLongestSegs(prevEdge.LongestNudgedSegment, pathEdge.LongestNudgedSegment);
                prevEdge = pathEdge;
            }
        }

        void ConstraintTwoLongestSegs(LongestNudgedSegment prevSeg, LongestNudgedSegment seg) {
            if (!prevSeg.IsFixed || !seg.IsFixed)
                Solver.AddConstraint(prevSeg.Id, seg.Id);
        }

        UniformOneDimensionalSolver Solver { get; set; }

        void CreateVariablesOfLongestSegment(LongestNudgedSegment segment) {
            if (!segment.IsFixed) {
                var leftBound = segment.GetLeftBound();
                var rightBound = segment.GetRightBound();
                if (leftBound >= rightBound) {//don't move the segment from the way it was generated
                    Solver.AddFixedVariable(segment.Id, SegmentPosition(segment, NudgingDirection));
                    segment.IsFixed = true;
                } else {
                    Solver.AddVariable(segment.Id, SegmentPosition(segment, NudgingDirection), segment.IdealPosition, segment.Width);
         //           Debug.Assert(leftBound + Curve.DistanceEpsilon < rightBound); //this assert does not hold for overlaps
                    if (leftBound != Double.NegativeInfinity)
                        Solver.SetLowBound(leftBound, segment.Id);
                    if (rightBound != Double.PositiveInfinity)
                        Solver.SetUpperBound(segment.Id, rightBound);
                }
            } else
                Solver.AddFixedVariable(segment.Id, SegmentPosition(segment, NudgingDirection));
        }

        static double SegmentPosition(SegmentBase segment, Direction direction) {
            return direction == Direction.North ? segment.Start.X : -segment.Start.Y;
        }

#if TEST_MSAGL
        // ReSharper disable UnusedMember.Local
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void ShowSegmentBounds(LongestNudgedSegment segment) {
            // ReSharper restore UnusedMember.Local
            var dd = GetObstacleBoundaries(Obstacles, "black");
            var segtop = segment.Edges.Max(e => Math.Max(e.Source.Y, e.Target.Y));
            var segbottom = segment.Edges.Min(e => Math.Min(e.Source.Y, e.Target.Y));
            var segx = segment.Start.X;
            var seg = new DebugCurve(80, 1, "brown", new LineSegment(new Point(segx, segbottom), new Point(segx, segtop)));

            var leftbound = new DebugCurve(80, 1.0, "red", new LineSegment(new Point(segment.GetLeftBound(), segbottom),
                                                                           new Point(segment.GetLeftBound(), segtop)));
            var rightbound = new DebugCurve(80, 1, "green", new LineSegment(new Point(segment.GetRightBound(), segbottom),
                            new Point(segment.GetRightBound(), segtop)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dd.Concat(new[] { seg, leftbound, rightbound }));
        }
        // ReSharper disable UnusedMember.Local
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void ShowSegment(LongestNudgedSegment segment) {
            // ReSharper restore UnusedMember.Local
            var dd = GetObstacleBoundaries(Obstacles, "black");
            var segtop = segment.Edges.Max(e => Math.Max(e.Source.Y, e.Target.Y));
            var segbottom = segment.Edges.Min(e => Math.Min(e.Source.Y, e.Target.Y));
            var segx = segment.Start.X;
            var seg = new DebugCurve(80, 1, "brown", new LineSegment(new Point(segx, segbottom), new Point(segx, segtop)));

            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dd.Concat(new[] { seg }));

        }
#endif

        List<LongestNudgedSegment> LongestNudgedSegs { get; set; }

        Dictionary<AxisEdge, List<PathEdge>> PathOrders { get; set; }
        /// <summary>
        /// maps each path to the pair of obstacles; the first element of the pair is 
        /// where the path starts and the second where the path ends
        /// </summary>
        Dictionary<Path, Tuple<Polyline, Polyline>> PathToObstacles { get; set; }


        void FindFreeSpaceInDirection(IEnumerable<AxisEdge> axisEdges) {
            BoundAxisEdgesByRectsKnownInAdvance();
            var freeSpaceFinder = new FreeSpaceFinder(NudgingDirection, Obstacles,
                                                      axisEdgesToObstaclesTheyOriginatedFrom, PathOrders, axisEdges);
            freeSpaceFinder.FindFreeSpace();
        }

        void BoundAxisEdgesByRectsKnownInAdvance() {
            foreach (var path in Paths) {
                if (HasGroups)
                    BoundPathByMinCommonAncestors(path);
                BoundAxisEdgesAdjacentToSourceAndTargetOnEdge(path);               
            }
        }

        void BoundPathByMinCommonAncestors(Path path) {
            foreach (var rect in GetMinCommonAncestors(path.EdgeGeometry).Select(sh => sh.BoundingBox))
                foreach (
                    var edge in
                        path.PathEdges.Select(e => e.AxisEdge).Where(
                            axisEdge => axisEdge.Direction == NudgingDirection)
                    )
                    BoundAxisEdgeByRect(rect, edge);
        }

        
        IEnumerable<Shape> GetMinCommonAncestors(EdgeGeometry edgeGeometry) {
            if (PortToShapes == null)
                PortToShapes = MapPortsToShapes(AncestorsSets.Keys);
            var commonAncestors = AncestorsForPort(edgeGeometry.SourcePort)*
                                  AncestorsForPort(edgeGeometry.TargetPort);
            return commonAncestors.Where(anc => !anc.Children.Any(child=>commonAncestors.Contains(child)));
        }
        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<Port, Shape> PortToShapes { get; private set; }

        Set<Shape> AncestorsForPort(Port port) {
            Shape shape;
            if (PortToShapes.TryGetValue(port, out shape)) {
                return AncestorsSets[shape];
            }

            // This is a FreePort or Waypoint; return all spatial parents.
            return new Set<Shape>(HierarchyOfGroups.AllHitItems(new Rectangle(port.Location, port.Location), null));
        }


        void BoundAxisEdgeAdjacentToObstaclePort(Port port, AxisEdge axisEdge) {
            if (port.Curve == null )
                BoundAxisByPoint(port.Location, axisEdge);
            else  {
                if (port.Curve.BoundingBox.Contains(port.Location))
                    BoundAxisEdgeByRect(port.Curve.BoundingBox, axisEdge);
            } 
        }

        void BoundAxisByPoint(Point point, AxisEdge axisEdge) {
            if (axisEdge != null && axisEdge.Direction == NudgingDirection)
                if (NudgingDirection == Direction.North) {
                    axisEdge.BoundFromLeft(point.X);
                    axisEdge.BoundFromRight(point.X);
                } else {
                    axisEdge.BoundFromLeft(-point.Y);
                    axisEdge.BoundFromRight(-point.Y);
                }
        }

        void BoundAxisEdgesAdjacentToSourceAndTargetOnEdge(Path path) {
            BoundAxisEdgeAdjacentToObstaclePort(path.EdgeGeometry.SourcePort, path.FirstEdge.AxisEdge);
            BoundAxisEdgeAdjacentToObstaclePort(path.EdgeGeometry.TargetPort, path.LastEdge.AxisEdge);
        }

        void BoundAxisEdgeByRect(Rectangle rectangle, AxisEdge axisEdge) {
            if (axisEdge != null && axisEdge.Direction == NudgingDirection)
                if (NudgingDirection == Direction.North) {
                    axisEdge.BoundFromLeft(rectangle.Left);
                    axisEdge.BoundFromRight(rectangle.Right);
                } else {
                    axisEdge.BoundFromLeft(-rectangle.Top);
                    axisEdge.BoundFromRight(-rectangle.Bottom);
                }
        }

     
        void CreateLongestNudgedSegments() {
            var projectionToPerp =
                NudgingDirection == Direction.East ? (PointProjection)FreeSpaceFinder.MinusY : FreeSpaceFinder.X;

            LongestNudgedSegs = new List<LongestNudgedSegment>();
            for (int i = 0; i < Paths.Count; i++) 
                CreateLongestNudgedSegmentsForPath(Paths[i], projectionToPerp);
        }


        void CreateLongestNudgedSegmentsForPath(Path path, PointProjection projectionToPerp) {
            //ShowEdgesOfEdgePath(path);
            GoOverPathAndCreateLongSegs(path);
            CalculateIdealPositionsForLongestSegs(path, projectionToPerp);
        }

        static void CalculateIdealPositionsForLongestSegs(Path path, PointProjection projectionToPerp) {
            LongestNudgedSegment currentLongSeg = null;
            LongestNudgedSegment ret = null;
            double prevOffset = projectionToPerp(path.Start);
            foreach (var edge in path.PathEdges) {
                if (edge.LongestNudgedSegment != null) {
                    currentLongSeg = edge.LongestNudgedSegment;
                    if (ret != null) {
                        double t;
                        SetIdealPositionForSeg(ret, t = projectionToPerp(ret.Start), prevOffset,
                                               projectionToPerp(currentLongSeg.Start));
                        prevOffset = t;
                        ret = null;
                    }
                } else if (currentLongSeg != null) {
                    ret = currentLongSeg;
                    currentLongSeg = null;
                }
            }
            if (ret != null)
                SetIdealPositionForSeg(ret, projectionToPerp(ret.Start), prevOffset, projectionToPerp(path.End));
            else if (currentLongSeg != null)
                currentLongSeg.IdealPosition = projectionToPerp(currentLongSeg.Start);
        }

        static void SetIdealPositionForSeg(LongestNudgedSegment segment, double segPosition, double offset0, double offset1) {
            var max = Math.Max(offset0, offset1);
            var min = Math.Min(offset0, offset1);
            if (min + ApproximateComparer.DistanceEpsilon < segPosition)
                if (segPosition < max)
                    segment.IdealPosition = 0.5 * (max + min);
                else
                    segment.IdealPosition = max;
            else
                segment.IdealPosition = min;


        }

        void GoOverPathAndCreateLongSegs(Path path) {
            LongestNudgedSegment currentLongestSeg = null;

            var oppositeDir = CompassVector.OppositeDir(NudgingDirection);

            foreach (var edge in path.PathEdges) {
                var edgeDir = edge.Direction;
                if (edgeDir == NudgingDirection || edgeDir == oppositeDir) {
                    if (currentLongestSeg == null)
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=368
                    {
                        edge.LongestNudgedSegment = currentLongestSeg = new LongestNudgedSegment(LongestNudgedSegs.Count);
                        LongestNudgedSegs.Add(edge.LongestNudgedSegment);
                    }
#else
                        LongestNudgedSegs.Add(
                            edge.LongestNudgedSegment =
                            currentLongestSeg = new LongestNudgedSegment(LongestNudgedSegs.Count));
#endif
                    else
                        edge.LongestNudgedSegment = currentLongestSeg;

                    if (edge.IsFixed)
                        currentLongestSeg.IsFixed = true;
                } else {
                    //the edge is perpendicular to "direction"
                    edge.LongestNudgedSegment = null;
                    currentLongestSeg = null;
                }
            }
        }

        static IEnumerable<Point> BuildPolylineForPath(Path path) {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=369
            var points = path.PathPoints.Select(p=>p.Clone()).ToArray();
#else
            var points = path.PathPoints.ToArray();
#endif
            ExtendPolylineToPorts(ref points, path);
            for(int i=0;i<points.Length-1;i++)
                Debug.Assert(CompassVector.IsPureDirection(points[i], points[i+1]));

            return points;
        }

        static void ExtendPolylineToPorts(ref Point[] points, Path path) {
            ExtendPolylineToSourcePort(ref points, path.EdgeGeometry.SourcePort.Location);
            ExtendPolylineToTargetPort(ref points, path.EdgeGeometry.TargetPort.Location);
            
            // In some overlapped cases where the source or target vertex used for the path
            // coincides with the target or source port location, we can end up with a single-point
            // path.  In that case, we just force a straightline path.
            if (points.Length < 2) {
                points = new Point[2];
                points[0] = path.EdgeGeometry.SourcePort.Location;
                points[1] = path.EdgeGeometry.TargetPort.Location;
            }
        }

        static void ExtendPolylineToTargetPort(ref Point[] points, Point location) {
            int n = points.Length - 1;
            var dir = CompassVector.VectorDirection(points[n - 1], points[n]);
            if (ProjectionsAreClose(points[n-1], dir, location)) {
                //it might be that the last point on polyline is at the port already
                //then we just drop the last point
                points = points.Take(n).ToArray();
                return;
            }
            if (dir == Direction.East || dir == Direction.West)
                points[n].X = location.X;
            else
                points[n].Y = location.Y;
        }

        static bool ProjectionsAreClose(Point a, Direction dir, Point b) {
            if (dir == Direction.East || dir == Direction.West)
                return ApproximateComparer.Close(a.X, b.X);
            return ApproximateComparer.Close(a.Y, b.Y);
        }

        static void ExtendPolylineToSourcePort(ref Point[] points, Point location) {
            var dir = CompassVector.VectorDirection(points[0], points[1]);
            if (ProjectionsAreClose(points[1], dir, location)) {
                //it might be that the second point on polyline is at the port already
                //then we just drop the first point
                points = points.Skip(1).ToArray();
                return;
            }
            if (dir == Direction.East || dir == Direction.West)
                points[0].X = location.X;
            else
                points[0].Y = location.Y;
        }

        static IEnumerable<Point> RemoveSwitchbacksAndMiddlePoints(IEnumerable<Point> points) {
            var en = points.GetEnumerator();
            en.MoveNext();
            var a = en.Current;
            yield return a;
            en.MoveNext();
            var b = en.Current;
            var prevDir = (b - a).CompassDirection;

            while (en.MoveNext()) {
                var dir = (en.Current - b).CompassDirection;
                if (!(dir == prevDir || CompassVector.OppositeDir(dir) == prevDir || dir == Direction.None)) { //we continue walking along the same straight line, maybe going backwards!
                    if (!ApproximateComparer.Close(a, b)) {//make sure that we are not returning the same point twice                        
                        yield return a = Rectilinearise(a, b);
                    }
                    prevDir = dir;
                }
                b = en.Current;

            }
            if (!ApproximateComparer.Close(a, b))
                yield return Rectilinearise(a, b);
        }


        /// <summary>
        /// this function defines the final path coordinates
        /// </summary>
        /// <param name="paths">the set of paths, point sequences</param>
        /// <param name="cornerFitRadius">the radius of the arc inscribed into the path corners</param>
        /// <param name="paddedObstacles">an enumeration of padded obstacles</param>
        /// <param name="ancestorsSets"></param>
        /// <param name="removeStaircases"></param>
        /// <returns>the mapping of the path to its modified path</returns>
        internal static void NudgePaths(IEnumerable<Path> paths, double cornerFitRadius, IEnumerable<Polyline> paddedObstacles, Dictionary<Shape, Set<Shape>> ancestorsSets, bool removeStaircases) {
            if (!paths.Any())
                return;
            var nudger = new Nudger(paths, cornerFitRadius, paddedObstacles, ancestorsSets);
            nudger.Calculate(Direction.North, true);
            nudger.Calculate(Direction.East, false);
            nudger.Calculate(Direction.North, false);
            if (removeStaircases)
                nudger.RemoveStaircases();
            foreach (var path in paths)
                path.EdgeGeometry.Curve = new Polyline(BuildPolylineForPath(path));
        }

        void RemoveStaircases() {
            StaircaseRemover.RemoveStaircases(Paths, HierarchyOfObstacles);
            
        }

        internal static Dictionary<Port, Shape> MapPortsToShapes(IEnumerable<Shape> listOfShapes) {
            var portToShapes = new Dictionary<Port, Shape>();
            foreach (Shape shape in listOfShapes)
                foreach (Port port in shape.Ports)
                    portToShapes[port] = shape;
            return portToShapes;
        }
    }
}
