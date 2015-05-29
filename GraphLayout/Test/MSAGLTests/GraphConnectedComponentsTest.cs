//-----------------------------------------------------------------------
// <copyright file="GraphConnectedComponentsTest.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Tests for connected components calculator (the tricky bit is it considers the contents of clusters connected)
    /// </summary>
    [TestClass]
    public class GraphConnectedComponentsTest : MsaglTestBase
    {
        private GeometryGraph graph;

        [TestInitialize]
        public override void Initialize()
        {
            graph = MsaglTestBase.CreateClusteredGraph();
        }

        [TestMethod]
        [Description("Test that FlatGraph generates a flat graph for a disconnected clustered graph with the expected number of nodes and edges")]
        public void FlatGraphTest()
        {
            var flatGraph = GraphConnectedComponents.FlatGraph(graph);
            Assert.AreEqual(19, flatGraph.Nodes.Count, "Expected 19 nodes in flatGraph (14 nodes + 5 clusters)");
            Assert.AreEqual(15, flatGraph.Edges.Count, "Expected 15 edges in flatGraph (5 root edges + 10 cluster edges)");
        }

        [TestMethod]
        [Description("Connected components are calculated for a fairly complicated little clustered graph, checks that the resultant components have the right configuration")]
        public void GetClusteredConnectedComponentsTest()
        {
            List<GeometryGraph> components = graph.GetClusteredConnectedComponents().ToList();

            List<Cluster> expectedTopLevelClusters = graph.RootCluster.Clusters.ToList();

            Assert.AreEqual(5, components.Count, "Expected 5 connected components");

            Node nodeA = graph.FindNodeByUserData("A");
            Node nodeB = graph.FindNodeByUserData("B");
            Node nodeC = graph.FindNodeByUserData("C");

            // go through each of the components and make sure that it matches the original
            // since the graph is traversed in order of Nodes we know what order the components will be appear in Actual
            GeometryGraph c = components[0];
            Assert.AreEqual(3, c.Nodes.Count, "Component has incorrect node count");
            Assert.IsTrue(c.FindNodeByUserData(nodeA) != null, "Component should contain node A");
            Assert.IsTrue(c.FindNodeByUserData(nodeB) != null, "Component should contain node B");
            Assert.IsTrue(c.FindNodeByUserData(nodeC) != null, "Component should contain node C");
            Assert.AreSame(c.FindNodeByUserData(nodeC).InEdges.First().UserData, nodeC.InEdges.First(), "Edge doesn't match original");

            Assert.AreEqual(1, c.Edges.Count, "Component should contain 1 edge");
            var cluster = c.RootCluster.Clusters.First();
            Assert.AreEqual(2, cluster.Nodes.Count(), "Component should contain 2 nodes");
            Assert.AreSame(expectedTopLevelClusters[0], cluster.UserData, "Component invalid");

            c = components[1];
            Assert.AreEqual(4, c.Nodes.Count, "Component invalid");
            Assert.AreEqual(2, c.Edges.Count, "Component invalid");
            cluster = c.RootCluster.Clusters.First();
            Assert.AreSame(expectedTopLevelClusters[1], cluster.UserData, "Component invalid");
            Assert.AreEqual(1, cluster.Nodes.Count(), "Component invalid");
            Assert.AreEqual("G", ((Node)cluster.Nodes.First().UserData).UserData, "Component invalid"); // double user data lookup since the first user data is the components source node
            var nested = cluster.Clusters.First();
            Assert.AreEqual("E", ((Node)nested.Nodes.First().UserData).UserData, "Component invalid");
            Assert.AreEqual("F", ((Node)nested.Nodes.Last().UserData).UserData, "Component invalid");
            Assert.AreSame(expectedTopLevelClusters[1].Clusters.First(), nested.UserData, "Component invalid");
            Assert.AreEqual(2, nested.Nodes.Count(), "Component invalid");

            c = components[2];
            Assert.AreEqual(2, c.Nodes.Count, "Component invalid");
            Assert.AreEqual(1, c.Edges.Count, "Component invalid");
            Assert.IsFalse(c.RootCluster.Clusters.Any(), "Component invalid");

            c = components[3];
            Assert.AreEqual(1, c.Nodes.Count, "Component invalid");
            Assert.AreEqual(0, c.Edges.Count, "Component invalid");
            Assert.IsFalse(c.RootCluster.Clusters.Any(), "Component invalid");

            c = components[4];
            cluster = c.RootCluster.Clusters.First();
            Assert.AreEqual("K", ((Node)cluster.Nodes.First().UserData).UserData, "Component invalid");
            Assert.AreEqual("L", ((Node)cluster.Nodes.Last().UserData).UserData, "Component invalid");
            cluster = c.RootCluster.Clusters.Last();
            Assert.AreEqual("M", ((Node)cluster.Nodes.First().UserData).UserData, "Component invalid");
            Assert.AreEqual("N", ((Node)cluster.Nodes.Last().UserData).UserData, "Component invalid");
            Assert.AreEqual(c.Edges.Count, 1, "Component invalid");
        }
    }
}
