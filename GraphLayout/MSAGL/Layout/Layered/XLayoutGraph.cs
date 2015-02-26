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
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Layered
{

  

    /// <summary>
    /// Follows the idea from Gansner etc 93, creating a special graph
    /// for x-coordinates calculation
    /// </summary>
    internal class XLayoutGraph : BasicGraph<IntEdge>
    {

        ProperLayeredGraph layeredGraph;//the result of layering

        LayerArrays layerArrays;//the result of layering
       
       
        int virtualVerticesStart;
        int virtualVerticesEnd; // we have 0,,,virtualVerticesStart-1 - usual vertices
        //virtualVerticesStart,...,virtualVerticesEnd -virtual vertices
        //and virtualVirticesEnd+1, ...NumberOfVertices - nvertices
        int weightMultiplierOfOriginalOriginal = 1; //weight multiplier for edges with Defaults or n end and start
        int weightMultOfOneVirtual = 3; //weight multiplier for edges with only one virtual node
        int weightMultiplierOfTwoVirtual = 8; //weight multiplier for edges with two virtual nodes

        internal XLayoutGraph(BasicGraph<IntEdge> graph, //DAG of the original graph with no multiple edges
                              ProperLayeredGraph layeredGraph,
                              LayerArrays layerArrays,
                              List<IntEdge> edges,
                              int nov)
        {
            this.SetEdges(edges, nov);
            this.virtualVerticesStart = graph.NodeCount;
            this.virtualVerticesEnd = layeredGraph.NodeCount - 1;
            this.layeredGraph = layeredGraph;
            this.layerArrays = layerArrays;
        }

      
        /// <summary>
        /// following Gansner etc 93 returning weight multplier bigger if there are virtual nodes
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        internal int EdgeWeightMultiplier(IntEdge edge)
        {


            int s = edge.Source;
            int t = edge.Target;

            if (s < this.layeredGraph.NodeCount &&
                layerArrays.Y[s] == layerArrays.Y[t] &&
                layerArrays.X[s] == layerArrays.X[t] + 1)
                return 0; //this edge needed only for separation vertices in the same layer

            int k = 0;
            System.Diagnostics.Debug.Assert(s >= this.layeredGraph.NodeCount); //check the graph on correctness`    
            //    throw new InvalidOperationException();//"XLayout graph is incorrect");

            //here (s0,t0) is the edge of underlying graph 
            int s0 = -1, t0 = -1; //t0 is set to -1 to only avoid the warning
            //there are only two edges in graph.OutEdges(s)
            foreach (IntEdge intEdge in this.OutEdges(s))
            {
                if (s0 == -1)
                    s0 = intEdge.Target;
                else
                    t0 = intEdge.Target;
            }

            if (s0 >= virtualVerticesStart && s0 <= virtualVerticesEnd)
                k++;

            if (t0 >= virtualVerticesStart && t0 <= virtualVerticesEnd)
                k++;

            int ret = k == 0 ? weightMultiplierOfOriginalOriginal : (k == 1 ? weightMultOfOneVirtual : weightMultiplierOfTwoVirtual);
            return ret;
        }

        /// <summary>
        /// caching edges weights
        /// </summary>
        internal void SetEdgeWeights()
        {

            foreach (IntEdge intEdge in this.Edges)
                intEdge.Weight = intEdge.Weight * EdgeWeightMultiplier(intEdge);                            
        }
       
    }
}
