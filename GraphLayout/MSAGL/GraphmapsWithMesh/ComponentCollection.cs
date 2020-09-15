using System;

namespace Microsoft.Msagl.GraphmapsWithMesh
{

    /*
     * This class is not used for the current implementation
     * This was written in the previous one when we were trying to route with 
     * regular grid and then recursively found the stainer trees for the 
     * components of different zoomlevel
     */

    public class ComponentCollection
    {
        internal int NumOfComponents;
        public int NumOfAliveComponents;
        public Component[] C; //componnent holder

        public ComponentCollection(Vertex[] w, int numOfv, Component givenComponent, int givenLevel)
        {   //initially every vertex is a single component
            int k = 0;
            C = new Component[numOfv + 1];
            for (int index = 1; index <= numOfv; index++)
            {

                w[index].CId = 0;
                if (w[index].Weight == 0 || w[index].ZoomLevel != givenLevel) continue;

                //NODE OVARLAPS WITH THE RAILS// SO IGNORE THE NODE
                if (givenComponent != null && givenComponent.V.Contains(w[index])) continue;

                k++;
                C[k] = new Component { CId = k };
                C[k].V.Add(w[index]);
                C[k].Dead = false;
                w[index].CId = C[k].CId;
                NumOfComponents++;
            }
            if (givenComponent != null)
            {
                k++;
                C[k] = givenComponent;
                C[k].CId = k;
                C[k].Dist = 0;
                C[k].Dead = false;
                foreach (Vertex x in C[k].V)
                {
                    x.CId = C[k].CId;
                }
                NumOfComponents++;
            }
            NumOfAliveComponents = NumOfComponents;

        }

    }
}
