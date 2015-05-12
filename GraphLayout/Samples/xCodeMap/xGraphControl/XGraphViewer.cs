using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Miscellaneous;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using Label = Microsoft.Msagl.Drawing.Label;
using ModifierKeys = Microsoft.Msagl.Drawing.ModifierKeys;
using Node = Microsoft.Msagl.Core.Layout.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Size = System.Windows.Size;
using WpfPoint = System.Windows.Point;
using System.Windows.Shapes;

namespace xCodeMap.xGraphControl {
    
    public class XGraphViewer : IViewer {
        public event EventHandler LayoutComplete;

        /*
                readonly DispatcherTimer layoutThreadCheckingTimer = new DispatcherTimer();
        */

        public static readonly RoutedEvent LayoutStartEvent = EventManager.RegisterRoutedEvent("LayoutStart",
                                                                                               RoutingStrategy.Bubble,
                                                                                               typeof (EventHandler),
                                                                                               typeof (XGraphViewer));

        public static readonly RoutedEvent LayoutEndEvent = EventManager.RegisterRoutedEvent("LayoutEnd",
                                                                                             RoutingStrategy.Bubble,
                                                                                             typeof (EventHandler),
                                                                                             typeof (XGraphViewer));

        Canvas graphCanvas = new Canvas();
        Graph drawingGraph;
        object selectedObject;

        readonly Dictionary<DrawingObject, FrameworkElement> graphObjectsToFrameworkElements =
            new Dictionary<DrawingObject, FrameworkElement>();

        DrawingLayoutEditor drawingLayoutEditor;

        GeometryGraph geometryGraphUnderLayout;
        /*
                Thread layoutThread;
        */
        bool needToCalculateLayout = true;
        System.Windows.Point mouseDownPointInSource;
        bool panning;
        IViewerObject objectUnderMouseCursor;
        static double dpiX;
        static int dpiY;

        Dictionary<DrawingObject, IViewerObjectX> drawingObjectsToIViewerObjects =
            new Dictionary<DrawingObject, IViewerObjectX>();

        System.Windows.Shapes.Rectangle rectToFillGraphBackground;
        System.Windows.Shapes.Rectangle rectToFillCanvas;

        #region WPF stuff


        public void BringToHomeView() {
            if (drawingGraph != null && drawingGraph.GeometryGraph != null)
                SetTransformWithScaleAndCenter(FitFactor, GeomGraph.BoundingBox.Center);
        }


        GeometryGraph GeomGraph {
            get {
                return LargeGraphBrowsing
                           ? ((LgLayoutSettings) drawingGraph.LayoutAlgorithmSettings).OGraph
                           : drawingGraph.GeometryGraph;
            }
        }

        /// <summary>
        /// this control needs to be insertes as a child to the user app
        /// </summary>
        public Panel MainPanel {
            get { return graphCanvas; }
        }

        public XGraphViewer() {
            LargeGraphNodeCountThreshold = 0;
            drawingLayoutEditor = new DrawingLayoutEditor(this);

            graphCanvas.SizeChanged += GraphCanvasSizeChanged;
            graphCanvas.MouseLeftButtonDown += GraphCanvasMouseLeftButtonDown;
            graphCanvas.MouseDown += GraphCanvasMouseDown;
            graphCanvas.MouseMove += GraphCanvasMouseMove;
            graphCanvas.MouseUp += GraphCanvasMouseUp;

            graphCanvas.MouseLeftButtonUp += GraphCanvasMouseLeftButtonUp;
            graphCanvas.MouseWheel += GraphCanvasMouseWheel;
            ViewChangeEvent += AdjustBtrectRenderTransform;
            ViewChangeEvent += (a, b) => SetupRoutingTimer();
            GraphChanged += (a, b) => { if (routingTimer != null) routingTimer.Stop();
                                routingTimer = null;
                            };
            LayoutEditingEnabled = true;
        }

        void SetupRoutingTimer() {
            if (routingTimer == null) {
                var lgSettings = (LgLayoutSettings) drawingGraph.LayoutAlgorithmSettings;

                routingTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(lgSettings.ReroutingDelayInSeconds)};
                routingTimer.Tick += (a, b) => {
                                         routingTimer.Stop();
                                         var splineMode = lgSettings.SplineRouting;
                                         lgSettings.SplineRouting = true;
                                         lgSettings.RerouteEdges();
                                         lgSettings.SplineRouting = splineMode;
                                     };
            }

            routingTimer.Stop();
            routingTimer.Start();

        }
        void AdjustBtrectRenderTransform(object sender, EventArgs e) {
            rectToFillCanvas.RenderTransform = (Transform) graphCanvas.RenderTransform.Inverse;
            var parent = (Panel) MainPanel.Parent;
            rectToFillCanvas.Width = parent.ActualWidth;
            rectToFillCanvas.Height = parent.ActualHeight;

        }

        void GraphCanvasMouseUp(object sender, MouseButtonEventArgs e) {
            if (MouseUp != null)
                MouseUp(this, CreateMouseEventArgs(e));
        }

