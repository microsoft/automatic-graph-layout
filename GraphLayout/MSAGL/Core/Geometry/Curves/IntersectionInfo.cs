using System;
using System.Globalization;

namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// Contains the result of the intersection of two ICurves.
    /// </summary>
    public class IntersectionInfo {


        /* The following conditions should hold:
         * X=seg0[par0]=seg1[par1]
         */ 

        double par0, par1;
        Point x;

        ICurve seg0, seg1;
        /// <summary>
        /// the parameter on the first curve
        /// </summary>
        public double Par0 {
            get { return par0; }
            set { par0 = value; }
        }
        /// <summary>
        /// the parameter on the second curve
        /// </summary>
        public double Par1 {
            get { return par1; }
            set { par1 = value; }
        }

        /// <summary>
        /// the intersection point
        /// </summary>
        public Point IntersectionPoint {
            get { return x; }
            set { x = value; }
        }

/// <summary>
/// the segment of the first curve where the intersection point belongs
/// </summary>
        public ICurve Segment0 {
            get { return seg0; }
            set { seg0=value; }
        }

        /// <summary>
        /// the segment of the second curve where the intersection point belongs
        /// </summary>
        public ICurve Segment1 {
            get { return seg1; }
            set { seg1 = value; }
        }

        /// <summary>
        /// the constructor
        /// </summary>
        /// <param name="pr0"></param>
        /// <param name="pr1"></param>
        /// <param name="x"></param>
        /// <param name="s0"></param>
        /// <param name="s1"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object,System.Object)")]
        internal IntersectionInfo(double pr0, double pr1, Point x, ICurve s0, ICurve s1) {
            par0 = pr0;
            par1 = pr1;
            this.x = x;
            seg0 = s0;
            seg1 = s1;
#if DETAILED_DEBUG
            System.Diagnostics.Debug.Assert(ApproximateComparer.Close(x, s0[pr0], ApproximateComparer.IntersectionEpsilon*10),
                    string.Format("intersection not at curve[param]; x = {0}, s0[pr0] = {1}, diff = {2}", x, s0[pr0], x - s0[pr0]));
            System.Diagnostics.Debug.Assert(ApproximateComparer.Close(x, s1[pr1], ApproximateComparer.IntersectionEpsilon*10),
                    string.Format("intersection not at curve[param]; x = {0}, s1[pr1] = {1}, diff = {2}", x, s1[pr1], x - s1[pr1]));
#endif 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return String.Format(CultureInfo.InvariantCulture, "XX({0} {1} {2})", par0, par1, x);
        }
    }
}
