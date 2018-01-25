using System;
using System.Diagnostics;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Rectilinear {
  static internal class SpliceUtility {
    // Most of the original contents of this file have been subsumed into ObstacleTree and TransientGraphUtility.
    internal static Point MungeClosestIntersectionInfo(Point rayOrigin, IntersectionInfo closestIntersection, bool isHorizontal) {
      Rectangle bbox = closestIntersection.Segment1.BoundingBox;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=369
            Point closest = RawIntersection(closestIntersection, rayOrigin).Clone();
#else
      Point closest = RawIntersection(closestIntersection, rayOrigin);
#endif
      if (isHorizontal) {
        closest.X = MungeIntersect(rayOrigin.X, closest.X, bbox.Left, bbox.Right);
      }
      else {                                          // vertical
        closest.Y = MungeIntersect(rayOrigin.Y, closest.Y, bbox.Bottom, bbox.Top);
      }
      return closest;
    }

    // Make sure that we intersect the object space.
    static internal double MungeIntersect(double site, double intersect, double start, double end) {
      if (site < intersect) {
        double min = Math.Min(start, end);
        if (intersect < min) {
          intersect = min;
        }
      }
      else if (site > intersect) {
        double max = Math.Max(start, end);
        if (intersect > max) {
          intersect = max;
        }
      }
      return ApproximateComparer.Round(intersect);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "origin")]
    static internal Point RawIntersection(IntersectionInfo xx, Point origin) {
      // If this fires, then you didn't pass the LineSegment as the first argument to GetAllIntersections.
      Debug.Assert(xx.Segment0 is LineSegment, "LineSegment was not first arg to GetAllIntersections");

      // The intersection snaps the end of the intersection to the PolylinePoint at the start/end
      // of the interesecting segment on the obstacle if the intersection is Curve.CloseIntersections
      // to that segment endpoint, which can return a point that is just more than Curve.DistanceEpsilon
      // off the line.  Therefore, re-create the intersection using the LineSegment and intersection
      // parameters (this assumes the LineSegment.End is not Curve.CloseIntersections to the intersection).
      Point point = xx.Segment0[xx.Par0];

#if DEBUG
            // This may legitimately be rounding-error'd in the same way as xx.IntersectionPoint (and the
            // caller addresses this later).  The purpose of the assert is to verify that the LineSegment
            // interception is not outside the bbox in the perpendicular direction.
            var lineSeg = (LineSegment)xx.Segment0;
            if (StaticGraphUtility.IsVertical(PointComparer.GetDirections(lineSeg.Start, lineSeg.End))) {
                Debug.Assert(PointComparer.Equal(point.X, origin.X), "segment0 obstacle intersection is off the vertical line");
            }
            else {
                Debug.Assert(PointComparer.Equal(point.Y, origin.Y), "segment0 obstacle intersection is off the horizontal line");
            }
#endif // DEBUG
      return ApproximateComparer.Round(point);
    }
  }
}