using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// vertical constraints for Suquiyama scheme
    /// </summary>
    internal class VerticalConstraintsForSugiyama {
        readonly Set<Node> _maxLayerOfGeomGraph = new Set<Node>();
        /// <summary>
        /// nodes that are pinned to the max layer
        /// </summary>
        internal Set<Node> MaxLayerOfGeomGraph {
            get { return _maxLayerOfGeomGraph; }
        }

        readonly Set<Node> _minLayerOfGeomGraph = new Set<Node>();
        /// <summary>
        /// nodes that are pinned to the min layer
        /// </summary>
        internal Set<Node> MinLayerOfGeomGraph
        {
            get { return _minLayerOfGeomGraph; }
        }

        Set<Tuple<Node, Node>> sameLayerConstraints = new Set<Tuple<Node, Node>>();
        /// <summary>
        /// set of couple of nodes belonging to the same layer
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        internal Set<Tuple<Node, Node>> SameLayerConstraints
        {
            get { return sameLayerConstraints; }
        }
        Set<Tuple<Node, Node>> upDownConstraints = new Set<Tuple<Node, Node>>();

        /// <summary>
        /// set of node couples such that the first node of the couple is above the second one
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        internal Set<Tuple<Node, Node>> UpDownConstraints
        {
            get { return upDownConstraints; }
        }
        /// <summary>
        /// pins a node to max layer
        /// </summary>
        /// <param name="node"></param>
        internal void PinNodeToMaxLayer(Node node)
        {
            MaxLayerOfGeomGraph.Insert(node);
        }

        /// <summary>
        /// pins a node to min layer
        /// </summary>
        /// <param name="node"></param>
        internal void PinNodeToMinLayer(Node node)
        {
            System.Diagnostics.Debug.Assert(node != null);
            MinLayerOfGeomGraph.Insert(node);
        }

        internal bool IsEmpty {
            get { return MaxLayerOfGeomGraph.Count == 0 && MinLayerOfGeomGraph.Count == 0 && SameLayerConstraints.Count == 0 && this.UpDownConstraints.Count == 0; }
        }


      
        

        internal void Clear() {
            this.MaxLayerOfGeomGraph.Clear(); 
            this.MinLayerOfGeomGraph.Clear();
            this.SameLayerConstraints.Clear();
            this.UpDownConstraints.Clear();
        }
        Set<IntPair> gluedUpDownIntConstraints = new Set<IntPair>();

        internal Set<IntPair> GluedUpDownIntConstraints {
            get { return gluedUpDownIntConstraints; }
            set { gluedUpDownIntConstraints = value; }
        }
        Dictionary<Node, int> nodeIdToIndex;
        BasicGraph<Node, PolyIntEdge> intGraph;
        /// <summary>
        /// this graph is obtained from intGraph by glueing together same layer vertices
        /// </summary>
        BasicGraphOnEdges<IntPair> gluedIntGraph;
        int maxRepresentative;
        int minRepresentative;
        /// <summary>
        /// Maps each node participating in same layer relation its representative on the layer.
        /// </summary>
        Dictionary<int, int> sameLayerDictionaryOfRepresentatives = new Dictionary<int, int>();
        Dictionary<int, IEnumerable<int>> representativeToItsLayer = new Dictionary<int, IEnumerable<int>>();
        internal IEnumerable<IEdge> GetFeedbackSet(BasicGraph<Node, PolyIntEdge> intGraphPar, Dictionary<Node, int> nodeIdToIndexPar) {
            this.nodeIdToIndex = nodeIdToIndexPar;
            this.intGraph = intGraphPar;
            this.maxRepresentative = -1;
            this.minRepresentative = -1;
            CreateIntegerConstraints();
            GlueTogetherSameConstraintsMaxAndMin();
            AddMaxMinConstraintsToGluedConstraints();
            RemoveCyclesFromGluedConstraints();
            return GetFeedbackSet();
        }

        private void RemoveCyclesFromGluedConstraints() {
            var feedbackSet= CycleRemoval<IntPair>.
                GetFeedbackSetWithConstraints(new BasicGraphOnEdges<IntPair>(GluedUpDownIntConstraints, this.intGraph.NodeCount), null);
            //feedbackSet contains all glued constraints making constraints cyclic
            foreach (IntPair p in feedbackSet)
                GluedUpDownIntConstraints.Remove(p);
        }

        private void AddMaxMinConstraintsToGluedConstraints() {
            if (this.maxRepresentative != -1)
                for (int i = 0; i < this.intGraph.NodeCount; i++) {
                    int j = NodeToRepr(i);
                    if (j != maxRepresentative)
                        GluedUpDownIntConstraints.Insert(new IntPair(maxRepresentative, j));
                }

            if (this.minRepresentative != -1)
                for (int i = 0; i < this.intGraph.NodeCount; i++) {
                    int j = NodeToRepr(i);
                    if (j != minRepresentative)
                        GluedUpDownIntConstraints.Insert(new IntPair(j, minRepresentative));
                }
        }

        private void GlueTogetherSameConstraintsMaxAndMin() {
            CreateDictionaryOfSameLayerRepresentatives();
            GluedUpDownIntConstraints = new Set<IntPair>(from p in UpDownInts select GluedIntPair(p));
        }

        internal IntPair GluedIntPair(Tuple<int, int> p) {
            return new IntPair(NodeToRepr(p.Item1), NodeToRepr(p.Item2));
        }
     
        private IntPair GluedIntPair(PolyIntEdge p) {
            return new IntPair(NodeToRepr(p.Source), NodeToRepr(p.Target));
        }

        internal IntPair GluedIntPair(IntPair p) {
            return new IntPair(NodeToRepr(p.First), NodeToRepr(p.Second));
        }

        internal PolyIntEdge GluedIntEdge(PolyIntEdge intEdge) {
            int sourceRepr = NodeToRepr(intEdge.Source);
            int targetRepr = NodeToRepr(intEdge.Target);
            PolyIntEdge ie = new PolyIntEdge(sourceRepr, targetRepr);
            ie.Separation = intEdge.Separation;
            ie.Weight = 0;
            ie.Edge = intEdge.Edge;
            return ie;
        }
        

        internal int NodeToRepr(int node) {
            int repr;
            if (this.sameLayerDictionaryOfRepresentatives.TryGetValue(node, out repr))
                return repr;
            return node;
        }

        private void CreateDictionaryOfSameLayerRepresentatives() {
            BasicGraphOnEdges<IntPair> graphOfSameLayers = CreateGraphOfSameLayers();
            foreach (var comp in ConnectedComponentCalculator<IntPair>.GetComponents(graphOfSameLayers))
                GlueSameLayerNodesOfALayer(comp);
        }

        private BasicGraphOnEdges<IntPair> CreateGraphOfSameLayers() {
            return new BasicGraphOnEdges<IntPair>(CreateEdgesOfSameLayers(), this.intGraph.NodeCount);
        }

        private IEnumerable<IntPair> CreateEdgesOfSameLayers() {
            List<IntPair> ret = new List<IntPair>();
            if (maxRepresentative != -1)
                ret.AddRange(from v in maxLayerInt where v != maxRepresentative select new IntPair(maxRepresentative, v));
            if (minRepresentative != -1)
                ret.AddRange(from v in minLayerInt where v != minRepresentative select new IntPair(minRepresentative, v));
            ret.AddRange(from couple in SameLayerInts select new IntPair(couple.Item1, couple.Item2));
            return ret;
        }
        /// <summary>
        /// maps all nodes of the component to one random representative
        /// </summary>
        /// <param name="sameLayerNodes"></param>
        private void GlueSameLayerNodesOfALayer(IEnumerable<int> sameLayerNodes) {
            if (sameLayerNodes.Count<int>() > 1) {
                int representative = -1;
                if (ComponentsIsMaxLayer(sameLayerNodes))
                    foreach (int v in sameLayerNodes)
                        this.sameLayerDictionaryOfRepresentatives[v] = representative = maxRepresentative;
                else if (ComponentIsMinLayer(sameLayerNodes))
                    foreach (int v in sameLayerNodes)
                        sameLayerDictionaryOfRepresentatives[v] = representative = minRepresentative;
                else {
                    foreach (int v in sameLayerNodes) {
                        if (representative == -1)
                            representative = v;
                        sameLayerDictionaryOfRepresentatives[v] = representative;
                    }
                }
                this.representativeToItsLayer[representative] = sameLayerNodes;
            }
        }

        private bool ComponentIsMinLayer(IEnumerable<int> component) {
            return component.Contains<int>(this.minRepresentative);
        }

        private bool ComponentsIsMaxLayer(IEnumerable<int> component) {
            return component.Contains<int>(this.maxRepresentative);
        }

        List<int> maxLayerInt = new List<int>();
        List<int> minLayerInt = new List<int>();
        List<Tuple<int, int>> sameLayerInts = new List<Tuple<int, int>>();

        /// <summary>
        /// contains also pinned max and min pairs
        /// </summary>
        internal List<Tuple<int, int>> SameLayerInts {
            get { return sameLayerInts; }
            set { sameLayerInts = value; }
        }
        List<Tuple<int, int>> upDownInts = new List<Tuple<int, int>>();

        internal List<Tuple<int, int>> UpDownInts {
            get { return upDownInts; }
            set { upDownInts = value; }
        }

        private void CreateIntegerConstraints() {
            CreateMaxIntConstraints();
            CreateMinIntConstraints();
            CreateUpDownConstraints();
            CreateSameLayerConstraints();
        }

        private void CreateSameLayerConstraints() {
            this.SameLayerInts = CreateIntConstraintsFromStringCouples(this.SameLayerConstraints);
        }

        private void CreateUpDownConstraints() {
            this.UpDownInts = CreateIntConstraintsFromStringCouples(this.UpDownConstraints);
        }

        private List<Tuple<int, int>> CreateIntConstraintsFromStringCouples(Set<Tuple<Node, Node>> set)
        {
            return new List<Tuple<int, int>>(from couple in set
                                              let t = new Tuple<int, int>(NodeIndex(couple.Item1), NodeIndex(couple.Item2))
                                              where t.Item1 != -1 && t.Item2 != -1
                                              select t);
        }

        private void CreateMinIntConstraints() {
            this.minLayerInt = CreateIntConstraintsFromExtremeLayer(this.MinLayerOfGeomGraph);
            if (minLayerInt.Count > 0)
                this.minRepresentative = minLayerInt[0];
        }

        private void CreateMaxIntConstraints() {
            this.maxLayerInt = CreateIntConstraintsFromExtremeLayer(this.MaxLayerOfGeomGraph);
            if (maxLayerInt.Count > 0)
                this.maxRepresentative = maxLayerInt[0];
        }

        private List<int> CreateIntConstraintsFromExtremeLayer(Set<Node> setOfNodes) {
            return new List<int>(from node in setOfNodes let index = NodeIndex(node) where index != -1 select index);   
        }
        int NodeIndex(Node node) {
            int index;
            if (this.nodeIdToIndex.TryGetValue(node, out index))
                return index;
            return -1;
        }
        private IEnumerable<IEdge> GetFeedbackSet() {
            this.gluedIntGraph = CreateGluedGraph();
            return UnglueIntPairs(CycleRemoval<IntPair>.GetFeedbackSetWithConstraints(gluedIntGraph, this.GluedUpDownIntConstraints));//avoiding lazy evaluation
        }

        private IEnumerable<IEdge> UnglueIntPairs(IEnumerable<IEdge> gluedEdges) {
            foreach (IEdge gluedEdge in gluedEdges)
                foreach (IEdge ungluedEdge in UnglueEdge(gluedEdge))
                    yield return ungluedEdge; 

        }

        private IEnumerable<IEdge> UnglueEdge(IEdge gluedEdge) {
            foreach (int source in UnglueNode(gluedEdge.Source))
                foreach (PolyIntEdge edge in intGraph.OutEdges(source))
                    if (NodeToRepr(edge.Target) == gluedEdge.Target)
                        yield return edge;
        }

        private BasicGraphOnEdges<IntPair> CreateGluedGraph() {
            return new BasicGraphOnEdges<IntPair>(new Set<IntPair>(from edge in this.intGraph.Edges select GluedIntPair(edge)), this.intGraph.NodeCount);
        }


        IEnumerable<int> UnglueNode(int node) {
            IEnumerable<int> layer;
            if (this.representativeToItsLayer.TryGetValue(node, out layer))
                return layer;
            return new int[] { node };
        }


        internal int[] GetGluedNodeCounts() {
            int[] ret = new int[this.nodeIdToIndex.Count];
            for (int node = 0; node < ret.Length; node++)
                ret[NodeToRepr(node)]++;
            return ret;
        }
    }
}
