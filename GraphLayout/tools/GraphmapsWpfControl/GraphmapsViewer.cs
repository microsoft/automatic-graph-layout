using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Miscellaneous.RegularGrid;
using Color = Microsoft.Msagl.Drawing.Color;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Ellipse = System.Windows.Shapes.Ellipse;
using Label = Microsoft.Msagl.Drawing.Label;
using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using ModifierKeys = Microsoft.Msagl.Drawing.ModifierKeys;
using Node = Microsoft.Msagl.Drawing.Node;
using Path = System.Windows.Shapes.Path;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Size = System.Windows.Size;
using Timer = Microsoft.Msagl.DebugHelpers.Timer;
using WpfPoint = System.Windows.Point;
using WPolyline = System.Windows.Shapes.Polyline;
using WpfRectangle = System.Windows.Shapes.Rectangle;
using Triple = System.Tuple<int, int, int>;


namespace Microsoft.Msagl.GraphmapsWpfControl
{
    public class GraphmapsViewer : IViewer
    {
        TextBlock textBoxForApproxNodeBoundaries;
        HitTestHandler _hitTestHandler;
        bool _panning;

        LgStringFinder _stringFinder;
        TileFetcher _tileFetcher;
        int _frame;


        int _layer;
        List<LgNodeInfo> SelectedNodeSet = new List<LgNodeInfo>();


        LgLayoutSettings _lgLayoutSettings;


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

        // roman: control points for editing rail
        Set<Rail> _selectedRails = new Set<Rail>();





        //jyoti controller
        private double pathThicknessController = 0.012;
        private int BackGroundEdgeWeight = 1;






        // Set<VNode> _selectedVnodes = new Set<VNode>();
        Set<LgNodeInfo> SelectedNodeInfos
        {
            get { return _lgLayoutSettings.Interactor.SelectedNodeInfos; }
        }


        List<Rail> _railsInsideSelectionRect = new List<Rail>();

        // CursorCross _cursor;


        protected Ellipse TargetPortCircle { get; set; }

        WpfPoint _objectUnderMouseDetectionLocation;
        public event EventHandler LayoutStarted;
        public event EventHandler LayoutComplete;


        /// <summary>
        /// if set to true will layout in a task
        /// </summary>
        public bool RunLayoutAsync;

        readonly Canvas _graphCanvas = new Canvas();
        Graph _drawingGraph;

        readonly LayoutEditor layoutEditor;

        public LgLayoutSettings DefaultLargeLayoutSettings
        {
            get { return _defaultLargeLayoutSettings; }
            set { _defaultLargeLayoutSettings = value; }
        }

        GeometryGraph geometryGraphUnderLayout;
        /*
                Thread layoutThread;
        */
        bool needToCalculateLayout = true;

        object _objectUnderMouseCursor;

        static double _dpiX;
        static int _dpiY;

        readonly Dictionary<DrawingObject, IViewerObject> _drawingObjectsToIViewerObjects =
            new Dictionary<DrawingObject, IViewerObject>();

        WpfRectangle _rectToFillCanvas;

        readonly Dictionary<Rail, FrameworkElement> _visibleRailsToFrameworkElems =
            new Dictionary<Rail, FrameworkElement>();


        readonly Dictionary<Triple, TileType> _tileDictionary = new Dictionary<Triple, TileType>();

        GeometryGraph GeomGraph
        {
            get
            {
                return _drawingGraph == null ? null : _drawingGraph.GeometryGraph;
            }
        }

        /// <summary>
        /// the canvas to draw the graph
        /// </summary>
        public Canvas GraphCanvas
        {
            get { return _graphCanvas; }
        }


        public GraphmapsViewer()
        {
            layoutEditor = new LayoutEditor(this);

            _graphCanvas.MouseLeftButtonDown += GraphCanvasMouseLeftButtonDown;
            _graphCanvas.MouseRightButtonDown += GraphCanvasRightMouseDown;
            _graphCanvas.MouseMove += GraphCanvasMouseMove;

            _graphCanvas.Focusable = true;
            _graphCanvas.FocusVisualStyle = null;
            _graphCanvas.KeyDown += GraphCanvasKeyDown;

            _graphCanvas.MouseLeftButtonUp += GraphCanvasMouseLeftButtonUp;
            _graphCanvas.MouseWheel += GraphCanvasMouseWheel;
            _graphCanvas.MouseRightButtonUp += GraphCanvasRightMouseUp;
            ViewChangeEvent += AdjustBtrectRenderTransform;

            LayoutEditingEnabled = true;
            clickCounter = new ClickCounter(() => Mouse.GetPosition((IInputElement)_graphCanvas.Parent));
            clickCounter.Elapsed += ClickCounterElapsed;

            _hitTestHandler = new HitTestHandler(_graphCanvas);
            CreateRectToFillCanvas();
            _stringFinder = new LgStringFinder(this, (a) => _lgLayoutSettings.GeometryNodesToLgNodeInfos[a.GeometryNode]);
            _tileFetcher = new TileFetcher(this, GetVisibleTilesSet);
        }



        #region WPF stuff

        /// <summary>
        /// adds the main panel of the viewer to the children of the parent
        /// </summary>
        /// <param name="panel"></param>
        public void BindToPanel(Panel panel)
        {
            panel.Children.Add(GraphCanvas);
            GraphCanvas.UpdateLayout();
        }


        void ClickCounterElapsed(object sender, EventArgs e)
        {
            if (_panning) return;
            var vedge = clickCounter.ClickedObject as GraphmapsEdge;
            if (vedge != null)
            {
                if (clickCounter.UpCount == clickCounter.DownCount && clickCounter.UpCount == 1)
                    HandleClickForEdge(vedge);
            }
            else
                AnalyseClicksOnVisuals();

            clickCounter.ClickedObject = null;
        }

        void AnalyseClicksOnVisuals()
        {
            var vnode = clickCounter.ClickedObject as GraphmapsNode;
            if (vnode != null)
                HandleClickForNode(vnode);
            else
            {
                var rail = clickCounter.ClickedObject as Rail;
                if (rail != null)
                {
                    System.Diagnostics.Debug.WriteLine("rail's zoomlevel = " + rail.ZoomLevel);                    
                    System.Diagnostics.Debug.WriteLine("rail's A" + rail.A);
                    System.Diagnostics.Debug.WriteLine("rail's B" + rail.B);
                    return;
                    /*
                    if (clickCounter.UpCount == clickCounter.DownCount && clickCounter.UpCount == 2)
                        HandleDoubleClickForRail(rail);
                    else if (clickCounter.UpCount == clickCounter.DownCount && clickCounter.UpCount == 1)
                        ToggleSelectRailUnderCursor();*/
                }
                else
                {
                    //return; //DO NOT SELECT INVISIBLE NODES
                    //jyoti - fixed the selection for all types of nodes
                    var invNode = _lgLayoutSettings.Interactor.AnalyzeClickForInvisibleNode(_mouseDownPositionInGraph, clickCounter.DownCount);
                    if (invNode != null)
                    {

                        IViewerObject o;
                        if (!_drawingObjectsToIViewerObjects.TryGetValue((Node)invNode.UserData, out o)) return;                        
                        var x = ((GraphmapsNode)o);
                        ViewChangeEvent(null, null);
                        var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
                        var nodeInfo = lgSettings.GeometryNodesToLgNodeInfos[invNode];
                        nodeInfo.Selected = false;
                        if (x != null)HandleClickForNode(x);                        
                        return;
                        /*
                        //jyoti assigned colors
                        var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
                        if (lgSettings == null) return;
                        var nodeInfo = lgSettings.GeometryNodesToLgNodeInfos[invNode];

                        var c = nodeInfo.Color;
                        List<SolidColorBrush> ColorSet = new List<SolidColorBrush>();
                        ColorSet.Add(Brushes.Red);
                        ColorSet.Add(Brushes.Blue);
                        ColorSet.Add(Brushes.Green);
                        ColorSet.Add(Brushes.Coral);
                        ColorSet.Add(Brushes.BlueViolet);
                        ColorSet.Add(Brushes.HotPink);

                        foreach (LgNodeInfo vinfo in SelectedNodeSet)
                        {
                            ColorSet.Remove(vinfo.Color);
                        }

                        if (ColorSet.Count > 0)
                            nodeInfo.Color = ColorSet.First();
                        else
                            nodeInfo.Color = Brushes.Red;


                        if (x != null) x.Invalidate();
                        SelectColoredEdgesIncidentTo(nodeInfo, c);
                        SelectUnselectNode(nodeInfo, true);
                        */ 

                        //ViewChangeEvent(null, null);
                        
                    }

                }
            }
        }

        void HandleDoubleClickForRail(Rail rail)
        {

            if (SelectedNodeInfos.Any())
            {
                _lgLayoutSettings.Interactor.SelectTopEdgePassingThroughRailWithEndpoint(rail, SelectedNodeInfos);
            }
            else if (rail.IsHighlighted) { }
            //PutOffEdgesPassingThroughTheRail(rail);
            //SelectAllRailsOfEdgesPassingThroughRail(rail, false);
            else
            //HighlightAllEdgesPassingThroughTheRail(rail);
            //SelectAllRailsOfEdgesPassingThroughRail(rail, true);
            {
                if (rail.TopRankedEdgeInfoOfTheRail != null)
                {
                    _lgLayoutSettings.Interactor.SelectEdge(rail.TopRankedEdgeInfoOfTheRail);
                }
            }

            ViewChangeEvent(null, null);
        }


        void AdjustBtrectRenderTransform(object sender, EventArgs e)
        {
            if (_rectToFillCanvas == null)
                return;

            var parent = (Panel)GraphCanvas.Parent;
            _rectToFillCanvas.Width = parent.ActualWidth;
            _rectToFillCanvas.Height = parent.ActualHeight;
            _rectToFillCanvas.RenderTransform = (Transform)_graphCanvas.RenderTransform.Inverse;
        }

        void GraphCanvasRightMouseUp(object sender, MouseButtonEventArgs e)
        {
            OnMouseUp(e);
        }

        
        void HandleClickForNode(GraphmapsNode vnode)
        {
            if (clickCounter.DownCount == clickCounter.UpCount && clickCounter.UpCount == 1)
            {
                //SelectRailsOfIncidentEdgesOnActiveLayer(vnode, !isSelected(vnode));
                //SelectEdgesIncidentTo(vnode);

                //jyoti assigned colors
                var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
                if (lgSettings == null) return;
                var nodeInfo = lgSettings.GeometryNodesToLgNodeInfos[vnode.Node.GeometryNode];

                var c = nodeInfo.Color;
                List<object> ColorSet = new List<object>();
                addColorsToSet(ColorSet);

                foreach (LgNodeInfo vinfo in SelectedNodeSet)
                {
                    ColorSet.Remove(vinfo.Color);
                }

                if (ColorSet.Count > 0)
                    nodeInfo.Color = ColorSet.First();
                else
                    nodeInfo.Color = Brushes.Red;  




                SelectColoredEdgesIncidentTo(vnode, (SolidColorBrush)c);
                SelectUnselectNode(vnode.LgNodeInfo, !IsSelected(vnode));


                ViewChangeEvent(null, null);

                //ToggleNodeSlidingZoom(vnode);
                //ToggleNodeEdgesSlidingZoom(vnode);
            }
            vnode.Invalidate();
        }

        private static void addColorsToSet(List<object> ColorSet)
        {
            ColorSet.Add(Brushes.Red);
            ColorSet.Add(Brushes.Blue);
            
            ColorSet.Add(Brushes.Violet);
            ColorSet.Add(Brushes.Aqua);
            ColorSet.Add(Brushes.Green);
            ColorSet.Add(Brushes.Tomato);
            ColorSet.Add(Brushes.LawnGreen);
            ColorSet.Add(Brushes.Gold);
            ColorSet.Add(Brushes.Fuchsia);
            ColorSet.Add(Brushes.GreenYellow);
            //ColorSet.Add(Brushes.LightSteelBlue);
            //ColorSet.Add(Brushes.Pink);
            //ColorSet.Add(Brushes.PaleGreen);
            //ColorSet.Add(Brushes.MediumPurple);
            
            //ColorSet.Add(Brushes.IndianRed);
            //ColorSet.Add(Brushes.CornflowerBlue);
        }

        void SelectColoredEdgesIncidentTo(LgNodeInfo nodeInfo, object c)
        {
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings == null) return;

            //var nodeInfo = lgSettings.GeometryNodesToLgNodeInfos[vnode.Node.GeometryNode];
            lgSettings.Interactor.SelectAllColoredEdgesIncidentTo(nodeInfo, (SolidColorBrush)c);
            //lgSettings.Interactor.SelectVisibleEdgesIncidentTo(nodeInfo, _layer);
        }

        void SelectColoredEdgesIncidentTo(GraphmapsNode vnode, object c)
        {
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings == null) return;

