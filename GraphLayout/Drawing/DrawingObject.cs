using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// Base class for graph objects  
    /// </summary>
    [Serializable]
    public abstract class DrawingObject {
        /// <summary>
        /// 
        /// </summary>
        public event Action<DrawingObject> IsVisibleChanged;
        object userData;
        /// <summary>
        /// This field can be used as a backpointer to the user data associated with the object
        /// </summary>
        public object UserData {
            get { return userData; }
            set { userData = value; }
        }


        /// <summary>
        /// gets the bounding box of the object
        /// </summary>
        abstract public Rectangle BoundingBox { get;}
        /// <summary>
        /// gets the geometry object corresponding to the drawing object
        /// </summary>
        public abstract GeometryObject GeometryObject { get; set; }

        bool isVisible = true;

        /// <summary>
        /// gets or sets the visibility of an object
        /// </summary>
        virtual public bool IsVisible {
            get { return isVisible; }
            set {
                var was = isVisible;
                isVisible = value;
                if (was != isVisible && IsVisibleChanged != null)
                    IsVisibleChanged(this);
            }
        }
    }
}
