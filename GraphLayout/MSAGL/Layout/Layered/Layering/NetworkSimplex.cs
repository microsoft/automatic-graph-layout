using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.GraphAlgorithms;

namespace Microsoft.Msagl.Layout.Layered {
    
    /// <summary>
    /// The algorithm follows "A technique for Drawing Directed Graphs", Gansner, Koutsofios, North, Vo.
    /// Consider re-implement this algorithm following Chvatal. The algorithm works for a connected graph.
    /// </summary>
    internal class NetworkSimplex : AlgorithmBase, LayerCalculator {

        static BasicGraphOnEdges<PolyIntEdge> CreateGraphWithIEEdges(BasicGraphOnEdges<PolyIntEdge> bg) {
            List<PolyIntEdge> ieEdges = new List<PolyIntEdge>();

            foreach (PolyIntEdge e in bg.Edges)
                ieEdges.Add(new NetworkEdge(e));

            return new BasicGraphOnEdges<PolyIntEdge>(ieEdges, bg.NodeCount);
        }

        int[] layers;


        internal NetworkSimplex(BasicGraphOnEdges<PolyIntEdge> graph, CancelToken cancelToken)
        {
            this.graph = CreateGraphWithIEEdges(graph);
            inTree = new bool[graph.NodeCount];
            NetworkCancelToken = cancelToken;
        }

        public int[] GetLayers() {
            if (layers == null)
                Run(NetworkCancelToken);

            return layers;
        }

        private void ShiftLayerToZero() {
            int minLayer = NetworkEdge.Infinity;
            foreach (int i in layers)
                if (i < minLayer)
                    minLayer = i;


            for (int i = 0; i < graph.NodeCount; i++)
                layers[i] -= minLayer;

        }



        /// <summary>
        /// The function FeasibleTree constructs an initial feasible spanning tree.
        /// </summary>
        void FeasibleTree() {
            InitLayers();

            while (TightTree() < this.graph.NodeCount) {

                PolyIntEdge e = GetNonTreeEdgeIncidentToTheTreeWithMinimalAmountOfSlack();
                if (e == null)
                    break; //all edges are tree edges
                int slack = Slack(e);
                if (slack == 0)
                    throw new InvalidOperationException();//"the tree should be tight");

                if (inTree[e.Source])
                    slack = -slack;

                //shift the tree rigidly up or down and make e tight ; since the slack is the minimum of slacks
                //the layering will still remain feasible
                foreach (int i in treeVertices)
                    layers[i] += slack;

            }

            InitCutValues();
        }

        /// <summary>
        /// treeEdge, belonging to the tree, divides the vertices to source and target components
        /// If v belongs to the source component we return 1 oterwise we return 0
        /// </summary>
        /// <param name="v">a v</param>
        /// <param name="treeEdge">a vertex</param>
        /// <returns>an edge from the tree</returns>
        int VertexSourceTargetVal(int v, NetworkEdge treeEdge) {
#if DEBUGNW
      if (treeEdge.inTree == false)
        throw new Exception("wrong params for VertexSourceTargetVal");
#endif

            int s = treeEdge.Source;
            int t = treeEdge.Target;
            if (lim[s] > lim[t])//s belongs to the tree root component
                if (lim[v] <= lim[t] && low[t] <= lim[v])
                    return 0;
                else
                    return 1;
            else //t belongs to the tree root component
                if (lim[v] <= lim[s] && low[s] <= lim[v])
                    return 1;
                else
                    return 0;

        }

        /// <summary>
        /// a convenient wrapper of IncEdges
        /// </summary>
        /// <param name="v"></param>
        /// <returns>edges incident to v</returns>
        IncEdges IncidentEdges(int v) {
            return new IncEdges(v, this);
        }

        bool AllLowCutsHaveBeenDone(int v) {
            foreach (NetworkEdge ie in IncidentEdges(v))
                if (ie.inTree && ie.Cut == NetworkEdge.Infinity && ie != parent[v])
                    return false;
            return true;
        }

