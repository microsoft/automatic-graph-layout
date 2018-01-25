using System;

internal struct PixelPoint {

    internal int X;
    internal int Y;
    
    public static PixelPoint operator /(PixelPoint point, double coefficient) {
        return new PixelPoint((int)(point.X / coefficient), (int)(point.Y / coefficient));
    }
    internal PixelPoint(int x, int y) {
        X = x;
        Y = y;
    }

    public static PixelPoint operator -(PixelPoint a, PixelPoint b) {
        return new PixelPoint(a.X - b.X, a.Y - b.Y);
    }
   
    public static PixelPoint operator +(PixelPoint pixel0, PixelPoint pixel1) {
        return new PixelPoint(pixel0.X + pixel1.X, pixel0.Y + pixel1.Y);
    }
    public static PixelPoint operator -(PixelPoint pixel0) {
        return new PixelPoint(-pixel0.X, -pixel0.Y);
    }
    public static int operator *(PixelPoint point0, PixelPoint point1) {
        return point0.X * point1.X + point0.Y * point1.Y;
    }

    public override string ToString() {
        return String.Format("({0},{1})", X, Y);
    }
}