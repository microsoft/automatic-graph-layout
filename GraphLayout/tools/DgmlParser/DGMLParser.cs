#if TEST_MSAGL
using System.Collections.Generic;
using Microsoft.Msagl.Drawing;
using Microsoft.VisualStudio.GraphModel;
using DgmlGraph = Microsoft.VisualStudio.GraphModel.Graph;

namespace DgmlParser {
    public static class DgmlParser {
        public static Microsoft.Msagl.Drawing.Graph Parse(string filename) {
            var dgmlGraph = Microsoft.VisualStudio.GraphModel.Graph.Load(filename,
                                                                         delegate { });
            Microsoft.Msagl.Drawing.Graph drawingGraph = new Microsoft.Msagl.Drawing.Graph();

            Dictionary<string, Subgraph> subgraphTable = GetSubgraphIds(dgmlGraph);

            ProcessNodes(dgmlGraph, subgraphTable, drawingGraph);

            ProcessLinks(dgmlGraph, subgraphTable, drawingGraph);

            foreach (var subgraph in subgraphTable.Values)
                if (subgraph.ParentSubgraph == null)
                    drawingGraph.RootSubgraph.AddSubgraph(subgraph);

            return drawingGraph;
        }

        static Dictionary<string, Subgraph> GetSubgraphIds(DgmlGraph g) {
            Dictionary<string, Subgraph> ret = new Dictionary<string, Subgraph>();
            foreach (GraphLink gl in g.Links)
                foreach (GraphCategory gc in gl.Categories)
                    if (gc.ToString().Replace("CodeSchema_", "") == "Contains")
                        ret[gl.Source.Id.LiteralValue] = null; //init it later
            return ret;
        }

        static void ProcessLinks(DgmlGraph g,
                                 Dictionary<string, Subgraph> subgraphTable, Microsoft.Msagl.Drawing.Graph drawingGraph) {
            foreach (GraphLink gl in g.Links) {
                var sourceId = gl.Source.Id.LiteralValue;
                var targetId = gl.Target.Id.LiteralValue;

                Subgraph sourceSubgraph;
                Node source = !subgraphTable.TryGetValue(sourceId, out sourceSubgraph)
                                  ? drawingGraph.FindNode(sourceId)
                                  : sourceSubgraph;

                Subgraph targetSubgraph;
                Node target = !subgraphTable.TryGetValue(targetId, out targetSubgraph)
                                  ? drawingGraph.FindNode(targetId)
                                  : targetSubgraph;

                bool containment=false;
                foreach (GraphCategory gc in gl.Categories) {
                    string c = gc.ToString().Replace("CodeSchema_", "");
                    if (c == "Contains") {
                        
                        if (targetSubgraph != null)
                            sourceSubgraph.AddSubgraph(targetSubgraph);
                        else
                            sourceSubgraph.AddNode(target);
                        containment = true;
                    }

                }
                if (!containment) {
                    Edge edge = new Edge(source, target, ConnectionToGraph.Connected);
                   // edge.Label = new Label(c);
                   
                }
            }
        }

        static void ProcessNodes(DgmlGraph g,
                                 Dictionary<string, Subgraph> subgraphTable, Microsoft.Msagl.Drawing.Graph drawingGraph) {
            foreach (GraphNode gn in g.Nodes) {
                Node drawingNode;
                if (subgraphTable.ContainsKey(gn.Id.LiteralValue)) {
                    var subgraph = new Subgraph(gn.Id.LiteralValue);
                    subgraphTable[subgraph.Id] = subgraph;
                    drawingNode = subgraph;
                }
                else
                    drawingNode = drawingGraph.AddNode(gn.Id.LiteralValue);

                drawingNode.Label = new Label(gn.Label) {Owner = drawingNode};
            }
        }
    }
}
#endif