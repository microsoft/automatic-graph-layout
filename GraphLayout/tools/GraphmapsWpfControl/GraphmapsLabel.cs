using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Microsoft.Msagl.Drawing;

namespace Microsoft.Msagl.GraphmapsWpfControl {
    internal class GraphmapsLabel:IViewerObject,IInvalidatable {
        internal readonly FrameworkElement FrameworkElement;
        bool markedForDragging;

        public GraphmapsLabel(Edge edge, FrameworkElement frameworkElement) {
            FrameworkElement = frameworkElement;
            DrawingObject = edge.Label;
        }
       
        public DrawingObject DrawingObject { get;  set; }

        public bool MarkedForDragging {
            get { return markedForDragging; }
            set {
                markedForDragging = value;
                if (value) {
                    AttachmentLine = new Line {
                        Stroke = System.Windows.Media.Brushes.Black,
                       StrokeDashArray = new System.Windows.Media.DoubleCollection(OffsetElems())
                    }; //the line will have 0,0, 0,0 start and end so it would not be rendered
                   
                    ((Canvas)FrameworkElement.Parent).Children.Add(AttachmentLine);
                }
                else {
                    ((Canvas) FrameworkElement.Parent).Children.Remove(AttachmentLine);
                    AttachmentLine = null;
                }
            }
        }



        IEnumerable<double> OffsetElems() {
            yield return 1;
            yield return 2;
        }

        Line AttachmentLine { get; set; }
#pragma warning disable 0067

        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;
#pragma warning restore 0067
        public void Invalidate()
        {
            var label = (Drawing.Label)DrawingObject;
            Common.PositionFrameworkElement(FrameworkElement, label.Center, 1);
            var geomLabel = label.GeometryLabel;
            if (AttachmentLine != null)
            {
                AttachmentLine.X1 = geomLabel.AttachmentSegmentStart.X;
                AttachmentLine.Y1 = geomLabel.AttachmentSegmentStart.Y;
                
                AttachmentLine.X2 = geomLabel.AttachmentSegmentEnd.X;
                AttachmentLine.Y2 = geomLabel.AttachmentSegmentEnd.Y;                
            }
        }
    }
}