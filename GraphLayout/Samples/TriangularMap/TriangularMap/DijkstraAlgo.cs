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
 
        public void selectShortestPath(Vertex [] vList, Edge [,] eList, int [] degList,  int source, int target,int N){


            PriorityQueue<double, Vertex> Q = new PriorityQueue<double, Vertex>(); ;
            double temp;
            int neighbor;
            vList[source].Dist = 0;
            Q.Enqueue(vList[source].Dist, vList[source]);
            for (int i = 1; i < N; i++)
            {
                if ( vList[i].Id != vList[source].Id)
                {
                    vList[i].Dist = double.MaxValue;
                    Q.Enqueue(vList[i].Dist, vList[i]);
                }
                vList[i].Parent = null;
                vList[i].Visited = false;
            }

            edgelist.Clear();
            Distance = 0;

            while (Q.Count > 0)
            {
                Vertex u = (Vertex)Q.Dequeue().Value;
                if (u.Visited == true) continue;
                else u.Visited = true;
                for (int neighb = 1; neighb <= degList[u.Id]; neighb++)
                {
                    neighbor = eList[u.Id,neighb].NodeId;

                    if (eList[u.Id, neighb].Selected == 0) continue;
                    //else encourangeReuse = 0;

                    temp = u.Dist + eList[u.Id, neighb].EDist;
                    if (temp < vList[neighbor].Dist)
                    {
                        vList[neighbor].Dist = temp;
                        vList[neighbor].Parent = u;
                        Q.Enqueue(vList[neighbor].Dist, vList[neighbor]);
                    }
                }
                if (u.Id ==vList[target].Id) 
                    break;
            }

            Vertex route = vList[target];
            
            while (route.Parent != null)
            {
                for (int neighb = 1; neighb <= degList[route.Id]; neighb++) 
                    if (eList[route.Id, neighb].NodeId == route.Parent.Id){
                        //eList[route.ID, neighb].selected = 1;

                        edgelist.Add(new VertexNeighbor(route.Id, neighb));                        
                        Distance += Math.Sqrt((route.XLoc - route.Parent.XLoc) * (route.XLoc - route.Parent.XLoc) + (route.YLoc - route.Parent.YLoc) * (route.YLoc - route.Parent.YLoc));  

                        //encourage reusing of path by decreasing weight
                        //if (eList[route.ID, neighb].weight == 1) eList[route.ID, neighb].weight /= 2;
                        break;
                    }
                for (int neighb = 1; neighb <= degList[route.Parent.Id]; neighb++)
                    if (eList[route.Parent.Id, neighb].NodeId == route.Id)
                    {
                        //eList[route.parent.ID, neighb].selected = 1;

                        edgelist.Add(new VertexNeighbor(route.Parent.Id, neighb));

                        //encourage reusing of path by decreasing weight
                        //if (eList[route.parent.ID, neighb].weight == 1) eList[route.parent.ID, neighb].weight /= 2;
                        break;
                    }
                route = route.Parent;
            }


        }




        public void selectShortestPathAvoidingNet(Vertex[] vList, Edge[,] eList, int[] degList, int source, int target, int N)
        {


            PriorityQueue<double, Vertex> Q = new PriorityQueue<double, Vertex>(); ;
            double temp;
            int neighbor;
            vList[source].Dist = 0;
            Q.Enqueue(vList[source].Dist, vList[source]);
            for (int i = 1; i < N; i++)
            {
                if (vList[i].Id != vList[source].Id)
                {
                    vList[i].Dist = double.MaxValue;
                    Q.Enqueue(vList[i].Dist, vList[i]);
                }
                vList[i].Parent = null;
                vList[i].Visited = false;
            }

            edgelist.Clear();
            Distance = 0;

            while (Q.Count > 0)
            {
                Vertex u = (Vertex)Q.Dequeue().Value;
                if (u.Visited == true) continue;
                else u.Visited = true;
                for (int neighb = 1; neighb <= degList[u.Id]; neighb++)
                {
                    neighbor = eList[u.Id, neighb].NodeId;

                    if (neighbor != source && neighbor != target && vList[neighbor].CId > 0) continue;
                    //else encourangeReuse = 0;

                    temp = u.Dist + eList[u.Id, neighb].EDist;
                    if (temp < vList[neighbor].Dist)
                    {
                        vList[neighbor].Dist = temp;
                        vList[neighbor].Parent = u;
                        Q.Enqueue(vList[neighbor].Dist, vList[neighbor]);
                    }
                }
                if (u.Id == vList[target].Id)
                    break;
            }

            Vertex route = vList[target];

            if (route.Parent == null) { pathExists = false; return; }
            else pathExists = true;

            while (route.Parent != null)
            {
                for (int neighb = 1; neighb <= degList[route.Id]; neighb++)
                    if (eList[route.Id, neighb].NodeId == route.Parent.Id)
                    {
                        //eList[route.ID, neighb].selected = 1;

                        edgelist.Add(new VertexNeighbor(route.Id, neighb));
                        Distance += Math.Sqrt((route.XLoc - route.Parent.XLoc) * (route.XLoc - route.Parent.XLoc) + (route.YLoc - route.Parent.YLoc) * (route.YLoc - route.Parent.YLoc));

                        //encourage reusing of path by decreasing weight
                        //if (eList[route.ID, neighb].weight == 1) eList[route.ID, neighb].weight /= 2;
                        break;
                    }
                for (int neighb = 1; neighb <= degList[route.Parent.Id]; neighb++)
                    if (eList[route.Parent.Id, neighb].NodeId == route.Id)
                    {
                        //eList[route.parent.ID, neighb].selected = 1;

                        edgelist.Add(new VertexNeighbor(route.Parent.Id, neighb));

                        //encourage reusing of path by decreasing weight
                        //if (eList[route.parent.ID, neighb].weight == 1) eList[route.parent.ID, neighb].weight /= 2;
                        break;
                    }
                route = route.Parent;
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
