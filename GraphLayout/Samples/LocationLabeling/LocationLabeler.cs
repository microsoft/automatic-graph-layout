using System.Collections.Generic;
using Microsoft.Msagl;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using System;
using Microsoft.Msagl.Routing;

namespace LocationLabeling {
    public class LocationLabeler {

        private int giveUpNumber = 10;

        public int GiveUpNumber {
            get { return giveUpNumber; }
            set { giveUpNumber = value; }
        }
        Random random = new Random(1);
        List<Node> labels;
        double locationRadius;
        GeometryGraph graph;
        Set<Node> locations;
        bool routeEdges;

        Dictionary<Node, TreeNode> nodeToTreeNode = new Dictionary<Node, TreeNode>();
        Dictionary<Node, ICurve> nodeToNodeBoundary = new Dictionary<Node, ICurve>();
        Dictionary<Node, Point> nodeToCenter = new Dictionary<Node, Point>();

        double labelSeparation;
        
        /// <summary>
        /// positions location nodes
        /// </summary>
        /// <param name="locationNodes">the nodes represent the labels and originally are positioned at locations</param>
        /// <param name="locationRadius">the minimum gap between a location and its label</param>
        /// <param name="removeCrossings">If set to true will remove intersections between line segments (location, locationLabel). 
        /// The result will be better but the calculation will take more time.
        /// </param>
        public static GeometryGraph PositionLabels(Node[] locationNodes, double locationRadius, bool routeEdges, double labelSeparation) {
#if TEST_MSAGL
            Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            var labeler = new LocationLabeler(locationNodes, locationRadius, routeEdges, labelSeparation);
            labeler.Work();
            return labeler.graph;
        }

        void Work() {
            if (OverlapsFound()) {
                CreateLocationNodesAndGraph();
                RemoveOverlaps();
                RemoveIntersectionsOfStems();
                OptimizePositionsByShiftingRandomly();
                if (routeEdges)
                    RouteEdges();
            } else {
                this.graph = new GeometryGraph();
                foreach (Node n in this.labels)
                    graph.Nodes.Add(n);
            }
        }


        TreeNode rootOfLabels;
        TreeNode rootOfLocs;
        private void OptimizePositionsByShiftingRandomly() {
            PrepareTreesForOverlapDetection();

            int numberOfUnseccsesfulAttemptsInARow = 0;
            SaveCurrentConfig();
                
            double e = GetEnergy();
            do {
                Shift();
                var newE = GetEnergy();
#if TEST_MSAGL
                System.Diagnostics.Debug.WriteLine("e {0} newE {1} diff {2}", e, newE, e - newE);
#endif

                if (newE < e) {
                    e = newE;
                    numberOfUnseccsesfulAttemptsInARow = 0;
                    //RemoveIntersectionsOfStems();
                    //RestoreTree(root);
                    SaveCurrentConfig();
                } else {
                    numberOfUnseccsesfulAttemptsInARow++;
                    RestoreConfig();
                }
                
            } while (numberOfUnseccsesfulAttemptsInARow<giveUpNumber);

            RestoreOffsettedCurveBoundaries();
            
        }

        private void RestoreConfig() {
         
            foreach (var p in nodeToCenter)
                p.Key.Center = p.Value;

            RestoreTree(rootOfLabels);
        }

        private Rectangle RestoreTree(TreeNode t) {
            if (t.node != null)
                return t.box = Pad(t.node.BoundingBox, labelSeparation/2);
            else {
                t.box = RestoreTree(t.l);
                t.box.Add(RestoreTree(t.r));
                return t.box;
            }
                
        }

        private void SaveCurrentConfig() {
            foreach (var node in labels) {
                nodeToNodeBoundary[node] = node.BoundaryCurve.Clone();
                nodeToCenter[node] = node.Center;
            }

        }

        private void Shift() {
            foreach (var label in labels)
                ShiftLabel(label);
        }

