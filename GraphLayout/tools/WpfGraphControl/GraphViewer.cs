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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Baml2006;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
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
using Size = System.Windows.Size;
using WpfPoint = System.Windows.Point;
using System.Windows.Shapes;
    using Edge = Microsoft.Msagl.Core.Layout.Edge;
    using Ellipse = System.Windows.Shapes.Ellipse;
    using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using Polyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;


namespace Microsoft.Msagl.WpfGraphControl {
    public class GraphViewer : IViewer {
        readonly RailSliding _railSlidingData = new RailSliding();
        Path targetArrowheadPathForRubberEdge;

        Path _rubberEdgePath;
        Path _rubberLinePath;
        Point _sourcePortLocationForEdgeRouting;
        //WpfPoint _objectUnderMouseDetectionLocation;
        CancelToken _cancelToken = new CancelToken();
        BackgroundWorker _backgroundWorker;
        LgLayoutSettings _defaultLargeLayoutSettings = new LgLayoutSettings(null, null, 0, 0, null);
        Point _mouseDownPositionInGraph;
       
        Ellipse _sourcePortCircle;
        Ellipse _railBall;
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

        readonly Canvas _graphCanvas = new Canvas();
        Graph drawingGraph;
        
        readonly Dictionary<DrawingObject, FrameworkElement> drawingObjectsToFrameworkElements =
            new Dictionary<DrawingObject, FrameworkElement>();

        readonly LayoutEditor layoutEditor;

        public LgLayoutSettings DefaultLargeLayoutSettings {
            get { return _defaultLargeLayoutSettings; }
            set { _defaultLargeLayoutSettings = value; }
        }

        GeometryGraph geometryGraphUnderLayout;
        /*
                Thread layoutThread;
        */
        bool needToCalculateLayout = true;


        object _objectUnderMouseCursor;

        Rail RailUnderCursor {
            get {
                return _railSlidingData._rail;
            }
            set {
                _railSlidingData._rail = value;
            }
        }

        static double dpiX;
        static int dpiY;

        readonly Dictionary<DrawingObject, IViewerObject> drawingObjectsToIViewerObjects =
            new Dictionary<DrawingObject, IViewerObject>();

        FrameworkElement _rectToFillGraphBackground;
        System.Windows.Shapes.Rectangle _rectToFillCanvas;

        //needed for LgLayout
        readonly Dictionary<Rail, FrameworkElement> visibleRailsToFrameworkElems = new Dictionary<Rail, FrameworkElement>();

        GeometryGraph GeomGraph {
            get { return drawingGraph.GeometryGraph; }
        }

        /// <summary>
        /// the canvas to draw the graph
        /// </summary>
        public Canvas GraphCanvas {
            get { return _graphCanvas; }
        }

        public GraphViewer() {
            //LargeGraphNodeCountThreshold = 0;
            layoutEditor = new LayoutEditor(this);

            _graphCanvas.SizeChanged += GraphCanvasSizeChanged;
            _graphCanvas.MouseLeftButtonDown += GraphCanvasMouseLeftButtonDown;
            _graphCanvas.MouseRightButtonDown += GraphCanvasRightMouseDown;
            _graphCanvas.MouseMove += GraphCanvasMouseMove;

            _graphCanvas.MouseLeftButtonUp += GraphCanvasMouseLeftButtonUp;
            _graphCanvas.MouseWheel += GraphCanvasMouseWheel;
            _graphCanvas.MouseRightButtonUp += GraphCanvasRightMouseUp;
            ViewChangeEvent += AdjustBtrectRenderTransform;
     
            LayoutEditingEnabled = true;
            clickCounter = new ClickCounter(() => Mouse.GetPosition((IInputElement) _graphCanvas.Parent));
            clickCounter.Elapsed += ClickCounterElapsed;
        }


        #region WPF stuff

        /// <summary>
        /// adds the main panel of the viewer to the children of the parent
        /// </summary>
        /// <param name="panel"></param>
        public void BindToPanel(Panel panel) {
            panel.Children.Add(GraphCanvas);
            GraphCanvas.UpdateLayout();
        }


        void ClickCounterElapsed(object sender, EventArgs e) {
            var vedge = clickCounter.ClickedObject as VEdge;
            if (vedge != null) {
                if (clickCounter.UpCount == clickCounter.DownCount && clickCounter.UpCount == 1)
                    HandleClickForEdge(vedge);
            } else {
                var vnode = clickCounter.ClickedObject as VNode;
                if (vnode != null)
                    HandleMultipleClickForNode(vnode);
                else {
                    var rail = clickCounter.ClickedObject as Rail;
                    if (rail != null) {
                        if (clickCounter.UpCount == clickCounter.DownCount && clickCounter.UpCount == 1)
                            HandleClickForRail(rail);
                    }
                }
            }
            clickCounter.ClickedObject = null;
        }

        void HandleClickForRail(Rail rail) {
            if (rail.IsHighlighted)
                PutOffEdgesPassingThroughTheRail(rail);
            else
                HighlightAllEdgesPassingThroughTheRail(rail);

            ViewChangeEvent(null, null);
        }

        void PutOffEdgesPassingThroughTheRail(Rail rail)
        {
            ((LgLayoutSettings)drawingGraph.LayoutAlgorithmSettings).PutOffEdgesPassingThroughTheRail(rail);            
        }

        void HighlightAllEdgesPassingThroughTheRail(Rail rail) {
            var lgLayoutSettings = (LgLayoutSettings) drawingGraph.LayoutAlgorithmSettings;
            lgLayoutSettings.HighlightEdgesPassingThroughTheRail(rail);
        }


        void AdjustBtrectRenderTransform(object sender, EventArgs e) {
            if (_rectToFillCanvas == null)
                return;
            _rectToFillCanvas.RenderTransform = (Transform) _graphCanvas.RenderTransform.Inverse;
            var parent = (Panel) GraphCanvas.Parent;
            _rectToFillCanvas.Width = parent.ActualWidth;
            _rectToFillCanvas.Height = parent.ActualHeight;

        }

        void GraphCanvasRightMouseUp(object sender, MouseButtonEventArgs e) {
            OnMouseUp(e);
        }

        void HandleMultipleClickForNode(VNode vnode) {
            if (clickCounter.DownCount == clickCounter.UpCount && clickCounter.UpCount == 1)
                ToggleNodeSlidingZoom(vnode);
            else if (clickCounter.DownCount >= 2) {
                ToggleNodeSlidingZoom(vnode);
                ToggleNodeEdgesSlidingZoom(vnode);
            }
        }

        void ToggleNodeEdgesSlidingZoom(VNode vnode) {
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings != null)
                foreach (var ei in vnode.Node.GeometryNode.Edges.Select(e => lgSettings.GeometryEdgesToLgEdgeInfos[e]))
                    ei.SlidingZoomLevel = ei.SlidingZoomLevel <= 1 ? double.PositiveInfinity : 1;
            ViewChangeEvent(null, null);
        }

        void ToggleNodeSlidingZoom(VNode vnode) {
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings != null) {
                var nodeInfo = lgSettings.GeometryNodesToLgNodeInfos[vnode.Node.GeometryNode];
                nodeInfo.SlidingZoomLevel = nodeInfo.SlidingZoomLevel == 0 ? double.PositiveInfinity : 0;
                if (nodeInfo.ZoomLevel > 0)
                    lgSettings.PutOffEdges(nodeInfo.GeometryNode.Edges.ToList());
                else
                    lgSettings.HighlightEdges(nodeInfo.GeometryNode.Edges);
            }
            ViewChangeEvent(null, null);
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



/*
        Tuple<string, VoidDelegate>[] CreateToggleZoomLevelMenuCoupleForNode(VNode vNode) {
            var list = new List<Tuple<string, VoidDelegate>>();
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings != null) {
                var lgNodeInfo = lgSettings.GeometryNodesToLgNodeInfos[(Node) vNode.DrawingObject.GeometryObject];
                list.Add(ToggleZoomLevelMenuCouple(lgNodeInfo));
                list.Add(MakeAllNodeEdgesAlwaysVisible(lgNodeInfo));
                list.Add(RestoreAllNodeEdgesZoomLevels(lgNodeInfo));

                return list.ToArray();
            }
            return null;
        }
*/

/*
        Tuple<string, VoidDelegate> RestoreAllNodeEdgesZoomLevels(LgNodeInfo lgNodeInfo) {
            const string title = "Restore zoom levels of adjacent edges";

            var lgSettings = (LgLayoutSettings) Graph.LayoutAlgorithmSettings;

            return new Tuple<string, VoidDelegate>(title, () => {
                foreach (var edge in lgNodeInfo.GeometryNode.Edges) {
                    var lgEi = lgSettings.GeometryEdgesToLgEdgeInfos[edge];
                    lgEi.SlidingZoomLevel = double.PositiveInfinity;
                }
                ViewChangeEvent(null, null);
            });
        }
*/

/*
        Tuple<string, VoidDelegate> MakeAllNodeEdgesAlwaysVisible(LgNodeInfo lgNodeInfo) {
            const string title = "Set zoom levels for adjacent edges to 1";

            var lgSettings = (LgLayoutSettings) Graph.LayoutAlgorithmSettings;

            return new Tuple<string, VoidDelegate>(title, () => {
                foreach (var edge in lgNodeInfo.GeometryNode.Edges) {
                    var lgEi = lgSettings.GeometryEdgesToLgEdgeInfos[edge];
                    lgEi.SlidingZoomLevel = 1;
                }
                ViewChangeEvent(null, null);
            });
        }
*/

/*
        Tuple<string, VoidDelegate> CreateToggleZoomLevelMenuCoupleForEdge(VEdge vedge) {
            var lgSettings = (LgLayoutSettings) Graph.LayoutAlgorithmSettings;
            var lgEdgeInfo = lgSettings.GeometryEdgesToLgEdgeInfos[(Edge) vedge.DrawingObject.GeometryObject];

            return ToggleZoomLevelMenuCouple(lgEdgeInfo);
        }
*/

/*
        Tuple<string, VoidDelegate> ToggleZoomLevelMenuCouple(LgInfoBase lgEdgeInfo) {
            string title;
            double newZoomLevel;
            if (lgEdgeInfo.ZoomLevel > 0) {
                title = "Make always visible";
                newZoomLevel = 0;
            } else {
                title = "Restore zoom level";
                newZoomLevel = double.PositiveInfinity;
            }

            return new Tuple<string, VoidDelegate>(title, () => {
                lgEdgeInfo.SlidingZoomLevel = newZoomLevel;
                ViewChangeEvent(null, null);
            });
        }
*/

