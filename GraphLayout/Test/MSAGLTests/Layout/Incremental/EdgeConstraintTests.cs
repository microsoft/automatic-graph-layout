//-----------------------------------------------------------------------
// <copyright file="EdgeConstraintTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Testing EdgeConstraintGenerator
    /// </summary>
    [TestClass]
    public class EdgeConstraintTests : MsaglTestBase
    {
        private Random random = new Random(999);
        [TestInitialize]
        public override void Initialize()
        {
            EnableDebugViewer();
            base.Initialize();
        }

        [TestCleanup]
        public override void Cleanup()
        {
            base.Cleanup();
        }

        #region Test
        [TestMethod]
        [Description("Test a chain graph")]
        public void ChainGraphDownwardConstraintTests()
        {
            GeometryGraph graph = GraphGenerator.GenerateSimpleChain(10);
            GraphGenerator.SetRandomNodeShapes(graph, random);

            LayoutAndValidate(graph, 1.5, null);
        }

        [TestMethod]
        [WorkItem(442793)]
        [Description("Test a circle graph")]
        public void CircleGraphDownwardConstraintTests()
        {
            GeometryGraph graph = GraphGenerator.GenerateCircle(6);
            ISet<Node> avoidNodes = new HashSet<Node>(graph.Nodes);
            
            Node firstNode = graph.Nodes.First();
            Node lastNode = graph.Nodes.Last();

            //add some non-cyclic nodes
            Node node1 = GraphGenerator.CreateNode(graph.Nodes.Count);
            Node node2 = GraphGenerator.CreateNode(graph.Nodes.Count + 1);
            graph.Nodes.Add(node1);
            graph.Nodes.Add(node2);
            graph.Edges.Add(GraphGenerator.CreateEdge(firstNode, node1));
            graph.Edges.Add(GraphGenerator.CreateEdge(node2, lastNode));

            GraphGenerator.SetRandomNodeShapes(graph, random);
            LayoutAndValidate(graph, 2.5, avoidNodes);
        }
        #endregion

        #region Helpers
        private static void LayoutAndValidate(GeometryGraph graph, double separationRatio, ISet<Node> validationExcludedNodes)
        {
            const double downwardEdgeSeparation = 70;
            FastIncrementalLayoutSettings settings = new FastIncrementalLayoutSettings();
            settings.IdealEdgeLength = new EdgeConstraints
            {
                Separation = downwardEdgeSeparation,
                Direction = Direction.South
            };
            settings.AvoidOverlaps = true;            

            InitialLayout initialLayout = new InitialLayout(graph, settings);
            initialLayout.Run();

            ShowGraphInDebugViewer(graph);

            ValidateDownwardEdgeConstraints(graph, downwardEdgeSeparation, validationExcludedNodes);
        }

        private static void ValidateDownwardEdgeConstraints(GeometryGraph graph, double minSeparation, ISet<Node> validationExcludedNodes)
        {
            foreach (Edge edge in graph.Edges)
            {
                if (validationExcludedNodes != null)
                {
                    if (validationExcludedNodes.Contains(edge.Source) && validationExcludedNodes.Contains(edge.Target))
                    {
                        continue;
                    }
                }
                Assert.IsTrue(edge.Target.Center.Y > edge.Source.Center.Y, string.Format("Edge from source {0} to target {1} does not follow downward rule", edge.Source.UserData, edge.Target.UserData));
                Assert.IsTrue(edge.Target.Center.Y - edge.Source.Center.Y + ApproximateComparer.DistanceEpsilon >= minSeparation, string.Format("Edge from source {0} to target {1} does not follow valid downward separation distance", edge.Source.UserData, edge.Target.UserData));
            }
        }
        #endregion
    }
}
