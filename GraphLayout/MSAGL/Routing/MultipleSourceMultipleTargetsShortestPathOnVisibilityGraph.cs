using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing {
    
    internal class MultipleSourceMultipleTargetsShortestPathOnVisibilityGraph {
        //we are not using the A* algorithm since it does not make much sense for muliple targets
        //but we use the upper bound heuristic
        readonly IEnumerable<VisibilityVertex> sources;
        readonly Set<VisibilityVertex> targets;
        VisibilityVertex _current;
        VisibilityVertex closestTarget;
        double upperBound = double.PositiveInfinity;
        VisibilityGraph _visGraph;
        internal MultipleSourceMultipleTargetsShortestPathOnVisibilityGraph(IEnumerable<VisibilityVertex> sourceVisVertices,
                                                                         IEnumerable<VisibilityVertex> targetVisVertices, VisibilityGraph visibilityGraph)
        {
            _visGraph = visibilityGraph;
            visibilityGraph.ClearPrevEdgesTable();
            foreach (var v in visibilityGraph.Vertices())
            {
                v.Distance = Double.PositiveInfinity;
            }
            sources = sourceVisVertices;
            targets = new Set<VisibilityVertex>(targetVisVertices);

        }


        /// <summary>
        /// Returns  a  path
        /// </summary>
        /// <returns>a path or null if the target is not reachable from the source</returns>
        internal IEnumerable<VisibilityVertex> GetPath(){ 
            var pq = new GenericBinaryHeapPriorityQueue<VisibilityVertex>();
            foreach (var v in sources) {
                v.Distance = 0;
                pq.Enqueue(v, 0);
            }
            while (!pq.IsEmpty()) {
                _current = pq.Dequeue();
                if (targets.Contains(_current))
                    break;

                foreach (var e in _current.OutEdges.Where(PassableOutEdge))
                    ProcessNeighbor(pq, e, e.Target);

                foreach (var e in _current.InEdges.Where(PassableInEdge))
                    ProcessNeighbor(pq, e, e.Source);
            }

            return _visGraph.PreviosVertex(_current) == null ? null : CalculatePath();
        }

        bool PassableOutEdge(VisibilityEdge e) {
            return e.Source == sources || 
                targets.Contains(e.Target) || 
                !IsForbidden(e);
        }

        bool PassableInEdge(VisibilityEdge e) {
            return targets.Contains(e.Source) || e.Target == sources || !IsForbidden(e);
        }


        internal static bool IsForbidden(VisibilityEdge e) {
            return e.IsPassable != null && !e.IsPassable() || e is TollFreeVisibilityEdge;
        }

        void ProcessNeighbor(GenericBinaryHeapPriorityQueue<VisibilityVertex> pq, VisibilityEdge l,
                             VisibilityVertex v)
        {
            var len = l.Length;
            var c = _current.Distance + len;
            if (c >= upperBound)
                return;
            if (targets.Contains(v))
            {
                upperBound = c;
                closestTarget = v;
            }
            if (v != sources && _visGraph.PreviosVertex(v) == null)
            {
                v.Distance = c;
                _visGraph.SetPreviousEdge(v, l);
                pq.Enqueue(v, c);
            }
            else if (c < v.Distance)
            {
                //This condition should never hold for the dequeued nodes.
                //However because of a very rare case of an epsilon error it might!
                //In this case DecreasePriority will fail to find "v" and the algorithm will continue working.
                //Since v is not in the queue changing its .Distance will not mess up the queue.
                //Changing v.Prev is fine since we come up with a path with an insignificantly
                //smaller distance.
                v.Distance = c;
                _visGraph.SetPreviousEdge(v, l);
                pq.DecreasePriority(v, c);
            }
        }

        
        IEnumerable<VisibilityVertex> CalculatePath() {
            if (closestTarget == null)
                return null;
            var ret = new List<VisibilityVertex>();
            var v = closestTarget;
            do {
                ret.Add(v);
                v = _visGraph.PreviosVertex(v);
            } while (v.Distance > 0);
            ret.Add(v);

            ret.Reverse();
            return ret; 
        }
    }
}