        void GraphCanvasRightMouseDown(object sender, MouseButtonEventArgs e) {
            if (MouseDown != null)
                MouseDown(this, CreateMouseEventArgs(e));            
        }

        void GraphCanvasMouseWheel(object sender, MouseWheelEventArgs e) {
            if (e.Delta != 0) {
                const double zoomFractionLocal = 0.9;
                var zoomInc = e.Delta < 0 ? zoomFractionLocal : 1.0/zoomFractionLocal;
                ZoomAbout(ZoomFactor*zoomInc, e.GetPosition(_graphCanvas));
                e.Handled = true;
            }
        }

        /// <summary>
        /// keeps centerOfZoom pinned to the screen and changes the scale by zoomFactor
        /// </summary>
        /// <param name="zoomFactor"></param>
        /// <param name="centerOfZoom"></param>
        public void ZoomAbout(double zoomFactor, WpfPoint centerOfZoom) {
            var scale = zoomFactor*FitFactor;
            var centerOfZoomOnScreen =
                _graphCanvas.TransformToAncestor((FrameworkElement) _graphCanvas.Parent).Transform(centerOfZoom);
            SetTransform(scale, centerOfZoomOnScreen.X - centerOfZoom.X*scale,
                         centerOfZoomOnScreen.Y + centerOfZoom.Y*scale);
        }

        public LayoutEditor LayoutEditor {
            get { return layoutEditor; }
        }

        void GraphCanvasMouseLeftButtonDown(object sender, MouseEventArgs e) {
            clickCounter.AddMouseDown(RailUnderCursor ?? _objectUnderMouseCursor);
            if (MouseDown != null)
                MouseDown(this, CreateMouseEventArgs(e));

            if (e.Handled) return;
            _mouseDownPositionInGraph = Common.MsaglPoint(e.GetPosition(_graphCanvas));
            if (RailUnderCursor != null && Mouse.LeftButton == MouseButtonState.Pressed && RailSlidingIsOn()) {
                InitRailSlidingLeftButtonDownPoints();
            }
        }

        void InitRailSlidingLeftButtonDownPoints() {
            if (Graph == null)
                return;
            if (RailUnderCursor == null)
                return;
            _railSlidingData._mouseDownPositionOnGraph = Common.MsaglPoint(Mouse.GetPosition(_graphCanvas));
            _railSlidingData._mouseDownPositionOnScreen = Mouse.GetPosition((FrameworkElement) _graphCanvas.Parent);
            var railCurve = RailUnderCursor.Geometry as ICurve;
            if (railCurve == null)
                return;
            _railSlidingData.Curve = railCurve;
            _railSlidingData.Parameter = railCurve.ClosestParameter(_railSlidingData._mouseDownPositionOnGraph);

            _railSlidingData._fromStartToEnd = _railSlidingData.Parameter  - railCurve.ParStart <
                                                railCurve.ParEnd - _railSlidingData.Parameter;

            //on one inch pass a viewport
            //(Dpi)*speed=ViewportX/cur
            _railSlidingData._speed = ClientViewportMappedToGraph.Diagonal/DpiX;

        }


