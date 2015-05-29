// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjFileWriter.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

using Microsoft.Msagl.Core.ProjectionSolver;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    internal static class ProjFileWriter
    {
        internal static void WriteFile(int seed,
                        double maxGenWeight,
                        double maxGenScale,
                        Solution solution,
                        List<VariableDef> lstVarDefs, 
                        List<ConstraintDef> lstCstDefs, 
                        List<NeighborDef> lstNbourDefs, 
                        StreamWriter outputFileWriter, 
                        int violationCount)
        {
            // TODO_cleanup: make shared strings; regenerate test files to verify

            // Add the summary information as comments. @@ allows findstr etc. scans of the file metadata.
            outputFileWriter.WriteLine("// @@Variables: {0}", lstVarDefs.Count);
            outputFileWriter.WriteLine("// @@Constraints: {0}", lstCstDefs.Count);
            outputFileWriter.WriteLine("// @@Neighbours: {0}", lstNbourDefs.Count);
            outputFileWriter.WriteLine();

            // Values we want to read back in.
            outputFileWriter.WriteLine("Seed 0x{0}", seed.ToString("X"));
            outputFileWriter.WriteLine("Weight {0:F5}", maxGenWeight);
            outputFileWriter.WriteLine("Scale {0:F5}", maxGenScale);
            outputFileWriter.WriteLine("Goal {0}", solution.GoalFunctionValue);
            if (0 != violationCount)
            {
                outputFileWriter.WriteLine("UnsatisfiableConstraints {0}", violationCount);
            }
            outputFileWriter.WriteLine();

            outputFileWriter.WriteLine(TestFileStrings.BeginVariables);
            for (int idxVar = 0; idxVar < lstVarDefs.Count; ++idxVar)
            {
                VariableDef varDef = lstVarDefs[idxVar];
                Validate.IsTrue(varDef.WeightX >= 0.01, "varDef.WeightX is less than expected");
                outputFileWriter.WriteLine(
                    TestFileStrings.WriteVariable1D, idxVar, varDef.DesiredPosX, varDef.SizeX, varDef.WeightX, varDef.ScaleX);
            }
            outputFileWriter.WriteLine(TestFileStrings.EndVariables);
            outputFileWriter.WriteLine();
            outputFileWriter.Flush();

            outputFileWriter.WriteLine(TestFileStrings.BeginConstraints);
            foreach (ConstraintDef cstDef in lstCstDefs)
            {
                outputFileWriter.WriteLine(
                    TestFileStrings.WriteConstraint,
                    cstDef.LeftVariableDef.IdString,
                    cstDef.RightVariableDef.IdString,
                    cstDef.IsEquality ? "=" : string.Empty,
                    cstDef.Gap);
            }
            outputFileWriter.WriteLine(TestFileStrings.EndConstraints);
            outputFileWriter.WriteLine();
            outputFileWriter.Flush();

            if (lstNbourDefs.Count > 0)
            {
                outputFileWriter.WriteLine(TestFileStrings.BeginNeighbours);
                foreach (NeighborDef nbourDef in lstNbourDefs)
                {
                    outputFileWriter.WriteLine(
                        TestFileStrings.WriteNeighbour,
                        nbourDef.LeftVariableDef.IdString,
                        nbourDef.RightVariableDef.IdString,
                        nbourDef.Weight);
                }
                outputFileWriter.WriteLine(TestFileStrings.EndNeighbours);
                outputFileWriter.WriteLine();
                outputFileWriter.Flush();
            }

            // Now write the expected output - it won't be correct if an exception was thrown
            // but at least we printed a message to the screen and file.
            outputFileWriter.WriteLine(TestFileStrings.BeginResults);
            foreach (VariableDef varDef in lstVarDefs)
            {
                outputFileWriter.WriteLine(TestFileStrings.WriteResults1D, varDef.IdString, varDef.VariableX.ActualPos);
            }
            outputFileWriter.WriteLine(TestFileStrings.EndResults);

            // Done.
            outputFileWriter.Flush();
            outputFileWriter.Close();
        }
    }
}
