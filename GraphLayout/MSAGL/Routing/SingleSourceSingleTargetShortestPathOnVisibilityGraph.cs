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
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing {
    internal class SingleSourceSingleTargetShortestPathOnVisibilityGraph {
        readonly VisibilityVertex source;
        readonly VisibilityVertex target;
        VisibilityGraph _visGraph;
        internal SingleSourceSingleTargetShortestPathOnVisibilityGraph(VisibilityGraph visGraph, VisibilityVertex sourceVisVertex, VisibilityVertex targetVisVertex) {
            _visGraph = visGraph;
            source = sourceVisVertex;
            target = targetVisVertex;
            source.Distance = 0;
        }


        /// <summary>
        /// Returns  a  path
        /// </summary>
        /// <returns>a path or null if the target is not reachable from the source</returns>
        internal IEnumerable<VisibilityVertex> GetPath(bool shrinkEdgeLength) {
            var pq = new GenericBinaryHeapPriorityQueue<VisibilityVertex>();
            source.Distance = 0;
            target.Distance = double.PositiveInfinity;
            pq.Enqueue(source, H(source));
            while (!pq.IsEmpty()) {
                double hu;
                var u = pq.Dequeue(out hu);
                if (hu >= target.Distance)
                    break;

                foreach (var e in u.OutEdges) {
                    if (PassableOutEdge(e)) {
                        var v = e.Target;
                        ProcessNeighbor(pq, u, e, v);
                    }
                }

                foreach (var e in u.InEdges) {
                    if (PassableInEdge(e)) {
                        var v = e.Source;
                        ProcessNeighbor(pq, u, e, v);
                    }
                }

            }
            return _visGraph.PreviosVertex(target) == null ? null : CalculatePath(shrinkEdgeLength);
        }

        bool PassableOutEdge(VisibilityEdge e) {
            return e.Source == source || e.Target == target || !IsForbidden(e);
        }

        bool PassableInEdge(VisibilityEdge e) {
            return e.Source == target || e.Target == source || !IsForbidden(e);
        }

        internal static bool IsForbidden(VisibilityEdge e) {
            return e.IsPassable != null && !e.IsPassable() || e is TollFreeVisibilityEdge;
        }

        void ProcessNeighbor(GenericBinaryHeapPriorityQueue<VisibilityVertex> pq, VisibilityVertex u, VisibilityEdge l, VisibilityVertex v) {
            var len = l.Length;
            var c = u.Distance + len;

            if (v != source && _visGraph.PreviosVertex(v) == null) {
                v.Distance = c;
                _visGraph.SetPreviousEdge(v, l);
                if (v != target)
                    pq.Enqueue(v, H(v));
            } else if (c < v.Distance) { //This condition should never hold for the dequeued nodes.
                //However because of a very rare case of an epsilon error it might!
                //In this case DecreasePriority will fail to find "v" and the algorithm will continue working.
                //Since v is not in the queue changing its .Distance will not influence other nodes.
                //Changing v.Prev is fine since we come up with the path with an insignificantly
                //smaller distance.
                v.Distance = c;
                _visGraph.SetPreviousEdge(v, l);
                if (v != target)
                    pq.DecreasePriority(v, H(v));
            }
        }

        double H(VisibilityVertex visibilityVertex) {
            return visibilityVertex.Distance + (visibilityVertex.Point - target.Point).Length;
        }

        IEnumerable<VisibilityVertex> CalculatePath(bool shrinkEdgeLength) {
            var ret = new List<VisibilityVertex>();
            var v = target;
            do {
                ret.Add(v);
                if (shrinkEdgeLength)
                    _visGraph.ShrinkLengthOfPrevEdge(v);

                v = _visGraph.PreviosVertex(v);
            } while (v != source);
            ret.Add(source);

            for (int i = ret.Count - 1; i >= 0; i--)
                yield return ret[i];
        }
    }
}
    
