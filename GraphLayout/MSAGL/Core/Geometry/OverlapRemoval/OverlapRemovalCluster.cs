// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlapRemovalCluster.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL Cluster class for Overlap removal constraint generation for Projection solutions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

// Remove this from project build and uncomment here to selectively enable per-class.
//#define VERBOSE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.ProjectionSolver;

//
// How Clusters Work
//
// As in the doc, we extend the existing constraint-generation mechanism by processing a
// Cluster as a normal Node (which it inherits from), adding a pair of fake variables
// (corresponding to its borders along the primary axis) to the Solver for it. This
// allows us to generate Constraints between Nodes and Clusters at the same level
// (i.e. within the same Cluster).  Clusters may contain Nodes which are other Clusters;
// these form a tree starting at the root of a ClusterHierarchy.  Multiple Cluster
// Hierarchies may exist; each has its constraints evaluated separately from all other
// ClusterHierarchies, thereby allowing intersecting clusters; a common example of this
// is the "layer cake", where # are horizontal cluster borders and =+| are vertical:
//    +===+ +===+ +===+
//    | 0 | | 1 | | 2 |
//  ##|###|#|###|#|###|##
//  # | 3 | | 4 | | 5 | #
//  ##|###|#|###|#|###|##
//    |   | |   | |   |
//  ##|###|#|###|#|###|##
//  # | 6 | | 7 | | 8 | #
//  ##|###|#|###|#|###|##
//    | 9 | | 10| | 11|
//    +===+ +===+ +===+
//
// In terms of flow, we process in a depth-first manner, going down through all Clusters
// to the lowest level before calling the Solver.  Constraints can be generated in either
// the top-down or bottom-up order; doing it bottom-up allows us to extend this to support
// a mode that uses a Solver at the level of each cluster to solve within the cluster so
// we know the location and size of the Cluster *after* its internal nodes have been moved
// to their "solved" positions (@@DCR; see "Precalculate Cluster Sizes" in the design doc).
// 
// Within each Cluster, we add a "fake node" to the event list, one for each Left (Top) or 
// Right (Bottom) border, and then generate constraints using the scan-line algorithm in the doc.
// Using the two "fake nodes" allows the within-border constraints to fall out automatically 
// from the algorithm.
//
// After a Cluster's component Clusters have been solved in this way, then its Clusters are
// processed as simple Nodes in its parent Cluster, including the horizontal/vertical decision
// on least-movement direction.  However, when the Constraints are generated, they're generated
// to the left or right border "fake nodes" rather than to the single node for the cluster.
// We need two nodes here so the cluster can grow or shrink along the axis.
//
// This proceeds until we back up to the root cluster of the ClusterHierarchy.
//
namespace Microsoft.Msagl.Core.Geometry
{
    /// <summary>
    /// A cluster is a structure that acts as a Node for Nodes and Clusters at a sibling level,
    /// and can also contain other Clusters and/or Nodes.
    /// </summary>
    public partial class OverlapRemovalCluster : OverlapRemovalNode
    {
        // Our internal Node list - some of which may be Clusters.
         readonly List<OverlapRemovalNode> nodeList = new List<OverlapRemovalNode>();

        // Our internal child Cluster list - duplicated from nodeList for iterative recursion perf.
         readonly List<OverlapRemovalCluster> clusterList = new List<OverlapRemovalCluster>();

        /// <summary>
        /// Empty clusters are ignored on positioning.
        /// </summary>
        public bool IsEmpty { get { return 0 == this.nodeList.Count; } }

        /// <summary>
        /// If the following is true then constraints will be generated the prevent children coming
        /// any closer to the cluster boundaries.  In effect, this means that the cluster and all
        /// it's children will be translated together rather than being "compressed" if there are
        /// overlaps with external nodes.
        /// </summary>
        // AKA: "Bump Mode"
        public bool TranslateChildren { get; set; }

        // Our internal "fake nodes" as above; these are separate from the size calculations
        // for the overall Cluster.

        /// <summary>
        /// The internal Node containing the Variable to which left-border constraints are made.
        /// </summary>
        public OverlapRemovalNode LeftBorderNode { get; private set; }

        /// <summary>
        /// The internal Node containing the Variable to which right-border constraints are made.
        /// </summary>
        public OverlapRemovalNode RightBorderNode { get; private set; }

        // Indicates if the cluster's GenerateWorker placed anything into the solver.
         bool IsInSolver { get; set; }

        /// <summary>
        /// Opening margin of this cluster (additional space inside the cluster border)
        /// along the primary axis; on Left if horizontal, else on Top.
        /// </summary>
        public BorderInfo OpenBorderInfo { get; private set; }

        /// <summary>
        /// Closing margin of this cluster (additional space inside the cluster border)
        /// along the primary axis; on Right if horizontal, else on Bottom.
        /// </summary>
        public BorderInfo CloseBorderInfo { get; private set; }

        /// <summary>
        /// Opening margin of this cluster (additional space inside the cluster border)
        /// along the secondary (Perpendicular) axis; on Top if horizontal, else on Left.
        /// </summary>
        public BorderInfo OpenBorderInfoP { get; private set; }

        /// <summary>
        /// Closing margin of this cluster (additional space inside the cluster border)
        /// along the secondary (Perpendicular) axis; on Bottom if horizontal, else on Right.
        /// </summary>
        public BorderInfo CloseBorderInfoP { get; private set; }

        /// <summary>
        /// Minimum size along the primary axis.
        /// </summary>
        public double MinimumSize { get; set; }

        /// <summary>
        /// Minimum size along the perpendicular axis.
        /// </summary>
        public double MinimumSizeP { get; set; }

        /// <summary>
        /// Padding of nodes within the cluster in the parallel direction.
        /// </summary>
        public double NodePadding { get; set; }

        /// <summary>
        /// Padding of nodes within the cluster in the perpendicular direction.
        /// </summary>
        public double NodePaddingP { get; set; }

        /// <summary>
        /// Padding outside the cluster in the parallel direction.
        /// </summary>
        public double ClusterPadding { get; set; }

        /// <summary>
        /// Padding outside the cluster in the perpendicular direction.
        /// </summary>
        public double ClusterPaddingP { get; set; }

#if VERBOSE
         string Name {
            get { return "Clus_" + (this.IsRootCluster ? "Root" : (this.LeftBorderNode.UserData + "-" + this.RightBorderNode.UserData)); }
        }
#endif // VERBOSE

