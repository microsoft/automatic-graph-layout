using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Prototype.LayoutEditing;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// Provides  graph nodes dragging functionality at the moment
    /// </summary>
    public class LayoutEditor {

        internal IViewerObject ActiveDraggedObject { get; set; }

        internal Site PolylineVertex { get; set; }

        Tuple<Site, PolylineCornerType> cornerInfo;

        readonly Dictionary<IViewerObject, VoidDelegate> decoratorRemovalsDict =
            new Dictionary<IViewerObject, VoidDelegate>();

        readonly Set<IViewerObject> dragGroup = new Set<IViewerObject>();

        readonly GeometryGraphEditor geomGraphEditor = new GeometryGraphEditor();
        Graph graph;

        Dictionary<Polyline, List<IViewerNode>> looseObstaclesToTheirViewerNodes;
        Point mouseDownGraphPoint;
        double mouseMoveThreshold = 0.05;
        Point mouseRightButtonDownPoint;
        DelegateForEdge removeEdgeDraggingDecorations;
        Polyline sourceLoosePolyline;
        IViewerNode sourceOfInsertedEdge;
        Port sourcePort;
        IViewerNode targetOfInsertedEdge;
        Port targetPort;
        IViewer viewer;
        EdgeGeometry EdgeGeometry { get; set; }
        InteractiveEdgeRouter InteractiveEdgeRouter { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="viewerPar">the viewer that the editor communicates with</param>
        public LayoutEditor(IViewer viewerPar) {
            viewer = viewerPar;
            HookUpToViewerEvents();
            ToggleEntityPredicate =
                (modifierKeys, mouseButtons, draggingParameter) => LeftButtonIsPressed(mouseButtons);
            NodeInsertPredicate =
                (modifierKeys, mouseButtons, draggingParameter) =>
                MiddleButtonIsPressed(mouseButtons) && draggingParameter == false;
            DecorateObjectForDragging = TheDefaultObjectDecorator;
            RemoveObjDraggingDecorations = TheDefaultObjectDecoratorRemover;
            DecorateEdgeForDragging = TheDefaultEdgeDecoratorStub;
            DecorateEdgeLabelForDragging = TheDefaultEdgeLabelDecoratorStub;
            RemoveEdgeDraggingDecorations = TheDefaultEdgeDecoratorStub;

            geomGraphEditor.ChangeInUndoRedoList += LayoutEditorChangeInUndoRedoList;

        }

        void HookUpToViewerEvents() {
            viewer.MouseDown += ViewerMouseDown;
            viewer.MouseMove += ViewerMouseMove;
            viewer.MouseUp += ViewerMouseUp;
            viewer.ObjectUnderMouseCursorChanged += ViewerObjectUnderMouseCursorChanged;
            viewer.GraphChanged += ViewerGraphChanged;
            viewer.ViewChangeEvent += ViewChangeEventHandler;
        }

        void ViewChangeEventHandler(object sender, EventArgs e) {
            if (graph == null) return;
            if (graph.LayoutAlgorithmSettings is LgLayoutSettings)
                geomGraphEditor.ReactOnViewChange();
        }

        
        /// <summary>
        /// current graph of under editin
        /// </summary>
        public Graph Graph {
            get { return graph; }
            set {
                graph = value;
                if (graph != null)
                {
                    geomGraphEditor.Graph = graph.GeometryGraph;
                    geomGraphEditor.LayoutSettings = graph.LayoutAlgorithmSettings;
                }
            }
        }


        /// <summary>
        /// the current selected edge
        /// </summary>
        public IViewerEdge SelectedEdge { get; set; }

        /// <summary>
        /// If the distance between the mouse down point and the mouse up point is greater than the threshold 
        /// then we have a mouse move. Otherwise we have a click.
        /// </summary>
        public double MouseMoveThreshold {
            get { return mouseMoveThreshold; }
            set { mouseMoveThreshold = value; }
        }

        /// <summary>
        /// the delegate to decide if an entity is dragged or we just zoom in the viewer
        /// </summary>
        public MouseAndKeysAnalyzer ToggleEntityPredicate { get; set; }

        bool Dragging { get; set; }

        Point MouseDownScreenPoint { get; set; }


        /// <summary>
        /// current pressed mouse buttons
        /// </summary>
        public MouseButtons PressedMouseButtons { get; set; }

        /// <summary>
        /// a delegate to decorate a node for dragging
        /// </summary>
        public DelegateForIViewerObject DecorateObjectForDragging { get; set; }

        /// <summary>
        /// a delegate decorate an edge for editing
        /// </summary>
        public DelegateForEdge DecorateEdgeForDragging { get; set; }

        /// <summary>
        /// a delegate decorate a label for editing
        /// </summary>
        public DelegateForIViewerObject DecorateEdgeLabelForDragging { get; set; }


        /// <summary>
        /// a delegate to remove node decorations
        /// </summary>
        public DelegateForIViewerObject RemoveObjDraggingDecorations { get; set; }

        /// <summary>
        /// a delegate to remove edge decorations
        /// </summary>
        public DelegateForEdge RemoveEdgeDraggingDecorations {
            get { return removeEdgeDraggingDecorations; }
            set { removeEdgeDraggingDecorations = value; }
        }

        /// <summary>
        /// The method analysing keys and mouse buttons to decide if we are inserting a node
        /// </summary>
        public MouseAndKeysAnalyzer NodeInsertPredicate { get; set; }

        bool LeftMouseButtonWasPressed { get; set; }

        internal IViewerNode SourceOfInsertedEdge {
            get { return sourceOfInsertedEdge; }
            set { sourceOfInsertedEdge = value; }
        }

        internal IViewerNode TargetOfInsertedEdge {
            get { return targetOfInsertedEdge; }
            set {
                targetOfInsertedEdge = value;
            }
        }

        Port SourcePort {
            get { return sourcePort; }
            set { sourcePort = value; }
        }

        Port TargetPort {
            get { return targetPort; }
            set { targetPort = value; }
        }

        /// <summary>
        /// returns true if Undo is available
        /// </summary>
        public bool CanUndo {
            get { return geomGraphEditor.CanUndo; }
        }

        /// <summary>
        /// return true if Redo is available
        /// </summary>
        public bool CanRedo {
            get { return geomGraphEditor.CanRedo; }
        }

        /// <summary>
        /// If set to true then we are in a mode for node insertion
        /// </summary>
        public bool InsertingEdge {
            get {
                if (viewer == null)
                    return false;
                return viewer.InsertingEdge;
            }

            set {
                if (viewer == null)
                    return;
                viewer.InsertingEdge = value;
            }
        }

        /// <summary>
        /// current undo action
        /// </summary>
        public UndoRedoAction CurrentUndoAction {
            get { return geomGraphEditor.UndoMode ? geomGraphEditor.CurrentUndoAction : geomGraphEditor.CurrentRedoAction; }
        }

        public EdgeAttr EdgeAttr { get => edgeAttr; set => edgeAttr = value; }


        /// <summary>
        /// signals that there is a change in the undo/redo list
        /// There are four possibilities: Undo(Redo) becomes available (unavailable)
        /// </summary>
        public event EventHandler ChangeInUndoRedoList;

        void ViewerObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e) {
            if (TargetPort != null) {
                viewer.RemoveTargetPortEdgeRouting();
                TargetPort = null;
            }
        }


        void ViewerGraphChanged(object sender, EventArgs e) {
            var iViewer = (sender as IViewer);
            if (iViewer != null) {
                graph = iViewer.Graph;
                if (graph != null && graph.GeometryGraph != null) {
                    geomGraphEditor.Graph = graph.GeometryGraph;
                    geomGraphEditor.LayoutSettings = graph.LayoutAlgorithmSettings;
                    AttachInvalidateEventsToGeometryObjects();
                }
            }
            ActiveDraggedObject = null;
            decoratorRemovalsDict.Clear();
            dragGroup.Clear();
            CleanObstacles();
        }

        /// <summary>
        /// 
        /// </summary>
        public void CleanObstacles() {
            InteractiveEdgeRouter = null;
            looseObstaclesToTheirViewerNodes = null;
            SourceOfInsertedEdge = null;
            TargetOfInsertedEdge = null;
            SourcePort = null;
            TargetPort = null;
            viewer.RemoveSourcePortEdgeRouting();
            viewer.RemoveTargetPortEdgeRouting();

        }

        void AttachInvalidateEventsToGeometryObjects() {
            foreach (var entity in viewer.Entities)
                AttachLayoutChangeEvent(entity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewerObject"></param>
        public void AttachLayoutChangeEvent(IViewerObject viewerObject) {
            var drawingObject = viewerObject.DrawingObject;
            if (drawingObject != null) {
                var geom = drawingObject.GeometryObject;
                if (geom != null)
                    geom.BeforeLayoutChangeEvent += (a, b) => ReportBeforeChange(viewerObject);
                var cluster = geom as Cluster;
                if (cluster != null) {
                    var iViewerNode = (IViewerNode) viewerObject;
                    iViewerNode.IsCollapsedChanged += RelayoutOnIsCollapsedChanged;
                }                
            }
        }

        void RelayoutOnIsCollapsedChanged(IViewerNode iCluster) {
            geomGraphEditor.PrepareForClusterCollapseChange(new[]{iCluster});
            var cluster = (Cluster)iCluster.DrawingObject.GeometryObject;
            if (cluster.IsCollapsed)
                CollapseCluster(cluster);
            else
                ExpandCluster(cluster);

            //LayoutAlgorithmSettings.ShowGraph(viewer.Graph.GeometryGraph);

            foreach(IViewerObject o in geomGraphEditor.CurrentUndoAction.AffectedObjects)
                viewer.Invalidate(o);

        }

        void ExpandCluster(Cluster cluster) {
            //todo: try to find a better method for expanding, mst tree? Procrustes transofrm
            var relayout = new Relayout(viewer.Graph.GeometryGraph, new[] { cluster }, null, cl =>
            {
                var subgraph = cl.UserData as Subgraph;
                if (subgraph != null && subgraph.LayoutSettings != null) return subgraph.LayoutSettings;
                return viewer.Graph.LayoutAlgorithmSettings;
            });
            relayout.Run();
            MakeExpandedNodesVisible(cluster);
            MakeExpandedEdgesVisible(cluster);
        }

        void MakeExpandedNodesVisible(Cluster cluster) {
            foreach (var node in cluster.Nodes)
                ((Node) node.UserData).IsVisible = true;
            foreach (var cl in cluster.Clusters) {
                ((Node) cl.UserData).IsVisible = true;
                if(!cl.IsCollapsed)
                    MakeExpandedNodesVisible(cl);
            }
        }

        void MakeExpandedEdgesVisible(Cluster cluster) {
            Debug.Assert(cluster.IsCollapsed == false);
            foreach (var node in cluster.Nodes)
                UnhideNodeEdges((Node) node.UserData);

            foreach (var cl in cluster.Clusters) {
                UnhideNodeEdges((Node) cl.UserData);
                if (!cl.IsCollapsed)
                    MakeExpandedEdgesVisible(cl);
            }
        }

        static void UnhideNodeEdges(Node drn) {
            foreach (var e in drn.SelfEdges)
                e.IsVisible = true;
            foreach (var e in drn.OutEdges.Where(e => e.TargetNode.IsVisible))
                e.IsVisible = true;
            foreach (var e in drn.InEdges.Where(e => e.SourceNode.IsVisible))
                e.IsVisible = true;
        }

        void CollapseCluster(Cluster cluster) {
            HideCollapsed(cluster);
            var center = cluster.RectangularBoundary.Rect.Center;
            var del = center - cluster.CollapsedBoundary.BoundingBox.Center;
            cluster.CollapsedBoundary.Translate(del);
            //todo: try to find a better method for collapsing, mst tree?
            var relayout = new Relayout(viewer.Graph.GeometryGraph, new[] {cluster}, null, cl => {
                var subgraph = cl.UserData as Subgraph;
                if (subgraph != null && subgraph.LayoutSettings != null) return subgraph.LayoutSettings;
                return viewer.Graph.LayoutAlgorithmSettings;
            });
            relayout.Run();
        }

        static void HideCollapsed(Cluster cluster) {
            foreach (var n in cluster.Nodes) {
                var drawingNode = (Node)n.UserData;
                drawingNode.IsVisible = false;
            }
            foreach (var cl in cluster.Clusters) {
                var drawingNode = (Node)cl.UserData;
                drawingNode.IsVisible = false;
                if (!cl.IsCollapsed)
                    HideCollapsed(cl);
            }            
        }


        void ReportBeforeChange(IViewerObject viewerObject) {
            if (CurrentUndoAction==null || CurrentUndoAction.ContainsAffectedObject(viewerObject)) return;
            CurrentUndoAction.AddAffectedObject(viewerObject);
            CurrentUndoAction.AddRestoreData(viewerObject.DrawingObject.GeometryObject,
                                         RestoreHelper.GetRestoreData(viewerObject.DrawingObject.GeometryObject));
        }


        

  

        /// <summary>
        /// Unsubscibes from the viewer events
        /// </summary>
        public void DetouchFromViewerEvents() {
            viewer.MouseDown -= ViewerMouseDown;
            viewer.MouseMove -= ViewerMouseMove;
            viewer.MouseUp -= ViewerMouseUp;
            viewer.GraphChanged -= ViewerGraphChanged;
            viewer.ViewChangeEvent -= ViewChangeEventHandler;
            geomGraphEditor.ChangeInUndoRedoList -= LayoutEditorChangeInUndoRedoList;
        }


        void LayoutEditorChangeInUndoRedoList(object sender, EventArgs e) {
            if (ChangeInUndoRedoList != null)
                ChangeInUndoRedoList(this, null);
        }


        void TheDefaultObjectDecorator(IViewerObject obj) {
            var node = obj as IViewerNode;
            if (node != null) {
                var drawingNode = node.Node;
                var w = drawingNode.Attr.LineWidth;
                if (!decoratorRemovalsDict.ContainsKey(node))
                {
                    decoratorRemovalsDict[node] = (() => drawingNode.Attr.LineWidth = w);
                }
                drawingNode.Attr.LineWidth = (int) Math.Max(viewer.LineThicknessForEditing, w*2);
            } else {
                var edge = obj as IViewerEdge;
                if (edge != null) {
                    var drawingEdge = edge.Edge;
                    var w = drawingEdge.Attr.LineWidth;
                    if (!decoratorRemovalsDict.ContainsKey(edge))
                    {
                        decoratorRemovalsDict[edge] = (() => drawingEdge.Attr.LineWidth = w);
                    }
                    drawingEdge.Attr.LineWidth = (int) Math.Max(viewer.LineThicknessForEditing, w*2);
                }
            }
            viewer.Invalidate(obj);
        }

        void TheDefaultObjectDecoratorRemover(IViewerObject obj) {
            VoidDelegate decoratorRemover;
            if (decoratorRemovalsDict.TryGetValue(obj, out decoratorRemover)) {
                decoratorRemover(); 
                decoratorRemovalsDict.Remove(obj);
                viewer.Invalidate(obj);
            }
            var node=obj as IViewerNode;
            if (node != null)
                foreach (var edge in Edges(node))
                    RemoveObjDraggingDecorations(edge);
        }
    

        static void TheDefaultEdgeDecoratorStub(IViewerEdge edge) {}

        static void TheDefaultEdgeLabelDecoratorStub(IViewerObject label) {}

        static bool LeftButtonIsPressed(MouseButtons mouseButtons) {
            return (mouseButtons & MouseButtons.Left) == MouseButtons.Left;
        }

        static bool MiddleButtonIsPressed(MouseButtons mouseButtons) {
            return (mouseButtons & MouseButtons.Middle) == MouseButtons.Middle;
        }


        bool MouseDownPointAndMouseUpPointsAreFarEnoughOnScreen(MsaglMouseEventArgs e) {
            int x = e.X;
            int y = e.Y;
            double dx = (MouseDownScreenPoint.X - x)/viewer.DpiX;
            double dy = (MouseDownScreenPoint.Y - y)/viewer.DpiY;
            return Math.Sqrt(dx*dx + dy*dy) > MouseMoveThreshold/3;
        }

        void AnalyzeLeftMouseButtonClick() {
            bool modifierKeyIsPressed = ModifierKeyIsPressed();
            IViewerObject obj = viewer.ObjectUnderMouseCursor;
            if (obj != null) {
                var editableEdge = obj as IViewerEdge;
                if (editableEdge != null) {
                    var drawingEdge = editableEdge.DrawingObject as Edge;
                    if (drawingEdge != null) {
                        var geomEdge = drawingEdge.GeometryEdge;
                        if (geomEdge != null && viewer.LayoutEditingEnabled) {
                            if (geomEdge.UnderlyingPolyline == null)
                                geomEdge.UnderlyingPolyline = CreateUnderlyingPolyline(geomEdge);

                            SwitchToEdgeEditing(editableEdge);
                        }
                    }
                } else {
                    if (obj.MarkedForDragging)
                        UnselectObjectForDragging(obj);
                    else {
                        if (!modifierKeyIsPressed)
                            UnselectEverything();
                        SelectObjectForDragging(obj);
                    }
                    UnselectEdge();
                }
            }
        }

        static SmoothedPolyline CreateUnderlyingPolyline(Core.Layout.Edge geomEdge) {
            var ret = SmoothedPolyline.FromPoints(CurvePoints(geomEdge));
            return ret;
        }

        static IEnumerable<Point> CurvePoints(Core.Layout.Edge geomEdge) {
            yield return geomEdge.Source.Center;
            var curve = geomEdge.Curve as Curve;
            if(curve!=null) {
                if (curve.Segments.Count > 0)
                    yield return curve.Start;
                for (int i = 0; i < curve.Segments.Count; i++)
                    yield return curve.Segments[i].End;
            }
            yield return geomEdge.Target.Center;
        }

//        static void SetCoefficientsCorrecty(SmoothedPolyline ret, ICurve curve) {
//           //  throw new NotImplementedException();
//        }

        bool ModifierKeyIsPressed() {
            bool modifierKeyWasUsed = (viewer.ModifierKeys & ModifierKeys.Control) == ModifierKeys.Control
                                      || (viewer.ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift;
            return modifierKeyWasUsed;
        }

        void SwitchToEdgeEditing(IViewerEdge edge) {
            UnselectEverything();
            var editableEdge = edge as IEditableObject;
            if (editableEdge == null) return;
                
            SelectedEdge = edge;
            editableEdge.SelectedForEditing = true;
            edge.RadiusOfPolylineCorner = viewer.UnderlyingPolylineCircleRadius;
            DecorateEdgeForDragging(edge);
            viewer.Invalidate(edge);
        }


        IEnumerable<IViewerNode> ViewerNodes() {
            foreach (IViewerObject o in viewer.Entities) {
                var n = o as IViewerNode;
                if (n != null)
                    yield return n;
            }
        }

        void SelectObjectForDragging(IViewerObject obj) {
            if (obj.MarkedForDragging == false) {
                obj.MarkedForDragging = true;
                dragGroup.Insert(obj);
                DecorateObjectForDragging(obj);
            }
        }

        void UnselectObjectForDragging(IViewerObject obj) {
            UnselectWithoutRemovingFromDragGroup(obj);
            dragGroup.Remove(obj);
        }

        void UnselectWithoutRemovingFromDragGroup(IViewerObject obj) {
            obj.MarkedForDragging = false;
            RemoveObjDraggingDecorations(obj);
        }

        void UnselectEverything() {
            foreach (IViewerObject obj in dragGroup) {
                viewer.Invalidate(obj);
                UnselectWithoutRemovingFromDragGroup(obj);
            }
            dragGroup.Clear();
            UnselectEdge();
        }

        void UnselectEdge() {
            if (SelectedEdge != null) {
                ((IEditableObject) SelectedEdge).SelectedForEditing = false;
                removeEdgeDraggingDecorations(SelectedEdge);
                viewer.Invalidate(SelectedEdge);
                SelectedEdge = null;
            }
        }

        static IEnumerable<IViewerEdge> Edges(IViewerNode node) {
            foreach (IViewerEdge edge in node.SelfEdges)
                yield return edge;
            foreach (IViewerEdge edge in node.OutEdges)
                yield return edge;
            foreach (IViewerEdge edge in node.InEdges)
                yield return edge;
        }

        

        void ViewerMouseDown(object sender, MsaglMouseEventArgs e) {
            if (!viewer.LayoutEditingEnabled || viewer.Graph == null) return;

            PressedMouseButtons = GetPressedButtons(e);
            mouseDownGraphPoint = viewer.ScreenToSource(e);
            MouseDownScreenPoint = new Point(e.X, e.Y);
            if (e.LeftButtonIsPressed) {
                LeftMouseButtonWasPressed = true;
                if (!InsertingEdge) {
                    if (!(viewer.ObjectUnderMouseCursor is IViewerEdge))
                        ActiveDraggedObject = viewer.ObjectUnderMouseCursor;
                    if (ActiveDraggedObject != null)
                        e.Handled = true;
                    if (SelectedEdge != null)
                        CheckIfDraggingPolylineVertex(e);
                } else if (SourceOfInsertedEdge != null && SourcePort != null && DraggingStraightLine())
                    viewer.StartDrawingRubberLine(sourcePort.Location);
            } else if (e.RightButtonIsPressed)
                if (SelectedEdge != null)
                    ProcessRightClickOnSelectedEdge(e);
        }


        
        void ViewerMouseMove(object sender, MsaglMouseEventArgs e) {
            if (viewer.LayoutEditingEnabled) {                 
                if (e.LeftButtonIsPressed) {
                    if (ActiveDraggedObject != null || PolylineVertex != null)
                        DragSomeObjects(e);
                    else if (InsertingEdge)
                        MouseMoveWhenInsertingEdgeAndPressingLeftButton(e);
                    else
                        MouseMoveLiveSelectObjectsForDragging(e);
                } else if(InsertingEdge)
                    HandleMouseMoveWhenInsertingEdgeAndNotPressingLeftButton(e);
            }            
        }

        
        void SetDraggingFlag(MsaglMouseEventArgs e) {
            if (!Dragging && MouseDownPointAndMouseUpPointsAreFarEnoughOnScreen(e))
                Dragging = true;
        }

        bool TrySetNodePort(MsaglMouseEventArgs e, ref IViewerNode node, ref Port port, out Polyline loosePolyline) {
            Debug.Assert(InsertingEdge);

            Point mousePosition = viewer.ScreenToSource(e);
            loosePolyline = null;
            if (Graph != null) {
                if (DraggingStraightLine()) {
                    node= SetPortWhenDraggingStraightLine(ref port, ref mousePosition);
                } else {
                    if (InteractiveEdgeRouter == null)
                        PrepareForEdgeDragging();
                    loosePolyline = InteractiveEdgeRouter.GetHitLoosePolyline(viewer.ScreenToSource(e));
                    if (loosePolyline != null)
                        SetPortUnderLoosePolyline(mousePosition, loosePolyline, ref node, ref port);
                    else {
                        node = null;
                        port = null;
                    }
                }
            }
            return port != null;
        }

        IViewerNode SetPortWhenDraggingStraightLine(ref Port port, ref Point mousePosition) {
           var viewerNode = viewer.ObjectUnderMouseCursor as IViewerNode;
            if (viewerNode != null){
                double t;
                GeometryNode geomNode = ((Node) viewerNode.DrawingObject).GeometryNode;
                if (NeedToCreateBoundaryPort(mousePosition, viewerNode, out t))
                    port = CreateOrUpdateCurvePort(t, geomNode, port);
                else
                    port = PointIsInside(mousePosition, ((Node) viewerNode.DrawingObject).GeometryNode.BoundaryCurve)
                               ? CreateFloatingPort(geomNode, ref mousePosition)
                               : null;
            }
            else 
                port = null;
            return viewerNode;
        }

        Port CreateOrUpdateCurvePort(double t, GeometryNode geomNode, Port port) {
            var cp = port as CurvePort;
            if (cp == null)
                return new CurvePort(geomNode.BoundaryCurve, t);
            cp.Parameter = t;
            cp.Curve = geomNode.BoundaryCurve;
            return port;
        }

        FloatingPort CreateFloatingPort( GeometryNode geomNode, ref Point location) {
            return new FloatingPort(geomNode.BoundaryCurve, location);
        }

        void SetPortUnderLoosePolyline(Point mousePosition, Polyline loosePoly, ref IViewerNode node, ref Port port) {
            double dist = double.PositiveInfinity;
            double par = 0;
            foreach (var viewerNode in GetViewerNodesInsideOfLooseObstacle(loosePoly)) {
                var curve = ((Node)viewerNode.DrawingObject).GeometryNode.BoundaryCurve;
                if (PointIsInside(mousePosition, curve)) {
                    node = viewerNode;
                    SetPortForMousePositionInsideOfNode(mousePosition, node, ref port);
                    return;
                }
                double p = curve.ClosestParameter(mousePosition);
                double d = (curve[p] - mousePosition).Length;
                if (d < dist) {
                    par = p;
                    dist = d;
                    node = viewerNode;
                }
            }
            
            port = CreateOrUpdateCurvePort(par, ((Node)node.DrawingObject).GeometryNode, port);
        }

        IEnumerable<IViewerNode> GetViewerNodesInsideOfLooseObstacle(Polyline loosePoly) {
            if (looseObstaclesToTheirViewerNodes == null)
                InitLooseObstaclesToViewerNodeMap();
            return looseObstaclesToTheirViewerNodes[loosePoly];
        }

        void InitLooseObstaclesToViewerNodeMap() {
            looseObstaclesToTheirViewerNodes = new Dictionary<Polyline, List<IViewerNode>>();
            foreach (IViewerNode viewerNode in ViewerNodes()) {
                Polyline loosePoly =
                     InteractiveEdgeRouter.GetHitLoosePolyline(GeometryNode(viewerNode).Center);
                List<IViewerNode> loosePolyNodes;
                if (!looseObstaclesToTheirViewerNodes.TryGetValue(loosePoly, out loosePolyNodes))
                    looseObstaclesToTheirViewerNodes[loosePoly] = loosePolyNodes = new List<IViewerNode>();

                loosePolyNodes.Add(viewerNode);
            }
        }

        void SetPortForMousePositionInsideOfNode(Point mousePosition,
                                                 IViewerNode node, ref Port port) {
            GeometryNode geomNode = GeometryNode(node);
            double t;
            if (NeedToCreateBoundaryPort(mousePosition, node, out t))
                port = CreateOrUpdateCurvePort(t, geomNode, port);
            else
                port = CreateFloatingPort(geomNode, ref mousePosition);
        }

        static GeometryNode GeometryNode(IViewerNode node) {
            GeometryNode geomNode = ((Node) node.DrawingObject).GeometryNode;
            return geomNode;
        }

        static bool PointIsInside(Point point, ICurve iCurve) {
            return Curve.PointRelativeToCurveLocation(point, iCurve) == PointLocation.Inside;
        }

        bool NeedToCreateBoundaryPort(Point mousePoint, IViewerNode node, out double portParameter) {
            var drawingNode = node.DrawingObject as Node;
            ICurve curve = drawingNode.GeometryNode.BoundaryCurve;
            portParameter = curve.ClosestParameter(mousePoint);
            Point pointOnCurve = curve[portParameter];
            double length = (mousePoint - pointOnCurve).Length;
            if (length <= viewer.UnderlyingPolylineCircleRadius*2 + drawingNode.Attr.LineWidth/2) {
                TryToSnapToTheSegmentEnd(ref portParameter, curve, pointOnCurve);
                return true;
            }
            return false;
        }

        void TryToSnapToTheSegmentEnd(ref double portParameter, ICurve curve, Point pointOnCurve) {
            var c = curve as Curve;
            if (c != null) {
                ICurve seg;
                double segPar;
                c.GetSegmentAndParameter(portParameter, out segPar, out seg);
                if (segPar - seg.ParStart < seg.ParEnd - segPar)
                    if ((seg.Start - pointOnCurve).Length < viewer.UnderlyingPolylineCircleRadius*2)
                        portParameter -= segPar - seg.ParStart;
                    else if ((seg.End - pointOnCurve).Length < viewer.UnderlyingPolylineCircleRadius*2)
                        portParameter += seg.ParEnd - segPar;
            }
        }

        Point _lastDragPoint;
       
        void DragSomeObjects(MsaglMouseEventArgs e) {
            if (!Dragging) {
                if (MouseDownPointAndMouseUpPointsAreFarEnoughOnScreen(e)) {
                    Dragging = true;
                    //first time we are in Dragging mode
                    if (PolylineVertex != null)
                        geomGraphEditor.PrepareForEdgeCornerDragging(
                            SelectedEdge.DrawingObject.GeometryObject as Core.Layout.Edge, PolylineVertex);
                    else if (ActiveDraggedObject != null) {
                        UnselectEdge();
                        if (!ActiveDraggedObject.MarkedForDragging)
                            UnselectEverything();
                        SelectObjectForDragging(ActiveDraggedObject);
                        geomGraphEditor.PrepareForObjectDragging(DraggedGeomObjects(), GetDraggingMode());
                    }
                }
                _lastDragPoint = mouseDownGraphPoint;
            }

            if (!Dragging) return;
            var currentDragPoint = viewer.ScreenToSource(e);
            geomGraphEditor.Drag(currentDragPoint - _lastDragPoint, GetDraggingMode(), _lastDragPoint);
            foreach (var affectedObject in CurrentUndoAction.AffectedObjects) {
                viewer.Invalidate(affectedObject);
            }
            if (geomGraphEditor.GraphBoundingBoxGetsExtended)
                viewer.Invalidate();       
            e.Handled = true;
            _lastDragPoint = currentDragPoint;
        }
        
        DraggingMode GetDraggingMode() {
            bool incremental = (viewer.ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift
                            || viewer.IncrementalDraggingModeAlways;
            return incremental
                    ? DraggingMode.Incremental
                    : DraggingMode.Default;

        }

        ///<summary>
        ///</summary>
        public static void RouteEdgesRectilinearly(IViewer viewer) {
            var geomGraph = viewer.Graph.GeometryGraph;
            var settings = viewer.Graph.LayoutAlgorithmSettings;
            RectilinearInteractiveEditor.CreatePortsAndRouteEdges(settings.NodeSeparation / 3, 1, geomGraph.Nodes, geomGraph.Edges,
                                         settings.EdgeRoutingSettings.EdgeRoutingMode, true,
                                         settings.EdgeRoutingSettings.UseObstacleRectangles, settings.EdgeRoutingSettings.BendPenalty);
            var labelPlacer = new EdgeLabelPlacement(geomGraph);
            labelPlacer.Run();

        }


        IEnumerable<GeometryObject> DraggedGeomObjects() {
		   //restrict the dragged elements to be under the same cluster
            Cluster activeObjCluster = GetActiveObjectCluster(ActiveDraggedObject);
            foreach (IViewerObject draggObj in dragGroup)
                if (GetActiveObjectCluster(draggObj) == activeObjCluster)
                    yield return draggObj.DrawingObject.GeometryObject;
        }

        static Cluster GetActiveObjectCluster(IViewerObject viewerObject) {
            var node = viewerObject.DrawingObject.GeometryObject as GeometryNode;
            return node != null ? node.ClusterParent : null;
        }


        void ViewerMouseUp(object sender, MsaglMouseEventArgs args) {
            if (args.Handled) return;
            if (viewer.LayoutEditingEnabled)
                HandleMouseUpOnLayoutEnabled(args);
        }

        void HandleMouseUpOnLayoutEnabled(MsaglMouseEventArgs args) {
            bool click = !MouseDownPointAndMouseUpPointsAreFarEnoughOnScreen(args);
            if (click && LeftMouseButtonWasPressed) {
                if (viewer.ObjectUnderMouseCursor != null) {
                    AnalyzeLeftMouseButtonClick();
                    args.Handled = true;
                }
                else
                    UnselectEverything();
            }
            else if (Dragging) {
                if (!InsertingEdge) {
                    geomGraphEditor.OnDragEnd(viewer.ScreenToSource(args) - mouseDownGraphPoint);
                    InteractiveEdgeRouter = null;
                    looseObstaclesToTheirViewerNodes = null;
                }
                else
                    InsertEdgeOnMouseUp();
                args.Handled = true;
            }
            Dragging = false;
            geomGraphEditor.ForgetDragging();
            PolylineVertex = null;
            ActiveDraggedObject = null;
            LeftMouseButtonWasPressed = false;
            if (TargetPort != null)
                viewer.RemoveTargetPortEdgeRouting();
            if (SourcePort != null)
                viewer.RemoveSourcePortEdgeRouting();

            SourceOfInsertedEdge = TargetOfInsertedEdge = null;
            SourcePort = TargetPort = null;
        }
        EdgeAttr edgeAttr = new EdgeAttr();
        void InsertEdgeOnMouseUp() {
            if (DraggingStraightLine()) {
                viewer.StopDrawingRubberLine();
                viewer.RemoveSourcePortEdgeRouting();
                viewer.RemoveTargetPortEdgeRouting();
                if (SourcePort != null && TargetOfInsertedEdge != null && TargetPort != null) {
                    var drawingEdge = new Edge(SourceOfInsertedEdge.DrawingObject as Node,
                                               TargetOfInsertedEdge.DrawingObject as Node,
                                               ConnectionToGraph.Connected) {
                                                   SourcePort = SourcePort,
                                                   TargetPort = TargetPort
                                               };

                    IViewerEdge edge = viewer.RouteEdge(drawingEdge);
                    viewer.AddEdge(edge, true);
                    AttachLayoutChangeEvent(edge);
                  
                }
            }
            else {
                viewer.StopDrawingRubberEdge();

                if (TargetPort != null) {
                    FinishRoutingEdge();
                    AddEdge();
                }
                InteractiveEdgeRouter.Clean();
            }
        }

        void AddEdge() {
            var drawingEdge = new Edge(SourceOfInsertedEdge.DrawingObject as Node,
                                       TargetOfInsertedEdge.DrawingObject as Node, ConnectionToGraph.Disconnected, this.EdgeAttr.Clone());
            var geomEdge = new Core.Layout.Edge(GeometryNode(SourceOfInsertedEdge),
                                                GeometryNode(TargetOfInsertedEdge)) {EdgeGeometry = EdgeGeometry};
            drawingEdge.GeometryEdge = geomEdge;
            drawingEdge.SourcePort = SourcePort;
            drawingEdge.TargetPort = TargetPort;

            var edge = viewer.CreateEdgeWithGivenGeometry(drawingEdge);
            viewer.AddEdge(edge, true);
            AttachLayoutChangeEvent(edge);
        }

        void FinishRoutingEdge() {
            EdgeGeometry.SourceArrowhead = this.EdgeAttr.ArrowheadAtSource != ArrowStyle.None ? new Arrowhead() { Length = this.EdgeAttr.ArrowheadLength } : null;

            EdgeGeometry.TargetArrowhead = this.EdgeAttr.ArrowheadAtTarget != ArrowStyle.None ? new Arrowhead() { Length = this.EdgeAttr.ArrowheadLength } : null;

            if (TargetOfInsertedEdge != SourceOfInsertedEdge) {
                InteractiveEdgeRouter.TryToRemoveInflectionsAndCollinearSegments(EdgeGeometry.SmoothedPolyline);
                InteractiveEdgeRouter.SmoothCorners(EdgeGeometry.SmoothedPolyline);
                EdgeGeometry.Curve = EdgeGeometry.SmoothedPolyline.CreateCurve();
               
                
                Arrowheads.TrimSplineAndCalculateArrowheads(EdgeGeometry,
                                                            GeometryNode(SourceOfInsertedEdge).BoundaryCurve,
                                                            GeometryNode(TargetOfInsertedEdge).BoundaryCurve,
                                                            EdgeGeometry.Curve, true);

            }
            else {
                EdgeGeometry = CreateEdgeGeometryForSelfEdge(graph.GeometryGraph, GeometryNode(SourceOfInsertedEdge));
            }
            viewer.RemoveSourcePortEdgeRouting();
            viewer.RemoveTargetPortEdgeRouting();
        }

        static EdgeGeometry CreateEdgeGeometryForSelfEdge(GeometryObject geometryGraph, GeometryNode node) {
            var tempEdge = new Core.Layout.Edge(node, node)
            {
                GeometryParent = geometryGraph,
                SourcePort =
                    new FloatingPort(node.BoundaryCurve, node.Center),
                TargetPort =
                    new FloatingPort(node.BoundaryCurve, node.Center),
                EdgeGeometry = { TargetArrowhead = new Arrowhead() }
            };
            StraightLineEdges.CreateSimpleEdgeCurveWithUnderlyingPolyline(tempEdge);
            return tempEdge.EdgeGeometry;
        }


        void SelectEntitiesForDraggingWithRectangle(MsaglMouseEventArgs args) {
            var rect =
                new Rectangle(mouseDownGraphPoint, viewer.ScreenToSource(args));

            foreach (IViewerNode node in ViewerNodes())
                if (rect.Intersects(node.Node.BoundingBox))
                    SelectObjectForDragging(node);

            args.Handled = true;
        }

        void ProcessRightClickOnSelectedEdge(MsaglMouseEventArgs e) {
            mouseRightButtonDownPoint = viewer.ScreenToSource(e);

            cornerInfo = AnalyzeInsertOrDeletePolylineCorner(mouseRightButtonDownPoint,
                                                             SelectedEdge.RadiusOfPolylineCorner);

            if (cornerInfo == null)
                return;

            e.Handled = true;

            var edgeRemoveCouple = new Tuple<string, VoidDelegate>("Remove edge",
                                                                    () => viewer.RemoveEdge(SelectedEdge, true));

            if (cornerInfo.Item2 == PolylineCornerType.PreviousCornerForInsertion)
                viewer.PopupMenus(
                    new Tuple<string, VoidDelegate>("Insert polyline corner", InsertPolylineCorner),
                    edgeRemoveCouple);
            else if (cornerInfo.Item2 == PolylineCornerType.CornerToDelete)
                viewer.PopupMenus(
                    new Tuple<string, VoidDelegate>("Delete polyline corner",
                                                              DeleteCorner), edgeRemoveCouple);
        }


        void CheckIfDraggingPolylineVertex(MsaglMouseEventArgs e) {
            if (SelectedEdge != null && SelectedEdge.Edge.GeometryEdge.UnderlyingPolyline!=null) {
                Site site = SelectedEdge.Edge.GeometryEdge.UnderlyingPolyline.HeadSite;
                do {
                    if (MouseScreenPointIsCloseEnoughToVertex(site.Point,
                                                              SelectedEdge.RadiusOfPolylineCorner +
                                                              SelectedEdge.Edge.Attr.LineWidth/2.0)) {
                        PolylineVertex = site;
                        e.Handled = true;
                        break;
                    }
                    site = site.Next;
                } while (site != null);
            }
        }

        bool MouseScreenPointIsCloseEnoughToVertex(Point point, double radius) {
            return (point - mouseDownGraphPoint).Length < radius;
        }


        static MouseButtons GetPressedButtons(MsaglMouseEventArgs e) {
            var ret = MouseButtons.None;
            if (e.LeftButtonIsPressed)
                ret |= MouseButtons.Left;
            if (e.MiddleButtonIsPressed)
                ret |= MouseButtons.Middle;
            if (e.RightButtonIsPressed)
                ret |= MouseButtons.Right;
            return ret;
        }

        /// <summary>
        /// Undoes the editing
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public void Undo() {
            if (geomGraphEditor.CanUndo) {
                UndoRedoAction action = geomGraphEditor.CurrentUndoAction;
                geomGraphEditor.Undo();
                foreach(var o in action.AffectedObjects)  
                    viewer.Invalidate(o);
                if (action.GraphBoundingBoxHasChanged)
                    viewer.Invalidate();
            }
        }


        /// <summary>
        /// Redoes the editing
        /// </summary>
        public void Redo() {
            if (geomGraphEditor.CanRedo) {
                geomGraphEditor.UndoMode = false;
                UndoRedoAction action = geomGraphEditor.CurrentRedoAction;
                geomGraphEditor.Redo();
                foreach (var o in action.AffectedObjects)
                    viewer.Invalidate(o);
                if (action.GraphBoundingBoxHasChanged)
                    viewer.Invalidate();
           
                geomGraphEditor.UndoMode = true;
            }
        }

        /// <summary>
        /// Clear the editor
        /// </summary>
        public void Clear() {
            UnselectEverything();
        }

        /// <summary>
        /// Finds a corner to delete or insert
        /// </summary>
        /// <param name="point"></param>
        /// <param name="tolerance"></param>
        /// <returns>null if a corner is not found</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline"),
         SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public Tuple<Site, PolylineCornerType> AnalyzeInsertOrDeletePolylineCorner(Point point, double tolerance) {
            if (SelectedEdge == null)
                return null;

            tolerance += SelectedEdge.Edge.Attr.LineWidth;
            Site corner=GeometryGraphEditor.FindCornerForEdit(SelectedEdge.Edge.GeometryEdge.UnderlyingPolyline, point,
                                                           tolerance);
            if (corner != null)
                return new Tuple<Site, PolylineCornerType>(corner, PolylineCornerType.CornerToDelete);

            corner = GeometryGraphEditor.GetPreviousSite(SelectedEdge.Edge.GeometryEdge, point);
            if (corner != null)
                return new Tuple<Site, PolylineCornerType>(corner, PolylineCornerType.PreviousCornerForInsertion);

            return null;
        }


        
        /// <summary>
        /// create a tight bounding box for the graph
        /// </summary>
        /// <param name="graphToFit"></param>
        public void FitGraphBoundingBox(IViewerObject graphToFit) {
            if (graphToFit != null) {
                geomGraphEditor.FitGraphBoundingBox(graphToFit, graphToFit.DrawingObject.GeometryObject as GeometryGraph);
                viewer.Invalidate();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public void RegisterNodeAdditionForUndo(IViewerNode node) {
            var undoAction = new AddNodeUndoAction(graph, viewer, node);
            geomGraphEditor.InsertToListAndSetTheBoxBefore(undoAction);
        }

        /// <summary>
        /// registers the edge addition for undo
        /// </summary>
        /// <param name="edge"></param>
        public void RegisterEdgeAdditionForUndo(IViewerEdge edge) {
            geomGraphEditor.InsertToListAndSetTheBoxBefore(new AddEdgeUndoAction(viewer, edge));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="edge"></param>
        public void RegisterEdgeRemovalForUndo(IViewerEdge edge) {
            geomGraphEditor.InsertToListAndSetTheBoxBefore(new RemoveEdgeUndoAction(graph, viewer, edge));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public void RegisterNodeForRemoval(IViewerNode node) {
            geomGraphEditor.InsertToListAndSetTheBoxBefore(new RemoveNodeUndoAction(viewer, node));
        }

    
        static internal bool RectRouting(EdgeRoutingMode mode) {
            return mode == EdgeRoutingMode.Rectilinear || mode == EdgeRoutingMode.RectilinearToCenter;
        }

        IEnumerable<ICurve> EnumerateNodeBoundaryCurves() {
            return from vn in ViewerNodes() select GeometryNode(vn).BoundaryCurve;
        }

        #region Edge handling
        /// <summary>
        /// 
        /// </summary>
        public void ForgetEdgeDragging() {
            if(viewer.Graph == null)
                return;
            if(DraggingStraightLine())
                return;
            if(!RectRouting(viewer.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode)) {
                InteractiveEdgeRouter = null;
                looseObstaclesToTheirViewerNodes = null;
            }
        }
        /// <summary>
        /// prepares for edge dragging
        /// </summary>
        public void PrepareForEdgeDragging() {
            if(viewer.Graph == null)
                return;
            if(DraggingStraightLine())
                return;
            var settings = viewer.Graph.LayoutAlgorithmSettings;
            if(!RectRouting(settings.EdgeRoutingSettings.EdgeRoutingMode)) {
                if(InteractiveEdgeRouter == null) {
                    var padding = settings.NodeSeparation / 3;
                    var loosePadding = 0.65 * padding;
                    InteractiveEdgeRouter = new InteractiveEdgeRouter(EnumerateNodeBoundaryCurves(),padding,
                                                                      loosePadding,0);
                }
            }
        }
        /// <summary>
        /// insert a polyline corner at the point befor the prevCorner
        /// </summary>
        /// <param name="point"></param>
        /// <param name="previousCorner"></param>
        [SuppressMessage("Microsoft.Naming","CA1704:IdentifiersShouldBeSpelledCorrectly",MessageId = "Polyline")]
        public void InsertPolylineCorner(Point point,Site previousCorner) {
            geomGraphEditor.InsertSite(SelectedEdge.Edge.GeometryEdge,point,previousCorner,SelectedEdge);
            viewer.Invalidate(SelectedEdge);
        }

        void InsertPolylineCorner() {
            geomGraphEditor.InsertSite(SelectedEdge.Edge.GeometryEdge,
                                       mouseRightButtonDownPoint,cornerInfo.Item1,SelectedEdge);
            viewer.Invalidate(SelectedEdge);
        }

        /// <summary>
        /// delete the polyline corner, shortcut it.
        /// </summary>
        /// <param name="corner"></param>
        public void DeleteCorner(Site corner) {
            geomGraphEditor.DeleteSite(SelectedEdge.Edge.GeometryEdge,corner,SelectedEdge);
            viewer.Invalidate(SelectedEdge);
            viewer.OnDragEnd(new IViewerObject [] { SelectedEdge });
        }

        void DeleteCorner() {
            geomGraphEditor.DeleteSite(SelectedEdge.Edge.GeometryEdge,cornerInfo.Item1,SelectedEdge);
            viewer.Invalidate(SelectedEdge);
            viewer.OnDragEnd(new IViewerObject [] { SelectedEdge });
        }

        void HandleMouseMoveWhenInsertingEdgeAndNotPressingLeftButton(MsaglMouseEventArgs e) {
            IViewerNode oldNode = SourceOfInsertedEdge;
            if (TrySetNodePort(e, ref sourceOfInsertedEdge, ref sourcePort, out sourceLoosePolyline))
                viewer.SetSourcePortForEdgeRouting(sourcePort.Location);
            else if (oldNode != null)
                viewer.RemoveSourcePortEdgeRouting();
        }

        void MouseMoveWhenInsertingEdgeAndPressingLeftButton(MsaglMouseEventArgs e) {
            if(SourcePort != null) {
                SetDraggingFlag(e);
                if(Dragging) {
                    Polyline loosePolyline;
                    if(TrySetNodePort(e,ref targetOfInsertedEdge,ref targetPort,out loosePolyline)) {
                        viewer.SetTargetPortForEdgeRouting(targetPort.Location);
                        if(DraggingStraightLine())
                            viewer.DrawRubberLine(TargetPort.Location);
                        else
                            DrawEdgeInteractivelyToPort(TargetPort,loosePolyline);
                    } else {
                        viewer.RemoveTargetPortEdgeRouting();
                        if(DraggingStraightLine())
                            viewer.DrawRubberLine(e);
                        else
                            DrawEdgeInteractivelyToLocation(e);
                    }                   
                }
                e.Handled = true;
            }
        }
        void MouseMoveLiveSelectObjectsForDragging(MsaglMouseEventArgs e) {
            UnselectEverything();
            if (ToggleEntityPredicate(viewer.ModifierKeys, PressedMouseButtons, true) &&
                (viewer.ModifierKeys & ModifierKeys.Shift) != ModifierKeys.Shift)
                SelectEntitiesForDraggingWithRectangle(e);
        }

        void DrawEdgeInteractivelyToLocation(MsaglMouseEventArgs e) {
            DrawEdgeInteractivelyToLocation(viewer.ScreenToSource(e));
        }

        void DrawEdgeInteractivelyToLocation(Point point) {
            viewer.DrawRubberEdge(EdgeGeometry = CalculateEdgeInteractivelyToLocation(point));
        }

        EdgeGeometry CalculateEdgeInteractivelyToLocation(Point location) {
            if(InteractiveEdgeRouter.SourcePort == null)
                InteractiveEdgeRouter.SetSourcePortAndSourceLoosePolyline(SourcePort,sourceLoosePolyline);
            return InteractiveEdgeRouter.RouteEdgeToLocation(location);
        }

        void DrawEdgeInteractivelyToPort(Port targetPortParameter,Polyline portLoosePolyline) {
            viewer.DrawRubberEdge(EdgeGeometry = CalculateEdgeInteractively(targetPortParameter,portLoosePolyline));
        }


        bool DraggingStraightLine() {
            if(viewer.Graph == null)
                return true;
            return 
                InteractiveEdgeRouter != null && InteractiveEdgeRouter.OverlapsDetected;
        }


        EdgeGeometry CalculateEdgeInteractively(Port targetPortParameter,Polyline portLoosePolyline) {
            if(InteractiveEdgeRouter.SourcePort == null)
                InteractiveEdgeRouter.SetSourcePortAndSourceLoosePolyline(SourcePort,sourceLoosePolyline);
            ICurve curve;
            SmoothedPolyline smoothedPolyline = null;
            if(SourceOfInsertedEdge == TargetOfInsertedEdge) {
                curve = new LineSegment(SourcePort.Location,TargetPort.Location);
            } else {
                curve = InteractiveEdgeRouter.RouteEdgeToPort(targetPortParameter,portLoosePolyline,false,
                                                              out smoothedPolyline);
            }
            return new EdgeGeometry { Curve = curve,SmoothedPolyline = smoothedPolyline };
        }
        #endregion

#pragma warning disable 1591
        public void ScaleNodeAroundCenter(IViewerNode viewerNode, double scale) {
#pragma warning restore 1591
            var nodePosition = viewerNode.Node.BoundingBox.Center;
            var scaleMatrix = new PlaneTransformation(scale, 0, 0, 0, scale, 0);
            var translateToOrigin = new PlaneTransformation(1, 0, -nodePosition.X, 0, 1, -nodePosition.Y);
            var translateToNode = new PlaneTransformation(1, 0, nodePosition.X, 0, 1, nodePosition.Y);
            var matrix = translateToNode*scaleMatrix*translateToOrigin;
            viewerNode.Node.GeometryNode.BoundaryCurve=viewerNode.Node.GeometryNode.BoundaryCurve.Transform(matrix);
            viewer.Invalidate(viewerNode);
            foreach (var edge in viewerNode.OutEdges.Concat(viewerNode.InEdges).Concat(viewerNode.SelfEdges))
                RecoverEdge(edge);
        }

        void RecoverEdge(IViewerEdge edge) {
            var curve = edge.Edge.GeometryEdge.UnderlyingPolyline.CreateCurve();
            Arrowheads.TrimSplineAndCalculateArrowheads(edge.Edge.GeometryEdge, curve, true,this.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.KeepOriginalSpline);
            viewer.Invalidate(edge);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public void DetachNode(IViewerNode node) {
            if (node == null) return;
            decoratorRemovalsDict.Remove(node);
            foreach (var edge in Edges(node))
                RemoveObjDraggingDecorations(edge);

        }
    }
}
