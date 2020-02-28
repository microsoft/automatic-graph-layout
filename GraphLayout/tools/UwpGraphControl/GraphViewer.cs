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
using System.ComponentModel;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Miscellaneous.LayoutEditing;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using ILabeledObject = Microsoft.Msagl.Drawing.ILabeledObject;
using Label = Microsoft.Msagl.Drawing.Label;
using ModifierKeys = Microsoft.Msagl.Drawing.ModifierKeys;
using Node = Microsoft.Msagl.Core.Layout.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Size = Windows.Foundation.Size;
using WpfPoint = Windows.Foundation.Point;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Ellipse = Windows.UI.Xaml.Shapes.Ellipse;
using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using FrameworkElement = Windows.UI.Xaml.FrameworkElement;
using Canvas = Windows.UI.Xaml.Controls.Canvas;
using PointerRoutedEventArgs = Windows.UI.Xaml.Input.PointerRoutedEventArgs;
using Visibility = Windows.UI.Xaml.Visibility;
using MatrixTransform = Windows.UI.Xaml.Media.MatrixTransform;
using SolidColorBrush = Windows.UI.Xaml.Media.SolidColorBrush;
using Colors = Windows.UI.Colors;
using Panel = Windows.UI.Xaml.Controls.Canvas;
using TextBlock = Windows.UI.Xaml.Controls.TextBlock;
using Path = Windows.UI.Xaml.Shapes.Path;


namespace Microsoft.Msagl.Viewers.Uwp {
    public class GraphViewer : Windows.UI.Xaml.Controls.ContentControl, IViewer {
        Windows.UI.Xaml.Shapes.Path _targetArrowheadPathForRubberEdge;

        Windows.UI.Xaml.Shapes.Path _rubberEdgePath;
        Windows.UI.Xaml.Shapes.Path _rubberLinePath;
        Point _sourcePortLocationForEdgeRouting;
        //WpfPoint _objectUnderMouseDetectionLocation;
        CancelToken _cancelToken = new CancelToken();
        BackgroundWorker _backgroundWorker;
        Point _mouseDownPositionInGraph;
        bool _mouseDownPositionInGraph_initialized;

        Ellipse _sourcePortCircle;
        protected Ellipse TargetPortCircle { get; set; }

        WpfPoint _objectUnderMouseDetectionLocation;
        public event EventHandler LayoutStarted;
        public event EventHandler LayoutComplete;

        /*
                readonly DispatcherTimer layoutThreadCheckingTimer = new DispatcherTimer();
        */

        /// <summary>
        /// if set to true will layout in a task
        /// </summary>
        public bool RunLayoutAsync;

        readonly Windows.UI.Xaml.Controls.Canvas _graphCanvas = new Windows.UI.Xaml.Controls.Canvas();
        Graph _drawingGraph;

        readonly Dictionary<DrawingObject, FrameworkElement> drawingObjectsToFrameworkElements =
            new Dictionary<DrawingObject, FrameworkElement>();

        readonly LayoutEditor layoutEditor;


        GeometryGraph geometryGraphUnderLayout;
        /*
                Thread layoutThread;
        */
        bool needToCalculateLayout = true;


        object _objectUnderMouseCursor;

        static double _dpiX;
        static double _dpiY;

        readonly Dictionary<DrawingObject, IViewerObject> drawingObjectsToIViewerObjects =
            new Dictionary<DrawingObject, IViewerObject>();

        Windows.UI.Xaml.FrameworkElement _rectToFillGraphBackground;
        Windows.UI.Xaml.Shapes.Rectangle _rectToFillCanvas;


        GeometryGraph GeomGraph {
            get { return _drawingGraph.GeometryGraph; }
        }

        /// <summary>
        /// the canvas to draw the graph
        /// </summary>
        public Windows.UI.Xaml.Controls.Canvas GraphCanvas {
            get { return _graphCanvas; }
        }

        public GraphViewer() {
            //LargeGraphNodeCountThreshold = 0;
            layoutEditor = new LayoutEditor(this);
            Content = _graphCanvas;
            SizeChanged += GraphCanvasSizeChanged;
            PointerPressed += (s, e) => {
                if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse) {
                    var properties = e.GetCurrentPoint(this).Properties;
                    if (properties.IsLeftButtonPressed)
                        GraphCanvasMouseLeftButtonDown(s, e);
                    else if (properties.IsRightButtonPressed)
                        GraphCanvasRightMouseDown(s, e);
                }
            };
            PointerMoved += GraphCanvasMouseMove;

            PointerReleased += (s, e) => {
                if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse) {
                    var properties = e.GetCurrentPoint(this).Properties;
                    if (properties.PointerUpdateKind == Windows.UI.Input.PointerUpdateKind.LeftButtonReleased)
                        GraphCanvasMouseLeftButtonUp(s, e);
                    else if (properties.PointerUpdateKind == Windows.UI.Input.PointerUpdateKind.RightButtonReleased)
                        GraphCanvasRightMouseUp(s, e);
                }

            };
            _graphCanvas.PointerWheelChanged += GraphCanvasMouseWheel;

            ViewChangeEvent += AdjustBtrectRenderTransform;

