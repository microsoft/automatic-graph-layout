using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using System.Threading.Tasks;
#if TEST_MSAGL
using Microsoft.Msagl.DebugHelpers;
#endif

namespace Microsoft.Msagl.Layout.Initial {
    /// <summary>
    /// Methods for obtaining an initial layout of a graph by arranging clusters bottom up using various means.
    /// </summary>
    public class InitialLayoutByCluster : AlgorithmBase {
        readonly GeometryGraph graph;

        readonly ICollection<Cluster> clusters;

        readonly Func<Cluster, LayoutAlgorithmSettings> clusterSettings;

        /// <summary>
        /// Recursively lay out the clusters of the given graph using the given settings.
        /// </summary>
        /// <param name="graph">The graph being operated on.</param>
        /// <param name="defaultSettings">Settings to use if none is provided for a particular cluster or its ancestors.</param>
        public InitialLayoutByCluster(GeometryGraph graph, LayoutAlgorithmSettings defaultSettings)
            : this(graph, anyCluster => defaultSettings) { }

        /// <summary>
        /// Recursively lay out the clusters of the given graph using the specified settings for each cluster, or if none is given for a particular
        /// cluster then inherit from the cluster's ancestor - or from the specifed defaultSettings.
        /// </summary>
        /// <param name="graph">The graph being operated on.</param>
        /// <param name="clusterSettings">Settings to use for each cluster and its descendents (if none provided for that descendent.</param>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public InitialLayoutByCluster(GeometryGraph graph, Func<Cluster, LayoutAlgorithmSettings> clusterSettings)
            : this(graph, new[] { graph.RootCluster }, clusterSettings) { }

        /// <summary>
        /// Recursively lay out the given clusters using the specified settings for each cluster, or if none is given for a particular
        /// cluster then inherit from the cluster's ancestor - or from the specifed defaultSettings.
        /// Clusters (other than the root) will be translated (together with their descendants) such that their 
        /// bottom-left point of their new boundaries are the same as the bottom-left of their old boundaries 
        /// (i.e. clusters are laid-out in place).
        /// </summary>
        /// <param name="graph">The graph being operated on.</param>
        /// <param name="clusters">The clusters to layout.</param>
        /// <param name="clusterSettings">Settings to use for each cluster and its descendents (if none provided for that descendent.</param>
        public InitialLayoutByCluster(GeometryGraph graph, IEnumerable<Cluster> clusters,
            Func<Cluster, LayoutAlgorithmSettings> clusterSettings) {
            ValidateArg.IsNotNull(graph, "graph");
            ValidateArg.IsNotNull(clusters, "clusters");
            ValidateArg.IsNotNull(clusterSettings, "clusterSettings");
#if TEST_MSAGL
            graph.SetDebugIds();
#endif

            this.graph = graph;
            this.clusters = clusters.ToList();
            this.clusterSettings = clusterSettings;
        }

        ParallelOptions parallelOptions;

#if SHARPKIT // no multithreading in JS
        bool runInParallel = false;
#else
        bool runInParallel = true;
#endif

        /// <summary>
        /// if set to true than parallel execution will b
        /// </summary>
        public bool RunInParallel {
            get { return runInParallel; }
            set { runInParallel = value; }
        }

        /// <summary>
        /// The actual layout process
        /// </summary>
        protected override void RunInternal() {
            if (runInParallel) {
                parallelOptions = new ParallelOptions();
#if PPC
                if (CancelToken != null)
                    parallelOptions.CancellationToken = CancelToken.CancellationToken;
#endif

            }

            // This call isn't super cheap, so we shouldn't do this too often.

            if (runInParallel && clusters.Count > 1)
                Parallel.ForEach(clusters, parallelOptions, ProcessCluster);
            else
                foreach (Cluster cluster in clusters)
                    ProcessCluster(cluster);

            bool isRootCluster = clusters.Any(c => c == graph.RootCluster);

            if (isRootCluster) {
                // only want to do this when we are working solely with the root cluster.
                // expanding individual clusters will mean that the containment hierarchy is not valid
                // (until it's fixed up by the next incremental layout)
                RouteParentEdges(graph, clusterSettings(graph.RootCluster).EdgeRoutingSettings);
            }

            graph.UpdateBoundingBox();

            if (isRootCluster) {
                Debug.Assert(clusters.Count() == 1,
                    "Layout by cluster with a root cluster should not contain any other cluster.");
                // Zero the graph
                graph.Translate(-graph.BoundingBox.LeftBottom);
                //LayoutAlgorithmSettings.ShowGraph(graph);
            }

            ProgressComplete();

        }

