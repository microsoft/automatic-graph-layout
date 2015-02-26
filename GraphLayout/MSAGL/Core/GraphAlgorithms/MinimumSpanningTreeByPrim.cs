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
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;

namespace Microsoft.Msagl.Core.GraphAlgorithms {
    /// <summary>
    /// 
    /// </summary>
    public class MinimumSpanningTreeByPrim {
        readonly BasicGraph<IEdge> graph;
        readonly Func<IEdge, double> weight;
        readonly int root;
        readonly BinaryHeapPriorityQueue q;
        Set<int> treeNodes = new Set<int>();
        //map of neighbors of the tree to the edges connected them to the tree
        Dictionary<int, IEdge> hedgehog = new Dictionary<int, IEdge>(); 
        
        /// <summary>
        /// 
        /// </summary>
        public static void Test() {

        }

        static IEnumerable<IntPair> GetEdges(int count) {
            for(int i=0;i<count;i++)
                for (int j = i + 1; j < count; j++) {
                    yield return new IntPair(i,j);
                }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="weight"></param>
        /// <param name="root">the node we start building the tree</param>
        internal MinimumSpanningTreeByPrim(BasicGraph<IEdge> graph, Func<IEdge, double> weight, int root) {
            this.graph = graph;
            this.weight = weight;
            this.root = root;
            q=new BinaryHeapPriorityQueue(graph.NodeCount);
        }

        bool NodeIsInTree(int i) {
            return treeNodes.Contains(i);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IList<IEdge> GetTreeEdges()
        {
            var ret = new List<IEdge>(graph.NodeCount - 1);
            Init();
            while (ret.Count < graph.NodeCount - 1 && q.Count > 0) //some nodes might have no edges
                AddEdgeToTree(ret);
            return ret;
        }

        void AddEdgeToTree(List<IEdge> ret) {
            var v = q.Dequeue();
            var e = hedgehog[v];
            treeNodes.Insert(v);
            ret.Add(e);
            UpdateOutEdgesOfV(v);
            UpdateInEdgesOfV(v);
        }

        void UpdateOutEdgesOfV(int v) {
            foreach (var outEdge in graph.OutEdges(v)) {
                var u = outEdge.Target;
                if (NodeIsInTree(u)) continue;
                IEdge oldEdge;
                if (hedgehog.TryGetValue(u, out oldEdge)) {
                    var oldWeight = weight(oldEdge);
                    var newWeight = weight(outEdge);
                    if (newWeight < oldWeight) {
                        q.DecreasePriority(u, newWeight);
                        hedgehog[u] = outEdge;
                    }
                } else {
                    q.Enqueue(u, weight(outEdge));
                    hedgehog[u] = outEdge;
                }
            }
        }

        void UpdateInEdgesOfV(int v)
        {
            foreach (var inEdge in graph.InEdges(v))
            {
                var u = inEdge.Source;
                if (NodeIsInTree(u)) continue;
                IEdge oldEdge;
                if (hedgehog.TryGetValue(u, out oldEdge))
                {
                    var oldWeight = weight(oldEdge);
                    var newWeight = weight(inEdge);
                    if (newWeight < oldWeight)
                    {
                        q.DecreasePriority(u, newWeight);
                        hedgehog[u] = inEdge;
                    }
                }
                else
                {
                    q.Enqueue(u, weight(inEdge));
                    hedgehog[u] = inEdge;
                }
            }
        }

        void Init() {
            treeNodes.Insert(root);

            foreach (var outEdge in graph.OutEdges(root)) {
                var w = weight(outEdge);
                q.Enqueue(outEdge.Target, w);
                hedgehog[outEdge.Target] = outEdge;
            }

            foreach (var inEdge in graph.InEdges(root)) {
                var w = weight(inEdge);
                q.Enqueue(inEdge.Source, w);
                hedgehog[inEdge.Source] = inEdge;
            }
        }
    }
}
