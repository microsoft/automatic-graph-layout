using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;
using System.Xml;
using System.Runtime.Serialization;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.DebugHelpers.Persistence;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// reads a drawing graph from a stream
    /// </summary>
    public class GraphReader {
        /// <summary>
        /// the list of edges, needed to match it with GeometryGraphReader edges
        /// </summary>
        public IList<Edge> EdgeList = new List<Edge>();
        Stream stream;
        Graph graph = new Graph();
        XmlReader xmlReader;
        Dictionary<string, SubgraphTemplate> subgraphTable=new Dictionary<string, SubgraphTemplate>();
        GeometryGraphReader geometryGraphReader;

        internal GraphReader(Stream streamP) {
            stream = streamP;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreWhitespace = true;
            readerSettings.IgnoreComments = true;
            xmlReader = XmlReader.Create(stream, readerSettings);
        }
        
        /// <summary>
        /// Reads the graph from a file
        /// </summary>
        /// <returns></returns>
        internal Graph Read() {
            System.Globalization.CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            try {
            ReadGraph();
            } finally { System.Threading.Thread.CurrentThread.CurrentCulture = currentCulture; }
            return graph;
        }

        private void ReadGraph() {
            MoveToContent();
            CheckToken(Tokens.MsaglGraph);
            XmlRead();
            if (TokenIs(Tokens.UserData))
                graph.UserData = ReadUserData();
            ReadAttr();
            ReadLabel(graph);
            bool done = false;
            do
            {
                switch (GetElementTag())
                {
                    case Tokens.Nodes:
                        ReadNodes();
                        break;
                    case Tokens.Edges:
                        FleshOutSubgraphs(); // for this moment the nodes and the clusters have to be set already
                        ReadEdges();
                        break;
                    case Tokens.Subgraphs:
                        ReadSubgraphs();
                        break;
                    case Tokens.graph:
                        ReadGeomGraph();
                        break;
                    case Tokens.End:
                        done = true;
                        break;
                    default: // ignore this element
                        xmlReader.Skip();
                        break;
                }
            } while (!done);
          
                     
        }

        void FleshOutSubgraphs() {
            foreach (var subgraphTemlate in subgraphTable.Values) {
                var subgraph = subgraphTemlate.Subgraph;
                foreach (var id in subgraphTemlate.SubgraphIdList)
                    subgraph.AddSubgraph(subgraphTable[id].Subgraph);
                foreach (var id in subgraphTemlate.NodeIdList)
                    subgraph.AddNode(this.graph.FindNode(id));
            }
            var rootSubgraphSet = new Set<Subgraph>();
            foreach (var subgraph in subgraphTable.Values.Select(c => c.Subgraph))
                if (subgraph.ParentSubgraph == null) {
                    rootSubgraphSet.Insert(subgraph);
                    graph.RootSubgraph.AddSubgraph(subgraph);
                }

            if (rootSubgraphSet.Count == 1)
                graph.RootSubgraph = rootSubgraphSet.First();
            else 
                foreach (var subgraph in rootSubgraphSet)
                    graph.RootSubgraph.AddSubgraph(subgraph);
        }

        void ReadSubgraphs() {
            xmlReader.Read();
            while (TokenIs(Tokens.Subgraph))
                ReadSubgraph();

            if (!xmlReader.IsStartElement())
                ReadEndElement();
            
        }

        void ReadSubgraph() {
            var listOfSubgraphs = xmlReader.GetAttribute(Tokens.listOfSubgraphs.ToString());
            var subgraphTempl = new SubgraphTemplate();
            if (!string.IsNullOrEmpty(listOfSubgraphs)) {
                subgraphTempl.SubgraphIdList.AddRange(listOfSubgraphs.Split(' '));
            }
            var listOfNodes = xmlReader.GetAttribute(Tokens.listOfNodes.ToString());
            if (!string.IsNullOrEmpty(listOfNodes)) {
                subgraphTempl.NodeIdList.AddRange(listOfNodes.Split(' '));
            }

            xmlReader.Read();
            var subgraph = ReadSubgraphContent();
            subgraphTempl.Subgraph = subgraph;
            subgraphTable[subgraph.Id] = subgraphTempl;
        }

        private object ReadUserData()
        {
            CheckToken(Tokens.UserData);
            XmlRead();
            string typeString = ReadStringElement(Tokens.UserDataType);
            string serString = ReadStringElement(Tokens.SerializedUserData);
            ReadEndElement();

            Type t = Type.GetType(typeString);
            DataContractSerializer dcs = new DataContractSerializer(t);
            StringReader sr = new StringReader(serString);
            XmlReader xr = XmlReader.Create(sr);

            return dcs.ReadObject(xr,true);
        }

        private void ReadLabel(DrawingObject parent) {
            CheckToken(Tokens.Label);
            bool hasLabel = !this.xmlReader.IsEmptyElement;
            if (hasLabel) {
                XmlRead();
                Label label = new Label {
                    Text = ReadStringElement(Tokens.Text),
                    FontName = ReadStringElement(Tokens.FontName),
                    FontColor = ReadColorElement(Tokens.FontColor),
                    FontStyle = TokenIs(Tokens.FontStyle) ? (FontStyle)ReadIntElement(Tokens.FontStyle) : FontStyle.Regular,
                    FontSize = ReadDoubleElement(Tokens.FontSize),
                    Width = ReadDoubleElement(Tokens.Width),
                    Height = ReadDoubleElement(Tokens.Height),
                    Owner = parent
                };
                ((ILabeledObject) parent).Label = label;

                ReadEndElement();
            }
            else {
                var node = parent as Node;
                if (node != null){//we still need a label!
                    Label label = new Label {
                        Text = node.Id,                       
                        Owner = parent
                    };
                    ((ILabeledObject) parent).Label = label;
                }
                xmlReader.Skip();
            }
        }

        void ReadGeomGraph() {
            geometryGraphReader = new GeometryGraphReader();
            geometryGraphReader.SetXmlReader(this.xmlReader);
            GeometryGraph geomGraph = geometryGraphReader.Read();
            BindTheGraphs(this.graph, geomGraph, graph.LayoutAlgorithmSettings);
        }

        void BindTheGraphs(Graph drawingGraph, GeometryGraph geomGraph, LayoutAlgorithmSettings settings) {
            drawingGraph.GeometryGraph = geomGraph;

            foreach (Node dn in drawingGraph.NodeMap.Values) {
                var geomNode = dn.GeometryNode = geometryGraphReader.FindNodeById(dn.Id);
                geomNode.UserData = dn;
            }

            foreach (var subgraph in drawingGraph.RootSubgraph.AllSubgraphsDepthFirst()) {
                var geomNode = subgraph.GeometryNode = geometryGraphReader.FindClusterById(subgraph.Id);
                if (geomNode != null)
                    geomNode.UserData = subgraph;
            }
            //  geom edges have to appear in the same order as drawing edges
            for(int i = 0;i < EdgeList.Count;i++) {
                var drawingEdge = EdgeList[i];
                var geomEdge = geometryGraphReader.EdgeList[i];
                drawingEdge.GeometryEdge = geomEdge;
                geomEdge.UserData = drawingEdge;
                if(drawingEdge.Label != null) {
                    drawingEdge.Label.GeometryLabel = geomEdge.Label;
                    geomEdge.Label.UserData = drawingEdge.Label;
                }
            }
                
            drawingGraph.LayoutAlgorithmSettings = settings;
        }

        private void ReadEdges() {
            CheckToken(Tokens.Edges);

            if (xmlReader.IsEmptyElement) {
                XmlRead();
                return;
            }

            XmlRead();
            while (TokenIs(Tokens.Edge))
                ReadEdge();
            ReadEndElement();
        }

        private void ReadEdge() {
            CheckToken(Tokens.Edge);
            XmlRead();
            object userData = null;
            if (TokenIs(Tokens.UserData))
                userData = ReadUserData();
            Edge edge=graph.AddEdge(ReadStringElement(Tokens.SourceNodeID), ReadStringElement(Tokens.TargetNodeID));
            
            edge.Attr = new EdgeAttr();
            edge.UserData = userData;
            ReadEdgeAttr(edge.Attr);
            ReadLabel(edge);
            EdgeList.Add(edge);
            ReadEndElement();
        }


        Tokens GetElementTag()
        {
            Tokens token;
            if (xmlReader.ReadState == ReadState.EndOfFile)
                return Tokens.End;
            if (Enum.TryParse(xmlReader.Name, true, out token))
                return token;
            throw new InvalidOperationException();
        }

        private void ReadEdgeAttr(EdgeAttr edgeAttr) {
            CheckToken(Tokens.EdgeAttribute);
            XmlRead();
            ReadBaseAttr(edgeAttr);
            edgeAttr.Separation = ReadIntElement(Tokens.EdgeSeparation);
            edgeAttr.Weight = ReadIntElement(Tokens.Weight);
            edgeAttr.ArrowheadAtSource = (ArrowStyle)Enum.Parse(typeof(ArrowStyle), ReadStringElement(Tokens.ArrowStyle), false);
            edgeAttr.ArrowheadAtTarget = (ArrowStyle)Enum.Parse(typeof(ArrowStyle), ReadStringElement(Tokens.ArrowStyle), false);
            edgeAttr.ArrowheadLength =(float) ReadDoubleElement(Tokens.ArrowheadLength);
            ReadEndElement();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "token")]
        private int ReadIntElement(Tokens token) {
            CheckToken(token);
            return ReadElementContentAsInt();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "token")]
        private string ReadStringElement(Tokens token) {
            CheckToken(token);
            return ReadElementContentAsString();
        }


        private int ReadElementContentAsInt() {
            return xmlReader.ReadElementContentAsInt();
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "token")]
        private bool ReadBooleanElement(Tokens token) {
            CheckToken(token);
            return ReadElementContentAsBoolean();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "token")]
        private double ReadDoubleElement(Tokens token) {
            CheckToken(token);
            return ReadElementContentAsDouble();
        }

        private void ReadNodes() {
            CheckToken(Tokens.Nodes);
            XmlRead();
            while (TokenIs(Tokens.Node))
                ReadNode();
            ReadEndElement();
        }

        private void ReadNode() {
            CheckToken(Tokens.Node);
            ReadNodeContent();
        }

        Subgraph ReadSubgraphContent()
        {
            object userData = null;
            if (TokenIs(Tokens.UserData))
                userData = ReadUserData();
            var nodeAttr = new NodeAttr();
            ReadNodeAttr(nodeAttr);
            var subgraph = new Subgraph(nodeAttr.Id) {Label = null, Attr = nodeAttr, UserData = userData};
            ReadLabel(subgraph);
            ReadEndElement();
            return subgraph;
        }

        void ReadNodeContent() {
            XmlRead();
            object userData = null;
            if (TokenIs(Tokens.UserData))
                userData = ReadUserData();
            var nodeAttr = new NodeAttr();
            ReadNodeAttr(nodeAttr);
            var node = graph.AddNode(nodeAttr.Id);
            node.Label = null;
            node.Attr = nodeAttr;
            node.UserData = userData;
            ReadLabel(node);
            ReadEndElement();
        }

        private void ReadNodeAttr(NodeAttr na) {
            CheckToken(Tokens.NodeAttribute); 
            XmlRead();
            ReadBaseAttr(na);
            na.FillColor = ReadColorElement(Tokens.Fillcolor);
            na.LabelMargin=ReadIntElement(Tokens.LabelMargin);
            na.Padding=ReadDoubleElement(Tokens.Padding);
            na.Shape = (Shape) Enum.Parse(typeof(Shape), ReadStringElement(Tokens.Shape), false);
            na.XRadius=ReadDoubleElement(Tokens.XRad);
            na.YRadius=ReadDoubleElement(Tokens.YRad);
            ReadEndElement();
       
        }

        private void ReadBaseAttr(AttributeBase baseAttr) {
            CheckToken(Tokens.BaseAttr);
            XmlRead();
            ReadStyles(baseAttr);
            baseAttr.Color = ReadColorElement(Tokens.Color);
            baseAttr.LineWidth = ReadDoubleElement(Tokens.LineWidth);
            baseAttr.Id = ReadStringElement(Tokens.ID);
            ReadEndElement();
        }

        private void ReadStyles(AttributeBase baseAttr) {
            CheckToken(Tokens.Styles);
            XmlRead();
            bool haveStyles = false;
            while (TokenIs(Tokens.Style)) {
                baseAttr.AddStyle((Style)Enum.Parse(typeof(Style), ReadStringElement(Tokens.Style), false));
                haveStyles = true;
            }
            if (haveStyles)
                ReadEndElement();
        }

        private void ReadEndElement() {
            xmlReader.ReadEndElement();
        }

        private string ReadElementContentAsString() {
            return xmlReader.ReadElementContentAsString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)"), System.Diagnostics.Conditional("TEST_MSAGL")]
        private void CheckToken(Tokens t) {
            if (!xmlReader.IsStartElement(t.ToString())) {
                throw new InvalidDataException(String.Format("expecting {0}", t));
            }

        }

        private bool TokenIs(Tokens t) {
            return xmlReader.IsStartElement(t.ToString());
        }


        private void ReadAttr() {
            CheckToken(Tokens.GraphAttribute);
            XmlRead();
            ReadBaseAttr(graph.Attr);
            ReadMinNodeHeight();
            ReadMinNodeWidth();
            ReadAspectRatio();
            ReadBorder();
            graph.Attr.BackgroundColor = ReadColorElement(Tokens.BackgroundColor);
            graph.Attr.Margin = ReadDoubleElement(Tokens.Margin);
            graph.Attr.OptimizeLabelPositions = ReadBooleanElement(Tokens.OptimizeLabelPositions);
            graph.Attr.NodeSeparation = ReadDoubleElement(Tokens.NodeSeparation);
            graph.Attr.LayerDirection = (LayerDirection)Enum.Parse(typeof(LayerDirection), ReadStringElement(Tokens.LayerDirection), false);
            graph.Attr.LayerSeparation = ReadDoubleElement(Tokens.LayerSeparation);
            ReadEndElement();
        }

        private void ReadBorder() {
            graph.Attr.Border = ReadIntElement(Tokens.Border);
        }

        private void ReadMinNodeWidth() {
            this.graph.Attr.MinNodeWidth = ReadDoubleElement(Tokens.MinNodeWidth);
        }

        private void ReadMinNodeHeight() {
            this.graph.Attr.MinNodeHeight = ReadDoubleElement(Tokens.MinNodeHeight);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "token")]
        Color ReadColorElement(Tokens token) {
            CheckToken(token);
            XmlRead();
            Color c = ReadColor();
            ReadEndElement();
            return c;
        }

        Color ReadColor() {
            CheckToken(Tokens.Color);
            XmlRead();
            Color c = new Color(ReadByte(Tokens.A), ReadByte(Tokens.R), ReadByte(Tokens.G), ReadByte(Tokens.B));
            ReadEndElement();
            return c;
        }

        private byte ReadByte(Tokens token) {
            return (byte)ReadIntElement(token);
        }

        private void ReadAspectRatio() {
            CheckToken(Tokens.AspectRatio);
            graph.Attr.AspectRatio = ReadElementContentAsDouble();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToBoolean(System.String)")]
        private bool ReadElementContentAsBoolean() {
            return Convert.ToBoolean(xmlReader.ReadElementContentAsString());
        }

     
        private double ReadElementContentAsDouble() {
            return xmlReader.ReadElementContentAsDouble();
        }

        private void MoveToContent() {
            xmlReader.MoveToContent();
        }

        private void XmlRead() {
            xmlReader.Read();
        }
    }
}
