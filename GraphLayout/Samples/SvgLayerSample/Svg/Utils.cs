using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace SvgLayerSample.Svg {
    public static class Utils {
        public static void WriteAttribute(this XmlWriter xmlWriter, string attrName, object attrValue) {
            if (attrValue is double)
                attrValue = DoubleToString((double)attrValue);
            else if (attrValue is Point)
                attrValue = PointToString((Point)attrValue);
            else if (attrValue is Color color)
                attrValue = MsaglColorToSvgColor(color);
            xmlWriter.WriteAttributeString(attrName, attrValue.ToString());
        }

        readonly static string formatForDoubleString = "#.###########";

        internal static string DoubleToString(double d) {
            return (Math.Abs(d) < 1e-11) ? "0" : d.ToString(formatForDoubleString, CultureInfo.InvariantCulture);
        }

        internal static  string PointToString(Point start) {
            return DoubleToString(start.X) + " " + DoubleToString(start.Y);
        }

        internal static string PointsToString(params Point[] points) {
            return String.Join(" ", points.Select(p => PointToString(p)).ToArray());
        }

        private static string MsaglColorToSvgColor(Color color) {
            return "#" + Color.Xex(color.R) + Color.Xex(color.G) + Color.Xex(color.B);
        }

        internal static string CubicBezierSegmentToString(CubicBezierSegment cubic) {
            return "C" + PointsToString(cubic.B(1), cubic.B(2), cubic.B(3));
        }

        internal static string LineSegmentString(LineSegment ls) {
            return "L " + PointToString(ls.End);
        }

        internal static string EllipseToString(Ellipse ellipse) {
            string largeArc = Math.Abs(ellipse.ParEnd - ellipse.ParStart) >= Math.PI ? "1" : "0";
            string sweepFlag = ellipse.OrientedCounterclockwise() ? "1" : "0";

            return String.Join(" ", "A", EllipseRadiuses(ellipse), DoubleToString(Point.Angle(new Point(1, 0), ellipse.AxisA) / (Math.PI / 180.0)), largeArc, sweepFlag, PointsToString(ellipse.End));
        }

        internal static string EllipseRadiuses(Ellipse ellipse) {
            return DoubleToString(ellipse.AxisA.Length) + "," + DoubleToString(ellipse.AxisB.Length);
        }

        public static Color backgroundColor = new Color(67, 141, 213);
        public static Color borderColor = new Color(60, 127, 192);
        public static Color fontColor = new Color(255, 255, 255);

        public static string WordWrap(string text, int maxLineLength, string wrapString) {
            var list = new List<string>();

            int currentIndex;
            var lastWrap = 0;
            var whitespace = new[] { ' ', '\r', '\n', '\t' };
            do {
                currentIndex = lastWrap + maxLineLength > text.Length ? text.Length : (text.LastIndexOfAny(new[] { ' ', ',', '.', '?', '!', ':', ';', '-', '\n', '\r', '\t' }, Math.Min(text.Length - 1, lastWrap + maxLineLength)) + 1);
                if (currentIndex <= lastWrap)
                    currentIndex = Math.Min(lastWrap + maxLineLength, text.Length);
                list.Add(text.Substring(lastWrap, currentIndex - lastWrap).Trim(whitespace));
                lastWrap = currentIndex;
            } while (currentIndex < text.Length);

            return string.Join(wrapString, list);
        }
        public static string WordWrap(string text, int maxLineLength) {
            return WordWrap(text, maxLineLength, Environment.NewLine);

        }
    }
}
