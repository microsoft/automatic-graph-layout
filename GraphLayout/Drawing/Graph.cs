using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Layout.Layered;
using P2 = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// Graph for drawing. Putting an instance of this class to property Graph triggers the layout under the hood
    /// </summary>
    [Serializable]
    public class Graph : DrawingObject, ILabeledObject {
        private Subgraph rootSubgraph=new Subgraph("the root subgraph's boundary");
        ///<summary>
        ///</summary>
        public Subgraph RootSubgraph { get { return rootSubgraph; } set { rootSubgraph = value; } }


#if TEST_MSAGL
        [NonSerialized] Database dataBase;

        /// <summary>
        /// debug only
        /// </summary>
        public Database DataBase {
            get { return dataBase; }
            set { dataBase = value; }
        }
#endif
        Label label;

        /// <summary>
        /// the label of the object
        /// </summary>
        public Label Label {
            get { return label; }
            set { label = value; }
        }

        ///<summary>
        ///</summary>
        public IEnumerable<Node> Nodes {
            get {
                foreach (var r in nodeMap.Values)
                    yield return (Node)r;
            }
        }

        LayoutAlgorithmSettings layoutAlgorithm = new SugiyamaLayoutSettings();

        /// <summary>
        /// the properties of the layout algorithm
        /// </summary>
        public LayoutAlgorithmSettings LayoutAlgorithmSettings
        {
            get { return layoutAlgorithm; }
            set { layoutAlgorithm = value; }
        }

        void WriteNodes(TextWriter sw) {
            sw.WriteLine("//nodes");
            foreach (Node node in nodeMap.Values)
                sw.WriteLine(node.ToString());
        }
        
        
        /// <summary>
        /// Prints Microsoft.Msagl.Drawing in the DOT format - has side effects!
        /// </summary>
        /// <returns>String</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider",
            MessageId = "System.IO.StringWriter.#ctor")]
        public override string ToString() {
            var sw = new StringWriter();

            sw.WriteLine("digraph \"" + (string.IsNullOrEmpty(Label.Text)? "noname":Label.Text) + "\" {");

            WriteStms(sw);

            sw.WriteLine("}");

            sw.Close();

            return sw.ToString();
        }


        void WriteEdges(TextWriter tw) {
            foreach (Edge edge in Edges) {
                tw.WriteLine(edge.ToDotGeometry());
            }
        }

        void WriteStms(TextWriter sw) {
            sw.WriteLine(attr.ToString(Label.Text));
            WriteNodes(sw);
            WriteEdges(sw);
        }

        /// <summary>
        /// Returns the bounding box of the graph
        /// </summary>
        public override Rectangle BoundingBox {
            get {
                return GeometryGraph != null
                           ? PumpByBorder(GeometryGraph.BoundingBox)
                           : new Rectangle(0, 0, new Point(1, 1));
            }
        }

        Rectangle PumpByBorder(Rectangle rectangle) {
            var del = new P2(Attr.Border, Attr.Border);
            return new Rectangle(rectangle.LeftBottom - del, rectangle.RightTop + del);
        }

        /// <summary>
        /// The graph attribute.
        /// </summary>
        internal GraphAttr attr;

        /// <summary>
        /// The graph attribute property
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Attr")]
        public GraphAttr Attr {
            get { return attr; }
            set { attr = value; }
        }

        /// <summary>
        /// the width of the graph
        /// </summary>
        public double Width {
            get { return GeometryGraph != null ? GeometryGraph.Width + Attr.Border : 1; }
        }

        /// <summary>
        /// the height of the graph
        /// </summary>
        public double Height {
            get { return GeometryGraph != null ? GeometryGraph.Height + Attr.Border : 1; }
        }

        /// <summary>
        /// left of the graph
        /// </summary>
        public double Left {
            get { return GeometryGraph != null ? GeometryGraph.Left - Attr.Border : 0; }
        }

        /// <summary>
        /// top of the graph
        /// </summary>
        public double Top {
            get { return GeometryGraph != null ? GeometryGraph.Top + Attr.Border : 1; }
        }

        /// <summary>
        /// bottom of the graph
        /// </summary>
        public double Bottom {
            get { return GeometryGraph != null ? GeometryGraph.Bottom - Attr.Border : 0; }
        }

        /// <summary>
        /// right of the graph
        /// </summary>
        public double Right {
            get { return GeometryGraph != null ? GeometryGraph.Right + Attr.Border : 1; }
        }

