using Microsoft.Msagl.Layout.LargeGraphLayout;
#if TEST_MSAGL
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;

namespace Microsoft.Msagl.DebugHelpers.Persistence
{
    /// <summary>
    /// writes a GeometryGraph to a stream
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "GraphWriter")]
    public class GeometryGraphWriter
    {
        const string FileExtension = ".msagl.geom";
        Dictionary<Node, string> nodeIds = new Dictionary<Node, string>();
        Dictionary<Edge, int> edgeIds = new Dictionary<Edge, int>();
        /// <summary>
        /// 
        /// </summary>
        GeometryGraph graph;

        bool needToCloseXmlWriter = true;
        Stream stream;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="streamPar">the stream to write the graph into</param>
        /// <param name="graphP">the graph</param>
        /// <param name="settings">The settings to be written.</param>
        public GeometryGraphWriter(Stream streamPar, GeometryGraph graphP, LayoutAlgorithmSettings settings)
        {
            stream = streamPar;
            Graph = graphP;
            Settings = settings;
            var xmlWriterSettings = new XmlWriterSettings { Indent = true };
            XmlWriter = XmlWriter.Create(stream, xmlWriterSettings);
            EdgeEnumeration = graphP.Edges;
        }

        /// <summary>
        /// an empty constructor
        /// </summary>
        public GeometryGraphWriter() { }

        /// <summary>
        /// if set to true then the XmlWriter will be closed after the graph writing
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly")]
        public bool NeedToCloseXmlWriter
        {
            get { return needToCloseXmlWriter; }
            set { needToCloseXmlWriter = value; }
        }

        /// <summary>
        /// the stream to write the graph into
        /// </summary>
        public Stream Stream
        {
            get { return stream; }
            set { stream = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly")]
        public XmlWriter XmlWriter { get; set; }

        /// <summary>
        /// the graph
        /// </summary>
        public GeometryGraph Graph
        {
            get { return graph; }
            set { graph = value; }
        }

        /// <summary>
        /// The settings
        /// </summary>
        public LayoutAlgorithmSettings Settings { get; set; }

        /// <summary>
        /// saves the graph to a file
        /// </summary>
        public static void Write(GeometryGraph graph, string fileName)
        {
            Write(graph, null, fileName);
        }

        /// <summary>
        /// saves the graph and settings to a file
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1309:UseOrdinalStringComparison",
            MessageId = "System.String.EndsWith(System.String,System.StringComparison)"),
         SuppressMessage("Microsoft.Globalization", "CA1309:UseOrdinalStringComparison",
             MessageId = "System.String.EndsWith(System.String,System.Boolean,System.Globalization.CultureInfo)")]
        public static void Write(GeometryGraph graph, LayoutAlgorithmSettings settings, string fileName)
        {
            if (fileName == null) return;

            if (!fileName.EndsWith(FileExtension, StringComparison.InvariantCultureIgnoreCase))
                fileName += FileExtension;

            using (Stream stream = File.Open(fileName, FileMode.Create))
            {
                var graphWriter = new GeometryGraphWriter(stream, graph, settings);
                graphWriter.Write();
            }
        }

        /// <summary>
        /// Writes the graph to a file
        /// </summary>
        public void Write()
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                Open();
                WriteLayoutSettings();

                InitEdgeIds();
                WriteNodes();
                WriteClusters();
                WriteEdges();
                WriteLayers();
                Close();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

        void WriteLayers()
        {
            if (graph.LgData == null) return;
            WriteStartElement(GeometryToken.LgLevels);
            WriteLgEdgeInfos();
            WriteSortedLgInfos();
            Dictionary<Rail, int> railIds = CreateRailIds();

            for (int i = 0; i < graph.LgData.Levels.Count; i++)
            {
                WriteLevel(graph.LgData.Levels[i], railIds, graph.LgData.LevelNodeCounts[i]);
            }
            WriteEndElement();

            WriteStartElement(GeometryToken.LgSkeletonLevels);
            for (int i = 0; i < graph.LgData.SkeletonLevels.Count; i++)
            {
                WriteSkeletonLevel(graph.LgData.SkeletonLevels[i], railIds);
            }
            WriteEndElement();

        }

        void WriteLgEdgeInfos()
        {
            WriteStartElement(GeometryToken.LgEdgeInfos);
            foreach (var t in graph.LgData.GeometryEdgesToLgEdgeInfos)
            {
                var edge = t.Key;
                var ei = t.Value;
                WriteLgEdgeInfo(edge, ei);
            }
            WriteEndElement();
        }

        void WriteLgEdgeInfo(Edge edge, LgEdgeInfo ei)
        {
            WriteStartElement(GeometryToken.LgEdgeInfo);
            WriteAttribute(GeometryToken.EdgeId, edgeIds[edge]);
            WriteAttribute(GeometryToken.Rank, ei.Rank);
            WriteAttribute(GeometryToken.Zoomlevel, ei.ZoomLevel);
            WriteEndElement();
        }

        void WriteLevel(LgLevel level, Dictionary<Rail, int> railsToIds, int nodeCountOnLevel)
        {
            WriteStartElement(GeometryToken.Level);
            WriteAttribute(GeometryToken.NodeCountOnLevel, nodeCountOnLevel);
            WriteAttribute(GeometryToken.Zoomlevel, level.ZoomLevel);
            WriteLevelRails(level, railsToIds);
            WriteEndElement();
        }

        void WriteSkeletonLevel(LgSkeletonLevel level, Dictionary<Rail, int> railsToIds)
        {
            WriteStartElement(GeometryToken.SkeletonLevel);
            //WriteAttribute(GeometryToken.NodeCountOnLevel, nodeCountOnLevel);
            WriteAttribute(GeometryToken.Zoomlevel, level.ZoomLevel);
            WriteEndElement();
        }

        void WriteLevelRails(LgLevel level, Dictionary<Rail, int> railIds)
        {
            WriteStartElement(GeometryToken.RailsPerEdge);
            foreach (var t in level._railsOfEdges)
            {
                WriteEdgeRails(t.Key, t.Value, railIds);
            }
            WriteEndElement();
            WriteRailsGeometry(level, railIds);
        }

        void WriteRailsGeometry(LgLevel level, Dictionary<Rail, int> railIds)
        {
            WriteStartElement(GeometryToken.Rails);
            foreach (var rail in level._railDictionary.Values)
                WriteRail(rail, railIds[rail]);
            WriteEndElement();
        }

        void WriteRail(Rail rail, int railId)
        {
            WriteStartElement(GeometryToken.Rail);
            WriteAttribute(GeometryToken.Id, railId);
            WriteAttribute(GeometryToken.Zoomlevel, rail.ZoomLevel);
            if (rail.MinPassingEdgeZoomLevel != Double.MaxValue)
                WriteAttribute(GeometryToken.MinPassingEdgeZoomLevel, rail.MinPassingEdgeZoomLevel);
            Arrowhead ah = rail.Geometry as Arrowhead;
            if (ah != null)
            {
                WriteStartElement(GeometryToken.Arrowhead);
                WriteAttribute(GeometryToken.ArrowheadPosition, ah.TipPosition);
                WriteAttribute(GeometryToken.CurveAttachmentPoint, rail.CurveAttachmentPoint);
                WriteEndElement();
            }
            else
            {
                ICurve curve = rail.Geometry as ICurve;
                if (curve != null)
                    WriteICurve(curve);
                else
                    throw new InvalidOperationException();
            }
            WriteEndElement();
        }

        void WriteEdgeRails(Edge edge, Set<Rail> rails, Dictionary<Rail, int> railIds)
        {
            WriteStartElement(GeometryToken.EdgeRails);
            WriteAttribute(GeometryToken.EdgeId, edgeIds[edge]);
            List<string> railIdStrings = new List<string>();
            foreach (var rail in rails)
            {
                railIdStrings.Add(railIds[rail].ToString());
            }

            WriteAttribute(GeometryToken.EdgeRails, String.Join(" ", railIdStrings));
            WriteEndElement();
        }


        Dictionary<Rail, int> CreateRailIds()
        {
            var ret = new Dictionary<Rail, int>();
            int id = 0;
            foreach (var level in graph.LgData.Levels)
            {
                foreach (var rail in level._railDictionary.Values)
                {
                    if (ret.ContainsKey(rail))
                    {
                        continue;
                    }
                    ret[rail] = id++;
                }
            }
            return ret;
        }

        void WriteSortedLgInfos()
        {
            WriteStartElement(GeometryToken.LgNodeInfos);
            foreach (var lgNodeInfo in graph.LgData.SortedLgNodeInfos)
            {
                WriteLgNodeInfo(lgNodeInfo);
            }
            WriteEndElement();
        }

        void WriteLgNodeInfo(LgNodeInfo lgNodeInfo)
        {
            WriteStartElement(GeometryToken.LgNodeInfo);
            WriteAttribute(GeometryToken.Id, nodeIds[lgNodeInfo.GeometryNode]);
            WriteAttribute(GeometryToken.Rank, lgNodeInfo.Rank);
            WriteAttribute(GeometryToken.Zoomlevel, lgNodeInfo.ZoomLevel);
            WriteAttribute(GeometryToken.LabelVisibleFromScale, lgNodeInfo.LabelVisibleFromScale);
            WriteAttribute(GeometryToken.LabelOffset, lgNodeInfo.LabelOffset);
            WriteAttribute(GeometryToken.LabelWidthToHeightRatio, lgNodeInfo.LabelWidthToHeightRatio);
            WriteEndElement();
        }

        void InitEdgeIds()
        {
            int id = 0;
            foreach (var e in graph.Edges)
                edgeIds[e] = id++;
        }

        /// <summary>
        /// roman: Writes the graph to an Ipe file
        /// todo
        /// </summary>
        public void WriteIpe()
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                Open();
                WriteNodesIpe();
                Close();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

        void WriteClusters()
        {
            if (graph.RootCluster == null) return;

            WriteStartElement(GeometryToken.Clusters);

            MapClustersToIds(graph.RootCluster);

            foreach (var cluster in graph.RootCluster.AllClustersDepthFirstExcludingSelf())
                WriteCluster(cluster, nodeIds[cluster]);
            WriteEndElement();
        }

        void WriteCluster(Cluster cluster, string clusterId)
        {
            WriteStartElement(GeometryToken.Cluster);
            WriteAttribute(GeometryToken.Id, clusterId);
            WriteAttribute(GeometryToken.Barycenter, cluster.Barycenter);
            WriteChildClusters(cluster);
            WriteChildNodes(cluster);
            if (cluster.BoundaryCurve != null)
                WriteICurve(cluster.BoundaryCurve);
            WriteClusterRectBoundary(cluster.RectangularBoundary);
            WriteEndElement();
        }

        void WriteClusterRectBoundary(RectangularClusterBoundary recClBnd)
        {
            if (recClBnd == null) return;
            WriteStartElement(GeometryToken.RectangularClusterBoundary);

            WriteAttribute(GeometryToken.LeftMargin, recClBnd.LeftMargin);
            WriteAttribute(GeometryToken.RightMargin, recClBnd.RightMargin);
            WriteAttribute(GeometryToken.TopMargin, recClBnd.TopMargin);
            WriteAttribute(GeometryToken.BottomMargin, recClBnd.BottomMargin);
            if (recClBnd.DefaultMarginIsSet)
            {
                WriteAttribute(GeometryToken.DefaultLeftMargin, recClBnd.DefaultLeftMargin);
                WriteAttribute(GeometryToken.DefaultRightMargin, recClBnd.DefaultRightMargin);
                WriteAttribute(GeometryToken.DefaultTopMargin, recClBnd.DefaultTopMargin);
                WriteAttribute(GeometryToken.DefaultBottomMargin, recClBnd.DefaultBottomMargin);
            }
            WriteAttribute(GeometryToken.GenerateFixedConstraints, recClBnd.GenerateFixedConstraints);
            WriteAttribute(GeometryToken.GenerateFixedConstraintsDefault,
                recClBnd.GenerateFixedConstraintsDefault);
            WriteAttribute(GeometryToken.MinNodeHeight, recClBnd.MinHeight);
            WriteAttribute(GeometryToken.MinNodeWidth, recClBnd.MinWidth);
            WriteRect(recClBnd.Rect.Left, recClBnd.Rect.Bottom, recClBnd.Rect.Width, recClBnd.Rect.Height, recClBnd.RadiusX, recClBnd.RadiusY);
            WriteBorderInfo(GeometryToken.RightBorderInfo, recClBnd.RightBorderInfo);
            WriteBorderInfo(GeometryToken.LeftBorderInfo, recClBnd.LeftBorderInfo);
            WriteBorderInfo(GeometryToken.TopBorderInfo, recClBnd.TopBorderInfo);
            WriteBorderInfo(GeometryToken.BottomBorderInfo, recClBnd.BottomBorderInfo);
            WriteEndElement();
        }

        void WriteBorderInfo(GeometryToken token, BorderInfo borderInfo)
        {
            WriteStartElement(token);
            WriteAttribute(GeometryToken.InnerMargin, borderInfo.InnerMargin);
            WriteAttribute(GeometryToken.FixedPosition, borderInfo.FixedPosition);
            WriteAttribute(GeometryToken.Weight, borderInfo.Weight);
            WriteEndElement();
        }

        void WriteChildNodes(Cluster cluster)
        {
            WriteAttribute(GeometryToken.ChildNodes,
                string.Join(" ",
                cluster.nodes.Select(child => nodeIds[child].ToString(CultureInfo.InvariantCulture))));
        }

        void WriteChildClusters(Cluster cluster)
        {
            WriteAttribute(GeometryToken.ChildClusters,
            String.Join(" ", cluster.Clusters.Select(child => NodeToIds[child])));
        }

        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString"
            )]
        void MapClustersToIds(Cluster cluster)
        {
            string id;
            var setOfIds = new Set<string>(nodeIds.Values);
            foreach (Cluster child in cluster.AllClustersDepthFirst())
            {
                if (!nodeIds.TryGetValue(child, out id))
                {
                    id = FindNewId(setOfIds);
                    nodeIds[child] = id;
                    setOfIds.Insert(id);
                }
            }
        }



        string FindNewId(Set<string> setOfIds)
        {
            int i = nodeIds.Count;
            do
            {
                var s = i.ToString();
                if (!setOfIds.Contains(s))
                    return s;
                i++;
            } while (true);
        }


        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower")]
        void Open()
        {
            XmlWriter.WriteStartElement(GeometryToken.Graph.ToString().ToLower());
            WriteAttribute(GeometryToken.Margins, this.graph.Margins);
        }

        void Close()
        {
            XmlWriter.WriteEndElement();
            if (NeedToCloseXmlWriter)
            {
                XmlWriter.WriteEndDocument();
                XmlWriter.Flush();
                XmlWriter.Close();
            }
        }

        void WriteEdges()
        {
            WriteStartElement(GeometryToken.Edges);
            foreach (Edge edge in EdgeEnumeration)
                WriteEdge(edge);
            WriteEndElement();
        }

        string NodeOrClusterId(Node node)
        {
            return nodeIds[node];
        }

        void WriteEdge(Edge edge)
        {
            WriteStartElement(GeometryToken.Edge);
            WriteAttribute(GeometryToken.Id, edgeIds[edge]);
            WriteAttribute(GeometryToken.S, NodeOrClusterId(edge.Source).ToString(CultureInfo.InvariantCulture));
            WriteAttribute(GeometryToken.T, NodeOrClusterId(edge.Target).ToString(CultureInfo.InvariantCulture));
            if (edge.LineWidth != 1)
                WriteAttribute(GeometryToken.LineWidth, edge.LineWidth);
            if (edge.ArrowheadAtSource)
            {
                WriteAttribute(GeometryToken.As, edge.EdgeGeometry.SourceArrowhead.TipPosition);
                WriteDefaultDouble(GeometryToken.Asl, edge.EdgeGeometry.SourceArrowhead.Length,
                                   Arrowhead.DefaultArrowheadLength);
            }
            if (edge.ArrowheadAtTarget)
            {
                WriteAttribute(GeometryToken.At, edge.EdgeGeometry.TargetArrowhead.TipPosition);
                WriteDefaultDouble(GeometryToken.Atl, edge.EdgeGeometry.TargetArrowhead.Length,
                                   Arrowhead.DefaultArrowheadLength);
            }

            if (edge.Weight != 1)
                WriteAttribute(GeometryToken.Weight, edge.Weight);
            if (edge.Separation != 1)
                WriteAttribute(GeometryToken.Separation, edge.Separation);
            if (edge.Label != null)
                WriteLabel(edge.Label);
            WriteICurve(edge.Curve);
            WriteEndElement();
        }

        void WriteDefaultDouble(GeometryToken geometryToken, double val, double defaultValue)
        {
            if (val != defaultValue)
                WriteAttribute(geometryToken, DoubleToString(val));
        }

        void WriteAttribute(GeometryToken attrKind, object val)
        {
            var attrString = FirstCharToLower(attrKind);
            if (val is Point)
                XmlWriter.WriteAttributeString(attrString, PointToString((Point)val));
            else if (val is Double)
                XmlWriter.WriteAttributeString(attrString, DoubleToString((double)val));
            else
                XmlWriter.WriteAttributeString(attrString, val.ToString());
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        static internal string FirstCharToLower(GeometryToken attrKind)
        {
            var attrString = attrKind.ToString();
            attrString = attrString.Substring(0, 1).ToLower(CultureInfo.InvariantCulture) + attrString.Substring(1, attrString.Length - 1);
            return attrString;
        }

        string PointToString(Point start)
        {
            return DoubleToString(start.X) + " " + DoubleToString(start.Y);
        }

        string formatForDoubleString = "#.###########";

        int precision = 11;
        IEnumerable<Edge> edgeEnumeration;

        ///<summary>
        ///</summary>
        public int Precision
        {
            get { return precision; }
            set
            {
                precision = Math.Max(1, value);
                var s = new char[precision + 2];
                s[0] = '#';
                s[1] = '.';
                for (int i = 0; i < precision; i++)
                    s[2 + i] = '#';
                formatForDoubleString = new string(s);
            }

        }

        ///<summary>
        /// a mapping from nodes to their ids
        ///</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

        public Dictionary<Node, string> NodeToIds
        {
            get { return nodeIds; }
            set { nodeIds = value; }
        }

        /// <summary>
        /// this enumeration is used in a combination with GraphWriter, to dictate the order of edges
        /// </summary>
        public IEnumerable<Edge> EdgeEnumeration
        {
            get { return edgeEnumeration; }
            set { edgeEnumeration = value; }
        }

        string DoubleToString(double d)
        {
            return (Math.Abs(d) < 1e-11) ? "0" : d.ToString(formatForDoubleString, CultureInfo.InvariantCulture);
        }

        void WriteLabel(Label label)
        {
            WriteAttribute(GeometryToken.Label, LabelToString(label));
        }

        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object,System.Object)")]
        string LabelToString(Label label)
        {
            return String.Format("{0} {1} {2}", PointToString(label.Center), DoubleToString(label.Width), DoubleToString(label.Height));
        }

        void WriteNodes()
        {
            WriteStartElement(GeometryToken.Nodes);
            foreach (Node node in Graph.Nodes)
                WriteNode(node);
            WriteEndElement();
        }

        void WriteNodesIpe()
        {
            foreach (Node node in Graph.Nodes)
                WriteNodeIpe(node);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        void WriteNode(Node node)
        {
            string id;
            if (!nodeIds.TryGetValue(node, out id))
                nodeIds[node] = id = nodeIds.Count.ToString();
            WriteStartElement(GeometryToken.Node);
            WriteAttribute(GeometryToken.Id, id);
            if (node.Padding != GeometryGraphReader.NodeDefaultPadding)
                WriteAttribute(GeometryToken.Padding, node.Padding);
            WriteICurve(node.BoundaryCurve);
            WriteEndElement();
        }

        void WriteNodeIpe(Node node)
        {
            string id;
            if (!nodeIds.TryGetValue(node, out id))
                nodeIds[node] = id = nodeIds.Count.ToString();
            // todo: add Id as text
            if (node.BoundaryCurve != null)
            {
                WriteICurveIpe(node.BoundaryCurve);
                WriteLabelIpe(node.BoundaryCurve.BoundingBox.Center, "" + id);
            }
        }

        void WriteLabelIpe(Point p, string label)
        {
            XmlWriter.WriteStartElement("text");
            XmlWriter.WriteAttributeString("pos", p.X + " " + p.Y);
            XmlWriter.WriteAttributeString("halign", "center");
            XmlWriter.WriteAttributeString("valign", "center");
            XmlWriter.WriteString(label);
            XmlWriter.WriteEndElement();
        }

        void WriteICurve(ICurve iCurve)
        {
            if (iCurve == null) return;
            var rect = iCurve as RoundedRect;
            if (rect != null)
                WriteRect(rect.BoundingBox.Left, rect.BoundingBox.Bottom, rect.BoundingBox.Width,
                          rect.BoundingBox.Height, rect.RadiusX, rect.RadiusY);
            else
            {
                var c = iCurve as Curve;
                if (c != null)
                    WriteCurveInSvgStyle(c);
                else
                {
                    var ellipse = iCurve as Ellipse;
                    if (ellipse != null)
                        WriteEllipseInSvgStyle(ellipse);
                    else
                    {
                        var poly = iCurve as Polyline;
                        if (poly != null)
                            WritePolylineInSvgStyle(poly);
                        else
                        {
                            var ls = iCurve as LineSegment;
                            if (ls != null)
                                WriteLineSeg(ls);
                            else
                            {
                                var bs = iCurve as CubicBezierSegment;
                                if (bs != null)
                                {
                                    WriteBezierSegment(bs);
                                }
                                else
                                    throw new InvalidOperationException();
                            }
                        }

                    }
                }
            }
        }

        void WriteBezierSegment(CubicBezierSegment bs)
        {
            WriteStartElement(GeometryToken.CubicBezierSegment);
            WriteAttribute(GeometryToken.Points, PointsToString(bs.B(0), bs.B(1), bs.B(2), bs.B(3)));
            WriteEndElement();
        }

        void WriteICurveIpe(ICurve iCurve)
        {
            if (iCurve == null) return;
            var rect = iCurve as RoundedRect;
            if (rect != null)
                WriteRectIpe(rect.BoundingBox.Left, rect.BoundingBox.Bottom, rect.BoundingBox.Width,
                          rect.BoundingBox.Height, rect.RadiusX, rect.RadiusY);
            return;
        }


        void WriteRect(double x, double y, double width, double height, double rx, double ry)
        {
            WriteStartElement(GeometryToken.Rect);
            WriteAttribute(GeometryToken.X, x);
            WriteAttribute(GeometryToken.Y, y);
            WriteAttribute(GeometryToken.Width, width);
            WriteAttribute(GeometryToken.Height, height);
            if (rx > 0)
                WriteAttribute(GeometryToken.Rx, rx);
            if (ry > 0)
                WriteAttribute(GeometryToken.Ry, ry);
            WriteEndElement();
        }

        void WriteRectIpe(double x, double y, double width, double height, double rx, double ry)
        {
            XmlWriter.WriteStartElement("path");
            XmlWriter.WriteString("\n" + x + " " + y + " m\n");
            XmlWriter.WriteString((x + width) + " " + y + " l\n");
            XmlWriter.WriteString((x + width) + " " + (y + height) + " l\n");
            XmlWriter.WriteString(x + " " + (y + height) + " l\nh\n");
            XmlWriter.WriteEndElement();
        }


        void WritePolylineInSvgStyle(Polyline poly)
        {
            WriteStartElement(poly.Closed ? GeometryToken.Polygon : GeometryToken.Polyline);
            WriteAttribute(GeometryToken.Points, PointsToString(poly.ToArray()));
            WriteEndElement();
        }


        void WriteEllipseInSvgStyle(Ellipse ellipse)
        {
            if (ApproximateComparer.Close(ellipse.ParStart, 0) && ApproximateComparer.Close(ellipse.ParEnd, 2 * Math.PI)) { WriteFullEllipse(ellipse); }
            else
            {
                WriteEllepticalArc(ellipse);
            }
        }

        void WriteFullEllipse(Ellipse ellipse)
        {
            WriteStartElement(GeometryToken.Ellipse);
            WriteAttribute(GeometryToken.Cx, ellipse.Center.X);
            WriteAttribute(GeometryToken.Cy, ellipse.Center.Y);
            WriteAttribute(GeometryToken.Rx, ellipse.AxisA.Length);
            WriteAttribute(GeometryToken.Ry, ellipse.AxisB.Length);
            WriteEndElement();
        }


        // ReSharper disable UnusedParameter.Local
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "ellipse")]
        static void WriteEllepticalArc(Ellipse ellipse)
        {
            // ReSharper restore UnusedParameter.Local
            throw new NotImplementedException();
        }

        void WriteCurveInSvgStyle(Curve curve)
        {
            WriteStartElement(GeometryToken.Curve);
            WriteAttribute(GeometryToken.CurveData, CurveString(curve));
            WriteEndElement();
        }
        string CurveString(ICurve iCurve)
        {
            return String.Join(" ", CurveStringTokens(iCurve));
        }
        IEnumerable<string> CurveStringTokens(ICurve iCurve)
        {
            yield return "M";
            yield return PointToString(iCurve.Start);
            var curve = iCurve as Curve;
            var previousInstruction = 'w'; //a character that is not used by the SVG curve
            if (curve != null)
                for (int i = 0; i < curve.Segments.Count; i++)
                {
                    var segment = curve.Segments[i];
                    if (i != curve.Segments.Count - 1)
                        yield return SegmentString(segment, ref previousInstruction);
                    else
                    { //it is the last seg
                        if (segment is LineSegment && ApproximateComparer.Close(segment.End, iCurve.Start))
                            yield return "Z";
                        else
                            yield return SegmentString(segment, ref previousInstruction);
                    }
                }
        }

        string SegmentString(ICurve segment, ref char previousInstruction)
        {
            var ls = segment as LineSegment;
            if (ls != null)
            {
                var str = LineSegmentString(ls, previousInstruction);
                previousInstruction = 'L';
                return str;
            }
            var cubic = segment as CubicBezierSegment;
            if (cubic != null)
            {
                var str = CubicBezierSegmentToString(cubic, previousInstruction);
                previousInstruction = 'C';
                return str;
            }
            var ellipseArc = segment as Ellipse;
            if (ellipseArc != null)
            {
                previousInstruction = 'A';
                return EllipticalArcToString(ellipseArc);

            }
            throw new NotImplementedException();
        }

        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        string EllipticalArcToString(Ellipse ellipse)
        {
            /*
             * rx ry x-axis-rotation large-arc-flag sweep-flag x y
             * */
            //In general in an Msagl ellipse the axes don't have to be orthogonal: we have a possible bug here
            var rx = "A" + DoubleToString(ellipse.AxisA.Length);
            var ry = DoubleToString(ellipse.AxisB.Length);
            var xAxisRotation = DoubleToString(180 * Point.Angle(new Point(1, 0), ellipse.AxisA) / Math.PI);
            var largeArcFlag = Math.Abs(ellipse.ParEnd - ellipse.ParStart) >= Math.PI ? "1" : "0";
            var sweepFlagInt = ellipse.ParEnd > ellipse.ParStart ? 1 : 0; //it happens because of the y-axis orientation down in SVG
            if (AxesSwapped(ellipse.AxisA, ellipse.AxisB))
            {
                sweepFlagInt = sweepFlagInt == 1 ? 0 : 1;
            }
            var endPoint = PointToString(ellipse.End);
            return string.Join(" ", new[] { rx, ry, xAxisRotation, largeArcFlag, sweepFlagInt.ToString(), endPoint });
        }

        static bool AxesSwapped(Point axisA, Point axisB)
        {
            return axisA.X * axisB.Y - axisB.X * axisA.Y < 0;
        }

        string CubicBezierSegmentToString(CubicBezierSegment cubic, char previousInstruction)
        {
            var str = PointsToString(cubic.B(1), cubic.B(2), cubic.B(3));
            return previousInstruction == 'C' ? str : "C" + str;
        }


        string PointsToString(params Point[] points)
        {
            return String.Join(" ", points.Select(PointToString));
        }

        string LineSegmentString(LineSegment ls, char previousInstruction)
        {
            var str = PointToString(ls.End);
            return previousInstruction == 'L' ? str : "L" + str;
        }

        void WriteLineSeg(LineSegment ls)
        {
            WriteStartElement(GeometryToken.LineSegment);
            WriteAttribute(GeometryToken.Points, PointsToString(ls.Start, ls.End));
            WriteEndElement();
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters",
            MessageId = "System.Xml.XmlWriter.WriteComment(System.String)")]
        void WriteTransformation(PlaneTransformation transformation)
        {
            WriteStartElement(GeometryToken.Transform);
            XmlWriter.WriteComment("the order of elements is [0,0],[0,1],[0,2],[1,0],[1,1],[1,2]");
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    WriteTransformationElement(transformation[i, j]);
            WriteEndElement();
        }

        void WriteTransformationElement(double t)
        {
            WriteStringElement(GeometryToken.TransformElement, t);
        }


        /// <summary>
        /// writes the starte element with the token
        /// </summary>
        /// <param name="token"></param>
        void WriteStartElement(GeometryToken token)
        {
            XmlWriter.WriteStartElement(FirstCharToLower(token));
        }

        //static  void WriteStartElement(XmlWriter writer, Tokens token) {
        //    writer.WriteStartElement(token.ToString());
        //}

        /// <summary>
        /// WriteStringElement with double
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="element"></param>
        void WriteStringElement(GeometryToken tokens, double element)
        {
            XmlWriter.WriteElementString(tokens.ToString(), XmlConvert.ToString(element));
        }

        /// <summary>
        /// writes the end element
        /// </summary>
        void WriteEndElement()
        {
            XmlWriter.WriteEndElement();
        }

        /// <summary>
        /// 
        /// </summary>
        void WriteLayoutSettings()
        {
            if (Settings != null)
            {
                LayoutAlgorithmSettings settings = Settings;
                EdgeRoutingSettings routingSettings = settings.EdgeRoutingSettings;

                WriteStartElement(GeometryToken.LayoutAlgorithmSettings);
                WriteAttribute(GeometryToken.EdgeRoutingMode, (int)routingSettings.EdgeRoutingMode);

                var sugiyama = settings as SugiyamaLayoutSettings;
                if (sugiyama != null) WriteSugiyamaSettings(sugiyama);
                else
                {
                    var mds = settings as MdsLayoutSettings;
                    if (mds != null)
                    {
                        WriteAttribute(GeometryToken.LayoutAlgorithmType, GeometryToken.MdsLayoutSettings);
#if TEST_MSAGL
                        WriteAttribute(GeometryToken.Reporting, mds.Reporting);
#endif
                        WriteAttribute(GeometryToken.Exponent, mds.Exponent);
                        WriteAttribute(GeometryToken.IterationsWithMajorization, mds.IterationsWithMajorization);
                        WriteAttribute(GeometryToken.PivotNumber, mds.PivotNumber);
                        WriteAttribute(GeometryToken.RotationAngle, mds.RotationAngle);
                        WriteAttribute(GeometryToken.ScaleX, mds.ScaleX);
                        WriteAttribute(GeometryToken.ScaleY, mds.ScaleY);
                    }
                }

                XmlWriter.WriteEndElement();
            }
        }

        void WriteSugiyamaSettings(SugiyamaLayoutSettings sugiyama)
        {
            WriteAttribute(GeometryToken.LayoutAlgorithmType, GeometryToken.SugiyamaLayoutSettings);
            WriteAttribute(GeometryToken.MinNodeWidth, sugiyama.MinNodeWidth);
            WriteAttribute(GeometryToken.MinNodeHeight, sugiyama.MinNodeHeight);
            WriteAttribute(GeometryToken.AspectRatio, sugiyama.AspectRatio);
            WriteAttribute(GeometryToken.NodeSeparation, sugiyama.NodeSeparation);
#if TEST_MSAGL
            WriteAttribute(GeometryToken.Reporting, sugiyama.Reporting);
#endif
            WriteAttribute(GeometryToken.RandomSeedForOrdering, sugiyama.RandomSeedForOrdering);
            WriteAttribute(GeometryToken.NoGainStepsBound, sugiyama.NoGainAdjacentSwapStepsBound);
            WriteAttribute(GeometryToken.MaxNumberOfPassesInOrdering, sugiyama.MaxNumberOfPassesInOrdering);
            WriteAttribute(GeometryToken.RepetitionCoefficientForOrdering,
                               sugiyama.RepetitionCoefficientForOrdering);
            WriteAttribute(GeometryToken.GroupSplit, sugiyama.GroupSplit);
            WriteAttribute(GeometryToken.LabelCornersPreserveCoefficient,
                               sugiyama.LabelCornersPreserveCoefficient);
            WriteAttribute(GeometryToken.BrandesThreshold, sugiyama.BrandesThreshold);
            WriteAttribute(GeometryToken.LayerSeparation, sugiyama.LayerSeparation);
            WriteTransformation(sugiyama.Transformation);
        }
    }
}

#endif