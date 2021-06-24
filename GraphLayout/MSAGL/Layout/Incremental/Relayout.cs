using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using System.Threading.Tasks;
#if TEST_MSAGL
using Microsoft.Msagl.DebugHelpers;
#endif

namespace Microsoft.Msagl.Layout.Initial {
    /// <summary>
    /// todo: 
    /// find a way to compact disconnected components - incremental packing?
    /// animate transitions
    /// </summary>
    public class Relayout : AlgorithmBase {
        readonly GeometryGraph graph;
        readonly IEnumerable<Node> modifiedNodes;

        readonly Func<Cluster, LayoutAlgorithmSettings> clusterSettings;

        readonly Set<Cluster> ancestorsOfModifiedNodes;

        readonly Dictionary<Cluster, HashSet<Node>> addedNodesByCluster = new Dictionary<Cluster, HashSet<Node>>();

        /// <summary>
        /// Recursively lay out the given clusters using the specified settings for each cluster, or if none is given for a particular
        /// cluster then inherit from the cluster's ancestor - or from the specifed defaultSettings.
        /// Clusters (other than the root) will be translated (together with their descendants) such that their 
        /// bottom-left point of their new boundaries are the same as the bottom-left of their old boundaries 
        /// (i.e. clusters are laid-out in place).
        /// </summary>
        /// <param name="graph">The graph being operated on.</param>
        /// <param name="modifiedNodes">The nodes whose bounds are modified.</param>
        /// <param name="addedNodes">Nodes added to the graph - a new initial position will be found for these nodes close to their neighbors</param>
        /// <param name="clusterSettings">Settings to use for each cluster.</param>
        public Relayout(GeometryGraph graph, IEnumerable<Node> modifiedNodes, IEnumerable<Node> addedNodes,
            Func<Cluster, LayoutAlgorithmSettings> clusterSettings) {
            ValidateArg.IsNotNull(graph, "graph");
            ValidateArg.IsNotNull(clusterSettings, "clusterSettings");
#if TEST_MSAGL
            graph.SetDebugIds();
#endif
            this.graph = graph;
            this.modifiedNodes = modifiedNodes;
            this.clusterSettings = clusterSettings;
            ancestorsOfModifiedNodes =
                new Set<Cluster>(modifiedNodes.SelectMany(v => v.AllClusterAncestors));
            if (addedNodes == null) return;
        
            foreach (var v in addedNodes)
                CreateOrGetAddedChildrenOfParent(v.ClusterParent).Add(v);
            ancestorsOfModifiedNodes.InsertRange(addedNodes.SelectMany(v => v.AllClusterAncestors));
        }

        HashSet<Node> CreateOrGetAddedChildrenOfParent(Cluster parent) {
            HashSet<Node> addedChildren;
            addedNodesByCluster.TryGetValue(parent, out addedChildren);
            if (addedChildren == null)
                addedNodesByCluster[parent] = addedChildren = new HashSet<Node>();
            return addedChildren;
        }

        /// <summary>
        /// The actual layout process
        /// </summary>
        protected override void RunInternal() {

            var openedClusters = modifiedNodes.OfType<Cluster>().Where(cl => !cl.IsCollapsed).ToArray();
            if (openedClusters.Length > 0)
                new InitialLayoutByCluster(graph, openedClusters, clusterSettings).Run();

            Visit(graph.RootCluster);

            // routing edges that cross cluster boundaries
            InitialLayoutByCluster.RouteParentEdges(graph, clusterSettings(graph.RootCluster).EdgeRoutingSettings);
            LayoutHelpers.RouteAndLabelEdges(graph, clusterSettings(graph.RootCluster),
                graph.Edges.Where(BetweenClusterOnTheRightLevel), 0, this.CancelToken);

            graph.UpdateBoundingBox();

            ProgressComplete();
        }

