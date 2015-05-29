// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestFileStrings.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Microsoft.Msagl.UnitTests.Constraints
{
    // Class for testing cluster definitions.  We can have two dimensions, X and Y, for
    // OverlapRemoval, but only one for ProjectionSolver, so we use X for that.
    internal struct TestFileStrings
    {
        internal static Regex ParseSeed = new Regex(@"^Seed\s+(?<seed>(0x)?\S+)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal static Regex ParseWeight = new Regex(@"^Weight\s+(?<weight>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal static Regex ParseScale = new Regex(@"^Scale\s+(?<scale>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal static Regex ParsePadding = new Regex(@"^Padding\s+(?<X>\d+(\.\d+)?)\s+(?<Y>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal static Regex ParseMinClusterSize = new Regex(@"^MinClusterSize\s+(?<X>\d+(\.\d+)?)\s+(?<Y>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal static Regex ParseMargin = new Regex(@"^Margin\s+(?<margin>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal static Regex ParseGoal1D = new Regex(@"^Goal?\s+(?<goalx>-?\d+(\.\d+([eE][+-]\d+)?)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal static Regex ParseGoal2D = new Regex(@"^Goal\s+(?<goalx>-?\d+(\.\d+([eE][+-]\d+)?)?)\s+(?<goaly>-?\d+(\.\d+([eE][+-]\d+)?)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        internal const string BeginVariables = "BEGIN VARIABLES";
        internal const string WriteVariable1D = "{0} {1:F5}"            // Ord, position
                                            + " {2:F1}"                 // size
                                            + " {3:F5}"                 // weight
                                            + " {4:F5}";                // scale
        internal static Regex ParseVariable1D = new Regex(@"^(?<ord>\d+)\s+"
                                            + @"(?<pos>-?\d+(\.\d+)?)\s+"
                                            + @"(?<size>\d+(\.\d+)?)\s+"
                                            + @"(?<weight>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal static Regex ParseVariable1DScale = new Regex(@"^(?<ord>\d+)\s+"
                                            + @"(?<pos>-?\d+(\.\d+)?)\s+"
                                            + @"(?<size>\d+(\.\d+)?)\s+"
                                            + @"(?<weight>\d+(\.\d+)?)\s+"
                                            + @"(?<scale>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string WriteVariable2D = "{0} {1:F5} {2:F5}"     // Ord, positions
                                            + " {3:F5} {4:F5}"          // sizes
                                            + " {5:F5} {6:F5}";         // weights
        internal static Regex ParseVariable2D = new Regex(@"^(?<ord>\d+)\s+"
                                            + @"(?<posX>-?\d+(\.\d+)?)\s+(?<posY>-?\d+(\.\d+)?)\s+"
                                            + @"(?<sizeX>\d+(\.\d+)?)\s+(?<sizeY>\d+(\.\d+)?)\s+"
                                            + @"(?<weightX>\d+(\.\d+)?)\s+(?<weightY>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string EndVariables = "END VARIABLES";

        internal const string BeginCluster = "BEGIN CLUSTER";
        internal const string WriteClusterId = "ID {0}";
        internal static Regex ParseClusterId = new Regex(@"^ID\s+(?<id>\d+)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string WriteClusterParent = "Parent {0}";
        internal static Regex ParseClusterParent = new Regex(@"^Parent\s+(?<parent>\d+)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string WriteClusterMinSize = "MinSize {0:F5} {1:F5}";
        internal static Regex ParseClusterMinSize = new Regex(@"^MinSize\s+(?<X>\d+(\.\d+)?)\s+(?<Y>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string WriteClusterBorder = "{0}Border {1:F5} {2} {3:F8}";
        internal static Regex ParseClusterBorder = new Regex(@"^(?<dir>(Left|Right|Top|Bottom))Border\s+"
                                            + @"(?<margin>\d+(\.\d+)?)\s+"
                                            + @"(?<fixedpos>(Fixed|Free))\s+"
                                            + @"(?<weight>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string WriteClusterVariable = "{0}";
        internal static Regex ParseClusterVariable = new Regex(@"^(?<var>\d+)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string EndCluster = "END CLUSTER";

        internal const string BeginConstraints = "BEGIN CONSTRAINTS";
        internal const string BeginConstraintsX = "BEGIN CONSTRAINTS_X";
        internal const string BeginConstraintsY = "BEGIN CONSTRAINTS_Y";
        internal const string WriteConstraint = "{0} {1} {2}{3:F5}";
        internal static Regex ParseConstraint = new Regex(@"^(?<left>\d+)\s+(?<right>\d+)\s+(?<eq>\=?)(?<gap>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string EndConstraints = "END CONSTRAINTS";

        internal const string BeginNeighbours = "BEGIN NEIGHBOURS";
        internal const string WriteNeighbour = "{0} {1} {2:F5}";
        internal static Regex ParseNeighbour = new Regex(@"^(?<left>\d+)\s+(?<right>\d+)\s+(?<weight>\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string EndNeighbours = "END NEIGHBOURS";


        internal const string BeginResults = "BEGIN RESULTS";
        internal const string WriteResults1D = "{0} {1:F5}";
        internal static Regex ParseExpected1D = new Regex(@"^(?<var>\d+)\s+(?<pos>-?\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string WriteResults2D = "{0} {1:F5} {2:F5}";
        internal static Regex ParseExpected2D = new Regex(@"^(?<var>\d+)\s+(?<posX>-?\d+(\.\d+)?)\s+(?<posY>-?\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string EndResults = "END RESULTS";

        internal const string BeginClusterResults = "BEGIN CLUSTER RESULTS";
        internal const string WriteClusterResults = "ID {0} L {1:F5} R {2:F5} T {3:F5} B {4:F5}";
        internal static Regex ParseClusterResult = new Regex(@"^ID\s+(?<ord>\d+)\s+"
                                            + @"L\s+(?<lpos>-?\d+(\.\d+)?)\s+"
                                            + @"R\s+(?<rpos>-?\d+(\.\d+)?)\s+"
                                            + @"T\s+(?<tpos>-?\d+(\.\d+)?)\s+"
                                            + @"B\s+(?<bpos>-?\d+(\.\d+)?)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal const string EndClusterResults = "END CLUSTER RESULTS";

        internal static Regex ParseUnsatisfiableConstraints = new Regex(@"^UnsatisfiableConstraints\s+(?<count>(0x)?\d+)",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}
