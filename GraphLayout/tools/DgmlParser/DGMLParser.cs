using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Msagl.Drawing;

namespace DgmlParser {
    public static class DgmlParser {
        public static Microsoft.Msagl.Drawing.Graph Parse(string filename) {
            XDocument doc = XDocument.Load(filename);
            var drawingGraph = new Microsoft.Msagl.Drawing.Graph();

            // Parse nodes
            var nodes = doc.Descendants().Where(e => e.Name.LocalName == "Node");
            foreach (var nodeElement in nodes) {
                string id = nodeElement.Attribute("Id")?.Value;
                if (id == null)
                    continue;

                string label = nodeElement.Attribute("Label")?.Value ?? id;

                var node = drawingGraph.AddNode(id);
                node.LabelText = label;
            }

            // Parse links
            var links = doc.Descendants().Where(e => e.Name.LocalName == "Link");
            foreach (var linkElement in links) {
                string sourceId = linkElement.Attribute("Source")?.Value;
                string targetId = linkElement.Attribute("Target")?.Value;

                if (sourceId != null && targetId != null) {
                    var edge = drawingGraph.AddEdge(sourceId, targetId);
                    // Optionally set edge attributes
                    string label = linkElement.Attribute("Label")?.Value;
                    if (!string.IsNullOrEmpty(label)) {
                        edge.LabelText = label;
                    }
                }
            }
            return drawingGraph;
        }

        
        

    }
}
