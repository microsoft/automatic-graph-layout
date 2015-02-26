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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using MsaglRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using MsaglPoint = Microsoft.Msagl.Core.Geometry.Point;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    /// <summary>
    /// Class building the node hierarchy 
    /// </summary>

    internal class Tessellator
    {

        /// <summary>
        /// a private constructor to avoid the instantiation
        /// </summary>
        Tessellator() { }

        internal static double DistToSegm(MsaglPoint p, MsaglPoint s, MsaglPoint e)
        {

            MsaglPoint l = e - s;
            double len = l.Length;
            if (len < Tessellator.epsilon)
                return (p - (0.5f * (s + e))).Length;
            MsaglPoint perp = new MsaglPoint(-l.Y, l.X);
            perp /= len;
            return Math.Abs((p - s) * perp);

        }

        static bool WithinEpsilon(ICurve bc, double start, double end)
        {
            int n = 3; //hack !!!!
            double d = (end - start) / n;
            MsaglPoint s = bc[start];
            MsaglPoint e = bc[end];

            return DistToSegm(bc[start + d], s, e) < epsilon
              &&
              DistToSegm(bc[start + d * (n - 1)], s, e) < epsilon;

        }

        internal static List<ObjectWithBox> TessellateCurve(DEdge dedge, double radiusForUnderlyingPolylineCorners)
        {
            DrawingEdge edge = dedge.DrawingEdge;
            ICurve bc = edge.EdgeCurve;
            double lineWidth = edge.Attr.LineWidth;
            List<ObjectWithBox> ret = new List<ObjectWithBox>();
            int n = 1;
            bool done;
            do
            {
                double d = (bc.ParEnd - bc.ParStart) / (double)n;
                done = true;
                if (n <= 64)//don't break a segment into more than 64 parts
                    for (int i = 0; i < n; i++)
                    {
                        if (!WithinEpsilon(bc, d * i, d * (i + 1)))
                        {
                            n *= 2;
                            done = false;
                            break;
                        }
                    }
            }
            while (!done);

            double del = (bc.ParEnd - bc.ParStart) / n;

            for (int j = 0; j < n; j++)
            {
                Line line = new Line(dedge, bc[del * (double)j], bc[del * (double)(j + 1)], lineWidth);
                ret.Add(line);
            }

            //if (dedge.Label != null)
            //    pf.Add(new LabelGeometry(dedge.Label, edge.Label.Left,
            //                              edge.Label.Bottom, new MsaglPoint(edge.Label.Size.Width, edge.Label.Size.Height)));


            if (edge.Attr.ArrowAtTarget)
                ret.Add(new Line(dedge, (MsaglPoint)edge.EdgeCurve.End, edge.ArrowAtTargetPosition, edge.Attr.LineWidth));



            if (edge.Attr.ArrowAtSource)
                ret.Add(
                        new Line(dedge, edge.EdgeCurve.Start, edge.ArrowAtSourcePosition, edge.Attr.LineWidth));

            if (radiusForUnderlyingPolylineCorners > 0)
                AddUnderlyingPolylineTessellation(ret, dedge, radiusForUnderlyingPolylineCorners);

            return ret;

        }


        private static void AddUnderlyingPolylineTessellation(List<ObjectWithBox> list, DEdge edge, double radiusForUnderlyingPolylineCorners)
        {

            MsaglPoint rad = new MsaglPoint(radiusForUnderlyingPolylineCorners, radiusForUnderlyingPolylineCorners);
            IEnumerator<MsaglPoint> en = edge.DrawingEdge.GeometryEdge.UnderlyingPolyline.GetEnumerator();
            en.MoveNext();
            MsaglPoint p = en.Current;
            list.Add(new Geometry(edge, new MsaglRectangle(p + rad, p - rad)));
            while (en.MoveNext())
            {
                list.Add(new Line(edge, p, p = en.Current, edge.DrawingEdge.Attr.LineWidth));
                list.Add(new Geometry(edge, new MsaglRectangle(p + rad, p - rad)));
            }

        }

        static double epsilon = 0.1;

    }
}