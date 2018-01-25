using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Msagl.Core.DataStructures;
using System.Diagnostics;

namespace Microsoft.Msagl.Layout.Layered {
    internal class AdjacentSwapsWithConstraints {
        const int maxNumberOfAdjacentExchanges = 50;
        readonly bool hasCrossWeights;

        readonly LayerInfo[] layerInfos;
        readonly int[] layering;
        readonly int[][] layers;
        readonly ProperLayeredGraph properLayeredGraph;
        readonly Random random = new Random(1);
        readonly int[] X;
        Dictionary<int, int>[] inCrossingCount;
        Dictionary<int, int>[] outCrossingCount;

        /// <summary>
        /// for each vertex v let P[v] be the array of predeccessors of v
        /// </summary>
        List<int>[] P;


        /// <summary>
        /// The array contains a dictionary per vertex
        /// The value POrder[v][u] gives the offset of u in the array P[v]
        /// </summary>
        Dictionary<int, int>[] POrder;

        /// <summary>
        /// for each vertex v let S[v] be the array of successors of v
        /// </summary>
        List<int>[] S;

        /// <summary>
        /// The array contains a dictionary per vertex
        /// The value SOrder[v][u] gives the offset of u in the array S[v]
        /// </summary>
        Dictionary<int, int>[] SOrder;

        internal AdjacentSwapsWithConstraints(LayerArrays layerArray,
                                              bool hasCrossWeights,
                                              ProperLayeredGraph properLayeredGraph,
                                              LayerInfo[] layerInfos) {
            X = layerArray.X;
            layering = layerArray.Y;
            layers = layerArray.Layers;
            this.properLayeredGraph = properLayeredGraph;
            this.hasCrossWeights = hasCrossWeights;
            this.layerInfos = layerInfos;
        }

        /// <summary>
        /// Gets or sets the number of of passes over all layers to run
        /// adjacent exchanges, where every pass goes
        /// all way up to the top layer and down to the lowest layer
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static int MaxNumberOfAdjacentExchanges {
            get { return maxNumberOfAdjacentExchanges; }
        }

        bool ExchangeWithGainWithNoDisturbance(int[] layer) {
            bool wasGain = false;

            bool gain;
            do {
                gain = ExchangeWithGain(layer);
                wasGain = wasGain || gain;
            } while (gain);

            return wasGain;
        }

        bool CanSwap(int i, int j) {
            if (IsVirtualNode(i) || IsVirtualNode(j))
                return true;
            LayerInfo layerInfo = layerInfos[layering[i]];
            if (layerInfo == null)
                return true;
            if (ConstrainedOrdering.BelongsToNeighbBlock(i, layerInfo)
                ||
                ConstrainedOrdering.BelongsToNeighbBlock(j, layerInfo)
                ||
                layerInfo.constrainedFromAbove.ContainsKey(i)
                ||
                layerInfo.constrainedFromBelow.ContainsKey(j)
                )
                return false;

            if (layerInfo.leftRight.Contains(new Tuple<int, int>(i, j)))
                return false;
            return true;
        }

        bool IsVirtualNode(int v) {
            return properLayeredGraph.IsVirtualNode(v);
        }

        ///// <summary>
        ///// swaps two vertices only if reduces the number of intersections
        ///// </summary>
        ///// <param name="layer">the layer to work on</param>
        ///// <param name="u">left vertex</param>
        ///// <param name="v">right vertex</param>
        ///// <returns></returns>
        bool SwapWithGain(int u, int v) {
            int gain = SwapGain(u, v);

            if (gain > 0) {
                Swap(u, v);
                return true;
            }
            return false;
        }

        int SwapGain(int u, int v) {
            if (!CanSwap(u, v))
                return -1;
            int cuv;
            int cvu;
            CalcPair(u, v, out cuv, out cvu);
            return cuv - cvu;
        }

