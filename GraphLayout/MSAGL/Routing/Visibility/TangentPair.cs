using System;
using System.Diagnostics;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Visibility {
    /// <summary>
    /// calculates the pair of tangent line segments between two convex non-intersecting polygons H and Q
    /// we suppose that polygons are clockwise oriented
    /// </summary>
    internal class TangentPair {
        //left tangent means that the polygon lies to the left of the tangent
        //right tangent means that the polygon lies to the right of the tangent
        //the first element of a couple referse to P and the second to Q
        //the left at P and left at Q tangent
        Polygon P;
        Polygon Q;
        internal Tuple<int, int> leftPLeftQ;
        //the left at P and right at Q tangent        
        internal Tuple<int, int> leftPRightQ;
        bool lowerBranchOnQ;
        //the right at P and left at Q tangent
        internal Tuple<int, int> rightPLeftQ;
        //the right at P and right at Q tangent
        internal Tuple<int, int> rightPRightQ;


        bool upperBranchOnP;

        internal TangentPair(Polygon polygonP, Polygon polygonQ) {
            PPolygon = polygonP;
            QPolygon = polygonQ;
        }

/*
        bool debug {
            get { return false; }
        }
*/

        internal Polygon PPolygon {
//            get { return P; }
            set { P = value; }
        }

        internal Polygon QPolygon {
//            get { return Q; }
            set { Q = value; }
        }

        bool LeftFromLineOnP(int vertexIndex, Point lineStart, Point lineEnd) {
            Point p = P.Pnt(vertexIndex);
            if (upperBranchOnP)
                return Point.PointToTheLeftOfLineOrOnLine(lineEnd, p, lineStart);
            return Point.PointToTheRightOfLineOrOnLine(lineEnd, p, lineStart);
        }

        bool LeftFromLineOnQ(int vertexIndex, Point lineStart, Point lineEnd) {
            Point point = Q.Pnt(vertexIndex);
            if (lowerBranchOnQ)
                return Point.PointToTheLeftOfLineOrOnLine(lineEnd, point, lineStart);
            return Point.PointToTheRightOfLineOrOnLine(lineEnd, point, lineStart);
        }

        int PrevOnP(int i) {
            if (upperBranchOnP)
                return P.Prev(i);
            return P.Next(i);
        }

        int PrevOnQ(int i) {
            if (lowerBranchOnQ)
                return Q.Prev(i);
            return Q.Next(i);
        }

        int NextOnP(int i) {
            if (upperBranchOnP)
                return P.Next(i);
            return P.Prev(i);
        }

        int NextOnQ(int i) {
            if (lowerBranchOnQ)
                return Q.Next(i);
            return Q.Prev(i);
        }


        int MedianOnP(int i, int j) {
            if (upperBranchOnP)
                return P.Median(i, j);
            return P.Median(j, i);
        }

        int MedianOnQ(int i, int j) {
            if (lowerBranchOnQ)
                return Q.Median(i, j);
            return Q.Median(j, i);
        }


        //internal LineSegment ls(Tuple<int, int> tangent) {
        //    return new LineSegment(P.Pnt(tangent.First), Q.Pnt(tangent.Second));
        //}

        //internal LineSegment ls(int a, int b) {
        //    return new LineSegment(P.Pnt(a), Q.Pnt(b));
        //}

        int ModuleP(int p0, int p1) {
            if (upperBranchOnP)
                return P.Module(p1 - p0);
            return P.Module(p0 - p1);
        }

        int ModuleQ(int q0, int q1) {
            if (lowerBranchOnQ)
                return Q.Module(q1 - q0);
            return Q.Module(q0 - q1);
        }

        /// <summary>
        /// we pretend here that the branches go clockwise from p0 to p1, and from q0 to q1 
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="q0"></param>
        /// <param name="q1"></param>
        /// <returns></returns>
        Tuple<int, int> TangentBetweenBranches(int p0, int p1, int q0, int q1) {
            while (p1 != p0 || q1 != q0) {
                int mp = p1 != p0 ? MedianOnP(p0, p1) : p0;
                int mq = q1 != q0 ? MedianOnQ(q0, q1) : q0;
                Point mpp = P.Pnt(mp);
                Point mqp = Q.Pnt(mq);
                //SugiyamaLayoutSettings.Show(P.Polyline, ls(mp, mq), ls(p1,q0), ls(p0,q1), Q.Polyline);
                bool moveOnP = true;
                if (ModuleP(p0, p1) > 1) {
                    if (LeftFromLineOnP(NextOnP(mp), mpp, mqp)) //try to move p0 clockwise
                        p0 = mp;
                    else if (LeftFromLineOnP(PrevOnP(mp), mpp, mqp)) //try to move p1 counterclockwise
                        p1 = mp;
                    else
                        moveOnP = false;
                } else if (p1 != p0) {
//we have only two point in the branch
                    //try to move p0 clockwise
                    if (LeftFromLineOnP(p1, P.Pnt(p0), mqp))
                        p0 = p1;
                    else if (LeftFromLineOnP(p0, P.Pnt(p1), mqp)) //try to move p1 counterclockwise
                        p1 = p0;
                    else
                        moveOnP = false;
                } else
                    moveOnP = false;

                bool moveOnQ = true;
                if (ModuleQ(q0, q1) > 1) {
                    if (LeftFromLineOnQ(NextOnQ(mq), mqp, mpp)) //try to move q0 clockwise
                        q0 = mq;
                    else if (LeftFromLineOnQ(PrevOnQ(mq), mqp, mpp)) //try to move q1 counterclockwise
                        q1 = mq;
                    else
                        moveOnQ = false;
                } else if (q1 != q0) {
//we have only two points in the branch
                    if (LeftFromLineOnQ(q1, Q.Pnt(q0), mpp)) //try to move q0 clockwise
                        q0 = q1;
                    else if (LeftFromLineOnQ(q0, Q.Pnt(q1), mpp)) //try to move q1 counterclockwise                   
                        q1 = q0;
                    else
                        moveOnQ = false;
                } else
                    moveOnQ = false;

                if (!moveOnP && !moveOnQ) {
                    p1 = p0 = mp;
                    q1 = q0 = mq;
                }
            }

            return new Tuple<int, int>(p0, q1);
        }

        /// <summary>
        /// following the paper of Edelsbrunner
        /// </summary>
        /// <param name="bisectorPivot"></param>
        /// <param name="bisectorRay"></param>
        /// <param name="p1">the closest feature start</param>
        /// <param name="p2">the closest feature end</param>
        /// <param name="q1">the closest feature end</param>
        /// <param name="q2">the closest feature start</param>
        internal void FindDividingBisector(out Point bisectorPivot, out Point bisectorRay,
                                           out int p1, out int p2, out int q1, out int q2) {
            Point pClosest;
            Point qClosest;
            FindClosestFeatures(out p1, out p2, out q1, out q2, out pClosest, out qClosest);
            bisectorPivot = (pClosest + qClosest)/2;
            bisectorRay = (pClosest - qClosest).Rotate(Math.PI/2);

            // int p=P.FindTheFurthestVertexFromBisector(
#if TEST_MSAGL
            //if (!ApproximateComparer.Close(pClosest, qClosest))
            //    SugiyamaLayoutSettings.Show(this.P.Polyline, this.Q.Polyline, new LineSegment(pClosest, qClosest));
#endif
        }


        internal void FindClosestPoints(out Point pClosest, out Point qClosest) {
            int p1, p2, q1, q2;
            FindClosestFeatures(out p1, out p2, out q1, out q2, out pClosest, out qClosest);
        }

        void FindClosestFeatures(out int p1, out int p2, out int q1, out int q2, out Point pClosest, out Point qClosest) {
            P.GetTangentPoints(out p2, out p1, Q[0].Point);
            // LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(P.Polyline), new DebugCurve(Q.Polyline), new DebugCurve("red",Ls(p2, 0)), new DebugCurve("blue",Ls(p1, 0)));

            if (p2 == p1)
                p2 += P.Count;
            Q.GetTangentPoints(out q1, out q2, P[0].Point);
            //LayoutAlgorithmSettings.Show(P.Polyline, Q.Polyline, Ls(0, q1), Ls(0, q2));
            if (q2 == q1)
                q1 += Q.Count;

//            LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(100,0.1,"black",P.Polyline),new DebugCurve(100,0.1,"black",Q.Polyline),new DebugCurve(100,0.1,"red",new LineSegment(P [p1].Point,Q [q1].Point)),new DebugCurve(100,0.1,"blue",new LineSegment(P [p2].Point,Q [q2].Point)));

            FindClosestPoints(ref p1, ref p2, ref q2, ref q1, out pClosest, out qClosest);
        }

        
/*
        ICurve Ls(int p0, int p1) {
            return new LineSegment(P[p0].Point, Q[p1].Point);
        }
*/
        


        //chunks go clockwise from p1 to p2 and from q2 to q1
        void FindClosestPoints(ref int p1, ref int p2, ref int q2, ref int q1, out Point pClosest, out Point qClosest) {
            while (ChunksAreLong(p2, p1, q2, q1))
                ShrinkChunks(ref p2, ref p1, ref q2, ref q1);


            if (p1 == p2) {
                pClosest = P[p2].Point;
                if (q1 == q2)
                    qClosest = Q[q1].Point;
                else {
//                    if(debug) LayoutAlgorithmSettings.Show(new LineSegment(P.Pnt(p2), Q.Pnt(q2)), new LineSegment(P.Pnt(p1), Q.Pnt(q1)), P.Polyline, Q.Polyline);
                    qClosest = Point.ClosestPointAtLineSegment(pClosest, Q[q1].Point, Q[q2].Point);
                    if (ApproximateComparer.Close(qClosest, Q.Pnt(q1)))
                        q2 = q1;
                    else if (ApproximateComparer.Close(qClosest, Q.Pnt(q2)))
                        q1 = q2;
                }
            } else {
                Debug.Assert(q1 == q2);
                qClosest = Q[q1].Point;
                pClosest = Point.ClosestPointAtLineSegment(qClosest, P[p1].Point, P[p2].Point);
                if (ApproximateComparer.Close(pClosest, P.Pnt(p1)))
                    p2 = p1;
                else if (ApproximateComparer.Close(qClosest, P.Pnt(p2)))
                    p1 = p2;
            }
        }

        bool ChunksAreLong(int p2, int p1, int q2, int q1) {
            int pLength = P.Module(p2 - p1) + 1;
            if (pLength > 2)
                return true;

            int qLength = Q.Module(q1 - q2) + 1;
            if (qLength > 2)
                return true;

            if (pLength == 2 && qLength == 2)
                return true;

            return false;
        }


        void ShrinkChunks(ref int p2, ref int p1, ref int q2, ref int q1) {
            int mp = p1 == p2 ? p1 : P.Median(p1, p2);
            int mq = q1 == q2 ? q1 : Q.Median(q2, q1);
            Point mP = P[mp].Point;
            Point mQ = Q[mq].Point;


            double a1;
            double a2;
            double b1;
            double b2;


            GetAnglesAtTheMedian(mp, mq, ref mP, ref mQ, out a1, out a2, out b1, out b2);
//            Core.Layout.LayoutAlgorithmSettings.Show(new LineSegment(P.Pnt(p2), Q.Pnt(q2)), new LineSegment(P.Pnt(p1), Q.Pnt(q1)), new LineSegment(P.Pnt(mp),Q.Pnt( mq)), P.Polyline, Q.Polyline);
            //if (MovingAlongHiddenSide(ref p1, ref p2, ref q1, ref q2, mp, mq, a1, a2, b1, b2)) {
            //  //  SugiyamaLayoutSettings.Show(ls(p2, q2), ls(p1, q1), ls(mp, mq), P.Polyline, Q.Polyline);
            //    return;
            //}

            if (InternalCut(ref p1, ref p2, ref q1, ref q2, mp, mq, a1, a2, b1, b2)) {
//               if(debug) LayoutAlgorithmSettings.Show(P.Polyline, Q.Polyline, Ls(p1, q1), Ls(p2,q2));
                return;
            }

            //case 1
            if (OneOfChunksContainsOnlyOneVertex(ref p2, ref p1, ref q2, ref q1, mp, mq, a1, b1))
                return;
            //case 2
            if (OnlyOneChunkContainsExactlyTwoVertices(ref p2, ref p1, ref q2, ref q1, mp, mq, a1, b1, a2, b2))
                return;

            // the case where we have exactly two vertices in each chunk
            if (p2 == P.Next(p1) && q1 == Q.Next(q2)) {
                double psol, qsol;

                LineSegment.MinDistBetweenLineSegments(P.Pnt(p1), P.Pnt(p2), Q.Pnt(q1), Q.Pnt(q2), out psol,
                                                             out qsol);
                //System.Diagnostics.Debug.Assert(res);
                if (psol == 0)
                    p2 = p1;
                else if (psol == 1)
                    p1 = p2;
                else if (qsol == 0)
                    q2 = q1;
                else if (qsol == 1)
                    q1 = q2;

                Debug.Assert(p1 == p2 || q1 == q2);
                return;
                ////we have trapeze {p1,p2,q2,q1} here
                ////let p1,p2 be the low base of the trapes
                ////where is the closest vertex , on the left side or on the rigth side?
                //if (Point.Angle(P.Pnt(p2), P.Pnt(p1), Q.Pnt(q1)) + Point.Angle(P.Pnt(p1), Q.Pnt(q1), Q.Pnt(q2)) >= Math.PI)
                //    ProcessLeftSideOfTrapez(ref p1, ref p2, ref q2, ref q1);
                //else {
                //    SwapPQ();
                //    ProcessLeftSideOfTrapez(ref q2, ref q1, ref p1, ref p2);
                //    SwapPQ();
                //}
                //return;
            }


            //case 3
            if (a1 <= Math.PI && a2 <= Math.PI && b1 <= Math.PI && b2 <= Math.PI) {
                if (a1 + b1 > Math.PI) {
                    if (a1 >= Math.PI/2)
                        p1 = mp;
                    else
                        q1 = mq;
                } else {
                    Debug.Assert(a2 + b2 >= Math.PI-ApproximateComparer.Tolerance);
                    if (a2 >= Math.PI/2)
                        p2 = mp;
                    else
                        q2 = mq;
                }
            } else {
                if (a1 > Math.PI)
                    p1 = mp;
                else if (a2 > Math.PI)
                    p2 = mp;
                else if (b1 > Math.PI)
                    q1 = mq;
                else {
                    Debug.Assert(b2 > Math.PI);
                    q2 = mq;
                }
            }
        }

        bool InternalCut(ref int p1, ref int p2, ref int q1, ref int q2, int mp, int mq, double a1, double a2, double b1,
                         double b2) {
            bool ret = false;
            if (a1 >= Math.PI && a2 >= Math.PI) {
                //Find out who is on the same side from [mq,mp] as Q[0], the next or the prev. Remember that we found the first chunk from Q[0]

                //System.Diagnostics.Debug.WriteLine("cutting P");
//                if(debug) LayoutAlgorithmSettings.Show(P.Polyline, Q.Polyline, Ls(p1, q1), Ls(p2, q2), Ls(mp, mq));
                Point mpp = P[mp].Point;
                Point mqp = Q[mq].Point;
                Point mpnp = P[P.Next(mp)].Point;
                TriangleOrientation orientation = Point.GetTriangleOrientation(mpp, mqp, Q[0].Point);
                TriangleOrientation nextOrientation = Point.GetTriangleOrientation(mpp, mqp, mpnp);

                if (orientation == nextOrientation)
                    p1 = P.Next(mp);
                else
                    p2 = P.Prev(mp);
                ret = true;
            }
            if (b1 >= Math.PI && b2 >= Math.PI) {
                //Find out who is on the same side from [mq,mp] as P[0], the next or the prev. Remember that we found the first chunk from P[0]
                //System.Diagnostics.Debug.WriteLine("cutting Q");
//                if (debug) LayoutAlgorithmSettings.Show(P.Polyline, Q.Polyline, Ls(p1, q1), Ls(p2, q2), Ls(mp, mq));
                Point mpp = P[mp].Point;
                Point mqp = Q[mq].Point;
                Point mqnp = Q[Q.Next(mq)].Point;
                TriangleOrientation orientation = Point.GetTriangleOrientation(mpp, mqp, P[0].Point);
                TriangleOrientation nextOrientation = Point.GetTriangleOrientation(mpp, mqp, mqnp);
                if (orientation == nextOrientation)
                    q2 = Q.Next(mq);
                else
                    q1 = Q.Prev(mq);
                ret = true;
            }
            return ret;
        }


        //private void ProcessLeftSideOfTrapez(ref int p1, ref int p2, ref int q2, ref int q1) {
        //    //the closest vertex is on the left side
        //    Point pn1 = P.Pnt(p1); Point pn2 = P.Pnt(p2);
        //    Point qn1 = Q.Pnt(q1); Point qn2 = Q.Pnt(q2);

        //   //SugiyamaLayoutSettings.Show(new LineSegment(pn1, pn2), new LineSegment(pn2, qn2), new LineSegment(qn2, qn1), new LineSegment(qn1, pn1));
        //    double ap1 = Point.Angle(pn2, pn1, qn1);
        //    double aq1 = Point.Angle(pn1, qn1, qn2);
        //    System.Diagnostics.Debug.Assert(ap1 + aq1 >= Math.PI);
        //    //the point is on the left side
        //    if (ap1 >= Math.PI / 2 && aq1 >= Math.PI / 2) {
        //        q2 = q1; //the vertices of the left side gives the solution
        //        p2 = p1;
        //    } else if (ap1 < Math.PI / 2) {
        //        q2 = q1;
        //        if (!Point.CanProject(qn1, pn1, pn2))
        //            p1 = p2;
        //    } else { //aq1<Pi/2
        //        p2 = p1;
        //        if (!Point.CanProject(pn1, qn1, qn2))
        //            q1 = q2;
        //    }
        //}


        void GetAnglesAtTheMedian(int mp, int mq, ref Point mP, ref Point mQ, out double a1, out double a2,
                                  out double b1, out double b2) {
            a1 = Point.Angle(mQ, mP, P.Pnt(P.Prev(mp)));
            a2 = Point.Angle(P.Pnt(P.Next(mp)), mP, mQ);
            b1 = Point.Angle(Q.Pnt(Q.Next(mq)), mQ, mP);
            b2 = Point.Angle(mP, mQ, Q.Pnt(Q.Prev(mq)));
        }

        /// <summary>
        /// we know here that p1!=p2 and q1!=q2
        /// </summary>
        /// <param name="p2"></param>
        /// <param name="p1"></param>
        /// <param name="q2"></param>
        /// <param name="q1"></param>
        /// <param name="mp"></param>
        /// <param name="mq"></param>
        /// <param name="a1"></param>
        /// <param name="b1"></param>
        /// <param name="a2"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        bool OnlyOneChunkContainsExactlyTwoVertices(ref int p2, ref int p1, ref int q2, ref int q1,
                                                    int mp, int mq, double a1, double b1, double a2, double b2) {
            bool pSideIsShort = p2 == P.Next(p1);
            bool qSideIsShort = q1 == Q.Next(q2);
            if (pSideIsShort && !qSideIsShort) {
                ProcessShortSide(ref p2, ref p1, ref q2, ref q1, mp, mq, a1, b1, a2, b2);
                return true;
            }

            if (qSideIsShort && !pSideIsShort) {
                SwapEverything(ref p2, ref p1, ref q2, ref q1, ref mp, ref mq, ref a1, ref b1, ref a2, ref b2);
                ProcessShortSide(ref p2, ref p1, ref q2, ref q1, mp, mq, a1, b1, a2, b2);
                SwapEverything(ref p2, ref p1, ref q2, ref q1, ref mp, ref mq, ref a1, ref b1, ref a2, ref b2);
                return true;
            }

            return false;
        }

        void SwapEverything(ref int p2, ref int p1, ref int q2, ref int q1, ref int mp, ref int mq, ref double a1,
                            ref double b1, ref double a2, ref double b2) {
            SwapPq();
            Swap(ref q1, ref p2);
            Swap(ref q2, ref p1);
            Swap(ref mp, ref mq);
            Swap(ref b1, ref a2);
            Swap(ref a1, ref b2);
        }

        static void Swap(ref double a1, ref double a2) {
            double t = a1;
            a1 = a2;
            a2 = t;
        }

        static void Swap(ref int a1, ref int a2) {
            int t = a1;
            a1 = a2;
            a2 = t;
        }

        void ProcessShortSide(ref int p2, ref int p1, ref int q2, ref int q1, int mp, int mq, double a1, double b1,
                              double a2, double b2) {
            //case 2.1
            if (mp == p2)
                ProcessSide(ref p2, ref p1, ref q2, ref q1, mq, a1, b1, b2);
            else {
                if (a2 <= Math.PI) {
                    if (a2 + b2 >= Math.PI) {
                        if (a2 >= Math.PI / 2) p2 = p1;
                        else q2 = mq;
                    } else {
                        if (b1 >= Math.PI / 2) q1 = mq;
                        else if (a2 < b2) {
                            //SugiyamaLayoutSettings.Show(new LineSegment(P.Pnt(p2), Q.Pnt(q2)), new LineSegment(P.Pnt(p1), Q.Pnt(q1)), new LineSegment(P.Pnt(p1), Q.Pnt(mq)), P.Polyline, Q.Polyline);
                            if (Point.CanProject(Q.Pnt(mq), P[p1].Point, P[p2].Point)) q1 = mq;
                            else p1 = p2;
                        }
                    }
                } else {
                    //a2>Pi , case 2.2                    
                    if (a1 + b1 <= Math.PI)
                        p1 = p2;
                    else
                        p2 = p1;
                }
            }
        }

        void SwapPq() {
            Polygon t = P;
            P = Q;
            Q = t;
        }

        void ProcessSide(ref int p2, ref int p1, ref int q2, ref int q1, int mq, double a1, double b1, double b2) {
            //SugiyamaLayoutSettings.Show(new LineSegment(P.Pnt(p2), Q.Pnt(q2)), new LineSegment(P.Pnt(p1), Q.Pnt(q1)),new LineSegment(P.Pnt(p1), Q.Pnt(mq)), P.Polyline, Q.Polyline);
            Point mQ = Q.Pnt(mq);
            if (a1 <= Math.PI) {
                if (a1 + b1 >= Math.PI) {
                    if (a1 >= Math.PI/2) p1 = p2;
                    else q1 = mq;
                } else if (b2 >= Math.PI/2) q2 = mq;
                else if (a1 < b2) {
                    if (Point.CanProject(mQ, P[p1].Point, P[p2].Point)) q2 = mq;
                    else p2 = p1;
                }
            } else {
                //a1>Pi , case 2.2
                p2 = p1;
                if (b1 >= Math.PI)
                    q1 = mq;
                else if (b2 >= Math.PI)
                    q2 = mq;
            }
        }


        static bool OneOfChunksContainsOnlyOneVertex(ref int p2, ref int p1, ref int q2, ref int q1, int mp, int mq,
                                                     double a1, double b1) {
            if (p1 == p2) {
                if (b1 >= Math.PI/2)
                    q1 = mq;
                else
                    q2 = mq;

                return true;
            }
            if (q1 == q2) {
                if (a1 >= Math.PI/2)
                    p1 = mp;
                else
                    p2 = mp;
                return true;
            }
            return false;
        }

        internal void CalculateLeftTangents() {
            Point bisectorPivot;
            Point bisectorRay;
            int p1;
            int p2;
            int q1;
            int q2;


            FindDividingBisector(out bisectorPivot, out bisectorRay,
                                 out p1, out p2, out q1, out q2);
            int pFurthest = P.FindTheFurthestVertexFromBisector(p1, p2, bisectorPivot, bisectorRay);
            int qFurthest = Q.FindTheFurthestVertexFromBisector(q2, q1, bisectorPivot, bisectorRay);


            upperBranchOnP = false;
            lowerBranchOnQ = true;
            leftPLeftQ = TangentBetweenBranches(pFurthest, p1, qFurthest, q1); //we need to take maximally wide branches
            lowerBranchOnQ = false;
            leftPRightQ = TangentBetweenBranches(pFurthest, p1, qFurthest, q2);
        }

        //private bool QContains(int x ,int y) {
        //    foreach (Point p in Q.Polyline) {
        //        if (p.X == x && p.Y == y)
        //            return true;
        //    }
        //    return false;
        //}

        //bool PContains(int x, int y) {
        //    foreach (Point p in P.Polyline) {
        //        if (p.X == x && p.Y == y)
        //            return true;
        //    }
        //    return false;
        //}

        internal void CalculateRightTangents() {
            Point bisectorPivot;
            Point bisectorRay;
            int p1;
            int p2;
            int q1;
            int q2;

            FindDividingBisector(out bisectorPivot, out bisectorRay, out p1, out p2, out q1, out q2);

            int pFurthest = P.FindTheFurthestVertexFromBisector(p1, p2, bisectorPivot, bisectorRay);
            int qFurthest = Q.FindTheFurthestVertexFromBisector(q2, q1, bisectorPivot, bisectorRay);
            //SugiyamaLayoutSettings.Show(ls(p1, q1), ls(p2, q2), ls(pFurthest, qFurthest), P.Polyline, Q.Polyline);

            upperBranchOnP = true;
            lowerBranchOnQ = true;
            rightPLeftQ = TangentBetweenBranches(pFurthest, p2, qFurthest, q1);
            lowerBranchOnQ = false;
            rightPRightQ = TangentBetweenBranches(pFurthest, p2, qFurthest, q2);
        }
    }
}