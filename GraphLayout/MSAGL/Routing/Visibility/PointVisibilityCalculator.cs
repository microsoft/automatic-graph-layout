using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing.Rectilinear;

namespace Microsoft.Msagl.Routing.Visibility {
    /// <summary>
    /// following "Visibility Algorithms in the Plane", Ghosh
    /// </summary>
    internal class PointVisibilityCalculator {
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")] //  VisibilityGraph graphOfHoleBoundaries;
            ActiveEdgeComparerWithRay activeEdgeComparer;

        RbTree<PolylinePoint> activeSidesTree;

        /// <summary>
        /// A mapping from sides to their RBNodes
        /// </summary>
        Dictionary<PolylinePoint, RBNode<PolylinePoint>> sideNodes = new Dictionary<PolylinePoint, RBNode<PolylinePoint>>();

        readonly BinaryHeapWithComparer<Stem> heapForSorting;

        readonly VisibilityGraph visibilityGraph;
        readonly VisibilityKind visibilityKind;

        /// <summary>
        /// These are parts of hole boundaries visible from q where each node is taken in isolation
        /// </summary>
        readonly Dictionary<Polyline, Stem> visibleBoundaries = new Dictionary<Polyline, Stem>();

        Point q;
        readonly PolylinePoint qPolylinePoint;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal VisibilityVertex QVertex { get; set; }

        readonly List<PolylinePoint> sortedListOfPolypoints = new List<PolylinePoint>();
                                     //the sorted list of possibly visible vertices

        readonly IEnumerable<Polyline> holes;

