using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.WpfGraphControl;
using Microsoft.Win32;
using Color = Microsoft.Msagl.Drawing.Color;
using ModifierKeys = System.Windows.Input.ModifierKeys;
using Size = System.Windows.Size;

namespace WpfApplicationSample
{
    class WpfApplicationSample : Application
    {
       

        public static readonly RoutedUICommand LoadSampleGraphCommand = new RoutedUICommand("Open File...", "OpenFileCommand",
                                                                                     typeof(WpfApplicationSample));
        public static readonly RoutedUICommand HomeViewCommand = new RoutedUICommand("Home view...", "HomeViewCommand",
                                                                                     typeof(WpfApplicationSample));

        

        Window appWindow;
        Grid mainGrid = new Grid();
        DockPanel graphViewerPanel = new DockPanel();
        ToolBar toolBar = new ToolBar();
        GraphViewer graphViewer = new GraphViewer();
        TextBox statusTextBox;


        protected override void OnStartup(StartupEventArgs e)
        {
#if TEST_MSAGL
     //       Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif

            appWindow = new Window {
                Title = "WpfApplicationSample",
                Content = mainGrid,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowState = WindowState.Normal
            };

            SetupToolbar();
            graphViewerPanel.ClipToBounds = true;
            mainGrid.Children.Add(toolBar);
            toolBar.VerticalAlignment=VerticalAlignment.Top;
            graphViewer.ObjectUnderMouseCursorChanged += graphViewer_ObjectUnderMouseCursorChanged;

            mainGrid.Children.Add(graphViewerPanel);
            graphViewer.BindToPanel(graphViewerPanel);

            SetStatusBar();
            graphViewer.MouseDown += WpfApplicationSample_MouseDown;
            appWindow.Loaded += (a,b)=>CreateAndLayoutAndDisplayGraph(null,null);

            //CreateAndLayoutAndDisplayGraph(null,null);
            //graphViewer.MainPanel.MouseLeftButtonUp += TestApi;
            appWindow.Show();
        }

        void WpfApplicationSample_MouseDown(object sender, MsaglMouseEventArgs e) {
            statusTextBox.Text = "there was a click...";
        }

        void SetStatusBar() {
            var statusBar = new StatusBar();
            statusTextBox = new TextBox {Text = "No object"};
            statusBar.Items.Add(statusTextBox);
            mainGrid.Children.Add(statusBar);
            statusBar.VerticalAlignment = VerticalAlignment.Bottom;
        }

        void graphViewer_ObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e) {
            var node = graphViewer.ObjectUnderMouseCursor as IViewerNode;
            if (node != null) {
                var drawingNode = (Node) node.DrawingObject;
                statusTextBox.Text = drawingNode.Label.Text;
            }
            else {
                var edge = graphViewer.ObjectUnderMouseCursor as IViewerEdge;
                if (edge != null)
                    statusTextBox.Text = ((Edge) edge.DrawingObject).SourceNode.Label.Text + "->" +
                                         ((Edge) edge.DrawingObject).TargetNode.Label.Text;
                else
                    statusTextBox.Text = "No object";
            }
        }




        void SetupToolbar()
        {
            SetupCommands();
            DockPanel.SetDock(toolBar, Dock.Top);
            SetMainMenu();
            //edgeRangeSlider = CreateRangeSlider();
            // toolBar.Items.Add(edgeRangeSlider.Visual);
        }


        void SetupCommands()
        {
            appWindow.CommandBindings.Add(new CommandBinding(LoadSampleGraphCommand, CreateAndLayoutAndDisplayGraph));
            appWindow.CommandBindings.Add(new CommandBinding(HomeViewCommand, (a, b) => graphViewer.SetInitialTransform()));
            appWindow.InputBindings.Add(new InputBinding(LoadSampleGraphCommand, new KeyGesture(Key.L, ModifierKeys.Control)));
            appWindow.InputBindings.Add(new InputBinding(HomeViewCommand, new KeyGesture(Key.H, ModifierKeys.Control)));

        }


