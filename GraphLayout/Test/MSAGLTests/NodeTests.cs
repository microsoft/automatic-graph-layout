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