        void ProcessCluster(Cluster cluster) {
            if (cluster.IsCollapsed)
                return;
            Rectangle oldBounds = cluster.BoundingBox;
            cluster.UnsetInitialLayoutStateIncludingAncestors();
            LayoutCluster(cluster);

            if (cluster != graph.RootCluster) {
                Rectangle newBounds = cluster.BoundingBox;
                cluster.DeepTranslation(oldBounds.Center - newBounds.Center, true);
            }
#if TEST_MSAGL
            //  ValidateLayout(cluster);
#endif
        }

        internal static void RouteParentEdges(GeometryGraph graph, EdgeRoutingSettings edgeRoutingSettings) {
            var inParentEdges = new List<Edge>();
            var outParentEdges = new List<Edge>();
            RouteSimplHooksAndFillTheLists(graph.RootCluster, inParentEdges, outParentEdges, edgeRoutingSettings);

            if (inParentEdges.Count > 0 || outParentEdges.Count > 0)
                LabelParentEdgesAndMaybeRerouteThemNicely(graph, inParentEdges, outParentEdges, edgeRoutingSettings);
        }

        static void LabelParentEdgesAndMaybeRerouteThemNicely(GeometryGraph graph, List<Edge> inParentEdges,
            List<Edge> outParentEdges, EdgeRoutingSettings edgeRoutingSettings) {
            if (AllowedToRoute(inParentEdges, outParentEdges, edgeRoutingSettings)) {
                var shapeGroupRouter = new SplineRouter(graph, edgeRoutingSettings.Padding,
                    edgeRoutingSettings.PolylinePadding,
                    edgeRoutingSettings.RoutingToParentConeAngle, inParentEdges, outParentEdges);
                shapeGroupRouter.Run();
            }

            var labeledEdges = outParentEdges.Concat(inParentEdges).Where(e => e.Labels.Any());
            if (labeledEdges.Any()) {
                var elb = new EdgeLabelPlacement(graph.Nodes, labeledEdges);
                //consider adding more nodes here: some sibling clusters maybe
                elb.Run();
            }
        }

        static bool AllowedToRoute(List<Edge> inParentEdges, List<Edge> outParentEdges,
            EdgeRoutingSettings edgeRoutingSettings) {
            return ShapeCreatorForRoutingToParents.NumberOfActiveNodesIsUnderThreshold(inParentEdges, outParentEdges,
                edgeRoutingSettings.
                    SimpleSelfLoopsForParentEdgesThreshold);
        }

        static void RouteSimplHooksAndFillTheLists(Cluster rootCluster, List<Edge> inParentEdges,
            List<Edge> outParentEdges, EdgeRoutingSettings edgeRoutingSettings) {
            var padding = edgeRoutingSettings.Padding + edgeRoutingSettings.PolylinePadding;
            foreach (var cluster in rootCluster.AllClustersWidthFirstExcludingSelfAvoidingChildrenOfCollapsed().Where(c => !c.IsCollapsed)) {
                RouteClusterParentInEdges(inParentEdges, edgeRoutingSettings, cluster, padding);
                RouteClusterParentOutEdges(outParentEdges, edgeRoutingSettings, cluster, padding);
            }
        }

