using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Prototype.Ranking;
#if TEST_MSAGL
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;

namespace Microsoft.Msagl.DebugHelpers.Persistence
{

    /// <summary>
    /// reads the GeometryGraph from a file
    /// </summary>
    public class GeometryGraphReader : IDisposable
    {
        /// <summary>
        /// the list of edges, needed to match it with GraphReader edges
        /// </summary>
        public IList<Edge> EdgeList = new List<Edge>();

        readonly Dictionary<string, Edge> idToEdges = new Dictionary<string, Edge>();
        readonly Dictionary<string, Rail> idToRails = new Dictionary<string, Rail>();
        readonly Dictionary<string, LgEdgeInfo> railIdsToTopRankedEdgeInfo = new Dictionary<string, LgEdgeInfo>();

        readonly GeometryGraph _graph = new GeometryGraph();

        /// <summary>
        /// The deserialized settings.
        /// </summary>
        public LayoutAlgorithmSettings Settings { get; set; }

        readonly Dictionary<string, ClusterWithChildLists> stringToClusters =
            new Dictionary<string, ClusterWithChildLists>();

        readonly Dictionary<string, Node> nodeIdToNodes = new Dictionary<string, Node>();
        readonly XmlTextReader xmlTextReader;

        /// <summary>
        /// an empty constructor
        /// </summary>
        public GeometryGraphReader()
        {
        }

