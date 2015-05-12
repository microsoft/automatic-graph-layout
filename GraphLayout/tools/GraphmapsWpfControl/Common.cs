using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Point = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.GraphmapsWpfControl {
    internal class Common {
        internal static System.Windows.Point WpfPoint(Point p) {
            return new System.Windows.Point(p.X,p.Y);
        }
        internal static Point MsaglPoint(System.Windows.Point p) {
            return new Point(p.X, p.Y);
        }


        static public Brush BrushFromMsaglColor(Microsoft.Msagl.Drawing.Color color) {
            Color avalonColor = new Color {A = color.A, B = color.B, G = color.G, R = color.R};
            return new SolidColorBrush(avalonColor);
        }

        static public Brush BrushFromMsaglColor(byte colorA, byte colorR, byte colorG, byte colorB)
        {
            Color avalonColor = new Color { A = colorA, R = colorR, G = colorG, B = colorB };
            return new SolidColorBrush(avalonColor);
        }




        internal static void PositionFrameworkElement(FrameworkElement frameworkElement, Point center, double scale) {
            PositionFrameworkElement(frameworkElement, center.X, center.Y, scale);
        }

        static void PositionFrameworkElement(FrameworkElement frameworkElement, double x, double y,  double scale) {
            if (frameworkElement == null)
                return;
            frameworkElement.RenderTransform =
                new MatrixTransform(new Matrix(scale, 0, 0, -scale, x - scale*frameworkElement.Width/2,
                    y + scale*frameworkElement.Height/2));
        }

        internal static void PositionFrameworkElement(FrameworkElement frameworkElement, System.Windows.Point center, double scale) {
            PositionFrameworkElement(frameworkElement, center.X, center.Y, scale);
        }
    }
}