        static void RouteClusterParentOutEdges(List<Edge> outParentEdges, EdgeRoutingSettings edgeRoutingSettings, Cluster cluster, double padding) {
            foreach (var e in cluster.OutEdges.Where(e => IsDescendant(e.Target, cluster))) {
                var ePadding = Math.Max(padding, 1.5 * ArrowlengthAtSource(e));
                var hookPort = e.SourcePort as HookUpAnywhereFromInsidePort;
                if (hookPort == null)
                    e.SourcePort = hookPort = new HookUpAnywhereFromInsidePort(() => cluster.BoundaryCurve);
                hookPort.HookSize = ePadding;

                e.Curve = StraightLineEdges.CreateLoop(e.Target.BoundingBox, cluster.BoundingBox, ePadding, false);
                Arrowheads.TrimSplineAndCalculateArrowheads(e, e.Curve, false,
                    edgeRoutingSettings.KeepOriginalSpline);
                outParentEdges.Add(e);
            }
        }

        static void RouteClusterParentInEdges(List<Edge> inParentEdges, EdgeRoutingSettings edgeRoutingSettings, Cluster cluster,
            double padding) {
            foreach (var e in cluster.InEdges.Where(e => IsDescendant(e.Source, cluster))) {
                double ePadding = Math.Max(padding, 1.5 * ArrowlengthAtTarget(e));
                var hookPort = e.TargetPort as HookUpAnywhereFromInsidePort;
                if (hookPort == null)
                    e.TargetPort = hookPort = new HookUpAnywhereFromInsidePort(() => cluster.BoundaryCurve);
                hookPort.HookSize = ePadding;
                e.Curve = StraightLineEdges.CreateLoop(e.Source.BoundingBox, cluster.BoundingBox, ePadding, false);
                Arrowheads.TrimSplineAndCalculateArrowheads(e, e.Curve, false,
                    edgeRoutingSettings.KeepOriginalSpline);
                inParentEdges.Add(e);
            }
        }

        static double ArrowlengthAtSource(Edge edge) {
            return edge.EdgeGeometry.SourceArrowhead == null ? 0 : edge.EdgeGeometry.SourceArrowhead.Length;
        }

        static double ArrowlengthAtTarget(Edge edge) {
            return edge.EdgeGeometry.TargetArrowhead == null ? 0 : edge.EdgeGeometry.TargetArrowhead.Length;
        }

#if TEST_MSAGL
        //        void CheckEdges() {
        //            foreach (var edge in graph.Edges)
        //                CheckEdge(edge);
        //        }
        //
        //        static void CheckEdge(Edge edge) {
        //            var s = edge.Source;
        //            var t = edge.Target;
        //            var sParents = new Set<Node>(s.ClusterParents);
        //            var tParents = new Set<Node>(t.ClusterParents);
        //            if (sParents == tParents) // the edge is between of the nodes of the same cluster, in a simple case
        //                return;
        //            var cluster = t as Cluster;
        //            if (cluster != null && IsDescendant(s, cluster))
        //                return;
        //            cluster = s as Cluster;
        //            if (cluster != null && IsDescendant(t, cluster))
        //                return;
        //            Debug.Assert(false, "an edge can be flat or connecting with an ancestor");
        //        }

        /// <summary>
        /// Ensures that containment is preserved
        /// </summary>
        /// <param name="cluster">check is applied to specified cluster and below</param>
        static void ValidateLayout(Cluster cluster) {
            foreach (var c in cluster.AllClustersDepthFirst())
                foreach (var v in c.nodes.Concat(c.Clusters.Cast<Node>()))
                    Debug.Assert(c.BoundingBox.Contains(v.BoundingBox));
        }
#endif

