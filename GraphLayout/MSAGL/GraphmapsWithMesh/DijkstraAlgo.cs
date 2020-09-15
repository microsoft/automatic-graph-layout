using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.GraphmapsWithMesh
{
    class DijkstraAlgo
    {
        public List<VertexNeighbor> Edgelist = new List<VertexNeighbor>();
        public List<int> ShortestPath = new List<int>();
        public double Distance;
        public bool PathExists = true;


        public Vertex getMin(List<Vertex> q)
        {
            double m = double.MaxValue;
            Vertex temp_vertex = null;
            foreach (Vertex w in q)
            {
                if (m > w.Dist)
                {
                    m = w.Dist;
                    temp_vertex = w;
                }
            }
            if (temp_vertex == null)
            {
                return null; // Graph is disconnected
            }
            foreach (Vertex w in q)
            {
                if (w.Id == temp_vertex.Id)
                {
                    q.Remove(temp_vertex);
                    break;
                }
            }

            return temp_vertex;
        }

        public void SelectShortestPath(Vertex[] vList, Edge[,] eList, int[] degList, int source, int target, int n)
        {


            List<Vertex> q = new List<Vertex>();
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
                Vertex u = getMin(q);
                if (u.Visited) continue;
                else u.Visited = true;
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

            while (route.Parent != null)
            {
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
            if (route.Id != vList[source].Id) 
                System.Diagnostics.Debug.WriteLine("path not found");

        }


        public List<int> MSAGLAstarShortestPath(Vertex[] vList, Edge[,] eList, int[] degList, int source, int target, int n)
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
                var deq = q.Dequeue();
                Vertex u = vList[deq];
                u.Visited = true;
                if (u == null || u.Invalid) return new List<int>();
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
            if (route.Id != source)
                System.Diagnostics.Debug.WriteLine("path not found");
            return ShortestPath;
        }


        public void MSAGLGreedy(Vertex[] vList, Edge[,] eList, int[] degList, int source, int target, int n)
        {

            for (int i = 0; i < n; i++)
            {
                vList[i].Parent = null;
                vList[i].Visited = false;
            }

            Edgelist.Clear();
            ShortestPath.Clear();
            Distance = 0;

            Vertex currentVertex = vList[source];
            int bestN;
            double bestD, temp, discourage=0;
            while (true)
            {
                if (currentVertex == null || currentVertex.Invalid) return;
                currentVertex.Visited = true;
                bestD = Double.MaxValue;
                bestN = -1;
                for (int neighb = 0; neighb < degList[currentVertex.Id]; neighb++)
                {
                    var neighbor = eList[currentVertex.Id, neighb].NodeId;
                    if (vList[neighbor].Visited) continue;
                    if (eList[currentVertex.Id, neighb].Selected == 1) discourage = 1000; //continue;
                    else discourage = 0;
                    temp = MsaglUtilities.EucledianDistance(vList[neighbor].XLoc, vList[neighbor].YLoc,
                        vList[target].XLoc, vList[target].YLoc) + discourage;
                    if (temp < bestD)
                    {
                        bestD = temp;
                        bestN = neighbor;
                    }

                }
                if (currentVertex.Id == target)
                    break;
                if (bestN == -1)
                {
                    MSAGLAstarShortestPath(vList, eList, degList, source, target, n);
                    return;
                }
                vList[bestN].Parent = currentVertex;
                currentVertex = vList[bestN];
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


                        break;
                    }

                route = route.Parent;
            }
            ShortestPath.Add(route.Id);
            if (route.Id != source)
                System.Diagnostics.Debug.WriteLine("path not found");
            return;
        }

        public void MSAGLAstarSSSP(Vertex[] vList, Edge[,] eList, int[] degList, int source, Dictionary<Node, int> nodeId, GeometryGraph _mainGeometryGraph,
            int NofNodesBeforeDetour, int n, Tiling g, Tiling g1)
        {

            var q = new BinaryHeapPriorityQueue(n);

             

            vList[source].Dist = 0;
            q.Enqueue(source, vList[source].Dist);

            for (int i = 0; i < n; i++)
            {
                if (vList[i].Id != source)
                {
                    vList[i].Dist = double.MaxValue;
                    q.Enqueue(i, vList[i].Dist);
                }
                vList[i].Parent = null;
                vList[i].Visited = false;
            }


            Distance = 0;

            while (q.Count > 0)
            {
                var deq = q.Dequeue();
                Vertex u = vList[deq];
                u.Visited = true;
                if (u == null || u.Invalid) return;
                for (int neighb = 0; neighb < degList[u.Id]; neighb++)
                {

                    var neighborId = eList[u.Id, neighb].NodeId;
                    int discourage = 0;

                    if (u.Id < NofNodesBeforeDetour && u.Id != source) discourage = 1000;

                    Vertex neighbor = vList[neighborId];
                    double edist = MsaglUtilities.EucledianDistance(u.XLoc, u.YLoc, neighbor.XLoc, neighbor.YLoc);
                    var tempDist = u.Dist + edist + discourage;
                    if (tempDist >= neighbor.Dist) continue;

                    neighbor.Dist = tempDist;
                    neighbor.Parent = u;
                    if (neighbor.Visited)
                    {
                        neighbor.Visited = false;
                        q.Enqueue(neighbor.Id, neighbor.Dist);
                    }
                    else q.DecreasePriority(neighbor.Id, neighbor.Dist);
                }
            }
            foreach (var node in _mainGeometryGraph.Nodes)
            {
                int target = nodeId[node];
                if (target == source) continue;
                Vertex route = vList[target];
                int zoomlevel = Math.Max(vList[source].ZoomLevel, vList[target].ZoomLevel);
                Edgelist.Clear();
                while (route.Parent != null)
                {
                    ShortestPath.Add(route.Id);
                    for (int neighb = 0; neighb < degList[route.Id]; neighb++)
                        if (eList[route.Id, neighb].NodeId == route.Parent.Id)
                        {
                            SetUsed(vList, eList, degList, route.Id, eList[route.Id, neighb].NodeId, zoomlevel);
                            Edgelist.Add(new VertexNeighbor(route.Id, neighb));
                            break;
                        }

                    route = route.Parent;
                }
                if (route.Id != source)
                    System.Diagnostics.Debug.WriteLine("path not found");
                foreach (VertexNeighbor vn in Edgelist)
                    g1.AddEdge(vn.A, g.EList[vn.A, vn.Neighbor].NodeId, g.EList[vn.A, vn.Neighbor].Selected, g.EList[vn.A, vn.Neighbor].Used);
            }
            return;
        }


        public void MSAGLSelectShortestPath(Vertex[] vList, Edge[,] eList, int[] degList, int source, int target, int n)
        {

            List<Vertex> q = new List<Vertex>();
            vList[source].Dist = 0;
            q.Add(vList[source]);
            for (int i = 0; i < n; i++)
            {
                if (vList[i].Id != source)
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
                Vertex u = getMin(q);
                if (u == null) return;
                if (u.Visited) continue;
                else u.Visited = true;
                for (int neighb = 0; neighb < degList[u.Id]; neighb++)
                {
                    var neighbor = eList[u.Id, neighb].NodeId;
                    if (vList[neighbor].Visited) continue;
                    int discourage = 0;
                    if (eList[u.Id, neighb].Selected == 1) discourage = 1000;//continue;
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
                if (u.Id == target)
                    break;
            }

            Vertex route = vList[target];

            while (route.Parent != null)
            {
                for (int neighb = 0; neighb < degList[route.Id]; neighb++)
                    if (eList[route.Id, neighb].NodeId == route.Parent.Id)
                    {
                        SetUsed(vList, eList, degList, route.Id, eList[route.Id, neighb].NodeId, 1);
                        Edgelist.Add(new VertexNeighbor(route.Id, neighb));

                        Distance += Math.Sqrt((route.XLoc - route.Parent.XLoc) * (route.XLoc - route.Parent.XLoc) + (route.YLoc - route.Parent.YLoc) * (route.YLoc - route.Parent.YLoc));

                        break;
                    }
                route = route.Parent;
            }
            if (route.Id != source)
                System.Diagnostics.Debug.WriteLine("path not found");

        }

        public void SetUsed(Vertex[] vList, Edge[,] eList, int[] degList, int a, int b, int zoomlevel)
        {
            for (int index = 0; index < degList[a]; index++)
            {
                if (eList[a, index].NodeId == b)
                {
                    if (eList[a, index].Used > 0) return;
                    else eList[a, index].Used = zoomlevel;
                }
            }
            for (int index = 0; index < degList[b]; index++)
            {
                if (eList[b, index].NodeId == a)
                {
                    if (eList[b, index].Used > 0) return;
                    else eList[b, index].Used = zoomlevel;
                }
            }
        }


    }
    public class VertexNeighbor
    {
        public int A;
        public int Neighbor;
        public VertexNeighbor(int p, int q)
        {
            A = p; Neighbor = q;
        }
    }
}
