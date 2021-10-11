//
// RectilinearEdgeRouter.cs
// MSAGL main class for Rectilinear Edge Routing.Routing.
//
// Copyright Microsoft Corporation.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing.Rectilinear.Nudging;
using Microsoft.Msagl.Routing.Visibility;
using Microsoft.Msagl.Core;

namespace Microsoft.Msagl.Routing.Rectilinear {

    /// <summary>
    /// Provides rectilinear edge routing functionality 
    /// </summary>
    public class RectilinearEdgeRouter : AlgorithmBase {
        /// <summary>
        /// If an edge does not connect to an obstacle it should stay away from it at least at the padding distance
        /// </summary>
        public double Padding { get; set; }

        /// <summary>
        /// The radius of the arc inscribed into the path corners.
        /// </summary>
        public double CornerFitRadius { get; set; }

        /// <summary>
        /// The relative penalty of a bend, representated as a percentage of the Manhattan distance between
        /// two ports being connected.
        /// </summary>
        public double BendPenaltyAsAPercentageOfDistance {
            get => bendPenaltyAsAPercentageOfDistance; set {
                bendPenaltyAsAPercentageOfDistance = value;
            }
        }

        /// <summary>
        /// If true, route to obstacle centers.  Initially false for greater accuracy with the current 
        /// MultiSourceMultiTarget approach.
        /// </summary>
        public bool RouteToCenterOfObstacles {
            get { return PortManager.RouteToCenterOfObstacles; }
            set { PortManager.RouteToCenterOfObstacles = value; }
        }

        /// <summary>
        /// If true, limits the extension of port visibility splices into the visibility graph to the rectangle defined by
        /// the path endpoints.
        /// </summary>
        public bool LimitPortVisibilitySpliceToEndpointBoundingBox {
            get { return this.PortManager.LimitPortVisibilitySpliceToEndpointBoundingBox; }
            set { this.PortManager.LimitPortVisibilitySpliceToEndpointBoundingBox = value; }
        }

        /// <summary>
        /// Add an EdgeGeometry to route
        /// </summary>
        /// <param name="edgeGeometry"></param>
        public void AddEdgeGeometryToRoute(EdgeGeometry edgeGeometry) {
            ValidateArg.IsNotNull(edgeGeometry, "edgeGeometry");
            // The Port.Location values are not necessarily rounded by the caller.  The values
            // will be rounded upon acquisition in PortManager.cs.  PointComparer.Equal expects
            // all values to be rounded.
            if (!PointComparer.Equal(ApproximateComparer.Round(edgeGeometry.SourcePort.Location)
                    , ApproximateComparer.Round(edgeGeometry.TargetPort.Location))) {
                EdgeGeometries.Add(edgeGeometry);
            }
            else {
                selfEdges.Add(edgeGeometry);
            }
        }

        /// <summary>
        /// Remove a routing specification for an EdgeGeometry.
        /// </summary>
        /// <param name="edgeGeometry"></param>
        public void RemoveEdgeGeometryToRoute(EdgeGeometry edgeGeometry) {
            EdgeGeometries.Remove(edgeGeometry);
        }


        /// <summary>
        /// List all edge routing specifications that are currently active.  We want to hide access to the
        /// List itself so people don't add or remove items directly.
        /// </summary>
        public IEnumerable<EdgeGeometry> EdgeGeometriesToRoute {
            get { return EdgeGeometries; }
        }

        /// <summary>
        /// Remove all EdgeGeometries to route
        /// </summary>
        public void RemoveAllEdgeGeometriesToRoute() {
            // Don't call RemoveEdgeGeometryToRoute as it will interrupt the EdgeGeometries enumerator.
            EdgeGeometries.Clear();
        }

        /// <summary>
        /// If true, this router uses a sparse visibility graph, which saves memory for large graphs but
        /// may choose suboptimal paths.  Set on constructor.
        /// </summary>
        public bool UseSparseVisibilityGraph {
            get { return GraphGenerator is SparseVisibilityGraphGenerator; }
        }