        /// <summary>
        /// Apply the appropriate layout to the specified cluster and its children (bottom up)
        /// </summary>
        /// <param name="cluster">the root of the cluster hierarchy to lay out</param>
        /// <returns>list of edges external to the cluster</returns>
        void LayoutCluster(Cluster cluster) {
            if (cluster.IsCollapsed)
                return;

            LayoutAlgorithmSettings settings = clusterSettings(cluster);
            cluster.UnsetInitialLayoutState();
            if (runInParallel && cluster.Clusters.Count() > 1)
                Parallel.ForEach(cluster.Clusters, parallelOptions, LayoutCluster);
            else
                foreach (var cl in cluster.Clusters)
                    LayoutCluster(cl);

            List<GeometryGraph> components = (List<GeometryGraph>)GetComponents(cluster, settings.LiftCrossEdges, settings.NodeSeparation);

            //currentComponentFraction = (1.0 / clusterCount) / components.Count;

            //            if (runInParallel)
            //                Parallel.ForEach(components, parallelOptions, comp => LayoutComponent(settings, comp));
            //            else // debug!!!!!!
            components.ForEach(c => LayoutComponent(settings, c));

            var bounds = MdsGraphLayout.PackGraphs(components, settings);

            foreach (var g in components)
                FixOriginalGraph(g, true);

            cluster.UpdateBoundary(bounds);

            cluster.SetInitialLayoutState(settings.ClusterMargin);
            cluster.RaiseLayoutDoneEvent();

            //            var l = new List<DebugCurve>();
            //            foreach (var node in cluster.Nodes) {
            //                l.Add(new DebugCurve(node.BoundaryCurve));
            //            }
            //            foreach (var cl in cluster.AllClustersDepthFirstExcludingSelf()) {
            //                l.Add(new DebugCurve(cl.BoundaryCurve));
            //                l.AddRange(cl.Nodes.Select(n=>new DebugCurve(n.BoundaryCurve)));
            //            }
            //            LayoutAlgorithmSettings.ShowDebugCurves(l.ToArray());
        }

        internal static void FixOriginalGraph(GeometryGraph graph, bool translateEdges) {
            foreach (var v in graph.Nodes) {
                var originalNode = (Node)v.UserData;
                var delta = v.BoundingBox.LeftBottom - originalNode.BoundingBox.LeftBottom;
                var cluster = originalNode as Cluster;
                if (cluster != null) {
                    cluster.DeepTranslation(delta, translateEdges);
                }
                else
                    originalNode.Center += delta;
            }
            if (translateEdges) {
                foreach (var e in graph.Edges) {
                    if (e.UserData is Edge) {
                        var originalEdge = e.UserData as Edge;
                        if (e.Curve != null)
                            originalEdge.Curve = e.Curve.Clone();
                        originalEdge.Length = e.Length;

                        originalEdge.EdgeGeometry.SourcePort = e.SourcePort = null;
                        // EdgeGeometry ports get clobbered by edge routing          
                        originalEdge.EdgeGeometry.TargetPort = e.TargetPort = null;

                        foreach (var l in originalEdge.Labels)
                            l.GeometryParent = originalEdge;
                    }
                }
            }
        }

        /// <summary>
        /// Check if root is an ancestor of node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="root"></param>
        /// <returns>true if the node is a descendant of root</returns>
        static bool IsDescendant(Node node, Cluster root) {
            return Ancestor(node, root) != null;
        }

