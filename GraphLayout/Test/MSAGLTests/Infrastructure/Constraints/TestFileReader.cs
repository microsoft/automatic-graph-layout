// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestFileReader.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    internal class TestFileReader
    {
        // The variables and expected results handle both one and two dimensions, but we need to separate the 
        // horizontal and vertical constraints.
        public int Seed { get; private set; }
        public double Weight { get; private set; }
        public double Scale { get; private set; }
        public double PaddingX { get; private set; }
        public double PaddingY { get; private set; }
        public double MinClusterSizeX { get; private set; }
        public double MinClusterSizeY { get; private set; }
        public int Margin { get; private set; }
        public double GoalX { get; private set; }
        public double GoalY { get; private set; }
        public List<VariableDef> VariableDefs { get; private set; }
        public List<ClusterDef> ClusterDefs { get; private set; }
        public List<ConstraintDef> ConstraintDefsX { get; private set; }
        public List<ConstraintDef> ConstraintsDefY { get; private set; }
        public List<NeighborDef> NeighborDefs { get; private set; }
        public bool HasEqualityConstraints { get; private set; }
        public int UnsatisfiableConstraintCount { get; private set; }

        // Indicates if we are loading two-dimensional Variables.
        private readonly bool isTwoDimensional;

        // For loading cluster results.
        private int lastClusterDefIndex = -1;

        // State of clusters, and the current cluster being created.
        private enum EClusterState
        {
            Id,
            Parent,
            LeftBorder,
            RightBorder,
            TopBorder,
            BottomBorder,
            Variable
        }
        private ClusterDef currentClusterDef;

        // This will be set depending on whether we are doing horizontal or vertical constraints.
        private List<ConstraintDef> currentConstraintDefs;

        private enum ESection
        {
            PreVariables,
            Variables,
            PreClusterOrConstraints,
            Cluster,
            Constraints,
            PreNeighboursOrResults,
            Neighbours,
            PreResults,
            Results,
            PreClusterResults,
            ClusterResults,
            Done
        }

        public TestFileReader(bool is2D)
        {
            this.Margin = 0;
            this.Weight = 0.0;
            this.Seed = 0;
            this.isTwoDimensional = is2D;

            GoalX = double.NaN;
            GoalY = double.NaN;
            VariableDefs = new List<VariableDef>();
            ClusterDefs = new List<ClusterDef>();
            ConstraintDefsX = new List<ConstraintDef>();
            ConstraintsDefY = new List<ConstraintDef>();
            this.NeighborDefs = new List<NeighborDef>();
        }

        // Some variables are instantiated after we see the section header.
        // ReSharper disable PossibleNullReferenceException
        public void Load(string strFullName)
        {
            this.VariableDefs.Clear();
            this.ConstraintDefsX.Clear();
            this.ConstraintsDefY.Clear();

            using (var sr = new StreamReader(strFullName))
            {
                string currentLine;
                ESection currentSection = ESection.PreVariables;
                EClusterState currentClusterState = EClusterState.Id;
                int lineNumber = 0;
                while ((currentLine = sr.ReadLine()) != null)
                {
                    ++lineNumber;
                    if (string.IsNullOrEmpty(currentLine) || currentLine.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (ESection.Done == currentSection)
                    {
                        break;
                    }
                    
                    switch (currentSection)
                    {
                        case ESection.PreVariables:
                            // Some stuff gets in at the top before variables.
                            currentSection = ProcessPreVariables(lineNumber, currentLine, currentSection);
                            break;

                        case ESection.Variables:
                            currentSection = ProcessVariables(lineNumber, currentLine, currentSection);
                            break;

                        case ESection.PreClusterOrConstraints:
                            currentSection = ProcessClusterOrConstraints(currentLine, currentSection, ref currentClusterState);
                            break;
                            
                        case ESection.Cluster:
                            currentSection = ProcessCluster(lineNumber, currentLine, currentSection, ref currentClusterState);
                            break;

                        case ESection.Constraints:
                            currentSection = ProcessConstraints(lineNumber, currentLine, currentSection);
                            break;

                        case ESection.PreNeighboursOrResults:
                            currentSection = ProcessNeighboursOrResults(currentLine, currentSection);
                            break;

                        case ESection.Neighbours:
                            currentSection = ProcessNeighbours(lineNumber, currentLine, currentSection);
                            break;

                        case ESection.PreResults:
                            currentSection = ProcessPreResults(currentLine, currentSection);
                            break;

                        case ESection.Results:
                            currentSection = ProcessResults(lineNumber, currentLine, currentSection);
                            break;

                        case ESection.PreClusterResults:
                            currentSection = ProcessPreClusterResults(currentLine, currentSection);
                            break;

                        case ESection.ClusterResults:
                            currentSection = ProcessClusterResults(lineNumber, currentLine, currentSection);
                            break;

                        default:
                            Validate.Fail("Unknown section");
                            break;
                    }
                } // endwhile sr.ReadLine
            } // end using sr

            if (0 == this.VariableDefs.Count)
            {
                Validate.Fail("No VARIABLEs found in file");
            }
        }

        private ESection ProcessPreVariables(int lineNumber, string currentLine, ESection currentSection)
        {
            Match m = TestFileStrings.ParseSeed.Match(currentLine);
            if (m.Success)
            {
                string strArg = m.Groups["seed"].ToString();
                System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Integer;
                if (strArg.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    // For some reason the 0x prefix is not allowed for hex strings.
                    strArg = strArg.Substring(2);
                    style = System.Globalization.NumberStyles.HexNumber;
                }
                this.Seed = int.Parse(strArg, style);
                return currentSection;
            }

            m = TestFileStrings.ParseWeight.Match(currentLine);
            if (m.Success)
            {
                this.Weight = double.Parse(m.Groups["weight"].ToString());
                return currentSection;
            }

            // Scale is optional.
            m = TestFileStrings.ParseScale.Match(currentLine);
            if (m.Success)
            {
                this.Scale = double.Parse(m.Groups["scale"].ToString());
                return currentSection;
            }

            m = TestFileStrings.ParsePadding.Match(currentLine);
            if (m.Success)
            {
                this.PaddingX = double.Parse(m.Groups["X"].ToString());
                this.PaddingY = double.Parse(m.Groups["Y"].ToString());
                return currentSection;
            }

            // Currently not actually used; the individual clusters record the random values
            // based upon this.
            m = TestFileStrings.ParseMinClusterSize.Match(currentLine);
            if (m.Success)
            {
                this.MinClusterSizeX = double.Parse(m.Groups["X"].ToString());
                this.MinClusterSizeY = double.Parse(m.Groups["Y"].ToString());
                return currentSection;
            }

            m = TestFileStrings.ParseMargin.Match(currentLine);
            if (m.Success)
            {
                this.Margin = int.Parse(m.Groups["margin"].ToString());
                return currentSection;
            }

            m = TestFileStrings.ParseUnsatisfiableConstraints.Match(currentLine);
            if (m.Success)
            {
                this.UnsatisfiableConstraintCount = int.Parse(m.Groups["count"].ToString());
                return currentSection;
            }

            m = this.isTwoDimensional
                    ? TestFileStrings.ParseGoal2D.Match(currentLine)
                    : TestFileStrings.ParseGoal1D.Match(currentLine);
            if (m.Success)
            {
                this.GoalX = double.Parse(m.Groups["goalx"].ToString());
                if (this.isTwoDimensional)
                {
                    this.GoalY = double.Parse(m.Groups["goaly"].ToString());
                }
                return currentSection;
            }

            if (currentLine.StartsWith(TestFileStrings.BeginVariables, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.Variables;
                return currentSection;
            }
            Validate.Fail(string.Format("Unknown header line {0}: {1}", lineNumber, currentLine));
            return currentSection;
        }

        private ESection ProcessVariables(int lineNumber, string currentLine, ESection currentSection)
        {
            if (currentLine.StartsWith(TestFileStrings.EndVariables, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.PreClusterOrConstraints;
                return currentSection;
            }
            Match m;
            if (this.isTwoDimensional)
            {
                m = TestFileStrings.ParseVariable2D.Match(currentLine);
                if (m.Success)
                {
                    this.VariableDefs.Add(new VariableDef(
                            uint.Parse(m.Groups["ord"].ToString()),
                            double.Parse(m.Groups["posX"].ToString()),
                            double.Parse(m.Groups["posY"].ToString()),
                            double.Parse(m.Groups["sizeX"].ToString()),
                            double.Parse(m.Groups["sizeY"].ToString()),
                            double.Parse(m.Groups["weightX"].ToString()),
                            double.Parse(m.Groups["weightY"].ToString())));
                }
            }
            else
            {
                m = TestFileStrings.ParseVariable1D.Match(currentLine);
                double scale = 1.0;
                if (!m.Success)
                {
                    m = TestFileStrings.ParseVariable1DScale.Match(currentLine);
                    scale = m.Success ? double.Parse(m.Groups["scale"].ToString()) : scale;
                }
                if (m.Success)
                {
                    var varDef = new VariableDef(
                            uint.Parse(m.Groups["ord"].ToString()),
                            double.Parse(m.Groups["pos"].ToString()),
                            double.Parse(m.Groups["size"].ToString()),
                            double.Parse(m.Groups["weight"].ToString()))
                                {
                                    ScaleX = scale 
                                };
                    this.VariableDefs.Add(varDef);
                }
            }
            if (!m.Success)
            {
                Validate.Fail(string.Format("Unparsable VARIABLE line {0}: {1}", lineNumber, currentLine));
            }

            // Verify the variables in the file are sorted.  This makes it easier for the results
            // reading to be in sync.
            Validate.AreEqual(this.VariableDefs[this.VariableDefs.Count - 1].Ordinal, (uint)(this.VariableDefs.Count - 1),
                "Out of order VARIABLE ordinal");
            return currentSection;
        }

        private ESection ProcessClusterOrConstraints(string currentLine, ESection currentSection, ref EClusterState currentClusterState)
        {
            if (currentLine.StartsWith(
                TestFileStrings.BeginCluster, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.Cluster;
                currentClusterState = EClusterState.Id;
                this.currentClusterDef = new ClusterDef(this.MinClusterSizeX, this.MinClusterSizeY);
                return currentSection;
            }
            if (currentLine.StartsWith(
                TestFileStrings.BeginConstraintsX, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.Constraints;
                this.currentConstraintDefs = this.ConstraintDefsX;
                return currentSection;
            }
            if (currentLine.StartsWith(
                TestFileStrings.BeginConstraintsY, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.Constraints;
                this.currentConstraintDefs = this.ConstraintsDefY;
                return currentSection;
            }
            if (currentLine.StartsWith(
                TestFileStrings.BeginConstraints, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.Constraints;
                this.currentConstraintDefs = this.ConstraintDefsX;
                return currentSection;
            }
            return currentSection;
        }

        private ESection ProcessCluster(int lineNumber, string currentLine, ESection currentSection, ref EClusterState currentClusterState)
        {
            if (currentLine.StartsWith(
                TestFileStrings.EndCluster, StringComparison.OrdinalIgnoreCase))
            {
                if (EClusterState.Variable != currentClusterState)
                {
                    Validate.Fail(string.Format("Unexpected END CLUSTER line {0}: {1}", lineNumber, currentLine));
                }
                currentSection = ESection.PreClusterOrConstraints;
                this.ClusterDefs.Add(this.currentClusterDef);
                this.currentClusterDef = null;
                return currentSection;
            }
            if (EClusterState.Id == currentClusterState)
            {
                Match m = TestFileStrings.ParseClusterId.Match(currentLine);
                if (m.Success)
                {
                    // Verify the Clusters in the file are sorted on ID.  This makes it easier for the results
                    // reading to be in sync, as we'll index ClusterDefs by [Parent - 1].
                    var id = int.Parse(m.Groups["id"].ToString());
                    Validate.IsTrue(this.currentClusterDef.ClusterId == id, "Out of order CLUSTER id");
                }
                else
                {
                    Validate.Fail(string.Format("Unparsable CLUSTER ID line {0}: {1}", lineNumber, currentLine));
                }
                currentClusterState = EClusterState.Parent;
            }
            else if (EClusterState.Parent == currentClusterState)
            {
                Match m = TestFileStrings.ParseClusterParent.Match(currentLine);
                if (m.Success)
                {
                    int parentId = int.Parse(m.Groups["parent"].ToString());

                    // Cluster IDs are 1-based because we use 0 for the "root cluster".
                    if (0 != parentId)
                    {
                        ClusterDef clusParent = this.ClusterDefs[parentId - 1];
                        Validate.AreEqual(clusParent.ClusterId, parentId, "clusParent.ClusterId mismatch with idParent");
                        clusParent.AddClusterDef(this.currentClusterDef);
                    }
                }
                else
                {
                    Validate.Fail(string.Format("Unparsable CLUSTER Parent line {0}: {1}", lineNumber, currentLine));
                }
                currentClusterState = EClusterState.LeftBorder;
            }
            else if (EClusterState.LeftBorder == currentClusterState)
            {
                // Older files didn't have MinSize.
                Match m = TestFileStrings.ParseClusterMinSize.Match(currentLine);
                if (m.Success)
                {
                    this.currentClusterDef.MinimumSizeX = double.Parse(m.Groups["X"].ToString());
                    this.currentClusterDef.MinimumSizeY = double.Parse(m.Groups["Y"].ToString());
                    return currentSection;
                }
                if (0 == string.Compare("NewHierarchy", currentLine, StringComparison.OrdinalIgnoreCase))
                {
                    // NewHierarchy is optional.
                    this.currentClusterDef.IsNewHierarchy = true;
                    return currentSection;
                }
                this.currentClusterDef.LeftBorderInfo = ParseBorderInfo("Left", currentLine, lineNumber);
                currentClusterState = EClusterState.RightBorder;
            }
            else if (EClusterState.RightBorder == currentClusterState)
            {
                this.currentClusterDef.RightBorderInfo = ParseBorderInfo("Right", currentLine, lineNumber);
                currentClusterState = EClusterState.TopBorder;
            }
            else if (EClusterState.TopBorder == currentClusterState)
            {
                this.currentClusterDef.TopBorderInfo = ParseBorderInfo("Top", currentLine, lineNumber);
                currentClusterState = EClusterState.BottomBorder;
            }
            else if (EClusterState.BottomBorder == currentClusterState)
            {
                this.currentClusterDef.BottomBorderInfo = ParseBorderInfo("Bottom", currentLine, lineNumber);
                currentClusterState = EClusterState.Variable;
            }
            else if (EClusterState.Variable == currentClusterState)
            {
                Match m = TestFileStrings.ParseClusterVariable.Match(currentLine);
                if (m.Success)
                {
                    int variableId = int.Parse(m.Groups["var"].ToString());
                    this.currentClusterDef.AddVariableDef(this.VariableDefs[variableId]);
                }
                else
                {
                    Validate.Fail(string.Format("Unparsable CLUSTER Variable line {0}: {1}", lineNumber, currentLine));
                }
            }
            return currentSection;
        }

        private ESection ProcessConstraints(int lineNumber, string currentLine, ESection currentSection)
        {
            if (currentLine.StartsWith(
                TestFileStrings.EndConstraints, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.PreNeighboursOrResults;
                this.currentConstraintDefs = null;
                return currentSection;
            }

            // TODOclust: if we have clusters, then we get Lnn/Rnn (TEST_MSAGL) or blank (RELEASE) which 
            // we can't read.  Currently we don't use these constraints programmatically; eventually
            // I want to be able to test them for changes, but for right now they're just useful as
            // a windiffable comparison after regeneration.
            if (0 == this.ClusterDefs.Count)
            {
                Match m = TestFileStrings.ParseConstraint.Match(currentLine);
                if (m.Success)
                {
                    bool isEquality = m.Groups["eq"].Length > 0;
                    this.currentConstraintDefs.Add(new ConstraintDef(
                        this.VariableDefs[int.Parse(m.Groups["left"].ToString())],
                        this.VariableDefs[int.Parse(m.Groups["right"].ToString())],
                        double.Parse(m.Groups["gap"].ToString()),
                        isEquality));
                    if (isEquality)
                    {
                        this.HasEqualityConstraints = true;
                    }
                }
                else
                {
                    Validate.Fail(string.Format("Unparsable CONSTRAINT line {0}: {1}", lineNumber, currentLine));
                }
            }
            return currentSection;
        }

        private static ESection ProcessNeighboursOrResults(string currentLine, ESection currentSection)
        {
            if (currentLine.StartsWith(
                TestFileStrings.BeginNeighbours, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.Neighbours;
                return currentSection;
            }
            if (currentLine.StartsWith(
                TestFileStrings.BeginResults, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.Results;
                return currentSection;
            }
            return currentSection;
        }

        private ESection ProcessNeighbours(int lineNumber, string currentLine, ESection currentSection)
        {
            if (currentLine.StartsWith(
                TestFileStrings.EndNeighbours, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.PreResults;
                this.currentConstraintDefs = null;
                return currentSection;
            }

            Match m = TestFileStrings.ParseNeighbour.Match(currentLine);
            if (m.Success)
            {
                this.NeighborDefs.Add(new NeighborDef(
                    this.VariableDefs[int.Parse(m.Groups["left"].ToString())],
                    this.VariableDefs[int.Parse(m.Groups["right"].ToString())],
                    double.Parse(m.Groups["weight"].ToString())));
            }
            else
            {
                Validate.Fail(string.Format("Unparsable NEIGHBOUR line {0}: {1}", lineNumber, currentLine));
            }
            return currentSection;
        }

        private static ESection ProcessPreResults(string currentLine, ESection currentSection)
        {
            if (currentLine.StartsWith(
                TestFileStrings.BeginResults, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.Results;
            }
            return currentSection;
        }

        private ESection ProcessResults(int lineNumber, string currentLine, ESection currentSection)
        {
            if (currentLine.StartsWith(
                TestFileStrings.EndResults, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.PreClusterResults;
                return currentSection;
            }
            Match m;
            if (this.isTwoDimensional)
            {
                m = TestFileStrings.ParseExpected2D.Match(currentLine);
                if (m.Success)
                {
                    this.VariableDefs[int.Parse(m.Groups["var"].ToString())].SetExpected(
                        double.Parse(m.Groups["posX"].ToString()), double.Parse(m.Groups["posY"].ToString()));
                }
            }
            else
            {
                m = TestFileStrings.ParseExpected1D.Match(currentLine);
                if (m.Success)
                {
                    this.VariableDefs[int.Parse(m.Groups["var"].ToString())].SetExpected(
                        double.Parse(m.Groups["pos"].ToString()));
                }
            }
            if (!m.Success)
            {
                Validate.Fail(string.Format("Unparsable RESULT line {0}: {1}", lineNumber, currentLine));
            }
            return currentSection;
        }

        private static ESection ProcessPreClusterResults(string currentLine, ESection currentSection)
        {
            if (currentLine.StartsWith(
                TestFileStrings.BeginClusterResults, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.ClusterResults;
            }
            return currentSection;
        }

        private ESection ProcessClusterResults(int lineNumber, string currentLine, ESection currentSection)
        {
            if (currentLine.StartsWith(
                TestFileStrings.EndClusterResults, StringComparison.OrdinalIgnoreCase))
            {
                currentSection = ESection.Done;
                return currentSection;
            }
            Match m = TestFileStrings.ParseClusterResult.Match(currentLine);
            if (m.Success)
            {
                int ord = int.Parse(m.Groups["ord"].ToString());

                // Root-level clusters aren't in the cluster list, which includes cluster 0.  So we'll
                // just walk forward from the last cluster index.  If this is the first time, then we've
                // initialized m_idxLastClusterDef to < 0.
                if (this.lastClusterDefIndex < 0)
                {
                    this.lastClusterDefIndex = 0;
                }
                else
                {
                    // Verify the cluster results in the file are sorted.  This makes it easier for the results
                    // reading to be in sync.
                    Validate.IsTrue(this.ClusterDefs[this.lastClusterDefIndex].ClusterId < ord,
                            "Out of order CLUSTER RESULT ordinal");
                    ++this.lastClusterDefIndex;
                }
                for (;; ++this.lastClusterDefIndex)
                {
                    if (this.lastClusterDefIndex >= this.ClusterDefs.Count)
                    {
                        Validate.Fail(string.Format("Ordinal not in Cluster List at CLUSTER RESULT line {0}: {1}",
                            lineNumber, currentLine));
                    }
                    if (this.ClusterDefs[this.lastClusterDefIndex].ClusterId == ord)
                    {
                        break;
                    }
                }

                this.ClusterDefs[this.lastClusterDefIndex].SetResultPositions(
                    double.Parse(m.Groups["lpos"].ToString()),
                    double.Parse(m.Groups["rpos"].ToString()),
                    double.Parse(m.Groups["tpos"].ToString()),
                    double.Parse(m.Groups["bpos"].ToString()));
            }
            else
            {
                Validate.Fail(string.Format("Unparsable CLUSTER RESULT line {0}: {1}", lineNumber, currentLine));
            }
            return currentSection;
        }

        // end Load()

        private static BorderInfo ParseBorderInfo(string strDir, string strLine, int lineNumber)
        {
            Match m = TestFileStrings.ParseClusterBorder.Match(strLine);
            if (m.Success)
            {
                string strRgxDir = m.Groups["dir"].ToString();
                if (strRgxDir != strDir)
                {
                    Validate.Fail(string.Format("Out-of-sequence CLUSTER Border line {0}: {1}", lineNumber, strLine));
                }
            }
            else
            {
                Validate.Fail(string.Format("Unparsable CLUSTER Border line {0}: {1}", lineNumber, strLine));
            }

            var bi = new BorderInfo
                { 
                    InnerMargin = double.Parse(m.Groups["margin"].ToString()),
                    FixedPosition = BorderInfo.NoFixedPosition,
                    Weight = BorderInfo.DefaultFreeWeight
                };
            string strFixedPos = m.Groups["fixedpos"].ToString();
            if (0 == string.Compare("Fixed", strFixedPos, StringComparison.OrdinalIgnoreCase))
            {
                // Slightly hacky; just set this to a non-NaN and we'll compute the value from the
                // variables that go into it.
                bi.FixedPosition = 0.0;
            }
            var weight = double.Parse(m.Groups["weight"].ToString());
            if (0.0 != weight)
            {
                bi.Weight = weight;
            }
            return bi;
        }
    }
}
