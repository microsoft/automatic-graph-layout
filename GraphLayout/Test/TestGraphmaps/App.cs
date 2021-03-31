using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Dot2Graph;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.GraphmapsWpfControl;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Color = Microsoft.Msagl.Drawing.Color;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using ModifierKeys = System.Windows.Input.ModifierKeys;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using RichTextBox = System.Windows.Controls.RichTextBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Size = System.Windows.Size;
using TextBox = System.Windows.Controls.TextBox;
using Timer = System.Timers.Timer;

namespace TestGraphmaps {
    internal class App : Application {
        const string OneTimeRunOption = "-onerun";

        const string QuietOption = "-quiet";
        const string FileOption = "-file";
        const string BundlingOption = "-bundling";
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
        const string BackgroundColorOption = "-bgcolor";
        const string RailColorsOption = "-railcolors";
        const string IncreaseNodeQuotaOption = "-inq";
        const string SelectionColorsOption = "-selcolors";

        public static readonly RoutedUICommand OpenFileCommand = new RoutedUICommand("Open File...", "OpenFileCommand",
            typeof (App));


        public static readonly RoutedUICommand CancelLayoutCommand = new RoutedUICommand("Cancel Layout...",
            "CancelLayoutCommand",
            typeof (App));

        public static readonly RoutedUICommand ReloadCommand = new RoutedUICommand("Reload File...", "ReloadCommand",
            typeof (App));

        public static readonly RoutedUICommand SaveImageCommand = new RoutedUICommand("Save Image...",
            "SaveImageCommand",
            typeof (App));

        public static readonly RoutedUICommand SaveMsaglCommand = new RoutedUICommand("Save Msagl...",
            "SaveMsaglCommand",
            typeof (App));

        public static readonly RoutedUICommand ExitCommand = new RoutedUICommand("Exit...", "ExitCommand",
            typeof (App));

        public static readonly RoutedUICommand HomeViewCommand = new RoutedUICommand("Home view...", "HomeViewCommand",
            typeof (App));

        public static readonly RoutedUICommand ScaleNodeDownCommand = new RoutedUICommand("Scale node down...",
            "ScaleNodeDownCommand",
            typeof (App));

        public static readonly RoutedUICommand ScaleNodeUpCommand = new RoutedUICommand("Scale node up...",
            "ScaleNodeUpCommand",
            typeof (App));


        AppWindow _appWindow;
        //readonly DockPanel dockPanel = new DockPanel();

        DockPanel _dockPanel;

        //readonly DockPanel graphViewerPanel = new DockPanel();
        DockPanel _graphViewerPanel;

        readonly GraphmapsViewer _graphViewer = new GraphmapsViewer();
        string _lastFileName;
        static ArgsParser.ArgsParser _argsParser;
        TextBox _statusTextBox;
        string _currentFileNameFromList;
        //RangeSlider edgeRangeSlider;

        protected void OnStartupTextBox(StartupEventArgs e) {
            EventManager.RegisterClassHandler(typeof (TextBox), UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(SelectivelyHandleMouseButton), true);
            EventManager.RegisterClassHandler(typeof (TextBox), UIElement.GotKeyboardFocusEvent,
                new RoutedEventHandler(SelectAllText), true);         
        }

        static void SelectivelyHandleMouseButton(object sender, MouseButtonEventArgs e) {
            var textbox = (sender as TextBox);
            if (textbox != null && !textbox.IsKeyboardFocusWithin) {
                if (e.OriginalSource.GetType().Name == "TextBoxView") {
                    e.Handled = true;
                    textbox.Focus();
                }
            }
        }

