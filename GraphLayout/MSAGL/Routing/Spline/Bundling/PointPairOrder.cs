using System.Collections.Generic;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    internal class PointPairOrder {
        //order for node u of edge u->v
        internal List<Metroline> Metrolines = new List<Metroline>();
        internal bool orderFixed;
        internal Dictionary<Metroline, int> LineIndexInOrder;

        internal void Add(Metroline metroline) {
            Metrolines.Add(metroline);
        }
    }
}