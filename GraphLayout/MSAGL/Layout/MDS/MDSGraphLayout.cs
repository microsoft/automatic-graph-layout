using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree;
using System.Threading.Tasks;

namespace Microsoft.Msagl.Layout.MDS {
    /// <summary>
    /// Class for graph layout with multidimensional scaling.
    /// </summary>
    public class MdsGraphLayout : AlgorithmBase {
        
        readonly GeometryGraph graph;

        readonly MdsLayoutSettings settings;

        /// <summary>
        /// Constructs the multidimensional scaling algorithm.
        /// </summary>
        public MdsGraphLayout(MdsLayoutSettings settings, GeometryGraph geometryGraph)
        {
            this.settings = settings;
            graph = geometryGraph;
        }

        /// <summary>
        /// Executes the algorithm
        /// </summary>
        protected override void RunInternal() {
            LayoutConnectedComponents();
            SetGraphBoundingBox();
        }
         void SetGraphBoundingBox() {
            graph.BoundingBox = graph.PumpTheBoxToTheGraphWithMargins();
        }

        
        /// <summary>
        /// Scales a configuration such that the average edge length in the drawing
        /// equals the average of the given edge lengths.
        /// </summary>
        /// <param name="g">A graph.</param>
        /// <param name="x">Coordinates.</param>
        /// <param name="y">Coordinates.</param>
         static void ScaleToAverageEdgeLength(GeometryGraph g, double[] x, double[] y) {
            var index = new Dictionary<Node, int>();
            int c=0;
            foreach(Node node in g.Nodes) {
                index.Add(node, c);
                c++;
            }
            double avgSum = 0, avgLength = 0;
            foreach(Edge edge in g.Edges) {
                int i=index[edge.Source];
                int j=index[edge.Target];
                avgSum += Math.Sqrt(Math.Pow(x[i] - x[j], 2) + Math.Pow(y[i] - y[j], 2));
                avgLength += edge.Length;
            }
            if(avgLength>0) avgSum /= avgLength;
            if(avgSum>0)
                for (int i = 0; i < x.Length; i++) {
                    x[i] /= avgSum;
                    y[i] /= avgSum;
                }
        }


        /// <summary>
        /// Layouts a connected graph with Multidimensional Scaling, using
        /// shortest-path distances as Euclidean target distances.
        /// </summary>
        /// <param name="geometryGraph">A graph.</param>
        /// <param name="settings">The settings for the algorithm.</param>
        /// <param name="x">Coordinate vector.</param>
        /// <param name="y">Coordinate vector.</param>
        internal static void LayoutGraphWithMds(GeometryGraph geometryGraph, MdsLayoutSettings settings, out double[] x, out double[] y) {            
            x = new double[geometryGraph.Nodes.Count];
            y = new double[geometryGraph.Nodes.Count];
            if (geometryGraph.Nodes.Count == 0) return;
            if (geometryGraph.Nodes.Count == 1)
            {
                x[0] = y[0] = 0;
                return;
            }
            int k = Math.Min(settings.PivotNumber, geometryGraph.Nodes.Count);
            int iter = settings.GetNumberOfIterationsWithMajorization(geometryGraph.Nodes.Count);
            double exponent = settings.Exponent;

            var pivotArray = new int[k];
            PivotDistances pivotDistances = new PivotDistances(geometryGraph, false, pivotArray);
            pivotDistances.Run();
            double[][] c = pivotDistances.Result;
            MultidimensionalScaling.LandmarkClassicalScaling(c, out x, out y, pivotArray);
            ScaleToAverageEdgeLength(geometryGraph, x, y);
            
            if (iter > 0) {
                AllPairsDistances apd = new AllPairsDistances(geometryGraph);
                apd.Run();
                double[][] d = apd.Result;
                double[][] w = MultidimensionalScaling.ExponentialWeightMatrix(d, exponent);
                // MultidimensionalScaling.DistanceScaling(d, x, y, w, iter);
                MultidimensionalScaling.DistanceScalingSubset(d, x, y, w, iter);
            }
        }


        // class GeometryGraphComparer : IComparer<GeometryGraph> {
        //    public int Compare(GeometryGraph g1, GeometryGraph g2) {
        //        return g2.Nodes.Count.CompareTo(g1.Nodes.Count);
        //    }
        //}

        /// <summary>
        /// Computes layout for possibly disconnected graphs by putting
        /// the layouts for connected components together.
        /// </summary>
        internal void LayoutConnectedComponents() {           
            GeometryGraph[] graphs = GraphConnectedComponents.CreateComponents(graph.Nodes, graph.Edges, this.settings.NodeSeparation).ToArray();
            // layout components, compute bounding boxes

            if (settings.RunInParallel) {
                ParallelOptions options = new ParallelOptions();
#if PPC
                if (this.CancelToken != null)
                    options.CancellationToken = this.CancelToken.CancellationToken;
#endif
                System.Threading.Tasks.Parallel.ForEach(graphs, options, LayoutConnectedGraphWithMds);
            }
            else
                for (int i = 0; i < graphs.Length; i++) {
                    LayoutConnectedGraphWithMds(graphs[i]);
                }

            if (graphs.Length > 1) {
                PackGraphs(graphs, settings);
                //restore the parents
                foreach (var node in graphs.SelectMany(g => g.Nodes))
                    node.GeometryParent = graph;
            }
        }

