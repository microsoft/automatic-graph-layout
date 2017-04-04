// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RectFileReader.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.UnitTests.Rectilinear 
{
    using System.Linq;
    using System.Text;

    using Core.DataStructures;
    using Core.Geometry;
    using Routing.Rectilinear;

    internal class RectFileReader : IDisposable
    {
        internal List<Shape> UnpaddedObstacles { get; private set; }
        internal List<Polyline> PaddedObstacles { get; private set; }
        internal List<EdgeGeometry> RoutingSpecs { get; private set; }
        internal List<LineSegment> HorizontalScanLineSegments { get; private set; }
        internal List<LineSegment> VerticalScanLineSegments { get; private set; }
        internal VisibilityGraph VisibilityGraph { get; private set; }

        internal int InitialSeed { get; private set; }
        internal string RandomObstacleArg { get; private set; }
        internal int RandomObstacleCount { get; private set; }
        internal string RandomObstacleDensity { get; private set; }
        internal bool RandomObstacleRotate { get; private set; }
        internal double Padding { get; private set; }
        internal double EdgeSeparation { get; private set; }
        internal bool RouteToCenter { get; private set; }
        internal double ArrowheadLength { get; private set; }

        private int fileVertexCount;
        private int fileEdgeCount;
        internal bool IsRandom { get; set; }

        // For reading the relationship between free relative ports and their shapes.
        internal Dictionary<Port, Shape> FreeRelativePortToShapeMap { get; private set; }
        internal bool UseFreePortsForObstaclePorts { get; private set; }
        internal bool UseSparseVisibilityGraph { get; private set; }
        internal bool UseObstacleRectangles { get; private set; }
        internal double BendPenalty { get; private set; }
        internal bool LimitPortVisibilitySpliceToEndpointBoundingBox { get; private set; }

        // Verification stuff.
        internal bool WantPaths { get; private set; }
        internal bool WantNudger { get; private set; }
        internal bool WantVerify { get; private set; }
        internal double StraightTolerance { get; private set; }
        internal double CornerTolerance { get; private set; }

        // Used by subroutines.
        private string currentLine;
        private int currentLineNumber;
        private StreamReader inputFileReader;
        private readonly Dictionary<int, Port> idToPortMap = new Dictionary<int, Port>();
        private readonly Dictionary<int, Shape> idToShapeMap = new Dictionary<int, Shape>();
        private readonly Dictionary<Shape, int> shapeToIdMap = new Dictionary<Shape, int>();
        private readonly Dictionary<int, Polyline> idToPaddedPolylineMap = new Dictionary<int, Polyline>();
        private readonly Dictionary<Tuple<Port, Port>, Curve> portsToPathMap = new Dictionary<Tuple<Port, Port>, Curve>();

        // For clumps and convex hulls.
        private class ObstacleAccretion
        {
            internal readonly int Id;
            internal readonly List<int> SiblingIds = new List<int>();
            internal Polyline Polyline;
            
            // Filled in by clump or convex hull verification.
            internal object RouterAccretion;

            internal ObstacleAccretion(int id)
            {
                this.Id = id;
            }
        }

        private readonly Dictionary<int, ObstacleAccretion> convexHullIdToAccretionMap = new Dictionary<int, ObstacleAccretion>();
        private readonly Dictionary<int, ObstacleAccretion> shapeIdToConvexHullMap = new Dictionary<int, ObstacleAccretion>();
        private readonly Dictionary<int, ObstacleAccretion> clumpIdToAccretionMap = new Dictionary<int, ObstacleAccretion>();
        private readonly Dictionary<int, ObstacleAccretion> shapeIdToClumpMap = new Dictionary<int, ObstacleAccretion>();

        // This is not "provably invalid" as any shape Id might have it but we currently don't
        // assign an id < 0 except for -1 for shapeId for FreePorts (which don't have a shape).
        private const int InvalidId = -42;

        private readonly TestPointComparer comparer;

        internal RectFileReader(string fileName, int fileRoundingDigits)
        {
            comparer = new TestPointComparer(fileRoundingDigits);

            UnpaddedObstacles = new List<Shape>();
            PaddedObstacles = new List<Polyline>();
            RoutingSpecs = new List<EdgeGeometry>();
            HorizontalScanLineSegments = new List<LineSegment>();
            VerticalScanLineSegments = new List<LineSegment>();
            VisibilityGraph = new VisibilityGraph();

            this.FreeRelativePortToShapeMap = new Dictionary<Port, Shape>();

            this.inputFileReader = new StreamReader(fileName);

            this.RandomObstacleArg = RectFileStrings.NullStr;
            this.RandomObstacleDensity = RectFileStrings.NullStr;

            // Input parameters
            ReadHeader();

            // Input detail
            ReadInputObstacles();
            ReadPorts();
            ReadRoutingSpecs();

            // Output detail.
            ReadPaddedObstacles();
            ReadConvexHulls();
            ReadScanSegments();
            ReadVisibilityGraph();
            ReadPaths();

            this.inputFileReader.Close();
            if (0 == this.UnpaddedObstacles.Count)
            {
                Validate.Fail("No obstacles found in file");
            }
        }

        private void ReadHeader()
        {
            // Input parameters and output summary
            Match m = ParseNext(RectFileStrings.ParseSeed);
            if (m.Success)
            {
                string strSeed = m.Groups["seed"].ToString();
                System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Integer;
                if (strSeed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    // For some reason the 0x prefix is not allowed for hex strings.
                    strSeed = strSeed.Substring(2);
                    style = System.Globalization.NumberStyles.HexNumber;
                }
                this.InitialSeed = int.Parse(strSeed, style);
            }
            m = ParseNext(RectFileStrings.ParseRandomArgs);
            if (!IsString(RectFileStrings.NullStr, m.Groups["arg"].ToString()))
            {
                this.IsRandom = true;
                this.RandomObstacleArg = m.Groups["arg"].ToString();
                this.RandomObstacleCount = int.Parse(m.Groups["count"].ToString());
                this.RandomObstacleDensity = m.Groups["density"].ToString();
                this.RandomObstacleRotate = bool.Parse(m.Groups["rotate"].ToString());
            }

            // This sequencing assumes the members are in the expected order but 

            m = ParseNext(RectFileStrings.ParsePadding);
            this.Padding = double.Parse(m.Groups["padding"].ToString());

            m = ParseNext(RectFileStrings.ParseEdgeSeparation);
            this.EdgeSeparation = double.Parse(m.Groups["sep"].ToString());

            m = ParseNext(RectFileStrings.ParseRouteToCenter);
            this.RouteToCenter = bool.Parse(m.Groups["toCenter"].ToString());

            m = ParseNext(RectFileStrings.ParseArrowheadLength);
            this.ArrowheadLength = double.Parse(m.Groups["length"].ToString());

            m = this.ParseNext(RectFileStrings.ParseUseFreePortsForObstaclePorts);
            this.UseFreePortsForObstaclePorts = bool.Parse(m.Groups["value"].ToString());

            m = this.ParseNext(RectFileStrings.ParseUseSparseVisibilityGraph);
            this.UseSparseVisibilityGraph = bool.Parse(m.Groups["value"].ToString());

            m = this.ParseNext(RectFileStrings.ParseUseObstacleRectangles);
            this.UseObstacleRectangles = bool.Parse(m.Groups["value"].ToString());

            m = ParseNext(RectFileStrings.ParseBendPenalty);
            this.BendPenalty = double.Parse(m.Groups["value"].ToString());

            m = this.ParseNext(RectFileStrings.ParseLimitPortVisibilitySpliceToEndpointBoundingBox);
            this.LimitPortVisibilitySpliceToEndpointBoundingBox = bool.Parse(m.Groups["value"].ToString());

            m = ParseNext(RectFileStrings.ParseWantPaths);
            this.WantPaths = bool.Parse(m.Groups["value"].ToString());

            m = ParseNext(RectFileStrings.ParseWantNudger);
            this.WantNudger = bool.Parse(m.Groups["value"].ToString());

            m = ParseNext(RectFileStrings.ParseWantVerify);
            this.WantVerify = bool.Parse(m.Groups["value"].ToString());

            m = ParseNext(RectFileStrings.ParseStraightTolerance);
            this.StraightTolerance = double.Parse(m.Groups["value"].ToString());

            m = ParseNext(RectFileStrings.ParseCornerTolerance);
            this.CornerTolerance = double.Parse(m.Groups["value"].ToString());

            // Output summary
            m = ParseNext(RectFileStrings.ParseVisibilityGraphSummary);
            this.fileVertexCount = int.Parse(m.Groups["vcount"].ToString());
            this.fileEdgeCount = int.Parse(m.Groups["ecount"].ToString());
        }

        private void ReadInputObstacles()
        {
            this.ReadUnpaddedObstacles();
            foreach (var sibList in this.shapeIdToClumpMap.Values.Select(acc => acc.SiblingIds)) { sibList.OrderBy(sibId => sibId); }
            foreach (var sibList in this.shapeIdToConvexHullMap.Values.Select(acc => acc.SiblingIds)) { sibList.OrderBy(sibId => sibId); }
        }

        private void ReadUnpaddedObstacles()
        {
            this.VerifyIsNextLine(RectFileStrings.BeginUnpaddedObstacles);

            // Get to the first line for consistency with the lookahead for children which will end up
            // reading the following line.
            this.NextLine();

            for (;;)
            {
                int id;
                Shape shape;
                if (this.IsLine(RectFileStrings.BeginCurve))
                {
                    id = ParseNextId();
                    shape = new Shape(this.ReadCurve()) { UserData = id };
                }
                else if (this.IsLine(RectFileStrings.BeginEllipse))
                {
                    id = ParseNextId();
                    shape = new Shape(this.ReadEllipse()) { UserData = id };
                }
                else if (this.IsLine(RectFileStrings.BeginRoundedRect))
                {
                    id = ParseNextId();
                    shape = new Shape(this.ReadRoundedRect()) { UserData = id };
                }
                else if (this.IsLine(RectFileStrings.BeginPolyline))
                {
                    id = ParseNextId();
                    shape = new Shape(this.ReadPolyline()) { UserData = id };
                }
                else
                {
                    this.VerifyIsLine(RectFileStrings.EndUnpaddedObstacles);
                    return;
                }

                this.UnpaddedObstacles.Add(shape);
                idToShapeMap.Add(id, shape);
                shapeToIdMap.Add(shape, id);

                this.NextLine();
                TryParseChildren(shape);
                this.TryParseClumpId(id);
                this.TryParseConvexHullId(id);
            }
        }

        private void TryParseChildren(Shape shape)
        {
            if (!IsLine(RectFileStrings.Children))
            {
                return;
            }
            int id;
            while (TryParseNextId(out id))
            {
                shape.AddChild(idToShapeMap[id]);
            }
        }

        private void TryParseClumpId(int shapeId)
        {
            Match m = this.TryParse(RectFileStrings.ParseClumpId);
            if (!m.Success)
            {
                return;
            }
            int clumpId = int.Parse(m.Groups["id"].ToString());
            this.shapeIdToClumpMap[shapeId] = AddShapeToAccretion(shapeId, clumpId, this.clumpIdToAccretionMap);
            this.NextLine();
        }

        private void TryParseConvexHullId(int shapeId)
        {
            Match m = this.TryParse(RectFileStrings.ParseConvexHullId);
            if (!m.Success)
            {
                return;
            }
            int convexHullId = int.Parse(m.Groups["id"].ToString());
            this.shapeIdToConvexHullMap[shapeId] = AddShapeToAccretion(shapeId, convexHullId, this.convexHullIdToAccretionMap);
            this.NextLine();
        }

        private static ObstacleAccretion AddShapeToAccretion(int shapeId, int accretionId, Dictionary<int, ObstacleAccretion> accretionMap)
        {
            ObstacleAccretion accretion;
            if (!accretionMap.TryGetValue(accretionId, out accretion))
            {
                accretion = new ObstacleAccretion(accretionId);
                accretionMap[accretionId] = accretion;
            }
            accretion.SiblingIds.Add(shapeId);
            return accretion;
        }

        private void ReadPorts()
        {
            Match m;
            this.VerifyIsNextLine(RectFileStrings.BeginPorts);

            // Get to the first line for consistency with the lookahead for multiPort offsets and/or any
            // PortEntries, which will end up reading the following line.
            this.NextLine();

            for (;;)
            {
                if (!(m = ParseOrDone(RectFileStrings.ParsePort, RectFileStrings.EndPorts)).Success)
                {
                    break;
                }

                bool isMultiPort = IsString(m.Groups["type"].ToString(), RectFileStrings.Multi);
                bool isRelative = IsString(m.Groups["type"].ToString(), RectFileStrings.Relative);
                var x = double.Parse(m.Groups["X"].ToString());
                var y = double.Parse(m.Groups["Y"].ToString());
                var portId = int.Parse(m.Groups["portId"].ToString());
                var shapeId = int.Parse(m.Groups["shapeId"].ToString());
                Validate.IsFalse(idToPortMap.ContainsKey(portId), "PortId already exists");
                var location = new Point(x, y);
                Shape shape = GetShapeFromId(shapeId, isMultiPort || isRelative);
                Port port;
                if (isMultiPort)
                {
                    // 'location' was actually the active offset of the multiPort.  Recreate it and reset the
                    // closest-location and verify the active offset index is the same.  This may fail if there
                    // are two identical offsets in the offset list, in which case fix the test setup.
                    int activeOffsetIndex;
                    var offsets = ReadMultiPortOffsets(out activeOffsetIndex);
                    var multiPort = new MultiLocationFloatingPort(() => shape.BoundaryCurve, () => shape.BoundingBox.Center, offsets);
                    multiPort.SetClosestLocation(multiPort.CenterDelegate() + location);
                    Validate.AreEqual(multiPort.ActiveOffsetIndex, activeOffsetIndex, CurrentLineError("ActiveOffsetIndex is not as expected"));
                    port = multiPort;
                }
                else
                {
                    if (isRelative)
                    {
                        // The location in the ParsePort line is the offset for the relative port.
                        port = new RelativeFloatingPort(() => shape.BoundaryCurve, () => shape.BoundingBox.Center, location);
                    }
                    else
                    {
                        Validate.IsTrue(IsString(m.Groups["type"].ToString(), RectFileStrings.Floating), CurrentLineError("Unknown port type"));
                        port = new FloatingPort((null == shape) ? null : shape.BoundaryCurve, location);
                    }
                    this.NextLine();    // Since we didn't read multiPort offsets
                }
                idToPortMap.Add(portId, port);
                if (null != shape)
                {
                    if (!this.UseFreePortsForObstaclePorts)
                    {
                        shape.Ports.Insert(port);
                    }
                    else
                    {
                        FreeRelativePortToShapeMap[port] = shape;
                    }
                }
                ReadPortEntries(port);
            }
        }

        private Shape GetShapeFromId(int shapeId, bool isMultiOrRelative)
        {
            Shape shape;
            if (!idToShapeMap.TryGetValue(shapeId, out shape))
            {
                Validate.IsFalse(isMultiOrRelative, CurrentLineError("Shape not found for MultiOrRelativePort"));
            }
            return shape;
        }

        private List<Point> ReadMultiPortOffsets(out int activeOffsetIndex)
        {
            var offsets = new List<Point>();
            Match m = ParseNext(RectFileStrings.ParseMultiPortOffsets);
            Validate.IsTrue(m.Success, CurrentLineError("Did not find expected MultiPortOffsets"));
            activeOffsetIndex = int.Parse(m.Groups["index"].ToString());

            for (;;)
            {
                m = TryParseNext(RectFileStrings.ParsePoint);
                if (!m.Success)
                {
                    break;
                }
                var x = double.Parse(m.Groups["X"].ToString());
                var y = double.Parse(m.Groups["Y"].ToString());
                offsets.Add(new Point(x, y));
            }
            Validate.IsTrue(offsets.Count > 0, CurrentLineError("No offsets found for multiPort"));
            return offsets;
        }

        private void ReadPortEntries(Port port)
        {
            // We've already positioned ourselves to the next line in ReadPorts.
            if (!IsLine(RectFileStrings.PortEntryOnCurve))
            {
                return;
            }

            var spans = new List<Tuple<double, double>>();
            Match m;
            while ((m = TryParseNext(RectFileStrings.ParsePoint)).Success)
            {
                // We reuse ParsePoint because a span is just two doubles as well.
                var first = double.Parse(m.Groups["X"].ToString());
                var second = double.Parse(m.Groups["Y"].ToString());
                spans.Add(new Tuple<double, double>(first, second));
            }
            Validate.IsTrue(spans.Count > 0, CurrentLineError("Empty span list"));
            port.PortEntry = new PortEntryOnCurve(port.Curve, spans);
        }

        private void ReadRoutingSpecs()
        {
            Match m;
            this.VerifyIsNextLine(RectFileStrings.BeginRoutingSpecs);

            // Get to the first line for consistency with the lookahead for waypoints, which will
            // end up reading the following line.
            this.NextLine();

            for (;;)
            {
                if (!(m = ParseOrDone(RectFileStrings.ParseEdgeGeometry, RectFileStrings.EndRoutingSpecs)).Success)
                {
                    break;
                }

                var sourceId = int.Parse(m.Groups["sourceId"].ToString());
                var targetId = int.Parse(m.Groups["targetId"].ToString());
                var arrowheadAtSource = bool.Parse(m.Groups["arrowheadAtSource"].ToString());
                var arrowheadAtTarget = bool.Parse(m.Groups["arrowheadAtTarget"].ToString());
                var arrowheadLength = double.Parse(m.Groups["arrowheadLength"].ToString());
                var arrowheadWidth = double.Parse(m.Groups["arrowheadWidth"].ToString());
                var lineWidth = double.Parse(m.Groups["lineWidth"].ToString());

                Port sourcePort, targetPort;
                Validate.IsTrue(idToPortMap.TryGetValue(sourceId, out sourcePort), CurrentLineError("Can't find source port"));
                Validate.IsTrue(idToPortMap.TryGetValue(targetId, out targetPort), CurrentLineError("Can't find target port"));

                var edgeGeom = new EdgeGeometry(sourcePort, targetPort) { LineWidth = lineWidth };
                if (arrowheadAtSource)
                {
                    edgeGeom.SourceArrowhead = new Arrowhead { Length = arrowheadLength, Width = arrowheadWidth };
                }
                if (arrowheadAtTarget)
                {
                    edgeGeom.TargetArrowhead = new Arrowhead { Length = arrowheadLength, Width = arrowheadWidth };
                }

                this.RoutingSpecs.Add(edgeGeom);
                this.ReadWaypoints(edgeGeom);
            }
        }

        private void ReadWaypoints(EdgeGeometry edgeGeom)
        {
            if (!IsNextLine(RectFileStrings.Waypoints))
            {
                return;
            }

            var waypoints = new List<Point>();
            Match m;
            while ((m = TryParseNext(RectFileStrings.ParsePoint)).Success)
            {
                // We reuse ParsePoint because a span is just two doubles as well.
                var x = double.Parse(m.Groups["X"].ToString());
                var y = double.Parse(m.Groups["Y"].ToString());
                waypoints.Add(new Point(x, y));
            }
            Validate.IsTrue(waypoints.Count > 0, CurrentLineError("Empty waypoint list"));
            edgeGeom.Waypoints = waypoints;
        }

        private void ReadPaddedObstacles()
        {
            this.VerifyIsNextLine(RectFileStrings.BeginPaddedObstacles);
            for (;;)
            {
                if (!this.IsNextLine(RectFileStrings.BeginPolyline))
                {
                    this.VerifyIsLine(RectFileStrings.EndPaddedObstacles);
                    break;
                }
                int id = ParseNextId();
                Validate.IsFalse(idToPaddedPolylineMap.ContainsKey(id), CurrentLineError("Duplicate padded-obstacle id"));
                var polyline = this.ReadPolyline();
                this.PaddedObstacles.Add(polyline);
                idToPaddedPolylineMap.Add(id, polyline);
            }
        }

        private void ReadConvexHulls()
        {
            this.VerifyIsNextLine(RectFileStrings.BeginConvexHulls);
            for (; ; )
            {
                if (!this.IsNextLine(RectFileStrings.BeginPolyline))
                {
                    this.VerifyIsLine(RectFileStrings.EndConvexHulls);
                    break;
                }
                int id = ParseNextId();
                var hullAccretion = this.convexHullIdToAccretionMap[id];
                Validate.AreEqual(id, hullAccretion.Id, "This should always be true.. accretion.Id is just to make debugging easier");
                hullAccretion.Polyline = this.ReadPolyline();
            }
        }

        public void VerifyObstaclePaddedPolylines(IEnumerable<Obstacle> routerObstacles)
        {
            if (0 == idToPaddedPolylineMap.Count)
            {
                return;
            }
            foreach (var routerObstacle in routerObstacles)
            {
                var filePolyline = idToPaddedPolylineMap[shapeToIdMap[routerObstacle.InputShape]];
                var routerPolyline = routerObstacle.PaddedPolyline;
                VerifyPolylinesAreSame(filePolyline, routerPolyline);
            }
        }

        private void VerifyPolylinesAreSame(Polyline filePolyline, Polyline routerPolyline)
        {
            Validate.AreEqual(filePolyline.PolylinePoints.Count(), routerPolyline.PolylinePoints.Count(), "Different number of points in polyline");
            Validate.IsTrue(comparer.Equals(filePolyline.StartPoint.Point, routerPolyline.StartPoint.Point), "Polyline StartPoints are not equal");
            Validate.IsTrue(comparer.Equals(filePolyline.EndPoint.Point, routerPolyline.EndPoint.Point), "Polyline EndPoints are not equal");
            var filePoint = filePolyline.StartPoint.NextOnPolyline;
            var obstaclePoint = routerPolyline.StartPoint.NextOnPolyline;
            while (!comparer.Equals(filePoint.Point, filePolyline.EndPoint.Point))
            {
                Validate.IsTrue(comparer.Equals(filePoint.Point, obstaclePoint.Point), "Polyline Intermediate Points are not equal");
                filePoint = filePoint.NextOnPolyline;
                obstaclePoint = obstaclePoint.NextOnPolyline;
            }
        }

        private void ReadScanSegments()
        {
            Match m;
            this.VerifyIsNextLine(RectFileStrings.BeginHScanSegments);
            for (;;)
            {
                if (!(m = ParseNextOrDone(RectFileStrings.ParseSegment, RectFileStrings.EndHScanSegments)).Success)
                {
                    break;
                }
                this.HorizontalScanLineSegments.Add(LoadLineSegment(m));
            }

            this.VerifyIsNextLine(RectFileStrings.BeginVScanSegments);
            for (;;)
            {
                if (!(m = ParseNextOrDone(RectFileStrings.ParseSegment, RectFileStrings.EndVScanSegments)).Success)
                {
                    break;
                }
                this.VerticalScanLineSegments.Add(LoadLineSegment(m));
            }
        }

        internal void VerifyScanSegments(RectilinearEdgeRouterWrapper router)
        {
            this.VerifyAxisScanSegments(this.HorizontalScanLineSegments, router.HorizontalScanLineSegments, "Horizontal ScanSegment");
            this.VerifyAxisScanSegments(this.VerticalScanLineSegments, router.VerticalScanLineSegments, "Vertical ScanSegment");
        }

        private void VerifyAxisScanSegments(List<LineSegment> readerSegmentList, IEnumerable<LineSegment> routerSegs, string name)
        {
            // These are already ordered as desired in the tree.
            if (0 != readerSegmentList.Count)
            {
                IEnumerator<LineSegment> readerSegs = readerSegmentList.GetEnumerator();
                foreach (var routerSeg in routerSegs)
                {
                    readerSegs.MoveNext();
                    var readerSeg = readerSegs.Current;
                    Validate.IsTrue(comparer.IsClose(routerSeg, readerSeg),
                            string.Format(System.Globalization.CultureInfo.InvariantCulture, "Router {0} does not match Reader", name));
                }
            }
        }

        internal void VerifyClumps(RectilinearEdgeRouterWrapper router) {
            if (this.clumpIdToAccretionMap.Count == 0)
            {
                return;
            }
            VerifyThatAllRouterClumpsAreInFile(router);
            VerifyThatAllFileClumpsAreInRouter(router);
        }

        private void VerifyThatAllRouterClumpsAreInFile(RectilinearEdgeRouterWrapper router) {
            foreach (var routerClump in router.ObstacleTree.GetAllPrimaryObstacles().Where(obs => obs.IsOverlapped).Select(obs => obs.Clump).Distinct())
            {
                var routerSiblings = routerClump.Select(obs => this.shapeToIdMap[obs.InputShape]).OrderBy(id => id).ToArray();
                var fileClump = this.shapeIdToClumpMap[routerSiblings.First()];
                fileClump.RouterAccretion = routerClump;
                VerifyOrderedSiblingLists(fileClump.SiblingIds, routerSiblings);    // SiblingIds are already ordered
            }
        }

        private void VerifyThatAllFileClumpsAreInRouter(RectilinearEdgeRouterWrapper router)
        {
            foreach (var fileClump in this.clumpIdToAccretionMap.Values)
            {
                var firstSibling = this.idToShapeMap[fileClump.SiblingIds.First()];
                var firstObstacle = router.ShapeToObstacleMap[firstSibling];

                // We've already verified all the router clumps, so we now just need to know that we do have a router clump.
                Validate.AreSame(fileClump.RouterAccretion, firstObstacle.Clump, "Clump from file was not found in router");
            }
        }

        internal void VerifyConvexHulls(RectilinearEdgeRouterWrapper router)
        {
            if (this.convexHullIdToAccretionMap.Count == 0) 
            {
                return;
            }
            VerifyThatAllRouterHullsAreInFile(router);
            VerifyThatAllFileHullsAreInRouter(router);
        }

        private void VerifyThatAllRouterHullsAreInFile(RectilinearEdgeRouterWrapper router) {
            foreach (var routerHull in router.ObstacleTree.GetAllPrimaryObstacles().Where(obs => obs.ConvexHull != null).Select(obs => obs.ConvexHull))
            {
                var routerSiblings = routerHull.Obstacles.Select(obs => this.shapeToIdMap[obs.InputShape]).OrderBy(id => id).ToArray();
                var fileHull = this.shapeIdToConvexHullMap[routerSiblings.First()];
                fileHull.RouterAccretion = routerHull;
                VerifyOrderedSiblingLists(fileHull.SiblingIds, routerSiblings);     // SiblingIds are already ordered

                // This may be null if -nowriteConvexHulls
                if (fileHull.Polyline != null)
                {
                    this.VerifyPolylinesAreSame(fileHull.Polyline, routerHull.Polyline);
                }

                // Convex Hulls can exist for both groups and non-groups.  For groups, there should only be one obstacle,
                // the group, in the hull.
                var firstSibling = this.idToShapeMap[routerSiblings.First()];
                if (firstSibling.IsGroup) 
                {
                    Validate.AreEqual(1, routerSiblings.Count(), "only one item should be in a convex hull for a group");
                }
                else
                {
                    Validate.IsFalse(routerSiblings.Any(sib => this.idToShapeMap[sib].IsGroup), "group found with non-groups in a convex hull");
                }
            }
        }

        private void VerifyThatAllFileHullsAreInRouter(RectilinearEdgeRouterWrapper router)
        {
            foreach (var fileHull in this.convexHullIdToAccretionMap.Values)
            {
                var firstSibling = this.idToShapeMap[fileHull.SiblingIds.First()];
                var firstObstacle = router.ShapeToObstacleMap[firstSibling];

                // We've already verified all the router hulls, so we now just need to know that we do have a router hull.
                Validate.AreSame(fileHull.RouterAccretion, firstObstacle.ConvexHull, "Convex hull from file was not found in router");
            }
        }

        private static void VerifyOrderedSiblingLists(List<int> fileSiblings, int[] routerSiblings) 
        {
            Validate.AreEqual(fileSiblings.Count, routerSiblings.Length, "Unequal number of file and router siblings");
            for (int ii = 0; ii < routerSiblings.Length; ++ii) 
            {
                Validate.AreEqual(fileSiblings[ii], routerSiblings[ii], "File and router siblings differ");
            }
        }

        private void ReadVisibilityGraph()
        {
            Match m;
            this.VerifyIsNextLine(RectFileStrings.BeginVisibilityVertices);
            for (;;)
            {
                if (!(m = ParseNextOrDone(RectFileStrings.ParsePoint, RectFileStrings.EndVisibilityVertices)).Success)
                {
                    break;
                }
                var x = double.Parse(m.Groups["X"].ToString());
                var y = double.Parse(m.Groups["Y"].ToString());
                this.VisibilityGraph.AddVertex(ApproximateComparer.Round(new Point(x, y)));
            }

            this.VerifyIsNextLine(RectFileStrings.BeginVisibilityEdges);
            for (;;)
            {
                if (!(m = ParseNextOrDone(RectFileStrings.ParseSegment, RectFileStrings.EndVisibilityEdges)).Success)
                {
                    break;
                }
                var startX = double.Parse(m.Groups["startX"].ToString());
                var startY = double.Parse(m.Groups["startY"].ToString());
                var endX = double.Parse(m.Groups["endX"].ToString());
                var endY = double.Parse(m.Groups["endY"].ToString());
                this.VisibilityGraph.AddEdge(ApproximateComparer.Round(new Point(startX, startY)),
                                             ApproximateComparer.Round(new Point(endX, endY)));
            }
        }

        internal void VerifyVisibilityGraph(RectilinearEdgeRouterWrapper router)
        {
            Validate.AreEqual(this.fileVertexCount, router.VisibilityGraph.VertexCount, "Graph vertex count difference");
            
            // If the vertices and edges were stored to the file, verify them.
            if (0 != this.VisibilityGraph.VertexCount)
            {
                foreach (var fileVertex in this.VisibilityGraph.Vertices())
                {
                    Validate.IsNotNull(router.VisibilityGraph.FindVertex(fileVertex.Point), "Cannot find file vertex in router graph");
                }
                foreach (var routerVertex in router.VisibilityGraph.Vertices())
                {
                    Validate.IsNotNull(this.VisibilityGraph.FindVertex(routerVertex.Point), "Cannot find router vertex in file graph");
                }
                foreach (var fileEdge in this.VisibilityGraph.Edges)
                {
                    Validate.IsNotNull(VisibilityGraph.FindEdge(fileEdge), "Cannot find file edge in router graph");
                }
                foreach (var routerEdge in router.VisibilityGraph.Edges)
                {
                    Validate.IsNotNull(VisibilityGraph.FindEdge(routerEdge), "Cannot find router edge in file graph");
                }
            }
        }

        private void ReadPaths()
        {
            Match m;
            this.VerifyIsNextLine(RectFileStrings.BeginPaths);

            for (;;)
            {
                if (!(m = ParseNextOrDone(RectFileStrings.ParsePathEndpoints, RectFileStrings.EndPaths)).Success)
                {
                    break;
                }
                var sourceId = int.Parse(m.Groups["sourceId"].ToString());
                var targetId = int.Parse(m.Groups["targetId"].ToString());
                Port sourcePort, targetPort;
                Validate.IsTrue(idToPortMap.TryGetValue(sourceId, out sourcePort), CurrentLineError("Can't find source port"));
                Validate.IsTrue(idToPortMap.TryGetValue(targetId, out targetPort), CurrentLineError("Can't find target port"));
                this.VerifyIsNextLine(RectFileStrings.BeginCurve);
                var curve = this.ReadCurve();
                portsToPathMap.Add(new Tuple<Port, Port>(sourcePort, targetPort), curve);
            }
        }

        private void VerifyPaths(RectilinearEdgeRouterWrapper router)
        {
            IEnumerable<EdgeGeometry> routerEdgeGeometries = router.EdgeGeometriesToRoute;
            foreach (EdgeGeometry routerEdgeGeom in routerEdgeGeometries.Where(edgeGeom => null != edgeGeom.Curve))
            {
                Curve fileCurve;
                if (this.portsToPathMap.TryGetValue(new Tuple<Port, Port>(routerEdgeGeom.SourcePort, routerEdgeGeom.TargetPort), out fileCurve))
                {
                    var routerCurve = (Curve)routerEdgeGeom.Curve;      // This is currently always a Curve
                    this.VerifyCurvesAreSame(fileCurve, routerCurve);
                }
            }
        }

        private void VerifyCurvesAreSame(Curve fileCurve, Curve routerCurve)
        {
            var fileSegments = fileCurve.Segments;
            var routerSegments = routerCurve.Segments;
            Validate.AreEqual(fileSegments.Count, routerSegments.Count, "Unequal Curve segment counts");
            for (int ii = 0; ii < fileSegments.Count; ++ii)
            {
                Validate.IsTrue(comparer.IsClose(fileSegments[ii].Start, routerSegments[ii].Start), "Unequal Curve segment Start");
                Validate.IsTrue(comparer.IsClose(fileSegments[ii].End, routerSegments[ii].End), "Unequal Curve segment End");
            }
        }

        public void VerifyRouter(RectilinearEdgeRouterWrapper router)
        {
            if (router.WantVerify)
            {
                VerifyVisibilityGraph(router);
                VerifyPaths(router);
            }
        }

        private Curve ReadCurve()
        {
            var c = new Curve();
            Match m;
            while ((m = ParseNextOrDone(RectFileStrings.ParseSegment, RectFileStrings.EndCurve)).Success)
            {
                c.AddSegment(LoadLineSegment(m));
            }
            return c;
        }

        private static LineSegment LoadLineSegment(Match m)
        {
            var startX = double.Parse(m.Groups["startX"].ToString());
            var startY = double.Parse(m.Groups["startY"].ToString());
            var endX = double.Parse(m.Groups["endX"].ToString());
            var endY = double.Parse(m.Groups["endY"].ToString());
            return new LineSegment(new Point(startX, startY), new Point(endX, endY));
        }

        private Ellipse ReadEllipse() 
        {
            Match m = ParseNext(RectFileStrings.ParseEllipse);
            var axisAx = double.Parse(m.Groups["axisAx"].ToString());
            var axisAy = double.Parse(m.Groups["axisAy"].ToString());
            var axisBx = double.Parse(m.Groups["axisBx"].ToString());
            var axisBy = double.Parse(m.Groups["axisBy"].ToString());
            var centerX = double.Parse(m.Groups["centerX"].ToString());
            var centerY = double.Parse(m.Groups["centerY"].ToString());
            VerifyIsNextLine(RectFileStrings.EndEllipse);
            return new Ellipse(new Point(axisAx, axisAy),
                             new Point(axisBx, axisBy), new Point(centerX, centerY));
        }

        private RoundedRect ReadRoundedRect()
        {
            Match m = ParseNext(RectFileStrings.ParseRoundedRect);
            var x = double.Parse(m.Groups["X"].ToString());
            var y = double.Parse(m.Groups["Y"].ToString());
            var width = double.Parse(m.Groups["width"].ToString());
            var height = double.Parse(m.Groups["height"].ToString());
            var radiusX = double.Parse(m.Groups["radiusX"].ToString());
            var radiusY = double.Parse(m.Groups["radiusY"].ToString());
            VerifyIsNextLine(RectFileStrings.EndRoundedRect);
            return new RoundedRect(new Rectangle(x, y, x + width, y + height), radiusX, radiusY);
        }

        private Polyline ReadPolyline()
        {
            var p = new Polyline();
            Match m;
            while ((m = ParseNextOrDone(RectFileStrings.ParsePoint, RectFileStrings.EndPolyline)).Success)
            {
                var x = double.Parse(m.Groups["X"].ToString());
                var y = double.Parse(m.Groups["Y"].ToString());
                p.AddPoint(new Point(x, y));
            }
            p.Closed = true;
            return p;
        }

        private void NextLine()
        {
            while ((this.currentLine = this.inputFileReader.ReadLine()) != null)
            {
                ++this.currentLineNumber;
                if (this.currentLine.StartsWith("//", StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }
                this.currentLine = this.currentLine.Trim();
                if (string.IsNullOrEmpty(this.currentLine))
                {
                    continue;
                }
                return;
            }

            // We only call this when we expect a line.
            Validate.Fail("Unexpected EOF in source file");
        }

        private Match ParseNext(Regex rgx)
        {
            NextLine();
            return Parse(rgx);
        }

        private Match Parse(Regex rgx)
        {
            Match m = rgx.Match(this.currentLine);
            if (!m.Success)
            {
                Validate.Fail(CurrentLineError("Unexpected Parse mismatch"));
            }
            return m;
        }

        private Match TryParseNext(Regex rgx)
        {
            NextLine();
            return TryParse(rgx);
        }

        private Match TryParse(Regex rgx)
        {
            Match m = rgx.Match(this.currentLine);
            return m;
        }

        private Match ParseNextOrDone(Regex rgx, string strDone)
        {
            NextLine();
            return ParseOrDone(rgx, strDone);
        }

        private Match ParseOrDone(Regex rgx, string strDone)
        {
            Match m = rgx.Match(this.currentLine);
            if (m.Success)
            {
                return m;
            }
            if (!this.IsLine(strDone))
            {
                Validate.Fail(CurrentLineError("Unexpected ParseOrDone mismatch"));
            }
            return m;
        }

        private int ParseNextId()
        {
            NextLine();
            return ParseId();
        }

        private int ParseId()
        {
            int id;
            if (!TryParseId(out id))
            {
                Validate.Fail(CurrentLineError("Unexpected ParseNextId mismatch"));
            }
            return id;
        }

        private bool TryParseNextId(out int id)
        {
            this.NextLine();
            return TryParseId(out id);
        }

        private bool TryParseId(out int id)
        {
            Match m = TryParse(RectFileStrings.ParseId);
            id = InvalidId;
            if (!m.Success)
            {
                return false;
            }
            id = int.Parse(m.Groups["id"].ToString());
            return true;
        }

        private void VerifyIsNextLine(string strTest)
        {
            NextLine();
            VerifyIsLine(strTest);
        }

        private void VerifyIsLine(string strTest)
        {
            if (!IsLine(strTest))
            {
                Validate.Fail(CurrentLineError("Unexpected mismatch: expected {0}", strTest));
            }
        }

        private bool IsNextLine(string strTest) 
        {
            NextLine();
            return IsLine(strTest);
        }

        private bool IsLine(string strTest) 
        {
            return IsString(strTest, this.currentLine);
        }

        private static bool IsString(string strWant, string strTest)
        {
            return 0 == string.Compare(strWant, strTest, StringComparison.CurrentCultureIgnoreCase);
        }

        private string CurrentLineError(string format, params object[] args)
        {
            var details = string.Format(format, args);
            return string.Format("{0} on line {1}: {2}", details, this.currentLineNumber, this.currentLine);
        }

        #region IDisposable Members

        public void Dispose() 
        {
            if (null != this.inputFileReader)
            {
                this.inputFileReader.Dispose();
                this.inputFileReader = null;
            }
        }

        #endregion
    }
}
