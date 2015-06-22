using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace WindowsFormsApplication3
{

    public class Network
    {
        public List<twin> edges = new List<twin>();        
        public int N = 0;
        public int E = 0;
        public int[,] M ;

        public Network(int num_v, int num_e) 
        {
            N = num_v;
            M = new int[num_v+1, num_v+1];
            int v1=0, v2=0;
            bool newEdge = true;
            while (E < num_e)
            {
                newEdge = true;
                Random r = new Random();
                v1 = r.Next(1, num_v);
                v2 = r.Next(1, num_v);
                
                if (v1 == v2) continue;
                foreach (twin x in edges)
                {
                    if ((v1 == x.a && v2 == x.b) || (v1 == x.b && v2 == x.a)){
                        newEdge = false; 
                        break;
                    }
                }
                if(newEdge){
                    E++;
                    edges.Add(new twin(v1,v2));
                    M[v1, v2] = 1;
                    M[v2, v1] = 1;
                }
                //Console.WriteLine(E);
            }
        }
    }
}
