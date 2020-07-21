// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlapRemovalVerifier.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.ProjectionSolver;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    /// <summary>
    /// Basic verification logic, as well as the single-inheritance-only propagation of MsaglTestBase.
    /// </summary>
    [TestClass]
    public class OverlapRemovalVerifier : ResultVerifierBase
    {
        // The ProjectionSolver.Solvers and Solutions created in CheckResult are used by CreateTestFile.
        protected Solver SolverX { get; set; }
        protected Solver SolverY { get; set; }
        protected Solution SolutionX { get; set; }
        protected Solution SolutionY { get; set; }

        // The minimal padding we require.
        internal static double InitialMinPaddingX { get; set; }
        internal static double InitialMinPaddingY { get; set; }
        internal double MinPaddingX { get; set; }
        internal double MinPaddingY { get; set; }

        // InitialAllowDeferToVertical can be set by TestConstraints.exe.
        internal static bool InitialAllowDeferToVertical { get; set; }
        internal bool AllowDeferToVertical { get; set; }

        static OverlapRemovalVerifier()
        {
            InitialAllowDeferToVertical = true;
        }

        internal OverlapRemovalVerifier()
        {
            this.InitializeMembers();
        }

        // Override of [TestInitialize] method.
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
            this.InitializeMembers();
        }

        private void InitializeMembers()
        {
            SolverX = null;
            SolverY = null;
            SolutionX = null;
            SolutionY = null;
            MinPaddingX = InitialMinPaddingX;
            MinPaddingY = InitialMinPaddingY;
            AllowDeferToVertical = InitialAllowDeferToVertical;
            ClusterDef.Reset();
        }

        // Centralized for easy breakpoint setting.
        private static string FailTag(string strTag)
        {
            return string.Format("[ {0} ]", strTag);
        }

        // These overloads work with the local Test*()...
        internal bool CheckResult(VariableDef[] variableDefs,
                        ConstraintDef[] constraintDefsX, ConstraintDef[] constraintDefsY,
                        double[] expectedPositionsX, double[] expectedPositionsY,
                        bool checkResults)
        {
            return CheckResult(variableDefs, /*clusterDefs:*/ null,
                        constraintDefsX, constraintDefsY,
                        expectedPositionsX, expectedPositionsY, checkResults);
        }
        
        internal bool CheckResult(VariableDef[] variableDefs, ClusterDef[] clusterDefs,
                        ConstraintDef[] constraintDefsX, ConstraintDef[] constraintDefsY,
                        double[] expectedPositionsX, double[] expectedPositionsY,
                        bool checkResults)
        {
            for (uint id = 0; id < variableDefs.Length; ++id)
            {
                variableDefs[id].SetExpected(id, expectedPositionsX[id], expectedPositionsY[id]);
            }
            return CheckResult(variableDefs, clusterDefs, constraintDefsX, constraintDefsY, checkResults);
        }

        // ... and this overload works with DataFiles as well as performing the actual work.
        internal bool CheckResult(IEnumerable<VariableDef> iterVariableDefs, IEnumerable<ClusterDef> iterClusterDefs,
                        IEnumerable<ConstraintDef> iterConstraintDefsX, IEnumerable<ConstraintDef> iterConstraintDefsY,
                        bool checkResults)
        {
            ShowInitialRectangles(iterVariableDefs);
            bool succeeded = true;
            
            var sw = new Stopwatch();

            this.CreateSolversAndGetSolutions(sw, iterClusterDefs, iterVariableDefs, iterConstraintDefsX, iterConstraintDefsY, ref succeeded);
            VerifyExpectedClusterPositions(iterClusterDefs, ref succeeded);

            // Verify the Variable positions are as expected.
            if (!PostCheckResults(iterVariableDefs, SolutionX.GoalFunctionValue, SolutionY.GoalFunctionValue, sw, checkResults))
            {
                // This will actually duplicate the PostCheckResults error notification, but we
                // want the failtag, so, well, oh well.
                WriteLine("Error {0}:  Expected Node positions failed", FailTag("NodePosFail"));
                succeeded = false;
            }

            if (TestGlobals.VerboseLevel >= 1)
            {
                var strIterationsX = GetIterationsString(SolutionX);
                var strIterationsY = GetIterationsString(SolutionY);
                WriteLine("  X Project iterations: {0}", strIterationsX);
                WriteLine("  Y Project iterations: {0}", strIterationsY);
                WriteLine("  Goal Function values: {0} {1}", SolutionX.GoalFunctionValue, SolutionY.GoalFunctionValue);
            }

            DoDetailedClusterVerification(iterClusterDefs, iterVariableDefs, ref succeeded);
            ShowRectangles(iterVariableDefs, iterClusterDefs);
            return succeeded;
        }

        private void CreateSolversAndGetSolutions(Stopwatch sw, IEnumerable<ClusterDef> iterClusterDefs, IEnumerable<VariableDef> iterVariableDefs, IEnumerable<ConstraintDef> iterConstraintDefsX, IEnumerable<ConstraintDef> iterConstraintDefsY, ref bool succeeded)
        {
            var olapParameters = new OverlapRemovalParameters(this.SolverParameters)
                {
                    AllowDeferToVertical = this.AllowDeferToVertical
                };

            for (uint rep = 0; rep < TestGlobals.TestReps; ++rep)
            {
                sw.Start();

                // Load the Horizontal ProjectionSolver and ConstraintGenerator with variables.
                // We must Solve the X coordinates before generating the Y ones, so that the
                // Y generation, whose scan line uses X coordinates, uses the updated values.
                var generatorX = new ConstraintGenerator(true /* fIsHorizontal */, this.MinPaddingX, this.MinPaddingY);
                this.SolverX = new Solver();

                // First create the X Clusters.
                if (null != iterClusterDefs)
                {
                    foreach (ClusterDef clusDef in iterClusterDefs)
                    {
                        clusDef.ComputeInitialBorders();  // called for H only; does both H and V
                        clusDef.CreateCluster(generatorX);
                    }
                }
                this.AddNodesAndSolve(generatorX, iterVariableDefs, olapParameters, iterConstraintDefsX, ref succeeded);

                sw.Stop();

                this.VerifyAxisResults(rep, generatorX, olapParameters, ref succeeded);

                sw.Start();

                // Load the Vertical ProjectionSolver and ConstraintGenerator with variables.
                // This uses the X coordinate determined by the above solution for the perpendicular.
                var generatorY = new ConstraintGenerator(false /* fIsHorizontal */, this.MinPaddingY, this.MinPaddingX);
                this.SolverY = new Solver();

                // First create the Y Clusters.
                if (null != iterClusterDefs)
                {
                    // Clear out the ConGenX Clusters first, then create them in ConGenY.
                    foreach (ClusterDef clusDef in iterClusterDefs)
                    {
                        if (!clusDef.PostX())
                        {
                            succeeded = false;
                        }
                    }
                    foreach (ClusterDef clusDef in iterClusterDefs)
                    {
                        clusDef.CreateCluster(generatorY);
                    }
                }

                this.AddNodesAndSolve(generatorY, iterVariableDefs, olapParameters, iterConstraintDefsY, ref succeeded);

                sw.Stop();

                if (null != iterClusterDefs)
                {
                    foreach (ClusterDef clusDef in iterClusterDefs)
                    {
                        if (!clusDef.PostY())
                        {
                            succeeded = false;
                        }
                    }
                }

                this.VerifyAxisResults(rep, generatorY, olapParameters, ref succeeded);
            }
        }

        private void AddNodesAndSolve(ConstraintGenerator generator,
                        IEnumerable<VariableDef> iterVariableDefs,
                        OverlapRemovalParameters olapParameters,
                        IEnumerable<ConstraintDef> iterConstraintDefs,
                        ref bool succeeded)
        {
            var axisName = generator.IsHorizontal ? "X" : "Y";
            var solver = generator.IsHorizontal ? this.SolverX : this.SolverY;
            foreach (VariableDef varDef in iterVariableDefs)
            {
                // Create the variable in its initial cluster.
                OverlapRemovalCluster olapClusParent = generator.DefaultClusterHierarchy;
                if (varDef.ParentClusters.Count > 0)
                {
                    olapClusParent = varDef.ParentClusters[0].Cluster;
                }
                OverlapRemovalNode newNode;
                if (generator.IsHorizontal)
                {
                    newNode = generator.AddNode(olapClusParent, varDef.IdString,
                                           varDef.DesiredPosX, varDef.DesiredPosY,
                                           varDef.SizeX, varDef.SizeY, varDef.WeightX);
                    varDef.VariableX = new OlapTestNode(newNode);
                }
                else 
                {
                    newNode = generator.AddNode(olapClusParent, varDef.IdString,
                                           varDef.DesiredPosY, varDef.VariableX.ActualPos,
                                           varDef.SizeY, varDef.SizeX, varDef.WeightY);
                    varDef.VariableY = new OlapTestNode(newNode);
                }

                // Add it to its other clusters.
                for (int ii = 1; ii < varDef.ParentClusters.Count; ++ii)
                {
                    olapClusParent = varDef.ParentClusters[ii].Cluster;
                    generator.AddNodeToCluster(olapClusParent, newNode);
                }
            }
            generator.Generate(solver, olapParameters);
            if (TestGlobals.VerboseLevel >= 3)
            {
                this.WriteLine("  {0} Constraints:", axisName);
                foreach (Constraint cst in solver.Constraints.OrderBy(cst => cst))
                {
                    this.WriteLine("    {0} {1} g {2:F5}", cst.Left.UserData, cst.Right.UserData, cst.Gap);
                }
            }

            if (null != iterConstraintDefs)
            {
                // TODO: Compare expected to actual constraints generated in solver
            }

            var solution = generator.Solve(solver, olapParameters, false /* doGenerate */);
            if (!this.VerifySolutionMembers(solution, /*iterNeighborDefs:*/ null))
            {
                succeeded = false;
            }
            if (generator.IsHorizontal)
            {
                this.SolutionX = solution;
            }
            else
            {
                this.SolutionY = solution;
            }
        }

        private void VerifyAxisResults(uint rep, ConstraintGenerator generator, OverlapRemovalParameters olapParameters, ref bool succeeded)
        {
            var axisName = generator.IsHorizontal ? "X" : "Y";
            var solver = generator.IsHorizontal ? this.SolverX : this.SolverY;
            var solution = generator.IsHorizontal ? this.SolutionX : this.SolutionY;
            if (TestGlobals.VerboseLevel >= 3)
            {
                this.WriteLine("  {0} Nodes after solving Constraints:", axisName);
                foreach (Variable var in solver.Variables)
                {
                    var node = (OverlapRemovalNode)var.UserData;
                    System.Diagnostics.Debug.Write($"    {node}");

                    const string Format = " - L/R T/B {0:F5}/{1:F5} {2:F5}/{3:F5}";
                    if (generator.IsHorizontal)
                    {
                        // X is Perpendicular here
                        this.WriteLine(Format, node.OpenP, node.CloseP, node.Open, node.Close);
                    }
                    else
                    {
                        // Y is Perpendicular here
                        this.WriteLine(Format, node.Open, node.Close, node.OpenP, node.CloseP);
                    }
                }
            }

            // We should never see unsatisfiable constraints since we created them ourselves.
            if (0 != solution.NumberOfUnsatisfiableConstraints)
            {
                succeeded = false;
                this.WriteLine(" *** Error! {0} unsatisfiable {1} constraints found ***",
                        solution.NumberOfUnsatisfiableConstraints, axisName);
                if ((TestGlobals.VerboseLevel >= 2) && (0 == rep))
                {
                    foreach (Constraint cst in solver.Constraints.Where(cst => cst.IsUnsatisfiable))
                    {
                        this.WriteLine("      {0}", cst);
                    }
                } // endif VerboseLevel
            } // endif unsatisfiable constraints

            bool violationSeen = false;
            foreach (Constraint cst in solver.Constraints)
            {
                if (!this.VerifyConstraint(olapParameters.SolverParameters, cst, generator.IsHorizontal, ref violationSeen))
                {
                    succeeded = false;
                }
            }

            if (solution.ExecutionLimitExceeded)
            {
                this.WriteLine(GetCutoffString(solution));
            }
        }

        private void VerifyExpectedClusterPositions(IEnumerable<ClusterDef> iterClusterDefs, ref bool succeeded)
        {
            if (null != iterClusterDefs)
            {
                if (TestGlobals.VerboseLevel >= 1)
                {
                    int minClusterSizesX = 0;
                    int minClusterSizesY = 0;
                    foreach (ClusterDef clusDef in iterClusterDefs)
                    {
                        if (clusDef.IsMinimumSizeX)
                        {
                            ++minClusterSizesX;
                        }
                        if (clusDef.IsMinimumSizeY)
                        {
                            ++minClusterSizesY;
                        }
                    }
                    this.WriteLine("Number of clusters at minimum sizes:  {0} X, {1} Y", minClusterSizesX, minClusterSizesY);
                }
                if (TestGlobals.VerboseLevel >= 2)
                {
                    this.WriteLine("Dumping Cluster final positions:");
                }

                foreach (ClusterDef clusDef in iterClusterDefs)
                {
                    bool thisClusterOk = clusDef.Verify();
                    if (!thisClusterOk)
                    {
                        this.WriteLine("Error {0}:  Expected Cluster positions failed", FailTag("ClusPosFail"));
                        succeeded = false;
                    }
                    if (TestGlobals.VerboseLevel >= 2 || !thisClusterOk)
                    {
                        this.WriteLine("  Clus [{0}] L/R T/B ({1:F5}/{2:F5}, {3:F5}/{4:F5}) SizeX/Y {5:F5}/{6:F5})", clusDef.ClusterId,
                            clusDef.Left, clusDef.Right, clusDef.Top, clusDef.Bottom, clusDef.SizeX, clusDef.SizeY);
                        if (!thisClusterOk)
                        {
                            this.WriteLine("    Expected  L/R T/B ({0:F5}/{1:F5}, {2:F5}/{3:F5}) SizeX/Y {4:F5}/{5:F5}) MinSizeX/Y {6:F5}/{7:F5})",
                                clusDef.LeftResultPos, clusDef.RightResultPos,
                                clusDef.TopResultPos, clusDef.BottomResultPos,
                                clusDef.RightResultPos - clusDef.LeftResultPos,
                                clusDef.BottomResultPos - clusDef.TopResultPos,
                                clusDef.MinimumSizeX, clusDef.MinimumSizeY);
                        }
                    }
                }

                DumpRectangles(iterClusterDefs);
            }
        }

        private void DoDetailedClusterVerification(IEnumerable<ClusterDef> iterClusterDefs, IEnumerable<VariableDef> iterVariableDefs, ref bool succeeded)
        {
            // For some tests, we'll need to "unflatten" the hierarchy, including keeping a list of nodes
            // within that hierarchy for convenience.  Take advantage of the fact that we know we have a flattened
            // list of hierarchies from the file - we will be ordering by start/size within that hierarchy
            // to verify containment within clusters. Start with the DefaultClusterHierarchy equivalent,
            // then add all nodes to it that don't have an explicitly-assigned parent.
            //
            // Note:  It doesn't really make sense for a variable to be parentless in the DefaultClusterHierarchy
            // (i.e. at the root level of the DefaultClusterHierarchy, which doubles as the "clusterless" free
            // space), *and* for it to be within a cluster of another hierarchy - instead, it would simply
            // be created within that other hierarchy's cluster.  Therefore, I haven't modified the testfile
            // format to support specifying such a case: either the node lives at that clusterless level (i.e.
            // it hasn't been included in a cluster in the file, so we create it in the DefaultClusterHierarchy),
            // *or* it lives only in the clusters to which it has been added.  But this test supports either way.

            // All variables live at some cluster level, even if it's the DefaultCluster, so populate that.
            var dictHierarchies = new Dictionary<int, KeyValuePair<List<ClusterDef>, List<VariableDef>>>();
            UnflattenEachClusterHierarchy(iterClusterDefs, iterVariableDefs, dictHierarchies);

            // For each hierarchy, verify there is no overlap in the Variables' rectangles (overlap removal
            // (i.e. constraint generation) is only done between variables in the same hierarchy).  Order the
            // Variables by top border (vertically), then for each following Variable with a top border its
            // bottom border, verify that there is no horizontal overlap.
            double epsilon = this.SolverParameters.GapTolerance + 1e-6; // Add rounding error
            this.VerifyThereAreNoIntraHierarchyNodeOverlaps(dictHierarchies, epsilon, ref succeeded);
            this.VerifyClusterDefs(iterClusterDefs, iterVariableDefs, dictHierarchies, epsilon, ref succeeded);
        }

        private static void UnflattenEachClusterHierarchy(IEnumerable<ClusterDef> iterClusterDefs, IEnumerable<VariableDef> iterVariableDefs, Dictionary<int, KeyValuePair<List<ClusterDef>, List<VariableDef>>> dictHierarchies)
        {
            var kvpCurrentHierarchy = new KeyValuePair<List<ClusterDef>, List<VariableDef>>(new List<ClusterDef>(), new List<VariableDef>());
            dictHierarchies[0] = kvpCurrentHierarchy;
            foreach (VariableDef varDef in iterVariableDefs)
            {
                // If no parent, or a child of cluster 0 (which is implicit and not added to a test datafile's
                // cluster specifications), add it to the DefaultClusterHierarchy.
                if (0 == varDef.ParentClusters.Count)
                {
                    kvpCurrentHierarchy.Value.Add(varDef);
                }
                else
                {
                    foreach (ClusterDef clusDef in varDef.ParentClusters)
                    {
                        if (0 == clusDef.ClusterId)
                        {
                            kvpCurrentHierarchy.Value.Add(varDef);
                        }
                    }
                }
            }

            // Now if there are explicit clusters and/or hierarchies, add them and their variables to their hierarchy.
            if (null != iterClusterDefs)
            {
                foreach (ClusterDef clusDef in iterClusterDefs)
                {
                    Validate.IsTrue(clusDef.ClusterId > 0, "Explicit clusters must have ID > 0");
                    if (clusDef.IsNewHierarchy)
                    {
                        // Create a new hierarchy.
                        kvpCurrentHierarchy = new KeyValuePair<List<ClusterDef>, List<VariableDef>>(new List<ClusterDef>(), new List<VariableDef>());
                        dictHierarchies[clusDef.ClusterId] = kvpCurrentHierarchy;
                    }
                    else
                    {
                        // Find the root cluster of the hierarchy.  Default to the DefaultClusterHierarchy.
                        int rootId = 0;
                        for (ClusterDef clusDefParent = clusDef.ParentClusterDef; null != clusDefParent; clusDefParent = clusDefParent.ParentClusterDef)
                        {
                            if (clusDefParent.IsNewHierarchy)
                            {
                                rootId = clusDefParent.ClusterId;
                                Validate.IsNull(clusDefParent.ParentClusterDef, "IsNewHierarchy cluster's parent must be null");
                                break;
                            }
                        }
                        kvpCurrentHierarchy = dictHierarchies[rootId];
                        kvpCurrentHierarchy.Key.Add(clusDef);
                    } // endifelse IsNewHierarchy

                    foreach (VariableDef varDef in clusDef.Variables)
                    {
                        kvpCurrentHierarchy.Value.Add(varDef);
                    }
                } // endfor each cluster
            }
        }

        private void VerifyThereAreNoIntraHierarchyNodeOverlaps(Dictionary<int, KeyValuePair<List<ClusterDef>, List<VariableDef>>> dictHierarchies, double epsilon, ref bool succeeded)
        {
            foreach (KeyValuePair<List<ClusterDef>, List<VariableDef>> kvpCurHier in dictHierarchies.Values)
            {
                VariableDef[] localVarDefs = kvpCurHier.Value.OrderBy(varDef => varDef.Top).ToArray();
                for (int ii = 0; ii < localVarDefs.Length; ++ii)
                {
                    VariableDef varCur = localVarDefs[ii];
                    for (int jj = ii + 1; jj < localVarDefs.Length; ++jj)
                    {
                        VariableDef varCheck = localVarDefs[jj];

                        // Rounding error may leave these calculations slightly greater or less than zero.
                        // Name is <relativeToVarCur><RelativeToVarCheck>
                        double bottomTopOverlap = varCheck.Top - (varCur.Bottom + this.MinPaddingY);
                        if (bottomTopOverlap >= -epsilon)
                        {
                            // Out of range of varCur's size, so we're done with varCur.
                            break;
                        }

                        // Does varCheck's left or right border overlap?
                        bool hasSideOverlap = false;
                        if ((varCheck.Left - (varCur.Left - this.MinPaddingX)) > epsilon)
                        {
                            if ((varCheck.Left - (varCur.Right + this.MinPaddingX)) < -epsilon)
                            {
                                hasSideOverlap = true;
                            }
                        }
                        if ((varCheck.Right - (varCur.Left - this.MinPaddingX)) > epsilon)
                        {
                            if ((varCheck.Right - (varCur.Right + this.MinPaddingX)) < -epsilon)
                            {
                                hasSideOverlap = true;
                            }
                        }

                        if (hasSideOverlap)
                        {
                            // Uh oh.
                            this.WriteLine("Error {0}: Overlap exists between Nodes '{1}' and '{2}'", FailTag("NodeOlap"),
                                varCur.IdString, varCheck.IdString);
                            this.WriteLine("   Node {0}: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}", varCur.IdString,
                                varCur.Left, varCur.Right, varCur.Top, varCur.Bottom);
                            this.WriteLine("   Node {0}: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}", varCheck.IdString,
                                varCheck.Left, varCheck.Right, varCheck.Top, varCheck.Bottom);
                            succeeded = false;
                        }
                    } // endfor each variable jj
                } // endfor each variable ii
            }
        }

        private void VerifyClusterDefs(IEnumerable<ClusterDef> iterClusterDefs, IEnumerable<VariableDef> iterVariableDefs, Dictionary<int, KeyValuePair<List<ClusterDef>, List<VariableDef>>> dictHierarchies, double epsilon, ref bool succeeded)
        {
            // If we have clusters, verify that variables are within their cluster, and that clusters
            // are within their parent and do not overlap with a cluster that isn't a parent.
            if (null != iterClusterDefs)
            {
                this.VerifyNodesAreWithinParentClusterBounds(iterVariableDefs, epsilon, ref succeeded);
                this.VerifyClustersAreWithinParentClusterBounds(iterClusterDefs, epsilon, ref succeeded);
                this.VerifyClustersAreTight(iterClusterDefs, epsilon, ref succeeded);
                this.VerifyEachHierarchy(dictHierarchies, epsilon, ref succeeded);
            }
        }

        private void VerifyNodesAreWithinParentClusterBounds(IEnumerable<VariableDef> iterVariableDefs, double epsilon, ref bool succeeded)
        {
            // Verify that variables are within their clusters.  A variable with no ParentClusters
            // lives in the DefaultClusterHierarchy, which we don't have a ConstraintDef for.
            foreach (VariableDef varCheck in iterVariableDefs)
            {
                foreach (ClusterDef clusParent in varCheck.ParentClusters)
                {
                    // Root clusters (those that are in the ClusterHierarchies collection) don't have
                    // borders.  The DefaultClusterHierarchy is also a root cluster, but if the variable
                    // lives only there we'll never get here.
                    if (clusParent.IsNewHierarchy)
                    {
                        continue;
                    }

                    // Are varCheck's borders outside cluster bounds?  Negative overlap means yes.
                    // We're testing for nested variables here so include cluster margin at the relevant border.
                    double leftOverlap = varCheck.Left - clusParent.Left
                                         - clusParent.LeftBorderInfo.InnerMargin - this.MinPaddingX;
                    double rightOverlap = clusParent.Right - clusParent.RightBorderInfo.InnerMargin
                                          - varCheck.Right - this.MinPaddingX;
                    double topOverlap = varCheck.Top - clusParent.Top
                                        - clusParent.TopBorderInfo.InnerMargin - this.MinPaddingY;
                    double bottomOverlap = clusParent.Bottom - clusParent.BottomBorderInfo.InnerMargin
                                           - varCheck.Bottom - this.MinPaddingY;

                    if ((leftOverlap < -epsilon)
                        || (rightOverlap < -epsilon)
                        || (topOverlap < -epsilon)
                        || (bottomOverlap < -epsilon))
                    {
                        // Uh oh.
                        this.WriteLine("Error {0}: Node '{1}' is outside ParentCluster '{2}' bounds", FailTag("NodeParentClus"),
                            varCheck.IdString, clusParent.ClusterId);
                        this.WriteLine("   Node    {0}: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}", varCheck.IdString,
                            varCheck.Left, varCheck.Right, varCheck.Top, varCheck.Bottom);
                        this.WriteLine("   Cluster {0}: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}", clusParent.ClusterId,
                            clusParent.Left, clusParent.Right,
                            clusParent.Top, clusParent.Bottom);
                        this.WriteLine("   Overlap  : L/R T/B {0:F5}/{1:F5} {2:F5}/{3:F5}",
                            leftOverlap, rightOverlap, topOverlap, bottomOverlap);
                        succeeded = false;
                    } // endfor each clusParent
                } // endif null != (object)varCheck.ParentClusterDef
            }
        }

        private void VerifyClustersAreWithinParentClusterBounds(IEnumerable<ClusterDef> iterClusterDefs, double epsilon, ref bool succeeded)
        {
            foreach (ClusterDef clusDef in iterClusterDefs)
            {
                // Empty clusters have nothing to verify
                // Clusters at the root of a hierarchy have no borders.
                if (clusDef.IsEmpty)
                {
                    continue;
                }
                if (null != clusDef.ParentClusterDef)
                {
                    // Clusters at the root of a hierarchy have no borders.
                    if (clusDef.ParentClusterDef.IsNewHierarchy)
                    {
                        continue;
                    }

                    // Is varCheck's left or right border out of bounds?  Negative overlap means yes.
                    // We're testing for nested variables here so include cluster margin at the relevant border.
                    double leftOverlap = clusDef.Left - clusDef.ParentClusterDef.Left
                                         - OverlapRemovalCluster.CalcBorderWidth(clusDef.ParentClusterDef.LeftBorderInfo.InnerMargin)
                                         - this.MinPaddingX;
                    double rightOverlap = clusDef.ParentClusterDef.Right - clusDef.Right
                                          - OverlapRemovalCluster.CalcBorderWidth(clusDef.ParentClusterDef.RightBorderInfo.InnerMargin)
                                          - this.MinPaddingX;
                    double topOverlap = clusDef.Top - clusDef.ParentClusterDef.Top
                                        - OverlapRemovalCluster.CalcBorderWidth(clusDef.ParentClusterDef.TopBorderInfo.InnerMargin)
                                        - this.MinPaddingY;
                    double bottomOverlap = clusDef.ParentClusterDef.Bottom - clusDef.Bottom
                                           - OverlapRemovalCluster.CalcBorderWidth(clusDef.ParentClusterDef.BottomBorderInfo.InnerMargin)
                                           - this.MinPaddingY;
                    if ((leftOverlap < -epsilon)
                        || (rightOverlap < -epsilon)
                        || (topOverlap < -epsilon)
                        || (bottomOverlap < -epsilon))
                    {
                        // Uh oh.
                        this.WriteLine("Error {0}: Cluster '{1}' is outside ParentCluster '{2}' bounds", FailTag("ClusParentClus"),
                            clusDef.ClusterId, clusDef.ParentClusterDef.ClusterId);
                        this.WriteLine("   Cluster {0}: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}", clusDef.ClusterId,
                            clusDef.Left, clusDef.Right, clusDef.Top, clusDef.Bottom);
                        this.WriteLine("   Parent  {0}: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}", clusDef.ParentClusterDef.ClusterId,
                            clusDef.ParentClusterDef.Left, clusDef.ParentClusterDef.Right,
                            clusDef.ParentClusterDef.Top, clusDef.ParentClusterDef.Bottom);
                        this.WriteLine("   Overlap  : L/R T/B {0:F5}/{1:F5} {2:F5}/{3:F5}",
                            leftOverlap, rightOverlap, topOverlap, bottomOverlap);
                        succeeded = false;
                    }
                } // endif null != (object)varCheck.ParentClusterDef
            }
        }

        private void VerifyClustersAreTight(IEnumerable<ClusterDef> iterClusterDefs, double epsilon, ref bool succeeded)
        {
                // Verify that clusters have at least one node or cluster immediately adjacent to
                // the cluster borders (i.e. verify the clusters are tight).
            foreach (ClusterDef clusDef in iterClusterDefs)
            {
                // Empty clusters have nothing to verify
                if (clusDef.IsEmpty)
                {
                    continue;
                }

                // Clusters at the root of a hierarchy have no borders.
                if (clusDef.IsNewHierarchy)
                {
                    continue;
                }

                double minLeft = double.MaxValue;
                double maxRight = double.MinValue;
                double minTop = double.MaxValue;
                double maxBottom = double.MinValue;

                foreach (VariableDef varChild in clusDef.Variables)
                {
                    minLeft = Math.Min(minLeft, varChild.Left);
                    maxRight = Math.Max(maxRight, varChild.Right);
                    minTop = Math.Min(minTop, varChild.Top);
                    maxBottom = Math.Max(maxBottom, varChild.Bottom);
                }

                foreach (ClusterDef clusChild in clusDef.Clusters)
                {
                    if (clusChild.IsEmpty)
                    {
                        continue;
                    }
                    minLeft = Math.Min(minLeft, clusChild.Left);
                    maxRight = Math.Max(maxRight, clusChild.Right);
                    minTop = Math.Min(minTop, clusChild.Top);
                    maxBottom = Math.Max(maxBottom, clusChild.Bottom);
                }

                // Are the cluster's borders tight?  Too big a positive gap means yes.
                // We're testing for children here so include cluster margin at the relevant border.
                double leftGap = minLeft - clusDef.Left
                                 - OverlapRemovalCluster.CalcBorderWidth(clusDef.LeftBorderInfo.InnerMargin)
                                 - this.MinPaddingX;
                double rightGap = clusDef.Right - maxRight
                                  - OverlapRemovalCluster.CalcBorderWidth(clusDef.RightBorderInfo.InnerMargin)
                                  - this.MinPaddingX;
                double topGap = minTop - clusDef.Top
                                - OverlapRemovalCluster.CalcBorderWidth(clusDef.TopBorderInfo.InnerMargin)
                                - this.MinPaddingY;
                double bottomGap = clusDef.Bottom - maxBottom
                                   - OverlapRemovalCluster.CalcBorderWidth(clusDef.BottomBorderInfo.InnerMargin)
                                   - this.MinPaddingY;

                // This is OK if the cluster is at its min size; assume it had to grow to meet the min size.
                bool badXgap = !clusDef.IsMinimumSizeX
                               && (((leftGap > epsilon) && !clusDef.LeftBorderInfo.IsFixedPosition)
                                   || ((rightGap > epsilon) && !clusDef.RightBorderInfo.IsFixedPosition));
                bool badYgap = !clusDef.IsMinimumSizeY
                               && (((topGap > epsilon) && !clusDef.TopBorderInfo.IsFixedPosition)
                                   || ((bottomGap > epsilon) && !clusDef.BottomBorderInfo.IsFixedPosition));
                if (badXgap || badYgap)
                {
                    // Uh oh.
                    this.WriteLine("Error {0}: Cluster '{1}' border is not tight (within {2})", FailTag("ClusTightBorder"),
                        clusDef.ClusterId, epsilon);
                    this.WriteLine("   Cluster {0}: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}", clusDef.ClusterId,
                        clusDef.Left, clusDef.Right, clusDef.Top, clusDef.Bottom);
                    this.WriteLine("   Gaps     : L/R T/B {0}/{1} {2}/{3}",
                        clusDef.LeftBorderInfo.IsFixedPosition ? "fixed" : string.Format("{0:F5}", leftGap),
                        clusDef.RightBorderInfo.IsFixedPosition ? "fixed" : string.Format("{0:F5}", rightGap),
                        clusDef.TopBorderInfo.IsFixedPosition ? "fixed" : string.Format("{0:F5}", topGap),
                        clusDef.BottomBorderInfo.IsFixedPosition ? "fixed" : string.Format("{0:F5}", bottomGap));
                    succeeded = false;
                }
            }
        }

        private void VerifyEachHierarchy(Dictionary<int, KeyValuePair<List<ClusterDef>, List<VariableDef>>> dictHierarchies, double epsilon, ref bool succeeded)
        {
            foreach (KeyValuePair<List<ClusterDef>, List<VariableDef>> kvpCurHier in dictHierarchies.Values)
            {
                // Verify that clusters do not overlap with non-parent clusters within their own hierarchy.
                ClusterDef[] localClusDefs;
                this.VerifyClustersDoNotOverlapWithNonParentClustersInTheirOwnHierarchy(epsilon, out localClusDefs, kvpCurHier, ref succeeded);
                this.VerifyClustersDoNotOverlapWithNonChildNodesInTheirOwnHierarchy(epsilon, kvpCurHier, localClusDefs, ref succeeded);
            }
        }

        private void VerifyClustersDoNotOverlapWithNonParentClustersInTheirOwnHierarchy(double epsilon, out ClusterDef[] localClusDefs, KeyValuePair<List<ClusterDef>, List<VariableDef>> kvpCurHier, ref bool succeeded)
        {
            localClusDefs = kvpCurHier.Key.OrderBy(clusDef => clusDef.Top).ToArray();
            for (int ii = 0; ii < localClusDefs.Length; ++ii)
            {
                ClusterDef clusCur = localClusDefs[ii];
                if (clusCur.IsEmpty || clusCur.IsNewHierarchy)
                {
                    continue;
                }
                for (int jj = ii + 1; jj < localClusDefs.Length; ++jj)
                {
                    ClusterDef clusCheck = localClusDefs[jj];
                    if (clusCheck.IsEmpty || clusCheck.IsNewHierarchy)
                    {
                        continue;
                    }

                    // Rounding error may leave these calculations slightly greater or less than zero.
                    // Since margin is calculated only for inner edges and here we are testing for
                    // sibling rather than nested nodes, we don't use margin here.
                    // Name is <relativeToVarCur><RelativeToVarCheck>
                    double bottomTopOverlap = clusCheck.Top - clusCur.Bottom - this.MinPaddingY;
                    if (bottomTopOverlap >= -epsilon)
                    {
                        // Out of range of clusCur's size, so we're done with clusCur.
                        break;
                    }

                    // Does clusCheck's left or right border overlap?  Negative overlap means yes.
                    // Again, margins are only cluster-internal and we're testing external boundaries
                    // here; so the cluster size should have been calculated large enough and we only
                    // look at padding.
                    double xa = clusCheck.Left - clusCur.Right - this.MinPaddingX;
                    double xb = clusCur.Left - clusCheck.Right - this.MinPaddingX;

                    if ((xa < -epsilon) && (xb < -epsilon))
                    {
                        // Let's see if it's a parent.
                        bool hasSideOverlap = true;
                        for (ClusterDef clusDefParent = clusCheck.ParentClusterDef; null != clusDefParent; clusDefParent = clusDefParent.ParentClusterDef)
                        {
                            if (clusDefParent == clusCur)
                            {
                                hasSideOverlap = false;
                                break;
                            }
                        }

                        // Note: This test may fail if clusCheck is a parent of clusCur, but in that case
                        // clusCheck should be outside clusCur - which means we had another error before this,
                        // that cluster {clusCheck} is outside the bounds of parent cluster {clusCur}.
                        if (hasSideOverlap)
                        {
                            // Uh oh.
                            this.WriteLine("Error {0}: Overlap exists between sibling Clusters '{1}' and '{2}'", FailTag("OlapSibClus"),
                                clusCur.ClusterId, clusCheck.ClusterId);
                            this.WriteLine("   Cluster {0}: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}", clusCur.ClusterId,
                                clusCur.Left, clusCur.Right, clusCur.Top, clusCur.Bottom);
                            this.WriteLine("   Cluster {0}: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}", clusCheck.ClusterId,
                                clusCheck.Left, clusCheck.Right, clusCheck.Top, clusCheck.Bottom);
                            succeeded = false;
                        }
                    } // endif overlap within epsilon
                } // endfor localClusDefs[jj]
            }
        }

        private void VerifyClustersDoNotOverlapWithNonChildNodesInTheirOwnHierarchy(double epsilon, KeyValuePair<List<ClusterDef>, List<VariableDef>> kvpCurHier, ClusterDef[] localClusDefs, ref bool succeeded)
        {
            int idxStartVar = 0;
            foreach (ClusterDef clusCur in localClusDefs)
            {
                if (clusCur.IsEmpty)
                {
                    continue;
                }

                VariableDef[] localVarDefs = kvpCurHier.Value.OrderBy(varDef => varDef.Top).ToArray();
                for (int jj = idxStartVar; jj < localVarDefs.Length; ++jj)
                {
                    VariableDef varCheck = localVarDefs[jj];

                    // Minimize variable-list traversal.
                    if (varCheck.Top < (clusCur.Top - epsilon))
                    {
                        idxStartVar = jj;
                    }

                    // If the variable ends before the cluster starts, there's no overlap.
                    if ((clusCur.Top - varCheck.Bottom - this.MinPaddingY) > -epsilon)
                    {
                        continue;
                    }

                    // Rounding error may leave these calculations slightly greater or less than zero.
                    // Since margin is calculated only for inner edges and here we are testing for
                    // sibling rather than nested nodes, we don't use margin here.
                    // Name is <relativeToVarCur><RelativeToVarCheck>
                    double bottomTopOverlap = varCheck.Top - clusCur.Bottom - this.MinPaddingY;
                    if (bottomTopOverlap >= -epsilon)
                    {
                        // Out of range of clusCur's size, so we're done with clusCur.
                        break;
                    }

                    // Does varCheck's left or right border overlap?  Negative overlap means yes.
                    // Again, margins are only cluster-internal and we're testing external boundaries
                    // here; so the cluster size should have been calculated large enough and we only
                    // look at padding.
                    double xa = varCheck.Left - clusCur.Right - this.MinPaddingX;
                    double xb = clusCur.Left - varCheck.Right - this.MinPaddingX;

                    if ((xa < -epsilon) && (xb < -epsilon))
                    {
                        // Let's see if it's an ancestor.
                        bool hasSideOverlap = true;
                        foreach (ClusterDef clusDefParent in varCheck.ParentClusters)
                        {
                            for (ClusterDef clusDefAncestor = clusDefParent; null != clusDefAncestor; clusDefAncestor = clusDefAncestor.ParentClusterDef)
                            {
                                if (clusDefAncestor == clusCur)
                                {
                                    hasSideOverlap = false;
                                    break;
                                }
                            }
                            if (!hasSideOverlap)
                            {
                                break;
                            }
                        }

                        if (hasSideOverlap)
                        {
                            // Uh oh.
                            this.WriteLine("Error {0}: Overlap exists between Cluster '{1}' and non-child Node '{2}'", FailTag("OlapClusNode"),
                                clusCur.ClusterId, varCheck.IdString);
                            this.WriteLine("   Cluster {0}: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}", clusCur.ClusterId,
                                clusCur.Left, clusCur.Right, clusCur.Top, clusCur.Bottom);
                            this.WriteLine("      Node {0}: L/R T/B {1:F5}/{2:F5} {3:F5}/{4:F5}", varCheck.IdString,
                                varCheck.Left, varCheck.Right, varCheck.Top, varCheck.Bottom);
                            succeeded = false;
                        }
                    } // endif overlap within epsilon
                } // endfor each non-child variable
            }
        }

        // Worker to ensure we set values from loaded testfiles.

        internal TestFileReader LoadTestDataFile(string strFileName)
        {
            var tdf = new TestFileReader(/*isTwoDimensional:*/ true);
            tdf.Load(strFileName);
            MinPaddingX = tdf.PaddingX;
            MinPaddingY = tdf.PaddingY;
            GoalFunctionValueX = tdf.GoalX;
            GoalFunctionValueY = tdf.GoalY;
            return tdf;
        }
    }
}
