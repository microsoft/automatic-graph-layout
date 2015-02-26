/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿//-----------------------------------------------------------------------
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
            double ratioTolerance = aspectRatio * 0.05;

            // Verify the graph is the correct size
            Assert.AreEqual(aspectRatio, graph.Width / graph.Height, ratioTolerance, "The graph does not conform to the aspect ratio.");

            // Verify the nodes were spread apart to fill the space
            IEnumerable<Rectangle> nodeBoxes = graph.Nodes.Select(n => n.BoundingBox);
            IEnumerable<Rectangle> edgeBoxes = graph.Edges.Select(e => e.BoundingBox);
            IEnumerable<Rectangle> labelBoxes = graph.CollectAllLabels().Select(l => l.BoundingBox);
            
            Rectangle itemBounds = new Rectangle(nodeBoxes.Concat(edgeBoxes).Concat(labelBoxes));
            Assert.AreEqual(aspectRatio, itemBounds.Width / itemBounds.Height, ratioTolerance, "The graph's nodes do not conform to the aspect ratio.");
        }
    }
}
