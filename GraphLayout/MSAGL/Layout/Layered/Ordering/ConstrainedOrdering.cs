using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.ProjectionSolver;

namespace Microsoft.Msagl.Layout.Layered {
    internal class ConstrainedOrdering {
        readonly GeometryGraph geometryGraph;
        readonly BasicGraph<Node, PolyIntEdge> intGraph;
        internal ProperLayeredGraph ProperLayeredGraph;
        readonly int[] initialLayering;
        LayerInfo[] layerInfos;
        internal LayerArrays LayerArrays;
        readonly HorizontalConstraintsForSugiyama horizontalConstraints;
        int numberOfNodesOfProperGraph;
        readonly Database database;
        double[][] xPositions;
        int[][] yetBestLayers;

        readonly List<PolyIntEdge> verticalEdges = new List<PolyIntEdge>();

        readonly AdjacentSwapsWithConstraints adjSwapper;

        SugiyamaLayoutSettings settings;


        int numberOfLayers = -1;
        int noGainSteps;
        const int MaxNumberOfNoGainSteps=5;

        int NumberOfLayers {
            get {
                if (numberOfLayers > 0)
                    return numberOfLayers;
                return numberOfLayers = initialLayering.Max(i => i + 1);
            }
        }

        double NodeSeparation() {
            return settings.NodeSeparation;
        }

        internal ConstrainedOrdering(
            GeometryGraph geomGraph,
            BasicGraph<Node, PolyIntEdge> basicIntGraph,
            int[] layering,
            Dictionary<Node, int> nodeIdToIndex,
            Database database,
            SugiyamaLayoutSettings settings) {

            this.settings = settings;
            horizontalConstraints = settings.HorizontalConstraints;

            horizontalConstraints.PrepareForOrdering(nodeIdToIndex, layering);

            geometryGraph = geomGraph;
            this.database = database;
            intGraph = basicIntGraph;
            initialLayering = layering;
            //this has to be changed only to insert layers that are needed
            if (NeedToInsertLayers(layering)) {
                for (int i = 0; i < layering.Length; i++)
                    layering[i] *= 2;
                LayersAreDoubled = true;
                numberOfLayers = -1;
            }

            PrepareProperLayeredGraphAndFillLayerInfos();

            adjSwapper = new AdjacentSwapsWithConstraints(
                LayerArrays,
                HasCrossWeights(),
                ProperLayeredGraph,
                layerInfos);
        }

        bool LayersAreDoubled { get; set; }

        bool NeedToInsertLayers(int[] layering) {
            return ExistsShortLabeledEdge(layering, intGraph.Edges) ||
                   ExistsShortMultiEdge(layering, database.Multiedges);
        }

        static bool ExistsShortMultiEdge(int[] layering, Dictionary<IntPair, List<PolyIntEdge>> multiedges) {
            return multiedges.Any(multiedge => multiedge.Value.Count > 2 && layering[multiedge.Key.x] == 1 + layering[multiedge.Key.y]);
        }

        internal void Calculate() {
            AllocateXPositions();
            var originalGraph = intGraph.Nodes[0].GeometryParent as GeometryGraph;
            LayeredLayoutEngine.CalculateAnchorSizes(database, out database.anchors, ProperLayeredGraph, originalGraph, intGraph, settings);
            LayeredLayoutEngine.CalcInitialYAnchorLocations(LayerArrays, 500, geometryGraph, database, intGraph, settings, LayersAreDoubled);
            Order();
        }

        
        ConstrainedOrderMeasure CreateMeasure() {
            return new ConstrainedOrderMeasure(Ordering.GetCrossingsTotal(ProperLayeredGraph, LayerArrays));
        }

        bool HasCrossWeights() {
            return ProperLayeredGraph.Edges.Any(le => le.CrossingWeight != 1);
        }

        static bool ExistsShortLabeledEdge(int[] layering, IEnumerable<PolyIntEdge> edges) {
            return edges.Any(edge => layering[edge.Source] == layering[edge.Target] + 1 && edge.Edge.Label != null);
        }

