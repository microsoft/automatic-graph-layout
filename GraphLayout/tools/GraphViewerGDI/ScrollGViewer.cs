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
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Prototype.Ranking;
using MouseButtons=System.Windows.Forms.MouseButtons;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = System.Drawing.Rectangle;
using Size=System.Drawing.Size;

namespace Microsoft.Msagl.GraphViewerGdi{
    /// <summary>
    /// Summary description for DOTViewer.
    /// </summary>
    partial class GViewer : IViewer{
        const string windowZoomButtonDisabledToolTipText = "Zoom in by dragging a rectangle, is disabled now";
        internal static double Dpi = GetDotsPerInch();
        internal static double dpix;
        internal static double dpiy;
        public MdsLayoutSettings mdsLayoutSettings;
        readonly RankingLayoutSettings rankingSettings = new RankingLayoutSettings();
        readonly SugiyamaLayoutSettings sugiyamaSettings;
        LayoutMethod currentLayoutMethod = LayoutMethod.UseSettingsOfTheGraph;
        System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(0, 0, 0, 0);
        DrawingPanel panel;
        bool saveAsImageEnabled = true;
        bool saveAsMsaglEnabled = true;
        bool saveInVectorFormatEnabled = true;
        bool zoomWhenMouseWheelScroll = true;

        const string panButtonToolTipText = "Pan";
        RectangleF srcRect = new RectangleF(0, 0, 0, 0);

        internal double zoomFraction = 0.5f;

        /// <summary>
        /// Default constructor
        /// </summary>
        public GViewer(){
            mdsLayoutSettings = new MdsLayoutSettings() { RunInParallel = this.AsyncLayout };
            sugiyamaSettings = new SugiyamaLayoutSettings();
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            BackwardEnabled = false;

            ForwardEnabled = false;

            toolbar.MouseMove += ToolBarMouseMoved;

            Assembly a = Assembly.GetExecutingAssembly();   
            foreach (string r in a.GetManifestResourceNames()){
                if (r.Contains("hmove.cur"))
                    panGrabCursor = new Cursor(a.GetManifestResourceStream(r));
                else if (r.Contains("oph.cur"))
                    panOpenCursor = new Cursor(a.GetManifestResourceStream(r));
            }

            originalCursor = Cursor;

            
            panButton.Checked = false;
            windowZoomButton.Checked = false;

            layoutSettingsButton.ToolTipText = "Configures the layout algorithm settings";

            undoButton.ToolTipText = "Undo layout editing";
            redoButton.ToolTipText = "Redo layout editing";
            forwardButton.ToolTipText = "Forward";
            panButton.ToolTipText = panButton.Checked ? panButtonToolTipText : PanButtonDisabledToolTipText;
            windowZoomButton.ToolTipText = windowZoomButton.Checked
                                               ? WindowZoomButtonToolTipText
                                               : windowZoomButtonDisabledToolTipText;

            InitDrawingLayoutEditor();

            toolbar.Invalidate();

            SuspendLayout();
            InitPanel();
            Controls.Add(toolbar);
            ResumeLayout();          
        }

      

        /*
         * (s, 0,a)(srcRect.X)= (destRect.Left,destRect.Top)
         * (0,-s,b)(srcRect.Y)
         * a=destRect.Left-s*srcRect.Left
         * b=destRect.Bottom + srcRect.Bottom * s
         * */

        

        internal RectangleF SrcRect{
            get { return srcRect; }
            set { srcRect = value; }
        }


        
        
        
        /// <summary>
        /// The width of the current graph
        /// </summary>
        public double GraphWidth{
            get { return OriginalGraph.Width; }
        }

        /// <summary>
        /// The height of the current graph
        /// </summary>
        public double GraphHeight{
            get { return OriginalGraph.Height; }
        }

