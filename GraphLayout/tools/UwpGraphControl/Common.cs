using System;
using SkiaSharp;

namespace Microsoft.Msagl.Viewers.Uwp {
    internal class Common {
        internal static Windows.Foundation.Point WpfPoint(Core.Geometry.Point p) {
            return new Windows.Foundation.Point(p.X, p.Y);
        }

        internal static Core.Geometry.Point MsaglPoint(Windows.Foundation.Point p) {
            return new Core.Geometry.Point(p.X, p.Y);
        }

        public static SKColor BrushFromMsaglColor(Drawing.Color color) =>
            new SKColor (color.R, color.G,  color.B, color.A);

        public static SKColor BrushFromMsaglColor(byte colorA, byte colorR, byte colorG, byte colorB) =>
            new SKColor(colorR, colorG,  colorB , colorA);
        internal static void PositionFrameworkElement(SKPaint frameworkElement, Core.Geometry.Point center, double scale) {
            PositionFrameworkElement(frameworkElement, center.X, center.Y, scale);
        }

        static void PositionFrameworkElement(SKPaint frameworkElement, double x, double y, double scale) {
            if (frameworkElement == null)
                return;
            frameworkElement.TextScaleX = (float)scale;
            throw new NotImplementedException();
            //new Windows.UI.Xaml.Media.Matrix(scale, 0, 0, -scale, x - scale * frameworkElement.Width / 2,
            //        y + scale * frameworkElement.Height / 2)
        }
    }

    public class SKTextBlock : SKFrameworkElement {
        public string Text { get; set; }
    }

    public class SKFrameworkElement : SKPaint {
        public VLabel Tag { get; set; }
        public SKCanvas Parent { get; set; }
    }
}