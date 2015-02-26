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

namespace TestWpfViewer {
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

        void ReadNodeFeatures(Node node) {
            GexfNodeAttr gexfNodeAttr;
            idsToGexfNodeAttr[node.Id] = gexfNodeAttr = new GexfNodeAttr();
            while (xmlReader.IsStartElement()) {
                switch (xmlReader.Name) {
                    case "viz:color":
                        ReadColor(node);
                        break;
                    case "viz:position":
                        ReadPosition(gexfNodeAttr);
                        break;
                    case "viz:size":
                        ReadSize(gexfNodeAttr);
                        break;
                    default:
                        xmlReader.Skip();
                        break;
                }
                xmlReader.Read();
            }
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