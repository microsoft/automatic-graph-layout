using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication3
{
    class SteinerTree
    {
        //public int [] ComponentVar;
        public Stack<tuple> Edgelist = new Stack<tuple>();
        public List<twin> SpanningTree = new List<twin>();
        ComponentCollection _compCollection;
         public Component ComputeTree(vertex[] vList, edge[,] eList, int[] degList,  int numOfv, Component givenComponent, int givenLevel )
        {
            double edgeCost = 0;
            vertex p,q;


            _compCollection = new ComponentCollection(vList, numOfv, givenComponent, givenLevel);

             
            { 
                for(int index = 1; index <=  _compCollection.NumOfComponents; index++)
                {
                    
                    //some neighbor of some of the vertices in this Component  is outside of it them it is an active component
                    if (isActive(_compCollection.C[index] , vList, eList, degList))
                    {
                        //Console.WriteLine(compCollection.numOfAliveComponents);
                        _compCollection.C[index].dist += 0.2;
                    }
                }

                for(int i = 1;i<= numOfv; i++){
                    for (int neighb = 1; neighb <= degList[i]; neighb++)
                    {
                        p = vList[i];
                        q = vList[eList[vList[i].ID, neighb].nodeId];
                        if(p.cID == 0 && q.cID == 0) continue;
                        if (p.cID > 0 && q.cID > 0 && p.cID == q.cID) continue;
                        //now at least one of p and q is a point or both are points; if both are points, then they lie in diff components

 
                        //find the components for  vList[i]  and  vList[eList[vList[i], neighb].nodeID] and check whether it is tight
                        if (p.cID > 0 && q.cID > 0)
                            edgeCost = _compCollection.C[p.cID].dist + _compCollection.C[q.cID].dist;
                        if (p.cID > 0 && q.cID == 0)
                            edgeCost = _compCollection.C[p.cID].dist ;
                        if (p.cID == 0 && q.cID > 0)
                            edgeCost =   _compCollection.C[q.cID].dist;

                        if( edgeCost >= eList[vList[i].ID, neighb].weight ) {//this is a tight edge

                            
                            Edgelist.Push(new tuple(p, q, selectEdge(eList, degList, p, q, givenLevel)));

                            int tempID;

                            if (p.cID == 0 && q.cID>0)
                            {//if p is a single vertex
                                    p.cID = q.cID;
                                    _compCollection.C[q.cID].v.Add(p);
                            }
                            else if (q.cID == 0 && p.cID>0)
                            {//if q is a single vertex
                                    q.cID = p.cID;
                                    _compCollection.C[p.cID].v.Add(q);
                            }
                            else{//p and q are components
                                    
                                    //Console.WriteLine(p.ID + ":" + q.ID);

                                    tempID = q.cID;
                                    //merge q-component to p-component                                
                                    foreach (vertex r in _compCollection.C[tempID].v)
                                    {
                                        r.cID = p.cID;
                                        _compCollection.C[p.cID].v.Add(r);
                                    }
                                    _compCollection.C[tempID].v.Clear();
                                    _compCollection.C[tempID].dead = true;
                                    _compCollection.numOfAliveComponents--;                                    

                                    //initialize the component variable
                                    _compCollection.C[p.cID].dist = 0;                                
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
                tuple e = (tuple)Edgelist.Pop();
                //Console.WriteLine(e.a.ID + " : " + e.b.ID);

                deSelectEdge(eList, degList, e.a, e.b);


                if (SpanningTree.Count > 0)
                {
                    foreach (twin t in SpanningTree)
                    {//if it is already in the spanning tree then you have to test whether it is connected after deselecting it
                        if (t.a == e.a.ID && t.b == e.b.ID)
                        {
                            testConnected = true;
                            break;
                        }
                    } 
          
                }
                else testConnected = true;

                //if testconnected is false then that edge is not on the spanning tree and you know that it is connected
                if (testConnected == false) continue;
                if (isConnected(vList, eList, degList, numOfv, givenLevel)) continue;
                else selectEdge(eList, degList,  e.a,  e.b, e.value);

            }

            //build component
            

            Component c = getTreeComponent(vList, eList, degList, numOfv, givenLevel);
            //if (givenComponent != null) givenComponent.v.Clear();
            //else givenComponent = new Component();
            //foreach (vertex w in c.v)
            //{
                //givenComponent.v.Add(w);
            //}

            //Console.WriteLine("Tree Size = " + c.v.Count);

            return c;
        }
        public bool isConnected(vertex[] vList, edge[,] eList, int[] degList,  int numOfv, int givenLevel)
        {

            vertex current_vertex;
            int num_points = 0;
            int num_visited = 0;
            vertex a=null; //source
            Queue<vertex> q = new Queue<vertex>();

            for (int index = 1; index <= numOfv; index++)
            {                
                vList[index].visited = false;
                if (vList[index].zoomLevel<= givenLevel &&  vList[index].weight > 0)
                {
                    a = vList[index]; num_points++; 
                }
            }

            q.Enqueue(a);
            a.visited = true;
 

            while(q.Count>0){
                
                current_vertex = q.Dequeue();
                if (current_vertex.zoomLevel <= givenLevel && current_vertex.weight > 0) num_visited++;
                //Console.Write(current_vertex.ID + ", ");


                for (int neighb = 1; neighb <= degList[current_vertex.ID]; neighb++)
                {
                    //Console.Write("[" + eList[current_vertex.ID, neighb].nodeId + "-"+ eList[current_vertex.ID, neighb].selected+" ]");
                    if (eList[current_vertex.ID, neighb].selected >= 1 && vList[eList[current_vertex.ID, neighb].nodeId].visited== false ) 
                    {                        
                        vList[eList[current_vertex.ID, neighb].nodeId].visited = true;
                        q.Enqueue(vList[eList[current_vertex.ID, neighb].nodeId]);
                    }
                }
            }
           // Console.WriteLine(num_visited + "=" + num_points);

            if (num_points == num_visited) BuildSpanningTree(vList, eList, degList, a.ID, numOfv, givenLevel);

            return  num_points ==  num_visited;
        }

        public Component getTreeComponent(vertex[] vList, edge[,] eList, int[] degList, int numOfv, int givenLevel){
            Component comp = new Component();
            vertex current_vertex;
            int num_points = 0;
            int num_visited = 0;
            vertex a = null; //source
            Queue<vertex> q = new Queue<vertex>();

            for (int index = 1; index <= numOfv; index++)
            {
                vList[index].visited = false;
                if (vList[index].zoomLevel <= givenLevel && vList[index].weight > 0)
                {
                    a = vList[index];
                    num_points++;
                }
            }

            q.Enqueue(a);
            a.visited = true;


            while (q.Count > 0)
            {

                current_vertex = q.Dequeue();
                if (current_vertex.zoomLevel <= givenLevel && current_vertex.weight > 0) num_visited++;
                comp.v.Add(current_vertex);
                //Console.Write(current_vertex.ID + ", ");

                for (int neighb = 1; neighb <= degList[current_vertex.ID]; neighb++)
                {
                    //Console.Write("[" + eList[current_vertex.ID, neighb].nodeId + "-"+ eList[current_vertex.ID, neighb].selected+" ]");
                    if (eList[current_vertex.ID, neighb].selected >= 1 && vList[eList[current_vertex.ID, neighb].nodeId].visited == false)
                    {
                        vList[eList[current_vertex.ID, neighb].nodeId].visited = true;
                        q.Enqueue(vList[eList[current_vertex.ID, neighb].nodeId]);
                    }
                }
            }
            //Console.WriteLine( "tree size " + comp.v.Count);

            return  comp;
        }
        public int selectEdge(edge[,] eList, int[] degList, vertex a, vertex b, int givenLevel)
        {
            int temp = givenLevel;
            for (int neighb = 1; neighb <= degList[a.ID]; neighb++)
            {
                if (eList[a.ID, neighb].nodeId == b.ID)
                {
                    if (eList[a.ID, neighb].selected == 0)
                    {
                        eList[a.ID, neighb].selected = givenLevel;
                    }
                    else temp = eList[a.ID, neighb].selected;
                    break;
                }
            }
            for (int neighb = 1; neighb <= degList[b.ID]; neighb++)
            {
                if (eList[b.ID, neighb].nodeId == a.ID)
                {
                    if (eList[b.ID, neighb].selected == 0)
                    {
                        eList[b.ID, neighb].selected = givenLevel;
                    }
                    else temp = eList[b.ID, neighb].selected;
                    break;
                }
            }
            return temp;
        }

        public void deSelectEdge(edge[,] eList, int[] degList, vertex a, vertex b )
        {
            for (int neighb = 1; neighb <= degList[a.ID]; neighb++)
            {
                if (eList[a.ID, neighb].nodeId == b.ID)
                {
                    eList[a.ID, neighb].selected = 0;
                    break;
                }
            }
            for (int neighb = 1; neighb <= degList[b.ID]; neighb++)
            {
                if (eList[b.ID, neighb].nodeId == a.ID)
                {
                    eList[b.ID, neighb].selected = 0;
                    break;
                }
            }

        }
        public bool isActive(Component comp,vertex[] vList, edge[,] eList, int[] degList)
        {
            if (comp.dead) return false;
            //Console.WriteLine(compCollection.numOfAliveComponents);

            foreach ( vertex w in comp.v){
                
                //check if there is a neighbor of w outside of comp.v
                for (int neighb = 1; neighb <= degList[w.ID]; neighb++ )
                {
                    if (w.cID != vList[eList[w.ID, neighb].nodeId].cID ) return true;
                }
            }
            return false;
        }




        public void BuildSpanningTree(vertex[] vList, edge[,] eList, int[] degList, int source, int N, int givenLevel)
        {
            //Console.WriteLine("start");

            PriorityQueue<double, vertex> Q = new PriorityQueue<double, vertex>(); ;
            double temp;
            int neighbor;

            SpanningTree.Clear();

            vList[source].dist = 0;
            Q.Enqueue(vList[source].dist, vList[source]);
            for (int i = 1; i <= N; i++)
            {
                if (vList[i].ID != vList[source].ID)
                {
                    vList[i].dist = double.MaxValue;
                    Q.Enqueue(vList[i].dist, vList[i]);
                }
                vList[i].parent = null;
                vList[i].visited = false;
            }

 
            while (Q.Count > 0)
            {
                vertex u = Q.Dequeue().Value;
                if (u.visited == true) continue;
                else u.visited = true;
                for (int neighb = 1; neighb <= degList[u.ID]; neighb++)
                {
                    neighbor = eList[u.ID, neighb].nodeId;

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
            }

            for (int i = 1; i <= N; i++)
            {                
                if (vList[i].weight > 0 && vList[i].zoomLevel <= givenLevel)
                {
                    var w = vList[i];
                    //Console.WriteLine("***"+w.ID);
                    while (w.parent != null)
                    {
                        //Console.WriteLine("" + w.ID + " " + w.parent.ID);
                        SpanningTree.Add(new twin(w.ID, w.parent.ID));
                        SpanningTree.Add(new twin( w.parent.ID, w.ID));
                        w = w.parent;

                    }
                }
            }
        }



    }

    public class Component
    {
        public int cID = 0;
        public double dist=0;
        public bool dead = true;
        public List<vertex> v = new List<vertex>();
        public List<VertexNeighbor> segments= new List<VertexNeighbor>();
    }

    public class twin
    {
        public int a = 0;
        public int b = 0;
        public twin(int x, int y) { a = x; b = y; }

    }
    public class tuple{
        public vertex a;
        public vertex b;
        public int value;
        public tuple(vertex x, vertex y, int z)
        {
            a = x; b = y; value = z;
        }
    }
}
