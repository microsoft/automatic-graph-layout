using System;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using P2=Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
namespace Microsoft.Msagl.Drawing {

    /// <summary>
    /// If this delegate is not null and returns true then no node rendering is done by the viewer, the delegate is supposed to do the job.
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="graphics"></param>
    public delegate bool DelegateToOverrideEdgeRendering(Edge edge, object graphics);
    /// <summary>
    /// Edge of Microsoft.Msagl.Drawing
    /// </summary>
    [Serializable]
    public class Edge : DrawingObject, ILabeledObject {

        Core.Layout.Edge geometryEdge;
        /// <summary>
        /// gets and sets the geometry edge
        /// </summary>

        public Core.Layout.Edge GeometryEdge
        {
            get { return geometryEdge; }
            set { geometryEdge = value; }
        }
        /// <summary>
        /// A delegate to draw node
        /// </summary>
        DelegateToOverrideEdgeRendering drawEdgeDelegate;
        /// <summary>
        /// If this delegate is not null and returns true then no node rendering is done
        /// </summary>
        public DelegateToOverrideEdgeRendering DrawEdgeDelegate {
            get { return drawEdgeDelegate; }
            set { drawEdgeDelegate = value; }
        }

        Port sourcePort;
        /// <summary>
        /// Defines the way the edge connects to the source.
        /// The member is used at the moment only when adding an edge to the graph.
        /// </summary>
        public Port SourcePort {
            get { return sourcePort; }
            set { sourcePort = value; }
        }
        Port targetPort;

        /// <summary>
        /// defines the way the edge connects to the target
        /// The member is used at the moment only when adding an edge to the graph.
        /// </summary>
        public Port TargetPort {
            get { return targetPort; }
            set { targetPort = value; }
        }

        Label label;
        /// <summary>
        /// the label of the object
        /// </summary>
        public Label Label {
            get { return label; }
            set { label = value; }
        }

/// <summary>
/// a shortcut to edge label
/// </summary>
        public string LabelText {
            get { return Label == null ? "" : Label.Text; }
            set {
                if (Label == null)
                    Label = new Label { Owner = this };
                Label.Text = value;
            }
        }

/// <summary>
/// the edge bounding box
/// </summary>
        override public Rectangle BoundingBox {
            get {

                if (GeometryEdge == null)
                    return new Rectangle(0, 0, -1, -1);

                Rectangle bb = EdgeCurve.BoundingBox;

                if (Label != null)
                    bb.Add(Label.BoundingBox);
                
                if (this.attr.ArrowAtTarget)
                    bb.Add(ArrowAtTargetPosition);

                if (this.attr.ArrowAtSource)
                    bb.Add(ArrowAtSourcePosition);

                return bb;
            }
        }


        EdgeAttr attr;
        /// <summary>
        /// The edge attribute.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Attr")]
        public EdgeAttr Attr {
            get { return attr; }
            set { attr = value; }
        }
        /// <summary>
        /// The id of the edge source node.
        /// </summary>
        string source;
        /// <summary>
        /// The id of the edge target node
        /// </summary>
        string target;

        /// <summary>
        /// source id, label ,target id
        /// </summary>
        /// <param name="source"> cannot be null</param>
        /// <param name="labelText">label can be null</param>
        /// <param name="target">cannot be null</param>
        public Edge(string source, string labelText, string target) {
            if (String.IsNullOrEmpty(source) || String.IsNullOrEmpty(target))
                throw new InvalidOperationException("Creating an edge with null or empty source or target IDs");
            this.source = source;
            this.target = target;

            this.attr = new EdgeAttr();
            if (!String.IsNullOrEmpty(labelText))
            {
                Label = new Label(labelText) {Owner = this};
            }
        }

        /// <summary>
        /// creates a detached edge
        /// </summary>
        /// <param name="sourceNode"></param>
        /// <param name="targetNode"></param>
        /// <param name="connection">controls is the edge will be connected to the graph</param>
        public Edge(Node sourceNode, Node targetNode, ConnectionToGraph connection)
            : this(sourceNode.Id, null, targetNode.Id) {
            this.SourceNode = sourceNode;
            this.TargetNode = targetNode;
            if (connection == ConnectionToGraph.Connected) {
                if (sourceNode == targetNode)
                    sourceNode.AddSelfEdge(this);
                else {
                    sourceNode.AddOutEdge(this);
                    targetNode.AddInEdge(this);
                }
            }
        }