        void GraphCanvasMouseMove(object sender, MouseEventArgs e) {
            if (MouseMove != null)
                MouseMove(this, CreateMouseEventArgs(e));

            if (e.Handled) return;


            if (Mouse.LeftButton == MouseButtonState.Pressed && (!LayoutEditingEnabled || _objectUnderMouseCursor == null))
                Pan(e);
            else {
                // Retrieve the coordinate of the mouse position.
                WpfPoint mouseLocation = e.GetPosition(_graphCanvas);
                // Clear the contents of the list used for hit test results.
                ObjectUnderMouseCursor = null;
                RailUnderCursor = null;
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
            if (RailUnderCursor == null)
               RemoveRailBall();
        }

        
        // Return the result of the hit test to the callback.
        HitTestResultBehavior MyHitTestResultCallback(HitTestResult result) {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;
            if(frameworkElement.Tag==null)
                return HitTestResultBehavior.Continue;
            var tag = frameworkElement.Tag;
            var iviewerObj = tag as IViewerObject;
            if (iviewerObj != null) {
                if (ObjectUnderMouseCursor is IViewerEdge || ObjectUnderMouseCursor == null
                    ||
                    Panel.GetZIndex(frameworkElement) >
                    Panel.GetZIndex(GetFrameworkElementFromIViewerObject(ObjectUnderMouseCursor)))
                    //always overwrite an edge or take the one with greater zIndex
                    ObjectUnderMouseCursor = iviewerObj;
            }
            else {
                var rail = tag as Rail;
                if (rail != null) {
                    ObjectUnderMouseCursor = GetIViewerObjectFromObjectUnderCursor(rail);
                    SetRailUnderCursor(rail);
                    return HitTestResultBehavior.Stop;
                }
            }
            return HitTestResultBehavior.Continue;
            
        }


        void SetRailUnderCursor(Rail rail) {
            if (rail != null) {
                if (RailSlidingIsOn())
                    MaybeCreateAndPositionRailBall();
            }
            else
                RemoveRailBall();
            RailUnderCursor = rail;
        }

        void RemoveRailBall() {
            if (_railBall != null) {
                _graphCanvas.Children.Remove(_railBall);
                _railBall = null;
            }
        }

        void MaybeCreateAndPositionRailBall() {
            if (_railBall == null)
                CreateRailBall();
            var pos = Mouse.GetPosition(_graphCanvas);
            Common.PositionFrameworkElement(_railBall, pos, 1);
        }

        bool RailSlidingIsOn() {
            return (ModifierKeys & ModifierKeys.Alt)==ModifierKeys.Alt;
        }

        void CreateRailBall() {
            _railBall = CreatePortPath();
            _railBall.Width = _railBall.Height = UnderlyingPolylineCircleRadius;
            _railBall.StrokeThickness = _railBall.Width / 10;
            _graphCanvas.Children.Add(_railBall);
        }

        FrameworkElement GetFrameworkElementFromIViewerObject(IViewerObject viewerObject) {
            FrameworkElement ret;

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
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;
            object tag = frameworkElement.Tag;
            if (tag != null) {
                //it is a tagged element
                var ivo = tag as IViewerObject;
                if (ivo != null) {
                    _objectUnderMouseCursor = ivo;
                    if (tag is VNode || tag is Label)
                        return HitTestResultBehavior.Stop;
                }else {
                    System.Diagnostics.Debug.Assert(tag is Rail);
                    _objectUnderMouseCursor = tag;
                    return HitTestResultBehavior.Stop;
                }
            }
            
            return HitTestResultBehavior.Continue;
        }


        protected double MouseHitTolerance {
            get { return (LargeGraphBrowsing?0.01:0.05)*DpiX/CurrentScale; }

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

        void Pan(MouseEventArgs e) {
            if (UnderLayout)
                return;
            
            if (!_graphCanvas.IsMouseCaptured)
                _graphCanvas.CaptureMouse();

            if (RailSlidingIsOn())
                SlideTheRail(e);
            else
                SetTransformFromTwoPoints(e.GetPosition((FrameworkElement) _graphCanvas.Parent),
                    _mouseDownPositionInGraph);

            if (ViewChangeEvent != null)
                 ViewChangeEvent(null, null);
        }

        WpfPoint MousePositionOnScreen(MouseEventArgs mouseEventArgs) {
            return mouseEventArgs.GetPosition((FrameworkElement) _graphCanvas.Parent);
        }

        void SlideTheRail(MouseEventArgs mouseEventArgs) {
            if (_railBall == null || _railSlidingData.Curve == null)
                return;
            var mousePositionOnScreen = MousePositionOnScreen(mouseEventArgs);
            if (_railSlidingData.IsStuck(mousePositionOnScreen)) {
                SwitchRail(mouseEventArgs);
            }
            if (RailUnderCursor == null)
                return;
            var vect = mousePositionOnScreen - _railSlidingData._mouseDownPositionOnScreen;
            if (vect.X == 0 && vect.Y == 0)
                return;
            var oldCurvePoint = _railSlidingData.GetCurrentPointOnRail();
            var curvePoint = GetRailPointAndUpdateMouseDownPos(mouseEventArgs);
            if (ApproximateComparer.CloseIntersections(oldCurvePoint, curvePoint))
                return;
            SetTransformFromTwoPoints(_railSlidingData._mouseDownPositionOnScreen, curvePoint);
            Common.PositionFrameworkElement(_railBall, curvePoint, 1);
        }

//        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
//        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
//        public static extern bool SetCursorPos(int X, int Y);   

        void SwitchRail(MouseEventArgs mouseEventArgs) {
            
            List<Rail> hitRails = FindAdjacentRails();

            if (hitRails == null || hitRails.Count == 0) return;
            Point mouseDelta =Common.MsaglPoint(mouseEventArgs.GetPosition(_graphCanvas)) - _railSlidingData._mouseDownPositionOnGraph;
            Rail rail = PickTheBestFittedRail(hitRails, mouseDelta);

            if (rail == null)
                return;

            Debug.Assert(rail != RailUnderCursor);
            var curve = (ICurve) rail.Geometry;
            var par = curve.ClosestParameter(Common.MsaglPoint(mouseEventArgs.GetPosition(_graphCanvas)));
            RailUnderCursor = rail;
            _railSlidingData.Parameter = par;
            _railSlidingData.Curve = curve;
            SetTransformFromTwoPoints(mouseEventArgs.GetPosition((FrameworkElement) _graphCanvas.Parent), curve[par]);
        }

        Rail PickTheBestFittedRail(List<Rail> hitRails, Point mouseDelta) {

            double delAngle = 100;// big number 

            Rail bestRail = null;
            var currentRailStuckPoint = _railSlidingData.GetStuckPoint();
            foreach (var rail in hitRails) {
                if (rail == RailUnderCursor)
                    continue;
                var curve = rail.Geometry as ICurve;
                if (curve == null)
                    continue;
                if (ApproximateComparer.Close(curve.Start, currentRailStuckPoint)) {
                    var curveDerivative = curve.Derivative(curve.ParStart);
                    delAngle = UpdateBestRailAndDelAngle(curveDerivative, mouseDelta, delAngle,
                        rail, ref bestRail);
                }
                else if (ApproximateComparer.Close(curve.End, currentRailStuckPoint)) {
                    var curveDerivative = curve.Derivative(curve.ParEnd);
                    delAngle = UpdateBestRailAndDelAngle(curveDerivative, mouseDelta, delAngle,
                        rail, ref bestRail);
                }                                    
            }
            return bestRail;
        }

        static double UpdateBestRailAndDelAngle(Point curveDerivative, Point currentRailDerivativeAtStuckPoint, double delAngle,
            Rail rail, ref Rail bestRail) {
            if (curveDerivative*currentRailDerivativeAtStuckPoint < 0) {
                curveDerivative = -curveDerivative;
            }
            var angle = Point.Angle(curveDerivative, currentRailDerivativeAtStuckPoint);
            if (angle > Math.PI) {
                angle = 2*Math.PI - angle;
            }
            if (delAngle > angle) {
                delAngle = angle;
                bestRail = rail;
            }
            return delAngle;
        }

        List<Rail> FindAdjacentRails() {
            var list = new List<Rail>();
            var pt = _railSlidingData.GetStuckPoint();
            var rect = new Rect(new WpfPoint(pt.X - MouseHitTolerance, pt.Y - MouseHitTolerance),
                                new WpfPoint(pt.X + MouseHitTolerance, pt.Y + MouseHitTolerance));
            var expandedHitTestArea = new RectangleGeometry(rect);
            // Set up a callback to receive the hit test result enumeration.
            VisualTreeHelper.HitTest(_graphCanvas, null,
                                     (a)=>AddRailUnderHit(a, list),
                                     new GeometryHitTestParameters(expandedHitTestArea));
            return list;
        }

        static HitTestResultBehavior AddRailUnderHit(HitTestResult result, List<Rail> hitRails) {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;
            if (frameworkElement.Tag == null)
                return HitTestResultBehavior.Continue;
            var tag = frameworkElement.Tag;
            var rail = tag as Rail;
            if (rail != null && rail.Geometry is ICurve) {
                hitRails.Add(rail);
            }
            return HitTestResultBehavior.Continue;
        }

        Point GetRailPointAndUpdateMouseDownPos(MouseEventArgs mouseEventArgs) {
            var mouseOnScreen = MousePositionOnScreen(mouseEventArgs);
            var mouseOnGraph = Common.MsaglPoint( mouseEventArgs.GetPosition(_graphCanvas));
            
            var par = GetNextParOnSlidingRail(mouseOnScreen);

            var curvePoint = _railSlidingData.GetPointOnRail(par);
            _railSlidingData._mouseDownPositionOnScreen = mouseOnScreen;
            _railSlidingData._mouseDownPositionOnGraph = mouseOnGraph;
            _railSlidingData.Parameter = par;
            return curvePoint;
        }



        double GetNextParOnSlidingRail(WpfPoint mouseNow) {
            
            var derivative = _railSlidingData.Derivative();
            var mouseDeltaOnScreen = mouseNow - _railSlidingData._mouseDownPositionOnScreen;

            int growingPar = derivative.X*mouseDeltaOnScreen.X-derivative.Y*mouseDeltaOnScreen.Y > 0 ? 1 : -1;
            
            _prevGrowingParDebug = growingPar;

            var mouseDist = (mouseNow - _railSlidingData._mouseDownPositionOnScreen).Length;

            double stepLength = mouseDist*_railSlidingData._speed;

            return GetNextParOnSlidingRailWithStepLength(stepLength, growingPar);
        }

        double GetNextParOnSlidingRailWithStepLength(double stepLength, int growingPar) {
            if (growingPar > 0)
                return _railSlidingData.GetNextPlusParOnSlidingRailWithStepLength(stepLength);
            return _railSlidingData.GetNextMinusParOnSlidingRailWithStepLength(stepLength);
        }


        Point ProjectToCurve(ICurve c, Point p) {
            var par = c.ClosestParameter(p);
            return c[par];
        }
//        WpfPoint ModifyDxDyForEdgeSliding(ref double dx, ref double dy, WpfPoint currentMousePositionOnScreen) {
//            var railCurve = _railUnderCursor.Geometry as ICurve;
//            if (railCurve == null)
//               return currentMousePositionOnScreen;
//            var mousePosInSource = Transform.Inverse* Common.MsaglPoint(currentMousePositionOnScreen);
//            var mouseProj = ProjectToCurve(railCurve, mousePosInSource);
//            var mouseDownInSource = ProjectToCurve(railCurve,Transform.Inverse*Common.MsaglPoint(_mouseDownPointOnScreen));
///*
////            var rad = (mouseProj-mouseDownInSource)*ZoomFactor;
//            
////            var finishLineEllipse = new Core.Geometry.Curves.Ellipse(0, Math.PI,
////                rad.Rotate90Cw(), rad, mouseDownInSource );
//
////            LayoutAlgorithmSettings.ShowDebugCurves(
////                new DebugCurve(railCurve),
////                new DebugCurve("red", ToPoly(finishLineEllipse)),
////                new DebugCurve("green",
////                    new LineSegment(mouseProj, mouseDownInSource))
////                );
//                 
//
//
//            var x = Curve.CurveCurveIntersectionOne(railCurve, finishLineEllipse, false);
//            if (x != null) {
////                LayoutAlgorithmSettings.ShowDebugCurves(
////                    new DebugCurve(railCurve), 
////                    new DebugCurve("red", ToPoly(finishLineEllipse)),
////                    new DebugCurve("green",
////                        new LineSegment(mousePosInSource, mouseDownInSource)),
////                        new DebugCurve("navy",
////                        new LineSegment(mousePosInSource,x.IntersectionPoint))
////                    );
//
//
//                var intersectionOnScreen = Transform*x.IntersectionPoint;
//                dx = currentMousePositionOnScreen.X - intersectionOnScreen.X;
//                dy = currentMousePositionOnScreen.Y - intersectionOnScreen.Y;
//                return Common.WpfPoint(intersectionOnScreen);
//            }
//            return currentMousePositionOnScreen;
//*/
//        }

        ICurve ToPoly(Core.Geometry.Curves.Ellipse finishLineEllipse) {
            var n = 20;
            var points = new Point[n];
            var del = (finishLineEllipse.ParEnd - finishLineEllipse.ParStart)/(n - 1);
            for (int i = 0; i < n; i++) {
                points[i] = finishLineEllipse[finishLineEllipse.ParStart + i*del];
            }
            var poly = new Polyline(points);
            return poly;
        }

        static bool DeltasAreBigEnough(double dx, double dy) {
            return Math.Sqrt(dx*dx + dy*dy) > 0.05/3;
        }


        public double CurrentScale {
            get { return ((MatrixTransform) _graphCanvas.RenderTransform).Matrix.M11; }
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
            OnMouseUp(e);
            clickCounter.AddMouseUp();
            if (_graphCanvas.IsMouseCaptured) {
                e.Handled = true;
                _graphCanvas.ReleaseMouseCapture();
            }
        }

        void OnMouseUp(MouseEventArgs e) {
            if (MouseUp != null)
                MouseUp(this, CreateMouseEventArgs(e));
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
            get { return ((MatrixTransform) _graphCanvas.RenderTransform).Matrix.OffsetX; }
        }

        protected double CurrentYOffset {
            get { return ((MatrixTransform) _graphCanvas.RenderTransform).Matrix.OffsetY; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double ZoomFactor {
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
            var ret = obj as IViewerObject;
            if (ret != null)
                return ret;

            return GetVEdgeOfRail((Rail) obj);
        }

        public void Invalidate(IViewerObject objectToInvalidate) {
            ((IInvalidatable) objectToInvalidate).Invalidate();
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings != null) {
                var edge = objectToInvalidate as VEdge;
                if (edge != null) {
                    SetEdgeAppearence(lgSettings, edge);
                } else {
                    var vnode = objectToInvalidate as VNode;
                    if (vnode != null)
                        SetNodeAppearence(vnode);
                }

            }
        }

        void SetNodeAppearence(VNode node) {
            node.Node.Attr.LineWidth = GetBorderPathThickness();
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
            return m*p;
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
            var menuItem = new MenuItem {Header = title};
            menuItem.Click += (RoutedEventHandler) (delegate { voidVoidDelegate(); });
            return menuItem;
        }

        public double UnderlyingPolylineCircleRadius {
            get { return 0.1*DpiX/CurrentScale; }
        }

        public Graph Graph {
            get { return drawingGraph; }
            set {
                drawingGraph = value;
                if (drawingGraph != null)
                    Console.WriteLine("starting processing a graph with {0} nodes and {1} edges", drawingGraph.NodeCount,
                        drawingGraph.EdgeCount);
                ProcessGraph();
            }
        }
//
//        void Dumpxy() {
//            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\tmp\dumpxy")) {
//                file.WriteLine("~nodes");
//                foreach (var node in Graph.Nodes) {
//                    var c = node.GeometryNode.Center;
//                    file.WriteLine("{0} {1} {2}", node.Id, c.X, c.Y);
//                }
//                file.WriteLine("~edges");
//                foreach (var edge in Graph.Edges)
//                {
//                    file.WriteLine("{0} {1}", edge.Source, edge.Target);
//                }
//            }
//        }
        bool LargeGraphBrowsing { get; set; }

        bool GraphIsLarge() {
            return drawingGraph != null && drawingGraph.NodeCount + drawingGraph.EdgeCount > LargeGraphCountThreshold;
        }


        const double DesiredPathThicknessInInches = 0.016;
        int largeGraphCountThreshold = 4000;

        readonly Dictionary<DrawingObject, Func<DrawingObject, FrameworkElement>> registeredCreators =
            new Dictionary<DrawingObject, Func<DrawingObject, FrameworkElement>>();

        readonly ClickCounter clickCounter;
        public string MsaglFileToSave;

        double GetBorderPathThickness() {
            return DesiredPathThicknessInInches*DpiX/CurrentScale;
        }

        readonly Object _processGraphLock=new object();
        void ProcessGraph() {
            lock (_processGraphLock) {
                ProcessGraphUnderLock();
            }            
        }

        void ProcessGraphUnderLock() {
            try {
                if (LayoutStarted != null)
                    LayoutStarted(null, null);

                CancelToken = new CancelToken();

                if (drawingGraph == null) return;

                LargeGraphBrowsing = GraphIsLarge();
                HideCanvas();
                ClearGraphViewer();
                if (!LargeGraphBrowsing)
                    CreateFrameworkElementsForLabelsOnly();
                if (LargeGraphBrowsing && MsaglFileToSave == null) {
                    LayoutEditingEnabled = false;
                    var lgsettings = new LgLayoutSettings(
                        () => new Rectangle(0, 0, _graphCanvas.RenderSize.Width, _graphCanvas.RenderSize.Height),
                        () => Transform, DpiX, DpiY, () => ArrowheadLength) {
                            ScreenRectangle = new Rectangle(0, 0, SystemParameters.WorkArea.Width,
                                SystemParameters.WorkArea.Height),
                            NeedToLayout = NeedToCalculateLayout,
                            MaxNumberNodesPerTile = DefaultLargeLayoutSettings.MaxNumberNodesPerTile,
                            ExitAfterInit = DefaultLargeLayoutSettings.ExitAfterInit,
                            BackgroundImage =  DefaultLargeLayoutSettings.BackgroundImage
                        };

                    drawingGraph.LayoutAlgorithmSettings = lgsettings;
                    lgsettings.ViewerChangeTransformAndInvalidateGraph +=
                        OGraphChanged;
                }
                if (NeedToCalculateLayout) {
                    drawingGraph.CreateGeometryGraph(); //forcing the layout recalculation
                    if (_graphCanvas.Dispatcher.CheckAccess())
                        PopulateGeometryOfGeometryGraph();
                    else
                        _graphCanvas.Dispatcher.Invoke(PopulateGeometryOfGeometryGraph);
                }

                geometryGraphUnderLayout = drawingGraph.GeometryGraph;
                if (RunLayoutAsync)
                    SetUpBackgrounWorkerAndRunAsync();
                else
                    RunLayoutInUIThread();
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
            }
        }

        void RunLayoutInUIThread()
        {
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
                } else if (CancelToken.Canceled) {
                    ClearGraphViewer();
                } else {
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
                    LayoutHelpers.CalculateLayout(geometryGraphUnderLayout, drawingGraph.LayoutAlgorithmSettings,
                                                  CancelToken);
                    if (MsaglFileToSave != null) {
                        drawingGraph.Write(MsaglFileToSave);
                        Console.WriteLine("saved into {0}", MsaglFileToSave);
                        Environment.Exit(0);
                    }
                } catch (OperationCanceledException) {
                    //swallow this exception
                }
            } else if (LargeGraphBrowsing)
                LayoutHelpers.InitLargeLayout(geometryGraphUnderLayout, drawingGraph.LayoutAlgorithmSettings,
                                              CancelToken);
        }

        void PostLayoutStep() {
            _graphCanvas.Visibility = Visibility.Visible;
            PushDataFromLayoutGraphToFrameworkElements();
            _backgroundWorker = null; //this will signal that we are not under layout anymore
            if (GraphChanged != null)
                GraphChanged(this, null);

            SetInitialTransform();
        }

/*
        void SubscribeToChangeVisualsEvents() {
            //            foreach(var cluster in drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf())
            //                cluster.Attr.VisualsChanged += AttrVisualsChanged;
            foreach (var edge in drawingGraph.Edges) {
                DrawingEdge edge1 = edge;
                edge.Attr.VisualsChanged += (a, b) => AttrVisualsChangedForEdge(edge1);
            }

            foreach (var node in drawingGraph.Nodes) {
                Drawing.Node node1 = node;
                node.Attr.VisualsChanged += (a, b) => AttrVisualsChangedForNode(node1);
            }

        }
*/

/*
        void AttrVisualsChangedForNode(Drawing.Node node) {
            IViewerObject viewerObject;
            if (drawingObjectsToIViewerObjects.TryGetValue(node, out viewerObject)) {
                var vNode = (VNode) viewerObject;
                if (vNode != null)
                    vNode.Invalidate();
            }
        }
*/


        void SetScaleRangeForLgLayoutSettings() {
            var lgSettings = (LgLayoutSettings) drawingGraph.LayoutAlgorithmSettings;
            var maximalZoomLevelForEdges = lgSettings.GetMaximalZoomLevel();

            var minSize = 5*drawingGraph.Nodes.Select(n => n.BoundingBox).Min(b => Math.Min(b.Width, b.Height));

            var nodeScale = Math.Min(drawingGraph.BoundingBox.Width, drawingGraph.BoundingBox.Height)/minSize;

            var gscale = Math.Max(nodeScale, maximalZoomLevelForEdges)*2;
            var fitFactor = FitFactor;
            lgSettings.ScaleInterval = new Interval(fitFactor/lgSettings.SattelliteZoomFactor, fitFactor*gscale);

        }


//        void SetupTimerOnViewChangeEvent(object sender, EventArgs e) {
//            SetupRoutingTimer();
//        }


        /// <summary>
        /// oGraph has changed too
        /// </summary>
        void OGraphChanged() {
            if (UnderLayout) return;
            var vDrawingEdges = new Set<DrawingEdge>();
            var vDrawingNodes = new Set<Drawing.Node>();
            foreach (var dro in drawingObjectsToIViewerObjects.Keys) {
                var n = dro as Drawing.Node;
                if (n != null)
                    vDrawingNodes.Insert(n);
                else {
                    var edge = dro as DrawingEdge;
                    if (edge != null)
                        vDrawingEdges.Insert((DrawingEdge) dro);
                }
            }
            var oDrawgingEdges = new Set<DrawingEdge>();
            var oDrawingNodes = new Set<Drawing.Node>();
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings == null) return;

            var railGraph = lgSettings.RailGraph;
            if (railGraph == null)
                return;
            foreach (var node in railGraph.Nodes)
                oDrawingNodes.Insert((Drawing.Node) node.UserData);
            foreach (var rail in railGraph.Rails)
                oDrawgingEdges.Insert((DrawingEdge) rail.TopRankedEdgeInfoOfTheRail.Edge.UserData);

            ProcessRemovalsForLg(vDrawingNodes - oDrawingNodes, vDrawingEdges - oDrawgingEdges);
            ProcessAdditionsForLg(oDrawingNodes - vDrawingNodes, oDrawgingEdges - vDrawingEdges, lgSettings);
            RemoveNoLongerVisibleRails(railGraph);

            CreateOrInvalidateFrameworksElementForVisibleRails(railGraph, lgSettings.EdgeTransparency);
           
            InvalidateNodesOfRailGraph();

            _rectToFillGraphBackground.Visibility = lgSettings.BackgroundImageIsHidden ? Visibility.Hidden : Visibility.Visible;
        }
        
