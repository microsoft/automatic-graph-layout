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
//-----------------------------------------------------------------------
// <copyright file="StickConstraintTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
//#define TEST_MSAGL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Testing StickConstraint and its derivatives
    /// </summary>
    [TestClass]
    public class StickConstraintTests : MsaglTestBase
    {
        private Random random = new Random(999);
        private GeometryGraph graph;
        private List<Node> allNodes;
        private FastIncrementalLayoutSettings settings;
        private Node[] constrainedNodes;

        [TestInitialize]
        public override void Initialize()
        {
            EnableDebugViewer();
            base.Initialize();
            graph = GraphGenerator.GenerateOneSimpleGraph();
            GraphGenerator.SetRandomNodeShapes(graph, random);
            allNodes = graph.Nodes.ToList();
            settings = new FastIncrementalLayoutSettings();
            settings.AvoidOverlaps = true;
        }

        [TestCleanup]
        public override void Cleanup()
        {
            base.Cleanup();
        }

        #region Test
        [TestMethod]
        [WorkItem(442941)]
        [Description("Test the default stick constraint")]
        public void DefaultStickConstraintTests()
        {
            constrainedNodes = new Node[] { allNodes[0], allNodes[random.Next(1, allNodes.Count)] };
            settings.AddStructuralConstraint(new StickConstraint(constrainedNodes[0], constrainedNodes[1], 100));
            LayoutAndValidate(true, 100, double.MinValue, double.MaxValue);
        }

        [TestMethod]
        [WorkItem(442941)]
        [Description("Test the MinSeparationConstraint")]
        public void MinSeparationConstraintTests()
        {
            constrainedNodes = new Node[] { allNodes[allNodes.Count - 1], allNodes[random.Next(allNodes.Count - 1)] };
            settings.AddStructuralConstraint(new MinSeparationConstraint(constrainedNodes[0], constrainedNodes[1], 50));
            LayoutAndValidate(false, -1, 50, double.MaxValue);
        }

        [TestMethod]
        [Description("Test the MaxSeparationConstraint")]
        public void MaxSeparationConstraintTests()
        {
            constrainedNodes = new Node[] { allNodes[allNodes.Count - 1], allNodes[random.Next(allNodes.Count - 1)] };
            settings.AddStructuralConstraint(new MaxSeparationConstraint(constrainedNodes[1], constrainedNodes[0], 150));
            LayoutAndValidate(false, -1, double.MinValue, 150);
        }

        [TestMethod]
        [WorkItem(442941)]
        [Description("Test the Min/MaxSeparationConstraint at the same time")]
        public void MinMaxSeparationConstraintTests()
        {
            int firstIndex = random.Next(allNodes.Count);
            int secondIndex = random.Next(allNodes.Count);
            while (secondIndex == firstIndex)
            {
                secondIndex = random.Next(allNodes.Count);
            }
            constrainedNodes = new Node[] { allNodes[firstIndex], allNodes[secondIndex] };
            settings.AddStructuralConstraint(new MinSeparationConstraint(allNodes[firstIndex], allNodes[secondIndex], 80));
            settings.AddStructuralConstraint(new MaxSeparationConstraint(allNodes[secondIndex], allNodes[firstIndex], 90));
            LayoutAndValidate(false, -1, 80, 90);
        }
        #endregion

        #region Helpers

        private void LayoutAndValidate(bool defaultSticky, double separation, double minSeparation, double maxSeparation)
        {
            InitialLayout initialLayout = new InitialLayout(graph, settings);
            initialLayout.Run();

            // Apply incremental layout to satisfy constraints.
            settings.IncrementalRun(graph);

            //ShowGraphInDebugViewer(graph);

            // stick constraints have a little slack in their projection
            const double StickDelta = 1;
            double distance = (constrainedNodes[0].Center - constrainedNodes[1].Center).Length;
            if (defaultSticky)
            {
                Assert.AreEqual(separation, distance, StickDelta, string.Format("Distance {3} between node {0} to node {1} not approximately equal to stickConstraint separation {2}", constrainedNodes[0].UserData, constrainedNodes[1].UserData, separation, distance));
            }
            else
            {
                Assert.IsTrue(distance >= minSeparation && distance <= maxSeparation, string.Format("Distance {4} between node {0} to node {1} not between min {2} and max {3}", constrainedNodes[0].UserData, constrainedNodes[1].UserData, minSeparation, maxSeparation, distance));
            }
        }

        #endregion
    }
}
