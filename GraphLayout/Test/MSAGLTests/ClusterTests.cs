//-----------------------------------------------------------------------
// <copyright file="ClusterTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Tests specific to the InitialLayout class.
    /// </summary>
    [TestClass]
    public class ClusterTests : MsaglTestBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
        }

        [TestMethod]
        [Description("Checks that a cluster and all its children (nodes and edge routes) are properly translated")]
        public void SimpleDeepTranslationTest()
        {
            var graph = new GeometryGraph();
            var a = new Node(CurveFactory.CreateRectangle(30, 20, new Point()));
            var b = new Node(CurveFactory.CreateRectangle(30, 20, new Point(100, 0)));
            var e = new Edge(a, b);
            graph.Nodes.Add(a);
            graph.Nodes.Add(b);
            graph.Edges.Add(e);
            var c = CreateCluster(new Node[] { a, b }, 10);
            c.CalculateBoundsFromChildren(0);
            var originalClusterBounds = c.BoundingBox;
            RouteEdges(graph, 10);
            var edgeBounds = e.BoundingBox;

            Assert.AreEqual(c.BoundingBox.Width, 150, "Cluster has incorrect width");
            Assert.AreEqual(c.BoundingBox.Height, 40, "Cluster has incorrect width");

            var delta = new Point(10, 20);
            c.DeepTranslation(delta, true);
            Rectangle translatedClusterBounds = c.BoundingBox;

            Assert.IsTrue(ApproximateComparer.Close((translatedClusterBounds.LeftBottom - originalClusterBounds.LeftBottom), delta), "edge was not translated");

            c.CalculateBoundsFromChildren(0);

            Assert.IsTrue(ApproximateComparer.Close(translatedClusterBounds, c.BoundingBox), "translated bounds do not equal computed bounds of translated cluster");
            Assert.IsTrue(ApproximateComparer.Close((e.BoundingBox.LeftBottom - edgeBounds.LeftBottom), delta), "edge was not translated");
        }

        [TestMethod]
        [Description("Checks that edges between nodes and clusters and all bounds are correctly translated when nested")]
        public void NestedDeepTranslationTest() {
            EnableDebugViewer();
            List<Node> nodes =
                new[]
                    {
                        new Node(CurveFactory.CreateRectangle(30, 20, new Point()), 0),
                        new Node(CurveFactory.CreateRectangle(30, 20, new Point(100, 0)), 1),
                        new Node(CurveFactory.CreateRectangle(30, 20, new Point(200, 0)), 2)
                    }
                    .ToList();
            var graph = new GeometryGraph();
            nodes.ForEach(graph.Nodes.Add);
            nodes.Add(CreateCluster(nodes.Take(2), 10, "first")); 

            Assert.AreEqual(nodes[3].BoundingBox.Width, 150, "Inner Cluster has incorrect width");
            Assert.AreEqual(nodes[3].BoundingBox.Height, 40, "Inner Cluster has incorrect height");

            nodes.Add(CreateCluster(new[] { nodes[3], nodes[2] }, 10, "second"));
            graph.RootCluster = new Cluster(new Node[] { }, new[] { (Cluster)nodes[4] });
            List<Edge> edges = new[]
                {
                new Edge(nodes[0], nodes[1]) {UserData= "01" },
                new Edge(nodes[0], nodes[2]){UserData= "02" },
                new Edge(nodes[2], nodes[1]){UserData= "21" },
                new Edge(nodes[3], nodes[2]){UserData= "31" },
                new Edge(nodes[2], nodes[3]){UserData= "23" }
                }.ToList();
            edges.ForEach(graph.Edges.Add);
            RouteEdges(graph, 10);
            var clusterToMove = (Cluster)nodes[4];

            var translatedStuff = (from v in nodes where v.IsDescendantOf(clusterToMove) select v).Concat(from e in edges where e.Source.IsDescendantOf(clusterToMove) && e.Target.IsDescendantOf(clusterToMove) select (GeometryObject)e);
            var bounds = (from v in translatedStuff select v.BoundingBox).ToArray();
            var delta = new Point(10, 20);
            clusterToMove.DeepTranslation(delta, true);
            ShowGraphInDebugViewer(graph);
            var newbounds = (from v in translatedStuff select v.BoundingBox).ToArray();

            foreach (var b in translatedStuff.Zip(bounds, (translated, original) => new { T = translated, O = original }))
                Assert.IsTrue(
                    ApproximateComparer.Close(b.T.BoundingBox, Rectangle.Translate(b.O, delta)), "object was not translated: "+ b.T);
        }

        private static void RouteEdges(GeometryGraph graph, double padding)
        {
            var router = new SplineRouter(graph, padding, padding * 2.1, Math.PI / 6, null);
            router.Run();
        }

        private static Cluster CreateCluster(IEnumerable<Node> nodes, double margin, string name = "")
        {
            
            var cluster = new Cluster(nodes.Where(n => !(n is Cluster)))
            {
                RectangularBoundary =
                    new RectangularClusterBoundary { LeftMargin = margin, RightMargin = margin, BottomMargin = margin, TopMargin = margin },
                BoundaryCurve = CurveFactory.CreateRectangle(1, 1, new Point())
            };
            cluster.CalculateBoundsFromChildren(0);
            cluster.UserData = name;
            var nc = nodes.Where(n => n is Cluster);
            foreach (var b in nc) {
                cluster.AddCluster((Cluster)b);
            }
            return cluster;
        }
    }
}
