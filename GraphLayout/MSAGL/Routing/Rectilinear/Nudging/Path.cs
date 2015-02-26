/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        internal void SetIsFixedByFollowingParallelEdges() {
//            SetIsFixedByFollowingParallelEdgesOnEnumeration(PathEdges);
//            SetIsFixedByFollowingParallelEdgesOnEnumeration(PathEdgesInReverseOrder());
            
            //            if(GetFirstPathEdge().IsFixed)
//                foreach(var edge in ParallelEdgesStartingFromFirst())
//                    edge.IsFixed = true;
//            if (GetLastPathEdge().IsFixed)
//                foreach (var edge in ParallelEdgesStartingFromLast())
//                    edge.IsFixed = true;

        }

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