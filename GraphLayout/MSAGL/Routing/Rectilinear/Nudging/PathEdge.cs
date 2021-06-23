using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging {
    /// <summary>
    /// A place holder for an edge in a path to keep it inside of a linked list representing a path.
    /// Each PathEdge belongs to only one path
    /// </summary>
    /// PathEdge passes through the AxisEdge that it points to but may go to the different direction.
    /// In the last case the PathEdge is marked as Reversed. Several PathEdges can share the same AxisEdge.
    internal class PathEdge {

        internal AxisEdge AxisEdge { get; set; }
        internal PathEdge Next { get; set; }
        internal PathEdge Prev { get; set; }
        internal double Width { get; set; }
        internal Path Path { get; set; }
        
#if TEST_MSAGL
        public override string ToString() {
            return Source + " " + Target;
        }
#endif


        internal PathEdge(AxisEdge edgeForNudging, double width) {
            AxisEdge = edgeForNudging;
            Width = width;
        }

   
        LongestNudgedSegment longestNudgedSegment;

        /// <summary>
        /// It is the offset of the edge from the underlying line segment 
        /// [VisibilityEdge.SourcePoint, VisibilityEdge.TargetPoint] in to the direction of the VisibilityEdge.Perpendicular.
        /// Offset holder is the same for the maximal parallel sequence of connected PathEdges
        /// </summary>
        internal LongestNudgedSegment LongestNudgedSegment {
            get { return longestNudgedSegment; }
            set { 
                longestNudgedSegment = value;
                if (longestNudgedSegment != null){
                    longestNudgedSegment.AddEdge(this);
                    AxisEdge.AddLongestNudgedSegment(longestNudgedSegment);
                }
            }
        }

        /// <summary>
        /// A fixed edge cannot be shifted from its visibility edge; offset is always 0.
        /// Such an edge can be, for example, a terminal edge going to a port. 
        /// </summary>
        internal bool IsFixed { get; set; }

        
        internal Point Source {
            get { return !Reversed? AxisEdge.SourcePoint: AxisEdge.TargetPoint; }
        }

        
        internal Point Target {
            get { return Reversed ? AxisEdge.SourcePoint : AxisEdge.TargetPoint; }
        }

       
        static internal bool VectorsAreParallel(Point a, Point b) {
            return ApproximateComparer.Close(a.X * b.Y - a.Y * b.X, 0);
        }

        public static bool EdgesAreParallel(PathEdge edge, PathEdge pathEdge) {
            return VectorsAreParallel(edge.AxisEdge.TargetPoint-edge.AxisEdge.SourcePoint,
                                      pathEdge.AxisEdge.TargetPoint-pathEdge.AxisEdge.SourcePoint);
        }

        internal Direction Direction { get { return Reversed ? CompassVector.OppositeDir(AxisEdge.Direction) : AxisEdge.Direction;} }
        /// <summary>
        /// if set to true then in the path the edge is reversed
        /// </summary>
        internal bool Reversed {get; set;}

        int index=-1;//not set yet

        /// <summary>
        /// the index of the edge in the order
        /// </summary>
        internal int Index {
            get { return index; }
            set { index = value; }
        }
    }
}