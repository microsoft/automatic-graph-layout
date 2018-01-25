using System;

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// the base class for LgNodeInfo and LgEdgeInfo
    /// </summary>
    public class LgInfoBase {
        double _slidingZoomLevel = double.PositiveInfinity;
        double _zoomLevel = double.PositiveInfinity;
        double _rank;

        /// <summary>
        /// 
        /// </summary>
        public double SlidingZoomLevel {
            get { return _slidingZoomLevel; }
            set { _slidingZoomLevel = value; }
        }

        /// <summary>
        /// if the zoom is at least ZoomLevel the node should be rendered
        /// </summary>
        public double ZoomLevel {
            get { return Math.Min(_zoomLevel, SlidingZoomLevel); }
            set { _zoomLevel = value; }
        }

        /// <summary>
        /// the rank of the element
        /// </summary>
        public double Rank {
            get { return _rank; }
            set { _rank = value; }
        }

        internal bool ZoomLevelIsNotSet {
            get { return ZoomLevel == double.PositiveInfinity; }

        }
    }
}