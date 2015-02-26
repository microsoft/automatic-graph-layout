/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