        void InvalidateNodesOfRailGraph() {
            foreach (var o in drawingObjectsToIViewerObjects.Values) {
                var vNode = o as VNode;
                if (vNode != null)
                    vNode.Invalidate();
            }
        }

        void CreateOrInvalidateFrameworksElementForVisibleRails(RailGraph railGraph, byte edgeTransparency) {
            foreach(var rail in railGraph.Rails)
                CreateOrInvalidateFrameworksElementForVisibleRail(rail, edgeTransparency);
        }

        void CreateOrInvalidateFrameworksElementForVisibleRail(Rail rail, byte edgeTransparency) {
            FrameworkElement fe;
            VEdge vEdgeOfRail = GetVEdgeOfRail(rail);
            if (visibleRailsToFrameworkElems.TryGetValue(rail, out fe)) {
                vEdgeOfRail.Invalidate(fe,rail, edgeTransparency);
            } else {
                fe = vEdgeOfRail.CreateFrameworkElementForRail(rail, edgeTransparency);
                _graphCanvas.Children.Add(fe);
                visibleRailsToFrameworkElems[rail] = fe;               
            }

        }


        VEdge GetVEdgeOfRail(Rail rail) {
            var dEdge = (Drawing.Edge)rail.TopRankedEdgeInfoOfTheRail.Edge.UserData;
            IViewerObject vEdge;
            if (drawingObjectsToIViewerObjects.TryGetValue(dEdge, out vEdge))
                return (VEdge) vEdge;
            return null;
        }