        /// <summary>
        /// treeEdge, belonging to the tree, divides the vertices to source and target components
        /// e does not belong to the tree . If e goes from the source component to target component 
        /// then the return value is 1,
        /// if e goes from the target component ot the source then the return value is -1
        /// otherwise it is zero
        /// </summary>
        /// <param name="e">a non-tree edge</param>
        /// <param name="treeEdge">a tree edge</param>
        /// <returns></returns>
        int EdgeSourceTargetVal(NetworkEdge e, NetworkEdge treeEdge) {

            // if (e.inTree || treeEdge.inTree == false)
            // throw new Exception("wrong params for EdgeSOurceTargetVal");

            return VertexSourceTargetVal(e.Source, treeEdge) - VertexSourceTargetVal(e.Target, treeEdge);
        }

        /// <summary>
        /// The init_cutvalues function computes the cut values of the tree edges.
        /// For each tree edge, this is computed by marking the nodes as belonging to the source or 
        /// target component, and then performing the sum of the signed weights of all 
        /// edges whose source and target are in different components, the sign being negative for those edges 
        /// going from the source to the target component.
        /// To reduce this cost, we note that the cut values can be computed using information local to an edge 
        /// if the search is ordered from the leaves of the feasible tree inward. It is trivial to compute the 
        /// cut value of a tree edge with one of its endpoints a leaf in the tree, 
        /// since either the source or the target component consists of a single node. 
        /// Now, assuming the cut values are known for all the edges incident on a given 
        /// node except one, the cut value of the remaining edge is the sum of the known cut 
        /// values plus a term dependent only on the edges incident to the given node.
        /// </summary>
        void InitCutValues() {
            InitLimLowAndParent();

            //going up from the leaves following parents
            Stack<int> front = new Stack<int>();
            foreach (int i in leaves)
                front.Push(i);
            Stack<int> newFront = new Stack<int>();
            while (front.Count > 0) {
                while (front.Count > 0) {
                    int w = front.Pop();
                    NetworkEdge cutEdge = parent[w]; //have to find the cut of e
                    if (cutEdge == null)
                        continue;
                    int cut = 0;
                    foreach (NetworkEdge e in IncidentEdges(w)) {

                        if (e.inTree == false) {
                            int e0Val = EdgeSourceTargetVal(e, cutEdge);
                            if (e0Val != 0)
                                cut += e0Val * e.Weight;
                        } else //e0 is a tree edge
            {
                            if (e == cutEdge)
                                cut += e.Weight;
                            else {
                                int impact = cutEdge.Source == e.Target || cutEdge.Target == e.Source ? 1 : -1;
                                int edgeContribution = EdgeContribution(e, w);
                                cut += edgeContribution * impact;

                            }
                        }
                    }

                    cutEdge.Cut = cut;
                    int v = cutEdge.Source == w ? cutEdge.Target : cutEdge.Source;
                    if (AllLowCutsHaveBeenDone(v))
                        newFront.Push(v);



                }
                //swap newFrontAndFront
                Stack<int> t = front;
                front = newFront;
                newFront = t;
            }
        }

        /// <summary>
        /// e is a tree edge for which the cut has been calculted already.
        /// EdgeContribution gives an amount that edge e brings to the cut of parent[w].
        /// The contribution is the cut value minus the weight of e. Let S be the component of e source. 
        /// We should also substruct W(ie) for every ie going from S to w and add W(ie) going from w to S.
        /// These numbers appear in e.Cut but with opposite signs.
        /// </summary>
        /// <param name="e">tree edge</param>
        /// <param name="w">parent[w] is in the process of the cut calculation</param>
        /// <returns></returns>
        int EdgeContribution(NetworkEdge e, int w) {
            int ret = e.Cut - e.Weight;
            foreach (NetworkEdge ie in IncidentEdges(w)) {
                if (ie.inTree == false) {
                    int sign = EdgeSourceTargetVal(ie, e);
                    if (sign == -1)
                        ret += ie.Weight;
                    else if (sign == 1)
                        ret -= ie.Weight;
                }
            }
            return ret;
        }

