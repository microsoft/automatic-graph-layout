//-----------------------------------------------------------------------
// <copyright file="AspectRatioTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Test class for manipulating the aspect ratio of sugiyama style graphs.
    /// </summary>
    [TestClass]
    [DeploymentItem(@"Resources\DotFiles\LevFiles\chat.dot", "Dots")]
    public class AspectRatioTests : MsaglTestBase
    {
        
        [TestMethod]
        [Description("Verifies that setting the aspect ratio to be very wide results in a wide graph.")]
        public void WideRatioSimpleStretch()
        {
            // Setup
            string filePath = Path.Combine(this.TestContext.TestDir, "Out\\Dots", "chat.dot");
            GeometryGraph graph = this.LoadGraph(filePath);
            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();
            settings.AspectRatio = 10.0;
        
            // Execute
            LayeredLayout layeredLayout = new LayeredLayout(graph, settings);
            layeredLayout.Run();

            // Verify
            VerifyAspectRatio(graph, settings.AspectRatio);
        }

        [TestMethod]
        [Description("Verifies that setting the aspect ratio to be very tall results in a tall graph.")]
        public void TallRatioSimpleStretch()
        {
            // Setup
            string filePath = Path.Combine(this.TestContext.TestDir, "Out\\Dots", "chat.dot");
            GeometryGraph graph = this.LoadGraph(filePath);
            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();
            settings.AspectRatio = 0.25;
        
            // Execute
            LayeredLayout layeredLayout = new LayeredLayout(graph, settings);
            layeredLayout.Run();

            // Verify
            VerifyAspectRatio(graph, settings.AspectRatio);
        }

        /// <summary>
        /// Verifies that the graph conforms to the correct aspect ratio.
        /// </summary>
        private static void VerifyAspectRatio(GeometryGraph graph, double aspectRatio)
        {
            double ratioTolerance = aspectRatio * 0.2;

            // Verify the graph is the correct size
            Assert.AreEqual(aspectRatio, (graph.Width-(graph.Margins*2)) / (graph.Height-(graph.Margins*2)), ratioTolerance, "The graph does not conform to the aspect ratio.");

            // Verify the nodes were spread apart to fill the space
            IEnumerable<Rectangle> nodeBoxes = graph.Nodes.Select(n => n.BoundingBox);
            IEnumerable<Rectangle> edgeBoxes = graph.Edges.Select(e => e.BoundingBox);
            IEnumerable<Rectangle> labelBoxes = graph.CollectAllLabels().Select(l => l.BoundingBox);
            
            Rectangle itemBounds = new Rectangle(nodeBoxes.Concat(edgeBoxes).Concat(labelBoxes));
            Assert.AreEqual(aspectRatio, itemBounds.Width / itemBounds.Height, ratioTolerance, "The graph's nodes do not conform to the aspect ratio.");
        }
    }
}
