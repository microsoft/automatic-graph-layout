using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Color = System.Windows.Media.Color;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Ellipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using Node = Microsoft.Msagl.Drawing.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Polyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
using Shape = Microsoft.Msagl.Drawing.Shape;
using Size = System.Windows.Size;
using WpfLineSegment = System.Windows.Media.LineSegment;

namespace Microsoft.Msagl.GraphmapsWpfControl {
    public class GraphmapsNode : IViewerNode, IInvalidatable {
        readonly LgLayoutSettings lgSettings;
        public Path BoundaryPath;
        internal FrameworkElement FrameworkElementOfNodeForLabel;
        readonly Func<Edge, GraphmapsEdge> funcFromDrawingEdgeToVEdge;
        internal LgNodeInfo LgNodeInfo;
        Subgraph subgraph;
        Node node;
        Border collapseButtonBorder;
        Rectangle topMarginRect;
        Path collapseSymbolPath;
        Brush collapseSymbolPathInactive = Brushes.Silver;
        

        internal int ZIndex {
            get {
                var geomNode = Node.GeometryNode;
                if (geomNode == null)
                    return 0;
                return geomNode.AllClusterAncestors.Count();
            }
        }

        public Node Node {
            get { return node; }
             set {
                node = value;
                subgraph = node as Subgraph;               
            }
        }


        internal GraphmapsNode(Node node, LgNodeInfo lgNodeInfo, FrameworkElement frameworkElementOfNodeForLabelOfLabel,
            Func<Edge, GraphmapsEdge> funcFromDrawingEdgeToVEdge, Func<double> pathStrokeThicknessFunc, LgLayoutSettings lgSettings)
        {
            this.lgSettings = lgSettings;
            PathStrokeThicknessFunc = pathStrokeThicknessFunc;
            LgNodeInfo = lgNodeInfo;
            Node = node;
            FrameworkElementOfNodeForLabel = frameworkElementOfNodeForLabelOfLabel;

            this.funcFromDrawingEdgeToVEdge = funcFromDrawingEdgeToVEdge;

            CreateNodeBoundaryPath();
            if (FrameworkElementOfNodeForLabel != null)
            {
                FrameworkElementOfNodeForLabel.Tag = this; //get a backpointer to the VNode 
                Common.PositionFrameworkElement(FrameworkElementOfNodeForLabel, node.GeometryNode.Center, 1);
                Panel.SetZIndex(FrameworkElementOfNodeForLabel, Panel.GetZIndex(BoundaryPath) + 1);
            }
            SetupSubgraphDrawing();
            Node.GeometryNode.BeforeLayoutChangeEvent += GeometryNodeBeforeLayoutChangeEvent;
            Node.Attr.VisualsChanged += (a, b) => Invalidate();

        }

        internal GraphmapsNode(Node node, LgNodeInfo lgNodeInfo, FrameworkElement frameworkElementOfNodeForLabelOfLabel,
            Func<Edge, GraphmapsEdge> funcFromDrawingEdgeToVEdge, Func<double> pathStrokeThicknessFunc)
        {
            PathStrokeThicknessFunc = pathStrokeThicknessFunc;
            LgNodeInfo = lgNodeInfo;
            Node = node;
            
            FrameworkElementOfNodeForLabel = frameworkElementOfNodeForLabelOfLabel;

            this.funcFromDrawingEdgeToVEdge = funcFromDrawingEdgeToVEdge;

            CreateNodeBoundaryPath();
            if (FrameworkElementOfNodeForLabel != null) {
                FrameworkElementOfNodeForLabel.Tag = this; //get a backpointer to the VNode 
                Common.PositionFrameworkElement(FrameworkElementOfNodeForLabel, node.GeometryNode.Center, 1);
                Panel.SetZIndex(FrameworkElementOfNodeForLabel, Panel.GetZIndex(BoundaryPath) + 1);
            }
            SetupSubgraphDrawing();
            Node.GeometryNode.BeforeLayoutChangeEvent += GeometryNodeBeforeLayoutChangeEvent;
            Node.Attr.VisualsChanged += (a, b) => Invalidate();         
           
        }

