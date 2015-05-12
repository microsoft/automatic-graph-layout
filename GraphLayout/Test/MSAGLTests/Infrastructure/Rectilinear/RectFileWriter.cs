// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RectFileWriter.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Path = System.IO.Path;

namespace Microsoft.Msagl.UnitTests.Rectilinear 
{
    using System.Collections.Generic;

    using Routing;
    using Routing.Rectilinear;

    internal class RectFileWriter : IDisposable
    {
        private readonly RectilinearEdgeRouterWrapper router;
        private readonly RectilinearVerifier verifier;

        // If the writer is instantiated by TestRectilinear it will set these.
        // Otherwise they have their default values in the header and only the
        // verifier information is of interest.
        internal int InitialSeed { get; set; }
        internal string RandomObstacleArg { get; set; }
        internal int RandomObstacleCount { get; set; }
        internal string RandomObstacleDensity { get; set; }
        internal bool RandomObstacleRotate { get; set; }

        // For writing out the relationship between free relative ports and their shapes.
        private readonly Dictionary<Port, Shape> freeRelativePortToShapeMap;

        private StreamWriter outputFileWriter;
        private readonly Dictionary<Port, int> portToIdMap = new Dictionary<Port, int>();
        private readonly Dictionary<Shape, int> shapeToIdMap = new Dictionary<Shape, int>();

        // For large files we may not want to write N^2 vertices and edges; we'll just write and verify the counts.
        private readonly bool writeGraph;

        // In some cases we may not want one or more of paths, padded obstacles, or scan segments.
        private readonly bool writePaths;
        private readonly bool writeScanSegments;
        private readonly bool writePaddedObstacles;
        private readonly bool writeConvexHulls;

        private readonly bool useFreePortsForObstaclesPorts;
        private readonly bool useSparseVisibilityGraph;
        private readonly bool useObstacleRectangles;
        private readonly double bendPenalty;
        private readonly bool limitPortVisibilitySpliceToEndpointBoundingBox;

        private const int FreePortShapeId = -1;

        private int nextPortId;     // post-increment so >= 0 is valid

        private int NextPortId { get { return nextPortId++; } }

        private readonly Dictionary<Clump, int> clumpToIdMap = new Dictionary<Clump, int>();
        private readonly Dictionary<OverlapConvexHull, int> convexHullToIdMap = new Dictionary<OverlapConvexHull, int>();
        private int nextClumpId = 10000;        // For easier debugging, start here
        private int nextConvexHullId = 20000;   // For easier debugging, start here
        private int GetNextClumpId() { return this.nextClumpId++; }
        private int GetNextConvexHullId() { return this.nextConvexHullId++; }

        internal RectFileWriter(RectilinearEdgeRouterWrapper router,
                RectilinearVerifier verifier,
                Dictionary<Port, Shape> freeRelativePortToShapeMap = null,
                bool writeGraph = true,
                bool writePaths = true,
                bool writeScanSegments = true,
                bool writePaddedObstacles = true,
                bool writeConvexHulls = true,
                bool useFreePortsForObstaclePorts = false
            ) 
        {
            this.router = router;
            this.verifier = verifier;
            this.freeRelativePortToShapeMap = freeRelativePortToShapeMap;

            this.writeGraph = writeGraph;
            this.writePaths = writePaths;
            this.writeScanSegments = writeScanSegments;
            this.writePaddedObstacles = writePaddedObstacles;
            this.writeConvexHulls = writeConvexHulls;

            this.useFreePortsForObstaclesPorts = useFreePortsForObstaclePorts;
            this.useSparseVisibilityGraph = router.UseSparseVisibilityGraph;
            this.useObstacleRectangles = router.UseObstacleRectangles;
            this.bendPenalty = router.BendPenaltyAsAPercentageOfDistance;
            this.limitPortVisibilitySpliceToEndpointBoundingBox = router.LimitPortVisibilitySpliceToEndpointBoundingBox;

            // Don't leave these null - they'll be overwritten by caller in most cases.
            this.RandomObstacleArg = RectFileStrings.NullStr;
            this.RandomObstacleDensity = RectFileStrings.NullStr;
        }

