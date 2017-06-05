using System;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Drawing {
    internal class VerticalConstraintsForLayeredLayout {
        internal readonly Set<Node> _maxLayerOfDrawingGraph = new Set<Node>();
        internal readonly Set<Node> _minLayerOfDrawingGraph = new Set<Node>();

        public void PinNodeToMaxLayer(Node node) {
            _maxLayerOfDrawingGraph.Insert(node);
        }

        public void PinNodeToMinLayer(Node node)
        {
            System.Diagnostics.Debug.Assert(node != null);
            _minLayerOfDrawingGraph.Insert(node);
        }

        internal Set<Tuple<Node, Node>> SameLayerConstraints = new Set<Tuple<Node, Node>>();
        internal readonly Set<Tuple<Node, Node>> UpDownConstraints = new Set<Tuple<Node, Node>>();
        public void Clear() {
            _maxLayerOfDrawingGraph.Clear();
            _minLayerOfDrawingGraph.Clear();
            SameLayerConstraints.Clear();
            UpDownConstraints.Clear();
        }
    }
}