using System;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.GraphViewerGdi;

namespace PopulateGViwerWithPrecalculatedDrawingGraph
{
    class Program
    {

        private static void SetNodeBoundary(Microsoft.Msagl.Drawing.Node drawingNode) {
            var font = new System.Drawing.Font(drawingNode.Label.FontName, (float)drawingNode.Label.FontSize);
            StringMeasure.MeasureWithFont(drawingNode.LabelText, font, out double width, out double height);
            drawingNode.Label.GeometryLabel = new Microsoft.Msagl.Core.Layout.Label();
            drawingNode.Label.GeometryLabel.Width = width;
            drawingNode.Label.GeometryLabel.Height = width;
            drawingNode.Label.Width = width;
            drawingNode.Label.Height = height;
            int r = drawingNode.Attr.LabelMargin;
            drawingNode.GeometryNode.BoundaryCurve = CurveFactory.CreateRectangleWithRoundedCorners(width + r * 2, height + r * 2, r, r, new Point());
        }
        static void Main(string[] args)
        {
            var gviewer = new GViewer();
            var form = TestFormForGViewer.FormStuff.CreateOrAttachForm(gviewer, null);
            gviewer.NeedToCalculateLayout = false;
            var drawingGraph = new Microsoft.Msagl.Drawing.Graph();
            var a = drawingGraph.AddNode("a");
            var b = drawingGraph.AddNode("b");
            drawingGraph.AddEdge("a", "b");
            var geometryGraph = drawingGraph.CreateGeometryGraph();
            SetNodeBoundary(a);
            SetNodeBoundary(b);

            // leave node "a" at (0, 0), but move "b" to a new spot
            b.GeometryNode.Center = new Point(50, 50);
            var router = new Microsoft.Msagl.Routing.SplineRouter(geometryGraph, new EdgeRoutingSettings());
            router.Run();
            geometryGraph.UpdateBoundingBox();
            gviewer.Graph = drawingGraph;
            Application.Run(form);
        }
    }
}
