using System;
using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation {
    /// <summary>
    /// a trianlge oriented counterclockwise
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif

    public class CdtTriangle {
        ///<summary>
        /// the edges
        ///</summary>
        public readonly ThreeArray<CdtEdge> Edges = new ThreeArray<CdtEdge>();
        ///<summary>
        /// the sites
        ///</summary>
        public readonly ThreeArray<CdtSite> Sites = new ThreeArray<CdtSite>();

        public TriangleOrientation Orientation { get { return Point.GetTriangleOrientation(Sites[0].Point, Sites[1].Point, Sites[2].Point); } }

        internal CdtTriangle(CdtSite a, CdtSite b, CdtSite c, Func<CdtSite, CdtSite, CdtEdge> createEdgeDelegate) {
            var orientation = Point.GetTriangleOrientation(a.Point, b.Point, c.Point);
            switch (orientation) {
                case TriangleOrientation.Counterclockwise:
                    FillCcwTriangle(a, b, c, createEdgeDelegate);
                    break;
                case TriangleOrientation.Clockwise:
                    FillCcwTriangle(a, c, b, createEdgeDelegate);
                    break;
                default: throw new InvalidOperationException();
            }
        }

        internal CdtTriangle(CdtSite pi, CdtEdge edge, Func<CdtSite, CdtSite, CdtEdge> createEdgeDelegate) {
            switch (Point.GetTriangleOrientation(edge.upperSite.Point, edge.lowerSite.Point, pi.Point)) {
                case TriangleOrientation.Counterclockwise:
                    edge.CcwTriangle = this;
                    Sites[0] = edge.upperSite;
                    Sites[1] = edge.lowerSite;
                    break;
                case TriangleOrientation.Clockwise:
                    edge.CwTriangle = this;
                    Sites[0] = edge.lowerSite;
                    Sites[1] = edge.upperSite;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            Edges[0] = edge;
            Sites[2] = pi;
            CreateEdge(1, createEdgeDelegate);
            CreateEdge(2, createEdgeDelegate);
        }

        //
        internal CdtTriangle(CdtSite aLeft, CdtSite aRight, CdtSite bRight, CdtEdge a, CdtEdge b, Func<CdtSite, CdtSite, CdtEdge> createEdgeDelegate) {
           // Debug.Assert(Point.GetTriangleOrientation(aLeft.Point, aRight.Point, bRight.Point) == TriangleOrientation.Counterclockwise);
            Sites[0] = aLeft;
            Sites[1] = aRight;
            Sites[2] = bRight;
            Edges[0] = a;
            Edges[1] = b;
            BindEdgeToTriangle(aLeft, a);
            BindEdgeToTriangle(aRight, b);
            CreateEdge(2, createEdgeDelegate);
            Debug.Assert(Orientation != TriangleOrientation.Collinear);
        }
        /// <summary>
        /// in the trianlge, which is always oriented counterclockwise, the edge starts at site 
        /// </summary>
        /// <param name="site"></param>
        /// <param name="edge"></param>
        void BindEdgeToTriangle(CdtSite site, CdtEdge edge) {
            if (site == edge.upperSite)
                edge.CcwTriangle = this;
            else
                edge.CwTriangle = this;
        }

        /// <summary>
        /// here a,b,c comprise a ccw triangle
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="createEdgeDelegate"></param>
        void FillCcwTriangle(CdtSite a, CdtSite b, CdtSite c, Func<CdtSite, CdtSite, CdtEdge> createEdgeDelegate) {
            Sites[0] = a; Sites[1] = b; Sites[2] = c;
            for (int i = 0; i < 3; i++)
                CreateEdge(i, createEdgeDelegate);
        }

        void CreateEdge(int i, Func<CdtSite, CdtSite, CdtEdge> createEdgeDelegate) {
            var a = Sites[i];
            var b = Sites[i + 1];
            var edge = Edges[i] = createEdgeDelegate(a, b);
            BindEdgeToTriangle(a, edge);
        }

        internal bool Contains(CdtSite cdtSite) {
            return Sites.Contains(cdtSite);
        }

        internal CdtEdge OppositeEdge(CdtSite pi) {
            var index = Sites.Index(pi);
            Debug.Assert(index != -1);
            return Edges[index + 1];
        }

#if TEST_MSAGL
        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() {
            return String.Format("({0},{1},{2}", Sites[0], Sites[1], Sites[2]);
        }
#endif

        internal CdtSite OppositeSite(CdtEdge cdtEdge) {
            var i = Edges.Index(cdtEdge);
            return Sites[i + 2];
        }

        internal Rectangle BoundingBox() {
            Rectangle rect = new Rectangle(Sites[0].Point, Sites[1].Point);
            rect.Add(Sites[2].Point);
            return rect;
        }
    }
}
