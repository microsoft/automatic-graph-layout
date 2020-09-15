using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing;

namespace LocationLabeling {
    internal class IncrementalLabeler {

        Dictionary<object, Node> labelById = new Dictionary<object, Node>();

        private int giveUpNumber = 10;

        public int GiveUpNumber {
            get { return giveUpNumber; }
            set { giveUpNumber = value; }
        }


        Random random = new Random(1);
        Set<Node> liveLabels = new Set<Node>();
        Set<Node> fixedLabels = new Set<Node>();
        double locationRadius;
        Set<Node> locations = new Set<Node>();
        bool routeEdges;

        public bool RouteEdges {
            get { return routeEdges; }
            set { routeEdges = value; }
        }

        Dictionary<Node, TreeNode> liveNodeToTreeNode = new Dictionary<Node, TreeNode>();
        Dictionary<Node, ICurve> nodeToNodeBoundary = new Dictionary<Node, ICurve>();
        Dictionary<Node, Point> nodeToCenter = new Dictionary<Node, Point>();

        double labelSeparation;

       


        TreeNode liveTree = new TreeNode(null);
        TreeNode fixedTree = new TreeNode(null);
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

            } while (numberOfUnseccsesfulAttemptsInARow < giveUpNumber);

        
        }

        private void RestoreConfig() {
            foreach (var p in nodeToCenter)
                p.Key.Center = p.Value;

            RestoreTree(liveTree);
        }

        private Rectangle RestoreTree(TreeNode t) {
            if (t.node != null)
                return t.box = Pad(t.node.BoundingBox, labelSeparation / 2);
            t.box = RestoreTree(t.l);
            t.box.Add(RestoreTree(t.r));
            return t.box;
        }

        private void SaveCurrentConfig() {
            foreach (var node in liveLabels) {
                nodeToNodeBoundary[node] = node.BoundaryCurve.Clone();
                nodeToCenter[node] = node.Center;
            }

        }

        private void Shift() {
            foreach (var label in liveLabels)
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

                if (LabelIsSeparatedFromOtherLabels(label, liveTree) && LabelIsSeparatedFromLocations(label, fixedTree)) {
                    UpdateTreeOfLabels(liveNodeToTreeNode[label]);
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

        private bool LabelIsSeparatedFromLocations(Node label, TreeNode t) {
            if (t == null)
                return true;
            if (label.BoundingBox.Intersects(t.box) == false)
                return true;
            if (t.node == null)
                return LabelIsSeparatedFromLocations(label, t.l) && LabelIsSeparatedFromLocations(label, t.r);

            var r = label.Width / 2 + t.node.Width / 2;
            var del = label.Center - t.node.Center;
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
                if (t.box.Contains(t.node.BoundingBox))
                    return true;
                else
                    return false;
            else
                return t.box.Contains(t.l.box) && t.box.Contains(t.r.box) && CheckTreeOnNode(t.l) &&
                    CheckTreeOnNode(t.r);
        }

        private void FillList(List<ICurve> listOfTreeCurves, TreeNode treeNode) {
            listOfTreeCurves.Add(BoundingBoxCurve(ref treeNode.box));
            if (treeNode.node != null)
                listOfTreeCurves.Add(treeNode.node.BoundaryCurve);
            else {
                FillList(listOfTreeCurves, treeNode.l);
                FillList(listOfTreeCurves, treeNode.r);
            }
        }

        private ICurve BoundingBoxCurve(ref Rectangle rectangle) {
            Curve c = new Curve();
            c.AddSegment(new LineSegment(rectangle.LeftTop, rectangle.LeftBottom));
            Curve.ContinueWithLineSegment(c, rectangle.RightBottom);
            Curve.ContinueWithLineSegment(c, rectangle.RightTop);
            Curve.CloseCurve(c);
            return c;
        }


        private double GetEnergy() {
            return (from n in liveLabels let p = FindLocationByLabel(n).Center let t = p - n.Center select t * t).Sum();
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
            liveTree = new TreeNode(null);

            foreach (var lab in this.liveLabels)
                AddNodeToTree(liveTree, lab, Pad(lab.BoundingBox, this.labelSeparation / 2));

            liveNodeToTreeNode.Clear();
            FillNodeToTreeNodeMap(liveTree);
        }


        private void FillNodeToTreeNodeMap(TreeNode tn) {
            if (tn == null)
                return;
            if (tn.node != null)
                liveNodeToTreeNode[tn.node] = tn;

            FillNodeToTreeNodeMap(tn.l);
            FillNodeToTreeNodeMap(tn.r);
        }

        private void AddNodeToTree(TreeNode tn, Node node, Rectangle box) {
            AddNodeToTreeWithoutOverlaps(tn, ref box, node);
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
            while (RemoveIntersections(liveLabels.ToArray())) ; //can take a while
        }

        private bool RemoveIntersections(Node[] labels) {
            double energy = GetEnergy();
            bool ret = false;
            for (int i = 0; i < labels.Length; i++)
                for (int j = i + 1; j < labels.Length; j++)
                    ret = (RemoveIntersection(labels[i], labels[j]) || ret);
            return ret && energy > GetEnergy();
        }

        private bool RemoveIntersection(Node a, Node b) {
            Point t;
            if (LineSegment.Intersect(a.Center, LocationNodeOfLabel(a).Center, b.Center, LocationNodeOfLabel(b).Center, out t)) {
                var tmpa = a.Center;
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
            var g = new GeometryGraph();
            NodeSeparation = 2 * Math.Max(NodeSeparation, (from label in liveLabels select 2 * Math.Max(label.Width, label.Height)).Max());
            foreach (var node in this.liveLabels.Concat(fixedLabels))
                g.Nodes.Add(node);

            Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree.GTreeOverlapRemoval.RemoveOverlaps(g.Nodes.ToArray(), NodeSeparation);

        }

        IEnumerable<Node> AllLabels() {
            return liveLabels.Concat(fixedLabels).Concat(locations);
        }

        IEnumerable<Edge> Edges() {
            return from n in AllLabels() from e in n.OutEdges select e;
        }

        private void Route() {

            //foreach (var edge in graph.Edges) {
            //    edge.EdgeGeometry.Curve = new LineSegment(edge.Source.Center, edge.Target.Center);
            //}
            //return;

            var edgeRouter = new InteractiveEdgeRouter(from v in AllLabels() select v.BoundaryCurve, this.locationRadius / 3, this.locationRadius/9, Math.PI/6);
            edgeRouter.Run();//it will calculate the visibility graph
            edgeRouter.GetVisibilityGraph();
            foreach (var edge in Edges()) {
                SmoothedPolyline sp;
                edge.EdgeGeometry.Curve =
                    edgeRouter.RouteSplineFromPortToPortWhenTheWholeGraphIsReady(new FloatingPort(edge.Source.BoundaryCurve, edge.Source.Center), new FloatingPort(null, edge.Target.Center), true, out sp);
                TrimAtSource(edge);
            }
        }

        private void TrimAtSource(Edge edge) {
            //pick the intersection furthest from the source            
            IntersectionInfo x = Curve.GetAllIntersections(edge.Source.BoundaryCurve, edge.Curve, true).Aggregate((a, b) => a.Par1 > b.Par1 ? a : b);
            edge.Curve = edge.Curve.Trim(x.Par1, edge.Curve.ParEnd);
        }



        private Node FindLocationByLabel(Node node) {
            foreach (var edge in node.OutEdges)
                return edge.Target;
            throw new Exception();
        }

        private bool OverlapsFound() {

            foreach (var label in liveLabels) {
                if (!LabelIsSeparatedFromOtherLabels(label, liveTree))
                    return true;
                if (!LabelIsSeparatedFromLocations(label, fixedTree))
                    return true;
            }

            return false;

        }

        internal IncrementalLabeler(double radius, bool route, double labelSep) {
            this.labelSeparation = labelSep;
            this.routeEdges = route;
            this.locationRadius = radius;
        }

        internal void AddNode(Node label) {
            liveLabels.Insert(label);
            var loc = new Node(
                    CurveFactory.CreateEllipse(this.locationRadius, this.locationRadius, label.Center), label.UserData + "_");
            loc.Center = label.Center;          
            label.AddOutEdge(new Edge(label, loc));
            this.locations.Insert(loc);
            AddNodeToTree(fixedTree, loc, Pad(loc.BoundingBox, this.labelSeparation / 2 + this.locationRadius));
        }

        internal void Layout() {
            if (OverlapsFound()) {
                RemoveOverlaps();
                RemoveIntersectionsOfStems();
                
                OptimizePositionsByShiftingRandomly();
                if (routeEdges)
                    Route();
            }

            foreach (var label in this.liveLabels) {
                AddNodeToTree(this.fixedTree, label, Pad(label.BoundingBox, labelSeparation/2));
                fixedLabels.Insert(label);
            }


            labelById.Clear();
            foreach (var node in this.fixedLabels)
                labelById[node.UserData] = node;

            liveLabels.Clear();
        }

        /// <summary>
        /// move the location and its label uniformly to the new place
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newPosition"></param>
           public void ChangeLocation(string id, Point newPosition) {
            Node label = labelById[id];
            Node location = FindLocationByLabel(label);
            
            Point del = newPosition - location.Center;
            location.Center = newPosition;
            label.Center += del;
        }
        /// <summary>
        /// update the internal data depending on the positions
        /// </summary>
        public void UpdateLocations() {
            fixedTree = new TreeNode(null);
            foreach (var label in fixedLabels) {
                var loc=FindLocationByLabel(label);
                AddNodeToTree(fixedTree, label, Pad(label.BoundingBox, labelSeparation/2));
                AddNodeToTree(fixedTree, loc, Pad(loc.BoundingBox, locationRadius+labelSeparation/2));
            }
        }

    }
}
