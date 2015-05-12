using System;
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
