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
using System.Collections;

using Microsoft.Msagl.Core.GraphAlgorithms;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// Enumerator of the vertex successors
    /// </summary>
    internal class SuccEnumerator : IEnumerator<int> {
        IEnumerator edges;

        public void Reset() {
            edges.Reset();
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
        }


        public bool MoveNext() {
            return edges.MoveNext();
        }

        internal SuccEnumerator(IEnumerator edges) {
            this.edges = edges;
        }

        #region IEnumerator<int> Members

        public int Current {
            get {
                return((IntEdge)edges.Current).Target;
            }

        }

        #endregion

        #region IEnumerator Members

        object IEnumerator.Current {
            get {
                throw new NotImplementedException();
            }
        }

        #endregion
    }

    /// <summary>
    /// Enumeration of the vertex predecessors
    /// </summary>
    internal class PredEnumerator : IEnumerator<int> {
        IEnumerator edges;

        public void Dispose() { GC.SuppressFinalize(this); }

        public void Reset() {
            edges.Reset();
        }

#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=203
        public int Current {
#else
        int IEnumerator<int>.Current {
#endif
            get {
                return ((IntEdge)edges.Current).Source;
            }
        }

        object IEnumerator.Current {
            get {
                IntEdge l = edges.Current as IntEdge;
                return l.Source;
            }
        }

        public bool MoveNext() {
            return edges.MoveNext();
        }

        internal PredEnumerator(IEnumerator edges) {
            this.edges = edges;
        }
    }

    internal class EmptyEnumerator : IEnumerator<int> {
        public bool MoveNext() { return false; }

#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=203
        public int Current { get { return 0; } }
#else
        int IEnumerator<int>.Current { get { return 0; } }
#endif
        object IEnumerator.Current { get { return 0; } }

        void IEnumerator.Reset() { }


        public void Dispose() { GC.SuppressFinalize(this); }

    }

    internal class Pred : IEnumerable<int> {
        #region IEnumerable Members

        BasicGraph<IntEdge> graph;

        int vert;

#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=203
        public IEnumerator<int> GetEnumerator() {
#else
        IEnumerator<int> IEnumerable<int>.GetEnumerator() {
#endif
        IEnumerable e = graph.InEdges(vert);
            if (e == null) {
                return new EmptyEnumerator();
            } else
                return new PredEnumerator(e.GetEnumerator());

        }

        IEnumerator IEnumerable.GetEnumerator() {
            IEnumerable e = graph.InEdges(vert);
            if (e == null) {
                return new EmptyEnumerator();
            } else
                return new PredEnumerator(e.GetEnumerator());

        }

        internal Pred(BasicGraph<IntEdge> g, int v) {
            this.graph = g;
            this.vert = v;
        }

        #endregion


    }


    internal class Succ {
        #region IEnumerable Members

        BasicGraph<IntEdge> graph;

        int vert;

        public IEnumerator<int> GetEnumerator() {
            return new SuccEnumerator(graph.OutEdges(vert).GetEnumerator());
        }

        internal Succ(BasicGraph<IntEdge> g, int v) {
            this.graph = g;
            this.vert = v;
        }



        #endregion
    }

}
