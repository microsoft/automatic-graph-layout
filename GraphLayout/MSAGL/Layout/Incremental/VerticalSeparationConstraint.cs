using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Incremental {
    /// <summary>
    /// A vertical separation constraint requires a minimum separation between y coordinates of two nodes,
    /// i.e. u.Y + separation less or equal v.Y
    /// </summary>
    public class VerticalSeparationConstraint : IConstraint {
        bool equality;
        /// <summary>
        /// 
        /// </summary>
        public bool IsEquality
        {
            get { return equality; }
        }
        private Node u;
        /// <summary>
        /// Constrained to be vertically above the BottomNode
        /// </summary>
        public Node TopNode {
            get { return u; }
        }
        private Node v;
        /// <summary>
        /// Constrained to be vertically below the TopNode
        /// </summary>
        public Node BottomNode {
            get { return v; }
        }
        private double separation;
        /// <summary>
        /// We allow the separation of existing constraints to be modified by the user.
        /// </summary>
        public double Separation {
            get { return separation; }
            set { separation = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="separation"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public VerticalSeparationConstraint(Node u, Node v, double separation)
        {
            this.u = u;
            this.v = v;
            this.separation = separation;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="separation"></param>
        /// <param name="equality"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public VerticalSeparationConstraint(Node u, Node v, double separation, bool equality)
        {
            this.equality = equality;
            this.u = u;
            this.v = v;
            this.separation = separation;
        }
        /// <summary>
        /// 
        /// </summary>
        public virtual double Project() {
            double uv = v.Center.Y - u.Center.Y;
            double d = separation - uv;
            if (d > 0) {
                double
                    wu = ((FiNode)u.AlgorithmData).stayWeight,
                    wv = ((FiNode)v.AlgorithmData).stayWeight;
                double f = d/(wu+wv);
                u.Center = new Point(u.Center.X, u.Center.Y - wv*f);
                v.Center = new Point(v.Center.X, v.Center.Y + wu*f);
                return d;
            } else {
                return 0;
            }
        }
        /// <summary>
        /// VerticalSeparationConstraint are usually structural and therefore default to level 0
        /// </summary>
        /// <returns>0</returns>
        public int Level { get { return 0; } }
        /// <summary>
        /// Get the list of nodes involved in the constraint
        /// </summary>
        public IEnumerable<Node> Nodes { get { return new Node[] { u, v }; } }
    }
}