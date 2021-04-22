using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
#if TEST_MSAGL
using Microsoft.Msagl.DebugHelpers;
#endif
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    internal class FlipCollapser {
        readonly MetroGraphData metroGraphData;
        readonly BundlingSettings bundlingSettings;
        readonly Cdt cdt;

        internal FlipCollapser(MetroGraphData metroGraphData, BundlingSettings bundlingSettings, Cdt cdt) {
            this.metroGraphData = metroGraphData;
            this.bundlingSettings = bundlingSettings;
            this.cdt = cdt;
        }

        //        void ShowFlip(Polyline a, Polyline b) {
        //            var l = new List<DebugCurve>();
        //
        //            l.AddRange(
        //                Cdt.GetTriangles().Select(
        //                    t => new DebugCurve(100, 1, "green", new Polyline(t.Sites.Select(v => v.Point)) {Closed = true})));
        //            l.AddRange(new[] {
        //                                 new DebugCurve(120, 1, "red", a),
        //                                 new DebugCurve(120, 1, "blue", b)
        //                             });
        //            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        //        }

        /*
                IEnumerable<CdtEdge> GetCdtEdgesCrossedBySegmentStartingFromSiteAtStart(CdtSiteFeature f, PolylinePoint pp, out CdtFeature nextFeature) {
                    Debug.Assert(f.Prev == null);
                    var ret = new List<CdtEdge>();
                    var site = f.Site;
                    foreach (var t in site.Triangles) {
                        var si = t.Sites.Index(site);
                        if (Point.PointIsInsideCone(pp.Point, site.Point, t.Sites[si + 1].Point, t.Sites[si + 2].Point)) {
                            if (Point.GetTriangleOrientation(pp.Point, t.Sites[si + 1].Point, t.Sites[si + 2].Point) ==
                                TriangleOrientation.Collinear) {//pp belongs to the edge [si+1]
                                ret.Add(t.Edges[si + 1]);
                                if (Point.GetTriangleOrientation(site.Point, t.Sites[si + 1].Point, pp.Point) ==
                                    TriangleOrientation.Collinear)
                                    nextFeature = new CdtSiteFeature(t.Sites[si + 1], f) {Prev = f};
                                else if (Point.GetTriangleOrientation(site.Point, t.Sites[si + 2].Point, pp.Point) ==
                                         TriangleOrientation.Collinear)
                                    nextFeature = new CdtSiteFeature(t.Sites[si + 2], f) {Prev = f};
                                else
                                    nextFeature = new CdtEdgeFeature(t.Edges[si + 1], pp.Point, f) {Prev = f};
                            }
                        }
                    }
                }
        */


        /*
                Set<CdtEdge> GetCdtEdgesCrossedByPath0(List<PolylinePoint> polyPoints) {
                    PolylinePoint prevPolyPoint = null, prevPrevPolyPoint=null;
                    var ret = new Set<CdtEdge>();
                    foreach (var polylinePoint in polyPoints) {
                        if (prevPolyPoint!=null)
                            ret.InsertRange(GetCdtEdgesCrossedBySegment(prevPrevPolyPoint, prevPolyPoint, polylinePoint));

                        prevPrevPolyPoint = prevPolyPoint;
                        prevPolyPoint = polylinePoint;
                    }
        //            var l = new List<DebugCurve>();
        //            
        //            l.AddRange(
        //                Cdt.GetTriangles().Select(
        //                    t => new DebugCurve(100, 1, "green", new Polyline(t.Sites.Select(v => v.Point)) {Closed = true})));
        //            l.Add(new DebugCurve(150,2,"blue",new Polyline(polyPoints)));
        //          l.AddRange(ret.Select(e=>new DebugCurve(200,2,"brown", new LineSegment(e.upperSite.Point,e.lowerSite.Point))));
        //            LayoutAlgorithmSettings.ShowDebugCurves(l.ToArray());
        //           
                    return ret;
                }
        */

        /*
                IEnumerable<CdtEdge> GetCdtEdgesCrossedBySegment(PolylinePoint prevA, PolylinePoint a, PolylinePoint b) {
                    var pp = new PointPair(a.Point, b.Point);
                    IEnumerable<CdtEdge> intersections;
                    if (segsToCdtEdges.TryGetValue(pp, out intersections))
                        return intersections;
                    return segsToCdtEdges[pp] = Cdt.GetCdtEdgesCrossedBySegment(prevA, a, b);
                }
        */

        internal static PointPair OrderedPair(PolylinePoint pp) {
            return OrderedPair(pp, pp.Next);
        }

        static PointPair OrderedPair(PolylinePoint p0, PolylinePoint p1) {
            return new PointPair(p0.Point, p1.Point);
        }
    }
}