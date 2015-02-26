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
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using BBox = Microsoft.Msagl.Core.Geometry.Rectangle;
using P2 = Microsoft.Msagl.Core.Geometry.Point;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;

namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// Class building the node hierarchy 
    /// </summary>
    internal class Tessellator {
        static double epsilon = 1.0/10;

        /// <summary>
        /// a private constructor to avoid the instantiation
        /// </summary>
        Tessellator() {
        }

        internal static double DistToSegm(P2 p, P2 s, P2 e) {
            P2 l = e - s;
            double len = l.Length;
            if (len < epsilon)
                return (p - (0.5f*(s + e))).Length;
            var perp = new P2(-l.Y, l.X);
            perp /= len;
            return Math.Abs((p - s)*perp);
        }

        static bool WithinEpsilon(ICurve bc, double start, double end) {
            int n = 3; //hack !!!!
            double d = (end - start)/n;
            P2 s = bc[start];
            P2 e = bc[end];

            return DistToSegm(bc[start + d], s, e) < epsilon && DistToSegm(bc[start + d*(n - 1)], s, e) < epsilon;
        }

        internal static List<ObjectWithBox> TessellateCurve(DEdge dedge, double radiusForUnderlyingPolylineCorners) {
            DrawingEdge edge = dedge.DrawingEdge;
            ICurve bc = edge.EdgeCurve;
            double lineWidth = edge.Attr.LineWidth;
            var ret = new List<ObjectWithBox>();
            int n = 1;
            bool done;
            do {
                double d = (bc.ParEnd - bc.ParStart)/n;
                done = true;
                if (n <= 64) //don't break a segment into more than 64 parts
                    for (int i = 0; i < n; i++) {
                        if (!WithinEpsilon(bc, d*i, d*(i + 1))) {
                            n *= 2;
                            done = false;
                            break;
                        }
                    }
            } while (!done);

            double del = (bc.ParEnd - bc.ParStart)/n;

            for (int j = 0; j < n; j++) {
                var line = new Line(dedge, bc[del*j], bc[del*(j + 1)], lineWidth);
                ret.Add(line);
            }

            //if (dedge.Label != null)
            //    ret.Add(new LabelGeometry(dedge.Label, edge.Label.Left,
            //                              edge.Label.Bottom, new P2(edge.Label.Size.Width, edge.Label.Size.Height)));


            if (edge.Attr.ArrowAtTarget)
                ret.Add(new Line(dedge, edge.EdgeCurve.End, edge.ArrowAtTargetPosition, edge.Attr.LineWidth));


            if (edge.Attr.ArrowAtSource)
                ret.Add(new Line(dedge, edge.EdgeCurve.Start, edge.ArrowAtSourcePosition, edge.Attr.LineWidth));

            if (radiusForUnderlyingPolylineCorners > 0)
                AddUnderlyingPolylineTessellation(ret, dedge, radiusForUnderlyingPolylineCorners);

            return ret;
        }


        static void AddUnderlyingPolylineTessellation(List<ObjectWithBox> list, DEdge edge,
                                                      double radiusForUnderlyingPolylineCorners) {
            var rad = new P2(radiusForUnderlyingPolylineCorners, radiusForUnderlyingPolylineCorners);
            IEnumerator<P2> en = edge.DrawingEdge.GeometryEdge.UnderlyingPolyline.GetEnumerator();
            en.MoveNext();
            P2 p = en.Current;
            list.Add(new Geometry(edge, new BBox(p + rad, p - rad)));
            while (en.MoveNext()) {
                list.Add(new Line(edge, p, p = en.Current, edge.DrawingEdge.Attr.LineWidth));
                list.Add(new Geometry(edge, new BBox(p + rad, p - rad)));
            }
        }
    }
}