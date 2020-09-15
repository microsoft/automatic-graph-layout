using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;

#if TEST_MSAGL
using System.Linq;
#endif

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging {
    /// <summary>
    /// represents a segment of a path
    /// </summary>
    internal class LinkedPoint : IEnumerable<Point> {
        internal Point Point { get; set; }

        internal LinkedPoint Next { get; set; }

        internal LinkedPoint(Point point) {
            Point = point;
        }

        public IEnumerator<Point> GetEnumerator() {
            for (var p = this; p != null; p = p.Next)
                yield return p.Point;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        internal double X { get { return Point.X; } }
 
        internal double Y { get { return Point.Y; } }

        internal void InsertVerts(int i, int j, Point[] points) {
            for (j--; i < j; j--)
                SetNewNext(points[j]);
        }

        public void InsertVertsInReverse(int i, int j, Point[] points) {
            for (i++; i < j; i++)
                SetNewNext(points[i]);
        }

        internal void SetNewNext(Point p) {
            var nv = new LinkedPoint(p);
            var tmp = Next;
            Next = nv;
            nv.Next = tmp;            
            Debug.Assert( CompassVector.IsPureDirection(Point, Next.Point) );
        }

#if TEST_MSAGL
        public override string ToString() {
            return Point.ToString();
        }
#endif


    }
}
