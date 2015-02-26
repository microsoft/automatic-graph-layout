/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
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

        Dictionary<Node, Tuple<int, int>> decisionIndex = new Dictionary<Node, Tuple<int, int>>();
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
#if MYDEBUG
                Console.WriteLine("e {0} newE {1} diff {2}", e, newE, e - newE);
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

        private bool IsCircle(ICurve iCurve) {
            Ellipse ellipse = iCurve as Ellipse;
            if (ellipse == null)
                return false;
            return ellipse.AxisA.X == ellipse.AxisB.Y && ellipse.ParStart == 0 && ellipse.ParEnd == Math.PI * 2;
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
        //                Console.WriteLine("mistake");
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

        private bool CirclesAreTooClose(Node a, Node b) {
            var del = a.Center - b.Center;
            var r = (a.Width + b.Width) / 2;
            if (del * del < r * r)
                return true;
            return false;
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

        private void UpdateTree(TreeNode tn) {
            tn.box = Pad(tn.node.BoundingBox, labelSeparation / 2);
            tn = tn.parent;
            while (tn != null) {
                tn.box = tn.l.box;
                tn.box.Add(tn.r.box);
                tn = tn.parent;
            }
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

        private static Rectangle CreateNodeRect(Node node) { return node.BoundingBox; }

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

            Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MST.OverlapRemoval.RemoveOverlaps(g, NodeSeparation);

        }

        IEnumerable<Node> AllLabels() {
            return liveLabels.Concat(fixedLabels).Concat(locations);
        }


        private ICurve[] GetCurves() {
            return
                (from n in AllLabels()
                 select n.BoundaryCurve)
                 .Concat(from l in locations select l.BoundaryCurve).Concat(
                 from e in Edges() select e.Curve != null ? e.Curve : new LineSegment(e.Source.Center, e.Target.Center)).ToArray();
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

    
        private Rectangle ExtendedBoundingBoxOfNode(Node n) {
            var del = new Point(this.locationRadius, -this.locationRadius);
            var bb = n.BoundingBox;
            bb.Add(bb.LeftTop - del);
            bb.Add(bb.RightBottom + del);
            return bb;
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

        internal void RemoveNode(Node label) {
            throw new NotImplementedException();
            //var loc = FindLocationByLabel(label);
            //this.locations.Remove(loc);
            //liveLabels.Remove(label);
            //fixedLabels.Remove(label);
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

        internal IEnumerable<Node> Labels() {
            return this.liveLabels.Concat(fixedLabels);
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
