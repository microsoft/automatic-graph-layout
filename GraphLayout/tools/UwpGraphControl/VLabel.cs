using System;
using Microsoft.Msagl.Drawing;
using SkiaSharp;
using UwpGraphControl;
using Colors = Windows.UI.Colors;
using FrameworkElement = Windows.UI.Xaml.FrameworkElement;
using Canvas = Windows.UI.Xaml.Controls.Canvas;
using SolidColorBrush = Windows.UI.Xaml.Media.SolidColorBrush;
using Line = Windows.UI.Xaml.Shapes.Line;

namespace Microsoft.Msagl.Viewers.Uwp {
    public class VLabel : IViewerObject {
        internal readonly SKFrameworkElement FrameworkElement;
        bool markedForDragging;

        public VLabel(Edge edge, SKFrameworkElement frameworkElement) {
            FrameworkElement = frameworkElement;
            DrawingObject = edge.Label;
        }

        public DrawingObject DrawingObject { get; private set; }

        public bool MarkedForDragging {
            get { return markedForDragging; }
            set {
                //markedForDragging = value;
                //if (value) {
                //    AttachmentLine = new SKLine {
                //        Stroke = new SolidColorBrush(Colors.Black),
                //        StrokeDashArray = { 1, 2 }
                //    }; //the line will have 0,0, 0,0 start and end so it would not be rendered

                //    (FrameworkElement.Parent.Children.Add(AttachmentLine);
                //}
                //else {
                //    ((Canvas)FrameworkElement.Parent).Children.Remove(AttachmentLine);
                //    AttachmentLine = null;
                //}
            }
        }

        Line AttachmentLine { get; set; }

        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;
        public void Invalidate() {
            var label = (Drawing.Label)DrawingObject;
            Common.PositionFrameworkElement(FrameworkElement, label.Center, 1);
            var geomLabel = label.GeometryLabel;
            if (AttachmentLine != null) {
                AttachmentLine.X1 = geomLabel.AttachmentSegmentStart.X;
                AttachmentLine.Y1 = geomLabel.AttachmentSegmentStart.Y;

                AttachmentLine.X2 = geomLabel.AttachmentSegmentEnd.X;
                AttachmentLine.Y2 = geomLabel.AttachmentSegmentEnd.Y;
            }
        }
    }
}