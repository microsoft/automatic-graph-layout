using System;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public abstract class DObject : ObjectWithBox, Drawing.IViewerObject
    {
        internal DObject()
        {
        }

        protected DObject(DObject parent)
        {
            ParentObject = parent;
        }

        public DObject ParentObject { get; internal set; }

        public DGraph ParentGraph
        {
            get
            {
                DObject ret = this;
                while (ret.ParentObject != null)
                    ret = ret.ParentObject;
                return ret as DGraph;
            }
        }

        /// <summary>
        /// get the underlying drawing object
        /// </summary>
        public abstract Drawing.DrawingObject DrawingObject
        {
            get;
        }

        internal float[] DashPatternArray { get; set; }

        internal virtual BBNode BBNode { get; set; }

        override internal Rectangle Box { get { return BBNode.Box; } }

        public abstract void MakeVisual();

        bool markedForDragging;
        /// <summary>
        /// Implements a property of an interface IEditViewer
        /// </summary>
        public bool MarkedForDragging
        {
            get
            {
                return markedForDragging;
            }
            set
            {
                markedForDragging = value;
                if (value)
                {
                    if (MarkedForDraggingEvent != null)
                        MarkedForDraggingEvent(this, null);
                }
                else
                {
                    if (UnmarkedForDraggingEvent != null)
                        UnmarkedForDraggingEvent(this, null);
                }
            }
        }

        /// <summary>
        /// raised when the entity is marked for dragging
        /// </summary>
        public event EventHandler MarkedForDraggingEvent;

        /// <summary>
        /// raised when the entity is unmarked for dragging
        /// </summary>
        public event EventHandler UnmarkedForDraggingEvent;
    }
}