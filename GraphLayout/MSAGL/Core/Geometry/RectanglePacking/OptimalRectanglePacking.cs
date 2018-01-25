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
    public class OptimalRectanglePacking<TData> : OptimalPacking<TData>
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
        public OptimalRectanglePacking(IEnumerable<RectangleToPack<TData>> rectangles, double aspectRatio) 
            : base(RectanglePacking<TData>.SortRectangles(rectangles).ToList(), aspectRatio)
        {
            ValidateArg.IsNotNull(rectangles, "rectangles");
            Debug.Assert(rectangles.Any(), "Expected more than one rectangle in rectangles");
            Debug.Assert(aspectRatio > 0, "aspect ratio should be greater than 0");

            this.createPacking = (rs, width) => new RectanglePacking<TData>(rs, width, rectanglesPresorted: true);
        }

        /// <summary>
        /// Performs a Golden Section Search on packing width for the 
        /// closest aspect ratio to the specified desired aspect ratio
        /// </summary>
        protected override void RunInternal()
        {
            double minRectWidth = double.MaxValue;
            double maxRectWidth = 0;
            double totalWidth = 0;

            // initial widthLowerBound is the width of a perfect packing for the desired aspect ratio
            foreach (var r in rectangles)
            {
                Debug.Assert(r.Rectangle.Width > 0, "Width must be greater than 0");
                Debug.Assert(r.Rectangle.Height > 0, "Height must be greater than 0");

                double width = r.Rectangle.Width;
                totalWidth += width;
                minRectWidth = Math.Min(minRectWidth, width);
                maxRectWidth = Math.Max(maxRectWidth, width);
            }

            Pack(maxRectWidth, totalWidth, minRectWidth);
        }
    }
}
