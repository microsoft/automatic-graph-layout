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
using System.ComponentModel;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Routing;
#if TEST_MSAGL
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Routing;
#endif

namespace Microsoft.Msagl.Core.Layout {

#if !SILVERLIGHT
    ///<summary>
    /// controls many properties of the layout algorithm
    ///</summary>
    [Description("Specifies the layout algorithm parametres")]
    [TypeConverter(typeof (ExpandableObjectConverter))]
    [DisplayName("Layout algorithm settings")]
#endif
#if TEST_MSAGL
    [Serializable]
#endif
    public abstract class LayoutAlgorithmSettings {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2235:MarkAllNonSerializableFields")]
        EdgeRoutingSettings edgeRoutingSettings = new EdgeRoutingSettings();
        ///<summary>
        /// defines edge routing behaviour
        ///</summary>
        public EdgeRoutingSettings EdgeRoutingSettings {
            get { return edgeRoutingSettings; }
            set { edgeRoutingSettings = value; }
        }
        #region
#if REPORTING
        bool reporting;

        /// <summary>
        /// Controls the reporting facility.
        /// </summary>
        [Description("Controls the reporting facility.")]
        [DefaultValue(false)]
        public bool Reporting {
            get { return reporting; }
            set { reporting = value; }
        }
#endif
        #endregion
        #region test_msagl

#if TEST_MSAGL
        static Show show;

        /// <summary>
        /// Used for debugging purposes to display all kind of intermediate curves.
        /// </summary>
        public static Show Show {
            get { return show; }
            set { show = value; }
        }

        /// <summary>
        /// Used for debugging purposes to display all kind of intermediate curves with widht and color
        /// </summary>
        public static ShowDebugCurves ShowDebugCurves { get; set; }


        /// <summary>
        /// Used for debugging purposes to display all kind of intermediate curves with width and color
        /// </summary>
        public static ShowDebugCurvesEnumeration ShowDebugCurvesEnumeration { get; set; }


        static ShowDatabase showDataBase;

        /// <summary>
        /// used for debugging purposes to display anchors and paths that are kept in the DataBase
        /// </summary>
        public static ShowDatabase ShowDatabase {
            get { return showDataBase; }
            set { showDataBase = value; }
        }

        ///<summary>
        ///</summary>
        public static ShowGraph ShowGraph { get; set; }

#endif

        #endregion

        double packingAspectRatio = PackingConstants.GoldenRatio;

        /// <summary>
        /// Controls the ideal aspect ratio for packing disconnected components
        /// </summary>
        public double PackingAspectRatio {
            get { return packingAspectRatio; }
            set { packingAspectRatio = value; }
        }

        PackingMethod packingMethod = PackingMethod.Compact;

        /// <summary>
        /// the packing method
        /// </summary>
        public PackingMethod PackingMethod {
            get { return packingMethod; }
            set { packingMethod = value; }
        }

        double nodeSeparation = 10;

        /// <summary>
        /// When AvoidOverlaps is set, we optionally enforce a little extra space around nodes
        /// </summary>
        public double NodeSeparation {
            get { return nodeSeparation; }
            set { nodeSeparation = value; }
        }

        double clusterMargin = 10;
        
        
        /// <summary>
        /// When AvoidOverlaps is set, we optionally enforce a little extra space between nodes and cluster boundaries
        /// </summary>
        public double ClusterMargin {
            get { return clusterMargin; }
            set { clusterMargin = value; }
        }
        

        /// <summary>
        /// Clones the object
        /// </summary>
        /// <returns></returns>
        public abstract LayoutAlgorithmSettings Clone();
    }
}