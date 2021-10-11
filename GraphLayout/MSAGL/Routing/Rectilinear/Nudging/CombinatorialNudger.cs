using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging {
    /// <summary>
    /// sets the order of connector paths on the edges
    /// </summary>
    internal class CombinatorialNudger {

        const int NotOrdered = int.MaxValue;

        //A new visibility graph is needed; the DAG of AxisEdges.
        readonly VisibilityGraph pathVisibilityGraph = new VisibilityGraph();
        internal VisibilityGraph PathVisibilityGraph {
            get { return pathVisibilityGraph; }
        }

    
        readonly Dictionary<AxisEdge, List<PathEdge>> axisEdgesToPathOrders = new Dictionary<AxisEdge, List<PathEdge>>();

        internal CombinatorialNudger(IEnumerable<Path> paths) {
            OriginalPaths = paths;
        }

        IEnumerable<Path> OriginalPaths { get; set; }

        internal Dictionary<AxisEdge, List<PathEdge>> GetOrder() {
            FillTheVisibilityGraphByWalkingThePaths();
            InitPathOrder();
            OrderPaths();
            return axisEdgesToPathOrders;
        }

        void FillTheVisibilityGraphByWalkingThePaths() {
            foreach (var path in OriginalPaths)
                FillTheVisibilityGraphByWalkingPath(path);
        }

        void FillTheVisibilityGraphByWalkingPath(Path path){
            var pathEdgesEnum = CreatePathEdgesFromPoints(path.PathPoints, path.Width).GetEnumerator();

            if (pathEdgesEnum.MoveNext())
                path.SetFirstEdge(pathEdgesEnum.Current);
            
            while(pathEdgesEnum.MoveNext())
                path.AddEdge(pathEdgesEnum.Current);
        }

        IEnumerable<PathEdge> CreatePathEdgesFromPoints(IEnumerable<Point> pathPoints, double width) {
            var p0 = pathPoints.First();
            foreach (var p1 in pathPoints.Skip(1)) {
                yield return CreatePathEdge(p0, p1, width);
                p0 = p1;
            }
        }

        PathEdge CreatePathEdge(Point p0, Point p1, double width){
            var dir = CompassVector.DirectionsFromPointToPoint(p0, p1);
            switch (dir){
                case Direction.East:
                case Direction.North:
                    return new PathEdge(GetAxisEdge(p0, p1), width);
                case Direction.South:
                case Direction.West:
                
                return new PathEdge(GetAxisEdge(p1,p0), width){Reversed = true};
                default:
                    throw new InvalidOperationException(
#if TEST_MSAGL
                        "Not a rectilinear path"
#endif
                        );
            }
        }

        AxisEdge GetAxisEdge(Point p0, Point p1){
            return PathVisibilityGraph.AddEdge(p0, p1, ((m, n) => new AxisEdge(m, n))) as AxisEdge;
        }

        void InitPathOrder() {
            foreach (var axisEdge in PathVisibilityGraph.Edges.Select(a => (AxisEdge) a))
                axisEdgesToPathOrders[axisEdge] = new List<PathEdge>();

            foreach (var pathEdge in OriginalPaths.SelectMany(path => path.PathEdges))
                axisEdgesToPathOrders[pathEdge.AxisEdge].Add(pathEdge);
        }


        void OrderPaths() {
            foreach (var axisEdge in WalkGraphEdgesInTopologicalOrderIfPossible(PathVisibilityGraph))
                OrderPathEdgesSharingEdge(axisEdge);
        }

        void OrderPathEdgesSharingEdge(AxisEdge edge) {
            var pathOrder = PathOrderOfVisEdge(edge);
            pathOrder.Sort(new Comparison<PathEdge>(CompareTwoPathEdges));
            var i = 0; //fill the index
            foreach (var pathEdge in pathOrder)
                pathEdge.Index = i++;
//            if (pathOrder.PathEdges.Count > 1)
//                Nudger.ShowOrderedPaths(null,pathOrder.PathEdges.Select(e => e.Path), edge.SourcePoint, edge.TargetPoint);
        }


        static int CompareTwoPathEdges(PathEdge x, PathEdge y) {
            if (x == y)
                return 0;
            Debug.Assert(x.AxisEdge == y.AxisEdge);
            //Nudger.ShowOrderedPaths(null, new[] { x.Path, y.Path }, x.AxisEdge.SourcePoint, x.AxisEdge.TargetPoint);
            int r = CompareInDirectionStartingFromAxisEdge(x, y, x.AxisEdge, x.AxisEdge.Direction);
            return r!=0 ? r : -CompareInDirectionStartingFromAxisEdge(x, y, x.AxisEdge, CompassVector.OppositeDir(x.AxisEdge.Direction));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="axisEdge">axisEdge together with the axisEdgeIsReversed parameter define direction of the movement over the paths</param>
        /// <param name="direction"></param>
        /// <returns></returns>
        static int CompareInDirectionStartingFromAxisEdge(PathEdge x, PathEdge y, AxisEdge axisEdge, Direction direction){
            while (true) {
                x = GetNextPathEdgeInDirection(x, axisEdge, direction);
                if (x == null)
                    return 0;
                y = GetNextPathEdgeInDirection(y, axisEdge, direction);
                if (y == null)
                    return 0;
                if (x.AxisEdge == y.AxisEdge) {
                    direction = FindContinuedDirection(axisEdge, direction, x.AxisEdge);
                    axisEdge = x.AxisEdge;
                    int r = GetExistingOrder(x, y);
                    if (r == NotOrdered) continue;
                    return direction == axisEdge.Direction ? r : -r;
                }
                //there is a fork
                var forkVertex = direction == axisEdge.Direction ? axisEdge.Target : axisEdge.Source;
                var xFork = OtherVertex(x.AxisEdge, forkVertex);
                var yFork = OtherVertex(y.AxisEdge, forkVertex);
                var projection = ProjectionForCompare(axisEdge, direction != axisEdge.Direction);
                return projection(xFork.Point).CompareTo(projection(yFork.Point));
            }
        }

        static Direction FindContinuedDirection(AxisEdge edge, Direction direction, AxisEdge nextAxisEdge) {
            if (edge.Direction == direction)
                return nextAxisEdge.Source == edge.Target
                           ? nextAxisEdge.Direction
                           : CompassVector.OppositeDir(nextAxisEdge.Direction);

            return nextAxisEdge.Source == edge.Source
                       ? nextAxisEdge.Direction
                       : CompassVector.OppositeDir(nextAxisEdge.Direction);
        }

        static VisibilityVertex OtherVertex(VisibilityEdge axisEdge, VisibilityVertex v) {
            return axisEdge.Source==v?axisEdge.Target:axisEdge.Source;
        }

        static PointProjection ProjectionForCompare(AxisEdge axisEdge, bool isReversed) {
            if (axisEdge.Direction == Direction.North)
                return isReversed ? (p => -p.X) : (PointProjection)(p => p.X);
            return isReversed ? (p => p.Y) : (PointProjection)(p => -p.Y);
        }

        static PathEdge GetNextPathEdgeInDirection(PathEdge e, AxisEdge axisEdge, Direction direction) {
            Debug.Assert(e.AxisEdge==axisEdge);
            return axisEdge.Direction == direction ? (e.Reversed ? e.Prev : e.Next) : (e.Reversed ? e.Next : e.Prev);
        }


        static int GetExistingOrder(PathEdge x, PathEdge y ) {
            int xi = x.Index;
            if (xi == -1)
                return NotOrdered;
            int yi = y.Index;
            Debug.Assert(yi!=-1);
            return xi.CompareTo(yi);
        }


        internal List<PathEdge> PathOrderOfVisEdge(AxisEdge axisEdge) {
            return axisEdgesToPathOrders[axisEdge];
        }

        static void InitQueueOfSources(Queue<VisibilityVertex> queue, IDictionary<VisibilityVertex, int> dictionary, VisibilityGraph graph) {
            foreach (var v in graph.Vertices()) {
                int inDegree = v.InEdgesCount();
                dictionary[v] = inDegree;
                if (inDegree == 0)
                    queue.Enqueue(v);
            }
            Debug.Assert(queue.Count > 0);

        }

        static internal IEnumerable<AxisEdge> WalkGraphEdgesInTopologicalOrderIfPossible(VisibilityGraph visibilityGraph){
            //Here the visibility graph is always a DAG since the edges point only to North and East
            // where possible
            var sourcesQueue = new Queue<VisibilityVertex>();
            var inDegreeLeftUnprocessed = new Dictionary<VisibilityVertex, int>();
            InitQueueOfSources(sourcesQueue, inDegreeLeftUnprocessed, visibilityGraph);
            while (sourcesQueue.Count > 0){
                var visVertex = sourcesQueue.Dequeue();
                foreach (var edge in visVertex.OutEdges){
                    var incomingEdges = inDegreeLeftUnprocessed[edge.Target]--;
                    if(incomingEdges == 1)//it is already zero in the dictionary; all incoming edges have been processed
                        sourcesQueue.Enqueue(edge.Target);
                    yield return (AxisEdge)edge;
                }

            }
        }
    }
}