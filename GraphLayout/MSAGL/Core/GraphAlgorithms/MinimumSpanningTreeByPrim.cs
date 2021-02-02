using System;
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
        readonly BasicGraphOnEdges<IEdge> graph;
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

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="weight"></param>
        /// <param name="root">the node we start building the tree</param>
        internal MinimumSpanningTreeByPrim(BasicGraphOnEdges<IEdge> graph, Func<IEdge, double> weight, int root) {
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
