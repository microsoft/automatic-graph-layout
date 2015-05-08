using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Msagl.ControlForWpfObsolete;
using Microsoft.Msagl;
using System.IO;
using System.Windows.Markup;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;
using Point = System.Windows.Point;

namespace TestForAvalon
{
    /// <summary>
    /// Interaction logic for Details.xaml
    /// </summary>

    public partial class Details : System.Windows.Window
    {
        GraphScroller graphScroller;
        Diagram diagram;
        Microsoft.Msagl.Drawing.LayerDirection direction = Microsoft.Msagl.Drawing.LayerDirection.TB;
      //  Details details;
       // EdgeDetails edgeDetails;

        public Details()
        {
            InitializeComponent();

            CreateGraphEditor();
      

            this.dragButton.Unchecked += new RoutedEventHandler(dragButton_Unchecked);
            this.Closing += new System.ComponentModel.CancelEventHandler(Details_Closing);

        }

        private delegate void DoNothing();

        void Details_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,  
                (DoNothing)delegate {
                    try
                    {
                        this.Hide();
                    }
                    catch (Exception exception) { System.Console.WriteLine(exception); }
                });
            e.Cancel = true;            
        }

        void dragButton_Unchecked(object sender, RoutedEventArgs e) {
            if (this.drawingLayoutEditor != null)
                drawingLayoutEditor.Clear();
        }
        
        void Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        internal void TakeGraph(Microsoft.Msagl.Drawing.Graph graph)
        {
            if (graph != null)
            {
                graph.Attr.AspectRatio = this.aspectRatioSlider.Value;
                this.diagram.Graph = graph;
                this.diagram.Graph.Attr.AspectRatio = this.aspectRatioSlider.Value;
                CheckDirectionMenu(graph.Attr.LayerDirection);
            }

            diagram.InvalidateVisual();               
        }

        void DockPanelInitialized(object sender, EventArgs e)
        {

#if DEBUGGLEE
      Microsoft.Msagl.Layout.Show = new Microsoft.Msagl.Show(Microsoft.Msagl.CommonTest.DisplayGeometryGraph.ShowCurves);
#endif
            zoomSlider.Value = zoomSlider.Minimum;
            zoomSlider.LostMouseCapture += new MouseEventHandler(zoomSlider_LostMouseCapture);

            graphScroller = new GraphScroller();
            graphScroller.ViewingSettingsChangedEvent += new EventHandler(graphScroller_ViewingSettingsChangedEvent);

            diagram = this.graphScroller.Diagram;
            diagram.Background = Brushes.AliceBlue;
            diagram.LayoutComplete += DiagramLayoutComplete;
          
            this.holder.Children.Add(graphScroller);
            this.holder.ClipToBounds = true;

            this.zoomSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(zoomSlider_ValueChanged);
            this.zoomSlider.Minimum = 0;
            this.zoomSlider.Maximum = 100;
            this.zoomSlider.Value = zoomSlider.Minimum;

            this.brandesThresholdSlider.Minimum = 0;
            this.brandesThresholdSlider.Maximum = 5000;
            //  this.brandesThresholdSlider.Value = Layout.BrandesThreshold;
            this.brandesThresholdSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(brandesThresholdSlider_ValueChanged);

            this.aspectRatioSlider.Minimum = 0;
            this.aspectRatioSlider.Maximum = 10;
            this.aspectRatioSlider.Value = 0;
            this.aspectRatioSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(aspectRatioSlider_ValueChanged);

            this.backwardButton.IsEnabled = false;
            this.forwardButton.IsEnabled = false;

            
            graphScroller.BackwardEnabledChanged += new NavigationChangeDelegate(graphScroller_BackwardEnabledChanged);
            graphScroller.ForwardEnabledChanged += new NavigationChangeDelegate(graphScroller_ForwardEnabledChanged);
            this.forwardButton.Click += new RoutedEventHandler(forwardButton_Click);
            this.backwardButton.Click += new RoutedEventHandler(backwardButton_Click);


            this.windowZoomButton.IsChecked = true;
            this.graphScroller.MouseDraggingMode = MouseDraggingMode.LayoutEditing;
        //    this.windowZoomButton.Click += new RoutedEventHandler(windowZoomButton_Click);
            this.panButton.Click += new RoutedEventHandler(panButton_Click);
            windowZoomButton.ToolTip = "Set window zoom mode";
            panButton.ToolTip = "Set pan mode";

            //this.diagram.SelectionChanged += new SelectionChangedEventHandler(
            //                delegate
            //                {
            //                    NodeShape ns = diagram.SelectedObject as NodeShape;

            //                    if (ns != null)
            //                    {
            //                        NodeInfo ni = (NodeInfo)ns.Node.UserData;
            //                        SubGraph bucket = ni.Contents as SubGraph;

            //                        if (bucket != null)
            //                        {
            //                            if (details == null)
            //                            {
            //                                details = new Details();
            //                            }

            //                            details.Title = "Details for " + ni.Name;
            //                            details.aspectRatioSlider.Value = this.aspectRatioSlider.Value;
            //                            details.Show();
            //                            details.TakeGraph(bucket.SubBuilder.MakeGraph(MpdGraphType.Class));                                        
            //                        }
            //                    }
            //                }
            //                );
            //this.diagram.EdgeSelectionChanged += new SelectionChangedEventHandler(
            //                delegate
            //                {
            //                    EdgeShape es = diagram.SelectedEdge as EdgeShape;

            //                    if (es != null)
            //                    {
            //                        EdgeInfo edgeInfo = es.Edge.UserData as EdgeInfo;

            //                        if (edgeInfo != null)
            //                        {
            //                            if (edgeDetails == null)
            //                            {
            //                                edgeDetails = new EdgeDetails();
            //                            }

            //                            edgeDetails.Title = "Details for Edge from " + es.Edge.Source + " to " + es.Edge.Target;
            //                            edgeDetails.TakeEdge(es.Edge);
            //                            edgeDetails.Show();
            //                        }
            //                    }
            //                }
            //                );
        }

        private string GetText(Microsoft.Msagl.Drawing.DrawingObject drawingObject) {
            if (drawingObject == null)
                return "null";
            return drawingObject.ToString();
        }

        void aspectRatioSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            aspectRatioSlider.ToolTip = toolStripStatusLabel.Text = String.Format("aspect ratio = {0}", aspectRatioSlider.Value);
        }

        void graphScroller_ViewingSettingsChangedEvent(object sender, EventArgs e)
        {
            QuietZoomSliderUpdate();
            if (!this.zoomSlider.IsMouseCaptureWithin)
            {
                this.graphScroller.PushViewState();
            }
        }


        void zoomSlider_LostMouseCapture(object sender, MouseEventArgs e)
        {
            this.graphScroller.PushViewState();
        }

        void panButton_Click(object sender, RoutedEventArgs e)
        {
            this.graphScroller.MouseDraggingMode = MouseDraggingMode.Pan;
           // windowZoomButton.IsEnabled = true;
           // panButton.IsEnabled = false;

        }

        void windowZoomButton_Click(object sender, RoutedEventArgs e)
        {
            this.graphScroller.MouseDraggingMode = MouseDraggingMode.ZoomWithRectangle;
           // panButton.IsEnabled = true;
           // windowZoomButton.IsEnabled = false;



        }

        void backwardButton_Click(object sender, RoutedEventArgs e)
        {
            graphScroller.NavigateBackward();
            QuietZoomSliderUpdate();
        }

        private void QuietZoomSliderUpdate()
        {
            quietSlider = true;
            zoomSlider.Value = this.ZoomToSliderVal(graphScroller.ZoomFactor);
            quietSlider = false;
        }

        void DiagramLayoutComplete(object sender, EventArgs e) {
            // adjust zoom so diagram fits on screen.      
            zoomSlider.Value = zoomSlider.Minimum;
            graphScroller.DiagramGraphChanged(null,null);

#if DEBUG
            //    toolStripStatusLabel.Text +=
            //   String.Format(" spline time={0}", Layout.SplineCalculationDuration);
#endif
        }

        void forwardButton_Click(object sender, RoutedEventArgs e)
        {
            graphScroller.NavigateForward();
            quietSlider = true;
            zoomSlider.Value = this.ZoomToSliderVal(graphScroller.ZoomFactor);
            quietSlider = false;
        }

        void graphScroller_ForwardEnabledChanged(object sender, bool enabled)
        {
            forwardButton.IsEnabled = enabled;
        }

        void graphScroller_BackwardEnabledChanged(object sender, bool enabled)
        {
            backwardButton.IsEnabled = enabled;
        }

        bool quietSlider = false;
        double zoomSliderBase = 1.1;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sv">slider value</param>
        /// <returns></returns>
        double SliderValToZoom(double sv)
        {
           // double v = zoomSlider.Maximum - sv;
            return Math.Pow(zoomSliderBase, sv);
        }


        double ZoomToSliderVal(double z)
        {
            return Math.Log(z, zoomSliderBase);
        }

        void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!quietSlider)
            {
                double scale = SliderValToZoom(zoomSlider.Value);
                this.graphScroller.ZoomFactor = scale;
            }
        }

        void brandesThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Layout.BrandesThreshold = (int)brandesThresholdSlider.Value;
        }

        void TopToBottom(object sender, RoutedEventArgs e)
        {
            ChangeDirection(Microsoft.Msagl.Drawing.LayerDirection.TB);
        }
        void BottomToTop(object sender, RoutedEventArgs e)
        {
            ChangeDirection(Microsoft.Msagl.Drawing.LayerDirection.BT);
        }
        void LeftToRight(object sender, RoutedEventArgs e)
        {
            ChangeDirection(Microsoft.Msagl.Drawing.LayerDirection.LR);
        }

        Microsoft.Msagl.Drawing.DrawingLayoutEditor drawingLayoutEditor;

        void DecorateNodeForDrag(Microsoft.Msagl.Drawing.IViewerObject obj) {
            //NodeShape nodeShape = node as NodeShape;
            //if (nodeShape != null) 
            //    nodeShape.Fill = Common.BrushFromMsaglColor( .SelectedNodeAttribute.FillColor);
        }

        void RemoveNodeDragDecorations(Microsoft.Msagl.Drawing.IViewerObject obj) {
            (obj as SelectableShape).Highlighted=false;
        }
        
      
        bool ToggleGroupElement(Microsoft.Msagl.Drawing.ModifierKeys modifierKeys,
            Microsoft.Msagl.Drawing.MouseButtons mouseButtons,
            bool dragging) {
            if (this.dragButton.IsChecked != null && this.dragButton.IsChecked.Value) {
                if (!dragging)
                    return LeftButtonIsPressed(mouseButtons);
                else
                    return
                        ((modifierKeys & Microsoft.Msagl.Drawing.ModifierKeys.Control) == Microsoft.Msagl.Drawing.ModifierKeys.Control) ||
                        ((modifierKeys & Microsoft.Msagl.Drawing.ModifierKeys.Shift) == Microsoft.Msagl.Drawing.ModifierKeys.Shift);
            }
            return false;
        }

        private bool LeftButtonIsPressed(Microsoft.Msagl.Drawing.MouseButtons mouseButtons) {
            return (mouseButtons & Microsoft.Msagl.Drawing.MouseButtons.Left) == Microsoft.Msagl.Drawing.MouseButtons.Left;
        }
       

        private void CreateGraphEditor() {
            this.drawingLayoutEditor = new Microsoft.Msagl.Drawing.DrawingLayoutEditor(this.graphScroller);
        }

        void RightToLeft(object sender, RoutedEventArgs e)
        {
            ChangeDirection(Microsoft.Msagl.Drawing.LayerDirection.RL);
        }

        void Check(string menu, bool check)
        {
            MenuItem item = (MenuItem)this.FindName(menu);
            item.IsChecked = check;
        }

        void ChangeDirection(Microsoft.Msagl.Drawing.LayerDirection dir)
        {
            CheckDirectionMenu(dir);
            Microsoft.Msagl.Drawing.Graph g = this.diagram.Graph;
            if (g != null)
            {
                g.Attr.LayerDirection= this.direction;
                this.diagram.OnGraphChanged();
            }
        }

        private void CheckDirectionMenu(Microsoft.Msagl.Drawing.LayerDirection dir) {
            Check(this.direction.ToString(), false);
            this.direction = dir;
            Check(dir.ToString(), true);
        }        
       
        void SaveFile(object sender, RoutedEventArgs e)
        {
            Save();
        }

        public void Save()
        {
            Microsoft.Win32.SaveFileDialog fd = new Microsoft.Win32.SaveFileDialog();
            fd.RestoreDirectory = true;
            fd.Filter = "Bitmap (*.bmp)|*.bmp|" +
                      "GIF (*.gif)|*.gif|" +
                      "Portable Network Graphics (*.png)|*.png|" +
                      "JPEG (*.jpg)|*.jpg|" +
                      "XAML (*.xaml)|*.xaml|" +
                      "XPS (*.xps)|*.xps|" +
                      "DOT (*.dot)|*.dot";

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
                string ext = System.IO.Path.GetExtension(filename);

                switch (ext.ToLower())
                {
                    case ".bmp":
                        enc = new BmpBitmapEncoder();
                        break;
                    case ".gif":
                        enc = new GifBitmapEncoder();
                        break;
                    case ".xaml":
                        using (StreamWriter sw = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
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
                    case ".dot":
                        break;
                }
                if (enc != null)
                {
                    // reset VisualOffset to (0,0).
                    Size s = this.diagram.RenderSize;
                    diagram.Arrange(new Rect(0, 0, s.Width, s.Height));

                    Transform t = this.diagram.LayoutTransform;
                    Point p = t.Transform(new Point(s.Width, s.Height));
                    RenderTargetBitmap rmi = new RenderTargetBitmap((int)p.X, (int)p.Y, 1 / 96, 1 / 96, PixelFormats.Pbgra32);
                    rmi.Render(this.diagram);

                    // fix the VisualOffset so diagram doesn't move inside scroller.
                    this.graphScroller.Content = null;
                    this.graphScroller.Content = diagram;

                    using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        enc.Frames.Add(BitmapFrame.Create(rmi));
                        enc.Save(fs);
                    }
                }

            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.ToString(), "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void SaveXps(string filename)
        {
            System.IO.Packaging.Package package = System.IO.Packaging.Package.Open(filename, System.IO.FileMode.Create);
            XpsDocument xpsDoc = new XpsDocument(package);

            XpsDocumentWriter xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc);

            // zero the VisualOffset
            Size s = this.diagram.RenderSize;
            Transform t = this.diagram.LayoutTransform;
            Point p = t.Transform(new Point(s.Width, s.Height));
            diagram.Arrange(new Rect(0, 0, p.X, p.Y));
            this.graphScroller.Content = null;

            FixedPage fp = new FixedPage();
            fp.Width = p.X;
            fp.Height = p.Y;
            // Must add the inherited styles before we add the diagram child!
            fp.Resources.MergedDictionaries.Add(this.Resources);
            fp.Children.Add(this.diagram);
            xpsWriter.Write(fp);

            // put the diagram back into the scroller.
            fp.Children.Remove(this.diagram);
            this.graphScroller.Content = diagram;

            package.Close();
        }

        void Cut(object sender, RoutedEventArgs e)
        {
        }
        void Copy(object sender, RoutedEventArgs e)
        {
        }
        void Paste(object sender, RoutedEventArgs e)
        {
        }

    }
}