        void RemoveNoLongerVisibleRails(RailGraph oGraph) {
            var railsToRemove = new List<Rail>();
            foreach (var rail in visibleRailsToFrameworkElems.Keys)
                if (!oGraph.Rails.Contains(rail))
                    railsToRemove.Add(rail);

            RemoveRustyRails(railsToRemove);
        }

        void RemoveRustyRails(List<Rail> railsToRemove) {
            foreach (var rail in railsToRemove) {
                var fe = visibleRailsToFrameworkElems[rail];
                _graphCanvas.Children.Remove(fe);
                visibleRailsToFrameworkElems.Remove(rail);
            }
        }


        /*
                void TestCorrectness(GeometryGraph oGraph, Set<Drawing.Node> oDrawingNodes, Set<DrawingEdge> oDrawgingEdges) {
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


        void ProcessAdditionsForLg(Set<Drawing.Node> nodesToAdd, Set<DrawingEdge> edgesToAdd, LgLayoutSettings lgSettings) {
            if (nodesToAdd != null)
                ProcessNodeAdditions(nodesToAdd);
            if (edgesToAdd != null)
                ProcessEdgeAdditionsForLg(edgesToAdd, lgSettings);
        }

        void ProcessEdgeAdditionsForLg(Set<DrawingEdge> edgesToAdd, LgLayoutSettings lgSettings) {
            foreach (var drawingEdge in edgesToAdd)
                CreateEdge(drawingEdge, lgSettings);
        }

        void ProcessNodeAdditions(Set<Drawing.Node> nodesToAdd) {
            foreach (var drawingNode in nodesToAdd)
                CreateVNode(drawingNode);
        }

        void ProcessRemovalsForLg(Set<Drawing.Node> nodesToRemove, Set<DrawingEdge> edgesToRemove) {
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

            IViewerObject vgedge;
            if (drawingObjectsToIViewerObjects.TryGetValue(drawingEdge, out vgedge)) {
                var vEdge = (VEdge) vgedge;
                _graphCanvas.Children.Remove(vEdge.CurvePath);
                if (vEdge.LabelFrameworkElement != null)
                    _graphCanvas.Children.Remove(vEdge.LabelFrameworkElement);
                if (vEdge.SourceArrowHeadPath != null) {
                    _graphCanvas.Children.Remove(vEdge.SourceArrowHeadPath);
                }
                if (vEdge.TargetArrowHeadPath != null) {
                    _graphCanvas.Children.Remove(vEdge.TargetArrowHeadPath);
                }
                drawingObjectsToIViewerObjects.Remove(drawingEdge);
                drawingObjectsToFrameworkElements.Remove(drawingEdge);
                drawingEdge.Attr = vEdge.EdgeAttrClone;
            }
        }

        void RemoveVNode(Drawing.Node drawingNode) {
            lock (this) {
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
                //                IViewerObject vedge;
                //                if (!drawingObjectsToIViewerObjects.TryGetValue(edge, out vedge)) continue;
                //                
                //                graphCanvas.Children.Remove(((VEdge)vedge).Path);
                //                drawingObjectsToIViewerObjects.Remove(edge);
                //            }
                var vnode = (VNode) drawingObjectsToIViewerObjects[drawingNode];
                foreach (var fe in vnode.FrameworkElements)
                    _graphCanvas.Children.Remove(fe);
                drawingObjectsToIViewerObjects.Remove(drawingNode);
                drawingObjectsToFrameworkElements.Remove(drawingNode);
            }
        }


        public int LargeGraphCountThreshold {
            get { return largeGraphCountThreshold; }
            set { largeGraphCountThreshold = value; }
        }

        /// <summary>
        /// creates a viewer node
        /// </summary>
        /// <param name="drawingNode"></param>
        /// <returns></returns>
        public IViewerNode CreateIViewerNode(Drawing.Node drawingNode) {
            var frameworkElement = CreateTextBlockForDrawingObj(drawingNode);
            var width = frameworkElement.Width + 2*drawingNode.Attr.LabelMargin;
            var height = frameworkElement.Height + 2*drawingNode.Attr.LabelMargin;
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
            visibleRailsToFrameworkElems.Clear();

        }

        void ClearGraphCanvasChildren() {
            if (_graphCanvas.Dispatcher.CheckAccess())
                _graphCanvas.Children.Clear();
            else _graphCanvas.Dispatcher.Invoke(() => _graphCanvas.Children.Clear());
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

        //        void TransferLayoutDataToWpf() {
        //            PushDataFromLayoutGraphToFrameworkElements();
        //            graphCanvas.Visibility = Visibility.Visible;
        //            SetInitialTransform();
        //        }
        /// <summary>
        /// zooms to the default view
        /// </summary>
        public void SetInitialTransform() {
            if (drawingGraph == null || GeomGraph == null) return;

            var scale = FitFactor;
            var graphCenter = GeomGraph.BoundingBox.Center;
            var vp = new Rectangle(new Point(0, 0),
                                   new Point(_graphCanvas.RenderSize.Width, _graphCanvas.RenderSize.Height));
            if (LargeGraphBrowsing) {
                SetTransformOnViewport(scale, graphCenter, vp);
                SetScaleRangeForLgLayoutSettings();
            } else
                SetTransformOnViewportWithoutRaisingViewChangeEvent(scale, graphCenter, vp);
        }

        public Image DrawImage(string fileName)
        {
            var ltrans = _graphCanvas.LayoutTransform;
            var rtrans = _graphCanvas.RenderTransform;
            _graphCanvas.LayoutTransform = null;
            _graphCanvas.RenderTransform = null;
            var renderSize = _graphCanvas.RenderSize;

            double scale = FitFactor;
            int w = (int)(this.GeomGraph.Width * scale);
            int h = (int)(GeomGraph.Height * scale);

            SetTransformOnViewportWithoutRaisingViewChangeEvent(scale, GeomGraph.BoundingBox.Center, new Rectangle(0, 0, w, h));
            
            Size size = new Size(w, h);
            // Measure and arrange the surface
            // VERY IMPORTANT
            _graphCanvas.Measure(size);
            _graphCanvas.Arrange(new Rect(size));
            
            foreach (var node in drawingGraph.Nodes.Concat(drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf()))
            {
                IViewerObject o;
                if (drawingObjectsToIViewerObjects.TryGetValue(node, out o))
                {
                    ((VNode)o).Invalidate();
                }
            }

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(w, h, DpiX, DpiY, PixelFormats.Pbgra32);
            renderBitmap.Render(_graphCanvas);

            if (fileName != null)
                // Create a file stream for saving image
                using (System.IO.FileStream outStream = new System.IO.FileStream(fileName, System.IO.FileMode.Create))
                {
                    // Use png encoder for our data
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    // push the rendered bitmap to it
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    // save the data to the stream
                    encoder.Save(outStream);
                }

            _graphCanvas.LayoutTransform = ltrans;
            _graphCanvas.RenderTransform = rtrans;
            _graphCanvas.Measure(renderSize);
            _graphCanvas.Arrange(new Rect(renderSize));

            return new Image { Source = renderBitmap };
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
            //_graphCanvas.Measure(new Size(vp.Width, vp.Height));
            //_graphCanvas.Arrange(new System.Windows.Rect(new WpfPoint(vp.Left, vp.Bottom), new Size(vp.Width, vp.Height)));
        }

        
        

/*
        void FixArrowheads(LgLayoutSettings lgSettings) {
            const double arrowheadRatioToBoxDiagonal = 0.3;
            var maximalArrowheadLength = lgSettings.MaximalArrowheadLength();
            if (lgSettings.OGraph == null) return;
            foreach (Edge geomEdge in lgSettings.OGraph.Edges) {

                var edge = (DrawingEdge) geomEdge.UserData;
                var vEdge = (VEdge) drawingObjectsToIViewerObjects[edge];

                if (geomEdge.EdgeGeometry.SourceArrowhead != null) {
                    var origLength = vEdge.EdgeAttrClone.ArrowheadLength;
                    geomEdge.EdgeGeometry.SourceArrowhead.Length =
                        Math.Min(Math.Min(origLength, maximalArrowheadLength),
                                 geomEdge.Source.BoundingBox.Diagonal*arrowheadRatioToBoxDiagonal);
                }
                if (geomEdge.EdgeGeometry.TargetArrowhead != null) {
                    var origLength = vEdge.EdgeAttrClone.ArrowheadLength;
                    geomEdge.EdgeGeometry.TargetArrowhead.Length =
                        Math.Min(Math.Min(origLength, maximalArrowheadLength),
                                 geomEdge.Target.BoundingBox.Diagonal*arrowheadRatioToBoxDiagonal);
                }
            }
        }
*/



