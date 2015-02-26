/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
        readonly BasicGraph<Node, IntEdge> intGraph;
        internal ProperLayeredGraph ProperLayeredGraph;
        readonly int[] initialLayering;
        LayerInfo[] layerInfos;
        internal LayerArrays LayerArrays;
        readonly HorizontalConstraintsForSugiyama horizontalConstraints;
        int numberOfNodesOfProperGraph;
        readonly Database database;
        double[][] xPositions;
        double[][] xPositionsClone;
        int[][] yetBestLayers;

        readonly List<IntEdge> verticalEdges = new List<IntEdge>();

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

        double GetNodeWidth(int p) {
            return database.anchors[p].Width;
        }

        internal ConstrainedOrdering(
            GeometryGraph geomGraph,
            BasicGraph<Node, IntEdge> basicIntGraph,
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

        static bool ExistsShortMultiEdge(int[] layering, Dictionary<IntPair, List<IntEdge>> multiedges) {
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

        double GetDeviationFromConstraints() {
            return horizontalConstraints.VerticalInts.Sum(c => VerticalDeviationOfCouple(c)) +
                   horizontalConstraints.LeftRighInts.Sum(c => LeftRightConstraintDeviation(c));
        }

        double LeftRightConstraintDeviation(Tuple<int, int> couple) {
            var l = XPosition(couple.Item1);
            var r = XPosition(couple.Item2);
            return Math.Max(0, l - r);
        }

        double VerticalDeviationOfCouple(Tuple<int, int> couple) {
            return Math.Abs(XPosition(couple.Item1) - XPosition(couple.Item2));
        }

        bool HasCrossWeights() {
            return ProperLayeredGraph.Edges.Any(le => le.CrossingWeight != 1);
        }

        static bool ExistsShortLabeledEdge(int[] layering, IEnumerable<IntEdge> edges) {
            return edges.Any(edge => layering[edge.Source] == layering[edge.Target] + 1 && edge.Edge.Label != null);
        }

        void AllocateXPositions() {
            xPositions = new double[NumberOfLayers][];
            for (int i = 0; i < NumberOfLayers; i++)
                xPositions[i] = new double[LayerArrays.Layers[i].Length];
        }

        double BlockWidth(int blockRoot) {
            return GetNodeWidth(blockRoot) +
                   horizontalConstraints.BlockRootToBlock[blockRoot].Sum(
                       l => GetNodeWidth(l) + settings.NodeSeparation);
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

            #region old code
            /*
             int noGainSteps = 0;
             for (int i = 0; i < NumberOfSweeps && noGainSteps <= MaxNumberOfNoGainSteps && !measure.Perfect(); i++) {
                 SweepDown(false);
                 SweepUp(false);
                 ConstrainedOrderMeasure newMeasure = CreateMeasure();
                 if (newMeasure < measure) {
                     noGainSteps = 0;
                     Ordering.CloneLayers(LayerArrays.Layers, ref yetBestLayers);
                     measure = newMeasure;
                 } else {
                     noGainSteps++;
                     RestoreState();
                 }
             }
             
             SwitchXPositions();
             SweepUpWithoutChangingLayerOrder(true);
             
             SwitchXPositions();
             SweepDownWithoutChangingLayerOrder(true);
             AverageXPositions();
              */
            #endregion
        }

        void SetXPositions() {
            ISolverShell solver = InitSolverWithoutOrder();
            ImproveWithAdjacentSwaps();
            PutLayerNodeSeparationsIntoSolver(solver);
            solver.Solve();
            SortLayers(solver);
            for (int i = 0; i < LayerArrays.Y.Length; i++)
                database.Anchors[i].X = solver.GetVariableResolvedPosition(i);

        }

        ISolverShell InitSolverWithoutOrder() {
            ISolverShell solver=CreateSolver();
            InitSolverVars(solver);
            
            PutLeftRightConstraintsIntoSolver(solver);
            PutVerticalConstraintsIntoSolver(solver);
            AddGoalsToKeepProperEdgesShort(solver);

            AddGoalsToKeepFlatEdgesShort(solver);
            return solver;
        }

        void SortLayers(ISolverShell solver) {
            for (int i = 0; i < LayerArrays.Layers.Length; i++)
                SortLayerBasedOnSolution(LayerArrays.Layers[i], solver);
        }

        void AddGoalsToKeepFlatEdgesShort(ISolverShell solver) {
            foreach (var layerInfo in layerInfos)
                AddGoalToKeepFlatEdgesShortOnBlockLevel(layerInfo, solver);
        }

        void InitSolverVars(ISolverShell solver) {
            for (int i = 0; i < LayerArrays.Y.Length; i++)
                solver.AddVariableWithIdealPosition(i, 0);
        }

        void AddGoalsToKeepProperEdgesShort(ISolverShell solver) {
            foreach (var edge in ProperLayeredGraph.Edges)
                solver.AddGoalTwoVariablesAreClose(edge.Source, edge.Target, PositionOverBaricenterWeight);

        }

        void PutVerticalConstraintsIntoSolver(ISolverShell solver) {
            foreach (var pair in horizontalConstraints.VerticalInts) {
                solver.AddGoalTwoVariablesAreClose(pair.Item1, pair.Item2, ConstrainedVarWeight);
            }
        }

        void PutLeftRightConstraintsIntoSolver(ISolverShell solver) {
            foreach (var pair in horizontalConstraints.LeftRighInts) {
                solver.AddLeftRightSeparationConstraint(pair.Item1, pair.Item2, SimpleGapBetweenTwoNodes(pair.Item1, pair.Item2));
            }
        }

        void PutLayerNodeSeparationsIntoSolver(ISolverShell solver) {
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
            var vertGraph = new BasicGraph<IntEdge>(from pair in horizontalConstraints.VerticalInts select new IntEdge(pair.Item1, pair.Item2));
            var verticalComponents = ConnectedComponentCalculator<IntEdge>.GetComponents(vertGraph);
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

        void AverageXPositions() {
            for (int i = 0; i < LayerArrays.Layers.Length; i++) {
                int[] layer = LayerArrays.Layers[i];
                double[] xPos = xPositions[i];
                double[] xPosClone = xPositionsClone[i];
                for (int j = 0; j < layer.Length; j++)
                    database.Anchors[layer[j]].X = (xPos[j] + xPosClone[j]) / 2;
            }
        }


        void SwitchXPositions() {
            if (xPositionsClone == null)
                AllocateXPositionsClone();
            double[][] xPositionsSaved = xPositions;
            xPositions = xPositionsClone;
            xPositionsClone = xPositionsSaved;
        }

        void AllocateXPositionsClone() {
            xPositionsClone = new double[xPositions.Length][];
            for (int i = 0; i < xPositions.Length; i++)
                xPositionsClone[i] = new double[xPositions[i].Length];
        }


        double GetBaricenterAbove(int v) {
            int inEdgesCount = ProperLayeredGraph.InEdgesCount(v);
            Debug.Assert(inEdgesCount > 0);
            return (from edge in ProperLayeredGraph.InEdges(v) select XPosition(edge.Source)).Sum() / inEdgesCount;
        }


        double XPosition(int node) {
            return database.Anchors[node].X;
        }

        double GetBaricenterBelow(int v) {
            int outEdgesCount = ProperLayeredGraph.OutEdgesCount(v);
            Debug.Assert(outEdgesCount > 0);

            return (from edge in ProperLayeredGraph.OutEdges(v) select XPosition(edge.Target)).Sum() / outEdgesCount;
        }

#if TEST_MSAGL
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.Write(System.String)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledCode")]
        static void PrintPositions(double[] positions) {
            for (int j = 0; j < positions.Length; j++)
                Console.Write(" " + positions[j]);
            Console.WriteLine();
        }
#endif


        void SortLayerBasedOnSolution(int[] layer, ISolverShell solver) {
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

        double GetGapFromClonedXPositions(int l, int r) {
            int layerIndex = LayerArrays.Y[l];
            int li = LayerArrays.X[l];
            int ri = LayerArrays.X[r];
            var layerXPositions = xPositionsClone[layerIndex];
            var gap = layerXPositions[ri] - layerXPositions[li];
            Debug.Assert(gap > 0);
            return gap;
        }

    
        void AddSeparationConstraintsForFlatEdges(LayerInfo layerInfo, ISolverShell solver) {
            if (layerInfo != null) {
                foreach (var p in layerInfo.flatEdges) {
                    int left, right;
                    if (LayerArrays.X[p.Item1] < LayerArrays.X[p.Item2]) {
                        left = p.Item1;
                        right = p.Item2;
                    } else {
                        left = p.Item2;
                        right = p.Item1;
                    }
                    if (left == right) continue;
                    double gap = GetGap(left, right);
                    foreach (IntEdge edge in database.GetMultiedge(p.Item1, p.Item2))
                        solver.AddLeftRightSeparationConstraint(left, right,
                                                                gap + NodeSeparation() +
                                                                (edge.Edge.Label != null ? edge.Edge.Label.Width : 0));
                }
            }
        }

        void ExtractPositionsFromSolver(int[] layer, ISolverShell solver, double[] positions) {
            solver.Solve();
            for (int i = 0; i < layer.Length; i++)
                database.Anchors[layer[i]].X = positions[i] = solver.GetVariableResolvedPosition(layer[i]);
        }

        static IEnumerable<int> AddBlocksToLayer(IEnumerable<int> collapsedSortedLayer,
                                                        Dictionary<int, List<int>> blockRootToList) {
            foreach (int i in collapsedSortedLayer) {
                yield return i;
                List<int> list;
                if (blockRootToList !=null &&  blockRootToList.TryGetValue(i, out list))
                    foreach (int j in list)
                        yield return j;
            }
        }


        static int NodeToBlockRootSoftOnLayerInfo(LayerInfo layerInfo, int node) {
            int root;
            return layerInfo.nodeToBlockRoot.TryGetValue(node, out root) ? root : node;
        }


        //at the moment we only are looking for the order of nodes in the layer
        void FillSolverWithoutKnowingLayerOrder(IEnumerable<int> layer, LayerInfo layerInfo, ISolverShell solver,
                                                       SweepMode sweepMode) {
            foreach (int v in layer)
                if (layerInfo.neigBlocks.ContainsKey(v)) {
                    //v is a block root
                    int blockNode = GetFixedBlockNode(v, layerInfo, sweepMode);
                    if (blockNode != -1)
                        solver.AddVariableWithIdealPosition(v, FixedNodePosition(blockNode, sweepMode),
                                                            ConstrainedVarWeight);
                    else {
                        IEnumerable<int> t = from u in layerInfo.neigBlocks[v].Concat(new[] { v })
                                             where IsConnectedToPrevLayer(u, sweepMode)
                                             select u;
                        if (t.Any()) {
                            blockNode = t.First();
                            solver.AddVariableWithIdealPosition(v, GetBaricenterOnPrevLayer(blockNode, sweepMode));
                        }
                    }
                } else if (!BelongsToNeighbBlock(v, layerInfo)) {
                    if (NodeIsConstrained(v, sweepMode, layerInfo))
                        solver.AddVariableWithIdealPosition(v, FixedNodePosition(v, sweepMode), ConstrainedVarWeight);
                    else if (IsConnectedToPrevLayer(v, sweepMode))
                        solver.AddVariableWithIdealPosition(v, GetBaricenterOnPrevLayer(v, sweepMode));
                }

            AddGoalToKeepFlatEdgesShortOnBlockLevel(layerInfo, solver);

            foreach (var p in layerInfo.leftRight)
                solver.AddLeftRightSeparationConstraint(p.Item1, p.Item2, GetGapBetweenBlockRoots(p.Item1, p.Item2));
        }

        static void AddGoalToKeepFlatEdgesShortOnBlockLevel(LayerInfo layerInfo, ISolverShell solver) {
            if (layerInfo != null)
                foreach (var couple in layerInfo.flatEdges) {
                    int sourceBlockRoot = NodeToBlockRootSoftOnLayerInfo(layerInfo, couple.Item1);
                    int targetBlockRoot = NodeToBlockRootSoftOnLayerInfo(layerInfo, couple.Item2);
                    if (sourceBlockRoot != targetBlockRoot)
                        solver.AddGoalTwoVariablesAreClose(sourceBlockRoot, targetBlockRoot);
                }
        }

        bool IsConnectedToPrevLayer(int v, SweepMode sweepMode) {
            return sweepMode == SweepMode.ComingFromAbove && ProperLayeredGraph.InEdgesCount(v) > 0 ||
                   sweepMode == SweepMode.ComingFromBelow && ProperLayeredGraph.OutEdgesCount(v) > 0;
        }

        double FixedNodePosition(int v, SweepMode sweepMode) {
            Debug.Assert(sweepMode != SweepMode.Starting);
            LayerInfo layerInfo = layerInfos[LayerArrays.Y[v]];
            return sweepMode == SweepMode.ComingFromAbove
                       ? XPosition(layerInfo.constrainedFromAbove[v])
                       : XPosition(layerInfo.constrainedFromBelow[v]);
        }


        double GetBaricenterOnPrevLayer(int v, SweepMode sweepMode) {
            Debug.Assert(sweepMode != SweepMode.Starting);
            return sweepMode == SweepMode.ComingFromAbove ? GetBaricenterAbove(v) : GetBaricenterBelow(v);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockRoot"></param>
        /// <param name="layerInfo"></param>
        /// <param name="sweepMode"></param>
        /// <returns>-1 if no node is constrained</returns>
        static int GetFixedBlockNode(int blockRoot, LayerInfo layerInfo, SweepMode sweepMode) {
            if (sweepMode == SweepMode.Starting)
                return -1;

            if (sweepMode == SweepMode.ComingFromBelow)
                return GetFixedBlockNodeFromBelow(blockRoot, layerInfo);
            return GetFixedBlockNodeFromAbove(blockRoot, layerInfo);
        }

        static int GetFixedBlockNodeFromBelow(int blockRoot, LayerInfo layerInfo) {
            if (layerInfo.constrainedFromBelow.ContainsKey(blockRoot))
                return blockRoot;
            foreach (int v in layerInfo.neigBlocks[blockRoot])
                if (layerInfo.constrainedFromBelow.ContainsKey(v))
                    return v;
            return -1;
        }

        static int GetFixedBlockNodeFromAbove(int blockRoot, LayerInfo layerInfo) {
            if (layerInfo.constrainedFromAbove.ContainsKey(blockRoot))
                return blockRoot;
            foreach (int v in layerInfo.neigBlocks[blockRoot])
                if (layerInfo.constrainedFromAbove.ContainsKey(v))
                    return v;
            return -1;
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

        double GetGapBetweenBlockRoots(int leftBlockRoot, int rightBlockRoot) {
            double lw = GetBlockWidth(leftBlockRoot);
            double rw = GetNodeWidth(rightBlockRoot);
            return settings.NodeSeparation + 0.5 * (lw + rw);
        }

        double GetBlockWidth(int leftBlockRoot) {
            if (horizontalConstraints.BlockRootToBlock.ContainsKey(leftBlockRoot))
                return BlockWidth(leftBlockRoot);
            return GetNodeWidth(leftBlockRoot);
        }

        double GetGap(int leftNode, int rightNode) {
            int layerIndex = LayerArrays.Y[leftNode];
            LayerInfo layerInfo = layerInfos[layerIndex];
            if (layerInfo == null)
                return SimpleGapBetweenTwoNodes(leftNode, rightNode);
            double gap = 0;
            if (NodesAreConstrainedAbove(leftNode, rightNode, layerInfo))
                gap = GetGapFromNodeNodesConstrainedAbove(leftNode, rightNode, layerInfo, layerIndex);
            if (NodesAreConstrainedBelow(leftNode, rightNode, layerInfo))
                gap = Math.Max(GetGapFromNodeNodesConstrainedBelow(leftNode, rightNode, layerInfo, layerIndex), gap);
            if (gap > 0)
                return gap;
            return SimpleGapBetweenTwoNodes(leftNode, rightNode);
        }

        static bool NodesAreConstrainedBelow(int leftNode, int rightNode, LayerInfo layerInfo) {
            return NodeIsConstrainedBelow(leftNode, layerInfo) && NodeIsConstrainedBelow(rightNode, layerInfo);
        }

        static bool NodesAreConstrainedAbove(int leftNode, int rightNode, LayerInfo layerInfo) {
            return NodeIsConstrainedAbove(leftNode, layerInfo) && NodeIsConstrainedAbove(rightNode, layerInfo);
        }

        static bool NodeIsConstrained(int v, SweepMode sweepMode, LayerInfo layerInfo) {
            if (sweepMode == SweepMode.Starting)
                return false;
            return sweepMode == SweepMode.ComingFromAbove && NodeIsConstrainedAbove(v, layerInfo) ||
                   sweepMode == SweepMode.ComingFromBelow && NodeIsConstrainedBelow(v, layerInfo);
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

        internal static ISolverShell CreateSolver() {
            return new SolverShell();
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
            foreach (IntEdge edge in ProperLayeredGraph.BaseGraph.Edges) {
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
            IEnumerable<IntEdge> edges = CreatePathEdgesOnIntGraph();
            var nodeCount = Math.Max(intGraph.NodeCount, BasicGraph<Node, IntEdge>.VertexCount(edges));
            var baseGraph = new BasicGraph<Node, IntEdge>(edges, nodeCount) { Nodes = intGraph.Nodes };
            ProperLayeredGraph = new ProperLayeredGraph(baseGraph);
        }

        IEnumerable<IntEdge> CreatePathEdgesOnIntGraph() {
            numberOfNodesOfProperGraph = intGraph.NodeCount;
            var ret = new List<IntEdge>();
            foreach (IntEdge ie in intGraph.Edges) {
                if (initialLayering[ie.Source] > initialLayering[ie.Target]) {
                    CreateLayerEdgesUnderIntEdge(ie);
                    ret.Add(ie);
                    if (horizontalConstraints.VerticalInts.Contains(new Tuple<int, int>(ie.Source, ie.Target)))
                        verticalEdges.Add(ie);
                }
            }

            return ret;
        }


        void CreateLayerEdgesUnderIntEdge(IntEdge ie) {
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
            foreach (IntEdge ie in verticalEdges) {
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
            foreach (IntEdge edge in intGraph.Edges) {
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