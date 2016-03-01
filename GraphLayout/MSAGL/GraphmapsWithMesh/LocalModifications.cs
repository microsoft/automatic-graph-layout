using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.LargeGraphLayout;

namespace Microsoft.Msagl.GraphmapsWithMesh
{
    class LocalModifications
    {

        public static void MsaglMoveToMedian(Tiling g, Dictionary<int, Node> idToNodes, LgLayoutSettings _lgLayoutSettings)
        {
            int[,] listNeighbors = new int[20, 3];
            double[] d = new double[10];
            int a = 0, b = 0;
            Core.Geometry.Point[] p = new Core.Geometry.Point[10];
            bool localRefinementsFound = true;
            int iteration = 10;
            int offset = iteration * 2;
            int unit = (int)_lgLayoutSettings.NodeSeparation/2;

            while (localRefinementsFound && iteration > 0)
            {
                iteration--;
                localRefinementsFound = false;


                for (int index = g.N; index < g.NumOfnodesBeforeDetour; index++)
                {
                    Vertex w = g.VList[index];

                    int numNeighbors = 0;

                    for (int k = 0; k < g.DegList[w.Id]; k++)
                    {
                        numNeighbors++;
                        listNeighbors[numNeighbors, 1] = g.EList[w.Id, k].NodeId;
                        listNeighbors[numNeighbors, 2] = k;
                    }

                    if (numNeighbors <= 1) continue;
                    
                    for (int counter = 1; counter <= 9; counter++)
                    {
                        d[counter] = 0;

                        if (counter == 1) { a = unit; b = unit; }
                        if (counter == 2) { a = 0; b = unit; }
                        if (counter == 3) { a = -unit; b = unit; }
                        if (counter == 4) { a = -unit; b = 0; }
                        if (counter == 5) { a = -unit; b = -unit; }
                        if (counter == 6) { a = 0; b = -unit; }
                        if (counter == 7) { a = unit; b = -unit; }
                        if (counter == 8) { a = unit; b = 0; }
                        if (counter == 9) { a = 0; b = 0; }


                        for (int k = 1; k <= numNeighbors; k++)
                        {
                            double length = Math.Sqrt((w.XLoc + a - g.VList[listNeighbors[k, 1]].XLoc) *
                                          (w.XLoc + a - g.VList[listNeighbors[k, 1]].XLoc)
                                          +
                                          (w.YLoc + b - g.VList[listNeighbors[k, 1]].YLoc) *
                                          (w.YLoc + b - g.VList[listNeighbors[k, 1]].YLoc)
                                    );
                            if (length < 1)
                            {
                                length = 1000;
                            }


                            d[counter] += length;

                        }

                    
                        p[counter] = new Core.Geometry.Point(a, b);


                    }
                    Array.Sort(d, p);

                    for (int counter = 1; counter <= 9; counter++)
                    {
                        var mincostA = (int)p[counter].X;
                        var mincostB = (int)p[counter].Y;

                        if (!(mincostA == 0 && mincostB == 0))
                        {
                            w.XLoc += mincostA;
                            w.YLoc += mincostB;
                            if (g.GetNodeExceptTheGivenNode(w, w.XLoc, w.YLoc, 5) >= 0 ||
                                g.MsaglGoodResolution(w, listNeighbors, numNeighbors, 5) == false
                                || g.noCrossingsHeuristics(w) == false
                                )
                            {
                                w.XLoc -= mincostA;
                                w.YLoc -= mincostB;
                            }
                            else
                            {
                                //Console.Write(".");
                                localRefinementsFound = true;
                                break;
                            }
                        }
                    }

                }
            }
            Console.WriteLine("Done");
        }


