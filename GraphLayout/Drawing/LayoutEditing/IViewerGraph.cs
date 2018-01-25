using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// this interface represents a graph that is drawn by the viewer and can be edited by it
    /// </summary>
    public interface IViewerGraph {
        /// <summary>
        /// gets the drawing graph
        /// </summary>
        Graph DrawingGraph { get; }
        /// <summary>
        /// yields the nodes
        /// </summary>
        /// <returns></returns>
        IEnumerable<IViewerNode> Nodes();
        /// <summary>
        /// yields the edges
        /// </summary>
        /// <returns></returns>
        IEnumerable<IViewerEdge> Edges();

    }
}
