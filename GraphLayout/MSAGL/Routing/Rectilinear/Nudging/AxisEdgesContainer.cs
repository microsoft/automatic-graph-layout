using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging {
    /// <summary>
    /// keeps a list of active overlapping AxisEdges discovered during the sweep
    ///  </summary>
    internal class AxisEdgesContainer {
        readonly Set<AxisEdge> edges = new Set<AxisEdge>();
        internal IEnumerable<AxisEdge> Edges {get { return edges; } }
        /// <summary>
        /// it is not necessarely the upper point but some point above the source
        /// </summary>
        internal Point UpPoint;


        internal void AddEdge(AxisEdge edge){
            UpPoint = edge.TargetPoint;
            Debug.Assert(edges.Contains(edge)==false);
            edges.Insert(edge);
        }

        internal AxisEdgesContainer(Point source){
            Source = source;
        }

        public Point Source { get; set; }

        
        
        internal void RemoveAxis(AxisEdge edge){
            Debug.Assert(edges.Contains(edge));
            edges.Remove(edge);
        }

        internal bool IsEmpty(){
            return edges.Count == 0;
        }
    }
}