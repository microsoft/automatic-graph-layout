using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.WpfGraphControl;
using Microsoft.Msagl.Core.DataStructures;
using Color = Microsoft.Msagl.Drawing.Color;
using Onnx;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace WpfApplicationSample_OnnxViewer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        DockPanel graphViewerPanel = new DockPanel();
        GraphViewer graphViewer = new GraphViewer();
        int downX;
        int downY;
        PlaneTransformation downTransform;

        public MainWindow() {
            InitializeComponent();

            mainGrid.Children.Add(graphViewerPanel);
            graphViewerPanel.Focusable = true;
            graphViewerPanel.KeyDown += Graph_KeyDown;
            graphViewerPanel.Focus();
            graphViewer.BindToPanel(graphViewerPanel);
            graphViewer.MouseDown += MouseDown;
            graphViewer.MouseMove += MouseMove;
        }

        void Grid_DragOver(object sender, DragEventArgs e) {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.All : DragDropEffects.None;
            e.Handled = true;
        }

        void Grid_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                LoadOnnxFile(fileNames[0]);
            }
        }

        private void Graph_KeyDown(object sender, KeyEventArgs e) {
            double dx = 0;
            double dy = 0;
            if (Keyboard.IsKeyDown(Key.W)) dy += 10;
            if (Keyboard.IsKeyDown(Key.A)) dx -= 10;
            if (Keyboard.IsKeyDown(Key.S)) dy -= 10;
            if (Keyboard.IsKeyDown(Key.D)) dx += 10;
            var scale = graphViewer.CurrentScale;
            dx /= scale;
            dy /= scale;
            var translate = new PlaneTransformation(1, 0, dx, 0, 1, dy);
            graphViewer.Transform *= translate;
        }

        void MouseDown(object sender, MsaglMouseEventArgs e) {
            this.downX = e.X;
            this.downY = e.Y;
            this.downTransform = graphViewer.Transform;
            graphViewer.GraphCanvas.CaptureMouse();
        }

        void MouseMove(object sender, MsaglMouseEventArgs e) {
            if (!e.LeftButtonIsPressed || this.downTransform == null)
                return;
            var scale = graphViewer.CurrentScale;
            var dx = (e.X - this.downX) / scale;
            var dy = (this.downY - e.Y) / scale;
            var translate = new PlaneTransformation(1, 0, dx, 0, 1, dy);
            graphViewer.Transform = this.downTransform * translate;
        }

        void LoadOnnxFile(string filePath) {
            try
            {
                var model = ModelProto.Parser.ParseFrom(File.ReadAllBytes(filePath));
                var modelGraph = model.Graph;
                Graph graph = new Graph("graph");
                graph.LayerConstraints.RemoveAllConstraints();
                SugiyamaLayoutSettings layoutSettings = (SugiyamaLayoutSettings)graph.LayoutAlgorithmSettings;
                var inputs = new Dictionary<string, List<string>>();
                var outputs = new Dictionary<string, List<string>>();
                var nodes = new Set<string>();
                var initializers = new Set<string>();
                foreach(var initializer in modelGraph.Initializer) {
                    initializers.Insert(initializer.Name);
                }
                var limit = 3000;
                var cnt = 0;
                foreach(var node in modelGraph.Node) {
                    var gNode = new Node(node.Name);
                    gNode.LabelText = node.OpType.ToString();
                    graph.AddNode(gNode);
                    nodes.Insert(node.Name);
                    foreach(var input in node.Input) {
                        if(!inputs.ContainsKey(input)) {
                            inputs.Add(input, new List<string>());
                        }
                        inputs[input].Add(node.Name);
                    }
                    foreach(var output in node.Output) {
                        if(!outputs.ContainsKey(output)) {
                            outputs.Add(output, new List<string>());
                        }
                        outputs[output].Add(node.Name);
                    }
                    if(++cnt > limit)
                        break;
                }
                cnt = 0;
                foreach(var node in modelGraph.Node) {
                    foreach(var output in node.Output) {
                        if(!inputs.ContainsKey(output)) {
                            if(nodes.Contains(output)) {
                                graph.AddNode(output);
                                graph.AddEdge(node.Name, output);
                            }
                        }
                        else {
                            var inputs2 = inputs[output];
                            inputs2.Reverse();
                            foreach(var input in inputs2) {
                                if(nodes.Contains(input)) {
                                    graph.AddEdge(node.Name, input);
                                }
                            }
                        }
                    }
                    if(++cnt > limit)
                        break;
                }
                graph.Attr.BackgroundColor = Color.Transparent;
                graphViewer.Graph = graph;
                this.Title = filePath + " - WpfApplicationSample OnnxViewer";
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
