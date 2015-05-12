using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.ProjectionSolver;

namespace Microsoft.Msagl.Layout.Incremental {
    /// <summary>
    /// Solver for structural separation constraints or non-overlap constraints in a single axis.
    /// Wrapper round all the ProjectionSolver stuff.
    /// </summary>
    class AxisSolver {
        internal List<IConstraint> structuralConstraints = new List<IConstraint>();
        internal int ConstraintLevel;
        /// <summary>
        /// true means this AxisSolver works horizontally
        /// </summary>
        internal bool IsHorizontal {
            get;
            private set;
        }
        internal OverlapRemovalParameters OverlapRemovalParameters = null;
        private bool avoidOverlaps;
        private IEnumerable<FiNode> nodes;
        private IEnumerable<Cluster> clusterHierarchies;
        private Func<Cluster, LayoutAlgorithmSettings> clusterSettings;

        ///<summary>
        /// a delegate that tells us how to get the center of a node used in initialization and constraint generation
        ///</summary>
        public delegate Point InitialCenterDelegateType(FiNode v);
        /// <summary>
        /// Do we even need to do a solve?
        /// </summary>
        internal bool NeedSolve {
            get {
                return avoidOverlaps && ConstraintLevel >= 2 || structuralConstraints.Count > 0 && ConstraintLevel >= 1;
            }
        }
        /// <summary>
        /// Have to reinstantiate if any of these parameters change
        /// </summary>
        /// <param name="isHorizontal"></param>
        /// <param name="nodes"></param>
        /// <param name="clusterHierarchies"></param>
        /// <param name="avoidOverlaps"></param>
        /// <param name="constraintLevel"></param>
        /// <param name="clusterSettings"></param>
        internal AxisSolver(bool isHorizontal, IEnumerable<FiNode> nodes, IEnumerable<Cluster> clusterHierarchies, bool avoidOverlaps, int constraintLevel, Func<Cluster, LayoutAlgorithmSettings> clusterSettings)
        {
            this.IsHorizontal = isHorizontal;
            this.nodes = nodes;
            this.clusterHierarchies = clusterHierarchies;
            this.avoidOverlaps = avoidOverlaps;
            this.ConstraintLevel = constraintLevel;
            this.clusterSettings = clusterSettings;
        }
        /// <summary>
        /// Add the constraint to this axis
        /// </summary>
        /// <param name="c"></param>
        internal void AddStructuralConstraint(IConstraint c) {
            structuralConstraints.Add(c);
        }
        Solver solver;
        ConstraintGenerator cg;
        /// <summary>
        /// Create variables, generate non-overlap constraints.
        /// </summary>
        /// <param name="hPad">horizontal node padding</param>
        /// <param name="vPad">vertical node padding</param>
        /// <param name="cHPad">horizontal cluster padding</param>
        /// <param name="cVPad">vertical cluster padding</param>
        /// <param name="nodeCenter"></param>
        internal void Initialize(double hPad, double vPad, double cHPad, double cVPad, InitialCenterDelegateType nodeCenter) {
            // For the Vertical ConstraintGenerator, Padding is vPad and PadddingP(erpendicular) is hPad.
            cg = new ConstraintGenerator(IsHorizontal
                                            , IsHorizontal ? hPad : vPad
                                            , IsHorizontal ? vPad : hPad
                                            , IsHorizontal ? cHPad : cVPad
                                            , IsHorizontal ? cVPad : cHPad);
            solver = new Solver();

            foreach (var filNode in nodes) {
                filNode.SetOlapNode(IsHorizontal,null);
            }
            // Calculate horizontal non-Overlap constraints.  
            if (avoidOverlaps && clusterHierarchies != null) {
                foreach (var c in clusterHierarchies) {
                    AddOlapClusters(cg, null /* OlapParentCluster */, c, nodeCenter);
                }
            }

            foreach (var filNode in nodes) {
                if (filNode.getOlapNode(IsHorizontal) == null) {
                    AddOlapNode(cg, cg.DefaultClusterHierarchy /* olapParentCluster */, filNode, nodeCenter);
                }
                filNode.getOlapNode(IsHorizontal).CreateVariable(solver);
            }
            if (avoidOverlaps && this.ConstraintLevel >= 2) {
                cg.Generate(solver, OverlapRemovalParameters);
            }
            AddStructuralConstraints();
        }

