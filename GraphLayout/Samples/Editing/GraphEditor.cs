using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Color = Microsoft.Msagl.Drawing.Color;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Label = Microsoft.Msagl.Drawing.Label;
using Point = Microsoft.Msagl.Core.Geometry.Point;

namespace Editing {
    /// <summary>
    /// This class implements a graph editor control. It is a work in progress, especially with regards to allowing users to configure the editor's behaviour.
    /// It is designed to be easy to plug into a program. This control is a GDI control, based on GViewer.
    /// </summary>
    internal class GraphEditor : Panel {
        readonly GViewer gViewer = new GViewer();
        readonly ToolTip toolTip = new ToolTip();
        readonly Image treeImage;

        /// <summary>
        /// 
        /// </summary>
        internal CloseEditor CloseEditorDelegate;

        /// <summary>
        /// 
        /// </summary>
        internal ShowEditor ShowEditorDelegate;

        /// <summary>
        /// 
        /// </summary>
        internal ValidateEditor ValidateEditorDelegate;

        int idcounter;

        /// <summary>
        /// The object currently being edited
        /// </summary>
        protected DrawingObject m_EditingObject;

        /// <summary>
        /// The text box used as the default editor (edits the label).
        /// </summary>
        protected TextBox m_LabelBox = new TextBox();

        /// <summary>
        /// The point where the user called up the context menu.
        /// </summary>
        protected Point m_MouseRightButtonDownPoint;

        /// <summary>
        /// An ArrayList containing all the node type entries (custom node types for insetion).
        /// </summary>
        protected ArrayList m_NodeTypes = new ArrayList();

        /// <summary>
        /// 
        /// </summary>
        //   internal bool ZoomEnabled { get { return (gViewer.DrawingPanel as DrawingPanel).ZoomEnabled; } set { (gViewer.DrawingPanel as DrawingPanel).ZoomEnabled = value; } }
        /// <summary>
        /// Default constructor
        /// </summary>
        public GraphEditor() : this(null, null, null) {
            ShowEditorDelegate = DefaultShowEditor;
            ValidateEditorDelegate = DefaultValidateEditor;
            CloseEditorDelegate = DefaultCloseEditor;
            ShowEditorDelegate(null);
            treeImage = Image.FromFile("tree.jpg");
        }

        /// <summary>
        /// A constructor that allows the user to specify functions which deals with the label/property editing
        /// </summary>
        /// <param name="showEditorDelegate">A delegate which constructs and displays the editor for a given object.</param>
        /// <param name="validateEditorDelegate">A delegate which validates the content of the editor and copies it to the graph.</param>
        /// <param name="closeEditorDelegate">A delegate which closes the editor.</param>
        internal GraphEditor(ShowEditor showEditorDelegate, ValidateEditor validateEditorDelegate,
                             CloseEditor closeEditorDelegate) {
            ShowEditorDelegate = showEditorDelegate;
            ValidateEditorDelegate = validateEditorDelegate;
            CloseEditorDelegate = closeEditorDelegate;

            SuspendLayout();
            Controls.Add(gViewer);
            gViewer.Dock = DockStyle.Fill;

            m_LabelBox.Enabled = false;
            m_LabelBox.Dock = DockStyle.Top;
            m_LabelBox.KeyDown += m_LabelBox_KeyDown;

            ResumeLayout();

            /*            this.drawingLayoutEditor = new Microsoft.Msagl.Drawing.DrawingLayoutEditor(this.gViewer,
                           new Microsoft.Msagl.Drawing.DelegateForEdge(edgeDecorator),
                           new Microsoft.Msagl.Drawing.DelegateForEdge(edgeDeDecorator),
                           new Microsoft.Msagl.Drawing.DelegateForNode(nodeDecorator),
                           new Microsoft.Msagl.Drawing.DelegateForNode(nodeDeDecorator),
                           new Microsoft.Msagl.Drawing.MouseAndKeysAnalyser(mouseAndKeysAnalyserForDragToggle));
                        */

            (gViewer as IViewer).MouseDown += Form1_MouseDown;
            (gViewer as IViewer).MouseUp += Form1_MouseUp;


            //   gViewer.DrawingPanel.Paint += new PaintEventHandler(gViewer_Paint);
            gViewer.NeedToCalculateLayout = false;

            toolTip.Active = true;
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 1000;
            toolTip.ReshowDelay = 500;

            ToolBar.ItemClicked += ToolBar_ButtonClick;
        }