            var nodeInfo = lgSettings.GeometryNodesToLgNodeInfos[vnode.Node.GeometryNode];
            lgSettings.Interactor.SelectAllColoredEdgesIncidentTo(nodeInfo,c);
            //lgSettings.Interactor.SelectVisibleEdgesIncidentTo(nodeInfo, _layer);
        }

        void SelectVEdgesIncidentTo(GraphmapsNode vnode)
        {
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings == null) return;

            var nodeInfo = lgSettings.GeometryNodesToLgNodeInfos[vnode.Node.GeometryNode];
            lgSettings.Interactor.SelectAllEdgesIncidentTo(nodeInfo);
            //lgSettings.Interactor.SelectVisibleEdgesIncidentTo(nodeInfo, _layer);
        }
        /*
        void SelectEdgesIncidentTo(GraphmapsNode vnode) {
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings == null) return;

            var nodeInfo = lgSettings.GeometryNodesToLgNodeInfos[vnode.Node.GeometryNode];
            lgSettings.Interactor.SelectAllEdgesIncidentTo(nodeInfo);
            ViewChangeEvent(null, null);
        }
        */
        bool IsSelected(GraphmapsNode vnode)
        {
            //if (lgSettings == null) return false;
            var nodeInfo = _lgLayoutSettings.GeometryNodesToLgNodeInfos[vnode.Node.GeometryNode];
            if (nodeInfo == null) return false;
            return nodeInfo.Selected;
        }


        void SelectUnselectNode(LgNodeInfo nodeInfo, bool selected)
        {
            nodeInfo.Selected = selected;
            if (selected)
            {
                SelectedNodeInfos.Insert(nodeInfo);
                SelectedNodeSet.Add(nodeInfo);

            }
            else
            {
                SelectedNodeInfos.Remove(nodeInfo);
                SelectedNodeSet.Remove(nodeInfo);
                nodeInfo.Color = null;
            }

        }

        void SelectNodeNoChangeEvent(LgNodeInfo nodeInfo, bool selected)
        {
            nodeInfo.Selected = selected;
            if (selected)
            {
                SelectedNodeInfos.Insert(nodeInfo);
            }
            else
            {
                SelectedNodeInfos.Remove(nodeInfo);
            }
        }

        void ScaleNode(LgNodeInfo nodeInfo, double xScale)
        {
            var p = nodeInfo.BoundingBox.Center;
            nodeInfo.GeometryNode.BoundaryCurve.Translate(-p);
            nodeInfo.GeometryNode.BoundaryCurve = nodeInfo.GeometryNode.BoundaryCurve.ScaleFromOrigin(xScale, xScale);
            nodeInfo.GeometryNode.BoundaryCurve.Translate(p);
            //nodeInfo.xScale *= xScale;
            //nodeInfo.GeometryNode.BoundaryCurve.ScaleFromOrigin()
        }

        void ScaleSelectedNodes(double xScale)
        {
            foreach (LgNodeInfo ni in SelectedNodeInfos)
            {
                ScaleNode(ni, xScale);
            }
        }

        void HandleClickForEdge(GraphmapsEdge vEdge)
        {
            //todo : add a hook
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings != null)
            {
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

        void GraphCanvasRightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (MouseDown != null)
                MouseDown(this, CreateMouseEventArgs(e));
        }

        void ToggleSelectRailUnderCursor()
        {
            // roman: highlight rail
            if (_hitTestHandler.GetOneRailInsideRect(GetUnderSnappedMouseRectGeom(2)) != null)
            {
                SelectRailsUnderMouse(true, true);
                //CreateOrRemoveRailControlPoints();
                //RailUnderCursor.IsHighlighted = !RailUnderCursor.IsHighlighted;
                ViewChangeEvent(null, null);
            }
        }

        void zoomout(double x)
        {
            SetInitialTransform();
        }

        void GraphCanvasKeyDown(object sender, KeyEventArgs e)
        {
             if (e.Key == Key.End)
            {
                ClearSelection();
                ViewChangeEvent(null, null);
            }
            else if (e.Key == Key.Space)
            {
                zoomout(.5);
                ViewChangeEvent(null, null);
            }
            else if (e.Key == Key.NumPad1)
            {
                zoomout(1);
                ViewChangeEvent(null, null);
            }
            else if (e.Key == Key.NumPad2)
            {
                zoomout(2);
                ViewChangeEvent(null, null);
            }
            else if (e.Key == Key.NumPad3)
            {
                zoomout(3);
                ViewChangeEvent(null, null);
            }
            else if (e.Key == Key.NumPad4)
            {
                zoomout(4);
                ViewChangeEvent(null, null);
            }
            else if (e.Key == Key.NumPad5)
            {
                zoomout(5);
                ViewChangeEvent(null, null);
            }
            else if (e.Key == Key.NumPad6)
            {
                zoomout(6);
                ViewChangeEvent(null, null);
            }
            else if (e.Key == Key.Q)
            {
                ScaleSelectedNodes(2);
                ViewChangeEvent(null, null);
            }
            else if (e.Key == Key.U)
            {
                MarkSelectedRailsAsUsedOnPreviousLevel();
            }
            else if (e.Key == Key.PrintScreen)
            {
                TakeScreenShot("C:/tmp/screenshot.png");
            }
            //jyoti added the following controls
            else if (e.Key == Key.L)
            {
                pathThicknessController += .01;
            }
            else if (e.Key == Key.S)
            {
                pathThicknessController -= .01;
            }
            //jyoti added the background node controls
            else if (e.Key == Key.H)
            {
                BackGroundEdgeWeight = Math.Abs(BackGroundEdgeWeight-1);  
            }

            Keyboard.Focus(_graphCanvas);
        }

        void UpdateAllNodeBorders()
        {
            foreach (var o in _drawingObjectsToIViewerObjects.Values)
            {
                var vnode = o as GraphmapsNode;
                if (vnode != null)
                    vnode.Node.Attr.LineWidth = GetBorderPathThickness();
            }
        }


        void GraphCanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta != 0)
            {
                const double zoomFractionLocal = 0.9;
                var zoomInc = e.Delta < 0 ? zoomFractionLocal : 1.0 / zoomFractionLocal;
                ZoomAbout(ZoomFactor * zoomInc, e.GetPosition(_graphCanvas));
                e.Handled = true;

                //_cursor.UpdateCursor(_currentMousePos, UnderlyingPolylineCircleRadius);
                //InvalidateAllViewerObjects();
            }

            UpdateAllNodeBorders();

            if (ViewChangeEvent != null)
                ViewChangeEvent(null, null);
        }

        /// <summary>
        /// keeps centerOfZoom pinned to the screen and changes the scale by zoomFactor
        /// </summary>
        /// <param name="zoomFactor"></param>
        /// <param name="centerOfZoom"></param>
        public void ZoomAbout(double zoomFactor, WpfPoint centerOfZoom)
        {
            var scale = zoomFactor * FitFactor;
            var centerOfZoomOnScreen =
                _graphCanvas.TransformToAncestor((FrameworkElement)_graphCanvas.Parent).Transform(centerOfZoom);

            //ScaleControlPoints();
            //ScaleControlPolylines();

            SetTransform(scale, centerOfZoomOnScreen.X - centerOfZoom.X * scale,
                centerOfZoomOnScreen.Y + centerOfZoom.Y * scale);
        }

        void GraphCanvasMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            clickCounter.AddMouseDown();
            if (MouseDown != null)
                MouseDown(this, CreateMouseEventArgs(e));

            if (e.Handled) return;
            _mouseDownPositionInGraph = Common.MsaglPoint(e.GetPosition(_graphCanvas));

            Keyboard.Focus(_graphCanvas);
        }


        void GraphCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (MouseMove != null)
                MouseMove(this, CreateMouseEventArgs(e));

            if (e.Handled) return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!_panning)
                {
                    _panning = true;
                    _mouseDownPositionInGraph = Common.MsaglPoint(e.GetPosition(_graphCanvas));
                }
                Pan(e);
            }
            else
                ToolTipService.SetIsEnabled(_graphCanvas, !_graphCanvas.IsMouseDirectlyOver);
        }

        void UpdateWithWpfHitObjectUnderMouseOnLocation(WpfPoint pt, HitTestResultCallback hitTestResultCallback)
        {
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


        HitTestResultBehavior RailsHitTestSelRectResultCallback(HitTestResult result)
        {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;
            object tag = frameworkElement.Tag;
            var rail = tag as Rail;
            if (rail != null)
            {
                _railsInsideSelectionRect.Add(rail);
            }

            return HitTestResultBehavior.Continue;
        }


        // Return the result of the hit test to the callback.
        HitTestResultBehavior MyHitTestResultCallbackWithNoCallbacksToTheUser(HitTestResult result)
        {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;
            object tag = frameworkElement.Tag;
            if (tag != null)
            {
                //it is a tagged element
                var ivo = tag as IViewerObject;
                if (ivo != null)
                {
                    _objectUnderMouseCursor = ivo;
                    if (tag is GraphmapsNode || tag is Label)
                        return HitTestResultBehavior.Stop;
                }
                else
                {
                    Debug.Assert(tag is Rail);
                    _objectUnderMouseCursor = tag;
                    return HitTestResultBehavior.Stop;
                }
            }

            return HitTestResultBehavior.Continue;
        }


        double MouseHitTolerance
        {
            get
            {
                return 0.2;
                //return 0.02;
            }

        }

        /// <summary>
        /// this function pins the sourcePoint to screenPoint
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="sourcePoint"></param>
        void SetTransformFromTwoPoints(WpfPoint screenPoint, Point sourcePoint)
        {
            var scale = CurrentScale;
            SetTransform(scale, screenPoint.X - scale * sourcePoint.X, screenPoint.Y + scale * sourcePoint.Y);
        }

        void Pan(MouseEventArgs e)
        {
            if (UnderLayout)
                return;

            if (!_graphCanvas.IsMouseCaptured)
                _graphCanvas.CaptureMouse();

            SetTransformFromTwoPoints(e.GetPosition((FrameworkElement)_graphCanvas.Parent),
                _mouseDownPositionInGraph);
        }

        //        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        //        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        //        public static extern bool SetCursorPos(int X, int Y);   


        public double CurrentScale
        {
            get { return ((MatrixTransform)_graphCanvas.RenderTransform).Matrix.M11; }
        }

        /*
                void Pan(Point vector) {
            
            
                    graphCanvas.RenderTransform = new MatrixTransform(mouseDownTransform[0, 0], mouseDownTransform[0, 1],
                                                                      mouseDownTransform[1, 0], mouseDownTransform[1, 1],
                                                                      mouseDownTransform[0, 2] +vector.X,
                                                                      mouseDownTransform[1, 2] +vector.Y);            
                }
        */

        internal MsaglMouseEventArgs CreateMouseEventArgs(MouseEventArgs e)
        {
            return new GvMouseEventArgs(e, this);
        }

        void GraphCanvasMouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            OnMouseUp(e);
            clickCounter.AddMouseUp();
            clickCounter.ClickedObject =
                _hitTestHandler.GetNodeOrRailUnderMouse(GetUnderMouseRectGeom(MouseHitTolerance));


            if (_graphCanvas.IsMouseCaptured)
            {
                e.Handled = true;
                _graphCanvas.ReleaseMouseCapture();
            }
        }


        RectangleGeometry GetUnderMouseRectGeom(double t)
        {
            WpfPoint pt = Mouse.GetPosition(_graphCanvas);
            return GetRectGeomAroundPoint(pt, t);
        }

        RectangleGeometry GetUnderSnappedMouseRectGeom(double t)
        {
            WpfPoint pt = Mouse.GetPosition(_graphCanvas); //no sure
            return GetRectGeomAroundPoint(pt, t);
        }

        RectangleGeometry GetRectGeomAroundPoint(WpfPoint pt, double t)
        {
            var rect = new Rect(new WpfPoint(pt.X - MouseHitTolerance * t, pt.Y - MouseHitTolerance * t),
                new WpfPoint(pt.X + MouseHitTolerance * t, pt.Y + MouseHitTolerance * t));
            var expandedHitTestArea = new RectangleGeometry(rect);
            return expandedHitTestArea;
        }


        void SelectRailsUnderMouse(bool selected, bool toggle)
        {
            var expandedHitTestArea = GetUnderSnappedMouseRectGeom(NodeDotWidth);
            _railsInsideSelectionRect.Clear();
            VisualTreeHelper.HitTest(_graphCanvas, null,
                RailsHitTestSelRectResultCallback,
                new GeometryHitTestParameters(expandedHitTestArea));

            if (!_railsInsideSelectionRect.Any()) return;
            foreach (var rail in _railsInsideSelectionRect)
                if (toggle)
                    SetRailSelection(rail, !rail.IsHighlighted);
                else
                    SetRailSelection(rail, selected);
        }

        void SetRailSelection(Rail rail, bool selected)
        {
            if (selected)
            {
                _selectedRails.Insert(rail);
            }
            else
            {
                _selectedRails.Remove(rail);
            }
            rail.IsHighlighted = selected;
        }

        void OnMouseUp(MouseEventArgs e)
        {
            _panning = false;
            if (MouseUp != null)
                MouseUp(this, CreateMouseEventArgs(e));
        }

        /// <summary>
        /// 
        /// </summary>
        public double ZoomFactor
        {
            get { return CurrentScale / FitFactor; }
        }

        #endregion

        #region IViewer stuff

        public event EventHandler<EventArgs> ViewChangeEvent;
        public event EventHandler<MsaglMouseEventArgs> MouseDown;
        public event EventHandler<MsaglMouseEventArgs> MouseMove;
        public event EventHandler<MsaglMouseEventArgs> MouseUp;

#pragma warning disable 67
        public event EventHandler<ObjectUnderMouseCursorChangedEventArgs> ObjectUnderMouseCursorChanged;
