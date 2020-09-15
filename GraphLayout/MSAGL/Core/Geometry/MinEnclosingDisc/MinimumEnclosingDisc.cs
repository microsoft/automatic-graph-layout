using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Msagl.Core.Geometry
{
    /// <summary>
    /// implementation of the "MoveToFront" method for computing the minimum enclosing disc of a collection of points.
    /// Runs in time linear in the number of points.  After Welzl'1991.
    /// </summary>
    public class MoveToFront {
        LinkedList<int> L;
        Point[] ps;
        /// <summary>
        /// minimum enclosing disc
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public Disc disc;
        /// <summary>
        /// list of 2 or 3 points lying on the boundary
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public List<int> boundary;
        /// <summary>
        /// Constructs the minimum enclosing disc for the specified points
        /// </summary>
        /// <param name="ps"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "ps")]
        public MoveToFront(Point[] ps)
        {
            ValidateArg.IsNotNull(ps, "ps");
            this.ps = ps;
            L = new LinkedList<int>();
            for (int i = 0; i < ps.Length; ++i)
            {
                L.AddLast(i);
            }
            MinDisc md = mtf_md(null, new List<int>());
            disc = md.disc;
            boundary = md.boundary;
        }
        class MinDisc
        {
            public Disc disc;
            public List<int> boundary;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1805:DoNotInitializeUnnecessarily")]
            public MinDisc(Point[] ps, List<int> b)
            {
                this.boundary = b;
                Debug.Assert(b.Count <= 3);
                switch(b.Count) {
                    case 0:
                        disc = null;
                        break;
                    case 1:
                        disc = new Disc(ps[b[0]]);
                        break;
                    case 2:
                        disc = new Disc(ps[b[0]], ps[b[1]]);
                        break;
                    case 3:
                        disc = new Disc(ps[b[0]], ps[b[1]], ps[b[2]]);
                        break;
                }
            }
            public bool contains(Point p)
            {
                if (disc == null)
                {
                    return false;
                }
                return disc.Contains(p);
            }
        }
#if TEST_MSAGL
         bool collinear3(List<int> b)
        {
            if (b.Count == 3)
            {
                return Disc.Collinear(ps[b[0]], ps[b[1]], ps[b[2]]);
            }
            return false;
        }
#endif
         MinDisc mtf_md(LinkedListNode<int> lPtr, List<int> b)
        {
            Debug.Assert(b.Count <= 3);
            MinDisc md = new MinDisc(ps,b);
            if (b.Count == 3)
            {
                return md;
            }
            LinkedListNode<int> lnode = L.First;
            while(lnode!=null&&lnode!=lPtr)
            {
                LinkedListNode<int> lnext = lnode.Next;
                int p = lnode.Value;
                if (!md.contains(ps[p]))
                {
                    List<int> _b = new List<int>(b);
                    _b.Add(p);
#if TEST_MSAGL
                    Debug.Assert(!collinear3(_b),"Collinear points on boundary of minimal enclosing disc");
#endif
                    md = mtf_md(lnode, _b);
                    L.Remove(lnode);
                    L.AddFirst(lnode);
                }
                lnode = lnext;
            }
            return md;
        }
    }
    /// <summary>
    /// static methods for obtaining a minimum enclosing disc of a collection of points
    /// </summary>
    public static class MinimumEnclosingDisc
    {
        /// <summary>
        /// linear-time computation using the move-to-front heuristic by Welzl
        /// </summary>
        /// <param name="points">points that must be enclosed</param>
        /// <returns>Smallest disc that encloses all the points</returns>
        public static Disc LinearComputation(Point[] points)
        {
            MoveToFront m = new MoveToFront(points);
            return m.disc;
        }
        /// <summary>
        /// Computing the minimum enclosing disc the slow stupid way.  Just for testing purposes.
        /// </summary>
        /// <param name="points"></param>
        /// <returns>Smallest disc that encloses all the points</returns>
        public static Disc SlowComputation(Point[] points)
        {
            ValidateArg.IsNotNull(points, "points");
            int n = points.Length;
            Disc mc = null;
            int[] b = null;
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i != j)
                    {
                        Disc c = new Disc(points[i], points[j]);
                        if (c.Contains(points, new int[] { i, j }))
                        {
                            if (mc == null || mc.Radius > c.Radius)
                            {
                                mc = c;
                                b = new int[] { i, j };
                            }
                        }
                    }
                    for (int k = 0; k < n; ++k)
                    {
                        if (k != i && k != j && !Disc.Collinear(points[i],points[j],points[k]))
                        {
                            Disc c3 = new Disc(points[i], points[j], points[k]);
                            if (c3.Contains(points, new int[]{i,j,k}))
                            {
                                if (mc == null || mc.Radius > c3.Radius)
                                {
                                    mc = c3;
                                    b = new int[] { i, j, k };
                                }
                            }
                        }
                    }
                }
            }
            Debug.Assert(b != null);
            return mc;
        }

    }
}
