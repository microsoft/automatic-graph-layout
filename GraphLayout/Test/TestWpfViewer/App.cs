using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
 using System.Runtime.InteropServices;
﻿using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Dot2Graph;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Miscellaneous.LayoutEditing;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.WpfGraphControl;
﻿using Microsoft.SqlServer.Server;
﻿using Application = System.Windows.Application;
using Menu = System.Windows.Controls.Menu;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using ModifierKeys = System.Windows.Input.ModifierKeys;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using ToolBar = System.Windows.Controls.ToolBar;
using WindowStartupLocation = System.Windows.WindowStartupLocation;
using WindowState = System.Windows.WindowState;
using Microsoft.Msagl.Layout.Incremental;
using Color = Microsoft.Msagl.Drawing.Color;
using Node = Microsoft.Msagl.Drawing.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Shape = Microsoft.Msagl.Drawing.Shape;
using Size = System.Windows.Size;
using Microsoft.Msagl.GraphViewerGdi;

namespace TestWpfViewer {
    internal class App : Application {
        const string OneTimeRunOption = "-onerun";
        
        const string QuietOption = "-quiet";
        const string FileOption = "-file";
        const string BundlingOption = "-bundling";
        const string ListOfFilesOption = "-listoffiles";
        const string TestCdtOption = "-tcdt";
        const string TestCdtOption2 = "-tcdt2";
        const string TestCdtOption0 = "-tcdt0";
        const string TestCdtOption1 = "-tcdt1";
        const string ReverseXOption = "-rx";
        const string MdsOption = "-mds";
        const string FdOption = "-fd";
        const string EdgeSeparationOption = "-es";
        const string RecoverSugiyamaTestOption = "-rst";
        const string InkImportanceOption = "-ink";
        const string ConstraintsTestOption = "-tcnstr";
        const string TightPaddingOption = "-tpad";
        const string LoosePaddingOption = "-lpad";
        const string CapacityCoeffOption = "-cc";
        const string PolygonDistanceTestOption = "-pd";
        const string RandomBundlingTest = "-rbt";
        const string TestCdtThreaderOption = "-tth";
        const string AsyncLayoutOption = "-async";
        const string DoNotLayoutOption = "-dnl";
        const string SaveMsaglOption = "-savemsagl";
        const string LargeLayoutThresholdOption = "-llth";
        const string MaxNodesPerTileOption = "-mnpt";

        const string EdgeZoomLevelsUpperBoundOption = "-ezlu";
        const string AllowOverlapsInMds = "-overlapMds";
        const string RunRemoveOverlapsOption = "-rov";
        const string EnlargeHighDegreeNodes = "-ehd";
        const string NodeSeparationOption = "-nodesep";
        const string RoundedCornersOption = "-rc";
        const string PrintMaxNodeDegreeOption = "-pmnd";
        const string FileListOption = "-fl";
        const string NodeQuotaOption = "-nq";
        const string NoIterationsWithMajorization = "-niwm";
        const string SequentialRunOption = "-seq";
        const string StraightLineEdgesOption = "-sl";
        const string NoEdgeRoutingOption = "-nr";
        const string ExitAfterLgLayoutOption = "-lgexit";
        const string BackgroundImageOption = "-bgimage";
        const string DrawBackgrounImageOption = "-drawbg";

        public static readonly RoutedUICommand OpenFileCommand = new RoutedUICommand("Open File...", "OpenFileCommand",
                                                                                     typeof (App));


        public static readonly RoutedUICommand CancelLayoutCommand = new RoutedUICommand("Cancel Layout...", "CancelLayoutCommand",
                                                                                     typeof(App));

        public static readonly RoutedUICommand ReloadCommand = new RoutedUICommand("Reload File...", "ReloadCommand",
                                                                                   typeof (App));

        public static readonly RoutedUICommand SaveImageCommand = new RoutedUICommand("Save Image...", "SaveImageCommand",
                                                                                   typeof(App));

        public static readonly RoutedUICommand SaveMsaglCommand = new RoutedUICommand("Save Msagl...", "SaveMsaglCommand",
                                                                                   typeof(App));

        public static readonly RoutedUICommand ExitCommand = new RoutedUICommand("Exit...", "ExitCommand",
                                                                                   typeof(App));

        public static readonly RoutedUICommand HomeViewCommand = new RoutedUICommand("Home view...", "HomeViewCommand",
                                                                                     typeof (App));
     
        public static readonly RoutedUICommand ScaleNodeDownCommand = new RoutedUICommand("Scale node down...",
                                                                                          "ScaleNodeDownCommand",
                                                                                          typeof (App));

        public static readonly RoutedUICommand ScaleNodeUpCommand = new RoutedUICommand("Scale node up...",
                                                                                        "ScaleNodeUpCommand",
                                                                                        typeof (App));


        Window appWindow;
        readonly DockPanel dockPanel = new DockPanel();
        readonly DockPanel graphViewerPanel = new DockPanel();
        readonly ToolBar toolBar = new ToolBar();
        readonly GraphViewer graphViewer = new GraphViewer();
        string lastFileName;
        static ArgsParser.ArgsParser argsParser;
        TextBox statusTextBox;
        Timer fileListTimer;
        int fileListDelay = 8000;
        string currentFileListName;
        //RangeSlider edgeRangeSlider;

