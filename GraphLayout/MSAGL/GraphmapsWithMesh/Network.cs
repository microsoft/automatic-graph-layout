using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Msagl.GraphmapsWithMesh
{

    public class Network
    {
        public List<Twin> Edges = new List<Twin>();
        public int N;
        public int E;
        public int[,] M;

        public Network(int numV, int numE, bool Random)
        {
            N = numV;
            E = numE;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=340
            throw new InvalidOperationException();
#else
            M = new int[numV + 1, numV + 1];
#endif
        }
        public Network(int numV, int numE)
        {
            N = numV;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=340
            throw new InvalidOperationException();
#else
            M = new int[numV + 1, numV + 1];
#endif
            int v2;
            while (E < numE)
            {
                Random r = new Random(1);
                var v1 = r.Next(1, numV);
                v2 = r.Next(1, numV);

                if (v1 == v2) continue;
                var newEdge = Edges.All(x => (v1 != x.A || v2 != x.B) && (v1 != x.B || v2 != x.A));
                if (newEdge)
                {
                    E++;
                    Edges.Add(new Twin(v1, v2));
                    M[v1, v2] = 1;
                    M[v2, v1] = 1;
                }
            }
        }
    }
}