        int[] lim;
        int[] low;
        NetworkEdge[] parent;

        internal struct StackStruct {
            internal int v;
            internal IEnumerator outEnum;
            internal IEnumerator inEnum;

            internal StackStruct(int v,
            IEnumerator outEnum,
            IEnumerator inEnum) {
                this.v = v;
                this.outEnum = outEnum;
                this.inEnum = inEnum;
            }

        }

        List<int> leaves = new List<int>();

        /// <summary> 
        /// A quote:
        /// Another valuable optimization, similar to a technique described in [Ch],
        /// is to perform a postorder traversal of the tree, starting from some fixed 
        /// root node vroot, and labeling each node v with its postorder 
        /// traversal number lim(v), the least number low(v) of any descendant in the search, 
        /// and the edge parent(v) by which the node was reached (see figure 2-5).
        /// This provides an inexpensive way to test whether a node lies in the 
        /// source or target component of a tree edge, and thus whether a non-tree edge 
        /// crosses between the two components. For example, if e = (u,v) is a 
        /// tree edge and vroot is in the source component of the edge (i.e., lim(u) less lim(v)), 
        /// then a node w is in the target component of e if and only if low(u) is less or equal than lim(w) 
        /// is less or equal than lim(u). These numbers can also be used to update the tree efficiently 
        /// during the network simplex iterations. If f = (w,x) is the entering edge, the 
        /// only edges whose cut values must be adjusted are those in the path 
        /// connecting w and x in the tree. This path is determined by following 
        /// the parent edges back from w and x until the least common ancestor is reached, 
        /// i.e., the first node l such that low(l) is less or equal lim(w) than ,
        /// lim(x) is less or equal than lim(l). 
        /// Of course, these postorder parameters must also be adjusted when 
        /// exchanging tree edges, but only for nodes below l.
        /// </summary>
        void InitLimLowAndParent() {
            lim = new int[graph.NodeCount];
            low = new int[graph.NodeCount];
            parent = new NetworkEdge[graph.NodeCount];

            InitLowLimParentAndLeavesOnSubtree(1, 0);
        }
        /// <summary>
        /// initializes lim and low in the subtree 
        /// </summary>
        /// <param name="curLim">the root of the subtree</param>
        /// <param name="v">the low[v]</param>
        private void InitLowLimParentAndLeavesOnSubtree(int curLim, int v) {
            Stack<StackStruct> stack = new Stack<StackStruct>();
            IEnumerator outEnum = this.graph.OutEdges(v).GetEnumerator();
            IEnumerator inEnum = this.graph.InEdges(v).GetEnumerator();

            stack.Push(new StackStruct(v, outEnum, inEnum));//vroot is 0 here
            low[v] = curLim;

            while (stack.Count > 0) {
                StackStruct ss = stack.Pop();
                v = ss.v;
                outEnum = ss.outEnum;
                inEnum = ss.inEnum;

                //for sure we will have a descendant with the lowest number curLim since curLim may only grow 
                //from the current value

                ProgressStep();
                bool done;
                do {
                    done = true;
                    while (outEnum.MoveNext()) {
                        NetworkEdge e = outEnum.Current as NetworkEdge;
                        if (!e.inTree || low[e.Target] > 0)
                            continue;
                        stack.Push(new StackStruct(v, outEnum, inEnum));
                        v = e.Target;
                        parent[v] = e;
                        low[v] = curLim;
                        outEnum = this.graph.OutEdges(v).GetEnumerator();
                        inEnum = this.graph.InEdges(v).GetEnumerator();
                    }
                    while (inEnum.MoveNext()) {
                        NetworkEdge e = inEnum.Current as NetworkEdge;
                        if (!e.inTree || low[e.Source] > 0) {
                            continue;
                        }
                        stack.Push(new StackStruct(v, outEnum, inEnum));
                        v = e.Source;
                        low[v] = curLim;
                        parent[v] = e;
                        outEnum = this.graph.OutEdges(v).GetEnumerator();
                        inEnum = this.graph.InEdges(v).GetEnumerator();
                        done = false;
                        break;
                    }
                } while (!done);

                //finally done with v
                lim[v] = curLim++;
                if (lim[v] == low[v])
                    leaves.Add(v);
            }
        }



