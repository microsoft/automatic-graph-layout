//-----------------------------------------------------------------------
// <copyright file="RectilinearEdgeRouterWrapper.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using Microsoft.Msagl.Routing.Rectilinear.Nudging;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.UnitTests.Rectilinear
{
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Test class wrapping the production class
    /// </summary>
    internal class RectilinearEdgeRouterWrapper : RectilinearEdgeRouter
    {
        /// <summary>
        /// Gets or sets a value indicating whether paths are to be generated after ports are spliced into the graph.
        /// </summary>
        internal bool WantPaths { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether paths are to be nudged.
        /// </summary>
        internal bool WantNudger { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether verification is to be done.
        /// </summary>
        internal bool WantVerify { get; set; }

        internal double StraightTolerance { get; set; }
        internal double CornerTolerance { get; set; }

        /// <summary>
        /// Indicates ports are not to be created or spliced into the graph.
        /// </summary>
        internal bool NoPorts { get; set; }

        private Dictionary<Obstacle, List<Obstacle>> spatialChildrenToGroups;
        private Dictionary<Clump, RectangleNode<Obstacle,Point>> clumpToRectNode;
        private SuperClumpMap superClumpMap;
        private Dictionary<Shape, Set<Shape>> originalAncestorSets;
        private RectangleNode<Obstacle,Point> allObstacleHierarchy;
        
        internal TestContext TestContext { get; set; }

        internal RectilinearEdgeRouterWrapper(IEnumerable<Shape> obstacles, double padding, double cornerFitRadius,
                                            bool routeToCenterOfObstacles, bool useSparseVisibilityGraph, bool useObstacleRectangles)
            : base(obstacles, padding, cornerFitRadius, useSparseVisibilityGraph, useObstacleRectangles)
        {
            this.WantPaths = true;
            this.WantNudger = true;
            this.WantVerify = true;
            this.RouteToCenterOfObstacles = routeToCenterOfObstacles;
            StraightTolerance = 0.001;
            CornerTolerance = 0.1;

            // We've removed Node/Shape.Id so make sure we have the ordinal in UserData unless the test
            // has already put something there.
            foreach (var shapeAndId in obstacles.Select((s, ii) => new Tuple<Shape, int>(s, ii)))
            {
                if (null == shapeAndId.Item1.UserData)
                {
                    shapeAndId.Item1.UserData = shapeAndId.Item2;
                }
            }
        }

        internal void TestWriteLine(string message)
        {
            if (null != TestContext)
            {
                TestContext.WriteLine(message);
                return;
            }
            System.Diagnostics.Debug.WriteLine(message);
        }

        internal ObstacleTree ObsTree
        {
            get
            {
                return GraphGenerator.ObsTree;
            }
        }

        internal override void InitObstacleTree()
        {
            base.InitObstacleTree();
            this.ShowObstacleTree();
            this.allObstacleHierarchy = ObstacleTree.CalculateHierarchy(this.ObsTree.GetAllObstacles());
        }

        internal override void CreateVisibilityGraph()
        {
            base.CreateVisibilityGraph();

            ShowScanSegments(true /*after initial graph*/);

            if (this.WantVerify)
            {
                this.VerifyObstacleVisibilityGraph();
            }
        }

        internal override void GeneratePaths() 
        {
            if (!this.NoPorts) 
            {
                base.GeneratePaths();
            }
        }

        internal override void SpliceVisibilityAndGeneratePath(MsmtRectilinearPath shortestPathRouter, Path edgePath)
        {
            // VisibilityGraph.EdgeCount now sums through all edges so only do it if we're verifying.
            int edgesBefore = -1;
            int verticesBefore = VisibilityGraph.VertexCount;
            var horizontalEdgesBefore = new List<VisibilityEdge>();
            var verticalEdgesBefore = new List<VisibilityEdge>();
            if (this.WantVerify)
            {
                foreach (var edge in base.VisibilityGraph.Edges)
                {
                    (StaticGraphUtility.IsVertical(edge) ? verticalEdgesBefore : horizontalEdgesBefore).Add(edge);
                    Validate.IsFalse(edge is TollFreeVisibilityEdge, "TransientVisibilityEdge exist before path generation");
                }
                edgesBefore = horizontalEdgesBefore.Count + verticalEdgesBefore.Count;
            }

            base.SpliceVisibilityAndGeneratePath(shortestPathRouter, edgePath);

            Validate.AreEqual(verticesBefore, VisibilityGraph.VertexCount, "Vertex count mismatch after path generation");
            if (this.WantVerify)
            {
                var transientEdgesAfter = new List<VisibilityEdge>();
                var unfoundEdgesAfter = new List<VisibilityEdge>();
                int edgesAfter = 0;
                foreach (var edge in this.VisibilityGraph.Edges) 
                {
                    ++edgesAfter;
                    if (edge is TollFreeVisibilityEdge)
                    {
                        transientEdgesAfter.Add(edge);
                    }
                }

                // If we had the same vertex and edge counts and no transient edges remaining, we should be good.  Otherwise,
                // do the more expensive test to detect unfound edges.
                if ((edgesBefore != edgesAfter) || (transientEdgesAfter.Count > 0))
                {
                    foreach (var edge in horizontalEdgesBefore.Concat(verticalEdgesBefore))
                    {
                        if (null == this.VisibilityGraph.FindEdge(edge.SourcePoint, edge.TargetPoint))
                        {
                            unfoundEdgesAfter.Add(edge);
                        }
                    }
                }
                Validate.AreEqual(edgesBefore, edgesAfter, "Edge count mismatch after path generation");
                Validate.IsTrue(transientEdgesAfter.Count == 0, "TransientVisibilityEdges exist after path generation");
                Validate.IsTrue(unfoundEdgesAfter.Count == 0, "Unfound VisibilityEdges exist after path generation");
            }
        }

        internal override bool GeneratePath(MsmtRectilinearPath shortestPathRouter, Path edgePath, bool lastChance = false)
        {
            Validate.IsNotNull(edgePath, "EdgePath should not be null");
            this.VerifySpliceEdges(edgePath.EdgeGeometry);

            // Show the per-path graph with ports and waypoints for that path.
            this.ShowVisibilityGraph(/*isForPath:*/ true, "VG before RER.GeneratePath");

            var ret = true;
            if (this.WantPaths)
            {
                ret = base.GeneratePath(shortestPathRouter, edgePath, lastChance);
                this.ShowGraphPerPath(edgePath, lastChance);
            }
            return ret;
        }

        private void VerifySpliceEdges(EdgeGeometry edgeGeom) 
        {
            if (!WantVerify) 
            {
                return;
            }

            var sourceOport = PortManager.FindObstaclePort(edgeGeom.SourcePort);
            var targetOport = PortManager.FindObstaclePort(edgeGeom.TargetPort);
            var sourceObstacle = (null == sourceOport) ? null : sourceOport.Obstacle;
            var targetObstacle = (null == targetOport) ? null : targetOport.Obstacle;

            // The base VG has been verified already so we just verify the transient edges (except those
            // we've added for group crossings; they're always tiny and non-overlapped, to grant or deny
            // passage across the group boundary).
            foreach (var edge in PortManager.TransUtil.AddedEdges.Where(e => e.IsPassable == null))
            {
                if (!VerifySpliceEdge(edge, sourceObstacle, targetObstacle, recursive:false)) 
                {
                    var infoString = GetSourceAndTargetString(edgeGeom, sourceObstacle, targetObstacle);
                    Validate.Fail(edge.Weight != ScanSegment.OverlappedWeight
                            ? string.Format("non-overlapped transient VisibilityEdge {0} crosses obstacles; {1}", edge, infoString)
                            : string.Format("overlapped transient VisibilityEdge {0} does not cross an obstacle; {1}", edge, infoString));
                }
            }
        }

        private bool VerifySpliceEdge(VisibilityEdge edge, Obstacle sourceObstacle, Obstacle targetObstacle, bool recursive) {
            // Set tolerance according to whether we're verifying that an overlapped edge actually crosses
            // something, or that a non-overlapped edge doesn't.
            var tolerance = ApproximateComparer.IntersectionEpsilon * 10;
            if (edge.Weight != ScanSegment.OverlappedWeight) {
                // Shrink things to make it less likely we'll get a false positive.
                tolerance *= -1.0;
            }

            var crossesNonEndpointObstacle = false;
            foreach (var crossedObstacle in ObstaclesCrossedByEdge(edge, tolerance))
            {
                if ((crossedObstacle == sourceObstacle) || (crossedObstacle == targetObstacle))
                {
                    return true;
                }
                crossesNonEndpointObstacle = true;
            }

            if (crossesNonEndpointObstacle)
            {
                if (edge.Weight != ScanSegment.OverlappedWeight) 
                {
                    return false;
                }
            }
            else
            {
                if (edge.Weight == ScanSegment.OverlappedWeight)
                {
                    // It may cross a convex hull in a space where there are no "real" objects.
                    if (this.ConvexHullsCrossedByEdge(edge, tolerance).Any())
                    {
                        return true;
                    }
                    if (recursive)
                    {
                        return false;
                    }

                    // It may be a FreePort edge that has an intersecting edge spliced into it, thereby creating
                    // two edges.  So grow it as long as it is overlapped and then see if that crosses anything.
                    bool wantOverlapped = (edge.Weight == ScanSegment.OverlappedWeight);
                    var edgeDir = StaticGraphUtility.EdgeDirection(edge);
                    var firstVertex = this.FurthestVertexInDirection(edge.Source, CompassVector.OppositeDir(edgeDir), wantOverlapped);
                    var lastVertex = this.FurthestVertexInDirection(edge.Target, edgeDir, wantOverlapped);
                    if ((firstVertex == edge.Source) && (lastVertex == edge.Target)) 
                    {
                        return false;
                    }
                    var newEdge = new VisibilityEdge(firstVertex, lastVertex, edge.Weight);
                    return this.VerifySpliceEdge(newEdge, sourceObstacle, targetObstacle, recursive:true);
                }
            }
            return true;
        }

        private VisibilityVertex FurthestVertexInDirection(VisibilityVertex start, Direction dir, bool wantOverlapped) 
        {
            var furthest = start;
            for ( ; ; ) 
            {
                var next = StaticGraphUtility.FindAdjacentVertex(furthest, dir);
                if (next == null)
                {
                    break;
                }
                var edge = this.VisibilityGraph.FindEdge(furthest.Point, next.Point);
                if ((edge.Weight == ScanSegment.OverlappedWeight) != wantOverlapped) 
                {
                    break;
                }
                furthest = next;
            }
            return furthest;
        }

        private IEnumerable<Obstacle> ObstaclesCrossedByEdge(VisibilityEdge edge, double tolerance)
        {
            var lineSegment = new LineSegment(edge.SourcePoint, edge.TargetPoint);
            return this.allObstacleHierarchy.AllHitItems(
                lineSegment.BoundingBox, obs => !obs.IsGroup 
                        && SegmentCrossesPolylineInterior(lineSegment, obs.PaddedPolyline, tolerance));
        }

        private IEnumerable<Obstacle> ConvexHullsCrossedByEdge(VisibilityEdge edge, double tolerance)
        {
            var lineSegment = new LineSegment(edge.SourcePoint, edge.TargetPoint);
            return this.ObsTree.Root.AllHitItems(
                lineSegment.BoundingBox, obs => !obs.IsGroup && obs.IsInConvexHull && obs.IsPrimaryObstacle
                        && SegmentCrossesPolylineInterior(lineSegment, obs.VisibilityPolyline, tolerance));
        }

        private static bool SegmentCrossesPolylineInterior(LineSegment segment, Polyline obstaclePolyline, double tolerance)
        {
            // If it is only on the obstacle border, we don't consider it overlapped.  Due to rounding issues,
            // shrink the box for this just a bit to avoid false positives; visually it doesn't matter if an
            // an edge that runs only a DistanceEpsilon inside a border is considered overlapped or not.
            var obstacleBbox = obstaclePolyline.BoundingBox;
            obstacleBbox.Pad(tolerance);
            if (!StaticGraphUtility.RectangleInteriorsIntersect(segment.BoundingBox, obstacleBbox))
            {
                return false;
            }

            IList<IntersectionInfo> xxs = Curve.GetAllIntersections(segment, obstaclePolyline, liftIntersections:true);
            if (xxs.Count == 0)
            {
                // No intersection so the segment's either entirely inside or entirely outside the obstacle,
                // but rounding may make it appear just outside, so do the tolerance check on the endpoints.
                return PointIsInsidePolylineWithTolerance(segment.Start, obstaclePolyline, tolerance)
                        || PointIsInsidePolylineWithTolerance(segment.End, obstaclePolyline, tolerance);
            }

            // If it has two (or more, but that shouldn't happen now) intersections then it must cross the interior
            // because we've verified above it is not just skimming along the bounding box edge.
            if (xxs.Count > 1)
            {
                return true;
            }
            
            // One intersection, so see if a midpoint between the intersection and either end is inside the obstacle.
            // The intersection may also be on an extreme vertex, e.g. the apex of a triangle, so check it directly.
            // Use two "return" statements so it's easier to set a breakpoint when we return true.
            var intersect = xxs[0].IntersectionPoint;
            if (PointIsInsidePolylineWithTolerance(intersect, obstaclePolyline, tolerance)
                || MidpointIsInsidePolyline(segment.Start, intersect, obstaclePolyline, tolerance)
                || MidpointIsInsidePolyline(intersect, segment.End, obstaclePolyline, tolerance)) 
            {
                return true;
            }

            return false;
        }

        private static bool MidpointIsInsidePolyline(Point start, Point end, Polyline obstaclePolyline, double tolerance) 
        {
            if (tolerance < 0.0)
            {
                // We're verifying the edge doesn't cross an obstacle so if it's tiny, ignore it due to
                // rounding error causing false positives.  We'll halve the distance so use double the tolerance.
                if (ApproximateComparer.Close(start, end, Math.Abs(tolerance) * 2)) {
                    return false;
                }
            }
            var midPoint = start + ((end - start) / 2);
            return PointIsInsidePolylineWithTolerance(midPoint, obstaclePolyline, tolerance);
        }

        private static bool PointIsInsidePolylineWithTolerance(Point point, Polyline polyline, double tolerance)
        {
            // If the tolerance is negative, we "shrink" the obstacle by making sure the point is more than
            // tolerance from its parameter location on the boundary.
            var isInside = PointLocation.Inside == Curve.PointRelativeToCurveLocation(point, polyline);
            var closestParameter = Curve.ClosestPoint(polyline, point);
            var isWithinTolerance = ApproximateComparer.Close(closestParameter, point, Math.Abs(tolerance));
            if (tolerance < 0.0) {
                // Is it inside by more than the required tolerance?
                return isInside && !isWithinTolerance;
            }
            // Is it inside or within the required tolerance?
            return isInside || isWithinTolerance;
        }

        internal override void GeneratePathThroughVisibilityIntersection(Path edgePath, Point[] intersectPoints)
        {
            Validate.IsNotNull(edgePath, "EdgePath should not be null");
            this.ShowVisibilityGraph(/*isForPath:*/ true, "VG before RER.GeneratePathThroughVisibilityIntersection");
            base.GeneratePathThroughVisibilityIntersection(edgePath, intersectPoints);
        }

        private void ShowGraphPerPath(Path edgePath, bool lastChance)
        {
            // This is the non-virtual form that calls the virtual/overridden form for the pathPoints.
            IEnumerable<Path> paths =  new List<Path> { edgePath };
            foreach (var path in paths) {
                // Only verify that this result is non-null if lastChance; otherwise we will retry with additional groups enabled.
                if (lastChance) {
                    Validate.IsNotNull(path.PathPoints, string.Format("Null path result between: {0}", GetSourceAndTargetString(path.EdgeGeometry)));
                }
                this.ShowGraphPerPath(path.PathPoints);
            }
        }

        internal override void RetryPathsWithAdditionalGroupsEnabled(
            MsmtRectilinearPath shortestPathRouter, Path edgePath)
        {
            if (this.WantPaths)
            {
                base.RetryPathsWithAdditionalGroupsEnabled(shortestPathRouter, edgePath);
            }
        }

        internal override void NudgePaths(IEnumerable<Path> edgePaths)
        {
            Validate.IsNotNull(edgePaths, "edgePaths should not be null");
            if (this.WantPaths)
            {
                foreach (var edgePath in edgePaths)
                {
                    Validate.IsNotNull(edgePath.PathPoints, string.Format("Null path result between: {0}", GetSourceAndTargetString(edgePath.EdgeGeometry)));
                    var pointList = edgePath.PathPoints.ToList();
                    for (int ii = 1; ii < pointList.Count; ++ii) {
                        var dir = CompassVector.DirectionsFromPointToPoint(pointList[ii - 1], pointList[ii]);
                        Validate.IsTrue(CompassVector.IsPureDirection(dir), "Impure direction in pathPoints before nudging");
                    }
                }
                if (this.WantNudger)
                {
                    base.NudgePaths(edgePaths);
                }
                else
                {
                    // Want paths but not nudger, so create a fake result from nudging.
                    foreach (var path in edgePaths.Where(path => path != null && path.PathPoints != null))
                    {
                        path.EdgeGeometry.Curve = new Polyline(path.PathPoints);
                    }
                }
            }
        }
internal override void FinaliseEdgeGeometries()
        {
            if (this.WantPaths)
            {
                base.FinaliseEdgeGeometries();
                if (this.WantVerify)
                {
                    VerifyPaths();
                }
            }
        }

        // Overridden by TestRectilinearEdgeRouter.
        internal virtual void ShowVisibilityGraph(bool isForPath, string message) { }
        internal virtual void ShowGraphPerPath(IEnumerable<Point> pathPoints) { }
        internal virtual void ShowScanSegments(bool isAfterInitialGraph) { }
        internal virtual void ShowObstacleTree() { }

        /// <summary>
        /// Gets segments generated by horizontal scan
        /// </summary>
        internal IEnumerable<LineSegment> HorizontalScanLineSegments
        {
            get
            {
                return GraphGenerator.HorizontalScanSegments.Segments.Select(seg => new LineSegment(seg.Start, seg.End));
            }
        }

        /// <summary>
        /// Gets segments generated by vertical scan
        /// </summary>
        internal IEnumerable<LineSegment> VerticalScanLineSegments
        {
            get
            {
                return GraphGenerator.VerticalScanSegments.Segments.Select(seg => new LineSegment(seg.Start, seg.End));
            }
        }

        /// <summary>
        /// Add a Free Port to the visibility graph.  Normally we just add them when
        /// routing between them; this method lets us inspect the VisibilityGraph.
        /// </summary>
        /// <param name="port">The port to be added.</param>
        internal void AddPortToVisibilityGraph(Port port)
        {
            // Due to LimitRectangle, fake an EdgeGeometry.
            PortManager.AddControlPointsToGraph(new EdgeGeometry(port, port), ShapeToObstacleMap);
        }

        /// <summary>
        /// Remove all ports from the visibility graph.
        /// </summary>
        internal void RemoveAllControlPointsFromVisibilityGraph()
        {
            PortManager.RemoveControlPointsFromGraph();
        }

        // VisibilityGraph verification.
        internal void VerifyObstacleVisibilityGraph()
        {
            // Verify that scansegments do not cross obstacles if the are not overlapped, and do if they
            // are overlapped.
            foreach (ScanSegment scanSeg in GraphGenerator.HorizontalScanSegments.Segments)
            {
                VerifyVisibilitySegment(scanSeg.Start, scanSeg.End, scanSeg.IsOverlapped);
            }

            foreach (ScanSegment scanSeg in GraphGenerator.VerticalScanSegments.Segments)
            {
                VerifyVisibilitySegment(scanSeg.Start, scanSeg.End, scanSeg.IsOverlapped);
            }

            VerifySegmentsAdjoiningObstacles();
            VerifyClumpsAndConvexHulls();
        }

        private static void VerifySegmentsAdjoiningObstacles()
        {
#if NOT_READY
            var graphBox = GraphGenerator.GraphBox;
            foreach (var obstacle in ObstacleTree.GetAllLeafNodes().Select(node => node.UserData)) {
                var bbox = obstacle.PaddedPolyline.BoundingBox;
                PolylinePoint ppt = obstacle.PaddedPolyline.StartPoint;
                do {
                    // Basic validation:  For every point on the polyline, if it is on a border (or two)
                    // of the polyline bounding box, it must have a ScanSegment passing through it, with
                    // First and LastVisibilityVertices that bracket the point (even if it is on the graphBox
                    // border, because endpoints are included in IsOnSegment).  It may be at a graphBox corner.
                    // TODOunit: This won't work if we've removed ScanSegments with no visibility.
                    if ((ppt.Point.X == bbox.Left) || (ppt.Point.X == bbox.Right)) {
                        // TODOolap: Verify overlap characteristics of the segment.
                        ScanSegment scanSeg = GraphGenerator.VerticalScanSegments.FindSegmentContainingPoint(
                                    ppt.Point, false /*allowUnfound*/);
                        Validate.IsNotNull(scanSeg, "Cannot find VScanSeg for ppt.X on border");
                        Validate.IsTrue(StaticGraphUtility.PointIsOnSegment(scanSeg, ppt.Point)
                                    , "HScanSegment VisibilityVertices do not bracket ppt.Point");
                    }
                    if ((ppt.Point.Y == bbox.Bottom) || (ppt.Point.Y == bbox.Top)) {
                        ScanSegment scanSeg = GraphGenerator.HorizontalScanSegments.FindSegmentContainingPoint(
                                    ppt.Point, false /*allowUnfound*/);
                        Validate.IsNotNull(scanSeg, "Cannot find HScanSeg for ppt.Y on border");
                        Validate.IsTrue(StaticGraphUtility.PointIsOnSegment(scanSeg, ppt.Point)
                                    , "VScanSegment VisibilityVertices do not bracket ppt.Point");
                    }
                    ppt = ppt.NextOnPolyline;
                } while (ppt != obstacle.PaddedPolyline.StartPoint);
            }
            // endforeach Obstacle
#endif  // NOT_READY
        }

        private void VerifyAndPopulateGroupSpatialChildren(Obstacle group, Obstacle obstacle) {
            Validate.IsTrue(group.IsGroup, "Validation hierarchy error - expected group");
            if (!group.IsRectangle || !obstacle.IsRectangle)
            {
                Validate.IsFalse(RectilinearVerifier.ObstaclesIntersect(group, obstacle),
                                "groups should only overlap an obstacle if both are rectangular");
                Validate.IsFalse(!obstacle.IsGroup && RectilinearVerifier.IsFirstObstacleEntirelyWithinSecond(group, obstacle, touchingOk:false),
                                "Group should only be inside non-group obstacles if both are rectangular");
                // By the time we're here we know that either they are outside each other or the obstacle is inside the group.
            }

            List<Obstacle> list;
            if (!this.spatialChildrenToGroups.TryGetValue(obstacle, out list))
            {
                list = new List<Obstacle>();
                this.spatialChildrenToGroups[obstacle] = list;
            }
            list.Add(group);
        }

        private void VerifyClumpsAndConvexHulls()
        {
            foreach (var obstacle in this.ObsTree.GetAllObstacles()) 
            {
                if (obstacle.IsOverlapped) 
                {
                    Validate.IsFalse(obstacle.IsGroup, "groups cannot be in a clump");
                    Validate.IsTrue(obstacle.IsRectangle, "obstacles in a clump must have initially been rectangles");
                    Validate.IsNull(obstacle.ConvexHull, "overlapped obstacles cannot be in a convex hull");
                    Validate.IsTrue(obstacle.Clump.Contains(obstacle), "Obstacle with a clump is not inside that clump's object list");
                    continue;
                }
                if (obstacle.IsInConvexHull) 
                {
                    Validate.IsTrue(obstacle.ConvexHull.Obstacles.Contains(obstacle), "Obstacle with a convex hull is not inside that hull's obstacle list");
                    if (obstacle.IsPrimaryObstacle) 
                    {
                        foreach (var sibling in obstacle.ConvexHull.Obstacles) 
                        {
                            Validate.IsTrue(obstacle.ConvexHull == sibling.ConvexHull, "Object inside a convex hull's obstacle list does not have a convex hull");
                        }
                    }
                }
                if (obstacle.IsGroup) 
                {
                    Validate.IsTrue(obstacle.IsPrimaryObstacle, "Groups should always be primary obstacles - their convex hull is never shared.");
                }
            }

            RectangleNodeUtils.CrossRectangleNodes<Obstacle,Point>(this.allObstacleHierarchy, this.allObstacleHierarchy, VerifyIntersectingObstacleBoundingBoxes);

            // Now verify all non-group spatial overlaps within a convex hull are part of that hull (we did groups already).
            foreach (var obstacleWithHull in this.allObstacleHierarchy.GetAllLeaves().Where(obs => obs.IsInConvexHull && obs.IsPrimaryObstacle && !obs.IsGroup)) 
            {
                var spatialChildren = this.allObstacleHierarchy.GetNodeItemsIntersectingRectangle(obstacleWithHull.VisibilityBoundingBox);
                foreach (var child in spatialChildren.Where(c => !c.IsGroup))
                {
                    if (child == obstacleWithHull) 
                    {
                        continue;
                    }
                    if (RectilinearVerifier.IsFirstPolylineEntirelyWithinSecond(child.PaddedPolyline, obstacleWithHull.VisibilityPolyline, true))
                    {
                        Validate.AreSame(obstacleWithHull.ConvexHull, child.ConvexHull, "obstacle inside convex hull bounds is not a member of that hull");
                        continue;
                    }
                    VerifyThatAnyIntersectionsAreCloseEnoughToTheHullBoundary(child.PaddedPolyline, obstacleWithHull.VisibilityPolyline);
                }
            }
        }

        private static void VerifyThatAnyIntersectionsAreCloseEnoughToTheHullBoundary(Polyline siblingPoly, Polyline hullPoly) {
            if (!Curve.CurvesIntersect(siblingPoly, hullPoly)) 
            {
                return;
            }
            ConvexHullTest.VerifyPointsAreInOrOnHull(siblingPoly.PolylinePoints.Select(ppt => ppt.Point), hullPoly);
        }

        private static void VerifyIntersectingObstacleBoundingBoxes(Obstacle a, Obstacle b)
        {
            // We've already verified that any members of a convex hull are correct, and that any
            // obstacle that says it's in a convex hull really is.  For non-convex hull there is always
            // a 1-1 relationship between polyline and obstacle.
            if (a.VisibilityPolyline == b.VisibilityPolyline)
            {
                return;
            }

            if (a.IsGroup && b.IsGroup) 
            {
                VerifyGroupAndGroupBoundingBoxIntersect(a, b);
                return;
            }
            if (a.IsGroup) 
            {
                VerifyGroupAndObstacleBoundingBoxIntersect(a, b);
                return;
            }
            if (b.IsGroup)
            {
                VerifyGroupAndObstacleBoundingBoxIntersect(b, a);
                return;
            }

            VerifyObstacleObstacleIntersect(a, b);
        }

        private static void VerifyGroupAndGroupBoundingBoxIntersect(Obstacle a, Obstacle b) {
            if (a.IsRectangle && b.IsRectangle) 
            {
                return;
            }

            // We don't care whether they are inside or outside; we just care that the borders don't touch.
            // The code wraps one with the other, with a bit of padding between them.
            Validate.IsFalse(Curve.CurvesIntersect(a.VisibilityPolyline, b.VisibilityPolyline), "two groups should not touch");
        }

        private static void VerifyGroupAndObstacleBoundingBoxIntersect(Obstacle group, Obstacle obstacle) {
            if (group.IsRectangle && obstacle.IsRectangle)
            {
                return;
            }

            // The same logic applies here as for groups/group; since we don't enforce spatial/hierarchical
            // consistency, the only thing we care about is that the borders don't touch.
            Validate.IsFalse(Curve.CurvesIntersect(group.VisibilityPolyline, obstacle.VisibilityPolyline), "a group and obstacle should not touch");
        }

        private static void VerifyObstacleObstacleIntersect(Obstacle a, Obstacle b) 
        {
            var curvesIntersect = Curve.CurvesIntersect(a.VisibilityPolyline, b.VisibilityPolyline);
            var oneCurveIsInTheOther = !curvesIntersect && InteractiveObstacleCalculator.OneCurveLiesInsideOfOther(a.VisibilityPolyline, b.VisibilityPolyline);
            if (!curvesIntersect && !oneCurveIsInTheOther) 
            {
                return;
            }

            // This could be either a clump or a (possibly transitively accreted from a clump) convex hull.
            Validate.AreEqual(a.IsOverlapped, b.IsOverlapped, "IsOverlapped is not equal");
            Validate.AreEqual(a.IsInConvexHull, b.IsInConvexHull, "OverlappedConvexHull is not equal");
            Validate.IsTrue(a.IsOverlapped || a.IsInConvexHull, "obstacles intersect but are not in a clump or OverlappedConvexHull");

            // These must be either in a clump or a convex hull.
            if (a.IsOverlapped) 
            {
                Validate.AreSame(a.Clump, b.Clump, "Intersecting obstacles are in different clumps");
                return;
            }

            Validate.AreEqual(a.ConvexHull, b.ConvexHull, "Intersecting obstacles are not in a clump or convex hull");
        }

        private Obstacle hitTestOverlappingObstacle;
        private Rectangle hitTestIntervalRect;
        private bool hitTestExpectOverlap;

        internal void VerifyVisibilitySegment(Point start, Point end, bool isOverlapped)
        {
            hitTestOverlappingObstacle = null;
            hitTestIntervalRect = new Rectangle(start, end);
            hitTestExpectOverlap = isOverlapped;
            ObsTree.Root.VisitTree(OverlappedRectangleHitTest, hitTestIntervalRect);
            if (isOverlapped != (null != hitTestOverlappingObstacle))
            {
                TestWriteLine(string.Format("  VisibilitySegment overlap mismatch: {0} -> {1}", start, end));
            }
            if (isOverlapped)
            {
                Validate.IsNotNull(hitTestOverlappingObstacle, "isOverlapped segment or edge doesn't cross an obstacle");
            }
            else
            {
                Validate.IsNull(hitTestOverlappingObstacle, "Not-isOverlapped segment or edge crosses an obstacle");
            }
        }

        private HitTestBehavior OverlappedRectangleHitTest(Obstacle obstacle)
        {
            // For VisitTree, we may have a non-leaf node.
            if ((null == obstacle) || obstacle.IsGroup)
            {
                return HitTestBehavior.Continue;
            }

            // If it is only on the obstacle border, we don't consider it overlapped.  Due to rounding issues,
            // shrink the box for this just a bit to avoid false positives; visually it doesn't matter if an
            // an edge that runs only a DistanceEpsilon inside a border is considered overlapped or not.
            var obstacleBbox = obstacle.PaddedBoundingBox;
            var boxAdjust = hitTestExpectOverlap ? 0.0 : (-ApproximateComparer.IntersectionEpsilon);
            obstacleBbox.Pad(boxAdjust);
            if (!StaticGraphUtility.RectangleInteriorsIntersect(hitTestIntervalRect, obstacleBbox))
            {
                return HitTestBehavior.Continue;
            }

            // Do the more expensive test for non-rectangular obstacle borders.  Because we have a single obstacle,
            // just do the maximum projection of the segment to the graphBox limits, then check for two intersections.
            Rectangle graphBox = this.ObsTree.GraphBox;
            bool isVertical = PointComparer.Equal(hitTestIntervalRect.Left, hitTestIntervalRect.Right);
            LineSegment hitTestSeg = isVertical
                                    ? new LineSegment(hitTestIntervalRect.Left, graphBox.Bottom, hitTestIntervalRect.Left, graphBox.Top)
                                    : new LineSegment(graphBox.Left, hitTestIntervalRect.Bottom, graphBox.Right, hitTestIntervalRect.Bottom);
            IList<IntersectionInfo> xxs = Curve.GetAllIntersections(hitTestSeg, obstacle.PaddedPolyline, true /*liftIntersections*/);

            // If we have < 2 intersections it's just intersecting an extreme point, hence is not inside.
            if (xxs.Count != 2) 
            {
                // Previously a collinear flat boundary there could have 3 intersections; that doesn't happen with the
                // new curve intersection logic, but leave this in case something changes.  This is not inside the obstacle,
                // but we should have detected above that it was not within the rectangle.
                Validate.IsTrue(xxs.Count < 3, "Border intersections should already have been skipped");
                return HitTestBehavior.Continue;
            }

            Point rawInt0, rawInt1;
            double par0, par1;
            GetAscendingRawIntersections(hitTestSeg, xxs, out rawInt0, out rawInt1, out par0, out par1);

            // Touching is not overlapped.  We're using different comparisons from ScanLineIntersectSide so require
            // an overlap or non-overlap of more than IntersectionEpsilon (which is more lenient than DistanceEpsilon)
            // to report a deviation from the expected result.
            var identity = new Point(1, 1);
            double lowEpsilon = ApproximateComparer.IntersectionEpsilon * (hitTestExpectOverlap ? -1.0 : 1.0);
            double highEpsilon = ApproximateComparer.IntersectionEpsilon * (hitTestExpectOverlap ? -1.0 : 1.0);

            // Sides that are nearly parallel may have larger variances so use the derivative as a multiplier.
            Point lowDeriv = obstacle.PaddedPolyline.Derivative(par1);
            Point highDeriv = obstacle.PaddedPolyline.Derivative(par0);
            lowEpsilon *= GetEpsilonMultiplier(isVertical ? lowDeriv.Y : lowDeriv.X, isVertical ? lowDeriv.X : lowDeriv.Y);
            highEpsilon *= GetEpsilonMultiplier(isVertical ? highDeriv.Y : highDeriv.X, isVertical ? highDeriv.X : highDeriv.Y);

            double lowOverlap = (rawInt1 - hitTestIntervalRect.LeftBottom) * identity;
            double highOverlap = (hitTestIntervalRect.RightTop - rawInt0) * identity;

            if ((lowOverlap > lowEpsilon) && (highOverlap > highEpsilon))
            {
                // If we expected overlap, then if they are "close enough" we found it.
                // If we didn't expect overlap, then don't say we found one if the segment is shorter than epsilon.
                if (hitTestExpectOverlap ||
                    (((hitTestIntervalRect.RightTop - hitTestIntervalRect.LeftBottom) * identity) >
                        Math.Max(lowEpsilon, highEpsilon)))
                {
                    if (!hitTestExpectOverlap) {
                        // Last chance - see if it's close to its parameter.
                        if (CheckForIntersectionClosenessToParameterPoint(obstacle.PaddedPolyline, hitTestIntervalRect.LeftBottom)) {
                            return HitTestBehavior.Continue;
                        }
                        if (CheckForIntersectionClosenessToParameterPoint(obstacle.PaddedPolyline, hitTestIntervalRect.RightTop)) {
                            return HitTestBehavior.Continue;
                        }
                        // If we're still here, both approaches think it's a hit.
                    }

                    hitTestOverlappingObstacle = obstacle;
                    return HitTestBehavior.Stop;
                }
            }

            return HitTestBehavior.Continue;
        }

        private static void GetAscendingRawIntersections(LineSegment hitTestSeg, IList<IntersectionInfo> xxs,
                                            out Point rawInt0, out Point rawInt1, out double par0, out double par1)
        {
            rawInt0 = ApproximateComparer.Round(xxs[0].IntersectionPoint);
            rawInt1 = ApproximateComparer.Round(xxs[1].IntersectionPoint);
            par0 = xxs[0].Par1;
            par1 = xxs[1].Par1;
            if (rawInt0 > rawInt1)
            {
                // Swap to order consistent with rect.LeftBottom/RightTop.
                Point tempInt = rawInt1;
                rawInt1 = rawInt0;
                rawInt0 = tempInt;

                double tempPar = par1;
                par1 = par0;
                par0 = tempPar;
            }
        }

        private static bool CheckForIntersectionClosenessToParameterPoint(Polyline polyline, Point testPoint) {
            Point checkParamPoint = Curve.ClosestPoint(polyline, testPoint);
            return ApproximateComparer.CloseIntersections(testPoint, checkParamPoint);
        }

        private static double GetEpsilonMultiplier(double parallel, double perpendicular)
        {
            if (parallel < 0.0)
            {
                parallel = -parallel;
            }

            if (perpendicular < 0.0)
            {
                perpendicular = -perpendicular;
            }

            if ((0 != perpendicular) && (parallel > perpendicular))
            {
                return parallel / perpendicular;
            }

            return 1.0;
        }

        private IEnumerable<Obstacle> ObstaclesCrossedByPath(EdgeGeometry edgeGeom)
        {
            return this.allObstacleHierarchy.AllHitItems(
                edgeGeom.Curve.BoundingBox, obs => PathCrossesPolylineInterior(edgeGeom.Curve, obs.PaddedPolyline));
        }

        // This is like Curve.ClosedCurveInteriorsIntersect, without requiring path to be closed.
        private static bool PathCrossesPolylineInterior(ICurve path, ICurve polyline) 
        {
            if (!path.BoundingBox.Intersects(polyline.BoundingBox))
                return false;
            IList<IntersectionInfo> xxs = Curve.GetAllIntersections(path, polyline, liftIntersections:true);
            foreach (var xx in xxs)
            {
                if ((xx.Par0 < path.ParStart) || (xx.Par0 > path.ParEnd))
                {
                    Validate.Fail(string.Format("path parameter out of range: xx.Par0 {0} parStart {1} parEnd {2}", 
                                                xx.Par0, path.ParStart, path.ParEnd));
                }

                // We lifted these so they should be Curve.CloseIntersections but allow for rounding.
                var tolerance = ApproximateComparer.IntersectionEpsilon*10;
                if (!ApproximateComparer.Close(path[xx.Par0], xx.IntersectionPoint, tolerance))
                {
                    Validate.Fail(string.Format("path paramPoint not close to intersection: xx.Par0 {0} path[par0] {1} intersectionPoint {2}",
                                                xx.Par0, path[xx.Par0], xx.IntersectionPoint));
                }
                if (!ApproximateComparer.Close(polyline[xx.Par1], xx.IntersectionPoint, tolerance))
                {
                    Validate.Fail(string.Format("polyline paramPoint not close to intersection: xx.Par1 {0} polyline[par1] {1} intersectionPoint {2}",
                                                xx.Par1, polyline[xx.Par1], xx.IntersectionPoint));
                }
            }

            // There may be more than two intersections if the curve, for example, hits the obstacle, runs along its
            // side, and then dives into it.
            if (xxs.Count < 2)
            {
                return false;
            }
            return Curve.PointsBetweenIntersections(path, xxs).Any(
                    p => Curve.PointRelativeToCurveLocation(p, polyline) == PointLocation.Inside);
        }

        private static bool IsObstacleAnAncestor(Set<Shape> sourceAncestors, Set<Shape> targetAncestors, Obstacle obstacle)
        {
            return ((sourceAncestors != null) && sourceAncestors.Contains(obstacle.InputShape))
                || ((targetAncestors != null) && targetAncestors.Contains(obstacle.InputShape));
        }

        private bool IsObstacleAnOriginalAncestor(Obstacle sourceObstacle, Obstacle targetObstacle, Obstacle obstacle)
        {
            // Spatial ancestors are inserted into AncestorSets if paths are blocked.  However, we remove groups from an
            // obstacle's AncestorSet if the obstacle is not a spatial child, so check the original hierarchical ancestors
            // too; otherwise, and earlier path which crosses a non-spatial hierarchical ancestor would fail this validation
            // if a subsequent path caused SpatialAncestorsAdjusted (thereby removing that crossed non-spatial ancestor).
            // File Test: TestCode_Groups_NonSpatial_Ancestor_Crossed_Before_AdjustSpatialAncestors.
            if (!this.ObsTree.SpatialAncestorsAdjusted)
            {
                return false;
            }
            var originalSourceAncestors = (sourceObstacle == null) ? null : this.originalAncestorSets[sourceObstacle.InputShape];
            var originalTargetAncestors = (targetObstacle == null) ? null : this.originalAncestorSets[targetObstacle.InputShape];
            return IsObstacleAnAncestor(originalSourceAncestors, originalTargetAncestors, obstacle);
        }

        private static bool IsEndpoint(Obstacle sourceObstacle, Obstacle targetObstacle, Obstacle crossedObstacle)
        {
            return (crossedObstacle == sourceObstacle) || (crossedObstacle == targetObstacle);
        }

        private static bool IsInEndpointClump(Obstacle sourceObstacle, Obstacle targetObstacle, Obstacle crossedObstacle)
        {
            return ((sourceObstacle != null) && (sourceObstacle.IsOverlapped) && (crossedObstacle.Clump == sourceObstacle.Clump)) ||
                   ((targetObstacle != null) && (targetObstacle.IsOverlapped) && (crossedObstacle.Clump == targetObstacle.Clump));
        }

        private static bool IsInEndpointConvexHull(Obstacle sourceObstacle, Obstacle targetObstacle, Obstacle crossedObstacle) 
        {
            return ((sourceObstacle != null) && (sourceObstacle.IsInConvexHull) && (crossedObstacle.ConvexHull == sourceObstacle.ConvexHull)) ||
                   ((targetObstacle != null) && (targetObstacle.IsInConvexHull) && (crossedObstacle.ConvexHull == targetObstacle.ConvexHull));
        }

        private static bool IsPortLocationInsideConvexHull(Port sourcePort, Port targetPort, Obstacle crossedObstacle)
        {
            if (crossedObstacle.ConvexHull == null)
            {
                return false;
            }
            if (Curve.PointRelativeToCurveLocation(sourcePort.Location, crossedObstacle.ConvexHull.Polyline) != PointLocation.Outside)
            {
                return true;
            }
            return (Curve.PointRelativeToCurveLocation(targetPort.Location, crossedObstacle.ConvexHull.Polyline) != PointLocation.Outside);
        }

        private static bool IsPortLocationInsideObstacle(Port sourcePort, Port targetPort, Obstacle crossedObstacle) 
        {
            // See if the port is inside the crossedObstacle.  This handles freeports.
            return PointLocation.Outside != Curve.PointRelativeToCurveLocation(sourcePort.Location, crossedObstacle.PaddedPolyline)
                || PointLocation.Outside != Curve.PointRelativeToCurveLocation(targetPort.Location, crossedObstacle.PaddedPolyline);
        }

        private bool IsGroupSpatialParentOfEndpointObstacle(Obstacle sourceObstacle, Obstacle targetObstacle, Obstacle group) 
        {
            // ObstacleTree.SpatialAncestorsAdjusted may not have been done, in which case obstacles that were
            // wrapped by an expanded group's convex hull will not have that group in their ancestor list.
            if (!group.IsGroup) 
            {
                return false;
            }
            return IsGroupSpatialParentOfObstacle(group, sourceObstacle) || IsGroupSpatialParentOfObstacle(group, targetObstacle);
        }

        private bool IsGroupSpatialParentOfObstacle(Obstacle group, Obstacle obstacle) {
            if (obstacle == null)
            {
                return false;
            }
            List<Obstacle> list;
            return this.spatialChildrenToGroups.TryGetValue(obstacle, out list) && list.Contains(group);
        }

        private void CreateGroupSpatialChildMap()
        {
            var groupHierarchy = ObstacleTree.CalculateHierarchy(this.ObsTree.GetAllGroups());
            this.spatialChildrenToGroups = new Dictionary<Obstacle, List<Obstacle>>();
            if ((groupHierarchy == null) || (this.allObstacleHierarchy == null)) 
            {
                return;
            }
            RectangleNodeUtils.CrossRectangleNodes<Obstacle,Point>(groupHierarchy, this.allObstacleHierarchy, this.VerifyAndPopulateGroupSpatialChildren);
        }

        private void CreateSuperClumpMap()
        {
            this.superClumpMap = new SuperClumpMap(this.ObsTree);
        }

        private void CreateClumpMap()
        {
            this.clumpToRectNode = new Dictionary<Clump, RectangleNode<Obstacle,Point>>();
            foreach (var clumpee in this.ObsTree.GetAllObstacles().Where(obs => obs.IsOverlapped))
            {
                this.clumpToRectNode[clumpee.Clump] = null;
            }
            var uniqueClumps = this.clumpToRectNode.Keys.ToArray();
            foreach (var clump in uniqueClumps)
            {
                this.clumpToRectNode[clump] = ObstacleTree.CalculateHierarchy(clump);
            }
        }

        internal void VerifyPaths()
        {
            if (!this.WantPaths)
            {
                return;
            }

            this.CreateGroupSpatialChildMap();
            this.CreateSuperClumpMap();
            this.CreateClumpMap();
            if (this.ObsTree.SpatialAncestorsAdjusted) {
                this.originalAncestorSets = SplineRouter.GetAncestorSetsMap(Obstacles);
            }

            // If a path crosses an obstacle, that obstacle must be either a parent (hierarchical or spatial) group, or 
            // an overlapped obstacle that overlaps with the source or target.
            foreach (var edgeGeom in EdgeGeometriesToRoute)
            {
                ObstaclePort sourceOport, targetOport;
                var sourceAncestors = PortManager.FindAncestorsAndObstaclePort(edgeGeom.SourcePort, out sourceOport);
                var targetAncestors = PortManager.FindAncestorsAndObstaclePort(edgeGeom.TargetPort, out targetOport);
                var sourceObstacle = (null == sourceOport) ? null : sourceOport.Obstacle;
                var targetObstacle = (null == targetOport) ? null : targetOport.Obstacle;

                
                Validate.IsNotNull(edgeGeom.Curve, string.Format("Null path result between: {0}", GetSourceAndTargetString(edgeGeom, sourceObstacle, targetObstacle)));
                foreach (var crossedObstacle in ObstaclesCrossedByPath(edgeGeom)) 
                {
                    // Broke out the first, most common stuff because VS can't set a breakpoint inside the multi-statement if().
                    List<Point> pointsInsidePadding;
                    if (IsEndpoint(sourceObstacle, targetObstacle, crossedObstacle)) 
                    {
                        continue;
                    }
                    if (this.NoIntersectionPointsAreInsidePadding(edgeGeom, crossedObstacle, out pointsInsidePadding)) 
                    {
                        continue;
                    }
                    if (IsInEndpointClump(sourceObstacle, targetObstacle, crossedObstacle) ||
                            IsInEndpointConvexHull(sourceObstacle, targetObstacle, crossedObstacle)) 
                    {
                        continue;
                    }
                    if (IsPortLocationInsideObstacle(edgeGeom.SourcePort, edgeGeom.TargetPort, crossedObstacle) ||
                            IsPortLocationInsideConvexHull(edgeGeom.SourcePort, edgeGeom.TargetPort, crossedObstacle)) 
                    {
                        continue;
                    }
                    if (IsObstacleAnAncestor(sourceAncestors, targetAncestors, crossedObstacle) ||
                            this.IsObstacleAnOriginalAncestor(sourceObstacle, targetObstacle, crossedObstacle) ||
                            this.IsGroupSpatialParentOfEndpointObstacle(sourceObstacle, targetObstacle, crossedObstacle) ||
                            this.CrossedObstacleClumpContainsEndpoint(edgeGeom, crossedObstacle) ||
                            IsCrossedObstacleBetweenGroupOportPaddedAndUnpaddedBorder(sourceObstacle, sourceOport, crossedObstacle) ||
                            IsCrossedObstacleBetweenGroupOportPaddedAndUnpaddedBorder(targetObstacle, targetOport, crossedObstacle) ||
                            this.IsCrossedObstaclePartOfLandlock(edgeGeom, crossedObstacle) ||
                            this.IsCrossedObstaclePartOfIntraGroupRoadBlock(edgeGeom, crossedObstacle))
                    {
                        continue;
                    }

                    this.PrintFailurePoints(edgeGeom, sourceObstacle, targetObstacle, crossedObstacle, pointsInsidePadding);
                }
            }
        }

        private bool NoIntersectionPointsAreInsidePadding(EdgeGeometry edgeGeom, Obstacle crossedObstacle, out List<Point> points) {
            // Re-get the intersection(s) to compare distance, and so we can examine them to debug.
            var xxs = Curve.GetAllIntersections(edgeGeom.Curve, crossedObstacle.PaddedPolyline, /*liftIntersections:*/ true);

            // If we are at a corner, the arc may cut off the corner, so make sure we are more than
            // Padding away from the inner unpadded curve.
            points = this.GetPointsInsidePadding(edgeGeom.Curve, crossedObstacle, xxs);
            return !points.Any();
        }

        private bool IsCrossedObstaclePartOfIntraGroupRoadBlock(EdgeGeometry edgeGeom, Obstacle crossedObstacle)
        {
            // Source or target may be inside a group and is roadblocked by overlapping obstacles which span the group 
            // in one or both axes between the two ports; in this case the overlapped edges are a high-cost but viable
            // path in the face of non-transparent group boundaries. TODOdoc doc this
            var routeAsTheCrowFlies = new LineSegment(edgeGeom.SourcePort.Location, edgeGeom.TargetPort.Location);

            // See if obstacles form a roadblock.  First try simple clumps...
            if (crossedObstacle.Clump != null)
            {
                Rectangle crossedClumpRect = (Rectangle)this.clumpToRectNode[crossedObstacle.Clump].Rectangle;
                if (this.CheckDirectRouteAcrossGroupThroughClumpRect(edgeGeom, routeAsTheCrowFlies, crossedClumpRect)) 
                {
                    return true;
                }
            }

            // ... then try superclumps.
            var superClump = this.superClumpMap.FindClump(crossedObstacle);
            if (superClump != null)
            {
                return this.CheckDirectRouteAcrossGroupThroughClumpRect(edgeGeom, routeAsTheCrowFlies, superClump.Rectangle);
            }
            return false;
        }

        private bool CheckDirectRouteAcrossGroupThroughClumpRect(EdgeGeometry edgeGeom, LineSegment routeAsTheCrowFlies, Rectangle crossedClumpRect)
        {
            return ClumpIsGroupRoadblock(routeAsTheCrowFlies, crossedClumpRect,
                        this.ObsTree.Root.AllHitItems(new Rectangle(edgeGeom.SourcePort.Location), obs => obs.IsGroup))
                   || ClumpIsGroupRoadblock(routeAsTheCrowFlies, crossedClumpRect,
                        this.ObsTree.Root.AllHitItems(new Rectangle(edgeGeom.TargetPort.Location), obs => obs.IsGroup));
        }

        private bool CrossedObstacleClumpContainsEndpoint(EdgeGeometry edgeGeom, Obstacle crossedObstacle)
        {
            // Return true iff crossedObstacle's clump contains the source or target port location.
            if (crossedObstacle.Clump == null)
            {
                return false;
            }
            var root = this.clumpToRectNode[crossedObstacle.Clump];
            return (null != root.FirstHitNode(edgeGeom.SourcePort.Location, IsPointInsidePaddedPolyline))
                || (null != root.FirstHitNode(edgeGeom.TargetPort.Location, IsPointInsidePaddedPolyline));
        }

        private static bool IsCrossedObstacleBetweenGroupOportPaddedAndUnpaddedBorder(Obstacle sourceObstacle, ObstaclePort sourceOport, Obstacle crossedObstacle) 
        {
            // As with convex hull children, a group may overwrite obstacles (which in the group case may be within convex hulls)
            // contained by the group's convex hull when it creates its unpadded-to-padded border intersections.
            if ((null == sourceObstacle) || !sourceObstacle.IsGroup || (null == sourceOport)) 
            {
                return false;
            }

            return sourceOport.PortEntrances.Where(oport => OportUnpaddedToPaddedBorderCrossesObstacle(oport, crossedObstacle)).Any();
        }

        private static bool OportUnpaddedToPaddedBorderCrossesObstacle(ObstaclePortEntrance oport, Obstacle crossedObstacle) {
            var oportRect = new Rectangle(oport.UnpaddedBorderIntersect, oport.VisibilityBorderIntersect);
            return crossedObstacle.PaddedBoundingBox.Intersects(oportRect);
        }

        private static HitTestBehavior IsPointInsidePaddedPolyline(Point pnt, Obstacle obs) 
        {
            return (Curve.PointRelativeToCurveLocation(pnt, obs.PaddedPolyline) != PointLocation.Outside) 
                        ? HitTestBehavior.Stop : HitTestBehavior.Continue;
        }

        private static bool ClumpIsGroupRoadblock(LineSegment routeAsTheCrowFlies, Rectangle crossedClumpRect, IEnumerable<Obstacle> groups)
        {
            foreach (var group in groups.Where(group => group.PaddedBoundingBox.Intersects(crossedClumpRect)))
            {
                // If it crosses the group at two locations and the straight routing crosses through it, consider it a roadblock.
                int numIntersects = 0;
                if (crossedClumpRect.Bottom <= group.PaddedBoundingBox.Bottom)
                {
                    ++numIntersects;
                }
                if (crossedClumpRect.Top >= group.PaddedBoundingBox.Top)
                {
                    ++numIntersects;
                }
                if (crossedClumpRect.Left <= group.PaddedBoundingBox.Left)
                {
                    ++numIntersects;
                }
                if (crossedClumpRect.Right >= group.PaddedBoundingBox.Right)
                {
                    ++numIntersects;
                }
                if (numIntersects > 1)
                {
                    if (crossedClumpRect.Contains(new Rectangle(routeAsTheCrowFlies.Start, routeAsTheCrowFlies.End)))
                    {
                        // Both ports are inside the clump bounding box.
                        return true;
                    }
                    var rect = CurveFactory.CreateRectangle(crossedClumpRect.Width, crossedClumpRect.Height, crossedClumpRect.Center);
                    if (Curve.GetAllIntersections(routeAsTheCrowFlies, rect, /*liftIntersections:*/ true).Any())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsCrossedObstaclePartOfLandlock(EdgeGeometry edgeGeom, Obstacle crossedObstacle)
        {
            if (crossedObstacle.Clump != null)
            {
                if (SuperClump.LandlocksPoint(this.clumpToRectNode[crossedObstacle.Clump], edgeGeom.SourcePort.Location)
                    || SuperClump.LandlocksPoint(this.clumpToRectNode[crossedObstacle.Clump], edgeGeom.TargetPort.Location))
                {
                    return true;
                }
            }
            var superClump = this.superClumpMap.FindClump(crossedObstacle);
            if (superClump != null)
            {
                return superClump.LandlocksPoint(edgeGeom.SourcePort.Location) || superClump.LandlocksPoint(edgeGeom.TargetPort.Location);
            }
            return false;
        }

        private List<Point> GetPointsInsidePadding(ICurve path, Obstacle crossedObstacle, IList<IntersectionInfo> intersections)
        {
            var unpaddedCurve = crossedObstacle.InputShape.BoundaryCurve;

            // Be a little more liberal than Curve.DistanceEpsilon or Curve.IntersectionEpsilon due to rounding issues.
            // This is especially important for corners due to the rounding combined with ellipse.
            if (intersections.Count < 2)
            {
                return new List<Point>(GetCurveTestPoints(path).Where(p => this.PointIsInsideCurveOrPadding(p, unpaddedCurve, StraightTolerance)));
            }

            var points = new List<Point>();
            for (int ii = 0; ii < intersections.Count - 1; ++ii)
            {
                var trimmedPath = path.Trim(intersections[ii].Par0, intersections[ii + 1].Par0);
                var curveDirections = PointComparer.GetDirections(
                        ApproximateComparer.Round(trimmedPath.Start), ApproximateComparer.Round(trimmedPath.End));
                var tolerance = CompassVector.IsPureDirection(curveDirections) ? StraightTolerance : CornerTolerance;
                points.AddRange(GetCurveTestPoints(trimmedPath).Where(p => this.PointIsInsideCurveOrPadding(p, unpaddedCurve, tolerance)));
            }
            return points;
        }

        private bool PointIsInsideCurveOrPadding(Point p, ICurve unpaddedCurve, double tolerance)
        {
            return Curve.PointRelativeToCurveLocation(p, unpaddedCurve) != PointLocation.Outside ||
                    (this.Padding - (p - Curve.ClosestPoint(unpaddedCurve, p)).Length) > tolerance;
        }

        // Get a set of points along the curve to test for containment.
        internal static IEnumerable<Point> GetCurveTestPoints(ICurve curve)
        {
            const int IntervalCount = 10;
            double del = (curve.ParEnd - curve.ParStart) / IntervalCount;
            for (var i = 0; i < IntervalCount; i++)
            {
                yield return curve[curve.ParStart + (i * del)];
            }
        }

        private void PrintFailurePoints(
            EdgeGeometry edgeGeom,
            Obstacle sourceObstacle,
            Obstacle targetObstacle,
            Obstacle crossedObstacle,
            IEnumerable<Point> points)
        {
            var message = String.Format("Path section [{0} -> {1}] crosses unexpected {2} [{3} {4}]",
                                        points.First(), points.Last(),
                                        crossedObstacle.IsGroup ? "group" : "obstacle",
                                        crossedObstacle.InputShape.BoundaryCurve.BoundingBox.Center, GetObstacleString(crossedObstacle));
            TestWriteLine(string.Format("Overlap error(s): {0}", message));
            foreach (var point in points)
            {
                TestWriteLine(string.Format("    {0}", point));
            }
            string sourceAndTargetString = GetSourceAndTargetString(edgeGeom, sourceObstacle, targetObstacle);
            TestWriteLine(string.Format("    {0}", sourceAndTargetString));
            Validate.Fail(string.Format("{0}: {1}", message, sourceAndTargetString));
        }

        private static string GetSourceAndTargetString(EdgeGeometry edgeGeom, Obstacle sourceObstacle, Obstacle targetObstacle)
        {
            return string.Format(
                "source [{0} {1}] target [{2} {3}]",
                edgeGeom.SourcePort.Location, GetObstacleString(sourceObstacle),
                edgeGeom.TargetPort.Location, GetObstacleString(targetObstacle));
        }

        private static string GetObstacleString(Obstacle obstacle)
        {
            if ((null == obstacle) || (null == obstacle.InputShape) || (null == obstacle.InputShape.UserData))
            {
                return RectFileStrings.NullStr;
            }
            return obstacle.InputShape.UserData.ToString();
        }

        private string GetSourceAndTargetString(EdgeGeometry edgeGeom)
        {
            ObstaclePort sourceOport, targetOport;
            PortManager.FindAncestorsAndObstaclePort(edgeGeom.SourcePort, out sourceOport);
            PortManager.FindAncestorsAndObstaclePort(edgeGeom.TargetPort, out targetOport);
            var sourceObstacle = (null == sourceOport) ? null : sourceOport.Obstacle;
            var targetObstacle = (null == targetOport) ? null : targetOport.Obstacle;
            return GetSourceAndTargetString(edgeGeom, sourceObstacle, targetObstacle);
        }
    }
}
