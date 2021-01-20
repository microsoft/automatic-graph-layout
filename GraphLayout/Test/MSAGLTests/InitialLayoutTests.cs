//-----------------------------------------------------------------------
// <copyright file="InitialLayoutTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Tests specific to the InitialLayout class.
    /// </summary>
    [TestClass]
    [DeploymentItem(@"Resources\MSAGLGeometryGraphs")]
    public class InitialLayoutTests : MsaglTestBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
        }

        [TestMethod]
        [Description("Verifies that CalculateLayout always reports 100% progress completed")]
        public void CalculateLayout_ReportsAllProgress()
        {
            GeometryGraph treeGraph = GraphGenerator.GenerateTree(20);
            var settings = new FastIncrementalLayoutSettings();
            InitialLayout layout = new InitialLayout(treeGraph, settings);
            
            double progress = 0.0;

            EventHandler<ProgressChangedEventArgs> handler = (s, e) => progress = e.RatioComplete;

            try
            {
                layout.ProgressChanged += handler;
                layout.Run();
            }
            finally
            {
                layout.ProgressChanged -= handler;
            }

            Assert.AreEqual(0, treeGraph.BoundingBox.Bottom);
            Assert.AreEqual(0, treeGraph.BoundingBox.Left);

            foreach (var v in treeGraph.Nodes)
            {
                Assert.IsTrue(treeGraph.BoundingBox.Contains(v.BoundingBox));
            }

            Assert.AreEqual(1.0, progress, "Progress was never reported as 100%.  Last update was at " + progress + "%");
        }

        [TestMethod]
        [Description("We have a simple calculation for the number of iterations of various pieces of layout that need to be applied for various size graphs.  We trade off quality against performance by applying more iterations for smaller graphs than larger graphs.")]
        public void CalculateIterationsForGraphSize()
        {
            const int LowerThreshold = 10;
            const int UpperThreshold = 50;
            const int MinIterations = 10;
            const int MaxIterations = 30;
            Assert.AreEqual(
                LayoutAlgorithmHelpers.NegativeLinearInterpolation(UpperThreshold + 1, LowerThreshold, UpperThreshold, MinIterations, MaxIterations), 
                MinIterations,
                "When nodeCount > upperThreshold number of iterations should be minIterations");
            Assert.AreEqual(
                LayoutAlgorithmHelpers.NegativeLinearInterpolation(UpperThreshold, LowerThreshold, UpperThreshold, MinIterations, MaxIterations),
                MinIterations,
                "When nodeCount == upperThreshold number of iterations should be minIterations");
            Assert.AreEqual(
                LayoutAlgorithmHelpers.NegativeLinearInterpolation(LowerThreshold - 1, LowerThreshold, UpperThreshold, MinIterations, MaxIterations),
                MaxIterations,
                "When nodeCount < lowerThreshold number of iterations should be maxIterations");
            Assert.AreEqual(
                LayoutAlgorithmHelpers.NegativeLinearInterpolation(LowerThreshold, LowerThreshold, UpperThreshold, MinIterations, MaxIterations),
                MaxIterations,
                "When nodeCount == lowerThreshold number of iterations should be maxIterations");
            Assert.AreEqual(
                LayoutAlgorithmHelpers.NegativeLinearInterpolation((UpperThreshold + LowerThreshold) / 2, LowerThreshold, UpperThreshold, MinIterations, MaxIterations),
                (MinIterations + MaxIterations) / 2,
                "When nodeCount is mid-way between lowerThreshold and upperThreshold number of iterations should be mid-way between minIterations and maxIterations");
        }

        [TestMethod]
        [Ignore]
        [Description("Verifies simple packing of a clustered graph (and checks progress for good measure)")]
        public void CalculateLayout_CorrectPacking()
        {
            var clusteredGraph = CreateClusteredGraph(padding: 1);
            var settings = new SugiyamaLayoutSettings { NodeSeparation = 5, PackingAspectRatio = 1.2, LayerSeparation = 5 };
            var layout = new InitialLayoutByCluster(clusteredGraph, settings);

            double progress = 0.0;
            EnableDebugViewer();

            EventHandler<ProgressChangedEventArgs> handler = (s, e) => progress = e.RatioComplete;

            try
            {
                layout.ProgressChanged += handler;
                layout.Run();
            }
            finally
            {
                layout.ProgressChanged -= handler;
            }

            double aspectRatio = clusteredGraph.BoundingBox.Width / clusteredGraph.BoundingBox.Height;

            //Assert.AreEqual(1.0, progress, "Progress was never reported as 100%.  Last update was at " + progress + "%");
            //var router = new SplineRouter(clusteredGraph, settings.Padding, settings.Padding/2.1, Math.PI/6);
            //router.Run();
            ShowGraphInDebugViewer(clusteredGraph);

            foreach (var e in clusteredGraph.Edges)
            {
                Assert.IsNotNull(e.Curve, "Edge was not routed");
            }
            // skinny aspect ratio specified above caused lots of vertical stacking in groups which actually leads
            // to a final aspect ratio close to 1
            Assert.AreEqual(settings.PackingAspectRatio, aspectRatio, 0.2, "Difference between actual and desired aspect ratios too large");
        }

        [TestMethod]
        [Description("Nested graph layout on a larger graph with layered layout")]
        public void BaseballLayeredTest()
        {

            LayoutAlgorithmSettings settings;
            var graph = LoadGraph("baseball.msagl.geom", out settings);
            settings = new SugiyamaLayoutSettings { NodeSeparation = 5, PackingAspectRatio = 1.2, LayerSeparation = 5 };
            var layout = new InitialLayoutByCluster(graph, settings);

            foreach (var c in graph.RootCluster.AllClustersDepthFirst().Where(c => c != graph.RootCluster))
            {
                c.RectangularBoundary = new RectangularClusterBoundary { LeftMargin = 5, RightMargin = 5, BottomMargin = 5, TopMargin = 5 };
                c.BoundaryCurve = CurveFactory.CreateRectangle(30, 30, new Point(15, 15));
            }

            double progress = 0.0;
            EnableDebugViewer();

            EventHandler<ProgressChangedEventArgs> handler = (s, e) => progress = e.RatioComplete;

            try
            {
                layout.ProgressChanged += handler;
                layout.Run();
            }
            finally
            {
                layout.ProgressChanged -= handler;
            }

            Assert.AreEqual(1.0, progress, "Progress was never reported as 100%.  Last update was at " + progress + "%");
            ShowGraphInDebugViewer(graph);

            foreach (var e in graph.Edges)
            {
                Assert.IsNotNull(e.Curve, "Edge was not routed");
            }
        }

        [TestMethod]
        [Description("Nested graph layout on a larger graph with layered layout")]
        public void GraphModelGroupedLayeredTest()
        {

            LayoutAlgorithmSettings settings;
            var graph = LoadGraph("GraphModelGrouped.msagl.geom", out settings);
            foreach (var e in graph.Edges)
            {
                e.Labels.Add(new Label(40, 10, e));
            }
            settings = new SugiyamaLayoutSettings { NodeSeparation = 5, PackingAspectRatio = 1.2, LayerSeparation = 5 };
            var layout = new InitialLayoutByCluster(graph, settings);

            foreach (var c in graph.RootCluster.AllClustersDepthFirst().Where(c => c != graph.RootCluster))
            {
                c.RectangularBoundary = new RectangularClusterBoundary { LeftMargin = 5, RightMargin = 5, BottomMargin = 5, TopMargin = 5 };
                c.BoundaryCurve = CurveFactory.CreateRectangle(30, 30, new Point(15, 15));
            }

            double progress = 0.0;
            EnableDebugViewer();

            EventHandler<ProgressChangedEventArgs> handler = (s, e) => progress = e.RatioComplete;

            try
            {
                layout.ProgressChanged += handler;
                layout.Run();
            }
            finally
            {
                layout.ProgressChanged -= handler;
            }

            Assert.AreEqual(1.0, progress, "Progress was never reported as 100%.  Last update was at " + progress + "%");
            ShowGraphInDebugViewer(graph);

            foreach (var e in graph.Edges)
            {
                Assert.IsNotNull(e.Curve, "Edge was not routed");
            }
        }

        [TestMethod]
        [Description("Nested graph layout on a larger graph with force directed layout and linear routing")]
        public void GraphModelGroupedForceDirectedRectilinearTest()
        {
            LayoutAlgorithmSettings settings;
            var graph = LoadGraph("GraphModelGrouped.msagl.geom", out settings);
            settings = new FastIncrementalLayoutSettings { NodeSeparation = 5, PackingAspectRatio = 1.2, AvoidOverlaps = true, GravityConstant = 0.5 };
            settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Rectilinear;
            settings.EdgeRoutingSettings.Padding = 2;
            settings.EdgeRoutingSettings.CornerRadius = 2;
            var layout = new InitialLayoutByCluster(graph, settings);

            foreach (var c in graph.RootCluster.AllClustersDepthFirst().Where(c => c != graph.RootCluster))
            {
                c.RectangularBoundary = new RectangularClusterBoundary { LeftMargin = 5, RightMargin = 5, BottomMargin = 5, TopMargin = 5 };
                c.BoundaryCurve = CurveFactory.CreateRectangle(30, 30, new Point(15, 15));
            }

            double progress = 0.0;
            EnableDebugViewer();

            EventHandler<ProgressChangedEventArgs> handler = (s, e) => progress = e.RatioComplete;

            try
            {
                layout.ProgressChanged += handler;
                layout.Run();
            }
            finally
            {
                layout.ProgressChanged -= handler;
            }

            Assert.IsTrue(1.0 <= progress, "Progress was never reported as 100%.  Last update was at " + progress + "%");
            ShowGraphInDebugViewer(graph);

            foreach (var e in graph.Edges)
            {
                Assert.IsNotNull(e.Curve, "Edge was not routed");
            }
        }

        [TestMethod]
        [Description("Nested graph layout on a larger graph with force directed and splines")]
        public void GraphModelGroupedForceDirectedSplineTest()
        {
            LayoutAlgorithmSettings settings;
            var graph = LoadGraph("GraphModelGrouped.msagl.geom", out settings);
            settings = new FastIncrementalLayoutSettings { NodeSeparation = 5, PackingAspectRatio = 1.2, AvoidOverlaps = true };
            settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Spline;
            settings.EdgeRoutingSettings.Padding = 2;
            var layout = new InitialLayoutByCluster(graph, settings);

            foreach (var c in graph.RootCluster.AllClustersDepthFirst().Where(c => c != graph.RootCluster))
            {
                c.RectangularBoundary = new RectangularClusterBoundary { LeftMargin = 5, RightMargin = 5, BottomMargin = 5, TopMargin = 5 };
                c.BoundaryCurve = CurveFactory.CreateRectangle(30, 30, new Point(15, 15));
            }

            double progress = 0.0;
            EnableDebugViewer();

            EventHandler<ProgressChangedEventArgs> handler = (s, e) => progress = e.RatioComplete;

            try
            {
                layout.ProgressChanged += handler;
                layout.Run();
            }
            finally
            {
                layout.ProgressChanged -= handler;
            }

	    // Ignore this assertion due to bug: 688960 - One MSAGL unit test is failing due to Parallel Linq which affects the Progress accounting.
            //Assert.AreEqual(1.0, progress, "Progress was never reported as 100%.  Last update was at " + progress + "%");

            ShowGraphInDebugViewer(graph);

            foreach (var e in graph.Edges)
            {
                Assert.IsNotNull(e.Curve, "Edge was not routed");
            }
        }

        [TestMethod]
        [Ignore]
        [Description("Applies layered layout by cluster, laying a nested subgraph out at a different orientation to the unnested groups")]
        public void LayeredLayoutOrientationByCluster()
        {
            var clusteredGraph = CreateClusteredGraph(padding: 1);
            foreach(var e in clusteredGraph.Edges)
            {
                e.Labels.Add(new Label(40,10,e));
            }
            var defaultSettings = new SugiyamaLayoutSettings { NodeSeparation = 5, PackingAspectRatio = 1.3, LayerSeparation = 5 };
            var settings = new Dictionary<Cluster, LayoutAlgorithmSettings>();
            foreach (var c in clusteredGraph.RootCluster.Clusters)
            {
                var localSettings = (SugiyamaLayoutSettings)defaultSettings.Clone();
                localSettings.Transformation = PlaneTransformation.Rotation(Math.PI / 2);
                settings[c] = localSettings;
            }
            var layout = new InitialLayoutByCluster(clusteredGraph, c => settings.ContainsKey(c) ? settings[c] : defaultSettings);

            double progress = 0.0;
            EnableDebugViewer();

            EventHandler<ProgressChangedEventArgs> handler = (s, e) => progress = e.RatioComplete;

            try
            {
                layout.ProgressChanged += handler;
                layout.Run();
            }
            finally
            {
                layout.ProgressChanged -= handler;
            }

            double aspectRatio = clusteredGraph.BoundingBox.Width / clusteredGraph.BoundingBox.Height;

            //Assert.AreEqual(1.0, progress, "Progress was never reported as 100%.  Last update was at " + progress + "%");
            //var router = new SplineRouter(clusteredGraph, settings.Padding, settings.Padding/2.1, Math.PI/6);
            //router.Run();
            ShowGraphInDebugViewer(clusteredGraph);

            // lots of components in this graph, it gets pretty close to the ideal aspect ratio
            Assert.AreEqual(defaultSettings.PackingAspectRatio, aspectRatio, 0.2, "Difference between actual and desired aspect ratios too large");

            foreach (var e in clusteredGraph.Edges)
            {
                if (e.Source.ClusterParent==clusteredGraph.RootCluster || e.Target.ClusterParent==clusteredGraph.RootCluster)
                {
                    Assert.IsTrue(e.Source.Center.Y > e.Target.Center.Y, "Top level edges should be vertical");
                }
                else
                {
                    Assert.IsTrue(e.Source.Center.X < e.Target.Center.X, "Nested edges should be horizontal");
                }
            }
        }

        [TestMethod]
        [Description("Verifies packing aspect ratio and progress")]
        public void TreeGraphMdsLayout()
        {
            GeometryGraph treeGraph = GraphGenerator.GenerateTree(20);
            treeGraph.RootCluster = new Cluster(treeGraph.Nodes);
            var settings = new MdsLayoutSettings { ScaleX = 1, ScaleY = 1, RemoveOverlaps = true, PackingAspectRatio = 1.4 };
            settings.EdgeRoutingSettings = new EdgeRoutingSettings { EdgeRoutingMode = EdgeRoutingMode.Spline, ConeAngle = Math.PI / 6, Padding = settings.NodeSeparation / 2.1 };
            foreach (var v in treeGraph.Nodes)
            {
                v.BoundingBox = new Rectangle(0, 0, new Point(30, 30));
            }
            var layout = new InitialLayoutByCluster(treeGraph, settings);

            double progress = 0.0;

            EventHandler<ProgressChangedEventArgs> handler = (s, e) => progress = e.RatioComplete;

            try
            {
                layout.ProgressChanged += handler;
                layout.Run();
            }
            finally
            {
                layout.ProgressChanged -= handler;
            }

            EnableDebugViewer();
            ShowGraphInDebugViewer(treeGraph);

            const double EdgeLengthDelta = 0.5;

            foreach (var e in treeGraph.Edges)
            {
                Assert.IsNotNull(e.Curve, "Edge curves not populated");
                if (e.Source != e.Target)
                {
                    double actualLength = (e.Source.Center - e.Target.Center).Length;
                    double actualDesiredRatio = e.Length / actualLength;
                    Assert.AreEqual(1, actualDesiredRatio, EdgeLengthDelta, "Edge length is not correct");
                }
            }

            double aspectRatio = treeGraph.BoundingBox.Width / treeGraph.BoundingBox.Height;
            Assert.AreEqual(settings.PackingAspectRatio, aspectRatio, 1.4, "Aspect ratio too far from desired");

            Assert.AreEqual(1.0, progress, "Progress was never reported as 100%.  Last update was at " + progress + "%");
        }


        [TestMethod]
        [Description("Verifies packing aspect ratio and progress")]
        public void TreeGraphFastIncrementalLayout()
        {
            GeometryGraph treeGraph = GraphGenerator.GenerateTree(20);
            treeGraph.RootCluster = new Cluster(treeGraph.Nodes);
            var settings = new FastIncrementalLayoutSettings { AvoidOverlaps = true, PackingAspectRatio = 1.6 };
            settings.EdgeRoutingSettings = new EdgeRoutingSettings { EdgeRoutingMode = EdgeRoutingMode.Spline, ConeAngle = Math.PI / 6, Padding = settings.NodeSeparation / 2.1 };
            foreach (var v in treeGraph.Nodes)
            {
                v.BoundingBox = new Rectangle(0, 0, new Point(30, 30));
            }
            foreach (var e in treeGraph.Edges)
            {
                e.EdgeGeometry.SourceArrowhead = new Arrowhead { Length = 4, Width = 4 };
                e.EdgeGeometry.TargetArrowhead = new Arrowhead { Length = 8, Width = 4 };
            }
            var layout = new InitialLayoutByCluster(treeGraph, settings);

            double progress = 0.0;

            EventHandler<ProgressChangedEventArgs> handler = (s, e) => progress = e.RatioComplete;

            try
            {
                layout.ProgressChanged += handler;
                layout.Run();
            }
            finally
            {
                layout.ProgressChanged -= handler;
            }

            EnableDebugViewer();
            ShowGraphInDebugViewer(treeGraph);

            const double EdgeLengthDelta = 0.5;

            foreach (var e in treeGraph.Edges)
            {
                Assert.IsNotNull(e.Curve, "Edge curves not populated");
                if (e.Source != e.Target)
                {
                    double actualLength = (e.Source.Center - e.Target.Center).Length;
                    double actualDesiredRatio = e.Length / actualLength;
                    Assert.AreEqual(1, actualDesiredRatio, EdgeLengthDelta, "Edge length is not correct");
                }
            }

            double aspectRatio = treeGraph.BoundingBox.Width / treeGraph.BoundingBox.Height;
            Assert.AreEqual(settings.PackingAspectRatio, aspectRatio, 0.2, "Aspect ratio too far from desired");

            Assert.AreEqual(1.0, progress, "Progress was never reported as 100%.  Last update was at " + progress + "%");
        }

        /// <summary>
        ///   o o   o
        /// ([o o]) o
        /// ( o o ) o
        /// </summary>
        [TestMethod]
        [Description("Clustered graph with 9 nodes, 2 clusters, 2 levels deep, no edges")]
        public void DenseClusteringNoEdges()
        {
            GeometryGraph graph = new GeometryGraph();
            const double Scale = 100;
            const double Margin = 5;
            for (int i = 0; i < 9; ++i)
            {
                graph.Nodes.Add(new Node(CurveFactory.CreateRectangle(Scale, Scale, new Point())));
            }
            Cluster innerCluster = CreateCluster(graph.Nodes.Take(2), Margin);
            Cluster outerCluster = CreateCluster(graph.Nodes.Take(4).Except(innerCluster.Nodes), Margin);
            outerCluster.AddChild(innerCluster);
            graph.RootCluster = new Cluster(graph.Nodes.Except(graph.Nodes.Take(4)));
            graph.RootCluster.AddChild(outerCluster);
            var initialLayout = new InitialLayoutByCluster(
                graph, new FastIncrementalLayoutSettings { NodeSeparation = Margin, ClusterMargin = Margin / 2, PackingAspectRatio = 1 });
            initialLayout.Run();

            EnableDebugViewer();
            ShowGraphInDebugViewer(graph);

            Assert.IsTrue(
                ApproximateComparer.Close(graph.BoundingBox.LeftBottom, new Point()), "Graph origin is not 0,0");
            Assert.AreEqual(340, graph.BoundingBox.Width, 0.1, "Width is incorrect");
            Assert.AreEqual(340, graph.BoundingBox.Height, 0.1, "Height is incorrect");

        }

        private static Cluster CreateCluster(IEnumerable<Node> nodes, double margin)
        {
            return new Cluster(nodes)
            {
                RectangularBoundary =
                    new RectangularClusterBoundary { LeftMargin = margin, RightMargin = margin, BottomMargin = margin, TopMargin = margin },
                BoundaryCurve = CurveFactory.CreateRectangle(1, 1, new Point())
            };
        }
    }
}
