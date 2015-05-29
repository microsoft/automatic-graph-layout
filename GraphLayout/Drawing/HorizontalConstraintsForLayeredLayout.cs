using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Drawing
{
    internal class HorizontalConstraintsForLayeredLayout {
        internal Set<Tuple<Node, Node>> UpDownVerticalConstraints = new Set<Tuple<Node, Node>>();
        readonly Set<Tuple<Node, Node>> leftRightConstraints = new Set<Tuple<Node, Node>>();

        internal Set<Tuple<Node, Node>> LeftRightConstraints
        {
            get { return leftRightConstraints; }
        }

        public void AddSameLayerNeighbors(List<Node> neighbors)
        {
            for (int i = 0; i < neighbors.Count - 1; i++)
                AddSameLayerNeighborsPair(neighbors[i], neighbors[i + 1]);

        }

        internal readonly Set<Tuple<Node, Node>> LeftRightNeighbors = new Set<Tuple<Node, Node>>();
        internal void AddSameLayerNeighborsPair(Node leftNode, Node rightNode)
        {
            LeftRightNeighbors.Insert(new Tuple<Node, Node>(leftNode, rightNode));
        }

        public void Clear() {
            leftRightConstraints.Clear();
            LeftRightNeighbors.Clear();

        }
    }
}