        /// <summary>
        /// We suppose that the holes are convex and oriented clockwis and are mutually disjoint
        /// </summary>
        /// <param name="listOfHoles"></param>
        /// <param name="visibilityGraph"></param>
        /// <param name="point">The point can belong to the boundary of one of the holes</param>
        /// <param name="visibilityKind">tangent or regural visibility</param>
        /// <param name="qVertex">the graph vertex corresponding to the pivot</param>        
        /// <returns></returns>
        internal static VisibilityVertex CalculatePointVisibilityGraph(
            IEnumerable<Polyline> listOfHoles,
            VisibilityGraph visibilityGraph,
            Point point,
            VisibilityKind visibilityKind) {
            //maybe there is nothing to do
            var qv = visibilityGraph.FindVertex(point);
            if (qv != null) {
                return qv;
            }

            var calculator = new PointVisibilityCalculator(listOfHoles, visibilityGraph, point, visibilityKind);
            calculator.FillGraph();
            return calculator.QVertex;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void FillGraph() {
            ComputeHoleBoundariesPossiblyVisibleFromQ();
            if (visibleBoundaries.Count > 0) {
                SortSAndInitActiveSides();
                // CheckActiveSidesAreConsistent();
                Sweep();
            }
        }

        /// <summary>
        /// sorts the set of potentially visible vertices around point q
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void SortSAndInitActiveSides() {
            InitHeapAndInsertActiveSides();
            for (Stem stem = heapForSorting.GetMinimum();; stem = heapForSorting.GetMinimum()) {
                sortedListOfPolypoints.Add(stem.Start);
                if (stem.MoveStartClockwise())
                    heapForSorting.ChangeMinimum(stem);
                else
                    heapForSorting.Dequeue();
                if (heapForSorting.Count == 0)
                    break;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void InitHeapAndInsertActiveSides() {
            foreach (Stem pp in GetInitialVisibleBoundaryStemsAndInsertActiveSides())
                heapForSorting.Enqueue(pp);
        }


        /// <summary>
        /// these are chuncks of the visible boundaries growing from the polyline  point just above its crossing with the horizontal ray or 
        /// from the visible part start
        /// In the general case we have two stems from one polyline
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        IEnumerable<Stem> GetInitialVisibleBoundaryStemsAndInsertActiveSides() {
            foreach (var keyValuePair in visibleBoundaries) {
                Polyline hole = keyValuePair.Key;
                Stem stem = keyValuePair.Value;
                bool crosses = false;

                foreach (PolylinePoint side in stem.Sides) {
                    PolylinePoint source = side;
                    if (source.Point.Y < q.Y) {
                        if (side.NextOnPolyline.Point.Y >= q.Y) {
                            TriangleOrientation orientation = Point.GetTriangleOrientation(q, source.Point,
                                                                                           side.NextOnPolyline.Point);
                            if (orientation == TriangleOrientation.Counterclockwise ||
                                orientation == TriangleOrientation.Collinear) {
                                crosses = true;
                                //we have two stems here
                                yield return new Stem(stem.Start, side);
                                yield return new Stem(side.NextOnPolyline, stem.End);

                                RegisterActiveSide(side);
                                break;
                            }
                        }
                    } else if (source.Point.Y > q.Y)
                        break;
                    else if (side.Point.X >= q.X) {
                        //we have pp.Y==q.Y
                        crosses = true;
                        //we need to add one or two stems here
                        yield return new Stem(side, stem.End);
                        if (side != stem.Start)
                            yield return new Stem(stem.Start, hole.Prev(source));

                        RegisterActiveSide(side);
                        break;
                    }
                }
                //there is no intersection with the ray
                if (!crosses)
                    yield return stem;
            }
        }

        void RegisterActiveSide(PolylinePoint side) 
        {
            activeEdgeComparer.IntersectionOfTheRayAndInsertedEdge = activeEdgeComparer.IntersectEdgeWithRay(side, new Point(1, 0));
            sideNodes[side] = activeSidesTree.Insert(side);
        }

        //private Polyline GetPolylineBetweenPolyPointsTest(Polyline hole, PolylinePoint p0, PolylinePoint p1) {
        //    Polyline ret = new Polyline();
        //    while (p0 != p1) {
        //        ret.AddPoint(p0.Point);
        //        p0 = hole.Next(p0);
        //    }

        //    ret.AddPoint(p1.Point);
        //    return ret;
        //}

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        PointVisibilityCalculator(IEnumerable<Polyline> holes, VisibilityGraph visibilityGraph, Point point,
                                  VisibilityKind visibilityKind) {
            this.holes = holes;
            //this.graphOfHoleBoundaries = holeBoundariesGraph;
            this.visibilityGraph = visibilityGraph;
            q = point;
            qPolylinePoint = new PolylinePoint(q);
            QVertex = this.visibilityGraph.AddVertex(qPolylinePoint);
            this.visibilityKind = visibilityKind;
            heapForSorting = new BinaryHeapWithComparer<Stem>(new StemStartPointComparer(q));
        }


        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void Sweep() {
            foreach (PolylinePoint polylinePoint in sortedListOfPolypoints)
                SweepPolylinePoint(polylinePoint);
#if TEST_MSAGL
            //List<ICurve> l = new List<ICurve>();
            //foreach (PEdge pe in this.visibilityGraph.Edges) {
            //    if (!ApproximateComparer.Close(pe.SourcePoint, pe.TargetPoint && pe.Target.PolylinePoint.Polyline!=pe.Source.PolylinePoint.Polyline))
            //        l.Add(new LineSegment(pe.SourcePoint, pe.TargetPoint));
            //}

            ////foreach(PEdge pe in this.graphOfHoleBoundaries.Edges)
            ////    l.Add(new LineSegment(pe.SourcePoint, pe.TargetPoint));

            //SugiyamaLayoutSettings.Show(l.ToArray());
#endif
        }

        //this code will work for convex holes
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void SweepPolylinePoint(PolylinePoint v) {
            PolylinePoint inSide = GetIncomingSide(v);
            PolylinePoint outSide = GetOutgoingSide(v);


            //if (inEdge != null && outEdge != null)
            //    SugiyamaLayoutSettings.Show(new LineSegment(inEdge.Start.Point, inEdge.End.Point), new LineSegment(outEdge.Start.Point,
            //        outEdge.End.Point), new LineSegment(this.q, v.Point));
            //else if (inEdge != null)
            //    SugiyamaLayoutSettings.Show(new LineSegment(inEdge.Start.Point, inEdge.End.Point), new LineSegment(this.q, v.Point));
            //else if (outEdge != null)
            //    SugiyamaLayoutSettings.Show(new LineSegment(outEdge.Start.Point, outEdge.End.Point), new LineSegment(this.q, v.Point));

            activeEdgeComparer.IntersectionOfTheRayAndInsertedEdge = v.Point;
            RBNode<PolylinePoint> node;
            if (sideNodes.TryGetValue(inSide, out node) && node != null) {
//we have an active edge
                if (node == activeSidesTree.TreeMinimum())
                    AddEdge(v);

                if (outSide != null) {
                    node.Item = outSide; //just replace the edge since the order does not change
                    sideNodes[outSide] = node;
                } else {
                    RBNode<PolylinePoint> changedNode = activeSidesTree.DeleteSubtree(node);
                    if (changedNode != null)
                        if (changedNode.Item != null)
                            sideNodes[changedNode.Item] = changedNode;
                }
                sideNodes.Remove(inSide);
            } else //the incoming edge is not active
                if (outSide != null) {
                    RBNode<PolylinePoint> outsideNode;
                    if (!sideNodes.TryGetValue(outSide, out outsideNode) || outsideNode == null) {
                        outsideNode = activeSidesTree.Insert(outSide);
                        sideNodes[outSide] = outsideNode;
                        if (outsideNode == activeSidesTree.TreeMinimum())
                            AddEdge(v);
                    }
                } else
                    throw new InvalidOperationException();

            // CheckActiveSidesAreConsistent();
        }


        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void AddEdge(PolylinePoint v) {
            if (visibilityKind == VisibilityKind.Regular ||
                (visibilityKind == VisibilityKind.Tangent && LineTouchesPolygon(QVertex.Point, v))) {
                visibilityGraph.AddEdge(QVertex.Point, v.Point, ((a,b)=>new TollFreeVisibilityEdge(a,b)));
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static bool LineTouchesPolygon(Point a, PolylinePoint p) {
            Point prev = p.Polyline.Prev(p).Point;
            Point next = p.Polyline.Next(p).Point;
            Point v = p.Point;
            return Point.SignedDoubledTriangleArea(a, v, prev)*Point.SignedDoubledTriangleArea(a, v, next) >= 0;
        }

#if TEST_MSAGL
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
// ReSharper disable UnusedMember.Local
        void DrawActiveEdgesAndVisibleGraph() {
// ReSharper restore UnusedMember.Local
            var l = new List<ICurve>();
            foreach (VisibilityEdge pe in visibilityGraph.Edges)
                l.Add(new LineSegment(pe.SourcePoint, pe.TargetPoint));

            foreach (PolylinePoint pe in activeSidesTree)
                l.Add(new LineSegment(pe.Point, pe.NextOnPolyline.Point));
            l.Add(new Ellipse(0.1, 0.1, q));


            LayoutAlgorithmSettings.Show(l.ToArray());
        }
#endif

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        PolylinePoint GetOutgoingSide(PolylinePoint v) {
            Stem visibleStem = visibleBoundaries[v.Polyline];

            if (v == visibleStem.End)
                return null;

            return v;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static PolylinePoint GetIncomingSide(PolylinePoint v) {
            return v.PrevOnPolyline;
        }


        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void ComputeHoleBoundariesPossiblyVisibleFromQ() {
            InitActiveEdgesAndActiveEdgesComparer();

            foreach (Polyline hole in holes)
                ComputeVisiblePartOfTheHole(hole);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void InitActiveEdgesAndActiveEdgesComparer() {
            activeEdgeComparer = new ActiveEdgeComparerWithRay {Pivot = q};
            activeSidesTree = new RbTree<PolylinePoint>(activeEdgeComparer);
        }


        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void ComputeVisiblePartOfTheHole(Polyline hole) {
            //find a separating edge
            PolylinePoint a;
            var needToGoCounterclockWise = true;

            for (a = hole.StartPoint; !HoleSideIsVisibleFromQ(hole, a); a = hole.Next(a)) {
                Debug.Assert(needToGoCounterclockWise || a != hole.StartPoint);
                    //check that we have not done the full circle                
                needToGoCounterclockWise = false;
            }

            PolylinePoint b = hole.Next(a);

            //now the side a, a.Next - is separating
            if (needToGoCounterclockWise)
                while (HoleSideIsVisibleFromQ(hole, hole.Prev(a)))
                    a = hole.Prev(a);

            //go clockwise starting from b
            for (; HoleSideIsVisibleFromQ(hole, b); b = hole.Next(b)) {}

            visibleBoundaries[hole] = new Stem(a, b);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        bool HoleSideIsVisibleFromQ(Polyline hole, PolylinePoint b) {
            return Point.SignedDoubledTriangleArea(q, b.Point, hole.Next(b).Point) >= -ApproximateComparer.SquareOfDistanceEpsilon;
        }
    }
}