        /// <summary>
        /// Creates a shallow copy of the root cluster and divides into GeometryGraphs each of which is a connected component with
        /// respect to edges internal to root.
        /// </summary>
        /// <param name="cluster">cluster to break into components</param>
        /// <param name="liftCrossEdges">set this to consider lower-level edges while arranging subclusters</param>
        /// <returns>GeometryGraphs that are each a connected component</returns>
        static IEnumerable<GeometryGraph> GetComponents(Cluster cluster, bool liftCrossEdges, double nodeSeparation) {
            // Create a copy of the cluster's nodes. Some or all of these may also be clusters. We call these "top nodes".
            Dictionary<Node, Node> originalToCopyNodeMap = ShallowNodeCopyDictionary(cluster);
            var copiedEdges = new List<Edge>();
            // Iterate on the top nodes.
            foreach (var target in originalToCopyNodeMap.Keys) {
                foreach (var e in target.InEdges) {
                    // Filippo Polo: I'm not sure what's going on here. This seems to be testing whether the source node of the edge is a child of the cluster. But the edge comes from enumerating the in-edges of the target, so the source node is always the target. And the target comes from enumerating the result of a shallow node copy of the cluster. So how could it ever NOT be a child of the cluster? I.e. it looks to me like the following test is always true. Maybe this is a remnant of an earlier attempt to implement edge lifting (see below)?
                    var sourceAncestorUnderRoot = Ancestor(e.Source, cluster);
                    if (sourceAncestorUnderRoot == e.Source)
                        //it is a flat edge and we are only interested in flat edges
                        copiedEdges.Add(CopyEdge(originalToCopyNodeMap, e, sourceAncestorUnderRoot, target));
                }
                copiedEdges.AddRange(target.SelfEdges.Select(e => CopyEdge(originalToCopyNodeMap, e)));

                // If this is a cluster, then lift the edges of contained nodes. This allows me to consider edges that connect cluster components as if they connected the clusters themselves, for the purpose of laying out clusters.
                if (liftCrossEdges && target is Cluster) {
                    var targetCluster = target as Cluster;
                    // Iterate on all sub nodes.
                    foreach (var sub in targetCluster.AllSuccessorsWidthFirst())
                        // Iterate on all the in-edges of the sub node.
                        foreach (var e in sub.InEdges) {
                            // I already know that the target of this edge is contained within the top node. Where is the source of the edge?
                            var sourceAncestorUnderRoot = Ancestor(e.Source, cluster);
                            // If the source of the edge is NOT the current top node, then this is an edge that crosses clusters. Note that this may also be null, if it connects to an entirely different cluster; in this case, it will be considered at a higher level.
                            if (sourceAncestorUnderRoot != null && sourceAncestorUnderRoot != target) {
                                // I'm adding a "virtual" (i.e. not actually in the graph) edge to the list, which serves to make these clusters considered to be connected. Note that the source is necessarily in the set of copied nodes, because it was returned by the Ancestor function, which returns top nodes or null (and null is excluded).
                                var virtualEdge = new Edge(originalToCopyNodeMap[sourceAncestorUnderRoot], originalToCopyNodeMap[target]);
                                copiedEdges.Add(virtualEdge);
                            }
                        }
                }
            }

            return GraphConnectedComponents.CreateComponents(originalToCopyNodeMap.Values.ToArray(), copiedEdges, nodeSeparation);
        }

        /// <summary>
        /// Create a copy of the edge using the specified original source and target, store the original in user data
        /// </summary>
        /// <param name="originalToCopyNodeMap">mapping from original nodes to their copies</param>
        /// <param name="originalEdge">edge to copy</param>
        /// <param name="originalSource">take this as the source node for the edge (e.g. an ancestor of the actual source)</param>
        /// <param name="originalTarget">take this as the target node for the edge (e.g. an ancestor of the actual target)</param>
        /// <returns></returns>
        internal static Edge CopyEdge(Dictionary<Node, Node> originalToCopyNodeMap, Edge originalEdge,
            Node originalSource,
            Node originalTarget) {
            var e = new Edge(originalToCopyNodeMap[originalSource], originalToCopyNodeMap[originalTarget]) {
                EdgeGeometry = originalEdge.EdgeGeometry,
                SourcePort = null,
                TargetPort = null,
                Length = originalEdge.Length,
                UserData = originalEdge
            };

            foreach (var l in originalEdge.Labels) {
                e.Labels.Add(l);
                l.GeometryParent = e;
            }
            return e;
        }

        /// <summary>
        /// Copy the specified edge, use the given dictionary to find the copies of the edges source and target nodes
        /// </summary>
        /// <param name="originalToCopyNodeMap">mapping from original nodes to their copies</param>
        /// <param name="originalEdge"></param>
        /// <returns>Copy of edge</returns>
        internal static Edge CopyEdge(Dictionary<Node, Node> originalToCopyNodeMap, Edge originalEdge) {
            return CopyEdge(originalToCopyNodeMap, originalEdge, originalEdge.Source, originalEdge.Target);
        }