        /// <summary>
        /// Gets or sets the zoom factor
        /// </summary>
        public double ZoomF{
            get { return CurrentScale/GetFitScale(); }
            set{
                
                if (OriginalGraph == null)
                    return;
                if (value < ApproximateComparer.Tolerance || double.IsNaN(value)){
                    //MessageBox.Show("the zoom value is out of range ")
                    return;
                }
                
                var center = new Point(panel.Width/2.0, panel.Height/2.0);
                var centerOnSource = transformation.Inverse*center;
                var scaleForZoom1 = GetFitScale();
                var scale = scaleForZoom1*value;
                SetTransformOnScaleAndCenter(scale,centerOnSource);
                panel.Invalidate();
            }
        }

        internal int PanelWidth{
            get { return panel.ClientRectangle.Width; }
        }

        internal int PanelHeight{
            get { return panel.ClientRectangle.Height; }
        }

        /// <summary>
        /// capturing the previous user's choice of which veiw to save
        /// </summary>
        internal bool SaveCurrentViewInImage { get; set; }

        /// <summary>
        /// The panel containing GViewer object
        /// </summary>
        public Control DrawingPanel{
            get { return panel; }
        }

        /// <summary>
        /// Gets or sets the forward and backward buttons visibility
        /// </summary>
        public bool NavigationVisible{
            get { return forwardButton.Visible; }
            set{
                forwardButton.Visible = value;
                backwardButton.Visible = value;
            }
        }

        /// <summary>
        /// Gets or sets the save button visibility
        /// </summary>
        public bool SaveButtonVisible{
            get { return saveButton.Visible; }
            set { saveButton.Visible = value; }
        }

        ///// <summary>
        ///// The event raised when the graph object under the mouse cursor changes
        ///// </summary>
        //public event EventHandler SelectionChanged;

        /// <summary>
        /// The rectangle for drawing
        /// </summary>
        internal System.Drawing.Rectangle DestRect{
            get { return destRect; }
            set { destRect = value; }
        }

        /// <summary>
        /// Enables or disables the forward button
        /// </summary>
        public bool ForwardEnabled{
            get { return forwardButton.ImageIndex == (int) ImageEnum.Forward; }

            set {
                int i = (int)(value ? ImageEnum.Forward : ImageEnum.ForwardDis);
                if (forwardButton.ImageIndex != i)
                    forwardButton.ImageIndex = i;
            }
        }

        /// <summary>
        /// Enables or disables the backward button
        /// </summary>
        public bool BackwardEnabled{
            get { return backwardButton.ImageIndex == (int) ImageEnum.Backward; }

            set {
                int i = (int)(value ? ImageEnum.Backward : ImageEnum.BackwardDis);
                if (backwardButton.ImageIndex != i)
                    backwardButton.ImageIndex = i;
            }
        }

        /// <summary>
        /// hides/shows the toolbar
        /// </summary>
        public bool ToolBarIsVisible{
            get { return Controls.Contains(toolbar); }
            set{
                if (value != ToolBarIsVisible){
                    SuspendLayout();
                    if (value){
                        Controls.Add(toolbar);
                        Controls.SetChildIndex(toolbar, 1); //it follows the panel
                    }
                    else
                        Controls.Remove(toolbar);

                    ResumeLayout();
                }
            }
        }

        
        /// <summary>
        /// If this property is set to true the control enables saving and loading of .MSAGL files
        /// Otherwise the "Load file" button and saving as .MSAGL file is disabled.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msagl")]
        public bool SaveAsMsaglEnabled{
            get { return saveAsMsaglEnabled; }
            set{
                if (saveAsMsaglEnabled != value){
                    openButton.Visible = value;
                    saveAsMsaglEnabled = value;
                }
            }
        }

        /// <summary>
        /// enables or disables saving the graph in a vector format
        /// </summary>
        public bool SaveInVectorFormatEnabled{
            get { return saveInVectorFormatEnabled; }
            set { saveInVectorFormatEnabled = value; }
        }

        /// <summary>
        /// enables or disables saving the graph as an image
        /// </summary>
        public bool SaveAsImageEnabled{
            get { return saveAsImageEnabled; }
            set { saveAsImageEnabled = value; }
        }


