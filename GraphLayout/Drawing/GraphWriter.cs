using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;
using Microsoft.Msagl.DebugHelpers.Persistence;

namespace Microsoft.Msagl.Drawing {
    ///<summary>
    ///</summary>
    public class GraphWriter {
        private readonly Graph graph;
        private readonly Stream stream;

        private readonly XmlWriter xmlWriter;

        ///<summary>
        ///</summary>
        ///<param name="streamPar"></param>
        ///<param name="graphP"></param>
        public GraphWriter(Stream streamPar, Graph graphP) {
            stream = streamPar;
            graph = graphP;
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;
            xmlWriter = XmlWriter.Create(stream, xmlWriterSettings);
        }

        ///<summary>
        ///</summary>
        public GraphWriter() {
        }

        ///<summary>
        ///</summary>
        public XmlWriter XmlWriter {
            get { return xmlWriter; }
        }

        /// <summary>
        /// Writes the graph to a file
        /// </summary>
        public void Write() {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try {
                Open();
                if (graph.UserData != null)
                    WriteUserData(graph.UserData);
                WriteGraphAttr(graph.Attr);
                WriteLabel(graph.Label);
                WriteSubgraphs();
                WriteNodes();
                WriteEdges();
                WriteGeometryGraph();
                Close();
            }
            finally {
                //restore the culture
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

        void WriteSubgraphs() {
            WriteStartElement(Tokens.Subgraphs);
            foreach (Subgraph node in graph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf())
                    WriteSubgraph(node);
            WriteEndElement();
        }


        static internal string FirstCharToLower(Tokens attrKind)
        {
            var attrString = attrKind.ToString();
            attrString = attrString.Substring(0, 1).ToLower(CultureInfo.InvariantCulture) + attrString.Substring(1, attrString.Length - 1);
            return attrString;
        }

        void WriteAttribute(Tokens attrKind, object val)
        {
            var attrString = FirstCharToLower(attrKind);          
            xmlWriter.WriteAttributeString(attrString, val.ToString());
        }


        void WriteSubgraph(Subgraph subgraph) {
            WriteStartElement(Tokens.Subgraph);
            var subgraphsString = String.Join(" ", subgraph.Subgraphs.Select(s => s.Id));
            WriteAttribute(Tokens.listOfSubgraphs, subgraphsString);
            var nodesString = String.Join(" ", subgraph.Nodes.Select(s => s.Id));
            WriteAttribute(Tokens.listOfNodes, nodesString);
            if (subgraph.UserData != null)
                WriteUserData(subgraph.UserData);
            WriteNodeAttr(subgraph.Attr);
            WriteLabel(subgraph.Label);
            WriteEndElement();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void WriteUserData(object o) {
            DataContractSerializer dcs = null;
            StringWriter sw;
            bool success = true;
            try {
                sw = WriteUserDataToStream(o, ref dcs);
            }
            catch (Exception e) {
                success = false;
                sw = WriteUserDataToStream(e.Message, ref dcs);
            }

            WriteStartElement(Tokens.UserData);
            if (success)
                WriteStringElement(Tokens.UserDataType, o.GetType().AssemblyQualifiedName);
            else
                WriteStringElement(Tokens.UserDataType, "string".GetType().AssemblyQualifiedName);

            WriteStringElement(Tokens.SerializedUserData, sw.ToString());
            WriteEndElement();
        }

        private static StringWriter WriteUserDataToStream(object obj, ref DataContractSerializer dcs) {
            dcs = new DataContractSerializer(obj.GetType());
            var sw = new StringWriter(CultureInfo.InvariantCulture);
            XmlWriter xw = XmlWriter.Create(sw);
            dcs.WriteObject(xw, obj);
            xw.Flush();
            return sw;
        }

        private void WriteLabel(Label label) {
            WriteStartElement(Tokens.Label);
            if (label != null && !String.IsNullOrEmpty(label.Text)) {
                WriteStringElement(Tokens.Text, label.Text);
                WriteStringElement(Tokens.FontName, label.FontName);
                WriteColorElement(Tokens.FontColor, label.FontColor);
                WriteStringElement(Tokens.FontStyle, (int)label.FontStyle);
                WriteStringElement(Tokens.FontSize, label.FontSize);
                WriteStringElement(Tokens.Width, label.Width);
                WriteStringElement(Tokens.Height, label.Height);
            }
            WriteEndElement();
        }

        private void WriteGraphAttr(GraphAttr graphAttr) {
            WriteStartElement(Tokens.GraphAttribute);
            WriteBaseAttr(graphAttr);
            WriteMinNodeHeight();
            WriteMinNodeWidth();
            WriteAspectRatio();
            WriteBorder();
            WriteColorElement(Tokens.BackgroundColor, graphAttr.BackgroundColor);
            WriteStringElement(Tokens.Margin, graphAttr.Margin);
            WriteStringElement(Tokens.OptimizeLabelPositions, graphAttr.OptimizeLabelPositions);
            WriteStringElement(Tokens.NodeSeparation, graphAttr.NodeSeparation);
            WriteStringElement(Tokens.LayerDirection, graphAttr.LayerDirection);
            WriteStringElement(Tokens.LayerSeparation, graphAttr.LayerSeparation);
            WriteEndElement();
        }

        private void WriteBorder() {
            WriteStringElement(Tokens.Border, graph.Attr.Border);
        }

        private void WriteMinNodeWidth() {
            WriteStringElement(Tokens.MinNodeWidth, graph.Attr.MinNodeWidth);
        }

        private void WriteMinNodeHeight() {
            WriteStringElement(Tokens.MinNodeHeight, graph.Attr.MinNodeHeight);
        }


        private Color WriteColorElement(Tokens t, Color c) {
            WriteStartElement(t);
            WriteColor(c);
            WriteEndElement();
            return c;
        }

        private void WriteColor(Color color) {
            WriteStartElement(Tokens.Color);
            WriteStringElement(Tokens.A, color.A);
            WriteStringElement(Tokens.R, color.R);
            WriteStringElement(Tokens.G, color.G);
            WriteStringElement(Tokens.B, color.B);
            WriteEndElement();
        }

        private void WriteGeometryGraph() {
            if (graph.geomGraph != null) {
                WriteStringElement(Tokens.GeometryGraphIsPresent, true);
                var ggw = new GeometryGraphWriter {
                                                      Settings = graph.LayoutAlgorithmSettings,
                                                      XmlWriter = XmlWriter,
                                                      Stream = stream,
                                                      Graph = graph.GeometryGraph,
                                                      NeedToCloseXmlWriter = false,
                                                      NodeToIds=BuildGeomNodesAndClustersToIdsDictionary(),
                                                      EdgeEnumeration=graph.Edges.Select(e=>e.GeometryEdge)
                                                  };
                ggw.Write();
            }
            else
                WriteStringElement(Tokens.GeometryGraphIsPresent, false);
        }

        Dictionary<Core.Layout.Node, string> BuildGeomNodesAndClustersToIdsDictionary() {
            var d = new Dictionary<Core.Layout.Node, string>();
            var nodesAndClusters = graph.Nodes;
            if (graph.RootSubgraph != null)
                nodesAndClusters = nodesAndClusters.Concat(graph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf());
            foreach (var node in nodesAndClusters)
                d[node.GeometryNode] = node.Id;
            return d;
        }

        private void Open() {
            xmlWriter.WriteStartElement(Tokens.MsaglGraph.ToString());
        }

        private void Close() {
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Flush();
            xmlWriter.Close();
        }

        private void WriteEdges() {
            WriteStartElement(Tokens.Edges);
            foreach (Edge edge in graph.Edges)
                WriteEdge(edge);
            WriteEndElement();
        }

        private void WriteEdge(Edge edge) {
            WriteStartElement(Tokens.Edge);
            if (edge.UserData != null)
                WriteUserData(edge.UserData);
            WriteStringElement(Tokens.SourceNodeID, edge.Source);
            WriteStringElement(Tokens.TargetNodeID, edge.Target);
            WriteEdgeAttr(edge.Attr);
            WriteLabel(edge.Label);
            WriteEndElement();
        }

        private void WriteEdgeAttr(EdgeAttr edgeAttr) {
            WriteStartElement(Tokens.EdgeAttribute);
            WriteBaseAttr(edgeAttr);
            WriteStringElement(Tokens.EdgeSeparation, edgeAttr.Separation);
            WriteStringElement(Tokens.Weight, edgeAttr.Weight);
            WriteStringElement(Tokens.ArrowStyle, edgeAttr.ArrowheadAtSource);
            WriteStringElement(Tokens.ArrowStyle, edgeAttr.ArrowheadAtTarget);
            WriteStringElement(Tokens.ArrowheadLength, edgeAttr.ArrowheadLength);
            WriteEndElement();
        }


        private void WriteNodes() {
            WriteStartElement(Tokens.Nodes);
            foreach (Node node in graph.Nodes)
                WriteNode(node);
            WriteEndElement();
        }

        private void WriteNode(Node node) {
            WriteStartElement(Tokens.Node);
            if (node.UserData != null)
                WriteUserData(node.UserData);
            WriteNodeAttr(node.Attr);
            WriteLabel(node.Label);
            WriteEndElement();
        }

        private void WriteNodeAttr(NodeAttr na) {
            WriteStartElement(Tokens.NodeAttribute);
            WriteBaseAttr(na);
            WriteColorElement(Tokens.Fillcolor, na.FillColor);
            WriteStringElement(Tokens.LabelMargin, na.LabelMargin);
            WriteStringElement(Tokens.Padding, na.Padding);
            WriteStringElement(Tokens.Shape, na.Shape);
            WriteStringElement(Tokens.XRad, na.XRadius);
            WriteStringElement(Tokens.YRad, na.YRadius);
            WriteEndElement();
        }

        private void WriteBaseAttr(AttributeBase baseAttr) {
            WriteStartElement(Tokens.BaseAttr);
            WriteStyles(baseAttr.Styles);
            WriteColorElement(Tokens.Color, baseAttr.Color);
            WriteStringElement(Tokens.LineWidth, baseAttr.LineWidth);
            WriteStringElement(Tokens.ID, baseAttr.Id == null ? "" : baseAttr.Id);
            WriteEndElement();
        }

        private void WriteStyles(IEnumerable<Style> styles) {
            WriteStartElement(Tokens.Styles);
            foreach (Style s in styles)
                WriteStringElement(Tokens.Style, s);
            WriteEndElement();
        }

        private void WriteAspectRatio() {
            WriteStringElement(Tokens.AspectRatio, graph.Attr.AspectRatio);
        }

        private void WriteEndElement() {
            xmlWriter.WriteEndElement();
        }

        private void WriteStartElement(Tokens t) {
            xmlWriter.WriteStartElement(t.ToString());
        }

        private void WriteStringElement(Tokens t, object s) {
            xmlWriter.WriteElementString(t.ToString(), s.ToString());
        }
    }
}