        internal IEnumerable<FrameworkElement> FrameworkElements {
            get {
                if (FrameworkElementOfNodeForLabel != null) yield return FrameworkElementOfNodeForLabel;
                if (BoundaryPath != null) yield return BoundaryPath;
                if (collapseButtonBorder != null) {
                    yield return collapseButtonBorder;
                    yield return topMarginRect;
                    yield return collapseSymbolPath;
                }
            }
        }

        void SetupSubgraphDrawing() {
            if (subgraph == null) return;

            SetupTopMarginBorder();
            SetupCollapseSymbol();
        }

        void SetupTopMarginBorder() {
            var cluster = (Cluster) subgraph.GeometryObject;
            topMarginRect = new Rectangle {
                Fill = Brushes.Transparent,
                Width = Node.Width,
                Height = cluster.RectangularBoundary.TopMargin
            };
            PositionTopMarginBorder(cluster);
            SetZIndexAndMouseInteractionsForTopMarginRect();
        }

        void PositionTopMarginBorder(Cluster cluster) {
            var box = cluster.BoundaryCurve.BoundingBox;

            Common.PositionFrameworkElement(topMarginRect,
                box.LeftTop + new Point(topMarginRect.Width/2, -topMarginRect.Height/2), 1);

            
        }

        void SetZIndexAndMouseInteractionsForTopMarginRect() {
            topMarginRect.MouseEnter +=
                (
                    (a, b) => {
                        collapseButtonBorder.Background = Common.BrushFromMsaglColor(subgraph.CollapseButtonColorActive);
                        collapseSymbolPath.Stroke = Brushes.Black;
                    }
                    );

            topMarginRect.MouseLeave +=
                (a, b) => {
                    collapseButtonBorder.Background = Common.BrushFromMsaglColor(subgraph.CollapseButtonColorInactive);
                    collapseSymbolPath.Stroke = Brushes.Silver;
                };
            Panel.SetZIndex(topMarginRect, int.MaxValue);
        }

        void SetupCollapseSymbol() {
            var collapseBorderSize = GetCollapseBorderSymbolSize();
            Debug.Assert(collapseBorderSize > 0);
            collapseButtonBorder = new Border {
                Background = Common.BrushFromMsaglColor(subgraph.CollapseButtonColorInactive),
                Width = collapseBorderSize,
                Height = collapseBorderSize,
                CornerRadius = new CornerRadius(collapseBorderSize/2)
            };

            Panel.SetZIndex(collapseButtonBorder, Panel.GetZIndex(BoundaryPath) + 1);


            var collapseButtonCenter = GetCollapseButtonCenter(collapseBorderSize);
            Common.PositionFrameworkElement(collapseButtonBorder, collapseButtonCenter, 1);

            double w = collapseBorderSize*0.4;
            collapseSymbolPath = new Path {
                Data = CreateCollapseSymbolPath(collapseButtonCenter + new Point(0, -w/2), w),
                Stroke = collapseSymbolPathInactive,
                StrokeThickness = 1
            };

            Panel.SetZIndex(collapseSymbolPath, Panel.GetZIndex(collapseButtonBorder) + 1);
            topMarginRect.MouseLeftButtonDown += TopMarginRectMouseLeftButtonDown;
        }


        /// <summary>
        /// </summary>
        public event Action<IViewerNode> IsCollapsedChanged;

        void InvokeIsCollapsedChanged()
        {
            if (IsCollapsedChanged != null)
                IsCollapsedChanged(this);
        }



        void TopMarginRectMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var pos = e.GetPosition(collapseButtonBorder);
            if (pos.X <= collapseButtonBorder.Width && pos.Y <= collapseButtonBorder.Height && pos.X >= 0 && pos.Y >= 0) {
                e.Handled = true;
                var cluster=(Cluster)subgraph.GeometryNode;
                cluster.IsCollapsed = !cluster.IsCollapsed;
                InvokeIsCollapsedChanged();
            }
        }

