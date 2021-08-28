using System.Collections.Generic;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    internal class PointPairOrder {
        //order for node u of edge u->v
        internal List<IPolyWithIndex> Metrolines = new List<IPolyWithIndex>();
        internal bool orderFixed;
        internal Dictionary<IPolyWithIndex, int> LineIndexInOrder;

        internal void Add(IPolyWithIndex metroline) {
            Metrolines.Add(metroline);
        }
    }
}