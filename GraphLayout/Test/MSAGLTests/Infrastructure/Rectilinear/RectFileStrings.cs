// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RectFileStrings.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Microsoft.Msagl.UnitTests.Rectilinear
{
    internal struct RectFileStrings
    {
        ////
        //// Input/output strings and regexes.
        ////

        internal const string NullStr = "-0-";

        internal const string ParseDouble = @"-?\d+(\.\d+([Ee]\-?\d+)?)?";
        
        // Header Input summary
        private const RegexOptions RgxOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
        internal const string WriteSeed = "Seed 0x{0}";
        internal static Regex ParseSeed = new Regex(@"^Seed\s+(?<seed>(0x)?\S+)", RgxOptions);
        internal const string WriteRandomArgs = "RandomArgs {0} {1} {2} {3}";
        internal static Regex ParseRandomArgs = new Regex(@"RandomArgs\s+(?<arg>\S+)\s+(?<count>\d+)\s+(?<density>\S+)\s+(?<rotate>\S+)", RgxOptions);
        internal const string WritePadding = "Padding {0}";
        internal static Regex ParsePadding = new Regex(@"^Padding\s+(?<padding>" + ParseDouble + @")", RgxOptions);
        internal const string WriteEdgeSeparation = "EdgeSeparation {0}";
        internal static Regex ParseEdgeSeparation = new Regex(@"^EdgeSeparation\s+(?<sep>" + ParseDouble + @")", RgxOptions);
        internal const string WriteRouteToCenter = "RouteToCenter {0}";
        internal static Regex ParseRouteToCenter = new Regex(@"^RouteToCenter\s+(?<toCenter>\w+)", RgxOptions);
        internal const string WriteArrowheadLength = "ArrowheadLength {0}";
        internal static Regex ParseArrowheadLength = new Regex(@"^ArrowheadLength\s+(?<length>" + ParseDouble + @")", RgxOptions);
        internal const string WriteUseFreePortsForObstaclePorts = "UseFreePortsForObstaclePorts {0}";
        internal static Regex ParseUseFreePortsForObstaclePorts = new Regex(@"^UseFreePortsForObstaclePorts\s+(?<value>\w+)", RgxOptions);
        internal const string WriteUseSparseVisibilityGraph = "UseSparseVisibilityGraph {0}";
        internal static Regex ParseUseSparseVisibilityGraph = new Regex(@"^UseSparseVisibilityGraph\s+(?<value>\w+)", RgxOptions);
        internal const string WriteUseObstacleRectangles = "UseObstacleRectangles {0}";
        internal static Regex ParseUseObstacleRectangles = new Regex(@"^UseObstacleRectangles\s+(?<value>\w+)", RgxOptions);
        internal const string WriteBendPenalty = "BendPenalty {0}";
        internal static Regex ParseBendPenalty = new Regex(@"^BendPenalty\s+(?<value>" + ParseDouble + @")", RgxOptions);
        internal const string WriteLimitPortVisibilitySpliceToEndpointBoundingBox = "LimitPortVisibilitySpliceToEndpointBoundingBox {0}";
        internal static Regex ParseLimitPortVisibilitySpliceToEndpointBoundingBox = new Regex(@"^LimitPortVisibilitySpliceToEndpointBoundingBox\s+(?<value>\w+)", RgxOptions);
        internal static string WriteWantPaths = "WantPaths {0}";
        internal static Regex ParseWantPaths = new Regex(@"^WantPaths\s+(?<value>\w+)", RgxOptions);
        internal static string WriteWantNudger = "WantNudger {0}";
        internal static Regex ParseWantNudger = new Regex(@"^WantNudger\s+(?<value>\w+)", RgxOptions);
        internal static string WriteWantVerify = "WantVerify {0}";
        internal static Regex ParseWantVerify = new Regex(@"^WantVerify\s+(?<value>\w+)", RgxOptions);
        internal const string WriteStraightTolerance = "StraightTolerance {0}";
        internal static Regex ParseStraightTolerance = new Regex(@"^StraightTolerance\s+(?<value>" + ParseDouble + @")", RgxOptions);
        internal const string WriteCornerTolerance = "CornerTolerance {0}";
        internal static Regex ParseCornerTolerance = new Regex(@"^CornerTolerance\s+(?<value>" + ParseDouble + @")", RgxOptions);

        // Header Output summary
        internal static Regex ParseVisibilityGraphSummary = new Regex(@"^\S+\s+(?<vcount>\d+)\s+\S+\s+(?<ecount>\d+)", RgxOptions);

        // Input detail
        internal const string BeginUnpaddedObstacles = "BEGIN UNPADDED_OBSTACLES";
        internal const string WriteId = "  Id {0}";
        internal static Regex ParseId = new Regex(@"^\s*Id\s+(?<id>-?\d+)",
                                      RgxOptions);
        internal const string BeginCurve = "Begin Curve";
        internal const string WriteSegment = "  [[{0}, {1}] -> [{2}, {3}]]";
        internal static Regex ParseSegment = new Regex(@"^\s*\["
                                      + @"\[(?<startX>" + ParseDouble + @"),\s+(?<startY>" + ParseDouble + @")\]"
                                      + @"\s+-\>\s+"
                                      + @"\[(?<endX>" + ParseDouble + @"),\s+(?<endY>" + ParseDouble + @")\]"
                                      + @"\]",
                                      RgxOptions);
        internal const string EndCurve = "End Curve";
        internal const string BeginPolyline = "Begin Polyline";
        internal const string WritePoint = "  [{0}, {1}]";
        internal static Regex ParsePoint = new Regex(@"^\s*"
                                      + @"\[(?<X>" + ParseDouble + @"),\s+(?<Y>" + ParseDouble + @")\]",
                                      RgxOptions);
        internal const string EndPolyline = "End Polyline";
        internal const string BeginEllipse = "Begin Ellipse";
        internal const string WriteEllipse = "  [{0}, {1}], [{2}, {3}], [{4}, {5}]";
        internal static Regex ParseEllipse = new Regex(@"^\s*"
                                      + @"\[(?<axisAx>" + ParseDouble + @"),\s+(?<axisAy>" + ParseDouble + @")\],\s+"
                                      + @"\[(?<axisBx>" + ParseDouble + @"),\s+(?<axisBy>" + ParseDouble + @")\],\s+"
                                      + @"\[(?<centerX>" + ParseDouble + @"),\s+(?<centerY>" + ParseDouble + @")\]",
                                      RgxOptions);
        internal const string EndEllipse = "End Ellipse";

        internal const string BeginRoundedRect = "Begin RoundedRect";
        internal const string WriteRoundedRect = "  x {0} y {1} w {2} h {3} rX {4} rY {5}";
        internal static Regex ParseRoundedRect = new Regex(@"^\s*"
                                      + @"x\s+(?<X>" + ParseDouble + @")\s+y\s+(?<Y>" + ParseDouble + @")\s+"
                                      + @"w\s+(?<width>" + ParseDouble + @")\s+h\s+(?<height>" + ParseDouble + @")\s+"
                                      + @"rX\s+(?<radiusX>" + ParseDouble + @")\s+rY\s+(?<radiusY>" + ParseDouble + @")",
                                      RgxOptions);
        internal const string EndRoundedRect = "End RoundedRect";
        
        internal const string Children = "Children";

        internal const string WriteClumpId = "  ClumpId {0}";
        internal static Regex ParseClumpId = new Regex(@"^\s*ClumpId\s+(?<id>\d+)", 
                                      RgxOptions);
        internal const string WriteConvexHullId = "  ConvexHullId {0}";
        internal static Regex ParseConvexHullId = new Regex(@"^\s*ConvexHullId\s+(?<id>\d+)",
                                      RgxOptions);

        internal const string EndUnpaddedObstacles = "END UNPADDED_OBSTACLES";

        internal const string BeginPorts = "BEGIN PORTS";
        internal const string Floating = "Float";
        internal const string Relative = "Relative";
        internal const string Multi = "Multi";
        internal const string WritePort = "{0} [{1}, {2}] pId {3} sId {4}";
        // Reuses (Write|Parse)Point for MultiPort offsets
        internal static Regex ParsePort = new Regex(@"^(?<type>\w+)\s+"
                                      + @"\[(?<X>" + ParseDouble + @"),\s+(?<Y>" + ParseDouble + @")\]"
                                      + @"\s+\w+\s+(?<portId>\d+)\s+\w+\s+(?<shapeId>-?\d+)",
                                      RgxOptions);
        internal const string WriteMultiPortOffsets = "Offsets ActiveIndex {0}";
        internal static Regex ParseMultiPortOffsets = new Regex(@"^\w+\s+\w+\s+(?<index>\d+)",
                                      RgxOptions);
        internal const string PortEntryOnCurve = "PortEntryOnCurve";
        // Reuses (Write|Parse)Point for the spans
        internal const string EndPorts = "END PORTS";

        // Output detail
        internal const string BeginRoutingSpecs = "BEGIN ROUTING_SPECS";
        internal const string WriteEdgeGeometry = "{0} -> {1}"
                                      + @" aS {2} aT {3}"
                                      + @" aL {4} aW {5}"
                                      + @" lW {6}";
        internal static Regex ParseEdgeGeometry = new Regex(@"^(?<sourceId>\d+)\s+\S+\s+(?<targetId>\d+)\s+"
                                      + @"\S+\s+(?<arrowheadAtSource>\w+)\s+\S+\s+(?<arrowheadAtTarget>\w+)\s+"
                                      + @"\S+\s+(?<arrowheadLength>" + ParseDouble + @")\s+\S+\s+(?<arrowheadWidth>" + ParseDouble + @")\s+"
                                      + @"\S+\s+(?<lineWidth>" + ParseDouble + @")",
                                      RgxOptions);
        internal const string Waypoints = "Waypoints";
        // Reuses (Write|Parse)Point for the waypoints
        internal const string EndRoutingSpecs = "END ROUTING_SPECS";

        internal const string BeginPaddedObstacles = "BEGIN PADDED_OBSTACLES";
        // Reuses (Write|Parse)(CurveSegment|PolylinePoint|ShapeId)
        internal const string EndPaddedObstacles = "END PADDED_OBSTACLES";

        internal const string BeginConvexHulls = "BEGIN CONVEX_HULLS";
        // Reuses (Write|Parse)PolylinePoint
        internal const string EndConvexHulls = "END CONVEX_HULLS";

        internal const string BeginHScanSegments = "BEGIN HSCAN_SEGMENTS";
        // Reuses (Write|Parse)Segment
        internal const string EndHScanSegments = "END HSCAN_SEGMENTS";
        internal const string BeginVScanSegments = "BEGIN VSCAN_SEGMENTS";
        // Reuses (Write|Parse)ScanSegment
        internal const string EndVScanSegments = "END VSCAN_SEGMENTS";

        internal const string BeginVisibilityVertices = "BEGIN VISIBILITY_VERTICES";
        // Reuses (Write|Parse)Point
        internal const string EndVisibilityVertices = "END VISIBILITY_VERTICES";

        internal const string BeginVisibilityEdges = "BEGIN VISIBILITY_EDGES";
        // Reuses (Write|Parse)Segment
        internal const string EndVisibilityEdges = "END VISIBILITY_EDGES";

        internal const string BeginPaths = "BEGIN PATHS";
        internal const string WritePathEndpoints = "Source {0} Target {1}";            // start -> end
        internal static Regex ParsePathEndpoints = new Regex(@"^\S+\s+(?<sourceId>\d+)\s+\S+\s+(?<targetId>\d+)",
                                      RgxOptions);
        // Reuses (Write|Parse)Polyline
        internal const string EndPaths = "END PATHS";
    }
}