        /// <summary>
        /// here we update values lim and low for the subtree with the root l
        /// </summary>
        /// <param name="l"></param>
        void UpdateLimLowLeavesAndParentsUnderNode(int l) {

            //first we zero all low values in the subtree since they are an indication when positive that 
            //the node has been processed
            //We are updating leaves also
            int llow = low[l];
            int llim = lim[l];

            leaves.Clear();



            for (int i = 0; i < this.graph.NodeCount; i++) {
                if (llow <= lim[i] && lim[i] <= llim)
                    low[i] = 0;
                else if (low[i] == lim[i])
                    leaves.Add(i);

            }

            InitLowLimParentAndLeavesOnSubtree(llow, l);

        }

        int Slack(PolyIntEdge e) {
            int ret = layers[e.Source] - layers[e.Target] - e.Separation;
#if DEBUGNW
      if (ret < 0)
        throw new Exception("separation is not satisfied");
#endif
            return ret;
        }

        /// <summary>
        /// one of the returned edge vertices does not belong to the tree but another does
        /// </summary>
        /// <returns></returns>
        NetworkEdge GetNonTreeEdgeIncidentToTheTreeWithMinimalAmountOfSlack() {
            PolyIntEdge eret = null;
            int minSlack = NetworkEdge.Infinity;

            foreach (int v in this.treeVertices) {
                foreach (NetworkEdge e in this.graph.OutEdges(v)) {
                    if (inTree[e.Source] && inTree[e.Target])
                        continue;
                    int slack = Slack(e);
                    if (slack < minSlack) {
                        eret = e;
                        minSlack = slack;
                        if (slack == 1)
                            return e;

                    }
                }

                foreach (NetworkEdge e in this.graph.InEdges(v)) {
                    if (inTree[e.Source] && inTree[e.Target])
                        continue;


                    int slack = Slack(e);
                    if (slack < minSlack) {
                        eret = e;
                        minSlack = slack;
                        if (slack == 1)
                            return e;

                    }
                }

            }

            return eret as NetworkEdge;

        }

        List<int> treeVertices = new List<int>();
        bool[] inTree;


        /// <summary>
        /// The function TightTree finds a maximal tree of tight edges containing 
        /// some fixed node and returns the number of nodes in the tree. 
        /// Note that such a maximal tree is just a spanning tree for the subgraph 
        /// induced by all nodes reachable from the fixed node in the underlying 
        /// undirected graph using only tight edges. In particular, all such trees have the same number of nodes.
        /// The function also builds the tree.
        /// </summary>
        /// <returns>number of verices in a tight tree</returns>
        int TightTree() {
            treeVertices.Clear();
            foreach (NetworkEdge ie in this.graph.Edges)
                ie.inTree = false;

            for (int i = 1; i < inTree.Length; i++)
                inTree[i] = false;


            //the vertex 0 is a fixed node
            inTree[0] = true;
            treeVertices.Add(0);
            Stack<int> q = new Stack<int>();
            q.Push(0);
            while (q.Count > 0) {
                int v = q.Pop();


                foreach (NetworkEdge e in graph.OutEdges(v)) {
                    if (inTree[e.Target])
                        continue;

                    if (layers[e.Source] - layers[e.Target] == e.Separation) {
                        q.Push(e.Target);
                        inTree[e.Target] = true;
                        treeVertices.Add(e.Target);
                        e.inTree = true;

                    }
                }
                foreach (NetworkEdge e in graph.InEdges(v)) {
                    if (inTree[e.Source])
                        continue;

                    if (layers[e.Source] - layers[e.Target] == e.Separation) {
                        q.Push(e.Source);
                        inTree[e.Source] = true;
                        treeVertices.Add(e.Source);
                        e.inTree = true;
                    }
                }
            }
            return treeVertices.Count;
        }

