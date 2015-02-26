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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Core.Layout
{
    /// <summary>
    /// Arrowhead calculations
    /// </summary>
    public static class Arrowheads {
        /// <summary>
        /// calculates new curve ends that are the arrowhead starts
        /// </summary>
        /// <param name="edgeGeometry">The edgeGeometry.Curve is trimmed already by the node boundaries</param>
        /// <returns></returns>
        internal static bool CalculateArrowheads(EdgeGeometry edgeGeometry) {
            ValidateArg.IsNotNull(edgeGeometry, "edgeGeometry");
            if (edgeGeometry.SourceArrowhead == null && edgeGeometry.TargetArrowhead == null)
                return true;
            double parStart, parEnd;
            if (!FindTrimStartForArrowheadAtSource(edgeGeometry, out parStart))
                return false;
            if (!FindTrimEndForArrowheadAtTarget(edgeGeometry, out parEnd))
                return false;
            if (parStart > parEnd - ApproximateComparer.IntersectionEpsilon || ApproximateComparer.CloseIntersections(edgeGeometry.Curve[parStart], edgeGeometry.Curve[parEnd]))
                return false; //after the trim nothing would be left of the curve
            var c = edgeGeometry.Curve.Trim(parStart, parEnd);
            if (c == null)
                return false;
            if (edgeGeometry.SourceArrowhead != null)
                edgeGeometry.SourceArrowhead.TipPosition = PlaceTip(c.Start, edgeGeometry.Curve.Start, edgeGeometry.SourceArrowhead.Offset);
            if (edgeGeometry.TargetArrowhead != null)
                edgeGeometry.TargetArrowhead.TipPosition = PlaceTip(c.End, edgeGeometry.Curve.End, edgeGeometry.TargetArrowhead.Offset);
            edgeGeometry.Curve = c;
            return true;
        }

        
        static IList<IntersectionInfo> GetIntersectionsWithArrowheadCircle(ICurve curve, double arrowheadLength, Point circleCenter) {
            Debug.Assert(arrowheadLength > 0);
            var e = new Ellipse(arrowheadLength, arrowheadLength, circleCenter);
            return Curve.GetAllIntersections(e, curve, true);
        }
        /// <summary>
        /// we need to pass arrowhead length here since the original length mibh
        /// </summary>
        /// <param name="edgeGeometry"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        static bool FindTrimEndForArrowheadAtTarget(EdgeGeometry edgeGeometry, out double p) {
            var eps = ApproximateComparer.DistanceEpsilon*ApproximateComparer.DistanceEpsilon;
            //Debug.Assert((edgeGeometry.Curve.End - edgeGeometry.Curve.Start).LengthSquared > eps);
            p = edgeGeometry.Curve.ParEnd;
            if (edgeGeometry.TargetArrowhead == null ||
                edgeGeometry.TargetArrowhead.Length <= ApproximateComparer.DistanceEpsilon)
                return true;
            var curve = edgeGeometry.Curve;
            var arrowheadLength = edgeGeometry.TargetArrowhead.Length;
            Point newCurveEnd;
            IList<IntersectionInfo> intersections;
            int reps = 10;
            do {
                reps--;
                if (reps == 0)
                    return false;
                intersections = GetIntersectionsWithArrowheadCircle(curve, arrowheadLength, curve.End);
                p = intersections.Count != 0 ? intersections.Max(x => x.Par1) : curve.ParEnd;
                newCurveEnd = edgeGeometry.Curve[p];
                arrowheadLength /= 2;
            } while (((newCurveEnd - curve.Start).LengthSquared < eps || intersections.Count == 0));
            //we would like to have at least something left from the curve
            return true;
        }

        static bool FindTrimStartForArrowheadAtSource(EdgeGeometry edgeGeometry, out double p) {
            p = 0; //does not matter
            if (edgeGeometry.SourceArrowhead == null || edgeGeometry.SourceArrowhead.Length <= ApproximateComparer.DistanceEpsilon)
                return true;
            var eps = ApproximateComparer.DistanceEpsilon * ApproximateComparer.DistanceEpsilon;
            Debug.Assert((edgeGeometry.Curve.End - edgeGeometry.Curve.Start).LengthSquared > eps);
            var arrowheadLength = edgeGeometry.SourceArrowhead.Length;
            Point newStart;
            var curve = edgeGeometry.Curve;
            IList<IntersectionInfo> intersections;
            int reps = 10;
            do
            {
                reps--;
                if (reps == 0)
                    return false; 
                intersections = GetIntersectionsWithArrowheadCircle(curve, arrowheadLength, curve.Start);
                p = intersections.Count != 0 ? intersections.Min(x => x.Par1) : curve.ParStart;
                newStart = curve[p];
                arrowheadLength /= 2;
            } while ((newStart - curve.End).LengthSquared < eps || intersections.Count==0);
            //we are checkng that something will be left from the curve
            return true;
        }


        internal static Point PlaceTip(Point arrowBase, Point arrowTip, double offset)
        {
            if (Math.Abs(offset) < ApproximateComparer.Tolerance)
                return arrowTip;

            var d=arrowBase - arrowTip;            
            var dLen=d.Length;
            if(dLen<ApproximateComparer.Tolerance)
                return arrowTip;
            return arrowTip + offset * (d / dLen);
        }

        /// <summary>
        /// trim the edge curve with the node boundaries
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="spline"></param>
        /// <param name="narrowestInterval"></param>
        /// <param name="keepOriginalSpline">to keep the original spline</param>
        /// <returns></returns>
        public static bool TrimSplineAndCalculateArrowheads(Edge edge, ICurve spline, bool narrowestInterval, bool keepOriginalSpline)
        {
            ValidateArg.IsNotNull(edge, "edge");
            return TrimSplineAndCalculateArrowheads(edge.EdgeGeometry,
                                                    edge.Source.BoundaryCurve,
                                                    edge.Target.BoundaryCurve,
                                                    spline,
                                                    narrowestInterval, keepOriginalSpline);
        }

        /// <summary>
        /// trim the edge curve with the node boundaries
        /// </summary>
        /// <param name="edgeGeometry"></param>
        /// <param name="targetBoundary"></param>
        /// <param name="spline"></param>
        /// <param name="narrowestInterval"></param>
        /// <param name="sourceBoundary"></param>
        /// <param name="keepOriginalSpline"></param>
        /// <returns></returns>
        public static bool TrimSplineAndCalculateArrowheads(EdgeGeometry edgeGeometry,
                                                            ICurve sourceBoundary,
                                                            ICurve targetBoundary,
                                                            ICurve spline,
                                                            bool narrowestInterval,
                                                            bool keepOriginalSpline) {
            ValidateArg.IsNotNull(spline, "spline");
            ValidateArg.IsNotNull(edgeGeometry, "edgeGeometry");
            
            
            edgeGeometry.Curve = Curve.TrimEdgeSplineWithNodeBoundaries(sourceBoundary, targetBoundary, spline,
                                                                        narrowestInterval);
            if (edgeGeometry.Curve == null)
                return false;
            
            if ((edgeGeometry.SourceArrowhead == null ||
                 edgeGeometry.SourceArrowhead.Length < ApproximateComparer.DistanceEpsilon) &&
                (edgeGeometry.TargetArrowhead == null ||
                 edgeGeometry.TargetArrowhead.Length < ApproximateComparer.DistanceEpsilon))
                return true; //there are no arrowheads
            bool success = false;
            double sourceArrowheadSavedLength = edgeGeometry.SourceArrowhead != null
                                                    ? edgeGeometry.SourceArrowhead.Length
                                                    : 0;
            double targetArrowheadSavedLength = edgeGeometry.TargetArrowhead != null
                                                    ? edgeGeometry.TargetArrowhead.Length
                                                    : 0;
            var len = (edgeGeometry.Curve.End - edgeGeometry.Curve.Start).Length;
            if (edgeGeometry.SourceArrowhead!=null)
            edgeGeometry.SourceArrowhead.Length = Math.Min(len, sourceArrowheadSavedLength);
            if (edgeGeometry.TargetArrowhead != null)
                edgeGeometry.TargetArrowhead.Length = Math.Min(len, targetArrowheadSavedLength);
            int count = 10;
            while (
                (
                    edgeGeometry.SourceArrowhead != null &&
                    edgeGeometry.SourceArrowhead.Length > ApproximateComparer.IntersectionEpsilon
                    ||
                    edgeGeometry.TargetArrowhead != null &&
                    edgeGeometry.TargetArrowhead.Length > ApproximateComparer.IntersectionEpsilon) && !success) {
                success = Arrowheads.CalculateArrowheads(edgeGeometry);
                if (!success) {
                    if (edgeGeometry.SourceArrowhead != null) edgeGeometry.SourceArrowhead.Length *= 0.5;
                    if (edgeGeometry.TargetArrowhead != null) edgeGeometry.TargetArrowhead.Length *= 0.5;
                }
                count--;
                if (count == 0)
                    break;
            }

            if (!success) {
                //to avoid drawing the arrowhead to (0,0)
                if (edgeGeometry.SourceArrowhead != null) edgeGeometry.SourceArrowhead.TipPosition = spline.Start;
                if (edgeGeometry.TargetArrowhead != null) edgeGeometry.TargetArrowhead.TipPosition = spline.End;
            }

            if (edgeGeometry.SourceArrowhead != null) edgeGeometry.SourceArrowhead.Length = sourceArrowheadSavedLength;
            if (edgeGeometry.TargetArrowhead != null) edgeGeometry.TargetArrowhead.Length = targetArrowheadSavedLength;

            return success;
        }

        /// <summary>
        /// Creates a spline between two nodes big enough to draw arrowheads
        /// </summary>
        /// <param name="edge"></param>
        public static void CreateBigEnoughSpline(Edge edge)
        {
            ValidateArg.IsNotNull(edge, "edge");
            Point a = edge.Source.Center;
            Point b = edge.Target.Center;
            Point bMinA = b - a;

            double l = bMinA.Length;
            Point perp;
            if (l < 0.001)
            {
                perp = new Point(1, 0);
                b = a + perp.Rotate(Math.PI / 2);
            }
            else
            {
                perp = bMinA.Rotate(Math.PI / 2);
            }

            double maxArrowLength = 1;
            if (edge.EdgeGeometry.SourceArrowhead != null)
            {
                maxArrowLength += edge.EdgeGeometry.SourceArrowhead.Length;
            }
            if (edge.EdgeGeometry.TargetArrowhead != null)
            {
                maxArrowLength += edge.EdgeGeometry.TargetArrowhead.Length;
            }
            perp = perp.Normalize() * 1.5 * maxArrowLength;

            int i = 1;
            do
            {
                CubicBezierSegment seg = Curve.CreateBezierSeg(a, b, perp, i);
                if (TrimSplineAndCalculateArrowheads(edge.EdgeGeometry, edge.Source.BoundaryCurve,
                                                     edge.Target.BoundaryCurve,
                                                     seg, false, false))
                {
                    break;
                }

                i *= 2;
                const int stop = 10000;
                if (i >= stop)
                {
                    CreateEdgeCurveWithNoTrimming(edge, a, b);
                    return;
                }
            } while (true);
        }

        /// <summary>
        /// this code should never work!
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        static void CreateEdgeCurveWithNoTrimming(Edge edge, Point a, Point b)
        {
            Point ab = b - a;

            ab = ab.Normalize();

            Point lineStart = a;
            Point lineEnd = b;

            Arrowhead targetArrow = edge.EdgeGeometry.TargetArrowhead;
            if (targetArrow != null)
            {
                targetArrow.TipPosition = b;
                lineEnd = b - ab * targetArrow.Length;
            }
            Arrowhead sourceArrow = edge.EdgeGeometry.SourceArrowhead;
            if (sourceArrow != null)
            {
                sourceArrow.TipPosition = a;
                lineStart = a + ab * sourceArrow.Length;
            }
            edge.Curve = new LineSegment(lineStart, lineEnd);
        }
    }
}