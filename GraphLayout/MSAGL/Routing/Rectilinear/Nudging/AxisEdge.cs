using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging {
    /// <summary>
    /// a wrapper arownd VisibilityEdge representing the same edge 
    /// but oriented along the X or the Y axis
    /// </summary>
    internal class AxisEdge:VisibilityEdge {
        internal Direction Direction { get; set; }
        internal AxisEdge(VisibilityVertex source, VisibilityVertex target)
            : base(source, target){
            RightBound = double.PositiveInfinity;
            LeftBound = double.NegativeInfinity;
            Direction = CompassVector.DirectionsFromPointToPoint(source.Point, target.Point);
            Debug.Assert(Direction == Direction.East || Direction == Direction.North);
        }

        readonly internal Set<AxisEdge> RightNeighbors = new Set<AxisEdge>();

        internal void AddRightNeighbor(AxisEdge edge) {
            RightNeighbors.Insert(edge);
        }

        internal double LeftBound { get; set; }

        internal double RightBound { get; private set; }

        readonly Set<LongestNudgedSegment> setOfLongestSegs = new Set<LongestNudgedSegment>();
        
        internal IEnumerable<LongestNudgedSegment> LongestNudgedSegments { get { return setOfLongestSegs; } }
        
        internal void AddLongestNudgedSegment(LongestNudgedSegment segment) {
            setOfLongestSegs.Insert(segment);
        }

        internal void BoundFromRight(double rightbound) {
            rightbound = Math.Max(rightbound, LeftBound);
            RightBound = Math.Min(rightbound, RightBound);
        }

        internal void BoundFromLeft(double leftbound) {
            leftbound = Math.Min(leftbound, RightBound);
            LeftBound = Math.Max(leftbound, LeftBound);
        }
    }
}