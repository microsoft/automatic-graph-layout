using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Geometry
{
    /// <summary>
    /// Algorithm to pack rectangles
    /// </summary>
    public abstract class Packing : AlgorithmBase
    {
        /// <summary>
        /// The width of the widest row in the packed solution
        /// </summary>
        public double PackedWidth { get; protected set; }

        /// <summary>
        /// The height of the bounding box of the packed solution
        /// </summary>
        public double PackedHeight { get; protected set; }

        /// <summary>
        /// Aspect ratio of the bounding box of the packed solution
        /// </summary>
        public double PackedAspectRatio
        {
            get
            {
                return PackedWidth / PackedHeight;
            }
        }
    }
}