        Random random = new Random(1);

        ///// <summary>
        ///// LeaveEnterEdge finds a non-tree edge to replace e. 
        ///// This is done by breaking the edge e, which divides 
        ///// the tree into the source and the target componentx. 
        ///// All edges going from the source component to the
        ///// target are considered, with an edge of minimum 
        ///// slack being chosen. This is necessary to maintain feasibility.
        ///// </summary>
        ///// <param name="leavingEdge">a leaving edge</param>
        ///// <param name="enteringEdge">an entering edge</param>
        ///// <returns>returns true if a pair is chosen</returns>
        Tuple<NetworkEdge, NetworkEdge> LeaveEnterEdge() {
            NetworkEdge leavingEdge = null;
            NetworkEdge enteringEdge = null; //to keep the compiler happy
            int minCut = 0;
            foreach (NetworkEdge e in graph.Edges) {
                if (e.inTree) {
                    if (e.Cut < minCut) {
                        minCut = e.Cut;
                        leavingEdge = e;
                    }
                }
            }

            if (leavingEdge == null)
                return null;

            //now we are looking for a non-tree edge with a minimal slack belonging to TS
            bool continuation = false;
            int minSlack = NetworkEdge.Infinity;
            foreach (NetworkEdge f in graph.Edges) {
                int slack = Slack(f);
                if (f.inTree == false && EdgeSourceTargetVal(f, leavingEdge) == -1 &&
                  (slack < minSlack || (slack == minSlack && (continuation = (random.Next(2) == 1))))
                  ) {
                    minSlack = slack;
                    enteringEdge = f;
                    if (minSlack == 0 && !continuation)
                        break;
                    continuation = false;
                }
            }

#if TEST_MSAGL
      if (enteringEdge == null)
      {
        throw new InvalidOperationException();
      }
#endif
            return new Tuple<NetworkEdge, NetworkEdge>(leavingEdge, enteringEdge);



        }
        /// <summary>
        /// If f = (w,x) is the entering edge, the 
        /// only edges whose cut values must be adjusted are those in the path 
        /// connecting w and x in the tree, excluding e. This path is determined by 
        /// following the parent edges back from w and x until the least common ancestor is 
        /// reached, i.e., the first node l such that low(l) less or equal lim(w) ,lim(x) less or equal lim(l). 
        /// Of course, these postorder parameters must also be adjusted when 
        /// exchanging tree edges, but only for nodes below l.
        /// </summary>
        /// <param name="e">exiting edge</param>
        /// <param name="f">entering edge</param>
        void Exchange(NetworkEdge e, NetworkEdge f) {
            int l = CommonPredecessorOfSourceAndTargetOfF(f);

            CreatePathForCutUpdates(e, f, l);
            UpdateLimLowLeavesAndParentsUnderNode(l);

            UpdateCuts(e);

            UpdateLayersUnderNode(l);



        }

        private void UpdateLayersUnderNode(int l) {

            //update the layers under l
            Stack<int> front = new Stack<int>();
            front.Push(l);

            //set layers to infinity under l
            for (int i = 0; i < this.graph.NodeCount; i++)
                if (low[l] <= lim[i] && lim[i] <= lim[l] && i != l)
                    layers[i] = NetworkEdge.Infinity;

            while (front.Count > 0) {
                int u = front.Pop();
                foreach (NetworkEdge oe in this.graph.OutEdges(u)) {
                    if (oe.inTree && layers[oe.Target] == NetworkEdge.Infinity) {
                        layers[oe.Target] = layers[u] - oe.Separation;
                        front.Push(oe.Target);
                    }
                }
                foreach (NetworkEdge ie in this.graph.InEdges(u)) {
                    if (ie.inTree && layers[ie.Source] == NetworkEdge.Infinity) {
                        layers[ie.Source] = layers[u] + ie.Separation;
                        front.Push(ie.Source);
                    }
                }
            }
        }

