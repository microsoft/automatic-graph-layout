using System.Windows;
using Microsoft.Msagl.Drawing;

namespace Microsoft.Msagl.WpfGraphControl {
    public partial class AutomaticGraphLayoutControl {
        public AutomaticGraphLayoutControl() {
            InitializeComponent();
        }

        public Graph Graph {
            get { return (Graph)GetValue(GraphProperty); }
            set { SetValue(GraphProperty, value); }
        }

        public static readonly DependencyProperty GraphProperty =
            DependencyProperty.Register("Graph", typeof(Graph), typeof(AutomaticGraphLayoutControl), new PropertyMetadata(default(Graph), 
                OnGraphChanged));

        private static void OnGraphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is AutomaticGraphLayoutControl c && e.NewValue is Graph g)
                c.SetGraph(g);
        }

        private void SetGraph(Graph graph) {
            if (graph == null) {
                dockPanel.Children.Clear();
                return;
            }
            var graphViewer = new GraphViewer();
            graphViewer.BindToPanel(dockPanel);
            graphViewer.Graph = graph;
        }
    }
}