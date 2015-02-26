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
﻿using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    internal class ShortestPathToAllOthers {
        LgNodeInfo source;
        readonly Dictionary<Node, LgNodeInfo> geometryNodesToLgNodes;
        GenericBinaryHeapPriorityQueue<LgNodeInfo> q = new GenericBinaryHeapPriorityQueue<LgNodeInfo>();
            

        public ShortestPathToAllOthers(LgNodeInfo source,
                                       Dictionary<Node, LgNodeInfo> geometryNodesToLgNodes) {
            this.source = source;
            this.geometryNodesToLgNodes = geometryNodesToLgNodes;
            foreach (var lgNodeInfo in geometryNodesToLgNodes.Values) {
                lgNodeInfo.Cost = double.PositiveInfinity;
                lgNodeInfo.InQueue = false;
                lgNodeInfo.Processed = false;
                lgNodeInfo.Prev = null;
            }
        }

        internal void Run() {
            source.Cost = 0;
            source.Processed = true;
            q.Enqueue(source, 0);
            while (q.Count > 0) {
                double cost;
                var n = q.Dequeue(out cost);
                ProcessNode(n);
            }
        }

        void ProcessNode(LgNodeInfo nodeInfo) {
            nodeInfo.Processed = true;
            foreach (Edge edge in nodeInfo.GeometryNode.OutEdges)
                ProcessEdge(edge, geometryNodesToLgNodes[edge.Target],nodeInfo.Cost);
            foreach (Edge edge in nodeInfo.GeometryNode.InEdges)
                ProcessEdge(edge, geometryNodesToLgNodes[edge.Source], nodeInfo.Cost);
        }

        void ProcessEdge(Edge edge, LgNodeInfo otherVert, double cost) {
            if (otherVert.Processed) return;
            var len = (edge.Source.Center - edge.Target.Center).Length;
            double newCost = len + cost;
            if (newCost >= otherVert.Cost) return;
            otherVert.Prev = edge;
            if (otherVert.Cost == double.PositiveInfinity) {
                otherVert.Cost = newCost;
                q.Enqueue(otherVert, newCost);
            } else {
                otherVert.Cost = newCost;
                q.DecreasePriority(otherVert, newCost);
            }
        }
    }
}