        static void SelectAllText(object sender, RoutedEventArgs e) {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null) textBox.SelectAll();
        }



        
        protected override void OnStartup(StartupEventArgs e) {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif

            // debug
            //Test1.RunTest9();

            _appWindow = new AppWindow
            {
                Title = "Graphmaps browser",
                Width = SystemParameters.WorkArea.Width - 300,
                Height = SystemParameters.WorkArea.Height,
                GraphViewer=_graphViewer,                
                //Content = dockPanel,
                //WindowStartupLocation = WindowStartupLocation.CenterScreen,
                //WindowState = WindowState.Normal
            };

            _dockPanel = _appWindow.GetMainDockPanel();
            _graphViewerPanel = _appWindow.GetGraphViewerPanel();
            _statusTextBox = _appWindow.GetStatusTextBox();

            //SetupToolbar();
            SetAppCommands();

            //graphViewerPanel.ClipToBounds = true;
            //dockPanel.Children.Add(toolBar);
            //SetUpStatusBar();

            //dockPanel.LastChildFill = true;
            //dockPanel.Children.Add(graphViewerPanel);
            _graphViewer.BindToPanel(_graphViewerPanel);
            _dockPanel.Loaded += GraphViewerLoaded;
            _argsParser = SetArgsParser(Args);

            if (_argsParser.OptionIsUsed(BackgroundColorOption)) {
                var bc = _argsParser.GetStringOptionValue(BackgroundColorOption);
                _graphViewerPanel.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(bc));
            }

            //graphViewer.MainPanel.MouseLeftButtonUp += TestApi;
            TrySettingGraphViewerLargeLayoutThresholdAndSomeOtherLgSettings();
            if (_argsParser.OptionIsUsed(ExitAfterLgLayoutOption)) {
                _graphViewer.DefaultLargeLayoutSettings.ExitAfterInit = true;
            }

            _graphViewer.ViewChangeEvent += GraphViewerViewChangeEvent;
            _graphViewer.ObjectUnderMouseCursorChanged += GvObjectUnderMouseCursorChanged;
            _graphViewer.MouseDown += GraphViewerMouseDown;
            _graphViewer.MouseMove += GraphViewerMouseMove;