        /// <summary>
        /// Do it!
        /// </summary>
        /// <returns></returns>
        internal Solution Solve() {
            // This updates the mOlapNode and clears the mOlapNode.Variable property.
            // We do just one solve over all the cluster constraints for the whole hierarchy.
            // It returns a list of lists of unsatisfiable constraints, or NULL.
            Solution solution = cg.Solve(solver, null /*parameters*/, false /* doGenerate */);

            // Update the positions.
            if (avoidOverlaps && clusterHierarchies != null) {
                foreach (var c in clusterHierarchies) {
                    // Don't update the root cluster of the hierarachy as it doesn't have borders.
                    UpdateOlapClusters(c.Clusters);
                }
            }
            foreach (FiNode v in nodes) {
                // Set the position from the constraint solution on this axis.
                v.UpdatePos(IsHorizontal);
            }
            this.DebugVerifyClusterHierarchy(solution);
            return solution;
        }
        /// <summary>
        /// Must be called before Solve if the caller has updated Variable Initial Positions
        /// </summary>
        internal void SetDesiredPositions() {
            foreach (var v in nodes) {
                v.SetVariableDesiredPos(IsHorizontal);
            }
            solver.UpdateVariables();
        }

        private void AddStructuralConstraints() {
            // Add the vertical structural constraints to the auto-generated ones. 
            foreach (var c in structuralConstraints) {
                if (ConstraintLevel >= c.Level) {
                    var hc = c as HorizontalSeparationConstraint;
                    if (hc != null && IsHorizontal) {
                        FiNode u = (FiNode)(hc.LeftNode.AlgorithmData);
                        FiNode v = (FiNode)(hc.RightNode.AlgorithmData);
                        solver.AddConstraint(u.getOlapNode(IsHorizontal).Variable, v.getOlapNode(IsHorizontal).Variable, hc.Separation, hc.IsEquality);
                    }
                    var vc = c as VerticalSeparationConstraint;
                    if (vc != null && !IsHorizontal) {
                        FiNode u = (FiNode)(vc.TopNode.AlgorithmData);
                        FiNode v = (FiNode)(vc.BottomNode.AlgorithmData);
                        solver.AddConstraint(u.getOlapNode(IsHorizontal).Variable, v.getOlapNode(IsHorizontal).Variable, vc.Separation, vc.IsEquality);
                    }
                }
            }
        }

