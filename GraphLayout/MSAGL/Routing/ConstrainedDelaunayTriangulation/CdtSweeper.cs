using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;

namespace Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation {
  /// <summary>
  /// this class builds the triangulation by a sweep with a horizontal line
  /// </summary>
    public class CdtSweeper:AlgorithmBase {
        RbTree<CdtFrontElement> front = new RbTree<CdtFrontElement>((a, b) => a.X.CompareTo(b.X));
        readonly Set<CdtTriangle> triangles=new Set<CdtTriangle>();
        List<CdtSite> listOfSites;
        CdtSite p_2;
        readonly Func<CdtSite, CdtSite, CdtEdge> createEdgeDelegate;
        CdtSite p_1;

        internal CdtSweeper(List<CdtSite> listOfSites, CdtSite p_1, CdtSite p_2, 
            Func<CdtSite, CdtSite, CdtEdge> createEdgeDelegate) {
            this.listOfSites=listOfSites;
            if (listOfSites.Count == 0)
                return;
            var firstTriangle=new CdtTriangle(p_1, p_2, listOfSites[0], createEdgeDelegate);
            Triangles.Insert( firstTriangle );
            front.Insert(new CdtFrontElement(p_1, firstTriangle.Edges[2] ));
            front.Insert(new CdtFrontElement(listOfSites[0], firstTriangle.Edges[1]));
            this.p_1 = p_1;
            this.p_2 = p_2;
            this.createEdgeDelegate = createEdgeDelegate;
         //   ShowFront();
        }

        internal Set<CdtTriangle> Triangles {
            get { return triangles; }
        }

        /// <summary>
        /// the method making the main work
        /// </summary>
        protected override void RunInternal() {
            if (listOfSites.Count == 0) return;
            for(int i=1;i<listOfSites.Count;i++)
                ProcessSite(listOfSites[i]);

            FinalizeTriangulation();
#if TEST_MSAGL&& TEST_MSAGL
            //TestTriangles();
            //ShowFront(triangles,null,null,null);
#endif
        }

        void FinalizeTriangulation() {
            RemoveP1AndP2Triangles(); 
            var list =CreateDoubleLinkedListOfPerimeter();
            MakePerimeterConvex(list);
            
        }

        void MakePerimeterConvex(PerimeterEdge firstPerimeterEdge) {
            firstPerimeterEdge = FindPivot(firstPerimeterEdge);
            var firstSite = firstPerimeterEdge.Start;
            var a=firstPerimeterEdge;
            PerimeterEdge b;
            do { 
               b=a.Next;
               if (Point.GetTriangleOrientation(a.Start.Point, a.End.Point, b.End.Point) == TriangleOrientation.Counterclockwise) {
                   a = ShortcutTwoListElements(a);
                   while (a.Start != firstSite) {
                       var c=a.Prev;
                       if (Point.GetTriangleOrientation(c.Start.Point, c.End.Point, a.End.Point) == TriangleOrientation.Counterclockwise) {
                           a = ShortcutTwoListElements(c);
                       } else break;
                   }
               } else
                   a = b;
            } while (a.End != firstSite); 
        }

        static PerimeterEdge FindPivot(PerimeterEdge firstPerimeterEdge) {
            var pivot = firstPerimeterEdge;
            var e=firstPerimeterEdge;
            do { 
                e=e.Next;
                if (e.Start.Point.X < pivot.Start.Point.X ||
                    e.Start.Point.X == pivot.Start.Point.X && e.Start.Point.Y < pivot.Start.Point.Y)
                    pivot = e;
            } while (e != firstPerimeterEdge);
            return pivot;
        }

        PerimeterEdge CreateDoubleLinkedListOfPerimeter() {

            CdtEdge firstEdge = this.triangles.SelectMany(t => t.Edges).FirstOrDefault(e => e.CwTriangle == null || e.CcwTriangle == null);
            var edge = firstEdge;
            PerimeterEdge pe, prevPe = null, listStart = null;

            do {
                pe = CreatePerimeterElementFromEdge(edge);
                edge = FindNextEdgeOnPerimeter(edge);
                if (prevPe != null) {
                    pe.Prev = prevPe;
                    prevPe.Next = pe;
                } else
                    listStart = pe;

                prevPe = pe;

            } while (edge != firstEdge);
            listStart.Prev = pe;
            pe.Next = listStart;
            return listStart;
        }

