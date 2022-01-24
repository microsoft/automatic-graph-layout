using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
#if TEST_MSAGL
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
#endif
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner {
    /// <summary>
    /// sweeps a given direction of cones and adds discovered edges to the graph
    /// </summary>
    internal class LineSweeper : LineSweeperBase, IConeSweeper {
        public Point ConeRightSideDirection { get; set; }
        public Point ConeLeftSideDirection { get; set; }

        readonly ConeSideComparer coneSideComparer;
        readonly VisibilityGraph visibilityGraph;

        readonly RbTree<ConeSide> rightConeSides;
        readonly RbTree<ConeSide> leftConeSides;
        VisibilityGraph portEdgesGraph;
        internal Func<VisibilityVertex, VisibilityVertex, VisibilityEdge> PortEdgesCreator { get; set; }

        LineSweeper(IEnumerable<Polyline> obstacles, Point direction, Point coneRsDir, Point coneLsDir,
                    VisibilityGraph visibilityGraph, Set<Point> ports, Polyline borderPolyline)
            : base(obstacles, direction) {
            this.visibilityGraph = visibilityGraph;
            ConeRightSideDirection = coneRsDir;
            ConeLeftSideDirection = coneLsDir;
            coneSideComparer = new ConeSideComparer(this);
            leftConeSides = new RbTree<ConeSide>(coneSideComparer);
            rightConeSides = new RbTree<ConeSide>(coneSideComparer);
            Ports = ports;
            BorderPolyline = borderPolyline;
            PortEdgesCreator = (a, b) => new TollFreeVisibilityEdge(a, b);
        }

        Polyline BorderPolyline { get; set; }

       
        internal static void Sweep(
            IEnumerable<Polyline> obstacles,
            Point direction,
            double coneAngle,
            VisibilityGraph visibilityGraph,
            Set<Point> ports,
            Polyline borderPolyline) {
         
            var cs = new LineSweeper(obstacles, direction, direction.Rotate(-coneAngle/2),
                                     direction.Rotate(coneAngle/2), visibilityGraph, ports,
                                     borderPolyline);
            cs.Calculate();
        }



        void Calculate() {
            InitQueueOfEvents();
            while (EventQueue.Count > 0)
                ProcessEvent(EventQueue.Dequeue());
            if (BorderPolyline != null)
                CloseRemainingCones();
            CreatePortEdges();

        }

        void CreatePortEdges() {
            if (portEdgesGraph != null)
                foreach (var edge in portEdgesGraph.Edges) {
                    visibilityGraph.AddEdge(edge.SourcePoint, edge.TargetPoint, PortEdgesCreator);
                }
        }

        void CloseRemainingCones() {
            if (leftConeSides.Count == 0)
                return;
            Debug.Assert(leftConeSides.Count == rightConeSides.Count);

            PolylinePoint p = BorderPolyline.StartPoint;
            var steps=leftConeSides.Count; //we cannot make more than leftConeSides.Count if the data is correct
            //because at each step we remove at least one cone
            do {
                var cone = leftConeSides.TreeMinimum().Item.Cone;
                p = FindPolylineSideIntersectingConeRightSide(p, cone);
                p = GetPolylinePointInsideOfConeAndRemoveCones(p, cone);
                steps--;
            } while (leftConeSides.Count > 0 && steps>0);
        }

        PolylinePoint GetPolylinePointInsideOfConeAndRemoveCones(PolylinePoint p, Cone cone) {
            var pn = p.NextOnPolyline;
            Point insidePoint = FindInsidePoint(p.Point, pn.Point, cone);

            if (ApproximateComparer.Close(insidePoint, p.Point)) {
                AddEdgeAndRemoveCone(cone, p.Point);
                AddEdgesAndRemoveRemainingConesByPoint(p.Point);
                //we don't move p forward here. In the next iteration we just cross [p,pn] with the new leftmost cone right side
            } else if (ApproximateComparer.Close(insidePoint, pn.Point)) {
                AddEdgeAndRemoveCone(cone, pn.Point);
                AddEdgesAndRemoveRemainingConesByPoint(pn.Point);
                p = pn;
            } else {
                p = InsertPointIntoPolylineAfter(BorderPolyline, p, insidePoint);
                AddEdgeAndRemoveCone(cone, p.Point);
                AddEdgesAndRemoveRemainingConesByPoint(p.Point);
            }
            return p;
        }

        static Point FindInsidePoint(Point leftPoint, Point rightPoint, Cone cone) {
            //            if (debug)
            //                LayoutAlgorithmSettings.Show(CurveFactory.CreateCircle(3, leftPoint),
            //                                             CurveFactory.CreateDiamond(3, 3, rightPoint),
            //                                             BorderPolyline, ExtendSegmentToZ(cone.LeftSide),
            //                                             ExtendSegmentToZ(cone.RightSide));
            return FindInsidePointBool(leftPoint, rightPoint, cone.Apex, cone.Apex + cone.LeftSideDirection,
                                       cone.Apex + cone.RightSideDirection);
        }

        static Point FindInsidePointBool(Point leftPoint, Point rightPoint, Point apex, Point leftSideConePoint,
                                         Point rightSideConePoint) {
            if (ApproximateComparer.Close(leftPoint, rightPoint))
                return leftPoint; //does not matter which one to return
            if (Point.PointIsInsideCone(leftPoint, apex, leftSideConePoint, rightSideConePoint))
                return leftPoint;

            if (Point.PointIsInsideCone(rightPoint, apex, leftSideConePoint, rightSideConePoint))
                return rightPoint;

            var m = 0.5 * (leftPoint + rightPoint);

            if (Point.PointToTheLeftOfLine(m, apex, leftSideConePoint))
                return FindInsidePointBool(m, rightPoint, apex, leftSideConePoint, rightSideConePoint);

            return FindInsidePointBool(leftPoint, m, apex, leftSideConePoint, rightSideConePoint);
        }

        

        void AddEdgesAndRemoveRemainingConesByPoint(Point point) {
            var conesToRemove = new List<Cone>();
            foreach (var leftConeSide in leftConeSides) {
                if (Point.PointToTheRightOfLineOrOnLine(point, leftConeSide.Start,
                                                        leftConeSide.Start + leftConeSide.Direction))
                    conesToRemove.Add(leftConeSide.Cone);
                else
                    break;
            }
            foreach (var cone in conesToRemove) {
                AddEdgeAndRemoveCone(cone, point);
            }
        }

        PolylinePoint FindPolylineSideIntersectingConeRightSide(PolylinePoint p, Cone cone) {
            var startPoint = p;
            var a = cone.Apex;
            var b = cone.Apex + ConeRightSideDirection;
            var pSign = GetSign(p, a, b);
            do {
                var pn = p.NextOnPolyline;
                var pnSigh = GetSign(pn, a, b);
                if (pnSigh - pSign > 0)
                    return p;
                p = pn;
                pSign = pnSigh;
                if (p == startPoint)
                    throw new InvalidOperationException();
            } while (true);
            /*
                        throw new InvalidOleVariantTypeException();
            */
        }

        static int GetSign(PolylinePoint p, Point a, Point b) {
            var d = Point.SignedDoubledTriangleArea(a, b, p.Point);
            if (d < 0)
                return 1;
            return d > 0 ? -1 : 0;
        }

#if TEST_MSAGL
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void Showside(PolylinePoint p, Point a, Point b, PolylinePoint pn) {
            ShowBothTrees(new DebugCurve(100, 1, "brown", BorderPolyline), new DebugCurve(100, 2, "blue",
                                                                                          new LineSegment(a, b)),
                          new DebugCurve(100, 2, "green",
                                         new LineSegment(
                                             pn.Point, p.Point)
                              ));
        }
#endif

        //        void CheckThatPolylineIsLegal()
        //        {
        //            var p = BorderPolyline.StartPoint;
        //            do
        //            {
        //                var pn = p.NextOnPolyline;
        //                Debug.Assert(!ApproximateComparer.Close(p.Point, pn.Point));
        //                Debug.Assert((pn.Point - p.Point)*(pn.NextOnPolyline.Point - pn.Point) > -ApproximateComparer.Tolerance);
        //                p = pn;
        //            } while (p != BorderPolyline.StartPoint);
        //        }

#if TEST_MSAGL
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void ShowBoundaryPolyline() {
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(CreateBoundaryPolyDebugCurves());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        IEnumerable<DebugCurve> CreateBoundaryPolyDebugCurves() {
            int i = 0;
            for (var p = BorderPolyline.StartPoint; p != null; p = p.Next) {
                yield return new DebugCurve(new Ellipse(1, 1, p.Point), i++);
            }
        }
#endif

        void AddEdgeAndRemoveCone(Cone cone, Point p) {
            if (Ports != null && Ports.Contains(cone.Apex))
                CreatePortEdge(cone, p);
            else
                visibilityGraph.AddEdge(cone.Apex, p);
            RemoveCone(cone);
        }

        /*********************
            A complication arises when we have overlaps. Loose obstacles become large enough to contain several
            ports. We need to avoid a situation when a port has degree more than one. 
            To avoid this situation we redirect to p every edge incoming into cone.Apex. 
            Notice that we create a new graph to keep port edges for ever 
            direction of the sweep and the above procedure just alignes the edges better.
            In the resulting graph, which contains the sum of the graphs passed to AddDirection, of course
            a port can have an incoming and outcoming edge at the same time
            *******************/

        void CreatePortEdge(Cone cone, Point p) {
            if (portEdgesGraph == null) portEdgesGraph = new VisibilityGraph();
            var coneApexVert = portEdgesGraph.FindVertex(cone.Apex);
            //all previous edges adjacent to cone.Apex 
            var edgesToFix = (coneApexVert != null)
                                 ? coneApexVert.InEdges.Concat(coneApexVert.OutEdges).ToArray()
                                 : null;
            if (edgesToFix != null)
                foreach (var edge in edgesToFix) {
                    var otherPort = (edge.Target == coneApexVert ? edge.Source : edge.Target).Point;
                    VisibilityGraph.RemoveEdge(edge);
                    portEdgesGraph.AddEdge(otherPort, p);
                }
            portEdgesGraph.AddEdge(cone.Apex, p);
        }


        internal static PolylinePoint InsertPointIntoPolylineAfter(Polyline borderPolyline, PolylinePoint insertAfter,
                                                                   Point pointToInsert) {
            PolylinePoint np;
            if (insertAfter.Next != null) {
                np = new PolylinePoint(pointToInsert) { Prev = insertAfter, Next = insertAfter.Next, Polyline = borderPolyline };
                insertAfter.Next.Prev = np;
                insertAfter.Next = np;
            } else {
                np = new PolylinePoint(pointToInsert) { Prev = insertAfter, Polyline = borderPolyline };
                insertAfter.Next = np;
                borderPolyline.EndPoint = np;
            }

            Debug.Assert(
                !(ApproximateComparer.Close(np.Point, np.PrevOnPolyline.Point) ||
                  ApproximateComparer.Close(np.Point, np.NextOnPolyline.Point)));

            borderPolyline.RequireInit();
            return np;
        }

        void ProcessEvent(SweepEvent p) {
            var vertexEvent = p as VertexEvent;
           

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
                        } else
                            ProcessPortObstacleEvent((PortObstacleEvent)p);
                        Z = GetZ(p);
                    }
                }
            }
            //Debug.Assert(TreesAreCorrect());
        }