        public Rectangle ClientViewportMappedToGraph {
            get {
                var t = Transform.Inverse;
                var p0 = new Point(0, 0);
                var p1 = new Point(_graphCanvas.RenderSize.Width, _graphCanvas.RenderSize.Height);
                return new Rectangle(t*p0, t*p1);
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
            if (LargeGraphBrowsing)
                return !((LgLayoutSettings) drawingGraph.LayoutAlgorithmSettings).ScaleInterval.Contains(scale);
            return scale < 0.000001 || scale > 100000.0; //todo: remove hardcoded values
        }


        double FitFactor {
            get {
                var geomGraph = GeomGraph;
                if (drawingGraph == null || geomGraph == null ||

                    geomGraph.Width == 0 || geomGraph.Height == 0)
                    return 1;

                var size = _graphCanvas.RenderSize;

                return GetFitFactor(size);
            }
        }

        double GetFitFactor(Size rect) {
            var geomGraph = GeomGraph;
            return geomGraph == null ? 1 : Math.Min(rect.Width/geomGraph.Width, rect.Height/geomGraph.Height);
        }

        void PushDataFromLayoutGraphToFrameworkElements() {
            CreateRectToFillCanvas();
            CreateAndPositionGraphBackgroundRectangle();
                
            if (LargeGraphBrowsing)
                OGraphChanged();
            else {
                CreateVNodes();
                CreateEdges();
            }
        }

        void CreateRectToFillCanvas() {
            var parent = (Panel) GraphCanvas.Parent;
            _rectToFillCanvas = new System.Windows.Shapes.Rectangle();
            Canvas.SetLeft(_rectToFillCanvas, 0);
            Canvas.SetTop(_rectToFillCanvas, 0);
            _rectToFillCanvas.Width = parent.ActualWidth;
            _rectToFillCanvas.Height = parent.ActualHeight;

            _rectToFillCanvas.Fill = Brushes.Transparent;
            Panel.SetZIndex(_rectToFillCanvas, -2);
            _graphCanvas.Children.Add(_rectToFillCanvas);
        }




        void CreateEdges() {
            foreach (var edge in drawingGraph.Edges)
                CreateEdge(edge, null);
        }

        VEdge CreateEdge(DrawingEdge edge, LgLayoutSettings lgSettings) {
            lock (this) {
                if (drawingObjectsToIViewerObjects.ContainsKey(edge))
                    return (VEdge) drawingObjectsToIViewerObjects[edge];
                if (lgSettings != null)
                    return CreateEdgeForLgCase(lgSettings, edge);

                FrameworkElement labelTextBox;
                drawingObjectsToFrameworkElements.TryGetValue(edge, out labelTextBox);
                var vEdge = new VEdge(edge, labelTextBox);

                var zIndex = ZIndexOfEdge(edge);
                drawingObjectsToIViewerObjects[edge] = vEdge;

                if (edge.Label != null)
                    SetVEdgeLabel(edge, vEdge, zIndex);

                Panel.SetZIndex(vEdge.CurvePath, zIndex);
                _graphCanvas.Children.Add(vEdge.CurvePath);
                SetVEdgeArrowheads(vEdge, zIndex);

                return vEdge;
            }
        }

        int ZIndexOfEdge(DrawingEdge edge) {
            var source = (VNode) drawingObjectsToIViewerObjects[edge.SourceNode];
            var target = (VNode) drawingObjectsToIViewerObjects[edge.TargetNode];

            var zIndex = Math.Max(source.ZIndex, target.ZIndex) + 1;
            return zIndex;
        }

        VEdge CreateEdgeForLgCase(LgLayoutSettings lgSettings, DrawingEdge edge) {
            return (VEdge) (drawingObjectsToIViewerObjects[edge] = new VEdge(edge, lgSettings) {
                PathStrokeThicknessFunc = () => GetBorderPathThickness()*edge.Attr.LineWidth
            });
        }

        void SetVEdgeLabel(DrawingEdge edge, VEdge vEdge, int zIndex) {
            FrameworkElement frameworkElementForEdgeLabel;
            if (!drawingObjectsToFrameworkElements.TryGetValue(edge, out frameworkElementForEdgeLabel)) {
                drawingObjectsToFrameworkElements[edge] =
                    frameworkElementForEdgeLabel = CreateTextBlockForDrawingObj(edge);
                frameworkElementForEdgeLabel.Tag = new VLabel(edge, frameworkElementForEdgeLabel);
            }

            vEdge.VLabel = (VLabel) frameworkElementForEdgeLabel.Tag;
            if (frameworkElementForEdgeLabel.Parent == null) {
                _graphCanvas.Children.Add(frameworkElementForEdgeLabel);
                Panel.SetZIndex(frameworkElementForEdgeLabel, zIndex);
            }
        }

        void SetVEdgeArrowheads(VEdge vEdge, int zIndex) {
            if (vEdge.SourceArrowHeadPath != null) {
                Panel.SetZIndex(vEdge.SourceArrowHeadPath, zIndex);
                _graphCanvas.Children.Add(vEdge.SourceArrowHeadPath);
            }
            if (vEdge.TargetArrowHeadPath != null) {
                Panel.SetZIndex(vEdge.TargetArrowHeadPath, zIndex);
                _graphCanvas.Children.Add(vEdge.TargetArrowHeadPath);
            }
        }

        void SetEdgeAppearence(LgLayoutSettings lgSettings, VEdge vEdge) {

            var lgEdgeInfo = lgSettings.GeometryEdgesToLgEdgeInfos[((DrawingEdge) vEdge.DrawingObject).GeometryEdge];

            if (lgEdgeInfo.SlidingZoomLevel == 0) {
                vEdge.Edge.Attr.LineWidth = 4*GetBorderPathThickness();
                vEdge.Edge.Attr.Color = Drawing.Color.Blue;
            } else {
                var attr = vEdge.EdgeAttrClone;
                vEdge.Edge.Attr.LineWidth = GetBorderPathThickness()*attr.LineWidth/2;
                var sf = lgSettings.SattelliteZoomFactor;
                // sf => 0
                // 1 => 255;    
                double alpha = lgEdgeInfo.ZoomLevel/ZoomFactor;
                if (alpha < 1)
                    alpha = 1;
                else if (alpha > sf)
                    alpha = sf;
                const double opaque = 255.0;
                double k = opaque/(1 - sf);
                double b = -k*sf;
                vEdge.Edge.Attr.Color = new Drawing.Color((byte) (k*alpha + b), attr.Color.R, attr.Color.G, attr.Color.B);
            }
        }

        void CreateVNodesWithLowTransparency() {
            foreach (var node in drawingGraph.Nodes.Concat(drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf()))
            {
                VNode vnode = (VNode)CreateVNode(node);
                vnode.SetLowTransparency();
            }
        }

        void CreateVNodes()
        {
            foreach (var node in drawingGraph.Nodes.Concat(drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf()))
            {
                CreateVNode(node);
            }
        }

        IViewerNode CreateVNode(Drawing.Node node) {
            lock (this) {
                if (drawingObjectsToIViewerObjects.ContainsKey(node))
                    return (IViewerNode) drawingObjectsToIViewerObjects[node];

                FrameworkElement feOfLabel;
                if (!drawingObjectsToFrameworkElements.TryGetValue(node, out feOfLabel))
                    feOfLabel = CreateAndRegisterFrameworkElementOfDrawingNode(node);

                var vn = new VNode(node, GetCorrespondingLgNode(node), feOfLabel,
                    e => (VEdge)drawingObjectsToIViewerObjects[e], () => GetBorderPathThickness() * node.Attr.LineWidth);

                foreach (var fe in vn.FrameworkElements)
                    _graphCanvas.Children.Add(fe);

                drawingObjectsToIViewerObjects[node] = vn;

                #region commented out animation

                /* //playing with the animation
                p.Fill = Brushes.Green;

                SolidColorBrush brush = new SolidColorBrush();
                p.Fill = brush;
                ColorAnimation ca = new ColorAnimation(Colors.Green, Colors.White, new Duration(TimeSpan.FromMilliseconds(3000)));
                //Storyboard sb = new Storyboard();
                //Storyboard.SetTargetProperty(ca, new PropertyPath("Color"));
                //Storyboard.SetTarget(ca, brush);            
                //sb.Children.Add(ca);
                //sb.Begin(p);
                brush.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                */

                #endregion

                return vn;
            }
        }

        public FrameworkElement CreateAndRegisterFrameworkElementOfDrawingNode(Drawing.Node node) {
            lock (this)
                return drawingObjectsToFrameworkElements[node] = CreateTextBlockForDrawingObj(node);
        }


        LgNodeInfo GetCorrespondingLgNode(Drawing.Node node) {
            var lgGraphBrowsingSettings = drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings;
            return lgGraphBrowsingSettings == null
                       ? null
                       : lgGraphBrowsingSettings.GeometryNodesToLgNodeInfos[node.GeometryNode];
        }

        void CreateAndPositionGraphBackgroundRectangle() {
            CreateGraphBackgroundRect();
            SetBackgroundRectanglePositionAndSize();

            var rect = _rectToFillGraphBackground as System.Windows.Shapes.Rectangle;
            if (rect != null)
            {
                rect.Fill = Common.BrushFromMsaglColor(drawingGraph.Attr.BackgroundColor);
                //rect.Fill = Brushes.Green;
            }
            Panel.SetZIndex(_rectToFillGraphBackground, -1);
            _graphCanvas.Children.Add(_rectToFillGraphBackground);
        }

        void CreateGraphBackgroundRect() {
            var lgGraphBrowsingSettings = drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgGraphBrowsingSettings == null) {
                _rectToFillGraphBackground = new System.Windows.Shapes.Rectangle();
            }
            else {
                if (String.IsNullOrEmpty(lgGraphBrowsingSettings.BackgroundImage))
                {
                    CreateVNodesWithLowTransparency();
                    _rectToFillGraphBackground = DrawImage(null);
                    foreach (var node in drawingGraph.Nodes.Concat(drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf()))
                    {
                        RemoveVNode(node);
                    }
                }
                else
                    _rectToFillGraphBackground = CreateImage(lgGraphBrowsingSettings.BackgroundImage);
            }

        }


