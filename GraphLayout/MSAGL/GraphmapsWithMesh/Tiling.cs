using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.GraphmapsWithMesh
{

    public class Tiling
    {
        public int NumOfnodes;
        public int NumOfnodesBeforeDetour;
        public int maxTheoreticalZoomLevel;

        public Dictionary<Microsoft.Msagl.Core.Layout.Edge, List<int>> pathList = new Dictionary<Microsoft.Msagl.Core.Layout.Edge, List<int>>();
        public Dictionary<int, List<Microsoft.Msagl.Core.Layout.Edge>> JunctionToEdgeList = new Dictionary<int, List<Microsoft.Msagl.Core.Layout.Edge>>();
        public Dictionary<Node, Point> nodeToLoc = new Dictionary<Node, Point>();


        public int[,] NodeMap;
        public int[] DegList;
        public Vertex[] VList;
        public Edge[,] EList;
        public double Maxweight;
        public int maxDeg;
        public int N;
        // TODO: Remove field as it is never used
        Component _sNet = null;
        readonly double[] _edgeNodeSeparation;
        readonly double _angularResolution;
        public bool isPlanar;
        public double thinness;

        public RTree<int, Point> nodeTree = new RTree<int, Point>();
        public RTree<int, Point> edgeTree = new RTree<int, Point>();
        

        public Tiling(int nodeCount, bool isGraph)
        {
            thinness = 2;
            _angularResolution = 0.3;
            NumOfnodes = N = nodeCount;
            maxDeg = 15;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=340
            throw new InvalidOperationException();
#else
            EList = new Edge[10 * N, maxDeg];
#endif
            VList = new Vertex[10 * N];
            DegList = new int[10 * N];
            _edgeNodeSeparation = new double[20];

            _edgeNodeSeparation[0] = 0.5;
            _edgeNodeSeparation[1] = 1;
            _edgeNodeSeparation[2] = 1;
            _edgeNodeSeparation[3] = 1;
            _edgeNodeSeparation[4] = 1;
            _edgeNodeSeparation[5] = 1;
            _edgeNodeSeparation[6] = 1;
            _edgeNodeSeparation[7] = 1;
        }


        public Tiling(int bound)
        {


            int m, n, k = 1; //node location and inddex
            N = bound;
            _angularResolution = 0.3;

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=340
            throw new InvalidOperationException();
#else
            NodeMap = new int[N, N];
            EList = new Edge[N * N, 20];
            VList = new Vertex[N * N];
            DegList = new int[N * N];
            _edgeNodeSeparation = new double[20];

            _edgeNodeSeparation[1] = 1;
            _edgeNodeSeparation[2] = 1;
            _edgeNodeSeparation[3] = 1;
            _edgeNodeSeparation[4] = 1;
            _edgeNodeSeparation[5] = 1;
            _edgeNodeSeparation[6] = 1;
            _edgeNodeSeparation[7] = 1;


            //create vertex list
            for (int y = 1; y < N; y++)
            {
                for (int x = 1; x < N - 1; x += 2)
                {
                    //create node at location m,n
                    m = x + (y + 1) % 2;
                    n = y;

                    VList[k] = new Vertex(m, n) { Id = k };

                    DegList[k] = 0;

                    //map location to the vertex index
                    NodeMap[m, n] = k;
                    k++;
                }

            }
            NumOfnodes = k - 1;

            //create edge list
            for (int index = 1; index < k; index++)
            {
                //find the current location
                m = VList[index].XLoc;
                n = VList[index].YLoc;

                //find the six neighbors


                //left                
                if (m - 2 > 0 && NodeMap[m - 2, n] > 0)
                {
                    DegList[index]++;
                    EList[index, DegList[index]] = new Edge(NodeMap[m - 2, n]);
                }

                //right                
                if (m + 2 < N && NodeMap[m + 2, n] > 0)
                {
                    DegList[index]++;
                    EList[index, DegList[index]] = new Edge(NodeMap[m + 2, n]);
                }

                //top-right                
                if (n + 1 < N && m + 1 < N && NodeMap[m + 1, n + 1] > 0)
                {
                    DegList[index]++;
                    EList[index, DegList[index]] = new Edge(NodeMap[m + 1, n + 1]);
                }
                //top-left                
                if (n + 1 < N && m - 1 > 0 && NodeMap[m - 1, n + 1] > 0)
                {
                    DegList[index]++;
                    EList[index, DegList[index]] = new Edge(NodeMap[m - 1, n + 1]);
                }
                //bottom-right                
                if (n - 1 > 0 && m + 1 < N && NodeMap[m + 1, n - 1] > 0)
                {
                    DegList[index]++;
                    EList[index, DegList[index]] = new Edge(NodeMap[m + 1, n - 1]);
                }
                //bottom-left                
                if (n - 1 > 0 && m - 1 > 0)
                {
                    DegList[index]++;
                    EList[index, DegList[index]] = new Edge(NodeMap[m - 1, n - 1]);
                }

            }
#endif
        }

        public int InsertVertex(int x, int y)
        {
            int index = NumOfnodes;
            VList[index] = new Vertex(x, y) { Id = index };
            NumOfnodes++;
            return index;
        }


        public int InsertVertexWithDuplicateCheck(int x, int y, Dictionary<Point, int> locationtoNode)
        {
            Point p = new Point(x, y);
            if (locationtoNode.ContainsKey(p))
                return locationtoNode[p];

            int index = NumOfnodes;
            VList[index] = new Vertex(x, y) { Id = index };
            locationtoNode.Add(p, index);
            NumOfnodes++;
            return index;
        }

        public void ComputeGridEdgeWeights()
        {
            Queue q = new Queue();
            //for each node, it it has a weight, then update edge weights 
            for (int index = 1; index <= NumOfnodes; index++)
            {
                if (VList[index].Weight == 0) continue;
                q.Enqueue(index);

                while (q.Count > 0)
                {
                    //take the current node
                    var currentNode = (int)q.Dequeue();
                    if (VList[currentNode].Visited) continue;
                    else VList[currentNode].Visited = true;
                    //for each neighbor of the current node
                    for (int neighb = 1; neighb <= DegList[currentNode]; neighb++)
                    {
                        var neighbor = EList[currentNode, neighb].NodeId;
                        //find an edge such that the target node is never visited; that is the edge has never been visited
                        if (VList[neighbor].Visited == false)
                        {
                            //BFS                            
                            q.Enqueue(neighbor);
                            //compute what would be the edge for the current edge
                            var temp = GetWeight(index, currentNode, neighbor, (int)VList[index].Weight);

                            if (temp < 0 || q.Count > 200)
                            {
                                q.Clear(); break;
                            }

                            //update the weight of the edge
                            EList[currentNode, neighb].Weight += temp;
                            //EList[currentNode, neighb].EDist = GetEucledianDist(currentNode, neighbor);

                            if (Maxweight < EList[currentNode, neighb].Weight)
                                Maxweight = EList[currentNode, neighb].Weight;

                            //find the reverse edge and update it
                            for (int r = 1; r <= DegList[neighbor]; r++)
                            {
                                if (EList[neighbor, r].NodeId == currentNode)
                                {
                                    EList[neighbor, r].Weight += temp;
                                    //EList[neighbor, r].EDist = GetEucledianDist(currentNode, neighbor);
                                }
                            }//endfor
                        }//endif                          
                    } //endfor
                }
                q.Clear();
                for (int j = 1; j <= NumOfnodes; j++) VList[j].Visited = false;
            }
        }
        public double GetWeight(int a, int b, int c, int w)
        {
            double d1 = Math.Sqrt((VList[a].XLoc - VList[b].XLoc) * (VList[a].XLoc - VList[b].XLoc) + (VList[a].YLoc - VList[b].YLoc) * (VList[a].YLoc - VList[b].YLoc));
            double d2 = Math.Sqrt((VList[a].XLoc - VList[c].XLoc) * (VList[a].XLoc - VList[c].XLoc) + (VList[a].YLoc - VList[c].YLoc) * (VList[a].YLoc - VList[c].YLoc));

            if (VList[a].Id == VList[b].Id) return 1000;


            //distribute around a disk of radious 
            double sigma = 15;// Math.Sqrt(w);
            w = 50;
            //return w - d;
            var w1 = w * (Math.Exp(-(d1 * d1 / (2 * sigma * sigma))) / (sigma * Math.Sqrt(2 * Math.PI)));
            var w2 = w * (Math.Exp(-(d2 * d2 / (2 * sigma * sigma))) / (sigma * Math.Sqrt(2 * Math.PI)));
            return (w1 + w2) / 2 + .0001;
        }
        public double GetEucledianDist(int a, int b)
        {
            return Math.Sqrt((VList[a].XLoc - VList[b].XLoc) * (VList[a].XLoc - VList[b].XLoc) + (VList[a].YLoc - VList[b].YLoc) * (VList[a].YLoc - VList[b].YLoc));
        }

        private Dictionary<int, int[]> allcandidates;

        public void buildCrossingCandidates()
        {
            allcandidates = new Dictionary<int, int[]>();
            for (int index = N; index < NumOfnodesBeforeDetour; index++)
            {
                Vertex w = VList[index];
                var p1 = new Microsoft.Msagl.Core.Geometry.Point(w.XLoc - 15, w.YLoc - 15);
                var p2 = new Microsoft.Msagl.Core.Geometry.Point(w.XLoc + 15, w.YLoc + 15);
                Microsoft.Msagl.Core.Geometry.Rectangle queryRectangle = new Microsoft.Msagl.Core.Geometry.Rectangle(
                    p1, p2);
                int[] candidateList = nodeTree.GetAllIntersecting(queryRectangle);
                allcandidates.Add(index, candidateList);
            }
        }

        public void MsaglMoveToMaximizeMinimumAngle()
        {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=340
            throw new InvalidOperationException();
#else
            int[,] listNeighbors = new int[20, 3];
            double[] d = new double[10];
            int a = 0, b = 0, mincostA = 0, mincostB = 0;
            bool localRefinementsFound = true;
            int iteration = 10;
            int offset = iteration * 2;

            while (localRefinementsFound && iteration > 0)
            {
                iteration--;
                localRefinementsFound = false;


                for (int index = N; index < NumOfnodes; index++)
                {
                    Vertex w = VList[index];

                    int numNeighbors = 0;
                    double profit = 0;

                    for (int k = 0; k < DegList[w.Id]; k++)
                    {
                        numNeighbors++;
                        listNeighbors[numNeighbors, 1] = EList[w.Id, k].NodeId;
                        listNeighbors[numNeighbors, 2] = k;
                    }

                    if (numNeighbors <= 1) continue;

                    for (int counter = 1; counter <= 9; counter++)
                    {
                        d[counter] = 0;

                        if (counter == 1) { a = 1; b = 1; }
                        if (counter == 2) { a = 0; b = 1; }
                        if (counter == 3) { a = -1; b = 1; }
                        if (counter == 4) { a = -1; b = 0; }
                        if (counter == 5) { a = -1; b = -1; }
                        if (counter == 6) { a = 0; b = -1; }
                        if (counter == 7) { a = 1; b = -1; }
                        if (counter == 8) { a = 1; b = 0; }
                        if (counter == 9) { a = 0; b = 0; }


                        for (int k = 1; k <= numNeighbors; k++)
                        {
                            double length = Math.Sqrt((w.XLoc + a - VList[listNeighbors[k, 1]].XLoc) *
                                          (w.XLoc + a - VList[listNeighbors[k, 1]].XLoc)
                                          +
                                          (w.YLoc + b - VList[listNeighbors[k, 1]].YLoc) *
                                          (w.YLoc + b - VList[listNeighbors[k, 1]].YLoc)
                                    );
                            if (length < 1)
                            {
                                mincostA = 0; mincostB = 0;
                                break;
                            }

                            // *try to maximize min angle
                            d[counter] = 3.1416;
                            for (int l = 1; l <= numNeighbors; l++)
                            {
                                if (l == k) continue;
                                d[counter] = Math.Min(d[counter],
                                    Angle.getAngleIfSmallerThanPIby2(new Vertex(w.XLoc + a, w.YLoc + b),
                                        VList[listNeighbors[k, 1]], VList[listNeighbors[l, 1]]));
                            }

                        }
                        // *try to maximize min angle
                        if (profit < d[counter])
                        {
                            profit = d[counter]; mincostA = a; mincostB = b;
                        }


                    }
                    if (!(mincostA == 0 && mincostB == 0))
                    {
                        w.XLoc += mincostA;
                        w.YLoc += mincostB;
                        if (GetNode(w.XLoc, w.YLoc) == -1 || MsaglGoodResolution(w, listNeighbors, numNeighbors, offset) == false || noCrossings(w) == false)
                        {
                            w.XLoc -= mincostA;
                            w.YLoc -= mincostB;
                        }
                        else
                        {
                            localRefinementsFound = true;
                        }
                    }

                }
            }
#endif
        }


        public bool noCrossingsHeuristics(Vertex w, int index)
        {
            int[] candidateList;
            allcandidates.TryGetValue(index, out candidateList);
            if (candidateList.Length == 0) return true;
            
            for (int q = 0; q < candidateList.Length; q++)
            {
                int i = candidateList[q];

                for (int j = 0; j < DegList[i]; j++)
                {
                    int k1 = EList[i, j].NodeId;
                    Microsoft.Msagl.Core.Geometry.Point a = new Microsoft.Msagl.Core.Geometry.Point(VList[i].XLoc, VList[i].YLoc);
                    Microsoft.Msagl.Core.Geometry.Point b = new Microsoft.Msagl.Core.Geometry.Point(VList[k1].XLoc, VList[k1].YLoc);
                    for (int l = 0; l < DegList[w.Id]; l++)
                    {
                        int k2 = EList[w.Id, l].NodeId;
                        Microsoft.Msagl.Core.Geometry.Point c = new Microsoft.Msagl.Core.Geometry.Point(w.XLoc, w.YLoc);
                        Microsoft.Msagl.Core.Geometry.Point d = new Microsoft.Msagl.Core.Geometry.Point(VList[k2].XLoc, VList[k2].YLoc);

                        if (w.Id == i || k2 == i || w.Id == k1 || k2 == k1) continue;
                        Microsoft.Msagl.Core.Geometry.Point intersectionPoint;
                        if (Microsoft.Msagl.Core.Geometry.Point.SegmentSegmentIntersection(a, b, c, d, out intersectionPoint))
                            return false;

                    }
                }
            }
            return true;
        }

        public bool noCrossings(Vertex w)
        {
            for (int i = 0; i < NumOfnodes; i++)
            {
                for (int j = 0; j < DegList[i]; j++)
                {
                    int k1 = EList[i, j].NodeId;
                    Microsoft.Msagl.Core.Geometry.Point a = new Microsoft.Msagl.Core.Geometry.Point(VList[i].XLoc, VList[i].YLoc);
                    Microsoft.Msagl.Core.Geometry.Point b = new Microsoft.Msagl.Core.Geometry.Point(VList[k1].XLoc, VList[k1].YLoc);
                    for (int l = 0; l < DegList[w.Id]; l++)
                    {
                        int k2 = EList[w.Id, l].NodeId;
                        Microsoft.Msagl.Core.Geometry.Point c = new Microsoft.Msagl.Core.Geometry.Point(w.XLoc, w.YLoc);
                        Microsoft.Msagl.Core.Geometry.Point d = new Microsoft.Msagl.Core.Geometry.Point(VList[k2].XLoc, VList[k2].YLoc);

                        if (w.Id == i || k2 == i || w.Id == k1 || k2 == k1) continue;
                        Microsoft.Msagl.Core.Geometry.Point intersectionPoint;
                        if (Microsoft.Msagl.Core.Geometry.Point.SegmentSegmentIntersection(a, b, c, d, out intersectionPoint))
                            return false;

                    }
                }
            }
            return true;
        }

        public bool noCrossings(Vertex w, Vertex w1, Vertex w2)
        {
            Microsoft.Msagl.Core.Geometry.Point c = new Microsoft.Msagl.Core.Geometry.Point(w1.XLoc, w1.YLoc);
            Microsoft.Msagl.Core.Geometry.Point d = new Microsoft.Msagl.Core.Geometry.Point(w2.XLoc, w2.YLoc);


            int minx, miny, maxx, maxy;
            int offset = 5;

            minx = Math.Min(w1.XLoc, w2.XLoc);
            maxx = Math.Max(w1.XLoc, w2.XLoc);
            miny = Math.Min(w1.YLoc, w2.YLoc);
            maxy = Math.Max(w1.YLoc, w2.YLoc);
                
            var p1 = new Point(minx-offset, miny-offset);
            var p2 = new Point(maxx+offset, maxy+offset);
            Rectangle queryRectangle = new Rectangle(p1, p2);
            int[] candidateList = nodeTree.GetAllIntersecting(queryRectangle);

            
            for (int q = 0; q < candidateList.Length; q++)
            {
                int i = candidateList[q];
            //for (int i = 0; i < NumOfnodes; i++)
            //{
                if (w.Id == i || w1.Id == i || w2.Id == i) continue;
                for (int j = 0; j < DegList[i]; j++)
                {
                    int k1 = EList[i, j].NodeId;
                    if (w1.Id == k1 || w2.Id == k1) continue;


                    Microsoft.Msagl.Core.Geometry.Point a = new Microsoft.Msagl.Core.Geometry.Point(VList[i].XLoc, VList[i].YLoc);
                    Microsoft.Msagl.Core.Geometry.Point b = new Microsoft.Msagl.Core.Geometry.Point(VList[k1].XLoc, VList[k1].YLoc);


                    Microsoft.Msagl.Core.Geometry.Point interestionPoint;

                    if (Microsoft.Msagl.Core.Geometry.Point.SegmentSegmentIntersection(a, b, c, d, out interestionPoint))
                        return false;

                }
            }
            return true;
        }

        public bool noCrossings(int[] r, int p, int q)
        {
            Vertex w1 = VList[p];
            Vertex w2 = VList[q];
            Point c = new Point(w1.XLoc, w1.YLoc);
            Point d = new Point(w2.XLoc, w2.YLoc);

                        
            Rectangle queryRectangle = new Rectangle(c, d);
            int[] candidateList = nodeTree.GetAllIntersecting(queryRectangle);

            for (int k  = 0; k < candidateList.Length; k++)
            {
                int i = candidateList[k];
            
            //for (int i = 0; i < NumOfnodes; i++)
            //{
                int index = 0;
                for (; index < r.Length; index++)
                    if (r[index] == i) break;

                if (index < r.Length || w1.Id == i || w2.Id == i) continue;
                for (int j = 0; j < DegList[i]; j++)
                {

                    int k1 = EList[i, j].NodeId;

                    index = 0;
                    for (; index < r.Length; index++)
                        if (r[index] == k1) break;

                    if (index < r.Length || w1.Id == k1 || w2.Id == k1) continue;


                    Point a = new Point(VList[i].XLoc, VList[i].YLoc);
                    Point b = new Point(VList[k1].XLoc, VList[k1].YLoc);


                    Point interestionPoint;

                    if (Point.SegmentSegmentIntersection(a, b, c, d, out interestionPoint))
                        return false;

                }
            }
            return true;
        }
 

        public bool Crossings(int p, int q)
        {
            Vertex w1 = VList[p];
            Vertex w2 = VList[q];
            Point c = new Point(w1.XLoc, w1.YLoc);
            Point d = new Point(w2.XLoc, w2.YLoc);


            Rectangle queryRectangle = new Rectangle(c, d);
            int[] candidateList = nodeTree.GetAllIntersecting(queryRectangle);

            for (int k = 0; k < candidateList.Length; k++)
            {
                int i = candidateList[k];
                 
                if( i == p || i == q) continue;

                for (int j = 0; j < DegList[i]; j++)
                {
                    int k1 = EList[i, j].NodeId;
                    
                    if (p == k1 || q == k1) continue;

                    Point a = new Point(VList[i].XLoc, VList[i].YLoc);
                    Point b = new Point(VList[k1].XLoc, VList[k1].YLoc);

                    Point interestionPoint;
                    if (Point.SegmentSegmentIntersection(a, b, c, d, out interestionPoint))
                        return true;

                }
            }
            return false;
        }
        public bool MsaglGoodResolution(Vertex w, int[,] listNeighbors, int numNeighbors, int offset)
        {
            for (int i = 1; i < numNeighbors; i++)
            {
                for (int j = i + 1; j <= numNeighbors; j++)
                {
                    //check for angular resolution   
                    if (Angle.getAngleIfSmallerThanPIby2(w, VList[listNeighbors[i, 1]], VList[listNeighbors[j, 1]]) < _angularResolution)
                    {
                        return false;
                    }
                }


                //check for distance
                double min_X = Math.Min(w.XLoc, VList[listNeighbors[i, 1]].XLoc) - offset;
                double min_Y = Math.Min(w.YLoc, VList[listNeighbors[i, 1]].YLoc) - offset;
                double Max_X = Math.Max(w.XLoc, VList[listNeighbors[i, 1]].XLoc) + offset;
                double Max_Y = Math.Max(w.YLoc, VList[listNeighbors[i, 1]].YLoc) + offset;
                Microsoft.Msagl.Core.Geometry.Point a = new Microsoft.Msagl.Core.Geometry.Point(min_X, min_Y);
                Microsoft.Msagl.Core.Geometry.Point b = new Microsoft.Msagl.Core.Geometry.Point(Max_X, Max_Y);

                Rectangle queryRectangle = new Rectangle(a, b);

                int[] candidateVertex = nodeTree.GetAllIntersecting(queryRectangle);

                //check for distance
                for (int index = 0; index < candidateVertex.Length; index++)
                {
                    Vertex z = VList[candidateVertex[index]];
                    if (z.Id == w.Id || z.Id == VList[listNeighbors[i, 1]].Id) continue;

                    //distance from z to w,i
                    if (PointToSegmentDistance.GetDistance(w, VList[listNeighbors[i, 1]], z) < _edgeNodeSeparation[0])
                    {
                        return false;
                    }
                }

            }
            return true;
        }

        public bool GoodResolution(Vertex w, int[,] listNeighbors, int numNeighbors, WeightedPoint[] pt, int numPoints)
        {
            for (int i = 1; i <= numNeighbors; i++)
            {
                for (int j = i + 1; j <= numNeighbors; j++)
                {
                    //check for angular resolution  


                    //if (Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2)) < 0.3)
                    if (Angle.getAngleIfSmallerThanPIby2(w, VList[listNeighbors[1, 1]], VList[listNeighbors[2, 1]]) < 0.5)
                    {
                        return false;
                    }
                }

                //check for distance
                foreach (var z in _sNet.V)
                {
                    if (z.Id == w.Id || z.Id == VList[listNeighbors[i, 1]].Id || z.Invalid) continue;

                    //distance from z to w,i

                    if (PointToSegmentDistance.GetDistance(w, VList[listNeighbors[i, 1]], z) < _edgeNodeSeparation[EList[w.Id, listNeighbors[i, 2]].Selected])
                    {
                        return false;
                    }
                }

            }
            return true;
        }
        public bool IsWellSeperated(Vertex w, int w1, int w2, WeightedPoint[] pt, int numPoints)
        {

            //add the edge if they are not very close to a point
            for (int index = 1; index <= numPoints; index++)
            {
                if (VList[pt[index].GridPoint].Invalid) continue;
                if ((pt[index].X == VList[w1].XLoc && pt[index].Y == VList[w1].YLoc)
                    || (pt[index].X == VList[w2].XLoc && pt[index].Y == VList[w2].YLoc)) continue;

                int k = 0;
                for (int neighb = 1; neighb <= DegList[w.Id]; neighb++)
                    if (EList[w.Id, neighb].NodeId == w1) { k = neighb; break; }

                if (PointToSegmentDistance.GetDistance(VList[w1], VList[w2], VList[pt[index].GridPoint]) < _edgeNodeSeparation[EList[w.Id, k].Selected])
                {
                    return false;
                }
            }


            //check for angular resolution at neighbor1
            for (int neighb = 1; neighb <= DegList[w1]; neighb++)
            {
                if (EList[w1, neighb].Used == 0) continue;
                if (EList[w1, neighb].NodeId == w.Id || EList[w1, neighb].NodeId == w2) continue;


                //if (Math.Abs(Math.Atan2(yDiff1 , xDiff1) - Math.Atan2(yDiff2 , xDiff2)) < 0.5 )
                if (Angle.getAngleIfSmallerThanPIby2(w, VList[w1], VList[w2]) < _angularResolution)
                {
                    return false;
                }
            }

            //check for angular resolution at neighbor2
            for (int neighb = 1; neighb <= DegList[w2]; neighb++)
            {
                if (EList[w2, neighb].Used == 0) continue;
                if (EList[w2, neighb].NodeId == w.Id || EList[w2, neighb].NodeId == w1) continue;

                float xDiff1 = VList[w2].XLoc - VList[w1].XLoc;
                float yDiff1 = VList[w2].YLoc - VList[w1].YLoc;

                float xDiff2 = VList[w2].XLoc - VList[EList[w2, neighb].NodeId].XLoc;
                float yDiff2 = VList[w2].YLoc - VList[EList[w2, neighb].NodeId].YLoc;

                if (Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2)) < _angularResolution)
                {
                    return false;
                }
            }
            return true;
        }



        public bool MsaglIsWellSeperated(Vertex w, int w1, int w2)
        {
            //check if you need to add or subtract some offset while you are doing the query
            var p1 = new Point(VList[w1].XLoc, VList[w1].YLoc);
            var p2 = new Point(VList[w2].XLoc, VList[w2].YLoc);
            Rectangle queryRectangle = new Rectangle(p1, p2);
            int[] candidateList = nodeTree.GetAllIntersecting(queryRectangle);

            //add the edge w1w2 if they are not very close to a point O(number of points inside rectangle w1 w2)
            for (int q = 0; q < candidateList.Length; q++)
            {
                int index = candidateList[q];

                //if vertex is one of the neighbors forget it
                if (VList[index].Invalid || DegList[index] == 0 || VList[index].Id == w1 || VList[index].Id == w2 || VList[index].Id == w.Id) continue;

                //otherwise find distance from index to w1w2                
                if (PointToSegmentDistance.GetDistance(VList[w1], VList[w2], VList[index]) < _edgeNodeSeparation[0])
                {
                    return false;
                }
            }

            //check for angular resolution at neighbor1 O(1)
            for (int neighb = 0; neighb < DegList[w1]; neighb++)
            {
                if (EList[w1, neighb].NodeId == w.Id || EList[w1, neighb].NodeId == w2) continue;
                if (Angle.getAngleIfSmallerThanPIby2(VList[w1], VList[w2], VList[EList[w1, neighb].NodeId]) < _angularResolution)
                    return false;
            }

            //check for angular resolution at neighbor2  O(1)
            for (int neighb = 0; neighb < DegList[w2]; neighb++)
            {
                if (EList[w2, neighb].NodeId == w.Id || EList[w2, neighb].NodeId == w1) continue;
                //if (Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2)) < AngularResolution)
                if (Angle.getAngleIfSmallerThanPIby2(VList[w2], VList[w1], VList[EList[w2, neighb].NodeId]) < _angularResolution)
                    return false;
            }

            return true;
        }


        public void MsaglRemoveDeg2(Dictionary<int, Node> idToNodes)
        {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=340
            throw new InvalidOperationException();
#else
            int[,] listNeighbors = new int[20, 3];

            bool localRefinementsFound = true;
            int iteration = 20;


            List<int> Deg2Vertices = new List<int>();
            for (int index = N; index < NumOfnodes; index++)
            {
                Vertex w = VList[index];
                 

                var numNeighbors = 0;
                //compute the deg of w
                for (int k = 0; k < DegList[w.Id]; k++)
                {
                    numNeighbors++;
                    listNeighbors[numNeighbors, 1] = EList[w.Id, k].NodeId;
                    listNeighbors[numNeighbors, 2] = k;
                }
                //if deg is 1 fix it
                if (numNeighbors == 1)
                    DegList[index] = 0;
                //if deg is 2 then add in the list
                if (numNeighbors == 2) Deg2Vertices.Add(index);
            }

            while (localRefinementsFound && iteration > 0)
            {
                iteration--;
                localRefinementsFound = false;
                foreach(int index in Deg2Vertices)// (int index = N; index < NumOfnodes; index++)
                {
                    Vertex w = VList[index];
                    
                    if(w.Invalid) continue;
                    var numNeighbors = 0;

                    for (int k = 0; k < DegList[w.Id]; k++)
                    { 
                        numNeighbors++;
                        listNeighbors[numNeighbors, 1] = EList[w.Id, k].NodeId;
                        listNeighbors[numNeighbors, 2] = k;
                    }

                    if (numNeighbors == 1)
                        DegList[index] = 0;

               
                    if (numNeighbors == 2)
                    {

                        var adjust = MsaglIsWellSeperated(w, listNeighbors[1, 1], listNeighbors[2, 1]);
                        adjust = adjust && noCrossings(w, VList[listNeighbors[1, 1]], VList[listNeighbors[2, 1]]);

                        if (adjust)
                        {
                            localRefinementsFound = true;
                            var selected = EList[index, listNeighbors[2, 2]].Selected;
                            var used = EList[index, listNeighbors[2, 2]].Used;
                            RemoveEdge(index, listNeighbors[1, 1]);
                            RemoveEdge(index, listNeighbors[2, 1]);

                            AddEdge(listNeighbors[1, 1], listNeighbors[2, 1], selected, used);

                            if (DegList[w.Id] == 0) w.Invalid = true;

                             
                        }
                         
                    }
                }
            }
            Deg2Vertices.Clear();
#endif
        }
        public void RemoveDeg2(WeightedPoint[] pt, int numPoints)
        {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=340
            throw new InvalidOperationException();
#else
            int[,] listNeighbors = new int[20, 3];

            bool localRefinementsFound = true;
            int iteration = 100;


            while (localRefinementsFound && iteration > 0)
            {
                iteration--;
                localRefinementsFound = false;
                foreach (Vertex w in _sNet.V)
                {
                    var numNeighbors = 0;
                    if (w.Weight > 0) continue;
                    for (int k = 1; k <= DegList[w.Id]; k++)
                    {
                        if (EList[w.Id, k].Used > 0)
                        {
                            numNeighbors++;
                            listNeighbors[numNeighbors, 1] = EList[w.Id, k].NodeId;
                            listNeighbors[numNeighbors, 2] = k;
                        }
                    }
                    if (numNeighbors <= 1)
                    {
                        w.CId = 0;
                        w.Invalid = true;
                    }
                    if (numNeighbors == 2)
                    {
                        var adjust = IsWellSeperated(w, listNeighbors[1, 1], listNeighbors[2, 1], pt, numPoints);

                        //dont remove if length is already large
                        //if (GetEucledianDist(w.Id, listNeighbors[1, 1]) > 10 ||
                        //GetEucledianDist(w.Id, listNeighbors[2, 1]) > 10)
                        //adjust = false;

                        if (adjust)
                        {
                            localRefinementsFound = true;

                            for (int j = 1; j <= DegList[listNeighbors[2, 1]]; j++)
                            {
                                if (EList[listNeighbors[2, 1], j].NodeId == w.Id)
                                {
                                    adjust = true;
                                    //check if it already exists in the neighbor list
                                    for (int check = 1; check <= DegList[listNeighbors[2, 1]]; check++)
                                        if (EList[listNeighbors[2, 1], check].NodeId == listNeighbors[1, 1])
                                        {
                                            EList[listNeighbors[2, 1], check].Selected = EList[w.Id, listNeighbors[2, 2]].Selected;
                                            EList[listNeighbors[2, 1], check].Used = EList[w.Id, listNeighbors[2, 2]].Used;
                                            EList[listNeighbors[2, 1], j].Selected = 0;
                                            EList[listNeighbors[2, 1], j].Used = 0;
                                            adjust = false;
                                        }

                                    if (adjust)
                                    {
                                        EList[listNeighbors[2, 1], j].NodeId = listNeighbors[1, 1];
                                        //eList[listNeighbors[2, 1], j].Selected = 8;
                                        //eList[listNeighbors[2, 1], j].Used = 8;
                                    }

                                }
                            }

                            for (int i = 1; i <= DegList[listNeighbors[1, 1]]; i++)
                            {
                                if (EList[listNeighbors[1, 1], i].NodeId == w.Id)
                                {
                                    adjust = true;
                                    //check if it already exists in the neighbor list
                                    for (int check = 1; check <= DegList[listNeighbors[1, 1]]; check++)
                                        if (EList[listNeighbors[1, 1], check].NodeId == listNeighbors[2, 1])
                                        {
                                            EList[listNeighbors[1, 1], check].Selected = EList[w.Id, listNeighbors[1, 2]].Selected;
                                            EList[listNeighbors[1, 1], check].Used = EList[w.Id, listNeighbors[1, 2]].Used;
                                            EList[listNeighbors[1, 1], i].Selected = 0;
                                            EList[listNeighbors[1, 1], i].Used = 0;
                                            adjust = false;
                                        }

                                    if (adjust)
                                    {
                                        EList[listNeighbors[1, 1], i].NodeId = listNeighbors[2, 1];
                                        //eList[listNeighbors[1, 1], i].Selected = 8;
                                        //eList[listNeighbors[1, 1], i].Used = 8;
                                    }
                                }
                            }



                            //delete old edges

                            EList[w.Id, listNeighbors[1, 2]].Selected = 0;
                            EList[w.Id, listNeighbors[1, 2]].Used = 0;
                            EList[w.Id, listNeighbors[2, 2]].Selected = 0;
                            EList[w.Id, listNeighbors[2, 2]].Used = 0;

                            //remove the vertex                            
                            w.Invalid = true;
                            w.CId = 0;

                        }
                    }
                }
            }
#endif
        }

        public void MsaglDetour(Dictionary<int, Node> idToNode, bool omit)
        {
            NumOfnodesBeforeDetour = NumOfnodes;

            if(omit) return;
            for (int index = 0; index < N; index++)
            {
                Vertex w = VList[index];
                Vertex[] list = new Vertex[10];
                int separation = 30;
                int neighbor;


#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=340
                throw new InvalidOperationException();
#else
                int[,] removelist = new int[10, 2];
                int[,] addlist = new int[10, 4];

                int remove = 0;
                int add = 0;
                int newnode = 0;

                for (neighbor = 0; neighbor < DegList[index]; neighbor++)
                {

                    int a = NumOfnodes;
                    Vertex b = VList[EList[index, neighbor].NodeId];



                    if (w.YLoc == b.YLoc && w.XLoc > b.XLoc)
                    {
                        int exists = GetNode(w.XLoc - separation, w.YLoc);
                        if (exists == -1)
                        {
                            VList[a] = new Vertex(w.XLoc - separation, w.YLoc) { Id = a };
                            removelist[remove, 0] = w.Id; removelist[remove, 1] = b.Id; remove++;
                            addlist[add, 0] = w.Id; addlist[add, 1] = a;
                            addlist[add, 2] = b.Id; addlist[add, 3] = a; add++;
                            list[newnode++] = VList[a];
                            NumOfnodes++;
                        }
                        else { list[newnode++] = VList[exists]; }
                    }

                    if (w.YLoc == b.YLoc && w.XLoc < b.XLoc)
                    {
                        int exists = GetNode(w.XLoc + separation, w.YLoc);
                        if (exists == -1)
                        {
                            VList[a] = new Vertex(w.XLoc + separation, w.YLoc) { Id = a };
                            removelist[remove, 0] = w.Id; removelist[remove, 1] = b.Id; remove++;
                            addlist[add, 0] = w.Id; addlist[add, 1] = a;
                            addlist[add, 2] = b.Id; addlist[add, 3] = a; add++;
                            list[newnode++] = VList[a];
                            NumOfnodes++;
                        }
                        else list[newnode++] = VList[exists];
                    }

                    if (w.XLoc == b.XLoc && w.YLoc > b.YLoc)
                    {
                        int exists = GetNode(w.XLoc, w.YLoc - separation);
                        if (exists == -1)
                        {
                            VList[a] = new Vertex(w.XLoc, w.YLoc - separation) { Id = a };
                            removelist[remove, 0] = w.Id; removelist[remove, 1] = b.Id; remove++;
                            addlist[add, 0] = w.Id; addlist[add, 1] = a;
                            addlist[add, 2] = b.Id; addlist[add, 3] = a; add++;
                            list[newnode++] = VList[a];
                            NumOfnodes++;
                        }
                        else list[newnode++] = VList[exists];
                    }

                    if (w.XLoc == b.XLoc && w.YLoc < b.YLoc)
                    {
                        int exists = GetNode(w.XLoc, w.YLoc + separation);
                        if (exists == -1)
                        {
                            VList[a] = new Vertex(w.XLoc, w.YLoc + separation) { Id = a };
                            removelist[remove, 0] = w.Id; removelist[remove, 1] = b.Id; remove++;
                            addlist[add, 0] = w.Id; addlist[add, 1] = a;
                            addlist[add, 2] = b.Id; addlist[add, 3] = a; add++;
                            list[newnode++] = VList[a];
                            NumOfnodes++;
                        }
                        else list[newnode++] = VList[exists];
                    }
                }

                for (int i = 0; i < remove; i++) RemoveEdge(removelist[i, 0], removelist[i, 1]);
                for (int i = 0; i < add; i++)
                {
                    AddEdge(addlist[i, 0], addlist[i, 1], 1, 0);
                    AddEdge(addlist[i, 2], addlist[i, 3]);
                }

                int removeA = 0, removeB = 0;
                for (int i = 0; i < newnode; i++)
                {
                    for (int j = i + 1; j < newnode; j++)
                    {
                        if (list[i] == null || list[j] == null) continue;
                        if (list[i].XLoc == list[j].XLoc || list[i].YLoc == list[j].YLoc) continue;
                        if (AddEdge(list[i].Id, list[j].Id))
                        {
                            removeA = list[i].Id;
                            removeB = list[j].Id;
                        }
                    }
                }
                //remove one edge
                if (removeA + removeB > 0) RemoveEdge(removeA, removeB);
#endif
            }
        }

        public void CreateNodeTreeEdgeTree()
        {
            nodeTree.Clear();
            for (int index = 0; index < NumOfnodes; index++)
            {
                if (DegList[index] > 0)
                    nodeTree.Add(new Rectangle(new Point(VList[index].XLoc, VList[index].YLoc)), index);
            }

            edgeTree.Clear();
        }

        public void ComputeDetourAroundVertex(WeightedPoint[] pt, int numPoints)
        {
            for (int i = numPoints; i >= 1; i--)
            {
                var x = pt[i].X;
                var y = pt[i].Y;
                int neighb;

                if (x + 1 < N && y + 1 < N && NodeMap[x + 1, y + 1] > 0 &&
                    x + 2 < N && y < N && NodeMap[x + 2, y] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x + 1, y + 1]]; neighb++)
                    {
                        if (EList[NodeMap[x + 1, y + 1], neighb].NodeId == VList[NodeMap[x + 2, y]].Id)
                        {
                            SelectEdge(EList, DegList, VList[NodeMap[x + 1, y + 1]], VList[EList[NodeMap[x + 1, y + 1], neighb].NodeId], 6);
                            break;
                        }
                    }
                }

                if (x + 1 > 0 && y - 1 > 0 && NodeMap[x + 1, y - 1] > 0 &&
                    x + 2 < N && y < N && NodeMap[x + 2, y] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x + 1, y - 1]]; neighb++)
                    {
                        if (EList[NodeMap[x + 1, y - 1], neighb].NodeId == VList[NodeMap[x + 2, y]].Id)
                        {
                            SelectEdge(EList, DegList, VList[NodeMap[x + 1, y - 1]], VList[EList[NodeMap[x + 1, y - 1], neighb].NodeId], 6);
                            break;
                        }
                    }
                }

                if (x - 1 > 0 && y - 1 > 0 && NodeMap[x - 1, y - 1] > 0 &&
                    x - 2 > 0 && y > 0 && NodeMap[x - 2, y] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x - 1, y - 1]]; neighb++)
                    {
                        if (EList[NodeMap[x - 1, y - 1], neighb].NodeId == VList[NodeMap[x - 2, y]].Id)
                        {
                            SelectEdge(EList, DegList, VList[NodeMap[x - 1, y - 1]], VList[EList[NodeMap[x - 1, y - 1], neighb].NodeId], 6);
                            break;
                        }
                    }
                }

                if (x - 1 > 0 && y + 1 < N && NodeMap[x - 1, y + 1] > 0 &&
                    x - 2 > 0 && y > 0 && NodeMap[x - 2, y] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x - 1, y + 1]]; neighb++)
                    {
                        if (EList[NodeMap[x - 1, y + 1], neighb].NodeId == VList[NodeMap[x - 2, y]].Id)
                        {
                            SelectEdge(EList, DegList, VList[NodeMap[x - 1, y + 1]], VList[EList[NodeMap[x - 1, y + 1], neighb].NodeId], 6);
                            break;
                        }
                    }
                }

                if (x - 1 > 0 && y + 1 < N && NodeMap[x - 1, y + 1] > 0 &&
                   x + 1 < N && y + 1 < N && NodeMap[x + 1, y + 1] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x - 1, y + 1]]; neighb++)
                    {
                        if (EList[NodeMap[x - 1, y + 1], neighb].NodeId == VList[NodeMap[x + 1, y + 1]].Id)
                        {
                            SelectEdge(EList, DegList, VList[NodeMap[x - 1, y + 1]], VList[EList[NodeMap[x - 1, y + 1], neighb].NodeId], 6);
                            break;
                        }
                    }
                }
            }
        }

        public void ComputeShortcutMesh(WeightedPoint[] pt, int numPoints)
        {
            //COMPUTE NEIGHBORHOOD SHORTCUTS
            for (int i = numPoints; i >= 1; i--)
            {
                var x = pt[i].X;
                var y = pt[i].Y;

                //if v_i has a neighbor in the first (top right) quadrant 
                int neighb;
                while (x + 1 < N && y + 1 < N && NodeMap[x + 1, y + 1] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y + 1]].Id) break;
                    }
                    if (EList[NodeMap[x, y], neighb].Selected == 0) break;
                    x = x + 1;
                    y = y + 1;
                }
                while (x + 1 < N && y + 1 < N && NodeMap[x + 1, y + 1] > 0 && VList[NodeMap[x + 1, y + 1]].CId == 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y + 1]].Id) break;
                    }
                    SelectEdge(EList, DegList, VList[NodeMap[x, y]], VList[EList[NodeMap[x, y], neighb].NodeId], 6);
                    x = x + 1;
                    y = y + 1;
                    VList[NodeMap[x, y]].CId = 1;
                    _sNet.V.Add(VList[NodeMap[x, y]]);
                }
                if (x + 1 < N && y + 1 < N && NodeMap[x + 1, y + 1] > 0 && VList[NodeMap[x + 1, y + 1]].CId > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y + 1]].Id) break;
                    }
                    SelectEdge(EList, DegList, VList[NodeMap[x, y]], VList[EList[NodeMap[x, y], neighb].NodeId], 6);
                    x = x + 1;
                    y = y + 1;
                    VList[NodeMap[x, y]].CId = 1;
                    _sNet.AddVertex(VList[NodeMap[x, y]]);
                }

                x = pt[i].X;
                y = pt[i].Y;

                //if v_i has a neighbor in the top left quadrant 
                while (x - 1 > 0 && y + 1 < N && NodeMap[x - 1, y + 1] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y + 1]].Id) break;
                    }
                    if (EList[NodeMap[x, y], neighb].Selected == 0) break;
                    x = x - 1;
                    y = y + 1;
                }
                while (x - 1 > 0 && y + 1 < N && NodeMap[x - 1, y + 1] > 0 && VList[NodeMap[x - 1, y + 1]].CId == 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y + 1]].Id) break;
                    }
                    SelectEdge(EList, DegList, VList[NodeMap[x, y]], VList[EList[NodeMap[x, y], neighb].NodeId], 6);
                    x = x - 1;
                    y = y + 1;
                    VList[NodeMap[x, y]].CId = 1;
                    _sNet.AddVertex(VList[NodeMap[x, y]]);
                }
                if (x - 1 > 0 && y + 1 < N && NodeMap[x - 1, y + 1] > 0 && VList[NodeMap[x - 1, y + 1]].CId > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y + 1]].Id) break;
                    }
                    SelectEdge(EList, DegList, VList[NodeMap[x, y]], VList[EList[NodeMap[x, y], neighb].NodeId], 6);
                    x = x - 1;
                    y = y + 1;
                    VList[NodeMap[x, y]].CId = 1;
                    _sNet.AddVertex(VList[NodeMap[x, y]]);
                }

                x = pt[i].X;
                y = pt[i].Y;

                //if v_i has a neighbor in the bottom right quadrant 
                while (x + 1 < N && y - 1 > 0 && NodeMap[x + 1, y - 1] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y - 1]].Id) break;
                    }
                    if (EList[NodeMap[x, y], neighb].Selected == 0) break;
                    x = x + 1;
                    y = y - 1;
                }
                while (x + 1 < N && y - 1 > 0 && NodeMap[x + 1, y - 1] > 0 && VList[NodeMap[x + 1, y - 1]].CId == 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y - 1]].Id) break;
                    }
                    SelectEdge(EList, DegList, VList[NodeMap[x, y]], VList[EList[NodeMap[x, y], neighb].NodeId], 6);
                    x = x + 1;
                    y = y - 1;
                    VList[NodeMap[x, y]].CId = 1;
                    _sNet.AddVertex(VList[NodeMap[x, y]]);
                }
                if (x + 1 < N && y - 1 > 0 && NodeMap[x + 1, y - 1] > 0 && VList[NodeMap[x + 1, y - 1]].CId > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y - 1]].Id) break;
                    }
                    SelectEdge(EList, DegList, VList[NodeMap[x, y]], VList[EList[NodeMap[x, y], neighb].NodeId], 6);
                    x = x + 1;
                    y = y - 1;
                    VList[NodeMap[x, y]].CId = 1;
                    _sNet.AddVertex(VList[NodeMap[x, y]]);
                }

                x = pt[i].X;
                y = pt[i].Y;

                //if v_i has a neighbor in the bottom-left quadrant 
                while (x - 1 > 0 && y - 1 > 0 && NodeMap[x - 1, y - 1] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y - 1]].Id) break;
                    }
                    if (EList[NodeMap[x, y], neighb].Selected == 0) break;
                    x = x - 1;
                    y = y - 1;
                }
                while (x - 1 > 0 && y - 1 > 0 && NodeMap[x - 1, y - 1] > 0 && VList[NodeMap[x - 1, y - 1]].CId == 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y - 1]].Id) break;
                    }
                    SelectEdge(EList, DegList, VList[NodeMap[x, y]], VList[EList[NodeMap[x, y], neighb].NodeId], 6);
                    x = x - 1;
                    y = y - 1;
                    VList[NodeMap[x, y]].CId = 1;
                    _sNet.AddVertex(VList[NodeMap[x, y]]);
                }
                if (x - 1 > 0 && y - 1 > 0 && NodeMap[x - 1, y - 1] > 0 && VList[NodeMap[x - 1, y - 1]].CId > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (EList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y - 1]].Id) break;
                    }
                    SelectEdge(EList, DegList, VList[NodeMap[x, y]], VList[EList[NodeMap[x, y], neighb].NodeId], 6);
                    x = x - 1;
                    y = y - 1;
                    VList[NodeMap[x, y]].CId = 1;
                    _sNet.AddVertex(VList[NodeMap[x, y]]);
                }

            }
        }
        public int SelectEdge(Edge[,] eList, int[] degList, Vertex a, Vertex b, int givenLevel)
        {
            int temp = givenLevel;
            for (int neighb = 1; neighb <= degList[a.Id]; neighb++)
            {
                if (eList[a.Id, neighb].NodeId == b.Id)
                {
                    if (eList[a.Id, neighb].Selected == 0)
                    {
                        eList[a.Id, neighb].Selected = givenLevel;
                    }
                    else temp = eList[a.Id, neighb].Selected;
                    break;
                }
            }
            for (int neighb = 1; neighb <= degList[b.Id]; neighb++)
            {
                if (eList[b.Id, neighb].NodeId == a.Id)
                {
                    if (eList[b.Id, neighb].Selected == 0)
                    {
                        eList[b.Id, neighb].Selected = givenLevel;
                    }
                    else temp = eList[b.Id, neighb].Selected;
                    break;
                }
            }
            return temp;
        }

        public bool AddEdge(int a, int b)
        {
            for (int index = 0; index < DegList[a]; index++)
            {
                if (EList[a, index].NodeId == b) return false;
            }
            for (int index = 0; index < DegList[b]; index++)
            {
                if (EList[b, index].NodeId == a) return false;
            }
            EList[a, DegList[a]] = new Edge(b);
            DegList[a]++;
            EList[b, DegList[b]] = new Edge(a);
            DegList[b]++;
            return true;
        }

        public bool IsAnEdge(int a, int b)
        {
            for (int index = 0; index < DegList[a]; index++)
            {
                if (EList[a, index].NodeId == b) return true;
            }
            return false;
        }

        public bool AddEdge(int a, int b, int select, int zoomLevel)
        {
            for (int index = 0; index < DegList[a]; index++)
            {
                if (EList[a, index].NodeId == b) return false;
            }
            for (int index = 0; index < DegList[b]; index++)
            {
                if (EList[b, index].NodeId == a) return false;
            }
            EList[a, DegList[a]] = new Edge(b) { Selected = select, Used = zoomLevel };
            DegList[a]++;
            EList[b, DegList[b]] = new Edge(a) { Selected = select, Used = zoomLevel };
            DegList[b]++;
            return true;
        }
        public int GetNode(int a, int b)
        {
            for (int index = 0; index < NumOfnodes; index++)
                if (a == VList[index].XLoc && b == VList[index].YLoc && VList[index].Invalid == false) return index;
            return -1;
        }
        public int GetNodeOtherthanThis(int givenNodeId, int a, int b)
        {
            for (int index = 0; index < NumOfnodes; index++)
                if (a == VList[index].XLoc && b == VList[index].YLoc && VList[index].Invalid == false && index != givenNodeId) return index;
            return -1;
        }
        public int GetNodeExceptTheGivenNode(Vertex w, int a, int b, int offset)
        {
            Microsoft.Msagl.Core.Geometry.Point p1 = new Microsoft.Msagl.Core.Geometry.Point(a - offset, b - offset);
            Microsoft.Msagl.Core.Geometry.Point p2 = new Microsoft.Msagl.Core.Geometry.Point(a + offset, b + offset);
            Rectangle queryRectangle = new Rectangle(p1, p2);
            int[] candidateList = nodeTree.GetAllIntersecting(queryRectangle);
            for (int index = 0; index < candidateList.Length; index++)
            {
                int candidate = candidateList[index];
                if (w.Id != candidate && a == VList[candidate].XLoc && b == VList[candidate].YLoc &&
                    VList[candidate].Invalid == false) return candidate;
            }
            return -1;
        }
        public bool RemoveEdge(int a, int b)
        {
            int i = 0;
            for (int index = 0; index < DegList[a]; index++)
            {
                if (EList[a, index].NodeId == b)
                {
                    DegList[a]--;
                    i++;
                    for (; index < DegList[a]; index++) { EList[a, index] = EList[a, index + 1]; }
                }
            }
            for (int index = 0; index < DegList[b]; index++)
            {
                if (EList[b, index].NodeId == a)
                {
                    DegList[b]--;
                    i++;
                    for (; index < DegList[b]; index++) EList[b, index] = EList[b, index + 1];
                }
            }

            return i == 2;
        }
    }
    public class Vertex
    {
        public int Id;
        public int CId; //component ID for steiner tree
        public int XLoc;
        public int YLoc;

        public double PreciseX;
        public double PreciseY;
        public double TargetX;
        public double TargetY;

        public double LeftX;
        public double LeftY;
        public double RightX;
        public double RightY;

        public double Dist = 0;
        public double Weight = 0; // priority
        public int ZoomLevel = 0;
        public Vertex Parent = null;
        public bool Visited;
        public bool Invalid;

        public Ray topRay;
        public Ray bottomRay;
        public Ray leftRay;
        public Ray rightRay;

        public Dictionary<LineSegment, bool> SegmentList = new Dictionary<LineSegment, bool>();

        public Vertex(int a, int b)
        {
            XLoc = a;
            YLoc = b;
        }


    }

    public class Ray
    {
        public LineSegment L;
        public bool dead;
        public Ray(LineSegment segment)
        {
            L = segment;
        }
    }

    public class Edge
    {
        public double Weight = 1;
        //public double EDist;
        public int EdgeId;
        public int Cost = 0;
        public int NodeId;
        public int Selected;
        public int Used;
        public Edge(int z)
        {
            NodeId = z;
        }

        public double GetEDist(Vertex[] vList, int a, int b)
        {
            return Math.Sqrt((vList[a].XLoc - vList[b].XLoc) * (vList[a].XLoc - vList[b].XLoc) + (vList[a].YLoc - vList[b].YLoc) * (vList[a].YLoc - vList[b].YLoc));
        }
    }

    public class ShortestPathEdgeList
    {
        public List<VertexNeighbor> Edgelist = new List<VertexNeighbor>();
    }

}
