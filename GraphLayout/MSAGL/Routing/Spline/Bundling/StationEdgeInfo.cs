using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    internal class StationEdgeInfo {

        internal int Count {get {return this.Metrolines.Count;} }
        internal double Width;

        internal List<Metroline> Metrolines = new List<Metroline>();

        #region cache

        internal double cachedBundleCost;

        //internal Polyline cachedBoundary;

        #endregion
    }
}