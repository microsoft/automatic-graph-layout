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
