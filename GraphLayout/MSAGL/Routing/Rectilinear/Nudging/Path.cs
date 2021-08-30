using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging {
    /// <summary>
    /// represents the path for an EdgeGeometry 
    /// </summary>
    internal class Path {
        /// <summary>
        /// the corresponding edge geometry
        /// </summary>
        internal EdgeGeometry EdgeGeometry { get; set; }
        /// <summary>
        /// the path points
        /// </summary>
        internal IEnumerable<Point> PathPoints { get; set; }

        internal double Width { get { return EdgeGeometry.LineWidth;} }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="edgeGeometry"></param>
        internal Path(EdgeGeometry edgeGeometry) {
            EdgeGeometry = edgeGeometry;
        }


        internal Point End {
            get {
                return LastEdge.Target;
            }
        }

        internal Point Start {
            get {
                return FirstEdge.Source;
            }
        }

        
        internal IEnumerable<PathEdge> PathEdges {
            get {
                for (var e = FirstEdge; e != null; e = e.Next)
                    yield return e;
            }
        }

        internal PathEdge FirstEdge { get; set; }

        internal PathEdge LastEdge { get; set; }


        internal void AddEdge(PathEdge edge) {
            edge.Path = this;
            Debug.Assert(edge.Source == LastEdge.Target);
            LastEdge.Next = edge;
            edge.Prev = LastEdge;
            LastEdge = edge;
        }

        internal void SetFirstEdge(PathEdge edge) {
            LastEdge = FirstEdge = edge;
            edge.Path = this;
        }
    
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}->{1}", this.EdgeGeometry.SourcePort.Location, this.EdgeGeometry.TargetPort.Location);
        }
    }
}