        /// <summary>
        /// If true, this router uses obstacle bounding box rectangles in the visibility graph.
        /// Set on constructor.
        /// </summary>
        public bool UseObstacleRectangles { get; private set; }

        #region Obstacle API

        /// <summary>
        /// The collection of input shapes to route around. Contains all source and target shapes.
        /// as well as any intervening obstacles.
        /// </summary>
        public IEnumerable<Shape> Obstacles {
            get { return ShapeToObstacleMap.Values.Select(obs => obs.InputShape); }
        }

        /// <summary>
        /// The collection of padded obstacle boundary polylines around the input shapes to route around.
        /// </summary>
        internal IEnumerable<Polyline> PaddedObstacles {
            get { return ShapeToObstacleMap.Values.Select(obs => obs.PaddedPolyline); }
        }

        /// <summary>
        /// Add obstacles to the router.
        /// </summary>
        /// <param name="obstacles"></param>
        public void AddObstacles(IEnumerable<Shape> obstacles) {
            ValidateArg.IsNotNull(obstacles, "obstacles");
            AddShapes(obstacles);
            RebuildTreeAndGraph();
        }

        private void AddShapes(IEnumerable<Shape> obstacles) {
            foreach (var shape in obstacles) {
                this.AddObstacleWithoutRebuild(shape);
            }
        }

        /// <summary>
        /// Add a single obstacle to the router.
        /// </summary>
        /// <param name="shape"></param>
        public void AddObstacle(Shape shape) {
            AddObstacleWithoutRebuild(shape);
            RebuildTreeAndGraph();
        }

        /// <summary>
        /// For each Shapes, update its position and reroute as necessary.
        /// </summary>
        /// <param name="obstacles"></param>
        public void UpdateObstacles(IEnumerable<Shape> obstacles) {
            ValidateArg.IsNotNull(obstacles, "obstacles");
            foreach (var shape in obstacles) {
                UpdateObstacleWithoutRebuild(shape);
            }
            RebuildTreeAndGraph();
        }

        /// <summary>
        /// For each Shapes, update its position and reroute as necessary.
        /// </summary>
        /// <param name="obstacle"></param>
        public void UpdateObstacle(Shape obstacle) {
            UpdateObstacleWithoutRebuild(obstacle);
            RebuildTreeAndGraph();
        }

        /// <summary>
        /// Remove obstacles from the router.
        /// </summary>
        /// <param name="obstacles"></param>
        public void RemoveObstacles(IEnumerable<Shape> obstacles) {
            ValidateArg.IsNotNull(obstacles, "obstacles");
            foreach (var shape in obstacles) {
                RemoveObstacleWithoutRebuild(shape);
            }
            RebuildTreeAndGraph();
        }

        /// <summary>
        /// Removes an obstacle from the router.
        /// </summary>
        /// <param name="obstacle"></param>
        /// <returns>All EdgeGeometries affected by the re-routing and re-nudging in order to avoid the new obstacle.</returns>
        public void RemoveObstacle(Shape obstacle) {
            RemoveObstacleWithoutRebuild(obstacle);
            RebuildTreeAndGraph();
        }

