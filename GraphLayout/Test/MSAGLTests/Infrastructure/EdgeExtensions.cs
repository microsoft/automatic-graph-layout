//-----------------------------------------------------------------------
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