        double GetCollapseBorderSymbolSize() {
            return ((Cluster) subgraph.GeometryNode).RectangularBoundary.TopMargin -
                                        PathStrokeThickness/2 - 0.5;
        }

        Point GetCollapseButtonCenter(double collapseBorderSize) {
            var box = subgraph.GeometryNode.BoundaryCurve.BoundingBox;
            //cannot trust subgraph.GeometryNode.BoundingBox for a cluster
            double offsetFromBoundaryPath = PathStrokeThickness/2 + 0.5;
            var collapseButtonCenter = box.LeftTop + new Point(collapseBorderSize/2 + offsetFromBoundaryPath,
                -collapseBorderSize/2 - offsetFromBoundaryPath);
            return collapseButtonCenter;
        }

        
/*
        void FlipCollapsePath() {
            var size = GetCollapseBorderSymbolSize();
            var center = GetCollapseButtonCenter(size);

            if (collapsePathFlipped) {
                collapsePathFlipped = false;
                collapseSymbolPath.RenderTransform = null;
            }
            else {
                collapsePathFlipped = true;
                collapseSymbolPath.RenderTransform = new RotateTransform(180, center.X, center.Y);
            }
        }
*/

        Geometry CreateCollapseSymbolPath(Point center, double width) {
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure {StartPoint = Common.WpfPoint(center + new Point(-width, width))};

            pathFigure.Segments.Add(new System.Windows.Media.LineSegment(Common.WpfPoint(center), true));
            pathFigure.Segments.Add(
                new System.Windows.Media.LineSegment(Common.WpfPoint(center + new Point(width, width)), true));

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        
        void GeometryNodeBeforeLayoutChangeEvent(object sender, LayoutChangeEventArgs e) {
            var newBoundaryCurve = e.DataAfterChange as ICurve;
            if (newBoundaryCurve != null) {
                //just compare the bounding boxes for the time being
                var nb = newBoundaryCurve.BoundingBox;
                var box = Node.BoundingBox;
                if (Math.Abs(nb.Width - box.Width) > 0.00001 || Math.Abs(nb.Height - box.Height) > 0.00001)
                    BoundaryCurveIsDirty = true;
            }
            else
                BoundaryCurveIsDirty = true;
        }

        internal void CreateNodeBoundaryPath() {
            BoundaryPath = new Path {Tag = this};
            Panel.SetZIndex(BoundaryPath, ZIndex);
            SetFillAndStroke();
            if (Node.Label != null)
                BoundaryPath.ToolTip = new ToolTip {Content = new TextBlock {Text = Node.LabelText}};
        }

        internal Func<double> PathStrokeThicknessFunc;
        public double PathStrokeThickness
        {
            get
            {
                return this.Node.Attr.LineWidth;
                //return PathStrokeThicknessFunc != null ? PathStrokeThicknessFunc() : this.Node.Attr.LineWidth;
            }
        }

        
        void SetFillAndStroke()
        {
            BoundaryPath.Stroke = Common.BrushFromMsaglColor(Drawing.Color.Black);
            //jyoti changed node color
            //BoundaryPath.Stroke = Common.BrushFromMsaglColor(node.Attr.Color);

            SetBoundaryFill();

            //BoundaryPath.StrokeThickness = PathStrokeThickness;
            //jyoti changed strokethickness
            BoundaryPath.StrokeThickness = PathStrokeThickness / 2;
            if (LgNodeInfo != null && LgNodeInfo.PartiteSet == 1)                
                BoundaryPath.StrokeThickness = (PathStrokeThickness*1.5);

            

            var textBlock = FrameworkElementOfNodeForLabel as TextBlock;
            if (textBlock != null)
            {
                textBlock.Foreground = Common.BrushFromMsaglColor(Drawing.Color.Black);
                //jyoti changed node color
                //var col = Node.Label.FontColor;
                //textBlock.Foreground = Common.BrushFromMsaglColor(new Drawing.Color(col.A, col.R, col.G, col.B));
            }
           

        }


        void SetBoundaryFill() {

            //jyoti changed all node colors

                          
            BoundaryPath.Fill = Brushes.DarkGray;
            if (LgNodeInfo != null && LgNodeInfo.Selected)
                BoundaryPath.Fill = (System.Windows.Media.Brush)LgNodeInfo.Color;//Brushes.Red;
            else if (LgNodeInfo != null && LgNodeInfo.SelectedNeighbor>0)
            {
                BoundaryPath.Fill = Brushes.Yellow;                
            }  
            return;
            /*
            if (LgNodeInfo == null) {
                BoundaryPath.Fill = Brushes.Blue;
                return;
            }

            var colBlack = new Drawing.Color(0, 0, 0);

            if (!Node.Attr.Color.Equals(colBlack))
            {
                BoundaryPath.Fill = LgNodeInfo.Selected
                ? GetSelBrushColor()
                : Common.BrushFromMsaglColor(Node.Attr.Color);
                return;
            }

            BoundaryPath.Fill = LgNodeInfo.Selected
                ? GetSelBrushColor()
                : (LgNodeInfo != null && LgNodeInfo.SlidingZoomLevel == 0
                    ? Brushes.Aqua
                    : Common.BrushFromMsaglColor(Node.Attr.FillColor));

            if (LgNodeInfo != null && !LgNodeInfo.Selected) {
                BoundaryPath.Fill = (LgNodeInfo.ZoomLevel < 2
                    ? Brushes.LightGreen
                    : (LgNodeInfo.ZoomLevel < 4 ? Brushes.LightBlue : Brushes.LightYellow));
            }*/
        }

        
        public static void DrawFigure(StreamGeometryContext ctx, PathFigure figure)
        {
            ctx.BeginFigure(figure.StartPoint, figure.IsFilled, figure.IsClosed);
            foreach (var segment in figure.Segments)
            {
                var lineSegment = segment as WpfLineSegment;
                if (lineSegment != null) { ctx.LineTo(lineSegment.Point, lineSegment.IsStroked, lineSegment.IsSmoothJoin); continue; }

                var bezierSegment = segment as BezierSegment;
                if (bezierSegment != null) { ctx.BezierTo(bezierSegment.Point1, bezierSegment.Point2, bezierSegment.Point3, bezierSegment.IsStroked, bezierSegment.IsSmoothJoin); continue; }

                var quadraticSegment = segment as QuadraticBezierSegment;
                if (quadraticSegment != null) { ctx.QuadraticBezierTo(quadraticSegment.Point1, quadraticSegment.Point2, quadraticSegment.IsStroked, quadraticSegment.IsSmoothJoin); continue; }

                var polyLineSegment = segment as PolyLineSegment;
                if (polyLineSegment != null) { ctx.PolyLineTo(polyLineSegment.Points, polyLineSegment.IsStroked, polyLineSegment.IsSmoothJoin); continue; }

                var polyBezierSegment = segment as PolyBezierSegment;
                if (polyBezierSegment != null) { ctx.PolyBezierTo(polyBezierSegment.Points, polyBezierSegment.IsStroked, polyBezierSegment.IsSmoothJoin); continue; }

                var polyQuadraticSegment = segment as PolyQuadraticBezierSegment;
                if (polyQuadraticSegment != null) { ctx.PolyQuadraticBezierTo(polyQuadraticSegment.Points, polyQuadraticSegment.IsStroked, polyQuadraticSegment.IsSmoothJoin); continue; }

                var arcSegment = segment as ArcSegment;
                if (arcSegment != null) { ctx.ArcTo(arcSegment.Point, arcSegment.Size, arcSegment.RotationAngle, arcSegment.IsLargeArc, arcSegment.SweepDirection, arcSegment.IsStroked, arcSegment.IsSmoothJoin); continue; }
            }
        }

        Geometry GetNodeDotEllipseGeometry(double nodeDotWidth) {
            return new EllipseGeometry(Common.WpfPoint(Node.BoundingBox.Center), nodeDotWidth / 2,
                nodeDotWidth / 2);
        }

        StreamGeometry GetNodeDotEllipseStreamGeometry(double nodeDotWidth) {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open()) {
                var ellipse = GetNodeDotEllipseGeometry(nodeDotWidth);
                var figure = PathGeometry.CreateFromGeometry(ellipse).Figures[0];
                DrawFigure(ctx, figure);
            }
            geometry.Freeze();
            return geometry;
        }

