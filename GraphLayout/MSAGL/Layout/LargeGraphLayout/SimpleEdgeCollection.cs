using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    internal class SimpleEdgeCollection : EdgeCollection {
        readonly List<Edge> edges;

        public SimpleEdgeCollection(IEnumerable<Edge> collectionEdges) : base(null) {
            edges = collectionEdges as List<Edge>?? 
                collectionEdges.ToList();
        }
        public override IEnumerator<Edge> GetEnumerator() {
            return edges.GetEnumerator();
        }

        public override int Count {
            get { return edges.Count; }
        }

        public override void Add(Edge item) {
            throw new NotImplementedException();
        }

        public override void Clear() {
            throw new NotImplementedException();
        }

        public override bool Remove(Edge item) {
            throw new NotImplementedException();
        }
    }
}