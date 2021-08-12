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
            Point closest = RawIntersection(closestIntersection);
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
        static internal Point RawIntersection(IntersectionInfo xx) {

            return ApproximateComparer.Round(xx.IntersectionPoint);
        }
    }
}