        FrameworkElement CreateImage(string createBackgroundFromImage)
        {
            var bmpSource = new BitmapImage(new Uri(createBackgroundFromImage));
            var img = new Image { Source = bmpSource };
            return img;
        }

        void SetBackgroundRectanglePositionAndSize() {
            if (GeomGraph == null) return;
//            Canvas.SetLeft(_rectToFillGraphBackground, geomGraph.Left);
//            Canvas.SetTop(_rectToFillGraphBackground, geomGraph.Bottom);
            _rectToFillGraphBackground.Width = GeomGraph.Width;
            _rectToFillGraphBackground.Height = GeomGraph.Height;
            
            var center = GeomGraph.BoundingBox.Center;
            Common.PositionFrameworkElement(_rectToFillGraphBackground, center, 1);
        }


        void PopulateGeometryOfGeometryGraph() {
            geometryGraphUnderLayout = drawingGraph.GeometryGraph;
            foreach (
                Node msaglNode in
                    geometryGraphUnderLayout.Nodes) {
                var node = (Drawing.Node) msaglNode.UserData;
                if (_graphCanvas.Dispatcher.CheckAccess())
                    msaglNode.BoundaryCurve = GetNodeBoundaryCurve(node);
                else {
                    var msagNodeInThread = msaglNode;
                    _graphCanvas.Dispatcher.Invoke(() => msagNodeInThread.BoundaryCurve = GetNodeBoundaryCurve(node));
                }
                //AssignLabelWidthHeight(msaglNode, msaglNode.UserData as DrawingObject);
            }

            foreach (
                Cluster cluster in geometryGraphUnderLayout.RootCluster.AllClustersWideFirstExcludingSelf()) {
                var subgraph = (Subgraph) cluster.UserData;
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
                                                        subgraph.Attr.LineWidth/2;
                //AssignLabelWidthHeight(msaglNode, msaglNode.UserData as DrawingObject);
            }

            foreach (var msaglEdge in geometryGraphUnderLayout.Edges) {
                var drawingEdge = (DrawingEdge) msaglEdge.UserData;
                AssignLabelWidthHeight(msaglEdge, drawingEdge);
            }
        }

        ICurve GetClusterCollapsedBoundary(Subgraph subgraph) {
            double width, height;

            FrameworkElement fe;
            if (drawingObjectsToFrameworkElements.TryGetValue(subgraph, out fe)) {

                width = fe.Width + 2*subgraph.Attr.LabelMargin + subgraph.DiameterOfOpenCollapseButton;
                height = Math.Max(fe.Height + 2*subgraph.Attr.LabelMargin, subgraph.DiameterOfOpenCollapseButton);
            } else
                return GetApproximateCollapsedBoundary(subgraph);

            if (width < drawingGraph.Attr.MinNodeWidth)
                width = drawingGraph.Attr.MinNodeWidth;
            if (height < drawingGraph.Attr.MinNodeHeight)
                height = drawingGraph.Attr.MinNodeHeight;
            return NodeBoundaryCurves.GetNodeBoundaryCurve(subgraph, width, height);
        }

        ICurve GetApproximateCollapsedBoundary(Subgraph subgraph) {
            if (textBoxForApproxNodeBoundaries == null)
                SetUpTextBoxForApproxNodeBoundaries();


            double width, height;
            if (String.IsNullOrEmpty(subgraph.LabelText))
                height = width = subgraph.DiameterOfOpenCollapseButton;
            else {
                double a = ((double) subgraph.LabelText.Length)/textBoxForApproxNodeBoundaries.Text.Length*
                           ((double) subgraph.Label.FontSize)/Label.DefaultFontSize;
                width = textBoxForApproxNodeBoundaries.Width*a + subgraph.DiameterOfOpenCollapseButton;
                height =
                    Math.Max(
                        textBoxForApproxNodeBoundaries.Height*subgraph.Label.FontSize/Label.DefaultFontSize,
                        subgraph.DiameterOfOpenCollapseButton);
            }

            if (width < drawingGraph.Attr.MinNodeWidth)
                width = drawingGraph.Attr.MinNodeWidth;
            if (height < drawingGraph.Attr.MinNodeHeight)
                height = drawingGraph.Attr.MinNodeHeight;

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
                width = fe.Width + 2*node.Attr.LabelMargin;
                height = fe.Height + 2*node.Attr.LabelMargin;
            } else
                return GetNodeBoundaryCurveByMeasuringText(node);