            _graphViewer.GraphChanged += graphViewer_GraphChanged;
            //graphViewer.LayoutEditingEnabled = false;
            OnStartupTextBox(e);
            base.OnStartup(e);
            _appWindow.Show();
//SetUpAndShowSideWindow();
        }

        

        void GraphViewerMouseMove(object sender, MsaglMouseEventArgs e) {
            SetStatusText();
        }



        void GvObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e) {
            SetStatusText();
        }

        void GraphViewerMouseDown(object sender, MsaglMouseEventArgs e) {
//            if (e.RightButtonIsPressed) {
//                PopupMenus(CreatePopupMenu());
//                e.Handled = true;
//            }
        }

        public static object CreateMenuItem(string title, VoidDelegate voidVoidDelegate) {
            var menuItem = new MenuItem {Header = title};
            menuItem.Click += (RoutedEventHandler) (delegate { voidVoidDelegate(); });
            return menuItem;
        }

        public void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems) {
            var contextMenu = new ContextMenu();
            foreach (var pair in menuItems)
                contextMenu.Items.Add(CreateMenuItem(pair.Item1, pair.Item2));
            contextMenu.Closed += ContextMenuClosed;
            ContextMenuService.SetContextMenu(_graphViewer.GraphCanvas, contextMenu);

        }

        void ContextMenuClosed(object sender, RoutedEventArgs e) {
            ContextMenuService.SetContextMenu(_graphViewer.GraphCanvas, null);
        }



        void graphViewer_GraphChanged(object sender, EventArgs e) {
            _appWindow.Title = (_lastFileName ?? "") +
                              String.Format(" ({0} nodes, {1} edges)", _graphViewer.Graph.NodeCount,
                                  _graphViewer.Graph.EdgeCount);

        }

        void GraphViewerViewChangeEvent(object sender, EventArgs e) {
            SetStatusText();
        }

        void SetStatusText() {

            var location = Mouse.GetPosition(_graphViewer.GraphCanvas);

            _statusTextBox.Text = String.Format("scale {0}, Mouse = ({1},{2}) children = {3} drawing object = {4} vr={5}",
                _graphViewer.ZoomFactor,
                location.X.ToString("N3"),
                location.Y.ToString("N3"), 
                _graphViewer.VisibleChildrenCount,
                _graphViewer.DrawingChildrenCount,
                _graphViewer.VisRailCount);
        }


        void TrySettingGraphViewerLargeLayoutThresholdAndSomeOtherLgSettings() {
            if (_argsParser.OptionIsUsed("-no_route_simpl")) {
                _graphViewer.DefaultLargeLayoutSettings.SimplifyRoutes = false;
            }
            if (_argsParser.OptionIsUsed("-no_tiles"))
                _graphViewer.DefaultLargeLayoutSettings.GenerateTiles = false;
            string labelH = _argsParser.GetStringOptionValue("-labelH");
            if (labelH != null) {
                double h;
                if (double.TryParse(labelH, out h)) {
                    _graphViewer.DefaultLargeLayoutSettings.NodeLabelHeightInInches = h;
                }
            }
            CheckNodeQuota();
            CheckRailQuota();
            CheckRailColors();
            CheckSelectionColors();
            CheckIncreaseNodeQuota();
        }

        void CheckRailColors() {
            string railColors = _argsParser.GetStringOptionValue(RailColorsOption);
            if (railColors != null)
            {
                _graphViewer.DefaultLargeLayoutSettings.RailColors = railColors.Split(',');
            }
        }

        void CheckSelectionColors() {
            string selColors = _argsParser.GetStringOptionValue(SelectionColorsOption);
            if (selColors != null) {
                _graphViewer.DefaultLargeLayoutSettings.SelectionColors = selColors.Split(',');
            }
        }

        void CheckRailQuota() {
            string railQuota = _argsParser.GetStringOptionValue("-rt");
            if (railQuota != null) {
                int n;
                if (Int32.TryParse(railQuota, out n))
                    _graphViewer.DefaultLargeLayoutSettings.MaxNumberOfRailsPerTile = n;
                else
                    System.Diagnostics.Debug.WriteLine("cannot parse {0}", railQuota);
            }
        }
        void CheckNodeQuota()
        {
            string nodeQuota = _argsParser.GetStringOptionValue(NodeQuotaOption);
            if (nodeQuota != null)
            {
                int n;
                if (Int32.TryParse(nodeQuota, out n))
                    _graphViewer.DefaultLargeLayoutSettings.MaxNumberOfNodesPerTile = n;
                else
                    System.Diagnostics.Debug.WriteLine("cannot parse {0}", nodeQuota);
            }
        }
        void CheckIncreaseNodeQuota() {
            string incrNodeQuota = _argsParser.GetStringOptionValue(IncreaseNodeQuotaOption);
            if (incrNodeQuota != null) {
                double inq;
                if (Double.TryParse(incrNodeQuota, out inq))
                    _graphViewer.DefaultLargeLayoutSettings.IncreaseNodeQuota = inq;
                else
                    System.Diagnostics.Debug.WriteLine("cannot parse {0}", incrNodeQuota);
            }
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
            string fileName = _argsParser.GetStringOptionValue(FileOption);
            if (fileName != null)
                CreateAndLayoutGraph(fileName);
            else {
                string fileList = _argsParser.GetStringOptionValue(FileListOption);
                if (fileList != null)
                    ProcessFileList(fileList);
            }
        }

        void ProcessFileList(string fileList) {
            try {
               
                var fileListDir = Path.GetDirectoryName(fileList);
                var streamReader = new StreamReader(fileList);
//                _graphViewer.LayoutStarted +=
//                    (a, b) => System.Diagnostics.Debug.WriteLine("processing {0}", _currentFileNameFromList);
//                _graphViewer.LayoutComplete += (a, b) =>
//                    {
//                        System.Diagnostics.Debug.WriteLine("Done with {0}", _currentFileNameFromList);
//                        if (!SetupNextRun(streamReader, fileListDir)) {
//                            System.Diagnostics.Debug.WriteLine("done with the list");
//                        }
//                    };

                do {
                    _currentFileNameFromList = ReadNextFileName(streamReader, fileListDir);
                    CreateAndLayoutGraph(_currentFileNameFromList);
                } while (_currentFileNameFromList != null);
            }
            catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        string ReadNextFileName(StreamReader sr, string fileListDir) {
            var fn = sr.ReadLine();
            if (fn == null)
                return null;
            return Path.Combine(fileListDir, fn.ToLower());
        }


        static ArgsParser.ArgsParser SetArgsParser(string[] args) {
            _argsParser = new ArgsParser.ArgsParser(args);
            _argsParser.AddAllowedOptionWithHelpString(OneTimeRunOption, "loads only one graph");
            _argsParser.AddAllowedOptionWithHelpString(SequentialRunOption, "no threads");
            _argsParser.AddAllowedOptionWithHelpString(StraightLineEdgesOption, "route straight line edges");
            _argsParser.AddAllowedOptionWithHelpString(NoEdgeRoutingOption, "don't route the edges");
            _argsParser.AddAllowedOptionWithHelpString(NoIterationsWithMajorization, "0 iterations with majorization");
            _argsParser.AddOptionWithAfterStringWithHelp(FileListOption, "file list");
            _argsParser.AddAllowedOptionWithHelpString(RoundedCornersOption, "rounded corners for boxes always");
            _argsParser.AddAllowedOptionWithHelpString(PrintMaxNodeDegreeOption, "print max node degree and exit");
            _argsParser.AddOptionWithAfterStringWithHelp(NodeSeparationOption, "node separation");
            _argsParser.AddOptionWithAfterStringWithHelp(NodeQuotaOption, "max number of nodes per tile");
            _argsParser.AddOptionWithAfterStringWithHelp(IncreaseNodeQuotaOption, "increase max number of nodes per tile for higher levels");
            _argsParser.AddOptionWithAfterStringWithHelp("-rt", "max number of rails per tile");
            _argsParser.AddAllowedOption(AllowOverlapsInMds);
            _argsParser.AddAllowedOption(RunRemoveOverlapsOption);

            _argsParser.AddAllowedOptionWithHelpString(EdgeZoomLevelsUpperBoundOption,
                "use upper bound in the edge zoom level algorithm");
            _argsParser.AddOptionWithAfterStringWithHelp(LargeLayoutThresholdOption, "sets the large layout threshold");
            _argsParser.AddOptionWithAfterStringWithHelp(BackgroundImageOption,
                "sets the background image for the large layout");
            _argsParser.AddOptionWithAfterStringWithHelp(BackgroundColorOption,
    "sets the background color for the large layout viewer");
            _argsParser.AddOptionWithAfterStringWithHelp(RailColorsOption,
"sets the rail colors for the large layout viewer");
            _argsParser.AddOptionWithAfterStringWithHelp(SelectionColorsOption,
"sets the selected rail colors for the large layout viewer");

            _argsParser.AddOptionWithAfterStringWithHelp(MaxNodesPerTileOption,
                "sets the max nodes per tile for large layout");
            _argsParser.AddAllowedOptionWithHelpString(DoNotLayoutOption, "do not call the layout calculation");
            _argsParser.AddAllowedOption(RecoverSugiyamaTestOption);
            _argsParser.AddAllowedOption(QuietOption);
            _argsParser.AddAllowedOption(BundlingOption);
            _argsParser.AddOptionWithAfterStringWithHelp(FileOption, "the name of the input file");
            _argsParser.AddAllowedOptionWithHelpString(TestCdtOption, "testing Constrained Delaunay Triangulation");
            _argsParser.AddAllowedOptionWithHelpString(TestCdtOption0,
                "testing Constrained Delaunay Triangulation on a small graph");
            _argsParser.AddAllowedOptionWithHelpString(TestCdtOption1, "testing threading through a CDT");
            _argsParser.AddAllowedOptionWithHelpString(TestCdtOption2,
                "testing Constrained Delaunay Triangulation on file \'polys\'");
            _argsParser.AddAllowedOptionWithHelpString(ReverseXOption, "reversing X coordinate");
            _argsParser.AddOptionWithAfterStringWithHelp(EdgeSeparationOption, "use specified edge separation");
            _argsParser.AddAllowedOptionWithHelpString(MdsOption, "use mds layout");
            _argsParser.AddAllowedOptionWithHelpString(FdOption, "use force directed layout");
            _argsParser.AddAllowedOptionWithHelpString(ConstraintsTestOption, "test constraints");
            _argsParser.AddOptionWithAfterStringWithHelp(InkImportanceOption, "ink importance coefficient");
            _argsParser.AddOptionWithAfterStringWithHelp(TightPaddingOption, "tight padding coefficient");
            _argsParser.AddOptionWithAfterStringWithHelp(LoosePaddingOption, "loose padding coefficient");
            _argsParser.AddOptionWithAfterStringWithHelp(CapacityCoeffOption, "capacity coeffiecient");
            _argsParser.AddAllowedOptionWithHelpString(PolygonDistanceTestOption, "test Polygon.Distance");
            _argsParser.AddAllowedOptionWithHelpString(RandomBundlingTest, "random bundling test");
            _argsParser.AddAllowedOptionWithHelpString(TestCdtThreaderOption, "test CdtThreader");
            _argsParser.AddAllowedOptionWithHelpString(AsyncLayoutOption, "test viewer in the async mode");
            _argsParser.AddAllowedOptionWithHelpString(EnlargeHighDegreeNodes, "enlarge high degree nodes");
            _argsParser.AddAllowedOptionWithHelpString(ExitAfterLgLayoutOption, "exit after lg calculation");
            _argsParser.AddAllowedOptionWithHelpString("-no_route_simpl", "do not simplify the routes");
            _argsParser.AddAllowedOptionWithHelpString("-no_tiles", "do not generate or load tiles");
            _argsParser.AddOptionWithAfterStringWithHelp("-labelH", "the height of labels");

            if (!_argsParser.Parse()) {
                System.Diagnostics.Debug.WriteLine(_argsParser.UsageString());
                Environment.Exit(1);
            }
            return _argsParser;
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


        void SetAppCommands() {
            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.SaveMsaglCommand, SaveMsagl));
            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.ExitCommand, ExitHandler));
            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.OpenFileCommand, OpenFile));
            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.CancelLayoutCommand,
                (a, b) => { _graphViewer.CancelToken.Canceled = true; }));

            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.ReloadCommand, ReloadFile));
            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.HomeViewCommand,
                (a, b) => _graphViewer.SetInitialTransform()));
            _appWindow.InputBindings.Add(new InputBinding(AppCommands.OpenFileCommand,
                new KeyGesture(Key.O, ModifierKeys.Control)));
            _appWindow.InputBindings.Add(new InputBinding(AppCommands.ReloadCommand, new KeyGesture(Key.F5)));
            _appWindow.InputBindings.Add(new InputBinding(AppCommands.HomeViewCommand,
                new KeyGesture(Key.H, ModifierKeys.Control)));
            _appWindow.InputBindings.Add(new InputBinding(AppCommands.CancelLayoutCommand,
                new KeyGesture(Key.C, ModifierKeys.Control)));
            _appWindow.InputBindings.Add(new InputBinding(AppCommands.ExitCommand,
                new KeyGesture(Key.F4, ModifierKeys.Alt)));


            //test
            
            _appWindow.CommandBindings.Add(
                new CommandBinding(AppCommands.RouteEdgesOnSkeletonTryKeepingOldTrajectoriesCommand,
                    RouteEdgesOnSkeletonTryKeepingOldTrajectories));
            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.SimplifyRoutesCommand, SimplifyRoutes));
            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.RunMdsCommand, RunMds));
            
            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.GenerateTilesCommand, SaveTilesToDisk));
            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.ShowVisibleChildrenCountCommand,
                ShowVisibleChildrenCount));

        
            
            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.SelectAllNodesOnVisibleLevelsCommand,
                SelectAllNodesOnVisibleLevels));
            _appWindow.InputBindings.Add(new InputBinding(AppCommands.SelectAllNodesOnVisibleLevelsCommand,
                new KeyGesture(Key.A, ModifierKeys.Control)));

            _appWindow.InputBindings.Add(new InputBinding(AppCommands.ScaleNodeDownCommand,
                new KeyGesture(Key.S, ModifierKeys.Control)));
            //  appWindow.CommandBindings.Add(new CommandBinding(ScaleNodeDownCommand, ScaleNodeDownTest));
            _appWindow.CommandBindings.Add(new CommandBinding(AppCommands.ScaleNodeDownCommand, ScaleNodeUpTest));
            _appWindow.InputBindings.Add(new InputBinding(AppCommands.ScaleNodeUpCommand,
                new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)));
        }

        void SaveTilesToDisk(object sender, ExecutedRoutedEventArgs e) {
            _graphViewer.GenerateTiles();
        }

        void ShowVisibleChildrenCount(object sender, ExecutedRoutedEventArgs e) {
            MessageBox.Show(String.Format("visible children count = {0}", _graphViewer.VisibleChildrenCount));
        }


        void RunMds(object sender, ExecutedRoutedEventArgs e) {
            LgLayoutSettings lgSettings;
            if (!GetLgSettings(out lgSettings)) return;
            lgSettings.Interactor.RunMds();
        }

        void SimplifyRoutes(object sender, ExecutedRoutedEventArgs e) {
            LgLayoutSettings lgSettings;
            int iLevel;
            if (!GetLgSettingsAndActiveLevel(out lgSettings, out iLevel)) return;
            lgSettings.Interactor.SimplifyRoutes(iLevel);
        }


        
        
        
        
        void RouteEdgesOnSkeletonTryKeepingOldTrajectories(object sender, ExecutedRoutedEventArgs e) {
            LgLayoutSettings lgSettings;
            int iLevel;
            if (!GetLgSettingsAndActiveSkeletonLevel(out lgSettings, out iLevel)) return;
            lgSettings.Interactor.RouteEdges(iLevel);
        }

        void SelectAllNodesOnVisibleLevels(object sender, ExecutedRoutedEventArgs e) {
            _graphViewer.SelectAllNodesOnVisibleLevels();
        }

        bool GetLgSettings(out LgLayoutSettings settings) {
            settings = null;
            if (_graphViewer == null) return false;
            if (_graphViewer.Graph == null) return false;
            if (_graphViewer.Graph.LayoutAlgorithmSettings == null) return false;
            settings = _graphViewer.Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            return settings != null;
        }

        bool GetLgSettingsAndActiveSkeletonLevel(out LgLayoutSettings settings, out int iLevel) {
            settings = _graphViewer.Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            iLevel = 0;
            return (settings != null && iLevel >= 0);
        }

        bool GetLgSettingsAndActiveLevel(out LgLayoutSettings settings, out int iLevel) {
            settings = _graphViewer.Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            iLevel = 0;
            return (settings != null && iLevel >= 0);
        }


        
        void SaveMsagl(object sender, ExecutedRoutedEventArgs e) {
            var dlg = new SaveFileDialog
            {
                FileName = _lastFileName + ".msagl",
                DefaultExt = ".msagl",
                Filter = "msagl files (.msagl)|*.msagl"
            };

            bool? result = dlg.ShowDialog();

            if (result == true) {
                _graphViewer.Graph.Write(dlg.FileName);
            }
        }

        void ExitHandler(object sender, ExecutedRoutedEventArgs e) {
            Environment.Exit(0);
        }


        void ScaleNodeUpTest(object sender, ExecutedRoutedEventArgs e) {
            throw new NotImplementedException();
        }


        void ReloadFile(object sender, ExecutedRoutedEventArgs e) {
            if (_lastFileName != null)
                CreateAndLayoutGraph(_lastFileName);
        }

        
        void OpenFile(object sender, ExecutedRoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog
            {
                RestoreDirectory = true,
            };
            if (openFileDialog.ShowDialog() == true) {
                CreateAndLayoutGraph(openFileDialog.FileName);
            }
        }

        void CreateAndLayoutGraph(string fileName) {
            try {
                if (fileName == null) return;
                string extension = Path.GetExtension(fileName).ToLower();
                _lastFileName = fileName;
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
                        return;
                    default:
                        System.Diagnostics.Debug.WriteLine("format {0} is not supported, cannot process {1}", extension, fileName);
                        return;
                }
                SaveMsaglAndTiles(fileName);
            }
            catch (Exception e) {
                MessageBox.Show(e.ToString(), "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void ProcessGraphml(string fileName) {
            var parser = new GraphmlParser(fileName);
            Graph graph = parser.Parse();
            PassGraphToGraphViewer(graph, fileName);
        }

        void ProcessMsagl(string fileName) {
            System.Diagnostics.Debug.WriteLine("reading {0}", fileName);
            var graph = Graph.Read(fileName);

            if (graph != null) {
                if (_argsParser.OptionIsUsed(PrintMaxNodeDegreeOption)) {
                    System.Diagnostics.Debug.WriteLine("max node degree {0}",
                        graph.Nodes.Max(n => n.OutEdges.Count() + n.InEdges.Count() + n.SelfEdges.Count()));
                    Environment.Exit(0);
                }


                if (_argsParser.OptionIsUsed(RoundedCornersOption))
                    foreach (var n in graph.Nodes)
                        n.Attr.XRadius = n.Attr.YRadius = 3;

                if (_argsParser.OptionIsUsed(RunRemoveOverlapsOption)) {
                    GTreeOverlapRemoval.RemoveOverlaps(graph.GeometryGraph.Nodes.ToArray(),
                        graph.LayoutAlgorithmSettings.NodeSeparation);
                }
            }
            System.Diagnostics.Debug.WriteLine("passing graph to the control");
            PassGraphToControl(graph, fileName);
        }

        void PassGraphToControl(Graph graph, string graphFileName) {
            var oldVal = _graphViewer.NeedToCalculateLayout;
            CreateTileDirectoryName(graphFileName);
            _graphViewer.NeedToCalculateLayout = false;
            _graphViewer.LayoutComplete += (a, b) => _graphViewer.NeedToCalculateLayout = oldVal;
            _graphViewer.Graph = graph;
            if (_graphViewer.DefaultLargeLayoutSettings.GenerateTiles)
                _graphViewer.InitTiles();
        }



        void CreateTileDirectoryName(string graphFileName) {
            var fileNameWithoutExtension = FileNameWithoutExtension(graphFileName);
            _graphViewer.TileDirectory =  fileNameWithoutExtension + ".tiles";
        }

        static string FileNameWithoutExtension(string graphFileName) {
            int extensionStart = graphFileName.LastIndexOf(".");
            string fileNameWithoutExtension = extensionStart == -1 ? graphFileName : graphFileName.Substring(0, extensionStart);
            return fileNameWithoutExtension;
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
                
                _graphViewer.NeedToCalculateLayout = !layoutExist;
                _graphViewer.Graph = dgraph;
                _graphViewer.NeedToCalculateLayout = true;
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
                _graphViewer.Graph = gwgraph;
            }
            else
                MessageBox.Show(msg + String.Format(" line {0} column {1}", line, column));
        }

        void ProcessDgml(string fileName) {
#if TEST_MSAGL
            Graph gwgraph = DgmlParser.DgmlParser.Parse(fileName);
            if (gwgraph != null) {
                SetLayoutSettings(gwgraph);
                _graphViewer.Graph = gwgraph;
            }
            else
#endif
                MessageBox.Show("cannot load " + fileName);
        }

        void ProcessGml(string fileName) {
            Graph gwgraph = GmlParser.Parse(fileName);
            if (gwgraph != null) {
                SetLayoutSettings(gwgraph);
                _graphViewer.Graph = gwgraph;
            }
            else
                MessageBox.Show("cannot load " + fileName);
        }

        bool ProcessDot(string fileName)
        {
            int line, column;
            string msg;
            Graph gwgraph = Parser.Parse(fileName, out line, out column, out msg);

            Debug.Assert(NodeMapOfGraphIsOk(gwgraph));

            if (gwgraph != null && (gwgraph.NodeCount > 0 || gwgraph.EdgeCount > 0))
            {
                PassGraphToGraphViewer(gwgraph, fileName);
                return true;
            }
            System.Diagnostics.Debug.WriteLine("Cannot parse {3} {2} line {0} column {1}", line, column, msg, fileName);
            return false;
        }

        void SaveMsaglAndTiles(string fileName)
        {
            if (_graphViewer.Graph == null) return;
            string rootName = FileNameWithoutExtension(fileName);
            string msaglFileName = rootName + ".msagl";
            System.Diagnostics.Debug.WriteLine("saving to {0}", msaglFileName);
            _graphViewer.Graph.Write(msaglFileName);
            if (_graphViewer.DefaultLargeLayoutSettings.GenerateTiles) {
                CreateTileDirectoryName(fileName);
                _graphViewer.GenerateTiles();
            }
        }

