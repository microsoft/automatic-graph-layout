using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging {
    /// <summary>
    /// Represent a maximal straight segment of a path
    /// </summary>
    internal class LongestNudgedSegment : SegmentBase {


        internal LongestNudgedSegment(int variable) {
            Id = variable;
        }

        
        /// <summary>
        /// has to be North or East
        /// </summary>
        internal Directions CompassDirection { get; set; }

        //the segment can go only North or East independently of the edge directions
        readonly List<PathEdge> edges = new List<PathEdge>();
        Point start;
        Point end;
        internal override Point Start { get { return start; } }
        internal override Point End { get { return end; } }



        /// <summary>
        /// the list of edges holding the same offset and direction
        /// </summary>
        internal List<PathEdge> Edges {
            get { return edges; }
        }

        internal void AddEdge(PathEdge edge) {
            if (Edges.Count == 0) {
                var dir = (edge.Target - edge.Source).CompassDirection;
                switch (dir) {
                    case Core.Geometry.Directions.South:
                        dir = Core.Geometry.Directions.North;
                        break;
                    case Core.Geometry.Directions.West:
                        dir = Core.Geometry.Directions.East;
                        break;
                }
                CompassDirection = dir;
                start = edge.Source;
                end = edge.Source; //does not matter; it will be fixed immediately
            }

            switch (CompassDirection) {
                case Core.Geometry.Directions.North:
                    TryPointForStartAndEndNorth(edge.Source);
                    TryPointForStartAndEndNorth(edge.Target);
                    break;
                case Core.Geometry.Directions.East:
                    TryPointForStartAndEndEast(edge.Source);
                    TryPointForStartAndEndEast(edge.Target);
                    break;
            }
            Edges.Add(edge);
        }

        void TryPointForStartAndEndNorth(Point p) {
            if (p.Y < start.Y)
                start = p;
            else if (p.Y > end.Y)
                end = p;
        }

        void TryPointForStartAndEndEast(Point p) {
            if (p.X < start.X)
                start = p;
            else if (p.X > end.X)
                end = p;
        }

        protected CompassVector DirectionPerp { get; set; }

        bool isFixed;

        /// <summary>
        /// the segments constraining "this" from the right
        /// </summary>

        internal bool IsFixed {
            get { return isFixed; }
            set { isFixed = value; }
        }

        internal int Id = -1;
        

        /// <summary>
        /// the maximal width of the edges 
        /// </summary>
        public double Width {
            get { return edges.Max(edge => edge.Width); }
        }


        internal double GetLeftBound() {
            if (!IsFixed)
                return Edges.Max(edge => edge.AxisEdge.LeftBound);
            return CompassDirection == Core.Geometry.Directions.North ? Edges[0].Source.X : -Edges[0].Source.Y;
        }

        internal double GetRightBound() {
            if (!IsFixed)
                return Edges.Min(edge => edge.AxisEdge.RightBound);
            return Position();
        }

        double Position() {
            return CompassDirection == Core.Geometry.Directions.North ? Edges[0].Source.X : -Edges[0].Source.Y;
        }

        internal double IdealPosition {get;set;}
    }
}