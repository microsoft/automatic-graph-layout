using System;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// an interface for an editable object 
    /// </summary>
    public interface IViewerObject {
        /// <summary>
        /// gets or sets the corresponding DrawingObject
        /// </summary>
        DrawingObject DrawingObject { get;}

        /// <summary>
        /// is set to true when the object is selected for editing
        /// </summary>
        bool MarkedForDragging { get;set;}
  
        /// <summary>
        /// raised when the entity is marked for dragging
        /// </summary>
        event EventHandler MarkedForDraggingEvent;

        /// <summary>
        /// raised when the entity is unmarked for dragging
        /// </summary>
        event EventHandler UnmarkedForDraggingEvent;

    }
}
