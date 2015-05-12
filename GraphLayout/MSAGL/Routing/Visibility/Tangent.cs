using System;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Visibility {
    internal class Tangent {

        Tangent comp;

        //the complimentary tangent
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Tangent Comp {
            get { return comp; }
            set { comp = value; }
        }

        internal bool IsHigh {
            get { return !IsLow; }
        }

        bool lowTangent; //true means that it is a low tangent to Q, false meanst that it is a high tangent to Q

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal bool IsLow {
            get { return lowTangent; }
            set { lowTangent = value; }
        }

        bool separatingPolygons;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal bool SeparatingPolygons
        {
            get { return separatingPolygons; }
            set { separatingPolygons = value; }
        }
       
        Diagonal diagonal;
        /// <summary>
        /// the diagonal will be not a null only when it is active
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Diagonal Diagonal {
            get { return diagonal; }
            set { diagonal = value; }
        }

        PolylinePoint start;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal PolylinePoint Start {
            get { return start; }
            set { start = value; }
        }

        PolylinePoint end;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public PolylinePoint End {
            get { return end; }
            set { end = value; }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Tangent(PolylinePoint start, PolylinePoint end) {
            this.Start = start;
            this.End = end;
        }

        public override string ToString() {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", Start, End);
        }
    }
}
