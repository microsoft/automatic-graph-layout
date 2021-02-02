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
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using BBox = Microsoft.Msagl.Core.Geometry.Rectangle;
using Color = System.Drawing.Color;
using DrawingGraph = Microsoft.Msagl.Drawing.Graph;
using GeometryEdge = Microsoft.Msagl.Core.Layout.Edge;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using P2 = Microsoft.Msagl.Core.Geometry.Point;


namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// exposes some drawing functionality
    /// </summary>
    public sealed class Draw {
        /// <summary>
        /// private constructor
        /// </summary>
        Draw() {
        }

        static double doubleCircleOffsetRatio = 0.9;

        internal static double DoubleCircleOffsetRatio {
            get { return doubleCircleOffsetRatio; }
        }


        internal static float dashSize = 0.05f; //inches

        /// <summary>
        /// A color converter
        /// </summary>
        /// <param name="gleeColor"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msagl")]
        public static Color MsaglColorToDrawingColor(Drawing.Color gleeColor) {
            return Color.FromArgb(gleeColor.A, gleeColor.R, gleeColor.G, gleeColor.B);
        }


        /// <summary>
        /// Drawing that can be performed on any Graphics object
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="precalculatedObject"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "precalculated"),
         SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "object"),
         SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Precalculated")]
        public static void DrawPrecalculatedLayoutObject(Graphics graphics, object precalculatedObject) {
            var dg = precalculatedObject as DGraph;
            if (dg != null)
                dg.DrawGraph(graphics);
        }

