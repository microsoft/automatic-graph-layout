using System;

using CommonDrawingUtilsForSamples;

using Gtk;

using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;

using UI = Gtk.Builder.ObjectAttribute;

namespace DrawingFromGeometryWithGtk
{
    class MainWindow : Window
    {
        [UI] private DrawingArea drawingArea = null;
        private GeometryGraph graph;
        private double width, height;

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            graph = CreateAndLayoutGraph();

            drawingArea.Drawn += Graph_Drawn;
            drawingArea.SizeAllocated += Graph_SizeAllocated;
            DeleteEvent += Window_DeleteEvent;
        }

        private void Graph_SizeAllocated(object sender, SizeAllocatedArgs args) {
            width = args.Allocation.Width;
            height = args.Allocation.Width;
            RecalculateLayout();
            drawingArea.QueueResize();
        }

        private void Graph_Drawn(object sender, DrawnArgs args) {
            var context = args.Cr;
            DrawingUtilsForSamples.DrawFromGraph(new Cairo.Rectangle(0, 0, width, height), graph, args.Cr);
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        static internal GeometryGraph CreateAndLayoutGraph() {
            GeometryGraph graph = new GeometryGraph();

            double width = 40;
            double height = 10;

            foreach (string id in "0 1 2 3 4 5 6 A B C D E F G a b c d e".Split(' '))
            {
                DrawingUtilsForSamples.AddNode(id, graph, width, height);
            }

            graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("B")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("C")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("D")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("E")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("B"), graph.FindNodeByUserData("E")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("0"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("1"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("2"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("3"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("4"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("5"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("6"), graph.FindNodeByUserData("F")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("a"), graph.FindNodeByUserData("b")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("b"), graph.FindNodeByUserData("c")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("c"), graph.FindNodeByUserData("d")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("d"), graph.FindNodeByUserData("e")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("B"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("C"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("E"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("F"), graph.FindNodeByUserData("a")));
            graph.Edges.Add(new Edge(graph.FindNodeByUserData("G"), graph.FindNodeByUserData("a")));

            var settings = new SugiyamaLayoutSettings {
                Transformation = PlaneTransformation.Rotation(Math.PI/2),
                EdgeRoutingSettings = {EdgeRoutingMode = EdgeRoutingMode.Spline}
            };
            var layout = new LayeredLayout(graph, settings);
            layout.Run();
            return graph;
        }

        internal void RecalculateLayout() {
            var settings = new SugiyamaLayoutSettings {
                Transformation = PlaneTransformation.Rotation(Math.PI / 2),
                EdgeRoutingSettings = {EdgeRoutingMode = EdgeRoutingMode.Spline}
            };
            var layout = new LayeredLayout(graph, settings);
            layout.Run();
        }
    }
}