#if TEST_MSAGL
        List<Node> history = new List<Node>();

        /// <summary>
        /// debug only visible
        /// </summary>
        public ICollection<Node> History {
            get { return history; }
            set { history = (List<Node>) value; }
        }
#endif

        /// <summary>
        /// Creates a new node and returns it or returns the old node.
        /// If the node label is not set the id is used as the label.
        /// </summary>
        /// <param name="nodeId">is a key to the node in the Node's table</param>
        /// <returns>it can return a Subgraph too</returns>
        public Node AddNode(string nodeId) {
            Node ret;
            if (RootSubgraph != null && RootSubgraph.IsUpdated) {
                SubgraphMap.Clear();
                foreach (var sg in RootSubgraph.AllSubgraphsDepthFirst())
                    SubgraphMap[sg.Id] = sg;
                RootSubgraph.IsUpdated = false;
            }
            Subgraph subgraph;
            if (SubgraphMap.TryGetValue(nodeId, out subgraph))
                return subgraph;

            ret = nodeMap[nodeId] as Node;
            if (ret == null) {
                ret = new Node(nodeId);
                nodeMap[nodeId] = ret;
#if TEST_MSAGL
                history.Add(ret);
#endif
            }
            return ret;
        }

        /// <summary>
        /// adds a node to the graph
        /// </summary>
        /// <param name="node"></param>
        public void AddNode(Node node) {
            if (nodeMap.ContainsKey(node.Id))
                return;
            if (subgraphMap.ContainsKey(node.Id))
                return;
            var sg = node as Subgraph;
            if (sg != null)
                subgraphMap[node.Id] = sg;
            nodeMap[node.Id] = node;
        }

        /// <summary>
        /// Number of nodes in the graph without counting the subgraphs.
        /// </summary>
        public int NodeCount {
            get { return nodeMap.Count; }
        }


        /// <summary>
        /// A lookup function.
        /// </summary>
        /// <param name="edgeId"></param>
        /// <returns></returns>
        public Edge EdgeById(string edgeId) {
            if (idToEdges == null || idToEdges.Count == 0) {
                foreach (Edge e in Edges)
                    if (e.Attr.Id != null)
                        idToEdges[e.Attr.Id] = e;
            }

            return idToEdges[edgeId] as Edge;
        }

        /// <summary>
        /// The number of dges in the graph.
        /// </summary>
        public int EdgeCount {
            get { return Edges.Count(); }
        }

        /// <summary>
        /// Removes an edge, if the edge doesn't exist then nothing happens.
        /// </summary>
        /// <param name="edge">edge reference</param>
        public virtual void RemoveEdge(Edge edge) {
            if (edge == null)
                return;
            Node source = edge.SourceNode;
            Node target = edge.TargetNode;
            if (source != target) {
                source.RemoveOutEdge(edge);
                target.RemoveInEdge(edge);
            } else
                source.RemoveSelfEdge(edge);
            if (edge.GeometryObject != null)
                GeometryGraph.Edges.Remove(edge.GeometryObject as Core.Layout.Edge);
        }

        /// <summary>
        /// Removes a node and all of its edges. If the node doesn't exist, nothing happens.
        /// </summary>
        /// <param name="node">node reference</param>
        public virtual void RemoveNode(Node node) {
            if (node == null || !NodeMap.ContainsKey(node.Id))
                return;
            var delendi = new ArrayList();
            foreach (Edge e in node.InEdges)
                delendi.Add(e);
            foreach (Edge e in node.OutEdges)
                delendi.Add(e);
            foreach (Edge e in node.SelfEdges)
                delendi.Add(e);
            foreach (Edge e in delendi)
                RemoveEdge(e);
            NodeMap.Remove(node.Id);
            GeometryGraph.Nodes.Remove(node.GeometryObject as Core.Layout.Node);
        }

        /// <summary>
        /// Always adds a new edge,if source or  nodes don't exist they will be created
        /// </summary>
        /// <param name="source">source node id</param>
        /// <param name="edgeLabel">edge labe - can be null</param>
        /// <param name="target">target node id</param>
        /// <returns>Edge</returns>
        public virtual Edge AddEdge(string source, string edgeLabel, string target) {
            string l = edgeLabel;
            if (l == null)
                l = "";
            var edge = new Edge(source, l, target) {SourceNode = AddNode(source), TargetNode = AddNode(target)};
            AddPrecalculatedEdge(edge);
            return edge;
        }

        /// <summary>
        /// adds and edge object
        /// </summary>
        /// <param name="edge"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Precalculated")]
        public void AddPrecalculatedEdge(Edge edge) {
            if (edge.Source != edge.Target) {
                edge.SourceNode.AddOutEdge(edge);
                edge.TargetNode.AddInEdge(edge);
            } else
                edge.SourceNode.AddSelfEdge(edge);
        }

        /// <summary>
        /// A lookup function: searching recursively in the subgraphs
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public Node FindNode(string nodeId) {
            return nodeMap[nodeId] as Node;
        }

        /// <summary>
        /// Finds the GeometryNode for the drawing node with the given id.
        /// </summary>
        /// <returns></returns>
        public Microsoft.Msagl.Core.Layout.Node FindGeometryNode(string nodeId)
        {
            if (GeometryGraph != null)
            {
                Node node = nodeMap[nodeId] as Node;
                if (node != null)
                {
                    return GeometryGraph.FindNodeByUserData(node);
                }
            }

            return null;
        }

        /// <summary>
        /// Always adds a new edge,if head or tail nodes don't exist they will be created
        /// </summary>
        /// <param name="source">the source node id</param>
        /// <param name="target">the target node id</param>
        /// <returns>edge</returns>
        public virtual Edge AddEdge(string source, string target) {
            return AddEdge(source, null, target);
        }
        
        /// <summary>
        /// It is very strange, but the layouts don't look not so good if I use Dictionary over strings
        /// </summary>
        internal Hashtable nodeMap = new Hashtable();

        /// <summary>
        /// labels -> nodes 
        /// </summary>
        public Hashtable NodeMap {
            get { return nodeMap; }
        }

        /// <summary>
        /// the map of the ids to their subgraphs
        /// </summary>
        public Dictionary<string, Subgraph> SubgraphMap { get { return subgraphMap; } }

