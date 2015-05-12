using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// interface of a node that could be edited
    /// </summary>
    public interface IViewerNode:IViewerObject {
        /// <summary>
        /// the corresponding drawing node
        /// </summary>
        Node Node { get;}
        /// <summary>
        /// incomind editable edges
        /// </summary>
        IEnumerable<IViewerEdge> InEdges {get;}
        /// <summary>
        /// outgoing editable edges
        /// </summary>
        IEnumerable<IViewerEdge> OutEdges {get;}
        /// <summary>
        /// self editable edges
        /// </summary>
        IEnumerable<IViewerEdge> SelfEdges {get;}


        /// <summary>
        /// </summary>
        event Action<IViewerNode> IsCollapsedChanged;

    }
}
