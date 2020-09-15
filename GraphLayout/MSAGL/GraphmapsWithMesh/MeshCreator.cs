using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Point = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.GraphmapsWithMesh
{
    class MeshCreator
    {

        private static void CreateFourRaysPerVertex(Tiling G, int maxX, int maxY)
        {
            int nodeIndex;
            for (nodeIndex = 0; nodeIndex < G.N; nodeIndex++)
            {
                int x = G.VList[nodeIndex].XLoc, y = G.VList[nodeIndex].YLoc;
                int y_new;

                var xNew = x + 1;
                y_new = y;
                if (xNew >= 0 && xNew <= maxX && y_new >= 0 && y_new <= maxY)
                {
                    LineSegment ls = new LineSegment(new Point(x, y), new Point(xNew, y_new));
                    G.VList[nodeIndex].SegmentList.Add(ls, true);
                }
                xNew = x;
                y_new = y + 1;
                if (xNew >= 0 && xNew <= maxX && y_new >= 0 && y_new <= maxY)
                {
                    LineSegment ls = new LineSegment(new Point(x, y), new Point(xNew, y_new));
                    G.VList[nodeIndex].SegmentList.Add(ls, true);
                }
                xNew = x - 1;
                y_new = y;
                if (xNew >= 0 && xNew <= maxX && y_new >= 0 && y_new <= maxY)
                {
                    LineSegment ls = new LineSegment(new Point(x, y), new Point(xNew, y_new));
                    G.VList[nodeIndex].SegmentList.Add(ls, true);
                }
                xNew = x;
                y_new = y - 1;
                if (xNew >= 0 && xNew <= maxX && y_new >= 0 && y_new <= maxY)
                {
                    LineSegment ls = new LineSegment(new Point(x, y), new Point(xNew, y_new));
                    G.VList[nodeIndex].SegmentList.Add(ls, true);
                }
            }
        }


        static Point GrowOneUnit(LineSegment ls, int maxX, int maxY)
        {
            int xNew = 0;
            int yNew = 0;
            if ((int)ls.Start.X == (int)ls.End.X && (int)ls.Start.Y < (int)ls.End.Y)
            {
                xNew = (int)ls.End.X;
                yNew = (int)ls.End.Y + 1;
            }
            if ((int)ls.Start.X == (int)ls.End.X && (int)ls.Start.Y > (int)ls.End.Y)
            {
                xNew = (int)ls.End.X;
                yNew = (int)ls.End.Y - 1;
            }

            if ((int)ls.Start.Y == (int)ls.End.Y && (int)ls.Start.X < (int)ls.End.X)
            {
                xNew = (int)ls.End.X + 1;
                yNew = (int)ls.End.Y;
            }
            if ((int)ls.Start.Y == (int)ls.End.Y && (int)ls.Start.X > (int)ls.End.X)
            {
                xNew = (int)ls.End.X - 1;
                yNew = (int)ls.End.Y;
            }
            if (!(xNew >= 0 && xNew <= maxX && yNew >= 0 && yNew <= maxY)) return new Point(-1, -1);
            return new Point(xNew, yNew);
        }


        static int FindVertexClosestToSegmentEnd(Tiling g, LineSegment ls)
        {
            double distance = double.MaxValue;
            int nearestVertex = -1;

            if ((int)ls.Start.X == (int)ls.End.X) //vertical
            {
                for (int nodeIndex = 0; nodeIndex < g.NumOfnodes; nodeIndex++)
                {
                    if (g.VList[nodeIndex].XLoc != (int)ls.Start.X) continue;
                    if (ls.Start.Y < ls.End.Y && g.VList[nodeIndex].YLoc >= ls.Start.Y && g.VList[nodeIndex].YLoc < ls.End.Y)
                    {
                        if (distance > ls.End.Y - g.VList[nodeIndex].YLoc)
                        {
                            distance = ls.End.Y - g.VList[nodeIndex].YLoc;
                            nearestVertex = nodeIndex;
                        }
                    }
                    if (ls.Start.Y > ls.End.Y && g.VList[nodeIndex].YLoc <= ls.Start.Y && g.VList[nodeIndex].YLoc > ls.End.Y)
                    {
                        if (distance > g.VList[nodeIndex].YLoc - ls.End.Y)
                        {
                            distance = g.VList[nodeIndex].YLoc - ls.End.Y;
                            nearestVertex = nodeIndex;
                        }
                    }
                }
            }
            if ((int)ls.Start.Y == (int)ls.End.Y) //horizontal
            {
                for (int nodeIndex = 0; nodeIndex < g.NumOfnodes; nodeIndex++)
                {
                    if (g.VList[nodeIndex].YLoc != (int)ls.Start.Y) continue;
                    if (ls.Start.X < ls.End.X && g.VList[nodeIndex].XLoc >= ls.Start.X && g.VList[nodeIndex].XLoc < ls.End.X)
                    {
                        if (distance > ls.End.X - g.VList[nodeIndex].XLoc)
                        {
                            distance = ls.End.X - g.VList[nodeIndex].XLoc;
                            nearestVertex = nodeIndex;
                        }
                    }
                    if (ls.Start.X > ls.End.X && g.VList[nodeIndex].XLoc <= ls.Start.X && g.VList[nodeIndex].XLoc > ls.End.X)
                    {
                        if (distance > g.VList[nodeIndex].XLoc - ls.End.X)
                        {
                            distance = g.VList[nodeIndex].XLoc - ls.End.X;
                            nearestVertex = nodeIndex;
                        }
                    }
                }
            }
            if (distance == double.MaxValue)
                System.Diagnostics.Debug.WriteLine("No vertex Found Error");
            return nearestVertex;
        }


        static int FindClosestVertexWhileWalkingToStart(Tiling g, LineSegment ls, Point p)
        {
            double distance = double.MaxValue;
            int nearestVertex = -1;

            if ((int)ls.Start.X == (int)ls.End.X) //vertical
            {
                for (int nodeIndex = 0; nodeIndex < g.NumOfnodes; nodeIndex++)
                {
                    if (g.VList[nodeIndex].XLoc != (int)ls.Start.X) continue;
                    if (ls.Start.Y <= p.Y && g.VList[nodeIndex].YLoc >= ls.Start.Y && g.VList[nodeIndex].YLoc <= p.Y)
                    {
                        if (distance > p.Y - g.VList[nodeIndex].YLoc)
                        {
                            distance = p.Y - g.VList[nodeIndex].YLoc;
                            nearestVertex = nodeIndex;
                        }
                    }
                    if (ls.Start.Y >= p.Y && g.VList[nodeIndex].YLoc <= ls.Start.Y && g.VList[nodeIndex].YLoc >= p.Y)
                    {
                        if (distance > g.VList[nodeIndex].YLoc - p.Y)
                        {
                            distance = g.VList[nodeIndex].YLoc - p.Y;
                            nearestVertex = nodeIndex;
                        }
                    }
                }
            }
            if ((int)ls.Start.Y == (int)ls.End.Y) //horizontal
            {
                for (int nodeIndex = 0; nodeIndex < g.NumOfnodes; nodeIndex++)
                {
                    if (g.VList[nodeIndex].YLoc != (int)ls.Start.Y) continue;
                    if (ls.Start.X <= p.X && g.VList[nodeIndex].XLoc >= ls.Start.X && g.VList[nodeIndex].XLoc <= p.X)
                    {
                        if (distance > p.X - g.VList[nodeIndex].XLoc)
                        {
                            distance = p.X - g.VList[nodeIndex].XLoc;
                            nearestVertex = nodeIndex;
                        }
                    }
                    if (ls.Start.X >= p.X && g.VList[nodeIndex].XLoc <= ls.Start.X && g.VList[nodeIndex].XLoc >= p.X)
                    {
                        if (distance > g.VList[nodeIndex].XLoc - p.X)
                        {
                            distance = g.VList[nodeIndex].XLoc - p.X;
                            nearestVertex = nodeIndex;
                        }
                    }
                }
            }
            if (distance == double.MaxValue)
                System.Diagnostics.Debug.WriteLine("No vertex Found Error");
            return nearestVertex;
        }

        static int FindClosestVertexWhileWalkingToEnd(Tiling g, LineSegment ls, Point p)
        {
            double distance = double.MaxValue;
            var nearestVertex = -1;

            if ((int)ls.Start.X == (int)ls.End.X) //vertical
            {
                for (int nodeIndex = 0; nodeIndex < g.NumOfnodes; nodeIndex++)
                {
                    if (g.VList[nodeIndex].XLoc != (int)ls.Start.X) continue;
                    if (p.Y <= ls.End.Y && g.VList[nodeIndex].YLoc >= p.Y && g.VList[nodeIndex].YLoc <= ls.End.Y)
                    {
                        if (distance > g.VList[nodeIndex].YLoc - p.Y)
                        {
                            distance = g.VList[nodeIndex].YLoc - p.Y;
                            nearestVertex = nodeIndex;
                        }
                    }
                    if (p.Y >= ls.End.Y && g.VList[nodeIndex].YLoc <= p.Y && g.VList[nodeIndex].YLoc >= ls.End.Y)
                    {
                        if (distance > p.Y - g.VList[nodeIndex].YLoc)
                        {
                            distance = p.Y - g.VList[nodeIndex].YLoc;
                            nearestVertex = nodeIndex;
                        }
                    }

                }
            }
            if ((int)ls.Start.Y == (int)ls.End.Y) //horizontal
            {
                for (int nodeIndex = 0; nodeIndex < g.NumOfnodes; nodeIndex++)
                {
                    if (g.VList[nodeIndex].YLoc != (int)ls.Start.Y) continue;
                    if (p.X <= ls.End.X && g.VList[nodeIndex].XLoc >= p.X && g.VList[nodeIndex].XLoc <= ls.End.X)
                    {
                        if (distance > g.VList[nodeIndex].XLoc - p.X)
                        {
                            distance = g.VList[nodeIndex].XLoc - p.X;
                            nearestVertex = nodeIndex;
                        }
                    }
                    if (p.X >= ls.End.X && g.VList[nodeIndex].XLoc <= p.X && g.VList[nodeIndex].XLoc >= ls.End.X)
                    {
                        if (distance > p.X - g.VList[nodeIndex].XLoc)
                        {
                            distance = p.X - g.VList[nodeIndex].XLoc;
                            nearestVertex = nodeIndex;
                        }
                    }
                }
            }

            return nearestVertex;
        }

        public static void CreateCompetitionMesh(Tiling g, Dictionary<int, Node> idToNode, int maxX, int maxY)
        {

            //for each node, create four line segments
            CreateFourRaysPerVertex(g, maxX, maxY);

            Dictionary<LineSegment, int> removeList = new Dictionary<LineSegment, int>();
            Dictionary<LineSegment, int> addList = new Dictionary<LineSegment, int>();

            for (int iteration = 0; iteration <= Math.Max(maxX, maxY); iteration++)
            {
                //for each line segment check whether it hits any other segment or point
                //if so then create a new junction at that point
                for (int nodeIndex1 = 0; nodeIndex1 < g.N; nodeIndex1++)
                {
                    foreach (LineSegment ls1 in g.VList[nodeIndex1].SegmentList.Keys)
                    {
                        if (g.VList[nodeIndex1].SegmentList[ls1] == false) continue;

                        for (int nodeIndex2 = 0; nodeIndex2 < g.N; nodeIndex2++)
                        {
                            if (nodeIndex1 == nodeIndex2) continue;
                            foreach (LineSegment ls2 in g.VList[nodeIndex2].SegmentList.Keys)
                            {


                                if (MsaglUtilities.PointIsOnAxisAlignedSegment(ls2, ls1.End))
                                {





                                    //if they are parallel then create an edge
                                    if (((int)ls1.Start.X == (int)ls1.End.X && (int)ls2.Start.X == (int)ls2.End.X)
                                        || ((int)ls1.Start.Y == (int)ls1.End.Y && (int)ls2.Start.Y == (int)ls2.End.Y))
                                    {

                                        if ((int)ls1.End.X == (int)ls2.Start.X && (int)ls1.End.Y == (int)ls2.Start.Y) continue;

                                        int a = FindVertexClosestToSegmentEnd(g, ls1);
                                        int b = FindVertexClosestToSegmentEnd(g, ls2);

                                        if (a == b)
                                        {
                                            // degenerate parallel collision
                                            //Point coordinates multiplied by 4 so that this condition does not arise.
                                        }
                                        if (g.AddEdge(a, b))
                                        {
                                            LineSegment l = new LineSegment(ls1.Start, ls2.Start);
                                            if (!addList.ContainsKey(l)) addList.Add(l, -1);
                                            l = new LineSegment(ls2.Start, ls1.Start);
                                            if (!addList.ContainsKey(l)) addList.Add(l, -1);

                                            if (!removeList.ContainsKey(ls1)) removeList.Add(ls1, nodeIndex1);
                                            if (!removeList.ContainsKey(ls2)) removeList.Add(ls2, nodeIndex2);
                                        }
                                    }

                                    //create a new node at the intersection point                 
                                    else
                                    {
                                        if (MsaglUtilities.PointIsOnAxisAlignedSegment(ls2, ls1.End) &&
                                            MsaglUtilities.PointIsOnAxisAlignedSegment(ls1, ls2.End) &&
                                            nodeIndex1 > nodeIndex2) continue;

                                        if (g.GetNode((int)ls1.End.X, (int)ls1.End.Y) == -1)
                                        {

                                            //create the edges
                                            int a = FindClosestVertexWhileWalkingToStart(g, ls2, ls1.End);
                                            int b = FindClosestVertexWhileWalkingToEnd(g, ls2, ls1.End);
                                            int c = FindVertexClosestToSegmentEnd(g, ls1);
                                            int d = g.NumOfnodes;

                                            g.VList[d] = new Vertex((int)ls1.End.X, (int)ls1.End.Y) { Id = d };
                                            g.NumOfnodes++;

                                            if (a >= 0) g.AddEdge(a, d);
                                            if (b >= 0) g.AddEdge(b, d);
                                            if (c >= 0) g.AddEdge(c, d);
                                            if (a == b)
                                                System.Diagnostics.Debug.WriteLine("degenerate orthogonal collision");
                                            if (a >= 0 && b >= 0) g.RemoveEdge(a, b);

                                            if (!removeList.ContainsKey(ls1))
                                            {
                                                removeList.Add(ls1, nodeIndex1);
                                                if (!addList.ContainsKey(ls1)) addList.Add(ls1, -1);
                                            }

                                        }
                                        else
                                        {
                                            int a = g.GetNode((int)ls1.End.X, (int)ls1.End.Y);
                                            int b = FindVertexClosestToSegmentEnd(g, ls1);
                                            if (b == -1)
                                                System.Diagnostics.Debug.WriteLine("vertex not found error");
                                            g.AddEdge(a, b);

                                            if (!removeList.ContainsKey(ls1))
                                            {
                                                removeList.Add(ls1, nodeIndex1);
                                                if (!addList.ContainsKey(ls1)) addList.Add(ls1, -1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var s in removeList)
                {
                    g.VList[s.Value].SegmentList.Remove(s.Key);
                }
                removeList.Clear();
                foreach (var s in addList)
                {
                    if (s.Value >= 0) g.VList[s.Value].SegmentList.Add(s.Key, true);
                    else g.VList[g.GetNode((int)s.Key.Start.X, (int)s.Key.Start.Y)].SegmentList.Add(s.Key, false);
                }
                addList.Clear();


                int nodeIndex;
                for (nodeIndex = 0; nodeIndex < g.N; nodeIndex++)
                {
                    foreach (LineSegment ls in g.VList[nodeIndex].SegmentList.Keys)
                    {

                        if (g.VList[nodeIndex].SegmentList[ls] == false) continue;
                        Core.Geometry.Point nextPoint = GrowOneUnit(ls, maxX + 1, maxY + 1);
                        if (nextPoint.X >= 0)
                        {
                            LineSegment l_new = new LineSegment(ls.Start, nextPoint);


                            if (!addList.ContainsKey(l_new)) addList.Add(l_new, nodeIndex);
                            if (!removeList.ContainsKey(ls)) removeList.Add(ls, nodeIndex);
                        }

                    }
                }

                foreach (var s in removeList)
                {
                    g.VList[s.Value].SegmentList.Remove(s.Key);
                }
                removeList.Clear();
                foreach (var s in addList)
                {
                    if (s.Value >= 0) g.VList[s.Value].SegmentList.Add(s.Key, true);
                    else g.VList[g.GetNode((int)s.Key.Start.X, (int)s.Key.Start.Y)].SegmentList.Add(s.Key, false);
                }
                addList.Clear();

            }

            FixMesh(g);
        }

        private  static Dictionary<double, List<OrthogonalEdge>> HVEdges;
        private  static Dictionary<double, List<OrthogonalEdge>> VHEdges;

        public static void FastCompetitionMesh(Tiling g, Dictionary<int, Node> idToNode, int maxX, int maxY, Dictionary<Point, int> locationtoNode)
        {
            HVEdges = new Dictionary<double, List<OrthogonalEdge>>();
            VHEdges = new Dictionary<double, List<OrthogonalEdge>>();

            double[] Px = new double[g.N];
            double[] Py = new double[g.N];
            int[] Pid = new int[g.N];
            double[] temp = new double[g.N];
            for (int i = 0; i < g.N; i++)
            {
                Px[i] = g.VList[i].XLoc;
                Py[i] = g.VList[i].YLoc;
                Pid[i] = i;
                temp[i] = g.VList[i].YLoc;
            }




            //sort the points by y coordinate
            iterativeMergesort(temp, Px, Py, Pid);


            //find the closest point in the Xth cone
            Dictionary<int, int> Neighbors1 = ManhattanNearestNeighbor(Px, Py, Pid, 1, maxX, maxY);
            Dictionary<int, int> Neighbors8 = ManhattanNearestNeighbor(Px, Py, Pid, 8, maxX, maxY);
            Dictionary<int, int> Neighbors4 = ManhattanNearestNeighbor(Px, Py, Pid, 4, maxX, maxY);
            Dictionary<int, int> Neighbors5 = ManhattanNearestNeighbor(Px, Py, Pid, 5, maxX, maxY);

            //create the bounding box             
            int a = g.InsertVertexWithDuplicateCheck(0, 0, locationtoNode);
            int b = g.InsertVertexWithDuplicateCheck(0, maxY, locationtoNode);
            int c = g.InsertVertexWithDuplicateCheck(maxX, maxY, locationtoNode);
            int d = g.InsertVertexWithDuplicateCheck(maxX, 0, locationtoNode);
            g.AddEdge(a, b);
            g.AddEdge(b, c);
            g.AddEdge(c, d);
            g.AddEdge(d, a);


            HVEdges[0] = new List<OrthogonalEdge>();
            HVEdges[maxX] = new List<OrthogonalEdge>();
            VHEdges[0] = new List<OrthogonalEdge>();
            VHEdges[maxY] = new List<OrthogonalEdge>();

            HVEdges[0].Add(new OrthogonalEdge(g.VList[a], g.VList[b]));
            HVEdges[maxX].Add(new OrthogonalEdge(g.VList[c], g.VList[d]));
            VHEdges[0].Add(new OrthogonalEdge(g.VList[a], g.VList[d]));
            VHEdges[maxY].Add(new OrthogonalEdge(g.VList[b], g.VList[c]));

            //Process each left ray
            ProcessLeftRays(g, Neighbors4, Neighbors5, maxX, maxY, locationtoNode);
            //Process each Right ray
            ProcessRightRays(g, Neighbors1, Neighbors8, maxX, maxY, locationtoNode);
            //Process each Upward ray
            ProcessUpwardRays(g, maxX, maxY, locationtoNode);
            //Process each Downward ray
            ProcessDownwardRays(g, maxX, maxY, locationtoNode);

            int numOfVEdges = 0, numOfHEdges = 0;
            foreach (var list in HVEdges.Keys)
                numOfVEdges += HVEdges[list].Count;

            foreach (var list in VHEdges.Keys)
                numOfHEdges += VHEdges[list].Count;

        }


        public static void ProcessLeftRays(Tiling g, Dictionary<int, int> Neighbors4, Dictionary<int, int> Neighbors5, int maxX, int maxY, Dictionary<Point, int> locationtoNode)
        {
            int a;
            //LineSegment l;
            for (int i = 0; i < g.N; i++)
            {
                Vertex CurrentVertex = g.VList[i];
                if (CurrentVertex.leftRay == null)
                {
                    Vertex Neighbor = null;
                    Vertex Neighbor4 = null, Neighbor5 = null;
                    //find the closest neighbor in C4
                    double distance4 = double.MaxValue;
                    if (Neighbors4.ContainsKey(g.VList[i].Id))
                    {
                        Neighbor4 = g.VList[Neighbors4[g.VList[i].Id]];
                        distance4 = Math.Abs(Neighbor4.XLoc - CurrentVertex.XLoc) + Math.Abs(Neighbor4.YLoc - CurrentVertex.YLoc);
                    }
                    //find the closest neighbor in C5
                    double distance5 = double.MaxValue;
                    if (Neighbors5.ContainsKey(g.VList[i].Id))
                    {
                        Neighbor5 = g.VList[Neighbors5[g.VList[i].Id]];
                        distance5 = Math.Abs(Neighbor5.XLoc - CurrentVertex.XLoc) + Math.Abs(Neighbor5.YLoc - CurrentVertex.YLoc);
                    }

                    //if (distance4 == double.MaxValue && distance5 == double.MaxValue)
                    {
                        //hit the boundary because if there were a vertex on the way - then we did not reach this case
                        //a = g.InsertVertex(0, CurrentVertex.YLoc);
                        //g.addEdge(CurrentVertex.Id, a);
                        //there is no one that can stop the ray
                        //l = new LineSegment(CurrentVertex.XLoc, CurrentVertex.YLoc, 0, CurrentVertex.YLoc);
                        //CurrentVertex.leftRay = new Ray(l) { dead = true };
                    }
                    //else
                    {   //there is a ray that can stop the left ray
                        if (distance4 == double.MaxValue && distance5 == double.MaxValue)
                            Neighbor = g.VList[g.N + 1];
                        else if (distance4 < distance5) Neighbor = Neighbor4;
                        else Neighbor = Neighbor5;


                        //check if the neighbor is on the same y line of current vertex
                        if (CurrentVertex.YLoc == Neighbor.YLoc)
                        {

                            
                            //find the rightmost point of the neighbor
                            Vertex rightNeighbor;

                            do
                            {
                                rightNeighbor = Neighbor ;
                                for (int j = 0; j < g.DegList[Neighbor.Id]; j++)
                                {
                                    Vertex Candidate = g.VList[g.EList[Neighbor.Id, j].NodeId];
                                    if (Candidate.YLoc > Neighbor.YLoc)
                                    {
                                        Neighbor = Candidate;
                                        break;
                                    }
                                }
                            } while (rightNeighbor.Id != Neighbor.Id);
                            if (rightNeighbor.XLoc >= CurrentVertex.XLoc) continue;
                            //dont need to find left neighbor since this is the first time ray is growing


                            g.AddEdge(CurrentVertex.Id, rightNeighbor.Id);
                            if (!VHEdges.ContainsKey(CurrentVertex.YLoc))
                                VHEdges[CurrentVertex.YLoc] = new List<OrthogonalEdge>();                            
                            VHEdges[CurrentVertex.YLoc].Add(new OrthogonalEdge(CurrentVertex, rightNeighbor));

                        }
                        else
                        {
                            //check if the left ray already hits a vertical Edge r
                            OrthogonalEdge r = null;
                            Vertex ClosestVertex = Neighbor;
                            if (HVEdges.ContainsKey(Neighbor.XLoc))
                            {
                                foreach (var vEdge in HVEdges[Neighbor.XLoc])
                                {
                                    if (vEdge.a.YLoc <= CurrentVertex.YLoc && vEdge.b.YLoc >= CurrentVertex.YLoc)
                                    {
                                        r = vEdge;
                                        break;
                                    }
                                    //find the vertex on the line that is the closest one
                                    if (Neighbor.YLoc > CurrentVertex.YLoc &&
                                        Neighbor.YLoc >= vEdge.a.YLoc && vEdge.a.YLoc >= CurrentVertex.YLoc &&
                                        Neighbor.YLoc >= vEdge.b.YLoc && vEdge.b.YLoc >= CurrentVertex.YLoc)
                                    {
                                        if (Math.Abs(vEdge.a.YLoc - CurrentVertex.YLoc) <
                                            Math.Abs(ClosestVertex.YLoc - CurrentVertex.YLoc)) ClosestVertex = vEdge.a;
                                        if (Math.Abs(vEdge.b.YLoc - CurrentVertex.YLoc) <
                                            Math.Abs(ClosestVertex.YLoc - CurrentVertex.YLoc)) ClosestVertex = vEdge.b;
                                    }
                                    else if (Neighbor.YLoc < CurrentVertex.YLoc &&
                                        Neighbor.YLoc <= vEdge.a.YLoc && vEdge.a.YLoc <= CurrentVertex.YLoc &&
                                        Neighbor.YLoc <= vEdge.b.YLoc && vEdge.b.YLoc <= CurrentVertex.YLoc)
                                    {
                                        if (Math.Abs(vEdge.a.YLoc - CurrentVertex.YLoc) <
                                            Math.Abs(ClosestVertex.YLoc - CurrentVertex.YLoc)) ClosestVertex = vEdge.a;
                                        if (Math.Abs(vEdge.b.YLoc - CurrentVertex.YLoc) <
                                            Math.Abs(ClosestVertex.YLoc - CurrentVertex.YLoc)) ClosestVertex = vEdge.b;
                                    }
                                }
                            }
                            if (r != null)
                            {
                                a = g.InsertVertexWithDuplicateCheck(Neighbor.XLoc, CurrentVertex.YLoc, locationtoNode);
                                g.RemoveEdge(r.a.Id, r.b.Id);
                                g.AddEdge(r.a.Id, a);
                                g.AddEdge(r.b.Id, a);
                                g.AddEdge(CurrentVertex.Id, a);

                                if (!HVEdges.ContainsKey(Neighbor.XLoc)) 
                                    HVEdges[Neighbor.XLoc] = new List<OrthogonalEdge>();
                                if (!VHEdges.ContainsKey(CurrentVertex.YLoc))
                                    VHEdges[CurrentVertex.YLoc] = new List<OrthogonalEdge>();
                                HVEdges[Neighbor.XLoc].Remove(r);
                                HVEdges[Neighbor.XLoc].Add(new OrthogonalEdge(r.a, g.VList[a]));
                                HVEdges[Neighbor.XLoc].Add(new OrthogonalEdge(r.b, g.VList[a]));
                                VHEdges[CurrentVertex.YLoc].Add(new OrthogonalEdge(CurrentVertex, g.VList[a]));
                            }
                            else
                            {
                                a = g.InsertVertexWithDuplicateCheck(ClosestVertex.XLoc, CurrentVertex.YLoc, locationtoNode);
                                g.AddEdge(CurrentVertex.Id, a);
                                g.AddEdge(ClosestVertex.Id, a);

                                if (!HVEdges.ContainsKey(Neighbor.XLoc)) 
                                    HVEdges[Neighbor.XLoc] = new List<OrthogonalEdge>();
                                if (!VHEdges.ContainsKey(CurrentVertex.YLoc)) 
                                    VHEdges[CurrentVertex.YLoc] = new List<OrthogonalEdge>();
                                VHEdges[CurrentVertex.YLoc].Add(new OrthogonalEdge(CurrentVertex, g.VList[a]));
                                HVEdges[ClosestVertex.XLoc].Add(new OrthogonalEdge(ClosestVertex, g.VList[a]));
                            }
                        }

                    }

                }
            }
        }


        public static void ProcessUpwardRays(Tiling g, int maxX, int maxY, Dictionary<Point, int> locationtoNode)
        {
            int a;

            //sort all horizontal segments according to the y coordinates
            List<double> Y = new List<double>();
            List<OrthogonalEdge> E = new List<OrthogonalEdge>();

            foreach (var hEdges in VHEdges.Values)
            {
                foreach (var edge in hEdges)
                {
                    if (edge.a == null) continue;
                    E.Add(edge);                    
                    Y.Add(edge.a.YLoc);
                }
            }
            double[] ArrayOfY = Y.ToArray();
            OrthogonalEdge[] SortedHorizontalEdges = E.ToArray();
            Array.Sort(ArrayOfY, SortedHorizontalEdges);
            //done sorting


            //sort the points according to Y
            double[] PointYArray = new double[g.N];
            int[] PointIdArray = new int[g.N];
            for (int i = 0; i < g.N; i++)
            {
                PointYArray[i] = g.VList[i].YLoc;
                PointIdArray[i] = i;
            }
            Array.Sort(PointYArray, PointIdArray);
            //done sorting


            //keep the points in a RB tree - compared accorfing to y coordinates
            RbTree<Vertex> PointTreeSortedByX = new RbTree<Vertex>(new xCoordinateComparator());



            //for each horizontal edge check what are the points below it, and whether any of these can be extended
            OrthogonalEdge CurrentEdge;
            int index = 0;
            //for each hEdge om sprted order
            for (int i = 0; i < SortedHorizontalEdges.Length; i++)
            {
                CurrentEdge = SortedHorizontalEdges[i];
                var CurrentY = CurrentEdge.a.YLoc;
                var leftEnd = CurrentEdge.a.XLoc;
                var rightEnd = CurrentEdge.b.XLoc;



                //insert the points below CurrentY according to the y-coordinates 
                while (index < PointIdArray.Length && g.VList[PointIdArray[index]].YLoc < CurrentY)
                    PointTreeSortedByX.Insert(g.VList[PointIdArray[index++]]);

                //do a search to find the intersecting points
                var node1 = PointTreeSortedByX.FindLast(v => v.XLoc >= leftEnd);
                var node2 = PointTreeSortedByX.FindFirst(v => v.XLoc <= rightEnd);
                if (node1 != null && node1.Item.XLoc > rightEnd) node1 = null;
                if (node2 != null && node2.Item.XLoc < leftEnd) node2 = null;
                if (node1 == null || node2 == null) continue;


                //collect all the candidate vertices
                var node = node2;
                List<Vertex> nodesToCheck = new List<Vertex>();

                if (node1.Item.Id == node2.Item.Id) nodesToCheck.Add(node.Item);
                else
                {
                    while (node.Item.Id != node1.Item.Id)
                    {
                        nodesToCheck.Add(node.Item);
                        node = PointTreeSortedByX.Next(node);
                    }
                    nodesToCheck.Add(node.Item);
                }


                List<Vertex> addedVertices = new List<Vertex>();
                List<Vertex> nodesToRemove = new List<Vertex>();
                //filter out the nodes that must be connecte to the hEdge
                foreach (var w in nodesToCheck)
                {
                    //find the topmost neighbor of w
                    int CurrentVertexId = w.Id;
                    int topNeighborId;
                    do
                    {
                        topNeighborId = CurrentVertexId;
                        for (int j = 0; j < g.DegList[CurrentVertexId]; j++)
                        {
                            Vertex Candidate = g.VList[g.EList[CurrentVertexId, j].NodeId];
                            if (Candidate.YLoc > g.VList[CurrentVertexId].YLoc)
                            {
                                CurrentVertexId = Candidate.Id;
                                break;
                            }
                        }
                    } while (topNeighborId != CurrentVertexId);
                    if (g.VList[topNeighborId].YLoc >= CurrentY) continue;

                    //add a subdivision
                    a = g.InsertVertexWithDuplicateCheck(g.VList[topNeighborId].XLoc, CurrentY, locationtoNode);
                    addedVertices.Add(g.VList[a]);


                    g.AddEdge(topNeighborId, a);
                    if (!HVEdges.ContainsKey(g.VList[topNeighborId].XLoc))
                        HVEdges[g.VList[topNeighborId].XLoc] = new List<OrthogonalEdge>();
                    HVEdges[g.VList[topNeighborId].XLoc].Add(new OrthogonalEdge(g.VList[topNeighborId], g.VList[a]));

                    nodesToRemove.Add(w);

                }


                foreach (var newnode in addedVertices)
                {


                    bool e1 = g.AddEdge(CurrentEdge.a.Id, newnode.Id);
                    if (!VHEdges.ContainsKey(newnode.YLoc))
                        VHEdges[newnode.YLoc] = new List<OrthogonalEdge>();
                    VHEdges[newnode.YLoc].Add(new OrthogonalEdge(CurrentEdge.a, newnode));

                    bool e2 = g.AddEdge(CurrentEdge.b.Id, newnode.Id);
                    if (!VHEdges.ContainsKey(newnode.YLoc))
                        VHEdges[newnode.YLoc] = new List<OrthogonalEdge>();
                    VHEdges[newnode.YLoc].Add(new OrthogonalEdge(CurrentEdge.b, newnode));


                    if (e1 && e2)
                    {
                        g.RemoveEdge(CurrentEdge.a.Id, CurrentEdge.b.Id);
                        if (VHEdges.ContainsKey(CurrentEdge.b.YLoc))
                            VHEdges[CurrentEdge.b.YLoc].Remove(CurrentEdge);

                    }


                    CurrentEdge = new OrthogonalEdge(newnode, CurrentEdge.a);


                }

                foreach (var w in nodesToRemove)
                    PointTreeSortedByX.Remove(w);

            }
        }


        public static void ProcessDownwardRays(Tiling g, int maxX, int maxY, Dictionary<Point, int> locationtoNode)
        {
            int a;
            //LineSegment l;

            //sort all horizontal segments according to the -y coordinates
            List<double> Y = new List<double>();
            List<OrthogonalEdge> E = new List<OrthogonalEdge>();

            foreach (var hEdges in VHEdges.Values)
            {
                foreach (var edge in hEdges)
                {
                    if(edge.a == null) continue;
                    E.Add(edge);
                    Y.Add(-edge.a.YLoc);
                }
            }
            double[] ArrayOfY = Y.ToArray();
            OrthogonalEdge[] SortedHorizontalEdges = E.ToArray();
            Array.Sort(ArrayOfY, SortedHorizontalEdges);
            //done sorting


            //sort the points according to -Y
            double[] PointYArray = new double[g.N];
            int[] PointIdArray = new int[g.N];
            for (int i = 0; i < g.N; i++)
            {
                PointYArray[i] = -g.VList[i].YLoc;
                PointIdArray[i] = i;
            }
            Array.Sort(PointYArray, PointIdArray);
            //done sorting


            //keep the points in a RB tree - compared accorfing to y coordinates
            RbTree<Vertex> PointTreeSortedByX = new RbTree<Vertex>(new xCoordinateComparator());



            //for each horizontal edge check what are the points below it, and whether any of these can be extended
            OrthogonalEdge CurrentEdge;
            int index = 0;
            //for each hEdge on sorted top to bottom order
            for (int i = 0; i < SortedHorizontalEdges.Length; i++)
            {
                CurrentEdge = SortedHorizontalEdges[i];
                var CurrentY = CurrentEdge.a.YLoc;
                var leftEnd = CurrentEdge.a.XLoc;
                var rightEnd = CurrentEdge.b.XLoc;



                //insert the points above CurrentY according to the -y coordinates 
                while (index < PointIdArray.Length && g.VList[PointIdArray[index]].YLoc > CurrentY)
                    PointTreeSortedByX.Insert(g.VList[PointIdArray[index++]]);

                //do a search to find the intersecting points
                var node1 = PointTreeSortedByX.FindLast(v => v.XLoc >= leftEnd);
                var node2 = PointTreeSortedByX.FindFirst(v => v.XLoc <= rightEnd);
                if (node1 != null && node1.Item.XLoc > rightEnd) node1 = null;
                if (node2 != null && node2.Item.XLoc < leftEnd) node2 = null;
                if (node1 == null || node2 == null) continue;


                //collect all the candidate vertices
                var node = node2;
                List<Vertex> nodesToCheck = new List<Vertex>();

                if (node1.Item.Id == node2.Item.Id) nodesToCheck.Add(node.Item);
                else
                {
                    while (node.Item.Id != node1.Item.Id)
                    {
                        nodesToCheck.Add(node.Item);
                        node = PointTreeSortedByX.Next(node);
                    }
                    nodesToCheck.Add(node.Item);
                }


                List<Vertex> addedVertices = new List<Vertex>();
                List<Vertex> nodesToRemove = new List<Vertex>();
                //filter out the nodes that must be connecte to the hEdge
                foreach (var w in nodesToCheck)
                {
                    //find the bottommost neighbor of w
                    Vertex CurrentVertex = w;
                    Vertex bottomNeighbor;
                    do
                    {
                        bottomNeighbor = CurrentVertex;
                        for (int j = 0; j < g.DegList[CurrentVertex.Id]; j++)
                        {
                            Vertex Candidate = g.VList[g.EList[CurrentVertex.Id, j].NodeId];
                            if (Candidate.YLoc < CurrentVertex.YLoc)
                            {
                                CurrentVertex = Candidate;
                                break;
                            }
                        }
                    } while (bottomNeighbor.Id != CurrentVertex.Id);
                    if (bottomNeighbor.YLoc <= CurrentY) continue;

                    //add a subdivision
                    a = g.InsertVertexWithDuplicateCheck(bottomNeighbor.XLoc, CurrentY, locationtoNode);
                    addedVertices.Add(g.VList[a]);

                    g.AddEdge(bottomNeighbor.Id, a);
                    if (!HVEdges.ContainsKey(bottomNeighbor.XLoc))
                        HVEdges[bottomNeighbor.XLoc] = new List<OrthogonalEdge>();
                    HVEdges[bottomNeighbor.XLoc].Add(new OrthogonalEdge(bottomNeighbor, g.VList[a]));

                    nodesToRemove.Add(w);

                }


                foreach (var newnode in addedVertices)
                {
                    bool e1 = g.AddEdge(CurrentEdge.a.Id, newnode.Id);
                    if (!VHEdges.ContainsKey(newnode.YLoc))
                        VHEdges[newnode.YLoc] = new List<OrthogonalEdge>();
                    VHEdges[newnode.YLoc].Add(new OrthogonalEdge(CurrentEdge.a, newnode));

                    bool e2 = g.AddEdge(CurrentEdge.b.Id, newnode.Id);
                    if (!VHEdges.ContainsKey(newnode.YLoc))
                        VHEdges[newnode.YLoc] = new List<OrthogonalEdge>();
                    VHEdges[newnode.YLoc].Add(new OrthogonalEdge(CurrentEdge.b, newnode));

                    if (e1 && e2)
                    {
                        g.RemoveEdge(CurrentEdge.a.Id, CurrentEdge.b.Id);
                        if (VHEdges.ContainsKey(CurrentEdge.b.YLoc))
                            VHEdges[CurrentEdge.b.YLoc].Remove(CurrentEdge);
                    }
                    CurrentEdge = new OrthogonalEdge(newnode, CurrentEdge.a);
                }

                foreach (var w in nodesToRemove)
                    PointTreeSortedByX.Remove(w);

            }
        }

        public static void ProcessRightRays(Tiling g, Dictionary<int, int> Neighbors1, Dictionary<int, int> Neighbors8, int maxX, int maxY, Dictionary<Point, int> locationtoNode)
        {
            int a;
            //LineSegment l;
            for (int i = 0; i < g.N; i++)
            {
                Vertex CurrentVertex = g.VList[i];
                if (CurrentVertex.leftRay == null)
                {
                    Vertex Neighbor = null;
                    Vertex Neighbor1 = null, Neighbor8 = null;
                    //find the closest neighbor in C4
                    double distance1 = double.MaxValue;
                    if (Neighbors1.ContainsKey(g.VList[i].Id))
                    {
                        Neighbor1 = g.VList[Neighbors1[g.VList[i].Id]];
                        distance1 = Math.Abs(Neighbor1.XLoc - CurrentVertex.XLoc) +
                                    Math.Abs(Neighbor1.YLoc - CurrentVertex.YLoc);
                    }
                    //find the closest neighbor in C5
                    double distance8 = double.MaxValue;
                    if (Neighbors8.ContainsKey(g.VList[i].Id))
                    {
                        Neighbor8 = g.VList[Neighbors8[g.VList[i].Id]];
                        distance8 = Math.Abs(Neighbor8.XLoc - CurrentVertex.XLoc) +
                                    Math.Abs(Neighbor8.YLoc - CurrentVertex.YLoc);
                    }


                    //there is a ray that can stop the left ray
                    if (distance1 == double.MaxValue && distance8 == double.MaxValue)
                        Neighbor = g.VList[g.N + 2];
                    else if (distance1 < distance8) Neighbor = Neighbor1;
                    else Neighbor = Neighbor8;

                    //check if the neighbor is on the same y line of current vertex
                    if (CurrentVertex.YLoc == Neighbor.YLoc)
                    {

                        //find the left end vertex starting from the neighbor
                        Vertex leftneighbor;
                        do
                        {
                            leftneighbor = Neighbor;
                            for (int j = 0; j < g.DegList[Neighbor.Id]; j++)
                            {
                                Vertex Candidate = g.VList[g.EList[Neighbor.Id, j].NodeId];
                                if (Candidate.XLoc < Neighbor.XLoc)
                                {
                                    Neighbor = Candidate;
                                    break;
                                }
                            }
                        } while (leftneighbor.Id != Neighbor.Id);
                        if (leftneighbor.XLoc <= CurrentVertex.XLoc) continue;
                        //no need to find the right neighbor since this is the first time it is growing

                        //add the edge 
                        g.AddEdge(CurrentVertex.Id, Neighbor.Id);
                        if (!VHEdges.ContainsKey(CurrentVertex.YLoc))
                            VHEdges[CurrentVertex.YLoc] = new List<OrthogonalEdge>();
                        VHEdges[CurrentVertex.YLoc].Add(new OrthogonalEdge(CurrentVertex, Neighbor));

                    }
                    else
                    {
                        //check if the right ray already hits a vertical Edge r
                        OrthogonalEdge r = null;
                        Vertex ClosestVertex = Neighbor;
                        if (HVEdges.ContainsKey(Neighbor.XLoc))
                        {
                            foreach (var vEdge in HVEdges[Neighbor.XLoc])
                            {
                                //check if there is an edge already that is blocking, in that case it must be the ray from the neighbor
                                if (vEdge.a.YLoc <= CurrentVertex.YLoc && vEdge.b.YLoc >= CurrentVertex.YLoc)
                                {
                                    r = vEdge;
                                    break;
                                }
                                //otherwise find the vertex on the line that is the closest one and between the neighbor and current vertex
                                if (Neighbor.YLoc > CurrentVertex.YLoc &&
                                    Neighbor.YLoc >= vEdge.a.YLoc && vEdge.a.YLoc >= CurrentVertex.YLoc &&
                                    Neighbor.YLoc >= vEdge.b.YLoc && vEdge.b.YLoc >= CurrentVertex.YLoc)
                                {
                                    if (Math.Abs(vEdge.a.YLoc - CurrentVertex.YLoc) <
                                        Math.Abs(ClosestVertex.YLoc - CurrentVertex.YLoc)) ClosestVertex = vEdge.a;
                                    if (Math.Abs(vEdge.b.YLoc - CurrentVertex.YLoc) <
                                        Math.Abs(ClosestVertex.YLoc - CurrentVertex.YLoc)) ClosestVertex = vEdge.b;
                                }
                                else if (Neighbor.YLoc < CurrentVertex.YLoc &&
                                         Neighbor.YLoc <= vEdge.a.YLoc && vEdge.a.YLoc <= CurrentVertex.YLoc &&
                                         Neighbor.YLoc <= vEdge.b.YLoc && vEdge.b.YLoc <= CurrentVertex.YLoc)
                                {
                                    if (Math.Abs(vEdge.a.YLoc - CurrentVertex.YLoc) <
                                        Math.Abs(ClosestVertex.YLoc - CurrentVertex.YLoc)) ClosestVertex = vEdge.a;
                                    if (Math.Abs(vEdge.b.YLoc - CurrentVertex.YLoc) <
                                        Math.Abs(ClosestVertex.YLoc - CurrentVertex.YLoc)) ClosestVertex = vEdge.b;
                                }
                            }
                        }
                        if (r != null)
                        {
                            a = g.InsertVertexWithDuplicateCheck(Neighbor.XLoc, CurrentVertex.YLoc, locationtoNode);
                            g.RemoveEdge(r.a.Id, r.b.Id);
                            g.AddEdge(r.a.Id, a);
                            g.AddEdge(r.b.Id, a);
                            g.AddEdge(CurrentVertex.Id, a);

                            if (!HVEdges.ContainsKey(Neighbor.XLoc))
                                HVEdges[Neighbor.XLoc] = new List<OrthogonalEdge>();
                            if (!VHEdges.ContainsKey(CurrentVertex.YLoc))
                                VHEdges[CurrentVertex.YLoc] = new List<OrthogonalEdge>();
                            HVEdges[Neighbor.XLoc].Remove(r);
                            HVEdges[Neighbor.XLoc].Add(new OrthogonalEdge(r.a, g.VList[a]));
                            HVEdges[Neighbor.XLoc].Add(new OrthogonalEdge(r.b, g.VList[a]));
                            VHEdges[CurrentVertex.YLoc].Add(new OrthogonalEdge(CurrentVertex, g.VList[a]));
                        }
                        else
                        {
                            a = g.InsertVertexWithDuplicateCheck(ClosestVertex.XLoc, CurrentVertex.YLoc, locationtoNode);
                            g.AddEdge(CurrentVertex.Id, a);
                            g.AddEdge(ClosestVertex.Id, a);

                            if (!HVEdges.ContainsKey(Neighbor.XLoc))
                                HVEdges[Neighbor.XLoc] = new List<OrthogonalEdge>();
                            if (!VHEdges.ContainsKey(CurrentVertex.YLoc))
                                VHEdges[CurrentVertex.YLoc] = new List<OrthogonalEdge>();
                            VHEdges[CurrentVertex.YLoc].Add(new OrthogonalEdge(CurrentVertex, g.VList[a]));
                            HVEdges[ClosestVertex.XLoc].Add(new OrthogonalEdge(ClosestVertex, g.VList[a]));

                        }
                    }



                }
            }
        }

        public static Dictionary<int, int> ManhattanNearestNeighbor(double[] X, double[] Y, int[] ID, int ConeId, double maxX, double maxY)
        {
            double[] a = new double[ID.Length];
            double[] Px = new double[ID.Length];
            double[] Py = new double[ID.Length];
            int[] Pid = new int[ID.Length];

            for (int i = 0; i < ID.Length; i++)
            {
                if (ConeId == 4)
                {
                    Px[i] = X[i];
                    Py[i] = Y[i];
                }
                if (ConeId == 1)
                {
                    Px[i] = -X[i] + maxX;
                    Py[i] = Y[i];
                }
                if (ConeId == 8)
                {
                    Px[i] = -X[i] + maxX;
                    Py[i] = -Y[i] + maxY;
                }
                if (ConeId == 5)
                {
                    Px[i] = X[i];
                    Py[i] = -Y[i] + maxY;
                }
                Pid[i] = ID[i];
                a[i] = Px[i] + Py[i];

            }

            Dictionary<int, int> neighborlist = new Dictionary<int, int>();
            double[] from = a;
            double[] to = new double[a.Length];

            int[] toPid = new int[a.Length];
            double[] toPx = new double[a.Length];
            double[] toPy = new double[a.Length];

            Dictionary<int, double> IdToX = new Dictionary<int, double>();
            for (int k = 0; k < a.Length; k++) IdToX.Add(Pid[k], Px[k]);
            Dictionary<int, double> IdToY = new Dictionary<int, double>();
            for (int k = 0; k < a.Length; k++) IdToY.Add(Pid[k], Py[k]);
            Dictionary<int, int> PosToId = new Dictionary<int, int>();
            for (int k = 0; k < a.Length; k++) PosToId.Add(k, Pid[k]);


            for (int blockSize = 1; blockSize < a.Length; blockSize *= 2)
            {
                for (int start = 0; start < a.Length; start += 2 * blockSize)
                    FindNeighbor(from, to, start, start + blockSize, start + 2 * blockSize, Px, Py, Pid, toPx, toPy, toPid, neighborlist, IdToX, IdToY, PosToId);
            }

            return neighborlist;
        }
        private static void FindNeighbor(double[] from, double[] to, int lo, int mid, int hi, double[] Px, double[] Py, int[] Pid, double[] toPx, double[] toPy, int[] toPid, Dictionary<int, int> neighborlist, Dictionary<int, double> IdToX, Dictionary<int, double> IdToY, Dictionary<int, int> PosToId)
        {
            if (mid > from.Length) mid = from.Length;
            if (hi > from.Length) hi = from.Length;
            int i = lo, j = mid;
            //sort all the points according to x+y
            for (int k = lo; k < hi; k++)
            {
                if (i == mid) { Assign(k, j, from, to, Px, Py, Pid, toPx, toPy, toPid); j++; }
                else if (j == hi) { Assign(k, i, from, to, Px, Py, Pid, toPx, toPy, toPid); i++; }
                else if (from[j] < from[i]) { Assign(k, j, from, to, Px, Py, Pid, toPx, toPy, toPid); j++; }
                else if (from[i] == from[j] && Px[i] < Px[j]) { Assign(k, i, from, to, Px, Py, Pid, toPx, toPy, toPid); i++; }
                else if (from[i] == from[j] && Px[i] >= Px[j]) { Assign(k, j, from, to, Px, Py, Pid, toPx, toPy, toPid); j++; }
                else { Assign(k, i, from, to, Px, Py, Pid, toPx, toPy, toPid); i++; }
            }






            //foreach point in x+y order
            double LargestXMinusY = double.MinValue;
            int CandidateNeighborId = -1;
            for (int k = lo; k < hi; k++)
            {
                int currentPointId = toPid[k];



                //find the neighbor
                if (currentPointId != CandidateNeighborId)
                {// if the point is on lower half
                    if (CandidateNeighborId >= 0)
                    {

                        if (neighborlist.ContainsKey(currentPointId))
                        {
                            //compare with the current neighbor
                            int currentneighborId = neighborlist[currentPointId];

                            double currentNeighborValue = IdToX[currentneighborId] - IdToY[currentneighborId];
                            if (currentNeighborValue < LargestXMinusY && IdToY[CandidateNeighborId] >= IdToY[currentPointId])
                            {
                                neighborlist[currentPointId] = CandidateNeighborId;
                            }
                        }
                        else
                        {
                            if (IdToY[CandidateNeighborId] >= IdToY[currentPointId])
                                neighborlist.Add(currentPointId, CandidateNeighborId);
                        }
                    }
                }

                //process current point
                if (mid == from.Length) --mid;
                if (IdToY[currentPointId] >= IdToY[PosToId[mid]] && (IdToX[currentPointId] - IdToY[currentPointId] >= LargestXMinusY))
                {
                    LargestXMinusY = IdToX[currentPointId] - IdToY[currentPointId];
                    CandidateNeighborId = currentPointId;
                }

            }

            for (int k = lo; k < hi; k++)
                Assign(k, k, to, from, toPx, toPy, toPid, Px, Py, Pid);

        }


        public static void Assign(int k, int j, double[] from, double[] to, double[] Px, double[] Py, int[] Pid, double[] toPx, double[] toPy, int[] toPid)
        {
            to[k] = from[j]; toPid[k] = Pid[j]; toPx[k] = Px[j]; toPy[k] = Py[j];
        }
        public static void iterativeMergesort(double[] a, double[] Px, double[] Py, int[] Pid)
        {
            double[] from = a;
            double[] to = new double[a.Length];
            int[] toPid = new int[a.Length];
            double[] toPx = new double[a.Length];
            double[] toPy = new double[a.Length];
            for (int blockSize = 1; blockSize < a.Length; blockSize *= 2)
            {
                for (int start = 0; start < a.Length; start += 2 * blockSize)
                    merge(from, to, start, start + blockSize, start + 2 * blockSize, Px, Py, Pid, toPx, toPy, toPid);
            }
            for (int k = 0; k < a.Length; k++)
                a[k] = from[k];
        }

        private static void merge(double[] from, double[] to, int lo, int mid, int hi, double[] Px, double[] Py, int[] Pid, double[] toPx, double[] toPy, int[] toPid)
        {
            if (mid > from.Length) mid = from.Length;
            if (hi > from.Length) hi = from.Length;
            int i = lo, j = mid;
            for (int k = lo; k < hi; k++)
            {
                if (i == mid) { Assign(k, j, from, to, Px, Py, Pid, toPx, toPy, toPid); j++; }
                else if (j == hi) { Assign(k, i, from, to, Px, Py, Pid, toPx, toPy, toPid); i++; }
                else if (from[j] < from[i]) { Assign(k, j, from, to, Px, Py, Pid, toPx, toPy, toPid); j++; }
                else if (from[i] == from[j] && Px[i] < Px[j]) { Assign(k, i, from, to, Px, Py, Pid, toPx, toPy, toPid); i++; }
                else if (from[i] == from[j] && Px[i] >= Px[j]) { Assign(k, j, from, to, Px, Py, Pid, toPx, toPy, toPid); j++; }
                else { Assign(k, i, from, to, Px, Py, Pid, toPx, toPy, toPid); i++; }
            }
            for (int k = lo; k < hi; k++)
                Assign(k, k, to, from, toPx, toPy, toPid, Px, Py, Pid);

        }

        private static void FixMesh(Tiling g)
        {
            for (int i = 0; i < g.NumOfnodes; i++)
            {
                if (g.DegList[i] == 0) continue;
                for (int j = g.N; j < g.NumOfnodes; j++)
                {
                    if (i == j) continue;
                    if (g.VList[i].XLoc == g.VList[j].XLoc && g.VList[i].YLoc == g.VList[j].YLoc)
                    {
                        for (int neighborIndex = 0; neighborIndex < g.DegList[j]; neighborIndex++)
                        {
                            if (g.DegList[g.EList[j, neighborIndex].NodeId] == 0) continue;
                            g.AddEdge(i, g.EList[j, neighborIndex].NodeId);
                        }
                        g.DegList[j] = 0;
                        g.VList[j].Invalid = true;
                    }
                }
            }


            for (int nodeIndex1 = 0; nodeIndex1 < g.N; nodeIndex1++)
            {
                foreach (LineSegment ls1 in g.VList[nodeIndex1].SegmentList.Keys)
                {
                    for (int nodeIndex2 = nodeIndex1 + 1; nodeIndex2 < g.N; nodeIndex2++)
                    {
                        foreach (LineSegment ls2 in g.VList[nodeIndex2].SegmentList.Keys)
                        {
                            if (ls1.Start.Equals(ls2.End) && ls2.Start.Equals(ls1.End))
                            {
                                List<Core.Geometry.Point> list = new List<Core.Geometry.Point>();
                                for (int index = 0; index < g.N; index++)
                                {
                                    Core.Geometry.Point p = new Core.Geometry.Point(g.VList[index].XLoc, g.VList[index].YLoc);
                                    if (MsaglUtilities.PointIsOnAxisAlignedSegment(ls1, p) ||
                                        MsaglUtilities.PointIsOnAxisAlignedSegment(ls2, p))
                                        list.Add(p);
                                }
                                if (list.Count > 2)
                                {
                                    list.Sort();
                                    Core.Geometry.Point[] points = list.ToArray();

                                    for (int i = 0; i < points.Length; i++)
                                        for (int j = i + 1; j < points.Length; j++)
                                            g.RemoveEdge(g.GetNode((int)points[i].X, (int)points[i].Y),
                                                g.GetNode((int)points[j].X, (int)points[j].Y));

                                    for (int i = 0; i < points.Length - 1; i++)
                                    {
                                        g.AddEdge(g.GetNode((int)points[i].X, (int)points[i].Y),
                                            g.GetNode((int)points[i + 1].X, (int)points[i + 1].Y));
                                    }
                                }
                            }
                        }
                    }
                }
            }


            for (int nodeIndex = 0; nodeIndex < g.NumOfnodes; nodeIndex++)
                g.nodeTree.Add(new Rectangle(new Core.Geometry.Point(g.VList[nodeIndex].XLoc, g.VList[nodeIndex].YLoc)),
                    nodeIndex);


            bool searchNew = true;
            while (searchNew)
            {
                searchNew = false;
                for (int nodeIndex1 = 0; nodeIndex1 < g.NumOfnodes; nodeIndex1++)
                {
                    for (int neighborIndex = 0; neighborIndex < g.DegList[nodeIndex1]; neighborIndex++)
                    {
                        int neighborId = g.EList[nodeIndex1, neighborIndex].NodeId;

                        Core.Geometry.Point a = new Core.Geometry.Point(g.VList[nodeIndex1].XLoc, g.VList[nodeIndex1].YLoc);
                        Core.Geometry.Point b = new Core.Geometry.Point(g.VList[neighborId].XLoc, g.VList[neighborId].YLoc);

                        int[] intersectedVertices = g.nodeTree.GetAllIntersecting(new Rectangle(a, b));

                        //check if there is any other node on this edge
                        for (int nodeIndex2 = 0; nodeIndex2 < intersectedVertices.Length; nodeIndex2++)
                        {
                            int currentVertexId = intersectedVertices[nodeIndex2];
                            if (g.VList[currentVertexId].Invalid) continue;
                            if (currentVertexId == nodeIndex1 || currentVertexId == neighborId) continue;

                            Core.Geometry.Point p = new Point(g.VList[currentVertexId].XLoc, g.VList[currentVertexId].YLoc);
                            LineSegment ls = new LineSegment(a, b);

                            if (MsaglUtilities.PointIsOnSegment(ls, p))
                            {
                                g.RemoveEdge(nodeIndex1, neighborId);
                                g.AddEdge(nodeIndex1, currentVertexId);
                                g.AddEdge(currentVertexId, neighborId);
                                searchNew = true;
                                break;
                            }
                        }
                        if (searchNew) break;
                    }
                    if (searchNew) break;
                }
            }
        }


        public static void CreateCompetitionMeshWithLeftPriority(Tiling g, Dictionary<int, Node> idToNode, int maxX, int maxY)
        {

            //for each node, create four line segments
            CreateFourRaysPerVertex(g, maxX, maxY);

            Dictionary<LineSegment, int> removeList = new Dictionary<LineSegment, int>();
            Dictionary<LineSegment, int> addList = new Dictionary<LineSegment, int>();

            for (int iteration = 0; iteration <= Math.Max(maxX, maxY); iteration++)
            {
                //for each line segment check whether it hits any other segment or point
                //if so then create a new junction at that point
                for (int nodeIndex1 = 0; nodeIndex1 < g.N; nodeIndex1++)
                {
                    foreach (LineSegment ls1 in g.VList[nodeIndex1].SegmentList.Keys)
                    {
                        if (g.VList[nodeIndex1].SegmentList[ls1] == false) continue;

                        for (int nodeIndex2 = 0; nodeIndex2 < g.N; nodeIndex2++)
                        {
                            if (nodeIndex1 == nodeIndex2) continue;
                            foreach (LineSegment ls2 in g.VList[nodeIndex2].SegmentList.Keys)
                            {


                                if (MsaglUtilities.PointIsOnAxisAlignedSegment(ls2, ls1.End))
                                {

                                    //if they are parallel then create an edge
                                    if (((int)ls1.Start.X == (int)ls1.End.X && (int)ls2.Start.X == (int)ls2.End.X)
                                        || ((int)ls1.Start.Y == (int)ls1.End.Y && (int)ls2.Start.Y == (int)ls2.End.Y))
                                    {

                                        if ((int)ls1.End.X == (int)ls2.Start.X && (int)ls1.End.Y == (int)ls2.Start.Y) continue;

                                        int a = FindVertexClosestToSegmentEnd(g, ls1);
                                        int b = FindVertexClosestToSegmentEnd(g, ls2);

                                        if (a == b)
                                        {
                                            // degenerate parallel collision
                                            //Point coordinates multiplied by >=3 so that this condition does not arise.
                                        }
                                        if (g.AddEdge(a, b))
                                        {
                                            LineSegment l = new LineSegment(ls1.Start, ls2.Start);
                                            if (!addList.ContainsKey(l)) addList.Add(l, -1);
                                            l = new LineSegment(ls2.Start, ls1.Start);
                                            if (!addList.ContainsKey(l)) addList.Add(l, -1);

                                            if (!removeList.ContainsKey(ls1)) removeList.Add(ls1, nodeIndex1);
                                            if (!removeList.ContainsKey(ls2)) removeList.Add(ls2, nodeIndex2);
                                        }
                                    }

                                    //create a new node at the intersection point                 
                                    else
                                    {
                                        if (MsaglUtilities.PointIsOnAxisAlignedSegment(ls2, ls1.End) &&
                                            MsaglUtilities.PointIsOnAxisAlignedSegment(ls1, ls2.End) &&
                                            nodeIndex1 > nodeIndex2) continue;

                                        if (g.GetNode((int)ls1.End.X, (int)ls1.End.Y) == -1)
                                        {

                                            //create the edges
                                            int a = FindClosestVertexWhileWalkingToStart(g, ls2, ls1.End);
                                            int b = FindClosestVertexWhileWalkingToEnd(g, ls2, ls1.End);
                                            int c = FindVertexClosestToSegmentEnd(g, ls1);
                                            int d = g.NumOfnodes;

                                            g.VList[d] = new Vertex((int)ls1.End.X, (int)ls1.End.Y) { Id = d };
                                            g.NumOfnodes++;

                                            if (a >= 0) g.AddEdge(a, d);
                                            if (b >= 0) g.AddEdge(b, d);
                                            if (c >= 0) g.AddEdge(c, d);
                                            if (a == b)
                                                System.Diagnostics.Debug.WriteLine("degenerate");
                                            if (a >= 0 && b >= 0) g.RemoveEdge(a, b);

                                            if (!removeList.ContainsKey(ls1) && MsaglUtilities.HittedSegmentComesFromLeft(ls2, ls1))
                                            {
                                                removeList.Add(ls1, nodeIndex1);
                                                if (!addList.ContainsKey(ls1)) addList.Add(ls1, -1);
                                            }

                                        }
                                        else
                                        {
                                            int a = g.GetNode((int)ls1.End.X, (int)ls1.End.Y);
                                            int b = FindVertexClosestToSegmentEnd(g, ls1);
                                            if (b == -1)
                                                System.Diagnostics.Debug.WriteLine("vertex not found error");
                                            g.AddEdge(a, b);

                                            if (!removeList.ContainsKey(ls1) && MsaglUtilities.HittedSegmentComesFromLeft(ls2, ls1))
                                            {
                                                removeList.Add(ls1, nodeIndex1);
                                                if (!addList.ContainsKey(ls1)) addList.Add(ls1, -1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var s in removeList)
                {
                    g.VList[s.Value].SegmentList.Remove(s.Key);
                }
                removeList.Clear();
                foreach (var s in addList)
                {
                    if (s.Value >= 0) g.VList[s.Value].SegmentList.Add(s.Key, true);
                    else g.VList[g.GetNode((int)s.Key.Start.X, (int)s.Key.Start.Y)].SegmentList.Add(s.Key, false);
                }
                addList.Clear();


                int nodeIndex;
                for (nodeIndex = 0; nodeIndex < g.N; nodeIndex++)
                {
                    foreach (LineSegment ls in g.VList[nodeIndex].SegmentList.Keys)
                    {

                        if (g.VList[nodeIndex].SegmentList[ls] == false) continue;
                        var nextWeightedPoint = GrowOneUnit(ls, maxX + 1, maxY + 1);
                        if (nextWeightedPoint.X >= 0)
                        {
                            LineSegment l_new = new LineSegment(ls.Start, nextWeightedPoint);


                            if (!addList.ContainsKey(l_new)) addList.Add(l_new, nodeIndex);
                            if (!removeList.ContainsKey(ls)) removeList.Add(ls, nodeIndex);
                        }

                    }
                }

                foreach (var s in removeList)
                {
                    g.VList[s.Value].SegmentList.Remove(s.Key);
                }
                removeList.Clear();
                foreach (var s in addList)
                {
                    if (s.Value >= 0) g.VList[s.Value].SegmentList.Add(s.Key, true);
                    else g.VList[g.GetNode((int)s.Key.Start.X, (int)s.Key.Start.Y)].SegmentList.Add(s.Key, false);
                }
                addList.Clear();

            }
            FixMesh(g);
        }


    }

    public class OrthogonalEdge
    {
        public Vertex a;
        public Vertex b;

        public OrthogonalEdge(Vertex p, Vertex q)
        {

            
            if (p.XLoc == q.XLoc)
            {
                if (p.YLoc <= q.YLoc)
                {
                    a = p;
                    b = q;
                }
                else
                {
                    a = q;
                    b = p;
                }
            }
            if (p.YLoc == q.YLoc)
            {
                if (p.XLoc <= q.XLoc)
                {
                    a = p;
                    b = q;
                }
                else
                {
                    a = q;
                    b = p;
                }
            }
        }

    }
    internal class yCoordinateComparator : IComparer<Vertex>
    {

        public int Compare(Vertex a, Vertex b)
        {
            if (a.YLoc < b.YLoc) return 1;
            if (a.YLoc == b.YLoc) return 0;
            return -1;
        }
    }
    internal class xCoordinateComparator : IComparer<Vertex>
    {

        public int Compare(Vertex a, Vertex b)
        {
            if (a.XLoc < b.XLoc) return 1;
            if (a.XLoc == b.XLoc) return 0;
            return -1;
        }
    }
}
