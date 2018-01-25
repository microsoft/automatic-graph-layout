using System;
using Microsoft.Msagl.Core.Geometry;


namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// The base class of the Graph,Node and Edge classes
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    abstract public class GeometryObject {
       
        GeometryObject geometryParent;
        /// <summary>
        /// the parent of the object
        /// </summary>
        public GeometryObject GeometryParent {
            get { return geometryParent; }
            set { geometryParent = value; }
        }

        /// <summary>
        /// Storage for any data algorithms may want to store temporarily.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
#if TEST_MSAGL
        [NonSerialized]
#endif

        public object AlgorithmData;

        /// <summary>
        /// keeps the back pointer to the user data
        /// </summary>
        public object UserData { get; set; }

        event EventHandler<LayoutChangeEventArgs> beforeLayoutChangeEvent;
        
        /// <summary>
        /// event signalling that the layout is about to change
        /// </summary>
        virtual public event EventHandler<LayoutChangeEventArgs> BeforeLayoutChangeEvent {
            add { beforeLayoutChangeEvent += value; }
            remove { beforeLayoutChangeEvent -= value; }
        }
        
        /// <summary>
        /// gets or sets the boundary box of a GeometryObject
        /// </summary>
        public abstract Rectangle BoundingBox { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newValue"></param>
        public virtual void RaiseLayoutChangeEvent(object newValue) {
            if (beforeLayoutChangeEvent != null)
                beforeLayoutChangeEvent(this, new LayoutChangeEventArgs {DataAfterChange = newValue});
        }
        
        
    }

}
