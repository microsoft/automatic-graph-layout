using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using Dot2Graph;
using Microsoft.Msagl;
using Microsoft.Msagl.ControlForWpfObsolete;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Routing;
using Microsoft.Win32;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace TestForAvalon
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1
    {
        // Details details;
        // EdgeDetails edgeDetails;
       
        // commands
        public static readonly RoutedUICommand NewFileCommand;
        public static readonly RoutedUICommand ReloadFileCommand;
        public static readonly RoutedUICommand OpenFileCommand;
        public static readonly RoutedUICommand SaveAsFileCommand;
        public static readonly RoutedUICommand ExitCommand;
        readonly List<string> filesToLayout = new List<string>();
        string currentFile;
        Diagram diagram;
        LayerDirection direction = LayerDirection.TB;
        GraphScroller graphScroller;
        int offsetInFiles;
        Image panImage;
        bool quietSlider;
        Image redoDisabledImage;
        Image redoImage;
        Image uncheckedPanImage;
        Image uncheckedWindowZoomImage;
        Image undoDisabledImage;
        Image undoImage;
        Image windowZoomWindowImage;
        const double ZoomSliderBase = 1.1;

        static Window1()
        {
            NewFileCommand = new RoutedUICommand("New File...", "NewFileCommand", typeof(Window1));
            ReloadFileCommand = new RoutedUICommand("Reload", "ReloadFileCommand", typeof(Window1));
            OpenFileCommand = new RoutedUICommand("Open File...", "OpenFileCommand", typeof(Window1));
            SaveAsFileCommand = new RoutedUICommand("Save File...", "SaveAsFileCommand", typeof(Window1));
            ExitCommand = new RoutedUICommand("Exit", "ExitCommand", typeof(Window1));
        }

        //string filename;
        public Window1()
        {
            // moved to XAML.
            //CommandBinding binding = new CommandBinding(OpenFileCommand);
            //binding.Executed += new ExecutedRoutedEventHandler(this.OpenFile);
            //this.CommandBindings.Add(binding);

            InitializeComponent();
            Width = 2 * SystemParameters.FullPrimaryScreenWidth / 3;
            Height = 2 * SystemParameters.FullPrimaryScreenHeight / 3;

            string fileName = null;
            foreach (string s in MyApp.Args)
            {
                if (s.StartsWith("-f"))
                    fileName = s.Substring(2);
                else if (s.StartsWith("-log:"))
                {
                    s.Substring(5);
                }
            }

            // if (verbose)
            //    Microsoft.Msagl.Layout.Reporting = true;

            // if (logFileName != null)
            // {
            //    Microsoft.Msagl.Layout.LogFileName = logFileName;
            //    Microsoft.Msagl.Layout.Reporting = true;
            // }

            if (fileName != null)
                ProcessFile(fileName);

            Closing += Window_Closing;
            //            this.graphScroller.MouseDown += new EventHandler<Microsoft.Msagl.Drawing.MsaglMouseEventArgs>(graphScroller_MouseDown);
            graphScroller.Background = Brushes.White;
            HookUpToUndoEnableEvents();
            CreateImages();
            undoButton.IsEnabled = false;
            redoButton.IsEnabled = false;
            undoButton.Content = undoDisabledImage;
            redoButton.Content = redoDisabledImage;
            panButton.Content = uncheckedPanImage;
            windowZoomButton.Content = uncheckedWindowZoomImage;
            graphScroller.MouseDown += GraphScrollerMouseDown;
            graphScroller.MouseMove += GraphScrollerMouseMove;
            graphScroller.MouseUp += GraphScrollerMouseUp;
            (graphScroller as IViewer).GraphChanged += Window1GraphChanged;
            graphScroller.Loaded += GraphScrollerLoaded;
        }

        void GraphScrollerLoaded(object sender, RoutedEventArgs e)
        {
            AnalyseArgs(MyApp.Args);
        }

        void AnalyseArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var s = args[i].ToLower();
                switch (s)
                {
                    case "-p":
                        if (i > args.Length - 1)
                        {
                            MessageBox.Show("missing file name");
                            Environment.Exit(1);
                        }
                        i++;
                        CreateAndLayoutGraph(args[i]);
                        break;
                }
                Console.WriteLine(args[i]);
            }
        }

        void Window1GraphChanged(object sender, EventArgs e)
        {
            if (polygonManager != null)
                polygonManager.ClearGroups();
        }

        void GraphScrollerMouseUp(object sender, MsaglMouseEventArgs e)
        {
        }

        void GraphScrollerMouseMove(object sender, MsaglMouseEventArgs e)
        {
            if (!e.LeftButtonIsPressed) return;
        }


        PolygonManager polygonManager;
        private Animator animator;

        void GraphScrollerMouseDown(object sender, MsaglMouseEventArgs e)
        {
            if (!e.LeftButtonIsPressed) return;

        }


        void CreateImages()
        {
            undoImage = CreateImage("undo");
            undoDisabledImage = CreateImage("disabledUndo");
            redoDisabledImage = CreateImage("disabledRedo");
            redoImage = CreateImage("redo");
            windowZoomWindowImage = CreateImage("zoomwindow");
            uncheckedWindowZoomImage = CreateImage("uncheckedZoomWindow");
            panImage = CreateImage("HAND");
            uncheckedPanImage = CreateImage("uncheckedPan");
        }

        static Image CreateImage(string key)
        {
            var image = new Image
            {
                Source = GetImage("pack://application:,,,/data/" + key + ".bmp")
            };
            return image;
        }

        void HookUpToUndoEnableEvents()
        {
            graphScroller.ChangeInUndo += GraphScrollerChangeInUndo;
        }

        void GraphScrollerChangeInUndo(object sender, EventArgs e)
        {
            if (undoButton.IsEnabled != graphScroller.CanUndo())
            {
                undoButton.IsEnabled = graphScroller.CanUndo();
                undoButton.Content = undoButton.IsEnabled ? undoImage : undoDisabledImage;
            }

            if (redoButton.IsEnabled != graphScroller.CanRedo())
            {
                redoButton.IsEnabled = graphScroller.CanRedo();
                redoButton.Content = redoButton.IsEnabled ? redoImage : redoDisabledImage;
            }
        }

        static void Window_Closing(object sender, CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        void Exit(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            Close();
        }

        void ProcessFile(string fileName)
        {
            //drawingLayoutEditor.Clear();
            var sr = new StreamReader(fileName);
            string l;
            while ((l = sr.ReadLine()) != null)
            {
                l = l.ToLower();
                if (l.EndsWith(".msagl"))
                {
                    l = Path.Combine(Path.GetDirectoryName(fileName), l);
                    filesToLayout.Add(l);
                }
            }
            sr.Close();
            CreateAndLayoutGraph(filesToLayout[offsetInFiles++]);
            diagram.InvalidateVisual();
        }

        void DockPanelInitialized(object sender, EventArgs e)
        {
#if DEBUGGLEE
      Microsoft.Msagl.Layout.Show = new Microsoft.Msagl.Show(Microsoft.Msagl.CommonTest.DisplayGeometryGraph.ShowCurves);
#endif
            zoomSlider.Value = zoomSlider.Minimum;
            zoomSlider.LostMouseCapture += ZoomSliderLostMouseCapture;

            graphScroller = new GraphScroller();
            graphScroller.ViewingSettingsChangedEvent += GraphScrollerViewingSettingsChangedEvent;

            diagram = graphScroller.Diagram;
            diagram.Background = Brushes.AliceBlue;
            diagram.LayoutComplete += DiagramLayoutComplete;

            holder.Children.Add(graphScroller);
            holder.ClipToBounds = true;

            zoomSlider.ValueChanged += ZoomSliderValueChanged;
            zoomSlider.Minimum = 0;
            zoomSlider.Maximum = 100;
            zoomSlider.Value = zoomSlider.Minimum;


            graphScroller.BackwardEnabledChanged += GraphScrollerBackwardEnabledChanged;
            graphScroller.ForwardEnabledChanged += GraphScrollerForwardEnabledChanged;
            forwardButton.Click += ForwardButtonClick;
            backwardButton.Click += BackwardButtonClick;
            graphScroller.MouseDraggingMode = MouseDraggingMode.LayoutEditing;
            //    this.windowZoomButton.Click += new RoutedEventHandler(windowZoomButton_Click);
            panButton.Click += PanButtonClick;
            windowZoomButton.ToolTip = "Set window zoom mode";
            panButton.ToolTip = "Set pan mode";

            //this.diagram.SelectionChanged += new SelectionChangedEventHandler(
            //    delegate
            //    {
            //        NodeShape ns = diagram.SelectedObject as NodeShape;

            //        if (ns != null)
            //        {
            //            NodeInfo ni = (NodeInfo)ns.Node.UserData;
            //            if (ni != null) {
            //                SubGraph bucket = ni.Contents as SubGraph;

            //                if (bucket != null) {
            //                    if (details == null) {
            //                        details = new Details();
            //                    }

            //                    details.Title = "Details for " + ni.Name;
            //                    details.aspectRatioSlider.Value = this.aspectRatioSlider.Value;
            //                    details.Show();
            //                    details.TakeGraph(bucket.SubBuilder.MakeGraph(MpdGraphType.Class));
            //                }
            //            }
            //        }                   
            //    }
            //    );
            //this.diagram.EdgeSelectionChanged += new SelectionChangedEventHandler(
            //    delegate
            //    {
            //        EdgeShape es = diagram.SelectedEdge as EdgeShape;

            //        if (es != null)
            //        {
            //            EdgeInfo edgeInfo = es.Edge.UserData as EdgeInfo;

            //            if (edgeInfo != null)
            //            {
            //                if (edgeDetails == null)
            //                {
            //                    edgeDetails = new EdgeDetails();
            //                }

            //                edgeDetails.Title = "Details for Edge from " + es.Edge.Source + " to " + es.Edge.Target;
            //                edgeDetails.TakeEdge(es.Edge);
            //                edgeDetails.Show();
            //            }
            //        }
            //    }
            //    );
        }


        void GraphScrollerViewingSettingsChangedEvent(object sender, EventArgs e)
        {
            QuietZoomSliderUpdate();
            if (!zoomSlider.IsMouseCaptureWithin)
            {
                graphScroller.PushViewState();
            }
        }


        void ZoomSliderLostMouseCapture(object sender, MouseEventArgs e)
        {
            graphScroller.PushViewState();
        }

        void PanButtonClick(object sender, RoutedEventArgs e)
        {
            if (panButton.IsChecked != null)
                if ((bool)panButton.IsChecked)
                {
                    windowZoomButton.IsChecked = false;
                    graphScroller.MouseDraggingMode = MouseDraggingMode.Pan;
                }
            CheckIfLayoutEditing();
        }

        void WindowZoomButtonClick(object sender, RoutedEventArgs e)
        {
            if (windowZoomButton.IsChecked != null)
                if ((bool)windowZoomButton.IsChecked)
                {
                    panButton.IsChecked = false;
                    graphScroller.MouseDraggingMode = MouseDraggingMode.ZoomWithRectangle;
                }

            CheckIfLayoutEditing();
        }

        void CheckIfLayoutEditing()
        {
            if (!((bool)windowZoomButton.IsChecked || (bool)panButton.IsChecked))
                graphScroller.MouseDraggingMode = MouseDraggingMode.LayoutEditing;
        }

        /*
                void undoButton_Click(object sender, RoutedEventArgs e) {
                    if (drawingLayoutEditor.CanUndo)
                        this.drawingLayoutEditor.Undo();
                }
        */
        /*
        void redoButton_Click(object sender, RoutedEventArgs e) {
            if (drawingLayoutEditor.CanRedo)
                this.drawingLayoutEditor.Redo();
        }
        */

        void BackwardButtonClick(object sender, RoutedEventArgs e)
        {
            graphScroller.NavigateBackward();
            QuietZoomSliderUpdate();
        }

        void QuietZoomSliderUpdate()
        {
            quietSlider = true;
            zoomSlider.Value = ZoomToSliderVal(graphScroller.ZoomFactor);
            quietSlider = false;
        }

        void DiagramLayoutComplete(object sender, EventArgs e)
        {
            // adjust zoom so diagram fits on screen.      
            zoomSlider.Value = zoomSlider.Minimum;
#if DEBUG
            //     toolStripStatusLabel.Text +=
            //    String.Format(" spline time={0}", Layout.SplineCalculationDuration);
#endif
        }

        void ForwardButtonClick(object sender, RoutedEventArgs e)
        {
            graphScroller.NavigateForward();
            quietSlider = true;
            zoomSlider.Value = ZoomToSliderVal(graphScroller.ZoomFactor);
            quietSlider = false;
        }

        void GraphScrollerForwardEnabledChanged(object sender, bool enabled)
        {
            forwardButton.IsEnabled = enabled;
        }

        void GraphScrollerBackwardEnabledChanged(object sender, bool enabled)
        {
            backwardButton.IsEnabled = enabled;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sv">slider value</param>
        /// <returns></returns>
        static double SliderValToZoom(double sv)
        {
            // double v = zoomSlider.Maximum - sv;
            return Math.Pow(ZoomSliderBase, sv);
        }


        static double ZoomToSliderVal(double z)
        {
            return Math.Log(z, ZoomSliderBase);
        }

        void ZoomSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!quietSlider)
            {
                double scale = SliderValToZoom(zoomSlider.Value);
                graphScroller.ZoomFactor = scale;
            }
        }

        /*
                static Color ToAvalonColor(string co) {
                    //work around to read old colors

                    try {
                        if (co[0] == '#')
                            return Colors.Black;
                        return (Color) ColorConverter.ConvertFromString(co);
                    }
                    catch {
                        return Colors.Black;
                    }
                }
        */

        public void CreateAndLayoutGraph(string fileName)
        {
            try
            {
                if (fileName != null)
                {
                    if (filesToLayout.Count > 0)
                    {
                        string nextFileName = filesToLayout[(offsetInFiles) % filesToLayout.Count];
                        toolStripStatusLabel.Text = fileName + ", coming " + nextFileName;
                    }
                    else
                    {
                        toolStripStatusLabel.Text = fileName;
                    }
                    string extension = Path.GetExtension(fileName).ToLower();

                    if (extension == ".dot")
                    {
                        int line, column;
                        string msg;
                        Graph gwgraph = Parser.Parse(fileName, out line, out column, out msg);
                        if (gwgraph != null)
                        {
                            diagram.Graph = gwgraph;

#if REPORTING
                            this.diagram.Graph.LayoutAlgorithmSettings.Reporting = verbose;
#endif


                            CheckDirectionMenu(gwgraph.Attr.LayerDirection);
                            return;
                        }
                        return;
                    }
                    if (extension == ".msagl") {
                        var g = Graph.Read(fileName);
                        if (g != null) {
                            var oldVal=diagram.NeedToCalculateLayout;
                            diagram.NeedToCalculateLayout=false;
                            diagram.Graph = g;
                            diagram.NeedToCalculateLayout = oldVal;
                        }
                        return;
                    }
                    if (extension == ".txt") {
                        var g = ReadTxtFile(fileName);
                        if (g != null) {
                            var oldVal = diagram.NeedToCalculateLayout;
                            diagram.NeedToCalculateLayout = false;
                            diagram.Graph = g;
                            diagram.NeedToCalculateLayout = oldVal;
                        }
                        return;
                    }

                    if (extension == ".tgf") {
                        var g = ReadTgfFile(fileName);
                        if (g != null) {
                            diagram.Graph = g;
                        }
                        return;
                    }
                    if (extension == ".xsd")
                    {
                        diagram.Graph = Open(fileName);
                        CheckDirectionMenu(diagram.Graph.Attr.LayerDirection);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return;
        }

        static Graph ReadTgfFile(string fileName) {
            var labelDictionary=new Dictionary<string, string>();
            var edgeList = new List<Tuple<string, string>>();
            using (var sr = new StreamReader(fileName)) {
                FillLabels(sr, labelDictionary);
                FillEdges(sr, edgeList);
            }
            return CreateGraphFromTables(labelDictionary, edgeList);
        }

        static Graph CreateGraphFromTables(Dictionary<string, string> labelDictionary, List<Tuple<string, string>> edgeList) {
            var graph=new Graph();
            graph.LayoutAlgorithmSettings = new MdsLayoutSettings();
            foreach (var v in labelDictionary.Values)
                graph.AddNode(v);

            foreach (var tuple in edgeList) {
                var e=graph.AddEdge(labelDictionary[tuple.Item1], labelDictionary[tuple.Item2]);
            }

            return graph;
        }

        static void FillEdges(StreamReader sr, List<Tuple<string, string>> edgeList) {
            do {
                var str = sr.ReadLine();
                
                if (string.IsNullOrEmpty(str) || str[0] == '#')
                    break;

                var tokens = str.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                edgeList.Add(new Tuple<string, string>(tokens[0],tokens[1]));
            } while (true); 
        }

        static void FillLabels(StreamReader sr, Dictionary<string, string> labelDictionary) {
            do {
                var str = sr.ReadLine();
                if (string.IsNullOrEmpty(str) || str[0] == '#')
                    break;

                var tokens = str.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                labelDictionary[tokens[0]] = tokens[1];
            } while (true); 
        }

        static Graph ReadTxtFile(string fileName) {
            var labelsToGeom = new Dictionary<string, Tuple<int, int, int, int>>();
            var edges = new List<Tuple<string, string>>();
            FillLabelsAndEdges(labelsToGeom, edges, fileName);
            return CreateGraphFromParsedStuff(labelsToGeom, edges);
        }

        static Graph CreateGraphFromParsedStuff(Dictionary<string, Tuple<int, int, int, int>> labelsToGeom, List<Tuple<string, string>> edges) {
            var graph = new Graph();
            foreach (var label in labelsToGeom.Keys)
                graph.AddNode(label);
            foreach (var tuple in edges) {
                var e=graph.AddEdge(tuple.Item1, tuple.Item2);
                e.Attr.ArrowheadAtTarget = ArrowStyle.None;

            }

            graph.CreateGeometryGraph();
            foreach (var node in graph.Nodes) {
                var tuple = labelsToGeom[node.Id];
                int centerX = tuple.Item1;
                int centerY = tuple.Item2;
                int w=tuple.Item3;
                int h = tuple.Item4;

                node.GeometryNode.BoundaryCurve = new RoundedRect(
                    new Rectangle(centerX-(double)w/2, centerY-(double)h/2, new Microsoft.Msagl.Core.Geometry.Point(tuple.Item3, tuple.Item4)), 3, 3);
            }

            var router = new SplineRouter(graph.GeometryGraph, 1, 1, Math.PI / 6, null);
            router.Run();
            graph.GeometryGraph.UpdateBoundingBox();
            //LayoutAlgorithmSettings.ShowGraph(graph.GeometryGraph);

            return graph;
        }

        static void FillLabelsAndEdges(Dictionary<string, Tuple<int, int, int, int>> labelsToGeom, List<Tuple<string, string>> edges, string fileName) {
            using (var file = new StreamReader(fileName)) {
                var str = file.ReadLine();
                if (string.IsNullOrEmpty(str) || str[0] != '#')
                    throw new InvalidDataException("unexpected first line in file" + fileName);
                ReadLabelsFromTxt(labelsToGeom, file);
                ReadEdgesFromTxt(edges, file);
            }
        }

        static void ReadLabelsFromTxt(Dictionary<string, Tuple<int, int, int, int>> labelsToGeom, StreamReader file) {
            do {
                var str = file.ReadLine();
                if (string.IsNullOrEmpty(str))
                    return;
                if (str.StartsWith("#"))
                    return;
                var tokens = str.Split(',');
                var label = tokens[0];
                int x, y, w, h;
                if(!int.TryParse(tokens[1], out x))
                    throw new InvalidDataException("cannot parse "+ str);
                if (!int.TryParse(tokens[2], out y))
                    throw new InvalidDataException("cannot parse " + str);
                if (!int.TryParse(tokens[3], out w))
                    throw new InvalidDataException("cannot parse " + str);
                if (!int.TryParse(tokens[4], out h))
                    throw new InvalidDataException("cannot parse " + str);
                var tuple = new Tuple<int, int, int, int>(x,y,w,h);
                labelsToGeom[label]=tuple;
            }
            while (true);
        }

        static void  ReadEdgesFromTxt(List<Tuple<string, string>> edges, StreamReader file) {
            do {
                var str = file.ReadLine();
                if (string.IsNullOrEmpty(str))
                    return;
                if (str.StartsWith("#") )
                    return;
                var tokens = str.Split(',');
                if (tokens.Length == 0)
                    return;
                edges.Add(new Tuple<string, string>(tokens[0], tokens[1]));
            }
            while (true);
        
        }

        static string DotString(string l)
        {
            var r = new StreamReader(l);
            string dotString = r.ReadToEnd();
            r.Close();
            return dotString;
        }

        void TopToBottom(object sender, RoutedEventArgs e)
        {
            ChangeDirection(LayerDirection.TB);
        }

        void BottomToTop(object sender, RoutedEventArgs e)
        {
            ChangeDirection(LayerDirection.BT);
        }

        void LeftToRight(object sender, RoutedEventArgs e)
        {
            ChangeDirection(LayerDirection.LR);
        }

        //  Microsoft.Msagl.Drawing.DrawingLayoutEditor drawingLayoutEditor;


        /*
        void DecorateNodeForDrag(Microsoft.Msagl.Drawing.IDraggableNode node) {
            NodeShape nodeShape = node as NodeShape;
            if (nodeShape != null) 
                nodeShape.Fill = Common.BrushFromMsaglColor(this.drawingLayoutEditor.SelectedEntityColor);
        }*/
        /*
                void RemoveNodeDragDecorations(Microsoft.Msagl.Drawing.IDraggableNode node) {
                    (node as NodeShape).Highlighted = false;
                }

                void DecorateEdgeForDrag(Microsoft.Msagl.Drawing.IDraggableEdge edge) {
                    EdgeShape edgeShape = edge as EdgeShape;
                    if (edgeShape != null)
                        edgeShape.Stroke.Brush = edgeShape.Fill = Common.BrushFromMsaglColor(drawingLayoutEditor.SelectedEntityColor);
                    Canvas.SetZIndex(edgeShape,100000);
                }

                void RemoveEdgeDragDecorations(Microsoft.Msagl.Drawing.IDraggableEdge edge) {
                    EdgeShape es=(edge as EdgeShape);
                    es.Highlighted = false;
                    Canvas.SetZIndex(es, 0);
                }
        */

        /*
                bool ToggleGroupElement(ModifierKeys modifierKeys,
                                        MouseButtons mouseButtons,
                                        bool dragging) {
                    if (editLayoutButton.IsChecked != null && editLayoutButton.IsChecked.Value) {
                        if (!dragging)
                            return LeftButtonIsPressed(mouseButtons);
                        else
                            return
                                ((modifierKeys & ModifierKeys.Control) == ModifierKeys.Control) ||
                                ((modifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift);
                    }
                    return false;
                }
        */

        /*
                bool LeftButtonIsPressed(MouseButtons mouseButtons) {
                    return (mouseButtons & MouseButtons.Left) == MouseButtons.Left;
                }
        */

        /*
        private void CreateGraphEditor() {
            this.drawingLayoutEditor = new Microsoft.Msagl.Drawing.DrawingLayoutEditor(this.graphScroller,
                new Microsoft.Msagl.Drawing.DelegateForEdge(this.DecorateEdgeForDrag),
                new Microsoft.Msagl.Drawing.DelegateForEdge(this.RemoveEdgeDragDecorations),
                new Microsoft.Msagl.Drawing.DelegateForNode(this.DecorateNodeForDrag),
                new Microsoft.Msagl.Drawing.DelegateForNode(this.RemoveNodeDragDecorations),
                new Microsoft.Msagl.Drawing.MouseAndKeysAnalyser(this.ToggleGroupElement));
        }
        */

        void RightToLeft(object sender, RoutedEventArgs e)
        {
            ChangeDirection(LayerDirection.RL);
        }

        void Check(string menu, bool check)
        {
            var item = (MenuItem)FindName(menu);
            item.IsChecked = check;
        }

        void ChangeDirection(LayerDirection dir)
        {
            CheckDirectionMenu(dir);
            Graph g = diagram.Graph;
            if (g != null)
            {
                g.Attr.LayerDirection = direction;
                diagram.OnGraphChanged();
            }
        }

        void CheckDirectionMenu(LayerDirection dir)
        {
            Check(direction.ToString(), false);
            direction = dir;
            Check(dir.ToString(), true);
        }

        void NewFile(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            diagram.Clear();
        }

        void OpenFile(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            var openFileDialog = new OpenFileDialog
            {
                RestoreDirectory = true,
                Filter = "(*.mpd;*.msagl;*.xsd;*.dot;*.txt;*.tgf)|*.mpd;*.msagl;*.xsd;*.dot;*.txt;*.tgf"
            };
            if (openFileDialog.ShowDialog(this) == true)
            {
                currentFile = openFileDialog.FileName;
                CreateAndLayoutGraph(openFileDialog.FileName);
            }
        }

        void ReloadFile(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            if (!string.IsNullOrEmpty(currentFile))
            {
                CreateAndLayoutGraph(currentFile);
            }
        }

        void SaveFile(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            Save();
        }

        public void Save()
        {
            var fd = new SaveFileDialog {
                RestoreDirectory = true,
                Filter = "MSAGL (*.msagl)|*.msagl|" +
                         "BMP (*.bmp)|*.bmp|" +
                         "GIF (*.gif)|*.gif|" +
                         "Portable Network Graphics (*.png)|*.png|" +
                         "JPEG (*.jpg)|*.jpg|" +
                         "XAML (*.xaml)|*.xaml|" +
                         "XPS (*.xps)|*.xps"
            };

            if (fd.ShowDialog() == true)
            {
                Save(fd.FileName);
            }
        }

        void Save(string filename)
        {
            try
            {
                BitmapEncoder enc = null;
                string ext = Path.GetExtension(filename);

                switch (ext.ToLower())
                {
                    case ".bmp":
                        enc = new BmpBitmapEncoder();
                        break;
                    case ".gif":
                        enc = new GifBitmapEncoder();
                        break;
                    case ".xaml":
                        using (var sw = new StreamWriter(filename, false, Encoding.UTF8))
                        {
                            XamlWriter.Save(diagram, sw);
                        }
                        break;
                    case ".xps":
                        SaveXps(filename);
                        break;
                    case ".png":
                        enc = new PngBitmapEncoder();
                        break;
                    case ".jpg":
                        enc = new JpegBitmapEncoder();
                        break;
                    case ".msagl":
                        if (this.diagram.Graph != null)
                            diagram.Graph.Write(filename);                
                        return;

                }
                if (enc != null)
                {
                    Brush graphScrollerBackground = graphScroller.Background;
                    graphScroller.Background = Brushes.White;

                    var rmi = new RenderTargetBitmap((int)graphScroller.ViewportWidth,
                                                     (int)graphScroller.ViewportHeight, 0, 0, PixelFormats.Default);
                    rmi.Render(graphScroller);
                    Clipboard.SetImage(rmi);
                    //// reset VisualOffset to (0,0).
                    //Size s = this.diagram.RenderSize;
                    //diagram.Arrange(new Rect(0, 0, s.Width, s.Height));

                    //Transform t = this.diagram.LayoutTransform;
                    //Point p = t.Transform(new Point(s.Width, s.Height));
                    //RenderTargetBitmap rmi = new RenderTargetBitmap((int)p.X, (int)p.Y, 1 / 96, 1 / 96, PixelFormats.Pbgra32);
                    //rmi.Render(this.diagram);

                    //// fix the VisualOffset so diagram doesn't move inside scroller.
                    //this.graphScroller.Content = null;
                    //this.graphScroller.Content = diagram;
                    try
                    {
                        using (var fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            enc.Frames.Add(BitmapFrame.Create(rmi));
                            enc.Save(fs);
                        }
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch { }
                    // ReSharper restore EmptyGeneralCatchClause
                    finally
                    {
                        graphScroller.Background = graphScrollerBackground;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void SaveXps(string filename)
        {
            Package package = Package.Open(filename, FileMode.Create);
            var xpsDoc = new XpsDocument(package);

            XpsDocumentWriter xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc);

            // zero the VisualOffset
            Size s = diagram.RenderSize;
            Transform t = diagram.LayoutTransform;
            Point p = t.Transform(new Point(s.Width, s.Height));
            diagram.Arrange(new Rect(0, 0, p.X, p.Y));
            graphScroller.Content = null;

            var fp = new FixedPage { Width = p.X, Height = p.Y };
            // Must add the inherited styles before we add the diagram child!
            fp.Resources.MergedDictionaries.Add(Resources);
            fp.Children.Add(diagram);
            xpsWriter.Write(fp);

            // put the diagram back into the scroller.
            fp.Children.Remove(diagram);
            graphScroller.Content = diagram;

            package.Close();
        }

        void ShowTypes(object sender, RoutedEventArgs e)
        {
            ShowTypesMenu.IsChecked = true;
            ShowImportsMenu.IsChecked = false;
            ShowTypeGraph = true;
            diagram.Graph = ShowXsdGraph();
        }

        void ShowImports(object sender, RoutedEventArgs e)
        {
            ShowTypesMenu.IsChecked = false;
            ShowImportsMenu.IsChecked = true;
            ShowTypeGraph = false;
            diagram.Graph = ShowXsdGraph();
        }

        void UndoButtonClick(object sender, RoutedEventArgs e)
        {
            graphScroller.Undo();
        }

        void AnimateButtonClick(object sender, RoutedEventArgs e)
        {
            if (IsTrue(animationButton.IsChecked))
                StartAnimation();
            else
                EndAnimation();
        }

        private void EndAnimation()
        {
            //throw new NotImplementedException();
            animator.Stop();
        }

        private void StartAnimation()
        {
            animator = new Animator(graphScroller);
            animator.Start();
        }

        void RedoButtonClick(object sender, RoutedEventArgs e)
        {
            graphScroller.Redo();
        }

        void FitTheBoundingBox(object sender, RoutedEventArgs e)
        {
            graphScroller.FitGraphBoundingBox();
        }


        void Cut(object sender, RoutedEventArgs e) { }
        void Copy(object sender, RoutedEventArgs e) { }
        void Paste(object sender, RoutedEventArgs e) { }

        static BitmapImage GetImage(string url)
        {
            var bitImage = new BitmapImage();
            bitImage.BeginInit();
            bitImage.UriSource = new Uri(url, UriKind.Absolute);
            bitImage.EndInit();
            return bitImage;
        }

        void PanButtonChecked(object sender, RoutedEventArgs e)
        {
            windowZoomButton.IsChecked = false;
            panButton.Content = panImage;
            graphScroller.MouseDraggingMode = MouseDraggingMode.Pan;
        }

        void PanButtonUnchecked(object sender, RoutedEventArgs e)
        {
            panButton.Content = uncheckedPanImage;
            CheckIfLayoutEditing();
        }

        void WindowZoomButtonChecked(object sender, RoutedEventArgs e)
        {
            windowZoomButton.Content = windowZoomWindowImage;
            panButton.IsChecked = false;
        }

        void WindowZoomButtonUnchecked(object sender, RoutedEventArgs e)
        {
            windowZoomButton.Content = uncheckedWindowZoomImage;
        }

        


        void RouteEdges()
        {
            var graph = graphScroller.Graph;
            if (graph == null) return;
            var gg = graph.GeometryGraph;
            if (gg == null) return;

            var ls = graph.LayoutAlgorithmSettings;
            var ers = ls.EdgeRoutingSettings;
            if(ers.EdgeRoutingMode==EdgeRoutingMode.SplineBundling) {
                if (ers.BundlingSettings == null)
                    ers.BundlingSettings = new BundlingSettings();
            }
                
            Microsoft.Msagl.Miscellaneous.LayoutHelpers.RouteAndLabelEdges(gg, ls, gg.Edges);
            foreach (var e in graphScroller.Entities.Where(e => e is IViewerEdge))
                graphScroller.Invalidate(e);
        }



        EdgeRoutingMode FigureOutEdgeRoutingMode()
        {
            if (IsTrue(rectRoutingButton.IsChecked))
                return EdgeRoutingMode.Rectilinear;
            if (IsTrue(rectToCenterButton.IsChecked))
                return EdgeRoutingMode.RectilinearToCenter;
            if (IsTrue(bundleRoutingButton.IsChecked))
                return EdgeRoutingMode.SplineBundling;
            return EdgeRoutingMode.Spline;
        }

        static bool IsTrue(bool? isChecked)
        {
            return isChecked != null && isChecked.Value;
        }

        void RoutingChangedClick(object sender, RoutedEventArgs e)
        {
            if (diagram.Graph != null)
                diagram.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = FigureOutEdgeRoutingMode();
            RouteEdges();
        }

        void EditRoutesClick(object sender, RoutedEventArgs e) {
            routeDialogBox.Visibility = IsTrue(editRoutesButton.IsChecked) ? Visibility.Visible : Visibility.Collapsed;
        }

        void EnterPolygonsClick(object sender, RoutedEventArgs e) {
            if (IsTrue(enterPolylineButton.IsChecked))
                AttachPolygonManager();
            else {               
                polygonManager.Tesselate();
                polygonManager.DetachEvents();
            }
        }

        private void AttachPolygonManager() {
            if (polygonManager != null) {
                polygonManager.ClearGroups();
            } else {
                polygonManager = new PolygonManager(graphScroller);
            }
            polygonManager.AttachEvents();
           
            graphScroller.MouseDraggingMode = MouseDraggingMode.UserDefinedMode;
            windowZoomButton.IsChecked = false;
            panButton.IsChecked = false;

            //defaultBackgroundForDefiningGroups = defineGroupButton.Background;
            //defineGroupButton.Background = Brushes.Brown;            

        }

       
        void InkSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (graphScroller == null || graphScroller.Graph == null ||
                graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings == null) return;
            var val = inkCoeffSlider.Value;
            BundlingSettings.DefaultInkImportance=val;
            if (graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings != null)
                graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings.InkImportance = val;
            RouteEdges();
        }
        void EdgeSeparationSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (graphScroller==null ||  graphScroller.Graph == null ||
                graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings==null) return;
            var val = edgeSeparationSlider.Value;
            BundlingSettings.DefaultEdgeSeparation=val;
            if(graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings!=null)
               graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings.EdgeSeparation=val; 
            RouteEdges();
        }
        void PathLengthCoeffSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (graphScroller == null || graphScroller.Graph == null ||
                   graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings == null) return;
            var val = pathLenghtCoeffSlider.Value;
            BundlingSettings.DefaultPathLenghtImportance = val;
            if (graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings != null)
                graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings.PathLengthImportance = val;
            RouteEdges();
        }
        void CapacityCoeffSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (graphScroller == null || graphScroller.Graph == null ||
                graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings == null) return;
            var val = capacityCoeffSlider.Value;
            BundlingSettings.DefaultCapacityOverflowCoefficientMultiplier = val;
            if (graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings != null)
                graphScroller.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings.CapacityOverflowCoefficient = val;
            RouteEdges();
        }

        void LayoutWithMds(object sender, RoutedEventArgs e) {
            var g = diagram.Graph;
            if (g == null) return;
            g.LayoutAlgorithmSettings = new MdsLayoutSettings();
            diagram.OnGraphChanged();
        }
    }
}