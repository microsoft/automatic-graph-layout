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
using System.Text;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Core.Layout
{
    /// <summary>
    /// Settings controlling how ideal edge lengths will be calculated for layouts that consider it.
    /// </summary>
    /// <remarks>
    /// This is a struct so we can do a shallow copy without having to do a MemberwiseClone.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct IdealEdgeLengthSettings
    {
        #region DefaultLength

        /// <summary>
        /// Basic desired length for edges
        /// </summary>
        public double DefaultLength
        {
            get;
            set;
        }

        #endregion DefaultLength

        #region EdgeLengthsProportionalToSymmetricDifferenceProperty

        /// <summary>
        /// Gets or sets whether to set the ideal edge length based on the degree and number of shared
        /// neighbors of end nodes.
        /// </summary>
        public bool ProportionalToSymmetricDifference
        {
            get;
            set;
        }

        #endregion ProportionalToSymmetricDifference

        #region ProportionalEdgeLengthAdjustmentProperty

        /// <summary>
        /// Gets or sets the fraction that ideal edge length grows based on the degree of its endpoints.
        /// </summary>
        public double ProportionalEdgeLengthAdjustment
        {
            get;
            set;
        }

        #endregion ProportionalEdgeLengthAdjustmentProperty

        #region ProportionalEdgeLengthOffsetProperty

        /// <summary>
        /// Gets or sets the fraction that the ideal edge length is initially modified prior to the proportional adjustments.
        /// </summary>
        public double ProportionalEdgeLengthOffset
        {
            get;
            set;
        }

        #endregion ProportionalEdgeLengthOffsetProperty

        #region EdgeSeparationConstraints

        /// <summary>
        /// If true then direction separation constraints will be applied to all edges on InitializeLayout
        /// </summary>
        public Directions EdgeDirectionConstraints { get; set; }

        /// <summary>
        /// Controls the separation used in Edge Constraints
        /// </summary>
        public double ConstrainedEdgeSeparation
        {
            get { return constrainedEdgeSeparation; }
            set { constrainedEdgeSeparation = value; }
        }

        internal double constrainedEdgeSeparation;

        #endregion EdgeSeparationConstraints
    }
}
