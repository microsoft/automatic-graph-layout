using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.GraphmapsWithMesh;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing
{
    internal class SingleSourceSingleTargetShortestPathOnVisibilityGraph
    {
        public Tiling _g;
        readonly VisibilityVertex _source;
        readonly VisibilityVertex _target;
        VisibilityGraph _visGraph;
        double _lengthMultiplier = 1;
        public double LengthMultiplier
        {
            get { return _lengthMultiplier; }
            set { _lengthMultiplier = value; }
        }

        double _lengthMultiplierForAStar = 1;
        public double LengthMultiplierForAStar
        {
            get { return _lengthMultiplierForAStar; }
            set { _lengthMultiplierForAStar = value; }
        }

        internal SingleSourceSingleTargetShortestPathOnVisibilityGraph(VisibilityGraph visGraph, VisibilityVertex sourceVisVertex, VisibilityVertex targetVisVertex, Tiling g)
        {
            _visGraph = visGraph;
            _source = sourceVisVertex;
            _target = targetVisVertex;
            _source.Distance = 0;
            _g = g;
        }
        internal SingleSourceSingleTargetShortestPathOnVisibilityGraph(VisibilityGraph visGraph, VisibilityVertex sourceVisVertex, VisibilityVertex targetVisVertex)
        {
            _visGraph = visGraph;
            _source = sourceVisVertex;
            _target = targetVisVertex;
            _source.Distance = 0;
        }

        /// <summary>
        /// Returns  a  path
        /// </summary>
        /// <returns>a path or null if the target is not reachable from the source</returns>
        internal IEnumerable<VisibilityVertex> GetPath(bool shrinkEdgeLength)
        {
            var pq = new GenericBinaryHeapPriorityQueue<VisibilityVertex>();

            _source.Distance = 0;
            _target.Distance = double.PositiveInfinity;
            pq.Enqueue(_source, H(_source));

            while (!pq.IsEmpty())
            {
                double hu;
                var u = pq.Dequeue(out hu);
                if (hu >= _target.Distance)
                    break;

                foreach (var e in u.OutEdges)
                {

                    if (PassableOutEdge(e))
                    {
                        var v = e.Target;
                        if (u != _source && u.isReal) ProcessNeighbor(pq, u, e, v, 1000);
                        else ProcessNeighbor(pq, u, e, v);
                    }
                }

                foreach (var e in u.InEdges)
                {
                    if (PassableInEdge(e))
                    {
                        var v = e.Source;
                        ProcessNeighbor(pq, u, e, v);
                    }
                }

            }
            return _visGraph.PreviosVertex(_target) == null
                ? null
                : CalculatePath(shrinkEdgeLength);
        }


        internal void AssertEdgesPassable(List<VisibilityEdge> path)
        {
            foreach (var edge in path)
            {
                Debug.Assert(PassableOutEdge(edge) || PassableInEdge(edge));
            }
        }

        bool PassableOutEdge(VisibilityEdge e)
        {
            return e.Source == _source || e.Target == _target || !IsForbidden(e);
        }

        bool PassableInEdge(VisibilityEdge e)
        {
            return e.Source == _target || e.Target == _source || !IsForbidden(e);
        }

        internal static bool IsForbidden(VisibilityEdge e)
        {
            return e.IsPassable != null && !e.IsPassable() || e is TollFreeVisibilityEdge;
        }


        void ProcessNeighbor(GenericBinaryHeapPriorityQueue<VisibilityVertex> pq, VisibilityVertex u, VisibilityEdge l, VisibilityVertex v, int penalty)
        {
            var len = l.Length + penalty;
            var c = u.Distance + len;

            if (v != _source && _visGraph.PreviosVertex(v) == null)
            {
                v.Distance = c;
                _visGraph.SetPreviousEdge(v, l);
                if (v != _target)
                {
                    pq.Enqueue(v, H(v));
                }
            }
            else if (v != _source && c < v.Distance)
            { //This condition should never hold for the dequeued nodes.
                //However because of a very rare case of an epsilon error it might!
                //In this case DecreasePriority will fail to find "v" and the algorithm will continue working.
                //Since v is not in the queue changing its .Distance will not influence other nodes.
                //Changing v.Prev is fine since we come up with the path with an insignificantly
                //smaller distance.
                var prevV = _visGraph.PreviosVertex(v);
                v.Distance = c;
                _visGraph.SetPreviousEdge(v, l);
                if (v != _target)
                    pq.DecreasePriority(v, H(v));
            }
        }

        void ProcessNeighbor(GenericBinaryHeapPriorityQueue<VisibilityVertex> pq, VisibilityVertex u, VisibilityEdge l, VisibilityVertex v)
        {
            var len = l.Length;
            var c = u.Distance + len;

            if (v != _source && _visGraph.PreviosVertex(v) == null)
            {
                v.Distance = c;
                _visGraph.SetPreviousEdge(v, l);
                if (v != _target)
                {
                    pq.Enqueue(v, H(v));
                }
            }
            else if (v != _source && c < v.Distance)
            { //This condition should never hold for the dequeued nodes.
                //However because of a very rare case of an epsilon error it might!
                //In this case DecreasePriority will fail to find "v" and the algorithm will continue working.
                //Since v is not in the queue changing its .Distance will not influence other nodes.
                //Changing v.Prev is fine since we come up with the path with an insignificantly
                //smaller distance.
                var prevV = _visGraph.PreviosVertex(v);
                v.Distance = c;
                _visGraph.SetPreviousEdge(v, l);
                if (v != _target)
                    pq.DecreasePriority(v, H(v));
            }
        }

        double H(VisibilityVertex visibilityVertex)
        {
            return visibilityVertex.Distance + (visibilityVertex.Point - _target.Point).Length * LengthMultiplierForAStar;
        }



        IEnumerable<VisibilityVertex> CalculatePath(bool shrinkEdgeLength)
        {
            var ret = new List<VisibilityVertex>();
            var v = _target;
            do
            {
                ret.Add(v);
                if (shrinkEdgeLength)
                    _visGraph.ShrinkLengthOfPrevEdge(v, LengthMultiplier);

                v = _visGraph.PreviosVertex(v);
            } while (v != _source);
            ret.Add(_source);

            for (int i = ret.Count - 1; i >= 0; i--)
                yield return ret[i];
        }
    }
}

