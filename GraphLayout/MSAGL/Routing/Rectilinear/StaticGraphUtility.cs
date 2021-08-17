//
// StaticGraphUtility.cs
// MSAGL class for static utility functions for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
#if TEST_MSAGL
using Microsoft.Msagl.DebugHelpers;
#endif // TEST_MSAGL
using Microsoft.Msagl.Routing.Rectilinear.Nudging;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
    // Static utilities for the visibility graph.
    internal static class StaticGraphUtility {
        // Determine the direction of an edge.
        static internal Direction EdgeDirection(VisibilityEdge edge) {
            return EdgeDirection(edge.Source, edge.Target);
        }
        static internal Direction EdgeDirection(VisibilityVertex source, VisibilityVertex target) {
            return PointComparer.GetPureDirection(source.Point, target.Point);
        }

        static internal VisibilityVertex GetEdgeEnd(VisibilityEdge edge, Direction dir) {
            Direction edgeDir = EdgeDirection(edge);
            Debug.Assert(0 != (dir & (edgeDir | CompassVector.OppositeDir(edgeDir))), "dir is orthogonal to edge");
            return (dir == edgeDir) ? edge.Target : edge.Source;
        }
        
        static internal VisibilityVertex FindAdjacentVertex(VisibilityVertex a, Direction dir) {
            // This function finds the next vertex in the desired direction relative to the
            // current vertex, not necessarily the edge orientation, hence it does not use
            // EdgeDirection().  This is so the caller can operate on a desired movement
            // direction without having to track whether we're going forward or backward
            // through the In/OutEdge chain.
            foreach (var edge in a.InEdges) {
                
                if (PointComparer.GetPureDirection(a.Point, edge.SourcePoint) == dir) {
                    return edge.Source;
                }
            }

            foreach (var edge in a.OutEdges) {
                if (PointComparer.GetPureDirection(a.Point, edge.TargetPoint) == dir) {
                    return edge.Target;
                }
            }
            return null;
        }

        static internal VisibilityEdge FindAdjacentEdge(VisibilityVertex a, Direction dir) {
            foreach (var edge in a.InEdges) {

                if (PointComparer.GetPureDirection(edge.SourcePoint, a.Point) == dir) {
                    return edge;
                }
            }

            foreach (var edge in a.OutEdges) {
                if (PointComparer.GetPureDirection(a.Point, edge.TargetPoint) == dir) {
                    return edge;
                }
            }
            return null;
        }

        static internal Point FindBendPointBetween(Point sourcePoint, Point targetPoint, Direction finalEdgeDir) {
            Direction targetDir = PointComparer.GetDirections(sourcePoint, targetPoint);
            Debug.Assert(!PointComparer.IsPureDirection(targetDir), "pure direction has no bend");
            Direction firstDir = targetDir & ~finalEdgeDir;
            Debug.Assert(PointComparer.IsPureDirection(firstDir), "firstDir is not pure");

            // Move along the first direction. If the first direction is horizontal then 
            // targetPoint is at the correct horizontal position, and vice-versa.
            return IsVertical(firstDir)
                    ? new Point(sourcePoint.X, targetPoint.Y)
                    : new Point(targetPoint.X, sourcePoint.Y);
        }

        static internal Point SegmentIntersection(Point first, Point second, Point from) {
            Direction dir = PointComparer.GetPureDirection(first, second);
            Point intersect = IsVertical(dir) ? new Point(first.X, from.Y) : new Point(from.X, first.Y);
            return intersect;
        }

        static internal Point SegmentIntersection(SegmentBase seg, Point from) {
            return SegmentIntersection(seg.Start, seg.End, from);
        }

        static internal bool SegmentsIntersect(SegmentBase first, SegmentBase second) {
            Point intersect;
            return SegmentsIntersect(first, second, out intersect);
        }
        static internal bool SegmentsIntersect(SegmentBase first, SegmentBase second, out Point intersect) {
            return IntervalsIntersect(first.Start, first.End, second.Start, second.End, out intersect);
        }
        static internal bool SegmentsIntersect(LineSegment first, LineSegment second, out Point intersect) {
            return IntervalsIntersect(first.Start, first.End, second.Start, second.End, out intersect);
        }
        static internal Point SegmentIntersection(SegmentBase first, SegmentBase second) {
            // Caller expects segments to intersect.
            Point intersect;
            if (!SegmentsIntersect(first, second, out intersect)) {
                Debug.Assert(false, "intersect is not on both segments");
            }
            return intersect;
        }

        static internal bool IntervalsOverlap(SegmentBase first, SegmentBase second) {
            return IntervalsOverlap(first.Start, first.End, second.Start, second.End);
        }

        static internal bool IntervalsOverlap(Point start1, Point end1, Point start2, Point end2) {
            return IntervalsAreCollinear(start1, end1, start2, end2)
                && PointComparer.Compare(start1, end2) != PointComparer.Compare(end1, start2);
        }

        static internal bool IntervalsAreCollinear(Point start1, Point end1, Point start2, Point end2) {
            Debug.Assert(IsVertical(start1, end1) == IsVertical(start2, end2), "segments are not in the same orientation");
            bool vertical = IsVertical(start1, end1);
            if (IsVertical(start2, end2) == vertical) {
                // This handles touching endpoints as well.
                return vertical ? PointComparer.Equal(start1.X, start2.X) : PointComparer.Equal(start1.Y, start2.Y);
            }
            return false;
        }
        static internal bool IntervalsAreSame(Point start1, Point end1, Point start2, Point end2) {
            return PointComparer.Equal(start1, start2) && PointComparer.Equal(end1, end2);
        }
        static internal bool IntervalsIntersect(Point firstStart, Point firstEnd, Point secondStart, Point secondEnd, out Point intersect) {
            Debug.Assert(IsVertical(firstStart, firstEnd) != IsVertical(secondStart, secondEnd), "cannot intersect two parallel segments");
            intersect = SegmentIntersection(firstStart, firstEnd, secondStart);
            return PointIsOnSegment(firstStart, firstEnd, intersect)
                && PointIsOnSegment(secondStart, secondEnd, intersect);
        }

        static internal Point SegmentIntersection(VisibilityEdge edge, Point from) {
            return SegmentIntersection(edge.SourcePoint, edge.TargetPoint, from);
        }

        static internal bool PointIsOnSegment(Point first, Point second, Point test) {
            return PointComparer.Equal(first, test)
                || PointComparer.Equal(second, test)
                || (PointComparer.GetPureDirection(first, test) == PointComparer.GetPureDirection(test, second));
        }
        static internal bool PointIsOnSegment(SegmentBase seg, Point test) {
            return PointIsOnSegment(seg.Start, seg.End, test);
        }

        static internal bool IsVertical(Direction dir) {
            return (0 != (dir & (Direction.North | Direction.South)));
        }
        static internal bool IsVertical(VisibilityEdge edge) {
            return IsVertical(PointComparer.GetPureDirection(edge.SourcePoint, edge.TargetPoint));
        }
        static internal bool IsVertical(Point first, Point second) {
            return IsVertical(PointComparer.GetPureDirection(first, second));
        }

        static internal bool IsVertical(LineSegment seg) {
            return IsVertical(PointComparer.GetPureDirection(seg.Start, seg.End));
        }

        static internal bool IsAscending(Direction dir) {
            return (0 != (dir & (Direction.North | Direction.East)));
        }

        static internal double Slope(Point start, Point end, ScanDirection scanDir) {
            // Find the slope relative to scanline - how much scan coord changes per sweep change.
            Point lineDirAsPoint = end - start;
            return (lineDirAsPoint * scanDir.PerpDirectionAsPoint) / (lineDirAsPoint * scanDir.DirectionAsPoint);
        }

        static internal Tuple<Point, Point> SortAscending(Point a, Point b) {
            Direction dir = PointComparer.GetDirections(a, b);
            Debug.Assert((Direction. None == dir) || PointComparer.IsPureDirection(dir), "SortAscending with impure direction");
            return ((Direction. None == dir) || IsAscending(dir)) ? new Tuple<Point, Point>(a, b) : new Tuple<Point, Point>(b, a);
        }

        static internal Point RectangleBorderIntersect(Rectangle boundingBox, Point point, Direction dir) {
            switch (dir) {
                case Direction.North:
                case Direction.South:
                    return new Point(point.X, GetRectangleBound(boundingBox, dir));
                case Direction.East:
                case Direction.West:
                    return new Point(GetRectangleBound(boundingBox, dir), point.Y);
                default:
                    throw new InvalidOperationException(
#if TEST_MSAGL
                            "Invalid direction"
#endif // TEST
                        );
            }
        }

        static internal double GetRectangleBound(Rectangle rect, Direction dir) {
            switch (dir) {
                case Direction.North:
                    return rect.Top;
                case Direction.South:
                    return rect.Bottom;
                case Direction.East:
                    return rect.Right;
                case Direction.West:
                    return rect.Left;
                default:
                    throw new InvalidOperationException(
#if TEST_MSAGL
                            "Invalid direction"
#endif // TEST
                        );
            }
        }

        static internal bool RectangleInteriorsIntersect(Rectangle a, Rectangle b) {
            return (PointComparer.Compare(a.Bottom, b.Top) < 0)
                && (PointComparer.Compare(b.Bottom, a.Top) < 0)
                && (PointComparer.Compare(a.Left, b.Right) < 0)
                && (PointComparer.Compare(b.Left, a.Right) < 0);
        }

        static internal bool PointIsInRectangleInterior(Point point, Rectangle rect) {
            return (PointComparer.Compare(point.Y, rect.Top) < 0)
                && (PointComparer.Compare(rect.Bottom, point.Y) < 0)
                && (PointComparer.Compare(point.X, rect.Right) < 0)
                && (PointComparer.Compare(rect.Left, point.X) < 0);
        }

        [Conditional("TEST_MSAGL")]
        static internal void Assert(bool condition, string message, ObstacleTree obstacleTree, VisibilityGraph vg) {
            if (!condition) {
                Test_DumpVisibilityGraph(obstacleTree, vg);
                Debug.Assert(condition, message);
            }
        }

        [Conditional("TEST_MSAGL")]
        static internal void Test_DumpVisibilityGraph(ObstacleTree obstacleTree, VisibilityGraph vg) {
#if TEST_MSAGL
            var debugCurves = Test_GetObstacleDebugCurves(obstacleTree);
            debugCurves.AddRange(Test_GetVisibilityGraphDebugCurves(vg));
            DebugCurveCollection.WriteToFile(debugCurves, GetDumpFileName("VisibilityGraph"));
#endif // TEST
        }

#if DEVTRACE
        static internal void Test_DumpScanSegments(ObstacleTree obstacleTree, ScanSegmentTree hSegs, ScanSegmentTree vSegs) {
            var debugCurves = Test_GetObstacleDebugCurves(obstacleTree);
            debugCurves.AddRange(Test_GetScanSegmentCurves(hSegs));
            debugCurves.AddRange(Test_GetScanSegmentCurves(vSegs));
            DebugCurveCollection.WriteToFile(debugCurves, GetDumpFileName("ScanSegments"));
        }
#endif // DEVTRACE

#if TEST_MSAGL
        static internal List<DebugCurve> Test_GetObstacleDebugCurves(ObstacleTree obstacleTree) {
            return Test_GetObstacleDebugCurves(obstacleTree, false, false);
        }

        static internal List<DebugCurve> Test_GetObstacleDebugCurves(ObstacleTree obstacleTree, bool noPadPoly, bool noVisPoly) {
            return Test_GetObstacleDebugCurves(obstacleTree.GetAllObstacles(), noPadPoly, noVisPoly);
        }

        static internal List<DebugCurve> Test_GetObstacleDebugCurves(IEnumerable<Obstacle> obstacles, bool noPadPoly, bool noVisPoly) {
            var debugCurves = new List<DebugCurve>();
            foreach (var obstacle in obstacles) {
                debugCurves.Add(new DebugCurve(0.1, "darkgray", obstacle.InputShape.BoundaryCurve));
                if (!noPadPoly || obstacle.IsGroup) {
                    debugCurves.Add(obstacle.IsTransparentAncestor
                            ? new DebugCurve(0.3, "gold", obstacle.PaddedPolyline)
                            : new DebugCurve(0.1, obstacle.IsGroup ? "purple" : "black", obstacle.PaddedPolyline));
                }
                if (!noVisPoly && obstacle.IsPrimaryObstacle && (obstacle.VisibilityPolyline != obstacle.PaddedPolyline)) {
                    debugCurves.Add(new DebugCurve(0.1, obstacle.IsGroup ? "mediumpurple" : "lightgray", obstacle.VisibilityPolyline));
                }
            }
            return debugCurves;
        }

        static internal List<DebugCurve> Test_GetVisibilityGraphDebugCurves(VisibilityGraph vg) {
            return vg.Edges.Select(edge => new DebugCurve(0.1,
                        (edge.Weight == ScanSegment.NormalWeight) ? "Blue"
                                : ((edge.Weight == ScanSegment.ReflectionWeight) ? "DarkCyan" : "LightBlue"),
                        new LineSegment(edge.Source.Point, edge.Target.Point))).ToList();

        }

        static internal List<DebugCurve> Test_GetPreNudgedPathDebugCurves(IEnumerable<Path> edgePaths) {
            var debugCurves = new List<DebugCurve>();
            foreach (var path in edgePaths) {
                var points = path.PathPoints.ToArray();
                for (int ii = 0; ii < points.Length - 1; ++ii) {
                    debugCurves.Add(new DebugCurve(0.1, "purple", new LineSegment(points[ii], points[ii + 1])));
                }
            }
            return debugCurves;
        }

        static internal List<DebugCurve> Test_GetPostNudgedPathDebugCurves(IEnumerable<Path> edgePaths) {
            var debugCurves = new List<DebugCurve>();
            foreach (var path in edgePaths) {
                debugCurves.AddRange(path.PathEdges.Select
                        (e => new DebugCurve(0.1, "purple", new LineSegment(e.Source, e.Target))));
            }
            return debugCurves;
        }

        static internal List<DebugCurve> Test_GetScanSegmentCurves(ScanSegmentTree segTree) {
            return segTree.Segments.Select(seg => new DebugCurve(0.2,
                        seg.IsOverlapped ? "Aqua" : (seg.IsReflection ? "LightGreen" : "DarkGreen"),
                        new LineSegment(seg.Start, seg.End))).ToList();
        }

        static internal string GetDumpFileName(string prefix) {
            return System.IO.Path.GetTempPath() + prefix + ".DebugCurves";
        }
#endif // TEST
// ReSharper restore InconsistentNaming

#if TEST_MSAGL
        // Make it easier for floating-point conditional breakpoints in the VS debugger
        // (the docs say they don't need to be non-private but apparently they do).
        static internal bool IsEqualForDebugger(double variable, double want) {
            return (variable < (want + 1.0)) && (variable > (want - 1.0));
        }
        static internal bool IsEqualForDebugger(Point variable, double wantX, double wantY) {
            return IsEqualForDebugger(variable.X, wantX) && IsEqualForDebugger(variable.Y, wantY);
        }
#endif // TEST_MSAGL
    }
}