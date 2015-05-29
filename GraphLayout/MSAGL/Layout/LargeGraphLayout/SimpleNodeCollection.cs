using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    internal class SimpleNodeCollection : IList<Node> {
        List<Node> nodes;

        public SimpleNodeCollection(IEnumerable<Node> nodes) {
            this.nodes = new List<Node>(nodes);
        }

        public IEnumerator<Node> GetEnumerator() {
            return nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(Node item) {
            throw new NotImplementedException();
        }

        public void Clear() {
            throw new NotImplementedException();
        }

        public bool Contains(Node item) {
            throw new NotImplementedException();
        }

        public void CopyTo(Node[] array, int arrayIndex) {
            nodes.CopyTo(array,arrayIndex);
        }

        public bool Remove(Node item) {
            throw new NotImplementedException();
        }

        public int Count { get { return nodes.Count; } }
        public bool IsReadOnly { get; private set; }
        public int IndexOf(Node item) {
            throw new NotImplementedException();
        }

        public void Insert(int index, Node item) {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index) {
            throw new NotImplementedException();
        }

        public Node this[int index] {
            get { return nodes[index]; }
            set { nodes[index]=value; }
        }
    }
}