        void AllocateXPositions() {
            xPositions = new double[NumberOfLayers][];
            for (int i = 0; i < NumberOfLayers; i++)
                xPositions[i] = new double[LayerArrays.Layers[i].Length];
        }

        void Order() {
            CreateInitialOrderInLayers();
            TryPushingOutStrangersFromHorizontalBlocks();
            int n = 5;

            ConstrainedOrderMeasure measure = null;
        
            while (n-- > 0 && noGainSteps <= MaxNumberOfNoGainSteps) {
                
                SetXPositions();
                
                ConstrainedOrderMeasure newMeasure = CreateMeasure();
                if (measure == null || newMeasure < measure) {
                    noGainSteps = 0;
                    Ordering.CloneLayers(LayerArrays.Layers, ref yetBestLayers);
                    measure = newMeasure;
                } else {
                    noGainSteps++;
                    RestoreState();
                }
                
            }
        }

        void SetXPositions() {
            SolverShell solver = InitSolverWithoutOrder();
            ImproveWithAdjacentSwaps();
            PutLayerNodeSeparationsIntoSolver(solver);
            solver.Solve();
            SortLayers(solver);
            for (int i = 0; i < LayerArrays.Y.Length; i++)
                database.Anchors[i].X = solver.GetVariableResolvedPosition(i);

        }

        SolverShell InitSolverWithoutOrder() {
            var solver=new SolverShell();
            InitSolverVars(solver);
            
            PutLeftRightConstraintsIntoSolver(solver);
            PutVerticalConstraintsIntoSolver(solver);
            AddGoalsToKeepProperEdgesShort(solver);

            AddGoalsToKeepFlatEdgesShort(solver);
            return solver;
        }

        void SortLayers(SolverShell solver) {
            for (int i = 0; i < LayerArrays.Layers.Length; i++)
                SortLayerBasedOnSolution(LayerArrays.Layers[i], solver);
        }

        void AddGoalsToKeepFlatEdgesShort(SolverShell solver) {
            foreach (var layerInfo in layerInfos)
                AddGoalToKeepFlatEdgesShortOnBlockLevel(layerInfo, solver);
        }

        void InitSolverVars(SolverShell solver) {
            for (int i = 0; i < LayerArrays.Y.Length; i++)
                solver.AddVariableWithIdealPosition(i, 0);
        }

        void AddGoalsToKeepProperEdgesShort(SolverShell solver) {
            foreach (var edge in ProperLayeredGraph.Edges)
                solver.AddGoalTwoVariablesAreClose(edge.Source, edge.Target, PositionOverBaricenterWeight);

        }

        void PutVerticalConstraintsIntoSolver(SolverShell solver) {
            foreach (var pair in horizontalConstraints.VerticalInts) {
                solver.AddGoalTwoVariablesAreClose(pair.Item1, pair.Item2, ConstrainedVarWeight);
            }
        }

        void PutLeftRightConstraintsIntoSolver(SolverShell solver) {
            foreach (var pair in horizontalConstraints.LeftRighInts) {
                solver.AddLeftRightSeparationConstraint(pair.Item1, pair.Item2, SimpleGapBetweenTwoNodes(pair.Item1, pair.Item2));
            }
        }

        void PutLayerNodeSeparationsIntoSolver(SolverShell solver) {
            foreach (var layer in LayerArrays.Layers) {
                for (int i = 0; i < layer.Length - 1; i++) {
                    int l = layer[i];
                    int r = layer[i + 1];
                    solver.AddLeftRightSeparationConstraint(l, r, SimpleGapBetweenTwoNodes(l, r));
                }
            }
        }

        void ImproveWithAdjacentSwaps() {
            adjSwapper.DoSwaps();
        }

        void TryPushingOutStrangersFromHorizontalBlocks() {

        }

        void CreateInitialOrderInLayers() {
            //the idea is to topologically ordering all nodes horizontally, by using vertical components, then fill the layers according to this order
            Dictionary<int, int> nodesToVerticalComponentsRoots = CreateVerticalComponents();
            IEnumerable<IntPair> liftedLeftRightRelations = LiftLeftRightRelationsToComponentRoots(nodesToVerticalComponentsRoots).ToArray();
            int[] orderOfVerticalComponentRoots = TopologicalSort.GetOrderOnEdges(liftedLeftRightRelations);
            FillLayersWithVerticalComponentsOrder(orderOfVerticalComponentRoots, nodesToVerticalComponentsRoots);
            LayerArrays.UpdateXFromLayers();
        }