        static CdtEdge FindNextEdgeOnPerimeter(CdtEdge e) {
            var t = e.CwTriangle ?? e.CcwTriangle;
            e = t.Edges[t.Edges.Index(e) + 2];
            while(e.CwTriangle != null && e.CcwTriangle != null) {
                t = e.GetOtherTriangle(t);
                e = t.Edges[t.Edges.Index(e) + 2];
            }
            return e;
        }

        static PerimeterEdge CreatePerimeterElementFromEdge(CdtEdge edge) {
            var pe = new PerimeterEdge(edge);
            if (edge.CwTriangle != null) {
                pe.Start=edge.upperSite;
                pe.End = edge.lowerSite;
            } else {
                pe.End = edge.upperSite;
                pe.Start = edge.lowerSite;
            }
            return pe;
        }

        void RemoveP1AndP2Triangles() {
            var trianglesToRemove = new Set<CdtTriangle>();
            foreach (var t in triangles)
                if (t.Sites.Contains(p_1) || t.Sites.Contains(p_2))
                    trianglesToRemove.Insert(t);
            foreach (var t in trianglesToRemove)
                RemoveTriangleWithEdges(triangles, t);
        }

       internal static void RemoveTriangleWithEdges(Set<CdtTriangle> cdtTriangles, CdtTriangle t) {
            cdtTriangles.Remove(t);
            foreach (var e in t.Edges) {
                if (e.CwTriangle == t)
                    e.CwTriangle = null;
                else e.CcwTriangle = null;
                if (e.CwTriangle == null && e.CcwTriangle == null)
                    e.upperSite.Edges.Remove(e);
            }
        }
       
        internal static void RemoveTriangleButLeaveEdges(Set<CdtTriangle> cdtTriangles, CdtTriangle t) {
           cdtTriangles.Remove(t);
           foreach (var e in t.Edges) 
               if (e.CwTriangle == t)
                   e.CwTriangle = null;
               else 
                   e.CcwTriangle = null;                          
       }

        void ProcessSite(CdtSite site) {
            PointEvent(site);
            for (int i = 0; i < site.Edges.Count; i++) {
                var edge = site.Edges[i];
                if (edge.Constrained)
                    EdgeEvent(edge);
            }
            //    ShowFrontWithSite(site);
            // TestThatFrontIsConnected();
        }
#if TEST_MSAGL
        void TestThatFrontIsConnected() {
          CdtFrontElement p = null;
          foreach (var cdtFrontElement in front) {
              if(p!=null) 
                  Debug.Assert(p.RightSite==cdtFrontElement.LeftSite);
              p = cdtFrontElement;
          }
      }
#endif
      void EdgeEvent(CdtEdge edge) {
            Debug.Assert(edge.Constrained);
            if(EdgeIsProcessed(edge))
                return;
            var edgeInserter = new EdgeInserter(edge, Triangles, front, createEdgeDelegate);
            edgeInserter.Run();
            
        }

