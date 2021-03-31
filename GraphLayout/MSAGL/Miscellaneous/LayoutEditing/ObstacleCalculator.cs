using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Prototype.LayoutEditing {
    /// <summary>
    /// calculations with obstacles
    /// </summary>
    public class ObstacleCalculator {
        List<Polyline> looseObstacles = new List<Polyline>();
        Set<ICurve> portObstacles = new Set<ICurve>();
        RectangleNode<Polyline, Point> rootOfLooseHierarchy;
        RectangleNode<Polyline, Point> rootOfTightHierarachy;
        RouterBetweenTwoNodes router;
        LineSegment sourceFilterLine;
        LineSegment targetFilterLine;
        Set<Polyline> tightObstacles = new Set<Polyline>();

        internal ObstacleCalculator(RouterBetweenTwoNodes router) {
            this.router = router;
        }

        internal Set<Polyline> TightObstacles {
            get { return tightObstacles; }
            //            set { tightObstacles = value; }
        }

        internal List<Polyline> LooseObstacles {
            get { return looseObstacles; }
            //            set { looseObstacles = value; }
        }

        internal RectangleNode<Polyline, Point> RootOfTightHierararchy {
            get { return rootOfTightHierarachy; }
            private set { rootOfTightHierarachy = value; }
        }

        RectangleNode<Polyline, Point> RootOfLooseHierarchy {
            get { return rootOfLooseHierarchy; }
            set { rootOfLooseHierarchy = value; }
        }

        internal LineSegment SourceFilterLine {
            get { return sourceFilterLine; }
        }

        internal LineSegment TargetFilterLine {
            get { return targetFilterLine; }
            //            set { targetFilterLine = value; }
        }

        double EnteringAngle {
            get { return router.EnteringAngleBound * Math.PI / 180; }
        }

        /// <summary>
        /// There are two sets of obstacles: tight and loose.
        /// We route the shortest path between loose obstacles, and then beautify it while only taking into account tight obstacles
        /// </summary>
        /// <returns></returns>
        internal void Calculate() {
            CreateTightObstacles();
            CreateLooseObstacles();
        }

        void CreateLooseObstacles() {
            RootOfLooseHierarchy = RootOfTightHierararchy.Clone();

            TraverseHierarchy(RootOfLooseHierarchy, delegate(RectangleNode<Polyline, Point> node) {
                if (node.UserData != null) {
                    Polyline tightPolyline = node.UserData;
                    double distance =
                        FindMaxPaddingForTightPolyline(tightPolyline);
                    LooseObstacles.Add(
                        node.UserData =
                        LoosePolylineWithFewCorners(tightPolyline,
                                                    Math.Min(
                                                        router.LoosePadding,
                                                        distance * 0.3)));
                    node.Rectangle = node.UserData.BoundingBox;
                    InteractiveObstacleCalculator.UpdateRectsForParents(node);
                }
            });
        }


        //internal void ShowRectangleNodesHierarchy(RectangleNode<Polyline, Point> node) {
        //    List<ICurve> ls = new List<ICurve>();
        //    FillList(ls, node);
        //    SugiyamaLayoutSettings.Show(ls.ToArray());
        //}

        //internal void FillList(List<ICurve> ls, RectangleNode<Polyline, Point> node) {
        //    if (node == null)
        //        return;
        //    if (node.UserData != null)
        //        ls.Add(node.UserData);
        //    else {
        //        FillList(ls, node.Left);
        //        FillList(ls, node.Right);
        //    }
        //}


        static void TraverseHierarchy(RectangleNode<Polyline, Point> node, Visitor visitor) {
            visitor(node);
            if (node.Left != null)
                TraverseHierarchy(node.Left, visitor);
            if (node.Right != null)
                TraverseHierarchy(node.Right, visitor);
        }

        void CreateTightObstacles() {
            CreateInitialTightObstacles();
            List<Set<Polyline>> overlappingPolylineSets;
            do {
                RemoveTightObstaclesOverlappingPortTightObstacles();
                CalculateTightHierarchy();
                overlappingPolylineSets = GetOverlappingSets();
                foreach (var overlappingSet in overlappingPolylineSets)
                    InsertOverlappingSet(overlappingSet);
            } while (overlappingPolylineSets.Count > 0);
        }

        void RemoveTightObstaclesOverlappingPortTightObstacles() {
            var toRemove = new List<Polyline>();
            foreach (Polyline poly in TightObstaclesMinusPortObstacles())
                foreach (ICurve portObstacle in portObstacles)
                    if (poly.BoundingBox.Intersects(portObstacle.BoundingBox))
                        if (Curve.GetAllIntersections(poly, portObstacle, false).Count > 0 ||
                            OneCurveLiesInsideOfOther(poly, portObstacle))
                            toRemove.Add(poly);

            foreach (Polyline poly in toRemove)
                TightObstacles.Remove(poly);
        }

        IEnumerable<Polyline> TightObstaclesMinusPortObstacles() {
            foreach (Polyline p in TightObstacles)
                if (portObstacles.Contains(p) == false)
                    yield return p;
        }

        void InsertOverlappingSet(Set<Polyline> overlappingSet) {
            foreach (Polyline p in overlappingSet)
                tightObstacles.Remove(p);

            var hull = new Polyline();
            foreach (Point p in ConvexHull.CalculateConvexHull(EnumerateOverSetOfPolylines(overlappingSet)))
                hull.AddPoint(p);
            hull.Closed = true;

            //debug 
            //List<ICurve> ls=new List<ICurve>();
            //foreach(Polyline p in overlappingSet)
            //    ls.Add(p);

            //ls.Add(hull);
            //SugiyamaLayoutSettings.Show(ls.ToArray());
            //end of debug

            tightObstacles.Insert(hull);
        }

        IEnumerable<Point> EnumerateOverSetOfPolylines(Set<Polyline> pp) {
            foreach (Polyline poly in pp)
                foreach (Point p in poly)
                    yield return p;
        }

        List<Set<Polyline>> GetOverlappingSets() {
            PolylineGraph overlapGraph = CalculateOverlapGraph();
            return ConnectedComponents(overlapGraph);
        }

        static List<Set<Polyline>> ConnectedComponents(PolylineGraph overlapGraph) {
            var list = new List<Set<Polyline>>();
            var processedPolylines = new Set<Polyline>();
            foreach (Polyline poly in overlapGraph.Nodes) {
                if (!processedPolylines.Contains(poly)) {
                    Set<Polyline> component = GetComponent(poly, overlapGraph);
                    if (component.Count > 1)
                        list.Add(component);
                    processedPolylines += component;
                }
            }
            return list;
        }

        static Set<Polyline> GetComponent(Polyline poly, PolylineGraph graph) {
            var ret = new Set<Polyline>();
            ret.Insert(poly);
            var queue = new Queue<Polyline>();
            queue.Enqueue(poly);
            while (queue.Count > 0) {
                foreach (Polyline p in graph.Descendents(queue.Dequeue())) {
                    if (!ret.Contains(p)) {
                        queue.Enqueue(p);
                        ret.Insert(p);
                    }
                }
            }
            return ret;
        }

        PolylineGraph CalculateOverlapGraph() {
            var graph = new PolylineGraph();
            CreateEdgesUnderTwoNodes(rootOfTightHierarachy, rootOfTightHierarachy, graph);
            return graph;
        }

        void CalculateTightHierarchy() {
            var rectNodes = new List<RectangleNode<Polyline, Point>>();
            foreach (Polyline polyline in TightObstacles)
                rectNodes.Add(CreateRectNodeOfPolyline(polyline));
            RootOfTightHierararchy = RectangleNode<Polyline, Point>.CreateRectangleNodeOnListOfNodes(rectNodes);
        }


        static RectangleNode<Polyline, Point> CreateRectNodeOfPolyline(Polyline polyline) {
            return new RectangleNode<Polyline, Point>(polyline, (polyline as ICurve).BoundingBox);
        }


        void CreateEdgesUnderTwoNodes(RectangleNode<Polyline, Point> a, RectangleNode<Polyline, Point> b,
                                      PolylineGraph overlapGraph) {
            //if (a.GetHashCode() < b.GetHashCode())
            //    return;

            Debug.Assert((a.UserData == null && a.Left != null && a.Right != null) ||
                         (a.UserData != null && a.Left == null && a.Right == null));
            Debug.Assert((b.UserData == null && b.Left != null && b.Right != null) ||
                         (b.UserData != null && b.Left == null && b.Right == null));
            if (a.Rectangle.Intersects(b.Rectangle)) {
                if (a.UserData != null) {
                    if (b.UserData != null) {
                        if (a.UserData != b.UserData)
                            if (Curve.GetAllIntersections(a.UserData, b.UserData, false).Count > 0 ||
                                OneCurveLiesInsideOfOther(a.UserData, b.UserData)) {
                                overlapGraph.AddEdge(a.UserData, b.UserData);
                                overlapGraph.AddEdge(b.UserData, a.UserData);
                            }
                    } else {
                        CreateEdgesUnderTwoNodes(a, b.Left, overlapGraph);
                        CreateEdgesUnderTwoNodes(a, b.Right, overlapGraph);
                    }
                } else if (b.UserData != null) {
                    CreateEdgesUnderTwoNodes(b, a.Left, overlapGraph);
                    CreateEdgesUnderTwoNodes(b, a.Right, overlapGraph);
                } else {
                    CreateEdgesUnderTwoNodes(a.Left, b.Left, overlapGraph);
                    CreateEdgesUnderTwoNodes(a.Left, b.Right, overlapGraph);
                    CreateEdgesUnderTwoNodes(a.Right, b.Left, overlapGraph);
                    CreateEdgesUnderTwoNodes(a.Right, b.Right, overlapGraph);
                }
            }
        }


        static bool OneCurveLiesInsideOfOther(ICurve polyA, ICurve polyB) {
            return (Curve.PointRelativeToCurveLocation(polyA.Start, polyB) != PointLocation.Outside ||
                    Curve.PointRelativeToCurveLocation(polyB.Start, polyA) != PointLocation.Outside);
        }


        /// <summary>
        /// 
        /// </summary>
        void CreateInitialTightObstacles() {
            tightObstacles = new Set<Polyline>();
            foreach (Node node in router.Graph.Nodes) {
                if (node == router.Source)
                    CreatePortObstacles(router.Source, router.SourcePort, out sourceFilterLine);
                else if (node == router.Target)
                    CreatePortObstacles(router.Target, router.TargetPort, out targetFilterLine);
                else {
                    TightObstacles.Insert(PaddedPolylineBoundaryOfNode(node, router.Padding));
                }
            }
        }


        void CreatePortObstacles(Node node, Port port, out LineSegment filterLine) {
            var bp = port as CurvePort;
            if (bp != null) {
                var padding = router.Padding;
                var closedCurve = node.BoundaryCurve;
                Curve paddingCurve = GetPaddedPolyline(closedCurve, padding).ToCurve();
                Point portPoint = node.BoundaryCurve[bp.Parameter];
                double length = node.BoundaryCurve.BoundingBox.Width + node.BoundaryCurve.BoundingBox.Height;
                double leftTipPar = GetLeftTipParam(bp.Parameter, portPoint, paddingCurve, node, length);
                double rightTipPar = GetRightTipParam(bp.Parameter, portPoint, paddingCurve, node, length);
                paddingCurve = TrimCurve(paddingCurve, rightTipPar, leftTipPar);
                //a simplifying hack here. I know that the parameter start from 0 and advances by 1 on every segment                
                int n = paddingCurve.Segments.Count / 2;
                Debug.Assert(n > 0);
                Curve rightChunk = TrimCurve(paddingCurve, 0, n);
                Curve leftChunk = TrimCurve(paddingCurve, n + 0.8, paddingCurve.ParEnd);
                filterLine = new LineSegment(0.5 * (leftChunk.Start + leftChunk.End),
                                             0.5 * (rightChunk.Start + rightChunk.End));
                Polyline pol = Polyline.PolylineFromCurve(leftChunk);
                pol.Closed = true;
                portObstacles.Insert(pol);
                TightObstacles.Insert(pol);
                pol = Polyline.PolylineFromCurve(rightChunk);
                pol.Closed = true;
                portObstacles.Insert(pol);
                TightObstacles.Insert(pol);
            } else {
                filterLine = null;
                portObstacles.Insert(node.BoundaryCurve);
            }
        }

        ///<summary>
        ///</summary>
        ///<param name="closedCurve"></param>
        ///<param name="padding"></param>
        ///<returns></returns>
        static public Polyline GetPaddedPolyline(ICurve closedCurve, double padding) {
            return InteractiveObstacleCalculator.CreatePaddedPolyline(Curve.PolylineAroundClosedCurve(closedCurve), padding);
        }



        static Curve TrimCurve(Curve curve, double u, double v) {
            Debug.Assert(u >= curve.ParStart && u <= curve.ParEnd);
            Debug.Assert(v >= curve.ParStart && v <= curve.ParEnd);
            if (u < v)
                return curve.Trim(u, v) as Curve;

            var c = new Curve();
            c.AddSegment(curve.Trim(u, curve.ParEnd) as Curve);
            c.AddSegment(curve.Trim(curve.ParStart, v) as Curve);
            return c;
        }

        double GetRightTipParam(double portParam, Point portPoint, Curve paddingCurve, Node node, double length) {
            bool curveIsClockwise = InteractiveObstacleCalculator.CurveIsClockwise(node.BoundaryCurve, node.Center);
            Point tan = curveIsClockwise
                            ? node.BoundaryCurve.RightDerivative(portParam)
                            : -node.BoundaryCurve.LeftDerivative(portParam);
            tan = (tan.Normalize() * length).Rotate(EnteringAngle);
            IList<IntersectionInfo> xs = Curve.GetAllIntersections(paddingCurve,
                                                                   new LineSegment(portPoint, portPoint + tan), true);
            Debug.Assert(xs.Count == 1);
            return xs[0].Par0;
        }

        double GetLeftTipParam(double portParam, Point portPoint, Curve paddingCurve, Node node, double length) {
            bool curveIsClockwise = InteractiveObstacleCalculator.CurveIsClockwise(node.BoundaryCurve, node.Center);
            Point tan = curveIsClockwise
                            ? node.BoundaryCurve.LeftDerivative(portParam)
                            : -node.BoundaryCurve.RightDerivative(portParam);
            tan = ((-tan.Normalize()) * length).Rotate(-EnteringAngle);
            IList<IntersectionInfo> xs = Curve.GetAllIntersections(paddingCurve,
                                                                   new LineSegment(portPoint, portPoint + tan), true);
            Debug.Assert(xs.Count == 1);
            return xs[0].Par0;
        }


        /// <summary>
        /// Creates a padded polyline boundary of the node. The polyline offsets at least as the padding from the node boundary.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        static Polyline PaddedPolylineBoundaryOfNode(Node node, double padding) {
            return InteractiveObstacleCalculator.CreatePaddedPolyline(Curve.PolylineAroundClosedCurve(node.BoundaryCurve), padding);
        }


        static Polyline LoosePolylineWithFewCorners(Polyline tightPolyline, double p) {
            if (p < ApproximateComparer.DistanceEpsilon)
                return tightPolyline;
            Polyline loosePolyline = CreateLoosePolylineOnBisectors(tightPolyline, p);
            return loosePolyline;
        }

        //private Polyline CutCorners(Polyline loosePolyline, Polyline tightPolyline) {
        //    Polyline ret = new Polyline();
        //    ret.Closed = true;
        //    PolylinePoint pp = loosePolyline.StartPoint;
        //    PolylinePoint tpp=tightPolyline.StartPoint;

        //    do {
        //        PolylinePoint furthestVisible = GetFurthestVisible(pp, ref tpp);
        //        ret.AddPoint(furthestVisible.Point);
        //        pp = furthestVisible;
        //    }
        //    while (pp != loosePolyline.StartPoint);

        //    System.Diagnostics.Debug.Assert(pp == loosePolyline.StartPoint);
        //    //distangle ret.StartPoint and ret.LastPoint
        //    return ret;
        //}

        //static PolylinePoint GetFurthestVisible(PolylinePoint pp, ref PolylinePoint tpp) {
        //    Point pivot = pp.Point;
        //    Point blockingPoint = tpp.NextOnPolyline.Point;
        //    while (Point.GetTriangleOrientation(pivot, blockingPoint, pp.NextOnPolyline.Point) == TriangleOrientation.Counterclockwise) {
        //        pp = pp.NextOnPolyline;
        //        tpp = tpp.NextOnPolyline;
        //    }
        //    return pp;
        //}

        static Polyline CreateLoosePolylineOnBisectors(Polyline tightPolyline, double p) {
            var ret = new Polyline();

            ret.AddPoint(GetStickingVertexOnBisector(tightPolyline.StartPoint, p));
            var blockingPoint = new Point(); //to silence the compiler
            var candidate = new Point();
            bool justAdded = true;

            for (PolylinePoint pp = tightPolyline.StartPoint.Next; pp != null; pp = pp.Next) {
                Point currentSticking = GetStickingVertexOnBisector(pp, p);
                if (justAdded) {
                    blockingPoint = pp.Point;
                    candidate = currentSticking;
                    justAdded = false;
                } else {
                    if (ret.Count > 1) {
                        // SugiyamaLayoutSettings.Show(tightPolyline, ret, new LineSegment(ret.StartPoint.Point, currentSticking));
                    }

                    //SugiyamaLayoutSettings.Show(new LineSegment(ret.EndPoint.Point, blockingPoint), tightPolyline, new LineSegment(ret.EndPoint.Point, currentSticking));
                    if (Point.GetTriangleOrientation(ret.EndPoint.Point, blockingPoint, currentSticking) !=
                        TriangleOrientation.Counterclockwise) {
                        ret.AddPoint(candidate);
                        // SugiyamaLayoutSettings.Show(ret, tightPolyline);
                        justAdded = true;
                        pp = pp.Prev;
                    } else {
                        candidate = currentSticking;
                        if (Point.GetTriangleOrientation(ret.EndPoint.Point, blockingPoint, pp.Point) ==
                            TriangleOrientation.Counterclockwise)
                            blockingPoint = pp.Point;
                    }
                }
            }

            //process the last point
            if (!justAdded) {
                if (Point.GetTriangleOrientation(ret.EndPoint.Point, blockingPoint, ret.StartPoint.Point) ==
                    TriangleOrientation.Counterclockwise) {
                    //the first point is visible, but now can we cut it
                    if (Point.GetTriangleOrientation(ret.EndPoint.Point, blockingPoint, ret.StartPoint.Next.Point) ==
                        TriangleOrientation.Counterclockwise)
                        ret.RemoveStartPoint();
                } else {
                    ret.AddPoint(candidate);
                }
            } else {
                //trying to cut away the first point
                if (
                    Point.GetTriangleOrientation(ret.EndPoint.Point, tightPolyline.StartPoint.Point,
                                                 ret.StartPoint.Next.Point) == TriangleOrientation.Counterclockwise)
                    ret.RemoveStartPoint();
                else { }
            }

            ret.Closed = true;
            // SugiyamaLayoutSettings.Show(tightPolyline, ret);
            return ret;
        }

        static Point GetStickingVertexOnBisector(PolylinePoint pp, double p) {
            Point u = pp.Polyline.Prev(pp).Point;
            Point v = pp.Point;
            Point w = pp.Polyline.Next(pp).Point;
            return p * ((v - u).Normalize() + (v - w).Normalize()).Normalize() + v;
        }


        double FindMaxPaddingForTightPolyline(Polyline polyline) {
            var dist = double.MaxValue;
            var polygon = new Polygon(polyline);

            foreach (var poly in RootOfLooseHierarchy.GetAllLeaves().Where(p => p != polyline))
                dist = Math.Min(dist, Polygon.Distance(polygon, new Polygon(poly)));

            //            TraverseHierarchy(RootOfLooseHierarchy, delegate(RectangleNode<Polyline, Point> node) {
            //                                                        if (node.UserData != null)
            //                                                            if (node.UserData != polyline)
            //                                                                dist = Math.Min(dist,
            //                                                                                Polygon.Distance(polygon,
            //                                                                                                 new Polygon(
            //                                                                                                     node.UserData)));
            //                                                    });
            dist = Math.Min(dist, RouterBetweenTwoNodes.DistanceFromPointToPolyline(router.SourcePoint, polyline));
            dist = Math.Min(dist, RouterBetweenTwoNodes.DistanceFromPointToPolyline(router.TargetPoint, polyline));
            return dist;
        }


        static bool CurvesIntersect(ICurve a, ICurve b) {
            return Curve.GetAllIntersections(a, b, false).Count > 0;
        }

        internal bool ObstaclesIntersectLine(Point a, Point b) {
            return ObstaclesIntersectICurve(new LineSegment(a, b));
        }

        internal bool ObstaclesIntersectICurve(ICurve curve) {
            return CurveIntersectsRectangleNode(curve, RootOfTightHierararchy)
                   ||
                   (SourceFilterLine != null && CurvesIntersect(curve, SourceFilterLine))
                   ||
                   (TargetFilterLine != null && CurvesIntersect(curve, TargetFilterLine));
        }

        internal static bool CurveIntersectsRectangleNode(ICurve curve, RectangleNode<Polyline, Point> rectNode) {
            Rectangle boundingBox = curve.BoundingBox;
            return CurveIntersectsRectangleNode(curve, ref boundingBox, rectNode);
        }

        static bool CurveIntersectsRectangleNode(ICurve curve, ref Rectangle curveBox, RectangleNode<Polyline, Point> rectNode) {
            if (!rectNode.Rectangle.Intersects(curveBox))
                return false;

            if (rectNode.UserData != null)
                return Curve.CurveCurveIntersectionOne(rectNode.UserData, curve, false) != null ||
                       Inside(rectNode.UserData, curve);

            Debug.Assert(rectNode.Left != null && rectNode.Right != null);

            return CurveIntersectsRectangleNode(curve, ref curveBox, rectNode.Left) ||
                   CurveIntersectsRectangleNode(curve, ref curveBox, rectNode.Right);
        }

        /// <summary>
        /// we know here that there are no intersection between "curveUnderTest" and "curve",
        /// We are testing that curve is inside of "curveUnderTest"
        /// </summary>
        /// <param name="curveUnderTest"></param>
        /// <param name="curve"></param>
        /// <returns></returns>
        static bool Inside(ICurve curveUnderTest, ICurve curve) {
            return Curve.PointRelativeToCurveLocation(curve.Start, curveUnderTest) == PointLocation.Inside;
        }

        #region Nested type: Visitor

        delegate void Visitor(RectangleNode<Polyline, Point> node);

        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <param name="quadrilateral"></param>
        /// <param name="rectangleNode"></param>
        /// <param name="polylineToIgnore"></param>
        /// <returns></returns>
        public static bool CurveIntersectsRectangleNode(Polyline quadrilateral, RectangleNode<Polyline, Point> rectangleNode, Polyline polylineToIgnore) {
            Rectangle boundingBox = quadrilateral.BoundingBox;
            return CurveIntersectsRectangleNode(quadrilateral, ref boundingBox, rectangleNode, polylineToIgnore);

        }

        static bool CurveIntersectsRectangleNode(ICurve curve, ref Rectangle curveBox, RectangleNode<Polyline, Point> rectNode, Polyline polylineToIgnore) {
            if (!rectNode.Rectangle.Intersects(curveBox))
                return false;

            if (rectNode.UserData != null)
                return rectNode.UserData != polylineToIgnore &&
                       (Curve.CurveCurveIntersectionOne(rectNode.UserData, curve, false) != null ||
                        Inside(rectNode.UserData, curve));

            Debug.Assert(rectNode.Left != null && rectNode.Right != null);

            return CurveIntersectsRectangleNode(curve, ref curveBox, rectNode.Left, polylineToIgnore) ||
                   CurveIntersectsRectangleNode(curve, ref curveBox, rectNode.Right, polylineToIgnore);
        }

        private class PolylineGraph
        {
            private Dictionary<Polyline, List<Polyline>> sourceToTargets = new Dictionary<Polyline, List<Polyline>>();

            internal IEnumerable<Polyline> Nodes { get { return sourceToTargets.Keys; } }

            internal void AddEdge(Polyline source, Polyline target)
            {
                List<Polyline> listOfEdges;
                if (!sourceToTargets.TryGetValue(source, out listOfEdges))
                {
                    sourceToTargets[source] = listOfEdges = new List<Polyline>();
                }

                listOfEdges.Add(target);
            }

            internal IEnumerable<Polyline> Descendents(Polyline Polyline)
            {
                return sourceToTargets[Polyline];
            }
        }
    }
}