        /// <summary>
        ///hides and shows the layout algorithm settings button
        /// </summary>
        public bool LayoutAlgorithmSettingsButtonVisible{
            get { return layoutSettingsButton.Visible; }
            set { layoutSettingsButton.Visible = value; }
        }

        /// <summary>
        /// hides and shows the "Save graph" button
        /// </summary>
        public bool SaveGraphButtonVisible{
            get { return saveButton.Visible; }
            set { saveButton.Visible = value; }
        }

        /// <summary>
        ///hides and shows the undo/redo buttons
        /// </summary>
        public bool UndoRedoButtonsVisible
        {
            get { return undoButton.Visible; }
            set
            {
                undoButton.Visible = value;
                redoButton.Visible = value;
            }
        }

        /// <summary>
        ///hides and shows the edge insert button
        /// </summary>
        public bool EdgeInsertButtonVisible
        {
            get { return edgeInsertButton.Visible; }
            set { edgeInsertButton.Visible = value; }
        }

        /// <summary>
        /// exposes the kind of the layout that is used when the graph is laid out by the viewer
        /// </summary>
        public LayoutMethod CurrentLayoutMethod{
            get { return currentLayoutMethod; }
            set { currentLayoutMethod = value; }

        }

        #region Members

        const int minimalSizeToDraw = 10;

        readonly ViewInfosList listOfViewInfos = new ViewInfosList();
        System.Drawing.Point mousePositonWhenSetSelectedObject;
        internal Cursor originalCursor;

        Brush outsideAreaBrush = Brushes.LightGray;
        internal Cursor panGrabCursor;
        internal Cursor panOpenCursor;

        internal DObject selectedDObject;
        /// <summary>
        /// The color of the area outside of the graph.
        /// </summary>
        public Brush OutsideAreaBrush{
            get { return outsideAreaBrush; }
            set { outsideAreaBrush = value; }
        }

        /// <summary>
        /// The object which is currently located under the mouse cursor
        /// </summary>
        public object SelectedObject{
            get { return selectedDObject != null ? selectedDObject.DrawingObject : null; }
        }

        internal System.Drawing.Point MousePositonWhenSetSelectedObject{
            get { return mousePositonWhenSetSelectedObject; }
            set { mousePositonWhenSetSelectedObject = value; }
        }


        internal ToolTip ToolTip{
            get { return toolTip1; }
            set { toolTip1 = value; }
        }

        internal void SetSelectedObject(object o){
            selectedDObject = (DObject) o;
            MousePositonWhenSetSelectedObject = MousePosition;
        }


        //public static double LocationToFloat(int location) { return ((double)location) * LayoutAlgorithmSettings.PointSize; }

        //public static double LocationToFloat(string location) { return LocationToFloat(Int32.Parse(location)); }



        #endregion

        bool editingEnabled = true;

        readonly FastIncrementalLayoutSettings
            fastIncrementalLayoutSettings = FastIncrementalLayoutSettings.CreateFastIncrementalLayoutSettings();


        /// <summary>
        /// 
        /// </summary>
        bool EditingEnabled
        {
            get { return editingEnabled; }
            set { editingEnabled = value; }
        }
        #region IViewer Members

        /// <summary>
        /// 
        /// </summary>
#pragma warning disable 67
        public event EventHandler<EventArgs> ViewChangeEvent;
#pragma warning restore 67

