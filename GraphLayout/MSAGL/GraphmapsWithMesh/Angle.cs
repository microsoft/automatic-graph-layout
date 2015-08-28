using System;

namespace Microsoft.Msagl.GraphmapsWithMesh
{
    class Angle
    {
        //compute the angle at apex if it is less than pi/2, otherwise return pi/2

        /*TESTS
            Console.WriteLine("0 0, 1 1, 0 1" +Angle.getAngleIfSmallerThanPIby2(new Vertex(0, 0), new Vertex(1, 1), new Vertex(0, 1)));
            Console.WriteLine("0 0, -1 1, 0 1" + Angle.getAngleIfSmallerThanPIby2(new Vertex(0, 0), new Vertex(-1, 1), new Vertex(0, 1)));
            Console.WriteLine("0 0, -1 -1, 0 1" + Angle.getAngleIfSmallerThanPIby2(new Vertex(0, 0), new Vertex(-1, -1), new Vertex(0, 1)));
            Console.WriteLine("0 0, 1 -1, 0 1" + Angle.getAngleIfSmallerThanPIby2(new Vertex(0, 0), new Vertex(1, -1), new Vertex(0, 1)));
            Console.WriteLine("0 0, -1 1, 1 1" + Angle.getAngleIfSmallerThanPIby2(new Vertex(0, 0), new Vertex(-1, 1), new Vertex(1, 1)));
            Console.WriteLine("0 0, -1 -1, 1 1" + Angle.getAngleIfSmallerThanPIby2(new Vertex(0, 0), new Vertex(-1, -1), new Vertex(1, 1)));
            Console.WriteLine("0 0, 1 -1, 1 1" + Angle.getAngleIfSmallerThanPIby2(new Vertex(0, 0), new Vertex(1, -1), new Vertex(1, 1)));
         */
        public static double GetAngleIfSmallerThanPIby2(Vertex apex, Vertex vertex1, Vertex vertex2)
        {

            double xDiff1 = apex.XLoc - vertex1.XLoc;
            double yDiff1 = apex.YLoc - vertex1.YLoc;

            double xDiff2 = apex.XLoc - vertex2.XLoc;
            double yDiff2 = apex.YLoc - vertex2.YLoc;

            if (vertex1.XLoc >= apex.XLoc && vertex2.XLoc >= apex.XLoc && vertex1.YLoc >= apex.YLoc && vertex2.YLoc >= apex.YLoc)
                return Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2));
            if (vertex1.XLoc >= apex.XLoc && vertex2.XLoc <= apex.XLoc && vertex1.YLoc >= apex.YLoc && vertex2.YLoc <= apex.YLoc)
                return Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2));
            if (vertex1.XLoc <= apex.XLoc && vertex2.XLoc >= apex.XLoc && vertex1.YLoc <= apex.YLoc && vertex2.YLoc >= apex.YLoc)
                return Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2));
            if (vertex1.XLoc <= apex.XLoc && vertex2.XLoc <= apex.XLoc && vertex1.YLoc <= apex.YLoc && vertex2.YLoc <= apex.YLoc)
                return Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2));


            return Math.PI/2;//Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2));
        }

        public static double GetClockwiseAngle(Vertex apex, Vertex vertex1, Vertex vertex2)
        {

            double xDiff1 = apex.XLoc - vertex1.XLoc;
            double yDiff1 = apex.YLoc - vertex1.YLoc;

            double xDiff2 = apex.XLoc - vertex2.XLoc;
            double yDiff2 = apex.YLoc - vertex2.YLoc;

            double dot = xDiff1 * xDiff2 + yDiff1 * yDiff2;     // dot product
            double det = xDiff1 * yDiff2 - yDiff1 * xDiff2;     // determinant
            double angle = Math.Atan2(det, dot);  // atan2(y, x) or atan2(sin, cos)

            if (angle < 0) angle = 2*Math.PI + angle;
            return Math.Abs(angle); // in degree*180/Math.PI; 
        }
    }
}
