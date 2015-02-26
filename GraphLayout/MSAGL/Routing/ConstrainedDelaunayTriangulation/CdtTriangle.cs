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


        internal CdtTriangle(CdtSite a, CdtSite b, CdtSite c, Func<CdtSite, CdtSite, CdtEdge> createEdgeDelegate) {
            var orientation = Point.GetTriangleOrientationWithNoEpsilon(a.Point, b.Point, c.Point);
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
            switch (Point.GetTriangleOrientationWithNoEpsilon(edge.upperSite.Point, edge.lowerSite.Point, pi.Point)) {
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

        internal bool EdgeIsReversed(int i) {
            return Edges[i].CwTriangle == this;
        }

        internal bool EdgeIsReversed(CdtEdge edge) {
            return edge.CwTriangle == this;
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

#if DEBUG&&TEST_MSAGL
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
