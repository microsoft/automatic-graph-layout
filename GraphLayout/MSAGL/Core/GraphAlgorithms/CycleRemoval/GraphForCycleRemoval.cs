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

    internal class GraphForCycleRemoval {
        /// <summary>
        /// this dictionary contains only buckets with nodes which are sources in the graph of constrained edges
        /// </summary>
        SortedDictionary<int, Set<int>> deltaDegreeBucketsForSourcesInConstrainedSubgraph = new SortedDictionary<int, Set<int>>();
        Dictionary<int, NodeInfo> nodeInfoDictionary = new Dictionary<int, NodeInfo>();
        Set<int> sources=new Set<int>();
        Set<int> sinks=new Set<int>();
        Set<IntPair> edgesToKeep = new Set<IntPair>();

        internal void AddEdge(IntPair edge) {
            edgesToKeep.Insert(edge);
            int source = edge.First; int target = edge.Second;
            GetOrCreateNodeInfo(source).AddOutEdge(target);
            GetOrCreateNodeInfo(target).AddInEdge(source);
        }


        private NodeInfo GetOrCreateNodeInfo(int node) {
            NodeInfo nodeInfo;
            if (!nodeInfoDictionary.TryGetValue(node, out nodeInfo)) {
                nodeInfo = new NodeInfo();
                nodeInfoDictionary[node] = nodeInfo;
            }
            return nodeInfo;
        }

        internal void AddConstraintEdge(IntPair intPair) {
            int source = intPair.First; int target = intPair.Second;
            GetOrCreateNodeInfo(source).AddOutConstrainedEdge(target);
            GetOrCreateNodeInfo(target).AddInConstrainedEdge(source);
        }

        internal bool IsEmpty() {
            return nodeInfoDictionary.Count == 0;
        }

        internal void RemoveNode(int u) {
            sources.Remove(u);
            sinks.Remove(u);
            RemoveNodeFromItsBucket(u);
            NodeInfo uNodeInfo=this.nodeInfoDictionary[u];
            Set<int> allNbs = new Set<int>(uNodeInfo.AllNeighbors);
            foreach(int v in allNbs)
                RemoveNodeFromItsBucket(v);

            DisconnectNodeFromGraph(u, uNodeInfo);

            foreach (int v in allNbs)
                AddNodeToBucketsSourcesAndSinks(v, nodeInfoDictionary[v]);
  

        }

        private void DisconnectNodeFromGraph(int u, NodeInfo uNodeInfo) {
            foreach (int v in uNodeInfo.OutEdges)
                nodeInfoDictionary[v].RemoveInEdge(u);

            foreach (int v in uNodeInfo.OutConstrainedEdges)
                nodeInfoDictionary[v].RemoveInConstrainedEdge(u);

            foreach (int v in uNodeInfo.InEdges)
                nodeInfoDictionary[v].RemoveOutEdge(u);

            foreach (int v in uNodeInfo.InConstrainedEdges)
                nodeInfoDictionary[v].RemoveOutConstrainedEdge(u);

            nodeInfoDictionary.Remove(u);
        }

        

        private void RemoveNodeFromItsBucket(int v) {
            int delta = DeltaDegree(v);
            Set<int> bucket;
            if (this.deltaDegreeBucketsForSourcesInConstrainedSubgraph.TryGetValue(delta, out bucket)) {
                bucket.Remove(v);
                if (bucket.Count == 0)
                    deltaDegreeBucketsForSourcesInConstrainedSubgraph.Remove(delta);
            }
        }

        private int DeltaDegree(int v) {
            int delta = nodeInfoDictionary[v].DeltaDegree;
            return delta;
        }



        internal bool TryFindVertexWithNoIncomingConstrainedEdgeAndMaximumOutDegreeMinusInDedree(out int u) {
            var enumerator = this.deltaDegreeBucketsForSourcesInConstrainedSubgraph.GetEnumerator();
            if (enumerator.MoveNext()) {
                var bucketSet = enumerator.Current.Value;
                System.Diagnostics.Debug.Assert(bucketSet.Count > 0);
                var nodeEnumerator = bucketSet.GetEnumerator();
                nodeEnumerator.MoveNext();
                u = nodeEnumerator.Current;
                return true;
            }
            u = -1;
            return false;
        }

        internal bool TryGetSource(out int u) {
            var enumerator = this.sources.GetEnumerator();
            if (enumerator.MoveNext()) {
                u = enumerator.Current;
                return true;
            }
            u = -1;
            return false;
        }

        internal bool TryGetSink(out int u) {
            var enumerator = this.sinks.GetEnumerator();
            if (enumerator.MoveNext()) {
                u = enumerator.Current;
                return true;
            }
            u = -1;
            return false;
        }

        internal IEnumerable<IntPair> GetOriginalIntPairs() {
            return this.edgesToKeep;
        }

        internal void Initialize() {
            foreach (var p in nodeInfoDictionary) 
                AddNodeToBucketsSourcesAndSinks(p.Key, p.Value);
        }

        private void AddNodeToBucketsSourcesAndSinks(int v, NodeInfo nodeInfo) {
            if (nodeInfo.InDegree == 0)
                sources.Insert(v);
            else if (nodeInfo.OutDegree == 0)
                sinks.Insert(v);
            else if (nodeInfo.InDegreeOfConstrainedEdges == 0)
                GetOrCreateBucket(nodeInfo.DeltaDegree).Insert(v);
        }

        private Set<int> GetOrCreateBucket(int delta) {
            Set<int> ret;
            if (this.deltaDegreeBucketsForSourcesInConstrainedSubgraph.TryGetValue(delta, out ret))
                return ret;
            return deltaDegreeBucketsForSourcesInConstrainedSubgraph[delta] = new Set<int>();
        }
    }
}