#pragma warning restore 67

        public IViewerObject ObjectUnderMouseCursor
        {
            get
            {
                // this function can bring a stale object
                var location = Mouse.GetPosition(_graphCanvas);
                if (!(_objectUnderMouseDetectionLocation == location))
                    UpdateWithWpfHitObjectUnderMouseOnLocation(location, MyHitTestResultCallbackWithNoCallbacksToTheUser);
                return GetIViewerObjectFromObjectUnderCursor(_objectUnderMouseCursor);
            }
        }

        IViewerObject GetIViewerObjectFromObjectUnderCursor(object obj)
        {
            if (obj == null)
                return null;
            var ret = obj as IViewerObject;
            if (ret != null)
                return ret;

            var rail = obj as Rail;
            if (rail != null)
                return GetVEdgeOfRail(rail);
            return null;
        }

        public void Invalidate(IViewerObject objectToInvalidate)
        {
            ((IInvalidatable)objectToInvalidate).Invalidate();
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings != null)
            {
                var vnode = objectToInvalidate as GraphmapsNode;
                if (vnode != null)
                    SetNodeAppearence(vnode);
            }
        }

        void SetNodeAppearence(GraphmapsNode node)
        {
            node.Node.Attr.LineWidth = GetBorderPathThickness();
        }

        public void Invalidate()
        {
            //todo: is it right to do nothing
        }

        public event EventHandler GraphChanged;

        public ModifierKeys ModifierKeys
        {
            get
            {
                switch (Keyboard.Modifiers)
                {
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

        public Point ScreenToSource(MsaglMouseEventArgs e)
        {
            var p = new Point(e.X, e.Y);
            var m = Transform.Inverse;
            return m * p;
        }


        public IEnumerable<IViewerObject> Entities
        {
            get
            {
                foreach (var viewerObject in _drawingObjectsToIViewerObjects.Values)
                {
                    yield return viewerObject;
                    var edge = viewerObject as GraphmapsEdge;
                    if (edge != null)
                        if (edge.GraphmapsLabel != null)
                            yield return edge.GraphmapsLabel;
                }
            }
        }

        internal static double DpiXStatic
        {
            get
            {
                if (_dpiX == 0)
                    GetDpi();
                return _dpiX;
            }
        }

        static void GetDpi()
        {
            int hdcSrc = NativeMethods.GetWindowDC(NativeMethods.GetDesktopWindow());
            //LOGPIXELSX = 88,
            //LOGPIXELSY = 90,
            _dpiX = NativeMethods.GetDeviceCaps(hdcSrc, 88);
            _dpiY = NativeMethods.GetDeviceCaps(hdcSrc, 90);
            NativeMethods.ReleaseDC(NativeMethods.GetDesktopWindow(), hdcSrc);
        }

        public double DpiX
        {
            get { return DpiXStatic; }
        }

        public double DpiY
        {
            get { return DpiYStatic; }
        }

        static double DpiYStatic
        {
            get
            {
                if (_dpiX == 0)
                    GetDpi();
                return _dpiY;
            }
        }

        public void OnDragEnd(IEnumerable<IViewerObject> changedObjects)
        {
            throw new NotImplementedException();
        }

        public double LineThicknessForEditing { get; set; }

        /// <summary>
        /// the layout editing with the mouse is enabled if and only if this field is set to false
        /// </summary>
        public bool LayoutEditingEnabled { get; set; }

        public bool InsertingEdge { get; set; }

        public void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems)
        {
            var contextMenu = new ContextMenu();
            foreach (var pair in menuItems)
                contextMenu.Items.Add(CreateMenuItem(pair.Item1, pair.Item2));
            contextMenu.Closed += ContextMenuClosed;
            ContextMenuService.SetContextMenu(_graphCanvas, contextMenu);

        }

        void ContextMenuClosed(object sender, RoutedEventArgs e)
        {
            ContextMenuService.SetContextMenu(_graphCanvas, null);
        }

        public static object CreateMenuItem(string title, VoidDelegate voidVoidDelegate)
        {
            var menuItem = new MenuItem { Header = title };
            menuItem.Click += (RoutedEventHandler)(delegate { voidVoidDelegate(); });
            return menuItem;
        }

        public double UnderlyingPolylineCircleRadius
        {
            get { return 0.1 * DpiX / CurrentScale; }
        }

        public Graph Graph
        {
            get { return _drawingGraph; }
            set
            {
                _drawingGraph = value;
                System.Diagnostics.Debug.WriteLine("starting processing a graph with {0} nodes and {1} edges", _drawingGraph.NodeCount,
                    _drawingGraph.EdgeCount);
                if (_drawingGraph.RootSubgraph.Subgraphs != null && _drawingGraph.RootSubgraph.Subgraphs.Any())
                {
                    System.Diagnostics.Debug.WriteLine("skipping a graph with clusters");
                    return;
                }
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


        const double DesiredPathThicknessInInches = 0.016;

        readonly ClickCounter clickCounter;

        double GetBorderPathThickness()
        {
            return pathThicknessController * DpiX / CurrentScale; //jyoti made it thinner
            //return DesiredPathThicknessInInches*DpiX/  CurrentScale;
        }

        readonly Object _processGraphLock = new object();


        void ProcessGraph()
        {
            lock (_processGraphLock)
            {
                ProcessGraphUnderLock();
            }
        }

        void ProcessGraphUnderLock()
        {
            try
            {
                if (LayoutStarted != null)
                    LayoutStarted(null, null);

                CancelToken = new CancelToken();

                if (_drawingGraph == null) return;
                HideCanvas();
                Clear();
                /*  if (!LargeGraphBrowsing)
                      CreateFrameworkElementsForLabelsOnly(); */
                LayoutEditingEnabled = false;
                var lgsettings = new LgLayoutSettings(
                    GetCanvasRenderViewport,
                    () => Transform, DpiX, DpiY, () => ArrowheadLength)
                    {
                        NeedToLayout = NeedToCalculateLayout,
                        MaxNumberOfNodesPerTile = DefaultLargeLayoutSettings.MaxNumberOfNodesPerTile,
                        MaxNumberOfRailsPerTile = DefaultLargeLayoutSettings.MaxNumberOfRailsPerTile,
                        RailColors = DefaultLargeLayoutSettings.RailColors,
                        SelectionColors = DefaultLargeLayoutSettings.SelectionColors,
                        IncreaseNodeQuota = DefaultLargeLayoutSettings.IncreaseNodeQuota,
                        ExitAfterInit = DefaultLargeLayoutSettings.ExitAfterInit,
                        SimplifyRoutes = DefaultLargeLayoutSettings.SimplifyRoutes,
                        NodeLabelHeightInInches = DefaultLargeLayoutSettings.NodeLabelHeightInInches,
                        ClientViewportMappedToGraph = () => GetVisibleRectangleInGraph()
                    };

                _drawingGraph.LayoutAlgorithmSettings = lgsettings;
                //lgsettings.ViewModel = ViewModel;
                lgsettings.ViewerChangeTransformAndInvalidateGraph +=
                    OGraphChanged;
                if (NeedToCalculateLayout)
                {
                    _drawingGraph.CreateGeometryGraph(); //forcing the layout recalculation
                    if (_graphCanvas.Dispatcher.CheckAccess())
                        PopulateGeometryOfGeometryGraph();
                    else
                        _graphCanvas.Dispatcher.Invoke(PopulateGeometryOfGeometryGraph);
                }
                else
                {
                    if (_graphCanvas.Dispatcher.CheckAccess())
                        SetLabelWidthToHeightRatiosOfGeometryGraph();
                    else
                        _graphCanvas.Dispatcher.Invoke(SetLabelWidthToHeightRatiosOfGeometryGraph);
                }

                geometryGraphUnderLayout = _drawingGraph.GeometryGraph;
                if (RunLayoutAsync)
                    SetUpBackgrounWorkerAndRunAsync();
                else
                    RunLayoutInUIThread();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        internal Rectangle GetCanvasRenderViewport()
        {
            return new Rectangle(0, 0, _graphCanvas.RenderSize.Width, _graphCanvas.RenderSize.Height);
        }

        void RunLayoutInUIThread()
        {
            LayoutGraph();
            PostLayoutStep();
            if (LayoutComplete != null)
                LayoutComplete(null, null);
        }

        void SetUpBackgrounWorkerAndRunAsync()
        {
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += (a, b) => LayoutGraph();
            _backgroundWorker.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    MessageBox.Show(args.Error.ToString());
                    Clear();
                }
                else if (CancelToken.Canceled)
                {
                    Clear();
                }
                else
                {
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

        void HideCanvas()
        {
            if (_graphCanvas.Dispatcher.CheckAccess())
                _graphCanvas.Visibility = Visibility.Hidden; // hide canvas while we lay it out asynchronously.
            else
                _graphCanvas.Dispatcher.Invoke(() => _graphCanvas.Visibility = Visibility.Hidden);
        }


        void LayoutGraph()
        {
            if (NeedToCalculateLayout)
            {
                try
                {
                    var ls = _drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings;
                    LayoutHelpers.CalculateLayout(geometryGraphUnderLayout, _drawingGraph.LayoutAlgorithmSettings,
                        CancelToken, TileDirectory);
                }
                catch (OperationCanceledException)
                {
                    //swallow this exception
                }
            }
            else
                LayoutHelpers.LayoutLargeGraphWithLayers(geometryGraphUnderLayout, _drawingGraph.LayoutAlgorithmSettings,
                    CancelToken, TileDirectory);

            _lgLayoutSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;

            var noldeLabelRatios = new List<double>();
            foreach (var n in geometryGraphUnderLayout.Nodes)
            {
                var node = (Drawing.Node)n.UserData;
                noldeLabelRatios.Add(node == null ? 1 : node.Attr.LabelWidthToHeightRatio);
            }


            LayoutHelpers.ComputeNodeLabelsOfLargeGraphWithLayers(geometryGraphUnderLayout,
                _drawingGraph.LayoutAlgorithmSettings,
                noldeLabelRatios,
                CancelToken);

            //added            
        }

        void PostLayoutStep()
        {
            _graphCanvas.Visibility = Visibility.Visible;
            PushDataFromLayoutGraphToFrameworkElements();
            _backgroundWorker = null; //this will signal that we are not under layout anymore
            if (GraphChanged != null)
                GraphChanged(this, null);

            SetInitialTransform();
            _graphCanvas.InvalidateVisual();
            //test
            // UnhideHideAllNodes();
            //CreateHideAllRails();
        }

        void SetScaleRangeForLgLayoutSettings()
        {
            var lgSettings = (LgLayoutSettings)_drawingGraph.LayoutAlgorithmSettings;
            var maximalZoomLevelForEdges = lgSettings.GetMaximalZoomLevel();

            var minSize = 5 * _drawingGraph.Nodes.Select(n => n.BoundingBox).Min(b => Math.Min(b.Width, b.Height));

            var nodeScale = Math.Min(_drawingGraph.BoundingBox.Width, _drawingGraph.BoundingBox.Height) / minSize;

            var largestScale = Math.Max(nodeScale, maximalZoomLevelForEdges) * 2;
            var fitFactor = FitFactor;
            lgSettings.ScaleInterval = new Interval(fitFactor / 10, fitFactor * largestScale);            

        }


        //        void SetupTimerOnViewChangeEvent(object sender, EventArgs e) {
        //            SetupRoutingTimer();
        //        }

        /// <summary>
        /// oGraph has changed too
        /// </summary>
        void OGraphChanged()
        {


            if (UnderLayout) return;
            var existingEdges = new Set<DrawingEdge>();
            var existindNodes = new Set<Node>();
            FillExistingNodesEdges(existindNodes, existingEdges);


            var railGraph = _lgLayoutSettings.RailGraph;
            if (railGraph == null)
                return;


            var nodesFromVectorTiles = NodesFromVectorTiles();
            var railGraphNodes = new Set<Node>(railGraph.Nodes.Select(node => (Node)node.UserData));
            var requiredNodes = railGraphNodes + NodesFromVectorTiles();
            var fakeTileNodes = nodesFromVectorTiles - railGraphNodes;

            fakeTileNodes = GetIntersectingVisibleRectangle(fakeTileNodes);

            ProcessNodesAddRemove(requiredNodes, existindNodes);
            var requiredEdges =
                new Set<DrawingEdge>(
                    railGraph.Rails.Where(rail => rail.TopRankedEdgeInfoOfTheRail != null)
                        .Select(rail => (DrawingEdge)rail.TopRankedEdgeInfoOfTheRail.Edge.UserData));
            ProcessEdgesAddRemove(existingEdges, requiredEdges);
            RemoveNoLongerVisibleRails(railGraph);

            double currentlayer = Math.Max(0, GetZoomFactorToTheGraph());
            
            _layer = GetLevelIndexByScale(currentlayer);
            UpdateVisibleRails(railGraph, Math.Log(currentlayer, 2));


            CreateOrInvalidateFrameworksElementForVisibleRails(railGraph);
            InvalidateNodesOfRailGraph(nodesFromVectorTiles);

            InvalidateNodesOfRailGraph(requiredNodes);
            _lgLayoutSettings.Interactor.AddLabelsOfHighlightedNodes(CurrentScale);

            InvalidateNodesOfRailGraph(fakeTileNodes);
            _tileFetcher.StartLoadindTiles();


        }
        internal int GetLevelIndexByScale(double scale)
        {
            if (scale <= 1) return 0;
            var z = Math.Floor(scale);//Math.Log(scale, 2);
            if (z >= _lgLayoutSettings.maximumNumOfLayers) return _lgLayoutSettings.maximumNumOfLayers - 1;
            return (int)z;
        }
        private Rectangle NodeDotRect(LgNodeInfo ni)
        {
            double w = NodeDotWidth;
            return new Rectangle(ni.Center - 0.5 * new Point(w, w), ni.Center + 0.5 * new Point(w, w));
        }

        private Set<Node> GetIntersectingVisibleRectangle(Set<Node> fakeTileNodes)
        {
            var rect = GetVisibleRectangleInGraph();
            var nodes = new Set<Node>();
            foreach (var node in fakeTileNodes)
            {
                IViewerObject o;
                if (!_drawingObjectsToIViewerObjects.TryGetValue(node, out o)) continue;
                var vnode = ((GraphmapsNode)o);
                if (NodeDotRect(vnode.LgNodeInfo).Intersects(rect))
                {
                    nodes.Insert(node);
                }
            }
            return nodes;
        }

        Set<Node> NodesFromVectorTiles()
        {
            var ret = new Set<Node>();
            foreach (var tile in VectorTiles())
                ret.InsertRange(GetTileNodes(tile));
            return ret;
        }

        IEnumerable<Node> GetTileNodes(Triple tile)
        {
            return _lgLayoutSettings.Interactor.GetTileNodes(tile).Select(n => (Node)n.UserData);
        }

        IEnumerable<Triple> VectorTiles()
        {
            int iLevel = GetBackgroundTileLevel();
            GridTraversal grid = new GridTraversal(GeomGraph.BoundingBox, iLevel);
            var visibleRectangle = GetVisibleRectangleInGraph();

            var t1 = grid.PointToTuple(visibleRectangle.LeftBottom);
            var t2 = grid.PointToTuple(visibleRectangle.RightTop);

            for (int ix = t1.   Item1; ix <= t2.Item1; ix++)
                for (int iy = t1.Item2; iy <= t2.Item2; iy++)
                {
                    var t = new Triple(iLevel, ix, iy);
                    TileType tileType;
                    if (!_tileDictionary.TryGetValue(t, out tileType)) continue;
                    if (tileType == TileType.Vector)
                        yield return t;
                }
        }


        void FillExistingNodesEdges(Set<Node> existindNodes, Set<DrawingEdge> existingEdges)
        {
            foreach (var dro in _drawingObjectsToIViewerObjects.Keys)
            {
                var n = dro as Node;

                if (n != null && dro.IsVisible) //added: visibility
                    existindNodes.Insert(n);
                else
                {
                    var edge = dro as DrawingEdge;
                    if (edge != null && dro.IsVisible)
                        existingEdges.Insert((DrawingEdge)dro);
                }
            }
        }

        void ProcessEdgesAddRemove(Set<DrawingEdge> vDrawingEdges, Set<DrawingEdge> oDrawgingEdges)
        {
            ProcessEdgeRemovals(vDrawingEdges - oDrawgingEdges);
            ProcessEdgeAdditions(oDrawgingEdges - vDrawingEdges);
        }

        void ProcessNodesAddRemove(Set<Node> requiredNodes, Set<Node> existindNodes)
        {
            lock (this)
            {
                foreach (var node in existindNodes.Where(node => !requiredNodes.Contains(node)))
                    HideVNode(node);
                foreach (var node in requiredNodes.Where(node => !existindNodes.Contains(node)))
                    UnhideVNode(node);
            }
        }


        void UpdateBackgroundTiles()
        {
            if (ThereAreNoTiles()) return;
            _tileFetcher.StartLoadindTiles();
        }


        bool ThereAreNoTiles()
        {
            return _tileDictionary.Count == 0;
        }


        Set<Tuple<int, int, int>> GetVisibleTilesSet()
        {
            int iLevel = GetBackgroundTileLevel();
            GridTraversal grid = new GridTraversal(GeomGraph.BoundingBox, iLevel);
            var tiles = new Set<Triple>();
            var visibleRectangle = GetVisibleRectangleInGraph();

            var t1 = grid.PointToTuple(visibleRectangle.LeftBottom);
            var t2 = grid.PointToTuple(visibleRectangle.RightTop);

            for (int ix = t1.Item1; ix <= t2.Item1; ix++)
                for (int iy = t1.Item2; iy <= t2.Item2; iy++)
                {
                    var t = new Triple(iLevel, ix, iy);

                    TileType tileType;
                    if (!_tileDictionary.TryGetValue(t, out tileType)) continue;
                    if (tileType == TileType.Image) tiles.Insert(t);
                }

            return tiles;
        }

        public Rectangle GetVisibleRectangleInGraph()
        {
            var t = Transform.Inverse;
            var p0 = new Point(0, 0);
            var vp = GetCanvasRenderViewport();
            var p1 = new Point(vp.Width, vp.Height);
            var rect = new Rectangle(t * p0, t * p1);
            if (GeomGraph == null)
                return rect;

            return rect.Intersection(GeomGraph.BoundingBox);
        }

        internal string CreateTileFileName(int ix, int iy, GridTraversal grid)
        {
            var splitName = grid.SplitTileNameOnDirectories(ix, iy);
            string fname = TileDirectory;
            for (int i = splitName.Count - 1; i >= 0; i--)
                fname = System.IO.Path.Combine(fname, splitName[i]);
            return fname + ".png";
        }

        internal int GetBackgroundTileLevel()
        {
            var zf = ZoomFactor;
            if (zf <= 1) return 0;
            return (int)Math.Log(zf, 2);
        }


        void ClearSelection()
        {
            ClearNodesSelection();
        }



        void DeselectNode(GraphmapsNode vnode)
        {


                //jyoti assigned colors
                var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
                if (lgSettings == null) return;
                var nodeInfo = lgSettings.GeometryNodesToLgNodeInfos[vnode.Node.GeometryNode];

                var c = nodeInfo.Color;
                List<object> ColorSet = new List<object>();
                addColorsToSet(ColorSet);

                foreach (LgNodeInfo vinfo in SelectedNodeSet)
                {
                    ColorSet.Remove(vinfo.Color);
                }

                if (ColorSet.Count > 0)
                    nodeInfo.Color = ColorSet.First();
                else
                    nodeInfo.Color = Brushes.Red;




                SelectColoredEdgesIncidentTo(vnode, c);
                SelectUnselectNode(vnode.LgNodeInfo, !IsSelected(vnode));
                vnode.Invalidate();


                
        }


        void ClearEdgeSelection()
        {
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings == null) return;
            lgSettings.Interactor.DeselectAllEdges();
        }

        void ClearNodesSelection()
        {
            //jyoti fixed the 'End' operation by deselecting colored nodes
            foreach (var o in _drawingObjectsToIViewerObjects.Values)
            {
                var vNode = o as GraphmapsNode;
                if (vNode != null && vNode.LgNodeInfo.Color != null)
                {
                    DeselectNode(vNode);
                }
            }
            
        }

        void ClearRailSelection()
        {
            var railsToDeselect = _selectedRails.Clone();
            foreach (var r in railsToDeselect)
            {
                r.Color = null; // this is to help KeyPress='End'
                SetRailSelection(r, false);
            }
        }

        public double NodeDotWidth
        {
            get { return _lgLayoutSettings.NodeDotWidthInInches * DpiX / CurrentScale; } //ZoomFactor;
        }

        internal double GetZoomFactorToTheGraph()
        {
            return _lgLayoutSettings.Interactor.GetZoomFactorToTheGraph();
        }

        void InvalidateNodesOfRailGraph(Set<Node> nodesFromVectorTiles)
        {
            double zf = ZoomFactor;

            //start: changing tooltip
            String tooltiptext = "";
            List<object> ColorSet = new List<object>();
            addColorsToSet(ColorSet);
            List<GraphmapsNode> coloredNodeList = new List<GraphmapsNode>();
            //end: changing tooltip

            foreach (var o in _drawingObjectsToIViewerObjects.Values)
            {                
                var vNode = o as GraphmapsNode;
                if (vNode != null)
                {

                    //Find the nodes that are selected                                        
                    if (ColorSet.Contains(vNode.LgNodeInfo.Color))
                    {
                        coloredNodeList.Add(vNode);
                    }

                    vNode.InvalidateNodeDot(NodeDotWidth);
                    if (vNode.LgNodeInfo == null) continue;
                    ArrangeNodeLabel(vNode, zf);
                    if (nodesFromVectorTiles.Contains(vNode.Node))
                        SetupTileNode(vNode);
                    vNode.Node.Attr.LineWidth = GetBorderPathThickness();

                    if (vNode.LgNodeInfo == null) continue;

                    double cs = CurrentScale;

                    double nodeLabelHeight = _lgLayoutSettings.NodeLabelHeightInInches * DpiY / CurrentScale;
                    double nodeLabelWidth = nodeLabelHeight * vNode.LgNodeInfo.LabelWidthToHeightRatio;

                    if (vNode.LgNodeInfo.LabelVisibleFromScale >= 0 &&
                        vNode.LgNodeInfo.LabelVisibleFromScale <= zf
                        )
                    {
                        var offset = Point.Scale(nodeLabelWidth + NodeDotWidth * 1.01, nodeLabelHeight + NodeDotWidth * 1.01,
                            vNode.LgNodeInfo.LabelOffset);
                        vNode.InvalidateNodeLabel(nodeLabelHeight, nodeLabelWidth, offset);
                    }
                    else if (_lgLayoutSettings.Interactor.SelectedNodeLabels.ContainsKey(vNode.LgNodeInfo)                        
                        )
                    {

                        var pos = _lgLayoutSettings.Interactor.SelectedNodeLabels[vNode.LgNodeInfo];
                        var offset = Point.Scale(nodeLabelWidth + NodeDotWidth * 1.01, nodeLabelHeight + NodeDotWidth * 1.01,
                            LgNodeInfo.GetLabelOffset(pos));
                        vNode.InvalidateNodeLabel(nodeLabelHeight, nodeLabelWidth, offset);
                    }
                    else
                    {
                        vNode.HideNodeLabel();
                    }
                }

            }
            //start: changing tooltip
            List<System.Windows.Media.SolidColorBrush> incidentColorSet;
            foreach (var o in _drawingObjectsToIViewerObjects.Values)
            {
                var vNode = o as GraphmapsNode;
                if (vNode == null) continue;
                tooltiptext = "";
                
                incidentColorSet = new List<System.Windows.Media.SolidColorBrush>();

                foreach (var w in coloredNodeList)
                {
                    foreach (var edge in w.Node.GeometryNode.OutEdges)
                    {
                        if (vNode.Node.GeometryNode == edge.Target)
                        {
                            if(!tooltiptext.Contains(w.Node.LabelText))
                                tooltiptext = tooltiptext+ "\n" + w.Node.LabelText;                                                        
                            incidentColorSet.Add((SolidColorBrush)w.LgNodeInfo.Color);
                        }
                    }
                    foreach (var edge in w.Node.GeometryNode.InEdges)
                    {
                        if (vNode.Node.GeometryNode == edge.Source)
                        {
                            if (!tooltiptext.Contains(w.Node.LabelText))
                                tooltiptext = tooltiptext + "\n" + w.Node.LabelText;
                            incidentColorSet.Add((SolidColorBrush)w.LgNodeInfo.Color);
                        }
                    }   
                }
                if (tooltiptext.Length > 0)
                {                    
                    tooltiptext = "\nSelected Neighbors:" + tooltiptext;                    
                }

                if (vNode.BoundaryPath.Fill!= null && vNode.BoundaryPath.Fill.Equals(Brushes.Yellow))
                {
                    int Ax = 0, Rx = 0, Gx = 0, Bx = 0;
                    foreach (var c in incidentColorSet)
                    {
                        Ax += c.Color.A;
                        Rx += c.Color.R;
                        Gx += c.Color.G;
                        Bx += c.Color.B;
                    }
                    byte Ay = 0, Ry = 0, Gy = 0, By = 0;
                    Ay = (Byte)((int)(Ax / incidentColorSet.Count));
                    Ry = (Byte)((int)(Rx / incidentColorSet.Count));
                    Gy = (Byte)((int)(Gx / incidentColorSet.Count));
                    By = (Byte)((int)(Bx / incidentColorSet.Count));

                    System.Windows.Media.Color brush = new System.Windows.Media.Color
                    {
                        A = Ay,
                        R = Ry,
                        G = Gy,
                        B = By
                    };

                    vNode.BoundaryPath.Stroke = new SolidColorBrush(brush);
                    vNode.BoundaryPath.StrokeThickness = vNode.PathStrokeThickness * 2;
                }
                else vNode.BoundaryPath.StrokeThickness = vNode.PathStrokeThickness/2;

                vNode.BoundaryPath.ToolTip = new ToolTip
                {
                    Content = new TextBlock { Text = vNode.Node.LabelText + tooltiptext }                    
                };
                
            }
            //end: changing tooltip
        }

        static void SetupTileNode(GraphmapsNode vNode)
        {
            vNode.Node.Attr.LineWidth = 0;
            vNode.SetLowTransparency();
        }

        private void ArrangeNodeLabel(GraphmapsNode vNode, double zf)
        {
            double nodeLabelHeight = _lgLayoutSettings.NodeLabelHeightInInches * DpiY / CurrentScale;
            double nodeLabelWidth = nodeLabelHeight * vNode.LgNodeInfo.LabelWidthToHeightRatio;

            if (vNode.LgNodeInfo.LabelVisibleFromScale >= 0 &&
                vNode.LgNodeInfo.LabelVisibleFromScale <= zf)
            {
                var offset = Point.Scale(nodeLabelWidth + NodeDotWidth * 1.01, nodeLabelHeight + NodeDotWidth * 1.01,
                    vNode.LgNodeInfo.LabelOffset);
                vNode.InvalidateNodeLabel(nodeLabelHeight, nodeLabelWidth, offset);
            }
            else
                vNode.HideNodeLabel();
        }

        void CreateOrInvalidateFrameworksElementForVisibleRails(RailGraph railGraph)
        {
            foreach (var rail in railGraph.Rails)
                CreateOrInvalidateFrameworksElementForVisibleRailWithoutChangingGeometry(rail);
        }

        double LayerNumber = -1;
        Stack<int> layers = new Stack<int>();
        void UpdateVisibleRails(RailGraph railGraph, double currentLayerNumber)
        {

            //int integralLayerNumber = GetLevelIndexByScale(currentLayerNumber);
            int integralLayerNumber = (int)(currentLayerNumber);
            double t = currentLayerNumber - integralLayerNumber;
            if (t > 1) t = 1;
            if (t < 0) t = 1 + t;

            if (LayerNumber < currentLayerNumber)
            {
                layers.Push(integralLayerNumber);
                LayerNumber = currentLayerNumber;
            }
            if (LayerNumber > currentLayerNumber)
            {
                if (layers.Count > 0) layers.Pop();
                LayerNumber = currentLayerNumber;
            }
            if (layers.Count >= 2)
            {
                var pop1 = layers.Pop(); var pop2 = layers.Pop();
                layers.Push(pop2); layers.Push(pop1);
                if (pop1 != pop2) t = 0;
            }


            if (CurrentScale / FitFactor <= 0.4) t = 0;
            if (integralLayerNumber > _lgLayoutSettings.maximumNumOfLayers)
            {
                t = 1;

            }

            List<Rail> highlightedRails = new List<Rail>();
            List<Rail> adjacenttoHighLightedRails = new List<Rail>();
            List<Edge> highlightedEdges = new List<Edge>();
            
            /*
            foreach (var rail in railGraph.Rails)
                if (rail.IsHighlighted)
                {
                    highlightedRails.Add(rail);
                    foreach (Edge e in _lgLayoutSettings.Interactor.RailToEdges[rail])
                    {
                        if(!highlightedEdges.Contains(e))
                            highlightedEdges.Add(e);
                    }
                }
            */
            
            //this can fix the bottom node garbage rail of b102
            //but why it cannot do that for the right node?
            List<Rail> UnusedRails = new List<Rail>();
            foreach (var rail in railGraph.Rails)
            if (rail.GetTopEdgeInfo() != null && rail.TopRankedEdgeInfoOfTheRail.ZoomLevel > _layer && !rail.IsHighlighted)            
                UnusedRails.Add(rail);

            foreach (var rail in UnusedRails)
            {
                railGraph.Rails.Remove(rail);
                FrameworkElement fe;
                GraphmapsEdge vEdgeOfRail = GetVEdgeOfRail(rail);
                
                if (_visibleRailsToFrameworkElems.TryGetValue(rail, out fe))
                {                   
                    fe.Visibility = Visibility.Hidden;
                    vEdgeOfRail.Invalidate(fe, rail);
                    Panel.SetZIndex(fe, -10);
                    RemoveRail(rail);
                }
                
            }
              

            foreach (var rail in railGraph.Rails)
            {
                 

                Point A = new Point();
                Point B = new Point();

                                  
                A.X = rail.A.X + t * (rail.targetA.X - rail.A.X);
                A.Y = rail.A.Y + t * (rail.targetA.Y - rail.A.Y);
                B.X = rail.B.X + t * (rail.targetB.X - rail.B.X);
                B.Y = rail.B.Y + t * (rail.targetB.Y - rail.B.Y);
                 
                /*
                if (SelectedNodeSet.Count > 0 )
                {
                    bool ChangeItToInitialCondition = false;
                    foreach (Edge e in _lgLayoutSettings.Interactor.RailToEdges[rail])
                    {
                        //checked if e contains a Selected  rail
                        if (highlightedEdges.Contains(e)) ChangeItToInitialCondition = true;  
                    }
                    if (ChangeItToInitialCondition)
                    {
                        A = rail.initialA;
                        B = rail.initialB;
                    } 
                }*/



                // For each selected highlighted rails,
                //take the lowest layer form of the rail
                if (SelectedNodeSet.Count > 0 && rail.IsHighlighted)
                {
                    A = rail.initialA;
                    B = rail.initialB;
                }
                else
                {
                    rail.Color = null;
                }


                rail.Geometry = new LineSegment(A, B);

                /*if (BackGroundEdgeWeight == 0 && !rail.IsHighlighted)
                {
                    rail.Weight = BackGroundEdgeWeight;
                }*/

                /*
                //this is a garbage rail
                if (rail.GetTopEdgeInfo() != null && rail.TopRankedEdgeInfoOfTheRail.ZoomLevel > _layer &&
                    !rail.IsHighlighted)
                {
                    
                }   
                */
               

                ReplaceFrameworkElementForSkeletonRail(rail);
            }
             
        }

        bool IntersectsRails(Rail rail, List<Rail> highlightedRails)
        {
            Microsoft.Msagl.Core.Geometry.Point interestionPoint;
            var a = rail.A;
            var b = rail.B;

            foreach (var hrail in highlightedRails)
            {
                var c = hrail.A;
                var d = hrail.B;
                if (Microsoft.Msagl.Core.Geometry.Point.SegmentSegmentIntersection(a, b, c, d, out interestionPoint))
                    return true;
            }
            return false;
        }
        void CreateOrInvalidateFrameworksElementForVisibleRailWithoutChangingGeometry(Rail rail)
        {
            FrameworkElement fe;
            GraphmapsEdge vEdgeOfRail = GetVEdgeOfRail(rail);
            if (vEdgeOfRail == null)
            {
                CreateOrInvalidateFrameworkElementForSkeletonRail(rail);
                return; // skeleton rails
            }
            if (_visibleRailsToFrameworkElems.TryGetValue(rail, out fe))
            {
                // added
                fe.Visibility = Visibility.Visible;

                vEdgeOfRail.Invalidate(fe, rail);
                // added
                UpdateRailZindex(fe, rail);
            }
            else
            {
                fe = vEdgeOfRail.CreateFrameworkElementForRail(rail);
                GraphCanvasChildrenAdd(fe);
                _visibleRailsToFrameworkElems[rail] = fe;

                // added
                UpdateRailZindex(fe, rail);
            }
        }

        void GraphCanvasChildrenAdd(FrameworkElement fe)
        {
            _graphCanvas.Children.Add(fe);
        }

        void GraphCanvasChildrenRemove(FrameworkElement fe)
        {
            if (_graphCanvas.Children.Contains(fe))
                _graphCanvas.Children.Remove(fe);
        }

        void UpdateRailZindex(FrameworkElement fe, Rail rail)
        {
            var railZindex = 50 - (int)Math.Log(rail.MinPassingEdgeZoomLevel, 2);
            if (rail.IsHighlighted)
                railZindex += 50;

            Panel.SetZIndex(fe, railZindex);
            //if (rail.IsHighlighted)
            //{
            //    Panel.SetZIndex(fe, 100);
            //}
            //else
            //{
            //    Panel.SetZIndex(fe, 50);
            //}
        }

        void CreateOrInvalidateFrameworkElementForSkeletonRail(Rail rail)
        {
            FrameworkElement fe;
            if (_visibleRailsToFrameworkElems.TryGetValue(rail, out fe))
            {
                var path = fe as Path;
                if (path == null) return;
                InvalidateSkeletonRail(rail, path);
                fe.Visibility = Visibility.Visible;
            }
            else
            {
                CreateFrameworkElementForSkeletonRail(rail);
            }
        }

        void CreateFrameworkElementForSkeletonRail(Rail rail)
        {
            var iCurve = rail.Geometry as ICurve;
            if (iCurve == null) return;
            var path = new Path
            {
                Data = GraphmapsEdge.GetICurveWpfGeometry(iCurve),
            };

            InvalidateSkeletonRail(rail, path);
 

            path.Tag = rail;
            
            GraphCanvasChildrenAdd(path);            
            _visibleRailsToFrameworkElems[rail] = path;
        }


        void ReplaceFrameworkElementForSkeletonRail(Rail rail)
        {
            var iCurve = rail.Geometry as ICurve;
            if (iCurve == null) return;
            if (!_visibleRailsToFrameworkElems.ContainsKey(rail)) return;
            var elm = _visibleRailsToFrameworkElems[rail] as Path;
            elm.Data = GraphmapsEdge.GetICurveWpfGeometry(iCurve);

            // GraphCanvasChildrenAdd(path);
            //_visibleRailsToFrameworkElems[rail] = path;
        }

        void InvalidateSkeletonRail(Rail rail, Path path)
        {
            path.StrokeThickness = rail.IsUsedOnPreviousLevel ? 2 * GetBorderPathThickness() : GetBorderPathThickness();
            path.Stroke = (rail.IsHighlighted
                ? Brushes.Red
                : (rail.IsUsedOnPreviousLevel ? Brushes.SeaGreen : Brushes.Blue));
            UpdateRailZindex(path, rail);
        }

        void MarkSelectedRailsAsUsedOnPreviousLevel()
        {
            foreach (var rail in _selectedRails)
                rail.IsUsedOnPreviousLevel = !rail.IsUsedOnPreviousLevel;
        }

        GraphmapsEdge GetVEdgeOfRail(Rail rail)
        {
            if (rail.TopRankedEdgeInfoOfTheRail == null) return null; // skeleton edge
            var dEdge = (DrawingEdge)rail.TopRankedEdgeInfoOfTheRail.Edge.UserData;
            IViewerObject vEdge;
            if (_drawingObjectsToIViewerObjects.TryGetValue(dEdge, out vEdge))
                return (GraphmapsEdge)vEdge;
            return null;
        }


        void RemoveNoLongerVisibleRails(RailGraph oGraph)
        {

            var railsToRemove = new List<Rail>();
            foreach (var rail in _visibleRailsToFrameworkElems.Keys)
            {
 
                if (!oGraph.Rails.Contains(rail))
                    railsToRemove.Add(rail);
            }
            RemoveRustyRails(railsToRemove);
        }

        void RemoveRustyRails(List<Rail> railsToRemove)
        {
            foreach (var rail in railsToRemove)
            {
                RemoveRail(rail);
            }
        }

        void RemoveRail(Rail rail)
        {
             _graphCanvas.Children.Remove(_visibleRailsToFrameworkElems[rail]);
            _visibleRailsToFrameworkElems.Remove(rail);
        }


        /*
                void TestCorrectness(GeometryGraph oGraph, Set<Drawing.Node> oDrawingNodes, Set<DrawingEdge> oDrawgingEdges) {
                    if (Entities.Count() != oGraph.Nodes.Count + oGraph.Edges.Count) {
                        foreach (var newDrawingNode in oDrawingNodes) {
                            if (!drawingObjectsToIViewerObjects.ContainsKey(newDrawingNode))
                                System.Diagnostics.Debug.WriteLine();
                        }
                        foreach (var drawingEdge in oDrawgingEdges) {
                            if (!drawingObjectsToIViewerObjects.ContainsKey(drawingEdge))
                                System.Diagnostics.Debug.WriteLine();
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

        void ProcessEdgeAdditions(Set<DrawingEdge> edgesToAdd)
        {
            foreach (var drawingEdge in edgesToAdd)
                UnhideEdge(drawingEdge);
            //CreateEdge(drawingEdge, lgSettings);
        }


        void ProcessEdgeRemovals(Set<DrawingEdge> edgesToRemove)
        {
            foreach (var edge in edgesToRemove)
                HideVEdge(edge);
        }

        void HideVNode(Node drawingNode)
        {
            IViewerObject inode;
            if (!_drawingObjectsToIViewerObjects.TryGetValue(drawingNode, out inode))
                return;
            var vnode = (GraphmapsNode)inode;
            foreach (var fe in vnode.FrameworkElements)
                _graphCanvas.Children.Remove(fe);
            _drawingObjectsToIViewerObjects.Remove(drawingNode);
        }

        void UnhideVNode(Node drawingNode)
        {

            drawingNode.IsVisible = true;

            if (!_drawingObjectsToIViewerObjects.ContainsKey(drawingNode))
            {
                CreateVNode(drawingNode);
                return;
            }

            var vnode = (GraphmapsNode)_drawingObjectsToIViewerObjects[drawingNode];
            foreach (var fe in vnode.FrameworkElements)
                GraphCanvasChildrenAdd(fe);
        }

        /// <summary>
        /// creates a viewer node
        /// </summary>
        /// <param name="drawingNode"></param>
        /// <returns></returns>
        public IViewerNode CreateIViewerNode(Node drawingNode)
        {
            throw new NotImplementedException();
        }

        void Clear()
        {
            ClearGraphCanvasChildren();
            _drawingObjectsToIViewerObjects.Clear();
            _visibleRailsToFrameworkElems.Clear();
            _tileDictionary.Clear();
            _tileFetcher.Clear();

        }


        void ClearChildrenButNoRectToFill()
        {
            _graphCanvas.Children.Clear();
            _graphCanvas.Children.Add(_rectToFillCanvas);
        }

        void ClearGraphCanvasChildren()
        {
            if (_graphCanvas.Dispatcher.CheckAccess())
                ClearChildrenButNoRectToFill();
            else _graphCanvas.Dispatcher.Invoke(ClearChildrenButNoRectToFill);
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
        public void SetInitialTransform()
        {
            if (_drawingGraph == null || GeomGraph == null) return;

            var scale = FitFactor;
            var graphCenter = GeomGraph.BoundingBox.Center;
            var vp = new Rectangle(new Point(0, 0),
                new Point(_graphCanvas.RenderSize.Width, _graphCanvas.RenderSize.Height));
            SetTransformOnViewport(scale, graphCenter, vp);
            SetScaleRangeForLgLayoutSettings();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tileBox"></param>
        /// <param name="ix"></param>
        /// <param name="iy"></param>
        /// <param name="renderBitmap"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="scale"></param>
        /// <param name="grid"></param>
        /// <returns>true if the tile is small, that is it intersects only a few nodes</returns>
        public bool DrawImageOfTile(Rectangle tileBox, int ix, int iy, RenderTargetBitmap renderBitmap, int w, int h, double scale, GridTraversal grid)
        {
            bool tileIsAlmostEmpty = _lgLayoutSettings.Interactor.NumberOfNodesOfLastLayerIntersectedRectIsLessThanBound(grid.ILevel, tileBox, 60); // test
            if (tileIsAlmostEmpty)
                return true;

            SetTransformOnViewportWithoutRaisingViewChangeEvent(scale, tileBox.Center, new Rectangle(0, 0, w, h));
            RenderTile(renderBitmap, w, h);
            SaveBitmapToFile(ix, iy, renderBitmap, grid);
            WpfMemoryPressureHelper.ResetTimers();
            return false;
        }

        void SaveBitmapToFile(int ix, int iy, RenderTargetBitmap renderBitmap, GridTraversal grid)
        {
            using (FileStream outStream = CreateTileFileStream(grid, ix, iy))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                encoder.Save(outStream);
            }
        }

        void RenderTile(RenderTargetBitmap renderBitmap, int w, int h)
        {
            // Measure and arrange the surface
            // VERY IMPORTANT
            var size = new Size(w, h);
            _graphCanvas.Measure(size);
            _graphCanvas.Arrange(new Rect(size));
            renderBitmap.Clear();
            renderBitmap.Render(_graphCanvas);            
        }

        bool RectIsEmptyAfterLevel(Rectangle tileBox, int iLevel)
        {
            return _lgLayoutSettings.Interactor.RectIsEmptyStartingFromLevel(tileBox, iLevel + 1);
        }

        void InvalidateNodesForTilesOnLevel(int iLevel)
        {
            foreach (
                var node in _drawingGraph.Nodes.Concat(_drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf()))
            {
                IViewerObject o;
                if (!_drawingObjectsToIViewerObjects.TryGetValue(node, out o)) continue;
                var vnode = ((GraphmapsNode)o);
                AdjustNodeVisualsForHighZoomLevel(iLevel, vnode, node);
            }
        }

        void AdjustNodeVisualsForHighZoomLevel(int iLevel, GraphmapsNode vnode, Node node)
        {
            var levelScale = GetLevelScale(iLevel);
            var nodeDotWidth = _lgLayoutSettings.NodeDotWidthInInches * DpiX / levelScale;
            var nodeMinWidth = _lgLayoutSettings.NodeDotWidthInInchesMinInImage * DpiX / levelScale;
            if (vnode.LgNodeInfo.ZoomLevel > Math.Pow(2, iLevel))
                nodeDotWidth = Math.Max(nodeMinWidth,
                    nodeDotWidth * (2 * Math.Pow(2, iLevel) / vnode.LgNodeInfo.ZoomLevel));
            else
            {
                HideVNode(node);
                return;
            }

            vnode.Node.Attr.LineWidth = 0; //GetBorderPathThickness(tileScale);
            //jyoti
            //this is to make the background nodes smaller
            //vnode.InvalidateNodeDot(nodeDotWidth * 0.8); // make them just a bit smaller
            vnode.InvalidateNodeDot((_lgLayoutSettings.GetMaximalZoomLevel()+2)* Math.Log(nodeDotWidth)); // make them just a bit smaller
            vnode.HideNodeLabel();
            vnode.SetLowTransparency();
        }

        double GetLevelScale(int iLevel)
        {
            return FitFactor * Math.Pow(2, iLevel);
        }


        public void CaptureFrame()
        {
            var fileName = "C:/tmp/video/frame" + _frame + ".png";
            TakeScreenShot(fileName);
            _frame++;
        }

        public void TakeScreenShot(string fileName)
        {
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings == null) return;

            int w = (int)_graphCanvas.ActualWidth;
            int h = (int)_graphCanvas.ActualHeight;
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(w, h, DpiX, DpiY, PixelFormats.Pbgra32);


            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.Gray, null,
                    new Rect(new WpfPoint(), new Size(w, h)));
            }
            renderBitmap.Render(drawingVisual);
            renderBitmap.Render(_graphCanvas);

            if (fileName != null)
                // Create a file stream for saving image
                using (FileStream outStream = new FileStream(fileName, FileMode.Create))
                {
                    // Use png encoder for our data
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    // push the rendered bitmap to it
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    // save the data to the stream
                    encoder.Save(outStream);
                }
        }

        //        public Image DrawImage(string fileName) {
        //            var ltrans = _graphCanvas.LayoutTransform;
        //            var rtrans = _graphCanvas.RenderTransform;
        //            _graphCanvas.LayoutTransform = null;
        //            _graphCanvas.RenderTransform = null;
        //            var renderSize = _graphCanvas.RenderSize;
        //
        //            double scale = FitFactor;
        //            int w = (int) (this.GeomGraph.Width*scale);
        //            int h = (int) (GeomGraph.Height*scale);
        //
        //            SetTransformOnViewportWithoutRaisingViewChangeEvent(scale, GeomGraph.BoundingBox.Center,
        //                new Rectangle(0, 0, w, h));
        //
        //            Size size = new Size(w, h);
        //            // Measure and arrange the surface
        //            // VERY IMPORTANT
        //            _graphCanvas.Measure(size);
        //            _graphCanvas.Arrange(new Rect(size));
        //
        //            foreach (
        //                var node in _drawingGraph.Nodes.Concat(_drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf())) {
        //                IViewerObject o;
        //                if (_drawingObjectsToIViewerObjects.TryGetValue(node, out o)) {
        //                    ((GraphmapsNode) o).Invalidate();
        //                }
        //            }
        //
        //            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(w, h, DpiX, DpiY, PixelFormats.Pbgra32);
        //            renderBitmap.Render(_graphCanvas);
        //
        //            if (fileName != null)
        //                // Create a file stream for saving image
        //                using (FileStream outStream = new FileStream(fileName, FileMode.Create)) {
        //                    // Use png encoder for our data
        //                    PngBitmapEncoder encoder = new PngBitmapEncoder();
        //                    // push the rendered bitmap to it
        //                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
        //                    // save the data to the stream
        //                    encoder.Save(outStream);
        //                }
        //
        //            _graphCanvas.LayoutTransform = ltrans;
        //            _graphCanvas.RenderTransform = rtrans;
        //            _graphCanvas.Measure(renderSize);
        //            _graphCanvas.Arrange(new Rect(renderSize));
        //
        //            return new Image {Source = renderBitmap};
        //        }

        void SetTransformOnViewportWithoutRaisingViewChangeEvent(double scale, Point graphCenter,
            Core.Geometry.Rectangle vp)
        {
            var dx = vp.Width / 2 - scale * graphCenter.X;
            var dy = vp.Height / 2 + scale * graphCenter.Y;

            SetTransformWithoutRaisingViewChangeEvent(scale, dx, dy);

        }

        void SetTransformOnViewport(double scale, Point graphCenter, Core.Geometry.Rectangle vp)
        {
            var dx = vp.Width / 2 - scale * graphCenter.X;
            var dy = vp.Height / 2 + scale * graphCenter.Y;
            SetTransform(scale, dx, dy);
        }


        void SetTransform(double scale, double dx, double dy)
        {
            if (ScaleIsOutOfRange(scale)) return;
            _graphCanvas.RenderTransform = new MatrixTransform(scale, 0, 0, -scale, dx, dy);

            if (ViewChangeEvent != null)
                ViewChangeEvent(null, null);
        }

        void SetTransformWithoutRaisingViewChangeEvent(double scale, double dx, double dy)
        {
            if (ScaleIsOutOfRange(scale)) return;
            _graphCanvas.RenderTransform = new MatrixTransform(scale, 0, 0, -scale, dx, dy);
        }

        bool ScaleIsOutOfRange(double scale)
        {
            return !((LgLayoutSettings)_drawingGraph.LayoutAlgorithmSettings).ScaleInterval.Contains(scale);
        }


        /// <summary>
        /// The geometry graph scaled by this number fits tightly into the viewport
        /// </summary>
        double FitFactor
        {
            get
            {
                var geomGraph = GeomGraph;
                if (_drawingGraph == null || geomGraph == null ||

                    geomGraph.Width == 0 || geomGraph.Height == 0)
                    return 1;

                var size = SystemParameters.WorkArea.Size; //_graphCanvas.RenderSize; // todo: change resize behaviour

                return GetFitFactor(size);
            }
        }

        /// <summary>
        /// The geometry graph scaled by this number fits tightly into rect
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        double GetFitFactor(Size rect)
        {
            var geomGraph = GeomGraph;
            return geomGraph == null ? 1 : Math.Min(rect.Width / geomGraph.Width, rect.Height / geomGraph.Height);
        }

        void PushDataFromLayoutGraphToFrameworkElements()
        {
            OGraphChanged();
        }

        void CreateRectToFillCanvas()
        {
            _rectToFillCanvas = new WpfRectangle();
            Canvas.SetLeft(_rectToFillCanvas, 0);
            Canvas.SetTop(_rectToFillCanvas, 0);
            _rectToFillCanvas.Fill = Brushes.Transparent;
            Panel.SetZIndex(_rectToFillCanvas, -100);
            GraphCanvasChildrenAdd(_rectToFillCanvas);
        }

        GraphmapsEdge CreateEdge(DrawingEdge edge)
        {
            lock (this)
            {
                IViewerObject iedge;
                if (_drawingObjectsToIViewerObjects.TryGetValue(edge, out iedge))
                    return (GraphmapsEdge)iedge;
                return CreateEdgeForLgCase(edge);
            }
        }

        GraphmapsEdge UnhideEdge(DrawingEdge edge)
        {
            IViewerObject iedge;
            if (!_drawingObjectsToIViewerObjects.TryGetValue(edge, out iedge))
            {
                return CreateEdge(edge);
            }

            var vEdge = (GraphmapsEdge)iedge;
            if (vEdge.CurvePath != null)
                GraphCanvasChildrenAdd(vEdge.CurvePath);
            return vEdge;
        }

        void HideVEdge(DrawingEdge edge)
        {
            IViewerObject iedge;
            if (!_drawingObjectsToIViewerObjects.TryGetValue(edge, out iedge))
            {
                return;
            }

            var vEdge = (GraphmapsEdge)iedge;
            if (vEdge.CurvePath != null)
            {
                _graphCanvas.Children.Remove(vEdge.CurvePath);
            }
            _drawingObjectsToIViewerObjects.Remove(edge);
        }

        GraphmapsEdge CreateEdgeForLgCase(DrawingEdge edge)
        {
            return (GraphmapsEdge)(_drawingObjectsToIViewerObjects[edge] = new GraphmapsEdge(edge, _lgLayoutSettings)
            {
                PathStrokeThicknessFunc = () => GetBorderPathThickness() * edge.Attr.LineWidth
            });
        }


        void MakeAllNodesVisible()
        {
            foreach (var node in _drawingGraph.Nodes)
            {
                //jyoti - for the case of include node position from file
                if (!_lgLayoutSettings.lgGeometryGraph.Nodes.Contains(node.GeometryNode)) return;


                node.IsVisible = true;
                UnhideVNode(node);
                //VNode vnode = (VNode)CreateVNode(node);
                //vnode.SetLowTransparency();
            }
        }

        GraphmapsNode CreateVNode(Node node)
        {
            lock (this)
            {
                Debug.Assert(!_drawingObjectsToIViewerObjects.ContainsKey(node));
                FrameworkElement feOfLabel = CreateAndRegisterFrameworkElementOfDrawingNode(node);

                var vn = new GraphmapsNode(node, GetCorrespondingLgNode(node), feOfLabel,
                    e => (GraphmapsEdge)_drawingObjectsToIViewerObjects[e], () => GetBorderPathThickness() * node.Attr.LineWidth, _lgLayoutSettings);

                foreach (var fe in vn.FrameworkElements)
                {
                    GraphCanvasChildrenAdd(fe);
                    Panel.SetZIndex(fe, 500);
                }
                if (feOfLabel != null)
                {
                    Panel.SetZIndex(feOfLabel, 600);
                    var viewbox = feOfLabel as Viewbox;
                    if (viewbox != null)
                        if (!string.IsNullOrEmpty(node.LabelText))
                            viewbox.IsHitTestVisible = true;
                }

                _drawingObjectsToIViewerObjects[node] = vn;
                node.Attr.LineWidth = GetBorderPathThickness();
                return vn;
            }
        }

        public FrameworkElement CreateAndRegisterFrameworkElementOfDrawingNode(Node node)
        {
            //SetNodeAppearence((VNode) _drawingObjectsToIViewerObjects[node]);
            return CreateViewboxForDrawingObj(node);
        }

        LgNodeInfo GetCorrespondingLgNode(Node node)
        {
            var lgGraphBrowsingSettings = _drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings;
            return lgGraphBrowsingSettings == null
                ? null
                : lgGraphBrowsingSettings.GeometryNodesToLgNodeInfos[node.GeometryNode];
        }

        void RemoveAllVisibleRails()
        {
            foreach (var rail in _visibleRailsToFrameworkElems.Keys)
            {
                var fe = _visibleRailsToFrameworkElems[rail];
                GraphCanvas.Children.Remove(fe);
            }
            _visibleRailsToFrameworkElems.Clear();
        }

        void CreateTileDirectoryIfNeededAndRemoveEverythingUnderIt()
        {
            if (Directory.Exists(TileDirectory))
                CleanTileDirectory();
            Directory.CreateDirectory(TileDirectory);
        }

        void CleanTileDirectory()
        {
            var tileDirInfo = new DirectoryInfo(TileDirectory);
            foreach (FileInfo file in tileDirInfo.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in tileDirInfo.GetDirectories())
                dir.Delete(true);
        }

        void PopulateGeometryOfGeometryGraph()
        {
            geometryGraphUnderLayout = _drawingGraph.GeometryGraph;
            foreach (
                Core.Layout.Node msaglNode in
                    geometryGraphUnderLayout.Nodes)
            {
                var node = (Node)msaglNode.UserData;
                if (_graphCanvas.Dispatcher.CheckAccess())
                    msaglNode.BoundaryCurve = GetNodeBoundaryCurve(node);
                else
                {
                    var msagNodeInThread = msaglNode;
                    _graphCanvas.Dispatcher.Invoke(() => msagNodeInThread.BoundaryCurve = GetNodeBoundaryCurve(node));
                }

                node.Attr.LabelWidthToHeightRatio = node.BoundingBox.Width / node.BoundingBox.Height;
                //AssignLabelWidthHeight(msaglNode, msaglNode.UserData as DrawingObject);
            }

            foreach (
                Cluster cluster in geometryGraphUnderLayout.RootCluster.AllClustersWideFirstExcludingSelf())
            {
                var subgraph = (Subgraph)cluster.UserData;
                if (_graphCanvas.Dispatcher.CheckAccess())
                    cluster.CollapsedBoundary = GetClusterCollapsedBoundary(subgraph);
                else
                {
                    var clusterInThread = cluster;
                    _graphCanvas.Dispatcher.Invoke(
                        () => clusterInThread.BoundaryCurve = GetClusterCollapsedBoundary(subgraph));
                }
                if (cluster.RectangularBoundary == null)
                    cluster.RectangularBoundary = new RectangularClusterBoundary();
                cluster.RectangularBoundary.TopMargin = subgraph.DiameterOfOpenCollapseButton + 0.5 +
                                                        subgraph.Attr.LineWidth / 2;
                //AssignLabelWidthHeight(msaglNode, msaglNode.UserData as DrawingObject);
            }

        }

        void SetLabelWidthToHeightRatiosOfGeometryGraph()
        {
            geometryGraphUnderLayout = _drawingGraph.GeometryGraph;
            foreach (
                Core.Layout.Node msaglNode in
                    geometryGraphUnderLayout.Nodes)
            {
                var node = (Node)msaglNode.UserData;
                if (_graphCanvas.Dispatcher.CheckAccess())
                {
                    node.Attr.LabelWidthToHeightRatio = GetLabelWidthToHeightRatioByMeasuringText(node);
                }
                else
                {
                    var msagNodeInThread = msaglNode;
                    _graphCanvas.Dispatcher.Invoke(() => node.Attr.LabelWidthToHeightRatio = GetLabelWidthToHeightRatioByMeasuringText(node));
                }
            }
        }

        ICurve GetClusterCollapsedBoundary(Subgraph subgraph)
        {
            return GetApproximateCollapsedBoundary(subgraph);
        }

        ICurve GetApproximateCollapsedBoundary(Subgraph subgraph)
        {
            if (textBoxForApproxNodeBoundaries == null)
                SetUpTextBoxForApproxNodeBoundaries();


            double width, height;
            if (String.IsNullOrEmpty(subgraph.LabelText))
                height = width = subgraph.DiameterOfOpenCollapseButton;
            else
            {
                double a = ((double)subgraph.LabelText.Length) / textBoxForApproxNodeBoundaries.Text.Length *
                           ((double)subgraph.Label.FontSize) / Label.DefaultFontSize;
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



        ICurve GetNodeBoundaryCurve(Node node)
        {
            return GetNodeBoundaryCurveByMeasuringText(node);
        }


        public static Size MeasureText(string text, FontFamily family, double size, Visual visual = null) {
            FormattedText formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(family, new System.Windows.FontStyle(), FontWeights.Regular, FontStretches.Normal),
                size,
                Brushes.Black,
                null,
                VisualTreeHelper.GetDpi(visual).PixelsPerDip);

            return new Size(formattedText.Width, formattedText.Height);
        }

        ICurve GetNodeBoundaryCurveByMeasuringText(Node node)
        {
            double width, height;
            if (String.IsNullOrEmpty(node.LabelText))
            {
                width = 10;
                height = 10;
            }
            else
            {
                var size = MeasureText(node.LabelText, new FontFamily(node.Label.FontName), node.Label.FontSize, GraphCanvas);
                width = size.Width;
                height = size.Height;

                if (width < _drawingGraph.Attr.MinNodeWidth)
                    width = _drawingGraph.Attr.MinNodeWidth;
                if (height < _drawingGraph.Attr.MinNodeHeight)
                    height = _drawingGraph.Attr.MinNodeHeight;
            }
            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }

        double GetLabelWidthToHeightRatioByMeasuringText(Node node)
        {
            double width, height;
            if (String.IsNullOrEmpty(node.LabelText))
            {
                width = 10;
                height = 10;
            }
            else
            {
                var size = MeasureText(node.LabelText, new FontFamily(node.Label.FontName), node.Label.FontSize, GraphCanvas);
                width = size.Width;
                height = size.Height;

                if (width < _drawingGraph.Attr.MinNodeWidth)
                    width = _drawingGraph.Attr.MinNodeWidth;
                if (height < _drawingGraph.Attr.MinNodeHeight)
                    height = _drawingGraph.Attr.MinNodeHeight;
            }
            return width / height;
        }

        void SetUpTextBoxForApproxNodeBoundaries()
        {
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

        FrameworkElement CreateViewboxForDrawingObj(DrawingObject drawingObj)
        {
            if (drawingObj is Subgraph)
                return null; //todo: add Label support later
            var labeledObj = drawingObj as Drawing.ILabeledObject;
            if (labeledObj == null)
                return null;

            var drawingLabel = labeledObj.Label;
            if (drawingLabel == null)
                return null;

            Viewbox viewbox = null;

            if (_graphCanvas.Dispatcher.CheckAccess())
                viewbox = CreateViewbox(CreateTextBlock(drawingLabel));
            else
                _graphCanvas.Dispatcher.Invoke(() => viewbox = CreateViewbox(CreateTextBlock(drawingLabel)));

            return viewbox;
        }

        static TextBlock CreateTextBlock(Label drawingLabel)
        {
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

            textBlock.Background = Brushes.Transparent;
            textBlock.IsHitTestVisible = true;
            return textBlock;
        }

        static Viewbox CreateViewbox(TextBlock textBlock)
        {
            var viewBox = new Viewbox();
            viewBox.Width = textBlock.Width;
            viewBox.Height = textBlock.Height;
            viewBox.Child = textBlock;

            /// todo: remove inner padding from textboxes
            // debug: show actual size
            //textBlock.Background = Brushes.Red;
            //textBlock.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            //textBlock.LineHeight = textBlock.Height;
            //textBlock.FontSize = textBlock.Height;
            return viewBox;
        }


        public void DrawRubberLine(MsaglMouseEventArgs args)
        {
            DrawRubberLine(ScreenToSource(args));
        }

        public void StopDrawingRubberLine()
        {
            _graphCanvas.Children.Remove(_rubberLinePath);
            _rubberLinePath = null;
            _graphCanvas.Children.Remove(targetArrowheadPathForRubberEdge);
            targetArrowheadPathForRubberEdge = null;
        }

        public void AddEdge(IViewerEdge edge, bool registerForUndo)
        {
            //if (registerForUndo) drawingLayoutEditor.RegisterEdgeAdditionForUndo(edge);

            var drawingEdge = edge.Edge;
            Edge geomEdge = drawingEdge.GeometryEdge;

            _drawingGraph.AddPrecalculatedEdge(drawingEdge);
            _drawingGraph.GeometryGraph.Edges.Add(geomEdge);

        }

        public IViewerEdge CreateEdgeWithGivenGeometry(DrawingEdge drawingEdge)
        {
            return CreateEdge(drawingEdge);
        }

        public void AddNode(IViewerNode node, bool registerForUndo)
        {
            if (_drawingGraph == null)
                throw new InvalidOperationException(); // adding a node when the graph does not exist
            var vNode = (GraphmapsNode)node;
            _drawingGraph.AddNode(vNode.Node);
            _drawingGraph.GeometryGraph.Nodes.Add(vNode.Node.GeometryNode);
            layoutEditor.AttachLayoutChangeEvent(vNode);
            GraphCanvasChildrenAdd(vNode.FrameworkElementOfNodeForLabel);
            layoutEditor.CleanObstacles();
        }

        public void RemoveEdge(IViewerEdge edge, bool registerForUndo)
        {
            lock (this)
            {
                var vedge = (GraphmapsEdge)edge;
                var dedge = vedge.Edge;
                _drawingGraph.RemoveEdge(dedge);
                _drawingGraph.GeometryGraph.Edges.Remove(dedge.GeometryEdge);
                _drawingObjectsToIViewerObjects.Remove(dedge);

                vedge.RemoveItselfFromCanvas(_graphCanvas);
            }
        }

        public void RemoveNode(IViewerNode node, bool registerForUndo)
        {
            lock (this)
            {
                RemoveEdges(node.Node.OutEdges);
                RemoveEdges(node.Node.InEdges);
                RemoveEdges(node.Node.SelfEdges);
                _drawingObjectsToIViewerObjects.Remove(node.Node);
                var vnode = (GraphmapsNode)node;
                vnode.DetouchFromCanvas(_graphCanvas);

                _drawingGraph.RemoveNode(node.Node);
                _drawingGraph.GeometryGraph.Nodes.Remove(node.Node.GeometryNode);
                layoutEditor.DetachNode(node);
                layoutEditor.CleanObstacles();
            }
        }

        void RemoveEdges(IEnumerable<DrawingEdge> drawingEdges)
        {
            foreach (var de in drawingEdges.ToArray())
            {
                var vedge = (GraphmapsEdge)_drawingObjectsToIViewerObjects[de];
                RemoveEdge(vedge, false);
            }
        }


        public IViewerEdge RouteEdge(DrawingEdge drawingEdge)
        {
            var geomEdge = GeometryGraphCreator.CreateGeometryEdgeFromDrawingEdge(drawingEdge);
            var geomGraph = _drawingGraph.GeometryGraph;
            LayoutHelpers.RouteAndLabelEdges(geomGraph, _drawingGraph.LayoutAlgorithmSettings, new[] { geomEdge }, 0, null);
            return CreateEdge(drawingEdge);
        }

        public IViewerGraph ViewerGraph { get; set; }

        public double ArrowheadLength
        {
            get { return 0.2 * DpiX / CurrentScale; }
        }

        public void SetSourcePortForEdgeRouting(Point portLocation)
        {
            _sourcePortLocationForEdgeRouting = portLocation;
            if (_sourcePortCircle == null)
            {
                _sourcePortCircle = CreatePortPath();
                GraphCanvasChildrenAdd(_sourcePortCircle);
            }
            _sourcePortCircle.Width = _sourcePortCircle.Height = UnderlyingPolylineCircleRadius;
            _sourcePortCircle.StrokeThickness = _sourcePortCircle.Width / 10;
            Common.PositionFrameworkElement(_sourcePortCircle, portLocation, 1);
        }

        Ellipse CreatePortPath()
        {
            return new Ellipse
            {
                Stroke = Brushes.Brown,
                Fill = Brushes.Brown,
            };
        }

        public void SetTargetPortForEdgeRouting(Point portLocation)
        {
            if (TargetPortCircle == null)
            {
                TargetPortCircle = CreatePortPath();
                GraphCanvasChildrenAdd(TargetPortCircle);
            }
            TargetPortCircle.Width = TargetPortCircle.Height = UnderlyingPolylineCircleRadius;
            TargetPortCircle.StrokeThickness = TargetPortCircle.Width / 10;
            Common.PositionFrameworkElement(TargetPortCircle, portLocation, 1);
        }

        public void RemoveSourcePortEdgeRouting()
        {
            _graphCanvas.Children.Remove(_sourcePortCircle);
            _sourcePortCircle = null;
        }

        public void RemoveTargetPortEdgeRouting()
        {
            _graphCanvas.Children.Remove(TargetPortCircle);
            TargetPortCircle = null;
        }


        public void DrawRubberEdge(EdgeGeometry edgeGeometry)
        {
            if (_rubberEdgePath == null)
            {
                _rubberEdgePath = new Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = GetBorderPathThickness() * 3
                };
                GraphCanvasChildrenAdd(_rubberEdgePath);
                targetArrowheadPathForRubberEdge = new Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = GetBorderPathThickness() * 3
                };
                GraphCanvasChildrenAdd(targetArrowheadPathForRubberEdge);
            }
            _rubberEdgePath.Data = GraphmapsEdge.GetICurveWpfGeometry(edgeGeometry.Curve);
            targetArrowheadPathForRubberEdge.Data = GraphmapsEdge.DefiningTargetArrowHead(edgeGeometry,
                edgeGeometry.LineWidth);
        }


        bool UnderLayout
        {
            get { return _backgroundWorker != null; }
        }

        public void StopDrawingRubberEdge()
        {
            _graphCanvas.Children.Remove(_rubberEdgePath);
            _graphCanvas.Children.Remove(targetArrowheadPathForRubberEdge);
            _rubberEdgePath = null;
            targetArrowheadPathForRubberEdge = null;
        }


        public PlaneTransformation Transform
        {
            get
            {
                var mt = _graphCanvas.RenderTransform as MatrixTransform;
                if (mt == null)
                    return PlaneTransformation.UnitTransformation;
                var m = mt.Matrix;
                return new PlaneTransformation(m.M11, m.M12, m.OffsetX, m.M21, m.M22, m.OffsetY);
            }
            set
            {
                SetRenderTransformWithoutRaisingEvents(value);

                if (ViewChangeEvent != null)
                    ViewChangeEvent(null, null);
            }
        }

        void SetRenderTransformWithoutRaisingEvents(PlaneTransformation value)
        {
            _graphCanvas.RenderTransform = new MatrixTransform(value[0, 0], value[0, 1], value[1, 0], value[1, 1],
                value[0, 2],
                value[1, 2]);
        }


        public bool NeedToCalculateLayout
        {
            get { return needToCalculateLayout; }
            set { needToCalculateLayout = value; }
        }

        /// <summary>
        /// the cancel token used to cancel a long running layout
        /// </summary>
        public CancelToken CancelToken
        {
            get { return _cancelToken; }
            set { _cancelToken = value; }
        }

        public string TileDirectory { get; set; }

        public int VisibleChildrenCount
        {
            get { return GraphCanvas.Children.Count; }
        }

        public int DrawingChildrenCount
        {
            get { return _drawingObjectsToIViewerObjects.Count; }
        }

        public int VisRailCount
        {
            get { return _visibleRailsToFrameworkElems.Count; }
        }

        public bool IncrementalDraggingModeAlways => false;

        public void DrawRubberLine(Point rubberEnd)
        {
            if (_rubberLinePath == null)
            {
                _rubberLinePath = new Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = GetBorderPathThickness() * 3
                };
                GraphCanvasChildrenAdd(_rubberLinePath);
                //                targetArrowheadPathForRubberLine = new Path {
                //                    Stroke = Brushes.Black,
                //                    StrokeThickness = GetBorderPathThickness()*3
                //                };
                //                graphCanvas.Children.Add(targetArrowheadPathForRubberLine);
            }
            _rubberLinePath.Data =
                GraphmapsEdge.GetICurveWpfGeometry(new LineSegment(_sourcePortLocationForEdgeRouting, rubberEnd));
        }

        public void StartDrawingRubberLine(Point startingPoint) { }

        #endregion

        public IViewerNode CreateIViewerNode(Node drawingNode, Point center, object visualElement)
        {
            throw new NotImplementedException();
        }

        public void SelectAllNodesOnVisibleLevels()
        {
            List<LgNodeInfo> nodesToSelect = _lgLayoutSettings.Interactor.GetAllNodesOnVisibleLayers();
            ClearNodesSelection();
            foreach (var node in nodesToSelect)
                SelectNodeNoChangeEvent(node, true);
            ViewChangeEvent(null, null);
        }

        bool getLgSettings(out LgLayoutSettings settings)
        {
            settings = _drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings;
            return settings != null;
        }

        /// <summary>
        /// generate and load tiles
        /// </summary>
        public void GenerateTiles()
        {
            var timer = new Timer();
            timer.Start();
            var renderTransform = _graphCanvas.RenderTransform.Clone();
            bool normalFlow = false;
            try
            {
                Debug.Assert(!string.IsNullOrEmpty(TileDirectory));
                Clear();
                CreateTileDirectoryIfNeededAndRemoveEverythingUnderIt();
                
                MakeAllNodesVisible();
                UpdateAllNodeBorders();
                RemoveAllVisibleRails();
                var fitFactor = GetFitFactor(new Size(800, 800));
                var w = GeomGraph.Width * fitFactor;
                var h = GeomGraph.Height * fitFactor;
                var renderBitmap = new RenderTargetBitmap((int)w, (int)h, 96, 96, PixelFormats.Pbgra32);
                var tileList = new List<Tuple<int, int>> { new Tuple<int, int>(0, 0) };
                var nextLevelTileList = new List<Tuple<int, int>>();
                for (int iLevel = 0; iLevel < _lgLayoutSettings.Interactor.GetNumberOfLevels() - 1 && tileList.Count > 0; )
                {
                    GridTraversal grid = new GridTraversal(GeomGraph.BoundingBox, iLevel);
                    System.Diagnostics.Debug.WriteLine("Drawing tiles on level {0} ...", iLevel);
                    DrawTilesOnLevel(tileList, grid, renderBitmap, w, h, fitFactor * Math.Pow(2, iLevel), iLevel,
                        nextLevelTileList);
                    iLevel++;
                    if (iLevel == _lgLayoutSettings.Interactor.GetNumberOfLevels() - 1) break;
                    tileList = SwapTileLists(tileList, ref nextLevelTileList);
                }
                System.Diagnostics.Debug.WriteLine("Done");
                normalFlow = true;
            }
            catch (Exception e)
            {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine("did not succeeed to save all tiles");
            }
            finally
            {
                // restore the original state
                foreach (
                    var node in
                        _drawingGraph.Nodes.Concat(_drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf()))
                {
                    HideVNode(node);
                }
                Clear();
                _graphCanvas.RenderTransform = renderTransform;
                if (normalFlow)
                    InitTiles();
                if (ViewChangeEvent != null)
                    ViewChangeEvent(null, null);
            }
            timer.Stop();
        }
        FileStream CreateTileFileStream(GridTraversal grid, int ix, int iy)
        {
            var splitName = grid.SplitTileNameOnDirectories(ix, iy);
            string fname = TileDirectory;
            for (int i = splitName.Count - 1; i >= 0; i--)
            {
                fname = System.IO.Path.Combine(fname, splitName[i]);
                if (i > 0)
                {
                    if (!Directory.Exists(fname))
                        Directory.CreateDirectory(fname);
                }
            }
            return new FileStream(fname + ".png", FileMode.CreateNew);
        }

        StreamWriter CreateTileStreamWriter(GridTraversal grid, int ix, int iy)
        {
            var splitName = grid.SplitTileNameOnDirectories(ix, iy);
            string fname = TileDirectory;
            for (int i = splitName.Count - 1; i >= 0; i--)
            {
                fname = System.IO.Path.Combine(fname, splitName[i]);
                if (i > 0)
                {
                    if (!Directory.Exists(fname))
                        Directory.CreateDirectory(fname);
                }
            }
            return new StreamWriter(fname + ".list");
        }
        static List<Tuple<int, int>> SwapTileLists(List<Tuple<int, int>> tileList, ref List<Tuple<int, int>> nextLevelTileList)
        {
            tileList.Clear();
            var ttt = tileList;
            tileList = nextLevelTileList;
            nextLevelTileList = ttt;
            return tileList;
        }

        void DrawTilesOnLevel(List<Tuple<int, int>> tileList, GridTraversal grid, RenderTargetBitmap renderBitmap, double w, double h, double scale,
            int iLevel, List<Tuple<int, int>> nextLevelTileList)
        {
            InvalidateNodesForTilesOnLevel(iLevel);
            foreach (var tile in tileList)
            {
                int i = tile.Item1;
                int j = tile.Item2;
                var rect = grid.GetTileRect(i, j);
                bool tileIsAlmostEmpty = DrawImageOfTile(rect, i, j, renderBitmap, (int)w, (int)h, scale, grid);
                if (tileIsAlmostEmpty) continue;
                if (iLevel < _lgLayoutSettings.Interactor.GetNumberOfLevels() - 1)
                    AddTileChildren(i, j, nextLevelTileList, grid, iLevel);
            }
        }

        void AddTileChildren(int i, int j, List<Tuple<int, int>> nextLevelTileList, GridTraversal grid, int iLevel)
        {
            var tileBox = grid.GetTileRect(i, j);
            i *= 2;
            j *= 2;
            var center = tileBox.Center;
            var rect = new Rectangle(tileBox.LeftBottom, center);
            if (!RectIsEmptyAfterLevel(rect, iLevel))
                nextLevelTileList.Add(new Tuple<int, int>(i, j));
            rect = new Rectangle(tileBox.RightBottom, center);
            if (!RectIsEmptyAfterLevel(rect, iLevel))
                nextLevelTileList.Add(new Tuple<int, int>(i + 1, j));

            rect = new Rectangle(tileBox.LeftTop, center);
            if (!RectIsEmptyAfterLevel(rect, iLevel))
                nextLevelTileList.Add(new Tuple<int, int>(i, j + 1));

            rect = new Rectangle(tileBox.RightTop, center);
            if (!RectIsEmptyAfterLevel(rect, iLevel))
                nextLevelTileList.Add(new Tuple<int, int>(i + 1, j + 1));
        }

        void InitTilesRecursively(int iLevel, int ix, int iy, List<GridTraversal> grids, TileType tileType)
        {
            if (grids.Count == 0) return;
            var fname = CreateTileFileName(ix, iy, grids[iLevel]);
            if (tileType == TileType.Image && File.Exists(fname))
            {
                _tileDictionary[new Triple(iLevel, ix, iy)] = TileType.Image;
            }
            else
            {
                if (TileIsEmpty(grids[iLevel].GetTileRect(ix, iy)))
                    return;
                _tileDictionary[new Triple(iLevel, ix, iy)] = TileType.Vector;
                tileType = TileType.Vector;
            }
            if (iLevel == grids.Count - 1)
                return;
            iLevel++;
            ix *= 2;
            iy *= 2;
            InitTilesRecursively(iLevel, ix, iy, grids, tileType);
            InitTilesRecursively(iLevel, ix + 1, iy, grids, tileType);
            InitTilesRecursively(iLevel, ix, iy + 1, grids, tileType);
            InitTilesRecursively(iLevel, ix + 1, iy + 1, grids, tileType);

        }

        bool TileIsEmpty(Rectangle getTileRect)
        {
            return _lgLayoutSettings.Interactor.TileIsEmpty(getTileRect);
        }

        public void InitTiles()
        {
            _tileDictionary.Clear();
            var grids = GetTileGridsForAllLevelsExceptLast();
            InitTilesRecursively(0, 0, 0, grids, TileType.Image);
            UpdateBackgroundTiles();
        }

        List<GridTraversal> GetTileGridsForAllLevelsExceptLast()
        {
            List<GridTraversal> grids = new List<GridTraversal>();
            for (int i = 0; i < _lgLayoutSettings.Interactor.GetNumberOfLevels() - 1; i++)
                grids.Add(new GridTraversal(GeomGraph.BoundingBox, i));
            return grids;
        }

        public string searchnode;
        public void FindNodeAndSelectIt(string text)
        {
            searchnode = text;
            //MessageBox.Show(String.Format("I am searching for {0}", text));
            if (Graph == null) return;
            LgNodeInfo nodeInfo = _stringFinder.Find(text);
            if (nodeInfo != null)
                ZoomToNodeInfo(nodeInfo);
        }
        /*
        //textbox: find node operation on 'Enter'
        void ZoomToNodeInfo(LgNodeInfo nodeInfo)
        {
            //jyoti commented out all the node update : 
            //since the node updates will mess up with the node selection colors
            
            // LN. I enabled it, and it does mess up the colors and the selected nodes.
            // However, without these lines the nodes that are found remain invisible and it is very frustrating.
            // Jyoti, can you fix the selection? I can look at it tomorrow. todo

            nodeInfo.Selected = true;
            _lgLayoutSettings.Interactor.SelectedNodeInfos.Insert(nodeInfo);
            var scale = Math.Max(CurrentScale, nodeInfo.LabelVisibleFromScale);
            var vp = new Rectangle(new Point(0, 0),
                new Point(_graphCanvas.RenderSize.Width, _graphCanvas.RenderSize.Height));
            SetTransformOnViewport(scale, nodeInfo.Center, vp);
            var drobject = nodeInfo.GeometryNode.UserData as DrawingObject;
            if (drobject != null)
            {
                IViewerObject ttt;
                if (_drawingObjectsToIViewerObjects.TryGetValue(drobject, out ttt))
                {
                    Invalidate(ttt);
                }
            }
        }
    }*/
    void ZoomToNodeInfo(LgNodeInfo nodeInfo)
        {
            var scale = Math.Max(CurrentScale, nodeInfo.LabelVisibleFromScale);
            var vp = new Rectangle(new Point(0, 0),
                new Point(_graphCanvas.RenderSize.Width, _graphCanvas.RenderSize.Height));
            SetTransformOnViewport(scale, nodeInfo.Center, vp);
        }
    }

    internal enum TileType
    {
        // Tile represented by an Image 
        Image,
        // if a tile intersects only a few nodes we draw them FrameworkElements
        Vector,

    }
}