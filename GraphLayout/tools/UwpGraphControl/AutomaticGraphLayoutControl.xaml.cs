using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using SkiaSharp;
using SkiaSharp.Views.UWP;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Miscellaneous.LayoutEditing;
using Microsoft.Msagl.Viewers.Uwp;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using ILabeledObject = Microsoft.Msagl.Drawing.ILabeledObject;
using Label = Microsoft.Msagl.Drawing.Label;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using ModifierKeys = Microsoft.Msagl.Drawing.ModifierKeys;
using Point = Microsoft.Msagl.Core.Geometry.Point;



namespace UwpGraphControl {

    public sealed partial class AutomaticGraphLayoutControl : IViewer {
        public AutomaticGraphLayoutControl() {
            this.InitializeComponent();
        }

        public static readonly Windows.UI.Xaml.DependencyProperty CurrentGraphProperty = Windows.UI.Xaml.DependencyProperty.Register(
            "CurrentGraph", typeof(Graph), typeof(AutomaticGraphLayoutControl), new Windows.UI.Xaml.PropertyMetadata(default(Graph), OnCurrentGraphChanged));

        private static void OnCurrentGraphChanged(Windows.UI.Xaml.DependencyObject d, Windows.UI.Xaml.DependencyPropertyChangedEventArgs e) {
            if (d is AutomaticGraphLayoutControl agControl) {
                agControl.Invalidate();
            }
        }

        public Graph CurrentGraph {
            get => (Graph)GetValue(CurrentGraphProperty);
            set => SetValue(CurrentGraphProperty, value);
        }
        readonly Dictionary<DrawingObject, IViewerObject> drawingObjectsToIViewerObjects =
            new Dictionary<DrawingObject, IViewerObject>();

        readonly Dictionary<DrawingObject, SKPaint> drawingObjectsToFrameworkElements =
            new Dictionary<DrawingObject, SKPaint>();


        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e) {

            var canvas = e.Surface.Canvas;
            canvas.Clear();
            ClearGraphViewer();
            CreateFrameworkElementsForLabelsOnly();

        }
        void ClearGraphViewer() {

            drawingObjectsToIViewerObjects.Clear();
            drawingObjectsToFrameworkElements.Clear();
        }


        void CreateFrameworkElementsForLabelsOnly() {
            foreach (var edge in CurrentGraph.Edges) {
                var fe = CreateDefaultFrameworkElementForDrawingObject(edge);
                if (fe != null)
                        fe.Tag = new VLabel(edge, fe);
                
            }

            foreach (var node in CurrentGraph.Nodes)
                CreateDefaultFrameworkElementForDrawingObject(node);
            if (CurrentGraph.RootSubgraph != null)
                foreach (var subGraph in CurrentGraph.RootSubgraph.AllSubgraphsWidthFirstExcludingSelf())
                    CreateDefaultFrameworkElementForDrawingObject(subGraph);
        }


        SKFrameworkElement CreateDefaultFrameworkElementForDrawingObject(DrawingObject drawingObject) {
            lock (this) {
                var textBlock = CreateTextBlockForDrawingObj(drawingObject);
                if (textBlock != null)
                    drawingObjectsToFrameworkElements[drawingObject] = textBlock;
                return textBlock;
            }
        }

        private SKTextBlock CreateTextBlockForDrawingObj(DrawingObject drawingObject) {

            if (!(drawingObject is Subgraph) && drawingObject is ILabeledObject labeledObj && labeledObj.Label != null)
                return CreateTextBlock(labeledObj.Label);
            return null;
        }

        SKTextBlock CreateTextBlock(Label drawingLabel) {
            var textBlock = new SKTextBlock {
                Text = drawingLabel.Text,
                Typeface = SKTypeface.FromFamilyName(drawingLabel.FontName),
                IsAntialias = true,
                TextSize = (float)drawingLabel.FontSize,
                Color = Common.BrushFromMsaglColor(drawingLabel.FontColor)
            };
            drawingObjectsToFrameworkElements[drawingLabel] = textBlock;
            return textBlock;
        }


        public double CurrentScale { get; }
        public IViewerNode CreateIViewerNode(DrawingNode drawingNode, Point center, object visualElement) => throw new NotImplementedException();

        public IViewerNode CreateIViewerNode(DrawingNode drawingNode) => throw new NotImplementedException();

        public bool NeedToCalculateLayout { get; set; }
        public event EventHandler<EventArgs> ViewChangeEvent;
        public event EventHandler<MsaglMouseEventArgs> MouseDown;
        public event EventHandler<MsaglMouseEventArgs> MouseMove;
        public event EventHandler<MsaglMouseEventArgs> MouseUp;
        public event EventHandler<ObjectUnderMouseCursorChangedEventArgs> ObjectUnderMouseCursorChanged;
        public IViewerObject ObjectUnderMouseCursor { get; }
        public void Invalidate(IViewerObject objectToInvalidate) {
        }

        public void Invalidate() {
        }

        public event EventHandler GraphChanged;
        public ModifierKeys ModifierKeys { get; }
        public Point ScreenToSource(MsaglMouseEventArgs e) => throw new NotImplementedException();

        public IEnumerable<IViewerObject> Entities { get; }
        public double DpiX { get; }
        public double DpiY { get; }
        public void OnDragEnd(IEnumerable<IViewerObject> changedObjects) {
            throw new NotImplementedException();
        }

        public double LineThicknessForEditing { get; }
        public bool LayoutEditingEnabled { get; }
        public bool InsertingEdge { get; set; }
        public void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems) {
            throw new NotImplementedException();
        }

        public double UnderlyingPolylineCircleRadius { get; }
        public Graph Graph { get; set; }
        public void StartDrawingRubberLine(Point startingPoint) {
            throw new NotImplementedException();
        }

        public void DrawRubberLine(MsaglMouseEventArgs args) {
            throw new NotImplementedException();
        }

        public void DrawRubberLine(Point point) {
            throw new NotImplementedException();
        }

        public void StopDrawingRubberLine() {
            throw new NotImplementedException();
        }

        public void AddEdge(IViewerEdge edge, bool registerForUndo) {
            throw new NotImplementedException();
        }

        public IViewerEdge CreateEdgeWithGivenGeometry(DrawingEdge drawingEdge) => throw new NotImplementedException();

        public void AddNode(IViewerNode node, bool registerForUndo) {
            throw new NotImplementedException();
        }

        public void RemoveEdge(IViewerEdge edge, bool registerForUndo) {
            throw new NotImplementedException();
        }

        public void RemoveNode(IViewerNode node, bool registerForUndo) {
            throw new NotImplementedException();
        }

        public IViewerEdge RouteEdge(DrawingEdge drawingEdge) => throw new NotImplementedException();

        public IViewerGraph ViewerGraph { get; }
        public double ArrowheadLength { get; }
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

        public PlaneTransformation Transform { get; set; }
    }
}