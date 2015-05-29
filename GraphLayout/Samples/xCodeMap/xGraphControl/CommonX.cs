using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using Microsoft.Msagl.Core.Geometry;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;

namespace xCodeMap.xGraphControl {
    internal class CommonX {
        internal static System.Windows.Point WpfPoint(Microsoft.Msagl.Core.Geometry.Point p)
        {
            return new System.Windows.Point(p.X,p.Y);
        }

        static public Brush BrushFromMsaglColor(Microsoft.Msagl.Drawing.Color color) {

            Color avalonColor = new Color();
            avalonColor.A = color.A;
            avalonColor.B = color.B;
            avalonColor.G = color.G;
            avalonColor.R = color.R;
            return new SolidColorBrush(avalonColor);
        }

        internal static void PositionElement(FrameworkElement fe, Point origin, double scale)
        {
            fe.RenderTransform = new MatrixTransform(new Matrix(scale, 0, 0, -scale, origin.X, origin.Y));
            Panel.SetZIndex(fe, 1);
        }

        internal static void PositionElement(FrameworkElement fe, Rectangle object_box, Rectangle bounding_box, double scale)
        {
            Microsoft.Msagl.Core.Geometry.Point origin = bounding_box.Center;
            double desiredH = object_box.Height * scale - fe.Margin.Top - fe.Margin.Bottom,
                   desiredW = object_box.Width * scale + fe.Margin.Left + fe.Margin.Right;

            if (bounding_box.Height < desiredH)
            {
                origin.Y += object_box.Center.Y * scale - fe.Margin.Top;
            }
            else
            {
                if (fe.VerticalAlignment == VerticalAlignment.Top)
                {
                    origin.Y = bounding_box.Top;
                }
                else if (fe.VerticalAlignment == VerticalAlignment.Bottom)
                {
                    origin.Y -= bounding_box.Height / 2 - object_box.Height * scale;
                }
                else
                {
                    origin.Y += object_box.Center.Y * scale;
                }
            }
            if (bounding_box.Width < desiredW)
            {
                origin.X -= object_box.Center.X * scale;
            }
            else
            {
                if (fe.HorizontalAlignment == HorizontalAlignment.Left)
                {
                    origin.X -= bounding_box.Width / 2 - fe.Margin.Left;
                }
                else if (fe.HorizontalAlignment == HorizontalAlignment.Right)
                {
                    origin.X -= bounding_box.Width / 2 - object_box.Width * scale;
                }
                else
                {
                    origin.X -= object_box.Center.X * scale;
                }
            }
            PositionElement(fe, origin, scale);
        }

        internal static Size Measure(FrameworkElement fe)
        {
            fe.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            return fe.DesiredSize;
        }
    }
}