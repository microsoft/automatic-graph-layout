using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using System.Windows.Input;
using Dot2Graph;
using Microsoft.Msagl.Drawing;
using Application = System.Windows.Application;
using Menu = System.Windows.Controls.Menu;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using ModifierKeys = System.Windows.Input.ModifierKeys;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using ToolBar = System.Windows.Controls.ToolBar;
using ToolTip = System.Windows.Controls.ToolTip;
using WindowStartupLocation = System.Windows.WindowStartupLocation;
using WindowState = System.Windows.WindowState;

using xCodeMap.xGraphControl;

namespace xCodeMap {
    class App:Application {
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
        const string PolygonDistanceTestOption3 = "-pd3";
        const string RandomBundlingTest = "-rbt";
        const string TestCdtThreaderOption = "-tth";
        const string AsyncLayoutOption = "-async";

        public static readonly RoutedUICommand OpenFileCommand = new RoutedUICommand("Open File...", "OpenFileCommand",
                                                                                     typeof (App));
        public static readonly RoutedUICommand ReloadCommand = new RoutedUICommand("Reload File...", "ReloadCommand",
                                                                                     typeof(App));
        public static readonly RoutedUICommand HomeViewCommand = new RoutedUICommand("Home view...", "HomeViewCommand",
                                                                                     typeof(App));

        public static readonly RoutedUICommand ScaleNodeDownCommand = new RoutedUICommand("Scale node down...", "ScaleNodeDownCommand",
                                                                                     typeof(App));
        public static readonly RoutedUICommand ScaleNodeUpCommand = new RoutedUICommand("Scale node up...", "ScaleNodeUpCommand",
                                                                                     typeof(App));

        
        Window appWindow;
        DockPanel dockPanel = new DockPanel();
        DockPanel graphViewerPanel = new DockPanel();
        ToolBar toolBar = new ToolBar();
        xCodeMap.xGraphControl.XGraphViewer xGraphViewer = new xCodeMap.xGraphControl.XGraphViewer();
        string lastFileName;
        static global::ArgsParser.ArgsParser argsParser;

        protected override void OnStartup(StartupEventArgs e) {
#if DEBUG
            Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif

            appWindow = new Window { Title = "My app for testing wpf graph control", 
                                     Content = dockPanel, 
                                     WindowStartupLocation = WindowStartupLocation.CenterScreen, 
                                     WindowState = WindowState.Maximized,
                                     BorderBrush = Brushes.Red,
                                     BorderThickness = new Thickness(2)
                                    };
         
            SetupToolbar();
            graphViewerPanel.ClipToBounds = true;

            dockPanel.Children.Add(toolBar);
            dockPanel.Children.Add(graphViewerPanel);
            graphViewerPanel.Children.Add(xGraphViewer.MainPanel);
            xGraphViewer.MainPanel.Loaded += GraphViewerLoaded;
            argsParser = SetArgsParser(Args);

            appWindow.Show();
        }

        void GraphViewerLoaded(object sender, EventArgs e) {
            string fileName = argsParser.GetValueOfOptionWithAfterString(FileOption);
            if (fileName != null)
                CreateAndLayoutGraph(fileName);
        }

        static ArgsParser.ArgsParser SetArgsParser(string [] args) {
            argsParser = new ArgsParser.ArgsParser(args);
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
            argsParser.AddAllowedOptionWithHelpString(PolygonDistanceTestOption3,"test PolygonDistance3");
            argsParser.AddAllowedOptionWithHelpString(RandomBundlingTest,"random bundling test");
            argsParser.AddAllowedOptionWithHelpString(TestCdtThreaderOption,"test CdtThreader");
            argsParser.AddAllowedOptionWithHelpString(AsyncLayoutOption,"test viewer in the async mode");

            if(!argsParser.Parse()) {
                Console.WriteLine(argsParser.UsageString());
                Environment.Exit(1);
            }
            return argsParser;
        }

        void SetupToolbar() {
            SetCommands();
            DockPanel.SetDock(toolBar, Dock.Top);
            SetMainMenu();
        }


        void SetCommands() {
            appWindow.CommandBindings.Add(new CommandBinding(OpenFileCommand, OpenFile));
            appWindow.CommandBindings.Add(new CommandBinding(ReloadCommand, ReloadFile));
            appWindow.CommandBindings.Add(new CommandBinding(HomeViewCommand, (a, b) => xGraphViewer.BringToHomeView()));
            appWindow.InputBindings.Add(new InputBinding(OpenFileCommand, new KeyGesture(Key.O, ModifierKeys.Control)));
            appWindow.InputBindings.Add(new InputBinding(ReloadCommand, new KeyGesture(Key.F5)));
            appWindow.InputBindings.Add(new InputBinding(HomeViewCommand, new KeyGesture(Key.H, ModifierKeys.Control)));



            appWindow.InputBindings.Add(new InputBinding(ScaleNodeDownCommand, new KeyGesture(Key.S, ModifierKeys.Control)));
          //  appWindow.CommandBindings.Add(new CommandBinding(ScaleNodeDownCommand, ScaleNodeDownTest));
            appWindow.CommandBindings.Add(new CommandBinding(ScaleNodeDownCommand, ScaleNodeUpTest));
            appWindow.InputBindings.Add(new InputBinding(ScaleNodeUpCommand, new KeyGesture(Key.S, ModifierKeys.Control|ModifierKeys.Shift)));

        }


        static Point P2(System.Windows.Point p) {
            return new Point(p.X, p.Y);
        }

        void ScaleNodeUpTest(object sender, ExecutedRoutedEventArgs e) {
            throw new NotImplementedException();
        }

        void SetMainMenu() {
            var mainMenu = new Menu {IsMainMenu = true};
            SetFileMenu(mainMenu);
            SetViewMenu(mainMenu);
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
            mainMenu.Items.Add(fileMenu);
            toolBar.Items.Add(mainMenu);
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
                if (fileName != null) {
                    string extension = Path.GetExtension(fileName).ToLower();

                    if (extension == ".dot") {
                        int line, column;
                        string msg;
                        Graph gwgraph = Parser.Parse(fileName, out line, out column, out msg);
                        if (gwgraph != null) {
                            xGraphViewer.Graph = gwgraph;
                            appWindow.Title = fileName;
                            lastFileName = fileName;
                        }
                        else
                            MessageBox.Show(msg);
                        return;
                    }
                    if (extension == ".msagl") {
                        var g = Graph.Read(fileName);
                        if (g != null) {
                            xGraphViewer.Graph = g;
                            appWindow.Title = fileName;
                            lastFileName = fileName;

                        }
                    }
                    if (extension == ".dgml")
                    {
                        Dictionary<DrawingObject,IViewerObjectX> vObjectsMapping;

                        var g = DGMLParser.Parse(fileName, out vObjectsMapping);
                        if (g != null)
                        {
                            xGraphViewer.LoadGraphWithVisuals(g, vObjectsMapping);
                            appWindow.Title = fileName;
                            lastFileName = fileName;
                        }
                    }
                }
            } catch (Exception e) {
                MessageBox.Show(e.ToString(), "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [STAThread]
        static void Main(string[] args) {
            new App{Args=args}.Run();
        }

        public string[] Args { get; set; }
    }
}