        internal void WriteFile(string fileName, string commandLine = null)
        {
            this.outputFileWriter = new StreamWriter(Path.GetFullPath(fileName));

            WriteCommandLineArgs(commandLine);

            // Prepopulate the shape-to-obstacle map so we can write children and order by shape Id.
            AssignShapeIds();

            // Input parameters and output summary
            WriteHeader();

            // Input detail
            WriteInputObstacles();
            WritePorts();
            WriteRoutingSpecs();

            // Output detail
            WritePaddedObstacles();
            WriteConvexHulls();
            WriteScanSegments();
            WriteVisibilityGraph();
            WritePaths();

            // Done.
            this.outputFileWriter.Flush();
            this.outputFileWriter.Close();
        }

        private void WriteCommandLineArgs(string commandLine)
        {
            // First thing to write is the commandline args.
            // If null != commandLine we're recreating an existing test file.
            if (null != commandLine)
            {
                // Some previously-generated files have " -quiet" which we don't want
                const string spaceAndQuiet = " -quiet";
                var index = commandLine.IndexOf(spaceAndQuiet, StringComparison.OrdinalIgnoreCase);
                if (index >= 0) {
                    commandLine = commandLine.Remove(index, spaceAndQuiet.Length);
                }
                this.outputFileWriter.WriteLine("// " + commandLine);
            }
            else
            {
                // Skip both program name and the outfile specification.  Quote any space-containing args.
                // This cmdline should be usable from cmdline as well as -updateFile.
                this.outputFileWriter.Write("//");
                var args = Environment.GetCommandLineArgs();
                var space = new[] {' '};
                for (int ii = 0; ii < args.Length; ++ii)
                {
                    if (0 == ii)
                    {
                        // Program name
                        continue;
                    }
                    var arg = args[ii];
                    if (0 == string.Compare("-outFile", arg, StringComparison.OrdinalIgnoreCase))
                    {
                        // Skip current and next arg.
                        ++ii;
                        continue;
                    }
                    if (0 == string.Compare("-quiet", arg, StringComparison.OrdinalIgnoreCase))
                    {
                        // Skip current arg.
                        continue;
                    }

                    if (arg.IndexOfAny(space) >= 0)
                    {
                        this.outputFileWriter.Write(" \"" + arg + "\"");
                        continue;
                    }
                    this.outputFileWriter.Write(" " + arg);
                }
                this.outputFileWriter.WriteLine();
            }
        }

        private void AssignShapeIds()
        {
            // Assign shape Ids in the ordinal order so the -ports option to TestRectilinear works.
            int id = 0;
            foreach (var shape in this.router.Obstacles)
            {
                // Post-increment so >= 0 is valid.
                this.shapeToIdMap.Add(shape, id++);
            }
        }