        bool BetweenClusterOnTheRightLevel(Edge edge) {
            var sourceAncestors = new Set<Cluster>(edge.Source.AllClusterAncestors);
            var targetAncestors = new Set<Cluster>(edge.Target.AllClusterAncestors);

            return (sourceAncestors*targetAncestors).IsContained(ancestorsOfModifiedNodes);
        }

        // depth first traversal of cluster hierarchy
        // if the cluster is not in initiallayoutstate then visit children and then apply layout
        void Visit(Cluster u) {
            if (u.IsCollapsed || !ancestorsOfModifiedNodes.Contains(u))
                return;

            foreach (var c in u.Clusters)
                Visit(c);

            LayoutCluster(u);
        }

        /// <summary>
        /// Apply the appropriate layout to the specified cluster
        /// </summary>
        /// <param name="cluster">the root of the cluster hierarchy to lay out</param>
        /// <returns>list of edges external to the cluster</returns>
        void LayoutCluster(Cluster cluster) {
            ProgressStep();
            cluster.UnsetInitialLayoutState();
            FastIncrementalLayoutSettings settings = null;
            LayoutAlgorithmSettings s = clusterSettings(cluster);
            Direction layoutDirection = Direction.None;
            if (s is SugiyamaLayoutSettings) {
                var ss = s as SugiyamaLayoutSettings;
                settings = ss.FallbackLayoutSettings != null
                    ? new FastIncrementalLayoutSettings((FastIncrementalLayoutSettings) ss.FallbackLayoutSettings)
                    : new FastIncrementalLayoutSettings();
                layoutDirection = LayeredLayoutEngine.GetLayoutDirection(ss);
            }
            else {
                settings = new FastIncrementalLayoutSettings((FastIncrementalLayoutSettings) s);
            }

            settings.ApplyForces = true;
            settings.MinorIterations = 10;
            settings.AvoidOverlaps = true;
            settings.InterComponentForces = false;
            settings.IdealEdgeLength = new EdgeConstraints {
                Direction = layoutDirection,
                Separation = 30
            };
            settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Spline;

            HashSet<Node> addedNodes;
           
            if (addedNodesByCluster.TryGetValue(cluster, out addedNodes)) {
                // if the structure of the cluster has changed then we apply unconstrained layout first,
                // then introduce structural constraints, and then all constraints
                settings.MinConstraintLevel = 0;
                settings.MaxConstraintLevel = 2;
            }
            else
                settings.MinConstraintLevel = 2;

            GeometryGraph newGraph = GetShallowCopyGraphUnderCluster(cluster);
            LayoutAlgorithmHelpers.ComputeDesiredEdgeLengths(settings.IdealEdgeLength, newGraph);

            // orthogonal ordering constraints preserve the left-of, above-of relationships between existing nodes
            // (we do not apply these to the newly added nodes)
            GenerateOrthogonalOrderingConstraints(
                newGraph.Nodes.Where(v => !addedNodes.Contains(v.UserData as Node)).ToList(), settings);

            LayoutComponent(newGraph, settings);
            //LayoutAlgorithmSettings.ShowGraph(newGraph);
            InitialLayoutByCluster.FixOriginalGraph(newGraph, true);

            cluster.UpdateBoundary(newGraph.BoundingBox);
        }

        /// <summary>
        /// Generate orthogonal ordering constraints to preserve the left/right, above/below relative positions of nodes
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="settings"></param>
        [Conditional("RelayoutOrthogonalOrderingConstraints")]
        void GenerateOrthogonalOrderingConstraints(IEnumerable<Node> nodes, FastIncrementalLayoutSettings settings) {
            Node p = null;
            foreach (var v in graph.Nodes.OrderBy(v => v.Center.X)) {
                if (p != null) {
                    settings.AddStructuralConstraint(new HorizontalSeparationConstraint(p, v, 0.1));
                }
                p = v;
            }
            p = null;
            foreach (var v in graph.Nodes.OrderBy(v => v.Center.Y)) {
                if (p != null) {
                    settings.AddStructuralConstraint(new VerticalSeparationConstraint(p, v, 0.1));
                }
                p = v;
            }
        }

