using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// arguments for the event of changing the object under the mouse cursor
    /// </summary>
    public class ObjectUnderMouseCursorChangedEventArgs:EventArgs {
        IViewerObject oldObject;
        /// <summary>
        /// The old object under the mouse
        /// </summary>
        public IViewerObject OldObject {
            get { return oldObject; }
            set { oldObject = value; }
        }
        IViewerObject newObject;

        /// <summary>
        /// the new object under the mouse
        /// </summary>
        public IViewerObject NewObject {
            get { return newObject; }
            set { newObject = value; }
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="oldObject"></param>
        /// <param name="newObject"></param>
        public ObjectUnderMouseCursorChangedEventArgs(IViewerObject oldObject, IViewerObject newObject) {
            OldObject = oldObject;
            NewObject = newObject;
        }
        /// <summary>
        /// an empty constructor
        /// </summary>
        public ObjectUnderMouseCursorChangedEventArgs() {}
    }
}
