//-----------------------------------------------------------------------
// <copyright file="RandomBundlingTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
#if TEST_MSAGL
using Microsoft.Msagl.GraphViewerGdi;
#endif
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Msagl.Layout.Layered;

namespace Microsoft.Msagl.UnitTests {
    /// <summary>
    /// Test class for methods inside EdgeRouterHelper
    /// </summary>
    [TestClass]
    [DeploymentItem(@"Resources\MSAGLGeometryGraphs")]
    public class RandomBundlingTests : MsaglTestBase {
        [TestMethod]
        [Description("Random small tree")]
        public void RouteEdges_SmallTree() {
            //DisplayGeometryGraph.SetShowFunctions();

            Random random = new Random(1);
            int ntest = 100;
            for (int i = 0; i < ntest; i++) {
                GeometryGraph graph = GraphGenerator.GenerateTree(10 + random.Next(10), random);
                AddRootCluster(graph);
                SetRandomNodeShapes(graph, random);

                Layout(graph, random);
                //DisplayGeometryGraph.ShowGraph(graph);

                RouteEdges(graph, 5 * random.NextDouble());
                //DisplayGeometryGraph.ShowGraph(graph);
            }
        }

        [TestMethod]
        [Description("Random large tree")]
        public void RouteEdges_LargeTree() {
            //DisplayGeometryGraph.SetShowFunctions();

            Random random = new Random(1);
            int ntest = 10;
            for (int i = 0; i < ntest; i++) {
                GeometryGraph graph = GraphGenerator.GenerateTree(50 + random.Next(100));
                AddRootCluster(graph);
                SetRandomNodeShapes(graph, random);

                Layout(graph, random);
                //DisplayGeometryGraph.ShowGraph(graph);

                RouteEdges(graph, 5 * random.NextDouble());
                //DisplayGeometryGraph.ShowGraph(graph);
            }
        }

        [TestMethod]
        [Description("Random small grid graph")]
        public void RouteEdges_SmallGrid() {
            //DisplayGeometryGraph.SetShowFunctions();

            Random random = new Random(1);
            int ntest = 20;
            for (int i = 0; i < ntest; i++) {
                GeometryGraph graph = GraphGenerator.GenerateSquareLattice(20 + random.Next(20));
                AddRootCluster(graph);
                SetRandomNodeShapes(graph, random);
                int additionalEdges = random.Next(20);
                
                for (int j = 0; j < additionalEdges; j++) {
                    Node source = graph.Nodes[random.Next(graph.Nodes.Count)];
                    Node target = graph.Nodes[random.Next(graph.Nodes.Count)];
                    Edge edge = GraphGenerator.CreateEdge(source, target);
                    graph.Edges.Add(edge);
                }

                Layout(graph, random);
                //DisplayGeometryGraph.ShowGraph(graph);

                RouteEdges(graph, 5 * random.NextDouble());
                //DisplayGeometryGraph.ShowGraph(graph);
            }
        }

        [TestMethod]
        [Description("Random graph with small subgraphs")]
        public void RouteEdges_Subgraphs() {
            //DisplayGeometryGraph.SetShowFunctions();

            Random random = new Random(1);
            int ntest = 20;
            for (int i = 0; i < ntest; i++) {
                int numberOfSubgraphs = 2 + random.Next(10);
                int numberOfNodesInSubgraphs = 2 + random.Next(10);
                GeometryGraph graph = GraphGenerator.GenerateGraphWithSameSubgraphs(numberOfSubgraphs, numberOfNodesInSubgraphs);
                AddRootCluster(graph);
                SetRandomNodeShapes(graph, random);

                int additionalEdges = random.Next(10);
                for (int j = 0; j < additionalEdges; j++) {
                    Node source = graph.Nodes[random.Next(graph.Nodes.Count)];
                    Node target = graph.Nodes[random.Next(graph.Nodes.Count)];
                    Edge edge = GraphGenerator.CreateEdge(source, target);
                    graph.Edges.Add(edge);
                }

                Layout(graph, random);
                //DisplayGeometryGraph.ShowGraph(graph);

                RouteEdges(graph, 5 * random.NextDouble());
                //DisplayGeometryGraph.ShowGraph(graph);
            }
        }

