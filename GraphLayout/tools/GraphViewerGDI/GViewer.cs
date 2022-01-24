using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Prototype.LayoutEditing;
using Microsoft.Msagl.Prototype.Ranking;
using Microsoft.Msagl.Routing;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Label = Microsoft.Msagl.Drawing.Label;
using MouseButtons = System.Windows.Forms.MouseButtons;
using Node = Microsoft.Msagl.Core.Layout.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = Microsoft.Msagl.Core.DataStructures.Size;

namespace Microsoft.Msagl.GraphViewerGdi {
  /// <summary>
  /// Summary description for DOTViewer.
  /// </summary>
  [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
  public sealed partial class GViewer : UserControl {
    #region Support for layout editing

    double arrowheadLength = 10;

    LayoutEditor layoutEditor;
    ToolStripButton edgeInsertButton;
    bool insertingEdge;
    ToolStripButton layoutSettingsButton;
    ToolStripButton redoButton;
    ToolStripButton undoButton;
    internal ToolStripButton zoomin;
    internal ToolStripButton zoomout;
    internal ToolStripButton windowZoomButton;
    internal ToolStripButton panButton;
    private ToolStripButton homeZoomButton;

    /// <summary>
    /// gets or sets the drawing layout editor
    /// </summary>
    public LayoutEditor LayoutEditor {
      get { return layoutEditor; }
      set { layoutEditor = value; }
    }

    /// <summary>
    /// if is set to true then the mouse left click on a node and dragging the cursor to 
    /// another node will create an edge and add it to the graph
    /// </summary>
    public bool InsertingEdge {
      get { return insertingEdge; }
      set {
        insertingEdge = value;
        if (LayoutEditor != null) {
          if (value)
            EntityFilterDelegate = EdgeFilter;
          else
            EntityFilterDelegate = null;
        }
      }
    }

    /// <summary>
    /// the length of arrowheads for newly inserted edges
    /// </summary>
    public double ArrowheadLength {
      get {
        if (Graph != null && Graph.LayoutAlgorithmSettings is SugiyamaLayoutSettings)
          return Math.Min(arrowheadLength, Graph.Attr.LayerSeparation / 2);
        return arrowheadLength;
      }
      set { arrowheadLength = value; }
    }

    /// <summary>
    /// creates the port visual if it does not exist, and sets the port location
    /// </summary>
    /// <param name="portLocation"></param>
    public void SetSourcePortForEdgeRouting(Point portLocation) {
      var box = new Core.Geometry.Rectangle(portLocation);
      box.Pad(UnderlyingPolylineCircleRadius);

      if (SourcePortIsPresent) {
        var prevBox = new Core.Geometry.Rectangle(SourcePortLocation);
        prevBox.Pad(UnderlyingPolylineCircleRadius);
        box.Add(prevBox);
      }

      SourcePortIsPresent = true;
      SourcePortLocation = portLocation;

      panel.Invalidate(MapSourceRectangleToScreenRectangle(box));
    }

    internal Point SourcePortLocation { get; private set; }

    internal bool SourcePortIsPresent { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="portLocation"></param>
    public void SetTargetPortForEdgeRouting(Point portLocation) {
      TargetPortIsPresent = true;
      TargetPortLocation = portLocation;
    }

    internal Point TargetPortLocation { get; private set; }

    internal bool TargetPortIsPresent { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public void RemoveSourcePortEdgeRouting() {
      if (SourcePortIsPresent) {
        var prevBox = new Core.Geometry.Rectangle(SourcePortLocation);
        prevBox.Pad(UnderlyingPolylineCircleRadius);
        panel.Invalidate(MapSourceRectangleToScreenRectangle(prevBox));
        SourcePortIsPresent = false;
      }
    }
    /// <summary>
    /// 
    /// </summary>
    public void RemoveTargetPortEdgeRouting() {
      TargetPortIsPresent = false;
    }


    void ClearLayoutEditor() {
      if (LayoutEditor != null)
        DisableDrawingLayoutEditor();
      InitDrawingLayoutEditor();
    }

    #endregion

    bool asyncLayout;
    ToolStripButton backwardButton;
    BBNode bBNode;
    bool buildHitTree = true;
    IContainer components;
    DGraph dGraph;
    string fileName = "";
    ToolStripButton forwardButton;
    EventHandler<MsaglMouseEventArgs> iEditViewerMouseDown;
    EventHandler<MsaglMouseEventArgs> iEditViewerMouseMove;
    EventHandler<MsaglMouseEventArgs> iEditViewerMouseUp;
    ImageList imageList;
    double looseOffsetForRouting = 1.0 / 8 * 2;
    double mouseHitDistance = 0.05;
    bool needToCalculateLayout = true;
    double offsetForRelaxingInRouting = 0.6;
    ToolStripButton openButton;
    Graph originalGraph;
    double paddingForEdgeRouting = 8;
    const string PanButtonDisabledToolTipText = "Pan, is disabled now";
    ToolStripButton print;
    ToolStripButton saveButton;
    double tightOffsetForRouting = 1.0 / 8;
    ToolStrip toolbar;
    ToolTip toolTip1;
    const double VisibleWidth = 0.05; //inches
    bool wasMinimized;
    const string WindowZoomButtonToolTipText = "Zoom in by dragging a rectangle";
    double zoomWindowThreshold = 0.05; //inches

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    void InitializeComponent() {
      this.components = new System.ComponentModel.Container();
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GViewer));
      this.imageList = new System.Windows.Forms.ImageList(this.components);
      this.toolbar = new System.Windows.Forms.ToolStrip();
      this.homeZoomButton = new System.Windows.Forms.ToolStripButton();
      this.zoomin = new System.Windows.Forms.ToolStripButton();
      this.zoomout = new System.Windows.Forms.ToolStripButton();
      this.windowZoomButton = new System.Windows.Forms.ToolStripButton();
      this.panButton = new System.Windows.Forms.ToolStripButton();
      this.backwardButton = new System.Windows.Forms.ToolStripButton();
      this.forwardButton = new System.Windows.Forms.ToolStripButton();
      this.saveButton = new System.Windows.Forms.ToolStripButton();
      this.undoButton = new System.Windows.Forms.ToolStripButton();
      this.redoButton = new System.Windows.Forms.ToolStripButton();
      this.openButton = new System.Windows.Forms.ToolStripButton();
      this.print = new System.Windows.Forms.ToolStripButton();
      this.layoutSettingsButton = new System.Windows.Forms.ToolStripButton();
      this.edgeInsertButton = new System.Windows.Forms.ToolStripButton();
      this.SuspendLayout();
      // 
      // imageList
      // 
      this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
      this.imageList.TransparentColor = System.Drawing.Color.Transparent;
      this.imageList.Images.SetKeyName(0, "");
      this.imageList.Images.SetKeyName(1, "");
      this.imageList.Images.SetKeyName(2, "zoom.bmp");
      this.imageList.Images.SetKeyName(3, "");
      this.imageList.Images.SetKeyName(4, "");
      this.imageList.Images.SetKeyName(5, "");
      this.imageList.Images.SetKeyName(6, "");
      this.imageList.Images.SetKeyName(7, "");
      this.imageList.Images.SetKeyName(8, "");
      this.imageList.Images.SetKeyName(9, "undo.bmp");
      this.imageList.Images.SetKeyName(10, "redo.bmp");
      this.imageList.Images.SetKeyName(11, "");
      this.imageList.Images.SetKeyName(12, "openfolderHS.png");
      this.imageList.Images.SetKeyName(13, "disabledUndo.bmp");
      this.imageList.Images.SetKeyName(14, "disabledRedo.bmp");
      this.imageList.Images.SetKeyName(15, "layoutMethodBlue.ico");
      this.imageList.Images.SetKeyName(16, "edge.jpg");
      this.imageList.Images.SetKeyName(17, "home.bmp");
      // 
      // toolbar
      // 
      this.toolbar.Items.AddRange(new System.Windows.Forms.ToolStripButton[] {
            this.homeZoomButton,
            this.zoomin,
            this.zoomout,
            this.windowZoomButton,
            this.panButton,
            this.backwardButton,
            this.forwardButton,
            this.saveButton,
            this.undoButton,
            this.redoButton,
            this.openButton,
            this.print,
            this.layoutSettingsButton,
            this.edgeInsertButton});
      this.toolbar.Size = new System.Drawing.Size(22, 23);
      this.toolbar.ImageList = this.imageList;
      this.toolbar.Location = new System.Drawing.Point(0, 0);
      this.toolbar.Name = "toolbar";
      this.toolbar.ShowItemToolTips = true;
      this.toolbar.Size = new System.Drawing.Size(624, 28);
      this.toolbar.TabIndex = 2;
      this.toolbar.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.ToolBarButtonClick);
      // 
      // homeZoomButton
      // 
      this.homeZoomButton.ImageIndex = 17;
      this.homeZoomButton.Name = "homeZoomButton";
      // 
      // zoomin
      // 
      this.zoomin.ImageIndex = 0;
      this.zoomin.Name = "zoomin";
      this.zoomin.ToolTipText = "Zoom In";
      // 
      // zoomout
      // 
      this.zoomout.ImageIndex = 1;
      this.zoomout.Name = "zoomout";
      this.zoomout.ToolTipText = "Zoom Out";
      // 
      // windowZoomButton
      // 
      this.windowZoomButton.ImageKey = "zoom.bmp";
      this.windowZoomButton.Name = "windowZoomButton";
      this.windowZoomButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
      this.windowZoomButton.CheckOnClick = true;
      this.windowZoomButton.ToolTipText = "Zoom in to the rectangle";
      this.windowZoomButton.CheckedChanged += new EventHandler(this.ToolBarButtonCheckChanged);
      // 
      // panButton
      // 
      this.panButton.ImageIndex = 3;
      this.panButton.Name = "panButton";
      this.panButton.CheckOnClick = true;
      this.panButton.ToolTipText = "Pan";
      this.panButton.CheckedChanged += new EventHandler(this.ToolBarButtonCheckChanged);
      // 
      // backwardButton
      // 
      this.backwardButton.ImageIndex = 6;
      this.backwardButton.Name = "backwardButton";
      this.backwardButton.ToolTipText = "Backward";
      // 
      // forwardButton
      // 
      this.forwardButton.ImageIndex = 4;
      this.forwardButton.Name = "forwardButton";
      // 
      // saveButton
      // 
      this.saveButton.ImageIndex = 8;
      this.saveButton.Name = "saveButton";
      this.saveButton.ToolTipText = "Save the graph or the drawing";
      // 
      // undoButton
      // 
      this.undoButton.ImageIndex = 9;
      this.undoButton.Name = "undoButton";
      // 
      // redoButton
      // 
      this.redoButton.ImageIndex = 10;
      this.redoButton.Name = "redoButton";
      // 
      // openButton
      // 
      this.openButton.ImageIndex = 12;
      this.openButton.Name = "openButton";
      this.openButton.ToolTipText = "Load a graph from a \".msagl\" file";
      // 
      // print
      // 
      this.print.ImageIndex = 11;
      this.print.Name = "print";
      this.print.ToolTipText = "Print the current view";
      // 
      // layoutSettingsButton
      // 
      this.layoutSettingsButton.ImageIndex = 15;
      this.layoutSettingsButton.Name = "layoutSettingsButton";
      // 
      // edgeInsertButton
      // 
      this.edgeInsertButton.ImageIndex = 16;
      this.edgeInsertButton.CheckOnClick = true;
      this.edgeInsertButton.Name = "edgeInsertButton";
      this.edgeInsertButton.ToolTipText = "Edge insertion";
      this.edgeInsertButton.CheckedChanged += new EventHandler(this.ToolBarButtonCheckChanged);
      // 
      // GViewer
      // 
      this.AutoScroll = true;
      this.Controls.Add(this.toolbar);
      this.Name = "GViewer";
      this.Size = new System.Drawing.Size(624, 578);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    internal EntityFilterDelegate EntityFilterDelegate { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public double CurrentScale {
      get { return Transform[0, 0]; }
    }

    /// <summary>
    /// support for mouse selection 
    /// </summary>
    public bool BuildHitTree {
      get { return buildHitTree; }
      set { buildHitTree = value; }
    }

    internal DGraph DGraph {
      get { return dGraph; }
      set { dGraph = value; }
    }

    internal Graph OriginalGraph {
      get { return originalGraph; }
      set {
        originalGraph = value;
        //this.nodeDragger = new DrawingNodeDragger(originalGraph);
      }
    }

    /// <summary>
    /// If set to false no layout is calculated. It is presumed that the layout is precalculated.
    /// </summary>
    public bool NeedToCalculateLayout {
      get { return needToCalculateLayout; }
      set { needToCalculateLayout = value; }
    }

    internal BBNode BbNode {
      get {
        if (bBNode == null) {
          DGraph.BuildBBHierarchy();
          bBNode = DGraph.BbNode;
        }
        return bBNode;
      }
      set { bBNode = value; }
    }

    /// <summary>
    /// the last MSAGL file name used when saving-opening an MSAGL file
    /// </summary>
    public string FileName {
      get { return fileName; }
      set { fileName = value; }
    }

    /// <summary>
    /// Controls the pan button.
    /// </summary>
    public bool PanButtonPressed {
      get { return panButton.Checked; }
      set { panButton.Checked = value; }
    }

    /// <summary>
    /// Controls the window zoom button.
    /// </summary>
    public bool WindowZoomButtonPressed {
      get { return windowZoomButton.Checked; }
      set { windowZoomButton.Checked = value; }
    }

    /// <summary>
    /// If the mininal side of the zoom window is shorter than the threshold then zoom 
    /// does not take place
    /// </summary>
    public double ZoomWindowThreshold {
      get { return zoomWindowThreshold; }
      set { zoomWindowThreshold = value; }
    }

    /// <summary>
    /// SelectedObject can be detected if the distance in inches between it and 
    /// the cursor is less than MouseHitDistance
    /// </summary>
    public double MouseHitDistance {
      get { return mouseHitDistance; }
      set { mouseHitDistance = value; }
    }

    /// <summary>
    /// Returns layouted Microsoft.Msagl.Drawing.Graph
    /// </summary>
    public Graph GraphWithLayout {
      get { return DGraph.DrawingGraph; }
    }

    /// <summary>
    /// 
    /// </summary>
    public double TightOffsetForRouting {
      get { return tightOffsetForRouting; }
      set { tightOffsetForRouting = value; }
    }

    /// <summary>
    /// 
    /// </summary>
    public double LooseOffsetForRouting {
      get { return looseOffsetForRouting; }
      set { looseOffsetForRouting = value; }
    }

    /// <summary>
    /// 
    /// </summary>
    public double OffsetForRelaxingInRouting {
      get { return offsetForRelaxingInRouting; }
      set { offsetForRelaxingInRouting = value; }
    }

    #region IViewer Members

    /// <summary>
    /// The event raised after changing the graph
    /// </summary>
    public event EventHandler GraphChanged;

    /// <summary>
    /// maps a point from the screen to the graph surface
    /// </summary>
    /// <param name="screenPoint"></param>
    /// <returns></returns>
    public Point ScreenToSource(Point screenPoint) {
      return Transform.Inverse * screenPoint;
    }

    /// <summary>
    /// Setting the Graph property shows the graph in the control
    /// </summary>
    /// 
    public Graph Graph {
      get { return OriginalGraph; }
      set {
        DGraph = null;
        ClearBackwardForwardList();
        if (value != null) {
          if (!asyncLayout) {
            OriginalGraph = value;
            try {
              if (NeedToCalculateLayout) {
                OriginalGraph.GeometryGraph = null;
                LayoutAndCreateDGraph();
                InitiateDrawing();
              }
              else {
                InitiateDrawing();
                DGraph = DGraph.CreateDGraphFromPrecalculatedDrawingGraph(OriginalGraph, this);
              }
            }
            catch (OperationCanceledException) {
              Graph = null;
            }
          }
          else
            SetGraphAsync(value);
        }
        else {
          OriginalGraph = null;
          DrawingPanel.Invalidate();
        }
        if (InsertingEdge)
          layoutEditor.PrepareForEdgeDragging();

        GraphChanged?.Invoke(this, null);        
      }
    }

    private void ClearBackwardForwardList() {
      listOfViewInfos.Clear();
      ForwardEnabled = listOfViewInfos.ForwardAvailable;
      BackwardEnabled = listOfViewInfos.BackwardAvailable;
    }

    /// <summary>
    /// returns the object under the cursor
    /// </summary>
    public IViewerObject ObjectUnderMouseCursor {
      get {
        if (MousePositonWhenSetSelectedObject != MousePosition)
          UnconditionalHit(null, EntityFilterDelegate);
        return selectedDObject;
      }
    }

    //Microsoft.Msagl.Drawing.DrawingObject DObjectToDrawingObject(DObject drObj) {
    //    return this.DGraph.drawingObjectsToDObjects[drObj as DrawingObject] as Microsoft.Msagl.Drawing.IDraggableObject;            
    //}

    /// <summary>
    /// The radius of a circle around an underlying polyline corner
    /// </summary>
    public double UnderlyingPolylineCircleRadius {
      get { return UnderlyingPolylineRadiusWithNoScale / CurrentScale; }
    }

    internal static double UnderlyingPolylineRadiusWithNoScale {
      get { return dpix * 0.05; }
    }

    /// <summary>
    /// Forces redraw of objectToInvalidate
    /// </summary>
    /// <param name="objectToInvalidate"></param>
    public void Invalidate(IViewerObject objectToInvalidate) {
      if (objectToInvalidate is DObject dObject) {
        dObject.Invalidate();
        ClearBoundingBoxHierarchy();
        Core.Geometry.Rectangle box = dObject.RenderedBox; //copying aside he previous rendering box
        dObject.UpdateRenderedBox();
        box.Add(dObject.RenderedBox);
        //this is now the box to invalidate; to erase the old object and to render the new one

        panel.Invalidate(MapSourceRectangleToScreenRectangle(box));
      }
    }

    /// <summary>
    /// return ModifierKeys
    /// </summary>
    ModifierKeys IViewer.ModifierKeys {
      get {
        switch (ModifierKeys) {
          case Keys.Control:
          case Keys.ControlKey:
            return Drawing.ModifierKeys.Control;
          case Keys.Shift:
          case Keys.ShiftKey:
            return Drawing.ModifierKeys.Shift;
          case Keys.Alt:
            return Drawing.ModifierKeys.Alt;
          default:
            return Drawing.ModifierKeys.None;
        }
      }
    }

    /// <summary>
    /// Maps a screen point to the graph surface point
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public Point ScreenToSource(MsaglMouseEventArgs e) {
      if (e != null)
        return ScreenToSource(e.X, e.Y);
      return new Point();
    }

    /// <summary>
    /// enumerates over all draggable entities
    /// </summary>
    public IEnumerable<IViewerObject> Entities {
      get {
        if (DGraph != null) {
          foreach (IViewerObject obj in DGraph.Entities)
            yield return obj;
        }
      }
    }

    /// <summary>
    /// number of dots per inch horizontally
    /// </summary>
    public double DpiX {
      get { return dpix; }
    }

    /// <summary>
    /// number of dots per inch vertically
    /// </summary>
    public double DpiY {
      get { return dpiy; }
    }
    /// <summary>
    /// 
    /// </summary>
    event EventHandler<MsaglMouseEventArgs> IViewer.MouseDown {
      add { iEditViewerMouseDown += value; }
      remove { iEditViewerMouseDown -= value; }
    }

    /// <summary>
    /// 
    /// </summary>
    event EventHandler<MsaglMouseEventArgs> IViewer.MouseMove {
      add { iEditViewerMouseMove += value; }
      remove { iEditViewerMouseMove -= value; }
    }

    event EventHandler<MsaglMouseEventArgs> IViewer.MouseUp {
      add { iEditViewerMouseUp += value; }
      remove { iEditViewerMouseUp -= value; }
    }

    /// <summary>
    /// A method of IEditViewer
    /// </summary>
    /// <param name="changedObjects"></param>
    public void OnDragEnd(IEnumerable<IViewerObject> changedObjects) {
      DGraph.UpdateBBoxHierarchy(changedObjects);
    }

    void IViewer.Invalidate() {
      panel.Invalidate();
    }

    /// <summary>
    /// The scale dependent width of an edited curve that should be clearly visible.
    /// Used in the default entity editing.
    /// </summary>
    public double LineThicknessForEditing {
      get { return DpiX * VisibleWidth / CurrentScale; }
    }

    /// <summary>
    /// Pops up a pop up menu with a menu item for each couple, the string is the title and the delegate is the callback
    /// </summary>
    /// <param name="menuItems"></param>
    public void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems) {
      var contextMenu = new ContextMenuStrip();
      foreach (var menuItem in menuItems)
        contextMenu.Items.Add(CreateMenuItem(menuItem.Item1, menuItem.Item2));
      contextMenu.Show(this, PointToClient(MousePosition));
    }

        /// <summary>
        /// adding a node to the graph with the undo support
        /// The node boundary curve should have (0,0) as its internal point.
        /// The curve will be moved the the node center.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="registerForUndo"></param>
        public void AddNode(IViewerNode node, bool registerForUndo) {
            var dNode = node as DNode;
            DrawingNode drawingNode = dNode.DrawingNode;

            var viewer = this as IViewer;

            DGraph.AddNode(dNode);
            Graph.AddNode(drawingNode);
            Graph.GeometryGraph.Nodes.Add(drawingNode.GeometryNode);

            foreach (DEdge e in dNode.outEdges) {
                e.Target.inEdges.Add(e);
                e.Target.DrawingNode.AddInEdge(e.DrawingEdge);
                e.Target.DrawingNode.GeometryNode.AddInEdge(e.DrawingEdge.GeometryEdge);
            }
            foreach (DEdge e in dNode.inEdges) {
                e.Source.outEdges.Add(e);
                e.Source.DrawingNode.AddOutEdge(e.DrawingEdge);
                e.Source.DrawingNode.GeometryNode.AddOutEdge(e.DrawingEdge.GeometryEdge);
            }

            viewer.Invalidate(node);
            foreach (DEdge e in Edges(dNode)) {
                DGraph.Edges.Add(e);
                Graph.AddPrecalculatedEdge(e.DrawingEdge);
                Graph.GeometryGraph.Edges.Add(e.DrawingEdge.GeometryEdge);
                viewer.Invalidate(e);
            }
            layoutEditor.AttachLayoutChangeEvent(node);
            if (registerForUndo) {
                layoutEditor.RegisterNodeAdditionForUndo(node);
                Core.Geometry.Rectangle bounds = Graph.GeometryGraph.BoundingBox;
                bounds.Add(drawingNode.BoundingBox.LeftTop);
                bounds.Add(drawingNode.BoundingBox.RightBottom);
                Graph.GeometryGraph.BoundingBox = bounds;
                layoutEditor.CurrentUndoAction.GraphBoundingBoxAfter = Graph.BoundingBox;
            }
            BbNode = null;
            DGraph.BbNode = null;
            DGraph.BuildBBHierarchy();
            viewer.Invalidate();

        }


    ///// <summary>
    ///// 
    ///// </summary>
    ///// <param name="source"></param>
    ///// <param name="target"></param>
    ///// <param name="registerForUndo"></param>
    ///// <returns></returns>
    public Drawing.Edge AddEdge(Drawing.Node source, Drawing.Node target, bool registerForUndo) {
      Debug.Assert(Graph.FindNode(source.Id) == source);
      Debug.Assert(Graph.FindNode(target.Id) == target);

      Drawing.Edge drawingEdge = Graph.AddEdge(source.Id, target.Id);
      drawingEdge.Label = new Label();
      var geometryEdge = drawingEdge.GeometryEdge = new Microsoft.Msagl.Core.Layout.Edge();
      geometryEdge.GeometryParent = this.Graph.GeometryGraph;

      var a = source.GeometryNode.Center;
      var b = target.GeometryNode.Center;
      if (source == target) {
        Site start = new Site(a);
        Site end = new Site(b);
        var mid1 = source.GeometryNode.Center;
        mid1.X += (source.GeometryNode.BoundingBox.Width / 3 * 2);
        var mid2 = mid1;
        mid1.Y -= source.GeometryNode.BoundingBox.Height / 2;
        mid2.Y += source.GeometryNode.BoundingBox.Height / 2;
        Site mid1s = new Site(mid1);
        Site mid2s = new Site(mid2);
        start.Next = mid1s;
        mid1s.Previous = start;
        mid1s.Next = mid2s;
        mid2s.Previous = mid1s;
        mid2s.Next = end;
        end.Previous = mid2s;
        geometryEdge.UnderlyingPolyline = new SmoothedPolyline(start);
        geometryEdge.Curve = geometryEdge.UnderlyingPolyline.CreateCurve();
      }
      else {
        Site start = new Site(a);
        Site end = new Site(b);
        Site mids = new Site(a * 0.5 + b * 0.5);
        start.Next = mids;
        mids.Previous = start;
        mids.Next = end;
        end.Previous = mids;
        geometryEdge.UnderlyingPolyline = new SmoothedPolyline(start);
        geometryEdge.Curve = geometryEdge.UnderlyingPolyline.CreateCurve();
      }

      geometryEdge.Source = drawingEdge.SourceNode.GeometryNode;
      geometryEdge.Target = drawingEdge.TargetNode.GeometryNode;
      geometryEdge.EdgeGeometry.TargetArrowhead = new Arrowhead() { Length = drawingEdge.Attr.ArrowheadLength };
      Arrowheads.TrimSplineAndCalculateArrowheads(geometryEdge, geometryEdge.Curve, true, true);


      IViewerEdge ve;
      AddEdge(ve = CreateEdgeWithGivenGeometry(drawingEdge), registerForUndo);
      layoutEditor.AttachLayoutChangeEvent(ve);
      return drawingEdge;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="registerForUndo"></param>
    public void AddEdge(IViewerEdge edge, bool registerForUndo) {
      if (registerForUndo) layoutEditor.RegisterEdgeAdditionForUndo(edge);

      var dEdge = edge as DEdge;
      var drawingEdge = edge.DrawingObject as DrawingEdge;
      Edge geomEdge = drawingEdge.GeometryEdge;

      //the edge has to be disconnected from the graph
      Debug.Assert(DGraph.Edges.Contains(dEdge) == false);
      //Debug.Assert(Graph.Edges.Contains(drawingEdge) == false);
      Debug.Assert(Graph.GeometryGraph.Edges.Contains(geomEdge) == false);

      DGraph.Edges.Add(dEdge);
      Graph.AddPrecalculatedEdge(drawingEdge);
      Graph.GeometryGraph.Edges.Add(geomEdge);


      Core.Geometry.Rectangle bounds = Graph.GeometryGraph.BoundingBox;
      bounds.Add(drawingEdge.GeometryEdge.Curve.BoundingBox.LeftTop);
      bounds.Add(drawingEdge.GeometryEdge.Curve.BoundingBox.RightBottom);
      Graph.GeometryGraph.BoundingBox = bounds;

      if (registerForUndo) layoutEditor.CurrentUndoAction.GraphBoundingBoxAfter = Graph.BoundingBox;


      BbNode = null;
      var source = edge.Source as DNode;
      var target = edge.Target as DNode;
      //the edge has to be disconnected from the graph
      Debug.Assert(source.outEdges.Contains(dEdge) == false);
      Debug.Assert(target.inEdges.Contains(dEdge) == false);
      Debug.Assert(source.selfEdges.Contains(dEdge) == false);


      if (source != target) {
        source.AddOutEdge(dEdge);
        target.AddInEdge(dEdge);

        source.DrawingNode.AddOutEdge(drawingEdge);
        target.DrawingNode.AddInEdge(drawingEdge);

        source.DrawingNode.GeometryNode.AddOutEdge(geomEdge);
        target.DrawingNode.GeometryNode.AddInEdge(geomEdge);
      }
      else {
        source.AddSelfEdge(dEdge);
        source.DrawingNode.AddSelfEdge(drawingEdge);
        source.DrawingNode.GeometryNode.AddSelfEdge(geomEdge);
      }

      DGraph.BbNode = null;
      DGraph.BuildBBHierarchy();

      Invalidate();

      if (EdgeAdded != null)
        EdgeAdded(dEdge.DrawingEdge, new EventArgs());
    }

    /// <summary>
    /// removes a node from the graph with the undo support
    /// </summary>
    /// <param name="node"></param>
    /// <param name="registerForUndo"></param>
    public void RemoveNode(IViewerNode node, bool registerForUndo) {
      if (registerForUndo)
        layoutEditor.RegisterNodeForRemoval(node);

      RemoveNodeFromAllGraphs(node);

      BbNode = null;
      DGraph.BbNode = null;
      DGraph.BuildBBHierarchy();

      Invalidate();
    }

    /// <summary>
    /// removes an edge from the graph with the undo support
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="registerForUndo"></param>
    public void RemoveEdge(IViewerEdge edge, bool registerForUndo) {
      var de = edge as DEdge;

      if (registerForUndo)
        layoutEditor.RegisterEdgeRemovalForUndo(edge);

      Graph.RemoveEdge(de.DrawingEdge);


      DGraph.Edges.Remove(de);
      if (de.Source != de.Target) {
        de.Source.RemoveOutEdge(de);
        de.Target.RemoveInEdge(de);
      }
      else
        de.Source.RemoveSelfEdge(de);


      BbNode = null;
      DGraph.BbNode = null;
      DGraph.BuildBBHierarchy();

      Invalidate();

      if (EdgeRemoved != null)
        EdgeRemoved(de.DrawingEdge, EventArgs.Empty);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startingPoint"></param>
    public void StartDrawingRubberLine(Point startingPoint) {
      panel.MarkTheStartOfRubberLine(startingPoint);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    public void DrawRubberLine(MsaglMouseEventArgs args) {
      panel.DrawRubberLine(args);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="point"></param>
    public void DrawRubberLine(Point point) {
      panel.DrawRubberLine(point);
    }

    /// <summary>
    /// 
    /// </summary>
    public void StopDrawingRubberLine() {
      panel.StopDrawRubberLine();
    }

    /// <summary>
    /// routes an edge and returns the corresponding edge of the viewer
    /// </summary>
    /// <returns></returns>
    public IViewerEdge RouteEdge(DrawingEdge drawingEdge) {
      drawingEdge.Label = new Label();
      Edge geometryEdge = drawingEdge.GeometryEdge = new Edge();
      if (drawingEdge.Attr.ArrowheadAtSource != ArrowStyle.NonSpecified &&
          drawingEdge.Attr.ArrowheadAtSource != ArrowStyle.None)
        geometryEdge.EdgeGeometry.SourceArrowhead = new Arrowhead { Length = drawingEdge.Attr.ArrowheadLength };
      if (drawingEdge.Attr.ArrowheadAtSource != ArrowStyle.None)
        geometryEdge.EdgeGeometry.TargetArrowhead = new Arrowhead { Length = drawingEdge.Attr.ArrowheadLength };

      geometryEdge.GeometryParent = Graph.GeometryGraph;
      geometryEdge.Source = drawingEdge.SourceNode.GeometryNode;
      geometryEdge.Target = drawingEdge.TargetNode.GeometryNode;
      geometryEdge.SourcePort = drawingEdge.SourcePort;
      geometryEdge.TargetPort = drawingEdge.TargetPort;
      LayoutHelpers.RouteAndLabelEdges(this.Graph.GeometryGraph, Graph.LayoutAlgorithmSettings,
                                       new[] { geometryEdge }, 0, null);

      var dEdge = new DEdge(DGraph.FindDNode(drawingEdge.SourceNode.Id), DGraph.FindDNode(drawingEdge.TargetNode.Id),
                            drawingEdge, ConnectionToGraph.Disconnected, this);
      dEdge.Label = new DLabel(dEdge, new Label(), this);
      return dEdge;
    }

    /// <summary>
    /// gets the visual graph
    /// </summary>
    public IViewerGraph ViewerGraph {
      get { return DGraph; }
    }

    /// <summary>
    /// creates a viewer node
    /// </summary>
    /// <param name="drawingNode"></param>
    /// <returns></returns>
    public IViewerNode CreateIViewerNode(DrawingNode drawingNode) {
      return DGraph.CreateDNodeAndSetNodeBoundaryCurve(Graph,
                                                       DGraph, drawingNode.GeometryNode, drawingNode, this);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="drawingNode"></param>
    /// <param name="center"></param>
    /// <param name="visualElement">does not play any role here</param>
    /// <returns></returns>
    public IViewerNode CreateIViewerNode(DrawingNode drawingNode, Point center, object visualElement) {
      CreateNodeGeometry(drawingNode, center);
      return CreateIViewerNode(drawingNode);
    }


    void CreateNodeGeometry(DrawingNode node, Point center) {
      double width, height;
      StringMeasure.MeasureWithFont(node.Label.Text, new Font(node.Label.FontName, (float)node.Label.FontSize, (System.Drawing.FontStyle)(int)node.Label.FontStyle), out width,
                                    out height);

      if (node.Label != null) {
        width += 2 * node.Attr.LabelMargin;
        height += 2 * node.Attr.LabelMargin;
      }
      if (width < Graph.Attr.MinNodeWidth)
        width = Graph.Attr.MinNodeWidth;
      if (height < Graph.Attr.MinNodeHeight)
        height = Graph.Attr.MinNodeHeight;


      Node geomNode =
          node.GeometryNode =
          GeometryGraphCreator.CreateGeometryNode(Graph, Graph.GeometryGraph, node, ConnectionToGraph.Disconnected);
      geomNode.BoundaryCurve = NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
      geomNode.BoundaryCurve.Translate(center);
      geomNode.Center = center;
    }


    /// <summary>
    /// sets the edge label
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="label"></param>
    public void SetEdgeLabel(DrawingEdge edge, Label label) {
      //find the edge first
      DEdge de = null;
      label.Owner = edge;
      foreach (DEdge dEdge in DGraph.Edges)
        if (dEdge.DrawingEdge == edge) {
          de = dEdge;
          break;
        }
      Debug.Assert(de != null);
      edge.Label = label;
      double w, h;
      DGraph.CreateDLabel(de, label, out w, out h, this);
      layoutEditor.AttachLayoutChangeEvent(de.Label);
      edge.GeometryEdge.Label = label.GeometryLabel;
      ICurve curve = edge.GeometryEdge.Curve;
      label.GeometryLabel.Center = curve[(curve.ParStart + curve.ParEnd) / 2];
      label.GeometryLabel.GeometryParent = edge.GeometryEdge;
      BbNode = DGraph.BbNode = null;
      Invalidate();
    }

    /// <summary>
    /// the event raised when the object under the mouse cursor changes
    /// </summary>
    public event EventHandler<ObjectUnderMouseCursorChangedEventArgs> ObjectUnderMouseCursorChanged;

    /// <summary>
    /// the padding used to route a new inserted edge around the nodes
    /// </summary>        
    public double PaddingForEdgeRouting {
      get {
        return Graph == null
                   ? paddingForEdgeRouting
                   : Math.Min(paddingForEdgeRouting, Graph.Attr.NodeSeparation / 6);
      }
      set { paddingForEdgeRouting = value; }
    }

    /// <summary>
    /// support for edge routing
    /// </summary>
    /// <param name="edgeGeometry"></param>
    public void DrawRubberEdge(EdgeGeometry edgeGeometry) {
      panel.DrawRubberEdge(edgeGeometry);
    }

    /// <summary>
    /// support for edge routing
    /// </summary>
    public void StopDrawingRubberEdge() {
      panel.StopDrawingRubberEdge();
    }
    /// <summary>
    /// the current transform to the client viewport
    /// </summary>
    public PlaneTransformation
        Transform {
      get {
        if (transformation == null)
          InitTransform();
        return transformation;
      }
      set { transformation = value; }
    }

    void InitTransform() {
      if (originalGraph == null) {
        transformation = PlaneTransformation.UnitTransformation;
        return;
      }
      var scale = GetFitScale();
      var sourceCenter = originalGraph.BoundingBox.Center;
      SetTransformOnScaleAndCenter(scale, sourceCenter);
    }

    internal void SetTransformOnScaleAndCenter(double scale, Point sourceCenter) {
      if (!ScaleIsAcceptable(scale))
        return;

      var dx = PanelWidth / 2.0 - scale * sourceCenter.X;
      var dy = PanelHeight / 2.0 + scale * sourceCenter.Y;
      transformation = new PlaneTransformation(scale, 0, dx, 0, -scale, dy);
    }




    /// <summary>
    /// drawing edge already has its geometry in place
    /// </summary>
    /// <param name="drawingEdge"></param>
    /// <returns></returns>
    public IViewerEdge CreateEdgeWithGivenGeometry(DrawingEdge drawingEdge) {
      drawingEdge.Label = new Label();
      Edge geometryEdge = drawingEdge.GeometryEdge;
      Debug.Assert(geometryEdge != null);
      geometryEdge.GeometryParent = Graph.GeometryGraph;

      var dEdge = new DEdge(DGraph.FindDNode(drawingEdge.SourceNode.Id), DGraph.FindDNode(drawingEdge.TargetNode.Id),
                            drawingEdge, ConnectionToGraph.Disconnected, this);
      dEdge.Label = new DLabel(dEdge, new Label(), this);
      return dEdge;
    }

    #endregion

    #region Asynchronous Layout

    // wrwg: added this for asynchronous layouting


    // The thread running the layout process
    Thread layoutThread;

    // A wait handle for ensuring layouting has started
    readonly EventWaitHandle layoutWaitHandle =
        new EventWaitHandle(false,
                            EventResetMode.AutoReset);

    PlaneTransformation transformation;

    /// <summary>
    /// Whether asynchronous layouting is enabled. Defaults to false.
    /// </summary>
    /// <remarks>
    /// If you set this property to true, setting the <see cref="Graph"/> property
    /// will work asynchronously by starting a thread which does the layout and 
    /// displaying. The coarse progress of layouting can be observed with 
    /// <see cref="AsyncLayoutProgress"/>, and layouting can be aborted with 
    /// <see cref="AbortAsyncLayout"/>.
    /// </remarks>
    public bool AsyncLayout {
      get { return asyncLayout; }
      set { asyncLayout = value; }
    }

    double HugeDiagonal = 10e6;

    /// <summary>
    /// An event which can be subscribed to get notification of layout progress.
    /// </summary>
    public event EventHandler<LayoutProgressEventArgs> AsyncLayoutProgress;

    /// <summary>
    /// Abort an asynchronous layout activity.
    /// </summary>
    public void AbortAsyncLayout() {
      if (layoutThread != null) {
        layoutThread.Abort();
        layoutThread = null;
      }
    }

    // Is called from Graph setter.
    void SetGraphAsync(Graph value) {
      if (layoutThread != null) {
        layoutThread.Abort();
        layoutThread = null;
      }
      layoutThread = new Thread((ThreadStart)delegate {
        var args = new LayoutProgressEventArgs(LayoutProgress.LayingOut, null);
        lock (value) {
          try {
            bool needToCalc = NeedToCalculateLayout;
            layoutWaitHandle.Set();
            OriginalGraph = value;
            if (needToCalc) {
              if (AsyncLayoutProgress != null)
                AsyncLayoutProgress(this, args);
              LayoutAndCreateDGraph();
            }
            else {
              DGraph = DGraph.CreateDGraphFromPrecalculatedDrawingGraph(OriginalGraph, this);
            }
            Invoke(
                (Invoker)
                delegate {
                  if (AsyncLayoutProgress != null) {
                    args.progress = LayoutProgress.Rendering;
                    AsyncLayoutProgress(this, args);
                  }
                  InitiateDrawing();
                  if (AsyncLayoutProgress != null) {
                    args.progress = LayoutProgress.Finished;
                    AsyncLayoutProgress(this, args);
                  }
                });
          }
          catch (ThreadAbortException) {
            if (AsyncLayoutProgress != null) {
              args.progress = LayoutProgress.Aborted;
              AsyncLayoutProgress(this, args);
            }
            // rethrown automatically
          }
          //catch (Exception e) {
          //    // must not leak through any exception, otherwise appl. terminates
          //    if (AsyncLayoutProgress != null) {
          //        args.progress = LayoutProgress.Aborted;
          //        args.diagnostics = e.ToString();
          //        AsyncLayoutProgress(this, args);
          //    }
          //}
          layoutThread = null;
        }
      });
      // Before we start the thread, ensure the control is created.
      // Otherwise Invoke inside of the thread might fail.
      CreateControl();
      layoutThread.Start();
      // Wait until the layout thread has started.
      // If we don't do this, there is a chance that the thread is aborted
      // before we are in the try-context which catches the AbortException, 
      // and we wouldn't get the abortion notification.
      layoutWaitHandle.WaitOne();
    }

    delegate void Invoker();

    #endregion

    /// <summary>
    /// returns false on filtered entities and only on them
    /// </summary>
    /// <param name="dObject"></param>
    /// <returns></returns>
    static bool EdgeFilter(DObject dObject) {
      return !(dObject is DEdge);
    }

        void UnconditionalHit(MouseEventArgs args, EntityFilterDelegate filter) {
            System.Drawing.Point point = args != null
                                             ? new System.Drawing.Point(args.X, args.Y)
                                             : DrawingPanel.PointToClient(MousePosition);

            object old = selectedDObject;
            if (bBNode == null && DGraph != null)
                bBNode = DGraph.BBNode;
            if (bBNode != null) {
                var subgraphs = new List<Geometry>();
                Geometry geometry = bBNode.Hit(ScreenToSource(point), GetHitSlack(), filter, subgraphs) ??
                                    PickSubgraph(subgraphs, ScreenToSource(point));
                selectedDObject = geometry?.dObject;
                if (old == selectedDObject) return;
                SetSelectedObject(selectedDObject);
                if (ObjectUnderMouseCursorChanged != null) {
                    var changedArgs = new ObjectUnderMouseCursorChangedEventArgs((IViewerObject)old,
                        selectedDObject);
                    ObjectUnderMouseCursorChanged(this, changedArgs);
                }
            }
            else {
                old = selectedDObject;
                SetSelectedObject(null);
                if (ObjectUnderMouseCursorChanged != null) {
                    var changedArgs = new ObjectUnderMouseCursorChangedEventArgs((IViewerObject)old,
                        null);
                    ObjectUnderMouseCursorChanged(this, changedArgs);
                }
            }
        }

    Geometry PickSubgraph(List<Geometry> subgraphs, Point screenToSource) {
      if (subgraphs.Count == 0) return null;
      double area = subgraphs[0].dObject.DrawingObject.BoundingBox.Area;
      int ret = 0;
      for (int i = 1; i < subgraphs.Count; i++) {
        double a = subgraphs[i].dObject.DrawingObject.BoundingBox.Area;
        if (a < area) {
          area = a;
          ret = i;
        }
      }
      return subgraphs[ret];
    }

    /// <summary>
    /// It should be physically on the screen by one tenth of an inch
    /// </summary>
    /// <returns></returns> 
    double GetHitSlack() {
      double inchSlack = MouseHitDistance;
      double slackInPoints = Dpi * inchSlack;
      return slackInPoints / CurrentScale;
    }

    void DrawingPanelMouseClick(object sender, MouseEventArgs e) {
      OnMouseClick(e);
    }

    internal static bool ModifierKeyWasPressed() {
      return ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift;
    }

    /// <summary>
    /// creates the corresponding rectangle on screen
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public Rectangle MapSourceRectangleToScreenRectangle(Core.Geometry.Rectangle rect) {
      return CreateScreenRectFromTwoCornersInTheSource(rect.LeftTop, rect.RightBottom);
    }


    internal Rectangle CreateScreenRectFromTwoCornersInTheSource(Point leftTop, Point rightBottom) {
      var pts = new[] { Pf(leftTop), Pf(rightBottom) };

      CurrentTransform().TransformPoints(pts);

      return Rectangle.FromLTRB(
          (int)Math.Floor(pts[0].X - 1),
          (int)Math.Floor(pts[0].Y) - 1,
          (int)Math.Ceiling(pts[1].X) + 1,
          (int)Math.Ceiling(pts[1].Y) + 1);
    }

    internal static PointF Pf(Point p2) {
      return new PointF((float)p2.X, (float)p2.Y);
    }

    /// <summary>
    /// Maps a point from the screen to the graph surface
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Point ScreenToSource(System.Drawing.Point point) {
      Matrix m = CurrentTransform();
      if (!m.IsInvertible)
        return new Point(0, 0);
      m.Invert();

      var pf = new[] { new PointF(point.X, point.Y) };
      m.TransformPoints(pf);
      return new Point(pf[0].X, pf[0].Y);
    }

    internal Point ScreenToSource(int x, int y) {
      return ScreenToSource(new System.Drawing.Point(x, y));
    }


    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing) {
      if (disposing) {
        if (components != null) {
          components.Dispose();
        }
        if (ToolTip != null) {
          ToolTip.RemoveAll();
          ToolTip.Dispose();
          ToolTip = null;
        }
        if (layoutWaitHandle != null)
          layoutWaitHandle.Close();
        if (panGrabCursor != null)
          panGrabCursor.Dispose();
        if (panOpenCursor != null)
          panOpenCursor.Dispose();
      }
      base.Dispose(disposing);
    }

    ViewInfo CurrentViewInfo() {
      var viewInfo = new ViewInfo {
        Transformation = Transform.Clone(),
        leftMouseButtonWasPressed =
                                          MouseButtons == MouseButtons.Left,
      };

      return viewInfo;
    }

    void HandleViewInfoList() {
      ViewInfo currentViewInfo = CurrentViewInfo();
      if (listOfViewInfos.AddNewViewInfo(currentViewInfo)) {
        BackwardEnabled = listOfViewInfos.BackwardAvailable;        
      }
    }


    internal void ProcessOnPaint(Graphics g, PrintPageEventArgs printPageEvenArgs) {
      if (PanelHeight < minimalSizeToDraw || PanelWidth < minimalSizeToDraw || DGraph == null)
        return;
      if (wasMinimized) {
        wasMinimized = false;
        panel.Invalidate();
      }

      if (OriginalGraph != null) {
        CalcRects(printPageEvenArgs);
        HandleViewInfoList();
        if (printPageEvenArgs == null) {
          g.FillRectangle(outsideAreaBrush, ClientRectangle);
          g.FillRectangle(new SolidBrush(Draw.MsaglColorToDrawingColor(OriginalGraph.Attr.BackgroundColor)),
                          destRect);
        }

        using (Matrix m = CurrentTransform()) {
          if (!m.IsInvertible) // just to make sure that the transform is legal
            return;
          g.Transform = m;

          g.Clip = new Region(SrcRect);
          if (DGraph == null)
            return;

          double scale = CurrentScale;
          foreach (IViewerObject viewerObject in Entities)
            ((DObject)viewerObject).UpdateRenderedBox();

          DGraph.DrawGraph(g);

          //some info is known only after the first drawing

          if (bBNode == null && BuildHitTree
#if TEST_MSAGL
                        && (
                     dGraph.DrawingGraph.DebugICurves == null
                 )
#endif
                        ) {
            DGraph.BuildBBHierarchy();
            bBNode = DGraph.BbNode;
          }
        }
      }
      else
        g.FillRectangle(Brushes.Gray, ClientRectangle);


      //g.Transform.Reset();
    }

    internal Matrix CurrentTransform() {
      return new Matrix((float)Transform[0, 0], (float)Transform[0, 1],
                        (float)Transform[1, 0], (float)Transform[1, 1],
                        (float)Transform[0, 2], (float)Transform[1, 2]);
    }


    internal static Rectangle RectFromPoints(System.Drawing.Point p1, System.Drawing.Point p2) {
      return new Rectangle(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                           Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cx"></param>
    /// <param name="cy"></param>
    /// <param name="val"></param>
    [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions"),
     SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters",
         MessageId = "System.Windows.Forms.MessageBox.Show(System.String)")]
    internal void Zoom(double cx, double cy, double val) {

      ZoomF = val;

      panel.Invalidate();
    }

    void InitiateDrawing() {

      transformation = null;

      CalcRects(null);

      bBNode = null; //to initiate new calculation

      panel.Invalidate();
    }

    [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
    void LayoutAndCreateDGraph() {
      switch (CurrentLayoutMethod) {
        case LayoutMethod.SugiyamaScheme:
          if (!(OriginalGraph.LayoutAlgorithmSettings is SugiyamaLayoutSettings))
            OriginalGraph.LayoutAlgorithmSettings = sugiyamaSettings;
          break;
        case LayoutMethod.MDS:
          if (!(OriginalGraph.LayoutAlgorithmSettings is MdsLayoutSettings))
            OriginalGraph.LayoutAlgorithmSettings = mdsLayoutSettings;
          break;
        case LayoutMethod.Ranking:
          if (!(OriginalGraph.LayoutAlgorithmSettings is RankingLayoutSettings))
            OriginalGraph.LayoutAlgorithmSettings = rankingSettings;
          break;
        case LayoutMethod.IcrementalLayout:
          if (!(OriginalGraph.LayoutAlgorithmSettings is FastIncrementalLayoutSettings))
            OriginalGraph.LayoutAlgorithmSettings = fastIncrementalLayoutSettings;
          break;
      }
      var localSugiyamaSettings = OriginalGraph.LayoutAlgorithmSettings as SugiyamaLayoutSettings;
      if (localSugiyamaSettings != null) {
        // Insert hard coded constraints for tests

#if TEST_MSAGL
        TestSomeGraphs();
#endif
      }
      OriginalGraph.CreateGeometryGraph();
      GeometryGraph geometryGraph = OriginalGraph.GeometryGraph;
      DGraph = DGraph.CreateDGraphAndGeometryInfo(OriginalGraph, geometryGraph, this);
      try {
        LayoutHelpers.CalculateLayout(geometryGraph, originalGraph.LayoutAlgorithmSettings, null);
      }
      catch (OperationCanceledException) {
        originalGraph = null;
        DGraph = null;
      }
      TransferGeometryFromMsaglGraphToGraph(geometryGraph);
      if (GraphChanged != null) {
        GraphChanged(this, null);
      }
    }

#if TEST_MSAGL
    void TestSomeGraphs() {
      if (fileName.EndsWith("lovett.dot")) {
        OriginalGraph.LayerConstraints.AddUpDownVerticalConstraint(OriginalGraph.FindNode("Logica"),
                                                                   OriginalGraph.FindNode("IBM"));
        OriginalGraph.LayerConstraints.AddUpDownVerticalConstraint(OriginalGraph.FindNode("IBM"),
                                                                   OriginalGraph.FindNode("Taligent"));
        OriginalGraph.LayerConstraints.AddUpDownVerticalConstraint(OriginalGraph.FindNode("Taligent"),
                                                                   OriginalGraph.FindNode("Walkabout"));
        OriginalGraph.LayerConstraints.AddUpDownVerticalConstraint(OriginalGraph.FindNode("Walkabout"),
                                                                   OriginalGraph.FindNode("Microsoft"));
        OriginalGraph.LayerConstraints.AddSameLayerNeighbors(OriginalGraph.FindNode("MSXML"),
                                                             OriginalGraph.FindNode("sysxml"),
                                                             OriginalGraph.FindNode("xsharp"),
                                                             OriginalGraph.FindNode("xmled"),
                                                             OriginalGraph.FindNode("Progression"));
      }
      else if (fileName.EndsWith("dependenciesForTaskCrashTest.dot")) {
        OriginalGraph.LayerConstraints.PinNodesToSameLayer(
            OriginalGraph.FindNode("C1"),
            OriginalGraph.FindNode("C2"),
            OriginalGraph.FindNode("C3"),
            OriginalGraph.FindNode("C4"),
            OriginalGraph.FindNode("C5"),
            OriginalGraph.FindNode("C6"),
            OriginalGraph.FindNode("C7"),
            OriginalGraph.FindNode("C8"),
            OriginalGraph.FindNode("C9"),
            OriginalGraph.FindNode("C10"));
        OriginalGraph.LayerConstraints.PinNodesToSameLayer(
            OriginalGraph.FindNode("R1"),
            OriginalGraph.FindNode("R2"),
            OriginalGraph.FindNode("R3"),
            OriginalGraph.FindNode("R4"),
            OriginalGraph.FindNode("R5"),
            OriginalGraph.FindNode("R6"),
            OriginalGraph.FindNode("R7"),
            OriginalGraph.FindNode("R8"),
            OriginalGraph.FindNode("R9"),
            OriginalGraph.FindNode("R10"));
      }
      else if (fileName.EndsWith("andrei.dot")) {
        OriginalGraph.LayerConstraints.AddUpDownVerticalConstraint(OriginalGraph.FindNode("open"),
                                                                   OriginalGraph.FindNode("bezier"));
        OriginalGraph.LayerConstraints.AddUpDownVerticalConstraint(OriginalGraph.FindNode("closed"),
                                                                   OriginalGraph.FindNode("ellipse"));
      }
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    public void ClearBoundingBoxHierarchy() {
      bBNode = null;
    }

    /// <summary>
    /// Brings in to the view the object of the group
    /// </summary>
    /// <param name="graphElements"></param>
    public void ShowGroup(object[] graphElements) {
      Core.Geometry.Rectangle bb = BBoxOfObjs(graphElements);

      ShowBBox(bb);
    }

    /// <summary>
    /// Changes the view in a way that the group is at the center
    /// </summary>
    /// <param name="graphElements"></param>
    public void CenterToGroup(params object[] graphElements) {
      Core.Geometry.Rectangle bb = BBoxOfObjs(graphElements);

      if (!bb.IsEmpty) {
        CenterToPoint(0.5f * (bb.LeftTop + bb.RightBottom));
      }

    }


    static Core.Geometry.Rectangle BBoxOfObjs(IEnumerable<object> objs) {
      var bb = new Core.Geometry.Rectangle(0, 0, 0, 0);
      bool boxIsEmpty = true;

      foreach (object o in objs) {
        var node = o as DrawingNode;
        Core.Geometry.Rectangle objectBb;
        if (node != null)
          objectBb = node.BoundingBox;
        else {
          var edge = o as DrawingEdge;
          if (edge != null)
            objectBb = edge.BoundingBox;
          else
            continue;
        }

        if (boxIsEmpty) {
          bb = objectBb;
          boxIsEmpty = false;
        }
        else
          bb.Add(objectBb);
      }


      return bb;
    }

    /// <summary>
    /// Make the bounding rect fully visible
    /// </summary>
    /// <param name="bb"></param>
    public void ShowBBox(Core.Geometry.Rectangle bb) {
      if (bb.IsEmpty == false) {
        double sc = Math.Min(OriginalGraph.Width / bb.Width,
                             OriginalGraph.Height / bb.Height);

        Point center = 0.5 * (bb.LeftTop + bb.RightBottom);
        SetTransformOnScaleAndCenter(sc, center);
        panel.Invalidate();
      }
    }

    /// <summary>
    /// Pans the view by vector (x,y)
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y"),
     SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
    public void Pan(double x, double y) {
      Transform[0, 2] += x;
      Transform[1, 2] += y;
      panel.Invalidate();
    }

    /// <summary>
    /// Pans the view by vector point
    /// </summary>
    /// <param name="point"></param>
    public void Pan(Point point) {
      Pan(point.X, point.Y);
    }



    /// <summary>
    /// Centers the view to the point p
    /// </summary>
    /// <param name="point"></param>
    public void CenterToPoint(Point point) {
      SetTransformOnScaleAndCenter(CurrentScale, point);
      panel.Invalidate();
    }

    /// <summary>
    /// Finds the object under point (x,y) where x,y are given in the window coordinates
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y"),
     SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
    public object GetObjectAt(int x, int y) {
      BBNode bn = BbNode;
      if (bn == null)
        return null;
      List<Geometry> subgraphs = new List<Geometry>();
      Geometry g = bn.Hit(ScreenToSource(new System.Drawing.Point(x, y)), GetHitSlack(), EntityFilterDelegate, subgraphs) ??
                   PickSubgraph(subgraphs, ScreenToSource(new System.Drawing.Point(x, y)));

      return g == null ? null : g.dObject;
    }

    /// <summary>
    /// Finds the object under point p where p is given in the window coordinates
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public object GetObjectAt(System.Drawing.Point point) {
      return GetObjectAt(point.X, point.Y);
    }

    internal bool ScaleIsAcceptable(double scale) {
      var d = OriginalGraph != null ? OriginalGraph.BoundingBox.Diagonal : 0;
      return !(d * scale < 5) && !(d * scale > HugeDiagonal);
    }
    /// <summary>
    /// Zooms in
    /// </summary>
    public void ZoomInPressed() {
      double zoomFractionLocal = ZoomF * ZoomFactor();
      if (!ScaleIsAcceptable(CurrentScale * zoomFractionLocal))
        return;
      ZoomF *= ZoomFactor();
    }

    /// <summary>
    /// Zooms out
    /// </summary>
    public void ZoomOutPressed() {
      ZoomF /= ZoomFactor();
    }

    double ZoomFactor() {
      return 1.0f + zoomFraction;
    }

    void ToolBarButtonClick(object sender, ToolStripItemClickedEventArgs e) {
      if (e.ClickedItem == zoomin)
        ZoomInPressed();
      else if (e.ClickedItem == zoomout)
        ZoomOutPressed();
      else if (e.ClickedItem == backwardButton)
        BackwardButtonPressed();
      else if (e.ClickedItem == forwardButton)
        ForwardButtonPressed();
      else if (e.ClickedItem == saveButton)
        SaveButtonPressed();
      else if (e.ClickedItem == print)
        PrintButtonPressed();
      else if (e.ClickedItem == openButton)
        OpenButtonPressed();
      else if (e.ClickedItem == undoButton)
        UndoButtonPressed();
      else if (e.ClickedItem == redoButton)
        RedoButtonPressed();
      else if (e.ClickedItem == layoutSettingsButton)
        LayoutSettingsIsClicked();
      else if (e.ClickedItem == homeZoomButton) {
        transformation = null;
        panel.Invalidate();
      }

    }

    void ToolBarButtonCheckChanged(object sender, EventArgs e) {
      if (sender == windowZoomButton)
        WindowZoomButtonIsPressed();
      else if (sender == panButton)
        PanButtonIsPressed();
      else if (sender == edgeInsertButton) {
        InsertingEdge = edgeInsertButton.Checked;
        if (InsertingEdge)
          layoutEditor.PrepareForEdgeDragging();
        else
          layoutEditor.ForgetEdgeDragging();
      }
    }

    void LayoutSettingsIsClicked() {
      var layoutSettingsForm = new LayoutSettingsForm();
      var wrapper = new LayoutSettingsWrapper { LayoutSettings = Graph != null ? Graph.LayoutAlgorithmSettings : null };
      wrapper.LayoutTypeHasChanged += OnLayoutTypeChange;
      wrapper.LayoutMethod = CurrentLayoutMethod;
      switch (CurrentLayoutMethod) {
        case LayoutMethod.SugiyamaScheme:
          wrapper.LayoutSettings = sugiyamaSettings;
          break;
        case LayoutMethod.MDS:
          wrapper.LayoutSettings = mdsLayoutSettings;
          break;
        case LayoutMethod.Ranking:
          wrapper.LayoutSettings = rankingSettings;
          break;
        case LayoutMethod.IcrementalLayout:
          wrapper.LayoutSettings = fastIncrementalLayoutSettings;
          break;
      }
      layoutSettingsForm.PropertyGrid.SelectedObject = wrapper;
      LayoutAlgorithmSettings backup = Graph != null ? Graph.LayoutAlgorithmSettings.Clone() : null;
      if (layoutSettingsForm.ShowDialog() == DialogResult.OK) {
        if (Graph != null) {
          LayoutAlgorithmSettings settings = Graph.LayoutAlgorithmSettings;
          if (settings.EdgeRoutingSettings.EdgeRoutingMode == EdgeRoutingMode.SplineBundling) {
            if (settings.EdgeRoutingSettings.BundlingSettings == null)
              settings.EdgeRoutingSettings.BundlingSettings = new BundlingSettings();
          }

          if (!(settings is SugiyamaLayoutSettings)) //fix wrong settings coming from the Sugiyama
            if (settings.EdgeRoutingSettings.EdgeRoutingMode == EdgeRoutingMode.SugiyamaSplines)
              settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Spline;
                    if (layoutSettingsForm.Wrapper.RerouteOnly == false)
                        Graph = Graph; //recalculate the layout
                    else {
                        LayoutHelpers.RouteAndLabelEdges(Graph.GeometryGraph, settings, Graph.GeometryGraph.Edges, 0, null);
                     
                        foreach (IViewerObject e in Entities.Where(e => e is IViewerEdge))
                            Invalidate(e);
                    }
        }
      }
      else if (Graph != null)
        Graph.LayoutAlgorithmSettings = backup;
    }


    void OnLayoutTypeChange(object o, EventArgs args) {
      var wrapper = o as LayoutSettingsWrapper;
      switch (wrapper.LayoutMethod) {
        case LayoutMethod.SugiyamaScheme:
          wrapper.LayoutSettings = sugiyamaSettings;
          break;
        case LayoutMethod.MDS:
          wrapper.LayoutSettings = mdsLayoutSettings;
          break;
        case LayoutMethod.Ranking:
          wrapper.LayoutSettings = rankingSettings;
          break;
        case LayoutMethod.IcrementalLayout:
          wrapper.LayoutSettings = fastIncrementalLayoutSettings;
          break;
        case LayoutMethod.UseSettingsOfTheGraph:
          wrapper.LayoutSettings = Graph != null ? Graph.LayoutAlgorithmSettings : null;
          break;
        default:
          Debug.Assert(false); //cannot be here
          break;
      }
      CurrentLayoutMethod = wrapper.LayoutMethod;
      if (Graph != null)
        Graph.LayoutAlgorithmSettings = wrapper.LayoutSettings;
    }

    void PanButtonIsPressed() {
      if (panButton.Checked) {
        panButton.ToolTipText = panButtonToolTipText;
        windowZoomButton.Checked = false;
        windowZoomButton.ToolTipText = windowZoomButtonDisabledToolTipText;
      }
      else
        panButton.ToolTipText = panButtonToolTipText;
    }

    void WindowZoomButtonIsPressed() {
      if (windowZoomButton.Checked) {
        windowZoomButton.ToolTipText = WindowZoomButtonToolTipText;
        panButton.Checked = false;
        panButton.ToolTipText = PanButtonDisabledToolTipText;
      }
      else
        windowZoomButton.ToolTipText = WindowZoomButtonToolTipText;
    }

    void RedoButtonPressed() {
      if (LayoutEditor != null && LayoutEditor.CanRedo)
        LayoutEditor.Redo();
    }

    void UndoButtonPressed() {
      if (LayoutEditor != null && LayoutEditor.CanUndo)
        LayoutEditor.Undo();
    }

    /// <summary>
    /// an event raised on graph loading
    /// </summary>
    public event EventHandler GraphLoadingEnded;

    public event EventHandler<HandledEventArgs> CustomOpenButtonPressed;

    [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
    void OpenButtonPressed() {

      HandledEventArgs args = new HandledEventArgs();
      CustomOpenButtonPressed?.Invoke(this, args);
      if (args.Handled) return;

      var openFileDialog = new OpenFileDialog { RestoreDirectory = true, Filter = "MSAGL Files(*.msagl)|*.msagl" };

      try {
        if (openFileDialog.ShowDialog() == DialogResult.OK) {
          FileName = openFileDialog.FileName;
          NeedToCalculateLayout = false;
          Graph = Graph.Read(openFileDialog.FileName);
          if (GraphLoadingEnded != null)
            GraphLoadingEnded(this, null);
        }
      }
      catch (Exception e) {
        MessageBox.Show(e.Message);
      }
      finally {
        NeedToCalculateLayout = true;
      }
    }

    /// <summary>
    /// Raises a dialog of saving the drawing image to a file
    /// </summary>
    public void SaveButtonPressed() {
      if (Graph == null) {
        return;
      }

      var contextMenu = new ContextMenuStrip();
      contextMenu.Items.AddRange(CreateSaveMenuItems());
      contextMenu.Show(this, new System.Drawing.Point(toolbar.Left + 100, toolbar.Bottom),
                       ToolStripDropDownDirection.BelowRight);
    }

    [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters",
        MessageId = "System.Windows.Forms.MenuItem.#ctor(System.String)")]
    ToolStripMenuItem[] CreateSaveMenuItems() {
      var menuItems = new List<ToolStripMenuItem>();
      ToolStripMenuItem menuItem;
      if (SaveAsMsaglEnabled) {
        menuItems.Add(menuItem = new ToolStripMenuItem("Save graph"));
        menuItem.Click += SaveGraphClick;
      }
      if (SaveAsImageEnabled) {
        menuItems.Add(menuItem = new ToolStripMenuItem("Save in bitmap format"));
        menuItem.Click += SaveImageClick;
      }
      if (SaveInVectorFormatEnabled) {
        menuItems.Add(menuItem = new ToolStripMenuItem("Save in vector format"));
        menuItem.Click += SaveInVectorGraphicsFormatClick;
      }
      return menuItems.ToArray();
    }

    void SaveInVectorGraphicsFormatClick(object sender, EventArgs e) {
      var saveForm = new SaveInVectorFormatForm(this);
      saveForm.ShowDialog();
    }

    void SaveImageClick(object sender, EventArgs e) {
      var saveViewForm = new SaveViewAsImageForm(this);
      saveViewForm.ShowDialog();
    }

    [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
    void SaveGraphClick(object sender, EventArgs e) {
      var saveFileDialog = new SaveFileDialog { Filter = "MSAGL Files(*.msagl)|*.msagl" };
      try {
        if (saveFileDialog.ShowDialog() == DialogResult.OK) {
          FileName = saveFileDialog.FileName;
          if (GraphSavingStarted != null)
            GraphSavingStarted(this, null);
          Graph.Write(saveFileDialog.FileName);
          if (GraphSavingEnded != null)
            GraphSavingEnded(this, null);
        }
      }
      catch (Exception ex) {
        MessageBox.Show(ex.Message);
        throw;
      }
    }

    /// <summary>
    /// Navigates forward in the view history
    /// </summary>
    public void ForwardButtonPressed() {
      if (listOfViewInfos.ForwardAvailable)
        if (listOfViewInfos.CurrentView.leftMouseButtonWasPressed) {
          while (listOfViewInfos.ForwardAvailable) {
            listOfViewInfos.Forward();
            SetViewFromViewInfo(listOfViewInfos.CurrentView);

            if (listOfViewInfos.CurrentView.leftMouseButtonWasPressed == false)
              break;
          }
        }
        else {
          listOfViewInfos.Forward();
          SetViewFromViewInfo(listOfViewInfos.CurrentView);
          /*
                                  if(listOfViewInfos.CurrentView.leftMouseButtonWasPressed) {
                                        while( listOfViewInfos.ForwardAvailable ) {
                                              listOfViewInfos.Forward();
                                              this.SetViewFromViewInfo(listOfViewInfos.CurrentView);

                                              if(listOfViewInfos.CurrentView.leftMouseButtonWasPressed==false)
                                                    break;

                                        }
                                  }
                                  */
        }
      ForwardEnabled = listOfViewInfos.ForwardAvailable;
      BackwardEnabled = listOfViewInfos.BackwardAvailable;
    }

    /// <summary>
    /// Navigates backward in the view history
    /// </summary>
    public void BackwardButtonPressed() {
      if (listOfViewInfos.BackwardAvailable)
        if (listOfViewInfos.CurrentView.leftMouseButtonWasPressed) {
          while (listOfViewInfos.BackwardAvailable) {
            listOfViewInfos.Backward();
            SetViewFromViewInfo(listOfViewInfos.CurrentView);

            if (listOfViewInfos.CurrentView.leftMouseButtonWasPressed == false)
              break;
          }
        }
        else {
          listOfViewInfos.Backward();
          SetViewFromViewInfo(listOfViewInfos.CurrentView);
        }
      ForwardEnabled = listOfViewInfos.ForwardAvailable;
      BackwardEnabled = listOfViewInfos.BackwardAvailable;

    }

    /// <summary>
    /// Prints the graph.
    /// </summary>
    public void PrintButtonPressed() {
      var p = new GraphPrinting(this);
      var pd = new PrintDialog { Document = p };
      if (pd.ShowDialog() == DialogResult.OK)
        p.Print();
    }

    void PanelClick(object sender, EventArgs e) {
      OnClick(e);
    }

    /// <summary>
    /// Reacts on some pressed keys
    /// </summary>
    /// <param name="e"></param>
    public void OnKey(KeyEventArgs e) {
      if (e == null)
        return;
      if (e.KeyData == (Keys)262181) {
        if (backwardButton.Enabled) {
          BackwardButtonPressed();
        }
      }
      else if (e.KeyData == (Keys)262183) //key==Keys.BrowserForward)
      {
        if (forwardButton.Enabled)
          ForwardButtonPressed();
      }
    }


    void TransferGeometryFromMsaglGraphToGraph(GeometryGraph gleeGraph) {
      foreach (Edge gleeEdge in gleeGraph.Edges) {
        var drawingEdge = gleeEdge.UserData as DrawingEdge;
        drawingEdge.GeometryEdge = gleeEdge;
      }

      foreach (Node gleeNode in gleeGraph.Nodes) {
        DrawingNode drawingNode = (Drawing.Node)gleeNode.UserData;
        drawingNode.GeometryNode = gleeNode;
      }
    }

    /// <summary>
    /// calcualates the layout and returns the object ready to be drawn
    /// </summary>
    /// <param name="graph"></param>
    /// <returns></returns>
    public object CalculateLayout(Graph graph) {
      OriginalGraph = graph;
      LayoutAndCreateDGraph();
      return DGraph;
    }

    /// <summary>
    /// sets a tool tip
    /// </summary>
    /// <param name="toolTip"></param>
    /// <param name="tip"></param>
    public void SetToolTip(ToolTip toolTip, string tip) {
      if (toolTip != null)
        toolTip.SetToolTip(DrawingPanel, tip);
    }

    /// <summary>
    /// Just uses the passed object to draw the graph. The method expects DGraph as the argument
    /// </summary>
    /// <param name="entityContainingLayout"></param>
    public void SetCalculatedLayout(object entityContainingLayout) {
      ClearLayoutEditor();
      DGraph = entityContainingLayout as DGraph;
      if (DGraph != null) {
        OriginalGraph = DGraph.DrawingGraph;
        if (GraphChanged != null)
          GraphChanged(this, null);
        InitiateDrawing();
      }
    }

    /// <summary>
    /// maps screen coordinates to viewer coordinates
    /// </summary>
    /// <param name="screenX"></param>
    /// <param name="screenY"></param>
    /// <param name="viewerX"></param>
    /// <param name="viewerY"></param>
    public void ScreenToSource(float screenX, float screenY, out float viewerX, out float viewerY) {
      Point p = ScreenToSource(new System.Drawing.Point((int)screenX, (int)screenY));
      viewerX = (float)p.X;
      viewerY = (float)p.Y;
    }

    /// <summary>
    /// pans the drawing on deltaX, deltaY in the drawing coords
    /// </summary>
    /// <param name="deltaX"></param>
    /// <param name="deltaY"></param>
    public void Pan(float deltaX, float deltaY) {
      Pan(new Point(deltaX, deltaY));
    }


    internal void RaiseMouseMoveEvent(MsaglMouseEventArgs iArgs) {
      if (iEditViewerMouseMove != null)
        iEditViewerMouseMove(this, iArgs);
    }


    internal void RaiseMouseDownEvent(MsaglMouseEventArgs iArgs) {
      if (iEditViewerMouseDown != null)
        iEditViewerMouseDown(this, iArgs);
    }

    internal void RaiseMouseUpEvent(MsaglMouseEventArgs iArgs) {
      if (iEditViewerMouseUp != null)
        iEditViewerMouseUp(this, iArgs);
    }


    /// <summary>
    /// This event is raised before the file saving
    /// </summary>
    public event EventHandler GraphSavingStarted;

    /// <summary>
    /// This even is raised after graph saving
    /// </summary>
    public event EventHandler GraphSavingEnded;


    /// <summary>
    /// Undoes the last edit action
    /// </summary>
    public void Undo() {
      if (LayoutEditor.CanUndo)
        LayoutEditor.Undo();
    }

    /// <summary>
    /// redoes the last undo
    /// </summary>
    public void Redo() {
      if (LayoutEditor.CanRedo)
        LayoutEditor.Redo();
    }


    /// <summary>
    /// returns true if an undo is available
    /// </summary>
    /// <returns></returns>
    public bool CanUndo() {
      return LayoutEditor.CanUndo;
    }

    /// <summary>
    /// returns true is a redo is available
    /// </summary>
    /// <returns></returns>
    public bool CanRedo() {
      return LayoutEditor.CanRedo;
    }


    static ToolStripMenuItem CreateMenuItem(string title, VoidDelegate voidVoidDelegate) {
      var menuItem = new ToolStripMenuItem { Text = title };
      menuItem.Click += ((sender, e) => voidVoidDelegate());
      return menuItem;
    }

    static IEnumerable<DEdge> Edges(DNode dNode) {
      foreach (DEdge de in dNode.OutEdges)
        yield return de;
      foreach (DEdge de in dNode.InEdges)
        yield return de;
      foreach (DEdge de in dNode.SelfEdges)
        yield return de;
    }

    /// <summary>
    /// makes the node unreachable
    /// </summary>
    /// <param name="node"></param>
    void RemoveNodeFromAllGraphs(IViewerNode node) {
      var drawingNode = node.DrawingObject as DrawingNode;

      DGraph.RemoveDNode(drawingNode.Id);
      Graph.NodeMap.Remove(drawingNode.Id);

      if (drawingNode.GeometryNode != null) {
        Graph.GeometryGraph.Nodes.Remove(drawingNode.GeometryNode);
      }

      foreach (DEdge de in Edges(node as DNode)) {
        DGraph.Edges.Remove(de);
        Graph.RemoveEdge(de.DrawingEdge);
        Graph.GeometryGraph.Edges.Remove(de.DrawingEdge.GeometryEdge);
      }

      foreach (DEdge de in node.OutEdges) {
        de.Target.inEdges.Remove(de);
        de.Target.DrawingNode.RemoveInEdge(de.DrawingEdge);
        de.Target.DrawingNode.GeometryNode.RemoveInEdge(de.DrawingEdge.GeometryEdge);
      }

      foreach (DEdge de in node.InEdges) {
        de.Source.outEdges.Remove(de);
        de.Source.DrawingNode.RemoveOutEdge(de.DrawingEdge);
        de.Source.DrawingNode.GeometryNode.RemoveOutEdge(de.DrawingEdge.GeometryEdge);
      }
    }

    /// <summary>
    /// Sets the size of the node to something appropriate to the label it has to display.
    /// </summary>
    /// <param name="node">The node to be resized</param>
    public void ResizeNodeToLabel(DrawingNode node) {
      double width = 0;
      double height = 0;
      string label = node.Label.Text;
      if (String.IsNullOrEmpty(label) == false) {
        var f = new Font(node.Label.FontName, (int)node.Label.FontSize, (System.Drawing.FontStyle)(int)node.Label.FontStyle);
        StringMeasure.MeasureWithFont(label, f, out width, out height);
      }
      node.Label.Size = new Size((float)width, (float)height);
      width += 2 * node.Attr.LabelMargin;
      height += 2 * node.Attr.LabelMargin;
      if (width < Graph.Attr.MinNodeWidth)
        width = Graph.Attr.MinNodeWidth;
      if (height < Graph.Attr.MinNodeHeight)
        height = Graph.Attr.MinNodeHeight;

      Point originalCenter = node.GeometryNode.Center;
      node.GeometryNode.BoundaryCurve = NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height).Clone();
      node.GeometryNode.BoundaryCurve.Translate(originalCenter);
    
      foreach (IViewerObject en in Entities)
        Invalidate(en);

      Invalidate();
    }


    internal void RaisePaintEvent(PaintEventArgs e) {
      base.OnPaint(e);
    }

    /// <summary>
    /// an event raised when an edge is added 
    /// </summary>
    public event EventHandler EdgeAdded;

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler EdgeRemoved;

    void RouteEdge(Edge geometryEdge) {
      var router = new RouterBetweenTwoNodes(Graph.GeometryGraph, Graph.Attr.NodeSeparation * tightOffsetForRouting,
                                             Graph.Attr.NodeSeparation * looseOffsetForRouting,
                                             Graph.Attr.NodeSeparation * offsetForRelaxingInRouting);
      router.RouteEdge(geometryEdge, true);
    }


    // ReSharper disable InconsistentNaming
    event MouseEventHandler mouseMove;
    // ReSharper restore InconsistentNaming
    /// <summary>
    /// 
    /// </summary>
    public new event MouseEventHandler MouseMove {
      add { mouseMove += value; }
      remove { mouseMove -= value; }
    }


    internal void RaiseRegularMouseMove(MouseEventArgs args) {
      if (mouseMove != null)
        mouseMove(this, args);
    }

    double GetFitScale() {
      return OriginalGraph == null ? 1 : Math.Min(panel.Width / originalGraph.Width, panel.Height / originalGraph.Height);
    }
  }


#if DEBUGLOG
  internal class Lo
  {
	static StreamWriter sw = null;

	static internal void W(string s)
	{
	  if (sw == null)
	  {
		sw = new StreamWriter("c:\\gdiviewerlog");
	  }
	  sw.WriteLine(s);
	  sw.Flush();

	}
	static internal void W(object o)
	{
	  if (sw == null)
	  {
		sw = new StreamWriter("c:\\gdiviewerlog");
	  }
	  sw.WriteLine(o.ToString());
	  sw.Flush();

	}

  }
#endif
}
