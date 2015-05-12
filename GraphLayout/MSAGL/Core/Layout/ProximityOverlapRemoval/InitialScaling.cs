using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval {
    /// <summary>
    /// Methods with which the graph is initially scaled.
    /// </summary>
    public enum InitialScaling {
        /// <summary>
        /// no scaling 
        /// </summary>
        None,
        /// <summary>
        /// Scaling such that the average edge length is 1 inch (72 pixels, due to historic reasons)
        /// </summary>
        Inch72Pixel,

        /// <summary>
        /// Scaling such that the average edge length is 4 times the average node size, where node size is the average of the width and height of the bounding box.
        /// </summary>
        AvgNodeSize,
        
    }

}