        // The number of node IDs used by a Cluster - for the cluster itself and its fake nodes.
        internal static uint NumInternalNodes
        {
            get { return 3; }
        }

        // The width (height) of the node along the primary axis, which should be fairly thin
        // (along the secondary (perpendicular) axis, it is the full size of the cluster).
        internal static double DefaultBorderWidth
        {
            get { return OverlapRemovalGlobalConfiguration.ClusterDefaultBorderWidth; }
        }

        /// <summary>
        /// The Root Cluster is a special case, functioning as the "infinite" root cluster of a hierarchy
        /// with no border nodes.  If a size is desired then create a single cluster in the root.  Leaving
        /// the root cluster "infinite" means we don't have to generate the constraints for nodes and clusters
        /// in the root, which may be numerous.
        /// </summary>
        public bool IsRootCluster { get { return null == this.ParentCluster; } }
        OverlapRemovalCluster ParentCluster { get; set; }

        // Zero cluster margins. This ctor is currently used only by the generator's DefaultClusterHierarchy,
        // which by default is created with non-fixed borders and no margins.
        internal OverlapRemovalCluster(uint id, OverlapRemovalCluster parentCluster, object userData, double padding, double paddingP)
            : this(id, parentCluster, userData, 0, 0, padding, paddingP, 0, 0, new BorderInfo(0.0), new BorderInfo(0.0), new BorderInfo(0.0), new BorderInfo(0.0))
        {
        }

        internal OverlapRemovalCluster(uint id, OverlapRemovalCluster parentCluster,
                        object userData, double minSize, double minSizeP,
                        double nodePadding, double nodePaddingP, double clusterPadding, double clusterPaddingP,
                        BorderInfo openBorderInfo, BorderInfo closeBorderInfo,
                        BorderInfo openBorderInfoP, BorderInfo closeBorderInfoP)
            : base(id, userData)
        {
            this.MinimumSize = minSize;
            this.MinimumSizeP = minSizeP;
            this.NodePadding = nodePadding;
            this.NodePaddingP = nodePaddingP;
            this.ClusterPadding = clusterPadding;
            this.ClusterPaddingP = clusterPaddingP;
            this.ParentCluster = parentCluster;
            this.OpenBorderInfo = openBorderInfo;
            this.OpenBorderInfo.EnsureWeight();
            this.CloseBorderInfo = closeBorderInfo;
            this.CloseBorderInfo.EnsureWeight();
            this.OpenBorderInfoP = openBorderInfoP;
            this.OpenBorderInfoP.EnsureWeight();
            this.CloseBorderInfoP = closeBorderInfoP;
            this.CloseBorderInfoP.EnsureWeight();
            CreateBorderNodes();
        }

         void CreateBorderNodes()
        {
            if (!this.IsRootCluster)
            {
                string strNodeIdL = null, strNodeIdR = null;
#if VERIFY || VERBOSE
                strNodeIdL = "L" + this.UserDataString;
                strNodeIdR = "R" + this.UserDataString;
#endif // VERIFY || VERBOSE
                this.LeftBorderNode = new OverlapRemovalNode(this.Id + 1, strNodeIdL);
                this.RightBorderNode = new OverlapRemovalNode(this.Id + 2, strNodeIdR);
            }
        }

        // Enumerates only cluster children of this cluster.
        internal IEnumerable<OverlapRemovalCluster> Clusters { get { return this.clusterList; } }

        /// <summary>
        /// Generate a string representation of the Cluster.
        /// </summary>
        /// <returns>A string representation of the Cluster.</returns>
        public override string ToString()
        {
            // Currently this is just the same as the base Node; all zero if we haven't
            // yet called Solve(), else the values at the last time we called Solve().
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "Cluster '{0}': id {1} p {2:F5} s {3:F5} pP {4:F5} sP {5:F5}",
                                this.UserDataString, this.Id, this.Position, this.Size, this.PositionP, this.SizeP);
        }

        // newNode may be a cluster in which case we add it to the cluster list.  We never call this to
        // add the fake border nodes to nodeList; the caller never sees them.
        internal void AddNode(OverlapRemovalNode newNode)
        {
            this.nodeList.Add(newNode);
            var newCluster = newNode as OverlapRemovalCluster;
            if (null != newCluster)
            {
                this.clusterList.Add(newCluster);
            }
        }

        // Adds an open/close event pair for the node. paddingP is either cluster or node padding.
         void AddEvents(OverlapRemovalNode node, List<Event> events)
        {
            // Add/subtract only half the padding so they meet in the middle of the padding.
            events.Add(new Event(true, node, node.OpenP - (NodePaddingP / 2)));
            events.Add(new Event(false, node, node.CloseP + (NodePaddingP / 2)));
        }

        // This is internal rather than  so Test_OverlapRemoval can see it.
        internal static double CalcBorderWidth(double margin)
        {
            // Margin applies only to the inside edge.
            if (margin > 0.0)
            {
                return margin;
            }
            return DefaultBorderWidth;
        }

        // For iterative recursion (though we do not expect deeply nested clusters).
         class ClusterItem
        {
            internal readonly OverlapRemovalCluster Cluster;
            internal bool ChildrenHaveBeenPushed;
            internal ClusterItem(OverlapRemovalCluster cluster)
            {
                this.Cluster = cluster;
            }
        }

        internal delegate void ClusterDelegate(OverlapRemovalCluster cluster);

         static void ProcessClusterHierarchy(OverlapRemovalCluster root, ClusterDelegate worker)
        {
            var stack = new Stack<ClusterItem>();
            stack.Push(new ClusterItem(root));
            while (stack.Count > 0)
            {
                // Keep the cluster on the stack until we're done with its children.
                var item = stack.Peek();
                int prevStackCount = stack.Count;
                if (!item.ChildrenHaveBeenPushed)
                {
                    item.ChildrenHaveBeenPushed = true;
                    foreach (var childCluster in item.Cluster.Clusters)
                    {
                        stack.Push(new ClusterItem(childCluster));
                    }
                    if (stack.Count > prevStackCount)
                    {
                        continue;
                    }
                } // endif !node.ChildrenHaveBeenPushed

                // No children to push so pop and process this cluster.
                Debug.Assert(stack.Peek() == item, "stack.Peek() should be 'item'");
                stack.Pop();
                worker(item.Cluster);
            }
        }

