// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConstraintGenerator.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL main class for Overlap removal constraint generation for Projection solutions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Msagl.Core.Geometry
{
    /// <summary>
    /// ConstraintGenerator is the driving class for overlap removal.  The caller
    /// adds variables (so it is similar to ProjectionSolver in that way, and in
    /// fact the variables added here are passed to the ProjectionSolver to solve
    /// the generated constraints).
    /// </summary>
    public class ConstraintGenerator
    {
        // A ClusterHierarchy is simply a Cluster (there is no ClusterHierarchy class).
        // This contains always at least one hierarchy, the DefaultClusterHierarchy, which is
        // created in the ConstraintGenerator constructor.  All nodes live in a ClusterHierarchy;
        // AddNode and AddCluster pass the appropriate cluster (DefaultClusterHierarchy,
        // a ClusterHierarchy created by AddCluster with a null parent Cluster, or a child cluster
        // of one of these clusters).  Thus there is no concept of a single "Root Cluster"; rather,
        // each cluster in clusterHierarchies is the root of a separate hierarchy.
        //
        // We only generate constraints between objects in the same ClusterHierarchy.  Because we
        // enumerate all ClusterHierarchies and recurse into each one, each Hierarchy is completely
        // unaware of the others.  Hierarchies added via AddCluster can be sparse.
        /// <summary>
        /// Read-only enumeration of the ClusterHierarchies; new cluster hierarchies are created
        /// by calling AddCluster 
        /// </summary>
        public IEnumerable<OverlapRemovalCluster> ClusterHierarchies
        {
            get { return this.clusterHierarchies; }
        }
         readonly List<OverlapRemovalCluster> clusterHierarchies = new List<OverlapRemovalCluster>();

        /// <summary>
        /// The initial, default ClusterHierarchy; a "flat" graph (with no user-defined clusters)
        /// lives entirely in this cluster.
        /// </summary>
        public OverlapRemovalCluster DefaultClusterHierarchy { get { return this.clusterHierarchies[0]; } }

        // This is the padding in the relevant direction, and the perpendicular padding that is
        // used if we are doing horizontal constraints (for the "amount of movement" comparisons).
        // It also includes fixed-border specifications.
        /// <summary>
        /// Padding in the direction of the primary axis.
        /// </summary>
        public double Padding { get; private set; }

        /// <summary>
        /// Padding in the secondary (Perpendicular) axis.
        /// </summary>
        public double PaddingP { get; private set; }

        /// <summary>
        /// Padding outside clusters in the parallel direction.
        /// </summary>
        public double ClusterPadding { get; private set; }

        /// <summary>
        /// Padding outside clusters in the perpendicular direction.
        /// </summary>
        public double ClusterPaddingP { get; private set; }

        /// <summary>
        /// Default padding value that is used (in both axes) if no padding is specified when
        /// calling the ConstraintGenerator constructor.
        /// </summary>
        public static double DefaultPadding { get { return 7.0; } }

        // An identifier to avoid duplicates in the ScanLine tree (otherwise the first
        // one encountered gets all the neighbours).  This sequence is shared with Clusters,
        // which are derived from Node; each Cluster consumes 3 IDs, one for the cluster
        // itself and one for each of its fake border nodes.
         uint nextNodeId;

        /// <summary>
        /// As passed to ctor; if this is true, we are doing horizontal (x) constraint generation,
        /// and must therefore consider whether a smaller vertical movement would remove the overlap.
        /// </summary>
        public bool IsHorizontal { get; private set; }

        /// <summary>
        /// This form of the constructor uses default values for the padding parameters.
        /// <param name="isHorizontal">Whether to generate horizontal or vertical constraints</param>
        /// </summary>
        public ConstraintGenerator(bool isHorizontal) : this(isHorizontal, DefaultPadding, DefaultPadding) { }

        /// <summary>
        /// This form of the constructor uses specifies the padding parameters.
        /// <param name="isHorizontal">Whether to generate horizontal or vertical constraints</param>
        /// <param name="padding">Padding outside nodes in the parallel direction</param>
        /// <param name="paddingP">Padding outside nodes in the perpendicular direction</param>
        /// <param name="clusterPadding">Padding outside clusters in the parallel direction</param>
        /// <param name="clusterPaddingP">Padding outside clusters in the perpendicular direction</param>
        /// </summary>
        public ConstraintGenerator(bool isHorizontal, double padding, double paddingP, double clusterPadding, double clusterPaddingP)
        {
            ValidateArg.IsPositive(padding, "padding");
            this.IsHorizontal = isHorizontal;
            this.Padding = padding;
            this.PaddingP = paddingP;
            this.ClusterPadding = clusterPadding;
            this.ClusterPaddingP = clusterPaddingP;

            // Create the DefaultClusterHierarchy.
            this.clusterHierarchies.Add(new OverlapRemovalCluster(0, null /* parentCluster */, 0 /* default userData is 0 id */, Padding, PaddingP));
            this.nextNodeId += OverlapRemovalCluster.NumInternalNodes;
        }

        /// <summary>
        /// Alternate form of the constructor to allow overriding the default padding.
        /// </summary>
        /// <param name="isHorizontal">Whether to generate horizontal or vertical constraints</param>
        /// <param name="padding">Minimal space between node or cluster rectangles in the primary axis.</param>
        /// <param name="paddingP">Minimal space between node or cluster rectangles in the secondary (Perpendicular) axis;
        ///                         used only when isHorizontal is true, to optimize the direction of movement.</param>
        public ConstraintGenerator(bool isHorizontal, double padding, double paddingP) : this(isHorizontal, padding, paddingP, padding, paddingP) { }

        /// <summary>
        /// Add a new variable to the ConstraintGenerator.
        /// </summary>
        /// <param name="initialCluster">The cluster this node is to be a member of.  It may not be null; pass
        ///                     DefaultClusterHierarchy to create a node at the lowest level.  Subsequently a node 
        ///                     may be added to additional clusters, but only to one cluster per hierarchy.</param>
        /// <param name="userData">An object that is passed through.</param>
        /// <param name="position">Position of the node in the primary axis; if isHorizontal, it contains horizontal
        ///                     position and size, else it contains vertical position and size.</param>
        /// <param name="size">Size of the node in the primary axis.</param>
        /// <param name="positionP">Position of the node in the secondary (Perpendicular) axis.</param>
        /// <param name="sizeP">Size of the node in the secondary (Perpendicular) axis.</param>
        /// <param name="weight">Weight of the node (indicates how freely it should move).</param>
        /// <returns>The created node.</returns>
        public OverlapRemovalNode AddNode(OverlapRemovalCluster initialCluster, object userData, double position,
                            double positionP, double size, double sizeP, double weight)
        {
            ValidateArg.IsNotNull(initialCluster, "initialCluster");
            // @@PERF: Currently every node will have at least one constraint generated if there are any
            // other nodes along its line, regardless of whether the perpendicular coordinates result in overlap.
            // It might be worthwhile to add a check to avoid constraint generation in the case that there cannot
            // be such an overlap on a line, or if the nodes are separated by some amount of distance.
            Debug.Assert(null != initialCluster, "initialCluster must not be null");
            var nodNew = new OverlapRemovalNode(this.nextNodeId++, userData, position, positionP, size, sizeP, weight);
            initialCluster.AddNode(nodNew);
            return nodNew;
        }

        /// <summary>
        /// Creates a new cluster with no minimum size within the specified parent cluster.  Clusters allow creating a subset of
        /// nodes that must be within a distinct rectangle.
        /// </summary>
        /// <param name="parentCluster">The cluster this cluster is to be a member of; if null, this is the root of a
        ///                             new hierarchy, otherwise must be non-NULL (perhaps DefaultClusterHierarchy).</param>
        /// <param name="userData">An object that is passed through.</param>
        /// <param name="openBorderInfo">Information about the Left (if isHorizontal, else Top) border.</param>
        /// <param name="closeBorderInfo">Information about the Right (if isHorizontal, else Bottom) border.</param>
        /// <param name="openBorderInfoP">Same as OpenBorder, but in the secondary (Perpendicular) axis.</param>
        /// <param name="closeBorderInfoP">Same as CloseBorder, but in the secondary (Perpendicular) axis.</param>
        /// <returns>The new Cluster.</returns>
        /// 
        public OverlapRemovalCluster AddCluster(OverlapRemovalCluster parentCluster, object userData,
                            BorderInfo openBorderInfo, BorderInfo closeBorderInfo,
                            BorderInfo openBorderInfoP, BorderInfo closeBorderInfoP)
        {
            return AddCluster(parentCluster, userData, 0.0 /*minSize*/, 0.0 /*minSizeP*/,
                            openBorderInfo, closeBorderInfo, openBorderInfoP, closeBorderInfoP);
        }

        /// <summary>
        /// Creates a new cluster with a minimum size within the specified parent cluster.  Clusters allow creating a subset of
        /// nodes that must be within a distinct rectangle.
        /// </summary>
        /// <param name="parentCluster">The cluster this cluster is to be a member of; if null, this is the root of a
        ///                             new hierarchy, otherwise must be non-NULL (perhaps DefaultClusterHierarchy).</param>
        /// <param name="userData">An object that is passed through.</param>
        /// <param name="minimumSize">Minimum cluster size along the primary axis.</param>
        /// <param name="minimumSizeP">Minimum cluster size along the perpendicular axis.</param>
        /// <param name="openBorderInfo">Information about the Left (if isHorizontal, else Top) border.</param>
        /// <param name="closeBorderInfo">Information about the Right (if isHorizontal, else Bottom) border.</param>
        /// <param name="openBorderInfoP">Same as OpenBorder, but in the secondary (Perpendicular) axis.</param>
        /// <param name="closeBorderInfoP">Same as CloseBorder, but in the secondary (Perpendicular) axis.</param>
        /// <returns>The new Cluster.</returns>
        /// 
        public OverlapRemovalCluster AddCluster(OverlapRemovalCluster parentCluster, object userData,
                            double minimumSize, double minimumSizeP,
                            BorderInfo openBorderInfo, BorderInfo closeBorderInfo,
                            BorderInfo openBorderInfoP, BorderInfo closeBorderInfoP)
        {
            var newCluster = new OverlapRemovalCluster(this.nextNodeId, parentCluster, userData, minimumSize, minimumSizeP,
                            this.Padding, this.PaddingP, this.ClusterPadding, this.ClusterPaddingP,
                            openBorderInfo, closeBorderInfo, openBorderInfoP, closeBorderInfoP);
            this.nextNodeId += OverlapRemovalCluster.NumInternalNodes;
            if (null == parentCluster)
            {
                this.clusterHierarchies.Add(newCluster);
            }
            else
            {
                // @@DCR: Enforce that Clusters live in only one hierarchy - they can have only one parent, so add a 
                //          Cluster.parentCluster to enforce this.
                parentCluster.AddNode(newCluster);
            }
            return newCluster;
        }

        
        /// <summary>
        /// Add a node to a cluster in another hierarchy (a node can be in only one cluster per hierarchy).
        /// </summary>
        /// <param name="cluster"></param>
        /// <param name="node"></param>
        // @@DCR:  Keep a node->hierarchyParentsList hash and use cluster.parentCluster to traverse to the hierarchy root
        //            to verify the node is in one cluster per hierarchy.  This will require that the function be
        //            non-static, hence the rule suppression.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public void AddNodeToCluster(OverlapRemovalCluster cluster, OverlapRemovalNode node)
        {
            // Node derives from Cluster so make sure we don't have this - the only way to create
            // cluster hierarchies is by AddCluster.
            ValidateArg.IsNotNull(cluster, "cluster");
            if (node is OverlapRemovalCluster)
            {
                throw new InvalidOperationException(
#if TEST_MSAGL
                        "Argument 'node' must not be a Cluster"
#endif // TEST_MSAGL
                        );
            }
            cluster.AddNode(node);
        }

        /// <summary>
        /// Generate the necessary constraints to ensure there is no overlap (unless we're doing
        /// a horizontal pass and deferring some movement, which would be smaller, to the vertical pass).
        /// </summary>
        /// <param name="solver">The solver to generate into.</param>
        /// <param name="parameters">Parameters to OverlapRemoval and ProjectionSolver.Solver.Solve().</param>
        public void Generate(ProjectionSolver.Solver solver, OverlapRemovalParameters parameters)
        {
            ValidateArg.IsNotNull(solver, "solver");
            if (null == parameters)
            {
                parameters = new OverlapRemovalParameters();
            }
            foreach (OverlapRemovalCluster cluster in this.clusterHierarchies)
            {
                cluster.Generate(solver, parameters, this.IsHorizontal);
            }

            // For Clusters we reposition their "fake border" variables between the constraint-generation
            // and solving phases, so we need to tell the solver to do this.
            solver.UpdateVariables();   // @@PERF: Not needed if no clusters were created.
        }

        /// <summary>
        /// Generates and solves the constraints.
        /// </summary>
        /// <param name="solver">The solver to generate into and solve.  May be null, in which case one
        ///                     is created by the method.</param>
        /// <param name="parameters">Parameters to OverlapRemoval and ProjectionSolver.Solver.Solve().</param>
        /// <param name="doGenerate">Generate constraints before solving; if false, solver is assumed to
        ///                     have already been populated by this.Generate().</param>
        /// <returns>The set of OverlapRemoval.Constraints that were unsatisfiable, or NULL.</returns>
        public ProjectionSolver.Solution Solve(ProjectionSolver.Solver solver,
                                    OverlapRemovalParameters parameters, bool doGenerate)
        {
            if (null == solver)
            {
                solver = new ProjectionSolver.Solver();
            }
            if (null == parameters)
            {
                parameters = new OverlapRemovalParameters();
            }
            if (doGenerate)
            {
                Generate(solver, parameters);
            }
            ProjectionSolver.Solution solverSolution = solver.Solve(parameters.SolverParameters);
            foreach (OverlapRemovalCluster cluster in this.clusterHierarchies)
            {
                cluster.UpdateFromVariable();   // "recursively" processes all child clusters
            }
            return solverSolution;
        }
    }
}