        /// <summary>
        /// constructor witha given stream
        /// </summary>
        /// <param name="streamP"></param>
        public GeometryGraphReader(Stream streamP)
        {
            var settings = new XmlReaderSettings { IgnoreComments = false, IgnoreWhitespace = true };
            xmlTextReader = new XmlTextReader(streamP);
            XmlReader = XmlReader.Create(xmlTextReader, settings);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Cluster FindClusterById(string id)
        {
            ClusterWithChildLists cwl;
            if (stringToClusters.TryGetValue(id, out cwl))
                return cwl.Cluster;
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Node FindNodeById(string id)
        {
            Node node;
            if (nodeIdToNodes.TryGetValue(id, out node))
                return node;
            return null;
        }

        /// <summary>
        /// creates the graph from a given file
        /// </summary>
        /// <returns></returns>
        public static GeometryGraph CreateFromFile(string fileName)
        {
            LayoutAlgorithmSettings settings;
            return CreateFromFile(fileName, out settings);
        }

        /// <summary>
        /// creates the graph and settings from a given file
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public static GeometryGraph CreateFromFile(string fileName, out LayoutAlgorithmSettings settings)
        {
#if TEST_MSAGL
            if (FirstCharacter(fileName) != '<') {
                settings = null;
                return null;
            }
#endif
            using (Stream stream = File.OpenRead(fileName))
            {
                var graphReader = new GeometryGraphReader(stream);
                GeometryGraph graph = graphReader.Read();
                settings = graphReader.Settings;
                return graph;
            }
        }

#if TEST_MSAGL
        static char FirstCharacter(string fileName) {
            using (TextReader reader = File.OpenText(fileName))
            {
                var first = (char)reader.Peek();
                return first;
            }
        }
#endif

        /// <summary>
        /// Reads the graph from the stream
        /// </summary>
        /// <returns></returns>
        public GeometryGraph Read()
        {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                ReadGraph();
                return _graph;
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

        /// <summary>
        /// reads the layout algorithm settings
        /// </summary>
        LayoutAlgorithmSettings ReadLayoutAlgorithmSettings(XmlReader reader)
        {
            LayoutAlgorithmSettings layoutSettings = null;
            CheckToken(GeometryToken.LayoutAlgorithmSettings);
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return null;
            }
            //reader.Read();

            var edgeRoutingMode =
                (EdgeRoutingMode)GetIntAttributeOrDefault(GeometryToken.EdgeRoutingMode, (int)EdgeRoutingMode.Spline);
            var str = GetAttribute(GeometryToken.LayoutAlgorithmType);
            if (XmlReader.NodeType == XmlNodeType.EndElement)
            {
                //todo - support fastincremental settings
                layoutSettings = new FastIncrementalLayoutSettings();
                EdgeRoutingSettings routingSettings = layoutSettings.EdgeRoutingSettings;
                routingSettings.EdgeRoutingMode = edgeRoutingMode;
            }
            else
            {
                if (str != null)
                {
                    var token =
                        (GeometryToken)Enum.Parse(typeof(GeometryToken), str, false);
                    if (token == GeometryToken.SugiyamaLayoutSettings)
                    {
                        layoutSettings = ReadSugiyamaLayoutSettings(edgeRoutingMode);
                    }
                    else if (token == GeometryToken.MdsLayoutSettings)
                    {
                        var mds = new MdsLayoutSettings();
                        EdgeRoutingSettings routingSettings = mds.EdgeRoutingSettings;
                        routingSettings.EdgeRoutingMode = edgeRoutingMode;

                        layoutSettings = mds;
                        if (XmlReader.IsStartElement(GeometryToken.Reporting.ToString()))
                        {
#if TEST_MSAGL
                            mds.Reporting =
#endif
 ReadBooleanElement(GeometryToken.Reporting);
                        }
                        mds.Exponent = ReadDoubleElement(reader);
                        mds.IterationsWithMajorization = ReadIntElement(GeometryToken.IterationsWithMajorization);
                        mds.PivotNumber = ReadIntElement(GeometryToken.PivotNumber);
                        mds.RotationAngle = ReadDoubleElement(reader);
                        mds.ScaleX = ReadDoubleElement(reader);
                        mds.ScaleY = ReadDoubleElement(reader);
                    }
                    else //todo - write a reader and a writer for FastIncrementalLayoutSettings 
                        throw new NotImplementedException();
                }
            }
            reader.ReadEndElement();

            return layoutSettings;
        }

        LayoutAlgorithmSettings ReadSugiyamaLayoutSettings(EdgeRoutingMode edgeRoutingMode)
        {
            var sugiyama = new SugiyamaLayoutSettings();
            EdgeRoutingSettings routingSettings = sugiyama.EdgeRoutingSettings;
            routingSettings.EdgeRoutingMode = edgeRoutingMode;

            LayoutAlgorithmSettings layoutSettings = sugiyama;

            sugiyama.MinNodeWidth = GetDoubleAttributeOrDefault(GeometryToken.MinNodeWidth, sugiyama.MinNodeWidth);
            sugiyama.MinNodeHeight = GetDoubleAttributeOrDefault(GeometryToken.MinNodeHeight, sugiyama.MinimalHeight);
            sugiyama.AspectRatio = GetDoubleAttributeOrDefault(GeometryToken.AspectRatio, sugiyama.AspectRatio);
            sugiyama.NodeSeparation = GetDoubleAttributeOrDefault(GeometryToken.NodeSeparation, sugiyama.NodeSeparation);
            sugiyama.ClusterMargin = sugiyama.NodeSeparation;

#if TEST_MSAGL
            sugiyama.Reporting = GetBoolAttributeOrDefault(GeometryToken.Reporting, false);
#endif

            sugiyama.RandomSeedForOrdering = GetIntAttributeOrDefault(GeometryToken.RandomSeedForOrdering,
                sugiyama.RandomSeedForOrdering);
            sugiyama.NoGainAdjacentSwapStepsBound = GetIntAttributeOrDefault(GeometryToken.NoGainStepsBound,
                sugiyama.NoGainAdjacentSwapStepsBound);
            sugiyama.MaxNumberOfPassesInOrdering = GetIntAttributeOrDefault(GeometryToken.MaxNumberOfPassesInOrdering,
                sugiyama.MaxNumberOfPassesInOrdering);
            sugiyama.RepetitionCoefficientForOrdering = GetIntAttributeOrDefault(GeometryToken.
                RepetitionCoefficientForOrdering, sugiyama.RepetitionCoefficientForOrdering);
            sugiyama.GroupSplit = GetIntAttributeOrDefault(GeometryToken.GroupSplit, sugiyama.GroupSplit);
            sugiyama.LabelCornersPreserveCoefficient = GetDoubleAttribute(GeometryToken.LabelCornersPreserveCoefficient);
            sugiyama.BrandesThreshold = GetIntAttributeOrDefault(GeometryToken.BrandesThreshold,
                sugiyama.BrandesThreshold);
            sugiyama.LayerSeparation = GetDoubleAttributeOrDefault(GeometryToken.LayerSeparation,
                sugiyama.LayerSeparation);
            var transform = new PlaneTransformation();
            ReadTransform(transform);
            return layoutSettings;
        }


        void ReadTransform(PlaneTransformation transform)
        {
            XmlRead();
            if (TokenIs(GeometryToken.Transform))
            {
                XmlRead();
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 3; j++)
                    {
                        CheckToken(GeometryToken.TransformElement);
                        MoveToContent();
                        transform[i, j] = ReadElementContentAsDouble();
                    }
                XmlRead();
            }
            else
            {
                //set the unit transform
                transform[0, 0] = 1;
                transform[0, 1] = 0;
                transform[0, 2] = 0;
                transform[1, 0] = 0;
                transform[1, 1] = 1;
                transform[1, 2] = 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "token")]
        bool ReadBooleanElement(GeometryToken tokens)
        {
            CheckToken(tokens);
            return ReadElementContentAsBoolean();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool ReadElementContentAsBoolean()
        {
            return XmlReader.ReadElementContentAsBoolean();
        }

        int ReadIntElement(GeometryToken token)
        {
            CheckToken(token);
            return ReadElementContentAsInt();
        }


        static double ReadDoubleElement(XmlReader r)
        {
            return r.ReadElementContentAsDouble();
        }

        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower"),
         SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        void ReadGraph()
        {
            MoveToContent();
            _graph.Margins = GetDoubleAttributeOrDefault(GeometryToken.Margins, 10);
            if (XmlReader.Name.ToLower() != GeometryToken.Graph.ToString().ToLower())
                Error("expecting element \"graph\"");
            bool done = false;
            do
            {
                switch (GetElementTag())
                {
                    case GeometryToken.Nodes:
                        ReadNodes();
                        break;
                    case GeometryToken.Edges:
                        ReadEdges();
                        break;
                    case GeometryToken.Clusters:
                        ReadClusters();
                        break;
                    case GeometryToken.LayoutAlgorithmSettings:
                        Settings = ReadLayoutAlgorithmSettings(XmlReader); //todo . not tested
                        break;
                    case GeometryToken.LgLevels:
                        ReadLgLevels();
                        break;
                    case GeometryToken.LgSkeletonLevels:
                        ReadLgSkeletonLevels();
                        break;
                    case GeometryToken.End:
                    case GeometryToken.Graph:
                        if (XmlReader.NodeType == XmlNodeType.EndElement)
                        {
                            done = true;
                            ReadEndElement();
                            break;
                        }

                        //jyoti - added this if block for reloading msagl
                        if (XmlReader.NodeType == XmlNodeType.None)
                        {
                            done = true;
                            break;
                        }
                        XmlRead();
                        break;
                    default: //ignore this element
                        XmlReader.Skip();
                        break;

                    //                        XmlReader.Skip();
                    //                        ReadHeader();
                    //                        if (TokenIs(GeometryToken.LayoutAlgorithmSettings))
                    //                            this.Settings = ReadLayoutAlgorithmSettings(XmlReader);
                    //                        ReadNodes();
                    //                        ReadClusters();
                    //                        ReadEdges();
                }
            } while (!done);
            _graph.BoundingBox = _graph.PumpTheBoxToTheGraphWithMargins();
        }

        void ReadLgLevels()
        {
            LgData lgData = new LgData(_graph);
            _graph.LgData = lgData;
            FillLgData(lgData);
            ReadEndElement();
        }

        void ReadLgSkeletonLevels()
        {
            XmlRead();
            ReadSkeletonLevels(_graph.LgData);
            ReadEndElement();
        }

        void FillLgData(LgData lgData)
        {

            XmlRead();
            if (TokenIs(GeometryToken.LgEdgeInfos))
                ReadLgEdgeInfos(lgData);
            if (TokenIs(GeometryToken.LgNodeInfos))
            {
                ReadLgNodeInfos(lgData);
            }
            ReadLevels(lgData);
        }

        void ReadLgEdgeInfos(LgData lgData)
        {
            if (XmlReader.IsEmptyElement)
            {
                XmlRead();
                return;
            }

            XmlRead();
            while (TokenIs(GeometryToken.LgEdgeInfo))
                ReadLgEdgeInfo(lgData);
            ReadEndElement();
        }

        void ReadLgEdgeInfo(LgData lgData)
        {
            string edgeId = GetAttribute(GeometryToken.EdgeId);
            Edge edge = idToEdges[edgeId];
            lgData.GeometryEdgesToLgEdgeInfos[edge] = new LgEdgeInfo(edge)
            {
                Rank = GetDoubleAttribute(GeometryToken.Rank),
                ZoomLevel = GetDoubleAttribute(GeometryToken.Zoomlevel)
            };
            XmlRead();
        }

        void ReadLevels(LgData lgData)
        {
            int zoomLevel = 1;
            while (GetElementTag() == GeometryToken.Level)
            {
                var dZoomLevel = GetDoubleAttributeOrDefault(GeometryToken.Zoomlevel, zoomLevel);
                ReadLevel(lgData, (int)dZoomLevel);
                zoomLevel = 2 * (int)dZoomLevel;

            }
        }

        void ReadSkeletonLevels(LgData lgData)
        {
            int zoomLevel = 1;
            while (GetElementTag() == GeometryToken.SkeletonLevel)
            {
                var dZoomLevel = GetDoubleAttributeOrDefault(GeometryToken.Zoomlevel, zoomLevel);
                ReadSkeletonLevel(lgData, (int)dZoomLevel);
                zoomLevel = 2 * (int)dZoomLevel;
            }
        }

        void ReadLevel(LgData lgData, int zoomLevel)
        {
            int levelNodeCount = GetIntAttribute(GeometryToken.NodeCountOnLevel);
            if (lgData.LevelNodeCounts == null)
            {
                lgData.LevelNodeCounts = new List<int>();
            }
            lgData.LevelNodeCounts.Add(levelNodeCount);
            LgLevel level = new LgLevel(zoomLevel, _graph);
            lgData.Levels.Add(level);
            XmlRead();
            Dictionary<string, Set<string>> edgeIdToEdgeRailsSet = new Dictionary<string, Set<string>>();
            ReadRailIdsPerEdgeIds(lgData, edgeIdToEdgeRailsSet);
            ReadRails(level);
            ReadEndElement();
            FillRailsOfEdges(level, edgeIdToEdgeRailsSet);
        }

        void ReadSkeletonLevel(LgData lgData, int zoomLevel)
        {
            LgSkeletonLevel level = new LgSkeletonLevel() { ZoomLevel = zoomLevel };
            lgData.SkeletonLevels.Add(level);

            if (XmlReader.IsEmptyElement)
            {
                XmlRead();
                return;
            }

            XmlRead();
            //ReadSkeletonRails(level);            
            ReadEndElement();
            //level.CreateRailTree();
        }

        void FillRailsOfEdges(LgLevel level, Dictionary<string, Set<string>> edgeIdToEdgeRailsSet)
        {
            foreach (var edgeRails in edgeIdToEdgeRailsSet)
            {
                var edge = idToEdges[edgeRails.Key];
                var railSet = new Set<Rail>(edgeRails.Value.Where(s => s != "").Select(r => idToRails[r]));
                level._railsOfEdges[edge] = railSet;
            }
        }


        void ReadRailIdsPerEdgeIds(LgData lgData, Dictionary<string, Set<string>> edgeIdToEdgeRailsSet)
        {
            if (XmlReader.IsEmptyElement)
            {
                XmlRead();
                return;
            }

            XmlRead();
            while (TokenIs(GeometryToken.EdgeRails))
                ReadEdgeRailIds(lgData, edgeIdToEdgeRailsSet);
            ReadEndElement();
        }

        void ReadEdgeRailIds(LgData lgData, Dictionary<string, Set<string>> edgeIdToEdgeRailsSet)
        {
            string edgeId = GetAttribute(GeometryToken.EdgeId);
            Set<string> railIdSet;
            edgeIdToEdgeRailsSet[edgeId] = railIdSet = new Set<string>();
            string edgeRailsString = GetAttribute(GeometryToken.EdgeRails);
            LgEdgeInfo edgeInfo = lgData.GeometryEdgesToLgEdgeInfos[idToEdges[edgeId]];
            foreach (var railId in edgeRailsString.Split(' '))
            {
                UpdateToRankedEdgeInfoForRail(railId, edgeInfo);
                railIdSet.Insert(railId);
            }
            XmlRead();
        }

        void UpdateToRankedEdgeInfoForRail(string railId, LgEdgeInfo edgeInfo)
        {
            LgEdgeInfo topRankeEdgeInfo;
            if (railIdsToTopRankedEdgeInfo.TryGetValue(railId, out topRankeEdgeInfo))
            {
                if (topRankeEdgeInfo.Rank < edgeInfo.Rank)
                    railIdsToTopRankedEdgeInfo[railId] = edgeInfo;
            }
            else
                railIdsToTopRankedEdgeInfo[railId] = edgeInfo;
        }

        void ReadRails(LgLevel level)
        {

            CheckToken(GeometryToken.Rails);
            if (XmlReader.IsEmptyElement)
            {
                XmlRead();
                return;
            }
            XmlRead();
            while (TokenIs(GeometryToken.Rail))
            {
                ReadRail(level);
            }
            ReadEndElement();
        }

        void ReadSkeletonRails(LgSkeletonLevel level)
        {
            CheckToken(GeometryToken.Rails);
            if (XmlReader.IsEmptyElement)
            {
                XmlRead();
                return;
            }
            XmlRead();
            while (TokenIs(GeometryToken.Rail))
            {
                ReadSkeletonRail(level);
            }
            ReadEndElement();
        }

        void ReadRail(LgLevel level)
        {
            string railId = GetAttribute(GeometryToken.Id);
            int zoomLevel = (int)GetDoubleAttribute(GeometryToken.Zoomlevel);
            double minPassigEdgeZoomLevel =
                (double)GetDoubleAttributeOrDefault(GeometryToken.MinPassingEdgeZoomLevel, zoomLevel);
            var topRankedEdgoInfo = GetTopRankedEdgeInfoOfRail(railId);
            Rail rail = ContinueReadingRail(topRankedEdgoInfo, zoomLevel, level);
            rail.MinPassingEdgeZoomLevel = minPassigEdgeZoomLevel;
            idToRails[railId] = rail;

        }

        void ReadSkeletonRail(LgSkeletonLevel level)
        {
            // do not save rails in skeleton level;
            return;
        }

        Rail ContinueReadingRail(LgEdgeInfo topRankedEdgoInfo, int zoomLevel, LgLevel level)
        {
            XmlRead();
            string pointString;
            if (TokenIs(GeometryToken.Arrowhead))
            {
                Point arrowheadPosition = TryGetPointAttribute(GeometryToken.ArrowheadPosition);
                Point attachmentPoint = TryGetPointAttribute(GeometryToken.CurveAttachmentPoint);
                Arrowhead ah = new Arrowhead
                {
                    TipPosition = arrowheadPosition,
                    Length = (attachmentPoint - arrowheadPosition).Length
                };
                XmlRead();
                ReadEndElement();
                var rail = new Rail(ah, attachmentPoint, topRankedEdgoInfo, zoomLevel);
                var tuple = new SymmetricSegment(arrowheadPosition, attachmentPoint);
                level._railDictionary[tuple] = rail;
                return rail;
            }

            if (TokenIs(GeometryToken.LineSegment))
            {
                pointString = GetAttribute(GeometryToken.Points);
                var linePoints = ParsePoints(pointString);
                Debug.Assert(linePoints.Length == 2);
                LineSegment ls = new LineSegment(linePoints[0], linePoints[1]);
                XmlRead();
                ReadEndElement();
                var rail = new Rail(ls, topRankedEdgoInfo, zoomLevel);
                var tuple = new SymmetricSegment(ls.Start, ls.End);
                level._railDictionary[tuple] = rail;
                level._railTree.Add(ls.BoundingBox, rail);
                return rail;
            }
            if (TokenIs(GeometryToken.CubicBezierSegment))
            {
                pointString = GetAttribute(GeometryToken.Points);
                var controlPoints = ParsePoints(pointString);
                Debug.Assert(controlPoints.Length == 4);
                var bs = new CubicBezierSegment(controlPoints[0], controlPoints[1], controlPoints[2], controlPoints[3]);
                XmlRead();
                ReadEndElement();
                var rail = new Rail(bs, topRankedEdgoInfo, zoomLevel);
                var tuple = new SymmetricSegment(bs.Start, bs.End);
                level._railDictionary[tuple] = rail;
                return rail;
            }
            throw new Exception();
        }

        LgEdgeInfo GetTopRankedEdgeInfoOfRail(string railId)
        {
            if (!railIdsToTopRankedEdgeInfo.ContainsKey(railId))
                return null;
            return railIdsToTopRankedEdgeInfo[railId];
        }

        LgEdgeInfo GetTopRankedEdgeInfoOfSkeletonRail(string railId)
        {
            if (railIdsToTopRankedEdgeInfo.ContainsKey(railId))
                return railIdsToTopRankedEdgeInfo[railId];
            return null;
        }

        void ReadLgNodeInfos(LgData lgData)
        {
            if (XmlReader.IsEmptyElement) return;
            lgData.GeometryNodesToLgNodeInfos = new Dictionary<Node, LgNodeInfo>();
            lgData.SortedLgNodeInfos = new List<LgNodeInfo>();
            XmlRead();
            while (TokenIs(GeometryToken.LgNodeInfo))
                ReadLgNodeInfo(lgData);
            ReadEndElement();
        }

        void ReadLgNodeInfo(LgData lgData)
        {
            var nodeId = GetAttribute(GeometryToken.Id);
            var nodeInfo = new LgNodeInfo(nodeIdToNodes[nodeId])
            {
                Rank = GetDoubleAttribute(GeometryToken.Rank),
                ZoomLevel = GetDoubleAttribute(GeometryToken.Zoomlevel),
                LabelVisibleFromScale = GetDoubleAttributeOrDefault(GeometryToken.LabelVisibleFromScale, 1.0),
                LabelWidthToHeightRatio = GetDoubleAttributeOrDefault(GeometryToken.LabelWidthToHeightRatio, 1.0),
                LabelOffset = TryGetPointAttribute(GeometryToken.LabelOffset)
            };
            lgData.SortedLgNodeInfos.Add(nodeInfo);
            lgData.GeometryNodesToLgNodeInfos[nodeIdToNodes[nodeId]] = nodeInfo;
            XmlRead();
        }

        GeometryToken GetElementTag()
        {
            if (XmlReader.NodeType == XmlNodeType.EndElement &&
                XmlReader.Name == "graph")
                return GeometryToken.End;
            GeometryToken token;
            if (XmlReader.ReadState == ReadState.EndOfFile)
                return GeometryToken.Graph;
            if (Enum.TryParse(XmlReader.Name, true, out token))
                return token;
            return GeometryToken.Unknown;
        }

        void ReadClusters()
        {
            XmlRead();
            while (TokenIs(GeometryToken.Cluster))
                ReadCluster();

            FleshOutClusters();
            var rootClusterSet = new Set<Cluster>();
            foreach (var cluster in stringToClusters.Values.Select(c => c.Cluster))
                if (cluster.ClusterParent == null)
                    rootClusterSet.Insert(cluster);

            if (rootClusterSet.Count == 1)
                _graph.RootCluster = rootClusterSet.First();
            else
            {
                _graph.RootCluster.AddRangeOfCluster(rootClusterSet);
            }

            if (!XmlReader.IsStartElement())
                ReadEndElement();
        }

        void FleshOutClusters()
        {
            foreach (var clusterWithLists in stringToClusters.Values)
            {
                var cl = clusterWithLists.Cluster;
                foreach (var i in clusterWithLists.ChildClusters)
                    cl.AddCluster(stringToClusters[i].Cluster);
                foreach (var i in clusterWithLists.ChildNodes)
                    cl.AddNode(nodeIdToNodes[i]);
            }
        }

        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower")]
        void ReadCluster()
        {
            var cluster = new Cluster { RectangularBoundary = new RectangularClusterBoundary() };
            var clusterWithChildLists = new ClusterWithChildLists(cluster);
            cluster.Barycenter = TryGetPointAttribute(GeometryToken.Barycenter);
            var clusterId = GetAttribute(GeometryToken.Id);
            stringToClusters[clusterId] = clusterWithChildLists;
            ReadChildClusters(clusterWithChildLists.ChildClusters);
            ReadChildNodes(clusterWithChildLists.ChildNodes);
            XmlRead();
            switch (NameToToken())
            {
                case GeometryToken.ICurve:
                    cluster.BoundaryCurve = ReadICurve();
                    break;
                case GeometryToken.Curve:
                    cluster.BoundaryCurve = ReadCurve();
                    XmlRead();
                    break;
                case GeometryToken.Rect:
                    cluster.BoundaryCurve = ReadRect();
                    XmlRead();
                    break;
            }

            if (XmlReader.NodeType != XmlNodeType.EndElement)
                cluster.RectangularBoundary = ReadClusterRectBoundary();

            ReadEndElement();
        }

        RectangularClusterBoundary ReadClusterRectBoundary()
        {
            RectangularClusterBoundary recClBnd = new RectangularClusterBoundary
            {
                LeftMargin = GetDoubleAttribute(GeometryToken.LeftMargin),
                RightMargin = GetDoubleAttribute(GeometryToken.RightMargin),
                TopMargin = GetDoubleAttribute(GeometryToken.TopMargin),
                BottomMargin = GetDoubleAttribute(GeometryToken.BottomMargin)
            };

            if (GetAttribute(GeometryToken.DefaultLeftMargin) != null)
            {
                var defaultLeftMargin = GetDoubleAttribute(GeometryToken.DefaultLeftMargin);
                var defaultRightMargin = GetDoubleAttribute(GeometryToken.DefaultRightMargin);
                var defaultTopMargin = GetDoubleAttribute(GeometryToken.DefaultBottomMargin);
                var defaultBottomMargin = GetDoubleAttribute(GeometryToken.DefaultBottomMargin);
                recClBnd.StoreDefaultMargin(defaultLeftMargin, defaultRightMargin, defaultBottomMargin, defaultTopMargin);
            }

            recClBnd.GenerateFixedConstraints = GetBoolAttributeOrDefault(GeometryToken.GenerateFixedConstraints, false);
            recClBnd.GenerateFixedConstraintsDefault =
                GetBoolAttributeOrDefault(GeometryToken.GenerateFixedConstraintsDefault, false);

            recClBnd.MinHeight = GetDoubleAttribute(GeometryToken.MinNodeHeight);
            recClBnd.MinWidth = GetDoubleAttribute(GeometryToken.MinNodeWidth);

            double ry;
            double left;
            double bottom;
            double w;
            double h;
            double rx;

            XmlRead();
            ReadRectParams(out left, out bottom, out w, out h, out rx, out ry);
            recClBnd.Rect = new Rectangle(left, bottom, new Point(w, h));
            recClBnd.RadiusX = rx;
            recClBnd.RadiusY = ry;


            recClBnd.RightBorderInfo = ReadBorderInfo(GeometryToken.RightBorderInfo);
            recClBnd.LeftBorderInfo = ReadBorderInfo(GeometryToken.LeftBorderInfo);
            recClBnd.TopBorderInfo = ReadBorderInfo(GeometryToken.TopBorderInfo);
            recClBnd.BottomBorderInfo = ReadBorderInfo(GeometryToken.BottomBorderInfo);
            XmlRead();
            ReadEndElement();
            return recClBnd;
        }

        BorderInfo ReadBorderInfo(GeometryToken token)
        {
            XmlRead();
            CheckToken(token);
            var bi = new BorderInfo
            {
                InnerMargin = GetDoubleAttribute(GeometryToken.InnerMargin),
                FixedPosition = GetDoubleAttribute(GeometryToken.FixedPosition),
                Weight = GetDoubleAttribute(GeometryToken.Weight)
            };
            return bi;
        }

        void ReadChildClusters(List<string> childClusters)
        {
            var clusterIds = GetAttribute(GeometryToken.ChildClusters);
            if (string.IsNullOrEmpty(clusterIds)) return;
            childClusters.AddRange(clusterIds.Split(' '));
        }

        void ReadChildNodes(List<string> childNodes)
        {
            var nodeIds = GetAttribute(GeometryToken.ChildNodes);
            if (string.IsNullOrEmpty(nodeIds)) return;
            childNodes.AddRange(nodeIds.Split(' '));
        }

        void ReadEdges()
        {
            CheckToken(GeometryToken.Edges);

            if (XmlReader.IsEmptyElement)
            {
                XmlRead();
                return;
            }

            XmlRead();
            while (TokenIs(GeometryToken.Edge))
                ReadEdge();
            ReadEndElement();
        }

        void ReadEdge()
        {
            CheckToken(GeometryToken.Edge);
            Node s = ReadSourceNode();
            Node t = ReadTargetNode();
            var edge = new Edge(s, t)
            {
                Separation = (int)GetDoubleAttributeOrDefault(GeometryToken.Separation, 1),
                LineWidth = GetDoubleAttributeOrDefault(GeometryToken.LineWidth, 1),
                Weight = (int)GetDoubleAttributeOrDefault(GeometryToken.Weight, 1),

            };
            string id = GetAttribute(GeometryToken.Id);
            if (id != null)
            {
                Debug.Assert(idToEdges.ContainsKey(id) == false);
                idToEdges[id] = edge;
            }
            else
            {
                Debug.Assert(idToEdges.Count == 0); // we consistently should have no ids or unique id per edge
            }

            EdgeList.Add(edge);
            ReadArrowheadAtSource(edge);
            ReadArrowheadAtTarget(edge);
            ReadLabelFromAttribute(edge);
            bool breakTheLoop = false;
            //edge.UnderlyingPolyline = ReadUnderlyingPolyline();
            _graph.Edges.Add(edge);
            if (XmlReader.IsEmptyElement)
            {
                XmlReader.Skip();
                return;
            }
            XmlRead();
            do
            {
                GeometryToken token = GetElementTag();
                switch (token)
                {
                    case GeometryToken.Curve:
                        edge.Curve = ReadICurve();
                        break;
                    case GeometryToken.LineSegment:
                        edge.Curve = ReadLineSeg();
                        break;
                    case GeometryToken.Edge:
                        ReadEndElement();
                        breakTheLoop = true;
                        break;
                    case GeometryToken.CubicBezierSegment:
                        edge.Curve = ReadCubucBezierSegment();
                        break;
                    case GeometryToken.Polyline:

                        break;
                    default:
                        breakTheLoop = true;
                        break;
                }
                if (!breakTheLoop)
                    XmlRead();
            } while (!breakTheLoop);
            //XmlReader.Skip();
        }

        CubicBezierSegment ReadCubucBezierSegment() {
            var str = GetAttribute(GeometryToken.Points);
            var ss = str.Split(' ');

            var nonEmptySs = ss.Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (nonEmptySs.Count != 8)
                Error("wrong number of points in LineSegment");

            var ds = nonEmptySs.Select(ParseDouble).ToArray();
            return new CubicBezierSegment(new Point(ds[0], ds[1]), new Point(ds[2], ds[3]), new Point(ds[4], ds[5]), new Point(ds[6], ds[7]) );
        }


        void ReadArrowheadAtSource(Edge edge)
        {
            var str = GetAttribute(GeometryToken.As);
            var arrowhead =
                edge.EdgeGeometry.SourceArrowhead = str != null ? new Arrowhead { TipPosition = ParsePoint(str) } : null;
            if (arrowhead != null)
                arrowhead.Length = GetDoubleAttributeOrDefault(GeometryToken.Asl, Arrowhead.DefaultArrowheadLength);
            else
            {
                str = GetAttribute(GeometryToken.Asl);
                if (str != null)
                    edge.EdgeGeometry.SourceArrowhead = new Arrowhead { Length = ParseDouble(str) };
            }
        }

        Point ParsePoint(string str)
        {
            var xy = str.Split(' ').ToArray();
            Debug.Assert(xy.Length == 2);
            double x, y;
            if (double.TryParse(xy[0], out x) && double.TryParse(xy[1], out y))
                return new Point(x, y);
            Error("invalid point format" + str);
            return new Point();
        }

        Point[] ParsePoints(string str)
        {
            var tokens = str.Split(' ');
            Debug.Assert(tokens.Length % 2 == 0);
            Point[] ret = new Point[tokens.Length / 2];
            for (int i = 0; i < tokens.Length - 1; i += 2)
            {
                double x, y;
                if (double.TryParse(tokens[i], out x) && double.TryParse(tokens[i + 1], out y))
                    ret[i / 2] = new Point(x, y);
                else
                    Error("invalid point format" + str);
            }
            return ret;
        }

        void ReadArrowheadAtTarget(Edge edge)
        {
            var str = GetAttribute(GeometryToken.At);
            var arrowhead =
                edge.EdgeGeometry.TargetArrowhead = str != null ? new Arrowhead { TipPosition = ParsePoint(str) } : null;
            if (arrowhead != null)
                arrowhead.Length = GetDoubleAttributeOrDefault(GeometryToken.Atl, Arrowhead.DefaultArrowheadLength);
            else
            {
                str = GetAttribute(GeometryToken.Atl);
                if (str != null)
                    edge.EdgeGeometry.TargetArrowhead = new Arrowhead { Length = ParseDouble(str) };
            }
        }

        void ReadLabelFromAttribute(GeometryObject geomObj)
        {
            string str;
            if (!TryGetAttribute(GeometryToken.Label, out str)) return;
            var label = new Label(geomObj);
            Point center;
            double width, height;
            ParseLabel(str, out center, out width, out height);
            label.Center = center;
            label.Width = width;
            label.Height = height;

            var edge = geomObj as Edge;
            if (edge != null)
            {
                edge.Label = label;
            }
        }


        void ParseLabel(string str, out Point center, out double width, out double height)
        {
            var ss = str.Split(' ');
            Debug.Assert(ss.Length == 4);

            center = new Point(ParseDouble(ss[0]), ParseDouble(ss[1]));
            width = ParseDouble(ss[2]);
            height = ParseDouble(ss[3]);
        }

        double ParseDouble(string s)
        {
            double ret;
            if (double.TryParse(s, out ret))
                return ret;
            Error(" cannot parse double " + s);
            return 0;
        }

        Node ReadTargetNode()
        {
            var targetId = GetMustAttribute(GeometryToken.T);
            return GetNodeOrClusterById(targetId);
        }

        Node ReadSourceNode()
        {
            var id = GetMustAttribute(GeometryToken.S);
            return GetNodeOrClusterById(id);
        }

        Node GetNodeOrClusterById(string id)
        {
            Node ret;
            if (nodeIdToNodes.TryGetValue(id, out ret))
                return ret;

            return stringToClusters[id].Cluster;

        }

        void ReadNodes()
        {
            if (XmlReader.IsEmptyElement) return;
            XmlRead();
            while (TokenIs(GeometryToken.Node))
                ReadNode();
            ReadEndElement();
        }

        internal const double NodeDefaultPadding = 1;

        void ReadNode()
        {
            string nodeId = GetMustAttribute(GeometryToken.Id);
            double nodePadding = GetDoubleAttributeOrDefault(GeometryToken.Padding, NodeDefaultPadding);
            XmlRead();
            var node = new Node(ReadICurve()) { Padding = nodePadding, UserData = nodeId };
            if (node.BoundaryCurve == null)
                throw new InvalidOperationException();
            _graph.Nodes.Add(node);
            XmlReader.Skip();
            ReadEndElement();
            nodeIdToNodes[nodeId] = node;
        }

        string GetAttribute(GeometryToken token)
        {
            return XmlReader.GetAttribute(GeometryGraphWriter.FirstCharToLower(token));
        }

        bool TryGetAttribute(GeometryToken token, out string val)
        {
            return (val = GetAttribute(token)) != null;
        }

        string GetMustAttribute(GeometryToken token)
        {
            var s = GeometryGraphWriter.FirstCharToLower(token);
            var ret = XmlReader.GetAttribute(GeometryGraphWriter.FirstCharToLower(token));
            if (ret != null)
                return ret;
            Error("attribute " + s + " not found");
            return null;
        }

        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider",
            MessageId = "System.String.Format(System.String,System.Object)")]
        int GetIntAttribute(GeometryToken token)
        {
            var val = GetAttribute(token);
            if (val == null)
                Error(String.Format("attribute {0} not found", token));
            int ret;
            if (int.TryParse(val, out ret))
                return ret;
            Error("cannot parse an int value " + val);
            return 0;
        }

        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider",
            MessageId = "System.String.Format(System.String,System.Object)")]
        double GetDoubleAttribute(GeometryToken token)
        {
            var val = GetAttribute(token);
            if (val == null)
                Error(String.Format("attribute {0} not found", token));
            double ret;
            if (double.TryParse(val, out ret))
                return ret;
            Error("cannot parse an int value " + val);
            return 0;
        }

        double GetDoubleAttributeOrDefault(GeometryToken token, double defaultVal)
        {
            string val = GetAttribute(token);
            if (val == null)
                return defaultVal;
            double ret;
            if (double.TryParse(val, out ret))
                return ret;
            Error("cannot parse a double value " + val);
            return 0;
        }

        bool GetBoolAttributeOrDefault(GeometryToken token, bool defaultVal)
        {
            string val = GetAttribute(token);
            if (val == null)
                return defaultVal;
            bool ret;
            if (bool.TryParse(val, out ret))
                return ret;
            Error("cannot parse a bool value " + val);
            return false;
        }

        int GetIntAttributeOrDefault(GeometryToken token, int defaultVal)
        {
            string val = GetAttribute(token);
            if (val == null)
                return defaultVal;
            int ret;
            if (int.TryParse(val, out ret))
                return ret;
            Error("cannot parse a bool value " + val);
            return 0;
        }


        Point TryGetPointAttribute(GeometryToken token)
        {
            string val = GetAttribute(token);
            return val == null ? new Point() : ParsePoint(val);
        }

        void Error(string msg)
        {
            throw new InvalidOperationException(msg + ";" + GetPositionInfo());
        }

        ICurve ReadICurve()
        {
            switch (NameToToken())
            {
                case GeometryToken.Curve:
                    return ReadCurve();
                case GeometryToken.Ellipse:
                    return ReadEllipse();
                case GeometryToken.Rect:
                    return ReadRect();
                case GeometryToken.Polygon:
                    return ReadPolygon();
                //            if (hasCurve) {
                //                XmlRead();
                //                ICurve ret = null;
                //                if (TokenIs(GeometryToken.Ellipse))
                //                    ret = ReadEllipse();
                //                else if (TokenIs(GeometryToken.Curve))
                //                    ret = ReadCurve();
                //                else if (TokenIs(GeometryToken.LineSegment))
                //                    ret = ReadLineSeg();
                //                else if (TokenIs(GeometryToken.CubicBezierSegment))
                //                    ret = ReadCubicBezierSeg();
                //                else if (TokenIs(GeometryToken.Polyline))
                //                    ret = ReadPolyline();
            }
            return null;
        }

        ICurve ReadPolygon()
        {
            var pointString = GetMustAttribute(GeometryToken.Points);
            var t = pointString.Split(' ');
            if (t.Length == 0 || t.Length % 2 != 0)
                Error("invalid input for the polygon");
            var poly = new Polyline { Closed = true };
            for (int i = 0; i < t.Length; i += 2)
                poly.AddPoint(new Point(ParseDouble(t[i]), ParseDouble(t[i + 1])));
            return poly;
        }

        ICurve ReadRect()
        {
            double y;
            double width;
            double height;
            double rx;
            double ry;
            double x;
            ReadRectParams(out x, out y, out width, out height, out rx, out ry);
            var box = new Rectangle(x, y, x + width, y + height);
            return new RoundedRect(box, rx, ry);
        }

        void ReadRectParams(out double x, out double y, out double width, out double height, out double rx,
            out double ry)
        {
            x = GetDoubleAttributeOrDefault(GeometryToken.X, 0);
            y = GetDoubleAttributeOrDefault(GeometryToken.Y, 0);
            width = ParseDouble(GetMustAttribute(GeometryToken.Width));
            height = ParseDouble(GetMustAttribute(GeometryToken.Height));
            rx = GetDoubleAttributeOrDefault(GeometryToken.Rx, 0);
            ry = GetDoubleAttributeOrDefault(GeometryToken.Ry, 0);
        }

        ICurve ReadCurve()
        {
            if (XmlReader.MoveToFirstAttribute())
            {
                var token = NameToToken();
                switch (token)
                {
                    case GeometryToken.CurveData:
                        return ParseCurve(XmlReader.Value);
                }
                throw new InvalidOperationException();
            }
            Error("No boundary curve is defined");
            return null;
        }

        GeometryToken NameToToken()
        {
            GeometryToken token;
            if (Enum.TryParse(XmlReader.Name, true, out token))
                return token;
            Error("cannot parse " + XmlReader.Name);
            return GeometryToken.Error;
        }

        ICurve ParseCurve(string curveData)
        {
            var curve = new Curve();
            var curveStream = new CurveStream(curveData);
            var currentPoint = new Point();
            do
            {
                var curveStreamElement = curveStream.GetNextCurveStreamElement();
                if (curveStreamElement == null)
                    return curve;
                var charStreamElement = curveStreamElement as CharStreamElement;
                if (charStreamElement == null)
                {
                    Error("wrong formatted curve string " + curveStreamElement);
                    return null;
                }
                AddCurveSegment(curveStream, charStreamElement.Char, curve, ref currentPoint);
            } while (true);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        void AddCurveSegment(CurveStream curveStream, char c, Curve curve, ref Point currentPoint)
        {
            switch (c)
            {
                case 'M': //moveto
                    currentPoint = GetNextPointFromCurveData(curveStream);
                    break;
                case 'm': //relative moveto
                    throw new NotImplementedException();
                case 'Z':
                case 'z': //closepath
                    if (curve.Segments.Count == 0)
                        Error("the curve is too short");
                    curve.AddSegment(new LineSegment(currentPoint, currentPoint = curve.Start));
                    break;
                case 'L': //lineto
                    ProceedWithLines(curve, curveStream, ref currentPoint);
                    break;
                case 'l': //lineto relative
                    throw new NotImplementedException();
                case 'H': //lineto horizontal
                    throw new NotImplementedException();
                case 'h': //lineto horizontal relative
                    throw new NotImplementedException();
                case 'V': //lineto vertical
                    throw new NotImplementedException();
                case 'v': //lineto vertical relative
                    throw new NotImplementedException();
                case 'C': //cubic Bezier
                    ProceedWithCubicBeziers(curve, curveStream, ref currentPoint);
                    break;
                case 'c': //cubic Bezier relative
                    throw new NotImplementedException();
                case 'S': //cubic Bezier shorthand
                    throw new NotImplementedException();
                case 's': //cubic Bezier relative shorthand
                    throw new NotImplementedException();
                case 'Q': //quadratic Bezier
                    throw new NotImplementedException();
                case 'q': //quadratic Bezier relative
                    throw new NotImplementedException();
                case 'T': //quadratic Bezier shorthand
                    throw new NotImplementedException();
                case 't': //quadratic Bezier relative shorthand
                    throw new NotImplementedException();
                case 'A': //elleptical arc
                    ReadEllepticalArc(curve, curveStream, ref currentPoint);
                    break;
                case 'a': //eleptical arc relative
                    throw new NotImplementedException();
                default:
                    Error("unknown character " + c);
                    break;
            }
        }

        void ProceedWithCubicBeziers(Curve curve, CurveStream curveStream, ref Point currentPoint)
        {
            do
            {
                curve.AddSegment(new CubicBezierSegment(currentPoint, GetNextPointFromCurveData(curveStream),
                    GetNextPointFromCurveData(curveStream), currentPoint = GetNextPointFromCurveData(curveStream)));
            } while (curveStream.PickNextCurveStreamElement() is DoubleStreamElement);
        }

        void ReadEllepticalArc(Curve curve, CurveStream curveStream, ref Point currentPoint)
        {
            curve.AddSegment(ReadEllepticalArc(curveStream, ref currentPoint));
        }

        ICurve ReadEllepticalArc(CurveStream curveStream, ref Point currentPoint)
        {
            var rx = GetNextDoubleFromCurveData(curveStream);
            var ry = GetNextDoubleFromCurveData(curveStream);
            var xAxisRotation = GetNextDoubleFromCurveData(curveStream) / 180 * Math.PI;
            var largeArcFlag = (int)GetNextDoubleFromCurveData(curveStream);
            var sweepFlag = (int)GetNextDoubleFromCurveData(curveStream);
            var endPoint = GetNextPointFromCurveData(curveStream);
            //figure out the transform to the circle
            //then solve this problem on the circle
            if (ApproximateComparer.Close(rx, 0) || ApproximateComparer.Close(ry, 0))
                Error("ellipseArc radius is too small");
            var yScale = rx / ry;
            var rotationMatrix = PlaneTransformation.Rotation(-xAxisRotation);
            var scaleMatrix = new PlaneTransformation(1, 0, 0, 0, yScale, 0);
            var transform = scaleMatrix * rotationMatrix;
            var start = transform * currentPoint;
            currentPoint = endPoint;
            var end = transform * endPoint;
            Point center;
            double startAngle;
            double endAngle;
            Point axisY;
            GetArcCenterAndAngles(rx, largeArcFlag, sweepFlag, start, end, out center, out startAngle, out endAngle,
                out axisY);
            var inverted = transform.Inverse;
            center = inverted * center;
            var rotation = PlaneTransformation.Rotation(xAxisRotation);
            var axisX = rotation * new Point(rx, 0);
            axisY = rotation * (axisY / yScale);
            var ret = new Ellipse(startAngle, endAngle, axisX, axisY, center);

            Debug.Assert(ApproximateComparer.Close(ret.End, endPoint));
            return ret;
        }

        void GetArcCenterAndAngles(double r, int largeArcFlag, int sweepFlag, Point start, Point end, out Point center,
            out double startAngle, out double endAngle, out Point axisY)
        {
            var d = end - start;
            var dLenSquared = d.LengthSquared; //use it to get more precision
            var dLen = d.Length;
            //            if(dLen<r-ApproximateComparer.DistanceEpsilon)
            //                Error("arc radius is too small");

            var middle = (start + end) / 2;

            //the circle center belongs to the perpendicular to d passing through 'middle'
            d /= dLen;
            var perp = new Point(d.Y, -d.X) * Math.Sqrt(r * r - dLenSquared / 4);
            center = sweepFlag == 1 && largeArcFlag == 0 || sweepFlag == 0 && largeArcFlag == 1
                ? middle - perp
                : middle + perp;
            var axisX = new Point(r, 0);
            axisY = sweepFlag == 1 ? new Point(0, r) : new Point(0, -r);
            startAngle = Point.Angle(axisX, start - center);
            if (sweepFlag == 0)
                startAngle = 2 * Math.PI - startAngle;

            endAngle = Point.Angle(axisX, end - center);
            if (sweepFlag == 0)
                endAngle = 2 * Math.PI - endAngle;
            if (ApproximateComparer.Close(endAngle, startAngle) && largeArcFlag == 1)
                endAngle += 2 * Math.PI;
            else if (endAngle < startAngle)
                endAngle += 2 * Math.PI;
        }

        void ProceedWithLines(Curve curve, CurveStream curveStream, ref Point currentPoint)
        {
            do
            {
                curve.AddSegment(new LineSegment(currentPoint, currentPoint = GetNextPointFromCurveData(curveStream)));
            } while (curveStream.PickNextCurveStreamElement() is DoubleStreamElement);
        }

        Point GetNextPointFromCurveData(CurveStream curveStream)
        {
            return new Point(GetNextDoubleFromCurveData(curveStream), GetNextDoubleFromCurveData(curveStream));
        }

        double GetNextDoubleFromCurveData(CurveStream curveStream)
        {
            var a = curveStream.GetNextCurveStreamElement();
            if (a == null)
                Error("cannot parse curveData");

            var d = a as DoubleStreamElement;
            if (d == null)
                Error("cannot parse curveData");
            // ReSharper disable PossibleNullReferenceException
            return d.Double;
            // ReSharper restore PossibleNullReferenceException
        }

        ICurve ReadLineSeg()
        {
            CheckToken(GeometryToken.LineSegment);
            var str = GetAttribute(GeometryToken.Points);
            var ss = str.Split(' ');
            if (ss.Length != 4)
                Error("wrong number of points in LineSegment");
            var ds = ss.Select(ParseDouble).ToArray();
            return new LineSegment(new Point(ds[0], ds[1]), new Point(ds[2], ds[3]));
        }

        ICurve ReadEllipse()
        {
            var cx = ParseDouble(GetMustAttribute(GeometryToken.Cx));
            var cy = ParseDouble(GetMustAttribute(GeometryToken.Cy));
            var rx = ParseDouble(GetMustAttribute(GeometryToken.Rx));
            var ry = ParseDouble(GetMustAttribute(GeometryToken.Ry));
            return new Ellipse(rx, ry, new Point(cx, cy));
        }


        bool TokenIs(GeometryToken t)
        {
            return XmlReader.IsStartElement(GeometryGraphWriter.FirstCharToLower(t)) ||
                   XmlReader.IsStartElement(t.ToString());
        }

        void MoveToContent()
        {
            XmlReader.MoveToContent();
        }

        /// <summary>
        /// the xml reader
        ///</summary>
        XmlReader XmlReader { get; set; }

        /// <summary>
        /// the xml reader
        ///<parameter>the reader</parameter>
        /// </summary>
        public void SetXmlReader(XmlReader reader)
        {
            XmlReader = reader;
        }

        ///<summary>
        ///used only in Debug configuration
        ///<param name="token">the token that should be here</param>
        ///</summary>
        void CheckToken(GeometryToken token)
        {
            if (!XmlReader.IsStartElement(GeometryGraphWriter.FirstCharToLower(token)) &&
                !XmlReader.IsStartElement(token.ToString()))
            {
                string positionInfo = GetPositionInfo();
                throw new InvalidDataException(
                    String.Format(CultureInfo.InvariantCulture,
                        "expected {0}, {1}", token, positionInfo));
            }
        }

        string GetPositionInfo()
        {
            if (xmlTextReader != null)
                return String.Format(CultureInfo.InvariantCulture, "line {0} col {1}", xmlTextReader.LineNumber,
                    xmlTextReader.LinePosition);
            return String.Empty;
        }

        ///<summary>
        ///reads the end element
        ///</summary>
        void ReadEndElement()
        {
            XmlReader.ReadEndElement();
        }


        ///<summary>
        /// reads a double
        ///</summary>        
        double ReadElementContentAsDouble()
        {
            return XmlReader.ReadElementContentAsDouble();
        }

        ///<summary>
        ///reads the line?
        ///</summary>
        void XmlRead()
        {
            XmlReader.Read();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        int ReadElementContentAsInt()
        {
            return XmlReader.ReadElementContentAsInt();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                xmlTextReader.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

#endif