using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Incremental {
    /// <summary>
    /// A MinSeparationConstraint requires a minimum distance between two nodes, i.e. if nodes are closer than
    /// the minimum separation they will be projected apart, if they are further apart than the minimum
    /// separation then we don't need to do anything.
    /// </summary>
    public class MinSeparationConstraint : StickConstraint {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="separation"></param>
        public MinSeparationConstraint(Node u, Node v, double separation)
            : base(u, v, separation) { }
        /// <summary>
        /// 
        /// </summary>
        public override double Project() {
            Point uv = v.Center - u.Center;
            if (uv.Length < separation) {
                return base.Project();
            } else {
                return 0;
            }
        }
    }
}