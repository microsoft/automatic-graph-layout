/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
                point = value;
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
            Point = p;
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