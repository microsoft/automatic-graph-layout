// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClusterDef.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    // Class for testing cluster definitions.  We can have two dimensions, X and Y, for
    // OverlapRemoval, but only one for ProjectionSolver, so we use X for that.
    internal class ClusterDef : IPositionInfo
    {
        private readonly List<VariableDef> variableDefs = new List<VariableDef>();
        private readonly List<ClusterDef> clusterDefs = new List<ClusterDef>();
        internal ClusterDef ParentClusterDef { get; set; }
        internal OverlapRemovalCluster Cluster { get; set; }
        internal int ClusterId { get; private set; }
        internal bool IsNewHierarchy { get; set; }
        internal bool IsEmpty
        {
            get
            {
                if (0 != this.variableDefs.Count)
                {
                    return false;
                }
                if (0 == this.clusterDefs.Count)
                {
                    return true;
                }
                return this.clusterDefs.All(clusDef => clusDef.IsEmpty);
            }
        }

        // For globals persisting for the life of a single test (so ToFailure calls this for each iteration).
        private static int nextClusterId = 1;     // Reserve 0 for the DefaultClusterHierarchy
        private static int numClustersWithFixedBordersH;
        private static int numClustersWithFixedBordersV;
        internal static void Reset()
        {
            nextClusterId = 1;
            numClustersWithFixedBordersH = 0;
            numClustersWithFixedBordersV = 0;
        }

        // Minimum size.
        internal double MinimumSizeX { get; set; }
        internal double MinimumSizeY { get; set; }
        private BorderInfo PostXLeftBorderInfo { get; set; }
        private BorderInfo PostXRightBorderInfo { get; set; }

        // Padding and fixed-border variables.
        internal BorderInfo LeftBorderInfo { get; set; }
        internal BorderInfo RightBorderInfo { get; set; }
        internal BorderInfo TopBorderInfo { get; set; }
        internal BorderInfo BottomBorderInfo { get; set; }
        internal bool RetainFixedBorders { get; set; }

        // Expected result borders.
        internal double LeftResultPos { get; set; }
        internal double RightResultPos { get; set; }
        internal double TopResultPos { get; set; }
        internal double BottomResultPos { get; set; }
        private bool resultPosWasSet;
        internal void SetResultPositions(double left, double right, double top, double bottom)
        {
            LeftResultPos = left;
            RightResultPos = right;
            TopResultPos = top;
            BottomResultPos = bottom;
            this.resultPosWasSet = true;
        }

        private static double MaxAllowedDiff { get { return ResultVerifierBase.DefaultPositionTolerance; } }

        internal List<VariableDef> Variables
        {
            get { return this.variableDefs; }
        }
        internal List<ClusterDef> Clusters
        {
            get { return this.clusterDefs; }
        }

        internal static TestContext TestContext { get; set; }
        private static void WriteLine(string format, params object[] args)
        {
            MsaglTestBase.WriteLine(TestContext, string.Format(format, args));
        }

        internal ClusterDef()
            : this(0.0, 0.0)
        {
        }

        internal ClusterDef(double minSizeX, double minSizeY)
        {
            this.ClusterId = nextClusterId++;
            this.DesiredPosX = double.NaN;
            this.DesiredPosY = double.NaN;
            this.MinimumSizeX = minSizeX;
            this.MinimumSizeY = minSizeY;
            this.LeftBorderInfo = new BorderInfo(0.0);
            this.RightBorderInfo = new BorderInfo(0.0);
            this.TopBorderInfo = new BorderInfo(0.0);
            this.BottomBorderInfo = new BorderInfo(0.0);
            this.PostXLeftBorderInfo = new BorderInfo(0.0);
            this.PostXRightBorderInfo = new BorderInfo(0.0);
        }

        internal ClusterDef(BorderInfo lbi, BorderInfo rbi,
                            BorderInfo tbi, BorderInfo bbi)
            : this(0.0, 0.0, lbi, rbi, tbi, bbi)
        {
        }

        internal ClusterDef(double minSizeX, double minSizeY,
                            BorderInfo lbi, BorderInfo rbi,
                            BorderInfo tbi, BorderInfo bbi)
            : this()
        {
            this.MinimumSizeX = minSizeX;
            this.MinimumSizeY = minSizeY;
            this.LeftBorderInfo = lbi;
            this.RightBorderInfo = rbi;
            this.TopBorderInfo = tbi;
            this.BottomBorderInfo = bbi;
        }

        internal bool Verify()
        {
            if (!this.resultPosWasSet)
            {
                return true;
            }

            Validate.IsFalse(IsNewHierarchy, "Hierarchy roots should not set expected positions");

            if (!IsEmpty)
            {
                if ((this.MinimumSizeX - this.SizeX) >= MaxAllowedDiff)
                {
                    return false;
                }
                if ((this.MinimumSizeY - this.SizeY) >= MaxAllowedDiff)
                {
                    return false;
                }
            }
            if (MaxAllowedDiff <= Math.Abs(this.Left - this.LeftResultPos))
            {
                return false;
            }
            if (MaxAllowedDiff <= Math.Abs(this.Right - this.RightResultPos))
            {
                return false;
            }
            if (MaxAllowedDiff <= Math.Abs(this.Top - this.TopResultPos))
            {
                return false;
            }
            if (MaxAllowedDiff <= Math.Abs(this.Bottom - this.BottomResultPos))
            {
                return false;
            }
            return true;
        }

        internal void AddVariableDef(VariableDef varDef)
        {
            this.variableDefs.Add(varDef);
            varDef.ParentClusters.Add(this);
        }
        internal void AddClusterDef(ClusterDef clusDef)
        {
            this.clusterDefs.Add(clusDef);
            clusDef.ParentClusterDef = this;
        }
        internal OverlapRemovalCluster CreateCluster(ConstraintGenerator conGen)
        {
            if (null == this.Cluster)
            {
                // Ensure the parent Cluster is created as we must pass it as a parameter.
                // Don't call this.ParentCluster here because it's got an Assert that our
                // cluster has been created - and it hasn't, yet; that's what we're doing here.
                // clusParent remains null if this.IsNewHierarchy.
                OverlapRemovalCluster clusParent = null;
                if (!this.IsNewHierarchy)
                {
                    clusParent = (null == this.ParentClusterDef)
                                     ? conGen.DefaultClusterHierarchy
                                     : this.ParentClusterDef.CreateCluster(conGen);
                }
                if (conGen.IsHorizontal)
                {
                    this.Cluster = conGen.AddCluster(
                        clusParent,
                        this.ClusterId,
                        this.MinimumSizeX,
                        this.MinimumSizeY,
                        this.LeftBorderInfo,
                        this.RightBorderInfo,
                        this.TopBorderInfo,
                        this.BottomBorderInfo);
                }
                else
                {
                    // Use horizontal PostX BorderInfos due to MinimumSize; see PostX().
                    this.Cluster = conGen.AddCluster(
                        clusParent,
                        this.ClusterId,
                        this.MinimumSizeY,
                        this.MinimumSizeX,
                        this.TopBorderInfo,
                        this.BottomBorderInfo,
                        this.PostXLeftBorderInfo,
                        this.PostXRightBorderInfo);
                }
            }
            return this.Cluster;
        }

        private bool UnexpectedFixedBorderMove(BorderInfo bi, double edgePosition, string edgeName, bool isX, ref bool summaryDone)
        {
            if (Math.Abs(bi.FixedPosition - edgePosition) > MaxAllowedDiff)
            {
                if (!summaryDone)
                {
                    WriteLine("Error: Unexpected {0} fixed-border change for Cluster '{1}':", isX ? "X" : "Y", this.ClusterId);
                    WriteLine("      OlapCluster: {0} L/R {1:F5}/{2:F5}", this.Cluster, isX ? this.Left : this.Top, isX ? this.Right : this.Bottom);
                    summaryDone = true;
                }
                WriteLine("   {0}BorderInfo: {1}", edgeName, this.LeftBorderInfo);
                return true;
            }
            return false;
        }
        internal bool PostX()
        {
            // Called when X ConGen/Solver is complete; preserve X coords and clear out the
            // Cluster object for replacement by Y.
            this.PositionX = this.Cluster.Position;
            this.SizeX = this.Cluster.Size;

            bool succeeded = true;
            if (!IsEmpty)
            {
                // Verify that if we have one fixed border, we didn't move it appreciably.
                // Don't do this if both borders are fixed because the heavyweights can force each other to move;
                // for this reason, update any BorderInfos that are IsFixedPosition to reflect this possible movement;
                // otherwise we could encounter situations as described in ComputeBorders where we have excessive
                // space between the fixed border and the outer nodes, and thus the Vertical pass in the Generator
                // won't know the true X border positions by just evaluating the nodes' X positions.  This only 
                // needs to be done for the horizontal pass; we don't revisit the vertical nodes in the Generator
                // after the vertical pass.
                bool summaryDone = false;
                if (this.LeftBorderInfo.IsFixedPosition)
                {
                    if (!this.RightBorderInfo.IsFixedPosition && (1 == numClustersWithFixedBordersH))
                    {
                        if (UnexpectedFixedBorderMove(this.LeftBorderInfo, this.Left, "Left", true /* isX */, ref summaryDone))
                        {
                            succeeded = false;
                        }
                    }
                    this.LeftBorderInfo = new BorderInfo(this.LeftBorderInfo.InnerMargin, this.Left, this.LeftBorderInfo.Weight);
                }
                if (this.RightBorderInfo.IsFixedPosition)
                {
                    if (!this.LeftBorderInfo.IsFixedPosition && (1 == numClustersWithFixedBordersH))
                    {
                        if (UnexpectedFixedBorderMove(this.RightBorderInfo, this.Right, "Right", true /* isX */, ref summaryDone))
                        {
                            succeeded = false;
                        }
                    }
                    this.RightBorderInfo = new BorderInfo(this.RightBorderInfo.InnerMargin, this.Right, this.RightBorderInfo.Weight);
                }

                // Minimum-size clusters run into the same issue as described in the foregoing FixedPosition, so that the
                // V constraint gen knows the full extent of the H cluster dimension that it has to generate constraints on.
                // In order to produce output testdatafiles that are not "polluted" by this, use a separate BorderInfo.
                this.PostXLeftBorderInfo = LeftBorderInfo;
                this.PostXRightBorderInfo = RightBorderInfo;
                if (0.0 != this.MinimumSizeX)
                {
                    this.PostXLeftBorderInfo = new BorderInfo(this.LeftBorderInfo.InnerMargin, this.Left, this.LeftBorderInfo.Weight);
                    this.PostXRightBorderInfo = new BorderInfo(this.RightBorderInfo.InnerMargin, this.Right, this.RightBorderInfo.Weight);
                }
            } // endif we have Variables
            this.Cluster = null;
            return succeeded;
        }
        internal bool PostY()
        {
            // Called when Y ConGen/Solver is complete.
            this.PositionY = this.Cluster.Position;
            this.SizeY = this.Cluster.Size;

            bool succeeded = true;
            if (!IsEmpty)
            {
                // The Horizontal pos/size should not have changed during the Vertical solving;
                // if either border is fixed, we'll have positioned it according to variables to
                // ensure that it is within constraint distance.  However, some movement can happen
                // if there is more than one cluster on the same perpendicular axis with fixed borders
                // (the high weights move each other directly or indirectly), or in the case we have
                // non-cluster variables with fairly high weight deep within cluster boundaries,
                // causing the weighted movement to evict it to move the fixed border a tiny bit. 
                double epsilon = MaxAllowedDiff;
                if ((Math.Abs(this.PositionX - this.Cluster.PositionP) > epsilon)
                         || (Math.Abs(this.SizeX - this.Cluster.SizeP) > epsilon))
                {
                    // If we have nested clusters and fixed borders there are some cases we can't verify
                    // due to the notes in ComputeBorders, unless we've gone outside the previous boundaries.
                    bool fOk = false;
                    if (this.clusterDefs.Count > 0)
                    {
                        fOk = true;
                        double prevLeft = this.PositionX - (this.SizeX / 2);
                        double prevRight = this.PositionX + (this.SizeX / 2);
                        double newLeft = this.Cluster.PositionP - (this.Cluster.SizeP / 2);
                        double newRight = this.Cluster.PositionP + (this.Cluster.SizeP / 2);
                        if (Math.Abs(prevLeft - newLeft) > epsilon)
                        {
                            if ((newLeft < prevLeft)       // outside the previous borders
                                || !this.LeftBorderInfo.IsFixedPosition)
                            {
                                fOk = false;
                            }
                        }
                        if (Math.Abs(prevRight - newRight) > epsilon)
                        {
                            if ((newRight > prevRight)     // outside the previous borders
                                || !this.RightBorderInfo.IsFixedPosition)
                            {
                                fOk = false;
                            }
                        }
                    }
                    if (!fOk)
                    {
                        WriteLine("Error: Unexpected change in X position and/or size for Cluster '{0}':", this.ClusterId);
                        WriteLine("   OlapCluster: {0} L/R {1:F5}/{2:F5}",
                                this.Cluster, this.Cluster.OpenP, this.Cluster.CloseP);
                        WriteLine("      Previous:                                       pP {0:F5} sP {1:F5} L/R {2:F5}/{3:F5}",
                                this.PositionX, this.SizeX, this.Left, this.Right);
                        succeeded = false;
                    }
                }

                if (1 == numClustersWithFixedBordersV)
                {
                    // Verify that if we have one fixed border, we didn't move it appreciably.
                    // Don't do this if both borders are fixed as they potentially can force each other to move.
                    bool summaryDone = false;
                    if (this.TopBorderInfo.IsFixedPosition && !this.BottomBorderInfo.IsFixedPosition)
                    {
                        if (UnexpectedFixedBorderMove(this.TopBorderInfo, this.Top, "Top", false /* isX */, ref summaryDone))
                        {
                            succeeded = false;
                        }
                    }
                    if (!this.TopBorderInfo.IsFixedPosition && this.BottomBorderInfo.IsFixedPosition)
                    {
                        if (UnexpectedFixedBorderMove(this.BottomBorderInfo, this.Bottom, "Bottom", false /* isX */, ref summaryDone))
                        {
                            succeeded = false;
                        }
                    }
                }
            } // endif we have Variables
            this.Cluster = null;
            return succeeded;
        }

        // Compute initial borders, fixed or not - but only "set" the fixed positions as
        // we let the ConstraintGenerator actually do that.
        internal void ComputeInitialBorders()
        {
            if (this.IsEmpty || this.HasDesiredPos)
            {
                return;
            }

            // Some verifications won't work if we've got more than one cluster with fixed
            // borders - so they'll check this and skip those checks.
            if (this.LeftBorderInfo.IsFixedPosition || this.RightBorderInfo.IsFixedPosition)
            {
                ++numClustersWithFixedBordersH;
            }
            if (this.TopBorderInfo.IsFixedPosition || this.BottomBorderInfo.IsFixedPosition)
            {
                ++numClustersWithFixedBordersV;
            }

            // Get the minimum variable positions.
            IPositionInfo posLeft = 0 == this.variableDefs.Count ? null : this.variableDefs[0];
            IPositionInfo posRight = posLeft;
            IPositionInfo posTop = posLeft;
            IPositionInfo posBottom = posLeft;
            if (null != posLeft)
            {
                foreach (VariableDef varDef in this.variableDefs)
                {
                    if (varDef.DesiredPosX < posLeft.DesiredPosX)
                    {
                        posLeft = varDef;
                    }
                    if (varDef.DesiredPosX > posRight.DesiredPosX)
                    {
                        posRight = varDef;
                    }
                    if (varDef.DesiredPosY < posTop.DesiredPosY)
                    {
                        posTop = varDef;
                    }
                    if (varDef.DesiredPosY > posBottom.DesiredPosY)
                    {
                        posBottom = varDef;
                    }
                } // endfor each variable
            }

            // Get the minimum cluster positions.
            foreach (ClusterDef clusDef in this.clusterDefs)
            {
                clusDef.ComputeInitialBorders();
                if (null == posLeft)
                {
                    // We had no variables so initialize with the first OlapCluster.
                    posLeft = clusDef;
                    posRight = posLeft;
                    posTop = posLeft;
                    posBottom = posLeft;
                    continue;
                }

                // Because child clusters have already evaluated their borders from their outer variables
                // as we've done above, compare the outer borders of child clusters rather than their DesiredX. 
                if (clusDef.InitialLeft < posLeft.InitialLeft)
                {
                    posLeft = clusDef;
                }
                if (clusDef.InitialRight > posRight.InitialRight)
                {
                    posRight = clusDef;
                }
                if (clusDef.InitialTop < posTop.InitialTop)
                {
                    posTop = clusDef;
                }
                if (clusDef.InitialBottom > posBottom.InitialBottom)
                {
                    posBottom = clusDef;
                }
            }

            // Compute our initial positions based upon variable boundaries.
            Validate.IsNotNull(posLeft, "posLeft should not be null as we checked for IsEmpty above");
// ReSharper disable PossibleNullReferenceException
            this.SizeX = posRight.InitialRight - posLeft.InitialLeft;
            this.SizeY = posBottom.InitialBottom - posTop.InitialTop;
// ReSharper restore PossibleNullReferenceException
            this.DesiredPosX = posLeft.InitialLeft + (this.SizeX / 2);
            this.DesiredPosY = posTop.InitialTop + (this.SizeY / 2);

            ////
            // Compute our fixed borders from the outer variables.  For this, we set the fixed border's
            // position such that its inner edge is adjacent to the outer edge of the outermost variable
            // in that direction.  Note that we do *not* set it to the outermost edge of all variables;
            // the outermost edge may belong to a variable whose position (midpoint) is inside that of
            // another variable, in which case the variable with the outermost edge would move inward,
            // leaving more than the minimally-satisfying space between the border and the variable that
            // ends up being outermost.  For example:
            //  ++     +-------+
            //  ||     |   A   |
            //  ||+---------------------------+
            //  |||             B             |
            //  ||+---------------------------+
            //  ||     |       |
            //  ++     +-------+
            // Nodes A and B are labelled at their midpoints.  Node B has the leftmost edge, but during
            // solving, it will be moved right to resolve the overlap with A (ignore the fact that in
            // this diagram it would actually be moved up instead because of deferral to Vertical would
            // result in less movement; drawing it this way makes it easier to understand the diagram).
            // If we'd started the border at the left of B, where it is in this diagram, Solve() could 
            // leave the space between the border and A to be greater than PaddingX, and thus the 
            // Y-pass generation would generate an X position closer to A, causing us to run afoul of the
            // Assert in PostY().  (This is not an issue for non-fixed borders, because ConstraintGenerator
            // forces their positions initially to be at the opposite side; thus the Solver will force
            // them to move across the cluster to the point at which they minimally satisfy the constraints).
            //
            // Unfortunately it turns out that we cannot rely on this when we have nested clusters due to
            // the following case (=== is cluster, --- is node):
            //  +=====================================+
            //  |                         +-----+     |
            //  |                         |  A  |     |
            //  |+===================================+|
            //  ||+---+                         +---+||
            //  ||| C |                         | B |||
            //  ||+---+                         +---+||
            //  |+===================================+|
            //  |                         |     |     |
            //  |                         +-----+     |
            //  +=====================================+
            // Node A is in the parent cluster, nodes B and C in the nested OlapCluster.  This will generate
            // constraints to move A to the right of the nested cluster's right border, which will in turn
            // move B to the left if it's an unfixed border (which will have a lighter weight).  Thus A
            // stays where it is, B and the nested cluster's right border move to the left of A, and we are
            // left with extra space between A and the right border of the parent OlapCluster.  So we will
            // end up skipping this check in PostY() for fixed borders if there are nested clusters.
            //
            // Note that all the foregoing is just for predictability for testing purposes; for live runs,
            // they can put the fixed border anywhere and the variables will follow them.
            ////
            LeftBorderInfo = ComputeFixedBorder(LeftBorderInfo, posLeft.InitialLeft);
            RightBorderInfo = ComputeFixedBorder(RightBorderInfo, posRight.InitialRight);
            TopBorderInfo = ComputeFixedBorder(TopBorderInfo, posTop.InitialTop);
            BottomBorderInfo = ComputeFixedBorder(BottomBorderInfo, posBottom.InitialBottom);

            if (TestGlobals.VerboseLevel >= 3)
            {
                WriteLine("Cluster {0} initially computed borders (padding not included):", this.ClusterId);
                WriteLine("   LeftBorder ({0} {1}):   {2} InitialX {3} SizeX {4}",
                    posLeft.ClassName, posLeft.InstanceId,
                    ComputeInitialBorder(LeftBorderInfo, this.InitialLeft),
                    this.DesiredPosX, this.SizeX);
                WriteLine("   RightBorder ({0} {1}):  {2}",
                    posRight.ClassName, posRight.InstanceId,
                    ComputeInitialBorder(RightBorderInfo, this.InitialRight));
                WriteLine("   TopBorder ({0} {1}):    {2} InitialY {3} SizeY {4}",
                    posTop.ClassName, posTop.InstanceId,
                    ComputeInitialBorder(TopBorderInfo, this.InitialTop),
                    this.DesiredPosY, this.SizeY);
                WriteLine("   BottomBorder ({0} {1}): {2}",
                    posBottom.ClassName, posBottom.InstanceId,
                    ComputeInitialBorder(BottomBorderInfo, this.InitialBottom));
            }
        } // end ComputeFixedBorders()

        private BorderInfo ComputeFixedBorder(BorderInfo bi, double innerEdgePosition)
        {
            // If the passed BorderInfo is fixed, then set its position such that its inner
            // edge is at innerEdgePosition.
            if (bi.IsFixedPosition && !RetainFixedBorders)
            {
                return new BorderInfo(bi.InnerMargin, innerEdgePosition - bi.InnerMargin, bi.Weight);
            }
            return bi;
        }

        // For initialization verbose output; we don't send an actual BorderInfo for borders with no margin etc.
        private static BorderInfo ComputeInitialBorder(BorderInfo bi, double unfixedInitialBorderPosition)
        {
            // If the passed BorderInfo is fixed, then set its position such that its inner
            // edge is at innerEdgePosition.
            if (bi.IsFixedPosition)
            {
                return bi;
            }
            return new BorderInfo(bi.InnerMargin, unfixedInitialBorderPosition - bi.InnerMargin, bi.Weight);
        }

        internal bool IsMinimumSizeX
        {
            get
            {
                // If we're within a rounding error of the minimum size, assume we had to grow to meet it.
                if (0.0 == this.MinimumSizeX)
                {
                    return false;
                }
                return (this.SizeX - this.MinimumSizeX
                                    - OverlapRemovalCluster.CalcBorderWidth(this.LeftBorderInfo.InnerMargin)
                                    - OverlapRemovalCluster.CalcBorderWidth(this.RightBorderInfo.InnerMargin))
                                <= MaxAllowedDiff;
            }
        }

        internal bool IsMinimumSizeY
        {
            get
            {
                // If we're within a rounding error of the minimum size, assume we had to grow to meet it.
                if (0.0 == this.MinimumSizeY)
                {
                    return false;
                }
                return (this.SizeY - this.MinimumSizeY
                                    - OverlapRemovalCluster.CalcBorderWidth(this.TopBorderInfo.InnerMargin)
                                    - OverlapRemovalCluster.CalcBorderWidth(this.BottomBorderInfo.InnerMargin))
                                <= MaxAllowedDiff;
            }
        }

        internal static string IsFixedString(BorderInfo bi)
        {
            if (bi.IsFixedPosition)
            {
                return "Fixed";
            }
            return "Free";
        }

        public override string ToString()
        {
            return this.ClusterId.ToString();
        }

        #region IPositionInfo
        // Available after initialization.

        // For preserving coordinates across the X/Y replacement.
        public double PositionX { get; set; }
        public double PositionY { get; set; }

        public double Left
        {
            get { return PositionX - (SizeX / 2); }
        }
        public double Right
        {
            get { return PositionX + (SizeX / 2); }
        }
        public double Top
        {
            get { return PositionY - (SizeY / 2); }
        }
        public double Bottom
        {
            get { return PositionY + (SizeY / 2); }
        }

        // For initial placement before Generate/Solve.
        public double DesiredPosX { get; private set; }
        public double DesiredPosY { get; private set; }
        private bool HasDesiredPos { get { return !double.IsNaN(DesiredPosX); } }

        public double SizeX { get; private set; }
        public double SizeY { get; private set; }

        public double InitialLeft { get { return DesiredPosX - (SizeX / 2); } }
        public double InitialRight { get { return DesiredPosX + (SizeX / 2); } }
        public double InitialTop { get { return DesiredPosY - (SizeY / 2); } }
        public double InitialBottom { get { return DesiredPosY + (SizeY / 2); } }

        public string ClassName { get { return "Clus"; } }
        public string InstanceId { get { return this.ClusterId.ToString(); } }
        #endregion IPositionInfo

    }
}
