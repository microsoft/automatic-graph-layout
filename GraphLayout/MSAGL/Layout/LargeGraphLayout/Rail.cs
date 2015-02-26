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
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    /// <summary>
    /// keeps a part of EdgeGeometry which is visible
    /// </summary>
    public class Rail {
#if DEBUG
        static int railCount;
        int id;
#endif

        /// <summary>
        /// the number of higlighted edges passing through the rail
        /// </summary>
        bool _isHighlighted;

        /// <summary>
        /// can be ICurve or Arrowhead
        /// </summary>
        public object Geometry;

        /// <summary>
        /// the point where the edge curve touches the arrowhead
        /// </summary>
        public readonly Point CurveAttachmentPoint;

        /// <summary>
        /// the corresponding LgEdgeInfo
        /// </summary>
        public LgEdgeInfo TopRankedEdgeInfoOfTheRail;

        internal int ZoomLevel;
#if DEBUG
        Rail() {
            railCount++;
            id = railCount;
        }
        /// <summary>
        /// returning id for debugging
        /// </summary>
        /// <returns></returns>
        public int GetId() {
            return id;
        }
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() {
            return id.ToString();
        }
#endif

        internal Rail(ICurve curveSegment, LgEdgeInfo topRankedEdgeInfoOfTheRail, int zoomLevel)
#if DEBUG
            : this()
#endif
        {
            TopRankedEdgeInfoOfTheRail = topRankedEdgeInfoOfTheRail;
            this.ZoomLevel = zoomLevel;
            Geometry = curveSegment;
        }

        internal Rail(Arrowhead arrowhead, Point curveAttachmentPoint, LgEdgeInfo topRankedEdgeInfoOfTheRail,
            int zoomLevel)
#if DEBUG
            : this()
#endif
        {
            TopRankedEdgeInfoOfTheRail = topRankedEdgeInfoOfTheRail;
            Geometry = arrowhead.Clone();
            CurveAttachmentPoint = curveAttachmentPoint;
            ZoomLevel = zoomLevel;
        }

        internal Rectangle BoundingBox {
            get {
                var icurve = Geometry as ICurve;
                if (icurve != null)
                    return icurve.BoundingBox;
                var arrowhead = (Arrowhead) Geometry;
                var rec = new Rectangle(arrowhead.TipPosition, CurveAttachmentPoint);
                rec.Pad(arrowhead.Width); // sometimes this box will not cover the arrowhead, but rarely
                return rec;
            }
        }

        /// <summary>
        /// the number of higlighted edges passing through the rail
        /// </summary>
        public bool IsHighlighted {
            get { return _isHighlighted; }
            set { _isHighlighted = value; }
        }



        internal Tuple<Point, Point> PointTuple() {
            var icurve = Geometry as ICurve;
            if (icurve != null)
                return new Tuple<Point, Point>(icurve.Start, icurve.End);
            var arrowhead = (Arrowhead) Geometry;
            return new Tuple<Point, Point>(arrowhead.TipPosition, CurveAttachmentPoint);
        }


    }
}