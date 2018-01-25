#region Using directives



#endregion

using System;

namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// Represents a node containing a parallelogram.
    /// Is used in curve intersections routines.
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    abstract public class ParallelogramNode {
        Parallelogram parallelogram;
        /// <summary>
        /// gets or sets the parallelogram of the node
        /// </summary>
        public Parallelogram Parallelogram {
            get {
                return parallelogram;
            }
            set {
                parallelogram = value;
            }
        }
    }
}