        private void AddOlapClusters(ConstraintGenerator generator, OverlapRemovalCluster olapParentCluster, Cluster incClus, InitialCenterDelegateType nodeCenter)
        {
            LayoutAlgorithmSettings settings = clusterSettings(incClus);
            double nodeSeparationH = settings.NodeSeparation;
            double nodeSeparationV = settings.NodeSeparation + 1e-4;
            double innerPaddingH = settings.ClusterMargin;
            double innerPaddingV = settings.ClusterMargin + 1e-4;

            // Creates the OverlapRemoval (Olap) Cluster/Node objects for our FastIncrementalLayout (FIL) objects.
            // If !isHorizontal this overwrites the Olap members of the Incremental.Clusters and Msagl.Nodes.

            // First create the olapCluster for the current incCluster.  If olapParentCluster is null, then
            // incCluster is the root of a new hierarchy.
            RectangularClusterBoundary rb = incClus.RectangularBoundary;
            if (IsHorizontal)
            {
                rb.olapCluster = generator.AddCluster(
                    olapParentCluster,
                    incClus /* userData */,
                    rb.MinWidth,
                    rb.MinHeight,
                    rb.LeftBorderInfo,
                    rb.RightBorderInfo,
                    rb.BottomBorderInfo,
                    rb.TopBorderInfo);
                rb.olapCluster.NodePadding = nodeSeparationH;
                rb.olapCluster.NodePaddingP = nodeSeparationV;
                rb.olapCluster.ClusterPadding = innerPaddingH;
                rb.olapCluster.ClusterPaddingP = innerPaddingV;
            }
            else
            {
                var postXLeftBorderInfo = new BorderInfo(rb.LeftBorderInfo.InnerMargin, rb.Rect.Left, rb.LeftBorderInfo.Weight);
                var postXRightBorderInfo = new BorderInfo(rb.RightBorderInfo.InnerMargin, rb.Rect.Right, rb.RightBorderInfo.Weight);
                rb.olapCluster = generator.AddCluster(
                    olapParentCluster,
                    incClus /* userData */,
                    rb.MinHeight,
                    rb.MinWidth,
                    rb.BottomBorderInfo,
                    rb.TopBorderInfo,
                    postXLeftBorderInfo,
                    postXRightBorderInfo);
                rb.olapCluster.NodePadding = nodeSeparationV;
                rb.olapCluster.NodePaddingP = nodeSeparationH;
                rb.olapCluster.ClusterPadding = innerPaddingV;
                rb.olapCluster.ClusterPaddingP = innerPaddingH;
            }
            rb.olapCluster.TranslateChildren = rb.GenerateFixedConstraints;
            // Note: Incremental.Cluster always creates child List<Cluster|Node> so we don't have to check for null here.
            // Add our child nodes.
            foreach (var filNode in incClus.Nodes)
            {
                AddOlapNode(generator, rb.olapCluster, (FiNode)filNode.AlgorithmData, nodeCenter);
            }

            // Now recurse through all child clusters.
            foreach (var incChildClus in incClus.Clusters)
            {
                AddOlapClusters(generator, rb.olapCluster, incChildClus, nodeCenter);
            }
        }

        private void AddOlapNode(ConstraintGenerator generator, OverlapRemovalCluster olapParentCluster, FiNode filNode, InitialCenterDelegateType nodeCenter) {
            // If the node already has an mOlapNode, it's already in a cluster (in a different
            // hierarchy); we just add it to the new cluster.
            if (null != filNode.getOlapNode(IsHorizontal)) {
                generator.AddNodeToCluster(olapParentCluster, filNode.getOlapNode(IsHorizontal));
                return;
            }

            var center = nodeCenter(filNode);
            // We need to create a new Node in the Generator.
            if (IsHorizontal) {
                // Add the Generator node with the X-axis coords primary, Y-axis secondary.
                filNode.mOlapNodeX = generator.AddNode(olapParentCluster, filNode /* userData */
                                    , center.X, center.Y
                                    , filNode.Width, filNode.Height, filNode.stayWeight);
            } else {
                // Add the Generator node with the Y-axis coords primary, X-axis secondary.
                filNode.mOlapNodeY = generator.AddNode(olapParentCluster, filNode /* userData */
                                    , center.Y, center.X
                                    , filNode.Height, filNode.Width, filNode.stayWeight);
            }
        }

