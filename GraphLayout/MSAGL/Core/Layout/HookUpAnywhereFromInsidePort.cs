using System;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// This port is for an edge connecting a node inside of the curve going out of the curve and creating a hook to 
    /// connect to the curve
    /// </summary>
    public class HookUpAnywhereFromInsidePort : Port {
        Func<ICurve> curve;
        double adjustmentAngle = Math.PI / 10;
        /// <summary>
        /// </summary>
        /// <param name="boundaryCurve"></param>
        /// <param name="hookSize"></param>
        public HookUpAnywhereFromInsidePort(Func<ICurve> boundaryCurve, double hookSize) {
            curve = boundaryCurve;
            HookSize=hookSize;
        }

        /// <summary>
        /// </summary>
        /// <param name="boundaryCurve"></param>
        public HookUpAnywhereFromInsidePort(Func<ICurve> boundaryCurve) {
            ValidateArg.IsNotNull(boundaryCurve,"boundaryCurve");
            curve = boundaryCurve;
            location=curve().Start;
        }

        Point location;
        /// <summary>
        /// returns a point on the boundary curve
        /// </summary>
        public override Point Location {
            get { return location; }
        }

        /// <summary>
        /// Gets the boundary curve of the port.
        /// </summary>
        public override ICurve Curve {
            get { return curve(); }
            set { throw new InvalidCastException(); }
        }

#if SHARPKIT
        internal void SetLocation(Point p) { location = p.Clone(); }
#else
        internal void SetLocation(Point p) { location = p; }
#endif
        internal Polyline LoosePolyline { get; set; }

        /// <summary>
        /// We are trying to correct the last segment of the polyline by make it perpendicular to the Port.Curve.
        ///For this purpose we trim the curve by the cone of the angle 2*adjustment angle and project the point before the last of the polyline to this curve.
        /// </summary>
        public double AdjustmentAngle {
            get {
                return adjustmentAngle;
            }
            set {
                adjustmentAngle = value;
            }
        }
        double hookSize = 9;
        /// <summary>
        /// the size of the self-loop
        /// </summary>
        public double HookSize { get { return hookSize; } set { hookSize = value; } }
    }
}