using System;
using System.Diagnostics;

namespace Microsoft.Msagl.Layout.OverlapRemovalFixedSegments {
    internal class FakeBitmap {
        readonly int _width;
        readonly int _height;
        readonly byte[] _m;

        public FakeBitmap(int width, int height) {
            _width = width;
            _height = height;
            int size = _width*_height;
            _m = new byte[size];
        }


        public byte GetPixelValue(int x, int y) {
            if (!InBounds(x, y))
                return 0;

            return _m[x + y*_width];
        }

        int Offset(int x, int y) {
            Debug.Assert(InBounds(x, y));
            return x + _width*y;
        }

        bool InBounds(int x, int y) {
            return 0 <= x && x < _width &&
                   0 <= y && y < _height;
        }

        public void FillRectangle(int left, int bottom, int w, int h) {
            AdjustLeftTop(ref left, ref bottom);
            int top = Math.Min(bottom + h, _height - 1);
            int right = Math.Min(left + w, _width - 1);
            for (int y = bottom; y < top; y++) {
                int offset = Offset(left, y);
                for (int x = left; x < right; x++) {
                    _m[offset++] = 1;
                }
            }
        }

        void AdjustLeftTop(ref int left, ref int bottom) {
            if (left < 0)
                left = 0;
            if (bottom < 0)
                bottom = 0;
            if (left >= _width)
                left = _width - 1;
            if (bottom >= _height)
                bottom = _height - 1;

        }

        void Plot(int x, int y) {
            _m[Offset(x, y)] = 1;
        }

        internal void DrawFatSegByX(PixelPoint a, PixelPoint b, int insertRectHalfSize) {
            var perp = new PixelPoint(-(b.Y - a.Y), b.X - a.X);
            if (perp.Y == 0) {
                FillRectangle(a.X, a.Y - insertRectHalfSize, b.X - a.X, 2*insertRectHalfSize);
                return;
            }
            if (perp.Y < 0) perp = -perp;
            var pixelInside = a;
            double perpLen = Math.Sqrt(perp*perp);
            // pixel p is inside if and only if (p-a)*perp belongs to the interval [-halfInterval, halfInterval]
            int halfInterval = (int) (perpLen*insertRectHalfSize);
            for (int x = a.X; x <= b.X; x++)
                ScanVertLine(x, ref pixelInside, a, b, perp, halfInterval);
        }

        void ScanVertLine(int x, ref PixelPoint pixelInside, PixelPoint a, PixelPoint b, PixelPoint perp,
            int halfInterval) {
            ScanVertLineUp(x, pixelInside, a, perp, halfInterval);
            ScanVertLineDown(x, pixelInside, a, perp, halfInterval);
            UpdatePixelInsideForXCase(ref pixelInside, a, b, perp, halfInterval, Math.Sign(b.Y - a.Y));
        }

        void ScanVertLineDown(int x, PixelPoint pixelInside, PixelPoint a, PixelPoint perp, int halfInterval) {
            var y = pixelInside.Y;
            var proj = perp*(pixelInside - a);
            Debug.Assert(Math.Abs(proj) <= halfInterval);
            do {
                Plot(x, y);
                proj -= perp.Y;
                if (proj < - halfInterval) break;
                y--;
            } while (y >= 0);
        }

        void ScanVertLineUp(int x, PixelPoint pixelInside, PixelPoint a, PixelPoint perp, int halfInterval) {
            Debug.Assert(perp.Y > 0);
            var y = pixelInside.Y;
            var proj = perp*(pixelInside - a);
            Debug.Assert(Math.Abs(proj) <= halfInterval);
            do {
                Plot(x, y);
                proj += perp.Y;
                if (proj > halfInterval) break;
                y++;
            } while (y < _height);

        }

        static void UpdatePixelInsideForXCase(ref PixelPoint pixelInside, PixelPoint a, PixelPoint b, PixelPoint perp,
            int halfInterval, int sign) {
            if (pixelInside.X == b.X) return;
            pixelInside.X++;
            int proj = perp*(pixelInside - a);
            if (Math.Abs(proj) <= halfInterval) return;
            pixelInside.Y += sign;
            proj += sign*perp.Y;
            if (Math.Abs(proj) <= halfInterval) return;

            pixelInside.Y -= 2*sign;
            proj -= 2*sign*perp.Y;
            Debug.Assert(Math.Abs(proj - 2*sign*perp.Y) <= halfInterval);
        }

