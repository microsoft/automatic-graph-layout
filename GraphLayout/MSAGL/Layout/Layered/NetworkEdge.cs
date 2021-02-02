using System;
using Microsoft.Msagl.Core.GraphAlgorithms;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// Differs from IntEdge in containing a flag indicating belonging to the tree
    /// and containing the cut value
    /// </summary>
    internal class NetworkEdge : PolyIntEdge {

        internal const int Infinity = Int32.MaxValue;
        internal NetworkEdge(PolyIntEdge e)
            : base(e.Source, e.Target) {
            Weight = e.Weight;
            Separation = e.Separation;
        }
        internal bool inTree;
        int cut = Infinity;

        internal int Cut {
            get { return cut; }
            set { cut = value; }
        }
    }

}