        // utilities
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BoundaryCurve")]
        void AddObstacleWithoutRebuild(Shape shape) {
            ValidateArg.IsNotNull(shape, "shape");
            if (shape.BoundaryCurve == null) {
                throw new InvalidOperationException(
#if TEST_MSAGL
                    "Shape must have a BoundaryCurve"
#endif // TEST_MSAGL
                    );
            }
            this.CreatePaddedObstacle(shape);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BoundaryCurve")]
        void UpdateObstacleWithoutRebuild(Shape shape) {
            ValidateArg.IsNotNull(shape, "shape");
            if (shape.BoundaryCurve == null) {
                throw new InvalidOperationException(
#if TEST_MSAGL
                    "Shape must have a BoundaryCurve"
#endif // TEST_MSAGL
                    );
            }

            // Always do all of this even if the Shape objects are the same, because the BoundaryCurve probably changed.
            PortManager.RemoveObstaclePorts(ShapeToObstacleMap[shape]);
            CreatePaddedObstacle(shape);
        }

        private void CreatePaddedObstacle(Shape shape) {
            var obstacle = new Obstacle(shape, this.UseObstacleRectangles, this.Padding);
            this.ShapeToObstacleMap[shape] = obstacle;
            this.PortManager.CreateObstaclePorts(obstacle);
        }

        void RemoveObstacleWithoutRebuild(Shape shape) {
            ValidateArg.IsNotNull(shape, "shape");
            Obstacle obstacle = ShapeToObstacleMap[shape];
            ShapeToObstacleMap.Remove(shape);
            PortManager.RemoveObstaclePorts(obstacle);
        }

        /// <summary>
        /// Remove all obstacles from the graph.
        /// </summary>
        public void RemoveAllObstacles() {
            InternalClear(retainObstacles: false);
        }

        #endregion // Obstacle API

        void RebuildTreeAndGraph() {
            bool hadTree = this.ObsTree.Root != null;
            bool hadVg = GraphGenerator.VisibilityGraph != null;
            InternalClear(retainObstacles: true);

            if (hadTree) {
                GenerateObstacleTree();
            }

            if (hadVg) {
                GenerateVisibilityGraph();
            }
        }

        /// <summary>
        /// The visibility graph generated by GenerateVisibilityGraph.
        /// </summary>
        internal VisibilityGraph VisibilityGraph {
            get {
                GenerateVisibilityGraph();
                return GraphGenerator.VisibilityGraph;
            }
        }

        /// <summary>
        /// Clears all data set into the router.
        /// </summary>
        public void Clear() {
            InternalClear(retainObstacles: false);
        }

        #region Private data

        /// <summary>
        /// Generates the visibility graph.
        /// </summary>
        internal readonly VisibilityGraphGenerator GraphGenerator;

        /// <summary>
        /// To support dynamic obstacles, we index obstacles by their Shape, which is
        /// the unpadded inner obstacle boundary and contains a unique ID so we can
        /// handle overlap due to dragging.
        /// </summary>
        internal readonly Dictionary<Shape, Obstacle> ShapeToObstacleMap = new Dictionary<Shape, Obstacle>();

        ///<summary>
        /// The list of EdgeGeometries to route
        ///</summary>
        internal readonly List<EdgeGeometry> EdgeGeometries = new List<EdgeGeometry>();

        ///<summary>
        /// Manages the mapping between App-level Ports, their locations, and their containing EdgeGeometries.
        ///</summary>
        internal readonly PortManager PortManager;

        internal Dictionary<Shape, Set<Shape>> AncestorsSets { get; private set; }

        #endregion // Private data

        /// <summary>
        /// Default constructor.
        /// </summary>
        public RectilinearEdgeRouter()
            : this(null) {
            // pass-through default arguments to parameterized ctor
        }

        /// <summary>
        /// The padding from an obstacle's curve to its enclosing polyline.
        /// </summary>
        public const double DefaultPadding = 1.0;

        /// <summary>
        /// The default radius of the arc inscribed into path corners.
        /// </summary>
        public const double DefaultCornerFitRadius = 3.0;

        /// <summary>
        /// Constructor that takes the obstacles but uses defaults for other arguments.
        /// </summary>
        /// <param name="obstacles">The collection of shapes to route around. Contains all source and target shapes
        /// as well as any intervening obstacles.</param>
        public RectilinearEdgeRouter(IEnumerable<Shape> obstacles)
            : this(obstacles, DefaultPadding, DefaultCornerFitRadius, useSparseVisibilityGraph: false, useObstacleRectangles: false) {
        }

        /// <summary>
        /// Constructor for a router that does not use obstacle rectangles in the visibility graph.
        /// </summary>
        /// <param name="obstacles">The collection of shapes to route around. Contains all source and target shapes
        /// as well as any intervening obstacles.</param>
        /// <param name="padding">The minimum padding from an obstacle's curve to its enclosing polyline.</param>
        /// <param name="cornerFitRadius">The radius of the arc inscribed into path corners</param>
        /// <param name="useSparseVisibilityGraph">If true, use a sparse visibility graph, which saves memory for large graphs
        /// but may select suboptimal paths</param>
        public RectilinearEdgeRouter(IEnumerable<Shape> obstacles, double padding, double cornerFitRadius, bool useSparseVisibilityGraph)
            : this(obstacles, padding, cornerFitRadius, useSparseVisibilityGraph, useObstacleRectangles: false) {
        }

        /// <summary>
        /// Constructor specifying graph and shape information.
        /// </summary>
        /// <param name="obstacles">The collection of shapes to route around. Contains all source and target shapes
        /// as well as any intervening obstacles.</param>
        /// <param name="padding">The minimum padding from an obstacle's curve to its enclosing polyline.</param>
        /// <param name="cornerFitRadius">The radius of the arc inscribed into path corners</param>
        /// <param name="useSparseVisibilityGraph">If true, use a sparse visibility graph, which saves memory for large graphs
        /// but may select suboptimal paths</param>
        /// <param name="useObstacleRectangles">Use obstacle bounding boxes in visibility graph</param>
        public RectilinearEdgeRouter(IEnumerable<Shape> obstacles, double padding, double cornerFitRadius,
                                    bool useSparseVisibilityGraph, bool useObstacleRectangles) {
            Padding = padding;
            CornerFitRadius = cornerFitRadius;
            BendPenaltyAsAPercentageOfDistance = SsstRectilinearPath.DefaultBendPenaltyAsAPercentageOfDistance;
            if (useSparseVisibilityGraph) {
                this.GraphGenerator = new SparseVisibilityGraphGenerator();
            }
            else {
                this.GraphGenerator = new FullVisibilityGraphGenerator();
            }
            this.UseObstacleRectangles = useObstacleRectangles;
            PortManager = new PortManager(GraphGenerator);
            AddShapes(obstacles);
        }

        /// <summary>
        /// Constructor specifying graph information.
        /// </summary>
        /// <param name="graph">The graph whose edges are being routed.</param>
        /// <param name="padding">The minimum padding from an obstacle's curve to its enclosing polyline.</param>
        /// <param name="cornerFitRadius">The radius of the arc inscribed into path corners</param>
        /// <param name="useSparseVisibilityGraph">If true, use a sparse visibility graph, which saves memory for large graphs
        /// but may select suboptimal paths</param>
        public RectilinearEdgeRouter(GeometryGraph graph, double padding, double cornerFitRadius, bool useSparseVisibilityGraph)
            : this(graph, padding, cornerFitRadius, useSparseVisibilityGraph, useObstacleRectangles: false) {
        }

        /// <summary>
        /// Constructor specifying graph information.
        /// </summary>
        /// <param name="graph">The graph whose edges are being routed.</param>
        /// <param name="padding">The minimum padding from an obstacle's curve to its enclosing polyline.</param>
        /// <param name="cornerFitRadius">The radius of the arc inscribed into path corners</param>
        /// <param name="useSparseVisibilityGraph">If true, use a sparse visibility graph, which saves memory for large graphs
        /// but may select suboptimal paths</param>
        /// <param name="useObstacleRectangles">If true, use obstacle bounding boxes in visibility graph</param>
        public RectilinearEdgeRouter(GeometryGraph graph, double padding, double cornerFitRadius,
                                    bool useSparseVisibilityGraph, bool useObstacleRectangles)
            : this(ShapeCreator.GetShapes(graph), padding, cornerFitRadius, useSparseVisibilityGraph, useObstacleRectangles) {
            ValidateArg.IsNotNull(graph, "graph");
            foreach (var edge in graph.Edges) {
                this.AddEdgeGeometryToRoute(edge.EdgeGeometry);
            }
        }

        /// <summary>
        /// Executes the algorithm.
        /// </summary>
        protected override void RunInternal() {
            // Create visibility graph if not already done.
            GenerateVisibilityGraph();
            GeneratePaths();
        }

        internal virtual void GeneratePaths() {
            var edgePaths = this.EdgeGeometries.Select(eg => new Path(eg)).ToList();
            this.FillEdgePathsWithShortestPaths(edgePaths);
            this.NudgePaths(edgePaths);
            this.RouteSelfEdges();
            this.FinaliseEdgeGeometries();
        }

        void RouteSelfEdges() {
            foreach (var edge in selfEdges) {
                edge.Curve = Edge.RouteSelfEdge(edge.SourcePort.Curve, Math.Max(Padding, 2 * edge.GetMaxArrowheadLength()), out SmoothedPolyline sp);
            }
        }



#if TEST_MSAGL
        private IEnumerable<DebugCurve> GetGraphDebugCurves() {
            List<DebugCurve> l =
                VisibilityGraph.Edges.Select(e => new DebugCurve(50, 0.1, "blue", new LineSegment(e.SourcePoint, e.TargetPoint))).ToList();
            l.AddRange(Obstacles.Select(o => new DebugCurve(1, "green", o.BoundaryCurve)));
            return l;
        }
#endif

        private void FillEdgePathsWithShortestPaths(IEnumerable<Path> edgePaths) {
            this.PortManager.BeginRouteEdges();
            var shortestPathRouter = new MsmtRectilinearPath(this.BendPenaltyAsAPercentageOfDistance);
            foreach (Path edgePath in edgePaths) {
                this.ProgressStep();
                AddControlPointsAndGeneratePath(shortestPathRouter, edgePath);
            }
            this.PortManager.EndRouteEdges();
        }

        private void AddControlPointsAndGeneratePath(MsmtRectilinearPath shortestPathRouter, Path edgePath) {
            Point[] intersectPoints = PortManager.GetPortVisibilityIntersection(edgePath.EdgeGeometry);
            if (intersectPoints != null) {
                GeneratePathThroughVisibilityIntersection(edgePath, intersectPoints);
                return;
            }

            this.SpliceVisibilityAndGeneratePath(shortestPathRouter, edgePath);
        }

        internal virtual void GeneratePathThroughVisibilityIntersection(Path edgePath, Point[] intersectPoints) {
            edgePath.PathPoints = intersectPoints;
        }

        internal virtual void SpliceVisibilityAndGeneratePath(MsmtRectilinearPath shortestPathRouter, Path edgePath) {
            this.PortManager.AddControlPointsToGraph(edgePath.EdgeGeometry, this.ShapeToObstacleMap);
            this.PortManager.TransUtil.DevTrace_VerifyAllVertices(this.VisibilityGraph);
            this.PortManager.TransUtil.DevTrace_VerifyAllEdgeIntersections(this.VisibilityGraph);
            if (!this.GeneratePath(shortestPathRouter, edgePath)) {
                this.RetryPathsWithAdditionalGroupsEnabled(shortestPathRouter, edgePath);
            }
            this.PortManager.RemoveControlPointsFromGraph();
        }

#if TEST_MSAGL
        // ReSharper disable UnusedMember.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void ShowEdgePath(Path path) {
            // ReSharper restore UnusedMember.Local
            List<DebugCurve> dd = Nudger.GetObstacleBoundaries(PaddedObstacles, "black");
            dd.AddRange(Nudger.PathDebugCurvesFromPoint(path));
            dd.AddRange(VisibilityGraph.Edges.Select(e => new DebugCurve(0.5, "blue", new LineSegment(e.SourcePoint, e.TargetPoint))));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dd);
        }
#endif

