
using Microsoft.Msagl.Drawing;

namespace MinUwpApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        public MainPage()
        {
            Graph graph = new Graph();
            graph.AddEdge("Octagon", "Hexagon");
            graph.FindNode("Octagon").Attr.Shape = Shape.Octagon;
            graph.FindNode("Hexagon").Attr.Shape = Shape.Hexagon;

            graph.Attr.LayerDirection = LayerDirection.LR;

            Graph1 = graph;
            InitializeComponent();
        }

        public Graph Graph1 { get; set; }
    }
}
