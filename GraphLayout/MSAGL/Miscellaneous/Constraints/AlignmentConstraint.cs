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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Incremental;

namespace Microsoft.Msagl.Prototype.Constraints {
    /// <summary>
    /// This doesn't work yet, but it will be a constraint that aligns nodes, optionally to some fixed orientation (horizontal, vertical, 45 degrees, etc).
    /// </summary>
    public class AlignmentConstraint : IConstraint {
        List<Node> nodes;
        /// <summary>
        /// Create an alignment constraint for the given set of nodes
        /// </summary>
        /// <param name="nodes"></param>
        public AlignmentConstraint(IEnumerable<Node> nodes) {
            this.nodes = nodes.ToList();
        }

        void leastSquaresLineFit(out double a, out double b) {
            double mx = 0, my = 0, sx2 = 0, sy2 = 0, sxy=0, n = nodes.Count;
            foreach (var v in nodes) {
                double x = v.Center.X, y = v.Center.Y;
                mx += x;
                my += y;
                sx2 += x * x;
                sy2 += y * y;
                sxy += x * y;
            }
            mx /= n;
            my /= n;
            double B = 0.5*((sy2 - n * my * my) - (sx2 - n * mx * mx)) / (n * mx * my - sxy);
            b = Math.Sqrt(B * B + 1) - B;
            //b = - Math.Sqrt(B * B + 1) - B;
            a = my - b * mx;
        }

        #region IConstraint Members
        /// <summary>
        /// project using a perpendicular least-squares fit
        /// </summary>
        /// <returns></returns>
        public double Project() {
            double a, b, d=0;
            leastSquaresLineFit(out a, out b);
            // project to the line y = a + b x
            foreach (var v in nodes) {
                double x0 = v.Center.X, y0 = v.Center.Y;
                double x = (b * y0 + x0 ) / (b * b + 1.0);
                double y = a + b * x;
                Point p = new Point(x, y);
                d = (v.Center - p).Length;
                v.Center = p;
            }
            return d;
        }
        /// <summary>
        /// level 1
        /// </summary>
        public int Level {
            get { return 1; }
        }
        /// <summary>
        /// Get the list of nodes involved in the constraint
        /// </summary>
        public IEnumerable<Node> Nodes { get { return nodes;} }

        #endregion
    }
}