        void FillLayersWithVerticalComponentsOrder(int[] order, Dictionary<int, int> nodesToVerticalComponentsRoots) {
            Dictionary<int, List<int>> componentRootsToComponents = CreateComponentRootsToComponentsMap(nodesToVerticalComponentsRoots);
            var alreadyInLayers = new bool[LayerArrays.Y.Length];
            var runninglayerCounts = new int[LayerArrays.Layers.Length];
            foreach (var vertCompRoot in order)
                PutVerticalComponentIntoLayers(EnumerateVertComponent(componentRootsToComponents, vertCompRoot), runninglayerCounts, alreadyInLayers);
            for (int i = 0; i < ProperLayeredGraph.NodeCount; i++)
                if (alreadyInLayers[i] == false)
                    AddVertToLayers(i, runninglayerCounts, alreadyInLayers);

        }

        IEnumerable<int> EnumerateVertComponent(Dictionary<int, List<int>> componentRootsToComponents, int vertCompRoot) {
            List<int> compList;
            if (componentRootsToComponents.TryGetValue(vertCompRoot, out compList)) {
                foreach (var i in compList)
                    yield return i;
            } else
                yield return vertCompRoot;

        }


        void PutVerticalComponentIntoLayers(IEnumerable<int> vertComponent, int[] runningLayerCounts, bool[] alreadyInLayers) {
            foreach (var i in vertComponent)
                AddVertToLayers(i, runningLayerCounts, alreadyInLayers);
        }

        void AddVertToLayers(int i, int[] runningLayerCounts, bool[] alreadyInLayers) {
            if (alreadyInLayers[i])
                return;
            int layerIndex = LayerArrays.Y[i];

            int xIndex = runningLayerCounts[layerIndex];
            var layer = LayerArrays.Layers[layerIndex];

            layer[xIndex++] = i;
            alreadyInLayers[i] = true;
            List<int> block;
            if (horizontalConstraints.BlockRootToBlock.TryGetValue(i, out block))
                foreach (var v in block) {
                    if (alreadyInLayers[v]) continue;
                    layer[xIndex++] = v;
                    alreadyInLayers[v] = true;
                }
            runningLayerCounts[layerIndex] = xIndex;
        }

        static Dictionary<int, List<int>> CreateComponentRootsToComponentsMap(Dictionary<int, int> nodesToVerticalComponentsRoots) {
            var d = new Dictionary<int, List<int>>();
            foreach (var kv in nodesToVerticalComponentsRoots) {
                int i = kv.Key;
                var root = kv.Value;
                List<int> component;
                if (!d.TryGetValue(root, out component)) {
                    d[root] = component = new List<int>();
                }
                component.Add(i);
            }
            return d;
        }

        IEnumerable<IntPair> LiftLeftRightRelationsToComponentRoots(Dictionary<int, int> nodesToVerticalComponentsRoots) {
            foreach (var pair in horizontalConstraints.LeftRighInts)
                yield return new IntPair(GetFromDictionaryOrIdentical(nodesToVerticalComponentsRoots, pair.Item1),
                    GetFromDictionaryOrIdentical(nodesToVerticalComponentsRoots, pair.Item2));
            foreach (var pair in horizontalConstraints.LeftRightIntNeibs)
                yield return new IntPair(GetFromDictionaryOrIdentical(nodesToVerticalComponentsRoots, pair.Item1),
                    GetFromDictionaryOrIdentical(nodesToVerticalComponentsRoots, pair.Item2));
        }

        static int GetFromDictionaryOrIdentical(Dictionary<int, int> d, int key) {
            int i;
            if (d.TryGetValue(key, out i))
                return i;
            return key;
        }

