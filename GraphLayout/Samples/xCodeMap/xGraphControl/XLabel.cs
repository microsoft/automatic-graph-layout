using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Routing;

namespace xCodeMap.xGraphControl
{
    internal class XLabel:IViewerObjectX,IInvalidatable {
        private FrameworkElement _visualObject;
        public FrameworkElement VisualObject
        {
            get { return _visualObject; }
        }

        public XLabel(Edge edge)
        {
            if(false)
            {
                TextBlock tb = null;
                if (edge.Label != null)
                {
                    tb = new TextBlock { Text = edge.Label.Text };
                    System.Windows.Size size = CommonX.Measure(tb);
                    tb.Width = size.Width;
                    tb.Height = size.Height;
                    CommonX.PositionElement(tb, edge.Label.Center, 1);
                }

                _visualObject = tb;
            }
            
            DrawingObject = edge;
        }

        public DrawingObject DrawingObject { get; private set; }
        public bool MarkedForDragging { get; set; }
        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;
        public void Invalidate(double scale = 1)
        {
            if (_visualObject != null)
            {
                double fontSize = Math.Min(12 / scale, 12);
                ((TextBlock)_visualObject).FontSize = fontSize;

                _visualObject.InvalidateVisual();
            }
        }
    }
}