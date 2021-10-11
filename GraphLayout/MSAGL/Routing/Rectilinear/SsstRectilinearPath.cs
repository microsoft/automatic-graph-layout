using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing.Visibility;

//following "Orthogonal Connector Routing" of Michael Wybrow and others

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// single source single target rectilinear path
    /// </summary>
    internal class SsstRectilinearPath {
        internal double LengthImportance { get; set; }
        internal double BendsImportance { get; set; }

        // Only bends importance needs to be public.
        internal const double DefaultBendPenaltyAsAPercentageOfDistance = 4.0;

        private VisibilityVertexRectilinear Target { get; set; }
        private VisibilityVertexRectilinear Source { get; set; }
        private Direction EntryDirectionsToTarget { get; set; }
        private double upperBoundOnCost;
        private double sourceCostAdjustment;
        private double targetCostAdjustment;

        /// <summary>
        /// The cost of the path calculation
        /// </summary>
        private double CombinedCost(double length, double numberOfBends) {
            return LengthImportance * length + BendsImportance * numberOfBends;
        }

        private double TotalCostFromSourceToVertex(double length, double numberOfBends) {
            return this.CombinedCost(length, numberOfBends) + this.sourceCostAdjustment;
        }

        /// <summary>
        /// The priority queue for path extensions.
        /// </summary>
        private GenericBinaryHeapPriorityQueue<VertexEntry> queue;

        /// <summary>
        /// The list of vertices we've visited for all paths.
        /// </summary>
        private List<VisibilityVertexRectilinear> visitedVertices;

        // For consistency and speed, path extensions impose an ordering as in the paper:  straight, right, left.  We
        // enqueue entries in the reverse order of preference so the latest timestamp will be the preferred direction.
        // Thus straight-ahead neighbors are in slot 2, right in slot 1, left in slot 0.  (If the target happens
        // to be to the Left, then the heuristic lookahead score will override the Right preference).
        private class NextNeighbor {
            internal VisibilityVertexRectilinear Vertex;
            internal double Weight;

            internal NextNeighbor() {
                Clear();
            }

            internal void Set(VisibilityVertexRectilinear v, double w) {
                this.Vertex = v;
                this.Weight = w;
            }

            internal void Clear() {
                this.Vertex = null;
                this.Weight = double.NaN;
            }
        }

        /// <summary>
        /// The next neighbors to extend the path to from the current vertex.
        /// </summary>
        private readonly NextNeighbor[] nextNeighbors = new[] {new NextNeighbor(), new NextNeighbor(), new NextNeighbor() };

        public SsstRectilinearPath() {
            LengthImportance = 1.0;
            BendsImportance = 1.0;
        }

        private bool InitPath(VertexEntry[] sourceVertexEntries, VisibilityVertexRectilinear source, VisibilityVertexRectilinear target) {
            if ((source == target) || !InitEntryDirectionsAtTarget(target)) {
                return false;
            }
            this.Target = target;
            this.Source = source;
            double cost = this.TotalCostFromSourceToVertex(0, 0) + HeuristicDistanceFromVertexToTarget(source.Point, Direction. None);
            if (cost >= this.upperBoundOnCost) {
                return false;
            }

            // This path starts lower than upperBoundOnCost, so create our structures and process it.
            this.queue = new GenericBinaryHeapPriorityQueue<VertexEntry>();
            this.visitedVertices = new List<VisibilityVertexRectilinear> { source };

            if (sourceVertexEntries == null) {
                EnqueueInitialVerticesFromSource(cost);
            } else {
                EnqueueInitialVerticesFromSourceEntries(sourceVertexEntries);
            }
            return this.queue.Count > 0;
        }

        private bool InitEntryDirectionsAtTarget(VisibilityVertex vert) {
            EntryDirectionsToTarget = Direction. None;

            // This routine is only called once so don't worry about optimizing foreach.
            foreach (var edge in vert.OutEdges) {
#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=368 property assignment not working with |= operator
                EntryDirectionsToTarget = EntryDirectionsToTarget | CompassVector.DirectionsFromPointToPoint(edge.TargetPoint, vert.Point);
#else
                EntryDirectionsToTarget |= CompassVector.DirectionsFromPointToPoint(edge.TargetPoint, vert.Point);
#endif
            }
            foreach (var edge in vert.InEdges) {
#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=368 property assignment not working with |= operator
                EntryDirectionsToTarget = EntryDirectionsToTarget | CompassVector.DirectionsFromPointToPoint(edge.SourcePoint, vert.Point);
#else
                EntryDirectionsToTarget |= CompassVector.DirectionsFromPointToPoint(edge.SourcePoint, vert.Point);
#endif
            }
            // If this returns false then the target is isolated.
            return EntryDirectionsToTarget != Direction. None;
        }

        private static bool IsInDirs(Direction direction, Direction dirs) {
            return direction == (direction & dirs);
        }

        internal double MultistageAdjustedCostBound(double bestCost) {
            // Allow an additional bend's cost for intermediate stages so we don't jump out early.
            return !double.IsPositiveInfinity(bestCost) ? bestCost + this.BendsImportance : bestCost;
        }

        /// <summary>
        /// estimation from below for the distance
        /// </summary>
        /// <param name="point"></param>
        /// <param name="entryDirToVertex"></param>
        /// <returns></returns>
        private double HeuristicDistanceFromVertexToTarget(Point point, Direction entryDirToVertex) {
            Point vectorToTarget = Target.Point - point;
            if (ApproximateComparer.Close(vectorToTarget.X, 0) && ApproximateComparer.Close(vectorToTarget.Y, 0)) {
                // We are at the target.
                return this.targetCostAdjustment;
            }
            Direction dirToTarget = CompassVector.VectorDirection(vectorToTarget);

            int numberOfBends;
            if (entryDirToVertex == Direction. None) {
                entryDirToVertex = Direction.East | Direction.North | Direction.West | Direction.South;
                numberOfBends = GetNumberOfBends(entryDirToVertex, dirToTarget);
            } else {
                numberOfBends = GetNumberOfBends(entryDirToVertex, dirToTarget);
            }
            return CombinedCost(ManhattanDistance(point, Target.Point), numberOfBends) + this.targetCostAdjustment;
        }

        private int GetNumberOfBends(Direction entryDirToVertex, Direction dirToTarget) {
            return CompassVector.IsPureDirection(dirToTarget)
                                    ? GetNumberOfBendsForPureDirection(entryDirToVertex, dirToTarget)
                                    : GetBendsForNotPureDirection(dirToTarget, entryDirToVertex, EntryDirectionsToTarget);
        }

        private int GetNumberOfBendsForPureDirection(Direction entryDirToVertex, Direction dirToTarget) {
            if ( (dirToTarget & entryDirToVertex) == dirToTarget) {
                if (IsInDirs(dirToTarget, EntryDirectionsToTarget)) {
                    return 0;
                }
                if (IsInDirs(Left(dirToTarget), EntryDirectionsToTarget) || IsInDirs(Right(dirToTarget), EntryDirectionsToTarget)) {
                    return 2;
                }
                return 4;
            }
            return GetNumberOfBendsForPureDirection(AddOneTurn[(int)entryDirToVertex], dirToTarget) + 1;
        }

        private static int GetBendsForNotPureDirection(Direction dirToTarget, Direction entryDirToVertex, Direction entryDirectionsToTarget) {
            Direction a = dirToTarget & entryDirToVertex;
            if (a == Direction. None) {
                return GetBendsForNotPureDirection(dirToTarget, AddOneTurn[(int)entryDirToVertex], entryDirectionsToTarget) + 1;
            }
            Direction b = dirToTarget & entryDirectionsToTarget;
            if (b == Direction. None) {
                return GetBendsForNotPureDirection(dirToTarget, entryDirToVertex, AddOneTurn[(int)entryDirectionsToTarget]) + 1;
            }
            return (a | b) == dirToTarget ? 1 : 2;
        }

        private static readonly Direction[] AddOneTurn=new[] {
            Direction.None,  //Directions. None-> None
            Direction.North|Direction.East|Direction.West, // 1=N -> N,E,W
            Direction.North|Direction.East|Direction.South, // 2 =E->E|N|S
            (Direction)15, // 3 =E|N->E|N|S
            Direction.East|Direction.South|Direction.West, // 4 =S->E|S|W
            (Direction)15, // 5 =E|N->E|N|S|W
            (Direction)15,          //6 - S|E
            (Direction)15,  //7
            (Direction)13,  //8=W
            (Direction)15,  //9
            (Direction)15,    //10
            (Direction)15,  //11
            (Direction)15,  //12
            (Direction)15,  //13
            (Direction)15,  //14
            (Direction)15,  //15
        };

        private static Direction Left(Direction direction) {
            switch (direction) {
                case Direction. None:
                    return Direction. None;
                case Direction.North:
                    return Direction.West;
                case Direction.East:
                    return Direction.North;
                case Direction.South:
                    return Direction.East;
                case Direction.West:
                    return Direction.South;
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
        }

        private static Direction Right(Direction direction) {
            switch (direction) {
                case Direction. None:
                    return Direction. None;
                case Direction.North:
                    return Direction.East;
                case Direction.East:
                    return Direction.South;
                case Direction.South:
                    return Direction.West;
                case Direction.West:
                    return Direction.North;
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
        }

        internal static IEnumerable<Point> RestorePath(VertexEntry entry) {
            return RestorePath(ref entry, null);
        }

        internal static IEnumerable<Point> RestorePath(ref VertexEntry entry, VisibilityVertex firstVertexInStage) {
            if (entry == null) {
                return null;
            }
            var list = new List<Point>();
            bool skippedCollinearEntry = false;
            Direction lastEntryDir = Direction. None;
            while (true) {
                // Reduce unnecessary AxisEdge creations in Nudger by including only bend points, not points in the middle of a segment.
                if (lastEntryDir == entry.Direction) {
                    skippedCollinearEntry = true;
                } else {
                    skippedCollinearEntry = false;
                    list.Add(entry.Vertex.Point);
                    lastEntryDir = entry.Direction;
                }

                var previousEntry = entry.PreviousEntry;
                if ((previousEntry == null) || (entry.Vertex == firstVertexInStage)) {
                    break;
                }
                entry = previousEntry;
            }
            if (skippedCollinearEntry) {
                list.Add(entry.Vertex.Point);
            }
            list.Reverse();
            return list;
        }

#region TEST_MSAGL
#if TEST_MSAGL
// ReSharper disable UnusedMember.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "len"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "newEntry"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "nbend"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "iters"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "hcost"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ccost"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "so"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "q"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "edges"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.WriteLine(System.String,System.Object[])"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void TestShowAllPaths(VisibilityVertex source, VertexEntry mostRecentlyExtendedPath) {
// ReSharper restore UnusedMember.Local
            var edges = GetAllEdgesTest(source).Select(e => (ICurve) (new LineSegment(e.SourcePoint, e.TargetPoint))).
                        Select(c => new DebugCurve(c));
            var q = queue.Select(ent => CurveFactory.CreateDiamond(2, 2, ent.Vertex.Point)).Select(c => new DebugCurve(c));
            var so = new[] {
                              new DebugCurve(1, "brown", new Ellipse(3, 3, source.Point)),
                              new DebugCurve(1, "purple", CurveFactory.CreateDiamond(4, 4, Target.Point)),
                              new DebugCurve(1, "red", CurveFactory.CreateDiamond(6, 6, mostRecentlyExtendedPath.Vertex.Point))
                          };

            var pathEdges = new List<DebugCurve>();
            var newEntries = new List<VertexEntry>();
            var count = this.visitedVertices.Count;
            for (int ii = 0; ii < count; ++ii) {
                var vertex = this.visitedVertices[ii];
                if (vertex.VertexEntries == null) {
                    continue;   // this is the source vertex
                }
                foreach (var entry in vertex.VertexEntries) {
                    if (entry == null) {
                        continue;
                    }
                    var color = "green";
                    if (entry.PreviousEntry == mostRecentlyExtendedPath) {
                        newEntries.Add(entry);
                        color = "red";
                    } else if (!entry.IsClosed) {
                        color = "yellow";
                    }
                    pathEdges.Add(new DebugCurve(2, color, new LineSegment(entry.PreviousVertex.Point, vertex.Point)));
                }
            }
            System.Diagnostics.Debug.WriteLine("entry {0} seq = {1} len = {2} nbend = {3} ccost = {4} hcost = {5} iters = {6}/{7}", mostRecentlyExtendedPath,
                             0,
                             mostRecentlyExtendedPath.Length, mostRecentlyExtendedPath.NumberOfBends,
                             this.CombinedCost(mostRecentlyExtendedPath.Length, mostRecentlyExtendedPath.NumberOfBends),
                             this.HeuristicDistanceFromVertexToTarget(mostRecentlyExtendedPath.Vertex.Point, mostRecentlyExtendedPath.Direction),
                             this.currentIterations, 0);
            foreach (var newEntry in newEntries) {
                System.Diagnostics.Debug.WriteLine("   newEntry {0} len = {1} nbend = {2} ccost = {3} hcost = {4}", newEntry,
                                 newEntry.Length, newEntry.NumberOfBends,
                                 this.CombinedCost(newEntry.Length, newEntry.NumberOfBends),
                                 this.HeuristicDistanceFromVertexToTarget(newEntry.Vertex.Point, newEntry.Direction));
            }
            DevTraceDisplay(edges.Concat(q).Concat(so).Concat(pathEdges));
        }

// ReSharper disable UnusedMember.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "len"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "nbend"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "iters"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "so"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "pathEdges"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "edges"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.WriteLine(System.String,System.Object[])"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void TestShowPath(VisibilityVertex source, IEnumerable<Point> pathPoints, double cost, double length, int numberOfBends) {
// ReSharper restore UnusedMember.Local
            var edges = GetAllEdgesTest(source).Select(e => (ICurve) (new LineSegment(e.SourcePoint, e.TargetPoint))).
                        Select(c => new DebugCurve(c));
            var so = new[] {
                              new DebugCurve(1, "brown", new Ellipse(3, 3, source.Point)),
                              new DebugCurve(1, "purple", CurveFactory.CreateDiamond(4, 4, Target.Point)),
                          };

            List<DebugCurve> pathEdges = GetPathEdgeDebugCurves(pathPoints, "green");
            System.Diagnostics.Debug.WriteLine("path {0} -> {1} cost = {2} len = {3} nbend = {4} iters = {5}",
                            source.Point, this.Target.Point, cost, length, numberOfBends, this.currentIterations);
            DevTraceDisplay(edges.Concat(so).Concat(pathEdges));
        }

        internal static List<DebugCurve> GetPathEdgeDebugCurves(IEnumerable<Point> pathPoints, string color) {
            var pathEdges = new List<DebugCurve>();
            if (pathPoints != null) {
                bool first = true;
                var prevPoint = new Point();
                foreach (var point in pathPoints) {
                    if (first) {
                        prevPoint = point;
                        first = false;
                        continue;
                    }
                    pathEdges.Add(new DebugCurve(2, color, new LineSegment(prevPoint, point)));
                    prevPoint = point;
                }
            }
            return pathEdges;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private IEnumerable<VisibilityEdge> GetAllEdgesTest(VisibilityVertex vertex) {
            var processedVertices = new Set<VisibilityVertex>();
            var q = new Queue<VisibilityVertex>();
            q.Enqueue(vertex);

            // Target may not be connected to source.
            bool targetFound = (vertex == this.Target);
            while (!targetFound) {
                while (q.Count > 0) {
                    VisibilityVertex v = q.Dequeue();
                    if (processedVertices.Contains(v)) {
                        continue;
                    }
                    targetFound |= (v == this.Target);
                    processedVertices.Insert(v);
                    foreach (var u in VertsToGo(v).Where(u => !processedVertices.Contains(u))) {
                        q.Enqueue(u);
                    }
                    foreach (VisibilityEdge edge in v.OutEdges) {
                        yield return edge;
                    }
                }
                q.Enqueue(this.Target);
                targetFound = true;
            }
        }

        private static IEnumerable<VisibilityVertex> VertsToGo(VisibilityVertex visibilityVertex) {
            foreach (var edge in visibilityVertex.OutEdges) {
                yield return edge.Target;
            }
            foreach (var edge in visibilityVertex.InEdges) {
                yield return edge.Source;}
        }

#endif // TEST_MSAGL
#endregion // TEST_MSAGL

        private void QueueReversedEntryToNeighborVertexIfNeeded(VertexEntry bestEntry, VertexEntry entryFromNeighbor, double weight) {
            // If we have a lower-cost path from bestEntry to entryFromNeighbor.PreviousVertex than the cost of entryFromNeighbor,
            // or bestEntry has degree 1 (it is a dead-end), enqueue a path in the opposite direction (entryFromNeighbor will probably
            // never be extended from this point).
            int numberOfBends;
            double length;
            var neigVer = entryFromNeighbor.PreviousVertex;
            var dirToNeighbor = GetLengthAndNumberOfBendsToNeighborVertex(bestEntry, neigVer, weight, out numberOfBends, out length);
            if ((CombinedCost(length, numberOfBends) < CombinedCost(entryFromNeighbor.Length, entryFromNeighbor.NumberOfBends))
                    || (bestEntry.Vertex.Degree == 1)) {
                var cost = this.TotalCostFromSourceToVertex(length, numberOfBends) + HeuristicDistanceFromVertexToTarget(neigVer.Point, dirToNeighbor);
                EnqueueEntry(bestEntry, neigVer, length, numberOfBends, cost);
            }
        }
        
        private void UpdateEntryToNeighborVertexIfNeeded(VertexEntry bestEntry, VertexEntry neigEntry, double weight) {
            int numberOfBends;
            double length;
            var dirToNeighbor = GetLengthAndNumberOfBendsToNeighborVertex(bestEntry, neigEntry.Vertex, weight, out numberOfBends, out length);
            if (CombinedCost(length, numberOfBends) < CombinedCost(neigEntry.Length, neigEntry.NumberOfBends)) {
                var newCost = this.TotalCostFromSourceToVertex(length, numberOfBends) + HeuristicDistanceFromVertexToTarget(neigEntry.Vertex.Point, dirToNeighbor);
                neigEntry.ResetEntry(bestEntry, length, numberOfBends, newCost);
                queue.DecreasePriority(neigEntry, newCost);
            }
        }

        private void CreateAndEnqueueEntryToNeighborVertex(VertexEntry bestEntry, VisibilityVertexRectilinear neigVer, double weight) {
            int numberOfBends;
            double length;
            var dirToNeighbor = GetLengthAndNumberOfBendsToNeighborVertex(bestEntry, neigVer, weight, out numberOfBends, out length);
            var cost = this.TotalCostFromSourceToVertex(length, numberOfBends) + HeuristicDistanceFromVertexToTarget(neigVer.Point, dirToNeighbor);
            if (cost < this.upperBoundOnCost) {
                if (neigVer.VertexEntries == null) {
                    this.visitedVertices.Add(neigVer);
                }
                EnqueueEntry(bestEntry, neigVer, length, numberOfBends, cost);
            }
        }

        private void EnqueueEntry(VertexEntry bestEntry, VisibilityVertexRectilinear neigVer, double length, int numberOfBends, double cost) {
            var entry = new VertexEntry(neigVer, bestEntry, length, numberOfBends, cost);
            neigVer.SetVertexEntry(entry);
            this.queue.Enqueue(entry, entry.Cost);
        }

        private static Direction GetLengthAndNumberOfBendsToNeighborVertex(VertexEntry prevEntry, 
                    VisibilityVertex vertex, double weight, out int numberOfBends, out double length) {
            length = prevEntry.Length + ManhattanDistance(prevEntry.Vertex.Point, vertex.Point)*weight;
            Direction directionToVertex = CompassVector.PureDirectionFromPointToPoint(prevEntry.Vertex.Point, vertex.Point);
            numberOfBends = prevEntry.NumberOfBends;
            if (prevEntry.Direction != Direction. None && directionToVertex != prevEntry.Direction) {
                numberOfBends++;
            }
            return directionToVertex;
        }

        internal static double ManhattanDistance(Point a, Point b) {
            return Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y);
        }

        internal VertexEntry GetPathWithCost(VertexEntry[] sourceVertexEntries, VisibilityVertexRectilinear source, double adjustmentToSourceCost,
                                             VertexEntry[] targetVertexEntries, VisibilityVertexRectilinear target, double adjustmentToTargetCost, 
                                             double priorBestCost) {
            this.upperBoundOnCost = priorBestCost;
            this.sourceCostAdjustment = adjustmentToSourceCost;
            this.targetCostAdjustment = adjustmentToTargetCost;

            DevTracePrintSourceAndTarget(source, target);
            if (!InitPath(sourceVertexEntries, source, target)) {
                this.DevTraceShowPath(source, null);
                return null;
            }


            while (queue.Count > 0) {
                this.TestPreDequeue();
                var bestEntry = queue.Dequeue();
                var bestVertex = bestEntry.Vertex;
                if (bestVertex == Target) {
                    this.DevTraceShowPath(source, bestEntry);
                    if (targetVertexEntries == null) {
                        Cleanup();
                        return bestEntry;
                    }

                    // We'll never get a duplicate entry direction here; we either relaxed the cost via UpdateEntryToNeighborIfNeeded
                    // before we dequeued it, or it was closed.  So, we simply remove the direction from the valid target entry directions
                    // and if we get to none, we're done.  We return a null path until the final stage.
#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=368 property assignment not working with &= operator
                    this.EntryDirectionsToTarget = this.EntryDirectionsToTarget & ~bestEntry.Direction;
#else
                    this.EntryDirectionsToTarget &= ~bestEntry.Direction;
#endif
                    if (this.EntryDirectionsToTarget == Direction. None) {
                        this.Target.VertexEntries.CopyTo(targetVertexEntries, 0); 
                        Cleanup();
                        return null;
                    }
                    this.upperBoundOnCost = Math.Min(this.MultistageAdjustedCostBound(bestEntry.Cost), this.upperBoundOnCost);
                    continue;
                }

                // It's safe to close this after removing it from the queue.  Any updateEntryIfNeeded that changes it must come
                // while it is still on the queue; it is removed from the queue only if it has the lowest cost path, and we have
                // no negative path weights, so any other path that might try to extend to it after this cannot have a lower cost.
                bestEntry.IsClosed = true;

                // PerfNote: Array.ForEach is optimized, but don't use .Where.
                foreach (var bendNeighbor in this.nextNeighbors) {
                    bendNeighbor.Clear();
                }
                var preferredBendDir = Right(bestEntry.Direction);
                this.ExtendPathAlongInEdges(bestEntry, bestVertex.InEdges, preferredBendDir);
                this.ExtendPathAlongOutEdges(bestEntry, bestVertex.OutEdges, preferredBendDir);
                foreach (var bendNeighbor in this.nextNeighbors) {
                    if (bendNeighbor.Vertex != null) {
                        this.ExtendPathToNeighborVertex(bestEntry, bendNeighbor.Vertex, bendNeighbor.Weight);
                    }
                }
                this.DevTraceShowAllPartialPaths(source, bestEntry);
            }

            // Either there is no path to the target, or we have abandoned the path due to exceeding priorBestCost.
            if ((targetVertexEntries != null) && (this.Target.VertexEntries != null)) {
                this.Target.VertexEntries.CopyTo(targetVertexEntries, 0);
            }
            this.DevTraceShowPath(source, null);
            Cleanup();
            return null;
        }

        private void ExtendPathAlongInEdges(VertexEntry bestEntry, IEnumerable<VisibilityEdge> edges, Direction preferredBendDir) {
            foreach (var edge in edges) {
                ExtendPathAlongEdge(bestEntry, edge, true, preferredBendDir);
            }
        }

        private void ExtendPathAlongOutEdges(VertexEntry bestEntry, RbTree<VisibilityEdge> edges, Direction preferredBendDir) {
            // Avoid GetEnumerator overhead.
            var outEdgeNode = edges.IsEmpty() ? null : edges.TreeMinimum();
            for (; outEdgeNode != null; outEdgeNode = edges.Next(outEdgeNode)) {
                ExtendPathAlongEdge(bestEntry, outEdgeNode.Item, false, preferredBendDir);
            }
        }

        private void ExtendPathAlongEdge(VertexEntry bestEntry, VisibilityEdge edge, bool isInEdges, Direction preferredBendDir) {
            if (!IsPassable(edge)) {
                return;
            }

            // This is after the initial source vertex so PreviousEntry won't be null.
            var neigVer = (VisibilityVertexRectilinear)(isInEdges ? edge.Source : edge.Target);
            if (neigVer == bestEntry.PreviousVertex) {
                // For multistage paths, the source may be a waypoint outside the graph boundaries that is collinear
                // with both the previous and next points in the path; in that case it may have only one degree.
                // For other cases, we just ignore it and the path will be abandoned.
                if ((bestEntry.Vertex.Degree > 1) || (bestEntry.Vertex != this.Source)) {
                    return;
                }
                this.ExtendPathToNeighborVertex(bestEntry, neigVer, edge.Weight);
                return;
            }

            // Enqueue in reverse order of preference per comments on NextNeighbor class.
            var neigDir = CompassVector.PureDirectionFromPointToPoint(bestEntry.Vertex.Point, neigVer.Point);
            var nextNeighbor = this.nextNeighbors[2];
            if (neigDir != bestEntry.Direction) {
                nextNeighbor = this.nextNeighbors[(neigDir == preferredBendDir) ? 1 : 0];
            }
            Debug.Assert(nextNeighbor.Vertex == null, "bend neighbor already exists");
            nextNeighbor.Set(neigVer, edge.Weight);
        }

        private void EnqueueInitialVerticesFromSource(double cost) {
            var bestEntry = new VertexEntry(this.Source, null, 0, 0, cost) {
                IsClosed = true
            };

            // This routine is only called once so don't worry about optimizing foreach.where
            foreach (var edge in this.Source.OutEdges.Where(IsPassable)) {
                this.ExtendPathToNeighborVertex(bestEntry, (VisibilityVertexRectilinear)edge.Target, edge.Weight);
            }
            foreach (var edge in this.Source.InEdges.Where(IsPassable)) {
                this.ExtendPathToNeighborVertex(bestEntry, (VisibilityVertexRectilinear)edge.Source, edge.Weight);
            }
        }

        private void EnqueueInitialVerticesFromSourceEntries(VertexEntry[] sourceEntries) {
            foreach (var entry in sourceEntries) {
                if (entry != null) {
                    this.queue.Enqueue(entry, entry.Cost);
                }
            }
        }

        private void ExtendPathToNeighborVertex(VertexEntry bestEntry, VisibilityVertexRectilinear neigVer, double weight) {
            var dirToNeighbor = CompassVector.PureDirectionFromPointToPoint(bestEntry.Vertex.Point, neigVer.Point);

            var neigEntry = (neigVer.VertexEntries != null) ? neigVer.VertexEntries[CompassVector.ToIndex(dirToNeighbor)] : null;
            if (neigEntry == null) {
                if (!this.CreateAndEnqueueReversedEntryToNeighborVertex(bestEntry, neigVer, weight)) {
                    this.CreateAndEnqueueEntryToNeighborVertex(bestEntry, neigVer, weight);
                }
            } else if (!neigEntry.IsClosed) {
                this.UpdateEntryToNeighborVertexIfNeeded(bestEntry, neigEntry, weight);
            }
        }

        private bool CreateAndEnqueueReversedEntryToNeighborVertex(VertexEntry bestEntry, VisibilityVertexRectilinear neigVer, double weight) {
            // VertexEntries is null for the initial source. Otherwise, if there is already a path into bestEntry's vertex
            // from neigVer, we're turning back on the path; therefore we have already enqueued the neighbors of neigVer.
            // However, the path cost includes both path length to the current point and the lookahead; this means that we
            // may now be coming into the neigVer from the opposite side with an equal score to the previous entry, but
            // the new path may be going toward the target while the old one (from neigVer to bestEntry) went away from
            // the target.  So, if we score better going in the opposite direction, enqueue bestEntry->neigVer; ignore
            // neigVer->bestEntry as it probably won't be extended again.
            if (bestEntry.Vertex.VertexEntries != null) {
                var dirFromNeighbor = CompassVector.PureDirectionFromPointToPoint(neigVer.Point, bestEntry.Vertex.Point);
                var entryFromNeighbor = bestEntry.Vertex.VertexEntries[CompassVector.ToIndex(dirFromNeighbor)];
                if (entryFromNeighbor != null) {
                    Debug.Assert(entryFromNeighbor.PreviousVertex == neigVer, "mismatch in turnback PreviousEntry");
                    Debug.Assert(entryFromNeighbor.PreviousEntry.IsClosed, "turnback PreviousEntry should be closed");
                    this.QueueReversedEntryToNeighborVertexIfNeeded(bestEntry, entryFromNeighbor, weight);
                    return true;
                }
            }
            return false;
        }

        private static bool IsPassable(VisibilityEdge edge) {
            return edge.IsPassable == null || edge.IsPassable();
        }

        private void Cleanup()
        {
            foreach (var v in this.visitedVertices) {
                v.RemoveVertexEntries();
            }
            this.visitedVertices.Clear();
            this.queue = null;
            this.TestClearIterations();
        }

#region DevTrace
#if DEVTRACE
        private static readonly DevTrace ssstTrace = new DevTrace("Rectilinear_SsstTrace", "Ssst");
        private static readonly DevTrace ssstDisplay = new DevTrace("Rectilinear_SsstDisplay");
#endif // DEVTRACE

        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        void DevTraceShowAllPartialPaths(VisibilityVertex source, VertexEntry mostRecentlyExtendedPath) { 
#if DEVTRACE
            if (ssstTrace.IsLevel(3))
            {
                this.TestShowAllPaths(source, mostRecentlyExtendedPath);
            }
#endif // DEVTRACE
        }

        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private void DevTraceShowPath(VisibilityVertex source, VertexEntry lastEntry) {
#if DEVTRACE
            if (ssstTrace.IsLevel(1)) {
                var pathPoints = RestorePath(lastEntry);
                if (pathPoints != null) {
                    this.TestShowPath(source, pathPoints, lastEntry.Cost, lastEntry.Length, lastEntry.NumberOfBends);
                    return;
                }
                System.Diagnostics.Debug.WriteLine("path abandoned; iters = {0}/{1}", this.currentIterations, totalIterations);
            }
#endif // DEVTRACE
        }

        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private void DevTracePrintSourceAndTarget(VisibilityVertex source, VisibilityVertex target) {
#if DEVTRACE
            if (ssstTrace.IsLevel(2))
            {
                // Don't use this.Target as it hasn't been set yet.
                System.Diagnostics.Debug.WriteLine("find path: {0} -> {1} distance {2} sourceAdjust {3} targetAdjust {4} bendPenalty {5}",
                            source.Point, target.Point, ManhattanDistance(source.Point, target.Point),
                            this.sourceCostAdjustment, this.targetCostAdjustment, this.BendsImportance);
            }
#endif // DEVTRACE
        }

#if TEST_MSAGL
        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private static void DevTraceDisplay(IEnumerable<DebugCurve> debugCurves)
        {
#if DEVTRACE
            if (!ssstDisplay.IsLevel(1))
            {
                return;
            }
#endif // DEVTRACE
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(debugCurves);
        }
#endif // TEST_MSAGL

#if TEST_MSAGL
        private int currentIterations;
        
#endif // TEST_MSAGL

        [Conditional("TEST_MSAGL")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private void TestPreDequeue() {
        }

        [Conditional("TEST_MSAGL")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private void TestClearIterations() {
#if TEST_MSAGL
            currentIterations = 0;
#endif // TEST_MSAGL
        }

#endregion // DevTrace
    }
}
