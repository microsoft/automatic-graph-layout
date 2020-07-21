using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.GraphmapsWithMesh
{
    class PlanarGraphUtilities
    {
        /*
         * input: a geometric straight line planar graph
         * output: a combinatorial adjacency list of the corresponding planar graph
         */

        public static void TransformToGeometricPlanarGraph(Tiling g)
        {
            for (int nodeIndex = 0; nodeIndex < g.NumOfnodes; nodeIndex++)
            {
                var sortedEdgeList = new Edge[g.DegList[nodeIndex]];
                var clockwiseAngularDistance = new double[g.DegList[nodeIndex]];

                //sort the neighbors according to the clockwise angular distance
                for (int neighborIndex = 0; neighborIndex < g.DegList[nodeIndex]; neighborIndex++)
                {
                    sortedEdgeList[neighborIndex] = g.EList[nodeIndex, neighborIndex];
                    Vertex apex = g.VList[nodeIndex];
                    Vertex referrence = g.VList[g.EList[nodeIndex, 0].NodeId];
                    Vertex current = g.VList[g.EList[nodeIndex, neighborIndex].NodeId];
                    clockwiseAngularDistance[neighborIndex] = Angle.getClockwiseAngle(apex, referrence, current);
                }
                Array.Sort(clockwiseAngularDistance, sortedEdgeList);
                for (int neighborIndex = 0; neighborIndex < g.DegList[nodeIndex]; neighborIndex++)
                    g.EList[nodeIndex, neighborIndex] = sortedEdgeList[neighborIndex];
            }
            g.isPlanar = true;

            for (int nodeIndex = 0; nodeIndex < g.NumOfnodes; nodeIndex++)
            {
                for (int neighborIndex = 0; neighborIndex < g.DegList[nodeIndex]; neighborIndex++)
                {
                    int neighbor = g.EList[nodeIndex, neighborIndex].NodeId;
                    bool consistent = false;
                    for (int neighborIndex2 = 0; neighborIndex2 < g.DegList[neighbor]; neighborIndex2++)
                        if (g.EList[neighbor, neighborIndex2].NodeId == nodeIndex) consistent = true;

                    if (!consistent)
                        System.Diagnostics.Debug.WriteLine("Beware! Graph is Not Consistent!");
                }
            }
        }

        /*
         * input: a planar graph and an edge a<---b
         * output: c, the neighbor next to b around a in anticlockwise order
         */
        public static int GetAnticlockwiseNextNeighbor(Tiling planarGraph, int currentVertexId, int neighborVertexId)
        {

            int nextNeighborId = -1;

            if (planarGraph.isPlanar)
            {
                for (int neighborIndex = 0; neighborIndex < planarGraph.DegList[currentVertexId]; neighborIndex++)
                {
                    if (planarGraph.EList[currentVertexId, neighborIndex].NodeId == neighborVertexId)
                    {
                        nextNeighborId = neighborIndex + 1 < planarGraph.DegList[currentVertexId] ? planarGraph.EList[currentVertexId, neighborIndex + 1].NodeId : planarGraph.EList[currentVertexId, 0].NodeId;
                        break;
                    }
                }
            }

            return nextNeighborId;
        }

        /*
         *input: a planar graph and an edge
         *output: a face that is incident to the right of this edge
         */

        public static List<Vertex> GetRightIncidentFace(Tiling gPlanar, Vertex givenTailVertex, Vertex givenHeadVertex, out bool degenerate)
        {
            degenerate = false;
            List<Vertex> face = new List<Vertex>();
            Vertex currentHead = givenHeadVertex, currentTail = givenTailVertex;

            do
            {
                int subsequentVertxId = GetAnticlockwiseNextNeighbor(gPlanar, currentHead.Id, currentTail.Id);
                currentTail = currentHead;
                currentHead = gPlanar.VList[subsequentVertxId];
                if (face.Contains(currentHead)) {
                    degenerate = true;
                    break;
                }
                face.Add(currentHead);
            } while (!(givenTailVertex.Id == currentTail.Id && givenHeadVertex.Id == currentHead.Id));
            return face;
        }

        /*
         * input: A geometric planar graph 
         * output: remove a long edge from each face if the face does not contain any real node
         */
        public static void RemoveLongEdgesFromThinFaces(Tiling gPlanar)
        {
            //you need to handle face one after another - since you are changing the adjacency list
            List<Face> faces = new List<Face>();
            for (int nodeIndex = 0; nodeIndex < gPlanar.NumOfnodes; nodeIndex++)
            {
                for (int neighborIndex = 0; neighborIndex < gPlanar.DegList[nodeIndex]; neighborIndex++)
                {
                    bool degenerate;
                    Vertex tailVertex = gPlanar.VList[nodeIndex];
                    Vertex headVertex = gPlanar.VList[gPlanar.EList[nodeIndex, neighborIndex].NodeId];
                    List<Vertex> boundary = GetRightIncidentFace(gPlanar, tailVertex, headVertex, out degenerate);
                    if (degenerate) continue; // Found a degenerate face!

                    //check whether if all the boundary vertices are junctions
                    if (AllBoundaryVerticesAreJunctions(gPlanar, boundary) == false) continue;

                    faces.Add(new Face(boundary));
                }
            }
            foreach (var face in faces)
            {
                //if not a valid boundary continue
                if (!FaceIsStillValid(gPlanar, face.boundary)) continue;
                //check whether the face is thin, and if so, find the longest edge in this face and remove it                      
                if (GetFacewidth(face.boundary) > gPlanar.thinness) continue;
                //searchFurther = 
                RemoveLongestEdge(gPlanar, face.boundary);
            }
            //}
        }

        public static bool FaceIsStillValid(Tiling g, List<Vertex> boundary)
        {
            Vertex[] b = boundary.ToArray();
            for (int index = 0; index < b.Length; index++)
            {
                if (index + 1 == b.Length)
                {
                    if (g.IsAnEdge(b[index].Id, b[0].Id) == false) return false;
                    continue;
                }
                if (g.IsAnEdge(b[index].Id, b[index + 1].Id) == false) return false;
            }
            return true;
        }

        public class Face
        {
            public List<Vertex> boundary;
            public Face(List<Vertex> b)
            {
                boundary = b;
            }
        }

        private static bool AllBoundaryVerticesAreJunctions(Tiling gPlanar, List<Vertex> face)
        {
            foreach (Vertex boundaryVertex in face)
                if (boundaryVertex.Id < gPlanar.N)
                {
                    return false;
                }
            return true;
        }

        private static double GetFacewidth(List<Vertex> face)
        {
            int index = 0;
            LineSegment[] segments = new LineSegment[face.Count];
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=340
            throw new InvalidOperationException();
#else
            Vertex[,] segmentVertices = new Vertex[face.Count, 2];
            Vertex initialVertex = null, oldVertex = null;
            Core.Geometry.Point u1, u2;
            foreach (var boundaryVertex in face)
            {
                if (oldVertex == null)
                {
                    initialVertex = oldVertex = boundaryVertex;
                    continue;
                }
                u1 = new Core.Geometry.Point(boundaryVertex.XLoc, boundaryVertex.YLoc);
                u2 = new Core.Geometry.Point(oldVertex.XLoc, oldVertex.YLoc);
                segmentVertices[index, 0] = boundaryVertex;
                segmentVertices[index, 1] = oldVertex;
                segments[index++] = new LineSegment(u1, u2);
                oldVertex = boundaryVertex;
            }
            u1 = new Core.Geometry.Point(oldVertex.XLoc, oldVertex.YLoc);
            u2 = new Core.Geometry.Point(initialVertex.XLoc, initialVertex.YLoc);
            segmentVertices[index, 0] = initialVertex;
            segmentVertices[index, 1] = oldVertex;
            segments[index] = new LineSegment(u1, u2);

            double facewidth = Double.MaxValue;
            for (int i = 0; i < face.Count; i++)
            {
                for (int j = 0; j < face.Count; j++)
                {
                    if (i == j) continue;
                    if (segments[i].Start == segments[j].Start || segments[i].End == segments[j].End ||
                        segments[i].End == segments[j].Start || segments[i].Start == segments[j].End) continue;

                    double width;
                    width = PointToSegmentDistance.GetDistance(segmentVertices[i, 0], segmentVertices[i, 1],
                        segmentVertices[j, 0]);
                    if (facewidth > width) facewidth = width;
                    width = PointToSegmentDistance.GetDistance(segmentVertices[i, 0], segmentVertices[i, 1],
                        segmentVertices[j, 1]);
                    if (facewidth > width) facewidth = width;
                    width = PointToSegmentDistance.GetDistance(segmentVertices[j, 0], segmentVertices[j, 1],
                        segmentVertices[i, 0]);
                    if (facewidth > width) facewidth = width;
                    width = PointToSegmentDistance.GetDistance(segmentVertices[j, 0], segmentVertices[j, 1],
                        segmentVertices[i, 1]);
                    if (facewidth > width) facewidth = width;

                }
            }
            return facewidth;
#endif
        }

        private static bool RemoveLongestEdge(Tiling gPlanar, List<Vertex> face)
        {

            double longestLength = -1, length;
            Vertex initialVertex = null, oldVertex = null;
            Vertex removeA = null, removeB = null;
            foreach (Vertex boundaryVertex in face)
            {
                if (oldVertex == null)
                {
                    initialVertex = oldVertex = boundaryVertex;
                    continue;
                }
                length = gPlanar.GetEucledianDist(boundaryVertex.Id, oldVertex.Id);
                if (longestLength < length)
                {
                    removeA = oldVertex;
                    removeB = boundaryVertex;
                    longestLength = length;
                }
                oldVertex = boundaryVertex;
            }
            length = gPlanar.GetEucledianDist(oldVertex.Id, initialVertex.Id);
            if (longestLength < length)
            {
                removeA = initialVertex;
                removeB = oldVertex;
                longestLength = length;
            }
            //remove the longest edge
            return gPlanar.RemoveEdge(removeA.Id, removeB.Id);
        }
    }
}
