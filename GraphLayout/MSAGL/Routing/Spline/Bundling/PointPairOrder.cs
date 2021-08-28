using System.Collections.Generic;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    internal class PointPairOrder {
        //order for node u of edge u->v
        internal List<PolyWithIndex> Metrolines = new List<PolyWithIndex>();
        internal bool orderFixed;
        internal Dictionary<PolyWithIndex, int> LineIndexInOrder;

        internal void Add(PolyWithIndex metroline) {
            Metrolines.Add(metroline);
        }
    }
}