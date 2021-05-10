using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SvgLayerSample.Svg {
    public class Connector : SvgElement {
        public Edge Edge { get; }
        private XmlWriter writer;
        private readonly double ArrowAngle = 25;

        public Connector(Edge edge) {
            //
            this.Edge = edge;
        }


        public override void WriteTo(XmlWriter writer) {
            this.writer = writer;

            writer.WriteStartElement("path");
            writer.WriteAttribute("fill", "none");
            var geometryEdge = Edge.GeometryEdge;
            var iCurve = geometryEdge.Curve;
            WriteStroke(Edge.Attr);
            writer.WriteAttribute("d", CurveString(iCurve));
            writer.WriteEndElement();
            if (geometryEdge.EdgeGeometry != null && geometryEdge.EdgeGeometry.SourceArrowhead != null)
                AddArrow(iCurve.Start, geometryEdge.EdgeGeometry.SourceArrowhead.TipPosition, Edge);
            if (geometryEdge.EdgeGeometry != null && geometryEdge.EdgeGeometry.TargetArrowhead != null)
                AddArrow(iCurve.End, geometryEdge.EdgeGeometry.TargetArrowhead.TipPosition, Edge);
            if (Edge.Label != null && Edge.Label.GeometryLabel != null && Edge.GeometryEdge.Label != null)
                WriteConnectorLabel(Edge.Label);
        }

        private void WriteConnectorLabel(Microsoft.Msagl.Drawing.Label label) {
            writer.WriteStartElement("text");
            writer.WriteAttribute("x", label.GeometryLabel.Center.X);
            writer.WriteAttribute("y", label.GeometryLabel.Center.Y);
            writer.WriteString(label.Text);
            writer.WriteEndElement();
        }

        protected void AddArrow(Point start, Point end, Edge edge) {
            var dir = end - start;
            var h = dir;
            dir /= dir.Length;

            var s = new Point(-dir.Y, dir.X);
            s *= h.Length * ((float)Math.Tan(ArrowAngle * 0.5f * (Math.PI / 180.0)));
            var points = new[] { start + s, end, start - s };
            DrawArrowPolygon(edge.Attr, points);
        }

        protected void WriteFill(NodeAttr attr) {
            var color = attr.FillColor;
            if (color.A == 0 && !attr.Styles.Contains(Style.Filled)) {
                writer.WriteAttribute("fill", "none");
            }
            else {
                writer.WriteAttribute("fill", color);
                writer.WriteAttribute("fill-opacity", color);
            }
        }

        void WriteStroke(AttributeBase attr) {
            writer.WriteAttribute("stroke", attr.Color);
            writer.WriteAttribute("stroke-opacity", attr.Color);
            writer.WriteAttribute("stroke-width", attr.LineWidth);
            if (attr.Styles.Any(style => style == Style.Dashed)) {
                writer.WriteAttribute("stroke-dasharray", 5);
            }
            else if (attr.Styles.Any(style => style == Style.Dotted)) {
                writer.WriteAttribute("stroke-dasharray", 2);
            }
        }

        void WriteFillAndStroke(NodeAttr attr) {
            WriteFill(attr);
            WriteStroke(attr);
        }

        void WriteCurve(Curve curve, Node node) {
            writer.WriteStartElement("path");
            WriteFillAndStroke(node.Attr);
            WriteCurveGeometry(curve);
            writer.WriteEndElement();
        }

        void WriteCurveGeometry(Curve curve) {
            writer.WriteAttribute("d", CurveString(curve));
        }

        string CurveString(ICurve iCurve) {
            return String.Join(" ", CurveStringTokens(iCurve).ToArray());
        }

        IEnumerable<string> CurveStringTokens(ICurve iCurve) {
            yield return "M";
            yield return Utils.PointToString(iCurve.Start);
            var curve = iCurve as Curve;
            if (curve != null)
                foreach (var segment in curve.Segments)
                    yield return SegmentString(segment);
            else {
                var lineSeg = iCurve as LineSegment;
                if (lineSeg != null) {
                    yield return "L";
                    yield return Utils.PointToString(lineSeg.End);
                }
                else {
                    var cubic = iCurve as CubicBezierSegment;
                    if (cubic != null) {
                        yield return Utils.CubicBezierSegmentToString(cubic);
                    }
                    else {
                        var poly = iCurve as Polyline;
                        if (poly != null) {
                            foreach (var p in poly.Skip(1)) {
                                yield return "L";
                                yield return Utils.PointToString(p);
                            }
                            if (poly.Closed) {
                                yield return "L";
                                yield return Utils.PointToString(poly.Start);
                            }
                        }
                        else {
                            var roundedRect = iCurve as RoundedRect;
                            if (roundedRect != null) {
                                foreach (var segment in roundedRect.Curve.Segments) {
                                    yield return SegmentString(segment);
                                }
                            }
                        }
                    }
                }
            }

        }

        protected string SegmentString(ICurve segment) {
            var ls = segment as LineSegment;
            if (ls != null)
                return Utils.LineSegmentString(ls);

            var cubic = segment as CubicBezierSegment;
            if (cubic != null)
                return Utils.CubicBezierSegmentToString(cubic);

            var ell = segment as Ellipse;
            if (ell != null)
                return Utils.EllipseToString(ell);


            throw new NotImplementedException();
        }

        Curve CreateCurveFromPolyline(Polyline poly) {
            Curve c = new Curve();
            foreach (PolylinePoint p in poly.PolylinePoints) {
                if (p.Next != null)
                    Curve.AddLineSegment(c, p.Point, p.Next.Point);
            }
            if (poly.Closed)
                Curve.AddLineSegment(c, c.End, poly.Start);
            return c;
        }

        protected virtual void DrawArrowPolygon(AttributeBase attr, Point[] points) {
            writer.WriteStartElement("polygon");
            WriteStroke(attr);
            var edgeAttr = attr as EdgeAttr;
            if (edgeAttr != null) {
                writer.WriteAttribute("fill", attr.Color);
                writer.WriteAttribute("fill-opacity", attr.Color);
            }
            else {
                var nodeAttr = attr as NodeAttr;
                if (nodeAttr != null)
                    WriteFill(nodeAttr);
            }
            writer.WriteAttribute("points", Utils.PointsToString(points));
            writer.WriteEndElement();
        }

       

       

        

    }
}
