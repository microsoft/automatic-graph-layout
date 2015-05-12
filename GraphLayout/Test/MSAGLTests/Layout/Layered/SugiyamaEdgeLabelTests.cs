//-----------------------------------------------------------------------
// <copyright file="SugiyamaEdgeLabelTests.cs" company="Microsoft">
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
    /// Test class for verifying the positioning of labels along edges.
    /// </summary>
    [TestClass]
    [DeploymentItem(@"Resources\DotFiles\LevFiles\chat.dot", "Dots")]
    public class SugiyamaEdgeLabelTests : MsaglTestBase
    {
        [TestMethod]
        [Description("Verifies that each label is placed close to its edge.")]
        public void LabelsNearEdges()
        {
            // Setup
            string filePath = Path.Combine(this.TestContext.TestDir, "Out\\Dots", "chat.dot");

            GeometryGraph graph = this.LoadGraph(filePath);
            AddLabelSizes(graph);
            Assert.IsTrue(graph.CollectAllLabels().Count > 0, "The loaded graph has no labels.");

            // Execute
            LayeredLayout layeredLayout = new LayeredLayout(graph, new SugiyamaLayoutSettings());
            layeredLayout.Run();

            // Verify
            foreach (Edge edge in graph.Edges)
            {
                VerifyLabelIsNearEdges(edge);

                // We do not verify that labels don't overlap other edges, 
                // since that is not gauranteed by sugiyama layout.
            }
        }

        #region Helpers

        /// <summary>
        /// Gives each label a size.
        /// By default they don't have a size so we must fill it in.
        /// </summary>
        private static void AddLabelSizes(GeometryGraph graph)
        {
            foreach (Label label in graph.CollectAllLabels())
            {
                label.Width = 30;
                label.Height = 15;
            }
        }

        /// <summary>
        /// Verifies that the edge's label is near the edge.
        /// </summary>
        private static void VerifyLabelIsNearEdges(Edge edge)
        {
            Label label = edge.Label;
            if (label != null)
            {
                Rectangle labelBox = label.BoundingBox;
                Point[] edgePoints = edge.GetPoints();
                Point[] labelPoints = new[] { labelBox.Center, labelBox.LeftTop, labelBox.LeftBottom, labelBox.RightBottom, labelBox.RightTop };
                
                if (!edgePoints.Any(p => labelBox.Contains(p)))
                {
                    // If the edge doesn't intersect the label, check that the label is at least nearby.
                    // 10 was chosen as the distance tolerance since it is fairly small and passes on pretty much every diagram I tested.
                    double closestDistance = GetClosestDistance(edgePoints, labelPoints);
                    Assert.IsTrue(closestDistance < 10, "The label was placed greater than 10 units from the edge.");
                }
            }
        }

        /// <summary>
        /// Gets the distance between the closest 2 points in the two collections.
        /// </summary>
        /// <returns>The distance between the closest 2 points in the two collections.</returns>
        private static double GetClosestDistance(Point[] points1, Point[] points2)
        {
            double closest = double.MaxValue;
            for (int i = 0; i < points1.Length; i++)
            {
                for (int j = 0; j < points2.Length; j++)
                {
                    double distance = (points1[i] - points2[j]).Length;
                    if (distance < closest)
                    {
                        closest = distance;
                    }
                }
            }

            return closest;
        }

        #endregion Helpers
    }
}