        private void ShiftLabel(Node label) {
            int nOfTries = 200;
            var curve = nodeToNodeBoundary[label];
            var center = nodeToCenter[label];
            var dir = FindLocationByLabel(label).Center - label.Center;

            while (nOfTries-- > 0) {
                double angle = Math.PI * random.NextDouble() - Math.PI / 2;
                double r = random.NextDouble();

                //only one time from 4 jump over the location
                int rn = random.Next(7);
                var del = r * dir.Rotate(angle);
                label.Center += del;

                if (LabelIsSeparatedFromOtherLabels(label, rootOfLabels) && LabelIsSeparatedFromLocations(label, rootOfLocs)) {
                    UpdateTreeOfLabels(nodeToTreeNode[label]);
                    return;
                }
            }
            label.Center = center;
        }

        private void UpdateTreeOfLabels(TreeNode t) {
            t.box = t.node.BoundingBox;
            t = t.parent;
            while (t != null) {
                t.box = t.l.box;
                t.box.Add(t.r.box);
                t = t.parent;
            }
        }

        private bool LabelIsSeparatedFromLocations(Node loc, TreeNode t) {
            if (t == null)
                return true;
            if (loc.BoundingBox.Intersects(t.box) == false)
                return true;
            if (t.node == null)
                return LabelIsSeparatedFromLocations(loc, t.l) && LabelIsSeparatedFromLocations(loc, t.r);

            var r = loc.Width / 2 + t.node.Width / 2;
            var del = loc.Center - t.node.Center;
            return del * del > r * r;
        }

        private bool LabelIsSeparatedFromOtherLabels(Node label, TreeNode t) {
            if (t == null)
                return true;
            if (label.BoundingBox.Intersects(t.box) == false)
                return true;
            if (t.node == null)
                return LabelIsSeparatedFromOtherLabels(label, t.l) && LabelIsSeparatedFromOtherLabels(label, t.r);

            if (t.node == label)
                return true;
            var r = label.Width / 2 + t.node.Width / 2 + labelSeparation;
            var del = label.Center - t.node.Center;
            return del * del > r * r;
      }

        //private void CheckLabelOverlap() {
        //    var ls = labels.ToArray();
        //    for (int i = 0; i < ls.Length ; i++)
        //        for (int j = i + 1; j < ls.Length; j++)
        //            if (CirclesAreTooClose(ls[i], ls[j]))
        //                System.Diagnostics.Debug.WriteLine("mistake");
        //}


        //private void CheckTree() {
            
        //    bool r=CheckTreeOnNode(root);

        //    System.Diagnostics.Debug.Assert(r);
        //}

        private bool CheckTreeOnNode(TreeNode t) {
            if (t == null)
                return true;

            if (t.node != null)
                if( t.box.Contains(t.node.BoundingBox)   )
                    return true;
                else 
                    return false;
            else
                return t.box.Contains(t.l.box) && t.box.Contains(t.r.box) && CheckTreeOnNode(t.l) &&
                    CheckTreeOnNode(t.r);
        }

        private double GetEnergy() {
            return (from n in labels let p=FindLocationByLabel(n).Center let t=p-n.Center select t*t).Sum();
        }

        private void RestoreOffsettedCurveBoundaries() {
            ////restore node boundary curves
            //foreach (var node in this.locations)
            //    node.BoundaryCurve = node.BoundaryCurve.OffsetCurve(this.locationRadius / 2,
            //        node.Center + new Point(node.Width, 0));

            //foreach (var node in this.labels)
            //    node.BoundaryCurve = node.BoundaryCurve.OffsetCurve(this.locationRadius / 2, node.Center);
        }

        Rectangle Pad(Rectangle box, double padding) {
          
            box.Width += 2 * padding;
            if (box.Width < 0)
                box.Width = 0;
            box.Height += 2 * padding;
            if (box.Height < 0)
                box.Height = 0;

            return box;

        }

