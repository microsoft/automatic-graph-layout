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
