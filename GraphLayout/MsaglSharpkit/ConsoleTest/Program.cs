using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;

using MSAGLNode = Microsoft.Msagl.Core.Layout.Node;

namespace ConsoleTest
{
    class Program
    {
        static private void WriteMessage(string message)
        {
            Console.WriteLine(message);
        }

        static private void OutputCurve(ICurve curve)
        {
            for (var i = curve.ParStart; i < curve.ParEnd; i += 0.1)
            {
                WriteMessage(string.Format("Edge bounds: {0} {1}", curve[i].X, curve[i].Y));
            }
        }

        static void Main(string[] args)
        {
            MDS();

            Console.ReadKey();
        }


        public static void LayeredLayoutAbc()
        {
            double w = 30;
            double h = 20;
            GeometryGraph graph = new GeometryGraph();

            MSAGLNode a = new MSAGLNode(new Ellipse(w, h, new Point()), "a");
            MSAGLNode b = new MSAGLNode(CurveFactory.CreateRectangle(w, h, new Point()), "b");
            MSAGLNode c = new MSAGLNode(CurveFactory.CreateRectangle(w, h, new Point()), "c");

            graph.Nodes.Add(a);
            graph.Nodes.Add(b);
            graph.Nodes.Add(c);
            Edge ab = new Edge(a, b) { Length = 10 };
            Edge bc = new Edge(b, c) { Length = 3 };
            graph.Edges.Add(ab);
            graph.Edges.Add(bc);

            var settings = new SugiyamaLayoutSettings();
            LayoutHelpers.CalculateLayout(graph, settings, null);


            WriteMessage("Layout progressed");

            /*WriteMessage("");
            WriteMessage("Segments A->B");
            //OutputCurve(ab.Curve);

            WriteMessage("");
            WriteMessage("Segments B->C");
            //OutputCurve(bc.Curve);

            WriteMessage("");
            WriteMessage("Segments C->A");
            //OutputCurve(ca.Curve);

            foreach (var node in graph.Nodes)
            {
                WriteMessage(string.Format("{0}: {1} {2}", node.UserData, node.Center.X, node.Center.Y));
            }*/

            /*var canvas = HtmlContext.document.getElementById("drawing").As<HtmlCanvasElement>();
            var ctx = canvas.getContext("2d").As<CanvasRenderingContext2D>();

            var canvasHeight = canvas.height;

            var bounds = calcBounds(graph.Nodes);

            var xScale = canvas.width / bounds.Width;
            var yScale = canvas.height / bounds.Height;

            var xShift = -bounds.Left * xScale;
            var yShift = -(canvas.height - bounds.Top) * yScale;

            WriteMessage(string.Format("Scaling : {0} {1}", xScale, yScale));
            WriteMessage(string.Format("Shifting : {0} {1}", xShift, yShift));

            foreach (var msaglEdge in graph.Edges)
            {
                DrawEdge(ctx, msaglEdge, xShift, yShift, xScale, yScale, canvasHeight);
            }

            foreach (var msaglNode in graph.Nodes)
            {
                DrawNode(ctx, msaglNode, xShift, yShift, xScale, yScale, canvasHeight);
            }*/
        }

        private static GeometryGraph AbcGraph()
        {
            GeometryGraph graph = MakeTestGeometry(new[]
            {
                new EdgeSpec("a", "b"),
                new EdgeSpec("b", "c"),
            });
            return graph;
        }

        private static GeometryGraph AbcdeGraph()
        {
            GeometryGraph graph = MakeTestGeometry(new[]
            {
                new EdgeSpec("a", "b"),
                new EdgeSpec("b", "c"),
                new EdgeSpec("b", "d"),
                new EdgeSpec("d", "e"),
            });
            return graph;
        }

        public static void MDS()
        {
            WriteMessage("Starting test...");

            WriteMessage("Create GeometryGraph");

            GeometryGraph graph = AbcdeGraph();

            //graph.Save("c:\\tmp\\saved.msagl");
            var settings = new MdsLayoutSettings();
            settings.RunInParallel = false;

            settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Rectilinear;

            LayoutHelpers.CalculateLayout(graph, settings, null);


            WriteMessage("Layout progressed");

            WriteMessage("");
            WriteMessage("Segments A->B");
            //OutputCurve(ab.Curve);

            WriteMessage("");
            WriteMessage("Segments B->C");
            //OutputCurve(bc.Curve);

            WriteMessage("");
            WriteMessage("Segments C->A");
            //OutputCurve(ca.Curve);

            foreach (var node in graph.Nodes)
            {
                WriteMessage(string.Format("{0}: {1} {2}", node.UserData, node.Center.X, node.Center.Y));
            }

            var canvas = HtmlContext.document.getElementById("drawing").As<HtmlCanvasElement>();
            var ctx = canvas.getContext("2d").As<CanvasRenderingContext2D>();

            var canvasHeight = canvas.height;

            var bounds = calcBounds(graph.Nodes);

            var xScale = canvas.width / bounds.Width;
            var yScale = canvas.height / bounds.Height;

            var xShift = -bounds.Left * xScale;
            var yShift = -(canvas.height - bounds.Top) * yScale;

            WriteMessage(string.Format("Scaling : {0} {1}", xScale, yScale));
            WriteMessage(string.Format("Shifting : {0} {1}", xShift, yShift));

            foreach (var msaglEdge in graph.Edges)
            {
                DrawEdge(ctx, msaglEdge, xShift, yShift, xScale, yScale, canvasHeight);
            }

            foreach (var msaglNode in graph.Nodes)
            {
                DrawNode(ctx, msaglNode, xShift, yShift, xScale, yScale, canvasHeight);
            }*/


        }

        public class EdgeSpec
        {
            public EdgeSpec(string source, string target)
            {
                Source = source;
                Target = target;
            }
            public string Source { get; private set; }
            public string Target { get; private set; }
        }

        public static GeometryGraph MakeTestGeometry(IEnumerable<EdgeSpec> edges)
        {
            double w = 30;
            double h = 20;
            var graph = new GeometryGraph();

            var nodes = new Dictionary<string, MSAGLNode>();

            foreach (var edge in edges)
            {
                if (!nodes.ContainsKey(edge.Source))
                {
                    var node = new MSAGLNode(CurveFactory.CreateRectangle(w, h, new Point()), edge.Source);
                    graph.Nodes.Add(node);
                    nodes[edge.Source] = node;
                }
                if (!nodes.ContainsKey(edge.Target))
                {
                    var node = new MSAGLNode(CurveFactory.CreateRectangle(w, h, new Point()), edge.Target);
                    graph.Nodes.Add(node);
                    nodes[edge.Target] = node;
                }
                graph.Edges.Add(new Edge(nodes[edge.Source], nodes[edge.Target]));
            }

            return graph;
        }

    }
}
