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
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Microsoft.Msagl.Routing {
    /// <summary>
    /// the edge direction is from the upperSite to lowerSite
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif

    public class CdtEdge {
        ///<summary>
        ///</summary>
        public CdtSite upperSite;
        ///<summary>
        ///</summary>
        public CdtSite lowerSite;
        ///<summary>
        ///</summary>
        CdtTriangle ccwTriangle; //in this triangle the edge goes counterclockwise
        ///<summary>
        ///</summary>
        CdtTriangle cwTriangle; //in this triangle the edge goes clockwise, against the triangle orientation
        ///<summary>
        /// is an obstacle side, or a given segment
        ///</summary>
        public bool Constrained;

        ///<summary>
        ///</summary>
        ///<param name="a"></param>
        ///<param name="b"></param>
        public CdtEdge(CdtSite a, CdtSite b) {
            var above = Cdt.Above(a.Point, b.Point);
            if (above == 1) {
                upperSite = a;
                lowerSite = b;
            }
            else {
                Debug.Assert(above != 0);
                lowerSite = a;
                upperSite = b;
            }

            upperSite.AddEdgeToSite(this);
        }

        /// <summary>
        /// the amount of free space around the edge
        /// </summary>
        internal double Capacity = 1000000;


        /// <summary>
        /// the amount of residual free space around the edge
        /// </summary>
        public double ResidualCapacity { get; set; }

        ///<summary>
        ///</summary>
        public CdtTriangle CcwTriangle {
            get { return ccwTriangle; }
            set {
                Debug.Assert(value == null || cwTriangle==null || value.OppositeSite(this) !=
                    cwTriangle.OppositeSite(this));
                ccwTriangle = value;
            }
        }

        ///<summary>
        ///</summary>
        public CdtTriangle CwTriangle {
            get { return cwTriangle; }
            set {
                Debug.Assert(value == null || ccwTriangle==null || value.OppositeSite(this) !=
                    ccwTriangle.OppositeSite(this));
                cwTriangle = value;
            }
        }


        /// <summary>
        /// returns the trianlge on the edge opposite to the site 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public CdtTriangle GetOtherTriangle(CdtSite p) {
            return cwTriangle.Contains(p) ? ccwTriangle : cwTriangle;
        }

        ///<summary>
        ///</summary>
        ///<param name="pi"></param>
        ///<returns></returns>
        public bool IsAdjacent(CdtSite pi) {
            return pi == upperSite || pi == lowerSite;
        }

        ///<summary>
        ///</summary>
        ///<param name="triangle"></param>
        ///<returns></returns>
        public CdtTriangle GetOtherTriangle(CdtTriangle triangle) {
            return ccwTriangle == triangle ? cwTriangle : ccwTriangle;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() {
            return String.Format("({0},{1})", upperSite, lowerSite);
        }

        ///<summary>
        ///</summary>
        ///<param name="site"></param>
        ///<returns></returns>
        public CdtSite OtherSite(CdtSite site) {
            Debug.Assert(IsAdjacent(site));
            return upperSite == site ? lowerSite : upperSite;
        }
    }
}
