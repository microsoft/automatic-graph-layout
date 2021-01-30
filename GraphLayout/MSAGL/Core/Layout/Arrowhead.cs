using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Core.Layout
{
    /// <summary>
    /// keeps the arrowhead info
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    public class Arrowhead
    {

        ///<summary>
        ///</summary>
        public const double DefaultArrowheadLength = 10;
        double length = DefaultArrowheadLength;

        ///<summary>
        /// The overall length of the arrow head
        ///</summary>
        public double Length {
            get { return length; }
            set { length = value; }
        }

        ///<summary>
        /// The width of the arrow head at the base
        ///</summary>
        public double Width { get; set; }

        ///<summary>
        /// Where the tip of the arrow head is
        ///</summary>
        public Point TipPosition { get; set; }

        /// <summary>
        /// Clone the arrowhead information
        /// </summary>
        /// <returns></returns>
        public Arrowhead Clone()
        {
            return new Arrowhead()
            {
                Length = this.length,
                Width = this.Width,
                TipPosition = this.TipPosition,
            };
        }
    }
}