        void LayoutConnectedGraphWithMds(GeometryGraph compGraph)
        {
            double[] x, y;

            LayoutGraphWithMds(compGraph, settings, out x, out y);
            if (settings.RotationAngle != 0)
                Transform.Rotate(x, y, settings.RotationAngle);

            double scaleX = settings.ScaleX;
            double scaleY = settings.ScaleY;
            int index = 0;
            foreach (Node node in compGraph.Nodes)
            {
                node.Center = new Point(x[index] * scaleX, y[index] * scaleY);
                index++;
                if ((index % 100) == 0)
                {
                    ProgressStep();
                }
            }
            if (settings.AdjustScale)
                AdjustScale(compGraph.Nodes);

            if (settings.RemoveOverlaps)
            {
                GTreeOverlapRemoval.RemoveOverlaps(compGraph.Nodes.ToArray(), settings.NodeSeparation);
            }
            

            compGraph.BoundingBox = compGraph.PumpTheBoxToTheGraphWithMargins();
        }

        void AdjustScale(IList<Node> nodes) {
            if (nodes.Count <= 5)
                return; //we can have only a little bit of white space with a layout with only few nodes
            int repetitions = 10;
            var scale = 1.0;
            var delta = 0.5;
            var tree = BuildNodeTree(nodes);
            var random = new Random(1);
            do
            {
                const int minNumberOfHits = 6;
                const int maxNumberOfHits = 15; 
                const int numberOfChecks = 100; 
                int hits = NumberOfHits(numberOfChecks, random, tree, maxNumberOfHits);
                if (hits < minNumberOfHits)
                    scale /= 1+delta;
                else if (hits > maxNumberOfHits)
                    scale *= 1+delta;
                else {
                    return;                    
                }
                delta /= 2;
                //adjust the scale the graph
                ScaleNodes(nodes, scale);
                if (repetitions-- == 0)
                    return;
                UpdateTree(tree);
            } while (true);            
        }

        void ScaleNodes(IList<Node> nodes, double scale) {
            int i = 0;
            foreach (Node node in nodes) {
                node.Center *= scale;
                i++;
                if ((i%100) == 0)
                    ProgressStep();
            }
        }

        static void UpdateTree(RectangleNode<Node, Point> tree) {
            if (tree.IsLeaf)
                tree.Rectangle = tree.UserData.BoundingBox;
            else {
                UpdateTree(tree.Left);
                UpdateTree(tree.Right);
                tree.rectangle = tree.Left.rectangle;
                tree.rectangle.Add(tree.Right.rectangle);
            }

        }

        static int NumberOfHits(int numberOfChecks, Random random, RectangleNode<Node, Point> tree, int maxNumberOfHits) {
           // var l = new List<Point>();
            int numberOfHits = 0;
            for (int i = 0; i < numberOfChecks; i++) {
                Point point = RandomPointFromBox(random, (Rectangle)tree.rectangle);
             //   l.Add(point);
                if ((tree.FirstHitNode(point, (p, t) => HitTestBehavior.Stop)) != null)
                    numberOfHits++;
                if (numberOfHits == maxNumberOfHits)
                    return maxNumberOfHits;
            }
            //LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(Getdc(tree, l));
            return numberOfHits;
        }
        /*
        static IEnumerable<DebugCurve> Getdc(RectangleNode<Node, Point> tree, List<Point> points) {
            foreach (var point in points)
                yield return new DebugCurve("red", new Ellipse(5, 5, point));
            foreach (var rn in tree.GetAllLeafNodes()) {
                yield return new DebugCurve("blue", rn.Rectangle.Perimeter());
                yield return new DebugCurve("green", rn.UserData.BoundaryCurve);
            }
        }
        */
        static RectangleNode<Node, Point> BuildNodeTree(IList<Node> nodes) {
            return  RectangleNode<Node, Point>.CreateRectangleNodeOnEnumeration(
                nodes.Select(n => new RectangleNode<Node, Point>(n, n.BoundingBox)));
        }


        static Point RandomPointFromBox(Random random, Rectangle boundingBox) {
            var x=random.NextDouble();
            var y=random.NextDouble();
            var p= new Point(boundingBox.Left + boundingBox.Width * x, boundingBox.Bottom + boundingBox.Height * y);
            return p;
        }

        /// <summary>
        /// Pack the given graph components to the specified aspect ratio
        /// </summary>
        /// <param name="components">set of graphs to pack</param>
        /// <param name="settings">settings for packing method and desired aspect ratio</param>
        /// <returns>Bounding box of the packed components</returns>
        public static Rectangle PackGraphs(IEnumerable<GeometryGraph> components, LayoutAlgorithmSettings settings) {
            List<RectangleToPack<GeometryGraph>> rectangles =
                (from c in components select new RectangleToPack<GeometryGraph>(c.BoundingBox, c)).ToList();
            if (rectangles.Count > 1) {
                OptimalPacking<GeometryGraph> packing = settings.PackingMethod == PackingMethod.Compact
                    ? new OptimalRectanglePacking<GeometryGraph>(rectangles, settings.PackingAspectRatio)
                    : (OptimalPacking<GeometryGraph>)
                        new OptimalColumnPacking<GeometryGraph>(rectangles, settings.PackingAspectRatio);
                packing.Run();
                foreach (var r in rectangles) {
                    GeometryGraph component = r.Data;
                    var delta = r.Rectangle.LeftBottom - component.boundingBox.LeftBottom;
                    component.Translate(delta);
                }
                return new Rectangle(0, 0, packing.PackedWidth, packing.PackedHeight);
            }
            if (rectangles.Count == 1)
                return rectangles[0].Rectangle;
            return Rectangle.CreateAnEmptyBox();
        }
    }
}
