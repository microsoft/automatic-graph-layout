using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication3
{
    class DijkstraAlgo
    {
        public List<VertexNeighbor> edgelist = new List<VertexNeighbor>();
        public double Distance = 0;
        public bool pathExists = true;
 
        public void selectShortestPath(vertex [] vList, edge [,] eList, int [] degList,  int source, int target,int N){


            PriorityQueue<double, vertex> Q = new PriorityQueue<double, vertex>(); ;
            double temp;
            int neighbor;
            vList[source].dist = 0;
            Q.Enqueue(vList[source].dist, vList[source]);
            for (int i = 1; i < N; i++)
            {
                if ( vList[i].ID != vList[source].ID)
                {
                    vList[i].dist = double.MaxValue;
                    Q.Enqueue(vList[i].dist, vList[i]);
                }
                vList[i].parent = null;
                vList[i].visited = false;
            }

            edgelist.Clear();
            Distance = 0;

            while (Q.Count > 0)
            {
                vertex u = (vertex)Q.Dequeue().Value;
                if (u.visited == true) continue;
                else u.visited = true;
                for (int neighb = 1; neighb <= degList[u.ID]; neighb++)
                {
                    neighbor = eList[u.ID,neighb].nodeId;

                    if (eList[u.ID, neighb].selected == 0) continue;
                    //else encourangeReuse = 0;

                    temp = u.dist + eList[u.ID, neighb].eDist;
                    if (temp < vList[neighbor].dist)
                    {
                        vList[neighbor].dist = temp;
                        vList[neighbor].parent = u;
                        Q.Enqueue(vList[neighbor].dist, vList[neighbor]);
                    }
                }
                if (u.ID ==vList[target].ID) 
                    break;
            }

            vertex route = vList[target];
            
            while (route.parent != null)
            {
                for (int neighb = 1; neighb <= degList[route.ID]; neighb++) 
                    if (eList[route.ID, neighb].nodeId == route.parent.ID){
                        //eList[route.ID, neighb].selected = 1;

                        edgelist.Add(new VertexNeighbor(route.ID, neighb));                        
                        Distance += Math.Sqrt((route.x_loc - route.parent.x_loc) * (route.x_loc - route.parent.x_loc) + (route.y_loc - route.parent.y_loc) * (route.y_loc - route.parent.y_loc));  

                        //encourage reusing of path by decreasing weight
                        //if (eList[route.ID, neighb].weight == 1) eList[route.ID, neighb].weight /= 2;
                        break;
                    }
                for (int neighb = 1; neighb <= degList[route.parent.ID]; neighb++)
                    if (eList[route.parent.ID, neighb].nodeId == route.ID)
                    {
                        //eList[route.parent.ID, neighb].selected = 1;

                        edgelist.Add(new VertexNeighbor(route.parent.ID, neighb));

                        //encourage reusing of path by decreasing weight
                        //if (eList[route.parent.ID, neighb].weight == 1) eList[route.parent.ID, neighb].weight /= 2;
                        break;
                    }
                route = route.parent;
            }


        }




        public void selectShortestPathAvoidingNet(vertex[] vList, edge[,] eList, int[] degList, int source, int target, int N)
        {


            PriorityQueue<double, vertex> Q = new PriorityQueue<double, vertex>(); ;
            double temp;
            int neighbor;
            vList[source].dist = 0;
            Q.Enqueue(vList[source].dist, vList[source]);
            for (int i = 1; i < N; i++)
            {
                if (vList[i].ID != vList[source].ID)
                {
                    vList[i].dist = double.MaxValue;
                    Q.Enqueue(vList[i].dist, vList[i]);
                }
                vList[i].parent = null;
                vList[i].visited = false;
            }

            edgelist.Clear();
            Distance = 0;

            while (Q.Count > 0)
            {
                vertex u = (vertex)Q.Dequeue().Value;
                if (u.visited == true) continue;
                else u.visited = true;
                for (int neighb = 1; neighb <= degList[u.ID]; neighb++)
                {
                    neighbor = eList[u.ID, neighb].nodeId;

                    if (neighbor != source && neighbor != target && vList[neighbor].cID > 0) continue;
                    //else encourangeReuse = 0;

                    temp = u.dist + eList[u.ID, neighb].eDist;
                    if (temp < vList[neighbor].dist)
                    {
                        vList[neighbor].dist = temp;
                        vList[neighbor].parent = u;
                        Q.Enqueue(vList[neighbor].dist, vList[neighbor]);
                    }
                }
                if (u.ID == vList[target].ID)
                    break;
            }

            vertex route = vList[target];

            if (route.parent == null) { pathExists = false; return; }
            else pathExists = true;

            while (route.parent != null)
            {
                for (int neighb = 1; neighb <= degList[route.ID]; neighb++)
                    if (eList[route.ID, neighb].nodeId == route.parent.ID)
                    {
                        //eList[route.ID, neighb].selected = 1;

                        edgelist.Add(new VertexNeighbor(route.ID, neighb));
                        Distance += Math.Sqrt((route.x_loc - route.parent.x_loc) * (route.x_loc - route.parent.x_loc) + (route.y_loc - route.parent.y_loc) * (route.y_loc - route.parent.y_loc));

                        //encourage reusing of path by decreasing weight
                        //if (eList[route.ID, neighb].weight == 1) eList[route.ID, neighb].weight /= 2;
                        break;
                    }
                for (int neighb = 1; neighb <= degList[route.parent.ID]; neighb++)
                    if (eList[route.parent.ID, neighb].nodeId == route.ID)
                    {
                        //eList[route.parent.ID, neighb].selected = 1;

                        edgelist.Add(new VertexNeighbor(route.parent.ID, neighb));

                        //encourage reusing of path by decreasing weight
                        //if (eList[route.parent.ID, neighb].weight == 1) eList[route.parent.ID, neighb].weight /= 2;
                        break;
                    }
                route = route.parent;
            }


        }
    }
    public class VertexNeighbor{
        public int a;
        public int neighbor;
        public VertexNeighbor(int p, int q)
        {
            a = p; neighbor = q;
        }
    }
}
