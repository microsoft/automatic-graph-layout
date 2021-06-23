using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using Microsoft.Msagl.Routing.Spline.Bundling;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// This is the top level class driving the calculation
    /// </summary>
    internal class LayeredLayoutEngine : AlgorithmBase {
#if TEST_MSAGL
        /// <summary>
        /// the Log property
        /// </summary>
        SugiyamaLayoutLogger SugiyamaLayoutLogger { get; set; }
#endif

        /// <summary>
        /// If set on always forces using fast but not optimal Brandes algorithm for x-coordinate assignment.
        /// </summary>
        bool Brandes { get; set; }

        LayerArrays engineLayerArrays;

        readonly GeometryGraph originalGraph;

        ProperLayeredGraph properLayeredGraph;

        /// <summary>
        /// the sugiyama layout settings responsible for the algorithm parameters
        /// </summary>
        readonly SugiyamaLayoutSettings sugiyamaSettings;
        
        Database database;

        /// <summary>
        /// Keeps assorted data associated with the graph.
        /// </summary>
        internal Database Database {
            set { database = value; }
            get { return database; }
        }

        /// <summary>
        /// it is a special recovery constructor to recreate the engine from the recovery engine
        /// </summary>
        internal LayeredLayoutEngine(LayerArrays engineLayerArrays, GeometryGraph originalGraph, ProperLayeredGraph properLayeredGraph, SugiyamaLayoutSettings sugiyamaSettings, Database database, BasicGraph<Node, PolyIntEdge> intGraph, Dictionary<Node, int> nodeIdToIndex, BasicGraph<Node, PolyIntEdge> gluedDagSkeletonForLayering, bool layersAreDoubled, ConstrainedOrdering constrainedOrdering, bool brandes, XLayoutGraph xLayoutGraph) {
            this.engineLayerArrays = engineLayerArrays;
            this.originalGraph = originalGraph;
            this.properLayeredGraph = properLayeredGraph;
            this.sugiyamaSettings = sugiyamaSettings;
            this.database = database;
            IntGraph = intGraph;
            this.nodeIdToIndex = nodeIdToIndex;
            GluedDagSkeletonForLayering = gluedDagSkeletonForLayering;
            LayersAreDoubled = layersAreDoubled;
            this.constrainedOrdering = constrainedOrdering;
            Brandes = brandes;
            anchors = database.anchors;
            this.xLayoutGraph = xLayoutGraph;
        }

       
        

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
            return settings.MinNodeHeight*1.5f/8;
        }

        
      

        internal BasicGraph<Node, PolyIntEdge> IntGraph; //the input graph

        BasicGraph<Node, PolyIntEdge> GluedDagSkeletonForLayering { get; set; }

        //the graph obtained after X coord calculation
        XLayoutGraph xLayoutGraph;
        readonly Dictionary<Node, int> nodeIdToIndex;
        Anchor[] anchors;

        /// <summary>
        /// constructor
        /// </summary>
        internal LayeredLayoutEngine(GeometryGraph originalGraph, BasicGraph<Node, PolyIntEdge> graph,
                                     Dictionary<Node, int> nodeIdToIndex, SugiyamaLayoutSettings settings) {
            if (originalGraph != null) {
                this.originalGraph = originalGraph;
                sugiyamaSettings = settings;
                IntGraph = graph;
                Database = new Database();
                this.nodeIdToIndex = nodeIdToIndex;
                foreach (PolyIntEdge e in graph.Edges)
                    database.RegisterOriginalEdgeInMultiedges(e);
#if TEST_MSAGL
                if (sugiyamaSettings.Reporting && SugiyamaLayoutLogger == null)
                    SugiyamaLayoutLogger = new SugiyamaLayoutLogger();
#endif
                CycleRemoval();
            }
        }

        /// <summary>
        /// Simpler constructor which initializes the internal graph representation automatically
        /// </summary>
        /// <param name="originalGraph"></param>
        /// <param name="settings"></param>
        internal LayeredLayoutEngine(GeometryGraph originalGraph, SugiyamaLayoutSettings settings) {
            if (originalGraph != null) {
                //enumerate the nodes - maps node indices to strings 
                nodeIdToIndex = new Dictionary<Node, int>();

                IList<Node> nodes = originalGraph.Nodes;

                int index = 0;
                foreach (Node n in nodes) {
                    nodeIdToIndex[n] = index;
                    index++;
                }

                var edges = originalGraph.Edges;

                var intEdges = new PolyIntEdge[edges.Count];
                int i = 0;
                foreach(var edge in edges){
                
                    if (edge.Source == null || edge.Target == null)
                        throw new InvalidOperationException(); //"creating an edge with null source or target");

                    var intEdge = new PolyIntEdge(nodeIdToIndex[edge.Source], nodeIdToIndex[edge.Target], edge);

                    intEdges[i] = intEdge;
                    i++;
                }

                IntGraph = new BasicGraph<Node, PolyIntEdge>(intEdges, originalGraph.Nodes.Count) {Nodes = nodes};
                this.originalGraph = originalGraph;
                sugiyamaSettings = settings;
                Database = new Database();
                foreach (PolyIntEdge e in IntGraph.Edges)
                    database.RegisterOriginalEdgeInMultiedges(e);

#if TEST_MSAGL
                if (sugiyamaSettings.Reporting && SugiyamaLayoutLogger == null)
                    SugiyamaLayoutLogger = new SugiyamaLayoutLogger();
#endif
                CycleRemoval();
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
        override protected void RunInternal() {
#if TEST_MSAGL
            Timer t = null;
            if (sugiyamaSettings.Reporting) {
                Report("removing cycles ... ");
                t = new Timer();
                t.Start();
            }
#endif
            if (originalGraph.Nodes.Count > 0) {
                engineLayerArrays = CalculateLayers();
  
                if (!sugiyamaSettings.LayeringOnly)
                    RunPostLayering();
            } else
                originalGraph.boundingBox.SetToEmpty();

#if TEST_MSAGL
            if (sugiyamaSettings.Reporting) {
                t.Stop();
                Report(String.Format(CultureInfo.InvariantCulture, "done for {0}, nodes {1} edges {2}", t.Duration,
                                     IntGraph.NodeCount, IntGraph.Edges.Count));
            }

            //SugiyamaLayoutSettings.ShowDatabase(database);
#endif
        }

        void RunPostLayering() {
#if TEST_MSAGL
            Timer t = null;
            if (sugiyamaSettings.Reporting) {
                Report("Calculating edge splines... ");
                t = new Timer();
                t.Start();
            }
#endif
            EdgeRoutingSettings routingSettings = sugiyamaSettings.EdgeRoutingSettings;
            var mode = routingSettings.EdgeRoutingMode;
            if (constrainedOrdering != null) //we switch to splines when there are constraints, since the ordering of virtual nodes sometimes does not do a good job
                mode = EdgeRoutingMode.Spline;
            switch (mode) {
                case EdgeRoutingMode.SugiyamaSplines:
                    CalculateEdgeSplines();
                    break;
                case EdgeRoutingMode.StraightLine:
                    StraightLineEdges.SetStraightLineEdgesWithUnderlyingPolylines(originalGraph);
                    SetLabels();
                    break;
                case EdgeRoutingMode.Spline:
                    double padding = sugiyamaSettings.NodeSeparation/4;
                    double loosePadding = SplineRouter.ComputeLooseSplinePadding(sugiyamaSettings.NodeSeparation,
                                                                                 padding);
                    var router = new SplineRouter(originalGraph, padding, loosePadding, (Math.PI / 6), null);
                    router.Run();
                    SetLabels();
                    break;
                case EdgeRoutingMode.SplineBundling:
                    double coneAngle = routingSettings.ConeAngle;
                    padding = sugiyamaSettings.NodeSeparation/20;
                    loosePadding = SplineRouter.ComputeLooseSplinePadding(sugiyamaSettings.NodeSeparation, padding)*2;
                    if (sugiyamaSettings.EdgeRoutingSettings.BundlingSettings == null)
                        sugiyamaSettings.EdgeRoutingSettings.BundlingSettings = new BundlingSettings();
                    var br = new SplineRouter(originalGraph, padding, loosePadding, coneAngle,
                                              sugiyamaSettings.EdgeRoutingSettings.BundlingSettings);
                    br.Run();
                    SetLabels();
                    break;
                case EdgeRoutingMode.Rectilinear:
                case EdgeRoutingMode.RectilinearToCenter:
                    //todo: are these good values?
                    var rer = new RectilinearEdgeRouter(originalGraph, sugiyamaSettings.NodeSeparation/3,
                                                        sugiyamaSettings.NodeSeparation/4,
                                                        true,
                                                        sugiyamaSettings.EdgeRoutingSettings.UseObstacleRectangles);
                    rer.RouteToCenterOfObstacles = routingSettings.EdgeRoutingMode ==
                                                   EdgeRoutingMode.RectilinearToCenter;
                    rer.BendPenaltyAsAPercentageOfDistance = routingSettings.BendPenalty;
                    rer.Run();
                    SetLabels();
                    break;
            }
            originalGraph.BoundingBox = originalGraph.PumpTheBoxToTheGraphWithMargins();

#if TEST_MSAGL
            if (sugiyamaSettings.Reporting) {
                t.Stop();
                Report(String.Format(CultureInfo.InvariantCulture, "splines done for {0}", t.Duration));
            }
#endif
        }

        void SetLabels() {
            var edgeLabeller = new EdgeLabelPlacement(originalGraph);
            edgeLabeller.Run();
        }


        public void IncrementalRun(Node node) {
            PushLayers(node);
           // LayoutAlgorithmSettings.ShowDatabase(database);
            RunPostLayering();
        }

        void PushLayers(Node node) {
            int nodeIndex = IntGraph.Nodes.TakeWhile(n => node != n).Count();

            Anchor nodeAnchor = anchors[nodeIndex];


            double delWidth = 0.5*(node.BoundaryCurve.BoundingBox.Width - nodeAnchor.Width);

            double delHight = 0.5*(node.BoundaryCurve.BoundingBox.Height - nodeAnchor.Height);

            nodeAnchor.RightAnchor += delWidth;
            nodeAnchor.LeftAnchor += delWidth;
            nodeAnchor.TopAnchor += delHight;
            nodeAnchor.BottomAnchor += delHight;
            nodeAnchor.Node = node;

            PushDownLowerLayers(delHight, nodeIndex);
            PushUpUpperLayers(delHight, nodeIndex);

            if (xLayoutGraph != null) {
                FixEdgeSeparation(nodeIndex);
                CalculateXLayersByGansnerNorthOnProperLayeredGraph();
            } else
                CalculateXPositionsByBrandes(engineLayerArrays);

            for (int i = 0; i < IntGraph.NodeCount; i++) {
                Node n = IntGraph.Nodes[i];
                Anchor a = anchors[i];
                n.Center = a.Origin;
                a.Node = n;
            }
        }

        void FixEdgeSeparation(int nodeIndex) {
            int nodeLayerIndex = engineLayerArrays.Y[nodeIndex];
            int[] layer = engineLayerArrays.Layers[nodeLayerIndex];
            int nodePosition = engineLayerArrays.X[nodeIndex];
            Anchor a = anchors[nodeIndex];
            if (nodePosition > 0) {
                int target = layer[nodePosition - 1];
                foreach (PolyIntEdge ie in xLayoutGraph.OutEdges(nodeIndex))
                    if (ie.Target == target) {
                        ie.Separation =
                            (int) (sugiyamaSettings.NodeSeparation + a.LeftAnchor + anchors[target].RightAnchor + 1);
                        break;
                    }
            }
            if (nodePosition < layer.Length - 1) {
                int source = layer[nodePosition + 1];
                foreach (PolyIntEdge ie in xLayoutGraph.InEdges(nodeIndex))
                    if (ie.Source == source) {
                        ie.Separation =
                            (int) (sugiyamaSettings.NodeSeparation + a.RightAnchor + anchors[source].LeftAnchor + 1);
                        break;
                    }
            }
        }


        void MoveNode(Point del, int i) {
            if (i < IntGraph.Nodes.Count) {
                Node node = IntGraph.Nodes[i];
                node.Center += del;
                anchors[i].Node = node;
            }
        }


        void PushUpUpperLayers(double delHight, int nodeIndex) {
            var delta = new Point(0, delHight);
            int layerIndex = engineLayerArrays.Y[nodeIndex];
            for (int i = layerIndex + 1; i < engineLayerArrays.Layers.Length; i++) {
                int[] layer = engineLayerArrays.Layers[i];
                foreach (int j in layer) {
                    MoveNode(delta, j);
                    anchors[j].Move(delta);
                }
            }
        }

        void PushDownLowerLayers(double delHight, int nodeIndex) {
            var delta = new Point(0, -delHight);
            int layerIndex = engineLayerArrays.Y[nodeIndex];
            for (int i = 0; i < layerIndex; i++) {
                int[] layer = engineLayerArrays.Layers[i];
                foreach (int j in layer) {
                    MoveNode(delta, j);
                    anchors[j].Move(delta);
                }
            }
        }

        LayerArrays CalculateLayers() {
            CreateGluedDagSkeletonForLayering();

            #region reporting

#if TEST_MSAGL
            Timer t = null;
            if (sugiyamaSettings.Reporting) {
                Report("calculating layers ... ");
                t = new Timer();
                t.Start();
            }
#endif

            #endregion

            LayerArrays layerArrays = CalculateLayerArrays();

            UpdateNodePositionData();

            #region reporting

#if TEST_MSAGL
            if (sugiyamaSettings.Reporting) {
                t.Stop();
                Report(String.Format(CultureInfo.CurrentCulture, "done with layers for {0}\n", t.Duration));
            }
#endif

            #endregion

            return layerArrays;
        }


        /// <summary>
        /// pushes positions from the anchors to node Centers
        /// </summary>
        void UpdateNodePositionData() {
            TryToSatisfyMinWidhtAndMinHeight();

            for (int i = 0; i < IntGraph.Nodes.Count && i < database.Anchors.Length; i++)
                IntGraph.Nodes[i].Center = database.Anchors[i].Origin;

            if (this.sugiyamaSettings.GridSizeByX > 0)
            {
                for (int i = 0; i < originalGraph.Nodes.Count; i++)
                {
                    SnapLeftSidesOfTheNodeToGrid(i, this.sugiyamaSettings.GridSizeByX);
                }
            }

        }

        private void SnapLeftSidesOfTheNodeToGrid(int i, double gridSize)
        {
            Node node = IntGraph.Nodes[i];

            Anchor anchor = database.Anchors[i];
            anchor.LeftAnchor -= gridSize / 2;
            anchor.RightAnchor -= gridSize / 2;

            double left = node.BoundingBox.Left;
            int k = (int)(left / gridSize);
            double delta = left - k * gridSize;

            if (Math.Abs(delta) < 0.001)
            {
                return;
            }
            // we are free to shift at least gridSize horizontally
            // find the minimal shift

            if (Math.Abs(delta) <= gridSize / 2)
            {
                node.Center += new Point(-delta, 0); // shifting to the left
                
            }
            else
            {
                node.Center += new Point(gridSize - delta, 0); // shifting to the right
            }
            anchor.X = node.Center.X;
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
                desiredSpan.AddValue(node.BoundingBox.Width/2);
                desiredSpan.AddValue(sugiyamaSettings.MinimalWidth - node.BoundingBox.Width/2);
            }

            var currentSpan = new RealNumberSpan();
            foreach (Anchor anchor in NodeAnchors())
                currentSpan.AddValue(anchor.X);

            if (currentSpan.Length > ApproximateComparer.DistanceEpsilon) {
                double stretch = desiredSpan.Length/currentSpan.Length;
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
                desiredSpan.AddValue(node.BoundingBox.Height/2);
                desiredSpan.AddValue(sugiyamaSettings.MinimalHeight - node.BoundingBox.Height/2);
            }

            var currentSpan = new RealNumberSpan();
            foreach (Anchor anchor in NodeAnchors())
                currentSpan.AddValue(anchor.Y);

            if (currentSpan.Length > ApproximateComparer.DistanceEpsilon) {
                double stretch = desiredSpan.Length/currentSpan.Length;
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


        void StretchToDesiredAspectRatio(double aspectRatio, double desiredAR) {
            if (aspectRatio > desiredAR)
                StretchInYDirection(aspectRatio/desiredAR);
            else if (aspectRatio < desiredAR)
                StretchInXDirection(desiredAR/aspectRatio);
        }

        void StretchInYDirection(double scaleFactor) {
            double center = (originalGraph.BoundingBox.Top + originalGraph.BoundingBox.Bottom)/2;
            foreach (Anchor a in Database.Anchors) {
                a.BottomAnchor *= scaleFactor;
                a.TopAnchor *= scaleFactor;
                a.Y = center + scaleFactor*(a.Y - center);
            }
            double h = originalGraph.Height*scaleFactor;
            originalGraph.BoundingBox = new Rectangle(originalGraph.BoundingBox.Left, center + h/2,
                                                      originalGraph.BoundingBox.Right, center - h/2);
        }

        void StretchInXDirection(double scaleFactor) {
            double center = (originalGraph.BoundingBox.Left + originalGraph.BoundingBox.Right)/2;
            foreach (Anchor a in Database.Anchors) {
                a.LeftAnchor *= scaleFactor;
                a.RightAnchor *= scaleFactor;
                a.X = center + scaleFactor*(a.X - center);
            }
            double w = originalGraph.Width*scaleFactor;
            originalGraph.BoundingBox = new Rectangle(center - w/2, originalGraph.BoundingBox.Top, center + w/2,
                                                      originalGraph.BoundingBox.Bottom);
        }

        LayerArrays CalculateLayerArrays() {
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
#if !SHARPKIT
            else {
                string s = Environment.GetEnvironmentVariable("Brandes");
                if (!String.IsNullOrEmpty(s) && String.Compare(s, "on", true, CultureInfo.CurrentCulture) == 0)
                    Brandes = true;
            }
#endif
        }

        void StraightensShortEdges() {
            for (; StraightenEdgePaths();) {}
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

            double x = (iAnchor.Y - upper.Y)*(lower.X - upper.X)/(lower.Y - upper.Y) + upper.X;
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
                YLayeringAndOrdering(new NetworkSimplexForGeneralGraph(GluedDagSkeletonForLayering, CancelToken));
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
            VerticalConstraintsForSugiyama verticalConstraints = sugiyamaSettings.VerticalConstraints;
            IEnumerable<IEdge> feedbackSet = verticalConstraints.IsEmpty
                                                 ? CycleRemoval<PolyIntEdge>.GetFeedbackSet(IntGraph)
                                                 : verticalConstraints.GetFeedbackSet(IntGraph, nodeIdToIndex);

            database.AddFeedbackSet(feedbackSet);
        }


#if TEST_MSAGL
        /// <summary>
        /// Prints the function to the console and to the log if the log file name is given
        /// </summary>
        /// <param name="s"></param>
        void Report(string s) {
            SugiyamaLayoutLogger.Write(s);
        }
#endif

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
        void CreaeteProperLayeredGraph(int[] layering, out LayerArrays layerArrays) {
            int n = layering.Length;
            int nOfVV = 0;


            foreach (PolyIntEdge e in database.SkeletonEdges()) {
                int span = EdgeSpan(layering, e);

                Debug.Assert(span >= 0);

                if (span > 0)
                    e.LayerEdges = new LayerEdge[span];
                int pe = 0; //offset in the string

                if (span > 1) {
                   
                    //we create span-2 dummy nodes and span new edges
                    int d0 = n + nOfVV++;
                   
                    var layerEdge = new LayerEdge(e.Source, d0, e.CrossingWeight,e.Weight);

                    e.LayerEdges[pe++] = layerEdge;


                    //create span-2 internal edges all from dummy nodes
                    for (int j = 0; j < span - 2; j++) {
                        d0++;
                        nOfVV++;
                        layerEdge = new LayerEdge(d0 - 1, d0, e.CrossingWeight,e.Weight);
                        e.LayerEdges[pe++] = layerEdge;
                    }

                    layerEdge = new LayerEdge(d0, e.Target, e.CrossingWeight, e.Weight);
                    e.LayerEdges[pe] = layerEdge;
                } else if (span == 1) {
                    var layerEdge = new LayerEdge(e.Source, e.Target, e.CrossingWeight, e.Weight);
                    e.LayerEdges[pe] = layerEdge;
                }
            }

            var extendedVertexLayering = new int[originalGraph.Nodes.Count + nOfVV];

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
            #region reporting

#if TEST_MSAGL
            Timer t = null;
            if (sugiyamaSettings.Reporting) {
                t = new Timer();
                Report("ylayering ... ");
                t.Start();
            }
#endif

            #endregion

            int[] yLayers = layering.GetLayers();
            Balancing.Balance(GluedDagSkeletonForLayering, yLayers, GetNodeCountsOfGluedDag(), null);
            yLayers = ExtendLayeringToUngluedSameLayerVertices(yLayers);

            #region reporting

#if TEST_MSAGL
            if (sugiyamaSettings.Reporting) {
                t.Stop();
                Report(String.Format(CultureInfo.CurrentCulture, "{0}\n", t.Duration));
                Report("ordering ... ");
                t.Start();
            }
#endif

            #endregion

            var layerArrays = new LayerArrays(yLayers);
            //if (!SugiyamaSettings.UseEdgeBundling && (HorizontalConstraints == null || HorizontalConstraints.IsEmpty)) {
            if (HorizontalConstraints == null || HorizontalConstraints.IsEmpty) {
                layerArrays = YLayeringAndOrderingWithoutHorizontalConstraints(layerArrays);

                #region reporting

#if TEST_MSAGL
                if (sugiyamaSettings.Reporting) {
                    t.Stop();
                    Report(String.Format(CultureInfo.CurrentCulture, "{0}\n", t.Duration));
                }
#endif

                #endregion

                return layerArrays;
            }
            constrainedOrdering = new ConstrainedOrdering(originalGraph, IntGraph, layerArrays.Y, nodeIdToIndex,
                                                          database, sugiyamaSettings);
            constrainedOrdering.Calculate();
            properLayeredGraph = constrainedOrdering.ProperLayeredGraph;

            #region reporting

#if TEST_MSAGL
            if (sugiyamaSettings.Reporting) {
                t.Stop();
                Report(String.Format(CultureInfo.CurrentCulture, "{0}\n", t.Duration));
            }
#endif

            #endregion

            // SugiyamaLayoutSettings.ShowDatabase(this.database);
            return constrainedOrdering.LayerArrays;
        }

        LayerArrays YLayeringAndOrderingWithoutHorizontalConstraints(LayerArrays layerArrays) {
            CreaeteProperLayeredGraph(layerArrays.Y, out layerArrays);
            Ordering.OrderLayers(properLayeredGraph, layerArrays, originalGraph.Nodes.Count,
                 sugiyamaSettings, CancelToken);
            MetroMapOrdering.UpdateLayerArrays(properLayeredGraph, layerArrays);
            return layerArrays;
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

                aspectRatio = box.Width/box.Height;

                double delta = (box.LeftTop - box.RightBottom).Length/2;

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
                if (kv.Value.Count%2 == 0 && layerArrays.Y[kv.Key.First] - 1 == layerArrays.Y[kv.Key.Second]) {
                    var newVirtualEdge = new PolyIntEdge(kv.Key.First, kv.Key.Second);
                    newVirtualEdge.Edge = new Edge();
                    newVirtualEdge.IsVirtualEdge = true;
                    kv.Value.Insert(kv.Value.Count/2, newVirtualEdge);
                    IntGraph.AddEdge(newVirtualEdge);
                }
        }


        int[] GetNodeCountsOfGluedDag() {
            if (VerticalConstraints.IsEmpty) {
                var ret = new int[IntGraph.NodeCount];
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = 1;
                return ret;
            }
            return VerticalConstraints.GetGluedNodeCounts();
        }

        int[] ExtendLayeringToUngluedSameLayerVertices(int[] p) {
            VerticalConstraintsForSugiyama vc = VerticalConstraints;
            for (int i = 0; i < p.Length; i++)
                p[i] = p[vc.NodeToRepr(i)];
            return p;
        }

        void CalculateEdgeSplines() {
#if TEST_MSAGL
            if (sugiyamaSettings.Reporting)
                Report("calculating splines ... ");
#endif
            var routing = new Routing(sugiyamaSettings, originalGraph, database, engineLayerArrays, properLayeredGraph,
                                      IntGraph);
#if TEST_MSAGL
            Timer t = null;
            if (sugiyamaSettings.Reporting) {
                t = new Timer();
                t.Start();
            }
#endif
            routing.Run();
#if TEST_MSAGL
            if (sugiyamaSettings.Reporting) {
                t.Stop();
                Report(String.Format(CultureInfo.CurrentCulture, " {0}\n", t.Duration));
            }
#endif
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
                                anchor.LeftAnchor = anchor.RightAnchor = VirtualNodeWidth/2.0f;
                                anchor.TopAnchor = anchor.BottomAnchor = VirtualNodeHeight(settings)/2.0f;
                            } else {
                                anchor.LeftAnchor = anchor.RightAnchor = VirtualNodeWidth*4;
                                anchor.TopAnchor = anchor.BottomAnchor = VirtualNodeHeight(settings)/2.0f;
                            }
                        }
                    }
                    //fix label vertices      
                    if (intEdge.HasLabel) {
                        int lj = intEdge.LayerEdges[intEdge.LayerEdges.Count/2].Source;
                        Anchor a = anchors[lj];
                        double w = intEdge.LabelWidth, h = intEdge.LabelHeight;
                        a.RightAnchor = w;
                        a.LeftAnchor = VirtualNodeWidth*8;

                        if (a.TopAnchor < h/2.0)
                            a.TopAnchor = a.BottomAnchor = h/2.0;

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

                MakeVirtualNodesTall(yLayer, bottomAnchorMax, topAnchorMax, originalGraph.Nodes.Count, database.Anchors);

                double flatEdgesHeight = SetFlatEdgesForLayer(database, layerArrays, i, intGraph, settings, ymax);

                double layerCenter = ymax + bottomAnchorMax + flatEdgesHeight;
                double layerTop = layerCenter + topAnchorMax;
                if (NeedToSnapTopsToGrid(settings)) {
                    layerTop += SnapDeltaUp(layerTop, settings.GridSizeByY);
                    foreach (int j in yLayer) anchors[j].Top = layerTop;                   
                } else if (NeedToSnapBottomsToGrid(settings)) {
                    double layerBottom = layerCenter - bottomAnchorMax;
                    layerBottom += SnapDeltaUp(layerBottom, layerBottom);
                    foreach (int j in yLayer)
                    {
                        anchors[j].Bottom = layerBottom;
                        layerTop = Math.Max(anchors[j].Top, layerTop);
                    }
                }
                else foreach (int j in yLayer) anchors[j].Y = layerCenter;
                
                double layerSep = settings.ActualLayerSeparation(layersAreDoubled);
                ymax = layerTop + layerSep;
                i++;
            }
            // for the last layer
            SetFlatEdgesForLayer(database, layerArrays, i, intGraph, settings, ymax);
        }

        private static bool NeedToSnapTopsToGrid(SugiyamaLayoutSettings settings)
        {
            return settings.SnapToGridByY == SnapToGridByY.Top;
        }

        private static bool NeedToSnapBottomsToGrid(SugiyamaLayoutSettings settings)
        {
            return settings.SnapToGridByY == SnapToGridByY.Bottom;
        }

        static double SnapDeltaUp(double y, double gridSize)
        {
            if (gridSize == 0)
                return 0;
            // how much to snap?
            int k =(int) (y / gridSize);
            double delta = y - k * gridSize;
            Debug.Assert(delta >= 0 && delta < gridSize);
            if (Math.Abs(delta) < 0.0001) // ??? 
            {
                return 0;
            }
            return gridSize - delta;
        }
      
        /// <summary>
        /// try to estimate the dimensions of the ensuing layout, useful for determining if we are going
        /// to go through with it, or try a different layout method (e.g. if the aspect ratio is too wacky)
        /// </summary>
        /// <returns>the width and height in X and Y respectively</returns>
        internal Point CalculateApproximateDimensions() {
            CreateGluedDagSkeletonForLayering();
            var ns = new NetworkSimplexForGeneralGraph(GluedDagSkeletonForLayering, null);

            // layers[0] is the layer of the first node, layers[1]: layer of second node, etc...
            int[] layers = ns.GetLayers();

            // for each layer we need the maximum node height and the sum of all node widths
            // in order to calculate the total width and height of the graph
            var layerMap = new Dictionary<int, Point>();
            double width, height;

            Direction layoutDirection = GetLayoutDirection(sugiyamaSettings);
            if (layoutDirection == Direction.North || layoutDirection == Direction.South) {
                for (int i = 0; i < layers.Length; ++i) {
                    Node v = originalGraph.Nodes[i];
#if SHARPKIT //https://github.com/SharpKit/SharpKit/issues/6 structs are not initialized
                    Point size = new Point(0.0, 0.0);
#else
                    Point size;
#endif
                    int l = layers[i];
                    layerMap.TryGetValue(l, out size);
                    layerMap[l] = new Point(
                        v.BoundingBox.Width + size.X + sugiyamaSettings.NodeSeparation,
                        Math.Max(v.BoundingBox.Height, size.Y));
                }
                width = 0;
                height = sugiyamaSettings.LayerSeparation*(layerMap.Count - 1);

                foreach (Point size in layerMap.Values) {
                    width = Math.Max(size.X, width);
                    height += size.Y;
                }
            } else {
                for (int i = 0; i < layers.Length; ++i) {
                    Node v = originalGraph.Nodes[i];
                    Point size;
                    int l = layers[i];
                    layerMap.TryGetValue(l, out size);
                    layerMap[l] = new Point(
                        Math.Max(v.BoundingBox.Width, size.X),
                        v.BoundingBox.Height + size.Y + sugiyamaSettings.NodeSeparation);
                }
                width = sugiyamaSettings.LayerSeparation*(layerMap.Count - 1);
                height = 0;

                foreach (Point size in layerMap.Values) {
                    width += size.X;
                    height = Math.Max(size.Y, height);
                }
            }
            return new Point(width, height);
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
                    double dyOfFlatEdge = settings.LayerSeparation/3;
                    double ym = ymax;
                    flatEdgesHeight =
                        (from pair in flatPairs
                         select SetFlatEdgesLabelsHeightAndPositionts(pair, ym, dyOfFlatEdge, database)).
                            Max();
                }
            }
            return flatEdgesHeight;
        }

        internal static Direction GetLayoutDirection(SugiyamaLayoutSettings settings) {
            Point dir = settings.Transformation*new Point(0, 1);
            return dir.CompassDirection;
        }

        static double SetFlatEdgesLabelsHeightAndPositionts(IntPair pair, double ymax, double dy, Database database) {
            double height = 0;
            List<PolyIntEdge> list = database.GetMultiedge(pair);
            foreach (PolyIntEdge edge in list) {
                height += dy;
                Label label = edge.Edge.Label;
                if (label != null) {
                    label.Center = new Point(label.Center.X, ymax + height + label.Height/2);
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

        static void MakeVirtualNodesTall(int[] yLayer, double bottomAnchorMax, double topAnchorMax,
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
                ExtendStandardAnchors(ref leftAnchor, ref rightAnchor, ref topAnchor, ref bottomAnchor, node, settings);
            }

            RightAnchorMultiSelfEdges(i, ref rightAnchor, ref topAnchor, ref bottomAnchor, database, settings);

            double hw = settings.MinNodeWidth/2;
            if (leftAnchor < hw)
                leftAnchor = hw;
            if (rightAnchor < hw)
                rightAnchor = hw;
            double hh = settings.MinNodeHeight/2;

            if (topAnchor < hh)
                topAnchor = hh;
            if (bottomAnchor < hh)
                bottomAnchor = hh;

            anchors[i] = new Anchor(leftAnchor, rightAnchor, topAnchor, bottomAnchor, intGraph.Nodes[i],
                                    settings.LabelCornersPreserveCoefficient) {Padding = intGraph.Nodes[i].Padding};
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
                        if (topAnchor < e.Edge.Label.Height/2.0)
                            topAnchor = bottomAnchor = e.Edge.Label.Height/2.0f;
                    }

                delta += (settings.NodeSeparation + settings.MinNodeWidth)*multiedges.Count;
            }
            return delta;
        }

        static void ExtendStandardAnchors(ref double leftAnchor, ref double rightAnchor, ref double topAnchor,
                                          ref double bottomAnchor, Node node, SugiyamaLayoutSettings settings) {
            double w = node.Width;
            double h = node.Height;


            w /= 2.0;
            h /= 2.0;


            rightAnchor = leftAnchor = w;
            topAnchor = bottomAnchor = h;
            if (settings.GridSizeByX > 0)
            {
                rightAnchor += settings.GridSizeByX / 2;
                leftAnchor += settings.GridSizeByX / 2;
            }
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
                n1.Weight = n2.Weight = e.Weight;
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

                    ie.Separation = (int) (sep + 1);

                    edges.Add(ie);
                }

            var ret = new XLayoutGraph(IntGraph, properLayeredGraph, layerArrays, edges, nOfVerts);
            ret.SetEdgeWeights();
            return ret;
        }

#if TEST_MSAGL
        ~LayeredLayoutEngine() {
            if (SugiyamaLayoutLogger != null)
                SugiyamaLayoutLogger.Dispose();
        }
#endif

        internal SugiyamaLayoutSettings SugiyamaSettings { get { return sugiyamaSettings; } }
    }
}