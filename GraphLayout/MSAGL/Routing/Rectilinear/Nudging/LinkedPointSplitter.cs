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
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging {
    /// <summary>
    /// intersects a set of horizontal LinkedPoints with a set of vertical LinkedPoints
    /// </summary>
    internal class LinkedPointSplitter {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="horizontalPoints">no two horizontal segs overlap, but they can share an end point</param>
        /// <param name="verticalPoints">no two vertical segs overlap, but they can share an end point</param>
        internal LinkedPointSplitter(List<LinkedPoint> horizontalPoints, List<LinkedPoint> verticalPoints) {
            VerticalPoints = verticalPoints;
            HorizontalPoints=horizontalPoints;
        }

        List<LinkedPoint> HorizontalPoints { get; set; }

        List<LinkedPoint> VerticalPoints { get; set; }

        internal void SplitPoints() {
            if(VerticalPoints.Count==0 || HorizontalPoints.Count==0)
                return; //there will be no intersections
            InitEventQueue();
            ProcessEvents();
        }

        void ProcessEvents() {
            while (!Queue.IsEmpty()) {
                double z;
                var linkedPoint = Queue.Dequeue(out z);
                ProcessEvent(linkedPoint, z);
            }
        }


        void ProcessEvent(LinkedPoint linkedPoint, double z){
            if(ApproximateComparer.Close(linkedPoint.Next.Point.X, linkedPoint.Point.X))
                if(z==Low(linkedPoint))
                    ProcessLowLinkedPointEvent(linkedPoint);
                else
                    ProcessHighLinkedPointEvent(linkedPoint);
            else
                IntersectWithTree(linkedPoint);
        }

        void IntersectWithTree(LinkedPoint horizontalPoint) {
            double left, right;
            bool xAligned;
            Debug.Assert(ApproximateComparer.Close(horizontalPoint.Y,horizontalPoint.Next.Y));
            var y = horizontalPoint.Y;
            if(horizontalPoint.Point.X<horizontalPoint.Next.Point.X) {
                left = horizontalPoint.Point.X;
                right = horizontalPoint.Next.Point.X;
                xAligned=true;
            }else {
                right= horizontalPoint.Point.X;
                left = horizontalPoint.Next.Point.X;
                xAligned=false;
            }
            if(xAligned)
            for( var node = tree.FindFirst(p => left<= p.Point.X); 
                node!=null &&  node.Item.Point.X <= right ;
                node=tree.Next(node)) {
                var p = new Point(node.Item.Point.X, y );
                horizontalPoint = TrySplitHorizontalPoint(horizontalPoint, p, true);
                TrySplitVerticalPoint(node.Item,p);
            }else //xAligned==false
                for (var node = tree.FindLast(p => p.Point.X <= right);
                node != null && node.Item.Point.X >= left;
                node = tree.Previous(node)) {
                    var p = new Point(node.Item.Point.X, y);
                    horizontalPoint = TrySplitHorizontalPoint(horizontalPoint, p, false);
                    TrySplitVerticalPoint(node.Item, p);
                }
        }

        static void TrySplitVerticalPoint(LinkedPoint linkedPoint, Point point) {
            Debug.Assert(ApproximateComparer.Close(linkedPoint.X, linkedPoint.Next.X));
            if (Low(linkedPoint) + ApproximateComparer.DistanceEpsilon < point.Y && point.Y + ApproximateComparer.DistanceEpsilon < High(linkedPoint))
                linkedPoint.SetNewNext(point);
        }

        static LinkedPoint TrySplitHorizontalPoint(LinkedPoint horizontalPoint, Point point, bool xAligned) {
            Debug.Assert(ApproximateComparer.Close(horizontalPoint.Y, horizontalPoint.Next.Y));
            if (xAligned && horizontalPoint.X + ApproximateComparer.DistanceEpsilon < point.X &&
                point.X + ApproximateComparer.DistanceEpsilon < horizontalPoint.Next.X ||
                !xAligned && horizontalPoint.Next.X + ApproximateComparer.DistanceEpsilon < point.X &&
                point.X + ApproximateComparer.DistanceEpsilon < horizontalPoint.X) {
                horizontalPoint.SetNewNext(point);
                return horizontalPoint.Next;
            }
            return horizontalPoint;
        }

        void ProcessHighLinkedPointEvent(LinkedPoint linkedPoint) {
            tree.Remove(linkedPoint);
        }

        readonly RbTree<LinkedPoint> tree = new RbTree<LinkedPoint>((a, b) => a.Point.X.CompareTo(b.Point.X));

        void ProcessLowLinkedPointEvent(LinkedPoint linkedPoint) {
            tree.Insert(linkedPoint);
        }

        void InitEventQueue() {
            Queue = new GenericBinaryHeapPriorityQueue<LinkedPoint>();
            foreach (var vertPoint in VerticalPoints)
                Queue.Enqueue(vertPoint, Low(vertPoint));
            //a horizontal point will appear in the queue after a vertical point 
            // with the same coordinate low coorinate
            foreach (var horizPoint in HorizontalPoints)
                Queue.Enqueue(horizPoint, horizPoint.Point.Y);
        }

        static double Low(LinkedPoint vertPoint) {
            return Math.Min(vertPoint.Point.Y, vertPoint.Next.Point.Y);
        }

        static double High(LinkedPoint vertPoint) {
            return Math.Max(vertPoint.Point.Y, vertPoint.Next.Point.Y);
        }

        GenericBinaryHeapPriorityQueue<LinkedPoint> Queue { get; set; }
    }
}