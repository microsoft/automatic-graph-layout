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
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Layered {
    internal class NetworkSimplexForGeneralGraph:LayerCalculator {
        BasicGraph<IntEdge> graph;
        /// <summary>
        /// a place holder for the cancel flag
        /// </summary>
        internal CancelToken Cancel { get; set; }

        public int[] GetLayers() {
            List<IEnumerable<int>> comps = new List<IEnumerable<int>>(ConnectedComponentCalculator<IntEdge>.GetComponents(graph));
            if (comps.Count == 1) {
                NetworkSimplex ns = new NetworkSimplex(graph, this.Cancel);
                return ns.GetLayers();
            }
            List<Dictionary<int, int>> mapToComponenents = GetMapsToComponent(comps);
            int[][] layerings = new int[comps.Count][];

            for (int i = 0; i < comps.Count; i++) {
                BasicGraph<Node, IntEdge> shrunkedComp = ShrunkComponent(mapToComponenents[i]);
                NetworkSimplex ns = new NetworkSimplex(shrunkedComp, Cancel);
                layerings[i] = ns.GetLayers();
            }

            return UniteLayerings(layerings, mapToComponenents);
        }

        private BasicGraph<Node, IntEdge> ShrunkComponent(Dictionary<int, int> dictionary) {
            return new BasicGraph<Node, IntEdge>(
                from p in dictionary
                let v = p.Key
                let newEdgeSource = p.Value
                from e in graph.OutEdges(v)
                select new IntEdge(newEdgeSource, dictionary[e.Target]) { Separation = e.Separation, Weight = e.Weight },
                dictionary.Count);
        }

        private int[] UniteLayerings(int[][] layerings, List<Dictionary<int, int>> mapToComponenents) {
            int[] ret = new int[graph.NodeCount];
            for (int i = 0; i < layerings.Length; i++) {
                int[] layering = layerings[i];
                Dictionary<int, int> mapToComp = mapToComponenents[i];
                //no optimization at the moment - just map the layers back
                int[] reverseMap = new int[mapToComp.Count];
                foreach (var p in mapToComp)
                    reverseMap[p.Value] = p.Key;

                for (int j = 0; j < layering.Length; j++)
                    ret[reverseMap[j]] = layering[j];
            }
            return ret;
        }

        static List<Dictionary<int, int>> GetMapsToComponent(List<IEnumerable<int>> comps) {
            List<Dictionary<int, int>> ret = new List<Dictionary<int, int>>();
            foreach (var comp in comps)
                ret.Add(MapForComp(comp));
            return ret;
        }

        static Dictionary<int, int> MapForComp(IEnumerable<int> comp) {
            int i = 0;
            Dictionary<int,int> map=new Dictionary<int,int>();
            foreach (int v in comp)
                map[v] = i++;
            return map;
        }

      
        internal NetworkSimplexForGeneralGraph(BasicGraph<Node, IntEdge> graph, CancelToken cancelObject) {
            this.graph = graph;
            this.Cancel = cancelObject;
        }

    }
}