        /// <summary>
        /// Creates a shallow copy of the cluster into a GeometryGraph
        /// </summary>
        /// <param name="cluster">cluster to copy</param>
        /// <returns>cluster children and edges between children in a GeometryGraph</returns>
        static GeometryGraph GetShallowCopyGraphUnderCluster(Cluster cluster) {
            Dictionary<Node, Node> originalToCopyNodeMap = InitialLayoutByCluster.ShallowNodeCopyDictionary(cluster);
            var newGraph = CreateGeometryGraphAndPopulateItWithNodes(originalToCopyNodeMap);

            foreach (var target in originalToCopyNodeMap.Keys)
                foreach (var underNode in AllSuccessors(target)) { 
                    foreach (var e in underNode.InEdges) {
                        var sourceAncestorUnderRoot = InitialLayoutByCluster.Ancestor(e.Source, cluster);
                        if (IsBetweenClusters(sourceAncestorUnderRoot, target))
                            //it is a flat edge and we are only interested in flat edges
                            newGraph.Edges.Add(InitialLayoutByCluster.CopyEdge(originalToCopyNodeMap, e,
                                sourceAncestorUnderRoot, target));
                    }
                    foreach (var e in target.SelfEdges)
                        newGraph.Edges.Add(InitialLayoutByCluster.CopyEdge(originalToCopyNodeMap, e));
                }
            return newGraph;
        }

        static GeometryGraph CreateGeometryGraphAndPopulateItWithNodes(Dictionary<Node, Node> originalToCopyNodeMap) {
            GeometryGraph newGraph = new GeometryGraph();
            foreach (var v in originalToCopyNodeMap.Values)
                newGraph.Nodes.Add(v);
            return newGraph;
        }

        static bool IsBetweenClusters(Node sourceAncestorUnderRoot, Node target) {
            return sourceAncestorUnderRoot != target && sourceAncestorUnderRoot != null;
        }

        static IEnumerable<Node> AllSuccessors(Node node) {
            var ret = new List<Node> {node};
            var cl = node as Cluster;
            if (cl != null)
                foreach (var u in cl.AllSuccessorsWidthFirst())
                    if (u != node) ret.Add(u);
            return ret;
        }

        internal void LayoutComponent(GeometryGraph component, FastIncrementalLayoutSettings settings) {
            // for small graphs (below 100 nodes) do extra iterations
            settings.MaxIterations = LayoutAlgorithmHelpers.NegativeLinearInterpolation(
                component.Nodes.Count,
                /*lowerThreshold:*/ 50, /*upperThreshold:*/ 500, /*minIterations:*/ 3, /*maxIterations:*/ 5);
            settings.MinorIterations = LayoutAlgorithmHelpers.NegativeLinearInterpolation(component.Nodes.Count,
                /*lowerThreshold:*/ 50, /*upperThreshold:*/ 500, /*minIterations:*/ 2, /*maxIterations:*/ 10);

            FastIncrementalLayout fil = new FastIncrementalLayout(component, settings, settings.MinConstraintLevel,
                anyCluster => settings);
            Debug.Assert(settings.Iterations == 0);

            foreach (var level in Enumerable.Range(settings.MinConstraintLevel, settings.MaxConstraintLevel + 1)) {
                if (level != fil.CurrentConstraintLevel) {
                    fil.CurrentConstraintLevel = level;
                    if (level == 2) {
                        settings.MinorIterations = 1;
                        settings.ApplyForces = false;
                    }
                }
                do {
                    fil.Run();
                } while (!settings.IsDone);
            }

            // Pad the graph with margins so the packing will be spaced out.
            component.Margins = settings.ClusterMargin;
            component.UpdateBoundingBox();
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