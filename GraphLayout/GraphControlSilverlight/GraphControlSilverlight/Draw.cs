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
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.DataStructures;
using MsaglRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Color = System.Windows.Media.Color;
using DrawingGraph = Microsoft.Msagl.Drawing.Graph;
using GeometryEdge = Microsoft.Msagl.Core.Layout.Edge;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using MsaglPoint = Microsoft.Msagl.Core.Geometry.Point;
using WinPoint = System.Windows.Point;
using MsaglStyle = Microsoft.Msagl.Drawing.Style;
using MsaglLineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using WinLineSegment = System.Windows.Media.LineSegment;
using WinSize = System.Windows.Size;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    /// <summary>
    /// exposes some drawing functionality
    /// </summary>
    public sealed class Draw
    {
        /// <summary>
        /// private constructor
        /// </summary>
        Draw()
        {
        }

        static double doubleCircleOffsetRatio = 0.9;

        internal static double DoubleCircleOffsetRatio
        {
            get { return doubleCircleOffsetRatio; }
        }


        internal static float dashSize = 0.05f; //inches

        /// <summary>
        /// A color converter
        /// </summary>
        /// <param name="gleeColor"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msagl")]
        public static Color MsaglColorToDrawingColor(Drawing.Color gleeColor)
        {
            return Color.FromArgb(gleeColor.A, gleeColor.R, gleeColor.G, gleeColor.B);
        }

        internal static void DrawUnderlyingPolyline(PathGeometry pg, DEdge edge)
        {
            IEnumerable<WinPoint> points = edge.GeometryEdge.UnderlyingPolyline.Select(p => WinPoint(p));
            PathFigure pf = new PathFigure() { IsFilled = false, IsClosed = false, StartPoint = points.First() };
            foreach (WinPoint p in points)
            {
                if (p != points.First())
                    pf.Segments.Add(new WinLineSegment() { Point = p });
                PathFigure circle = new PathFigure() { IsFilled = false, IsClosed = true, StartPoint = new WinPoint(p.X - edge.RadiusOfPolylineCorner, p.Y) };
                circle.Segments.Add(
                   new ArcSegment()
                   {
                       Size = new WinSize(edge.RadiusOfPolylineCorner, edge.RadiusOfPolylineCorner),
                       SweepDirection = SweepDirection.Clockwise,
                       Point = new WinPoint(p.X + edge.RadiusOfPolylineCorner, p.Y)
                   });
                circle.Segments.Add(
                   new ArcSegment()
                   {
                       Size = new WinSize(edge.RadiusOfPolylineCorner, edge.RadiusOfPolylineCorner),
                       SweepDirection = SweepDirection.Clockwise,
                       Point = new WinPoint(p.X - edge.RadiusOfPolylineCorner, p.Y)
                   });
                pg.Figures.Add(circle);
            }
            pg.Figures.Add(pf);
        }

        internal static void DrawEdgeArrows(PathGeometry pg, DrawingEdge edge, bool fillAtSource, bool fillAtTarget)
        {
            ArrowAtTheEnd(pg, edge, fillAtTarget);
            ArrowAtTheBeginning(pg, edge, fillAtSource);
        }

        private static void ArrowAtTheBeginning(PathGeometry pg, DrawingEdge edge, bool fill)
        {
            if (edge.GeometryEdge != null && edge.Attr.ArrowAtSource)
                DrawArrowAtTheBeginningWithControlPoints(pg, edge, fill);
        }

        private static void DrawArrowAtTheBeginningWithControlPoints(PathGeometry pg, DrawingEdge edge, bool fill)
        {
            if (edge.EdgeCurve != null)
                if (edge.Attr.ArrowheadAtSource == ArrowStyle.None)
                    DrawLine(pg, edge.EdgeCurve.Start, edge.ArrowAtSourcePosition);
                else
                    DrawArrow(pg, edge.EdgeCurve.Start, edge.ArrowAtSourcePosition, edge.Attr.LineWidth, edge.Attr.ArrowheadAtSource, fill);
        }

        private static void ArrowAtTheEnd(PathGeometry pg, DrawingEdge edge, bool fill)
        {
            if (edge.GeometryEdge != null && edge.Attr.ArrowAtTarget)
                DrawArrowAtTheEndWithControlPoints(pg, edge, fill);
        }

        const float toDegrees = 180 / (float)Math.PI;

        static void DrawArrowAtTheEndWithControlPoints(PathGeometry pg, DrawingEdge edge, bool fill)
        {
            if (edge.EdgeCurve != null)
                if (edge.Attr.ArrowheadAtTarget == ArrowStyle.None)
                    DrawLine(pg, edge.EdgeCurve.End,
                             edge.ArrowAtTargetPosition);
                else
                    DrawArrow(pg, edge.EdgeCurve.End,
                              edge.ArrowAtTargetPosition, edge.Attr.LineWidth, edge.Attr.ArrowheadAtTarget, fill);
        }


        internal static WinPoint WinPoint(MsaglPoint p)
        {
            return new WinPoint(p.X, p.Y);
        }

        internal static void CreateGraphicsPathFromCurve(PathFigure pathFigure, Curve curve)
        {
            foreach (ICurve seg in curve.Segments)
            {
                if (seg is CubicBezierSegment)
                {
                    var bezSeg = seg as CubicBezierSegment;
                    pathFigure.Segments.Add(new BezierSegment
                    {
                        Point1 = WinPoint(bezSeg.B(1)),
                        Point2 = WinPoint(bezSeg.B(2)),
                        Point3 = WinPoint(bezSeg.B(3))
                    });
                }
                else if (seg is Ellipse)
                {
                    var ellipse = seg as Ellipse;
                    pathFigure.Segments.Add(new ArcSegment()
                    {
                        Size = new WinSize(ellipse.AxisA.Length, ellipse.AxisB.Length),
                        SweepDirection = ellipse.OrientedCounterclockwise() ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
                        Point = WinPoint(ellipse.End)
                    });
                }
                else
                    pathFigure.Segments.Add(new WinLineSegment() { Point = WinPoint(seg.End) });
            }
        }

        internal static PathFigure CreateGraphicsPath(ICurve iCurve)
        {
            var pathFigure = new PathFigure { StartPoint = WinPoint(iCurve.Start), IsFilled = false, IsClosed = false };

            if (iCurve is Curve)
            {
                CreateGraphicsPathFromCurve(pathFigure, iCurve as Curve);
            }
            else if (iCurve is Polyline)
            {
                var polyline = iCurve as Polyline;
                pathFigure.IsClosed = polyline.Closed;
                foreach (var p in polyline.PolylinePoints)
                {
                    pathFigure.Segments.Add(new WinLineSegment() { Point = WinPoint(p.Point) });
                }
            }
            else if (iCurve is CubicBezierSegment)
            {
                var bezSeg = iCurve as CubicBezierSegment;
                pathFigure.Segments.Add(new BezierSegment
                {
                    Point1 = WinPoint(bezSeg.B(1)),
                    Point2 = WinPoint(bezSeg.B(2)),
                    Point3 = WinPoint(bezSeg.B(3))
                });
            }
            else if (iCurve is MsaglLineSegment)
            {
                var segment = iCurve as MsaglLineSegment;
                pathFigure.Segments.Add(
                    new WinLineSegment()
                    {
                        Point = WinPoint(segment.End)
                    });
            }
            else if (iCurve is Ellipse)
            {
                var ellipse = iCurve as Ellipse;
                pathFigure.Segments.Add(
                   new ArcSegment()
                   {
                       Size = new WinSize(ellipse.BoundingBox.Width / 2, ellipse.BoundingBox.Height / 2),
                       SweepDirection = SweepDirection.Clockwise,
                       Point = WinPoint(ellipse[Math.PI])
                   });
                pathFigure.Segments.Add(
                   new ArcSegment()
                   {
                       Size = new WinSize(ellipse.BoundingBox.Width / 2, ellipse.BoundingBox.Height / 2),
                       SweepDirection = SweepDirection.Clockwise,
                       Point = WinPoint(ellipse.Start)
                   });
            }
            else if (iCurve is RoundedRect)
            {
                CreateGraphicsPathFromCurve(pathFigure, (iCurve as RoundedRect).Curve);
            }
            return pathFigure;
        }

        const double arrowAngle = 25.0; //degrees

        internal static void DrawArrow(PathGeometry pg, MsaglPoint start, MsaglPoint end, double thickness, ArrowStyle arrowStyle, bool fill)
        {
            switch (arrowStyle)
            {
                case ArrowStyle.NonSpecified:
                case ArrowStyle.Normal:
                    DrawNormalArrow(pg, start, end, thickness, fill);
                    break;
                case ArrowStyle.Tee:
                    DrawTeeArrow(pg, start, end, fill);
                    break;
                case ArrowStyle.Diamond:
                    DrawDiamondArrow(pg, start, end);
                    break;
                case ArrowStyle.ODiamond:
                    throw new NotImplementedException();
                case ArrowStyle.Generalization:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException();
            }
        }

        internal static void DrawNormalArrow(PathGeometry pg, MsaglPoint start, MsaglPoint end, double thickness, bool fill)
        {
            MsaglPoint dir = end - start;
            MsaglPoint h = dir;
            dir /= dir.Length;

            // compensate for line thickness
            end -= dir * thickness / ((double)Math.Tan(arrowAngle * (Math.PI / 180.0)));

            var s = new MsaglPoint(-dir.Y, dir.X);

            s *= h.Length * ((double)Math.Tan(arrowAngle * 0.5 * (Math.PI / 180.0)));

            PathFigure pf = new PathFigure() { IsFilled = fill, IsClosed = true };
            pf.StartPoint = WinPoint(start + s);
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(end) });
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(start - s) });
            pg.Figures.Add(pf);
        }

        // For tee arrows, "fill" indicates whether the line should continue up to the node's boundary.
        static void DrawTeeArrow(PathGeometry pg, MsaglPoint start, MsaglPoint end, bool fill)
        {
            MsaglPoint dir = end - start;
            MsaglPoint h = dir;
            dir /= dir.Length;

            if (fill)
            {
                PathFigure pf = new PathFigure();
                pf.StartPoint = WinPoint(start);
                pf.Segments.Add(new WinLineSegment() { Point = WinPoint(end) });
                pg.Figures.Add(pf);
            }

            var s = new MsaglPoint(-dir.Y, dir.X);

            s *= 2 * h.Length * ((float)Math.Tan(arrowAngle * 0.5f * (Math.PI / 180.0)));
            s += s.Normalize();

            PathFigure pf2 = new PathFigure();
            pf2.StartPoint = WinPoint(start + s);
            pf2.Segments.Add(new WinLineSegment() { Point = WinPoint(start - s) });
            pg.Figures.Add(pf2);
        }

        internal static void DrawDiamondArrow(PathGeometry pg, MsaglPoint start, MsaglPoint end)
        {
            MsaglPoint dir = end - start;
            MsaglPoint h = dir;
            dir /= dir.Length;

            var s = new MsaglPoint(-dir.Y, dir.X);

            PathFigure pf = new PathFigure();
            pf.StartPoint = WinPoint(start - dir);
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(start + (h / 2) + s * (h.Length / 3)) });
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(end) });
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(start + (h / 2) - s * (h.Length / 3)) });
            pf.IsClosed = true;
            pg.Figures.Add(pf);
        }

        internal static void DrawLine(PathGeometry pg, MsaglPoint start, MsaglPoint end)
        {
            PathFigure pf = new PathFigure() { StartPoint = WinPoint(start) };
            pf.Segments.Add(new WinLineSegment() { Point = WinPoint(end) });
            pg.Figures.Add(pf);
        }
    }
}