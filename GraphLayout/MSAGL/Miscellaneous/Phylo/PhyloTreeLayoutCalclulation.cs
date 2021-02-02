using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Prototype.Phylo {
    internal class PhyloTreeLayoutCalclulation : AlgorithmBase{
        Anchor[] anchors;
        ProperLayeredGraph properLayeredGraph;
        Database dataBase;
        PhyloTree tree;
        BasicGraph<Node, PolyIntEdge> intGraph;
        LayerArrays layerArrays;
        SortedDictionary<int, double> gridLayerOffsets = new SortedDictionary<int, double>();
        double[] layerOffsets;
        double cellSize;
        Dictionary<Node, double> nodeOffsets = new Dictionary<Node, double>();
        Dictionary<Node, int> nodesToIndices = new Dictionary<Node, int>();
        int[] originalNodeToGridLayerIndices;
        Dictionary<int, int> gridLayerToLayer=new Dictionary<int,int>();

        ///// <summary>
        ///// the layout responsible for the algorithm parameters
        ///// </summary>
        internal SugiyamaLayoutSettings LayoutSettings { get; private set; }

        internal PhyloTreeLayoutCalclulation(PhyloTree phyloTreeP, SugiyamaLayoutSettings settings, BasicGraph<Node, PolyIntEdge> intGraphP, Database dataBase) {
            this.dataBase = dataBase;
            this.tree = phyloTreeP;
            this.LayoutSettings = settings;
            this.intGraph = intGraphP;
            originalNodeToGridLayerIndices = new int[intGraph.Nodes.Count];
        }

        protected override void RunInternal() {
            if (!IsATree())
                throw new InvalidDataException("the graph is not a tree");
            DefineCellSize();
            CalculateOriginalNodeToGridLayerIndices();
            CreateLayerArraysAndProperLayeredGraph();            
            FillDataBase();
            RunXCoordinateAssignmentsByBrandes();
            CalcTheBoxFromAnchors();
            StretchIfNeeded();
            ProcessPositionedAnchors();

//            SugiyamaLayoutSettings.ShowDataBase(this.dataBase);

            RouteSplines();
        }

        bool IsATree() {
            Set<Node> visited = new Set<Node>();
            Node root = tree.Nodes.FirstOrDefault(n => !n.InEdges.Any());
            if (root == null)
                return false;

            return IsATreeUnderNode(root, visited) && visited.Count==tree.Nodes.Count;
        }

        static bool IsATreeUnderNode(Node node, Set<Node> visited) {
            if (visited.Contains(node)) return false;
            visited.Insert(node);
            return node.OutEdges.All(outEdge => IsATreeUnderNode(outEdge.Target, visited));
        }

        private void StretchIfNeeded() {
            if (this.LayoutSettings.AspectRatio != 0) {
                double aspectRatio = this.tree.Width / this.tree.Height;
                StretchToDesiredAspectRatio(aspectRatio, LayoutSettings.AspectRatio);
            }
        }

        private void StretchToDesiredAspectRatio(double aspectRatio, double desiredAR) {
            if (aspectRatio > desiredAR)
                StretchInYDirection(aspectRatio / desiredAR);
            else if (aspectRatio < desiredAR)
                StretchInXDirection(desiredAR / aspectRatio);
        }
        private void StretchInYDirection(double scaleFactor) {
            double center = (this.tree.BoundingBox.Top + this.tree.BoundingBox.Bottom) / 2;
            foreach (Anchor a in this.dataBase.Anchors) {
                a.BottomAnchor *= scaleFactor;
                a.TopAnchor *= scaleFactor;
                a.Y = center + scaleFactor * (a.Y - center);
            }
            double h = this.tree.Height * scaleFactor;
            this.tree.BoundingBox = new Rectangle(this.tree.BoundingBox.Left, center + h / 2, this.tree.BoundingBox.Right, center - h / 2);

        }
       
        private void StretchInXDirection(double scaleFactor) {
            double center = (this.tree.BoundingBox.Left + this.tree.BoundingBox.Right) / 2;
            foreach (Anchor a in this.dataBase.Anchors) {
                a.LeftAnchor *= scaleFactor;
                a.RightAnchor *= scaleFactor;
                a.X = center + scaleFactor * (a.X - center);
            }
            double w = this.tree.Width * scaleFactor;
            this.tree.BoundingBox =
                new Rectangle(center - w / 2, tree.BoundingBox.Top,
                center + w / 2, this.tree.BoundingBox.Bottom);
        }

        private void DefineCellSize() {

            double min = double.MaxValue;
            foreach (PhyloEdge e in this.tree.Edges) 
                min = Math.Min(min, e.Length);

            this.cellSize=0.3*min;

        }

        private void CalculateOriginalNodeToGridLayerIndices() {
            InitNodesToIndices();
            FillNodeOffsets();
            foreach (KeyValuePair<Node, double> kv in this.nodeOffsets) {
                int nodeIndex = this.nodesToIndices[kv.Key];
                int gridLayerIndex=originalNodeToGridLayerIndices[nodeIndex] = GetGridLayerIndex(kv.Value);
                if (!gridLayerOffsets.ContainsKey(gridLayerIndex))
                    gridLayerOffsets[gridLayerIndex] = kv.Value;
            }
        }

        private int GetGridLayerIndex(double len) {
            return (int)(len / this.cellSize + 0.5);
        }

        private void InitNodesToIndices() {
            for (int i = 0; i < this.intGraph.Nodes.Count; i++)
                nodesToIndices[intGraph.Nodes[i]] = i;
        }

        private void FillNodeOffsets() {
            FillNodeOffsets(0.0, tree.Root);
        }

        private void FillNodeOffsets(double p, Node node) {
            nodeOffsets[node] = p;
            foreach (PhyloEdge e in node.OutEdges)
                FillNodeOffsets(p+e.Length, e.Target);
        }


        private  void FillDataBase() {
            foreach (PolyIntEdge e in intGraph.Edges)
                dataBase.RegisterOriginalEdgeInMultiedges(e);
            SizeAnchors();
            FigureYCoordinates();

        }

        private void FigureYCoordinates() {
            double m = GetMultiplier();
            int root = nodesToIndices[tree.Root];
            CalculateAnchorsY(root,m,0);

            for (int i = intGraph.NodeCount; i < dataBase.Anchors.Length; i++)
                dataBase.Anchors[i].Y = -m * layerOffsets[layerArrays.Y[i]];
   
            //fix layer offsets
            for (int i = 0; i < layerOffsets.Length; i++)
                layerOffsets[i] *= m;

 
        }

        private double GetMultiplier() {
            double m = 1;
            for (int i = layerArrays.Layers.Length - 1; i > 0; i--) {
                double nm = GetMultiplierBetweenLayers(i);
                if (nm > m)
                    m = nm;
            }

            return m;
        }

        private double GetMultiplierBetweenLayers(int i) {
            int a = FindLowestBottomOnLayer(i);
            int b = FindHighestTopOnLayer(i - 1);
            double ay = NodeY(i, a);
            double by = NodeY(i - 1, b);
            // we need to have m*(a[y]-b[y])>=anchors[a].BottomAnchor+anchors[b].TopAnchor+layerSeparation;
            double diff = ay - by;
            if (diff < 0)
                throw new InvalidOperationException();
            double nm = (dataBase.Anchors[a].BottomAnchor + dataBase.Anchors[b].TopAnchor + LayoutSettings.LayerSeparation) / diff;
            if (nm > 1)
                return nm;
            return 1;
        }

        private int FindHighestTopOnLayer(int layerIndex) {
            int[] layer = layerArrays.Layers[layerIndex];
            int ret = layer[0];
            double top = NodeY(layerIndex, ret) + dataBase.Anchors[ret].TopAnchor;
            for (int i = 1; i < layer.Length; i++) {
                int node=layer[i];
                double nt = NodeY(layerIndex, node) + dataBase.Anchors[node].TopAnchor;
                if (nt > top) {
                    top = nt;
                    ret = node;
                }

            }
            return ret;
        }

        private int FindLowestBottomOnLayer(int layerIndex) {
            int[] layer = layerArrays.Layers[layerIndex];
            int ret = layer[0];
            double bottom = NodeY(layerIndex, ret) - dataBase.Anchors[ret].BottomAnchor;
            for (int i = 1; i < layer.Length; i++) {
                int node = layer[i];
                double nb = NodeY(layerIndex, node) - dataBase.Anchors[node].BottomAnchor;
                if (nb < bottom) {
                    bottom = nb;
                    ret = node;
                }

            }
            return ret;
        }

        private double NodeY(int layer, int node) {
            return - (IsOriginal(node) ? nodeOffsets[intGraph.Nodes[node]] : layerOffsets[layer]);
        }

        private bool IsOriginal(int node) {
            return node < intGraph.NodeCount;
        }

        private void CalculateAnchorsY(int node, double m, double y) {
            //go over original nodes
            dataBase.Anchors[node].Y = -y;
            foreach (PolyIntEdge e in intGraph.OutEdges(node))
                CalculateAnchorsY(e.Target, m, y + e.Edge.Length * m);
   
        }

        private void SizeAnchors() {
            dataBase.Anchors = anchors = new Anchor[properLayeredGraph.NodeCount];

            for (int i = 0; i < anchors.Length; i++)
                anchors[i] = new Anchor(LayoutSettings.LabelCornersPreserveCoefficient);

            //go over the old vertices
            for (int i = 0; i < intGraph.NodeCount; i++)
                CalcAnchorsForOriginalNode(i);

            //go over virtual vertices
            foreach (PolyIntEdge intEdge in dataBase.AllIntEdges) {
                if (intEdge.LayerEdges != null) {
                    foreach (LayerEdge layerEdge in intEdge.LayerEdges) {
                        int v = layerEdge.Target;
                        if (v != intEdge.Target) {
                            Anchor anchor = anchors[v];
                            if (!dataBase.MultipleMiddles.Contains(v)) {
                                anchor.LeftAnchor = anchor.RightAnchor = VirtualNodeWidth / 2.0f;
                                anchor.TopAnchor = anchor.BottomAnchor = VirtualNodeHeight / 2.0f;
                            } else {
                                anchor.LeftAnchor = anchor.RightAnchor = VirtualNodeWidth * 4;
                                anchor.TopAnchor = anchor.BottomAnchor = VirtualNodeHeight / 2.0f;
                            }
                        }
                    }
                    //fix label vertices      

                    if (intEdge.Edge.Label!=null) {
                        int lj = intEdge.LayerEdges[intEdge.LayerEdges.Count / 2].Source;
                        Anchor a = anchors[lj];
                        double w = intEdge.LabelWidth, h = intEdge.LabelHeight;
                        a.RightAnchor = w;
                        a.LeftAnchor = LayoutSettings.NodeSeparation;

                        if (a.TopAnchor < h / 2.0)
                            a.TopAnchor = a.BottomAnchor = h / 2.0;

                        a.LabelToTheRightOfAnchorCenter = true;
                    }
                }
            }

        }

        /// <summary>
        /// the width of dummy nodes
        /// </summary>
        static double VirtualNodeWidth {
            get {
                return 1;
            }
        }

        /// <summary>
        /// the height of dummy nodes
        /// </summary>
        double VirtualNodeHeight {
            get {
                return LayoutSettings.MinNodeHeight * 1.5f / 8;
            }
        }


        void CalcAnchorsForOriginalNode(int i) {

            double leftAnchor = 0;
            double rightAnchor = leftAnchor;
            double topAnchor = 0;
            double bottomAnchor = topAnchor;

            //that's what we would have without the label and multiedges 

            if (intGraph.Nodes != null) {
                Node node = intGraph.Nodes[i];
                ExtendStandardAnchors(ref leftAnchor, ref rightAnchor, ref topAnchor, ref bottomAnchor, node);
            }

            RightAnchorMultiSelfEdges(i, ref rightAnchor, ref topAnchor, ref bottomAnchor);

            double hw = LayoutSettings.MinNodeWidth / 2;
            if (leftAnchor < hw)
                leftAnchor = hw;
            if (rightAnchor < hw)
                rightAnchor = hw;
            double hh = LayoutSettings.MinNodeHeight / 2;

            if (topAnchor < hh)
                topAnchor = hh;
            if (bottomAnchor < hh)
                bottomAnchor = hh;

            anchors[i] = new Anchor(leftAnchor, rightAnchor, topAnchor, bottomAnchor, this.intGraph.Nodes[i], LayoutSettings.LabelCornersPreserveCoefficient)
                         {Padding = this.intGraph.Nodes[i].Padding};
#if TEST_MSAGL
            //anchors[i].Id = this.intGraph.Nodes[i].Id;
#endif
        }

        private static void ExtendStandardAnchors(ref double leftAnchor, ref double rightAnchor, ref double topAnchor, ref double bottomAnchor, Node node) {
            double w = node.Width;
            double h = node.Height;


            w /= 2.0;
            h /= 2.0;


            rightAnchor = leftAnchor = w;
            topAnchor = bottomAnchor = h;
        }

        private void RightAnchorMultiSelfEdges(int i, ref double rightAnchor, ref double topAnchor, ref double bottomAnchor) {
            double delta = WidthOfSelfeEdge(i, ref rightAnchor, ref topAnchor, ref bottomAnchor);

            rightAnchor += delta;
        }


        private double WidthOfSelfeEdge(int i, ref double rightAnchor, ref double topAnchor, ref double bottomAnchor) {
            double delta = 0;
            List<PolyIntEdge> multiedges = dataBase.GetMultiedge(i, i);
            //it could be a multiple self edge
            if (multiedges.Count > 0) {
                foreach (PolyIntEdge e in multiedges) {
                    if (e.Edge.Label != null) {
                        rightAnchor += e.Edge.Label.Width;
                        if (topAnchor < e.Edge.Label.Height / 2.0)
                            topAnchor = bottomAnchor = e.Edge.Label.Height / 2.0f;
                    }
                }

                delta += (LayoutSettings.NodeSeparation + LayoutSettings.MinNodeWidth) * multiedges.Count;
            }
            return delta;
        }

        private  void RouteSplines() {
            Layout.Layered.Routing routing = new Layout.Layered.Routing(LayoutSettings, this.tree, dataBase, this.layerArrays, this.properLayeredGraph, null);
            routing.Run();
        }

        private  void RunXCoordinateAssignmentsByBrandes() {
            XCoordsWithAlignment.CalculateXCoordinates(this.layerArrays, this.properLayeredGraph, this.tree.Nodes.Count, this.dataBase.Anchors, this.LayoutSettings.NodeSeparation);
        }

        private  void CreateLayerArraysAndProperLayeredGraph() {
            int numberOfLayers = this.gridLayerOffsets.Count;
            this.layerOffsets=new double[numberOfLayers];
            int i = numberOfLayers-1;

            foreach (KeyValuePair<int, double> kv in this.gridLayerOffsets) {
                layerOffsets[i] = kv.Value;
                gridLayerToLayer[kv.Key] = i--;
            }

            int nOfNodes=CountTotalNodesIncludingVirtual(nodesToIndices[tree.Root]);

            ////debugging !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //int tt = 0;
            //foreach (IntEdge ie in this.intGraph.Edges)
            //    tt += this.OriginalNodeLayer(ie.Source) - this.OriginalNodeLayer(ie.Target) - 1;
            //if (tt + this.intGraph.Nodes.Count != nOfNodes)
            //    throw new Exception();

            int[] layering = new int[nOfNodes];

            List<int>[] layers = new List<int>[numberOfLayers];
            for (i = 0; i < numberOfLayers; i++)
                layers[i] = new List<int>();

            WalkTreeAndInsertLayerEdges(layering, layers);

            this.layerArrays = new LayerArrays(layering);

            int[][]ll=layerArrays.Layers=new int[numberOfLayers][];

            i = 0;
            foreach (List<int> layer in layers) {
                ll[i++] = layer.ToArray();
            }

            this.properLayeredGraph = new ProperLayeredGraph(intGraph);
        }

    
        private int CountTotalNodesIncludingVirtual(int node) {
            int ret = 1;
            foreach (PolyIntEdge edge in this.intGraph.OutEdges(node)) 
                ret += NumberOfVirtualNodesOnEdge(edge) + CountTotalNodesIncludingVirtual(edge.Target);
            
            return ret;
        }

        private int NumberOfVirtualNodesOnEdge(PolyIntEdge edge) {
            return OriginalNodeLayer(edge.Source) - OriginalNodeLayer(edge.Target) - 1;
        }

        private int OriginalNodeLayer(int node) {
            return gridLayerToLayer[originalNodeToGridLayerIndices[node]];
        }

        private void WalkTreeAndInsertLayerEdges(int[] layering, List<int>[] layers) {
            int virtualNode = this.intGraph.NodeCount;
            int root = nodesToIndices[tree.Root];
            int l;
            layering[root] = l = OriginalNodeLayer(root);
            layers[l].Add(root);
            WalkTreeAndInsertLayerEdges(layering, layers, root , ref virtualNode);
        }

        private void WalkTreeAndInsertLayerEdges(int[] layering, List<int>[] layers, int node, ref int virtualNode) {
            foreach (PolyIntEdge edge in this.intGraph.OutEdges(node)) 
                InsertLayerEdgesForEdge(edge, layering, ref virtualNode, layers);
        }

        private void InsertLayerEdgesForEdge(PolyIntEdge edge, int[] layering, ref int virtualNode, List<int>[] layers) {
            int span = OriginalNodeLayer(edge.Source) - OriginalNodeLayer(edge.Target);
            edge.LayerEdges=new LayerEdge[span];
            for (int i = 0; i < span; i++) 
                edge.LayerEdges[i] = new LayerEdge(GetSource(i, edge, ref virtualNode), GetTarget(i, span, edge, virtualNode), edge.CrossingWeight);
       
            int l = OriginalNodeLayer(edge.Source) - 1;
            for (int i = 0; i < span; i++) {
                int node=edge.LayerEdges[i].Target;
                layering[node] = l;
                layers[l--].Add(node);
            }

            WalkTreeAndInsertLayerEdges(layering, layers, edge.Target, ref virtualNode);

        }

        static private int GetTarget(int i, int span, PolyIntEdge edge, int virtualNode) {
            if (i < span-1)
                return virtualNode;

            return edge.Target;
        }

        static private int GetSource(int i, PolyIntEdge edge, ref int virtualNode) {
            if (i > 0)
                return virtualNode++; 
            
            return edge.Source;
        }

        void ProcessPositionedAnchors() {
            for (int i = 0; i < this.tree.Nodes.Count; i++)
                intGraph.Nodes[i].Center=anchors[i].Origin;
        }

        private void CalcTheBoxFromAnchors() {
            if (anchors.Length > 0) {

                Rectangle box = new Rectangle(anchors[0].Left, anchors[0].Top, anchors[0].Right, anchors[0].Bottom);

                for (int i = 1; i < anchors.Length; i++) {
                    Anchor a = anchors[i];
                    box.Add(a.LeftTop);
                    box.Add(a.RightBottom);
                }


                double m = Math.Max(box.Width, box.Height);

                double delta = this.tree.Margins / 100.0 * m;

                Point del = new Point(-delta, delta);
                box.Add(box.LeftTop + del);
                box.Add(box.RightBottom - del);
                this.tree.BoundingBox = box;
            }
        }

    }
}