        internal virtual bool GeneratePath(MsmtRectilinearPath shortestPathRouter, Path edgePath, bool lastChance = false) {
            var sourceVertices = PortManager.FindVertices(edgePath.EdgeGeometry.SourcePort);
            var targetVertices = PortManager.FindVertices(edgePath.EdgeGeometry.TargetPort);
            return GetSingleStagePath(edgePath, shortestPathRouter, sourceVertices, targetVertices, lastChance);
        }

        private static bool GetSingleStagePath(Path edgePath, MsmtRectilinearPath shortestPathRouter,
                    List<VisibilityVertex> sourceVertices, List<VisibilityVertex> targetVertices, bool lastChance) {
            edgePath.PathPoints = shortestPathRouter.GetPath(sourceVertices, targetVertices);
            if (lastChance) {
                EnsureNonNullPath(edgePath);
            }
            return (edgePath.PathPoints != null);
        }

        private static void EnsureNonNullPath(Path edgePath) {
            if (edgePath.PathPoints == null) {
                // Probably a fully-landlocked obstacle such as RectilinearTests.Route_Between_Two_Separately_Landlocked_Obstacles
                // or disconnected subcomponents due to excessive overlaps, such as Rectilinear(File)Tests.*Disconnected*.  In this
                // case, just put the single-bend path in there, even though it most likely cuts across unrelated obstacles.
                if (PointComparer.IsPureDirection(edgePath.EdgeGeometry.SourcePort.Location, edgePath.EdgeGeometry.TargetPort.Location)) {
                    edgePath.PathPoints = new[] {
                            edgePath.EdgeGeometry.SourcePort.Location,
                            edgePath.EdgeGeometry.TargetPort.Location
                    };
                    return;
                }
                edgePath.PathPoints = new[] {
                    edgePath.EdgeGeometry.SourcePort.Location,
                    new Point(edgePath.EdgeGeometry.SourcePort.Location.X, edgePath.EdgeGeometry.TargetPort.Location.Y),
                    edgePath.EdgeGeometry.TargetPort.Location
                };
            }
        }

