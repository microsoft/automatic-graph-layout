using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    ///     A cluster has a list of nodes and a list of nested clusters
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    public class Cluster : Node {
        bool isCollapsed;

        /// <summary>
        ///     this flag should be respected by layout algorithms
        /// </summary>
        public bool IsCollapsed {
            get { return isCollapsed; }
            set { isCollapsed = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public override ICurve BoundaryCurve {
            get { return IsCollapsed ? CollapsedBoundary : base.BoundaryCurve; }
            set { base.BoundaryCurve = value; }
        }

        event EventHandler<LayoutChangeEventArgs> layoutDoneEvent;

        /// <summary>
        ///     event signalling that the layout is done
        /// </summary>
        public event EventHandler<LayoutChangeEventArgs> LayoutDoneEvent {
            add { layoutDoneEvent += value; }
            remove { layoutDoneEvent -= value; }
        }

        internal Point Barycenter; // Filled in by SetBarycenter
        ICurve collapsedBoundary;

        /// <summary>
        ///     the boundary curve when the cluster is collapsed
        /// </summary>
        public ICurve CollapsedBoundary {
            get { return collapsedBoundary; }
            set { collapsedBoundary = value; }
        }

        internal List<Cluster> clusters = new List<Cluster>();
        internal List<Node> nodes = new List<Node>();

        /// <summary>
        /// </summary>
        public Cluster() : this(new Point()) {}

        /// <summary>
        ///     Bottom-most ctor.
        /// </summary>
        /// <param name="origin"></param>
        public Cluster(Point origin) {
            Weight = 0;
            Barycenter = origin;
        }

        /// <summary>
        ///     Construct a cluster with the specified nodes as members
        /// </summary>
        /// <param name="nodes"></param>
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public Cluster(IEnumerable<Node> nodes)
            : this() {
            ValidateArg.IsNotNull(nodes, "nodes");
            foreach (Node v in nodes) {
                AddNode(v);
            }
        }

        /// <summary>
        ///     Construct a cluster with the specified nodes and clusters as child members
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="clusters"></param>
        public Cluster(IEnumerable<Node> nodes, IEnumerable<Cluster> clusters)
            : this(nodes) {
            ValidateArg.IsNotNull(clusters, "clusters");
            foreach (Cluster c in clusters) {
                AddCluster(c);
            }
        }

        /// <summary>
        ///     Clusters can (optionally) have a rectangular border which is respected by overlap avoidance.
        ///     Currently, this is controlled by FastIncrementalLayoutSettings.RectangularClusters.
        ///     If FastIncrementalLayoutSettings.RectangularClusters is true, then the
        ///     FastIncrementalLayout constructor will create a RectangularBoundary in each cluster.
        ///     Otherwise it will be null.
        /// </summary>
        public RectangularClusterBoundary RectangularBoundary { get; set; }

        /// <summary>
        ///     List of member nodes
        /// </summary>
        public IEnumerable<Node> Nodes {
            get { return nodes; }
        }

        /// <summary>
        ///     List of child clusters
        /// </summary>
        public IEnumerable<Cluster> Clusters {
            get { return clusters; }
        }

        /// <summary>
        ///     number of nodes in cluster
        /// </summary>
        public double Weight { get; private set; }

        /// <summary>
        ///     BoundingBox_get uses the RectangularBoundary.rectangle if available, otherwise uses the cluster's content bounds
        ///     BoundingBox_set scales the old bounds to fit the desired bounds
        /// </summary>
        public override Rectangle BoundingBox {
            get {
                if (!IsCollapsed ||CollapsedBoundary==null) {
                    if (RectangularBoundary != null) {
                        return RectangularBoundary.Rect;
                    }

                    // Default to the cluster's content bounds
                    return new Rectangle(nodes.Concat(clusters).Select(n => n.BoundingBox));
                }

                return CollapsedBoundary.BoundingBox;

            }
            set { FitBoundaryCurveToTarget(value); }
        }

        /// <summary>
        ///     Add a child node or cluster.  It is added to the correct list (nodes or clusters) based on type
        /// </summary>
        /// <param name="child"></param>
        public void AddChild(Node child) {
            ValidateArg.IsNotNull(child, "child");
            Debug.Assert(child != this);
            var childCluster = child as Cluster;
            if (childCluster != null) {
                clusters.Add(childCluster);
            }
            else {
                nodes.Add(child);
            }
            child.AddClusterParent(this);
        }

        /// <summary>
        ///     Cleares the child clusters.
        /// </summary>
        public void ClearClusters() {
            clusters.Clear();
        }

        /// <summary>
        ///     Compute the total weight of all nodes and clusters in this cluster.
        /// </summary>
        /// <returns></returns>
        public double ComputeWeight() {
            Weight = nodes.Count;
            foreach (Cluster c in clusters) {
                Weight += c.ComputeWeight();
            }
            return Weight;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Barycenter"),
         SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "SetBarycenter")]
        public Point SetBarycenter() {
            Barycenter = new Point();

            // If these are empty then Weight is 0 and barycenter becomes NaN.
            // If there are no child clusters with nodes, then Weight stays 0.
            if ((0 != nodes.Count) || (0 != clusters.Count)) {
                if (0 == Weight) {
                    ComputeWeight();
                }
                if (0 != Weight) {
                    foreach (Node v in nodes) {
                        //double wv = ((FastIncrementalLayout.Node)v.AlgorithmData).stayWeight;
                        //p+=wv*v.Center;
                        //w += wv;
                        Barycenter += v.Center;
                    }
                    foreach (Cluster c in clusters) {
                        // SetBarycenter ensures Weight is calculated so call it first.
                        Barycenter += c.SetBarycenter()*c.Weight;
                    }
                    Barycenter /= Weight;
                }
            }
            Debug.Assert(!Double.IsNaN(Barycenter.X) && !Double.IsNaN(Barycenter.Y));
            return Barycenter;
        }


        /// <summary>
        ///     TODO: Check all the places we use this and make sure we don't have O(n^2) complexity
        /// </summary>
        /// <returns>This cluster and all clusters beneath this one, in depth first order</returns>
        public IEnumerable<Cluster> AllClustersDepthFirst() {
            foreach (Cluster c in clusters) {
                foreach (Cluster d in c.AllClustersDepthFirst()) {
                    yield return d;
                }
            }
            yield return this;
        }


        /// <summary>
        /// </summary>
        /// <returns>This cluster and all clusters beneath this one, in width first order</returns>
        public IEnumerable<Node> AllSuccessorsWidthFirst() {
            foreach (Node n in Nodes)
                yield return n;

            foreach (Cluster c in clusters) {
                yield return c;
                foreach (Node n in c.AllSuccessorsWidthFirst())
                    yield return n;
            }
        }

       

        /// <summary>
        /// </summary>
        /// <returns>This cluster and all clusters beneath this one, in depth first order</returns>
        public IEnumerable<Cluster> AllClustersDepthFirstExcludingSelf() {
            foreach (Cluster c in clusters) {
                foreach (Cluster d in c.AllClustersDepthFirst()) {
                    yield return d;
                }
            }
        }

        /// <summary>
        ///     TODO: Check all the places we use this and make sure we don't have O(n^2) complexity
        /// </summary>
        /// <param name="f"></param>
        internal void ForEachNode(Action<Node> f) {
            foreach (Node v in nodes) {
                f(v);
            }
            foreach (Cluster c in clusters) {
                c.ForEachNode(f);
            }
        }

        /// <summary>
        ///     Remove the specified cluster from the list of children of this cluster
        /// </summary>
        /// <param name="cluster"></param>
        public void RemoveCluster(Cluster cluster) {
            clusters.Remove(cluster);
        }

        /// <summary>
        ///     Translates the cluster's contents by the delta.
        /// </summary>
        public void DeepContentsTranslation(Point delta, bool translateEdges) {
            foreach (Cluster c in AllClustersDepthFirst()) {
                foreach (Node v in c.Nodes.Concat(c.Clusters.Cast<Node>())) {
                    var cluster = v as Cluster;
                    if (cluster != null) {
                        cluster.RectangularBoundary.TranslateRectangle(delta);
                    }
                    v.Center += delta;
                    if (translateEdges) {
                        foreach (Edge e in EdgesIncomingToNodeWithDescendantSource(v)) {
                            e.Translate(delta);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Get edges both of whose end-points are immediate children of this cluster
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Edge> ChildEdges() {
            return Nodes.Concat(Clusters).SelectMany(EdgesIncomingToNodeWithChildSource);
        }

        /// <summary>
        ///     get the edges incoming to the specified node where the source of the edge is a descendant of this cluster.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        IEnumerable<Edge> EdgesIncomingToNodeWithDescendantSource(Node node) {
            return node.InEdges.Concat(node.SelfEdges).Where(e => e.Source.IsDescendantOf(this));
        }

        /// <summary>
        ///     get the edges incoming to the specified node where the source of an edge is an immediate child of this cluster.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        IEnumerable<Edge> EdgesIncomingToNodeWithChildSource(Node node) {
            return node.InEdges.Concat(node.SelfEdges).Where(e => e.Source.ClusterParent==this);
        }

        /// <summary>
        ///     Translates the cluster and all of it's contents by the delta.
        /// </summary>
        internal void DeepTranslation(Point delta, bool translateEdges) {
            DeepContentsTranslation(delta, translateEdges);
            RectangularBoundary.TranslateRectangle(delta);
            Center += delta;
        }

        /// <summary>
        /// </summary>
        /// <param name="padding"></param>
        public void CalculateBoundsFromChildren(double padding) {
            var r = new Rectangle((from v in Nodes select v.BoundingBox).Concat(
                from d in Clusters select d.BoundingBox));
            r.Pad(padding);
            UpdateBoundary(r);
        }

        internal void UpdateBoundary(Rectangle bounds) {
            Rectangle r = bounds;
            if (RectangularBoundary != null) {
                r = new Rectangle(
                    r.Left - RectangularBoundary.LeftMargin,
                    r.Bottom - RectangularBoundary.BottomMargin,
                    r.Right + RectangularBoundary.RightMargin,
                    r.Top + RectangularBoundary.TopMargin);
                double widthPad = (RectangularBoundary.MinWidth - r.Width)/2;
                if (widthPad > 0) {
                    r.PadWidth(widthPad);
                }
                double heightPad = (RectangularBoundary.MinHeight - r.Height)/2;
                if (heightPad > 0) {
                    r.PadHeight(heightPad);
                }
                RectangularBoundary.Rect = r;
            }
            BoundingBox = r;
        }

        bool isInInitialLayoutState;


        
        /// <summary>
        ///     Has the cluster contents been moved or changed since an initial layout was applied?
        /// </summary>
        public bool IsInInitialLayoutState {
            get { return isInInitialLayoutState; }
        }


        /// <summary>
        ///     Calculate cluster's RectangularBoundary to preserve the offsets calculated in initial layout, for example,
        ///     to allow for extra space required for non-shortest path edge routes or for labels.
        /// </summary>
        /// <param name="padding">amount of padding between child node bounding box and expected inner bounds</param>
        public void SetInitialLayoutState(double padding) {
            isInInitialLayoutState = true;
            if (RectangularBoundary != null) {
                RectangularBoundary.StoreDefaultMargin();
                var childBounds =
                    new Rectangle(from v in Nodes.Concat(Clusters) select v.BoundingBox);
                childBounds.Pad(padding);
                RectangularBoundary.LeftMargin = childBounds.Left - RectangularBoundary.Rect.Left;
                RectangularBoundary.RightMargin = RectangularBoundary.Rect.Right - childBounds.Right;
                RectangularBoundary.BottomMargin = childBounds.Bottom - RectangularBoundary.Rect.Bottom;
                RectangularBoundary.TopMargin = RectangularBoundary.Rect.Top - childBounds.Top;
            }
        }

        /// <summary>
        /// Set the initial layout state such that our current margin is stored and the new margin is taken from the given rb
        /// </summary>
        public void SetInitialLayoutState(RectangularClusterBoundary bounds) {
            isInInitialLayoutState = true;
            if (RectangularBoundary != null && bounds != null) {
                RectangularBoundary.StoreDefaultMargin();
                RectangularBoundary.LeftMargin = bounds.LeftMargin;
                RectangularBoundary.RightMargin = bounds.RightMargin;
                RectangularBoundary.BottomMargin = bounds.BottomMargin;
                RectangularBoundary.TopMargin = bounds.TopMargin;
            }
        }

        /// <summary>
        ///     sets IsInitialLayoutState to false and restores the default margins if we have a RectangularBoundary
        /// </summary>
        public void UnsetInitialLayoutState() {
            isInInitialLayoutState = false;
            RectangularClusterBoundary rb = RectangularBoundary;
            if (rb != null) {
                rb.RestoreDefaultMargin();
            }
        }

        /// <summary>
        ///     Unset the initial layout state of this cluster and also all of its ancestors
        /// </summary>
        public void UnsetInitialLayoutStateIncludingAncestors() {
            UnsetInitialLayoutState();
            foreach (Cluster c in AllClusterAncestors) {
                c.UnsetInitialLayoutState();
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Cluster> AllClustersWideFirstExcludingSelf() {
            var q = new Queue<Cluster>();
            foreach (Cluster cluster in Clusters)
                q.Enqueue(cluster);

            while (q.Count > 0) {
                Cluster c = q.Dequeue();
                yield return c;
                foreach (Cluster cluster in c.Clusters)
                    q.Enqueue(cluster);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Cluster> AllClustersWidthFirstExcludingSelfAvoidingChildrenOfCollapsed() {
            var q = new Queue<Cluster>();
            foreach (Cluster cluster in Clusters)
                q.Enqueue(cluster);

            while (q.Count > 0) {
                Cluster c = q.Dequeue();
                yield return c;
                if (c.IsCollapsed) continue;
                foreach (Cluster cluster in c.Clusters)
                    q.Enqueue(cluster);
            }
        }

        /// <summary>
        ///     adding a node without checking that it is a cluster
        /// </summary>
        /// <param name="node"></param>
        internal void AddNode(Node node) {
            node.AddClusterParent(this);
            nodes.Add(node);
        }

        internal void AddCluster(Cluster cluster) {
            cluster.AddClusterParent(this);
            clusters.Add(cluster);
        }

        internal void AddRangeOfCluster(IEnumerable<Cluster> clustersToAdd) {
            foreach (Cluster cluster in clustersToAdd) {
                cluster.AddClusterParent(this);
                clusters.Add(cluster);
            }
        }

        /// <summary>
        ///     to string!
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return UserData != null ? UserData.ToString() : base.ToString();
        }

        internal void RaiseLayoutDoneEvent() {
            if (layoutDoneEvent != null)
                layoutDoneEvent(this, null);
        }
    }
}