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
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Core.GraphAlgorithms {
    internal class NodeInfo {
        Set<int> outEdges = new Set<int>();

        internal Set<int> OutEdges {
            get { return outEdges; }
        }
        Set<int> inEdges = new Set<int>();

        internal Set<int> InEdges {
            get { return inEdges; }
        }
        Set<int> outConstrainedEdges = new Set<int>();

        internal Set<int> OutConstrainedEdges {
            get { return outConstrainedEdges; }
        }

        Set<int> inConstrainedEdges = new Set<int>();

        public Set<int> InConstrainedEdges {
            get { return inConstrainedEdges; }
        }
        /// <summary>
        /// it is the out degree without the in degree
        /// </summary>
        internal int DeltaDegree {
            get { return InDegree-OutDegree; }
        }
        internal void AddOutEdge(int v) {
            outEdges.Insert(v);
        }
        internal void RemoveOutEdge(int v) {
            outEdges.Remove(v);
        }

        internal void AddInEdge(int v) {
            inEdges.Insert(v);
        }
        internal void RemoveInEdge(int v) {
            inEdges.Remove(v);
        }
        internal void AddOutConstrainedEdge(int v) {
            outConstrainedEdges.Insert(v);
        }
        internal void RemoveOutConstrainedEdge(int v) {
            outConstrainedEdges.Remove(v);
        }

        internal void AddInConstrainedEdge(int v) {
            inConstrainedEdges.Insert(v);
        }
        internal void RemoveInConstrainedEdge(int v) {
            inConstrainedEdges.Remove(v);
        }

        internal int OutDegree {
            get {
                return outEdges.Count+outConstrainedEdges.Count;
            }
        }
        internal int InDegreeOfConstrainedEdges {
            get {
                return inConstrainedEdges.Count;
            }
        }
        internal int InDegree {
            get { return inEdges.Count+inConstrainedEdges.Count; }
        }


        /// <summary>
        /// including constrained neighbors
        /// </summary>
        internal IEnumerable<int> AllNeighbors {
            get {
                foreach (int v in this.OutConstrainedEdges)
                    yield return v;
                foreach (int v in this.InConstrainedEdges)
                    yield return v;
                foreach (int v in this.OutEdges)
                    yield return v;
                foreach (int v in this.InEdges)
                    yield return v;
            }
        }
    }
}
