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
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Layout.LargeGraphLayout;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MST {
    internal class MstLineSweeper {
        readonly List<Tuple<int, int, double, double, double>> _proximityEdges;
        readonly Size[] _nodeSizes;
        readonly Point[] _nodePositions;
        IntervalRTree<int> _intervalTree;
        BinaryHeapPriorityQueue _q;
        int _numberOfOverlaps = 0;

        public MstLineSweeper(List<Tuple<int, int, double, double, double>> proximityEdges, Size[] nodeSizes, Point[] nodePositions) {
            _proximityEdges = proximityEdges;
            _nodeSizes = nodeSizes;
            _nodePositions = nodePositions;
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
                _intervalTree = new IntervalRTree<int>();

            _intervalTree.Add(interval, i);
            
        }

        void FindOverlapsWithInterval(int i) {
            if (_intervalTree == null)
                return;
            var interval = GetInterval(i);
            foreach (int j in _intervalTree.GetAllIntersecting(interval)) {
                var tuple = OverlapRemoval.GetIdealEdgeLength(i, j, 
                    _nodePositions[i], _nodePositions[j], _nodeSizes);

                if (!(tuple.Item3 > 1))
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