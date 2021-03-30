using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// Specifies the way an edge is connected to a curve
    /// </summary>
#if TEST_MSAGL
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors"), System.Serializable]
#endif
    abstract public class Port {
        
        /// <summary>
        /// Gets the point associated with the port.
        /// </summary>
        public abstract Point Location { get; }

        /// <summary>
        /// Gets the boundary curve of the port.
        /// </summary>
        public abstract ICurve Curve { get; set; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return Location.ToString();
        }
    }
}