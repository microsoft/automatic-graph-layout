using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// an interface for an edge drawn by viewer which is enabled for editing
    /// </summary>
    public interface IViewerEdge : IViewerObject {
        /// <summary>
        /// the corresponding drawing edge
        /// </summary>
        Edge Edge { get;}
        /// <summary>
        /// the edge source
        /// </summary>
        IViewerNode Source { get;}
        /// <summary>
        /// the edge target
        /// </summary>
        IViewerNode Target { get;}

        /// <summary>
        ///the radius of circles drawin around polyline corners 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
         double RadiusOfPolylineCorner { get; set;}
    }
}
