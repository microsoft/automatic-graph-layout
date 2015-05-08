using System;
using System.IO;
using System.Reflection;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Net;
using System.Windows.Markup;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Layout.Incremental;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public partial class GraphControl : UserControl, INotifyPropertyChanged
    {
        private static bool UseComboInsertion = false;

        public GraphControl()
        {
            InitializeComponent();

            LayoutRoot.DataContext = this;

            EdgeRoutingComboBox.SelectionChanged += (sender, args) => DoLayout_Click(null, null);
            Loaded += new RoutedEventHandler(GraphControl_Loaded);

            if (UseComboInsertion)
            {
                InsertionButton.Content = "Insert";
                InsertionButton.Tag = "ComboInsertion";
            }
        }

        public EdgeRoutingMode EdgeRoutingMode
        {
            get { return (EdgeRoutingMode)typeof(EdgeRoutingMode).GetField((EdgeRoutingComboBox.SelectedItem as ComboBoxItem).Tag.ToString()).GetValue(null); }
            set
            {
                int c = 0;
                foreach (string tag in EdgeRoutingComboBox.Items.Cast<ComboBoxItem>().Select(cbi => cbi.Tag.ToString()))
                    if (tag == value.ToString())
                        EdgeRoutingComboBox.SelectedIndex = c;
                    else
                        c++;
            }
        }

        private void GraphControl_Loaded(object sender, RoutedEventArgs e)
        {
            NodeTypeComboBox.SelectionChanged += (sender2, args) => { InsertionButton.IsChecked = true; Graph.MouseMode = DraggingMode.ComboInsertion; };
        }

        public bool RouteEdgesAfterDragging
        {
            get
            {
                return Graph == null ? false : Graph.RouteEdgesAfterDragging;
            }
            set
            {
                if (Graph != null)
                    Graph.RouteEdgesAfterDragging = value;
            }
        }

        public static DependencyProperty ShowExperimentalControlsProperty = DependencyProperty.Register("ShowExperimentalControls", typeof(bool), typeof(GraphControl), new PropertyMetadata(false));
        public bool ShowExperimentalControls
        {
            get { return (bool)GetValue(ShowExperimentalControlsProperty); }
            set { SetValue(ShowExperimentalControlsProperty, value); }
        }

        public static DependencyProperty ShowDebugInformationProperty = DependencyProperty.Register("ShowDebugInformation", typeof(bool), typeof(GraphControl), new PropertyMetadata(false));
        public bool ShowDebugInformation
        {
            get { return (bool)GetValue(ShowDebugInformationProperty); }
            set { SetValue(ShowDebugInformationProperty, value); }
        }

        public static DependencyProperty AllowSaveLoadProperty = DependencyProperty.Register("AllowSaveLoad", typeof(bool), typeof(GraphControl), new PropertyMetadata(true));
        public bool AllowSaveLoad
        {
            get { return (bool)GetValue(AllowSaveLoadProperty); }
            set { SetValue(AllowSaveLoadProperty, value); }
        }

        public static DependencyProperty AllowLayoutEditingProperty = DependencyProperty.Register("AllowLayoutEditing", typeof(bool), typeof(GraphControl), new PropertyMetadata(true, (sender, args) => (sender as GraphControl).OnAllowLayoutEditingChanged()));
        private void OnAllowLayoutEditingChanged()
        {
            Graph.MouseMode = Graph.DefaultMouseMode;
            PanButton.IsChecked = true;
            if (!AllowLayoutEditing)
                AllowGraphEditing = false;
        }
        public bool AllowLayoutEditing
        {
            get { return (bool)GetValue(AllowLayoutEditingProperty); }
            set { SetValue(AllowLayoutEditingProperty, value); }
        }

        public static DependencyProperty AllowLabelEditingProperty = DependencyProperty.Register("AllowLabelEditing", typeof(bool), typeof(GraphControl), new PropertyMetadata(true, (sender, args) => (sender as GraphControl).OnAllowLabelEditingChanged()));
        private void OnAllowLabelEditingChanged()
        {
            Graph.MouseMode = Graph.DefaultMouseMode;
        }
        public bool AllowLabelEditing
        {
            get { return (bool)GetValue(AllowLabelEditingProperty); }
            set { SetValue(AllowLabelEditingProperty, value); }
        }

        public static DependencyProperty AllowGraphEditingProperty = DependencyProperty.Register("AllowGraphEditing", typeof(bool), typeof(GraphControl), new PropertyMetadata(true, (sender, args) => (sender as GraphControl).OnAllowGraphEditingChanged()));
        private void OnAllowGraphEditingChanged()
        {
            Graph.MouseMode = Graph.DefaultMouseMode;
            if (UseComboInsertion)
                NodeTypeComboBox.Visibility = (AllowGraphEditing && _NodeTypes.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
            if (AllowGraphEditing)
                AllowLayoutEditing = true;
        }
        public bool AllowGraphEditing
        {
            get { return (bool)GetValue(AllowGraphEditingProperty); }
            set { SetValue(AllowGraphEditingProperty, value); }
        }

        public static DependencyProperty ShowBorderProperty = DependencyProperty.Register("ShowBorder", typeof(bool), typeof(GraphControl), new PropertyMetadata(false, (sender, args) => (sender as GraphControl).OnShowBorderChanged()));
        private void OnShowBorderChanged()
        {
            Graph.ShowBorder = ShowBorder;
        }
        public bool ShowBorder
        {
            get { return (bool)GetValue(ShowBorderProperty); }
            set { SetValue(ShowBorderProperty, value); }
        }

        public static DependencyProperty LayeredLayoutProperty = DependencyProperty.Register("LayeredLayout", typeof(bool), typeof(GraphControl), new PropertyMetadata(true));
        public bool LayeredLayout
        {
            get { return (bool)GetValue(LayeredLayoutProperty); }
            set { SetValue(LayeredLayoutProperty, value); }
        }

        private DGraph _Graph;
        public DGraph Graph
        {
            get
            {
                if (_Graph == null)
                {
                    Graph = new DGraph();
                    Graph.Graph.GeometryGraph.UpdateBoundingBox();
                    Graph.Invalidate();
                }
                return _Graph;
            }
            set
            {
                if (value != _Graph)
                {
                    if (_Graph != null)
                    {
                        GraphContainer.Children.Clear();
                    }
                    if (value != null)
                    {
                        value.GraphLayoutStarting += Graph_GraphLayoutStarting;
                        value.GraphLayoutDone += (sender, args) => value.FitToContents();
                        value.NodeInsertingByUser += Graph_NodeInsertingByUser;
                        value.GeneratingPopup += Graph_GeneratingPopup;
                        GraphContainer.Children.Add(value);

                        Binding b = new Binding("Zoom") { Mode = BindingMode.TwoWay, Converter = Resources["ZoomConverter"] as ZoomConverter, Source = value };
                        ZoomPerc.SetBinding(TextBox.TextProperty, b);
                        ZoomSlider.SetBinding(Slider.ValueProperty, b);

                        value.PropertyChanged += Graph_PropertyChanged;
                    }
                    _Graph = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("Graph"));
                }
            }
        }

        private void Graph_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Working")
            {
                DoLayoutButton.Visibility = Graph.Working ? Visibility.Collapsed : Visibility.Visible;
                AbortLayoutButton.Visibility = Graph.Working ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (e.PropertyName == "CanUndo")
                UndoButton.IsEnabled = Graph.CanUndo;
            else if (e.PropertyName == "CanRedo")
                RedoButton.IsEnabled = Graph.CanRedo;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void DraggingMode_Click(object sender, RoutedEventArgs e)
        {
            // Search for an enum value corresponding to the Tag text.
            string t = (sender as RadioButton).Tag as string;
            FieldInfo v = typeof(DraggingMode).GetFields().FirstOrDefault(fi => fi.Name == t);
            Graph.MouseMode = v == null ? Graph.DefaultMouseMode : (DraggingMode)v.GetRawConstantValue();
        }

        public bool AllowDeletingElements { get; set; }

        public event EventHandler<CancelEventArgs> DoLayoutClicked;

        private void DoLayout_Click(object sender, RoutedEventArgs e)
        {
            if (DoLayoutClicked != null)
            {
                CancelEventArgs a = new CancelEventArgs(false);
                DoLayoutClicked(this, a);
                if (a.Cancel)
                    return;
            }
            BeginLayoutWithConstraints();
        }

        private void AbortLayout_Click(object sender, RoutedEventArgs e)
        {
            Graph.AbortLayout();
        }

        private void FitToContent_Click(object sender, RoutedEventArgs e)
        {
            Graph.FitGraphBoundingBox();
            Graph.FitToContents();
        }

        public static readonly DependencyProperty InitialNodesProperty = DependencyProperty.Register("InitialNodes", typeof(IEnumerable<DNode>), typeof(GraphControl), null);
        public IEnumerable<DNode> InitialNodes
        {
            get { return (IEnumerable<DNode>)GetValue(InitialNodesProperty); }
            set { SetValue(InitialNodesProperty, value); }
        }

        public static readonly DependencyProperty GroupInitialNodesProperty = DependencyProperty.Register("GroupInitialNodes", typeof(bool), typeof(GraphControl), new PropertyMetadata((sender, args) => (sender as GraphControl).LayoutOptionChanged()));
        public bool GroupInitialNodes
        {
            get { return (bool)GetValue(GroupInitialNodesProperty); }
            set { SetValue(GroupInitialNodesProperty, value); }
        }

        public static readonly DependencyProperty EnforceAspectRatioProperty = DependencyProperty.Register("EnforceAspectRatio", typeof(bool), typeof(GraphControl), new PropertyMetadata((sender, args) => (sender as GraphControl).LayoutOptionChanged()));
        public bool EnforceAspectRatio
        {
            get { return (bool)GetValue(EnforceAspectRatioProperty); }
            set { SetValue(EnforceAspectRatioProperty, value); }
        }

        public static readonly DependencyProperty HorizontalLayoutProperty = DependencyProperty.Register("HorizontalLayout", typeof(bool), typeof(GraphControl), new PropertyMetadata((sender, args) => (sender as GraphControl).LayoutOptionChanged()));
        public bool HorizontalLayout
        {
            get { return (bool)GetValue(HorizontalLayoutProperty); }
            set { SetValue(HorizontalLayoutProperty, value); }
        }

        private void LayoutOptionChanged()
        {
            if (Graph.Nodes().Any(n => !(n is DCluster)))
                DoLayout_Click(null, null);
        }

        private void Graph_GraphLayoutStarting(object sender, EventArgs e)
        {
            // I'd like to just call RemoveAllConstraints here, but it doesn't work.
            if (LayeredLayout)
            {
                var settings = Graph.ConfigureSugiyamaLayout(HorizontalLayout ? Math.PI / 2.0 : Math.PI);

                if (GroupInitialNodes && InitialNodes != null)
                {
                    // Declare all initial nodes to be above every other node.
                    foreach (DNode nc in InitialNodes)
                        foreach (DNode nc2 in Graph.Nodes().Where(ec => !(ec is DCluster) && !InitialNodes.Contains(ec as DNode)))
                            settings.AddUpDownConstraint(nc.GeometryNode, nc2.GeometryNode);
                }

                if (EnforceAspectRatio && GraphContainer.ActualHeight != 0.0 && GraphContainer.ActualWidth != 0.0)
                    settings.AspectRatio = HorizontalLayout ? GraphContainer.ActualHeight / GraphContainer.ActualWidth : GraphContainer.ActualWidth / GraphContainer.ActualHeight;

                if (HorizontalLayout)
                    settings.Transformation = PlaneTransformation.Rotation(Math.PI / 2);
                else
                    settings.Transformation = PlaneTransformation.Rotation(Math.PI);

                settings.EdgeRoutingSettings.EdgeRoutingMode = (EdgeRoutingMode)typeof(EdgeRoutingMode).GetField((EdgeRoutingComboBox.SelectedItem as ComboBoxItem).Tag.ToString()).GetValue(null);
                int separation = 5;
                Int32.TryParse(BundlingSeparationTextBox.Text, out separation);
                settings.EdgeRoutingSettings.BundlingSettings = new BundlingSettings() { EdgeSeparation = separation };

                Graph.Graph.LayoutAlgorithmSettings = settings;
            }
            else
            {
                Graph.ConfigureIncrementalLayout();
            }
        }

        public void BeginLayoutWithConstraints()
        {
            Graph.BeginLayout();
        }

        private void RouteEdges_Click(object sender, RoutedEventArgs e)
        {
            int separation = 5;
            Int32.TryParse(BundlingSeparationTextBox.Text, out separation);
            Graph.EdgeRoutingMode = (EdgeRoutingMode)typeof(EdgeRoutingMode).GetField((EdgeRoutingComboBox.SelectedItem as ComboBoxItem).Tag.ToString()).GetValue(null);
            Graph.RouteSelectedEdges(separation);
        }

        private List<NodeTypeEntry> _NodeTypes = new List<NodeTypeEntry>();
        public IEnumerable<NodeTypeEntry> NodeTypes { get { return _NodeTypes; } }

        public void AddNodeType(NodeTypeEntry nte)
        {
            _NodeTypes.Add(nte);
            NodeTypeComboBox.ItemsSource = NodeTypes.Select(x => x); // If I use NodeTypes directly, it crashes. No idea why. Looks like a bug in Silverlight.
            NodeTypeComboBox.SelectedIndex = 0;
            if (AllowGraphEditing && UseComboInsertion)
                NodeTypeComboBox.Visibility = Visibility.Visible;
        }

        private NodeTypeEntry m_MenuSelectedNodeType;
        private void Graph_GeneratingPopup(object sender, GeneratingPopupEventArgs e)
        {
            if (AllowGraphEditing)
                foreach (NodeTypeEntry nte in NodeTypes)
                {
                    TextBlock txb = new TextBlock() { Text = string.Format("Insert {0}", nte.Name), Tag = nte };
                    txb.MouseLeftButtonUp += (sender2, e2) =>
                    {
                        m_MenuSelectedNodeType = (sender2 as TextBlock).Tag as NodeTypeEntry;
                        DNode node = Graph.AddNodeAtLocation(e.MousePos, true);
                        m_MenuSelectedNodeType = null;
                        Graph.ClosePopup();
                        Graph.BeginContentEdit(node);
                    };
                    e.ListBox.Items.Add(txb);
                }
            else
            {
                e.ListBox.Items.RemoveAt(2);
                e.ListBox.Items.RemoveAt(1);
            }
            if (!AllowLabelEditing)
                e.ListBox.Items.RemoveAt(0);
        }

        private void ApplyNodeTypeToInsertedNode(NodeTypeEntry nte, DNode node)
        {
            node.DrawingNode.Attr.Shape = nte.Shape;
            if (nte.XRadius != 0)
                node.DrawingNode.Attr.XRadius = nte.XRadius;
            if (nte.YRadius != 0)
                node.DrawingNode.Attr.YRadius = nte.YRadius;
            node.Tag = nte.Tag;
            Graph.ResizeNodeToLabel(node);
        }

        private void Graph_NodeInsertingByUser(object sender, EventArgs e)
        {
            if (NodeTypeComboBox.SelectedItem == null && m_MenuSelectedNodeType == null)
                return;
            NodeTypeEntry nte = m_MenuSelectedNodeType ?? (NodeTypeEntry)NodeTypeComboBox.SelectedItem;
            DNode dn = sender as DNode;
            ApplyNodeTypeToInsertedNode(nte, dn);
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "MSAGL file (*.msagl)|*.msagl";
            if (d.ShowDialog() == true)
            {
                using (Stream stream = d.File.OpenRead())
                {
                    Graph.Load(stream);
                    stream.Close();
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "MSAGL file (*.msagl)|*.msagl";
            d.DefaultExt = "msagl";
            if (d.ShowDialog() == true)
            {
                using (Stream stream = d.OpenFile())
                {
                    Graph.Save(stream);
                    stream.Close();
                }
            }
        }

        private void OptionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            (sender as ComboBox).SelectedItem = null;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Graph.Clear();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            Graph.Undo();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            Graph.Redo();
        }

        private void ZoomPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Graph.Zoom = (double)(Resources["ZoomConverter"] as ZoomConverter).ConvertBack(ZoomPerc.Text, typeof(double), null, CultureInfo.InvariantCulture);
            }
        }
    }
}