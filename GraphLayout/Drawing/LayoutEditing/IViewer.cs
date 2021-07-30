using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// the interface for the viewer which is able to edit the graph layout
    /// </summary>
    public interface IViewer {
        bool IncrementalDraggingModeAlways { get; }
        /// <summary>
        /// the scale to screen
        /// </summary>
        double CurrentScale { get; }

        /// <summary>
        /// creates a visual element for the node, and the corresponding geometry node is created according 
        /// to the size of the visual element
        /// </summary>
        /// <param name="drawingNode">usually the drawing node has a label, and the visual element is created accordingly</param>
        /// <param name="center">the node center location</param>
        /// <param name="visualElement">if this value is not null then is should be a visual for the label, and the node width and height 
        /// will be taken from this visual</param>
        /// <returns>new IViewerNode</returns>
        IViewerNode CreateIViewerNode(Node drawingNode, Point center, object visualElement);


        /// <summary>
        /// creates a default visual element for the node
        /// </summary>
        /// <param name="drawingNode"></param>
        /// <returns></returns>
        IViewerNode CreateIViewerNode(Node drawingNode);

        /// <summary>
        /// if set to true the Graph geometry is unchanged after the assignment viewer.Graph=graph;
        /// </summary>
        bool NeedToCalculateLayout { get; set; }


        /// <summary>
        /// the viewer signalls that the view, the transform or the viewport, has changed
        /// </summary>
        event EventHandler<EventArgs> ViewChangeEvent;

        /// <summary>
        /// signalling the mouse down event
        /// </summary>
        event EventHandler<MsaglMouseEventArgs> MouseDown;

        /// <summary>
        /// signalling the mouse move event
        /// </summary>
        event EventHandler<MsaglMouseEventArgs> MouseMove;

        /// <summary>
        /// signalling the mouse up event
        /// </summary>
        event EventHandler<MsaglMouseEventArgs> MouseUp;

        /// <summary>
        /// the event raised at a time when ObjectUnderMouseCursor changes
        /// </summary>
        event EventHandler<ObjectUnderMouseCursorChangedEventArgs> ObjectUnderMouseCursorChanged;


        /// <summary>
        /// Returns the object under the cursor and null if there is none
        /// </summary>
        IViewerObject ObjectUnderMouseCursor { get; }

        /// <summary>
        /// forcing redraw of the object
        /// </summary>
        /// <param name="objectToInvalidate"></param>
        void Invalidate(IViewerObject objectToInvalidate);


        /// <summary>
        /// invalidates everything
        /// </summary>
        void Invalidate();

        /// <summary>
        /// is raised after the graph is changed
        /// </summary>
        event EventHandler GraphChanged;

        /// <summary>
        /// returns modifier keys; control, shift, or alt are pressed at the moments
        /// </summary>
        ModifierKeys ModifierKeys { get; }

        /// <summary>
        /// maps a point in screen coordinates to the point in the graph surface
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        Point ScreenToSource(MsaglMouseEventArgs e);

        /// <summary>
        /// gets all entities which can be dragged
        /// </summary>
        IEnumerable<IViewerObject> Entities { get; }

        /// <summary>
        /// number of dots per inch in x direction
        /// </summary>
        double DpiX { get; }

        /// <summary>
        /// number of dots per inch in y direction
        /// </summary>
        double DpiY { get; }

        /// <summary>
        /// this method should be called on the end of the dragging
        /// </summary>
        /// <param name="changedObjects"></param>
        void OnDragEnd(IEnumerable<IViewerObject> changedObjects);

        /// <summary>
        /// The scale dependent width of an edited curve that should be clearly visible.
        /// Used in the default entity editing.
        /// </summary>
        double LineThicknessForEditing { get; }

        /// <summary>
        /// enables and disables the default editing of the viewer
        /// </summary>
        bool LayoutEditingEnabled { get; }

        /// <summary>
        /// if is set to true then the mouse left click on a node and dragging the cursor to 
        /// another node will create an edge and add it to the graph
        /// </summary>
        bool InsertingEdge { get; set; }

        /// <summary>
        /// Pops up a pop up menu with a menu item for each couple, the string is the title and the delegate is the callback
        /// </summary>
        /// <param name="menuItems"></param>
        void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems);

        /// <summary>
        /// The radius of the circle drawn around a polyline corner
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        double UnderlyingPolylineCircleRadius { get; }

        /// <summary>
        /// gets or sets the graph
        /// </summary>
        Graph Graph { get; set; }

        /// <summary>
        /// prepare to draw the rubber line
        /// </summary>
        /// <param name="startingPoint"></param>
        void StartDrawingRubberLine(Point startingPoint);

        /// <summary>
        /// draw the rubber line to the current mouse position
        /// </summary>
        /// <param name="args"></param>
        void DrawRubberLine(MsaglMouseEventArgs args);

        /// <summary>
        /// draw rubber line to a given point
        /// </summary>
        /// <param name="point"></param>
        void DrawRubberLine(Point point);

        /// <summary>
        /// stop drawing the rubber line
        /// </summary>
        void StopDrawingRubberLine();

        /// <summary>
        /// add an edge to the viewer graph
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="registerForUndo"></param>
        /// <returns></returns>
        void AddEdge(IViewerEdge edge, bool registerForUndo);

        /// <summary>
        /// drawing edge already has its geometry in place
        /// </summary>
        /// <param name="drawingEdge"></param>
        /// <returns></returns>
        IViewerEdge CreateEdgeWithGivenGeometry(Edge drawingEdge);

        /// <summary>
        /// adds a node to the viewer graph
        /// </summary>
        /// <param name="node"></param>
        /// <param name="registerForUndo"></param>
        void AddNode(IViewerNode node, bool registerForUndo);

        /// <summary>
        /// removes an edge from the graph
        /// </summary>
        /// <param name="edge"></param>
        ///<param name="registerForUndo"></param>
        void RemoveEdge(IViewerEdge edge, bool registerForUndo);

        /// <summary>
        /// deletes node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="registerForUndo"></param>
        void RemoveNode(IViewerNode node, bool registerForUndo);

        /// <summary>
        /// Routes the edge. The edge will not be not attached to the graph after the routing
        /// </summary>
        /// <returns></returns>
        IViewerEdge RouteEdge(Edge drawingEdge);

        /// <summary>
        /// gets the viewer graph
        /// </summary>
        IViewerGraph ViewerGraph { get; }

        /// <summary>
        /// arrowhead length for newly created edges
        /// </summary>
        double ArrowheadLength { get; }

        /// <summary>
        /// creates the port visual if it does not exist, and sets the port location
        /// </summary>
        /// <param name="portLocation"></param>
        void SetSourcePortForEdgeRouting(Point portLocation);

        /// <summary>
        /// creates the port visual if it does not exist, and sets the port location
        /// </summary>
        /// <param name="portLocation"></param>
        void SetTargetPortForEdgeRouting(Point portLocation);

        /// <summary>
        /// removes the port
        /// </summary>
        void RemoveSourcePortEdgeRouting();
        /// <summary>
        /// removes the port
        /// </summary>
        void RemoveTargetPortEdgeRouting();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="edgeGeometry"></param>
        void DrawRubberEdge(EdgeGeometry edgeGeometry);

        /// <summary>
        /// stops drawing the rubber edge
        /// </summary>
        void StopDrawingRubberEdge();

        /// <summary>
        /// the transformation from the graph surface to the client viewport
        /// </summary>
        PlaneTransformation Transform { get; set; }
    }
}