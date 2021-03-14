using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// Following "A technique for Drawing Directed Graphs" of Gansner, Koutsofios, North and Vo
    /// Works on the layered graph. Also see GraphLayout.pdfhttps://www.researchgate.net/profile/Lev_Nachmanson/publication/30509007_Drawing_graphs_with_GLEE/links/54b6b2930cf2e68eb27edf71/Drawing-graphs-with-GLEE.pdf
    /// </summary>
    internal partial class Ordering : AlgorithmBase{
        #region Fields

        bool hasCrossWeights;

        LayerArrays layerArrays;
        int[][] layerArraysCopy;
        int[] layering;
        int[][] layers;
        OrderingMeasure measure;

        int nOfLayers;
        double[] optimalOriginalGroupSize;
        double[] optimalVirtualGroupSize;
        ProperLayeredGraph properLayeredGraph;

        /// <summary>
        /// this field is needed to randomly choose between transposing
        /// and not transposing in the adjacent exchange
        /// </summary>
        Random random;

        SugiyamaLayoutSettings settings;
        int startOfVirtNodes;

        bool tryReverse = true;

        int NoGainStepsBound {
            get {
                return SugiyamaLayoutSettings.NoGainAdjacentSwapStepsBound*
                       SugiyamaLayoutSettings.RepetitionCoefficientForOrdering;
            }
        }

        SugiyamaLayoutSettings SugiyamaLayoutSettings {
            get { return settings; }
        }

        /// <summary>
        /// gets the random seed for some random choices inside of layer ordering
        /// </summary>
        int SeedOfRandom {
            get { return SugiyamaLayoutSettings.RandomSeedForOrdering; }
        }

        #endregion

        int[] X;

        Ordering(ProperLayeredGraph graphPar, bool tryReverse, LayerArrays layerArraysParam, int startOfVirtualNodes, bool hasCrossWeights, SugiyamaLayoutSettings settings) {
            this.tryReverse = tryReverse;
            startOfVirtNodes = startOfVirtualNodes;
            layerArrays = layerArraysParam;
            layering = layerArraysParam.Y;
            nOfLayers = layerArraysParam.Layers.Length;
            layers = layerArraysParam.Layers;
            properLayeredGraph = graphPar;
            this.hasCrossWeights = hasCrossWeights;
            this.settings = settings;
            random = new Random(SeedOfRandom);
        }

        /// <summary>
        /// an upper limit on a number of passes in layer ordering
        /// </summary>
        int MaxOfIterations {
            get {
                return SugiyamaLayoutSettings.MaxNumberOfPassesInOrdering*
                       SugiyamaLayoutSettings.RepetitionCoefficientForOrdering;
            }
        }

        internal static void OrderLayers(ProperLayeredGraph graph,
                                         LayerArrays layerArrays,
                                         int startOfVirtualNodes,
                                         SugiyamaLayoutSettings settings, CancelToken cancelToken) {
            bool hasCrossWeight = false;
            foreach (LayerEdge le in graph.Edges)
                if (le.CrossingWeight != 1) {
                    hasCrossWeight = true;
                    break;
                }

            var o = new Ordering(graph, true, layerArrays, startOfVirtualNodes, hasCrossWeight, settings);
            o.Run(cancelToken);
        }

        protected override void RunInternal()
        {
#if DEBUGORDERING
      if (graph.NumberOfVertices != layering.Length)
        throw new System.Exception("the layering does not correspond to the graph");
      foreach (IntEdge e in graph.Edges)
        if (layering[e.Source] - layering[e.Target] != 1)
          throw new System.Exception("the edge in the graph does not span exactly one layer:" + e);
#endif

#if PPC // Parallel -- susanlwo
            LayerArrays secondLayers = null;
            Ordering revOrdering = null;
            System.Threading.Tasks.Task t = null;

            if (/*orderingMeasure.x>0 &&*/ tryReverse) {
                secondLayers = layerArrays.ReversedClone();

                revOrdering = new Ordering(properLayeredGraph.ReversedClone(), false, secondLayers, startOfVirtNodes, balanceVirtAndOrigNodes, this.hasCrossWeights, settings);

                // note: below we need to pass the CancelToken from this thread into the new thread, to make sure a previous thread from the ThreadPool's
                // thread static token (that may be in a cancelled state) is picked up.
                t = System.Threading.Tasks.Task.Factory.StartNew(() => revOrdering.Run(this.CancelToken));
            }

            Calculate();

            if (/*orderingMeasure.x>0 &&*/ tryReverse) {

                t.Wait();

                if (revOrdering.measure < measure) {
                    for (int j = 0; j < nOfLayers; j++)
                        secondLayers.Layers[j].CopyTo(layerArrays.Layers[nOfLayers - 1 - j], 0);

                    layerArrays.UpdateXFromLayers();
                }
            }
#else
            Calculate();

            if ( /*orderingMeasure.x>0 &&*/ tryReverse) {
                LayerArrays secondLayers = layerArrays.ReversedClone();

                var revOrdering = new Ordering(properLayeredGraph.ReversedClone(), false, secondLayers, startOfVirtNodes, 
                                               hasCrossWeights, settings);

                revOrdering.Run();

                if (revOrdering.measure < measure) {
                    for (int j = 0; j < nOfLayers; j++)
                        secondLayers.Layers[j].CopyTo(layerArrays.Layers[nOfLayers - 1 - j], 0);

                    layerArrays.UpdateXFromLayers();
                }
            }
#endif
        }

        void Calculate() {
            Init();

            CloneLayers(layers, ref layerArraysCopy);
            int countOfNoGainSteps = 0;
            measure = new OrderingMeasure(layerArraysCopy, GetCrossingsTotal(properLayeredGraph, layerArrays),
                                          startOfVirtNodes, optimalOriginalGroupSize,
                                          optimalVirtualGroupSize);

            //Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < MaxOfIterations && countOfNoGainSteps < NoGainStepsBound && !measure.IsPerfect(); i++) {
                this.ProgressStep();

                bool up = i%2 == 0;

                LayerByLayerSweep(up);

                AdjacentExchange();

                var newMeasure = new OrderingMeasure(layerArrays.Layers,
                                                     GetCrossingsTotal(properLayeredGraph, layerArrays),
                                                     startOfVirtNodes,
                                                     optimalOriginalGroupSize, optimalVirtualGroupSize);

                if (measure < newMeasure) {
                    Restore();
                    countOfNoGainSteps++;
                } else if (newMeasure < measure || HeadOfTheCoin()) {
                    countOfNoGainSteps = 0;
                    CloneLayers(layers, ref layerArraysCopy);
                    measure = newMeasure;
                }
            }
        }

        internal static void CloneLayers(int[][] layers, ref int[][] layerArraysCopy) {
            if (layerArraysCopy == null) {
                layerArraysCopy = (int[][]) layers.Clone();
                for (int i = 0; i < layers.Length; i++)
                    layerArraysCopy[i] = (int[]) layers[i].Clone();
            } else
                for (int i = 0; i < layers.Length; i++)
                    layers[i].CopyTo(layerArraysCopy[i], 0);
        }

        void Restore() {
            layerArrays.UpdateLayers(layerArraysCopy);
        }

        void LayerByLayerSweep(bool up) {
            if (up) {
                for (int i = 1; i < nOfLayers; i++)
                    SweepLayer(i, true);
            } else
                for (int i = nOfLayers - 2; i >= 0; i--)
                    SweepLayer(i, false);
        }

        //static int count;

        /// <summary>
        /// the layer layer-1 is fixed if 
        /// upperLayer us true and layer+1 is fixed in 
        /// the opposite case
        /// the layer with index "layer" is updated    
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="upperLayer">upperLayer means if "layer" is the upperLayer 
        /// of the strip</param>
        void SweepLayer(int layer, bool upperLayer) {
            this.ProgressStep();

            int[] l = layers[layer];
            var medianValues = new float[l.Length];

            for (int i = 0; i < medianValues.Length; i++)
                medianValues[i] = WMedian(l[i], upperLayer);

            Sort(layer, medianValues);

            //update X
            int[] vertices = layerArrays.Layers[layer];
            for (int i = 0; i < vertices.Length; i++)
                layerArrays.X[vertices[i]] = i;
        }

        /// <summary>
        /// sorts layerToSort according to medianValues
        /// if medianValues[i] is -1 then layer[i] does not move
        /// </summary>
        /// <param name="layerToSort"></param>
        /// <param name="medianValues"></param>
        void Sort(int layerToSort, float[] medianValues) {
            var s = new SortedDictionary<float, object>();
            int[] vertices = layers[layerToSort];
            int i = 0;

            foreach (float m in medianValues) {
                int v = vertices[i++];
                if (m == -1.0)
                    continue;

                if (!s.ContainsKey(m))
                    s[m] = v;
                else {
                    object o = s[m];
                    var al = o as List<int>;
                    if (al != null) {
                        if (HeadOfTheCoin())
                            al.Add(v);
                        else {
//stick it in the middle 
                            int j = random.Next(al.Count);
                            int k = al[j];
                            al[j] = v;
                            al.Add(k);
                        }
                    } else {
                        var io = (int) o;
                        al = new List<int>();
                        s[m] = al;
                        if (HeadOfTheCoin()) {
                            al.Add(io);
                            al.Add(v);
                        } else {
                            al.Add(v);
                            al.Add(io);
                        }
                    }
                }
            }

            var senum = s.GetEnumerator();

            for (i = 0; i < vertices.Length;) {
                if (medianValues[i] != -1) {
                    senum.MoveNext();

                    object o = senum.Current.Value;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=347
                    //SharpKit/Colin - is check gets converted to true
                    if (o.GetType() == typeof(int))
#else
                    if (o is int)
#endif
                        vertices[i++] = (int) o;
                    else {
                        var al = o as List<int>;
                        foreach (int v in al) {
//find the first empty spot
                            while (medianValues[i] == -1)
                                i++;
                            vertices[i++] = v;
                        }
                    }
                } else
                    i++;
            }
        }


        float WMedian(int node, bool theMedianGoingDown) {
            IEnumerable<LayerEdge> edges;
            int p;
            if (theMedianGoingDown) {
                edges = properLayeredGraph.OutEdges(node);
                p = properLayeredGraph.OutEdgesCount(node);
            } else {
                edges = properLayeredGraph.InEdges(node);
                p = properLayeredGraph.InEdgesCount(node);
            }

            if (p == 0)
                return -1.0f;

            var parray = new int[p]; //we have no multiple edges

            int i = 0;
            if (theMedianGoingDown)
                foreach (LayerEdge e in edges)
                    parray[i++] = X[e.Target];
            else
                foreach (LayerEdge e in edges)
                    parray[i++] = X[e.Source];

            Array.Sort(parray);

            int m = p/2;

            if (p%2 == 1)
                return parray[m];

            if (p == 2)
                return 0.5f*((float) parray[0] + (float) parray[1]);

            float left = parray[m - 1] - parray[0];

            float right = parray[p - 1] - parray[m];

            return ((float) parray[m - 1]*left + (float) parray[m]*right)/(left + right);
        }


        /// <summary>
        /// Just depth search and assign the index saying when the node was visited
        /// </summary>
        void Init() {
            var counts = new int[nOfLayers];

            //the initial layers are set by following the order of the 
            //depth first traversal inside one layer
            var q = new Stack<int>();
            //enqueue all sources of the graph 
            for (int i = 0; i < properLayeredGraph.NodeCount; i++)
                if (properLayeredGraph.InEdgesCount(i) == 0)
                    q.Push(i);


            var visited = new bool[properLayeredGraph.NodeCount];

            while (q.Count > 0) {
                int u = q.Pop();
                int l = layerArrays.Y[u];


                layerArrays.Layers[l][counts[l]] = u;
                layerArrays.X[u] = counts[l];
                counts[l]++;

                foreach (int v in properLayeredGraph.Succ(u))
                    if (!visited[v]) {
                        visited[v] = true;
                        q.Push(v);
                    }
            }

            X = layerArrays.X;

        }

        void InitOptimalGroupSizes() {
            optimalOriginalGroupSize = new double[nOfLayers];
            optimalVirtualGroupSize = new double[nOfLayers];

            for (int i = 0; i < nOfLayers; i++)
                InitOptimalGroupSizesForLayer(i);
        }

        void InitOptimalGroupSizesForLayer(int i) {
            //count original and virtual nodes
            int originals = 0;
            foreach (int j in layers[i])
                if (j < startOfVirtNodes)
                    originals++;

            int virtuals = layers[i].Length - originals;

            if (originals < virtuals) {
                optimalOriginalGroupSize[i] = 1;
                optimalVirtualGroupSize[i] = (double) virtuals/(originals + 1);
            } else {
                optimalVirtualGroupSize[i] = 1;
                optimalOriginalGroupSize[i] = (double) originals/(virtuals + 1);
            }
        }


        internal static int GetCrossingsTotal(ProperLayeredGraph properLayeredGraph, LayerArrays layerArrays) {
            int x = 0;
            for (int i = 0; i < layerArrays.Layers.Length - 1; i++)
                x += GetCrossingCountFromStrip(i, properLayeredGraph, layerArrays);

            return x;
        }


        ///// <summary>
        ///// This method can be improved: see the paper Simple And Efficient ...
        ///// </summary>
        ///// <param name="graph"></param>
        ///// <param name="layerArrays"></param>
        ///// <param name="bottom">bottom of the strip</param>
        ///// <returns></returns>
        static int GetCrossingCountFromStrip(int bottom, ProperLayeredGraph properLayeredGraph, LayerArrays layerArrays) {
            int[] topVerts = layerArrays.Layers[bottom + 1];
            int[] bottomVerts = layerArrays.Layers[bottom];
            if (bottomVerts.Length <= topVerts.Length)
                return GetCrossingCountFromStripWhenBottomLayerIsShorter(bottomVerts, properLayeredGraph, layerArrays);
            else
                return GetCrossingCountFromStripWhenTopLayerIsShorter(topVerts, bottomVerts, properLayeredGraph,
                                                                      layerArrays);
        }

        static int GetCrossingCountFromStripWhenTopLayerIsShorter(int[] topVerts, int[] bottomVerts,
                                                                  ProperLayeredGraph properLayeredGraph,
                                                                  LayerArrays layerArrays) {
            LayerEdge[] edges = EdgesOfStrip(bottomVerts, properLayeredGraph);
            Array.Sort(edges, new EdgeComparerByTarget(layerArrays.X));
            //find first n such that 2^n >=topVerts.Length
            int n = 1;
            while (n < topVerts.Length)
                n *= 2;
            //init the accumulator tree

            var tree = new int[2*n - 1];

            n--; // the first bottom node starts from n now

            int cc = 0; //number of crossings
            foreach (LayerEdge edge in edges) {
                int index = n + layerArrays.X[edge.Source];
                int ew = edge.CrossingWeight;
                tree[index] += ew;
                while (index > 0) {
                    if (index%2 != 0)
                        cc += ew*tree[index + 1]; //intersect everything accumulated in the right sibling 
                    index = (index - 1)/2;
                    tree[index] += ew;
                }
            }
            return cc;
        }


        static int GetCrossingCountFromStripWhenBottomLayerIsShorter(int[] bottomVerts,
                                                                     ProperLayeredGraph properLayeredGraph,
                                                                     LayerArrays layerArrays) {
            LayerEdge[] edges = EdgesOfStrip(bottomVerts, properLayeredGraph);
            Array.Sort(edges, new EdgeComparerBySource(layerArrays.X));
            //find first n such that 2^n >=bottomVerts.Length
            int n = 1;
            while (n < bottomVerts.Length)
                n *= 2;
            //init accumulator

            var tree = new int[2*n - 1];

            n--; // the first bottom node starts from n now

            int cc = 0; //number of crossings
            foreach (LayerEdge edge in edges) {
                int index = n + layerArrays.X[edge.Target];
                int ew = edge.CrossingWeight;
                tree[index] += ew;
                while (index > 0) {
                    if (index%2 != 0)
                        cc += ew*tree[index + 1]; //intersect everything accumulated in the right sibling 
                    index = (index - 1)/2;
                    tree[index] += ew;
                }
            }

            return cc;
        }

        static LayerEdge[] EdgesOfStrip(int[] bottomVerts, ProperLayeredGraph properLayeredGraph) {
            LayerEdge[] edges = (from v in bottomVerts
                                 from e in properLayeredGraph.InEdges(v)
                                 select e).ToArray();

            return edges;
        }
    }
}