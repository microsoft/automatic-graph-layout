using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;


namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// </summary>
    public class EdgeLabelPlacement : AlgorithmBase {
        /// <summary>
        ///     The list of labels to be placed
        /// </summary>
        ICollection<Label> labels;

        readonly RTree<IObstacle, Point>[] obstacleMaps = new RTree<IObstacle, Point>[3];
        RTree<IObstacle, Point> labelObstacleMap;

        readonly Dictionary<Edge, List<KeyValuePair<double, Point>>> edgePoints =
            new Dictionary<Edge, List<KeyValuePair<double, Point>>>();

        /// <summary>
        ///     The default and minimum granularity for breaking up a curve into many points.
        /// </summary>
        const int MinGranularity = 5;

        /// <summary>
        ///     The maximum granulairty for breaking up a curve into many points.
        /// </summary>
        const int MaxGranularity = 50;

        /// <summary>
        ///     The number of edges at which to start increasing the granularity.
        /// </summary>
        const int LowerEdgeBound = 500;

        /// <summary>
        ///     The number of edges at which to stop increasing the granularity.
        /// </summary>
        const int UpperEdgeBound = 3000;

        int granularity = MinGranularity;

        /// <summary>
        ///     The granularity with which to break up a curve into sub points.
        /// </summary>
        public int CollisionGranularity {
            get { return granularity; }
            set { granularity = value; }
        }

        /// <summary>
        ///     True if the edge collision granularity should be degraded as the number of edges increases.
        /// </summary>
        public bool ScaleCollisionGranularity { get; set; }

        bool parallelProcessingEnabled = true;

        /// <summary>
        ///     True if label placement should be done using multiple threads.
        /// </summary>
        public bool ParallelProcessingEnabled {
            get { return parallelProcessingEnabled; }
            set { parallelProcessingEnabled = value; }
        }

        /// <summary>
        ///     Constructs an edge label placer that places all labels in the graph.
        /// </summary>
        public EdgeLabelPlacement(GeometryGraph graph) {
            InitObstacles(graph);
            labels = graph.CollectAllLabels();
        }

        void InitObstacles(GeometryGraph graph) {
            IEnumerable<Cluster> allClusters = graph.RootCluster.AllClustersDepthFirst();
            InitializeObstacles(graph.Nodes.Concat(allClusters.Select(c => (Node) c)), graph.Edges);
        }

        /// <summary>
        ///     Constructs an edge label placer that places the given labels in the graph.
        /// </summary>
        public EdgeLabelPlacement(GeometryGraph graph, ICollection<Label> labels){
            InitObstacles(graph);
            this.labels = labels;
        }

        /// <summary>
        ///     Constructs a edge label placer that will only avoid overlaps with the given nodes and edges.
        /// </summary>
        public EdgeLabelPlacement(IEnumerable<Node> nodes, IEnumerable<Edge> edges) {
            InitializeObstacles(nodes, edges);
            labels = edges.SelectMany(e => e.Labels).ToList();
        }

        void InitializeObstacles(IEnumerable<Node> nodes, IEnumerable<Edge> edges) {
            List<Edge> edgeList = edges.ToList();
            var modifiedGranularity = GetModifiedGranularity(edgeList);

            var edgeObstacles = GetEdgeObstacles(edges, modifiedGranularity);

            var edgeObstacleMap =
                new RTree<IObstacle, Point>(edgeObstacles.Select(e => new KeyValuePair<IRectangle<Point>, IObstacle>(e.Rectangle, e)));

            var nodeObstacles = new List<IObstacle>();
            foreach (Node v in nodes)
                nodeObstacles.Add(new RectangleObstacle(v.BoundingBox, v));

            var nodeObstacleMap =
                new RTree<IObstacle, Point>(nodeObstacles.Select(n => new KeyValuePair<IRectangle<Point>, IObstacle>(n.Rectangle, n)));
            //later we init obstacleMaps[0] to lableObstacleMap
            obstacleMaps[1] = nodeObstacleMap;
            obstacleMaps[2] = edgeObstacleMap; // Avoiding edge overlaps is lowest priority, so put it last            
        }

        List<IObstacle> GetEdgeObstacles(IEnumerable<Edge> edges, int modifiedGranularity) {
            var edgeObstacles = new List<IObstacle>();
            foreach (Edge e in edges) {                
                var curvePoints = CurvePoints(e.Curve ?? new LineSegment(e.Source.Center, e.Target.Center), modifiedGranularity);
                edgePoints[e] = curvePoints;
                foreach (var p in curvePoints)
                    edgeObstacles.Add(new PortObstacle(p.Value));

                ProgressStep();
            }
            return edgeObstacles;
        }

        int GetModifiedGranularity(List<Edge> edgeList) {
            int modifiedGranularity = CollisionGranularity;
            if (ScaleCollisionGranularity)
                modifiedGranularity = LayoutAlgorithmHelpers.LinearInterpolation(edgeList.Count, LowerEdgeBound,
                                                                                 UpperEdgeBound,
                                                                                 CollisionGranularity,
                                                                                 MaxGranularity);
            return modifiedGranularity;
        }

       
        /// <summary>
        ///     Adds the label to the label obstacle map.
        /// </summary>
        void AddLabelObstacle(IObstacle label) {
            // Labels can be added by multiple threads at once.  Lock to prevent conflicts.
            // The lock doesn't appear to significantly affect the performance of the algorithm.
            lock (this)
                if (labelObstacleMap == null) {
                    labelObstacleMap =
                        new RTree<IObstacle, Point>(new[] {new KeyValuePair<IRectangle<Point>, IObstacle>(label.Rectangle, label)});
                    obstacleMaps[0] = labelObstacleMap;
                }
                else
                    labelObstacleMap.Add(label.Rectangle, label);
        }

        /// <summary>
        ///     Places the given labels.
        /// </summary>
        protected override void RunInternal() {
            Label[] lbs = labels.Where(l => l != null).ToArray();
            StartListenToLocalProgress(lbs.Length);

            // Place outer most labels first, since their positions are more semantically important
            // Also place labels on short edges before labels on long edges, since short edges have less options.
            IEnumerable<Label> sortedLabels = lbs.OrderByDescending(l => Math.Abs(0.5 - l.PlacementOffset))
                                                 .ThenBy(l => edgePoints[((Edge) l.GeometryParent)].Count);

#if PARALLEL_SUPPORTED
            if (ParallelProcessingEnabled && lbs.Length > 50)
                ParallelUtilities.ForEach(sortedLabels, PlaceLabel, ProgressSteps);
            else
#endif
                foreach (Label label in sortedLabels) {
                    PlaceLabel(label);
                    ProgressStep();
                }
        }

        /// <summary>
        ///     Places the given label in an available location.
        /// </summary>
        void PlaceLabel(Label label) {
            bool placed = false;
            if (label.PlacementStrategyPriority != null)
                foreach (Label.PlacementStrategy s in label.PlacementStrategyPriority) {
                    placed = s == Label.PlacementStrategy.AlongCurve && PlaceEdgeLabelOnCurve(label)
                             || s == Label.PlacementStrategy.Horizontal && PlaceEdgeLabelHorizontally(label);
                    if (placed)
                        break;
                }

            if (placed)
                CalculateCenterNotSure(label);
            else // just place it at its desired location
                PlaceLabelAtFirstPosition(label);
        }

        /// <summary>
        ///     Places the label at the first position requested.  Ignores all overlaps.
        /// </summary>
        void PlaceLabelAtFirstPosition(Label label) {
            var edge = (Edge) label.GeometryParent;
            ICurve curve = edge.Curve ?? new LineSegment(edge.Source.Center, edge.Target.Center);

            List<KeyValuePair<double, Point>> points = edgePoints[edge];

            int index = StartIndex(label, points);
            Point point = points[index].Value;
            Point derivative = curve.Derivative(points[index].Key);

            // If the curve is a line of length (close to) 0, the derivative may be (close to) 0.
            // Pick a direction in that case.
            if (derivative.Length < ApproximateComparer.Tolerance) {
                derivative = new Point(1, 1);
            }

            var widthHeight = new Point(label.Width, label.Height);
            double side = GetPossibleSides(label.Side, derivative).First();

            Rectangle bounds = GetLabelBounds(point, derivative, widthHeight, side);
            SetLabelBounds(label, bounds);
        }

        static int StartIndex(Label label, ICollection points) {
            return Math.Min(points.Count - 1, Math.Max(0, (int) Math.Floor(points.Count*label.PlacementOffset)));
        }

        static void CalculateCenterNotSure(Label label) {
            var cen = new Point();
            foreach (var p in label.InnerPoints)
                cen += p;
            foreach (var p in label.OuterPoints)
                cen += p;
            label.Center = cen/(label.InnerPoints.Count + label.OuterPoints.Count);
        }

        /// <summary>
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public bool PlaceEdgeLabelHorizontally(Label label) {
            ValidateArg.IsNotNull(label, "label");
            var e = (Edge) label.GeometryParent;
            // approximate label with a rectangle
            // process candidate points for label ordered by priority
            // check candidate point for conflicts - if none then stop and keep placement
            label.InnerPoints = null;
            List<KeyValuePair<double, Point>> curvePoints = edgePoints[e];
            var wh = new Point(label.Width, label.Height);

            int bestConflictIndex = -1;
            var bestRectangle = new Rectangle();
            foreach (int index in ExpandingSearch(StartIndex(label,curvePoints), 0, curvePoints.Count)) {
                KeyValuePair<double, Point> cp = curvePoints[index];

                ICurve curve = e.Curve ?? new LineSegment(e.Source.Center, e.Target.Center);
                Point der = curve.Derivative(cp.Key);
                if (der.LengthSquared < ApproximateComparer.DistanceEpsilon) continue;

                foreach (double side in GetPossibleSides(label.Side, der)) {
                    Rectangle queryRect = GetLabelBounds(cp.Value, der, wh, side);

                    int conflictIndex = ConflictIndex(queryRect, label);
                    if (conflictIndex > bestConflictIndex) {
                        bestConflictIndex = conflictIndex;
                        bestRectangle = queryRect;

                        // If the best location was found, we're done
                        if (bestConflictIndex == int.MaxValue)
                            break;
                    }
                }

                // If the best location was found, we're done
                if (bestConflictIndex == int.MaxValue)
                    break;
            }

            if (bestConflictIndex >= 0) {
                SetLabelBounds(label, bestRectangle);

                var r = new RectangleObstacle(bestRectangle);
                AddLabelObstacle(r);

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=371
                if (bestConflictIndex == 0)
                    label.PlacementResult = LabelPlacementResult.OverlapsOtherLabels;
                else if (bestConflictIndex == 1)
                    label.PlacementResult = LabelPlacementResult.OverlapsNodes;
                else if (bestConflictIndex == 2)
                    label.PlacementResult = LabelPlacementResult.OverlapsEdges;
                else
                    label.PlacementResult = LabelPlacementResult.OverlapsNothing;
#else
                label.PlacementResult = (LabelPlacementResult) bestConflictIndex;
#endif
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Gets the label placement bounds for the given location, side, and label size.
        /// </summary>
        /// <param name="point">The point along a curve that the label should be placed near.</param>
        /// <param name="derivative">The derivative of the curve at the point position.</param>
        /// <param name="widthHeight">The width and height of the label.</param>
        /// <param name="side">The side (1 or -1) of the line to place the label on.</param>
        /// <returns>The label's desired position.</returns>
        static Rectangle GetLabelBounds(Point point, Point derivative, Point widthHeight, double side) {
            Point o = derivative.Rotate(Math.PI/2).Normalize()*side*2;
            Point labelPos = point + o;

            double left = (o.X > 0) ? labelPos.X : labelPos.X - widthHeight.X;
            double bottom = (o.Y > 0 ? labelPos.Y : labelPos.Y - widthHeight.Y);

            // If the line is near horizontal, shift the placement
            // to make it naturally transistion from o.X being negative to positive.
            if (Math.Abs(o.X) < 0.75) {
                // _________  /
                // |______w_|/
                //     \   o/ 
                //      \  /
                //       \/ <-- right angle
                //       /
                //      /
                // Get the angle, 'o', between the line and the label
                double horizontalAngle = Math.Acos(Math.Abs(o.Y)/o.Length);
                // Get the distance, 'w', from the tip of the normal to the line
                double horizontalShift = o.Length/Math.Sin(horizontalAngle);
                double verticalShift = o.Length/Math.Cos(horizontalAngle);
                // Shift the label by this amount, or by half the width.  Whichever is smaller
                left += (o.X > 0 ? -1 : 1)*Math.Min(horizontalShift, widthHeight.X/2.0);
                bottom += (o.Y > 0 ? 1 : -1)*verticalShift;
            }
            else if (Math.Abs(o.Y) < 0.75) {
                double verticalAngle = Math.Acos(Math.Abs(o.X)/o.Length);
                double verticalShift = o.Length/Math.Sin(verticalAngle);
                double horizontalShift = o.Length/Math.Cos(verticalAngle);
                left += (o.X > 0 ? 1 : -1)*horizontalShift;
                bottom += (o.Y > 0 ? -1 : 1)*Math.Min(verticalShift, widthHeight.Y/2.0);
            }

            return new Rectangle(left, bottom, widthHeight);
        }

        /// <summary>
        ///     Sets the label's position to be the given bounds.
        /// </summary>
        static void SetLabelBounds(Label label, Rectangle bounds) {
            var innerPoints = new List<Point>();
            var outerPoints = new List<Point>();

            innerPoints.Add(new Point(bounds.LeftTop.X, bounds.LeftTop.Y));
            innerPoints.Add(new Point(bounds.RightTop.X, bounds.RightTop.Y));
            outerPoints.Add(new Point(bounds.LeftBottom.X, bounds.LeftBottom.Y));
            outerPoints.Add(new Point(bounds.RightBottom.X, bounds.RightBottom.Y));
            label.InnerPoints = innerPoints;
            label.OuterPoints = outerPoints;
        }

        /// <summary>
        ///     Gets the possible sides for the given label and the given derivative point.
        /// </summary>
        /// <returns>An enumeration of the possible sides (-1 or 1).</returns>
        static double[] GetPossibleSides(Label.PlacementSide side, Point derivative) {
            if (derivative.Length == 0) {
                side = Label.PlacementSide.Any;
            }

            switch (side) {
                case Label.PlacementSide.Port:
                    return new double[] {-1};
                case Label.PlacementSide.Starboard:
                    return new double[] {1};
                case Label.PlacementSide.Top:
                    if (ApproximateComparer.Close(derivative.X, 0)) {
                        // If the line is vertical, Top becomes Left
                        return GetPossibleSides(Label.PlacementSide.Left, derivative);
                    }
                    return new double[] {derivative.X < 0 ? 1 : -1};
                case Label.PlacementSide.Bottom:
                    if (ApproximateComparer.Close(derivative.X, 0)) {
                        // If the line is vertical, Bottom becomes Right
                        return GetPossibleSides(Label.PlacementSide.Right, derivative);
                    }
                    return new double[] {derivative.X < 0 ? -1 : 1};
                case Label.PlacementSide.Left:
                    if (ApproximateComparer.Close(derivative.Y, 0)) {
                        // If the line is horizontal, Left becomes Top
                        return GetPossibleSides(Label.PlacementSide.Top, derivative);
                    }
                    return new double[] {derivative.Y < 0 ? -1 : 1};
                case Label.PlacementSide.Right:
                    if (ApproximateComparer.Close(derivative.Y, 0)) {
                        // If the line is horizontal, Right becomes Bottom
                        return GetPossibleSides(Label.PlacementSide.Bottom, derivative);
                    }
                    return new double[] {derivative.Y < 0 ? 1 : -1};
                default:
                    return new double[] {-1, 1};
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="start"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static IEnumerable<int> ExpandingSearch(int start, int min, int max) {
            Debug.Assert(start >= min);
            Debug.Assert(start < max);
            Debug.Assert(min < max);
            int upper;
            int lower = upper = start + 1;
            while (lower > min || upper < max) {
                if (lower > min)
                    yield return --lower;
                if (upper < max)
                    yield return upper++;
            }
        }

        class PointSet {
            internal Point Center;
            internal Point Inner;
            internal double Key;
            internal Point Outer;
        }

        static double PointSetLength(IEnumerable<PointSet> ps) {
            double l = 0;
            Point? q = null;
            foreach (PointSet p in ps.OrderBy(p => p.Key)) {
                if (q != null) {
                    l += ((Point) q - p.Center).Length;
                }
                q = p.Center;
            }
            return l;
        }

        class PointSetList {
            internal readonly LinkedList<PointSet> Points = new LinkedList<PointSet>();
            double coveredLength;

            internal double AddFirst(PointSet p) {
                if (Points.Count != 0) {
                    PointSet q = Points.First.Value;
                    coveredLength += (p.Center - q.Center).Length;
                }
                Points.AddFirst(p);
                return coveredLength;
            }

            internal double AddLast(PointSet p) {
                if (Points.Count != 0) {
                    PointSet q = Points.Last.Value;
                    coveredLength += (p.Center - q.Center).Length;
                }
                Points.AddLast(p);
                return coveredLength;
            }
        }

        /// <summary>
        ///     places a label
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        bool PlaceEdgeLabelOnCurve(Label label) {
            ValidateArg.IsNotNull(label, "label");
            // approximate label with a set of circles
            // generate list of candidate points for label ordered by priority
            // check candidate point for conflicts - if none then stop and keep placement
            var edge = (Edge) label.GeometryParent;
            label.InnerPoints = null;
            List<KeyValuePair<double, Point>> curvePoints = edgePoints[edge];
            const double distanceFromCurve = 3;
            double radius = label.Height/2.0;
            var wh = new Point(radius, radius);
            double labelLength = label.Width;
            foreach (int index in ExpandingSearch(StartIndex(label,curvePoints), 0, curvePoints.Count)) {
                double[] sides;
                var curve = GetSidesAndEdgeCurve(label, edge, curvePoints, index, out sides);

                foreach (double side in sides) {
                    var placedPoints = new PointSetList();
                    double coveredLength = 0;
                    ProcessExpandingSearchOnSide(index, curvePoints, curve, side, radius, distanceFromCurve, wh,
                                                 ref coveredLength, placedPoints, labelLength);

                    if (coveredLength >= labelLength) {
                        CaseOfCoveredLengthGreaterThanLabelLength(label, placedPoints, coveredLength, labelLength, wh);
                        return true;
                    }
                }
            }
            return false;
        }

        void CaseOfCoveredLengthGreaterThanLabelLength(Label label, PointSetList placedPoints, double coveredLength,
                                                       double labelLength, Point wh) {
            var innerPoints = new List<Point>();
            var outerPoints = new List<Point>();
            List<PointSet> orderedPoints = placedPoints.Points.ToList();
            double excess = coveredLength - labelLength;
            if (excess > 0) {
                // move back the last point
                PointSet q = orderedPoints[orderedPoints.Count - 1];
                PointSet p = orderedPoints[orderedPoints.Count - 2];
                Point v = q.Center - p.Center;
                double length = v.Length;
                if (excess > length) {
                    q = orderedPoints[0];
                    p = orderedPoints[1];
                    v = q.Center - p.Center;
                    length = v.Length;
                }
                Debug.Assert(length > excess);
                Point w = v*((length - excess)/length);
                Debug.Assert(Math.Abs((length - w.Length) - excess) < 0.01);
                q.Center = p.Center + w;
                q.Inner = p.Inner + w;
                q.Outer = p.Outer + w;
            }
            double cl = PointSetLength(orderedPoints);
            Debug.Assert(Math.Abs(cl - labelLength) < 0.01);
            GoOverOrderedPointsAndAddLabelObstacels(orderedPoints, innerPoints, outerPoints, wh);
            // placed all points in label so we are done
            label.InnerPoints = innerPoints;
            label.OuterPoints = outerPoints;
        }

        void GoOverOrderedPointsAndAddLabelObstacels(List<PointSet> orderedPoints, List<Point> innerPoints, List<Point> outerPoints, Point wh) {
            foreach (PointSet p in orderedPoints) {
                Point center = p.Center;
                innerPoints.Add(p.Inner);
                outerPoints.Add(p.Outer);
                var r = new RectangleObstacle(new Rectangle(center + wh, center - wh));
                AddLabelObstacle(r);
            }
        }

        void ProcessExpandingSearchOnSide(int index, List<KeyValuePair<double, Point>> curvePoints, ICurve curve,
                                          double side, double radius,
                                          double distanceFromCurve, Point wh, ref double coveredLength,
                                          PointSetList placedPoints,
                                          double labelLength) {
            foreach (int i in ExpandingSearch(index, 0, curvePoints.Count)) {
                KeyValuePair<double, Point> p = curvePoints[i];
                Point der = curve.Derivative(p.Key);

                if (der.LengthSquared < ApproximateComparer.DistanceEpsilon) continue;

                Point o = der.Rotate(Math.PI/2).Normalize()*side;
                Point labelPos = p.Value + (radius + distanceFromCurve)*o;
                if (!Conflict(labelPos, radius, wh)) {
                    // found a valid candidate position
                    var ps = new PointSet {
                        Key = p.Key,
                        Center = labelPos,
                        Inner = p.Value + distanceFromCurve*o,
                        Outer = p.Value + (2.0*radius + distanceFromCurve)*o
                    };
                    coveredLength = i <= index
                                        ? placedPoints.AddFirst(ps)
                                        : placedPoints.AddLast(ps);
                    Debug.Assert(Math.Abs(PointSetLength(placedPoints.Points) - coveredLength) < 0.01);
                    if (coveredLength >= labelLength) {
                        break;
                    }
                }
                else {
                    // not going to work!
                    break;
                }
            }
        }

        static ICurve GetSidesAndEdgeCurve(Label label, Edge e, List<KeyValuePair<double, Point>> curvePoints, int index, out double[] sides) {
            ICurve curve = e.Curve ?? new LineSegment(e.Source.Center, e.Target.Center);
            Point initialDer = curve.Derivative(curvePoints[index].Key);
            sides = GetPossibleSides(label.Side, initialDer);
            return curve;
        }

        /// <summary>
        ///     Determines if the query point intersects with any of the obstacles.
        /// </summary>
        /// <returns>True if the query point itnersects with any of the obstacles.</returns>
        bool Conflict(Point labelPos, double radius, Point wh) {
            return ConflictIndex(labelPos, radius, wh) != int.MaxValue;
        }

        /// <summary>
        ///     Determines the index of the first obstacle map that the point intersects.
        /// </summary>
        /// <returns>The index of the first obstacle map that the point intersects. int.MaxValue if there is no intersection.</returns>
        int ConflictIndex(Point labelPos, double radius, Point wh) {
            var queryRect = new Rectangle(labelPos - wh, labelPos + wh);
            double r2 = radius*radius;

            for (int i = 0; i < obstacleMaps.Length; i++) {
                if (obstacleMaps[i] == null)
                    continue;

                foreach (IObstacle c in obstacleMaps[i].GetAllIntersecting(queryRect)) {
                    if (c is PortObstacle) {
                        if ((labelPos - ((PortObstacle) c).Location).LengthSquared < r2)
                            return i;
                    }
                    else return i;
                }
            }

            return int.MaxValue;
        }

        /// <summary>
        ///     Determines the index of the first obstacle map that the rectangle intersects.
        ///     Clusters that are parents/grandparents of the label's source/target nodes are not considered intersection.
        /// </summary>
        /// <returns>The index of the first obstacle map that the rectangle intersects. int.MaxValue if there is no intersection.</returns>
        int ConflictIndex(Rectangle queryRect, Label label) {
            var edge = (Edge) label.GeometryParent;
            Node source = edge.Source;
            Node target = edge.Target;

            for (int i = 0; i < obstacleMaps.Length; i++) {
                if (obstacleMaps[i] == null)
                    continue;

                foreach (IObstacle obstacle in obstacleMaps[i].GetAllIntersecting(queryRect)) {
                    // If we're overlapping a node...
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=371
                    if (i == 1) {
#else
                    if ((LabelPlacementResult) i == LabelPlacementResult.OverlapsNodes) {
#endif
                        // ...and the node is a cluster...
                        var rectangleObstacle = obstacle as RectangleObstacle;
                        if (rectangleObstacle != null) {
                            var cluster = rectangleObstacle.Data as Cluster;

                            // ...and the cluster is a grandparent of the source or target...
                            if (cluster != null && (source.IsDescendantOf(cluster) || target.IsDescendantOf(cluster))) {
                                // ...don't consider the overlap to be a conflict.
                                continue;
                            }
                        }
                    }

                    return i;
                }
            }

            return int.MaxValue;
        }

        static void SubdivideCurveSegment(List<KeyValuePair<double, Point>> list, ICurve curve, double delta2,
                                          double start, double end) {
            if (list.Count > 64) //LN I saw this function never finishing for a very long curve
                return;
            Point startPoint = curve[start];
            Point endPoint = curve[end];
            if ((startPoint - endPoint).LengthSquared > delta2) {
                double mid = (start + end)/2.0;
                SubdivideCurveSegment(list, curve, delta2, start, mid);
                SubdivideCurveSegment(list, curve, delta2, mid, end);
            }
            else {
                list.Add(new KeyValuePair<double, Point>(start, startPoint));
            }
        }

        class PointComparer : IComparer<KeyValuePair<double, Point>> {
            public int Compare(KeyValuePair<double, Point> x, KeyValuePair<double, Point> y) {
                if (x.Key < y.Key) {
                    return -1;
                }
                if (x.Key > y.Key) {
                    return 1;
                }
                return 0;
            }
        }

        static List<KeyValuePair<double, Point>> CurvePoints(ICurve curve, int granularity) {
            var points = new List<KeyValuePair<double, Point>>();
            double delta = (curve.End - curve.Start).LengthSquared/(granularity*granularity);
            SubdivideCurveSegment(points, curve, delta, curve.ParStart, curve.ParEnd);

            points.Sort(new PointComparer());
            if (points.Last().Key < curve.ParEnd) {
                points.Add(new KeyValuePair<double, Point>(curve.ParEnd, curve.End));
            }
            return points;
        }

        /// <summary>
        /// Places the given labels at their default positions.  Only avoids overlaps with the edge and source/target node that the label is connected to.
        /// </summary>
        public static void PlaceLabelsAtDefaultPositions(CancelToken cancelToken, IEnumerable<Edge> edges) {
            ValidateArg.IsNotNull(edges, "edges");
            foreach (Edge edge in edges) {
                if (edge.Labels.Count > 0) {
                    EdgeLabelPlacement placer = new EdgeLabelPlacement(new[] { edge.Source, edge.Target }, new[] { edge });
                    placer.Run(cancelToken);
                }
            }
        }
    }
}