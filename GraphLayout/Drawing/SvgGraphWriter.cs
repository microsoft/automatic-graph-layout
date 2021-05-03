using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using System.Linq;
using Microsoft.Msagl.DebugHelpers;
using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using System.Text.RegularExpressions;

namespace Microsoft.Msagl.Drawing
{
    ///<summary>
    ///</summary>
    public class SvgGraphWriter
    {
        readonly Graph _graph;
        Func<string, string> nodeSanitizer = s => s;
        Func<string, string> attrSanitizer = s => s;
        /// <summary>
        /// if is set to true then no edges are written
        /// </summary>
        public bool IgnoreEdges { get; set; }
        readonly Stream stream;

        readonly XmlWriter xmlWriter;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="streamPar"></param>
        /// <param name="graphP"></param>
        public SvgGraphWriter(Stream streamPar, Graph graphP) {
            InitColorSet();
            stream = streamPar;
            _graph = graphP;
            var xmlWriterSettings = new XmlWriterSettings { Indent = true };
            xmlWriter = XmlWriter.Create(stream, xmlWriterSettings);
        }

        ///<summary>
        ///</summary>
        public SvgGraphWriter() { }
        /// <summary>
        /// 
        /// </summary>
        public XmlWriter XmlWriter
        {
            get { return xmlWriter; }
        }

        ///<summary>
        ///</summary>
        public Func<string, string> NodeSanitizer
        {
            get { return nodeSanitizer; }
            set { nodeSanitizer = value; }
        }

        ///<summary>
        ///</summary>
        public Func<string, string> AttrSanitizer
        {
            get { return attrSanitizer; }
            set { attrSanitizer = value; }
        }