        internal void Generate(Solver solver, OverlapRemovalParameters parameters, bool isHorizontal)
        {
            ProcessClusterHierarchy(this,
                    cluster => cluster.IsInSolver = cluster.GenerateWorker(solver, parameters, isHorizontal));
            ProcessClusterHierarchy(this, cluster => cluster.SqueezeNonFixedBorderPositions());
        }

        /// <summary>
        /// If a border is not fixed swap its position with the opposite border to ensure
        /// cluster is tight to its contents.
        /// </summary>
         void SqueezeNonFixedBorderPositions()
        {
            // Here's an example of why this is necessary:  If we base the initial border position
            // on a child cluster's border and that child cluster is initially sparse, then its
            // size can shrink considerably; leaving the border of this cluster further out than
            // it needs to be (since we expand space between nodes but don't necessarily shrink it).
            // Squeezing causes constraints to be *minimally* enforced.  Note - this may introduce
            // a defer-to-vertical issue, so we special-case that section to ignore border nodes).
            if (this.IsEmpty || this.IsRootCluster || !this.IsInSolver)
            {
                return;
            }

            double leftBorderDesiredPos = this.LeftBorderNode.Position;
            double rightBorderDesiredPos = this.RightBorderNode.Position;
            if (!this.OpenBorderInfo.IsFixedPosition)
            {
                this.LeftBorderNode.Variable.DesiredPos = rightBorderDesiredPos;
            }
            if (!this.CloseBorderInfo.IsFixedPosition)
            {
                this.RightBorderNode.Variable.DesiredPos = leftBorderDesiredPos;
            }
        }

        // Returns false if the cluster is empty; this handles nested clusters of empty clusters.
        // TODOunit: several of the test files cover this but add a specific test for it.
         bool GenerateWorker(Solver solver, OverlapRemovalParameters parameters,
                             bool isHorizontal)
        {
            // @@DCR "Precalculate Cluster Sizes": if we are solving per-cluster to calculate best sizes before
            // generating constraints, then solver would be passed in as null and we'd create one here.

            // Variables to calculate our boundaries.  Top and Bottom refer to the perpendicular direction;
            // for vertical, read Top <-> Left and Bottom <-> Right.
            var boundaryRect = new Rectangle
                {
                    //Left =
                    //    this.OpenBorderInfo.IsFixedPosition && this.TranslateChildren
                    //        ? this.OpenBorderInfo.FixedPosition
                    //        : double.MaxValue,
                    //Right =
                    //    this.CloseBorderInfo.IsFixedPosition && this.TranslateChildren
                    //        ? this.CloseBorderInfo.FixedPosition
                    //        : double.MinValue,
                    //Bottom =
                    //    this.OpenBorderInfoP.IsFixedPosition && this.TranslateChildren
                    //        ? this.OpenBorderInfoP.FixedPosition
                    //        : double.MaxValue,
                    //Top =
                    //    this.CloseBorderInfoP.IsFixedPosition && this.TranslateChildren
                    //        ? this.CloseBorderInfoP.FixedPosition
                    //        : double.MinValue
                    Left = double.MaxValue,
                    Right = double.MinValue,
                    Bottom = double.MaxValue,
                    Top = double.MinValue
                };

            if (IsEmpty)
            {
                // Nothing to generate.
                return false;
            }

            // The list of open/close events, which will be sorted on the perpendicular coordinate of the event
            // (e.g. for horizontal constraint generation, order on vertical position).
            var events = this.CreateEvents(solver, ref boundaryRect);

            // If we added no events, we're either Fixed (so continue) or empty (so return).
            if (0 == events.Count && !TranslateChildren)
            {
                return false;
            }

            // Top/Bottom are considered the secondary (Perpendicular) axis here.
            double leftBorderWidth = DefaultBorderWidth;
            double rightBorderWidth = DefaultBorderWidth;
            if (!this.IsRootCluster)
            {
                CalculateBorderWidths(solver, events, boundaryRect, out leftBorderWidth, out rightBorderWidth);
#if VERBOSE
                System.Diagnostics.Debug.WriteLine(" {0} After CalculateBorderWidths: p {1:F5} s {2:F5} pP {3:F5} sP {4:F5}"
                        , this.Name, this.Size, this.Position, this.Size, this.SizeP);
#endif
            }

            GenerateFromEvents(solver, parameters, events, isHorizontal);

            if (!this.IsRootCluster)
            {
                // Non-fixed borders are moved later by SqueezeNonFixedBorderPositions().
                this.AdjustFixedBorderPositions(solver, leftBorderWidth, rightBorderWidth, isHorizontal);
            }
            return true;
        }

