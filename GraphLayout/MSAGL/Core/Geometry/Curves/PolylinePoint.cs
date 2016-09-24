using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Msagl.Core.Geometry.Curves {
	/// <summary>
	/// 
	/// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
	public class PolylinePoint {
		/// <summary>
		/// 
		/// </summary>
		Point point;

		/// <summary>
		/// 
		/// </summary>
		public Point Point {
            get { return point; }
            set {
#if SHARPIT
                point = value.Clone();
#else
                point = value;
#endif
                if (Polyline != null)
                    Polyline.RequireInit();
            }
        }


        PolylinePoint next;

		/// <summary>
		/// 
		/// </summary>
		public PolylinePoint Next {
            get { return next; }
            set {
                next = value;
                if (Polyline != null)
                    Polyline.RequireInit();
            }
        }

        PolylinePoint prev;

        /// <summary>
        /// 
        /// </summary>
        public PolylinePoint Prev {
            get { return prev; }
            set {
                prev = value;
                if (Polyline != null)
                    Polyline.RequireInit();
            }
        }

        internal PolylinePoint() {
        }

        /// <summary>
        /// 
        /// </summary>
        public PolylinePoint(Point p) {
#if SHARKPIT
            Point = p.Clone();
#else
            Point = p;
#endif
        }

        Polyline polyline;

        /// <summary>
        /// 
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Polyline Polyline {
            get { return polyline; }
            set { polyline = value; }
        }

		/// <summary>
		/// 
		/// </summary>
		public override string ToString() {
            return point.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        public PolylinePoint NextOnPolyline {
            get { return Polyline.Next(this); }
        }

        /// <summary>
        /// 
        /// </summary>
        public PolylinePoint PrevOnPolyline {
            get { return Polyline.Prev(this); }
        }

    }
}