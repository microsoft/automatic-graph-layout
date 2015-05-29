using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;

namespace LocationLabeling {
    internal class TreeNode {
        internal Rectangle box = Rectangle.CreateAnEmptyBox();
        internal int count;
        /// <summary>
        /// left child
        /// </summary>
        internal TreeNode l;
        /// <summary>
        /// right child
        /// </summary>
        internal TreeNode r;
        internal Node node;
        public override string ToString() {
            return node != null ? node.UserData.ToString() : l.ToString() + " " + r.ToString();
        }

        internal TreeNode(TreeNode parent) {
            this.parent = parent;
        }

    
        internal TreeNode parent;
    }
}
