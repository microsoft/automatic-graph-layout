using System;
using System.Collections;

namespace WindowsFormsApplication3
{
    
    public class Tiling
    {
        public int NumOfnodes;
        public int[,] NodeMap;
        public int []DegList;
        public Vertex []VList;
        public Edge[,] eList;
        public double maxweight = 0;
        public int N = 0;
        Component sNet = null;

        public Tiling(int bound)
        {
            int m, n,  k = 1; //node location and inddex
            N = bound;
            
            NodeMap = new int[N,N];
            eList = new Edge[N * N,20];
            VList = new Vertex[N * N];
            DegList = new int[N * N];


            //create vertex list
            for (int y = 1; y < N; y ++)
            {
                for (int x = 1; x < N-1; x += 2)
                {
                    //create node at location m,n
                    m = x+ (y+1)%2;
                    n = y;

                    VList[k] = new Vertex(m,n);
                    VList[k].Id = k;
                     
                    DegList[k] = 0;

                    //map location to the vertex index
                    NodeMap[m,n] = k;
                    k++;
                }
                
            }
            NumOfnodes = k-1;

            //create edge list
            for (int index = 1; index < k; index++)
            {
                //find the current location
                m = VList[index].XLoc;
                n = VList[index].YLoc;

                //find the six neighbors

              
                //left                
                if (m - 2 > 0 && NodeMap[m - 2, n] >0)
                {
                    DegList[index]++;  
                    eList[index, DegList[index]] = new Edge(NodeMap[m-2, n]);
                }

                //right                
                if (m + 2 < N && NodeMap[m + 2, n] > 0)
                {
                    DegList[index]++;
                    eList[index, DegList[index]] = new Edge(NodeMap[m +2, n]);
                }

                //top-right                
                if (n + 1 < N && m + 1 < N && NodeMap[m +1, n+1] > 0)
                {
                    DegList[index]++;
                    eList[index, DegList[index]] = new Edge(NodeMap[m + 1, n+1]);
                }
                //top-left                
                if (n + 1 < N && m - 1 > 0 && NodeMap[m -1 , n+1] > 0)
                {
                    DegList[index]++;
                    eList[index, DegList[index]] = new Edge(NodeMap[m - 1, n + 1]);
                }
                //bottom-right                
                if (n - 1 > 0 && m + 1 < N && NodeMap[m +1, n-1] > 0)
                {
                    DegList[index]++;
                    eList[index, DegList[index]] = new Edge(NodeMap[m + 1, n - 1]);
                }
                //bottom-left                
                if (n - 1 >0 && m - 1 >0)
                {
                    DegList[index]++;
                    eList[index, DegList[index]] = new Edge(NodeMap[m - 1, n - 1]);
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
        public void ComputeGridEdgeWeights()
        {
            double temp;
            Queue q = new Queue();
            //for each node, it it has a weight, then update edge weights 
            for (int index = 1;    index <= NumOfnodes; index++)
            {
                if(VList[index].Weight ==0) continue;
                q.Enqueue(index);

                while (q.Count > 0)
                {
                    //take the current node
                    var current_node = (int)q.Dequeue();
                    if (VList[current_node].Visited) continue;
                    else VList[current_node].Visited = true;                    
                    //for each neighbor of the current node
                    for (int neighb = 1; neighb <= DegList[current_node]; neighb++)
                    {
                        var neighbor = eList[current_node, neighb].NodeId;
                        //find an edge such that the target node is never visited; that is the edge has never been visited
                        if (VList[neighbor].Visited == false)
                        {
                            //BFS                            
                            q.Enqueue(neighbor); 
                            //compute what would be the edge for the current edge
                            temp = GetWeight(index, current_node, neighbor, VList[index].Weight);
                            
                            if (temp < 0 || q.Count>200 ) {
                                q.Clear(); break; 
                            }

                            //update the weight of the edge
                            eList[current_node, neighb].Weight += temp;
                            eList[current_node, neighb].EDist = GetEucledianDist(index, neighbor);

                            if (maxweight < eList[current_node, neighb].Weight) 
                                maxweight =  eList[current_node, neighb].Weight;
                            
                            //Console.WriteLine(current_node   + "," +  vList[current_node].visited  + ":" +  neighbor  + "," +  vList[neighbor].visited  + "::" + (int)eList[current_node, neighb].weight); 

                            //find the reverse edge and update it
                            for (int r = 1; r <= DegList[neighbor]; r++)
                            {
                                if (eList[neighbor, r].NodeId == current_node)
                                {
                                    eList[neighbor, r].Weight += temp;
                                    eList[neighbor, r].EDist = GetEucledianDist(index, neighbor);
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
            double d1 =  Math.Sqrt((VList[a].XLoc - VList[b].XLoc) * (VList[a].XLoc - VList[b].XLoc) + (VList[a].YLoc - VList[b].YLoc) * (VList[a].YLoc - VList[b].YLoc));
            double d2 = Math.Sqrt((VList[a].XLoc - VList[c].XLoc) * (VList[a].XLoc - VList[c].XLoc) + (VList[a].YLoc - VList[c].YLoc) * (VList[a].YLoc - VList[c].YLoc));
            double d3 = Math.Sqrt((VList[b].XLoc - VList[c].XLoc) * (VList[b].XLoc - VList[c].XLoc) + (VList[b].YLoc - VList[c].YLoc) * (VList[b].YLoc - VList[c].YLoc));

            //d = Math.Abs(vList[a].x_loc - vList[b].x_loc) / 2 + Math.Abs(vList[a].y_loc - vList[b].y_loc);

            if (VList[a].Id==VList[b].Id) return 1000;
            

            //distribute around a disk of radious 
            double sigma = 5;// Math.Sqrt(w);

            //return w - d;
            var w1 = w * (Math.Exp(-(d1 * d1 / (2 * sigma * sigma))) / (sigma * Math.Sqrt(2 * Math.PI)));
            var w2 = w * (Math.Exp(-(d2 * d2 / (2 * sigma * sigma))) / (sigma * Math.Sqrt(2 * Math.PI))); 
            return (w1+w2)/2;
        }
        public double GetEucledianDist(int a, int b)
        {
            return Math.Sqrt((VList[a].XLoc - VList[b].XLoc) * (VList[a].XLoc - VList[b].XLoc) + (VList[a].YLoc - VList[b].YLoc) * (VList[a].YLoc - VList[b].YLoc));             
        }
        public void PlotAllEdges(Point [] pt, Network g, int numPoints, int numOfLevels)
        {
            
            SteinerTree t = new SteinerTree();


            //COMPUTE RECURSIVE STEINER
            for(int level = 1;level <= numOfLevels; level ++)
            {
                sNet = t.ComputeTree(VList, eList, DegList, NumOfnodes, sNet, level);                
            }

            for (int i = 1; i <= NumOfnodes; i++) VList[i].CId = 0;
            foreach (Vertex w in sNet.V) w.CId = 1;

            Console.WriteLine("Compute the local neighborhood: Shortcut Mesh.");
            ComputeShortcutMesh(pt,numPoints);

           
            Console.WriteLine("Compute edge routes.");
            //COMPUTE EDGE ROUTES
            DijkstraAlgo dijkstra = new DijkstraAlgo();
            
            for (int i = 1; i <= numPoints; i++)
            {
                for (int j = i + 1; j <= numPoints; j++)
                {
                    if(g.M[i,j]==1)
                        dijkstra.selectShortestPath(VList, eList, DegList, pt[i].GridPoint, pt[j].GridPoint, NumOfnodes);
                    foreach (VertexNeighbor e in dijkstra.edgelist)
                    {
                        eList[e.a, e.neighbor].Used++;
                        sNet.Segments.Add(new VertexNeighbor(e.a, e.neighbor));

                    }
                }
            }

            for (int q = 1; q <= 1; q++)
            {
                //LOCAL REFINEMENTS
                Console.WriteLine("Remove Deg-2 vertices when possible.");
                RemoveDeg2(pt, numPoints);

                Console.WriteLine("Move towards center of mass.");
                MoveToMedian(pt, numPoints);
            }

        }

        public void MoveToMedian(Point[] pt, int numPoints)
        {
            int[,] listNeighbors = new int[20, 3];            
            double []d = new double[10];
            double mincost = 100000;
            int a=0,b=0, mincostA=0, mincostB=0;
            bool localRefinementsFound = true;
            int iteration = 100;


            while (localRefinementsFound && iteration > 0)
            {
                iteration--;
                localRefinementsFound = false;
                

                foreach (Vertex w in sNet.V)
                {
                    if (w.invalid || w.Weight > 0) continue;


                    int numNeighbors = 0;
                    mincost = 100000;

                    for (int k = 1; k <= DegList[w.Id]; k++)
                    {
                        if (eList[w.Id, k].Used > 0)
                        {
                            numNeighbors++;
                            listNeighbors[numNeighbors, 1] = eList[w.Id, k].NodeId;
                            listNeighbors[numNeighbors, 2] = k;
                        }
                    }
                    if (numNeighbors <= 1) continue;

                    for (int index = 1; index <= 9; index++)
                    {
                        d[index] = 0;

                        if (index == 1){a = 1;b = 1;}
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
                                d[index] +=Math.Sqrt((w.XLoc+a - VList[listNeighbors[k, 1]].XLoc)*
                                              (w.XLoc+a - VList[listNeighbors[k, 1]].XLoc)
                                              +
                                              (w.YLoc+b - VList[listNeighbors[k, 1]].YLoc)*
                                              (w.YLoc+b - VList[listNeighbors[k, 1]].YLoc)
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

        public bool GoodResolution(Vertex w, int[,] listNeighbors, int numNeighbors, Point[] pt, int numPoints)
        {
             for (int i = 1; i <= numNeighbors; i++)
            {
                for (int j = i+1; j <= numNeighbors; j++)
                {
                    //check for angular resolution  

                    float xDiff1 = w.XLoc - VList[listNeighbors[i,1]].XLoc;
                    float yDiff1 = w.YLoc - VList[listNeighbors[i, 1]].YLoc;

                    float xDiff2 = w.XLoc - VList[listNeighbors[j, 1]].XLoc;
                    float yDiff2 = w.YLoc - VList[listNeighbors[j, 1]].YLoc;

                    if (Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2)) < 0.3)
                    {
                        //Console.WriteLine(" Angle");
                        return false;
                    }
                }

                //check for distance
                foreach (var z in sNet.V)
                {
                    if (z.Id == w.Id || z.Id == VList[listNeighbors[i, 1]].Id  || z.invalid) continue;
                    
                    //distance from z to w,i

                    //Console.WriteLine(" " + z.Id);

                    if (PointToSegmentDistance.getDistance(w, VList[listNeighbors[i, 1]], z) < 1)
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
        public bool IsWellSeperated(Vertex w, int w1,int w2, Point [] pt, int numPoints)
        {
 
            double d1, d2, d3, edge_node_separation = 0.01;
            //add the edge if they are not very close to a point
            for (int index = 1; index <= numPoints; index++)
            {
                if (VList[pt[index].GridPoint].invalid) continue;
                if ((pt[index].X == VList[w1].XLoc && pt[index].Y == VList[w1].YLoc)
                    || (pt[index].X == VList[w2].XLoc && pt[index].Y == VList[w2].YLoc)) continue;

                 
                d1 = (pt[index].X - VList[w1].XLoc) * (pt[index].X - VList[w1].XLoc)
                    + (pt[index].Y - VList[w1].YLoc) * (pt[index].Y - VList[w1].YLoc);
                d2 = (pt[index].X - VList[w2].XLoc) * (pt[index].X - VList[w2].XLoc)
                    + (pt[index].Y - VList[w2].YLoc) * (pt[index].Y - VList[w2].YLoc);
                d3 = (VList[w1].XLoc - VList[w2].XLoc) * (VList[w1].XLoc - VList[w2].XLoc)
                    + (VList[w1].YLoc - VList[w2].YLoc) * (VList[w1].YLoc - VList[w2].YLoc);
                if (d1 + d2 < d3 + edge_node_separation)
                 
                //if (PointToSegmentDistance.getDistance(VList[pt[index].GridPoint], VList[w1], VList[w2]) < edge_node_separation)
                {
                    //Console.WriteLine("close" + w.Id + " " + w1 + " " + w2 + " : " + pt[index].GridPoint);
                    return  false;
                }
            }

            
            //check for angular resolution at neighbor1
            for (int neighb = 1; neighb <= DegList[w1]; neighb++)
            {
                if (eList[w1, neighb].Used == 0) continue;
                if (eList[w1, neighb].NodeId ==  w.Id || eList[w1, neighb].NodeId == w2   ) continue;           

                float xDiff1 = VList[w1].XLoc - VList[w2].XLoc;
                float yDiff1 = VList[w1].YLoc - VList[w2].YLoc;
                float m1 = yDiff1 / (0.0001F + xDiff1);

                float xDiff2 = VList[w1].XLoc - VList[eList[w1,neighb].NodeId].XLoc;
                float yDiff2 = VList[w1].YLoc - VList[eList[w1,neighb].NodeId].YLoc;
                float m2 = yDiff2 / (0.0001F + xDiff2);

                if (Math.Abs(Math.Atan2(yDiff1 , xDiff1) - Math.Atan2(yDiff2 , xDiff2)) < 0.5 )
                {
                    //Console.WriteLine("sharp*" + w.Id + " " + w2 + "(" + xDiff1 + "," + yDiff1 + ")" + w1 + "(" + xDiff2 + "," + xDiff2 + ")" + "::" + eList[w1, neighb].NodeId);
                    return false;
                }
            }

            //check for angular resolution at neighbor2
            for (int neighb = 1; neighb <= DegList[w2]; neighb++)
            {
                if (eList[w2, neighb].Used == 0) continue;
                if (eList[w2, neighb].NodeId == w.Id || eList[w2, neighb].NodeId ==  w1    ) continue;

                float xDiff1 = VList[w2].XLoc - VList[w1].XLoc;
                float yDiff1 = VList[w2].YLoc- VList[w1].YLoc ;
                float m1 = yDiff1/(0.0001F + xDiff1);

                float xDiff2 = VList[w2].XLoc - VList[eList[w2, neighb].NodeId].XLoc;
                float yDiff2 = VList[w2].YLoc - VList[eList[w2, neighb].NodeId].YLoc;
                float m2 = yDiff2 / (0.0001F + xDiff2);

                if (Math.Abs(Math.Atan2(yDiff1, xDiff1) - Math.Atan2(yDiff2, xDiff2)) < 0.5)
                {
                    //Console.WriteLine("sharp" + w.Id + " " + w2 + "(" + xDiff1 + "," + yDiff1 + ")" + w1 + "(" + xDiff2 + "," + xDiff2 + ")" + "::" + eList[w2, neighb].NodeId);
                    return false;
                }
            }
            //Console.WriteLine( "Interesting : " + (Math.Atan2(1, 1)-Math.Atan2(1, -1)));
            return true;
        }
        public void RemoveDeg2(Point [] pt,int numPoints)
        {
            int[,] listNeighbors = new int[20, 3];
           
            bool adjust = true;
            bool localRefinementsFound = true;
            int iteration = 100;


            while (localRefinementsFound && iteration > 0)
            {
                iteration--;
                localRefinementsFound = false;
                foreach (Vertex w in sNet.V)
                {
                    var numNeighbors = 0;
                    if (w.Weight > 0) continue;
                    for (int k = 1; k <= DegList[w.Id]; k++)
                    {
                        if (eList[w.Id, k].Used > 0)
                        {
                            numNeighbors++;
                            listNeighbors[numNeighbors, 1] = eList[w.Id, k].NodeId;
                            listNeighbors[numNeighbors, 2] = k;
                        }
                    }
                    if (numNeighbors <=1)
                    {
                        w.CId = 0;
                        w.invalid = true;
                    }
                    if (numNeighbors == 2)
                    {

                        adjust = IsWellSeperated(w,listNeighbors[1,1],listNeighbors[2,1], pt, numPoints);

                        if (adjust)
                        {
                            //Console.WriteLine(w.ID + " :: " + listNeighbors[2, 1] + " " + w1);
                            localRefinementsFound = true;
                             
                            for (int j = 1; j <= DegList[listNeighbors[2, 1]]; j++)
                            {
                                if (eList[listNeighbors[2, 1], j].NodeId == w.Id)
                                {
                                    adjust = true;
                                    //check if it already exists in the neighbor list
                                    for (int check = 1; check <= DegList[listNeighbors[2, 1]]; check++)
                                        if (eList[listNeighbors[2, 1], check].NodeId == listNeighbors[1, 1])
                                        {
                                            eList[listNeighbors[2, 1], check].Selected =  eList[w.Id, listNeighbors[2, 2]].Selected;
                                            eList[listNeighbors[2, 1], check].Used =  eList[w.Id, listNeighbors[2, 2]].Used;
                                            eList[listNeighbors[2, 1], j].Selected = 0;
                                            eList[listNeighbors[2, 1], j].Used = 0;
                                            adjust = false;
                                        }

                                    if (adjust) {
                                        eList[listNeighbors[2, 1], j].NodeId = listNeighbors[1, 1];
                                        //eList[listNeighbors[2, 1], j].Selected = 8;
                                        //eList[listNeighbors[2, 1], j].Used = 8;
                                    }

                                }
                            }

                            for (int i = 1; i <= DegList[listNeighbors[1, 1]]; i++)
                            {
                                if (eList[listNeighbors[1, 1], i].NodeId == w.Id)
                                {
                                    adjust = true;
                                    //check if it already exists in the neighbor list
                                    for (int check = 1; check <= DegList[listNeighbors[1, 1]]; check++)
                                        if (eList[listNeighbors[1, 1], check].NodeId == listNeighbors[2, 1])
                                        {
                                            eList[listNeighbors[1, 1], check].Selected =  eList[w.Id, listNeighbors[1, 2]].Selected;
                                            eList[listNeighbors[1, 1], check].Used =  eList[w.Id, listNeighbors[1, 2]].Used;
                                            eList[listNeighbors[1, 1], i].Selected = 0;
                                            eList[listNeighbors[1, 1], i].Used = 0;
                                            adjust = false;
                                        }

                                    if (adjust) {
                                        eList[listNeighbors[1, 1], i].NodeId = listNeighbors[2, 1];
                                        //eList[listNeighbors[1, 1], i].Selected = 8;
                                        //eList[listNeighbors[1, 1], i].Used = 8;
                                    }
                                }
                            }
                           


                            //delete old edges

                            eList[w.Id, listNeighbors[1, 2]].Selected = 0;
                            eList[w.Id, listNeighbors[1, 2]].Used = 0;
                            eList[w.Id, listNeighbors[2, 2]].Selected = 0;
                            eList[w.Id, listNeighbors[2, 2]].Used = 0;

                            //remove the vertex                            
                            w.invalid = true;
                            w.CId = 0;
                            Console.WriteLine("removed " + w.Id);

                        }
                    }

                }
            }

        }
        public void ComputeShortcutMesh(Point []pt, int numPoints)
        {


            //COMPUTE NEIGHBORHOOD SHORTCUTS
            int y = 0, neighb = 0;
            for (int i = numPoints; i >= 1; i--)
            {
                var x = pt[i].X;
                y = pt[i].Y;

                //if v_i has a neighbor in the first (top right) quadrant 
                while (x + 1 < N && y + 1 < N && NodeMap[x + 1, y + 1] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y + 1]].Id) break;
                    }
                    if (eList[NodeMap[x, y], neighb].Selected == 0) break;
                    x = x + 1;
                    y = y + 1;
                }
                while (x + 1 < N && y + 1 < N && NodeMap[x + 1, y + 1] > 0 && VList[NodeMap[x + 1, y + 1]].CId == 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y + 1]].Id) break;
                    }
                    eList[NodeMap[x, y], neighb].Selected = 6;
                    x = x + 1;
                    y = y + 1;
                    VList[NodeMap[x, y]].CId = 1;
                    sNet.V.Add(VList[NodeMap[x, y]]);
                }
                if (x + 1 < N && y + 1 < N && NodeMap[x + 1, y + 1] > 0 && VList[NodeMap[x + 1, y + 1]].CId > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y + 1]].Id) break;
                    }
                    eList[NodeMap[x, y], neighb].Selected = 6;
                    x = x + 1;
                    y = y + 1;
                    VList[NodeMap[x, y]].CId = 1;
                    sNet.AddVertex(VList[NodeMap[x, y]]);
                }

                x = pt[i].X;
                y = pt[i].Y;

                //if v_i has a neighbor in the top left quadrant 
                while (x - 1 > 0 && y + 1 < N && NodeMap[x - 1, y + 1] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y + 1]].Id) break;
                    }
                    if (eList[NodeMap[x, y], neighb].Selected == 0) break;
                    x = x - 1;
                    y = y + 1;
                }
                while (x - 1 > 0 && y + 1 < N && NodeMap[x - 1, y + 1] > 0 && VList[NodeMap[x - 1, y + 1]].CId == 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y + 1]].Id) break;
                    }
                    eList[NodeMap[x, y], neighb].Selected = 6;
                    x = x - 1;
                    y = y + 1;
                    VList[NodeMap[x, y]].CId = 1;
                    sNet.AddVertex(VList[NodeMap[x, y]]);
                }
                if (x - 1 > 0 && y + 1 < N && NodeMap[x - 1, y + 1] > 0 && VList[NodeMap[x - 1, y + 1]].CId > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y + 1]].Id) break;
                    }
                    eList[NodeMap[x, y], neighb].Selected = 6;
                    x = x - 1;
                    y = y + 1;
                    VList[NodeMap[x, y]].CId = 1;
                    sNet.AddVertex(VList[NodeMap[x, y]]);
                }

                x = pt[i].X;
                y = pt[i].Y;

                //if v_i has a neighbor in the bottom right quadrant 
                while (x + 1 < N && y - 1 > 0 && NodeMap[x + 1, y - 1] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y - 1]].Id) break;
                    }
                    if (eList[NodeMap[x, y], neighb].Selected == 0) break;
                    x = x + 1;
                    y = y - 1;
                }
                while (x + 1 < N && y - 1 > 0 && NodeMap[x + 1, y - 1] > 0 && VList[NodeMap[x + 1, y - 1]].CId == 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y - 1]].Id) break;
                    }
                    eList[NodeMap[x, y], neighb].Selected = 6;
                    x = x + 1;
                    y = y - 1;
                    VList[NodeMap[x, y]].CId = 1;
                    sNet.AddVertex(VList[NodeMap[x, y]]);
                }
                if (x + 1 < N && y - 1 > 0 && NodeMap[x + 1, y - 1] > 0 && VList[NodeMap[x + 1, y - 1]].CId > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x + 1, y - 1]].Id) break;
                    }
                    eList[NodeMap[x, y], neighb].Selected = 6;
                    x = x + 1;
                    y = y - 1;
                    VList[NodeMap[x, y]].CId = 1;
                    sNet.AddVertex(VList[NodeMap[x, y]]);
                }

                x = pt[i].X;
                y = pt[i].Y;

                //if v_i has a neighbor in the bottom-left quadrant 
                while (x - 1 > 0 && y - 1 > 0 && NodeMap[x - 1, y - 1] > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y - 1]].Id) break;
                    }
                    if (eList[NodeMap[x, y], neighb].Selected == 0) break;
                    x = x - 1;
                    y = y - 1;
                }
                while (x - 1 > 0 && y - 1 > 0 && NodeMap[x - 1, y - 1] > 0 && VList[NodeMap[x - 1, y - 1]].CId == 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y - 1]].Id) break;
                    }
                    eList[NodeMap[x, y], neighb].Selected = 6;
                    x = x - 1;
                    y = y - 1;
                    VList[NodeMap[x, y]].CId = 1;
                    sNet.AddVertex(VList[NodeMap[x, y]]);
                }
                if (x - 1 > 0 && y - 1 > 0 && NodeMap[x - 1, y - 1] > 0 && VList[NodeMap[x - 1, y - 1]].CId > 0)
                {
                    for (neighb = 1; neighb <= DegList[NodeMap[x, y]]; neighb++)
                    {
                        if (eList[NodeMap[x, y], neighb].NodeId == VList[NodeMap[x - 1, y - 1]].Id) break;
                    }
                    eList[NodeMap[x, y], neighb].Selected = 6;
                    x = x - 1;
                    y = y - 1;
                    VList[NodeMap[x, y]].CId = 1;
                    sNet.AddVertex(VList[NodeMap[x, y]]);
                }

            }
        }
    }
    public class Vertex
    {
        public int Id;
        public int CId; //component ID for steiner tree
        public int XLoc;
        public int YLoc;
        public double Dist = 0;
        public int Weight = 0; // priority
        public int ZoomLevel = 0;
        public Vertex Parent = null;
        public bool Visited = false;
        public bool invalid = false;

        public Vertex(int a, int b)
        {
            XLoc = a;
            YLoc = b;
        }


    }
    public class Edge
    {        
        public double Weight = 1;
        public double EDist;
        public int Cost = 0;
        public int NodeId;
        public int Selected;
        public int Used;
        public Edge(int z)
        {
            NodeId = z;
        }
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

                foreach (VertexNeighbor _tuple in p.edgelist)
                {
                    //Console.Write(_tuple.a + " ");
                    if (sNet.v.Contains(vList[_tuple.a]) && _tuple.a != pt[i].grid_point && _tuple.a != pt[j].grid_point) PlanarPath = false;
                }
                //Console.WriteLine();

                if (PlanarPath == false) continue;

                foreach (VertexNeighbor _tuple in p.edgelist)
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