        /// <summary>
        /// enables and disables the default editing of the viewer
        /// </summary>
        public bool LayoutEditingEnabled{
            get { return !(panButton.Checked || windowZoomButton.Checked) && EditingEnabled; }
            set { EditingEnabled = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        public Core.Geometry.Rectangle ClientViewport { get; private set; }


        /// <summary>
        /// 
        /// </summary>
        public double DistanceForSnappingThePortToNodeBoundary{
            get { return UnderlyingPolylineCircleRadius*2; }
        }

        public bool IncrementalDraggingModeAlways { get; set; }

        #endregion

        void CalcDestRect() {
            var lt = Transform*new Point(srcRect.Left, srcRect.Bottom);
            destRect.X = (int) lt.X;
            destRect.Y = (int) lt.Y;
            destRect.Width = (int) (CurrentScale*srcRect.Width);
            destRect.Height = (int) (CurrentScale*srcRect.Height);
        }

        void CalcRects(PrintPageEventArgs printPageEvenArgs) {
            var w = printPageEvenArgs == null ? PanelWidth : printPageEvenArgs.PageBounds.Width;
            var h = printPageEvenArgs == null ? PanelHeight : printPageEvenArgs.PageBounds.Height;

            if (OriginalGraph != null){
                CalcSrcRect(w,h);
                CalcDestRect();
            }
            prevPanelClientRectangle = panel.ClientRectangle;
        }

        void CalcSrcRect(double w, double h) {
            var m = Transform.Inverse;
            var rec=new  Core.Geometry.Rectangle(m*(new Point(0, 0)), m*new Point(w, h));
            rec = Core.Geometry.Rectangle.Intersect(originalGraph.BoundingBox,rec);
            srcRect = new RectangleF((float)rec.Left, (float)rec.Bottom, (float)rec.Width, (float)rec.Height);

//            if (scaledDown == false){
//                double k = OriginalGraph.Width/ScrollMaxF;
//
//                srcRect.Width = (float) Math.Min(OriginalGraph.Width, k*HLargeChangeF);
//                srcRect.X = (float) (k*HValF) + (float) OriginalGraph.Left;
//
//                k = OriginalGraph.Height/ScrollMaxF;
//                srcRect.Y = (float) OriginalGraph.Height + (float) ScaleFromScrollToSrcY(VVal + VLargeChange) +
//                            (float) OriginalGraph.Bottom;
//                srcRect.Height = (float) Math.Min(OriginalGraph.Height, k*VLargeChangeF);
//            }
//            else{
//                srcRect.X = (float) OriginalGraph.Left;
//                srcRect.Y = (float) OriginalGraph.Height + (float) ScaleFromScrollToSrcY(vScrollBar.Maximum) +
//                            (float) OriginalGraph.Bottom;
//                srcRect.Width = (float) GraphWidth;
//                srcRect.Height = (float) GraphHeight;
//            }
        }

        static double GetDotsPerInch(){
            Graphics g = (new Form()).CreateGraphics();
            return Math.Max(dpix = g.DpiX, dpiy = g.DpiY);
        }


        /// <summary>
        /// The ViewInfo gives all info needed for setting the view
        /// </summary>
        protected override void OnPaint(PaintEventArgs e){
            panel.Invalidate();
        }

        void SetViewFromViewInfo(ViewInfo viewInfo) {
            Transform = viewInfo.Transformation.Clone();
            panel.Invalidate();
        }


        void ToolBarMouseMoved(object o, MouseEventArgs a){
            Cursor = originalCursor;
        }

        /// <summary>
        /// Tightly fit the bounding box around the graph
        /// </summary>
        public void FitGraphBoundingBox(){
            if (LayoutEditor != null){
                if (Graph != null)
                    LayoutEditor.FitGraphBoundingBox(DGraph);
                Invalidate();
            }
        }


        void InitPanel(){
            panel = new DrawingPanel{TabIndex = 0};
            Controls.Add(panel);
            panel.Dock = DockStyle.Fill;

            panel.Name = "panel";
            panel.TabIndex = 0;
            panel.GViewer = this;
            panel.SetDoubleBuffering();
            panel.Click += PanelClick;
            DrawingPanel.MouseClick += DrawingPanelMouseClick;
            DrawingPanel.MouseDoubleClick += DrawingPanel_MouseDoubleClick;
            DrawingPanel.MouseCaptureChanged += DrawingPanel_MouseCaptureChanged;
            DrawingPanel.MouseDown += DrawingPanel_MouseDown;
            DrawingPanel.MouseEnter += DrawingPanel_MouseEnter;
            DrawingPanel.MouseHover += DrawingPanel_MouseHover;
            DrawingPanel.MouseLeave += DrawingPanel_MouseLeave;
            DrawingPanel.MouseMove += DrawingPanel_MouseMove;
            DrawingPanel.MouseUp += DrawingPanel_MouseUp;
            DrawingPanel.MouseWheel += GViewer_MouseWheel;
            DrawingPanel.Move += GViewer_Move;
            DrawingPanel.KeyDown += DrawingPanel_KeyDown;
            DrawingPanel.KeyPress += DrawingPanel_KeyPress;
            DrawingPanel.KeyUp += DrawingPanel_KeyUp;
            DrawingPanel.DoubleClick += DrawingPanel_DoubleClick;
            DrawingPanel.SizeChanged += DrawingPanelSizeChanged;
            this.SizeChanged += GViewer_SizeChanged;


        }

        void GViewer_SizeChanged(object sender, EventArgs e) {
            panel.Invalidate();
        }

        Rectangle prevPanelClientRectangle;
        void DrawingPanelSizeChanged(object sender, EventArgs e) {            
            if (originalGraph == null || panel.ClientRectangle.Width<2 || panel.ClientRectangle.Height<2) return;
            double oldFitFactor = Math.Min(prevPanelClientRectangle.Width/originalGraph.Width, prevPanelClientRectangle.Height/originalGraph.Height);
            var center = new Point(prevPanelClientRectangle.Width / 2.0, prevPanelClientRectangle.Height / 2.0);
            if (transformation != null) {
                var centerOnSource = transformation.Inverse*center;
                SetTransformOnScaleAndCenter(GetFitScale()*CurrentScale/oldFitFactor, centerOnSource);
            }
            prevPanelClientRectangle = panel.ClientRectangle;
            
        }

        void DrawingPanel_DoubleClick(object sender, EventArgs e){
            OnDoubleClick(e);
        }


        void DisableDrawingLayoutEditor(){
            if (LayoutEditor != null){
                LayoutEditor.DetouchFromViewerEvents();
                LayoutEditor = null;
            }
        }

        void InitDrawingLayoutEditor(){
            if (LayoutEditor == null){
                LayoutEditor = new LayoutEditor(this);
                LayoutEditor.ChangeInUndoRedoList += DrawingLayoutEditor_ChangeInUndoRedoList;
            }
            undoButton.ImageIndex = (int) ImageEnum.UndoDisabled;
            redoButton.ImageIndex = (int) ImageEnum.RedoDisabled;
        }


        void DrawingLayoutEditor_ChangeInUndoRedoList(object sender, EventArgs args) {
            if (InvokeRequired)
                Invoke((Invoker) FixUndoRedoButtons);
            else
                FixUndoRedoButtons();
        }

        void FixUndoRedoButtons() {
            undoButton.ImageIndex = UndoImageIndex();
            redoButton.ImageIndex = RedoImageIndex();
        }

        int RedoImageIndex(){
            return (int) (LayoutEditor.CanRedo ? ImageEnum.Redo : ImageEnum.RedoDisabled);
        }

        int UndoImageIndex(){
            return (int) (LayoutEditor.CanUndo ? ImageEnum.Undo : ImageEnum.UndoDisabled);
        }

        /// <summary>
        /// Set context menu strip for DrawingPanel
        /// </summary>
        /// <param name="contexMenuStrip"></param>
        public void SetContextMenumStrip(ContextMenuStrip contexMenuStrip)
        {
            DrawingPanel dp = this.DrawingPanel as DrawingPanel;

            dp.SetCms(contexMenuStrip);
        }

        void DrawingPanel_KeyUp(object sender, KeyEventArgs e){
            OnKeyUp(e);
        }

        void DrawingPanel_KeyPress(object sender, KeyPressEventArgs e){
            OnKeyPress(e);
        }

        void DrawingPanel_KeyDown(object sender, KeyEventArgs e){
            OnKeyDown(e);
        }

        void GViewer_Move(object sender, EventArgs e){
            OnMove(e);
        }

        void GViewer_MouseWheel(object sender, MouseEventArgs e){
           
            if (zoomWhenMouseWheelScroll){
                if (OriginalGraph == null) return;
                var pointSrc = ScreenToSource(e.X, e.Y);
                const double zoomFractionLocal = 0.9;
                var zoomInc = e.Delta < 0 ? zoomFractionLocal : 1.0 / zoomFractionLocal;
                var scale = CurrentScale*zoomInc;
                var d = OriginalGraph.BoundingBox.Diagonal;
                if (d*scale < 5 || d*scale > HugeDiagonal)
                        return;
                

                var dx = e.X - pointSrc.X*scale;
                var dy = e.Y + pointSrc.Y*scale;
                Transform[0, 0] = scale;
                Transform[1, 1] = -scale;
                Transform[0, 2] = dx;
                Transform[1, 2] = dy;
                panel.Invalidate();
            }
            OnMouseWheel(e);
        }

/*
        double FindZoomIncrementForWheel(double zoomFraction, MouseEventArgs e) {
            double xs = FindZoomIncrementForWheelX(e);
            double ys = FindZoomIncrementForWheelY(e);
            double s = 1/Math.Max(xs, ys);
            return Math.Min(zoomFraction, s);
        }
*/

/*
        double FindZoomIncrementForWheelY(MouseEventArgs args){
            var y = args.Y;
            double ph = PanelHeight;
            double gh = originalGraph.Height * LocalScale;
            if (scaledDown)
                gh *= scaleDownCoefficient;
            return Math.Max (y/(y - (ph - gh) / 2), (ph - y) / ((ph + gh) / 2 - y));
        }
*/

/*
        double FindZoomIncrementForWheelX(MouseEventArgs args){
            var x = args.X;
            double pw = PanelWidth;
            double gw = originalGraph.Width*LocalScale;
            if (scaledDown)
                gw *= scaleDownCoefficient;
            return Math.Max(x/(x - (pw - gw) / 2), (pw - x) / ((pw + gw) / 2 - x));           
        }
*/


        void DrawingPanel_MouseUp(object sender, MouseEventArgs e){
            OnMouseUp(e);
        }

        void DrawingPanel_MouseMove(object sender, MouseEventArgs e){
            OnMouseMove(e);
        }

        void DrawingPanel_MouseLeave(object sender, EventArgs e){
            OnMouseLeave(e);
        }

        void DrawingPanel_MouseHover(object sender, EventArgs e){
            OnMouseHover(e);
        }

        void DrawingPanel_MouseEnter(object sender, EventArgs e){
            OnMouseEnter(e);
        }

        void DrawingPanel_MouseDown(object sender, MouseEventArgs e){
            OnMouseDown(e);
        }

        void DrawingPanel_MouseCaptureChanged(object sender, EventArgs e){
            OnMouseCaptureChanged(e);
        }

        void DrawingPanel_MouseDoubleClick(object sender, MouseEventArgs e){
            OnMouseDoubleClick(e);
        }

        internal void Hit(MouseEventArgs args){
            if (args.Button == MouseButtons.None)
                UnconditionalHit(args, EntityFilterDelegate);
        }

        #region Nested type: ImageEnum

        enum ImageEnum{
            ZoomIn,
            ZoomOut,
            WindowZoom,
            Hand,
            Forward,
            ForwardDis,
            Backward,
            BackwardDis,
            Save,
            Undo,
            Redo,
            Print,
            Open,
            UndoDisabled,
            RedoDisabled
        }

        #endregion

        /// <summary>
        /// maps a screen point to the graph
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Point ScreenToSource(double x, double y){
            return ScreenToSource(new Point(x, y));
        }
    }
}