        /// <summary>
        /// calculates the number of intersections between edges adjacent to u and v
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">a vertex</param>
        /// <param name="cuv">the result when u is to the left of v</param>
        /// <param name="cvu">the result when v is to the left of u</param>
        void CalcPair(int u, int v, out int cuv, out int cvu) {
            List<int> su = S[u], sv = S[v], pu = P[u], pv = P[v];
            if (!hasCrossWeights) {
                cuv = CountOnArrays(su, sv) +
                      CountOnArrays(pu, pv);
                cvu = CountOnArrays(sv, su) +
                      CountOnArrays(pv, pu);
            } else {
                Dictionary<int, int> uOutCrossCounts = outCrossingCount[u];
                Dictionary<int, int> vOutCrossCounts = outCrossingCount[v];
                Dictionary<int, int> uInCrossCounts = inCrossingCount[u];
                Dictionary<int, int> vInCrossCounts = inCrossingCount[v];
                cuv = CountOnArrays(su, sv, uOutCrossCounts, vOutCrossCounts) +
                      CountOnArrays(pu, pv, uInCrossCounts, vInCrossCounts);
                cvu = CountOnArrays(sv, su, vOutCrossCounts, uOutCrossCounts) +
                      CountOnArrays(pv, pu, vInCrossCounts, uInCrossCounts);
            }
        }

        int CountOnArrays(List<int> unbs, List<int> vnbs) {
            int ret = 0;
            int vl = vnbs.Count - 1;
            int j = -1; //the right most position of vnbs to the left from the current u neighbor 
            int vnbsSeenAlready = 0;
            foreach (int uNeighbor in unbs) {
                int xu = X[uNeighbor];
                for (; j < vl && X[vnbs[j + 1]] < xu; j++)
                    vnbsSeenAlready++;
                ret += vnbsSeenAlready;
            }
            return ret;
        }

        /// <summary>
        /// every inversion between unbs and vnbs gives an intersecton
        /// </summary>
        /// <param name="unbs">neighbors of u but only from one layer</param>
        /// <param name="vnbs">neighbors of v from the same layers</param>
        /// <returns>number of intersections when u is to the left of v</returns>
        /// <param name="uCrossingCounts"></param>
        /// <param name="vCrossingCount"></param>
        int CountOnArrays(List<int> unbs, List<int> vnbs, Dictionary<int, int> uCrossingCounts,
                          Dictionary<int, int> vCrossingCount) {
            int ret = 0;
            int vl = vnbs.Count - 1;
            int j = -1; //the right most position of vnbs to the left from the current u neighbor 

            int vCrossingNumberSeenAlready = 0;
            foreach (int uNeib in unbs) {
                int xu = X[uNeib];
                int vnb;
                for (; j < vl && X[vnb = vnbs[j + 1]] < xu; j++)
                    vCrossingNumberSeenAlready += vCrossingCount[vnb];
                ret += vCrossingNumberSeenAlready*uCrossingCounts[uNeib];
            }
            return ret;
        }


        //in this routine u and v are adjacent, and u is to the left of v before the swap
        void Swap(int u, int v) {
            Debug.Assert(UAndVAreOnSameLayer(u, v));
            Debug.Assert(UIsToTheLeftOfV(u, v));
            Debug.Assert(CanSwap(u, v));

            int left = X[u];
            int right = X[v];
            int ln = layering[u]; //layer number
            int[] layer = layers[ln];

            layer[left] = v;
            layer[right] = u;

            X[u] = right;
            X[v] = left;

            //update sorted arrays POrders and SOrders
            //an array should be updated only in case it contains both u and v.
            // More than that, v has to follow u in an the array.

            UpdateSsContainingUV(u, v);

            UpdatePsContainingUV(u, v);
        }

        bool ExchangeWithGain(int[] layer) {
            //find a first pair giving some gain
            for (int i = 0; i < layer.Length - 1; i++)
                if (SwapWithGain(layer[i], layer[i + 1])) {
                    SwapToTheLeft(layer, i);
                    SwapToTheRight(layer, i + 1);
                    return true;
                }

            return false;
        }