        /// <summary>
        /// Returns the GViewer contained in the control.
        /// </summary>
        internal GViewer Viewer {
            get { return gViewer; }
        }

        /// <summary>
        /// Gets or sets the graph associated to the viewer.
        /// </summary>
        internal Graph Graph {
            get { return gViewer.Graph; }
            set { gViewer.Graph = value; }
        }

        /// <summary>
        /// The ToolBar contained in the viewer.
        /// </summary>
        internal ToolStrip ToolBar {
            get {
                foreach (Control c in gViewer.Controls) {
                    var t = c as ToolStrip;
                    if (t != null)
                        return t;
                }
                return null;
            }
        }

        void ToolBar_ButtonClick(object sender, ToolStripItemClickedEventArgs e) {
            foreach (NodeTypeEntry nte in m_NodeTypes)
                if (nte.Button == e.ClickedItem) {
                    var center = new Point();
                    var random = new Random(1);

                    var rect1 = gViewer.ClientRectangle; //gViewer.Graph.GeometryGraph.BoundingBox;
                    var rect2 = gViewer.Graph.BoundingBox;
                    Point p = gViewer.ScreenToSource(rect1.Location);
                    Point p2 = gViewer.ScreenToSource(rect1.Location + rect1.Size);
                    if (p.X < rect2.Left)
                        p.X = rect2.Left;
                    if (p2.X > rect2.Right)
                        p2.X = rect2.Right;
                    if (p.Y > rect2.Top)
                        p.Y = rect2.Top;
                    if (p2.Y < rect2.Bottom)
                        p2.Y = rect2.Bottom;
                    var rect = new Microsoft.Msagl.Core.Geometry.Rectangle(p, p2);

                    center.X = rect.Left + random.NextDouble()*rect.Width;
                    center.Y = rect.Bottom + random.NextDouble()*rect.Height;

                    DrawingNode n = InsertNode(center, nte);
                    if (NodeInsertedByUser != null)
                        NodeInsertedByUser(n);
                }
        }

        string GetNewId() {
            string ret = "_ID" + idcounter++;
            if (gViewer.Graph.FindNode(ret) != null)
                return GetNewId();
            return ret;
        }

        /// <summary>
        /// Changes a node's label and updates the display
        /// </summary>
        /// <param name="n">The node whose label has to be changed</param>
        /// <param name="newLabel">The new label</param>
        internal void RelabelNode(DrawingNode n, string newLabel) {
            n.Label.Text = newLabel;
            gViewer.ResizeNodeToLabel(n);
        }

        /// <summary>
        /// Changes an edge's label and updates the display
        /// </summary>
        /// <param name="e">The edge whose label has to be changed</param>
        /// <param name="newLabel">The new label</param>
        internal void RelabelEdge(Edge e, string newLabel) {
            if (e.Label == null)
                e.Label = new Label(newLabel);
            else
                e.Label.Text = newLabel;


            gViewer.SetEdgeLabel(e, e.Label);
            e.Label.GeometryLabel.InnerPoints = new List<Point>();
            var ep = new EdgeLabelPlacement(gViewer.Graph.GeometryGraph);
            ep.Run();
            gViewer.Graph.GeometryGraph.UpdateBoundingBox();
            gViewer.Invalidate();
        }

