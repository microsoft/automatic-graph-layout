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
using System.Diagnostics;
using System.IO;
using System.Windows.Documents;
using System.Xml;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.Drawing;

namespace TestWpfViewer {
    internal class GraphmlParser {
        readonly string _fileName;
        //the edges are kept under the nodes
        Dictionary<string, GraphmlNode> _graphmlNodes = new Dictionary<string, GraphmlNode>();
        XmlTextReader _xmlTextReader;
        XmlReader _xmlReader;

        public GraphmlParser(string fileName) {
            _fileName = fileName;
        }

        void MoveToContent() {
            _xmlReader.MoveToContent();
        }

        void XmlRead() {
            try {
                _xmlReader.Read();
            }
            catch (XmlException) {}
        }
    

        public Graph Parse() {

            using (Stream stream = File.OpenRead(_fileName)) {
                _xmlTextReader = new XmlTextReader(stream);
                _xmlReader = XmlReader.Create(_xmlTextReader, new XmlReaderSettings(){ DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true, IgnoreWhitespace = true, CheckCharacters = false});
                ParseUntilGraphElem();
                ParseUntilNode();
                ParseNodes();
                ParseEdges();
            }
            return GetGraph();
        }

        void ParseEdges() {
            Debug.Assert(Name == "edge");
            do {
                ParseEdge();
            } while (Name == "edge");
        }

        void ParseEdge() {
            var source = GetAttr("source");
            var target = GetAttr("target");
            var src = _graphmlNodes[source];
            var trg = _graphmlNodes[target];
            var edge = new GraphmlEdge(src, trg);
            src.outEdges.Insert(edge);
            trg.inEdges.Insert(edge);
            Skip();
            XmlRead();
        }

        string GetAttr(string attr) {
            var s = _xmlReader.GetAttribute(attr);
            if (s == null) {
                throw new InvalidDataException(String.Format("cannot find attr {0} at line{1} position {2}", attr,_xmlTextReader.LineNumber, _xmlTextReader.LinePosition));
                throw new InvalidDataException(String.Format("cannot find attr {0} at line{1} position {2}", attr,_xmlTextReader.LineNumber, _xmlTextReader.LinePosition));
            }
            return s;
        }

        void ParseNodes() {
            do {
                ReadNode();
            } while (Name == "node");
        }

        void ReadNode() {
            Debug.Assert(Name == "node");
            string id = GetAttr("id");
            GraphmlNode glnode;
            _graphmlNodes[id] = glnode = new GraphmlNode(id);
            FillFields(glnode);
        }

        void FillFields(GraphmlNode glnode) {
            do {
                XmlRead();
                while (Name == "data") {
                    var val = GetAttr("key");
                    if (val == "v_pubid") {
                        glnode.Pubid = _xmlReader.ReadElementContentAsString();
                    }
                    if (val == "label") {
                        glnode.Fullname = _xmlReader.ReadElementContentAsString();
                    }
                    Skip();
                    XmlRead();
                }

            } while (Name == "data");
            ReadEndElement();
        }

        void ReadEndElement() {
            _xmlReader.ReadEndElement();
        }

        void Skip() {
                _xmlReader.Skip();
        }

        public string Value { get { return _xmlReader.Value; } }

        string Name { get { return _xmlReader.Name; } }
        bool EOF { get { return _xmlReader.EOF; } }
        void ParseUntilNode() {
            do {
                XmlRead();
            } while (Name != "node" || EOF);
            CheckOnEof();
        }

        void ParseUntilGraphElem() {
           
            MoveToContent();
            do {
                XmlRead();
            } while (_xmlReader.Name != "graph" || _xmlReader.EOF);
            CheckOnEof();
        }

        void CheckOnEof() {
            if (_xmlReader.EOF) {
                throw new InvalidDataException("unexpected EOF");
            }
        }

        Graph GetGraph() {

            var graph = new Graph();
            foreach (var graphmlNodePair in _graphmlNodes) {
                Microsoft.Msagl.Drawing.Node dnode = graph.AddNode(graphmlNodePair.Key);
                dnode.UserData = graphmlNodePair;
                if (!string.IsNullOrEmpty(graphmlNodePair.Value.Fullname))
                    dnode.LabelText = graphmlNodePair.Value.Fullname;

            }
            foreach (var graphmlNode in _graphmlNodes.Values) {
                foreach (var edge in graphmlNode.outEdges) {
                    graph.AddEdge(graphmlNode.Id, edge.Target.Id).UserData = edge;
                }
            }
            return graph;
        }
    }
}