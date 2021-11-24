using System;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Visibility {
    internal class Diagonal {

        public override string ToString() {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", Start, End);
        }
        internal Point Start {
            get { return leftTangent.End.Point; }
        }

        internal Point End {
            get { return rightTangent.End.Point; }
        }

        internal Diagonal(Tangent leftTangent, Tangent rightTangent) {
            this.LeftTangent = leftTangent;
            this.RightTangent = rightTangent;
        }

        Tangent leftTangent;

        internal Tangent LeftTangent {
            get { return leftTangent; }
            set { leftTangent = value; }
        }
        Tangent rightTangent;

        internal Tangent RightTangent {
            get { return rightTangent; }
            set { rightTangent = value; }
        }

        RBNode<Diagonal> rbNode;

        internal RBNode<Diagonal> RbNode {
            get { return rbNode; }
            set { rbNode = value; }
        }
    }
}