         List<Event> CreateEvents(Solver solver, ref Rectangle boundaryRect)
        {
            var events = new List<Event>();
            int cNodes = this.nodeList.Count; // cache for perf
            double leftBorderWidth = CalcBorderWidth(this.OpenBorderInfo.InnerMargin);
            double rightBorderWidth = CalcBorderWidth(this.CloseBorderInfo.InnerMargin);
            double openBorderWidth = CalcBorderWidth(this.OpenBorderInfoP.InnerMargin);
            double closeBorderWidth = CalcBorderWidth(this.CloseBorderInfoP.InnerMargin);
            for (int nodeIndex = 0; nodeIndex < cNodes; ++nodeIndex)
            {
                OverlapRemovalNode node = this.nodeList[nodeIndex];
                var cluster = node as OverlapRemovalCluster;
                if (null != cluster)
                {
                    // Child Clusters have already "recursively" been processed before the current cluster,
                    // so we just need to check to see if it had any events.  If so, then it has created its
                    // two fake nodes (and their variables) along the primary axis, but these are only put
                    // into the event list at the nested level; at this level, we put Node underlying the 
                    // entire Cluster span (in both directions) into the event list.

                    // If a child cluster is empty, it will have zero size and no way to set its position.
                    // That includes clusters containing nothing but empty clusters.  We skip those here.
                    if (!cluster.IsInSolver)
                    {
                        continue;
                    }
                }
                else
                {
                    // Not a cluster; just have it add its variable to the solver.
                    node.CreateVariable(solver);
                }

                // Now add the Node to the ScanLine event list.  Use paddingP because the scan line moves
                // perpendicularly to the direction we're generating the constraints in.
                AddEvents(node, events);

                // Update our boundaries if this node goes past any of them.
                if (!this.IsRootCluster)
                {
                    double pad = node.Size / 2 + ClusterPadding;
                    double padP = node.SizeP / 2 + ClusterPaddingP;
                    double newLeft = node.Position - pad - leftBorderWidth;
                    double newRight = node.Position + pad + rightBorderWidth;
                    double newBottom = node.PositionP - padP - openBorderWidth;
                    double newTop = node.PositionP + padP + closeBorderWidth;

                    boundaryRect.Left = Math.Min(boundaryRect.Left, newLeft);
                    boundaryRect.Right = Math.Max(boundaryRect.Right, newRight);
                    boundaryRect.Bottom = Math.Min(boundaryRect.Bottom, newBottom);
                    boundaryRect.Top = Math.Max(boundaryRect.Top, newTop);

#if VERBOSE
                    System.Diagnostics.Debug.WriteLine(" {0} BoundaryRect after AddEvents: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}"
                            , this.Name, boundaryRect.Left, boundaryRect.Right, boundaryRect.Top, boundaryRect.Bottom);
#endif
                }
            }
            if (!this.IsRootCluster)
            {
                // Force the cluster borders to the full minimum sizes if any were specified.
                // Without the full cluster boundaries being available at constraint generation time, Tuvalu was
                // getting unresolved overlaps when dragging an external node over the corner of a cluster boundary.
                double padMinSize = this.MinimumSize - boundaryRect.Width;
                if (padMinSize > 0)
                {
                    boundaryRect.PadWidth(padMinSize / 2);
                }
                double padMinSizeP = this.MinimumSizeP - boundaryRect.Height;
                if (padMinSizeP > 0)
                {
                    boundaryRect.PadHeight(padMinSizeP / 2);
                }

#if VERBOSE
                System.Diagnostics.Debug.WriteLine(" {0} BoundaryRect after CreateEvents: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}"
                        , this.Name, boundaryRect.Left, boundaryRect.Right, boundaryRect.Top, boundaryRect.Bottom);
#endif

            }

            return events;
        }

         void CalculateBorderWidths(Solver solver, List<Event> events, Rectangle boundaryRect,
                                            out double leftBorderWidth, out double rightBorderWidth)
        {
            // Cluster-level padding (the space around the borders) complicates this.  Margin
            // is added only at the inside edge of the cluster; for example, as space for a
            // title of the cluster to be printed.  We just use the margin as the boundary node
            // sizes.  Margin is separate from padding; padding is always added.
            leftBorderWidth = CalcBorderWidth(this.OpenBorderInfo.InnerMargin);
            rightBorderWidth = CalcBorderWidth(this.CloseBorderInfo.InnerMargin);

            // @@DCR "Precalculate Cluster Sizes": at this point we could solve them to get the "real" cluster
            // size (as above, this may be requested by solver being null on input so we create our own above).

            // Now calculate our position (as midpoints) and size.  This will be used in the parentCluster's
            // Generate() operation.  We want to get to the outside border of the border, so subtract the
            // border width.  We've added pre-border padding above.  Note: This is done before checking
            // for fixed positions, because we want the constraint generation to see them in the correct
            // relative positions - border midpoints are always outside the outermost node midpoints, so that
            // constraints will be generated in the correct direction (it would be bad if, for example, a Left
            // border was the rhs of a constraint with a node inside the cluster; it should always be an lhs
            // to any node in the cluster, and having it as an rhs will probably generate a cycle).  We adjust
            // to fixed positions below after GenerateFromEvents.
            this.Size = boundaryRect.Width;
            this.Position = boundaryRect.Center.X;

            // The final perpendicular positions may be modified below, after GenerateFromEvents; they
            // will be used by a parent cluster's Generate after we return if this is a recursive call.
            // We don't do it here because we are doing the variables internal to this cluster, based
            // upon their current positions, so this would get confused if we moved the P coordinates here.
            this.SizeP = boundaryRect.Height;
            this.PositionP = boundaryRect.Center.Y;

            // Now create the two "fake nodes" for the borders and add them to the event list line and solver.
            // This constraint will never be deferred, since there is no overlap in the secondary axis but is
            // in the primary axis.  In the perpendicular direction, we want them to be the size of the
            // outer borders of the outer nodes, regardless of whether the perpendicular borders are
            // fixed-position; this ensures that the scan line will correctly see their open and close.
            // Left/Open...
            this.LeftBorderNode.Position = boundaryRect.Left + (leftBorderWidth / 2);
            this.LeftBorderNode.Size = leftBorderWidth;
            this.LeftBorderNode.Weight = this.OpenBorderInfo.Weight;
            this.LeftBorderNode.PositionP = this.PositionP;
            this.LeftBorderNode.SizeP = this.SizeP;
            this.LeftBorderNode.CreateVariable(solver);
            AddEvents(this.LeftBorderNode, events);

            // Note:  The Left/Right, Open/Close terminology here is inconsistent with GenerateFromEvents
            //  since here Open is in the primary axis and in GenerateFromEvents it's in the secondary/P axis.

            // Right/Close...
            this.RightBorderNode.Position = boundaryRect.Right - (rightBorderWidth / 2);
            this.RightBorderNode.Size = rightBorderWidth;
            this.RightBorderNode.Weight = this.CloseBorderInfo.Weight;
            this.RightBorderNode.PositionP = this.PositionP;
            this.RightBorderNode.SizeP = this.SizeP;
            this.RightBorderNode.CreateVariable(solver);
            AddEvents(this.RightBorderNode, events);
        }