#if TEST_MSAGL
        /// <summary>
        /// visible only in debug
        /// </summary>
        /// <param name="nodeM"></param>
        public void InitNodeMap(Hashtable nodeM) {
            nodeMap = nodeM;
        }
#endif

        
        /// <summary>
        /// The enumeration of edges. One need to be careful with calling Edges.Count() since it enumerates the whole collection
        /// </summary>
        public IEnumerable<Edge> Edges {
            get {
                if (RootSubgraph != null) {
                    foreach (var subgraph in RootSubgraph.AllSubgraphsWidthFirstExcludingSelf()) {
                        foreach (var e in subgraph.OutEdges)
                            yield return e;
                        foreach (var e in subgraph.SelfEdges)
                            yield return e;
                    }
                }
                foreach (var node in Nodes) {
                    foreach (var e in node.OutEdges)
                        yield return e;
                    foreach (var e in node.SelfEdges)
                        yield return e;
                }
            }
        }


        Hashtable idToEdges = new Hashtable();
       


        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")] string id;

        /// <summary>
        /// Graph constructor
        /// </summary>
        /// <param name="label">graph label</param>
        /// <param name="id">graph id</param>
        public Graph(string label, string id) {
            this.id = id;
            Label = new Label();
            Label.Text = label;
            InitAttributes();
        }

        /// <summary>
        /// constructor
        /// </summary>
        public Graph() : this("") {
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="labelPar"></param>
        public Graph(string labelPar) {
            Label = new Label();
            id = Label.Text = labelPar;
            InitAttributes();
        }

        void InitAttributes() {
            attr = new GraphAttr();
        }


        bool directed = true;

        /// <summary>
        /// true is the graph is directed
        /// </summary>
        public bool Directed {
            get { return directed; }
            set { directed = value; }
        }

        internal GeometryGraph geomGraph;

        /// <summary>
        /// underlying graph with pure geometry info
        /// </summary>
        public GeometryGraph GeometryGraph {
            get { return geomGraph;
            }
            set { geomGraph = value; }
        }

        /// <summary>
        /// Creates the corresponding geometry graph
        /// </summary>
        public virtual GeometryGraph CreateGeometryGraph() {
            return GeometryGraph = new GeometryGraphCreator(this).Create();
        }

        /// <summary>
        /// Creates the corresponding layout settings.
        /// </summary>
        /// <returns>The created layout settings.</returns>
        public virtual SugiyamaLayoutSettings CreateLayoutSettings()
        {
            return GeometryGraphCreator.CreateLayoutSettings(this);
        }

#if TEST_MSAGL

        List<Color> debugColors;

        /// <summary>
        /// debug only
        /// </summary>
        public List<Color> DebugColors {
            get { return debugColors; }
            set { debugColors = value; }
        }

        Dictionary<object, Color> colorDictionary = new Dictionary<object, Color>();

        /// <summary>
        /// debug only
        /// </summary>
        public Dictionary<object, Color> ColorDictionary {
            get { return colorDictionary; }
            set { colorDictionary = value; }
        }

        /// <summary>
        /// for debug only
        /// </summary>
        List<ICurve> debugICurves = new List<ICurve>();

        /// <summary>
        ///  field used for debug purposes only 
        /// </summary>
        bool showControlPoints=true;

        /// <summary>
        /// debug only
        /// </summary>
        public bool ShowControlPoints {
            get { return showControlPoints; }
            set { showControlPoints = value; }
        }
#endif

        /// <summary>
        /// the geometry graph
        /// </summary>
        public override GeometryObject GeometryObject {
            get { return GeometryGraph; }
            set { GeometryGraph = (GeometryGraph) value; }
        }
            
#if TEST_MSAGL
        ///<summary>
        ///</summary>
        public List<ICurve> DebugICurves {
            get { return debugICurves; }
            set { debugICurves = value; }
        }

        ///<summary>
        ///</summary>
        public DebugCurve[] DebugCurves {
            get { return geomGraph == null ? null : geomGraph.DebugCurves; }
            set {
                if (geomGraph != null)
                    geomGraph.DebugCurves = value;
            }
        }
#endif

        LayerConstraints layerConstraints = new LayerConstraints();
        Dictionary<string, Subgraph> subgraphMap=new Dictionary<string, Subgraph>();
        

        ///<summary>
        /// 
        ///</summary>
        public LayerConstraints LayerConstraints {
            get { return layerConstraints; }
            set { layerConstraints = value; }
        }

        const string FileExtension = ".msagl";

        /// <summary>
        /// Write the graph to a file
        /// </summary>
        /// <param name="fileName"></param>
        public void Write(string fileName) {
            if (fileName != null) {
                if (!fileName.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
                    fileName += FileExtension;
                using (Stream stream = File.Open(fileName, FileMode.Create)) {
                    WriteToStream(stream);
                }
            }
        }

        /// <summary>
        /// writes the graph to a stream
        /// </summary>
        /// <param name="stream"></param>
        public void WriteToStream(Stream stream) {
            var graphWriter = new GraphWriter(stream, this);
            graphWriter.Write();
        }

        /// <summary>
        /// Reads the graph from a file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Graph Read(string fileName) {
            using (Stream stream = File.OpenRead(fileName)) {
                return ReadGraphFromStream(stream);
            }
        }

        /// <summary>
        /// reads graph from a stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Graph ReadGraphFromStream(Stream stream) {
            var graphReader = new GraphReader(stream);
            return graphReader.Read();
        }
    }
}