        protected override void OnStartup(StartupEventArgs e) {
#if TEST_MSAGL
            Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif

            appWindow = new Window {
                Title = "My app for testing wpf graph control",
                Content = dockPanel,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowState = WindowState.Normal
            };

            SetupToolbar();
            graphViewerPanel.ClipToBounds = true;
            dockPanel.Children.Add(toolBar);
            SetUpStatusBar();

            dockPanel.LastChildFill = true;
            dockPanel.Children.Add(graphViewerPanel);
            graphViewer.BindToPanel(graphViewerPanel);
            dockPanel.Loaded += GraphViewerLoaded;
            argsParser = SetArgsParser(Args);
            //graphViewer.MainPanel.MouseLeftButtonUp += TestApi;
            graphViewer.ViewChangeEvent += GraphViewerViewChangeEvent;
            graphViewer.ObjectUnderMouseCursorChanged += GvObjectUnderMouseCursorChanged;
            graphViewer.MouseDown += GraphViewerMouseDown;
            graphViewer.MouseMove += GraphViewerMouseMove;
            

            var msaglFile = argsParser.GetStringOptionValue(SaveMsaglOption);
            if (msaglFile != null)
                graphViewer.MsaglFileToSave = msaglFile;
            
            graphViewer.GraphChanged += graphViewer_GraphChanged;
            //graphViewer.LayoutEditingEnabled = false;
            appWindow.Show();
        }

        void GraphViewerMouseMove(object sender, MsaglMouseEventArgs e) {
            SetStatusText();
        }

        

        void GvObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e) {
            SetStatusText();
        }

        void GraphViewerMouseDown(object sender, MsaglMouseEventArgs e) {
            if (e.RightButtonIsPressed) {
                PopupMenus(CreatePopupMenu());
                e.Handled = true;
            }            
        }

        Tuple<string,VoidDelegate> [] CreatePopupMenu() {
            var ret = new List<Tuple<string, VoidDelegate>> {
                new Tuple<string, VoidDelegate>("create node", CreateNodeDelegate)
            };
            var objectUnderMouseCursor = graphViewer.ObjectUnderMouseCursor;
            var vedge = objectUnderMouseCursor as IViewerEdge;
            if (vedge != null) {
                ret.Add(new Tuple<string, VoidDelegate>("remove edge", () => { graphViewer.RemoveEdge(vedge, false); }));
                ret.Add(ColorChangeMenuTuple(vedge.Edge.Attr));
            } else {
                var vnode = objectUnderMouseCursor as IViewerNode;
                if (vnode != null) {
                    ret.Add(ColorChangeMenuTuple(vnode.Node.Attr));
                    ret.Add(new Tuple<string, VoidDelegate>("remove node", () => graphViewer.RemoveNode(vnode, false)));
                }
            }

            ret.Add(new Tuple<string, VoidDelegate>("find a node to pan here", PanToNode));

            return ret.ToArray();
        }

        void CreateNodeDelegate() {
            if (graphViewer.Graph == null)
                return;
            var nodeCenter = MousePositionToGraph();
            Node drawingNode = CreateDrawingNodeByUsingDialog();
            var iNode = graphViewer.CreateIViewerNode(drawingNode, nodeCenter, null);
            iNode.Node.Attr.Color = Color.Red;
            
        }


        void PanToNode() {
            System.Windows.Point mousePosition = Mouse.GetPosition(MainWindow); 
            Node drawingNode = FindNodeByUsingDialog();
            if (drawingNode != null)
                PanDrawingNodeToMousePosition(drawingNode, mousePosition);
        }

        void PanDrawingNodeToMousePosition(Node drawingNode, System.Windows.Point mousePosition) {
            PlaneTransformation transform = graphViewer.Transform;
            var nodeCenterOnScreen = transform * drawingNode.GeometryNode.Center;
            transform[0, 2] += mousePosition.X - nodeCenterOnScreen.X;
            transform[1, 2] += mousePosition.Y - nodeCenterOnScreen.Y;
            graphViewer.Transform = transform;
        }

        Node FindNodeByUsingDialog() {
            TextBox textBox;
            Window window = CreateNodeFindDialog(out textBox);
            var res=window.ShowDialog();
            var ret= graphViewer.Graph.Nodes.FirstOrDefault(n => n.Label != null && n.LabelText.ToLower().Contains(textBox.Text.ToLower()));
            if (ret == null)
                MessageBox.Show(String.Format("cannot find a node with substring \"{0}\"", textBox.Text));
            return ret;
        }