            if (width < drawingGraph.Attr.MinNodeWidth)
                width = drawingGraph.Attr.MinNodeWidth;
            if (height < drawingGraph.Attr.MinNodeHeight)
                height = drawingGraph.Attr.MinNodeHeight;
            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }
       
        static void CreateBareTextBlock(Label drawingLabel, out double width, out double height)
        {//todo, for optimizations look here http://smellegantcode.wordpress.com/2008/07/03/glyphrun-and-so-forth/
            var textBlock = new TextBlock
            {
                Text = drawingLabel.Text,
                FontFamily = new FontFamily(drawingLabel.FontName),
                FontSize = drawingLabel.FontSize,                
            };

            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            width= textBlock.DesiredSize.Width;
            height = textBlock.DesiredSize.Height;            
        }

        TextBlock textBoxForApproxNodeBoundaries;
        int _prevGrowingParDebug=0;
      
        public static Size MeasureText(string text,
        FontFamily family, double size)
        {

            
            FormattedText formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(family, new System.Windows.FontStyle(), FontWeights.Regular, FontStretches.Normal),
                size,
                Brushes.Black,
                null);

            return new Size(formattedText.Width, formattedText.Height);
        }

        ICurve GetNodeBoundaryCurveByMeasuringText(Drawing.Node node)
        {
            double width, height;
            if (String.IsNullOrEmpty(node.LabelText))
            {
                width = 10;
                height = 10;
            }
            else
            {
                var size = MeasureText(node.LabelText, new FontFamily(node.Label.FontName), node.Label.FontSize);
                width = size.Width;
                height = size.Height;
            }

            width += 2 * node.Attr.LabelMargin;
            height += 2 * node.Attr.LabelMargin;

            if (width < drawingGraph.Attr.MinNodeWidth)
                width = drawingGraph.Attr.MinNodeWidth;
            if (height < drawingGraph.Attr.MinNodeHeight)
                height = drawingGraph.Attr.MinNodeHeight;

            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }


        void SetUpTextBoxForApproxNodeBoundaries() {
            textBoxForApproxNodeBoundaries = new TextBlock
                {
                    Text = "Fox jumping over River",
                    FontFamily = new FontFamily(Label.DefaultFontName),
                    FontSize = Label.DefaultFontSize,
                };

            textBoxForApproxNodeBoundaries.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            textBoxForApproxNodeBoundaries.Width = textBoxForApproxNodeBoundaries.DesiredSize.Width;
            textBoxForApproxNodeBoundaries.Height = textBoxForApproxNodeBoundaries.DesiredSize.Height;
        }


        void CreateFrameworkElementsForLabelsOnly() {
            foreach (var edge in drawingGraph.Edges) {
                var fe = CreateDefaultFrameworkElementForDrawingObject(edge);
                if (fe != null)
                    if (_graphCanvas.Dispatcher.CheckAccess())
                        fe.Tag = new VLabel(edge, fe);
                    else {
                        var localEdge = edge;
                        _graphCanvas.Dispatcher.Invoke(() => fe.Tag = new VLabel(localEdge, fe));
                    }
            }

            foreach (var node in drawingGraph.Nodes)
                CreateDefaultFrameworkElementForDrawingObject(node);
            if (drawingGraph.RootSubgraph != null)
                foreach (var subgraph in drawingGraph.RootSubgraph.AllSubgraphsWidthFirstExcludingSelf())
                    CreateDefaultFrameworkElementForDrawingObject(subgraph);
        }

        //        void CreateFrameworkElementForEdgeLabel(DrawingEdge edge) {
        //            var textBlock = CreateTextBlockForDrawingObj(edge);
        //            if (textBlock == null) return;
        //            drawingGraphObjectsToTextBoxes[edge] = textBlock;            
        //            textBlock.Tag = new VLabel(edge, textBlock);
        //        }

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
            var textBlock = new TextBlock
                {
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
            _graphCanvas.Children.Remove(targetArrowheadPathForRubberEdge);
            targetArrowheadPathForRubberEdge = null;
        }

        public void AddEdge(IViewerEdge edge, bool registerForUndo) {
            //if (registerForUndo) drawingLayoutEditor.RegisterEdgeAdditionForUndo(edge);

            var drawingEdge = edge.Edge;
            Edge geomEdge = drawingEdge.GeometryEdge;

            drawingGraph.AddPrecalculatedEdge(drawingEdge);
            drawingGraph.GeometryGraph.Edges.Add(geomEdge);

        }

        public IViewerEdge CreateEdgeWithGivenGeometry(DrawingEdge drawingEdge) {
            return CreateEdge(drawingEdge, drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings);
        }

        public void AddNode(IViewerNode node, bool registerForUndo) {
            if (drawingGraph == null)
                throw new InvalidOperationException(); // adding a node when the graph does not exist
            var vNode = (VNode) node;
            drawingGraph.AddNode(vNode.Node);
            drawingGraph.GeometryGraph.Nodes.Add(vNode.Node.GeometryNode);
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
                var vedge = (VEdge) edge;
                var dedge = vedge.Edge;
                drawingGraph.RemoveEdge(dedge);
                drawingGraph.GeometryGraph.Edges.Remove(dedge.GeometryEdge);
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
                var vnode = (VNode) node;
                vnode.DetouchFromCanvas(_graphCanvas);

                drawingGraph.RemoveNode(node.Node);
                drawingGraph.GeometryGraph.Nodes.Remove(node.Node.GeometryNode);
                layoutEditor.DetachNode(node);
                layoutEditor.CleanObstacles();
            }
        }

        void RemoveEdges(IEnumerable<DrawingEdge> drawingEdges) {
            foreach (var de in drawingEdges.ToArray()) {
                var vedge = (VEdge) drawingObjectsToIViewerObjects[de];
                RemoveEdge(vedge, false);
            }
        }


        public IViewerEdge RouteEdge(DrawingEdge drawingEdge) {
            var geomEdge = GeometryGraphCreator.CreateGeometryEdgeFromDrawingEdge(drawingEdge);
            var geomGraph = drawingGraph.GeometryGraph;
            LayoutHelpers.RouteAndLabelEdges(geomGraph, drawingGraph.LayoutAlgorithmSettings, new[] {geomEdge});
            return CreateEdge(drawingEdge, drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings);
        }

        public IViewerGraph ViewerGraph { get; set; }

        public double ArrowheadLength {
            get { return 0.2*DpiX/CurrentScale; }
        }

        public void SetSourcePortForEdgeRouting(Point portLocation) {
            _sourcePortLocationForEdgeRouting = portLocation;
            if (_sourcePortCircle == null) {
                _sourcePortCircle = CreatePortPath();
                _graphCanvas.Children.Add(_sourcePortCircle);
            }
            _sourcePortCircle.Width = _sourcePortCircle.Height = UnderlyingPolylineCircleRadius;
            _sourcePortCircle.StrokeThickness = _sourcePortCircle.Width/10;
            Common.PositionFrameworkElement(_sourcePortCircle, portLocation, 1);
        }

        System.Windows.Shapes.Ellipse CreatePortPath() {
            return new System.Windows.Shapes.Ellipse
                {
                    Stroke = Brushes.Brown,
                    Fill = Brushes.Brown,
                };
        }

        
        
        public void SetTargetPortForEdgeRouting(Point portLocation) {
            if (TargetPortCircle == null) {
                TargetPortCircle = CreatePortPath();
                _graphCanvas.Children.Add(TargetPortCircle);
            }
            TargetPortCircle.Width = TargetPortCircle.Height = UnderlyingPolylineCircleRadius;
            TargetPortCircle.StrokeThickness = TargetPortCircle.Width/10;
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
                _rubberEdgePath = new Path
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = GetBorderPathThickness()*3
                    };
                _graphCanvas.Children.Add(_rubberEdgePath);
                targetArrowheadPathForRubberEdge = new Path
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = GetBorderPathThickness()*3
                    };
                _graphCanvas.Children.Add(targetArrowheadPathForRubberEdge);
            }
            _rubberEdgePath.Data = VEdge.GetICurveWpfGeometry(edgeGeometry.Curve);
            targetArrowheadPathForRubberEdge.Data = VEdge.DefiningTargetArrowHead(edgeGeometry,
                                                                                  edgeGeometry.LineWidth);
        }

        
        bool UnderLayout {
            get { return _backgroundWorker != null; }
        }

        public void StopDrawingRubberEdge() {
            _graphCanvas.Children.Remove(_rubberEdgePath);
            _graphCanvas.Children.Remove(targetArrowheadPathForRubberEdge);
            _rubberEdgePath = null;
            targetArrowheadPathForRubberEdge = null;
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
            _graphCanvas.RenderTransform = new MatrixTransform(value[0, 0], value[0, 1], value[1, 0], value[1, 1],
                                                              value[0, 2],
                                                              value[1, 2]);
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
                _rubberLinePath = new Path
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = GetBorderPathThickness()*3
                    };
                _graphCanvas.Children.Add(_rubberLinePath);
                //                targetArrowheadPathForRubberLine = new Path {
                //                    Stroke = Brushes.Black,
                //                    StrokeThickness = GetBorderPathThickness()*3
                //                };
                //                graphCanvas.Children.Add(targetArrowheadPathForRubberLine);
            }
            _rubberLinePath.Data =
                VEdge.GetICurveWpfGeometry(new LineSegment(_sourcePortLocationForEdgeRouting, rubberEnd));
        }

        public void StartDrawingRubberLine(Point startingPoint) {
        }

        #endregion



        public IViewerNode CreateIViewerNode(Drawing.Node drawingNode, Point center, object visualElement) {
            if (drawingGraph == null)
                return null;
            var frameworkElement = visualElement as FrameworkElement ?? CreateTextBlockForDrawingObj(drawingNode);
            var width = frameworkElement.Width + 2*drawingNode.Attr.LabelMargin;
            var height = frameworkElement.Height + 2*drawingNode.Attr.LabelMargin;
            var bc = NodeBoundaryCurves.GetNodeBoundaryCurve(drawingNode, width, height);
            drawingNode.GeometryNode = new Node(bc, drawingNode) {Center = center};
            var vNode = CreateVNode(drawingNode);
            drawingGraph.AddNode(drawingNode);
            drawingGraph.GeometryGraph.Nodes.Add(drawingNode.GeometryNode);
            layoutEditor.AttachLayoutChangeEvent(vNode);
            MakeRoomForNewNode(drawingNode);

            return vNode;
        }

        void MakeRoomForNewNode(Drawing.Node drawingNode) {
            IncrementalDragger incrementalDragger = new IncrementalDragger(new[] {drawingNode.GeometryNode},
                                                                           Graph.GeometryGraph,
                                                                           Graph.LayoutAlgorithmSettings);
            incrementalDragger.Drag(new Point());

            foreach (var n in incrementalDragger.ChangedGraph.Nodes) {
                var dn = (Drawing.Node) n.UserData;
                var vn = this.drawingObjectsToIViewerObjects[dn] as VNode;
                if (vn != null)
                    vn.Invalidate();
            }

            foreach (var n in incrementalDragger.ChangedGraph.Edges) {
                var dn = (Drawing.Edge) n.UserData;
                var ve = drawingObjectsToIViewerObjects[dn] as VEdge;
                if (ve != null)
                    ve.Invalidate();
            }
        }
    }
}