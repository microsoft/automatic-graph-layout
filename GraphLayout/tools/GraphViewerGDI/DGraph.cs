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
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Routing;
using Color = System.Drawing.Color;
using DrawingGraph = Microsoft.Msagl.Drawing.Graph;
using GeometryEdge = Microsoft.Msagl.Core.Layout.Edge;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using P2 = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Shape = Microsoft.Msagl.Drawing.Shape;

namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// This yet another graph is needed to hold additional GDI specific data for drawing.
    /// It has a pointer to Microsoft.Msagl.Drawing.Graph and Microsoft.Msagl.Graph.
    /// It is passed to the drawing routine
    /// </summary>
    internal sealed class DGraph : DObject, IViewerGraph {
#if TEST_MSAGL

        internal static bool DrawControlPoints { get; set; }
#endif

        public override DrawingObject DrawingObject {
            get { return DrawingGraph; }
        }

        readonly List<DEdge> edges = new List<DEdge>();

        public List<DEdge> Edges {
            get { return edges; }
        }

        readonly Dictionary<IComparable, DNode> nodeMap = new Dictionary<IComparable, DNode>();

        internal BBNode BBNode {
            get {
                if (BbNode == null)
                    BuildBBHierarchy();
                return BbNode;
            }
        }

        internal void RemoveDNode(string id) {
            nodeMap.Remove(id);
        }

        internal DNode FindDNode(string id) {
            return nodeMap[id];
        }
        internal void AddNode(DNode dNode) {
            nodeMap[dNode.DrawingNode.Id] = dNode;
        }

        void AddEdge(DEdge dEdge) {
            edges.Add(dEdge);
        }

        internal void UpdateBBoxHierarchy(IEnumerable<IViewerObject> movedObjects) {
            var changedObjects = new Set<DObject>();
            foreach (DObject dObj in movedObjects)
                foreach (DObject relatedObj in RelatedObjs(dObj))
                    changedObjects.Insert(relatedObj);


            foreach (DObject dObj in changedObjects) {
                RebuildBBHierarchyUnderObject(dObj);
                InvalidateBBNodesAbove(dObj);
            }

            if (BBNode != null)
                UpdateBoxes(BBNode);
        }


        static void RebuildBBHierarchyUnderObject(DObject dObj) {
            BBNode oldNode = dObj.BbNode;
            BBNode newNode = BuildBBHierarchyUnderDObject(dObj);
            if (newNode != null) {
                //now copy all fields, except the parent
                oldNode.left = newNode.left;
                oldNode.right = newNode.right;
                oldNode.geometry = newNode.geometry;
                oldNode.bBox = newNode.bBox;
            }
            else
                oldNode.bBox = Rectangle.CreateAnEmptyBox();
        }

        static void InvalidateBBNodesAbove(DObject dObj) {
            for (BBNode node = dObj.BbNode; node != null; node = node.parent)
                node.bBox.Width = -1; //this will make the box empty
        }

        void UpdateBoxes(BBNode bbNode) {
            if (bbNode != null) {
                if (bbNode.Box.IsEmpty) {
                    if (bbNode.geometry != null)
                        bbNode.bBox = bbNode.geometry.Box;
                    else {
                        UpdateBoxes(bbNode.left);
                        UpdateBoxes(bbNode.right);
                        bbNode.bBox = bbNode.left.Box;
                        bbNode.bBox.Add(bbNode.right.Box);
                    }
                }
            }
        }

        IEnumerable<DObject> RelatedObjs(DObject dObj) {
            yield return dObj;
            var dNode = dObj as DNode;
            if (dNode != null) {
                foreach (DEdge e in dNode.OutEdges)
                    yield return e;
                foreach (DEdge e in dNode.InEdges)
                    yield return e;
                foreach (DEdge e in dNode.SelfEdges)
                    yield return e;
            }
            else {
                var dEdge = dObj as DEdge;
                if (dEdge != null) {
                    yield return dEdge.Source;
                    yield return dEdge.Target;
                    if (dEdge.Label != null)
                        yield return dEdge.Label;
                }
            }
        }

        internal void BuildBBHierarchy() {
            var objectsWithBox = new List<ObjectWithBox>();

            foreach (DObject dObject in Entities) {
                dObject.BbNode = BuildBBHierarchyUnderDObject(dObject);
                if (dObject.BbNode == null)
                    return; //the graph is not ready
                objectsWithBox.Add(dObject);
            }

            BbNode = SpatialAlgorithm.CreateBBNodeOnGeometries(objectsWithBox);
        }


        internal IEnumerable<IViewerObject> Entities {
            get {
                foreach (DEdge dEdge in Edges) {
                    yield return dEdge;
                    if (dEdge.Label != null)
                        yield return dEdge.Label;
                }

                foreach (DNode dNode in this.nodeMap.Values)
                    yield return dNode;
            }
        }

        static BBNode BuildBBHierarchyUnderDObject(DObject dObject) {
            var dNode = dObject as DNode;
            if (dNode != null)
                return BuildBBHierarchyUnderDNode(dNode);
            var dedge = dObject as DEdge;
            if (dedge != null) {
                return BuildBBHierarchyUnderDEdge(dedge);
            }
            var dLabel = dObject as DLabel;
            if (dLabel != null)
                if (DLabelIsValid(dLabel))
                    return BuildBBHierarchyUnderDLabel(dLabel);
                else return null;

            var dGraph = dObject as DGraph;
            if (dGraph != null) {
                dGraph.BbNode.bBox = dGraph.DrawingGraph.BoundingBox;
                return dGraph.BbNode;
            }

            throw new InvalidOperationException();
        }

        internal static bool DLabelIsValid(DLabel dLabel) {
            var drawingObj = dLabel.DrawingObject;
            var edge = drawingObj.GeometryObject.GeometryParent as GeometryEdge;
            return edge == null || edge.Label != null;
        }

        static BBNode BuildBBHierarchyUnderDLabel(DLabel dLabel) {
            var bbNode = new BBNode();
            bbNode.geometry = new Geometry(dLabel);
            bbNode.bBox = dLabel.DrawingLabel.BoundingBox;
            return bbNode;
        }

        static BBNode BuildBBHierarchyUnderDNode(DNode dNode) {
            var bbNode = new BBNode();
            bbNode.geometry = new Geometry(dNode);
            bbNode.bBox = dNode.DrawingNode.BoundingBox;
            return bbNode;
        }

        static BBNode BuildBBHierarchyUnderDEdge(DEdge dEdge) {
            if (dEdge.DrawingEdge.GeometryObject == null ||
                dEdge.Edge.GeometryEdge.Curve == null)
                return null;
            List<ObjectWithBox> geometries = Tessellator.TessellateCurve(dEdge,
                                                                         dEdge.MarkedForDragging
                                                                             ? dEdge.RadiusOfPolylineCorner
                                                                             : 0);
            return SpatialAlgorithm.CreateBBNodeOnGeometries(geometries);
        }

        internal override float DashSize() {
            return 0; //not implemented
        }

        protected internal override void Invalidate() { }
        /// <summary>
        /// calculates the rendered rectangle and RenderedBox to it
        /// </summary>
        public override void UpdateRenderedBox() {
            RenderedBox = DrawingGraph.BoundingBox;
        }

        //     internal Dictionary<DrawingObject, DObject> drawingObjectsToDObjects = new Dictionary<DrawingObject, DObject>();


        Graph drawingGraph;

        public Graph DrawingGraph {
            get { return drawingGraph; }
            set { drawingGraph = value; }
        }


        internal void DrawGraph(Graphics g) {
            #region drawing of database for debugging only

#if TEST_MSAGL
            Graph dg = DrawingGraph;

            if (dg.DataBase != null) {
                var myPen = new Pen(Color.Blue, (float)(1 / 1000.0));
                Draw.DrawDataBase(g, myPen, dg);
            }

            if (NeedToDrawDebugStuff()) {
                var myPen = new Pen(Color.Blue, (float)(1 / 1000.0));
                Draw.DrawDebugStuff(g, this, myPen);
            }

#endif

            #endregion

            if (drawingGraph.Attr.Border > 0)
                DrawGraphBorder(drawingGraph.Attr.Border, g);

            //we need to draw the edited edges last
            DEdge dEdgeSelectedForEditing = null;

            foreach (var subgraph in drawingGraph.RootSubgraph.AllSubgraphsWidthFirstExcludingSelf())
                DrawNode(g, nodeMap[subgraph.Id]);


            foreach (DEdge dEdge in Edges)
                if (!dEdge.SelectedForEditing)
                    DrawEdge(g, dEdge);
                else //there must be no more than one edge selected for editing
                    dEdgeSelectedForEditing = dEdge;


            foreach (DNode dnode in nodeMap.Values)
                if (!(dnode.DrawingNode is Subgraph))
                    DrawNode(g, dnode);

            //draw the selected edge
            if (dEdgeSelectedForEditing != null) {
                DrawEdge(g, dEdgeSelectedForEditing);
                DrawUnderlyingPolyline(g, dEdgeSelectedForEditing);
            }

            if (Viewer.SourcePortIsPresent)
                DrawPortAtLocation(g, Viewer.SourcePortLocation);
            if (Viewer.TargetPortIsPresent)
                DrawPortAtLocation(g, Viewer.TargetPortLocation);
        }

#if TEST_MSAGL
        bool NeedToDrawDebugStuff() {
            return drawingGraph.DebugCurves != null ||
                   drawingGraph.DebugICurves != null && drawingGraph.DebugICurves.Count > 0 ||
                   drawingGraph.DataBase != null ||
                   drawingGraph.GeometryGraph != null;
        }
#endif

        static void DrawUnderlyingPolyline(Graphics g, DEdge editedEdge) {
            SmoothedPolyline underlyingPolyline = editedEdge.DrawingEdge.GeometryEdge.UnderlyingPolyline;
            if (underlyingPolyline != null) {
                var pen = new Pen(editedEdge.Color, (float)editedEdge.DrawingEdge.Attr.LineWidth);
                IEnumerator<P2> en = underlyingPolyline.GetEnumerator();
                en.MoveNext();
                PointF p = P2P(en.Current);
                while (en.MoveNext())
                    g.DrawLine(pen, p, p = P2P(en.Current));

                foreach (P2 p2 in underlyingPolyline)
                    DrawCircleAroungPolylineCorner(g, p2, pen, editedEdge.RadiusOfPolylineCorner);
            }
        }

        static void DrawCircleAroungPolylineCorner(Graphics g, P2 p, Pen pen, double radius) {
            g.DrawEllipse(pen, (float)(p.X - radius), (float)(p.Y - radius),
                          (float)(2 * radius), (float)(2 * radius));
        }


        static PointF P2P(P2 p) {
            return new PointF((float)p.X, (float)p.Y);
        }

        void DrawGraphBorder(int borderWidth, Graphics graphics) {
            using (var myPen = new Pen(Draw.MsaglColorToDrawingColor(drawingGraph.Attr.Color), (float)borderWidth))
                graphics.DrawRectangle(myPen,
                                       (float)drawingGraph.Left,
                                       (float)drawingGraph.Bottom,
                                       (float)drawingGraph.Width,
                                       (float)drawingGraph.Height);
        }

        //don't know what to do about the try-catch block
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static void DrawEdge(Graphics graphics, DEdge dEdge) {
            DrawingEdge drawingEdge = dEdge.DrawingEdge;
            if (!drawingEdge.IsVisible || drawingEdge.GeometryEdge == null)
                return;


            DrawingEdge edge = dEdge.DrawingEdge;
            if (edge.DrawEdgeDelegate != null)
                if (edge.DrawEdgeDelegate(edge, graphics))
                    return; //the client draws instead

            if (dEdge.GraphicsPath == null)
                dEdge.GraphicsPath = Draw.CreateGraphicsPath(dEdge.Edge.GeometryEdge.Curve);

            EdgeAttr attr = drawingEdge.Attr;

            using (var myPen = new Pen(dEdge.Color, (float)attr.LineWidth)) {
                foreach (Style style in attr.Styles) {
                    Draw.AddStyleForPen(dEdge, myPen, style);
                }
                try {
                    if (dEdge.GraphicsPath != null)
                        graphics.DrawPath(myPen, dEdge.GraphicsPath);
                }
                catch {
                    //  sometimes on Vista it throws an out of memory exception without any obvious reason
                }
                Draw.DrawEdgeArrows(graphics, drawingEdge, dEdge.Color, myPen);
                if (dEdge.DrawingEdge.GeometryEdge.Label != null)
                    Draw.DrawLabel(graphics, dEdge.Label);

#if TEST_MSAGL
                if (DrawControlPoints) {
                    ICurve iCurve = dEdge.DrawingEdge.GeometryEdge.Curve;
                    var c = iCurve as Curve;

                    if (c != null) {
                        foreach (ICurve seg in c.Segments) {
                            var cubic = seg as CubicBezierSegment;
                            if (cubic != null)
                                Draw.DrawControlPoints(graphics, cubic);
                        }
                    }
                    else {
                        var seg = iCurve as CubicBezierSegment;
                        if (seg != null)
                            Draw.DrawControlPoints(graphics, seg);
                    }
                }

#endif
            }
        }

        internal void DrawNode(Graphics g, DNode dnode) {

            DrawingNode node = dnode.DrawingNode;
            if (node.IsVisible == false)
                return;

            if (node.DrawNodeDelegate != null)
                if (node.DrawNodeDelegate(node, g))
                    return; //the client draws instead

            if (node.GeometryNode == null || node.GeometryNode.BoundaryCurve == null) //node comes with non-initilalized attribute - should not be drawn
                return;
            NodeAttr attr = node.Attr;

            using (var pen = new Pen(dnode.Color, (float)attr.LineWidth)) {
                foreach (Style style in attr.Styles)
                    Draw.AddStyleForPen(dnode, pen, style);
                switch (attr.Shape) {
                    case Shape.DoubleCircle:
                        Draw.DrawDoubleCircle(g, pen, dnode);
                        break;
                    case Shape.Box:
                        Draw.DrawBox(g, pen, dnode);
                        break;
                    case Shape.Diamond:
                        Draw.DrawDiamond(g, pen, dnode);
                        break;
                    case Shape.Point:
                        Draw.DrawEllipse(g, pen, dnode);
                        break;
                    case Shape.Plaintext: {
                            break;
                            //do nothing
                        }
                    case Shape.Octagon:
                    case Shape.House:
                    case Shape.InvHouse:
                    case Shape.Ellipse:
                    case Shape.DrawFromGeometry:

#if TEST_MSAGL
                    case Shape.TestShape:
#endif
                        pen.EndCap = LineCap.Square;
                        Draw.DrawFromMsaglCurve(g, pen, dnode);
                        break;

                    default:
                        Draw.DrawEllipse(g, pen, dnode);
                        break;
                }
            }
            Draw.DrawLabel(g, dnode.Label);
        }


        void DrawPortAtLocation(Graphics g, P2 center) {
            Brush brush = Brushes.Brown;
            double rad = Viewer.UnderlyingPolylineCircleRadius;
            g.FillEllipse(brush, (float)center.X - (float)rad,
                          (float)center.Y - (float)rad,
                          2.0f * (float)rad,
                          2.0f * (float)rad);
        }

        internal DGraph(Graph drawingGraph, GViewer gviewer) : base(gviewer) {
            DrawingGraph = drawingGraph;
        }

        /// <summary>
        /// creates DGraph from a precalculated drawing graph
        /// </summary>
        /// <param name="drawingGraph"></param>
        /// <param name="viewer">the owning viewer</param>
        /// <returns></returns>
        internal static DGraph CreateDGraphFromPrecalculatedDrawingGraph(Graph drawingGraph, GViewer viewer) {
            var dGraph = new DGraph(drawingGraph, viewer);
            //create dnodes and node boundary curves

            if (drawingGraph.RootSubgraph != null)
                foreach (DrawingNode drawingNode in drawingGraph.RootSubgraph.AllSubgraphsWidthFirstExcludingSelf()) {
                    var dNode = new DNode(drawingNode, viewer);
                    if (drawingNode.Label != null)
                        dNode.Label = new DLabel(dNode, drawingNode.Label, viewer);
                    dGraph.AddNode(dNode);
                }

            foreach (DrawingNode drawingNode in drawingGraph.Nodes) {
                var dNode = new DNode(drawingNode, viewer);
                if (drawingNode.Label != null)
                    dNode.Label = new DLabel(dNode, drawingNode.Label, viewer);
                dGraph.AddNode(dNode);
            }


            foreach (DrawingEdge drawingEdge in drawingGraph.Edges)
                dGraph.AddEdge(new DEdge(dGraph.GetNode(drawingEdge.SourceNode), dGraph.GetNode(drawingEdge.TargetNode),
                                      drawingEdge, ConnectionToGraph.Connected, viewer));

            return dGraph;
        }

        internal static void CreateDLabel(DObject parent, Drawing.Label label, out double width, out double height,
                                          GViewer viewer) {
            var dLabel = new DLabel(parent, label, viewer) { Font = new Font(label.FontName, (int)label.FontSize, (System.Drawing.FontStyle)(int)label.FontStyle) };
            StringMeasure.MeasureWithFont(label.Text, dLabel.Font, out width, out height);

            if (width <= 0)
                //this is a temporary fix for win7 where Measure fonts return negative lenght for the string " "
                StringMeasure.MeasureWithFont("a", dLabel.Font, out width, out height);

            label.Width = width;
            label.Height = height;
        }

        internal static DGraph CreateDGraphAndGeometryInfo(Graph drawingGraph, GeometryGraph geometryGraph,
                                                           GViewer viewer) {
            var dGraph = new DGraph(drawingGraph, viewer);
            //create dnodes and glee node boundary curves
            var nodeMapping = new Dictionary<GeometryNode, DNode>();
            if (geometryGraph.RootCluster != null)
                foreach (var geomCluster in geometryGraph.RootCluster.AllClustersDepthFirstExcludingSelf()) {
                    var drawingNode = (Drawing.Node)geomCluster.UserData;
                    DNode dNode = CreateDNodeAndSetNodeBoundaryCurveForSubgraph(drawingGraph, dGraph, geomCluster,
                                                                                drawingNode, viewer);
                    nodeMapping[geomCluster] = dNode;
                }

            foreach (GeometryNode geomNode in geometryGraph.Nodes) {
                var drawingNode = (Drawing.Node)geomNode.UserData;
                DNode dNode = CreateDNodeAndSetNodeBoundaryCurve(drawingGraph, dGraph, geomNode, drawingNode, viewer);
                nodeMapping[geomNode] = dNode;
            }

            foreach (GeometryEdge gleeEdge in geometryGraph.Edges) {
                var dEdge = new DEdge(nodeMapping[gleeEdge.Source], nodeMapping[gleeEdge.Target],
                                      gleeEdge.UserData as DrawingEdge, ConnectionToGraph.Connected, viewer);
                dGraph.AddEdge(dEdge);
                DrawingEdge drawingEdge = dEdge.Edge;
                Drawing.Label label = drawingEdge.Label;

                if (label != null) {
                    double width, height;
                    CreateDLabel(dEdge, label, out width, out height, viewer);
                }
            }

            return dGraph;
        }

        internal static DNode CreateDNodeAndSetNodeBoundaryCurveForSubgraph(Graph drawingGraph, DGraph dGraph, GeometryNode geomNode,
                                                                 DrawingNode drawingNode, GViewer viewer) {
            double width = 0;
            double height = 0;
            var dNode = new DNode(drawingNode, viewer);
            dGraph.AddNode(dNode);
            Drawing.Label label = drawingNode.Label;
            if (label != null) {
                CreateDLabel(dNode, label, out width, out height, viewer);
            }
            if (width < drawingGraph.Attr.MinNodeWidth)
                width = drawingGraph.Attr.MinNodeWidth;
            if (height < drawingGraph.Attr.MinNodeHeight)
                height = drawingGraph.Attr.MinNodeHeight;

            var cluster = (Cluster)geomNode;
            var margin = dNode.DrawingNode.Attr.LabelMargin;
            if (label != null) {
                CreateDLabel(dNode, label, out width, out height, viewer);
                width += 2 * dNode.DrawingNode.Attr.LabelMargin + 2 * drawingNode.Attr.LineWidth;
                height += 2 * dNode.DrawingNode.Attr.LabelMargin + 2 * drawingNode.Attr.LineWidth;
            }
            if (cluster.RectangularBoundary == null) {
                var lp = dNode.DrawingNode.Attr.ClusterLabelMargin;
                cluster.RectangularBoundary = new RectangularClusterBoundary() {
                    BottomMargin = lp == LgNodeInfo.LabelPlacement.Bottom ? height : margin,
                    LeftMargin = lp == LgNodeInfo.LabelPlacement.Left ? height : margin,
                    RightMargin = lp == LgNodeInfo.LabelPlacement.Right ? height : margin,
                    TopMargin = lp == LgNodeInfo.LabelPlacement.Top ? height : margin,
                    MinWidth = lp == LgNodeInfo.LabelPlacement.Top || lp == LgNodeInfo.LabelPlacement.Bottom ? width : 0,
                    MinHeight = lp == LgNodeInfo.LabelPlacement.Left || lp == LgNodeInfo.LabelPlacement.Right ? width : 0,
                };
            }
            // Filippo Polo: I'm taking this out because I've modified the drawing of a double circle
            // so that it can be used with ellipses too.
            //if (drawingNode.Attr.Shape == Shape.DoubleCircle)
            //width = height = Math.Max(width, height) * Draw.DoubleCircleOffsetRatio;
            ICurve curve;
            if (drawingNode.NodeBoundaryDelegate != null &&
                (curve = drawingNode.NodeBoundaryDelegate(drawingNode)) != null)
                geomNode.BoundaryCurve = curve;
            else if (geomNode.BoundaryCurve == null)
                geomNode.BoundaryCurve =
                    NodeBoundaryCurves.GetNodeBoundaryCurve(dNode.DrawingNode, width, height);
            return dNode;
        }


        internal static DNode CreateDNodeAndSetNodeBoundaryCurve(Graph drawingGraph, DGraph dGraph, GeometryNode geomNode,
                                                                 DrawingNode drawingNode, GViewer viewer) {
            double width = 0;
            double height = 0;
            var dNode = new DNode(drawingNode, viewer);
            dGraph.AddNode(dNode);
            Drawing.Label label = drawingNode.Label;
            if (label != null) {
                CreateDLabel(dNode, label, out width, out height, viewer);
                width += 2 * dNode.DrawingNode.Attr.LabelMargin;
                height += 2 * dNode.DrawingNode.Attr.LabelMargin;
            }
            if (width < drawingGraph.Attr.MinNodeWidth)
                width = drawingGraph.Attr.MinNodeWidth;
            if (height < drawingGraph.Attr.MinNodeHeight)
                height = drawingGraph.Attr.MinNodeHeight;

            // Filippo Polo: I'm taking this out because I've modified the drawing of a double circle
            // so that it can be used with ellipses too.
            //if (drawingNode.Attr.Shape == Shape.DoubleCircle)
            //width = height = Math.Max(width, height) * Draw.DoubleCircleOffsetRatio;
            ICurve curve;
            if (drawingNode.NodeBoundaryDelegate != null &&
                (curve = drawingNode.NodeBoundaryDelegate(drawingNode)) != null)
                geomNode.BoundaryCurve = curve;
            else if (geomNode.BoundaryCurve == null)
                geomNode.BoundaryCurve =
                    NodeBoundaryCurves.GetNodeBoundaryCurve(dNode.DrawingNode, width, height);
            return dNode;
        }

        DNode GetNode(DrawingNode node) {
            return nodeMap[node.Id];
        }

        /// <summary>
        /// yields the nodes
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IViewerNode> Nodes() {
            foreach (DNode dNode in nodeMap.Values)
                yield return dNode;
        }

        IEnumerable<IViewerEdge> IViewerGraph.Edges() {
            foreach (DEdge dEdge in Edges)
                yield return dEdge;
        }
    }
}