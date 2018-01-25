using System;
using System.Collections.Generic;

namespace Microsoft.Msagl.Core.Geometry {
    internal class PointNodesList : IEnumerator<Point>, IEnumerable<Point> {
        Site current, head;

        Site Head {
            get { return head; }
        }

        internal PointNodesList(Site pointNode) {
            head = pointNode;
        }

        internal PointNodesList() { }
        #region IEnumerator<Point> Members

        public Point Current {
            get { return current.Point; }
        }
     
        #endregion

        #region IDisposable Members

        public void Dispose() { GC.SuppressFinalize(this); }

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current {
            get { return current.Point; }
        }

        public bool MoveNext() {
            if (current != null) {
                if (current.Next != null) {
                    current = current.Next;
                    return true;
                } else
                    return false;
            } else {
                current = Head;
                return true;
            }

        }

        public void Reset() {
            current = null;
        }

        #endregion

        #region IEnumerable<Point> Members

        public IEnumerator<Point> GetEnumerator() {
            return this;
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this;
        }

        #endregion
    }
}
