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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Routing;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Node = Microsoft.Msagl.Core.Layout.Node;

namespace FindOverlapSample {
    class Helper {
        public static GeometryGraph CopyGraph(GeometryGraph graph) {
            if (graph == null) return null;
            var copy = new GeometryGraph();

            Dictionary<Node,Node> nodeCopy=new Dictionary<Node, Node>(graph.Nodes.Count);

            foreach (Node node in graph.Nodes) {
                var c = new Node();
                copy.Nodes.Add(c);
                nodeCopy[node] = c;
                c.BoundaryCurve = node.BoundaryCurve.Clone();
            }

            foreach (Edge edge in graph.Edges) {
                var source = edge.Source;
                var target = edge.Target;
                var copySource = nodeCopy[source];
                var copyTarget = nodeCopy[target];
                Edge edgeCopy=new Edge(copySource,copyTarget);
                copy.Edges.Add(edgeCopy);
                StraightLineEdges.RouteEdge(edgeCopy,0);
            }

            return copy;
        }

        /// <summary>
        /// Copies a graph and its GeometryGraph.
        /// </summary>
        /// <param name="parentGraph"></param>
        /// <returns></returns>
        public static Graph CopyGraph(Graph parentGraph) {
            GeometryGraph geometryCopy = CopyGraph(parentGraph.GeometryGraph);

            Graph graph=new Graph();
            graph.GeometryGraph = geometryCopy;

            Dictionary<Node,int> nodeId=new Dictionary<Node, int>(geometryCopy.Nodes.Count);
            for (int i = 0; i < geometryCopy.Nodes.Count; i++) {
                nodeId[geometryCopy.Nodes[i]] = i;
                String id = i.ToString();
            
               graph.AddNode(id);
                var node = graph.FindNode(id);
                node.GeometryNode = geometryCopy.Nodes[i];
                geometryCopy.Nodes[i].UserData = node;

            }

            foreach (var edge in geometryCopy.Edges) {
                String sourceId = nodeId[edge.Source].ToString();
                String targetId = nodeId[edge.Target].ToString();

                var edgeCopy=graph.AddEdge(sourceId, "", targetId);
                edgeCopy.GeometryEdge = edge;
                edge.UserData = edgeCopy;
            }
            return graph;
        }

    }
}
