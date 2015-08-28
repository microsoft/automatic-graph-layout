using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.GraphmapsWithMesh
{


    /*
     * The only function we now use is MsaglAstarShortestPath - which I am trying to 
     * replace with the shortest path function available in the visibilityGraph context
     */
    class DijkstraAlgo
    {
        public List<VertexNeighbor> Edgelist = new List<VertexNeighbor>();
        public List<int> ShortestPath = new List<int>();
        public double Distance;
        public bool PathExists = true;

        public List<int> MsaglAstarShortestPath(Vertex[] vList, Edge[,] eList, int[] degList, int source, int target, int n)
        {

            var q = new BinaryHeapPriorityQueue(n);


            vList[source].Dist = 0;
            vList[source].Weight = vList[source].Dist +
                                   MsaglUtilities.EucledianDistance(
                                   vList[source].XLoc, vList[source].YLoc,
                                   vList[target].XLoc, vList[target].YLoc);
            q.Enqueue(source, vList[source].Weight);

            for (int i = 0; i < n; i++)
            {
                if (vList[i].Id != source)
                {
                    vList[i].Dist = double.MaxValue;
                    vList[i].Weight = double.MaxValue;
                    q.Enqueue(i, vList[i].Weight);
                }
                vList[i].Parent = null;
                vList[i].Visited = false;
            }

            Edgelist.Clear();
            ShortestPath.Clear();
            Distance = 0;

            while (q.Count > 0)
            {
                Vertex u = vList[q.Dequeue()];
                u.Visited = true;
                if (u.Invalid) return new List<int>();
                for (int neighb = 0; neighb < degList[u.Id]; neighb++)
                {
                    var neighborId = eList[u.Id, neighb].NodeId;
                    int discourage = 0;
                    if (eList[u.Id, neighb].Selected == 1) discourage = 1000;//continue;
                    if (eList[u.Id, neighb].NodeId == source || eList[u.Id, neighb].NodeId == target) discourage = 0;
                    if (u.Id == source || u.Id == target) discourage = 0;

                    Vertex neighbor = vList[neighborId];
                    double edist = MsaglUtilities.EucledianDistance(u.XLoc, u.YLoc, neighbor.XLoc, neighbor.YLoc);
                    var tempDist = u.Dist + edist + discourage;
                    if (tempDist >= neighbor.Dist) continue;

                    neighbor.Dist = tempDist;
                    var tempWeight = neighbor.Dist + MsaglUtilities.EucledianDistance(vList[target].XLoc, vList[target].YLoc, neighbor.XLoc, neighbor.YLoc);
                    neighbor.Weight = tempWeight;
                    neighbor.Parent = u;
                    if (neighbor.Visited)
                    {
                        neighbor.Visited = false;
                        q.Enqueue(neighbor.Id, neighbor.Weight);
                    }
                    else q.DecreasePriority(neighbor.Id, neighbor.Weight);
                }
                if (u.Id == target)
                    break;
            }

            Vertex route = vList[target];
            int zoomlevel = Math.Max(vList[source].ZoomLevel, vList[target].ZoomLevel);
            while (route.Parent != null)
            {
                ShortestPath.Add(route.Id);
                for (int neighb = 0; neighb < degList[route.Id]; neighb++)
                    if (eList[route.Id, neighb].NodeId == route.Parent.Id)
                    {
                        SetUsed(vList, eList, degList, route.Id, eList[route.Id, neighb].NodeId, zoomlevel);
                        Edgelist.Add(new VertexNeighbor(route.Id, neighb));

                        Distance += Math.Sqrt((route.XLoc - route.Parent.XLoc) * (route.XLoc - route.Parent.XLoc) + (route.YLoc - route.Parent.YLoc) * (route.YLoc - route.Parent.YLoc));

                        break;
                    }

                route = route.Parent;
            }
            ShortestPath.Add(route.Id);
            if (route.Id != source) Console.WriteLine("path not found");
            return ShortestPath;
        }
         




















        public Vertex GetMin(List<Vertex> q)
        {
            double m = double.MaxValue;
            Vertex tempVertex = null;
            foreach (Vertex w in q)
            {
                if (m > w.Dist)
                {
                    m = w.Dist;
                    tempVertex = w;
                }
            }
            if (tempVertex == null)
            {
                Console.WriteLine("Graph is disconnected");
                return null;
            }
            if (q.Any(w => w.Id == tempVertex.Id))
            {
                q.Remove(tempVertex);
            }

            return tempVertex;
        }

        public void SelectShortestPath(Vertex[] vList, Edge[,] eList, int[] degList, int source, int target, int n)
        {


            var q = new List<Vertex>();
            vList[source].Dist = 0;
            q.Add(vList[source]);
            for (int i = 1; i <= n; i++)
            {
                if (vList[i].Id != vList[source].Id)
                {
                    vList[i].Dist = double.MaxValue;
                    q.Add(vList[i]);
                }
                vList[i].Parent = null;
                vList[i].Visited = false;
            }

            Edgelist.Clear();
            Distance = 0;

            while (q.Count > 0)
            {
                Vertex u = GetMin(q);
                if (u.Visited) continue;
                u.Visited = true;
                for (int neighb = 1; neighb <= degList[u.Id]; neighb++)
                {
                    var neighbor = eList[u.Id, neighb].NodeId;

                    int discourage = 0;
                    if (eList[u.Id, neighb].Selected == 0) discourage = 1000;//continue;
                    if (vList[eList[u.Id, neighb].NodeId].Weight > 0 &&
                         eList[u.Id, neighb].NodeId != vList[target].Id) continue;


                    var temp = u.Dist + eList[u.Id, neighb].GetEDist(vList, u.Id, eList[u.Id, neighb].NodeId) + discourage;

                    if (temp < vList[neighbor].Dist)
                    {
                        vList[neighbor].Dist = temp;
                        vList[neighbor].Parent = u;
                        //Update(q,  vList[neighbor]);
                    }
                }
                if (u.Id == vList[target].Id)
                    break;
            }

            Vertex route = vList[target];

            //Console.WriteLine();
            //Console.WriteLine("Source,Target" + source + " " + target);
            while (route.Parent != null)
            {
                //Console.Write(route.Id+" ");
                for (int neighb = 1; neighb <= degList[route.Id]; neighb++)
                    if (eList[route.Id, neighb].NodeId == route.Parent.Id)
                    {

                        Edgelist.Add(new VertexNeighbor(route.Id, neighb));

                        Distance += Math.Sqrt((route.XLoc - route.Parent.XLoc) * (route.XLoc - route.Parent.XLoc) + (route.YLoc - route.Parent.YLoc) * (route.YLoc - route.Parent.YLoc));

                        break;
                    }
                for (int neighb = 1; neighb <= degList[route.Parent.Id]; neighb++)
                    if (eList[route.Parent.Id, neighb].NodeId == route.Id)
                    {

                        Edgelist.Add(new VertexNeighbor(route.Parent.Id, neighb));

                        break;
                    }
                route = route.Parent;
            }
            //Console.Write(route.Id + " ");
            if (route.Id != vList[source].Id) Console.WriteLine("path not found");

        }



        public void MsaglSelectShortestPath(Vertex[] vList, Edge[,] eList, int[] degList, int source, int target, int n)
        {

            var q = new List<Vertex>();
            vList[source].Dist = 0;
            q.Add(vList[source]);
            for (int i = 0; i <  n; i++)
            {
                if (vList[i].Id != source )
                {
                    vList[i].Dist = double.MaxValue;
                    q.Add(vList[i]);
                }
                vList[i].Parent = null;
                vList[i].Visited = false;
            }

            Edgelist.Clear();
            Distance = 0;

            while (q.Count > 0)
            {
                Vertex u = GetMin(q);
                if (u == null) return;
                if (u.Visited) continue;
                u.Visited = true;
                for (int neighb = 0; neighb < degList[u.Id]; neighb++)
                {
                    var neighbor = eList[u.Id, neighb].NodeId;
                    if (vList[neighbor].Visited) continue;
                    int discourage = 0;
                    if (eList[u.Id, neighb].Selected == 1  )discourage = 1000;//continue;
                    if (eList[u.Id, neighb].NodeId == source || eList[u.Id, neighb].NodeId == target) discourage = 0;
                    if (u.Id == source || u.Id == target) discourage = 0;


                    var temp = u.Dist + eList[u.Id, neighb].GetEDist(vList, u.Id, eList[u.Id, neighb].NodeId) + discourage;

                    if (temp < vList[neighbor].Dist)
                    {
                        vList[neighbor].Dist = temp;
                        vList[neighbor].Parent = u;
                        //Update(q,  vList[neighbor]);
                    }
                }
                if (u.Id ==  target )
                    break;
            }

            Vertex route = vList[target];

             
            while (route.Parent != null)
            {
                //Console.Write(route.Id+" ");
                for (int neighb = 0; neighb <  degList[route.Id]; neighb++)
                    if (eList[route.Id, neighb].NodeId == route.Parent.Id)
                    {
                        SetUsed( vList,  eList,   degList,route.Id, eList[route.Id, neighb].NodeId, 1);
                        Edgelist.Add(new VertexNeighbor(route.Id, neighb));

                        Distance += Math.Sqrt((route.XLoc - route.Parent.XLoc) * (route.XLoc - route.Parent.XLoc) + (route.YLoc - route.Parent.YLoc) * (route.YLoc - route.Parent.YLoc));

                        break;
                    }
                
                route = route.Parent;
            }
            
            if (route.Id !=  source ) Console.WriteLine("path not found");

        }
         
        public void SetUsed(Vertex[] vList, Edge[,] eList, int[] degList, int a, int b, int zoomlevel)
        {
            for (int index = 0; index <  degList[a]; index++)
            {
                if ( eList[a, index].NodeId == b)
                {
                    if (eList[a, index].Used > 0) return;
                    eList[a, index].Used = zoomlevel;
                }
            }
            for (int index = 0; index <  degList[b]; index++)
            {
                if (eList[b, index].NodeId == a)
                {
                    if (eList[b, index].Used > 0) return;
                    eList[b, index].Used = zoomlevel;
                }
            }
        }
        /*     
             public void SelectShortestPath(Vertex [] vList, Edge [,] eList, int [] degList,  int source, int target,int n){


                 PriorityQueue<double, Vertex> q = new PriorityQueue<double, Vertex>();
                 vList[source].Dist = 0;
                 q.Enqueue(vList[source].Dist, vList[source]);
                 for (int i = 1; i <= n; i++)
                 {
                     if ( vList[i].Id != vList[source].Id)
                     {
                         vList[i].Dist = double.MaxValue;
                         q.Enqueue(vList[i].Dist, vList[i]);
                     }
                     vList[i].Parent = null;
                     vList[i].Visited = false;
                 }

                 Edgelist.Clear();
                 Distance = 0;

                 while (q.Count > 0)
                 {
                     Vertex u = q.Dequeue().Value;
                     if (u.Visited) continue;
                     else u.Visited = true;
                     for (int neighb = 1; neighb <= degList[u.Id]; neighb++)
                     {
                         var neighbor = eList[u.Id,neighb].NodeId;

                         int discourage = 0;
                         if (eList[u.Id, neighb].Selected == 0) discourage = 10000;//continue;
                         if (vList[eList[u.Id, neighb].NodeId].Weight> 0 &&
                              eList[u.Id, neighb].NodeId  != target) continue;


                         var temp = u.Dist + eList[u.Id, neighb].getEDist(vList, u.Id, eList[u.Id, neighb].NodeId) + discourage;

                         if (temp < vList[neighbor].Dist)
                         {
                             vList[neighbor].Dist = temp;
                             vList[neighbor].Parent = u;
                             q.Enqueue(vList[neighbor].Dist, vList[neighbor]);
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

                             Edgelist.Add(new VertexNeighbor(route.Id, neighb));
                        
                             Distance += Math.Sqrt((route.XLoc - route.Parent.XLoc) * (route.XLoc - route.Parent.XLoc) + (route.YLoc - route.Parent.YLoc) * (route.YLoc - route.Parent.YLoc));  

                             //encourage reusing of path by decreasing weight
                             //if (eList[route.ID, neighb].weight == 1) eList[route.ID, neighb].weight /= 2;
                             break;
                         }
                     for (int neighb = 1; neighb <= degList[route.Parent.Id]; neighb++)
                         if (eList[route.Parent.Id, neighb].NodeId == route.Id)
                         {
                             //eList[route.parent.ID, neighb].selected = 1;

                             Edgelist.Add(new VertexNeighbor(route.Parent.Id, neighb));

                             //encourage reusing of path by decreasing weight
                             //if (eList[route.parent.ID, neighb].weight == 1) eList[route.parent.ID, neighb].weight /= 2;
                             break;
                         }
                     route = route.Parent;
                 }
                 if(route.Id != source) Console.WriteLine("path not found");

             }



        public void SelectShortestPathAvoidingNet(Vertex[] vList, Edge[,] eList, int[] degList, int source, int target, int n)
        {


            var q = new PriorityQueue<double, Vertex>();
            vList[source].Dist = 0;
            q.Enqueue(vList[source].Dist, vList[source]);
            for (int i = 1; i < n; i++)
            {
                if (vList[i].Id != vList[source].Id)
                {
                    vList[i].Dist = double.MaxValue;
                    q.Enqueue(vList[i].Dist, vList[i]);
                }
                vList[i].Parent = null;
                vList[i].Visited = false;
            }

            Edgelist.Clear();
            Distance = 0;

            while (q.Count > 0)
            {
                Vertex u = q.Dequeue().Value;
                if (u.Visited) continue;
                u.Visited = true;
                for (int neighb = 1; neighb <= degList[u.Id]; neighb++)
                {
                    var neighbor = eList[u.Id, neighb].NodeId;

                    if (neighbor != source && neighbor != target && vList[neighbor].CId > 0) continue;
                    //else encourangeReuse = 0;

                    var temp = u.Dist + eList[u.Id, neighb].GetEDist(vList, u.Id, eList[u.Id, neighb].NodeId);
                    if (temp < vList[neighbor].Dist)
                    {
                        vList[neighbor].Dist = temp;
                        vList[neighbor].Parent = u;
                        q.Enqueue(vList[neighbor].Dist, vList[neighbor]);
                    }
                }
                if (u.Id == vList[target].Id)
                    break;
            }

            Vertex route = vList[target];

            if (route.Parent == null) { PathExists = false; return; }
            PathExists = true;

            while (route.Parent != null)
            {
                for (int neighb = 1; neighb <= degList[route.Id]; neighb++)
                    if (eList[route.Id, neighb].NodeId == route.Parent.Id)
                    {
                        //eList[route.ID, neighb].selected = 1;

                        Edgelist.Add(new VertexNeighbor(route.Id, neighb));
                        Distance += Math.Sqrt((route.XLoc - route.Parent.XLoc) * (route.XLoc - route.Parent.XLoc) + (route.YLoc - route.Parent.YLoc) * (route.YLoc - route.Parent.YLoc));

                        //encourage reusing of path by decreasing weight
                        //if (eList[route.ID, neighb].weight == 1) eList[route.ID, neighb].weight /= 2;
                        break;
                    }
                for (int neighb = 1; neighb <= degList[route.Parent.Id]; neighb++)
                    if (eList[route.Parent.Id, neighb].NodeId == route.Id)
                    {
                        //eList[route.parent.ID, neighb].selected = 1;

                        Edgelist.Add(new VertexNeighbor(route.Parent.Id, neighb));

                        //encourage reusing of path by decreasing weight
                        //if (eList[route.parent.ID, neighb].weight == 1) eList[route.parent.ID, neighb].weight /= 2;
                        break;
                    }
                route = route.Parent;
            }


        }
         * 
     */
    }
    public class VertexNeighbor{
        public int A;
        public int Neighbor;
        public VertexNeighbor(int p, int q)
        {
            A = p; Neighbor = q;
        }
    }
}
