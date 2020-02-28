using System.Windows;
using System.Windows.Controls;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.WpfGraphControl;

namespace MinWpfApp {
        public partial class MainWindow {
         public MainWindow() {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
             var graph = new Graph();

            var edge = graph.AddEdge("A", "B");
            graph.Attr.LayerDirection = LayerDirection.LR;
            graphControl.Graph = graph; 
        }
    }
}