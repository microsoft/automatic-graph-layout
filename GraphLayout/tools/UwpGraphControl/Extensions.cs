using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Microsoft.Msagl.Viewers.Uwp {
    public static class Extensions {
        public static void BeginFigure(this GeometryGroup group, Point p, bool b1, bool b2) {
        }
        public static void BezierTo(this GeometryGroup group, Point p1, Point p2, Point p3, bool b1, bool b2) {
            var bg = new BezierSegment { Point1 = p1, Point2 = p2, Point3 = p3 };

        //    group.Children.Add(bg);
        }
        public static void LineTo(this GeometryGroup group, Point p, bool b1, bool b2) {
            var l = new LineGeometry();
            if (group.Children.Last() is LineGeometry lAnt)
                l.StartPoint = lAnt.EndPoint;
            l.EndPoint = p;
            group.Children.Add(l);
        }
        public static void ArcTo(this GeometryGroup group, Point p, Size s, double angle, bool largeArc, SweepDirection direction, bool b1, bool b2) {
            var arc = new PathGeometry();
            group.Children.Add(arc);
        }
    }
}