        /// <summary>
        /// Inserts a new node at the selected point, with standard attributes, and displays it.
        /// </summary>
        /// <param name="center">The location of the node on the graph</param>
        /// <param name="nte">The NodeTypeEntry structure containing the initial aspect of the node</param>
        /// <param name="id">The id for the node</param>
        /// <returns>The new node</returns>
        internal virtual DrawingNode InsertNode(Point center, NodeTypeEntry nte, string id) {
            var node = new DrawingNode(id);
            node.Label.Text = nte.DefaultLabel;
            node.Attr.FillColor = nte.FillColor;
            node.Label.FontColor = nte.FontColor;
            node.Label.FontSize = nte.FontSize;
            node.Attr.Shape = nte.Shape;
            string s = nte.UserData;
            node.UserData = s;
            IViewerNode dNode = gViewer.CreateIViewerNode(node,center,null);
            gViewer.AddNode(dNode, true);

            return node;
        }

        /// <summary>
        /// Inserts a new node at the selected point, with standard attributes, and displays it.
        /// </summary>
        /// <param name="center">The location of the node on the graph</param>
        /// <param name="nte">The NodeTypeEntry structure containing the initial aspect of the node</param>
        /// <returns>The new node</returns>
        internal virtual DrawingNode InsertNode(Point center, NodeTypeEntry nte) {
            return InsertNode(center, nte, GetNewId());
        }

        void insertNode_Click(object sender, EventArgs e) {
            NodeTypeEntry selectedNTE = null;
            foreach (NodeTypeEntry nte in m_NodeTypes)
                if (nte.MenuItem == sender)
                    selectedNTE = nte;
            DrawingNode n = InsertNode(m_MouseRightButtonDownPoint, selectedNTE);
            if (NodeInsertedByUser != null)
                NodeInsertedByUser(n);
        }


        /// <summary>
        /// 
        /// </summary>
        internal event NodeInsertedByUserHandler NodeInsertedByUser;

        void deleteSelected_Click(object sender, EventArgs e) {
            var al = new ArrayList();
            foreach (IViewerObject ob in gViewer.Entities)
                if (ob.MarkedForDragging)
                    al.Add(ob);
            foreach (IViewerObject ob in al) {
                var edge = ob.DrawingObject as IViewerEdge;
                if (edge != null)
                    gViewer.RemoveEdge(edge, true);
                else {
                    var node = ob as IViewerNode;
                    if (node != null)
                        gViewer.RemoveNode(node, true);
                }
            }
        }

        /// <summary>
        /// Call this to recalculate the graph layout
        /// </summary>
        internal virtual void RedoLayout() {
            gViewer.NeedToCalculateLayout = true;
            gViewer.Graph = gViewer.Graph;
            gViewer.NeedToCalculateLayout = false;
            gViewer.Graph = gViewer.Graph;
        }