        #region Implementation of IViewerObject

        public DrawingObject DrawingObject {
            get { return Node; }
        }

        public bool MarkedForDragging { get; set; }

#pragma warning disable 0067
        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;
#pragma warning restore 0067

        #endregion

        public IEnumerable<IViewerEdge> InEdges {
            get { foreach (var e in Node.InEdges) yield return funcFromDrawingEdgeToVEdge(e); }
        }

        public IEnumerable<IViewerEdge> OutEdges {
            get { foreach (var e in Node.OutEdges) yield return funcFromDrawingEdgeToVEdge(e); }
        }

        public IEnumerable<IViewerEdge> SelfEdges {
            get { foreach (var e in Node.SelfEdges) yield return funcFromDrawingEdgeToVEdge(e); }
        }

        
        public void InvalidateNodeDot(double nodeDotWidth)
        {
            if (!Node.IsVisible)
            {
                foreach (var fe in FrameworkElements)
                    fe.Visibility = Visibility.Hidden;
                return;
            }
            BoundaryPath.Data = GetNodeDotEllipseStreamGeometry(nodeDotWidth);
            BoundaryCurveIsDirty = false;

        }

        public void HideNodeLabel()
        {
            FrameworkElementOfNodeForLabel.Visibility = Visibility.Hidden;
        }

