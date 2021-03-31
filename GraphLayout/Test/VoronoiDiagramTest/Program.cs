/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace VoronoiDiagramTest
{
    internal class VoronoiDiagram {
        IEnumerable<Point> cites;
        RTree<Point,Point> voronoiSiteTree = new RTree<Point,Point>();
            
        internal Rectangle BoundingBox;
        private double eps=0.00001;

        internal VoronoiDiagram(IEnumerable<Point> cites) {
            this.cites = cites;
        }

        internal IEnumerable<Point> Calculate(){
            if (BoundingBox.Width==0)
                FillBoundingBoxWithSites();
            Cdt cdt = new Cdt(cites, null,null);
            cdt.Run();
            var triangles = cdt.GetTriangles();
            foreach (var triangle in triangles)
                AddVoronoiCite(triangle);
            return voronoiSiteTree.GetAllLeaves();
        }

        private void FillBoundingBoxWithSites()
        {
            BoundingBox = new Rectangle(cites);
        }

        void AddVoronoiCite(CdtTriangle triangle)
        {
            Point p;
            var goodTriangle = GetCenterOfDescribedCircle(triangle, out p);
            if (!goodTriangle)
                return;
            if (!BoundingBox.Contains(p))
                return;
            var rect=new Rectangle(p);
            rect.Pad(eps);
            if (voronoiSiteTree.GetAllIntersecting(rect).Count() > 0)
                return;

            voronoiSiteTree.Add(rect, p);
        }

        private bool GetCenterOfDescribedCircle(CdtTriangle triangle, out Point x)
        {
            var p0 = (triangle.Sites[0].Point + triangle.Sites[1].Point) / 2;
            var p1 = p0 + (triangle.Sites[0].Point - triangle.Sites[1].Point).Rotate(-Math.PI/2);
            var p2 = (triangle.Sites[1].Point + triangle.Sites[2].Point) / 2;
            var p3 = p0 + (triangle.Sites[1].Point - triangle.Sites[2].Point).Rotate(-Math.PI/2);
            return Point.LineLineIntersection(p0, p1, p2, p3, out x);
        }

    }
    class Program
    {
        static void Main(string[] args)
        {
            int n;
            if (args.Length == 0) {
                Generate(100);
                Generate(1000);
                Generate(10000);
            } else {
                if (!int.TryParse(args[0], out n)) {
                    System.Diagnostics.Debug.WriteLine("expecting an integer parameter, getting {0}", args[0]);
                    Environment.Exit(1);
                }
                Generate(n);
            }
        }

        static void Generate(int n) {
            var size = 10.0*Math.Sqrt(n);
            var random = new Random(1);
            var points = GetRandomPoints(n, random, size).ToArray();

            for (int i = 0; i < Iterations(n); i++) {
                var fileName = CreateFileName(n, i, points);
                points=ProcessPoints(points, fileName).ToArray();
                System.Diagnostics.Debug.WriteLine(fileName);
            }
        }

        static int Iterations(int n) {
            switch (n) {
                case 100:
                    return 16;
                case 1000:
                    return 12;
                case 10000:
                    return 9;
                default:
                throw new NotImplementedException();
            }
        }

        private static string CreateFileName(int n, int i, Point[] points)
        {
            return String.Format("c:\\tmp\\{0}initial_{1}iteration_{2}vv.txt", n,i, points.Length);
        }

        private static IEnumerable<Point> ProcessPoints(Point[] points, string fileName)
        {
            var vd = new VoronoiDiagram(points);
            var centers = vd.Calculate();
            using (var file = new StreamWriter(fileName)) {
                foreach (var p in centers)
                    file.WriteLine("{0} {1}", p.X, p.Y);
            }
            return centers;
        }
        
        private static IEnumerable<Point> GetRandomPoints(int n, Random random, double size)
        {
            for (int i = 0; i < n; i++)
            {
                yield return new Point(size * random.NextDouble(), size * random.NextDouble());
            }

            
        }
    }
}
