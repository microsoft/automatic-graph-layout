// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OlapFileWriter.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.ProjectionSolver;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    internal static class OlapFileWriter
    {
        internal static void WriteFile(int seed,
                        double maxWeightToGenerate,
                        Solver solverX,
                        Solver solverY,
                        Solution solutionX,
                        Solution solutionY,
                        double minPaddingX,
                        double minPaddingY,
                        double minClusterSizeX,
                        double minClusterSizeY,
                        double maxMargin,
                        List<VariableDef> lstVarDefs,
                        List<ClusterDef> lstClusDefs,
                        StreamWriter outputFileWriter)
        {
            // TODO_cleanup: make shared strings; regenerate test files to verify

            // Add the summary information as comments. @@ (high-level) and @# (low-level) allow
            // findstr etc. scans of the file metadata; @[@#] gets both.
            outputFileWriter.WriteLine("// @@Variables: {0}", lstVarDefs.Count);
            outputFileWriter.WriteLine("// @@Clusters: {0}", (null == lstClusDefs) ? 0 : lstClusDefs.Count);
            outputFileWriter.WriteLine("// @@Constraints_X: {0}", solverX.Constraints.Count());
            outputFileWriter.WriteLine("// @@Constraints_Y: {0}", solverY.Constraints.Count());
            outputFileWriter.WriteLine();

            // Values we want to read back in.
            outputFileWriter.WriteLine("Seed 0x{0}", seed.ToString("X"));
            outputFileWriter.WriteLine("Weight {0:F5}", maxWeightToGenerate);
            outputFileWriter.WriteLine("Padding {0:F5} {1:F5}", minPaddingX, minPaddingY);
            outputFileWriter.WriteLine("MinClusterSize {0:F5} {1:F5}", minClusterSizeX, minClusterSizeY);
            outputFileWriter.WriteLine("Margin {0}", maxMargin);
            outputFileWriter.WriteLine("Goal {0} {1}", solutionX.GoalFunctionValue, solutionY.GoalFunctionValue);
            outputFileWriter.WriteLine();

            outputFileWriter.WriteLine(TestFileStrings.BeginVariables);
            for (int idxVar = 0; idxVar < lstVarDefs.Count; ++idxVar)
            {
                VariableDef varDef = lstVarDefs[idxVar];
                outputFileWriter.WriteLine(
                    TestFileStrings.WriteVariable2D,
                    idxVar,
                    varDef.DesiredPosX,
                    varDef.DesiredPosY,
                    varDef.SizeX,
                    varDef.SizeY,
                    varDef.WeightX,
                    varDef.WeightY);
            }
            outputFileWriter.WriteLine(TestFileStrings.EndVariables);
            outputFileWriter.WriteLine();
            outputFileWriter.Flush();

            if (null != lstClusDefs)
            {
                // Write out the clusters, starting at 1 to skip the root.  Since we populate the
                // clusterdefs left-to-right we'll always print out the parents before the children.
                foreach (ClusterDef clusDef in lstClusDefs)
                {
                    outputFileWriter.WriteLine(TestFileStrings.BeginCluster);

                    // Write the current cluster definition.
                    outputFileWriter.WriteLine(TestFileStrings.WriteClusterId, clusDef.ClusterId);
                    outputFileWriter.WriteLine(
                        TestFileStrings.WriteClusterParent,
                        null == clusDef.ParentClusterDef ? 0 : clusDef.ParentClusterDef.ClusterId);
                    outputFileWriter.WriteLine(
                        TestFileStrings.WriteClusterMinSize, clusDef.MinimumSizeX, clusDef.MinimumSizeY);
                    if (clusDef.IsNewHierarchy)
                    {
                        outputFileWriter.WriteLine("NewHierarchy");
                    }
                    outputFileWriter.WriteLine(
                        TestFileStrings.WriteClusterBorder,
                        "Left",
                        clusDef.LeftBorderInfo.InnerMargin,
                        ClusterDef.IsFixedString(clusDef.LeftBorderInfo),
                        clusDef.LeftBorderInfo.Weight);
                    outputFileWriter.WriteLine(
                        TestFileStrings.WriteClusterBorder,
                        "Right",
                        clusDef.RightBorderInfo.InnerMargin,
                        ClusterDef.IsFixedString(clusDef.RightBorderInfo),
                        clusDef.RightBorderInfo.Weight);
                    outputFileWriter.WriteLine(
                        TestFileStrings.WriteClusterBorder,
                        "Top",
                        clusDef.TopBorderInfo.InnerMargin,
                        ClusterDef.IsFixedString(clusDef.TopBorderInfo),
                        clusDef.TopBorderInfo.Weight);
                    outputFileWriter.WriteLine(
                        TestFileStrings.WriteClusterBorder,
                        "Bottom",
                        clusDef.BottomBorderInfo.InnerMargin,
                        ClusterDef.IsFixedString(clusDef.BottomBorderInfo),
                        clusDef.BottomBorderInfo.Weight);
                    outputFileWriter.WriteLine("// @#ClusterVars: {0}", clusDef.Variables.Count);
                    foreach (VariableDef varDef in clusDef.Variables)
                    {
                        outputFileWriter.WriteLine(TestFileStrings.WriteClusterVariable, varDef.IdString);
                    }
                    outputFileWriter.WriteLine(TestFileStrings.EndCluster);
                    outputFileWriter.WriteLine();
                }
                outputFileWriter.Flush();
            } // endif clusters exist

            // Write the constraints.
            // TODOclus: This is outputting vars Lnn Rnn in TEST_MSAGL and an empty string in RELEASE; consider making the file
            //           output (with clusters, anyway) run in TEST_MSAGL-only and have TestFileReader.cs know how to decode them.
            outputFileWriter.WriteLine(TestFileStrings.BeginConstraintsX);
            foreach (Constraint cst in solverX.Constraints.OrderBy(cst => cst))
            {
                // There are no equality constraints in OverlapRemoval so pass an empty string.
                outputFileWriter.WriteLine(
                    TestFileStrings.WriteConstraint,
                    ((OverlapRemovalNode)cst.Left.UserData).UserData,
                    ((OverlapRemovalNode)cst.Right.UserData).UserData,
                    string.Empty,
                    cst.Gap);
            }
            outputFileWriter.WriteLine(TestFileStrings.EndConstraints);
            outputFileWriter.WriteLine();
            outputFileWriter.WriteLine(TestFileStrings.BeginConstraintsY);
            foreach (Constraint cst in solverY.Constraints.OrderBy(cst => cst))
            {
                // There are no equality constraints in OverlapRemoval so pass an empty string.
                outputFileWriter.WriteLine(
                    TestFileStrings.WriteConstraint,
                    ((OverlapRemovalNode)cst.Left.UserData).UserData,
                    ((OverlapRemovalNode)cst.Right.UserData).UserData,
                    string.Empty,
                    cst.Gap);
            }
            outputFileWriter.WriteLine(TestFileStrings.EndConstraints);
            outputFileWriter.WriteLine();

            // Now write the results.
            outputFileWriter.WriteLine(TestFileStrings.BeginResults);
            foreach (VariableDef varDef in lstVarDefs)
            {
                outputFileWriter.WriteLine(TestFileStrings.WriteResults2D, varDef.IdString, varDef.VariableX.ActualPos, varDef.VariableY.ActualPos);
            } // endforeach varDef
            outputFileWriter.WriteLine(TestFileStrings.EndResults);

            if (null != lstClusDefs)
            {
                outputFileWriter.WriteLine();
                outputFileWriter.WriteLine(TestFileStrings.BeginClusterResults);
                outputFileWriter.WriteLine("// (includes only clusters that are not IsNewHierarchy)");
                foreach (ClusterDef clusDef in lstClusDefs)
                {
                    // Clusters at the root of a hierarchy have no borders.
                    if (!clusDef.IsNewHierarchy)
                    {
                        outputFileWriter.WriteLine(
                            TestFileStrings.WriteClusterResults,
                            clusDef.ClusterId,
                            clusDef.Left,
                            clusDef.Right,
                            clusDef.Top,
                            clusDef.Bottom);
                    }
                }
                outputFileWriter.WriteLine(TestFileStrings.EndClusterResults);
            }

            // Done.
            outputFileWriter.Flush();
            outputFileWriter.Close();
        }

        // end WriteTestFile()
    }
}