        public void InvalidateNodeLabel(double labelHeight, double labelWidth, Point offset)
        {
            if (LgNodeInfo == null) return;

            FrameworkElementOfNodeForLabel.Height = labelHeight;
            FrameworkElementOfNodeForLabel.Width = labelWidth;

            Common.PositionFrameworkElement(FrameworkElementOfNodeForLabel, Node.BoundingBox.Center+offset, 1);

            if (Node.IsVisible) {
                FrameworkElementOfNodeForLabel.Visibility = Visibility.Visible;
            }            
        }

        public void Invalidate() {
            SetFillAndStroke();
        }

        public override string ToString() {
            return Node.Id;
        }

        protected bool BoundaryCurveIsDirty { get; set; }


        internal void DetouchFromCanvas(Canvas graphCanvas) {
            if (BoundaryPath != null)
                graphCanvas.Children.Remove(BoundaryPath);
            if (FrameworkElementOfNodeForLabel != null)
                graphCanvas.Children.Remove(FrameworkElementOfNodeForLabel);
        }


        byte Low(byte b)
        {
            return (byte)(b/3);
        }
        
        private Brush GetSelBrushColor()
        {
            if (lgSettings != null)
            {
                var col = lgSettings.GetNodeSelColor();
                var brush = (SolidColorBrush)(new BrushConverter().ConvertFrom(col));
                return brush;
            }
            else
            {
                return Brushes.Red;
            }
        }
        internal void SetLowTransparency()
        {
            if (BoundaryPath != null) {
                var col = Node.Attr.Color;
                BoundaryPath.Stroke =
                    Common.BrushFromMsaglColor(new Drawing.Color(Low(col.A), Low(col.R), Low(col.G), Low(col.B)));
                var fill = BoundaryPath.Fill as SolidColorBrush;
                if (fill != null)
                    BoundaryPath.Fill =
                        new SolidColorBrush(Color.FromArgb(200, fill.Color.R, fill.Color.G, fill.Color.B));
            }
            var textBlock = FrameworkElementOfNodeForLabel as TextBlock;
            if (textBlock != null)
            {
                var col = Node.Label.FontColor;
                textBlock.Foreground = Common.BrushFromMsaglColor(new Drawing.Color(Low(col.A), Low(col.R), Low(col.G), Low(col.B)));
            }
           
        }

    }
}