//
// RelativeShape.cs
// MSAGL RelativeShape class for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing {
    /// <summary>
    /// A shape wrapping an ICurve delegate, providing additional information.
    /// </summary>
    public class RelativeShape : Shape {
        /// <summary>
        /// The curve of the shape.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BoundaryCurve"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "RelativeShape")]
        public override ICurve BoundaryCurve { 
            get { return curveDelegate(); }
            set {
                throw new InvalidOperationException(
#if TEST_MSAGL
                        "Cannot set BoundaryCurve directly for RelativeShape"
#endif // TEST
                    );
            }
        }

        readonly Func<ICurve> curveDelegate;

        /// <summary>
        /// Constructor taking the ID and the curve delegate for the shape.
        /// </summary>
        /// <param name="curveDelegate"></param>
        public RelativeShape(Func<ICurve> curveDelegate)
        {
            this.curveDelegate = curveDelegate;
        }
    }
}
