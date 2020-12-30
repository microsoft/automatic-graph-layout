using System;

namespace Microsoft.Msagl.GraphmapsWithMesh
{

    public class PointSet
    {
        public int NumPoints;
        public WeightedPoint[] Pt;
        public int NumOfLevels;

        int[] _selected;

        public int[,] pointMap;



        public PointSet(int n)
        {
            NumPoints = n;
            Pt = new WeightedPoint[n];
            Random r1 = new Random(2);


            //generate points in random
            while (n > 0)
            {
                int XLoc = r1.Next(100);
                int YLoc = r1.Next(100);
                if (exists(XLoc, YLoc, n)) continue;
                n--;
                Pt[n] = new WeightedPoint(XLoc, YLoc, 0);
            }
            for (int i = 0; i < NumPoints; i++)
            {
                Pt[i].X *= 3;
                Pt[i].Y *= 3;
            }

        }

        public bool isVeryClose(int x, int y, int n)
        {
            double d;
            for (int i = NumPoints - 1; i > n; i--)
            {
                d = Math.Sqrt((Pt[i].X - x) * (Pt[i].X - x) + (Pt[i].Y - y) * (Pt[i].Y - y));
                if (d <= 3) return true;
            }
            return false;
        }


        public bool exists(int x, int y, int n)
        {
            for (int i = NumPoints - 1; i > n; i--)
            {
                if (Pt[i].X == x && Pt[i].Y == y) return true;
            }
            return false;
        }
        public PointSet(int n, Tiling g, int pointsPerLevel)
        {
            NumPoints = n;
            Pt = new WeightedPoint[n + 1];
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=340
            throw new InvalidOperationException();
#else
            pointMap = new int[g.NumOfnodes + 1, g.NumOfnodes + 1];
#endif
            _selected = new int[g.NumOfnodes + 1];
            Random r1 = new Random(1);


            //generate points in random
            while (n > 0)
            {
                var temp = r1.Next(1, g.NumOfnodes); //,temp2;
                if (_selected[temp] == 1 || (g.VList[temp].XLoc + g.VList[temp].YLoc) % 2 == 1) continue;
                Pt[n] = new WeightedPoint(g.VList[temp].XLoc, g.VList[temp].YLoc, 0);
                _selected[temp] = 1;
                //compute weight based on point density
                Pt[n].GridPoint = temp;
                g.VList[temp].Weight = Pt[n].Weight;
                n--;
            }
            AssignWeight(Pt, NumPoints, (int)Math.Sqrt(g.NumOfnodes));


            NumOfLevels = NumPoints / pointsPerLevel;
            if (NumPoints % pointsPerLevel > 0) NumOfLevels++;

            for (int index = 1; index <= NumPoints; index++)
            {
                Pt[index].ZoomLevel = 1 + (index - 1) / pointsPerLevel;
            }

            for (int i = 1; i <= NumPoints; i++)
            {
                g.VList[Pt[i].GridPoint].Weight = Pt[i].Weight;
                g.VList[Pt[i].GridPoint].ZoomLevel = Pt[i].ZoomLevel;
                pointMap[Pt[i].X, Pt[i].Y] = i;

            }
        }
        public void AssignWeight(WeightedPoint[] pt, int numPoints, int rad)
        {
            for (int i = 1; i <= numPoints; i++)
            {
                pt[i].Weight++;
                for (int j = i + 1; j <= numPoints; j++)
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
                if (tempMin > pt[i].Weight) tempMin = pt[i].Weight;
            }
            for (int i = 1; i <= numPoints; i++)
                pt[i].Weight = 50 + (int)((pt[i].Weight - tempMin) * 200 / (tempMax - tempMin));

        }


    }

    public class WeightedPoint
    {
        public int X;
        public int Y;
        public int Weight;
        public int GridPoint; //id of the grid point
        public int ZoomLevel;
        public WeightedPoint() { }
        public WeightedPoint(int a, int b, int c)
        {
            X = a; Y = b; Weight = c;
        }
    }
}
