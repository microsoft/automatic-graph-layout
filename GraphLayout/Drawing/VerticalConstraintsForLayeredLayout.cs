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

        /// <summary>
        /// unpins a node from max layer
        /// </summary>
        /// <param name="node"></param>
        internal void UnpinNodeFromMaxLayer(Node node)
        {
            _maxLayerOfDrawingGraph.Remove(node);
        }

        
        /// <summary>
        /// unpins a node from min layer
        /// </summary>
        /// <param name="node"></param>
        internal void UnpinNodeFromMinLayer(Node node)
        {
            _minLayerOfDrawingGraph.Remove(node);
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