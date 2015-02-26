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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Prototype.Ranking {
    /// <summary>
    /// Ranking layout settings
    /// </summary>
#if PROPERTY_GRID_SUPPORT
    [Description("Settings for the layout with ranking by y-axis"),
    TypeConverterAttribute(typeof(ExpandableObjectConverter))]
#endif
    public class RankingLayoutSettings:LayoutAlgorithmSettings {
        private int pivotNumber = 50;
        private double scaleX = 200;
        private double scaleY = 200;
        private double omegaX = .15;
        private double omegaY = .15;

        ///<summary>
        ///</summary>
        public RankingLayoutSettings()
        {
            this.NodeSeparation = 0;
        }

        /// <summary>
        /// Number of pivots in Landmark Scaling (between 3 and number of objects).
        /// </summary>
        #if PROPERTY_GRID_SUPPORT
[DisplayName("Number of pivots")]
        [Description("Number of pivots in MDS")]
        [DefaultValue(50)]
#endif
        public int PivotNumber {
            set { pivotNumber = value; }
            get { return pivotNumber; }
        }

        /// <summary>
        /// Impact of group structure on layout in the x-axis.
        /// </summary>
#if PROPERTY_GRID_SUPPORT
        [DisplayName("Group effect by x-axis")]
        [DefaultValue(0.15)]
#endif
        public double OmegaX {
            set { omegaX = value; }
            get { return omegaX; }
        }

        /// <summary>
        /// Impact of group structure on layout in the y-axis.
        /// </summary>
#if PROPERTY_GRID_SUPPORT
        [DisplayName("Group effect by y-axis")]
        [DefaultValue(0.15)]
#endif
        public double OmegaY {
            set { omegaY = value; }
            get { return omegaY; }
        }


        /// <summary>
        /// X Scaling Factor.
        /// </summary>
#if PROPERTY_GRID_SUPPORT
        [DisplayName("Scale by x-axis")]
        [Description("The resulting layout will be scaled by x-axis by this number")]
        [DefaultValue(200.0)]
#endif
        public double ScaleX {
            set { scaleX = value; }
            get { return scaleX; }
        }

        /// <summary>
        /// Y Scaling Factor.
        /// </summary>
#if PROPERTY_GRID_SUPPORT
        [DisplayName("Scale by y-axis")]
        [Description("The resulting layout will be scaled by y-axis by this number")]
        [DefaultValue(200.0)]

#endif
        public double ScaleY {
            set { scaleY = value; }
            get { return scaleY; }
        }

        /// <summary>
/// Clones the object
/// </summary>
/// <returns></returns>
        public override LayoutAlgorithmSettings Clone() {
            return MemberwiseClone() as LayoutAlgorithmSettings;
        }
    }
}