        public static void MsaglStretchAccordingToZoomLevel(Tiling g, Dictionary<int, Node> idToNodes)
        {
            int[,] listNeighbors = new int[20, 3];
            double[] d = new double[10];
            int[] a = new int[10];
            int[] b = new int[10];
            Core.Geometry.Point[] p = new Core.Geometry.Point[10];
            bool localRefinementsFound = true;
            int iteration = 8;
            int offset = iteration * 2;

            a[1] = 1; b[1] = 1; a[2] = 0; b[2] = 1; a[3] = -1; b[3] = 1; a[4] = -1; b[4] = 0;
            a[5] = -1; b[5] = -1; a[6] = 0; b[6] = -1; a[7] = 1; b[7] = -1; a[8] = 1; b[8] = 0; a[9] = 0; b[9] = 0;

            while (localRefinementsFound && iteration > 0)
            {
                iteration--;
                localRefinementsFound = false;


                for (int index = g.N; index < g.NumOfnodesBeforeDetour; index++)
                {
                    Vertex w = g.VList[index];

                    int numNeighbors = 0;

                    for (int k = 0; k < g.DegList[w.Id]; k++)
                    {
                        if (g.EList[w.Id, k].Used == 0) continue;
                        numNeighbors++;
                        listNeighbors[numNeighbors, 1] = g.EList[w.Id, k].NodeId;
                        listNeighbors[numNeighbors, 2] = k;
                    }

                    if (numNeighbors <= 1) continue;

                    //compute the lowest zoom level among incident rails
                    var lowestZoomLevel = int.MaxValue;
                    for (int index2 = 1; index2 <= numNeighbors; index2++)
                        if (g.EList[index, listNeighbors[index2, 2]].Used < lowestZoomLevel)
                            lowestZoomLevel = g.EList[index, listNeighbors[index2, 2]].Used;

                    //for each possible move
                    for (int counter = 1; counter <= 9; counter++)
                    {
                        d[counter] = 0;
                        //find the ink cost of that move
                        for (int k = 1; k <= numNeighbors; k++)
                        {
                            //try to stretch the high priority rails
                            if (g.EList[index, listNeighbors[k, 2]].Used != lowestZoomLevel) continue;
                            double length = Math.Sqrt((w.XLoc + a[counter] - g.VList[listNeighbors[k, 1]].XLoc) *
                                          (w.XLoc + a[counter] - g.VList[listNeighbors[k, 1]].XLoc)
                                          +
                                          (w.YLoc + b[counter] - g.VList[listNeighbors[k, 1]].YLoc) *
                                          (w.YLoc + b[counter] - g.VList[listNeighbors[k, 1]].YLoc)
                                    );
                            if (length < .5) length = 1000;

                            d[counter] += length;

                        }

                        p[counter] = new Core.Geometry.Point(a[counter], b[counter]);


                    }
                    Array.Sort(d, p);

                    for (int counter = 1; counter <= 9; counter++)
                    {
                        var mincostA = (int)p[counter].X;
                        var mincostB = (int)p[counter].Y;

                        if (!(mincostA == 0 && mincostB == 0))
                        {
                            w.XLoc += mincostA;
                            w.YLoc += mincostB;
                            if (g.GetNodeExceptTheGivenNode(w, w.XLoc, w.YLoc, offset) >= 0 ||
                                g.MsaglGoodResolution(w, listNeighbors, numNeighbors, offset) == false
                                //||g.noCrossings(w) == false
                                )
                            {
                                w.XLoc -= mincostA;
                                w.YLoc -= mincostB;
                            }
                            else
                            {
                                //Console.Write(".");
                                localRefinementsFound = true;
                                break;
                            }
                        }
                    }

                }
            }
            Console.WriteLine("Done");
        }


        /*Douglas-Peucker line simplification algorithm 
         * INPUT: A set of points P in sequence and a tolerance value
         * OUTPUT: A simplified chain after removing some of the points from P
         */
        public static void PolygonalChainSimplification(Microsoft.Msagl.Core.Geometry.Point[] PointList, int start, int end, double epsilon)
        {

            // Find the point with the maximum distance
            double dmax = 0, d = 0;
            int index = 0;
            for (int i = start + 1; i <= end - 1; i++)
            {
                d = PointToSegmentDistance.GetDistance(PointList[start], PointList[end], PointList[i]);
                if (d > dmax)
                {
                    index = i;
                    dmax = d;
                }
            }


            // If max distance is greater than epsilon, recursively simplify
            if (dmax > epsilon)
            {
                // Recursive call
                PolygonalChainSimplification(PointList, start, index, epsilon);
                PolygonalChainSimplification(PointList, index, end, epsilon);
            }
            else
            {
                for (int i = start + 1; i <= end - 1; i++)
                    PointList[i] = new Core.Geometry.Point(-1, -1);
            }
        }
    }
}
