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
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Drawing;
using System.IO;
using Microsoft.VisualStudio.GraphModel;
using xCodeMap.xGraphControl;
using Graph = Microsoft.VisualStudio.GraphModel.Graph;

namespace xCodeMap
{
    static public class DGMLParser
    {
        static public Microsoft.Msagl.Drawing.Graph Parse(string filename, 
                                                          out Dictionary<DrawingObject, IViewerObjectX> vObjectsMapping)
        {
            Microsoft.VisualStudio.GraphModel.Graph g = Microsoft.VisualStudio.GraphModel.Graph.Load(filename, delegate(object sender, GraphDeserializationProgressEventArgs e) { });
            Microsoft.Msagl.Drawing.Graph drawingGraph = new Microsoft.Msagl.Drawing.Graph();
            
            vObjectsMapping = new Dictionary<DrawingObject, IViewerObjectX>();

            Dictionary<string, Subgraph> subgraphTable = GetSubgraphIds(g);
            
            ProcessNodes(vObjectsMapping, g, subgraphTable, drawingGraph);

            ProcessLinks(vObjectsMapping, g, subgraphTable, drawingGraph);

            foreach (var subgraph in subgraphTable.Values)
                if (subgraph.ParentSubgraph == null)
                    drawingGraph.RootSubgraph.AddSubgraph(subgraph);

            return drawingGraph;
        }

        static Dictionary<string,Subgraph> GetSubgraphIds(Graph g) {
            Dictionary<string, Subgraph> ret = new Dictionary<string, Subgraph>();
            foreach (GraphLink gl in g.Links)
                foreach (GraphCategory gc in gl.Categories)
                    if (gc.ToString().Replace("CodeSchema_", "") == "Contains")
                        ret[gl.Source.Id.LiteralValue] = null; //init it later
            return ret;
        }

        static void ProcessLinks(Dictionary<DrawingObject, IViewerObjectX> vObjectsMapping, Graph g, Dictionary<string, Subgraph> subgraphTable, Microsoft.Msagl.Drawing.Graph drawingGraph) {
            foreach (GraphLink gl in g.Links) {
                var sourceId = gl.Source.Id.LiteralValue;
                var targetId = gl.Target.Id.LiteralValue;

                Subgraph sourceSubgraph;
                Node source = !subgraphTable.TryGetValue(sourceId, out sourceSubgraph) ? drawingGraph.FindNode(sourceId) : sourceSubgraph;

                Subgraph targetSubgraph;
                Node target = !subgraphTable.TryGetValue(targetId, out targetSubgraph) ? drawingGraph.FindNode(targetId) : targetSubgraph;
                
                foreach (GraphCategory gc in gl.Categories) {
                    string c = gc.ToString().Replace("CodeSchema_", "");
                    if (c == "Contains") {
                        if (targetSubgraph != null)
                            sourceSubgraph.AddSubgraph(targetSubgraph);
                        else
                            sourceSubgraph.AddNode(target);
                    } else {
                        Edge edge = new Edge(source, target, ConnectionToGraph.Connected);
                        
                        edge.Label = new Label(c);
                        XEdge xEdge = new XEdge(edge, c);
                        vObjectsMapping[edge] = xEdge;
                    }
                }
            }
        }

        static void ProcessNodes(Dictionary<DrawingObject, IViewerObjectX> vObjectsMapping, Graph g, Dictionary<string, Subgraph> subgraphTable, Microsoft.Msagl.Drawing.Graph drawingGraph) {
            foreach (GraphNode gn in g.Nodes) {
                Node drawingNode;
                if (subgraphTable.ContainsKey(gn.Id.LiteralValue)) {
                    var subgraph = new Subgraph(gn.Id.LiteralValue);
                    subgraphTable[subgraph.Id] = subgraph;

                    drawingNode = subgraph;
                } else
                    drawingNode = drawingGraph.AddNode(gn.Id.LiteralValue);

                drawingNode.Label=new Label(gn.Label);

                string category = null;
                if (gn.Categories.Any())
                    category = gn.Categories.ElementAt(0).ToString().Replace("CodeSchema_", "");

                XNode vNode = new XNode(drawingNode, gn);
                vObjectsMapping[drawingNode] = vNode;
            }
        }
    }
}
