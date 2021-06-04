//-----------------------------------------------------------------------
// <copyright file="SugiyamaLayoutTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Dot2Graph;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests 
{
    [TestClass]
    public class SugiyamaLayoutTests : MsaglTestBase
    {
        private enum LayerDirection
        {
            /// <summary>
            /// top to bottom
            /// </summary>
            TopToBottom,

            /// <summary>
            /// left to right
            /// </summary>
            LeftToRight,
            
            /// <summary>
            /// bottom top
            /// </summary>
            BottomToTop,
            
            /// <summary>
            /// right to left
            /// </summary>
            RightToLeft,

            /// <summary>
            /// Default option
            /// </summary>
            None
        }
        private const int Seed = 999;
        private Random random;

        [TestInitialize]
        public override void Initialize()
        {
            EnableDebugViewer();
            random = new Random(Seed);
            base.Initialize();

        }

        [TestCleanup]
        public override void Cleanup()
        {
            base.Cleanup();
        }

        #region Test
        [TestMethod]
        [Description("Randomly selects some DOT files and do Sugiyam layout testing")]
        [DeploymentItem(@"Resources\DotFiles\LevFiles", "Dots")]
        public void RandomDotFileTests() {
            int line, column;
            string msg;

            string fileName = Path.Combine(this.TestContext.TestDir, "Out\\Dots\\fsm.dot");
            Drawing.Graph drawGraph = Parser.Parse(fileName, out line, out column, out msg);
            drawGraph.CreateGeometryGraph();
            GeometryGraph graph = drawGraph.GeometryGraph;
            GraphGenerator.SetRandomNodeShapes(graph, random);
            LayeredLayout layeredLayout = new LayeredLayout(graph, new SugiyamaLayoutSettings() { BrandesThreshold = 1 });
            layeredLayout.Run();
            string[] allFiles = Directory.GetFiles(Path.Combine(this.TestContext.TestDir, "Out\\Dots"), "*.dot");
            List<int> selected = new List<int>();

            for (int i = 0; i < 10; i++)
            {
                int next = random.Next(allFiles.Length);
                while (selected.Contains(next))
                {
                    next = random.Next(allFiles.Length);
                }
                selected.Add(next);
                WriteLine("Now handling dot file: " + allFiles[next]);
                drawGraph = Parser.Parse(allFiles[next], out line, out column, out msg);
                drawGraph.CreateGeometryGraph();
                graph = drawGraph.GeometryGraph;
                GraphGenerator.SetRandomNodeShapes(graph, random);

                LayerDirection direction = LayerDirection.None;
                switch (i % 4)
                {
                    case 0:
                        direction = LayerDirection.TopToBottom;
                        break;
                    case 1:
                        direction = LayerDirection.BottomToTop;
                        break;
                    case 2:
                        direction = LayerDirection.LeftToRight;
                        break;
                    case 3:
                        direction = LayerDirection.RightToLeft;
                        break;
                }
                LayoutAndValidate(graph, (SugiyamaLayoutSettings)drawGraph.LayoutAlgorithmSettings, direction);
            }
        }

        [TestMethod]
        [Description("Generate one simple graph and do Sugiyam layout testing")]
        public void OnlyNodes() {
            // GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
            var graph = new GeometryGraph();
            graph.Nodes.Add(new Node() { BoundaryCurve = CurveFactory.CreateCircle(80, new Point(0, 0)) });
            graph.Nodes.Add(new Node() { BoundaryCurve = CurveFactory.CreateCircle(20, new Point(0, 0)) });
            graph.Nodes.Add(new Node() { BoundaryCurve = CurveFactory.CreateCircle(100, new Point(0, 0)) });
            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();
            WriteLine("Trying nodes only graph with ");
            LayoutAndValidate(graph, settings);
            
        }

        [TestMethod]
        [Description("Generate one simple graph and do Sugiyam layout testing")]
        public void SimpleGraphTests()
        {
            GeometryGraph graph = GraphGenerator.GenerateOneSimpleGraph();
            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();
            GraphGenerator.SetRandomNodeShapes(graph, random);
            WriteLine("Trying Simple Graph with Top to Down layer direction");
            LayoutAndValidate(graph, settings);
            GraphGenerator.SetRandomNodeShapes(graph, random);
            WriteLine("Trying Simple Graph with Left to Right layer direction");
            LayoutAndValidate(graph, settings, LayerDirection.LeftToRight);
        }

        [TestMethod]
        [Description("Generate one tree graph and do Sugiyam layout testing")]
        public void TreeGraphTests()
        {
            GeometryGraph graph = GraphGenerator.GenerateFullTree(10, 3);
            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();
            GraphGenerator.SetRandomNodeShapes(graph, random);
            WriteLine("Trying Tree Graph with Right to Left layer direction");
            LayoutAndValidate(graph, settings, LayerDirection.RightToLeft);
            GraphGenerator.SetRandomNodeShapes(graph, random);
            WriteLine("Trying Tree Graph with Bottom to Top layer direction");
            LayoutAndValidate(graph, settings, LayerDirection.BottomToTop);
        }

        [TestMethod]
        [Description("Generate one circle graph and do Sugiyam layout testing")]
        public void CircleGraphTests()
        {
            GeometryGraph graph = GraphGenerator.GenerateCircle(10);
            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();
            GraphGenerator.SetRandomNodeShapes(graph, random);
            WriteLine("Trying Circle Graph with Top to Down layer direction");
            LayoutAndValidate(graph, settings, 18, 10);
        }

        [TestMethod]
        [Description("Generate one disjoint graph and do Sugiyam layout testing")]
        public void DisjointGraphTests()
        {
            GeometryGraph graph = GraphGenerator.GenerateGraphWithSameSubgraphs(3, 6);
            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();
            GraphGenerator.SetRandomNodeShapes(graph, random);
            WriteLine("Trying Disjoint Graph with Left To Right layer direction");
            LayoutAndValidate(graph, settings, 32, 25, LayerDirection.LeftToRight);
        }

        [TestMethod]
        [Description("Generate one lattice graph and do Sugiyam layout testing")]
        public void LatticeGraphTests()
        {
            GeometryGraph graph = GraphGenerator.GenerateSquareLattice(20);
            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();
            GraphGenerator.SetRandomNodeShapes(graph, random);
            WriteLine("Trying Lattice Graph with Top to Down layer direction");
            LayoutAndValidate(graph, settings);
        }

        [TestMethod]
        [Description("Generate one chain graph and do Sugiyam layout testing")]
        public void ChainGraphTests()
        {
            GeometryGraph graph = GraphGenerator.GenerateSimpleChain(10);
            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();
            GraphGenerator.SetRandomNodeShapes(graph, random);
            WriteLine("Trying Chain Graph with Bottom to Top layer direction");
            LayoutAndValidate(graph, settings, LayerDirection.BottomToTop);
            GraphGenerator.SetRandomNodeShapes(graph, random);
            WriteLine("Trying Chain Graph with Left to Right layer direction");
            LayoutAndValidate(graph, settings, LayerDirection.LeftToRight);
        }

        [TestMethod]
        [Description("Generate one fully connected graph and do Sugiyam layout testing")]
        public void FullyConnectedGraphTests()
        {
            GeometryGraph graph = GraphGenerator.GenerateFullyConnectedGraph(20);
            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();
            GraphGenerator.SetRandomNodeShapes(graph, random);
            WriteLine("Trying Fully Connected Graph with Bottom to Top layer direction");
            LayoutAndValidate(graph, settings, LayerDirection.BottomToTop);
            GraphGenerator.SetRandomNodeShapes(graph, random);
            WriteLine("Trying Fully Connected Graph with Left to Right layer direction");
            LayoutAndValidate(graph, settings, LayerDirection.LeftToRight);
        }

        #endregion

        #region helpers
        private static void LayoutAndValidate(GeometryGraph graph, SugiyamaLayoutSettings settings)
        {
            LayoutAndValidate(graph, settings, LayerDirection.TopToBottom);
        }

        private static void LayoutAndValidate(GeometryGraph graph, SugiyamaLayoutSettings settings, LayerDirection direction)
        {
            LayoutAndValidate(graph, settings, settings.NodeSeparation, settings.LayerSeparation, direction);
        }

        private static void LayoutAndValidate(GeometryGraph graph, SugiyamaLayoutSettings settings, double nodeSeparation, double layerSeparation)
        {
            LayoutAndValidate(graph, settings, nodeSeparation, layerSeparation, LayerDirection.TopToBottom);
        }

        private static void LayoutAndValidate(GeometryGraph graph, SugiyamaLayoutSettings settings, double nodeSeparation, double layerSeparation, LayerDirection direction)
        {
            settings.NodeSeparation = nodeSeparation;
            
            switch (direction)
            {
                case LayerDirection.None:
                case LayerDirection.TopToBottom:                    
                    break;
                case LayerDirection.BottomToTop:
                    settings.Transformation = PlaneTransformation.Rotation(Math.PI);
                    break;
                case LayerDirection.LeftToRight:
                    settings.Transformation = PlaneTransformation.Rotation(Math.PI / 2);
                    break;
                case LayerDirection.RightToLeft:
                    settings.Transformation = PlaneTransformation.Rotation(-Math.PI / 2);
                    break;
            }

            settings.LayerSeparation = layerSeparation;
            LayeredLayout layeredLayout = new LayeredLayout(graph, settings);
            layeredLayout.Run();


            ShowGraphInDebugViewer(graph);

//            SugiyamaValidation.ValidateGraph(graph, settings);
        }
        #endregion
    }
}