#if TEST_MSAGL
        internal static void DrawDebugStuff(Graphics g, DGraph graphToDraw, Pen myPen) {
            if (graphToDraw.DrawingGraph.DebugICurves != null) {
                foreach (ICurve c in graphToDraw.DrawingGraph.DebugICurves) {
                    DrawDebugICurve(graphToDraw, c, myPen, g);
                }
            }

            if (graphToDraw.DrawingGraph.DebugCurves != null) {
                foreach (DebugCurve shape in graphToDraw.DrawingGraph.DebugCurves)
                    DrawDebugCurve(graphToDraw, shape, g);
            }
        }

        static void DrawDebugCurve(DGraph graph, DebugCurve debugCurve, Graphics graphics) {

            using (var pen = new Pen(GetColorFromString(debugCurve), (float)debugCurve.Width))
            using (var brush = new SolidBrush(GetFillColorFromString(debugCurve))) {
                if (debugCurve.DashArray != null) {
                    pen.DashStyle = DashStyle.Dash;
                    pen.DashPattern = CreateDashArray(debugCurve.DashArray);
                    pen.DashOffset = pen.DashPattern[0];
                }
                DrawDebugCurve(graph, debugCurve.Curve, pen, brush, graphics, debugCurve.Label);
            }
        }

        static float[] CreateDashArray(double[] dashArray) {
            var ret = new float[dashArray.Length];
            for (int i = 0; i < dashArray.Length; i++)
                ret[i] = (float)dashArray[i];
            return ret;
        }

        static Drawing.Color StringToMsaglColor(string val) {
            return DrawingColorToGLEEColor(GetDrawingColorFromString(val));
        }

        internal static Drawing.Color DrawingColorToGLEEColor(Color drawingColor) {
            return new Drawing.Color(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }
        static string Get(string[] a, int i) {
            if (i < a.Length) return a[i];
            return "";
        }
        static Color GetDrawingColorFromString(string val) {
            // object o=FindInColorTable(val);
            //	if(o==null)	//could be an RGB color

            try {
                if (val.IndexOf(" ") != -1) {
                    string[] nums = Split(val);
                    double H = GetNumber(Get(nums, 0)); //hue
                    double S = GetNumber(Get(nums, 1)); //saturation
                    double V = GetNumber(Get(nums, 2)); //value
                    double r, g, b;

                    H *= 360.0;
                    if (S == 0) r = g = b = V;
                    else {
                        int Hi = ((int)(H + 0.5)) / 60;
                        double f = H / 60.0 - Hi;
                        double p = V * (1.0 - S);
                        double q = V * (1.0 - (S * f));
                        double t = V * (1.0 - (S * (1.0 - f)));

                        if (Hi == 0) {
                            r = V;
                            g = t;
                            b = p;
                        }
                        else if (Hi == 1) {
                            r = q;
                            g = V;
                            b = p;
                        }
                        else if (Hi == 2) {
                            r = p;
                            g = V;
                            b = t;
                        }
                        else if (Hi == 3) {
                            r = p;
                            g = q;
                            b = V;
                        }
                        else if (Hi == 4) {
                            r = t;
                            g = p;
                            b = V;
                        }
                        else if (Hi == 5) {
                            r = V;
                            g = p;
                            b = q;
                        }
                        else throw new Exception("unexpected value of Hi " + Hi);
                    }
                    return Color.FromArgb(ToByte(r), ToByte(g), ToByte(b));
                }
                else if (val[0] == '#') //could be #%2x%2x%2x or #%2x%2x%2x%2x
                    if (val.Length == 7) {
                        int r = FromX(val.Substring(1, 2));
                        int g = FromX(val.Substring(3, 2));
                        int b = FromX(val.Substring(5, 2));

                        return Color.FromArgb(r, g, b);
                    }
                    else if (val.Length == 9) {
                        int r = FromX(val.Substring(1, 2));
                        int g = FromX(val.Substring(3, 2));
                        int b = FromX(val.Substring(5, 2));
                        int a = FromX(val.Substring(7, 2));

                        return Color.FromArgb(a, r, g, b);
                    }
                    else
                        throw new Exception("unexpected color " + val);
                else
                    return FromNameOrBlack(val);
            }
            catch {
                return FromNameOrBlack(val);
            }
        }
        static int FromX(string s) {
            return Int32.Parse(s, NumberStyles.AllowHexSpecifier, AttributeBase.USCultureInfo);
        }

        static int ToByte(double c) {
            var ret = (int)(255.0 * c + 0.5);
            if (ret > 255)
                ret = 255;
            else if (ret < 0)
                ret = 0;
            return ret;
        }
        static Color FromNameOrBlack(string val) {
            Color ret = Color.FromName(val);
            if (ret.A == 0 && ret.R == 0 && ret.B == 0 && ret.G == 0) //the name is not recognized
                return Color.Black;
            return ret;
        }

        static string[] Split(string txt) {
            return txt.Split(new char[] { ' ', ',', '\n', '\r', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        static double GetNumber(string txt) {
            int i;
            double d;
            if (Int32.TryParse(txt, out i)) {
                return i;
            }
            if (Double.TryParse(txt, out d)) {
                return d;
            }
            if (txt == "") return 0;
            throw new Exception(String.Format("Cannot convert \"{0}\" to a number", txt));
        }

        static Color GetColorFromString(DebugCurve curve) {
            Drawing.Color msaglColor = StringToMsaglColor(curve.Color);
            msaglColor.A = curve.Transparency;
            return MsaglColorToDrawingColor(msaglColor);
        }

        static Color GetFillColorFromString(DebugCurve curve) {
            if (curve.FillColor == null || curve.FillColor == "") return Color.Transparent;
            Drawing.Color msaglColor = StringToMsaglColor(curve.FillColor);
            msaglColor.A = curve.Transparency;
            return MsaglColorToDrawingColor(msaglColor);
        }

        static void DrawDebugCurve(DGraph graphToDraw, ICurve c, Pen myPen, SolidBrush solidBrush, Graphics g, object id) {

            var p = c as Polyline;
            if (p != null) {
                if (solidBrush.Color != Color.Transparent)
                    g.FillPolygon(solidBrush, GetPolylinePoints(p));
                if (p.Closed) {
                    g.DrawPolygon(myPen, GetPolylinePoints(p));

                }
                else
                    g.DrawLines(myPen, GetPolylinePoints(p));

            }
            else {
                if (SimpleSeg(c)) {
                    DrawSimpleSeg(c, g, myPen, graphToDraw);
                }
                else {
                    var curve = c as Curve;
                    if (curve != null)
                        foreach (ICurve ss in curve.Segments) {
                            DrawSimpleSeg(ss, g, myPen, graphToDraw);
                        }
                    else {
                        var rect = c as RoundedRect;
                        if (rect != null) {
                            foreach (ICurve ss in rect.Curve.Segments)
                                DrawSimpleSeg(ss, g, myPen, graphToDraw);
                        }
                    }
                }
            }
            if (id != null && c != null) {
                var s = id.ToString();
                var brush = new SolidBrush(myPen.Color);
                var point = c.Start;
                var rect = new RectangleF((float)c.Start.X, (float)c.Start.Y, (float)c.BoundingBox.Width, (float)c.BoundingBox.Height);
                DrawStringInRectCenter(g, brush, new Font(FontFamily.GenericSerif, 10), s, rect);

            }

        }

        static void DrawSimpleSeg(ICurve c, Graphics g, Pen myPen, DGraph graphToDraw) {
            var lineSeg = c as LineSegment;

            if (lineSeg != null) {
                g.DrawLine(myPen, (float)lineSeg.Start.X, (float)lineSeg.Start.Y, (float)lineSeg.End.X,
                           (float)lineSeg.End.Y);
            }
            else {
                var bs = c as CubicBezierSegment;
                if (bs != null) {
                    g.DrawBezier(myPen, (float)bs.B(0).X, (float)bs.B(0).Y,
                                 (float)bs.B(1).X, (float)bs.B(1).Y,
                                 (float)bs.B(2).X, (float)bs.B(2).Y,
                                 (float)bs.B(3).X, (float)bs.B(3).Y);
                    if (graphToDraw.DrawingGraph.ShowControlPoints)
                        DrawControlPoints(g, bs);
                }
                else {
                    var el = c as Ellipse;
                    if (el != null) {
                        DrawArc(myPen, g, el);
                    }
                    else {
                        throw new InvalidOperationException();
                    }
                }
            }
        }

        static bool SimpleSeg(ICurve curve) {
            return curve is LineSegment || curve is Ellipse || curve is CubicBezierSegment;
        }


        static void DrawDebugICurve(DGraph graphToDraw, ICurve c, Pen myPen, Graphics g) {
            var p = c as Polyline;
            if (p != null) {
                SetColor(graphToDraw, myPen, p);
                DrawPolyline(p, myPen, g);
            }
            else {
                var lineSeg = c as LineSegment;

                if (lineSeg != null) {
                    SetColor(graphToDraw, myPen, lineSeg);

                    DrawLine(myPen, g, lineSeg);
                }
                else {
                    var bs = c as CubicBezierSegment;
                    if (bs != null) {
                        SetColor(graphToDraw, myPen, bs);
                        DrawBezier(graphToDraw, myPen, g, bs);
                    }
                    else {
                        var el = c as Ellipse;
                        if (el != null) {
                            SetColor(graphToDraw, myPen, el);
                            DrawArc(myPen, g, el);
                        }
                        else {
                            var curve = c as Curve;
                            if (curve != null)
                                foreach (ICurve ss in curve.Segments)
                                    DrawDebugICurve(graphToDraw, ss, myPen, g);
                            else {
                                var rect = c as RoundedRect;
                                if (rect != null) {
                                    foreach (ICurve ss in rect.Curve.Segments)
                                        DrawDebugICurve(graphToDraw, ss, myPen, g);
                                }
                            }

                        }
                    }
                }
            }
        }

        static void DrawArc(Pen pen, Graphics g, Ellipse el) {
            double sweepAngle;
            BBox box;
            float startAngle;
            GetGdiArcDimensions(el, out startAngle, out sweepAngle, out box);
            //an exception is thrown for very small arcs
            if (box.Width < 0.01 || box.Height < 0.01 || ((el.ParEnd - el.ParStart) < (Math.PI / 4) && (el.End - el.Start).Length < 0.01)) {
                g.DrawLines(pen, EllipsePoints(10, el));
            }
            else {
                g.DrawArc(pen,
                          (float)box.Left,
                          (float)box.Bottom,
                          (float)box.Width,
                          (float)box.Height,
                          startAngle,
                          (float)sweepAngle);
            }
        }

        static PointF[] EllipsePoints(int n, Ellipse el) {
            var ret = new PointF[n + 1];
            var del = (el.ParEnd - el.ParStart) / n;
            for (int i = 0; i <= n; i++) {
                ret[i] = PP(el[el.ParStart + i * del]);
            }
            return ret;
        }

        static void DrawBezier(DGraph graphToDraw, Pen myPen, Graphics g, CubicBezierSegment bs) {
            g.DrawBezier(myPen, (float)bs.B(0).X, (float)bs.B(0).Y,
                         (float)bs.B(1).X, (float)bs.B(1).Y,
                         (float)bs.B(2).X, (float)bs.B(2).Y,
                         (float)bs.B(3).X, (float)bs.B(3).Y);
            if (graphToDraw.DrawingGraph.ShowControlPoints)
                DrawControlPoints(g, bs);
        }

        static void DrawLine(Pen myPen, Graphics g, LineSegment lineSeg) {
            g.DrawLine(myPen, (float)lineSeg.Start.X, (float)lineSeg.Start.Y, (float)lineSeg.End.X,
                       (float)lineSeg.End.Y);
        }

        static void DrawPolyline(Polyline p, Pen myPen, Graphics g) {
            if (p.Closed)
                g.DrawPolygon(myPen, GetPolylinePoints(p));
            //g.FillPolygon(new SolidBrush(myPen.Color), GetPolylinePoints(p));
            else
                g.DrawLines(myPen, GetPolylinePoints(p));
        }


        static PointF[] GetPolylinePoints(Polyline p) {
            var ret = new List<PointF>();
            foreach (P2 pnt in p) {
                ret.Add(new PointF((float)pnt.X, (float)pnt.Y));
            }
            return ret.ToArray();
        }

        static void SetColor(DGraph graphToDraw, Pen myPen, object bs) {
            // Microsoft.Msagl.Drawing.Color color;
            if (bs is CubicBezierSegment)
                myPen.Color = Color.Green;
            else if (bs is Polyline)
                myPen.Color = Color.Brown;
            else
                myPen.Color = Color.Blue;
        }


        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString"
            )]
        internal static void DrawDataBase(Graphics g, Pen myPen, DrawingGraph dg) {
            int i = 0;

            foreach (Anchor p in dg.DataBase.Anchors)
                i = DrawAnchor(g, i, p);


            myPen.Color = Color.Blue;
            Pen myOtherPen = new Pen(Color.FromArgb(100, 0, 0, 255), 1);
            foreach (var edges in dg.DataBase.Multiedges.Values)
                foreach (PolyIntEdge e in edges) {
                    //                    if (e.LayerEdges != null)
                    //                        foreach (LayerEdge le in e.LayerEdges) {
                    //                            g.DrawLine(myPen, PointF(dg.DataBase.Anchors[le.Source].Origin),
                    //                                       PointF(dg.DataBase.Anchors[le.Target].Origin));
                    //                        }
                    if (e.Edge.UnderlyingPolyline == null) continue;
                    var points = e.Edge.UnderlyingPolyline.ToArray();
                    for (int j = 0; j < points.Length - 1; j++)
                        g.DrawLine(myOtherPen, PointF(points[j]), PointF(points[j + 1]));
                }

            myPen.Color = Color.Red;
            if (dg.DataBase.nodesToShow == null)
                foreach (var li in dg.DataBase.Multiedges.Values)
                    foreach (PolyIntEdge ie in li)
                        if (ie.Edge.Curve is Curve) {
                            foreach (ICurve s in (ie.Edge.Curve as Curve).Segments) {
                                var bs = s as CubicBezierSegment;
                                if (bs != null) {
                                    g.DrawBezier(myPen, (float)bs.B(0).X, (float)bs.B(0).Y,
                                                 (float)bs.B(1).X, (float)bs.B(1).Y,
                                                 (float)bs.B(2).X, (float)bs.B(2).Y,
                                                 (float)bs.B(3).X, (float)bs.B(3).Y);
                                }
                                else {
                                    var ls = s as LineSegment;
                                    g.DrawLine(myPen, (float)ls.Start.X, (float)ls.Start.Y,
                                               (float)ls.End.X, (float)ls.End.Y);
                                }
                            }

                            myPen.Color = Color.FromArgb(50, 100, 100, 0);
                            if (ie.Edge.UnderlyingPolyline != null)
                                foreach (LineSegment ls in ie.Edge.UnderlyingPolyline.GetSegments())
                                    g.DrawLine(myPen, (float)ls.Start.X, (float)ls.Start.Y,
                                               (float)ls.End.X, (float)ls.End.Y);
                            myPen.Color = Color.Red;
                        }
        }


        static int DrawAnchor(Graphics g, int i, Anchor p) {
            string stringToShow = i + (p.UserData != null ? (" " + p.UserData) : String.Empty);

            DrawStringInRectCenter(g, Brushes.Blue, new Font(FontFamily.GenericSerif, 10), stringToShow,
                                   new RectangleF((float)p.Left, (float)p.Bottom,
                                                  (float)p.RightAnchor + (float)p.LeftAnchor,
                                                  (float)p.TopAnchor + (float)p.BottomAnchor));
            i++;
            return i;
        }

        internal static void DrawControlPoints(Graphics g, CubicBezierSegment bs) {
            using (var pen = new Pen(Color.Green, (float)(1.0 / 1000.0))) {
                pen.DashPattern = new[] { 1, (float)1 };

                pen.DashStyle = DashStyle.Dot;
                g.DrawLine(pen, PointF(bs.B(0)), PointF(bs.B(1)));
                g.DrawLine(pen, PointF(bs.B(1)), PointF(bs.B(2)));
                g.DrawLine(pen, PointF(bs.B(2)), PointF(bs.B(3)));
            }
        }
#endif


        internal static void AddStyleForPen(DObject dObj, Pen myPen, Style style) {
            if (style == Style.Dashed) {
                myPen.DashStyle = DashStyle.Dash;

                if (dObj.DashPatternArray == null) {
                    float f = dObj.DashSize();
                    dObj.DashPatternArray = new[] { f, f };
                }
                myPen.DashPattern = dObj.DashPatternArray;

                myPen.DashOffset = dObj.DashPatternArray[0];
            }
            else if (style == Style.Dotted) {
                myPen.DashStyle = DashStyle.Dash;
                if (dObj.DashPatternArray == null) {
                    float f = dObj.DashSize();
                    dObj.DashPatternArray = new[] { 1, f };
                }
                myPen.DashPattern = dObj.DashPatternArray;
            }
        }

        internal static void DrawEdgeArrows(Graphics g, DrawingEdge edge, Color edgeColor, Pen myPen) {
            ArrowAtTheEnd(g, edge, edgeColor, myPen);
            ArrawAtTheBeginning(g, edge, edgeColor, myPen);
        }

        static void ArrawAtTheBeginning(Graphics g, DrawingEdge edge, Color edgeColor, Pen myPen) {
            if (edge.GeometryEdge != null && edge.Attr.ArrowAtSource)
                DrawArrowAtTheBeginningWithControlPoints(g, edge, edgeColor, myPen);
        }


        static void DrawArrowAtTheBeginningWithControlPoints(Graphics g, DrawingEdge edge, Color edgeColor, Pen myPen) {
            if (edge.EdgeCurve != null)
                if (edge.Attr.ArrowheadAtSource == ArrowStyle.None)
                    DrawLine(g, myPen, edge.EdgeCurve.Start,
                             edge.ArrowAtSourcePosition);
                else
                    using (var sb = new SolidBrush(edgeColor))
                        DrawArrow(g, sb, edge.EdgeCurve.Start,
                                  edge.ArrowAtSourcePosition, edge.Attr.LineWidth, edge.Attr.ArrowheadAtSource);
        }

        static void ArrowAtTheEnd(Graphics g, DrawingEdge edge, Color edgeColor, Pen myPen) {
            if (edge.GeometryEdge != null && edge.Attr.ArrowAtTarget)
                DrawArrowAtTheEndWithControlPoints(g, edge, edgeColor, myPen);
        }

        static void DrawArrowAtTheEndWithControlPoints(Graphics g, DrawingEdge edge, Color edgeColor, Pen myPen) {
            if (edge.EdgeCurve != null)
                if (edge.Attr.ArrowheadAtTarget == ArrowStyle.None)
                    DrawLine(g, myPen, edge.EdgeCurve.End,
                             edge.ArrowAtTargetPosition);
                else
                    using (var sb = new SolidBrush(edgeColor))
                        DrawArrow(g, sb, edge.EdgeCurve.End,
                                  edge.ArrowAtTargetPosition, edge.Attr.LineWidth, edge.Attr.ArrowheadAtTarget);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iCurve"></param>
        /// <returns></returns>
        public static GraphicsPath CreateGraphicsPath(ICurve iCurve) {
            var graphicsPath = new GraphicsPath();
            if (iCurve == null)
                return null;
            var c = iCurve as Curve;

            if (c != null)
                HandleCurve(c, graphicsPath);
            else {
                var ls = iCurve as LineSegment;
                if (ls != null)
                    graphicsPath.AddLine(PointF(ls.Start), PointF(ls.End));
                else {
                    var seg = iCurve as CubicBezierSegment;
                    if (seg != null)
                        graphicsPath.AddBezier(PointF(seg.B(0)), PointF(seg.B(1)), PointF(seg.B(2)), PointF(seg.B(3)));
                    else {
                        var ellipse = iCurve as Ellipse;
                        if (ellipse != null)
                            AddEllipseSeg(graphicsPath, iCurve as Ellipse);
                        else {
                            var poly = iCurve as Polyline;
                            if (poly != null) HandlePolyline(poly, graphicsPath);
                            else {
                                var rr = (RoundedRect)iCurve;
                                HandleCurve(rr.Curve, graphicsPath);
                            }
                        }
                    }
                }
            }

            /* 
             if (false) {
                 if (c != null) {
                     foreach (var s in c.Segments) {
                         CubicBezierSegment cubic = s as CubicBezierSegment;
                         if (cubic != null)
                             foreach (var t in cubic.MaximalCurvaturePoints) {
                                 graphicsPath.AddPath(CreatePathOnCurvaturePoint(t, cubic), false);
                             }

                     }
                 } else {
                     CubicBezierSegment cubic = iCurve as CubicBezierSegment;
                     if (cubic != null) {
                         foreach (var t in cubic.MaximalCurvaturePoints) {
                             graphicsPath.AddPath(CreatePathOnCurvaturePoint(t, cubic), false);
                         }
                     }
                 }
             }

              */

            return graphicsPath;
        }

        static void HandlePolyline(Polyline poly, GraphicsPath graphicsPath) {
            graphicsPath.AddLines(poly.Select(PointF).ToArray());
            if (poly.Closed)
                graphicsPath.CloseFigure();
        }

        static void HandleCurve(Curve c, GraphicsPath graphicsPath) {
            foreach (ICurve seg in c.Segments) {
                var cubic = seg as CubicBezierSegment;
                if (cubic != null)
                    graphicsPath.AddBezier(PointF(cubic.B(0)), PointF(cubic.B(1)), PointF(cubic.B(2)),
                                           PointF(cubic.B(3)));
                else {
                    var ls = seg as LineSegment;
                    if (ls != null)
                        graphicsPath.AddLine(PointF(ls.Start), PointF(ls.End));
                    else {
                        var el = seg as Ellipse;
                        //                            double del = (el.ParEnd - el.ParStart)/11.0;
                        //                            graphicsPath.AddLines(Enumerable.Range(1, 10).Select(i => el[el.ParStart + del*i]).
                        //                                    Select(p => new PointF((float) p.X, (float) p.Y)).ToArray());

                        AddEllipseSeg(graphicsPath, el);
                    }
                }
            }
        }

        static void AddEllipseSeg(GraphicsPath graphicsPath, Ellipse el) {
            double sweepAngle;
            BBox box;
            float startAngle;
            GetGdiArcDimensions(el, out startAngle, out sweepAngle, out box);

            graphicsPath.AddArc((float)box.Left,
                                (float)box.Bottom,
                                (float)box.Width,
                                (float)box.Height,
                                startAngle,
                                (float)sweepAngle);
        }

#if TEST_MSAGL || DEVTRACE
        static PointF PP(P2 point) {
            return new PointF((float)point.X, (float)point.Y);
        }
#endif

        static bool NeedToFill(Color fillColor) {
            return fillColor.A != 0; //the color is not transparent
        }

        internal static void DrawDoubleCircle(Graphics g, Pen pen, DNode dNode) {
            var drNode = dNode.DrawingNode;
            NodeAttr nodeAttr = drNode.Attr;

            double x = drNode.GeometryNode.Center.X - drNode.GeometryNode.Width / 2.0f;
            double y = drNode.GeometryNode.Center.Y - drNode.GeometryNode.Height / 2.0f;
            if (NeedToFill(dNode.FillColor)) {
                g.FillEllipse(new SolidBrush(dNode.FillColor), (float)x, (float)y, (float)drNode.Width,
                              (float)drNode.Height);
            }

            g.DrawEllipse(pen, (float)x, (float)y, (float)drNode.Width, (float)drNode.Height);
            var w = (float)drNode.Width;
            var h = (float)drNode.Height;
            float m = Math.Max(w, h);
            float coeff = (float)1.0 - (float)(DoubleCircleOffsetRatio);
            x += coeff * m / 2.0;
            y += coeff * m / 2.0;
            g.DrawEllipse(pen, (float)x, (float)y, w - coeff * m, h - coeff * m);
        }

        static Color FillColor(NodeAttr nodeAttr) {
            return MsaglColorToDrawingColor(nodeAttr.FillColor);
        }

        ///<summary>
        ///</summary>
        internal const double ArrowAngle = 25.0; //degrees

        internal static void DrawArrow(Graphics g, Brush brush, P2 start, P2 end, double lineWidth,
                                       ArrowStyle arrowStyle) {
            switch (arrowStyle) {
                case ArrowStyle.NonSpecified:
                case ArrowStyle.Normal:

                    DrawNormalArrow(g, brush, ref start, ref end, lineWidth);
                    break;
                case ArrowStyle.Tee:
                    DrawTeeArrow(g, brush, ref start, ref end, lineWidth);
                    break;
                case ArrowStyle.Diamond:
                    DrawDiamondArrow(g, brush, ref start, ref end, lineWidth);
                    break;
                case ArrowStyle.ODiamond:
                    DrawODiamondArrow(g, brush, ref start, ref end, lineWidth);
                    break;
                case ArrowStyle.Generalization:
                    DrawGeneralizationArrow(g, brush, ref start, ref end, lineWidth);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal static void DrawNormalArrow(Graphics g, Brush brush, ref P2 start, ref P2 end, double lineWidth) {
            PointF[] points;

            if (lineWidth == 1) {
                P2 dir = end - start;
                P2 h = dir;
                dir /= dir.Length;

                var s = new P2(-dir.Y, dir.X);

                s *= h.Length * ((float)Math.Tan(ArrowAngle * 0.5f * (Math.PI / 180.0)));

                points = new[] { PointF(start + s), PointF(end), PointF(start - s) };
            }
            else {
                P2 dir = end - start;
                P2 h = dir;
                dir /= dir.Length;
                var s = new P2(-dir.Y, dir.X);
                float w = (float)(0.5 * lineWidth);
                P2 s0 = w * s;
                double al = ArrowAngle * 0.5f * (Math.PI / 180.0);
                s *= h.Length * ((float)Math.Tan(al));
                s += s0;

                points = new[] { PointF(start + s), PointF(start - s), PointF(end - s0), PointF(end + s0) };
                P2 center = end - dir * w * (float)Math.Tan(al);
                double rad = w / Math.Cos(al);
                g.FillEllipse(brush,
                              (float)center.X - (float)rad,
                              (float)center.Y - (float)rad,
                              2.0f * (float)rad,
                              2.0f * (float)rad);
            }


            g.FillPolygon(brush, points);
        }

        static void DrawTeeArrow(Graphics g, Brush brush, ref P2 start, ref P2 end, double lineWidth) {
            double lw = lineWidth == -1 ? 1 : lineWidth;
            using (var p = new Pen(brush, (float)lw)) {
                g.DrawLine(p, PointF(start), PointF(end));
                P2 dir = end - start;
                P2 h = dir;
                dir /= dir.Length;

                var s = new P2(-dir.Y, dir.X);

                s *= 2 * h.Length * ((float)Math.Tan(ArrowAngle * 0.5f * (Math.PI / 180.0)));
                s += (1 + lw) * s.Normalize();

                g.DrawLine(p, PointF(start + s), PointF(start - s));
            }
        }

        internal static void DrawDiamondArrow(Graphics g, Brush brush, ref P2 start, ref P2 end, double lineWidth) {
            double lw = lineWidth == -1 ? 1 : lineWidth;
            using (var p = new Pen(brush, (float)lw)) {
                P2 dir = end - start;
                P2 h = dir;
                dir /= dir.Length;

                var s = new P2(-dir.Y, dir.X);

                var points = new[]{
                                      PointF(start - dir), PointF(start + (h/2) + s*(h.Length/3)), PointF(end),
                                      PointF(start + (h/2) - s*(h.Length/3))
                                  };
                g.FillPolygon(p.Brush, points);
            }
        }

        internal static void DrawODiamondArrow(Graphics g, Brush brush, ref P2 start, ref P2 end, double lineWidth) {
            double lw = lineWidth == -1 ? 1 : lineWidth;
            using (var p = new Pen(brush, (float)lw)) {
                P2 dir = end - start;
                P2 h = dir;
                dir /= dir.Length;

                var s = new P2(-dir.Y, dir.X);

                var points = new[]{
                                      PointF(start - dir), PointF(start + (h/2) + s*(h.Length/3)), PointF(end),
                                      PointF(start + (h/2) - s*(h.Length/3))
                                  };
                g.DrawPolygon(p, points);
            }
        }

        internal static void DrawGeneralizationArrow(Graphics g, Brush brush, ref P2 start, ref P2 end,
                                                     double lineWidth) {
            double lw = lineWidth == -1 ? 1 : lineWidth;
            using (var p = new Pen(brush, (float)lw)) {
                P2 dir = end - start;
                P2 h = dir;
                dir /= dir.Length;

                var s = new P2(-dir.Y, dir.X);

                var points = new[]{
                                      PointF(start), PointF(start + s*(h.Length/2)), PointF(end), PointF(start - s*(h.Length/2))
                                  };

                // g.FillPolygon(p.Brush, points);
                g.DrawPolygon(p, points);
            }
        }

        internal static void DrawLine(Graphics g, Pen pen, P2 start, P2 end) {
            g.DrawLine(pen, PointF(start), PointF(end));
        }


        internal static void DrawBox(Graphics g, Pen pen, DNode dNode) {
            var drNode = dNode.DrawingNode;
            NodeAttr nodeAttr = drNode.Attr;
            if (nodeAttr.XRadius == 0 || nodeAttr.YRadius == 0) {
                double x = drNode.GeometryNode.Center.X - drNode.Width / 2.0f;
                double y = drNode.GeometryNode.Center.Y - drNode.Height / 2.0f;

                if (NeedToFill(dNode.FillColor)) {
                    Color fc = FillColor(nodeAttr);
                    g.FillRectangle(new SolidBrush(fc), (float)x, (float)y, (float)drNode.Width,
                                    (float)drNode.Height);
                }

                g.DrawRectangle(pen, (float)x, (float)y, (float)drNode.Width, (float)drNode.Height);
            }
            else {
                var width = (float)drNode.Width;
                var height = (float)drNode.Height;
                var xRadius = (float)nodeAttr.XRadius;
                var yRadius = (float)nodeAttr.YRadius;
                using (var path = new GraphicsPath()) {
                    FillTheGraphicsPath(drNode, width, height, ref xRadius, ref yRadius, path);

                    if (NeedToFill(dNode.FillColor)) {
                        g.FillPath(new SolidBrush(dNode.FillColor), path);
                    }


                    g.DrawPath(pen, path);
                }
            }
        }

        static void FillTheGraphicsPath(DrawingNode drNode, float width, float height, ref float xRadius,
                                        ref float yRadius, GraphicsPath path) {
            NodeAttr nodeAttr = drNode.Attr;
            float w = (width / 2);
            if (xRadius > w)
                xRadius = w;
            float h = (height / 2);
            if (yRadius > h)
                yRadius = h;
            var x = (float)drNode.GeometryNode.Center.X;
            var y = (float)drNode.GeometryNode.Center.Y;
            float ox = w - xRadius;
            float oy = h - yRadius;
            float top = y + h;
            float bottom = y - h;
            float left = x - w;
            float right = x + w;

            const float PI = 180;
            if (ox > 0)
                path.AddLine(x - ox, bottom, x + ox, bottom);
            path.AddArc(x + ox - xRadius, y - oy - yRadius, 2 * xRadius, 2 * yRadius, 1.5f * PI, 0.5f * PI);

            if (oy > 0)
                path.AddLine(right, y - oy, right, y + oy);
            path.AddArc(x + ox - xRadius, y + oy - yRadius, 2 * xRadius, 2 * yRadius, 0, 0.5f * PI);
            if (ox > 0)
                path.AddLine(x + ox, top, x - ox, top);
            path.AddArc(x - ox - xRadius, y + oy - yRadius, 2 * xRadius, 2 * yRadius, 0.5f * PI, 0.5f * PI);
            if (oy > 0)
                path.AddLine(left, y + oy, left, y - oy);
            path.AddArc(x - ox - xRadius, y - oy - yRadius, 2 * xRadius, 2 * yRadius, PI, 0.5f * PI);
        }


        internal static void DrawDiamond(Graphics g, Pen pen, DNode dNode) {
            var drNode = dNode.DrawingNode;
            NodeAttr nodeAttr = drNode.Attr;

            double w2 = drNode.Width / 2.0f;
            double h2 = drNode.Height / 2.0f;
            double cx = drNode.Pos.X;
            double cy = drNode.Pos.Y;
            var ps = new[]{
                              new PointF((float) cx - (float) w2, (float) cy),
                              new PointF((float) cx, (float) cy + (float) h2),
                              new PointF((float) cx + (float) w2, (float) cy),
                              new PointF((float) cx, (float) cy - (float) h2)
                          };

            if (NeedToFill(dNode.FillColor)) {
                Color fc = FillColor(nodeAttr);
                g.FillPolygon(new SolidBrush(fc), ps);
            }

            g.DrawPolygon(pen, ps);
        }

        internal static void DrawEllipse(Graphics g, Pen pen, DNode dNode) {
            var drNode = dNode.DrawingNode;
            NodeAttr nodeAttr = drNode.Attr;
            var width = (float)drNode.Width;
            var height = (float)drNode.Height;
            var x = (float)(drNode.Pos.X - width / 2.0);
            var y = (float)(drNode.Pos.Y - height / 2.0);

            DrawEllipseOnPosition(dNode, nodeAttr, g, x, y, width, height, pen);
        }

        static void DrawEllipseOnPosition(DNode dNode, NodeAttr nodeAttr, Graphics g, float x, float y, float width,
                                          float height, Pen pen) {
            if (NeedToFill(dNode.FillColor))
                g.FillEllipse(new SolidBrush(dNode.FillColor), x, y, width, height);
            if (nodeAttr.Shape == Shape.Point)
                g.FillEllipse(new SolidBrush(pen.Color), x, y, width, height);

            g.DrawEllipse(pen, x, y, width, height);
        }

        //static internal void DrawGraphBBox(Graphics g,DGraph graphToDraw)
        //{

        //  foreach( Style style in graphToDraw.DrawingGraph.GraphAttr.Styles)
        //  {
        //    if(style==Style.Filled)
        //    {
        //      BBox bb=graphToDraw.DrawingGraph.GraphAttr.BoundingBox;

        //      g.FillRectangle(
        //        new SolidBrush(System.Drawing.Color.LightSteelBlue),
        //        (float)bb.LeftTop.X,(float)bb.LeftTop.Y,(float)bb.RightBottom.X-(float)bb.LeftTop.X,-(float)bb.RightBottom.Y+(float)bb.RightBottom.Y);

        //      return;
        //    }
        //  }

        //  if(!(graphToDraw.DrawingGraph.GraphAttr.Backgroundcolor.A==0))
        //  {
        //    BBox bb=graphToDraw.DrawingGraph.GraphAttr.BoundingBox;

        //    SolidBrush brush=new SolidBrush((MsaglColorToDrawingColor( graphToDraw.DrawingGraph.GraphAttr.Backgroundcolor)));

        //    if(!bb.IsEmpty)
        //      g.FillRectangle(brush, 
        //      (float)	bb.LeftTop.X,(float)bb.LeftTop.Y,(float)bb.RightBottom.X-(float)bb.LeftTop.X,-(float)bb.LeftTop.Y+(float)bb.RightBottom.Y);
        //  }

        //}


        //don't know what to do about the throw-catch block
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static void DrawLabel(Graphics g, DLabel label) {
            if (label == null || label.DrawingLabel.Width == 0)
                return;

            var rectF = GetLabelRect(label);

            try {
                DrawStringInRectCenter(g, new SolidBrush(MsaglColorToDrawingColor(label.DrawingLabel.FontColor)),
                                       label.Font, label.DrawingLabel.Text, rectF);
            }
            catch {
            }
            if (label.MarkedForDragging) {
                var pen = new Pen(MsaglColorToDrawingColor(label.DrawingLabel.FontColor));
                pen.DashStyle = DashStyle.Dot;
                DrawLine(g, pen, label.DrawingLabel.GeometryLabel.AttachmentSegmentStart,
                         label.DrawingLabel.GeometryLabel.AttachmentSegmentEnd);
            }
        }

        private static RectangleF GetLabelRect(DLabel label) {
            var subgraph = label.DrawingLabel.Owner as Subgraph;
            if (subgraph != null) {
                var cluster = (Cluster)subgraph.GeometryNode;
                var rb = cluster.RectangularBoundary;
                double cy = cluster.BoundingBox.Top, cx = rb.Rect.Left + rb.Rect.Width / 2;
                switch (subgraph.Attr.ClusterLabelMargin) {
                    case LgNodeInfo.LabelPlacement.Top:
                        cy -= rb.TopMargin / 2;
                        break;
                    case LgNodeInfo.LabelPlacement.Bottom:
                        cy -= rb.BottomMargin / 2;
                        break;
                    case LgNodeInfo.LabelPlacement.Left:
                        cy -= rb.LeftMargin / 2;
                        break;
                    case LgNodeInfo.LabelPlacement.Right:
                        cy -= rb.RightMargin / 2;
                        break;
                }
                var size = label.DrawingLabel.Size;
                return new RectangleF(
                    (float)(cx - size.Width / 2),
                    (float)(cy - size.Height / 2),
                    (float)size.Width,
                    (float)size.Height);
            }
            else {
                var rectF = new RectangleF((float)label.DrawingLabel.Left, (float)label.DrawingLabel.Bottom,
                    (float)label.DrawingLabel.Size.Width,
                    (float)label.DrawingLabel.Size.Height);
                return rectF;
            }
        }

        static void DrawStringInRectCenter(Graphics g, Brush brush, Font f, string s, RectangleF r
            /*, double rectLineWidth*/) {
            if (String.IsNullOrEmpty(s))
                return;

            using (Matrix m = g.Transform) {
                using (Matrix saveM = m.Clone()) {
                    //rotate the label around its center
                    float c = (r.Bottom + r.Top) / 2;

                    using (var m2 = new Matrix(1, 0, 0, -1, 0, 2 * c)) {
                        m.Multiply(m2);
                    }
                    g.Transform = m;
                    using (StringFormat stringFormat = StringFormat.GenericTypographic) {
                        g.DrawString(s, f, brush, r.Left, r.Top, stringFormat);
                    }
                    g.Transform = saveM;
                }
            }
        }

        internal static PointF PointF(P2 p) {
            return new PointF((float)p.X, (float)p.Y);
        }


        internal static void DrawFromMsaglCurve(Graphics g, Pen pen, DNode dNode) {
            var drNode = dNode.DrawingNode;
            NodeAttr attr = dNode.DrawingNode.Attr;
            var iCurve = drNode.GeometryNode.BoundaryCurve;
            var c = iCurve as Curve;
            if (c != null) {
                DrawCurve(dNode, c, g, pen);
            }
            else {
                var ellipse = iCurve as Ellipse;
                if (ellipse != null) {
                    double w = ellipse.AxisA.X;
                    double h = ellipse.AxisB.Y;
                    DrawEllipseOnPosition(dNode, dNode.DrawingNode.Attr, g, (float)(ellipse.Center.X - w),
                                          (float)(ellipse.Center.Y - h),
                                          (float)w * 2, (float)h * 2, pen);
                }
                else {
                    var poly = iCurve as Polyline;
                    if (poly != null) {
                        var path = new GraphicsPath();
                        path.AddLines(poly.Select(p => new Point((int)p.X, (int)p.Y)).ToArray());
                        path.CloseAllFigures();
                        if (NeedToFill(dNode.FillColor))
                            g.FillPath(new SolidBrush(dNode.FillColor), path);
                        g.DrawPath(pen, path);
                    }
                    else {
                        var roundedRect = iCurve as RoundedRect;
                        if (roundedRect != null)
                            DrawCurve(dNode, roundedRect.Curve, g, pen);
                    }
                }

            }
        }

        static void DrawCurve(DNode dNode, Curve c, Graphics g, Pen pen) {
            var path = new GraphicsPath();
            foreach (ICurve seg in c.Segments)
                AddSegToPath(seg, ref path);

            if (NeedToFill(dNode.FillColor))
                g.FillPath(new SolidBrush(dNode.FillColor), path);
            g.DrawPath(pen, path);
        }


        static void AddSegToPath(ICurve seg, ref GraphicsPath path) {
            var line = seg as LineSegment;
            if (line != null)
                path.AddLine(PointF(line.Start), PointF(line.End));
            else {
                var cb = seg as CubicBezierSegment;
                if (cb != null)
                    path.AddBezier(PointF(cb.B(0)), PointF(cb.B(1)), PointF(cb.B(2)), PointF(cb.B(3)));
                else {
                    var ellipse = seg as Ellipse;
                    if (ellipse != null) {
                        //we assume that ellipes are going counterclockwise
                        double cx = ellipse.Center.X;
                        double cy = ellipse.Center.Y;
                        double w = ellipse.AxisA.X * 2;
                        double h = ellipse.AxisB.Y * 2;
                        double sweep = ellipse.ParEnd - ellipse.ParStart;

                        if (sweep < 0)
                            sweep += Math.PI * 2;
                        const double toDegree = 180 / Math.PI;
                        path.AddArc((float)(cx - w / 2), (float)(cy - h / 2), (float)w, (float)h,
                                    (float)(ellipse.ParStart * toDegree), (float)(sweep * toDegree));
                    }
                }
            }
        }

        const double ToDegreesMultiplier = 180 / Math.PI;

        /// <summary>
        /// it is a very tricky function, please change carefully
        /// </summary>
        /// <param name="ellipse"></param>
        /// <param name="startAngle"></param>
        /// <param name="sweepAngle"></param>
        /// <param name="box"></param>
        public static void GetGdiArcDimensions(Ellipse ellipse, out float startAngle, out double sweepAngle, out BBox box) {
            box = ellipse.FullBox();
            startAngle = EllipseStandardAngle(ellipse, ellipse.ParStart);
            bool orientedCcw = ellipse.OrientedCounterclockwise();
            if (Math.Abs((Math.Abs(ellipse.ParEnd - ellipse.ParStart) - Math.PI * 2)) < 0.001)//we have a full ellipse
                sweepAngle = 360;
            else
                sweepAngle = (orientedCcw ? P2.Angle(ellipse.Start, ellipse.Center, ellipse.End) : P2.Angle(ellipse.End, ellipse.Center, ellipse.Start))
                    * ToDegreesMultiplier;
            if (!orientedCcw)
                sweepAngle = -sweepAngle;
        }

        static float EllipseStandardAngle(Ellipse ellipse, double angle) {
            P2 p = Math.Cos(angle) * ellipse.AxisA + Math.Sin(angle) * ellipse.AxisB;
            return (float)(Math.Atan2(p.Y, p.X) * ToDegreesMultiplier);
        }

        ///<summary>
        ///</summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        public static void GviewerMouseMove(object sender, MouseEventArgs e) {
            var gviewer = sender as GViewer;
            if (gviewer != null) {
                float viewerX;
                float viewerY;
                gviewer.ScreenToSource(e.Location.X, e.Location.Y, out viewerX, out viewerY);
                var str = String.Format(String.Format("{0},{1}", viewerX, viewerY));
                var form = gviewer.ParentForm;
                foreach (var ch in form.Controls) {
                    var sb = ch as StatusStrip;
                    if (sb != null) {
                        foreach (var item in sb.Items) {
                            var label = item as ToolStripStatusLabel;
                            if (label == null) continue;
                            label.Text = str;
                            return;
                        }
                    }
                }
            }
        }
    }
}