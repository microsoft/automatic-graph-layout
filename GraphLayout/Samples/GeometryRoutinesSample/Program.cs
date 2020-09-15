using System;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace GeometryRoutinesSample {
    class Program {
        static void Main(string[] args) {
#if TEST
            Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif

            //define a polygone
            var polygon=new Polyline(new []{new Point(0,0), new Point(7,8), new Point(7,7), new Point(8,8), new Point(5,0)}){Closed=true};
            var point = new Point(3, 3);

            var ellipse = new Ellipse(0.5, 0.5, point);//just to show the point

            //showing the polygon and the ellipse with the center at the point
#if TEST
            LayoutAlgorithmSettings.Show(polygon, ellipse);
#endif

            // prints out the location of the point relative ot the polygon
            System.Diagnostics.Debug.WriteLine(Curve.PointRelativeToCurveLocation(point, polygon));

            //=====================================================================//
            // find out if one curve intersects another 
            var polygon0 = new Polyline(new[] { new Point(7.041886, 4.683227), new Point(7.102811, 5.797279), new Point(8.303899, 6.467452), new Point(8.547598, 5.1097) }) { Closed = true };
#if TEST
            LayoutAlgorithmSettings.Show(polygon, polygon0);
#endif

            if (PolylinesIntersect(polygon0, polygon))
                System.Diagnostics.Debug.WriteLine("intersects");
            else
                System.Diagnostics.Debug.WriteLine("do not intersects");

            (polygon0 as ICurve).Translate(new Point(1, 0));
#if TEST
            LayoutAlgorithmSettings.Show(polygon, polygon0);
#endif

            if (PolylinesIntersect(polygon0, polygon))
                System.Diagnostics.Debug.WriteLine("intersect");
            else
                System.Diagnostics.Debug.WriteLine("do not intersect");
          
        }

        static bool PolylinesIntersect(Polyline c0, Polyline c1) {
            if(Curve.CurveCurveIntersectionOne(c0, c1, false)!=null)
                return true;
            //could be that one lies inside of another
            if(c0.Closed)
                if(Curve.PointRelativeToCurveLocation((c1 as ICurve).Start, c0)!=PointLocation.Outside)
                    return true;

            if (c1.Closed)
                if (Curve.PointRelativeToCurveLocation((c0 as ICurve).Start, c1) != PointLocation.Outside)
                    return true;

            return false;
        }
    }
}
