using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
 
namespace WindowsFormsApplication3
{
    
    public class Tiling
    {
        public int numOfnodes;
        public int[,] nodeMap;
        public int []degList;
        public vertex []vList;
        public edge[,] eList;
        public double maxweight = 0;
        public int N = 0;
        Component sNet = null;

        public Tiling(int bound)
        {
            int m, n,  k = 1; //node location and inddex
            N = bound;
            
            nodeMap = new int[N,N];
            eList = new edge[N * N,20];
            vList = new vertex[N * N];
            degList = new int[N * N];


            //create vertex list
            for (int y = 1; y < N; y ++)
            {
                for (int x = 1; x < N-1; x += 2)
                {
                    //create node at location m,n
                    m = x+ (y+1)%2;
                    n = y;
                    
                    vList[k] = new vertex();
                    vList[k].ID = k;
                    vList[k].x_loc = m;
                    vList[k].y_loc = n;
                    degList[k] = 0;

                    //map location to the vertex index
                    nodeMap[m,n] = k;
                    k++;
                }
                
            }
            numOfnodes = k-1;

            //create edge list
            for (int index = 1; index < k; index++)
            {
                //find the current location
                m = vList[index].x_loc;
                n = vList[index].y_loc;

                //find the six neighbors

              
                //left                
                if (m - 2 > 0 && nodeMap[m - 2, n] >0)
                {
                    degList[index]++;  
                    eList[index, degList[index]] = new edge(nodeMap[m-2, n]);
                }

                //right                
                if (m + 2 < N && nodeMap[m + 2, n] > 0)
                {
                    degList[index]++;
                    eList[index, degList[index]] = new edge(nodeMap[m +2, n]);
                }

                //top-right                
                if (n + 1 < N && m + 1 < N && nodeMap[m +1, n+1] > 0)
                {
                    degList[index]++;
                    eList[index, degList[index]] = new edge(nodeMap[m + 1, n+1]);
                }
                //top-left                
                if (n + 1 < N && m - 1 > 0 && nodeMap[m -1 , n+1] > 0)
                {
                    degList[index]++;
                    eList[index, degList[index]] = new edge(nodeMap[m - 1, n + 1]);
                }
                //bottom-right                
                if (n - 1 > 0 && m + 1 < N && nodeMap[m +1, n-1] > 0)
                {
                    degList[index]++;
                    eList[index, degList[index]] = new edge(nodeMap[m + 1, n - 1]);
                }
                //bottom-left                
                if (n - 1 >0 && m - 1 >0)
                {
                    degList[index]++;
                    eList[index, degList[index]] = new edge(nodeMap[m - 1, n - 1]);
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
        public void computeGridEdgeWeights()
        {
            int current_node, neighbor;
            double temp;
            Queue q = new Queue();
            //for each node, it it has a weight, then update edge weights 
            for (int index = 1;    index <= numOfnodes; index++)
            {
                if(vList[index].weight ==0) continue;
                q.Enqueue(index);

                while (q.Count > 0)
                {
                    //take the current node
                    current_node = (int)q.Dequeue();
                    if (vList[current_node].visited == true) continue;
                    else vList[current_node].visited = true;                    
                    //for each neighbor of the current node
                    for (int neighb = 1; neighb <= degList[current_node]; neighb++)
                    {
                        neighbor = eList[current_node, neighb].nodeId;
                        //find an edge such that the target node is never visited; that is the edge has never been visited
                        if (vList[neighbor].visited == false)
                        {
                            //BFS                            
                            q.Enqueue(neighbor); 
                            //compute what would be the edge for the current edge
                            temp = getWeight(index, current_node, neighbor, vList[index].weight);
                            
                            if (temp < 0 || q.Count>200 ) {
                                q.Clear(); break; 
                            }

                            //update the weight of the edge
                            eList[current_node, neighb].weight += temp;
                            eList[current_node, neighb].eDist = getEucledianDist(index, neighbor);

                            if (maxweight < eList[current_node, neighb].weight) 
                                maxweight =  eList[current_node, neighb].weight;
                            
                            //Console.WriteLine(current_node   + "," +  vList[current_node].visited  + ":" +  neighbor  + "," +  vList[neighbor].visited  + "::" + (int)eList[current_node, neighb].weight); 

                            //find the reverse edge and update it
                            for (int r = 1; r <= degList[neighbor]; r++)
                            {
                                if (eList[neighbor, r].nodeId == current_node)
                                {
                                    eList[neighbor, r].weight += temp;
                                    eList[neighbor, r].eDist = getEucledianDist(index, neighbor);
                                }
                            }//endfor
                        }//endif                          
                    }//endfor
                }
                q.Clear();
                for (int j = 1; j <= numOfnodes; j++) vList[j].visited = false;
            }
        }
        public double getWeight(int a, int b, int c, int w)
        {
            double w1 = 0, w2 = 0;
            double d1 =  Math.Sqrt((vList[a].x_loc - vList[b].x_loc) * (vList[a].x_loc - vList[b].x_loc) + (vList[a].y_loc - vList[b].y_loc) * (vList[a].y_loc - vList[b].y_loc));
            double d2 = Math.Sqrt((vList[a].x_loc - vList[c].x_loc) * (vList[a].x_loc - vList[c].x_loc) + (vList[a].y_loc - vList[c].y_loc) * (vList[a].y_loc - vList[c].y_loc));
            double d3 = Math.Sqrt((vList[b].x_loc - vList[c].x_loc) * (vList[b].x_loc - vList[c].x_loc) + (vList[b].y_loc - vList[c].y_loc) * (vList[b].y_loc - vList[c].y_loc));

            //d = Math.Abs(vList[a].x_loc - vList[b].x_loc) / 2 + Math.Abs(vList[a].y_loc - vList[b].y_loc);

            if (vList[a].ID==vList[b].ID) return 1000;
            

            //distribute around a disk of radious 
            double sigma = 5;// Math.Sqrt(w);

            //return w - d;
            w1 = w * (Math.Exp(-(d1 * d1 / (2 * sigma * sigma))) / (sigma * Math.Sqrt(2 * Math.PI))); ;
            w2 = w * (Math.Exp(-(d2 * d2 / (2 * sigma * sigma))) / (sigma * Math.Sqrt(2 * Math.PI))); ;
            return (w1+w2)/2;
        }
        public double getEucledianDist(int a, int b)
        {
            return Math.Sqrt((vList[a].x_loc - vList[b].x_loc) * (vList[a].x_loc - vList[b].x_loc) + (vList[a].y_loc - vList[b].y_loc) * (vList[a].y_loc - vList[b].y_loc));             
        }
        public void plotAllEdges(Point [] pt, Network G, int num_points, int numOfLevels)
        {

            DijkstraAlgo p = new DijkstraAlgo();
            SteinerTree t = new SteinerTree();
            double eDist = 0;           
            bool PlanarPath = false;
            
            
            //COMPUTE RECURSIVE STEINER
            for(int level = 1;level <= numOfLevels; level ++)
            {
                sNet = t.ComputeTree(vList, eList, degList, numOfnodes, sNet, level);                
            }

            for (int i = 1; i <= numOfnodes; i++) vList[i].cID = 0;
            foreach (vertex w in sNet.v) w.cID = 1;

            Console.WriteLine("Compute the local neighborhood: Shortcut Mesh.");
            computeShortcutMesh(pt,num_points);

            Console.WriteLine("Compute edge routes.");
            //COMPUTE EDGE ROUTES
            DijkstraAlgo dijkstra = new DijkstraAlgo();
            
            for (int i = 1; i <= num_points; i++)
            {
                for (int j = i + 1; j <= num_points; j++)
                {
                    if(G.M[i,j]==1)
                        dijkstra.selectShortestPath(vList, eList, degList, pt[i].grid_point, pt[j].grid_point, numOfnodes);
                    foreach (VertexNeighbor e in dijkstra.edgelist)
                    {
                        eList[e.a, e.neighbor].used++;
                        sNet.segments.Add(new VertexNeighbor(e.a, e.neighbor));

                    }
                }
            }


            //LOCAL REFINEMENTS
            Console.WriteLine("Remove Deg-2 vertices when possible.");
            removeDeg2(pt,num_points);

            Console.WriteLine("Move towards center of mass.");           
            MoveToMass(pt, num_points);
                    
        }

        public void MoveToMass(Point[] pt, int num_points)
        {
            int numNeighbors = 0;
            int[,] listNeighbors = new int[20, 3];
            double d1, d2, d3, edge_node_separation = 1;
            bool adjust = true;
            bool localRefinementsFound = true;
            int iteration = 100;


            while (localRefinementsFound && iteration > 0)
            {
                iteration--;
                localRefinementsFound = false;
                foreach (vertex w in sNet.v)
                {
                    numNeighbors = 0;
                    if (w.weight > 0) continue;
                    for (int k = 1; k <= degList[w.ID]; k++)
                    {
                        if (eList[w.ID, k].used > 0)
                        {
                            numNeighbors++;
                            listNeighbors[numNeighbors, 1] = eList[w.ID, k].nodeId;
                            listNeighbors[numNeighbors, 2] = k;
                        }
                    }

                    if (numNeighbors == 2)
                    {

                    }
                }
            }
        }


        public void removeDeg2(Point [] pt,int num_points)
        {
            int numNeighbors = 0;
            int[,] listNeighbors = new int[20, 3];
            double d1, d2, d3, edge_node_separation = 1;
            bool adjust = true;
            bool localRefinementsFound = true;
            int iteration = 100;


            while (localRefinementsFound && iteration > 0)
            {
                iteration--;
                localRefinementsFound = false;
                foreach (vertex w in sNet.v)
                {
                    numNeighbors = 0;
                    if (w.weight > 0) continue;
                    for (int k = 1; k <= degList[w.ID]; k++)
                    {
                        if (eList[w.ID, k].used > 0)
                        {
                            numNeighbors++;
                            listNeighbors[numNeighbors, 1] = eList[w.ID, k].nodeId;
                            listNeighbors[numNeighbors, 2] = k;
                        }
                    }

                    if (numNeighbors == 2)
                    {

                        adjust = true;
                        //add the edge if they are not very close to a point
                        for (int index = 1; index <= num_points; index++)
                        {
                            if ((pt[index].x == vList[listNeighbors[1, 1]].x_loc && pt[index].y == vList[listNeighbors[1, 1]].y_loc)
                                || (pt[index].x == vList[listNeighbors[1, 2]].x_loc && pt[index].y == vList[listNeighbors[1, 2]].y_loc)) continue;

                            d1 = (pt[index].x - vList[listNeighbors[1, 1]].x_loc) * (pt[index].x - vList[listNeighbors[1, 1]].x_loc)
                                + (pt[index].y - vList[listNeighbors[1, 1]].y_loc) * (pt[index].y - vList[listNeighbors[1, 1]].y_loc);
                            d2 = (pt[index].x - vList[listNeighbors[2, 1]].x_loc) * (pt[index].x - vList[listNeighbors[2, 1]].x_loc)
                                + (pt[index].y - vList[listNeighbors[2, 1]].y_loc) * (pt[index].y - vList[listNeighbors[2, 1]].y_loc);
                            d3 = (vList[listNeighbors[1, 1]].x_loc - vList[listNeighbors[2, 1]].x_loc) * (vList[listNeighbors[1, 1]].x_loc - vList[listNeighbors[2, 1]].x_loc)
                                + (vList[listNeighbors[1, 1]].y_loc - vList[listNeighbors[2, 1]].y_loc) * (vList[listNeighbors[1, 1]].y_loc - vList[listNeighbors[2, 1]].y_loc);
                            if (d1 + d2 < d3 + edge_node_separation) adjust = false;
                        }
                        if (adjust)
                        {
                            //Console.WriteLine(w.ID + " :: " + listNeighbors[2, 1] + " " + listNeighbors[1, 1]);
                            localRefinementsFound = true;

                            for (int j = 1; j <= degList[listNeighbors[2, 1]]; j++)
                            {
                                if (eList[listNeighbors[2, 1], j].nodeId == w.ID)
                                {
                                    adjust = true;
                                    //check if it already exists in the neighbor list
                                    for (int check = 1; check <= degList[listNeighbors[2, 1]]; check++)
                                        if (eList[listNeighbors[2, 1], check].nodeId == listNeighbors[1, 1])
                                        {
                                            eList[listNeighbors[2, 1], check].selected = eList[w.ID, listNeighbors[2, 2]].selected;
                                            eList[listNeighbors[2, 1], check].used = eList[w.ID, listNeighbors[2, 2]].used;
                                            eList[listNeighbors[2, 1], j].selected = 0;
                                            eList[listNeighbors[2, 1], j].used = 0;
                                            adjust = false;
                                        }

                                    if (adjust) eList[listNeighbors[2, 1], j].nodeId = listNeighbors[1, 1];

                                }
                            }

                            for (int i = 1; i <= degList[listNeighbors[1, 1]]; i++)
                            {
                                if (eList[listNeighbors[1, 1], i].nodeId == w.ID)
                                {
                                    adjust = true;
                                    //check if it already exists in the neighbor list
                                    for (int check = 1; check <= degList[listNeighbors[1, 1]]; check++)
                                        if (eList[listNeighbors[1, 1], check].nodeId == listNeighbors[2, 1])
                                        {
                                            eList[listNeighbors[1, 1], check].selected = eList[w.ID, listNeighbors[1, 2]].selected;
                                            eList[listNeighbors[1, 1], check].used = eList[w.ID, listNeighbors[1, 2]].used;
                                            eList[listNeighbors[1, 1], i].selected = 0;
                                            eList[listNeighbors[1, 1], i].used = 0;
                                            adjust = false;
                                        }

                                    if (adjust) eList[listNeighbors[1, 1], i].nodeId = listNeighbors[2, 1];
                                }
                            }



                            //delete old edges

                            eList[w.ID, listNeighbors[1, 2]].selected = 0;
                            eList[w.ID, listNeighbors[1, 2]].used = 0;
                            eList[w.ID, listNeighbors[2, 2]].selected = 0;
                            eList[w.ID, listNeighbors[2, 2]].used = 0;

                            //recursive calls


                        }
                    }

                }
            }

        }
        public void computeShortcutMesh(Point []pt, int num_points)
        {


            //COMPUTE NEIGHBORHOOD SHORTCUTS
            int x = 0, y = 0, neighb = 0;
            for (int i = num_points; i >= 1; i--)
            {
                x = pt[i].x;
                y = pt[i].y;

                //if v_i has a neighbor in the first (top right) quadrant 
                while (x + 1 < N && y + 1 < N && nodeMap[x + 1, y + 1] > 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x + 1, y + 1]].ID) break;
                    }
                    if (eList[nodeMap[x, y], neighb].selected == 0) break;
                    x = x + 1;
                    y = y + 1;
                }
                while (x + 1 < N && y + 1 < N && nodeMap[x + 1, y + 1] > 0 && vList[nodeMap[x + 1, y + 1]].cID == 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x + 1, y + 1]].ID) break;
                    }
                    eList[nodeMap[x, y], neighb].selected = 6;
                    x = x + 1;
                    y = y + 1;
                    vList[nodeMap[x, y]].cID = 1;
                    sNet.v.Add(vList[nodeMap[x, y]]);
                }
                if (x + 1 < N && y + 1 < N && nodeMap[x + 1, y + 1] > 0 && vList[nodeMap[x + 1, y + 1]].cID > 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x + 1, y + 1]].ID) break;
                    }
                    eList[nodeMap[x, y], neighb].selected = 6;
                    x = x + 1;
                    y = y + 1;
                    vList[nodeMap[x, y]].cID = 1;
                    sNet.v.Add(vList[nodeMap[x, y]]);
                }

                x = pt[i].x;
                y = pt[i].y;

                //if v_i has a neighbor in the top left quadrant 
                while (x - 1 > 0 && y + 1 < N && nodeMap[x - 1, y + 1] > 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x - 1, y + 1]].ID) break;
                    }
                    if (eList[nodeMap[x, y], neighb].selected == 0) break;
                    x = x - 1;
                    y = y + 1;
                }
                while (x - 1 > 0 && y + 1 < N && nodeMap[x - 1, y + 1] > 0 && vList[nodeMap[x - 1, y + 1]].cID == 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x - 1, y + 1]].ID) break;
                    }
                    eList[nodeMap[x, y], neighb].selected = 6;
                    x = x - 1;
                    y = y + 1;
                    vList[nodeMap[x, y]].cID = 1;
                    sNet.v.Add(vList[nodeMap[x, y]]);
                }
                if (x - 1 > 0 && y + 1 < N && nodeMap[x - 1, y + 1] > 0 && vList[nodeMap[x - 1, y + 1]].cID > 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x - 1, y + 1]].ID) break;
                    }
                    eList[nodeMap[x, y], neighb].selected = 6;
                    x = x - 1;
                    y = y + 1;
                    vList[nodeMap[x, y]].cID = 1;
                    sNet.v.Add(vList[nodeMap[x, y]]);
                }

                x = pt[i].x;
                y = pt[i].y;

                //if v_i has a neighbor in the bottom right quadrant 
                while (x + 1 < N && y - 1 > 0 && nodeMap[x + 1, y - 1] > 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x + 1, y - 1]].ID) break;
                    }
                    if (eList[nodeMap[x, y], neighb].selected == 0) break;
                    x = x + 1;
                    y = y - 1;
                }
                while (x + 1 < N && y - 1 > 0 && nodeMap[x + 1, y - 1] > 0 && vList[nodeMap[x + 1, y - 1]].cID == 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x + 1, y - 1]].ID) break;
                    }
                    eList[nodeMap[x, y], neighb].selected = 6;
                    x = x + 1;
                    y = y - 1;
                    vList[nodeMap[x, y]].cID = 1;
                    sNet.v.Add(vList[nodeMap[x, y]]);
                }
                if (x + 1 < N && y - 1 > 0 && nodeMap[x + 1, y - 1] > 0 && vList[nodeMap[x + 1, y - 1]].cID > 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x + 1, y - 1]].ID) break;
                    }
                    eList[nodeMap[x, y], neighb].selected = 6;
                    x = x + 1;
                    y = y - 1;
                    vList[nodeMap[x, y]].cID = 1;
                    sNet.v.Add(vList[nodeMap[x, y]]);
                }

                x = pt[i].x;
                y = pt[i].y;

                //if v_i has a neighbor in the bottom-left quadrant 
                while (x - 1 > 0 && y - 1 > 0 && nodeMap[x - 1, y - 1] > 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x - 1, y - 1]].ID) break;
                    }
                    if (eList[nodeMap[x, y], neighb].selected == 0) break;
                    x = x - 1;
                    y = y - 1;
                }
                while (x - 1 > 0 && y - 1 > 0 && nodeMap[x - 1, y - 1] > 0 && vList[nodeMap[x - 1, y - 1]].cID == 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x - 1, y - 1]].ID) break;
                    }
                    eList[nodeMap[x, y], neighb].selected = 6;
                    x = x - 1;
                    y = y - 1;
                    vList[nodeMap[x, y]].cID = 1;
                    sNet.v.Add(vList[nodeMap[x, y]]);
                }
                if (x - 1 > 0 && y - 1 > 0 && nodeMap[x - 1, y - 1] > 0 && vList[nodeMap[x - 1, y - 1]].cID > 0)
                {
                    for (neighb = 1; neighb <= degList[nodeMap[x, y]]; neighb++)
                    {
                        if (eList[nodeMap[x, y], neighb].nodeId == vList[nodeMap[x - 1, y - 1]].ID) break;
                    }
                    eList[nodeMap[x, y], neighb].selected = 6;
                    x = x - 1;
                    y = y - 1;
                    vList[nodeMap[x, y]].cID = 1;
                    sNet.v.Add(vList[nodeMap[x, y]]);
                }

            }
        }
    }
    public class vertex
    {
        public int ID = 0;
        public int cID = 0; //component ID for steiner tree
        public int x_loc = 0;
        public int y_loc = 0;
        public double dist = 0;
        public int weight = 0; // priority
        public int zoomLevel = 0;
        public vertex parent = null;
        public bool visited = false;

    }
    public class edge
    {        
        public double weight = 1;
        public double eDist = 0;
        public int cost = 0;
        public int nodeId = 0;
        public int selected = 0;
        public int used = 0;
        public edge(int z)
        {
            nodeId = z;
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
                        sNet.v.Add(vList[_tuple.a]);
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