        private void UpdateCuts(NetworkEdge e) {
            //going up from the leaves of the branch following parents
            Stack<int> front = new Stack<int>();
            Stack<int> newFront = new Stack<int>();


            //We start cut updates from the vertices of e. It will work only if in the new tree
            // the  parents of the vertices of e are end edges on the path connecting the two vertices.
            //Let  e be (w,x) and let f be (u,v). Let T be the tree containing e but no f,
            //and T0 be the tree without with e but containg f. Let us consider the path with no edge repetitions from u to v in T.
            //It has to contain e since there is a path from u to v in T containing e, because v lies in the component of w in T
            //and u lies in the component of x in T, if there is a path without e then we have a cycle in T.
            // Now if we romove e from this path and add f to it we get a path without edge repetitions connecting w to x.
            // The edge adjacent in this path to w is parent[w] in T0, and the edge of the path adjacent to x is
            //parent[x] in T0. If it is not true then we can get a cycle by constructing another path from w to x going up through the
            //parents to the common ancessor of w and x.

            front.Push(e.Source);
            front.Push(e.Target);

            while (front.Count > 0) {
                while (front.Count > 0) {
                    int w = front.Pop();

                    ProgressStep();
                    NetworkEdge cutEdge = parent[w]; //have to find the cut of cutEdge

                    if (cutEdge == null)
                        continue;

                    if (cutEdge.Cut != NetworkEdge.Infinity)
                        continue; //the value of this cut has not been changed
                    int cut = 0;
                    foreach (NetworkEdge ce in IncidentEdges(w)) {
                        if (ce.inTree == false) {
                            cut += EdgeSourceTargetVal(ce, cutEdge) * ce.Weight;
                        }
                        else { //e0 is a tree edge
                            if (ce == cutEdge) {
                                cut += ce.Weight;
                            }
                            else {
                                int impact = cutEdge.Source == ce.Target || cutEdge.Target == ce.Source ? 1 : -1;
                                int edgeContribution = EdgeContribution(ce, w);
                                cut += edgeContribution * impact;
                            }
                        }
                    }

                    cutEdge.Cut = cut;
                    int u = cutEdge.Source == w ? cutEdge.Target : cutEdge.Source;
                    if (AllLowCutsHaveBeenDone(u))
                        newFront.Push(u);

                }
                //swap newFrontAndFront
                Stack<int> t = front;
                front = newFront;
                newFront = t;
            }
        }

        private void CreatePathForCutUpdates(NetworkEdge e, NetworkEdge f, int l) {
            //we mark the path by setting the cut value to infinity

            int v = f.Target;
            while (v != l) {
                NetworkEdge p = parent[v];
                p.Cut = NetworkEdge.Infinity;
                v = p.Source == v ? p.Target : p.Source;
            }

            f.Cut = NetworkEdge.Infinity; //have to do it because f will be in the path between end points of e in the new tree

            //remove e from the tree and put f inside of it
            e.inTree = false; f.inTree = true;

        }

        private int CommonPredecessorOfSourceAndTargetOfF(NetworkEdge f) {
            //find the common predecessor of f.Source and f.Target 
            int fMin, fmax;
            if (lim[f.Source] < lim[f.Target]) {
                fMin = lim[f.Source];
                fmax = lim[f.Target];
            } else {
                fMin = lim[f.Target];
                fmax = lim[f.Source];
            }
            //it is the best to walk up from the highest of nodes f 
            //but we don't know the depths
            //so just start walking up from the source
            int l = f.Source;


            while ((low[l] <= fMin && fmax <= lim[l]) == false) {
                NetworkEdge p = parent[l];

                p.Cut = NetworkEdge.Infinity;

                l = p.Source == l ? p.Target : p.Source;
            }
            return l;
        }
#if TEST_MSAGL
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.WriteLine(System.String,System.Object,System.Object,System.Object)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void CheckCutValues() {
            foreach (NetworkEdge e in this.graph.Edges) {
                if (e.inTree) {
                    int cut = 0;
                    foreach (NetworkEdge f in graph.Edges) {


                        cut += EdgeSourceTargetVal(f, e) * f.Weight;

                    }
                    if (e.Cut != cut)
                        System.Diagnostics.Debug.WriteLine("cuts are wrong for {0}; should be {1} but is {2}", e, cut, e.Cut);
                }


            }


        }
#endif

