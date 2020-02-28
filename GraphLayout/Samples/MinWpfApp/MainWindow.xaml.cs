using System.Windows;
using Microsoft.Msagl.Drawing;

namespace MinWpfApp {
    public partial class MainWindow {

        public MainWindow() {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {

            Graph graph = new Graph();
            //graph.AddEdge("Box", "House");
            //graph.AddEdge("House", "InvHouse");
            //graph.AddEdge("InvHouse", "Diamond");
            //graph.AddEdge("Diamond", "Octagon");
            graph.AddEdge("Octagon", "Hexagon");
            //graph.AddEdge("Hexagon", "2 Circle");
            //graph.AddEdge("2 Circle", "Box");

            //graph.FindNode("Box").Attr.Shape = Shape.Box;
            //graph.FindNode("House").Attr.Shape = Shape.House;
            //graph.FindNode("InvHouse").Attr.Shape = Shape.InvHouse;
            //graph.FindNode("Diamond").Attr.Shape = Shape.Diamond;
            graph.FindNode("Octagon").Attr.Shape = Shape.Octagon;
            graph.FindNode("Hexagon").Attr.Shape = Shape.Hexagon;
            //graph.FindNode("2 Circle").Attr.Shape = Shape.DoubleCircle;

            graph.Attr.LayerDirection = LayerDirection.LR;

            graphControl.Graph = graph;
        }
    }
}