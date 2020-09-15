using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;
using Microsoft.Msagl.Core;

namespace Microsoft.Msagl.Routing.Visibility {
    internal class LineSweeperBase : IComparer<SweepEvent> {
        Point directionPerp; // sweep direction rotated 90 degrees clockwse
        BinaryHeapWithComparer<SweepEvent> eventQueue;

        protected RbTree<SegmentBase> LeftObstacleSideTree { get; set; }

        protected ObstacleSideComparer ObstacleSideComparer { get; set; }

        protected RbTree<SegmentBase> RightObstacleSideTree { get; set; }
        protected Set<Point> Ports;
        public LineSweeperBase(IEnumerable<Polyline> obstacles, Point sweepDirection) {
            Obstacles = obstacles;
            SweepDirection = sweepDirection;
            DirectionPerp = sweepDirection.Rotate(-Math.PI / 2);
            EventQueue = new BinaryHeapWithComparer<SweepEvent>(this);
            ObstacleSideComparer = new ObstacleSideComparer(this);
            LeftObstacleSideTree = new RbTree<SegmentBase>(ObstacleSideComparer);
            RightObstacleSideTree = new RbTree<SegmentBase>(ObstacleSideComparer);
        }

        protected internal BinaryHeapWithComparer<SweepEvent> EventQueue {
            get { return eventQueue; }
            set { eventQueue = value; }
        }
        public Point SweepDirection { get; set; }
        /// <summary>
        /// sweep direction rotated by 90 degrees clockwise
        /// </summary>
        protected Point DirectionPerp {
            get { return directionPerp; }
            set { directionPerp = value; }
        }

        protected double PreviousZ=double.NegativeInfinity;
        double z;
        public double Z {
            get { return z; }
            set {
                if (value > z + ApproximateComparer.Tolerance)
                    PreviousZ = z;
#if TEST_MSAGL
                Debug.Assert(PreviousZ<=value); 
#endif
                z = value;
         //       Debug.Assert(TreesAreCorrect());
            }
        }

       // protected virtual bool TreesAreCorrect() { return true; }

        protected internal IEnumerable<Polyline> Obstacles { get; set; }


        protected double GetZ(SweepEvent eve) {
            return SweepDirection * eve.Site;
        }

        protected double GetZ(Point point) {
            return SweepDirection * point;
        }

        protected bool SegmentIsNotHorizontal(Point a, Point b) {
            return Math.Abs((a - b) * SweepDirection) > ApproximateComparer.DistanceEpsilon;
        }

        protected void RemoveLeftSide(LeftObstacleSide side) {
            ObstacleSideComparer.SetOperand(side);
            LeftObstacleSideTree.Remove(side);
        }

        protected void RemoveRightSide(RightObstacleSide side) {
            ObstacleSideComparer.SetOperand(side);
            RightObstacleSideTree.Remove(side);
        }

        protected void InsertLeftSide(LeftObstacleSide side) {
            ObstacleSideComparer.SetOperand(side);
            LeftObstacleSideTree.Insert((side));
        }

        protected void InsertRightSide(RightObstacleSide side) {
            ObstacleSideComparer.SetOperand(side);
            RightObstacleSideTree.Insert(side);
        }

        protected RightObstacleSide FindFirstObstacleSideToTheLeftOfPoint(Point point) {
            var node =
                RightObstacleSideTree.FindLast(
                    s => Point.PointToTheRightOfLineOrOnLine(point, s.Start, s.End));
            return node == null ? null : (RightObstacleSide)(node.Item);
        }

        protected LeftObstacleSide FindFirstObstacleSideToToTheRightOfPoint(Point point) {
            var node =
                LeftObstacleSideTree.FindFirst(
                    s => !Point.PointToTheRightOfLineOrOnLine(point, s.Start, s.End));
            return node == null ? null : (LeftObstacleSide)node.Item;
        }

        protected void EnqueueEvent(SweepEvent eve) {
            Debug.Assert(GetZ(eve.Site)>=PreviousZ);
            eventQueue.Enqueue(eve);
        }

        protected void InitQueueOfEvents() {
            foreach (var obstacle in Obstacles)
                EnqueueLowestPointsOnObstacles(obstacle);
            if (Ports != null)
                foreach (var point in Ports)
                    EnqueueEvent(new PortObstacleEvent(point));
        }

        void EnqueueLowestPointsOnObstacles(Polyline poly) {
            PolylinePoint candidate = GetLowestPoint(poly);
            EnqueueEvent(new LowestVertexEvent(candidate));
        }

        PolylinePoint GetLowestPoint(Polyline poly) {
            PolylinePoint candidate = poly.StartPoint;
            PolylinePoint pp = poly.StartPoint.Next;

            for (; pp != null; pp = pp.Next)
                if (Less(pp.Point, candidate.Point))
                    candidate = pp;
            return candidate;
        }

        /// <summary>
        /// imagine that direction points up,
        /// lower events have higher priorities,
        /// for events at the same level events to the left have higher priority
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public int Compare(SweepEvent a, SweepEvent b) {
            ValidateArg.IsNotNull(a, "a");
            ValidateArg.IsNotNull(b, "b");
            Point aSite = a.Site;
            Point bSite = b.Site;
            return ComparePoints(ref aSite, ref bSite);
        }

        bool Less(Point a, Point b) {
            return ComparePoints(ref a, ref b) < 0;
        }

        int ComparePoints(ref Point aSite, ref Point bSite) {
            var aProjection = SweepDirection * aSite;
            var bProjection = SweepDirection * bSite;
            if (aProjection < bProjection)
                return -1;
            if (aProjection > bProjection)
                return 1;

            aProjection = directionPerp * aSite;
            bProjection = directionPerp * bSite;

            if (aProjection < bProjection)
                return -1;
            return aProjection > bProjection ? 1 : 0;
        }
    }
}
