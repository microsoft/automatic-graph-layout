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
// <copyright file="IncrementalSugiyamaTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Verifies that incremental sugiyama layout works correctly.
    /// </summary>
    [TestClass]
    [DeploymentItem(@"Resources\DotFiles\LevFiles\chat.dot", "Dots")]
    public class IncrementalSugiyamaTests : MsaglTestBase
    {
        [TestMethod]
        [Description("Verifies that updating shapes and calling incremental sugiyama does not affect the ordering of nodes and layers.")]
        public void NodeShapeChange()
        {
            // Setup
            string filePath = Path.Combine(this.TestContext.TestDir, "Out\\Dots", "chat.dot");
            GeometryGraph graph = this.LoadGraph(filePath);
            var settings = new SugiyamaLayoutSettings();

            // Initial layout
            LayeredLayout layeredLayout = new LayeredLayout(graph, settings);
            layeredLayout.Run();
            SortedList<double, SortedList<double, Node>> originalLayers = SugiyamaValidation.GetLayers(graph, true);

            // Incremental layout
            List<Node> nodes = graph.Nodes.ToList();
            for (int i = 0; i < nodes.Count; i++)
            {
                // Resize a node
                Node node = nodes[i];
                node.BoundaryCurve = node.BoundaryCurve.ScaleFromOrigin(2.0, 2.0);

                // Run incremental layout
                LayeredLayout.IncrementalLayout(graph, node);
                
                // Verify - the layering and ordering of nodes should not have changed.
                SortedList<double, SortedList<double, Node>> newLayers = SugiyamaValidation.GetLayers(graph, true);
                VerifyLayersAreEqual(originalLayers, newLayers);
            }
        }

        /// <summary>
        /// Verifies that to sets of layers are ordered the same and each layer contains the same order of the same nodes.
        /// </summary>
        private static void VerifyLayersAreEqual(SortedList<double, SortedList<double, Node>> layers1, SortedList<double, SortedList<double, Node>> layers2)
        {
            Assert.AreEqual(layers1.Count, layers2.Count, "The two layer collections have different number of layers.");

            for (int i = 0; i < layers1.Count; i++)
            {
                IList<Node> nodes1 = layers1.Values[i].Values;
                IList<Node> nodes2 = layers2.Values[i].Values;

                Assert.AreEqual(nodes1.Count, nodes2.Count, "The two layers have different number of nodes.");

                for (int j = 0; j < nodes1.Count; j++)
                {
                    Assert.AreEqual(nodes1[j], nodes2[j], "The two layers have different ordered nodes.");
                }
            }
        }
    }
}
