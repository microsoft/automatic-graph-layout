using Microsoft.Msagl.Core.GraphAlgorithms;

namespace Microsoft.Msagl.Prototype.LayoutEditing {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class WeightedEdge : IEdge {
        int source;

        /// <summary>
        /// source
        /// </summary>
        public int Source {
            get { return source; }
            set { source = value; }
        }
        int target;

        /// <summary>
        /// source
        /// </summary>
        public int Target {
            get { return target; }
            set { target = value; }
        }

        double weight;

        internal double Weight {
            get { return weight; }
            set { weight = value; }
        }

    
        internal WeightedEdge(int source, int target, double weight) {
            Source = source;
            Target = target;
            Weight = weight;
        }

    }
}
