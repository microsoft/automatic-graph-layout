//-----------------------------------------------------------------------
// <copyright file="RTreeTest.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests {
    /// <summary>
    /// This is a test class for RTreeTest and is intended
    /// to contain all RTreeTest Unit Tests
    /// </summary>
    [TestClass()]
    public class NetworkSimplexTest : MsaglTestBase {
        [TestMethod]
        public void SmallGraph() {

            // (ab)(bc)(cd)(dh)(af)(fg)(ae)(eg)(gh)
            int a = 0;
            int b = 1;
            int c = 2;
            int d = 3;
            int e = 4;
            int f = 5;
            int g = 6;
            int h = 7;
            Func<int, int, PolyIntEdge> edge = (int x, int y) => new PolyIntEdge(x, y, null) { Separation = 1 };

            var edges = new PolyIntEdge[] { edge(a, b), edge(b, c), edge(c, d), edge(d, h), edge(a, f), edge(f, g), edge(a, e), edge(e, g), edge(g, h) };
            var graph = new BasicGraphOnEdges<PolyIntEdge>(edges);
            var ns = new NetworkSimplex(graph, new CancelToken());
            ns.Run();
            Assert.AreEqual(ns.Weight, 10);
        }
    }
}
