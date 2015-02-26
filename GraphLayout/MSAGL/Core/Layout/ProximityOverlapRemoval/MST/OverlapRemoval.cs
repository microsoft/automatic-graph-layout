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
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MST {
    /// <summary>
    /// Overlap Removal using Minimum Spanning Tree on the delaunay triangulation. The edge weight corresponds to the amount of overlap between two nodes.
    /// </summary>
    public class OverlapRemoval : IOverlapRemoval {
        OverlapRemovalSettings Settings { get; set; }

        int lastRunNumberIterations;

        /// <summary>
        /// Settings to be used for the overlap removal, not all of them are used.
        /// </summary>
        /// <param name="settings"></param>
        public OverlapRemoval(OverlapRemovalSettings settings) {
            Settings = settings;
        }

        /// <summary>
        /// Removes the overlap with default settings.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="nodeSeparation"></param>
        public static void RemoveOverlaps(GeometryGraph graph, double nodeSeparation) {
            var settings = new OverlapRemovalSettings {
                RandomizeAllPointsOnStart = true,
                NodeSeparation = nodeSeparation
            };
            var mst = new OverlapRemoval(settings);
            mst.RemoveOverlap(graph);

        }

        /// <summary>
        /// Removes the overlap for the given graph.
        /// </summary>
        /// <param name="graph"></param>
        public void RemoveOverlap(GeometryGraph graph) {
            if (graph.Nodes.Count < 3) {
                RemoveOverlapsOnTinyGraph(graph);
                return;
            }

            Point[] nodePositions;
            Size[] nodeSizes;
            ProximityOverlapRemoval.InitNodePositionsAndBoxes(Settings,
                graph, out nodePositions,
                out nodeSizes);
            if (Settings.InitialScaling != InitialScaling.None)
                DoInitialScaling(graph, nodePositions, nodeSizes, InitialScaling.Inch72Pixel);

            lastRunNumberIterations = 0;
            while (OneIteration(nodePositions, nodeSizes, false)) {
                lastRunNumberIterations++;
//                if (lastRunNumberIterations%10 == 0)
//                    Console.Write("removing overlaps with cdt only {0},", lastRunNumberIterations);
            }
            //        Console.WriteLine();
            while (OneIteration(nodePositions, nodeSizes, true)) {
                lastRunNumberIterations++;
//                Console.Write("iterations with sweeping line {0},", lastRunNumberIterations);
            }

            Console.WriteLine();

            for (int i = 0; i < graph.Nodes.Count; i++) {
                graph.Nodes[i].Center = nodePositions[i];
            }
        }

        void RemoveOverlapsOnTinyGraph(GeometryGraph graph) {
            if (graph.Nodes.Count == 1)
                return;
            if (graph.Nodes.Count == 2) {
                var nodes = graph.Nodes.ToArray();
                var a = nodes[0];
                var b = nodes[1];

                if (ApproximateComparer.Close(a.Center, b.Center))
                    b.Center += new Point(0.001, 0);

                var idealDist = GetIdealDistanceBetweenTwoNodes(a, b);
                var center = (a.Center + b.Center)/2;
                var dir = (a.Center - b.Center);
                var dist = dir.Length;

                dir *= 0.5*idealDist/dist;

                a.Center = center + dir;
                b.Center = center - dir;

            }
        }

        double GetIdealDistanceBetweenTwoNodes(Node a, Node b) {
            var abox = a.BoundingBox;
            var bbox = b.BoundingBox;
            abox.Pad(Settings.NodeSeparation/2);
            bbox.Pad(Settings.NodeSeparation/2);
            var ac = abox.Center;
            var bc = bbox.Center;

            var ab = ac - bc;
            double dx = Math.Abs(ab.X);
            double dy = Math.Abs(ab.Y);
            double wx = (abox.Width/2 + bbox.Width/2);
            double wy = (abox.Height/2 + bbox.Height/2);
            const double machineAcc = 1.0e-16;
            double t;
            if (dx < machineAcc*wx)
                t = wy/dy;
            else if (dy < machineAcc*wy)
                t = wx/dx;
            else
                t = Math.Min(wx/dx, wy/dy);

            return t*ab.Length;
        }


#if ! SILVERLIGHT
        static void PrintTimeSpan(Stopwatch stopWatch) {
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;
            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds/10);
            Console.WriteLine(elapsedTime, "RunTime");
        }

#endif

        static double AvgEdgeLength(GeometryGraph graph) {
            int i = 0;
            double avgEdgeLength = 0;
            foreach (Edge edge in graph.Edges) {
                Point sPoint = edge.Source.Center;
                Point tPoint = edge.Target.Center;
                double euclid = (sPoint - tPoint).Length;
                avgEdgeLength += euclid;
                i++;
            }
            avgEdgeLength /= i;
            return avgEdgeLength;
        }


        /// <summary>
        /// Does one iterations in which a miniminum spanning tree is determined on the delaunay triangulation and finally the tree is exanded to resolve the overlap.
        /// </summary>
        /// <param name="nodePositions"></param>
        /// <param name="nodeSizes"></param>
        /// <param name="scanlinePhase"></param>
        /// <returns></returns>
        bool OneIteration(Point[] nodePositions, Size[] nodeSizes, bool scanlinePhase) {
            var cdt = new Cdt(nodePositions.Select((p, index) => Tuple.Create(p, (object) index)));
            cdt.Run();
            var siteIndex = new Dictionary<CdtSite, int>();
            for (int i = 0; i < nodePositions.Length; i++)
                siteIndex[cdt.PointsToSites[nodePositions[i]]] = i;

            int numCrossings = 0;
            List<Tuple<int, int, double, double, double>> proximityEdges =
                new List<Tuple<int, int, double, double, double>>();
            foreach (var site in cdt.PointsToSites.Values)
                foreach (var edge in site.Edges) {

                    Point point1 = edge.upperSite.Point;
                    Point point2 = edge.lowerSite.Point;
                    var nodeId1 = siteIndex[edge.upperSite];
                    var nodeId2 = siteIndex[edge.lowerSite];
                    Debug.Assert(ApproximateComparer.Close(point1, nodePositions[nodeId1]));
                    Debug.Assert(ApproximateComparer.Close(point2, nodePositions[nodeId2]));
                    var tuple = GetIdealEdgeLength(nodeId1, nodeId2, point1, point2, nodeSizes);
                    proximityEdges.Add(tuple);
                    if (tuple.Item3 > 1) numCrossings++;
                }


            if (numCrossings == 0 || scanlinePhase) {
                int additionalCrossings = FindProximityEdgesWithSweepLine(proximityEdges, nodeSizes, nodePositions);
                if (numCrossings == 0 && additionalCrossings == 0) {
//                    if(nodeSizes.Length>100)
//                    ShowAndMoveBoxesRemoveLater(null, proximityEdges, nodeSizes, nodePositions, -1);
                    return false;
                }

                if (numCrossings == 0 && !scanlinePhase) return false;
            }
            var treeEdges = MstOnDelaunayTriangulation.GetMstOnTuple(proximityEdges, nodePositions.Length);

            int rootId = treeEdges.First().Item1;

//            if (nodeSizes.Length > 100)
//                ShowAndMoveBoxesRemoveLater(treeEdges, proximityEdges, nodeSizes, nodePositions, rootId);

            MoveNodePositions(treeEdges, nodePositions, rootId);

            return true;
        }


        int FindProximityEdgesWithSweepLine(List<Tuple<int, int, double, double, double>> proximityEdges,
            Size[] nodeSizes, Point[] nodePositions) {
            MstLineSweeper mstLineSweeper = new MstLineSweeper(proximityEdges, nodeSizes, nodePositions);
            return mstLineSweeper.Run();
        }

/*
        /// <summary>
        /// Add additional proximity edges which where not found by the triangulation.
        /// </summary>
        /// <param name="proximityEdges">tuple representing an edge with more information: nodeId1, nodeId2,expandingFactor t, ideal distance, weight</param>
        /// <param name="nodeSizes"></param>
        /// <param name="nodePositions"></param>
        /// <returns></returns>
        int CreateProximityEdgesWithRTree(List<Tuple<int, int, double, double, double>> proximityEdges,
            Size[] nodeSizes, Point[] nodePositions) {
            HashSet<Tuple<int, int>> edgeSet = new HashSet<Tuple<int, int>>();

            foreach (var proximityEdge in proximityEdges) {
                edgeSet.Add(Tuple.Create(proximityEdge.Item1, proximityEdge.Item2));
            }
            RectangleNode<int> rootNode =
                RectangleNode<int>.CreateRectangleNodeOnEnumeration(
                    nodeSizes.Select(
                        (size, index) => new RectangleNode<int>(index, new Rectangle(size, nodePositions[index]))));
            int numCrossings = 0;
            RectangleNodeUtils.CrossRectangleNodes<int, int>(rootNode, rootNode,
                (a, b) => {
                    if (a == b) return;

                    var tuple = GetIdealEdgeLength
                        (
                            a, b,
                            nodePositions[a
                                ],
                            nodePositions[b
                                ],
                            nodeSizes);

                    Tuple<int, int> setTuple;
                    if (!(tuple.Item3 > 1) ||
                        edgeSet.Contains(setTuple = new Tuple<int, int>(tuple.Item1, tuple.Item2)))
                        return;
                    proximityEdges.Add(tuple);
                    edgeSet.Add(setTuple);
                    numCrossings++;
                });

            return numCrossings;
        }
*/


        /// <summary>
        /// Returns a tuple representing an edge with: nodeId1, nodeId2, t(overlapFactor), ideal distance, edge weight.
        /// </summary>
        /// <param name="nodeId1"></param>
        /// <param name="nodeId2"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="nodeSizes"></param>
        /// <returns></returns>
        internal static Tuple<int, int, double, double, double> GetIdealEdgeLength(int nodeId1, int nodeId2,
            Point point1,
            Point point2,
            Size[] nodeSizes) {
            double t;

            double idealDist = GetIdealEdgeLength(nodeId1, nodeId2, point1, point2, nodeSizes, out t);
            double length = (point1 - point2).Length;

            var box1 = new Rectangle(nodeSizes[nodeId1], point1);
            var box2 = new Rectangle(nodeSizes[nodeId2], point2);

            var distBox = GetDistanceRects(box1, box2);

            double weight;
            if (t > 1) //overlap
                weight = -(idealDist - length);
            else
                weight = distBox;
            int smallId = nodeId1;
            int bigId = nodeId2;
            if (nodeId1 > nodeId2) {
                smallId = nodeId2;
                bigId = nodeId1;
            }
            return Tuple.Create(smallId, bigId, t, idealDist, weight);

        }




        /// <summary>
        /// Returns the ideal edge length, such that the overlap is removed.
        /// </summary>
        /// <param name="nodeId1"></param>
        /// <param name="nodeId2"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="nodeBoxes"></param>
        /// <param name="tRes"></param>
        /// <returns></returns>
        static double GetIdealEdgeLength(int nodeId1, int nodeId2, Point point1, Point point2,
            Size[] nodeBoxes, out double tRes) {
            if (nodeBoxes == null) throw new ArgumentNullException("nodeBoxes");


            //            double expandMax = double.PositiveInfinity; //todo : this expands all the way
            const double expandMax = 1.5;
            const double expandMin = 1;

            //todo: replace machineAcc with global epsilon method in MSAGL
            const double machineAcc = 1.0e-16;
            double dist = (point1 - point2).Length;
            double dx = Math.Abs(point1.X - point2.X);
            double dy = Math.Abs(point1.Y - point2.Y);

            double wx = (nodeBoxes[nodeId1].Width/2 + nodeBoxes[nodeId2].Width/2);
            double wy = (nodeBoxes[nodeId1].Height/2 + nodeBoxes[nodeId2].Height/2);

            double t;
            if (dx < machineAcc*wx) {
                t = wy/dy;
            }
            else if (dy < machineAcc*wy) {
                t = wx/dx;
            }
            else {
                t = Math.Min(wx/dx, wy/dy);
            }

            if (t > 1) t = Math.Max(t, 1.001); // must be done, otherwise the convergence is very slow

            t = Math.Min(expandMax, t);
            t = Math.Max(expandMin, t);
            tRes = t;
            return t*dist;
        }


        /// <summary>
        /// Returns the distance between two given rectangles or zero if they intersect.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static double GetDistanceRects(Rectangle a, Rectangle b) {
            if (a.Intersects(b))
                return 0;

            double dx = 0, dy = 0;

            if (a.Right < b.Left) {
                dx = a.Left - b.Right;
            }
            else if (b.Right < a.Left) {
                dx = a.Left - b.Right;
            }

            if (a.Top < b.Bottom) {
                dy = b.Bottom - a.Top;
            }
            else if (b.Top < a.Bottom) {
                dy = a.Bottom - b.Top;
            }

            double euclid = Math.Sqrt(dx*dx + dy*dy);
            return euclid;
        }

