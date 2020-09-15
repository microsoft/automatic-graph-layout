using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Layout.Layered {
    //  internal delegate bool Direction(ref Point a, ref Point b, ref Point c);
    internal delegate IEnumerable<Point> Points();

    internal class RefinerBetweenTwoLayers {
        int topNode;
        int bottomNode;
        Site topSite;
        Site bottomSite;
        Site currentTopSite;
        Site currentBottomSite;
        LayerArrays layerArrays;
        ProperLayeredGraph layeredGraph;
        GeometryGraph originalGraph;
        Points topCorners;
        Points bottomCorners;
        Anchor[] anchors;
        Random random = new Random(1);
        double layerSeparation;

        RefinerBetweenTwoLayers(
                int topNodeP,
                int bottomNodeP,
                Site topSiteP,
                LayerArrays layerArraysP,
                ProperLayeredGraph layeredGraphP, GeometryGraph originalGraphP, Anchor[] anchorsP, double layerSeparation) {
            this.topNode = topNodeP;
            this.bottomNode = bottomNodeP;
            this.topSite = topSiteP;
            this.bottomSite = topSiteP.Next;
            this.currentTopSite = topSiteP;
            this.currentBottomSite = topSiteP.Next;
            this.layerArrays = layerArraysP;
            this.layeredGraph = layeredGraphP;
            this.originalGraph = originalGraphP;
            this.anchors = anchorsP;
            this.layerSeparation = layerSeparation;
        }

        internal static void Refine(
            int topNodeP,
            int bottomNode,
            Site topSiteP,
            Anchor[] anchors,
            LayerArrays layerArraysP,
            ProperLayeredGraph layeredGraph,
            GeometryGraph originalGraph,
            double layerSeparation) {
            RefinerBetweenTwoLayers refiner = new RefinerBetweenTwoLayers(topNodeP,
                                                                          bottomNode, topSiteP, layerArraysP,
                                                                          layeredGraph, originalGraph, anchors,
                                                                          layerSeparation);
            refiner.Refine();
        }

        void Refine() {
            Init();
            while (InsertSites());
        }

        private Point FixCorner(Point start, Point corner, Point end) {
            if (start == corner)
                return corner;
            Point a = Point.ClosestPointAtLineSegment(corner, start, end);
            Point offsetInTheChannel = corner - a;
            double y = Math.Abs(offsetInTheChannel.Y);
            double sep = this.layerSeparation / 2.0;
            if (y > sep) //push the return value closer to the corner
                offsetInTheChannel *= (sep / (y * 2));

            return offsetInTheChannel + corner;
        }


        private bool InsertSites() {
            if (this.random.Next(2) == 0)
                return CalculateNewTopSite() | CalculateNewBottomSite();
            else
                return CalculateNewBottomSite() | CalculateNewTopSite();
        }
        /// <summary>
        /// circimvating from the side
        /// </summary>
        /// <returns></returns>
        bool CalculateNewBottomSite() {
            Point mainSeg = currentBottomSite.Point - currentTopSite.Point;
            double cotan = AbsCotan(mainSeg);
            Point vOfNewSite = new Point();//to silence the compiler
            bool someBottomCorners = false;
            foreach (Point p in this.bottomCorners()) {
                double cornerCotan = AbsCotan(p - currentBottomSite.Point);
                if (cornerCotan < cotan) {
                    cotan = cornerCotan;
                    vOfNewSite = p;
                    someBottomCorners = true;
                }
            }

            if (!someBottomCorners)
                return false;
            if (!ApproximateComparer.Close(cotan, AbsCotan(mainSeg))) {
                currentBottomSite = new Site(currentTopSite, FixCorner(currentTopSite.Point, vOfNewSite, currentBottomSite.Point), currentBottomSite);//consider a different FixCorner
                return true;
            }

            return false; //no progress


        }

        private static double AbsCotan(Point mainSeg) {
            return Math.Abs(mainSeg.X / mainSeg.Y);
        }

        private bool CalculateNewTopSite() {
            Point mainSeg = currentBottomSite.Point - currentTopSite.Point;
            double cotan = AbsCotan(mainSeg);
            Point vOfNewSite = new Point();//to silence the compiler
            bool someTopCorners = false;
            foreach (Point p in this.topCorners()) {
                double cornerCotan = AbsCotan(p - currentTopSite.Point);
                if (cornerCotan < cotan) {
                    cotan = cornerCotan;
                    vOfNewSite = p;
                    someTopCorners = true;
                }
            }
            if (!someTopCorners)
                return false;
            if (!ApproximateComparer.Close(cotan, AbsCotan(mainSeg))) {
                currentTopSite = new Site(currentTopSite,
                    FixCorner(currentTopSite.Point, vOfNewSite, currentBottomSite.Point),
                    currentBottomSite
                    );//consider a different FixCorner
                return true;
            }

            return false; //no progress
        }

        //private Site AvoidBottomLayer() {
        //    Point corner;
        //    if (StickingCornerFromTheBottomLayer(out corner)) {
        //        corner = FixCorner(this.currentTopSite.v, corner, this.currentBottomSite.v);
        //        return new Site(this.currentTopSite, corner, this.currentBottomSite);
        //    } else
        //        return null;
        //}

        //private Site AvoidTopLayer() {
        //    Point corner;
        //    if (StickingCornerFromTheTopLayer(out corner)) {
        //        corner = FixCorner(this.currentTopSite.v, corner, this.currentBottomSite.v);
        //        return new Site(this.currentTopSite, corner, this.currentBottomSite);
        //    } else
        //        return null;
        //}

        //private bool StickingCornerFromTheTopLayer(out Point corner) {
        //    corner = this.currentBottomSite.v;
        //    foreach (Point l in this.topCorners()) {
        //        Point p = l;
        //        if (this.counterClockwise(ref currentTopSite.v, ref p, ref corner)) 
        //            corner = p;
        //    }
        //    return corner != this.currentBottomSite.v;
        //}
        //private bool StickingCornerFromTheBottomLayer(out Point corner) {
        //    corner = this.currentTopSite.v;
        //    foreach (Point l in this.bottomCorners()) {
        //        Point p = l;
        //        if (this.counterClockwise(ref currentBottomSite.v, ref p, ref corner))
        //            corner = p;
        //    }
        //    return corner != this.currentTopSite.v;
        //}

        private void Init() {
            if (IsTopToTheLeftOfBottom()) {
                this.topCorners = new Points(CornersToTheRightOfTop);
                this.bottomCorners = new Points(CornersToTheLeftOfBottom);
            } else {
                this.topCorners = new Points(CornersToTheLeftOfTop);
                this.bottomCorners = new Points(CornersToTheRightOfBottom);
            }
        }

        private bool IsTopToTheLeftOfBottom() {
            return (this.topSite.Point.X < this.topSite.Next.Point.X);
        }

        IEnumerable<Point> NodeCorners(int node) {
            foreach (Point p in NodeAnchor(node).PolygonalBoundary)
                yield return p;
        }

        Anchor NodeAnchor(int node) {
            return anchors[node];
        }
        IEnumerable<Point> CornersToTheLeftOfBottom() {
            int bottomPosition = layerArrays.X[this.bottomNode];
            double leftMost = this.currentTopSite.Point.X;
            double rightMost = this.currentBottomSite.Point.X;
            foreach (int node in this.LeftFromTheNode(NodeLayer(bottomNode), bottomPosition,
                NodeKind.Bottom, leftMost, rightMost))
                foreach (Point p in NodeCorners(node))
                    if (p.Y > currentBottomSite.Point.Y && PossibleCorner(leftMost, rightMost, p))
                        yield return p;
        }
        IEnumerable<Point> CornersToTheLeftOfTop() {
            int topPosition = layerArrays.X[this.topNode];
            double leftMost = this.currentBottomSite.Point.X;
            double rightMost = this.currentTopSite.Point.X;
            foreach (int node in this.LeftFromTheNode(NodeLayer(topNode), topPosition, NodeKind.Top, leftMost, rightMost))
                foreach (Point p in NodeCorners(node))
                    if (p.Y < currentTopSite.Point.Y && PossibleCorner(leftMost, rightMost, p))
                        yield return p;
        }
        IEnumerable<Point> CornersToTheRightOfBottom() {
            int bottomPosition = layerArrays.X[this.bottomNode];
            double leftMost = this.currentBottomSite.Point.X;
            double rightMost = this.currentTopSite.Point.X;

            foreach (int node in this.RightFromTheNode(NodeLayer(bottomNode), bottomPosition,
                NodeKind.Bottom, leftMost, rightMost))
                foreach (Point p in NodeCorners(node))
                    if (p.Y > currentBottomSite.Point.Y && PossibleCorner(leftMost, rightMost, p))
                        yield return p;

        }
        IEnumerable<Point> CornersToTheRightOfTop() {
            int topPosition = layerArrays.X[this.topNode];
            double leftMost = this.currentTopSite.Point.X;
            double rightMost = this.currentBottomSite.Point.X;
            foreach (int node in this.RightFromTheNode(NodeLayer(topNode), topPosition, NodeKind.Top, leftMost, rightMost))
                foreach (Point p in NodeCorners(node))
                    if (p.Y < currentTopSite.Point.Y && PossibleCorner(leftMost, rightMost, p))
                        yield return p;
        }

        private static bool PossibleCorner(double leftMost, double rightMost, Point p) {
            return p.X > leftMost && p.X < rightMost;
        }

        private int[] NodeLayer(int j) {
            return layerArrays.Layers[layerArrays.Y[j]];
        }

        //private static bool CounterClockwise(ref Point topPoint, ref Point cornerPoint, ref Point p) {
        //    return Point.GetTriangleOrientation(topPoint, cornerPoint, p) == TriangleOrientation.Counterclockwise;
        //}

        //private static bool Clockwise(ref Point topPoint, ref Point cornerPoint, ref Point p) {
        //    return Point.GetTriangleOrientation(topPoint, cornerPoint, p) == TriangleOrientation.Clockwise;
        //}

        bool IsLabel(int u) {
            return this.anchors[u].RepresentsLabel;
        }

        private bool NodeUCanBeCrossedByNodeV(int u, int v) {
            if (IsLabel(u) || IsLabel(v))
                return false;
            if (this.IsVirtualVertex(u) && this.IsVirtualVertex(v) && AdjacentEdgesIntersect(u, v))
                return true;
            return false;
        }

        private bool AdjacentEdgesIntersect(int u, int v) {
            return Intersect(IncomingEdge(u), IncomingEdge(v)) || Intersect(OutcomingEdge(u), OutcomingEdge(v));
        }

        private bool Intersect(LayerEdge e, LayerEdge m) {
            return (layerArrays.X[e.Source] - layerArrays.X[m.Source]) * (layerArrays.X[e.Target] - layerArrays.X[m.Target]) < 0;
        }

        private LayerEdge IncomingEdge(int u) {
            foreach (LayerEdge le in layeredGraph.InEdges(u))
                return le;

            throw new InvalidOperationException();
        }
        //here u is a virtual vertex
        private LayerEdge OutcomingEdge(int u) {
            foreach (LayerEdge le in layeredGraph.OutEdges(u))
                return le;

            throw new InvalidOperationException();
        }
        bool IsVirtualVertex(int v) {
            return v >= this.originalGraph.Nodes.Count;
        }

        IEnumerable<int> RightFromTheNode(int[] layer, int vPosition, NodeKind nodeKind, double leftMostX, double rightMostX) {
            double t = 0, b = 0;
            if (nodeKind == NodeKind.Bottom)
                b = Single.MaxValue;//we don't have bottom boundaries here since they will be cut off
            else if (nodeKind == NodeKind.Top)
                t = Single.MaxValue;//we don't have top boundaries here since they will be cut off

            int v = layer[vPosition];

            for (int i = vPosition + 1; i < layer.Length; i++) {
                int u = layer[i];
                if (NodeUCanBeCrossedByNodeV(u, v))
                    continue;
                Anchor anchor = anchors[u];
                if (anchor.Left >= rightMostX)
                    break;
                if (anchor.Right > leftMostX) {
                    if (anchor.TopAnchor > t + ApproximateComparer.DistanceEpsilon) {
                        t = anchor.TopAnchor;
                        yield return u;
                    } else if (anchor.BottomAnchor > b + ApproximateComparer.DistanceEpsilon) {
                        b = anchor.BottomAnchor;
                        yield return u;
                    }
                }
            }
        }

        IEnumerable<int> LeftFromTheNode(int[] layer, int vPosition, NodeKind nodeKind, double leftMostX, double rightMostX) {
            double t = 0, b = 0;
            if (nodeKind == NodeKind.Bottom)
                b = Single.MaxValue;//we don't have bottom boundaries here since they will be cut off
            else if (nodeKind == NodeKind.Top)
                t = Single.MaxValue;//we don't have top boundaries here since they will be cut off

            int v = layer[vPosition];

            for (int i = vPosition - 1; i > -1; i--) {
                int u = layer[i];
                if (NodeUCanBeCrossedByNodeV(u, v))
                    continue;
                Anchor anchor = anchors[u];
                if (anchor.Right <= leftMostX)
                    break;
                if (anchor.Left < rightMostX) {
                    if (anchor.TopAnchor > t + ApproximateComparer.DistanceEpsilon) {
                        t = anchor.TopAnchor;
                        yield return u;
                    } else if (anchor.BottomAnchor > b + ApproximateComparer.DistanceEpsilon) {
                        b = anchor.BottomAnchor;
                        yield return u;
                    }
                }
            }
        }
    }
}