         void AdjustFixedBorderPositions(Solver solver, double leftBorderWidth, double rightBorderWidth, bool isHorizontal)
        {
            // Note:  Open == Left, Close == Right.
            if (this.OpenBorderInfo.IsFixedPosition && this.CloseBorderInfo.IsFixedPosition)
            {
                // Both are fixed, so just move them to their specified positions.  For FixedPosition
                // the API is that it's the outer border edge, so add or subtract half the (left|right)BorderWidth
                // to set the position to the midpoint.  Since both borders are fixed, this provides a
                // limit to the size of the overall node.
                this.LeftBorderNode.UpdateDesiredPosition(
                    this.OpenBorderInfo.FixedPosition + (leftBorderWidth / 2));
                this.RightBorderNode.UpdateDesiredPosition(
                    this.CloseBorderInfo.FixedPosition - (rightBorderWidth / 2));
                this.Size = this.CloseBorderInfo.FixedPosition - this.OpenBorderInfo.FixedPosition;
                this.Position = this.OpenBorderInfo.FixedPosition + (this.Size / 2);
            }
            else if (this.OpenBorderInfo.IsFixedPosition || this.CloseBorderInfo.IsFixedPosition)
            {
                // One border is fixed and the other isn't.  We'll keep the same cluster size,
                // move the fixed border to its specified position, adjust our midpoint to reflect that,
                // and then move the unfixed border to be immediately adjacent to the fixed border; the
                // solver will cause it to be moved to the minimal position satisfying the constraints.
                if (this.OpenBorderInfo.IsFixedPosition)
                {
                    // FixedPosition is the outer border edge so add BorderWidth/2 to set it to the Left midpoint.
                    this.LeftBorderNode.UpdateDesiredPosition(
                        this.OpenBorderInfo.FixedPosition + (leftBorderWidth / 2));
                    this.Position = this.OpenBorderInfo.FixedPosition + (this.Size / 2);
                }
                else /* this.CloseBorderInfo.IsFixedPosition */
                {
                    // FixedPosition is the outer border edge so subtract BorderWidth/2 to set it to the Right midpoint.
                    this.RightBorderNode.UpdateDesiredPosition(
                        this.CloseBorderInfo.FixedPosition - (rightBorderWidth / 2));
                    this.Position = this.CloseBorderInfo.FixedPosition - (this.Size / 2);
                }
            }

            // If we have a minimum size, generate constraints for it.  Although this may change the size
            // considerably, so may the movement of variables in the cluster, so we need no precalculation
            // of sizes or positions; but after the Horizontal pass, the caller must pass in the resultant
            // positions in the Horizontal (perpendicular) BorderInfos parameter to Vertical generation;
            // otherwise, because the Horizontal cluster span may be larger than is calculated simply from
            // variable positions, some variables may not have appropriate constraints generated.
            if (this.MinimumSize > 0.0)
            {
                Constraint cst = solver.AddConstraint(
                    this.LeftBorderNode.Variable, this.RightBorderNode.Variable, this.MinimumSize - leftBorderWidth/2 - rightBorderWidth/2);
                Debug.Assert(null != cst, "Minimum Cluster size: unexpected null cst");
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine(" {0} MinClusterSizeCst {1} -> {2} g {3:F5}", isHorizontal ? "H" : "V"
                            , cst.Left.Name, cst.Right.Name, cst.Gap);
#endif
                // VERBOSE
            }

            // Now recalculate our perpendicular PositionP/SizeP if either perpendicular border is fixed,
            // since we know we're going to move things there.  We don't actually create variables for the
            // perpendicular axis on this pass, but we set the primary axis border nodes' perpendicular size
            // and position, thus creating "virtual" perpendicular borders used by the parent cluster's
            // Generate() and for its events in its GenerateFromEvents().  This must be done on both H and V
            // passes, because multiple heavyweight Fixed borders can push each other around on the horizontal
            // pass and leave excessive space between the fixed border and the outer nodes.  In that case the
            // Vertical pass can't get the true X border positions by evaluating our nodes' X positions; the
            // caller must pass this updated position in (the same thing it must do for nodes' X coordinates).
            if (this.OpenBorderInfoP.IsFixedPosition || this.CloseBorderInfoP.IsFixedPosition)
            {
                // If both are fixed, we'll set to those positions and recalculate size.
                // Remember that FixedPosition API is the outer border edge so we don't need to adjust for border width.
                if (this.OpenBorderInfoP.IsFixedPosition && this.CloseBorderInfoP.IsFixedPosition)
                {
                    this.SizeP = this.CloseBorderInfoP.FixedPosition - this.OpenBorderInfoP.FixedPosition;
                    this.PositionP = this.OpenBorderInfoP.FixedPosition + (this.SizeP / 2);
                    if (this.SizeP < 0)
                    {
                        // Open border is to the right of close border; they'll move later, but we have to
                        // make the size non-negative.  TODOunit: create a specific test for this (fixed LRTB)
                        this.SizeP = -this.SizeP;
                    }
                }
                else
                {
                    // Only one is fixed, so we'll adjust in the appropriate direction as needed.
                    // - If we're on the horizontal pass we'll preserve the above calculation of this.SizeP
                    //   and only shift things around to preserve the relative vertical starting positions;
                    //   running the Solver will change these positions.
                    // - If we're on the vertical pass, we know the horizontal nodes are in their final positions,
                    //   so we need to accommodate the case described above, where the Solver opened up space
                    //   between the fixed border and the outermost nodes (it will never *reduce* this distance
                    //   of course).  This means we adjust both border position and our overall node size.
                    double curTopOuterBorder = this.PositionP - (this.SizeP / 2);
                    double curBottomOuterBorder = this.PositionP + (this.SizeP / 2);
                    if (this.OpenBorderInfoP.IsFixedPosition)
                    {
                        if (isHorizontal)
                        {
                            // Don't change SizeP.
                            this.PositionP += this.OpenBorderInfoP.FixedPosition - curTopOuterBorder;
                        }
                        else
                        {
                            this.SizeP = curBottomOuterBorder - this.OpenBorderInfoP.FixedPosition;
                            this.PositionP = this.OpenBorderInfoP.FixedPosition + (this.SizeP / 2);
                        }
                    }
                    else
                    {
                        if (isHorizontal)
                        {
                            // Don't change SizeP.
                            this.PositionP += this.CloseBorderInfoP.FixedPosition - curBottomOuterBorder;
                        }
                        else
                        {
                            this.SizeP = this.CloseBorderInfoP.FixedPosition - curTopOuterBorder;
                            this.PositionP = curTopOuterBorder + (this.SizeP / 2);
                        }
                    }
                } // endifelse both borders fixed or only one border is

                // Now update our fake border nodes' PositionP/SizeP to be consistent.
                this.LeftBorderNode.PositionP = this.PositionP;
                this.LeftBorderNode.SizeP = this.SizeP;
                this.RightBorderNode.PositionP = this.PositionP;
                this.RightBorderNode.SizeP = this.SizeP;
            }
        }

        // end Generate()

