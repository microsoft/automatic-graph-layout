using System;

namespace Microsoft.Msagl.GraphmapsWithMesh
{
    public class PointToSegmentDistance
    {
        public static double GetDistance(Microsoft.Msagl.Core.Geometry.Point pointA, Microsoft.Msagl.Core.Geometry.Point pointB, Microsoft.Msagl.Core.Geometry.Point pointC)
        {
            return GetDistance(new Vertex((int)pointA.X, (int)pointA.Y), new Vertex((int)pointB.X, (int)pointB.Y), new Vertex((int)pointC.X, (int)pointC.Y));
        }
        public static Microsoft.Msagl.Core.Geometry.Point getClosestPoint(Vertex pointA, Vertex pointB, Vertex pointP)
        {

            double[] AP = new double[2];
            AP[0] = pointP.XLoc - pointA.XLoc;
            AP[1] = pointP.YLoc - pointA.YLoc;

            double[] AB = new double[2];
            AB[0] = pointB.XLoc - pointA.XLoc;
            AB[1] = pointB.YLoc - pointA.YLoc;

            double AB_length = AB[0] * AB[0] + AB[1] * AB[1];
            double AP_AB_Dot = AP[0] * AB[0] + AP[1] * AB[1];
            double t = AP_AB_Dot / AB_length;

            if (t < 0) t = 0;
            if (t > 1) t = 1;
            double pointx = pointA.XLoc + AB[0] * t;
            double pointy = pointA.YLoc + AB[1] * t;
            return new Microsoft.Msagl.Core.Geometry.Point(pointx, pointy);
        }
        //Compute the distance from AB to C
        //if isSegment is true, AB is a segment, not a line.
        public static double GetDistance(Vertex pointA, Vertex pointB, Vertex pointC)
        {
            double dist = CrossProduct(pointA, pointB, pointC) / Distance(pointA, pointB);

            double dot1 = DotProduct(pointA, pointB, pointC);
            if (dot1 > 0)
                return Distance(pointB, pointC);

            double dot2 = DotProduct(pointB, pointA, pointC);
            if (dot2 > 0)
                return Distance(pointA, pointC);

            return Math.Abs(dist);
        }



        //Compute the dot product AB . AC
        private static double DotProduct(Vertex pointA, Vertex pointB, Vertex pointC)
        {
            double[] ab = new double[2];
            double[] bc = new double[2];
            ab[0] = pointB.XLoc - pointA.XLoc;
            ab[1] = pointB.YLoc - pointA.YLoc;
            bc[0] = pointC.XLoc - pointB.XLoc;
            bc[1] = pointC.YLoc - pointB.YLoc;
            double dot = ab[0] * bc[0] + ab[1] * bc[1];

            return dot;
        }

        //Compute the cross product AB x AC
        static private double CrossProduct(Vertex pointA, Vertex pointB, Vertex pointC)
        {
            double[] ab = new double[2];
            double[] ac = new double[2];
            ab[0] = pointB.XLoc - pointA.XLoc;
            ab[1] = pointB.YLoc - pointA.YLoc;
            ac[0] = pointC.XLoc - pointA.XLoc;
            ac[1] = pointC.YLoc - pointA.YLoc;
            double cross = ab[0] * ac[1] - ab[1] * ac[0];

            return cross;
        }

        //Compute the distance from A to B
        static double Distance(Vertex pointA, Vertex pointB)
        {
            double d1 = pointA.XLoc - pointB.XLoc;
            double d2 = pointA.YLoc - pointB.YLoc;

            return Math.Sqrt(d1 * d1 + d2 * d2);
        }



    }
}