        /// <summary>
        /// These blocks are connected components in the vertical constraints. They don't necesserely span consequent layers.
        /// </summary>
        /// <returns></returns>
        Dictionary<int, int> CreateVerticalComponents() {
            var vertGraph = new BasicGraphOnEdges<PolyIntEdge>(from pair in horizontalConstraints.VerticalInts select new PolyIntEdge(pair.Item1, pair.Item2));
            var verticalComponents = ConnectedComponentCalculator<PolyIntEdge>.GetComponents(vertGraph);
            var nodesToComponentRoots = new Dictionary<int, int>();
            foreach (var component in verticalComponents) {
                var ca = component.ToArray();
                if (ca.Length == 1)
                    continue;
                int componentRoot = -1;
                foreach (var j in component) {
                    if (componentRoot == -1)
                        componentRoot = j;
                    nodesToComponentRoots[j] = componentRoot;
                }
            }
            return nodesToComponentRoots;
        }

        void RestoreState() {
            LayerArrays.UpdateLayers(yetBestLayers);
        }

#if TEST_MSAGL
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledCode")]
        void Show() {
            SugiyamaLayoutSettings.ShowDatabase(database);
        }
#endif

#if TEST_MSAGL
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.Write(System.String)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledCode")]
        static void PrintPositions(double[] positions) {
            for (int j = 0; j < positions.Length; j++)
                System.Diagnostics.Debug.Write(" " + positions[j]);
            System.Diagnostics.Debug.WriteLine("");
        }
#endif


        void SortLayerBasedOnSolution(int[] layer, SolverShell solver) {
            int length = layer.Length;
            var positions = new double[length];
            int k = 0;
            foreach (int v in layer)
                positions[k++] = solver.GetVariableResolvedPosition(v);

            Array.Sort(positions, layer);
            int i = 0;
            foreach (int v in layer)
                LayerArrays.X[v] = i++;
        }

        const double ConstrainedVarWeight = 10e6;
        const double PositionOverBaricenterWeight = 5;

        static int NodeToBlockRootSoftOnLayerInfo(LayerInfo layerInfo, int node) {
            int root;
            return layerInfo.nodeToBlockRoot.TryGetValue(node, out root) ? root : node;
        }

        static void AddGoalToKeepFlatEdgesShortOnBlockLevel(LayerInfo layerInfo, SolverShell solver) {
            if (layerInfo != null)
                foreach (var couple in layerInfo.flatEdges) {
                    int sourceBlockRoot = NodeToBlockRootSoftOnLayerInfo(layerInfo, couple.Item1);
                    int targetBlockRoot = NodeToBlockRootSoftOnLayerInfo(layerInfo, couple.Item2);
                    if (sourceBlockRoot != targetBlockRoot)
                        solver.AddGoalTwoVariablesAreClose(sourceBlockRoot, targetBlockRoot);
                }
        }

        static bool NodeIsConstrainedBelow(int v, LayerInfo layerInfo) {
            if (layerInfo == null)
                return false;
            return layerInfo.constrainedFromBelow.ContainsKey(v);
        }

        static bool NodeIsConstrainedAbove(int v, LayerInfo layerInfo) {
            if (layerInfo == null)
                return false;
            return layerInfo.constrainedFromAbove.ContainsKey(v);
        }

        internal static bool BelongsToNeighbBlock(int p, LayerInfo layerInfo) {
            return layerInfo != null && (layerInfo.nodeToBlockRoot.ContainsKey(p) || layerInfo.neigBlocks.ContainsKey(p));
            //p is a root of the block
        }

        static bool NodesAreConstrainedBelow(int leftNode, int rightNode, LayerInfo layerInfo) {
            return NodeIsConstrainedBelow(leftNode, layerInfo) && NodeIsConstrainedBelow(rightNode, layerInfo);
        }

        static bool NodesAreConstrainedAbove(int leftNode, int rightNode, LayerInfo layerInfo) {
            return NodeIsConstrainedAbove(leftNode, layerInfo) && NodeIsConstrainedAbove(rightNode, layerInfo);
        }

