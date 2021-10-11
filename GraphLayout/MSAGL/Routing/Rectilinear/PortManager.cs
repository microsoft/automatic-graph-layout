//  
// PortManager.cs
// MSAGL class for Port management for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// This stores information mapping the App-level Ports (e.g. FloatingPort, RelativeFloatingPort,
    /// and MultiLocationFloatingPort) to the router's BasicScanPort subclasses (ObstaclePort and FreePoint).
    /// </summary>
    internal class PortManager {
        // The mapping of Msagl.Port (which may be MultiLocation) to the underlying Obstacle.Shape.
        private readonly Dictionary<Port, ObstaclePort> obstaclePortMap = new Dictionary<Port, ObstaclePort>();

        // The mapping of Msagl.Port.Location or a Waypoint to a FreePoint with visibility info.
        private readonly Dictionary<Point, FreePoint> freePointMap = new Dictionary<Point, FreePoint>();

        // This tracks which locations were used by the last call to RouteEdges, so we can remove unused locations.
        private readonly Set<Point> freePointLocationsUsedByRouteEdges = new Set<Point>();

        // Created to wrap the graph for adding transient vertices/edges to the graph.
        internal TransientGraphUtility TransUtil { get; set; }

        // Owned by RectilinearEdgeRouter.
        private readonly VisibilityGraphGenerator graphGenerator;

        // Storage and implementation of RectilinearEdgeRouter property of the same name.
        internal bool RouteToCenterOfObstacles { get; set; }

        // Extension of port visibility splices into the visibility graph.
        internal bool LimitPortVisibilitySpliceToEndpointBoundingBox 
        {
            get { return TransUtil.LimitPortVisibilitySpliceToEndpointBoundingBox; }
            set { TransUtil.LimitPortVisibilitySpliceToEndpointBoundingBox = value; }
        }

        // A control point is a source, target, or waypoint (terminology only, there's no ControlPoint
        // class).  These lists are the control points we've added for the current path.
        private readonly List<ObstaclePort> obstaclePortsInGraph = new List<ObstaclePort>();
        private readonly Set<FreePoint> freePointsInGraph = new Set<FreePoint>();

        // The limit for edge-chain extension.
        private Rectangle portSpliceLimitRectangle;

        // The current set of Obstacles that are groups whose boundaries are crossable.
        private readonly List<Obstacle> activeAncestors = new List<Obstacle>();

        // Typing shortcuts
        private VisibilityGraph VisGraph { get { return graphGenerator.VisibilityGraph; } }
        private ScanSegmentTree HScanSegments { get { return graphGenerator.HorizontalScanSegments; } }
        private ScanSegmentTree VScanSegments { get { return graphGenerator.VerticalScanSegments; } }
        private ObstacleTree ObstacleTree { get { return graphGenerator.ObsTree; } }
        private Dictionary<Shape, Set<Shape>> AncestorSets { get { return ObstacleTree.AncestorSets; } }

        internal PortManager(VisibilityGraphGenerator graphGenerator) {
            this.TransUtil = new TransientGraphUtility(graphGenerator);
            this.graphGenerator = graphGenerator;
        }

        internal void Clear() {
            TransUtil.RemoveFromGraph();    // Probably nothing in here when this is called
            obstaclePortMap.Clear();
        }

        internal void CreateObstaclePorts(Obstacle obstacle) {
            // Create ObstaclePorts for all Ports of this obstacle.  This just creates the
            // ObstaclePort object; we don't add its edges/vertices to the graph until we
            // do the actual routing.
            foreach (var port in obstacle.Ports) {
                CreateObstaclePort(obstacle, port);
            }
        }

        private ObstaclePort CreateObstaclePort(Obstacle obstacle, Port port) {
            // This will replace any previous specification for the port (last one wins).
            Debug.Assert(!obstaclePortMap.ContainsKey(port), "Port is used by more than one obstacle");

            if (port.Curve==null) {
                return null;
            }

            var roundedLocation = ApproximateComparer.Round(port.Location);
            if (PointLocation.Outside == Curve.PointRelativeToCurveLocation(roundedLocation, obstacle.InputShape.BoundaryCurve)) {
                // Obstacle.Port is outside Obstacle.Shape; handle it as a FreePoint.
                return null;
            }

            if ((obstacle.InputShape.BoundaryCurve != port.Curve) 
                && (PointLocation.Outside == Curve.PointRelativeToCurveLocation(roundedLocation, port.Curve))) {
                // Obstacle.Port is outside port.Curve; handle it as a FreePoint.
                return null;
            }

            var oport = new ObstaclePort(port, obstacle);
            obstaclePortMap[port] = oport;
            return oport;
        }

        internal List<VisibilityVertex> FindVertices(Port port) {
            var vertices = new List<VisibilityVertex>();
            ObstaclePort oport;
            if (obstaclePortMap.TryGetValue(port, out oport)) {
                if (RouteToCenterOfObstacles) {
                    vertices.Add(oport.CenterVertex);
                }
                else {
                    // Add all vertices on the obstacle borders.  Avoid LINQ for performance.
                    foreach (var entrance in oport.PortEntrances) {
                        VisibilityVertex vertex = this.VisGraph.FindVertex(entrance.UnpaddedBorderIntersect);
                        if (vertex != null) {
                            vertices.Add(vertex);
                        }
                    }
                }
            }
            else {
                vertices.Add(VisGraph.FindVertex(ApproximateComparer.Round(port.Location)));
            }
            return vertices;
        }

        internal void RemoveObstaclePorts(Obstacle obstacle) {
            foreach (var port in obstacle.Ports) {
                // Since we remove the port from the visibility graph after each routing, all we
                // have to do here is remove it from the dictionary.
                RemoveObstaclePort(port);
            }
        }

        void RemoveObstaclePort(Port port) {
            obstaclePortMap.Remove(port);
        }

        // Add path control points - source, target, and any waypoints.
        internal void AddControlPointsToGraph(EdgeGeometry edgeGeom, Dictionary<Shape, Obstacle> shapeToObstacleMap) {
            this.GetPortSpliceLimitRectangle(edgeGeom);
            activeAncestors.Clear();

            ObstaclePort sourceOport, targetOport;
            var ssAncs = FindAncestorsAndObstaclePort(edgeGeom.SourcePort, out sourceOport);
            var ttAncs = FindAncestorsAndObstaclePort(edgeGeom.TargetPort, out targetOport);

            if ((AncestorSets.Count > 0) && (sourceOport != null) && (targetOport != null)) {
                // Make non-common ancestors' boundaries transparent (we don't want to route outside common ancestors).
                var ttAncsOnly = ttAncs - ssAncs;
                var ssAncsOnly = ssAncs - ttAncs;
                ActivateAncestors(ssAncsOnly, ttAncsOnly, shapeToObstacleMap);
            }

            // Now that we've set any active ancestors, splice in the port visibility.
            AddPortToGraph(edgeGeom.SourcePort, sourceOport);
            AddPortToGraph(edgeGeom.TargetPort, targetOport);            
        }

        
        private void ConnectOobWaypointToEndpointVisibilityAtGraphBoundary(FreePoint oobWaypoint, Port port) {
            if ((oobWaypoint == null) || !oobWaypoint.IsOutOfBounds) {
                return;
            }

            // Connect to the graphbox side at points collinear with the vertices.  The waypoint may be
            // OOB in two directions so call once for each axis.
            var endpointVertices = this.FindVertices(port);
            var dirFromGraph = oobWaypoint.OutOfBoundsDirectionFromGraph & (Direction.North | Direction.South);
            ConnectToGraphAtPointsCollinearWithVertices(oobWaypoint, dirFromGraph, endpointVertices);
            dirFromGraph = oobWaypoint.OutOfBoundsDirectionFromGraph & (Direction.East | Direction.West);
            ConnectToGraphAtPointsCollinearWithVertices(oobWaypoint, dirFromGraph, endpointVertices);
        }

        private void ConnectToGraphAtPointsCollinearWithVertices(FreePoint oobWaypoint, Direction dirFromGraph,
                    List<VisibilityVertex> endpointVertices) {
            if (Direction. None == dirFromGraph) {
                // Not out of bounds on this axis.
                return;
            }
            var dirToGraph = CompassVector.OppositeDir(dirFromGraph);
            foreach (var vertex in endpointVertices) {
                var graphBorderLocation = this.InBoundsGraphBoxIntersect(vertex.Point, dirFromGraph);
                var graphBorderVertex = this.VisGraph.FindVertex(graphBorderLocation);
                if (graphBorderVertex != null) {
                    this.TransUtil.ConnectVertexToTargetVertex(oobWaypoint.Vertex, graphBorderVertex, dirToGraph, ScanSegment.NormalWeight);
                }
            }
        }

        internal bool SetAllAncestorsActive(EdgeGeometry edgeGeom, Dictionary<Shape, Obstacle> shapeToObstacleMap) {
            if (0 == AncestorSets.Count) {
                return false;
            }
            ObstacleTree.AdjustSpatialAncestors();

            ClearActiveAncestors();

            ObstaclePort sourceOport, targetOport;
            var ssAncs = FindAncestorsAndObstaclePort(edgeGeom.SourcePort, out sourceOport);
            var ttAncs = FindAncestorsAndObstaclePort(edgeGeom.TargetPort, out targetOport);

            if ((AncestorSets.Count > 0) && (ssAncs != null) && (ttAncs != null)) {
                // Make all ancestors boundaries transparent; in this case we've already tried with only
                // non-common and found no path, so perhaps an obstacle is outside its parent group's bounds.
                ActivateAncestors(ssAncs, ttAncs, shapeToObstacleMap);
                return true;
            }
            return false;
        }

        internal void SetAllGroupsActive() {
            // We couldn't get a path when we activated all hierarchical and spatial group ancestors of the shapes,
            // so assume we may be landlocked and activate all groups, period.
            ClearActiveAncestors();
            foreach (var group in ObstacleTree.GetAllGroups()) {
                group.IsTransparentAncestor = true;
                activeAncestors.Add(group);
            }
        }

        internal Set<Shape> FindAncestorsAndObstaclePort(Port port, out ObstaclePort oport) {
            oport = FindObstaclePort(port);
            if (0 == AncestorSets.Count) {
                return null;
            }
            if (oport != null) {
                return AncestorSets[oport.Obstacle.InputShape];
            }

            // This is a free Port (not associated with an obstacle) or a Waypoint; return all spatial parents.
            return new Set<Shape>(ObstacleTree.Root.AllHitItems(new Rectangle(port.Location, port.Location), shape => shape.IsGroup)
                                        .Select(obs => obs.InputShape));
        }

        private void ActivateAncestors(Set<Shape> ssAncsToUse, Set<Shape> ttAncsToUse, Dictionary<Shape, Obstacle> shapeToObstacleMap) {
            foreach (var shape in ssAncsToUse + ttAncsToUse) {
                var group = shapeToObstacleMap[shape];
                Debug.Assert(group.IsGroup, "Ancestor shape is not a group");
                group.IsTransparentAncestor = true;
                activeAncestors.Add(group);
            }
        }

        void ClearActiveAncestors() {
            foreach (var group in activeAncestors) {
                group.IsTransparentAncestor = false;
            }
            activeAncestors.Clear();
        }

        internal void RemoveControlPointsFromGraph() {
            ClearActiveAncestors();
            RemoveObstaclePortsFromGraph();
            RemoveFreePointsFromGraph();
            TransUtil.RemoveFromGraph();
            this.portSpliceLimitRectangle = new Rectangle();
        }

        private void RemoveObstaclePortsFromGraph() {
            foreach (var oport in this.obstaclePortsInGraph) {
                oport.RemoveFromGraph();
            }
            this.obstaclePortsInGraph.Clear();
        }

        private void RemoveFreePointsFromGraph() {
            foreach (var freePoint in this.freePointsInGraph) {
                freePoint.RemoveFromGraph();
            }
            this.freePointsInGraph.Clear();
        }

        private void RemoveStaleFreePoints() {
            // FreePoints are not necessarily persistent - they may for example be waypoints which are removed.
            // So after every routing pass, remove any that were not added to the graph. Because the FreePoint has
            // be removed from the graph, its Vertex (and thus Point) are no longer set in the FreePoint, so we
            // must use the key from the dictionary.
            if (this.freePointMap.Count > this.freePointLocationsUsedByRouteEdges.Count) {
                var staleFreePairs = this.freePointMap.Where(kvp => !this.freePointLocationsUsedByRouteEdges.Contains(kvp.Key)).ToArray();
                foreach (var staleFreePair in staleFreePairs) {
                    this.freePointMap.Remove(staleFreePair.Key);
                }
            }
        }

        internal void ClearVisibility() {
            // Most of the retained freepoint stuff is about precalculated visibility.
            this.freePointMap.Clear();
            foreach (var oport in obstaclePortMap.Values) {
                oport.ClearVisibility();
            }
        }

        internal void BeginRouteEdges() {
            this.RemoveControlPointsFromGraph(); // ensure there are no leftovers
            this.freePointLocationsUsedByRouteEdges.Clear();
        }

        internal void EndRouteEdges() {
            this.RemoveStaleFreePoints();
        }

        internal ObstaclePort FindObstaclePort(Port port) {
            ObstaclePort oport;
            if (obstaclePortMap.TryGetValue(port, out oport)) {
                // First see if the obstacle's port list has changed without UpdateObstacles() being called.
                // Unfortunately we don't have a way to update the obstacle's ports until we enter
                // this block; there is no direct Port->Shape/Obstacle mapping.  So UpdateObstacle must still
                // be called, but at least this check here will remove obsolete ObstaclePorts.
                Set<Port> addedPorts, removedPorts;
                if (oport.Obstacle.GetPortChanges(out addedPorts, out removedPorts)) {
                    foreach (var newPort in addedPorts) {
                        CreateObstaclePort(oport.Obstacle, newPort);
                    }
                    foreach (var oldPort in removedPorts) {
                        RemoveObstaclePort(oldPort);
                    }

                    // If it's not still there, it was moved outside the obstacle so we'll just add it as a FreePoint.
                    if (!obstaclePortMap.TryGetValue(port, out oport)) {
                        oport = null;
                    }
                }
            }
            return oport;
        }


        private void AddPortToGraph(Port port, ObstaclePort oport) {
            if (oport != null) {
                AddObstaclePortToGraph(oport);
                return;
            }

            // This is a FreePoint, either a Waypoint or a Port not in an Obstacle.Ports list.
            AddFreePointToGraph(port.Location);
        }

        private void AddObstaclePortToGraph(ObstaclePort oport) {
            // If the port's position has changed without UpdateObstacles() being called, recreate it.
            if (oport.LocationHasChanged) {
                RemoveObstaclePort(oport.Port);
                oport = CreateObstaclePort(oport.Obstacle, oport.Port);
                if ( oport == null)
                {
                    // Port has been moved outside obstacle; return and let caller add it as a FreePoint.
                    return;
                }
            }
            oport.AddToGraph(TransUtil, RouteToCenterOfObstacles);
            obstaclePortsInGraph.Add(oport);

            this.CreateObstaclePortEntrancesIfNeeded(oport);

            // We've determined the entrypoints on the obstacle boundary for each PortEntry,
            // so now add them to the VisGraph.
            foreach (var entrance in oport.PortEntrances) {
                AddObstaclePortEntranceToGraph(entrance);
            }
            return;
        }

        private void CreateObstaclePortEntrancesIfNeeded(ObstaclePort oport) {
            if (oport.PortEntrances.Count > 0) {
                return;
            }
            
            // Create the PortEntrances with initial information:  border intersect and outer edge direction.
                this.CreateObstaclePortEntrancesFromPoints(oport);
            
        }

        public Point[] GetPortVisibilityIntersection(EdgeGeometry edgeGeometry) {
            var sourceOport = this.FindObstaclePort(edgeGeometry.SourcePort);
            var targetOport = this.FindObstaclePort(edgeGeometry.TargetPort);
            if ((sourceOport == null) || (targetOport == null)) {
                return null;
            }
            if (sourceOport.Obstacle.IsInConvexHull || targetOport.Obstacle.IsInConvexHull) {
                return null;
            }
            this.CreateObstaclePortEntrancesIfNeeded(sourceOport);
            this.CreateObstaclePortEntrancesIfNeeded(targetOport);
            if (!sourceOport.VisibilityRectangle.Intersects(targetOport.VisibilityRectangle)) {
                return null;
            }
            foreach (var sourceEntrance in sourceOport.PortEntrances) {
                if (!sourceEntrance.WantVisibilityIntersection) {
                    continue;
                }
                foreach (var targetEntrance in targetOport.PortEntrances) {
                    if (!targetEntrance.WantVisibilityIntersection) {
                        continue;
                    }
                    var points = (sourceEntrance.IsVertical == targetEntrance.IsVertical)
                                ? GetPathPointsFromOverlappingCollinearVisibility(sourceEntrance, targetEntrance)
                                : GetPathPointsFromIntersectingVisibility(sourceEntrance, targetEntrance);
                    if (points != null) {
                        return points;
                    }
                }
            }
            return null;
        }

        private static Point[] GetPathPointsFromOverlappingCollinearVisibility(ObstaclePortEntrance sourceEntrance, ObstaclePortEntrance targetEntrance) {
            // If the segments are the same they'll be in reverse.  Note: check for IntervalsOverlap also, if we support FreePoints here.
            if (!StaticGraphUtility.IntervalsAreSame(sourceEntrance.MaxVisibilitySegment.Start, sourceEntrance.MaxVisibilitySegment.End,
                                                     targetEntrance.MaxVisibilitySegment.End, targetEntrance.MaxVisibilitySegment.Start)) {
                return null;
            }
            if (sourceEntrance.HasGroupCrossings || targetEntrance.HasGroupCrossings) {
                return null;
            }
            if (PointComparer.Equal(sourceEntrance.UnpaddedBorderIntersect, targetEntrance.UnpaddedBorderIntersect)) {
                // Probably one obstacle contained within another; we handle that elsewhere.
                return null;
            }
            return new[] {
                    sourceEntrance.UnpaddedBorderIntersect,
                    targetEntrance.UnpaddedBorderIntersect
            };
        }

        private static Point[] GetPathPointsFromIntersectingVisibility(ObstaclePortEntrance sourceEntrance, ObstaclePortEntrance targetEntrance) {
            Point intersect;
            if (!StaticGraphUtility.SegmentsIntersect(sourceEntrance.MaxVisibilitySegment, targetEntrance.MaxVisibilitySegment, out intersect)) {
                return null;
            }
            if (sourceEntrance.HasGroupCrossingBeforePoint(intersect) || targetEntrance.HasGroupCrossingBeforePoint(intersect)) {
                return null;
            }
            return new[] {
                    sourceEntrance.UnpaddedBorderIntersect,
                    intersect,
                    targetEntrance.UnpaddedBorderIntersect
            };
        }

        private void CreateObstaclePortEntrancesFromPoints(ObstaclePort oport) {
            var graphBox = graphGenerator.ObsTree.GraphBox;
            var curveBox = new Rectangle(ApproximateComparer.Round(oport.PortCurve.BoundingBox.LeftBottom)
                                             , ApproximateComparer.Round(oport.PortCurve.BoundingBox.RightTop));

            // This Port does not have a PortEntry, so we'll have visibility edges to its location
            // in the Horizontal and Vertical directions (possibly all 4 directions, if not on boundary).
            //
            // First calculate the intersection with the obstacle in all directions.  Do nothing in the
            // horizontal direction for port locations that are on the unpadded vertical extremes, because
            // this will have a path that moves alongside a rectilinear obstacle side in less than the
            // padding radius and will thus create the PaddedBorderIntersection on the side rather than top
            // (and vice-versa for the vertical direction).  We'll have an edge in the vertical direction
            // to the padded extreme boundary ScanSegment, and the Nudger will modify paths as appropriate
            // to remove unnecessary bends.
            
            // Use the unrounded port location to intersect with its curve.
            Point location = ApproximateComparer.Round(oport.PortLocation);
            Point xx0, xx1;
            bool found = false;
            if (!PointComparer.Equal(location.Y, curveBox.Top)
                    && !PointComparer.Equal(location.Y, curveBox.Bottom)) {
                found = true;
                var hSeg = new LineSegment(graphBox.Left, location.Y, graphBox.Right, location.Y);
                GetBorderIntersections(location, hSeg, oport.PortCurve, out xx0, out xx1);
                var wBorderIntersect = new Point(Math.Min(xx0.X, xx1.X), location.Y);
                if (wBorderIntersect.X < curveBox.Left) {        // Handle rounding error
                    wBorderIntersect.X = curveBox.Left;
                }
                var eBorderIntersect = new Point(Math.Max(xx0.X, xx1.X), location.Y);
                if (eBorderIntersect.X > curveBox.Right) {
                    eBorderIntersect.X = curveBox.Right;
                }
                CreatePortEntrancesAtBorderIntersections(curveBox, oport, location, wBorderIntersect, eBorderIntersect);
            } // endif horizontal pass is not at vertical extreme

            if (!PointComparer.Equal(location.X, curveBox.Left)
                    && !PointComparer.Equal(location.X, curveBox.Right)) {
                found = true;
                var vSeg = new LineSegment(location.X, graphBox.Bottom, location.X, graphBox.Top);
                GetBorderIntersections(location, vSeg, oport.PortCurve, out xx0, out xx1);
                var sBorderIntersect = new Point(location.X, Math.Min(xx0.Y, xx1.Y));
                if (sBorderIntersect.Y < graphBox.Bottom) {      // Handle rounding error
                    sBorderIntersect.Y = graphBox.Bottom;
                }
                var nBorderIntersect = new Point(location.X, Math.Max(xx0.Y, xx1.Y));
                if (nBorderIntersect.Y > graphBox.Top) {
                    nBorderIntersect.Y = graphBox.Top;
                }
                CreatePortEntrancesAtBorderIntersections(curveBox, oport, location, sBorderIntersect, nBorderIntersect);
            } // endif vertical pass is not at horizontal extreme

            if (!found) {
                // This must be on a corner, else one of the above would have matched.
                this.CreateEntrancesForCornerPort(curveBox, oport, location);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private void GetBorderIntersections(Point location, LineSegment lineSeg, ICurve curve
                                    , out Point xx0, out Point xx1) {
            // Important:  the LineSegment must be the first arg to GetAllIntersections so RawIntersection works.
            IList<IntersectionInfo> xxs = Curve.GetAllIntersections(lineSeg, curve, true /*liftIntersections*/);
            StaticGraphUtility.Assert(2 == xxs.Count, "Expected two intersections", this.ObstacleTree, this.VisGraph);
            xx0 = ApproximateComparer.Round(xxs[0].IntersectionPoint);
            xx1 = ApproximateComparer.Round(xxs[1].IntersectionPoint);
        }

        private void CreatePortEntrancesAtBorderIntersections(Rectangle curveBox, ObstaclePort oport, Point location
                                    , Point unpaddedBorderIntersect0, Point unpaddedBorderIntersect1) {
            // Allow entry from both sides, except from the opposite side of a point on the border.
            Direction dir = PointComparer.GetPureDirection(unpaddedBorderIntersect0, unpaddedBorderIntersect1);
            if (!PointComparer.Equal(unpaddedBorderIntersect0, location)) {
                CreatePortEntrance(curveBox, oport, unpaddedBorderIntersect1, dir);
            }
            if (!PointComparer.Equal(unpaddedBorderIntersect1, location)) {
                CreatePortEntrance(curveBox, oport, unpaddedBorderIntersect0, CompassVector.OppositeDir(dir));
            }
        }

        private static Point GetDerivative(ObstaclePort oport, Point borderPoint) {
            // This is only used for ObstaclePorts, which have ensured Port.Curve is not null.
            double param = oport.PortCurve.ClosestParameter(borderPoint);
            var deriv = oport.PortCurve.Derivative(param);
            var parMid = (oport.PortCurve.ParStart + oport.PortCurve.ParEnd) / 2;
            if (!InteractiveObstacleCalculator.CurveIsClockwise(oport.PortCurve, oport.PortCurve[parMid])) {
                deriv = -deriv;
            }
            return deriv;
        }

        private void CreatePortEntrance(Rectangle curveBox, ObstaclePort oport, Point unpaddedBorderIntersect, Direction outDir) {
            oport.CreatePortEntrance(unpaddedBorderIntersect, outDir, this.ObstacleTree);
            ScanDirection scanDir = ScanDirection.GetInstance(outDir);
            double axisDistanceBetweenIntersections = StaticGraphUtility.GetRectangleBound(curveBox, outDir) - scanDir.Coord(unpaddedBorderIntersect);
            if (axisDistanceBetweenIntersections < 0.0) {
                axisDistanceBetweenIntersections = -axisDistanceBetweenIntersections;
            }
            if (axisDistanceBetweenIntersections > ApproximateComparer.IntersectionEpsilon) {
                // This is not on an extreme boundary of the unpadded curve (it's on a sloping (nonrectangular) boundary),
                // so we need to generate another entrance in one of the perpendicular directions (depending on which
                // way the side slopes).  Derivative is always clockwise.
                Direction perpDirs = CompassVector.VectorDirection(GetDerivative(oport, unpaddedBorderIntersect));
                Direction perpDir = perpDirs & ~(outDir | CompassVector.OppositeDir(outDir));
                if (Direction. None != (outDir & perpDirs)) {
                    // If the derivative is in the same direction as outDir then perpDir is toward the obstacle
                    // interior and must be reversed.
                    perpDir = CompassVector.OppositeDir(perpDir);
                }
                oport.CreatePortEntrance(unpaddedBorderIntersect, perpDir, this.ObstacleTree);
            }
        }

        private void CreateEntrancesForCornerPort(Rectangle curveBox, ObstaclePort oport, Point location) {
            // This must be a corner or it would have been within one of the bounds and handled elsewhere.
            // Therefore create an entrance in both directions, with the first direction selected so that
            // the second can be obtained via RotateRight.
            Direction outDir = Direction.North;
            if (PointComparer.Equal(location, curveBox.LeftBottom)) {
                outDir = Direction.South;
            }
            else if (PointComparer.Equal(location, curveBox.LeftTop)) {
                outDir = Direction.West;
            }
            else if (PointComparer.Equal(location, curveBox.RightTop)) {
                outDir = Direction.North;
            }
            else if (PointComparer.Equal(location, curveBox.RightBottom)) {
                outDir = Direction.East;
            }
            else {
                Debug.Assert(false, "Expected Port to be on corner of curveBox");
            }
            oport.CreatePortEntrance(location, outDir, this.ObstacleTree);
            oport.CreatePortEntrance(location, CompassVector.RotateRight(outDir), this.ObstacleTree);
        }

        private void AddObstaclePortEntranceToGraph(ObstaclePortEntrance entrance) {
            // Note: As discussed in ObstaclePortEntrance.AddToGraph, oport.VisibilityBorderIntersect may be
            // on a border shared with another obstacle, in which case we'll extend into that obstacle.  This
            // should be fine if we're consistent about "touching means overlapped", so that a path that comes
            // through the other obstacle on the shared border is OK.
            VisibilityVertex borderVertex = VisGraph.FindVertex(entrance.VisibilityBorderIntersect);
            if (borderVertex != null) {
                entrance.ExtendEdgeChain(TransUtil, borderVertex, borderVertex, this.portSpliceLimitRectangle, RouteToCenterOfObstacles);
                return;
            }

            // There may be no scansegment to splice to before we hit an adjacent obstacle, so if the edge 
            // is null there is nothing to do.
            VisibilityVertex targetVertex;
            double weight = entrance.IsOverlapped ? ScanSegment.OverlappedWeight : ScanSegment.NormalWeight;
            VisibilityEdge edge = this.FindorCreateNearestPerpEdge(entrance.MaxVisibilitySegment.End, entrance.VisibilityBorderIntersect,
                    entrance.OutwardDirection, weight /*checkForObstacle*/, out targetVertex);
            if (edge != null) {
                entrance.AddToAdjacentVertex(TransUtil, targetVertex, this.portSpliceLimitRectangle, RouteToCenterOfObstacles);
            }
        }

        private Point InBoundsGraphBoxIntersect(Point point, Direction dir) {
            return StaticGraphUtility.RectangleBorderIntersect(graphGenerator.ObsTree.GraphBox, point, dir);
        }

        private VisibilityEdge FindorCreateNearestPerpEdge(Point first, Point second, Direction dir, double weight) {
            VisibilityVertex targetVertex;
            return this.FindorCreateNearestPerpEdge(first, second, dir, weight, out targetVertex);
        }

        private VisibilityEdge FindorCreateNearestPerpEdge(Point first, Point second, Direction dir, double weight, out VisibilityVertex targetVertex) {
            // Find the closest perpendicular ScanSegment that intersects a segment with endpoints
            // first and second, then find the closest parallel ScanSegment that intersects that
            // perpendicular ScanSegment.  This gives us a VisibilityVertex location from which we
            // can walk to the closest perpendicular VisibilityEdge that intersects first->second.
            Tuple<Point, Point> couple = StaticGraphUtility.SortAscending(first, second);
            Point low = couple.Item1;
            Point high = couple.Item2;
            ScanSegmentTree perpendicularScanSegments = StaticGraphUtility.IsVertical(dir) ? HScanSegments : VScanSegments;

            // Look up the nearest intersection.  For obstacles, we cannot just look for the bounding box
            // corners because nonrectilinear obstacles may have other obstacles overlapping the bounding
            // box (at either the corners or between the port border intersection and the bounding box
            // side), and of course obstacles may overlap too.
            ScanSegment nearestPerpSeg = StaticGraphUtility.IsAscending(dir) 
                            ? perpendicularScanSegments.FindLowestIntersector(low, high) 
                            : perpendicularScanSegments.FindHighestIntersector(low, high);

            if (nearestPerpSeg == null) {
                // No ScanSegment between this and visibility limits.
                targetVertex = null;
                return null;
            }
            Point edgeIntersect = StaticGraphUtility.SegmentIntersection(nearestPerpSeg, low);

            // We now know the nearest perpendicular segment that intersects start->end.  Next we'll find a close
            // parallel scansegment that intersects the perp segment, then walk to find the nearest perp edge.
            return FindOrCreateNearestPerpEdgeFromNearestPerpSegment(StaticGraphUtility.IsAscending(dir) ? low : high,
                            nearestPerpSeg, edgeIntersect, weight, out targetVertex);
        }

        private VisibilityEdge FindOrCreateNearestPerpEdgeFromNearestPerpSegment(Point pointLocation, ScanSegment scanSeg,
                            Point edgeIntersect, double weight, out VisibilityVertex targetVertex) {
            // Given: a ScanSegment scanSeg perpendicular to pointLocation->edgeIntersect and containing edgeIntersect.
            // To find: a VisibilityEdge perpendicular to pointLocation->edgeIntersect which may be on scanSeg, or may
            //          be closer to pointLocation than the passed edgeIntersect is.
            // Since there may be TransientEdges between pointLocation and edgeIntersect, we start by finding
            // a scanSeg-intersecting (i.e. parallel to pointLocation->edgeIntersect) ScanSegment, then starting from
            // the intersection of those segments, walk the VisibilityGraph until we find the closest VisibilityEdge
            // perpendicular to pointLocation->edgeIntersect.  If there is a vertex on that edge collinear to
            // pointLocation->edgeIntersect, return the edge for which it is Source, else split the edge.

            // If there is already a vertex at edgeIntersect, we do not need to look for the intersecting ScanSegment.
            VisibilityVertex segsegVertex = VisGraph.FindVertex(edgeIntersect);
            if ( segsegVertex == null) {
                var edge = this.FindOrCreateSegmentIntersectionVertexAndAssociatedEdge(pointLocation, edgeIntersect, scanSeg, weight,
                            out segsegVertex, out targetVertex);
                if (edge != null) {
                    return edge;
                }
            }
            else if (PointComparer.Equal(pointLocation, edgeIntersect)) {
                // The initial pointLocation was on scanSeg at an existing vertex so return an edge
                // from that vertex along scanSeg. Look in both directions in case of dead ends.
                targetVertex = segsegVertex;
                return TransUtil.FindNextEdge(targetVertex, scanSeg.ScanDirection.Direction)
                    ?? TransUtil.FindNextEdge(targetVertex, CompassVector.OppositeDir(scanSeg.ScanDirection.Direction));
            }

            // pointLocation is not on the initial scanSeg, so see if there is a transient edge between
            // pointLocation and edgeIntersect.  edgeIntersect == segsegVertex.Point if pointLocation is
            // collinear with intSegBefore (pointLocation is before or after intSegBefore's VisibilityVertices).
            Direction dirTowardLocation = PointComparer.GetPureDirection(edgeIntersect, pointLocation);
            Direction perpDir = PointComparer.GetDirections(segsegVertex.Point, pointLocation);
            if (dirTowardLocation == perpDir) {
                // intSegBefore is collinear with pointLocation so walk to the vertex closest to pointLocation.
                VisibilityVertex bracketTarget;
                TransientGraphUtility.FindBracketingVertices(segsegVertex, pointLocation, dirTowardLocation
                                    , out targetVertex, out bracketTarget);

                // Return an edge. Look in both directions in case of dead ends.
                return TransUtil.FindNextEdge(targetVertex, CompassVector.RotateLeft(dirTowardLocation))
                    ?? TransUtil.FindNextEdge(targetVertex, CompassVector.RotateRight(dirTowardLocation));
            }

            // Now make perpDir have only the perpendicular component.
            perpDir &= ~dirTowardLocation;              // if this is Directions. None, pointLocation == edgeIntersect
            StaticGraphUtility.Assert(Direction. None != perpDir
                    , "pointLocation == initial segsegVertex.Point should already have exited", ObstacleTree, VisGraph);

            // Other TransientVE edge chains may have been added between the control point and the
            // ScanSegment (which is always non-transient), and they may have split ScanSegment VEs.
            // Fortunately we know we'll always have all transient edge chains extended to or past any
            // control point (due to LimitRectangle), so we can just move up lowestIntSeg toward
            // pointLocation, updating segsegVertex and edgeIntersect.  There are 3 possibilities:
            //  - location is not on an edge - the usual case, we just create an edge perpendicular
            //    to an edge on scanSeg, splitting that scanSeg edge in the process.
            //  - location is on a VE that is parallel to scanSeg.  This is essentially the same thing
            //    but we don't need the first perpendicular edge to scanSeg.
            //  - location is on a VE that is perpendicular to scanSeg.  In that case the vertex on ScanSeg
            //    already exists; TransUtil.FindOrAddEdge just returns the edge starting at that intersection.
            // FreePoint tests of this are in RectilinearTests.FreePortLocationRelativeToTransientVisibilityEdges*.
            VisibilityEdge perpendicularEdge = TransUtil.FindNearestPerpendicularOrContainingEdge(segsegVertex, perpDir, pointLocation);
            if ( perpendicularEdge == null) {
                // Dead end; we're above the highest point at which there is an intersection of scanSeg.
                // Create a new vertex and edge higher than the ScanSegment's HighestVisibilityVertex
                // if that doesn't cross an obstacle (if we are between two ScanSegment dead-ends, we may).
                // We hit this in RectilinearFileTests.Nudger_Many_Paths_In_Channel and .Nudger_Overlap*.
                StaticGraphUtility.Assert(edgeIntersect > scanSeg.HighestVisibilityVertex.Point
                            , "edgeIntersect is not > scanSeg.HighestVisibilityVertex", ObstacleTree, VisGraph);
                targetVertex = TransUtil.AddVertex(edgeIntersect);
                return TransUtil.FindOrAddEdge(targetVertex, scanSeg.HighestVisibilityVertex, scanSeg.Weight);
            }

            // We have an intersecting perp edge, which may be on the original scanSeg or closer to pointLocation.
            // Get one of its vertices and re-find the intersection on it (it doesn't matter which vertex of the
            // edge we use, but for consistency use the "lower in perpDir" one).
            segsegVertex = StaticGraphUtility.GetEdgeEnd(perpendicularEdge, CompassVector.OppositeDir(perpDir));
            edgeIntersect = StaticGraphUtility.SegmentIntersection(pointLocation, edgeIntersect, segsegVertex.Point);
            
            // By this point we've verified there's no intervening Transient edge, so if we have an identical
            // point, we're done.  
            if (PointComparer.Equal(segsegVertex.Point, edgeIntersect)) {
                targetVertex = segsegVertex;
                return TransUtil.FindNextEdge(segsegVertex, perpDir);
            }

            // The targetVertex doesn't exist; this will split the edge and add it.
            targetVertex = TransUtil.FindOrAddVertex(edgeIntersect);
            return TransUtil.FindOrAddEdge(segsegVertex, targetVertex, weight);
        }

        private VisibilityEdge FindOrCreateSegmentIntersectionVertexAndAssociatedEdge(Point pointLocation, Point edgeIntersect, ScanSegment scanSeg,
                                            double weight, out VisibilityVertex segsegVertex, out VisibilityVertex targetVertex) {
            ScanSegmentTree intersectingSegments = scanSeg.IsVertical ? this.HScanSegments : this.VScanSegments;
            ScanSegment intSegBefore = intersectingSegments.FindHighestIntersector(scanSeg.Start, edgeIntersect);
            if ( intSegBefore == null) {
                // Dead end; we're below the lowest point at which there is an intersection of scanSeg.
                // Create a new vertex and edge lower than the ScanSegment's LowestVisibilityVertex.
                // Test: RectilinearFileTests.Overlap_Rotate_SplicePort_FreeObstaclePorts.
                segsegVertex = null;
                targetVertex = this.TransUtil.AddVertex(edgeIntersect);
                return this.TransUtil.FindOrAddEdge(targetVertex, scanSeg.LowestVisibilityVertex, scanSeg.Weight);
            }

            // Get the VisibilityVertex at the intersection of the two segments we just found;
            // edgeIntersect is between that vertex and another on the segment, and we'll split
            // the edge between those two vertices (or find one nearer to walk to).
            Point segsegIntersect = StaticGraphUtility.SegmentIntersection(scanSeg, intSegBefore);
            segsegVertex = this.VisGraph.FindVertex(segsegIntersect);
            if ( segsegVertex == null) {
                // This happens only for UseSparseVisibilityGraph; in that case we must create the
                // intersection vertex in the direction of both segments so we can start walking.
                segsegVertex = this.TransUtil.AddVertex(segsegIntersect);
                var newEdge = this.AddEdgeToClosestSegmentEnd(scanSeg, segsegVertex, scanSeg.Weight);
                this.AddEdgeToClosestSegmentEnd(intSegBefore, segsegVertex, intSegBefore.Weight);
                if (PointComparer.Equal(segsegVertex.Point, edgeIntersect)) {
                    targetVertex = segsegVertex;
                    return newEdge;
                }
            }

            if (PointComparer.Equal(pointLocation, edgeIntersect)) {
                // The initial pointLocation was on scanSeg and we had to create a new vertex for it,
                // so we'll find or create (by splitting) the edge on scanSeg that contains pointLocation.
                targetVertex = this.TransUtil.FindOrAddVertex(edgeIntersect);
                return this.TransUtil.FindOrAddEdge(segsegVertex, targetVertex, weight);
            }
            targetVertex = null;
            return null;
        }

        private VisibilityEdge AddEdgeToClosestSegmentEnd(ScanSegment scanSeg, VisibilityVertex segsegVertex, double weight) {
            // FindOrAddEdge will walk until it finds the minimal bracketing vertices.
            if (PointComparer.IsPureLower(scanSeg.HighestVisibilityVertex.Point, segsegVertex.Point)) {
                return this.TransUtil.FindOrAddEdge(scanSeg.HighestVisibilityVertex, segsegVertex, weight);
            }
            if (PointComparer.IsPureLower(segsegVertex.Point, scanSeg.LowestVisibilityVertex.Point)) {
                return this.TransUtil.FindOrAddEdge(segsegVertex, scanSeg.LowestVisibilityVertex, weight);
            }
            return this.TransUtil.FindOrAddEdge(scanSeg.LowestVisibilityVertex, segsegVertex);
        }

        private void GetPortSpliceLimitRectangle(EdgeGeometry edgeGeom) {
            if (!this.LimitPortVisibilitySpliceToEndpointBoundingBox) {
                this.portSpliceLimitRectangle = graphGenerator.ObsTree.GraphBox;
                return;
            }

            // Return the endpoint-containing rectangle marking the limits of edge-chain extension for a single path.
            this.portSpliceLimitRectangle = GetPortRectangle(edgeGeom.SourcePort);
            this.portSpliceLimitRectangle.Add(GetPortRectangle(edgeGeom.TargetPort));
            
        }

        Rectangle GetPortRectangle(Port port)
        {
            ObstaclePort oport;
            obstaclePortMap.TryGetValue(port, out oport);
            if (oport != null) {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=369 there are no structs in js
                return (oport.Obstacle.VisibilityBoundingBox).Clone();
#else
                return (oport.Obstacle.VisibilityBoundingBox);
#endif
            }

            // FreePoint.
            return new Rectangle(ApproximateComparer.Round(port.Location));
        }

        void AddToLimitRectangle(Point location)
        {
            if (graphGenerator.IsInBounds(location))
            {
                this.portSpliceLimitRectangle.Add(location);
            }
        }

        internal IEnumerable<VisibilityVertex> FindWaypointVertices(IEnumerable<Point> waypoints)
        {
            // We can't modify EdgeGeometry.Waypoints as the caller owns that, so ApproximateComparer.Round on lookup.
            return waypoints.Select(w => this.VisGraph.FindVertex(ApproximateComparer.Round(w)));
        }

        private FreePoint FindOrCreateFreePoint(Point location) {
            FreePoint freePoint;
            if (!this.freePointMap.TryGetValue(location, out freePoint)) {
                freePoint = new FreePoint(TransUtil, location);
                this.freePointMap[location] = freePoint;
            } else {
                freePoint.GetVertex(TransUtil, location);
            }
            freePointsInGraph.Insert(freePoint);
            freePointLocationsUsedByRouteEdges.Insert(location);
            return freePoint;
        }

        // This is private because it depends on LimitRectangle
        private FreePoint AddFreePointToGraph(Point location) {
            // This is a FreePoint, either a Waypoint or a Port not in an Obstacle.Ports list.
            // We can't modify the Port.Location as the caller owns that, so ApproximateComparer.Round it
            // at the point at which we acquire it.
            location = ApproximateComparer.Round(location);

            // If the point already exists before FreePoint creation, there's nothing to do.
            var vertex = VisGraph.FindVertex(location);
            var freePoint = this.FindOrCreateFreePoint(location);
            if (vertex != null) {
                return freePoint;
            }

            if (!graphGenerator.IsInBounds(location)) {
                CreateOutOfBoundsFreePoint(freePoint);
                return freePoint;
            }

            // Vertex is inbounds and does not yet exist.  Possibilities are:
            //  - point is on one ScanSegment (perhaps a dead-end)
            //  - point is not on any edge (it's in free space so it's in the middle of some rectangle
            //    (possibly not closed) formed by ScanSegment intersections)
            VisibilityEdge edge = null;
            freePoint.IsOverlapped = this.ObstacleTree.PointIsInsideAnObstacle(freePoint.Point, HScanSegments.ScanDirection);
            var scanSegment = HScanSegments.FindSegmentContainingPoint(location, true /*allowUnfound*/) ??
                          VScanSegments.FindSegmentContainingPoint(location, true /*allowUnfound*/);
            if (scanSegment!=null) {
                // The location is on one ScanSegment.  Find the intersector and split an edge along the segment
                // (or extend the VisibilityEdges of the segment in the desired direction).
                VisibilityVertex targetVertex;
                edge = FindOrCreateNearestPerpEdgeFromNearestPerpSegment(location, scanSegment, location, freePoint.InitialWeight, out targetVertex);
            }

            Direction edgeDir = Direction.South;
            if (edge != null) {
                // The freePoint is on one (but not two) segments, and has already been spliced into 
                // that segment's edge chain.  Add edges laterally to the parallel edges.
                edgeDir = StaticGraphUtility.EdgeDirection(edge);
                ConnectFreePointToLateralEdge(freePoint, CompassVector.RotateLeft(edgeDir));
                ConnectFreePointToLateralEdge(freePoint, CompassVector.RotateRight(edgeDir));
            }
            else {
                // The freePoint is not on ScanSegment so we must splice to 4 surrounding edges (or it may be on a
                // TransientVE). Look in each of the 4 directions, trying first to avoid crossing any obstacle
                // boundaries.  However if we cannot find an edge that does not cross an obstacle boundary, the 
                // freepoint is inside a non-overlapped obstacle, so take a second pass to connect to the nearest
                // edge regardless of obstacle boundaries.
                for (int ii = 0; ii < 4; ++ii) {
                    ConnectFreePointToLateralEdge(freePoint, edgeDir);
                    edgeDir = CompassVector.RotateLeft(edgeDir);
                }
            }
            return freePoint;
        }

        private void CreateOutOfBoundsFreePoint(FreePoint freePoint) {
            // For an out of bounds (OOB) point, we'll link one edge from it to the inbounds edge if it's
            // out of bounds in only one direction; if in two, we'll add a bend. Currently we don't need
            // to do any more because multiple waypoints are processed as multiple subpaths.
            var oobLocation = freePoint.Point;
            Point inboundsLocation = graphGenerator.MakeInBoundsLocation(oobLocation);
            Direction dirFromGraph = PointComparer.GetDirections(inboundsLocation, oobLocation);
            freePoint.OutOfBoundsDirectionFromGraph = dirFromGraph;
            if (!PointComparer.IsPureDirection(dirFromGraph)) {
                // It's OOB in two directions so will need a bend, but we know inboundsLocation
                // is a graph corner so it has a vertex already and we don't need to look up sides.
                StaticGraphUtility.Assert(VisGraph.FindVertex(inboundsLocation) != null, "graph corner vertex not found", ObstacleTree, VisGraph);
                freePoint.AddOobEdgesFromGraphCorner(TransUtil, inboundsLocation);
                return;
            }

            // We know inboundsLocation is on the nearest graphBox border ScanSegment, so this won't return a
            // null edge, and we'll just do normal join-to-one-edge handling, extending in the direction to the graph.
            var inboundsVertex = this.VisGraph.FindVertex(inboundsLocation);
            var dirToGraph = CompassVector.OppositeDir(dirFromGraph);
            if (inboundsVertex != null) {
                freePoint.AddToAdjacentVertex(this.TransUtil, inboundsVertex, dirToGraph, this.portSpliceLimitRectangle);
            }
            else {
                var edge = this.FindorCreateNearestPerpEdge(oobLocation, inboundsLocation, dirFromGraph, ScanSegment.NormalWeight);
                if (edge != null) {
                    inboundsVertex = freePoint.AddEdgeToAdjacentEdge(this.TransUtil, edge, dirToGraph, this.portSpliceLimitRectangle);
                }
            }

            // This may be an oob waypoint, in which case we want to add additional edges so we can
            // go outside graph, cross the waypoint, and come back in.  Shortest-paths will do the
            // work of determining the optimal path, to avoid backtracking.
            var inboundsLeftVertex = StaticGraphUtility.FindAdjacentVertex(inboundsVertex, CompassVector.RotateLeft(dirToGraph));
            if (inboundsLeftVertex != null) {
                this.TransUtil.ConnectVertexToTargetVertex(freePoint.Vertex, inboundsLeftVertex, dirToGraph, ScanSegment.NormalWeight);
            }
            var inboundsRightVertex = StaticGraphUtility.FindAdjacentVertex(inboundsVertex, CompassVector.RotateRight(dirToGraph));
            if (inboundsRightVertex != null) {
                this.TransUtil.ConnectVertexToTargetVertex(freePoint.Vertex, inboundsRightVertex, dirToGraph, ScanSegment.NormalWeight);
            }
        }

        private void ConnectFreePointToLateralEdge(FreePoint freePoint, Direction lateralDir) {
            // Turn on pivot vertex to either side to find the next edge to connect to.  If the freepoint is
            // overlapped (inside an obstacle), just find the closest ScanSegment outside the obstacle and 
            // start extending from there; otherwise, we can have the FreePoint calculate its max visibility.
            var end = freePoint.IsOverlapped ? this.InBoundsGraphBoxIntersect(freePoint.Point, lateralDir)
                                             : freePoint.MaxVisibilityInDirectionForNonOverlappedFreePoint(lateralDir, this.TransUtil);
            var lateralEdge = this.FindorCreateNearestPerpEdge(end, freePoint.Point, lateralDir, freePoint.InitialWeight);

            // There may be no VisibilityEdge between the current point and any adjoining obstacle in that direction.
            if (lateralEdge != null) {
                freePoint.AddEdgeToAdjacentEdge(TransUtil, lateralEdge, lateralDir, this.portSpliceLimitRectangle);
            }
        }
    }
}