        Window CreateNodeFindDialog(out TextBox textBox) {
            var window = new Window { Width = 200,Height = 200 };
            var mp = Mouse.GetPosition(dockPanel);
            window.Left = mp.X;
            window.Top = mp.Y;
            var panel = new DockPanel();
            window.Title = "Enter a node label substring";
 
            window.Content = panel;
         
            textBox = new TextBox();
            textBox.FontSize *= 1.5;
            textBox.FontFamily = new FontFamily("System.Diagnostics.Debugs");
            textBox.Width = 400;
            DockPanel.SetDock(textBox,Dock.Top);
            panel.Children.Add(textBox);
            panel.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            var button = new Button { Content = "OK" };
            button.Click += (a,b) => window.Close();
            DockPanel.SetDock(button,Dock.Bottom);
            button.IsDefault = true;
            button.Width = 40;
            button.Height = 40;
            
            panel.Children.Add(button);
            panel.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            window.SizeToContent = SizeToContent.WidthAndHeight;
            textBox.Focus();
            return window;
        
        }

        Point MousePositionToGraph() {
            var pos = Mouse.GetPosition(graphViewer.GraphCanvas);
            return new Point(pos.X, pos.Y);
        }

        Node CreateDrawingNodeByUsingDialog() {
            RichTextBox richBox;
            var window = CreateNodeDialog(out richBox);
            window.ShowDialog();

            var r = new Random();
            var i = r.Next();

            var createdNode = new Node(i.ToString());
            var s = new TextRange(richBox.Document.ContentStart, richBox.Document.ContentEnd).Text;
            createdNode.LabelText = s.Trim('\r', '\n',' ','\t');
            return createdNode;
        }

        Window CreateNodeDialog(out RichTextBox richBox) {
            var window = new Window {Width = 200, Height = 200};
            var mp = Mouse.GetPosition(dockPanel);
            window.Left = mp.X;
            window.Top = mp.Y;
            var panel = new DockPanel();

            window.Content = panel;

            var textBox = new TextBox {Text = "Please modify the node label:"};

            DockPanel.SetDock(textBox, Dock.Top);
            panel.Children.Add(textBox);

            richBox = new RichTextBox();
            richBox.FontSize *= 1.5;
            richBox.AppendText("Label");
            richBox.FontFamily = new FontFamily("System.Diagnostics.Debugs");
            richBox.Width = window.Width;
            DockPanel.SetDock(richBox, Dock.Top);
            panel.Children.Add(richBox);
            panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            panel.Width = textBox.Width;
            var button = new Button {Content = "OK"};
            button.Click += (a, b) => window.Close();
            DockPanel.SetDock(button, Dock.Bottom);
            button.IsDefault = true;
            button.Width = 40;
            button.Height = 40;
            panel.Children.Add(button);
            panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            window.SizeToContent = SizeToContent.WidthAndHeight;
            return window;
        }


        static Tuple<string, VoidDelegate> ColorChangeMenuTuple(AttributeBase attr) {
            return new Tuple<string, VoidDelegate>("set color", () => {
                var dialog = new System.Windows.Forms.ColorDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    var color = dialog.Color;
                    attr.Color = new Microsoft.Msagl.Drawing.Color(color.A, color.R, color.G, color.B);
                }
            });
        }

        static public object CreateMenuItem(string title, VoidDelegate voidVoidDelegate)
        {
            var menuItem = new MenuItem { Header = title };
            menuItem.Click += (RoutedEventHandler)(delegate { voidVoidDelegate(); });
            return menuItem;
        }

        public void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems)
        {
            var contextMenu = new ContextMenu();
            foreach (var pair in menuItems)
                contextMenu.Items.Add(CreateMenuItem(pair.Item1, pair.Item2));
            contextMenu.Closed += ContextMenuClosed;
            ContextMenuService.SetContextMenu(graphViewer.GraphCanvas, contextMenu);

        }

        void ContextMenuClosed(object sender, RoutedEventArgs e) {
            ContextMenuService.SetContextMenu(graphViewer.GraphCanvas, null);
        }



        void graphViewer_GraphChanged(object sender, EventArgs e) {
            appWindow.Title = (lastFileName ?? "") +
                                    String.Format(" ({0} nodes, {1} edges)", graphViewer.Graph.NodeCount,
                                    graphViewer.Graph.EdgeCount);
        }

        void GraphViewerViewChangeEvent(object sender, EventArgs e) {
            SetStatusText();
        }

        void SetStatusText() {
            
            var location = Mouse.GetPosition(graphViewer.GraphCanvas);

            statusTextBox.Text = String.Format("scale {0}, children={1} MouseX={2} MouseY={3}",
                                               graphViewer.ZoomFactor,
                                               graphViewer.GraphCanvas.Children.Count,
                                               location.X.ToString("N3"),
                                               location.Y.ToString("N3"));
        }


        void SetUpStatusBar()
        {
            var statusBar = new StatusBar();
            statusTextBox = new TextBox();
            statusBar.Items.Add(statusTextBox);
            DockPanel.SetDock(statusBar, Dock.Bottom);
            statusBar.VerticalAlignment = VerticalAlignment.Bottom;
            dockPanel.Children.Add(statusBar);
        }