        void InitLayers() {
            LongestPathLayering lp = new LongestPathLayering(this.graph);
            this.layers = lp.GetLayers();
        }


        #region Enumerators
        /// <summary>
        /// to enumerate over all edges incident to v
        /// </summary>
        internal class IncEdges : IEnumerable<NetworkEdge> {
            int v;
            NetworkSimplex nw;

#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=203
            //SharpKit/Colin - also https://code.google.com/p/sharpkit/issues/detail?id=332
            public IEnumerator<NetworkEdge> GetEnumerator()
            {
#else
            IEnumerator<NetworkEdge> IEnumerable<NetworkEdge>.GetEnumerator() {
#endif
                return new IncEdgeEnumerator(nw.graph.OutEdges(v).GetEnumerator(), nw.graph.InEdges(v).GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return new IncEdgeEnumerator(nw.graph.OutEdges(v).GetEnumerator(), nw.graph.InEdges(v).GetEnumerator());
            }


            internal IncEdges(int v, NetworkSimplex nw) {
                this.v = v;
                this.nw = nw;
            }
        }

        internal class IncEdgeEnumerator : IEnumerator<NetworkEdge> {
            IEnumerator outEdges;
            IEnumerator inEdges;

            bool outIsActive;
            bool inIsActive;


            public void Dispose() { GC.SuppressFinalize(this); }

            internal IncEdgeEnumerator(IEnumerator outEdges, IEnumerator inEdges) {
                this.outEdges = outEdges;
                this.inEdges = inEdges;
            }

            void IEnumerator.Reset() {
                outEdges.Reset();
                inEdges.Reset();
            }

            public bool MoveNext() {
                outIsActive = outEdges.MoveNext();
                if (!outIsActive)
                    inIsActive = inEdges.MoveNext();

                return outIsActive || inIsActive;
            }

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=203
            //SharpKit/Colin - https://code.google.com/p/sharpkit/issues/detail?id=332
            public NetworkEdge Current
            {
#else
            NetworkEdge IEnumerator<NetworkEdge>.Current {
#endif
                get {
                    if (outIsActive)
                        return outEdges.Current as NetworkEdge;
                    if (inIsActive)
                        return inEdges.Current as NetworkEdge;

                    throw new InvalidOperationException();
                }
            }

            object IEnumerator.Current {
                get {
                    if (outIsActive)
                        return outEdges.Current as NetworkEdge;
                    if (inIsActive)
                        return inEdges.Current as NetworkEdge;

                    throw new InvalidOperationException();//"bug in the IncEdge enumerator");
                }
            }
        }
        #endregion

  
        BasicGraphOnEdges<PolyIntEdge> graph;
        private CancelToken NetworkCancelToken;

        public int Weight { get {
                return this.graph.Edges.Select(e => e.Weight*(this.layers[e.Source]-this.layers[e.Target])).Sum();
            }
        }

        protected override void RunInternal() {
            if (graph.Edges.Count == 0 && graph.NodeCount == 0)
                layers = new int[0];

            FeasibleTree();

            Tuple<NetworkEdge, NetworkEdge> leaveEnter;
            while ((leaveEnter = LeaveEnterEdge()) != null) {
                ProgressStep();
                Exchange(leaveEnter.Item1, leaveEnter.Item2);
            }

            ShiftLayerToZero();
        }
    }
}
