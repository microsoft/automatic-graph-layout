using System;
using System.Collections.Generic;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// Following "A technique for Drawing Directed Graphs" of Gansner, Koutsofios, North and Vo
    /// Works on the layered graph. 
    /// For explanations of the algorithm here see https://www.researchgate.net/profile/Lev_Nachmanson/publication/30509007_Drawing_graphs_with_GLEE/links/54b6b2930cf2e68eb27edf71/Drawing-graphs-with-GLEE.pdf
    /// 
    /// </summary>
    internal partial class Ordering {
        /// <summary>
        /// for each vertex v let P[v] be the array of predeccessors of v
        /// </summary>
        int[][] predecessors;


        /// <summary>
        /// The array contains a dictionary per vertex
        /// The value POrder[v][u] gives the offset of u in the array P[v]
        /// </summary>
        Dictionary<int, int>[] pOrder;

        /// <summary>
        /// for each vertex v let S[v] be the array of successors of v
        /// </summary>
        int[][] successors;

        /// <summary>
        /// The array contains a dictionary per vertex
        /// The value SOrder[v][u] gives the offset of u in the array S[v]
        /// </summary>
        Dictionary<int, int>[] sOrder;

        Dictionary<int, int>[] inCrossingCount;
        /// <summary>
        /// Gets or sets the number of of passes over all layers to rung adjacent exchanges, where every pass goes '
        /// all way up to the top layer and down to the lowest layer
        /// </summary>
        const int MaxNumberOfAdjacentExchanges = 50;
        
        Dictionary<int, int>[] outCrossingCount;

        
        bool HeadOfTheCoin() {
            return random.Next(2) == 0;
        }


        void AdjacentExchange() {
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
        }

        void AllocArrays() {
            int n = properLayeredGraph.NodeCount;
            predecessors = new int[n][];
            successors = new int[n][];


            pOrder = new Dictionary<int, int>[n];
            sOrder = new Dictionary<int, int>[n];
            if (hasCrossWeights) {
                outCrossingCount = new Dictionary<int, int>[n];
                inCrossingCount = new Dictionary<int, int>[n];
            }
            for (int i = 0; i < n; i++) {
                int count = properLayeredGraph.InEdgesCount(i);
                predecessors[i] = new int[count];
                if (hasCrossWeights) {
                    Dictionary<int, int> inCounts = inCrossingCount[i] = new Dictionary<int, int>(count);
                    foreach (LayerEdge le in properLayeredGraph.InEdges(i))
                        inCounts[le.Source] = le.CrossingWeight;
                }
                pOrder[i] = new Dictionary<int, int>(count);
                count = properLayeredGraph.OutEdgesCount(i);
                successors[i] = new int[count];
                sOrder[i] = new Dictionary<int, int>(count);
                if (hasCrossWeights) {
                    Dictionary<int, int> outCounts = outCrossingCount[i] = new Dictionary<int, int>(count);
                    foreach (LayerEdge le in properLayeredGraph.OutEdges(i))
                        outCounts[le.Target] = le.CrossingWeight;
                }
            }
        }

        /// <summary>
        /// Is called just after median layer swap is done
        /// </summary>
        void InitArrays() {
            if (successors == null)
                AllocArrays();


            for (int i = 0; i < properLayeredGraph.NodeCount; i++) {
                pOrder[i].Clear();
                sOrder[i].Clear();
            }


            foreach (int[] t in layers)
                InitPsArraysForLayer(t);
        }


        /// <summary>
        /// calculates the number of intersections between edges adjacent to u and v
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">a vertex</param>
        /// <param name="cuv">the result when u is to the left of v</param>
        /// <param name="cvu">the result when v is to the left of u</param>
        void CalcPair(int u, int v, out int cuv, out int cvu) {
            int[] su = successors[u], sv = successors[v], pu = predecessors[u], pv = predecessors[v];
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

        /// <summary>
        /// Sweep layer from left to right and fill S,P arrays as we go.
        /// The arrays P and S will be sorted according to X. Note that we will not keep them sorted
        /// as we doing adjacent swaps. Initial sorting only needed to calculate initial clr,crl values.
        /// </summary>
        /// <param name="layer"></param>
        void InitPsArraysForLayer(int[] layer) {
            this.ProgressStep();

            foreach (int l in layer) {
                foreach (int p in properLayeredGraph.Pred(l)) {
                    Dictionary<int, int> so = sOrder[p];
                    int sHasNow = so.Count;
                    successors[p][sHasNow] = l; //l takes the first available slot in S[p]
                    so[l] = sHasNow;
                }
                foreach (int s in properLayeredGraph.Succ(l)) {
                    Dictionary<int, int> po = pOrder[s];
                    int pHasNow = po.Count;
                    predecessors[s][pHasNow] = l; //l take the first available slot in P[s]
                    po[l] = pHasNow;
                }
            }
        }

        int CountOnArrays(int[] unbs, int[] vnbs) {
            int ret = 0;
            int vl = vnbs.Length - 1;
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
        int CountOnArrays(int[] unbs, int[] vnbs, Dictionary<int, int> uCrossingCounts,
                          Dictionary<int, int> vCrossingCount) {
            int ret = 0;
            int vl = vnbs.Length - 1;
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

        bool AdjExchangeLayer(int i) {
            this.ProgressStep();

            int[] layer = layers[i];
            bool gain = ExchangeWithGainWithNoDisturbance(layer);

            if (gain)
                return true;

            DisturbLayer(layer);

            return ExchangeWithGainWithNoDisturbance(layer);
        }

        //in this routine u and v are adjacent, and u is to the left of v before the swap
        void Swap(int u, int v) {
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

            UpdateSsContainingUv(u, v);

            UpdatePsContainingUv(u, v);
        }

        void UpdatePsContainingUv(int u, int v) {
            if (successors[u].Length <= successors[v].Length)
                foreach (int a in successors[u]) {
                    Dictionary<int, int> porder = pOrder[a];
                    //of course porder contains u, let us see if it contains v
                    if (porder.ContainsKey(v)) {
                        int vOffset = porder[v];
                        //swap u and v in the array P[coeff]
                        int[] p = predecessors[a];
                        p[vOffset - 1] = v;
                        p[vOffset] = u;
                        //update sorder itself
                        porder[v] = vOffset - 1;
                        porder[u] = vOffset;
                    }
                }
            else
                foreach (int a in successors[v]) {
                    Dictionary<int, int> porder = pOrder[a];
                    //of course porder contains u, let us see if it contains v
                    if (porder.ContainsKey(u)) {
                        int vOffset = porder[v];
                        //swap u and v in the array P[coeff]
                        int[] p = predecessors[a];
                        p[vOffset - 1] = v;
                        p[vOffset] = u;
                        //update sorder itself
                        porder[v] = vOffset - 1;
                        porder[u] = vOffset;
                    }
                }
        }

        void UpdateSsContainingUv(int u, int v) {
            if (predecessors[u].Length <= predecessors[v].Length)
                foreach (int a in predecessors[u]) {
                    Dictionary<int, int> sorder = sOrder[a];
                    //of course sorder contains u, let us see if it contains v
                    if (sorder.ContainsKey(v)) {
                        int vOffset = sorder[v];
                        //swap u and v in the array S[coeff]
                        int[] s = successors[a];
                        s[vOffset - 1] = v;
                        s[vOffset] = u;
                        //update sorder itself
                        sorder[v] = vOffset - 1;
                        sorder[u] = vOffset;
                    }
                }
            else
                foreach (int a in predecessors[v]) {
                    Dictionary<int, int> sorder = sOrder[a];
                    //of course sorder contains u, let us see if it contains v
                    if (sorder.ContainsKey(u)) {
                        int vOffset = sorder[v];
                        //swap u and v in the array S[coeff]
                        int[] s = successors[a];
                        s[vOffset - 1] = v;
                        s[vOffset] = u;
                        //update sorder itself
                        sorder[v] = vOffset - 1;
                        sorder[u] = vOffset;
                    }
                }
        }


        void DisturbLayer(int[] layer) {
            for (int i = 0; i < layer.Length - 1; i++)
                AdjacentSwapToTheRight(layer, i);
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

        
        void SwapToTheLeft(int[] layer, int i) {
            for (int j = i - 1; j >= 0; j--)
                AdjacentSwapToTheRight(layer, j);
        }

        void SwapToTheRight(int[] layer, int i) {
            for (int j = i; j < layer.Length - 1; j++)
                AdjacentSwapToTheRight(layer, j);
        }

        /// <summary>
        /// swaps i-th element with i+1
        /// </summary>
        /// <param name="layer">the layer to work on</param>
        /// <param name="i">the position to start</param>
        void AdjacentSwapToTheRight(int[] layer, int i) {
            int u = layer[i], v = layer[i + 1];

            int gain = SwapGain(u, v);

            if (gain > 0 || (gain == 0 && HeadOfTheCoin()))
                Swap(u, v);
        }

        int SwapGain(int u, int v) {
            int cuv;
            int cvu;
            CalcPair(u, v, out cuv, out cvu);
            return cuv - cvu;
        }

        bool UvAreOfSameKind(int u, int v) {
            return u < startOfVirtNodes && v < startOfVirtNodes || u >= startOfVirtNodes && v >= startOfVirtNodes;
        }

        int SwapGroupGain(int u, int v) {
            int layerIndex = layerArrays.Y[u];
            int[] layer = layers[layerIndex];

            if (NeighborsForbidTheSwap(u, v))
                return -1;

            int uPosition = X[u];
            bool uIsSeparator;
            if (IsOriginal(u))
                uIsSeparator = optimalOriginalGroupSize[layerIndex] == 1;
            else
                uIsSeparator = optimalVirtualGroupSize[layerIndex] == 1;

            int delta = CalcDeltaBetweenGroupsToTheLeftAndToTheRightOfTheSeparator(layer,
                                                                                   uIsSeparator
                                                                                       ? uPosition
                                                                                       : uPosition + 1,
                                                                                   uIsSeparator ? u : v);

            if (uIsSeparator) {
                if (delta < -1)
                    return 1;
                if (delta == -1)
                    return 0;
                return -1;
            }
            if (delta > 1)
                return 1;
            if (delta == 1)
                return 0;
            return -1;
        }

        bool NeighborsForbidTheSwap(int u, int v) {
            return UpperNeighborsForbidTheSwap(u, v) || LowerNeighborsForbidTheSwap(u, v);
        }

        bool LowerNeighborsForbidTheSwap(int u, int v) {
            int uCount, vCount;
            if (((uCount = properLayeredGraph.OutEdgesCount(u)) == 0) ||
                ((vCount = properLayeredGraph.OutEdgesCount(v)) == 0))
                return false;

            return X[successors[u][uCount >> 1]] < X[successors[v][vCount >> 1]];
        }


        bool UpperNeighborsForbidTheSwap(int u, int v) {
            int uCount = properLayeredGraph.InEdgesCount(u);
            int vCount = properLayeredGraph.InEdgesCount(v);
            if (uCount == 0 || vCount == 0)
                return false;

            return X[predecessors[u][uCount >> 1]] < X[predecessors[v][vCount >> 1]];
        }

        int CalcDeltaBetweenGroupsToTheLeftAndToTheRightOfTheSeparator(int[] layer, int separatorPosition, int separator) {
            Func<int, bool> kind = GetKindDelegate(separator);
            int leftGroupSize = 0;
            for (int i = separatorPosition - 1; i >= 0 && !kind(layer[i]); i--)
                leftGroupSize++;
            int rightGroupSize = 0;
            for (int i = separatorPosition + 1; i < layer.Length && !kind(layer[i]); i++)
                rightGroupSize++;

            return leftGroupSize - rightGroupSize;
        }

        bool IsOriginal(int v) {
            return v < startOfVirtNodes;
        }

        bool IsVirtual(int v) {
            return v >= startOfVirtNodes;
        }


        Func<int, bool> GetKindDelegate(int v) {
            Func<int, bool> kind = IsVirtual(v) ? IsVirtual : new Func<int, bool>(IsOriginal);
            return kind;
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
    }
}