        internal virtual void RetryPathsWithAdditionalGroupsEnabled(MsmtRectilinearPath shortestPathRouter, Path edgePath) {
            // Insert any spatial parent groups that are not in our hierarchical parent tree and retry,
            // if we haven't already done this.
            if (!PortManager.SetAllAncestorsActive(edgePath.EdgeGeometry, ShapeToObstacleMap)
                    || !GeneratePath(shortestPathRouter, edgePath)) {
                // Last chance: enable all groups (if we have any).  Only do this on a per-path basis so a single degenerate
                // path won't make the entire graph look bad.
                PortManager.SetAllGroupsActive();
                GeneratePath(shortestPathRouter, edgePath, lastChance:true);
            }
        }

#if TEST_MSAGL && !SILVERLIGHT
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.WriteLine(System.String)")]
        internal static void ShowPointEnum(IEnumerable<Point> p) {
            // ReSharper disable InconsistentNaming
            const double w0 = 0.1;
            const int w1 = 3;
            Point[] arr = p.ToArray();
            double d = (w1 - w0) / (arr.Length - 1);
            var l = new List<DebugCurve>();
            for (int i = 0; i < arr.Length - 1; i++) {
                l.Add(new DebugCurve(100, w0 + i * d, "blue", new LineSegment(arr[i], arr[i + 1])));
            }


            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
            // ReSharper restore InconsistentNaming
        }
#endif

