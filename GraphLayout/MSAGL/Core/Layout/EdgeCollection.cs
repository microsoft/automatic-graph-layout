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
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Layout.LargeGraphLayout;

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// A collection of edges.  Adding or removing edges from the collection automatically updates the related nodes.
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    public class EdgeCollection : IEnumerable<Edge> {
        readonly IList<Edge> edges = new List<Edge>();
        /// <summary>
        /// the graph of the collection
        /// </summary>
        protected readonly GeometryGraph graph;

        /// <summary>
        /// Creates a collection for edges.
        /// </summary>
        /// <param name="graph">The graph that each edge will be parented under.</param>
        public EdgeCollection(GeometryGraph graph) {
            this.graph = graph;
        }

        /// <summary>
        /// Adds the edge to the collection
        /// </summary>
        virtual public void Add(Edge item) {
            ValidateArg.IsNotNull(item, "item");
            item.GeometryParent = graph;
            edges.Add(item);
            AddEdgeToNodes(item);
        }

        static void AddEdgeToNodes(Edge item) {
            if (item.Source != item.Target) {
                item.Source.AddOutEdge(item);
                item.Target.AddInEdge(item);
            }
            else {
                item.Target.AddSelfEdge(item);
            }
        }

        /// <summary>
        /// Clears all of the edges.
        /// </summary>
        virtual public void Clear() {
            this.edges.Clear();
            foreach (var node in graph.Nodes) {
                ((Set<Edge>) node.OutEdges).Clear();
                ((Set<Edge>) node.InEdges).Clear();
                ((Set<Edge>) node.SelfEdges).Clear();
            }
        }

        /// <summary>
        /// Returns true if the edge is found in the collection.
        /// </summary>
        /// <returns>True if the edge is found in the collection.</returns>
        public bool Contains(Edge item) {
            ValidateArg.IsNotNull(item, "item");
            return edges.Contains(item);
        }

        /// <summary>
        /// Copies the contents of the collection to the given array, starting at the given index.
        /// </summary>
        public void CopyTo(Edge[] array, int arrayIndex) {
            this.edges.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// The number of edges in the collection.
        /// </summary>
        public virtual int Count {
            get { return this.edges.Count; }
        }

        /// <summary>
        /// Returns false. Edge collections are never readonly.
        /// </summary>
        public bool IsReadOnly {
            get { return false; }
        }

        /// <summary>
        /// Removes the edge from the collection.
        /// </summary>
        /// <returns>True if the edge was removed. False if the edge was not removed or the edge was not found.</returns>
        virtual public bool Remove(Edge item) {
            if (item == null || !this.edges.Contains(item))
                return false;

            DetouchEdge(item);

            return edges.Remove(item);
        }

        static void DetouchEdge(Edge item) {
            Node source = item.Source;
            Node target = item.Target;
            if (source != target) {
                source.RemoveOutEdge(item);
                target.RemoveInEdge(item);
            }
            else {
                source.RemoveSelfEdge(item);
            }
            item.GeometryParent = null;
        }

        /// <summary>
        /// Gets an enumerator for the collection.
        /// </summary>
        /// <returns>The enumerator for the colleciton.</returns>
        virtual public IEnumerator<Edge> GetEnumerator() {
            return edges.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator for the collection.
        /// </summary>
        /// <returns>The enumerator for the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable) this.edges).GetEnumerator();
        }
    }
}