        private void PrepareTreesForOverlapDetection() {
            rootOfLabels = new TreeNode(null);
           
            foreach (var lab in this.labels) 
                AddNodeToTree(rootOfLabels, lab, Pad(lab.BoundingBox, this.labelSeparation/2));

            rootOfLocs = new TreeNode(null);

            foreach (var loc in this.locations) {
                //node.BoundaryCurve = node.BoundaryCurve.OffsetCurve(this.locationRadius / 2,
                //    node.Center + new Point(node.Width, 0));
                AddNodeToTree(rootOfLocs, loc, Pad(loc.BoundingBox, this.labelSeparation/2+this.locationRadius));                
            }

            FillNodeToTreeNodeMap(rootOfLabels);
        }


        private void FillNodeToTreeNodeMap(TreeNode tn) {
            if (tn == null)
                return;
            if (tn.node != null) 
                nodeToTreeNode[tn.node] = tn;
            
            FillNodeToTreeNodeMap(tn.l);
            FillNodeToTreeNodeMap(tn.r);
        }

       

        private void AddNodeToTree(TreeNode rootTreeNode, Node node, Rectangle box) {
            AddNodeToTreeWithoutOverlaps(rootTreeNode, ref box, node);
        }

        private void AddNodeToTreeWithoutOverlaps(TreeNode tn, ref Rectangle box, Node node) {
            if (tn.count == 0) {
                tn.node = node;
                tn.box = box;
            } else if (tn.count == 1) {
                if (tn.node == null) {
                    System.Diagnostics.Debug.Assert(tn.l != null);
                    TreeNode t = tn.r = new TreeNode(tn);
                    t.node = node;
                    t.count = 1;
                    t.box = box;
                } else {
                    tn.l = new TreeNode(tn);
                    tn.l.box = tn.box;
                    tn.l.node = tn.node;
                    tn.l.count = 1;
                    tn.node = null;
                    tn.r = new TreeNode(tn);
                    tn.r.node = node;
                    tn.r.box = box;
                    tn.r.count = 1;
                }
            } else if (tn.l.count * 2 < tn.r.count)
                AddNodeToTreeWithoutOverlaps(tn.l, ref box, node);
            else if (tn.r.count * 2 < tn.l.count)
                AddNodeToTreeWithoutOverlaps(tn.r, ref box, node);
            else {
                double leftGrouth = CommonArea(ref tn.l.box, ref box) - tn.l.box.Area;
                double rigthGrouth = CommonArea(ref tn.r.box, ref box) - tn.r.box.Area;
                if (leftGrouth < rigthGrouth)
                    AddNodeToTreeWithoutOverlaps(tn.l, ref box, node);
                else
                    AddNodeToTreeWithoutOverlaps(tn.r, ref box, node);
           }
            tn.count++;
            tn.box.Add(box);
            // ShowRoot();
        }

        static double CommonArea(ref Rectangle a, ref Rectangle b) {
            double l = Math.Min(a.Left, b.Left);
            double r = Math.Max(a.Right, b.Right);
            double t = Math.Max(a.Top, b.Top);
            double bt = Math.Min(a.Bottom, b.Bottom);
            return (r - l) * (t - bt);

        }

        private void RemoveIntersectionsOfStems() {
            while (RemoveIntersections(labels.ToArray())); //can take a while
     }

        private bool RemoveIntersections(Node[]labels) {
            double energy = GetEnergy();
            bool ret=false;
            for (int i = 0; i < labels.Length; i++)
                for (int j = i + 1; j < labels.Length; j++)
                    ret = (RemoveIntersection(labels[i], labels[j]) || ret);
            return ret && energy > GetEnergy();
        }

        private bool RemoveIntersection(Node a, Node b) {
            Point t;
            if (LineSegment.Intersect(a.Center, LocationNodeOfLabel(a).Center, b.Center, LocationNodeOfLabel(b).Center, out t)) {
                Point tmpa = a.Center;
                a.Center = b.Center;
                b.Center = tmpa;
                return true;
            }
            return false;
        }

        Node LocationNodeOfLabel(Node label) {
            return label.OutEdges.First().Target;
        }