        internal virtual void NudgePaths(IEnumerable<Path> edgePaths) {

            // If we adjusted for spatial ancestors, this nudging can get very weird, so refetch in that case.
            var ancestorSets = this.ObsTree.SpatialAncestorsAdjusted ? SplineRouter.GetAncestorSetsMap(Obstacles) : this.AncestorsSets;

            // Using VisibilityPolyline retains any reflection/staircases on the convex hull borders; using
            // PaddedPolyline removes them.
            Nudger.NudgePaths(edgePaths, CornerFitRadius, PaddedObstacles, ancestorSets, RemoveStaircases);
            //Nudger.NudgePaths(edgePaths, CornerFitRadius, this.ObstacleTree.GetAllPrimaryObstacles().Select(obs => obs.VisibilityPolyline), ancestorSets, RemoveStaircases);

        }
        private bool removeStaircases = true;
        private double bendPenaltyAsAPercentageOfDistance;
        readonly List<EdgeGeometry> selfEdges = new List<EdgeGeometry>();

        ///<summary>
        ///</summary>
        public bool RemoveStaircases {
            get { return removeStaircases; }
            set { removeStaircases = value; }
        }

        internal virtual void FinaliseEdgeGeometries() {
            foreach (EdgeGeometry edgeGeom in EdgeGeometries.Concat(selfEdges)) {
                if (edgeGeom.Curve == null) {
                    continue;
                }
                var poly = (edgeGeom.Curve as Polyline);
                if (poly != null) {
                    edgeGeom.Curve = FitArcsIntoCorners(CornerFitRadius, poly.ToArray());
                }
                CalculateArrowheads(edgeGeom);
            }
        }

