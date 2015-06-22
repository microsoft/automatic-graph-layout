using System;

namespace WindowsFormsApplication3
{
    public class ComponentCollection
    {
        internal int NumOfComponents;
        public int numOfAliveComponents = 0;
        public  Component [] C; //componnent holder

        public ComponentCollection(vertex[] w, int numOfv, Component givenComponent, int givenLevel)
        {   //initially every vertex is a single component
            int k = 0;
            C = new Component[numOfv + 1];
            for (int index = 1; index <= numOfv; index++) {

                w[index].cID = 0;
                if(w[index].weight == 0 || w[index].zoomLevel != givenLevel) continue;

                //NODE OVARLAPS WITH THE RAILS// SO IGNORE THE NODE
                if (givenComponent!=null && givenComponent.v.Contains(w[index])) continue;

                k++;
                C[k] = new Component();
                C[k].cID = k;
                C[k].v.Add(w[index]);
                C[k].dead = false;
                w[index].cID = C[k].cID;
                NumOfComponents++;
            }
            if(givenComponent != null){
                k++;
                C[k] = givenComponent;
                C[k].cID = k;
                C[k].dist = 0;
                C[k].dead = false;
                foreach (vertex x in C[k].v)
                {
                    x.cID = C[k].cID;
                }
                NumOfComponents++;
            }
            numOfAliveComponents = NumOfComponents;
            Console.WriteLine(" N of C:" + numOfAliveComponents);

        }
         
    }
}