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
﻿//-----------------------------------------------------------------------
// <copyright file="EdgeExtensions.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Extensions for the MSAGL Edge type.
    /// </summary>
    public static class EdgeExtensions
    {
        /// <summary>
        /// Gets the series of points that run along the edge.
        /// </summary>
        /// <returns>An array of points that go along the edge from start to end.</returns>
        public static Point[] GetPoints(this Edge edge)
        {
            return edge.GetPoints(1000);
        }

        /// <summary>
        /// Gets the series of points that run along the edge.
        /// </summary>
        /// <param name="edge">The edge to be broken into points.</param>
        /// <param name="sampleCount">The number of points the edge should be broken in to.</param>
        /// <returns>An array of points that go along the edge from start to end.</returns>
        public static Point[] GetPoints(this Edge edge, int sampleCount)
        {
            if (edge == null || edge.Curve == null)
            {
                throw new ArgumentNullException("edge");
            }

            ICurve curve = edge.Curve;
            Point[] edgePoints = new Point[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                double positionAlongEdge = curve.ParStart + ((i / (double)sampleCount) * (curve.ParEnd - curve.ParStart));
                edgePoints[i] = edge.Curve[positionAlongEdge];
            }

            return edgePoints;
        }
    }
}
