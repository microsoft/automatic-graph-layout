/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
//
// ScanDirection.cs
// MSAGL ScanDirection class for Rectilinear Edge Routing line generation.
//
// Copyright Microsoft Corporation.
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
    // Encapsulate a direction and some useful values derived therefrom.
    internal class ScanDirection {
        // The direction of primary interest, either the direction of the sweep (the
        // coordinate the scanline sweeps "up" in) or along the scan line ("sideways"
        // to the sweep direction, scanning for obstacles).
        internal Directions Direction { get; private set; }
        internal Point DirectionAsPoint { get; private set; }

        // The perpendicular direction - opposite of comments for Direction.
        internal Directions PerpDirection { get; private set; }
        internal Point PerpDirectionAsPoint { get; private set; }

        // The oppposite direction of the primary direction.
        internal Directions OppositeDirection { get; private set; }

        // Use the internal static xxxInstance properties to get an instance.
        private ScanDirection(Directions directionAlongScanLine) {
            System.Diagnostics.Debug.Assert(StaticGraphUtility.IsAscending(directionAlongScanLine),
                "directionAlongScanLine must be ascending");
            Direction = directionAlongScanLine;
            DirectionAsPoint = CompassVector.ToPoint(Direction);
            PerpDirection = (Directions.North == directionAlongScanLine) ? Directions.East : Directions.North;
            PerpDirectionAsPoint = CompassVector.ToPoint(PerpDirection);
            OppositeDirection = CompassVector.OppositeDir(directionAlongScanLine);
        }

        internal bool IsHorizontal { get { return Directions.East == Direction; } }
        internal bool IsVertical { get { return Directions.North == Direction; } }

        // Compare in perpendicular direction first, then parallel direction.
        internal int Compare(Point lhs, Point rhs) {
            int cmp = ComparePerpCoord(lhs, rhs);
            return (0 != cmp) ? cmp : CompareScanCoord(lhs, rhs);
        }

        internal int CompareScanCoord(Point lhs, Point rhs) {
            return PointComparer.Compare((lhs - rhs) * DirectionAsPoint, 0.0);
        }
        internal int ComparePerpCoord(Point lhs, Point rhs) {
            return PointComparer.Compare((lhs - rhs) * PerpDirectionAsPoint, 0.0);
        }

        internal bool IsEqualScanCoord(Point first, Point second) {
            return 0 == CompareScanCoord(first, second);
        }
        internal bool IsEqualPerpCoord(Point first, Point second) {
            return 0 == ComparePerpCoord(first, second);
        }

        internal bool IsFlat(SegmentBase seg) {
            return IsFlat(seg.Start, seg.End);
        }
        internal bool IsFlat(Point start, Point end) {
            // Return true if there is no change in the perpendicular direction.
            return PointComparer.Equal((end - start) * PerpDirectionAsPoint, 0.0);
        }
        internal bool IsPerpendicular(SegmentBase seg) {
            return IsPerpendicular(seg.Start, seg.End);
        }
        internal bool IsPerpendicular(Point start, Point end) {
            // Return true if there is no change in the primary direction.
            return PointComparer.Equal((end - start) * DirectionAsPoint, 0.0);
        }

        internal Point Mask(Point point) {
            return new Point(point.X * DirectionAsPoint.X, point.Y * DirectionAsPoint.Y);
        }
        internal Point PerpMask(Point point) {
            return new Point(point.X * PerpDirectionAsPoint.X, point.Y * PerpDirectionAsPoint.Y);
        }

        internal double Coord(Point point) {
            return point * DirectionAsPoint;
        }
        internal double PerpCoord(Point point) {
            return point * PerpDirectionAsPoint;
        }

        internal Point Min(Point first, Point second) {
            return (Compare(first, second) <= 0) ? first : second;
        }
        internal Point Max(Point first, Point second) {
            return (Compare(first, second) >= 0) ? first : second;
        }

// ReSharper disable InconsistentNaming
        private static readonly ScanDirection horizontalInstance = new ScanDirection(Directions.East);
        private static readonly ScanDirection verticalInstance = new ScanDirection(Directions.North);
// ReSharper restore InconsistentNaming
        internal static ScanDirection HorizontalInstance { get { return horizontalInstance; } }
        internal static ScanDirection VerticalInstance { get { return verticalInstance; } }
        internal ScanDirection PerpendicularInstance { get { return IsHorizontal ? VerticalInstance : HorizontalInstance; } }
        static internal ScanDirection GetInstance(Directions dir) { return StaticGraphUtility.IsVertical(dir) ? VerticalInstance : HorizontalInstance; }

        /// <summary/>
        public override string ToString() {
            return Direction.ToString();
        }
    }
}