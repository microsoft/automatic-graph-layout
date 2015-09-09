using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Routing;
using Node = Microsoft.Msagl.Drawing.Node;

namespace TestGraphmaps {
    internal class GexfParser {
        XmlReader xmlReader;
        XmlTextReader xmlTextReader;
        Graph graph;
        Dictionary<string,GexfNodeAttr> idsToGexfNodeAttr=new Dictionary<string,GexfNodeAttr>();
        
        GexfParser(Stream stream) {
            var settings = new XmlReaderSettings {IgnoreComments = false, IgnoreWhitespace = true};
            xmlTextReader = new XmlTextReader(stream);
            xmlReader = XmlReader.Create(xmlTextReader, settings);
            graph = new Graph();
        }


        public static Graph Parse(string fileName, out int line, out int column, out string msg) {
            using (Stream stream = File.OpenRead(fileName)) {
                line = 0;
                column = 0;
                msg = "";
                var gexfReader = new GexfParser(stream);
                return gexfReader.Run();
            }
        }

        Graph Run() {
            xmlReader.MoveToContent();
            while (IsStartElement()) {
                switch (Name) {
                    case "edges":
                        ReadEdges();
                        break;
                    case "nodes":
                        ReadNodes();
                        break;
                    case "gexf":
                        Read();
                        break;
                    case "graph":
                        Read();
                        break;
                    default:
                        xmlReader.Skip();
                        break;
                }               
            }
            if (!GeometryPresent(graph.Nodes))
                return graph;
            graph.CreateGeometryGraph();
            foreach (var n in graph.Nodes) {
                var geomNode = n.GeometryNode;

                GexfNodeAttr nodeData;
                if (idsToGexfNodeAttr.TryGetValue(n.Id, out nodeData)) {
                    n.Label.FontSize *= nodeData.Size;
                    geomNode.BoundaryCurve = CurveFactory.CreateCircle(nodeData.Size, nodeData.Position);
                }
            }
            foreach (var e in graph.Edges) {
                if (e.GeometryEdge.Source.BoundaryCurve != null && e.GeometryEdge.Target.BoundaryCurve != null)
                StraightLineEdges.RouteEdge(e.GeometryEdge, e.Source==e.Target?graph.LayoutAlgorithmSettings.NodeSeparation/4: 0);
            }
            return graph;
        }

        bool GeometryPresent(IEnumerable<Node> nodes) {
            return nodes.Count() == idsToGexfNodeAttr.Count && idsToGexfNodeAttr.Values.All(v => v.Size != 0) &&
                   idsToGexfNodeAttr.Values.Any(v => v.Position != new Point(0, 0));
        }

        void ReadEdges() {
            Read();
            while (IsStartElement() && Name == "edge")
                ReadEdge();
            xmlReader.ReadEndElement();
        }

        void ReadEdge() {            
            graph.AddEdge(GetAttr("source"), GetAttr("target"));
            do {
                Read();
            } while (Name != "edge" && Name != "edges");
            if(Name=="edge"&& xmlReader.NodeType==XmlNodeType.EndElement)
                xmlReader.ReadEndElement();
        }

        void ReadNodes() {
            xmlReader.Read();
            while (IsStartElement() && Name == "node")
                ReadNode();
            if(Name=="nodes")
                xmlReader.ReadEndElement();
        }

        string Name {
            get { return xmlReader.Name; }
        }

        bool IsStartElement() {
            return xmlReader.IsStartElement();
        }

        void ReadNode() {
            string id=xmlReader.GetAttribute("id");
            var node = graph.AddNode(id);
            ReadNodeContent(node);
            if (IsStartElement() && Name == "node") 
                return; 
            Read();
        }

        void ReadNodeContent(Node node) {
            var label = xmlReader.GetAttribute("label");
            if (label != null)
                node.LabelText = label;
            xmlReader.Read();
            if (IsStartElement() && Name == "node"
                || Name=="nodes") return;
            ReadNodeFeatures(node);
        }

        private void SetLabelFromAttrValues(Node node, GexfNodeAttr gexfNodeAttr)
        {
            if (gexfNodeAttr.Attvalues.Count > 0)
            {
                var first = true;
                String labelText = "";
                foreach (var attVal in gexfNodeAttr.Attvalues.Values)
                {
                    if (!first) labelText += " ";
                    labelText += attVal;
                    first = false;
                }
                node.LabelText = labelText;
            }
        }

        void ReadNodeFeatures(Node node) {
            GexfNodeAttr gexfNodeAttr;
            idsToGexfNodeAttr[node.Id] = gexfNodeAttr = new GexfNodeAttr();
            while (xmlReader.IsStartElement()) {
                switch (xmlReader.Name) {
                    case "viz:color":
                        ReadColor(node);
                        xmlReader.Read();
                        xmlReader.ReadEndElement();
                        break;
                    case "viz:position":
                        ReadPosition(gexfNodeAttr);
                        xmlReader.Read();
                        xmlReader.ReadEndElement();
                        break;
                    case "viz:size":
                        ReadSize(gexfNodeAttr);
                        xmlReader.Read();
                        xmlReader.ReadEndElement();
                        break;
                    case "attvalues":
                        ReadAttvalues(gexfNodeAttr);
                        break;
                    default:
                        xmlReader.Skip();
                        break;
                }
                //xmlReader.Read();
            }

            SetLabelFromAttrValues(node, gexfNodeAttr);
        }

        private void ReadAttvalues(GexfNodeAttr gexfNodeAttr)
        {
            xmlReader.Read();
            while (IsStartElement() && Name == "attvalue")
            {
                ReadAttvalue(gexfNodeAttr);
                if(!xmlReader.IsEmptyElement)
                    xmlReader.Read();
                xmlReader.ReadEndElement();
            }
            if(Name=="attvalues")
                xmlReader.ReadEndElement();
        }

        private void ReadAttvalue(GexfNodeAttr gexfNodeAttr)
        {
            var attFor = xmlReader.GetAttribute("for");
            var attVal = xmlReader.GetAttribute("value");
            if (attFor != null && attVal != null)
                gexfNodeAttr.Attvalues[attFor] = attVal;
        }

        void ReadSize(GexfNodeAttr gexfNodeAttr) {
            var sizeVal = xmlReader.GetAttribute("value");
            if (sizeVal != null)
                gexfNodeAttr.Size = double.Parse(sizeVal);
        }

        void ReadPosition(GexfNodeAttr gexfNodeAttr) {
            var xStr = GetAttr("x");
            if (xStr != null) {
                double x;
                if (double.TryParse(xStr, out x)) {
                    var yStr = GetAttr("y");
                    if(yStr!=null) {
                        double y;
                        if (double.TryParse(yStr, out y)) {
                            gexfNodeAttr.Position = new Point(x, y);
                        }
                    }
                }
            }
        }

        void Read() {
            xmlReader.Read();
        }

        string GetAttr(string a) {
            return xmlReader.GetAttribute(a);
        }

        void ReadColor(Node node) {
            var r = byte.Parse(GetAttr("r"));
            var g = byte.Parse(GetAttr("g"));
            var b = byte.Parse(GetAttr("b"));
            node.Attr.Color = new Color(r, g, b);
        }
    }
}