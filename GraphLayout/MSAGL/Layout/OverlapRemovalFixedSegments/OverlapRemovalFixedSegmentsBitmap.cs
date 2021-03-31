using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Point = Microsoft.Msagl.Core.Geometry.Point;

using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Microsoft.Msagl.Core.Geometry.Curves;
using SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;


namespace Microsoft.Msagl.Layout.OverlapRemovalFixedSegments {
    public class OverlapRemovalFixedSegmentsBitmap {
        Rectangle[] _moveableRectangles;
        Rectangle[] _fixedRectangles;
        SymmetricSegment[] _fixedSegments;

        Rectangle[] movedRectangles;

        FakeBitmap _bitmap;

        const int MaxSize = 1024*2; //512
        int _mapWidth, _mapHeight;


        double _pixelSize;

        Rectangle _bbox;

        PlaneTransformation _worldToMap;
        PlaneTransformation _mapToWorld;

        double _insertRectSize;
        
        internal int NumPixelsPadding = 5;
        
        public void InitInsertRectSize(Rectangle box) {
            _insertRectSize = box.Width;
            _insertRectSize += NumPixelsPadding*_pixelSize;
        }

        public OverlapRemovalFixedSegmentsBitmap(Rectangle[] moveableRectangles, Rectangle[] fixedRectangles,
            SymmetricSegment[] fixedSegments) {
            _moveableRectangles = moveableRectangles;
            _fixedRectangles = fixedRectangles;
            _fixedSegments = fixedSegments;

            _bbox = GetInitialBoundingBox();
            _bbox.ScaleAroundCenter(1.25);

            InitBitmap();
            InitTransform();
            
            movedRectangles = new Rectangle[moveableRectangles.Length];
        }

        public void ScaleBbox(double scale) {
            _bbox.ScaleAroundCenter(1.25);
            InitBitmap();
            InitTransform();
        }

        public OverlapRemovalFixedSegmentsBitmap(Rectangle bbox) {
            this._bbox = bbox;
            InitBitmap();
            InitTransform();
        }

        public Rectangle GetInitialBoundingBox() {
            Rectangle bbox = new Rectangle();
            bbox.SetToEmpty();
            foreach (var rect in _fixedRectangles) {
                bbox.Add(rect);
            }
            foreach (var rect in _moveableRectangles) {
                bbox.Add(rect);
            }
            return bbox;
        }

        public void InitBitmap() {
            var maxDim = Math.Max(_bbox.Width, _bbox.Height);
            _mapWidth = (int) (_bbox.Width/maxDim*MaxSize);
            _mapHeight = (int) (_bbox.Height/maxDim*MaxSize);

            _pixelSize = _bbox.Width/_mapWidth;            
            _bitmap = new FakeBitmap(_mapWidth, _mapHeight);
        }

        internal bool FindClosestFreePixel(Point p, out PixelPoint found) {
            var pPixel = PointToPixel(p);

            for (int r = 0; r < MaxSize/2; r++) {
                if (!GetFreePixelInRadius(ref pPixel, r)) continue;
                found = pPixel;
                return true;
            }

            found = new PixelPoint();
            return false;
        }

        public bool IsPositionFree(Point p) {
            return IsFree(PointToPixel(p));
        }

        public int PositionAllMoveableRectsSameSize(int startInd, RTree<Rectangle, Point> fixedRectanglesTree,
            RTree<SymmetricSegment, Point> fixedSegmentsTree) {
            int i;
            if (_moveableRectangles.Length == 0) return 0;

            InitInsertRectSize(_moveableRectangles[startInd]);

            DrawFixedRectsSegments(fixedRectanglesTree, fixedSegmentsTree);

            for (i = startInd; i < _moveableRectangles.Length; i++) {
                var rect = _moveableRectangles[i];

                bool couldInsert;

                if (IsPositionFree(rect.Center)) {
                    movedRectangles[i] = rect;
                    couldInsert = true;
                }
                else {
                    Point newPos;
                    couldInsert = FindClosestFreePos(rect.Center, out newPos);
                    movedRectangles[i] = Translate(rect, newPos - rect.Center);
                }

                if (!couldInsert) {
                    return i;
                }

                fixedRectanglesTree.Add(movedRectangles[i], movedRectangles[i]);
                DrawRectDilated(movedRectangles[i]);
            }
            return _moveableRectangles.Length;
        }


