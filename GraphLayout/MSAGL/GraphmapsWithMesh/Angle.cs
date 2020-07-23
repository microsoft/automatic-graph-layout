using System;

namespace Microsoft.Msagl.GraphmapsWithMesh
{
    class Angle
    {
        //compute the angle at A if it is less than pi/2, otherwise return pi/2
        public static double getAngleIfSmallerThanPIby2(Vertex A, Vertex B, Vertex C)
        {

            double xDiff1 = A.XLoc - B.XLoc;
            double yDiff1 = A.YLoc - B.YLoc;

            double xDiff2 = A.XLoc - C.XLoc;
            double yDiff2 = A.YLoc - C.YLoc;

            if (B.XLoc >= A.XLoc && C.XLoc >= A.XLoc && B.YLoc >= A.YLoc && C.YLoc >= A.YLoc)
                return Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2));
            if (B.XLoc >= A.XLoc && C.XLoc <= A.XLoc && B.YLoc >= A.YLoc && C.YLoc <= A.YLoc)
                return Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2));
            if (B.XLoc <= A.XLoc && C.XLoc >= A.XLoc && B.YLoc <= A.YLoc && C.YLoc >= A.YLoc)
                return Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2));
            if (B.XLoc <= A.XLoc && C.XLoc <= A.XLoc && B.YLoc <= A.YLoc && C.YLoc <= A.YLoc)
                return Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2));


            return Math.PI / 2;
        }

        public static double getClockwiseAngle(Vertex A, Vertex B, Vertex C)
        {

            double xDiff1 = A.XLoc - B.XLoc;
            double yDiff1 = A.YLoc - B.YLoc;

            double xDiff2 = A.XLoc - C.XLoc;
            double yDiff2 = A.YLoc - C.YLoc;

            double dot = xDiff1 * xDiff2 + yDiff1 * yDiff2;     // dot product
            double det = xDiff1 * yDiff2 - yDiff1 * xDiff2;     // determinant
            double angle = Math.Atan2(det, dot);  // atan2(y, x) or atan2(sin, cos)

            if (angle < 0) angle = 2 * Math.PI + angle;
            return Math.Abs(angle); // in degree*180/Math.PI; 
        }
    }
}