#if DEBUG && !SILVERLIGHT && !SHARPKIT

    /// <summary>
    /// Shows the current state of the algorithm for debug purposes.
    /// </summary>
    /// <param name="treeEdges"></param>
    /// <param name="proximityEdges"></param>
    /// <param name="nodeSizes"></param>
    /// <param name="nodePos"></param>
    /// <param name="rootId"></param>
        void ShowAndMoveBoxesRemoveLater(List<Tuple<int, int, double, double, double>> treeEdges,
            List<Tuple<int, int, double, double, double>> proximityEdges, Size[] nodeSizes, Point[] nodePos, int rootId) {
            var l = new List<DebugCurve>();
            foreach (var tuple in proximityEdges)
                l.Add(new DebugCurve(100, 0.5, "black", new LineSegment(nodePos[tuple.Item1], nodePos[tuple.Item2])));
            //just for debug
            var nodeBoxes = new Rectangle[nodeSizes.Length];
            for (int i = 0; i < nodePos.Length; i++)
                nodeBoxes[i] = new Rectangle(nodeSizes[i], nodePos[i]);
            l.AddRange(nodeBoxes.Select(b => new DebugCurve(100, 0.3, "green", b.Perimeter())));
            if (treeEdges != null)
                l.AddRange(
                    treeEdges.Select(
                        e =>
                            new DebugCurve(200, GetEdgeWidth(e), "red",
                                new LineSegment(nodePos[e.Item1], nodePos[e.Item2]))));
            if (rootId >= 0)
                l.Add(new DebugCurve(100, 10, "blue", CurveFactory.CreateOctagon(30, 30, nodePos[rootId])));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        }
        static double GetEdgeWidth(Tuple<int, int, double, double, double> edge) {
            if (edge.Item3 > 1) //overlap
                return 6;
            return 2;
        }
