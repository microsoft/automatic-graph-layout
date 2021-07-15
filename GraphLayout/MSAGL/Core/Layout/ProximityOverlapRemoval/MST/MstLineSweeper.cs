using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Layout.LargeGraphLayout;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree {
    internal class MstLineSweeper {
        readonly List<OverlappedEdge> _proximityEdges;
        readonly Size[] _nodeSizes;
        readonly Point[] _nodePositions;
        readonly bool _forLayers;
        RTree<int, double> _intervalTree;
        BinaryHeapPriorityQueue _q;
        int _numberOfOverlaps = 0;

        public MstLineSweeper(List<OverlappedEdge> proximityEdges, Size[] nodeSizes, Point[] nodePositions, bool forLayers) {
            _proximityEdges = proximityEdges;
            _nodeSizes = nodeSizes;
            _nodePositions = nodePositions;
            _forLayers = forLayers;
            Debug.Assert(nodePositions.Length==nodeSizes.Length);
            _q = new BinaryHeapPriorityQueue(nodeSizes.Length*2); 
        }

        public int Run() {
            InitQueue();
            FindOverlaps();
            return _numberOfOverlaps;
        }

        void FindOverlaps() {
            while (_q.Count > 0) {
                int i = _q.Dequeue();
                if (i < _nodePositions.Length) {
                    FindOverlapsWithInterval(i);
                    AddIntervalToTree(i);
                }
                else {
                    i -= _nodePositions.Length;
                    RemoveIntervalFromTree(i);
                }
            }
        }

        void RemoveIntervalFromTree(int i) {
            _intervalTree.Remove(GetInterval(i), i);
        }

        void AddIntervalToTree(int i) {
            var interval = GetInterval(i);
            if (_intervalTree == null)
                _intervalTree = new RTree<int, double>();

            _intervalTree.Add(interval, i);
            
        }

        void FindOverlapsWithInterval(int i) {
            if (_intervalTree == null)
                return;
            var interval = GetInterval(i);
            foreach (int j in _intervalTree.GetAllIntersecting(interval)) {
                var tuple = GTreeOverlapRemoval.GetIdealEdge(i, j,
                    _nodePositions[i], _nodePositions[j], _nodeSizes, _forLayers);

                if (!(tuple.overlapFactor > 1))
                    return;
                _proximityEdges.Add(tuple);
                _numberOfOverlaps++;
            }
        }

        Interval GetInterval(int i) {
            var w = _nodeSizes[i].Width/2;
            var nodeCenterX = _nodePositions[i].X;
            return new Interval(nodeCenterX-w, nodeCenterX+w);
        }

        void InitQueue() {
            for (int i = 0; i < _nodeSizes.Length; i++) {
                var h = _nodeSizes[i].Height/2;
                var nodeCenterY = _nodePositions[i].Y;
                _q.Enqueue(i, nodeCenterY - h); // enqueue the bottom event
                _q.Enqueue(_nodeSizes.Length + i, nodeCenterY + h); // enqueue the top event
            }
        }
    }
}