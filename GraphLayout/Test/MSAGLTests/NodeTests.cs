//-----------------------------------------------------------------------
// <copyright file="NodeTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    [TestClass]
    public class NodeTests : MsaglTestBase {
        [TestMethod]
        [Description("Verifies that IsDescendantOf() works correctly in the basic node/cluster case.")]
        public void IsDescendantOf_BasicTest() {
            Cluster cluster = new Cluster();
            Node node = new Node();
            Node node2 = new Node();
            cluster.AddChild(node);

            Assert.IsTrue(node.IsDescendantOf(cluster), "Node is a descendant of cluster but IsDescendantOf returns false.");
            Assert.IsFalse(node2.IsDescendantOf(cluster), "Node2 is not a descendant of cluster but IsDescendantOf returns true.");
            Assert.IsFalse(cluster.IsDescendantOf(cluster), "A cluster should not be considered a descendant of itself.");
        }

    }
        
    
}