//        void TestApi(object o, MouseButtonEventArgs e) {
//            var pt = e.GetPosition(graphViewer.MainPanel);
//
//            // Expand the hit test area by creating a geometry centered on the hit test point.
//
//            var rect = new Rect(new System.Windows.Point(pt.X - 2, pt.Y - 2),
//                                new System.Windows.Point(pt.X + 2, pt.Y + 2));
//            var expandedHitTestArea = new RectangleGeometry(rect);
//
//            hitVNode = null;
//            
//            // Set up a callback to receive the hit test result enumeration.
//            VisualTreeHelper.HitTest(graphViewer.MainPanel, null,
//                                     MyHitTestResultCallback,
//                                     new GeometryHitTestParameters(expandedHitTestArea));
//            if(hitVNode!=null)
//                graphViewer.ApiTestForChangingZoomLevels(hitVNode);  
//        }


        //        void SetupEdgeRangeSlider() {
//            var lgSettings = GetLgLayoutSettings();
//            if (lgSettings == null) return;
//            if (lgSettings.MaximalEdgeZoomLevelRange != null)
//            {
//                edgeRangeSlider.Minimum = lgSettings.MaximalEdgeZoomLevelRange.Start;
//                edgeRangeSlider.Maximum = lgSettings.MaximalEdgeZoomLevelRange.End;
//
//                lgSettings.RangeOfEdgeZoomLevels = () => new Range(edgeRangeSlider.Low, edgeRangeSlider.High);
//            }
//            edgeRangeSlider.RangeChanged+=EdgeRangeSliderRangeChanged;
//        }

        bool _graphViewerIsLoaded;
        void GraphViewerLoaded(object sender, EventArgs e) {
            if (_graphViewerIsLoaded) return;

            _graphViewerIsLoaded = true;
            
            string fileName = argsParser.GetStringOptionValue(FileOption);
            if (fileName != null)
                CreateAndLayoutGraph(fileName);
            else {
                string fileList = argsParser.GetStringOptionValue(FileListOption);
                if (fileList != null)
                    ProcessFileList(fileList);
            }            
        }

        void ProcessFileList(string fileList) {
            StreamReader sr;
            string fileListDir; 
            try
            {
                fileListDir = Path.GetDirectoryName(fileList);
                sr = new StreamReader(fileList);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return;
            }
            graphViewer.LayoutStarted += (a, b) => { System.Diagnostics.Debug.WriteLine("processing {0}", currentFileListName); };
            graphViewer.LayoutComplete += (a, b) => { System.Diagnostics.Debug.WriteLine("Done with {0}", currentFileListName);
                SetupNextRun(sr, fileListDir);
            };
            currentFileListName = ReadNextFileName(sr, fileListDir);
            CreateAndLayoutGraph(currentFileListName);            
           
        }

        void SetupNextRun(StreamReader sr, string fileListDir) {
            currentFileListName = ReadNextFileName(sr, fileListDir);
            if (currentFileListName == null) {
                sr.Close();
                return;
            }

            fileListTimer = new Timer(fileListDelay);
            fileListTimer.Elapsed += (c, d) => {
                fileListTimer.Stop();
                CreateAndLayoutGraph(currentFileListName);
            };
            fileListTimer.Start();
        }

        string ReadNextFileName(StreamReader sr, string fileListDir) {
            var fn = sr.ReadLine();
            if (fn == null)
                return null;
            return Path.Combine(fileListDir, fn.ToLower());
        }


        static ArgsParser.ArgsParser SetArgsParser(string [] args) {
            argsParser = new ArgsParser.ArgsParser(args);
            argsParser.AddAllowedOptionWithHelpString(OneTimeRunOption, "loads only one graph");
            argsParser.AddAllowedOptionWithHelpString(SequentialRunOption, "no threads");
            argsParser.AddAllowedOptionWithHelpString(StraightLineEdgesOption,"route straight line edges");
            argsParser.AddAllowedOptionWithHelpString(NoEdgeRoutingOption, "don't route the edges");
            argsParser.AddAllowedOptionWithHelpString(NoIterationsWithMajorization, "0 iterations with majorization");
            argsParser.AddOptionWithAfterStringWithHelp(FileListOption,"file list");            
            argsParser.AddAllowedOptionWithHelpString(RoundedCornersOption,"rounded corners for boxes always");
            argsParser.AddAllowedOptionWithHelpString(PrintMaxNodeDegreeOption, "print max node degree and exit");
            argsParser.AddOptionWithAfterStringWithHelp(NodeSeparationOption, "node separation");
            argsParser.AddOptionWithAfterStringWithHelp(NodeQuotaOption, "node quota");
            argsParser.AddAllowedOption(AllowOverlapsInMds);
            argsParser.AddAllowedOption(RunRemoveOverlapsOption);
            argsParser.AddAllowedOptionWithHelpString(DrawBackgrounImageOption, "will draw the background in LG browsing - used for experimenting");
            
            argsParser.AddAllowedOptionWithHelpString(EdgeZoomLevelsUpperBoundOption, "use upper bound in the edge zoom level algorithm");
            argsParser.AddOptionWithAfterStringWithHelp(LargeLayoutThresholdOption, "sets the large layout threshold");
            argsParser.AddOptionWithAfterStringWithHelp(BackgroundImageOption, "sets the background image for the large layout");

            argsParser.AddOptionWithAfterStringWithHelp(MaxNodesPerTileOption, "sets the max nodes per tile for large layout");
            argsParser.AddAllowedOptionWithHelpString(DoNotLayoutOption, "do not call the layout calculation");
            argsParser.AddOptionWithAfterStringWithHelp(SaveMsaglOption, "saves the file into a msagl file");
            argsParser.AddAllowedOption(RecoverSugiyamaTestOption);
            argsParser.AddAllowedOption(QuietOption);
            argsParser.AddAllowedOption(BundlingOption);
            argsParser.AddOptionWithAfterStringWithHelp(FileOption,"the name of the input file");
            argsParser.AddOptionWithAfterStringWithHelp(ListOfFilesOption,
                                                        "the name of the file containing a list of files");
            argsParser.AddAllowedOptionWithHelpString(TestCdtOption,"testing Constrained Delaunay Triangulation");
            argsParser.AddAllowedOptionWithHelpString(TestCdtOption0,
                                                      "testing Constrained Delaunay Triangulation on a small graph");
            argsParser.AddAllowedOptionWithHelpString(TestCdtOption1,"testing threading through a CDT");
            argsParser.AddAllowedOptionWithHelpString(TestCdtOption2,
                                                      "testing Constrained Delaunay Triangulation on file \'polys\'");
            argsParser.AddAllowedOptionWithHelpString(ReverseXOption,"reversing X coordinate");
            argsParser.AddOptionWithAfterStringWithHelp(EdgeSeparationOption,"use specified edge separation");
            argsParser.AddAllowedOptionWithHelpString(MdsOption,"use mds layout");
            argsParser.AddAllowedOptionWithHelpString(FdOption,"use force directed layout");
            argsParser.AddAllowedOptionWithHelpString(ConstraintsTestOption,"test constraints");
            argsParser.AddOptionWithAfterStringWithHelp(InkImportanceOption,"ink importance coefficient");
            argsParser.AddOptionWithAfterStringWithHelp(TightPaddingOption,"tight padding coefficient");
            argsParser.AddOptionWithAfterStringWithHelp(LoosePaddingOption,"loose padding coefficient");
            argsParser.AddOptionWithAfterStringWithHelp(CapacityCoeffOption,"capacity coeffiecient");
            argsParser.AddAllowedOptionWithHelpString(PolygonDistanceTestOption,"test Polygon.Distance");
            argsParser.AddAllowedOptionWithHelpString(RandomBundlingTest,"random bundling test");
            argsParser.AddAllowedOptionWithHelpString(TestCdtThreaderOption,"test CdtThreader");
            argsParser.AddAllowedOptionWithHelpString(AsyncLayoutOption,"test viewer in the async mode");
            argsParser.AddAllowedOptionWithHelpString(EnlargeHighDegreeNodes, "enlarge high degree nodes");
            argsParser.AddAllowedOptionWithHelpString(ExitAfterLgLayoutOption, "exit after lg calculation");

            if(!argsParser.Parse()) {
                System.Diagnostics.Debug.WriteLine(argsParser.UsageString());
                Environment.Exit(1);
            }
            return argsParser;
        }

        void SetupToolbar() {
            SetCommands();
            DockPanel.SetDock(toolBar, Dock.Top);
            SetMainMenu();
            //edgeRangeSlider = CreateRangeSlider();
           // toolBar.Items.Add(edgeRangeSlider.Visual);
        }

        /*
        static void HandleMouseUpOnSlider(Action<double, double> updateEdgesInZoomRange, MouseButtonEventArgs mouseButtonEventArgs, Slider slider) {
            mouseButtonEventArgs.Handled = true;
            updateEdgesInZoomRange(slider.SelectionStart, slider.SelectionEnd);
        }
        */
       /* 
        void UpdateThumbOnDrag(Slider slider, double del, Thumb  draggedThumb) {
            var currentPosition = Canvas.GetLeft(draggedThumb);
            var newPos = currentPosition + del;
            if (newPos < 0)
                newPos = 0;
            else if (newPos > slider.Width)
                newPos = slider.Width-draggedThumb.Width;
            Canvas.SetLeft(draggedThumb,newPos);
            var k = (slider.Maximum - slider.Minimum) / (slider.Width - draggedThumb.Width);
             
            if (del != 0 && Keyboard.Modifiers == ModifierKeys.Control) {
                slider.Value += del*k;
                return;
            }

            var thumbVal = k*Canvas.GetLeft(draggedThumb) + slider.Minimum;
            slider.SelectionStart = Math.Min(thumbVal, slider.Value);
            slider.SelectionEnd = Math.Max(thumbVal, slider.Value);
            UpdateEdgesInZoomRange(slider.SelectionStart, slider.SelectionEnd);
        }
        */

       

        



