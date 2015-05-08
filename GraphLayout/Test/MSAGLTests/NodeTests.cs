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
    public class NodeTests : MsaglTestBase
    {
        [TestMethod]
        [Description("Verifies that IsDescendantOf() works correctly in the basic node/cluster case.")]
        public void IsDescendantOf_BasicTest()
        {
            Cluster cluster = new Cluster();
            Node node = new Node();
            Node node2 = new Node();
            cluster.AddChild(node);

            Assert.IsTrue(node.IsDescendantOf(cluster), "Node is a descendant of cluster but IsDescendantOf returns false.");
            Assert.IsFalse(node2.IsDescendantOf(cluster), "Node2 is not a descendant of cluster but IsDescendantOf returns true.");
            Assert.IsFalse(cluster.IsDescendantOf(cluster), "A cluster should not be considered a descendant of itself.");
        }

        [TestMethod]
        [Description("verifies that IsDescendantOf() works correctly when there are multiple parents.")]
        public void IsDescendantOf_ManyParents()
        {
            Cluster grandMother = new Cluster();
            Cluster grandFather = new Cluster();
            Cluster parent = new Cluster();
            Cluster uncle = new Cluster();
            grandMother.AddChild(parent);
            grandMother.AddChild(uncle);
            grandFather.AddChild(parent);
            grandFather.AddChild(uncle);

            Node child = new Node();
            parent.AddChild(child);

            Assert.IsTrue(child.IsDescendantOf(parent), "The child node should be considered a descendant of its parent.");
            Assert.IsTrue(child.IsDescendantOf(grandMother), "The child node should be considered a descendant of its grandmother.");
            Assert.IsTrue(child.IsDescendantOf(grandFather), "The child node should be considered a descendant of its grandfather.");
            Assert.IsFalse(child.IsDescendantOf(uncle), "The child node should not be considered a descendant of its uncle.");
        }
    }
}