        // Get the Node to use in generating constraints:
        // - If the Node is not a Cluster, then use the Node.
        // - Else if it is being operated on as the left neighbour, use its right border as the
        //   variable FROM which we create the constraint.
        // - Else it is being operated on as the right neighbour, so use its left border as the
        //   variable TO which we create the constraint.
        internal static OverlapRemovalNode GetLeftConstraintNode(OverlapRemovalNode node)
        {
            var cluster = node as OverlapRemovalCluster;
            return (null != cluster) ? cluster.RightBorderNode : node;
        }
        internal static OverlapRemovalNode GetRightConstraintNode(OverlapRemovalNode node)
        {
            var cluster = node as OverlapRemovalCluster;
            return (null != cluster) ? cluster.LeftBorderNode : node;
        }

         void GenerateFromEvents(Solver solver, OverlapRemovalParameters parameters,
                        List<Event> events, bool isHorizontal)
        {
            // First, sort the events on the perpendicular coordinate of the event
            // (e.g. for horizontal constraint generation, order on vertical position).
            events.Sort();

#if VERBOSE
            System.Diagnostics.Debug.WriteLine("Events:");
            foreach (Event evt in events)
            {
                System.Diagnostics.Debug.WriteLine("    {0}", evt);
            }
#endif // VERBOSE

            var scanLine = new ScanLine();
            foreach (Event evt in events)
            {
                OverlapRemovalNode currentNode = evt.Node;
                if (evt.IsForOpen)
                {
                    // Insert the current node into the scan line.
                    scanLine.Insert(currentNode);
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine("ScanAdd: {0}", currentNode);
#endif // VERBOSE

                    // Find the nodes that are currently open to either side of it and are either overlapping
                    // nodes or the first non-overlapping node in that direction.
                    currentNode.LeftNeighbors = GetLeftNeighbours(parameters, scanLine, currentNode, isHorizontal);
                    currentNode.RightNeighbors = GetRightNeighbours(parameters, scanLine, currentNode, isHorizontal);

                    // Use counts for indexing for performance (rather than foreach, and hoist the count-control
                    // variable out of the loop so .Count isn't checked on each iteration, since we know it's
                    // not going to be changed).
                    int numLeftNeighbors = currentNode.LeftNeighbors.Count;
                    int numRightNeighbors = currentNode.RightNeighbors.Count;

                    // If there is currently a non-overlap constraint between any two nodes across the
                    // two neighbour lists we've just created, we can remove them because they will be
                    // transitively enforced by the constraints we'll create for the current node.
                    // I.e., we can remove the specification for the constraint
                    //      leftNeighborNode + gap + padding <= rightNeighborNode
                    // because it is implied by the constraints we'll create for
                    //      leftNeighborNode + gap + padding <= node
                    //      node + gap + padding <= rightNeighborNode
                    // We must also add the current node as a neighbour in the appropriate direction.
                    // @@PERF: List<T>.Remove is a sequential search so averages 1/2 the size of the
                    // lists. We currently don't expect the neighbour lists to be very large or Remove
                    // to be a frequent operation, and using HashSets would incur the GetEnumerator overhead
                    // on the outer and inner loops; but .Remove creates an inner-inner loop so do some
                    // timing runs to compare performance.
                    // @@PERF:  handles the case where we are node c and have added node b as a lnbour
                    // and node d as rnbour, where those nodes are already nbours.  But it does not handle
                    // the case where we add node b and node a as lnbours, and node b already has node a
                    // as an lnbour.  To do this I think we'd just want to skip adding the node-a lnbour,
                    // but that forms a new inner loop (iterating all lnbours before adding a new one)
                    // unless we develop different storage for nbours.
                    for (int ii = 0; ii < numLeftNeighbors; ++ii)
                    {
                        OverlapRemovalNode leftNeighborNode = currentNode.LeftNeighbors[ii];
                        for (int jj = 0; jj < numRightNeighbors; ++jj)
                        {     // TODOunit: test this
                            OverlapRemovalNode nodeToRemove = currentNode.RightNeighbors[jj];
                            if (leftNeighborNode.RightNeighbors.Remove(nodeToRemove))
                            {
#if VERBOSE
                                System.Diagnostics.Debug.WriteLine(" {0} RnbourRem {1} --> {2}", isHorizontal ? "H" : "V", leftNeighborNode, nodeToRemove);
#endif // VERBOSE
                            }
                        }
                        leftNeighborNode.RightNeighbors.Add(currentNode);
                    }
                    for (int ii = 0; ii < numRightNeighbors; ++ii)
                    {         // TODOunit: test this
                        OverlapRemovalNode rightNeighborNode = currentNode.RightNeighbors[ii];
                        for (int jj = 0; jj < numLeftNeighbors; ++jj)
                        {
                            OverlapRemovalNode nodeToRemove = currentNode.LeftNeighbors[jj];
                            if (rightNeighborNode.LeftNeighbors.Remove(nodeToRemove))
                            {
#if VERBOSE
                                System.Diagnostics.Debug.WriteLine(" {0} LnbourRem {1} --> {2}", isHorizontal ? "H" : "V", nodeToRemove, rightNeighborNode);
#endif // VERBOSE
                            }
                        }
                        rightNeighborNode.LeftNeighbors.Add(currentNode);
                    }
                } // endif evt.IsForOpen
                else
                {
                    // This is a close event, so generate the constraints and remove the closing node
                    // from its neighbours lists.  If we're closing we should have left neighbours so
                    // this is null then we've likely got some sort of internal calculation error.
                    if (null == currentNode.LeftNeighbors)
                    {
                        Debug.Assert(null != currentNode.LeftNeighbors, "LeftNeighbors should not be null for a Close event");
                        continue;
                    }

                    // currentNode is the current node; if it's a cluster, translate it to the node that
                    // should be involved in the constraint (if it's the left neighbour then use its
                    // right border as the constraint variable, and vice-versa).
                    OverlapRemovalNode currentLeftNode = GetLeftConstraintNode(currentNode);
                    OverlapRemovalNode currentRightNode = GetRightConstraintNode(currentNode);

                    // LeftNeighbors must end before the current node...
                    int cLeftNeighbours = currentNode.LeftNeighbors.Count;
                    for (int ii = 0; ii < cLeftNeighbours; ++ii)
                    {
                        // Keep track of the original Node; it may be the base of a Cluster, in which
                        // case it will have the active neighbours list, not leftNeighborNode (which will
                        // be the left border "fake Node").
                        OverlapRemovalNode origLeftNeighborNode = currentNode.LeftNeighbors[ii];
                        origLeftNeighborNode.RightNeighbors.Remove(currentNode);
                        OverlapRemovalNode leftNeighborNode = GetLeftConstraintNode(origLeftNeighborNode);
                        Debug.Assert(leftNeighborNode.OpenP == origLeftNeighborNode.OpenP, "leftNeighborNode.OpenP must == origLeftNeighborNode.OpenP");

                        // This assert verifies we match the Solver.ViolationTolerance check in AddNeighbor.
                        // We are closing the node here so use an alternative to OverlapP for additional
                        // consistency verification.  Allow a little rounding error.
                        Debug.Assert(isHorizontal
                                || ((currentNode.CloseP + NodePaddingP - leftNeighborNode.OpenP) > (parameters.SolverParameters.GapTolerance - 1e-6)),
                                "LeftNeighbors: unexpected close/open overlap");

                        double p = leftNeighborNode == LeftBorderNode || currentRightNode == RightBorderNode ? ClusterPadding : NodePadding;
                        double separation = ((leftNeighborNode.Size + currentRightNode.Size) / 2) + p;
                        if (TranslateChildren)
                        {
                            separation = Math.Max(separation, currentRightNode.Position - leftNeighborNode.Position);
                        }
                        Constraint cst = solver.AddConstraint(leftNeighborNode.Variable, currentRightNode.Variable, separation);
                        Debug.Assert(null != cst, "LeftNeighbors: unexpected null cst");
#if VERBOSE
                        System.Diagnostics.Debug.WriteLine(" {0} LnbourCst {1} -> {2} g {3:F5}", isHorizontal ? "H" : "V"
                                , cst.Left.Name, cst.Right.Name, cst.Gap);
#endif // VERBOSE
                    }

                    // ... and RightNeighbors must start after the current node.
                    int cRightNeighbours = currentNode.RightNeighbors.Count;
                    for (int ii = 0; ii < cRightNeighbours; ++ii)
                    {
                        // Keep original node, which may be a cluster; see comments in LeftNeighbors above.
                        OverlapRemovalNode origRightNeighborNode = currentNode.RightNeighbors[ii];
                        origRightNeighborNode.LeftNeighbors.Remove(currentNode);
                        OverlapRemovalNode rightNeighborNode = GetRightConstraintNode(origRightNeighborNode);

                        // This assert verifies we match the Solver.ViolationTolerance check in AddNeighbor.
                        // Allow a little rounding error.
                        Debug.Assert(isHorizontal
                                || ((currentNode.CloseP + NodePaddingP - rightNeighborNode.OpenP) > (parameters.SolverParameters.GapTolerance - 1e-6)),
                                "RightNeighbors: unexpected close/open overlap");

                        double p = currentLeftNode == LeftBorderNode || rightNeighborNode == RightBorderNode ? ClusterPadding : NodePadding;
                        double separation = ((currentLeftNode.Size + rightNeighborNode.Size) / 2) + p;
                        if (TranslateChildren)
                        {
                            separation = Math.Max(separation, rightNeighborNode.Position - currentLeftNode.Position);
                        }
                        Constraint cst = solver.AddConstraint(currentLeftNode.Variable, rightNeighborNode.Variable, separation);
                        Debug.Assert(null != cst, "RightNeighbors: unexpected null cst");
#if VERBOSE
                        System.Diagnostics.Debug.WriteLine(" {0} RnbourCst {1} -> {2} g {3:F5}", isHorizontal ? "H" : "V"
                                , cst.Left.Name, cst.Right.Name, cst.Gap);
#endif // VERBOSE
                    }

                    // Note:  although currentNode is closed, there may still be open nodes in its
                    // Neighbour lists; these will subsequently be processed (and removed from
                    // currentNode.*Neighbour) when those Neighbors are closed.
                    scanLine.Remove(currentNode);
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine("ScanRem: {0}", currentNode);
#endif // VERBOSE
                } // endelse !evt.IsForOpen

                // @@PERF:  Set Node.Left/RightNeighbors null to let the GC know we're not using them
                // anymore, unless we can reasonably assume a short lifetime for the ConstraintGenerator.
            } // endforeach Event
        }

