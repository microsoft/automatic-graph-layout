using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Geometry
{
    /// <summary>
    /// Pack rectangles (without rotation) into a given aspect ratio
    /// </summary>
    public class OptimalColumnPacking<TData> : OptimalPacking<TData>
    {
        /// <summary>
        /// Constructor for packing, call Run to do the actual pack.
        /// Each RectangleToPack.Rectangle is updated in place.
        /// Performs a Golden Section Search on packing width for the 
        /// closest aspect ratio to the specified desired aspect ratio
        /// </summary>
        /// <param name="rectangles"></param>
        /// <param name="aspectRatio"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public OptimalColumnPacking(IEnumerable<RectangleToPack<TData>> rectangles, double aspectRatio)
            : base(rectangles.ToList(), aspectRatio)
        {
            ValidateArg.IsNotNull(rectangles, "rectangles");
            Debug.Assert(rectangles.Any(), "Expected more than one rectangle in rectangles");
            Debug.Assert(aspectRatio > 0, "aspect ratio should be greater than 0");

            this.createPacking = (rs, height) => new ColumnPacking<TData>(rs, height);
        }

        /// <summary>
        /// Performs a Golden Section Search on packing height for the 
        /// closest aspect ratio to the specified desired aspect ratio
        /// </summary>
        protected override void RunInternal()
        {
            double minRectHeight = double.MaxValue;
            double maxRectHeight = 0;
            double totalHeight = 0;

            // initial widthLowerBound is the width of a perfect packing for the desired aspect ratio
            foreach (var rtp in rectangles)
            {
                Rectangle r = rtp.Rectangle;
                Debug.Assert(r.Width > 0, "Width must be greater than 0");
                Debug.Assert(r.Height > 0, "Height must be greater than 0");
                totalHeight += r.Height;
                minRectHeight = Math.Min(minRectHeight, r.Height);
                maxRectHeight = Math.Max(maxRectHeight, r.Height);
            }
            Pack(maxRectHeight, totalHeight, minRectHeight);
        }
    }
}