        /// <summary>
        /// Copy the cluster's child Nodes and Clusters as nodes and return a mapping of original to copy.
        /// The reverse mapping (copy to original) is available via the copy's UserData
        /// </summary>
        /// <param name="cluster">Cluster whose contents will be copied</param>
        /// <returns>the mapping from original to copy</returns>
        internal static Dictionary<Node, Node> ShallowNodeCopyDictionary(Cluster cluster) {
            var originalNodeToCopy = new Dictionary<Node, Node>();

            foreach (var v in cluster.Nodes)
                originalNodeToCopy[v] = new Node(v.BoundaryCurve.Clone()) { UserData = v };

            foreach (var cl in cluster.Clusters) {
                if (cl.IsCollapsed)
                    originalNodeToCopy[cl] = new Node(cl.CollapsedBoundary.Clone()) { UserData = cl };
                else {
                    if (cl.BoundaryCurve == null)
                        cl.BoundaryCurve = cl.RectangularBoundary.RectangularHull();

                    originalNodeToCopy[cl] = new Node(cl.BoundaryCurve.Clone()) { UserData = cl };
                }
            }

            return originalNodeToCopy;
        }

        /// <summary>
        /// find ancestor of node that is immediate child of root, or node itself if node is a direct child of root
        /// null if none
        /// </summary>
        /// <param name="node"></param>
        /// <param name="root"></param>
        /// <returns>returns highest ancestor of node (or node itself) that is a direct child of root, null if not 
        /// a descendent of root</returns>
        internal static Node Ancestor(Node node, Cluster root) {
            if (node.ClusterParent == root)
                return node;
            foreach (var c in node.AllClusterAncestors)
                if (c.ClusterParent == root)
                    return c;
            return null;
        }



        internal void LayoutComponent(LayoutAlgorithmSettings settings, GeometryGraph component) {
            var fdSettings = settings as FastIncrementalLayoutSettings;
            var mdsSettings = settings as MdsLayoutSettings;
            var layeredSettings = settings as SugiyamaLayoutSettings;
            if (fdSettings != null) {
                ForceDirectedLayout(fdSettings, component);
            }
            else if (mdsSettings != null) {
                MDSLayout(mdsSettings, component);
            }
            else if (layeredSettings != null) {
                LayeredLayout(layeredSettings, component);
            }
            else {
                throw new NotImplementedException("Unknown type of layout settings!");
            }
            //LayoutAlgorithmSettings.ShowGraph(component);
        }

        void ForceDirectedLayout(FastIncrementalLayoutSettings settings, GeometryGraph component) {
            LayoutAlgorithmHelpers.ComputeDesiredEdgeLengths(settings.IdealEdgeLength, component);
            var layout = new InitialLayout(component, settings) { SingleComponent = true };
            layout.Run(this.CancelToken);
            InitialLayoutHelpers.RouteEdges(component, settings, this.CancelToken);
            InitialLayoutHelpers.PlaceLabels(component, this.CancelToken);
            InitialLayoutHelpers.FixBoundingBox(component, settings);
        }

        void MDSLayout(MdsLayoutSettings settings, GeometryGraph component) {
            LayoutAlgorithmHelpers.ComputeDesiredEdgeLengths(settings.EdgeConstraints, component);
            var layout = new MdsGraphLayout(settings, component);
            layout.Run(this.CancelToken);
            InitialLayoutHelpers.RouteEdges(component, settings, this.CancelToken);
            InitialLayoutHelpers.PlaceLabels(component, this.CancelToken);
            InitialLayoutHelpers.FixBoundingBox(component, settings);
        }