        private void UpdateOlapClusters(IEnumerable<Cluster> incClusters) {
            foreach (var incClus in incClusters) {
                RectangularClusterBoundary rb = incClus.RectangularBoundary;
                // Because two heavily-weighted nodes can force each other to move, we have to update
                // any BorderInfos that are IsFixedPosition to reflect this possible movement; for example,
                // a fixed border and a node being dragged will both have heavy weights.
                if (IsHorizontal) {
                    rb.rectangle.Left = rb.olapCluster.Position - (rb.olapCluster.Size / 2);
                    rb.rectangle.Right = rb.olapCluster.Position + (rb.olapCluster.Size / 2);
                    if (rb.LeftBorderInfo.IsFixedPosition) {
                        rb.LeftBorderInfo = new BorderInfo(
                                rb.LeftBorderInfo.InnerMargin, rb.rectangle.Left, rb.LeftBorderInfo.Weight);
                    }
                    if (rb.RightBorderInfo.IsFixedPosition) {
                        rb.RightBorderInfo = new BorderInfo(
                                rb.RightBorderInfo.InnerMargin, rb.rectangle.Right, rb.RightBorderInfo.Weight);
                    }
                } else {
                    rb.rectangle.Bottom = rb.olapCluster.Position - (rb.olapCluster.Size / 2);
                    rb.rectangle.Top = rb.olapCluster.Position + (rb.olapCluster.Size / 2);
                    if (rb.TopBorderInfo.IsFixedPosition) {
                        rb.TopBorderInfo = new BorderInfo(
                                rb.TopBorderInfo.InnerMargin, rb.rectangle.Top, rb.TopBorderInfo.Weight);
                    }
                    if (rb.BottomBorderInfo.IsFixedPosition) {
                        rb.BottomBorderInfo = new BorderInfo(
                                rb.BottomBorderInfo.InnerMargin, rb.rectangle.Bottom, rb.BottomBorderInfo.Weight);
                    }
                }

                // We don't use this anymore now that we've transferred the position and size
                // so clean it up as the Gen/Solver will be going out of scope.
                rb.olapCluster = null;

                // Recurse.
                UpdateOlapClusters(incClus.Clusters);
            }
        }

        [Conditional("VERIFY")]
        private void DebugVerifyClusterHierarchy(Solution solution)
        {
            if (avoidOverlaps && (null != clusterHierarchies) && (0 != solution.NumberOfUnsatisfiableConstraints ))
            {
                foreach (var c in clusterHierarchies)
                    DebugVerifyClusters(cg, c, c);
            }
        }