        void redoLayout_Click(object sender, EventArgs e) {
            RedoLayout();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        internal void DefaultShowEditor(DrawingObject obj) {
            m_EditingObject = obj;

            if (obj != null)
                m_LabelBox.Enabled = true;
            var node = obj as DrawingNode;
            if (node != null && node.Label != null)
                m_LabelBox.Text = node.Label.Text;
            else {
                var edge = obj as Edge;
                if (edge != null && edge.Label != null)
                    m_LabelBox.Text = edge.Label.Text;
            }
            Controls.Add(m_LabelBox);
            if (obj != null)
                m_LabelBox.Focus();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal bool DefaultValidateEditor() {
            if (m_EditingObject is DrawingNode) {
                RelabelNode((m_EditingObject as DrawingNode), m_LabelBox.Text);
            } else if (m_EditingObject is Edge) {
                RelabelEdge((m_EditingObject as Edge), m_LabelBox.Text);
            }

            gViewer.Invalidate();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void DefaultCloseEditor() {
            m_LabelBox.Enabled = false;
            m_LabelBox.Text = "";
            m_EditingObject = null;
        }

        void m_LabelBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyValue == 13) {
                if (ValidateEditorDelegate())
                    CloseEditorDelegate();
            }
        }

        void Form1_MouseUp(object sender, MsaglMouseEventArgs e) {
            object obj = gViewer.GetObjectAt(e.X, e.Y);
            DrawingNode node = null;
            Edge edge = null;
            var dnode = obj as DNode;
            var dedge = obj as DEdge;
            var dl = obj as DLabel;
            if (dnode != null)
                node = dnode.DrawingNode;
            else if (dedge != null)
                edge = dedge.DrawingEdge;
            else if (dl != null) {
                if (dl.Parent is DNode)
                    node = (dl.Parent as DNode).DrawingNode;
                else if (dl.Parent is DEdge)
                    edge = (dl.Parent as DEdge).DrawingEdge;
            }
            if (node != null) {
                ShowEditorDelegate(node);
            } else if (edge != null) {
                ShowEditorDelegate(edge);
            } else {
                CloseEditorDelegate();
            }
        }

        /// <summary>
        /// Overloaded. Adds a new node type to the list. If the parameter contains an image, a button with that image will be added to the toolbar.
        /// </summary>
        /// <param name="nte">The NodeTypeEntry structure containing the initial aspect of the node, type name, and additional parameters required
        /// for node insertion.</param>
        void AddNodeType(NodeTypeEntry nte) {
            m_NodeTypes.Add(nte);

            if (nte.ButtonImage != null) {
                ToolStrip tb = ToolBar;
                var btn = new ToolStripButton();
                tb.ImageList.Images.Add(nte.ButtonImage);
                btn.ImageIndex = tb.ImageList.Images.Count - 1;
                tb.Items.Add(btn);
                nte.Button = btn;
            }
        }


        /// <summary>
        /// Overloaded. Adds a new node type to the list. If the parameters contain an image, a button with that image will be added to the toolbar.
        /// </summary>
        /// <param name="name">The name for the new node type</param>
        /// <param name="shape">The initial node shape</param>
        /// <param name="fillcolor">The initial node fillcolor</param>
        /// <param name="fontcolor">The initial node fontcolor</param>
        /// <param name="fontsize">The initial node fontsize</param>
        /// <param name="userdata">A string which will be copied into the node userdata</param>
        /// <param name="deflabel">The initial node label</param>
        internal void AddNodeType(string name, Shape shape, Color fillcolor, Color fontcolor, int fontsize,
                                  string userdata, string deflabel) {
            AddNodeType(new NodeTypeEntry(name, shape, fillcolor, fontcolor, fontsize, userdata, deflabel));
        }

        /// <summary>
        /// Builds the context menu for when the user right-clicks on the graph.
        /// </summary>
        /// <param name="point">The point where the user clicked</param>
        /// <returns>The context menu to be displayed</returns>
        protected virtual ContextMenuStrip BuildContextMenu(Point point) {
            var cm = new ContextMenuStrip();

            ToolStripMenuItem mi;
            if (m_NodeTypes.Count == 0) {
                mi = new ToolStripMenuItem();
                mi.Text = "Insert node";
                mi.Click += insertNode_Click;
                cm.Items.Add(mi);
            } else {
                foreach (NodeTypeEntry nte in m_NodeTypes) {
                    mi = new ToolStripMenuItem();
                    mi.Text = "Insert " + nte.Name;
                    mi.Click += insertNode_Click;
                    nte.MenuItem = mi;
                    cm.Items.Add(mi);
                }
            }

            cm.Items.Add(new ToolStripSeparator());

            mi = new ToolStripMenuItem();
            mi.Text = "Delete selected";
            mi.Click += deleteSelected_Click;
            cm.Items.Add(mi);

            mi = new ToolStripMenuItem();
            mi.Text = "Redo layout";
            mi.Click += redoLayout_Click;
            cm.Items.Add(mi);

            return cm;
        }

        void mi_DrawItem(object sender, DrawItemEventArgs e) {
            e.DrawBackground();
            if ((e.State & DrawItemState.Selected) != DrawItemState.Selected)
                e.Graphics.FillRectangle(SystemBrushes.Control, e.Bounds);

            var nte = (m_NodeTypes[e.Index] as NodeTypeEntry);
            int x = 14;
            if (nte.ButtonImage != null) {
                e.Graphics.DrawImage(nte.ButtonImage, e.Bounds.X, e.Bounds.Y);
                x = nte.ButtonImage.Width + 1;
            }
            var mi = sender as ToolStripMenuItem;
            var h = (int) e.Graphics.MeasureString(mi.Text, Font).Height;
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                e.Graphics.DrawString(mi.Text, Font, SystemBrushes.HighlightText, x,
                                      (e.Bounds.Height - h)/2 + e.Bounds.Y);
            else
                e.Graphics.DrawString(mi.Text, Font, SystemBrushes.ControlText, x, (e.Bounds.Height - h)/2 + e.Bounds.Y);
        }

        void mi_MeasureItem(object sender, MeasureItemEventArgs e) {
            if (e.Index < m_NodeTypes.Count) {
                var nte = (m_NodeTypes[e.Index] as NodeTypeEntry);

                var mi = sender as ToolStripMenuItem;
                e.ItemHeight = SystemInformation.MenuHeight;
                e.ItemWidth = (int) e.Graphics.MeasureString(mi.Text, Font).Width;

                if (nte.ButtonImage != null) {
                    if (e.ItemHeight < nte.ButtonImage.Height)
                        e.ItemHeight = nte.ButtonImage.Height;
                    e.ItemWidth += nte.ButtonImage.Width + 1;
                }
            }
        }

        void Form1_MouseDown(object sender, MsaglMouseEventArgs e) {
            /*if (e.RightButtonIsPressed && this.gViewer.DrawingLayoutEditor.SelectedEdge != null && this.gViewer.ObjectUnderMouseCursor == this.gViewer.DrawingLayoutEditor.SelectedEdge)
                ProcessRightClickOnSelectedEdge(e);
            else*/
            if (e.RightButtonIsPressed && !e.Handled) {
                m_MouseRightButtonDownPoint = (gViewer).ScreenToSource(e);

                ContextMenuStrip cm = BuildContextMenu(m_MouseRightButtonDownPoint);

                cm.Show(this, new System.Drawing.Point(e.X, e.Y));
            }
        }

        #region Nested type: CloseEditor

        /// <summary>
        /// Closes the object editor
        /// </summary>
        internal delegate void CloseEditor();

        #endregion

        #region Nested type: EdgeInsertedByUserHandler

        /// <summary>
        /// 
        /// </summary>
        /// <param name="edge"></param>
        internal delegate void EdgeInsertedByUserHandler(Edge edge);

        #endregion

        #region Nested type: NodeInsertedByUserHandler

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        internal delegate void NodeInsertedByUserHandler(DrawingNode node);

        #endregion

        #region Nested type: NodeTypeEntry

        /// <summary>
        /// Contains all the information needed to allow the user to insert a node of a specific type. This includes a name and, optionally, an image
        /// to associate with the type, as well as several default aspect factors for the node.
        /// </summary>
        internal class NodeTypeEntry {
            /// <summary>
            /// If this node type has an associated button, then this will contain a reference to the button.
            /// </summary>
            internal ToolStripButton Button;

            /// <summary>
            /// If this is not null, then a button will be created in the toolbar, which allows the user to insert a node.
            /// </summary>
            internal Image ButtonImage;

            /// <summary>
            /// The initial label for the node.
            /// </summary>
            internal string DefaultLabel;

            /// <summary>
            /// The initial fillcolor of the node.
            /// </summary>
            internal Color FillColor;

            /// <summary>
            /// The initial fontcolor of the node.
            /// </summary>
            internal Color FontColor;

            /// <summary>
            /// The initial fontsize of the node.
            /// </summary>
            internal int FontSize;

            /// <summary>
            /// This will contain the menu item to which this node type is associated.
            /// </summary>
            internal ToolStripMenuItem MenuItem;

            /// <summary>
            /// The name for this type.
            /// </summary>
            internal string Name;

            /// <summary>
            /// The initial shape of the node.
            /// </summary>
            internal Shape Shape;

            /// <summary>
            /// A string which will be initially copied into the user data of the node.
            /// </summary>
            internal string UserData;

            /// <summary>
            /// Constructs a NodeTypeEntry with the supplied parameters.
            /// </summary>
            /// <param name="name">The name for the node type</param>
            /// <param name="shape">The initial node shape</param>
            /// <param name="fillcolor">The initial node fillcolor</param>
            /// <param name="fontcolor">The initial node fontcolor</param>
            /// <param name="fontsize">The initial node fontsize</param>
            /// <param name="userdata">A string which will be copied into the node userdata</param>
            /// <param name="deflabel">The initial label for the node</param>
            /// <param name="button">An image which will be used to create a button in the toolbar to insert a node</param>
            internal NodeTypeEntry(string name, Shape shape, Color fillcolor, Color fontcolor, int fontsize,
                                   string userdata, string deflabel, Image button) {
                Name = name;
                Shape = shape;
                FillColor = fillcolor;
                FontColor = fontcolor;
                FontSize = fontsize;
                UserData = userdata;
                ButtonImage = button;
                DefaultLabel = deflabel;
            }

            /// <summary>
            /// Constructs a NodeTypeEntry with the supplied parameters.
            /// </summary>
            /// <param name="name">The name for the node type</param>
            /// <param name="shape">The initial node shape</param>
            /// <param name="fillcolor">The initial node fillcolor</param>
            /// <param name="fontcolor">The initial node fontcolor</param>
            /// <param name="fontsize">The initial node fontsize</param>
            /// <param name="userdata">A string which will be copied into the node userdata</param>
            /// <param name="deflabel">The initial label for the node</param>
            internal NodeTypeEntry(string name, Shape shape, Color fillcolor, Color fontcolor, int fontsize,
                                   string userdata, string deflabel)
                : this(name, shape, fillcolor, fontcolor, fontsize, userdata, deflabel, null) {
            }
        }

        #endregion

        #region Nested type: ShowEditor

        /// <summary>
        /// Displays an editor for the selected object.
        /// </summary>
        /// <param name="obj"></param>
        internal delegate void ShowEditor(DrawingObject obj);

        #endregion

        #region Nested type: ValidateEditor

        /// <summary>
        /// Validates the data inserted by the user into the object editor, and if successful transfers it to the graph.
        /// </summary>
        /// <returns>Returns true if the data was successfully validated.</returns>
        internal delegate bool ValidateEditor();

        #endregion


        float radiusRatio = 0.3f;

        public ICurve GetNodeBoundary(DrawingNode node) {
            double width = treeImage.Width/2.0;
            double height = treeImage.Height/2.0;

            return CurveFactory.CreateRectangleWithRoundedCorners(width, height, width * radiusRatio, height * radiusRatio, new Point());
        }

        public bool DrawNode18(DrawingNode node, object graphics){
            var g = (Graphics)graphics;

            //flip the image around its center
            using (Matrix m = g.Transform){
                using (Matrix saveM = m.Clone()){
                    var clipNow = g.Clip;
                    g.Clip = new Region(Draw.CreateGraphicsPath(node.GeometryNode.BoundaryCurve));
                    var geomNode = node.GeometryNode;
                    var leftTop = geomNode.BoundingBox.LeftTop;
                    using (var m2 = new Matrix(1, 0, 0, -1, (float) leftTop.X, (float) leftTop.Y)){

                        m.Multiply(m2);
                        g.Transform = m;
                        var rect = new RectangleF(0, 0, (float)geomNode.Width, (float)geomNode.Height);
                        using (var lBrush = new LinearGradientBrush(rect, System.Drawing.Color.Red,
                                                                    System.Drawing.Color.Yellow,
                                                                    LinearGradientMode.BackwardDiagonal)
                            ){
                            g.FillRectangle(lBrush, rect);
                        }
                        g.Transform = saveM;
                        g.Clip = clipNow;
                    }
                }
            }
            return false; //returning false would enable the default rendering

        }
    }
}