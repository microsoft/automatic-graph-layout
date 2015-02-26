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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Point = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.WpfGraphControl {
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