        // This is initially called with Clusters that live at the root level; verify their nodes
        // are within their boundaries, then recurse.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "jjNodeRect"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "jjClusRect"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "incClusComp")]
        [Conditional("VERIFY")]
        private void DebugVerifyClusters(ConstraintGenerator generator, Cluster incCluster, Cluster root) {
            double dblEpsilon = 0.0001;

            // First verify that all nodes are within the cluster.
            Rectangle clusRect = incCluster.RectangularBoundary.rectangle;
            foreach (var v in incCluster.Nodes) {
                FiNode iiFilNode = (FiNode)v.AlgorithmData;
                Rectangle iiNodeRect = iiFilNode.mNode.BoundaryCurve.BoundingBox;

                if (IsHorizontal) {
                    // Don't check containment for the root ClusterHierarchy as there is no border for it.
                    if (incCluster != root) {
                        // This is horizontal so we've not yet calculated the Y-axis stuff.  The only thing we
                        // can do is verify we're within cluster X bounds.  If *Space is negative, there's overlap.
                        // Generator primary axis is horizontal so use its Padding.
                        double dblLboundSpace = iiNodeRect.Left - clusRect.Left - generator.Padding;
                        double dblRboundSpace = clusRect.Right - iiNodeRect.Right - generator.Padding;
                        Debug.Assert((dblLboundSpace >= -dblEpsilon) && (dblRboundSpace >= -dblEpsilon)
                                    , "Node is not within parent Cluster");
                    }
                } else {
                    // Don't check containment for the root ClusterHierarchy as there is no border for it.
                    if (incCluster != root) {
                        // This is vertical so we've calculated the Y-axis stuff and horizontal is Perpendicular.
                        DebugVerifyRectContains(clusRect, iiNodeRect
                                                , generator.PaddingP, generator.Padding, dblEpsilon);
                    }
                    // Make sure the node doesn't intersect any following nodes, or any clusters.
                    foreach (var u in incCluster.Nodes) {
                        if (u == v) continue;
                        FiNode jjFilNode = (FiNode)u.AlgorithmData;
                        Rectangle jjNodeRect = jjFilNode.mNode.BoundaryCurve.BoundingBox;

                        // We've already added the padding for the node so don't add it for the jjNode/Cluster.
                        DebugVerifyRectsDisjoint(iiNodeRect, jjNodeRect
                                                , generator.PaddingP, generator.Padding, dblEpsilon);
                    }
                    foreach (Cluster incClusComp in incCluster.Clusters)
                    {
                        DebugVerifyRectsDisjoint(iiNodeRect, incClusComp.RectangularBoundary.rectangle
                                            , generator.PaddingP, generator.Padding, dblEpsilon);
                    }
                } // endif isHorizontal
            } // endfor iiNode

            // Now verify the clusters are contained and don't overlap.
            foreach (var iiIncClus in incCluster.Clusters) {
                Rectangle iiClusRect = iiIncClus.RectangularBoundary.rectangle;

                if (IsHorizontal) {
                    // Don't check containment for the root ClusterHierarchy as there is no border for it.
                    if (incCluster != root) {
                        // This is horizontal so we've not yet calculated the Y-axis stuff.  The only thing we
                        // can do is verify we're within cluster X bounds.  If *Space is negative, there's overlap.
                        // Generator primary axis is horizontal so use its Padding.
                        double dblLboundSpace = iiClusRect.Left - clusRect.Left - generator.Padding;
                        double dblRboundSpace = clusRect.Right - iiClusRect.Right - generator.Padding;
                        Debug.Assert((dblLboundSpace >= -dblEpsilon) && (dblRboundSpace >= -dblEpsilon)
                                    , "Cluster is not within parent Cluster");
                    }
                } else {
                    // Don't check containment for the root ClusterHierarchy as there is no border for it.
                    if (incCluster != root) {
                        // This is vertical so we've calculated the Y-axis stuff and horizontal is Perpendicular.
                        DebugVerifyRectContains(clusRect, iiClusRect
                                                , generator.PaddingP, generator.Padding, dblEpsilon);
                    }
                    // Make sure the cluster doesn't intersect any following clusters.
                    foreach (var jjIncClus in incCluster.Clusters) {
                        if (jjIncClus == iiIncClus) continue;
                        Rectangle jjClusRect = jjIncClus.RectangularBoundary.rectangle;
                        DebugVerifyRectsDisjoint(iiClusRect, jjClusRect
                                                , generator.PaddingP, generator.Padding, dblEpsilon);
                    }
                } // endif isHorizontal

                // Now recurse.
                DebugVerifyClusters(generator, iiIncClus, root);
            } // endfor iiCluster
        }

        [Conditional("VERIFY")]
        static void DebugVerifyRectContains(Rectangle rectOuter, Rectangle rectInner, double dblPaddingX, double dblPaddingY, double dblEpsilon)
        {
            rectInner.PadWidth(dblPaddingX/2.0 - dblEpsilon);
            rectInner.PadHeight(dblPaddingY/2.0 - dblEpsilon);
            Debug.Assert(rectOuter.Contains(rectInner)
                        , "Inner Node/Cluster rectangle is not contained within outer Cluster"
                        );
        }

        [Conditional("VERIFY")]
        static void DebugVerifyRectsDisjoint(Rectangle rect1, Rectangle rect2, double dblPaddingX, double dblPaddingY, double dblEpsilon)
        {
            rect1.PadWidth(dblPaddingX/2.0 - dblEpsilon);
            rect1.PadHeight(dblPaddingY/2.0 - dblEpsilon);
            rect2.PadWidth(dblPaddingX/2.0 - dblEpsilon);
            rect2.PadHeight(dblPaddingY/2.0 - dblEpsilon);
            Debug.Assert(!rect1.Intersects(rect2));
        }
    } // end class AxisSolver
} // end namespace Microsoft.Msagl.Incremental
