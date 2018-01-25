using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging{
    /// <summary>
    /// keeps a list of active overlapping AxisEdges discovered during the sweep
    ///  </summary>
    internal class AxisEdgesContainer:IEnumerable<AxisEdge>{
        readonly Set<AxisEdge> edges = new Set<AxisEdge>();
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

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<AxisEdge> GetEnumerator(){
            return edges.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator(){
            return GetEnumerator();
        }

        internal void RemoveAxis(AxisEdge edge){
            Debug.Assert(edges.Contains(edge));
            edges.Remove(edge);
        }

        internal bool IsEmpty(){
            return edges.Count == 0;
        }
    }
}