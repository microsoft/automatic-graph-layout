using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Microsoft.Msagl.Routing
{
    ///<summary>
    ///</summary>
    [DebuggerDisplay("{Point}")]
#if TEST_MSAGL
    [Serializable]
#endif
    public class CdtSite
    {
        /// <summary>
        /// Object to which this site refers to.
        /// </summary>
        public object Owner { get; set; }

        ///<summary>
        ///</summary>
        public Point Point;
        /// <summary>
        /// each CdtSite points to the edges for which it is the upper virtex ( for horizontal edges it is the left point)
        /// </summary>
        public List<CdtEdge> Edges;

        internal List<CdtEdge> InEdges;

        ///<summary>
        ///</summary>
        ///<param name="isolatedSite"></param>
        public CdtSite(Point isolatedSite)
        {
            Point = isolatedSite;
        }

        internal void AddEdgeToSite(CdtEdge edge)
        {
            if (Edges == null)
                Edges = new List<CdtEdge>();
            Edges.Add(edge);
        }
#if TEST_MSAGL
        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Point.ToString();
        }
#endif

        internal CdtEdge EdgeBetweenUpperSiteAndLowerSite(CdtSite b)
        {
            Debug.Assert(Cdt.Above(this, b) > 0);
            if (Edges != null)
                foreach (var edge in Edges)
                    if (edge.lowerSite == b)
                        return edge;
            return null;
        }

        internal void AddInEdge(CdtEdge e)
        {
            if (InEdges == null)
                InEdges = new List<CdtEdge>();
            InEdges.Add(e);
        }
        /// <summary>
        /// enumerates over all site triangles
        /// </summary>
        internal IEnumerable<CdtTriangle> Triangles
        {
            // this function might not work correctly if InEdges are not set
            get
            {
                CdtEdge edge;
                if (Edges != null && Edges.Count>0)
                    edge = Edges[0];
                else if (InEdges != null && InEdges.Count>0)
                    edge = InEdges[0];
                else yield break;

                //going counterclockwise around the site
                var e = edge;
                do
                {
                    var t = e.upperSite == this ? e.CcwTriangle : e.CwTriangle;
                    if (t == null)
                    {
                        e = null;
                        break;
                    }
                    yield return t;
                    e = t.Edges[t.Edges.Index(e) + 2];
                } while (e != edge);//full circle

                if (e != edge)
                { //we have not done the full circle, starting again with edge but now going clockwise around the site
                    e = edge;
                    do
                    { 
                        var t = e.upperSite == this ? e.CwTriangle : e.CcwTriangle;
                        if (t == null)
                        {
                            break;
                        }
                        yield return t;
                        e = t.Edges[t.Edges.Index(e) + 1];
                    } while (true); // we will hit a null triangle for the convex hull border edge
                }
            }
            
        }
    }
}