/*
        bool OkToCreateOrOverwriteMsaglFile(string fileName) {
            string msaglFileName = CreateMsaglFileNameFromDotName(fileName);
            System.Diagnostics.Debug.WriteLine(msaglFileName);
            if (File.Exists(msaglFileName)) {
                string message = String.Format("Do you want to overwrite {0}?", msaglFileName);
                MessageBoxResult result = MessageBox.Show(message, "confirm overwrite", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes) {
                    return true;
                }
                if (result == MessageBoxResult.No) {
                    return false;
                }
                return false;
            }
            return true;
        }
*/

        void PassGraphToGraphViewer(Graph gwgraph, string fileName) {
            CreateTileDirectoryName(fileName);
            FixRoundCorners(gwgraph);
            SetLayoutSettings(gwgraph);
            if (_argsParser.OptionIsUsed(RunRemoveOverlapsOption)) {
                var compGraph = gwgraph.GeometryGraph;
                        GTreeOverlapRemoval.RemoveOverlaps(compGraph.Nodes.ToArray(),
                            gwgraph.LayoutAlgorithmSettings.NodeSeparation);
                
            }
            _graphViewer.Graph = gwgraph;
        }

        static void FixRoundCorners(Graph gwgraph) {
            if (_argsParser.OptionIsUsed(RoundedCornersOption))
                foreach (var n in gwgraph.Nodes) {
                    n.Attr.Shape = Shape.Box;
                    n.Attr.XRadius = n.Attr.YRadius = 3;
                }
        }

        bool NodeMapOfGraphIsOk(Graph gwgraph) {
            if (gwgraph == null) return true;
            var sg = gwgraph.RootSubgraph;
            foreach (var ssg in sg.AllSubgraphsDepthFirst())
                foreach (var n in ssg.Nodes)
                    if (!gwgraph.NodeMap.ContainsKey(n.Id))
                        return false;
            return true;
        }

        void SetLayoutSettings(Graph gwgraph) {
            bool mdsIsUsed = _argsParser.OptionIsUsed(MdsOption);
            if (_argsParser.OptionIsUsed(DoNotLayoutOption) && GraphHasGeometry(gwgraph))
                _graphViewer.NeedToCalculateLayout = false;
            if (gwgraph.RootSubgraph != null && gwgraph.RootSubgraph.Subgraphs.Any()) {
                gwgraph.LayoutAlgorithmSettings = new SugiyamaLayoutSettings
                {
                    FallbackLayoutSettings =
                        new FastIncrementalLayoutSettings
                        {
                            AvoidOverlaps = true
                        },
                };
            }
            else if (mdsIsUsed)
                gwgraph.LayoutAlgorithmSettings = GetMdsLayoutSettings();
            if (_argsParser.OptionIsUsed(NodeSeparationOption)) {
                var ns = double.Parse(_argsParser.GetStringOptionValue(NodeSeparationOption));
                if (ns != 0)
                    gwgraph.LayoutAlgorithmSettings.NodeSeparation = ns;
            }
            if (_argsParser.OptionIsUsed(NoEdgeRoutingOption))
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
            settings.IterationsWithMajorization = _argsParser.OptionIsUsed(NoIterationsWithMajorization) ? 0 : 30;
            if (_argsParser.OptionIsUsed(AllowOverlapsInMds))
                settings.RemoveOverlaps = false;
            if (_argsParser.OptionIsUsed(SequentialRunOption))
                settings.RunInParallel = false;
            if (_argsParser.OptionIsUsed(StraightLineEdgesOption))
                settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.StraightLine;
            if (_argsParser.OptionIsUsed(NoEdgeRoutingOption))
                settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.None;
            //settings.AdjustScale = true;
            return settings;
        }


        [STAThread]
        static void Main(string[] args) {
            new App {Args = args}.Run();
        }


        public string[] Args { get; set; }
        
    }
}
