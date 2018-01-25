namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// Keeps left and right sons of the node. Is used in curve intersections routines.
    /// </summary>
    internal class ParallelogramBinaryTreeNode:ParallelogramNode {
        
        ParallelogramNode leftSon;
        public ParallelogramNode LeftSon {
            get {
                return leftSon;
            }
            set {
                leftSon = value;
            }
        }
        ParallelogramNode rightSon;

        public ParallelogramNode RightSon {
            get {
                return rightSon;
            }
            set {
                rightSon = value;
            }
        }
    }
}