            LayoutEditingEnabled = true;
            clickCounter = new ClickCounter(() => lastPosition);
            clickCounter.Elapsed += ClickCounterElapsed;
        }

        #region WPF stuff

        /// <summary>
        /// adds the main panel of the viewer to the children of the parent
        /// </summary>
        /// <param name="panel"></param>
        public void BindToPanel(Windows.UI.Xaml.Controls.Panel panel) {
            panel.Children.Add(GraphCanvas);
            GraphCanvas.UpdateLayout();
        }
        void ClickCounterElapsed(object sender, EventArgs e) {
            var vedge = clickCounter.ClickedObject as VEdge;
            if (vedge != null) {
                if (clickCounter.UpCount == clickCounter.DownCount && clickCounter.UpCount == 1)
                    HandleClickForEdge(vedge);
            }
            clickCounter.ClickedObject = null;
        }
        void AdjustBtrectRenderTransform(object sender, EventArgs e) {
            if (_rectToFillCanvas == null)
                return;
            _rectToFillCanvas.RenderTransform = (Windows.UI.Xaml.Media.Transform)_graphCanvas.RenderTransform.Inverse;
            var parent = (Windows.UI.Xaml.Controls.Panel)GraphCanvas.Parent;
            _rectToFillCanvas.Width = parent.ActualWidth;
            _rectToFillCanvas.Height = parent.ActualHeight;
        }

        void GraphCanvasRightMouseUp(object sender, PointerRoutedEventArgs e) {
            OnMouseUp(e);
        }

        void HandleClickForEdge(VEdge vEdge) {
            //todo : add a hook
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings != null) {
                var lgEi = lgSettings.GeometryEdgesToLgEdgeInfos[vEdge.Edge.GeometryEdge];
                lgEi.SlidingZoomLevel = lgEi.SlidingZoomLevel != 0 ? 0 : double.PositiveInfinity;

                ViewChangeEvent(null, null);
            }
        }
        void GraphCanvasRightMouseDown(object sender, PointerRoutedEventArgs e) {
            MouseDown?.Invoke(this, CreateMouseEventArgs(e, 1));
        }

        void GraphCanvasMouseWheel(object sender, PointerRoutedEventArgs e) {
            var pointerPoint = e.GetCurrentPoint(this);
            var delta = pointerPoint.Properties.MouseWheelDelta;
            if (delta != 0) {
                const double zoomFractionLocal = 0.9;
                var zoomInc = delta < 0 ? zoomFractionLocal : 1.0 / zoomFractionLocal;
                ZoomAbout(ZoomFactor * zoomInc, pointerPoint.Position);
                e.Handled = true;
            }
        }

        /// <summary>
        /// keeps centerOfZoom pinned to the screen and changes the scale by zoomFactor
        /// </summary>
        /// <param name="zoomFactor"></param>
        /// <param name="centerOfZoom"></param>
        public void ZoomAbout(double zoomFactor, WpfPoint centerOfZoom) {
            var scale = zoomFactor * FitFactor;
            var centerOfZoomOnScreen =
                _graphCanvas.TransformToAncestor((FrameworkElement)_graphCanvas.Parent).Transform(centerOfZoom);
            SetTransform(scale, centerOfZoomOnScreen.X - centerOfZoom.X * scale,
                         centerOfZoomOnScreen.Y + centerOfZoom.Y * scale);
        }

        public LayoutEditor LayoutEditor {
            get { return layoutEditor; }
        }

        void GraphCanvasMouseLeftButtonDown(object sender, PointerRoutedEventArgs e) {
            clickCounter.AddMouseDown(_objectUnderMouseCursor);
            MouseDown?.Invoke(this, CreateMouseEventArgs(e, 1));

            if (e.Handled) return;
            _mouseDownPositionInGraph = Common.MsaglPoint(e.GetCurrentPoint(_graphCanvas).Position);
            _mouseDownPositionInGraph_initialized = true;
        }

        WpfPoint lastPosition;
        void GraphCanvasMouseMove(object sender, PointerRoutedEventArgs e) {
            lastPosition = e.GetCurrentPoint(this).Position;
            MouseMove?.Invoke(this, CreateMouseEventArgs(e, 0));

            if (e.Handled) return;

            if (Mouse.LeftButton == MouseButtonState.Pressed && (!LayoutEditingEnabled || _objectUnderMouseCursor == null)) {
                if (!_mouseDownPositionInGraph_initialized) {
                    _mouseDownPositionInGraph = Common.MsaglPoint(e.GetCurrentPoint(_graphCanvas).Position);
                    _mouseDownPositionInGraph_initialized = true;
                }

                Pan(e);
            }
            else {
                // Retrieve the coordinate of the mouse position.
                WpfPoint mouseLocation = e.GetCurrentPoint(_graphCanvas).Position;
                // Clear the contents of the list used for hit test results.
                ObjectUnderMouseCursor = null;
                UpdateWithWpfHitObjectUnderMouseOnLocation(mouseLocation, MyHitTestResultCallback);
            }
        }

        void UpdateWithWpfHitObjectUnderMouseOnLocation(WpfPoint pt, HitTestResultCallback hitTestResultCallback) {
            _objectUnderMouseDetectionLocation = pt;
            // Expand the hit test area by creating a geometry centered on the hit test point.
            var rect = new Rect(new WpfPoint(pt.X - MouseHitTolerance, pt.Y - MouseHitTolerance),
                new WpfPoint(pt.X + MouseHitTolerance, pt.Y + MouseHitTolerance));
            var expandedHitTestArea = new RectangleGeometry(rect);
            // Set up a callback to receive the hit test result enumeration.
            VisualTreeHelper.HitTest(_graphCanvas, null,
                hitTestResultCallback,
                new GeometryHitTestParameters(expandedHitTestArea));
        }


        // Return the result of the hit test to the callback.
        HitTestResultBehavior MyHitTestResultCallback(HitTestResult result) {
            var frameworkElement = result.VisualHit as FrameworkElement;
            new Canvas
            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;
            if (frameworkElement.Tag == null)
                return HitTestResultBehavior.Continue;
            var tag = frameworkElement.Tag;
            var iviewerObj = tag as IViewerObject;
            if (iviewerObj != null && iviewerObj.DrawingObject.IsVisible) {
                if (ObjectUnderMouseCursor is IViewerEdge || ObjectUnderMouseCursor == null
                    ||
                    Panel.GetZIndex(frameworkElement) >
                    Panel.GetZIndex(GetFrameworkElementFromIViewerObject(ObjectUnderMouseCursor)))
                    //always overwrite an edge or take the one with greater zIndex
                    ObjectUnderMouseCursor = iviewerObj;
            }
            return HitTestResultBehavior.Continue;
        }


        Windows.UI.Xaml.FrameworkElement GetFrameworkElementFromIViewerObject(IViewerObject viewerObject) {
            Windows.UI.Xaml.FrameworkElement ret;

            var vNode = viewerObject as VNode;
            if (vNode != null) ret = vNode.FrameworkElementOfNodeForLabel ?? vNode.BoundaryPath;
            else {
                var vLabel = viewerObject as VLabel;
                if (vLabel != null) ret = vLabel.FrameworkElement;
                else {
                    var vEdge = viewerObject as VEdge;
                    if (vEdge != null) ret = vEdge.CurvePath;
                    else {
                        throw new InvalidOperationException(
#if DEBUG
                            "Unexpected object type in GraphViewer"
#endif
                            );
                    }
                }
            }
            if (ret == null)
                throw new InvalidOperationException(
#if DEBUG
                    "did not find a framework element!"
#endif
                    );

            return ret;
        }

        // Return the result of the hit test to the callback.
        HitTestResultBehavior MyHitTestResultCallbackWithNoCallbacksToTheUser(HitTestResult result) {
            var frameworkElement = result.VisualHit as Windows.UI.Xaml.FrameworkElement;

            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;
            object tag = frameworkElement.Tag;
            if (tag != null) {
                //it is a tagged element
                var ivo = tag as IViewerObject;
                if (ivo != null) {
                    if (ivo.DrawingObject.IsVisible) {
                        _objectUnderMouseCursor = ivo;
                        if (tag is VNode || tag is Label)
                            return HitTestResultBehavior.Stop;
                    }
                }
                else {
                    System.Diagnostics.Debug.Assert(tag is Rail);
                    _objectUnderMouseCursor = tag;
                    return HitTestResultBehavior.Stop;
                }
            }

            return HitTestResultBehavior.Continue;
        }


        protected double MouseHitTolerance {
            get { return (0.05) * DpiX / CurrentScale; }

        }
        /// <summary>
        /// this function pins the sourcePoint to screenPoint
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="sourcePoint"></param>
        void SetTransformFromTwoPoints(WpfPoint screenPoint, Point sourcePoint) {
            var scale = CurrentScale;
            SetTransform(scale, screenPoint.X - scale * sourcePoint.X, screenPoint.Y + scale * sourcePoint.Y);
        }
        /// <summary>
        /// Moves the point to the center of the viewport
        /// </summary>
        /// <param name="sourcePoint"></param>
        public void PointToCenter(Point sourcePoint) {
            WpfPoint center = new WpfPoint(_graphCanvas.RenderSize.Width / 2, _graphCanvas.RenderSize.Height / 2);
            SetTransformFromTwoPoints(center, sourcePoint);
        }
        public void NodeToCenterWithScale(Drawing.Node node, double scale) {
            if (node.GeometryNode == null) return;
            var screenPoint = new WpfPoint(_graphCanvas.RenderSize.Width / 2, _graphCanvas.RenderSize.Height / 2);
            var sourcePoint = node.BoundingBox.Center;
            SetTransform(scale, screenPoint.X - scale * sourcePoint.X, screenPoint.Y + scale * sourcePoint.Y);
        }

        public void NodeToCenter(Drawing.Node node) {
            if (node.GeometryNode == null) return;
            PointToCenter(node.GeometryNode.Center);
        }

        void Pan(PointerRoutedEventArgs e) {
            if (UnderLayout)
                return;

            if (!_graphCanvas.IsMouseCaptured)
                _graphCanvas.CaptureMouse();


            SetTransformFromTwoPoints(e.GetCurrentPoint((FrameworkElement)_graphCanvas.Parent).Position,
                    _mouseDownPositionInGraph);

            if (ViewChangeEvent != null)
                ViewChangeEvent(null, null);
        }


        public double CurrentScale {
            get { return ((MatrixTransform)_graphCanvas.RenderTransform).Matrix.M11; }
        }


        internal MsaglMouseEventArgs CreateMouseEventArgs(PointerRoutedEventArgs e, int clicks) {
            return new GvMouseEventArgs(e, this, clicks);
        }

        void GraphCanvasMouseLeftButtonUp(object sender, PointerRoutedEventArgs e) {
            OnMouseUp(e);
            clickCounter.AddMouseUp();
            if (_graphCanvas.IsMouseCaptured) {
                e.Handled = true;
                _graphCanvas.ReleaseMouseCapture();
            }
        }

        void OnMouseUp(PointerRoutedEventArgs e) {
            MouseUp?.Invoke(this, CreateMouseEventArgs(e, 0));
        }

        void GraphCanvasSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e) {
            if (_drawingGraph == null) return;
            // keep the same zoom level
            double oldfit = GetFitFactor(e.PreviousSize);
            double fitNow = FitFactor;
            double scaleFraction = fitNow / oldfit;
            SetTransform(CurrentScale * scaleFraction, CurrentXOffset * scaleFraction, CurrentYOffset * scaleFraction);
        }

        protected double CurrentXOffset {
            get { return ((MatrixTransform)_graphCanvas.RenderTransform).Matrix.OffsetX; }
        }

        protected double CurrentYOffset {
            get { return ((MatrixTransform)_graphCanvas.RenderTransform).Matrix.OffsetY; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double ZoomFactor {
            get { return CurrentScale / FitFactor; }
        }

        #endregion

        #region IViewer stuff

        public event EventHandler<EventArgs> ViewChangeEvent;
        public event EventHandler<MsaglMouseEventArgs> MouseDown;
        public event EventHandler<MsaglMouseEventArgs> MouseMove;
        public event EventHandler<MsaglMouseEventArgs> MouseUp;
        public event EventHandler<ObjectUnderMouseCursorChangedEventArgs> ObjectUnderMouseCursorChanged;

        public IViewerObject ObjectUnderMouseCursor {
            get {
                // this function can bring a stale object
                var location = Mouse.GetPosition(_graphCanvas);
                if (!(_objectUnderMouseDetectionLocation == location))
                    UpdateWithWpfHitObjectUnderMouseOnLocation(location, MyHitTestResultCallbackWithNoCallbacksToTheUser);
                return GetIViewerObjectFromObjectUnderCursor(_objectUnderMouseCursor);
            }
            private set {
                var old = _objectUnderMouseCursor;
                bool callSelectionChanged = _objectUnderMouseCursor != value && ObjectUnderMouseCursorChanged != null;

                _objectUnderMouseCursor = value;

                if (callSelectionChanged)
                    ObjectUnderMouseCursorChanged(this,
                                                  new ObjectUnderMouseCursorChangedEventArgs(
                                                      GetIViewerObjectFromObjectUnderCursor(old),
                                                      GetIViewerObjectFromObjectUnderCursor(_objectUnderMouseCursor)));
            }
        }

        IViewerObject GetIViewerObjectFromObjectUnderCursor(object obj) {
            if (obj == null)
                return null;
            return obj as IViewerObject;
        }

        public void Invalidate(IViewerObject objectToInvalidate) {
            ((IInvalidatable)objectToInvalidate).Invalidate();
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
            var p = new Point(e.X, e.Y);
            var m = Transform.Inverse;
            return m * p;
        }


        public IEnumerable<IViewerObject> Entities {
            get {
                foreach (var viewerObject in drawingObjectsToIViewerObjects.Values) {
                    yield return viewerObject;
                    var edge = viewerObject as VEdge;
                    if (edge != null)
                        if (edge.VLabel != null)
                            yield return edge.VLabel;
                }
            }
        }

        internal static double DpiXStatic {
            get {
                if (_dpiX == 0)
                    GetDpi();
                return _dpiX;
            }
        }

        static void GetDpi() {
            var di = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
            _dpiX = di.RawDpiX;
            _dpiY = di.RawDpiY;
        }

        public double DpiX {
            get { return DpiXStatic; }
        }

        public double DpiY {
            get { return DpiYStatic; }
        }

        static double DpiYStatic {
            get {
                if (_dpiX == 0)
                    GetDpi();
                return _dpiY;
            }
        }

        public void OnDragEnd(IEnumerable<IViewerObject> changedObjects) {
            throw new NotImplementedException();
        }

        public double LineThicknessForEditing { get; set; }

        /// <summary>
        /// the layout editing with the mouse is enabled if and only if this field is set to false
        /// </summary>
        public bool LayoutEditingEnabled { get; set; }

        public bool InsertingEdge { get; set; }

        public void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems) {
            var contextMenu = new ContextMenu();
            foreach (var pair in menuItems)
                contextMenu.Items.Add(CreateMenuItem(pair.Item1, pair.Item2));
            contextMenu.Closed += ContextMenuClosed;
            ContextMenuService.SetContextMenu(_graphCanvas, contextMenu);
        }

        void ContextMenuClosed(object sender, RoutedEventArgs e) {
            ContextMenuService.SetContextMenu(_graphCanvas, null);
        }

        public static object CreateMenuItem(string title, VoidDelegate voidVoidDelegate) {
            var menuItem = new MenuItem { Header = title };
            menuItem.Click += (RoutedEventHandler)(delegate { voidVoidDelegate(); });
            return menuItem;
        }

        public double UnderlyingPolylineCircleRadius {
            get { return 0.1 * DpiX / CurrentScale; }
        }

        public Graph Graph {
            get { return _drawingGraph; }
            set {
                _drawingGraph = value;
                if (_drawingGraph != null)
                    Console.WriteLine("starting processing a graph with {0} nodes and {1} edges", _drawingGraph.NodeCount,
                        _drawingGraph.EdgeCount);
                ProcessGraph();
            }
        }


        const double DesiredPathThicknessInInches = 0.008;

        readonly Dictionary<DrawingObject, Func<DrawingObject, FrameworkElement>> registeredCreators =
            new Dictionary<DrawingObject, Func<DrawingObject, FrameworkElement>>();

        readonly ClickCounter clickCounter;
        public string MsaglFileToSave;

        double GetBorderPathThickness() {
            return DesiredPathThicknessInInches * DpiX;
        }

        readonly Object _processGraphLock = new object();
        void ProcessGraph() {
            lock (_processGraphLock) {
                ProcessGraphUnderLock();
            }
        }

        void ProcessGraphUnderLock() {
            try {
                LayoutStarted?.Invoke(null, null);

                CancelToken = new CancelToken();

                if (_drawingGraph == null) return;

                HideCanvas();
                ClearGraphViewer();
                CreateFrameworkElementsForLabelsOnly();
                if (NeedToCalculateLayout) {
                    _drawingGraph.CreateGeometryGraph(); //forcing the layout recalculation
                    if (_graphCanvas.Dispatcher.CheckAccess())
                        PopulateGeometryOfGeometryGraph();
                    else
                        _graphCanvas.Dispatcher.Invoke(PopulateGeometryOfGeometryGraph);
                }

                geometryGraphUnderLayout = _drawingGraph.GeometryGraph;
                if (RunLayoutAsync)
                    SetUpBackgrounWorkerAndRunAsync();
                else
                    RunLayoutInUIThread();
            }
            catch { }
        }

        void RunLayoutInUIThread() {
            LayoutGraph();
            PostLayoutStep();
            if (LayoutComplete != null)
                LayoutComplete(null, null);
        }

        void SetUpBackgrounWorkerAndRunAsync() {
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += (a, b) => LayoutGraph();
            _backgroundWorker.RunWorkerCompleted += (sender, args) => {
                if (args.Error != null) {
                    MessageBox.Show(args.Error.ToString());
                    ClearGraphViewer();
                }
                else if (CancelToken.Canceled) {
                    ClearGraphViewer();
                }
                else {
                    if (_graphCanvas.Dispatcher.CheckAccess())
                        PostLayoutStep();
                    else
                        _graphCanvas.Dispatcher.Invoke(PostLayoutStep);
                }
                _backgroundWorker = null; //this will signal that we are not under layout anymore          
                if (LayoutComplete != null)
                    LayoutComplete(null, null);
            };
            _backgroundWorker.RunWorkerAsync();
        }

        void HideCanvas() {
            if (_graphCanvas.Dispatcher.CheckAccess())
                _graphCanvas.Visibility = Visibility.Hidden; // hide canvas while we lay it out asynchronously.
            else
                _graphCanvas.Dispatcher.Invoke(() => _graphCanvas.Visibility = Visibility.Hidden);
        }


        void LayoutGraph() {
            if (NeedToCalculateLayout) {
                try {
                    LayoutHelpers.CalculateLayout(geometryGraphUnderLayout, _drawingGraph.LayoutAlgorithmSettings,
                                                  CancelToken);
                    if (MsaglFileToSave != null) {
                        _drawingGraph.Write(MsaglFileToSave);
                        Console.WriteLine("saved into {0}", MsaglFileToSave);
                        Environment.Exit(0);
                    }
                }
                catch (OperationCanceledException) {
                    //swallow this exception
                }
            }
        }

        void PostLayoutStep() {
            _graphCanvas.Visibility = Visibility.Visible;
            PushDataFromLayoutGraphToFrameworkElements();
            _backgroundWorker = null; //this will signal that we are not under layout anymore
            if (GraphChanged != null)
                GraphChanged(this, null);

            SetInitialTransform();
        }
        /// <summary>
        /// creates a viewer node
        /// </summary>
        /// <param name="drawingNode"></param>
        /// <returns></returns>
        public IViewerNode CreateIViewerNode(Drawing.Node drawingNode) {
            var frameworkElement = CreateTextBlockForDrawingObj(drawingNode);
            var width = frameworkElement.Width + 2 * drawingNode.Attr.LabelMargin;
            var height = frameworkElement.Height + 2 * drawingNode.Attr.LabelMargin;
            var bc = NodeBoundaryCurves.GetNodeBoundaryCurve(drawingNode, width, height);
            drawingNode.GeometryNode = new Node(bc, drawingNode);
            var vNode = CreateVNode(drawingNode);
            layoutEditor.AttachLayoutChangeEvent(vNode);
            return vNode;
        }

        void ClearGraphViewer() {
            ClearGraphCanvasChildren();

            drawingObjectsToIViewerObjects.Clear();
            drawingObjectsToFrameworkElements.Clear();
        }

        void ClearGraphCanvasChildren() {
            if (_graphCanvas.Dispatcher.CheckAccess())
                _graphCanvas.Children.Clear();
            else _graphCanvas.Dispatcher.Invoke(() => _graphCanvas.Children.Clear());
        }

        /// <summary>
        /// zooms to the default view
        /// </summary>
        public void SetInitialTransform() {
            if (_drawingGraph == null || GeomGraph == null) return;

            var scale = FitFactor;
            var graphCenter = GeomGraph.BoundingBox.Center;
            var vp = new Rectangle(new Point(0, 0),
                                   new Point(_graphCanvas.RenderSize.Width, _graphCanvas.RenderSize.Height));

            SetTransformOnViewportWithoutRaisingViewChangeEvent(scale, graphCenter, vp);
        }

        //public Image DrawImage(string fileName) {
        //    var ltrans = _graphCanvas.LayoutTransform;
        //    var rtrans = _graphCanvas.RenderTransform;
        //    _graphCanvas.LayoutTransform = null;
        //    _graphCanvas.RenderTransform = null;
        //    var renderSize = _graphCanvas.RenderSize;

        //    double scale = FitFactor;
        //    int w = (int)(GeomGraph.Width * scale);
        //    int h = (int)(GeomGraph.Height * scale);

        //    SetTransformOnViewportWithoutRaisingViewChangeEvent(scale, GeomGraph.BoundingBox.Center, new Rectangle(0, 0, w, h));

        //    Size size = new Size(w, h);
        //    // Measure and arrange the surface
        //    // VERY IMPORTANT
        //    _graphCanvas.Measure(size);
        //    _graphCanvas.Arrange(new Rect(size));

        //    foreach (var node in _drawingGraph.Nodes.Concat(_drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf())) {
        //        IViewerObject o;
        //        if (drawingObjectsToIViewerObjects.TryGetValue(node, out o)) {
        //            ((VNode)o).Invalidate();
        //        }
        //    }

        //    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(w, h, DpiX, DpiY, PixelFormats.Pbgra32);
        //    renderBitmap.Render(_graphCanvas);

        //    if (fileName != null)
        //        // Create a file stream for saving image
        //        using (System.IO.FileStream outStream = new System.IO.FileStream(fileName, System.IO.FileMode.Create)) {
        //            // Use png encoder for our data
        //            PngBitmapEncoder encoder = new PngBitmapEncoder();
        //            // push the rendered bitmap to it
        //            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
        //            // save the data to the stream
        //            encoder.Save(outStream);
        //        }

        //    _graphCanvas.LayoutTransform = ltrans;
        //    _graphCanvas.RenderTransform = rtrans;
        //    _graphCanvas.Measure(renderSize);
        //    _graphCanvas.Arrange(new Rect(renderSize));

        //    return new Image { Source = renderBitmap };
        //}

        void SetTransformOnViewportWithoutRaisingViewChangeEvent(double scale, Point graphCenter, Rectangle vp) {
            var dx = vp.Width / 2 - scale * graphCenter.X;
            var dy = vp.Height / 2 + scale * graphCenter.Y;

            SetTransformWithoutRaisingViewChangeEvent(scale, dx, dy);

        }

        public Rectangle ClientViewportMappedToGraph {
            get {
                var t = Transform.Inverse;
                var p0 = new Point(0, 0);
                var p1 = new Point(_graphCanvas.RenderSize.Width, _graphCanvas.RenderSize.Height);
                return new Rectangle(t * p0, t * p1);
            }
        }


        void SetTransform(double scale, double dx, double dy) {
            if (ScaleIsOutOfRange(scale)) return;
            _graphCanvas.RenderTransform = new MatrixTransform(scale, 0, 0, -scale, dx, dy);
            if (ViewChangeEvent != null)
                ViewChangeEvent(null, null);
        }

        void SetTransformWithoutRaisingViewChangeEvent(double scale, double dx, double dy) {
            if (ScaleIsOutOfRange(scale)) return;
            _graphCanvas.RenderTransform = new MatrixTransform(scale, 0, 0, -scale, dx, dy);
        }

        bool ScaleIsOutOfRange(double scale) {
            return scale < 0.000001 || scale > 100000.0; //todo: remove hardcoded values
        }


        double FitFactor {
            get {
                var geomGraph = GeomGraph;
                if (_drawingGraph == null || geomGraph == null ||

                    geomGraph.Width == 0 || geomGraph.Height == 0)
                    return 1;

                var size = _graphCanvas.RenderSize;

                return GetFitFactor(size);
            }
        }

        double GetFitFactor(Size rect) {
            var geomGraph = GeomGraph;
            return geomGraph == null ? 1 : Math.Min(rect.Width / geomGraph.Width, rect.Height / geomGraph.Height);
        }

        void PushDataFromLayoutGraphToFrameworkElements() {
            CreateRectToFillCanvas();
            CreateAndPositionGraphBackgroundRectangle();
            CreateVNodes();
            CreateEdges();
        }


        void CreateRectToFillCanvas() {
            var parent = (Panel)GraphCanvas.Parent;
            _rectToFillCanvas = new Windows.UI.Xaml.Shapes.Rectangle();
            Canvas.SetLeft(_rectToFillCanvas, 0);
            Canvas.SetTop(_rectToFillCanvas, 0);
            _rectToFillCanvas.Width = parent.ActualWidth;
            _rectToFillCanvas.Height = parent.ActualHeight;

            _rectToFillCanvas.Fill = new SolidColorBrush(Colors.Transparent);
            Canvas.SetZIndex(_rectToFillCanvas, -2);
            _graphCanvas.Children.Add(_rectToFillCanvas);
        }




        void CreateEdges() {
            foreach (var edge in _drawingGraph.Edges)
                CreateEdge(edge, null);
        }

        VEdge CreateEdge(DrawingEdge edge, LgLayoutSettings lgSettings) {
            lock (this) {
                if (drawingObjectsToIViewerObjects.ContainsKey(edge))
                    return (VEdge)drawingObjectsToIViewerObjects[edge];
                if (lgSettings != null)
                    return CreateEdgeForLgCase(lgSettings, edge);

                FrameworkElement labelTextBox;
                drawingObjectsToFrameworkElements.TryGetValue(edge, out labelTextBox);
                var vEdge = new VEdge(edge, labelTextBox);

                var zIndex = ZIndexOfEdge(edge);
                drawingObjectsToIViewerObjects[edge] = vEdge;

                if (edge.Label != null)
                    SetVEdgeLabel(edge, vEdge, zIndex);

                Canvas.SetZIndex(vEdge.CurvePath, zIndex);
                _graphCanvas.Children.Add(vEdge.CurvePath);
                SetVEdgeArrowheads(vEdge, zIndex);

                return vEdge;
            }
        }

        int ZIndexOfEdge(DrawingEdge edge) {
            var source = (VNode)drawingObjectsToIViewerObjects[edge.SourceNode];
            var target = (VNode)drawingObjectsToIViewerObjects[edge.TargetNode];

            var zIndex = Math.Max(source.ZIndex, target.ZIndex) + 1;
            return zIndex;
        }

        VEdge CreateEdgeForLgCase(LgLayoutSettings lgSettings, DrawingEdge edge) {
            return (VEdge)(drawingObjectsToIViewerObjects[edge] = new VEdge(edge, lgSettings) {
                PathStrokeThicknessFunc = () => GetBorderPathThickness() * edge.Attr.LineWidth
            });
        }

        void SetVEdgeLabel(DrawingEdge edge, VEdge vEdge, int zIndex) {
            FrameworkElement frameworkElementForEdgeLabel;
            if (!drawingObjectsToFrameworkElements.TryGetValue(edge, out frameworkElementForEdgeLabel)) {
                drawingObjectsToFrameworkElements[edge] =
                    frameworkElementForEdgeLabel = CreateTextBlockForDrawingObj(edge);
                frameworkElementForEdgeLabel.Tag = new VLabel(edge, frameworkElementForEdgeLabel);
            }

            vEdge.VLabel = (VLabel)frameworkElementForEdgeLabel.Tag;
            if (frameworkElementForEdgeLabel.Parent == null) {
                _graphCanvas.Children.Add(frameworkElementForEdgeLabel);
                Canvas.SetZIndex(frameworkElementForEdgeLabel, zIndex);
            }
        }

        void SetVEdgeArrowheads(VEdge vEdge, int zIndex) {
            if (vEdge.SourceArrowHeadPath != null) {
                Canvas.SetZIndex(vEdge.SourceArrowHeadPath, zIndex);
                _graphCanvas.Children.Add(vEdge.SourceArrowHeadPath);
            }
            if (vEdge.TargetArrowHeadPath != null) {
                Canvas.SetZIndex(vEdge.TargetArrowHeadPath, zIndex);
                _graphCanvas.Children.Add(vEdge.TargetArrowHeadPath);
            }
        }

        void CreateVNodes() {
            foreach (var node in _drawingGraph.Nodes.Concat(_drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf())) {
                CreateVNode(node);
                Invalidate(drawingObjectsToIViewerObjects[node]);
            }
        }

        IViewerNode CreateVNode(Drawing.Node node) {
            lock (this) {
                if (drawingObjectsToIViewerObjects.ContainsKey(node))
                    return (IViewerNode)drawingObjectsToIViewerObjects[node];

                FrameworkElement feOfLabel;
                if (!drawingObjectsToFrameworkElements.TryGetValue(node, out feOfLabel))
                    feOfLabel = CreateAndRegisterFrameworkElementOfDrawingNode(node);

                var vn = new VNode(node, feOfLabel,
                    e => (VEdge)drawingObjectsToIViewerObjects[e], () => GetBorderPathThickness() * node.Attr.LineWidth);

                foreach (var fe in vn.FrameworkElements)
                    _graphCanvas.Children.Add(fe);

                drawingObjectsToIViewerObjects[node] = vn;


                return vn;
            }
        }

        public FrameworkElement CreateAndRegisterFrameworkElementOfDrawingNode(Drawing.Node node) {
            lock (this)
                return drawingObjectsToFrameworkElements[node] = CreateTextBlockForDrawingObj(node);
        }

        void CreateAndPositionGraphBackgroundRectangle() {
            CreateGraphBackgroundRect();
            SetBackgroundRectanglePositionAndSize();

            var rect = _rectToFillGraphBackground as System.Windows.Shapes.Rectangle;
            if (rect != null) {
                rect.Fill = Common.BrushFromMsaglColor(_drawingGraph.Attr.BackgroundColor);
            }
            Panel.SetZIndex(_rectToFillGraphBackground, -1);
            _graphCanvas.Children.Add(_rectToFillGraphBackground);
        }

        void CreateGraphBackgroundRect() {
            var lgGraphBrowsingSettings = _drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgGraphBrowsingSettings == null) {
                _rectToFillGraphBackground = new System.Windows.Shapes.Rectangle();
            }
        }


        void SetBackgroundRectanglePositionAndSize() {
            if (GeomGraph == null) return;
            _rectToFillGraphBackground.Width = GeomGraph.Width;
            _rectToFillGraphBackground.Height = GeomGraph.Height;

            var center = GeomGraph.BoundingBox.Center;
            Common.PositionFrameworkElement(_rectToFillGraphBackground, center, 1);
        }


        void PopulateGeometryOfGeometryGraph() {
            geometryGraphUnderLayout = _drawingGraph.GeometryGraph;
            foreach (
                Node msaglNode in
                    geometryGraphUnderLayout.Nodes) {
                var node = (Drawing.Node)msaglNode.UserData;
                if (_graphCanvas.Dispatcher.CheckAccess())
                    msaglNode.BoundaryCurve = GetNodeBoundaryCurve(node);
                else {
                    var msagNodeInThread = msaglNode;
                    _graphCanvas.Dispatcher.Invoke(() => msagNodeInThread.BoundaryCurve = GetNodeBoundaryCurve(node));
                }
            }

            foreach (
                Cluster cluster in geometryGraphUnderLayout.RootCluster.AllClustersWideFirstExcludingSelf()) {
                var subgraph = (Subgraph)cluster.UserData;
                if (_graphCanvas.Dispatcher.CheckAccess())
                    cluster.CollapsedBoundary = GetClusterCollapsedBoundary(subgraph);
                else {
                    var clusterInThread = cluster;
                    _graphCanvas.Dispatcher.Invoke(
                        () => clusterInThread.BoundaryCurve = GetClusterCollapsedBoundary(subgraph));
                }
                if (cluster.RectangularBoundary == null)
                    cluster.RectangularBoundary = new RectangularClusterBoundary();
                cluster.RectangularBoundary.TopMargin = subgraph.DiameterOfOpenCollapseButton + 0.5 +
                                                        subgraph.Attr.LineWidth / 2;
            }

            foreach (var msaglEdge in geometryGraphUnderLayout.Edges) {
                var drawingEdge = (DrawingEdge)msaglEdge.UserData;
                AssignLabelWidthHeight(msaglEdge, drawingEdge);
            }
        }

        ICurve GetClusterCollapsedBoundary(Subgraph subgraph) {
            double width, height;

            FrameworkElement fe;
            if (drawingObjectsToFrameworkElements.TryGetValue(subgraph, out fe)) {

                width = fe.Width + 2 * subgraph.Attr.LabelMargin + subgraph.DiameterOfOpenCollapseButton;
                height = Math.Max(fe.Height + 2 * subgraph.Attr.LabelMargin, subgraph.DiameterOfOpenCollapseButton);
            }
            else
                return GetApproximateCollapsedBoundary(subgraph);

            if (width < _drawingGraph.Attr.MinNodeWidth)
                width = _drawingGraph.Attr.MinNodeWidth;
            if (height < _drawingGraph.Attr.MinNodeHeight)
                height = _drawingGraph.Attr.MinNodeHeight;
            return NodeBoundaryCurves.GetNodeBoundaryCurve(subgraph, width, height);
        }

        ICurve GetApproximateCollapsedBoundary(Subgraph subgraph) {
            if (textBoxForApproxNodeBoundaries == null)
                SetUpTextBoxForApproxNodeBoundaries();


            double width, height;
            if (String.IsNullOrEmpty(subgraph.LabelText))
                height = width = subgraph.DiameterOfOpenCollapseButton;
            else {
                double a = ((double)subgraph.LabelText.Length) / textBoxForApproxNodeBoundaries.Text.Length *
                           subgraph.Label.FontSize / Label.DefaultFontSize;
                width = textBoxForApproxNodeBoundaries.Width * a + subgraph.DiameterOfOpenCollapseButton;
                height =
                    Math.Max(
                        textBoxForApproxNodeBoundaries.Height * subgraph.Label.FontSize / Label.DefaultFontSize,
                        subgraph.DiameterOfOpenCollapseButton);
            }

            if (width < _drawingGraph.Attr.MinNodeWidth)
                width = _drawingGraph.Attr.MinNodeWidth;
            if (height < _drawingGraph.Attr.MinNodeHeight)
                height = _drawingGraph.Attr.MinNodeHeight;

            return NodeBoundaryCurves.GetNodeBoundaryCurve(subgraph, width, height);
        }


        void AssignLabelWidthHeight(Core.Layout.ILabeledObject labeledGeomObj,
                                    DrawingObject drawingObj) {
            if (drawingObjectsToFrameworkElements.ContainsKey(drawingObj)) {
                FrameworkElement fe = drawingObjectsToFrameworkElements[drawingObj];
                labeledGeomObj.Label.Width = fe.Width;
                labeledGeomObj.Label.Height = fe.Height;
            }
        }


        ICurve GetNodeBoundaryCurve(Drawing.Node node) {
            double width, height;

            FrameworkElement fe;
            if (drawingObjectsToFrameworkElements.TryGetValue(node, out fe)) {
                width = fe.Width + 2 * node.Attr.LabelMargin;
                height = fe.Height + 2 * node.Attr.LabelMargin;
            }
            else
                return GetNodeBoundaryCurveByMeasuringText(node);

            if (width < _drawingGraph.Attr.MinNodeWidth)
                width = _drawingGraph.Attr.MinNodeWidth;
            if (height < _drawingGraph.Attr.MinNodeHeight)
                height = _drawingGraph.Attr.MinNodeHeight;
            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }

        TextBlock textBoxForApproxNodeBoundaries;

        public static Size MeasureText(string text,
        FontFamily family, double size) {


            FormattedText formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(family, new System.Windows.FontStyle(), FontWeights.Regular, FontStretches.Normal),
                size,
                new SolidColorBrush(Colors.Black),
                null);

            return new Size(formattedText.Width, formattedText.Height);
        }

        ICurve GetNodeBoundaryCurveByMeasuringText(Drawing.Node node) {
            double width, height;
            if (String.IsNullOrEmpty(node.LabelText)) {
                width = 10;
                height = 10;
            }
            else {
                var size = MeasureText(node.LabelText, new FontFamily(node.Label.FontName), node.Label.FontSize);
                width = size.Width;
                height = size.Height;
            }

            width += 2 * node.Attr.LabelMargin;
            height += 2 * node.Attr.LabelMargin;

            if (width < _drawingGraph.Attr.MinNodeWidth)
                width = _drawingGraph.Attr.MinNodeWidth;
            if (height < _drawingGraph.Attr.MinNodeHeight)
                height = _drawingGraph.Attr.MinNodeHeight;

            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }


        void SetUpTextBoxForApproxNodeBoundaries() {
            textBoxForApproxNodeBoundaries = new TextBlock {
                Text = "Fox jumping over River",
                FontFamily = new FontFamily(Label.DefaultFontName),
                FontSize = Label.DefaultFontSize,
            };

            textBoxForApproxNodeBoundaries.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            textBoxForApproxNodeBoundaries.Width = textBoxForApproxNodeBoundaries.DesiredSize.Width;
            textBoxForApproxNodeBoundaries.Height = textBoxForApproxNodeBoundaries.DesiredSize.Height;
        }


        void CreateFrameworkElementsForLabelsOnly() {
            foreach (var edge in _drawingGraph.Edges) {
                var fe = CreateDefaultFrameworkElementForDrawingObject(edge);
                if (fe != null)
                    if (_graphCanvas.Dispatcher.CheckAccess())
                        fe.Tag = new VLabel(edge, fe);
                    else {
                        var localEdge = edge;
                        _graphCanvas.Dispatcher.Invoke(() => fe.Tag = new VLabel(localEdge, fe));
                    }
            }

            foreach (var node in _drawingGraph.Nodes)
                CreateDefaultFrameworkElementForDrawingObject(node);
            if (_drawingGraph.RootSubgraph != null)
                foreach (var subgraph in _drawingGraph.RootSubgraph.AllSubgraphsWidthFirstExcludingSelf())
                    CreateDefaultFrameworkElementForDrawingObject(subgraph);
        }

        public void RegisterLabelCreator(DrawingObject drawingObject, Func<DrawingObject, FrameworkElement> func) {
            registeredCreators[drawingObject] = func;
        }

        public void UnregisterLabelCreator(DrawingObject drawingObject) {
            registeredCreators.Remove(drawingObject);
        }

        public Func<DrawingObject, FrameworkElement> GetLabelCreator(DrawingObject drawingObject) {
            return registeredCreators[drawingObject];
        }

        FrameworkElement CreateTextBlockForDrawingObj(DrawingObject drawingObj) {
            Func<DrawingObject, FrameworkElement> registeredCreator;
            if (registeredCreators.TryGetValue(drawingObj, out registeredCreator))
                return registeredCreator(drawingObj);
            if (drawingObj is Subgraph)
                return null; //todo: add Label support later
            var labeledObj = drawingObj as ILabeledObject;
            if (labeledObj == null)
                return null;

            var drawingLabel = labeledObj.Label;
            if (drawingLabel == null)
                return null;

            TextBlock textBlock = null;
            if (_graphCanvas.Dispatcher.CheckAccess())
                textBlock = CreateTextBlock(drawingLabel);
            else
                _graphCanvas.Dispatcher.Invoke(() => textBlock = CreateTextBlock(drawingLabel));

            return textBlock;
        }

        static TextBlock CreateTextBlock(Label drawingLabel) {
            var textBlock = new TextBlock {
                Tag = drawingLabel,
                Text = drawingLabel.Text,
                FontFamily = new FontFamily(drawingLabel.FontName),
                FontSize = drawingLabel.FontSize,
                Foreground = Common.BrushFromMsaglColor(drawingLabel.FontColor)
            };

            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            textBlock.Width = textBlock.DesiredSize.Width;
            textBlock.Height = textBlock.DesiredSize.Height;
            return textBlock;
        }


        FrameworkElement CreateDefaultFrameworkElementForDrawingObject(DrawingObject drawingObject) {
            lock (this) {
                var textBlock = CreateTextBlockForDrawingObj(drawingObject);
                if (textBlock != null)
                    drawingObjectsToFrameworkElements[drawingObject] = textBlock;
                return textBlock;
            }
        }




        public void DrawRubberLine(MsaglMouseEventArgs args) {
            DrawRubberLine(ScreenToSource(args));
        }

        public void StopDrawingRubberLine() {
            _graphCanvas.Children.Remove(_rubberLinePath);
            _rubberLinePath = null;
            _graphCanvas.Children.Remove(_targetArrowheadPathForRubberEdge);
            _targetArrowheadPathForRubberEdge = null;
        }

        public void AddEdge(IViewerEdge edge, bool registerForUndo) {

            var drawingEdge = edge.Edge;
            Edge geomEdge = drawingEdge.GeometryEdge;

            _drawingGraph.AddPrecalculatedEdge(drawingEdge);
            _drawingGraph.GeometryGraph.Edges.Add(geomEdge);

        }

        public IViewerEdge CreateEdgeWithGivenGeometry(DrawingEdge drawingEdge) {
            return CreateEdge(drawingEdge, _drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings);
        }

        public void AddNode(IViewerNode node, bool registerForUndo) {
            if (_drawingGraph == null)
                throw new InvalidOperationException(); // adding a node when the graph does not exist
            var vNode = (VNode)node;
            _drawingGraph.AddNode(vNode.Node);
            _drawingGraph.GeometryGraph.Nodes.Add(vNode.Node.GeometryNode);
            layoutEditor.AttachLayoutChangeEvent(vNode);
            _graphCanvas.Children.Add(vNode.FrameworkElementOfNodeForLabel);
            layoutEditor.CleanObstacles();
        }

        public IViewerObject AddNode(Drawing.Node drawingNode) {
            Graph.AddNode(drawingNode);
            var vNode = CreateVNode(drawingNode);
            LayoutEditor.AttachLayoutChangeEvent(vNode);
            LayoutEditor.CleanObstacles();
            return vNode;
        }

        public void RemoveEdge(IViewerEdge edge, bool registerForUndo) {
            lock (this) {
                var vedge = (VEdge)edge;
                var dedge = vedge.Edge;
                _drawingGraph.RemoveEdge(dedge);
                _drawingGraph.GeometryGraph.Edges.Remove(dedge.GeometryEdge);
                drawingObjectsToFrameworkElements.Remove(dedge);
                drawingObjectsToIViewerObjects.Remove(dedge);

                vedge.RemoveItselfFromCanvas(_graphCanvas);
            }
        }

        public void RemoveNode(IViewerNode node, bool registerForUndo) {
            lock (this) {
                RemoveEdges(node.Node.OutEdges);
                RemoveEdges(node.Node.InEdges);
                RemoveEdges(node.Node.SelfEdges);
                drawingObjectsToFrameworkElements.Remove(node.Node);
                drawingObjectsToIViewerObjects.Remove(node.Node);
                var vnode = (VNode)node;
                vnode.DetouchFromCanvas(_graphCanvas);

                _drawingGraph.RemoveNode(node.Node);
                _drawingGraph.GeometryGraph.Nodes.Remove(node.Node.GeometryNode);
                layoutEditor.DetachNode(node);
                layoutEditor.CleanObstacles();
            }
        }

        void RemoveEdges(IEnumerable<DrawingEdge> drawingEdges) {
            foreach (var de in drawingEdges.ToArray()) {
                var vedge = (VEdge)drawingObjectsToIViewerObjects[de];
                RemoveEdge(vedge, false);
            }
        }


        public IViewerEdge RouteEdge(DrawingEdge drawingEdge) {
            var geomEdge = GeometryGraphCreator.CreateGeometryEdgeFromDrawingEdge(drawingEdge);
            var geomGraph = _drawingGraph.GeometryGraph;
            LayoutHelpers.RouteAndLabelEdges(geomGraph, _drawingGraph.LayoutAlgorithmSettings, new[] { geomEdge });
            return CreateEdge(drawingEdge, _drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings);
        }

        public IViewerGraph ViewerGraph { get; set; }

        public double ArrowheadLength {
            get { return 0.2 * DpiX / CurrentScale; }
        }

        public void SetSourcePortForEdgeRouting(Point portLocation) {
            _sourcePortLocationForEdgeRouting = portLocation;
            if (_sourcePortCircle == null) {
                _sourcePortCircle = CreatePortPath();
                _graphCanvas.Children.Add(_sourcePortCircle);
            }
            _sourcePortCircle.Width = _sourcePortCircle.Height = UnderlyingPolylineCircleRadius;
            _sourcePortCircle.StrokeThickness = _sourcePortCircle.Width / 10;
            Common.PositionFrameworkElement(_sourcePortCircle, portLocation, 1);
        }

        Ellipse CreatePortPath() {
            return new Ellipse {
                Stroke = new SolidColorBrush(Colors.Brown),
                Fill = new SolidColorBrush(Colors.Brown),
            };
        }



        public void SetTargetPortForEdgeRouting(Point portLocation) {
            if (TargetPortCircle == null) {
                TargetPortCircle = CreatePortPath();
                _graphCanvas.Children.Add(TargetPortCircle);
            }
            TargetPortCircle.Width = TargetPortCircle.Height = UnderlyingPolylineCircleRadius;
            TargetPortCircle.StrokeThickness = TargetPortCircle.Width / 10;
            Common.PositionFrameworkElement(TargetPortCircle, portLocation, 1);
        }

        public void RemoveSourcePortEdgeRouting() {
            _graphCanvas.Children.Remove(_sourcePortCircle);
            _sourcePortCircle = null;
        }

        public void RemoveTargetPortEdgeRouting() {
            _graphCanvas.Children.Remove(TargetPortCircle);
            TargetPortCircle = null;
        }


        public void DrawRubberEdge(EdgeGeometry edgeGeometry) {
            if (_rubberEdgePath == null) {
                _rubberEdgePath = new Path {
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = GetBorderPathThickness() * 3
                };
                _graphCanvas.Children.Add(_rubberEdgePath);
                _targetArrowheadPathForRubberEdge = new Path {
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = GetBorderPathThickness() * 3
                };
                _graphCanvas.Children.Add(_targetArrowheadPathForRubberEdge);
            }
            _rubberEdgePath.Data = VEdge.GetICurveWpfGeometry(edgeGeometry.Curve);
            _targetArrowheadPathForRubberEdge.Data = VEdge.DefiningTargetArrowHead(edgeGeometry,
                                                                                  edgeGeometry.LineWidth);
        }


        bool UnderLayout {
            get { return _backgroundWorker != null; }
        }

        public void StopDrawingRubberEdge() {
            _graphCanvas.Children.Remove(_rubberEdgePath);
            _graphCanvas.Children.Remove(_targetArrowheadPathForRubberEdge);
            _rubberEdgePath = null;
            _targetArrowheadPathForRubberEdge = null;
        }


        public PlaneTransformation Transform {
            get {
                var mt = _graphCanvas.RenderTransform as MatrixTransform;
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
            _graphCanvas.RenderTransform = new MatrixTransform {
                Matrix = new Windows.UI.Xaml.Media.Matrix {
                    M11 = value[0, 0],
                    M12 = value[0, 1],
                    M21 = value[1, 0],
                    M22 = value[1, 1],
                    OffsetX = value[0, 2],
                    OffsetY = value[1, 2]
                }
            };
        }


        public bool NeedToCalculateLayout {
            get { return needToCalculateLayout; }
            set { needToCalculateLayout = value; }
        }

        /// <summary>
        /// the cancel token used to cancel a long running layout
        /// </summary>
        public CancelToken CancelToken {
            get { return _cancelToken; }
            set { _cancelToken = value; }
        }

        /// <summary>
        /// no layout is done, but the overlap is removed for graphs with geometry
        /// </summary>
        public bool NeedToRemoveOverlapOnly { get; set; }


        public void DrawRubberLine(Point rubberEnd) {
            if (_rubberLinePath == null) {
                _rubberLinePath = new Path {
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = GetBorderPathThickness() * 3
                };
                _graphCanvas.Children.Add(_rubberLinePath);
            }
            _rubberLinePath.Data =
                VEdge.GetICurveWpfGeometry(new LineSegment(_sourcePortLocationForEdgeRouting, rubberEnd));
        }

        public void StartDrawingRubberLine(Point startingPoint) {
        }

        #endregion



        public IViewerNode CreateIViewerNode(Drawing.Node drawingNode, Point center, object visualElement) {
            if (_drawingGraph == null)
                return null;
            var frameworkElement = visualElement as FrameworkElement ?? CreateTextBlockForDrawingObj(drawingNode);
            var width = frameworkElement.Width + 2 * drawingNode.Attr.LabelMargin;
            var height = frameworkElement.Height + 2 * drawingNode.Attr.LabelMargin;
            var bc = NodeBoundaryCurves.GetNodeBoundaryCurve(drawingNode, width, height);
            drawingNode.GeometryNode = new Node(bc, drawingNode) { Center = center };
            var vNode = CreateVNode(drawingNode);
            _drawingGraph.AddNode(drawingNode);
            _drawingGraph.GeometryGraph.Nodes.Add(drawingNode.GeometryNode);
            layoutEditor.AttachLayoutChangeEvent(vNode);
            MakeRoomForNewNode(drawingNode);

            return vNode;
        }

        void MakeRoomForNewNode(Drawing.Node drawingNode) {
            IncrementalDragger incrementalDragger = new IncrementalDragger(new[] { drawingNode.GeometryNode },
                                                                           Graph.GeometryGraph,
                                                                           Graph.LayoutAlgorithmSettings);
            incrementalDragger.Drag(new Point());

            foreach (var n in incrementalDragger.ChangedGraph.Nodes) {
                var dn = (Drawing.Node)n.UserData;
                var vn = drawingObjectsToIViewerObjects[dn] as VNode;
                if (vn != null)
                    vn.Invalidate();
            }

            foreach (var n in incrementalDragger.ChangedGraph.Edges) {
                var dn = (Drawing.Edge)n.UserData;
                var ve = drawingObjectsToIViewerObjects[dn] as VEdge;
                if (ve != null)
                    ve.Invalidate();
            }
        }
    }
}
