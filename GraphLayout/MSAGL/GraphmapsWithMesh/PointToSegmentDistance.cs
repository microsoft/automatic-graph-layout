using System;

namespace Microsoft.Msagl.GraphmapsWithMesh
{
    public class PointToSegmentDistance
    {
        public static double GetDistance(Core.Geometry.Point pointA, Core.Geometry.Point pointB, Core.Geometry.Point pointC)
        {
            return GetDistance(new Vertex((int)pointA.X, (int)pointA.Y), new Vertex((int)pointB.X, (int)pointB.Y), new Vertex((int)pointC.X, (int)pointC.Y));
        }
        public static Core.Geometry.Point GetClosestPoint(Vertex pointA, Vertex pointB, Vertex pointP)
        {
         
            var ap = new double[2];
            ap[0] = pointP.XLoc - pointA.XLoc;  
            ap[1] = pointP.YLoc - pointA.YLoc;

            var ab = new double[2];
            ab[0] = pointB.XLoc - pointA.XLoc;  
            ab[1] = pointB.YLoc - pointA.YLoc;

            double abLength = ab[0]*ab[0] + ab[1]*ab[1];    
            double apAbDot = ap[0]*ab[0] + ap[1]*ab[1];
            double t = apAbDot/abLength;
            
            if(t<0) t = 0;
            if(t>1) t = 1;
            double pointx = pointA.XLoc+ ab[0]*t;
            double pointy = pointA.YLoc+ ab[1]*t;
            return new Core.Geometry.Point(pointx,pointy);
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
            var ab = new double[2];
            var bc = new double[2];
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
            var ab = new double[2];
            var ac = new double[2];
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