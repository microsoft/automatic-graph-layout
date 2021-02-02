using System.Collections.Generic;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// This class is used in the case when there are multiple edges, but there is no need to dublicate layers.
    /// We just insert dummy nodes for edge middles without distorting the order of vertices in the layers.
    /// </summary>
    internal class EdgePathsInserter {
        Database database;
        BasicGraph<Node, PolyIntEdge> intGraph;
        ProperLayeredGraph layeredGraph;
        ProperLayeredGraph nLayeredGraph;
        Dictionary<int, PolyIntEdge> virtNodesToIntEdges = new Dictionary<int, PolyIntEdge>();

        internal ProperLayeredGraph NLayeredGraph {
            get { return nLayeredGraph; }
        }
        LayerArrays la;
        LayerArrays nla;

        internal LayerArrays Nla {
            get { return nla; }
        }

        int[] NLayering {
            get {
                return this.nla.Y;
            }
        }

        static internal void InsertPaths(
                                         ref ProperLayeredGraph layeredGraph, ref LayerArrays la,
                                         Database db, BasicGraph<Node, PolyIntEdge> intGraphP) {
            EdgePathsInserter li = new EdgePathsInserter(layeredGraph, la, db, intGraphP);
            li.InsertPaths();
            layeredGraph = li.NLayeredGraph;
            la = li.Nla;
        }

        EdgePathsInserter(
                          ProperLayeredGraph layeredGraph, LayerArrays la, Database database, BasicGraph<Node, PolyIntEdge> intGraphP) {
            this.la = la;
            this.database = database;
            this.layeredGraph = layeredGraph;
            this.intGraph = intGraphP;
        }

        void InsertPaths() {

            CreateFullLayeredGraph();

            InitNewLayering();

            MapVirtualNodesToEdges();

            WidenOriginalLayers();

        }

        void WidenOriginalLayers() {
            for (int i = 0; i < la.Layers.Length; i++) {
                int[] layer = nla.Layers[i];
                int offset = 0;
                foreach (int v in la.Layers[i]) {
                    PolyIntEdge e;
                    this.virtNodesToIntEdges.TryGetValue(v, out e);
                    if (e != null) {
                        int layerOffsetInTheEdge = NLayering[e.Source] - NLayering[v];
                        List<PolyIntEdge> list = database.Multiedges[new IntPair(e.Source, e.Target)];

                        foreach (PolyIntEdge ie in list) {
                            if (!EdgeIsFlat(ie)) {
                                if (ie != e) {
                                    int u = ie.LayerEdges[layerOffsetInTheEdge].Source;
                                    layer[offset] = u;
                                    nla.X[u] = offset++;
                                } else {
                                    layer[offset] = v;
                                    nla.X[v] = offset++;
                                }
                            }
                        }
                    } else {
                        layer[offset] = v;
                        nla.X[v] = offset++;
                    }
                }
            }
        }

        private bool EdgeIsFlat(PolyIntEdge ie) {
            return la.Y[ie.Source] == la.Y[ie.Target];
        }


        void MapVirtualNodesToEdges() {
            foreach (List<PolyIntEdge> list in this.database.RegularMultiedges)
                foreach (PolyIntEdge e in list)
                    if (! EdgeIsFlat(e))//the edge is not flat
                        foreach (LayerEdge le in e.LayerEdges)
                            if (le.Target != e.Target)
                                this.virtNodesToIntEdges[le.Target] = e;

        }


        private void CreateFullLayeredGraph() {
            int currentVV = this.layeredGraph.NodeCount;
            foreach (KeyValuePair<IntPair, List<PolyIntEdge>>
                    kv in database.Multiedges) {
                if (kv.Key.x != kv.Key.y) { //not a self edge
                    List<PolyIntEdge> list = kv.Value;
                    bool first = true;
                    int span = 0;
                    foreach (PolyIntEdge e in list) {
                        if (first) {
                            first = false;
                            span = e.LayerSpan;
                        } else {
                            e.LayerEdges = new LayerEdge[span];
                            if (span == 1)
                                e.LayerEdges[0] = new LayerEdge(e.Source, e.Target, e.CrossingWeight);
                            else {
                                for (int i = 0; i < span; i++) {
                                    int source = GetSource(ref currentVV, e, i);
                                    int target = GetTarget(ref currentVV, e, i, span);
                                    e.LayerEdges[i] = new LayerEdge(source, target, e.CrossingWeight);
                                }
                            }
                        }
                        LayerInserter.RegisterDontStepOnVertex(this.database, e);
                    }
                }
            }
            this.nLayeredGraph = new ProperLayeredGraph(this.intGraph);
        }

        internal static int GetTarget(ref int currentVV, PolyIntEdge e, int i, int span) {
            if (i < span - 1)
                return currentVV;
            return e.Target;
        }

        internal static int GetSource(ref int currentVV, PolyIntEdge e, int i) {
            if (i == 0)
                return e.Source;
            return currentVV++;
        }

       
        void InitNewLayering() {

       
            nla = new LayerArrays(new int[this.NLayeredGraph.NodeCount]);

            for (int i = 0; i < layeredGraph.NodeCount; i++)
                NLayering[i] = la.Y[i];

            foreach (KeyValuePair<IntPair,List<PolyIntEdge>> kv in database.Multiedges) {
                if (kv.Key.First != kv.Key.Second && la.Y[kv.Key.First]!=la.Y[kv.Key.Second]) { //not a self edge and not a flat edge
                    int layer = 0;
                    bool first = true;
                    List<PolyIntEdge> list = kv.Value;
                    foreach (PolyIntEdge e in list) {
                        if (first) {
                            first = false;
                            layer = la.Y[e.Source];
                        }
                        int cl = layer - 1;
                        foreach (LayerEdge le in e.LayerEdges)
                            NLayering[le.Target] = cl--;
                    }
                }
            }

            int[][] newLayers = new int[la.Layers.Length][];

            //count new layer widths
            int[] counts = new int[newLayers.Length];

            foreach (int l in NLayering)
                counts[l]++;


            for (int i = 0; i < counts.Length; i++)
                newLayers[i] = new int[counts[i]];


            nla = new LayerArrays(NLayering);
            nla.Layers = newLayers;

        }
    }
}
