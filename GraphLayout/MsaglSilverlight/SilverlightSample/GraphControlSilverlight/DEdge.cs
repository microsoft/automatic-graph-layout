using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Msagl.Drawing;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using GeometryEdge = Microsoft.Msagl.Core.Layout.Edge;
using MsaglPoint = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    /// <summary>
    /// This class represents a rendering edge. It contains the Microsoft.Msagl.Drawing.Edge and Microsoft.Msagl.Edge.
    /// The rendering edge is a XAML user control. Its template is stored in Themes/generic.xaml.
    /// </summary>
    public sealed class DEdge : DObject, IViewerEdge, IHavingDLabel, IEditableObject
    {
        /// <summary>
        /// the corresponding drawing edge
        /// </summary>
        public DrawingEdge DrawingEdge { get; set; }

        public GeometryEdge GeometryEdge { get { return this.DrawingEdge.GeometryObject as GeometryEdge; } set { this.DrawingEdge.GeometryEdge = value; } }

        internal DEdge(DNode source, DNode target, DrawingEdge drawingEdgeParam, ConnectionToGraph connection)
            : base(source.ParentObject)
        {
            this.DrawingEdge = drawingEdgeParam;
            this.Source = source;
            this.Target = target;

            if (connection == ConnectionToGraph.Connected)
            {
                if (source == target)
                    source.AddSelfEdge(this);
                else
                {
                    source.AddOutEdge(this);
                    target.AddInEdge(this);
                }
            }

            if (drawingEdgeParam.Label != null)
                this.Label = new DTextLabel(this, DrawingEdge.Label);
        }

        public DNode Source { get; set; }

        public DNode Target { get; set; }

        public override void MakeVisual()
        {
            if (DrawingEdge.GeometryEdge.Curve == null)
                return;
            SetValue(Canvas.LeftProperty, Edge.BoundingBox.Left);
            SetValue(Canvas.TopProperty, Edge.BoundingBox.Bottom);

            if (StrokeDashArray == null)
            {
                // Note that Draw.CreateGraphicsPath returns a curve with coordinates in graph space.
                var pathFigure = Draw.CreateGraphicsPath((DrawingEdge.GeometryObject as GeometryEdge).Curve);
                var pathGeometry = new PathGeometry();
                pathGeometry.Figures.Add(pathFigure);
                Draw.DrawEdgeArrows(pathGeometry, DrawingEdge, FillArrowheadAtSource, FillArrowheadAtTarget);
                // Apply a translation to bring the coordinates in node space (i.e. top left corner is 0,0).
                pathGeometry.Transform = new MatrixTransform() { Matrix = new Matrix(1.0, 0.0, 0.0, 1.0, -Edge.BoundingBox.Left, -Edge.BoundingBox.Bottom) };

                if (SelectedForEditing && GeometryEdge.UnderlyingPolyline != null)
                    Draw.DrawUnderlyingPolyline(pathGeometry, this);
                var path = new Path() { Data = pathGeometry, StrokeThickness = Math.Max(LineWidth, Edge.Attr.LineWidth), Stroke = EdgeBrush, Fill = EdgeBrush };
                Content = path;
            }
            else
            {
                // A dash array has been specified. I don't want to apply it to both the edge and the arrowheads; for this reason, I'm going to split the drawing in two Path instances.
                // This is not done in the general case to keep the number of Paths down.
                var pathFigure = Draw.CreateGraphicsPath((DrawingEdge.GeometryObject as GeometryEdge).Curve);
                var pathGeometry = new PathGeometry();
                pathGeometry.Figures.Add(pathFigure);
                pathGeometry.Transform = new MatrixTransform() { Matrix = new Matrix(1.0, 0.0, 0.0, 1.0, -Edge.BoundingBox.Left, -Edge.BoundingBox.Bottom) };

                if (SelectedForEditing && GeometryEdge.UnderlyingPolyline != null)
                    Draw.DrawUnderlyingPolyline(pathGeometry, this);
                DoubleCollection dc = new DoubleCollection();
                foreach (double d in StrokeDashArray)
                    dc.Add(d);
                var path1 = new Path() { Data = pathGeometry, StrokeThickness = Math.Max(LineWidth, Edge.Attr.LineWidth), Stroke = EdgeBrush, Fill = EdgeBrush, StrokeDashArray = dc };
                
                pathGeometry = new PathGeometry();
                pathGeometry.Transform = new MatrixTransform() { Matrix = new Matrix(1.0, 0.0, 0.0, 1.0, -Edge.BoundingBox.Left, -Edge.BoundingBox.Bottom) };
                Draw.DrawEdgeArrows(pathGeometry, DrawingEdge, FillArrowheadAtSource, FillArrowheadAtTarget);
                var path2 = new Path() { Data = pathGeometry, StrokeThickness = Math.Max(LineWidth, Edge.Attr.LineWidth), Stroke = EdgeBrush, Fill = EdgeBrush };

                var c = new Grid();
                c.Children.Add(path1);
                c.Children.Add(path2);
                Content = c;
            }
        }

        public DoubleCollection StrokeDashArray { get; set; }

        // Workaround for MSAGL bug which causes Edge.Attr.LineWidth to be ignored
        public int LineWidth { get; set; }

        /// <summary>
        /// Gets or sets the edge brush.
        /// </summary>
        public Brush EdgeBrush
        {
            get { return (Brush)GetValue(EdgeBrushProperty); }
            set { SetValue(EdgeBrushProperty, value); }
        }

        public static readonly DependencyProperty EdgeBrushProperty =
            DependencyProperty.Register("EdgeBrush",
            typeof(Brush),
            typeof(DEdge),
            new PropertyMetadata(DGraph.BlackBrush));

        public ArrowStyle ArrowheadAtSource
        {
            get
            {
                return DrawingEdge.Attr.ArrowheadAtSource;
            }
            set
            {
                DrawingEdge.Attr.ArrowheadAtSource = value;
                if (value == ArrowStyle.None)
                    GeometryEdge.EdgeGeometry.SourceArrowhead = null;
                else
                    GeometryEdge.EdgeGeometry.SourceArrowhead = new Core.Layout.Arrowhead();
                //DrawingEdge.GeometryEdge.ArrowheadAtSource = value != ArrowStyle.None;
            }
        }

        public bool FillArrowheadAtSource { get; set; }

        public ArrowStyle ArrowheadAtTarget
        {
            get
            {
                return DrawingEdge.Attr.ArrowheadAtTarget;
            }
            set
            {
                DrawingEdge.Attr.ArrowheadAtTarget = value;
                if (value == ArrowStyle.None)
                    GeometryEdge.EdgeGeometry.TargetArrowhead = null;
                else
                    GeometryEdge.EdgeGeometry.TargetArrowhead = new Core.Layout.Arrowhead();
                //DrawingEdge.GeometryEdge.ArrowheadAtTarget = value != ArrowStyle.None;
            }
        }

        public bool FillArrowheadAtTarget { get; set; }

        #region IDraggableEdge Members

        /// <summary>
        /// underlying Drawing edge
        /// </summary>
        public DrawingEdge Edge
        {
            get { return this.DrawingEdge; }
        }

        IViewerNode IViewerEdge.Source
        {
            get { return Source; }
        }

        IViewerNode IViewerEdge.Target
        {
            get { return Target; }
        }

        private DLabel _Label;
        /// <summary>
        /// Gets or sets the rendering label.
        /// </summary>
        public DLabel Label
        {
            get
            {
                return _Label;
            }
            set
            {
                _Label = value;
                // Also set the drawing label and geometry label.
                DrawingEdge.Label = value.DrawingLabel;
                DrawingEdge.GeometryEdge.Label = value.DrawingLabel.GeometryLabel;
            }
        }

        /// <summary>
        /// The underlying DrawingEdge
        /// </summary>
        override public DrawingObject DrawingObject
        {
            get { return this.DrawingEdge; }
        }

        /// <summary>
        ///the radius of circles drawin around polyline corners 
        /// </summary>
        public double RadiusOfPolylineCorner { get; set; }

        #endregion

        #region IEditableObject Members

        /// <summary>
        /// is set to true then the edge should set up for editing
        /// </summary>
        public bool SelectedForEditing { get; set; }

        #endregion
    }
}