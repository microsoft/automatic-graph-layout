using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Routing
{
    /// <summary>
    /// Basic edge router for producing straight edges.
    /// </summary>
    public class StraightLineEdges : AlgorithmBase
    {
        private readonly IEnumerable<Edge> edges;

        private readonly double padding;

        /// <summary>
        /// Constructs a basic straight edge router.
        /// </summary>
        public StraightLineEdges(IEnumerable<Edge> edges, double padding)
        {
            this.edges = edges;
            this.padding = padding;
        }

        /// <summary>
        /// Executes the algorithm.
        /// </summary>
        protected override void RunInternal()
        {
            this.StartListenToLocalProgress(edges.Count());
            SplineRouter.CreatePortsIfNeeded(edges);
            foreach (Edge edge in edges)
            {
                RouteEdge(edge, this.padding);
                this.ProgressStep();
            }
        }

        /// <summary>
        /// populate the geometry including curve and arrowhead positioning for the given edge using simple
        /// straight line routing style.  Self edges will be drawn as a loop, padding is used to control the
        /// size of the loop.
        /// </summary>
        /// <param name="edge">edge to route</param>
        /// <param name="padding">controls size of loop</param>
        public static void RouteEdge(Edge edge, double padding)
        {
            ValidateArg.IsNotNull(edge, "edge");

            var eg = edge.EdgeGeometry;

            if (eg.SourcePort == null)
            {
#if SHARPKIT // Lambdas bind differently in JS
                eg.SourcePort = ((Func<Edge,RelativeFloatingPort>)(ed => new RelativeFloatingPort(() => ed.Source.BoundaryCurve,
                    () => ed.Source.Center)))(edge);
#else
                eg.SourcePort = new RelativeFloatingPort(() => edge.Source.BoundaryCurve, () => edge.Source.Center);
#endif
            }

            if (eg.TargetPort == null)
            {
#if SHARPKIT // Lambdas bind differently in JS
                eg.TargetPort = ((Func<Edge, RelativeFloatingPort>)(ed => new RelativeFloatingPort(() => ed.Target.BoundaryCurve,
                    () => ed.Target.Center)))(edge);
#else
                eg.TargetPort = new RelativeFloatingPort(() => edge.Target.BoundaryCurve, () => edge.Target.Center);
#endif
            }

            if (!ContainmentLoop(eg, padding))
            {
                eg.Curve = GetEdgeLine(edge);
            }

            Arrowheads.TrimSplineAndCalculateArrowheads(eg, eg.SourcePort.Curve,
                                                         eg.TargetPort.Curve, edge.Curve, false);
                      
        }

        internal static bool ContainmentLoop(EdgeGeometry eg, double padding) {
            var sourceCurve = eg.SourcePort.Curve;
            var targetCurve = eg.TargetPort.Curve;
            if (sourceCurve == null || targetCurve == null)
                return false;
            Rectangle targetBox = sourceCurve.BoundingBox;
            Rectangle sourceBox = targetCurve.BoundingBox;
            bool targetInSource = targetBox.Contains(sourceBox);
            bool sourceInTarget = (!targetInSource) && sourceBox.Contains(targetBox);
            if (targetInSource || sourceInTarget) {
                eg.Curve = CreateLoop(targetBox, sourceBox, sourceInTarget, padding);
                return true;
            }
            return false;
        }

        private static Curve CreateLoop(Rectangle targetBox, Rectangle sourceBox, bool sourceContainsTarget, double padding)
        {
            return sourceContainsTarget ? CreateLoop(targetBox, sourceBox, padding, false) : CreateLoop(sourceBox, targetBox, padding, true);
        }

        /// <summary>
        /// creates a loop from sourceBox center to the closest point on the targetBox boundary
        /// </summary>
        /// <param name="sourceBox"></param>
        /// <param name="targetBox">contains sourceBox</param>
        /// <param name="howMuchToStickOut"></param>
        /// <param name="reverse">reverse the loop if true</param>
        /// <returns></returns>
        internal static Curve CreateLoop(Rectangle sourceBox, Rectangle targetBox, double howMuchToStickOut, bool reverse)
        {
            var center=sourceBox.Center;
            var closestPoint = FindClosestPointOnBoxBoundary(sourceBox.Center, targetBox);
            var dir = closestPoint - center;
            var vert=Math.Abs(dir.X)<ApproximateComparer.DistanceEpsilon;
            var maxWidth=(vert? Math.Min(center.Y-targetBox.Bottom, targetBox.Top-center.Y): Math.Min(center.X-targetBox.Left, targetBox.Right-center.X))/2; //divide over 2 to not miss the rect
            var width = Math.Min(howMuchToStickOut, maxWidth);
            if (dir.Length <= ApproximateComparer.DistanceEpsilon)
                dir = new Point(1, 0);
            var hookDir=dir.Normalize();
            var hookPerp=hookDir.Rotate(Math.PI/2);
            var p1 = closestPoint + hookDir * howMuchToStickOut;
            var p2 = p1 + hookPerp * width;
            var p3 = closestPoint + hookPerp * width;
            var end = center + hookPerp * width;
            
            var smoothedPoly=reverse?SmoothedPolyline.FromPoints( new []{end, p3, p2, p1, closestPoint, center}): SmoothedPolyline.FromPoints( new []{center, closestPoint, p1, p2, p3, end});
            return smoothedPoly.CreateCurve();
        }

        static Point FindClosestPointOnBoxBoundary(Point c, Rectangle targetBox){
            var x = c.X - targetBox.Left < targetBox.Right - c.X ? targetBox.Left : targetBox.Right;
            var y = c.Y - targetBox.Bottom < targetBox.Top - c.Y ? targetBox.Bottom : targetBox.Top;
            return Math.Abs(x - c.X) < Math.Abs(y - c.Y) ? new Point(x, c.Y) : new Point(c.X, y);
        }

        /// <summary>
        /// Returns a line segment for the given edge.
        /// </summary>
        /// <returns>The line segment representing the given edge.</returns>
        public static LineSegment GetEdgeLine(Edge edge)
        {
            ValidateArg.IsNotNull(edge, "edge");
            Point sourcePoint;
            ICurve sourceBox;
            if (edge.SourcePort == null)
            {
                sourcePoint = edge.Source.Center;
                sourceBox = edge.Source.BoundaryCurve;
            }
            else
            {
                sourcePoint = edge.SourcePort.Location;
                sourceBox = edge.SourcePort.Curve;
            }
            Point targetPoint;
            ICurve targetBox;
            if (edge.TargetPort == null)
            {
                targetPoint = edge.Target.Center;
                targetBox = edge.Target.BoundaryCurve;
            }
            else
            {
                targetPoint = edge.TargetPort.Location;
                targetBox = edge.TargetPort.Curve;
            }
            LineSegment line = new LineSegment(sourcePoint, targetPoint);
            IList<IntersectionInfo> intersects = Curve.GetAllIntersections(sourceBox, line, false);

            if (intersects.Count > 0)
            {
                var trimmedLine = (LineSegment)line.Trim(intersects[0].Par1, 1.0);
                if(trimmedLine != null)
                {
                    line = trimmedLine;
                    intersects = Curve.GetAllIntersections(targetBox, line, false);
                    if (intersects.Count > 0)
                    {
                        trimmedLine = (LineSegment)line.Trim(0.0, intersects[0].Par1);
                        if(trimmedLine != null)
                        {
                            line = trimmedLine;
                        }
                    }
                }
            }

            return line;
        }

        /// <summary>
        /// creates an edge curve based only on the source and target geometry
        /// </summary>
        /// <param name="edge"></param>
        public static void CreateSimpleEdgeCurveWithUnderlyingPolyline(Edge edge)
        {
            ValidateArg.IsNotNull(edge, "edge");
            var a = edge.Source.Center;
            var b = edge.Target.Center;
            if (edge.Source == edge.Target)
            {
                var dx = 2.0 / 3 * edge.Source.BoundaryCurve.BoundingBox.Width;
                var dy = edge.Source.BoundingBox.Height / 4;
                edge.UnderlyingPolyline = CreateUnderlyingPolylineForSelfEdge(a, dx, dy);
                edge.Curve = edge.UnderlyingPolyline.CreateCurve();
            }
            else
            {
                edge.UnderlyingPolyline = SmoothedPolyline.FromPoints(new[] { a, b });
                edge.Curve = edge.UnderlyingPolyline.CreateCurve();
            }
            Arrowheads.TrimSplineAndCalculateArrowheads(edge.EdgeGeometry,
                                                                 edge.Source.BoundaryCurve,
                                                                 edge.Target.BoundaryCurve,
                                                                 edge.Curve, false);
            
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.Msagl.Core.Geometry.Site")]
        internal static SmoothedPolyline CreateUnderlyingPolylineForSelfEdge(Point p0, double dx, double dy)
        {
            var p1 = p0 + new Point(0, dy);
            var p2 = p0 + new Point(dx, dy);
            var p3 = p0 + new Point(dx, -dy);
            var p4 = p0 + new Point(0, -dy);

            var site = new Site(p0);
            var polyline = new SmoothedPolyline(site);
            site = new Site(site, p1);
            site = new Site(site, p2);
            site = new Site(site, p3);
            site = new Site(site, p4);
            new Site(site, p0);
            return polyline;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.Msagl.Site")]
        static internal void SetStraightLineEdgesWithUnderlyingPolylines(GeometryGraph graph) {
            SplineRouter.CreatePortsIfNeeded(graph.Edges);
            foreach (Edge edge in graph.Edges) 
                CreateSimpleEdgeCurveWithUnderlyingPolyline(edge);
        }
    }
}