        void GraphCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (MouseDown != null)
                MouseDown(this, CreateMouseEventArgs(e));
        }

        void GraphCanvasMouseWheel(object sender, MouseWheelEventArgs e) {
            if (e.Delta != 0) {
                const double zoomFractionLocal = 0.9;
                var zoomInc = e.Delta < 0 ? zoomFractionLocal : 1.0/zoomFractionLocal;
                ZoomAbout(ZoomFactor*zoomInc, e.GetPosition(graphCanvas));
                e.Handled = true;
            }
        }

        void ZoomAbout(double zoomFactor, WpfPoint centerOfZoom) {
            var scale = zoomFactor*FitFactor;
            var centerOfZoomOnScreen =
                graphCanvas.TransformToAncestor((FrameworkElement) graphCanvas.Parent).Transform(centerOfZoom);
            SetTransform(scale, centerOfZoomOnScreen.X - centerOfZoom.X*scale,
                         centerOfZoomOnScreen.Y + centerOfZoom.Y*scale);
        }

        public DrawingLayoutEditor DrawingLayoutEditor {
            get { return drawingLayoutEditor; }
        }

        void GraphCanvasMouseLeftButtonDown(object sender, MouseEventArgs e) {
            if (MouseDown != null)
                MouseDown(this, CreateMouseEventArgs(e));

            if (e.Handled) return;

            if (!LayoutEditingEnabled || selectedObject == null) {
                panning = true;
                mouseDownPointInSource = Mouse.GetPosition(graphCanvas);
                graphCanvas.CaptureMouse();
            }
        }


        void GraphCanvasMouseMove(object sender, MouseEventArgs e) {
            if (MouseMove != null)
                MouseMove(this, CreateMouseEventArgs(e));

            if (e.Handled) return;

            if (panning)
                Pan(e);
            else {
                // Retrieve the coordinate of the mouse position.
                WpfPoint pt = e.GetPosition(graphCanvas);

                // Expand the hit test area by creating a geometry centered on the hit test point.

                var rect = new Rect(new WpfPoint(pt.X - MouseHitTolerance, pt.Y - MouseHitTolerance),
                                    new WpfPoint(pt.X + MouseHitTolerance, pt.Y + MouseHitTolerance));
                var expandedHitTestArea = new RectangleGeometry(rect);

                // Clear the contents of the list used for hit test results.
                ObjectUnderMouseCursor = null;

                // Set up a callback to receive the hit test result enumeration.
                VisualTreeHelper.HitTest(graphCanvas, null,
                                         MyHitTestResultCallback,
                                         new GeometryHitTestParameters(expandedHitTestArea));
            }
        }

        // Return the result of the hit test to the callback.
        HitTestResultBehavior MyHitTestResultCallback(HitTestResult result) {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;
            object tag = frameworkElement.Tag;
            if (tag != null) {
                //it is a tagged element
                ObjectUnderMouseCursor = (IViewerObjectX) frameworkElement.Tag;
                if (tag is XNode || tag is Label)
                    return HitTestResultBehavior.Stop;
            }
            return HitTestResultBehavior.Continue;
        }




        protected double MouseHitTolerance {
            get { return DpiX*0.05/CurrentScale; }

        }

        void Pan(MouseEventArgs e) {
            var graphCanvasAncestor = (FrameworkElement) graphCanvas.Parent;
            var currentMousePositionOnScreen = e.GetPosition(graphCanvasAncestor);
            var mouseDownPointOnScreenCurrently =
                graphCanvas.TransformToAncestor(graphCanvasAncestor).Transform(mouseDownPointInSource);
            var dx = currentMousePositionOnScreen.X - mouseDownPointOnScreenCurrently.X;
            var dy = currentMousePositionOnScreen.Y - mouseDownPointOnScreenCurrently.Y;
            if (!DeltasAreBigEnough(dx, dy))
                return;
            var rt = (MatrixTransform) graphCanvas.RenderTransform;
            var matrix = rt.Matrix;
            matrix.Translate(dx, dy);

            rt.Matrix = matrix;
            if (ViewChangeEvent != null)
                ViewChangeEvent(null, null);
        }

        static bool DeltasAreBigEnough(double dx, double dy) {
            return Math.Sqrt(dx*dx + dy*dy) > 0.05/3;
        }


        double CurrentScale {
            get { return ((MatrixTransform) graphCanvas.RenderTransform).Matrix.M11; }
        }

        public IViewerNode CreateIViewerNode(DrawingNode drawingNode, Point center, object visualElement) {
            throw new NotImplementedException();
        }

        public IViewerNode CreateIViewerNode(DrawingNode drawingNode) {
            throw new NotImplementedException();
        }

        /*
                void Pan(Point vector) {
            
            
                    graphCanvas.RenderTransform = new MatrixTransform(mouseDownTransform[0, 0], mouseDownTransform[0, 1],
                                                                      mouseDownTransform[1, 0], mouseDownTransform[1, 1],
                                                                      mouseDownTransform[0, 2] +vector.X,
                                                                      mouseDownTransform[1, 2] +vector.Y);            
                }
        */

        internal MsaglMouseEventArgs CreateMouseEventArgs(MouseEventArgs e) {
            return new GvMouseEventArgs(e, this);
        }

        void GraphCanvasMouseLeftButtonUp(object sender, MouseEventArgs e) {
            graphCanvas.ReleaseMouseCapture();
            if (panning)
                panning = false;
        }

        void GraphCanvasSizeChanged(object sender, SizeChangedEventArgs e) {
            if (drawingGraph == null) return;
            // keep the same zoom level
            double oldfit = GetFitFactor(e.PreviousSize);
            double fitNow = FitFactor;
            double scaleFraction = fitNow/oldfit;
            SetTransform(CurrentScale*scaleFraction, CurrentXOffset*scaleFraction, CurrentYOffset*scaleFraction);
        }

        protected double CurrentXOffset {
            get { return ((MatrixTransform) graphCanvas.RenderTransform).Matrix.OffsetX; }
        }

        protected double CurrentYOffset {
            get { return ((MatrixTransform) graphCanvas.RenderTransform).Matrix.OffsetY; }
        }

        protected double ZoomFactor {
            get { return CurrentScale/FitFactor; }
        }

        #endregion

        #region IViewer stuff

        public event EventHandler<EventArgs> ViewChangeEvent;
        public event EventHandler<MsaglMouseEventArgs> MouseDown;
        public event EventHandler<MsaglMouseEventArgs> MouseMove;
        public event EventHandler<MsaglMouseEventArgs> MouseUp;
        public event EventHandler<ObjectUnderMouseCursorChangedEventArgs> ObjectUnderMouseCursorChanged;

        public IViewerObject ObjectUnderMouseCursor {
            get { return objectUnderMouseCursor; }
            private set {
                var old = objectUnderMouseCursor;
                bool callSelectionChanged = objectUnderMouseCursor != value && ObjectUnderMouseCursorChanged != null;

                objectUnderMouseCursor = value;

                if (callSelectionChanged)
                    ObjectUnderMouseCursorChanged(this,
                                                  new ObjectUnderMouseCursorChangedEventArgs(old, objectUnderMouseCursor));
            }
        }

        public void Invalidate(IViewerObject objectToInvalidate) {
            ((IInvalidatable)objectToInvalidate).Invalidate(ZoomFactor * FitFactor);
        }

        public void Invalidate() {
            //todo: is it right to do nothing
        }

        public event EventHandler GraphChanged;

        public ModifierKeys ModifierKeys {
            get {
                switch (Keyboard.Modifiers) {
                    case System.Windows.Input.ModifierKeys.Alt:
                        return ModifierKeys.Alt;
                    case System.Windows.Input.ModifierKeys.Control:
                        return ModifierKeys.Control;
                    case System.Windows.Input.ModifierKeys.None:
                        return ModifierKeys.None;
                    case System.Windows.Input.ModifierKeys.Shift:
                        return ModifierKeys.Shift;
                    case System.Windows.Input.ModifierKeys.Windows:
                        return ModifierKeys.Windows;
                    default:
                        return ModifierKeys.None;
                }
            }
        }

        public Point ScreenToSource(MsaglMouseEventArgs e) {
            return new Point(e.X, e.Y);
        }


        public IEnumerable<IViewerObject> Entities {
            get {
                foreach (var viewerObject in drawingObjectsToIViewerObjects.Values) {
                    yield return viewerObject;
                    var edge = viewerObject as XEdge;
                    if (edge != null)
                        if (edge.XLabel != null)
                            yield return edge.XLabel;
                }
            }
        }

        internal static double DpiXStatic {
            get {
                if (dpiX == 0)
                    GetDpi();
                return dpiX;
            }
        }

        static void GetDpi() {
            int hdcSrc = NativeMethods.GetWindowDC(NativeMethods.GetDesktopWindow());
            //LOGPIXELSX = 88,
            //LOGPIXELSY = 90,
            dpiX = NativeMethods.GetDeviceCaps(hdcSrc, 88);
            dpiY = NativeMethods.GetDeviceCaps(hdcSrc, 90);
            NativeMethods.ReleaseDC(NativeMethods.GetDesktopWindow(), hdcSrc);
        }

        public double DpiX {
            get { return DpiXStatic; }
        }

        public double DpiY {
            get { return DpiYStatic; }
        }

        static double DpiYStatic {
            get {
                if (dpiX == 0)
                    GetDpi();
                return dpiY;
            }
        }

        public void OnDragEnd(IEnumerable<IViewerObject> changedObjects) {
            throw new NotImplementedException();
        }

        public double LineThicknessForEditing { get; private set; }
        public bool LayoutEditingEnabled { get; private set; }
        public bool InsertingEdge { get; set; }

        public void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems) {
            throw new NotImplementedException();
        }

        
        public double UnderlyingPolylineCircleRadius { get; private set; }

        public Graph Graph {
            get { return drawingGraph; }
            set {
                drawingGraph = value;
                ProcessGraph();
            }
        }

        bool LargeGraphBrowsing {
            get { return drawingGraph != null && drawingGraph.NodeCount >= LargeGraphNodeCountThreshold; }
        }

        double desiredPathThicknessInInches=0.016;
        double DesiredPathThicknessInInches {
            get { return desiredPathThicknessInInches; }
            set { desiredPathThicknessInInches = value; }
        }

        double desiredArrowLength = 0.3;

        Dictionary<DrawingObject, IViewerObjectX> vObjectsMapping = new Dictionary<DrawingObject, IViewerObjectX>();
        DispatcherTimer routingTimer;

        public void LoadGraphWithVisuals(Graph g, Dictionary<DrawingObject, IViewerObjectX> mapping)
        {
            vObjectsMapping = mapping;
            Graph = g;
        }

        

        void ProcessGraph() {
            try {
                selectedObject = null;
                if (drawingGraph == null) return;

                graphCanvas.Visibility = Visibility.Hidden; // hide canvas while we lay it out asynchronously.
                ClearGraphViewer();

                // Make nodes and edges

                if (LargeGraphBrowsing) {
                    LayoutEditingEnabled = false;
                    var lgsettings = new LgLayoutSettings(
                        () => new Rectangle(0, 0, graphCanvas.RenderSize.Width, graphCanvas.RenderSize.Height),
                        () => Transform, DpiX, DpiY, ()=> 0.1 * DpiX / CurrentScale /*0.1 inch*/);
                    drawingGraph.LayoutAlgorithmSettings = lgsettings;
                    lgsettings.ViewerChangeTransformAndInvalidateGraph +=OGraphChanged;
                }
                if (NeedToCalculateLayout) {
                    drawingGraph.CreateGeometryGraph(); //forcing the layout recalculation
                    geometryGraphUnderLayout = drawingGraph.GeometryGraph;
                }

                PushGeometryIntoLayoutGraph();
                graphCanvas.RaiseEvent(new RoutedEventArgs(LayoutStartEvent));
                LayoutHelpers.CalculateLayout(geometryGraphUnderLayout, drawingGraph.LayoutAlgorithmSettings);

                TransferLayoutDataToWpf();
            }
            catch
                (Exception e) {
                MessageBox.Show(e.ToString());
            }
        }

        Point GetMousePosition() {
            var pos = Mouse.GetPosition(graphCanvas);
            return new Point(pos.X, pos.Y);
        }

        /// <summary>
        /// oGraph has changed too
        /// </summary>
        void OGraphChanged() {
            var vDrawingEdges = new Set<DrawingEdge>();
            var vDrawingNodes = new Set<DrawingNode>();
            foreach (var dro in drawingObjectsToIViewerObjects.Keys) {
                var n = dro as DrawingNode;
                if (n != null)
                    vDrawingNodes.Insert(n);
                else {
                    var edge = dro as DrawingEdge;
                    if (edge != null)
                        vDrawingEdges.Insert((DrawingEdge) dro);
                }
            }
            var oDrawgingEdges = new Set<DrawingEdge>();
            var oDrawingNodes = new Set<DrawingNode>();
            var oGraph = ((LgLayoutSettings) Graph.LayoutAlgorithmSettings).OGraph;
            foreach (var node in oGraph.Nodes)
                oDrawingNodes.Insert((DrawingNode) node.UserData);
            foreach (var edge in oGraph.Edges)
                oDrawgingEdges.Insert((DrawingEdge) edge.UserData);

            ProcessRemovalsForLg(vDrawingNodes - oDrawingNodes, vDrawingEdges - oDrawgingEdges);
            ProcessAdditions(oDrawingNodes - vDrawingNodes, oDrawgingEdges - vDrawingEdges);
            //    TestCorrectness(oGraph, oDrawingNodes, oDrawgingEdges);
            double pathThickness = GetBorderPathThickness();
            foreach (var viewerObject in Entities) {
                if (viewerObject is XNode)
                {
                    ((XNode)viewerObject).BorderPathThickness = pathThickness;
                }
                else if (viewerObject is XEdge)
                {
                    ((XEdge)viewerObject).StrokePathThickness = pathThickness / 2;
                }
                Invalidate(viewerObject);
            }
            SetBackgroundRectanglePositionAndSize(((LgLayoutSettings) drawingGraph.LayoutAlgorithmSettings).OGraph);
        }

        double GetBorderPathThickness() {
            return DesiredPathThicknessInInches*DpiX/CurrentScale;
        }


        /*
                void TestCorrectness(GeometryGraph oGraph, Set<DrawingNode> oDrawingNodes, Set<DrawingEdge> oDrawgingEdges) {
                    if (Entities.Count() != oGraph.Nodes.Count + oGraph.Edges.Count) {
                        foreach (var newDrawingNode in oDrawingNodes) {
                            if (!drawingObjectsToIViewerObjects.ContainsKey(newDrawingNode))
                                Console.WriteLine();
                        }
                        foreach (var drawingEdge in oDrawgingEdges) {
                            if (!drawingObjectsToIViewerObjects.ContainsKey(drawingEdge))
                                Console.WriteLine();
                        }
                        foreach (var viewerObject in Entities) {
                            if (viewerObject is VEdge) {
                                Debug.Assert(oDrawgingEdges.Contains(viewerObject.DrawingObject));
                            } else {
                                if (viewerObject is VNode) {
                                    Debug.Assert(oDrawingNodes.Contains(viewerObject.DrawingObject));
                                } else {
                                    Debug.Fail("expecting a node or an edge");
                                }
                            }

                        }

                    }
                }
        */


        void ProcessAdditions(Set<DrawingNode> nodesToAdd, Set<DrawingEdge> edgesToAdd) {
            if (nodesToAdd != null)
                ProcessNodeAdditions(nodesToAdd);
            if (edgesToAdd != null)
                ProcessEdgeAdditions(edgesToAdd);
        }

        void ProcessEdgeAdditions(Set<DrawingEdge> edgesToAdd) {
            foreach (var drawingEdge in edgesToAdd)
                CreateEdge(drawingEdge);
        }

        void ProcessNodeAdditions(Set<DrawingNode> nodesToAdd) {
            foreach (var drawingNode in nodesToAdd)
                CreateNode(drawingNode);
        }

        void ProcessRemovalsForLg(Set<DrawingNode> nodesToRemove, Set<DrawingEdge> edgesToRemove) {
            if (nodesToRemove != null)
                foreach (var vNode in nodesToRemove)
                    RemoveVNode(vNode);
            if (edgesToRemove != null)
                foreach (var edge in edgesToRemove)
                    RemoveVEdge(edge);
        }

        void RemoveVEdge(DrawingEdge drawingEdge) {
            //            graph.Edges.Remove(drawingEdge);
            //            var source = drawingEdge.SourceNode;
            //            var target = drawingEdge.TargetNode;
            //            if (source != target) {
            //                source.RemoveOutEdge(drawingEdge);
            //                target.RemoveInEdge(drawingEdge);
            //            }
            //            else
            //                source.RemoveSelfEdge(drawingEdge);

            IViewerObjectX vgedge;
            if (drawingObjectsToIViewerObjects.TryGetValue(drawingEdge, out vgedge)) {
                graphCanvas.Children.Remove(((XEdge) vgedge).Path);
                drawingObjectsToIViewerObjects.Remove(drawingEdge);
            }
        }

        void RemoveVNode(DrawingNode drawingNode) {
            //            foreach (var outEdge in drawingNode.OutEdges) {
            //                graph.Edges.Remove(outEdge);
            //                outEdge.TargetNode.RemoveInEdge(outEdge);
            //            }
            //            foreach (var inEdge in drawingNode.InEdges) {
            //                graph.Edges.Remove(inEdge);
            //                inEdge.SourceNode.RemoveOutEdge(inEdge);
            //            }
            //            var selfEdges = drawingNode.SelfEdges.ToArray();
            //            foreach (var selfEdge in selfEdges)
            //                drawingNode.RemoveSelfEdge(selfEdge);
            //
            //            foreach (var edge in drawingNode.Edges.Concat(selfEdges)) {
            //                IViewerObjectX vedge;
            //                if (!drawingObjectsToIViewerObjects.TryGetValue(edge, out vedge)) continue;
            //                
            //                graphCanvas.Children.Remove(((VEdge)vedge).Path);
            //                drawingObjectsToIViewerObjects.Remove(edge);
            //            }
            var vnode = (XNode) drawingObjectsToIViewerObjects[drawingNode];
            var vgrid = graphObjectsToFrameworkElements[drawingNode];
            graphCanvas.Children.Remove(vgrid);
            graphCanvas.Children.Remove(vnode.BoundaryPath);
            drawingObjectsToIViewerObjects.Remove(drawingNode);
        }


        int LargeGraphNodeCountThreshold { get; set; }


        void ClearGraphViewer() {
            graphCanvas.Children.Clear();
            drawingObjectsToIViewerObjects.Clear();
            graphObjectsToFrameworkElements.Clear();
        }

        /*
                void StartLayoutCalculationInThread() {
                    PushGeometryIntoLayoutGraph();
                    graphCanvas.RaiseEvent(new RoutedEventArgs(LayoutStartEvent));

                    layoutThread =
                        new Thread(
                            () =>
                            LayoutHelpers.CalculateLayout(geometryGraphUnderLayout, graph.LayoutAlgorithmSettings));

                    layoutThread.Start();

                    //the timer monitors the thread and then pushes the data from layout graph to the framework
                    layoutThreadCheckingTimer.IsEnabled = true;
                    layoutThreadCheckingTimer.Tick += LayoutThreadCheckingTimerTick;
                    layoutThreadCheckingTimer.Interval = new TimeSpan((long) 10e6);
                    layoutThreadCheckingTimer.Start();
                }
        */

        /*
                void LayoutThreadCheckingTimerTick(object sender, EventArgs e) {
                    if (layoutThread.IsAlive)
                        return;

                    if (Monitor.TryEnter(layoutThreadCheckingTimer)) {
                        if (layoutThreadCheckingTimer.IsEnabled == false)
                            return; //somehow it is called on more time after stopping and disabling
                        layoutThreadCheckingTimer.Stop();
                        layoutThreadCheckingTimer.IsEnabled = false;

                        TransferLayoutDataToWpf();

                        graphCanvas.RaiseEvent(new RoutedEventArgs(LayoutEndEvent));
                        if (LayoutComplete != null) 
                            LayoutComplete(this, new EventArgs());               
                    }
                }
        */

        void TransferLayoutDataToWpf() {
            PushDataFromLayoutGraphToFrameworkElements();
            graphCanvas.Visibility = Visibility.Visible;
            if (GraphChanged != null)
                GraphChanged(this, null);
            SetInitialTransform();
        }

        void SetInitialTransform() {
            if (drawingGraph == null)
                return;
            var scale = FitFactor;
            var graphCenter = GeomGraph.BoundingBox.Center;
            var vp = new Rectangle(new Point(0, 0),
                                   new Point(graphCanvas.RenderSize.Width, graphCanvas.RenderSize.Height));
            if (LargeGraphBrowsing)
                SetTransformOnViewport(scale, graphCenter, vp);
            else
                SetTransformOnViewportWithoutRaisingViewChangeEvent(scale, graphCenter, vp);
        }

        void SetTransformWithScaleAndCenter(double scale, Point graphCenter) {
            var vp = ClientViewportMappedToGraph;
            SetTransformOnViewport(scale, graphCenter, vp);
        }

        void SetTransformOnViewportWithoutRaisingViewChangeEvent(double scale, Point graphCenter, Rectangle vp) {
            var dx = vp.Width/2 - scale*graphCenter.X;
            var dy = vp.Height/2 + scale*graphCenter.Y;

            SetTransformWithoutRaisingViewChangeEvent(scale, dx, dy);
        }

        void SetTransformOnViewport(double scale, Point graphCenter, Rectangle vp) {
            var dx = vp.Width/2 - scale*graphCenter.X;
            var dy = vp.Height/2 + scale*graphCenter.Y;

            SetTransform(scale, dx, dy);
        }

        public Rectangle ClientViewportMappedToGraph {
            get { 
                var t = Transform.Inverse;
                var p0 = new Point(0, 0);
                var p1 = new Point(graphCanvas.RenderSize.Width, graphCanvas.RenderSize.Height);
                return new Rectangle(t*p0, t*p1);
            }
        }



        void SetTransform(double scale, double dx, double dy) {
            graphCanvas.RenderTransform = new MatrixTransform(scale, 0, 0, -scale, dx, dy);
            if (ViewChangeEvent != null)
                ViewChangeEvent(null, null);
        }

        void SetTransformWithoutRaisingViewChangeEvent(double scale, double dx, double dy) {
            graphCanvas.RenderTransform = new MatrixTransform(scale, 0, 0, -scale, dx, dy);
        }


        double FitFactor {
            get {
                if (drawingGraph == null || GeomGraph.Width == 0 || GeomGraph.Height == 0)
                    return 1;
                var size = graphCanvas.RenderSize;

                return GetFitFactor(size);
            }
        }

        double GetFitFactor(Size rect) {
            return Math.Min(rect.Width/GeomGraph.Width, rect.Height/GeomGraph.Height);
        }

        void PushDataFromLayoutGraphToFrameworkElements() {
            CreateBackgroundRectangleForEventCaptures();
            if (LargeGraphBrowsing) {
                LgLayoutSettings lgSettings = (LgLayoutSettings) drawingGraph.LayoutAlgorithmSettings;
                CreateBackgroundRectangle(lgSettings.OGraph);
                foreach (Node geomNode in lgSettings.OGraph.Nodes)
                    CreateNode((DrawingNode) geomNode.UserData);
            } else {
                CreateBackgroundRectangle(drawingGraph.GeometryGraph);
                CreateNodes();
                CreateEdges();
            }
        }

        void CreateBackgroundRectangleForEventCaptures() {
            var parent = (Panel) MainPanel.Parent;
            rectToFillCanvas = new System.Windows.Shapes.Rectangle();
            Canvas.SetLeft(rectToFillCanvas, 0);
            Canvas.SetTop(rectToFillCanvas, 0);
            rectToFillCanvas.Width = parent.ActualWidth;
            rectToFillCanvas.Height = parent.ActualHeight;

            rectToFillCanvas.Fill = Brushes.Transparent;
            Panel.SetZIndex(rectToFillCanvas, -2);
            graphCanvas.Children.Add(rectToFillCanvas);
        }


        void CreateEdges() {
            foreach (var edge in drawingGraph.Edges)
                CreateEdge(edge);
        }

        void CreateEdge(DrawingEdge edge) {
            if (drawingObjectsToIViewerObjects.ContainsKey(edge)) return;

            XEdge xEdge;
            if (vObjectsMapping.ContainsKey(edge))
            {
                xEdge = (XEdge)vObjectsMapping[edge];
            }
            else
            {
                xEdge = new XEdge(edge);
            }
            drawingObjectsToIViewerObjects[edge] = xEdge;


            if (edge.Source == edge.Target)
                ((XNode) drawingObjectsToIViewerObjects[edge.SourceNode]).selfEdges.Add(xEdge);
            else {
                ((XNode) drawingObjectsToIViewerObjects[edge.SourceNode]).outEdges.Add(xEdge);
                ((XNode) drawingObjectsToIViewerObjects[edge.TargetNode]).inEdges.Add(xEdge);
            }
            drawingObjectsToIViewerObjects[edge] = xEdge;

            FrameworkElement _visuals = xEdge.VisualObject;
            if (_visuals != null)
            {
                graphObjectsToFrameworkElements[edge] = _visuals;
                if (_visuals.Parent == null)
                    graphCanvas.Children.Add(_visuals);
            }
            graphCanvas.Children.Add(xEdge.Path);
        }

        void CreateNodes() {
            foreach (var node in drawingGraph.Nodes)
                CreateNode(node);
        }

        XNode CreateNode(DrawingNode node) {
            if (drawingObjectsToIViewerObjects.ContainsKey(node)) {
                return null;
            }
            XNode xNode;
            if (vObjectsMapping.ContainsKey(node)) {
                xNode = (XNode) vObjectsMapping[node];
            } else {
                xNode = new XNode(node);
            }
            drawingObjectsToIViewerObjects[node] = xNode;

            Path p = xNode.BoundaryPath;

            int insertPosition = 0;
            foreach (UIElement uie in graphCanvas.Children) {
                Path uipath = uie as Path;
                if (uipath != null && (uipath.Tag is XNode)) {
                    XNode xn = (XNode) uipath.Tag;
                    if (IsAncesterAndDecendant(node, xn.Node)) {
                        break;
                    }
                } else {
                    break;
                }
                insertPosition++;
            }
            graphCanvas.Children.Insert(insertPosition, p);

            FrameworkElement _visuals = xNode.VisualObject;
            if (_visuals != null && _visuals.Parent == null) {
                graphCanvas.Children.Add(_visuals);
                graphObjectsToFrameworkElements[node] = _visuals;
                _visuals.MouseLeftButtonDown += (a, b) =>
                {
                    ApiTestForChangingZoomLevels(xNode);
                };
            }
            
            xNode.LgNodeInfo = GetCorrespondingLgNode(xNode.Node);
            xNode.Node.GeometryNode.LayoutChangeEvent += xNode.GeometryNodeBeforeLayoutChangeEvent;

            return xNode;
        }

        bool IsAncesterAndDecendant(DrawingNode node1, DrawingNode node2)
        {
            if (node1 is Subgraph)
            {
                Subgraph subg = (Subgraph)node1;
                if (subg.Nodes.Contains(node2)) return true;
                foreach (Subgraph subsubg in subg.Subgraphs)
                {
                    if (subsubg == node2 || IsAncesterAndDecendant(subsubg, node2))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        LgNodeInfo GetCorrespondingLgNode(DrawingNode node) {
            var lgGraphBrowsingSettings = drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings;
            return lgGraphBrowsingSettings == null
                       ? null
                       : lgGraphBrowsingSettings.GeometryNodesToLgNodeInfos[node.GeometryNode];
        }

        void CreateBackgroundRectangle(GeometryGraph geomGraph) {
            rectToFillGraphBackground = new System.Windows.Shapes.Rectangle();
            SetBackgroundRectanglePositionAndSize(geomGraph);

            rectToFillGraphBackground.Fill = CommonX.BrushFromMsaglColor(drawingGraph.Attr.BackgroundColor);
            Panel.SetZIndex(rectToFillGraphBackground, -1);
            graphCanvas.Children.Add(rectToFillGraphBackground);
        }

        void SetBackgroundRectanglePositionAndSize(GeometryGraph geomGraph) {
            if (rectToFillGraphBackground == null)
                CreateBackgroundRectangle(geomGraph);
            Canvas.SetLeft(rectToFillGraphBackground, geomGraph.Left);
            Canvas.SetTop(rectToFillGraphBackground, geomGraph.Bottom);
            rectToFillGraphBackground.Width = geomGraph.Width;
            rectToFillGraphBackground.Height = geomGraph.Height;
        }


        void PushGeometryIntoLayoutGraph() {
            foreach (
                Node msaglNode in
                    geometryGraphUnderLayout.Nodes.Concat(
                        geometryGraphUnderLayout.RootCluster.AllClustersWideFirstExcludingSelf())) {
                var node = (DrawingNode) msaglNode.UserData;
                if (node.Attr.Shape == Microsoft.Msagl.Drawing.Shape.Box)
                    node.Attr.XRadius = node.Attr.YRadius = 4;//TODO: what is the right value here? Levnach

                msaglNode.BoundaryCurve = GetNodeBoundaryCurve(node);
                //AssignLabelWidthHeight(msaglNode, msaglNode.UserData as DrawingObject);
            }

            foreach (var msaglEdge in geometryGraphUnderLayout.Edges) {
                var drawingEdge = (DrawingEdge) msaglEdge.UserData;
                AssignLabelWidthHeight(msaglEdge, drawingEdge);
            }
        }

        void AssignLabelWidthHeight(Microsoft.Msagl.Core.Layout.ILabeledObject labeledGeomObj,
                                    DrawingObject drawingObj) {
            if (graphObjectsToFrameworkElements.ContainsKey(drawingObj)) {
                FrameworkElement fe = graphObjectsToFrameworkElements[drawingObj];
                labeledGeomObj.Label.Width = fe.Width;
                labeledGeomObj.Label.Height = fe.Height;
            }
        }


        ICurve GetNodeBoundaryCurve(DrawingNode node) {
            double width = 0;
            double height = 0;

            if (graphObjectsToFrameworkElements.ContainsKey(node)) {
                FrameworkElement fe = graphObjectsToFrameworkElements[node];
                if (fe == null)
                    return CurveFactory.CreateRectangleWithRoundedCorners(10, 10, 1, 1, new Point());
                width = fe.Width + 2*node.Attr.LabelMargin;
                height = fe.Height + 2*node.Attr.LabelMargin;
            } else
                return CurveFactory.CreateRectangleWithRoundedCorners(10, 10, 5, 5, new Point());


            if (width < drawingGraph.Attr.MinNodeWidth*2)
                width = drawingGraph.Attr.MinNodeWidth*2;
            if (height < drawingGraph.Attr.MinNodeHeight*2)
                height = drawingGraph.Attr.MinNodeHeight*2;

            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }

        public void DrawRubberLine(MsaglMouseEventArgs args) {
            throw new NotImplementedException();
        }

        public void StopDrawingRubberLine() {
            throw new NotImplementedException();
        }

        public void AddEdge(IViewerEdge edge, bool registerForUndo) {
            throw new NotImplementedException();
        }

        public IViewerEdge CreateEdgeWithGivenGeometry(DrawingEdge drawingEdge) {
            throw new NotImplementedException();
        }

        public void AddNode(IViewerNode node, bool registerForUndo) {
            throw new NotImplementedException();
        }

        public void RemoveEdge(IViewerEdge edge, bool registerForUndo) {
            throw new NotImplementedException();
        }

        public void RemoveNode(IViewerNode node, bool registerForUndo) {
            throw new NotImplementedException();
        }

        public IViewerEdge RouteEdge(DrawingEdge edgeToRoute) {
            throw new NotImplementedException();
        }

        public IViewerGraph ViewerGraph { get; private set; }
        public double ArrowheadLength { get; private set; }
        public void SetSourcePortForEdgeRouting(Point portLocation) {
            throw new NotImplementedException();
        }

        public void SetTargetPortForEdgeRouting(Point portLocation) {
            throw new NotImplementedException();
        }

        public void RemoveSourcePortEdgeRouting() {
            throw new NotImplementedException();
        }

        public void RemoveTargetPortEdgeRouting() {
            throw new NotImplementedException();
        }

        public void DrawRubberEdge(EdgeGeometry edgeGeometry) {
            throw new NotImplementedException();
        }

        public void StopDrawingRubberEdge() {
            throw new NotImplementedException();
        }


        public PlaneTransformation Transform {
            get {
                var mt = graphCanvas.RenderTransform as MatrixTransform;
                if (mt == null)
                    return PlaneTransformation.UnitTransformation;
                var m = mt.Matrix;
                return new PlaneTransformation(m.M11, m.M12, m.OffsetX, m.M21, m.M22, m.OffsetY);
            }
            set {
                SetRenderTransformWithoutRaisingEvents(value);

                if (ViewChangeEvent != null)
                    ViewChangeEvent(null, null);
            }
        }

        void SetRenderTransformWithoutRaisingEvents(PlaneTransformation value) {
            graphCanvas.RenderTransform = new MatrixTransform(value[0, 0], value[0, 1], value[1, 0], value[1, 1],
                                                              value[0, 2],
                value[1, 2]);
        }


        double IViewer.CurrentScale {
            get { return CurrentScale; }
        }

        public bool NeedToCalculateLayout {
            get { return needToCalculateLayout; }
            set { needToCalculateLayout = value; }
        }

        public void DrawRubberLine(Point point) {
            throw new NotImplementedException();
        }

        public void StartDrawingRubberLine(Point startingPoint) {
            throw new NotImplementedException();
        }

        #endregion

        void ApiTestForChangingZoomLevels(XNode nodeToShow) {
            var assignedZoomLevel = 0;
            var settingsl = (LgLayoutSettings) Graph.LayoutAlgorithmSettings;
            var lgNodeInfo = nodeToShow.LgNodeInfo;
            lgNodeInfo.ZoomLevel = assignedZoomLevel;
            foreach (var edge in lgNodeInfo.GeometryNode.Edges) {
                var lgEdgeInfo = settingsl.GeometryEdgesToLgEdgeInfos[edge];
                lgEdgeInfo.ZoomLevel = assignedZoomLevel;
            }
            ViewChangeEvent(null, null);
        }

    }
}