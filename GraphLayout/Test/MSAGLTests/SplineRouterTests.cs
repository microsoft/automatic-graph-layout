//-----------------------------------------------------------------------
// <copyright file="SplineRouterTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if TEST_MSAGL
using Microsoft.Msagl.GraphViewerGdi;
#endif
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using Microsoft.Msagl.Routing.Spline.Bundling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Test class for methods inside EdgeRouterHelper
    /// </summary>
    [TestClass]
    [DeploymentItem(@"Resources\MSAGLGeometryGraphs")]
    public class SplineRouterTests : MsaglTestBase
    {
        [TestMethod]
        [Description("Verifies that RouteEdges calls progress changed.")]
        public void RouteEdges_CallsProgress()
        {
            //DisplayGeometryGraph.SetShowFunctions();
            GeometryGraph graph = GraphGenerator.GenerateTree(10);
            double ratioComplete = 0;
            EventHandler<ProgressChangedEventArgs> handler = (s, e) => ratioComplete = e.RatioComplete;
            SplineRouter splineRouter = null;
            try
            {
                splineRouter = new SplineRouter(graph, 10, 1, Math.PI / 6, null);
                splineRouter.ProgressChanged += handler;
                splineRouter.Run();
             //   DisplayGeometryGraph.ShowGraph(graph);
            }
            finally
            {
                splineRouter.ProgressChanged -= handler;
            }

            Assert.AreEqual(1, ratioComplete, "RouteEdges did not complete");
        }

        [TestMethod]
        [WorkItem(571435)]
        [Description("Runs spline routing on a relatively small graph that has caused problems in the past.")]
        public void RouteEdges_SplineRoutingRegressionBug20110127()
        {
            double tightPadding = 1;
            double loosePadding = 20;
            double coneAngle = Math.PI / 6;
            GeometryGraph graph = LoadGraph("RoutingRegressionBug20110127.msagl.geom");
            SplineRouter splineRouter = new SplineRouter(graph, tightPadding, loosePadding, coneAngle, null);
            splineRouter.Run();
            //DisplayGeometryGraph.ShowGraph(graph);
            CheckEdgesForOverlapWithNodes(tightPadding, graph);
        }

        [TestMethod]
        [WorkItem(446802)]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        [Timeout(30 * 1000)]
        [Description("Simple timed test of routing with 20 degree cones over a large graph")]
        public void RouteEdges_SplineRouting1138Bus()
        {
            var g = LoadGraph("GeometryGraph_1138bus.msagl.geom");
            var sw = new Stopwatch();
            sw.Start();
            var loosePadding = SplineRouter.ComputeLooseSplinePadding(10, 2);
            SplineRouter splineRouter = new SplineRouter(g, 2, loosePadding, Math.PI / 6, null);
            splineRouter.Run();

            sw.Stop();
            System.Diagnostics.Debug.WriteLine("Edge routing took: {0} seconds.", sw.ElapsedMilliseconds / 1000.0);
        }

        [TestMethod]
        [WorkItem(448382)]
        [WorkItem(446802)]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        // [Timeout(30 * 1000)]
        [Description("Simple timed test of routing with 20 degree cones over a large graph")]
        public void RouteEdges_Nodes50()
        {
            var g = LoadGraph("nodes50.msagl.geom");
            var sw = new Stopwatch();
            sw.Start();
            const double TightPadding = 2.0;
            var loosePadding = SplineRouter.ComputeLooseSplinePadding(10, TightPadding);
            SplineRouter splineRouter = new SplineRouter(g, TightPadding, loosePadding, Math.PI / 6, null);
            splineRouter.Run();
            sw.Stop();
            System.Diagnostics.Debug.WriteLine("Edge routing took: {0} seconds.", sw.ElapsedMilliseconds / 1000.0);
            CheckEdgesForOverlapWithNodes(TightPadding, g);
        }

        private static void CheckEdgesForOverlapWithNodes(double tightPadding, GeometryGraph graph)
        {
#if TEST_MSAGL
            if (!DontShowTheDebugViewer())
            {
                DisplayGeometryGraph.SetShowFunctions();
            }
#endif
            foreach (var e in graph.Edges.Where(e=>!MultiEdge(e)))//avoid checking multi-edges since they routed as bundles and can slightly go over the nodes
            {
                Assert.IsNotNull(e.EdgeGeometry, "EdgeGeometry is null");
                Assert.IsNotNull(e.EdgeGeometry.Curve, "EdgeGeometry.Curve is null");
                foreach (var v in graph.Nodes)
                {
                    if (v == e.Source || v == e.Target)
                    {
                        continue;
                    }
                    var box = v.BoundingBox;
                    var poly = InteractiveObstacleCalculator.CreatePaddedPolyline(Curve.PolylineAroundClosedCurve(v.BoundaryCurve), tightPadding / 2);
                    bool overlaps = CurveOverlapsBox(e.EdgeGeometry.Curve, ref box, poly);
#if TEST_MSAGL
    //uncomment to see the graph and the overlaps  
                    if (overlaps && !DontShowTheDebugViewer())
                    {
                        LayoutAlgorithmSettings.ShowGraph(graph);
                        LayoutAlgorithmSettings.Show(poly, e.Curve);
                    }
#endif
                    Assert.IsFalse(overlaps);
                }
            }
        }


        static bool CurveOverlapsBox(ICurve curve, ref Rectangle box, Polyline boxPolyline) {
            // if the curve bounding box doesn't intersect the invalidated region then no overlap!
            if (!box.Intersects(curve.BoundingBox)) {
                return false;
            }

            // if either end of the curve is inside the box then there is definitely overlap!
            if (box.Contains(curve.Start) || box.Contains(curve.End)) {
                return true;
            }

            // we have already determined that the curve is close but not fully contained so now we
            // need to check for a more expensive intersection
            return Curve.CurveCurveIntersectionOne(boxPolyline, curve, false) != null;
        }

        private static bool MultiEdge(Edge edge)
        {
            return edge.Source.OutEdges.Where(e => e.Target == edge.Target && e != edge).Any() || edge.Source.InEdges.Where(e => e.Source == edge.Target && e != edge).Any();
        }

        

        [TestMethod]
        [WorkItem(446802)]
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        [Timeout(20000000)]
        [Description("Simple timed test of routing with 20 degree cones over a small complete graph")]
        public void SplineRoutingSmallCompleteGraph()
        {
            //DisplayGeometryGraph.SetShowFunctions();
            var g = LoadGraph("K20.msagl.geom");
            var sw = new Stopwatch();
            sw.Start();
            var loosePadding = SplineRouter.ComputeLooseSplinePadding(10, 2);
            SplineRouter splineRouter = new SplineRouter(g, 2, loosePadding, Math.PI / 6, null); 
            splineRouter.Run();
            sw.Stop();
            TestContext.WriteLine("Edge routing took: {0} seconds.", sw.ElapsedMilliseconds / 1000.0);
        }
        
        [TestMethod]
        [Description("the run does not stop")]
        public void BundlingBug1GeomGraph() {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            var graph = GeometryGraphReader.CreateFromFile(GetGeomGraphFileName("bug1.msagl.geom"));
            var settings = new BundlingSettings();
            var router = new SplineRouter(graph, 0.1, 0.75, Math.PI / 6, settings);
            router.Run();
        }

        [TestMethod]
        [Description("the run does not stop")]
        public void ComplexGeomGraph() {
            var graph = GeometryGraphReader.CreateFromFile(GetGeomGraphFileName("complex.msagl.geom"));
            var settings = new BundlingSettings();
            var br = new SplineRouter(graph, 1, 1.5, Math.PI / 6, settings);
            br.Run();
        }

        [TestMethod]
        [Description("bundling with groups")]
        public void BundlingWithGroups() {
            var graph = GeometryGraphReader.CreateFromFile(GetGeomGraphFileName("graphWithGroups.msagl.geom"));
            var settings = new BundlingSettings();
            var router = new SplineRouter(graph, 3, 1, Math.PI / 6, settings);
            router.Run();
        }

        
        string GetGeomGraphFileName(string graphName) {
            var dirName = (null != this.TestContext) ? this.TestContext.DeploymentDirectory : Path.GetTempPath();
            return Path.Combine(dirName, graphName);
        }
        [TestMethod]
        [Description("Simple test of rectilinear routing between two nodes, one inside a cluster")]
        public void SimpleClusterGraphRectilinear()
        {
            var g = new GeometryGraph();
            var node0 = new Node(CurveFactory.CreateRectangle(10, 10, new Point()), 0);
            var node1 = new Node(CurveFactory.CreateRectangle(10, 10, new Point()), 1);
            var cluster = new Cluster(new[] { node1 });
            cluster.UserData = 2; 
            cluster.BoundaryCurve = CurveFactory.CreateRectangle(10, 10, new Point());
            var edge = new Edge(node0, node1) { Length = 100 };
            g.Nodes.Add(node0);
            g.Nodes.Add(node1);
            g.Edges.Add(edge);
            var cluster2 = new Cluster(new[] { node0 }, new[] { cluster });
            cluster2.UserData = 3;
            cluster2.BoundaryCurve = CurveFactory.CreateRectangle(10, 10, new Point());
            g.RootCluster = cluster2;
            InitialLayout initialLayout = new InitialLayout(g, new FastIncrementalLayoutSettings() { AvoidOverlaps = true });
            initialLayout.Run();
            
            RectilinearEdgeRouter router = new RectilinearEdgeRouter(g, 1, 1, false);
            router.Run();
            EnableDebugViewer();
            ShowGraphInDebugViewer(g);
            var bb0 = node0.BoundingBox;
            bb0.Pad(1);
            Assert.IsTrue(bb0.Contains(edge.EdgeGeometry.Curve.Start));
            var bb1 = node1.BoundingBox;
            bb1.Pad(1);
            Assert.IsTrue(bb1.Contains(edge.EdgeGeometry.Curve.End));
        }

        [WorkItem(535708)]
        [TestMethod]
        [TestCategory("Rectilinear routing")]
        [TestCategory("NonRollingBuildTest")]
        [Timeout(5000)]
        [Description("Create three groups, a couple of embedded and see if the routing succeeds")]
        public void RoutingWithThreeGroups()
        {
            var graph = LoadGraph("abstract.msagl.geom");
            var root = graph.RootCluster;
            var a = new Cluster { UserData = "a" };
            foreach (string id in new[] { "17", "39", "13", "19", "28", "12" })
                a.AddChild(graph.FindNodeByUserData(id));

            var b = new Cluster { UserData = "b" };
            b.AddChild(a);
            b.AddChild(graph.FindNodeByUserData("18"));
            root.AddChild(b);

            var c = new Cluster { UserData = "c" };
            foreach (string id in new[] { "30", "5", "6", "7", "8" }) {
                var n = graph.FindNodeByUserData(id);
                if (n != null) 
                    c.AddChild(n);
            }
            root.AddChild(c);

            var clusterNodes = new Set<Node>(root.AllClustersDepthFirst().SelectMany(cl => cl.Nodes));
            foreach (var node in graph.Nodes.Where(n => clusterNodes.Contains(n) == false))
                root.AddChild(node);

            FixClusterBoundariesWithNoRectBoundaries(root, 5);
            var defaultSettings = new FastIncrementalLayoutSettings();
            var rootSettings = new FastIncrementalLayoutSettings() { AvoidOverlaps = true };

            var initialLayout = new InitialLayoutByCluster(graph, new[] { graph.RootCluster }, cl => cl == root ? rootSettings : defaultSettings);
            initialLayout.Run();
            
            const double Padding = 5;

            SplineRouter splineRouter = new SplineRouter(graph, Padding/3, Padding, Math.PI / 6);
            splineRouter.Run();
#if TEST_MSAGL
            if (!DontShowTheDebugViewer())
            {
                graph.UpdateBoundingBox();
                DisplayGeometryGraph.ShowGraph(graph);                
            }
#endif
            
        }

        static void FixClusterBoundariesWithNoRectBoundaries(Cluster cluster, double padding) {
            foreach (Cluster cl in cluster.Clusters)
                FixClusterBoundariesWithNoRectBoundaries(cl, padding);

            var box = Rectangle.CreateAnEmptyBox();

            var clusterPoints =
                    cluster.Clusters.SelectMany(c => ClusterPoints(c)).Concat(
                        cluster.Nodes.SelectMany(n => NodePerimeterPoints(n)));
            foreach (var clusterPoint in clusterPoints)
                box.Add(clusterPoint);

            box.Pad(padding);
            cluster.BoundaryCurve = box.Perimeter();
            cluster.RectangularBoundary = new RectangularClusterBoundary();
        }

        static IEnumerable<Point> NodePerimeterPoints(Node node) {
            return new[] {
                             node.BoundingBox.RightTop, node.BoundingBox.LeftBottom, node.BoundingBox.LeftTop,
                             node.BoundingBox.RightBottom
                         };
        }
        static IEnumerable<Point> ClusterPoints(Cluster cluster) {
            return cluster.BoundaryCurve as Polyline;
        }
    }
}
