/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
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

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public partial class GraphControl : UserControl, INotifyPropertyChanged
    {
        private static bool UseComboInsertion = false;

        public GraphControl()
        {
            InitializeComponent();

            EdgeRoutingComboBox.SelectionChanged += (sender, args) => DoLayout_Click(null, null);
            Loaded += new RoutedEventHandler(GraphControl_Loaded);

            if (UseComboInsertion)
            {
                InsertionButton.Content = "Insert";
                InsertionButton.Tag = "ComboInsertion";
            }
        }

        void GraphControl_Loaded(object sender, RoutedEventArgs e)
        {
            NodeTypeComboBox.SelectionChanged += (sender2, args) => { InsertionButton.IsChecked = true; Graph.MouseMode = DraggingMode.ComboInsertion; };
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
                        value.NodeInsertedByUser += Graph_NodeInsertedByUser;
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

        void Graph_PropertyChanged(object sender, PropertyChangedEventArgs e)
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

        public bool ShowBorder
        {
            get
            {
                return Graph.ShowBorder;
            }
            set
            {
                Graph.ShowBorder = value;
                BorderCheckBox.IsChecked = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void DraggingMode_Click(object sender, RoutedEventArgs e)
        {
            // Search for an enum value corresponding to the Tag text.
            string t = (sender as RadioButton).Tag as string;
            FieldInfo v = typeof(DraggingMode).GetFields().FirstOrDefault(fi => fi.Name == t);
            Graph.MouseMode = v == null ? Graph.DefaultMouseMode : (DraggingMode)v.GetRawConstantValue();
        }

        private bool _AllowLayoutEditing = true;
        public bool AllowLayoutEditing
        {
            get
            {
                return _AllowLayoutEditing;
            }
            set
            {
                _AllowLayoutEditing = value;
                Graph.MouseMode = Graph.DefaultMouseMode;
                PanButton.IsChecked = true;
                LayoutEditButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                RouteEdgesButton.Visibility = value && ShowExperimentalControls ? Visibility.Visible : Visibility.Collapsed;
                UndoButton.Visibility = value && ShowExperimentalControls ? Visibility.Visible : Visibility.Collapsed;
                RedoButton.Visibility = value && ShowExperimentalControls ? Visibility.Visible : Visibility.Collapsed;
                if (!value)
                    AllowGraphEditing = false;
            }
        }

        private bool _AllowLabelEditing = true;
        public bool AllowLabelEditing
        {
            get
            {
                return _AllowLabelEditing;
            }
            set
            {
                _AllowLabelEditing = value;
                Graph.MouseMode = Graph.DefaultMouseMode;
            }
        }

        private bool _AllowGraphEditing = true;
        public bool AllowGraphEditing
        {
            get
            {
                return _AllowGraphEditing;
            }
            set
            {
                _AllowGraphEditing = value;
                Graph.MouseMode = Graph.DefaultMouseMode;
                LayoutEditButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                InsertionButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                if (UseComboInsertion)
                    NodeTypeComboBox.Visibility = (value && _NodeTypes.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
                ClearButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                if (value)
                    AllowLayoutEditing = true;
            }
        }

        private bool _AllowSaveLoad = true;
        public bool AllowSaveLoad
        {
            get
            {
                return _AllowSaveLoad;
            }
            set
            {
                _AllowSaveLoad = value;
                OpenButton.Visibility = value && ShowExperimentalControls ? Visibility.Visible : Visibility.Collapsed;
                SaveButton.Visibility = value && ShowExperimentalControls ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool _ShowExperimentalControls = false;
        public bool ShowExperimentalControls
        {
            get
            {
                return _ShowExperimentalControls;
            }
            set
            {
                _ShowExperimentalControls = value;
                LayeredLayoutButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                EdgeRoutingComboBox.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                RouteEdgesButton.Visibility = value && AllowLayoutEditing ? Visibility.Visible : Visibility.Collapsed;
                BorderCheckBox.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                OpenButton.Visibility = value && AllowSaveLoad ? Visibility.Visible : Visibility.Collapsed;
                SaveButton.Visibility = value && AllowSaveLoad ? Visibility.Visible : Visibility.Collapsed;
                UndoButton.Visibility = value && AllowLayoutEditing ? Visibility.Visible : Visibility.Collapsed;
                RedoButton.Visibility = value && AllowLayoutEditing ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void DoLayout_Click(object sender, RoutedEventArgs e)
        {
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

        private IEnumerable<DNode> _InitialNodes;
        public IEnumerable<DNode> InitialNodes
        {
            get
            {
                return _InitialNodes;
            }
            set
            {
                _InitialNodes = value;
                InitialNodesButton.Visibility = InitialNodes == null ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void Graph_GraphLayoutStarting(object sender, EventArgs e)
        {
            // I'd like to just call RemoveAllConstraints here, but it doesn't work.

            if (LayeredLayoutButton.IsChecked.Value)
            {
                Graph.Graph.LayoutAlgorithmSettings = new SugiyamaLayoutSettings();
                var settings = Graph.Graph.LayoutAlgorithmSettings as SugiyamaLayoutSettings;

                if (InitialNodesButton.IsChecked.Value && InitialNodes != null)
                {
                    // Declare all initial nodes to be above every other node.
                    foreach (DNode nc in InitialNodes)
                        foreach (DNode nc2 in Graph.Nodes().Where(ec => !InitialNodes.Contains(ec as DNode)))
                            settings.AddUpDownConstraint(nc.GeometryNode, nc2.GeometryNode);
                }

                if (AspectRatioButton.IsChecked.Value && GraphContainer.ActualHeight != 0.0 && GraphContainer.ActualWidth != 0.0)
                    settings.AspectRatio = HorizontalLayoutButton.IsChecked.Value ? GraphContainer.ActualHeight / GraphContainer.ActualWidth : GraphContainer.ActualWidth / GraphContainer.ActualHeight;

                if (HorizontalLayoutButton.IsChecked.Value)
                    (Graph.Graph.LayoutAlgorithmSettings as SugiyamaLayoutSettings).Transformation = PlaneTransformation.Rotation(Math.PI / 2);
                else
                    (Graph.Graph.LayoutAlgorithmSettings as SugiyamaLayoutSettings).Transformation = PlaneTransformation.Rotation(Math.PI);

                settings.EdgeRoutingSettings.EdgeRoutingMode = (EdgeRoutingMode)typeof(EdgeRoutingMode).GetField((EdgeRoutingComboBox.SelectedItem as ComboBoxItem).Tag.ToString()).GetValue(null);
                int separation = 5;
                Int32.TryParse(BundlingSeparationTextBox.Text, out separation);
                settings.EdgeRoutingSettings.BundlingSettings = new BundlingSettings() { EdgeSeparation = separation };
            }
            else
            {
                Graph.Graph.LayoutAlgorithmSettings = new MdsLayoutSettings();
                var settings = Graph.Graph.LayoutAlgorithmSettings as MdsLayoutSettings;

                settings.ScaleX = 100.0;
                settings.ScaleY = 100.0;
            }
        }

        public void BeginLayoutWithConstraints()
        {
            HorizontalLayoutButton.IsEnabled = LayeredLayoutButton.IsChecked.Value;
            InitialNodesButton.IsEnabled = LayeredLayoutButton.IsChecked.Value;
            AspectRatioButton.IsEnabled = LayeredLayoutButton.IsChecked.Value;
            EdgeRoutingComboBox.IsEnabled = LayeredLayoutButton.IsChecked.Value;

            Graph.BeginLayout();
        }

        private void RouteEdges_Click(object sender, RoutedEventArgs e)
        {
            int separation = 5;
            Int32.TryParse(BundlingSeparationTextBox.Text, out separation);
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

        private bool m_AddingFromMenu;
        void Graph_GeneratingPopup(object sender, GeneratingPopupEventArgs e)
        {
            foreach (NodeTypeEntry nte in NodeTypes)
            {
                TextBlock txb = new TextBlock() { Text = string.Format("Insert {0}", nte.Name), Tag = nte };
                txb.MouseLeftButtonUp += (sender2, e2) =>
                {
                    NodeTypeEntry nte2 = (sender2 as TextBlock).Tag as NodeTypeEntry;
                    m_AddingFromMenu = true;
                    DNode node = Graph.AddNodeAtLocation(e.MousePos, true);
                    m_AddingFromMenu = false;
                    ApplyNodeTypeToInsertedNode(nte2, node);
                    Graph.ClosePopup();
                    Graph.BeginContentEdit(node);
                };
                e.ListBox.Items.Add(txb);
            }
        }

        void ApplyNodeTypeToInsertedNode(NodeTypeEntry nte, DNode node)
        {
            node.DrawingNode.Attr.Shape = nte.Shape;
            if (nte.XRadius != 0)
                node.DrawingNode.Attr.XRadius = nte.XRadius;
            if (nte.YRadius != 0)
                node.DrawingNode.Attr.YRadius = nte.YRadius;
            node.Tag = nte.Tag;
            Graph.ResizeNodeToLabel(node);
        }

        void Graph_NodeInsertedByUser(object sender, EventArgs e)
        {
            if (NodeTypeComboBox.SelectedItem == null || m_AddingFromMenu)
                return;
            NodeTypeEntry nte = (NodeTypeEntry)NodeTypeComboBox.SelectedItem;
            DNode dn = sender as DNode;
            ApplyNodeTypeToInsertedNode(nte, dn);
        }

        private void BorderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ShowBorder = BorderCheckBox.IsChecked.Value;
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

    public class ZoomConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)((double)value * 100.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double foo;
            Double.TryParse(value.ToString(), out foo);
            return foo / 100.0;
        }
    }

    public class NodeTypeEntry
    {
        public string Name { get; set; }
        public Drawing.Shape Shape { get; set; }
        public double XRadius { get; set; }
        public double YRadius { get; set; }
        public object Tag { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}