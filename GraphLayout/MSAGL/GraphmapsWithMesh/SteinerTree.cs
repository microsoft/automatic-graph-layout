using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Msagl.GraphmapsWithMesh
{
    class SteinerTree
    {
        //public int [] ComponentVar;
        public Stack<Tuple> Edgelist = new Stack<Tuple>();
        public List<Twin> SpanningTree = new List<Twin>();
        ComponentCollection _compCollection;
        public Component ComputeTree(Vertex[] vList, Edge[,] eList, int[] degList, int numOfv, Component givenComponent, int givenLevel)
        {
            double edgeCost = 0;
            Vertex p, q;


            _compCollection = new ComponentCollection(vList, numOfv, givenComponent, givenLevel);

            while (_compCollection.NumOfAliveComponents > 1)
            {
                for (int index = 1; index <= _compCollection.NumOfComponents; index++)
                {

                    //some neighbor of some of the vertices in this Component  is outside of it them it is an active component
                    if (IsActive(_compCollection.C[index], vList, eList, degList))
                    {
                        //Console.WriteLine(compCollection.numOfAliveComponents);
                        _compCollection.C[index].Dist += 0.2;
                    }
                }

                for (int i = 1; i <= numOfv; i++)
                {
                    for (int neighb = 1; neighb <= degList[i]; neighb++)
                    {
                        p = vList[i];
                        q = vList[eList[vList[i].Id, neighb].NodeId];
                        if (p.CId == 0 && q.CId == 0) continue;
                        if (p.CId > 0 && q.CId > 0 && p.CId == q.CId) continue;
                        //now at least one of p and q is a point or both are points; if both are points, then they lie in diff components


                        //find the components for  vList[i]  and  vList[eList[vList[i], neighb].nodeID] and check whether it is tight
                        if (p.CId > 0 && q.CId > 0)
                            edgeCost = _compCollection.C[p.CId].Dist + _compCollection.C[q.CId].Dist;
                        if (p.CId > 0 && q.CId == 0)
                            edgeCost = _compCollection.C[p.CId].Dist;
                        if (p.CId == 0 && q.CId > 0)
                            edgeCost = _compCollection.C[q.CId].Dist;

                        if (edgeCost >= eList[vList[i].Id, neighb].Weight)
                        {//this is a tight edge


                            Edgelist.Push(new Tuple(p, q, SelectEdge(eList, degList, p, q, givenLevel)));

                            int tempID;

                            if (p.CId == 0 && q.CId > 0)
                            {//if p is a single vertex
                                p.CId = q.CId;
                                _compCollection.C[q.CId].V.Add(p);
                            }
                            else if (q.CId == 0 && p.CId > 0)
                            {//if q is a single vertex
                                q.CId = p.CId;
                                _compCollection.C[p.CId].V.Add(q);
                            }
                            else
                            {//p and q are components

                                //Console.WriteLine(p.ID + ":" + q.ID);

                                tempID = q.CId;
                                //merge q-component to p-component                                
                                foreach (Vertex r in _compCollection.C[tempID].V)
                                {
                                    r.CId = p.CId;
                                    _compCollection.C[p.CId].V.Add(r);
                                }
                                _compCollection.C[tempID].V.Clear();
                                _compCollection.C[tempID].Dead = true;
                                _compCollection.NumOfAliveComponents--;

                                //initialize the component variable
                                _compCollection.C[p.CId].Dist = 0;
                            }



                        }//endif
                    }
                }//endfor


            }//endwhile

            SpanningTree.Clear();
            bool testConnected = false;
            while (Edgelist.Count > 0)
            {
                testConnected = false;
                Tuple e = Edgelist.Pop();
                //Console.WriteLine(e.a.ID + " : " + e.b.ID);

                DeSelectEdge(eList, degList, e.A, e.B);


                if (SpanningTree.Count > 0)
                {
                    if (SpanningTree.Any(t => t.A == e.A.Id && t.B == e.B.Id))
                    {
                        testConnected = true;
                    }

                }
                else testConnected = true;

                //if testconnected is false then that edge is not on the spanning tree and you know that it is connected
                if (testConnected == false) continue;
                if (IsConnected(vList, eList, degList, numOfv, givenLevel)) continue;
                else SelectEdge(eList, degList, e.A, e.B, e.Value);

            }

            //build component


            Component c = GetTreeComponent(vList, eList, degList, numOfv, givenLevel);
            //if (givenComponent != null) givenComponent.v.Clear();
            //else givenComponent = new Component();
            //foreach (vertex w in c.v)
            //{
            //givenComponent.v.Add(w);
            //}

            //Console.WriteLine("Tree Size = " + c.v.Count);

            return c;
        }
        public bool IsConnected(Vertex[] vList, Edge[,] eList, int[] degList, int numOfv, int givenLevel)
        {
            int numPoints = 0;
            int numVisited = 0;
            Vertex a = null; //source
            Queue<Vertex> q = new Queue<Vertex>();

            for (int index = 1; index <= numOfv; index++)
            {
                vList[index].Visited = false;
                if (vList[index].ZoomLevel <= givenLevel && vList[index].Weight > 0)
                {
                    a = vList[index]; numPoints++;
                }
            }

            q.Enqueue(a);
            a.Visited = true;


            while (q.Count > 0)
            {

                var currentVertex = q.Dequeue();
                if (currentVertex.ZoomLevel <= givenLevel && currentVertex.Weight > 0) numVisited++;
                //Console.Write(current_vertex.ID + ", ");


                for (int neighb = 1; neighb <= degList[currentVertex.Id]; neighb++)
                {
                    //Console.Write("[" + eList[current_vertex.ID, neighb].nodeId + "-"+ eList[current_vertex.ID, neighb].selected+" ]");
                    if (eList[currentVertex.Id, neighb].Selected >= 1 && vList[eList[currentVertex.Id, neighb].NodeId].Visited == false)
                    {
                        vList[eList[currentVertex.Id, neighb].NodeId].Visited = true;
                        q.Enqueue(vList[eList[currentVertex.Id, neighb].NodeId]);
                    }
                }
            }
            // Console.WriteLine(num_visited + "=" + num_points);

            if (numPoints == numVisited) BuildSpanningTree(vList, eList, degList, a.Id, numOfv, givenLevel);

            return numPoints == numVisited;
        }

        public Component GetTreeComponent(Vertex[] vList, Edge[,] eList, int[] degList, int numOfv, int givenLevel)
        {
            Component comp = new Component();
            Vertex a = null; //source
            Queue<Vertex> q = new Queue<Vertex>();

            for (int index = 1; index <= numOfv; index++)
            {
                vList[index].Visited = false;
                if (vList[index].ZoomLevel <= givenLevel && vList[index].Weight > 0)
                {
                    a = vList[index];
                }
            }

            q.Enqueue(a);
            a.Visited = true;


            while (q.Count > 0)
            {

                var currentVertex = q.Dequeue();
                comp.V.Add(currentVertex);
                //Console.Write(current_vertex.ID + ", ");

                for (int neighb = 1; neighb <= degList[currentVertex.Id]; neighb++)
                {
                    //Console.Write("[" + eList[current_vertex.ID, neighb].nodeId + "-"+ eList[current_vertex.ID, neighb].selected+" ]");
                    if (eList[currentVertex.Id, neighb].Selected >= 1 && vList[eList[currentVertex.Id, neighb].NodeId].Visited == false)
                    {
                        vList[eList[currentVertex.Id, neighb].NodeId].Visited = true;
                        q.Enqueue(vList[eList[currentVertex.Id, neighb].NodeId]);
                    }
                }
            }
            //Console.WriteLine( "tree size " + comp.v.Count);

            return comp;
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

        public void DeSelectEdge(Edge[,] eList, int[] degList, Vertex a, Vertex b)
        {
            for (int neighb = 1; neighb <= degList[a.Id]; neighb++)
            {
                if (eList[a.Id, neighb].NodeId == b.Id)
                {
                    eList[a.Id, neighb].Selected = 0;
                    break;
                }
            }
            for (int neighb = 1; neighb <= degList[b.Id]; neighb++)
            {
                if (eList[b.Id, neighb].NodeId == a.Id)
                {
                    eList[b.Id, neighb].Selected = 0;
                    break;
                }
            }

        }
        public bool IsActive(Component comp, Vertex[] vList, Edge[,] eList, int[] degList)
        {
            if (comp.Dead) return false;
            //Console.WriteLine(compCollection.numOfAliveComponents);

            foreach (Vertex w in comp.V)
            {

                //check if there is a neighbor of w outside of comp.v
                for (int neighb = 1; neighb <= degList[w.Id]; neighb++)
                {
                    if (w.CId != vList[eList[w.Id, neighb].NodeId].CId) return true;
                }
            }
            return false;
        }




        public void BuildSpanningTree(Vertex[] vList, Edge[,] eList, int[] degList, int source, int n, int givenLevel)
        {
            //Console.WriteLine("start");

            var q = new PriorityQueue<double, Vertex>();

            SpanningTree.Clear();

            vList[source].Dist = 0;
            q.Enqueue(vList[source].Dist, vList[source]);
            for (int i = 1; i <= n; i++)
            {
                if (vList[i].Id != vList[source].Id)
                {
                    vList[i].Dist = double.MaxValue;
                    q.Enqueue(vList[i].Dist, vList[i]);
                }
                vList[i].Parent = null;
                vList[i].Visited = false;
            }


            while (q.Count > 0)
            {
                Vertex u = q.Dequeue().Value;
                if (u.Visited == true) continue;
                else u.Visited = true;
                for (int neighb = 1; neighb <= degList[u.Id]; neighb++)
                {
                    var neighbor = eList[u.Id, neighb].NodeId;

                    if (eList[u.Id, neighb].Selected == 0) continue;
                    //else encourangeReuse = 0;

                    var temp = u.Dist + eList[u.Id, neighb].GetEDist(vList, u.Id, eList[u.Id, neighb].NodeId);
                    if (temp < vList[neighbor].Dist)
                    {
                        vList[neighbor].Dist = temp;
                        vList[neighbor].Parent = u;
                        q.Enqueue(vList[neighbor].Dist, vList[neighbor]);
                    }
                }
            }

            for (int i = 1; i <= n; i++)
            {
                if (vList[i].Weight > 0 && vList[i].ZoomLevel <= givenLevel)
                {
                    var w = vList[i];
                    //Console.WriteLine("***"+w.ID);
                    while (w.Parent != null)
                    {
                        //Console.WriteLine("" + w.ID + " " + w.parent.ID);
                        SpanningTree.Add(new Twin(w.Id, w.Parent.Id));
                        SpanningTree.Add(new Twin(w.Parent.Id, w.Id));
                        w = w.Parent;

                    }
                }
            }
        }



    }

    public class Component
    {
        public int CId = 0;
        public double Dist;
        public bool Dead = true;
        public List<Vertex> V = new List<Vertex>();
        public List<VertexNeighbor> Segments = new List<VertexNeighbor>();

        public void AddVertex(Vertex w)
        {
            foreach (Vertex z in V)
            {
                if (w.Id == z.Id) return;
            }
            V.Add(w);
        }
    }

    public class Twin
    {
        public int A;
        public int B;
        public Twin(int x, int y) { A = x; B = y; }

    }
    public class Tuple
    {
        public Vertex A;
        public Vertex B;
        public int Value;
        public Tuple(Vertex x, Vertex y, int z)
        {
            A = x; B = y; Value = z;
        }
    }
}
