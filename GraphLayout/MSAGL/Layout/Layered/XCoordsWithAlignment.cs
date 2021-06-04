using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.GraphAlgorithms;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// The implementation follows "Fast and Simple Horizontal Coordinate Assignment" of Ulrik Brandes and Boris K¨opf
    /// The paper has two serious bugs that this code resolves.
    /// </summary>
    internal partial class XCoordsWithAlignment {
        LayerArrays la;
        ProperLayeredGraph graph;
        int nOfOriginalVertices;
        int[] root;
        int[] align;
        //int[] sink;
        //double[] shift;
        int nOfVertices;
        Anchor[] anchors;
        double nodeSep;
        object[] lowMedians;
        object[] upperMedians; //each element or int or IntPair
        Set<IntPair> markedEdges = new Set<IntPair>();
        int h;//number of layers

        //We pretend, when calculating the alignment, that we traverse layers from left to right and from bottom to top.
        //The actual directions are defined by variables "LR" and "BT". 

        /// <summary>
        /// from left to right
        /// </summary>
        bool LR;
        /// <summary>
        /// from bottom to top
        /// </summary>
        bool BT;

        int EnumRightUp { get { return (LR ? 0 : 1) + 2 * (BT ? 0 : 1); } }


        /// <summary>
        /// Returns true if v is a virtual vertex
        /// </summary>
        /// <param name="v"></param>GG
        /// <returns></returns>
        bool IsVirtual(int v) { return v >= nOfOriginalVertices; }


        //four arrays for four different direction combinations
        double[][] xCoords = new double[4][];
        double[] x;

        int Source(LayerEdge edge) { return BT ? edge.Source : edge.Target; }

        int Target(LayerEdge edge) { return BT ? edge.Target : edge.Source; }

        /// <summary>
        /// 
        /// </summary>
        static internal void CalculateXCoordinates(LayerArrays layerArrays, ProperLayeredGraph layeredGraph, int nOfOriginalVs, Anchor[] anchors, double nodeSeparation) {
            XCoordsWithAlignment x = new XCoordsWithAlignment(layerArrays, layeredGraph, nOfOriginalVs, anchors, nodeSeparation);
            x.Calculate();
        }

        void Calculate() {
            SortInAndOutEdges();

            RightUpSetup();
            CalcBiasedAlignment();

            LeftUpSetup();
            CalcBiasedAlignment();

            RightDownSetup();
            CalcBiasedAlignment();

            LeftDownSetup();
            CalcBiasedAlignment();
            HorizontalBalancing();

        }
        //We need to find a median of a vertex neighbors from a specific layer. That is, if we have a vertex v and edges (v,coeff), (v,side1), (v,cornerC) 
        // going down, and X[coeff]<X[side1]<X[cornerC], then side1 is the median.
        //There is an algorithm that finds the median with expected linear number of steps,
        //see for example http://www.ics.uci.edu/~eppstein/161/960125.html. However, I think we are better off 
        //with sorting, since we are taking median at least twice. 
        //Notice, that the sorting should be done only for original vertices since dummy vertices 
        //have only one incoming edge and one outcoming edge.
        //Consider here reusing the sorting that comes from the ordering step,
        //if it is not broken by layer insertions.
        void SortInAndOutEdges() {
            FillLowMedians();
            FillUpperMedins();
            //Microsoft.Msagl.Ordering.EdgeComparerBySource edgeComparerBySource = new Ordering.EdgeComparerBySource(this.la.X);
            //Microsoft.Msagl.Ordering.EdgeComparerByTarget edgeComparerByTarget = new Ordering.EdgeComparerByTarget(this.la.X);
            //for (int i = 0; i < this.nOfOriginalVertices; i++) {
            //    Array.Sort<LayerEdge>(this.graph.InEdges(i) as LayerEdge[], edgeComparerBySource);
            //    Array.Sort<LayerEdge>(this.graph.OutEdges(i) as LayerEdge[], edgeComparerByTarget);
            //}
        }

        private void FillUpperMedins() {
            upperMedians = new object[graph.NodeCount];
            for (int i = 0; i < graph.NodeCount; i++)
                FillUpperMediansForNode(i);
        }

        int CompareByX(int a, int b) { return this.la.X[a] - this.la.X[b]; }

        private void FillUpperMediansForNode(int i) {
            int count = this.graph.InEdgesCount(i);
            if (count > 0) {
                int[] predecessors = new int[count];
                count = 0;
                foreach (LayerEdge e in this.graph.InEdges(i))
                    predecessors[count++] = e.Source;

                Array.Sort(predecessors, new System.Comparison<int>(CompareByX));
                int m = count / 2;
                if (m * 2 == count) {
                    this.upperMedians[i] = new IntPair(predecessors[m - 1], predecessors[m]);
                } else
                    this.upperMedians[i] = predecessors[m];
            } else
                this.upperMedians[i] = -1;
          
        }

        private void FillLowMedians() {
            lowMedians = new object[graph.NodeCount];
            for (int i = 0; i < graph.NodeCount; i++)
                FillLowMediansForNode(i);
        }

        private void FillLowMediansForNode(int i) {
            int count = this.graph.OutEdgesCount(i);
            if (count > 0) {
                int[] successors = new int[count];
                count = 0;
                foreach (LayerEdge e in this.graph.OutEdges(i))
                    successors[count++] = e.Target;

                Array.Sort(successors, new System.Comparison<int>(CompareByX));
                int m = count / 2;
                if (m * 2 == count) {
                    this.lowMedians[i] = new IntPair(successors[m - 1], successors[m]);
                } else
                    this.lowMedians[i] = successors[m];
            } else
                this.lowMedians[i] = -1;
        }


        void HorizontalBalancing() {

            int leastWidthAssignment = -1;
            double[] a = new double[4];
            double[] b = new double[4];

            double leastWidth = Double.MaxValue;
            for (int i = 0; i < 4; i++) {
                AssignmentBounds(i, out a[i], out b[i]);
                double w = b[i] - a[i];
                if (w < leastWidth) {
                    leastWidthAssignment = i;
                    leastWidth = w;
                }
            }

            for (int i = 0; i < 4; i++) {
                double delta;
                if (IsLeftMostAssignment(i))
                    //need to align left ends according to the paper
                    delta = a[leastWidthAssignment] - a[i];
                else
                    delta = b[leastWidthAssignment] - b[i];
                x = xCoords[i];
                if (delta != 0)
                    for (int j = 0; j < nOfVertices; j++)
                        x[j] += delta;

            }



            double[] arr = new double[4];
            for (int v = 0; v < nOfVertices; v++) {
                arr[0] = xCoords[0][v];
                arr[1] = xCoords[1][v];
                arr[2] = xCoords[2][v];
                arr[3] = xCoords[3][v];
                Array.Sort(arr);
                anchors[v].X = (arr[1] + arr[2]) / 2;
            }

            //    Layout.ShowDataBase(dataBase);

        }

        static bool IsLeftMostAssignment(int i) {
            return i == 0 || i == 2;
        }

        void AssignmentBounds(int i, out double a, out double b) {
            if (nOfVertices == 0) {
                a = 0;
                b = 0;
            } else {
                x = xCoords[i];
                a = b = x[0];
                for (int j = 1; j < nOfVertices; j++) {
                    double r = x[j];
                    if (r < a)
                        a = r;
                    else if (r > b)
                        b = r;
                }
            }
        }

        void CalcBiasedAlignment() {
            ConflictElimination();
            Align();
#if TEST_MSAGL
            //for (int i = 0; i < nOfVertices; i++)
            //    anchors[i].X = x[i];
            //Layout.ShowDataBase(dataBase);
#endif
        }

        void LeftUpSetup() {
            LR = false;
            BT = true;
        }
        void LeftDownSetup() {
            LR = false;
            BT = false;
        }

        void RightDownSetup() {
            LR = true;
            BT = false;
        }

        void RightUpSetup() {
            LR = true;
            BT = true;
        }


        /// <summary>
        /// The code is written as if we go left up, but in fact the settings define the directions.
        /// 
        /// We need to create a subgraph for alignment:
        /// where no edge segments intersect, and every vertex has
        /// at most one incoming and at most one outcoming edge.
        /// This function marks edges to resolve conflicts with only one inner segment.  
        /// An inner segment is a segment between two dummy nodes.
        /// We mark edges that later will not participate in the alignment. 
        /// Inner segments are preferred to other ones. So, in a conflict with one inner and one
        /// non-inner edges we leave the inner edge to participate in the alignment. 
        /// At the moment we mark as not participating both of the two intersecting inner segments
        /// </summary>
        void ConflictElimination() {
            RemoveMarksFromEdges();
            MarkConflictingEdges();
        }

        /*
         * Type 0 conflicts are those where inner edges do not participate. 
         * They are resolved not by marking but just when we calculate the alignment in CreateBlocks.
         * A quote from "Fast and ..." with some typo corrections:
         * Type 0 conflicts are resolved greedily in a leftmost fashion, 
         * i.e. in every layer we process the vertices from left to right and 
         * for each vertex we consider its median upper neighbor (its left and right 
         * median upper neighbor, in this order, if there are two). 
         * The pair is aligned, if no conflicting alignment is to the left of this one.
         * The resulting bias is mediated by the fact that the symmetric bias is applied 
         * in one of the other three assignments.
         */




        IEnumerable<int> UpperEdgeMedians(int target) {
            object medians = this.BT ? upperMedians[target] : lowMedians[target];
            if (medians is IntPair) {
                IntPair ip = medians as IntPair;
                if (this.LR) {
                    yield return ip.First; yield return ip.Second;
                } else {
                    yield return ip.Second; yield return ip.First;
                }
            } else {
                int i = (int)medians;
                if (i >= 0)
                    yield return i;
            }
        }



        /// <summary>
        /// here we eliminate all constraints 
        /// </summary>
        void MarkConflictingEdges() {
            int i = LowerOf(0, h - 1);
            int lowest = i;
            int upperBound = UpperOf(0, h - 1);
            int nextBelowUpperBound = NextLower(upperBound);

            //our top layer has index h-1, our bottom layer has index 0
            //inner segments can appear only between layers with indices i+1 and i where i>0 and i<h-1
            for (; IsBelow(i, upperBound); i = NextUpper(i)) {
                if (IsBelow(lowest, i) && IsBelow(i, nextBelowUpperBound))
                    ConflictsWithAtLeastOneInnerEdgeForALayer(i);
            }
        }

       


        /// <summary>
        /// parameterized next upper 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        int NextUpper(int i) { return BT ? i + 1 : i - 1; }

        /// <summary>
        /// parameterized next lower
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        int NextLower(int i) { return BT ? i - 1 : i + 1; }

        /// <summary>
        /// parameterize highest of two numbers
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        int UpperOf(int i, int j) { return BT ? Math.Max(i, j) : Math.Min(i, j); }
        /// <summary>
        /// parameterized lowest of a pair
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        int LowerOf(int i, int j) { return BT ? Math.Min(i, j) : Math.Max(i, j); }


        /// <summary>
        /// returns parameterized below
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        bool IsBelow(int i, int j) { return BT ? i < j : j < i; }
        /// <summary>
        /// returns the "parameterized" left of the two positions
        /// </summary>
        /// <param name="pos0"></param>
        /// <param name="pos1"></param>
        /// <returns></returns>
        int LeftMost(int pos0, int pos1) { return LR ? Math.Min(pos0, pos1) : Math.Max(pos0, pos1); }
        double LeftMost(double pos0, double pos1) { return LR ? Math.Min(pos0, pos1) : Math.Max(pos0, pos1); }

        /// <summary>
        /// returns the "parameterized" right of the two positions
        /// </summary>
        /// <param name="pos0"></param>
        /// <param name="pos1"></param>
        /// <returns></returns>
        int RightMost(int pos0, int pos1) { return LR ? Math.Max(pos0, pos1) : Math.Min(pos0, pos1); }
        /// <summary>
        /// returns the "parameterized" right of the two positions
        /// </summary>
        /// <param name="pos0"></param>
        /// <param name="pos1"></param>
        /// <returns></returns>
        double RightMost(double pos0, double pos1) { return LR ? Math.Max(pos0, pos1) : Math.Min(pos0, pos1); }

        /// <summary>
        /// Return true if i is to the left or equal to pos in a "parameterized" fasion
        /// </summary>
        /// <param name="i"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        bool IsNotRightFrom(int i, int pos) { return LR ? i <= pos : pos <= i; }

        /// <summary>
        /// Parameterized left relation
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        bool IsLeftFrom(int i, int j) { return LR ? i < j : j < i; }

        /// <summary>
        /// parameterized next right
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        int NextRight(int i) { return LR ? i + 1 : i - 1; }

        /// <summary>
        /// parameterized next left
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        int NextLeft(int i) { return LR ? i - 1 : i + 1; }

        ///// <summary>
        ///// Eliminates conflicts with at least one inner edge inside of one layer
        ///// </summary>
        ///// <param name="i"></param>
        void ConflictsWithAtLeastOneInnerEdgeForALayer(int layerIndex) {
            if (layerIndex >= 0 && layerIndex < this.la.Layers.Length) {
                int[] lowerLayer = this.la.Layers[layerIndex];
                LayerEdge innerEdge = null;

                //start looking for the first inner edge from the left of lowerLayer
                int targetPos = LeftMost(0, lowerLayer.Length - 1);
                int lastTargetPos = RightMost(0, lowerLayer.Length - 1); ;

                for (; IsNotRightFrom(targetPos, lastTargetPos) && innerEdge == null;
                  targetPos = NextRight(targetPos))
                    innerEdge = InnerEdgeByTarget(lowerLayer[targetPos]);

                //now targetPos points to the right of the innerEdge target at lowerLayer
                if (innerEdge != null) {
                    int positionOfInnerEdgeSource = Pos(Source(innerEdge));
                    //We are still not in the main loop.
                    //We mark conflicting edges with targets to the left of targetPos,
                    //That of course means 
                    //that the sources of conflicting edges lie to the right of Source(innerEdge)
                    for (int j = LeftMost(0, lowerLayer.Length - 1);
                      IsLeftFrom(j, targetPos);
                      j = NextRight(j)) {
                        foreach (LayerEdge ie in InEdges(lowerLayer[j]))
                            if (IsLeftFrom(positionOfInnerEdgeSource, Pos(Source(ie))))
                                MarkEdge(ie);

                    }

                    int innerSourcePos = Pos(Source(innerEdge));
                    //starting the main loop
                    while (IsNotRightFrom(targetPos, lastTargetPos)) {
                        //Now we look for the next inner edge in the alignment to the right of the current innerEdge,
                        //and we mark the conflicts later. Marking the conflicts later makes sense. 
                        //We would have to go through positions between innerEdge and newInnerEdge targets 
                        //again anyway to resolve conflicts with not inner edges and newInnerEdge
                        LayerEdge newInnerEdge = AlignmentToTheRightOfInner(lowerLayer,
                            targetPos, positionOfInnerEdgeSource);

                        targetPos = NextRight(targetPos);
                        if (newInnerEdge != null) {
                            int newInnerSourcePos = Pos(Source(newInnerEdge));
                            MarkEdgesBetweenInnerAndNewInnerEdges(lowerLayer, innerEdge,
                              newInnerEdge, innerSourcePos, newInnerSourcePos);
                            innerEdge = newInnerEdge;
                            innerSourcePos = newInnerSourcePos;
                        }
                    }

                    //look for conflicting edges with targets to the right from the target of innerEdge
                    for (int k = NextRight(Pos(Target(innerEdge))); IsNotRightFrom(k, lastTargetPos); k = NextRight(k)) {

                        foreach (LayerEdge ie in InEdges(lowerLayer[k])) {
                            if (IsLeftFrom(Pos(Source(ie)), Pos(Source(innerEdge))))
                                MarkEdge(ie);
                        }
                    }
                }
            }
        }

        LayerEdge InEdgeOfVirtualNode(int v) { return (this.BT ? graph.InEdgeOfVirtualNode(v) : graph.OutEdgeOfVirtualNode(v)); }
      

        IEnumerable<LayerEdge> InEdges(int v) { return this.BT ? graph.InEdges(v) : graph.OutEdges(v); }
        ///// <summary>
        ///// This function marks conflicting edges with targets positioned between innerEdge and newInnerEdge targets.
        ///// </summary>
        ///// <param name="lowerLayer"></param>
        ///// <param name="innerEdge"></param>
        ///// <param name="newInnerEdge"></param>
        ///// <param name="posInnerEdgeTarget"></param>
        ///// <param name="posNewInnerEdgeTarget"></param>
        void MarkEdgesBetweenInnerAndNewInnerEdges(int[] lowerLayer, LayerEdge innerEdge, LayerEdge newInnerEdge,
          int innerEdgeSourcePos, int newInnerEdgeSourcePos) {
            int u = NextRight(Pos(Target(innerEdge)));


            for (; IsLeftFrom(u, Pos(Target(newInnerEdge))); u = NextRight(u)) {
                foreach (LayerEdge ie in InEdges(lowerLayer[u])) {
                    int ieSourcePos = Pos(Source(ie));
                    if (IsLeftFrom(ieSourcePos, innerEdgeSourcePos))//the equality is not possible
                        MarkEdge(ie);
                    else if (IsLeftFrom(newInnerEdgeSourcePos, ieSourcePos))
                        MarkEdge(ie);
                }
            }
        }
        ///// <summary>
        ///// Returns the inner non-conflicting edge incoming into i-th position 
        ///// of the layer or null if there is no such edge
        ///// </summary>
        ///// <param name="layer"></param>
        ///// <param name="innerEdge"></param>
        ///// <param name="i"></param>
        ///// <returns></returns>
        private LayerEdge AlignmentToTheRightOfInner(int[] lowLayer,
          int i,
          int posInnerSource
          ) {
            int numOfInEdges = NumberOfInEdges(lowLayer[i]);
            if(numOfInEdges==1){

                LayerEdge ie = null;
                foreach(LayerEdge e in InEdges(lowLayer[i]))
                    ie=e;
                if (IsInnerEdge(ie) && IsLeftFrom(posInnerSource, Pos(ie.Source)))
                    return ie;

                return null;
            }

            return null;
        }

        private int NumberOfInEdges(int v) {
            return this.BT ? graph.InEdgesCount(v) : graph.OutEdgesCount(v);
        }

        int Pos(int v) {
            return la.X[v];
        }



        LayerEdge InnerEdgeByTarget(int v) {
            if (IsVirtual(v)) {
                LayerEdge ie = InEdgeOfVirtualNode(v);//there is exactly one edge entering in to the dummy node
                if (IsVirtual(Source(ie)))
                    return ie;
            }
            return null;
        }


        bool IsInnerEdge(LayerEdge e) {
            return IsVirtual(e.Source) && IsVirtual(e.Target);
        }

        private void RemoveMarksFromEdges() {
            markedEdges.Clear();
        }

        ///// <summary>
        ///// private constructor
        ///// </summary>
        ///// <param name="layerArrays"></param>
        ///// <param name="anchs"></param>
        ///// <param name="layeredGraph"></param>
        ///// <param name="nOfOriginalVs"></param>
        XCoordsWithAlignment(LayerArrays layerArrays, ProperLayeredGraph layeredGraph, 
            int nOfOriginalVs, Anchor[] anchorsP, double ns) {
            this.la = layerArrays;
            this.graph = layeredGraph;
            this.nOfOriginalVertices = nOfOriginalVs;
            this.nOfVertices = graph.NodeCount;
            this.h = la.Layers.Length;
            this.root = new int[nOfVertices];
            this.align = new int[nOfVertices];
            // this.sink = new int[nOfVertices];
            // this.shift = new double[nOfVertices];
            this.anchors = anchorsP;
            this.nodeSep = ns;
        }

        /// <summary>
        ///Calculate the alignment based on the marked edges and greedily resolving the remaining conflicts on the fly, without marking
        /// </summary>
        void Align() {
                CreateBlocks();
                AssignCoordinatesByLongestPath();
        }

        void AssignCoordinatesByLongestPath() {
            this.x = this.xCoords[this.EnumRightUp] = new double[this.nOfVertices];
            /*
             * We create a graph of blocks or rather of block roots. There is an edge
             * from u-block to v-block  if some of elements of u-block is to the left of v 
             * on the same layer. Then we topologically sort the graph and assign coordinates 
             * taking into account separation between the blocks.
             */
            //create the graph first
            List<PolyIntEdge> edges = new List<PolyIntEdge>();
            for (int v = 0; v < nOfVertices; v++) {
                if (v == root[v])//v is a root
                {
                    int w = v;//w will be running over the block
                    do {
                        int rightNeighbor;
                        if (TryToGetRightNeighbor(w, out rightNeighbor))
                            edges.Add(new PolyIntEdge(v, root[rightNeighbor]));
                        w = align[w];
                    }
                    while (w != v);
                }
            }

            BasicGraphOnEdges<PolyIntEdge> blockGraph = new BasicGraphOnEdges<PolyIntEdge>(edges, nOfVertices);
            //sort the graph in the topological order
            int[] topoSort = PolyIntEdge.GetOrder(blockGraph);
            //start placing the blocks according to the order

            foreach (int v in topoSort) {
                if (v == root[v])//not every element of topoSort is a root!
                {
                    double vx = 0;
                    bool vIsLeftMost = true;
                    int w = v;//w is running over the block
                    do {
                        int wLeftNeighbor;
                        if (TryToGetLeftNeighbor(w, out wLeftNeighbor)) {
                            if (vIsLeftMost) {
                                vx = x[root[wLeftNeighbor]] + DeltaBetweenVertices(wLeftNeighbor, w);
                                vIsLeftMost = false;
                            } else
                                vx = RightMost(vx, x[root[wLeftNeighbor]] + DeltaBetweenVertices(wLeftNeighbor, w));
                        }
                        w = align[w];
                    }
                    while (w != v);

                    x[v] = vx;
                }
            }

            //push the roots of the graph maximally to the right 
            foreach (int v in topoSort) {
                if (v == root[v])
                    if (blockGraph.InEdges(v).Count == 0) {
                        int w = v;//w runs over the block
                        double xLeftMost = RightMost(-infinity, infinity);
                        double xl = xLeftMost;
                        do {
                            int wRightNeigbor;
                            if (TryToGetRightNeighbor(w, out wRightNeigbor))
                                xLeftMost = LeftMost(xLeftMost,
                                    x[root[wRightNeigbor]] - DeltaBetweenVertices(w, wRightNeigbor));

                            w = align[w];
                        } while (w != v);

                        //leave the value zero if there are no right neighbours
                        if (xl != xLeftMost)
                            x[v] = xLeftMost;
                    }

            }

            for (int v = 0; v < this.nOfVertices; v++) {
                if (v != root[v])
                    x[v] = x[root[v]];
            }

        }

        /// <summary>
        /// returns true is u has a right neighbor on its layer
        /// </summary>
        /// <param name="u"></param>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        bool TryToGetRightNeighbor(int u, out int neighbor) {
            int neighborPos = NextRight(Pos(u));
            int[] layer = la.Layers[la.Y[u]];
            if (neighborPos >= 0 && neighborPos < layer.Length) {
                neighbor = layer[neighborPos];
                return true;
            } else {
                neighbor = 0;
                return false;
            }
        }

        /// <summary>
        /// returns true is u has a right neighbor on its layer
        /// </summary>
        /// <param name="u"></param>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        bool TryToGetLeftNeighbor(int u, out int neighbor) {
            int neighborPos = NextLeft(Pos(u));
            int[] layer = la.Layers[la.Y[u]];
            if (neighborPos >= 0 && neighborPos < layer.Length) {
                neighbor = layer[neighborPos];
                return true;
            } else {
                neighbor = 0;
                return false;
            }
        }


        /// <summary>
        /// Organizes the vertices into blocks. A block is a maximal path in the alignment subgraph. 
        /// The alignment is defined by array align. Every vertex is connected to the top vertex of 
        /// the block by using root array. The alignment is cyclic. If we start from a root vertex v and 
        /// apply align then we return to v at some point.
        /// </summary>
        void CreateBlocks() {

            for (int v = 0; v < nOfVertices; v++)
                root[v] = align[v] = v;

            int lowBound = this.LowerOf(0, h - 1);

            //i points to the last layer before the highest one

            for (int i = NextLower(this.UpperOf(0, h - 1)); !IsBelow(i, lowBound); i = NextLower(i)) {
                int[] layer = la.Layers[i];

                int r = LeftMost(-1, la.Layers[NextUpper(i)].Length);
                //We align vertices of the layer above the i-th one only if their positions are
                //to the right of r. This moves us forward on the layer above the current and resolves the conflicts.

                int rightBound = RightMost(0, layer.Length - 1);
                for (int k = LeftMost(0, layer.Length - 1);
                    IsNotRightFrom(k, rightBound); k = NextRight(k)) {
                    int vk = layer[k];
                    foreach (int upperNeighborOfVk in UpperEdgeMedians(vk)) {
                        if (!IsMarked(vk, upperNeighborOfVk)) {
                            if (IsLeftFrom(r, Pos(upperNeighborOfVk))) {
                                align[upperNeighborOfVk] = vk;
                                align[vk] = root[vk] = root[upperNeighborOfVk];
                                r = Pos(upperNeighborOfVk);
                                break;// done with the alignement for vk
                            }
                        }
                    }
                }
            }
        }
        

        private bool IsMarked(int source, int target) {
            if (BT)
                return markedEdges.Contains(new IntPair(target, source));
            else
                return markedEdges.Contains(new IntPair(source, target));
        }

        private void MarkEdge(LayerEdge ie) {
            this.markedEdges.Insert(new IntPair(ie.Source, ie.Target));
        }

        /// <summary>
        /// Assigning xcoords starting from roots
        /// </summary>

        const double infinity = Double.MaxValue;

        /// <summary>
        /// Calculates the minimum separation between two neighboring vertices: if u is to the left of v on the same layer return positive
        /// number, otherwise negative.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        double DeltaBetweenVertices(int u, int v) {
            int sign = 1;
            if (Pos(u) > Pos(v)) { //swap u and v
                int t = u;
                u = v;
                v = t;
                sign = -1;
            }

            double anchorSepar = anchors[u].RightAnchor + anchors[v].LeftAnchor;
            return (anchorSepar + nodeSep) * sign;

        }

    }
}
