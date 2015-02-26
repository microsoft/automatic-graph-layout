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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    internal class LgNodeCollection : IList<Node> {
        readonly Func<IEnumerable<Node>> funcOfNodes;

        public LgNodeCollection(Func<IEnumerable<LgNodeInfo>> funcOfLgNodes) {
            this.funcOfNodes = ()=>funcOfLgNodes().Select(n=>n.GeometryNode);
        }

        public IEnumerator<Node> GetEnumerator() {
            return funcOfNodes().GetEnumerator();
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
            throw new NotImplementedException();
        }

        public bool Remove(Node item) {
            throw new NotImplementedException();
        }

        public int Count {
            get {
                return funcOfNodes().Count();                
            }
            
        }
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
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}