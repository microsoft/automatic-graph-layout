using System;
using System.Collections.Generic;

namespace Microsoft.Msagl.Core.DataStructures {
    internal class RBTreeEnumerator<T> : IEnumerator<T> {
        bool initialState;
        RbTree<T> tree;
        RBNode<T> c;
        public T Current {
            get { return c.Item; }
        }
        public void Reset() {
            initialState = true;
        }

        public bool MoveNext() {
            if (tree.IsEmpty())
                return false;

            if (initialState == true) {
                initialState = false;
                c = tree.TreeMinimum();
            } else {
                c = tree.Next(c);
            }
            return c != null;
        }

        internal RBTreeEnumerator(RbTree<T> tree) {
            this.tree = tree;
            Reset();

        }

        #region IDisposable Members

        public void Dispose() {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current {
            get { return c.Item; }
        }

        #endregion
    }


}
