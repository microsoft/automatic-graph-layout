using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Drawing {
    ///<summary>
    /// keeps constraints for layered layouts
    ///</summary>
    public class LayerConstraints {
        readonly HorizontalConstraintsForLayeredLayout horizontalConstraints =
            new HorizontalConstraintsForLayeredLayout();

        readonly VerticalConstraintsForLayeredLayout verticalConstraints = new VerticalConstraintsForLayeredLayout();

        /// <summary>
        /// _minLayerOfDrawingGraph, _maxLayerOfDrawingGraph, same layer, up-down, up-down vertical and left-right constraints are supported by this class
        /// </summary>
        internal VerticalConstraintsForLayeredLayout VerticalConstraints {
            get { return verticalConstraints; }
        }

        internal HorizontalConstraintsForLayeredLayout HorizontalConstraints {
            get { return horizontalConstraints; }
        }

        /// <summary>
        /// adds a constraint to keep one node to the left of another on the same layer
        /// </summary>
        /// <param name="leftNode"></param>
        /// <param name="rightNode"></param>
        public void AddLeftRightConstraint(Node leftNode, Node rightNode) {
            HorizontalConstraints.LeftRightConstraints.Insert(new Tuple<Node, Node>(leftNode, rightNode));
        }

        /// <summary>
        /// removes a left-right constraint from
        /// </summary>
        /// <param name="leftNode"></param>
        /// <param name="rightNode"></param>
        public void RemoveLeftRightConstraint(Node leftNode, Node rightNode) {
            HorizontalConstraints.LeftRightConstraints.Remove(new Tuple<Node, Node>(leftNode, rightNode));
        }


        /// <summary>
        /// Pins the nodes of the list to the max layer and 
        /// </summary>
        public void PinNodesToMaxLayer(params Node[] nodes) {
            for (int i = 0; i < nodes.Length; i++)
                VerticalConstraints.PinNodeToMaxLayer(nodes[i]);
        }

        /// <summary>
        /// Pins the nodes of the list to the min layer and 
        /// </summary>
        public void PinNodesToMinLayer(params Node[] nodes) {
            for (int i = 0; i < nodes.Length; i++)
                VerticalConstraints.PinNodeToMinLayer(nodes[i]);
        }

        /// <summary>
        /// adds a same layer constraint
        /// </summary>
        public void PinNodesToSameLayer(params Node[] nodes) {
            for (int i = 1; i < nodes.Length; i++)
                VerticalConstraints.SameLayerConstraints.Insert(new Tuple<Node, Node>(nodes[0], nodes[i]));
        }


        /// <summary>
        /// these nodes belong to the same layer and are adjacent positioned from left to right
        /// </summary>
        /// <param name="neighbors"></param>
        public void AddSameLayerNeighbors(params Node[] neighbors) {
            AddSameLayerNeighbors(new List<Node>(neighbors));
        }

        /// <summary>
        /// these nodes belong to the same layer and are adjacent positioned from left to right
        /// </summary>
        /// <param name="neighbors"></param>
        public void AddSameLayerNeighbors(IEnumerable<Node> neighbors) {
            var neibs = new List<Node>(neighbors);
            HorizontalConstraints.AddSameLayerNeighbors(neibs);
            for (int i = 0; i < neibs.Count - 1; i++)
                VerticalConstraints.SameLayerConstraints.Insert(new Tuple<Node, Node>(neibs[i], neibs[i + 1]));
        }

        /// <summary>
        /// adds a pair of adjacent neighbors
        /// </summary>
        /// <param name="leftNode"></param>
        /// <param name="rightNode"></param>
        public void AddSameLayerNeighbors(Node leftNode, Node rightNode) {
            HorizontalConstraints.AddSameLayerNeighborsPair(leftNode, rightNode);
            VerticalConstraints.SameLayerConstraints.Insert(new Tuple<Node, Node>(leftNode, rightNode));
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveAllConstraints() {
            HorizontalConstraints.Clear();
            VerticalConstraints.Clear();
        }


        /// <summary>
        /// adds an up-down constraint to the couple of nodes
        /// </summary>
        /// <param name="upperNode"></param>
        /// <param name="lowerNode"></param>
        public void AddUpDownConstraint(Node upperNode, Node lowerNode) {
            VerticalConstraints.UpDownConstraints.Insert(new Tuple<Node, Node>(upperNode, lowerNode));
        }

        /// <summary>
        /// adds a constraint where the top node center is positioned exactly above the lower node center
        /// </summary>
        /// <param name="upperNode"></param>
        /// <param name="lowerNode"></param>
        public void AddUpDownVerticalConstraint(Node upperNode, Node lowerNode) {
            VerticalConstraints.UpDownConstraints.Insert(new Tuple<Node, Node>(upperNode, lowerNode));
            HorizontalConstraints.UpDownVerticalConstraints.Insert(new Tuple<Node, Node>(upperNode, lowerNode));
        }

        /// <summary>
        /// adds a sequence of constraints where the top node center is positioned exactly above the lower node center
        /// </summary>        
        public void AddSequenceOfUpDownVerticalConstraint(params Node[]  nodes) {
            for (int i = 0; i < nodes.Length - 1; i++)
                AddUpDownVerticalConstraint(nodes[i], nodes[i + 1]);
        }

        /// <summary>
        /// adds vertical up down constraints udDownIds[0]->upDownIds[1]-> ... -> upDownsIds[upDownIds.Length-1]
        /// </summary>
        /// <param name="upDownNodes"></param>
        public void AddUpDownVerticalConstraints(params Node[] upDownNodes) {
            for (int i = 1; i < upDownNodes.Length; i++)
                AddUpDownVerticalConstraint(upDownNodes[i - 1], upDownNodes[i]);
        }
    }
}