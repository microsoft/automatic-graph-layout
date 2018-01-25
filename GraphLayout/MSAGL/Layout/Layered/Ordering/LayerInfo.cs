using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Layout.Layered {
    internal class LayerInfo {
        /// <summary>
        /// constrained on the level of neighBlocks
        /// </summary>
        internal Set<Tuple<int, int>> leftRight = new Set<Tuple<int, int>>();
        internal Set<Tuple<int, int>> flatEdges = new Set<Tuple<int, int>>();
        internal Dictionary<int, List<int>> neigBlocks = new Dictionary<int, List<int>>();
        internal Dictionary<int, int> constrainedFromAbove = new Dictionary<int, int>();
        internal Dictionary<int, int> constrainedFromBelow = new Dictionary<int, int>();
        internal Dictionary<int, int> nodeToBlockRoot = new Dictionary<int, int>();
        /// <summary>
        /// if the block contains a fixed node v,  it can be only one because of the monotone paths feature,
        /// then blockToFixedNodeOfBlock[block]=v
        /// /// </summary>
        
        internal Dictionary<int, int> blockRootToVertConstrainedNodeOfBlock = new Dictionary<int, int>();
    }
}
