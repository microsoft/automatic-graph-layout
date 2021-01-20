using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using System.Collections.Generic;

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// Node of the graph
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    public class Node : GeometryObject {
#if TEST_MSAGL

        ///<summary>
        /// used for debugging purposes
        ///</summary>
        public object DebugId { get; set; }
#endif
        #region Fields set by the client

        double padding = 1;
        /// <summary>
        /// Padding around the node: splines should not get closer than padding to the node boundary
        /// </summary>
        public double Padding {
            get { return padding; }
            set { padding = value; }
        }

        ICurve boundaryCurve;
        /// <summary>
        /// The engine assumes that the node boundaryCurve is defined relatively to the point (0,0)
        /// This must be a closed curve.
        /// </summary>
        public virtual ICurve 
            BoundaryCurve {
            get { return boundaryCurve; }
            set {
                RaiseLayoutChangeEvent(value);
                boundaryCurve = value;
            }
        }

        /// <summary>
        /// The default constructor
        /// </summary>
        public Node() { }

        /// <summary>
        /// Creates a Node instance
        /// </summary>
        /// <param name="curve">node boundaryCurve</param>
        public Node(ICurve curve) {
            boundaryCurve = curve;
        }

        /// <summary>
        /// Create a node instance with the given curve and user data.
        /// </summary>
        public Node(ICurve curve, object userData)
        {
            this.boundaryCurve = curve;
            this.UserData = userData;
        }
        
        /// <summary>
        /// Gets the UserData string if present.
        /// </summary>
        /// <returns>The UserData string.</returns>
        public override string ToString() {

            if (UserData != null) {
                var ret = UserData.ToString();
#if TEST_MSAGL
//            if(DebugId!=null)
//                ret+= " "+DebugId.ToString();
#endif
                return ret;
            }

            return base.ToString();
        }
        
        #endregion
        /// <summary>
        /// the list of in edges
        /// </summary>
        protected Set<Edge> inEdges_ = new Set<Edge>();
        /// <summary>
        /// enumeration of the node incoming edges
        /// </summary>
        virtual public IEnumerable<Edge> InEdges {
            get { return inEdges_; }
            set { inEdges_ = (Set<Edge>)value; }
        }
        /// <summary>
        /// the list of out edges
        /// </summary>
        protected Set<Edge> outEdges_ = new Set<Edge>();

        /// <summary>
        /// enumeration of the node outcoming edges
        /// </summary>
       virtual public IEnumerable<Edge> OutEdges {
            get { return outEdges_; }
            set { outEdges_ = (Set<Edge>)value; }
        }
        /// <summary>
        /// the list of self edges
        /// </summary>
        protected Set<Edge> selfEdges_ = new Set<Edge>();
        /// <summary>
        ///enumeration of the node self edges
        /// </summary>
       virtual public IEnumerable<Edge> SelfEdges {
            get { return selfEdges_; }
            set { selfEdges_ = (Set<Edge>)value; }
        }

        Cluster clusterParent = null;

        /// <summary>
        /// Parents (if any) of which this node is a member
        /// </summary>
        public Cluster ClusterParent
        {
            get {
                   return this.clusterParent;
            }
        }

        /// <summary>
        /// Walk up the ancestor chain for this node
        /// </summary>
        /// <value>an IEnumerable of ancestor clusters</value>
        public IEnumerable<Cluster> AllClusterAncestors
        {
            get
            {
                Cluster parent = this.ClusterParent;
                while (parent != null)
                {
                    yield return parent;
                    parent = parent.ClusterParent;
                }
            }
        }

        /// <summary>
        /// Add the parent cluster to this node's list of parents
        /// </summary>
        /// <param name="parent"></param>
        public void AddClusterParent(Cluster parent)
        {
            ValidateArg.IsNotNull(parent, "parent");
            Debug.Assert(this.clusterParent == null);

            Debug.Assert(parent != this);
            clusterParent = parent;
        }

        /// <summary>
        /// removes a self edge
        /// </summary>
        /// <param name="edge"></param>
        public bool RemoveSelfEdge(Edge edge) {
            return selfEdges_.Remove(edge);
        }

        /// <summary>
        /// adds and outgoing edge
        /// </summary>
        /// <param name="edge"></param>
        public void AddOutEdge(Edge edge) {
            ValidateArg.IsNotNull(edge, "edge");
            Debug.Assert(edge.Source != edge.Target);
            Debug.Assert(edge.Source == this);
            outEdges_.Insert(edge);
        }

        /// <summary>
        /// add an incoming edge
        /// </summary>
        /// <param name="edge"></param>
        public void AddInEdge(Edge edge) {
            ValidateArg.IsNotNull(edge, "edge");
            Debug.Assert(edge.Source != edge.Target);
            Debug.Assert(edge.Target == this);
            inEdges_.Insert(edge);
        }
        /// <summary>
        /// adds a self edge
        /// </summary>
        /// <param name="edge"></param>
        public void AddSelfEdge(Edge edge) {
            ValidateArg.IsNotNull(edge, "edge");
            Debug.Assert(edge.Target == this && edge.Source == this);
            selfEdges_.Insert(edge);
        }
        /// <summary>
        /// enumerates over all edges
        /// </summary>
        public IEnumerable<Edge> Edges {
            get {
                foreach (Edge e in outEdges_)
                    yield return e;
                foreach (Edge e in inEdges_)
                    yield return e;
                foreach (Edge e in selfEdges_)
                    yield return e;
            }
        }

        #region Fields which are set by Msagl
     //   Point center;
        /// <summary>
        /// return the center of the curve bounding box
        /// </summary>
        public Point Center {
            get { return BoundaryCurve.BoundingBox.Center; }
            set {
                var del = value - Center;
                if (del.X==0 && del.Y==0) return;
                RaiseLayoutChangeEvent(value);
                //an optimization can be appied here; move the boundary curve only on demand
                BoundaryCurve.Translate(del);
            }
        }

        /// <summary>
        /// sets the bounding curve scaled to fit the targetBounds
        /// </summary>
        /// <param name="targetBounds"></param>
        protected void FitBoundaryCurveToTarget(Rectangle targetBounds)
        {
            if (BoundaryCurve != null)
            {
                // RoundedRect is special, rather than simply scaling the geometry we want to keep the corner radii constant
                RoundedRect rr = BoundaryCurve as RoundedRect;
                if (rr == null)
                {
                    Debug.Assert(BoundaryCurve.BoundingBox.Width > 0);
                    Debug.Assert(BoundaryCurve.BoundingBox.Height > 0);
                    double scaleX = targetBounds.Width / BoundaryCurve.BoundingBox.Width;
                    double scaleY = targetBounds.Height / BoundaryCurve.BoundingBox.Height;

                    BoundaryCurve.Translate(-BoundaryCurve.BoundingBox.LeftBottom);
                    BoundaryCurve = BoundaryCurve.ScaleFromOrigin(scaleX, scaleY);
                    BoundaryCurve.Translate(targetBounds.LeftBottom);
                }
                else
                {
                    BoundaryCurve = rr.FitTo(targetBounds);
                }
                Debug.Assert(ApproximateComparer.Close(BoundaryCurve.BoundingBox, targetBounds, ApproximateComparer.UserDefinedTolerance),
                    "FitToBounds didn't succeed in scaling/translating to target bounds");
            }
        }

        /// <summary>
        /// the bounding box of the node
        /// </summary>
        override public Rectangle BoundingBox {
            get 
            {
                    return BoundaryCurve != null ? BoundaryCurve.BoundingBox : Rectangle.CreateAnEmptyBox();
            }
            set
            {
                if(Math.Abs(value.Width - Width) < 0.01 && Math.Abs(value.Height - Height) < 0.01)
                {
                    Center = value.Center;
                }
                else
                {
                    this.FitBoundaryCurveToTarget(value);
                }
            }
        }
        /// <summary>
        /// Width of the node does not include the padding
        /// </summary>
        public double Width { get { return BoundaryCurve.BoundingBox.Width; } }

        /// <summary>
        /// Height of the node does not including the padding
        /// </summary>
        public double Height { get { return BoundaryCurve.BoundingBox.Height; } }

        /// <summary>
        /// returns the node degree
        /// </summary>
        public int Degree {
            get {  return OutEdges.Count()+InEdges.Count()+SelfEdges.Count(); }            
        }

        #endregion

        /// <summary>
        /// removes an outgoing edge
        /// </summary>
        /// <param name="edge"></param>
        /// <returns>True if the node is adjacent to the edge , and false otherwise.</returns>
        public bool RemoveInEdge(Edge edge) {
            return inEdges_.Remove(edge);
        }
        /// <summary>
        /// removes an incoming edge
        /// </summary>
        /// <param name="edge"></param>
        /// <returns>True if the node is adjacent to the edge , and false otherwise.</returns>
        public bool RemoveOutEdge(Edge edge)
        {
           return outEdges_.Remove(edge);
        }
        /// <summary>
        /// remove all edges
        /// </summary>
        public void ClearEdges() {
            inEdges_.Clear();
            outEdges_.Clear();
        }

        ///<summary>
        ///</summary>
        ///<param name="transformation"></param>
        public void Transform(PlaneTransformation transformation) {
            if (BoundaryCurve != null)
                BoundaryCurve = BoundaryCurve.Transform(transformation);
        }


        /// <summary>
        /// Determines if this node is a descendant of the given cluster.
        /// </summary>
        /// <returns>True if the node is a descendant of the cluster.  False otherwise.</returns>
        public bool IsDescendantOf(Cluster cluster)
        {
            return this.AllClusterAncestors.Contains(cluster);            
        }

        internal bool UnderCollapsedCluster() {
            return ClusterParent != null && ClusterParent.IsCollapsed;
        }
    }
}