        private void WriteHeader()
        {
            // Input parameters
            this.outputFileWriter.WriteLine(RectFileStrings.WriteSeed, this.InitialSeed.ToString("X"));
            this.outputFileWriter.WriteLine(RectFileStrings.WriteRandomArgs,
                this.RandomObstacleArg,
                this.RandomObstacleCount,
                this.RandomObstacleDensity,
                this.RandomObstacleRotate);
            this.outputFileWriter.WriteLine(RectFileStrings.WritePadding, this.verifier.RouterPadding);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteEdgeSeparation, this.verifier.RouterEdgeSeparation);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteRouteToCenter, this.verifier.RouteToCenterOfObstacles);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteArrowheadLength, this.verifier.RouterArrowheadLength);

            // New stuff
            this.outputFileWriter.WriteLine(RectFileStrings.WriteUseFreePortsForObstaclePorts, this.useFreePortsForObstaclesPorts);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteUseSparseVisibilityGraph, this.useSparseVisibilityGraph);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteUseObstacleRectangles, this.useObstacleRectangles);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteBendPenalty, this.bendPenalty);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteLimitPortVisibilitySpliceToEndpointBoundingBox, this.limitPortVisibilitySpliceToEndpointBoundingBox);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteWantPaths, this.verifier.WantPaths);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteWantNudger, this.verifier.WantNudger);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteWantVerify, this.verifier.WantVerify);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteStraightTolerance, this.verifier.StraightTolerance);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteCornerTolerance, this.verifier.CornerTolerance);

            // Output summary            
        }

        private void WriteInputObstacles()
        {
            this.outputFileWriter.WriteLine();
            this.outputFileWriter.WriteLine(RectFileStrings.BeginUnpaddedObstacles);

            foreach (var shape in this.router.Obstacles.OrderBy(shape => shapeToIdMap[shape]))
            {
                WriteInputShape(shape);
                WriteChildren(shape.Children);
                var obstacle = router.ShapeToObstacleMap[shape];
                this.WriteClumpId(obstacle);
                this.WriteConvexHullId(obstacle);
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndUnpaddedObstacles);
        }

        private void WriteInputShape(Shape shape) 
        {
            var curve = shape.BoundaryCurve as Curve;
            if (null != curve)
            {
                this.WriteCurve(shape, curve);
                return;
            }
            
            var ellipse = shape.BoundaryCurve as Ellipse;
            if (null != ellipse)
            {
                this.WriteEllipse(shape, ellipse);
                return;
            }

            var roundRect = shape.BoundaryCurve as RoundedRect;
            if (null != roundRect)
            {
                this.WriteRoundedRect(shape, roundRect);
                return;
            }

            // this must be a polyline.
            var poly = (Polyline)shape.BoundaryCurve; 
            this.WritePolyline(shape, poly);
        }

        private void WriteCurve(Shape shape, Curve curve)
        {
            this.outputFileWriter.WriteLine(RectFileStrings.BeginCurve);
            if (null != shape)
            {
                // If a path, this is null
                this.outputFileWriter.WriteLine(RectFileStrings.WriteId, shapeToIdMap[shape]);
            }
            foreach (var seg in curve.Segments)
            {
                this.outputFileWriter.WriteLine(RectFileStrings.WriteSegment, seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y);
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndCurve);
        }

        private void WriteEllipse(Shape shape, Ellipse ellipse)
        {
            this.outputFileWriter.WriteLine(RectFileStrings.BeginEllipse);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteId, shapeToIdMap[shape]);
            this.outputFileWriter.WriteLine(
                RectFileStrings.WriteEllipse,
                ellipse.AxisA.X,
                ellipse.AxisA.Y,
                ellipse.AxisB.X,
                ellipse.AxisB.Y,
                ellipse.Center.X,
                ellipse.Center.Y);
            this.outputFileWriter.WriteLine(RectFileStrings.EndEllipse);
        }

        private void WriteRoundedRect(Shape shape, RoundedRect roundRect)
        {
            this.outputFileWriter.WriteLine(RectFileStrings.BeginRoundedRect);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteId, shapeToIdMap[shape]);
            this.outputFileWriter.WriteLine(
                RectFileStrings.WriteRoundedRect,
                roundRect.BoundingBox.Left,
                roundRect.BoundingBox.Bottom,
                roundRect.BoundingBox.Width,
                roundRect.BoundingBox.Height,
                roundRect.RadiusX,
                roundRect.RadiusY);
            this.outputFileWriter.WriteLine(RectFileStrings.EndRoundedRect);
        }

        private void WritePolyline(Shape shape, Polyline poly)
        {
            this.outputFileWriter.WriteLine(RectFileStrings.BeginPolyline);
            this.outputFileWriter.WriteLine(RectFileStrings.WriteId, shapeToIdMap[shape]);
            foreach (var point in poly)
            {
                this.outputFileWriter.WriteLine(RectFileStrings.WritePoint, point.X, point.Y);
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndPolyline);
        }

        private void WriteChildren(IEnumerable<Shape> children)
        {
            // A group is a shape with children.
            if ((null == children) || !children.Any())
            {
                return;
            }
            this.outputFileWriter.WriteLine(RectFileStrings.Children);
            foreach (var child in children)
            {
                this.outputFileWriter.WriteLine(RectFileStrings.WriteId, shapeToIdMap[child]);
            }
        }

        private void WriteClumpId(Obstacle obstacle)
        {
            if (!obstacle.IsOverlapped) 
            {
                return;
            }
            int id;
            if (!this.clumpToIdMap.TryGetValue(obstacle.Clump, out id))
            {
                id = this.GetNextClumpId();
                this.clumpToIdMap[obstacle.Clump] = id;
            }
            this.outputFileWriter.WriteLine(RectFileStrings.WriteClumpId, id);
        }

        private void WriteConvexHullId(Obstacle obstacle)
        {
            // This writes for both group and non-group.
            if (!obstacle.IsInConvexHull)
            {
                return;
            }
            int id;
            if (!this.convexHullToIdMap.TryGetValue(obstacle.ConvexHull, out id))
            {
                id = this.GetNextConvexHullId();
                this.convexHullToIdMap[obstacle.ConvexHull] = id;
            }
            this.outputFileWriter.WriteLine(RectFileStrings.WriteConvexHullId, id);
        }

        private void WritePorts()
        {
            this.outputFileWriter.WriteLine();
            this.outputFileWriter.WriteLine(RectFileStrings.BeginPorts);

            foreach (var shape in this.router.Obstacles.Where(shape => null != shape.Ports))
            {
                foreach (var port in shape.Ports)
                {
                    WriteObstaclePort(shape, port);
                }
            }

            foreach (var edgeGeom in this.router.EdgeGeometriesToRoute)
            {
                WriteIfFreePort(edgeGeom.SourcePort);
                WriteIfFreePort(edgeGeom.TargetPort);
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndPorts);
        }

        private void WriteObstaclePort(Shape shape, Port port)
        {
            Validate.IsFalse(portToIdMap.ContainsKey(port), "Duplicate entries for port in the same or different shape.Ports list(s)");
            var relativePort = port as RelativeFloatingPort;
            if (null != relativePort)
            {
                Validate.IsTrue(PointComparer.Equal(shape.BoundingBox.Center, relativePort.CenterDelegate()),
                                "Port CenterDelegate must be Shape.Center for file persistence");
                Validate.AreEqual(shape.BoundaryCurve, port.Curve, "Port curve is not the same as that of the Shape in which it is a member");
            }
            portToIdMap.Add(port, NextPortId);
            WritePort(port, shapeToIdMap[shape]);
        }

        private void WriteIfFreePort(Port port)
        {
            // Skip this port if we've already processed it.
            if (portToIdMap.ContainsKey(port))
            {
                return;
            }

            int shapeId = FreePortShapeId;
            if (port is RelativeFloatingPort)
            {
                Shape shape;
                if ((null == this.freeRelativePortToShapeMap) || !this.freeRelativePortToShapeMap.TryGetValue(port, out shape))
                {
                    Validate.Fail("Relative FreePorts must be in FreeRelativePortToShapeMap");
                    return;
                }
                shapeId = shapeToIdMap[shape];
            }

            portToIdMap.Add(port, NextPortId);
            WritePort(port, shapeId);
        }

        private void WritePort(Port port, int shapeId)
        {
            if (typeof(FloatingPort) == port.GetType())
            {
                this.outputFileWriter.WriteLine(RectFileStrings.WritePort, RectFileStrings.Floating,
                            port.Location.X, port.Location.Y, portToIdMap[port], shapeId);
            }
            else
            {
                WriteRelativePort(port, shapeId);
            }

            WritePortEntries(port);
        }

        private void WriteRelativePort(Port port, int shapeId)
        {
            var relativePort = port as RelativeFloatingPort;
            var multiPort = port as MultiLocationFloatingPort;
            Validate.IsNotNull(relativePort, "Unsupported port type for file persistence");
            // ReSharper disable PossibleNullReferenceException

            // The location in the main WritePort line is relevant only for non-multiPort; for multiPorts
            // the caller sets location via ActiveOffsetIndex.
            var portTypeString = (null != multiPort) ? RectFileStrings.Multi : RectFileStrings.Relative;
            this.outputFileWriter.WriteLine(RectFileStrings.WritePort, portTypeString,
                    relativePort.LocationOffset.X, relativePort.LocationOffset.Y,
                    this.portToIdMap[port], shapeId);
            // ReSharper restore PossibleNullReferenceException

            if (null != multiPort)
            {
                this.outputFileWriter.WriteLine(RectFileStrings.WriteMultiPortOffsets, multiPort.ActiveOffsetIndex);
                foreach (var offset in multiPort.LocationOffsets)
                {
                    this.outputFileWriter.WriteLine(RectFileStrings.WritePoint, offset.X, offset.Y);
                }
            }
        }

        private void WritePortEntries(Port port)
        {
            if (null == port.PortEntry)
            {
                return;
            }
            var portEntryOnCurve = port.PortEntry as PortEntryOnCurve;
            Validate.IsNotNull(portEntryOnCurve, "Unknown IPortEntry implementation");
            // ReSharper disable PossibleNullReferenceException

            this.outputFileWriter.WriteLine(RectFileStrings.PortEntryOnCurve);
            foreach (var span in portEntryOnCurve.Spans)
            {
                this.outputFileWriter.WriteLine(RectFileStrings.WritePoint, span.Item1, span.Item2);
            }
            // ReSharper restore PossibleNullReferenceException
        }

        private void WriteRoutingSpecs()
        {
            this.outputFileWriter.WriteLine();
            this.outputFileWriter.WriteLine(RectFileStrings.BeginRoutingSpecs);
            foreach (var geom in this.router.EdgeGeometriesToRoute)
            {
                var arrowhead = geom.SourceArrowhead ?? geom.TargetArrowhead;
                var arrowheadLength = (null == arrowhead) ? 0.0 : arrowhead.Length;
                var arrowheadWidth = (null == arrowhead) ? 0.0 : arrowhead.Width;
                this.outputFileWriter.WriteLine(RectFileStrings.WriteEdgeGeometry,
                        portToIdMap[geom.SourcePort], portToIdMap[geom.TargetPort],
                        null != geom.SourceArrowhead, null != geom.TargetArrowhead, arrowheadLength, arrowheadWidth,
                        geom.LineWidth);
                if (null != geom.Waypoints)
                {
                    this.outputFileWriter.WriteLine(RectFileStrings.Waypoints);
                    foreach (var waypoint in geom.Waypoints)
                    {
                        this.outputFileWriter.WriteLine(RectFileStrings.WritePoint, waypoint.X, waypoint.Y);
                    }
                }
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndRoutingSpecs);
        }

        private void WritePaddedObstacles()
        {
            this.outputFileWriter.WriteLine();
            this.outputFileWriter.WriteLine(RectFileStrings.BeginPaddedObstacles);
            if (this.writePaddedObstacles)
            {
                foreach (Obstacle obstacle in this.router.ObstacleTree.GetAllObstacles().OrderBy(obstacle => shapeToIdMap[obstacle.InputShape]))
                {
                    this.outputFileWriter.WriteLine(RectFileStrings.BeginPolyline);
                    this.outputFileWriter.WriteLine(RectFileStrings.WriteId, shapeToIdMap[obstacle.InputShape]);
                    foreach (Point point in obstacle.PaddedPolyline)
                    {
                        this.outputFileWriter.WriteLine(RectFileStrings.WritePoint, point.X, point.Y);
                    }
                    this.outputFileWriter.WriteLine(RectFileStrings.EndPolyline);
                }
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndPaddedObstacles);
        }

        private void WriteConvexHulls()
        {
            this.outputFileWriter.WriteLine();
            this.outputFileWriter.WriteLine(RectFileStrings.BeginConvexHulls);
            if (this.writeConvexHulls)
            {
                foreach (var hullIdPair in this.convexHullToIdMap)
                {
                    this.outputFileWriter.WriteLine(RectFileStrings.BeginPolyline);
                    this.outputFileWriter.WriteLine(RectFileStrings.WriteId, hullIdPair.Value);
                    foreach (Point point in hullIdPair.Key.Polyline)
                    {
                        this.outputFileWriter.WriteLine(RectFileStrings.WritePoint, point.X, point.Y);
                    }
                    this.outputFileWriter.WriteLine(RectFileStrings.EndPolyline);
                }
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndConvexHulls);
        }

        private void WriteScanSegments()
        {
            this.outputFileWriter.WriteLine();
            this.outputFileWriter.WriteLine(RectFileStrings.BeginHScanSegments);
            if (this.writeScanSegments)
            {
                // These are already ordered as desired in the tree.
                foreach (var scanSeg in this.router.HorizontalScanLineSegments)
                {
                    this.outputFileWriter.WriteLine(
                        RectFileStrings.WriteSegment, scanSeg.Start.X, scanSeg.Start.Y, scanSeg.End.X, scanSeg.End.Y);
                }
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndHScanSegments);

            this.outputFileWriter.WriteLine();
            this.outputFileWriter.WriteLine(RectFileStrings.BeginVScanSegments);
            if (this.writeScanSegments)
            {
                // These are already ordered as desired in the tree.
                foreach (var scanSeg in this.router.VerticalScanLineSegments)
                {
                    this.outputFileWriter.WriteLine(
                        RectFileStrings.WriteSegment, scanSeg.Start.X, scanSeg.Start.Y, scanSeg.End.X, scanSeg.End.Y);
                }
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndVScanSegments);
        }

        private void WriteVisibilityGraph()
        {
            this.outputFileWriter.WriteLine();
            this.outputFileWriter.WriteLine(RectFileStrings.BeginVisibilityVertices);

            // Put in the begin/end even if !writeGraph, to make reading easier.
            if (writeGraph)
            {
                foreach (Point vertexPoint in
                        this.router.VisibilityGraph.Vertices().Select(v => v.Point).OrderBy(pt => pt, new VertexPointOrderer()))
                {
                    this.outputFileWriter.WriteLine(RectFileStrings.WritePoint, vertexPoint.X, vertexPoint.Y);
                }
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndVisibilityVertices);

            this.outputFileWriter.WriteLine();
            this.outputFileWriter.WriteLine(RectFileStrings.BeginVisibilityEdges);
            if (writeGraph)
            {
                foreach (LineSegment edgeSegment in
                        this.router.VisibilityGraph.Edges.Select(edge => new LineSegment(edge.SourcePoint, edge.TargetPoint)).
                                OrderBy(seg => seg, new SegmentOrderer(new TestPointComparer())))
                {
                    this.outputFileWriter.WriteLine(
                        RectFileStrings.WriteSegment, edgeSegment.Start.X, edgeSegment.Start.Y, edgeSegment.End.X, edgeSegment.End.Y);
                }
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndVisibilityEdges);
        }

        private void WritePaths()
        {
            this.outputFileWriter.WriteLine();
            this.outputFileWriter.WriteLine(RectFileStrings.BeginPaths);

            // Put in the begin/end even if !writePaths, to make reading easier.
            if (writePaths)
            {
                foreach (var geom in
                        this.router.EdgeGeometriesToRoute.Where(geom => null != geom.Curve).OrderBy(
                            geom => geom, new EdgeGeometryOrderer(portToIdMap)))
                {
                    this.outputFileWriter.WriteLine(
                        RectFileStrings.WritePathEndpoints, portToIdMap[geom.SourcePort], portToIdMap[geom.TargetPort]);
                    var curve = (Curve)geom.Curve; // this is currently always a Curve.
                    WriteCurve(/*shape:*/ null, curve);
                }
            }
            this.outputFileWriter.WriteLine(RectFileStrings.EndPaths);
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (null != this.outputFileWriter)
            {
                this.outputFileWriter.Dispose();
                this.outputFileWriter = null;
            }
        }

        #endregion
    }
}