        [TestMethod]
        [Description("Random small graph")]
        public void RouteEdges_RandomGraph() {
            //DisplayGeometryGraph.SetShowFunctions();

            Random random = new Random(1);
            int ntest = 100;
            for (int i = 0; i < ntest; i++) {
                int nodeCount = 5 + random.Next(10);
                int edgeCount = 10 + random.Next(20);
                GeometryGraph graph = GraphGenerator.GenerateRandomGraph(nodeCount, edgeCount, random);
                AddRootCluster(graph);
                SetRandomNodeShapes(graph, random);

                Layout(graph, random);
                //DisplayGeometryGraph.ShowGraph(graph);

                RouteEdges(graph, 5 * random.NextDouble());
                //DisplayGeometryGraph.ShowGraph(graph);
            }
        }
        [Timeout(TestTimeout.Infinite)]
        [TestMethod]
        [Description("Random graph with groups")]
        public void RouteEdges_SmallGroups()
        {
            RsmContent();
        }


        public static void RsmContent() {
            const int ntest = 70;
            int iStart = 1;            
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            for (int i = iStart; i < ntest; i++) {
                Random random = new Random(i);
                int multiplier = random.Next() % 5 + 1;
                GeometryGraph graph = GenerateGraphWithGroups(random, multiplier);
                SetRandomNodeShapes(graph, random);
                Layout(graph, random);
                // DisplayGeometryGraph.ShowGraph(graph);

                double edgeSeparation = 5 * random.NextDouble();
                System.Diagnostics.Debug.WriteLine("i={0} es={1}", i, edgeSeparation);
                // GeometryGraphWriter.Write(graph, "c:\\tmp\\graph");
                RouteEdges(graph, edgeSeparation);
                // DisplayGeometryGraph.ShowGraph(graph);

                //TODO: try to move it
            }
        }
        

        static GeometryGraph GenerateGraphWithGroups(Random random, int multiplier)
        {
            int clusterCount = (2 + random.Next(5))*multiplier;
            int nodeCount = (clusterCount + random.Next(20))*multiplier;
            int edgeCount = (10 + random.Next(20))*multiplier;

            //tree of clusters
            var parent = GenerateClusterTree(clusterCount, random);
            GeometryGraph graph = new GeometryGraph();

            //create nodes
            for (int i = 0; i < nodeCount; i++) {
                Node node = GraphGenerator.CreateNode(i);
                graph.Nodes.Add(node);
            }

            //create clusters
            var clusters = new Cluster[clusterCount];
            for (int i = 0; i < clusterCount; i++) {
                clusters[i] = new Cluster();
                clusters[i].BoundaryCurve = CurveFactory.CreateRectangle(30, 30, new Point(15, 15));
                clusters[i].RectangularBoundary = new RectangularClusterBoundary { LeftMargin = 5, RightMargin = 5, BottomMargin = 5, TopMargin = 5 };
            }

            //set cluster hiearchy
            graph.RootCluster = clusters[0];
            for (int i = 1; i < clusterCount; i++) {
                clusters[parent[i]].AddChild(clusters[i]);
            }

            //put nodes to clusters
            for (int i = 0; i < nodeCount; i++) {
                clusters[random.Next(clusterCount)].AddChild(graph.Nodes[i]);
            }

            for (int i = 0; i < clusterCount; i++) {
                if (clusters[i].Nodes.Count() == 0 && clusters[i].Clusters.Count() == 0) {
                    Node node = GraphGenerator.CreateNode(i);
                    graph.Nodes.Add(node);
                    clusters[i].AddChild(node);
                    nodeCount++;
                }
            }

            //adding edges
            for (int i = 0; i < edgeCount; i++) {
                int s = random.Next(nodeCount + clusterCount - 1);
                Node snode = (s < nodeCount ? graph.Nodes[s] : clusters[s - nodeCount + 1]);
                int t = random.Next(nodeCount + clusterCount - 1);
                Node tnode = (t < nodeCount ? graph.Nodes[t] : clusters[t - nodeCount + 1]);

                if (EdgeIsValid(snode, tnode)) {
                    var edge = new Edge(snode, tnode);
                    edge.LineWidth = 0.5 + 3 * random.NextDouble();
                    graph.Edges.Add(edge);
                }
                else
                    i--;
            }

            SetupPorts(graph);
            return graph;
        }
        static void FixHookPorts(GeometryGraph geometryGraph)
        {
            foreach (var edge in geometryGraph.Edges)
            {
                var s = edge.Source;
                var t = edge.Target;
                var sc = s as Cluster;
                if (sc != null && Ancestor(sc, t))
                {
                    edge.SourcePort = new HookUpAnywhereFromInsidePort(() => s.BoundaryCurve);
                }
                else
                {
                    var tc = t as Cluster;
                    if (tc != null && Ancestor(tc, s))
                    {
                        edge.TargetPort = new HookUpAnywhereFromInsidePort(() => t.BoundaryCurve);
                    }
                }
            }
        }

