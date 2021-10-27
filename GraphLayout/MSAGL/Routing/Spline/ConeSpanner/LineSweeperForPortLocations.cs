using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Visibility;
#if TEST_MSAGL
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.DebugHelpers.Persistence;
#endif

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner {
    /// <summary>
    /// Sweeps a given direction of cones and adds discovered edges to the graph.
    /// The cones can only start at ports here.
    /// </summary>
    class LineSweeperForPortLocations : LineSweeperBase, IConeSweeper {
        public Point ConeRightSideDirection {
            get;
            set;
        }

        public Point ConeLeftSideDirection {
            get;
            set;
        }

        readonly ConeSideComparer coneSideComparer;


        readonly VisibilityGraph visibilityGraph;

        readonly RbTree<ConeSide> rightConeSides;
        readonly RbTree<ConeSide> leftConeSides;
 

        LineSweeperForPortLocations(IEnumerable<Polyline> obstacles, Point direction, Point coneRsDir, Point coneLsDir,
                                    VisibilityGraph visibilityGraph, IEnumerable<Point> portLocations)
            : base(obstacles, direction) {
            this.visibilityGraph = visibilityGraph;
            ConeRightSideDirection = coneRsDir;
            ConeLeftSideDirection = coneLsDir;
            coneSideComparer = new ConeSideComparer(this);
            leftConeSides = new RbTree<ConeSide>(coneSideComparer);
            rightConeSides = new RbTree<ConeSide>(coneSideComparer);
            PortLocations = portLocations;
        }

        IEnumerable<Point> PortLocations {
            get;
            set;
        }

        internal static void Sweep(IEnumerable<Polyline> obstacles,
                                   Point direction, double coneAngle, VisibilityGraph visibilityGraph,
                                   IEnumerable<Point> portLocations) {
            var cs = new LineSweeperForPortLocations(obstacles, direction, direction.Rotate(-coneAngle/2),
                                                     direction.Rotate(coneAngle/2), visibilityGraph, portLocations);
            cs.Calculate();
        }

        void Calculate() {
            InitQueueOfEvents();
            foreach (Point portLocation in PortLocations)
                EnqueueEvent(new PortLocationEvent(portLocation));
            while (EventQueue.Count > 0)
                ProcessEvent(EventQueue.Dequeue());
        }

        void ProcessEvent(SweepEvent p) {
            var vertexEvent = p as VertexEvent;
            // ShowTrees(CurveFactory.CreateDiamond(3, 3, p.Site));
            if (vertexEvent != null)
                ProcessVertexEvent(vertexEvent);
            else {
                var rightIntersectionEvent = p as RightIntersectionEvent;
                if (rightIntersectionEvent != null)
                    ProcessRightIntersectionEvent(rightIntersectionEvent);
                else {
                    var leftIntersectionEvent = p as LeftIntersectionEvent;
                    if (leftIntersectionEvent != null)
                        ProcessLeftIntersectionEvent(leftIntersectionEvent);
                    else {
                        var coneClosure = p as ConeClosureEvent;
                        if (coneClosure != null) {
                            if (!coneClosure.ConeToClose.Removed)
                                RemoveCone(coneClosure.ConeToClose);
                        } else {
                            var portLocationEvent = p as PortLocationEvent;
                            if (portLocationEvent != null)
                                ProcessPortLocationEvent(portLocationEvent);
                            else
                                ProcessPointObstacleEvent((PortObstacleEvent) p);
                        }
                        Z = GetZ(p);
                    }
                }
            }
            //     ShowTrees(CurveFactory.CreateEllipse(3,3,p.Site));
        }

        void ProcessPointObstacleEvent(PortObstacleEvent portObstacleEvent) {
            Z = GetZ(portObstacleEvent);
            GoOverConesSeeingVertexEvent(portObstacleEvent);
        }

        void CreateConeOnPortLocation(SweepEvent sweepEvent) {
            var cone = new Cone(sweepEvent.Site, this);
            RBNode<ConeSide> leftNode = InsertToTree(leftConeSides, cone.LeftSide = new ConeLeftSide(cone));
            RBNode<ConeSide> rightNode = InsertToTree(rightConeSides, cone.RightSide = new ConeRightSide(cone));
            LookForIntersectionWithConeRightSide(rightNode);
            LookForIntersectionWithConeLeftSide(leftNode);
        }

        void ProcessPortLocationEvent(PortLocationEvent portEvent) {
            Z = GetZ(portEvent);
            GoOverConesSeeingVertexEvent(portEvent);
            CreateConeOnPortLocation(portEvent);
        }


        void ProcessLeftIntersectionEvent(LeftIntersectionEvent leftIntersectionEvent) {
            if (leftIntersectionEvent.coneLeftSide.Removed == false) {
                if (Math.Abs((leftIntersectionEvent.EndVertex.Point - leftIntersectionEvent.Site)*SweepDirection) <
                    ApproximateComparer.DistanceEpsilon) {
                    //the cone is totally covered by a horizontal segment
                    RemoveCone(leftIntersectionEvent.coneLeftSide.Cone);
                } else {
                    RemoveSegFromLeftTree(leftIntersectionEvent.coneLeftSide);
                    Z = SweepDirection*leftIntersectionEvent.Site; //it is safe now to restore the order
                    var leftSide = new BrokenConeSide(
                        leftIntersectionEvent.Site,
                        leftIntersectionEvent.EndVertex, leftIntersectionEvent.coneLeftSide);
                    InsertToTree(leftConeSides, leftSide);
                    leftIntersectionEvent.coneLeftSide.Cone.LeftSide = leftSide;
                    LookForIntersectionOfObstacleSideAndLeftConeSide(leftIntersectionEvent.Site,
                                                                     leftIntersectionEvent.EndVertex);
                    TryCreateConeClosureForLeftSide(leftSide);
                }
            } else
                Z = SweepDirection*leftIntersectionEvent.Site;
        }

        void TryCreateConeClosureForLeftSide(BrokenConeSide leftSide) {
            var coneRightSide = leftSide.Cone.RightSide as ConeRightSide;
            if (coneRightSide != null)
                if (
                    Point.GetTriangleOrientation(coneRightSide.Start, coneRightSide.Start + coneRightSide.Direction,
                                                 leftSide.EndVertex.Point) == TriangleOrientation.Clockwise)
                    CreateConeClosureEvent(leftSide, coneRightSide);
        }

        void CreateConeClosureEvent(BrokenConeSide brokenConeSide, ConeSide otherSide) {
            Point x;
            bool r = Point.RayIntersectsRayInteriors(brokenConeSide.start, brokenConeSide.Direction, otherSide.Start,
                                                     otherSide.Direction, out x);
            Debug.Assert(r);
            EnqueueEvent(new ConeClosureEvent(x, brokenConeSide.Cone));
        }

        void ProcessRightIntersectionEvent(RightIntersectionEvent rightIntersectionEvent) {
            //restore Z for the time being
            // Z = PreviousZ;
            if (rightIntersectionEvent.coneRightSide.Removed == false) {
                //it can happen that the cone side participating in the intersection is gone;
                //obstracted by another obstacle or because of a vertex found inside of the cone
                //PrintOutRightSegTree();
                RemoveSegFromRightTree(rightIntersectionEvent.coneRightSide);
                Z = SweepDirection*rightIntersectionEvent.Site;
                var rightSide = new BrokenConeSide(
                    rightIntersectionEvent.Site,
                    rightIntersectionEvent.EndVertex, rightIntersectionEvent.coneRightSide);
                InsertToTree(rightConeSides, rightSide);
                rightIntersectionEvent.coneRightSide.Cone.RightSide = rightSide;
                LookForIntersectionOfObstacleSideAndRightConeSide(rightIntersectionEvent.Site,
                                                                  rightIntersectionEvent.EndVertex);

                TryCreateConeClosureForRightSide(rightSide);
            } else
                Z = SweepDirection*rightIntersectionEvent.Site;
        }

        void TryCreateConeClosureForRightSide(BrokenConeSide rightSide) {
            var coneLeftSide = rightSide.Cone.LeftSide as ConeLeftSide;
            if (coneLeftSide != null)
                if (
                    Point.GetTriangleOrientation(coneLeftSide.Start, coneLeftSide.Start + coneLeftSide.Direction,
                                                 rightSide.EndVertex.Point) == TriangleOrientation.Counterclockwise)
                    CreateConeClosureEvent(rightSide, coneLeftSide);
        }

        void RemoveConesClosedBySegment(Point leftPoint, Point rightPoint) {
            CloseConesCoveredBySegment(leftPoint, rightPoint,
                                       SweepDirection*leftPoint > SweepDirection*rightPoint
                                           ? leftConeSides
                                           : rightConeSides);
        }

        void CloseConesCoveredBySegment(Point leftPoint, Point rightPoint, RbTree<ConeSide> tree) {
            RBNode<ConeSide> node = tree.FindFirst(
                s => Point.GetTriangleOrientation(s.Start, s.Start + s.Direction, leftPoint) ==
                     TriangleOrientation.Counterclockwise);

            Point x;
            if (node == null || !Point.IntervalIntersectsRay(leftPoint, rightPoint,
                                                             node.Item.Start, node.Item.Direction, out x))
                return;
            var conesToRemove = new List<Cone>();
            do {
                conesToRemove.Add(node.Item.Cone);
                node = tree.Next(node);
            } while (node != null && Point.IntervalIntersectsRay(leftPoint, rightPoint,
                                                                 node.Item.Start, node.Item.Direction, out x));


            foreach (Cone cone in conesToRemove)
                RemoveCone(cone);
        }

        void ProcessVertexEvent(VertexEvent vertexEvent) {
            Z = GetZ(vertexEvent);
            GoOverConesSeeingVertexEvent(vertexEvent);
            AddConeAndEnqueueEvents(vertexEvent);
        }

#if TEST_MSAGL
    // ReSharper disable UnusedMember.Local
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static Ellipse EllipseOnVert(SweepEvent vertexEvent) {
            // ReSharper restore UnusedMember.Local
            return new Ellipse(2, 2, vertexEvent.Site);
        }

        // ReSharper disable UnusedMember.Local
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static Ellipse EllipseOnPolylinePoint(PolylinePoint pp) {
            // ReSharper restore UnusedMember.Local
            return new Ellipse(2, 2, pp.Point);
        }

#endif


#if TEST_MSAGL
    // ReSharper disable UnusedMember.Local
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.WriteLine(System.String)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void CheckConsistency() {
            // ReSharper restore UnusedMember.Local
            foreach (var s in rightConeSides) {
                coneSideComparer.SetOperand(s);
            }
            foreach (var s in leftConeSides) {
                coneSideComparer.SetOperand(s);
                if (!rightConeSides.Contains(s.Cone.RightSide)) {
                    PrintOutRightSegTree();
                    PrintOutLeftSegTree();
                }
            }
        }

        // ReSharper disable UnusedMember.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void ShowTrees(params ICurve[] curves) {
            // ReSharper restore UnusedMember.Local
            var l = Obstacles.Select(c => new DebugCurve(100, 1, "blue", c));
            l = l.Concat(rightConeSides.Select(s => new DebugCurve(200, 1, "brown", ExtendSegmentToZ(s))));
            l = l.Concat(leftConeSides.Select(s => new DebugCurve(200, 1, "gree", ExtendSegmentToZ(s))));
            l = l.Concat(curves.Select(c => new DebugCurve("red", c)));
            l =
                l.Concat(
                    visibilityGraph.Edges.Select(e => new LineSegment(e.SourcePoint, e.TargetPoint)).Select(
                        c => new DebugCurve("marine", c)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        }

        void ShowLeftTree(params ICurve[] curves) {
            var l = Obstacles.Select(c => new DebugCurve(c));
            l = l.Concat(leftConeSides.Select(s => new DebugCurve("brown", ExtendSegmentToZ(s))));
            l = l.Concat(curves.Select(c => new DebugCurve("red", c)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);

        }
        void ShowRightTree(params ICurve[] curves) {
            var l = Obstacles.Select(c => new DebugCurve(c));
            l = l.Concat(rightConeSides.Select(s => new DebugCurve("brown", ExtendSegmentToZ(s))));
            l = l.Concat(curves.Select(c => new DebugCurve("red", c)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        }

        // ReSharper disable UnusedMember.Global
        internal void Show(params ICurve[] curves) {
            // ReSharper restore UnusedMember.Global
            var l = Obstacles.Select(c => new DebugCurve(100, 1, "black", c));

            l = l.Concat(curves.Select(c => new DebugCurve(200, 1, "red", c)));
            //            foreach (var s in rightConeSides){
            //                l.Add(ExtendSegmentToZ(s));
            //                if (s is BrokenConeSide)
            //                    l.Add(Diamond(s.Start));
            //                l.Add(ExtendSegmentToZ(s.Cone.LeftSide));
            //            }

            l =
                l.Concat(
                    visibilityGraph.Edges.Select(edge => new LineSegment(edge.SourcePoint, edge.TargetPoint)).Select(
                        c => new DebugCurve(100, 1, "blue", c)));


            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);

        }

        ICurve ExtendSegmentToZ(ConeSide segment) {
            double den = segment.Direction * SweepDirection;
            Debug.Assert(Math.Abs(den) > ApproximateComparer.DistanceEpsilon);
            double t = (Z - segment.Start * SweepDirection) / den;

            return new LineSegment(segment.Start, segment.Start + segment.Direction * t);
        }



#endif
        void AddConeAndEnqueueEvents(VertexEvent vertexEvent) {
            var leftVertexEvent = vertexEvent as LeftVertexEvent;
            if (leftVertexEvent != null) {
                PolylinePoint nextPoint = vertexEvent.Vertex.NextOnPolyline;
                CloseConesAtLeftVertex(leftVertexEvent, nextPoint);
            } else {
                var rightVertexEvent = vertexEvent as RightVertexEvent;
                if (rightVertexEvent != null) {
                    PolylinePoint nextPoint = vertexEvent.Vertex.PrevOnPolyline;
                    CloseConesAtRightVertex(rightVertexEvent, nextPoint);
                } else {
                    CloseConesAtLeftVertex(vertexEvent, vertexEvent.Vertex.NextOnPolyline);
                    CloseConesAtRightVertex(vertexEvent, vertexEvent.Vertex.PrevOnPolyline);
                }
            }
        }

        void CloseConesAtRightVertex(VertexEvent rightVertexEvent,
                                     PolylinePoint nextVertex) {
            Point prevSite = rightVertexEvent.Vertex.NextOnPolyline.Point;
            double prevZ = prevSite*SweepDirection;
            if (prevZ <= Z && Z - prevZ < ApproximateComparer.DistanceEpsilon)
                RemoveConesClosedBySegment(prevSite, rightVertexEvent.Vertex.Point);

            Point site = rightVertexEvent.Site;
            Point coneLp = site + ConeLeftSideDirection;
            Point coneRp = site + ConeRightSideDirection;
            Point nextSite = nextVertex.Point;
            //SugiyamaLayoutSettings.Show(new LineSegment(site, coneLP), new LineSegment(site, coneRP), new LineSegment(site, nextSite));
            //try to remove the right side
            if ((site - prevSite)*SweepDirection > ApproximateComparer.DistanceEpsilon)
                RemoveRightSide(new RightObstacleSide(rightVertexEvent.Vertex.NextOnPolyline));
            if (GetZ(nextSite) + ApproximateComparer.DistanceEpsilon < GetZ(rightVertexEvent))
                return;
            if (!Point.PointToTheRightOfLineOrOnLine(nextSite, site, coneLp)) {
                //if (angle <= -coneAngle / 2) {
                //   CreateConeOnVertex(rightVertexEvent);
                if (Point.PointToTheLeftOfLineOrOnLine(nextSite + DirectionPerp, nextSite, site))
                    EnqueueEvent(new RightVertexEvent(nextVertex));
                //  TryEnqueueRighVertexEvent(nextVertex);
            } else if (Point.PointToTheLeftOfLineOrOnLine(nextSite, site, coneRp)) {
                //if (angle < coneAngle / 2) {
                CaseToTheLeftOfLineOrOnLineConeRp(rightVertexEvent, nextVertex);
            } else {
                if ((nextSite - site)*SweepDirection > ApproximateComparer.DistanceEpsilon) {
                    LookForIntersectionOfObstacleSideAndLeftConeSide(rightVertexEvent.Site, nextVertex);
                    InsertRightSide(new RightObstacleSide(rightVertexEvent.Vertex));
                }
                EnqueueEvent(new RightVertexEvent(nextVertex));
            }
        }

        void CaseToTheLeftOfLineOrOnLineConeRp(VertexEvent rightVertexEvent, PolylinePoint nextVertex) {
            EnqueueEvent(new RightVertexEvent(nextVertex));
            //the obstacle side is inside of the cone
            //we need to create an obstacle left side segment instead of the left cone side
            //                var cone = new Cone(rightVertexEvent.Vertex.Point, this);
            //                var obstacleSideSeg = new BrokenConeSide(cone.Apex, nextVertex, new ConeLeftSide(cone));
            //                cone.LeftSide = obstacleSideSeg;
            //                cone.RightSide = new ConeRightSide(cone);
            //                var rnode = InsertToTree(rightConeSides, cone.RightSide);
            //                LookForIntersectionWithConeRightSide(rnode);
            RBNode<ConeSide> lnode =
                leftConeSides.FindFirst(side => PointIsToTheLeftOfSegment(rightVertexEvent.Site, side));
            FixConeLeftSideIntersections(rightVertexEvent.Vertex, nextVertex, lnode);
            if ((nextVertex.Point - rightVertexEvent.Site)*SweepDirection > ApproximateComparer.DistanceEpsilon)
                InsertRightSide(new RightObstacleSide(rightVertexEvent.Vertex));
        }


        void LookForIntersectionOfObstacleSideAndRightConeSide(Point obstacleSideStart,
                                                               PolylinePoint obstacleSideVertex) {
            RBNode<ConeSide> node = GetLastNodeToTheLeftOfPointInRightSegmentTree(obstacleSideStart);

            if (node != null) {
                var coneRightSide = node.Item as ConeRightSide;
                if (coneRightSide != null) {
                    Point intersection;
                    if (Point.IntervalIntersectsRay(obstacleSideStart, obstacleSideVertex.Point,
                                                    coneRightSide.Start, ConeRightSideDirection, out intersection) &&
                        SegmentIsNotHorizontal(intersection, obstacleSideVertex.Point)) {
                        EnqueueEvent(CreateRightIntersectionEvent(coneRightSide, intersection, obstacleSideVertex));
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        RightIntersectionEvent CreateRightIntersectionEvent(ConeRightSide coneRightSide, Point intersection,
                                                            PolylinePoint obstacleSideVertex) {
            Debug.Assert(Math.Abs((obstacleSideVertex.Point - intersection)*SweepDirection) >
                         ApproximateComparer.DistanceEpsilon);
            return new RightIntersectionEvent(coneRightSide,
                                              intersection, obstacleSideVertex);
        }

        RBNode<ConeSide> GetLastNodeToTheLeftOfPointInRightSegmentTree(Point obstacleSideStart) {
            return rightConeSides.FindLast(
                s => PointIsToTheRightOfSegment(obstacleSideStart, s));
        }

        void LookForIntersectionOfObstacleSideAndLeftConeSide(Point obstacleSideStart,
                                                              PolylinePoint obstacleSideVertex) {
            RBNode<ConeSide> node = GetFirstNodeToTheRightOfPoint(obstacleSideStart);
            //          ShowLeftTree(Box(obstacleSideStart));
            if (node == null) return;
            var coneLeftSide = node.Item as ConeLeftSide;
            if (coneLeftSide == null) return;
            Point intersection;
            if (Point.IntervalIntersectsRay(obstacleSideStart, obstacleSideVertex.Point, coneLeftSide.Start,
                                            ConeLeftSideDirection, out intersection)) {
                EnqueueEvent(new LeftIntersectionEvent(coneLeftSide, intersection, obstacleSideVertex));
            }
        }

        RBNode<ConeSide> GetFirstNodeToTheRightOfPoint(Point p) {
            return leftConeSides.FindFirst(s => PointIsToTheLeftOfSegment(p, s));
        }

#if TEST_MSAGL
    // ReSharper disable UnusedMember.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static ICurve Box(Point p) {
            // ReSharper restore UnusedMember.Local
            return CurveFactory.CreateRectangle(2, 2, p);
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.WriteLine(System.String)")]
        void PrintOutRightSegTree() {
            System.Diagnostics.Debug.WriteLine("right segment tree");
            foreach (var t in rightConeSides)
                System.Diagnostics.Debug.WriteLine(t);
            System.Diagnostics.Debug.WriteLine("end of right segments");
        }
#endif

        static bool PointIsToTheLeftOfSegment(Point p, ConeSide seg) {
            return (Point.GetTriangleOrientation(seg.Start, seg.Start + seg.Direction, p) ==
                    TriangleOrientation.Counterclockwise);
        }

        static bool PointIsToTheRightOfSegment(Point p, ConeSide seg) {
            return (Point.GetTriangleOrientation(seg.Start, seg.Start + seg.Direction, p) ==
                    TriangleOrientation.Clockwise);
        }


        void FixConeLeftSideIntersections(PolylinePoint obstSideStart, PolylinePoint obstSideEnd,
                                          RBNode<ConeSide> rbNode) {
            if (rbNode != null) {
                Point intersection;
                var seg = rbNode.Item as ConeLeftSide;
                if (seg != null &&
                    Point.IntervalIntersectsRay(obstSideStart.Point, obstSideEnd.Point, seg.Start, seg.Direction,
                                                out intersection)) {
                    EnqueueEvent(new LeftIntersectionEvent(seg, intersection, obstSideEnd));
                }
            }
        }


        RBNode<ConeSide> InsertToTree(RbTree<ConeSide> tree, ConeSide coneSide) {
            Debug.Assert(coneSide.Direction*SweepDirection > ApproximateComparer.DistanceEpsilon);
            coneSideComparer.SetOperand(coneSide);
            return tree.Insert(coneSide);
        }


        void CloseConesAtLeftVertex(VertexEvent leftVertexEvent, PolylinePoint nextVertex) {
            //close segments first
            Point prevSite = leftVertexEvent.Vertex.PrevOnPolyline.Point;
            double prevZ = prevSite*SweepDirection;
            if (prevZ <= Z && Z - prevZ < ApproximateComparer.DistanceEpsilon) {
                //Show(
                //    new Ellipse(1, 1, prevSite),
                //    CurveFactory.CreateBox(2, 2, leftVertexEvent.Vertex.Point));

                RemoveConesClosedBySegment(leftVertexEvent.Vertex.Point, prevSite);
            }

            Point site = leftVertexEvent.Site;
            Point coneLp = site + ConeLeftSideDirection;
            Point coneRp = site + ConeRightSideDirection;
            Point nextSite = nextVertex.Point;
            // SugiyamaLayoutSettings.Show(new LineSegment(site, coneLP), new LineSegment(site, coneRP), new LineSegment(site, nextSite));

            if ((site - prevSite)*SweepDirection > ApproximateComparer.DistanceEpsilon)
                RemoveLeftSide(new LeftObstacleSide(leftVertexEvent.Vertex.PrevOnPolyline));


            if (Point.PointToTheRightOfLineOrOnLine(nextSite, site, site + DirectionPerp)) {
                //if (angle > Math.PI / 2)
                //   CreateConeOnVertex(leftVertexEvent); //it is the last left vertex on this obstacle
            } else if (!Point.PointToTheLeftOfLineOrOnLine(nextSite, site, coneRp)) {
                //if (angle >= coneAngle / 2) {
                // CreateConeOnVertex(leftVertexEvent);
                EnqueueEvent(new LeftVertexEvent(nextVertex));
                //we schedule LeftVertexEvent for a vertex with horizontal segment to the left on the top of the obstace
            } else if (!Point.PointToTheLeftOfLineOrOnLine(nextSite, site, coneLp)) {
                //if (angle >= -coneAngle / 2) {
                //we cannot completely obscure the cone here
                EnqueueEvent(new LeftVertexEvent(nextVertex));
                //the obstacle side is inside of the cone
                //we need to create an obstacle right side segment instead of the cone side
                //                var cone = new Cone(leftVertexEvent.Vertex.Point, this);
                //                var rightSide = new BrokenConeSide(leftVertexEvent.Vertex.Point, nextVertex,
                //                                                        new ConeRightSide(cone));
                //                cone.RightSide = rightSide;
                //                cone.LeftSide = new ConeLeftSide(cone);
                //                LookForIntersectionWithConeLeftSide(InsertToTree(leftConeSides, cone.LeftSide));
                RBNode<ConeSide> rbNode = rightConeSides.FindLast(s => PointIsToTheRightOfSegment(site, s));
                FixConeRightSideIntersections(leftVertexEvent.Vertex, nextVertex, rbNode);
                if ((nextVertex.Point - leftVertexEvent.Site)*SweepDirection > ApproximateComparer.DistanceEpsilon)
                    InsertLeftSide(new LeftObstacleSide(leftVertexEvent.Vertex));
            } else {
                EnqueueEvent(new LeftVertexEvent(nextVertex));
                if ((nextVertex.Point - leftVertexEvent.Site)*SweepDirection > ApproximateComparer.DistanceEpsilon) {
                    //if( angle >- Pi/2
                    // Debug.Assert(angle > -Math.PI / 2);
                    LookForIntersectionOfObstacleSideAndRightConeSide(leftVertexEvent.Site, nextVertex);
                    InsertLeftSide(new LeftObstacleSide(leftVertexEvent.Vertex));
                }
            }
        }

        void RemoveCone(Cone cone) {
            Debug.Assert(cone.Removed == false);
            cone.Removed = true;
            RemoveSegFromLeftTree(cone.LeftSide);
            RemoveSegFromRightTree(cone.RightSide);
        }


        void RemoveSegFromRightTree(ConeSide coneSide) {
            //   ShowRightTree();
            Debug.Assert(coneSide.Removed == false);
            coneSideComparer.SetOperand(coneSide);
            RBNode<ConeSide> b = rightConeSides.Remove(coneSide);
            coneSide.Removed = true;
            if (b == null) {
                double tmpZ = Z;
                Z = Math.Max(GetZ(coneSide.Start), Z - 0.01);
                //we need to return to the past a little bit when the order was still correc
                coneSideComparer.SetOperand(coneSide);
                b = rightConeSides.Remove(coneSide);
                Z = tmpZ;

#if TEST_MSAGL
                if (b == null) {
                    PrintOutRightSegTree();
                    ShowRightTree(CurveFactory.CreateDiamond(3, 4, coneSide.Start));
                    GeometryGraph gg = CreateGraphFromObstacles(Obstacles);
                    GeometryGraphWriter.Write(gg, "c:\\tmp\\bug1");
                }
#endif
            }
            Debug.Assert(b != null);
        }

        void RemoveSegFromLeftTree(ConeSide coneSide) {
            Debug.Assert(coneSide.Removed == false);
            coneSide.Removed = true;
            coneSideComparer.SetOperand(coneSide);
            RBNode<ConeSide> b = leftConeSides.Remove(coneSide);

            if (b == null) {
                double tmpZ = Z;
                Z = Math.Max(GetZ(coneSide.Start), Z - 0.01);
                coneSideComparer.SetOperand(coneSide);

                b = leftConeSides.Remove(coneSide);
                Z = tmpZ;
#if TEST_MSAGL
                if (b == null) {
                    PrintOutLeftSegTree();
                    ShowLeftTree(new Ellipse(2, 2, coneSide.Start));
                }
#endif
            }

            Debug.Assert(b != null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obstSideEndVertex"></param>
        /// <param name="rbNode">represents a node of the right cone side</param>
        /// <param name="obstSideStartVertex"></param>
        void FixConeRightSideIntersections(PolylinePoint obstSideStartVertex, PolylinePoint obstSideEndVertex,
                                           RBNode<ConeSide> rbNode) {
            if (rbNode != null) {
                Point intersection;
                var seg = rbNode.Item as ConeRightSide;
                if (seg != null &&
                    Point.IntervalIntersectsRay(obstSideStartVertex.Point, obstSideEndVertex.Point, seg.Start,
                                                seg.Direction,
                                                out intersection)) {
                    EnqueueEvent(CreateRightIntersectionEvent(seg, intersection, obstSideEndVertex));
                }
            }
        }


        void LookForIntersectionWithConeLeftSide(RBNode<ConeSide> leftNode) {
            //Show(new Ellipse(1, 1, leftNode.item.Start));


            var coneLeftSide = leftNode.Item as ConeLeftSide;
            if (coneLeftSide != null) {
                //leftNode = leftSegmentTree.TreePredecessor(leftNode);
                //if (leftNode != null) {
                //    var seg = leftNode.item as ObstacleSideSegment;
                //    if (seg != null)
                //        TryIntersectionOfConeLeftSideAndObstacleConeSide(coneLeftSide, seg);
                //}

                RightObstacleSide rightObstacleSide = FindFirstObstacleSideToTheLeftOfPoint(coneLeftSide.Start);
                if (rightObstacleSide != null)
                    TryIntersectionOfConeLeftSideAndObstacleSide(coneLeftSide, rightObstacleSide);
            } else {
                var seg = (BrokenConeSide) leftNode.Item;
                leftNode = leftConeSides.Next(leftNode);
                if (leftNode != null) {
                    coneLeftSide = leftNode.Item as ConeLeftSide;
                    if (coneLeftSide != null)
                        TryIntersectionOfConeLeftSideAndObstacleConeSide(coneLeftSide, seg);
                }
            }
        }


        void LookForIntersectionWithConeRightSide(RBNode<ConeSide> rightNode) {
            //Show(new Ellipse(10, 5, rightNode.item.Start));
            var coneRightSide = rightNode.Item as ConeRightSide;
            if (coneRightSide != null) {
                //rightNode = rightSegmentTree.TreeSuccessor(rightNode);
                //if (rightNode != null) {
                //    var seg = rightNode.item as ObstacleSideSegment;
                //    if (seg != null)
                //        TryIntersectionOfConeRightSideAndObstacleConeSide(coneRightSide, seg);
                //}

                LeftObstacleSide leftObstacleSide = FindFirstObstacleSideToToTheRightOfPoint(coneRightSide.Start);
                if (leftObstacleSide != null)
                    TryIntersectionOfConeRightSideAndObstacleSide(coneRightSide, leftObstacleSide);
            } else {
                var seg = (BrokenConeSide) rightNode.Item;
                rightNode = rightConeSides.Previous(rightNode);
                if (rightNode != null) {
                    coneRightSide = rightNode.Item as ConeRightSide;
                    if (coneRightSide != null)
                        TryIntersectionOfConeRightSideAndObstacleConeSide(coneRightSide, seg);
                }
            }
        }

        void TryIntersectionOfConeRightSideAndObstacleConeSide(ConeRightSide coneRightSide,
                                                               BrokenConeSide seg) {
            Point x;
            if (Point.IntervalIntersectsRay(seg.Start, seg.End, coneRightSide.Start,
                                            coneRightSide.Direction, out x)) {
                EnqueueEvent(CreateRightIntersectionEvent(coneRightSide, x, seg.EndVertex));
                //Show(CurveFactory.CreateDiamond(3, 3, x));
            }
        }

        void TryIntersectionOfConeRightSideAndObstacleSide(ConeRightSide coneRightSide, ObstacleSide side) {
            Point x;
            if (Point.IntervalIntersectsRay(side.Start, side.End, coneRightSide.Start,
                                            coneRightSide.Direction, out x)) {
                EnqueueEvent(CreateRightIntersectionEvent(coneRightSide, x, side.EndVertex));
                //Show(CurveFactory.CreateDiamond(3, 3, x));
            }
        }

        void TryIntersectionOfConeLeftSideAndObstacleConeSide(ConeLeftSide coneLeftSide, BrokenConeSide seg) {
            Point x;
            if (Point.IntervalIntersectsRay(seg.Start, seg.End, coneLeftSide.Start, coneLeftSide.Direction, out x)) {
                EnqueueEvent(new LeftIntersectionEvent(coneLeftSide, x, seg.EndVertex));
                //Show(CurveFactory.CreateDiamond(3, 3, x));
            }
        }

        void TryIntersectionOfConeLeftSideAndObstacleSide(ConeLeftSide coneLeftSide, ObstacleSide side) {
            Point x;
            if (Point.IntervalIntersectsRay(side.Start, side.End, coneLeftSide.Start, coneLeftSide.Direction, out x)) {
                EnqueueEvent(new LeftIntersectionEvent(coneLeftSide, x, side.EndVertex));
                //    Show(CurveFactory.CreateDiamond(3, 3, x));
            }
        }


        //        static int count;
        void GoOverConesSeeingVertexEvent(SweepEvent vertexEvent) {
            RBNode<ConeSide> rbNode = FindFirstSegmentInTheRightTreeNotToTheLeftOfVertex(vertexEvent);

            if (rbNode == null) return;
            ConeSide coneRightSide = rbNode.Item;
            Cone cone = coneRightSide.Cone;
            ConeSide leftConeSide = cone.LeftSide;
            if (VertexIsToTheLeftOfSegment(vertexEvent, leftConeSide)) return;
            var visibleCones = new List<Cone> {cone};
            coneSideComparer.SetOperand(leftConeSide);
            rbNode = leftConeSides.Find(leftConeSide);

            if (rbNode == null) {
                double tmpZ = Z;

                Z = Math.Max(GetZ(leftConeSide.Start), PreviousZ);
                //we need to return to the past when the order was still correct
                coneSideComparer.SetOperand(leftConeSide);
                rbNode = leftConeSides.Find(leftConeSide);
                Z = tmpZ;


#if TEST_MSAGL
                if (rbNode == null) {
                    //GeometryGraph gg = CreateGraphFromObstacles();
                    //gg.Save("c:\\tmp\\bug");


                    PrintOutLeftSegTree();
                    System.Diagnostics.Debug.WriteLine(leftConeSide);
                    ShowLeftTree(new Ellipse(3, 3, vertexEvent.Site));
                    ShowRightTree(new Ellipse(3, 3, vertexEvent.Site));
                }
#endif
            }

            rbNode = leftConeSides.Next(rbNode);
            while (rbNode != null && !VertexIsToTheLeftOfSegment(vertexEvent, rbNode.Item)) {
                visibleCones.Add(rbNode.Item.Cone);
                rbNode = leftConeSides.Next(rbNode);
            }

            //Show(new Ellipse(1, 1, vertexEvent.Site));

            foreach (Cone c in visibleCones) {
                AddEdge(c.Apex, vertexEvent.Site);
                RemoveCone(c);
            }
        }

        void AddEdge(Point a, Point b) {
            Debug.Assert(PortLocations.Contains(a));
            /*********************
            A complication arises when we have overlaps. Loose obstacles become large enough to contain several
            ports. We need to avoid a situation when a port has degree more than one. 
            To avoid this situation we redirect to b every edge incoming into a. 
            Notice that we create a new graph for each AddDiriction call, so all this edges point roughly to the 
            direction of the sweep and the above procedure just alignes the edges better.
            In the resulting graph, which contains the sum of the graphs passed to AddDirection, of course
            a port can have an incoming and outcoming edge at the same time
            *******************/


            VisibilityEdge ab = visibilityGraph.AddEdge(a, b);
            VisibilityVertex av = ab.Source;
            Debug.Assert(av.Point == a && ab.TargetPoint == b);
            //all edges adjacent to a which are different from ab
            VisibilityEdge[] edgesToFix =
                av.InEdges.Where(e => e != ab).Concat(av.OutEdges.Where(e => e != ab)).ToArray();
            foreach (VisibilityEdge edge in edgesToFix) {
                Point c = (edge.Target == av ? edge.Source : edge.Target).Point;
                VisibilityGraph.RemoveEdge(edge);
                visibilityGraph.AddEdge(c, b);
            }
        }


#if TEST_MSAGL
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        static GeometryGraph CreateGraphFromObstacles(IEnumerable<Polyline> obstacles) {
            var gg = new GeometryGraph();
            foreach (var ob in obstacles) {
                gg.Nodes.Add(new Node(ob.ToCurve()));
            }
            return gg;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.WriteLine(System.String)")]
        void PrintOutLeftSegTree() {
            System.Diagnostics.Debug.WriteLine("Left cone segments");
            foreach (var t in leftConeSides)
                System.Diagnostics.Debug.WriteLine(t);
            System.Diagnostics.Debug.WriteLine("end of left cone segments");
        }
#endif

        static bool VertexIsToTheLeftOfSegment(SweepEvent vertexEvent, ConeSide seg) {
            return (Point.GetTriangleOrientation(seg.Start, seg.Start + seg.Direction,
                                                 vertexEvent.Site) == TriangleOrientation.Counterclockwise);
        }

        static bool VertexIsToTheRightOfSegment(SweepEvent vertexEvent, ConeSide seg) {
            return (Point.GetTriangleOrientation(seg.Start, seg.Start + seg.Direction,
                                                 vertexEvent.Site) == TriangleOrientation.Clockwise);
        }

        RBNode<ConeSide> FindFirstSegmentInTheRightTreeNotToTheLeftOfVertex(SweepEvent vertexEvent) {
            return rightConeSides.FindFirst(
                s => !VertexIsToTheRightOfSegment(vertexEvent, s)
                );
        }

        void EnqueueEvent(RightVertexEvent vertexEvent) {
            if (SweepDirection*(vertexEvent.Site - vertexEvent.Vertex.PrevOnPolyline.Point) >
                ApproximateComparer.Tolerance)
                return;
                    //otherwise we enqueue the vertex twice; once as a LeftVertexEvent and once as a RightVertexEvent
            base.EnqueueEvent(vertexEvent);
        }
    }
}