        bool HeadOfTheCoin() {
            return random.Next(2) == 0;
        }

        
        internal void DoSwaps() {
            InitArrays();
            int count = 0;
            bool progress = true;
            while (progress && count++ < MaxNumberOfAdjacentExchanges) {
                progress = false;
                for (int i = 0; i < layers.Length; i++)
                    progress = AdjExchangeLayer(i) || progress;
            
                for (int i = layers.Length - 2; i >= 0; i--)
                    progress = AdjExchangeLayer(i) || progress;
            }
            Debug.Assert(SPAreCorrect());
        }

        private bool SPAreCorrect()
        {
            int n = this.properLayeredGraph.NodeCount;
            for (int i = 0; i < n; i++)
                if (!SIsCorrect(i))
                    return false;

            return true;
        }

        private bool SIsCorrect(int i)
        {
            var s = S[i];
            Dictionary<int, int> so = SOrder[i];
            for (int k = 0; k < s.Count; k++)
            {
                int u = s[k];
                int uPosition = 0;
                if (so.TryGetValue(u, out uPosition) == false)
                    return false;
                if (uPosition != k)
                    return false;
            }

            for (int k = 0; k < s.Count - 1; k++)
            {
                int u = s[k];
                int v = s[k + 1];
                if (!UIsToTheLeftOfV(u, v))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Is called just after median layer swap is done
        /// </summary>
        void InitArrays() {
            if (S == null)
                AllocArrays();

            for (int i = 0; i < properLayeredGraph.NodeCount; i++) {
                POrder[i].Clear();
                SOrder[i].Clear();
                S[i].Clear();
                P[i].Clear();
            }


            for (int i = 0; i < layers.Length; i++)
                InitPSArraysForLayer(layers[i]);
        }

        void DisturbLayer(int[] layer) {
            for (int i = 0; i < layer.Length - 1; i++)
                AdjacentSwapToTheRight(layer, i);
        }

        bool AdjExchangeLayer(int i) {
            int[] layer = layers[i];
            bool gain = ExchangeWithGainWithNoDisturbance(layer);

            if (gain)
                return true;

            DisturbLayer(layer);

            return ExchangeWithGainWithNoDisturbance(layer);
        }

        void AllocArrays() {
            int n = properLayeredGraph.NodeCount;
            P = new List<int>[n];
            S = new List<int>[n];


            POrder = new Dictionary<int, int>[n];
            SOrder = new Dictionary<int, int>[n];
            if (hasCrossWeights) {
                outCrossingCount = new Dictionary<int, int>[n];
                inCrossingCount = new Dictionary<int, int>[n];
            }
            for (int i = 0; i < n; i++) {
                int count = properLayeredGraph.InEdgesCount(i);
                P[i] = new List<int>();
                if (hasCrossWeights) {
                    Dictionary<int, int> inCounts = inCrossingCount[i] = new Dictionary<int, int>(count);
                    foreach (LayerEdge le in properLayeredGraph.InEdges(i))
                        inCounts[le.Source] = le.CrossingWeight;
                }
                POrder[i] = new Dictionary<int, int>(count);
                count = properLayeredGraph.OutEdgesCount(i);
                S[i] = new List<int>();
                SOrder[i] = new Dictionary<int, int>(count);
                if (hasCrossWeights) {
                    Dictionary<int, int> outCounts = outCrossingCount[i] = new Dictionary<int, int>(count);
                    foreach (LayerEdge le in properLayeredGraph.OutEdges(i))
                        outCounts[le.Target] = le.CrossingWeight;
                }
            }
        }

        void UpdatePsContainingUV(int u, int v) {
            if (S[u].Count <= S[v].Count)
                foreach (int a in S[u]) {
                    Dictionary<int, int> porder = POrder[a];
                    //of course porder contains u, let us see if it contains v
                    if (porder.ContainsKey(v)) {
                        int vOffset = porder[v];
                        //swap u and v in the array P[coeff]
                        var p = P[a];
                        p[vOffset - 1] = v;
                        p[vOffset] = u;
                        //update sorder itself
                        porder[v] = vOffset - 1;
                        porder[u] = vOffset;
                    }
                }
            else
                foreach (int a in S[v]) {
                    Dictionary<int, int> porder = POrder[a];
                    //of course porder contains u, let us see if it contains v
                    if (porder.ContainsKey(u)) {
                        int vOffset = porder[v];
                        //swap u and v in the array P[coeff]
                        var p = P[a];
                        p[vOffset - 1] = v;
                        p[vOffset] = u;
                        //update sorder itself
                        porder[v] = vOffset - 1;
                        porder[u] = vOffset;
                    }
                }
        }

        void SwapToTheRight(int[] layer, int i) {
            for (int j = i; j < layer.Length - 1; j++)
                AdjacentSwapToTheRight(layer, j);
        }

        void SwapToTheLeft(int[] layer, int i) {
            for (int j = i - 1; j >= 0; j--)
                AdjacentSwapToTheRight(layer, j);
        }

        /// <summary>
        /// swaps i-th element with i+1
        /// </summary>
        /// <param name="layer">the layer to work on</param>
        /// <param name="i">the position to start</param>
        /// <returns></returns>
        void AdjacentSwapToTheRight(int[] layer, int i) {
            int u = layer[i], v = layer[i + 1];

            int gain = SwapGain(u, v);

            if (gain > 0 || (gain == 0 && HeadOfTheCoin())) {
                Swap(u, v);
                return;
            }
        }

        /// <summary>
        /// Sweep layer from left to right and fill S,P arrays as we go.
        /// The arrays P and S will be sorted according to X. Note that we will not keep them sorted
        /// as we doing adjacent swaps. Initial sorting only needed to calculate initial clr,crl values.
        /// </summary>
        /// <param name="layer"></param>
        void InitPSArraysForLayer(int[] layer) {
            foreach (int l in layer)
            {
                foreach (int p in properLayeredGraph.Pred(l))
                {
                    Dictionary<int, int> so = SOrder[p];
                    if (so.ContainsKey(l))
                        continue;
                    int sHasNow = so.Count;
                    S[p].Add(l); //l takes the first available slot in S[p]
                    so[l] = sHasNow;
                }
                foreach (int s in properLayeredGraph.Succ(l))
                {
                    Dictionary<int, int> po = POrder[s];
                    if (po.ContainsKey(l))
                        continue;
                    int pHasNow = po.Count;
                    P[s].Add(l); //l take the first available slot in P[s]
                    po[l] = pHasNow;
                }
            }
        }

        void UpdateSsContainingUV(int u, int v) {
            if (P[u].Count <= P[v].Count)
                foreach (int a in P[u]) {
                    Dictionary<int, int> sorder = SOrder[a];
                    //of course sorder contains u, let us see if it contains v
                    if (sorder.ContainsKey(v)) {
                        int vOffset = sorder[v];
                        //swap u and v in the array S[coeff]
                        var s = S[a];
                        s[vOffset - 1] = v;
                        s[vOffset] = u;
                        //update sorder itself
                        sorder[v] = vOffset - 1;
                        sorder[u] = vOffset;
                    }
                }
            else
                foreach (int a in P[v]) {
                    Dictionary<int, int> sorder = SOrder[a];
                    //of course sorder contains u, let us see if it contains v
                    if (sorder.ContainsKey(u)) {
                        int vOffset = sorder[v];
                        //swap u and v in the array S[coeff]
                        var s = S[a];
                        s[vOffset - 1] = v;
                        s[vOffset] = u;
                        //update sorder itself
                        sorder[v] = vOffset - 1;
                        sorder[u] = vOffset;
                    }
                }
        }

        private bool UAndVAreOnSameLayer(int u, int v)
        {
            return layering[u] == layering[v];
        }

        private bool UIsToTheLeftOfV(int u, int v)
        {
            return X[u] < X[v];
        }
    }
}