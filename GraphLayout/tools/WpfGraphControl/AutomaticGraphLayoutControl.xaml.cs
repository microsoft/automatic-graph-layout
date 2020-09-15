using System.Windows;
using Microsoft.Msagl.Drawing;

namespace Microsoft.Msagl.WpfGraphControl {
    public partial class AutomaticGraphLayoutControl {
        public AutomaticGraphLayoutControl() {
            InitializeComponent();
            Loaded += (s, e) => SetGraph();
        }
        public Graph Graph {
            get => (Graph)GetValue(GraphProperty);
            set => SetValue(GraphProperty, value);
        }
        public static readonly DependencyProperty GraphProperty =
            DependencyProperty.Register("Graph", typeof(Graph), typeof(AutomaticGraphLayoutControl), new PropertyMetadata(default(Graph),
                (d,e)=> ((AutomaticGraphLayoutControl)d)?.SetGraph()));
     
        private void SetGraph() {
            if (Graph == null) {
                dockPanel.Children.Clear();
                return;
            }
            var graphViewer = new GraphViewer();
            graphViewer.BindToPanel(dockPanel);
            graphViewer.Graph = Graph;
        }
    }
}