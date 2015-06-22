using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication3
{
    
    public class PointSet
    {
        public int num_points;
        public Point []pt;
        public int numOfLevels;

        int []selected;
        public PointSet(int n, Tiling g, int pointsPerLevel)
        {
            int temp; //,temp2;
            num_points = n;
            pt = new Point[n+1];
            selected = new int[g.numOfnodes];
            Random r1 = new Random();
            Random r2 = new Random();
            //List<int> list = new List<int>();

            //for (int i = 1; i < 255; i++)
            //{
                //list.Add(i);
            //}

                
            //generate points in random
            while (n > 0)
            {
                temp = r1.Next(1, g.numOfnodes);
                if (selected[temp] == 1) continue;
                pt[n] = new Point(g.vList[temp].x_loc, g.vList[temp].y_loc, 0);
                selected[temp] = 1;
                //compute weight based on point density                    
                //temp2 = r2.Next(1, list.Count);
                //pt[n] = new Point(g.vList[temp].x_loc, g.vList[temp].y_loc, (int) list.ElementAt(temp2));
                //list.RemoveAt(temp2);
                pt[n].grid_point = temp;
                g.vList[temp].weight = pt[n].weight;
                n--;
            }
            assignWeight(pt, num_points, (int)Math.Sqrt(g.numOfnodes));


            numOfLevels =  num_points/pointsPerLevel;
            if (num_points % pointsPerLevel > 0) numOfLevels++;

            for (int index = 1; index <= num_points; index ++)
            {
                 pt[index].zoomLevel = 1+ (index-1)/pointsPerLevel;
            }

            for (int i = 1; i <= num_points; i++)
            {
                Console.WriteLine(pt[i].weight);
                g.vList[pt[i].grid_point].weight = pt[i].weight;
                g.vList[pt[i].grid_point].zoomLevel = pt[i].zoomLevel;
            }

            //rendom shuffle
            //int p1;
            //for (int i = 1; i <= 10000; i++)
            //{
            //    p1 = r1.Next(1, num_points);
            //    pt[p1].weight = r2.Next(10, 255);
            //    g.vList[pt[p1].grid_point].weight = pt[p1].weight;
            //}
        }
        public void assignWeight(Point[] pt, int num_points, int rad)
        {
            double temp = 0;
            for (int i = 1; i <=  num_points; i++)
            {
                pt[i].weight++;
                for (int j = i+1; j <=  num_points; j++)
                {
                    temp = Math.Sqrt((pt[i].x - pt[j].x) * (pt[i].x - pt[j].x) + (pt[i].y - pt[j].y) * (pt[i].y - pt[j].y));
                    if (temp < rad) { pt[i].weight++; pt[j].weight++; }
                }
            }

            double temp_max = 0, temp_min = num_points;
            for (int i = 1; i <= num_points; i++)
            {
                if (temp_max < pt[i].weight) temp_max = pt[i].weight;
                if (temp_min> pt[i].weight) temp_min = pt[i].weight;
            }
            for (int i = 1; i <= num_points; i++)
                pt[i].weight = 50+ (int)((pt[i].weight -temp_min) * 200/(temp_max-temp_min));

        }

        
    }

    public class Point
    {
        public int x=0;
        public int y=0;
        public int weight=0;
        public int grid_point = 0; //id of the grid point
        public int zoomLevel = 0;
        public Point() { }
        public Point(int a, int b, int c) {
            x = a; y = b; weight = c;
        }
    }
}