        /// <summary>
        /// Writes the graph to a file
        /// </summary>
        public void Write()
        {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            
            try
            {
                TransformGraphByFlippingY();
                Open();
                WriteGraphAttr(_graph.Attr);
                WriteLabel(_graph.Label);
                WriteEdges();
                WriteNodes();
                
#if TEST_MSAGL
                WriteDebugCurves();
#endif
                Close();
            }
            finally
            {
                TransformGraphByFlippingY();

                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }
#if TEST_MSAGL
        void WriteDebugCurves() {
            if(_graph.DebugCurves!=null)
                foreach (var debugCurve in _graph.DebugCurves) {
                    WriteDebugCurve(debugCurve);
                }
        }

        void WriteDebugCurve(DebugCurve debugCurve) {
            WriteStartElement("path");
            WriteAttribute("fill", "none");
            var iCurve = debugCurve.Curve;
            WriteStroke(debugCurve);
            WriteAttribute("d", CurveString(iCurve));
            if (debugCurve.DashArray != null)
                WriteAttribute("style", DashArrayString(debugCurve.DashArray));
            WriteEndElement();            
        }

        string DashArrayString(double[] dashArray) {
            StringBuilder stringBuilder = new StringBuilder("stroke-dasharray:");
            for (int i = 0; ;) {
                stringBuilder.Append(DoubleToString(dashArray[i]));
                i++;
                if (i < dashArray.Length)
                    stringBuilder.Append(' ');
                else {
                    stringBuilder.Append(';');
                    break;
                }
            }
            return stringBuilder.ToString();
        }

        protected void WriteStroke(DebugCurve debugCurve) {
            var color = ValidColor(debugCurve.Color);
            WriteAttribute("stroke", color);
            WriteAttribute("stroke-opacity", debugCurve.Transparency/255.0);
            WriteAttribute("stroke-width", debugCurve.Width);
        }

        string ValidColor(string color) {
            if (colorSet.Contains(color.ToLower()))
                return color;
            return "black";
        }
#endif
        static bool LabelIsValid(Label label)
        {
            if (label == null || String.IsNullOrEmpty(label.Text) || label.Width == 0)
                return false;
            return true;
        }

        protected virtual void WriteLabel(Label label)
        {
            if (!LabelIsValid(label))
                return;
            //need to remove these hecks. TODO
            const double yScaleAdjustment = 1.5;

            var x = label.Center.X - label.Width / 2;
            var y = label.Center.Y + label.Height / (2 * yScaleAdjustment);
            var fontSize = 16;
            WriteStartElement("text");
            WriteAttribute("x", x);
            WriteAttribute("y", y);
            WriteAttribute("font-family", "Arial");
            WriteAttribute("font-size", fontSize);
            WriteAttribute("fill", MsaglColorToSvgColor(label.FontColor));
            WriteLabelText(label.Text, x, fontSize);
            WriteEndElement();
        }

        private void WriteLabelText(string text, double xContainer, double fontSize) 
        {
            var endOfLines = new List<string> { "\r\n", "\r", "\n" };
            var textLines = Regex.Split(NodeSanitizer(text), "(\r\n|\r|\n)").Where(it => !endOfLines.Contains(it)).ToList();
            var isFirstLine = true;
            textLines.ForEach(line => 
            {
                WriteStartElement("tspan");
                WriteAttribute("x", xContainer);
                if (isFirstLine) {
                    isFirstLine = false;
                    WriteAttribute("dy", -1 * fontSize * (textLines.Count - 1));
                }
                else 
                {
                    WriteAttribute("dy", fontSize);
                }
                xmlWriter.WriteRaw(line);
                WriteEndElement();
            });
        }

        protected string MsaglColorToSvgColor(Color color) {
            if (BlackAndWhite)
                return "#000000";
            return "#" + Color.Xex(color.R) + Color.Xex(color.G) + Color.Xex(color.B);
        }

        /// <summary>
        /// If set then only the black color is used
        /// </summary>
        public bool BlackAndWhite { get; set; }

        protected void WriteAttribute(string attrName, object attrValue)
        {
            if (attrValue is double)
                attrValue = DoubleToString((double)attrValue);
            else if (attrValue is Point)
                attrValue = PointToString((Point)attrValue);
            xmlWriter.WriteAttributeString(attrName, attrValue.ToString());
        }

        protected void WriteAttributeWithPrefix(string prefix, string attrName, object attrValue)
        {
            xmlWriter.WriteAttributeString(prefix, attrName, null, attrValue.ToString());
        }


        static void WriteGraphAttr(GraphAttr graphAttr)
        {
        }




        void Open()
        {
            WriteComment("SvgWriter version " + typeof(SvgGraphWriter).Assembly.GetName().Version);
            var box = _graph.BoundingBox;
            xmlWriter.WriteStartElement("svg", "http://www.w3.org/2000/svg");
            WriteAttributeWithPrefix("xmlns", "xlink", "http://www.w3.org/1999/xlink");
            WriteAttribute("width", box.Width);
            WriteAttribute("height", box.Height);
            WriteAttribute("id", "svg2");
            WriteAttribute("version", "1.1");
            WriteStartElement("g");
            WriteAttribute("transform", String.Format("translate({0},{1})", -box.Left, -box.Bottom));
        }
        /// <summary>
        /// writes the end of the file end closes the the stream
        /// </summary>
        public void Close()
        {
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Flush();
            xmlWriter.Close();
        }

        protected void WriteEdges() {
            if (IgnoreEdges) return;
            WriteComment("Edges");
            foreach (Edge edge in _graph.Edges)
                WriteEdge(edge);
        }

        protected virtual void WriteEdge(Edge edge)
        {
            WriteStartElement("path");
            WriteAttribute("fill", "none");
            var geometryEdge = edge.GeometryEdge;
            var iCurve = geometryEdge.Curve;
            WriteStroke(edge.Attr);
            WriteAttribute("d", CurveString(iCurve));
            WriteEndElement();
            if (geometryEdge.EdgeGeometry != null && geometryEdge.EdgeGeometry.SourceArrowhead != null)
                AddArrow(iCurve.Start, geometryEdge.EdgeGeometry.SourceArrowhead.TipPosition, edge);
            if (geometryEdge.EdgeGeometry != null && geometryEdge.EdgeGeometry.TargetArrowhead != null)
                AddArrow(iCurve.End, geometryEdge.EdgeGeometry.TargetArrowhead.TipPosition, edge);
            if (edge.Label != null && edge.Label.GeometryLabel != null)
                WriteLabel(edge.Label);
        }

        protected void AddArrow(Point start, Point end, Edge edge)
        {
            var dir = end - start;
            var h = dir;
            dir /= dir.Length;

            var s = new Point(-dir.Y, dir.X);

            s *= h.Length * ((float)Math.Tan(ArrowAngle * 0.5f * (Math.PI / 180.0)));

            var points = new[] { start + s, end, start - s };
            DrawArrowPolygon(edge.Attr, points);
        }

        protected virtual void DrawPolygon(AttributeBase attr, Point[] points)
        {
            WriteStartElement("polygon");
            WriteStroke(attr);
            var edgeAttr = attr as EdgeAttr;
            if (edgeAttr != null)
            {
                WriteAttribute("fill", "none");
            }
            else
            {
                var nodeAttr = attr as NodeAttr;
                if (nodeAttr != null)
                    WriteFill(nodeAttr);
            }
            WriteAttribute("points", PointsToString(points));
            WriteEndElement();
        }

        protected virtual void DrawArrowPolygon(AttributeBase attr, Point[] points)
        {
            WriteStartElement("polygon");
            WriteStroke(attr);
            var edgeAttr = attr as EdgeAttr;
            if (edgeAttr != null)
            {
                WriteAttribute("fill", MsaglColorToSvgColor(attr.Color));
                WriteAttribute("fill-opacity", MsaglColorToSvgOpacity(attr.Color));
            }
            else
            {
                var nodeAttr = attr as NodeAttr;
                if (nodeAttr != null)
                    WriteFill(nodeAttr);
            }
            WriteAttribute("points", PointsToString(points));
            WriteEndElement();
        }

        protected void WriteNodes()
        {
            WriteComment("nodes");
            foreach (Node node in _graph.Nodes)
                WriteNode(node);
            WriteComment("end of nodes");

        }

        protected void WriteComment(string comment)
        {
            xmlWriter.WriteComment(comment);
        }

        protected void WriteNode(Node node)
        {
            if (node.IsVisible == false || node.GeometryNode == null)
                return;

            var attr = node.Attr;
            var hasUri = !String.IsNullOrEmpty(attr.Uri);
            if (hasUri && AllowedToWriteUri)
            {
                WriteStartElement("a");
                WriteAttributeWithPrefix("xlink", "href", attr.Uri);
            }

            WriteStyles(attr.Styles);
            switch (attr.Shape)
            {
                case Shape.DoubleCircle:
                    WriteDoubleCircle(node);
                    break;
                case Shape.Box:
                    WriteBox(node);
                    break;
                case Shape.Diamond:
                    WriteDiamond(node);
                    break;
                case Shape.Point:
                    WriteEllipse(node);
                    break;
                case Shape.Plaintext:
                    {
                        break;
                        //do nothing
                    }
                case Shape.Octagon:
                case Shape.House:
                case Shape.InvHouse:
                case Shape.Ellipse:
                case Shape.DrawFromGeometry:
                case Shape.Hexagon:
#if TEST_MSAGL
                case Shape.TestShape:
#endif
                    WriteFromMsaglCurve(node);
                    break;

                default:
                    WriteEllipse(node);
                    break;
            }

            WriteLabel(node.Label);
            if (hasUri && AllowedToWriteUri)
                WriteEndElement();
        }

        protected virtual void WriteFromMsaglCurve(Node node)
        {
            NodeAttr attr = node.Attr;

            var iCurve = node.GeometryNode.BoundaryCurve;
            var c = iCurve as Curve;
            if (c != null)
                WriteCurve(c, node);
            else
            {
                var ellipse = iCurve as Ellipse;
                if (ellipse != null)//a bug when the axis are rotated
                    WriteEllipseOnPosition(node.Attr, ellipse.Center, ellipse.AxisA.Length, ellipse.AxisB.Length);
                else
                {
                    var poly = iCurve as Polyline;
                    if (poly != null)
                        WriteCurve(CreateCurveFromPolyline(poly), node);
                    else
                        throw new NotImplementedException();
                }
            }
        }

        Curve CreateCurveFromPolyline(Polyline poly)
        {
            Curve c = new Curve();
            foreach (PolylinePoint p in poly.PolylinePoints)
            {
                if (p.Next != null)
                    Curve.AddLineSegment(c, p.Point, p.Next.Point);
            }
            if (poly.Closed)
                Curve.AddLineSegment(c, c.End, poly.Start);
            return c;
        }

        void WriteFillAndStroke(NodeAttr attr)
        {
            WriteFill(attr);
            WriteStroke(attr);
        }

        void WriteStroke(AttributeBase attr)
        {
            WriteAttribute("stroke", MsaglColorToSvgColor(attr.Color));
            WriteAttribute("stroke-opacity", MsaglColorToSvgOpacity(attr.Color));
            WriteAttribute("stroke-width", attr.LineWidth);
            if (attr.Styles.Any(style => style == Style.Dashed)) {
                WriteAttribute("stroke-dasharray", 5);
            } else if (attr.Styles.Any(style => style == Style.Dotted)) {
                WriteAttribute("stroke-dasharray", 2);
            }
        }

        void WriteCurve(Curve curve, Node node)
        {
            WriteStartElement("path");
            WriteFillAndStroke(node.Attr);
            WriteCurveGeometry(curve);
            WriteEndElement();
        }

        void WriteCurveGeometry(Curve curve)
        {
            WriteAttribute("d", CurveString(curve));
        }

        string CurveString(ICurve iCurve)
        {
            return String.Join(" ", CurveStringTokens(iCurve).ToArray());
        }

        IEnumerable<string> CurveStringTokens(ICurve iCurve)
        {
            yield return "M";
            yield return PointToString(iCurve.Start);
            var curve = iCurve as Curve;
            if (curve != null)
                foreach (var segment in curve.Segments)
                    yield return SegmentString(segment);
            else {
                var lineSeg = iCurve as LineSegment;
                if (lineSeg != null) {
                    yield return "L";
                    yield return PointToString(lineSeg.End);
                }
                else {
                    var cubic = iCurve as CubicBezierSegment;
                    if (cubic != null) {
                        yield return CubicBezierSegmentToString(cubic);
                    }
                    else {
                        var poly = iCurve as Polyline;
                        if (poly != null) {
                            foreach (var p in poly.Skip(1)) {
                                yield return "L";
                                yield return PointToString(p);
                            }
                            if (poly.Closed) {
                                yield return "L";
                                yield return PointToString(poly.Start);
                            }
                        }
                        else {
                            var roundedRect = iCurve as RoundedRect;
                            if (roundedRect != null) {
                                foreach (var segment in roundedRect.Curve.Segments) {
                                    yield return SegmentString(segment);
                                }
                            }
                            else {
                                var ellipse = iCurve as Ellipse;
                                if (ellipse != null) {
                                    if (IsFullEllipse(ellipse)) {

                                        yield return
                                            EllipseToString(new Ellipse(0, Math.PI, ellipse.AxisA, ellipse.AxisB,
                                                ellipse.Center));
                                        yield return
                                            EllipseToString(new Ellipse(Math.PI, Math.PI*2, ellipse.AxisA, ellipse.AxisB,
                                                ellipse.Center));
                                    }
                                    else yield return EllipseToString(ellipse);
                                }
                            }
                        }
                    }
                }
            }

        }

        static bool IsFullEllipse(Ellipse ell) {
            return ell.ParEnd == Math.PI * 2 && ell.ParStart == 0;
        }

        protected string SegmentString(ICurve segment)
        {
            var ls = segment as LineSegment;
            if (ls != null)
                return LineSegmentString(ls);

            var cubic = segment as CubicBezierSegment;
            if (cubic != null)
                return CubicBezierSegmentToString(cubic);

            var ell=segment as Ellipse;
            if (ell != null)
                return EllipseToString(ell);

            throw new NotImplementedException();
        }


        protected string EllipseToString(Ellipse ellipse) {
            string largeArc = Math.Abs(ellipse.ParEnd-ellipse.ParStart) >= Math.PI? "1":"0";
            string sweepFlag= ellipse.OrientedCounterclockwise()?"1":"0";

            return String.Join(" ", "A", EllipseRadiuses(ellipse), DoubleToString(Point.Angle(new Point(1, 0), ellipse.AxisA) / (Math.PI / 180.0)), largeArc, sweepFlag, PointsToString(ellipse.End));
        }

        protected string EllipseRadiuses(Ellipse ellipse) {
            return DoubleToString(ellipse.AxisA.Length) + "," + DoubleToString(ellipse.AxisB.Length);
        }


        protected string CubicBezierSegmentToString(CubicBezierSegment cubic)
        {
            return "C" + PointsToString(cubic.B(1), cubic.B(2), cubic.B(3));
        }


        protected string PointsToString(params Point[] points)
        {
            return String.Join(" ", points.Select(p => PointToString(p)).ToArray());
        }

        protected string LineSegmentString(LineSegment ls)
        {
            return "L " + PointToString(ls.End);
        }

        string formatForDoubleString = "#.###########";

        int precision = 11;
        bool allowedToWriteUri=true;

        ///<summary>
        ///</summary>
        public int Precision
        {
            get { return precision; }
            set
            {
                precision = Math.Max(1, value);
                var s = new char[precision + 2];
                s[0] = '#';
                s[1] = '.';
                for (int i = 0; i < precision; i++)
                    s[2 + i] = '#';
                formatForDoubleString = new string(s);
            }
        }

        ///<summary>
        ///</summary>
        public bool AllowedToWriteUri {
            get { return allowedToWriteUri; }
            set { allowedToWriteUri = value; }
        }

        protected string PointToString(Point start)
        {
            return DoubleToString(start.X) + " " + DoubleToString(start.Y);
        }

        protected string DoubleToString(double d)
        {
            return (Math.Abs(d) < 1e-11) ? "0" : d.ToString(formatForDoubleString, CultureInfo.InvariantCulture);
        }

        protected virtual void WriteDiamond(Node node)
        {
            NodeAttr nodeAttr = node.Attr;
            double w2 = node.GeometryNode.Width / 2.0f;
            double h2 = node.GeometryNode.Height / 2.0f;
            double cx = node.GeometryNode.Center.X;
            double cy = node.GeometryNode.Center.Y;
            var ps = new[]{
                              new Point(cx - w2,  cy),
                              new Point( cx,  cy + h2),
                              new Point(cx +  w2,  cy),
                              new Point( cx,  cy -  h2)
                          };
            DrawPolygon(node.Attr, ps);
        }

        protected virtual void WriteBox(Node node)
        {
            WriteStartElement("rect");
            WriteFill(node.Attr);
            WriteStroke(node.Attr);
            var curve = node.GeometryNode.BoundaryCurve;
            WriteAttribute("x", node.BoundingBox.Left);
            WriteAttribute("y", node.BoundingBox.Bottom);
            WriteAttribute("width", curve.BoundingBox.Width);
            WriteAttribute("height", curve.BoundingBox.Height);
            WriteAttribute("rx", node.Attr.XRadius);
            WriteAttribute("ry", node.Attr.YRadius);
            WriteEndElement();
        }

        protected virtual void WriteEllipse(Node node)
        {
            var geomNode = node.GeometryNode;
            var center = geomNode.Center;
            var rx = geomNode.Width / 2;
            var ry = geomNode.Height / 2;
            WriteEllipseOnPosition(node.Attr, center, rx, ry);
        }

        protected virtual void WriteDoubleCircle(Node node)
        {
            var geomNode = node.GeometryNode;
            var center = geomNode.Center;
            var rx = geomNode.Width / 2;
            var ry = geomNode.Height / 2;
            WriteEllipseOnPosition(node.Attr, center, rx * DoubleCircleOffsetRatio, ry * DoubleCircleOffsetRatio);
            WriteEllipseOnPosition(node.Attr, center, rx, ry);
        }


        protected virtual void WriteEllipseOnPosition(NodeAttr nodeAttr, Point center, double rx, double ry)
        {
            WriteStartElement("ellipse");
            WriteFill(nodeAttr);
            WriteStroke(nodeAttr);

            WriteFullEllipseGeometry(center, rx, ry);
            WriteEndElement();
        }

        protected void WriteFill(NodeAttr attr)
        {
            var color = attr.FillColor;
            if (color.A == 0 && !attr.Styles.Contains(Style.Filled))
            {
                WriteAttribute("fill", "none");
            }
            else
            {
                WriteAttribute("fill", MsaglColorToSvgColor(color));
                WriteAttribute("fill-opacity", MsaglColorToSvgOpacity(color));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        static double MsaglColorToSvgOpacity(Color color)
        {
            return color.A / 255.0;
        }

        protected void WriteFullEllipseGeometry(Point cx, double rx, double ry)
        {
            WriteAttribute("cx", cx.X);
            WriteAttribute("cy", cx.Y);
            WriteAttribute("rx", rx);
            WriteAttribute("ry", ry);
        }

        const double DoubleCircleOffsetRatio = 0.9;
        const double ArrowAngle = 25; //degrees

        static void WriteStyles(IEnumerable<Style> styles)
        {
        }

        protected void WriteEndElement()
        {
            xmlWriter.WriteEndElement();
        }

        protected void WriteStartElement(string s)
        {
            xmlWriter.WriteStartElement(s);
        }

        ///<summary>
        ///</summary>
        ///<param name="graph"></param>
        ///<param name="outputFile"></param>
        public static void WriteAllExceptEdges(Graph graph, string outputFile) {
            using (var stream = File.Create(outputFile)) {
                var writer = new SvgGraphWriter(stream, graph) {
                    Precision = 4,
                    IgnoreEdges = true
                };
                writer.Write();
            }
        }

        ///<summary>
        ///</summary>
        ///<param name="graph"></param>
        ///<param name="outputFile"></param>
        ///<param name="nodeSanitizer"></param>
        ///<param name="attrSanitizer"></param>
        ///<param name="precision"></param>
        public static void Write(Graph graph, string outputFile, Func<string, string> nodeSanitizer, Func<string, string> attrSanitizer, int precision)
        {
            using (var stream = File.Create(outputFile))
            {
                var writer = new SvgGraphWriter(stream, graph) {
                                                                   Precision = precision,
                                                                   NodeSanitizer = nodeSanitizer ?? (t => t),
                                                                   AttrSanitizer = attrSanitizer ?? (t => t)
                                                               };
                writer.Write();
            }
        }
        
        Set<String> colorSet=new Set<string>();
        void InitColorSet() {
            colorSet.Insert("AliceBlue".ToLower());
            colorSet.Insert("AntiqueWhite".ToLower());
            colorSet.Insert("Aqua".ToLower());
            colorSet.Insert("Aquamarine".ToLower());
            colorSet.Insert("Azure".ToLower());
            colorSet.Insert("Beige".ToLower());
            colorSet.Insert("Bisque".ToLower());
            colorSet.Insert("Black".ToLower());
            colorSet.Insert("BlanchedAlmond".ToLower());
            colorSet.Insert("Blue".ToLower());
            colorSet.Insert("BlueViolet".ToLower());
            colorSet.Insert("Brown".ToLower());
            colorSet.Insert("BurlyWood".ToLower());
            colorSet.Insert("CadetBlue".ToLower());
            colorSet.Insert("Chartreuse".ToLower());
            colorSet.Insert("Chocolate".ToLower());
            colorSet.Insert("Coral".ToLower());
            colorSet.Insert("CornflowerBlue".ToLower());
            colorSet.Insert("Cornsilk".ToLower());
            colorSet.Insert("Crimson".ToLower());
            colorSet.Insert("Cyan".ToLower());
            colorSet.Insert("DarkBlue".ToLower());
            colorSet.Insert("DarkCyan".ToLower());
            colorSet.Insert("DarkGoldenrod".ToLower());
            colorSet.Insert("DarkGray".ToLower());
            colorSet.Insert("DarkGreen".ToLower());
            colorSet.Insert("DarkKhaki".ToLower());
            colorSet.Insert("DarkMagenta".ToLower());
            colorSet.Insert("DarkOliveGreen".ToLower());
            colorSet.Insert("DarkOrange".ToLower());
            colorSet.Insert("DarkOrchid".ToLower());
            colorSet.Insert("DarkRed".ToLower());
            colorSet.Insert("DarkSalmon".ToLower());
            colorSet.Insert("DarkSeaGreen".ToLower());
            colorSet.Insert("DarkSlateBlue".ToLower());
            colorSet.Insert("DarkSlateGray".ToLower());
            colorSet.Insert("DarkTurquoise".ToLower());
            colorSet.Insert("DarkViolet".ToLower());
            colorSet.Insert("DeepPink".ToLower());
            colorSet.Insert("DeepSkyBlue".ToLower());
            colorSet.Insert("DimGray".ToLower());
            colorSet.Insert("DodgerBlue".ToLower());
            colorSet.Insert("Firebrick".ToLower());
            colorSet.Insert("FloralWhite".ToLower());
            colorSet.Insert("ForestGreen".ToLower());
            colorSet.Insert("Fuchsia".ToLower());
            colorSet.Insert("Gainsboro".ToLower());
            colorSet.Insert("GhostWhite".ToLower());
            colorSet.Insert("Gold".ToLower());
            colorSet.Insert("Goldenrod".ToLower());
            colorSet.Insert("Gray".ToLower());
            colorSet.Insert("Green".ToLower());
            colorSet.Insert("GreenYellow".ToLower());
            colorSet.Insert("Honeydew".ToLower());
            colorSet.Insert("HotPink".ToLower());
            colorSet.Insert("IndianRed".ToLower());
            colorSet.Insert("Indigo".ToLower());
            colorSet.Insert("Ivory".ToLower());
            colorSet.Insert("Khaki".ToLower());
            colorSet.Insert("Lavender".ToLower());
            colorSet.Insert("LavenderBlush".ToLower());
            colorSet.Insert("LawnGreen".ToLower());
            colorSet.Insert("LemonChiffon".ToLower());
            colorSet.Insert("LightBlue".ToLower());
            colorSet.Insert("LightCoral".ToLower());
            colorSet.Insert("LightCyan".ToLower());
            colorSet.Insert("LightGoldenrodYellow".ToLower());
            colorSet.Insert("LightGray".ToLower());
            colorSet.Insert("LightGreen".ToLower());
            colorSet.Insert("LightPink".ToLower());
            colorSet.Insert("LightSalmon".ToLower());
            colorSet.Insert("LightSeaGreen".ToLower());
            colorSet.Insert("LightSkyBlue".ToLower());
            colorSet.Insert("LightSlateGray".ToLower());
            colorSet.Insert("LightSteelBlue".ToLower());
            colorSet.Insert("LightYellow".ToLower());
            colorSet.Insert("Lime".ToLower());
            colorSet.Insert("LimeGreen".ToLower());
            colorSet.Insert("Linen".ToLower());
            colorSet.Insert("Magenta".ToLower());
            colorSet.Insert("Maroon".ToLower());
            colorSet.Insert("MediumAquamarine".ToLower());
            colorSet.Insert("MediumBlue".ToLower());
            colorSet.Insert("MediumOrchid".ToLower());
            colorSet.Insert("MediumPurple".ToLower());
            colorSet.Insert("MediumSeaGreen".ToLower());
            colorSet.Insert("MediumSlateBlue".ToLower());
            colorSet.Insert("MediumSpringGreen".ToLower());
            colorSet.Insert("MediumTurquoise".ToLower());
            colorSet.Insert("MediumVioletRed".ToLower());
            colorSet.Insert("MidnightBlue".ToLower());
            colorSet.Insert("MintCream".ToLower());
            colorSet.Insert("MistyRose".ToLower());
            colorSet.Insert("Moccasin".ToLower());
            colorSet.Insert("NavajoWhite".ToLower());
            colorSet.Insert("Navy".ToLower());
            colorSet.Insert("OldLace".ToLower());
            colorSet.Insert("Olive".ToLower());
            colorSet.Insert("OliveDrab".ToLower());
            colorSet.Insert("Orange".ToLower());
            colorSet.Insert("OrangeRed".ToLower());
            colorSet.Insert("Orchid".ToLower());
            colorSet.Insert("PaleGoldenrod".ToLower());
            colorSet.Insert("PaleGreen".ToLower());
            colorSet.Insert("PaleTurquoise".ToLower());
            colorSet.Insert("PaleVioletRed".ToLower());
            colorSet.Insert("PapayaWhip".ToLower());
            colorSet.Insert("PeachPuff".ToLower());
            colorSet.Insert("Peru".ToLower());
            colorSet.Insert("Pink".ToLower());
            colorSet.Insert("Plum".ToLower());
            colorSet.Insert("PowderBlue".ToLower());
            colorSet.Insert("Purple".ToLower());
            colorSet.Insert("Red".ToLower());
            colorSet.Insert("RosyBrown".ToLower());
            colorSet.Insert("RoyalBlue".ToLower());
            colorSet.Insert("SaddleBrown".ToLower());
            colorSet.Insert("Salmon".ToLower());
            colorSet.Insert("SandyBrown".ToLower());
            colorSet.Insert("SeaGreen".ToLower());
            colorSet.Insert("SeaShell".ToLower());
            colorSet.Insert("Sienna".ToLower());
            colorSet.Insert("Silver".ToLower());
            colorSet.Insert("SkyBlue".ToLower());
            colorSet.Insert("SlateBlue".ToLower());
            colorSet.Insert("SlateGray".ToLower());
            colorSet.Insert("Snow".ToLower());
            colorSet.Insert("SpringGreen".ToLower());
            colorSet.Insert("SteelBlue".ToLower());
            colorSet.Insert("Tan".ToLower());
            colorSet.Insert("Teal".ToLower());
            colorSet.Insert("Thistle".ToLower());
            colorSet.Insert("Tomato".ToLower());
            colorSet.Insert("Transparent".ToLower());
            colorSet.Insert("Turquoise".ToLower());
            colorSet.Insert("Violet".ToLower());
            colorSet.Insert("Wheat".ToLower());
            colorSet.Insert("White".ToLower());
            colorSet.Insert("WhiteSmoke".ToLower());
            colorSet.Insert("Yellow".ToLower());
            colorSet.Insert("YellowGreen".ToLower());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="outputFile"></param>
        public static void WriteAllExceptEdgesInBlack(Graph graph, string outputFile) {
            using (var stream = File.Create(outputFile)) {
                var writer = new SvgGraphWriter(stream, graph) {
                    Precision = 4,
                    IgnoreEdges = true,
                    BlackAndWhite =true
                };
                writer.Write();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void WriteOpening() {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Open();

        }
        /// <summary>
        /// flips the y-coordinate
        /// </summary>
        public void TransformGraphByFlippingY() {
            var matrix = new PlaneTransformation(1, 0, 0, 0, -1, 0);
            _graph.GeometryGraph.Transform(matrix);
        }
        /// <summary>
        /// writes a black line
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public void WriteLine(Point a, Point b) {
            WriteStartElement("line");
            WriteAttribute("x1", a.X);
            WriteAttribute("y1", a.Y);
            WriteAttribute("x2", b.X);
            WriteAttribute("y2", b.Y);
            WriteAttribute("style", "stroke:rgb(0,0,0);stroke-width:1");
            // < line x1 = "0" y1 = "0" x2 = "200" y2 = "200" style = "stroke:rgb(255,0,0);stroke-width:2" />
            WriteEndElement();
        }
    }
}