        static bool Ancestor(Cluster root, Node node)
        {
            if (node.ClusterParent == root)
                return true;
            return node.AllClusterAncestors.Any(p => p.ClusterParent == root);
        }
        
        static bool EdgeIsValid(Node snode, Node tnode)
        {
            if (snode == tnode) return false;

            if (snode.ClusterParent == tnode.ClusterParent)
                return true;
            /*if (!(snode is Cluster) && snode.AllClusterAncestors.Contains(tnode))
                return true;
            if (!(tnode is Cluster) && tnode.AllClusterAncestors.Contains(snode))
                return true;*/
            return false;
        }

        
        static int[] GenerateClusterTree(int nc, Random random)
        {
            int[] parents = new int[nc];
            for (int i = 1; i < nc; i++) {
                parents[i] = random.Next(i);
            }

            return parents;
        }

        static void RouteEdges(GeometryGraph graph, double edgeSeparation)
        {
            var bs = new BundlingSettings();
            bs.EdgeSeparation = edgeSeparation;
            var br = new SplineRouter(graph, 0.25, 10, Math.PI / 6, bs);
            br.Run();
        }

        static void Layout(GeometryGraph graph, Random rnd)
        {
            if (rnd.Next() % 5 < 3){ 
                var settings = new SugiyamaLayoutSettings();
                var layout = new InitialLayoutByCluster(graph, settings) {RunInParallel = false};
                layout.Run();
            }
            else {
                var settings = new FastIncrementalLayoutSettings { AvoidOverlaps = true };
                var layout = new InitialLayoutByCluster(graph, settings);
                layout.Run();
            }
        }

        void AddRootCluster(GeometryGraph graph) {
            var c = new Cluster(graph.Nodes);
            graph.RootCluster = c;
        }

        /// <summary>
        /// Update node shape with randomly selected boundary curve
        /// </summary>        
        static void SetRandomNodeShapes(GeometryGraph graph, Random random) {
            if (graph == null) {
                return;
            }

            foreach (Node node in graph.Nodes) {
                node.BoundaryCurve = GetRandomShape(random);
            }
        }

        static ICurve GetRandomShape(Random random)
        {
            //we support rectangle, roundedRectangle, circle, ellipse, diamond, Octagon, triangle, star            
            int index = random.Next(3);
            var center = new Microsoft.Msagl.Core.Geometry.Point();
            switch (index) {
                case 0:
                    return CurveFactory.CreateRectangle(20 + random.Next(10), 10 + random.Next(10), center);
                case 1:
                    return CurveFactory.CreateRectangleWithRoundedCorners(30 + random.Next(10), 20 + random.Next(10), 1 + random.Next(8), 1 + random.Next(8), center);
                case 2:
                    return CurveFactory.CreateEllipse(26, 18, center);
            }

            return null;
        }

    }
}
