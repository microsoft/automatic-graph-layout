//
// TransientGraphUtility.cs
// MSAGL class to manage transient vertices and edges for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
    using DebugHelpers;

    internal class TransientGraphUtility {
        // Vertices added to the graph for routing.
        internal List<VisibilityVertexRectilinear> AddedVertices = new List<VisibilityVertexRectilinear>();

        // Edges added to the graph for routing.
        internal List<TollFreeVisibilityEdge> AddedEdges = new List<TollFreeVisibilityEdge>();

        // Edges joining two non-transient vertices; these must be replaced.
        readonly List<VisibilityEdge> edgesToRestore = new List<VisibilityEdge>();

        internal bool LimitPortVisibilitySpliceToEndpointBoundingBox { get; set; }

        // Owned by creator of this class.
        VisibilityGraphGenerator GraphGenerator { get; set; }
        internal ObstacleTree ObstacleTree { get { return GraphGenerator.ObsTree; } }
        internal VisibilityGraph VisGraph { get { return GraphGenerator.VisibilityGraph; } }
        private bool IsSparseVg { get { return this.GraphGenerator is SparseVisibilityGraphGenerator; } }

        internal TransientGraphUtility(VisibilityGraphGenerator graphGen) {
            GraphGenerator = graphGen;
        }

        internal VisibilityVertex AddVertex(Point location) {
            var vertex = this.VisGraph.AddVertex(location);
            AddedVertices.Add((VisibilityVertexRectilinear)vertex);
            return vertex;
        }

        internal VisibilityVertex FindOrAddVertex(Point location) {
            var vertex = VisGraph.FindVertex(location);
            return vertex ?? AddVertex(location);
        }

        internal VisibilityEdge FindOrAddEdge(VisibilityVertex sourceVertex, VisibilityVertex targetVertex)
        {
            return FindOrAddEdge(sourceVertex, targetVertex, ScanSegment.NormalWeight);
        }

        internal VisibilityEdge FindOrAddEdge(VisibilityVertex sourceVertex, VisibilityVertex targetVertex, double weight) {
            // Since we're adding transient edges into the graph, we're not doing full intersection
            // evaluation; thus there may already be an edge from the source vertex in the direction
            // of the target vertex, but ending before or after the target vertex.
            Direction dirToTarget = PointComparer.GetPureDirection(sourceVertex, targetVertex);
            VisibilityVertex bracketSource, bracketTarget, splitVertex;
            GetBrackets(sourceVertex, targetVertex, dirToTarget, out bracketSource, out bracketTarget, out splitVertex);

            // If null != edge then targetVertex is between bracketSource and bracketTarget and SplitEdge returns the 
            // first half-edge (and weight is ignored as the split uses the edge weight).
            var edge = VisGraph.FindEdge(bracketSource.Point, bracketTarget.Point);
            edge = (edge != null)
                    ? this.SplitEdge(edge, splitVertex)
                    : CreateEdge(bracketSource, bracketTarget, weight);
            DevTrace_VerifyEdge(edge);
            return edge;
        }

        private static void GetBrackets(VisibilityVertex sourceVertex, VisibilityVertex targetVertex, Direction dirToTarget, out VisibilityVertex bracketSource, out VisibilityVertex bracketTarget, out VisibilityVertex splitVertex) {

            // Is there an edge in the chain from sourceVertex in the direction of targetVertex
            // that brackets targetvertex?
            //      <sourceVertex> -> ..1.. -> ..2.. <end>   3
            // Yes if targetVertex is at the x above 1 or 2, No if it is at 3.  If false, bracketSource
            // will be set to the vertex at <end> (if there are any edges in that direction at all).
            splitVertex = targetVertex;
            if (!FindBracketingVertices(sourceVertex, targetVertex.Point, dirToTarget
, out bracketSource, out bracketTarget)) {
                // No bracketing of targetVertex from sourceVertex but bracketSource has been updated.
                // Is there a bracket of bracketSource from the targetVertex direction?
                //                      3   <end> ..2.. <- ..1..   <targetVertex>
                // Yes if bracketSource is at the x above 1 or 2, No if it is at 3.  If false, bracketTarget
                // will be set to the vertex at <end> (if there are any edges in that direction at all).
                // If true, then bracketSource and splitVertex must be updated.
                VisibilityVertex tempSource;
                if (FindBracketingVertices(targetVertex, sourceVertex.Point, CompassVector.OppositeDir(dirToTarget)
                                        , out bracketTarget, out tempSource)) {
                    Debug.Assert(bracketSource == sourceVertex, "Mismatched bracketing detection");
                    bracketSource = tempSource;
                    splitVertex = sourceVertex;
                }
            }
        }

        internal static bool FindBracketingVertices(VisibilityVertex sourceVertex, Point targetPoint, Direction dirToTarget
                                    , out VisibilityVertex bracketSource, out VisibilityVertex bracketTarget) {
            // Walk from the source to target until we bracket target or there is no nextVertex
            // in the desired direction.
            bracketSource = sourceVertex;
            for (; ; ) {
                bracketTarget = StaticGraphUtility.FindAdjacentVertex(bracketSource, dirToTarget);
                if ( bracketTarget == null) {
                    break;
                }
                if (PointComparer.Equal(bracketTarget.Point, targetPoint)) {
                    // Desired edge already exists.
                    return true;
                }
                if (dirToTarget != PointComparer.GetPureDirection(bracketTarget.Point, targetPoint)) {
                    // bracketTarget is past vertex in the traversal direction.
                    break;
                }
                bracketSource = bracketTarget;
            }
            return  bracketTarget != null;
        }

        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
// ReSharper disable InconsistentNaming
        void DevTrace_VerifyEdge(VisibilityEdge edge) {
#if DEVTRACE
            if (transGraphVerify.IsLevel(1)) {
                Debug.Assert(PointComparer.IsPureLower(edge.SourcePoint, edge.TargetPoint), "non-ascending edge");

                // For A -> B -> C, make sure there is no A -> C, and vice-versa.  Simplest way to do
                // this is to ensure that there is only one edge in any given direction for each vertex.
                DevTrace_VerifyVertex(edge.Source);
                DevTrace_VerifyVertex(edge.Target);
            }
#endif // DEVTRACE
        }

#if DEVTRACE
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        void DevTrace_VerifyVertex(VisibilityVertex vertex) {
            if (transGraphVerify.IsLevel(1)) {
                Directions dir = Directions.North;
                for (int idir = 0; idir < 4; ++idir, dir = CompassVector.RotateRight(dir)) {
                    int count = 0;
                    int cEdges = vertex.InEdges.Count;      // indexing is faster than foreach for Lists
                    for (int ii = 0; ii < cEdges; ++ii) {
                        var edge = vertex.InEdges[ii];
                        if (PointComparer.GetPureDirection(vertex.Point, edge.SourcePoint) == dir) {
                            ++count;
                        }
                    }

                    // Avoid GetEnumerator overhead.
                    var outEdgeNode = vertex.OutEdges.IsEmpty() ? null : vertex.OutEdges.TreeMinimum();
                    for (; outEdgeNode != null; outEdgeNode = vertex.OutEdges.Next(outEdgeNode)) {
                        var edge = outEdgeNode.Item;
                        if (PointComparer.GetPureDirection(vertex.Point, edge.TargetPoint) == dir) {
                            ++count;
                        }
                    }
                    Debug.Assert(count < 2, "vertex has multiple edges in one direction");
                }
            }
        }
#endif // DEVTRACE

        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        internal void DevTrace_VerifyAllVertices(VisibilityGraph vg) {
#if DEVTRACE
            if (transGraphVerify.IsLevel(4)) {
                foreach (var vertex in vg.Vertices()) {
                    DevTrace_VerifyVertex(vertex);
                }
            }
#endif // DEVTRACE
        }

        [Conditional("DEVTRACE")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        internal void DevTrace_VerifyAllEdgeIntersections(VisibilityGraph visibilityGraph) {
#if DEVTRACE
            // By design, edges cross at non-vertices in SparseVg.
            if (this.IsSparseVg) {
                return;
            }
            if (transGraphVerify.IsLevel(4)) {
                var edges = visibilityGraph.Edges.ToArray();
                for (int i = 0; i < edges.Length; i++) {
                    var iEdge = edges[i];
                    for (int j = i + 1; j < edges.Length; j++) {
                        var jEdge = edges[j];
                        Point x;
                        if (LineSegment.Intersect(iEdge.SourcePoint, iEdge.TargetPoint, jEdge.SourcePoint, jEdge.TargetPoint, out x)) {
                            if (!DevTrace_IsEndOfEdge(iEdge, x) && !DevTrace_IsEndOfEdge(jEdge, x)) {
                                Debug.Assert(false, "VisibilityEdges cross at non-endpoints");
                            }
                        }
                    }
                }
            }
#endif // DEVTRACE
        }

#if DEVTRACE
        static bool DevTrace_IsEndOfEdge(VisibilityEdge e, Point x) {
            return PointComparer.Equal(e.SourcePoint, x) || PointComparer.Equal(e.TargetPoint, x);
        }
#endif // DEVTRACE
        // ReSharper restore InconsistentNaming

        private VisibilityEdge CreateEdge(VisibilityVertex first, VisibilityVertex second, double weight)
        {
            // All edges in the graph are ascending.
            VisibilityVertex source = first;
            VisibilityVertex target = second;
            if (!PointComparer.IsPureLower(source.Point, target.Point)) {
                source = second;
                target = first;
            }

            var edge = new TollFreeVisibilityEdge(source, target, weight);
            VisibilityGraph.AddEdge(edge);
            this.AddedEdges.Add(edge);
            return edge;
        }

        internal void RemoveFromGraph() {
            RemoveAddedVertices();
            RemoveAddedEdges();
            RestoreRemovedEdges();
        }

        private void RemoveAddedVertices() {
            foreach (var vertex in this.AddedVertices) {
                // Removing all transient vertices will remove all associated transient edges as well.
                if ( this.VisGraph.FindVertex(vertex.Point) != null)
                {
                    this.VisGraph.RemoveVertex(vertex);
                }
            }
            this.AddedVertices.Clear();
        }

        private void RemoveAddedEdges() {
            foreach (var edge in this.AddedEdges) {
                // If either vertex was removed, so was the edge, so just check source.
                if ( this.VisGraph.FindVertex(edge.SourcePoint) != null) {
                    VisibilityGraph.RemoveEdge(edge);
                }
            }
            this.AddedEdges.Clear();
        }

        private void RestoreRemovedEdges() {
            foreach (var edge in this.edgesToRestore) {
                // We should only put TransientVisibilityEdges in this list, and should never encounter
                // a non-transient edge in the graph after we've replaced it with a transient one, so 
                // the edge should not be in the graph until we re-insert it.
                Debug.Assert(!(edge is TollFreeVisibilityEdge), "Unexpected Transient edge");
                VisibilityGraph.AddEdge(edge);
                this.DevTrace_VerifyEdge(edge);
            }
            this.edgesToRestore.Clear();
        }

        internal VisibilityEdge FindNextEdge(VisibilityVertex vertex, Direction dir) {
            return StaticGraphUtility.FindAdjacentEdge(vertex, dir);
        }

        internal VisibilityEdge FindPerpendicularOrContainingEdge(VisibilityVertex startVertex
                            , Direction dir, Point pointLocation) {
            // Return the edge in 'dir' from startVertex that is perpendicular to pointLocation.
            // startVertex must therefore be located such that pointLocation is in 'dir' direction from it,
            // or is on the same line.
            StaticGraphUtility.Assert(0 == (CompassVector.OppositeDir(dir)
                                & PointComparer.GetDirections(startVertex.Point, pointLocation))
                         , "the ray from 'dir' is away from pointLocation", ObstacleTree, VisGraph);
            while (true) {
                VisibilityVertex nextVertex = StaticGraphUtility.FindAdjacentVertex(startVertex, dir);
                if (nextVertex == null) {
                    break;
                }
                Direction dirCheck = PointComparer.GetDirections(nextVertex.Point, pointLocation);
                
                // If the next vertex is past the intersection with pointLocation, this edge brackets it.
                if (0 != (CompassVector.OppositeDir(dir) & dirCheck)) {
                    return VisGraph.FindEdge(startVertex.Point, nextVertex.Point);
                }
                startVertex = nextVertex;
            }
            return null;
        }

        internal VisibilityEdge FindNearestPerpendicularOrContainingEdge(VisibilityVertex startVertex
                            , Direction dir, Point pointLocation) {
            // Similar to FindPerpendicularEdge, but first try to move closer to pointLocation,
            // as long as there are edges going in 'dir' that extend to pointLocation.
            Direction dirTowardLocation = ~dir & PointComparer.GetDirections(startVertex.Point, pointLocation);
            
            // If Directions. None then pointLocation is collinear.
            VisibilityVertex currentVertex = startVertex;
            Direction currentDirTowardLocation = dirTowardLocation;

            // First move toward pointLocation far as we can.
            while (Direction. None != currentDirTowardLocation) {
                VisibilityVertex nextVertex = StaticGraphUtility.FindAdjacentVertex(currentVertex, dirTowardLocation);
                if (nextVertex == null) {
                    break;
                }
                if (0 != (CompassVector.OppositeDir(dirTowardLocation)
                            & PointComparer.GetDirections(nextVertex.Point, pointLocation))) {
                    break;
                }
                currentVertex = nextVertex;
                currentDirTowardLocation = ~dir & PointComparer.GetDirections(currentVertex.Point, pointLocation);
            }
 
            // Now find the first vertex that has a chain that intersects pointLocation, if any, moving away
            // from pointLocation until we find it or arrive back at startVertex.
            VisibilityEdge perpEdge;
            while (true) {
                perpEdge = FindPerpendicularOrContainingEdge(currentVertex, dir, pointLocation);
                if (( perpEdge != null) || (currentVertex == startVertex)) {
                    break;
                }
                currentVertex = StaticGraphUtility.FindAdjacentVertex(currentVertex, CompassVector.OppositeDir(dirTowardLocation));
            }
            return perpEdge;
        }

        internal void ConnectVertexToTargetVertex(VisibilityVertex sourceVertex, VisibilityVertex targetVertex, Direction finalEdgeDir, double weight) {
            // finalDir is the required direction of the final edge to the targetIntersect
            // (there will be two edges if we have to add a bend vertex).
            StaticGraphUtility.Assert(PointComparer.IsPureDirection(finalEdgeDir), "finalEdgeDir is not pure", ObstacleTree, VisGraph);

            // targetIntersect may be CenterVertex if that is on an extreme bend or a flat border.
            if (PointComparer.Equal(sourceVertex.Point, targetVertex.Point)) {
                return;
            }

            // If the target is collinear with sourceVertex we can just create one edge to it.
            Direction targetDirs = PointComparer.GetDirections(sourceVertex.Point, targetVertex.Point);
            if (PointComparer.IsPureDirection(targetDirs)) {
                FindOrAddEdge(sourceVertex, targetVertex);
                return;
            }

            // Not collinear so we need to create a bend vertex and edge if they don't yet exist.
            Point bendPoint = StaticGraphUtility.FindBendPointBetween(sourceVertex.Point, targetVertex.Point, finalEdgeDir);
            VisibilityVertex bendVertex = FindOrAddVertex(bendPoint);
            FindOrAddEdge(sourceVertex, bendVertex, weight);

            // Now create the outer target vertex if it doesn't exist.
            FindOrAddEdge(bendVertex, targetVertex, weight);
        }

        internal VisibilityVertex AddEdgeToTargetEdge(VisibilityVertex sourceVertex, VisibilityEdge targetEdge
                                        , Point targetIntersect) {
            StaticGraphUtility.Assert(PointComparer.Equal(sourceVertex.Point, targetIntersect)
                      || PointComparer.IsPureDirection(sourceVertex.Point, targetIntersect)
                      , "non-orthogonal edge request", ObstacleTree, VisGraph);
            StaticGraphUtility.Assert(StaticGraphUtility.PointIsOnSegment(targetEdge.SourcePoint, targetEdge.TargetPoint, targetIntersect)
                      , "targetIntersect is not on targetEdge", ObstacleTree, VisGraph);

            // If the target vertex does not exist, we must split targetEdge to add it.
            VisibilityVertex targetVertex = VisGraph.FindVertex(targetIntersect);
            if (targetVertex == null) {
                targetVertex = AddVertex(targetIntersect);
                SplitEdge(targetEdge, targetVertex);
            }
            FindOrAddEdge(sourceVertex, targetVertex);
            return targetVertex;
        }

        internal VisibilityEdge SplitEdge(VisibilityEdge edge, VisibilityVertex splitVertex) {
            // If the edge is NULL it means we could not find an appropriate one, so do nothing.
            if (edge == null) {
                return null;
            }
            StaticGraphUtility.Assert(StaticGraphUtility.PointIsOnSegment(edge.SourcePoint, edge.TargetPoint, splitVertex.Point)
                        , "splitVertex is not on edge", ObstacleTree, VisGraph);
            if (PointComparer.Equal(edge.Source.Point, splitVertex.Point) || PointComparer.Equal(edge.Target.Point, splitVertex.Point)) {
                // No split needed.
                return edge;
            }

            // Store the original edge, if needed.
            if (!(edge is TollFreeVisibilityEdge)) {
                edgesToRestore.Add(edge);
            }

            VisibilityGraph.RemoveEdge(edge);

            // If this is an overlapped edge, or we're in sparseVg, then it may be an unpadded->padded edge that crosses
            // over another obstacle's padded boundary, and then either a collinear splice from a free point or another
            // obstacle in the same cluster starts splicing from that leapfrogged boundary, so we have the edges:
            //      A   ->   D                      | D is unpadded, A is padded border of sourceObstacle
            //        B -> C  ->  E  ->  F          | B and C are vertical ScanSegments between A and D
            //      <-- splice direction is West    | F is unpadded, E is padded border of targetObstacle
            // Now after splicing F to E to C to B we go A, calling FindOrAddEdge B->A; the bracketing process finds
            // A->D which we'll be splitting at B, which would wind up with A->B, B->C, B->D, having to Eastward
            // outEdges from B.  See RectilinearTests.Reflection_Block1_Big_UseRect for overlapped, and 
            // RectilinearTests.FreePortLocationRelativeToTransientVisibilityEdgesSparseVg for sparseVg.
            // To avoid this we add the edges in each direction from splitVertex with FindOrAddEdge.  If we've
            // come here from a previous call to FindOrAddEdge, then that call has found the bracketing vertices, 
            // which are the endpoints of 'edge', and we've removed 'edge', so we will not call SplitEdge again.
            if ((this.IsSparseVg || (edge.Weight == ScanSegment.OverlappedWeight)) && (splitVertex.Degree > 0)) {
                FindOrAddEdge(splitVertex, edge.Source, edge.Weight);
                return FindOrAddEdge(splitVertex, edge.Target, edge.Weight);
            }

            // Splice it into the graph in place of targetEdge.  Return the first half, because
            // this may be called from AddEdge, in which case the split vertex is the target vertex.
            CreateEdge(splitVertex, edge.Target, edge.Weight);
            return CreateEdge(edge.Source, splitVertex, edge.Weight);
        }

        internal void ExtendEdgeChain(VisibilityVertex startVertex, Rectangle limitRect, LineSegment maxVisibilitySegment,
                                    PointAndCrossingsList pacList, bool isOverlapped) {
            var dir = PointComparer.GetDirections(maxVisibilitySegment.Start, maxVisibilitySegment.End);
            if (dir == Direction. None) {
                return;
            }
            Debug.Assert(CompassVector.IsPureDirection(dir), "impure max visibility segment");
            
            // Shoot the edge chain out to the shorter of max visibility or intersection with the limitrect.
            StaticGraphUtility.Assert(PointComparer.Equal(maxVisibilitySegment.Start, startVertex.Point)
                                    || (PointComparer.GetPureDirection(maxVisibilitySegment.Start, startVertex.Point) == dir)
                                    , "Inconsistent direction found", ObstacleTree, VisGraph);
            double oppositeFarBound = StaticGraphUtility.GetRectangleBound(limitRect, dir);
            Point maxDesiredSplicePoint = StaticGraphUtility.IsVertical(dir) 
                                    ? ApproximateComparer.Round(new Point(startVertex.Point.X, oppositeFarBound))
                                    : ApproximateComparer.Round(new Point(oppositeFarBound, startVertex.Point.Y));
            if (PointComparer.Equal(maxDesiredSplicePoint, startVertex.Point)) {
                // Nothing to do.
                return;
            }
            if (PointComparer.GetPureDirection(startVertex.Point, maxDesiredSplicePoint) != dir) {
                // It's in the opposite direction, so no need to do anything.
                return;
            }

            // If maxDesiredSplicePoint is shorter, create a new shorter segment.  We have to pass both segments
            // through to the worker function so it knows whether it can go past maxDesiredSegment (which may be limited
            // by limitRect).
            var maxDesiredSegment = maxVisibilitySegment;
            if (PointComparer.GetDirections(maxDesiredSplicePoint, maxDesiredSegment.End) == dir) {
                maxDesiredSegment = new LineSegment(maxDesiredSegment.Start, maxDesiredSplicePoint);
            }

            ExtendEdgeChain(startVertex, dir, maxDesiredSegment, maxVisibilitySegment, pacList, isOverlapped);
        }

        private void ExtendEdgeChain(VisibilityVertex startVertex, Direction extendDir
                                    , LineSegment maxDesiredSegment, LineSegment maxVisibilitySegment
                                    , PointAndCrossingsList pacList, bool isOverlapped) {
            StaticGraphUtility.Assert(PointComparer.GetPureDirection(maxDesiredSegment.Start, maxDesiredSegment.End) == extendDir
                        , "maxDesiredSegment is reversed", ObstacleTree, VisGraph);

            // Direction*s*, because it may return None, which is valid and means startVertex is on the
            // border of an obstacle and we don't want to go inside it.
            Direction segmentDir = PointComparer.GetDirections(startVertex.Point, maxDesiredSegment.End);
            if (segmentDir != extendDir) {
                // OppositeDir may happen on overlaps where the boundary has a gap in its ScanSegments due to other obstacles
                // overlapping it and each other.  This works because the port has an edge connected to startVertex,
                // which is on a ScanSegment outside the obstacle.
                StaticGraphUtility.Assert(isOverlapped || (segmentDir != CompassVector.OppositeDir(extendDir))
                        , "obstacle encountered between prevPoint and startVertex", ObstacleTree, VisGraph);
                return;
            }

            // We'll find the segment to the left (or right if to the left doesn't exist),
            // then splice across in the opposite direction.
            Direction spliceSourceDir = CompassVector.RotateLeft(extendDir);
            VisibilityVertex spliceSource = StaticGraphUtility.FindAdjacentVertex(startVertex, spliceSourceDir);
            if (spliceSource == null) {
                spliceSourceDir = CompassVector.OppositeDir(spliceSourceDir);
                spliceSource = StaticGraphUtility.FindAdjacentVertex(startVertex, spliceSourceDir);
                if (spliceSource == null) {
                    return;
                }
            }

            // Store this off before ExtendSpliceWorker, which overwrites it.
            Direction spliceTargetDir = CompassVector.OppositeDir(spliceSourceDir);
            VisibilityVertex spliceTarget;
            if (ExtendSpliceWorker(spliceSource, extendDir, spliceTargetDir, maxDesiredSegment, maxVisibilitySegment, isOverlapped, out spliceTarget)) {
                // We ended on the source side and may have dead-ends on the target side so reverse sides.
                ExtendSpliceWorker(spliceTarget, extendDir, spliceSourceDir, maxDesiredSegment, maxVisibilitySegment, isOverlapped, out spliceTarget);
            }

            SpliceGroupBoundaryCrossings(pacList, startVertex, maxDesiredSegment);
        }

        private void SpliceGroupBoundaryCrossings(PointAndCrossingsList crossingList, VisibilityVertex startVertex, LineSegment maxSegment) {
            if ((crossingList == null) || (0 == crossingList.Count)) {
                return;
            }
            crossingList.Reset();
            var start = maxSegment.Start;
            var end = maxSegment.End;
            var dir = PointComparer.GetPureDirection(start, end);

            // Make sure we are going in the ascending direction.
            if (!StaticGraphUtility.IsAscending(dir)) {
                start = maxSegment.End;
                end = maxSegment.Start;
                dir = CompassVector.OppositeDir(dir);
            } 

            // We need to back up to handle group crossings that are between a VisibilityBorderIntersect on a sloped border and the
            // incoming startVertex (which is on the first ScanSegment in Perpendicular(dir) that is outside that padded border).
            startVertex = TraverseToFirstVertexAtOrAbove(startVertex, start, CompassVector.OppositeDir(dir));

            // Splice into the Vertices between and including the start/end points.
            for (var currentVertex = startVertex;  currentVertex != null; currentVertex = StaticGraphUtility.FindAdjacentVertex(currentVertex, dir)) {
                bool isFinalVertex = (PointComparer.Compare(currentVertex.Point, end) >= 0);
                while (crossingList.CurrentIsBeforeOrAt(currentVertex.Point)) {
                    PointAndCrossings pac = crossingList.Pop();

                    // If it's past the start and at or before the end, splice in the crossings in the descending direction.
                    if (PointComparer.Compare(pac.Location, startVertex.Point) > 0) {
                        if (PointComparer.Compare(pac.Location, end) <= 0) {
                            SpliceGroupBoundaryCrossing(currentVertex, pac, CompassVector.OppositeDir(dir));
                        }
                    }

                    // If it's at or past the start and before the end, splice in the crossings in the descending direction.
                    if (PointComparer.Compare(pac.Location, startVertex.Point) >= 0)
                    {
                        if (PointComparer.Compare(pac.Location, end) < 0) {
                            SpliceGroupBoundaryCrossing(currentVertex, pac, dir);
                        }
                    }
                }

                if (isFinalVertex) {
                    break;
                }
            }
        }

        private static VisibilityVertex TraverseToFirstVertexAtOrAbove(VisibilityVertex startVertex, Point start, Direction dir) {
            var returnVertex = startVertex;
            var oppositeDir = CompassVector.OppositeDir(dir);

            for ( ; ; ) {
                var nextVertex = StaticGraphUtility.FindAdjacentVertex(returnVertex, dir);

                // This returns Directions. None on a match.
                if ((nextVertex == null) || (PointComparer.GetDirections(nextVertex.Point, start) == oppositeDir)) {
                    break;
                }
                returnVertex = nextVertex;
            }
            return returnVertex;
        }

        private void SpliceGroupBoundaryCrossing(VisibilityVertex currentVertex, PointAndCrossings pac, Direction dirToInside) {
            GroupBoundaryCrossing[] crossings = PointAndCrossingsList.ToCrossingArray(pac.Crossings, dirToInside);
            if ( crossings != null) {
                var outerVertex = VisGraph.FindVertex(pac.Location) ?? AddVertex(pac.Location);
                if (currentVertex.Point != outerVertex.Point) {
                    FindOrAddEdge(currentVertex, outerVertex);
                }
                var interiorPoint = crossings[0].GetInteriorVertexPoint(pac.Location);
                var interiorVertex = VisGraph.FindVertex(interiorPoint) ?? AddVertex(interiorPoint);
                
                // FindOrAddEdge splits an existing edge so may not return the portion bracketed by outerVertex and interiorVertex.
                FindOrAddEdge(outerVertex, interiorVertex);
                var edge = VisGraph.FindEdge(outerVertex.Point, interiorVertex.Point);
                var crossingsArray = crossings.Select(c => c.Group.InputShape).ToArray();
                edge.IsPassable = delegate { return crossingsArray.Any(s => s.IsTransparent); };
            }
        }

        // The return value is whether we should try a second pass if this is called on the first pass,
        // using spliceTarget to wrap up dead-ends on the target side.
        bool ExtendSpliceWorker(VisibilityVertex spliceSource, Direction extendDir, Direction spliceTargetDir
                              , LineSegment maxDesiredSegment, LineSegment maxVisibilitySegment
                              , bool isOverlapped, out VisibilityVertex spliceTarget) {
            // This is called after having created at least one extension vertex (initially, the
            // first one added outside the obstacle), so we know extendVertex will be there. spliceSource
            // is the vertex to the OppositeDir(spliceTargetDir) of that extendVertex.
            VisibilityVertex extendVertex = StaticGraphUtility.FindAdjacentVertex(spliceSource, spliceTargetDir);
            spliceTarget = StaticGraphUtility.FindAdjacentVertex(extendVertex, spliceTargetDir);
            for (; ; ) {
                if (!GetNextSpliceSource(ref spliceSource, spliceTargetDir, extendDir)) {
                    break;
                }

                // spliceSource is now on the correct edge relative to the desired nextExtendPoint.
                // spliceTarget is in the opposite direction of the extension-line-to-spliceSource.
                Point nextExtendPoint = StaticGraphUtility.FindBendPointBetween(extendVertex.Point
                            , spliceSource.Point, CompassVector.OppositeDir(spliceTargetDir));

                // We test below for being on or past maxDesiredSegment; here we may be skipping 
                // over maxDesiredSegmentEnd which is valid since we want to be sure to go to or
                // past limitRect, but be sure to stay within maxVisibilitySegment.
                if (IsPointPastSegmentEnd(maxVisibilitySegment, nextExtendPoint)) {
                    break;
                }

                spliceTarget = GetSpliceTarget(ref spliceSource, spliceTargetDir, nextExtendPoint);

                //StaticGraphUtility.Test_DumpVisibilityGraph(ObstacleTree, VisGraph);

                if (spliceTarget == null) {
                    // This may be because spliceSource was created just for Group boundaries.  If so,
                    // skip to the next nextExtendVertex location.
                    if (this.IsSkippableSpliceSourceWithNullSpliceTarget(spliceSource, extendDir)) {
                        continue;
                    }

                    // We're at a dead-end extending from the source side, or there is an intervening obstacle, or both.
                    // Don't splice across lateral group boundaries.
                    if (ObstacleTree.SegmentCrossesAnObstacle(spliceSource.Point, nextExtendPoint)) {
                        return false;
                    }
                }

                // We might be walking through a point where a previous chain dead-ended.
                VisibilityVertex nextExtendVertex = VisGraph.FindVertex(nextExtendPoint);
                if ( nextExtendVertex != null) {
                    if ((spliceTarget == null) || ( this.VisGraph.FindEdge(extendVertex.Point, nextExtendPoint) != null)) {
                        // We are probably along a ScanSegment so visibility in this direction has already been determined.
                        // Stop and don't try to continue extension from the opposite side.  If we continue splicing here
                        // it might go across an obstacle.
                        if (spliceTarget == null) {
                            Debug_VerifyNonOverlappedExtension(isOverlapped, extendVertex, nextExtendVertex, spliceSource:null, spliceTarget:null);
                            FindOrAddEdge(extendVertex, nextExtendVertex, isOverlapped ? ScanSegment.OverlappedWeight : ScanSegment.NormalWeight);
                        }
                        return false;
                    }

                    // This should always have been found in the find-the-next-target loop above if there is
                    // a vertex (which would be nextExtendVertex, which we just found) between spliceSource
                    // and spliceTarget.  Even for a sparse graph, an edge should not skip over a vertex.
                    StaticGraphUtility.Assert(spliceTarget == StaticGraphUtility.FindAdjacentVertex(nextExtendVertex, spliceTargetDir)
                               , "no edge exists between an existing nextExtendVertex and spliceTarget"
                               , ObstacleTree, VisGraph);
                }
                else {
                    StaticGraphUtility.Assert((spliceTarget == null)
                                || spliceTargetDir == PointComparer.GetPureDirection(nextExtendPoint, spliceTarget.Point)
                               , "spliceTarget is not to spliceTargetDir of nextExtendVertex"
                               , ObstacleTree, VisGraph);
                    nextExtendVertex = this.AddVertex(nextExtendPoint);
                }
                FindOrAddEdge(extendVertex, nextExtendVertex, isOverlapped ? ScanSegment.OverlappedWeight : ScanSegment.NormalWeight);

                Debug_VerifyNonOverlappedExtension(isOverlapped, extendVertex, nextExtendVertex, spliceSource, spliceTarget);

                // This will split the edge if targetVertex is non-null; otherwise we are at a dead-end
                // on the target side so must not create a vertex as it would be inside an obstacle.
                FindOrAddEdge(spliceSource, nextExtendVertex, isOverlapped ? ScanSegment.OverlappedWeight : ScanSegment.NormalWeight);
                if (isOverlapped) {
                    isOverlapped = this.SeeIfSpliceIsStillOverlapped(extendDir, nextExtendVertex);
                }
                extendVertex = nextExtendVertex;

                // Test GetDirections because it may return Directions. None.
                if (0 == (extendDir & PointComparer.GetDirections(nextExtendPoint, maxDesiredSegment.End))) {
                    // At or past the desired max extension point, so we're done.
                    spliceTarget = null;
                    break;
                }
            }
            return  spliceTarget != null;
        }

        [Conditional("TEST_MSAGL")]
        private void Debug_VerifyNonOverlappedExtension(bool isOverlapped, VisibilityVertex extendVertex, VisibilityVertex nextExtendVertex,
                                                        VisibilityVertex spliceSource, VisibilityVertex spliceTarget) {
            if (isOverlapped) {
                return;
            }
#if TEST_MSAGL
            StaticGraphUtility.Assert(!this.ObstacleTree.SegmentCrossesANonGroupObstacle(extendVertex.Point, nextExtendVertex.Point)
                    , "extendDir edge crosses an obstacle", this.ObstacleTree, this.VisGraph);
#endif // TEST_MSAGL

            if (spliceSource == null) {
                // Only verifying the direct extension.
                return;
            }

            // Verify lateral splices as well.
            if ((spliceTarget == null)
                    || ( this.VisGraph.FindEdge(spliceSource.Point, spliceTarget.Point) == null
                            && (this.VisGraph.FindEdge(spliceSource.Point, nextExtendVertex.Point) == null))) {
                // If targetVertex isn't null and the proposed edge from nextExtendVertex -> targetVertex
                // edge doesn't already exist, then we assert that we're not creating a new edge that
                // crosses the obstacle bounds (a bounds-crossing edge may already exist, from a port
                // within the obstacle; in that case nextExtendPoint splits that edge).  As above, don't
                // splice laterally across groups.
                StaticGraphUtility.Assert(!this.ObstacleTree.SegmentCrossesAnObstacle(spliceSource.Point, nextExtendVertex.Point)
                        , "spliceSource->extendVertex edge crosses an obstacle", this.ObstacleTree, this.VisGraph);

                // Above we moved spliceTarget over when nextExtendVertex existed, so account
                // for that here.
                StaticGraphUtility.Assert((spliceTarget == null)
                        || ( this.VisGraph.FindEdge(nextExtendVertex.Point, spliceTarget.Point) != null)
                                || !this.ObstacleTree.SegmentCrossesAnObstacle(nextExtendVertex.Point, spliceTarget.Point)
                        , "extendVertex->spliceTarget edge crosses an obstacle", this.ObstacleTree, this.VisGraph);
            }
        }

        private static bool GetNextSpliceSource(ref VisibilityVertex spliceSource, Direction spliceTargetDir, Direction extendDir) {
            VisibilityVertex nextSpliceSource = StaticGraphUtility.FindAdjacentVertex(spliceSource, extendDir);
            if (nextSpliceSource == null) {
                // See if there is a source further away from the extension line - we might have
                // been on freePoint line (or another nearby PortEntry line) that dead-ended.
                // Look laterally from the previous spliceSource first.
                nextSpliceSource = spliceSource;
                for (;;) {
                    nextSpliceSource = StaticGraphUtility.FindAdjacentVertex(nextSpliceSource, CompassVector.OppositeDir(spliceTargetDir));
                    if (nextSpliceSource == null) {
                        return false;
                    }
                    var nextSpliceSourceExtend = StaticGraphUtility.FindAdjacentVertex(nextSpliceSource, extendDir);
                    if ( nextSpliceSourceExtend != null) {
                        nextSpliceSource = nextSpliceSourceExtend;
                        break;
                    }
                }
            }
            spliceSource = nextSpliceSource;
            return true;
        }

        private static VisibilityVertex GetSpliceTarget(ref VisibilityVertex spliceSource, Direction spliceTargetDir, Point nextExtendPoint) {
            // Look for the target.  There may be a dead-ended edge starting at the current spliceSource
            // edge that has a vertex closer to the extension line; in that case keep walking until we
            // have the closest vertex on the Source side of the extension line as spliceSource.
            Direction prevDir = PointComparer.GetPureDirection(spliceSource.Point, nextExtendPoint);
            Direction nextDir = prevDir;
            var spliceTarget = spliceSource;
            while (nextDir == prevDir) {
                spliceSource = spliceTarget;
                spliceTarget = StaticGraphUtility.FindAdjacentVertex(spliceSource, spliceTargetDir);
                if (spliceTarget == null) {
                    break;
                }
                if (PointComparer.Equal(spliceTarget.Point, nextExtendPoint)) {
                    // If we encountered an existing vertex for the extension chain, update spliceTarget
                    // to be after it and we're done with this loop.
                    spliceTarget = StaticGraphUtility.FindAdjacentVertex(spliceTarget, spliceTargetDir);
                    break;
                }
                nextDir = PointComparer.GetPureDirection(spliceTarget.Point, nextExtendPoint);
            }
            return spliceTarget;
        }

        private bool SeeIfSpliceIsStillOverlapped(Direction extendDir, VisibilityVertex nextExtendVertex)
        {
            // If we've spliced out of overlapped space into free space, we may be able to turn off the 
            // overlapped state if we have a perpendicular non-overlapped edge.
            var edge = this.FindNextEdge(nextExtendVertex, CompassVector.RotateLeft(extendDir));
            var maybeFreeSpace = (edge == null) ? false : (ScanSegment.NormalWeight == edge.Weight);
            if (!maybeFreeSpace)
            {
                edge = this.FindNextEdge(nextExtendVertex, CompassVector.RotateRight(extendDir));
                maybeFreeSpace = (edge == null) ? false : (ScanSegment.NormalWeight == edge.Weight);
            }
            return !maybeFreeSpace || this.ObstacleTree.PointIsInsideAnObstacle(nextExtendVertex.Point, extendDir);
        }

        bool IsSkippableSpliceSourceWithNullSpliceTarget(VisibilityVertex spliceSource, Direction extendDir) {
            if (IsSkippableSpliceSourceEdgeWithNullTarget(StaticGraphUtility.FindAdjacentEdge(spliceSource, extendDir))) {
                return true;
            }
            var spliceSourceEdge = StaticGraphUtility.FindAdjacentEdge(spliceSource, CompassVector.OppositeDir(extendDir));

            // Since target is null, if this is a reflection, it is bouncing off an outer side of a group or 
            // obstacle at spliceSource.  In that case, we don't want to splice from it because then we could
            // cut through the group and outside again; instead we should just stay outside it.
            return (IsSkippableSpliceSourceEdgeWithNullTarget(spliceSourceEdge) || IsReflectionEdge(spliceSourceEdge));
        }

        static bool IsSkippableSpliceSourceEdgeWithNullTarget(VisibilityEdge spliceSourceEdge) {
            return ( spliceSourceEdge != null)
                && ( spliceSourceEdge.IsPassable != null) 
                && (PointComparer.Equal(spliceSourceEdge.Length, GroupBoundaryCrossing.BoundaryWidth));
        }

        static bool IsReflectionEdge(VisibilityEdge edge) {
            return ( edge != null) && (edge.Weight == ScanSegment.ReflectionWeight);
        }

        static bool IsPointPastSegmentEnd(LineSegment maxSegment, Point point) {
            return PointComparer.GetDirections(maxSegment.Start, maxSegment.End) == PointComparer.GetDirections(maxSegment.End, point);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        public override string ToString() {
            return string.Format("{0} {1}", AddedVertices.Count, edgesToRestore.Count);
        }

        #region DevTrace
#if DEVTRACE
        readonly DevTrace transGraphTrace = new DevTrace("Rectilinear_TransGraphTrace", "TransGraph");
        readonly DevTrace transGraphVerify = new DevTrace("Rectilinear_TransGraphVerify");
#endif // DEVTRACE

        #endregion // DevTrace
    }
}