#if TEST_MSAGL
//        protected override bool TreesAreCorrect() {
//            return TreeIsCorrect(leftConeSides) && TreeIsCorrect(rightConeSides);
//        }
//
//        bool TreeIsCorrect(RbTree<ConeSide> tree) {
//            var y = double.NegativeInfinity;
//            foreach (var t in tree) {
//                var x = coneSideComparer.IntersectionOfSegmentAndSweepLine(t);
//                var yp = x*DirectionPerp;
//                if (yp < y - ApproximateComparer.DistanceEpsilon)
//                    return false;
//                y = yp;
//            }
//            return true;
//        }
#endif
        void ProcessPortObstacleEvent(PortObstacleEvent portObstacleEvent) {
            Z = GetZ(portObstacleEvent);
            GoOverConesSeeingVertexEvent(portObstacleEvent);
            CreateConeOnVertex(portObstacleEvent);
        }


        void ProcessLeftIntersectionEvent(LeftIntersectionEvent leftIntersectionEvent) {
            if (leftIntersectionEvent.coneLeftSide.Removed == false) {
                if (Math.Abs((leftIntersectionEvent.EndVertex.Point - leftIntersectionEvent.Site) * SweepDirection) <
                    ApproximateComparer.DistanceEpsilon) {
                    //the cone is totally covered by a horizontal segment
                    RemoveCone(leftIntersectionEvent.coneLeftSide.Cone);
                } else {
                    RemoveSegFromLeftTree(leftIntersectionEvent.coneLeftSide);
                    Z = SweepDirection * leftIntersectionEvent.Site; //it is safe now to restore the order
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
                Z = SweepDirection * leftIntersectionEvent.Site;
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
#if TEST_MSAGL
            var r =
#endif
 Point.RayIntersectsRayInteriors(brokenConeSide.start, brokenConeSide.Direction, otherSide.Start,
                                                otherSide.Direction, out x);
#if TEST_MSAGL
            if (!r)
                LayoutAlgorithmSettings.ShowDebugCurves(
                    new DebugCurve(100, 0.1, "red",new LineSegment(brokenConeSide.Start, brokenConeSide.start + brokenConeSide.Direction)),
                    new DebugCurve(100,0.1, "black", new Ellipse(0.1,0.1, brokenConeSide.Start)),
                    new DebugCurve(100, 0.1, "blue",new LineSegment(otherSide.Start, otherSide.Start + otherSide.Direction)));
            Debug.Assert(r);
#endif
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
                Z = SweepDirection * rightIntersectionEvent.Site;
                var rightSide = new BrokenConeSide(
                    rightIntersectionEvent.Site,
                    rightIntersectionEvent.EndVertex, rightIntersectionEvent.coneRightSide);
                InsertToTree(rightConeSides, rightSide);
                rightIntersectionEvent.coneRightSide.Cone.RightSide = rightSide;
                LookForIntersectionOfObstacleSideAndRightConeSide(rightIntersectionEvent.Site,
                                                                  rightIntersectionEvent.EndVertex);

                TryCreateConeClosureForRightSide(rightSide);
            } else
                Z = SweepDirection * rightIntersectionEvent.Site;
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
                                       SweepDirection * leftPoint > SweepDirection * rightPoint
                                           ? leftConeSides
                                           : rightConeSides);
        }

        void CloseConesCoveredBySegment(Point leftPoint, Point rightPoint, RbTree<ConeSide> tree) {
            var node = tree.FindFirst(
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


            foreach (var cone in conesToRemove)
                RemoveCone(cone);
        }

        void ProcessVertexEvent(VertexEvent vertexEvent) {
            Z = GetZ(vertexEvent);
            GoOverConesSeeingVertexEvent(vertexEvent);
            AddConeAndEnqueueEvents(vertexEvent);
        }

#if TEST_MSAGL
        // ReSharper disable UnusedMember.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static Ellipse EllipseOnVert(SweepEvent vertexEvent) {
            // ReSharper restore UnusedMember.Local
            return new Ellipse(5, 5, vertexEvent.Site);
        }

        // ReSharper disable UnusedMember.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static Ellipse EllipseOnPolylinePoint(PolylinePoint pp) {
            // ReSharper restore UnusedMember.Local
            return EllipseOnPolylinePoint(pp, 5);
        }
        // ReSharper disable UnusedMember.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static Ellipse EllipseOnPolylinePoint(PolylinePoint pp, double i)
            // ReSharper restore UnusedMember.Local
        {
            return new Ellipse(i, i, pp.Point);
        }

        static ICurve Diamond(Point p) {
            return CurveFactory.CreateDiamond(2, 2, p);
        }

        // ReSharper disable UnusedMember.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization",
            "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.WriteLine(System.String)"
            ),
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void CheckConsistency() {
            // ReSharper restore UnusedMember.Local
            foreach (var s in rightConeSides) {
                coneSideComparer. SetOperand(s);
            }
            foreach (var s in leftConeSides) {
                coneSideComparer.SetOperand(s);
                if (!rightConeSides.Contains(s.Cone.RightSide)) {
                    PrintOutRightSegTree();
                    PrintOutLeftSegTree();

                    ShowLeftTree();
                    ShowRightTree();
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void ShowRightTree(params ICurve[] curves) {
            var l = Obstacles.Select(p => new DebugCurve(100, 5, "green", p)).ToList();
            l.AddRange(rightConeSides.Select(s => new DebugCurve(100, 5, "blue",ExtendSegmentToZ(s))));

            //            foreach (VisibilityEdge edge in visibilityGraph.Edges)
            //                l.Add(BezierOnEdge(edge));

            l.AddRange(curves.Select(c => new DebugCurve(100, 5, "brown", c)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "curves"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void ShowBothTrees(params DebugCurve[] curves) {
            var l = Obstacles.Select(p => new DebugCurve(100, 5, "green", p)).ToList();
            l.AddRange(leftConeSides.Select(s => new DebugCurve(ExtendSegmentToZ(s))));
            l .AddRange(rightConeSides.Select(s => new DebugCurve(ExtendSegmentToZ(s))));

            //            foreach (VisibilityEdge edge in visibilityGraph.Edges)
            //                l.Add(BezierOnEdge(edge));

            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        }

        void ShowLeftTree(params ICurve[] curves) {
            var l = Obstacles.Select(p => new DebugCurve(100, 0.01,"green", p)).ToList();
            var range = new RealNumberSpan();
            var ellipseSize = 0.01;

            foreach (var s in leftConeSides) {
                var curve = ExtendSegmentToZ(s);
                range.AddValue(curve.Start*DirectionPerp);
                range.AddValue(curve.End * DirectionPerp);
                l.Add(new DebugCurve(100, 0.1, "red", curve));
                l.Add(new DebugCurve(200,0.1, "black", new Ellipse(ellipseSize,ellipseSize, curve.End)));
                ellipseSize += 2;
            }
            l.Add(DebugSweepLine(range));

            //            foreach (VisibilityEdge edge in visibilityGraph.Edges)
            //                l.Add(BezierOnEdge(edge));

            l.AddRange(curves.Select(c => new DebugCurve(100, 0.5, "brown", c)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        }

        DebugCurve DebugSweepLine(RealNumberSpan range) {
            var ls = new LineSegment(Z * SweepDirection + DirectionPerp * range.Min, Z * SweepDirection + DirectionPerp * range.Max);
            return new DebugCurve(100,0.1,"magenta", ls);
        }
#endif

        void AddConeAndEnqueueEvents(VertexEvent vertexEvent) {
            var leftVertexEvent = vertexEvent as LeftVertexEvent;
            if (leftVertexEvent != null) {
                PolylinePoint nextPoint = vertexEvent.Vertex.NextOnPolyline;
                CloseConesAddConeAtLeftVertex(leftVertexEvent, nextPoint);
            } else {
                var rightVertexEvent = vertexEvent as RightVertexEvent;
                if (rightVertexEvent != null) {
                    PolylinePoint nextPoint = vertexEvent.Vertex.PrevOnPolyline;
                    CloseConesAddConeAtRightVertex(rightVertexEvent, nextPoint);
                } else {
                    CloseConesAddConeAtLeftVertex(vertexEvent, vertexEvent.Vertex.NextOnPolyline);
                    CloseConesAddConeAtRightVertex(vertexEvent, vertexEvent.Vertex.PrevOnPolyline);
                }
            }
        }

        void CloseConesAddConeAtRightVertex(VertexEvent rightVertexEvent,
                                            PolylinePoint nextVertex) {
            var prevSite = rightVertexEvent.Vertex.NextOnPolyline.Point;
            var prevZ = prevSite*SweepDirection;
            if (ApproximateComparer.Close(prevZ, Z))
                RemoveConesClosedBySegment(prevSite, rightVertexEvent.Vertex.Point);

            var site = rightVertexEvent.Site;
            var coneLp = site + ConeLeftSideDirection;
            var coneRp = site + ConeRightSideDirection;
            var nextSite = nextVertex.Point;
            //SugiyamaLayoutSettings.Show(new LineSegment(site, coneLP), new LineSegment(site, coneRP), new LineSegment(site, nextSite));
            //try to remove the right side
            if ((site - prevSite)*SweepDirection > ApproximateComparer.DistanceEpsilon)
                RemoveRightSide(new RightObstacleSide(rightVertexEvent.Vertex.NextOnPolyline));
            if ((site - nextVertex.Point) * SweepDirection > ApproximateComparer.DistanceEpsilon)
                RemoveLeftSide(new LeftObstacleSide(nextVertex));

            if (GetZ(nextSite) + ApproximateComparer.DistanceEpsilon < GetZ(rightVertexEvent))
                  CreateConeOnVertex(rightVertexEvent);   
            if (!Point.PointToTheRightOfLineOrOnLine(nextSite, site, coneLp)) {
                //if (angle <= -coneAngle / 2) {
                CreateConeOnVertex(rightVertexEvent);
                if (Point.PointToTheLeftOfLineOrOnLine(nextSite + DirectionPerp, nextSite, site))
                    EnqueueEvent(new RightVertexEvent(nextVertex));
                //  TryEnqueueRighVertexEvent(nextVertex);
            } else if (Point.PointToTheLeftOfLineOrOnLine(nextSite, site, coneRp))
                //if (angle < coneAngle / 2) {
                CaseToTheLeftOfLineOrOnLineConeRp(rightVertexEvent, nextVertex);
            else {
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
            var cone = new Cone(rightVertexEvent.Vertex.Point, this);
            var obstacleSideSeg = new BrokenConeSide(cone.Apex, nextVertex, new ConeLeftSide(cone));
            cone.LeftSide = obstacleSideSeg;
            cone.RightSide = new ConeRightSide(cone);
            var rnode = InsertToTree(rightConeSides, cone.RightSide);
            LookForIntersectionWithConeRightSide(rnode);
            var lnode = InsertToTree(leftConeSides, cone.LeftSide);
            FixConeLeftSideIntersections(obstacleSideSeg, lnode);
            if ((nextVertex.Point - rightVertexEvent.Site) * SweepDirection > ApproximateComparer.DistanceEpsilon)
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        RightIntersectionEvent CreateRightIntersectionEvent(ConeRightSide coneRightSide, Point intersection,
                                                            PolylinePoint obstacleSideVertex) {
            Debug.Assert(Math.Abs((obstacleSideVertex.Point - intersection) * SweepDirection) > 0);
            return new RightIntersectionEvent(coneRightSide,
                                              intersection, obstacleSideVertex);
        }

        RBNode<ConeSide> GetLastNodeToTheLeftOfPointInRightSegmentTree(Point obstacleSideStart) {
            return rightConeSides.FindLast(
                s => PointIsToTheRightOfSegment(obstacleSideStart, s));
        }

        void LookForIntersectionOfObstacleSideAndLeftConeSide(Point obstacleSideStart,
                                                              PolylinePoint obstacleSideVertex) {
            var node = GetFirstNodeToTheRightOfPoint(obstacleSideStart);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization",
            "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.WriteLine(System.String)"
            )]
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


        void FixConeLeftSideIntersections(BrokenConeSide leftSide, RBNode<ConeSide> rbNode) {
            //the first intersection can happen only with succesors of leftSide
            Debug.Assert(rbNode != null);
            do { //this loop usually works only once
                rbNode = leftConeSides.Next(rbNode);
            } while (rbNode != null &&  Point.PointToTheRightOfLineOrOnLine(leftSide.Start, rbNode.Item.Start, rbNode.Item.Start+rbNode.Item.Direction)); 

            if (rbNode != null) {
                Point intersection;
                var seg = rbNode.Item as ConeLeftSide;
                if (seg != null &&
                    Point.IntervalIntersectsRay(leftSide.Start, leftSide.End, seg.Start, seg.Direction, out intersection)) {
                    EnqueueEvent(new LeftIntersectionEvent(seg, intersection, leftSide.EndVertex));
                    //Show(CurveFactory.CreateDiamond(3, 3, intersection));
                }
            }
        }


        RBNode<ConeSide> InsertToTree(RbTree<ConeSide> tree, ConeSide coneSide) {
            Debug.Assert(coneSide.Direction * SweepDirection > 0);
            coneSideComparer.SetOperand(coneSide);
            return tree.Insert(coneSide);
        }


        void CloseConesAddConeAtLeftVertex(VertexEvent leftVertexEvent, PolylinePoint nextVertex) {
            //close segments first
            Point prevSite = leftVertexEvent.Vertex.PrevOnPolyline.Point;
            double prevZ = prevSite * SweepDirection;
            if (ApproximateComparer.Close(prevZ, Z) && (prevSite - leftVertexEvent.Site) * DirectionPerp > 0) {
                //Show(
                //    new Ellipse(1, 1, prevSite),
                //    CurveFactory.CreateBox(2, 2, leftVertexEvent.Vertex.Point));
                RemoveConesClosedBySegment(leftVertexEvent.Vertex.Point, prevSite);
            }

            var site = leftVertexEvent.Site;
            var coneLp = site + ConeLeftSideDirection;
            var coneRp = site + ConeRightSideDirection;
            var nextSite = nextVertex.Point;
            // SugiyamaLayoutSettings.Show(new LineSegment(site, coneLP), new LineSegment(site, coneRP), new LineSegment(site, nextSite));

            if ((site - prevSite) * SweepDirection > ApproximateComparer.DistanceEpsilon)
                RemoveLeftSide(new LeftObstacleSide(leftVertexEvent.Vertex.PrevOnPolyline));
           

            var nextDelZ = GetZ(nextSite) - Z;
            if(nextDelZ<-ApproximateComparer.DistanceEpsilon)
                RemoveRightSide(new RightObstacleSide(nextVertex));

            if (nextDelZ < -ApproximateComparer.DistanceEpsilon ||
                ApproximateComparer.Close(nextDelZ, 0) && (nextSite - leftVertexEvent.Site) * DirectionPerp > 0) {
                //if (angle > Math.PI / 2)
                CreateConeOnVertex(leftVertexEvent); //it is the last left vertex on this obstacle
                
            } else if (!Point.PointToTheLeftOfLineOrOnLine(nextSite, site, coneRp)) {
                //if (angle >= coneAngle / 2) {
                CreateConeOnVertex(leftVertexEvent);
                EnqueueEvent(new LeftVertexEvent(nextVertex));
                //we schedule LeftVertexEvent for a vertex with horizontal segment to the left on the top of the obstace
            } else if (!Point.PointToTheLeftOfLineOrOnLine(nextSite, site, coneLp)) {
                //if (angle >= -coneAngle / 2) {
                //we cannot completely obscure the cone here
                EnqueueEvent(new LeftVertexEvent(nextVertex));
                //the obstacle side is inside of the cone
                //we need to create an obstacle right side segment instead of the cone side
                var cone = new Cone(leftVertexEvent.Vertex.Point, this);
                var rightSide = new BrokenConeSide(leftVertexEvent.Vertex.Point, nextVertex,
                                                   new ConeRightSide(cone));
                cone.RightSide = rightSide;
                cone.LeftSide = new ConeLeftSide(cone);
                LookForIntersectionWithConeLeftSide(InsertToTree(leftConeSides, cone.LeftSide));
                var rbNode = InsertToTree(rightConeSides, rightSide);
                FixConeRightSideIntersections(rightSide, rbNode);
                if ((nextVertex.Point - leftVertexEvent.Site) * SweepDirection > ApproximateComparer.DistanceEpsilon)
                    InsertLeftSide(new LeftObstacleSide(leftVertexEvent.Vertex));
            } else {
                EnqueueEvent(new LeftVertexEvent(nextVertex));
                if ((nextVertex.Point - leftVertexEvent.Site) * SweepDirection > ApproximateComparer.DistanceEpsilon) {
                    //if( angle >- Pi/2
                    // Debug.Assert(angle > -Math.PI / 2);
                    LookForIntersectionOfObstacleSideAndRightConeSide(leftVertexEvent.Site, nextVertex);
                    InsertLeftSide(new LeftObstacleSide(leftVertexEvent.Vertex));
                }
            }
        }

        void RemoveCone(Cone cone)
        {
            // the following should not happen if the containment hierarchy is correct.  
            // If containment is not correct it still should not result in a fatal error, just a funny looking route.
            // Debug.Assert(cone.Removed == false);
            cone.Removed = true;
            RemoveSegFromLeftTree(cone.LeftSide);
            RemoveSegFromRightTree(cone.RightSide);
        }


        void RemoveSegFromRightTree(ConeSide coneSide) {
            //   ShowRightTree();
            Debug.Assert(coneSide.Removed == false);
            coneSideComparer.SetOperand(coneSide);
            var b = rightConeSides.Remove(coneSide);
            coneSide.Removed = true;
            if (b == null) {
                var tmpZ = Z;
                Z = Math.Max(GetZ(coneSide.Start), Z - 0.01);
                //we need to return to the past a little bit when the order was still correc
                coneSideComparer.SetOperand(coneSide);
                b = rightConeSides.Remove(coneSide);
                Z = tmpZ;

#if TEST_MSAGL
                if (b == null) {
                    PrintOutRightSegTree();
                }
#endif
            }
        }

        void RemoveSegFromLeftTree(ConeSide coneSide) {
            Debug.Assert(coneSide.Removed == false);
            coneSide.Removed = true;
            coneSideComparer.SetOperand(coneSide);
            var b = leftConeSides.Remove(coneSide);

            if (b == null) {
                var tmpZ = Z;
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
        /// <param name="rightSide"></param>
        /// <param name="rbNode">represents a node of the right cone side</param>
        void FixConeRightSideIntersections(BrokenConeSide rightSide, RBNode<ConeSide> rbNode) {
            //the first intersection can happen only with predecessors of rightSide
            Debug.Assert(rbNode != null);
            do { //this loop usually works only once
                rbNode = rightConeSides.Previous(rbNode);
            } while (rbNode != null && Point.PointToTheLeftOfLineOrOnLine(rightSide.Start, rbNode.Item.Start, rbNode.Item.Start + rbNode.Item.Direction));  
            if (rbNode != null) {
                Point intersection;
                var seg = rbNode.Item as ConeRightSide;
                if (seg != null &&
                    Point.IntervalIntersectsRay(rightSide.Start, rightSide.End, seg.Start, seg.Direction,
                                                out intersection)) {
                    EnqueueEvent(CreateRightIntersectionEvent(seg, intersection, rightSide.EndVertex));
                    // Show(CurveFactory.CreateDiamond(3, 3, intersection));
                }
            }
        }


        void CreateConeOnVertex(SweepEvent sweepEvent) {
            var cone = new Cone(sweepEvent.Site, this);
#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=368
            //SharpKit/Colin - property assignment values not retained
            cone.LeftSide = new ConeLeftSide(cone);
            cone.RightSide = new ConeRightSide(cone);

            var leftNode = InsertToTree(leftConeSides, cone.LeftSide);
            var rightNode = InsertToTree(rightConeSides, cone.RightSide);
#else
            var leftNode = InsertToTree(leftConeSides, cone.LeftSide = new ConeLeftSide(cone));
            var rightNode = InsertToTree(rightConeSides, cone.RightSide = new ConeRightSide(cone));
#endif       
            LookForIntersectionWithConeRightSide(rightNode);
            LookForIntersectionWithConeLeftSide(leftNode);
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
                var seg = (BrokenConeSide)leftNode.Item;
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
                var seg = (BrokenConeSide)rightNode.Item;
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


#if TEST_MSAGL
        internal void Show(params ICurve[] curves) {
            var l = Obstacles.Select(o => new DebugCurve(100, 0.1, "blue", o)).ToList();

            foreach (var s in rightConeSides) {
                l.Add(new DebugCurve(0.5, "brown", ExtendSegmentToZ(s)));
                if (s is BrokenConeSide)
                    l.Add(new DebugCurve("brown", Diamond(s.Start)));
                l.Add(new DebugCurve(0.5, "green",
                                     ExtendSegmentToZ(s.Cone.LeftSide)));
                if (s.Cone.LeftSide is BrokenConeSide)
                    l.Add(new DebugCurve("green", Diamond(s.Cone.LeftSide.Start)));
            }

//            l.AddRange(
//                visibilityGraph.Edges.Select(
//                    edge => new DebugCurve(0.2, "maroon", new LineSegment(edge.SourcePoint, edge.TargetPoint))));

            l.AddRange(curves.Select(c => new DebugCurve("red", c)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static CubicBezierSegment BezierOnEdge(VisibilityEdge edge) {
            return new CubicBezierSegment(edge.SourcePoint, 2.0 / 3.0 * edge.SourcePoint + 1.0 / 3.0 * edge.TargetPoint,
                                          1.0 / 3.0 * edge.SourcePoint + 2.0 / 3.0 * edge.TargetPoint, edge.TargetPoint);
        }

        internal ICurve ExtendSegmentToZ(ConeSide segment) {
            double den = segment.Direction * SweepDirection;
            Debug.Assert(Math.Abs(den) > ApproximateComparer.DistanceEpsilon);
            double t = (Z + 40 - segment.Start * SweepDirection) / den;
            return new LineSegment(segment.Start, segment.Start + segment.Direction * t);
        }

        internal ICurve ExtendSegmentToZPlus1(ConeSide segment) {
            double den = segment.Direction * SweepDirection;
            Debug.Assert(Math.Abs(den) > ApproximateComparer.DistanceEpsilon);
            double t = (Z + 1 - segment.Start * SweepDirection) / den;

            return new LineSegment(segment.Start, segment.Start + segment.Direction * t);
        }
#endif

        //        static int count;
        void GoOverConesSeeingVertexEvent(SweepEvent vertexEvent) {
            var rbNode = FindFirstSegmentInTheRightTreeNotToTheLeftOfVertex(vertexEvent);

            if (rbNode == null) return;
            var coneRightSide = rbNode.Item;
            var cone = coneRightSide.Cone;
            var leftConeSide = cone.LeftSide;
            if (VertexIsToTheLeftOfSegment(vertexEvent, leftConeSide)) return;
            var visibleCones = new List<Cone> { cone };
            coneSideComparer.SetOperand(leftConeSide);
            rbNode = leftConeSides.Find(leftConeSide);

            if (rbNode == null) {
                var tmpZ = Z;
                Z = Math.Max(GetZ(leftConeSide.Start), PreviousZ);
                //we need to return to the past a little bit when the order was still correct
                coneSideComparer.SetOperand(leftConeSide);
                rbNode = leftConeSides.Find(leftConeSide);
                Z = tmpZ;
#if TEST_MSAGL
//                if (rbNode == null) {
                    //GeometryGraph gg = CreateGraphFromObstacles();
                    //gg.Save("c:\\tmp\\bug");
//                    PrintOutLeftSegTree();
//                    System.Diagnostics.Debug.WriteLine(leftConeSide);
//                    ShowLeftTree(new Ellipse(3, 3, vertexEvent.Site));
//                    ShowRightTree(new Ellipse(3, 3, vertexEvent.Site));
//                }
#endif
            }
            // the following should not happen if the containment hierarchy is correct.  
            // If containment is not correct it still should not result in a fatal error, just a funny looking route.
            // Debug.Assert(rbNode!=null);

            if (rbNode == null) {//it is an emergency measure and should not happen                
                rbNode = GetRbNodeEmergency(rbNode, leftConeSide);
                if (rbNode == null)
                    return; // the cone is not there! and it is a bug
            }

            rbNode = leftConeSides.Next(rbNode);
            while (rbNode != null && !VertexIsToTheLeftOfSegment(vertexEvent, rbNode.Item)) {
                visibleCones.Add(rbNode.Item.Cone);
                rbNode = leftConeSides.Next(rbNode);
            }

            //Show(new Ellipse(1, 1, vertexEvent.Site));
            foreach (var visCone in visibleCones)
                AddEdgeAndRemoveCone(visCone, vertexEvent.Site);
        }

        RBNode<ConeSide> GetRbNodeEmergency(RBNode<ConeSide> rbNode, ConeSide leftConeSide) {
            for (var node = leftConeSides.TreeMinimum(); node != null; node = leftConeSides.Next(node))
                if (node.Item == leftConeSide) {
                    rbNode = node;
                    break;
                }
            return rbNode;
        }

#if TEST_MSAGL
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider",
            MessageId = "System.Int32.ToString")]
        internal static GeometryGraph CreateGraphFromObstacles(IEnumerable<Polyline> obstacles) {
            var gg = new GeometryGraph();
            foreach (var ob in obstacles) {
                gg.Nodes.Add(new Node(ob.ToCurve()));
            }
            return gg;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.WriteLine(System.String,System.Object,System.Object)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization",
            "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.WriteLine(System.String)"
            )]
        void PrintOutLeftSegTree() {
            System.Diagnostics.Debug.WriteLine("Left cone segments########");
            foreach (var t in leftConeSides) {
                var x = coneSideComparer.IntersectionOfSegmentAndSweepLine(t);

                System.Diagnostics.Debug.WriteLine("{0} x={1}", t, x*DirectionPerp );                
            }
            System.Diagnostics.Debug.WriteLine("##########end of left cone segments");
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
            if (SweepDirection * (vertexEvent.Site - vertexEvent.Vertex.PrevOnPolyline.Point) > ApproximateComparer.Tolerance)
                return;//otherwise we enqueue the vertex twice; once as a LeftVertexEvent and once as a RightVertexEvent
            base.EnqueueEvent(vertexEvent);
        }

    }
}