using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// This is almost a copy, considering the data, of LayeredLayoutEngine.
    /// The class restores itself from a given GeometryGraph
    /// </summary>
    internal class RecoveryLayeredLayoutEngine {

        bool Brandes { get; set; }

        LayerArrays engineLayerArrays;

        GeometryGraph originalGraph;

        ProperLayeredGraph properLayeredGraph;

        /// <summary>
        /// the sugiyama layout settings responsible for the algorithm parameters
        /// </summary>
        internal SugiyamaLayoutSettings sugiyamaSettings;

        /// <summary>
        /// the width of dummy nodes
        /// </summary>
        static double VirtualNodeWidth {
            get { return 1; }
        }

        /// <summary>
        /// the height of dummy nodes
        /// </summary>
        static double VirtualNodeHeight(SugiyamaLayoutSettings settings) {
            return settings.MinNodeHeight * 1.5f / 8;
        }

        Database database;

        /// <summary>
        /// Keeps assorted data associated with the graph.
        /// </summary>
        internal Database Database {
            set { database = value; }
            get { return database; }
        }

        internal BasicGraph<Node, PolyIntEdge> IntGraph; //the input graph

        BasicGraph<Node, PolyIntEdge> GluedDagSkeletonForLayering { get; set; }

        //the graph obtained after X coord calculation
        XLayoutGraph xLayoutGraph;
        Dictionary<Node, int> nodeIdToIndex;
        Anchor[] anchors;

        readonly LayerArrays recoveredLayerArrays;
        double[] layerToRecoveredYCoordinates;
        readonly Dictionary<int, double> skeletonVirtualVerticesToX = new Dictionary<int, double>();


        internal RecoveryLayeredLayoutEngine(GeometryGraph originalGraph) {
            if (originalGraph != null) {
                Init(originalGraph);
                recoveredLayerArrays = RecoverOriginalLayersAndSettings(originalGraph, out sugiyamaSettings);                
            }
        }

        void FillLayersToRecoveredYCoordinates() {
            layerToRecoveredYCoordinates = new double[recoveredLayerArrays.Layers.Length];
            for (int i = 0; i < layerToRecoveredYCoordinates.Length; i++)
                layerToRecoveredYCoordinates[i] = originalGraph.Nodes[recoveredLayerArrays.Layers[i][0]].Center.Y;
        }

        void Init(GeometryGraph geometryGraph) {
            nodeIdToIndex = new Dictionary<Node, int>();

            IList<Node> nodes = geometryGraph.Nodes;

            InitNodesToIndex(nodes);

            PolyIntEdge[] intEdges = CreateIntEdges(geometryGraph);

            IntGraph = new BasicGraph<Node, PolyIntEdge>(intEdges, geometryGraph.Nodes.Count) { Nodes = nodes };
            originalGraph = geometryGraph;
            if (sugiyamaSettings == null)
                sugiyamaSettings = new SugiyamaLayoutSettings();
            CreateDatabaseAndRegisterIntEdgesInMultiEdges();
        }

        void CreateDatabaseAndRegisterIntEdgesInMultiEdges() {
            Database = new Database();
            foreach (PolyIntEdge e in IntGraph.Edges)
                database.RegisterOriginalEdgeInMultiedges(e);
        }

        PolyIntEdge[] CreateIntEdges(GeometryGraph geometryGraph) {
            var edges = geometryGraph.Edges;


            var intEdges = new PolyIntEdge[edges.Count];
            int i = 0;
            foreach (var edge in edges) {
                if (edge.Source == null || edge.Target == null)
                    throw new InvalidOperationException(); //"creating an edge with null source or target");

                var intEdge = new PolyIntEdge(nodeIdToIndex[edge.Source], nodeIdToIndex[edge.Target], edge);

                intEdges[i] = intEdge;
                i++;
            }

            return intEdges;
        }

        void InitNodesToIndex(IList<Node> nodes) {
            int index = 0;
            foreach (Node n in nodes) {
                nodeIdToIndex[n] = index;
                index++;
            }
        }

        internal static int EdgeSpan(int[] layers, PolyIntEdge e) {
            return layers[e.Source] - layers[e.Target];
        }

        /// <summary>
        /// Creating a DAG which glues same layer vertices into one original, 
        /// replacing original multiple edges with a single edge.
        /// upDown constraints will be added as edges
        /// </summary>
        void CreateGluedDagSkeletonForLayering() {
            GluedDagSkeletonForLayering = new BasicGraph<Node, PolyIntEdge>(GluedDagSkeletonEdges(),
                                                                        originalGraph.Nodes.Count);
            SetGluedEdgesWeights();
        }

        void SetGluedEdgesWeights() {
            var gluedPairsToGluedEdge = new Dictionary<IntPair, PolyIntEdge>();
            foreach (PolyIntEdge ie in GluedDagSkeletonForLayering.Edges)
                gluedPairsToGluedEdge[new IntPair(ie.Source, ie.Target)] = ie;

            foreach (var t in database.Multiedges)
                if (t.Key.x != t.Key.y) {
                    IntPair gluedPair = VerticalConstraints.GluedIntPair(t.Key);
                    if (gluedPair.x == gluedPair.y) continue;
                    PolyIntEdge gluedIntEdge = gluedPairsToGluedEdge[gluedPair];
                    foreach (PolyIntEdge ie in t.Value)
                        gluedIntEdge.Weight += ie.Weight;
                }
        }

        IEnumerable<PolyIntEdge> GluedDagSkeletonEdges() {
            var ret =
                new Set<PolyIntEdge>(from kv in database.Multiedges
                                 where kv.Key.x != kv.Key.y
                                 let e = VerticalConstraints.GluedIntEdge(kv.Value[0])
                                 where e.Source != e.Target
                                 select e);

            IEnumerable<PolyIntEdge> gluedUpDownConstraints = from p in VerticalConstraints.GluedUpDownIntConstraints
                                                          select CreateUpDownConstrainedIntEdge(p);
            foreach (PolyIntEdge edge in gluedUpDownConstraints)
                ret.Insert(edge);
            return ret;
        }

        static PolyIntEdge CreateUpDownConstrainedIntEdge(IntPair intPair) {
            var intEdge = new PolyIntEdge(intPair.x, intPair.y);
            intEdge.Weight = 0;
            //we do not want the edge weight to contribute in to the sum but just take the constraint into account
            intEdge.Separation = 1;
            return intEdge;
        }


        /// <summary>
        /// The main calculation is done here
        /// </summary>       
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider",
            MessageId = "System.String.Format(System.String,System.Object,System.Object,System.Object)"),
         SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider",
             MessageId = "System.String.Format(System.String,System.Object)")]
        internal void Run() {
            if (originalGraph.Nodes.Count > 0) {
                engineLayerArrays = CalculateLayers();
            } else
                originalGraph.boundingBox.SetToEmpty();

        }

        LayerArrays CalculateLayers() {
            CreateGluedDagSkeletonForLayering();
            LayerArrays layerArrays = CalculateLayersWithoutAspectRatio();

            UpdateNodePositionData();


            return layerArrays;
        }


        /// <summary>
        /// pushes positions from the anchors to node Centers
        /// </summary>
        void UpdateNodePositionData() {
            TryToSatisfyMinWidhtAndMinHeight();

            for (int i = 0; i < IntGraph.Nodes.Count && i < database.Anchors.Length; i++)
                IntGraph.Nodes[i].Center = database.Anchors[i].Origin;
        }

        void TryToSatisfyMinWidhtAndMinHeight() {
            TryToSatisfyMinWidth();
            TryToSatisfyMinHeight();
        }

        void TryToSatisfyMinWidth() {
            if (sugiyamaSettings.MinimalWidth == 0)
                return;
            double w = GetCurrentWidth();
            if (w < sugiyamaSettings.MinimalWidth)
                StretchWidth();
        }

        /// <summary>
        /// </summary>
        void StretchWidth() {
            //calculate the desired span of the anchor centers and the current span of anchor center
            var desiredSpan = new RealNumberSpan();
            foreach (Node node in originalGraph.Nodes) {
                desiredSpan.AddValue(node.BoundingBox.Width / 2);
                desiredSpan.AddValue(sugiyamaSettings.MinimalWidth - node.BoundingBox.Width / 2);
            }

            var currentSpan = new RealNumberSpan();
            foreach (Anchor anchor in NodeAnchors())
                currentSpan.AddValue(anchor.X);

            if (currentSpan.Length > ApproximateComparer.DistanceEpsilon) {
                double stretch = desiredSpan.Length / currentSpan.Length;
                if (stretch > 1)
                    foreach (Anchor a in anchors)
                        a.X *= stretch;
            }
        }

        void TryToSatisfyMinHeight() {
            if (sugiyamaSettings.MinimalHeight == 0)
                return;
            double h = GetCurrentHeight();
            if (h < sugiyamaSettings.MinimalHeight)
                StretchHeight();
        }

        double GetCurrentHeight() {
            var span = new RealNumberSpan();
            foreach (Anchor anchor in NodeAnchors()) {
                span.AddValue(anchor.Top);
                span.AddValue(anchor.Bottom);
            }
            return span.Length;
        }

        void StretchHeight() {
            var desiredSpan = new RealNumberSpan();
            foreach (Node node in originalGraph.Nodes) {
                desiredSpan.AddValue(node.BoundingBox.Height / 2);
                desiredSpan.AddValue(sugiyamaSettings.MinimalHeight - node.BoundingBox.Height / 2);
            }

            var currentSpan = new RealNumberSpan();
            foreach (Anchor anchor in NodeAnchors())
                currentSpan.AddValue(anchor.Y);

            if (currentSpan.Length > ApproximateComparer.DistanceEpsilon) {
                double stretch = desiredSpan.Length / currentSpan.Length;
                if (stretch > 1)
                    foreach (Anchor a in anchors)
                        a.Y *= stretch;
            }
        }

        IEnumerable<Anchor> NodeAnchors() {
            return anchors.Take(Math.Min(IntGraph.Nodes.Count, anchors.Length));
        }

        double GetCurrentWidth() {
            var span = new RealNumberSpan();
            foreach (Anchor anchor in NodeAnchors()) {
                span.AddValue(anchor.Left);
                span.AddValue(anchor.Right);
            }
            return span.Length;
        }


        void StretchToDesiredAspectRatio(double aspectRatio, double desiredAr) {
            if (aspectRatio > desiredAr)
                StretchInYDirection(aspectRatio / desiredAr);
            else if (aspectRatio < desiredAr)
                StretchInXDirection(desiredAr / aspectRatio);
        }

        void StretchInYDirection(double scaleFactor) {
            double center = (originalGraph.BoundingBox.Top + originalGraph.BoundingBox.Bottom) / 2;
            foreach (Anchor a in Database.Anchors) {
                a.BottomAnchor *= scaleFactor;
                a.TopAnchor *= scaleFactor;
                a.Y = center + scaleFactor * (a.Y - center);
            }
            double h = originalGraph.Height * scaleFactor;
            originalGraph.BoundingBox = new Rectangle(originalGraph.BoundingBox.Left, center + h / 2,
                                                      originalGraph.BoundingBox.Right, center - h / 2);
        }

        void StretchInXDirection(double scaleFactor) {
            double center = (originalGraph.BoundingBox.Left + originalGraph.BoundingBox.Right) / 2;
            foreach (Anchor a in Database.Anchors) {
                a.LeftAnchor *= scaleFactor;
                a.RightAnchor *= scaleFactor;
                a.X = center + scaleFactor * (a.X - center);
            }
            double w = originalGraph.Width * scaleFactor;
            originalGraph.BoundingBox = new Rectangle(center - w / 2, originalGraph.BoundingBox.Top, center + w / 2,
                                                      originalGraph.BoundingBox.Bottom);
        }

        LayerArrays CalculateLayersWithoutAspectRatio() {
            LayerArrays layerArrays = CalculateYLayers();

            if (constrainedOrdering == null) {
                DecideIfUsingFastXCoordCalculation(layerArrays);

                CalculateAnchorsAndYPositions(layerArrays);

                if (Brandes)
                    CalculateXPositionsByBrandes(layerArrays);
                else
                    CalculateXLayersByGansnerNorth(layerArrays);
            } else
                anchors = database.Anchors;

            OptimizeEdgeLabelsLocations();

            engineLayerArrays = layerArrays;
            StraightensShortEdges();

            double aspectRatio;
            CalculateOriginalGraphBox(out aspectRatio);

            if (sugiyamaSettings.AspectRatio != 0)
                StretchToDesiredAspectRatio(aspectRatio, sugiyamaSettings.AspectRatio);

            return layerArrays;
        }

        void DecideIfUsingFastXCoordCalculation(LayerArrays layerArrays) {
            if (layerArrays.X.Length >= sugiyamaSettings.BrandesThreshold)
                Brandes = true;
            else {
                string s = Environment.GetEnvironmentVariable("Brandes");
                if (!String.IsNullOrEmpty(s) && String.Compare(s, "on", true, CultureInfo.CurrentCulture) == 0)
                    Brandes = true;
            }
        }

        void StraightensShortEdges() {
            for (; StraightenEdgePaths(); ) { }
        }


        bool StraightenEdgePaths() {
            bool ret = false;
            foreach (PolyIntEdge e in database.AllIntEdges)
                if (e.LayerSpan == 2)
                    ret =
                        ShiftVertexWithNeighbors(e.LayerEdges[0].Source, e.LayerEdges[0].Target, e.LayerEdges[1].Target) ||
                        ret;
            return ret;
            //foreach (LayerEdge[][] edgeStrings in this.dataBase.RefinedEdges.Values)
            //    if (edgeStrings[0].Length == 2)
            //        foreach (LayerEdge[] edgePath in edgeStrings)
            //            ret = ShiftVertexWithNeighbors(edgePath[0].Source, edgePath[0].Target, edgePath[1].Target) || ret;
            //return ret;
        }


        bool ShiftVertexWithNeighbors(int u, int i, int v) {
            Anchor upper = database.Anchors[u];
            Anchor lower = database.Anchors[v];
            Anchor iAnchor = database.Anchors[i];
            //calculate the ideal x position for i
            // (x- upper.x)/(iAnchor.y-upper.y)=(lower.x-upper.x)/(lower.y-upper.y)

            double x = (iAnchor.Y - upper.Y) * (lower.X - upper.X) / (lower.Y - upper.Y) + upper.X;
            const double eps = 0.0001;
            if (x > iAnchor.X + eps)
                return TryShiftToTheRight(x, i);
            if (x < iAnchor.X - eps)
                return TryShiftToTheLeft(x, i);
            return false;
        }


        bool TryShiftToTheLeft(double x, int v) {
            int[] layer = engineLayerArrays.Layers[engineLayerArrays.Y[v]];
            int vPosition = engineLayerArrays.X[v];
            if (vPosition > 0) {
                Anchor uAnchor = database.Anchors[layer[vPosition - 1]];
                double allowedX = Math.Max(
                    uAnchor.Right + sugiyamaSettings.NodeSeparation + database.Anchors[v].LeftAnchor, x);
                if (allowedX < database.Anchors[v].X - 1) {
                    database.Anchors[v].X = allowedX;
                    return true;
                }
                return false;
            }
            database.Anchors[v].X = x;
            return true;
        }

        bool TryShiftToTheRight(double x, int v) {
            int[] layer = engineLayerArrays.Layers[engineLayerArrays.Y[v]];
            int vPosition = engineLayerArrays.X[v];
            if (vPosition < layer.Length - 1) {
                Anchor uAnchor = database.Anchors[layer[vPosition + 1]];
                double allowedX = Math.Min(
                    uAnchor.Left - sugiyamaSettings.NodeSeparation - database.Anchors[v].RightAnchor, x);
                if (allowedX > database.Anchors[v].X + 1) {
                    database.Anchors[v].X = allowedX;
                    return true;
                }
                return false;
            }
            database.Anchors[v].X = x;
            return true;
        }

        LayerArrays CalculateYLayers() {
            LayerArrays layerArrays =
                YLayeringAndOrdering(new RecoveryLayerCalculator(recoveredLayerArrays));
            if (constrainedOrdering != null)
                return layerArrays;
            return InsertLayersIfNeeded(layerArrays);
        }

        VerticalConstraintsForSugiyama VerticalConstraints {
            get { return sugiyamaSettings.VerticalConstraints; }
        }

        HorizontalConstraintsForSugiyama HorizontalConstraints {
            get { return sugiyamaSettings.HorizontalConstraints; }
        }

        void CalculateAnchorsAndYPositions(LayerArrays layerArrays) {
            CalculateAnchorSizes(database, out anchors, properLayeredGraph, originalGraph, IntGraph, sugiyamaSettings);
            CalcInitialYAnchorLocations(layerArrays, 500, originalGraph, database, IntGraph, sugiyamaSettings,
                                        LayersAreDoubled);
        }

        /// <summary>
        /// put some labels to the left of the splines if it makes sense
        /// </summary>
        void OptimizeEdgeLabelsLocations() {
            for (int i = 0; i < anchors.Length; i++) {
                Anchor a = anchors[i];
                if (a.LabelToTheRightOfAnchorCenter) {
                    //by default the label is put to the right of the spline
                    Anchor predecessor;
                    Anchor successor;
                    GetSuccessorAndPredecessor(i, out predecessor, out successor);
                    if (!TryToPutLabelOutsideOfAngle(a, predecessor, successor)) {
                        double sumNow = (predecessor.Origin - a.Origin).Length + (successor.Origin - a.Origin).Length;
                        double nx = a.Right - a.LeftAnchor; //new potential anchor center 
                        var xy = new Point(nx, a.Y);
                        double sumWouldBe = (predecessor.Origin - xy).Length + (successor.Origin - xy).Length;
                        if (sumWouldBe < sumNow) //we need to swap
                            PutLabelToTheLeft(a);
                    }
                }
            }
        }

        static bool TryToPutLabelOutsideOfAngle(Anchor a, Anchor predecessor, Anchor successor) {
            if (a.LabelToTheRightOfAnchorCenter) {
                if (Point.GetTriangleOrientation(predecessor.Origin, a.Origin, successor.Origin) ==
                    TriangleOrientation.Clockwise)
                    return true;

                double la = a.LeftAnchor;
                double ra = a.RightAnchor;
                double x = a.X;
                PutLabelToTheLeft(a);
                if (Point.GetTriangleOrientation(predecessor.Origin, a.Origin, successor.Origin) ==
                    TriangleOrientation.Counterclockwise)
                    return true;
                a.X = x;
                a.LeftAnchor = la;
                a.RightAnchor = ra;
                a.LabelToTheRightOfAnchorCenter = true;
                a.LabelToTheLeftOfAnchorCenter = false;
                return false;
            }
            return false;
        }

        static void PutLabelToTheLeft(Anchor a) {
            double r = a.Right;
            double t = a.LeftAnchor;
            a.LeftAnchor = a.RightAnchor;
            a.RightAnchor = t;
            a.X = r - a.RightAnchor;

            a.LabelToTheLeftOfAnchorCenter = true;
            a.LabelToTheRightOfAnchorCenter = false;
        }

        void GetSuccessorAndPredecessor(int i, out Anchor p, out Anchor s) {
            int predecessor = 10; //the value does not matter, just to silence the compiler
            foreach (LayerEdge ie in properLayeredGraph.InEdges(i))
                predecessor = ie.Source; // there will be only one

            int successor = 10; // the value does not matter, just to silence the compiler
            foreach (LayerEdge ie in properLayeredGraph.OutEdges(i))
                successor = ie.Target; //there will be only one

            //we compare the sum of length of projections of edges (predecessor,i), (i,successor) to x in cases when the label is to the right and to the left
            p = anchors[predecessor];

            s = anchors[successor];
        }

        //void MakeXOfAnchorsPositive(LayerArrays layerArrays)
        //{
        //    //find the minimum of x of anchors
        //    //we don't care about y since they are not part of the unknown variables

        //    double min;
        //    if (anchors.Length > 0)
        //    {
        //        min = anchors[0].X;


        //        for (int i = 1; i < anchors.Length; i++)
        //        {
        //            double x = anchors[i].X;
        //            if (x < min)
        //                min = x;
        //        }

        //        //span of the layers in Y direction
        //        int[][] layers = layerArrays.Layers;
        //        double s = anchors[layers[layers.Length - 1][0]].Y - anchors[layers[0][0]].Y;
        //        //shift anchors to the right that their minimum would be equal to min, unless min is already not less than s.
        //        if (min < s)
        //        {
        //            double shift = s - min;
        //            for (int i = 0; i < anchors.Length; i++)
        //                anchors[i].X += shift;
        //        }
        //    }
        //}


        /// <summary>
        /// Create a DAG from the given graph
        /// </summary>
        void CycleRemoval() {
            var feedbackSet = IntGraph.Edges.Where(e => recoveredLayerArrays.Y[e.Source] < recoveredLayerArrays.Y[e.Target]).ToArray();
            database.AddFeedbackSet(feedbackSet);
        }



        /// <summary>
        /// The function calculates y-layers and x-layers, 
        /// thus, in fact, defining node, including dummy nodes, locations.
        /// </summary>
        /// <param name="layerArrays"></param>
        void CalculateXLayersByGansnerNorth(LayerArrays layerArrays) {
            xLayoutGraph = CreateXLayoutGraph(layerArrays);
            CalculateXLayersByGansnerNorthOnProperLayeredGraph();
        }

        void CalculateXLayersByGansnerNorthOnProperLayeredGraph() {
            int[] xLayers = (new NetworkSimplex(xLayoutGraph, null)).GetLayers();

            //TestYXLayers(layerArrays, xLayers);//this will not be called in the release version

            for (int i = 0; i < database.Anchors.Length; i++)
                anchors[i].X = xLayers[i];
        }

        //[System.Diagnostics.Conditional("TEST_MSAGL")]
        //private void TestYXLayers(LayerArrays layerArrays, int[] xLayers) {
        //    foreach (IntEdge e in this.xLayoutGraph.Edges) {
        //        int s = e.Source; int targ = e.Target;
        //        if (e.Source >= layeredGraph.Nodes.Count) {
        //            if (xLayoutGraph.OutEdges(s).Count != 2 || xLayoutGraph.InEdges(s).Count != 0)
        //                Report("must be two out edges and none incoming");

        //            if (targ >= layeredGraph.Nodes.Count)
        //                Report("an illegal edge");

        //        } else {

        //            if (layerArrays.Y[s] != layerArrays.Y[targ])
        //                Report("layers don't coincide");

        //            if (layerArrays.X[s] - 1 != layerArrays.X[targ])
        //                Report("wrong input");

        //            if (xLayers[s] <= xLayers[targ])
        //                Report("wrong xlayering");

        //        }
        //    }
        //}

        /// <summary>
        /// Creating a proper layered graph, a graph where each 
        /// edge goes only one layer down from the i+1-th layer to the i-th layer.
        /// </summary>
        /// <param name="layering"></param>
        /// <param name="layerArrays"></param>
        void CreateProperLayeredGraph(int[] layering, out LayerArrays layerArrays) {
            int n = layering.Length;
            int nOfVv = 0;

            foreach (PolyIntEdge e in database.SkeletonEdges()) {
                int span = EdgeSpan(layering, e);
                Debug.Assert(span >= 0);
                if (span > 0)
                    e.LayerEdges = new LayerEdge[span];
                int pe = 0; //offset in the string
                if (span > 1) {
                    //we create span-2 dummy nodes and span new edges
                    int d0 = n + nOfVv++;
                    var layerEdge = new LayerEdge(e.Source, d0, e.CrossingWeight);
                    e.LayerEdges[pe++] = layerEdge;
                    //create span-2 internal edges all from dummy nodes
                    for (int j = 0; j < span - 2; j++) {
                        d0++;
                        nOfVv++;
                        layerEdge = new LayerEdge(d0 - 1, d0, e.CrossingWeight);
                        e.LayerEdges[pe++] = layerEdge;
                    }

                    layerEdge = new LayerEdge(d0, e.Target, e.CrossingWeight);
                    e.LayerEdges[pe] = layerEdge;
                } else if (span == 1) {
                    var layerEdge = new LayerEdge(e.Source, e.Target, e.CrossingWeight);
                    e.LayerEdges[pe] = layerEdge;
                }
            }

            var extendedVertexLayering = new int[originalGraph.Nodes.Count + nOfVv];

            foreach (PolyIntEdge e in database.SkeletonEdges())
                if (e.LayerEdges != null) {
                    int l = layering[e.Source];
                    extendedVertexLayering[e.Source] = l--;
                    foreach (LayerEdge le in e.LayerEdges)
                        extendedVertexLayering[le.Target] = l--;
                } else {
                    extendedVertexLayering[e.Source] = layering[e.Source];
                    extendedVertexLayering[e.Target] = layering[e.Target];
                }

            properLayeredGraph =
                new ProperLayeredGraph(new BasicGraph<Node, PolyIntEdge>(database.SkeletonEdges(), layering.Length));
            properLayeredGraph.BaseGraph.Nodes = IntGraph.Nodes;
            layerArrays = new LayerArrays(extendedVertexLayering);
        }

        ConstrainedOrdering constrainedOrdering;

        LayerArrays YLayeringAndOrdering(LayerCalculator layering) {

            int[] yLayers = layering.GetLayers();
            yLayers = ExtendLayeringToUngluedSameLayerVertices(yLayers);
            var layerArrays = new LayerArrays(yLayers);
            //if (!SugiyamaSettings.UseEdgeBundling && (HorizontalConstraints == null || HorizontalConstraints.IsEmpty)) {
            if (HorizontalConstraints == null || HorizontalConstraints.IsEmpty) {
                layerArrays = YLayeringAndOrderingWithoutHorizontalConstraints(layerArrays);
                return layerArrays;
            }
            constrainedOrdering = new ConstrainedOrdering(originalGraph, IntGraph, layerArrays.Y, nodeIdToIndex,
                                                          database, sugiyamaSettings);
            constrainedOrdering.Calculate();
            properLayeredGraph = constrainedOrdering.ProperLayeredGraph;


            // SugiyamaLayoutSettings.ShowDatabase(this.database);
            return constrainedOrdering.LayerArrays;
        }

        LayerArrays YLayeringAndOrderingWithoutHorizontalConstraints(LayerArrays layerArrays) {
            CreateProperLayeredGraph(layerArrays.Y, out layerArrays);
            GetXCoordinatesOfVirtualNodesOfTheProperLayeredGraph(layerArrays);
            OrderLayers(layerArrays);
             MetroMapOrdering.UpdateLayerArrays(properLayeredGraph, layerArrays);
            return layerArrays;
        }

        void GetXCoordinatesOfVirtualNodesOfTheProperLayeredGraph(LayerArrays layerArrays) {
            foreach (var skeletonEdge in database.SkeletonEdges())
                GetXCoordinatesOfVirtualNodesOfTheProperLayeredGraphForSkeletonEdge(skeletonEdge, layerArrays);
        }
      
        void GetXCoordinatesOfVirtualNodesOfTheProperLayeredGraphForSkeletonEdge(PolyIntEdge intEdge, LayerArrays layerArrays) {
            if (intEdge.LayerEdges == null || intEdge.LayerEdges.Count < 2) return;
            var edgeCurve = intEdge.Edge.Curve;
            var layerIndex = layerArrays.Y[intEdge.Source] - 1;//it is the layer of the highest virtual node of the edge
            for (int i = 1; i < intEdge.LayerEdges.Count; i++) {
                var v = intEdge.LayerEdges[i].Source;
                var layerY = layerToRecoveredYCoordinates[layerIndex--];
                var layerLine = new LineSegment(new Point(originalGraph.Left, layerY), new Point(originalGraph.Right, layerY));
                var intersection=Curve.CurveCurveIntersectionOne(edgeCurve, layerLine, false);
               
                skeletonVirtualVerticesToX[v] =intersection.IntersectionPoint.X;
                
            }
        }

        void OrderLayers(LayerArrays layerArrays) {
            foreach (var layer in layerArrays.Layers)
                OrderLayerBasedOnRecoveredXCoords(layer);
        }

        void OrderLayerBasedOnRecoveredXCoords(int[] layer) {
            if (layer.Length <= 1) return;
            var keys = new double[layer.Length];
            for (int i = 0; i < layer.Length; i++) {
                int v = layer[i];
                keys[i] = properLayeredGraph.IsVirtualNode(v)
                              ? skeletonVirtualVerticesToX[v]
                              : originalGraph.Nodes[v].Center.X;
            }

            Array.Sort(keys, layer);

        }


        void CalculateXPositionsByBrandes(LayerArrays layerArrays) {
            XCoordsWithAlignment.CalculateXCoordinates(layerArrays, properLayeredGraph, originalGraph.Nodes.Count,
                                                       database.Anchors, sugiyamaSettings.NodeSeparation);
        }


        void CalculateOriginalGraphBox(out double aspectRatio) {
            aspectRatio = 0;
            if (anchors.Length > 0) {
                var box = new Rectangle(anchors[0].Left, anchors[0].Top, anchors[0].Right, anchors[0].Bottom);

                for (int i = 1; i < anchors.Length; i++) {
                    Anchor a = anchors[i];
                    box.Add(a.LeftTop);
                    box.Add(a.RightBottom);
                }

                aspectRatio = box.Width / box.Height;

                double delta = (box.LeftTop - box.RightBottom).Length / 2;

                var del = new Point(-delta, delta);
                box.Add(box.LeftTop + del);
                box.Add(box.RightBottom - del);

                originalGraph.BoundingBox = box;
            }
        }


        LayerArrays InsertLayersIfNeeded(LayerArrays layerArrays) {
            bool needToInsertLayers = false;
            bool multipleEdges = false;

            InsertVirtualEdgesIfNeeded(layerArrays);

            AnalyzeNeedToInsertLayersAndHasMultiedges(layerArrays, ref needToInsertLayers, ref multipleEdges);

            if (needToInsertLayers) {
                LayerInserter.InsertLayers(ref properLayeredGraph, ref layerArrays, database, IntGraph);
                LayersAreDoubled = true;
            } else if (multipleEdges)
                EdgePathsInserter.InsertPaths(ref properLayeredGraph, ref layerArrays, database, IntGraph);

            RecreateIntGraphFromDataBase();

            return layerArrays;
        }

        bool LayersAreDoubled { get; set; }

        void RecreateIntGraphFromDataBase() {
            var edges = new List<PolyIntEdge>();
            foreach (var list in database.Multiedges.Values)
                edges.AddRange(list);
            IntGraph.SetEdges(edges, IntGraph.NodeCount);
        }

        void AnalyzeNeedToInsertLayersAndHasMultiedges(LayerArrays layerArrays, ref bool needToInsertLayers,
                                                       ref bool multipleEdges) {
            foreach (PolyIntEdge ie in IntGraph.Edges)
                if (ie.HasLabel && layerArrays.Y[ie.Source] != layerArrays.Y[ie.Target]) {
                    //if an edge is a flat edge then
                    needToInsertLayers = true;
                    break;
                }

            if (needToInsertLayers == false && constrainedOrdering == null)
                //if we have constrains the multiple edges have been already represented in layers
                foreach (var kv in database.Multiedges)
                    if (kv.Value.Count > 1) {
                        multipleEdges = true;
                        if (layerArrays.Y[kv.Key.x] - layerArrays.Y[kv.Key.y] == 1) {
                            //there is a multi edge spanning exactly one layer; unfortunately we need to introduce virtual vertices for 
                            //the edges middle points 
                            needToInsertLayers = true;
                            break;
                        }
                    }
        }

        void InsertVirtualEdgesIfNeeded(LayerArrays layerArrays) {
            if (constrainedOrdering != null) //if there are constraints we handle multiedges correctly
                return;

            foreach (var kv in database.Multiedges)
                // If there are an even number of multi-edges between two nodes then
                //  add a virtual edge in the multi-edge dict to improve the placement, but only in case when the edge goes down only one layer.         
                if (kv.Value.Count % 2 == 0 && layerArrays.Y[kv.Key.First] - 1 == layerArrays.Y[kv.Key.Second]) {
                    var newVirtualEdge = new PolyIntEdge(kv.Key.First, kv.Key.Second);
                    newVirtualEdge.Edge = new Edge();
                    newVirtualEdge.IsVirtualEdge = true;
                    kv.Value.Insert(kv.Value.Count / 2, newVirtualEdge);
                    IntGraph.AddEdge(newVirtualEdge);
                }
        }


        int[] ExtendLayeringToUngluedSameLayerVertices(int[] p) {
            VerticalConstraintsForSugiyama vc = VerticalConstraints;
            for (int i = 0; i < p.Length; i++)
                p[i] = p[vc.NodeToRepr(i)];
            return p;
        }

        internal static void CalculateAnchorSizes(Database database, out Anchor[] anchors,
                                                  ProperLayeredGraph properLayeredGraph, GeometryGraph originalGraph,
                                                  BasicGraph<Node, PolyIntEdge> intGraph, SugiyamaLayoutSettings settings) {
            database.Anchors = anchors = new Anchor[properLayeredGraph.NodeCount];

            for (int i = 0; i < anchors.Length; i++)
                anchors[i] = new Anchor(settings.LabelCornersPreserveCoefficient);

            //go over the old vertices
            for (int i = 0; i < originalGraph.Nodes.Count; i++)
                CalcAnchorsForOriginalNode(i, intGraph, anchors, database, settings);

            //go over virtual vertices
            foreach (PolyIntEdge intEdge in database.AllIntEdges)
                if (intEdge.LayerEdges != null) {
                    foreach (LayerEdge layerEdge in intEdge.LayerEdges) {
                        int v = layerEdge.Target;
                        if (v != intEdge.Target) {
                            Anchor anchor = anchors[v];
                            if (!database.MultipleMiddles.Contains(v)) {
                                anchor.LeftAnchor = anchor.RightAnchor = VirtualNodeWidth / 2.0f;
                                anchor.TopAnchor = anchor.BottomAnchor = VirtualNodeHeight(settings) / 2.0f;
                            } else {
                                anchor.LeftAnchor = anchor.RightAnchor = VirtualNodeWidth * 4;
                                anchor.TopAnchor = anchor.BottomAnchor = VirtualNodeHeight(settings) / 2.0f;
                            }
                        }
                    }
                    //fix label vertices      
                    if (intEdge.HasLabel) {
                        int lj = intEdge.LayerEdges[intEdge.LayerEdges.Count / 2].Source;
                        Anchor a = anchors[lj];
                        double w = intEdge.LabelWidth, h = intEdge.LabelHeight;
                        a.RightAnchor = w;
                        a.LeftAnchor = VirtualNodeWidth * 8;

                        if (a.TopAnchor < h / 2.0)
                            a.TopAnchor = a.BottomAnchor = h / 2.0;

                        a.LabelToTheRightOfAnchorCenter = true;
                    }
                }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>the height of the graph+spaceBeforeMargins</returns>
        internal static void CalcInitialYAnchorLocations(LayerArrays layerArrays, double spaceBeforeMargins,
                                                         GeometryGraph originalGraph, Database database,
                                                         BasicGraphOnEdges<PolyIntEdge> intGraph,
                                                         SugiyamaLayoutSettings settings,
                                                         bool layersAreDoubled) {
            Anchor[] anchors = database.Anchors;
            double ymax = originalGraph.Margins + spaceBeforeMargins; //setting up y coord - going up by y-layers
            int i = 0;
            foreach (var yLayer in layerArrays.Layers) {
                double bottomAnchorMax = 0;
                double topAnchorMax = 0;
                foreach (int j in yLayer) {
                    Anchor p = anchors[j];
                    if (p.BottomAnchor > bottomAnchorMax)
                        bottomAnchorMax = p.BottomAnchor;
                    if (p.TopAnchor > topAnchorMax)
                        topAnchorMax = p.TopAnchor;
                }

                MakeVirtualNodesHigh(yLayer, bottomAnchorMax, topAnchorMax, originalGraph.Nodes.Count, database.Anchors);

                double flatEdgesHeight = SetFlatEdgesForLayer(database, layerArrays, i, intGraph, settings, ymax);

                double y = ymax + bottomAnchorMax + flatEdgesHeight;
                foreach (int j in yLayer)
                    anchors[j].Y = y;
                double layerSep = settings.ActualLayerSeparation(layersAreDoubled);
                ymax = y + topAnchorMax + layerSep;
                i++;
            }
            SetFlatEdgesForLayer(database, layerArrays, i, intGraph, settings, ymax);
        }

        static double SetFlatEdgesForLayer(Database database, LayerArrays layerArrays, int i,
                                           BasicGraphOnEdges<PolyIntEdge> intGraph, SugiyamaLayoutSettings settings, double ymax) {
            double flatEdgesHeight = 0;
            if (i > 0) {
                //looking for flat edges on the previous level                
                //we stack labels of multiple flat edges on top of each other
                IEnumerable<IntPair> flatPairs = GetFlatPairs(layerArrays.Layers[i - 1], layerArrays.Y,
                                                              intGraph);
                if (flatPairs.Any()) {
                    double dyOfFlatEdge = settings.LayerSeparation / 3;
                    double ym = ymax;
                    flatEdgesHeight =
                        (from pair in flatPairs
                         select SetFlatEdgesLabelsHeightAndPositionts(pair, ym, dyOfFlatEdge, database)).
                            Max();
                }
            }
            return flatEdgesHeight;
        }

        static double SetFlatEdgesLabelsHeightAndPositionts(IntPair pair, double ymax, double dy, Database database) {
            double height = 0;
            List<PolyIntEdge> list = database.GetMultiedge(pair);
            foreach (PolyIntEdge edge in list) {
                height += dy;
                Label label = edge.Edge.Label;
                if (label != null) {
                    label.Center = new Point(label.Center.X, ymax + height + label.Height / 2);
                    height += label.Height;
                }
            }
            return height;
        }


        static IEnumerable<IntPair> GetFlatPairs(int[] layer, int[] layering, BasicGraphOnEdges<PolyIntEdge> intGraph) {
            return new Set<IntPair>(from v in layer
                                    where v < intGraph.NodeCount
                                    from edge in intGraph.OutEdges(v)
                                    where layering[edge.Source] == layering[edge.Target]
                                    select new IntPair(edge.Source, edge.Target));
        }

        static void MakeVirtualNodesHigh(int[] yLayer, double bottomAnchorMax, double topAnchorMax,
                                         int originalNodeCount, Anchor[] anchors) {
            if (LayerIsOriginal(yLayer, originalNodeCount))
                foreach (int j in yLayer)
                    if (j >= originalNodeCount) {
                        Anchor p = anchors[j];
                        p.BottomAnchor = bottomAnchorMax;
                        p.TopAnchor = topAnchorMax;
                    }
        }

        static bool LayerIsOriginal(int[] yLayer, int origNodeCount) {
            foreach (int j in yLayer)
                if (j < origNodeCount)
                    return true;
            return false;
        }

        static void CalcAnchorsForOriginalNode(int i, BasicGraph<Node, PolyIntEdge> intGraph, Anchor[] anchors,
                                               Database database, SugiyamaLayoutSettings settings) {
            double leftAnchor = 0;
            double rightAnchor = leftAnchor;
            double topAnchor = 0;
            double bottomAnchor = topAnchor;

            //that's what we would have without the label and multiedges 

            if (intGraph.Nodes != null) {
                Node node = intGraph.Nodes[i];
                ExtendStandardAnchors(ref leftAnchor, ref rightAnchor, ref topAnchor, ref bottomAnchor, node);
            }

            RightAnchorMultiSelfEdges(i, ref rightAnchor, ref topAnchor, ref bottomAnchor, database, settings);

            double hw = settings.MinNodeWidth / 2;
            if (leftAnchor < hw)
                leftAnchor = hw;
            if (rightAnchor < hw)
                rightAnchor = hw;
            double hh = settings.MinNodeHeight / 2;

            if (topAnchor < hh)
                topAnchor = hh;
            if (bottomAnchor < hh)
                bottomAnchor = hh;

            anchors[i] = new Anchor(leftAnchor, rightAnchor, topAnchor, bottomAnchor, intGraph.Nodes[i],
                                    settings.LabelCornersPreserveCoefficient) { Padding = intGraph.Nodes[i].Padding };
#if TEST_MSAGL
            anchors[i].UserData = intGraph.Nodes[i].UserData;
#endif
        }

        static void RightAnchorMultiSelfEdges(int i, ref double rightAnchor, ref double topAnchor,
                                              ref double bottomAnchor, Database database,
                                              SugiyamaLayoutSettings settings) {
            double delta = WidthOfSelfEdge(database, i, ref rightAnchor, ref topAnchor, ref bottomAnchor, settings);

            rightAnchor += delta;
        }

        static double WidthOfSelfEdge(Database database, int i, ref double rightAnchor, ref double topAnchor,
                                      ref double bottomAnchor, SugiyamaLayoutSettings settings) {
            double delta = 0;
            List<PolyIntEdge> multiedges = database.GetMultiedge(i, i);
            //it could be a multiple self edge
            if (multiedges.Count > 0) {
                foreach (PolyIntEdge e in multiedges)
                    if (e.Edge.Label != null) {
                        rightAnchor += e.Edge.Label.Width;
                        if (topAnchor < e.Edge.Label.Height / 2.0)
                            topAnchor = bottomAnchor = e.Edge.Label.Height / 2.0f;
                    }

                delta += (settings.NodeSeparation + settings.MinNodeWidth) * multiedges.Count;
            }
            return delta;
        }

        static void ExtendStandardAnchors(ref double leftAnchor, ref double rightAnchor, ref double topAnchor,
                                          ref double bottomAnchor, Node node) {
            double w = node.Width;
            double h = node.Height;


            w /= 2.0;
            h /= 2.0;


            rightAnchor = leftAnchor = w;
            topAnchor = bottomAnchor = h;
        }

        ///// <summary>
        ///// A quote from Gansner93.
        ///// The method involves constructing an auxiliary graph as illustrated in figure 4-2.
        ///// This transformation is the graphical analogue of the algebraic 
        ///// transformation mentioned above for removing the absolute values 
        ///// from the optimization problem. The nodes of the auxiliary graph Gў are the nodes of 
        ///// the original graph G plus, for every edge e in G, there is a new node ne. 
        ///// There are two kinds of edges in Gў. One edge class encodes the 
        ///// cost of the original edges. Every edge e = (u,v) in G is replaced by two edges (ne ,u)
        ///// and (ne, v) with d = 0 and w = w(e)W(e). The other class of edges separates nodes in the same layer. 
        ///// If v is the left neighbor of w, then Gў has an edge f = e(v,w) with d( f ) = r(v,w) and 
        ///// w( f ) = 0. This edge forces the nodes to be sufficiently 
        ///// separated but does not affect the cost of the layout.
        XLayoutGraph CreateXLayoutGraph(LayerArrays layerArrays) {
            int nOfVerts = properLayeredGraph.NodeCount;

            //create edges of XLayoutGraph
            var edges = new List<PolyIntEdge>();

            foreach (LayerEdge e in properLayeredGraph.Edges) {
                var n1 = new PolyIntEdge(nOfVerts, e.Source);
                var n2 = new PolyIntEdge(nOfVerts, e.Target);
                n1.Weight = n2.Weight = 1;
                n1.Separation = 0; //these edge have 0 separation
                n2.Separation = 0;
                nOfVerts++;
                edges.Add(n1);
                edges.Add(n2);
            }

            foreach (var layer in layerArrays.Layers)
                for (int i = layer.Length - 1; i > 0; i--) {
                    int source = layer[i];
                    int target = layer[i - 1];
                    var ie = new PolyIntEdge(source, target);
                    Anchor sourceAnchor = database.Anchors[source];
                    Anchor targetAnchor = database.Anchors[target];

                    double sep = sourceAnchor.LeftAnchor + targetAnchor.RightAnchor + sugiyamaSettings.NodeSeparation;

                    ie.Separation = (int)(sep + 1);

                    edges.Add(ie);
                }

            var ret = new XLayoutGraph(IntGraph, properLayeredGraph, layerArrays, edges, nOfVerts);
            ret.SetEdgeWeights();
            return ret;
        }

        static LayerArrays RecoverOriginalLayersAndSettings(GeometryGraph geometryGraph, out SugiyamaLayoutSettings sugiyamaLayoutSettings) {
            sugiyamaLayoutSettings = new SugiyamaLayoutSettings();

            return RecoverOriginalHorizontalLayers(geometryGraph, sugiyamaLayoutSettings) ??
                   RecoverOriginalVerticalLayers(geometryGraph, sugiyamaLayoutSettings);
        }

        static LayerArrays RecoverOriginalHorizontalLayers(GeometryGraph geometryGraph, SugiyamaLayoutSettings sugiyamaLayoutSettings) {
            var nodes = geometryGraph.Nodes;
            var list = new List<int>();
            for (int i = 0; i < nodes.Count; i++)
                list.Add(i);
            var layers = list.GroupBy(i => nodes[i].Center.Y);
            var layerList = new List<int[]>();
            foreach (var layer in layers)
                layerList.Add(layer.ToArray());

            sugiyamaLayoutSettings.LayerSeparation = double.PositiveInfinity;

            layerList = new List<int[]>(layerList.OrderBy(l => nodes[l[0]].Center.Y));
            //check that the layers are separated
            for (int i = 0; i < layerList.Count - 1; i++) {
                var topOfI = layerList[i].Max(j => nodes[j].BoundingBox.Top);
                var bottomOfINext = layerList[i + 1].Min(j => nodes[j].BoundingBox.Bottom);
                var layerSep = bottomOfINext - topOfI;
                if (layerSep <= 0) {
                    sugiyamaLayoutSettings.Transformation = null;
                    return null;
                }
                sugiyamaLayoutSettings.LayerSeparation = Math.Min(sugiyamaLayoutSettings.LayerSeparation, layerSep);
            }
            var nodesToLayers = new int[nodes.Count];

            for (int i = 0; i < layerList.Count; i++) {
                var layer = layerList[i];
                for (int j = 0; j < layer.Length; j++)
                    nodesToLayers[layer[j]] = i;
            }

            sugiyamaLayoutSettings.Transformation = geometryGraph.Edges.Any(e => e.Source.Center.Y > e.Target.Center.Y)
                                 ? new PlaneTransformation()
                                 : PlaneTransformation.Rotation(Math.PI);


            return new LayerArrays(nodesToLayers);
        }

        static LayerArrays RecoverOriginalVerticalLayers(GeometryGraph geometryGraph, SugiyamaLayoutSettings sugiyamaLayoutSettings) {

            var nodes = geometryGraph.Nodes;
            var list = new List<int>();
            for (int i = 0; i < geometryGraph.Nodes.Count; i++) list.Add(i);
            var layers = list.GroupBy(i => nodes[i].Center.X);
            var layerList = new List<int[]>();
            foreach (var layer in layers) {
                layerList.Add(layer.ToArray());
            }
            layerList = new List<int[]>(layerList.OrderBy(l => nodes[l[0]].Center.X));
            sugiyamaLayoutSettings.LayerSeparation = double.PositiveInfinity;
            //check that the layers are separated
            for (int i = 0; i < layerList.Count - 1; i++) {
                var rightOfI = layerList[i].Max(j => nodes[j].BoundingBox.Right);
                var leftOfNext = layerList[i + 1].Min(j => nodes[j].BoundingBox.Left);
                var layerSep = leftOfNext - rightOfI;
                if (layerSep <= 0) {
                    return null;
                }
                sugiyamaLayoutSettings.LayerSeparation = Math.Min(sugiyamaLayoutSettings.LayerSeparation, layerSep);
            }
            var nodesToLayers = new int[nodes.Count];

            for (int i = 0; i < layerList.Count; i++) {
                var layer = layerList[i];
                for (int j = 0; j < layer.Length; j++)
                    nodesToLayers[layer[j]] = i;
            }

            sugiyamaLayoutSettings.Transformation = geometryGraph.Edges.Any(e => e.Source.Center.X > e.Target.Center.X)
                                                        ? PlaneTransformation.Rotation(-Math.PI/2)
                                                        : PlaneTransformation.Rotation(Math.PI/2);


            return new LayerArrays(nodesToLayers);
        }

        public LayeredLayoutEngine GetEngine() {
            if (recoveredLayerArrays == null) return null; //it is not a layered layout
            if (!sugiyamaSettings.Transformation.IsIdentity) {
                originalGraph.Transform(sugiyamaSettings.Transformation.Inverse);
            }

            FillLayersToRecoveredYCoordinates();
            CycleRemoval();
            Run();
            var engine= CreateEngine();
            if (!sugiyamaSettings.Transformation.IsIdentity) {
                originalGraph.Transform(sugiyamaSettings.Transformation);
            }
            return engine;
        }

        LayeredLayoutEngine CreateEngine() {
            return new LayeredLayoutEngine(engineLayerArrays, originalGraph, properLayeredGraph, 
                sugiyamaSettings, database, IntGraph, nodeIdToIndex, GluedDagSkeletonForLayering, 
                LayersAreDoubled, constrainedOrdering, Brandes, xLayoutGraph);
        }
    }
}