        double NodeSeparation = 9;
        private void RemoveOverlaps() {
            var g=new GeometryGraph();
            NodeSeparation = 2* Math.Max(NodeSeparation, (from label in labels select 2 * Math.Max(label.Width, label.Height)).Max());
            foreach (var node in this.labels)
                g.Nodes.Add(node);
            Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree.GTreeOverlapRemoval.RemoveOverlaps(g.Nodes.ToArray(), NodeSeparation);
            //restore the parent for label nodes
            foreach (var node in this.labels)
                node.GeometryParent = this.graph;
           
        }

        private void RouteEdges() {

            //foreach (var edge in graph.Edges) {
            //    edge.EdgeGeometry.Curve = new LineSegment(edge.Source.Center, edge.Target.Center);
            //}
            //return;

            var edgeRouter = new InteractiveEdgeRouter(from v in this.labels select v.BoundaryCurve,
                this.locationRadius / 3, this.locationRadius / 9, Math.PI / 6);
            edgeRouter.Run();//will calculate the visibility graph
            foreach (var edge in graph.Edges) {
                SmoothedPolyline sp;
                edge.EdgeGeometry.Curve =
                    edgeRouter.RouteSplineFromPortToPortWhenTheWholeGraphIsReady(new FloatingPort( edge.Source.BoundaryCurve, edge.Source.Center), new FloatingPort(null, edge.Target.Center), true, out sp);
                TrimAtSource(edge);
            }
        }

        private void TrimAtSource(Edge edge) {
          //pick the intersection furthest from the source            
            IntersectionInfo x=Curve.GetAllIntersections(edge.Source.BoundaryCurve, edge.Curve, true).Aggregate((a,b)=> a.Par1>b.Par1?a:b);
            edge.Curve = edge.Curve.Trim(x.Par1, edge.Curve.ParEnd);
        }

        private Node FindLocationByLabel(Node node) {
            foreach (var edge in node.OutEdges)
                return edge.Target;
            throw new Exception();
        }

       
        private void CreateLocationNodesAndGraph() {
            this.locations = new Set<Node>();
            this.graph = new GeometryGraph();
            foreach (Node n in this.labels) {
                Node locationNode = new Node(
                    CurveFactory.CreateEllipse(this.locationRadius, this.locationRadius, n.Center), GetUniqueId(n.UserData.ToString()));
                locationNode.Center = n.Center;
                locations.Insert(locationNode);
                graph.Nodes.Add(n); 
                graph.Nodes.Add(locationNode);
                graph.Edges.Add(new Edge(n, locationNode));
            }
        }

        int idCounter;
        private string GetUniqueId(IComparable p) {
            string id;
            do
                id = p.ToString() + idCounter++;
            while (graph.FindNodeByUserData(id) != null);
            return id;
        }

        private bool OverlapsFound() {
            IList<RectangleNode<Node, Point>> rects;
            var rectNode = RectangleNode<Node, Point>.CreateRectangleNodeOnListOfNodes(rects = CreateRectanglesAroundNodes());
            foreach (var r in rects) {
                if ((from rn in rectNode.GetNodeItemsIntersectingRectangle(r.Rectangle) where rn != r.UserData select rn).Any())
                    return true;
            }
            return false;

        }

        private IList<RectangleNode<Node, Point>> CreateRectanglesAroundNodes() {
            return
                new List<RectangleNode<Node, Point>>(
                    from n in this.labels
                    select new RectangleNode<Node, Point>(n, ExtendedBoundingBoxOfNode(n))
                    );
        }


        private Rectangle ExtendedBoundingBoxOfNode(Node n) {
            var del = new Point(this.locationRadius, -this.locationRadius);
            var bb = n.BoundingBox;
            bb.Add(bb.LeftTop - del);
            bb.Add(bb.RightBottom + del);
            return bb;
        }

        LocationLabeler(IEnumerable<Node> nodes, double radius, bool route, double labelSep) {
            this.labelSeparation = labelSep;
            this.routeEdges = route;
            this.labels = new List<Node>(nodes);
            this.locationRadius = radius;
        }
    }


}
