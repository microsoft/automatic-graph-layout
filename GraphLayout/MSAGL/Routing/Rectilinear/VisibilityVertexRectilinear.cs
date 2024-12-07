using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// This vertex class is used in rectilinear shortest paths
    /// </summary>
    public class VisibilityVertexRectilinear:VisibilityVertex {
        public VisibilityVertexRectilinear(Point point) : base(point) {}

        public VertexEntry[] VertexEntries { get; set; }
        
        public void SetVertexEntry(VertexEntry entry) {
            if (this.VertexEntries == null) {
                this.VertexEntries = new VertexEntry[4];
            }
            this.VertexEntries[CompassVector.ToIndex(entry.Direction)] = entry;
        }

        public void RemoveVertexEntries() {
            this.VertexEntries = null;
        }

    }
}   