//        void slider_MouseWheel(object sender, MouseWheelEventArgs e) {
//            double scale = e.Delta > 0 ? 1.1 : 0.9;
//            var slider = (Slider) sender;
//            var m = (slider.SelectionEnd + slider.SelectionStart)/2;
//            var w = (slider.SelectionEnd - slider.SelectionStart)/2*scale;
//            slider.SelectionStart = m - w;
//            slider.SelectionEnd = m + w;
//        }


        void SetCommands() {
            appWindow.CommandBindings.Add(new CommandBinding(SaveImageCommand, SaveImage));
            appWindow.CommandBindings.Add(new CommandBinding(SaveMsaglCommand, SaveMsagl));
            appWindow.CommandBindings.Add(new CommandBinding(ExitCommand, ExitHandler));
            appWindow.CommandBindings.Add(new CommandBinding(OpenFileCommand, OpenFile));
            appWindow.CommandBindings.Add(new CommandBinding(CancelLayoutCommand,
                                                             (a, b) => { graphViewer.CancelToken.Canceled = true; }));
            appWindow.CommandBindings.Add(new CommandBinding(ReloadCommand, ReloadFile));
            appWindow.CommandBindings.Add(new CommandBinding(HomeViewCommand, (a, b) => graphViewer.SetInitialTransform()));
            appWindow.InputBindings.Add(new InputBinding(OpenFileCommand, new KeyGesture(Key.O, ModifierKeys.Control)));
            appWindow.InputBindings.Add(new InputBinding(ReloadCommand, new KeyGesture(Key.F5))); 
            appWindow.InputBindings.Add(new InputBinding(HomeViewCommand, new KeyGesture(Key.H, ModifierKeys.Control)));
            appWindow.InputBindings.Add(new InputBinding(CancelLayoutCommand, new KeyGesture(Key.C, ModifierKeys.Control)));
            appWindow.InputBindings.Add(new InputBinding(ExitCommand, new KeyGesture(Key.F4, ModifierKeys.Alt)));
            
        


            appWindow.InputBindings.Add(new InputBinding(ScaleNodeDownCommand, new KeyGesture(Key.S, ModifierKeys.Control)));
          //  appWindow.CommandBindings.Add(new CommandBinding(ScaleNodeDownCommand, ScaleNodeDownTest));
            appWindow.CommandBindings.Add(new CommandBinding(ScaleNodeDownCommand, ScaleNodeUpTest));
            appWindow.InputBindings.Add(new InputBinding(ScaleNodeUpCommand, new KeyGesture(Key.S, ModifierKeys.Control|ModifierKeys.Shift)));

        }

        void SaveImage(object sender, ExecutedRoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = lastFileName+".png"; // Default file name
            dlg.DefaultExt = ".png"; // Default file extension
            dlg.Filter = "png files (.png)|*.png"; // Filter files by extension

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                graphViewer.DrawImage(dlg.FileName);
            }
        }

        void SaveMsagl(object sender, ExecutedRoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = lastFileName + ".msagl"; // Default file name
            dlg.DefaultExt = ".msagl"; // Default file extension
            dlg.Filter = "msagl files (.msagl)|*.msagl"; // Filter files by extension

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                graphViewer.Graph.Write(dlg.FileName);
            }
        }

        void ExitHandler(object sender, ExecutedRoutedEventArgs e)
        {
            Environment.Exit(0);
        }


        void ScaleNodeUpTest(object sender, ExecutedRoutedEventArgs e) {
            throw new NotImplementedException();
        }

        void SetMainMenu() {
            var mainMenu = new Menu {IsMainMenu = true};
            SetFileMenu(mainMenu);
            SetViewMenu(mainMenu);
            SetInsertEdgesCheckBox(mainMenu);
        }

        void SetInsertEdgesCheckBox(Menu mainMenu) {
            var insertEdgesCheckBox = new CheckBox {Content = "Insert edges",  };
            mainMenu.Items.Add(insertEdgesCheckBox);
            insertEdgesCheckBox.Checked += (a, b) => graphViewer.InsertingEdge = true;
            insertEdgesCheckBox.Unchecked += (a, b) => graphViewer.InsertingEdge = false;
        }

        void SetViewMenu(Menu mainMenu) {
            var viewMenu = new MenuItem { Header = "_View" };
            var viewMenuItem = new MenuItem { Header = "_Home", Command = HomeViewCommand };
            viewMenu.Items.Add(viewMenuItem);
            mainMenu.Items.Add(viewMenu);
        }

        void SetFileMenu(Menu mainMenu) {
            var fileMenu = new MenuItem {Header = "_File"};
            var openFileMenuItem = new MenuItem {Header = "_Open", Command = OpenFileCommand};
            fileMenu.Items.Add(openFileMenuItem);
            var reloadFileMenuItem = new MenuItem {Header = "_Reload", Command = ReloadCommand};
            fileMenu.Items.Add(reloadFileMenuItem);

            var cancelLayoutMenuItem = new MenuItem { Header = "_Cancel Layout", Command = CancelLayoutCommand };
            fileMenu.Items.Add(cancelLayoutMenuItem);
            mainMenu.Items.Add(fileMenu);
            toolBar.Items.Add(mainMenu);

            var saveImageMenuItem = new MenuItem { Header = "_Save Image", Command = SaveImageCommand };
            fileMenu.Items.Add(saveImageMenuItem);

            var saveMsaglMenuItem = new MenuItem { Header = "_Save MSAGL", Command = SaveMsaglCommand };
            fileMenu.Items.Add(saveMsaglMenuItem);

            var exitMenuItem = new MenuItem { Header = "_Exit", Command = ExitCommand };
            fileMenu.Items.Add(exitMenuItem);
        }

        void ReloadFile(object sender, ExecutedRoutedEventArgs e) {
            if (lastFileName != null)
                CreateAndLayoutGraph(lastFileName);
        }


        void OpenFile(object sender, ExecutedRoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog {
                RestoreDirectory = true,                
            };
            if (openFileDialog.ShowDialog() == true)
                CreateAndLayoutGraph(openFileDialog.FileName);
            
        }

        public void CreateAndLayoutGraph(string fileName) {
            try {
                if (fileName == null) return;
                string extension = Path.GetExtension(fileName).ToLower();
                lastFileName = fileName;
                switch (extension) {
                    case ".graphml":
                        ProcessGraphml(fileName);
                        break;
                    case ".dot":
                        ProcessDot(fileName);
                        break;
                    case ".gml":
                        ProcessGml(fileName);
                        break;
                    case ".dgml":
                        ProcessDgml(fileName);
                        break;
                    case ".net":
                        ProcessNet(fileName);
                        break;
                    case ".gexf":
                        ProcessGexf(fileName);
                        break;
                    case ".msagl":
                        ProcessMsagl(fileName);
                        break;
                    default:
                        throw new NotImplementedException(String.Format("format {0} is not supported", extension));
                }
            } catch (Exception e) {
                MessageBox.Show(e.ToString(), "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void ProcessGraphml(string fileName) {
            var parser = new GraphmlParser(fileName);
            Graph graph = parser.Parse();
            PassGraphToControl(graph);
        }

        void ProcessMsagl(string fileName) {
            var graph = Graph.Read(fileName);
            
            if (graph != null) {
                if (argsParser.OptionIsUsed(PrintMaxNodeDegreeOption)) {
                    System.Diagnostics.Debug.WriteLine("max node degree {0}",
                        graph.Nodes.Max(n => n.OutEdges.Count() + n.InEdges.Count() + n.SelfEdges.Count()));
                    Environment.Exit(0);
                }


                if (argsParser.OptionIsUsed(RoundedCornersOption))
                    foreach (var n in graph.Nodes)
                        n.Attr.XRadius = n.Attr.YRadius = 3;

                if (argsParser.OptionIsUsed(RunRemoveOverlapsOption)) {
                    GTreeOverlapRemoval.RemoveOverlaps(graph.GeometryGraph.Nodes.ToArray(),
                        graph.LayoutAlgorithmSettings.NodeSeparation);
                }
            }

            GiveGraphToControlFromMsagl(graph);
        }

        void GiveGraphToControlFromMsagl(Graph graph) {
            var fn = argsParser.GetStringOptionValue(SaveMsaglOption);
            var oldVal = graphViewer.NeedToCalculateLayout;
            if (fn != null) {
                graphViewer.NeedToCalculateLayout = true;
                SetLayoutSettings(graph);
                graphViewer.MsaglFileToSave = fn;
                graphViewer.Graph = graph;
            }
            else {
                graphViewer.NeedToCalculateLayout = false;
                graphViewer.LayoutComplete += (a, b) => graphViewer.NeedToCalculateLayout = oldVal;
                graphViewer.Graph = graph;
            }
        }

        void ProcessGexf(string fileName) {
            int line, column;
            string msg;
            Graph dgraph = GexfParser.Parse(fileName, out line, out column, out msg);
            if (dgraph != null) {
                SetLayoutSettings(dgraph);
                var origin = new Microsoft.Msagl.Core.Geometry.Point(0, 0);
                bool layoutExist = dgraph.GeometryGraph != null &&
                                   dgraph.GeometryGraph.Nodes.All(n => n.BoundaryCurve != null) &&
                                   dgraph.GeometryGraph.Nodes.Any(n => n.Center != origin);
                if (layoutExist && argsParser.GetStringOptionValue(SaveMsaglOption) != null) {
                    dgraph.Write(argsParser.GetStringOptionValue(SaveMsaglOption));
                    System.Diagnostics.Debug.WriteLine("saved to {0}", argsParser.GetStringOptionValue(SaveMsaglOption));
                    Environment.Exit(0);
                }

                graphViewer.NeedToCalculateLayout = !layoutExist;

                graphViewer.Graph = dgraph;
                graphViewer.NeedToCalculateLayout = true;
            }
            else
                MessageBox.Show(msg + String.Format(" line {0} column {1}", line, column));
        }

        void ProcessNet(string fileName) {
            int line, column;
            string msg;
            Graph gwgraph = NetParser.Parse(fileName, out line, out column, out msg);
            if (gwgraph != null) {
                SetLayoutSettings(gwgraph);
                graphViewer.Graph = gwgraph;
            }
            else
                MessageBox.Show(msg + String.Format(" line {0} column {1}", line, column));
        }

        void ProcessDgml(string fileName) {
#if TEST_MSAGL
            Graph gwgraph = DgmlParser.DgmlParser.Parse(fileName);
            if (gwgraph != null) {
                SetLayoutSettings(gwgraph);
                graphViewer.Graph = gwgraph;
            }
            else
#endif
                MessageBox.Show("cannot load " + fileName);
        }

        void ProcessGml(string fileName) {
            Graph gwgraph = GmlParser.Parse(fileName);
            if (gwgraph != null) {
                SetLayoutSettings(gwgraph);
                graphViewer.Graph = gwgraph;
            }
            else
                MessageBox.Show("cannot load " + fileName);
        }

        void ProcessDot(string fileName) {
            int line, column;
            string msg;
            Graph gwgraph = Parser.Parse(fileName, out line, out column, out msg);
            if (gwgraph != null) {
                TestGraph(gwgraph);
                PassGraphToControl(gwgraph);
            }
            else
                MessageBox.Show(msg + String.Format(" line {0} column {1}", line, column));
        }

        void PassGraphToControl(Graph gwgraph) {
            if (argsParser.OptionIsUsed(RoundedCornersOption))
                foreach (var n in gwgraph.Nodes) {
                    n.Attr.Shape = Shape.Box;
                    n.Attr.XRadius = n.Attr.YRadius = 3;
                }


        
            SetLayoutSettings(gwgraph);
            if (argsParser.OptionIsUsed(RunRemoveOverlapsOption)) {
                var compGraph = gwgraph.GeometryGraph;
                GTreeOverlapRemoval.RemoveOverlaps(compGraph.Nodes.ToArray(), gwgraph.LayoutAlgorithmSettings.NodeSeparation);
                
                if (graphViewer.MsaglFileToSave != null) {
                    gwgraph.Write(graphViewer.MsaglFileToSave);
                    System.Diagnostics.Debug.WriteLine("saved into {0}", graphViewer.MsaglFileToSave);
                    Environment.Exit(0);
                }
            }


            graphViewer.Graph = gwgraph;
        }

        void TestGraph(Graph gwgraph) {
            var sg = gwgraph.RootSubgraph;
            foreach (var ssg in sg.AllSubgraphsDepthFirst())
                foreach (var n in ssg.Nodes)
                    if (!gwgraph.NodeMap.ContainsKey(n.Id))
                        throw new InvalidOperationException("a subgraph node does not belong to NodeMap");
        }

        void SetLayoutSettings(Graph gwgraph) {
            bool mdsIsUsed = argsParser.OptionIsUsed(MdsOption);
            if (argsParser.OptionIsUsed(DoNotLayoutOption) && GraphHasGeometry(gwgraph))
                graphViewer.NeedToCalculateLayout = false;  
            if (gwgraph.RootSubgraph != null && gwgraph.RootSubgraph.Subgraphs.Any()) {
                gwgraph.LayoutAlgorithmSettings = new SugiyamaLayoutSettings {
                    FallbackLayoutSettings =
                        new FastIncrementalLayoutSettings {
                            AvoidOverlaps = true
                        },
                };
            }
            else if (mdsIsUsed)
                gwgraph.LayoutAlgorithmSettings = GetMdsLayoutSettings();
            if (argsParser.OptionIsUsed(NodeSeparationOption)) {
                var ns = double.Parse(argsParser.GetStringOptionValue(NodeSeparationOption));
                if (ns != 0)
                    gwgraph.LayoutAlgorithmSettings.NodeSeparation = ns;


            }
            if (argsParser.OptionIsUsed(NoEdgeRoutingOption))
               gwgraph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.None; 

        }

        bool GraphHasGeometry(Graph drawingGraph) {
            return drawingGraph.GeometryGraph != null && drawingGraph.Nodes.All(n => n.GeometryNode != null) &&
                   drawingGraph.Edges.All(e => e.GeometryEdge != null);
        }


        MdsLayoutSettings GetMdsLayoutSettings() {
            var settings = new MdsLayoutSettings();
            settings.ScaleX *= 3;
            settings.ScaleY *= 3;
            settings.EdgeRoutingSettings.KeepOriginalSpline = true;
            settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Spline;
            settings.IterationsWithMajorization = argsParser.OptionIsUsed(NoIterationsWithMajorization) ? 0 : 30;
            if (argsParser.OptionIsUsed(AllowOverlapsInMds))
                settings.RemoveOverlaps = false;
            if (argsParser.OptionIsUsed(SequentialRunOption))
                settings.RunInParallel = false;
            if (argsParser.OptionIsUsed(StraightLineEdgesOption))
                settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.StraightLine;
            if (argsParser.OptionIsUsed(NoEdgeRoutingOption))
                settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.None; 
            return settings;
        }


        

        [STAThread]
        static void Main(string[] args) {
            new App{Args=args}.Run();
        }

        
        public string[] Args { get; set; }
    }
}