        static bool EdgeIsProcessed(CdtEdge edge) {
            return edge.CwTriangle != null || edge.CcwTriangle != null;
        }

#if TEST_MSAGL
        void ShowFrontWithSite(CdtSite site, params ICurve[] redCurves) {
            var ls = new List<DebugCurve>();


            if (site.Edges != null)
                foreach (var e in site.Edges)
                    ls.Add(new DebugCurve(100, 0.001, e.Constrained?"pink":"brown", new LineSegment(e.upperSite.Point, e.lowerSite.Point)));

            ls.Add(new DebugCurve(100,0.01,"brown", new Ellipse(0.5,0.5,site.Point)));

            foreach (var t in Triangles) {
                for (int i = 0; i < 3; i++) {
                    var e = t.Edges[i];
                    ls.Add(new DebugCurve(e.Constrained?(byte)150:(byte)50, e.Constrained?0.002:0.001, e.Constrained? "pink":"navy", new LineSegment(e.upperSite.Point, e.lowerSite.Point)));
                }
            }

            foreach (var c in redCurves)
                ls.Add(new DebugCurve(100, 0.005, "red", c));


            foreach (var frontElement in front)
                ls.Add(new DebugCurve(100, 0.005, "green",
                                      new LineSegment(frontElement.Edge.upperSite.Point,
                                                      frontElement.Edge.lowerSite.Point)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(ls);
        }
        void ShowFront() {
            ShowFront(Triangles, front, null, null);
        }

        internal static void ShowFront(IEnumerable<CdtTriangle> cdtTriangles, RbTree<CdtFrontElement> cdtFrontElements, IEnumerable<ICurve> redCurves, IEnumerable<ICurve> blueCurves) {
            List<DebugCurve> ls = new List<DebugCurve>();
            if (redCurves != null)
                foreach (var c in redCurves)
                    ls.Add(new DebugCurve(100, 0.5, "red", c));
            if (blueCurves != null)
                foreach (var c in blueCurves)
                    ls.Add(new DebugCurve(100, 2, "blue", c));

            if (cdtFrontElements != null)
                foreach (var frontElement in cdtFrontElements)
                    ls.Add(new DebugCurve(100, 0.001, "green",
                                          new LineSegment(frontElement.Edge.upperSite.Point,
                                                          frontElement.Edge.lowerSite.Point)));
            if (cdtTriangles !=null)
            foreach (var t in cdtTriangles) {
                for (int i = 0; i < 3; i++) {
                    var e = t.Edges[i];
                    ls.Add(GetDebugCurveOfCdtEdge(e));
                }
            }
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(ls);
        }

      static DebugCurve GetDebugCurveOfCdtEdge(CdtEdge e) {
          if(e.CcwTriangle==null || e.CwTriangle==null)
              return new DebugCurve(100, 0.5, e.Constrained ? "pink" : "rose", new LineSegment(e.upperSite.Point, e.lowerSite.Point));
          return new DebugCurve(100, e.Constrained?0.002:0.001, e.Constrained?"violet":"yellow", new LineSegment(e.upperSite.Point, e.lowerSite.Point));
      }
#endif
//        int count;
//         bool db { get { return count == 147; } }
        void PointEvent(CdtSite pi) {
            RBNode<CdtFrontElement> hittedFrontElementNode;

            ProjectToFront(pi, out hittedFrontElementNode);
            CdtSite rightSite;
            CdtSite leftSite = hittedFrontElementNode.Item.X + ApproximateComparer.DistanceEpsilon < pi.Point.X
                                   ? MiddleCase(pi, hittedFrontElementNode, out rightSite)
                                   : LeftCase(pi, hittedFrontElementNode, out rightSite);
            var piNode = InsertSiteIntoFront(leftSite, pi, rightSite);
            TriangulateEmptySpaceToTheRight(piNode);
            piNode = FindNodeInFrontBySite(front, leftSite);
            TriangulateEmptySpaceToTheLeft(piNode);
        }

#if TEST_MSAGL
        void TestTriangles() {
            var usedSites = new Set<CdtSite>();
            foreach(var t in Triangles)
                usedSites.InsertRange(t.Sites);
            foreach (var triangle in Triangles) {
                TestTriangle(triangle, usedSites);
            }
        }

        void TestTriangle(CdtTriangle triangle, Set<CdtSite> usedSites) {
            var tsites=triangle.Sites;
            foreach (var site in usedSites) {
                if (!tsites.Contains(site)) {
                    if (!SeparatedByConstrainedEdge(triangle, site) && InCircle(site, tsites[0], tsites[1], tsites[2])) {
                        List<ICurve> redCurves=new List<ICurve>();
                        redCurves.Add(new Ellipse(2, 2, site.Point));
                        List<ICurve> blueCurves = new List<ICurve>();
                        blueCurves.Add(Circumcircle(tsites[0].Point,tsites[1].Point,tsites[2].Point));
                        ShowFront(Triangles, front, redCurves, blueCurves);
                    }
                }
            }
        }

        static bool SeparatedByConstrainedEdge(CdtTriangle triangle, CdtSite site) {
            for (int i = 0; i < 3; i++)
                if (SeparatedByEdge(triangle, i, site))
                    return true;
            return false;
        }

        static bool SeparatedByEdge(CdtTriangle triangle, int i, CdtSite site) {
            var e = triangle.Edges[i];
            var s = triangle.Sites[i+2];
            var a0 = ApproximateComparer.Sign(Point.SignedDoubledTriangleArea(s.Point, e.upperSite.Point, e.lowerSite.Point));
            var a1 = ApproximateComparer.Sign(Point.SignedDoubledTriangleArea(site.Point, e.upperSite.Point, e.lowerSite.Point));
            return a0 * a1 <= 0;
        }
#endif
        CdtSite LeftCase(CdtSite pi, RBNode<CdtFrontElement> hittedFrontElementNode, out CdtSite rightSite) {
            //left case
            //                if(db)ShowFrontWithSite(pi, new LineSegment(pi.Point, hittedFrontElementNode.Item.Edge.upperSite.Point), new LineSegment(pi.Point, hittedFrontElementNode.Item.Edge.lowerSite.Point));
            Debug.Assert(ApproximateComparer.Close(pi.Point.X, hittedFrontElementNode.Item.X));
            var hittedFrontElement = hittedFrontElementNode.Item;
            InsertAndLegalizeTriangle(pi, hittedFrontElement);
            var prevToHitted = front.Previous(hittedFrontElementNode);
            var leftSite = prevToHitted.Item.LeftSite;
            rightSite = hittedFrontElementNode.Item.RightSite;
            //                if(db)ShowFrontWithSite(pi, new LineSegment(pi.Point, leftSite.Point), new LineSegment(pi.Point, prevToHitted.Item.RightSite.Point));
            InsertAndLegalizeTriangle(pi, prevToHitted.Item);
            front.DeleteNodeInternal(prevToHitted);
            var d = front.Remove(hittedFrontElement);
            Debug.Assert(d != null);
            return leftSite;
        }

        CdtSite MiddleCase(CdtSite pi, RBNode<CdtFrontElement> hittedFrontElementNode, out CdtSite rightSite) {
//            if(db)
//                ShowFrontWithSite(pi, new LineSegment(pi.Point, hittedFrontElementNode.Item.Edge.upperSite.Point), new LineSegment(pi.Point, hittedFrontElementNode.Item.Edge.lowerSite.Point));
            var leftSite = hittedFrontElementNode.Item.LeftSite;
            rightSite = hittedFrontElementNode.Item.RightSite;
            InsertAndLegalizeTriangle(pi, hittedFrontElementNode.Item);
            front.DeleteNodeInternal(hittedFrontElementNode);
            return leftSite;
        }

        void TriangulateEmptySpaceToTheLeft(RBNode<CdtFrontElement> leftLegNode) {
            var peakSite = leftLegNode.Item.RightSite;
            var previousNode = front.Previous(leftLegNode);

            while (previousNode != null) {
                var prevElement = previousNode.Item;
                var rp = prevElement.LeftSite;
                var r=prevElement.RightSite;
                if ((r.Point - peakSite.Point) * (rp.Point - r.Point) < 0) {
                    //see figures 9(a) and 9(b) of the paper
                    leftLegNode=ShortcutTwoFrontElements(previousNode, leftLegNode);
                    previousNode = front.Previous(leftLegNode);
                } else {
                    TryTriangulateBasinToTheLeft(leftLegNode);
                    break;
                }

            }
        }
       
        PerimeterEdge ShortcutTwoListElements(PerimeterEdge a) {
            var b = a.Next;
            Debug.Assert(a.End == b.Start);
            var t = new CdtTriangle(a.Start, a.End, b.End, a.Edge, b.Edge,
                                            createEdgeDelegate);
            Triangles.Insert(t);
            var newEdge = t.Edges[2];
            Debug.Assert(newEdge.IsAdjacent(a.Start) && newEdge.IsAdjacent(b.End));
            LegalizeEdge(a.Start, t.OppositeEdge(a.Start));
            t = newEdge.CcwTriangle ?? newEdge.CwTriangle;
            LegalizeEdge(b.End, t.OppositeEdge(b.End));
            var c = new PerimeterEdge(newEdge) { Start = a.Start, End = b.End };
            a.Prev.Next = c;
            c.Prev = a.Prev;
            c.Next = b.Next;
            b.Next.Prev = c;
            return c;
        }
        /// <summary>
        /// aNode is to the left of bNode, and they are consecutive
        /// </summary>
        /// <param name="aNode"></param>
        /// <param name="bNode"></param>
        RBNode<CdtFrontElement> ShortcutTwoFrontElements(RBNode<CdtFrontElement> aNode, RBNode<CdtFrontElement> bNode) {
            var aElem = aNode.Item;
            var bElem = bNode.Item;
            Debug.Assert(aElem.RightSite == bElem.LeftSite);
            CdtTriangle t = new CdtTriangle(aElem.LeftSite, aElem.RightSite, bElem.RightSite, aElem.Edge, bElem.Edge,
                                            createEdgeDelegate);
            Triangles.Insert(t);
            front.DeleteNodeInternal(aNode); 
            //now bNode might b not valid anymore
            front.Remove(bElem);
            var newEdge = t.Edges[2];
            Debug.Assert(newEdge.IsAdjacent( aElem.LeftSite) && newEdge.IsAdjacent(bElem.RightSite));
            LegalizeEdge(aElem.LeftSite, t.OppositeEdge(aElem.LeftSite));
            t=newEdge.CcwTriangle ?? newEdge.CwTriangle;
            LegalizeEdge(bElem.RightSite, t.OppositeEdge(bElem.RightSite));
            return front.Insert(new CdtFrontElement(aElem.LeftSite, newEdge));          
        }


        void TryTriangulateBasinToTheLeft(RBNode<CdtFrontElement> leftLegNode) {
            if (!DropsSharpEnoughToTheLeft(leftLegNode.Item))
                return;
            //ShowFrontWithSite(leftLegNode.Item.LeftSite);
            var stack = new Stack<CdtSite>();
            stack.Push(leftLegNode.Item.LeftSite);
            while (true) {
                var site = stack.Pop();
                leftLegNode = FindNodeInFrontBySite(front, site);
                var prev = front.Previous(leftLegNode);
                if (prev == null)
                    return;
                if (Point.GetTriangleOrientation(prev.Item.LeftSite.Point, leftLegNode.Item.LeftSite.Point, leftLegNode.Item.RightSite.Point) ==
                    TriangleOrientation.Counterclockwise) {
                    stack.Push(prev.Item.LeftSite);
                    ShortcutTwoFrontElements(prev, leftLegNode);
              //      ShowFrontWithSite(site);
                } else {
                    if (leftLegNode.Item.LeftSite.Point.Y > leftLegNode.Item.RightSite.Point.Y) {
                        stack.Push(prev.Item.LeftSite);
                    } else {
                        if (prev.Item.LeftSite.Point.Y <= prev.Item.RightSite.Point.Y)
                            return;
                        stack.Push(prev.Item.LeftSite);
                    }
                }
            }
        }

        static bool DropsSharpEnoughToTheLeft(CdtFrontElement frontElement) {
            var edge = frontElement.Edge;
            if (frontElement.RightSite != edge.upperSite)
                return false;
            var d = edge.lowerSite.Point - edge.upperSite.Point;
            Debug.Assert(d.X < 0 && d.Y <= 0);
            return d.X >= 0.5 * d.Y;
        }

        RBNode<CdtFrontElement> InsertSiteIntoFront(CdtSite leftSite, CdtSite pi, CdtSite rightSite) {
            CdtEdge leftEdge = null, rightEdge = null;
            foreach (var edge in pi.Edges) {
                if (leftEdge==null && edge.lowerSite == leftSite)
                    leftEdge = edge;
                if ( rightEdge==null && edge.lowerSite == rightSite)
                    rightEdge = edge;
                if (leftEdge != null && rightEdge != null) break;
            }
            Debug.Assert(leftEdge!=null && rightEdge!=null);
            front.Insert(new CdtFrontElement(leftSite, leftEdge));
            return front.Insert(new CdtFrontElement(pi, rightEdge));
        }


        void TriangulateEmptySpaceToTheRight(RBNode<CdtFrontElement> piNode) {
            var piSite=piNode.Item.LeftSite;
            var piPoint=piSite.Point;           
            var piNext = front.Next(piNode);
            while (piNext != null) {
                var frontElem=piNext.Item;
                var r=frontElem.LeftSite;
                var rp = frontElem.RightSite;
                if ((r.Point - piPoint) * (rp.Point - r.Point) < 0) {
//see figures 9(a) and 9(b) of the paper
                    piNode = ShortcutTwoFrontElements(piNode, piNext);
                    piNext = front.Next(piNode);
                } else {
                    TryTriangulateBasinToTheRight(piNode);
                    break;
                }
            }            
        }

        void TryTriangulateBasinToTheRight(RBNode<CdtFrontElement> piNode) {           
            if (!DropsSharpEnoughToTheRight(piNode.Item))
                return;
           // ShowFrontWithSite(piNode.Item.LeftSite);
            var stack = new Stack<CdtSite>();
            stack.Push(piNode.Item.LeftSite);
            while(true) {
                var site = stack.Pop();
                piNode = FindNodeInFrontBySite(front, site);      
                var next = front.Next(piNode);
                if (next == null)
                    return;
                if (Point.GetTriangleOrientation(piNode.Item.LeftSite.Point, piNode.Item.RightSite.Point, next.Item.RightSite.Point) == TriangleOrientation.Counterclockwise) {
                    ShortcutTwoFrontElements(piNode, next);
                    stack.Push(site);
                } else {
                    if (piNode.Item.LeftSite.Point.Y > piNode.Item.RightSite.Point.Y) {
                        stack.Push(piNode.Item.RightSite);
                    } else {
                        if (next.Item.LeftSite.Point.Y >= next.Item.RightSite.Point.Y)
                            return;
                        stack.Push(piNode.Item.RightSite);
                    }
                }
            } 
        }

      static bool DropsSharpEnoughToTheRight(CdtFrontElement frontElement) {
            var edge = frontElement.Edge;
            if(frontElement.LeftSite!=edge.upperSite)
                return false;
            var d=edge.lowerSite.Point-edge.upperSite.Point;
            Debug.Assert(d.X > 0 && d.Y <= 0);
            return d.X <= -0.5 * d.Y;
        }

        internal static RBNode<CdtFrontElement> FindNodeInFrontBySite(RbTree<CdtFrontElement> cdtFrontElements, CdtSite piSite) {
            return  cdtFrontElements.FindLast(x => x.LeftSite.Point.X <= piSite.Point.X);
        }


        void InsertAndLegalizeTriangle(CdtSite pi, CdtFrontElement frontElement) {
            if (Point.GetTriangleOrientation(pi.Point, frontElement.LeftSite.Point, frontElement.RightSite.Point) != TriangleOrientation.Collinear) {
                var tr = new CdtTriangle(pi, frontElement.Edge, createEdgeDelegate);
                Triangles.Insert(tr);
                LegalizeEdge(pi, tr.Edges[0]);
            } else { //we need to split the triangle below the element in to two triangles and legalize the old edges 
                //we also delete, that is forget, the frontElement.Edge
                var e = frontElement.Edge;
                e.upperSite.Edges.Remove(e);
                var t=e.CcwTriangle??e.CwTriangle;
                var oppositeSite = t.OppositeSite(e);
                RemoveTriangleButLeaveEdges(triangles, t);
                t=new CdtTriangle(frontElement.LeftSite, oppositeSite, pi, createEdgeDelegate);
                var t1 = new CdtTriangle(frontElement.RightSite, oppositeSite, pi, createEdgeDelegate);
                triangles.Insert(t);
                triangles.Insert(t1);
                LegalizeEdge(pi, t.OppositeEdge(pi));
                LegalizeEdge(pi,t1.OppositeEdge(pi));
            }

        }

        void LegalizeEdge(CdtSite pi, CdtEdge edge) {
            Debug.Assert(pi!=edge.upperSite && pi!=edge.lowerSite);
            if (edge.Constrained || edge.CcwTriangle == null || edge.CwTriangle == null) return;
            if (edge.CcwTriangle.Contains(pi))
                LegalizeEdgeForOtherCwTriangle(pi, edge);
            else
                LegalizeEdgeForOtherCcwTriangle(pi, edge);
        }

        void LegalizeEdgeForOtherCcwTriangle(CdtSite pi, CdtEdge edge) {
            var i = edge.CcwTriangle.Edges.Index(edge);
            if (IsIllegal(pi, edge.lowerSite, edge.CcwTriangle.Sites[i + 2], edge.upperSite)) {
                CdtEdge e = Flip(pi, edge);                
                LegalizeEdge(pi, e.CwTriangle.OppositeEdge(pi));
                LegalizeEdge(pi, e.CcwTriangle.OppositeEdge(pi));
            }
        }

#if TEST_MSAGL
        List<DebugCurve> ShowIllegalEdge(CdtEdge edge, CdtSite pi, int i) {
            List<DebugCurve> ls = new List<DebugCurve>();
            ls.Add(new DebugCurve(new Ellipse(2, 2, pi.Point)));
            for (int j = 0; j < 3; j++) {
                var ee = edge.CcwTriangle.Edges[j];
                ls.Add(new DebugCurve(j == i ? "red" : "blue", new LineSegment(ee.upperSite.Point, ee.lowerSite.Point)));
            }
            ls.Add(new DebugCurve(100,1, "black", Circumcircle(edge.CcwTriangle.Sites[0].Point,edge.CcwTriangle.Sites[1].Point,edge.CcwTriangle.Sites[2].Point)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(ls);
            return ls;
        }

        static Ellipse Circumcircle(Point a, Point b, Point c) {
            var mab = 0.5 * (a + b);
            var mbc = 0.5 * (c + b);
            Point center;
            Point.LineLineIntersection(mab, mab + (b - a).Rotate(Math.PI / 2), mbc, mbc + (b - c).Rotate(Math.PI / 2), out center);
            var r = (center - a).Length;
            return new Ellipse(r,r, center);
        }
#endif

        void LegalizeEdgeForOtherCwTriangle(CdtSite pi, CdtEdge edge) {
            var i=edge.CwTriangle.Edges.Index(edge);
//            if (i == -1)
//            {
//                List<DebugCurve> ls = new List<DebugCurve>();
//                ls.Add(new DebugCurve(new Ellipse(2, 2, pi.Point)));
//                for (int j = 0; j < 3; j++)
//                {
//                    var ee = edge.CwTriangle.Edges[j];
//                    ls.Add(new DebugCurve(100,1, j == i ? "red" : "blue", new LineSegment(ee.upperSite.Point, ee.lowerSite.Point)));
//                }
//                ls.Add(new DebugCurve("purple", new LineSegment(edge.upperSite.Point, edge.lowerSite.Point)));
//                
//                LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(ls);
//            }
            Debug.Assert(i>=0);
            if (IsIllegal(pi, edge.upperSite, edge.CwTriangle.Sites[i + 2], edge.lowerSite)) {
                //ShowIllegalEdge(edge, i, pi);

                CdtEdge e = Flip(pi, edge);
                LegalizeEdge(pi, e.CwTriangle.OppositeEdge(pi) );
                LegalizeEdge(pi, e.CcwTriangle.OppositeEdge(pi));
            }
        }
#if TEST_MSAGL
        void ShowIllegalEdge(CdtEdge edge, int i, CdtSite pi) {
            List<DebugCurve> ls=new List<DebugCurve>();
            ls.Add(new DebugCurve(new Ellipse(2, 2, pi.Point)));
            for(int j=0;j<3;j++) {
                var ee=edge.CwTriangle.Edges[j];
                ls.Add(new DebugCurve(j==i?"red":"blue", new LineSegment(ee.upperSite.Point,ee.lowerSite.Point)));
            }
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(ls);
        }
#endif
        static bool IsIllegal(CdtSite pi, CdtSite a, CdtSite b, CdtSite c) {
            return InCone(pi, a, b, c) && InCircle(pi, a, b, c);
        }

        /// <summary>
        /// Testing that d in inside of the circumcircle of (a,b,c). 
        /// The good explanation of this test is in 
        /// "Guibas, Stolfi,"Primitives for the Manipulation of General Subdivisions and the Computation of Voronoi Diagrams
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool InCircle(CdtSite d, CdtSite a, CdtSite b, CdtSite c) {
            Debug.Assert(Point.GetTriangleOrientation(a.Point, b.Point, c.Point) == TriangleOrientation.Counterclockwise);
            /*
             *  | ax-dx ay-dy (ax-dx)^2+(ay-dy)^2|
             *  | bx-dx by-dy (bx-dx)^2+(by-dy)^2|
             *  | cx-dx cy-dy (cx-dx)^2+(cy-dy)^2|
             */
            var axdx = a.Point.X - d.Point.X;
            var aydy = a.Point.Y - d.Point.Y;
            var bxdx = b.Point.X - d.Point.X;
            var bydy = b.Point.Y - d.Point.Y;
            var cxdx = c.Point.X - d.Point.X;
            var cydy = c.Point.Y - d.Point.Y;
            var t0 = axdx * axdx + aydy * aydy;
            var t1 = bxdx * bxdx + bydy * bydy;
            var t2 = cxdx * cxdx + cydy * cydy;
            return axdx * (bydy * t2 - cydy * t1) - bxdx * (aydy * t2 - cydy * t0) + cxdx * (aydy * t1 - bydy * t0) > ApproximateComparer.Tolerance;                    
        }

      /// <summary>
        /// 
        /// </summary>
        /// <param name="pi"></param>
        /// <param name="a">point on left side of the cone</param>
        /// <param name="b">the apex</param>
        /// <param name="c">point on the right side of the cone</param>
        static bool InCone(CdtSite pi, CdtSite a, CdtSite b, CdtSite c) {
            Debug.Assert(Point.GetTriangleOrientation(a.Point,b.Point,c.Point)==TriangleOrientation.Counterclockwise);

            return Point.GetTriangleOrientation(a.Point, pi.Point, b.Point) == TriangleOrientation.Clockwise &&
                Point.GetTriangleOrientation(b.Point, pi.Point, c.Point) == TriangleOrientation.Clockwise;
        }

        static CdtEdge Flip(CdtSite pi, CdtEdge edge) {
            Debug.Assert(!edge.IsAdjacent(pi));
            Debug.Assert(edge.CcwTriangle.Contains(pi) || edge.CwTriangle.Contains(pi));
            //get surrounding data
            CdtTriangle t, ot;
            if (edge.CcwTriangle.Contains(pi)) {
                t = edge.CcwTriangle;
                ot = edge.CwTriangle;
            } else {
                t = edge.CwTriangle;
                ot = edge.CcwTriangle;
            }
            Debug.Assert(t.Contains(pi));
            var eIndex = t.Edges.Index(edge);
            var eOtherIndex = ot.Edges.Index(edge);
            Debug.Assert(eIndex > -1 && eOtherIndex > -1);
            var pl = ot.Sites[eOtherIndex + 2];
            var edgeBeforPi = t.Edges[eIndex + 1];
            var edgeBeforPl = ot.Edges[eOtherIndex + 1];

            //changing t 
            var newEdge = Cdt.GetOrCreateEdge(pi, pl);
            t.Sites[eIndex + 1] = pl;
            t.Edges[eIndex] = edgeBeforPl;
            t.Edges[eIndex + 1] = newEdge;
            //changing ot
            ot.Sites[eOtherIndex + 1] = pi;
            ot.Edges[eOtherIndex] = edgeBeforPi;
            ot.Edges[eOtherIndex + 1] = newEdge;
            //orient the new edge and the two edges that move from one triangle to another
            if (edgeBeforPl.lowerSite == pl)
                edgeBeforPl.CcwTriangle = t;
            else
                edgeBeforPl.CwTriangle = t;

            if (edgeBeforPi.lowerSite == pi)
                edgeBeforPi.CcwTriangle = ot;
            else
                edgeBeforPi.CwTriangle = ot;

            if (newEdge.upperSite == pi) {
                newEdge.CcwTriangle = ot;
                newEdge.CwTriangle = t;
            } else {
                newEdge.CcwTriangle = t;
                newEdge.CwTriangle = ot;
            }
            Debug.Assert(CheckTriangle(t));
            Debug.Assert(CheckTriangle(t));
            //ShowFlip(pi, t, ot);
            edge.upperSite.Edges.Remove(edge); //forget the edge 
            return newEdge;
        }
#if TEST_MSAGL
        static void ShowFlip(CdtSite pi, CdtTriangle t, CdtTriangle ot) {
            List<DebugCurve> ls=new List<DebugCurve>();
            ls.Add(new DebugCurve(new Ellipse(2,2, pi.Point)));
            for(int i=0;i<3;i++) {
                var e=t.Edges[i];
                ls.Add(new DebugCurve(100, 1, "red", new LineSegment(e.upperSite.Point,e.lowerSite.Point)));
            }
            for (int i = 0; i < 3; i++)
            {
                var e = ot.Edges[i];
                ls.Add(new DebugCurve(100, 1, "blue", new LineSegment(e.upperSite.Point, e.lowerSite.Point)));
            }
            ls.Add(new DebugCurve(Circumcircle(t.Sites[0].Point, t.Sites[1].Point, t.Sites[2].Point)));
            LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(ls);
        }
#endif

        static bool CheckTriangle(CdtTriangle t) {
            if (Point.GetTriangleOrientation(t.Sites[0].Point, t.Sites[1].Point, t.Sites[2].Point) != TriangleOrientation.Counterclockwise) {
                return false;
            }
            for (int i = 0; i < 3; i++) {
                var e = t.Edges[i];
                var a = t.Sites[i];
                var b = t.Sites[i+1];
                if (!e.IsAdjacent(a) || !e.IsAdjacent(b)) return false;
                if (e.upperSite == a) {
                    if (e.CcwTriangle != t)
                        return false;
                }
                else if (e.CwTriangle != t)
                    return false;

            }
            return true;
        }

        void ProjectToFront(CdtSite site, out RBNode<CdtFrontElement> frontElement) {
            frontElement = front.FindLast(s => s.X <= site.Point.X);
        }
        
    }
}