        void SetMainMenu()
        {
            var mainMenu = new Menu { IsMainMenu = true };
            toolBar.Items.Add(mainMenu);
            SetFileMenu(mainMenu);
            SetViewMenu(mainMenu);
        }

        void SetViewMenu(Menu mainMenu)
        {
            var viewMenu = new MenuItem { Header = "_View" };
            var viewMenuItem = new MenuItem { Header = "_Home", Command = HomeViewCommand };
            viewMenu.Items.Add(viewMenuItem);
            mainMenu.Items.Add(viewMenu);
        }

        void SetFileMenu(Menu mainMenu)
        {
            var fileMenu = new MenuItem { Header = "_File" };
            var openFileMenuItem = new MenuItem { Header = "_Load Sample Graph", Command = LoadSampleGraphCommand };
            fileMenu.Items.Add(openFileMenuItem);
            mainMenu.Items.Add(fileMenu);            
        }



        void CreateAndLayoutAndDisplayGraph(object sender, ExecutedRoutedEventArgs ex) {
            try
            {
//                Graph graph = new Graph();
//                
//                //graph.LayoutAlgorithmSettings=new MdsLayoutSettings();
//             
//                graph.AddEdge("1", "2");
//                graph.AddEdge("1", "3");
//                var e = graph.AddEdge("4", "5");
//                e.LabelText = "Some edge label";
//                e.Attr.Color = Color.Red;
//                e.Attr.LineWidth *= 2;
//
//                graph.AddEdge("4", "6");
//                e = graph.AddEdge("7", "8");
//                e.Attr.LineWidth *= 2;
//                e.Attr.Color = Color.Red;
//
//                graph.AddEdge("7", "9");
//                e = graph.AddEdge("5", "7");
//                e.Attr.Color = Color.Red;
//                e.Attr.LineWidth *= 2;
//
//                graph.AddEdge("2", "7");
//                graph.AddEdge("10", "11");
//                graph.AddEdge("10", "12");
//                graph.AddEdge("2", "10");
//                graph.AddEdge("8", "10");
//                graph.AddEdge("5", "10");
//                graph.AddEdge("13", "14");
//                graph.AddEdge("13", "15");
//                graph.AddEdge("8", "13");
//                graph.AddEdge("2", "13");
//                graph.AddEdge("5", "13");
//                graph.AddEdge("16", "17");
//                graph.AddEdge("16", "18");
//                graph.AddEdge("16", "18");
//                graph.AddEdge("19", "20");
//                graph.AddEdge("19", "21");
//                graph.AddEdge("17", "19");
//                graph.AddEdge("2", "19");
//                graph.AddEdge("22", "23");
//
//                e = graph.AddEdge("22", "24");
//                e.Attr.Color = Color.Red;
//                e.Attr.LineWidth *= 2;
//
//                e = graph.AddEdge("8", "22");
//                e.Attr.Color = Color.Red;
//                e.Attr.LineWidth *= 2;
//
//                graph.AddEdge("20", "22");
//                graph.AddEdge("25", "26");
//                graph.AddEdge("25", "27");
//                graph.AddEdge("20", "25");
//                graph.AddEdge("28", "29");
//                graph.AddEdge("28", "30");
//                graph.AddEdge("31", "32");
//                graph.AddEdge("31", "33");
//                graph.AddEdge("5", "31");
//                graph.AddEdge("8", "31");
//                graph.AddEdge("2", "31");
//                graph.AddEdge("20", "31");
//                graph.AddEdge("17", "31");
//                graph.AddEdge("29", "31");
//                graph.AddEdge("34", "35");
//                graph.AddEdge("34", "36");
//                graph.AddEdge("20", "34");
//                graph.AddEdge("29", "34");
//                graph.AddEdge("5", "34");
//                graph.AddEdge("2", "34");
//                graph.AddEdge("8", "34");
//                graph.AddEdge("17", "34");
//                graph.AddEdge("37", "38");
//                graph.AddEdge("37", "39");
//                graph.AddEdge("29", "37");
//                graph.AddEdge("5", "37");
//                graph.AddEdge("20", "37");
//                graph.AddEdge("8", "37");
//                graph.AddEdge("2", "37");
//                graph.AddEdge("40", "41");
//                graph.AddEdge("40", "42");
//                graph.AddEdge("17", "40");
//                graph.AddEdge("2", "40");
//                graph.AddEdge("8", "40");
//                graph.AddEdge("5", "40");
//                graph.AddEdge("20", "40");
//                graph.AddEdge("29", "40");
//                graph.AddEdge("43", "44");
//                graph.AddEdge("43", "45");
//                graph.AddEdge("8", "43");
//                graph.AddEdge("2", "43");
//                graph.AddEdge("20", "43");
//                graph.AddEdge("17", "43");
//                graph.AddEdge("5", "43");
//                graph.AddEdge("29", "43");
//                graph.AddEdge("46", "47");
//                graph.AddEdge("46", "48");
//                graph.AddEdge("29", "46");
//                graph.AddEdge("5", "46");
//                graph.AddEdge("17", "46");
//                graph.AddEdge("49", "50");
//                graph.AddEdge("49", "51");
//                graph.AddEdge("5", "49");
//                graph.AddEdge("2", "49");
//                graph.AddEdge("52", "53");
//                graph.AddEdge("52", "54");
//                graph.AddEdge("17", "52");
//                graph.AddEdge("20", "52");
//                graph.AddEdge("2", "52");
//                graph.AddEdge("50", "52");
//                graph.AddEdge("55", "56");
//                graph.AddEdge("55", "57");
//                graph.AddEdge("58", "59");
//                graph.AddEdge("58", "60");
//                graph.AddEdge("20", "58");
//                graph.AddEdge("29", "58");
//                graph.AddEdge("5", "58");
//                graph.AddEdge("47", "58");
//
//                var subgraph = new Subgraph("subgraph 1");
//                graph.RootSubgraph.AddSubgraph(subgraph);
//                subgraph.AddNode(graph.FindNode("47"));
//                subgraph.AddNode(graph.FindNode("58"));
//
//                graph.AddEdge(subgraph.Id, "55");
//
//                var node = graph.FindNode("5");
//                node.LabelText = "Label of node 5";
//                node.Label.FontSize = 5;
//                node.Label.FontName = "New Courier";
//                node.Label.FontColor = Microsoft.Msagl.Drawing.Color.Blue;
//
//                node = graph.FindNode("55");
//               
//            
//                graph.Attr.LayerDirection= LayerDirection.LR;
//             //   graph.LayoutAlgorithmSettings.EdgeRoutingSettings.RouteMultiEdgesAsBundles = true;
//                //graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SplineBundling;
//                //layout the graph and draw it
                Graph graph = new Graph();
                graph.AddEdge("47", "58");
                graph.AddEdge("70", "71");



                var subgraph = new Subgraph("subgraph1");
                graph.RootSubgraph.AddSubgraph(subgraph);
                subgraph.AddNode(graph.FindNode("47"));
                subgraph.AddNode(graph.FindNode("58"));

                var subgraph2 = new Subgraph("subgraph2");
                subgraph2.Attr.Color = Color.Black;
                subgraph2.Attr.FillColor = Color.Yellow;
                subgraph2.AddNode(graph.FindNode("70"));
                subgraph2.AddNode(graph.FindNode("71"));
                subgraph.AddSubgraph(subgraph2);
                graph.AddEdge("58", subgraph2.Id);
                graph.Attr.LayerDirection = LayerDirection.LR;
                graphViewer.Graph = graph;
                
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            new WpfApplicationSample { Args = args }.Run();
        }

        public string[] Args { get; set; }
    }
}
