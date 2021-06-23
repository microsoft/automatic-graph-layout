using System.Diagnostics;

using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear {
    internal class VertexEntry {
        /// <summary>
        /// A class that records an entry from a specific direction for a vertex.
        /// </summary>
        /// <param name="vertex">Vertex that this VertexEntry enters</param>
        /// <param name="prevEntry">The previous VertexEntry along this path; null for a path source</param>
        /// <param name="length">Length of the path up to this vertex</param>
        /// <param name="numberOfBends">Number of bends in the path up to this vertex</param>
        /// <param name="cost">Cost of the path up to this vertex</param>
        internal VertexEntry(VisibilityVertexRectilinear vertex, VertexEntry prevEntry, double length, int numberOfBends, double cost) {
            this.Vertex = vertex;
            this.Direction = (prevEntry != null) ? CompassVector.PureDirectionFromPointToPoint(prevEntry.Vertex.Point, vertex.Point) : Direction. None;
            this.ResetEntry(prevEntry, length, numberOfBends, cost);
        }

        internal void ResetEntry(VertexEntry prevEntry, double length, int numberOfBends, double cost) {
            // A new prevEntry using the same previous vertex but a different entry to that vertex is valid here;
            // e.g. we could have prevEntry from S, which in turn had a prevEntry from E, replaced by prevEntry from
            // S which has a prevEntry from S.
#if TEST_MSAGL
            if (this.PreviousEntry != null) {
                Debug.Assert(this.PreviousEntry.Vertex == prevEntry.Vertex, "Inconsistent prevEntry vertex");
                Debug.Assert(this.PreviousEntry.Direction != prevEntry.Direction, "Duplicate prevEntry direction");
                Debug.Assert(this.Direction == CompassVector.PureDirectionFromPointToPoint(this.PreviousEntry.Vertex.Point, this.Vertex.Point),
                        "Inconsistent entryDir");
            }
#endif // TEST_MSAGL
            this.PreviousEntry = prevEntry;
            this.Length = length;
            this.NumberOfBends = numberOfBends;
            this.Cost = cost;
        }

        /// <summary>
        /// Cost of the path up to this vertex
        /// </summary>
        internal double Cost { get; private set; }

        /// <summary>
        /// The vertex that this VertexEntry enters
        /// </summary>
        internal VisibilityVertexRectilinear Vertex { get; private set; }

        /// <summary>
        /// The vertex that this VertexEntry is entered from
        /// </summary>
        internal VisibilityVertexRectilinear PreviousVertex { get { return (this.PreviousEntry == null) ? null : this.PreviousEntry.Vertex; } }

        /// <summary>
        /// The direction of entry to the vertex for this path (i.e. the direction from PreviousVertex to this.Vertex).
        /// </summary>
        internal Direction Direction { get; private set; }

        /// <summary>
        /// The length of the path up to this vertex
        /// </summary>
        internal double Length { get; private set; }
        
        /// <summary>
        /// The number of bends in the path up to this vertex
        /// </summary>
        internal int NumberOfBends { get; private set; }

        /// <summary>
        /// The previous VertexEntry along this path; null for a path source.
        /// </summary>
        internal VertexEntry PreviousEntry { get; private set; }

        /// <summary>
        /// Indicates whether we are allowing further entries into this vertex from this direction.
        /// </summary>
        internal bool IsClosed { get; set; }

        /// <summary>
        /// </summary>
        public override string ToString() {
            return this.Vertex.Point + " " + this.Direction + " " + this.IsClosed + " " + this.Cost;
        }
    }
}