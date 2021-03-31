using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging {
    internal class StaircaseRemover {
        protected List<Path> Paths { get; set; }

        protected RTree<Polyline,Point> HierarchyOfObstacles { get; set; }
        readonly RTree<SegWithIndex, Point> segTree=new RTree<SegWithIndex, Point>();
        Set<Path> crossedOutPaths = new Set<Path>();

        StaircaseRemover(List<Path> paths, RectangleNode<Polyline, Point> hierarchyOfObstacles) {
            HierarchyOfObstacles = new RTree<Polyline, Point>(hierarchyOfObstacles);
            Paths = paths;
        }


        internal static void RemoveStaircases(List<Path> paths, RectangleNode<Polyline, Point> hierarchyOfObstacles) {
            var r = new StaircaseRemover(paths, hierarchyOfObstacles);
            r.Calculate();
        }

        void Calculate() {
            InitHierarchies();
            bool success;
            do {
                success = false;
                foreach (var path in Paths.Where(p=>!crossedOutPaths.Contains(p)))
                    success |= ProcessPath(path);
            } while (success);
        }

        bool ProcessPath(Path path) {
            var pts = (Point[])path.PathPoints;
            bool canHaveStaircase;
            if (ProcessPoints(ref pts, out canHaveStaircase)) {
                path.PathPoints = pts;
                return true;
            }
            if (!canHaveStaircase)
                crossedOutPaths.Insert(path);
            return false;
        }

        bool ProcessPoints(ref Point[] pts, out bool canHaveStaircase) {
            var staircaseStart  = FindStaircaseStart(pts, out canHaveStaircase);
            if (staircaseStart < 0) return false;
            pts = RemoveStaircase(pts, staircaseStart);
            return true;
        }


        int FindStaircaseStart(Point[] pts, out bool canHaveStaircase) {
            canHaveStaircase = false;
            if (pts.Length < 5) // At least five points make a staircase
                return -1;
            var segs = new[] {
                                 new SegWithIndex(pts, 0), new SegWithIndex(pts, 1), new SegWithIndex(pts, 2),
                                 new SegWithIndex(pts, 3)
                             };
            int segToReplace = 0;

            for (int i = 0;;) {
                bool canHaveStaircaseAtI;
                if (IsStaircase(pts, i, segs, out canHaveStaircaseAtI)) {
                    canHaveStaircase = true;
                    return i;                    
                }
                canHaveStaircase = canHaveStaircase || canHaveStaircaseAtI;
                i++;
                if (pts.Length < i + 5)// At least five points make a staircase
                    return -1;

                segs[segToReplace] = new SegWithIndex(pts, i + 3);
                segToReplace += 1;
                segToReplace %= 4;
            }
        }

        static Point GetFlippedPoint(Point[] pts, int offset) {
            var horiz = ApproximateComparer.Close(pts[offset].Y, pts[offset + 1].Y);
            return horiz ? new Point(pts[offset + 4].X, pts[offset].Y) : new Point(pts[offset].X, pts[offset + 4].Y);
        }

        /// <summary>
        /// ignoring crossing at a
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="segsToIgnore"></param>
        /// <returns></returns>
        bool Crossing(Point a, Point b, SegWithIndex[] segsToIgnore) {
            return IsCrossing(new LineSegment(a, b), segTree, segsToIgnore);
        }

        /// <summary>
        /// ignoring crossing at ls.Start
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="rTree"></param>
        /// <param name="segsToIgnore"></param>
        /// <returns></returns>
        static bool IsCrossing(LineSegment ls, RTree<SegWithIndex,Point> rTree, SegWithIndex[] segsToIgnore) {
            return rTree.GetAllIntersecting(ls.BoundingBox).Where(seg => !segsToIgnore.Contains(seg)).Any();
        }

        
        bool IntersectObstacleHierarchy(Point a, Point b, Point c) {
            return IntersectObstacleHierarchy(new LineSegment(a, b)) ||
                   IntersectObstacleHierarchy(new LineSegment(b, c));
        }

        bool IntersectObstacleHierarchy(LineSegment ls) {
            return
                HierarchyOfObstacles.GetAllIntersecting(ls.BoundingBox).Any(
                    poly => Curve.CurveCurveIntersectionOne(ls, poly, false) != null);
        }

        bool IsStaircase(Point[] pts, int offset, SegWithIndex[] segsToIgnore, out bool canHaveStaircaseAtI) {
            var a = pts[offset];
            var b = pts[offset + 1];
            var c = pts[offset + 2];
            var d = pts[offset + 3];
            var f = pts[offset + 4];
            canHaveStaircaseAtI = false;
            if (CompassVector.DirectionsFromPointToPoint(a, b) != CompassVector.DirectionsFromPointToPoint(c, d) ||
                CompassVector.DirectionsFromPointToPoint(b, c) != CompassVector.DirectionsFromPointToPoint(d, f))
                return false;

            c = GetFlippedPoint(pts, offset);
            if (IntersectObstacleHierarchy(b, c, d))
                return false;
            canHaveStaircaseAtI = true;
            return !Crossing(b, c, segsToIgnore);
        }

        Point[] RemoveStaircase(Point[] pts, int staircaseStart) {
            Point a = pts[staircaseStart];
            Point b = pts[staircaseStart + 1];
            var horiz = Math.Abs(a.Y - b.Y) < ApproximateComparer.DistanceEpsilon/2;
            return RemoveStaircase(pts, staircaseStart, horiz);

        }

        Point[] RemoveStaircase(Point[] pts, int staircaseStart, bool horiz) {
            RemoveSegs(pts);
            var ret = new Point[pts.Length - 2];
            Array.Copy(pts, ret, staircaseStart + 1);
            var a = pts[staircaseStart + 1];
            var c = pts[staircaseStart + 3];
            ret[staircaseStart + 1] = horiz ? new Point(c.X, a.Y) : new Point(a.X, c.Y);
            Array.Copy(pts, staircaseStart + 4, ret, staircaseStart + 2, ret.Length - staircaseStart - 2);
            InsertNewSegs(ret, staircaseStart);
            return ret;
        }

        void RemoveSegs(Point[] pts) {
            for (int i = 0; i < pts.Length-1; i++)
                RemoveSeg(new SegWithIndex(pts,i));
        }

        void RemoveSeg(SegWithIndex seg) {
            segTree.Remove(Rect(seg), seg);
        }
        
       
        void InsertNewSegs(Point[] pts, int staircaseStart) {
            InsSeg(pts, staircaseStart);
            InsSeg(pts, staircaseStart+1);
        }

        void InitHierarchies() {
            foreach (var path in Paths)
                InsertPathSegs(path);
        }
       
        void InsertPathSegs(Path path) {
            InsertSegs((Point[])path.PathPoints);
        }

   
        void InsertSegs(Point[] pts) {
            for (int i = 0; i < pts.Length - 1; i++)
                InsSeg(pts, i);
        }

        void InsSeg(Point[] pts, int i) {
            var seg = new SegWithIndex(pts, i);
            segTree.Add(Rect(seg), seg);
        }

        static Rectangle Rect(SegWithIndex seg) {
            return new Rectangle(seg.Start,seg.End);
        }
    }
}