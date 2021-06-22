using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Miscellaneous.LayoutEditing;
using Microsoft.Msagl.Prototype.LayoutEditing;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using GeomNode = Microsoft.Msagl.Core.Layout.Node;
using GeomEdge = Microsoft.Msagl.Core.Layout.Edge;
using GeomLabel = Microsoft.Msagl.Core.Layout.Label;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    ///     the editor of a graph layout
    /// </summary>
    public class GeometryGraphEditor {
        readonly Set<GeomEdge> edgesDraggedWithSource = new Set<GeomEdge>();
        readonly Set<GeomEdge> edgesDraggedWithTarget = new Set<GeomEdge>();

        GeometryGraph graph;
        LayoutAlgorithmSettings layoutSettings;
        Set<GeometryObject> objectsToDrag = new Set<GeometryObject>();
        UndoRedoActionsList undoRedoActionsList = new UndoRedoActionsList();
        bool undoMode = true;
        IncrementalDragger incrementalDragger;

        internal UndoRedoActionsList UndoRedoActionsList {
            get { return undoRedoActionsList; }
            set { undoRedoActionsList = value; }
        }

        /// <summary>
        ///     return the current undo action
        /// </summary>
        public UndoRedoAction CurrentUndoAction {
            get { return UndoRedoActionsList.CurrentUndo; }
        }

        /// <summary>
        ///     return the current redo action
        /// </summary>
        public UndoRedoAction CurrentRedoAction {
            get { return UndoRedoActionsList.CurrentRedo; }
        }

        /// <summary>
        ///     Will be set to true if an entity was dragged out of the graph bounding box
        /// </summary>
        public bool GraphBoundingBoxGetsExtended { get; internal set; }

        /// <summary>
        ///     Current graph under editing
        /// </summary>
        public GeometryGraph Graph {
            get { return graph; }
            set {
                graph = value;
                Clear();
                RaiseChangeInUndoList();
            }
        }


        /// <summary>
        /// </summary>
        public LayoutAlgorithmSettings LayoutSettings {
            get { return layoutSettings; }
            set {
                layoutSettings = value;
                LgLayoutSettings = layoutSettings as LgLayoutSettings;
            }
        }

        internal LgLayoutSettings LgLayoutSettings { get; set; }

        /// <summary>
        /// </summary>
        protected EdgeRoutingMode EdgeRoutingMode {
            get { return LayoutSettings.EdgeRoutingSettings.EdgeRoutingMode; }
        }


        /// <summary>
        ///     The edge data of the edge selected for editing
        /// </summary>
        internal GeomEdge EditedEdge { get; set; }

        /// <summary>
        ///     enumerates over the nodes chosen for dragging
        /// </summary>
        public IEnumerable<GeometryObject> ObjectsToDrag {
            get { return objectsToDrag; }
        }

        /// <summary>
        ///     returns true if "undo" is available
        /// </summary>
        public bool CanUndo {
            get { return UndoRedoActionsList.CurrentUndo != null; }
        }

        /// <summary>
        ///     returns true if "redo" is available
        /// </summary>
        public bool CanRedo {
            get { return UndoRedoActionsList.CurrentRedo != null; }
        }


        /// <summary>
        /// indicates if the editor is under the undo mode
        /// </summary>
        internal bool UndoMode {
            get { return undoMode; }
            set { undoMode = value; }
        }


        /// <summary>
        ///     signals that there is a change in the undo/redo list
        ///     There are four possibilities: Undo(Redo) becomes available (unavailable)
        /// </summary>
        public event EventHandler ChangeInUndoRedoList;


        internal static void DragLabel(GeomLabel label, Point delta) {
            label.Center += delta;
            var edge = label.GeometryParent as GeomEdge;
            if (edge != null) {
                CalculateAttachedSegmentEnd(label, edge);
                if (!ApproximateComparer.Close(label.AttachmentSegmentEnd, label.Center)) {
                    IntersectionInfo x = Curve.CurveCurveIntersectionOne(label.BoundingBox.Perimeter(),
                        new LineSegment(
                            label.AttachmentSegmentEnd,
                            label.Center), false);

                    label.AttachmentSegmentStart = x != null ? x.IntersectionPoint : label.Center;
                }
                else
                    label.AttachmentSegmentStart = label.Center;
            }
        }


        static void CalculateAttachedSegmentEnd(GeomLabel label, GeomEdge edge) {
            label.AttachmentSegmentEnd = edge.Curve[edge.Curve.ClosestParameter(label.Center)];
        }

        /// <summary>
        ///     drags elements by the delta
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="draggingMode">describes the way we process the dragging </param>
        /// <param name="lastMousePosition">the last position of the mouse pointer </param>
        internal void Drag(Point delta, DraggingMode draggingMode, Point lastMousePosition) {
            GraphBoundingBoxGetsExtended = false;
            if (delta.X != 0 || delta.Y != 0) {
                if (EditedEdge == null)
                    if (EdgeRoutingMode != EdgeRoutingMode.Rectilinear &&
                        EdgeRoutingMode != EdgeRoutingMode.RectilinearToCenter)
                        DragObjectsForNonRectilinearCase(delta, draggingMode);
                    else
                        DragObjectsForRectilinearCase(delta);
                else {
                    DragEdgeEdit(lastMousePosition, delta);
                    UpdateGraphBoundingBoxWithCheck(EditedEdge);
                }
            }
        }

        void DragObjectsForRectilinearCase(Point delta) {
            foreach (GeomNode node in objectsToDrag.Where(n => n is GeomNode))
                node.Center += delta;

            RectilinearInteractiveEditor.CreatePortsAndRouteEdges(LayoutSettings.NodeSeparation/3, 1, graph.Nodes,
                graph.Edges,
                LayoutSettings.EdgeRoutingSettings.EdgeRoutingMode,
                true,
                LayoutSettings.EdgeRoutingSettings
                    .UseObstacleRectangles,
                LayoutSettings.EdgeRoutingSettings.BendPenalty);
            var labelPlacer = new EdgeLabelPlacement(graph);
            labelPlacer.Run();

            foreach (GeomEdge e in Graph.Edges)
                UpdateGraphBoundingBoxWithCheck(e);
            foreach (GeomNode n in Graph.Nodes)
                UpdateGraphBoundingBoxWithCheck(n);
        }

        void DragObjectsForNonRectilinearCase(Point delta, DraggingMode draggingMode) {
            if (draggingMode == DraggingMode.Incremental)
                DragIncrementally(delta);
            else if (EdgeRoutingMode == EdgeRoutingMode.Spline || EdgeRoutingMode == EdgeRoutingMode.SplineBundling)
                DragWithSplinesOrBundles(delta);
            else
                DragWithStraightLines(delta);
        }

        void DragWithStraightLines(Point delta) {
            foreach (var geomObj in objectsToDrag) {
                var node = geomObj as GeomNode;
                if (node != null) {
                    node.Center += delta;
                    var cl = node as Cluster;
                    if (cl != null) {
                        cl.DeepContentsTranslation(delta, translateEdges: false);
                        cl.RectangularBoundary.TranslateRectangle(delta);
                    }
                }
                else
                    ShiftDragEdge(delta, geomObj);
                UpdateGraphBoundingBoxWithCheck(geomObj);
            }

            PropagateChangesToClusterParents();
            DragEdgesAsStraighLines(delta);
        }

        void PropagateChangesToClusterParents() {
            var touchedClusters = new Set<Cluster>();
            foreach (var n in objectsToDrag) {
                var node = n as GeomNode;
                if (node == null) continue;
                foreach (var c in node.AllClusterAncestors)
                    if (c != graph.RootCluster && !objectsToDrag.Contains(c))
                        touchedClusters.Insert(c);
            }
            if (touchedClusters.Any())
                foreach (var c in graph.RootCluster.AllClustersDepthFirstExcludingSelf())
                    if (touchedClusters.Contains(c))
                        c.CalculateBoundsFromChildren(layoutSettings.ClusterMargin);
        }

        static void ShiftDragEdge(Point delta, GeometryObject geomObj) {
            var edge = geomObj as GeomEdge;
            if (edge != null)
                edge.Translate(delta);
            else {
                var label = geomObj as GeomLabel;
                if (label != null)
                    DragLabel(label, delta);
                else
                    throw new NotImplementedException();
            }
        }

        void DragWithSplinesOrBundles(Point delta) {
            foreach (GeometryObject geomObj in objectsToDrag) {
                var node = geomObj as GeomNode;
                if (node != null)
                    node.Center += delta;
            }
            RunSplineRouterAndPutLabels();
        }


        void RunSplineRouterAndPutLabels() {
            var router = new SplineRouter(graph, LayoutSettings.EdgeRoutingSettings.Padding,
                LayoutSettings.EdgeRoutingSettings.PolylinePadding,
                LayoutSettings.EdgeRoutingSettings.ConeAngle,
                LayoutSettings.EdgeRoutingSettings.BundlingSettings);
            router.Run();
            var elp = new EdgeLabelPlacement(graph);
            elp.Run();
            UpdateGraphBoundingBoxWithCheck();
        }

        void DragEdgesAsStraighLines(Point delta) {
            foreach (GeomEdge edge in edgesDraggedWithSource)
                DragEdgeAsStraightLine(delta, edge);
            foreach (GeomEdge edge in edgesDraggedWithTarget)
                DragEdgeAsStraightLine(delta, edge);
            var ep = new EdgeLabelPlacement(graph.Nodes, edgesDraggedWithSource.Union(edgesDraggedWithTarget));
            ep.Run();
        }

        static void DragEdgeAsStraightLine(Point delta, GeomEdge edge) {
            StraightLineEdges.CreateSimpleEdgeCurveWithUnderlyingPolyline(edge);
        }

        void UpdateGraphBoundingBoxWithCheck() {
            foreach (GeomNode node in graph.Nodes)
                UpdateGraphBoundingBoxWithCheck(node);
            foreach (GeomEdge edge in graph.Edges)
                UpdateGraphBoundingBoxWithCheck(edge);
        }

        void DragIncrementally(Point delta) {
            Rectangle box = graph.BoundingBox;
            if (incrementalDragger == null)
                InitIncrementalDragger();
            incrementalDragger.Drag(delta);

            GraphBoundingBoxGetsExtended = box != graph.BoundingBox;
        }

        void DragEdgeEdit(Point lastMousePosition, Point delta) {
            EditedEdge.RaiseLayoutChangeEvent(delta);
            Site site = FindClosestCornerForEdit(EditedEdge.UnderlyingPolyline, lastMousePosition);
            site.Point += delta;
            CreateCurveOnChangedPolyline(EditedEdge);
        }

        /// <summary>
        /// </summary>
        /// <param name="delta">delta of the drag</param>
        /// <param name="e">the modified edge</param>
        /// <param name="site"></param>
        internal static void DragEdgeWithSite(Point delta, GeomEdge e, Site site) {
            e.RaiseLayoutChangeEvent(delta);
            site.Point += delta;
            CreateCurveOnChangedPolyline(e);
        }

        static void CreateCurveOnChangedPolyline(GeomEdge e) {
            Curve curve = e.UnderlyingPolyline.CreateCurve();
            if (
                !Arrowheads.TrimSplineAndCalculateArrowheads(e.EdgeGeometry, e.Source.BoundaryCurve,
                    e.Target.BoundaryCurve, curve, false))
                Arrowheads.CreateBigEnoughSpline(e);
        }

        /// <summary>
        ///     prepares for node dragging
        /// </summary>
        /// <param name="markedObjects">markedObjects will be dragged</param>
        /// <param name="dragMode"> is shift is pressed then the mode changes </param>
        /// <returns></returns>
        internal void PrepareForObjectDragging(IEnumerable<GeometryObject> markedObjects, DraggingMode dragMode) {
            EditedEdge = null;
            CalculateDragSets(markedObjects);
            InsertToListAndSetTheBoxBefore(new ObjectDragUndoRedoAction(graph));
            if (dragMode == DraggingMode.Incremental)
                InitIncrementalDragger();
        }

        void InitIncrementalDragger() {
            incrementalDragger = new IncrementalDragger(objectsToDrag.OfType<GeomNode>().ToArray(), graph,
                layoutSettings);
        }

        void ClearDraggedSets() {
            objectsToDrag.Clear();
            edgesDraggedWithSource.Clear();
            edgesDraggedWithTarget.Clear();
        }

        void CalculateDragSets(IEnumerable<GeometryObject> markedObjects) {
            ClearDraggedSets();

            foreach (GeometryObject geometryObject in markedObjects) {
                objectsToDrag.Insert(geometryObject);
                var edge = geometryObject as GeomEdge;
                if (edge != null) {
                    objectsToDrag.Insert(edge.Source);
                    objectsToDrag.Insert(edge.Target);
                }
            }
            RemoveClusterSuccessorsFromObjectsToDrag();
            CalculateDragSetsForEdges();
        }

        void RemoveClusterSuccessorsFromObjectsToDrag() {
            var listToRemove = new List<GeomNode>();
            foreach (var node in objectsToDrag.OfType<GeomNode>())
                if (node.AllClusterAncestors.Any(anc => objectsToDrag.Contains(anc)))
                    listToRemove.Add(node);
            foreach (var node in listToRemove)
                objectsToDrag.Remove(node);
        }

        void UpdateGraphBoundingBoxWithCheck(GeometryObject geomObj) {
            var cl = geomObj as Cluster;
            Rectangle bBox = cl != null ? cl.BoundaryCurve.BoundingBox : geomObj.BoundingBox;
            {
                var edge = geomObj as GeomEdge;
                if (edge != null && edge.Label != null)
                    bBox.Add(edge.Label.BoundingBox);
            }
            var p = new Point(-Graph.Margins, Graph.Margins);

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=369 there are no structs in js
            Rectangle bounds = Graph.BoundingBox.Clone();
#else
            Rectangle bounds = Graph.BoundingBox;
#endif
            GraphBoundingBoxGetsExtended |= bounds.AddWithCheck(bBox.LeftTop + p);
            GraphBoundingBoxGetsExtended |= bounds.AddWithCheck(bBox.RightBottom - p);
            Graph.BoundingBox = bounds;
        }


        void CalculateDragSetsForEdges() {
            foreach (GeometryObject geomObj in objectsToDrag.Clone()) {
                var node = geomObj as GeomNode;
                if (node != null)
                    AssignEdgesOfNodeToEdgeDragSets(node);
            }
        }

        void AssignEdgesOfNodeToEdgeDragSets(GeomNode node) {
            foreach (GeomEdge edge in node.SelfEdges)
                objectsToDrag.Insert(edge);
            foreach (GeomEdge edge in node.InEdges)
                if (objectsToDrag.Contains(edge.Source) ||
                    edge.Source.ClusterParent != null && objectsToDrag.Contains(edge.Source.ClusterParent))
                    objectsToDrag.Insert(edge);
                else
                    edgesDraggedWithTarget.Insert(edge);

            foreach (GeomEdge edge in node.OutEdges)
                if (objectsToDrag.Contains(edge.Target) ||
                    edge.Target.ClusterParent!=null && objectsToDrag.Contains(edge.Target.ClusterParent))
                    objectsToDrag.Insert(edge);
                else
                    edgesDraggedWithSource.Insert(edge);

            CalculateOffsetsForMultiedges(node, LayoutSettings.NodeSeparation);
            var cl = node as Cluster;
            if (cl != null)
                foreach (var n in cl.AllSuccessorsWidthFirst())
                    AssignEdgesOfNodeToEdgeDragSets(n);
        }

        internal static Dictionary<GeomEdge, double> CalculateOffsetsForMultiedges(GeomNode node, double nodeSeparation) {
            var offsetsInsideOfMultiedge = new Dictionary<GeomEdge, double>();
            foreach (var multiedge in GetMultiEdges(node))
                CalculateMiddleOffsetsForMultiedge(multiedge, node, offsetsInsideOfMultiedge, nodeSeparation);
            return offsetsInsideOfMultiedge;
        }

        static void CalculateMiddleOffsetsForMultiedge(List<GeomEdge> multiedge, GeomNode node,
            Dictionary<GeomEdge, double> offsetsInsideOfMultiedge,
            double nodeSeparation) {
            Dictionary<GeomEdge, double> middleAngles = GetMiddleAnglesOfMultiedge(multiedge, node);
            var angles = new double[middleAngles.Count];
            var edges = new GeomEdge[middleAngles.Count];
            int i = 0;
            foreach (var v in middleAngles) {
                angles[i] = v.Value;
                edges[i] = v.Key;
                i++;
            }
            Array.Sort(angles, edges);

            double separation = nodeSeparation*6;

            int k = edges.Length/2;
            bool even = k*2 == edges.Length;
            double off;
            if (even) {
                off = -separation/2;
                for (int j = k - 1; j >= 0; j--) {
                    GeomEdge edge = edges[j];
                    offsetsInsideOfMultiedge[edge] = off;
                    off -= separation + (edge.Label != null ? edge.Label.Width : 0);
                }

                off = separation/2;
                for (int j = k; j < edges.Length; j++) {
                    GeomEdge edge = edges[j];
                    offsetsInsideOfMultiedge[edge] = off;
                    off += separation + (edge.Label != null ? edge.Label.Width : 0);
                }
            }
            else {
                off = 0;
                for (int j = k; j >= 0; j--) {
                    GeomEdge edge = edges[j];
                    offsetsInsideOfMultiedge[edge] = off;
                    off -= separation + (edge.Label != null ? edge.Label.Width : 0);
                }
                off = separation;
                for (int j = k + 1; j < edges.Length; j++) {
                    GeomEdge edge = edges[j];
                    offsetsInsideOfMultiedge[edge] = off;
                    off += separation + (edge.Label != null ? edge.Label.Width : 0);
                }
            }
        }

        static Dictionary<GeomEdge, double> GetMiddleAnglesOfMultiedge(List<GeomEdge> multiedge, GeomNode node) {
            var ret = new Dictionary<GeomEdge, double>();
            GeomEdge firstEdge = multiedge[0];

            Point a = node.Center;
            Point b = Middle(firstEdge.Curve);
            ret[firstEdge] = 0;

            for (int i = 1; i < multiedge.Count; i++) {
                GeomEdge edge = multiedge[i];
                Point c = Middle(edge.Curve);
                double angle = Point.Angle(b, a, c);
                if (angle > Math.PI)
                    angle = angle - Math.PI*2;

                ret[edge] = angle;
            }

            return ret;
        }

        static Point Middle(ICurve iCurve) {
            return iCurve[iCurve.ParStart + 0.5*(iCurve.ParEnd - iCurve.ParStart)];
        }

        static IEnumerable<List<GeomEdge>> GetMultiEdges(GeomNode node) {
            var nodeToMultiEdge = new Dictionary<GeomNode, List<GeomEdge>>();
            foreach (GeomEdge edge in node.OutEdges)
                GetOrCreateListOfMultiedge(nodeToMultiEdge, edge.Target).Add(edge);
            foreach (GeomEdge edge in node.InEdges)
                GetOrCreateListOfMultiedge(nodeToMultiEdge, edge.Source).Add(edge);

            foreach (var list in nodeToMultiEdge.Values)
                if (list.Count > 1)
                    yield return list;
        }

        static List<GeomEdge> GetOrCreateListOfMultiedge(Dictionary<GeomNode, List<GeomEdge>> nodeToMultiEdge,
            GeomNode node) {
            List<GeomEdge> list;
            if (nodeToMultiEdge.TryGetValue(node, out list))
                return list;

            return nodeToMultiEdge[node] = new List<GeomEdge>();
        }

        internal UndoRedoAction InsertToListAndSetTheBoxBefore(UndoRedoAction action) {
            UndoRedoActionsList.AddAction(action);
            action.GraphBoundingBoxBefore = action.Graph.BoundingBox;
            RaiseChangeInUndoList();
            return action;
        }

        void RaiseChangeInUndoList() {
            if (ChangeInUndoRedoList != null)
                ChangeInUndoRedoList(this, null);
        }

        /// <summary>
        ///     preparing for an edge corner dragging
        /// </summary>
        /// <param name="geometryEdge"></param>
        /// <param name="site"></param>
        /// <returns></returns>
        public UndoRedoAction PrepareForEdgeCornerDragging(GeomEdge geometryEdge, Site site) {
            EditedEdge = geometryEdge;
            UndoRedoAction edgeDragUndoRedoAction = CreateEdgeEditUndoRedoAction();
//            var edgeRestoreDate = (EdgeRestoreData) edgeDragUndoRedoAction.GetRestoreData(geometryEdge);
//            edgeRestoreDate.Site = site;
            return InsertToListAndSetTheBoxBefore(edgeDragUndoRedoAction);
        }

        /// <summary>
        ///     prepares for the polyline corner removal
        /// </summary>
        /// <param name="affectedEdge"></param>
        /// <param name="site"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public UndoRedoAction PrepareForPolylineCornerRemoval(IViewerObject affectedEdge, Site site) {
            var action = new SiteRemoveUndoAction(EditedEdge) {
                RemovedSite = site,
            };
            action.AddAffectedObject(affectedEdge);
            return InsertToListAndSetTheBoxBefore(action);
        }

        /// <summary>
        ///     prepare for polyline corner insertion
        /// </summary>
        /// <param name="affectedObj">edited objects</param>
        /// <param name="site">the site to insert</param>
        /// <returns></returns>
        internal UndoRedoAction PrepareForPolylineCornerInsertion(IViewerObject affectedObj, Site site) {
            var action = new SiteInsertUndoAction(EditedEdge) {
                InsertedSite = site,
            };
            action.AddAffectedObject(affectedObj);
            return InsertToListAndSetTheBoxBefore(action);
        }


        UndoRedoAction CreateEdgeEditUndoRedoAction() {
            return new EdgeDragUndoRedoAction(EditedEdge);
        }

        /// <summary>
        ///     Undoes the last editing.
        /// </summary>
        public void Undo() {
            if (CanUndo) {
                UndoRedoActionsList.CurrentUndo.Undo();
                UndoRedoActionsList.CurrentRedo = UndoRedoActionsList.CurrentUndo;
                UndoRedoActionsList.CurrentUndo = UndoRedoActionsList.CurrentUndo.Previous;
                RaiseChangeInUndoList();
            }
        }

        /// <summary>
        ///     redo the dragging
        /// </summary>
        public void Redo() {
            if (CanRedo) {
                UndoRedoActionsList.CurrentRedo.Redo();
                UndoRedoActionsList.CurrentUndo = UndoRedoActionsList.CurrentRedo;
                UndoRedoActionsList.CurrentRedo = UndoRedoActionsList.CurrentRedo.Next;
                RaiseChangeInUndoList();
            }
        }

        /// <summary>
        ///     clear the editor
        /// </summary>
        public void Clear() {
            objectsToDrag = new Set<GeometryObject>();
            edgesDraggedWithSource.Clear();
            edgesDraggedWithTarget.Clear();
            UndoRedoActionsList = new UndoRedoActionsList();
            EditedEdge = null;
        }

        /// <summary>
        ///     gets the enumerator pointing to the polyline corner before the point
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Site GetPreviousSite(GeomEdge edge, Point point) {
            Site prevSite = edge.UnderlyingPolyline.HeadSite;
            Site nextSite = prevSite.Next;
            do {
                if (BetweenSites(prevSite, nextSite, point))
                    return prevSite;
                prevSite = nextSite;
                nextSite = nextSite.Next;
            } while (nextSite != null);
            return null;
        }

        static bool BetweenSites(Site prevSite, Site nextSite, Point point) {
            double par = Point.ClosestParameterOnLineSegment(point, prevSite.Point, nextSite.Point);
            return par > 0.1 && par < 0.9;
        }

        /// <summary>
        ///     insert a polyline corner
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="point">the point to insert the corner</param>
        /// <param name="siteBeforeInsertion"></param>
        /// <param name="affectedEntity">an object to be stored in the undo action</param>
        public void InsertSite(GeomEdge edge, Point point, Site siteBeforeInsertion, IViewerObject affectedEntity) {
            EditedEdge = edge;
            //creating the new site
            Site first = siteBeforeInsertion;
            Site second = first.Next;
            var s = new Site(first, point, second);
            PrepareForPolylineCornerInsertion(affectedEntity, s);

            //just to recalc everything in a correct way
            DragEdgeWithSite(new Point(0, 0), edge, s);
        }

        /// <summary>
        ///     deletes the polyline corner
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="site"></param>
        /// <param name="userData">an object to be stored in the unde action</param>
        public void DeleteSite(GeomEdge edge, Site site, IViewerObject userData) {
            EditedEdge = edge;
            PrepareForPolylineCornerRemoval(userData, site);
            site.Previous.Next = site.Next; //removing the site from the list
            site.Next.Previous = site.Previous;
            //just to recalc everything in a correct way
            DragEdgeWithSite(new Point(0, 0), edge,
                site.Previous);
        }

        /// <summary>
        ///     finds the polyline corner near the mouse position
        /// </summary>
        /// <param name="underlyingPolyline"></param>
        /// <param name="mousePoint"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public static Site FindCornerForEdit(SmoothedPolyline underlyingPolyline, Point mousePoint, double tolerance) {
            Site site = underlyingPolyline.HeadSite.Next;
            tolerance *= tolerance; //square the tolerance

            do {
                if (site.Previous == null || site.Next == null)
                    continue; //don't return the first and the last corners
                Point diff = mousePoint - site.Point;
                if (diff*diff <= tolerance)
                    return site;

                site = site.Next;
            } while (site.Next != null);
            return null;
        }

        /// <summary>
        ///     finds the polyline corner near the mouse position
        /// </summary>
        /// <param name="underlyingPolyline"></param>
        /// <param name="mousePoint"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        static Site FindClosestCornerForEdit(SmoothedPolyline underlyingPolyline, Point mousePoint) {
            var site = underlyingPolyline.HeadSite.Next;
            var bestSite = site;
            var dist = (bestSite.Point - mousePoint).LengthSquared;
            while (site.Next != null) {
                site = site.Next;
                var d = (mousePoint - site.Point).LengthSquared;
                if (d < dist) {
                    bestSite = site;
                    dist = d;
                }
            }
            return bestSite;
        }

        /// <summary>
        ///     creates a "tight" bounding box
        /// </summary>
        /// <param name="affectedEntity">the object corresponding to the graph</param>
        /// <param name="geometryGraph"></param>
        public void FitGraphBoundingBox(IViewerObject affectedEntity, GeometryGraph geometryGraph) {
            if (geometryGraph != null) {
                var uAction = new UndoRedoAction(geometryGraph) {Graph = geometryGraph};
                UndoRedoActionsList.AddAction(uAction);
                var r = new Rectangle();
                foreach (GeomNode n in geometryGraph.Nodes) {
                    r = n.BoundingBox;
                    break;
                }
                foreach (GeomNode n in geometryGraph.Nodes) {
                    r.Add(n.BoundingBox);
                }
                foreach (GeomEdge e in geometryGraph.Edges) {
                    r.Add(e.BoundingBox);
                    if (e.Label != null)
                        r.Add(e.Label.BoundingBox);
                }


                r.Left -= geometryGraph.Margins;
                r.Top += geometryGraph.Margins;
                r.Bottom -= geometryGraph.Margins;
                r.Right += geometryGraph.Margins;
                uAction.ClearAffectedObjects();
                uAction.AddAffectedObject(affectedEntity);
                uAction.GraphBoundingBoxAfter = geometryGraph.BoundingBox = r;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="delta"></param>
        public void OnDragEnd(Point delta) {

            if (CurrentUndoAction != null) {
                var action = CurrentUndoAction;
                action.GraphBoundingBoxAfter = action.Graph.BoundingBox;
            }
        }

        internal void ReactOnViewChange() {
            LgLayoutSettings.Interactor.RunOnViewChange();
        }

        internal void ForgetDragging() {
            incrementalDragger = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="changedClusters"></param>
        public void PrepareForClusterCollapseChange(IEnumerable<IViewerNode> changedClusters) {
            InsertToListAndSetTheBoxBefore(new ClustersCollapseExpandUndoRedoAction(graph));
            foreach (var iCluster in changedClusters) {
                CurrentUndoAction.AddAffectedObject(iCluster);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ClustersCollapseExpandUndoRedoAction : UndoRedoAction {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="geometryGraph"></param>
        public ClustersCollapseExpandUndoRedoAction(GeometryGraph geometryGraph) : base(geometryGraph) {
        }
    }
}