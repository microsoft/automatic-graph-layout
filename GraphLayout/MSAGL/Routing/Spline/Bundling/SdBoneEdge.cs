using System;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;
using System.Diagnostics;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    [DebuggerDisplay("({SourcePoint.X},{SourcePoint.Y})->({TargetPoint.X},{TargetPoint.Y})")]
    internal class SdBoneEdge {
        internal readonly VisibilityEdge VisibilityEdge;
        internal readonly SdVertex Source;
        internal readonly SdVertex Target;
        int numberOfPassedPaths;

        internal SdBoneEdge(VisibilityEdge visibilityEdge, SdVertex source, SdVertex target) {
            VisibilityEdge = visibilityEdge;
            Source = source;
            Target = target;
        }

        internal Point TargetPoint {
            get { return Target.Point; }
        }

        internal Point SourcePoint {
            get { return Source.Point; }
        }

        internal bool IsOccupied {
            get { return numberOfPassedPaths > 0; }
        }

        internal Set<CdtEdge> CrossedCdtEdges { get; set; }

        internal bool IsPassable {
            get {
                return Target.IsTargetOfRouting || Source.IsSourceOfRouting ||
                       VisibilityEdge.IsPassable == null ||
                       VisibilityEdge.IsPassable();
            }
        }

        internal void AddOccupiedEdge() {
            numberOfPassedPaths++;
        }

        internal void RemoveOccupiedEdge() {
            numberOfPassedPaths--;
            Debug.Assert(numberOfPassedPaths >= 0);
        }
    }
}