        /// <summary>
        /// Head->Tail->Label.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return Utils.Quote(source) + " -> " + Utils.Quote(target) +(Label==null?"":"[" + Label.Text + "]");
        }
        /// <summary>
        /// Head->Tail->Label.
        /// </summary>
        /// <returns></returns>
        public string ToDotGeometry()
        {
            return Utils.Quote(source) + " -> " + Utils.Quote(target) + 
                "[" + Utils.ConcatWithComma((Label == null ? "" : Label.Text), DotGeomString(geometryEdge),this.attr.ToStringWithText(""))+ "]";
        }

        private string DotGeomString(Core.Layout.Edge geometryEdge)
        {
            var edgeGeom = geometryEdge.EdgeGeometry;
            if (edgeGeom == null) return "";
            CubicBezierSegment b = edgeGeom.Curve as CubicBezierSegment;
            if (b == null)
                return "";
            string ret = "";
            if (edgeGeom.SourceArrowhead != null)
            {
                ret += string.Format("s,{0},{1} ", edgeGeom.SourceArrowhead.TipPosition.X, edgeGeom.SourceArrowhead.TipPosition.Y);
            }
            if (edgeGeom.TargetArrowhead != null)
            {
                ret += string.Format("e,{0},{1} ", edgeGeom.TargetArrowhead.TipPosition.X, edgeGeom.TargetArrowhead.TipPosition.Y);
            }
            for (int i= 0;i<4;i++)
            ret += string.Format("{0},{1} ", b.B(i).X, b.B(i).Y);
            return "pos=" + Utils.Quote(ret);
        }

        /// <summary>
        /// the edge source node ID
        /// </summary>
        public string Source {
            get { return source; }
        }
/// <summary>
/// the edge target node ID
/// </summary>
        public string Target {
            get { return target; }
        }

        private Node sourceNode;
        /// <summary>
        /// the edge source node
        /// </summary>
        public Node SourceNode {
            get { return sourceNode; }
            internal set { sourceNode = value; }
        }
        private Node targetNode;
        /// <summary>
        /// the edge target node
        /// </summary>
        public Node TargetNode {
            get { return targetNode; }
            internal set { targetNode = value; }
        }
/// <summary>
/// gets the corresponding geometry edge
/// </summary>
        public override GeometryObject GeometryObject {
            get { return GeometryEdge; }
            set { GeometryEdge=(Core.Layout.Edge)value; }
        }
        /// <summary>
        /// gets and sets the edge curve
        /// </summary>
        public ICurve EdgeCurve
        {
            get
            {
                if (this.GeometryEdge == null)
                    return null;
                return this.GeometryEdge.Curve;
            }
            set { this.GeometryEdge.Curve = value; }
        }
        /// <summary>
        /// the arrow position
        /// </summary>
        public P2 ArrowAtTargetPosition
        {
            get
            {
                if (this.GeometryEdge == null || this.GeometryEdge.EdgeGeometry.TargetArrowhead == null)
                    return new P2();
                return this.GeometryEdge.EdgeGeometry.TargetArrowhead.TipPosition;
            }
            set
            {
                if (this.GeometryEdge.EdgeGeometry != null)
                {
                    if (this.GeometryEdge.EdgeGeometry.TargetArrowhead == null)
                    {
                        this.GeometryEdge.EdgeGeometry.TargetArrowhead = new Arrowhead();
                    }
                    this.GeometryEdge.EdgeGeometry.TargetArrowhead.TipPosition = value;
                }
            }
        }
        /// <summary>
        /// the arrow position
        /// </summary>
        public P2 ArrowAtSourcePosition
        {
            get
            {
                if (this.GeometryEdge == null || this.GeometryEdge.EdgeGeometry.SourceArrowhead == null)
                    return new P2();
                return this.GeometryEdge.EdgeGeometry.SourceArrowhead.TipPosition;
            }
            set
            {
                if (this.GeometryEdge.EdgeGeometry != null)
                {
                    if (this.GeometryEdge.EdgeGeometry.SourceArrowhead == null)
                    {
                        this.GeometryEdge.EdgeGeometry.SourceArrowhead = new Arrowhead();
                    }
                    this.GeometryEdge.EdgeGeometry.SourceArrowhead.TipPosition = value;
                }
            }
        }

   
    }
}