        internal void DrawFatSegByY(PixelPoint a, PixelPoint b, int halfDistInPixels) {
            Debug.Assert(a.Y <= b.Y);
            var perp = new PixelPoint(-b.Y + a.Y, b.X - a.X);
            if (perp.X == 0) {
                FillRectangle(a.X - halfDistInPixels, a.Y, 2*halfDistInPixels, b.Y - a.Y);
                return;
            }
            if (perp.X < 0) perp = -perp;
            var pixelInside = a;
            double perpLen = Math.Sqrt(perp*perp);
            // pixel p is inside if and only if (p-a)*perp belongs to the interval [-halfInterval, halfInterval]
            int halfInterval = (int) (perpLen*halfDistInPixels);
            for (int y = a.Y; y <= b.Y; y++)
                ScanHorizontalLine(y, ref pixelInside, a, b, perp, halfInterval);
        }

        void ScanHorizontalLine(int x, ref PixelPoint pixelInside, PixelPoint a, PixelPoint b, PixelPoint perp,
            int halfInterval) {
            ScanHorizontalLineRight(x, pixelInside, a, perp, halfInterval);
            ScanHorizontalLineLeft(x, pixelInside, a, perp, halfInterval);
            UpdatePixelInsideForYCase(ref pixelInside, a, b, perp, halfInterval, Math.Sign(b.Y - a.Y));

        }

        void UpdatePixelInsideForYCase(ref PixelPoint pixelInside, PixelPoint a, PixelPoint b, PixelPoint perp,
            int halfInterval, int sign) {
            if (pixelInside.Y == b.Y) return;
            pixelInside.Y++;
            int proj = perp*(pixelInside - a);
            if (Math.Abs(proj) <= halfInterval) return;
            pixelInside.X += sign;
            proj += sign*perp.X;
            if (Math.Abs(proj) <= halfInterval) return;

            pixelInside.X -= 2*sign;
            proj -= 2*sign*perp.X;
            Debug.Assert(Math.Abs(proj) <= halfInterval);
        }

        void ScanHorizontalLineRight(int y, PixelPoint pixelInside, PixelPoint a, PixelPoint perp, int halfInterval) {
            Debug.Assert(perp.X > 0);
            var x = pixelInside.X;
            var proj = perp*(pixelInside - a);
            Debug.Assert(Math.Abs(proj) <= halfInterval);
            do {
                Plot(x, y);
                proj += perp.X;
                if (proj > halfInterval) break;
                x++;
            } while (x < _width);
        }

        void ScanHorizontalLineLeft(int y, PixelPoint pixelInside, PixelPoint a, PixelPoint perp, int halfInterval) {
            Debug.Assert(perp.X > 0);
            var x = pixelInside.X;
            var proj = perp*(pixelInside - a);
            Debug.Assert(Math.Abs(proj) <= halfInterval);
            do {
                Plot(x, y);
                proj -= perp.X;
                if (proj < - halfInterval) break;
                x--;
            } while (x >= 0);

        }

        internal void DrawFatSeg(PixelPoint a, PixelPoint b, int insertRectHalfSizeInPixels) {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            FillRectangle(a.X - insertRectHalfSizeInPixels, a.Y - insertRectHalfSizeInPixels,
                2*insertRectHalfSizeInPixels,
                2*insertRectHalfSizeInPixels);
            FillRectangle(b.X - insertRectHalfSizeInPixels, b.Y - insertRectHalfSizeInPixels,
                2*insertRectHalfSizeInPixels,
                2*insertRectHalfSizeInPixels);
            if (Math.Abs(dx) > Math.Abs(dy)) {
                if (dx > 0)
                    DrawFatSegByX(a, b, insertRectHalfSizeInPixels);
                else
                    DrawFatSegByX(b, a, insertRectHalfSizeInPixels);
            }
            else {
                if (dy > 0)
                    DrawFatSegByY(a, b, insertRectHalfSizeInPixels);
                else
                    DrawFatSegByY(b, a, insertRectHalfSizeInPixels);
            }
        }
    }
}