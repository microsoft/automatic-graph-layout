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
using System.ComponentModel;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Prototype.LayoutEditing;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Spline.Bundling;
using Microsoft.Msagl.Prototype.Ranking;
using Microsoft.Msagl.Routing.Rectilinear;
using DrawingGraph = Microsoft.Msagl.Drawing.Graph;
using GeometryEdge = Microsoft.Msagl.Core.Layout.Edge;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using MsaglPoint = Microsoft.Msagl.Core.Geometry.Point;
using MsaglRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using WinPoint = System.Windows.Point;
using Stream = System.IO.Stream;
using MsaglPolyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public enum DraggingMode
    {
        Pan,
        WindowZoom,
        ComboInsertion,
        EdgeInsertion,
        LayoutEdit,
        None
    };

    public class GeneratingPopupEventArgs : EventArgs
    {
        public ListBox ListBox { get; private set; }
        public MsaglPoint MousePos { get; private set; }

        public GeneratingPopupEventArgs(ListBox listBox, MsaglPoint mousePos)
            : base()
        {
            ListBox = listBox;
            MousePos = mousePos;
        }
    }

    public partial class DGraph : DObject, IViewerGraph, IViewer, INotifyPropertyChanged
    {
        public static Brush BlackBrush = new SolidColorBrush(Colors.Black);
        public static Brush TransparentBrush = new SolidColorBrush(Colors.Transparent);

        public DGraph()
            : this(null)
        {
        }

        public DGraph(DNestedGraphLabel parent)
            : base(parent)
        {
            InitializeComponent();

            NeedToCalculateLayout = true;
            ContentEditorProvider = new ContentEditorProvider(this);

            Edges = new List<DEdge>();
            NodeMap = new Dictionary<IComparable, DNode>();

            DrawingLayoutEditor = new LayoutEditor(this);
            DrawingLayoutEditor.ChangeInUndoRedoList += (sender, args) =>
            {
                FirePropertyChanged("CanUndo");
                FirePropertyChanged("CanRedo");
            };

            // I'm doing a slight change to the default toggle entity predicate: I don't want the user to be able to select nodes while I'm in
            // insertion mode. Note that this only concerns the DrawingLayoutEditor, which deals with dragging - by changing the toggle entity
            // predicate, I affect selection of entities while dragging a selection window, NOT selection by simple click. So, this ensures that
            // the user cannot draw a selection window (which doesn't make sense in editing mode), but he can still select single entities by
            // clicking on them.
            var defaultToggleEntityPredicate = DrawingLayoutEditor.ToggleEntityPredicate;
            DrawingLayoutEditor.ToggleEntityPredicate = (modKeys, mButtons, dragPar) => defaultToggleEntityPredicate(modKeys, mButtons, dragPar) && MouseMode != DraggingMode.ComboInsertion && MouseMode != DraggingMode.EdgeInsertion;

            // These decorators are exactly the same as the default ones, but they use my LineWidth fix (see DNode.cs).
            // Once the LineWidth bug in MSAGL is fixed, we should remove these.
            DrawingLayoutEditor.DecorateObjectForDragging = DecorateObjectForDragging;
            DrawingLayoutEditor.RemoveObjDraggingDecorations = RemoveObjDraggingDecorations;

            EdgeRoutingMode = EdgeRoutingMode.Spline;

            MouseMode = DefaultMouseMode = DraggingMode.Pan;

            BeginContentEdit = DefaultBeginContentEdit;
            //EndContentEdit = DefaultEndContentEdit;

            MouseLeftButtonDoubleClick += new EventHandler<MsaglMouseEventArgs>(DGraph_MouseLeftButtonDoubleClick);

            Clear();
        }

        public bool RouteEdgesAfterDragging { get; set; }

        private Dictionary<IViewerObject, Action> m_DecoratorRemovalsDict = new Dictionary<IViewerObject, Action>();

        protected void DecorateObjectForDragging(IViewerObject obj)
        {
            if (m_DecoratorRemovalsDict.ContainsKey(obj))
                return;
            InvalidateBeforeTheChange(obj);
            var node = obj as DNode;
            if (node != null)
            {
                var w = node.LineWidth;
                m_DecoratorRemovalsDict[node] = (delegate() { node.LineWidth = w; });
                node.LineWidth = (int)Math.Max(LineThicknessForEditing, w * 2);
            }
            else
            {
                var edge = obj as DEdge;
                if (edge != null)
                {
                    var w = edge.LineWidth;
                    m_DecoratorRemovalsDict[edge] = (delegate() { edge.LineWidth = w; });
                    edge.LineWidth = (int)Math.Max(LineThicknessForEditing, w * 2);
                }
            }
            Invalidate(obj);
        }

        protected void RemoveObjDraggingDecorations(IViewerObject obj)
        {
            InvalidateBeforeTheChange(obj);
            Action decoratorRemover;
            if (m_DecoratorRemovalsDict.TryGetValue(obj, out decoratorRemover))
            {
                decoratorRemover();
                m_DecoratorRemovalsDict.Remove(obj);
                Invalidate(obj);
            }
            var node = obj as DNode;
            if (node != null)
                foreach (var edge in node.Edges)
                    RemoveObjDraggingDecorations(edge);
        }

        /// <summary>
        /// Set to true to show a black border around the graph.
        /// </summary>
        public bool ShowBorder
        {
            get
            {
                return ParentBorder.BorderBrush != TransparentBrush;
            }
            set
            {
                if (value)
                    ParentBorder.BorderBrush = BlackBrush;
                else
                    ParentBorder.BorderBrush = TransparentBrush;
                if (UpdateGraphBoundingBoxPreservingCenter())
                    Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the dragging mode that's used in case the current mouse mode becomes invalid (for example, because the Allow*Editing settings
        /// have changed).
        /// </summary>
        public DraggingMode DefaultMouseMode { get; set; }

        public override void MakeVisual()
        {

        }

        public override DrawingObject DrawingObject
        {
            get { return DrawingGraph; }
        }

        public List<DEdge> Edges { get; private set; }
        public Dictionary<IComparable, DNode> NodeMap { get; private set; }
        public IEnumerable<IViewerObject> Entities
        {
            get
            {
                foreach (DEdge dEdge in Edges)
                {
                    yield return dEdge;
                    if (dEdge.Label != null)
                        yield return dEdge.Label;
                }

                foreach (DNode dNode in NodeMap.Values)
                    yield return dNode;
            }
        }

        #region IViewerGraph Members

        private Graph m_DrawingGraph;
        public Graph DrawingGraph
        {
            get
            {
                return m_DrawingGraph;
            }
            set
            {
                m_DrawingGraph = value;
                if (GraphChanged != null)
                    GraphChanged(this, EventArgs.Empty);
                Invalidate();
            }
        }

        public IEnumerable<IViewerNode> Nodes() { return NodeMap.Values.Cast<IViewerNode>(); }

        IEnumerable<IViewerEdge> IViewerGraph.Edges() { return Edges.Cast<IViewerEdge>(); }

        #endregion

        internal void UpdateWidthAndHeight()
        {
            ParentBorder.Width = DrawingGraph.Width * Zoom;
            ParentBorder.Height = DrawingGraph.Height * Zoom;
            MainCanvas.Width = Zoom * (Graph.GeometryGraph.Width + 2.0 * Graph.GeometryGraph.Margins);
            MainCanvas.Height = Zoom * (Graph.GeometryGraph.Height + 2.0 * Graph.GeometryGraph.Margins);
        }

        // Zooming is done through a XAML render transform, which is applied to the main canvas.
        private double m_Zoom = 1.0;
        public double Zoom
        {
            get { return m_Zoom; }
            set
            {
                if (double.IsInfinity(value))
                    value = 1.0;

                // I need to store the center and then re-center the scroll viewer, because if I just change the zoom without moving the
                // scroll bars, then the zoom will effectively be centered on the top-left corner.
                double currentCenterX = (MainScrollViewer.HorizontalOffset + DrawingGraph.Width / 2.0) / (double)Zoom;
                double currentCenterY = (MainScrollViewer.VerticalOffset + DrawingGraph.Height / 2.0) / (double)Zoom;

                m_Zoom = value;

                // I also need to change the border height and width (because the border is outside the scope of the render transform,
                // which in turn is because I don't want it to become thicker or thinner when zooming).
                UpdateWidthAndHeight();

                if (DrawingGraph != null)
                    SetRenderTransform();

                MainScrollViewer.ScrollToHorizontalOffset(currentCenterX * (double)Zoom - DrawingGraph.Width / 2.0);
                MainScrollViewer.ScrollToVerticalOffset(currentCenterY * (double)Zoom - DrawingGraph.Height / 2.0);

                FirePropertyChanged("Zoom");
            }
        }

        public static DependencyProperty ScrollBarVisibilityProperty = DependencyProperty.Register("ScrollBarVisibility", typeof(ScrollBarVisibility), typeof(DGraph), new PropertyMetadata(ScrollBarVisibility.Auto));
        public ScrollBarVisibility ScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(ScrollBarVisibilityProperty); }
            set { SetValue(ScrollBarVisibilityProperty, value); }
        }

        private int m_IDCounter = 1;
        /// <summary>
        /// Generates a fresh ID for a new node. You don't need to use IDs generated by this function, but if you don't care what
        /// IDs the nodes get, this is a handy way to make sure each node has a unique ID.
        /// </summary>
        /// <returns>A string which is not the ID of any node currently in the graph.</returns>
        public string GetNewNodeID()
        {
            if (Graph != null)
            {
                string id;
                do
                {
                    id = string.Format("_ID{0}", m_IDCounter++);
                }
                while (Graph.NodeMap.ContainsKey(id));
                return id;
            }
            return "_ID1";
        }

        /// <summary>
        /// Updates the graph bounding box to make it fit the graph content.
        /// </summary>
        public void FitGraphBoundingBox()
        {
            if (DrawingLayoutEditor != null)
            {
                if (Graph != null)
                    DrawingLayoutEditor.FitGraphBoundingBox(this);
                Invalidate();
            }
        }

        private DCluster m_RootCluster;
        public DCluster AddRootCluster()
        {
            return AddCluster(null, null);
        }
        public DCluster AddRootCluster(string id)
        {
            return AddCluster(null, id);
        }
        public DCluster AddCluster(DCluster parent)
        {
            return AddCluster(parent, null);
        }
        public DCluster AddCluster(DCluster parent, string id)
        {
            if (parent == null)
            {
                Graph.LayoutAlgorithmSettings = new FastIncrementalLayoutSettings();
                Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.KeepOriginalSpline = true;
                var settings = Graph.LayoutAlgorithmSettings as FastIncrementalLayoutSettings;
                settings.AvoidOverlaps = true;
                settings.NodeSeparation = 30;
                settings.RouteEdges = true;
            }

            Subgraph cluster = new Subgraph(id == null ? GetNewNodeID() : id);
            Cluster geometryCluster = new Cluster() { GeometryParent = parent == null ? Graph.GeometryGraph : (GeometryObject)parent.GeometryNode };
            cluster.GeometryNode = geometryCluster;
            geometryCluster.UserData = cluster;

            DCluster ret = new DCluster((DObject)parent ?? this, cluster);
            cluster.LabelText = null;
            SetNodeBoundaryCurve(Graph, cluster);

            if (parent == null)
                m_RootCluster = ret;
            else
                parent.AddCluster(ret);

            if (UpdateGraphBoundingBoxPreservingCenter())
                Invalidate();

            AddCluster(ret, false);

            return ret;
        }

        public void AddCluster(IViewerNode dCluster, bool registerForUndo)
        {
            DCluster cluster = dCluster as DCluster;
            Cluster geometryCluster = cluster.GeometryCluster;
            Subgraph drawingCluster = cluster.DrawingCluster;

            // Add the node to my node map, to the drawing graph, and to the geometry graph.
            NodeMap[drawingCluster.Id] = cluster;
            if (cluster.ParentObject == this)
                Graph.RootSubgraph = drawingCluster;
            else
                (cluster.ParentObject as DCluster).DrawingCluster.AddSubgraph(drawingCluster);
            if (cluster.ParentObject == this)
                Graph.GeometryGraph.RootCluster = geometryCluster;
            else
                (cluster.ParentObject as DCluster).GeometryCluster.AddChild(geometryCluster);
        }

        public void AddNodeToCluster(DCluster owner, DNode node)
        {
            node.ParentObject = owner;
            owner.AddNode(node);
            owner.DrawingCluster.AddNode(node.DrawingNode);
            owner.GeometryCluster.AddChild(node.GeometryNode);
        }

        public DNode AddNode()
        {
            return AddNode(false);
        }

        public DNode AddNode(string id)
        {
            return AddNode(id, false);
        }

        public DNode AddNode(bool registerForUndo)
        {
            return AddNode(null as string, registerForUndo);
        }

        public DNode AddNode(string id, bool registerForUndo)
        {
            return AddNodeAtLocation(new MsaglPoint(0.0, 0.0), id, registerForUndo);
        }

        public DNode AddNodeAtLocation(MsaglPoint p)
        {
            return AddNodeAtLocation(p, false);
        }

        public DNode AddNodeAtLocation(MsaglPoint p, string id)
        {
            return AddNodeAtLocation(p, id, false);
        }

        public DNode AddNodeAtLocation(MsaglPoint p, bool registerForUndo)
        {
            return AddNodeAtLocation(p, null, registerForUndo);
        }

        public DNode AddNodeAtLocation(MsaglPoint p, string id, bool registerForUndo)
        {
            DrawingNode node = new DrawingNode(id == null ? GetNewNodeID() : id);
            node.Attr.Shape = Drawing.Shape.Ellipse;
            GeometryNode geometryNode = new GeometryNode() { GeometryParent = Graph.GeometryGraph }; // NEWMSAGL: geometry nodes no longer have an id?
            node.GeometryNode = geometryNode;
            geometryNode.UserData = node;

            DNode dNode = CreateNode(node) as DNode;
            geometryNode.Center = p;
            AddNode(dNode, registerForUndo);

            if (registerForUndo)
            {
                DrawingLayoutEditor.RegisterNodeAdditionForUndo(dNode);
                var bounds = Graph.GeometryGraph.BoundingBox;
                bounds.Add(node.BoundingBox.LeftTop);
                bounds.Add(node.BoundingBox.RightBottom);
                Graph.GeometryGraph.BoundingBox = bounds;
                DrawingLayoutEditor.CurrentUndoAction.GraphBoundingBoxAfter = Graph.BoundingBox;
            }

            if (UpdateGraphBoundingBoxPreservingCenter())
                Invalidate();

            if (MouseMode == DraggingMode.EdgeInsertion)
            {
                DrawingLayoutEditor.ForgetEdgeDragging();
                DrawingLayoutEditor.PrepareForEdgeDragging();
            }

            return dNode;
        }

        private bool UpdateGraphBoundingBoxPreservingCenter()
        {
            var center = Graph.GeometryGraph.BoundingBox.Center;
            var oldbb = Graph.GeometryGraph.BoundingBox;
            Graph.GeometryGraph.UpdateBoundingBox();
            if (oldbb != Graph.GeometryGraph.BoundingBox)
            {
                CenterBoundingBox(center);
                return true;
            }
            return false;
        }

        private void CenterBoundingBox(MsaglPoint c)
        {
            MsaglPoint diff = Graph.GeometryGraph.BoundingBox.Center - c;
            MsaglPoint np = Graph.GeometryGraph.BoundingBox.Center;
            if (diff.X > 0.0)
                np.X = Graph.GeometryGraph.BoundingBox.Left - diff.X * 2.0;
            else if (diff.X < 0.0)
                np.X = Graph.GeometryGraph.BoundingBox.Right - diff.X * 2.0;
            if (diff.Y > 0.0)
                np.Y = Graph.GeometryGraph.BoundingBox.Bottom - diff.Y * 2.0;
            else if (diff.Y < 0.0)
                np.Y = Graph.GeometryGraph.BoundingBox.Top - diff.Y * 2.0;
            Graph.GeometryGraph.BoundingBox.Add(np);
            foreach (var el in Entities.Concat(m_CrossEdges))
            {
                var go = (el as DObject).GeometryObject;
                if (go != null)
                    go.BoundingBox.Add(np);
            }
            //var bb = Graph.GeometryGraph.BoundingBox;
            //bb.Add(np);
            //Graph.GeometryGraph.BoundingBox = bb;
            UpdateWidthAndHeight();
        }

        private bool IsContainedSub(DNode n)
        {
            if (Nodes().Contains(n))
                return true;
            foreach (DGraph g in NestedGraphs)
                if (g.IsContainedSub(n))
                    return true;
            return false;
        }

        private List<DEdge> m_CrossEdges = new List<DEdge>();
        private DEdge AddCrossEdge(DNode source, DNode target)
        {
            DEdge dEdge = new DEdge(source, target, new DrawingEdge(source.Node, target.Node, ConnectionToGraph.Disconnected), ConnectionToGraph.Disconnected);
            GeometryEdge gEdge = new GeometryEdge(source.GeometryNode, target.GeometryNode) { GeometryParent = Graph.GeometryGraph };
            dEdge.GeometryEdge = gEdge;
            dEdge.ArrowheadAtTarget = ArrowStyle.Normal;

            m_CrossEdges.Add(dEdge);

            return dEdge;

            /*DEdge ret;
            var s = VisualTreeHelper.GetRoot(this);
            var ss = VisualTreeHelper.GetRoot(source);
            var ts = VisualTreeHelper.GetRoot(target);
            if (s != ss || s != ts || ss != ts)
                ret = new DEdge(source, target, null, ConnectionToGraph.Disconnected);
            else
                ret = NestedGraphHelper.DrawCrossEdge(this, source, target);
            m_CrossEdges.Add(ret);
            return ret;*/
        }

        public void RemoveCrossEdge(DEdge edge)
        {
            m_CrossEdges.Remove(edge);
            if (MainCanvas.Children.Contains(edge))
                MainCanvas.Children.Remove(edge);
        }

        public IEnumerable<DEdge> CrossEdges
        {
            get
            {
                if (IsNestedGraph)
                    return m_CrossEdges.Concat((ParentObject as DNestedGraphLabel).ParentGraph.CrossEdges);
                return m_CrossEdges;
            }
        }

        public DEdge AddEdgeBetweenNodes(DNode source, DNode target)
        {
            if (!IsContainedSub(source))
                throw new InvalidOperationException("The edge source node must be in the graph or in one of its nested graphs.");
            if (!IsContainedSub(target))
                throw new InvalidOperationException("The edge target node must be in the graph or in one of its nested graphs.");
            if (source.ParentGraph == this && target.ParentGraph == this)
            {
                DEdge dEdge = new DEdge(source, target, new DrawingEdge(source.Node, target.Node, ConnectionToGraph.Disconnected), ConnectionToGraph.Disconnected);
                GeometryEdge gEdge = new GeometryEdge(source.GeometryNode, target.GeometryNode) { GeometryParent = Graph.GeometryGraph };
                dEdge.GeometryEdge = gEdge;
                dEdge.ArrowheadAtTarget = ArrowStyle.Normal;

                AddEdge(dEdge, false);

                return dEdge;
            }
            else
                return AddCrossEdge(source, target);
        }

        public void ResizeNodeToLabel(DNode node)
        {
            if (node.Label == null)
                return;
            double width = Math.Max(node.Label.ActualWidth, node.Label.Width);
            double height = Math.Max(node.Label.ActualHeight, node.Label.Height);
            width += 2 * node.Node.Attr.LabelMargin;
            height += 2 * node.Node.Attr.LabelMargin;
            if (width < Graph.Attr.MinNodeWidth)
                width = Graph.Attr.MinNodeWidth;
            if (height < Graph.Attr.MinNodeHeight)
                height = Graph.Attr.MinNodeHeight;

            SetNodeBoundaryCurve(Graph, node.DrawingNode, node.Label);

            var allEdges = node.Edges.Concat(CrossEdges.Where(ed => ed.Source == node || ed.Target == node));
            foreach (GeometryEdge e in allEdges.Select(ed => ed.GeometryEdge))
            {
                if (e.UnderlyingPolyline != null)
                {
                    Curve curve = e.UnderlyingPolyline.CreateCurve();
                    if (!Arrowheads.TrimSplineAndCalculateArrowheads(e.EdgeGeometry, e.Source.BoundaryCurve, e.Target.BoundaryCurve, curve, false, true))
                        Arrowheads.CreateBigEnoughSpline(e);
                }
            }

            if (UpdateGraphBoundingBoxPreservingCenter())
                Invalidate();
            else
            {
                Invalidate(node);
                foreach (DObject obj in node.Edges)
                    Invalidate(obj);
            }
        }

        #region Mouse handling

        private DraggingMode m_MouseMode;
        public DraggingMode MouseMode
        {
            get
            {
                return m_MouseMode;
            }
            set
            {
                bool wasInserting = m_MouseMode == DraggingMode.ComboInsertion || m_MouseMode == DraggingMode.EdgeInsertion;
                m_MouseMode = value;
                bool isInserting = m_MouseMode == DraggingMode.ComboInsertion || m_MouseMode == DraggingMode.EdgeInsertion;
                if (!wasInserting && isInserting)
                {
                    DrawingLayoutEditor.InsertingEdge = true;
                    DrawingLayoutEditor.PrepareForEdgeDragging();
                }
                else if (wasInserting && !isInserting)
                {
                    DrawingLayoutEditor.InsertingEdge = false;
                    DrawingLayoutEditor.ForgetEdgeDragging();
                }
                DrawingLayoutEditor.Clear();
                if (m_LabelEditor != null)
                {
                    m_EditingLabel = false;
                    RemoveObjDraggingDecorations(m_LabelEditor.EditTarget);
                    LayoutRoot.Children.Remove(m_LabelEditor);
                    m_LabelEditor = null;
                }
                if (MouseModeChanged != null)
                    MouseModeChanged(null, EventArgs.Empty);
                foreach (DGraph dg in NestedGraphs)
                    dg.MouseMode = MouseMode;
            }
        }

        private WinPoint GetMouseScreenSpacePosition(System.Windows.Input.MouseEventArgs e)
        {
            return e.GetPosition(this);
        }

        private MsaglPoint GetMouseGraphSpacePosition(System.Windows.Input.MouseEventArgs e)
        {
            var p = e.GetPosition(MainCanvas);
            return new MsaglPoint(p.X, p.Y);
        }

        public event EventHandler MouseModeChanged;
        private WinPoint MousePosition { get; set; }
        private Nullable<WinPoint> MouseDownPosition { get; set; }

        public event EventHandler NodeInsertedByUser;
        public event EventHandler NodeInsertingByUser;
        public event EventHandler EdgeInsertedByUser;
        public event EventHandler NodeDeletedByUser;
        public event EventHandler EdgeDeletedByUser;

        private WinPoint MouseRightPosition { get; set; }

        void OnMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MouseMode == DraggingMode.None)
                return;
            WinPoint pos = GetMouseScreenSpacePosition(e);
            MsaglPoint graphPos = GetMouseGraphSpacePosition(e);
            if (MouseUp != null)
                MouseUp(this, new ViewerMouseEventArgs((int)graphPos.X, (int)graphPos.Y, false, false, false, 0));
            UpdateObjectUnderCursor(graphPos);
        }

        void OnMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MouseMode == DraggingMode.None)
                return;
            WinPoint pos = GetMouseScreenSpacePosition(e);
            MsaglPoint graphPos = GetMouseGraphSpacePosition(e);
            MouseRightPosition = e.GetPosition(null);
            if (MouseDown != null)
                MouseDown(this, new ViewerMouseEventArgs((int)graphPos.X, (int)graphPos.Y, false, false, true, 1));

            if (m_CurrentPopup == null)
                ShowStandardPopup(graphPos);

            e.Handled = true; // Prevent "Silverlight" popup
        }

        private static double Distance(WinPoint p1, WinPoint p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y * p2.Y));
        }

        void OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MouseMode == DraggingMode.None)
                return;
            var pos = GetMouseScreenSpacePosition(e);
            var graphPos = GetMouseGraphSpacePosition(e);

            bool finishingEdgeDrawing = RubberEdgePath != null;

            //var preselobj = SelectedObjects.ToArray();
            if (MouseUp != null)
                MouseUp(this, new ViewerMouseEventArgs((int)graphPos.X, (int)graphPos.Y, false, false, false, 0));
            /*if (SelectedObjectsChanged != null && preselobj.Except(SelectedObjects).Union(SelectedObjects.Except(preselobj)).Count() > 0) // is there a more efficient way to find out if a set has changed?
                SelectedObjectsChanged(this, EventArgs.Empty); //*/

            if (MouseDownPosition.HasValue)
                EndDragging(pos, graphPos);

            if (MouseMode == DraggingMode.ComboInsertion && ObjectUnderMouseCursor == null && !finishingEdgeDrawing && MouseDownPosition.HasValue && Distance(MouseDownPosition.Value, pos) < 5.0)
            {
                DrawingLayoutEditor.ForgetEdgeDragging();
                DNode newNode = AddNodeAtLocation(graphPos, true);
                BeginContentEdit(newNode);
                DrawingLayoutEditor.PrepareForEdgeDragging();
            }

            MouseDownPosition = null;
            UpdateObjectUnderCursor(graphPos);
        }

        public delegate void BeginContentEditDelegate(DObject obj);
        public BeginContentEditDelegate BeginContentEdit;

        public ContentEditorProvider ContentEditorProvider { get; set; }

        private bool m_EditingLabel;
        private LabelEditor m_LabelEditor;
        public LabelEditor LabelEditor { get { return m_LabelEditor; } }
        private void DefaultBeginContentEdit(DObject obj)
        {
            if (m_EditingLabel)
                return;
            FrameworkElement fe = ContentEditorProvider.GetNewGUIInstance(obj);
            if (fe != null)
            {
                DecorateObjectForDragging(obj);
                m_LabelEditor = new LabelEditor() { EditControl = fe, EditTarget = obj, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                m_LabelEditor.Closed += (sender, args) =>
                    {
                        RemoveObjDraggingDecorations(obj);
                        if (m_LabelEditor.OK)
                            ContentEditorProvider.UpdateLabel(fe, obj);
                        m_EditingLabel = false;
                        LayoutRoot.Children.Remove(m_LabelEditor);
                        m_LabelEditor = null;
                    };
                m_EditingLabel = true;
                LayoutRoot.Children.Add(m_LabelEditor);
                SynchronizationContext.Current.Post(o => ContentEditorProvider.FocusGUI(fe, obj), null);
            }
        }

        private void RemoveLabelEditor()
        {
        }

        void DGraph_MouseLeftButtonDoubleClick(object sender, MsaglMouseEventArgs e)
        {
            if (ObjectUnderMouseCursor != null)
                BeginContentEdit(ObjectUnderMouseCursor as DObject);
        }


        public event EventHandler<MsaglMouseEventArgs> MouseLeftButtonDoubleClick;

        private DispatcherTimer m_DoubleClickDetector = null;
        void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MouseMode == DraggingMode.None)
                return;
            var pos = GetMouseScreenSpacePosition(e);
            var graphPos = GetMouseGraphSpacePosition(e);
            if (MouseDown != null)
                MouseDown(this, new ViewerMouseEventArgs((int)graphPos.X, (int)graphPos.Y, true, false, false, 1));

            MouseDownPosition = pos;
            StartDragging(pos, graphPos);

            if (m_DoubleClickDetector != null)
            {
                if (MouseLeftButtonDoubleClick != null)
                    MouseLeftButtonDoubleClick(this, new ViewerMouseEventArgs((int)graphPos.X, (int)graphPos.Y, true, false, false, 1));
            }
            else
            {
                m_DoubleClickDetector = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(300.0) };
                m_DoubleClickDetector.Tick += (sender2, e2) => { m_DoubleClickDetector.Stop(); m_DoubleClickDetector = null; };
                m_DoubleClickDetector.Start();
            }
        }

        /*        public event EventHandler SelectedObjectsChanged;

                public IEnumerable<DObject> SelectedObjects
                {
                    get
                    {
                        foreach (DObject obj in Entities)
                            if (obj.MarkedForDragging)
                                yield return obj;
                    }
                } //*/

        void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            /*var pos = GetMouseScreenSpacePosition(e);
            var graphPos = GetMouseGraphSpacePosition(e);
            if (MouseUp != null)
                MouseUp(this, new ViewerMouseEventArgs((int)graphPos.X, (int)graphPos.Y, false, false, false, 0));

            if (MouseDownPosition.HasValue)
                EndDragging(pos);
            MouseDownPosition = null;*/
        }

        void UpdateObjectUnderCursor(MsaglPoint graphPos)
        {
            /*
            var o = GetObjectAtPosition(graphPos);
            if (o != ObjectUnderMouseCursor)
            {
                var old = ObjectUnderMouseCursor;
                ObjectUnderMouseCursor = o;
                if (ObjectUnderMouseCursorChanged != null)
                    ObjectUnderMouseCursorChanged(this, new ObjectUnderMouseCursorChangedEventArgs(old, ObjectUnderMouseCursor));
            }*/
        }

        void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var pos = GetMouseScreenSpacePosition(e);
            var graphPos = GetMouseGraphSpacePosition(e);

            UpdateObjectUnderCursor(graphPos);

            MsaglPoint c = Graph.BoundingBox.Center;

            m_Dragging = true;
            if (MouseMove != null)
            {
                try // prevent byzantine MSAGL exceptions from crashing the program
                {
                    MouseMove(this, new ViewerMouseEventArgs((int)graphPos.X, (int)graphPos.Y, (MouseDownPosition != null), false, false, 0));
                }
                catch (Exception exc)
                {
                    Console.Error.Write(exc.ToString());
                }
            }
            m_Dragging = false;

            if (c != Graph.BoundingBox.Center)
            {
                //CenterBoundingBox(new MsaglPoint(Math.Floor(c.X), Math.Floor(c.Y)));
                CenterBoundingBox(c);
                //Invalidate();
            }

            if (MouseDownPosition.HasValue)
                DoDragging(pos, graphPos);
        }

        void StartDragging(WinPoint p, MsaglPoint gp)
        {
            if (MouseMode == DraggingMode.Pan)
                PanningStartOffset = new WinPoint() { X = MainScrollViewer.HorizontalOffset, Y = MainScrollViewer.VerticalOffset };
            else if (MouseMode == DraggingMode.WindowZoom || (LayoutEditingEnabled && ObjectUnderMouseCursor == null && DrawingLayoutEditor.SelectedEdge == null))
                DragWindowStart = gp;
        }

        void EndDragging(WinPoint p, MsaglPoint gp)
        {
            PanningStartOffset = null;
            if (MouseMode == DraggingMode.WindowZoom && DragWindowStart != null)
                DoWindowZoom(DragWindowStart.Value, gp);
            DragWindowStart = null;
            DrawDragWindow(gp);

            if (RouteEdgesAfterDragging && DrawingLayoutEditor.CurrentUndoAction is ObjectDragUndoRedoAction)
            {
                //RouteSelectedEdges((int)Graph.LayoutAlgorithmSettings.NodeSeparation);
                var settings = new EdgeRoutingSettings() { EdgeRoutingMode = Core.Routing.EdgeRoutingMode.Spline };
                DoEdgeRouting(Graph.GeometryGraph, settings, Graph.LayoutAlgorithmSettings.NodeSeparation);
            }
        }

        void DoDragging(WinPoint p, MsaglPoint gp)
        {
            if (PanningStartOffset.HasValue && MouseMode == DraggingMode.Pan)
                DoPanning(p);
            else if (MouseMode == DraggingMode.WindowZoom || MouseMode == DraggingMode.LayoutEdit)
                DrawDragWindow(gp);
        }

        private Nullable<MsaglPoint> DragWindowStart { get; set; }
        private Path DragWindow { get; set; }
        void DrawDragWindow(MsaglPoint gp)
        {
            if (DragWindowStart == null)
                MainCanvas.Children.Remove(DragWindow);
            else
            {
                if (DragWindow == null)
                {
                    var dc = new DoubleCollection();
                    dc.Add(1.0);
                    dc.Add(1.0);
                    DragWindow = new Path() { Stroke = BlackBrush, StrokeDashArray = dc };
                    Canvas.SetZIndex(DragWindow, 32000);
                }
                PathGeometry pg = new PathGeometry();
                PathFigure pf = new PathFigure() { IsClosed = true };
                pf.StartPoint = new WinPoint(DragWindowStart.Value.X, DragWindowStart.Value.Y);
                pf.Segments.Add(new System.Windows.Media.LineSegment() { Point = new WinPoint(DragWindowStart.Value.X, gp.Y) });
                pf.Segments.Add(new System.Windows.Media.LineSegment() { Point = new WinPoint(gp.X, gp.Y) });
                pf.Segments.Add(new System.Windows.Media.LineSegment() { Point = new WinPoint(gp.X, DragWindowStart.Value.Y) });
                pg.Figures.Add(pf);
                DragWindow.Data = pg;
                if (!MainCanvas.Children.Contains(DragWindow))
                    MainCanvas.Children.Add(DragWindow);
            }
        }

        private void DoWindowZoom(MsaglPoint start, MsaglPoint end)
        {
            double width = Math.Abs(start.X - end.X);
            double height = Math.Abs(start.Y - end.Y);
            double zoom = Math.Min(MainScrollViewer.ViewportWidth / width, MainScrollViewer.ViewportHeight / height);
            if (zoom > 5.0)
                return;
            Zoom = zoom;
            MainScrollViewer.UpdateLayout();
            WinPoint p = MainCanvas.RenderTransform.Transform(new WinPoint(start.X, start.Y));
            MainScrollViewer.ScrollToHorizontalOffset(p.X);
            MainScrollViewer.ScrollToVerticalOffset(p.Y);
        }

        private Nullable<WinPoint> PanningStartOffset { get; set; }
        void DoPanning(WinPoint p)
        {
            double dx = (p.X - MouseDownPosition.Value.X);
            double dy = (p.Y - MouseDownPosition.Value.Y);
            MainScrollViewer.ScrollToHorizontalOffset(PanningStartOffset.Value.X - dx);
            MainScrollViewer.ScrollToVerticalOffset(PanningStartOffset.Value.Y - dy);
        }

        public bool IsNestedGraph { get { return ParentObject is DNestedGraphLabel; } }

        private void OnMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (IsNestedGraph)
                return;
            if (MouseDownPosition.HasValue || MouseMode == DraggingMode.None)
                return;
            if (e.Delta > 0 && Zoom < 100.0)
                Zoom *= 1.1;
            else if (e.Delta < 0 && Zoom > 0.01)
                Zoom /= 1.1;
        }

        #endregion

        internal void PopulateCrossEdges()
        {
            if (!m_CrossEdges.Any())
                return;
            foreach (DEdge e in m_CrossEdges)
                if (MainCanvas.Children.Contains(e))
                    MainCanvas.Children.Remove(e);
            NestedGraphHelper.DrawCrossEdges(this, m_CrossEdges);
            VerifyPolylines();
        }

        private bool m_Populating;
        public void PopulateChildren()
        {
            if (m_Populating)
                return;
            m_Populating = true;
            MainCanvas.Children.Clear();

            // Add everything to the canvas.
            foreach (DObject obj in Entities)
            {
                if (obj == m_RootCluster)
                    continue;
                MainCanvas.Children.Add(obj);
                if (obj is DNode && (obj as DNode).Label != null)
                    MainCanvas.Children.Add((obj as DNode).Label);
            }

            foreach (DObject obj in Entities)
            {
                obj.MakeVisual();
                if (obj is DNode && (obj as DNode).Label != null)
                    (obj as DNode).Label.MakeVisual();
            }

            Dispatcher.BeginInvoke((Action)(() =>
            {
                //PopulateCrossEdges();

                SetRenderTransform();
                m_Populating = false;
            }));
        }

        private void SetRenderTransform()
        {
            //center of the graph
            var g0 = (DrawingGraph.Left + DrawingGraph.Right) / 2.0;
            var g1 = (DrawingGraph.Top + DrawingGraph.Bottom) / 2.0;

            //center of the screen
            var c0 = DrawingGraph.Width * Zoom / 2.0;
            var c1 = DrawingGraph.Height * Zoom / 2.0;
            //we need to map the center of the graph to the center of LayoutRoot
            //we reverse the y-coordinate to -y
            var dx = c0 - Zoom * g0 - ParentBorder.BorderThickness.Left;
            var dy = c1 - Zoom * g1 - ParentBorder.BorderThickness.Top;

            //MainCanvas.RenderTransform = new MatrixTransform { Matrix = new Matrix(Zoom, 0.0, 0.0, Zoom, 0.0, 0.0) };
            MainCanvas.RenderTransform = new MatrixTransform { Matrix = new Matrix(Zoom, 0.0, 0.0, Zoom, dx, dy) };
        }

        public void FitToContents()
        {
            Graph.GeometryGraph.UpdateBoundingBox();
            this.SizeChanged -= DelayFitToContents;
            if (DrawingGraph != null && (DrawingGraph.Height > 0) && (DrawingGraph.Width > 0))
            {
                if (MainScrollViewer.ViewportWidth == 0.0 || MainScrollViewer.ViewportHeight == 0.0)
                    this.SizeChanged += DelayFitToContents;
                else
                    Zoom = Math.Min(MainScrollViewer.ViewportWidth / DrawingGraph.Width, MainScrollViewer.ViewportHeight / DrawingGraph.Height);
            }
        }

        private void DelayFitToContents(object sender, EventArgs args)
        {
            this.SizeChanged -= DelayFitToContents;
            FitToContents();
        }

        private void PopulateDClusterFromDrawing(DCluster dc, Subgraph drawingc)
        {
            foreach (var drawingn in drawingc.Nodes)
                dc.AddNode(NodeMap[drawingn.Id]);
            foreach (var drawingc2 in drawingc.Subgraphs)
            {
                DCluster dc2 = new DCluster(dc, drawingc2);
                dc.AddCluster(dc2);
                NodeMap[drawingc2.Id] = dc2;
                PopulateDClusterFromDrawing(dc2, drawingc2);
            }
        }

        private void RebuildFromDrawingGraph()
        {
            if (Graph.GeometryGraph == null)
            {
                // A geometry graph is required. Note that I have to set the node boundary curves myself (because they depend on the label size, which in turn depends on the rendering engine).
                Graph.CreateGeometryGraph();
                foreach (DrawingNode drawingNode in Graph.NodeMap.Values)
                    SetNodeBoundaryCurve(Graph, drawingNode);
            }

            // Create DNode instances
            NodeMap.Clear();
            foreach (DrawingNode drawingNode in Graph.NodeMap.Values)
                NodeMap[drawingNode.Id] = new DNode(this, drawingNode);

            // Create DCluster instances
            if (Graph.RootSubgraph != null && Graph.RootSubgraph.GeometryNode != null)
            {
                m_RootCluster = new DCluster(this, Graph.RootSubgraph);
                NodeMap[Graph.RootSubgraph.Id] = m_RootCluster;
                PopulateDClusterFromDrawing(m_RootCluster, Graph.RootSubgraph);
            }

            // Create DEdge instances
            Edges.Clear();
            foreach (DrawingEdge drawingEdge in Graph.Edges)
                Edges.Add(new DEdge(NodeMap[drawingEdge.SourceNode.Id], NodeMap[drawingEdge.TargetNode.Id], drawingEdge, ConnectionToGraph.Connected));

            FirePropertyChanged("HasContent");
        }

        public static DGraph FromDrawingGraph(Graph drawingGraph)
        {
            var ret = new DGraph() { DrawingGraph = drawingGraph };
            ret.RebuildFromDrawingGraph();
            ret.Invalidate();
            return ret;
        }

        internal static void SetNodeBoundaryCurve(DrawingGraph drawingGraph, DrawingNode drawingNode)
        {
            SetNodeBoundaryCurve(drawingGraph, drawingNode, null);
        }

        internal static void SetNodeBoundaryCurve(DrawingGraph drawingGraph, DrawingNode drawingNode, DLabel existingLabel)
        {
            // Create the boundary curve. Note that a delegate may be allowed to create the curve (or replace it, if it already exists).
            GeometryNode geomNode = drawingNode.GeometryNode;
            if (geomNode == null)
                return;
            ICurve curve;
            if (drawingNode.NodeBoundaryDelegate == null || (curve = drawingNode.NodeBoundaryDelegate(drawingNode)) == null)
            {
                // I need to know the node size, which may depend on a label.
                double width = 0.0;
                double height = 0.0;
                if (existingLabel != null)
                {
                    //existingLabel.MakeVisual();
                    width = existingLabel.Label.Width;
                    height = existingLabel.Label.Height;
                    width += 2 * drawingNode.Attr.LabelMargin;
                    height += 2 * drawingNode.Attr.LabelMargin;
                }
                else if (drawingNode.Label != null && drawingNode.Label.GeometryLabel != null)
                {
                    // Measure the label's size (the only safe way is to instantiate it).
                    var dLabel = new DTextLabel(null, drawingNode.Label);
                    dLabel.MakeVisual();
                    width = dLabel.Label.Width;
                    height = dLabel.Label.Height;

                    // Add node label margin.
                    width += 2 * drawingNode.Attr.LabelMargin;
                    height += 2 * drawingNode.Attr.LabelMargin;
                }
                // Apply lower cap to node size
                if (width < drawingGraph.Attr.MinNodeWidth)
                    width = drawingGraph.Attr.MinNodeWidth;
                if (height < drawingGraph.Attr.MinNodeHeight)
                    height = drawingGraph.Attr.MinNodeHeight;

                curve = NodeBoundaryCurves.GetNodeBoundaryCurve(drawingNode, width, height);
            }
            if (geomNode.BoundaryCurve != null)
                curve.Translate(geomNode.BoundingBox.Center);
            geomNode.BoundaryCurve = curve;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void FirePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public void Clear()
        {
            if (Working)
                throw new InvalidOperationException("Layout in progress - abort layout first");
            DrawingGraph dg = new DrawingGraph();
            dg.GeometryGraph = new GeometryGraph();
            dg.GeometryGraph.Margins = 5.0;
            dg.Attr.NodeSeparation = 10;
            (dg.LayoutAlgorithmSettings as SugiyamaLayoutSettings).Transformation = new PlaneTransformation(-1.0, 0.0, 0.0, 0.0, -1.0, 0.0);
            Edges = new List<DEdge>();
            NodeMap = new Dictionary<IComparable, DNode>();
            Graph = dg;
            Invalidate();

            Zoom = 1.0;
            MainScrollViewer.ScrollToHorizontalOffset(0.0);
            MainScrollViewer.ScrollToVerticalOffset(0.0);

            FirePropertyChanged("HasContent");
        }

        public bool HasContent
        {
            get
            {
                return Nodes().Any();
            }
        }

        public event EventHandler GraphLayoutStarting;
        public event EventHandler GraphLayoutDone;
        private bool m_Working = false;
        public bool Working
        {
            get
            {
                return m_Working;
            }
            set
            {
                m_Working = value;
                WorkingText.Visibility = Working ? Visibility.Visible : Visibility.Collapsed;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Working"));
            }
        }

        public void AbortLayout()
        {
            if (Working)
            {
                if (CancelToken != null)
                    CancelToken.Canceled = true;
                Working = false;
            }
        }

        private Msagl.Core.CancelToken CancelToken;

        private static void TransformUnderlyingPolyline(GeometryGraph geometryGraph, GeometryEdge e, PlaneTransformation transformation)
        {
            if (e.UnderlyingPolyline != null)
            {
                for (Microsoft.Msagl.Core.Geometry.Site s = e.UnderlyingPolyline.HeadSite; s != null; s = s.Next)
                {
                    s.Point = transformation * s.Point;
                }
            }
        }

        public void RouteEdgeSetSpline(IEnumerable<DEdge> edges)
        {
            if (!edges.Any())
                return;

            GeometryGraph ggAux = new GeometryGraph();
            foreach (DEdge edge in edges)
            {
                if (!ggAux.Nodes.Contains(edge.Source.GeometryNode))
                    ggAux.Nodes.Add(edge.Source.GeometryNode);
                if (!ggAux.Nodes.Contains(edge.Target.GeometryNode))
                    ggAux.Nodes.Add(edge.Target.GeometryNode);
                if (!ggAux.Edges.Contains(edge.GeometryEdge))
                    ggAux.Edges.Add(edge.GeometryEdge);
            }

            var router = new SplineRouter(ggAux, 3.0, 2.0, Math.PI / 6.0);
            router.Run();
            VerifyPolylines();
        }

        public void RouteSelectedEdges(int separation)
        {
            double nodeSeparation = Graph.LayoutAlgorithmSettings.NodeSeparation;
            double nodePadding = nodeSeparation / 3;
            double loosePadding = SplineRouter.ComputeLooseSplinePadding(nodeSeparation, nodePadding);
            BundlingSettings bundlingSettings = new BundlingSettings() { EdgeSeparation = separation, CreateUnderlyingPolyline = true };
            EdgeRoutingSettings settings = new EdgeRoutingSettings() { BundlingSettings = bundlingSettings, EdgeRoutingMode = EdgeRoutingMode };
            if (settings.EdgeRoutingMode == EdgeRoutingMode.SugiyamaSplines)
                settings.EdgeRoutingMode = EdgeRoutingMode.Spline;

            if (Nodes().Any(n => n.MarkedForDragging))
            {
                Dictionary<DNode, GeometryNode> nodesThisToAux = new Dictionary<DNode, GeometryNode>();
                Dictionary<DEdge, GeometryEdge> edgesThisToAux = new Dictionary<DEdge, GeometryEdge>();
                GeometryGraph ggAux = new GeometryGraph();
                foreach (DNode node in Nodes())
                {
                    var n = new GeometryNode()
                    {
                        BoundaryCurve = node.GeometryNode.BoundaryCurve,
                        GeometryParent = ggAux,
                        Padding = node.GeometryNode.Padding,
                    };
                    ggAux.Nodes.Add(n);
                    nodesThisToAux[node] = n;
                }

                foreach (DNode node in Nodes().Where(n => n.MarkedForDragging))
                {
                    foreach (DEdge edge in node.Edges)
                    {
                        if (!edgesThisToAux.ContainsKey(edge))
                        {
                            var e = new GeometryEdge(nodesThisToAux[edge.Source], nodesThisToAux[edge.Target]);
                            if (edge.Source == edge.Target)
                                nodesThisToAux[edge.Source].AddSelfEdge(e);
                            else
                            {
                                nodesThisToAux[edge.Source].AddOutEdge(e);
                                nodesThisToAux[edge.Target].AddInEdge(e);
                            }
                            ggAux.Edges.Add(e);
                            edgesThisToAux[edge] = e;
                        }
                    }

                    /*ggAux.Nodes.Add(node.GeometryNode);
                    if (node.MarkedForDragging)
                    {
                        foreach (DEdge edge in node.Edges)
                        {
                            if (!ggAux.Nodes.Contains(edge.Source.GeometryNode))
                                ggAux.Nodes.Add(edge.Source.GeometryNode);
                            if (!ggAux.Nodes.Contains(edge.Target.GeometryNode))
                                ggAux.Nodes.Add(edge.Target.GeometryNode);
                            if (!ggAux.Edges.Contains(edge.GeometryEdge))
                                ggAux.Edges.Add(edge.GeometryEdge);
                        }
                    }*/
                }

                DoEdgeRouting(ggAux, settings, nodeSeparation);

                //BundledEdgeRouter.RouteEdgesInMetroMapStyle(ggAux, Math.PI / 6.0, nodePadding, loosePadding, settings);
                /*var router = new SplineRouter(ggAux, settings);
                router.Run(CancelToken);
                VerifyPolylines();*/
                foreach (var kv in edgesThisToAux)
                    kv.Key.GeometryEdge.EdgeGeometry = kv.Value.EdgeGeometry;
            }
            else
            {
                DoEdgeRouting(Graph.GeometryGraph, settings, nodeSeparation);
                //BundledEdgeRouter.RouteEdgesInMetroMapStyle(Graph.GeometryGraph, Math.PI / 6.0, nodePadding, loosePadding, settings);
                /*var router = new SplineRouter(Graph.GeometryGraph, settings);
                router.Run(CancelToken);
                VerifyPolylines();*/
            }

            //Invalidate();
        }

        private static MsaglPolyline PolylineFromCurve(Curve curve)
        {
            var ret = new MsaglPolyline();
            ret.AddPoint(curve.Start);
            foreach (var ls in curve.Segments)
                ret.AddPoint(ls.End);
            ret.Closed = curve.Start == curve.End;
            return ret;
        }

        private void VerifyPolylines()
        {
            // Is this still needed?
            foreach (DEdge e in Edges.Concat(m_CrossEdges))
            {
                var edge = e.GeometryEdge;
                if (edge.UnderlyingPolyline != null)
                    continue;
                if (edge.Curve == null)
                    continue;
                Curve c = edge.Curve as Curve;
                if (c == null)
                {
                    c = new Curve();
                    c.Segments.Add(edge.Curve);
                }
                edge.UnderlyingPolyline = Microsoft.Msagl.Core.Geometry.SmoothedPolyline.FromPoints(new[] { edge.Source.Center }.Concat(PolylineFromCurve(c)).Concat(new[] { edge.Target.Center }));
            }
        }

        private void DoEdgeRouting(GeometryGraph gg, EdgeRoutingSettings settings, double nodeSeparation)
        {
            var mode = settings.EdgeRoutingMode;
            if (mode == EdgeRoutingMode.SugiyamaSplines)
            {
            }
            else
            {
                if (mode == EdgeRoutingMode.Rectilinear || mode == EdgeRoutingMode.RectilinearToCenter)
                {
                    RectilinearInteractiveEditor.CreatePortsAndRouteEdges(
                        nodeSeparation / 3,
                        nodeSeparation / 3,
                        gg.Nodes,
                        gg.Edges,
                        mode,
                        true);
                }
                else if (mode == EdgeRoutingMode.Spline || mode == EdgeRoutingMode.SplineBundling)
                {
                    /*var coneAngle = Math.PI / 6.0;
                    var nodePadding = nodeSeparation / 3;
                    var loosePadding = SplineRouter.ComputeLooseSplinePadding(nodeSeparation, nodePadding);*/
                    //if (mode == EdgeRoutingMode.Spline)
                    if (mode == EdgeRoutingMode.Spline)
                        settings.BundlingSettings = null;
                    new SplineRouter(gg, settings).Run(CancelToken);
                    //else
                    //BundledEdgeRouter.RouteEdgesInMetroMapStyle(gg, coneAngle, nodePadding, loosePadding, settings.BundlingSettings);
                }
                else if (mode == EdgeRoutingMode.StraightLine)
                {
                    new StraightLineEdges(gg.Edges, settings.Padding).Run(CancelToken);
                }

                // Place labels
                new EdgeLabelPlacement(gg).Run(CancelToken);

                if (CancelToken == null || !CancelToken.Canceled)
                {
                    //                    foreach (GeometryEdge e in gg.Edges)
                    //                      TransformUnderlyingPolyline(gg, e, (dg.LayoutAlgorithmSettings as SugiyamaLayoutSettings).Transformation);
                    gg.UpdateBoundingBox();
                    Dispatcher.BeginInvoke(Invalidate);
                }
            }
        }

        public FastIncrementalLayoutSettings ConfigureIncrementalLayout()
        {
            Graph.LayoutAlgorithmSettings = new FastIncrementalLayoutSettings();
            Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.KeepOriginalSpline = true;
            var settings = Graph.LayoutAlgorithmSettings as FastIncrementalLayoutSettings;
            settings.AvoidOverlaps = true;
            settings.NodeSeparation = 30;
            settings.RouteEdges = true;
            return settings;
        }

        public SugiyamaLayoutSettings ConfigureSugiyamaLayout()
        {
            return ConfigureSugiyamaLayout(Math.PI);
        }

        public SugiyamaLayoutSettings ConfigureSugiyamaLayout(double rotation)
        {
            Graph.LayoutAlgorithmSettings = new SugiyamaLayoutSettings();
            Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.KeepOriginalSpline = true;
            var settings = Graph.LayoutAlgorithmSettings as SugiyamaLayoutSettings;
            settings.Transformation = PlaneTransformation.Rotation(rotation);
            return settings;
        }

        private void DoLayoutSugiyama(DrawingGraph dg)
        {
            var gg = dg.GeometryGraph;
            var layoutAlgorithm = new LayeredLayout(gg, dg.LayoutAlgorithmSettings as SugiyamaLayoutSettings);
            layoutAlgorithm.Run(CancelToken = new Core.CancelToken());

            var settings = (dg.LayoutAlgorithmSettings as SugiyamaLayoutSettings);
            if (!CancelToken.Canceled)
                DoEdgeRouting(dg.GeometryGraph, settings.EdgeRoutingSettings, settings.NodeSeparation);
        }

        public IEnumerable<DGraph> NestedGraphs
        {
            get
            {
                return Nodes().Cast<DNode>().Where(n => n.Label is DNestedGraphLabel).SelectMany(n => (n.Label as DNestedGraphLabel).Graphs);
            }
        }

        private void MeasureAllLabels()
        {
            foreach (IHavingDLabel dobj in Entities.Where(e => e is IHavingDLabel))
                if (dobj.Label != null)
                    dobj.Label.MeasureLabel();
        }

        private void DoGeometryLayout()
        {
            if (DrawingGraph.LayoutAlgorithmSettings is SugiyamaLayoutSettings)
            {
                try
                {
                    DoLayoutSugiyama(DrawingGraph);
                    VerifyPolylines();
                }
                catch (Microsoft.Msagl.Core.OperationCanceledException)
                {
                }
                catch (Exception exc)
                {
                    Dispatcher.BeginInvoke(() => MessageBox.Show(exc.ToString()));
                }
            }
            else
            {
                Microsoft.Msagl.Miscellaneous.LayoutHelpers.CalculateLayout(Graph.GeometryGraph, DrawingGraph.LayoutAlgorithmSettings, CancelToken);
                Graph.GeometryGraph.UpdateBoundingBox();
                DoEdgeRouting(DrawingGraph.GeometryGraph, new EdgeRoutingSettings() { EdgeRoutingMode = Core.Routing.EdgeRoutingMode.SplineBundling, KeepOriginalSpline = true }, DrawingGraph.LayoutAlgorithmSettings.NodeSeparation);
            }
        }

        public void BeginLayout()
        {
            BeginLayout(false);
        }

        internal void BeginLayout(bool ignoreNesting)
        {
            if (Working)
                throw new InvalidOperationException("Layout already in progress - abort layout first");
            if (!ignoreNesting && NestedGraphs.Any())
            {
                NestedGraphHelper.BeginLayout(this);
                return;
            }
            DrawingLayoutEditor.Clear();
            Working = true;
            if (GraphLayoutStarting != null)
                GraphLayoutStarting(this, EventArgs.Empty);
            if (Entities.Any())
            {
                // Remove all elements from the visual tree.
                MainCanvas.Children.Clear();

                // BeginInvoke to wait for the visual tree layout.
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    // Measure label content, copy content size to Drawing labels.
                    MeasureAllLabels();

                    // Create node curves according to label size and node shape.
                    foreach (DNode n in Nodes())
                        SetNodeBoundaryCurve(Graph, n.DrawingNode, n.Label);

                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        DoGeometryLayout();

                        if (CancelToken == null || !CancelToken.Canceled)
                        {
                            // Switch back to the GUI thread.
                            Dispatcher.BeginInvoke((Action)(() =>
                            {
                                if (GraphChanged != null)
                                    GraphChanged(DrawingGraph, EventArgs.Empty);

                                UpdateWidthAndHeight();

                                // Fill the visual tree.
                                PopulateChildren();

                                // BeginInvoke to wait for the visual tree layout.
                                Dispatcher.BeginInvoke((Action)(() =>
                                {
                                    if (GraphLayoutDone != null)
                                        GraphLayoutDone(this, EventArgs.Empty);
                                    Working = false;
                                }));
                            }));
                        }
                    }, null);
                }));
            }
            else
            {
                if (GraphLayoutDone != null)
                    GraphLayoutDone(this, EventArgs.Empty);
                Working = false;
            }
        }

        public bool CanUndo { get { return DrawingLayoutEditor.CanUndo; } }
        public bool CanRedo { get { return DrawingLayoutEditor.CanRedo; } }

        public void Undo()
        {
            if (DrawingLayoutEditor.CanUndo)
                DrawingLayoutEditor.Undo();
        }

        public void Redo()
        {
            if (DrawingLayoutEditor.CanRedo)
                DrawingLayoutEditor.Redo();
        }

        public event EventHandler GraphSaving;
        public event EventHandler GraphSaved;
        public event EventHandler GraphLoading;
        public event EventHandler GraphLoaded;

        public void Save(Stream stream)
        {
            if (GraphSaving != null)
                GraphSaving(this, EventArgs.Empty);
            foreach (DrawingNode n in Graph.Nodes)
                n.GeometryObject.UserData = n.Id;
            Graph.WriteToStream(stream);
            stream.Flush();
            if (GraphSaved != null)
                GraphSaved(this, EventArgs.Empty);
        }

        public void Load(Stream stream)
        {
            if (GraphLoading != null)
                GraphLoading(this, EventArgs.Empty);
            DrawingGraph dg = Msagl.Drawing.Graph.ReadGraphFromStream(stream);
            Graph = dg;
            if (GraphLoaded != null)
                GraphLoaded(this, EventArgs.Empty);
            FitGraphBoundingBox();
            FitToContents();
        }

        #region IViewer Members

        private LayoutEditor DrawingLayoutEditor { get; set; }

        public void AddEdge(IViewerEdge edge, bool registerForUndo)
        {
            if (registerForUndo)
                DrawingLayoutEditor.RegisterEdgeAdditionForUndo(edge);

            DEdge dEdge = edge as DEdge;
            DrawingEdge drawingEdge = dEdge.DrawingEdge;
            GeometryEdge geomEdge = drawingEdge.GeometryEdge;

            // Set an edge label if not already present
            if (geomEdge.Label == null)
                dEdge.Label = new DTextLabel(dEdge, new Drawing.Label(""));

            // Add the edge to my edge list, to the drawing graph, and to the geometry graph.
            Edges.Add(dEdge);
            Graph.AddPrecalculatedEdge(drawingEdge);
            Graph.GeometryGraph.Edges.Add(geomEdge);

            // Add the edge to each node. Note that you don't need to do this for the geometry objects. The calls to the drawing objects already do it.
            DNode source = edge.Source as DNode;
            DNode target = edge.Target as DNode;
            if (source != target)
            {
                source.AddOutEdge(dEdge);
                target.AddInEdge(dEdge);

                source.DrawingNode.AddOutEdge(drawingEdge);
                target.DrawingNode.AddInEdge(drawingEdge);
            }
            else
            {
                source.AddSelfEdge(dEdge);
                source.DrawingNode.AddSelfEdge(drawingEdge);
            }

            if (registerForUndo && EdgeInsertedByUser != null)
                EdgeInsertedByUser(dEdge, EventArgs.Empty);

            if (!Edges.Contains(dEdge))
                return;

            DrawingLayoutEditor.AttachLayoutChangeEvent(dEdge);
            if (dEdge.Label != null)
                DrawingLayoutEditor.AttachLayoutChangeEvent(dEdge);

            // Display the edge.
            if (drawingEdge.GeometryEdge.Curve != null)
            {
                dEdge.MakeVisual();
                MainCanvas.Children.Add(dEdge);

                if (dEdge.Label != null)
                {
                    dEdge.Label.MakeVisual();
                    MainCanvas.Children.Add(dEdge.Label);
                }

                UpdateGraphBoundingBoxPreservingCenter();
            }

            if (registerForUndo)
                DrawingLayoutEditor.CurrentUndoAction.GraphBoundingBoxAfter = Graph.BoundingBox;

            if (registerForUndo)
                BeginContentEdit(dEdge);
        }

        public void AddNode(IViewerNode node, bool registerForUndo)
        {
            DNode dNode = node as DNode;
            DrawingNode drawingNode = dNode.DrawingNode;

            if (registerForUndo && NodeInsertingByUser != null)
                NodeInsertingByUser(dNode, EventArgs.Empty);

            // Add the node to my node map, to the drawing graph, and to the geometry graph.
            NodeMap[drawingNode.Id] = dNode;
            Graph.AddNode(drawingNode);
            Graph.GeometryGraph.Nodes.Add(drawingNode.GeometryNode);

            // The node may come with edges that also need to be added to the internal edge list, and to the other nodes.
            foreach (DEdge e in dNode.OutEdges)
            {
                e.Target._InEdges.Add(e);
                e.Target.DrawingNode.AddInEdge(e.DrawingEdge);
                e.Target.DrawingNode.GeometryNode.AddInEdge(e.DrawingEdge.GeometryEdge);
            }
            foreach (DEdge e in dNode.InEdges)
            {
                e.Source._OutEdges.Add(e);
                e.Source.DrawingNode.AddOutEdge(e.DrawingEdge);
                e.Source.DrawingNode.GeometryNode.AddOutEdge(e.DrawingEdge.GeometryEdge);
            }

            // The edges also need to be added to the drawing graph, and to the geometry graph.
            foreach (DEdge e in dNode.Edges)
            {
                Edges.Add(e);
                Graph.AddPrecalculatedEdge(e.DrawingEdge);
                Graph.GeometryGraph.Edges.Add(e.DrawingEdge.GeometryEdge);
            }

            if (registerForUndo && NodeInsertedByUser != null)
                NodeInsertedByUser(dNode, EventArgs.Empty);

            if (!NodeMap.ContainsValue(dNode))
                return;

            DrawingLayoutEditor.AttachLayoutChangeEvent(dNode);

            // Display the node and edges.
            dNode.MakeVisual();
            if (!MainCanvas.Children.Contains(dNode))
                MainCanvas.Children.Add(dNode);
            foreach (DEdge e in dNode.Edges)
            {
                e.MakeVisual();
                if (!MainCanvas.Children.Contains(e))
                    MainCanvas.Children.Add(e);
            }

            //BuildBBHierarchy();

            FirePropertyChanged("HasContent");
        }

        private double m_ArrowheadLength = 10.0;
        public double ArrowheadLength
        {
            get
            {
                // Cap arrowhead length to half the layer separation value.
                if (Graph != null && Graph.LayoutAlgorithmSettings is SugiyamaLayoutSettings)
                    return Math.Min(m_ArrowheadLength, Graph.Attr.LayerSeparation / 2);
                return m_ArrowheadLength;
            }
            set
            {
                m_ArrowheadLength = value;
            }
        }

        public IViewerEdge CreateEdgeWithGivenGeometry(DrawingEdge drawingEdge)
        {
            GeometryEdge geometryEdge = drawingEdge.GeometryEdge;
            geometryEdge.GeometryParent = Graph.GeometryGraph;

            var dEdge = new DEdge(NodeMap[drawingEdge.SourceNode.Id], NodeMap[drawingEdge.TargetNode.Id], drawingEdge, ConnectionToGraph.Disconnected);
            if (drawingEdge.Label != null)
                dEdge.Label = new DTextLabel(dEdge, new Drawing.Label());

            dEdge.MakeVisual();
            return dEdge;
        }

        public IViewerNode CreateNode(DrawingNode node)
        {
            DNode ret = new DNode(this, node);
            node.LabelText = null;
            SetNodeBoundaryCurve(Graph, node);
            return ret;
        }

        public double DistanceForSnappingThePortToNodeBoundary
        {
            get { return UnderlyingPolylineCircleRadius * 2; }
        }

        public double DpiX
        {
            get { return 1; }
        }

        public double DpiY
        {
            get { return 1; }
        }

        private Path RubberEdgePath;
        public void DrawRubberEdge(EdgeGeometry edgeGeometry)
        {
            if (RubberEdgePath != null)
                MainCanvas.Children.Remove(RubberEdgePath);
            RubberEdgePath = new Path() { Stroke = BlackBrush, StrokeThickness = 2.0 };
            PathFigure figure = Draw.CreateGraphicsPath(edgeGeometry.Curve);
            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            RubberEdgePath.Data = geometry;
            RubberEdgePath.SetValue(Canvas.LeftProperty, edgeGeometry.BoundingBox.Left);
            RubberEdgePath.SetValue(Canvas.TopProperty, edgeGeometry.BoundingBox.Bottom);
            geometry.Transform = new MatrixTransform() { Matrix = new Matrix(1.0, 0.0, 0.0, 1.0, -edgeGeometry.BoundingBox.Left, -edgeGeometry.BoundingBox.Bottom) };
            MainCanvas.Children.Add(RubberEdgePath);
        }

        public void DrawRubberLine(MsaglPoint point)
        {
            if (RubberEdgePath != null)
                MainCanvas.Children.Remove(RubberEdgePath);
            var startPoint = (RubberEdgePath.Data as PathGeometry).Figures[0].StartPoint;
            RubberEdgePath = new Path() { Stroke = BlackBrush, StrokeThickness = 2.0 };
            PathFigure figure = new PathFigure() { StartPoint = startPoint };
            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            figure.Segments.Add(new System.Windows.Media.LineSegment() { Point = new WinPoint(point.X, point.Y) });
            RubberEdgePath.Data = geometry;
            MainCanvas.Children.Add(RubberEdgePath);
        }

        public void DrawRubberLine(MsaglMouseEventArgs args)
        {
            DrawRubberLine(new MsaglPoint(args.X, args.Y));
        }

        public EdgeRoutingMode EdgeRoutingMode { get; set; }

        public event EventHandler GraphChanged;

        public bool InsertingEdge
        {
            get
            {
                return MouseMode == DraggingMode.ComboInsertion || MouseMode == DraggingMode.EdgeInsertion;
            }
            set
            {
                if (InsertingEdge != value)
                    throw new NotSupportedException(); // this should already have been set
                /*if (value)
                    MouseMode = DraggingMode.ComboInsertion;
                else
                    MouseMode = DefaultMouseMode;*/
            }
        }

        bool m_Dragging = false;
        public void Invalidate()
        {
            if (m_Dragging)
                return;
            PopulateChildren();
        }

        public void Invalidate(IViewerObject editObj)
        {
            if (editObj is DObject)
            {
                (editObj as DObject).MakeVisual();
                if (editObj is IHavingDLabel && (editObj as IHavingDLabel).Label != null)
                    (editObj as IHavingDLabel).Label.MakeVisual();
            }
            //PopulateChildren();
            //SetRenderTransform();
        }

        public void InvalidateBeforeTheChange(IViewerObject editObj)
        {
            Invalidate(editObj);
        }

        public bool LayoutEditingEnabled
        {
            get
            {
                return MouseMode == DraggingMode.LayoutEdit || MouseMode == DraggingMode.ComboInsertion || MouseMode == DraggingMode.EdgeInsertion;
            }
        }

        public double LineThicknessForEditing
        {
            get
            {
                return 2.0;
            }
        }

        public ModifierKeys ModifierKeys
        {
            get
            {
                if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                    return Drawing.ModifierKeys.Shift;
                else if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
                    return Drawing.ModifierKeys.Alt;
                else if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                    return Drawing.ModifierKeys.Control;
                return Drawing.ModifierKeys.None;
            }
        }

        public event EventHandler<MsaglMouseEventArgs> MouseDown;

        public new event EventHandler<MsaglMouseEventArgs> MouseMove;

        public event EventHandler<MsaglMouseEventArgs> MouseUp;

        private IViewerObject m_ObjectUnderMouseCursor;
        public IViewerObject ObjectUnderMouseCursor
        {
            get
            {
                return m_ObjectUnderMouseCursor;
            }
            internal set
            {
                if (m_ObjectUnderMouseCursor != value)
                {
                    var old = m_ObjectUnderMouseCursor;
                    m_ObjectUnderMouseCursor = value;
                    FirePropertyChanged("ObjectUnderMouseCursor");
                    if (ObjectUnderMouseCursorChanged != null)
                        ObjectUnderMouseCursorChanged(this, new ObjectUnderMouseCursorChangedEventArgs(old, value));
                }
            }
        }

        public event EventHandler<ObjectUnderMouseCursorChangedEventArgs> ObjectUnderMouseCursorChanged;

        public void OnDragEnd(IEnumerable<IViewerObject> changedObjects)
        {
        }

        private double m_PaddingForEdgeRouting;
        public double PaddingForEdgeRouting
        {
            get
            {
                return Graph == null ? m_PaddingForEdgeRouting : Math.Min(m_PaddingForEdgeRouting, Graph.Attr.NodeSeparation / 6);
            }
            set
            {
                m_PaddingForEdgeRouting = value;
            }
        }

        public event EventHandler<GeneratingPopupEventArgs> GeneratingPopup;

        public void ClosePopup()
        {
            m_CurrentPopup.IsOpen = false;
        }

        private void ShowStandardPopup(MsaglPoint mousePos)
        {
            m_CurrentPopup = new Popup();
            m_CurrentPopup.Closed += (sender, e) => m_CurrentPopup = null;
            Grid popupGrid = new Grid();
            Canvas popupCanvas = new Canvas();
            m_CurrentPopup.Child = popupGrid;
            popupCanvas.MouseLeftButtonDown += (sender, e) => { m_CurrentPopup.IsOpen = false; };
            popupCanvas.MouseRightButtonDown += (sender, e) => { e.Handled = true; m_CurrentPopup.IsOpen = false; };
            popupCanvas.Background = TransparentBrush;
            popupGrid.Children.Add(popupCanvas);
            ListBox lstContextMenu = new ListBox();

            TextBlock txb = new TextBlock() { Text = "Edit", Tag = ObjectUnderMouseCursor };
            if (ObjectUnderMouseCursor == null)
                txb.Foreground = new SolidColorBrush(Colors.Gray);
            txb.MouseLeftButtonUp += (sender, e) =>
                {
                    DObject dobj = (sender as TextBlock).Tag as DObject;
                    if (dobj != null)
                        BeginContentEdit(dobj);
                    ClosePopup();
                };
            lstContextMenu.Items.Add(txb);

            txb = new TextBlock() { Text = "Remove", Tag = ObjectUnderMouseCursor };
            if (ObjectUnderMouseCursor == null)
                txb.Foreground = new SolidColorBrush(Colors.Gray);
            txb.MouseLeftButtonUp += (sender, e) =>
            {
                DObject dobj = (sender as TextBlock).Tag as DObject;
                if (dobj is DNode)
                    RemoveNode(dobj as DNode, true);
                else if (dobj is DEdge)
                    RemoveEdge(dobj as DEdge, true);
                else if (dobj is DLabel && (dobj as DLabel).Parent is DEdge)
                    RemoveEdge((dobj as DLabel).Parent as DEdge, true);
                if (UpdateGraphBoundingBoxPreservingCenter())
                    Invalidate();
                ClosePopup();
            };
            lstContextMenu.Items.Add(txb);

            txb = new TextBlock() { Text = "Remove Selected" };
            if (!Entities.Any(obj => !obj.MarkedForDragging))
                txb.Foreground = new SolidColorBrush(Colors.Gray);
            txb.MouseLeftButtonUp += (sender, e) =>
            {
                List<DEdge> edgesToRemove = new List<DEdge>();
                foreach (DEdge edge in Edges.Where(ed => ed.MarkedForDragging))
                    edgesToRemove.Add(edge);
                foreach (DEdge edge in edgesToRemove)
                    RemoveEdge(edge, true);
                List<DNode> nodesToRemove = new List<DNode>();
                foreach (DNode node in Nodes().Where(ed => ed.MarkedForDragging))
                    nodesToRemove.Add(node);
                foreach (DNode node in nodesToRemove)
                    RemoveNode(node, true);

                if (UpdateGraphBoundingBoxPreservingCenter())
                    Invalidate();
                ClosePopup();
            };
            lstContextMenu.Items.Add(txb);

            if (GeneratingPopup != null)
                GeneratingPopup(this, new GeneratingPopupEventArgs(lstContextMenu, mousePos));
            if (lstContextMenu.Items.Count == 0)
            {
                m_CurrentPopup = null;
                return;
            }

            Grid rootGrid = new Grid()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(MouseRightPosition.X, MouseRightPosition.Y, 0, 0)
            };
            rootGrid.Children.Add(lstContextMenu);
            popupGrid.Children.Add(rootGrid);

            popupGrid.Width = Application.Current.Host.Content.ActualWidth;
            popupGrid.Height = Application.Current.Host.Content.ActualHeight;
            popupCanvas.Width = popupGrid.Width;
            popupCanvas.Height = popupGrid.Height;

            m_CurrentPopup.IsOpen = true;
        }

        private Popup m_CurrentPopup = null;
        public void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems)
        {
            m_CurrentPopup = new Popup();
            m_CurrentPopup.Closed += (sender, e) => m_CurrentPopup = null;
            Grid popupGrid = new Grid();
            Canvas popupCanvas = new Canvas();
            m_CurrentPopup.Child = popupGrid;
            popupCanvas.MouseLeftButtonDown += (sender, e) => { m_CurrentPopup.IsOpen = false; };
            popupCanvas.MouseRightButtonDown += (sender, e) => { e.Handled = true; m_CurrentPopup.IsOpen = false; };
            popupCanvas.Background = TransparentBrush;
            popupGrid.Children.Add(popupCanvas);

            ListBox lstContextMenu = new ListBox();
            foreach (var c in menuItems)
            {
                if (c.Item1 == "Remove edge")
                    continue;
                TextBlock txb = new TextBlock() { Text = c.Item1, Tag = c.Item2 };
                txb.MouseLeftButtonUp += (sender, e) => { (((sender as TextBlock).Tag) as VoidDelegate)(); m_CurrentPopup.IsOpen = false; };
                lstContextMenu.Items.Add(txb);
            }

            Grid rootGrid = new Grid()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(MouseRightPosition.X, MouseRightPosition.Y, 0, 0)
            };
            rootGrid.Children.Add(lstContextMenu);
            popupGrid.Children.Add(rootGrid);

            popupGrid.Width = Application.Current.Host.Content.ActualWidth;
            popupGrid.Height = Application.Current.Host.Content.ActualHeight;
            popupCanvas.Width = popupGrid.Width;
            popupCanvas.Height = popupGrid.Height;

            m_CurrentPopup.IsOpen = true;
        }

        public void RemoveEdge(IViewerEdge edge, bool registerForUndo)
        {
            var de = edge as DEdge;

            if (registerForUndo)
                DrawingLayoutEditor.RegisterEdgeRemovalForUndo(edge);

            Graph.RemoveEdge(de.DrawingEdge);

            Edges.Remove(de);
            if (de.Source != de.Target)
            {
                de.Source.RemoveOutEdge(de);
                de.Target.RemoveInEdge(de);
            }
            else
                de.Source.RemoveSelfEdge(de);

            MainCanvas.Children.Remove(de);
            if (de.Label != null)
                MainCanvas.Children.Remove(de.Label);

            if (registerForUndo && EdgeDeletedByUser != null)
                EdgeDeletedByUser(de, EventArgs.Empty);
        }

        public void RemoveNode(IViewerNode node, bool registerForUndo)
        {
            var dNode = node as DNode;
            var drawingNode = dNode.DrawingNode;

            if (registerForUndo)
                DrawingLayoutEditor.RegisterNodeForRemoval(node);

            NodeMap.Remove(drawingNode.Id);
            Graph.NodeMap.Remove(drawingNode.Id);
            Graph.GeometryGraph.Nodes.Remove(drawingNode.GeometryNode);

            foreach (DEdge de in dNode.Edges)
            {
                MainCanvas.Children.Remove(de);
                if (de.Label != null)
                    MainCanvas.Children.Remove(de.Label);
                Edges.Remove(de);
                Graph.RemoveEdge(de.DrawingEdge);
                Graph.GeometryGraph.Edges.Remove(de.DrawingEdge.GeometryEdge);
            }

            foreach (DEdge de in node.OutEdges)
            {
                de.Target._InEdges.Remove(de);
                de.Target.DrawingNode.RemoveInEdge(de.DrawingEdge);
                de.Target.DrawingNode.GeometryNode.RemoveInEdge(de.DrawingEdge.GeometryEdge);
            }

            foreach (DEdge de in node.InEdges)
            {
                de.Source._OutEdges.Remove(de);
                de.Source.DrawingNode.RemoveOutEdge(de.DrawingEdge);
                de.Source.DrawingNode.GeometryNode.RemoveOutEdge(de.DrawingEdge.GeometryEdge);
            }

            MainCanvas.Children.Remove(dNode);
            if (dNode.Label != null)
                MainCanvas.Children.Remove(dNode.Label);

            if (registerForUndo && NodeDeletedByUser != null)
                NodeDeletedByUser(dNode, EventArgs.Empty);

            FirePropertyChanged("HasContent");
        }

        double tightOffsetForRouting = 1.0 / 8;
        double looseOffsetForRouting = 1.0 / 8 * 2;
        double offsetForRelaxingInRouting = 0.6;
        private void RouteEdge(GeometryEdge geometryEdge)
        {
            var router = new RouterBetweenTwoNodes(Graph.GeometryGraph, Graph.Attr.NodeSeparation * tightOffsetForRouting,
                                                   Graph.Attr.NodeSeparation * looseOffsetForRouting,
                                                   Graph.Attr.NodeSeparation * offsetForRelaxingInRouting);
            try
            {
                router.RouteEdge(geometryEdge, true);
            }
            catch (Exception)
            {
            }
        }

        public IViewerEdge RouteEdge(DrawingEdge edgeToRoute)
        {
            edgeToRoute.Label = new Drawing.Label();
            GeometryEdge geometryEdge = edgeToRoute.GeometryEdge = new GeometryEdge();
            geometryEdge.GeometryParent = Graph.GeometryGraph;
            geometryEdge.Source = edgeToRoute.SourceNode.GeometryNode;
            geometryEdge.Target = edgeToRoute.TargetNode.GeometryNode;
            //geometryEdge.ArrowheadLength = edgeToRoute.Attr.ArrowheadLength;
            geometryEdge.SourcePort = edgeToRoute.SourcePort;
            geometryEdge.TargetPort = edgeToRoute.TargetPort;
            if (edgeToRoute.Source == edgeToRoute.Target)
            {
                RectilinearInteractiveEditor.CreateSimpleEdgeCurve(geometryEdge);
            }
            else
            {
                RouteEdge(geometryEdge);
                Arrowheads.TrimSplineAndCalculateArrowheads(geometryEdge, geometryEdge.Curve, false, true);
                //Arrowheads.FixArrowheadAtSource(geometryEdge.EdgeGeometry, edgeToRoute.SourcePort);
                //Arrowheads.FixArrowheadAtTarget(geometryEdge.EdgeGeometry, edgeToRoute.TargetPort);
            }
            var dEdge = new DEdge(NodeMap[edgeToRoute.SourceNode.Id], NodeMap[edgeToRoute.TargetNode.Id],
                                  edgeToRoute, ConnectionToGraph.Disconnected);
            dEdge.Label = new DTextLabel(dEdge, new Drawing.Label());
            return dEdge;
        }

        public MsaglPoint ScreenToSource(MsaglPoint screenPoint)
        {
            return screenPoint;
        }

        public MsaglPoint ScreenToSource(MsaglMouseEventArgs e)
        {
            return new MsaglPoint(e.X, e.Y);
        }

        public void SetEdgeLabel(DrawingEdge edge, Drawing.Label label)
        {
            //find the edge first
            DEdge de = null;
            foreach (DEdge dEdge in Edges)
                if (dEdge.DrawingEdge == edge)
                {
                    de = dEdge;
                    break;
                }
            edge.Label = label;
            de.Label = new DTextLabel(de, label);
            edge.GeometryEdge.Label = label.GeometryLabel;
            ICurve curve = edge.GeometryEdge.Curve;
            label.GeometryLabel.Center = curve[(curve.ParStart + curve.ParEnd) / 2];
            label.GeometryLabel.GeometryParent = edge.GeometryEdge;
        }

        public void StartDrawingRubberLine(MsaglPoint startingPoint)
        {
            if (RubberEdgePath != null)
                MainCanvas.Children.Remove(RubberEdgePath);
            RubberEdgePath = new Path() { Stroke = BlackBrush, StrokeThickness = 2.0 };
            PathGeometry pg = new PathGeometry();
            PathFigure pf = new PathFigure();
            pf.StartPoint = new WinPoint(startingPoint.X, startingPoint.Y);
            pg.Figures.Add(pf);
            RubberEdgePath.Data = pg;
            MainCanvas.Children.Add(RubberEdgePath);
        }

        public void StopDrawingRubberEdge()
        {
            MainCanvas.Children.Remove(RubberEdgePath);
            RubberEdgePath = null;
        }

        public void StopDrawingRubberLine()
        {
            MainCanvas.Children.Remove(RubberEdgePath);
            RubberEdgePath = null;
        }

        public double UnderlyingPolylineCircleRadius
        {
            get { return 5.0; }
        }

        public IViewerGraph ViewerGraph
        {
            get { return this; }
        }

        public DrawingGraph Graph
        {
            get
            {
                return DrawingGraph;
            }
            set
            {
                DrawingGraph = value;
                RebuildFromDrawingGraph();
            }
        }

        // Currently unused. This should probably get called when the size or zoom changes?
        public event EventHandler<EventArgs> ViewChangeEvent;

        public IViewerNode CreateIViewerNode(DrawingNode drawingNode)
        {
            return new DNode(this, drawingNode);
        }

        public IViewerNode CreateIViewerNode(DrawingNode drawingNode, MsaglPoint center, object visualElement)
        {
            DNode ret = new DNode(this, drawingNode);
            // not sure what I'm supposed to do here...
            return ret;
        }

        public double CurrentScale
        {
            get { return 1.0; } // I suppose this is the zoom?
        }

        public bool NeedToCalculateLayout { get; set; }

        public void RemoveSourcePortEdgeRouting()
        {
            // I think this is to remove the visual hint of the source port location.
        }

        public void RemoveTargetPortEdgeRouting()
        {
            // I think this is to remove the visual hint of the target port location.
        }

        public void SetSourcePortForEdgeRouting(MsaglPoint portLocation)
        {
            // I think this is to add the visual hint of the source port location.
        }

        public void SetTargetPortForEdgeRouting(MsaglPoint portLocation)
        {
            // I think this is to add the visual hint of the target port location.
        }

        public PlaneTransformation Transform { get; set; }

        #endregion // IViewer Members
    }
}