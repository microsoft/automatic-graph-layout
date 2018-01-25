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