        void LayeredLayout(SugiyamaLayoutSettings layeredSettings, GeometryGraph component) {
            var layeredLayout = new LayeredLayout(component, layeredSettings);
            layeredLayout.SetCancelToken(this.CancelToken);
            double aspectRatio = layeredLayout.EstimateAspectRatio();
            double edgeDensity = (double)component.Edges.Count / component.Nodes.Count;

            // if the estimated aspect ratio is not in the range below then we fall back to force directed layout
            // with constraints which is both faster and usually returns a better aspect ratio for largish graphs
            var fallbackLayoutSettings = layeredSettings.FallbackLayoutSettings;
            if (fallbackLayoutSettings != null &&
                (component.Nodes.Count > 50 && edgeDensity > 2 // too dense
                 || component.Nodes.Count > 40 && edgeDensity > 3.0 // too dense
                 || component.Nodes.Count > 30 && edgeDensity > 4.0 // too dense
                 || component.Nodes.Count > 30 && aspectRatio > layeredSettings.MaxAspectRatioEccentricity // too wide
                 || component.Nodes.Count > 30 && aspectRatio < 1d / layeredSettings.MaxAspectRatioEccentricity
                    // too high
                    )) {
                // for large graphs there's really no point trying to produce nice edge routes
                // the sugiyama edge routing can be quite circuitous on large graphs anyway
                var prevEdgeRouting = fallbackLayoutSettings.EdgeRoutingSettings.EdgeRoutingMode;
                if (component.Nodes.Count > 100 && edgeDensity > 2.0)
                    fallbackLayoutSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.StraightLine;

                LayoutComponent(fallbackLayoutSettings, component);
                fallbackLayoutSettings.EdgeRoutingSettings.EdgeRoutingMode = prevEdgeRouting;
            }
            else {
                var prevEdgeRouting = layeredSettings.EdgeRoutingSettings.EdgeRoutingMode;
                // for large graphs there's really no point trying to produce nice edge routes
                // the sugiyama edge routing can be quite circuitous on large graphs anyway
                if (component.Nodes.Count > 100 && edgeDensity > 2.0) {
                    layeredSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.StraightLine;
                }
                layeredLayout.Run(this.CancelToken);
                layeredSettings.EdgeRoutingSettings.EdgeRoutingMode = prevEdgeRouting;
                InitialLayoutHelpers.FixBoundingBox(component, layeredSettings);
            }
            //LayoutAlgorithmSettings.ShowGraph(component);
        }


#if TEST_MSAGL_AND_HAVEGRAPHVIEWERGDI
        protected static void ShowGraphInDebugViewer(GeometryGraph graph)
        {
            if (graph == null)
            {
                return;
            }
            Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
            //FixNullCurveEdges(graph.Edges);
            var debugCurves = graph.Nodes.Select(n => n.BoundaryCurve).Select(c => new DebugCurve("red", c));
            debugCurves = debugCurves.Concat(graph.RootCluster.AllClustersDepthFirst().Select(c => c.BoundaryCurve).Select(c => new DebugCurve("green", c)));
            debugCurves = debugCurves.Concat(graph.Edges.Select(e => new DebugCurve(120, 1, "blue", e.Curve)));
            debugCurves = debugCurves.Concat(graph.Edges.Where(e => e.Label != null).Select(e => new DebugCurve("green", CurveFactory.CreateRectangle(e.LabelBBox))));
            var arrowHeadsAtSource = from e in graph.Edges
                                     where e.Curve != null && e.EdgeGeometry.SourceArrowhead != null
                                     select new DebugCurve(120, 2, "black", new LineSegment(e.Curve.Start, e.EdgeGeometry.SourceArrowhead.TipPosition));
            var arrowHeadsAtTarget = from e in graph.Edges
                                     where e.Curve != null && e.EdgeGeometry.TargetArrowhead != null
                                     select new DebugCurve(120, 2, "black", new LineSegment(e.Curve.End, e.EdgeGeometry.TargetArrowhead.TipPosition));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(debugCurves.Concat(arrowHeadsAtSource).Concat(arrowHeadsAtTarget));
        }
#endif
    }
}