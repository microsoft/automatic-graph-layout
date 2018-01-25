using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Geometry
{
    /// <summary>
    /// Constants used by OptimalRectanglePacking
    /// </summary>
    public static class PackingConstants
    {
        /// <summary>
        /// The greeks thought the GoldenRatio was a good aspect ratio: Phi = (1 + Math.Sqrt(5)) / 2
        /// </summary>
        /// <remarks>we also use this internally in our golden section search</remarks>
        public static readonly double GoldenRatio = (1 + Math.Sqrt(5)) / 2;

        /// <summary>
        /// equiv to 1 - (1/Phi) where Phi is the Golden Ratio: i.e. the smaller of the two sections
        /// if you divide a unit length by the golden ratio
        /// </summary>
        internal static readonly double GoldenRatioRemainder = 2 - GoldenRatio;
    }
}
