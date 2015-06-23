using System;

namespace WindowsFormsApplication3
{
    
    public class PointSet
    {
        public int NumPoints;
        public Point []Pt;
        public int NumOfLevels;

        int []_selected;
        public PointSet(int n, Tiling g, int pointsPerLevel)
        {
            NumPoints = n;
            Pt = new Point[n+1];
            _selected = new int[g.NumOfnodes];
            Random r1 = new Random();
           
            //List<int> list = new List<int>();

            //for (int i = 1; i < 255; i++)
            //{
                //list.Add(i);
            //}

                
            //generate points in random
            while (n > 0)
            {
                var temp = r1.Next(1, g.NumOfnodes); //,temp2;
                if (_selected[temp] == 1 || (g.VList[temp].XLoc+g.VList[temp].YLoc)%2==1 ) continue;
                Pt[n] = new Point(g.VList[temp].XLoc, g.VList[temp].YLoc, 0);
                _selected[temp] = 1;
                //compute weight based on point density                    
                //temp2 = r2.Next(1, list.Count);
                //pt[n] = new Point(g.vList[temp].x_loc, g.vList[temp].y_loc, (int) list.ElementAt(temp2));
                //list.RemoveAt(temp2);
                Pt[n].GridPoint = temp;
                g.VList[temp].Weight = Pt[n].Weight;
                n--;
            }
            AssignWeight(Pt, NumPoints, (int)Math.Sqrt(g.NumOfnodes));


            NumOfLevels =  NumPoints/pointsPerLevel;
            if (NumPoints % pointsPerLevel > 0) NumOfLevels++;

            for (int index = 1; index <= NumPoints; index ++)
            {
                 Pt[index].ZoomLevel = 1+ (index-1)/pointsPerLevel;
            }

            for (int i = 1; i <= NumPoints; i++)
            {
                Console.WriteLine(Pt[i].Weight);
                g.VList[Pt[i].GridPoint].Weight = Pt[i].Weight;
                g.VList[Pt[i].GridPoint].ZoomLevel = Pt[i].ZoomLevel;
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
        public void AssignWeight(Point[] pt, int numPoints, int rad)
        {
            for (int i = 1; i <=  numPoints; i++)
            {
                pt[i].Weight++;
                for (int j = i+1; j <=  numPoints; j++)
                {
                    var temp = Math.Sqrt((pt[i].X - pt[j].X) * (pt[i].X - pt[j].X) + (pt[i].Y - pt[j].Y) * (pt[i].Y - pt[j].Y));
                    if (temp < rad) { pt[i].Weight++; pt[j].Weight++; }
                }
            }

            double tempMin = numPoints;
            double tempMax = 0;
            for (int i = 1; i <= numPoints; i++)
            {
                if (tempMax < pt[i].Weight) tempMax = pt[i].Weight;
                if (tempMin> pt[i].Weight) tempMin = pt[i].Weight;
            }
            for (int i = 1; i <= numPoints; i++)
                pt[i].Weight = 50+ (int)((pt[i].Weight -tempMin) * 200/(tempMax-tempMin));

        }

        
    }

    public class Point
    {
        public int X;
        public int Y;
        public int Weight;
        public int GridPoint; //id of the grid point
        public int ZoomLevel;
        public Point() { }
        public Point(int a, int b, int c) {
            X = a; Y = b; Weight = c;
        }
    }
}
