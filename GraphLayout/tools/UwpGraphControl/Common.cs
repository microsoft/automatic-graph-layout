namespace Microsoft.Msagl.Viewers.Uwp {
    internal class Common {
        internal static Windows.Foundation.Point WpfPoint(Core.Geometry.Point p) {
            return new Windows.Foundation.Point(p.X, p.Y);
        }

        internal static Core.Geometry.Point MsaglPoint(Windows.Foundation.Point p) {
            return new Core.Geometry.Point(p.X, p.Y);
        }

        public static Windows.UI.Xaml.Media.Brush BrushFromMsaglColor(Microsoft.Msagl.Drawing.Color color) {
            var avalonColor = new Windows.UI.Color { A = color.A, B = color.B, G = color.G, R = color.R };
            return new Windows.UI.Xaml.Media.SolidColorBrush(avalonColor);
        }

        public static Windows.UI.Xaml.Media.Brush BrushFromMsaglColor(byte colorA, byte colorR, byte colorG, byte colorB) {
            var avalonColor = new Windows.UI.Color { A = colorA, R = colorR, G = colorG, B = colorB };
            return new Windows.UI.Xaml.Media.SolidColorBrush(avalonColor);
        }
        internal static void PositionFrameworkElement(Windows.UI.Xaml.FrameworkElement frameworkElement, Core.Geometry.Point center, double scale) {
            PositionFrameworkElement(frameworkElement, center.X, center.Y, scale);
        }

        static void PositionFrameworkElement(Windows.UI.Xaml.FrameworkElement frameworkElement, double x, double y, double scale) {
            if (frameworkElement == null)
                return;
            frameworkElement.RenderTransform =
                new Windows.UI.Xaml.Media.MatrixTransform {
                    Matrix = new Windows.UI.Xaml.Media.Matrix(scale, 0, 0, -scale, x - scale * frameworkElement.Width / 2,
                    y + scale * frameworkElement.Height / 2)
                };
        }
    }
}