        double GetGapFromNodeNodesConstrainedBelow(int leftNode, int rightNode, LayerInfo layerInfo,
                                                          int layerIndex) {
            double gap = SimpleGapBetweenTwoNodes(leftNode, rightNode);
            leftNode = layerInfo.constrainedFromBelow[leftNode];
            rightNode = layerInfo.constrainedFromBelow[rightNode];
            layerIndex--;
            layerInfo = layerInfos[layerIndex];
            if (layerIndex > 0 && NodesAreConstrainedBelow(leftNode, rightNode, layerInfo))
                return Math.Max(gap, GetGapFromNodeNodesConstrainedBelow(leftNode, rightNode, layerInfo, layerIndex));
            return Math.Max(gap, SimpleGapBetweenTwoNodes(leftNode, rightNode));
        }

        double GetGapFromNodeNodesConstrainedAbove(int leftNode, int rightNode, LayerInfo layerInfo,
                                                          int layerIndex) {
            double gap = SimpleGapBetweenTwoNodes(leftNode, rightNode);
            leftNode = layerInfo.constrainedFromAbove[leftNode];
            rightNode = layerInfo.constrainedFromAbove[rightNode];
            layerIndex++;
            layerInfo = layerInfos[layerIndex];
            if (layerIndex < LayerArrays.Layers.Length - 1 && NodesAreConstrainedAbove(leftNode, rightNode, layerInfo))
                return Math.Max(gap, GetGapFromNodeNodesConstrainedAbove(leftNode, rightNode, layerInfo, layerIndex));
            return Math.Max(gap, SimpleGapBetweenTwoNodes(leftNode, rightNode));
        }

        double SimpleGapBetweenTwoNodes(int leftNode, int rightNode) {
            return database.anchors[leftNode].RightAnchor +
                   NodeSeparation() + database.anchors[rightNode].LeftAnchor;
        }

        
        void PrepareProperLayeredGraphAndFillLayerInfos() {
            layerInfos = new LayerInfo[NumberOfLayers];
            CreateProperLayeredGraph();
            CreateExtendedLayerArrays();
            FillBlockRootToBlock();
            FillLeftRightPairs();
            FillFlatEdges();
            FillAboveBelow();
            FillBlockRootToVertConstrainedNode();
        }

        void FillBlockRootToVertConstrainedNode() {
            foreach (LayerInfo layerInfo in layerInfos)
                foreach (int v in VertConstrainedNodesOfLayer(layerInfo)) {
                    int blockRoot;
                    if (TryGetBlockRoot(v, out blockRoot, layerInfo))
                        layerInfo.blockRootToVertConstrainedNodeOfBlock[blockRoot] = v;
                }
        }

        static bool TryGetBlockRoot(int v, out int blockRoot, LayerInfo layerInfo) {
            if (layerInfo.nodeToBlockRoot.TryGetValue(v, out blockRoot))
                return true;
            if (layerInfo.neigBlocks.ContainsKey(v)) {
                blockRoot = v;
                return true;
            }
            return false;
        }

        static IEnumerable<int> VertConstrainedNodesOfLayer(LayerInfo layerInfo) {
            if (layerInfo != null) {
                foreach (int v in layerInfo.constrainedFromAbove.Keys)
                    yield return v;
                foreach (int v in layerInfo.constrainedFromBelow.Keys)
                    yield return v;
            }
        }


        void CreateExtendedLayerArrays() {
            var layeringExt = new int[numberOfNodesOfProperGraph];
            Array.Copy(initialLayering, layeringExt, initialLayering.Length);
            foreach (PolyIntEdge edge in ProperLayeredGraph.BaseGraph.Edges) {
                var ledges = (LayerEdge[])edge.LayerEdges;
                if (ledges != null && ledges.Length > 1) {
                    int layerIndex = initialLayering[edge.Source] - 1;
                    for (int i = 0; i < ledges.Length - 1; i++)
                        layeringExt[ledges[i].Target] = layerIndex--;
                }
            }
            LayerArrays = new LayerArrays(layeringExt);
        }

        void CreateProperLayeredGraph() {
            IEnumerable<PolyIntEdge> edges = CreatePathEdgesOnIntGraph();
            var nodeCount = Math.Max(intGraph.NodeCount, BasicGraph<Node, PolyIntEdge>.VertexCount(edges));
            var baseGraph = new BasicGraph<Node, PolyIntEdge>(edges, nodeCount) { Nodes = intGraph.Nodes };
            ProperLayeredGraph = new ProperLayeredGraph(baseGraph);
        }

