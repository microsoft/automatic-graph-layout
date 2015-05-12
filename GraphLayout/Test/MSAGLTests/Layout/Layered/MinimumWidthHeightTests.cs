//-----------------------------------------------------------------------
// <copyright file="MinimumWidthHeightTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System.IO;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Test class for verifying that specified minimum sizes are respected in sugiyama layout.
    /// </summary>
    [TestClass]
    public class MinimumWidthHeightTests : MsaglTestBase
    {
        [TestMethod]
        [Description("Verifies that the specified minimum width and heights are respected.")]
        [DeploymentItem(@"Resources\DotFiles\LevFiles\chat.dot", "Dots")]
        public void MinimumSizeIsRespected()
        {
            // Setup
            string filePath = Path.Combine(this.TestContext.TestDir, "Out\\Dots", "chat.dot");
            GeometryGraph graph = this.LoadGraph(filePath);

            const double DesiredHeight = 100000;
            const double DesiredWidth = 100000;

            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();
            settings.MinimalHeight = DesiredHeight;
            settings.MinimalWidth = DesiredWidth;
            
            // Execute
            LayeredLayout layeredLayout = new LayeredLayout(graph, settings);
            layeredLayout.Run();

            // Verify the graph is the correct size
            Assert.IsTrue(DesiredHeight < graph.Height, "Graph height should be the minimal height.");
            Assert.IsTrue(DesiredWidth < graph.Width, "Graph width should be the minimal width.");

            // Verify the nodes were spread apart to fill the space
            Rectangle nodeBounds = new Rectangle(graph.Nodes.Select(n => n.BoundingBox));
            Assert.IsTrue(DesiredWidth < nodeBounds.Height, "The graph nodes weren't scaled vertically to fill the space.");
            Assert.IsTrue(DesiredWidth < nodeBounds.Width, "The graph nodes weren't scaled horizontally to fill the space.");
        }
    }
}
