using System;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.GraphmapsWithMesh
{
    class MsaglUtilities
    {
        public static bool HittedSegmentComesFromLeft(LineSegment hittedSegment, LineSegment hittingSegment)
        {
            //going up
            if (hittingSegment.Start.X == hittingSegment.End.X &&
                hittingSegment.Start.Y <= hittingSegment.End.Y &&
                hittedSegment.End.X <= hittedSegment.Start.X) return true;
            //going left
            if (hittingSegment.Start.Y == hittingSegment.End.Y &&
                hittingSegment.Start.X >= hittingSegment.End.X &&
                hittedSegment.End.Y <= hittedSegment.Start.Y) return true;
            //going down
            if (hittingSegment.Start.X == hittingSegment.End.X &&
                hittingSegment.Start.Y >= hittingSegment.End.Y &&
                hittedSegment.End.X >= hittedSegment.Start.X) return true;
            //going right
            if (hittingSegment.Start.Y == hittingSegment.End.Y &&
                hittingSegment.Start.X <= hittingSegment.End.X &&
                hittedSegment.End.Y >= hittedSegment.Start.Y) return true;

            return false;
        }




        public static bool PointIsOnAxisAlignedSegment(LineSegment l, Core.Geometry.Point p)
        {
            if ((int)l.Start.X == (int)l.End.X && (int)l.Start.X == (int)p.X)
            {
                if (l.Start.Y < l.End.Y && l.Start.Y <= p.Y && p.Y <= l.End.Y) return true;
                if (l.Start.Y > l.End.Y && l.Start.Y >= p.Y && p.Y >= l.End.Y) return true;
            }

            if ((int)l.Start.Y == (int)l.End.Y && (int)l.Start.Y == (int)p.Y)
            {
                if (l.Start.X < l.End.X && l.Start.X <= p.X && p.X <= l.End.X) return true;
                if (l.Start.X > l.End.X && l.Start.X >= p.X && p.X >= l.End.X) return true;
            }
            return false;
        }

        public static bool PointIsOnSegment(LineSegment l, Core.Geometry.Point p)
        {

            double AB = Math.Sqrt((l.Start.X - l.End.X) * (l.Start.X - l.End.X) + (l.Start.Y - l.End.Y) * (l.Start.Y - l.End.Y));
            double AP = Math.Sqrt((l.Start.X - p.X) * (l.Start.X - p.X) + (l.Start.Y - p.Y) * (l.Start.Y - p.Y));
            double PB = Math.Sqrt((p.X - l.End.X) * (p.X - l.End.X) + (p.Y - l.End.Y) * (p.Y - l.End.Y));
            if (AB < (AP + PB + .0001) && (AP + PB - .0001) < AB) return true;
            return false;
        }

        public static double EucledianDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }
    }
}