        IEnumerable<PolyIntEdge> CreatePathEdgesOnIntGraph() {
            numberOfNodesOfProperGraph = intGraph.NodeCount;
            var ret = new List<PolyIntEdge>();
            foreach (PolyIntEdge ie in intGraph.Edges) {
                if (initialLayering[ie.Source] > initialLayering[ie.Target]) {
                    CreateLayerEdgesUnderIntEdge(ie);
                    ret.Add(ie);
                    if (horizontalConstraints.VerticalInts.Contains(new Tuple<int, int>(ie.Source, ie.Target)))
                        verticalEdges.Add(ie);
                }
            }

            return ret;
        }


        void CreateLayerEdgesUnderIntEdge(PolyIntEdge ie) {
            int source = ie.Source;
            int target = ie.Target;

            int span = LayeredLayoutEngine.EdgeSpan(initialLayering, ie);
            ie.LayerEdges = new LayerEdge[span];
            Debug.Assert(span > 0);
            if (span == 1)
                ie.LayerEdges[0] = new LayerEdge(ie.Source, ie.Target, ie.CrossingWeight);
            else {
                ie.LayerEdges[0] = new LayerEdge(source, numberOfNodesOfProperGraph, ie.CrossingWeight);
                for (int i = 0; i < span - 2; i++)
                    ie.LayerEdges[i + 1] = new LayerEdge(numberOfNodesOfProperGraph++, numberOfNodesOfProperGraph,
                                                         ie.CrossingWeight);
                ie.LayerEdges[span - 1] = new LayerEdge(numberOfNodesOfProperGraph++, target, ie.CrossingWeight);
            }
        }


        void FillAboveBelow() {
            foreach (PolyIntEdge ie in verticalEdges) {
                foreach (LayerEdge le in ie.LayerEdges) {
                    int upper = le.Source;
                    int lower = le.Target;
                    RegisterAboveBelowOnConstrainedUpperLower(upper, lower);
                }
            }

            foreach (var p in horizontalConstraints.VerticalInts)
                RegisterAboveBelowOnConstrainedUpperLower(p.Item1, p.Item2);
        }

        void RegisterAboveBelowOnConstrainedUpperLower(int upper, int lower) {
            LayerInfo topLayerInfo = GetOrCreateLayerInfo(LayerArrays.Y[upper]);
            LayerInfo bottomLayerInfo = GetOrCreateLayerInfo(LayerArrays.Y[lower]);

            topLayerInfo.constrainedFromBelow[upper] = lower;
            bottomLayerInfo.constrainedFromAbove[lower] = upper;
        }

        void FillFlatEdges() {
            foreach (PolyIntEdge edge in intGraph.Edges) {
                int l = initialLayering[edge.Source];
                if (l == initialLayering[edge.Target]) {
                    GetOrCreateLayerInfo(l).flatEdges.Insert(new Tuple<int, int>(edge.Source, edge.Target));
                }
            }
        }

        void FillLeftRightPairs() {
            foreach (var p in horizontalConstraints.LeftRighInts) {
                LayerInfo layerInfo = GetOrCreateLayerInfo(initialLayering[p.Item1]);
                layerInfo.leftRight.Insert(p);
            }
        }

        /// <summary>
        /// when we call this function we know that a LayerInfo is needed
        /// </summary>
        /// <param name="layerNumber"></param>
        /// <returns></returns>
        LayerInfo GetOrCreateLayerInfo(int layerNumber) {
            LayerInfo layerInfo = layerInfos[layerNumber] ?? (layerInfos[layerNumber] = new LayerInfo());
            return layerInfo;
        }

        void FillBlockRootToBlock() {
            foreach (var p in horizontalConstraints.BlockRootToBlock) {
                LayerInfo layerInfo = GetOrCreateLayerInfo(initialLayering[p.Key]);
                layerInfo.neigBlocks[p.Key] = p.Value;
                foreach (int i in p.Value)
                    layerInfo.nodeToBlockRoot[i] = p.Key;
            }
        }
    }
}