        void DrawFixedRectsSegments(RTree<Rectangle, Point> fixedRectanglesTree,
            RTree<SymmetricSegment, Point> fixedSegmentsTree) {
            foreach (var fr in fixedRectanglesTree.GetAllLeaves())
                DrawRectDilated(fr);
            foreach (var seg in fixedSegmentsTree.GetAllLeaves()) {
                DrawLineSegDilated(seg.A, seg.B);
            }
        }
        
        public bool FindClosestFreePos(Point p, out Point newPos) {
            PixelPoint iPos;
            bool found = FindClosestFreePixel(p, out iPos);
            newPos = GetWorldCoord(iPos);
            return found;
        }

        public Point[] GetTranslations() {
            Point[] translation = new Point[_moveableRectangles.Length];

            for (int i = 0; i < _moveableRectangles.Length; i++)
                translation[i] = movedRectangles[i].Center - _moveableRectangles[i].Center;
            return translation;
        }

        static Rectangle Translate(Rectangle rect, Point delta) {
            return new Rectangle(rect.LeftBottom + delta, rect.RightTop + delta);
        }

        bool GetFreePixelInRadius(ref PixelPoint source, int rad) {
            for (int x = source.X - rad; x <= source.X + rad; x++) {
                PixelPoint p1 = new PixelPoint(x, source.Y - rad);
                if (IsFree(p1)) {
                    source = p1;
                    return true;
                }
                PixelPoint p2 = new PixelPoint(x, source.Y + rad);
                if (rad != 0 && IsFree(p2)) {
                    source = p2;
                    return true;
                }
            }
            for (int y = source.Y - rad + 1; y < source.Y + rad; y++) {
                PixelPoint p1 = new PixelPoint(source.X - rad, y);
                if (IsFree(p1)) {
                    source = p1;
                    return true;
                }
                PixelPoint p2 = new PixelPoint(source.X + rad, y);
                if (IsFree(p2)) {
                    source = p2;
                    return true;
                }
            }
            return false;
        }

        bool IsFree(PixelPoint p) {
            return _bitmap.GetPixelValue(p.X, p.Y) == 0;
        }


        void DrawRectDilated(Rectangle rect) {
            Point d = new Point(_insertRectSize/2, _insertRectSize/2);
            var leftBottom = _worldToMap*(rect.LeftBottom - d);
            var rightTop = _worldToMap*(rect.RightTop + d);
            int ix1 = (int) leftBottom.X;
            int iy1 = (int) leftBottom.Y;
            int iw = (int) (rightTop.X - leftBottom.X);
            int ih = (int) (rightTop.Y - leftBottom.Y);
            _bitmap.FillRectangle(ix1, iy1, iw, ih);
        }



        void DrawLineSegDilated(Point p1, Point p2) {
            var a = PointToPixel(p1);
            var b = PointToPixel(p2);
            _bitmap.DrawFatSeg(a, b, (int) (_insertRectSize/(2*_pixelSize)+0.5));
        }


        internal PixelPoint PointToPixel(Point p) {
            var l = _worldToMap*p;
            return new PixelPoint((int) (l.X + 0.5), (int) (l.Y + 0.5));
        }

        internal Point GetWorldCoord(PixelPoint p) {
            return _mapToWorld*new Point(p.X, p.Y);
        }

        public void InitTransform() {
            double mapCenterX = 0.5*_mapWidth;
            double mapCenterY = 0.5*_mapHeight;
            double worldCenterX = _bbox.Center.X;
            double worldCenterY = _bbox.Center.Y;

            double scaleX = _mapWidth/_bbox.Width;
            double scaleY = _mapHeight/_bbox.Height;
            _worldToMap = new PlaneTransformation(scaleX, 0, mapCenterX - scaleX*worldCenterX,
                0, scaleY, mapCenterY - scaleY*worldCenterY);

            _mapToWorld = _worldToMap.Inverse;
        }

    }
}