         List<OverlapRemovalNode> GetLeftNeighbours(OverlapRemovalParameters parameters, ScanLine scanLine,
                                    OverlapRemovalNode currentNode, bool isHorizontal)
        {
            var lstNeighbours = new List<OverlapRemovalNode>();
            OverlapRemovalNode nextNode = scanLine.NextLeft(currentNode);
            for (; null != nextNode; nextNode = scanLine.NextLeft(nextNode))
            {
                // AddNeighbor returns false if we are done adding them.
                if (!AddNeighbour(parameters, currentNode, nextNode, lstNeighbours, true /* isLeftNeighbor */
                                , isHorizontal))
                {
                    if (!nextNode.DeferredLeftNeighborToV)
                    {
                        break;
                    }
                }
            } // endfor NextLeft
            return lstNeighbours;
        } // end GetLeftNeighbours

         List<OverlapRemovalNode> GetRightNeighbours(OverlapRemovalParameters parameters, ScanLine scanLine,
                                    OverlapRemovalNode currentNode, bool isHorizontal)
        {
            var lstNeighbours = new List<OverlapRemovalNode>();
            OverlapRemovalNode nextNode = scanLine.NextRight(currentNode);
            for (; null != nextNode; nextNode = scanLine.NextRight(nextNode))
            {
                // AddNeighbor returns false if we are done adding them.
                if (!AddNeighbour(parameters, currentNode, nextNode, lstNeighbours, false /* isLeftNeighbor */
                                , isHorizontal))
                {
                    if (!nextNode.DeferredRightNeighborToV)
                    {
                        break;
                    }
                }
            } // endfor NextLeft
            return lstNeighbours;
        } // end GetRightNeighbours

