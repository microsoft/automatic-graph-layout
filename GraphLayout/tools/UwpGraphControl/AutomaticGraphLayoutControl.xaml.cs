using System;
using System.Collections.Generic;
using System.Threading;
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

        GeometryGraph geometryGraphUnderLayout;

        public Graph CurrentGraph {
            get => (Graph)GetValue(CurrentGraphProperty);
            set => SetValue(CurrentGraphProperty, value);
        }
        readonly Dictionary<DrawingObject, IViewerObject> drawingObjectsToIViewerObjects =
            new Dictionary<DrawingObject, IViewerObject>();

        readonly Dictionary<DrawingObject, SKFrameworkElement> drawingObjectsToFrameworkElements =
            new Dictionary<DrawingObject, SKFrameworkElement>();


        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e) {

            var canvas = e.Surface.Canvas;
            canvas.Clear();
            ClearGraphViewer();
            CreateFrameworkElementsForLabelsOnly();
            if (NeedToCalculateLayout) {
                CurrentGraph.CreateGeometryGraph(); 
                PopulateGeometryOfGeometryGraph();
            }

            geometryGraphUnderLayout = CurrentGraph.GeometryGraph;
            //LayoutGraph();
            //PostLayoutStep();
        }
        public CancellationTokenSource CancelTokenSource { get; set; }
        void ClearGraphViewer() {

            drawingObjectsToIViewerObjects.Clear();
            drawingObjectsToFrameworkElements.Clear();
        }


        void PopulateGeometryOfGeometryGraph() {
            geometryGraphUnderLayout = CurrentGraph.GeometryGraph;
            foreach (
                var msaglNode in
                    geometryGraphUnderLayout.Nodes) {
                var node = (DrawingNode)msaglNode.UserData;
                    msaglNode.BoundaryCurve = GetNodeBoundaryCurve(node);
            }

            foreach (
                Cluster cluster in geometryGraphUnderLayout.RootCluster.AllClustersWideFirstExcludingSelf()) {
                var subGraph = (Subgraph)cluster.UserData;
                    cluster.CollapsedBoundary = GetClusterCollapsedBoundary(subGraph);
                if (cluster.RectangularBoundary == null)
                    cluster.RectangularBoundary = new RectangularClusterBoundary();
                cluster.RectangularBoundary.TopMargin = subGraph.DiameterOfOpenCollapseButton + 0.5 +
                                                        subGraph.Attr.LineWidth / 2;
            }

            foreach (var msaglEdge in geometryGraphUnderLayout.Edges) {
                var drawingEdge = (DrawingEdge)msaglEdge.UserData;
                AssignLabelWidthHeight(msaglEdge, drawingEdge);
            }
        }

        void AssignLabelWidthHeight(Microsoft.Msagl.Core.Layout.ILabeledObject labeledGeomObj,
            DrawingObject drawingObj) {
            if (drawingObjectsToFrameworkElements.ContainsKey(drawingObj)) {
                var fe = drawingObjectsToFrameworkElements[drawingObj];
                var wh = fe.Dimensions();
                labeledGeomObj.Label.Width = wh.Item1;
                labeledGeomObj.Label.Height = wh.Item2;
            }
        }


        ICurve GetNodeBoundaryCurve(DrawingNode node) {
            double width, height;

            if (drawingObjectsToFrameworkElements.TryGetValue(node, out var fe)) {
                var wh = fe.Dimensions();
                width = wh.Item1 + 2 * node.Attr.LabelMargin;
                height = wh.Item2 + 2 * node.Attr.LabelMargin;
            }
            else
                return GetNodeBoundaryCurveByMeasuringText(node);

            if (width < CurrentGraph.Attr.MinNodeWidth)
                width = CurrentGraph.Attr.MinNodeWidth;
            if (height < CurrentGraph.Attr.MinNodeHeight)
                height = CurrentGraph.Attr.MinNodeHeight;
            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }
        ICurve GetNodeBoundaryCurveByMeasuringText(DrawingNode node) {
            double width, height;
            if (String.IsNullOrEmpty(node.LabelText)) {
                width = 10;
                height = 10;
            }
            else {
                var t1 = new SKTextBlock{ 
                    Text = node.LabelText,
                    Typeface = SKTypeface.FromFamilyName(node.Label.FontName),
                    TextSize = (float)node.Label.FontSize
                };
                var size =t1.Dimensions();
                width = size.Item1;
                height = size.Item2;
            }

            width += 2 * node.Attr.LabelMargin;
            height += 2 * node.Attr.LabelMargin;

            if (width < CurrentGraph.Attr.MinNodeWidth)
                width = CurrentGraph.Attr.MinNodeWidth;
            if (height < CurrentGraph.Attr.MinNodeHeight)
                height = CurrentGraph.Attr.MinNodeHeight;

            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }
        ICurve GetClusterCollapsedBoundary(Subgraph subgraph) {
            double width, height;

            SKFrameworkElement fe;
            if (drawingObjectsToFrameworkElements.TryGetValue(subgraph, out fe)) {
                var wh = fe.Dimensions();
                width = wh.Item1 + 2 * subgraph.Attr.LabelMargin + subgraph.DiameterOfOpenCollapseButton;
                height = Math.Max(wh.Item2 + 2 * subgraph.Attr.LabelMargin, subgraph.DiameterOfOpenCollapseButton);
            }
            else
                return GetApproximateCollapsedBoundary(subgraph);

            if (width < CurrentGraph.Attr.MinNodeWidth)
                width = CurrentGraph.Attr.MinNodeWidth;
            if (height < CurrentGraph.Attr.MinNodeHeight)
                height = CurrentGraph.Attr.MinNodeHeight;
            return NodeBoundaryCurves.GetNodeBoundaryCurve(subgraph, width, height);
        }
        SKTextBlock textBoxForApproxNodeBoundaries;

        ICurve GetApproximateCollapsedBoundary(Subgraph subgraph) {
            if (textBoxForApproxNodeBoundaries == null)
                SetUpTextBoxForApproxNodeBoundaries();


            double width, height;
            if (String.IsNullOrEmpty(subgraph.LabelText))
                height = width = subgraph.DiameterOfOpenCollapseButton;
            else {
                double a = ((double)subgraph.LabelText.Length) / textBoxForApproxNodeBoundaries.Text.Length *
                           subgraph.Label.FontSize / Label.DefaultFontSize;
                var wh = textBoxForApproxNodeBoundaries.Dimensions();
                width = wh.Item1 * a + subgraph.DiameterOfOpenCollapseButton;
                height =
                    Math.Max(
                        wh.Item2 * subgraph.Label.FontSize / Label.DefaultFontSize,
                        subgraph.DiameterOfOpenCollapseButton);
            }

            if (width < CurrentGraph.Attr.MinNodeWidth)
                width = CurrentGraph.Attr.MinNodeWidth;
            if (height < CurrentGraph.Attr.MinNodeHeight)
                height = CurrentGraph.Attr.MinNodeHeight;

            return NodeBoundaryCurves.GetNodeBoundaryCurve(subgraph, width, height);
        }

        void SetUpTextBoxForApproxNodeBoundaries() {
            textBoxForApproxNodeBoundaries = new SKTextBlock {
                Text = "Fox jumping over River",
                Typeface = SKTypeface.FromFamilyName(Label.DefaultFontName),
                IsAntialias = true,
                TextSize = Label.DefaultFontSize
            };
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

        public bool NeedToCalculateLayout { get; set; } = true;
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