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
﻿using System;
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
            Console.WriteLine(Curve.PointRelativeToCurveLocation(point, polygon));

            //=====================================================================//
            // find out if one curve intersects another 
            var polygon0 = new Polyline(new[] { new Point(7.041886, 4.683227), new Point(7.102811, 5.797279), new Point(8.303899, 6.467452), new Point(8.547598, 5.1097) }) { Closed = true };
#if TEST
            LayoutAlgorithmSettings.Show(polygon, polygon0);
#endif

            if (PolylinesIntersect(polygon0, polygon))
                Console.WriteLine("intersects");
            else
                Console.WriteLine("do not intersects");

            (polygon0 as ICurve).Translate(new Point(1, 0));
#if TEST
            LayoutAlgorithmSettings.Show(polygon, polygon0);
#endif

            if (PolylinesIntersect(polygon0, polygon))
                Console.WriteLine("intersect");
            else
                Console.WriteLine("do not intersect");
          
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
