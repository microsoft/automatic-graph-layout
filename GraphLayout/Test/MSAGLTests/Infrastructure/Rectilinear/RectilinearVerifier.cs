// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RectilinearVerifier.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests.Rectilinear
{
    using System.Diagnostics.CodeAnalysis;

    using Core.DataStructures;

    /// <summary>
    /// Contains test-setup utilities and calls to routing and verification functions for Rectilinear edge routing,
    /// and provides the single-inheritance-only propagation of MsaglTestBase.
    /// </summary>
    [TestClass]
    public class RectilinearVerifier : MsaglTestBase
    {
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        protected List<int> SourceOrdinals { get; private set; }
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        protected List<int> TargetOrdinals { get; private set; }

        // If one or both obstacle indexes are >= 0, they identify the source and target objects for routing
        // (otherwise, we just create the visibility graph).
        protected internal int DefaultSourceOrdinal { get; set; }
        protected internal int DefaultTargetOrdinal { get; set; }

        protected internal bool DefaultWantPorts { get; set; }
        protected internal bool RouteFromSourceToAllFreePorts { get; set; }

        // For useFreePortsForObstaclePorts; we don't keep the port in the Shape.Ports collection so need
        // to use this to track the shape.
        protected Dictionary<Port, Shape> FreeRelativePortToShapeMap { get; private set; }

        ////
        // RectFile header members
        ////
        internal double RouterPadding { get; set; }
        internal double RouterEdgeSeparation { get; set; }
        internal bool RouteToCenterOfObstacles { get; set; }
        internal double RouterArrowheadLength { get; set; }

        // Use FreePorts instead of ObstaclePorts
        internal bool UseFreePortsForObstaclePorts { get; set; }
        internal bool UseSparseVisibilityGraph { get; set; }
        internal bool UseObstacleRectangles { get; set; }
        internal bool LimitPortVisibilitySpliceToEndpointBoundingBox { get; set; }

        internal bool WantPaths { get; set; }
        internal bool WantNudger { get; set; }
        internal bool WantVerify { get; set; }

        internal double StraightTolerance { get; set; }
        internal double CornerTolerance { get; set; }
        internal double BendPenalty { get; set; }

        ////
        // Allow global overrides in TestRectilinear and per-file overrides in RectilinearFileTests.
        ////
        protected double? OverrideRouterPadding { get; set; }
        protected double? OverrideRouterEdgeSeparation { get; set; }
        protected bool? OverrideRouteToCenterOfObstacles { get; set; }
        protected double? OverrideRouterArrowheadLength { get; set; }

        protected bool? OverrideUseFreePortsForObstaclePorts { get; set; }
        protected bool? OverrideUseSparseVisibilityGraph { get; set; }
        protected bool? OverrideUseObstacleRectangles { get; set; }
        protected bool? OverrideLimitPortVisibilitySpliceToEndpointBoundingBox { get; set; }

        protected bool? OverrideWantPaths { get; set; }
        protected bool? OverrideWantNudger { get; set; }
        protected bool? OverrideWantVerify { get; set; }

        protected double? OverrideStraightTolerance { get; set; }
        protected double? OverrideCornerTolerance { get; set; }
        protected double? OverrideBendPenalty { get; set; }

        /// <summary>
        /// Do not create ports for loaded files - tests timing of VG-generation only.
        /// </summary>
        internal bool NoPorts { get; set; }

        internal delegate void WriteRouterResultFileFunc(RectilinearEdgeRouter router);
        internal WriteRouterResultFileFunc WriteRouterResultFile { get; set; }

        public RectilinearVerifier()
        {
            SourceOrdinals = new List<int>();
            TargetOrdinals = new List<int>();
            DefaultSourceOrdinal = -1;
            DefaultTargetOrdinal = -1;
            
            FreeRelativePortToShapeMap = new Dictionary<Port, Shape>();
            
            InitializeMembers();
        }

        public override void Initialize()
        {
            base.Initialize();
            this.InitializeMembers();
        }

        // Default initializer
        private void InitializeMembers()
        {
            this.RouterPadding = 1.0;
            this.RouterEdgeSeparation = 1.0;
            this.RouteToCenterOfObstacles = false;
            this.RouterArrowheadLength = 7.0;

            this.UseFreePortsForObstaclePorts = false;
            this.UseSparseVisibilityGraph = false;
            this.UseObstacleRectangles = false;
            this.LimitPortVisibilitySpliceToEndpointBoundingBox = false;

            this.WantPaths = true;
            this.WantNudger = true;
            this.WantVerify = true;

            this.StraightTolerance = 0.001;
            this.CornerTolerance = 0.1;
            this.BendPenalty = SsstRectilinearPath.DefaultBendPenaltyAsAPercentageOfDistance;

            this.FreeRelativePortToShapeMap.Clear();
            this.OverrideMembers();
        }

        // Initializer from file
        
        // After initializing from file, some members may need to be further overridden,
        // either in a particular unit test or by a TestRectilinear commandline argument.
        protected void OverrideMembers()
        {
            this.RouterPadding = this.OverrideRouterPadding ?? this.RouterPadding;
            this.RouterEdgeSeparation = this.OverrideRouterEdgeSeparation ?? this.RouterEdgeSeparation;
            this.RouteToCenterOfObstacles = this.OverrideRouteToCenterOfObstacles ?? this.RouteToCenterOfObstacles;
            this.RouterArrowheadLength = this.OverrideRouterArrowheadLength ?? this.RouterArrowheadLength;
            
            this.UseFreePortsForObstaclePorts = this.OverrideUseFreePortsForObstaclePorts ?? this.UseFreePortsForObstaclePorts;
            this.UseSparseVisibilityGraph = this.OverrideUseSparseVisibilityGraph ?? this.UseSparseVisibilityGraph;
            this.UseObstacleRectangles = this.OverrideUseObstacleRectangles ?? this.UseObstacleRectangles;
            this.LimitPortVisibilitySpliceToEndpointBoundingBox = this.OverrideLimitPortVisibilitySpliceToEndpointBoundingBox ?? this.LimitPortVisibilitySpliceToEndpointBoundingBox;

            this.WantPaths = this.OverrideWantPaths ?? this.WantPaths;
            this.WantNudger = this.OverrideWantNudger ?? this.WantNudger;
            this.WantVerify = this.OverrideWantVerify ?? this.WantVerify;

            this.StraightTolerance = this.OverrideStraightTolerance ?? this.StraightTolerance;
            this.CornerTolerance = this.OverrideCornerTolerance ?? this.CornerTolerance;
            this.BendPenalty = this.OverrideBendPenalty ?? this.BendPenalty;
        }

        protected void ClearOverrideMembers()
        {
            this.OverrideRouterPadding = null;
            this.OverrideRouterEdgeSeparation = null;
            this.OverrideRouteToCenterOfObstacles = null;
            this.OverrideRouterArrowheadLength = null;

            this.OverrideUseFreePortsForObstaclePorts = null;
            this.OverrideUseSparseVisibilityGraph = null;
            this.OverrideUseObstacleRectangles = null;
            this.OverrideLimitPortVisibilitySpliceToEndpointBoundingBox = null;

            this.OverrideWantPaths = null;
            this.OverrideWantNudger = null;
            this.OverrideWantVerify = null;

            this.OverrideStraightTolerance = null;
            this.OverrideCornerTolerance = null;
        }

        ////
        //// Utilities
        ////

        /// <summary>
        /// This is the function that instantiates the router wrapper, overridden by TestRectilinear if not
        /// called from MSTest.
        /// </summary>
        /// <param name="obstacles">List of obstacles</param>
        /// <returns>The instantiated router</returns>
        internal virtual RectilinearEdgeRouterWrapper CreateRouter(IEnumerable<Shape> obstacles) {
            return new RectilinearEdgeRouterWrapper(obstacles, this.RouterPadding, this.RouterEdgeSeparation,
                                                    this.RouteToCenterOfObstacles, this.UseSparseVisibilityGraph, this.UseObstacleRectangles) {
                    WantNudger = this.WantNudger,
                    WantPaths = this.WantPaths,
                    WantVerify = this.WantVerify,
                    StraightTolerance = this.StraightTolerance,
                    CornerTolerance = this.CornerTolerance,
                    BendPenaltyAsAPercentageOfDistance = this.BendPenalty,
                    LimitPortVisibilitySpliceToEndpointBoundingBox = this.LimitPortVisibilitySpliceToEndpointBoundingBox,

                    TestContext = this.TestContext
                };
        }

        internal static List<Point> OffsetsFromRect(Rectangle rect)
        {
            var offsets = new List<Point>
                {
                    new Point(rect.Left - rect.Center.X, 0),     // middle of left side
                    new Point(0, rect.Top - rect.Center.Y),      // middle of top side
                    new Point(rect.Right - rect.Center.X, 0),    // middle of right side
                    new Point(0, rect.Bottom - rect.Center.Y)    // middle of bottom side
                };
            return offsets;
        }

        // Returns the two test squares in return[0] == left, return[1] == right.
        internal List<Shape> CreateTwoTestSquares()
        {
            var curves = new List<Shape>
                {
                    PolylineFromRectanglePoints(new Point(20, 20), new Point(100, 100)),  // left
                    PolylineFromRectanglePoints(new Point(220, 20), new Point(300, 100))  // right
                };
            return curves;
        }

        internal Shape CreateSquare(Point center, double size) 
        {
            size /= 2;
            return PolylineFromRectanglePoints(new Point(center.X - size, center.Y - size),
                                               new Point(center.X + size, center.Y + size));
        }

        // Returns the two test squares in return[0] == left, return[1] == right, and sentinels as noted.
        internal List<Shape> CreateTwoTestSquaresWithSentinels()
        {
            var curves = CreateTwoTestSquares(); // left, right
            curves.Add(PolylineFromRectanglePoints(new Point(0, 20), new Point(5, 100))); // leftSentinel
            curves.Add(PolylineFromRectanglePoints(new Point(315, 20), new Point(320, 100))); // rightSentinel
            curves.Add(PolylineFromRectanglePoints(new Point(0, 115), new Point(320, 120))); // topSentinel
            curves.Add(PolylineFromRectanglePoints(new Point(0, 0), new Point(320, 5))); // bottomSentinel
            return curves;
        }

        internal static Shape PolylineFromPoints(Point[] points)
        {
            if (TriangleOrientation.Clockwise == Point.GetTriangleOrientation(points[0], points[1], points[2]))
            {
                return new Shape(new Polyline(points) { Closed = true });
            }
            return new Shape(new Polyline(points.Reverse()) { Closed = true });
        }

        internal static Shape CurveFromPoints(Point[] points)
        {
            var curve = new Curve();
            for (int ii = 0; ii < points.Length - 1; ++ii)
            {
                curve.AddSegment(new LineSegment(points[ii], points[ii + 1]));
            }
            curve.AddSegment(new LineSegment(points[points.Length - 1], points[0]));
            return new Shape(curve);
        }

        protected Shape PolylineFromRectanglePoints(Point lowerLeft, Point upperRight)
        {
            return PolylineFromPoints(new[]
                        {
                            lowerLeft, new Point(upperRight.X, lowerLeft.Y), upperRight,
                            new Point(lowerLeft.X, upperRight.Y)
                        });
        }

        internal static bool IsFirstPolylineEntirelyWithinSecond(Polyline first, Polyline second, bool touchingOk) 
        {
            foreach (var firstPoint in first.PolylinePoints) 
            {
                if (touchingOk)
                {
                    if (Curve.PointRelativeToCurveLocation(firstPoint.Point, second) == PointLocation.Outside)
                    {
                        return false;
                    }
                    continue;
                }
                if (Curve.PointRelativeToCurveLocation(firstPoint.Point, second) != PointLocation.Inside)
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool IsFirstObstacleEntirelyWithinSecond(Obstacle first, Obstacle second, bool touchingOk) 
        {
            return IsFirstPolylineEntirelyWithinSecond(first.VisibilityPolyline, second.VisibilityPolyline, touchingOk);   
        }

        internal static bool ObstaclesIntersect(Obstacle first, Obstacle second) 
        {
            if (first == second) 
            {
                return false;
            }
            return Curve.CurvesIntersect(first.VisibilityPolyline, second.VisibilityPolyline);    
        }

        internal virtual bool LogError(string message)
        {
            // TestRectilinear overrides this and handles the error so returns true.
            return false;
        }

        private IEnumerable<EdgeGeometry> CreateSourceToFreePortRoutings(RectilinearEdgeRouterWrapper router,
                List<Shape> obstacles, IEnumerable<FloatingPort> freePorts)
        {
            var routings = CreateSourceToFreePortRoutings(obstacles, this.DefaultSourceOrdinal, freePorts);
            this.UpdateObstaclesForSourceOrdinal(obstacles, this.DefaultSourceOrdinal, router);
            return routings;
        }

        private void UpdateObstaclesForSourceOrdinal(List<Shape> obstacles, int sourceOrdinal, RectilinearEdgeRouterWrapper router) {
            for (var idxScan = 0; idxScan < obstacles.Count; ++idxScan)
            {
                if ((idxScan != this.DefaultSourceOrdinal) && (sourceOrdinal >= 0))
                {
                    continue;
                }
                router.UpdateObstacle(obstacles[idxScan]);
                if (idxScan == this.DefaultSourceOrdinal)
                {
                    break;
                }
            }
        }

        internal IEnumerable<EdgeGeometry> CreateSourceToFreePortRoutings(List<Shape> obstacles, int sourceObstacleIndex, IEnumerable<FloatingPort> freePorts)
        {
            if ((!RouteFromSourceToAllFreePorts && !IsDefaultRoutingState) || NoPorts)
            {
                return null;
            }
            var sourceOrdinal = sourceObstacleIndex;
            if (RouteFromSourceToAllFreePorts && (-1 != DefaultSourceOrdinal))
            {
                // Overridden by TestRectilinear
                sourceOrdinal = DefaultSourceOrdinal;
            }

            // Route from one or all obstacles to all freePorts - not a real-world test, probably,
            // but a good stressor.
            var newEdgeGeoms = new List<EdgeGeometry>();
            for (var idxScan = 0; idxScan < obstacles.Count; ++idxScan)
            {
                if ((idxScan != sourceOrdinal) && (sourceOrdinal >= 0))
                {
                    continue;
                }

                var shape = obstacles[idxScan];
                var newPort = this.MakeAbsoluteObstaclePort(shape, shape.BoundingBox.Center);
                newEdgeGeoms.AddRange(freePorts.Select(freePort => CreateRouting(newPort, freePort)));
                if (idxScan == sourceOrdinal)
                {
                    break;
                }
            } // endfor each obstacle
            return newEdgeGeoms;
        }

        private bool IsDefaultRoutingState
        {
            get
            {
                return (-1 == DefaultSourceOrdinal) && (-1 == DefaultTargetOrdinal) && (0 == this.SourceOrdinals.Count) && (0 == this.TargetOrdinals.Count);
            }
        }

        internal IEnumerable<EdgeGeometry> CreateRoutingBetweenFirstTwoObstacles(List<Shape> obstacles)
        {
            if (!IsDefaultRoutingState || NoPorts)
            {
                return null;
            }

            var edgeGeoms = new List<EdgeGeometry>();
            var port0 = this.MakeSingleRelativeObstaclePort(obstacles[0], new Point(0, 0));
            var port1 = this.MakeSingleRelativeObstaclePort(obstacles[1], new Point(0, 0));
            edgeGeoms.Add(this.CreateRouting(port0, port1));
            return edgeGeoms;
        }

        internal IEnumerable<EdgeGeometry> CreateRoutingBetweenObstacles(List<Shape> obstacles, int sourceIndex, int targetIndex)
        {
            if (!IsDefaultRoutingState || NoPorts)
            {
                return null;
            }

            foreach (var shape in obstacles.Where(shape => 0 == shape.Ports.Count))
            {
                shape.Ports.Insert(this.MakeSingleRelativeObstaclePort(shape, new Point(0, 0)));
            }
            var sourceShape = obstacles[sourceIndex];
            return obstacles.Where((t, ii) => (ii != sourceIndex) && ((targetIndex < 0) || (targetIndex == ii)))
                        .Select(targetShape => this.CreateRouting(sourceShape.Ports.First(), targetShape.Ports.First())).ToList();
        }

        internal void DoRouting(IEnumerable<Shape> obstacleEnum)
        {
            // This is just from tests in this file that do VG generation only.
            DoRouting(obstacleEnum, null /*routings*/);
        }

        internal RectilinearEdgeRouterWrapper DoRouting(
            IEnumerable<Shape> obstacleEnum,
            IEnumerable<EdgeGeometry> routingEnum)
        {
            return DoRouting(obstacleEnum, routingEnum, /*freePorts:*/ null);
        }

        internal RectilinearEdgeRouterWrapper DoRouting(
            IEnumerable<Shape> obstacleEnum,
            IEnumerable<EdgeGeometry> routingEnum,
            IEnumerable<FloatingPort> freePorts)
        {
            return DoRouting(obstacleEnum, routingEnum, freePorts, null);
        }

        internal virtual RectilinearEdgeRouterWrapper DoRouting(
            IEnumerable<Shape> obstacleEnum,
            IEnumerable<EdgeGeometry> routingEnum,
            IEnumerable<FloatingPort> freePorts,
            IEnumerable<Point> waypoints)
        {
            // C# doesn't support bypassing override aka VB's MyClass or C++ MyClass::
            // and there is at least one place that needs to do this.
            return BasicDoRouting(obstacleEnum, routingEnum, freePorts, waypoints);
        }

        internal RectilinearEdgeRouterWrapper BasicDoRouting(
            IEnumerable<Shape> obstacleEnum,
            IEnumerable<EdgeGeometry> routingEnum,
            IEnumerable<FloatingPort> freePorts,
            IEnumerable<Point> waypoints)
        {
            if ((null != freePorts) && (null != waypoints)) 
            {
                throw new ApplicationException("Can't specify default creation of both freePorts and waypoints");
            }

            var obstacles = new List<Shape>(obstacleEnum);
            ShowShapes(obstacles);
            var router = GetRouter(obstacles);

            // Add routing specifications.
            bool wantRouting = this.DefaultWantPorts && !NoPorts;
            if ((null == routingEnum) && (null != freePorts) && !NoPorts)
            {
                routingEnum = CreateSourceToFreePortRoutings(router, obstacles, freePorts);
            }
            if ((null != routingEnum) && !NoPorts)
            {
                // Specifically set by test.
                wantRouting = true;
                foreach (var edgeGeom in routingEnum)
                {
                    router.AddEdgeGeometryToRoute(edgeGeom);
                }
            }
            else if (wantRouting)
            {
                // Route between all sources and all targets.  If they specified both
                // -sources and -ports -1, -sources wins.
                // Because we may have a random density, and filling for -1 is local to this block and 
                // the loaded values are not used elsewhere, don't load them - otherwise we could either
                // have out of range values on the next test rep, or not fill all values in for it.
                if (this.DefaultSourceOrdinal != -1)
                {
                    FinalizeObstacleOrdinals(this.SourceOrdinals, this.DefaultSourceOrdinal, obstacles.Count);
                }
                var sourceEnum = (this.SourceOrdinals.Count > 0) ? this.SourceOrdinals.Select(ord => obstacles[ord]) : obstacles;
                if (this.DefaultTargetOrdinal != -1)
                {
                    FinalizeObstacleOrdinals(this.TargetOrdinals, this.DefaultTargetOrdinal, obstacles.Count);
                }
                var targetEnum = (this.TargetOrdinals.Count > 0) ? this.TargetOrdinals.Select(ord => obstacles[ord]) : obstacles;

                // Because of freeOports, we can't just use the shape.Ports collection.
                var ports = new Dictionary<Shape, Port>();
                foreach (var source in sourceEnum)
                {
                    var sourceLocal = source;
                    foreach (var target in targetEnum.Where(target => sourceLocal != target))
                    {
                        this.AddRoutingPorts(router, this.GetPort(router, ports, source), this.GetPort(router, ports, target));
                    }
                }
            } // endifelse all the create-port combinations.

            if (wantRouting)
            {
                // This calculates the VG, then for each EdgeGeometry it adds the ControlPoints
                // to the VG, routes the path, and removes the ControlPoints.
                router.Run();
            }
            else
            {
                // Just generate the graph.
                router.GenerateVisibilityGraph();
            }
            if (this.WriteRouterResultFile != null) {
                this.WriteRouterResultFile(router);
            }
            return router;
        }

        internal virtual RectilinearEdgeRouterWrapper GetRouter(List<Shape> obstacles)
        {
            return CreateRouter(obstacles);
        }

        private static void AddObstacleOrdinal(List<int> ordinals, int ordinal)
        {
            if (!ordinals.Contains(ordinal))
            {
                ordinals.Add(ordinal);
            }
        }

        private static void FinalizeObstacleOrdinals(List<int> ordinals, int idxObstacle, int obstacleCount)
        {
            if (idxObstacle >= 0)
            {
                AddObstacleOrdinal(ordinals, idxObstacle);
                return;
            }

            // idxObstacle is -1: if we don't have a more limited specification, include them all.
            if (0 == ordinals.Count)
            {
                for (int ii = 0; ii < obstacleCount; ++ii)
                {
                    ordinals.Add(ii);
                }
            }
        }

        internal Port GetPort(RectilinearEdgeRouter router, IDictionary<Shape, Port> ports, Shape shape)
        {
            // Because of freeOports, we can't just use the shape.Ports collection.
            Port port;
            if (!ports.TryGetValue(shape, out port))
            {
                port = MakeAbsoluteObstaclePort(router, shape, shape.BoundingBox.Center);
                ports[shape] = port;
            }
            return port;
        }

        internal Port GetRelativePort(Shape shape)
        {
            if (0 == shape.Ports.Count)
            {
                shape.Ports.Insert(this.MakeSingleRelativeObstaclePort(shape, new Point(0, 0)));
            }
            return shape.Ports.First();
        }

        #region Overridden by TestRectilinear.
        internal virtual void ShowGraph(RectilinearEdgeRouterWrapper router) { }
        internal virtual void ShowIncrementalGraph(RectilinearEdgeRouterWrapper router) { }
        internal virtual void ShowShapes(IEnumerable<Shape> obstacles) { }
        #endregion // Overridden by TestRectilinear.

        #region Port_Creation

        protected EdgeGeometry AddRoutingPorts(RectilinearEdgeRouter router, Port sourcePort, Port targetPort
                                )
        {
            Validate.IsNotNull(router, "Router should not be null");
            var eg = CreateRouting(sourcePort, targetPort);
            router.AddEdgeGeometryToRoute(eg);
            return eg;
        }

        protected EdgeGeometry AddRoutingPorts(RectilinearEdgeRouter router, IList<Shape> obstacles, int source, int target
                                )
        {
            Validate.IsNotNull(router, "Router should not be null");
            Validate.IsNotNull(obstacles, "Obstacles should not be null");
            var eg = CreateRouting(obstacles[source].Ports.First(), obstacles[target].Ports.First());
            router.AddEdgeGeometryToRoute(eg);
            return eg;
        }

        protected EdgeGeometry CreateRouting(Port sourcePort, Port targetPort)
        {
            // We currently don't draw arrowheads.
          return new EdgeGeometry(sourcePort, targetPort) { LineWidth = 1 };
        }

        protected FloatingPort MakeAbsoluteObstaclePort(Shape obstacle, Point location)
        {
            Validate.IsNotNull(obstacle, "Obstacle should not be null");

            // For absolute obstacle ports, we don't associate a shape if we are using freeports.
            // This gives test coverage of the case of a freeport/waypoint covered by an unrelated obstacle.
            if (this.UseFreePortsForObstaclePorts)
            {
                return new FloatingPort(null, location);
            }
            var port = new FloatingPort(obstacle.BoundaryCurve, location);
            obstacle.Ports.Insert(port);
            return port;
        }

        protected FloatingPort MakeAbsoluteObstaclePort(RectilinearEdgeRouter router, Shape obstacle, Point location)
        {
            Validate.IsNotNull(router, "Router should not be null");
            var port = MakeAbsoluteObstaclePort(obstacle, location);
            router.UpdateObstacle(obstacle); // Port changes are now auto-detected
            return port;
        }

        protected FloatingPort MakeSingleRelativeObstaclePort(Shape obstacle, Point offset)
        {
            var port = new RelativeFloatingPort(() => obstacle.BoundaryCurve, () => obstacle.BoundingBox.Center, offset);
            RecordRelativePortAndObstacle(obstacle, port);
            return port;
        }

        private void RecordRelativePortAndObstacle(Shape obstacle, RelativeFloatingPort port)
        {
            // For relative obstacle ports, associate the shape and pass this association through
            // the file writer/reader.
            if (!this.UseFreePortsForObstaclePorts)
            {
                obstacle.Ports.Insert(port);
            }
            else
            {
                this.FreeRelativePortToShapeMap[port] = obstacle;
            }
        }

#if UNUSED
        protected FloatingPort MakeSingleRelativeObstaclePort(RectilinearEdgeRouter router, Shape obstacle, Point offset)
        {
            var port = MakeSingleRelativeObstaclePort(obstacle, offset);
            router.UpdateObstacle(obstacle); // Port changes are now auto-detected
            return port;
        }
#endif // UNUSED

        protected static FloatingPort MakeAbsoluteFreePort(Point location)
        {
            return new FloatingPort(null /*curve*/, location);
        }

        #endregion // Port_Creation

        #region Common_Test_Funcs

        // Functions that populate and verify a router, shared between File and non-File tests.

        // ReSharper disable InconsistentNaming

        internal RectilinearEdgeRouterWrapper GroupTest_Simple_Worker(bool wantGroup)
        {
            var obstacles = new List<Shape>();

            // Add initial singles first, slightly off-center to see separate port visibility.
            Shape s1, s2 = null, s3;
            obstacles.Add(s1 = PolylineFromRectanglePoints(new Point(10, 12), new Point(20, 22)));
            s1.UserData = "s1";
            if (wantGroup)
            {
                obstacles.Add(s2 = PolylineFromRectanglePoints(new Point(40, 5), new Point(50, 25)));
                s2.UserData = "s2";
            }
            obstacles.Add(s3 = PolylineFromRectanglePoints(new Point(70, 8), new Point(80, 18)));
            s3.UserData = "s3";

            var ps1 = MakeSingleRelativeObstaclePort(s1, new Point());
            Port ps2 = null;
            if (wantGroup)
            {
                ps2 = MakeSingleRelativeObstaclePort(s2, new Point());
            }
            var ps3 = MakeSingleRelativeObstaclePort(s3, new Point());

            var routings = new List<EdgeGeometry> { CreateRouting(ps1, ps3) };
            if (wantGroup)
            {
                routings.Add(CreateRouting(ps1, ps2));
                routings.Add(CreateRouting(ps2, ps3));
            }

            Shape g1;
            obstacles.Add(g1 = PolylineFromRectanglePoints(new Point(30, -15), new Point(60, 45)));
            g1.UserData = "g1";
            if (wantGroup)
            {
                g1.AddChild(s2);
            }

            return DoRouting(obstacles, routings, null /*freePorts*/);
        }

        
        
        internal RectilinearEdgeRouterWrapper RunSimpleWaypoints(int numPoints, bool multiplePaths, bool wantTopRect)
        {
            var obstacles = CreateTwoTestSquares();

            Validate.IsTrue(wantTopRect || !multiplePaths, "Must have topRect for multiplePaths");
            if (wantTopRect)
            {
                // Add a rectangle on top, to give us some space inbounds to work with
                obstacles.Add(PolylineFromRectanglePoints(new Point(0, 150), new Point(320, 155)));
            }

            var a = obstacles[0]; // left square
            var b = obstacles[1]; // right square
            Shape c = (obstacles.Count > 2) ? obstacles[2] : null; // top rectangle
            var abox = a.BoundingBox;
            var bbox = b.BoundingBox;
            var cbox = (c != null) ? c.BoundingBox : Rectangle.CreateAnEmptyBox();

            var portA = MakeAbsoluteObstaclePort(a, abox.Center);
            var portB = MakeAbsoluteObstaclePort(b, bbox.Center);
            var portC = multiplePaths ? MakeAbsoluteObstaclePort(c, cbox.Center) : null;

            var router = CreateRouter(obstacles);
            var connectAtoB = AddRoutingPorts(router, portA, portB);

            router.Run();
            ShowGraph(router);
            if (this.WriteRouterResultFile != null)
            {
                this.WriteRouterResultFile(router);
            }
            return router;
        }

        // ReSharper restore InconsistentNaming

        #endregion // Common_Test_Funcs
    }
}