        internal virtual void CreateVisibilityGraph() {
            GraphGenerator.Clear();
            InitObstacleTree();
            GraphGenerator.GenerateVisibilityGraph();
        }

        private static void CalculateArrowheads(EdgeGeometry edgeGeom) {
            Arrowheads.TrimSplineAndCalculateArrowheads(edgeGeom, edgeGeom.SourcePort.Curve, edgeGeom.TargetPort.Curve, edgeGeom.Curve, true);
        }

        #region Private functions

        private ObstacleTree ObsTree {
            get { return this.GraphGenerator.ObsTree; }
        }

        private void GenerateObstacleTree() {
            if ((Obstacles == null) || !Obstacles.Any()) {
                throw new InvalidOperationException(
#if TEST_MSAGL
                    "No obstacles have been added"
#endif // TEST
                    );
            }

            if (this.ObsTree.Root == null) {
                InitObstacleTree();
            }
        }

        internal virtual void InitObstacleTree() {
            AncestorsSets = SplineRouter.GetAncestorSetsMap(Obstacles);
            this.ObsTree.Init(ShapeToObstacleMap.Values, AncestorsSets, ShapeToObstacleMap);
        }

        private void InternalClear(bool retainObstacles) {
            GraphGenerator.Clear();
            ClearShortestPaths();
            if (retainObstacles) {
                // Remove precalculated visibility, since we're likely revising obstacle positions.
                PortManager.ClearVisibility();
            }
            else {
                PortManager.Clear();
                ShapeToObstacleMap.Clear();
                EdgeGeometries.Clear();
            }
        }

        private void ClearShortestPaths() {
            foreach (EdgeGeometry edgeGeom in EdgeGeometries) {
                edgeGeom.Curve = null;
            }
        }


        #endregion Private functions

