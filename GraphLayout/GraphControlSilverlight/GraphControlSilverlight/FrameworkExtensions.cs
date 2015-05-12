using System;
using System.Net;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public static class FrameworkExtensions
    {
        // Performs Measure and Arrange steps, so that the element's ActualHeight and ActualWidth are properly calculated. Note that setting the alignment is required, otherwise
        // some elements will just grow indefinitely if they're left with Stretch as their alignment.
        public static void Measure(this FrameworkElement fe)
        {
            if (fe == null)
                return;
            VerticalAlignment oldV = fe.VerticalAlignment;
            HorizontalAlignment oldH = fe.HorizontalAlignment;
            fe.VerticalAlignment = VerticalAlignment.Top;
            fe.HorizontalAlignment = HorizontalAlignment.Left;
            fe.UpdateLayout();
            fe.Measure(new Size(double.MaxValue, double.MaxValue));
            fe.Arrange(new Rect(0.0, 0.0, double.MaxValue, double.MaxValue));
            fe.VerticalAlignment = oldV;
            fe.HorizontalAlignment = oldH;
        }

        public static string ToStringDebug(this GeometryGraph graph)
        {
            using (StringWriter sw = new StringWriter())
            {
                sw.WriteLine("Nodes:");
                foreach (Node n in graph.Nodes)
                    sw.WriteLine(n.BoundingBox == null ? "no bb" : n.BoundingBox.ToString());
                sw.WriteLine("Edges:");
                foreach (Edge e in graph.Edges)
                    sw.WriteLine(e.BoundingBox == null ? "no bb" : e.BoundingBox.ToString());
                return sw.ToString();
            }
        }
    }
}
