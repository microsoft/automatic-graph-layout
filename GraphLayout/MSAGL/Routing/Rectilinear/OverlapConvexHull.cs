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
//
// OverlapConvexHull.cs
// MSAGL class for Convex Hulls around overlapping obstacles for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System.Collections.Generic;
using System.Linq;

using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// This stores the location and type of a Port.
    /// </summary>
    internal class OverlapConvexHull {
        internal Polyline Polyline { get; private set; }

        /// <summary>
        /// This is some arbitrary obstacle inside the convex hull so we qualify Select().Where() so we
        /// don't get the CH duplicated in the scanline etc. enumerations.
        /// </summary>
        internal Obstacle PrimaryObstacle { get; private set; }

        internal List<Obstacle> Obstacles { get; private set; }

        internal OverlapConvexHull(Polyline polyline, IEnumerable<Obstacle> obstacles) {
            this.Polyline = polyline;
            this.Obstacles = obstacles.ToList();
            this.PrimaryObstacle = this.Obstacles[0];

            Obstacle.RoundVertices(this.Polyline);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return this.Polyline.ToString();
        }
    }
}