#endif

        /// <summary>
        /// Lets the tree grow according to the ideal distances.
        /// </summary>
        /// <param name="treeEdges"></param>
        /// <param name="nodePositions"></param>
        /// <param name="rootNodeId"></param>
        static void MoveNodePositions(List<Tuple<int, int, double, double, double>> treeEdges, Point[] nodePositions,
            int rootNodeId) {
            var posOld = (Point[]) nodePositions.Clone();

            var visited = new Set<int>();
            visited.Insert(rootNodeId);
            for (int i = 0; i < treeEdges.Count; i++) {
                var tupleEdge = treeEdges[i];
                if (visited.Contains(tupleEdge.Item1))
                    MoveUpperSite(tupleEdge, nodePositions, posOld, visited);
                else {
                    Debug.Assert(visited.Contains(tupleEdge.Item2));
                    MoveLowerSite(tupleEdge, nodePositions, posOld, visited);
                }
            }

        }

        static void MoveUpperSite(Tuple<int, int, double, double, double> edge, Point[] posNew, Point[] oldPos,
            Set<int> visited) {
            double idealLen = edge.Item4;
            var dir = oldPos[edge.Item2] - oldPos[edge.Item1];
            var len = dir.Length;

            dir *= (idealLen/len + 0.01);
            int standingNode = edge.Item1;
            int movedNode = edge.Item2;
            posNew[movedNode] = posNew[standingNode] + dir;
            visited.Insert(movedNode);
        }

        static void MoveLowerSite(Tuple<int, int, double, double, double> edge, Point[] posNew, Point[] oldPos,
            Set<int> visited) {
            double idealLen = edge.Item4;
            var dir = -oldPos[edge.Item2] + oldPos[edge.Item1];
            var len = dir.Length;
            dir *= (idealLen/len + 0.01);
            var standingNode = edge.Item2;
            var movedNode = edge.Item1;
            posNew[movedNode] = posNew[standingNode] + dir;
            visited.Insert(movedNode);
        }

        /// <summary>
        /// Does the initial scaling of the layout, could also be avoided.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="nodePositions"></param>
        /// <param name="nodeSizes"></param>
        /// <param name="scalingMethod"></param>
        static void DoInitialScaling(GeometryGraph graph, Point[] nodePositions, Size[] nodeSizes,
            InitialScaling scalingMethod) {

            var avgEdgeLength = AvgEdgeLength(graph);
            double goalLength;
            if (scalingMethod == InitialScaling.Inch72Pixel)
                goalLength = 72;
            else if (scalingMethod == InitialScaling.AvgNodeSize)
                goalLength = nodeSizes.Average(box => (box.Width + box.Height)/2);
            else return;

            double scaling = goalLength/avgEdgeLength;
#if DEBUG
            Console.WriteLine("AvgEdgeLength Scaling Method: {0}, ScaleFactor={1:F2}", scalingMethod, scaling);
#endif
            for (int j = 0; j < nodePositions.Length; j++) {
                nodePositions[j] *= scaling;
            }
        }

        void IOverlapRemoval.Settings(OverlapRemovalSettings settings) {
            Settings = settings;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetLastRunIterations() {
            return lastRunNumberIterations;
        }

    }
}