        /// <summary>
        /// Generates the visibility graph if it hasn't already been done.
        /// </summary>
        internal void GenerateVisibilityGraph() {
            if ((Obstacles == null) || !Obstacles.Any()) {
                throw new InvalidOperationException(
#if TEST_MSAGL
                    "No obstacles have been set"
#endif
                    );
            }

            // Must test GraphGenerator.VisibilityGraph because this.VisibilityGraph calls back to
            // this function to ensure the graph is present.
            if (GraphGenerator.VisibilityGraph == null) {
                CreateVisibilityGraph();
            }
        }

#if TEST_MSAGL
        internal void ShowPathWithTakenEdgesAndGraph(IEnumerable<VisibilityVertex> path, Set<VisibilityEdge> takenEdges) {
            var list = new List<VisibilityVertex>(path);
            var lines = new List<LineSegment>();
            for (int i = 0; i < list.Count - 1; i++)
                lines.Add(new LineSegment(list[i].Point, list[i + 1].Point));

            // ReSharper disable InconsistentNaming
            double w0 = 4;
            const double w1 = 8;
            double delta = (w1 - w0) / (list.Count - 1);

            var dc = new List<DebugCurve>();
            foreach (LineSegment line in lines) {
                dc.Add(new DebugCurve(50, w0, "red", line));
                w0 += delta;
            }
            dc.AddRange(takenEdges.Select(edge => new DebugCurve(50, 2, "black", new LineSegment(edge.SourcePoint, edge.TargetPoint))));
            IEnumerable<DebugCurve> k = GetGraphDebugCurves();
            dc.AddRange(k);
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dc);
            // ReSharper restore InconsistentNaming
        }
#endif

        internal static ICurve FitArcsIntoCorners(double radius, Point[] polyline) {
            IEnumerable<Ellipse> ellipses = GetFittedArcSegs(radius, polyline);
            var curve = new Curve(polyline.Length);
            Ellipse prevEllipse = null;
            foreach (Ellipse ellipse in ellipses) {
                bool ellipseIsAlmostCurve = EllipseIsAlmostLineSegment(ellipse);

                if (prevEllipse != null) {
                    if (ellipseIsAlmostCurve)
                        Curve.ContinueWithLineSegment(curve, CornerPoint(ellipse));
                    else {
                        Curve.ContinueWithLineSegment(curve, ellipse.Start);
                        curve.AddSegment(ellipse);
                    }
                }
                else {
                    if (ellipseIsAlmostCurve)
                        Curve.AddLineSegment(curve, polyline[0], CornerPoint(ellipse));
                    else {
                        Curve.AddLineSegment(curve, polyline[0], ellipse.Start);
                        curve.AddSegment(ellipse);
                    }
                }

                prevEllipse = ellipse;
            }

            if (curve.Segments.Count > 0)
                Curve.ContinueWithLineSegment(curve, polyline[polyline.Length - 1]);
            else
                Curve.AddLineSegment(curve, polyline[0], polyline[polyline.Length - 1]);

            return curve;
        }

        static Point CornerPoint(Ellipse ellipse) {
            return ellipse.Center + ellipse.AxisA + ellipse.AxisB;
        }

        private static bool EllipseIsAlmostLineSegment(Ellipse ellipse) {
            return ellipse.AxisA.LengthSquared < 0.0001 || ellipse.AxisB.LengthSquared < 0.0001;
        }

        private static IEnumerable<Ellipse> GetFittedArcSegs(double radius, Point[] polyline) {
            Point leg = polyline[1] - polyline[0];
            Point dir = leg.Normalize();
            double rad0 = Math.Min(radius, leg.Length / 2);

            for (int i = 1; i < polyline.Length - 1; i++) {
                Ellipse ret = null;
                leg = polyline[i + 1] - polyline[i];
                double legLength = leg.Length;

                if (legLength < ApproximateComparer.IntersectionEpsilon)
                    ret = /*new Ellipse(0, 0, polyline[i]) = */
                        new Ellipse(0, 0, new Point(), new Point(), polyline[i]);

                Point ndir = leg / legLength;
                if (Math.Abs(ndir * dir) > 0.9) //the polyline does't make a 90 degrees turn
                    ret = new Ellipse(0, 0, polyline[i]);

                double nrad0 = Math.Min(radius, leg.Length / 2);
                Point axis0 = -nrad0 * ndir;
                Point axis1 = rad0 * dir;
                yield return ret ?? (new Ellipse(0, Math.PI / 2, axis0, axis1, polyline[i] - axis1 - axis0));
                dir = ndir;
                rad0 = nrad0;
            }
        }
    }
} 
