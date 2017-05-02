using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Min cost max flow algorithm using an adjacency matrix.  If you
// want just regular max flow, setting all edge costs to 1 gives
// running time O(|E|^2 |V|).
//
// Running time: O(min(|V|^2 * totflow, |V|^3 * totcost))
//
// INPUT: cap -- a matrix such that cap[i][j] is the capacity of
//               a directed edge from node i to node j
//
//        cost -- a matrix such that cost[i][j] is the (positive)
//                cost of sending one unit of flow along a 
//                directed edge from node i to node j
//
//        source -- starting node
//        sink -- ending node
//
// OUTPUT: max flow and min cost; the matrix flow will contain
//         the actual flow values (note that unlike in the MaxFlow
//         code, you don't need to ignore negative flow values -- there
//         shouldn't be any)
//
// To use this, create a MinCostMaxFlow object, and call it like this:
//
//   MinCostMaxFlow nf;
//   int maxflow = nf.getMaxFlow(cap,cost,source,sink);

namespace Microsoft.Msagl.GraphmapsWithMesh
{
    internal class MinCostMaxFlow
    {
        private bool []found;
        private int N;
        int [,]cap;
        int [,]flow;
        int [,]cost;
        int []dad;
        int []dist;
        int []pi;

        private int INF = int.MaxValue/2 - 1;

        private bool search(int source, int sink)
        {
            Array.Clear(found,0,found.Length); //fill(found, false);
            for (int i = 0; i<dist.Length; i++) dist[i] = INF; //Array.fill(dist, INF);
            dist[source] = 0;

            while (source != N)
            {
                int best = N;
                found[source] = true;
                for (int k = 0; k < N; k++)
                {
                    if (found[k]) continue;
                    if (flow[k,source] != 0)
                    {
                        int val = dist[source] + pi[source] - pi[k] - cost[k,source];
                        if (dist[k] > val)
                        {
                            dist[k] = val;
                            dad[k] = source;
                        }
                    }
                    if (flow[source,k] < cap[source,k])
                    {
                        int val = dist[source] + pi[source] - pi[k] + cost[source,k];
                        if (dist[k] > val)
                        {
                            dist[k] = val;
                            dad[k] = source;
                        }
                    }

                    if (dist[k] < dist[best]) best = k;
                }
                source = best;
            }
            for (int k = 0; k < N; k++)
                pi[k] = Math.Min(pi[k] + dist[k], INF);
            return found[sink];
        }


        public int[] getMaxFlow(int [,]cap, int [,]cost, int source, int sink )
        {
            this.cap = cap;
            this.cost = cost;

            N = cap.GetLength(1);
            found = new bool[N];
            flow = new int[N,N];
            dist = new int[N + 1];
            dad = new int[N];
            pi = new int[N];

            int totflow = 0, totcost = 0;
            while (search(source, sink))
            {
                int amt = INF;
                for (int x = sink; x != source; x = dad[x])
                    amt = Math.Min(amt, flow[x,dad[x]] != 0
                        ? flow[x,dad[x]]
                        : cap[dad[x],x] - flow[dad[x],x]);
                for (int x = sink; x != source; x = dad[x])
                {
                    if (flow[x,dad[x]] != 0)
                    {
                        flow[x,dad[x]] -= amt;
                        totcost -= amt*cost[x,dad[x]];
                    }
                    else
                    {
                        flow[dad[x],x] += amt;
                        totcost += amt*cost[dad[x],x];
                    }
                }
                totflow += amt;
            }

            return new int[] {totflow, totcost};
        }


    }
}
