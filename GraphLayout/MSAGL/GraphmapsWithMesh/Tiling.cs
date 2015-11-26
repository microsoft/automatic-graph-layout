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
        public int N;
        Component _sNet;
        readonly double[] _edgeNodeSeparation;
        readonly double _angularResolution;
        public bool isPlanar;
        public double thinness;

        public RTree<int> nodeTree = new RTree<int>();
        public RTree<int> edgeTree = new RTree<int>();
        

        public Tiling(int nodeCount, bool isGraph)
        {
            thinness = 2;
            _angularResolution = 0.3;
            NumOfnodes = N = nodeCount;
            EList = new Edge[10 * N, 10];
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
            /*//check whether all neighbors are distinct
            for (int x = 1; x <= numOfnodes; x++)
            {                
                for (int y = 1; y <= degList[x]; y++)
                {
                    for (int z = y + 1; z <= degList[x]; z++)
                        if (eList[x, y].nodeId == eList[x, z].nodeId) Console.WriteLine("BAD");
                }
            }*/

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

                            //Console.WriteLine(current_node   + "," +  vList[current_node].visited  + ":" +  neighbor  + "," +  vList[neighbor].visited  + "::" + (int)eList[current_node, neighb].weight); 

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

            //d = Math.Abs(vList[a].x_loc - vList[b].x_loc) / 2 + Math.Abs(vList[a].y_loc - vList[b].y_loc);

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


        public void MsaglMoveToMaximizeMinimumAngle()
        {
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
                    double cost = 100000;
                    double profit = 0;

                    for (int k = 0; k < DegList[w.Id]; k++)
                    {
                        numNeighbors++;
                        listNeighbors[numNeighbors, 1] = EList[w.Id, k].NodeId;
                        listNeighbors[numNeighbors, 2] = k;
                    }

                    if (numNeighbors <= 1) continue;

                    //if (index > NumOfnodesBeforeDetour)
                    //Console.WriteLine();
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

                            ///*try to maximize min angle
                            d[counter] = 3.1416;
                            for (int l = 1; l <= numNeighbors; l++)
                            {
                                if (l == k) continue;
                                d[counter] = Math.Min(d[counter],
                                    Angle.getAngleIfSmallerThanPIby2(new Vertex(w.XLoc + a, w.YLoc + b),
                                        VList[listNeighbors[k, 1]], VList[listNeighbors[l, 1]]));
                            }
                            //*/
                            /*try to minimize ink
                            d[counter] += Math.Sqrt((w.XLoc + a - VList[listNeighbors[k, 1]].XLoc) *
                                              (w.XLoc + a - VList[listNeighbors[k, 1]].XLoc)
                                              +
                                              (w.YLoc + b - VList[listNeighbors[k, 1]].YLoc) *
                                              (w.YLoc + b - VList[listNeighbors[k, 1]].YLoc)
                                        );
                            //*/

                        }
                        ///*try to maximize min angle
                        if (profit < d[counter])
                        {
                            profit = d[counter]; mincostA = a; mincostB = b;
                        }
                        //*/

                        /*try to minimize ink
                        if (cost > d[counter])
                        {
                            cost = d[counter]; mincostA = a; mincostB = b;
                        }
                        //*/


                    }
                    //if (MSAGLGoodResolution(w, listNeighbors, numNeighbors) == false)
                    //Console.WriteLine("iteration " + iteration);
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
                            Console.Write(".");
                            localRefinementsFound = true;
                        }
                    }

                }
            }
            Console.WriteLine("Done");
        }


        public bool noCrossingsHeuristics(Vertex w)
        {             
            var p1 = new Point(w.XLoc-5, w.YLoc-5);
            var p2 = new Point(w.XLoc+5, w.YLoc+5);
            Rectangle queryRectangle = new Rectangle(p1, p2);
            int[] candidateList = nodeTree.GetAllIntersecting(queryRectangle);

            
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
            Microsoft.Msagl.Core.Geometry.Point c = new Microsoft.Msagl.Core.Geometry.Point(w1.XLoc, w1.YLoc);
            Microsoft.Msagl.Core.Geometry.Point d = new Microsoft.Msagl.Core.Geometry.Point(w2.XLoc, w2.YLoc);

            for (int i = 0; i < NumOfnodes; i++)
            {
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


                    Microsoft.Msagl.Core.Geometry.Point a = new Microsoft.Msagl.Core.Geometry.Point(VList[i].XLoc, VList[i].YLoc);
                    Microsoft.Msagl.Core.Geometry.Point b = new Microsoft.Msagl.Core.Geometry.Point(VList[k1].XLoc, VList[k1].YLoc);


                    Microsoft.Msagl.Core.Geometry.Point interestionPoint;

                    if (Microsoft.Msagl.Core.Geometry.Point.SegmentSegmentIntersection(a, b, c, d, out interestionPoint))
                        return false;

                }
            }
            return true;
        }
        public void MoveToMedian(WeightedPoint[] pt, int numPoints)
        {
            int[,] listNeighbors = new int[20, 3];
            double[] d = new double[10];
            int a = 0, b = 0, mincostA = 0, mincostB = 0;
            bool localRefinementsFound = true;
            int iteration = 100;


            while (localRefinementsFound && iteration > 0)
            {
                iteration--;
                localRefinementsFound = false;


                foreach (Vertex w in _sNet.V)
                {
                    if (w.Invalid || w.Weight > 0) continue;


                    int numNeighbors = 0;
                    double mincost = 100000;

                    for (int k = 1; k <= DegList[w.Id]; k++)
                    {
                        if (EList[w.Id, k].Used > 0)
                        {
                            numNeighbors++;
                            listNeighbors[numNeighbors, 1] = EList[w.Id, k].NodeId;
                            listNeighbors[numNeighbors, 2] = k;
                        }
                    }
                    if (numNeighbors <= 1) continue;

                    for (int index = 1; index <= 9; index++)
                    {
                        d[index] = 0;

                        if (index == 1) { a = 1; b = 1; }
                        if (index == 2) { a = 0; b = 1; }
                        if (index == 3) { a = -1; b = 1; }
                        if (index == 4) { a = -1; b = 0; }
                        if (index == 5) { a = -1; b = -1; }
                        if (index == 6) { a = 0; b = -1; }
                        if (index == 7) { a = 1; b = -1; }
                        if (index == 8) { a = 1; b = 0; }
                        if (index == 9) { a = 0; b = 0; }


                        for (int k = 1; k <= numNeighbors; k++)
                        {
                            double length = Math.Sqrt((w.XLoc + a - VList[listNeighbors[k, 1]].XLoc) *
                                          (w.XLoc + a - VList[listNeighbors[k, 1]].XLoc)
                                          +
                                          (w.YLoc + b - VList[listNeighbors[k, 1]].YLoc) *
                                          (w.YLoc + b - VList[listNeighbors[k, 1]].YLoc)
                                    );
                            if (length < 5)
                            {
                                mincostA = 0; mincostB = 0;
                                break;
                            }
                            else
                                d[index] += Math.Sqrt((w.XLoc + a - VList[listNeighbors[k, 1]].XLoc) *
                                              (w.XLoc + a - VList[listNeighbors[k, 1]].XLoc)
                                              +
                                              (w.YLoc + b - VList[listNeighbors[k, 1]].YLoc) *
                                              (w.YLoc + b - VList[listNeighbors[k, 1]].YLoc)
                                        );

                        }
                        if (mincost > d[index])
                        {
                            mincost = d[index]; mincostA = a; mincostB = b;
                        }


                    }

                    if (!(mincostA == 0 && mincostB == 0))
                    {
                        w.XLoc += mincostA;
                        w.YLoc += mincostB;
                        if (GoodResolution(w, listNeighbors, numNeighbors, pt, numPoints) == false)
                        {
                            w.XLoc -= mincostA;
                            w.YLoc -= mincostB;
                            //Console.WriteLine(""+w.Id);
                        }
                        else
                        {
                            Console.Write(".");
                            localRefinementsFound = true;
                        }
                    }

                }
            }
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
                        //Console.WriteLine(" Angle");
                        return false;
                    }
                }

                //check for distance
                foreach (var z in _sNet.V)
                {
                    if (z.Id == w.Id || z.Id == VList[listNeighbors[i, 1]].Id || z.Invalid) continue;

                    //distance from z to w,i

                    //Console.WriteLine(" " + z.Id);

                    if (PointToSegmentDistance.GetDistance(w, VList[listNeighbors[i, 1]], z) < _edgeNodeSeparation[EList[w.Id, listNeighbors[i, 2]].Selected])
                    {
                        /*
                            Console.Write(" Distance "  );
                            Console.WriteLine(" " + w.Id + " " + ": " + VList[listNeighbors[i, 1]].Id + " " + z.Id + " " + PointToSegmentDistance.getDistance(w, VList[listNeighbors[i, 1]], z));
                            Console.WriteLine(" " + w.XLoc + "," + w.YLoc +
                                    ": " + VList[listNeighbors[i, 1]].XLoc + " " + VList[listNeighbors[i, 1]].YLoc +
                                    ": " + z.XLoc + " " + z.YLoc +
                                    " " + PointToSegmentDistance.getDistance(w, VList[listNeighbors[i, 1]], z));
                            Console.WriteLine("Distance = " + PointToSegmentDistance.getDistance(new Vertex(w.XLoc, w.YLoc), new Vertex(VList[listNeighbors[i, 1]].XLoc, VList[listNeighbors[i, 1]].YLoc), new Vertex(z.XLoc, z.YLoc)));
                            */
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

                /*
                d1 = (pt[index].X - VList[w1].XLoc) * (pt[index].X - VList[w1].XLoc)
                    + (pt[index].Y - VList[w1].YLoc) * (pt[index].Y - VList[w1].YLoc);
                d2 = (pt[index].X - VList[w2].XLoc) * (pt[index].X - VList[w2].XLoc)
                    + (pt[index].Y - VList[w2].YLoc) * (pt[index].Y - VList[w2].YLoc);
                d3 = (VList[w1].XLoc - VList[w2].XLoc) * (VList[w1].XLoc - VList[w2].XLoc)
                    + (VList[w1].YLoc - VList[w2].YLoc) * (VList[w1].YLoc - VList[w2].YLoc);
                if (d1 + d2 < d3 + edge_node_separation)
                */
                int k = 0;
                for (int neighb = 1; neighb <= DegList[w.Id]; neighb++)
                    if (EList[w.Id, neighb].NodeId == w1) { k = neighb; break; }

                if (PointToSegmentDistance.GetDistance(VList[w1], VList[w2], VList[pt[index].GridPoint]) < _edgeNodeSeparation[EList[w.Id, k].Selected])
                {
                    //Console.WriteLine("close" + w.Id + " " + w1 + " " + w2 + " : " + pt[index].GridPoint);
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
                    //Console.WriteLine("sharp*" + w.Id + " " + w2 + "(" + xDiff1 + "," + yDiff1 + ")" + w1 + "(" + xDiff2 + "," + xDiff2 + ")" + "::" + eList[w1, neighb].NodeId);
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
                    //Console.WriteLine("sharp" + w.Id + " " + w2 + "(" + xDiff1 + "," + yDiff1 + ")" + w1 + "(" + xDiff2 + "," + xDiff2 + ")" + "::" + eList[w2, neighb].NodeId);
                    return false;
                }
            }
            //Console.WriteLine( "Interesting : " + (Math.Atan2(1, 1)-Math.Atan2(1, -1)));
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

            /*
            //add the edge w1w2 if they are not very close to a point O(|V|)
            for (int index = 0; index <NumOfnodes; index++)
            {
                //if vertex is one of the neighbors forget it
                if (VList[index].Invalid || DegList[index] ==0 || VList[index].Id == w1 || VList[index].Id == w2 || VList[index].Id == w.Id) continue;

                //otherwise find distance from index to w1w2                
                if (PointToSegmentDistance.GetDistance(VList[w1], VList[w2], VList[index]) < _edgeNodeSeparation[0])
                {
                     return false;
                }
            }
            */

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
        }
        /*
        public void MsaglRemoveDeg2(Dictionary<int, Node> idToNodes)
        {


            bool localRefinementsFound = true;
            int iteration = 20;

            //find all deg 2 vertices
            Dictionary<int, int[,]> Deg2Vertices = new Dictionary<int, int[,]>();
            for (int index = N; index < NumOfnodes; index++)
            {
                Vertex w = VList[index];
                int[,] listNeighbors = new int[20, 3];

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
                if (numNeighbors == 2) Deg2Vertices.Add(index, listNeighbors);
            }

            while (localRefinementsFound && iteration > 0)
            {
                iteration--;
                if (Deg2Vertices.Count == 0) break;
                localRefinementsFound = false;

                List<int> RemoveList = new List<int>();
                foreach (int index in Deg2Vertices.Keys)
                {
                    int[,] listNeighbors;
                    Vertex w = VList[index];
                    listNeighbors = Deg2Vertices[index];

                    //check whether the neighboring two edges are well separated from the other entities
                    //check if there is any edge crossing with the neighboring edges
                    var adjust = MsaglIsWellSeperated(w, listNeighbors[1, 1], listNeighbors[2, 1]);
                    adjust = adjust && noCrossings(w, VList[listNeighbors[1, 1]], VList[listNeighbors[2, 1]]);

                    //if the new position is good then remove this deg 2 vertex 
                    if (adjust)
                    {
                         

                        localRefinementsFound = true;
                        var selected = Math.Max(EList[index, listNeighbors[1, 2]].Selected, EList[index, listNeighbors[2, 2]].Selected);
                        var used = Math.Max(EList[index, listNeighbors[1, 2]].Used, EList[index, listNeighbors[2, 2]].Used);
                        RemoveEdge(index, listNeighbors[1, 1]);
                        RemoveEdge(index, listNeighbors[2, 1]);

                        AddEdge(listNeighbors[1, 1], listNeighbors[2, 1], selected, used);

                        if (DegList[w.Id] == 0) w.Invalid = true;

                        RemoveList.Add(index);

                       // nodeTree.Remove(new Rectangle(new Point(VList[index].XLoc, VList[index].YLoc)), index);

                    }
                }

                foreach (int q in RemoveList)
                    Deg2Vertices.Remove(q);
            }
        }
        */
        public void RemoveDeg2(WeightedPoint[] pt, int numPoints)
        {
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
                            //Console.WriteLine(w.ID + " :: " + listNeighbors[2, 1] + " " + w1);
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
                            //Console.WriteLine("removed " + w.Id);

                        }
                    }
                }
            }

        }

        public void MsaglDetour(Dictionary<int, Node> idToNode)
        {
            NumOfnodesBeforeDetour = NumOfnodes;

            for (int index = 0; index < N; index++)
            {
                Vertex w = VList[index];
                Vertex[] list = new Vertex[10];
                int separation = 1;
                int neighbor;


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
                    //if(list[neighbor] == null)
                    //Console.WriteLine("Degenerate Case "+ w.XLoc + " " + w.YLoc + " : " +b.XLoc+" "+b.YLoc);
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

            for (int index = 0; index < NumOfnodes; index++)
            {
                for (int j = 0; j < DegList[index]; j++)
                {

                    EList[index, j].EdgeId = index * 10 + j;
                    int neighbor = EList[index, j].NodeId;
                    Point a = new Point(VList[index].XLoc, VList[index].YLoc);
                    Point b = new Point(VList[neighbor].XLoc, VList[neighbor].YLoc);

                    edgeTree.Add(new Rectangle(a, b), EList[index, j].EdgeId);
                }
            }
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

                /*
                if (x - 1 > 0 && y - 1 > 0 && NodeMap[x - 1, y - 1] > 0 &&
                    x + 1 > 0 && y - 1 > 0 && NodeMap[x + 1, y - 1] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x - 1, y - 1]]; neighb++)
                    {
                        if (EList[NodeMap[x - 1, y - 1], neighb].NodeId == VList[NodeMap[x + 1, y - 1]].Id)
                        {
                            SelectEdge(EList, DegList, VList[NodeMap[x - 1, y - 1]], VList[EList[NodeMap[x - 1, y - 1], neighb].NodeId], 6);
                            break;
                        }
                    }
                }*/

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



//**************************************************************


/* SHORTEST PATH IDEA
for (int i = 1; i <= numOfnodes; i++) vList[i].cID = 0;
foreach (vertex w in sNet.v) w.cID = 1;
    for (int i = 1; i <= num_points; i++)
    {
        //Console.Write(i + "finished ");
        for (int j = i + 1; j <= num_points; j++)
        {
            //if (pt[i].zoomLevel != level || pt[j].zoomLevel != level) continue;

            //eDist = Math.Sqrt((pt[i].x - pt[j].x) * (pt[i].x - pt[j].x) + (pt[i].y - pt[j].y) * (pt[i].y - pt[j].y));
            //p.selectShortestPath(vList, eList, degList, pt[i].grid_point, pt[j].grid_point, numOfnodes);

            //if (p.Distance > 2 * eDist )
            {

                //Console.WriteLine(pt[i].grid_point + " * " + pt[j].grid_point  );
                p.selectShortestPathAvoidingNet(vList, eList, degList, pt[i].grid_point, pt[j].grid_point, numOfnodes);
                if (p.pathExists == false) continue;

                PlanarPath = true;

                foreach (IntegerPair _tuple in p.edgelist)
                {
                    //Console.Write(_tuple.a + " ");
                    if (sNet.v.Contains(vList[_tuple.a]) && _tuple.a != pt[i].grid_point && _tuple.a != pt[j].grid_point) PlanarPath = false;
                }
                //Console.WriteLine();

                if (PlanarPath == false) continue;

                foreach (IntegerPair _tuple in p.edgelist)
                {
                    if (eList[_tuple.a, _tuple.neighbor].selected == 0)
                        eList[_tuple.a, _tuple.neighbor].selected = Math.Min(pt[i].zoomLevel, pt[j].zoomLevel);
                    if (sNet.v.Contains(vList[_tuple.a]) == false)
                        sNet.AddVertex(vList[_tuple.a]);
                }
            }

        }
    }           
*/
/*
DijkstraAlgo p = new DijkstraAlgo();
     
for (int i = 1; i <=  num_points; i++)
{
    for (int j = i + 1; j <=  num_points; j++)
    {
        p.selectShortestPath(vList, eList, degList,  pt[i].grid_point,  pt[j].grid_point, numOfnodes);

    }
}
 * */