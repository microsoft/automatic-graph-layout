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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing;

#if PPC
using System.Threading;
// susanlwo
#endif

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// The class responsible for the routing of splines
    /// </summary>
    internal class Routing : AlgorithmBase {
        readonly SugiyamaLayoutSettings settings;
        internal Database Database;
        internal BasicGraph<Node, IntEdge> IntGraph;

        internal LayerArrays LayerArrays;
        internal GeometryGraph OriginalGraph;

        internal ProperLayeredGraph ProperLayeredGraph;

        internal Routing(SugiyamaLayoutSettings settings, GeometryGraph originalGraph, Database dbP,
                         LayerArrays yLayerArrays,
                         ProperLayeredGraph properLayeredGraph,
                         BasicGraph<Node, IntEdge> intGraph
            ) {
            this.settings = settings;
            OriginalGraph = originalGraph;
            Database = dbP;
            ProperLayeredGraph = properLayeredGraph;
            LayerArrays = yLayerArrays;
            IntGraph = intGraph;
        }

        /// <summary>
        /// Executes the actual algorithm.
        /// </summary>
        protected override void RunInternal()
        {
            this.CreateSplines();
        }

        /// <summary>
        /// The method does the main work.
        /// </summary>
        void CreateSplines()
        {
            CreateRegularSplines();
            CreateSelfSplines();
            if (IntGraph != null) RouteFlatEdges();
        }

        void RouteFlatEdges() {
            var flatEdgeRouter = new FlatEdgeRouter(settings, this);
            flatEdgeRouter.Run();
        }

        void CreateRegularSplines() {
#if PPC // Parallel -- susanlwo
            var options = new ParallelOptions();
            if (CancelToken != null)
                options.CancellationToken = CancelToken.CancellationToken;
            Parallel.ForEach<List<IntEdge>>(this.Database.RegularMultiedges, options, (intEdgeList) =>
            {
#else
            foreach (var intEdgeList in Database.RegularMultiedges) {
#endif
                //Here we try to optimize multi-edge routing
                int m = intEdgeList.Count;
                for (int i = m / 2; i < m; i++) CreateSplineForNonSelfEdge(intEdgeList[i]);
#if SHARPKIT // https://github.com/SharpKit/SharpKit/issues/4
                for (int i = (m / 2) - 1; i >= 0; i--) CreateSplineForNonSelfEdge(intEdgeList[i]);
#else
                for (int i = m / 2 - 1; i >= 0; i--) CreateSplineForNonSelfEdge(intEdgeList[i]);
#endif
            }
#if PPC
);
#endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.Msagl.Core.Geometry.Site")]
        void CreateSelfSplines()
        {
            foreach (var kv in Database.Multiedges)
            {
                this.ProgressStep();

                IntPair ip = kv.Key;
                if (ip.x == ip.y) {
                    Anchor anchor = Database.Anchors[ip.x];
                    double offset = anchor.LeftAnchor;
                    foreach (IntEdge intEdge in kv.Value) {
                        ProgressStep();

                        double dx = settings.NodeSeparation + settings.MinNodeWidth + offset;
                        double dy = anchor.BottomAnchor / 2;
                        Point p0 = anchor.Origin;
                        Point p1 = p0 + new Point(0, dy);
                        Point p2 = p0 + new Point(dx, dy);
                        Point p3 = p0 + new Point(dx, -dy);
                        Point p4 = p0 + new Point(0, -dy);

                        var s = new Site(p0);
                        var polyline = new SmoothedPolyline(s);
                        s = new Site(s, p1);
                        s = new Site(s, p2);
                        s = new Site(s, p3);
                        s = new Site(s, p4);
                        new Site(s, p0);

                        Curve c;
                        intEdge.Curve = c = polyline.CreateCurve();
                        intEdge.Edge.UnderlyingPolyline = polyline;
                        offset = dx;
                        if (intEdge.Edge.Label != null) {
                            offset += intEdge.Edge.Label.Width;
                            Point center =
                                intEdge.Edge.Label.Center =
                                new Point(c[(c.ParStart + c.ParEnd) / 2].X + intEdge.LabelWidth / 2, anchor.Y);
                            var del = new Point(intEdge.Edge.Label.Width / 2, intEdge.Edge.Label.Height / 2);

                            var box = new Rectangle(center + del, center - del);
                            intEdge.Edge.Label.BoundingBox = box;
                        }
                        Arrowheads.TrimSplineAndCalculateArrowheads(intEdge.Edge.EdgeGeometry,
                                                               intEdge.Edge.Source.BoundaryCurve,
                                                               intEdge.Edge.Target.BoundaryCurve, c, false, 
                                                               settings.EdgeRoutingSettings.KeepOriginalSpline);
                    }
                }
            }
        }
        void CreateSplineForNonSelfEdge(IntEdge es){
            this.ProgressStep();

            if (es.LayerEdges != null)
            {
                DrawSplineBySmothingThePolyline(es);
                if (!es.IsVirtualEdge)
                {
                    es.UpdateEdgeLabelPosition(Database.Anchors);
                    Arrowheads.TrimSplineAndCalculateArrowheads(es.Edge.EdgeGeometry, es.Edge.Source.BoundaryCurve,
                                                                     es.Edge.Target.BoundaryCurve, es.Curve, true, 
                                                                     settings.EdgeRoutingSettings.KeepOriginalSpline);
                }
            }
        }

        void DrawSplineBySmothingThePolyline(IntEdge edgePath) {
            var smoothedPolyline = new SmoothedPolylineCalculator(edgePath, Database.Anchors, OriginalGraph, settings,
                                                                  LayerArrays,
                                                                  ProperLayeredGraph, Database);
            ICurve spline = smoothedPolyline.GetSpline();
            if (edgePath.Reversed) {
                edgePath.Curve = spline.Reverse();
                edgePath.UnderlyingPolyline = smoothedPolyline.Reverse().GetPolyline;
            }
            else {
                edgePath.Curve = spline;
                edgePath.UnderlyingPolyline = smoothedPolyline.GetPolyline;
            }
        }

        //void UpdateEdgeLabelPosition(LayerEdge[][] list, int i) {
        //    IntEdge e;
        //    int labelNodeIndex;
        //    if (Engine.GetLabelEdgeAndVirtualNode(list, i, out e, out labelNodeIndex)) {
        //        UpdateLabel(e, labelNodeIndex, db.Anchors);
        //    }
        //}

        internal static void UpdateLabel(Edge e, Anchor anchor){
            LineSegment labelSide = null;
            if (anchor.LabelToTheRightOfAnchorCenter){
                e.Label.Center = new Point(anchor.X + anchor.RightAnchor/2, anchor.Y);
                labelSide = new LineSegment(e.LabelBBox.LeftTop, e.LabelBBox.LeftBottom);
            }
            else if (anchor.LabelToTheLeftOfAnchorCenter){
                e.Label.Center = new Point(anchor.X - anchor.LeftAnchor/2, anchor.Y);
                labelSide = new LineSegment(e.LabelBBox.RightTop, e.LabelBBox.RightBottom);
            }
            ICurve segmentInFrontOfLabel = GetSegmentInFrontOfLabel(e.Curve, e.Label.Center.Y);
            if (segmentInFrontOfLabel == null)
                return;
            if (Curve.GetAllIntersections(e.Curve, Curve.PolyFromBox(e.LabelBBox), false).Count == 0){
                Point curveClosestPoint;
                Point labelSideClosest;
                if (FindClosestPoints(out curveClosestPoint, out labelSideClosest, segmentInFrontOfLabel, labelSide)){
                    //shift the label if needed
                    ShiftLabel(e, ref curveClosestPoint, ref labelSideClosest);
                }
                else{
                    //assume that the distance is reached at the ends of labelSideClosest
                    double u = segmentInFrontOfLabel.ClosestParameter(labelSide.Start);
                    double v = segmentInFrontOfLabel.ClosestParameter(labelSide.End);
                    if ((segmentInFrontOfLabel[u] - labelSide.Start).Length <
                        (segmentInFrontOfLabel[v] - labelSide.End).Length){
                        curveClosestPoint = segmentInFrontOfLabel[u];
                        labelSideClosest = labelSide.Start;
                    }
                    else{
                        curveClosestPoint = segmentInFrontOfLabel[v];
                        labelSideClosest = labelSide.End;
                    }
                    ShiftLabel(e, ref curveClosestPoint, ref labelSideClosest);
                }
            }
        }

        static void ShiftLabel(Edge e, ref Point curveClosestPoint, ref Point labelSideClosest) {
            double w = e.LineWidth/2;
            Point shift = curveClosestPoint - labelSideClosest;
            double shiftLength = shift.Length;
            //   SugiyamaLayoutSettings.Show(e.Curve, shiftLength > 0 ? new LineSegment(curveClosestPoint, labelSideClosest) : null, PolyFromBox(e.LabelBBox));
            if (shiftLength > w)
                e.Label.Center += shift/shiftLength*(shiftLength - w);
        }

        static bool FindClosestPoints(out Point curveClosestPoint, out Point labelSideClosest,
                                      ICurve segmentInFrontOfLabel, LineSegment labelSide) {
            double u, v;
            return Curve.MinDistWithinIntervals(segmentInFrontOfLabel, labelSide, segmentInFrontOfLabel.ParStart,
                                                segmentInFrontOfLabel.ParEnd, labelSide.ParStart, labelSide.ParEnd,
                                                (segmentInFrontOfLabel.ParStart + segmentInFrontOfLabel.ParEnd)/2,
                                                (labelSide.ParStart + labelSide.ParEnd)/2,
                                                out u, out v, out curveClosestPoint, out labelSideClosest);
        }

        static ICurve GetSegmentInFrontOfLabel(ICurve edgeCurve, double labelY) {
            var curve = edgeCurve as Curve;
            if (curve != null) {
                foreach (ICurve seg in curve.Segments)
                    if ((seg.Start.Y - labelY)*(seg.End.Y - labelY) <= 0)
                        return seg;
            }
            else Debug.Assert(false); //not implemented
            return null;
        }


        internal static NodeKind GetNodeKind(int vertexOffset, IntEdge edgePath) {
            return vertexOffset == 0
                       ? NodeKind.Top
                       : (vertexOffset < edgePath.Count ? NodeKind.Internal : NodeKind.Bottom);
        }
    }
}