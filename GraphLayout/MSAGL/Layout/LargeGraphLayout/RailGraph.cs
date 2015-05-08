using System.Collections;
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    /// <summary>
    /// class containing the visible part of the drawing
    /// </summary>
    public class RailGraph {
        /// <summary>
        /// the set of visible nodes
        /// </summary>
        public Set<Node> Nodes = new Set<Node>();

        /// <summary>
        /// the set of visible rails
        /// </summary>
        public Set<Rail> Rails = new Set<Rail>();
        /// <summary>
        /// the set of visible edges
        /// </summary>
        public Set<Edge> Edges=new Set<Edge>();
    }
}