         bool AddNeighbour(OverlapRemovalParameters parameters, OverlapRemovalNode currentNode, OverlapRemovalNode nextNode,
                        List<OverlapRemovalNode> neighbors, bool isLeftNeighbor, bool isHorizontal)
        {
            // Sanity check to be sure that the borders are past all other nodes.
            Debug.Assert(currentNode != (isLeftNeighbor ? this.LeftBorderNode : this.RightBorderNode), "currentNode must != BorderNode");

            double overlap = Overlap(currentNode, nextNode, NodePadding);
            if (overlap <= 0)
            {
                // This is the first node encountered on this neighbour-traversal that did not
                // overlap within the required padding. Add it to the list and we're done with
                // this traversal, unless this is a vertical pass and it is not an overlap on
                // the horizontal axis; in that case, pretend we never saw it and return true
                // so the next non-overlapping node will be found.  (See below for more information
                // on why this is necessary).
                if (!isHorizontal && (OverlapP(currentNode, nextNode, NodePaddingP) <= parameters.SolverParameters.GapTolerance))
                {
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine(" V {0}nbourHTolSkipNO: {1}", isLeftNeighbor ? "L" : "R", nextNode);
#endif // VERBOSE
                    return true;
                }
                neighbors.Add(nextNode);
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("{0}nbourAddNO: {1}", isLeftNeighbor ? "L" : "R", nextNode);
#endif // VERBOSE
                return false;
            }

            if (isHorizontal)
            {
                if (parameters.AllowDeferToVertical)
                {
                    // We are doing horizontal constraints so examine the vertical overlap and see which
                    // is the smallest (normalized by total node size in that orientation) such that the
                    // least amount of movement required.  this.padding is currently the same in both
                    // directions; if this changes, we'll have to add different padding values here for
                    // each direction.  @@DCR: consider adding weights to the defer-to-vertical calculation;
                    // this would allow two nodes to pop up/down if they're being squeezed, rather than
                    // force apart the borders (which happens regardless of their weight).
                    double overlapP = OverlapP(currentNode, nextNode, NodePaddingP);
                    bool isOverlapping =
                         parameters.ConsiderProportionalOverlap
                        ? overlap / (currentNode.Size + nextNode.Size) > overlapP / (currentNode.SizeP + nextNode.SizeP)
                        : overlap > overlapP;
                    if (isOverlapping)
                    {
                        // Don't skip if either of these is a border node.
                        if ((currentNode != this.LeftBorderNode)
                                && (currentNode != this.RightBorderNode)
                                && (nextNode != this.LeftBorderNode)
                                && (nextNode != this.RightBorderNode))
                        {
                            // Moving in the horizontal direction requires more movement than in the vertical
                            // direction to remove the overlap, so skip this node on horizontal constraint
                            // generation and we'll pick it up on vertical constraint generation.  Return true
                            // to keep looking for more overlapping nodes.
                            // Note: it is still possible that we'll pick up a constraint in both directions,
                            // due to either or both of this.padding and the "create a constraint to the first
                            // non-overlapping node" logic.  This is expected and the latter helps retain stability.
#if VERBOSE
                            System.Diagnostics.Debug.WriteLine("{0}nbourDeferToV: {1}", isLeftNeighbor ? "L" : "R", nextNode);
#endif // VERBOSE
                            // We need to track whether we skipped these so that we don't have a broken transition chain.
                            // See Test_OverlapRemoval.cs, Test_DeferToV_Causing_Missing_Cst() for more information.
                            if (isLeftNeighbor)
                            {
                                currentNode.DeferredLeftNeighborToV = true;
                                nextNode.DeferredRightNeighborToV = true;
                            }
                            else
                            {
                                currentNode.DeferredRightNeighborToV = true;
                                nextNode.DeferredLeftNeighborToV = true;
                            }
                            return true;
                        }
                    } // endif Overlap is greater than OverlapP
                } // endif AllowDeferToVertical
            }
            else
            {
                // We're on the vertical pass so make sure we match up with the Solver's tolerance in the
                // scanline direction, because it is possible that there was a horizontal constraint between
                // these nodes that was within the Solver's tolerance and thus was not enforced.  In that
                // case, we could spuriously add a vertical constraint here that would result in undesired
                // and possibly huge vertical movement.  There is a corresponding Assert during constraint
                // generation when the node is Closed. We have to do this here rather than at runtime because
                // doing it then may skip a Neighbour that replaced other Neighbors by transitivity.
                if (OverlapP(currentNode, nextNode, NodePaddingP) <= parameters.SolverParameters.GapTolerance)
                {
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine(" V {0}nbourHTolSkipO: {1}", isLeftNeighbor ? "L" : "R", nextNode);
#endif // VERBOSE
                    return true;
                }
            }

            // Add this overlapping neighbour and return true to keep looking for more overlapping neighbours.
            neighbors.Add(nextNode);
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("{0}nbourAddO: {1}", isLeftNeighbor ? "L" : "R", nextNode);
#endif // VERBOSE
            return true;
        }

        // Overrides Node.UpdateFromVariable.
        internal override void UpdateFromVariable()
        {
            ProcessClusterHierarchy(this, cluster => cluster.UpdateFromVariableWorker());
        }

         void UpdateFromVariableWorker()
        {
            // The root cluster has no "position" and thus no border variables.
            if (!this.IsRootCluster)
            {
                // If empty, we had nothing to do.  We're also empty if we had child clusters
                // that were empty; in that case we check that we don't have our Variables created.
                if (IsEmpty || (null == this.LeftBorderNode.Variable))
                {
                    return;
                }

                // We put the fake border Nodes right up against the outer true Nodes (plus padding) initially,
                // and then moved them to the midpoint (subsequently, the caller may have updated their position
                // to the barycenter of the cluster, e.g. FastIncrementalLayout).  Because the algorithm
                // guarantees a minimal solution, the borders are in their optimal position now (including
                // padding from their outer nodes).
                this.LeftBorderNode.UpdateFromVariable();
                this.RightBorderNode.UpdateFromVariable();

                // Now update our position and size from those nodes, accounting for possibly different widths
                // of the border variables (due to cluster margin specifications). Because the margins apply at
                // the inner edge (only), we must take our size as the difference between the outer borders of
                // the border variables.  This leaves our single Node as having the size necessary to accommodate
                // the internal padding when external nodes/clusters have their constraints to it formed.
                double clusterLeft = this.LeftBorderNode.Position - (this.LeftBorderNode.Size / 2);
                double clusterRight = this.RightBorderNode.Position + (this.RightBorderNode.Size / 2);
                this.Size = clusterRight - clusterLeft;
                this.Position = clusterLeft + (this.Size / 2);
            }

            // Update our child nodes.
            int cNodes = this.nodeList.Count;          // cache for perf
            for (int nodeIndex = 0; nodeIndex < cNodes; ++nodeIndex)
            {
                OverlapRemovalNode node = this.nodeList[nodeIndex];

                // Child Clusters have already "recursively" been processed before the current cluster.
                if (!(node is OverlapRemovalCluster))
                {
                    node.UpdateFromVariable();
                }
            }
        }
    }
}